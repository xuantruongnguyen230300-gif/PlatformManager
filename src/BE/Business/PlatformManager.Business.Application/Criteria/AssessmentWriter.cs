using System.Globalization;
using PlatformManager.Business.Application.Common;
using PlatformManager.Business.Domain.Entities;
using PlatformManager.Core.Application.Common.Interfaces;
using PlatformManager.Core.Application.Common.Results;
using CriteriaEntity = PlatformManager.Business.Domain.Entities.Criteria;

namespace PlatformManager.Business.Application.Criteria;

/// <summary>
/// Kết quả một lời ghi đánh giá: hoặc kỳ đích đã ghi xong, hoặc lỗi kèm tham số câu.
/// </summary>
/// <param name="TargetWeek">Kỳ đích đã ghi — <c>null</c> khi thất bại.</param>
public sealed record AssessmentWriteResult(
    PeriodRange? TargetWeek,
    ErrorDescriptor? Error,
    (string Name, object? Value)[] ErrorArgs)
{
    public bool IsSuccess => TargetWeek is not null;

    public static AssessmentWriteResult Ok(PeriodRange week) => new(week, null, []);

    public static AssessmentWriteResult Fail(ErrorDescriptor error, params (string Name, object? Value)[] args) =>
        new(null, error, args);
}

/// <summary>
/// <b>Đường ghi đánh giá DÙNG CHUNG của DM-4 (dialog) và DM-6 (sửa inline).</b>
///
/// <para>Lớp này tồn tại để hai card đó KHÔNG mọc nhánh riêng: cả hai gọi cùng một
/// <see cref="WritePeriodResolver"/> (bước 1) và cùng một <see cref="AssessmentUpsert"/> (bước 2),
/// và khác nhau đúng ở <see cref="AssessmentWrite"/> chúng dựng — 6 trường với dialog, 2 trường với
/// inline. Ranh giới trường đó là ràng buộc của hợp đồng (Q9), không phải giới hạn tạm thời, nên nó
/// nằm ở NƠI GỌI chứ không ở đây.</para>
///
/// <para>Đường IMPORT (DM-7) không đi qua lớp này vì nó ghi theo LÔ — nhưng nó gọi đúng hai thành
/// phần trên, nên vẫn là một luật ghi cho cả ba đường.</para>
/// </summary>
public sealed class AssessmentWriter(ICriteriaWriteRepository repository, IDateTimeProvider clock)
{
    /// <summary>
    /// Ghi một lời đánh giá vào kỳ đích. KHÔNG gọi <c>SaveChanges</c> — handler own việc đó.
    /// </summary>
    public async Task<AssessmentWriteResult> WriteAsync(
        CriteriaEntity criteria,
        string? period,
        int? year,
        string? version,
        AssessmentWrite fields,
        CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);

        // ── Bước 1 — kỳ đích ────────────────────────────────────────────────────────────────
        var resolution = WritePeriodResolver.Resolve(period, year, today);

        if (!resolution.IsAccepted)
        {
            return AssessmentWriteResult.Fail(
                PeriodError(resolution.Rejection),
                ("Period", period), ("Year", year), ("CurrentYear", today.Year));
        }

        var targetWeek = resolution.TargetWeek!;

        // ── Kiểm giá trị TRƯỚC khi chạm entity ──────────────────────────────────────────────
        // Đặt trước có chủ đích: entity chỉ được sửa khi mọi thứ đã hợp lệ, nếu không một lời ghi
        // hỏng nửa chừng vẫn để lại thay đổi trong change tracker — và lần SaveChanges của việc
        // khác trong cùng request sẽ cuốn nó theo.
        var scoreError = ValidateScores(criteria, fields);
        if (scoreError is not null)
            return scoreError;

        if (fields.OwnerId is { IsSet: true, Value: { } ownerId }
            && !await repository.OwnerExistsAsync(ownerId, ct))
        {
            return AssessmentWriteResult.Fail(CriteriaErrors.OwnerNotFound);
        }

        // ── Bước 2 — ghi vào bản ghi nào của kỳ đích ────────────────────────────────────────
        var existing = await repository.FindAssessmentInRangeAsync(
            criteria.Id, targetWeek.Start, targetWeek.End, ct);

        if (existing is not null)
        {
            // Tranh chấp ghi: so token client gửi với token vừa đọc từ DB TRONG CHÍNH request này.
            // Kiểm TRƯỚC khi sửa nên lệch ⇒ không ghi gì.
            //
            // Chỉ kiểm khi kỳ đích ĐÃ CÓ bản ghi: chưa có thì không có gì để ghi đè, nên không có
            // xung đột nào để phát hiện — trả 409 ở đó là từ chối một lời ghi hoàn toàn an toàn.
            if (!string.IsNullOrWhiteSpace(version)
                && !string.Equals(version, existing.Version.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal))
            {
                return AssessmentWriteResult.Fail(CriteriaErrors.AssessmentConflict);
            }

            // Nguồn copy-forward là null: trường không gửi thì GIỮ NGUYÊN giá trị đang có, không
            // kéo lại số của kỳ trước.
            AssessmentUpsert.Apply(existing, copyForwardSource: null, fields);

            return AssessmentWriteResult.Ok(targetWeek);
        }

        var assessment = CriteriaAssessment.Create(
            criteria.Id, WritePeriodResolver.AnchorDate(targetWeek, today));

        var prior = await repository.FindLatestAssessmentBeforeAsync(criteria.Id, targetWeek.Start, ct);

        AssessmentUpsert.Apply(assessment, prior, fields);
        repository.AddAssessment(assessment);

        return AssessmentWriteResult.Ok(targetWeek);
    }

    /// <summary>
    /// Điểm vượt trần ⇒ <c>422</c>. So với <c>MaxScore</c> ĐANG LƯU của chỉ tiêu, không phải một
    /// con số nào khác trong request: DM-4 cho phép sửa <c>maxScore</c> cùng lượt, nên nơi gọi phải
    /// gán giá trị mới vào entity TRƯỚC khi gọi hàm này — xem <c>UpdateCriteriaHandler</c>.
    /// </summary>
    private static AssessmentWriteResult? ValidateScores(CriteriaEntity criteria, AssessmentWrite fields)
    {
        if (fields.SelfScore is { IsSet: true, Value: { } self } && self > criteria.MaxScore)
        {
            return AssessmentWriteResult.Fail(
                CriteriaErrors.AssessmentSelfScoreExceedsMax,
                ("SelfScore", self), ("MaxScore", criteria.MaxScore));
        }

        if (fields.VerifiedScore is { IsSet: true, Value: { } verified } && verified > criteria.MaxScore)
        {
            return AssessmentWriteResult.Fail(
                CriteriaErrors.AssessmentVerifiedScoreExceedsMax,
                ("VerifiedScore", verified), ("MaxScore", criteria.MaxScore));
        }

        return null;
    }

    /// <summary>
    /// Ánh xạ lý do từ chối kỳ sang catalog CỦA ĐƯỜNG GHI TAY. Cùng bộ giải với đường import, khác
    /// bộ mã — xem docstring của <see cref="WritePeriodRejection"/>.
    /// </summary>
    private static ErrorDescriptor PeriodError(WritePeriodRejection rejection) => rejection switch
    {
        WritePeriodRejection.Required => CriteriaErrors.AssessmentPeriodRequired,
        WritePeriodRejection.NotWeekly => CriteriaErrors.AssessmentPeriodNotWeekly,
        WritePeriodRejection.OutOfYear => CriteriaErrors.AssessmentPeriodOutOfYear,
        _ => CriteriaErrors.AssessmentPeriodInvalid,
    };
}

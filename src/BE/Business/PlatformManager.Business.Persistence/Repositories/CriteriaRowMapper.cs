using System.Globalization;
using PlatformManager.Business.Application.Common;
using PlatformManager.Business.Application.Criteria;
using PlatformManager.Business.Application.Dashboard;

namespace PlatformManager.Business.Persistence.Repositories;

/// <summary>Phần dữ liệu của một dòng đến từ bảng <c>Criteria</c> + <c>CriteriaGroups</c>.</summary>
internal sealed record CriteriaRowSource(
    Guid CriteriaId, string Code, string Name, Guid GroupId, string GroupCode, string GroupName, decimal MaxScore);

/// <summary>Phần dữ liệu đến từ bản ghi đánh giá đại diện của kỳ.</summary>
internal sealed record AssessmentSource(
    Guid Id,
    Guid CriteriaId,
    DateOnly AssessmentDate,
    int? ProgressPercent,
    decimal? SelfScore,
    decimal? VerifiedScore,
    string? Status,
    Guid? OwnerId,
    DateOnly? Deadline,
    string? Note,
    uint Version);

/// <summary>
/// MỘT chỗ dựng <see cref="CriteriaRowDto"/> — dùng chung bởi lưới (DM-2) và bởi phản hồi của hai
/// đường ghi (DM-4, DM-6).
///
/// <para><b>Vì sao phải là một chỗ:</b> hợp đồng khai response của DM-4/DM-6 là <i>"cùng shape với
/// một dòng của DM-2, để FE thay thẳng dòng trong lưới"</i>. Hai bộ dựng rời nhau nghĩa là một
/// trường dẫn xuất (<c>diff</c>, nhãn kỳ, tên người phụ trách) có thể tính khác nhau ở hai đường —
/// và triệu chứng là dòng vừa sửa trông khác các dòng còn lại của chính lưới đó, thứ không test
/// nào bắt được vì cả hai đường đều "chạy đúng" theo định nghĩa riêng.</para>
/// </summary>
internal static class CriteriaRowMapper
{
    public static CriteriaRowDto ToDto(
        CriteriaRowSource row, AssessmentSource? assessment, IReadOnlyDictionary<Guid, string> ownerNames)
    {
        if (assessment is null)
        {
            // Chỉ tiêu chưa có bản ghi đánh giá trong kỳ: dòng VẪN hiện, mọi trường đánh giá vắng
            // mặt (§5.2). Vắng mặt chứ không phải "null" trên dây — luật casing §0.
            return new CriteriaRowDto(
                row.CriteriaId, row.Code, row.Name, row.GroupId, row.GroupCode, row.GroupName, row.MaxScore,
                null, null, null, null, null, null, null, null, null, null, null, null, null, null);
        }

        // Kỳ của CHÍNH dòng này (Q31) — LUÔN là một tuần, vì sau Q37 không đường nào ghi dữ liệu
        // vào kỳ tháng. BE dựng sẵn cả nhãn: FE ghép được thì FE phải có lịch ISO riêng.
        var rowPeriod = PeriodRange.IsoWeekOf(assessment.AssessmentDate);

        return new CriteriaRowDto(
            row.CriteriaId,
            row.Code,
            row.Name,
            row.GroupId,
            row.GroupCode,
            row.GroupName,
            row.MaxScore,
            assessment.Id,
            assessment.AssessmentDate,
            assessment.ProgressPercent,
            assessment.SelfScore,
            assessment.VerifiedScore,
            DashboardCalculator.Diff(assessment.SelfScore, assessment.VerifiedScore),
            assessment.Status,
            assessment.OwnerId,
            assessment.OwnerId is { } ownerId ? ownerNames.GetValueOrDefault(ownerId) : null,
            assessment.Deadline,
            assessment.Note,
            assessment.Version.ToString(CultureInfo.InvariantCulture),
            rowPeriod.Value,
            PeriodLabels.Full(rowPeriod));
    }
}

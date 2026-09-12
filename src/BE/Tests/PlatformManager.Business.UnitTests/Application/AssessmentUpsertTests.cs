using PlatformManager.Business.Application.Common;
using PlatformManager.Business.Application.Criteria;
using PlatformManager.Business.Domain.Entities;
using Xunit;

namespace PlatformManager.Business.UnitTests.Application;

/// <summary>
/// <b>Bước 2 của luật ghi — copy-forward</b> (spec/danh-muc-dti/business-rules.md §5.3).
///
/// <para>Đây là thứ chặn đúng một lỗi cụ thể và rất đắt: người dùng sửa mỗi <c>Tiến độ %</c>, hệ
/// thống tạo một bản ghi mới cho tuần đó với điểm số TRỐNG, và Dashboard của tuần đó lập tức báo
/// tụt về 0. Dữ liệu đúng theo từng lời ghi, sai theo cái người ta đọc.</para>
/// </summary>
public class AssessmentUpsertTests
{
    private static readonly Guid CriteriaId = Guid.CreateVersion7();

    private static CriteriaAssessment Prior()
    {
        var prior = CriteriaAssessment.Create(CriteriaId, new DateOnly(2026, 8, 9));
        prior.SetScores(7.5m, 6m);
        prior.SetProgressPercent(80);
        prior.SetStatus(AssessmentStatuses.InProgress);
        prior.SetDeadline(new DateOnly(2026, 9, 30));
        prior.SetNote("ghi chú kỳ trước");
        return prior;
    }

    [Fact(DisplayName = "Bản ghi MỚI: trường không gửi được copy-forward từ kỳ trước")]
    public void NewRecord_CopiesForward_UnsentFields()
    {
        var target = CriteriaAssessment.Create(CriteriaId, new DateOnly(2026, 8, 12));

        AssessmentUpsert.Apply(target, Prior(), new AssessmentWrite
        {
            ProgressPercent = Assigned<int?>.Set(95),
        });

        Assert.Equal(95, target.ProgressPercent);

        // Năm trường không gửi giữ nguyên giá trị của kỳ trước — đây chính là thứ giữ Dashboard
        // khỏi tụt về 0.
        Assert.Equal(7.5m, target.SelfScore);
        Assert.Equal(6m, target.VerifiedScore);
        Assert.Equal(AssessmentStatuses.InProgress, target.Status);
        Assert.Equal(new DateOnly(2026, 9, 30), target.Deadline);
        Assert.Equal("ghi chú kỳ trước", target.Note);
    }

    [Fact(DisplayName = "Bản ghi MỚI, KHÔNG có kỳ trước: trường không gửi để TRỐNG")]
    public void NewRecord_WithoutPrior_LeavesUnsentFieldsEmpty()
    {
        var target = CriteriaAssessment.Create(CriteriaId, new DateOnly(2026, 8, 12));

        AssessmentUpsert.Apply(target, copyForwardSource: null, new AssessmentWrite
        {
            SelfScore = Assigned<decimal?>.Set(4m),
        });

        Assert.Equal(4m, target.SelfScore);
        Assert.Null(target.VerifiedScore);
        Assert.Null(target.ProgressPercent);
        Assert.Null(target.Status);
        Assert.Null(target.Note);
    }

    /// <summary>
    /// Cập nhật bản ghi ĐÃ CÓ của kỳ đích: nơi gọi truyền <c>null</c> làm nguồn copy-forward, nên
    /// trường không gửi GIỮ NGUYÊN giá trị đang có — không kéo lại số của kỳ trước.
    /// </summary>
    [Fact(DisplayName = "Cập nhật bản ghi đã có: trường không gửi giữ nguyên, KHÔNG kéo số kỳ trước")]
    public void ExistingRecord_KeepsOwnValues_ForUnsentFields()
    {
        var target = CriteriaAssessment.Create(CriteriaId, new DateOnly(2026, 8, 12));
        target.SetScores(2m, 1m);
        target.SetNote("ghi chú của chính kỳ này");

        AssessmentUpsert.Apply(target, copyForwardSource: null, new AssessmentWrite
        {
            ProgressPercent = Assigned<int?>.Set(10),
        });

        Assert.Equal(2m, target.SelfScore);
        Assert.Equal(1m, target.VerifiedScore);
        Assert.Equal("ghi chú của chính kỳ này", target.Note);
        Assert.Equal(10, target.ProgressPercent);
    }

    /// <summary>
    /// Ca phân biệt <b>vắng mặt</b> với <b>gửi null</b> — toàn bộ lý do <see cref="Assigned{T}"/>
    /// tồn tại. Gộp hai nghĩa thì không có cách nào xoá trắng một ô qua dialog.
    /// </summary>
    [Fact(DisplayName = "Gửi tường minh null ⇒ XOÁ TRẮNG, không copy-forward")]
    public void ExplicitNull_Clears_DoesNotCopyForward()
    {
        var target = CriteriaAssessment.Create(CriteriaId, new DateOnly(2026, 8, 12));

        AssessmentUpsert.Apply(target, Prior(), new AssessmentWrite
        {
            SelfScore = Assigned<decimal?>.Set(null),
            Note = Assigned<string?>.Set(null),
        });

        Assert.Null(target.SelfScore);
        Assert.Null(target.Note);

        // Trường KHÔNG gửi trong cùng lời ghi đó vẫn copy-forward bình thường.
        Assert.Equal(6m, target.VerifiedScore);
    }

    [Fact(DisplayName = "ProgressPercent ngoài 0..100 bị KẸP về biên, không báo lỗi (§3.2)")]
    public void ProgressPercent_IsClamped()
    {
        var target = CriteriaAssessment.Create(CriteriaId, new DateOnly(2026, 8, 12));

        AssessmentUpsert.Apply(target, null, new AssessmentWrite
        {
            ProgressPercent = Assigned<int?>.Set(180),
        });

        Assert.Equal(100, target.ProgressPercent);
    }
}

using PlatformManager.Core.Domain.Common;

namespace PlatformManager.Modules.DtiWeekly.Domain.Entities;

/// <summary>
/// 1 bản ghi đánh giá của 1 Criteria, gắn với 1 mốc thời gian — phần NGÀY của
/// BaseEntity.DateCreate CHÍNH LÀ "kỳ" nghiệp vụ của record này (xem doc/ERD/ERD.md
/// mục "Kỳ (tuần/tháng/năm) — khái niệm ngầm định"). Cùng ngày ghi đè (UPDATE), khác
/// ngày insert record mới copy-forward baseline — cơ chế này nằm ở
/// Application/Assessments/AssessmentUpsertService.cs, KHÔNG nằm trong entity.
/// </summary>
public class CriteriaAssessment : BaseEntity
{
    /// <summary>4 giá trị quan sát được từ CSV mẫu — nhập tay ở quy trình thẩm định
    /// riêng, KHÔNG qua tab "Đánh giá theo tuần" (xem business-rules.md mục 5).</summary>
    public static readonly string[] ValidStatuses =
    [
        "Chưa thực hiện",
        "Đang thực hiện",
        "Cần bổ sung minh chứng",
        "Hoàn thành",
    ];

    public Guid CriteriaId { get; private set; }
    public decimal ProgressPercent { get; private set; }
    public decimal? SelfScore { get; private set; }
    public decimal? VerifiedScore { get; private set; }
    public string? Status { get; private set; }
    public Guid? OwnerId { get; private set; }
    public DateOnly? Deadline { get; private set; }
    public string? Note { get; private set; }

    private CriteriaAssessment() { }

    public static CriteriaAssessment Create(Guid criteriaId)
    {
        if (criteriaId == Guid.Empty)
            throw new DomainException("CRITERIA_ASSESSMENT_CRITERIA_REQUIRED", "CriteriaId không được để trống.");

        return new CriteriaAssessment
        {
            Id = EntityId.New(),
            CriteriaId = criteriaId,
        };
    }

    /// <summary>Kẹp [0,100] — giá trị không hợp lệ (NaN thực tế đã bị chặn ở tầng
    /// FluentValidation/model binding) vẫn được kẹp an toàn, khớp hành vi JS gốc
    /// Math.max(0,Math.min(100,...)).</summary>
    public void UpdateProgress(decimal progressPercent)
        => ProgressPercent = Math.Clamp(progressPercent, 0m, 100m);

    public void UpdateNote(string? note) => Note = note;

    /// <summary>Chỉ luồng Import mới gọi — nhập tay không có control cho 2 field này
    /// (xem spec/danh-muc-dti/business-rules.md mục 2.2/2.3).</summary>
    public void UpdateSelfAssessment(decimal? selfScore, decimal? verifiedScore)
    {
        if (selfScore is < 0)
            throw new DomainException("CRITERIA_ASSESSMENT_SELF_SCORE_INVALID", "Điểm tự đánh giá không được âm.");
        if (verifiedScore is < 0)
            throw new DomainException("CRITERIA_ASSESSMENT_VERIFIED_SCORE_INVALID", "Điểm thẩm định không được âm.");

        SelfScore = selfScore;
        VerifiedScore = verifiedScore;
    }

    public void UpdateStatus(string? status)
    {
        if (status is not null && !ValidStatuses.Contains(status))
            throw new DomainException("CRITERIA_ASSESSMENT_STATUS_INVALID", $"Trạng thái '{status}' không hợp lệ.");

        Status = status;
    }

    public void AssignOwner(Guid? ownerId) => OwnerId = ownerId;

    public void SetDeadline(DateOnly? deadline) => Deadline = deadline;

    /// <summary>Copy-forward 7 field nghiệp vụ từ record baseline (kỳ gần nhất trước
    /// đó) khi tạo record mới cho "hôm nay" — KHÔNG copy CriteriaEvidence (rule riêng,
    /// xem doc/ERD/ERD.md entity 5).</summary>
    public void CopyBaselineFrom(CriteriaAssessment baseline)
    {
        ArgumentNullException.ThrowIfNull(baseline);

        ProgressPercent = baseline.ProgressPercent;
        SelfScore = baseline.SelfScore;
        VerifiedScore = baseline.VerifiedScore;
        Status = baseline.Status;
        OwnerId = baseline.OwnerId;
        Deadline = baseline.Deadline;
        Note = baseline.Note;
    }
}

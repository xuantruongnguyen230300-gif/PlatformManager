namespace PlatformManager.Api.Entities;

/// <summary>
/// Minh chứng/ghi chú dạng danh sách (1–N) của 1 <see cref="CriteriaAssessment"/>.
/// KHÔNG copy-forward khi hệ thống tự tạo record CriteriaAssessment mới —
/// xem doc/ERD/ERD.md mục 5.
/// </summary>
public class CriteriaEvidence : BaseEntity
{
    public Guid CriteriaAssessmentId { get; set; }
    public string Content { get; set; } = string.Empty;
    public int OrderIndex { get; set; }

    public CriteriaAssessment? CriteriaAssessment { get; set; }
}

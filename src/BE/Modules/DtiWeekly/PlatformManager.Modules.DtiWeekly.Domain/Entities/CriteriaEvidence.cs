using PlatformManager.Core.Domain.Common;

namespace PlatformManager.Modules.DtiWeekly.Domain.Entities;

/// <summary>
/// Minh chứng/ghi chú (1–N) gắn với đúng 1 CriteriaAssessment cụ thể — KHÔNG copy-forward
/// khi tạo record CriteriaAssessment mới (xem doc/ERD/ERD.md entity 5, đã chốt 2026-08-12).
/// </summary>
public class CriteriaEvidence : BaseEntity
{
    public Guid CriteriaAssessmentId { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public int OrderIndex { get; private set; }

    private CriteriaEvidence() { }

    public static CriteriaEvidence Create(Guid criteriaAssessmentId, string content, int orderIndex)
    {
        if (criteriaAssessmentId == Guid.Empty)
            throw new DomainException("CRITERIA_EVIDENCE_ASSESSMENT_REQUIRED", "CriteriaAssessmentId không được để trống.");
        if (string.IsNullOrWhiteSpace(content))
            throw new DomainException("CRITERIA_EVIDENCE_CONTENT_REQUIRED", "Nội dung minh chứng không được để trống.");

        return new CriteriaEvidence
        {
            Id = EntityId.New(),
            CriteriaAssessmentId = criteriaAssessmentId,
            Content = content.Trim(),
            OrderIndex = orderIndex,
        };
    }
}

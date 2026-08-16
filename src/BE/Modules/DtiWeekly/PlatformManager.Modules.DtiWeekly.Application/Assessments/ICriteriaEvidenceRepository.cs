using PlatformManager.Modules.DtiWeekly.Domain.Entities;

namespace PlatformManager.Modules.DtiWeekly.Application.Assessments;

public interface ICriteriaEvidenceRepository
{
    /// <summary>Xoá toàn bộ minh chứng hiện có của 1 CriteriaAssessment — Import GHI ĐÈ
    /// toàn bộ (xoá hết, ghi lại từ CSV mỗi lần), xem doc/ERD/ERD.md entity 5.</summary>
    Task RemoveAllForAssessmentAsync(Guid criteriaAssessmentId, CancellationToken ct);

    Task AddRangeAsync(IEnumerable<CriteriaEvidence> evidence, CancellationToken ct);

    Task<List<CriteriaEvidence>> GetForAssessmentAsync(Guid criteriaAssessmentId, CancellationToken ct);
}

using PlatformManager.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using PlatformManager.Modules.DtiWeekly.Application.Assessments;
using PlatformManager.Modules.DtiWeekly.Domain.Entities;

namespace PlatformManager.Modules.DtiWeekly.Infrastructure.Persistence.Repositories;

public sealed class CriteriaEvidenceRepository(PlatformManagerDbContext db) : ICriteriaEvidenceRepository
{
    public async Task RemoveAllForAssessmentAsync(Guid criteriaAssessmentId, CancellationToken ct)
    {
        var existing = await db.Set<CriteriaEvidence>()
            .Where(e => e.CriteriaAssessmentId == criteriaAssessmentId)
            .ToListAsync(ct);

        if (existing.Count > 0)
            db.Set<CriteriaEvidence>().RemoveRange(existing);
    }

    public async Task AddRangeAsync(IEnumerable<CriteriaEvidence> evidence, CancellationToken ct)
        => await db.Set<CriteriaEvidence>().AddRangeAsync(evidence, ct);

    public Task<List<CriteriaEvidence>> GetForAssessmentAsync(Guid criteriaAssessmentId, CancellationToken ct)
        => db.Set<CriteriaEvidence>()
            .Where(e => e.CriteriaAssessmentId == criteriaAssessmentId)
            .OrderBy(e => e.OrderIndex)
            .ToListAsync(ct);
}

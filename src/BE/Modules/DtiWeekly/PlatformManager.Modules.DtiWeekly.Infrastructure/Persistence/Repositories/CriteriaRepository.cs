using PlatformManager.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using PlatformManager.Modules.DtiWeekly.Application.Criteria;

namespace PlatformManager.Modules.DtiWeekly.Infrastructure.Persistence.Repositories;

public sealed class CriteriaRepository(PlatformManagerDbContext db) : ICriteriaRepository
{
    public Task<Domain.Entities.Criteria?> GetByIdAsync(Guid id, CancellationToken ct)
        => db.Set<Domain.Entities.Criteria>().FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<bool> CodeExistsAsync(string code, Guid? excludeId, CancellationToken ct)
        => db.Set<Domain.Entities.Criteria>().AnyAsync(c => c.Code == code && (excludeId == null || c.Id != excludeId), ct);

    public Task<Domain.Entities.Criteria?> GetByCodeAsync(string code, CancellationToken ct)
        => db.Set<Domain.Entities.Criteria>().FirstOrDefaultAsync(c => c.Code == code, ct);

    public Task<List<Domain.Entities.Criteria>> GetAllActiveAsync(CancellationToken ct)
        => db.Set<Domain.Entities.Criteria>().OrderBy(c => c.Code).ToListAsync(ct);

    public async Task AddAsync(Domain.Entities.Criteria criteria, CancellationToken ct)
        => await db.Set<Domain.Entities.Criteria>().AddAsync(criteria, ct);

    public void Remove(Domain.Entities.Criteria criteria) => db.Set<Domain.Entities.Criteria>().Remove(criteria);
}

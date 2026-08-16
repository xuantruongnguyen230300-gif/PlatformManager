using PlatformManager.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using PlatformManager.Modules.DtiWeekly.Application.CriteriaGroups;
using PlatformManager.Modules.DtiWeekly.Domain.Entities;

namespace PlatformManager.Modules.DtiWeekly.Infrastructure.Persistence.Repositories;

public sealed class CriteriaGroupRepository(PlatformManagerDbContext db) : ICriteriaGroupRepository
{
    public Task<List<CriteriaGroup>> GetAllAsync(CancellationToken ct)
        => db.Set<CriteriaGroup>().OrderBy(g => g.DisplayOrder).ToListAsync(ct);

    public Task<CriteriaGroup?> GetByIdAsync(Guid id, CancellationToken ct)
        => db.Set<CriteriaGroup>().FirstOrDefaultAsync(g => g.Id == id, ct);

    public Task<CriteriaGroup?> GetByNameAsync(string name, CancellationToken ct)
        => db.Set<CriteriaGroup>().FirstOrDefaultAsync(g => g.Name.ToLower() == name.ToLower(), ct);

    public async Task AddAsync(CriteriaGroup group, CancellationToken ct)
        => await db.Set<CriteriaGroup>().AddAsync(group, ct);
}

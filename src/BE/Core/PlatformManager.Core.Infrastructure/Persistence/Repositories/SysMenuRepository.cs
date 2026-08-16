using Microsoft.EntityFrameworkCore;
using PlatformManager.Core.Application.Menu;
using PlatformManager.Core.Domain.Entities;

namespace PlatformManager.Core.Infrastructure.Persistence.Repositories;

public sealed class SysMenuRepository(PlatformManagerDbContext db) : ISysMenuRepository
{
    public Task<List<SysMenu>> GetAllAsync(CancellationToken ct)
        => db.SysMenus.OrderBy(m => m.DisplayOrder).ToListAsync(ct);
}

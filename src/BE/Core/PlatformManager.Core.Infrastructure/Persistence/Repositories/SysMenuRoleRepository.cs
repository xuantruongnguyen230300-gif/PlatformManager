using Microsoft.EntityFrameworkCore;
using PlatformManager.Core.Application.Menu;
using PlatformManager.Core.Domain.Entities;

namespace PlatformManager.Core.Infrastructure.Persistence.Repositories;

public sealed class SysMenuRoleRepository(PlatformManagerDbContext db) : ISysMenuRoleRepository
{
    public async Task<Dictionary<Guid, List<string>>> GetAssignedRoleNamesBySysMenuAsync(CancellationToken ct)
    {
        var links = await db.SysMenuRoles.ToListAsync(ct);
        if (links.Count == 0)
            return [];

        var roleIds = links.Select(l => l.RoleId).Distinct().ToList();
        var roleNameById = await db.Roles
            .Where(r => roleIds.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id, r => r.Name!, ct);

        return links
            .GroupBy(l => l.SysMenuId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(l => roleNameById.GetValueOrDefault(l.RoleId)).Where(n => n is not null).Select(n => n!).ToList());
    }

    public async Task<HashSet<Guid>> GetVisibleSysMenuIdsForRolesAsync(IReadOnlyCollection<string> roleNames, CancellationToken ct)
    {
        var allMenuIds = await db.SysMenus.Select(m => m.Id).ToListAsync(ct);
        var restrictedMenuIds = await db.SysMenuRoles.Select(l => l.SysMenuId).Distinct().ToListAsync(ct);

        // SysMenu KHÔNG có dòng nào trong SysMenuRole = mở cho mọi user đã đăng nhập.
        var visible = allMenuIds.Except(restrictedMenuIds).ToHashSet();

        if (roleNames.Count == 0)
            return visible;

        var roleIds = await db.Roles.Where(r => roleNames.Contains(r.Name!)).Select(r => r.Id).ToListAsync(ct);
        if (roleIds.Count == 0)
            return visible;

        var matchedMenuIds = await db.SysMenuRoles
            .Where(l => roleIds.Contains(l.RoleId))
            .Select(l => l.SysMenuId)
            .ToListAsync(ct);

        foreach (var id in matchedMenuIds)
            visible.Add(id);

        return visible;
    }

    public async Task ReplaceAllAsync(IReadOnlyDictionary<Guid, IReadOnlyCollection<string>> assignments, CancellationToken ct)
    {
        var existing = await db.SysMenuRoles.ToListAsync(ct);
        db.SysMenuRoles.RemoveRange(existing);

        var allRoleNames = assignments.Values.SelectMany(v => v).Distinct().ToList();
        var roleIdByName = allRoleNames.Count > 0
            ? await db.Roles.Where(r => allRoleNames.Contains(r.Name!)).ToDictionaryAsync(r => r.Name!, r => r.Id, ct)
            : [];

        foreach (var (sysMenuId, roleNames) in assignments)
        {
            foreach (var roleName in roleNames)
            {
                if (roleIdByName.TryGetValue(roleName, out var roleId))
                    db.SysMenuRoles.Add(SysMenuRole.Create(sysMenuId, roleId));
            }
        }
    }
}

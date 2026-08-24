using Microsoft.EntityFrameworkCore;
using PlatformManager.Core.Application.Permissions;
using PlatformManager.Core.Domain.Entities;

namespace PlatformManager.Core.Infrastructure.Persistence.Repositories;

public sealed class RolePermissionRepository(PlatformManagerDbContext db) : IRolePermissionRepository
{
    public async Task<Dictionary<string, List<string>>> GetAssignedRoleNamesByResourceKeyAsync(CancellationToken ct)
    {
        // Chỉ ĐỌC (ma trận phân quyền hành động) — chỉ dựng DTO, không sửa rồi SaveChanges.
        var links = await db.RolePermissions.AsNoTracking().ToListAsync(ct);
        if (links.Count == 0)
            return [];

        var roleIds = links.Select(l => l.RoleId).Distinct().ToList();
        var roleNameById = await db.Roles
            .AsNoTracking()
            .Where(r => roleIds.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id, r => r.Name!, ct);

        return links
            .GroupBy(l => l.ResourceKey)
            .ToDictionary(
                g => g.Key,
                g => g.Select(l => roleNameById.GetValueOrDefault(l.RoleId)).Where(n => n is not null).Select(n => n!).ToList());
    }

    public async Task ReplaceAllAsync(IReadOnlyDictionary<string, IReadOnlyCollection<string>> assignments, CancellationToken ct)
    {
        // CỐ Ý KHÔNG AsNoTracking: entity lấy ra để RemoveRange rồi SaveChanges ở handler —
        // bỏ tracking ở đây là lỗi im lặng (Q1, .claude/rules/performance.md).
        var existing = await db.RolePermissions.ToListAsync(ct);
        db.RolePermissions.RemoveRange(existing);

        var allRoleNames = assignments.Values.SelectMany(v => v).Distinct().ToList();
        // Role chỉ dùng để TRA Id theo tên (không sửa role nào) → AsNoTracking an toàn.
        var roleIdByName = allRoleNames.Count > 0
            ? await db.Roles.AsNoTracking().Where(r => allRoleNames.Contains(r.Name!)).ToDictionaryAsync(r => r.Name!, r => r.Id, ct)
            : [];

        foreach (var (resourceKey, roleNames) in assignments)
        {
            foreach (var roleName in roleNames)
            {
                if (roleIdByName.TryGetValue(roleName, out var roleId))
                    db.RolePermissions.Add(RolePermission.Create(roleId, resourceKey));
            }
        }
    }
}

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

    /// <summary>Token phiên bản của ma trận resource × role — cùng luật với
    /// <c>SysMenuRoleRepository.GetVersionAsync</c>, kể cả điều cấm dùng
    /// <c>IgnoreQueryFilters()</c>.</summary>
    public async Task<string> GetVersionAsync(CancellationToken ct)
    {
        var pairs = await db.RolePermissions
            .AsNoTracking()
            .Select(link => new { link.RoleId, link.ResourceKey })
            .ToListAsync(ct);

        return MatrixVersion.Compute(pairs.Select(p => $"{p.RoleId:N}:{p.ResourceKey}"));
    }

    public async Task ReplaceAllAsync(IReadOnlyDictionary<string, IReadOnlyCollection<string>> assignments, CancellationToken ct)
    {
        // CỐ Ý KHÔNG AsNoTracking: entity lấy ra để ĐÁNH DẤU XOÁ rồi SaveChanges ở handler —
        // bỏ tracking ở đây là lỗi im lặng (Q1, doc/huong_dan/quy-uoc/be-performance.md).
        //
        // XOÁ MỀM thay cho RemoveRange (2026-08-31) + dòng mới cho mọi cặp trong payload: lý do,
        // cái giá đã cân, và điều kiện về thứ tự lệnh của EF đều ghi ở SysMenuRoleRepository —
        // hai repository này là một quyết định, không phải hai.
        var existing = await db.RolePermissions.ToListAsync(ct);
        foreach (var link in existing)
            link.IsDeleted = true;

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

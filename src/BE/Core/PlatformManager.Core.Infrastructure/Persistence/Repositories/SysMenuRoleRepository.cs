using Microsoft.EntityFrameworkCore;
using PlatformManager.Core.Application.Menu;
using PlatformManager.Core.Application.Permissions;
using PlatformManager.Core.Domain.Entities;

namespace PlatformManager.Core.Infrastructure.Persistence.Repositories;

public sealed class SysMenuRoleRepository(PlatformManagerDbContext db) : ISysMenuRoleRepository
{
    public async Task<Dictionary<Guid, List<string>>> GetAssignedRoleNamesBySysMenuAsync(CancellationToken ct)
    {
        // Chỉ ĐỌC (ma trận menu × role) — chỉ dựng dictionary trả ra ngoài, không sửa rồi
        // SaveChanges. Cùng khuôn với RolePermissionRepository trong thư mục này.
        var links = await db.SysMenuRoles.AsNoTracking().ToListAsync(ct);
        if (links.Count == 0)
            return [];

        var roleIds = links.Select(l => l.RoleId).Distinct().ToList();
        var roleNameById = await db.Roles
            .AsNoTracking()
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
        // Cả 4 query của hàm này đều thuần đọc (chỉ dựng HashSet id để lọc menu hiển thị).
        var allMenuIds = await db.SysMenus.AsNoTracking().Select(m => m.Id).ToListAsync(ct);
        var restrictedMenuIds = await db.SysMenuRoles.AsNoTracking().Select(l => l.SysMenuId).Distinct().ToListAsync(ct);

        // SysMenu KHÔNG có dòng nào trong SysMenuRole = mở cho mọi user đã đăng nhập.
        var visible = allMenuIds.Except(restrictedMenuIds).ToHashSet();

        if (roleNames.Count == 0)
            return visible;

        var roleIds = await db.Roles.AsNoTracking().Where(r => roleNames.Contains(r.Name!)).Select(r => r.Id).ToListAsync(ct);
        if (roleIds.Count == 0)
            return visible;

        var matchedMenuIds = await db.SysMenuRoles
            .AsNoTracking()
            .Where(l => roleIds.Contains(l.RoleId))
            .Select(l => l.SysMenuId)
            .ToListAsync(ct);

        foreach (var id in matchedMenuIds)
            visible.Add(id);

        return visible;
    }

    /// <summary>
    /// Token phiên bản của ma trận menu × role. Đọc qua <c>db.SysMenuRoles</c> BÌNH THƯỜNG, tức
    /// global query filter đã loại dòng đã xoá mềm — <b>không được</b> thêm
    /// <c>IgnoreQueryFilters()</c> ở đây: trộn lịch sử vào token thì token đổi sau mỗi lần lưu kể
    /// cả khi ma trận không đổi, và mọi PUT hợp lệ thành 409 (bẫy ghi ở
    /// doc/huong_dan/wiki-core/be/06-concurrency-control.md).
    /// </summary>
    public async Task<string> GetVersionAsync(CancellationToken ct)
    {
        var pairs = await db.SysMenuRoles
            .AsNoTracking()
            .Select(link => new { link.SysMenuId, link.RoleId })
            .ToListAsync(ct);

        // Thứ tự do MatrixVersion áp đặt (Ordinal), không dựa vào ORDER BY của DB.
        return MatrixVersion.Compute(pairs.Select(p => $"{p.SysMenuId:N}:{p.RoleId:N}"));
    }

    public async Task ReplaceAllAsync(IReadOnlyDictionary<Guid, IReadOnlyCollection<string>> assignments, CancellationToken ct)
    {
        // CỐ Ý KHÔNG AsNoTracking: entity lấy ra để ĐÁNH DẤU XOÁ rồi SaveChanges ở handler — bỏ
        // tracking ở đây là lỗi im lặng (xem RolePermissionRepository.ReplaceAllAsync, cùng luật).
        //
        // XOÁ MỀM, KHÔNG RemoveRange (đổi 2026-08-31, quyết định người dùng — xem
        // doc/huong_dan/quy-uoc/be-entity-domain.md §"Quyết định người dùng 2026-08-31"). Xoá cứng
        // trả lời được "ai vừa ghi" nhưng KHÔNG trả lời được "trước đó ma trận là gì" — mà vế sau
        // mới là vế cho phép khôi phục, và hệ thống này không có nhật ký nào khác để bù.
        //
        // AuditInterceptor tự điền UpdatedBy/UpdatedAt cho các dòng bị đánh dấu (chúng ở trạng
        // thái Modified), nên không ghi tay ở đây.
        var existing = await db.SysMenuRoles.ToListAsync(ct);
        foreach (var link in existing)
            link.IsDeleted = true;

        var allRoleNames = assignments.Values.SelectMany(v => v).Distinct().ToList();
        // Role chỉ dùng để TRA Id theo tên (không sửa role nào) → AsNoTracking an toàn.
        var roleIdByName = allRoleNames.Count > 0
            ? await db.Roles.AsNoTracking().Where(r => allRoleNames.Contains(r.Name!)).ToDictionaryAsync(r => r.Name!, r => r.Id, ct)
            : [];

        // Dòng mới cho MỌI cặp trong payload, kể cả cặp vừa bị đánh dấu xoá ở trên — mỗi lần lưu
        // là một THẾ HỆ đầy đủ, đó là thứ làm việc so hai thế hệ trở nên tầm thường (group theo
        // UpdatedAt) và là cái giá đã cân trong quyết định 2026-08-31 ("bảng phình theo số lần
        // lưu", đã loại job dọn định kỳ).
        //
        // ⚠️ Việc này dựa vào một tính chất của EF: trong CÙNG một bảng, lệnh Modified được gửi
        // TRƯỚC lệnh Added, nên dòng cũ đã mang IsDeleted = true khi dòng mới cùng cặp được chèn —
        // nếu không, unique index lọc (IX_SysMenuRoles_SysMenuId_RoleId_Active) sẽ bắn 23505.
        // Tính chất này KHÔNG mới ở đây: bản trước dùng RemoveRange rồi chèn lại đúng cặp vừa xoá
        // trên PK ghép, tức đã dựa vào cùng thứ tự đó, và có integration test chạy trên Postgres
        // thật chốt (ResourcePermissionEndpointTests — "xoá sạch gán cũ rồi ghi lại đúng payload").
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

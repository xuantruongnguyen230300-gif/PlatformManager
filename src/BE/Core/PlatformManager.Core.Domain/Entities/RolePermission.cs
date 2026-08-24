using PlatformManager.Core.Domain.Common;

namespace PlatformManager.Core.Domain.Entities;

/// <summary>
/// Bảng nối nhiều-nhiều Role↔ResourceKey — PK ghép (RoleId, ResourceKey), THUẦN bảng nối (giống
/// SysMenuRole/AspNetUserRoles), KHÔNG kế thừa BaseEntity (không cần soft-delete, xoá dòng = thu
/// hồi quyền). Đây là ma trận "role được thao tác resource nào" (permission-key đầy đủ) — KHÁC
/// với SysMenuRole (chỉ điều khiển menu nhìn thấy được, không chặn gọi API trực tiếp). Quy ước:
/// role KHÔNG có dòng nào ở đây cho 1 ResourceKey = KHÔNG được thao tác resource đó
/// (deny-by-default — ngược với SysMenuRole, xem RequirePermissionFilter +
/// doc/contracts/permissions.md CONTRACT PERM-2 §"Rủi ro rollout"). ResourceKey là hằng số khai
/// ở ResourceKeys (Core.Application) — KHÔNG lưu tên hiển thị ở đây, tên tiếng Việt map tĩnh ở
/// Application, không lưu DB. RoleId trỏ AspNetRoles.Id (Identity, thuộc Infrastructure) — chỉ
/// Guid FK thuần, giống cách SysMenuRole.RoleId không cần biết kiểu AppRole cụ thể.
/// </summary>
public class RolePermission
{
    public Guid RoleId { get; private set; }
    public string ResourceKey { get; private set; } = string.Empty;

    private RolePermission() { }

    public static RolePermission Create(Guid roleId, string resourceKey)
    {
        if (roleId == Guid.Empty)
            throw new DomainException("ROLE_PERMISSION_ROLE_REQUIRED", "RoleId không được để trống.");
        if (string.IsNullOrWhiteSpace(resourceKey))
            throw new DomainException("ROLE_PERMISSION_RESOURCE_KEY_REQUIRED", "ResourceKey không được để trống.");

        return new RolePermission { RoleId = roleId, ResourceKey = resourceKey };
    }
}

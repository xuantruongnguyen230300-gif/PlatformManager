using PlatformManager.Core.Domain.Common;

namespace PlatformManager.Core.Domain.Entities;

/// <summary>
/// Bảng nối nhiều-nhiều SysMenu↔Role — PK ghép (SysMenuId, RoleId), THUẦN bảng nối (giống
/// AspNetUserRoles), KHÔNG kế thừa BaseEntity (không cần soft-delete, xoá dòng = thu hồi
/// quyền). Quy ước: SysMenu không có dòng nào ở đây = mở cho mọi user đã đăng nhập. RoleId
/// trỏ AspNetRoles.Id (Identity, thuộc Infrastructure) — chỉ Guid FK thuần, giống cách
/// CriteriaAssessment.OwnerId trỏ AppUser.Id mà không cần biết kiểu AppUser cụ thể.
/// </summary>
public class SysMenuRole
{
    public Guid SysMenuId { get; private set; }
    public Guid RoleId { get; private set; }

    private SysMenuRole() { }

    public static SysMenuRole Create(Guid sysMenuId, Guid roleId)
    {
        if (sysMenuId == Guid.Empty)
            throw new DomainException("SYS_MENU_ROLE_MENU_REQUIRED", "SysMenuId không được để trống.");
        if (roleId == Guid.Empty)
            throw new DomainException("SYS_MENU_ROLE_ROLE_REQUIRED", "RoleId không được để trống.");

        return new SysMenuRole { SysMenuId = sysMenuId, RoleId = roleId };
    }
}

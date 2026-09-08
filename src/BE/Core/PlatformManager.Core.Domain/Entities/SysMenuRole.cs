using PlatformManager.Core.Domain.Common;

namespace PlatformManager.Core.Domain.Entities;

/// <summary>
/// Bảng nối nhiều-nhiều SysMenu↔Role. Quy ước: SysMenu không có dòng nào ở đây = mở cho mọi
/// user đã đăng nhập. RoleId trỏ AspNetRoles.Id (Identity, thuộc Infrastructure) — chỉ Guid FK
/// thuần, giống cách SysMenu không cần biết kiểu AppRole cụ thể.
///
/// <para><b>KẾ THỪA BaseEntity từ 2026-08-31</b> (quyết định người dùng, xem
/// doc/huong_dan/quy-uoc/be-entity-domain.md §"Quyết định người dùng 2026-08-31"). Trước đó đây
/// là bảng nối THUẦN, không trường vết, và <c>ReplaceAllAsync</c> xoá cứng — nên không có cách
/// nào biết ai đã đổi phân quyền, lúc nào, và TRƯỚC ĐÓ ma trận là gì. Vế thứ ba mới là vế cho
/// phép khôi phục, và nó đòi xoá MỀM chứ không chỉ đòi thêm cột.</para>
///
/// <para><b>Hệ quả về khoá:</b> PK không còn là cặp ghép (SysMenuId, RoleId) — cặp ghép làm khoá
/// thì không thể tồn tại đồng thời một dòng đã xoá mềm và một dòng mới cùng cặp, tức lần lưu thứ
/// hai của cùng một ô sẽ trùng khoá. Nay PK là <see cref="BaseEntity.Id"/>, còn tính duy nhất của
/// cặp được giữ bằng UNIQUE INDEX LỌC theo <c>IsDeleted = false</c> (xem
/// SysMenuRoleConfiguration) — cùng khuôn với <c>IX_SysMenus_Code</c>.</para>
/// </summary>
public class SysMenuRole : BaseEntity
{
    public Guid SysMenuId { get; private set; }
    public Guid RoleId { get; private set; }

    private SysMenuRole() { }

    public static SysMenuRole Create(Guid sysMenuId, Guid roleId)
    {
        if (sysMenuId == Guid.Empty)
            throw new DomainException(SysMenuRoleErrors.MenuRequired);
        if (roleId == Guid.Empty)
            throw new DomainException(SysMenuRoleErrors.RoleRequired);

        return new SysMenuRole { SysMenuId = sysMenuId, RoleId = roleId };
    }
}

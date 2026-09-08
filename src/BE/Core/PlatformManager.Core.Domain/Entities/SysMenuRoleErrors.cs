using PlatformManager.Core.Domain.Common;

namespace PlatformManager.Core.Domain.Entities;

/// <summary>
/// Catalog lỗi của <see cref="SysMenuRole"/>.
///
/// <para>Tiền tố <c>SYS_MENU_ROLE.</c> cố ý giữ dấu chấm sát tên miền: FE phân nhánh theo tiền tố
/// <b>kèm dấu chấm</b>, nên <c>SYS_MENU.</c> không nuốt nhầm mã của bảng nối này.</para>
/// </summary>
public static class SysMenuRoleErrors
{
    public static readonly DomainError MenuRequired = new(
        "SYS_MENU_ROLE.MENU_REQUIRED", "SysMenuId không được để trống.");

    public static readonly DomainError RoleRequired = new(
        "SYS_MENU_ROLE.ROLE_REQUIRED", "RoleId không được để trống.");
}

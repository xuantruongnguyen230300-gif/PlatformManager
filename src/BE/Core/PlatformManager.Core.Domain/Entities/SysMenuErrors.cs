using PlatformManager.Core.Domain.Common;

namespace PlatformManager.Core.Domain.Entities;

/// <summary>
/// Catalog lỗi của <see cref="SysMenu"/> — khuôn <c>{Entity}Errors.cs</c> giống hệt
/// <c>UserErrors</c>/<c>AuthErrors</c> bên Application, chỉ khác kiểu bản ghi (<see cref="DomainError"/>
/// thay vì <c>ErrorDescriptor</c>) vì lý do tầng đã ghi ở <see cref="DomainError"/>.
/// </summary>
public static class SysMenuErrors
{
    public static readonly DomainError CodeRequired = new(
        "SYS_MENU.CODE_REQUIRED", "Mã menu không được để trống.");

    /// <summary>
    /// MỘT khai báo cho HAI chỗ ném (<see cref="SysMenu.Create"/> và <see cref="SysMenu.ReviveWith"/>).
    /// Trước 2026-09-03 mã <c>SYS_MENU_NAME_REQUIRED</c> được gõ tay ở cả hai chỗ; hai bản chuỗi
    /// giống nhau y hệt là trạng thái tạm — chỉ cần một lần sửa câu chữ hoặc đổi mã ở một chỗ là
    /// hai đường ném cùng một invariant trả về hai mã khác nhau, mà không phía nào báo lỗi.
    /// </summary>
    public static readonly DomainError NameRequired = new(
        "SYS_MENU.NAME_REQUIRED", "Tên menu không được để trống.");
}

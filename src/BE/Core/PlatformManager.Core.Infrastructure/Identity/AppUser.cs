using Microsoft.AspNetCore.Identity;

namespace PlatformManager.Core.Infrastructure.Identity;

/// <summary>
/// "SysUser" = AppUser/AspNetUsers, không phải khái niệm khác (xem
/// doc/ERD/ERD-corebase.md §1). KHÔNG kế thừa BaseEntity — Identity tự quản lý vòng đời
/// bằng field riêng (LockoutEnd/SecurityStamp...), soft-delete qua LockoutEnd thay vì
/// IsDelete (xem .claude/rules/entity-domain.md).
/// </summary>
public class AppUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;
    public DateTimeOffset? DateCreate { get; set; }
    public DateTimeOffset? DateUpdate { get; set; }

    /// <summary>Bắt buộc đổi mật khẩu ngay sau lần đăng nhập đầu — áp dụng chung cho tài
    /// khoản bootstrap (SuperAdmin) VÀ mọi user do Admin tạo qua màn Quản trị người dùng.</summary>
    public bool MustChangePassword { get; set; } = true;
}

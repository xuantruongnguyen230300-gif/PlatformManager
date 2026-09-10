using Microsoft.AspNetCore.Identity;

namespace PlatformManager.Core.Infrastructure.Identity;

/// <summary>
/// "SysUser" = AppUser/AspNetUsers, không phải khái niệm khác (xem
/// doc/cau-truc-database.md §4.1). KHÔNG kế thừa BaseEntity — Identity tự quản lý vòng đời
/// bằng field riêng (LockoutEnd/SecurityStamp...), soft-delete qua LockoutEnd thay vì
/// IsDelete (xem doc/huong_dan/quy-uoc/be-entity-domain.md).
/// </summary>
public class AppUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;
    public DateTimeOffset? DateCreate { get; set; }
    public DateTimeOffset? DateUpdate { get; set; }

    /// <summary>
    /// Ai TẠO / ai SỬA lần cuối — thêm 2026-08-31 (quyết định người dùng, xem
    /// doc/huong_dan/quy-uoc/be-entity-domain.md §"Quyết định người dùng 2026-08-31" quyết định 2).
    /// Trước đó chỉ có <see cref="DateCreate"/>/<see cref="DateUpdate"/>: biết KHI NÀO, không biết
    /// AI — trên chính bảng quyết định ai vào được hệ thống.
    ///
    /// <para><b>Điền TAY, không qua AuditInterceptor.</b> Interceptor chỉ chạm entity kế thừa
    /// <c>BaseEntity</c>, mà AppUser là <c>IdentityUser&lt;Guid&gt;</c> nên nằm ngoài tầm với của
    /// nó — cùng lý do <see cref="DateUpdate"/> phải ghi tay ở UserAdminService. Giá trị PHẢI là
    /// người THAO TÁC (<c>ICurrentUser.UserName</c>), không phải người bị sửa: đó là phép nghiệm
    /// thu số 4 của quyết định trên.</para>
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <inheritdoc cref="CreatedBy"/>
    public string? UpdatedBy { get; set; }

    /// <summary>Bắt buộc đổi mật khẩu ngay sau lần đăng nhập đầu — áp dụng chung cho tài
    /// khoản bootstrap (SuperAdmin) VÀ mọi user do Admin tạo qua màn Quản trị người dùng.</summary>
    public bool MustChangePassword { get; set; } = true;
}

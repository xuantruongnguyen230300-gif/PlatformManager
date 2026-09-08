namespace PlatformManager.Core.Application.Auth;

/// <summary>
/// Implement ở Infrastructure/Identity dùng SignInManager&lt;AppUser&gt;/UserManager&lt;AppUser&gt;
/// — cookie session (đã CHỐT, KHÔNG JWT). Application chỉ biết interface + DTO thuần, không
/// biết tới AppUser/kiểu Identity cụ thể (xem doc/huong_dan/quy-uoc/be-api-controller.md §Auth/Permission).
/// </summary>
public interface IIdentityService
{
    /// <param name="rememberMe">
    /// <c>false</c> (mặc định của <c>LoginCommand</c>) ⇒ cookie PHIÊN: hết hiệu lực khi đóng trình
    /// duyệt. <c>true</c> ⇒ cookie sống theo <c>ExpireTimeSpan</c> đã cấu hình. Trước 2026-08-31
    /// tham số này không tồn tại và <c>isPersistent: true</c> khai cứng, nên mọi lần đăng nhập đều
    /// để lại phiên sống nhiều ngày — trên máy dùng chung, người mở trình duyệt tiếp theo đang ở
    /// trong phiên của người trước. Xem doc/contracts/auth.md §POST /api/auth/login.
    /// </param>
    Task<LoginOutcome> SignInAsync(string userName, string password, bool rememberMe, CancellationToken ct);

    /// <summary>
    /// Nạp chồng giữ lại cho nơi gọi không quan tâm "ghi nhớ đăng nhập" (đường test tầng dịch vụ).
    /// Mặc định là bên AN TOÀN — cookie phiên — nên bỏ quên tham số không bao giờ nới lỏng bảo mật.
    /// </summary>
    Task<LoginOutcome> SignInAsync(string userName, string password, CancellationToken ct)
        => SignInAsync(userName, password, rememberMe: false, ct);

    Task SignOutAsync(CancellationToken ct);

    Task<CurrentUserInfo?> GetUserInfoAsync(Guid userId, CancellationToken ct);

    Task<ChangePasswordResult> ChangePasswordAsync(
        Guid userId, string currentPassword, string newPassword, CancellationToken ct);
}

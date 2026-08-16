namespace PlatformManager.Core.Application.Auth;

/// <summary>
/// Implement ở Infrastructure/Identity dùng SignInManager&lt;AppUser&gt;/UserManager&lt;AppUser&gt;
/// — cookie session (đã CHỐT, KHÔNG JWT). Application chỉ biết interface + DTO thuần, không
/// biết tới AppUser/kiểu Identity cụ thể (xem .claude/rules/api-controller.md §Auth/Permission).
/// </summary>
public interface IIdentityService
{
    Task<LoginOutcome> SignInAsync(string userName, string password, CancellationToken ct);

    Task SignOutAsync(CancellationToken ct);

    Task<CurrentUserInfo?> GetUserInfoAsync(Guid userId, CancellationToken ct);

    Task<ChangePasswordResult> ChangePasswordAsync(
        Guid userId, string currentPassword, string newPassword, CancellationToken ct);
}

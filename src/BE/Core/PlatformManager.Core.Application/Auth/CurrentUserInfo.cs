namespace PlatformManager.Core.Application.Auth;

public sealed record CurrentUserInfo(
    Guid Id,
    string UserName,
    string? Email,
    string FullName,
    IReadOnlyList<string> Roles,
    bool MustChangePassword);

/// <summary>Đặt tên "LoginOutcome" (không phải "SignInResult") để tránh đụng tên với
/// Microsoft.AspNetCore.Identity.SignInResult ở Infrastructure/Identity/IdentityService.cs.</summary>
public sealed record LoginOutcome(bool Succeeded, bool IsLockedOut, CurrentUserInfo? User);

public sealed record ChangePasswordResult(bool Succeeded, IReadOnlyList<string> Errors);

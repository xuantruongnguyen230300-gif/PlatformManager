using Microsoft.AspNetCore.Identity;
using PlatformManager.Core.Application.Auth;

namespace PlatformManager.Core.Infrastructure.Identity;

/// <inheritdoc cref="IIdentityService"/>
public sealed class IdentityService(SignInManager<AppUser> signInManager, UserManager<AppUser> userManager)
    : IIdentityService
{
    public async Task<LoginOutcome> SignInAsync(string userName, string password, CancellationToken ct)
    {
        var user = await userManager.FindByNameAsync(userName);
        if (user is null)
            return new LoginOutcome(false, false, null);

        var checkResult = await signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);
        if (checkResult.IsLockedOut)
            return new LoginOutcome(false, true, null);

        if (!checkResult.Succeeded)
            return new LoginOutcome(false, false, null);

        await signInManager.SignInAsync(user, isPersistent: true);

        return new LoginOutcome(true, false, await BuildUserInfoAsync(user));
    }

    public async Task SignOutAsync(CancellationToken ct) => await signInManager.SignOutAsync();

    public async Task<CurrentUserInfo?> GetUserInfoAsync(Guid userId, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        return user is null ? null : await BuildUserInfoAsync(user);
    }

    public async Task<ChangePasswordResult> ChangePasswordAsync(
        Guid userId, string currentPassword, string newPassword, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return new ChangePasswordResult(false, ["Không tìm thấy người dùng."]);

        var result = await userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        if (!result.Succeeded)
            return new ChangePasswordResult(false, [.. result.Errors.Select(e => e.Description)]);

        user.MustChangePassword = false;
        user.DateUpdate = DateTimeOffset.UtcNow;
        await userManager.UpdateAsync(user);

        return new ChangePasswordResult(true, []);
    }

    private async Task<CurrentUserInfo> BuildUserInfoAsync(AppUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        return new CurrentUserInfo(user.Id, user.UserName!, user.Email, user.FullName, [.. roles], user.MustChangePassword);
    }
}

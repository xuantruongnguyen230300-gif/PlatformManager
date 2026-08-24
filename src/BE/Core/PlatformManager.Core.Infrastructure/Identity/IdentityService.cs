using Microsoft.AspNetCore.Identity;
using PlatformManager.Core.Application.Auth;
using PlatformManager.Core.Application.Common;

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

        // SuperAdmin MIỄN khoá theo username (quyết định người dùng 2026-08-20) — xem
        // doc/huong_dan/wiki-core/be/09-security-beyond-auth.md §"Khoá tài khoản theo username".
        // lockoutOnFailure=true cho phép BẤT KỲ AI, kể cả chưa đăng nhập, khoá 15 phút MỘT tài
        // khoản bất kỳ bằng 5 lần đoán sai — với break-glass SuperAdmin đó là đường tự-DoS quản
        // trị viên cuối cùng ra khỏi hệ thống. SuperAdminAccountGuard KHÔNG chạm được đường này
        // (nằm TRƯỚC lúc đăng nhập, chưa có ai để mà guard). CHỈ ảnh hưởng việc ĐẾM lần sai
        // (AccessFailedCount) — không nới độ mạnh kiểm mật khẩu, không bỏ qua bước xác thực nào.
        var isSuperAdmin = await userManager.IsInRoleAsync(user, Roles.SuperAdmin);

        var checkResult = await signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: !isSuperAdmin);
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

        // UserManager.ChangePasswordAsync ở trên đã TỰ đổi SecurityStamp bên trong (hành vi có
        // sẵn của Identity — đúng bảo mật, giết mọi phiên cũ, nhưng giết CẢ phiên đang gọi).
        // BẮT BUỘC cấp lại cookie mang con dấu MỚI ở đây, gọi SAU CÙNG (sau khi MustChangePassword
        // đã ghi ổn định) — thiếu bước này, người vừa đổi mật khẩu bị chính SecurityStampValidator
        // đá ra ~30 phút sau, không rõ lý do. Các phiên KHÁC của cùng người vẫn mang con dấu cũ
        // ⇒ vẫn bị chấm dứt — ĐÚNG chuẩn bảo mật, không "sửa" nốt phần này. Xem
        // doc/huong_dan/wiki-core/be/02-identity-auth.md §"Cạm bẫy: ChangePasswordAsync TỰ đổi
        // con dấu".
        await signInManager.RefreshSignInAsync(user);

        return new ChangePasswordResult(true, []);
    }

    private async Task<CurrentUserInfo> BuildUserInfoAsync(AppUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        return new CurrentUserInfo(user.Id, user.UserName!, user.Email, user.FullName, [.. roles], user.MustChangePassword);
    }
}

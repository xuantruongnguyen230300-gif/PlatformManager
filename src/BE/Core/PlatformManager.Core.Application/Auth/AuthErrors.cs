using PlatformManager.Core.Application.Common.Results;

namespace PlatformManager.Core.Application.Auth;

public static class AuthErrors
{
    public static readonly ErrorDescriptor InvalidCredentials = new(
        "AUTH.INVALID_CREDENTIALS", ErrorCode.BusinessRuleError, "Tên đăng nhập hoặc mật khẩu không đúng.");

    public static readonly ErrorDescriptor LockedOut = new(
        "AUTH.LOCKED_OUT", ErrorCode.BusinessRuleError, "Tài khoản đã bị khoá — liên hệ quản trị viên.");

    public static readonly ErrorDescriptor NotAuthenticated = new(
        "AUTH.NOT_AUTHENTICATED", ErrorCode.AuthenticationError, "Chưa đăng nhập.");

    public static readonly ErrorDescriptor ChangePasswordFailed = new(
        "AUTH.CHANGE_PASSWORD_FAILED", ErrorCode.BusinessRuleError, "Đổi mật khẩu thất bại: {0}");
}

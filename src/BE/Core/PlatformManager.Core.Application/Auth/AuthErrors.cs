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

    /// <summary>
    /// Câu KHÔNG mang tham số — đổi 2026-09-05 theo quyết định người dùng
    /// (doc/huong_dan/wiki-core/be/16-i18n-va-ma-loi.md §11.2). Trước đó khuôn này là <c>"Đổi mật khẩu thất bại: {Reasons}"</c>
    /// và nơi gọi nhét vào đó <c>string.Join("; ", result.Errors)</c>, nên người dùng đọc được
    /// <i>"Đổi mật khẩu thất bại: PasswordTooShort; PasswordRequiresDigit"</i> — định danh tiếng Anh
    /// nối bằng dấu chấm phẩy, nằm giữa một câu tiếng Việt.
    ///
    /// <para>Từng mã nay đi ra <c>fieldErrors</c> kèm ĐÚNG ô nhập nó nói tới
    /// (<see cref="Common.Results.IdentityFieldErrors"/>). <b>Đừng thêm chỗ giữ lại:</b> một danh
    /// sách nối vào giữa câu không dịch được, vì trật tự từ và cách nối danh sách mỗi ngôn ngữ một
    /// khác.</para>
    /// </summary>
    public static readonly ErrorDescriptor ChangePasswordFailed = new(
        "AUTH.CHANGE_PASSWORD_FAILED", ErrorCode.BusinessRuleError, "Đổi mật khẩu thất bại.");
}

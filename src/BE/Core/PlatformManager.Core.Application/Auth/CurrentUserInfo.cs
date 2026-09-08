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

/// <summary>
/// Kết quả đổi mật khẩu.
/// </summary>
/// <param name="Succeeded">Đã đổi và đã commit.</param>
/// <param name="NotFound">
/// Không còn bản ghi người dùng — <b>KHÔNG phải lỗi Identity</b>, tách riêng 2026-09-05 (bẫy 3 của
/// doc/huong_dan/wiki-core/be/16-i18n-va-ma-loi.md §11.2). Trước đó nhánh này nhét một CÂU TIẾNG
/// VIỆT ("Không tìm thấy người dùng.") vào <paramref name="Errors"/>; khi danh sách đó trở thành
/// khoá tra bảng dịch thì một khoá dịch sẽ là một câu tiếng Việt — đúng loại rác mà quyết định
/// 2026-09-05 sinh ra để dọn. Cùng khuôn với <c>UpdateUserOutcome.NotFound</c>.
/// </param>
/// <param name="Errors">
/// Mã <c>IdentityError.Code</c> — <b>chỉ mã</b>, không bao giờ là câu chữ. Mỗi mã đi ra một phần tử
/// <c>fieldErrors</c> gắn đúng ô nhập nó nói tới.
/// </param>
public sealed record ChangePasswordResult(bool Succeeded, bool NotFound, IReadOnlyList<string> Errors);

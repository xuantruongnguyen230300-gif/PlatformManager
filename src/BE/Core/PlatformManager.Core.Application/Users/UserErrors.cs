using PlatformManager.Core.Application.Common.Results;

namespace PlatformManager.Core.Application.Users;

public static class UserErrors
{
    public static readonly ErrorDescriptor NotFound = new(
        "USER.NOT_FOUND", ErrorCode.NotFound, "Không tìm thấy người dùng.");

    public static readonly ErrorDescriptor DuplicateUserName = new(
        "USER.DUPLICATE_USERNAME", ErrorCode.Conflict, "Tên đăng nhập '{UserName}' đã tồn tại.");

    public static readonly ErrorDescriptor DuplicateEmail = new(
        "USER.DUPLICATE_EMAIL", ErrorCode.Conflict, "Email '{Email}' đã được sử dụng.");

    /// <summary>
    /// Câu KHÔNG mang tham số — đổi 2026-09-05 theo quyết định người dùng
    /// (doc/huong_dan/wiki-core/be/16-i18n-va-ma-loi.md §11.2). Từng mã Identity
    /// (<c>DuplicateUserName</c>, <c>PasswordTooShort</c>…) nay đi ra <c>fieldErrors</c> kèm đúng ô
    /// nhập nó nói tới, thay vì bị nối chuỗi vào giữa câu tiếng Việt — xem
    /// <see cref="Common.Results.IdentityFieldErrors"/> cho lý do đầy đủ.
    /// </summary>
    public static readonly ErrorDescriptor CreateFailed = new(
        "USER.CREATE_FAILED", ErrorCode.BusinessRuleError, "Tạo người dùng thất bại.");

    /// <summary>
    /// Mã RIÊNG cho đường cập nhật (thêm 2026-08-29, finding BE-8). Trước đó UpdateUserHandler
    /// mượn <see cref="CreateFailed"/> nên người dùng sửa một user đọc được câu "Tạo người dùng
    /// thất bại: cập nhật thất bại" — sai cả hành động lẫn mã lỗi, và FE không phân biệt được
    /// hai đường bằng <c>businessCode</c>.
    ///
    /// <para><b>Câu bỏ chỗ giữ <c>{Reasons}</c> 2026-09-05</b> (quyết định người dùng,
    /// doc/huong_dan/wiki-core/be/16-i18n-va-ma-loi.md §11.2 + §11.3). Chỗ giữ đó không có gì THẬT
    /// để điền: <c>IUserAdminService.UpdateAsync</c> khi ấy trả <c>bool</c> trần nên mã lỗi Identity
    /// đã bị vứt trước khi tới handler, và handler phải BỊA một chuỗi tại chỗ gọi
    /// (<c>"không lưu được thay đổi"</c>) — câu ghép ra <i>"Cập nhật người dùng thất bại: không lưu
    /// được thay đổi"</i>, nói hai lần cùng một điều. Nay <c>UpdateAsync</c> trả
    /// <see cref="UpdateUserOutcome"/> mang mã thật, và mã đi ra <c>fieldErrors</c>.</para>
    /// </summary>
    public static readonly ErrorDescriptor UpdateFailed = new(
        "USER.UPDATE_FAILED", ErrorCode.BusinessRuleError, "Cập nhật người dùng thất bại.");

    /// <summary>
    /// Người khác đã sửa chính người dùng này kể từ lúc client tải form về — <b>409</b>, và
    /// handler KHÔNG ghi gì. So bằng <c>ConcurrencyStamp</c> của Identity (quyết định người dùng
    /// 2026-08-30, doc/contracts/users.md quyết định 3).
    ///
    /// <para>Có MỘT câu hỏi còn để ngỏ trong chính card đó: <c>ConcurrencyFailure</c> nội bộ của
    /// Identity hiện quy về <see cref="UpdateFailed"/> (422) trong khi kiểm tường minh ở đây trả
    /// 409. Hai mã cho cùng một chuyện với người dùng cuối; gộp lại là thay đổi phá vỡ tương thích
    /// với FE đang chạy nên chưa gộp.</para>
    /// </summary>
    public static readonly ErrorDescriptor VersionConflict = new(
        "USER.VERSION_CONFLICT", ErrorCode.Conflict,
        "Người dùng này vừa được người khác cập nhật. Hãy tải lại danh sách rồi thực hiện lại — " +
        "thay đổi vừa rồi CHƯA được lưu.");

    // 2 mã dưới đây cho đường khoá/mở khoá (thêm 2026-08-29, finding BE-4). Trước đó hai handler
    // trả Ok(ok) — thất bại vẫn là HTTP 200 với data=false, mà FE map data thành undefined rồi
    // hiện toast "Đã khoá tài khoản.". Quản trị viên tin là đã khoá trong khi CHƯA khoá.
    //
    // Ca này đặc biệt đắt vì LockAsync đổi SecurityStamp TRƯỚC khi set lockout: hỏng ở bước sau
    // để lại trạng thái nửa vời — người dùng bị đá phiên (≤30 phút) nhưng đăng nhập lại được.
    // Nói thật rằng thao tác hỏng là cách duy nhất để quản trị viên biết mà thử lại.
    public static readonly ErrorDescriptor LockFailed = new(
        "USER.LOCK_FAILED", ErrorCode.BusinessRuleError,
        "Khoá tài khoản không thành công. Vui lòng thử lại; nếu vẫn lỗi, kiểm tra lại trạng thái tài khoản trước khi coi là đã khoá.");

    public static readonly ErrorDescriptor UnlockFailed = new(
        "USER.UNLOCK_FAILED", ErrorCode.BusinessRuleError,
        "Mở khoá tài khoản không thành công. Vui lòng thử lại.");

    // 4 lỗi dưới đây phục vụ SuperAdminAccountGuard — xem file đó để hiểu ngữ cảnh từng luật.
    //
    // Câu chữ 3 message (SelfSuperAdminRemoval/SuperAdminLock/SelfLock) chép ĐÚNG bảng ở
    // doc/contracts/users.md §"Bảo vệ tài khoản quản trị" (đồng bộ 2026-08-29, finding D-5):
    // card khai bản có LỐI RA ("hãy nhờ một SuperAdmin khác", "hãy đăng xuất") còn code trả bản
    // cụt hơn. Chọn sửa code chứ không sửa card vì card đã chốt "FE hiển thị thẳng message là
    // đủ, không map lại theo businessCode" — với hợp đồng đó, message thiếu lối ra là màn hình
    // chỉ nói "không được" mà không nói phải làm gì tiếp.
    public static readonly ErrorDescriptor SelfSuperAdminRemovalForbidden = new(
        "USER.SELF_SUPERADMIN_REMOVAL_FORBIDDEN", ErrorCode.AuthorizationError,
        "Bạn không thể tự gỡ vai trò SuperAdmin của chính mình. Hãy nhờ một SuperAdmin khác thực hiện.");

    public static readonly ErrorDescriptor SuperAdminRoleChangeForbidden = new(
        "USER.SUPERADMIN_ROLE_CHANGE_FORBIDDEN", ErrorCode.AuthorizationError,
        "Chỉ SuperAdmin mới có thể thêm hoặc gỡ quyền SuperAdmin của người dùng khác.");

    public static readonly ErrorDescriptor SelfLockForbidden = new(
        "USER.SELF_LOCK_FORBIDDEN", ErrorCode.AuthorizationError,
        "Bạn không thể tự khoá tài khoản của chính mình. Nếu muốn kết thúc phiên làm việc, hãy đăng xuất.");

    public static readonly ErrorDescriptor SuperAdminLockForbidden = new(
        "USER.SUPERADMIN_LOCK_FORBIDDEN", ErrorCode.AuthorizationError,
        "Chỉ SuperAdmin mới được khoá tài khoản có vai trò SuperAdmin.");
}

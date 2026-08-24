using PlatformManager.Core.Application.Common.Results;

namespace PlatformManager.Core.Application.Users;

public static class UserErrors
{
    public static readonly ErrorDescriptor NotFound = new(
        "USER.NOT_FOUND", ErrorCode.NotFound, "Không tìm thấy người dùng.");

    public static readonly ErrorDescriptor DuplicateUserName = new(
        "USER.DUPLICATE_USERNAME", ErrorCode.Conflict, "Tên đăng nhập '{0}' đã tồn tại.");

    public static readonly ErrorDescriptor DuplicateEmail = new(
        "USER.DUPLICATE_EMAIL", ErrorCode.Conflict, "Email '{0}' đã được sử dụng.");

    public static readonly ErrorDescriptor CreateFailed = new(
        "USER.CREATE_FAILED", ErrorCode.BusinessRuleError, "Tạo người dùng thất bại: {0}");

    // 4 lỗi dưới đây phục vụ SuperAdminAccountGuard — xem file đó để hiểu ngữ cảnh từng luật.
    public static readonly ErrorDescriptor SelfSuperAdminRemovalForbidden = new(
        "USER.SELF_SUPERADMIN_REMOVAL_FORBIDDEN", ErrorCode.AuthorizationError,
        "Không thể tự gỡ quyền SuperAdmin của chính mình.");

    public static readonly ErrorDescriptor SuperAdminRoleChangeForbidden = new(
        "USER.SUPERADMIN_ROLE_CHANGE_FORBIDDEN", ErrorCode.AuthorizationError,
        "Chỉ SuperAdmin mới có thể thêm hoặc gỡ quyền SuperAdmin của người dùng khác.");

    public static readonly ErrorDescriptor SelfLockForbidden = new(
        "USER.SELF_LOCK_FORBIDDEN", ErrorCode.AuthorizationError,
        "Không thể tự khoá tài khoản của chính mình.");

    public static readonly ErrorDescriptor SuperAdminLockForbidden = new(
        "USER.SUPERADMIN_LOCK_FORBIDDEN", ErrorCode.AuthorizationError,
        "Chỉ SuperAdmin mới có thể khoá tài khoản của một SuperAdmin khác.");
}

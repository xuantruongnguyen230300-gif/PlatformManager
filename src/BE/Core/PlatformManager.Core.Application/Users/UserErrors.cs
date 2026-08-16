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
}

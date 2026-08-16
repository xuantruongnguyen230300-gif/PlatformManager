namespace PlatformManager.Core.Domain.Common;

/// <summary>
/// Ném khi phát hiện xung đột trạng thái của chính resource (trùng giá trị unique,
/// đang bị ràng buộc không cho thao tác). Application layer dịch thành IApiResult lỗi
/// ErrorCode.Conflict (409) — xem .claude/rules/api-controller.md §Error → HTTP status mapping.
/// </summary>
public class ConflictException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

using System.Text.Json.Serialization;

namespace PlatformManager.Core.Application.Common.Results;

/// <summary>
/// Giá trị enum CHÍNH LÀ mã HTTP — không có bảng map thứ hai để lệch (xem
/// .claude/rules/api-controller.md §Envelope response). On-wire là TÊN member,
/// không phải số (JsonStringEnumConverter).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<ErrorCode>))]
public enum ErrorCode
{
    Success = 0,
    ValidationError = 400,
    AuthenticationError = 401,
    AuthorizationError = 403,
    NotFound = 404,
    Conflict = 409,
    BusinessRuleError = 422,
    TooManyRequests = 429,
    SystemError = 500,
}

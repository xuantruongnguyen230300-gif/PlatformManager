namespace PlatformManager.Core.Application.Common.Results;

/// <summary>
/// [ĐƠN GIẢN HOÁ] chỉ dùng System.Text.Json — bỏ hẳn ShouldSerialize*()/dual-serializer
/// của bản gốc VNR (PlatformManager chỉ có 1 serializer). Xem
/// .claude/rules/api-controller.md §Envelope response.
/// </summary>
public class ApiResult<T> : IApiResult<T>
{
    public T? Data { get; init; }
    public string? Message { get; init; }
    public ApiResultStatus Status { get; init; }
    public ErrorCode Code { get; init; }
    public string? BusinessCode { get; init; }
    public string? TraceId { get; set; }
    public bool? Retryable { get; init; }
    public Dictionary<string, string[]>? Fields { get; init; }

    public static ApiResult<T> Success(T? data, string? message = null)
        => new() { Status = ApiResultStatus.SUCCESS, Code = ErrorCode.Success, Data = data, Message = message };

    public static ApiResult<T> BusinessError(ErrorDescriptor error, string message) => new()
    {
        Status = ApiErrorIds.StatusForCode(error.ErrorCode),
        Code = error.ErrorCode,
        BusinessCode = error.BusinessCode,
        Message = message,
        Retryable = error.Retryable,
    };

    public static ApiResult<T> ValidationError(string message, Dictionary<string, string[]> fields) => new()
    {
        Status = ApiResultStatus.VALIDATION_ERROR,
        Code = ErrorCode.ValidationError,
        Message = message,
        Fields = fields,
    };

    public static ApiResult<T> SystemError(string message) => new()
    {
        Status = ApiResultStatus.SYSTEM_ERROR,
        Code = ErrorCode.SystemError,
        Message = message,
    };
}

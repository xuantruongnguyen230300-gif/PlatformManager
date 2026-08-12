namespace PlatformManager.Api.Common;

/// <summary>
/// Envelope response nhất quán cho MỌI endpoint (kể cả list/grid) — convention
/// FE đang mong đợi. Không trả PagedResult/DTO trần ở bất kỳ đâu.
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public string? TraceId { get; init; }

    public static ApiResponse<T> Ok(T data, string? traceId = null) =>
        new() { Success = true, Data = data, TraceId = traceId };

    public static ApiResponse<T> Fail(string errorCode, string errorMessage, string? traceId = null) =>
        new() { Success = false, ErrorCode = errorCode, ErrorMessage = errorMessage, TraceId = traceId };
}

public static class ApiResponse
{
    public static ApiResponse<T> Ok<T>(T data, string? traceId = null) => ApiResponse<T>.Ok(data, traceId);

    public static ApiResponse<object> Fail(string errorCode, string errorMessage, string? traceId = null) =>
        ApiResponse<object>.Fail(errorCode, errorMessage, traceId);
}

/// <summary>
/// Exception nghiệp vụ có ErrorCode rõ ràng, map sang HTTP status tương ứng
/// qua <see cref="Middleware.ExceptionMiddleware"/> — dùng cho lỗi MONG ĐỢI
/// (validation, not found, conflict), khác lỗi hạ tầng/bug không mong đợi.
/// </summary>
public class ApiException(int statusCode, string errorCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
    public string ErrorCode { get; } = errorCode;

    public static ApiException NotFound(string errorCode, string message) => new(404, errorCode, message);
    public static ApiException Conflict(string errorCode, string message) => new(409, errorCode, message);
    public static ApiException BadRequest(string errorCode, string message) => new(400, errorCode, message);
}

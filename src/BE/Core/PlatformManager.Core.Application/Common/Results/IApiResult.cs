namespace PlatformManager.Core.Application.Common.Results;

/// <summary>Cho phép đọc Status/Code mà không cần biết T.</summary>
public interface IHasApiResultStatus
{
    ApiResultStatus Status { get; }
    ErrorCode Code { get; }
}

/// <summary>
/// Envelope response nhất quán cho MỌI endpoint (kể cả list/grid) — xem
/// .claude/rules/api-controller.md §Envelope response.
/// </summary>
public interface IApiResult<T> : IHasApiResultStatus
{
    T? Data { get; }
    string? Message { get; }
    string? BusinessCode { get; }
    string? TraceId { get; set; }
    bool? Retryable { get; }
    Dictionary<string, string[]>? Fields { get; }
}

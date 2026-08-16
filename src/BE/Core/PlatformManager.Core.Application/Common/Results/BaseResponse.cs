namespace PlatformManager.Core.Application.Common.Results;

/// <summary>
/// API mà handler thật sự dùng — chỉ 2 động từ Ok/Fail, không tự dựng ApiResult&lt;T&gt;
/// bằng tay ở từng handler. Xem .claude/rules/cqrs-handler.md §BaseResponse.
/// </summary>
public abstract class BaseResponse
{
    protected static IApiResult<T> Ok<T>(T data, string? message = null)
        => ApiResult<T>.Success(data, message);

    protected static IApiResult<T> Fail<T>(ErrorDescriptor error, params object[] args)
        => ApiResult<T>.BusinessError(error, string.Format(error.MessageTemplate, args));
}

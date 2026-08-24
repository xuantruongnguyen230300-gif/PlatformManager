namespace PlatformManager.Core.Application.Common.Results;

/// <summary>
/// Lỗi 429 dùng bởi <c>RateLimiterOptions.OnRejected</c> (<c>PlatformManager.Api/Program.cs</c>) —
/// đặt cạnh <see cref="ErrorCode"/>/<see cref="ApiResult{T}"/> vì đây là lỗi hạ tầng dùng chung cho
/// cả policy "login" lẫn <c>GlobalLimiter</c>, không thuộc riêng module/feature nào. Xem
/// doc/huong_dan/quy-uoc/be-api-controller.md §"Rate limiting".
/// </summary>
public static class RateLimitErrors
{
    public static readonly ErrorDescriptor TooManyRequests = new(
        "RATE_LIMIT.TOO_MANY_REQUESTS",
        ErrorCode.TooManyRequests,
        "Bạn đã gửi quá nhiều yêu cầu — vui lòng thử lại sau.",
        Retryable: true);
}

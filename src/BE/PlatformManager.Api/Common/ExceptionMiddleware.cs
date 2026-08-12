using System.Net;
using System.Text.Json;

namespace PlatformManager.Api.Common;

/// <summary>
/// Middleware toàn cục bắt exception. <see cref="ApiException"/> (lỗi nghiệp
/// vụ mong đợi: validation/not found/conflict) map đúng status + ErrorCode
/// tường minh. Exception khác (không mong đợi — bug/lỗi hạ tầng) -> 500 +
/// ErrorCode="INTERNAL_ERROR", KHÔNG lộ chi tiết/stack trace ra response.
/// </summary>
public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ApiException apiEx)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = apiEx.StatusCode;
            var body = ApiResponse.Fail(apiEx.ErrorCode, apiEx.Message, context.TraceIdentifier);
            await context.Response.WriteAsync(JsonSerializer.Serialize(body));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception. TraceId={TraceId}", context.TraceIdentifier);
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            var body = ApiResponse.Fail("INTERNAL_ERROR", "Đã có lỗi xảy ra.", context.TraceIdentifier);
            await context.Response.WriteAsync(JsonSerializer.Serialize(body));
        }
    }
}

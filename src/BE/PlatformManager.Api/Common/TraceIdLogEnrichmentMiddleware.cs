using Serilog.Context;

namespace PlatformManager.Api.Common;

/// <summary>
/// Gắn <see cref="HttpContext.TraceIdentifier"/> vào MỌI log entry sinh ra trong lúc xử lý request.
///
/// <para><b>Vì sao bắt buộc, không phải trang trí:</b> mọi response lỗi trả <c>traceId</c> về client
/// (<c>IApiResult.TraceId</c>) và FE hiện mã đó cho người dùng đọc cho bộ phận hỗ trợ. Thiếu bước
/// enrich này thì <b>có log cũng không nối được với mã người dùng cầm</b> — chuỗi hỗ trợ đứt đúng
/// chỗ nó bắt đầu. Xem doc/huong_dan/wiki-core/be/07-observability.md §"Serilog ghi file, giữ 7
/// ngày" quyết định 3.</para>
///
/// <para><b>Đặt ở đâu:</b> TRƯỚC mọi middleware khác. Log sinh ra bởi middleware đứng trước nó
/// (kể cả <c>UseExceptionHandler</c>) sẽ không mang <c>TraceId</c>.</para>
///
/// <para>Tên thuộc tính <c>TraceId</c> phải khớp <c>outputTemplate</c> khai lúc cấu hình Serilog
/// trong <c>Program.cs</c> — lệch tên thì template in ra rỗng mà không báo lỗi gì.</para>
/// </summary>
internal sealed class TraceIdLogEnrichmentMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        using (LogContext.PushProperty("TraceId", context.TraceIdentifier))
        {
            await next(context);
        }
    }
}

/// <summary>Đăng ký <see cref="TraceIdLogEnrichmentMiddleware"/> vào pipeline.</summary>
internal static class TraceIdLogEnrichmentMiddlewareExtensions
{
    public static IApplicationBuilder UseTraceIdLogEnrichment(this IApplicationBuilder app)
        => app.UseMiddleware<TraceIdLogEnrichmentMiddleware>();
}

using PlatformManager.Core.Application.Common.Results;

namespace PlatformManager.Api.Common;

/// <summary>
/// Bọc envelope <see cref="ApiResult{T}"/> cho hai mã mà <b>hạ tầng định tuyến</b> tự sinh với thân
/// response RỖNG: <b>404</b> (không endpoint nào khớp đường dẫn) và <b>405</b> (khớp đường dẫn,
/// sai HTTP verb).
///
/// <para><b>Lỗ đã đo (2026-09-04):</b> <c>curl -sk -D - https://localhost:7168/api/khong-he-co</c>
/// trả <c>404</c> với <c>Content-Length: 0</c>; gọi đúng route nhưng sai verb trả <c>405</c> cũng
/// rỗng. Trong khi doc/contracts/auth.md §Envelope chung khẳng định *"Mọi response đi qua
/// <c>IApiResult&lt;T&gt;</c>"*. Hai mã này rơi ngoài mọi bộ dựng envelope đã có, vì chúng không
/// đến từ handler (nên MediatR không thấy), không đến từ exception (nên
/// <see cref="GlobalExceptionHandler"/> không thấy), và không đến từ model binding (nên
/// <see cref="ModelBindingProblemFactory"/> không thấy). Chúng do
/// <c>EndpointMiddleware</c>/terminal middleware ghi thẳng vào response ở CUỐI pipeline.</para>
///
/// <para>Hệ quả ở FE: <c>http-error.interceptor.ts</c> đọc <c>body.message</c> → <c>undefined</c>,
/// nên một URL gõ sai (hoặc một service FE gọi nhầm verb) hiện ra như lỗi hệ thống chung chung
/// thay vì một tín hiệu tra được. Không có <c>businessCode</c> thì cũng không có KHOÁ DỊCH
/// (doc/huong_dan/wiki-core/be/16-i18n-va-ma-loi.md §4(a)).</para>
///
/// <para><b>Vì sao là middleware "đo status sau khi <c>next</c> chạy xong" chứ không phải
/// <c>MapFallback</c>:</b> <c>MapFallback</c> đăng ký một route bắt-tất-cả với mọi HTTP method, nên
/// nó trở thành ứng viên hợp lệ cho request sai verb và <b>nuốt mất chính mã 405</b> — sai verb sẽ
/// biến thành 404. Phân biệt 404/405 là thông tin chẩn đoán thật (client gọi sai địa chỉ vs gọi sai
/// động từ), không được đánh mất để đổi lấy một dòng cấu hình ngắn hơn.</para>
///
/// <para><b>Vì sao không dùng <c>UseStatusCodePages</c>:</b> nó áp cho MỌI mã 400–599 có thân rỗng.
/// Ở đây chỉ có đúng hai mã cần bọc — mọi nhánh lỗi còn lại (401/403/429/400/500) đã có bộ dựng
/// envelope riêng. Một bộ bắt-tất-cả sẽ âm thầm trở thành lưới hứng cho nhánh thứ ba mọc lên sau
/// này mà không ai phải khai mã cho nó, tức đúng thói quen mà
/// <c>EnvelopeBusinessCodeTests</c> sinh ra để chặn.</para>
/// </summary>
internal sealed class ApiStatusCodeEnvelopeMiddleware(RequestDelegate next)
{
    /// <summary>
    /// CHỈ bọc bề mặt API. Đây là <b>allowlist</b> có chủ đích, không phải blocklist các nhánh cần
    /// tránh — vì blocklist đòi người thêm nhánh mới sau này phải nhớ cập nhật nó, còn allowlist thì
    /// mặc định bỏ qua thứ chưa khai.
    ///
    /// <para>Ba nhánh phải KHÔNG bị chạm tới, và cả ba nằm ngoài <c>/api</c> nên được loại trừ tự
    /// động: <c>/hangfire</c> (Dashboard tự phục vụ HTML + tài nguyên tĩnh của nó — trả JSON cho
    /// một thẻ <c>&lt;script&gt;</c> 404 là biến một tài nguyên thiếu thành một trang hỏng khó
    /// đoán), <c>/swagger</c> (Development), và <c>/health*</c>. Nếu sau này API tự phục vụ file
    /// tĩnh hay host SPA, chúng cũng nằm ngoài <c>/api</c> và vẫn không bị chạm.</para>
    /// </summary>
    private const string ApiPathPrefix = "/api";

    public async Task InvokeAsync(HttpContext context)
    {
        await next(context);

        var response = context.Response;

        // HasStarted: byte đầu tiên đã lên dây, không còn sửa được gì.
        //
        // ContentLength/ContentType: dấu hiệu "đã có nhánh khác dựng thân response". Đây chính là
        // thứ bảo vệ 404 mà HANDLER CHỦ ĐỘNG trả (vd USER.NOT_FOUND) — nó đi qua MVC nên đã có
        // Content-Type application/json và một envelope đầy đủ; ghi đè nó sẽ thay một mã nghiệp vụ
        // đúng bằng một mã định tuyến sai. Cùng phép thử mà StatusCodePagesMiddleware của
        // ASP.NET Core dùng, và cùng lý do.
        if (response.HasStarted
            || response.ContentLength.HasValue
            || !string.IsNullOrEmpty(response.ContentType))
        {
            return;
        }

        if (!context.Request.Path.StartsWithSegments(ApiPathPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var error = response.StatusCode switch
        {
            StatusCodes.Status404NotFound => InfrastructureErrors.RouteNotFound,
            StatusCodes.Status405MethodNotAllowed => InfrastructureErrors.MethodNotAllowed,
            _ => null,
        };

        if (error is null)
        {
            return;
        }

        // Dùng factory chứ không dựng bằng object initializer: mã và câu đi cùng nhau từ catalog,
        // và Status đi qua ApiErrorIds.StatusForCode (nguồn DUY NHẤT map ErrorCode → ApiResultStatus).
        var result = ApiResult<object>.BusinessError(error, error.MessageTemplate);
        result.TraceId = context.TraceIdentifier;

        response.ContentType = "application/json";
        await response.WriteAsJsonAsync(result, context.RequestAborted);
    }
}

/// <summary>Đăng ký <see cref="ApiStatusCodeEnvelopeMiddleware"/> vào pipeline.</summary>
internal static class ApiStatusCodeEnvelopeMiddlewareExtensions
{
    public static IApplicationBuilder UseApiStatusCodeEnvelope(this IApplicationBuilder app)
        => app.UseMiddleware<ApiStatusCodeEnvelopeMiddleware>();
}

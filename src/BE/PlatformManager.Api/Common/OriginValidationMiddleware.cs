using Microsoft.Extensions.Options;
using PlatformManager.Core.Application.Common.Results;

namespace PlatformManager.Api.Common;

/// <summary>
/// CSRF <b>Lớp 1</b> — mọi request ghi (<c>POST</c>/<c>PUT</c>/<c>PATCH</c>/<c>DELETE</c>) phải mang
/// header <c>Origin</c> nằm trong allowlist, không thì <b>403</b>.
///
/// <para><b>Vì sao không phải <c>SameSite</c>:</b> FE nằm ở origin khác (mô hình triển khai B, chốt
/// 2026-08-30 — xem doc/huong_dan/wiki-core/fe/17-phuc-vu-va-trien-khai.md §2), nên cookie phiên
/// buộc phải khai <c>SameSite=None</c>. Lớp bảo vệ <c>SameSite</c> vì thế <b>không tồn tại</b> ở
/// dự án này — đây là thứ thay vào đúng chỗ đó (chốt 2026-08-31, xem
/// doc/huong_dan/wiki-core/be/02-identity-auth.md §"kiểm header Origin thay cho Lớp 1").</para>
///
/// <para><b>Vì sao nó là một lớp thật, không trùng với token antiforgery:</b> trình duyệt LUÔN gắn
/// <c>Origin</c> cho request cross-site và JavaScript KHÔNG đặt hay sửa được header này (nằm trong
/// danh sách header cấm ghi của Fetch). Nó độc lập hoàn toàn với cơ chế token — hỏng cái này không
/// kéo theo cái kia.</para>
/// </summary>
internal sealed class OriginValidationMiddleware(
    RequestDelegate next,
    IOptions<CorsPolicyOptions> corsOptions,
    ILogger<OriginValidationMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var request = context.Request;

        var isWriteRequest = HttpMethods.IsPost(request.Method)
                             || HttpMethods.IsPut(request.Method)
                             || HttpMethods.IsPatch(request.Method)
                             || HttpMethods.IsDelete(request.Method);

        if (!isWriteRequest || IsOriginAcceptable(context))
        {
            await next(context);
            return;
        }

        logger.LogWarning(
            "Từ chối request ghi {Method} {Path}: header Origin '{Origin}' không nằm trong Cors:AllowedOrigins.",
            request.Method,
            request.Path,
            request.Headers.Origin.ToString());

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/json";

        var result = ApiResult<object>.BusinessError(
            InfrastructureErrors.OriginRejected,
            InfrastructureErrors.OriginRejected.MessageTemplate);
        result.TraceId = context.TraceIdentifier;

        await context.Response.WriteAsJsonAsync(result, context.RequestAborted);
    }

    private bool IsOriginAcceptable(HttpContext context)
    {
        var origin = context.Request.Headers.Origin.ToString();

        // ── QUYẾT ĐỊNH THI CÔNG 2026-08-31: request KHÔNG có Origin thì CHO QUA ────────────────
        // Đây là ca mà 02-identity-auth.md §"kiểm header Origin" để ngỏ và yêu cầu chọn có chủ
        // đích. Chọn CHO QUA, ba lý do:
        //   1. Kẻ tấn công KHÔNG dùng được đường này. Kịch bản CSRF luôn xuất phát từ một trang web
        //      trong trình duyệt, mà trình duyệt không cho phép bỏ Origin cho request cross-site —
        //      nó là cam kết của trình duyệt, không phải quy ước tự nguyện. "Không có Origin" nghĩa
        //      là "không phải trình duyệt", tức nằm ngoài mô hình tấn công mà lớp này chặn.
        //   2. Chặn hết sẽ khoá mọi client không phải trình duyệt: script vận hành, công cụ tích
        //      hợp, kiểm thử tự động, lệnh curl khi chẩn đoán sự cố — trong khi chúng KHÔNG bớt an
        //      toàn đi chút nào (xem lý do 1).
        //   3. Lớp 2 (token antiforgery) VẪN áp cho đúng những request này, không có ngoại lệ. Đây
        //      là điều làm lựa chọn này khác với "bỏ trống": request thiếu Origin không hề đi vào
        //      hệ thống mà không qua kiểm tra nào — nó vẫn phải có X-XSRF-TOKEN hợp lệ.
        // Đổi hướng khi nào: nếu sau này có yêu cầu "chỉ trình duyệt được ghi dữ liệu", đổi nhánh
        // này thành 403 và cấp cho script một đường xác thực riêng (API key/service account) —
        // KHÔNG nới lỏng Lớp 2 để bù.
        if (string.IsNullOrEmpty(origin))
        {
            return true;
        }

        foreach (var allowed in corsOptions.Value.AllowedOrigins)
        {
            // So khớp chuỗi chính xác (bỏ qua hoa/thường) — ĐÚNG cách WithOrigins của CORS so khớp.
            // Không cắt bớt, không so khớp theo tiền tố: "https://app.example.com.evil.net" bắt đầu
            // bằng origin hợp lệ nhưng là một site hoàn toàn khác.
            if (string.Equals(origin, allowed, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        // Cùng origin với chính API (Swagger UI ở Development, Hangfire Dashboard, mọi công cụ mở
        // thẳng trên host của API). Trình duyệt vẫn gắn Origin cho POST cùng origin, mà origin của
        // chính API thì không nằm trong allowlist CORS (allowlist đó dành cho FE). Không có nhánh
        // này thì bấm "Try it out" trong Swagger sẽ nhận 403 khó hiểu. An toàn: kẻ tấn công không
        // đặt được Origin bằng origin của chính API từ một site khác.
        var selfOrigin = $"{context.Request.Scheme}://{context.Request.Host.Value}";
        return string.Equals(origin, selfOrigin, StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>Đăng ký <see cref="OriginValidationMiddleware"/> vào pipeline.</summary>
internal static class OriginValidationMiddlewareExtensions
{
    public static IApplicationBuilder UseOriginValidation(this IApplicationBuilder app)
        => app.UseMiddleware<OriginValidationMiddleware>();
}

using FluentValidation;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Diagnostics;
using PlatformManager.Core.Application.Common.Results;

namespace PlatformManager.Api.Common;

/// <summary>
/// Bắt 3 loại lỗi không đi qua HandleResult: FluentValidation.ValidationException (validator
/// fail trước handler), AntiforgeryValidationException (thiếu/sai token CSRF — xem
/// doc/huong_dan/wiki-core/be/02-identity-auth.md §CSRF) và exception không mong đợi (bug/hạ
/// tầng) — dịch cả ba thành đúng IApiResult envelope, KHÔNG lộ stack trace ra response. Xem
/// .claude/rules/api-controller.md §Exception-handling middleware toàn cục.
/// </summary>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var traceId = httpContext.TraceIdentifier;

        ApiResult<object> result = exception switch
        {
            ValidationException vex => new ApiResult<object>
            {
                Status = ApiResultStatus.VALIDATION_ERROR,
                Code = ErrorCode.ValidationError,
                Message = "Dữ liệu không hợp lệ.",
                TraceId = traceId,
                Fields = vex.Errors
                    .GroupBy(e => NormalizeField(e.PropertyName))
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()),
            },
            // Thiếu/sai header X-XSRF-TOKEN (hoặc cookie XSRF-TOKEN) trên request ghi — 403, KHÔNG
            // phải 500: đây là request bị TỪ CHỐI có chủ đích, không phải lỗi hệ thống.
            AntiforgeryValidationException => new ApiResult<object>
            {
                Status = ApiResultStatus.BUSINESS_ERROR,
                Code = ErrorCode.AuthorizationError,
                Message = "Yêu cầu bị từ chối — thiếu hoặc sai token chống giả mạo (CSRF).",
                TraceId = traceId,
            },
            _ => new ApiResult<object>
            {
                Status = ApiResultStatus.SYSTEM_ERROR,
                Code = ErrorCode.SystemError,
                Message = "Đã có lỗi xảy ra.",
                TraceId = traceId,
            },
        };

        if (result.Code == ErrorCode.SystemError)
            logger.LogError(exception, "Lỗi không mong đợi — TraceId={TraceId}", traceId);
        else
            logger.LogWarning(exception, "Validation lỗi — TraceId={TraceId}", traceId);

        httpContext.Response.ContentType = "application/json";
        httpContext.Response.StatusCode = (int)result.Code;
        await httpContext.Response.WriteAsJsonAsync(result, cancellationToken);

        return true;
    }

    // Envelope (Data/Message/Status...) serialize camelCase qua PropertyNamingPolicy toàn cục
    // (xem Program.cs), NHƯNG Fields (Dictionary<string,string[]>) CỐ Ý giữ nguyên PascalCase
    // — DictionaryKeyPolicy mặc định là null (không set) nên key KHÔNG bị camelCase hoá, khớp
    // đúng tên property C# gốc (vd "Code", "MaxScore") mà FE đang mong đợi cho việc bind lỗi
    // vào field trên form (xem wiki-core/fe/02-http-envelope.md). Chỉ bỏ tiền tố "Request."
    // nếu FE gửi wrapper DTO, không đổi casing.
    private static string NormalizeField(string propertyName)
        => propertyName.StartsWith("Request.", StringComparison.Ordinal) ? propertyName["Request.".Length..] : propertyName;
}

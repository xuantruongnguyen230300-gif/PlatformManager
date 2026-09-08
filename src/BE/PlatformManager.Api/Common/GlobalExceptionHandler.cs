using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Diagnostics;
using PlatformManager.Core.Application.Common.Results;

namespace PlatformManager.Api.Common;

/// <summary>
/// Bắt 3 loại lỗi không đi qua HandleResult: FluentValidation.ValidationException (validator
/// fail trước handler), AntiforgeryValidationException (thiếu/sai token CSRF — xem
/// doc/huong_dan/wiki-core/be/02-identity-auth.md §CSRF) và exception không mong đợi (bug/hạ
/// tầng) — dịch cả ba thành đúng IApiResult envelope, KHÔNG lộ stack trace ra response. Xem
/// doc/huong_dan/quy-uoc/be-api-controller.md §Exception-handling middleware toàn cục.
/// </summary>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    /// <summary>
    /// Gắn <c>TraceId</c> sau khi factory đã dựng. Factory KHÔNG nhận traceId vì nó nằm ở tầng
    /// Application, không biết gì về <c>HttpContext</c> — nơi duy nhất biết là đây.
    /// </summary>
    private static ApiResult<object> WithTrace(ApiResult<object> result, string traceId)
    {
        result.TraceId = traceId;
        return result;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var traceId = httpContext.TraceIdentifier;

        ApiResult<object> result = exception switch
        {
            ValidationException vex => BuildValidationResult(vex, traceId),
            // Thiếu/sai header X-XSRF-TOKEN (hoặc cookie XSRF-TOKEN) trên request ghi — 403, KHÔNG
            // phải 500: đây là request bị TỪ CHỐI có chủ đích, không phải lỗi hệ thống.
            AntiforgeryValidationException => WithTrace(
                ApiResult<object>.BusinessError(
                    InfrastructureErrors.CsrfRejected,
                    InfrastructureErrors.CsrfRejected.MessageTemplate),
                traceId),
            _ => WithTrace(ApiResult<object>.SystemError(InfrastructureErrors.Unexpected), traceId),
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

    /// <summary>
    /// Nhánh 400 — <b>chỗ DUY NHẤT</b> lỗi validate biến thành envelope. Không handler nào đi qua
    /// đây: <c>ValidationBehavior</c> ném <c>ValidationException</c> TRƯỚC khi vào handler.
    ///
    /// <para><b>Vì sao thêm <c>fieldErrors</c> chứ không sửa <c>fields</c> tại chỗ</b> (2026-09-03):
    /// <c>fields</c> chỉ mang chuỗi trần, nên client không có gì để tra và buộc phải hiện thẳng câu
    /// BE sinh ra. Nghĩa là quyết định "ai sở hữu câu chữ" đang bị áp đặt bởi một field còn thiếu
    /// chứ không phải bởi ai đó chọn như vậy. Xem doc/huong_dan/wiki-core/be/16-i18n-va-ma-loi.md
    /// §4(b). Hai trường chạy song song có chủ đích — lý do đầy đủ ở
    /// <c>IApiResult&lt;T&gt;.FieldErrors</c>.</para>
    ///
    /// <para><b>Gộp MỘT lần, đổ ra hai trường.</b> Gộp hai lần độc lập là mở đường cho hai trường
    /// mô tả hai tập lỗi khác nhau sau một lần sửa cẩu thả — và không có gì báo, vì mỗi trường đọc
    /// riêng vẫn hợp lệ.</para>
    /// </summary>
    private static ApiResult<object> BuildValidationResult(ValidationException exception, string traceId)
    {
        var grouped = exception.Errors.GroupBy(e => NormalizeField(e.PropertyName)).ToList();

        var result = ApiResult<object>.ValidationError(
            ValidationErrors.Failed,
            grouped.ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()),
            grouped.ToDictionary(g => g.Key, g => g.Select(ToFieldError).ToArray()));

        result.TraceId = traceId;
        return result;
    }

    /// <summary>
    /// Mã lấy từ <c>ValidationFailure.ErrorCode</c> của FluentValidation — mặc định là TÊN
    /// VALIDATOR (<c>NotEmptyValidator</c>, <c>GreaterThanValidator</c>…). Dùng khoá thư viện đã
    /// cấp thay vì tự đặt khoá mới: lý do ở <see cref="ApiFieldError"/>.
    ///
    /// <para>Rule <c>Custom</c>/<c>CustomAsync</c> báo lỗi qua <c>context.AddFailure(prop, msg)</c>
    /// để lại <c>ErrorCode</c> null — ca có thật trong repo, nên nhánh dự phòng ở đây KHÔNG phải
    /// phòng xa. Nó phải là điều kiện trên chuỗi rỗng chứ không chỉ null: một <c>ErrorCode</c>
    /// rỗng ra tới client cũng vô dụng y như null, chỉ khác là nó lọt qua mọi phép kiểm null.</para>
    ///
    /// <para><b>Tham số của câu (thêm 2026-09-04,
    /// doc/huong_dan/wiki-core/be/16-i18n-va-ma-loi.md §10.3): CHUYỂN TIẾP, không dựng cơ chế.</b>
    /// <c>ValidationFailure.FormattedMessagePlaceholderValues</c> đã là một từ điển tham số ĐÃ ĐẶT
    /// TÊN, do <c>MessageFormatter</c> của chính FluentValidation sinh ra để ráp câu lỗi mặc định —
    /// lấy từ CÙNG đối tượng đang đọc <c>ErrorCode</c>/<c>ErrorMessage</c>, không thêm nguồn dữ
    /// liệu, không thêm lượt duyệt, không đụng validator nào. Đây là điều khiến việc này khả thi:
    /// nếu tham số phải đặt tay ở từng rule thì cơ chế chết ngay từ rule thứ mười.</para>
    ///
    /// <para><b>⚠️ Đi qua <see cref="MessageParamPolicy"/> chứ KHÔNG chuyển tiếp cả từ điển.</b> Từ
    /// điển gốc luôn chứa <c>PropertyValue</c> = giá trị người dùng vừa gõ; đổ thẳng ra là đẩy mật
    /// khẩu mới vừa nhập vào HTTP response ở đúng lần nhập hỏng. Lý do đầy đủ ở chính lớp đó.</para>
    /// </summary>
    private static ApiFieldError ToFieldError(ValidationFailure failure)
        => new(
            string.IsNullOrWhiteSpace(failure.ErrorCode) ? ApiFieldError.UnspecifiedCode : failure.ErrorCode,
            failure.ErrorMessage,
            MessageParamPolicy.FromValidationPlaceholders(failure.FormattedMessagePlaceholderValues));

    // Envelope (Data/Message/Status...) serialize camelCase qua PropertyNamingPolicy toàn cục
    // (xem Program.cs), NHƯNG Fields (Dictionary<string,string[]>) CỐ Ý giữ nguyên PascalCase
    // — DictionaryKeyPolicy mặc định là null (không set) nên key KHÔNG bị camelCase hoá, khớp
    // đúng tên property C# gốc (vd "Code", "MaxScore") mà FE đang mong đợi cho việc bind lỗi
    // vào field trên form (xem wiki-core/fe/02-http-envelope.md). Chỉ bỏ tiền tố "Request."
    // nếu FE gửi wrapper DTO, không đổi casing.
    private static string NormalizeField(string propertyName)
        => propertyName.StartsWith("Request.", StringComparison.Ordinal) ? propertyName["Request.".Length..] : propertyName;
}

using Microsoft.AspNetCore.Mvc;
using PlatformManager.Core.Application.Common.Results;

namespace PlatformManager.Api.Common;

/// <summary>
/// Dựng envelope cho lỗi <b>model binding</b> — nhánh 400 xảy ra TRƯỚC MediatR, nên
/// <c>GlobalExceptionHandler</c> không bao giờ thấy nó.
///
/// <para><b>Vì sao phải có lớp này</b> (2026-09-03): <c>[ApiController]</c> tự sinh
/// <c>ValidationProblemDetails</c> khi <c>ModelState</c> không hợp lệ, và trả nó THẲNG — không
/// <c>status</c>, không <c>code</c>, không <c>businessCode</c>, không <c>fields</c>. Ca này KHÔNG
/// hiếm: mọi command là record vị trí với tham số <c>string</c> non-nullable (dự án bật
/// <c>Nullable</c>) đều rơi vào đây khi client gửi thiếu field. Đo thật trên dây:
/// <c>POST /api/auth/login</c> với body <c>{}</c> trả về RFC-9110 problem details thô.
///
/// Hệ quả ở FE: <c>http-error.interceptor.ts</c> đọc <c>body.message</c> → <c>undefined</c> ⇒
/// toast rơi về câu dự phòng theo HTTP status ("Đã có lỗi xảy ra"), và <c>fields</c> vắng mặt nên
/// KHÔNG ô nhập nào được tô đỏ. Người dùng thấy một lỗi hệ thống ở đúng chỗ lẽ ra phải là
/// "bạn chưa nhập tên đăng nhập".</para>
///
/// <para><b>Vì sao KHÔNG chỉ dùng <c>SuppressModelStateInvalidFilter</c>:</b> tắt filter thì
/// request đi tiếp vào handler với model đã hỏng — và với record vị trí non-nullable thì đó là
/// <c>null</c> lọt vào chỗ hợp đồng nói là không bao giờ null. Chỗ này cần <b>đổi hình dạng
/// response</b>, không phải bỏ kiểm tra.</para>
/// </summary>
internal static class ModelBindingProblemFactory
{
    /// <summary>
    /// Câu trả cho client thay cho thông điệp gốc của model binder.
    ///
    /// <para><b>Cố ý KHÔNG chuyển tiếp <c>ModelError.ErrorMessage</c>.</b> Thông điệp gốc rò chi
    /// tiết nội bộ ra ngoài — đo thật: <c>"Expected depth to be zero … BytePositionInLine: 12."</c>
    /// (nội tại của bộ đọc JSON) và <c>"The cmd field is required."</c> (tên tham số C# của
    /// action, thứ client không có lý do gì được biết). Nó cũng là tiếng Anh, tức rò đúng loại
    /// chữ mà hướng A đang gỡ khỏi đường về client.</para>
    ///
    /// <para>Thông điệp gốc KHÔNG bị vứt đi — nó được ghi log kèm <c>TraceId</c>, cùng khuôn với
    /// nhánh 500 của <c>GlobalExceptionHandler</c>: chi tiết ở lại phía server, client nhận định
    /// danh để tra.</para>
    /// </summary>
    private const string FieldMessage = "Giá trị không hợp lệ.";

    /// <summary>
    /// Tên field mà binder dùng khi lỗi không thuộc về field nào (JSON hỏng toàn phần, sai
    /// Content-Type). Khoá rỗng ra tới client là một ô không tồn tại trên form.
    /// </summary>
    private const string BodyField = "Body";

    public static IActionResult Build(ActionContext context)
    {
        var traceId = context.HttpContext.TraceIdentifier;

        var grouped = context.ModelState
            .Where(entry => entry.Value is { Errors.Count: > 0 })
            .GroupBy(entry => NormalizeKey(entry.Key, context))
            .ToList();

        var result = ApiResult<object>.ValidationError(
            ValidationErrors.Failed,
            grouped.ToDictionary(g => g.Key, _ => new[] { FieldMessage }),
            grouped.ToDictionary(
                g => g.Key,
                _ => new[] { new ApiFieldError(ApiFieldError.ModelBindingCode, FieldMessage) }));

        result.TraceId = traceId;

        // Chi tiết thật ở lại server. Warning chứ không Error: đây là client gửi sai, không phải
        // bug của hệ thống — cùng mức mà GlobalExceptionHandler dùng cho nhánh validate.
        context.HttpContext.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(ModelBindingProblemFactory))
            .LogWarning(
                "Model binding lỗi — TraceId={TraceId}, chi tiết={Detail}",
                traceId,
                string.Join(" | ", context.ModelState
                    .Where(e => e.Value is { Errors.Count: > 0 })
                    .Select(e => $"{e.Key}: {string.Join("; ", e.Value!.Errors.Select(x => x.ErrorMessage))}")));

        return new BadRequestObjectResult(result);
    }

    /// <summary>
    /// Đưa khoá của binder về đúng tên field mà FE bind lỗi lên form.
    ///
    /// <para>Ba dạng khoá phải xử lý, cả ba đều đo được trên dây: <c>"$.userName"</c> (đường dẫn
    /// JSON khi parse hỏng), <c>"cmd"</c> (tên THAM SỐ của action — client không biết nó và không
    /// nên biết), và <c>"Request.UserName"</c> (wrapper DTO). Giữ nguyên PascalCase như
    /// <c>GlobalExceptionHandler.NormalizeField</c> — <c>DictionaryKeyPolicy</c> không được set
    /// nên khoá không bị camelCase hoá, và FE đang mong đúng tên property C#.</para>
    /// </summary>
    private static string NormalizeKey(string key, ActionContext context)
    {
        if (string.IsNullOrEmpty(key))
            return BodyField;

        // "$.userName" → "userName" → khớp property theo tên, không phân biệt hoa thường.
        if (key.StartsWith("$.", StringComparison.Ordinal))
            key = key[2..];
        else if (key == "$")
            return BodyField;

        if (key.StartsWith("Request.", StringComparison.Ordinal))
            key = key["Request.".Length..];

        // Khoá trùng tên tham số action (vd "cmd", "command", "query") là chi tiết nội bộ.
        var isActionParameter = context.ActionDescriptor.Parameters
            .Any(p => string.Equals(p.Name, key, StringComparison.Ordinal));

        return isActionParameter ? BodyField : key;
    }
}

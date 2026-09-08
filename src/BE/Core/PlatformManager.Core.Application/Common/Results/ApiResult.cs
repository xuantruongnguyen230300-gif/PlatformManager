namespace PlatformManager.Core.Application.Common.Results;

/// <summary>
/// [ĐƠN GIẢN HOÁ] chỉ dùng System.Text.Json — bỏ hẳn ShouldSerialize*()/dual-serializer
/// của bản gốc VNR (PlatformManager chỉ có 1 serializer). Xem
/// doc/huong_dan/quy-uoc/be-api-controller.md §Envelope response.
/// </summary>
public class ApiResult<T> : IApiResult<T>
{
    public T? Data { get; init; }
    public string? Message { get; init; }
    public ApiResultStatus Status { get; init; }
    public ErrorCode Code { get; init; }
    public string? BusinessCode { get; init; }
    public string? TraceId { get; set; }
    public bool? Retryable { get; init; }
    public Dictionary<string, string[]>? Fields { get; init; }
    public Dictionary<string, ApiFieldError[]>? FieldErrors { get; init; }
    public Dictionary<string, string>? MessageParams { get; init; }

    public static ApiResult<T> Success(T? data, string? message = null)
        => new() { Status = ApiResultStatus.SUCCESS, Code = ErrorCode.Success, Data = data, Message = message };

    /// <summary>
    /// <paramref name="messageParams"/> là tham số RỜI của câu (thêm 2026-09-04,
    /// doc/huong_dan/wiki-core/be/16-i18n-va-ma-loi.md §10). Tuỳ chọn, mặc định null ⇒ trường vắng
    /// mặt trên dây — nên mọi nơi gọi cũ giữ nguyên hành vi.
    ///
    /// <para><b>Truyền cùng lúc với <paramref name="message"/>, KHÔNG phải gán sau:</b> câu fallback
    /// và bộ tham số phải sinh ra từ CÙNG một bộ giá trị (xem <c>BaseResponse.Fail</c>) thì chúng
    /// mới không lệch nhau được. Gán rời ở nơi gọi là mở lại đúng khả năng câu nói một con số còn
    /// <c>messageParams</c> mang con số khác.</para>
    ///
    /// <para><b><paramref name="fieldErrors"/> — lỗi nghiệp vụ CŨNG chỉ được tới một ô nhập</b>
    /// (thêm 2026-09-05, doc/huong_dan/wiki-core/be/16-i18n-va-ma-loi.md §11). Trước đó trường này
    /// chỉ dựng được ở nhánh 400 (<see cref="ValidationError"/>), nên một mã Identity như
    /// <c>PasswordTooShort</c> — vốn LÀ lỗi của ô mật khẩu — không có đường nào ra ngoài ngoài việc
    /// bị nối chuỗi vào giữa câu tiếng Việt. Tuỳ chọn, mặc định null ⇒ trường vắng mặt trên dây.</para>
    ///
    /// <para>⚠️ <b>Đổi chữ ký hàm này thì PHẢI sửa <c>ExceptionHandlingBehavior.BuildErrorResponse</c></b>
    /// — nó gọi tới đây QUA REFLECTION với danh sách kiểu chính xác, nên thêm tham số làm
    /// <c>GetMethod</c> trả null và mọi <c>DomainException</c> thành <c>NullReferenceException</c>,
    /// KHÔNG có lỗi biên dịch. Lý do đầy đủ ghi tại chính chỗ đó.</para>
    /// </summary>
    public static ApiResult<T> BusinessError(
        ErrorDescriptor error,
        string message,
        Dictionary<string, string>? messageParams = null,
        Dictionary<string, ApiFieldError[]>? fieldErrors = null) => new()
    {
        Status = ApiErrorIds.StatusForCode(error.ErrorCode),
        Code = error.ErrorCode,
        BusinessCode = error.BusinessCode,
        Message = message,
        Retryable = error.Retryable,
        MessageParams = messageParams,
        FieldErrors = fieldErrors,
    };

    /// <summary>
    /// Dựng envelope cho nhánh 400.
    ///
    /// <para><b>Chữ ký đổi 2026-09-03, và trước đó hàm này là CODE CHẾT</b> — 0 nơi gọi, trong khi
    /// đường đi thật của mọi lỗi validate là <c>GlobalExceptionHandler</c> tự dựng envelope bằng
    /// tay. Hai bản dựng cho cùng một loại response là hai nguồn sự thật: bản không ai gọi thì
    /// không ai sửa, nên nó lặng lẽ lệch khỏi bản đang chạy (và đã lệch thật — nó thiếu cả
    /// <c>BusinessCode</c>). Nay handler gọi đúng hàm này, nên chỗ dựng chỉ còn MỘT.</para>
    ///
    /// <para>Nhận <see cref="ErrorDescriptor"/> thay cho <c>string message</c> vì cùng lý do
    /// <see cref="BusinessError"/> nhận: mã và câu phải đi cùng nhau từ một catalog, không rời ra
    /// để nơi gọi tự ghép. <c>Status</c> vẫn qua <see cref="ApiErrorIds.StatusForCode"/> chứ không
    /// gán tay <c>VALIDATION_ERROR</c> — giữ đúng luật "một nguồn duy nhất map ErrorCode →
    /// ApiResultStatus"; gán tay ở đây sẽ là chỗ thứ hai, và nó chỉ sai khi ai đó đổi bảng map.</para>
    /// </summary>
    public static ApiResult<T> ValidationError(
        ErrorDescriptor error,
        Dictionary<string, string[]> fields,
        Dictionary<string, ApiFieldError[]> fieldErrors) => new()
    {
        Status = ApiErrorIds.StatusForCode(error.ErrorCode),
        Code = error.ErrorCode,
        BusinessCode = error.BusinessCode,
        Message = error.MessageTemplate,
        Fields = fields,
        FieldErrors = fieldErrors,
    };

    /// <summary>
    /// Nhánh 500. Nhận <see cref="ErrorDescriptor"/> chứ không <c>string</c> (đổi 2026-09-03):
    /// bản cũ nhận chuỗi trần nên KHÔNG thể mang <c>BusinessCode</c>, và nó có <b>0 nơi gọi</b> —
    /// nhánh 500 thật thì dựng <c>ApiResult</c> bằng tay ở <c>GlobalExceptionHandler</c>. Đúng
    /// hình dạng "code chết lệch khỏi bản đang chạy" mà nhánh validate từng mắc.
    /// </summary>
    public static ApiResult<T> SystemError(ErrorDescriptor error) => new()
    {
        Status = ApiErrorIds.StatusForCode(error.ErrorCode),
        BusinessCode = error.BusinessCode,
        Code = error.ErrorCode,
        Message = error.MessageTemplate,
    };
}

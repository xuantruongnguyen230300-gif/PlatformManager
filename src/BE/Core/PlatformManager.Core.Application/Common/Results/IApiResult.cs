namespace PlatformManager.Core.Application.Common.Results;

/// <summary>Cho phép đọc Status/Code mà không cần biết T.</summary>
public interface IHasApiResultStatus
{
    ApiResultStatus Status { get; }
    ErrorCode Code { get; }
}

/// <summary>
/// Envelope response nhất quán cho MỌI endpoint (kể cả list/grid) — xem
/// doc/huong_dan/quy-uoc/be-api-controller.md §Envelope response.
/// </summary>
public interface IApiResult<T> : IHasApiResultStatus
{
    T? Data { get; }
    string? Message { get; }
    string? BusinessCode { get; }
    string? TraceId { get; set; }
    bool? Retryable { get; }
    Dictionary<string, string[]>? Fields { get; }

    /// <summary>
    /// Lỗi theo field, dạng <b>mã + câu</b>.
    ///
    /// <para><b>Hai nguồn, và chúng KHÔNG cùng ràng buộc</b> (mở rộng 2026-09-05,
    /// doc/huong_dan/wiki-core/be/16-i18n-va-ma-loi.md §11.2):</para>
    /// <list type="number">
    ///   <item><b>Nhánh 400</b> (<c>ApiResult&lt;T&gt;.ValidationError</c>) — cùng tập lỗi với
    ///   <see cref="Fields"/>, cùng bộ khoá, chỉ khác phần tử. Hai trường dựng MỘT lần rồi đổ ra
    ///   hai chỗ (<c>GlobalExceptionHandler.BuildValidationResult</c>).</item>
    ///   <item><b>Lỗi nghiệp vụ</b> (<c>ApiResult&lt;T&gt;.BusinessError</c>) — CHỈ trường này được
    ///   điền, <see cref="Fields"/> để trống. Không phải sót: nội dung ở đây là MÃ Identity
    ///   (<c>PasswordTooShort</c>), mà <see cref="Fields"/> thì mang chuỗi để hiện thẳng — đổ mã
    ///   vào đó là đặt một định danh tiếng Anh xuống dưới ô nhập của giao diện tiếng Việt. Lý do
    ///   đầy đủ ở nạp chồng <c>BaseResponse.Fail</c> nhận <c>fieldErrors</c>.</item>
    /// </list>
    ///
    /// <para><b>Hai trường chạy SONG SONG là cố ý, không phải trùng lặp bị bỏ quên.</b> Envelope
    /// là thứ MỌI màn hình đi qua, nên đổi shape một nhát tạo ra một khoảnh khắc client cũ gặp BE
    /// mới. Trình tự đã chốt: thêm trường mới cạnh <see cref="Fields"/> → client chuyển sang đọc
    /// trường mới → <b>rồi mới</b> gỡ <see cref="Fields"/>. Bước gỡ là bước 11 của
    /// doc/huong_dan/wiki-core/be/16-i18n-va-ma-loi.md §7 và KHÔNG được bỏ — bỏ nó là để lại vĩnh
    /// viễn hai đường đọc lỗi, tức đúng thứ mà việc thêm trường này đang trả giá để tránh.</para>
    ///
    /// <para><b>Khoá dictionary giữ PascalCase</b> y như <see cref="Fields"/> (cùng đi qua
    /// <c>NormalizeField</c>, và <c>DictionaryKeyPolicy</c> cố ý không set). Nhưng
    /// <see cref="ApiFieldError.Code"/>/<see cref="ApiFieldError.Message"/> là property của một
    /// object nên CHÚNG đi qua naming policy và ra dây dưới dạng <c>code</c>/<c>message</c>
    /// camelCase. Casing lệch nhau trong cùng một trường là hệ quả của quy ước đã có, ghi ra đây
    /// vì nó không đoán được từ tên trường.</para>
    /// </summary>
    Dictionary<string, ApiFieldError[]>? FieldErrors { get; }

    /// <summary>
    /// Tham số RỜI của câu mà <see cref="BusinessCode"/> trỏ tới — khoá là TÊN tham số
    /// (<c>UserName</c>, <c>MinLength</c>), giá trị đã đổi sang chuỗi bằng văn hoá invariant.
    ///
    /// <para><b>Vì sao envelope cần trường này</b>
    /// (doc/huong_dan/wiki-core/be/16-i18n-va-ma-loi.md §10): dưới hướng i18n đã chốt,
    /// <see cref="Message"/> tụt xuống vai trò <i>dev-facing + fallback</i> còn client dựng câu từ
    /// <see cref="BusinessCode"/>. Câu nào có tham số ("Tên đăng nhập 'abc' đã tồn tại") thì client
    /// KHÔNG tách lại được tham số ra khỏi chuỗi BE đã ráp, nên nó buộc phải hiện nguyên câu tiếng
    /// Việt — tức mọi thứ đã làm để lỗi mang mã dừng lại đúng trước cửa.</para>
    ///
    /// <para><b>NULL ⇒ vắng mặt trên dây</b> khi mã không có tham số, đúng khuôn
    /// <see cref="Retryable"/>/<see cref="Fields"/>: envelope không phình thêm một khoá cho MỌI
    /// lỗi chỉ vì một số lỗi cần nó.</para>
    ///
    /// <para><b>Khoá là TÊN chứ không phải số thứ tự</b> (<c>{0}</c>, <c>{1}</c>): khoá số buộc
    /// người dịch phải biết thứ tự BE truyền — thứ không có ở đâu trong bảng dịch — trong khi trật
    /// tự từ mỗi ngôn ngữ một khác. Đây cũng chính là khuôn khoá FluentValidation vốn đã dùng, nên
    /// nhánh lỗi validate không phải quy đổi gì.</para>
    ///
    /// <para><b>Cùng trường này còn nằm ở <see cref="ApiFieldError.MessageParams"/></b> — một cơ
    /// chế đặt ở hai chỗ, KHÔNG phải hai cơ chế: <c>messageParams</c> luôn nằm cạnh MÃ mà nó tham
    /// số hoá. Gom hết về gốc envelope thì hai field cùng fail một validator sẽ ghi đè khoá của
    /// nhau, và hỏng im lặng — câu của field này hiện con số của field kia (§10.1).</para>
    /// </summary>
    Dictionary<string, string>? MessageParams { get; }
}

using FluentValidation.Internal;

namespace PlatformManager.Core.Application.Common.Results;

/// <summary>
/// API mà handler thật sự dùng — chỉ 2 động từ Ok/Fail, không tự dựng ApiResult&lt;T&gt;
/// bằng tay ở từng handler. Xem doc/huong_dan/quy-uoc/be-cqrs-handler.md §BaseResponse.
/// </summary>
public abstract class BaseResponse
{
    protected static IApiResult<T> Ok<T>(T data, string? message = null)
        => ApiResult<T>.Success(data, message);

    /// <summary>
    /// Lỗi nghiệp vụ, kèm tham số của câu dưới dạng <b>cặp (tên, giá trị)</b>.
    ///
    /// <para><b>Chữ ký đổi 2026-09-04</b> (doc/huong_dan/wiki-core/be/16-i18n-va-ma-loi.md §10.5):
    /// trước đó là <c>params object[] args</c> — tham số ĐÃ được truyền vào đầy đủ,
    /// <c>string.Format</c> ráp chúng vào câu tiếng Việt rồi <b>vứt bản rời đi</b>, chỉ câu đã ráp
    /// ra tới envelope. Việc phải làm không phải là "thu thập tham số" mà là <b>thôi vứt chúng
    /// đi</b>: client cần bản rời để dựng lại câu ở ngôn ngữ đang chọn.</para>
    ///
    /// <para><b>Vì sao tên chứ không phải thứ tự:</b> khoá số (<c>{0}</c>) buộc bảng dịch phải biết
    /// thứ tự BE truyền — thứ không có ở đâu trong bảng dịch — trong khi trật tự từ mỗi ngôn ngữ
    /// một khác. Đổi lấy: nơi gọi phải gõ thêm tên, và tên phải khớp chỗ giữ trong
    /// <see cref="ErrorDescriptor.MessageTemplate"/>. Gõ sai tên KHÔNG làm đỏ biên dịch — nó để lại
    /// nguyên chỗ giữ <c>{Tên}</c> trong câu fallback, tức lộ ra ngay ở dòng chữ đọc được chứ
    /// không im lặng.</para>
    ///
    /// <para><b>MỘT nguồn, HAI đầu ra.</b> Câu fallback và <c>messageParams</c> dựng từ CÙNG một bộ
    /// giá trị trong cùng một vòng lặp, nên chúng không lệch nhau được. Dựng hai lần ở hai chỗ là
    /// mở lại đúng khả năng câu nói một con số còn tham số mang con số khác — và không có gì báo.</para>
    ///
    /// <para><b>Dùng <c>MessageFormatter</c> của FluentValidation</b> chứ không viết bộ ráp câu
    /// riêng: nó ráp theo khoá TÊN, là API công khai, và project này ĐÃ tham chiếu thư viện đó cho
    /// tầng validator — nên đây không thêm phụ thuộc nào. Cùng một lớp ráp câu cho cả lỗi nghiệp vụ
    /// lẫn lỗi validate là thứ biến "một cơ chế" từ khẩu hiệu thành điều kiểm được.</para>
    /// </summary>
    protected static IApiResult<T> Fail<T>(ErrorDescriptor error, params (string Name, object? Value)[] args)
    {
        // Không tham số ⇒ trả thẳng khuôn thông điệp, KHÔNG chạy qua bộ ráp câu: khuôn nào lỡ chứa
        // một cặp ngoặc nhọn cũng không bị diễn giải, và messageParams để null nên trường vắng mặt.
        if (args.Length == 0)
            return ApiResult<T>.BusinessError(error, error.MessageTemplate);

        var formatter = new MessageFormatter();
        var messageParams = new Dictionary<string, string>(args.Length, StringComparer.Ordinal);

        foreach (var (name, value) in args)
        {
            formatter.AppendArgument(name, value ?? string.Empty);
            messageParams[name] = MessageParamPolicy.Stringify(value);
        }

        return ApiResult<T>.BusinessError(error, formatter.BuildMessage(error.MessageTemplate), messageParams);
    }

    /// <summary>
    /// Lỗi nghiệp vụ mà nguyên nhân thật nằm ở <b>từng ô nhập</b> — câu giữ nguyên khuôn cố định,
    /// danh sách nguyên nhân đi ra <c>fieldErrors</c> (quyết định người dùng 2026-09-05,
    /// doc/huong_dan/wiki-core/be/16-i18n-va-ma-loi.md §11.2).
    ///
    /// <para><b>Vì sao KHÔNG dùng nạp chồng <c>Fail</c> có tham số ở trên cho việc này:</b> nối
    /// danh sách mã vào một chỗ giữ (<c>"Đổi mật khẩu thất bại: {Reasons}"</c>) là thứ vỡ khi đổi
    /// ngôn ngữ — trật tự từ và cách nối danh sách mỗi ngôn ngữ một khác, nên bảng dịch không có
    /// cách nào can thiệp vào chuỗi BE đã ghép sẵn. Danh sách phải đi ra dưới dạng DANH SÁCH.</para>
    ///
    /// <para><b>Chỉ điền <c>fieldErrors</c>, cố ý KHÔNG điền <c>Fields</c></b> — dù hai trường ở
    /// nhánh 400 luôn mang cùng tập lỗi (xem <c>IApiResult&lt;T&gt;.FieldErrors</c>). Lý do:
    /// <c>Fields</c> mang CHUỖI TRẦN để hiển thị thẳng, mà thứ ta có ở đây là MÃ
    /// (<c>PasswordTooShort</c>) — đổ vào đó là đặt một định danh tiếng Anh xuống ngay dưới ô nhập
    /// của giao diện tiếng Việt, tức đổi chỗ rò chứ không bịt. Mã chỉ đọc được sau khi đi qua bảng
    /// dịch, và bảng dịch thì tra theo <c>fieldErrors[].code</c>.</para>
    ///
    /// <para>Hệ quả đã biết và ghi ra ở §11.6: pha BE này <b>chưa</b> đổi gì trên màn hình, vì FE
    /// hôm nay còn bind lỗi từ <c>fields</c>. Việc tô đỏ đúng ô thuộc pha FE.</para>
    /// </summary>
    protected static IApiResult<T> Fail<T>(
        ErrorDescriptor error, Dictionary<string, ApiFieldError[]>? fieldErrors)
        => ApiResult<T>.BusinessError(error, error.MessageTemplate, messageParams: null, fieldErrors: fieldErrors);
}

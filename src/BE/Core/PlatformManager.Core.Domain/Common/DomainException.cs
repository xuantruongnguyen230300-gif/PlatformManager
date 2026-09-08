namespace PlatformManager.Core.Domain.Common;

/// <summary>
/// Ném từ Domain khi vi phạm invariant nghiệp vụ (factory method / mutation method).
/// Application layer bắt và dịch thành IApiResult lỗi (ErrorCode.BusinessRuleError, 422)
/// qua ErrorDescriptor tương ứng — xem doc/huong_dan/quy-uoc/be-entity-domain.md và cqrs-handler.md.
///
/// <para><b>Chỉ nhận <see cref="DomainError"/>, không nhận chuỗi mã tự do</b> (đổi 2026-09-03 theo
/// doc/huong_dan/wiki-core/be/16-i18n-va-ma-loi.md §4(a)). Trước đó chữ ký là
/// <c>(string code, string message)</c>, và chuỗi lập trình viên gõ tại chỗ <c>throw</c> đi THẲNG
/// ra field <c>businessCode</c> của envelope mà không qua catalog nào — repo vì vậy có hai hệ mã
/// trong cùng một field, và <c>ErrorCatalogTests</c> (chỉ soi field <c>static readonly</c>) mù với
/// hệ thứ hai. Bắt buộc truyền một bản ghi đã khai sẵn là cách DUY NHẤT khiến trình biên dịch chặn
/// mã sai khuôn ngay tại chỗ ném, thay vì phát hiện ở Production qua một mã FE không dịch được.</para>
/// </summary>
public class DomainException : Exception
{
    public DomainException(DomainError error, params object[] args)
        : base(Render(error, args))
    {
        Error = error;
    }

    /// <summary>Bản ghi lỗi đã khai trong catalog — nguồn của <c>businessCode</c> trên envelope.</summary>
    public DomainError Error { get; }

    /// <summary>
    /// Không gọi <c>string.Format</c> khi không có tham số: khuôn thông điệp chứa <c>{0}</c> mà
    /// format với 0 tham số sẽ ném <see cref="FormatException"/> — tức một lỗi nghiệp vụ bình
    /// thường biến thành lỗi 500 và nuốt mất mã lỗi thật.
    ///
    /// <para><b>Vẫn dùng chỗ giữ THEO SỐ ở đây, khác tầng Application</b> (đối chiếu 2026-09-05):
    /// đợt thi hành cơ chế tham số (doc/huong_dan/wiki-core/be/16-i18n-va-ma-loi.md §10) đã đổi
    /// <c>BaseResponse.Fail</c> sang chỗ giữ ĐẶT TÊN và đẩy tham số ra envelope, nhưng KHÔNG đụng
    /// đường Domain vì hôm nay <b>không</b> <c>DomainError</c> nào có khuôn thông điệp mang tham số
    /// — đổi một chữ ký cho 0 nơi dùng là công việc không kiểm chứng được bằng bất cứ ca nào. Khi
    /// <c>DomainError</c> đầu tiên cần tham số, đổi CẢ chữ ký này lẫn chỗ dựng envelope ở
    /// <c>ExceptionHandlingBehavior.BuildErrorResponse</c> — hai chỗ đó phải đi cùng nhau, và
    /// chú thích ở chỗ kia đã nói lại điều này để người sửa không chỉ thấy một nửa.</para>
    ///
    /// <para>KHÔNG truyền culture tường minh ở đây: câu này là <i>dev-facing + dự phòng</i>, không
    /// phải dữ liệu đi ra hợp đồng API — thứ bắt buộc invariant là GIÁ TRỊ THAM SỐ rời, và nó đã
    /// được đổi chuỗi ở <c>MessageParamPolicy</c> (§10.4). Xem thêm §5.1 về việc BE cố ý không bật
    /// hạ tầng bản địa hoá nào.</para>
    /// </summary>
    private static string Render(DomainError error, object[] args)
        => args.Length == 0 ? error.MessageTemplate : string.Format(error.MessageTemplate, args);
}

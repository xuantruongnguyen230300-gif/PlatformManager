namespace PlatformManager.Core.Application.Notifications;

/// <summary>
/// Một thông báo cần gửi, mô tả bằng <b>khoá + tham số + ngôn ngữ</b> — KHÔNG phải bằng chuỗi đã
/// dựng xong.
///
/// <para><b>Vì sao đổi</b> (2026-09-03, doc/huong_dan/wiki-core/be/16-i18n-va-ma-loi.md §6): email
/// là kênh <b>không có FE</b>, nên theo bảng phân xử ở §3 thì BE sở hữu câu chữ. Chữ ký cũ nhận
/// <c>(to, subject, body)</c> tức nhận chuỗi đã dựng — trách nhiệm dựng câu bị đẩy ngược lên nơi
/// gọi, và nơi gọi thì không biết người nhận đọc ngôn ngữ nào. Kết quả là BE sở hữu câu chữ trên
/// giấy nhưng trên thực tế câu chữ nằm rải ở các handler, mỗi handler một ngôn ngữ cứng.</para>
///
/// <para><b>Vì sao đổi NGAY hôm nay dù chưa ai gửi email:</b> seam này đang có <b>0 consumer</b>
/// (<c>PlatformManager.Api/Program.cs</c> ghi rõ nó cố ý chưa đăng ký), nên đổi chữ ký là sửa vài
/// file. Sau consumer đầu tiên, cùng việc đó là một cuộc di trú — và cửa sổ này chỉ mở một lần.</para>
/// </summary>
/// <param name="To">Địa chỉ người nhận. Vẫn là chuỗi vì nó là ĐỊNH DANH, không phải câu chữ.</param>
/// <param name="TemplateKey">
/// Khoá mẫu thông báo, ví dụ <c>"ACCOUNT.PASSWORD_RESET"</c>. Cùng vai trò với
/// <c>businessCode</c> của envelope: định danh ổn định để tra ra câu, không bao giờ được dịch.
/// </param>
/// <param name="Parameters">
/// Tham số RỜI để nơi dựng câu tự ráp vào mẫu.
///
/// <para><b>Vì sao <c>object?</c> chứ không <c>string</c>:</b> ép nơi gọi format sẵn thành chuỗi
/// nghĩa là ép nó quyết định cách hiển thị số và ngày — quyết định phụ thuộc ngôn ngữ, mà nơi gọi
/// lại là chỗ ít biết về ngôn ngữ người nhận nhất. Giữ giá trị thô để việc format thuộc về nơi
/// dựng câu, tức nơi đã biết <see cref="Locale"/>.</para>
///
/// <para>Cùng lý do với luật "không ghép sẵn tham số vào câu" ở §3 của file doc trên: ghép sẵn thì
/// không tách lại được.</para>
/// </param>
/// <param name="Locale">
/// Ngôn ngữ người nhận, dạng thẻ BCP-47 (<c>"vi"</c>, <c>"en"</c>).
///
/// <para><b>Bắt buộc, KHÔNG cho null và KHÔNG có giá trị mặc định</b> — có chủ đích. Một mặc định
/// ngầm ở đây sẽ tái lập đúng vấn đề vừa sửa: mọi nơi gọi bỏ qua tham số này, và hệ thống lại gửi
/// một ngôn ngữ cho mọi người mà không ai từng quyết định như vậy. Bắt buộc khai thì câu hỏi "lấy
/// ngôn ngữ của người nhận ở đâu" phải được trả lời tại chỗ, chứ không trôi qua im lặng.</para>
///
/// <para><b>Nợ CHƯA đóng, ghi ra vì nó không tự lộ:</b> hôm nay <b>chưa có chỗ nào lưu ngôn ngữ ưa
/// dùng</b> của người nhận — <c>AppUser</c> không có cột nào như vậy (đối chiếu 2026-09-03). Bảng
/// "sẽ thành" ở §6 của file doc trên có liệt dòng đó, nhưng nó là thay đổi lược đồ DB nên không
/// nằm trong bước đổi chữ ký này. Consumer đầu tiên sẽ phải đối mặt: hoặc thêm cột, hoặc lấy ngôn
/// ngữ từ phiên đang thao tác, hoặc chốt một hằng số ở tầng host — cả ba đều là quyết định, không
/// phải chi tiết cài đặt.</para>
/// </param>
public sealed record NotificationRequest(
    string To,
    string TemplateKey,
    IReadOnlyDictionary<string, object?> Parameters,
    string Locale);

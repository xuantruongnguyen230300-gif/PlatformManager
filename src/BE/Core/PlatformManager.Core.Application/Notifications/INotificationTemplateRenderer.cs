namespace PlatformManager.Core.Application.Notifications;

/// <summary>
/// Seam "mẫu thông báo của dự án" — <b>host cung cấp, Core tiêu thụ</b>. Biến
/// <see cref="NotificationRequest"/> (khoá + tham số + ngôn ngữ) thành tiêu đề và nội dung đã dựng.
///
/// <para><b>Vì sao seam này phải xuất hiện cùng lúc với việc đổi chữ ký</b>
/// (doc/huong_dan/wiki-core/be/16-i18n-va-ma-loi.md §6): sau khi
/// <see cref="INotificationSender"/> nhận khoá thay vì chuỗi, người gửi thư <b>không còn câu để
/// gửi</b>. Câu phải sinh ra ở đâu đó, và đó không thể là nơi gọi (nơi gọi vừa được giải phóng
/// khỏi việc đó) cũng không thể là <c>SmtpNotificationSender</c> (nó biết SMTP, không biết mẫu
/// thư của dự án nào). Bỏ seam này thì bước đổi chữ ký chỉ dời vấn đề đi chỗ khác.</para>
///
/// <para><b>Core cố ý KHÔNG có hiện thực mặc định</b> — cùng khuôn với
/// <c>ICoreMenuSeedSource</c> / <c>ICoreBootstrapAccountSource</c>. Một bản mặc định trả chuỗi
/// rỗng, hoặc trả thẳng khoá làm tiêu đề, sẽ biến "quên đăng ký" thành những email gửi đi thật với
/// nội dung vô nghĩa — hỏng trong im lặng, ở đúng kênh mà không ai nhìn thấy màn hình để phát
/// hiện. Thiếu đăng ký ⇒ DI không phân giải được <see cref="INotificationSender"/> ⇒ hỏng lúc
/// khởi động chứ không lúc gửi.</para>
///
/// <para><b>Bảng dịch nằm ở đây, không nằm ở Core</b>: Core giữ CƠ CHẾ và tập khoá của Core, dự án
/// cấp bản dịch của mình (§2 cùng file doc). Hình dạng cụ thể của kho mẫu — file, DB hay hằng số —
/// cố ý để mở: nó thuộc bước 6 của §7 và chưa chốt. Interface này chỉ khẳng định một điều, và đó
/// là điều duy nhất cần khẳng định lúc này: <b>ai đó ngoài Core phải trả lời câu hỏi "khoá này,
/// ngôn ngữ này, thì câu là gì".</b></para>
/// </summary>
public interface INotificationTemplateRenderer
{
    /// <summary>
    /// <c>Async</c> vì kho mẫu của dự án có quyền nằm ở file hoặc DB. Một chữ ký đồng bộ sẽ buộc
    /// hiện thực đầu tiên chạm IO phải chặn luồng hoặc đổi lại interface — mà đổi interface thì
    /// chỉ miễn phí đúng lúc còn 0 consumer, tức lúc này.
    /// </summary>
    /// <exception cref="KeyNotFoundException">
    /// Khoá không có trong kho mẫu. NÉM chứ không trả chuỗi rỗng: một email gửi đi với tiêu đề
    /// trống là lỗi không ai báo cáo lại được, còn một exception thì vào log kèm khoá thiếu.
    /// </exception>
    Task<RenderedNotification> RenderAsync(NotificationRequest request, CancellationToken ct);
}

/// <summary>
/// Kết quả dựng câu — tách khỏi <see cref="NotificationRequest"/> vì đây là chiều ngược lại: đã
/// hết khoá, hết tham số, chỉ còn chữ để đẩy ra kênh.
/// </summary>
public sealed record RenderedNotification(string Subject, string Body);

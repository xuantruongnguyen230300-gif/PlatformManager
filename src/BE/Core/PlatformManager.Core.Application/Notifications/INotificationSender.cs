namespace PlatformManager.Core.Application.Notifications;

/// <summary>
/// Seam tối thiểu cho gửi thông báo ra ngoài (hiện chỉ email) — xem
/// doc/huong_dan/quy-uoc/be-architecture.md §Notification. Application/Domain chỉ phụ thuộc interface
/// này, KHÔNG bao giờ biết tới SmtpClient/MailKit hay bất kỳ chi tiết hạ tầng nào
/// (implementation nằm ở Core.Infrastructure/Notifications/SmtpNotificationSender.cs).
/// </summary>
public interface INotificationSender
{
    /// <summary>
    /// Nhận <see cref="NotificationRequest"/> (khoá + tham số + ngôn ngữ) chứ KHÔNG nhận
    /// <c>(to, subject, body)</c> như trước 2026-09-03 — lý do đầy đủ ở docstring của
    /// <see cref="NotificationRequest"/> và doc/huong_dan/wiki-core/be/16-i18n-va-ma-loi.md §6.
    ///
    /// <para>Tóm tắt phần quan trọng: chữ ký cũ nhận <b>chuỗi đã dựng xong</b>, nên nơi gọi phải
    /// tự chọn câu chữ trong khi nó là chỗ ít biết nhất về ngôn ngữ của người nhận. Đổi lúc này vì
    /// seam còn <b>0 consumer</b>; sau consumer đầu tiên thì cùng việc đó là một cuộc di trú.</para>
    /// </summary>
    Task SendAsync(NotificationRequest request, CancellationToken ct);
}

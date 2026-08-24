namespace PlatformManager.Core.Application.Notifications;

/// <summary>
/// Seam tối thiểu cho gửi thông báo ra ngoài (hiện chỉ email) — xem
/// .claude/rules/architecture.md §Notification. Application/Domain chỉ phụ thuộc interface
/// này, KHÔNG bao giờ biết tới SmtpClient/MailKit hay bất kỳ chi tiết hạ tầng nào
/// (implementation nằm ở Core.Infrastructure/Notifications/SmtpNotificationSender.cs).
/// </summary>
public interface INotificationSender
{
    Task SendAsync(string to, string subject, string body, CancellationToken ct);
}

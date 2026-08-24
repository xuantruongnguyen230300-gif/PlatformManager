using System.Net.Mail;
using Microsoft.Extensions.Options;
using PlatformManager.Core.Application.Notifications;

namespace PlatformManager.Core.Infrastructure.Notifications;

/// <summary>
/// Implementation đầu tiên của <see cref="INotificationSender"/> — dùng
/// <see cref="SmtpClient"/> built-in .NET (KHÔNG thêm dependency MailKit — dependencies hiện
/// tại của solution không có sẵn MailKit, xem .claude/rules/architecture.md §Notification).
/// Đọc cấu hình qua <see cref="IOptions{TOptions}"/>, KHÔNG đọc IConfiguration trực tiếp (đúng
/// luật ở .claude/rules/architecture.md §"Project layout").
/// </summary>
public sealed class SmtpNotificationSender(IOptions<SmtpOptions> options) : INotificationSender
{
    public async Task SendAsync(string to, string subject, string body, CancellationToken ct)
    {
        var smtp = options.Value;

        using var client = new SmtpClient(smtp.Host, smtp.Port);
        using var message = new MailMessage(smtp.FromAddress, to, subject, body);

        await client.SendMailAsync(message, ct);
    }
}

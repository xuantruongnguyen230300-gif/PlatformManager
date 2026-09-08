using System.Net.Mail;
using Microsoft.Extensions.Options;
using PlatformManager.Core.Application.Notifications;

namespace PlatformManager.Core.Infrastructure.Notifications;

/// <summary>
/// Implementation đầu tiên của <see cref="INotificationSender"/> — dùng
/// <see cref="SmtpClient"/> built-in .NET (KHÔNG thêm dependency MailKit — dependencies hiện
/// tại của solution không có sẵn MailKit, xem doc/huong_dan/quy-uoc/be-architecture.md §Notification).
/// Đọc cấu hình qua <see cref="IOptions{TOptions}"/>, KHÔNG đọc IConfiguration trực tiếp (đúng
/// luật ở doc/huong_dan/quy-uoc/be-architecture.md §"Project layout").
/// </summary>
public sealed class SmtpNotificationSender(
    IOptions<SmtpOptions> options,
    INotificationTemplateRenderer renderer) : INotificationSender
{
    /// <summary>
    /// Dựng câu TRƯỚC, gửi SAU — và cố ý không bắt lỗi của bước dựng.
    ///
    /// <para><b>Vì sao lớp này không tự dựng câu</b> (2026-09-03): nó biết SMTP, không biết mẫu
    /// thư của dự án nào. Nhét bảng mẫu vào đây là nhét câu chữ của một dự án cụ thể vào Core —
    /// đúng thứ mà ranh giới "Core giữ cơ chế, dự án cấp dữ liệu" loại trừ, và là lý do
    /// <see cref="INotificationTemplateRenderer"/> tồn tại.</para>
    ///
    /// <para><b>Khoá mẫu thiếu ⇒ ném, không gửi.</b> Bắt lỗi rồi gửi một email tiêu đề rỗng là
    /// biến một lỗi cấu hình thành một lá thư thật gửi tới người thật — không hoàn tác được, và
    /// không ai báo cáo lại một cách hữu ích.</para>
    /// </summary>
    public async Task SendAsync(NotificationRequest request, CancellationToken ct)
    {
        var smtp = options.Value;
        var rendered = await renderer.RenderAsync(request, ct);

        using var client = new SmtpClient(smtp.Host, smtp.Port);
        using var message = new MailMessage(smtp.FromAddress, request.To, rendered.Subject, rendered.Body);

        await client.SendMailAsync(message, ct);
    }
}

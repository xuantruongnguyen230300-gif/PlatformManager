using System.ComponentModel.DataAnnotations;

namespace PlatformManager.Core.Infrastructure.Notifications;

/// <summary>
/// Bind từ section "Smtp" của appsettings — validate fail-fast qua
/// <c>ValidateDataAnnotations().ValidateOnStart()</c> (xem
/// NotificationInfrastructureExtensions.AddNotificationInfrastructure và
/// doc/huong_dan/quy-uoc/be-architecture.md §"Cấu hình — fail-fast validation").
///
/// <para>⚠️ <b>Hiện CHƯA có section <c>Smtp</c> trong cấu hình</b> — appsettings.json, file cấu
/// hình DUY NHẤT trong repo, không có khoá đó (appsettings.Development.json là cấu hình cục bộ
/// từng máy, không nằm trong repo) — phải thêm TRƯỚC khi gọi
/// <c>AddNotificationInfrastructure</c>, nếu không <c>ValidateOnStart()</c> ném
/// <c>OptionsValidationException</c> ngay lúc boot và app không khởi động được. Đó cũng chính là
/// lý do dòng đăng ký đang cố ý bị tắt — xem khối comment "Notification ... CỐ Ý CHƯA ĐĂNG KÝ"
/// ở PlatformManager.Api/Program.cs.</para>
///
/// <para>Và khi thêm thì điền giá trị THẬT: đặt placeholder cho "qua được validation" là cách
/// hỏng âm thầm đúng lúc cần gửi mail — appsettings.json từng có <c>localhost:25</c> kiểu đó, xem
/// docstring <c>BootstrapOptions</c>.</para>
/// </summary>
public sealed class SmtpOptions
{
    [Required] public string Host { get; init; } = default!;
    [Range(1, 65535)] public int Port { get; init; }
    [Required] public string FromAddress { get; init; } = default!;
}

using System.ComponentModel.DataAnnotations;

namespace PlatformManager.Core.Infrastructure.Notifications;

/// <summary>
/// Bind từ section "Smtp" của appsettings — validate fail-fast qua
/// <c>ValidateDataAnnotations().ValidateOnStart()</c> (xem
/// NotificationInfrastructureExtensions.AddNotificationInfrastructure và
/// .claude/rules/architecture.md §"Cấu hình — fail-fast validation"). Giá trị trong
/// appsettings.json hiện là PLACEHOLDER (localhost) — điền thật trước khi dùng production.
/// </summary>
public sealed class SmtpOptions
{
    [Required] public string Host { get; init; } = default!;
    [Range(1, 65535)] public int Port { get; init; }
    [Required] public string FromAddress { get; init; } = default!;
}

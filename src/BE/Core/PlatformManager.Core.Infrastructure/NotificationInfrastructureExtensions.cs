using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PlatformManager.Core.Application.Notifications;
using PlatformManager.Core.Infrastructure.Notifications;

namespace PlatformManager.Core.Infrastructure;

/// <summary>
/// Đăng ký riêng khỏi <see cref="DependencyInjection.AddCoreModule"/> có chủ đích — Notification
/// là seam dùng khi có nhu cầu thật (xem .claude/rules/architecture.md §Notification), Program.cs
/// (Api) gọi thẳng <see cref="AddNotificationInfrastructure"/> thay vì gộp ngầm vào
/// AddCoreModule() để lúc đọc Program.cs thấy rõ từng mảnh hạ tầng được bật ở đâu.
/// </summary>
public static class NotificationInfrastructureExtensions
{
    public static IServiceCollection AddNotificationInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<SmtpOptions>()
            .Bind(configuration.GetSection("Smtp"))
            .ValidateDataAnnotations()
            .ValidateOnStart(); // app KHÔNG khởi động được nếu thiếu/sai cấu hình Smtp — biết ngay, không đợi request đầu

        services.AddScoped<INotificationSender, SmtpNotificationSender>();

        return services;
    }
}

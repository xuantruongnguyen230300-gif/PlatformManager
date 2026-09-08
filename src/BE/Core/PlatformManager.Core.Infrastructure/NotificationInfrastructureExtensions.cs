using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PlatformManager.Core.Application.Notifications;
using PlatformManager.Core.Infrastructure.Notifications;

namespace PlatformManager.Core.Infrastructure;

/// <summary>
/// Đăng ký riêng khỏi <see cref="DependencyInjection.AddCoreModule"/> có chủ đích — Notification
/// là seam dùng khi có nhu cầu thật (xem doc/huong_dan/quy-uoc/be-architecture.md §Notification), Program.cs
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

        // KHÔNG đăng ký INotificationTemplateRenderer ở đây — host phải tự khai (thêm 2026-09-03
        // cùng lúc với việc đổi chữ ký INotificationSender). Cùng khuôn với ICoreMenuSeedSource /
        // ICoreBootstrapAccountSource: Core giữ cơ chế, dự án cấp dữ liệu — mà mẫu thư là dữ liệu
        // của dự án, không phải của Core.
        //
        // Hệ quả cố ý: host gọi AddNotificationInfrastructure mà quên đăng ký renderer thì
        // SmtpNotificationSender không phân giải được, và hỏng NGAY lúc phân giải chứ không lúc
        // gửi. Một bản mặc định "cho chạy được" sẽ đổi lỗi đó lấy những email gửi đi thật với nội
        // dung rỗng — hỏng ở kênh không có màn hình nào để ai đó nhìn thấy.
        return services;
    }
}

using Microsoft.Extensions.DependencyInjection;
using PlatformManager.Core.Application.Common.Interfaces;
using PlatformManager.Core.Infrastructure.BackgroundJobs;

namespace PlatformManager.Core.Infrastructure;

/// <summary>
/// Đăng ký riêng khỏi <see cref="DependencyInjection.AddCoreModule"/> có chủ đích — CÙNG LÝ DO
/// với <see cref="NotificationInfrastructureExtensions"/>: hiện thực này phụ thuộc
/// <c>IBackgroundJobClient</c> do <c>AddHangfire(...)</c> đăng ký, nên nó chỉ hợp lệ khi host
/// đã bật Hangfire. Gộp ngầm vào AddCoreModule() sẽ biến "quên gọi AddHangfire" thành lỗi
/// runtime lúc resolve, thay vì thấy rõ ngay khi đọc Program.cs.
/// </summary>
public static class BackgroundJobInfrastructureExtensions
{
    /// <summary>Gọi SAU <c>AddHangfire(...)</c> trong Program.cs.</summary>
    public static IServiceCollection AddBackgroundJobInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IBackgroundJobScheduler, HangfireBackgroundJobScheduler>();

        return services;
    }
}

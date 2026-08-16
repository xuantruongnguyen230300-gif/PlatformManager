using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using PlatformManager.Core.Application.Common.Behaviors;

namespace PlatformManager.Core.Application;

/// <summary>
/// Composition của riêng Core.Application — đăng ký MediatR/FluentValidation cho assembly này
/// (Auth/Users/Menu/Permissions) VÀ 2 pipeline behavior dùng CHUNG cho MỌI request trong toàn
/// hệ thống (kể cả request của Module khác — behavior là open-generic, áp dụng bất kể assembly
/// nào đăng ký handler). Vì behavior sống ở đây, đây là nơi DUY NHẤT đăng ký chúng — Module
/// KHÔNG đăng ký lại (xem Modules.DtiWeekly.Application/DependencyInjection.cs).
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddCoreApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ExceptionHandlingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}

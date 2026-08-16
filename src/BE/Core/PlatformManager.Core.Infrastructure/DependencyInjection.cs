using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PlatformManager.Core.Application;
using PlatformManager.Core.Application.Auth;
using PlatformManager.Core.Application.Common.Interfaces;
using PlatformManager.Core.Application.Menu;
using PlatformManager.Core.Application.Users;
using PlatformManager.Core.Infrastructure.Common;
using PlatformManager.Core.Infrastructure.Identity;
using PlatformManager.Core.Infrastructure.Persistence;
using PlatformManager.Core.Infrastructure.Persistence.Interceptors;
using PlatformManager.Core.Infrastructure.Persistence.Repositories;

namespace PlatformManager.Core.Infrastructure;

/// <summary>Composition duy nhất của Core module — Api gọi AddCoreModule() một lần trong
/// Program.cs, TRƯỚC AddDtiWeeklyModule()/mọi AddXxxModule() khác (Module có thể cần role/user
/// Core đã tồn tại lúc seed — xem doc/kien-truc-core-module.md).</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddCoreModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddCoreApplication();

        services.AddScoped<AuditInterceptor>();

        // Assembly CHÍNH NÓ (Core.Infrastructure) chứa AppUserConfiguration/AppRoleConfiguration/
        // SysMenuConfiguration/SysMenuRoleConfiguration — tự đăng ký, KHÔNG hardcode assembly của
        // bất kỳ Module nào (xem PlatformManagerDbContext.cs + doc/kien-truc-core-module.md).
        services.AddSingleton(new EfConfigurationAssembly(typeof(DependencyInjection).Assembly));

        services.AddDbContext<PlatformManagerDbContext>((sp, options) =>
        {
            // __EFMigrationsHistory đặt trong schema "core" (không phải "public" mặc định của
            // Postgres) — cùng chủ trương với HasDefaultSchema("core") ở
            // PlatformManagerDbContext.OnModelCreating: "public" không chứa bảng nào của app.
            // PHẢI khớp với PlatformManagerDbContextFactory.cs (design-time, sinh migration
            // script) — lệch nhau sẽ khiến runtime và migration script tra __EFMigrationsHistory
            // ở 2 schema khác nhau.
            options.UseNpgsql(configuration.GetConnectionString("Default"),
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "core"));
            options.AddInterceptors(sp.GetRequiredService<AuditInterceptor>());
        });

        services.AddIdentity<AppUser, AppRole>(options =>
            {
                // [ĐƠN GIẢN HOÁ] demo/nội bộ — nới lỏng độ phức tạp mật khẩu mặc định của
                // Identity, vẫn giữ độ dài tối thiểu hợp lý.
                options.Password.RequiredLength = 6;
                options.Password.RequireDigit = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;

                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;

                // Email chỉ là quy ước đặt tên, không bắt buộc duy nhất/không bắt buộc có —
                // xem doc/ke-hoach-xay-lai-corebase.md.
                options.User.RequireUniqueEmail = false;
            })
            .AddEntityFrameworkStores<PlatformManagerDbContext>()
            .AddDefaultTokenProviders();

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        services.AddScoped<ISysMenuRepository, SysMenuRepository>();
        services.AddScoped<ISysMenuRoleRepository, SysMenuRoleRepository>();

        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IUserAdminService, UserAdminService>();
        services.AddScoped<IUserLookupService, UserLookupService>();

        services.AddScoped<CoreSeeder>();

        return services;
    }
}

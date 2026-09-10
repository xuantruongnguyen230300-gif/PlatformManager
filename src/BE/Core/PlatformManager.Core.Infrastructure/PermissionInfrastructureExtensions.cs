using Microsoft.Extensions.DependencyInjection;
using PlatformManager.Core.Application.Permissions;
using PlatformManager.Core.Infrastructure.Permissions;
using PlatformManager.Core.Infrastructure.Persistence.Repositories;

namespace PlatformManager.Core.Infrastructure;

/// <summary>
/// Đăng ký DI riêng cho permission-key enforcement (RolePermissionRepository +
/// RequirePermissionFilter) — TÁCH khỏi DependencyInjection.AddCoreModule() có chủ đích, để
/// tránh 2 track sửa cùng lúc đụng chung 1 file. Extension này CHỈ đăng ký DI
/// (services.AddScoped), KHÔNG tự thêm filter vào MVC options — Program.cs (Api, composition
/// root) tự gọi AddPermissionInfrastructure() RỒI tự thêm
/// options.Filters.Add&lt;RequirePermissionFilter&gt;() khi cấu hình AddControllers(). Xem
/// doc/huong_dan/quy-uoc/be-api-controller.md §"Phân quyền theo hành động — permission-key đầy đủ".
/// </summary>
public static class PermissionInfrastructureExtensions
{
    public static IServiceCollection AddPermissionInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IRolePermissionRepository, RolePermissionRepository>();
        services.AddScoped<IPermissionChecker, PermissionChecker>();
        services.AddScoped<RequirePermissionFilter>();

        // Fail-fast cho DANH MỤC key mà host khai (thêm 2026-09-09). Không phải một dịch vụ nền —
        // nó chạy đúng một lần lúc khởi động rồi thôi; lý do đầy đủ ở chính lớp đó. Đặt cạnh phần
        // enforcement vì cấu hình sai ở đây làm hỏng đúng đường enforcement: key rơi khỏi danh mục
        // ⇒ RequirePermissionFilter deny-by-default ⇒ 403 không kèm lời giải thích nào.
        services.AddHostedService<ResourceKeyCatalogStartupValidator>();

        return services;
    }
}

using Microsoft.Extensions.DependencyInjection;
using PlatformManager.Business.Application.Criteria;
using PlatformManager.Business.Application.CriteriaGroups;
using PlatformManager.Business.Application.Dashboard;
using PlatformManager.Business.Application.Import;
using PlatformManager.Business.Persistence.Repositories;

namespace PlatformManager.Business.Persistence;

/// <summary>
/// Composition của <c>Business.Persistence</c>: nối interface repository (khai ở
/// <c>Business.Application</c>) với hiện thực EF ở tầng này.
///
/// <para><b>KHÔNG đăng ký <c>DbContext</c> ở đây.</b> <c>PlatformManagerDbContext</c> +
/// <c>UseNpgsql</c> do <c>AddCoreModule</c> khai một lần duy nhất — hai chỗ đăng ký cùng một
/// <c>DbContext</c> nghĩa là hai bộ options, và bộ nào thắng thì chỉ đọc thứ tự đăng ký mới
/// biết. Tầng nghiệp vụ chỉ TIÊU THỤ kiểu đó (doc/kien-truc-core-module.md §DbContext).</para>
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddBusinessPersistence(this IServiceCollection services)
    {
        // Scoped — cùng vòng đời với DbContext mà chúng nhận vào.
        services.AddScoped<ICriteriaGroupRepository, CriteriaGroupRepository>();
        services.AddScoped<ICriteriaGridRepository, CriteriaGridRepository>();
        services.AddScoped<IDashboardRepository, DashboardRepository>();

        // Đường GHI của lượt nạp (DM-7). Hai repository này là chỗ DUY NHẤT của cụm trả entity
        // THEO DÕI ĐƯỢC — xem docstring của CriteriaImportRepository.
        services.AddScoped<IImportJobRepository, ImportJobRepository>();
        services.AddScoped<ICriteriaImportRepository, CriteriaImportRepository>();
        services.AddScoped<ICriteriaWriteRepository, CriteriaWriteRepository>();

        // Seeder: chỉ tiến trình `--seed` phân giải nó (SeedCommand ở host). Đăng ký ở đây thay
        // vì ở host để host không phải biết seeder của tầng nghiệp vụ nằm ở project nào.
        services.AddScoped<BusinessSeeder>();

        return services;
    }
}

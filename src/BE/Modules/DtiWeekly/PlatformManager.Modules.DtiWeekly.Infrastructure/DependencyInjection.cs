using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PlatformManager.Core.Infrastructure.Persistence;
using PlatformManager.Modules.DtiWeekly.Application;
using PlatformManager.Modules.DtiWeekly.Application.Criteria;
using PlatformManager.Modules.DtiWeekly.Application.CriteriaGroups;
using PlatformManager.Modules.DtiWeekly.Application.Assessments;
using PlatformManager.Modules.DtiWeekly.Infrastructure.Persistence;
using PlatformManager.Modules.DtiWeekly.Infrastructure.Persistence.Repositories;

namespace PlatformManager.Modules.DtiWeekly.Infrastructure;

/// <summary>Composition duy nhất của module DTI Weekly — Api gọi AddDtiWeeklyModule() một lần
/// trong Program.cs, SAU AddCoreModule(). Khuôn mẫu cho mọi module nghiệp vụ mới sau này (xem
/// doc/kien-truc-core-module.md §Nguyên tắc áp dụng khi thêm module nghiệp vụ mới).</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddDtiWeeklyModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDtiWeeklyApplication();

        // Tự đăng ký assembly của MÌNH (chứa CriteriaGroupConfiguration/CriteriaConfiguration/
        // CriteriaAssessmentConfiguration/CriteriaEvidenceConfiguration) để
        // PlatformManagerDbContext (Core.Infrastructure) áp dụng — Core không hardcode biết tới
        // assembly này (xem PlatformManagerDbContext.cs + doc/kien-truc-core-module.md).
        services.AddSingleton(new EfConfigurationAssembly(typeof(DependencyInjection).Assembly));

        services.AddScoped<ICriteriaGroupRepository, CriteriaGroupRepository>();
        services.AddScoped<ICriteriaRepository, CriteriaRepository>();
        services.AddScoped<ICriteriaEvidenceRepository, CriteriaEvidenceRepository>();

        // ICriteriaAssessmentRepository (ghi/kiểm tra tồn tại) + ICriteriaAssessmentQueryRepository
        // (đọc dạng lưới/tổng hợp) tách theo ISP nhưng dùng CHUNG 1 instance CriteriaAssessmentRepository
        // trong cùng scope (stateless, chỉ bọc PlatformManagerDbContext scoped sẵn có) — đăng ký
        // class cụ thể 1 lần rồi trỏ cả 2 interface vào đó, tránh 2 instance riêng biệt không cần thiết.
        services.AddScoped<CriteriaAssessmentRepository>();
        services.AddScoped<ICriteriaAssessmentRepository>(sp => sp.GetRequiredService<CriteriaAssessmentRepository>());
        services.AddScoped<ICriteriaAssessmentQueryRepository>(sp => sp.GetRequiredService<CriteriaAssessmentRepository>());

        services.AddScoped<DtiWeeklySeeder>();

        return services;
    }
}

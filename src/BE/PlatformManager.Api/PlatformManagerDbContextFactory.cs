using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using PlatformManager.Core.Infrastructure.Persistence;
using PlatformManager.Modules.DtiWeekly.Infrastructure.Persistence.Configurations;

namespace PlatformManager.Api;

/// <summary>
/// Cho phép `dotnet ef` chạy được ngoài runtime DI, KHÔNG chạy Program.cs thật (tránh vô tình
/// kích hoạt seed/kết nối DB thật lúc chỉ đang sinh migration) — chỉ dùng lúc thiết kế
/// (migrations add/script), KHÔNG dùng để chạy app.
///
/// Đặt Ở ĐÂY (Api), KHÔNG đặt ở Core.Infrastructure — vì factory cần biết đủ assembly của MỌI
/// Module đã đăng ký (để `dotnet ef migrations add` sinh migration đầy đủ, bao gồm cả bảng của
/// Module) trong khi Core.Infrastructure tuyệt đối không được phép biết tới bất kỳ
/// Modules.*.Infrastructure nào (xem doc/kien-truc-core-module.md §DbContext). Api là composition
/// root duy nhất thấy cả 2 bên nên đây là chỗ đúng cho factory này — khi thêm Module mới, chỉ
/// cần thêm 1 dòng vào <see cref="ConfigurationAssemblies"/> bên dưới.
/// </summary>
public class PlatformManagerDbContextFactory : IDesignTimeDbContextFactory<PlatformManagerDbContext>
{
    public PlatformManagerDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("Default")
            ?? "Host=localhost;Port=5432;Database=platformmanager_dev;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<PlatformManagerDbContext>();
        // __EFMigrationsHistory → schema "core", PHẢI khớp với DependencyInjection.cs (runtime) —
        // xem comment ở đó.
        optionsBuilder.UseNpgsql(connectionString,
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "core"));

        // Danh sách assembly chứa IEntityTypeConfiguration<T> — Core.Infrastructure's own
        // (SysMenu/SysMenuRole/AppUser/AppRole) + từng Module đã đăng ký (thêm dòng mới khi
        // thêm Module).
        var configurationAssemblies = new List<EfConfigurationAssembly>
        {
            new(typeof(PlatformManagerDbContext).Assembly),
            new(typeof(CriteriaConfiguration).Assembly),
        };

        return new PlatformManagerDbContext(optionsBuilder.Options, configurationAssemblies);
    }
}

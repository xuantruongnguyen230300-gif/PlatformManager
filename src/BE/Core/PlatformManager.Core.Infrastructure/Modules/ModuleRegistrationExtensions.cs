using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PlatformManager.Core.Application.Modules;
using PlatformManager.Core.Infrastructure.Persistence;

namespace PlatformManager.Core.Infrastructure.Modules;

/// <summary>
/// Phía CƠ CHẾ của seam <see cref="IModuleRegistrar"/>: host nộp danh sách registrar, hai hàm
/// dưới đây nối chúng vào nền tảng. Host không tự tay làm ba việc này cho từng tầng — làm tay
/// nghĩa là mỗi tầng mới lại phải nhớ đủ ba bước, và bước bị quên sẽ hỏng trong im lặng.
///
/// <para>Hai hàm tách rời vì chúng chạy ở hai thời điểm khác nhau của lúc dựng ứng dụng:
/// ApplicationPart phải gắn ngay tại <c>AddControllers()</c> (nó trả về <c>IMvcBuilder</c>),
/// còn DI + EF thì gắn vào <c>IServiceCollection</c> sau đó. Cùng MỘT danh sách registrar đi
/// qua cả hai — đó là điều kiện để "thêm một tầng" chỉ là thêm một phần tử vào danh sách.</para>
/// </summary>
public static class ModuleRegistrationExtensions
{
    /// <summary>
    /// Đường 1 và 2 của seam: gọi <see cref="IModuleRegistrar.RegisterServices"/> của từng tầng,
    /// rồi nộp <see cref="IModuleRegistrar.PersistenceAssembly"/> của tầng đó vào DI dưới dạng
    /// <see cref="EfConfigurationAssembly"/> để <c>PlatformManagerDbContext.OnModelCreating</c>
    /// quét.
    ///
    /// <para><b>Thứ tự trong danh sách là thứ tự đăng ký.</b> Core phải đứng đầu — tầng nghiệp
    /// vụ có thể cần dịch vụ Core đã có mặt (và lúc seed thì cần role/user Core đã tồn tại).</para>
    ///
    /// <para><b>Registrar tự nó cũng vào DI</b> (dưới kiểu <see cref="IModuleRegistrar"/>): nhờ
    /// vậy đọc lại được danh sách tầng đã cắm mà không cần một nguồn sự thật thứ hai — ArchTest
    /// dùng đúng đường này để biết tầng nào ĐÃ được nối vào thật.</para>
    /// </summary>
    public static IServiceCollection AddModules(
        this IServiceCollection services,
        IConfiguration configuration,
        IEnumerable<IModuleRegistrar> registrars)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(registrars);

        var ordered = registrars.ToList();

        // Danh sách rỗng nghĩa là KHÔNG có Core: ứng dụng sẽ dựng lên với DI trống rỗng và chỉ
        // chết ở request đầu tiên. Chết ngay tại đây rẻ hơn nhiều.
        if (ordered.Count == 0)
        {
            throw new InvalidOperationException(
                "Không tầng nào được nộp cho AddModules — ít nhất CoreModuleRegistrar phải có mặt. " +
                "Xem doc/kien-truc-core-module.md §IModuleRegistrar.");
        }

        var duplicate = ordered
            .GroupBy(registrar => registrar.ModuleName, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicate is not null)
        {
            throw new InvalidOperationException(
                $"Hai tầng cùng khai ModuleName '{duplicate.Key}'. Tên tầng phải duy nhất — trùng tên " +
                "nghĩa là một danh sách registrar bị nối hai lần, hoặc một tầng được chép ra mà quên đổi " +
                "tên; cả hai đều làm dịch vụ bị đăng ký hai lượt.");
        }

        foreach (var registrar in ordered)
        {
            services.AddSingleton(registrar);
            registrar.RegisterServices(services, configuration);
            services.AddSingleton(new EfConfigurationAssembly(registrar.PersistenceAssembly));
        }

        return services;
    }

    /// <summary>
    /// Đường 3 của seam: controller sống trong assembly khác assembly host thì MVC KHÔNG tự tìm
    /// thấy — phải nạp assembly đó làm ApplicationPart. Registrar khai
    /// <see cref="IModuleRegistrar.ApiAssembly"/> là <c>null</c> thì bỏ qua (tầng không có
    /// controller riêng).
    ///
    /// <para>PHẢI gọi ngay trên <c>IMvcBuilder</c> do <c>AddControllers()</c> trả về — sau khi
    /// đã rời khỏi builder thì không còn chỗ nối phần này vào nữa.</para>
    /// </summary>
    public static IMvcBuilder AddModuleApplicationParts(
        this IMvcBuilder builder,
        IEnumerable<IModuleRegistrar> registrars)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(registrars);

        foreach (var registrar in registrars)
        {
            if (registrar.ApiAssembly is { } apiAssembly)
                builder.AddApplicationPart(apiAssembly);
        }

        return builder;
    }
}

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PlatformManager.Business.Application.Dashboard;
using PlatformManager.Business.Infrastructure.Export;

namespace PlatformManager.Business.Infrastructure;

/// <summary>
/// Composition của <c>Business.Infrastructure</c> — phần tích hợp RA NGOÀI DATABASE của tầng
/// nghiệp vụ.
///
/// <para><b>Người tiêu thụ đầu tiên đã tới, đúng như dự đoán 2026-09-10:</b> bộ ghi file
/// <c>.xlsx</c> của DB-4, viết NPOI trực tiếp ở tầng này (Q9, doc/contracts/dashboard.md). Trước
/// lượt 2026-09-11 hàm này đăng ký rỗng — đó là trạng thái đúng dự kiến khi vòng 1 chỉ có đường
/// ĐỌC.</para>
///
/// <para><b>Vì sao đường nối này dựng sẵn từ trước khi có gì để đăng ký</b>: một đường nối chỉ
/// dựng lúc cần đến sẽ được viết vào đúng lúc người viết đang bận việc khác — và đó là lúc người
/// ta đăng ký nhầm chỗ, thường là vào <c>Persistence</c>, nơi đã có sẵn một hàm tương tự.</para>
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddBusinessInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        // Singleton: không trạng thái, không giữ tài nguyên nào giữa hai lần ghi — mỗi lời gọi
        // Write dựng workbook riêng rồi vứt.
        services.AddSingleton<IDashboardExportWriter, DashboardExcelExportWriter>();

        return services;
    }
}

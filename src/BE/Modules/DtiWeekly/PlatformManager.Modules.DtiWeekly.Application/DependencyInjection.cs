using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using PlatformManager.Modules.DtiWeekly.Application.Assessments;
using PlatformManager.Modules.DtiWeekly.Application.Dashboard;
using PlatformManager.Modules.DtiWeekly.Application.Import;

namespace PlatformManager.Modules.DtiWeekly.Application;

/// <summary>
/// Composition của riêng Modules.DtiWeekly.Application — đăng ký MediatR/FluentValidation cho
/// assembly này (Criteria/CriteriaGroups/Assessments/Dashboard) + service Application-layer
/// thuần (AssessmentUpsertService/CsvImportService/AggregationService — không phụ thuộc EF,
/// chỉ qua repository interface). KHÔNG đăng ký lại 2 pipeline behavior — Core.Application đã
/// đăng ký, áp dụng chung cho MỌI request kể cả của Module này (xem
/// Core.Application/DependencyInjection.cs).
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddDtiWeeklyApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);

        services.AddScoped<IAssessmentUpsertService, AssessmentUpsertService>();
        services.AddScoped<ICsvImportService, CsvImportService>();
        services.AddScoped<IAggregationService, AggregationService>();

        // Import job nền (CONTRACT DM-7) — xem doc/huong_dan/quy-uoc/be-cqrs-handler.md
        // §"Command chạy lâu → job nền". IImportFileStorage/IImportJobRepository (cần
        // IWebHostEnvironment/DbContext) + IImportFileReader impl Excel (cần NPOI) đăng ký ở
        // Modules.DtiWeekly.Infrastructure/DependencyInjection.cs — KHÔNG đặt ở đây.
        services.AddScoped<IImportJobRunner, ImportJobRunner>();
        services.AddScoped<IImportRowProcessor, ImportRowProcessor>();
        // CsvFileReader không phụ thuộc gì ngoài CsvHelper (thư viện parse thuần) nên đăng ký
        // được ngay ở Application — ExcelFileReader (NPOI) đăng ký ở Infrastructure. Cả 2 cùng
        // implement IImportFileReader, ImportJobRunner resolve qua IEnumerable<IImportFileReader>
        // rồi tự chọn đúng reader theo phần mở rộng file (xem IImportFileReader.CanRead).
        services.AddScoped<IImportFileReader, CsvFileReader>();

        return services;
    }
}

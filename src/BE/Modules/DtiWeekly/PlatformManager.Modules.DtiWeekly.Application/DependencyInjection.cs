using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using PlatformManager.Modules.DtiWeekly.Application.Assessments;
using PlatformManager.Modules.DtiWeekly.Application.Dashboard;

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

        return services;
    }
}

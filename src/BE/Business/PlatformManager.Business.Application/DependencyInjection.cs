using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using PlatformManager.Business.Application.Common;
using PlatformManager.Business.Application.Criteria;
using PlatformManager.Business.Application.Import;
using PlatformManager.Business.Application.Permissions;

namespace PlatformManager.Business.Application;

/// <summary>
/// Composition của riêng <c>Business.Application</c> — đăng ký MediatR/FluentValidation cho
/// assembly NÀY và các dịch vụ thuần Application của tầng nghiệp vụ.
///
/// <para><b>KHÔNG đăng ký lại hai pipeline behavior dùng chung</b>
/// (<c>ExceptionHandlingBehavior</c>, <c>ValidationBehavior</c>): chúng sống ở
/// <c>Core.Application</c> và là open-generic, nên chúng đã áp cho MỌI request bất kể assembly
/// nào đăng ký handler. Đăng ký lần thứ hai làm mỗi request đi qua chúng HAI lượt — validator
/// chạy hai lần, ngoại lệ được dịch hai lần, và không có lỗi nào báo. Xem docstring của
/// <c>Core.Application/DependencyInjection.cs</c>.</para>
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddBusinessApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);

        // Scoped: phụ thuộc ICurrentUser (scoped theo request) — đăng ký singleton ở đây sẽ giữ
        // lại danh tính của request ĐẦU TIÊN cho mọi request sau, tức trả canWrite của người khác.
        services.AddScoped<IDtiWriteAccess, DtiWriteAccess>();

        // Thân của lượt nạp file (DM-7). Scoped, KHÔNG singleton: nó dùng IUnitOfWork/repository —
        // tức DbContext, thứ có vòng đời scoped. Một singleton giữ DbContext của lần chạy đầu tiên
        // sẽ dùng lại đúng change tracker đó cho mọi job sau, và hai lượt nạp sẽ ghi đè nhau.
        //
        // Worker Hangfire tự mở một scope DI riêng cho mỗi job (AspNetCoreJobActivator), nên
        // "scoped" ở đây là đúng ngữ cảnh chạy chứ không phải một lựa chọn cho có.
        services.AddScoped<IImportJobRunner, ImportJobRunner>();

        // Đường ghi TAY (DM-3…DM-6). Scoped vì phụ thuộc repository, tức DbContext.
        services.AddScoped<AssessmentWriter>();

        // Scoped chứ không singleton dù nó chỉ bọc đồng hồ: nó phải đọc CÙNG một mốc thời gian với
        // phần còn lại của request, và vòng đời scoped là cách khai điều đó ra.
        services.AddScoped<IDtiPeriodContext, DtiPeriodContext>();

        return services;
    }
}

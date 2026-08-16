using System.Reflection;
using Xunit;

namespace PlatformManager.ArchTests;

/// <summary>
/// 2 ArchTest theo đúng doc/kien-truc-core-module.md §"2 ArchTest mới cần thêm" — bắt đúng 2
/// hướng vi phạm dễ xảy ra nhất khi thêm code/module mới: Core lỡ tay biết tới 1 Module cụ thể,
/// hoặc 1 Module lỡ tay reference thẳng Module khác thay vì nâng logic dùng chung lên
/// Core.Application. `Modules_MustNotReference_OtherModules` hiện vô nghĩa (chỉ có 1 module
/// DtiWeekly) — đó chính là điểm hay: bảo hiểm MIỄN PHÍ cho ngày có module #2, bắt lỗi ngay ở
/// CI trước khi kịp ăn sâu vào code.
/// </summary>
public class CoreModuleBoundaryTests
{
    private static readonly Assembly[] CoreAssemblies =
    [
        typeof(PlatformManager.Core.Domain.Common.BaseEntity).Assembly,
        typeof(PlatformManager.Core.Application.DependencyInjection).Assembly,
        typeof(PlatformManager.Core.Infrastructure.DependencyInjection).Assembly,
    ];

    // Thêm module nghiệp vụ mới: thêm bộ 3 (Domain/Application/Infrastructure) vào đây, gắn
    // đúng tên module — 2 test bên dưới tự động bao phủ module mới mà không cần sửa gì khác.
    private static readonly (string ModuleName, Assembly Assembly)[] ModuleAssemblies =
    [
        ("DtiWeekly", typeof(PlatformManager.Modules.DtiWeekly.Domain.Entities.Criteria).Assembly),
        ("DtiWeekly", typeof(PlatformManager.Modules.DtiWeekly.Application.DependencyInjection).Assembly),
        ("DtiWeekly", typeof(PlatformManager.Modules.DtiWeekly.Infrastructure.DependencyInjection).Assembly),
    ];

    [Fact]
    public void Core_MustNotReference_AnyModulesAssembly()
    {
        var violations = new List<string>();

        foreach (var coreAssembly in CoreAssemblies)
        {
            var referencedModuleAssemblies = coreAssembly.GetReferencedAssemblies()
                .Where(a => a.Name!.StartsWith("PlatformManager.Modules.", StringComparison.Ordinal))
                .Select(a => a.Name!);

            violations.AddRange(referencedModuleAssemblies.Select(n => $"{coreAssembly.GetName().Name} -> {n}"));
        }

        Assert.True(violations.Count == 0, $"Core reference Modules (KHÔNG được phép): {string.Join(", ", violations)}");
    }

    [Fact]
    public void Modules_MustNotReference_OtherModules()
    {
        var violations = new List<string>();
        var moduleNames = ModuleAssemblies.Select(m => m.ModuleName).Distinct().ToList();

        foreach (var (moduleName, assembly) in ModuleAssemblies)
        {
            var referencedNames = assembly.GetReferencedAssemblies().Select(a => a.Name!).ToList();

            foreach (var otherModule in moduleNames.Where(m => m != moduleName))
            {
                var otherModulePrefix = $"PlatformManager.Modules.{otherModule}.";
                var crossReferences = referencedNames.Where(n => n.StartsWith(otherModulePrefix, StringComparison.Ordinal));
                violations.AddRange(crossReferences.Select(n => $"{assembly.GetName().Name} -> {n}"));
            }
        }

        Assert.True(violations.Count == 0, $"Module reference module khác (KHÔNG được phép): {string.Join(", ", violations)}");
    }
}

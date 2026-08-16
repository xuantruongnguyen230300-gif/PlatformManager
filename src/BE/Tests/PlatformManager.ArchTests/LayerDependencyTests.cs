using PlatformManager.Core.Domain.Common;
using Xunit;

namespace PlatformManager.ArchTests;

/// <summary>
/// Bắt bằng máy — không dựa vào review người — luật kiến trúc quan trọng nhất của dự án (xem
/// doc/huong_dan/wiki-core/be/00-lo-trinh-tong-the.md Nguyên tắc 3, .claude/rules/architecture.md,
/// doc/kien-truc-core-module.md). Cố tình chạy test này FAIL trước khi có (thêm
/// EntityFrameworkCore vào Core.Domain.csproj tạm thời) để xác nhận nó thật sự bắt được lỗi.
/// </summary>
public class LayerDependencyTests
{
    [Fact]
    public void Core_Domain_Assembly_MustHave_ZeroPackageReference()
    {
        var referenced = typeof(BaseEntity).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name!)
            .Where(n => !n.StartsWith("System", StringComparison.Ordinal)
                        && !n.StartsWith("netstandard", StringComparison.Ordinal)
                        && !n.StartsWith("mscorlib", StringComparison.Ordinal))
            .ToList();

        Assert.True(referenced.Count == 0, $"Core.Domain đang reference (phải zero): {string.Join(", ", referenced)}");
    }

    [Fact]
    public void Modules_DtiWeekly_Domain_MustOnlyDependOn_CoreDomain()
    {
        var referenced = typeof(PlatformManager.Modules.DtiWeekly.Domain.Entities.Criteria).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name!)
            .Where(n => !n.StartsWith("System", StringComparison.Ordinal)
                        && !n.StartsWith("netstandard", StringComparison.Ordinal)
                        && !n.StartsWith("mscorlib", StringComparison.Ordinal)
                        && n != "PlatformManager.Core.Domain")
            .ToList();

        Assert.True(referenced.Count == 0,
            $"Modules.DtiWeekly.Domain chỉ được reference PlatformManager.Core.Domain: {string.Join(", ", referenced)}");
    }

    [Fact]
    public void Core_Application_MustNotDependOn_EntityFrameworkCore_Or_AspNetCore_Or_AnyInfrastructure()
    {
        var referenced = typeof(PlatformManager.Core.Application.DependencyInjection).Assembly
            .GetReferencedAssemblies().Select(a => a.Name!).ToList();

        var forbidden = referenced.Where(n =>
                n.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal)
                || n.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal)
                || n.Contains(".Infrastructure", StringComparison.Ordinal))
            .ToList();

        Assert.True(forbidden.Count == 0, $"Core.Application đang reference (không được phép): {string.Join(", ", forbidden)}");
    }

    [Fact]
    public void Modules_DtiWeekly_Application_MustNotDependOn_EntityFrameworkCore_Or_AspNetCore_Or_AnyInfrastructure()
    {
        var referenced = typeof(PlatformManager.Modules.DtiWeekly.Application.DependencyInjection).Assembly
            .GetReferencedAssemblies().Select(a => a.Name!).ToList();

        var forbidden = referenced.Where(n =>
                n.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal)
                || n.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal)
                || n.Contains(".Infrastructure", StringComparison.Ordinal))
            .ToList();

        Assert.True(forbidden.Count == 0,
            $"Modules.DtiWeekly.Application đang reference (không được phép): {string.Join(", ", forbidden)}");
    }
}

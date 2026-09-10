using Microsoft.Extensions.Hosting;
using PlatformManager.Core.Infrastructure.Storage;
using Xunit;

namespace PlatformManager.Core.UnitTests.Storage;

/// <summary>
/// <see cref="StorageOptionsValidator"/> — luật khác nhau theo môi trường, nên nó phải là
/// <c>IValidateOptions&lt;T&gt;</c> chứ không phải <c>[Required]</c>.
///
/// <para>Bộ integration test ở <c>Production/ProductionHostTests.cs</c> canh việc host THẬT không
/// boot được khi thiếu cấu hình; bộ này canh chính cái luật đó ở mức đơn vị, gồm cả nhánh mà một
/// host Production không thể hiện được (Development vẫn phải bỏ trống được).</para>
/// </summary>
public class StorageOptionsValidatorTests
{
    private static StorageOptionsValidator ValidatorFor(string environmentName) =>
        new(new StubEnvironment(environmentName));

    [Fact(DisplayName = "Production + RootPath rỗng → FAIL, nêu đích danh Storage__RootPath")]
    public void Production_WithoutRootPath_Fails()
    {
        var result = ValidatorFor(Environments.Production).Validate(null, new StorageOptions { RootPath = null });

        Assert.True(result.Failed);
        Assert.Contains("Storage__RootPath", result.FailureMessage, StringComparison.Ordinal);
    }

    [Theory(DisplayName = "Ngoài Production + RootPath rỗng → OK (mặc định {ContentRootPath}/App_Data)")]
    [InlineData("Development")]
    [InlineData("Staging")]
    public void NonProduction_WithoutRootPath_Succeeds(string environmentName)
    {
        Assert.True(ValidatorFor(environmentName).Validate(null, new StorageOptions()).Succeeded);
    }

    /// <summary>
    /// Áp ở MỌI môi trường, kể cả Development: đường dẫn tương đối phân giải theo thư mục làm việc
    /// của tiến trình, thứ khác nhau giữa <c>dotnet run</c>, một service và một container — nên
    /// cùng một dòng cấu hình trỏ vào ba chỗ khác nhau mà không có gì báo.
    /// </summary>
    [Theory(DisplayName = "RootPath TƯƠNG ĐỐI → FAIL ở mọi môi trường")]
    [InlineData("Development")]
    [InlineData("Production")]
    public void RelativeRootPath_Fails(string environmentName)
    {
        var result = ValidatorFor(environmentName)
            .Validate(null, new StorageOptions { RootPath = "App_Data" });

        Assert.True(result.Failed);
        Assert.Contains("TUYỆT ĐỐI", result.FailureMessage, StringComparison.Ordinal);
    }

    [Theory(DisplayName = "RootPath TUYỆT ĐỐI → OK ở mọi môi trường")]
    [InlineData("Development")]
    [InlineData("Production")]
    public void AbsoluteRootPath_Succeeds(string environmentName)
    {
        var absolute = Path.Combine(Path.GetTempPath(), "pm-storage-hop-le");

        Assert.True(ValidatorFor(environmentName)
            .Validate(null, new StorageOptions { RootPath = absolute }).Succeeded);
    }

    private sealed class StubEnvironment(string environmentName) : IHostEnvironment
    {
        public string ApplicationName { get; set; } = "PlatformManager.Tests";
        public string EnvironmentName { get; set; } = environmentName;
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}

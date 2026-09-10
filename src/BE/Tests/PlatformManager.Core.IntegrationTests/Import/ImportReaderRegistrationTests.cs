using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using PlatformManager.Core.Application.Import;
using PlatformManager.Core.Infrastructure.Import;
using Xunit;

namespace PlatformManager.Core.IntegrationTests.Import;

/// <summary>
/// Nối dây DI của bộ máy import. Bộ test đơn vị ở <c>Core.UnitTests/Import/</c> dựng reader bằng
/// <c>new</c> nên không test nào trong số đó đỏ khi host quên đăng ký — mà "quên đăng ký" ở đây có
/// hậu quả rất cụ thể: <see cref="IImportFileReaderSelector"/> nhận một tập rỗng và MỌI file đều bị
/// từ chối là <see cref="ImportFileRejection.UnsupportedFormat"/>, kể cả file hoàn toàn hợp lệ.
/// Không có exception nào, chỉ có một chức năng im lặng không dùng được.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class ImportReaderRegistrationTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;

    public ImportReaderRegistrationTests(PostgresFixture fixture)
    {
        IntegrationTestHostEnvironment.Configure(fixture.ConnectionString);
        _factory = new WebApplicationFactory<Program>();
    }

    public void Dispose() => _factory.Dispose();

    [Fact(DisplayName = "Host đăng ký đủ 3 reader (CSV · XLSX · XLS) thành MỘT tập")]
    public void AllThreeReaders_AreRegistered()
    {
        using var scope = _factory.Services.CreateScope();

        var readers = scope.ServiceProvider.GetRequiredService<IEnumerable<IImportFileReader>>().ToList();

        Assert.Contains(readers, reader => reader is CsvImportFileReader);
        Assert.Contains(readers, reader => reader is XlsxImportFileReader);
        Assert.Contains(readers, reader => reader is XlsImportFileReader);

        // Trùng đăng ký không làm sai kết quả (selector lấy cái đầu khớp) nhưng là dấu hiệu ai đó
        // vừa đăng ký lần thứ hai ở một chỗ khác — và lần sau sẽ là một reader khác.
        Assert.Equal(readers.Count, readers.Select(reader => reader.GetType()).Distinct().Count());
    }

    [Fact(DisplayName = "Selector phân giải được và nhận đúng file .csv qua đồ hình DI thật")]
    public void Selector_IsResolvable_AndWorksEndToEnd()
    {
        using var scope = _factory.Services.CreateScope();
        var selector = scope.ServiceProvider.GetRequiredService<IImportFileReaderSelector>();

        using var stream = new MemoryStream("Mã,Tên\r\nA1,X\r\n"u8.ToArray());

        var selection = selector.Select(stream, "bao-cao.csv");

        // Chặn "xanh rỗng": phân giải được service mà tập reader rỗng thì mọi file đều bị từ chối,
        // và một khẳng định "không ném" sẽ vẫn xanh.
        Assert.True(selection.IsAccepted,
            $"Selector từ chối một file .csv hợp lệ với lý do {selection.Rejection} ⇒ nhiều khả năng " +
            "tập IImportFileReader trong DI đang rỗng.");
        Assert.IsType<CsvImportFileReader>(selection.Reader);
    }

    [Fact(DisplayName = "Trần dung lượng ở host là 10 MB (mặc định, không cấu hình đè)")]
    public void Cap_IsTenMegabytes()
    {
        using var scope = _factory.Services.CreateScope();
        var selector = scope.ServiceProvider.GetRequiredService<IImportFileReaderSelector>();

        using var stream = new MemoryStream("Mã\r\n"u8.ToArray());

        Assert.Equal(10L * 1024 * 1024, selector.Select(stream, "bao-cao.csv").MaxFileSizeBytes);
    }
}

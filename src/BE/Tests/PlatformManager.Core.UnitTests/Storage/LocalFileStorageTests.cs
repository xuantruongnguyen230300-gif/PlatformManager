using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using PlatformManager.Core.Application.Storage;
using PlatformManager.Core.Infrastructure.Storage;
using Xunit;

namespace PlatformManager.Core.UnitTests.Storage;

/// <summary>
/// <see cref="LocalFileStorage"/> trên một thư mục tạm THẬT — không giả lập hệ thống tệp.
///
/// <para>Phần lớn giá trị của lớp này nằm ở những thứ chỉ hệ thống tệp thật mới thể hiện: đường dẫn
/// phân giải ra ngoài gốc kho, thư mục chưa tồn tại, file bị xoá. Một lớp trừu tượng hoá hệ thống
/// tệp trong test sẽ tự định nghĩa lại chính những hành vi đang cần đo.</para>
/// </summary>
public sealed class LocalFileStorageTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"pm-storage-{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    private LocalFileStorage Storage(string? rootPath = null) =>
        new(Options.Create(new StorageOptions { RootPath = rootPath ?? _root }), new FakeHostEnvironment(_root));

    private static MemoryStream Content(string text = "nội dung") =>
        new(System.Text.Encoding.UTF8.GetBytes(text));

    private async Task<string> SaveAsync(
        FileStorageArea area = FileStorageArea.Upload,
        string feature = "danh-muc",
        string fileName = "bao-cao.csv",
        string text = "nội dung")
    {
        using var content = Content(text);
        return await Storage().SaveAsync(area, feature, Guid.NewGuid(), fileName, content, CancellationToken.None);
    }

    // ───────────────────────────── Khoá lưu trữ ─────────────────────────────

    /// <summary>
    /// Khoá phải TƯƠNG ĐỐI. Đây là điểm khác quan trọng nhất so với bản cũ (trả đường dẫn tuyệt
    /// đối): khoá đi vào DB, và một đường dẫn tuyệt đối nằm trong DB làm mọi bản ghi cũ chết ngay
    /// lần đầu đổi <c>Storage:RootPath</c> — mà đổi gốc kho chính là việc phải làm khi chuyển sang
    /// volume/mount.
    /// </summary>
    [Fact(DisplayName = "SaveAsync trả khoá TƯƠNG ĐỐI dạng uploads/<phân-hệ>/<id>.<ext>, dùng dấu '/'")]
    public async Task SaveAsync_ReturnsRelativeKey()
    {
        var fileId = Guid.NewGuid();
        using var content = Content();

        var key = await Storage().SaveAsync(
            FileStorageArea.Upload, "danh-muc", fileId, "Báo Cáo.CSV", content, CancellationToken.None);

        Assert.Equal($"uploads/danh-muc/{fileId:D}.csv", key);
        Assert.False(Path.IsPathRooted(key));
        Assert.DoesNotContain('\\', key);
    }

    [Fact(DisplayName = "Khu Export dùng thư mục 'exports' (vòng đời khác Upload, không dùng chung chỗ)")]
    public async Task ExportArea_UsesExportsFolder()
    {
        var key = await SaveAsync(FileStorageArea.Export);

        Assert.StartsWith("exports/", key, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Tên file thật trên đĩa theo id, KHÔNG theo tên file người dùng đặt")]
    public async Task FileOnDisk_IsNamedById_NotByOriginalName()
    {
        var fileId = Guid.NewGuid();
        using var content = Content();

        await Storage().SaveAsync(
            FileStorageArea.Upload, "danh-muc", fileId, "bao-cao.csv", content, CancellationToken.None);

        var files = Directory.GetFiles(Path.Combine(_root, "uploads", "danh-muc"));

        Assert.Equal($"{fileId:D}.csv", Path.GetFileName(Assert.Single(files)));
    }

    [Theory(DisplayName = "Phần mở rộng lạ bị BỎ, không làm hỏng lượt lưu")]
    [InlineData("khong-co-duoi")]
    [InlineData("ten.qua-nhieu-ky-tu-cho-mot-duoi")]
    [InlineData("ten.")]
    public async Task StrangeExtension_IsDropped_WithoutThrowing(string fileName)
    {
        var fileId = Guid.NewGuid();
        using var content = Content();

        var key = await Storage().SaveAsync(
            FileStorageArea.Upload, "danh-muc", fileId, fileName, content, CancellationToken.None);

        Assert.Equal($"uploads/danh-muc/{fileId:D}", key);
    }

    // ───────────────────────────── Vòng đời file ─────────────────────────────

    [Fact(DisplayName = "Lưu rồi mở lại → đúng nội dung; thư mục con được tạo tự động")]
    public async Task SaveThenOpenRead_RoundTrips()
    {
        var key = await SaveAsync(text: "Tự đánh giá 7,04");

        await using var stream = await Storage().OpenReadAsync(key, CancellationToken.None);
        using var reader = new StreamReader(stream);

        Assert.Equal("Tự đánh giá 7,04", await reader.ReadToEndAsync());
    }

    [Fact(DisplayName = "Lưu đè cùng id → nội dung MỚI, không nhân đôi file (job chạy lại được)")]
    public async Task SaveTwiceWithSameId_Overwrites()
    {
        var fileId = Guid.NewGuid();
        var storage = Storage();

        using (var first = Content("lần một"))
            await storage.SaveAsync(FileStorageArea.Upload, "danh-muc", fileId, "a.csv", first, CancellationToken.None);

        using (var second = Content("lần hai"))
            await storage.SaveAsync(FileStorageArea.Upload, "danh-muc", fileId, "a.csv", second, CancellationToken.None);

        var files = Directory.GetFiles(Path.Combine(_root, "uploads", "danh-muc"));
        Assert.Single(files);
        Assert.Equal("lần hai", await File.ReadAllTextAsync(files[0]));
    }

    [Fact(DisplayName = "ExistsAsync: true sau khi lưu, false sau khi xoá")]
    public async Task Exists_TracksFileLifecycle()
    {
        var key = await SaveAsync();
        var storage = Storage();

        Assert.True(await storage.ExistsAsync(key, CancellationToken.None));
        Assert.True(await storage.DeleteAsync(key, CancellationToken.None));
        Assert.False(await storage.ExistsAsync(key, CancellationToken.None));
    }

    /// <summary>Dọn dẹp phải chạy lại được nhiều lần — job dọn định kỳ sẽ gặp lại chính những khoá
    /// nó vừa xoá nếu bản ghi DB chưa kịp cập nhật.</summary>
    [Fact(DisplayName = "DeleteAsync trên file đã mất → false, KHÔNG ném (dọn chạy lại được)")]
    public async Task Delete_IsIdempotent()
    {
        var key = await SaveAsync();
        var storage = Storage();

        Assert.True(await storage.DeleteAsync(key, CancellationToken.None));
        Assert.False(await storage.DeleteAsync(key, CancellationToken.None));
    }

    /// <summary>
    /// "Đường dẫn chết" là ca CÓ THẬT, không phải lý thuyết: kho file không đi qua backup của
    /// database, nên khôi phục DB về mốc cũ không mang file quay lại. Thông điệp phải nói ra điều
    /// đó thay vì để người đọc tưởng code hỏng.
    /// </summary>
    [Fact(DisplayName = "OpenReadAsync trên khoá chết → FileNotFoundException")]
    public async Task OpenRead_OnMissingFile_Throws()
    {
        await Assert.ThrowsAsync<FileNotFoundException>(
            () => Storage().OpenReadAsync("uploads/danh-muc/khong-ton-tai.csv", CancellationToken.None));
    }

    // ───────────────────────────── Chặn thoát khỏi gốc kho ─────────────────────────────

    /// <summary>
    /// <paramref name="feature"/> đi thẳng vào đường dẫn trên đĩa. Hôm nay mọi nơi gọi đều truyền
    /// hằng số, nhưng "hôm nay" không phải một bảo đảm — và hậu quả nếu lọt là ghi file ra ngoài
    /// kho, ở một chỗ do người gọi chỉ định.
    /// </summary>
    [Theory(DisplayName = "Tên phân hệ nguy hiểm → ArgumentException, KHÔNG ghi file nào")]
    [InlineData("../../..")]
    [InlineData("danh/muc")]
    [InlineData("danh\\muc")]
    [InlineData("C:")]
    [InlineData("")]
    [InlineData("Danh-Muc")] // chữ hoa: một tên thư mục khác trên Linux, cùng thư mục trên Windows
    public async Task DangerousFeature_IsRejected(string feature)
    {
        using var content = Content();

        await Assert.ThrowsAsync<ArgumentException>(() => Storage().SaveAsync(
            FileStorageArea.Upload, feature, Guid.NewGuid(), "a.csv", content, CancellationToken.None));
    }

    /// <summary>
    /// Khoá lưu trữ do <c>BuildKey</c> sinh ra, NHƯNG nó đi qua DB rồi quay lại — và mọi giá trị đi
    /// qua DB đều có thể bị sửa bằng một đường ghi khác. Không có phép kiểm này,
    /// <c>OpenReadAsync</c> là một máy đọc file tuỳ ý trên máy chủ.
    /// </summary>
    [Theory(DisplayName = "Khoá phân giải ra NGOÀI gốc kho → ArgumentException (không thành máy đọc file tuỳ ý)")]
    [InlineData("../appsettings.json")]
    [InlineData("uploads/../../appsettings.json")]
    [InlineData("")]
    public async Task KeyEscapingRoot_IsRejected(string key)
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => Storage().OpenReadAsync(key, CancellationToken.None));
    }

    /// <summary>
    /// Phép so tiền tố phải tính cả DẤU PHÂN CÁCH. Không có nó, gốc kho <c>…/kho</c> cũng "chứa"
    /// <c>…/kho-cu</c> — một khoá <c>../kho-cu/bi-mat</c> sẽ qua được phép kiểm và đọc file của một
    /// thư mục hàng xóm.
    /// </summary>
    [Fact(DisplayName = "Khoá trỏ sang thư mục hàng xóm CÙNG TIỀN TỐ TÊN → vẫn bị từ chối")]
    public async Task SiblingDirectoryWithSharedPrefix_IsRejected()
    {
        var neighbour = $"{Path.GetFileName(_root)}-hang-xom";

        await Assert.ThrowsAsync<ArgumentException>(
            () => Storage().OpenReadAsync($"../{neighbour}/bi-mat.txt", CancellationToken.None));
    }

    [Fact(DisplayName = "Khoá TUYỆT ĐỐI bị từ chối (khoá phải tương đối theo hợp đồng)")]
    public async Task AbsoluteKey_IsRejected()
    {
        var absolute = Path.Combine(_root, "uploads", "danh-muc", "a.csv");

        await Assert.ThrowsAsync<ArgumentException>(
            () => Storage().OpenReadAsync(absolute, CancellationToken.None));
    }

    /// <summary>
    /// Đối chứng cho hai ca trên: chứng minh phép kiểm KHÔNG chặn nhầm khoá hợp lệ. Thiếu ca này
    /// thì một bản cài từ chối MỌI khoá vẫn xanh ở cả hai ca chặn.
    /// </summary>
    [Fact(DisplayName = "Đối chứng: khoá hợp lệ KHÔNG bị phép kiểm chặn nhầm")]
    public async Task ValidKey_IsNotRejected()
    {
        var key = await SaveAsync();

        Assert.True(await Storage().ExistsAsync(key, CancellationToken.None));
    }

    // ───────────────────────────── Gốc kho ─────────────────────────────

    /// <summary>
    /// Bỏ trống <c>Storage:RootPath</c> ⇒ <c>{ContentRootPath}/App_Data</c>. Hợp lệ ngoài Production;
    /// ở Production <c>StorageOptionsValidator</c> chặn ngay lúc khởi động.
    /// </summary>
    [Fact(DisplayName = "RootPath bỏ trống → {ContentRootPath}/App_Data")]
    public async Task BlankRootPath_FallsBackToAppData()
    {
        var storage = new LocalFileStorage(
            Options.Create(new StorageOptions { RootPath = null }), new FakeHostEnvironment(_root));

        var fileId = Guid.NewGuid();
        using var content = Content();
        await storage.SaveAsync(
            FileStorageArea.Upload, "danh-muc", fileId, "a.csv", content, CancellationToken.None);

        Assert.True(File.Exists(Path.Combine(_root, "App_Data", "uploads", "danh-muc", $"{fileId:D}.csv")));
    }

    private sealed class FakeHostEnvironment(string contentRootPath) : IHostEnvironment
    {
        public string ApplicationName { get; set; } = "PlatformManager.Tests";
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ContentRootPath { get; set; } = contentRootPath;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}

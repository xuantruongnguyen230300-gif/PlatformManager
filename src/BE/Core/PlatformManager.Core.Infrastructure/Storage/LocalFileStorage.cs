using System.Text.RegularExpressions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using PlatformManager.Core.Application.Storage;

namespace PlatformManager.Core.Infrastructure.Storage;

/// <summary>
/// <see cref="IFileStorage"/> trên hệ thống tệp cục bộ (hoặc một volume/mount gắn vào).
///
/// <para>Đổi sang S3/Blob là đổi ĐÚNG lớp này — mọi nơi gọi chỉ thấy khoá lưu trữ tương đối, không
/// thấy đường dẫn thật. Ngưỡng đổi (doc/huong_dan/wiki-core/be/14-file-storage.md §8): khi chạy ≥2
/// instance THẬT; một volume dùng chung giải quyết được đa số, và đổi hạ tầng lưu trữ chỉ để "cho
/// chuẩn" là đổi chi phí vận hành lấy không gì.</para>
/// </summary>
public sealed partial class LocalFileStorage : IFileStorage
{
    private readonly string _root;

    public LocalFileStorage(IOptions<StorageOptions> options, IHostEnvironment environment)
    {
        var configured = options.Value.RootPath;

        // Bỏ trống ⇒ {ContentRootPath}/App_Data. Chỉ hợp lệ ngoài Production —
        // StorageOptionsValidator chặn ca Production ngay lúc khởi động.
        _root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(
            string.IsNullOrWhiteSpace(configured)
                ? Path.Combine(environment.ContentRootPath, "App_Data")
                : configured));
    }

    public async Task<string> SaveAsync(
        FileStorageArea area,
        string feature,
        Guid fileId,
        string originalFileName,
        Stream content,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(content);

        var storageKey = BuildKey(area, feature, fileId, originalFileName);
        var fullPath = ResolveFullPath(storageKey);

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        // FileMode.Create (ghi đè) chứ không CreateNew: một job nền chạy lại sau lỗi tạm thời sẽ
        // ghi lại đúng file đó, và "file đã tồn tại" ở đó không phải lỗi.
        await using var target = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await content.CopyToAsync(target, ct);

        return storageKey;
    }

    public Task<Stream> OpenReadAsync(string storageKey, CancellationToken ct)
    {
        var fullPath = ResolveFullPath(storageKey);

        if (!File.Exists(fullPath))
            throw new FileNotFoundException(
                $"Không còn file nào ở khoá lưu trữ '{storageKey}'. Kho file KHÔNG đi qua backup của " +
                "database, nên khôi phục DB về mốc cũ không mang file quay lại — xem " +
                "doc/huong_dan/wiki-core/be/14-file-storage.md §6.",
                fullPath);

        return Task.FromResult<Stream>(
            new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read));
    }

    public Task<bool> DeleteAsync(string storageKey, CancellationToken ct)
    {
        var fullPath = ResolveFullPath(storageKey);

        if (!File.Exists(fullPath))
            return Task.FromResult(false);

        File.Delete(fullPath);
        return Task.FromResult(true);
    }

    public Task<bool> ExistsAsync(string storageKey, CancellationToken ct) =>
        Task.FromResult(File.Exists(ResolveFullPath(storageKey)));

    // ───────────────────────────── Dựng và phân giải khoá ─────────────────────────────

    /// <summary>Thư mục cấp một theo khu — tên cố định, khai một chỗ.</summary>
    private static string AreaFolder(FileStorageArea area) => area switch
    {
        FileStorageArea.Upload => "uploads",
        FileStorageArea.Export => "exports",
        _ => throw new ArgumentOutOfRangeException(nameof(area), area, "Khu file chưa được hỗ trợ."),
    };

    private static string BuildKey(FileStorageArea area, string feature, Guid fileId, string originalFileName)
    {
        GuardFeature(feature);

        // Dấu '/' cố định trong khoá, KHÔNG dùng Path.DirectorySeparatorChar: khoá được lưu vào DB
        // và có thể được đọc lại trên một hệ điều hành khác. Một khoá mang '\' ghi từ Windows sẽ
        // thành một tên file có dấu gạch chéo ngược trên Linux, không phải một thư mục con.
        return $"{AreaFolder(area)}/{feature}/{fileId:D}{SafeExtension(originalFileName)}";
    }

    /// <summary>
    /// <paramref name="feature"/> đi thẳng vào đường dẫn nên nó là một đầu vào NGUY HIỂM, kể cả khi
    /// hôm nay mọi nơi gọi đều truyền hằng số: một giá trị như <c>"../../.."</c> ghi file ra ngoài
    /// kho. Chặn ở đây thay vì tin vào từng nơi gọi.
    /// </summary>
    private static void GuardFeature(string feature)
    {
        if (!FeatureSegment().IsMatch(feature))
            throw new ArgumentException(
                $"Tên phân hệ '{feature}' không hợp lệ: chỉ chữ thường, số và dấu gạch ngang, dài tối đa " +
                "64 ký tự, không bắt đầu/kết thúc bằng gạch ngang. Giá trị này đi thẳng vào đường dẫn " +
                "trên đĩa.", nameof(feature));
    }

    /// <summary>
    /// Chỉ giữ phần mở rộng nếu nó "trông như" phần mở rộng. Không ném với tên file lạ: tên file do
    /// người dùng đặt, và làm hỏng cả lượt upload vì một cái tên là phản ứng quá tay — định dạng
    /// thật vẫn được nhận theo NỘI DUNG lúc đọc (<c>IImportFileReader.CanRead</c>), không theo đuôi.
    /// </summary>
    private static string SafeExtension(string originalFileName)
    {
        var extension = Path.GetExtension(originalFileName ?? string.Empty);
        return SafeExtensionPattern().IsMatch(extension) ? extension.ToLowerInvariant() : string.Empty;
    }

    /// <summary>
    /// Khoá tương đối → đường dẫn tuyệt đối, và <b>chứng minh nó vẫn nằm trong kho</b>.
    ///
    /// <para>Phép kiểm này là phòng thủ theo tầng, không thừa: khoá lưu trữ tuy do
    /// <see cref="BuildKey"/> sinh ra, nhưng nó đi qua DB rồi quay lại — và mọi giá trị đi qua DB
    /// đều có thể bị sửa bằng một đường ghi khác. Không có nó, một khoá
    /// <c>"../../appsettings.json"</c> biến <see cref="OpenReadAsync"/> thành một máy đọc file tuỳ ý.</para>
    /// </summary>
    private string ResolveFullPath(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
            throw new ArgumentException("Khoá lưu trữ rỗng.", nameof(storageKey));

        if (Path.IsPathRooted(storageKey))
            throw new ArgumentException(
                $"Khoá lưu trữ phải là đường dẫn TƯƠNG ĐỐI, đang nhận '{storageKey}'.", nameof(storageKey));

        var fullPath = Path.GetFullPath(Path.Combine(_root, storageKey));

        // Nối dấu phân cách vào gốc rồi mới so tiền tố: thiếu nó thì gốc "/data/kho" cũng khớp
        // "/data/kho-cu/...". Kiểm sẵn dấu phân cách cuối để gốc là thư mục gốc ổ đĩa ("C:\") không
        // thành "C:\\" và từ chối mọi khoá hợp lệ.
        var rootPrefix = _root.EndsWith(Path.DirectorySeparatorChar)
            ? _root
            : _root + Path.DirectorySeparatorChar;

        if (!fullPath.StartsWith(rootPrefix, StringComparison.Ordinal))
            throw new ArgumentException(
                $"Khoá lưu trữ '{storageKey}' phân giải ra ngoài gốc kho. Từ chối để một khoá bị sửa " +
                "không biến kho file thành đường đọc/ghi file tuỳ ý trên máy chủ.", nameof(storageKey));

        return fullPath;
    }

    [GeneratedRegex(@"^[a-z0-9](?:[a-z0-9-]{0,62}[a-z0-9])?$", RegexOptions.CultureInvariant)]
    private static partial Regex FeatureSegment();

    [GeneratedRegex(@"^\.[A-Za-z0-9]{1,10}$", RegexOptions.CultureInvariant)]
    private static partial Regex SafeExtensionPattern();
}

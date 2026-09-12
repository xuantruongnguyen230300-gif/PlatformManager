using System.Text.RegularExpressions;
using Xunit;

namespace PlatformManager.ArchTests;

/// <summary>
/// <b>Hai luật về SỰ ĐƯỢC NỐI VÀO của hai seam mà "quên dùng" là hỏng IM LẶNG.</b> Cùng họ với
/// <see cref="MiddlewareWiringTests"/>: không kiểm hình dạng, kiểm việc thứ đã viết ra có thật sự
/// được dùng không.
///
/// <para>Cả hai luật ở đây đến từ <c>core-reviewer</c> ngày 2026-09-11 (finding F3 và F6). Điểm
/// chung của chúng: <b>vi phạm vẫn biên dịch sạch, mọi test hiện có vẫn xanh</b>, và hậu quả chỉ
/// lộ ra ở dữ liệu sai trong database.</para>
/// </summary>
public class ImportSeamUsageTests
{
    // ─────────────────────────── F3 — trần SỐ DÒNG ───────────────────────────

    /// <summary>
    /// Gọi thẳng <c>Reader.ReadAsync</c> = đọc file mà KHÔNG áp trần số dòng.
    ///
    /// <para>Bắt cả <c>selection.Reader.ReadAsync</c> lẫn <c>selection.Reader!.ReadAsync</c>, và cả
    /// khi đặt reader ra biến trung gian rồi mới gọi (<c>var r = selection.Reader; r.ReadAsync</c>)
    /// — dạng cuối là dạng người ta viết khi "dọn cho gọn".</para>
    /// </summary>
    private static readonly Regex RawReaderRead = new(
        @"\.Reader\s*!?\s*\.\s*ReadAsync\s*\(", RegexOptions.Compiled);

    /// <summary>
    /// Chỗ DUY NHẤT được phép gọi reader thô: chính đường đọc đã áp trần của Core.
    /// </summary>
    private const string EnforcedReadPathFile = "ImportFileReaderSelectionExtensions.cs";

    [Fact(DisplayName = "Không mã sản phẩm nào gọi thẳng Reader.ReadAsync — phải đi qua ReadRowsAsync đã áp trần")]
    public void NoProductCode_CallsRawReaderReadAsync()
    {
        var files = RepoSourceTree.AllProductSourceFilesInTree();

        // Chặn "xanh mà không đo gì": tập file rỗng ⇒ "không ai vi phạm".
        Assert.True(files.Count > 0,
            $"Không tìm thấy file nguồn sản phẩm nào dưới '{RepoSourceTree.BackendRoot}' ⇒ test này không đo gì.");

        var violations = files
            .Where(path => !Path.GetFileName(path).Equals(EnforcedReadPathFile, StringComparison.Ordinal))
            .Where(path => RawReaderRead.IsMatch(RepoSourceTree.ReadCodeWithoutComments(path)))
            .Select(RepoSourceTree.Relative)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();

        Assert.True(violations.Count == 0,
            "Gọi THẲNG IImportFileReader.ReadAsync ⇒ BỎ QUA trần số dòng (Import:MaxRows, Q75): " +
            string.Join(", ", violations) + ". " +
            "Trần dung lượng được ép bằng CẤU TRÚC (vượt trần ⇒ Reader = null ⇒ nổ ngay), còn trần số " +
            "dòng thì chỉ ép được bằng việc đi qua ImportFileReaderSelectionExtensions.ReadRowsAsync — " +
            "gọi thẳng reader là bỏ qua nó mà vẫn biên dịch sạch, test xanh, và trần biến mất IM LẶNG " +
            "(finding F3, 2026-09-11). Sửa: đổi sang `selection.ReadRowsAsync(stream, fileName, ct)` rồi " +
            "bắt ImportRowLimitExceededException và ánh xạ sang ErrorDescriptor của nghiệp vụ mình.");
    }

    /// <summary>Đối chứng: bộ dò phải bắt được CẢ BA dạng viết, và không bắt nhầm lời gọi hợp lệ.</summary>
    [Fact(DisplayName = "Đối chứng: bộ dò bắt đủ ba dạng gọi reader thô, bỏ qua ReadRowsAsync")]
    public void RawReaderDetector_CatchesEveryShape()
    {
        Assert.Matches(RawReaderRead, "await foreach (var r in selection.Reader.ReadAsync(s, f, ct))");
        Assert.Matches(RawReaderRead, "await foreach (var r in selection.Reader!.ReadAsync(s, f, ct))");
        Assert.True(RawReaderRead.IsMatch("var reader = x.Reader; await reader.ReadAsync(s, f, ct);")
                    || RawReaderRead.IsMatch("x.Reader . ReadAsync(s, f, ct)"),
            "Bộ dò không bắt dạng tách biến/tách khoảng trắng ⇒ lách qua luật bằng một lần refactor vô hại.");

        Assert.DoesNotMatch(RawReaderRead, "await foreach (var r in selection.ReadRowsAsync(s, f, ct))");
    }

    // ─────────────────────────── F6 — JsonConverterFactory ───────────────────────────

    /// <summary>
    /// Mọi <c>JsonConverterFactory</c> khai trong mã sản phẩm phải được NỐI vào một
    /// <c>JsonSerializerOptions</c>.
    ///
    /// <para><b>Lỗi thật nó bắt (finding F6, 2026-09-11):</b> <c>AssignedJsonConverterFactory</c> có
    /// đúng MỘT dòng nối ở <c>Program.cs</c> và không test nào canh nó. Gỡ dòng đó ⇒ mọi khoá CÓ MẶT
    /// trong thân request đọc ra <c>Assigned&lt;T&gt;.Unset</c> ⇒ <b>mọi lệnh ghi âm thầm thành
    /// "không gửi gì cả"</b>: dialog lưu xong mà không trường nào đổi, và không có lỗi nào cả.</para>
    /// </summary>
    [Fact(DisplayName = "Mọi JsonConverterFactory của mã sản phẩm đều được nối vào Converters.Add")]
    public void EveryJsonConverterFactory_IsWiredInto_SerializerOptions()
    {
        var factories = ProductAssemblies.AllLoadableTypes()
            .Where(type => type is { IsAbstract: false, IsClass: true }
                           && typeof(System.Text.Json.Serialization.JsonConverterFactory).IsAssignableFrom(type))
            .Select(type => type.Name)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        // Chặn "xanh mà không đo gì" — reflection trả rỗng thì mọi assert dưới đây vô nghĩa. Hôm nay
        // repo có đúng một factory; con số không chép vào đây (.claude/CLAUDE.md §6), chỉ cần > 0.
        Assert.True(factories.Count > 0,
            "Không tìm thấy JsonConverterFactory nào trong mã sản phẩm ⇒ test này không đo gì. Nếu factory " +
            "đã bị xoá thật thì xoá luôn test này; nếu không, kiểm ProductAssemblies.All.");

        // Đọc TOÀN BỘ cây nguồn sản phẩm chứ không riêng Program.cs: nơi nối converter là quyết định
        // của composition root, và composition root có thể tách file.
        var sources = RepoSourceTree.AllProductSourceFilesInTree()
            .Select(RepoSourceTree.ReadCodeWithoutComments)
            .ToList();

        var notWired = factories
            .Where(name => !sources.Any(code =>
                Regex.IsMatch(code, @"Converters\s*\.\s*Add\s*\(\s*new\s+" + Regex.Escape(name))))
            .ToList();

        Assert.True(notWired.Count == 0,
            "JsonConverterFactory được KHAI nhưng KHÔNG được nối vào Converters.Add ở đâu cả: " +
            string.Join(", ", notWired) + ". " +
            "Một converter không được nối thì bộ đọc JSON dùng hành vi mặc định, và với Assigned<T> hành " +
            "vi mặc định là ĐỌC RA Unset cho mọi khoá — tức mọi lệnh ghi âm thầm thành \"không gửi gì " +
            "cả\" (finding F6, 2026-09-11). Nối ở Program.cs, trong AddJsonOptions.");
    }
}

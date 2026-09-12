using Microsoft.Extensions.Options;
using PlatformManager.Core.Application.Import;
using PlatformManager.Core.Infrastructure.Import;
using Xunit;

namespace PlatformManager.Core.UnitTests.Import;

/// <summary>
/// <see cref="ImportFileReaderSelector"/> — chọn reader theo NỘI DUNG file và chặn trần dung lượng.
///
/// <para>Dùng đúng ba reader thật (không giả lập): thứ đang đo là "ba bộ nhận diện này có phân
/// loại đúng nhau không", mà một tập reader giả thì luôn phân loại đúng theo định nghĩa của chính
/// nó.</para>
/// </summary>
public class ImportFileReaderSelectorTests
{
    private const long TenMegabytes = 10L * 1024 * 1024;

    private const int DefaultMaxRows = 20_000;

    private static ImportFileReaderSelector Selector(long maxBytes = TenMegabytes, int maxRows = DefaultMaxRows) =>
        new(
            [new CsvImportFileReader(), new XlsxImportFileReader(), new XlsImportFileReader()],
            Options.Create(new ImportOptions { MaxFileSizeBytes = maxBytes, MaxRows = maxRows }));

    private static MemoryStream RealXlsx() => ImportTestFiles.Xlsx(sheet => sheet.WriteTextRow(0, "Mã"));

    private static MemoryStream RealXls() => ImportTestFiles.Xls(sheet => sheet.WriteTextRow(0, "Mã"));

    [Fact(DisplayName = "Trần mặc định của ImportOptions là 10 MB (chốt Q12b)")]
    public void DefaultCap_IsTenMegabytes()
    {
        Assert.Equal(TenMegabytes, new ImportOptions().MaxFileSizeBytes);
    }

    /// <summary>
    /// Đối xứng với <see cref="DefaultCap_IsTenMegabytes"/>. Con số 20.000 là ĐO RA chứ không chọn
    /// tròn (Q75): file thật 62 dòng ≈ 19 KB ⇒ ~314 byte/dòng ⇒ trần dung lượng 10 MB còn cho lọt
    /// ~33.000 dòng, nên trần dòng phải nằm DƯỚI con số đó mới thật sự chạy tới; cận dưới là ca lớn
    /// nhất còn hợp lý (~12.000). Phép tính đầy đủ ở doc/huong_dan/wiki-core/be/15-import-export.md §2.
    /// </summary>
    [Fact(DisplayName = "Trần SỐ DÒNG mặc định của ImportOptions là 20.000 (chốt Q75)")]
    public void DefaultRowCap_Is20000()
    {
        Assert.Equal(DefaultMaxRows, new ImportOptions().MaxRows);
    }

    /// <summary>
    /// 🔴 <b>Đây là dây nối mà finding F4 chỉ ra là KHÔNG AI CANH.</b> Gãy dây ⇒ <c>MaxRows</c> về
    /// <c>0</c> ⇒ <c>ImportRowLimitExceededException</c> ném ngay dòng đầu tiên ⇒ <b>mọi</b> file bị
    /// từ chối, mà không một test nào khi đó đỏ.
    ///
    /// <para>Kiểm ở CẢ BA nhánh <c>return</c> của bộ chọn: giá trị cấu hình phải đi ra nguyên vẹn
    /// kể cả khi lượt chọn bị TỪ CHỐI — bên gọi dựng câu lỗi từ chính con số đó.</para>
    /// </summary>
    [Fact(DisplayName = "MaxRows của cấu hình đi ra ở CẢ BA nhánh return của bộ chọn")]
    public void MaxRows_IsCarried_OnEveryBranch()
    {
        const int configured = 7;

        using var accepted = ImportTestFiles.Csv("Mã,Tên" + Environment.NewLine + "A1,X" + Environment.NewLine);
        Assert.Equal(configured, Selector(maxRows: configured).Select(accepted, "bao-cao.csv").MaxRows);

        using var unsupported = new MemoryStream([0x7F, 0x45, 0x4C, 0x46, 0x01, 0x02, 0x03, 0x04]);
        var rejectedFormat = Selector(maxRows: configured).Select(unsupported, "bao-cao.bin");
        Assert.Equal(ImportFileRejection.UnsupportedFormat, rejectedFormat.Rejection);
        Assert.Equal(configured, rejectedFormat.MaxRows);

        using var tooLarge = ImportTestFiles.Csv(new string('x', 4096));
        var rejectedSize = Selector(maxBytes: 16, maxRows: configured).Select(tooLarge, "bao-cao.csv");
        Assert.Equal(ImportFileRejection.FileTooLarge, rejectedSize.Rejection);
        Assert.Equal(configured, rejectedSize.MaxRows);
    }

    [Fact(DisplayName = ".csv → CsvImportFileReader")]
    public void CsvFile_SelectsCsvReader()
    {
        using var stream = ImportTestFiles.Csv("Mã,Tên\r\nA1,X\r\n");

        var selection = Selector().Select(stream, "bao-cao.csv");

        Assert.True(selection.IsAccepted);
        Assert.IsType<CsvImportFileReader>(selection.Reader);
        Assert.Equal(ImportFileRejection.None, selection.Rejection);
    }

    [Fact(DisplayName = ".xlsx → XlsxImportFileReader; .xls → XlsImportFileReader")]
    public void ExcelFiles_SelectMatchingReader()
    {
        using var xlsx = RealXlsx();
        using var xls = RealXls();
        var selector = Selector();

        Assert.IsType<XlsxImportFileReader>(selector.Select(xlsx, "bao-cao.xlsx").Reader);
        Assert.IsType<XlsImportFileReader>(selector.Select(xls, "bao-cao.xls").Reader);
    }

    /// <summary>
    /// <b>Lỗi §2a của doc/huong_dan/wiki-core/be/15-import-export.md — ca đắt nhất.</b> Bản cũ chọn
    /// bộ đọc theo <c>Path.GetExtension()</c>, nên người dùng đổi tên <c>bao-cao.xlsx</c> thành
    /// <c>.xls</c> nhận một exception khó hiểu thay vì một lượt import chạy được. Ba ca dưới đây đi
    /// đủ ba chiều đổi tên có thể xảy ra.
    /// </summary>
    [Fact(DisplayName = "§2a: file bị ĐỔI ĐUÔI vẫn về đúng reader (chọn theo nội dung)")]
    public void RenamedFiles_StillSelectCorrectReader()
    {
        var selector = Selector();

        using var xlsxNamedXls = RealXlsx();
        Assert.IsType<XlsxImportFileReader>(selector.Select(xlsxNamedXls, "thuc-ra-la-xlsx.xls").Reader);

        using var xlsNamedXlsx = RealXls();
        Assert.IsType<XlsImportFileReader>(selector.Select(xlsNamedXlsx, "thuc-ra-la-xls.xlsx").Reader);

        // Chiều nguy hiểm nhất: nhị phân mang đuôi .csv. Nếu bộ đọc CSV nhận, nó sẽ "đọc thành
        // công" ra dữ liệu rác — không exception, không log.
        using var xlsxNamedCsv = RealXlsx();
        Assert.IsType<XlsxImportFileReader>(selector.Select(xlsxNamedCsv, "thuc-ra-la-xlsx.csv").Reader);
    }

    [Fact(DisplayName = "File sai định dạng (chữ thường mang đuôi .xlsx) → UnsupportedFormat, Reader null")]
    public void UnknownContent_IsRejected()
    {
        using var stream = new MemoryStream("day khong phai file excel"u8.ToArray());

        var selection = Selector().Select(stream, "bao-cao.xlsx");

        Assert.False(selection.IsAccepted);
        Assert.Null(selection.Reader);
        Assert.Equal(ImportFileRejection.UnsupportedFormat, selection.Rejection);
    }

    [Fact(DisplayName = "File rỗng mang đuôi .csv vẫn được nhận (0 dòng, không phải lỗi định dạng)")]
    public void EmptyCsv_IsAccepted()
    {
        using var stream = new MemoryStream([]);

        Assert.IsType<CsvImportFileReader>(Selector().Select(stream, "rong.csv").Reader);
    }

    [Fact(DisplayName = "File rỗng mang đuôi .xlsx → UnsupportedFormat (không có chữ ký nào để nhận)")]
    public void EmptyXlsx_IsRejected()
    {
        using var stream = new MemoryStream([]);

        Assert.Equal(ImportFileRejection.UnsupportedFormat, Selector().Select(stream, "rong.xlsx").Rejection);
    }

    // ───────────────────────────── Trần dung lượng ─────────────────────────────

    [Fact(DisplayName = "Vượt trần → FileTooLarge, kèm CẢ dung lượng thật lẫn trần đang áp")]
    public void OversizedFile_IsRejected_WithBothNumbers()
    {
        using var stream = new MemoryStream(new byte[2048]);

        var selection = Selector(maxBytes: 1024).Select(stream, "qua-to.csv");

        Assert.False(selection.IsAccepted);
        Assert.Equal(ImportFileRejection.FileTooLarge, selection.Rejection);

        // Bên gọi cần cả hai con số để dựng câu lỗi tử tế — thiếu chúng thì mỗi nghiệp vụ lại tự
        // đo lại một lần, và sẽ có nơi đo bằng một trần khác.
        Assert.Equal(2048, selection.FileSizeBytes);
        Assert.Equal(1024, selection.MaxFileSizeBytes);
    }

    /// <summary>
    /// Trần kiểm TRƯỚC khi đọc byte nội dung nào (doc §2). Ca này chứng minh bằng một file có nội
    /// dung HỢP LỆ hoàn toàn: nó bị từ chối vì dung lượng, không phải vì không nhận ra định dạng —
    /// nếu thứ tự đảo lại, lý do trả về sẽ là <see cref="ImportFileRejection.UnsupportedFormat"/>
    /// hoặc file đã bị đọc mất một lượt.
    /// </summary>
    [Fact(DisplayName = "Vượt trần thì từ chối vì DUNG LƯỢNG, kể cả khi định dạng hoàn toàn hợp lệ")]
    public void CapIsCheckedBeforeFormat()
    {
        using var stream = ImportTestFiles.Csv("Mã,Tên\r\nA1,X\r\n");

        var selection = Selector(maxBytes: 1).Select(stream, "bao-cao.csv");

        Assert.Equal(ImportFileRejection.FileTooLarge, selection.Rejection);
    }

    [Fact(DisplayName = "Đúng bằng trần → vẫn nhận (chỉ VƯỢT mới bị chặn)")]
    public void ExactlyAtCap_IsAccepted()
    {
        using var stream = new MemoryStream(new byte[64]);

        Assert.True(Selector(maxBytes: 64).Select(stream, "vua-du.csv").IsAccepted);
    }

    // ───────────────────────────── Hợp đồng về stream ─────────────────────────────

    /// <summary>
    /// Chỗ dễ quên nhất của cả cơ chế: quên tua stream về đầu thì reader bắt đầu đọc từ byte thứ 8,
    /// và triệu chứng KHÔNG phải "lỗi đọc file" — nó là "file thiếu cột", vì đúng dòng header bị ăn
    /// mất một khúc.
    /// </summary>
    [Fact(DisplayName = "Sau khi chọn, stream đã được tua về ĐẦU (byte 0)")]
    public async Task Select_RewindsStream()
    {
        using var stream = ImportTestFiles.Csv("Mã,Tên\r\nA1,X\r\n");
        stream.Seek(3, SeekOrigin.Begin); // cố ý để con trỏ ở giữa file trước khi gọi

        var selection = Selector().Select(stream, "bao-cao.csv");

        Assert.Equal(0, stream.Position);

        // Và bằng chứng đi tới cùng: reader đọc lại được nguyên vẹn cả header lẫn dòng dữ liệu.
        var rows = new List<IReadOnlyDictionary<string, ImportCellValue>>();
        await foreach (var row in selection.Reader!.ReadAsync(stream, "bao-cao.csv", CancellationToken.None))
            rows.Add(row);

        Assert.Equal("A1", Assert.Single(rows)["Mã"].Text);
    }

    [Fact(DisplayName = "Stream không seek được → ArgumentException nêu cách sửa")]
    public void NonSeekableStream_Throws()
    {
        using var stream = new NonSeekableStream();

        var ex = Assert.Throws<ArgumentException>(() => Selector().Select(stream, "bao-cao.csv"));

        Assert.Contains("seek", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class NonSeekableStream : MemoryStream
    {
        public override bool CanSeek => false;
    }
}

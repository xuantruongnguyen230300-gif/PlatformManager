using NPOI.SS.UserModel;
using PlatformManager.Core.Application.Import;
using PlatformManager.Core.Infrastructure.Import;
using Xunit;

namespace PlatformManager.Core.UnitTests.Import;

/// <summary>
/// <see cref="XlsxImportFileReader"/> và <see cref="XlsImportFileReader"/>. Mỗi ca chạy trên CẢ HAI
/// định dạng qua <c>[Theory]</c>: hai lớp chỉ khác nhau ở lớp workbook của NPOI, nhưng "chỉ khác
/// mỗi chỗ đó" là điều cần được chứng minh chứ không phải giả định — <c>.xls</c> (HSSF) và
/// <c>.xlsx</c> (XSSF) là hai bản cài hoàn toàn tách rời trong NPOI.
/// </summary>
public class ExcelImportFileReaderTests
{
    public static TheoryData<string> Formats => ["xlsx", "xls"];

    private static ExcelImportFileReader ReaderFor(string format) =>
        format == "xlsx" ? new XlsxImportFileReader() : new XlsImportFileReader();

    private static MemoryStream Build(string format, Action<ISheet> build) =>
        format == "xlsx" ? ImportTestFiles.Xlsx(build) : ImportTestFiles.Xls(build);

    private static async Task<List<IReadOnlyDictionary<string, ImportCellValue>>> ReadAllAsync(
        string format, Action<ISheet> build)
    {
        using var stream = Build(format, build);
        var reader = ReaderFor(format);

        var rows = new List<IReadOnlyDictionary<string, ImportCellValue>>();
        await foreach (var row in reader.ReadAsync(stream, $"file.{format}", CancellationToken.None))
            rows.Add(row);
        return rows;
    }

    [Theory(DisplayName = "File hợp lệ → đúng số dòng, đúng tên cột, đúng giá trị")]
    [MemberData(nameof(Formats))]
    public async Task ValidFile_IsReadIntoRows(string format)
    {
        var rows = await ReadAllAsync(format, sheet =>
        {
            sheet.WriteTextRow(0, "Mã", "Tên");
            sheet.WriteTextRow(1, "A1", "Chỉ tiêu một");
            sheet.WriteTextRow(2, "A2", "Chỉ tiêu hai");
        });

        Assert.Equal(2, rows.Count);
        Assert.Equal("A1", rows[0]["Mã"].Text);
        Assert.Equal("Chỉ tiêu hai", rows[1]["Tên"].Text);
    }

    [Theory(DisplayName = "Sheet không có dòng nào → 0 dòng, KHÔNG ném")]
    [MemberData(nameof(Formats))]
    public async Task EmptySheet_YieldsNothing(string format)
    {
        Assert.Empty(await ReadAllAsync(format, _ => { }));
    }

    [Theory(DisplayName = "Chỉ có dòng header → 0 dòng, KHÔNG ném")]
    [MemberData(nameof(Formats))]
    public async Task HeaderOnly_YieldsNothing(string format)
    {
        Assert.Empty(await ReadAllAsync(format, sheet => sheet.WriteTextRow(0, "Mã", "Tên")));
    }

    [Theory(DisplayName = "Tên cột có khoảng trắng thừa → trim; cột header rỗng → bỏ")]
    [MemberData(nameof(Formats))]
    public async Task Headers_AreTrimmed_AndBlankOnesDropped(string format)
    {
        var row = Assert.Single(await ReadAllAsync(format, sheet =>
        {
            sheet.WriteTextRow(0, "  Mã  ", "", "Tên");
            sheet.WriteTextRow(1, "A1", "bỏ đi", "X");
        }));

        Assert.Equal(["Mã", "Tên"], row.Keys.Order(StringComparer.Ordinal));
        Assert.Equal("X", row["Tên"].Text);
    }

    /// <summary>
    /// §F8 (2026-09-09) — dòng TỒN TẠI VẬT LÝ nhưng toàn ô rỗng, đứng trước dòng tiêu đề. Đây là ca
    /// người dùng chèn một dòng ở đầu sheet rồi xoá NỘI DUNG mà không xoá DÒNG; Excel giữ lại dòng
    /// đó, nên <c>sheet.FirstRowNum</c> trỏ vào nó chứ không vào dòng tiêu đề thật.
    ///
    /// <para>Bản cũ <c>yield break</c> ⇒ kết quả là "file không có dòng nào", không exception,
    /// không log. Lý do chọn NÉM thay vì bỏ qua dòng trống: xem test cùng tên ở
    /// <c>CsvImportFileReaderTests</c>.</para>
    /// </summary>
    [Theory(DisplayName = "F8: dòng rỗng TRƯỚC header → ném (lỗi của cả file), KHÔNG trả 0 dòng")]
    [MemberData(nameof(Formats))]
    public async Task BlankRowBeforeHeader_Throws(string format)
    {
        var exception = await Assert.ThrowsAsync<InvalidDataException>(() => ReadAllAsync(format, sheet =>
        {
            // Tạo dòng 0 nhưng không đặt giá trị nào — dòng có thật, mọi ô rỗng.
            sheet.CreateRow(0).CreateCell(0);
            sheet.WriteTextRow(1, "Mã", "Tên");
            sheet.WriteTextRow(2, "A1", "X");
        }));

        Assert.Contains("Xoá hẳn dòng trống", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>Dòng rỗng KHÔNG bị bỏ — hợp đồng ở <see cref="IImportFileReader.ReadAsync"/>: bên
    /// gọi đếm số thứ tự dòng theo thứ tự yield.</summary>
    [Theory(DisplayName = "Dòng rỗng giữa file vẫn được yield (giữ đúng số thứ tự dòng cho bên gọi)")]
    [MemberData(nameof(Formats))]
    public async Task BlankRow_IsNotSkipped(string format)
    {
        var rows = await ReadAllAsync(format, sheet =>
        {
            sheet.WriteTextRow(0, "Mã");
            sheet.WriteTextRow(1, "A1");
            // Dòng 2 CỐ Ý không tạo — sheet.GetRow(2) sẽ trả null.
            sheet.WriteTextRow(3, "A3");
        });

        Assert.Equal(3, rows.Count);
        Assert.True(rows[1]["Mã"].IsEmpty);
        Assert.Equal("A3", rows[2]["Mã"].Text);
    }

    // ───────────────────────────── §2b — ô công thức ─────────────────────────────

    /// <summary>
    /// <b>Lỗi §2b của doc/huong_dan/wiki-core/be/15-import-export.md.</b> Bản cũ trả
    /// <c>cell.ToString()</c> cho ô công thức, mà NPOI trả về CHUỖI CÔNG THỨC — ô <c>=B2*100</c> vào
    /// DB thành chữ <c>"B2*100"</c>. Không crash, không log, chỉ có dữ liệu rác nằm im.
    ///
    /// <para>Khẳng định phải có CẢ HAI chiều: lấy đúng 704, và KHÔNG lấy chuỗi công thức. Chỉ
    /// khẳng định chiều đầu thì một bản cài trả <c>null</c> cũng trượt qua được ở ca khác.</para>
    /// </summary>
    [Theory(DisplayName = "§2b: ô công thức trả KẾT QUẢ đã lưu, KHÔNG phải chuỗi công thức")]
    [MemberData(nameof(Formats))]
    public async Task FormulaCell_ReturnsCachedResult_NotFormulaText(string format)
    {
        var row = Assert.Single(await ReadAllAsync(format, sheet =>
        {
            sheet.WriteTextRow(0, "Điểm");
            var cell = sheet.CreateRow(1).CreateCell(0);
            cell.SetCellFormula("B2*100");
            cell.SetCellValue(704d); // kết quả Excel đã lưu sẵn cùng công thức
        }));

        Assert.Equal(704d, row["Điểm"].AsNumber);
        Assert.DoesNotContain("B2", row["Điểm"].ToString(), StringComparison.Ordinal);
    }

    [Theory(DisplayName = "§2b: ô công thức trả CHUỖI cũng lấy được kết quả, không phải công thức")]
    [MemberData(nameof(Formats))]
    public async Task StringFormulaCell_ReturnsCachedResult(string format)
    {
        var row = Assert.Single(await ReadAllAsync(format, sheet =>
        {
            sheet.WriteTextRow(0, "Trạng thái");
            var cell = sheet.CreateRow(1).CreateCell(0);
            cell.SetCellFormula("CONCATENATE(\"Đang \",\"thực hiện\")");
            cell.SetCellValue("Đang thực hiện");
        }));

        Assert.Equal("Đang thực hiện", row["Trạng thái"].Text);
    }

    // ───────────────────────────── §2c — ô ngày ─────────────────────────────

    /// <summary>
    /// <b>Lỗi §2c.</b> Bản cũ ép ô ngày về chuỗi <c>dd/MM/yyyy</c> rồi bên kia parse ngược — mất
    /// giờ/phút và dính locale máy chạy. Nay giữ nguyên <see cref="DateTime"/> qua seam.
    ///
    /// <para>Giá trị test cố ý có GIỜ VÀ PHÚT: một bản cài quay lại đường <c>dd/MM/yyyy</c> vẫn
    /// khớp phần ngày, chỉ phần giờ mới tố cáo nó.</para>
    /// </summary>
    [Theory(DisplayName = "§2c: ô ngày giữ nguyên kiểu DateTime, không bị ép về chuỗi dd/MM/yyyy")]
    [MemberData(nameof(Formats))]
    public async Task DateCell_KeepsRealDateTime(string format)
    {
        var expected = new DateTime(2026, 8, 11, 14, 30, 0, DateTimeKind.Unspecified);

        var row = Assert.Single(await ReadAllAsync(format, sheet =>
        {
            sheet.WriteTextRow(0, "Hạn xử lý");

            var workbook = sheet.Workbook;
            var style = workbook.CreateCellStyle();
            style.DataFormat = workbook.CreateDataFormat().GetFormat("dd/MM/yyyy HH:mm");

            var cell = sheet.CreateRow(1).CreateCell(0);
            cell.SetCellValue(expected);
            cell.CellStyle = style;
        }));

        Assert.Equal(expected, row["Hạn xử lý"].AsDate);

        // Dạng chữ đi kèm phải là ISO round-trip, KHÔNG theo locale nào.
        Assert.StartsWith("2026-08-11T14:30:00", row["Hạn xử lý"].ToString(), StringComparison.Ordinal);
    }

    [Theory(DisplayName = "Ô số KHÔNG định dạng ngày vẫn là số, không bị nhận nhầm thành ngày")]
    [MemberData(nameof(Formats))]
    public async Task PlainNumericCell_IsNotMistakenForDate(string format)
    {
        var row = Assert.Single(await ReadAllAsync(format, sheet =>
        {
            sheet.WriteTextRow(0, "Tự đánh giá");
            sheet.CreateRow(1).CreateCell(0).SetCellValue(7.04d);
        }));

        Assert.Equal(7.04d, row["Tự đánh giá"].AsNumber);
        Assert.Null(row["Tự đánh giá"].AsDate);
    }

    [Theory(DisplayName = "Ô luận lý đọc được, ô trống là Empty")]
    [MemberData(nameof(Formats))]
    public async Task BooleanAndBlankCells(string format)
    {
        var row = Assert.Single(await ReadAllAsync(format, sheet =>
        {
            sheet.WriteTextRow(0, "Đạt", "Ghi chú");
            var dataRow = sheet.CreateRow(1);
            dataRow.CreateCell(0).SetCellValue(true);
            dataRow.CreateCell(1).SetCellType(CellType.Blank);
        }));

        Assert.True(row["Đạt"].AsBoolean);
        Assert.True(row["Ghi chú"].IsEmpty);
    }

    // ───────────────────────────── File sai định dạng ─────────────────────────────

    /// <summary>
    /// Nội dung không phải workbook ⇒ ném. Đây là lỗi của CẢ file (hợp đồng
    /// <see cref="IImportFileReader.ReadAsync"/>), khác lỗi của một dòng — bên gọi bắt ở vòng ngoài
    /// và đánh dấu cả lượt import là thất bại.
    /// </summary>
    [Theory(DisplayName = "Nội dung không phải workbook → ném (lỗi của cả file)")]
    [MemberData(nameof(Formats))]
    public async Task GarbageContent_Throws(string format)
    {
        using var stream = new MemoryStream("day khong phai file excel"u8.ToArray());
        var reader = ReaderFor(format);

        await Assert.ThrowsAnyAsync<Exception>(async () =>
        {
            await foreach (var _ in reader.ReadAsync(stream, $"file.{format}", CancellationToken.None))
            {
                // Iterator chỉ chạy khi được duyệt — vòng lặp rỗng này chính là chỗ ném.
            }
        });
    }

    // ───────────────────────────── §2a — CanRead theo magic byte ─────────────────────────────

    /// <summary>
    /// <b>Lỗi §2a.</b> Nhận diện theo NỘI DUNG, không theo đuôi file do người dùng đặt: một
    /// <c>.xlsx</c> đổi tên thành <c>.xls</c> phải về đúng bộ đọc OOXML.
    /// </summary>
    [Fact(DisplayName = "§2a: CanRead xét chữ ký byte, KHÔNG xét phần mở rộng")]
    public void CanRead_UsesMagicBytes_NotExtension()
    {
        var xlsx = new XlsxImportFileReader();
        var xls = new XlsImportFileReader();

        ReadOnlySpan<byte> zip = [0x50, 0x4B, 0x03, 0x04, 0x14, 0x00, 0x06, 0x00];
        ReadOnlySpan<byte> ole2 = [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1];

        // Nội dung OOXML nhưng mang tên .xls → vẫn là việc của bộ đọc OOXML.
        Assert.True(xlsx.CanRead(zip, "thuc-ra-la-xlsx.xls"));
        Assert.False(xls.CanRead(zip, "thuc-ra-la-xlsx.xls"));

        // Và chiều ngược lại.
        Assert.True(xls.CanRead(ole2, "thuc-ra-la-xls.xlsx"));
        Assert.False(xlsx.CanRead(ole2, "thuc-ra-la-xls.xlsx"));
    }

    [Fact(DisplayName = "CanRead: header NGẮN hơn chữ ký (file rỗng) trả false, không ném")]
    public void CanRead_ToleratesShortHeader()
    {
        Assert.False(new XlsxImportFileReader().CanRead([], "rong.xlsx"));
        Assert.False(new XlsImportFileReader().CanRead([0xD0, 0xCF], "cut.xls"));
    }

    /// <summary>
    /// Nghiệm thu đường đi thật: file do NPOI ghi ra PHẢI mở đầu bằng đúng chữ ký mà
    /// <see cref="ExcelImportFileReader.CanRead"/> tìm. Không có ca này thì bảng magic byte chỉ là
    /// một hằng số chép tay không ai đối chiếu với file thật.
    /// </summary>
    [Theory(DisplayName = "File thật ghi ra mang đúng chữ ký mà CanRead của chính nó tìm")]
    [MemberData(nameof(Formats))]
    public void RealFileBytes_MatchOwnSignature(string format)
    {
        using var stream = Build(format, sheet => sheet.WriteTextRow(0, "Mã"));
        var header = stream.ToArray().AsSpan(0, 8);

        Assert.True(ReaderFor(format).CanRead(header, $"file.{format}"));
    }
}

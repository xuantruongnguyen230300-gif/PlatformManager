using System.Text;
using PlatformManager.Core.Application.Import;
using PlatformManager.Core.Infrastructure.Import;
using Xunit;

namespace PlatformManager.Core.UnitTests.Import;

/// <summary>
/// <see cref="CsvImportFileReader"/> — file hợp lệ, file rỗng, file sai định dạng, và file CSV
/// THẬT do BA gửi.
/// </summary>
public class CsvImportFileReaderTests
{
    private static readonly CsvImportFileReader Reader = new();

    private static async Task<List<IReadOnlyDictionary<string, ImportCellValue>>> ReadAllAsync(
        Stream stream, string fileName = "file.csv")
    {
        var rows = new List<IReadOnlyDictionary<string, ImportCellValue>>();
        await foreach (var row in Reader.ReadAsync(stream, fileName, CancellationToken.None))
            rows.Add(row);
        return rows;
    }

    [Fact(DisplayName = "File hợp lệ → đúng số dòng, đúng tên cột, đúng giá trị")]
    public async Task ValidFile_IsReadIntoRows()
    {
        using var stream = ImportTestFiles.Csv("Mã,Tên\r\nA1,Chỉ tiêu một\r\nA2,Chỉ tiêu hai\r\n");

        var rows = await ReadAllAsync(stream);

        Assert.Equal(2, rows.Count);
        Assert.Equal(["Mã", "Tên"], rows[0].Keys.Order(StringComparer.Ordinal));
        Assert.Equal("A1", rows[0]["Mã"].Text);
        Assert.Equal("Chỉ tiêu hai", rows[1]["Tên"].Text);
    }

    /// <summary>
    /// BOM UTF-8 phải bị nuốt, không dính vào tên cột đầu tiên. Đây là ca phổ biến NHẤT (Excel
    /// trên Windows luôn xuất kèm BOM) và cũng là ca hỏng khó tin nhất khi gặp: cột "Mã" vẫn hiện
    /// đúng chữ "Mã" trong mọi trình xem, nhưng mọi phép tra theo tên đều trượt vì ký tự vô hình
    /// ở đầu.
    /// </summary>
    [Fact(DisplayName = "BOM UTF-8 không dính vào tên cột đầu tiên")]
    public async Task Bom_IsStrippedFromFirstHeader()
    {
        using var withBom = ImportTestFiles.Csv("Mã,Tên\r\nA1,X\r\n");
        var rows = await ReadAllAsync(withBom);

        Assert.True(rows[0].ContainsKey("Mã"));
        Assert.DoesNotContain(rows[0].Keys, key => key.StartsWith('﻿'));
    }

    [Fact(DisplayName = "Không BOM cũng đọc được (file do công cụ khác sinh)")]
    public async Task WithoutBom_IsAlsoRead()
    {
        using var stream = ImportTestFiles.CsvWithoutBom("Mã,Tên\r\nA1,X\r\n");

        var rows = await ReadAllAsync(stream);

        Assert.Equal("A1", Assert.Single(rows)["Mã"].Text);
    }

    [Fact(DisplayName = "Tên cột có khoảng trắng thừa → trim; cột header rỗng → bỏ")]
    public async Task Headers_AreTrimmed_AndBlankOnesDropped()
    {
        using var stream = ImportTestFiles.Csv("  Mã  ,,Tên\r\nA1,bỏ đi,X\r\n");

        var row = Assert.Single(await ReadAllAsync(stream));

        Assert.Equal(["Mã", "Tên"], row.Keys.Order(StringComparer.Ordinal));
        Assert.Equal("A1", row["Mã"].Text);
        // Cột thứ 2 bị bỏ vì không có tên — giá trị của nó KHÔNG được trôi sang cột "Tên".
        Assert.Equal("X", row["Tên"].Text);
    }

    [Fact(DisplayName = "File rỗng → 0 dòng, KHÔNG ném")]
    public async Task EmptyFile_YieldsNothing()
    {
        using var stream = new MemoryStream([]);

        Assert.Empty(await ReadAllAsync(stream));
    }

    [Fact(DisplayName = "File chỉ có header → 0 dòng, KHÔNG ném")]
    public async Task HeaderOnly_YieldsNothing()
    {
        using var stream = ImportTestFiles.Csv("Mã,Tên\r\n");

        Assert.Empty(await ReadAllAsync(stream));
    }

    /// <summary>
    /// §F8 (2026-09-09) — dòng trống đứng TRƯỚC header. Bản cũ trả về mỗi dòng dữ liệu thành một
    /// dictionary RỖNG: đúng số bản ghi nhưng không tra được cột nào, và không exception nào.
    ///
    /// <para><b>Vì sao chọn NÉM chứ không "bỏ qua dòng trống rồi lấy dòng sau làm header":</b> hợp
    /// đồng <see cref="IImportFileReader.ReadAsync"/> buộc reader KHÔNG bỏ dòng nào, vì bên gọi
    /// đánh số dòng theo thứ tự yield. Bỏ dòng ở đầu file làm phép tính đó lệch đúng bằng số dòng
    /// đã bỏ — tức đổi một lỗi im lặng lấy một lỗi im lặng khác. Ném thì lỗi nói ra được.</para>
    /// </summary>
    [Fact(DisplayName = "F8: dòng trống TRƯỚC header → ném (lỗi của cả file), KHÔNG trả 0 dòng")]
    public async Task BlankLineBeforeHeader_Throws()
    {
        using var stream = ImportTestFiles.Csv(",,\r\nMã,Tên\r\nA1,X\r\n");

        var exception = await Assert.ThrowsAsync<InvalidDataException>(() => ReadAllAsync(stream));

        // Nói ra CÁCH SỬA, không chỉ "file sai": người dùng xoá nội dung dòng chứ không xoá dòng là
        // đúng ca sinh ra lỗi này.
        Assert.Contains("Xoá hẳn dòng trống", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// Dòng rỗng KHÔNG được bỏ — hợp đồng ở <see cref="IImportFileReader.ReadAsync"/>. Bên gọi đếm
    /// số thứ tự dòng theo thứ tự yield, nên bỏ một dòng ở đây làm mọi thông báo lỗi cấp dòng phía
    /// sau nó trỏ nhầm, và sai lệch tăng dần theo số dòng rỗng.
    /// </summary>
    [Fact(DisplayName = "Dòng rỗng giữa file vẫn được yield (giữ đúng số thứ tự dòng cho bên gọi)")]
    public async Task BlankRow_IsNotSkipped()
    {
        using var stream = ImportTestFiles.Csv("Mã,Tên\r\nA1,X\r\n,\r\nA3,Z\r\n");

        var rows = await ReadAllAsync(stream);

        Assert.Equal(3, rows.Count);
        Assert.True(rows[1]["Mã"].IsEmpty);
        Assert.Equal("A3", rows[2]["Mã"].Text);
    }

    [Fact(DisplayName = "Dòng thiếu cột → ô trống, KHÔNG ném (lỗi của MỘT dòng, không phải của file)")]
    public async Task ShortRow_YieldsEmptyCells()
    {
        using var stream = ImportTestFiles.Csv("Mã,Tên,Ghi chú\r\nA1,X\r\n");

        var row = Assert.Single(await ReadAllAsync(stream));

        Assert.Equal("X", row["Tên"].Text);
        Assert.True(row["Ghi chú"].IsEmpty);
    }

    /// <summary>CSV không mang kiểu — mọi ô là chữ, kể cả ô trông như số/ngày.</summary>
    [Fact(DisplayName = "Mọi ô CSV là chữ: AsNumber/AsDate luôn null")]
    public async Task CsvCells_AreAlwaysText()
    {
        using var stream = ImportTestFiles.Csv("Số,Ngày\r\n7.04,11/08/2026\r\n");

        var row = Assert.Single(await ReadAllAsync(stream));

        Assert.Equal("7.04", row["Số"].Text);
        Assert.Null(row["Số"].AsNumber);
        Assert.Null(row["Ngày"].AsDate);
    }

    // ───────────────────────────── CanRead ─────────────────────────────

    [Fact(DisplayName = "CanRead: nhận .csv, từ chối .xlsx/.xls")]
    public void CanRead_AcceptsCsvExtensionOnly()
    {
        ReadOnlySpan<byte> text = "Mã,Tên"u8;

        Assert.True(Reader.CanRead(text, "bao-cao.csv"));
        Assert.True(Reader.CanRead(text, "BAO-CAO.CSV"));
        Assert.False(Reader.CanRead(text, "bao-cao.xlsx"));
        Assert.False(Reader.CanRead(text, "bao-cao.txt"));
    }

    /// <summary>
    /// Lỗi §2a (doc/huong_dan/wiki-core/be/15-import-export.md), chiều nguy hiểm nhất: một file
    /// <c>.xlsx</c> đổi tên thành <c>.csv</c> mà lọt vào bộ đọc CSV sẽ "đọc thành công" ra vài dòng
    /// rác nhị phân — không exception, không log, chỉ có dữ liệu vô nghĩa đi thẳng vào DB.
    /// </summary>
    [Fact(DisplayName = "CanRead: file NHỊ PHÂN đổi đuôi thành .csv vẫn bị từ chối (nhận theo nội dung)")]
    public void CanRead_RejectsBinaryContent_EvenWithCsvExtension()
    {
        Assert.False(Reader.CanRead([0x50, 0x4B, 0x03, 0x04, 0x14, 0x00, 0x06, 0x00], "thuc-ra-la.csv"));
        Assert.False(Reader.CanRead([0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1], "thuc-ra-la.csv"));
    }

    [Fact(DisplayName = "CanRead: header NGẮN hơn chữ ký (file rỗng/1 byte) không làm vỡ phép so")]
    public void CanRead_ToleratesShortHeader()
    {
        Assert.True(Reader.CanRead([], "rong.csv"));
        Assert.True(Reader.CanRead([0x50], "mot-byte.csv"));
    }

    // ──────────────────── File CSV trên đĩa (không phải chuỗi dựng tay) ────────────────────

    /// <summary>
    /// File CSV mẫu ẨN DANH có trong repo, dựng theo đúng hình dạng file BA gửi — số liệu tham
    /// chiếu ở <c>spec/danh-muc-dti/business-rules.md</c> §7: 62 chỉ tiêu, 11 cột. Vì sao là bản
    /// ẩn danh chứ không phải file gốc: xem <see cref="ImportTestFiles.RealCsvPath"/>.
    ///
    /// <para><b>Vì sao đáng chạy trên một file trên đĩa chứ không chỉ trên chuỗi dựng tay:</b> file
    /// này có ô chứa XUỐNG DÒNG bên trong dấu nháy, nên số dòng vật lý của nó LỚN HƠN số bản ghi.
    /// Một bộ đọc tách theo ký tự xuống dòng sẽ ra thừa "dòng dữ liệu" — sai, mà vẫn chạy trơn tru.
    /// Khẳng định con số 62 là thứ duy nhất bắt được ca đó.</para>
    /// </summary>
    [Fact(DisplayName = "File CSV mẫu trên đĩa → đúng 62 dòng, 11 cột, tiếng Việt không lỗi mã hoá")]
    public async Task RealBaFile_Reads62RowsAnd11Columns()
    {
        await using var stream = File.OpenRead(ImportTestFiles.RealCsvPath);

        var rows = await ReadAllAsync(stream, Path.GetFileName(ImportTestFiles.RealCsvPath));

        Assert.Equal(62, rows.Count);
        Assert.All(rows, row => Assert.Equal(11, row.Count));

        // Tên cột tiếng Việt còn nguyên dấu ⇒ mã hoá đọc đúng (BOM + UTF-8), không phải "đọc được
        // một chuỗi byte nào đó".
        Assert.Contains("Điểm tối đa", rows[0].Keys);
        Assert.Contains("Minh chứng/Ghi chú", rows[0].Keys);

        Assert.Equal("1.1", rows[0]["Mã"].Text);
        Assert.Equal("Hạ tầng và Nền tảng số", rows[0]["Nhóm"].Text);

        // §7 của spec: cột "Phụ trách" và "Hạn xử lý" RỖNG toàn bộ 62 dòng.
        Assert.All(rows, row => Assert.True(row["Phụ trách"].IsEmpty));
        Assert.All(rows, row => Assert.True(row["Hạn xử lý"].IsEmpty));
    }

    /// <summary>
    /// Đối chứng cho khẳng định 62 dòng ở trên: chứng minh file mẫu ĐÚNG là có nhiều hơn 62 dòng
    /// vật lý, tức con số 62 không đến từ việc đếm dòng văn bản.
    ///
    /// <para>Ngưỡng là <c>&gt; 63</c> (62 bản ghi + 1 dòng header) chứ không phải một con số chép
    /// cứng: nó đo đúng TÍNH CHẤT cần có — "có ít nhất một ô xuống dòng" — nên vẫn đúng khi file
    /// mẫu được dựng lại với số ô nhiều dòng khác đi.</para>
    /// </summary>
    [Fact(DisplayName = "Đối chứng: file mẫu có nhiều dòng vật lý hơn 63 — 62 là số BẢN GHI, không phải số dòng")]
    public async Task RealBaFile_HasMorePhysicalLinesThanRecords()
    {
        var physicalLines = (await File.ReadAllLinesAsync(ImportTestFiles.RealCsvPath, Encoding.UTF8)).Length;

        Assert.True(physicalLines > 63,
            $"File mẫu chỉ có {physicalLines} dòng vật lý ⇒ nó không còn ô nào chứa xuống dòng, và " +
            "khẳng định '62 bản ghi' không còn phân biệt được bộ đọc CSV thật với một phép tách dòng.");
    }
}

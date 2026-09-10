using System.Runtime.CompilerServices;
using NPOI.SS.UserModel;
using PlatformManager.Core.Application.Import;

namespace PlatformManager.Core.Infrastructure.Import;

/// <summary>
/// Phần dùng chung của hai bộ đọc Excel. Khác biệt duy nhất giữa <c>.xlsx</c> và <c>.xls</c> là
/// lớp workbook của NPOI và chữ ký byte đầu file — nên đó là hai thứ duy nhất lớp con phải khai.
///
/// <para><b>CHỈ sheet đầu tiên</b>, và <b>dòng VẬT LÝ đầu tiên của sheet = header</b>. Không gộp
/// ô, không nhiều sheet — giữ nguyên phạm vi của bản cũ; mở rộng thì phải có yêu cầu thật kèm
/// theo, vì mỗi khái niệm thêm vào đây là một khái niệm mọi nghiệp vụ dùng import đều phải hiểu.</para>
///
/// <para><b>Sửa 2026-09-09 — câu trên trước đây ghi "dòng đầu tiên CÓ DỮ LIỆU = header", mà code
/// thì luôn lấy <c>sheet.GetRow(sheet.FirstRowNum)</c>.</b> Hai câu đó khác nhau ở đúng ca hay gặp
/// nhất: người dùng chèn một dòng tiêu đề ở đầu sheet rồi xoá nội dung đi, để lại một dòng TỒN TẠI
/// VẬT LÝ nhưng toàn ô rỗng. Khi đó bộ đọc lấy dòng rỗng đó làm header, không tên cột nào đọc
/// được, và bản cũ <c>yield break</c> — kết quả trả về là "file không có dòng nào", không exception,
/// không log. Nay ca đó NÉM (xem <see cref="ReadAsync"/>).</para>
///
/// <para>NPOI là thư viện đồng bộ, không có API đọc file bất đồng bộ thật. Đường đọc này chạy
/// trong job nền (không giữ request HTTP nào) và trần dung lượng đã chặn ở
/// <see cref="ImportOptions"/>, nên đọc đồng bộ là đánh đổi chấp nhận được — <c>Task.Yield()</c> ở
/// đầu để lời gọi trả về ngay thay vì chạy hết phần dựng workbook ngay trên luồng gọi.</para>
/// </summary>
public abstract class ExcelImportFileReader : IImportFileReader
{
    public abstract bool CanRead(ReadOnlySpan<byte> header, string fileName);

    /// <summary>Mở workbook đúng họ định dạng của lớp con. Ném nếu nội dung không phải workbook —
    /// đó là lỗi của cả file, đúng hợp đồng <see cref="IImportFileReader.ReadAsync"/>.</summary>
    protected abstract IWorkbook CreateWorkbook(Stream stream);

    public async IAsyncEnumerable<IReadOnlyDictionary<string, ImportCellValue>> ReadAsync(
        Stream stream, string fileName, [EnumeratorCancellation] CancellationToken ct)
    {
        await Task.Yield();

        using var workbook = CreateWorkbook(stream);

        // GetSheetAt(0) ném khi workbook không có sheet nào — file rỗng là "không có dòng nào",
        // không phải "hỏng định dạng".
        if (workbook.NumberOfSheets == 0)
            yield break;

        var sheet = workbook.GetSheetAt(0);
        var headerRow = sheet?.GetRow(sheet.FirstRowNum);
        if (sheet is null || headerRow is null)
            yield break;

        // Bỏ cột header rỗng (hợp đồng ở IImportFileReader) — giữ chỉ số cột để còn tra ô.
        var headers = new List<(string Name, int Index)>();
        for (var column = 0; column < headerRow.LastCellNum; column++)
        {
            var name = ReadCell(headerRow.GetCell(column)).ToString().Trim();
            if (name.Length > 0)
                headers.Add((name, column));
        }

        // KHÔNG yield break: "dòng đầu không có tên cột nào đọc được" là lỗi của CẢ FILE (hợp đồng
        // ở IImportFileReader.ReadAsync), không phải "file có 0 dòng". Trả 0 dòng ở đây là hỏng IM
        // LẶNG — bên gọi báo import thành công với 0 bản ghi, và người dùng đi tìm dữ liệu của mình.
        if (headers.Count == 0)
            throw new InvalidDataException(
                $"Dòng đầu tiên của sheet '{sheet.SheetName}' trong '{fileName}' không có tên cột nào " +
                "đọc được (mọi ô đều rỗng). Bộ đọc lấy đúng DÒNG VẬT LÝ ĐẦU TIÊN làm header, nên một " +
                "dòng trống chèn ở đầu sheet sẽ chiếm chỗ dòng tiêu đề. Xoá hẳn dòng trống đó (xoá cả " +
                "dòng, không chỉ xoá nội dung) rồi tải lên lại.");

        for (var rowIndex = sheet.FirstRowNum + 1; rowIndex <= sheet.LastRowNum; rowIndex++)
        {
            ct.ThrowIfCancellationRequested();

            // KHÔNG bỏ dòng rỗng, kể cả khi excelRow là null: bên gọi đếm số thứ tự dòng theo thứ
            // tự yield (hợp đồng ở IImportFileReader). Bỏ một dòng ở đây làm mọi thông báo lỗi cấp
            // dòng phía sau nó trỏ nhầm.
            var excelRow = sheet.GetRow(rowIndex);

            var row = new Dictionary<string, ImportCellValue>(headers.Count, StringComparer.Ordinal);
            foreach (var (name, index) in headers)
                row[name] = ReadCell(excelRow?.GetCell(index));

            yield return row;
        }
    }

    /// <summary>
    /// Một ô → giá trị trung tính. <b>Đây là nơi hai trong ba lỗi của bản cũ được sửa</b>
    /// (doc/huong_dan/wiki-core/be/15-import-export.md §2b, §2c).
    ///
    /// <para><b>§2b — ô công thức.</b> Bản cũ trả <c>cell.ToString()</c> cho
    /// <see cref="CellType.Formula"/>, mà NPOI (giống Apache POI) trả về CHUỖI CÔNG THỨC chứ không
    /// phải kết quả: ô <c>=B2*100</c> vào DB thành chữ <c>"B2*100"</c>. Không crash, không log, chỉ
    /// có dữ liệu rác nằm im tới khi ai đó phát hiện một con số vô lý. Sửa: hỏi
    /// <see cref="ICell.CachedFormulaResultType"/> rồi đọc theo đúng kiểu đó — Excel đã lưu sẵn kết
    /// quả lần tính cuối, không cần <c>IFormulaEvaluator</c> (thứ phải tính lại cả workbook và tự
    /// nó cũng ném với hàm NPOI chưa hỗ trợ).</para>
    ///
    /// <para><b>§2c — ô ngày.</b> Bản cũ ép về chuỗi <c>dd/MM/yyyy</c> rồi bên kia parse ngược:
    /// mất giờ/phút và phụ thuộc locale của máy chạy. Nay giữ nguyên <see cref="DateTime"/> qua
    /// seam.</para>
    /// </summary>
    private static ImportCellValue ReadCell(ICell? cell)
    {
        if (cell is null)
            return ImportCellValue.Empty;

        var type = cell.CellType == CellType.Formula ? cell.CachedFormulaResultType : cell.CellType;

        return type switch
        {
            CellType.String => ImportCellValue.FromText(cell.StringCellValue),
            CellType.Numeric => ReadNumeric(cell),
            CellType.Boolean => ImportCellValue.FromBoolean(cell.BooleanCellValue),
            CellType.Error => ReadError(cell),
            // Blank và Unknown: không có giá trị nào để lấy.
            _ => ImportCellValue.Empty,
        };
    }

    /// <summary>
    /// Ngày trong Excel LÀ một con số — phân biệt được với số thường chỉ qua định dạng hiển thị của
    /// ô. Gọi <see cref="DateUtil.IsCellDateFormatted"/> chỉ khi kiểu hiệu lực đã là
    /// <see cref="CellType.Numeric"/>: hàm đó đọc <c>NumericCellValue</c>, và trên một ô công thức
    /// trả chuỗi thì chính lời gọi ấy ném.
    /// </summary>
    private static ImportCellValue ReadNumeric(ICell cell) =>
        DateUtil.IsCellDateFormatted(cell) && cell.DateCellValue is { } date
            ? ImportCellValue.FromDate(date)
            : ImportCellValue.FromNumber(cell.NumericCellValue);

    /// <summary>
    /// Ô lỗi (<c>#DIV/0!</c>, <c>#N/A</c>…) — trả nguyên mã lỗi dạng chữ thay vì coi như ô trống.
    /// Bên xử lý sẽ không parse được nó thành số/ngày và sẽ báo lỗi đúng dòng đó, kèm một câu người
    /// dùng nhận ra ngay khi mở file gốc. Coi như trống thì dòng đó âm thầm mất giá trị.
    /// </summary>
    private static ImportCellValue ReadError(ICell cell)
    {
        try
        {
            return ImportCellValue.FromText(FormulaError.ForInt(cell.ErrorCellValue).String);
        }
        catch (ArgumentException)
        {
            // Mã lỗi ngoài bảng NPOI biết — vẫn phải nói ra là "có lỗi ở ô này".
            return ImportCellValue.FromText("#ERROR!");
        }
    }
}

using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using PlatformManager.Core.Application.Import;

namespace PlatformManager.Core.Infrastructure.Import;

/// <summary>
/// Đọc CSV bằng CsvHelper. Dòng 1 = header; mọi ô là chữ (CSV không mang kiểu), nên
/// <see cref="ImportCellValue.AsDate"/>/<see cref="ImportCellValue.AsNumber"/> của reader này luôn
/// <c>null</c> và bên xử lý phải tự parse từ <see cref="ImportCellValue.Text"/>.
///
/// <para><b>BOM UTF-8 được nhận và bỏ đi</b> (<c>detectEncodingFromByteOrderMarks</c>). Không có
/// nó, tên cột đầu tiên mang thêm ký tự vô hình ở đầu và mọi phép tra cột theo tên đều trượt —
/// đúng lúc file do Excel trên Windows xuất ra, tức ca phổ biến nhất.</para>
///
/// <para><b>"Dòng 1 = header" là DÒNG VẬT LÝ, không phải "dòng đầu tiên có dữ liệu"</b> (làm rõ
/// 2026-09-09). Một dòng trống ở đầu file chiếm chỗ dòng tiêu đề, và ca đó nay NÉM thay vì trả 0
/// dòng — cùng luật với <c>ExcelImportFileReader</c>, lý do đầy đủ ghi ở đó.</para>
/// </summary>
public sealed class CsvImportFileReader : IImportFileReader
{
    /// <summary>
    /// CSV KHÔNG có chữ ký nhận biết được, nên đây là nhánh mặc định và bắt buộc phải xét đuôi
    /// file (doc/huong_dan/wiki-core/be/15-import-export.md §2, bảng magic byte).
    ///
    /// <para>Nhưng vẫn phải loại các định dạng nhị phân đã biết: một file <c>.xlsx</c> đổi tên
    /// thành <c>.csv</c> mà lọt vào đây sẽ được "đọc thành công" thành vài dòng rác nhị phân —
    /// không exception, không log, chỉ có dữ liệu vô nghĩa. Đó là hướng hỏng tệ hơn hẳn một lỗi
    /// "không đọc được file".</para>
    /// </summary>
    public bool CanRead(ReadOnlySpan<byte> header, string fileName) =>
        string.Equals(Path.GetExtension(fileName), ".csv", StringComparison.OrdinalIgnoreCase)
        && !ImportFileSignatures.IsKnownBinaryFormat(header);

    public async IAsyncEnumerable<IReadOnlyDictionary<string, ImportCellValue>> ReadAsync(
        Stream stream, string fileName, [EnumeratorCancellation] CancellationToken ct)
    {
        // leaveOpen: reader KHÔNG đóng stream nó không mở. Bên gọi (job nền) còn dùng lại stream
        // đó cho việc khác, và một stream bị đóng sớm chỉ lộ ra ở lời gọi tiếp theo.
        using var textReader = new StreamReader(
            stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 4096, leaveOpen: true);

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            // Cột khai trong header nhưng thiếu ở một dòng ⇒ ô trống, không ném. Dòng thiếu cột là
            // dữ liệu sai của MỘT dòng — để bên xử lý báo lỗi dòng đó, đừng làm hỏng cả lượt import.
            MissingFieldFound = null,
            // Ô sai quy tắc trích dẫn ⇒ lấy nguyên văn, không ném. Cùng lý lẽ trên.
            BadDataFound = null,
        };

        using var csv = new CsvReader(textReader, config);

        // File rỗng: ReadAsync trả false ngay. Không có guard này, ReadHeader() ném ReaderException
        // và một file rỗng sẽ báo là "hỏng định dạng" thay vì "không có dòng nào".
        if (!await csv.ReadAsync())
            yield break;

        csv.ReadHeader();

        // Trim: file do người dùng sửa tay rất hay có khoảng trắng thừa quanh tên cột, và một tên
        // cột lệch một dấu cách thì mọi phép tra theo tên đều trượt mà không có gì báo.
        // Bỏ cột header rỗng: không có tên thì không tra được (hợp đồng ở IImportFileReader).
        var headers = (csv.HeaderRecord ?? [])
            .Select((name, index) => (Name: name?.Trim() ?? string.Empty, Index: index))
            .Where(column => column.Name.Length > 0)
            .ToList();

        // Cùng luật với ExcelImportFileReader (2026-09-09): dòng đầu không có tên cột nào đọc được
        // là lỗi của CẢ FILE, không phải "0 dòng". Bản cũ đi tiếp và yield mỗi dòng dữ liệu thành
        // một dictionary RỖNG — tệ hơn cả trả 0 dòng, vì bên gọi thấy đúng số bản ghi nhưng mọi
        // phép tra cột đều trượt, và không có gì báo.
        if (headers.Count == 0)
            throw new InvalidDataException(
                $"Dòng đầu tiên của '{fileName}' không có tên cột nào đọc được (mọi ô đều rỗng). Bộ " +
                "đọc lấy đúng DÒNG ĐẦU TIÊN làm header, nên một dòng trống ở đầu file sẽ chiếm chỗ " +
                "dòng tiêu đề. Xoá hẳn dòng trống đó rồi tải lên lại.");

        while (await csv.ReadAsync())
        {
            ct.ThrowIfCancellationRequested();

            var row = new Dictionary<string, ImportCellValue>(headers.Count, StringComparer.Ordinal);
            foreach (var column in headers)
            {
                // Tra theo CHỈ SỐ cột, không theo tên: tên đã bị trim nên không còn khớp tên gốc
                // trong HeaderRecord, và hai cột trùng tên thì tra theo tên luôn trả cột đầu.
                row[column.Name] = csv.TryGetField<string>(column.Index, out var value)
                    ? ImportCellValue.FromText(value)
                    : ImportCellValue.Empty;
            }

            yield return row;
        }
    }
}

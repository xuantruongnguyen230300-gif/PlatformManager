using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;

namespace PlatformManager.Modules.DtiWeekly.Application.Import;

/// <summary>
/// Bọc lại NGUYÊN VẸN logic đọc CSV từ CsvImportService cũ (trước khi tách job nền) — cùng
/// cấu hình CsvHelper (HasHeaderRecord/MissingFieldFound/BadDataFound), cùng cách detect UTF-8
/// BOM. Chỉ đổi shape output: thay vì đọc trực tiếp CsvReader theo tên cột đã biết trước, trả
/// về dict tên-cột→giá-trị cho MỌI cột trong header, để IImportRowProcessor tự đọc theo tên cột
/// nó cần — không đổi hành vi parse.
/// </summary>
public sealed class CsvFileReader : IImportFileReader
{
    public bool CanRead(string fileName)
        => string.Equals(Path.GetExtension(fileName), ".csv", StringComparison.OrdinalIgnoreCase);

    public async IAsyncEnumerable<IReadOnlyDictionary<string, string?>> ReadAsync(
        Stream stream, string fileName, [EnumeratorCancellation] CancellationToken ct)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            BadDataFound = null,
        };
        using var csv = new CsvReader(reader, config);

        await csv.ReadAsync();
        csv.ReadHeader();
        var headers = csv.HeaderRecord ?? [];

        while (await csv.ReadAsync())
        {
            ct.ThrowIfCancellationRequested();

            var row = new Dictionary<string, string?>(headers.Length);
            foreach (var header in headers)
                row[header] = csv.TryGetField<string>(header, out var value) ? value : null;

            yield return row;
        }
    }
}

using System.Globalization;
using System.Runtime.CompilerServices;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using PlatformManager.Modules.DtiWeekly.Application.Import;

namespace PlatformManager.Modules.DtiWeekly.Infrastructure.Import;

/// <summary>
/// Đọc Excel .xlsx/.xls cho CONTRACT DM-7 (doc/contracts/danh-muc-dti.md) — CHỈ sheet ĐẦU
/// TIÊN, dòng 1 = header, không hỗ trợ nhiều sheet/merged cell ở version đầu. Chọn
/// HSSFWorkbook (.xls, OLE2 cũ) hay XSSFWorkbook (.xlsx, OOXML) theo PHẦN MỞ RỘNG file (không
/// dùng WorkbookFactory.Create() tự nhận diện theo magic byte). Dùng package NPOI — package chỉ
/// thêm ở Infrastructure.csproj (KHÔNG kéo vào Application, giữ đúng
/// .claude/rules/architecture.md §"*.Application → *.Infrastructure").
/// </summary>
public sealed class ExcelFileReader : IImportFileReader
{
    public bool CanRead(string fileName)
    {
        var ext = Path.GetExtension(fileName);
        return string.Equals(ext, ".xlsx", StringComparison.OrdinalIgnoreCase)
            || string.Equals(ext, ".xls", StringComparison.OrdinalIgnoreCase);
    }

    public async IAsyncEnumerable<IReadOnlyDictionary<string, string?>> ReadAsync(
        Stream stream, string fileName, [EnumeratorCancellation] CancellationToken ct)
    {
        // NPOI không có API đọc file async thật — file đã giới hạn 20MB và chạy trong Hangfire
        // worker (không chặn request HTTP nào) nên đọc đồng bộ là chấp nhận được.
        await Task.Yield();

        var extension = Path.GetExtension(fileName);
        IWorkbook workbook = string.Equals(extension, ".xls", StringComparison.OrdinalIgnoreCase)
            ? new HSSFWorkbook(stream)
            : new XSSFWorkbook(stream);

        using (workbook)
        {
            var sheet = workbook.GetSheetAt(0); // CHỈ sheet đầu tiên — CONTRACT DM-7
            if (sheet is null)
                yield break;

            var headerRow = sheet.GetRow(sheet.FirstRowNum);
            if (headerRow is null)
                yield break;

            var headers = new List<string>();
            for (var col = 0; col < headerRow.LastCellNum; col++)
                headers.Add(headerRow.GetCell(col)?.ToString()?.Trim() ?? string.Empty);

            for (var rowIndex = sheet.FirstRowNum + 1; rowIndex <= sheet.LastRowNum; rowIndex++)
            {
                ct.ThrowIfCancellationRequested();

                var excelRow = sheet.GetRow(rowIndex);
                var row = new Dictionary<string, string?>(headers.Count);
                for (var col = 0; col < headers.Count; col++)
                {
                    var header = headers[col];
                    if (string.IsNullOrEmpty(header))
                        continue;

                    row[header] = GetCellValueAsString(excelRow?.GetCell(col));
                }

                yield return row;
            }
        }
    }

    private static string? GetCellValueAsString(ICell? cell)
    {
        if (cell is null)
            return null;

        return cell.CellType switch
        {
            CellType.String => cell.StringCellValue,
            CellType.Numeric => DateUtil.IsCellDateFormatted(cell)
                ? cell.DateCellValue?.ToString("dd/MM/yyyy")
                : cell.NumericCellValue.ToString(CultureInfo.InvariantCulture),
            CellType.Boolean => cell.BooleanCellValue.ToString(),
            CellType.Formula => cell.ToString(),
            CellType.Blank => null,
            _ => cell.ToString(),
        };
    }
}

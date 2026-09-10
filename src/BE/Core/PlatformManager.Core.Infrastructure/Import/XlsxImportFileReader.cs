using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace PlatformManager.Core.Infrastructure.Import;

/// <summary>
/// <c>.xlsx</c> (OOXML) — NPOI <see cref="XSSFWorkbook"/>.
/// </summary>
public sealed class XlsxImportFileReader : ExcelImportFileReader
{
    /// <summary>
    /// Nhận theo CHỮ KÝ ZIP, không theo đuôi file: một <c>.xlsx</c> bị đổi tên thành <c>.xls</c>
    /// vẫn về đúng bộ đọc này (lỗi §2a của
    /// doc/huong_dan/wiki-core/be/15-import-export.md — bản cũ giao nó cho bộ đọc OLE2 và vỡ bằng
    /// một exception không đọc ra nguyên nhân).
    /// </summary>
    public override bool CanRead(ReadOnlySpan<byte> header, string fileName) =>
        ImportFileSignatures.StartsWith(header, ImportFileSignatures.Zip);

    protected override IWorkbook CreateWorkbook(Stream stream) => new XSSFWorkbook(stream);
}

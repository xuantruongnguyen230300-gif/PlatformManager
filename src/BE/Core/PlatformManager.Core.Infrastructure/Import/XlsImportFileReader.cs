using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;

namespace PlatformManager.Core.Infrastructure.Import;

/// <summary>
/// <c>.xls</c> (OLE2 compound, Excel 97–2003) — NPOI <see cref="HSSFWorkbook"/>.
///
/// <para><b>Vì sao còn hỗ trợ định dạng này</b> (chốt 2026-08-29): người dùng khu vực công thật sự
/// còn file cũ, không phải "cho đủ bộ". Cũng vì nó mà thư viện phải là NPOI: ClosedXML và EPPlus
/// đều chỉ đọc được <c>.xlsx</c>. Chiều NGƯỢC lại thì không đối xứng và cố ý — hệ thống KHÔNG xuất
/// <c>.xls</c> (doc/huong_dan/wiki-core/be/15-import-export.md §3).</para>
/// </summary>
public sealed class XlsImportFileReader : ExcelImportFileReader
{
    /// <summary>
    /// Nhận theo CHỮ KÝ OLE2, không theo đuôi file — xem
    /// <see cref="XlsxImportFileReader.CanRead"/> cho lý lẽ đầy đủ.
    ///
    /// <para>OLE2 không riêng của Excel (<c>.doc</c>, <c>.ppt</c>, <c>.msg</c> cùng chữ ký), nên
    /// một file Word sẽ về tới đây rồi vỡ ở <see cref="HSSFWorkbook"/> — đúng chỗ nó nên vỡ, và
    /// vỡ như một lỗi "hỏng định dạng" của cả file.</para>
    /// </summary>
    public override bool CanRead(ReadOnlySpan<byte> header, string fileName) =>
        ImportFileSignatures.StartsWith(header, ImportFileSignatures.Ole2);

    protected override IWorkbook CreateWorkbook(Stream stream) => new HSSFWorkbook(stream);
}

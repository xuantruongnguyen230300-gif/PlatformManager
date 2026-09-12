using System.Runtime.CompilerServices;

namespace PlatformManager.Core.Application.Import;

/// <summary>
/// Đường đọc file <b>ĐÃ ÁP TRẦN SỐ DÒNG</b> — Core cấp một đường đọc, không cấp một con số rời.
///
/// <para>🛑 <b>Bên gọi phải dùng hàm này, KHÔNG gọi thẳng <c>selection.Reader.ReadAsync</c></b>
/// (finding F3, 2026-09-11). Trước bản này, <c>ImportFileReaderSelection.MaxRows</c> chỉ là một
/// <c>int</c> đi kèm: bên gọi thứ nhất có đọc và áp đúng, nhưng bên gọi <b>thứ hai</b> bỏ qua nó
/// thì biên dịch sạch, test xanh, và trần <b>biến mất im lặng</b>. Một trần "khuyến nghị" không
/// phải là trần.</para>
///
/// <para>So với trần DUNG LƯỢNG, thứ được ép bằng cấu trúc từ đầu: vượt trần ⇒
/// <c>ImportFileReaderSelection.Reader</c> là <c>null</c> ⇒ quên kiểm là nổ ngay, ồn ào. Hàm này
/// đưa trần dòng về đúng mức cưỡng chế đó — bỏ qua nó là bỏ qua cả đường đọc, không phải bỏ qua
/// một tham số.</para>
/// </summary>
public static class ImportFileReaderSelectionExtensions
{
    /// <summary>
    /// Đọc từng dòng dữ liệu, ném <see cref="ImportRowLimitExceededException"/> ngay khi vượt
    /// <see cref="ImportFileReaderSelection.MaxRows"/>.
    ///
    /// <para><b>Dừng NGAY khi vượt, không đọc nốt để đếm</b> — đọc tiếp chính là thứ trần này sinh
    /// ra để tránh. Hệ quả: ngoại lệ thành thật là không biết tổng số dòng, nó chỉ nêu trần.</para>
    ///
    /// <para>Mọi hợp đồng khác của <see cref="IImportFileReader.ReadAsync"/> giữ nguyên — không bỏ
    /// dòng nào (kể cả dòng rỗng), và ngoại lệ "file hỏng định dạng" vẫn bay thẳng lên như cũ.</para>
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="selection"/> đã bị từ chối (<c>Reader</c> là <c>null</c>) — bên gọi phải
    /// kiểm <see cref="ImportFileReaderSelection.IsAccepted"/> trước.
    /// </exception>
    public static async IAsyncEnumerable<IReadOnlyDictionary<string, ImportCellValue>> ReadRowsAsync(
        this ImportFileReaderSelection selection,
        Stream stream,
        string fileName,
        [EnumeratorCancellation] CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(selection);

        if (selection.Reader is null)
            throw new InvalidOperationException(
                $"Lượt chọn reader đã bị từ chối ({selection.Rejection}) — không có gì để đọc. Bên gọi " +
                $"phải kiểm {nameof(ImportFileReaderSelection.IsAccepted)} và ánh xạ " +
                $"{nameof(ImportFileRejection)} sang mã lỗi của mình trước khi gọi hàm này.");

        var read = 0;

        await foreach (var row in selection.Reader.ReadAsync(stream, fileName, ct))
        {
            if (read >= selection.MaxRows)
                throw new ImportRowLimitExceededException(selection.MaxRows);

            read++;
            yield return row;
        }
    }
}

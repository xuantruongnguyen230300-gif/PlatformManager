using PlatformManager.Core.Application.Import;
using Xunit;

namespace PlatformManager.Core.UnitTests.Import;

/// <summary>
/// Trần SỐ DÒNG (Q75) — <b>đường đọc đã áp trần</b> của Core
/// (<see cref="ImportFileReaderSelectionExtensions.ReadRowsAsync"/>).
///
/// <para>🔴 <b>Vì sao bộ test này tồn tại (finding F3 + F4, 2026-09-11):</b> bản đầu của Q75 để
/// <c>MaxRows</c> là một <c>int</c> đi kèm <see cref="ImportFileReaderSelection"/>, tức việc áp trần
/// là chuyện TỰ GIÁC của từng bên gọi — bên gọi thứ hai bỏ qua thì biên dịch sạch, test xanh, trần
/// biến mất im lặng. Nay Core cấp một ĐƯỜNG ĐỌC, và bỏ qua nó là bỏ qua cả đường đọc.</para>
/// </summary>
public class ImportRowLimitTests
{
    /// <summary>Reader sinh <paramref name="rowCount"/> dòng — không chạm đĩa, không cần file thật.</summary>
    private sealed class CountingReader(int rowCount) : IImportFileReader
    {
        public int RowsYielded { get; private set; }

        public bool CanRead(ReadOnlySpan<byte> header, string fileName) => true;

        public async IAsyncEnumerable<IReadOnlyDictionary<string, ImportCellValue>> ReadAsync(
            Stream stream,
            string fileName,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
        {
            for (var i = 0; i < rowCount; i++)
            {
                RowsYielded++;
                yield return new Dictionary<string, ImportCellValue>(StringComparer.Ordinal)
                {
                    ["Mã"] = ImportCellValue.FromText($"1.{i}"),
                };

                await Task.Yield();
            }
        }
    }

    private static ImportFileReaderSelection Accepted(IImportFileReader reader, int maxRows) =>
        new(reader, ImportFileRejection.None, 1024, 10L * 1024 * 1024, maxRows);

    private static async Task<List<IReadOnlyDictionary<string, ImportCellValue>>> ReadAllAsync(
        ImportFileReaderSelection selection)
    {
        var rows = new List<IReadOnlyDictionary<string, ImportCellValue>>();

        await foreach (var row in selection.ReadRowsAsync(Stream.Null, "bao-cao.csv", CancellationToken.None))
            rows.Add(row);

        return rows;
    }

    [Fact(DisplayName = "Dưới trần ⇒ đọc đủ mọi dòng, không ném")]
    public async Task UnderCap_ReadsEveryRow()
    {
        var rows = await ReadAllAsync(Accepted(new CountingReader(5), maxRows: 10));

        Assert.Equal(5, rows.Count);
    }

    [Fact(DisplayName = "ĐÚNG bằng trần ⇒ vẫn đọc hết, không ném (trần là giới hạn TRÊN, bao gồm)")]
    public async Task ExactlyAtCap_DoesNotThrow()
    {
        var rows = await ReadAllAsync(Accepted(new CountingReader(10), maxRows: 10));

        Assert.Equal(10, rows.Count);
    }

    [Fact(DisplayName = "Vượt trần ⇒ ném ImportRowLimitExceededException mang đúng con số trần")]
    public async Task OverCap_Throws_WithMaxRows()
    {
        var selection = Accepted(new CountingReader(50), maxRows: 10);

        var ex = await Assert.ThrowsAsync<ImportRowLimitExceededException>(() => ReadAllAsync(selection));

        Assert.Equal(10, ex.MaxRows);
    }

    /// <summary>
    /// Dừng NGAY khi vượt, không đọc nốt để đếm — đọc tiếp chính là thứ trần này sinh ra để tránh.
    /// Đây là phép đo phân biệt "có trần" với "có trần nhưng vẫn nạp cả file vào bộ nhớ rồi mới từ
    /// chối", hai thứ trông giống hệt nhau từ phía người gọi.
    /// </summary>
    [Fact(DisplayName = "Vượt trần ⇒ DỪNG đọc ngay, không nuốt hết phần còn lại của file")]
    public async Task OverCap_StopsReadingImmediately()
    {
        var reader = new CountingReader(100_000);

        await Assert.ThrowsAsync<ImportRowLimitExceededException>(
            () => ReadAllAsync(Accepted(reader, maxRows: 10)));

        // Reader chỉ được quay tay đúng một lần quá trần rồi dừng — KHÔNG chạy hết 100.000 dòng.
        Assert.Equal(11, reader.RowsYielded);
    }

    /// <summary>
    /// Lượt chọn bị TỪ CHỐI (<c>Reader = null</c>) mà vẫn gọi đường đọc là lỗi của NƠI GỌI — nó
    /// quên kiểm <c>IsAccepted</c>. Ném một thông điệp nói thẳng phải sửa gì, thay vì
    /// <c>NullReferenceException</c> câm.
    /// </summary>
    [Fact(DisplayName = "Lượt chọn đã bị từ chối ⇒ ném InvalidOperationException, không phải NRE")]
    public async Task RejectedSelection_ThrowsInvalidOperation()
    {
        var selection = new ImportFileReaderSelection(
            null, ImportFileRejection.UnsupportedFormat, 1024, 10L * 1024 * 1024, 20_000);

        await Assert.ThrowsAsync<InvalidOperationException>(() => ReadAllAsync(selection));
    }
}

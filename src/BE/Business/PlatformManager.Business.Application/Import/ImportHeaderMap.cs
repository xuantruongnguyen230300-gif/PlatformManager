using PlatformManager.Core.Application.Import;

namespace PlatformManager.Business.Application.Import;

/// <summary>
/// Bảng tra <i>tên cột mong đợi → khoá thật trong dictionary của reader</i>.
///
/// <para><b>Vì sao phải có một bảng tra thay vì tra thẳng:</b> reader của Core trả dictionary khoá
/// theo header đã cắt khoảng trắng, so sánh <c>Ordinal</c>
/// (<c>CsvImportFileReader</c>/<c>ExcelImportFileReader</c>). Nhưng §6.2 quy định khớp header
/// KHÔNG phân biệt hoa/thường — nên <c>"MÃ"</c>, <c>"mã"</c> và <c>"Mã"</c> đều phải khớp cột
/// <see cref="ImportColumns.Code"/>. Tra thẳng bằng khoá gốc sẽ trượt hết và người dùng nhận "file
/// thiếu cột" cho một file có đủ cột.</para>
///
/// <para><b>CÓ phân biệt dấu</b>, cố ý: <c>"Ma"</c> KHÔNG khớp <c>"Mã"</c>. Bỏ dấu ở đây sẽ làm
/// <c>Thẩm định</c> và một cột tưởng tượng tên <c>Tham dinh</c> cùng khớp — hai chuỗi người dùng
/// gõ với ý định khác nhau. Đây là chỗ Q47 (tìm kiếm không dấu) CỐ Ý không áp.</para>
///
/// <para>Dựng MỘT lần cho cả file: khoá của mọi dòng giống hệt nhau vì reader dựng chúng từ cùng
/// một dòng header.</para>
/// </summary>
public sealed class ImportHeaderMap
{
    private readonly Dictionary<string, string> _actualKeyByExpectedName;

    private ImportHeaderMap(Dictionary<string, string> actualKeyByExpectedName) =>
        _actualKeyByExpectedName = actualKeyByExpectedName;

    /// <summary>
    /// Dựng bảng tra từ khoá của dòng dữ liệu ĐẦU TIÊN.
    ///
    /// <para>Hai cột trùng tên sau khi chuẩn hoá: giữ cột ĐẦU. Reader đã xử lý ca trùng tên nguyên
    /// văn (cột sau ghi đè cột trước) nên ca còn lại ở đây là trùng-sau-khi-bỏ-hoa-thường; giữ cột
    /// nào cũng là đoán, nên chọn quy tắc xác định được và ghi ra.</para>
    /// </summary>
    public static ImportHeaderMap From(IEnumerable<string> headers)
    {
        ArgumentNullException.ThrowIfNull(headers);

        var map = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var header in headers)
        {
            var normalized = Normalize(header);
            if (normalized.Length == 0)
                continue;

            // TryAdd chứ không gán đè: giữ cột đầu tiên.
            map.TryAdd(normalized, header);
        }

        return new ImportHeaderMap(map);
    }

    /// <summary>File có cột này không.</summary>
    public bool Contains(string column) => _actualKeyByExpectedName.ContainsKey(Normalize(column));

    /// <summary>
    /// Ô của <paramref name="column"/> trong <paramref name="row"/>. Cột vắng mặt khỏi file, hoặc
    /// dòng ngắn hơn header ⇒ <see cref="ImportCellValue.Empty"/> — "không có cột" và "cột có
    /// nhưng ô trống" là cùng một chuyện với luật từng dòng.
    /// </summary>
    public ImportCellValue Cell(IReadOnlyDictionary<string, ImportCellValue> row, string column)
    {
        ArgumentNullException.ThrowIfNull(row);

        return _actualKeyByExpectedName.TryGetValue(Normalize(column), out var key)
               && row.TryGetValue(key, out var cell)
            ? cell
            : ImportCellValue.Empty;
    }

    /// <summary>Nội dung ô ở dạng chữ, đã bỏ dấu gạch "không có dữ liệu" — xem <see cref="ImportColumns.Text"/>.</summary>
    public string? Text(IReadOnlyDictionary<string, ImportCellValue> row, string column) =>
        ImportColumns.Text(Cell(row, column));

    /// <summary>
    /// Cắt khoảng trắng + hạ chữ thường theo <see cref="System.Globalization.CultureInfo.InvariantCulture"/>.
    /// KHÔNG bỏ dấu — xem docstring của lớp.
    ///
    /// <para><c>ToLowerInvariant</c> chứ không <c>ToLower()</c>: kết quả không được đổi theo locale
    /// của máy chạy, nếu không thì cùng một file đọc được trên máy này và "thiếu cột" trên máy kia.</para>
    /// </summary>
    private static string Normalize(string? name) => (name ?? string.Empty).Trim().ToLowerInvariant();
}

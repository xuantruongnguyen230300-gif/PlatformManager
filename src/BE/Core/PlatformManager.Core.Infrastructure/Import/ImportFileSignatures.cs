namespace PlatformManager.Core.Infrastructure.Import;

/// <summary>
/// Chữ ký byte đầu file của các định dạng import nhị phân — nền của lỗi §2a đã sửa
/// (doc/huong_dan/wiki-core/be/15-import-export.md): nhận diện theo NỘI DUNG, không theo phần mở
/// rộng do người dùng đặt.
///
/// <para><b>Giới hạn phải biết:</b> cả hai chữ ký đều KHÔNG riêng của Excel. <c>50 4B 03 04</c> là
/// chữ ký zip nói chung (<c>.docx</c>, <c>.jar</c>, <c>.zip</c> đều có), còn
/// <c>D0 CF 11 E0 A1 B1 1A E1</c> là OLE2 compound file (<c>.doc</c>, <c>.ppt</c>, <c>.msg</c>).
/// Vì vậy chúng chỉ đủ để CHỌN reader; một file zip không phải workbook sẽ vỡ ở bước mở workbook,
/// và đó là lỗi "hỏng định dạng" của cả file — đúng chỗ nó nên vỡ. Đọc hết file để chắc chắn hơn
/// thì mâu thuẫn với chính mục đích: quyết định phải xong TRƯỚC khi đọc nội dung.</para>
/// </summary>
internal static class ImportFileSignatures
{
    /// <summary>OOXML (<c>.xlsx</c>) = kho zip.</summary>
    public static ReadOnlySpan<byte> Zip => [0x50, 0x4B, 0x03, 0x04];

    /// <summary>OLE2 compound file (<c>.xls</c> bản cũ).</summary>
    public static ReadOnlySpan<byte> Ole2 => [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1];

    /// <summary>
    /// Số byte đầu file cần lấy để phân biệt được mọi định dạng ở trên — bằng chữ ký DÀI NHẤT.
    /// </summary>
    public const int HeaderLength = 8;

    /// <summary>
    /// <paramref name="header"/> mở đầu bằng <paramref name="signature"/> hay không. Header ngắn
    /// hơn chữ ký ⇒ <c>false</c>, không ném: file nhỏ hơn 8 byte là chuyện bình thường (file rỗng),
    /// không phải lỗi lập trình.
    /// </summary>
    public static bool StartsWith(ReadOnlySpan<byte> header, ReadOnlySpan<byte> signature) =>
        header.Length >= signature.Length && header[..signature.Length].SequenceEqual(signature);

    /// <summary>Header khớp một trong các định dạng NHỊ PHÂN đã biết — dùng cho nhánh mặc định
    /// (CSV) để nó không nhận nhầm một file Excel bị đổi đuôi thành <c>.csv</c>.</summary>
    public static bool IsKnownBinaryFormat(ReadOnlySpan<byte> header) =>
        StartsWith(header, Zip) || StartsWith(header, Ole2);
}

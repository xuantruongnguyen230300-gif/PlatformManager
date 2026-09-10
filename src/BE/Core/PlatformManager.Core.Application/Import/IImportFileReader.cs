namespace PlatformManager.Core.Application.Import;

/// <summary>
/// Đọc MỘT file import thành dòng dữ liệu TRUNG TÍNH định dạng — key = tên cột (header dòng 1),
/// value = nội dung ô. Bên xử lý không cần biết file gốc là CSV, <c>.xlsx</c> hay <c>.xls</c>.
///
/// <para><b>Core dừng ở đây.</b> Cột nào bắt buộc, giá trị hợp lệ là gì, ghi vào entity nào — toàn
/// bộ phần đó thuộc nghiệp vụ gọi. Seam này không có khái niệm nào về danh mục, kỳ báo cáo hay
/// bảng đích; nó chỉ biết "file có header và có dòng". Ranh giới:
/// doc/huong_dan/wiki-core/be/15-import-export.md §1.</para>
///
/// <para><b>Ba hiện thực</b> (Core.Infrastructure, vì cả CsvHelper lẫn NPOI đều là thư viện ngoài
/// và tầng Application phải sạch thư viện): CSV, <c>.xlsx</c>, <c>.xls</c>. Thêm định dạng thứ tư
/// về sau chỉ là thêm một hiện thực nữa — không nơi nào phải sửa một câu <c>switch</c>, vì việc
/// chọn reader đi qua <see cref="IImportFileReaderSelector"/>.</para>
/// </summary>
public interface IImportFileReader
{
    /// <summary>
    /// Reader này có đọc được file đó không.
    ///
    /// <para><b><paramref name="header"/> đứng TRƯỚC <paramref name="fileName"/> có chủ đích, và
    /// đây là lỗi §2a của doc/huong_dan/wiki-core/be/15-import-export.md đã được sửa.</b> Bản cũ
    /// chỉ so <c>Path.GetExtension()</c>, nên một file <c>.xlsx</c> bị đổi tên thành <c>.xls</c> sẽ
    /// được giao cho bộ đọc OLE2 và vỡ bằng một exception không ai đọc ra nguyên nhân. Phần mở rộng
    /// do người dùng đặt — nó là một GỢI Ý, không phải bằng chứng về nội dung.</para>
    /// </summary>
    /// <param name="header">Vài byte đầu file (bên gọi lấy giúp, xem
    /// <see cref="IImportFileReaderSelector"/>). Có thể NGẮN HƠN yêu cầu, kể cả rỗng, nếu file
    /// nhỏ — hiện thực phải tự kiểm độ dài trước khi so.</param>
    /// <param name="fileName">Tên file gốc. Chỉ dùng cho định dạng KHÔNG có chữ ký nhận biết
    /// được (CSV); định dạng nhị phân thì so chữ ký, không so đuôi.</param>
    bool CanRead(ReadOnlySpan<byte> header, string fileName);

    /// <summary>
    /// Đọc từng dòng dữ liệu, KHÔNG bao gồm dòng header.
    ///
    /// <para><b>Hợp đồng: reader KHÔNG bỏ dòng nào, kể cả dòng rỗng.</b> Seam không mang số thứ tự
    /// dòng, nên bên gọi đếm theo thứ tự yield ra (dòng dữ liệu thứ n = dòng n+1 của file). Bỏ qua
    /// dòng rỗng ở đây sẽ làm mọi thông báo lỗi cấp dòng của bên gọi trỏ nhầm dòng — sai lệch tăng
    /// dần và không có gì báo.</para>
    ///
    /// <para>Cột có header rỗng bị BỎ (không có tên thì không tra được). Hai cột trùng tên: cột
    /// đứng sau ghi đè cột đứng trước.</para>
    ///
    /// <para><b>Ném khi file hỏng định dạng / không đọc được</b> — đó là lỗi của CẢ file, khác hẳn
    /// lỗi của một dòng. Bên gọi bắt ở vòng ngoài và đánh dấu cả lượt import là thất bại; lỗi từng
    /// dòng thì bắt riêng quanh mỗi phần tử yield ra từ đây.</para>
    /// </summary>
    IAsyncEnumerable<IReadOnlyDictionary<string, ImportCellValue>> ReadAsync(
        Stream stream, string fileName, CancellationToken ct);
}

namespace PlatformManager.Business.Application.Dashboard;

/// <summary>
/// Một dòng của FILE XUẤT — <b>12 cột, projection RIÊNG, không dùng lại <c>table[]</c> của DB-1</b>
/// (Q68, doc/contracts/dashboard.md §"Projection của export là RIÊNG").
///
/// <para><b>Ba trường mà <c>DashboardTableRowDto</c> KHÔNG có</b> —
/// <see cref="OwnerName"/> · <see cref="Deadline"/> · <see cref="ProgressPercent"/>: bảng chi tiết
/// trên màn chỉ hiện 9 cột (Q8), còn file xuất phải đủ 12 để <b>nạp ngược lại được</b>.</para>
///
/// <para><b>Vì sao không phình <c>table[]</c> cho tiện:</b> ba trường đó không có cột nào trên màn
/// để hiện, nên thêm vào là bắt MỌI lần tải Dashboard mang theo dữ liệu không ai đọc — cộng một
/// lần tra tên người cho từng dòng mà màn hình không dùng tới. Chiều ngược cũng sai: bỏ ba cột khỏi
/// file xuất là phá round-trip export → sửa → import.</para>
///
/// <para>⚠️ <b>Hai đường vẫn dùng CHUNG object bộ lọc và CHUNG luật chọn bản ghi đại diện</b> (§5.2
/// + Q46). Chỉ bộ trường chiếu ra là khác. Kiểm nghiệm thu <i>"số dòng file = số phần tử
/// <c>data.table</c>"</i> là thứ DUY NHẤT cưỡng chế điều đó — hai projection rời nhau nghĩa là sửa
/// bộ lọc ở một đường sẽ KHÔNG làm đường kia đỏ, trừ kiểm này.</para>
/// </summary>
public sealed record DashboardExportRow(
    string Code,
    string Name,
    string GroupName,
    decimal MaxScore,
    decimal? SelfScore,
    decimal? VerifiedScore,
    decimal? Diff,
    string? Status,
    string? OwnerName,
    DateOnly? Deadline,
    int? ProgressPercent,
    string? Note);

/// <summary>
/// Khối nhận dạng kỳ ở đầu file — 10 dòng đầu của sheet
/// (spec/dashboard-dti/business-rules.md §4.2).
///
/// <para><b>Dựng ở tầng Application, không ở bộ ghi Excel:</b> đây là NỘI DUNG nghiệp vụ (nhãn kỳ,
/// khoảng ngày, danh sách tuần giao với tháng, mô tả bộ lọc), còn bộ ghi chỉ biết đặt chữ vào ô.
/// Ranh giới đó là thứ giữ cho lượt dựng seam <c>ITabularWriter</c> về sau không phải kéo theo
/// khái niệm "kỳ báo cáo" vào Core.</para>
/// </summary>
/// <param name="SheetName">
/// <c>Tuần 33-2026</c> · <c>Tháng 8-2026</c> — dấu <b>gạch ngang</b>, không phải <c>/</c>: Excel cấm
/// <c>/ \ ? * [ ]</c> trong tên sheet.
/// </param>
/// <param name="FileName">Chỉ ASCII, không dấu, không khoảng trắng — xem docstring của handler.</param>
/// <param name="PeriodScopeLabel">
/// Nhãn dòng 5, ĐỔI THEO CHẾ ĐỘ và đó là chủ đích: file tuần cần biết tuần đó thuộc tháng nào; file
/// tháng cần biết nó gộp những tuần nào.
/// </param>
/// <param name="PeriodScopeValue">Giá trị của dòng 5.</param>
/// <param name="FilterSummary">
/// Dòng 10 <c>Bộ lọc đang áp</c> — <b>BẮT BUỘC, kể cả khi không lọc</b>. Lý do nó nằm trong FILE
/// chứ không chỉ trên màn hình: <b>file sống lâu hơn màn hình</b>. Người mở file tuần sau, hoặc
/// người nhận nó qua email, không có cách nào biết nó được xuất lúc đang lọc gì; một tooltip không
/// đi theo file. Dòng VẮNG MẶT thì không phân biệt được với "quên ghi".
/// </param>
public sealed record DashboardExportLayout(
    string SheetName,
    string FileName,
    string PeriodLabel,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    string PeriodScopeLabel,
    string PeriodScopeValue,
    int Year,
    int CriteriaCount,
    DateTimeOffset ExportedAt,
    string ExportedBy,
    string FilterSummary);

/// <summary>File đã dựng xong, sẵn sàng stream về client.</summary>
public sealed record DashboardExportFile(string FileName, string ContentType, byte[] Content);

/// <summary>
/// Bộ ghi file <c>.xlsx</c> của DB-4.
///
/// <para>🛑 <b>Hiện thực dùng NPOI TRỰC TIẾP ở <c>Business.Infrastructure</c>, KHÔNG qua seam
/// <c>ITabularWriter</c> của Core</b> — chốt Q9 (2026-09-09),
/// doc/huong_dan/wiki-core/be/15-import-export.md §7. Seam đó hoãn tới khi có người tiêu thụ thứ
/// hai, vì chữ ký của nó (danh sách cột + luồng dòng) chỉ diễn đạt được <i>một bảng phẳng bắt đầu
/// từ ô đầu tiên</i>, còn bố cục ở đây có khối nhận dạng kỳ nhiều dòng, một dòng <c>TỔNG CỘNG</c>,
/// nền màu cho hàng header và tên sheet đổi theo kỳ.</para>
///
/// <para><b>Ràng buộc vẫn còn hiệu lực dù chưa có seam:</b> NPOI chỉ được reference ở tầng
/// <c>*.Infrastructure</c> — không kéo vào <c>Business.Application</c>, không kéo vào Core. Interface
/// này là ranh giới đó.</para>
/// </summary>
public interface IDashboardExportWriter
{
    byte[] Write(DashboardExportLayout layout, IReadOnlyList<DashboardExportRow> rows);
}

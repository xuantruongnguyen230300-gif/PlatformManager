namespace PlatformManager.Business.Application.Dashboard;

/// <summary>
/// Năm ô KPI của Dashboard — doc/contracts/dashboard.md DB-1 và
/// spec/dashboard-dti/business-rules.md §1.4 + §1.6.
///
/// <para><b><c>—</c> và <c>0</c> là HAI thứ khác nhau, và khác biệt đó phải sống sót tới màn
/// hình.</b> <see cref="OverallProgress"/> và <see cref="Delta"/> là <c>decimal?</c> nên khi
/// chưa có dữ liệu chúng VẮNG MẶT khỏi JSON; ba ô đếm luôn có số. Trả <c>0</c> cho ô đầu thay vì
/// bỏ trường đi sẽ làm dashboard tuyên bố "tiến độ toàn xã: 0%" ngay sau khi nạp một file có 26
/// chỉ tiêu đã hoàn thành — một câu sai, hiển thị tự tin, và không ai kiểm lại vì nó trông như
/// một con số bình thường.</para>
/// </summary>
/// <param name="Up">Số chỉ tiêu có <c>Δ progressPercent &gt; ε</c> so với kỳ trước.</param>
/// <param name="Flat">
/// <c>|Δ| ≤ ε</c>. ⚠️ Ngay sau import ô này là <b>0</b>, KHÔNG phải 62: chỉ tiêu thiếu dữ liệu ở
/// MỘT TRONG HAI kỳ thì không vào <c>up</c>/<c>flat</c>/<c>down</c> nào cả, mà lúc đó cả hai kỳ
/// đều thiếu. <c>flat</c> nghĩa là <i>"đã đo hai lần và không đổi"</i>, không phải
/// <i>"chưa đo lần nào"</i>.
/// </param>
/// <param name="Down">
/// KHÔNG có ô KPI nào trên màn hình (bộ 5 ô đã chốt ở Q22) — vẫn nằm trong response vì
/// <c>up</c>/<c>flat</c>/<c>down</c> là một bộ ba, cắt một phần tử ra khỏi phép đếm làm luật §1.4
/// khó đọc.
/// </param>
/// <param name="Done">
/// Đếm theo <c>status = "Hoàn thành"</c> — trạng thái người dùng chọn tay, KHÔNG suy từ điểm.
/// Đây là ô DUY NHẤT có số thật ngay sau import, vì <c>status</c> đến thẳng từ cột
/// <c>Trạng thái</c> của file (§1.6).
/// </param>
public sealed record DashboardKpiDto(
    decimal? OverallProgress,
    decimal? Delta,
    string? PreviousPeriodLabel,
    int Up,
    int Flat,
    int Down,
    int Done,
    int TotalCriteria);

/// <summary>Tiến độ theo nhóm. <c>Progress</c> vắng mặt = nhóm chưa có chỉ tiêu nào có <c>Tiến độ %</c>.</summary>
public sealed record DashboardGroupProgressDto(Guid GroupId, string GroupCode, string GroupName, decimal? Progress);

/// <summary>
/// Một điểm của biểu đồ xu hướng — Q43 + Q44.
/// </summary>
/// <param name="Period">KHOÁ định danh của kỳ: <c>"YYYY-Www"</c> | <c>"YYYY-MM"</c>.</param>
/// <param name="PeriodLabel">
/// Nhãn trục X BE DỰNG SẴN: <c>06/07 – 12/07</c> (chế độ tuần) | <c>Th.1</c> (chế độ tháng/năm).
/// Trùng tên với <c>periodLabel</c> ở cấp gốc là CÓ CHỦ ĐÍCH — cùng vai "chuỗi hiển thị của một
/// kỳ, BE dựng" — nhưng khuôn KHÁC (cấp gốc là nhãn kỳ đầy đủ).
/// </param>
/// <param name="Value">
/// <c>null</c> = kỳ KHÔNG có dữ liệu (Q44) ⇒ khoá <c>value</c> vắng mặt trên dây, nhưng
/// <b>phần tử của kỳ VẪN có mặt</b>. Đừng "dọn" các điểm rỗng cho gọn: trên trục category của
/// <c>chart.js</c>, bỏ hẳn một kỳ thì trục mất luôn ô của kỳ đó và hai điểm kề nhau được nối
/// THẲNG — tức chuỗi thưa vẽ ra đúng thứ mà "không nội suy" muốn cấm.
/// </param>
public sealed record DashboardTrendPointDto(string Period, string PeriodLabel, decimal? Value);

/// <summary>
/// Một dòng của BẢNG CHI TIẾT — <b>đúng 9 cột</b> đã duyệt (Q8): Mã · Chỉ tiêu · Nhóm ·
/// Điểm tối đa · Tự đánh giá · Thẩm định · Chênh lệch · Trạng thái · Minh chứng/Ghi chú.
///
/// <para>⚠️ <b>KHÔNG thêm <c>ownerName</c>/<c>deadline</c>/<c>progressPercent</c> vào đây</b>
/// (Q68). File xuất cần 12 cột, nhưng nó dùng một PROJECTION RIÊNG — phình <c>table[]</c> là bắt
/// MỌI lần tải Dashboard mang theo dữ liệu không ai đọc, cộng một lần tra tên người cho từng dòng
/// mà màn hình không dùng tới.</para>
/// </summary>
public sealed record DashboardTableRowDto(
    Guid CriteriaId,
    string Code,
    string Name,
    Guid GroupId,
    string GroupCode,
    string GroupName,
    decimal MaxScore,
    decimal? SelfScore,
    decimal? VerifiedScore,
    decimal? Diff,
    string? Status,
    string? Note);

/// <summary>Payload của DB-1.</summary>
/// <param name="Mode"><c>"week"</c> | <c>"month"</c> | <c>"year"</c> — chữ thường.</param>
/// <param name="PeriodStart">Mốc kỳ GHI RÕ, không để FE tự tính (Q12).</param>
public sealed record DashboardAggregateDto(
    string Mode,
    string PeriodLabel,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    DashboardKpiDto Kpi,
    IReadOnlyList<DashboardGroupProgressDto> Groups,
    IReadOnlyList<DashboardTrendPointDto> Trend,
    IReadOnlyList<DashboardTableRowDto> Table);

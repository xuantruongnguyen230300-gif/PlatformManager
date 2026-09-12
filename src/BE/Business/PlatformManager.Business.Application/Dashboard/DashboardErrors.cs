using PlatformManager.Core.Application.Common.Results;

namespace PlatformManager.Business.Application.Dashboard;

/// <summary>
/// Catalog mã lỗi miền <c>DASHBOARD.*</c> — doc/contracts/dashboard.md §"Mã lỗi của DB-1".
/// Cưỡng chế bằng máy giống <see cref="Criteria.CriteriaErrors"/>; xem docstring ở đó.
///
/// <para><b>Người dùng bình thường KHÔNG gặp ba mã này (Q62):</b> FE đưa tham số lạ trên URL về
/// mặc định TRƯỚC khi gọi API, nên chúng là lưới chặn cuối ở BE và không cần câu hiển thị.
/// Chúng vẫn phải có ở BE — lưới chặn chỉ có tác dụng khi nó tồn tại.</para>
///
/// <para><c>DASHBOARD.EXPORT_MODE_UNSUPPORTED</c> (DB-4) khai 2026-09-11, CÙNG LƯỢT với endpoint
/// export — đúng luật "không khai mã trước khi có nơi ném" ở <c>CriteriaErrors</c>.</para>
/// </summary>
public static class DashboardErrors
{
    /// <summary>
    /// <c>mode</c> không thuộc <c>"week"</c> / <c>"month"</c> / <c>"year"</c>, KỂ CẢ sai
    /// hoa/thường. <c>mode</c> vắng mặt cũng ra mã này — cả hai ca đều kết thúc bằng "gửi lại một
    /// <c>mode</c> hợp lệ", nên không cần một mã <c>..._REQUIRED</c> riêng.
    ///
    /// <para>⚠️ KHÁC <c>DASHBOARD.EXPORT_MODE_UNSUPPORTED</c>: mã này nghĩa là <i>"giá trị này
    /// không phải một mode"</i>; mã kia nghĩa là <i>"<c>year</c> là mode hợp lệ, nhưng endpoint
    /// export không phục vụ nó"</i>. Gộp làm một thì câu FE hiện cho người dùng sai ở đúng ca hay
    /// gặp: bấm Xuất khi đang xem <c>Tất cả</c>.</para>
    /// </summary>
    public static readonly ErrorDescriptor ModeInvalid = new(
        "DASHBOARD.MODE_INVALID", ErrorCode.ValidationError,
        "Chế độ xem '{Mode}' không hợp lệ — chỉ nhận week/month/year.");

    /// <summary>Cùng luật với <c>CRITERIA.STATUS_INVALID</c>, cho bộ lọc bảng chi tiết của DB-1.</summary>
    public static readonly ErrorDescriptor StatusInvalid = new(
        "DASHBOARD.STATUS_INVALID", ErrorCode.ValidationError,
        "Trạng thái '{Status}' không hợp lệ.");

    /// <summary>
    /// Q61 — <c>mode=week</c> mà <c>year</c> KHÁC năm ISO của tuần chứa <c>date</c>. Ví dụ
    /// <c>date = 2025-12-29</c> thuộc tuần 1/2026, nên <c>year</c> phải là <c>2026</c>.
    ///
    /// <para>Không mã nào có sẵn phủ được ca này: <c>MODE_INVALID</c> nói về <c>mode</c>, còn
    /// model binder chỉ bắt sai KIỂU chứ không bắt hai giá trị đúng kiểu mà lệch nhau. Bỏ trống
    /// <c>year</c> thì KHÔNG có gì để mà lệch — Q63, lấy chính năm ISO của tuần đó.</para>
    /// </summary>
    public static readonly ErrorDescriptor PeriodYearMismatch = new(
        "DASHBOARD.PERIOD_YEAR_MISMATCH", ErrorCode.ValidationError,
        "Năm '{Year}' không phải năm ISO của tuần chứa ngày '{Date}' (năm ISO là '{IsoYear}').");

    /// <summary>
    /// DB-4 — <c>mode=year</c> là mode HỢP LỆ của DB-1 nhưng endpoint export không phục vụ nó.
    ///
    /// <para>Bố cục file đã duyệt có khối nhận dạng kỳ với <c>Từ ngày</c>/<c>Đến ngày</c> của MỘT
    /// kỳ (Q14); "cả năm" không ánh xạ được vào khuôn đó mà không thiết kế lại file.</para>
    ///
    /// <para>⚠️ KHÁC <see cref="ModeInvalid"/>, và đây là lý do chúng là hai mã: mã kia nghĩa là
    /// <i>"giá trị này không phải một mode"</i>; mã này nghĩa là <i>"year là mode hợp lệ, nhưng
    /// export không phục vụ nó"</i>. Gộp làm một thì câu FE hiện cho người dùng sai ở đúng ca hay
    /// gặp: bấm Xuất khi đang xem <c>Tất cả</c>.</para>
    ///
    /// <para>⚠️ Mã này KHÔNG liên quan tới Q37. <c>mode=month</c> vẫn xuất TRỌN tháng — xuất một
    /// tháng là phép tổng hợp trên dữ liệu đã có, không tạo bản ghi nào.</para>
    /// </summary>
    public static readonly ErrorDescriptor ExportModeUnsupported = new(
        "DASHBOARD.EXPORT_MODE_UNSUPPORTED", ErrorCode.ValidationError,
        "Chế độ '{Mode}' không xuất được báo cáo — chỉ xuất theo tuần hoặc tháng.");
}

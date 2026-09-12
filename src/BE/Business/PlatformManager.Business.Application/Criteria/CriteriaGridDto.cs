using PlatformManager.Business.Application.Common;
using PlatformManager.Core.Application.Common.Models;

namespace PlatformManager.Business.Application.Criteria;

/// <summary>
/// Payload của DM-2 — <c>PagedList&lt;CriteriaRowDto&gt;</c> <b>CỘNG THÊM</b> khối quyền ghi cấp
/// màn. Đây là ngoại lệ ĐÃ ĐĂNG KÝ của luật shape phân trang (doc/contracts/danh-muc-dti.md §0):
/// bốn trường <c>items</c>/<c>totalCount</c>/<c>page</c>/<c>pageSize</c> giữ nguyên tên, nguyên
/// vị trí, nguyên TẦNG — nên mapper lưới dùng chung của FE chạy nguyên vẹn. Kế thừa
/// <see cref="PagedList{T}"/> chứ không LỒNG nó vào một object khác: lồng là phá mapper dùng
/// chung, cộng thêm thì không.
///
/// <para><b>Ba trường quyền, nhưng KHÔNG ba nguồn sự thật.</b> <see cref="CanWrite"/> là nguồn;
/// hai trường kia SUY RA từ nó trong CÙNG một lần đánh giá của request — bất biến đầy đủ ở
/// <see cref="Evaluate"/>.</para>
/// </summary>
public sealed class CriteriaGridDto : PagedList<CriteriaRowDto>
{
    /// <summary>Người gọi được phép GHI dữ liệu DTI (Q39). FE dùng để hiện/ẩn nút ghi.</summary>
    public bool CanWrite { get; init; }

    /// <summary>
    /// <c>= CanWrite VÀ EditBlockedBy rỗng</c>. FE dùng để bật/tắt sửa inline và nút Lưu.
    /// </summary>
    public bool IsEditable { get; init; }

    /// <summary>
    /// LUÔN có mặt; <c>[]</c> khi <see cref="IsEditable"/>. ⚠️ Trả <c>[]</c> chứ KHÔNG trả
    /// <c>null</c>: luật "null ⇒ khoá biến mất" áp cho <c>null</c>, không áp cho mảng rỗng — và
    /// chỉ cần BE trả <c>null</c> là FE nhận <c>undefined</c> rồi mọi chỗ <c>.includes(...)</c>
    /// ném lỗi.
    /// </summary>
    public IReadOnlyList<string> EditBlockedBy { get; init; } = [];

    /// <summary>
    /// Kỳ đang xem có thuộc NĂM HIỆN TẠI không (Q66). FE chọn BIẾN THỂ câu của lời nhắc
    /// <c>PERIOD_NOT_WEEKLY</c> theo đây (Q60), KHÔNG suy từ đồng hồ máy khách — cùng lý do Q40
    /// bắt server quy đổi kỳ: một sự thật về thời gian thì chỉ một bên được cầm.
    /// </summary>
    public bool IsCurrentYear { get; init; }

    /// <summary>
    /// KỲ HIỆN TẠI — tuần ISO chứa HÔM NAY, khuôn <c>"YYYY-Www"</c> (Q72). Đây là kỳ mà một lời
    /// ghi sẽ rơi vào khi người dùng đang ở chế độ <c>Tất cả</c> (§5.3).
    ///
    /// <para><b>Vì sao phải có trường này</b>: dải băng "nhắc kỳ đích" cần nói lời ghi sẽ rơi vào
    /// TUẦN NÀO, và trước Q72 không nguồn nào trả lời được — <see cref="IsCurrentYear"/> chỉ nói
    /// về NĂM; <c>weeksInYear[]</c> của DB-3 không có cờ nào đánh dấu tuần hiện tại và còn RỖNG
    /// khi năm chưa có dữ liệu; còn đồng hồ máy khách thì bị cấm (cùng lý do Q40 — lịch ISO chỉ
    /// có một bản cài).</para>
    ///
    /// <para><b><c>required</c> là có chủ đích:</b> hợp đồng khai <i>"không bao giờ vắng mặt —
    /// kể cả lưới rỗng, kể cả <c>canWrite = false</c>"</i>. Để mặc định <c>string.Empty</c> thì
    /// quên gán sẽ ra một chuỗi rỗng trên dây, tức khoá CÓ mặt nhưng vô nghĩa — hỏng im lặng.
    /// Với <c>required</c>, quên gán là lỗi BIÊN DỊCH.</para>
    /// </summary>
    public required string CurrentPeriod { get; init; }

    /// <summary>
    /// Nhãn kỳ đầy đủ của <see cref="CurrentPeriod"/>, BE dựng sẵn
    /// (<c>Tuần 33/2026 (10/08 – 16/08/2026)</c> — khuôn ở
    /// spec/dashboard-dti/business-rules.md §6.2). Cùng ràng buộc "không bao giờ vắng mặt".
    /// </summary>
    public required string CurrentPeriodLabel { get; init; }

    /// <summary>
    /// Tính khối quyền cấp màn theo đúng bất biến của DM-2 mục 3 — MỘT chỗ, để hai handler (và
    /// vòng 2) không dựng lại một nửa.
    ///
    /// <code>
    /// isEditable = true   ⟺  canWrite = true  VÀ  editBlockedBy = []
    /// canWrite   = false  ⇒  editBlockedBy = ["NO_WRITE_PERMISSION"]  (ĐÚNG một phần tử)
    /// canWrite   = true   ⇒  editBlockedBy ⊆ ["PERIOD_NOT_WEEKLY", "PERIOD_OUT_OF_YEAR"]
    /// </code>
    ///
    /// <para>Với ba mã hôm nay, hai mã lọc LOẠI TRỪ NHAU: <c>PERIOD_OUT_OF_YEAR</c> chỉ sinh khi
    /// <c>period = "all"</c>, còn <c>PERIOD_NOT_WEEKLY</c> chỉ sinh khi kỳ là tháng — một
    /// <c>period</c> không thể là cả hai (Q48). Shape mảng giữ nguyên vì nó là chỗ cho một điều
    /// kiện THỨ TƯ về sau, không phải cho ca hai mã lọc cùng trượt.</para>
    /// </summary>
    public static WriteAccessBlock Evaluate(bool canWrite, PeriodRange period, DateOnly today)
    {
        // Kỳ hiện tại tính CÙNG chỗ, CÙNG lần đánh giá với ba cờ kia (Q72). Đó là toàn bộ lý do
        // hai trường này ở DM-2 chứ không ở DB-3: cả năm giá trị trả lời cùng một câu hỏi — "lời
        // ghi của tôi sẽ đi đâu, và có đi được không" — nên chúng không được phép sinh ra ở hai
        // thời điểm khác nhau. Ghép từ hai response là mở cửa cho ca tuần hiện tại đổi giữa hai
        // lời gọi.
        var currentPeriod = PeriodRange.IsoWeekOf(today);
        var currentYear = today.Year;
        var isCurrentYear = period.Year == currentYear;

        if (!canWrite)
        {
            return new WriteAccessBlock(
                false, false, [EditBlockReasons.NoWritePermission], isCurrentYear,
                currentPeriod.Value, PeriodLabels.Full(currentPeriod));
        }

        var blocked = new List<string>(1);

        // Thứ tự liệt kê là thứ tự cố định của bảng ở DM-2 mục 3 — giữ nguyên để một điều kiện
        // thứ tư về sau không phải đổi hợp đồng.
        if (period.Kind == PeriodKind.Month)
            blocked.Add(EditBlockReasons.PeriodNotWeekly);

        if (period.Kind == PeriodKind.All && !isCurrentYear)
            blocked.Add(EditBlockReasons.PeriodOutOfYear);

        return new WriteAccessBlock(
            true, blocked.Count == 0, blocked, isCurrentYear,
            currentPeriod.Value, PeriodLabels.Full(currentPeriod));
    }
}

/// <summary>
/// Kết quả của <see cref="CriteriaGridDto.Evaluate"/> — sáu giá trị đi CÙNG NHAU, sinh ra trong
/// một lần đánh giá duy nhất. Gói chúng vào một kiểu thay vì trả rời là cách ép điều đó: không
/// nơi gọi nào lấy được bốn cờ mà bỏ quên hai trường kỳ, hay ngược lại.
/// </summary>
public sealed record WriteAccessBlock(
    bool CanWrite,
    bool IsEditable,
    IReadOnlyList<string> EditBlockedBy,
    bool IsCurrentYear,
    string CurrentPeriod,
    string CurrentPeriodLabel);

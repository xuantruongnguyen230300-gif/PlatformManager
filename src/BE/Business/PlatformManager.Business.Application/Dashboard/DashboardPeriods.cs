using System.Globalization;
using PlatformManager.Business.Application.Common;

namespace PlatformManager.Business.Application.Dashboard;

/// <summary>Ba chế độ xem của Dashboard — doc/contracts/dashboard.md DB-1.</summary>
public enum DashboardMode
{
    Week,
    Month,

    /// <summary><c>"year"</c> — nhãn trên màn hình là "Tất cả", phạm vi là trọn một năm.</summary>
    Year,
}

/// <summary>
/// Quy tham số <c>mode</c>/<c>date</c>/<c>year</c> của DB-1 về một <see cref="PeriodRange"/>, và
/// dựng cửa sổ <c>trend</c>. Tách khỏi handler để phần dễ sai nhất (Q54 · Q57 · Q61 · Q63) có
/// test thuần, không cần DB.
/// </summary>
public static class DashboardPeriods
{
    /// <summary>
    /// Số kỳ của biểu đồ xu hướng ở CHẾ ĐỘ TUẦN — Q54 (2026-09-10, người dùng chọn 12; thiết kế
    /// đã duyệt vẽ 6). Cửa sổ KẾT THÚC ở tuần đang xem, không phải tuần hiện tại của lịch.
    /// </summary>
    public const int WeekTrendWindow = 12;

    public static bool TryParseMode(string? mode, out DashboardMode parsed)
    {
        parsed = DashboardMode.Week;

        // So ORDINAL, chữ thường: "Week" hay "WEEK" đều là MODE_INVALID. Hợp đồng khai rõ "chữ
        // thường", và một bộ đọc rộng rãi ở đây sẽ dạy client gửi bất cứ dạng nào rồi khoá luôn
        // khả năng siết lại về sau.
        switch (mode)
        {
            case "week":
                parsed = DashboardMode.Week;
                return true;
            case "month":
                parsed = DashboardMode.Month;
                return true;
            case "year":
                parsed = DashboardMode.Year;
                return true;
            default:
                return false;
        }
    }

    public static string ToWireValue(DashboardMode mode) => mode switch
    {
        DashboardMode.Week => "week",
        DashboardMode.Month => "month",
        _ => "year",
    };

    /// <summary>
    /// Kỳ ĐANG XEM của chế độ TUẦN — sáu hàng của bảng ở doc/contracts/dashboard.md DB-1
    /// §"Mã lỗi", khối <b>Q71</b>. Trả <c>false</c> ⇒ <c>400 DASHBOARD.PERIOD_YEAR_MISMATCH</c>.
    ///
    /// <list type="bullet">
    ///   <item><b>Q63</b> — có <paramref name="date"/>, không <paramref name="year"/>: tuần ISO
    ///   chứa ngày đó. Không có gì để mà lệch.</item>
    ///   <item><b>Q61</b> — có CẢ HAI mà <paramref name="year"/> khác NĂM ISO của tuần chứa ngày
    ///   đó: <c>400</c>. Ví dụ <c>2025-12-29</c> thuộc tuần 1 của <b>2026</b>.</item>
    ///   <item><b>Q71</b> — không <paramref name="date"/>, có <paramref name="year"/> khác năm
    ///   ISO hiện tại: <b>tuần ISO CUỐI</b> của năm đó.</item>
    ///   <item>Không <paramref name="date"/>, năm trùng năm ISO hiện tại (hoặc không
    ///   <paramref name="year"/>): tuần hiện tại.</item>
    /// </list>
    ///
    /// <para>🔴 <b><c>400</c> CHỈ sinh ở hàng 3</b> — khi gửi CẢ HAI mà lệch. Bản cài trước
    /// 2026-09-10 neo <c>date</c> mặc định vào <i>hôm nay</i> rồi áp Q61, nên nó bắn <c>400</c>
    /// cho cả hàng 5 — và hàng 5 là đường đi HOÀN TOÀN HỢP LỆ: DB-3 khai <i>"năm không có dữ
    /// liệu ⇒ <c>weeksInYear</c> RỖNG, 200, không phải lỗi"</i>, nên FE KHÔNG CÓ <c>date</c> nào
    /// để gửi. Đó là lỗi tích hợp đã xảy ra thật, Q71 sửa nó. Đừng khôi phục
    /// <c>date ?? today</c>: nó nhìn gọn hơn và sai ở đúng ca người dùng gặp.</para>
    ///
    /// <para><b>Vì sao tuần CUỐI chứ không phải tuần 1:</b> khớp ngữ nghĩa "mới nhất" mà cả mô
    /// hình dùng (§5.2 lấy bản ghi có <c>AssessmentDate</c> lớn nhất), và cho <c>trend</c> một cửa
    /// sổ 12 tuần đầy đủ — Q57 cắt cửa sổ ở ĐẦU năm, nên neo vào tuần 1 sẽ trả về một trục chỉ có
    /// một điểm.</para>
    /// </summary>
    public static bool TryResolveWeek(
        DateOnly? date, int? year, DateOnly today, out PeriodRange period, out int isoYear)
    {
        if (date is { } anchor)
        {
            var asDateTime = anchor.ToDateTime(TimeOnly.MinValue);

            isoYear = ISOWeek.GetYear(asDateTime);
            period = PeriodRange.IsoWeek(isoYear, ISOWeek.GetWeekOfYear(asDateTime));

            // Hàng 1 (year vắng ⇒ Q63) và hàng 2 (khớp) đi tiếp; hàng 3 (lệch) ra 400.
            return year is null || year.Value == isoYear;
        }

        // KHÔNG có `date` ⇒ không bao giờ 400: không có hai giá trị nào để mà lệch nhau.
        //
        // So với NĂM ISO của hôm nay, không phải năm dương lịch — cuối tháng 12 hai giá trị đó
        // khác nhau (29/12/2025 thuộc năm ISO 2026), và dùng năm dương lịch ở đây sẽ đẩy người
        // đang xem "năm 2026" sang nhánh "tuần cuối của 2026" trong khi tuần hiện tại CHÍNH LÀ
        // một tuần của 2026.
        var currentIsoYear = ISOWeek.GetYear(today.ToDateTime(TimeOnly.MinValue));
        isoYear = year ?? currentIsoYear;

        period = isoYear == currentIsoYear
            ? PeriodRange.IsoWeekOf(today)                                      // hàng 4 và hàng 6
            : PeriodRange.IsoWeek(isoYear, ISOWeek.GetWeeksInYear(isoYear));    // hàng 5 — Q71

        return true;
    }

    /// <summary>
    /// Kỳ đang xem của chế độ THÁNG. <paramref name="date"/> có thì lấy tháng của nó; không thì
    /// lấy tháng hiện tại của năm đang lọc.
    /// </summary>
    public static PeriodRange ResolveMonth(DateOnly? date, int? year, DateOnly today) =>
        date is { } value
            ? PeriodRange.CalendarMonth(value.Year, value.Month)
            : PeriodRange.CalendarMonth(year ?? today.Year, today.Month);

    /// <summary>Kỳ đang xem của chế độ NĂM.</summary>
    public static PeriodRange ResolveYear(int? year, DateOnly today) =>
        PeriodRange.WholeYear(year ?? today.Year);

    /// <summary>
    /// Cửa sổ các kỳ của biểu đồ xu hướng — Q44 (trả ĐỦ các kỳ, kỳ rỗng vẫn có phần tử) + Q54 +
    /// Q57.
    ///
    /// <list type="bullet">
    ///   <item><b>Tuần</b>: <see cref="WeekTrendWindow"/> tuần KẾT THÚC ở tuần đang xem, CẮT ở
    ///   đầu năm đang lọc — xem tuần 3 thì trả tuần 1..3 (3 phần tử), không bao giờ lấn sang năm
    ///   trước.</item>
    ///   <item><b>Tháng/Năm</b>: năm đang lọc là năm hiện tại thì từ tháng 1 tới THÁNG HIỆN TẠI
    ///   (không trả kỳ tương lai); năm đã qua thì trọn 12 tháng.</item>
    /// </list>
    /// </summary>
    public static IReadOnlyList<PeriodRange> TrendWindow(DashboardMode mode, PeriodRange period, DateOnly today)
    {
        if (mode == DashboardMode.Week)
        {
            var week = ISOWeek.GetWeekOfYear(period.Start.ToDateTime(TimeOnly.MinValue));
            var first = Math.Max(1, week - WeekTrendWindow + 1);

            var weeks = new List<PeriodRange>(week - first + 1);
            for (var i = first; i <= week; i++)
                weeks.Add(PeriodRange.IsoWeek(period.Year, i));

            return weeks;
        }

        var lastMonth = period.Year == today.Year ? today.Month : 12;
        var months = new List<PeriodRange>(lastMonth);
        for (var month = 1; month <= lastMonth; month++)
            months.Add(PeriodRange.CalendarMonth(period.Year, month));

        return months;
    }

    /// <summary>
    /// Kỳ LIỀN TRƯỚC <b>CÓ DỮ LIỆU</b> — không phải kỳ liền kề theo lịch (§1.3). Lùi từng kỳ một
    /// cho tới khi gặp kỳ có ít nhất một bản ghi, hoặc chạm <paramref name="notEarlierThan"/>
    /// (biên dữ liệu đã nạp). Không có thì trả <c>null</c> và cả <c>delta</c> lẫn
    /// <c>previousPeriodLabel</c> vắng mặt — FE hiện dấu gạch, KHÔNG hiện 0.
    /// </summary>
    public static PeriodRange? PreviousWithData(
        DashboardMode mode,
        PeriodRange period,
        IReadOnlyCollection<AssessmentFact> facts,
        DateOnly notEarlierThan)
    {
        var cursor = period;

        // Trần vòng lặp = số kỳ tối đa có thể nằm trong cửa sổ dữ liệu đã nạp (2 năm), cộng dư.
        // Có trần vì "lùi tới khi gặp dữ liệu" trên một DB rỗng là vòng lặp không điểm dừng.
        var maxSteps = mode switch
        {
            DashboardMode.Week => 120,
            DashboardMode.Month => 30,
            _ => 5,
        };

        for (var step = 0; step < maxSteps; step++)
        {
            cursor = Step(mode, cursor);

            if (cursor.End < notEarlierThan)
                return null;

            foreach (var fact in facts)
            {
                if (cursor.Contains(fact.AssessmentDate))
                    return cursor;
            }
        }

        return null;
    }

    private static PeriodRange Step(DashboardMode mode, PeriodRange period) => mode switch
    {
        DashboardMode.Week => PeriodRange.IsoWeekOf(period.Start.AddDays(-7)),
        DashboardMode.Month => PeriodRange.CalendarMonth(
            period.Start.AddMonths(-1).Year, period.Start.AddMonths(-1).Month),
        _ => PeriodRange.WholeYear(period.Year - 1),
    };
}

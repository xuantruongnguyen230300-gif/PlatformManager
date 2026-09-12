using System.Globalization;

namespace PlatformManager.Business.Application.Common;

/// <summary>
/// Dựng NHÃN kỳ — spec/dashboard-dti/business-rules.md §6 là **file chủ của định dạng**, mục này
/// chỉ hiện thực đúng các khuôn ở §6.2.
///
/// <para><b>Vì sao BE dựng chuỗi thay vì trả mã cho FE tự ghép</b> (§6.3): ghép được thì FE phải
/// quy <c>"2026-W33"</c> về khoảng ngày, tức phải có LỊCH ISO của riêng nó — đúng phép tính mà
/// §5.1 cấm tự làm. Dựng ở BE giữ lịch ISO ở một chỗ.</para>
///
/// <para><b>Dấu gạch là EN DASH có khoảng trắng hai bên</b> (<c>" – "</c>, U+2013 — chốt T5).
/// KHÔNG viết dính <c>10/08–16/08</c>. Hằng số <see cref="RangeSeparator"/> tồn tại để luật đó
/// có đúng một chỗ khai; đừng gõ lại dấu gạch ở nơi gọi.</para>
/// </summary>
public static class PeriodLabels
{
    /// <summary>En dash kèm khoảng trắng hai bên — T5, §6.1 mục 2.</summary>
    public const string RangeSeparator = " – ";

    /// <summary>
    /// Nhãn kỳ ĐANG XEM, khuôn đầy đủ (§6.2): <c>Tuần 33/2026 (10/08 – 16/08/2026)</c> ·
    /// <c>Tháng 8/2026 (01/08 – 31/08/2026)</c> · <c>Năm 2026</c>.
    /// Năm viết MỘT lần, ở mốc cuối (§6.1 mục 3).
    /// </summary>
    public static string Full(PeriodRange range) => range.Kind switch
    {
        PeriodKind.Week => $"Tuần {WeekNumberOf(range)}/{range.Year} ({DayMonth(range.Start)}{RangeSeparator}{DayMonthYear(range.End)})",
        PeriodKind.Month => $"Tháng {range.Start.Month}/{range.Year} ({DayMonth(range.Start)}{RangeSeparator}{DayMonthYear(range.End)})",
        _ => $"Năm {range.Year}",
    };

    /// <summary>
    /// Nhãn TRỤC X của biểu đồ xu hướng (§1.5 + Q43): chế độ tuần là khoảng ngày
    /// <c>06/07 – 12/07</c>; chế độ tháng/năm là <c>Th.1 … Th.12</c> (T7 — 12 nhãn khoảng-ngày
    /// không đủ chỗ trên trục).
    /// </summary>
    public static string TrendAxis(PeriodRange range) => range.Kind switch
    {
        PeriodKind.Week => $"{DayMonth(range.Start)}{RangeSeparator}{DayMonth(range.End)}",
        PeriodKind.Month => $"Th.{range.Start.Month}",
        _ => $"Năm {range.Year}",
    };

    private static int WeekNumberOf(PeriodRange range) =>
        ISOWeek.GetWeekOfYear(range.Start.ToDateTime(TimeOnly.MinValue));

    private static string DayMonth(DateOnly date) =>
        date.ToString("dd/MM", CultureInfo.InvariantCulture);

    private static string DayMonthYear(DateOnly date) =>
        date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
}

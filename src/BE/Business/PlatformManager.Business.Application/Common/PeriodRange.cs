using System.Globalization;

namespace PlatformManager.Business.Application.Common;

/// <summary>Đơn vị của một kỳ — spec/danh-muc-dti/business-rules.md §5.1.</summary>
public enum PeriodKind
{
    /// <summary><c>"all"</c> — cả năm <c>Year</c>, 01/01 → 31/12.</summary>
    All,

    /// <summary><c>"YYYY-Www"</c> — tuần ISO-8601, thứ Hai → Chủ nhật.</summary>
    Week,

    /// <summary><c>"YYYY-MM"</c> — tháng dương lịch, ngày 1 → ngày cuối tháng.</summary>
    Month,
}

/// <summary>
/// Một kỳ đã quy về KHOẢNG NGÀY — kết quả của phép quy đổi mà
/// spec/dashboard-dti/business-rules.md §5 xếp vào ranh giới nghiệp vụ.
///
/// <para><b>Ranh giới Core ↔ Business ở đây, đọc trước khi chuyển chỗ:</b> <i>"tuần ISO thứ N
/// của năm Y từ ngày nào tới ngày nào"</i> là logic THỜI GIAN THUẦN — và nó đã có sẵn ở
/// <see cref="ISOWeek"/> của BCL, tức "Core" theo nghĩa rộng nhất; không dựng lại, không tính
/// bằng <c>ngày / 7</c>. Còn <i>"kỳ nào thì lọc bản ghi đánh giá theo <c>AssessmentDate</c> ra
/// sao"</i> là NGHIỆP VỤ, và đó chính là kiểu này — nên nó ở <c>Business.Application</c>. Phép
/// thử của §5: <i>"sản phẩm thứ hai có dùng câu này không?"</i> — <see cref="ISOWeek"/>: có;
/// <see cref="PeriodRange"/>: không.</para>
/// </summary>
/// <param name="Kind">Đơn vị kỳ.</param>
/// <param name="Value">Chuỗi định danh trên dây: <c>"all"</c> · <c>"2026-W33"</c> · <c>"2026-08"</c>.</param>
/// <param name="Year">
/// Năm của kỳ. Với <see cref="PeriodKind.Week"/> đây là NĂM ISO của tuần, có thể khác năm dương
/// lịch của ngày đầu tuần — tuần 1 của một năm có thể bắt đầu từ tháng 12 năm trước (§5.1).
/// </param>
/// <param name="Start">Ngày đầu kỳ (bao gồm).</param>
/// <param name="End">Ngày cuối kỳ (bao gồm).</param>
public sealed record PeriodRange(PeriodKind Kind, string Value, int Year, DateOnly Start, DateOnly End)
{
    public bool Contains(DateOnly date) => date >= Start && date <= End;

    /// <summary>Cả năm <paramref name="year"/> — ngữ nghĩa của <c>period = "all"</c> và của <c>mode=year</c>.</summary>
    public static PeriodRange WholeYear(int year) => new(
        PeriodKind.All,
        PeriodValues.All,
        year,
        new DateOnly(year, 1, 1),
        new DateOnly(year, 12, 31));

    /// <summary>
    /// Tuần ISO thứ <paramref name="week"/> của năm ISO <paramref name="isoYear"/>. Quy đổi qua
    /// <see cref="ISOWeek"/> — KHÔNG tự tính, vì tuần ISO không nằm gọn trong một tháng và cũng
    /// không nằm gọn trong một năm (§5.1).
    /// </summary>
    public static PeriodRange IsoWeek(int isoYear, int week)
    {
        var monday = DateOnly.FromDateTime(ISOWeek.ToDateTime(isoYear, week, DayOfWeek.Monday));
        return new PeriodRange(
            PeriodKind.Week,
            PeriodValues.Week(isoYear, week),
            isoYear,
            monday,
            monday.AddDays(6));
    }

    /// <summary>Tuần ISO CHỨA <paramref name="date"/>.</summary>
    public static PeriodRange IsoWeekOf(DateOnly date)
    {
        var asDateTime = date.ToDateTime(TimeOnly.MinValue);
        return IsoWeek(ISOWeek.GetYear(asDateTime), ISOWeek.GetWeekOfYear(asDateTime));
    }

    /// <summary>Tháng dương lịch.</summary>
    public static PeriodRange CalendarMonth(int year, int month) => new(
        PeriodKind.Month,
        PeriodValues.Month(year, month),
        year,
        new DateOnly(year, month, 1),
        new DateOnly(year, month, DateTime.DaysInMonth(year, month)));
}

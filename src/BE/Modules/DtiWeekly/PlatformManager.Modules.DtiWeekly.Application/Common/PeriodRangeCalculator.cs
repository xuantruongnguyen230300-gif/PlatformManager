using System.Globalization;

namespace PlatformManager.Modules.DtiWeekly.Application.Common;

/// <summary>
/// Hàm thuần suy ra khoảng ngày [Start, EndExclusive) từ (Year, Period, PeriodValue) —
/// dùng chung cho grid "Danh mục DTI" (GetCriteriaListQuery) và Dashboard
/// (AggregationService). "Tuần" = tuần ISO (thứ Hai–Chủ Nhật, đã chốt), xem
/// doc/ERD/ERD.md mục "Kỳ (tuần/tháng/năm)".
/// </summary>
public static class PeriodRangeCalculator
{
    public const string PeriodAll = "all";
    public const string PeriodWeek = "week";
    public const string PeriodMonth = "month";

    /// <summary>PeriodValue cho "week": "yyyy-Www" (vd "2026-W03"). Cho "month": "yyyy-MM".
    /// "all" không cần PeriodValue.</summary>
    public static (DateOnly Start, DateOnly EndExclusive) Resolve(int year, string period, string? periodValue)
        => period switch
        {
            PeriodWeek => ResolveWeek(periodValue),
            PeriodMonth => ResolveMonth(year, periodValue),
            PeriodAll => (new DateOnly(year, 1, 1), new DateOnly(year + 1, 1, 1)),
            _ => throw new ArgumentException($"Period '{period}' không hợp lệ — chỉ nhận 'all'|'week'|'month'.", nameof(period)),
        };

    /// <summary>"Live" = đang xem đúng "Tất cả" của NĂM HIỆN TẠI, chưa thu hẹp tuần/tháng
    /// nào (xem spec/danh-muc-dti/business-rules.md mục 2.4).</summary>
    public static bool IsLive(int year, string period, DateOnly today)
        => year == today.Year && period == PeriodAll;

    public static DateOnly IsoWeekStart(DateOnly date)
    {
        var dt = date.ToDateTime(TimeOnly.MinValue);
        var isoWeek = ISOWeek.GetWeekOfYear(dt);
        var isoYear = ISOWeek.GetYear(dt);
        return DateOnly.FromDateTime(ISOWeek.ToDateTime(isoYear, isoWeek, DayOfWeek.Monday));
    }

    /// <summary>"yyyy-Www" (vd "2026-W03") cho đúng 1 điểm bắt đầu tuần ISO — dùng để sinh
    /// PeriodOptionDto.Value ở GetPeriodsAsync, đối xứng với ResolveWeek() ở trên (parse ngược
    /// lại đúng định dạng này).</summary>
    public static string IsoWeekValue(DateOnly weekStart)
    {
        var dt = weekStart.ToDateTime(TimeOnly.MinValue);
        return $"{ISOWeek.GetYear(dt)}-W{ISOWeek.GetWeekOfYear(dt):D2}";
    }

    /// <summary>"yyyy-MM" cho 1 tháng — đối xứng với ResolveMonth().</summary>
    public static string MonthValue(DateOnly monthStart) => $"{monthStart.Year:D4}-{monthStart.Month:D2}";

    /// <summary>Liệt kê điểm bắt đầu của mọi tuần ISO giao với [start, endExclusive).</summary>
    public static List<DateOnly> EnumerateIsoWeekStarts(DateOnly start, DateOnly endExclusive)
    {
        var result = new List<DateOnly>();
        var cursor = IsoWeekStart(start);
        while (cursor < endExclusive)
        {
            result.Add(cursor);
            cursor = cursor.AddDays(7);
        }

        return result;
    }

    private static (DateOnly, DateOnly) ResolveWeek(string? periodValue)
    {
        if (string.IsNullOrWhiteSpace(periodValue))
            throw new ArgumentException("Thiếu periodValue cho period='week' (định dạng 'yyyy-Www', vd '2026-W03').");

        var parts = periodValue.Split('-', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2 || !parts[1].StartsWith('W')
            || !int.TryParse(parts[0], out var isoYear)
            || !int.TryParse(parts[1].AsSpan(1), out var isoWeek))
        {
            throw new ArgumentException($"periodValue '{periodValue}' không đúng định dạng 'yyyy-Www'.");
        }

        var start = DateOnly.FromDateTime(ISOWeek.ToDateTime(isoYear, isoWeek, DayOfWeek.Monday));
        return (start, start.AddDays(7));
    }

    private static (DateOnly, DateOnly) ResolveMonth(int year, string? periodValue)
    {
        int y = year, m;
        if (!string.IsNullOrWhiteSpace(periodValue))
        {
            var parts = periodValue.Split('-', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2 || !int.TryParse(parts[0], out y) || !int.TryParse(parts[1], out m))
                throw new ArgumentException($"periodValue '{periodValue}' không đúng định dạng 'yyyy-MM'.");
        }
        else
        {
            throw new ArgumentException("Thiếu periodValue cho period='month' (định dạng 'yyyy-MM').");
        }

        var start = new DateOnly(y, m, 1);
        return (start, start.AddMonths(1));
    }
}

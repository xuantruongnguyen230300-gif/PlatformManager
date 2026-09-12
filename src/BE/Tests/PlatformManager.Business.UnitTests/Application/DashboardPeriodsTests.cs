using PlatformManager.Business.Application.Common;
using PlatformManager.Business.Application.Dashboard;
using Xunit;

namespace PlatformManager.Business.UnitTests.Application;

/// <summary>
/// Quy tham số của DB-1 về một kỳ, và cửa sổ <c>trend</c> — Q54 · Q57 · Q61 · Q63,
/// doc/contracts/dashboard.md DB-1.
/// </summary>
public class DashboardPeriodsTests
{
    private static readonly DateOnly Today = new(2026, 8, 12);   // thứ Tư, tuần ISO 33

    [Theory]
    [InlineData("week", DashboardMode.Week)]
    [InlineData("month", DashboardMode.Month)]
    [InlineData("year", DashboardMode.Year)]
    public void TryParseMode_Accepts_TheThreeLowercaseValues(string input, DashboardMode expected)
    {
        Assert.True(DashboardPeriods.TryParseMode(input, out var mode));
        Assert.Equal(expected, mode);
    }

    /// <summary>
    /// Sai hoa/thường cũng là <c>MODE_INVALID</c> — hợp đồng khai rõ "chữ thường". Một bộ đọc
    /// rộng rãi ở đây sẽ dạy client gửi bất cứ dạng nào rồi khoá luôn khả năng siết lại về sau.
    /// <c>mode</c> vắng mặt cũng ra cùng mã: cả hai ca đều kết thúc bằng "gửi lại một mode hợp lệ".
    /// </summary>
    [Theory]
    [InlineData("Week")]
    [InlineData("WEEK")]
    [InlineData("tuan")]
    [InlineData("")]
    [InlineData(null)]
    public void TryParseMode_Rejects_EverythingElse(string? input)
    {
        Assert.False(DashboardPeriods.TryParseMode(input, out _));
    }

    /// <summary>Q63 — bỏ trống <c>year</c> thì lấy năm ISO của tuần chứa <c>date</c>, không phải 400.</summary>
    [Fact]
    public void ResolveWeek_WithoutYear_Takes_TheIsoYearOfTheDate()
    {
        // 29/12/2025 thuộc tuần 1 của 2026.
        Assert.True(DashboardPeriods.TryResolveWeek(new DateOnly(2025, 12, 29), null, Today, out var period, out var isoYear));

        Assert.Equal(2026, isoYear);
        Assert.Equal("2026-W01", period.Value);
    }

    /// <summary>
    /// Hàng 3 — <b>ca âm duy nhất còn sinh 400</b>. Bắt buộc phải có: thiếu nó thì một bản cài
    /// BỎ HẲN phép kiểm Q61 cũng qua được mọi ca còn lại của file này.
    /// </summary>
    [Fact]
    public void ResolveWeek_WithBothSentAndMismatched_IsRejected()
    {
        Assert.False(DashboardPeriods.TryResolveWeek(new DateOnly(2025, 12, 29), 2025, Today, out _, out var isoYear));
        Assert.Equal(2026, isoYear);

        // Cùng ngày với year đúng ⇒ chấp nhận.
        Assert.True(DashboardPeriods.TryResolveWeek(new DateOnly(2025, 12, 29), 2026, Today, out _, out _));
    }

    /// <summary>Hàng 6 — không gửi gì cả: tuần hiện tại của năm hiện tại.</summary>
    [Fact]
    public void ResolveWeek_WithoutDate_Uses_TheCurrentWeek()
    {
        Assert.True(DashboardPeriods.TryResolveWeek(null, null, Today, out var period, out _));

        Assert.Equal("2026-W33", period.Value);
        Assert.Equal(new DateOnly(2026, 8, 10), period.Start);
    }

    /// <summary>Hàng 4 — có <c>year</c> trùng năm ISO hiện tại, không <c>date</c>: tuần hiện tại.</summary>
    [Fact]
    public void ResolveWeek_WithCurrentYearAndNoDate_Uses_TheCurrentWeek()
    {
        Assert.True(DashboardPeriods.TryResolveWeek(null, 2026, Today, out var period, out _));

        Assert.Equal("2026-W33", period.Value);
    }

    /// <summary>
    /// <b>Hàng 5 — Q71.</b> Có <c>year</c> khác năm hiện tại, KHÔNG có <c>date</c> ⇒ tuần ISO
    /// CUỐI của năm đó, và <b>KHÔNG</b> phải 400.
    ///
    /// <para>Đây là lỗi tích hợp đã xảy ra thật: bản cài trước neo <c>date</c> vào hôm nay rồi áp
    /// Q61, nên nó bắn <c>400 PERIOD_YEAR_MISMATCH</c> cho một đường đi hoàn toàn hợp lệ — DB-3
    /// khai "năm không có dữ liệu ⇒ <c>weeksInYear</c> RỖNG, 200", nên FE không có <c>date</c>
    /// nào để gửi.</para>
    ///
    /// <para>2025 có 52 tuần ISO, 2026 có 53 — hai năm khác nhau để ca này không thể xanh nhờ một
    /// hằng số chép cứng.</para>
    /// </summary>
    [Theory]
    [InlineData(2025, "2025-W52")]
    [InlineData(2020, "2020-W53")]
    public void ResolveWeek_WithOtherYearAndNoDate_Uses_TheLastIsoWeekOfThatYear(int year, string expected)
    {
        Assert.True(DashboardPeriods.TryResolveWeek(null, year, Today, out var period, out var isoYear));

        Assert.Equal(expected, period.Value);
        Assert.Equal(year, isoYear);
    }

    /// <summary>
    /// Hàng 5, tiếp — kỳ trả về phải cho <c>trend</c> một cửa sổ 12 tuần ĐẦY ĐỦ. Đó chính là lý
    /// do chọn tuần CUỐI thay vì tuần 1: Q57 cắt cửa sổ ở ĐẦU năm, nên neo vào tuần 1 sẽ trả về
    /// một trục chỉ có một điểm.
    /// </summary>
    [Fact]
    public void ResolveWeek_LastWeekOfYear_Gives_AFullTrendWindow()
    {
        Assert.True(DashboardPeriods.TryResolveWeek(null, 2025, Today, out var period, out _));

        var window = DashboardPeriods.TrendWindow(DashboardMode.Week, period, Today);

        Assert.Equal(DashboardPeriods.WeekTrendWindow, window.Count);
        Assert.Equal("2025-W52", window[^1].Value);
    }

    /// <summary>
    /// Bất biến bao trùm của Q71: <b>không có <c>date</c> thì KHÔNG BAO GIỜ 400</b> — không có
    /// hai giá trị nào để mà lệch nhau. Quét mọi năm quanh mốc, kể cả năm tương lai.
    /// </summary>
    [Theory]
    [InlineData(2020)]
    [InlineData(2025)]
    [InlineData(2026)]
    [InlineData(2027)]
    [InlineData(2030)]
    public void ResolveWeek_WithoutDate_NeverRejects(int year)
    {
        Assert.True(DashboardPeriods.TryResolveWeek(null, year, Today, out _, out _));
    }

    /// <summary>
    /// Q54 — cửa sổ 12 tuần KẾT THÚC ở tuần ĐANG XEM, không phải tuần hiện tại của lịch. Xem tuần
    /// 20 thì trục chạy 9..20, kể cả khi hôm nay là tuần 33.
    /// </summary>
    [Fact]
    public void TrendWindow_ForWeekMode_Ends_AtTheViewedWeek()
    {
        var window = DashboardPeriods.TrendWindow(DashboardMode.Week, PeriodRange.IsoWeek(2026, 20), Today);

        Assert.Equal(DashboardPeriods.WeekTrendWindow, window.Count);
        Assert.Equal("2026-W09", window[0].Value);
        Assert.Equal("2026-W20", window[^1].Value);
    }

    /// <summary>
    /// Q57 — cửa sổ CẮT ở đầu năm đang lọc, không bao giờ lấn sang năm trước. Xem tuần 3 ⇒ trả
    /// đúng tuần 1..3, ba phần tử.
    /// </summary>
    [Fact]
    public void TrendWindow_ForWeekMode_IsClipped_AtTheStartOfYear()
    {
        var window = DashboardPeriods.TrendWindow(DashboardMode.Week, PeriodRange.IsoWeek(2026, 3), Today);

        Assert.Equal(3, window.Count);
        Assert.Equal(["2026-W01", "2026-W02", "2026-W03"], window.Select(period => period.Value));
    }

    /// <summary>Năm HIỆN TẠI ⇒ từ tháng 1 tới tháng hiện tại; không trả kỳ tương lai.</summary>
    [Fact]
    public void TrendWindow_ForMonthMode_CurrentYear_StopsAtCurrentMonth()
    {
        var window = DashboardPeriods.TrendWindow(DashboardMode.Month, PeriodRange.CalendarMonth(2026, 8), Today);

        Assert.Equal(8, window.Count);
        Assert.Equal("2026-01", window[0].Value);
        Assert.Equal("2026-08", window[^1].Value);
    }

    /// <summary>Năm ĐÃ QUA ⇒ trọn 12 tháng.</summary>
    [Fact]
    public void TrendWindow_ForYearMode_PastYear_CoversAllTwelveMonths()
    {
        var window = DashboardPeriods.TrendWindow(DashboardMode.Year, PeriodRange.WholeYear(2025), Today);

        Assert.Equal(12, window.Count);
        Assert.Equal("2025-12", window[^1].Value);
    }

    /// <summary>
    /// §1.3 — "kỳ liền trước" là kỳ liền kề CÓ DỮ LIỆU, không phải kỳ liền kề theo lịch. Ca này
    /// bỏ trống hai tuần ở giữa để phân biệt hai cách hiểu.
    /// </summary>
    [Fact]
    public void PreviousWithData_Skips_EmptyPeriods()
    {
        var criteriaId = Guid.CreateVersion7();
        AssessmentFact[] facts =
        [
            new(criteriaId, new DateOnly(2026, 7, 22), 40, null),   // tuần 30
            new(criteriaId, new DateOnly(2026, 8, 12), 70, null),   // tuần 33
        ];

        var previous = DashboardPeriods.PreviousWithData(
            DashboardMode.Week, PeriodRange.IsoWeek(2026, 33), facts, new DateOnly(2025, 1, 1));

        Assert.NotNull(previous);
        Assert.Equal("2026-W30", previous.Value);
    }

    /// <summary>
    /// Không có kỳ nào trước ⇒ <c>null</c> ⇒ <c>delta</c> và <c>previousPeriodLabel</c> vắng mặt,
    /// FE hiện dấu gạch chứ không hiện 0. Ca này cũng canh việc vòng lặp lùi kỳ có ĐIỂM DỪNG trên
    /// một tập dữ liệu rỗng.
    /// </summary>
    [Fact]
    public void PreviousWithData_IsNull_WhenNothingCameBefore()
    {
        var previous = DashboardPeriods.PreviousWithData(
            DashboardMode.Week, PeriodRange.IsoWeek(2026, 33), [], new DateOnly(2025, 1, 1));

        Assert.Null(previous);
    }
}

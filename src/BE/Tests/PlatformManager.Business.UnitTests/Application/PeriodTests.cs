using PlatformManager.Business.Application.Common;
using Xunit;

namespace PlatformManager.Business.UnitTests.Application;

/// <summary>
/// Quy đổi kỳ và nhãn kỳ — spec/danh-muc-dti/business-rules.md §5.1 và
/// spec/dashboard-dti/business-rules.md §6.
/// </summary>
public class PeriodTests
{
    /// <summary>
    /// Tám tuần ISO 2026 mà file luật §5.1 liệt kê sẵn. Chúng là mốc đối chiếu ĐỘC LẬP với code:
    /// nếu bộ quy đổi lệch một ngày (lỗi kinh điển khi ai đó tự tính bằng "ngày / 7"), ca này đỏ.
    /// </summary>
    [Theory]
    [InlineData(28, "2026-07-06", "2026-07-12")]
    [InlineData(29, "2026-07-13", "2026-07-19")]
    [InlineData(30, "2026-07-20", "2026-07-26")]
    [InlineData(31, "2026-07-27", "2026-08-02")]
    [InlineData(32, "2026-08-03", "2026-08-09")]
    [InlineData(33, "2026-08-10", "2026-08-16")]
    [InlineData(34, "2026-08-17", "2026-08-23")]
    [InlineData(35, "2026-08-24", "2026-08-30")]
    public void IsoWeek_MapsTo_TheDateRange_InTheRuleFile(int week, string start, string end)
    {
        var range = PeriodRange.IsoWeek(2026, week);

        Assert.Equal(DateOnly.Parse(start), range.Start);
        Assert.Equal(DateOnly.Parse(end), range.End);
        Assert.Equal(DayOfWeek.Monday, range.Start.DayOfWeek);
        Assert.Equal(DayOfWeek.Sunday, range.End.DayOfWeek);
    }

    /// <summary>
    /// Tuần ISO KHÔNG nằm gọn trong một năm: 29/12/2025 thuộc tuần 1 của <b>2026</b>. Đây chính
    /// là ca mà Q61 dùng làm ví dụ, và là lý do <see cref="PeriodRange.Year"/> mang NĂM ISO chứ
    /// không phải năm dương lịch của ngày đầu tuần.
    /// </summary>
    [Fact]
    public void IsoWeek_CanStart_InThePreviousCalendarYear()
    {
        var range = PeriodRange.IsoWeekOf(new DateOnly(2025, 12, 29));

        Assert.Equal(2026, range.Year);
        Assert.Equal("2026-W01", range.Value);
        Assert.Equal(new DateOnly(2025, 12, 29), range.Start);
    }

    [Theory]
    [InlineData(null, PeriodKind.All, "all")]
    [InlineData("", PeriodKind.All, "all")]
    [InlineData("all", PeriodKind.All, "all")]
    [InlineData("ALL", PeriodKind.All, "all")]
    [InlineData("2026-W33", PeriodKind.Week, "2026-W33")]
    [InlineData("2026-08", PeriodKind.Month, "2026-08")]
    public void TryParse_Accepts_TheThreeShapes(string? input, PeriodKind kind, string value)
    {
        Assert.True(PeriodParser.TryParse(input, 2026, out var range));
        Assert.Equal(kind, range.Kind);
        Assert.Equal(value, range.Value);
    }

    /// <summary>
    /// Chuỗi sai khuôn KHÔNG được âm thầm rơi về <c>"all"</c>: rơi về mặc định nghĩa là một client
    /// gửi sai vẫn nhận 200 với dữ liệu của một kỳ khác — ca hỏng người dùng không phát hiện được.
    /// </summary>
    [Theory]
    [InlineData("2026-W99")]  // vượt số tuần thật của năm
    [InlineData("2026-W00")]
    [InlineData("2026-13")]   // tháng 13
    [InlineData("2026-00")]
    [InlineData("tuần 33")]
    [InlineData("2026-w33")]  // chữ w thường — khoá này do FE lấy nguyên văn từ DB-3, không phải người gõ
    [InlineData("2026")]
    public void TryParse_Rejects_MalformedPeriod(string input)
    {
        Assert.False(PeriodParser.TryParse(input, 2026, out _));
    }

    /// <summary>
    /// Số tuần hợp lệ phụ thuộc NĂM (52 hoặc 53 tuần ISO), nên bộ đọc phải hỏi lịch chứ không
    /// chặn cứng ở 53. 2026 có 53 tuần; 2025 chỉ có 52.
    /// </summary>
    [Fact]
    public void TryParse_Uses_TheRealNumberOfWeeksInYear()
    {
        Assert.True(PeriodParser.TryParse("2026-W53", 2026, out _));
        Assert.False(PeriodParser.TryParse("2025-W53", 2025, out _));
    }

    /// <summary>Khuôn nhãn kỳ đầy đủ — §6.2. Năm viết MỘT lần, ở mốc cuối.</summary>
    [Fact]
    public void FullLabel_Follows_TheMasterFormat()
    {
        Assert.Equal(
            "Tuần 33/2026 (10/08 – 16/08/2026)",
            PeriodLabels.Full(PeriodRange.IsoWeek(2026, 33)));

        Assert.Equal(
            "Tháng 8/2026 (01/08 – 31/08/2026)",
            PeriodLabels.Full(PeriodRange.CalendarMonth(2026, 8)));

        Assert.Equal("Năm 2026", PeriodLabels.Full(PeriodRange.WholeYear(2026)));
    }

    /// <summary>
    /// Dấu gạch là EN DASH (U+2013) CÓ khoảng trắng hai bên — chốt T5. Viết dính là sai, và đó là
    /// lỗi mà prototype cũ mắc ở một nửa số chỗ. Ca này so bằng chính mã ký tự để không ai "sửa"
    /// nó thành gạch nối ASCII rồi vẫn thấy test xanh.
    /// </summary>
    [Fact]
    public void RangeSeparator_IsEnDash_WithSpaces()
    {
        Assert.Equal(" – ", PeriodLabels.RangeSeparator);
        Assert.Contains(" – ", PeriodLabels.Full(PeriodRange.IsoWeek(2026, 33)), StringComparison.Ordinal);
    }

    /// <summary>Nhãn trục X — Q43: tuần là khoảng ngày, tháng/năm là "Th.{M}" (T7).</summary>
    [Fact]
    public void TrendAxisLabel_DiffersByMode()
    {
        Assert.Equal("06/07 – 12/07", PeriodLabels.TrendAxis(PeriodRange.IsoWeek(2026, 28)));
        Assert.Equal("Th.1", PeriodLabels.TrendAxis(PeriodRange.CalendarMonth(2026, 1)));
        Assert.Equal("Th.12", PeriodLabels.TrendAxis(PeriodRange.CalendarMonth(2026, 12)));
    }
}

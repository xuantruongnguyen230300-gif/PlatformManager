using PlatformManager.Business.Application.Common;
using Xunit;

namespace PlatformManager.Business.UnitTests.Application;

/// <summary>
/// <b>Bước 1 của luật ghi</b> — spec/danh-muc-dti/business-rules.md §5.3, dùng CHUNG cho DM-4,
/// DM-6 và DM-7.
///
/// <para>Đây là chỗ đắt nhất nếu sai, vì nó quyết định lời ghi RƠI VÀO KỲ NÀO. Một bản cài "gần
/// đúng" (vd suy <c>PERIOD_OUT_OF_YEAR</c> từ <i>"năm cũ"</i> thay vì từ <i>"all + năm cũ"</i>)
/// vẫn qua được mọi ca khác và chỉ hai ca dưới đây bắt được.</para>
/// </summary>
public class WritePeriodResolverTests
{
    /// <summary>Thứ Tư 12/08/2026 — nằm trong tuần ISO 33 (10/08 – 16/08).</summary>
    private static readonly DateOnly Today = new(2026, 8, 12);

    [Fact(DisplayName = "period vắng mặt ⇒ Required — server KHÔNG chọn hộ, KHÔNG rơi về \"all\"")]
    public void MissingPeriod_IsRejected_NotDefaultedToAll()
    {
        Assert.Equal(WritePeriodRejection.Required, WritePeriodResolver.Resolve(null, 2026, Today).Rejection);
        Assert.Equal(WritePeriodRejection.Required, WritePeriodResolver.Resolve("   ", 2026, Today).Rejection);
    }

    [Fact(DisplayName = "Tuần cụ thể ⇒ nhận, kể cả tuần ĐÃ QUA và năm TRƯỚC (Q20 không bị lật)")]
    public void SpecificWeek_IsAccepted_EvenInPastYear()
    {
        var resolution = WritePeriodResolver.Resolve("2025-W33", year: 2025, Today);

        Assert.True(resolution.IsAccepted);
        Assert.Equal("2025-W33", resolution.TargetWeek!.Value);
        Assert.Equal(2025, resolution.TargetWeek.Year);
    }

    /// <summary>
    /// Ca ÂM BẮT BUỘC của T15 (nghiệm thu mục 14). Thiếu ca này thì một bản cài "cấm ghi vào năm
    /// cũ" vẫn qua được nghiệm thu trong khi nó đã lật mất Q20.
    /// </summary>
    [Fact(DisplayName = "year cũ + tuần cụ thể ⇒ VẪN ghi được — T15 chỉ chặn lối tắt \"all\"")]
    public void PastYear_WithSpecificWeek_IsStillWritable()
    {
        Assert.True(WritePeriodResolver.Resolve("2025-W02", year: 2025, Today).IsAccepted);
    }

    [Fact(DisplayName = "\"all\" + năm hiện tại ⇒ tuần ISO chứa hôm nay (Q26)")]
    public void All_InCurrentYear_ResolvesToCurrentWeek()
    {
        var resolution = WritePeriodResolver.Resolve("all", year: 2026, Today);

        Assert.True(resolution.IsAccepted);
        Assert.Equal("2026-W33", resolution.TargetWeek!.Value);
        Assert.Equal(new DateOnly(2026, 8, 10), resolution.TargetWeek.Start);
        Assert.Equal(new DateOnly(2026, 8, 16), resolution.TargetWeek.End);
    }

    [Fact(DisplayName = "\"all\" + năm khác ⇒ OutOfYear (T15)")]
    public void All_InAnotherYear_IsRejected()
    {
        Assert.Equal(
            WritePeriodRejection.OutOfYear,
            WritePeriodResolver.Resolve("all", year: 2025, Today).Rejection);
    }

    /// <summary>
    /// Lưới an toàn: ca này đã bị validator chặn thành <c>400 ValidationError</c> + <c>fields</c>
    /// trước khi tới handler, nhưng bộ giải không được phép rơi vào nhánh "nhận".
    /// </summary>
    [Fact(DisplayName = "\"all\" mà thiếu year ⇒ KHÔNG nhận")]
    public void All_WithoutYear_IsRejected()
    {
        Assert.False(WritePeriodResolver.Resolve("all", year: null, Today).IsAccepted);
    }

    [Fact(DisplayName = "Kỳ THÁNG ⇒ NotWeekly, KHÔNG phải Invalid (Q37 — ba mã rời nhau, cố ý)")]
    public void MonthPeriod_IsNotWeekly_NotInvalid()
    {
        Assert.Equal(
            WritePeriodRejection.NotWeekly,
            WritePeriodResolver.Resolve("2026-08", year: 2026, Today).Rejection);
    }

    [Theory(DisplayName = "Chuỗi sai khuôn ⇒ Invalid")]
    [InlineData("2026-W99")]
    [InlineData("tuần 33")]
    [InlineData("2026-13")]
    [InlineData("2026/W33")]
    public void MalformedPeriod_IsInvalid(string period)
    {
        Assert.Equal(
            WritePeriodRejection.Invalid,
            WritePeriodResolver.Resolve(period, year: 2026, Today).Rejection);
    }

    [Fact(DisplayName = "Neo ngày: hôm nay NẰM TRONG tuần đích ⇒ neo vào hôm nay")]
    public void AnchorDate_InsideTargetWeek_IsToday()
    {
        var week = PeriodRange.IsoWeek(2026, 33);

        Assert.Equal(Today, WritePeriodResolver.AnchorDate(week, Today));
    }

    /// <summary>
    /// Nhập bù cho một tuần đã qua: KHÔNG bao giờ sinh ra một ngày nằm NGOÀI tuần đích — nếu
    /// không, cột <c>Kỳ của số liệu</c> sẽ báo một tuần khác tuần người dùng vừa chọn.
    /// </summary>
    [Fact(DisplayName = "Neo ngày: tuần đích ĐÃ QUA ⇒ neo vào CHỦ NHẬT của tuần đó")]
    public void AnchorDate_OutsideTargetWeek_IsSunday()
    {
        var week = PeriodRange.IsoWeek(2026, 30);   // 20/07 – 26/07

        var anchor = WritePeriodResolver.AnchorDate(week, Today);

        Assert.Equal(new DateOnly(2026, 7, 26), anchor);
        Assert.Equal(DayOfWeek.Sunday, anchor.DayOfWeek);
        Assert.True(week.Contains(anchor));
    }
}

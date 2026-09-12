using PlatformManager.Business.Application.Common;
using PlatformManager.Business.Application.Criteria;
using Xunit;

namespace PlatformManager.Business.UnitTests.Application;

/// <summary>
/// Khối quyền cấp màn của DM-2 — <c>canWrite</c> / <c>isEditable</c> / <c>editBlockedBy</c> /
/// <c>isCurrentYear</c>. Bảng dưới đây là NGUYÊN VĂN bảng 5 hàng ở §5 mục 15 của
/// doc/contracts/danh-muc-dti.md, tức bộ nghiệm thu do hợp đồng đặt ra chứ không do code đặt ra.
///
/// <para>Hàng 3 (<c>Năm = 2025</c> + <c>Tháng 8</c>) là hàng QUAN TRỌNG NHẤT: một bản cài suy
/// <c>PERIOD_OUT_OF_YEAR</c> từ <i>"năm cũ"</i> thay vì từ <i>"all + năm cũ"</i> vẫn qua được mọi
/// hàng khác và CHỈ hàng này bắt được nó (Q48).</para>
/// </summary>
public class WriteAccessBlockTests
{
    /// <summary>
    /// Mốc "hôm nay" của mọi ca dưới đây: thứ Tư 12/08/2026, tuần ISO 33. Là một NGÀY chứ không
    /// còn là một năm — Q72 (2026-09-10) bắt <c>Evaluate</c> dựng luôn kỳ hiện tại, và kỳ thì
    /// không suy ra được từ mỗi con số năm.
    /// </summary>
    private static readonly DateOnly Today = new(2026, 8, 12);

    [Fact]
    public void HasPermission_CurrentYear_SpecificWeek_IsEditable()
    {
        var block = CriteriaGridDto.Evaluate(true, PeriodRange.IsoWeek(2026, 33), Today);

        Assert.True(block.CanWrite);
        Assert.True(block.IsEditable);
        Assert.Empty(block.EditBlockedBy);
        Assert.True(block.IsCurrentYear);
    }

    [Fact]
    public void HasPermission_CurrentYear_Month_IsBlockedByPeriodNotWeekly()
    {
        var block = CriteriaGridDto.Evaluate(true, PeriodRange.CalendarMonth(2026, 8), Today);

        Assert.True(block.CanWrite);
        Assert.False(block.IsEditable);
        Assert.Equal([EditBlockReasons.PeriodNotWeekly], block.EditBlockedBy);
    }

    /// <summary>
    /// Hàng 3 của bảng nghiệm thu — ĐÚNG MỘT phần tử. Bản trước của hợp đồng ghi hai phần tử và
    /// Q48 (2026-09-10) lật lại: <c>PERIOD_OUT_OF_YEAR</c> chỉ sinh khi <c>period = "all"</c>, mà
    /// "Tháng 8" thì không phải <c>"all"</c>. Với ba mã hôm nay, hai mã lọc LOẠI TRỪ NHAU.
    /// </summary>
    [Fact]
    public void HasPermission_PastYear_Month_YieldsExactlyOneReason()
    {
        var block = CriteriaGridDto.Evaluate(true, PeriodRange.CalendarMonth(2025, 8), Today);

        Assert.False(block.IsEditable);
        Assert.Equal([EditBlockReasons.PeriodNotWeekly], block.EditBlockedBy);
        Assert.False(block.IsCurrentYear);
    }

    /// <summary>Hàng 4 — ca DUY NHẤT sinh <c>PERIOD_OUT_OF_YEAR</c> một mình (T15).</summary>
    [Fact]
    public void HasPermission_PastYear_All_IsBlockedByPeriodOutOfYear()
    {
        var block = CriteriaGridDto.Evaluate(true, PeriodRange.WholeYear(2025), Today);

        Assert.False(block.IsEditable);
        Assert.Equal([EditBlockReasons.PeriodOutOfYear], block.EditBlockedBy);
    }

    /// <summary>
    /// T15 KHÔNG lật Q20 (khẳng định lại bằng Q41): chọn <c>Năm = 2025</c> rồi chọn một TUẦN CỤ
    /// THỂ của 2025 thì vẫn ghi được — kỳ đích đúng bằng thứ người dùng đang nhìn. Thiếu ca âm
    /// này thì một bản cài "cấm ghi vào năm cũ" sẽ qua được nghiệm thu trong khi nó đã xoá sạch
    /// mục đích của Q20 (nhập bù cho kỳ đã qua).
    /// </summary>
    [Fact]
    public void HasPermission_PastYear_SpecificWeek_IsStillEditable()
    {
        var block = CriteriaGridDto.Evaluate(true, PeriodRange.IsoWeek(2025, 33), Today);

        Assert.True(block.IsEditable);
        Assert.Empty(block.EditBlockedBy);
        Assert.False(block.IsCurrentYear);
    }

    /// <summary>
    /// Hàng 5 — thiếu quyền thì ĐÚNG MỘT phần tử, KHÔNG kèm hai mã lọc dù bộ lọc lúc đó cũng có
    /// thể đang sai. Hai mã kia là lời mời "đổi bộ lọc đi rồi sửa được"; nói câu đó với người
    /// không có quyền là dắt họ đi một vòng rồi vẫn không sửa được.
    /// </summary>
    [Theory]
    [InlineData(2026, PeriodKind.Week)]
    [InlineData(2025, PeriodKind.Month)]
    [InlineData(2025, PeriodKind.All)]
    public void NoPermission_Always_YieldsExactlyNoWritePermission(int year, PeriodKind kind)
    {
        var period = kind switch
        {
            PeriodKind.Week => PeriodRange.IsoWeek(year, 33),
            PeriodKind.Month => PeriodRange.CalendarMonth(year, 8),
            _ => PeriodRange.WholeYear(year),
        };

        var block = CriteriaGridDto.Evaluate(false, period, Today);

        Assert.False(block.CanWrite);
        Assert.False(block.IsEditable);
        Assert.Equal([EditBlockReasons.NoWritePermission], block.EditBlockedBy);
    }

    /// <summary>
    /// Bất biến của hợp đồng: <c>isEditable = true ⟺ canWrite = true VÀ editBlockedBy = []</c>.
    /// Kiểm trên toàn bộ tổ hợp thay vì tin vào từng ca rời — một điều kiện thứ tư thêm sau này
    /// vẫn phải giữ bất biến này.
    /// </summary>
    [Fact]
    public void IsEditable_IsAlways_EquivalentTo_CanWriteAndNoReasons()
    {
        PeriodRange[] periods =
        [
            PeriodRange.IsoWeek(2026, 33),
            PeriodRange.IsoWeek(2025, 33),
            PeriodRange.CalendarMonth(2026, 8),
            PeriodRange.CalendarMonth(2025, 8),
            PeriodRange.WholeYear(2026),
            PeriodRange.WholeYear(2025),
        ];

        foreach (var period in periods)
        {
            foreach (var canWrite in new[] { true, false })
            {
                var block = CriteriaGridDto.Evaluate(canWrite, period, Today);

                Assert.Equal(block.CanWrite && block.EditBlockedBy.Count == 0, block.IsEditable);

                // editBlockedBy LUÔN có mặt, rỗng chứ không null — trả null làm FE nhận undefined
                // rồi mọi chỗ .includes(...) ném lỗi.
                Assert.NotNull(block.EditBlockedBy);
            }
        }
    }

    /// <summary>
    /// Q72 — <c>currentPeriod</c>/<c>currentPeriodLabel</c> là TUẦN ISO CHỨA HÔM NAY, không phải
    /// kỳ đang xem. Ca này cố ý xem một kỳ KHÁC (tuần 12 của một năm khác) để hai thứ đó không
    /// thể trùng nhau — một bản cài trả nhầm kỳ đang xem sẽ đỏ ở đây.
    /// </summary>
    [Fact]
    public void CurrentPeriod_IsTheIsoWeekOfToday_NotThePeriodBeingViewed()
    {
        var block = CriteriaGridDto.Evaluate(true, PeriodRange.IsoWeek(2025, 12), Today);

        Assert.Equal("2026-W33", block.CurrentPeriod);
        Assert.Equal("Tuần 33/2026 (10/08 – 16/08/2026)", block.CurrentPeriodLabel);
    }

    /// <summary>
    /// Q72 — hai trường KHÔNG BAO GIỜ vắng mặt: kể cả khi thiếu quyền, kể cả ở mọi đơn vị kỳ.
    /// Chúng mô tả LỊCH, không mô tả quyền. Đây là bất biến mà hợp đồng nêu đích danh, và cũng là
    /// chỗ dễ hỏng nhất — nhánh <c>canWrite = false</c> thoát sớm khỏi <c>Evaluate</c>.
    /// </summary>
    [Fact]
    public void CurrentPeriod_IsPresent_EvenWithoutPermission_AndInEveryPeriodKind()
    {
        PeriodRange[] periods =
        [
            PeriodRange.IsoWeek(2026, 33),
            PeriodRange.CalendarMonth(2025, 8),
            PeriodRange.WholeYear(2025),
        ];

        foreach (var period in periods)
        {
            foreach (var canWrite in new[] { true, false })
            {
                var block = CriteriaGridDto.Evaluate(canWrite, period, Today);

                Assert.Equal("2026-W33", block.CurrentPeriod);
                Assert.False(string.IsNullOrWhiteSpace(block.CurrentPeriodLabel));
            }
        }
    }
}

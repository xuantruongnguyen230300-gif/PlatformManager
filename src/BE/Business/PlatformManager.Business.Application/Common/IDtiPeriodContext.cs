using PlatformManager.Core.Application.Common.Interfaces;

namespace PlatformManager.Business.Application.Common;

/// <summary>
/// "Tuần ISO chứa HÔM NAY" — một câu hỏi về lịch, hỏi ở nhiều chỗ trong cụm DTI.
///
/// <para><b>Vì sao là một seam mỏng chứ không gọi thẳng <c>IDateTimeProvider</c> mỗi chỗ:</b> phép
/// quy "hôm nay" → tuần ISO phải cho ra CÙNG một kết quả trong CÙNG một request. Gọi đồng hồ hai
/// lần là mở cửa cho ca request rơi đúng nửa đêm và hai giá trị nói về hai ngày khác nhau — đúng
/// lớp lỗi không bao giờ tái hiện được. Gói lại một chỗ cũng làm các handler ghi khỏi phải lặp lại
/// hai dòng quy đổi giống hệt nhau.</para>
/// </summary>
public interface IDtiPeriodContext
{
    /// <summary>Ngày hệ thống, theo đồng hồ SERVER — không bao giờ theo đồng hồ máy khách (Q40).</summary>
    DateOnly Today();

    /// <summary>Tuần ISO chứa <see cref="Today"/> — "kỳ hiện tại" của §5.1.</summary>
    PeriodRange CurrentWeek();
}

/// <inheritdoc />
public sealed class DtiPeriodContext(IDateTimeProvider clock) : IDtiPeriodContext
{
    public DateOnly Today() => DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);

    public PeriodRange CurrentWeek() => PeriodRange.IsoWeekOf(Today());
}

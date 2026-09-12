using System.Globalization;

namespace PlatformManager.Business.Application.Common;

/// <summary>
/// Đọc tham số <c>period</c> của DM-2 (và của mọi đường ghi ở vòng 2) thành một
/// <see cref="PeriodRange"/> — doc/contracts/danh-muc-dti.md §1 +
/// spec/danh-muc-dti/business-rules.md §5.1.
///
/// <para>Bộ đọc CỐ Ý chặt: <c>"2026-W99"</c>, <c>"2026-13"</c>, <c>"tuần 33"</c> đều là SAI
/// KHUÔN ⇒ <c>400 CRITERIA.ASSESSMENT_PERIOD_INVALID</c>, không âm thầm rơi về <c>"all"</c>.
/// Rơi về mặc định nghĩa là một client gửi sai vẫn nhận 200 với dữ liệu của một kỳ khác — ca
/// hỏng người dùng không có cách nào phát hiện.</para>
/// </summary>
public static class PeriodParser
{
    /// <summary>
    /// <paramref name="period"/> vắng mặt hoặc rỗng ⇒ <c>"all"</c> của <paramref name="year"/>
    /// (mặc định của ĐƯỜNG ĐỌC — DM-2). ⚠️ Đường GHI thì ngược lại: <c>period</c> vắng mặt là
    /// LỖI (<c>CRITERIA.ASSESSMENT_PERIOD_REQUIRED</c>, §5.3), server không chọn hộ. Vòng 1 chưa
    /// có đường ghi nào; khi thêm, ĐỪNG dùng lại hàm này cho nó.
    /// </summary>
    public static bool TryParse(string? period, int year, out PeriodRange range)
    {
        range = PeriodRange.WholeYear(year);

        if (string.IsNullOrWhiteSpace(period))
            return true;

        var value = period.Trim();

        if (string.Equals(value, PeriodValues.All, StringComparison.OrdinalIgnoreCase))
            return true;

        // "YYYY-Www" — chữ W hoa, đúng 8 ký tự. So ordinal, không chấp nhận "w" thường: chuỗi này
        // là KHOÁ do FE lấy nguyên văn từ DB-3 trả về, không phải thứ người dùng gõ.
        if (value.Length == 8 && value[4] == '-' && value[5] == 'W')
        {
            if (!int.TryParse(value.AsSpan(0, 4), NumberStyles.None, CultureInfo.InvariantCulture, out var isoYear)
                || !int.TryParse(value.AsSpan(6, 2), NumberStyles.None, CultureInfo.InvariantCulture, out var week))
                return false;

            // Số tuần hợp lệ phụ thuộc NĂM: năm ISO có 52 hoặc 53 tuần. Kiểm bằng lịch thật thay
            // vì hằng số 53 — "2026-W53" là chuỗi sai nếu 2026 chỉ có 52 tuần.
            if (isoYear < 1 || isoYear > 9999 || week < 1 || week > ISOWeek.GetWeeksInYear(isoYear))
                return false;

            range = PeriodRange.IsoWeek(isoYear, week);
            return true;
        }

        // "YYYY-MM"
        if (value.Length == 7 && value[4] == '-')
        {
            if (!int.TryParse(value.AsSpan(0, 4), NumberStyles.None, CultureInfo.InvariantCulture, out var monthYear)
                || !int.TryParse(value.AsSpan(5, 2), NumberStyles.None, CultureInfo.InvariantCulture, out var month))
                return false;

            if (monthYear < 1 || monthYear > 9999 || month is < 1 or > 12)
                return false;

            range = PeriodRange.CalendarMonth(monthYear, month);
            return true;
        }

        return false;
    }
}

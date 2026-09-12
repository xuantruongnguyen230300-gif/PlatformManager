namespace PlatformManager.Business.Application.Common;

/// <summary>
/// Lý do một KỲ ĐÍCH bị từ chối — bước 1 của luật ghi
/// (spec/danh-muc-dti/business-rules.md §5.3).
///
/// <para><b>Trả về lý do chứ KHÔNG trả về <c>ErrorDescriptor</c>, có chủ đích:</b> ba đường ghi
/// dùng CHUNG luật nhưng mỗi đường có catalog mã lỗi riêng — dialog/inline trả
/// <c>CRITERIA.ASSESSMENT_PERIOD_*</c>, import trả <c>IMPORT.PERIOD_*</c>. Gói mã lỗi vào đây thì
/// hoặc phải nhân đôi bộ giải, hoặc phải cho một mã của nghiệp vụ này rò sang endpoint của nghiệp
/// vụ kia. Cùng khuôn với <c>ImportFileRejection</c> của Core.</para>
/// </summary>
public enum WritePeriodRejection
{
    /// <summary>Không bị từ chối — <see cref="WritePeriodResolution.TargetWeek"/> có giá trị.</summary>
    None = 0,

    /// <summary><c>period</c> vắng mặt. Server KHÔNG chọn hộ — xem docstring của bộ giải.</summary>
    Required,

    /// <summary>Chuỗi sai khuôn (<c>"2026-W99"</c>, <c>"tuần 33"</c>…).</summary>
    Invalid,

    /// <summary>Đúng khuôn nhưng là kỳ THÁNG (Q37) — tháng là tổng hợp tự tính, chỉ đọc.</summary>
    NotWeekly,

    /// <summary><c>"all"</c> trong khi <c>year</c> đang xem ≠ năm hiện tại (T15).</summary>
    OutOfYear,
}

/// <param name="TargetWeek">Tuần ISO đích đã quy đổi; <c>null</c> khi bị từ chối.</param>
/// <param name="Rejection">Lý do từ chối; <see cref="WritePeriodRejection.None"/> khi nhận.</param>
public readonly record struct WritePeriodResolution(PeriodRange? TargetWeek, WritePeriodRejection Rejection)
{
    public bool IsAccepted => TargetWeek is not null;

    public static WritePeriodResolution Accept(PeriodRange week) => new(week, WritePeriodRejection.None);

    public static WritePeriodResolution Reject(WritePeriodRejection reason) => new(null, reason);
}

/// <summary>
/// <b>Bước 1 của luật ghi — xác định KỲ ĐÍCH</b> (spec/danh-muc-dti/business-rules.md §5.3).
/// Dùng CHUNG cho cả ba đường ghi: dialog (DM-4), sửa inline (DM-6), import (DM-7).
///
/// <para>🎯 <b>Nguyên tắc đằng sau ba luật Q26 + Q37 + T15</b> — đối chiếu với nó, đừng đối chiếu
/// với ba luật rời: <i>lời ghi không bao giờ được rơi vào một kỳ mà người dùng KHÔNG nhìn thấy
/// trên màn hình.</i> Một lối ghi thứ tư về sau sẽ có lỗ hổng thứ tư mà ba luật này không phủ.</para>
///
/// <para><b>SERVER quy đổi, FE gửi nguyên</b> (Q40): FE gửi nguyên giá trị ô <c>Kỳ trong năm</c>,
/// kể cả <c>"all"</c>, kèm <c>year</c>. Ba lý do phép quy đổi chỉ được có một chỗ cài: "hôm nay"
/// là đồng hồ của SERVER (client lệch múi giờ sẽ ghi vào tuần khác); ba đường ghi phải cho ra
/// cùng một kết quả; và FE biết kết quả ngay trong phản hồi qua <c>assessmentPeriod</c>.</para>
/// </summary>
public static class WritePeriodResolver
{
    /// <summary>
    /// Quy <c>period</c> + <c>year</c> của một lời ghi về đúng MỘT tuần ISO.
    ///
    /// <list type="table">
    ///   <item><term><c>"YYYY-Www"</c></term><description>đúng tuần đó — kể cả tuần ĐÃ QUA, kể cả
    ///   năm trước (Q20). <paramref name="year"/> bị bỏ qua: năm đã nằm sẵn trong chuỗi tuần.</description></item>
    ///   <item><term><c>"all"</c></term><description>tuần ISO chứa hôm nay (Q26) — CHỈ KHI
    ///   <paramref name="year"/> là năm hiện tại (T15).</description></item>
    ///   <item><term><c>"YYYY-MM"</c></term><description>TỪ CHỐI (Q37).</description></item>
    ///   <item><term>vắng mặt</term><description>TỪ CHỐI — server không chọn hộ.</description></item>
    /// </list>
    ///
    /// <para><b><c>period</c> vắng mặt KHÔNG rơi về <c>"all"</c>.</b> <c>"all"</c> là một lựa chọn
    /// người dùng nhìn thấy trên màn hình và chủ động để nguyên; <c>period</c> thiếu là một client
    /// quên gửi. Đối xử hai ca như nhau nghĩa là mọi bug quên-gửi-tham-số đều âm thầm ghi vào tuần
    /// này. Đây cũng là lý do KHÔNG dùng lại <c>PeriodParser.TryParse</c> cho đường ghi — bộ đọc
    /// đó mặc định về <c>"all"</c>, đúng cho đường ĐỌC và sai cho đường ghi.</para>
    ///
    /// <para><b><paramref name="year"/> <c>null</c> khi <c>period = "all"</c></b> cho ra
    /// <see cref="WritePeriodRejection.OutOfYear"/>. Đó là lưới an toàn, không phải đường chính:
    /// ca này đã bị validator chặn thành <c>400 ValidationError</c> + <c>fields</c> trước khi tới
    /// handler (hợp đồng: "lỗi validate trường bắt buộc, không cần một mã nghiệp vụ riêng").</para>
    /// </summary>
    /// <param name="today">Ngày hệ thống — lấy từ <c>IDateTimeProvider</c>, KHÔNG đọc đồng hồ tại chỗ.</param>
    public static WritePeriodResolution Resolve(string? period, int? year, DateOnly today)
    {
        if (string.IsNullOrWhiteSpace(period))
            return WritePeriodResolution.Reject(WritePeriodRejection.Required);

        var value = period.Trim();

        if (string.Equals(value, PeriodValues.All, StringComparison.OrdinalIgnoreCase))
        {
            return year == today.Year
                ? WritePeriodResolution.Accept(PeriodRange.IsoWeekOf(today))
                : WritePeriodResolution.Reject(WritePeriodRejection.OutOfYear);
        }

        // Khuôn chuỗi dùng lại bộ đọc của đường ĐỌC — một bộ phân tích cú pháp, không hai. Khác
        // biệt duy nhất giữa hai đường nằm ở CHÍNH SÁCH (mặc định, kỳ tháng có hợp lệ không), và
        // chính sách thì ở đây.
        if (!PeriodParser.TryParse(value, today.Year, out var range))
            return WritePeriodResolution.Reject(WritePeriodRejection.Invalid);

        return range.Kind switch
        {
            PeriodKind.Week => WritePeriodResolution.Accept(range),

            // Kỳ tháng đúng khuôn nhưng không phải chỗ nhập liệu: mô hình lưu theo AssessmentDate
            // (một NGÀY), và KHÔNG ngày neo nào trong một tháng bất kỳ có tuần ISO nằm gọn trong
            // tháng đó (neo 31/08/2026 ⇒ tuần 36, vắt sang tháng 9). Lỗi này không vá được bằng
            // cách chọn ngày neo khéo hơn — §5.1.
            PeriodKind.Month => WritePeriodResolution.Reject(WritePeriodRejection.NotWeekly),

            // PeriodKind.All chỉ sinh ra từ chuỗi "all" (đã xử lý ở trên) hoặc từ nhánh mặc định
            // của bộ đọc — mà nhánh đó không tới được đây vì chuỗi rỗng đã bị chặn.
            _ => WritePeriodResolution.Reject(WritePeriodRejection.Invalid),
        };
    }

    /// <summary>
    /// <b>Luật neo ngày</b> (§5.3 bước 2) — kỳ đích luôn là một tuần (Q37) nên luật gọn còn một dòng:
    ///
    /// <code>
    /// AssessmentDate = hôm nay                  nếu hôm nay nằm TRONG tuần đích
    ///                = Chủ nhật của tuần đích   nếu không
    /// </code>
    ///
    /// <para>Không bao giờ sinh ra một ngày NẰM NGOÀI tuần đích, và không bao giờ sinh ra ngày ở
    /// tương lai trừ khi chính tuần đích ở tương lai.</para>
    /// </summary>
    public static DateOnly AnchorDate(PeriodRange targetWeek, DateOnly today) =>
        targetWeek.Contains(today) ? today : targetWeek.End;
}

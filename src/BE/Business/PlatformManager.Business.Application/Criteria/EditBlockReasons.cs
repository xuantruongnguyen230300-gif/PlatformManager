namespace PlatformManager.Business.Application.Criteria;

/// <summary>
/// Mã lý do của <c>editBlockedBy</c> — doc/contracts/danh-muc-dti.md DM-2 mục 3.
///
/// <para><b>Một bộ từ vựng, không hai.</b> Hai mã kỳ TRÙNG TÊN với hai mã lỗi mà server trả khi
/// một request ghi lọt qua lớp FE, chỉ bỏ tiền tố <c>CRITERIA.ASSESSMENT_</c>:
/// <c>PERIOD_NOT_WEEKLY</c> ↔ <c>CRITERIA.ASSESSMENT_PERIOD_NOT_WEEKLY</c>. Ánh xạ cơ học như vậy
/// để bảng dịch của FE dùng lại được, và để hai đường không mô tả cùng một tình huống bằng hai
/// cái tên khác nhau. <c>NO_WRITE_PERMISSION</c> không có mã lỗi tương ứng vì ca đó ra <c>403</c>
/// từ filter, không qua catalog DTI.</para>
/// </summary>
public static class EditBlockReasons
{
    /// <summary>
    /// <c>canWrite = false</c> (Q39). Trả về ĐÚNG MỘT phần tử, không kèm hai mã kia dù bộ lọc lúc
    /// đó cũng có thể đang sai: hai mã kia là lời mời <i>"đổi bộ lọc đi rồi sửa được"</i>, nói câu
    /// đó với người không có quyền là dắt họ đi một vòng rồi vẫn không sửa được.
    /// </summary>
    public const string NoWritePermission = "NO_WRITE_PERMISSION";

    /// <summary>Kỳ đang chọn là THÁNG (Q37). Lối ra: đổi ô <c>Kỳ trong năm</c>.</summary>
    public const string PeriodNotWeekly = "PERIOD_NOT_WEEKLY";

    /// <summary>
    /// <c>period = "all"</c> VÀ <c>year</c> ≠ năm hiện tại (T15). Lối ra: đổi ô
    /// <c>Năm đánh giá</c>, hoặc chọn một tuần cụ thể của năm đó.
    ///
    /// <para>⚠️ Điều kiện là <b><c>"all"</c> + năm cũ</b>, KHÔNG phải "năm cũ". Một bản cài suy mã
    /// này từ <i>"năm cũ"</i> vẫn qua được mọi hàng nghiệm thu khác và chỉ hàng
    /// <c>Năm = 2025 + Tháng 8</c> bắt được nó — hàng đó phải cho ĐÚNG
    /// <c>["PERIOD_NOT_WEEKLY"]</c>, một phần tử (Q48).</para>
    /// </summary>
    public const string PeriodOutOfYear = "PERIOD_OUT_OF_YEAR";
}

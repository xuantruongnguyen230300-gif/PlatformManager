using PlatformManager.Core.Domain.Common;

namespace PlatformManager.Business.Domain.Entities;

/// <summary>
/// Catalog lỗi BẤT BIẾN của <see cref="Criteria"/>. Xem <see cref="CriteriaGroupDomainErrors"/>
/// cho lý do đặt tên file <c>*DomainErrors.cs</c>.
///
/// <para>Ba mã dưới đây là lưới CUỐI ở tầng Domain. Đường ghi (DM-3/DM-4/DM-7 — vòng 2) vẫn
/// phải kiểm trước và trả đúng mã của hợp đồng (<c>CRITERIA.CODE_FORMAT_INVALID</c>,
/// <c>CRITERIA.CODE_SEGMENT_TOO_LONG</c>, <c>CRITERIA.CODE_TOO_LONG</c> —
/// doc/contracts/danh-muc-dti.md §2), vì mã domain đi ra HTTP 422 chứ không phải 400. Có cả hai
/// lớp là CỐ Ý: lớp trên cho câu chữ đúng, lớp dưới cho bất biến không lách được.</para>
///
/// <para>🛑 <b>Nhưng tầng Application KHÔNG được KHAI LẠI những mã này</b> (đo được 2026-09-11):
/// <c>ErrorCatalogTests</c> canh tính DUY NHẤT của <c>businessCode</c> trên toàn hệ thống, gộp
/// chung một rổ <c>DomainError</c> và <c>ErrorDescriptor</c> — hai bản khai cùng mã là test ĐỎ.
/// Cách đúng: <c>CriteriaErrors</c> DẪN XUẤT descriptor 400 từ chính bản ghi ở đây, nên vẫn là MỘT
/// mã, MỘT nguồn khai, hai ánh xạ HTTP do đường đi quyết định. Bản trước của đoạn này mô tả hai
/// lớp mà không nói ra ràng buộc đó, và lượt thi công DM-3 đã vấp đúng chỗ.</para>
/// </summary>
public static class CriteriaDomainErrors
{
    public static readonly DomainError CodeRequired = new(
        "CRITERIA.CODE_REQUIRED", "Mã chỉ tiêu không được để trống.");

    public static readonly DomainError CodeFormatInvalid = new(
        "CRITERIA.CODE_FORMAT_INVALID",
        "Mã chỉ tiêu chỉ gồm chữ số ngăn bằng dấu chấm đơn, không có đoạn rỗng.");

    /// <summary>
    /// Câu CỐ Ý không mang chỗ giữ tham số. Mọi <c>DomainError</c> của repo hôm nay đều là câu
    /// cố định, và <c>ExceptionHandlingBehavior.BuildErrorResponse</c> truyền <c>null</c> cho
    /// <c>messageParams</c> vì đúng lý do đó — mã domain đầu tiên mang tham số sẽ ra dây với câu
    /// đã ráp sẵn nhưng KHÔNG có bản tham số rời, tức lệch khỏi cơ chế i18n
    /// (doc/huong_dan/wiki-core/be/16-i18n-va-ma-loi.md §10). Trần chữ số là một hằng số của
    /// nghiệp vụ, không phải giá trị của từng lần vi phạm, nên viết thẳng vào câu là đúng.
    /// Bản có tham số (<c>{MaxSegmentDigits}</c>) nằm ở <c>ErrorDescriptor</c> tầng Application —
    /// doc/contracts/danh-muc-dti.md §2.
    /// </summary>
    public static readonly DomainError CodeSegmentTooLong = new(
        "CRITERIA.CODE_SEGMENT_TOO_LONG", "Mỗi đoạn của mã chỉ tiêu tối đa 4 chữ số.");

    public static readonly DomainError NameRequired = new(
        "CRITERIA.NAME_REQUIRED", "Tên chỉ tiêu không được để trống.");

    public static readonly DomainError MaxScoreNotPositive = new(
        "CRITERIA.MAX_SCORE_INVALID", "Điểm tối đa phải lớn hơn 0.");
}

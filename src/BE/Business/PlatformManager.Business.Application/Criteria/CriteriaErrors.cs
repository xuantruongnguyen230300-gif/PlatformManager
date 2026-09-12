using PlatformManager.Business.Domain.Entities;
using PlatformManager.Core.Application.Common.Results;
using PlatformManager.Core.Domain.Common;

namespace PlatformManager.Business.Application.Criteria;

/// <summary>
/// Catalog mã lỗi miền <c>CRITERIA.*</c> — doc/contracts/danh-muc-dti.md §2.
///
/// <para><b>File này bị cưỡng chế bằng máy, không phải quy ước thẩm mỹ:</b> <c>ArchTests</c> nhận
/// diện file catalog bằng ĐUÔI TÊN <c>Errors.cs</c> (<c>ErrorCodeSourceTests</c>) và đọc GIÁ TRỊ
/// thật của từng descriptor qua reflection để canh khuôn <c>MIEN.MA_LOI</c> + tính duy nhất
/// (<c>ErrorCatalogTests</c>). Dựng <c>ErrorDescriptor</c> bằng chuỗi literal trong thân handler
/// là test ĐỎ.</para>
///
/// <para><b><c>Retryable = false</c> cho toàn bộ mã của cụm này</b> (§2): không mã nào ở đây là
/// lỗi thoáng qua — sai trạng thái, sai khuôn kỳ đều cần người sửa dữ liệu rồi mới gọi lại.
/// Đặt <c>true</c> nghĩa là bảo FE tự thử lại, mà thử lại thì lần nào cũng hỏng y hệt.</para>
///
/// <para>Mã của đường GHI (DM-3…DM-6) khai ở nửa dưới, thêm 2026-09-11 <b>cùng lượt</b> với handler
/// dùng chúng — đúng luật "khai trước mà không có nơi ném là dựng một hợp đồng chưa ai giữ: mã ra
/// tới catalog, FE thấy nó trong tài liệu, nhưng không đường nào sinh ra nó".</para>
///
/// <para>🛑 <b><c>CRITERIA.PROGRESS_PERCENT_INVALID</c> CỐ Ý KHÔNG khai</b> dù
/// doc/contracts/danh-muc-dti.md DM-6 có liệt nó. Hai file chủ nói ngược nhau, và luật nghiệp vụ
/// thắng: spec/danh-muc-dti/business-rules.md §3.2 quy định <i>ngoài miền 0..100 thì KẸP về biên,
/// KHÔNG báo lỗi khi sửa inline</i>, và entity đã kẹp thật
/// (<c>CriteriaAssessment.SetProgressPercent</c>). Khai một mã không đường nào ném được chính là
/// thứ đoạn ngay trên vừa cấm. Xung đột đã ghi lại ở card để lượt sau không khai lại nó.</para>
/// </summary>
public static class CriteriaErrors
{
    /// <summary>
    /// Tham số <c>status</c> không thuộc 4 giá trị của §4 — doc/contracts/danh-muc-dti.md
    /// §"Mã lỗi của DM-2". Dùng chung cho MỌI endpoint nhận tham số <c>status</c> của miền này,
    /// không riêng DM-2.
    ///
    /// <para><b>Không âm thầm bỏ lọc.</b> Trả 200 với bộ lọc bị lờ đi là ca hỏng tệ nhất của một
    /// lưới: người dùng thấy dữ liệu không khớp bộ lọc đang hiện và không có gì báo cho họ.</para>
    /// </summary>
    public static readonly ErrorDescriptor StatusInvalid = new(
        "CRITERIA.STATUS_INVALID", ErrorCode.ValidationError,
        "Trạng thái '{Status}' không hợp lệ.");

    /// <summary>
    /// Chuỗi <c>period</c> sai khuôn (<c>"all"</c> / <c>"YYYY-Www"</c> / <c>"YYYY-MM"</c>).
    ///
    /// <para>Đường ĐỌC dùng LẠI đúng mã của đường ghi, không sinh mã thứ hai
    /// (doc/contracts/danh-muc-dti.md §"Mã lỗi của DM-2"): cùng một chuỗi sai khuôn thì cùng một
    /// câu chữ, và FE chỉ phải dịch một lần. Hai mã kỳ còn lại (<c>…NOT_WEEKLY</c>,
    /// <c>…OUT_OF_YEAR</c>) KHÔNG áp cho đường đọc — đọc một kỳ tháng hay một năm cũ là hợp lệ.</para>
    /// </summary>
    public static readonly ErrorDescriptor AssessmentPeriodInvalid = new(
        "CRITERIA.ASSESSMENT_PERIOD_INVALID", ErrorCode.ValidationError,
        "Kỳ '{Period}' không đúng khuôn 'all' / 'YYYY-Www' / 'YYYY-MM'.");

    // ── Đường GHI — DM-3 (tạo) · DM-4 (dialog) · DM-5 (xoá) · DM-6 (sửa inline) ──────────────

    /// <summary>Không có chỉ tiêu nào mang id đó trong tập chưa xoá mềm.</summary>
    public static readonly ErrorDescriptor NotFound = new(
        "CRITERIA.NOT_FOUND", ErrorCode.NotFound,
        "Không tìm thấy chỉ tiêu.");

    /// <summary>
    /// <c>CODE_TOO_LONG</c> là mã DUY NHẤT của nhóm "mã/tên/điểm sai khuôn" mà tầng Domain KHÔNG
    /// khai — <c>CriteriaCode.IsWellFormed</c> gộp phép kiểm độ dài vào chung với khuôn, nên domain
    /// chỉ ném <c>CODE_FORMAT_INVALID</c> cho cả hai. Ở đây tách ra được vì handler kiểm độ dài
    /// TRƯỚC khuôn, và câu hướng dẫn của hai ca khác hẳn nhau.
    /// </summary>
    public static readonly ErrorDescriptor CodeTooLong = new(
        "CRITERIA.CODE_TOO_LONG", ErrorCode.ValidationError,
        "Mã chỉ tiêu tối đa {MaxLength} ký tự.");

    // ── Năm mã DÙNG LẠI mã của tầng Domain, chỉ đổi mã HTTP ─────────────────────────────────
    //
    // 🛑 KHÔNG khai lại chúng thành `static readonly ErrorDescriptor` ở đây. ErrorCatalogTests
    // canh TÍNH DUY NHẤT của businessCode trên toàn hệ thống, gộp chung một rổ `DomainError` và
    // `ErrorDescriptor` — hai bản khai cùng mã là test ĐỎ, và lý do nó đỏ đúng: FE chỉ có MỘT bản
    // dịch cho một mã, nên bản thứ hai sẽ nhận câu hướng dẫn của bản thứ nhất.
    //
    // Vì sao vẫn cần một bản ở tầng này: mã domain đi ra HTTP 422 (ExceptionHandlingBehavior dịch
    // theo LOẠI exception), còn hợp đồng DM-3/DM-4 đòi 400 — đây là khuôn dữ liệu sai, không phải
    // vi phạm luật nghiệp vụ trên dữ liệu hợp khuôn. Dẫn xuất tại chỗ giữ được cả hai: MỘT mã, MỘT
    // nguồn khai, hai ánh xạ HTTP do ĐƯỜNG ĐI quyết định.
    //
    // ⚠️ Khuôn `new ErrorDescriptor(biến, …)` này KHÔNG vi phạm ErrorCodeSourceTests: luật đó cấm
    // dựng bằng CHUỖI LITERAL, và có hẳn một ca đối chứng khẳng định dạng ghép-từ-mã-đã-có là hợp
    // lệ (chỗ tương tự ở ExceptionHandlingBehavior).

    /// <summary>
    /// Lấy MỘT bản ghi lỗi đã khai ở tầng Domain và gắn mã HTTP của đường GHI TAY (<c>400</c>).
    /// Không đặt ra mã mới — <paramref name="error"/> là nguồn duy nhất của <c>businessCode</c>.
    ///
    /// <para>⚠️ <b>PHẢI là một PHƯƠNG THỨC, không phải thuộc tính hay trường</b> (đo được
    /// 2026-09-11): <c>ErrorCatalogTests</c> quét cả <c>GetFields</c> LẪN <c>GetProperties</c> của
    /// mọi catalog, nên phơi năm mã này ra thành <c>public static</c> property vẫn bị tính là bản
    /// khai THỨ HAI của cùng một mã và test vẫn ĐỎ. Gọi tại chỗ dùng thì không có bản khai nào để
    /// mà trùng.</para>
    ///
    /// <para>Nơi gọi vì thế viết <c>CriteriaErrors.AsValidation(CriteriaDomainErrors.CodeRequired)</c>
    /// — dài hơn một chút, nhưng nó nói thẳng ra điều đang xảy ra: mã đến TỪ tầng Domain, chỉ có mã
    /// HTTP là của đường này.</para>
    /// </summary>
    /// <param name="template">
    /// Khuôn câu thay thế, khi bản Application cần chỗ giữ ĐẶT TÊN mà bản Domain cố ý không có
    /// (ca duy nhất hôm nay: <c>CODE_SEGMENT_TOO_LONG</c>). <c>null</c> = dùng nguyên câu của Domain.
    /// </param>
    public static ErrorDescriptor AsValidation(DomainError error, string? template = null) =>
        new(error.BusinessCode, ErrorCode.ValidationError, template ?? error.MessageTemplate);

    /// <summary>
    /// Trùng mã trong tập CHƯA xoá mềm. <c>409</c> chứ không <c>400</c>: đây là xung đột với trạng
    /// thái hiện có của dữ liệu, không phải khuôn dữ liệu sai.
    ///
    /// <para>Xoá mềm một mã rồi tạo lại đúng mã đó PHẢI thành công — unique index là partial theo
    /// <c>IsDeleted = false</c> (§1.4), nên phép kiểm ở handler cũng chỉ được nhìn tập chưa xoá.</para>
    /// </summary>
    public static readonly ErrorDescriptor DuplicateCode = new(
        "CRITERIA.DUPLICATE_CODE", ErrorCode.Conflict,
        "Mã chỉ tiêu '{Code}' đã tồn tại.");

    /// <summary>
    /// <b>422, KHÔNG phải 404</b> — <c>groupId</c> là FK nằm TRONG payload, không phải resource
    /// chính của route. Đúng bảng phân biệt ở doc/huong_dan/quy-uoc/be-api-controller.md
    /// §"Error → HTTP status mapping"; bản card cũ ghi 404 và đã sửa.
    /// </summary>
    public static readonly ErrorDescriptor GroupNotFound = new(
        "CRITERIA.GROUP_NOT_FOUND", ErrorCode.BusinessRuleError,
        "Nhóm chỉ tiêu không tồn tại.");

    /// <summary>422, cùng lý do với <see cref="GroupNotFound"/>: FK nằm trong payload.</summary>
    public static readonly ErrorDescriptor OwnerNotFound = new(
        "CRITERIA.OWNER_NOT_FOUND", ErrorCode.BusinessRuleError,
        "Người phụ trách không tồn tại.");

    /// <summary>
    /// ⚠️ Ở đường GHI TAY, điểm vượt trần là <b>422</b> — vi phạm luật nghiệp vụ trên dữ liệu đã
    /// hợp khuôn. Khác hẳn đường IMPORT, nơi cùng tình huống là lỗi MỘT DÒNG và job vẫn
    /// <c>Succeeded</c>. Hai kênh, hai cách xử lý ở FE — đó là lý do chúng không dùng chung mã.
    /// </summary>
    public static readonly ErrorDescriptor AssessmentSelfScoreExceedsMax = new(
        "CRITERIA.ASSESSMENT_SELF_SCORE_EXCEEDS_MAX", ErrorCode.BusinessRuleError,
        "Điểm tự đánh giá {SelfScore} vượt điểm tối đa {MaxScore}.");

    /// <inheritdoc cref="AssessmentSelfScoreExceedsMax"/>
    public static readonly ErrorDescriptor AssessmentVerifiedScoreExceedsMax = new(
        "CRITERIA.ASSESSMENT_VERIFIED_SCORE_EXCEEDS_MAX", ErrorCode.BusinessRuleError,
        "Điểm thẩm định {VerifiedScore} vượt điểm tối đa {MaxScore}.");

    /// <summary>
    /// <c>period</c> vắng mặt ở một lời GHI. Server KHÔNG chọn hộ và KHÔNG rơi về <c>"all"</c>:
    /// <c>"all"</c> là lựa chọn người dùng nhìn thấy trên màn hình và chủ động để nguyên, còn
    /// <c>period</c> thiếu là một client quên gửi. Đối xử hai ca như nhau nghĩa là mọi bug
    /// quên-gửi-tham-số đều âm thầm ghi vào tuần này.
    /// </summary>
    public static readonly ErrorDescriptor AssessmentPeriodRequired = new(
        "CRITERIA.ASSESSMENT_PERIOD_REQUIRED", ErrorCode.ValidationError,
        "Thiếu kỳ đích của lời ghi.");

    /// <summary>
    /// Q37 — kỳ THÁNG không phải chỗ nhập liệu. Tách khỏi <see cref="AssessmentPeriodInvalid"/> có
    /// chủ đích: chỉ mã này dựng được câu dẫn đường <i>chọn một tuần trong tháng để sửa</i>. Trong
    /// luồng bình thường FE không kích hoạt nó (chọn tháng thì <c>isEditable = false</c> nên không
    /// có control nào để bấm) — nó là LƯỚI CHẶN PHÍA SERVER.
    /// </summary>
    public static readonly ErrorDescriptor AssessmentPeriodNotWeekly = new(
        "CRITERIA.ASSESSMENT_PERIOD_NOT_WEEKLY", ErrorCode.ValidationError,
        "Kỳ '{Period}' là kỳ tháng — chỉ ghi được theo tuần.");

    /// <summary>T15 — <c>"all"</c> giải nghĩa thành TUẦN HIỆN TẠI, tức một kỳ không nằm trong năm đang xem.</summary>
    public static readonly ErrorDescriptor AssessmentPeriodOutOfYear = new(
        "CRITERIA.ASSESSMENT_PERIOD_OUT_OF_YEAR", ErrorCode.ValidationError,
        "Kỳ 'Tất cả' chỉ dùng được khi đang xem năm hiện tại ({CurrentYear}), không phải năm {Year}.");

    /// <summary>
    /// Token <c>version</c> client gửi lệch token đang lưu — có người khác vừa ghi đè.
    ///
    /// <para>Bản ghi đánh giá có HAI luồng ghi độc lập chạm vào nó (import hàng loạt · sửa tay);
    /// không có phép kiểm này thì người ghi sau âm thầm nuốt thay đổi của người ghi trước.</para>
    /// </summary>
    public static readonly ErrorDescriptor AssessmentConflict = new(
        "CRITERIA.ASSESSMENT_CONFLICT", ErrorCode.Conflict,
        "Bản ghi đánh giá vừa được người khác cập nhật. Tải lại rồi thử lại.");
}

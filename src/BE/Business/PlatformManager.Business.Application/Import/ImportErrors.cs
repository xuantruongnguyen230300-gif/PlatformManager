using PlatformManager.Core.Application.Common.Results;

namespace PlatformManager.Business.Application.Import;

/// <summary>
/// Catalog mã lỗi miền <c>IMPORT.*</c> — doc/contracts/danh-muc-dti.md §2 và DM-7.
///
/// <para><b>File này bị cưỡng chế bằng máy:</b> <c>ArchTests</c> nhận diện file catalog bằng ĐUÔI
/// TÊN <c>Errors.cs</c> (<c>ErrorCodeSourceTests</c>) và đọc GIÁ TRỊ thật của từng descriptor qua
/// reflection để canh khuôn <c>MIEN.MA_LOI</c> + tính duy nhất (<c>ErrorCatalogTests</c>). Dựng
/// <c>ErrorDescriptor</c> bằng chuỗi literal trong thân handler là test ĐỎ.</para>
///
/// <para>⚠️ <b>HAI NHÓM mã, đi HAI đường khác nhau tới FE — tiền tố <c>ROW_</c> là bắt buộc và có
/// nghĩa:</b></para>
/// <list type="table">
///   <item><term>Không có <c>ROW_</c></term><description>lỗi của CẢ REQUEST ⇒ <c>400</c>/<c>404</c>
///   ngay ở bước 1 (hoặc ở endpoint poll). <see cref="ErrorDescriptor.ErrorCode"/> CHÍNH LÀ mã
///   HTTP.</description></item>
///   <item><term>Có <c>ROW_</c></term><description>lỗi của MỘT DÒNG ⇒ job vẫn <c>Succeeded</c>,
///   lỗi nằm trong <c>result.errors[]</c> kèm <c>rowNumber</c>.
///   <see cref="ErrorDescriptor.ErrorCode"/> của chúng KHÔNG BAO GIỜ thành HTTP status — khai
///   <c>ValidationError</c> vì đó là bản chất (dữ liệu người dùng gửi không hợp lệ), không phải
///   vì response sẽ mang 400.</description></item>
/// </list>
///
/// <para><b>Một dòng lỗi KHÔNG làm hỏng cả file</b> (§6.3). Ngược lại, <c>Failed</c> dành cho lỗi
/// HẠ TẦNG (file hỏng, job crash) — và khi đó Q64 bảo đảm <b>không dòng nào</b> được ghi.</para>
///
/// <para><c>Retryable = false</c> cho toàn bộ mã của cụm này (§2): không mã nào là lỗi thoáng qua.
/// Thử lại một <c>FORMAT_UNSUPPORTED</c> thì lần nào cũng hỏng y hệt.</para>
/// </summary>
public static class ImportErrors
{
    // ── Lỗi của CẢ REQUEST — bước 1: POST /api/import ───────────────────────────────────────

    /// <summary>Form không mang phần <c>file</c> nào.</summary>
    public static readonly ErrorDescriptor FileMissing = new(
        "IMPORT.FILE_MISSING", ErrorCode.ValidationError,
        "Chưa chọn file để nạp.");

    /// <summary>
    /// Có phần <c>file</c> nhưng 0 byte. Tách khỏi <see cref="FileMissing"/> vì hai ca có hai
    /// cách sửa khác nhau: một bên là quên chọn file, một bên là file trên máy hỏng/rỗng.
    /// </summary>
    public static readonly ErrorDescriptor FileEmpty = new(
        "IMPORT.FILE_EMPTY", ErrorCode.ValidationError,
        "File '{FileName}' không có nội dung.");

    /// <summary>
    /// Không reader nào nhận file — nhận diện bằng MAGIC BYTE, không bằng phần mở rộng
    /// (doc/huong_dan/wiki-core/be/15-import-export.md §2a). Đây là mã cho ca "đổi đuôi file":
    /// một <c>.xlsx</c> đổi tên thành <c>.xls</c> phải nhận mã này hoặc được đọc đúng, KHÔNG
    /// được ném exception hạ tầng.
    /// </summary>
    public static readonly ErrorDescriptor FormatUnsupported = new(
        "IMPORT.FORMAT_UNSUPPORTED", ErrorCode.ValidationError,
        "Định dạng file '{FileName}' không được hỗ trợ. Chỉ nhận .csv, .xlsx, .xls.");

    /// <summary>
    /// Vượt trần dung lượng (Q12b — mặc định 10 MB, là CẤU HÌNH của Core:
    /// <c>Import:MaxFileSizeBytes</c>). Kiểm TRƯỚC khi đọc byte nội dung nào và trước khi ghi file
    /// tạm — <c>IImportFileReaderSelector</c> làm việc đó.
    ///
    /// <para>⚠️ Trần này áp cho DUNG LƯỢNG, độc lập với trần SỐ DÒNG. Một file 200 KB có 500.000
    /// dòng vẫn phải bị chặn — bởi trần số dòng, không phải bởi mã này.</para>
    /// </summary>
    public static readonly ErrorDescriptor FileTooLarge = new(
        "IMPORT.FILE_TOO_LARGE", ErrorCode.ValidationError,
        "File vượt trần {MaxFileSizeBytes} byte (file đang gửi {FileSizeBytes} byte).");

    /// <summary>
    /// <c>period</c> vắng mặt. Server KHÔNG chọn hộ và KHÔNG rơi về <c>"all"</c>: <c>"all"</c> là
    /// một lựa chọn người dùng nhìn thấy trên màn hình và chủ động để nguyên, còn <c>period</c>
    /// thiếu là một client quên gửi. Đối xử hai ca như nhau nghĩa là mọi bug quên-gửi-tham-số đều
    /// âm thầm nạp 62 dòng vào tuần này.
    /// </summary>
    public static readonly ErrorDescriptor PeriodRequired = new(
        "IMPORT.PERIOD_REQUIRED", ErrorCode.ValidationError,
        "Thiếu kỳ đích của file nạp.");

    /// <summary>Chuỗi sai khuôn (<c>"2026-W99"</c>, <c>"tuần 33"</c>…).</summary>
    public static readonly ErrorDescriptor PeriodInvalid = new(
        "IMPORT.PERIOD_INVALID", ErrorCode.ValidationError,
        "Kỳ '{Period}' không đúng khuôn 'all' / 'YYYY-Www'.");

    /// <summary>
    /// Kỳ THÁNG (Q37). Tách khỏi <see cref="PeriodInvalid"/> có chủ đích: chỉ mã này dựng được câu
    /// dẫn đường <i>"nạp số liệu cả tháng thì nạp theo từng tuần"</i>, và đó cũng đúng cách BA đang
    /// làm việc — file mẫu của họ là file MỘT KỲ.
    /// </summary>
    public static readonly ErrorDescriptor PeriodNotWeekly = new(
        "IMPORT.PERIOD_NOT_WEEKLY", ErrorCode.ValidationError,
        "Kỳ '{Period}' là kỳ tháng — chỉ nạp được theo tuần.");

    /// <summary>
    /// <c>period = "all"</c> trong khi <c>year</c> đang xem ≠ năm hiện tại (T15). <c>"all"</c> giải
    /// nghĩa thành TUẦN HIỆN TẠI, tức một kỳ không nằm trong năm người dùng đang nhìn.
    /// </summary>
    public static readonly ErrorDescriptor PeriodOutOfYear = new(
        "IMPORT.PERIOD_OUT_OF_YEAR", ErrorCode.ValidationError,
        "Kỳ 'all' chỉ dùng được khi đang xem năm hiện tại ({CurrentYear}), không phải năm {Year}.");

    /// <summary>
    /// Vượt trần SỐ DÒNG (Q75 — mặc định 20.000, cấu hình <c>Import:MaxRows</c> của Core).
    ///
    /// <para>⚠️ <b>Tiền tố <c>FILE_</c>, KHÔNG phải <c>ROW_</c></b>, dù tên có chữ "rows": tiền tố
    /// <c>ROW_</c> ở catalog này phân biệt lỗi MỘT DÒNG (job vẫn <c>Succeeded</c>, lỗi nằm trong
    /// <c>result.errors</c>) với lỗi CẢ FILE. Vượt trần số dòng là lỗi cả file nên nó đi cùng họ
    /// với <see cref="FileTooLarge"/>. Đặt nhầm tiền tố là trộn hai luồng xử lý khác nhau ở FE.</para>
    ///
    /// <para>Mã này ra dây qua <c>errorCode</c> của bước 2 (job <c>Failed</c>), KHÔNG qua
    /// <c>result.errors[]</c> — số dòng chỉ biết được khi đang đọc, tức sau khi job đã chạy.</para>
    /// </summary>
    public static readonly ErrorDescriptor FileTooManyRows = new(
        "IMPORT.FILE_TOO_MANY_ROWS", ErrorCode.ValidationError,
        "File vượt trần {MaxRows} dòng.");

    /// <summary>
    /// Thiếu cột bắt buộc (<c>Mã</c> hoặc <c>Nhóm</c>) trong dòng header — lỗi CẢ FILE, không phải
    /// lỗi dòng.
    ///
    /// <para><b>Vì sao là lỗi cả file:</b> thiếu <c>Mã</c> thì không định danh được dòng nào; thiếu
    /// <c>Nhóm</c> thì mọi dòng sinh đúng một lỗi giống hệt nhau, và một danh sách 62 lỗi trùng
    /// nhau không nói được điều gì mà một câu không nói được. Ca này gần như luôn là "tải nhầm
    /// file".</para>
    ///
    /// <para><c>Columns</c> là tên các cột ĐANG THIẾU, lấy từ chính hằng số tên cột — không phải
    /// giá trị người dùng nhập.</para>
    /// </summary>
    public static readonly ErrorDescriptor FileMissingColumn = new(
        "IMPORT.FILE_MISSING_COLUMN", ErrorCode.ValidationError,
        "File thiếu cột bắt buộc: {Columns}.");

    // ── Lỗi của endpoint POLL — bước 2: GET /api/import/{jobId} ─────────────────────────────

    /// <summary>
    /// <c>jobId</c> không có trong <c>ImportJobs</c>.
    ///
    /// <para><b>KHÔNG phân biệt "sai id" với "job đã bị dọn theo retention"</b> — cùng một mã. Trả
    /// hai mã khác nhau là tiết lộ <i>"id này từng tồn tại"</i> cho người gọi bất kỳ, mà thông tin
    /// đó không giúp gì: cả hai ca đều kết thúc bằng "nạp lại file".</para>
    ///
    /// <para><b>404 phải làm FE DỪNG poll.</b> Không có mã này thì FE không có tín hiệu dừng nào
    /// và sẽ poll vô hạn một job không bao giờ tồn tại — kiểu hỏng chỉ lộ ra ở tab để mở qua đêm.</para>
    /// </summary>
    public static readonly ErrorDescriptor JobNotFound = new(
        "IMPORT.JOB_NOT_FOUND", ErrorCode.NotFound,
        "Không tìm thấy lượt nạp file.");

    // ── Lỗi của MỘT DÒNG — job vẫn Succeeded, lỗi nằm trong result.errors[] ─────────────────

    /// <summary>
    /// Cột <c>Nhóm</c> không khớp <c>CriteriaGroup.Name</c> nào.
    ///
    /// <para><b>Vì sao "nhóm lạ" là lỗi còn "phụ trách lạ" thì không</b> (§6.3): nhóm là DANH MỤC
    /// ĐÓNG do BA quản (6 nhóm), sai nhóm nghĩa là sai chính tả hoặc thừa khoảng trắng — tự tạo
    /// nhóm thứ 7 làm mọi phép tổng hợp theo nhóm sai ngay và không ai thấy. Còn <c>Phụ trách</c>
    /// là THAM CHIẾU MỀM sang danh sách người dùng của Core: người phụ trách chưa có tài khoản là
    /// chuyện bình thường, chặn cả dòng vì lý do đó là chặn dữ liệu đúng.</para>
    /// </summary>
    public static readonly ErrorDescriptor RowGroupNotFound = new(
        "IMPORT.ROW_GROUP_NOT_FOUND", ErrorCode.ValidationError,
        "Nhóm '{GroupName}' của chỉ tiêu '{Code}' không có trong danh mục.");

    /// <summary>
    /// <c>Tự đánh giá</c> &gt; <c>Điểm tối đa</c>.
    ///
    /// <para><b>Phải là HAI mã rời với <see cref="RowVerifiedScoreExceedsMax"/>, không gộp:</b> câu
    /// người dùng đọc phải nói đúng ô nào trong file cần sửa. Một mã chung buộc FE dựng câu mơ hồ
    /// kiểu "một cột điểm vượt trần", và người dùng mở file ra không biết nhìn cột nào. Một dòng
    /// sai CẢ HAI cột thì báo HAI phần tử <c>errors[]</c> cùng <c>rowNumber</c>.</para>
    /// </summary>
    public static readonly ErrorDescriptor RowSelfScoreExceedsMax = new(
        "IMPORT.ROW_SELF_SCORE_EXCEEDS_MAX", ErrorCode.ValidationError,
        "Điểm tự đánh giá {SelfScore} của chỉ tiêu '{Code}' vượt điểm tối đa {MaxScore}.");

    /// <inheritdoc cref="RowSelfScoreExceedsMax"/>
    public static readonly ErrorDescriptor RowVerifiedScoreExceedsMax = new(
        "IMPORT.ROW_VERIFIED_SCORE_EXCEEDS_MAX", ErrorCode.ValidationError,
        "Điểm thẩm định {VerifiedScore} của chỉ tiêu '{Code}' vượt điểm tối đa {MaxScore}.");

    /// <summary>Cột <c>Trạng thái</c> ngoài 4 giá trị của §4.</summary>
    public static readonly ErrorDescriptor RowStatusInvalid = new(
        "IMPORT.ROW_STATUS_INVALID", ErrorCode.ValidationError,
        "Trạng thái '{Status}' của chỉ tiêu '{Code}' không hợp lệ.");

    /// <summary>
    /// Cột <c>Mã</c> rỗng. <b>CỐ Ý không có tham số <c>Code</c></b> — chính <c>Code</c> là thứ đang
    /// thiếu; <c>rowNumber</c> là toàn bộ thông tin định vị có được.
    /// </summary>
    public static readonly ErrorDescriptor RowCodeMissing = new(
        "IMPORT.ROW_CODE_MISSING", ErrorCode.ValidationError,
        "Dòng này thiếu mã chỉ tiêu.");

    /// <summary>
    /// Cùng một mã xuất hiện hai lần trong CÙNG file. Báo ở dòng THỨ HAI, và
    /// <c>FirstRowNumber</c> trỏ về dòng đầu tiên mang mã đó — không có tham số này thì người dùng
    /// phải tự dò cả file để tìm cái còn lại.
    /// </summary>
    public static readonly ErrorDescriptor RowCodeDuplicatedInFile = new(
        "IMPORT.ROW_CODE_DUPLICATED_IN_FILE", ErrorCode.ValidationError,
        "Mã chỉ tiêu '{Code}' đã xuất hiện ở dòng {FirstRowNumber}.");

    /// <summary>Một đoạn của <c>Mã</c> vượt trần chữ số (Q58, §2).</summary>
    public static readonly ErrorDescriptor RowCodeSegmentTooLong = new(
        "IMPORT.ROW_CODE_SEGMENT_TOO_LONG", ErrorCode.ValidationError,
        "Mã chỉ tiêu '{Code}' có đoạn dài quá {MaxSegmentDigits} chữ số.");

    /// <summary>
    /// <c>Mã</c> sai định dạng (Q65): có chữ cái, đoạn rỗng kiểu <c>4..2</c>, hoặc dấu chấm ở đầu/cuối.
    /// </summary>
    public static readonly ErrorDescriptor RowCodeFormatInvalid = new(
        "IMPORT.ROW_CODE_FORMAT_INVALID", ErrorCode.ValidationError,
        "Mã chỉ tiêu '{Code}' sai định dạng.");

    /// <summary><c>Mã</c> vượt trần độ dài tổng (Q65).</summary>
    public static readonly ErrorDescriptor RowCodeTooLong = new(
        "IMPORT.ROW_CODE_TOO_LONG", ErrorCode.ValidationError,
        "Mã chỉ tiêu '{Code}' dài quá {MaxLength} ký tự.");

    // ── Năm mã THÊM ở lượt thi công DM-7 (2026-09-11) — CHỜ NGƯỜI DÙNG DUYỆT TÊN ────────────
    //
    // 🛑 doc/contracts/danh-muc-dti.md §"errors[].code" liệt 9 tình huống lỗi dòng, và KHÔNG tình
    // huống nào phủ 5 ca dưới đây. Chúng vẫn phải có mã, vì cả ba lối xử lý còn lại đều tệ hơn:
    //
    //   (a) Bỏ qua, coi ô hỏng như ô trống  -> mất dữ liệu IM LẶNG, đúng lớp lỗi đắt nhất mà
    //       15-import-export.md §2b dựng cả một mục để chặn.
    //   (b) Để DomainException bay lên       -> Criteria.Create ném, job thành Failed, và theo Q64
    //       thì CẢ FILE không dòng nào được ghi. Một ô trống làm hỏng 61 dòng đúng.
    //   (c) Điền giá trị mặc định            -> bịa số liệu mà không ai ký tên (đúng thứ Q24 cấm).
    //
    // Tên đặt theo đúng khuôn của 9 mã trên (tiền tố ROW_, tham số đặt TÊN). Người dùng đổi tên
    // thì đổi ở ĐÂY và ở bảng §"errors[].code" của card — không có chỗ thứ ba nào giữ chuỗi này.

    /// <summary>
    /// Mã chưa có trong hệ thống (⇒ phải TẠO <c>Criteria</c> mới) nhưng cột <c>Chỉ tiêu</c> rỗng.
    /// §6.2 khai cột này là bắt buộc "(khi tạo mới)"; mã đã có sẵn thì import KHÔNG đổi
    /// <c>Name</c> nên cột rỗng là hợp lệ.
    /// </summary>
    public static readonly ErrorDescriptor RowNameMissing = new(
        "IMPORT.ROW_NAME_MISSING", ErrorCode.ValidationError,
        "Chỉ tiêu '{Code}' chưa có trong hệ thống nên cột 'Chỉ tiêu' không được để trống.");

    /// <summary>
    /// Như <see cref="RowNameMissing"/>, cho cột <c>Điểm tối đa</c>: rỗng, không phải số, hoặc
    /// <c>&lt;= 0</c>.
    /// </summary>
    public static readonly ErrorDescriptor RowMaxScoreInvalid = new(
        "IMPORT.ROW_MAX_SCORE_INVALID", ErrorCode.ValidationError,
        "Điểm tối đa '{MaxScore}' của chỉ tiêu '{Code}' không hợp lệ.");

    /// <summary>
    /// Ô <c>Tự đánh giá</c> có nội dung nhưng không đọc ra số. Tách khỏi
    /// <see cref="RowSelfScoreExceedsMax"/> vì hai ca có hai cách sửa khác nhau — một bên sửa CON
    /// SỐ, một bên sửa KIỂU Ô. Hai mã rời cho hai cột điểm, cùng lý do với cặp
    /// <c>*_EXCEEDS_MAX</c>.
    /// </summary>
    public static readonly ErrorDescriptor RowSelfScoreInvalid = new(
        "IMPORT.ROW_SELF_SCORE_INVALID", ErrorCode.ValidationError,
        "Điểm tự đánh giá '{SelfScore}' của chỉ tiêu '{Code}' không phải một số.");

    /// <inheritdoc cref="RowSelfScoreInvalid"/>
    public static readonly ErrorDescriptor RowVerifiedScoreInvalid = new(
        "IMPORT.ROW_VERIFIED_SCORE_INVALID", ErrorCode.ValidationError,
        "Điểm thẩm định '{VerifiedScore}' của chỉ tiêu '{Code}' không phải một số.");

    /// <summary>
    /// Ô <c>Hạn xử lý</c> có nội dung nhưng không đọc ra ngày. Ô Excel mang KIỂU NGÀY đi qua seam
    /// nguyên kiểu (<c>ImportCellValue.AsDate</c>); mã này dành cho ô CHỮ ghi sai khuôn, ca duy
    /// nhất còn lại sau khi seam đã giữ kiểu thật.
    /// </summary>
    public static readonly ErrorDescriptor RowDeadlineInvalid = new(
        "IMPORT.ROW_DEADLINE_INVALID", ErrorCode.ValidationError,
        "Hạn xử lý '{Deadline}' của chỉ tiêu '{Code}' không phải một ngày hợp lệ.");
}

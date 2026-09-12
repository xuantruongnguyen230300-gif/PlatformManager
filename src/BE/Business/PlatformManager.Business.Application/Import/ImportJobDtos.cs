namespace PlatformManager.Business.Application.Import;

/// <summary>
/// Payload của <c>POST /api/import</c> — DM-7 bước 1.
///
/// <para><b>Object bọc, KHÔNG trả <c>Guid</c> trần</b> (hợp đồng): thêm một trường sau này (vd
/// <c>estimatedRows</c>) không phá shape mà FE đang bind.</para>
/// </summary>
public sealed record StartImportResultDto(Guid JobId);

/// <summary>
/// Một lỗi của MỘT DÒNG — DM-7 bước 2.
///
/// <para>⚠️ <b>KHÔNG có trường <c>message</c>.</b> BE trả MÃ + THAM SỐ CÓ CẤU TRÚC; FE sở hữu câu
/// chữ (doc/huong_dan/wiki-core/be/16-i18n-va-ma-loi.md §3). Bản hợp đồng trước từng cho
/// <c>errors[]</c> mang một câu tiếng Việt BE dựng sẵn, và §3 nêu đúng ca này làm ví dụ: BE ghép
/// sẵn <i>"Mã chỉ tiêu 'ABC' đã tồn tại."</i> thì FE KHÔNG tách lại được <c>ABC</c> ra khỏi câu.</para>
/// </summary>
/// <param name="RowNumber">
/// Số dòng trong FILE NGƯỜI DÙNG GỬI, dòng 1 = header — để câu lỗi trỏ đúng chỗ họ mở file ra sửa.
/// Seam đọc file không mang số dòng và cam kết KHÔNG bỏ dòng nào (kể cả dòng rỗng), nên dòng dữ
/// liệu thứ <c>n</c> là dòng <c>n + 1</c> của file.
/// </param>
/// <param name="Code"><c>businessCode</c> của một descriptor trong <see cref="ImportErrors"/>.</param>
/// <param name="MessageParams">
/// Tham số RỜI, khoá là TÊN (<c>{MaxScore}</c>), không phải số thứ tự. Vắng mặt khi mã không có
/// tham số nào.
///
/// <para>⚠️ Allowlist áp y như ở envelope: chỉ đưa ra tham số mô tả CHÍNH SÁCH/VỊ TRÍ và những giá
/// trị ĐÃ CÓ SẴN trong chính file người dùng gửi lên (mã, tên nhóm, điểm, trạng thái) — không gì
/// khác.</para>
/// </param>
public sealed record ImportRowErrorDto(
    int RowNumber,
    string Code,
    IReadOnlyDictionary<string, string>? MessageParams);

/// <summary>
/// Kết quả một lượt nạp đã chạy xong — chỉ có mặt khi <c>status = "Succeeded"</c>.
/// </summary>
/// <param name="TotalRows">Số dòng DỮ LIỆU đã đọc (không kể header).</param>
/// <param name="SuccessCount">Số dòng đã ghi được.</param>
/// <param name="ErrorCount">
/// Số DÒNG có lỗi — không phải số phần tử <see cref="Errors"/>: một dòng sai cả hai cột điểm cho ra
/// hai phần tử <c>errors[]</c> cùng <c>rowNumber</c> nhưng vẫn là một dòng hỏng.
/// </param>
/// <param name="CriteriaCreatedCount">Số chỉ tiêu MỚI được tạo (mã chưa có trong hệ thống).</param>
public sealed record ImportResultDto(
    int TotalRows,
    int SuccessCount,
    int ErrorCount,
    int CriteriaCreatedCount,
    IReadOnlyList<ImportRowErrorDto> Errors);

/// <summary>
/// Payload của <c>GET /api/import/{jobId}</c> — DM-7 bước 2.
/// </summary>
/// <param name="Status">
/// <c>"Pending"</c> · <c>"Running"</c> · <c>"Succeeded"</c> · <c>"Failed"</c>.
/// </param>
/// <param name="Result">
/// Chỉ có khi <c>Succeeded</c>. <b>Dòng lỗi KHÔNG làm job <c>Failed</c></b> — chúng nằm trong
/// <see cref="ImportResultDto.Errors"/> và job vẫn <c>Succeeded</c>.
/// </param>
/// <param name="ErrorCode">
/// <c>businessCode</c> của lỗi CẢ FILE khi lỗi đó có mã nghiệp vụ (vd
/// <c>IMPORT.FILE_TOO_MANY_ROWS</c>) — thêm 2026-09-11 cùng Q75. FE tra bảng dịch như mọi
/// <c>businessCode</c> khác.
///
/// <para>Vắng mặt khi <c>Status</c> ≠ <c>Failed</c>, hoặc khi lỗi là hạ tầng thuần (job crash, file
/// hỏng ở mức byte) — chúng không có mã để dịch. FE khi đó lùi về câu chung và ghi log
/// <see cref="ErrorMessage"/>.</para>
/// </param>
/// <param name="ErrorMessage">
/// Chỉ có khi <c>Failed</c>. Giữ đúng vai <b>dev-facing + fallback</b> như <c>message</c> của
/// envelope gốc: FE ghi log hoặc hiện cho quản trị, KHÔNG dùng làm câu cho người dùng cuối.
/// </param>
public sealed record ImportJobStatusDto(
    string Status,
    ImportResultDto? Result,
    string? ErrorCode,
    string? ErrorMessage);

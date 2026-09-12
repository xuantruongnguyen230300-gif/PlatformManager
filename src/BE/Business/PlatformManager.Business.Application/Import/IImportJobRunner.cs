namespace PlatformManager.Business.Application.Import;

/// <summary>
/// Việc chạy NỀN của một lượt nạp file — DM-7.
///
/// <para><b>Vì sao là một interface riêng chứ không phải một handler MediatR:</b> đây là thứ được
/// đẩy qua <c>IBackgroundJobScheduler.EnqueueAsync&lt;TJob&gt;</c>, và seam đó nhận một BIỂU THỨC
/// gọi method trên <c>TJob</c> — worker tự phân giải <c>TJob</c> qua DI rồi gọi lại. Tham số vì thế
/// phải tuần tự hoá được: <c>Guid</c>, không phải <c>Stream</c>/<c>IFormFile</c>/entity.</para>
///
/// <para><b>Worker KHÔNG có <c>HttpContext</c></b>. Danh tính người bấm nút vẫn đi theo job nhờ
/// filter Hangfire toàn cục ở host (Q35, spec/danh-muc-dti/business-rules.md §5.6), nên
/// <c>AuditInterceptor</c> ghi <c>UpdatedBy</c> là TÀI KHOẢN ĐÃ NẠP FILE chứ không phải
/// <c>"system"</c>. Runner không phải làm gì để hưởng điều đó — và cũng không được tự gán
/// <c>UpdatedBy</c>, vì interceptor ghi đè vô điều kiện.</para>
/// </summary>
public interface IImportJobRunner
{
    /// <summary>
    /// Chạy trọn một lượt nạp: đọc file từ kho, áp luật từng dòng, ghi dữ liệu, cập nhật trạng thái
    /// job.
    ///
    /// <para><b>KHÔNG ném ra ngoài.</b> Mọi lỗi đều được ghi vào chính bản ghi job
    /// (<c>Status = Failed</c> + <c>ErrorMessage</c>) — đó là kênh DUY NHẤT mà FE đọc được. Ném
    /// thêm chỉ làm Hangfire thử lại một file hỏng thêm chín lần nữa.</para>
    /// </summary>
    Task RunAsync(Guid jobId, CancellationToken ct);
}

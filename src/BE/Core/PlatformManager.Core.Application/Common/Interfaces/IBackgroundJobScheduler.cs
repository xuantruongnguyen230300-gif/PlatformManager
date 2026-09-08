using System.Linq.Expressions;

namespace PlatformManager.Core.Application.Common.Interfaces;

/// <summary>
/// SEAM enqueue job nền — tầng Application chỉ biết "đẩy việc này ra chạy nền", KHÔNG biết
/// chạy bằng gì (Hangfire hôm nay, có thể là hàng đợi khác sau này). Hiện thực nằm ở tầng
/// Infrastructure (HangfireBackgroundJobScheduler), đăng ký DI ở Program.cs.
///
/// Vì sao có seam này thay vì gọi thẳng <c>BackgroundJob.Enqueue</c>: gọi thẳng nghĩa là
/// *.Application phải reference Hangfire.Core, tức tầng Application phụ thuộc hạ tầng cụ thể —
/// vi phạm quy tắc phụ thuộc của Clean Architecture. Cùng khuôn với 2 seam đã có
/// (INotificationSender cho email; nơi lưu file khi có đường import trở lại). Xem
/// doc/huong_dan/quy-uoc/be-cqrs-handler.md §"Command chạy lâu → job nền".
///
/// Luật cấm này được ArchTest cưỡng chế: LayerDependencyTests liệt "Hangfire" vào danh sách
/// assembly cấm ở cả Core.Application lẫn Modules.*.Application.
/// </summary>
public interface IBackgroundJobScheduler
{
    /// <summary>
    /// Đẩy 1 lời gọi method lên hàng đợi nền, chạy NGAY khi có worker rảnh. <typeparamref name="TJob"/>
    /// là interface của job runner — worker tự resolve nó qua DI rồi gọi lại đúng biểu thức
    /// truyền vào, nên tham số phải serialize được (Guid/string/số...), KHÔNG truyền
    /// Stream/IFormFile/entity — nơi lưu file phải là một seam riêng, xem 14-file-storage.md.
    /// </summary>
    /// <param name="methodCall">Ví dụ: <c>runner =&gt; runner.RunAsync(jobId, CancellationToken.None)</c>.</param>
    /// <param name="ct">Huỷ thao tác *enqueue* (không phải huỷ job đã vào hàng đợi).</param>
    Task EnqueueAsync<TJob>(Expression<Func<TJob, Task>> methodCall, CancellationToken ct = default);
}

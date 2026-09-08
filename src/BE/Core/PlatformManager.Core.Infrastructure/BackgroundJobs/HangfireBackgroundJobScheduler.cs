using System.Linq.Expressions;
using Hangfire;
using PlatformManager.Core.Application.Common.Interfaces;

namespace PlatformManager.Core.Infrastructure.BackgroundJobs;

/// <summary>
/// Hiện thực Hangfire của seam <see cref="IBackgroundJobScheduler"/> — đây là NƠI DUY NHẤT
/// ngoài composition root (Program.cs) được phép biết tới Hangfire.
///
/// Dùng <see cref="IBackgroundJobClient"/> (đăng ký bởi AddHangfire()) thay cho facade tĩnh
/// <c>BackgroundJob</c>: client đi qua DI nên thay thế/giả lập được, còn facade tĩnh đọc
/// <c>JobStorage.Current</c> - trạng thái toàn cục.
/// </summary>
public sealed class HangfireBackgroundJobScheduler(IBackgroundJobClient client) : IBackgroundJobScheduler
{
    public Task EnqueueAsync<TJob>(Expression<Func<TJob, Task>> methodCall, CancellationToken ct = default)
    {
        // CỐ Ý không ThrowIfCancellationRequested: caller enqueue SAU khi đã commit DB bản ghi
        // job (ca điển hình: handler khởi tạo một job nền dài). Client huỷ request lúc này mà bỏ enqueue sẽ để
        // lại 1 bản ghi Pending vĩnh viễn không ai chạy — tệ hơn là cứ chạy tiếp. `ct` giữ trong
        // chữ ký để hiện thực sau này (hàng đợi qua mạng) có chỗ dùng thật.
        _ = ct;

        // Enqueue của Hangfire là đồng bộ (ghi thẳng vào storage) — không có overload async,
        // nên trả Task.CompletedTask thay vì bọc Task.Run.
        client.Enqueue(methodCall);
        return Task.CompletedTask;
    }
}

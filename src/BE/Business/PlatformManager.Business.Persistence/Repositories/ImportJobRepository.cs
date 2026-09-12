using Microsoft.EntityFrameworkCore;
using PlatformManager.Business.Application.Import;
using PlatformManager.Business.Domain.Entities;
using PlatformManager.Core.Infrastructure.Persistence;

namespace PlatformManager.Business.Persistence.Repositories;

/// <summary>
/// Bảng theo dõi lượt nạp (<c>business."ImportJobs"</c>) — DM-7.
///
/// <para><b>Hai phép đọc, hai chế độ theo dõi khác nhau, và đó là chủ đích:</b> đường JOB cần
/// entity THEO DÕI ĐƯỢC để đổi trạng thái rồi lưu, còn đường POLL chỉ chiếu ba cột ra một record —
/// <c>AsNoTracking</c> ở đó là bắt buộc theo doc/huong_dan/quy-uoc/be-performance.md, và nó cũng
/// tránh việc một vòng poll mỗi 2 giây nhồi entity vào change tracker của request.</para>
/// </summary>
internal sealed class ImportJobRepository(PlatformManagerDbContext db) : IImportJobRepository
{
    public void Add(ImportJob job) => db.Set<ImportJob>().Add(job);

    public async Task<ImportJob?> GetTrackedAsync(Guid jobId, CancellationToken ct) =>
        await db.Set<ImportJob>().FirstOrDefaultAsync(job => job.Id == jobId, ct);

    public async Task<ImportJobStatusSnapshot?> GetStatusAsync(Guid jobId, CancellationToken ct) =>
        await db.Set<ImportJob>().AsNoTracking()
            .Where(job => job.Id == jobId)
            .Select(job => new ImportJobStatusSnapshot(job.Status, job.ResultJson, job.ErrorMessage, job.ErrorCode))
            .FirstOrDefaultAsync(ct);
}

using Microsoft.EntityFrameworkCore;
using PlatformManager.Core.Infrastructure.Persistence;
using PlatformManager.Modules.DtiWeekly.Application.Import;
using PlatformManager.Modules.DtiWeekly.Domain.Entities;

namespace PlatformManager.Modules.DtiWeekly.Infrastructure.Persistence.Repositories;

public sealed class ImportJobRepository(PlatformManagerDbContext db) : IImportJobRepository
{
    public Task<ImportJob?> GetByIdAsync(Guid id, CancellationToken ct)
        => db.Set<ImportJob>().FirstOrDefaultAsync(j => j.Id == id, ct);

    public async Task AddAsync(ImportJob job, CancellationToken ct)
        => await db.Set<ImportJob>().AddAsync(job, ct);
}

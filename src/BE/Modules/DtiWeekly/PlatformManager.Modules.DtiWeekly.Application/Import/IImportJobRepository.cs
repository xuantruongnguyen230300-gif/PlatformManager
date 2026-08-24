using PlatformManager.Modules.DtiWeekly.Domain.Entities;

namespace PlatformManager.Modules.DtiWeekly.Application.Import;

public interface IImportJobRepository
{
    Task<ImportJob?> GetByIdAsync(Guid id, CancellationToken ct);

    Task AddAsync(ImportJob job, CancellationToken ct);
}

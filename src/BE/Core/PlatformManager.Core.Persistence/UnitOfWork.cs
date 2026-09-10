using PlatformManager.Core.Application.Common.Interfaces;

namespace PlatformManager.Core.Infrastructure.Persistence;

public sealed class UnitOfWork(PlatformManagerDbContext db) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);

    public void DiscardTrackedChanges() => db.ChangeTracker.Clear();
}

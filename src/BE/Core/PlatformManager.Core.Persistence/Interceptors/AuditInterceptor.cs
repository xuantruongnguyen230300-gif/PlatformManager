using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PlatformManager.Core.Application.Common.Interfaces;
using PlatformManager.Core.Domain.Common;

namespace PlatformManager.Core.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Ghi CreatedBy/UpdatedBy/CreatedAt/UpdatedAt cho MỌI BaseEntity đang được
/// SaveChanges — setter public chính là để interceptor này ghi được mà không cần
/// reflection (xem doc/huong_dan/quy-uoc/be-entity-domain.md §Base entity). Chạy trong
/// SavingChanges/SavingChangesAsync — TRƯỚC khi lệnh SQL thật sự được gửi đi.
/// </summary>
public sealed class AuditInterceptor(ICurrentUser currentUser, IDateTimeProvider clock) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        UpdateAuditFields(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        UpdateAuditFields(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateAuditFields(DbContext? context)
    {
        if (context is null)
            return;

        var userName = currentUser.IsAuthenticated ? currentUser.UserName : null;
        var now = clock.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy = userName ?? "system";
                    // Set luôn UpdatedAt/UpdatedBy = CreatedAt/CreatedBy lúc tạo mới — không
                    // để 2 cột này null cho tới lần Modified đầu tiên, tránh FE phải tự viết
                    // `UpdatedAt ?? CreatedAt` ở mọi nơi hiển thị "lần sửa cuối".
                    // Luật + lý do đầy đủ: doc/huong_dan/quy-uoc/be-entity-domain.md §Base entity.
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = userName ?? "system";
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = userName ?? "system";
                    break;
            }
        }
    }
}

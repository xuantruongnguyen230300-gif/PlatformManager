using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PlatformManager.Core.Application.Common.Interfaces;
using PlatformManager.Core.Domain.Common;

namespace PlatformManager.Core.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Ghi UserCreate/UserUpdate/DateCreate/DateUpdate cho MỌI BaseEntity đang được
/// SaveChanges — setter public chính là để interceptor này ghi được mà không cần
/// reflection (xem .claude/rules/entity-domain.md §Base entity). Chạy trong
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
                    entry.Entity.DateCreate = now;
                    entry.Entity.UserCreate = userName ?? "system";
                    // Set luôn DateUpdate/UserUpdate = DateCreate/UserCreate lúc tạo mới — không
                    // để 2 cột này null cho tới lần Modified đầu tiên, tránh FE phải tự viết
                    // `DateUpdate ?? DateCreate` ở mọi nơi hiển thị "lần sửa cuối" (xem
                    // wiki-core/be/trien-khai/04-p3-platform-persistence.md §7.2).
                    entry.Entity.DateUpdate = now;
                    entry.Entity.UserUpdate = userName ?? "system";
                    break;
                case EntityState.Modified:
                    entry.Entity.DateUpdate = now;
                    entry.Entity.UserUpdate = userName ?? "system";
                    break;
            }
        }
    }
}

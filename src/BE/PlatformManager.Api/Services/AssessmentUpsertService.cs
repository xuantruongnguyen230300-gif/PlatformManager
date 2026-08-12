using Microsoft.EntityFrameworkCore;
using PlatformManager.Api.Data;
using PlatformManager.Api.Entities;

namespace PlatformManager.Api.Services;

/// <summary>
/// Cài đặt "Quy tắc ghi" duy nhất cho <see cref="CriteriaAssessment"/>, dùng
/// chung cho luồng nhập tay (sửa 1 ô Tiến độ %/Ghi chú) và luồng Import CSV —
/// xem doc/ERD/ERD.md mục "Kỳ (tuần/tháng/năm) — khái niệm ngầm định":
///
/// 1. Tìm record CHƯA xoá mềm của đúng CriteriaId có CreatedAt = hôm nay (UTC).
/// 2. Có rồi -> UPDATE: chỉ áp field mà `apply` thực sự thay đổi, CreatedAt
///    giữ nguyên, chỉ UpdatedAt đổi.
/// 3. Chưa có -> copy-forward toàn bộ 7 field nghiệp vụ từ record CreatedAt
///    lớn nhất trước đó (nếu có), áp `apply` đè lên, INSERT với
///    CreatedAt = UpdatedAt = now. Nếu chưa từng có record nào trước đó và
///    `apply` có set SelfScore -> seed ProgressPercent = SelfScore/MaxScore*100
///    (kẹp [0,100]) đúng công thức seed cũ, TRỪ KHI `apply` đã tự set
///    ProgressPercent (khi đó giữ nguyên giá trị `apply` đặt — `apply` luôn
///    thắng trên field nó chủ động set).
/// </summary>
public class AssessmentUpsertService(AppDbContext db)
{
    public async Task<CriteriaAssessment> UpsertTodayAsync(
        Guid criteriaId,
        Action<CriteriaAssessment> apply,
        CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var todayStart = new DateTimeOffset(now.UtcDateTime.Date, TimeSpan.Zero);
        var todayEnd = todayStart.AddDays(1);

        var existing = await db.CriteriaAssessments
            .Where(a => a.CriteriaId == criteriaId && !a.IsDeleted)
            .Where(a => a.CreatedAt >= todayStart && a.CreatedAt < todayEnd)
            .FirstOrDefaultAsync(ct);

        if (existing != null)
        {
            apply(existing);
            existing.UpdatedAt = now;
            await db.SaveChangesAsync(ct);
            return existing;
        }

        var baseline = await db.CriteriaAssessments
            .Where(a => a.CriteriaId == criteriaId && !a.IsDeleted && a.CreatedAt < todayStart)
            .OrderByDescending(a => a.CreatedAt)
            .FirstOrDefaultAsync(ct);

        var entity = new CriteriaAssessment
        {
            Id = Guid.NewGuid(),
            CriteriaId = criteriaId,
            CreatedAt = now,
            UpdatedAt = now,
        };

        var hadBaseline = baseline != null;
        if (baseline != null)
        {
            entity.ProgressPercent = baseline.ProgressPercent;
            entity.SelfScore = baseline.SelfScore;
            entity.VerifiedScore = baseline.VerifiedScore;
            entity.Status = baseline.Status;
            entity.OwnerId = baseline.OwnerId;
            entity.Deadline = baseline.Deadline;
            entity.Note = baseline.Note;
        }

        var progressBeforeApply = entity.ProgressPercent;
        apply(entity);

        // Chưa từng có record nào trước đó + operation này cung cấp SelfScore
        // + operation không tự set ProgressPercent (progress vẫn = giá trị mặc định 0
        // từ trước khi apply) -> seed theo công thức cũ.
        if (!hadBaseline && entity.SelfScore.HasValue && entity.ProgressPercent == progressBeforeApply)
        {
            var criteria = await db.Criteria
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.Id == criteriaId, ct);
            if (criteria is { MaxScore: > 0 })
            {
                var seeded = entity.SelfScore.Value / criteria.MaxScore * 100m;
                entity.ProgressPercent = Math.Clamp(seeded, 0, 100);
            }
        }

        db.CriteriaAssessments.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }
}

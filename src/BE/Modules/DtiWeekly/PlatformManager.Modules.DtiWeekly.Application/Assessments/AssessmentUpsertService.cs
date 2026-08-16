using PlatformManager.Core.Application.Common.Interfaces;
using PlatformManager.Modules.DtiWeekly.Application.Criteria;
using PlatformManager.Core.Domain.Common;
using PlatformManager.Modules.DtiWeekly.Domain.Entities;

namespace PlatformManager.Modules.DtiWeekly.Application.Assessments;

/// <inheritdoc cref="IAssessmentUpsertService"/>
public sealed class AssessmentUpsertService(
    ICriteriaAssessmentRepository assessmentRepo, ICriteriaRepository criteriaRepo, IDateTimeProvider clock)
    : IAssessmentUpsertService
{
    public async Task<CriteriaAssessment> UpsertTodayAsync(
        Guid criteriaId, Action<CriteriaAssessment> apply, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(apply);

        var today = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);

        var existingToday = await assessmentRepo.FindByCriteriaAndDateAsync(criteriaId, today, ct);
        if (existingToday is not null)
        {
            apply(existingToday);
            return existingToday;
        }

        var baseline = await assessmentRepo.FindLatestBeforeAsync(criteriaId, today, ct);
        var record = CriteriaAssessment.Create(criteriaId);

        if (baseline is not null)
            record.CopyBaselineFrom(baseline);

        var selfScoreBefore = record.SelfScore;
        var progressBefore = record.ProgressPercent;

        apply(record);

        // Case đặc biệt: chưa từng có baseline, apply có set SelfScore, apply KHÔNG tự set
        // ProgressPercent (dùng progressBefore không đổi làm tín hiệu "chưa bị chạm tới").
        if (baseline is null && record.SelfScore is not null && selfScoreBefore is null
            && record.ProgressPercent == progressBefore)
        {
            var criteria = await criteriaRepo.GetByIdAsync(criteriaId, ct)
                ?? throw new DomainException("CRITERIA_ASSESSMENT_CRITERIA_MISSING", "Không tìm thấy chỉ tiêu để suy ra tiến độ.");

            var derived = Math.Clamp(record.SelfScore.Value / criteria.MaxScore * 100m, 0m, 100m);
            record.UpdateProgress(derived);
        }

        await assessmentRepo.AddAsync(record, ct);
        return record;
    }
}

using PlatformManager.Modules.DtiWeekly.Application.Assessments;
using PlatformManager.Modules.DtiWeekly.Application.Common;
using PlatformManager.Modules.DtiWeekly.Application.Criteria;

namespace PlatformManager.Modules.DtiWeekly.Application.Dashboard;

public sealed record WeeklyProgressPoint(DateOnly WeekStart, decimal Progress);

/// <summary>KHÔNG carry-forward: chỉ tiêu không có record trong tuần bị loại khỏi CẢ tử số
/// lẫn mẫu số. null = "chưa có dữ liệu" (không phải 0%) — xem
/// spec/dashboard-dti-weekly/business-rules.md mục 3.3.</summary>
public sealed record PeriodAggregateResult(
    decimal? OverallProgress,
    IReadOnlyDictionary<Guid, decimal?> ProgressByGroup,
    IReadOnlyDictionary<Guid, decimal?> ProgressByCriteria,
    IReadOnlyList<WeeklyProgressPoint> WeeklyBreakdown);

/// <summary>
/// Hàm THUẦN (không I/O) dùng chung cho week/month/year/"Tất cả" — tháng/năm = trung bình
/// cộng các tuần-ISO có dữ liệu trong khoảng; khi khoảng = đúng 1 tuần, tự suy biến đúng
/// công thức tuần đơn (trung bình của 1 phần tử = chính nó). Xem doc/ERD/ERD.md mục "Kỳ
/// (tuần/tháng/năm)" và spec/dashboard-dti-weekly/business-rules.md mục 3.3/3.4.
/// </summary>
public static class PeriodAggregateCalculator
{
    public static PeriodAggregateResult Compute(
        DateOnly periodStart,
        DateOnly periodEndExclusive,
        IReadOnlyList<CriteriaGridRowDto> itemsInRange,
        IReadOnlyList<CriteriaSummaryDto> activeCriteria)
    {
        var activeById = activeCriteria.ToDictionary(c => c.Id);
        var weekStarts = PeriodRangeCalculator.EnumerateIsoWeekStarts(periodStart, periodEndExclusive);

        var weeklyReps = new List<(DateOnly WeekStart, List<CriteriaGridRowDto> Reps)>();
        foreach (var weekStart in weekStarts)
        {
            var weekEndExclusive = weekStart.AddDays(7);
            var lo = weekStart > periodStart ? weekStart : periodStart;
            var hi = weekEndExclusive < periodEndExclusive ? weekEndExclusive : periodEndExclusive;

            var weekItems = itemsInRange
                .Where(x => x.AssessmentDate is { } d && d >= lo && d < hi && activeById.ContainsKey(x.CriteriaId))
                .ToList();

            if (weekItems.Count == 0)
                continue; // tuần không có dữ liệu -> loại hẳn khỏi phép tính, KHÔNG carry-forward

            var reps = weekItems
                .GroupBy(x => x.CriteriaId)
                .Select(g => g.OrderByDescending(x => x.AssessmentDate!.Value).First())
                .ToList();

            weeklyReps.Add((weekStart, reps));
        }

        if (weeklyReps.Count == 0)
        {
            return new PeriodAggregateResult(null, new Dictionary<Guid, decimal?>(), new Dictionary<Guid, decimal?>(), []);
        }

        var weeklyBreakdown = weeklyReps
            .Select(w => new WeeklyProgressPoint(w.WeekStart, WeightedProgress(w.Reps, activeCriteria)))
            .OrderBy(w => w.WeekStart)
            .ToList();

        var overallProgress = Math.Round(weeklyBreakdown.Average(w => w.Progress), 2);

        var progressByGroup = new Dictionary<Guid, decimal?>();
        foreach (var groupId in activeCriteria.Select(c => c.GroupId).Distinct())
        {
            var groupPool = activeCriteria.Where(c => c.GroupId == groupId).ToList();
            var weeklyGroupValues = new List<decimal>();
            foreach (var (_, reps) in weeklyReps)
            {
                var groupReps = reps.Where(r => activeById[r.CriteriaId].GroupId == groupId).ToList();
                if (groupReps.Count == 0)
                    continue;

                weeklyGroupValues.Add(WeightedProgress(groupReps, groupPool));
            }

            progressByGroup[groupId] = weeklyGroupValues.Count > 0 ? Math.Round(weeklyGroupValues.Average(), 2) : null;
        }

        var progressByCriteria = new Dictionary<Guid, decimal?>();
        foreach (var criteria in activeCriteria)
        {
            var values = weeklyReps
                .SelectMany(w => w.Reps)
                .Where(r => r.CriteriaId == criteria.Id)
                .Select(r => r.ProgressPercent ?? 0m)
                .ToList();

            progressByCriteria[criteria.Id] = values.Count > 0 ? Math.Round(values.Average(), 2) : null;
        }

        return new PeriodAggregateResult(overallProgress, progressByGroup, progressByCriteria, weeklyBreakdown);
    }

    /// <summary>Σ(MaxScore × Progress/100) / Σ(MaxScore) — chỉ trên tập criteria CÓ record
    /// (reps), theo đúng công thức weightedProgress() gốc, khoanh vùng bởi "loại chỉ tiêu
    /// thiếu dữ liệu khỏi mẫu số" đã chốt.</summary>
    private static decimal WeightedProgress(IReadOnlyList<CriteriaGridRowDto> reps, IReadOnlyList<CriteriaSummaryDto> pool)
    {
        var maxScoreById = pool.ToDictionary(c => c.Id, c => c.MaxScore);
        decimal numerator = 0, denominator = 0;

        foreach (var r in reps)
        {
            if (!maxScoreById.TryGetValue(r.CriteriaId, out var maxScore))
                continue;

            numerator += maxScore * (r.ProgressPercent ?? 0m) / 100m;
            denominator += maxScore;
        }

        return denominator > 0 ? Math.Round(numerator / denominator * 100m, 2) : 0m;
    }
}

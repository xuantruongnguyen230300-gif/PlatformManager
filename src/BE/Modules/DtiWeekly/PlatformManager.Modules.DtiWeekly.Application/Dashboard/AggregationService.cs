using System.Text;
using PlatformManager.Modules.DtiWeekly.Application.Assessments;
using PlatformManager.Modules.DtiWeekly.Application.Common;
using PlatformManager.Core.Application.Common.Interfaces;
using PlatformManager.Modules.DtiWeekly.Application.Criteria;
using PlatformManager.Modules.DtiWeekly.Application.CriteriaGroups;

namespace PlatformManager.Modules.DtiWeekly.Application.Dashboard;

/// <inheritdoc cref="IAggregationService"/>
public sealed class AggregationService(
    ICriteriaAssessmentQueryRepository assessmentQueryRepo,
    ICriteriaRepository criteriaRepo,
    ICriteriaGroupRepository groupRepo,
    IDateTimeProvider clock) : IAggregationService
{
    private const decimal Epsilon = 0.001m;
    private const decimal DoneThreshold = 99.999m;

    public async Task<DashboardDto> GetDashboardAsync(int? year, string period, string? periodValue, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
        var resolvedYear = year ?? today.Year;
        var (periodStart, periodEndExclusive) = PeriodRangeCalculator.Resolve(resolvedYear, period, periodValue);

        var activeCriteriaEntities = await criteriaRepo.GetAllActiveAsync(ct);
        var activeCriteria = activeCriteriaEntities
            .Select(c => new CriteriaSummaryDto(c.Id, c.GroupId, c.MaxScore))
            .ToList();

        var groups = await groupRepo.GetAllAsync(ct);
        var groupNameById = groups.ToDictionary(g => g.Id, g => g.Name);

        var items = await assessmentQueryRepo.GetRecordsInRangeAsync(periodStart, periodEndExclusive, includeInactiveCriteria: false, ct);
        var current = PeriodAggregateCalculator.Compute(periodStart, periodEndExclusive, items, activeCriteria);

        var allDates = await assessmentQueryRepo.GetAllDistinctAssessmentDatesAsync(ct);
        var previousRange = FindPreviousPeriodRange(period, periodStart, allDates);

        PeriodAggregateResult? previous = null;
        if (previousRange is { } pr)
        {
            var previousItems = await assessmentQueryRepo.GetRecordsInRangeAsync(pr.Start, pr.EndExclusive, includeInactiveCriteria: false, ct);
            previous = PeriodAggregateCalculator.Compute(pr.Start, pr.EndExclusive, previousItems, activeCriteria);
        }

        var kpi = BuildKpi(activeCriteria, current, previous);
        var groupProgress = current.ProgressByGroup
            .Select(kv => new GroupProgressDto(kv.Key, groupNameById.GetValueOrDefault(kv.Key, "?"), kv.Value))
            .OrderBy(g => g.GroupName)
            .ToList();
        var trend = current.WeeklyBreakdown.Select(w => new TrendPointDto(w.WeekStart, w.Progress)).ToList();
        var table = BuildTable(activeCriteriaEntities, groupNameById, current, previous);

        return new DashboardDto
        {
            HasData = current.OverallProgress.HasValue,
            Kpi = kpi,
            GroupProgress = groupProgress,
            Trend = trend,
            Table = table,
        };
    }

    public async Task<DashboardPeriodsDto> GetPeriodsAsync(int? year, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
        var resolvedYear = year ?? today.Year;

        var allDates = await assessmentQueryRepo.GetAllDistinctAssessmentDatesAsync(ct);

        // LUÔN kèm năm hiện tại dù chưa có dữ liệu (CONTRACT DB-3) — không chỉ liệt kê năm có
        // dữ liệu, để FE luôn có ít nhất 1 lựa chọn năm hợp lệ (năm hiện tại) trong dropdown.
        var years = allDates.Select(d => d.Year).Append(today.Year).Distinct().OrderByDescending(y => y).ToList();

        var activeCriteriaEntities = await criteriaRepo.GetAllActiveAsync(ct);
        var activeCriteria = activeCriteriaEntities
            .Select(c => new CriteriaSummaryDto(c.Id, c.GroupId, c.MaxScore))
            .ToList();

        var yearStart = new DateOnly(resolvedYear, 1, 1);
        var yearEndExclusive = new DateOnly(resolvedYear + 1, 1, 1);
        var yearItems = await assessmentQueryRepo.GetRecordsInRangeAsync(yearStart, yearEndExclusive, includeInactiveCriteria: false, ct);
        var datesInYear = allDates.Where(d => d.Year == resolvedYear).ToList();

        var weeksInYear = datesInYear
            .Select(PeriodRangeCalculator.IsoWeekStart)
            .Distinct()
            .OrderBy(w => w)
            .Select(weekStart =>
            {
                var result = PeriodAggregateCalculator.Compute(weekStart, weekStart.AddDays(7), yearItems, activeCriteria);
                return new PeriodOptionDto(PeriodRangeCalculator.IsoWeekValue(weekStart), weekStart, result.OverallProgress);
            })
            .ToList();

        var monthsInYear = datesInYear
            .Select(d => new DateOnly(d.Year, d.Month, 1))
            .Distinct()
            .OrderBy(m => m)
            .Select(monthStart =>
            {
                var result = PeriodAggregateCalculator.Compute(monthStart, monthStart.AddMonths(1), yearItems, activeCriteria);
                return new PeriodOptionDto(PeriodRangeCalculator.MonthValue(monthStart), monthStart, result.OverallProgress);
            })
            .ToList();

        return new DashboardPeriodsDto { Years = years, WeeksInYear = weeksInYear, MonthsInYear = monthsInYear };
    }

    public async Task<string> GetReportHtmlAsync(int? year, string period, string? periodValue, CancellationToken ct)
    {
        var dashboard = await GetDashboardAsync(year, period, periodValue, ct);

        if (!dashboard.HasData)
            return "<div class=\"dti-report\"><p>Chưa có dữ liệu cho kỳ đã chọn.</p></div>";

        var topIncrease = dashboard.Table
            .Where(r => r.Delta is not null && r.Delta > Epsilon)
            .OrderByDescending(r => r.Delta)
            .Take(8)
            .ToList();

        var stalled = dashboard.Table
            .Where(r => r.Progress is not null && r.Progress < DoneThreshold
                        && r.Delta is not null && r.Delta <= Epsilon)
            .Take(8)
            .ToList();

        var sb = new StringBuilder();
        sb.Append("<div class=\"dti-report\">");
        sb.Append("<h3>Báo cáo nhanh</h3>");
        AppendSection(sb, "Top tăng nhiều nhất", topIncrease);
        AppendSection(sb, "Dừng lại (không tăng, chưa hoàn thành)", stalled);
        sb.Append("</div>");

        return sb.ToString();
    }

    private static void AppendSection(StringBuilder sb, string title, IReadOnlyList<CriteriaTableRowDto> rows)
    {
        sb.Append("<section><h4>").Append(System.Net.WebUtility.HtmlEncode(title)).Append("</h4>");
        if (rows.Count == 0)
        {
            sb.Append("<p>Không có dữ liệu.</p></section>");
            return;
        }

        sb.Append("<ol>");
        foreach (var row in rows)
        {
            var deltaText = row.Delta is null ? "—" : $"{row.Delta:+0.00;-0.00;0.00}%";
            sb.Append("<li>")
              .Append(System.Net.WebUtility.HtmlEncode(row.Code)).Append(" — ")
              .Append(System.Net.WebUtility.HtmlEncode(row.Name))
              .Append(" (").Append(row.Progress?.ToString("0.00") ?? "—").Append("%, ").Append(deltaText).Append(")")
              .Append("</li>");
        }

        sb.Append("</ol></section>");
    }

    private static DashboardKpiDto BuildKpi(
        IReadOnlyList<CriteriaSummaryDto> activeCriteria, PeriodAggregateResult current, PeriodAggregateResult? previous)
    {
        int up = 0, flat = 0, down = 0, done = 0;

        foreach (var criteria in activeCriteria)
        {
            var v = current.ProgressByCriteria.GetValueOrDefault(criteria.Id);
            if (v is null)
                continue; // loại khỏi MỌI số đếm — xem spec/dashboard-dti-weekly/business-rules.md mục 3.5

            if (v.Value >= DoneThreshold)
                done++;

            var pv = previous?.ProgressByCriteria.GetValueOrDefault(criteria.Id);
            if (pv is null)
                continue;

            var delta = v.Value - pv.Value;
            if (delta > Epsilon) up++;
            else if (delta < -Epsilon) down++;
            else flat++;
        }

        decimal? delta2 = current.OverallProgress is { } cv && previous?.OverallProgress is { } pv2 ? cv - pv2 : null;

        return new DashboardKpiDto(current.OverallProgress, delta2, up, flat, down, done);
    }

    private static List<CriteriaTableRowDto> BuildTable(
        IReadOnlyList<Domain.Entities.Criteria> activeCriteria,
        IReadOnlyDictionary<Guid, string> groupNameById,
        PeriodAggregateResult current,
        PeriodAggregateResult? previous)
    {
        var table = new List<CriteriaTableRowDto>(activeCriteria.Count);

        foreach (var criteria in activeCriteria)
        {
            var v = current.ProgressByCriteria.GetValueOrDefault(criteria.Id);
            var pv = previous?.ProgressByCriteria.GetValueOrDefault(criteria.Id);
            var delta = v is not null && pv is not null ? v.Value - pv.Value : (decimal?)null;
            var (badge, label) = ComputeBadge(v, pv);

            table.Add(new CriteriaTableRowDto(
                criteria.Id, criteria.Code, criteria.Name, criteria.GroupId,
                groupNameById.GetValueOrDefault(criteria.GroupId, "?"), criteria.MaxScore,
                v, delta, badge, label));
        }

        return [.. table.OrderBy(r => r.Code)];
    }

    /// <summary>statusFor(v,d) gốc của dashboard.html — 3 giá trị, badge KHÔNG phân biệt
    /// "giảm" riêng (giảm cũng rơi vào "Không tăng"), xem doc/ERD/ERD.md mục 4.</summary>
    private static (string Badge, string Label) ComputeBadge(decimal? v, decimal? pv)
    {
        if (v is null)
            return ("bnodata", "Chưa có dữ liệu");
        if (v.Value >= DoneThreshold)
            return ("bdone", "Hoàn thành");
        if (pv is not null && (v.Value - pv.Value) <= Epsilon)
            return ("bstall", "Không tăng");

        return ("bwork", "Đang thực hiện");
    }

    /// <summary>Kỳ liền trước gần nhất CÓ dữ liệu — không nhất thiết liền kề theo lịch, xem
    /// spec/dashboard-dti-weekly/business-rules.md mục 3.2.</summary>
    private static (DateOnly Start, DateOnly EndExclusive)? FindPreviousPeriodRange(
        string period, DateOnly currentPeriodStart, IReadOnlyList<DateOnly> allDates)
    {
        switch (period)
        {
            case PeriodRangeCalculator.PeriodWeek:
            {
                var candidates = allDates.Select(PeriodRangeCalculator.IsoWeekStart).Where(w => w < currentPeriodStart).ToList();
                return candidates.Count == 0 ? null : (candidates.Max(), candidates.Max().AddDays(7));
            }
            case PeriodRangeCalculator.PeriodMonth:
            {
                var candidates = allDates.Select(d => new DateOnly(d.Year, d.Month, 1)).Where(m => m < currentPeriodStart).ToList();
                return candidates.Count == 0 ? null : (candidates.Max(), candidates.Max().AddMonths(1));
            }
            default:
            {
                var candidates = allDates.Select(d => new DateOnly(d.Year, 1, 1)).Where(y => y < currentPeriodStart).ToList();
                return candidates.Count == 0 ? null : (candidates.Max(), candidates.Max().AddYears(1));
            }
        }
    }
}

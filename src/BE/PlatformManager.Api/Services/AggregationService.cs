using System.Globalization;
using Microsoft.EntityFrameworkCore;
using PlatformManager.Api.Common;
using PlatformManager.Api.Data;
using PlatformManager.Api.Dtos;
using PlatformManager.Api.Entities;

namespace PlatformManager.Api.Services;

/// <summary>
/// Công thức "Tiến độ chung"/theo nhóm/theo tháng/theo năm — KHÔNG carry-forward
/// (chỉ tiêu không có record trong đúng kỳ đang xem bị loại khỏi CẢ tử số lẫn
/// mẫu số). Xem doc/ERD/ERD.md mục "Kỳ (tuần/tháng/năm)" và
/// spec/dashboard-dti-weekly/business-rules.md mục 3, spec/danh-muc-dti/business-rules.md mục 2.4/3.
///
/// Thiết kế cốt lõi: MỘT hàm thuần (<see cref="ComputePeriodAggregate"/>) tính
/// đúng công thức cho cả 3 cấp độ (tuần/tháng/năm) — vì công thức tháng/năm
/// ("trung bình cộng các kỳ-tuần có dữ liệu trong phạm vi") suy biến đúng về
/// công thức tuần khi phạm vi chỉ là 1 tuần (danh sách kỳ-tuần chỉ còn 1 phần
/// tử). Tuần ISO (thứ Hai–Chủ Nhật) qua System.Globalization.ISOWeek.
/// </summary>
public class AggregationService(AppDbContext db)
{
    private sealed record PeriodAggregate(
        decimal? Overall,
        Dictionary<Guid, decimal?> PerCriteria,
        Dictionary<Guid, decimal?> PerGroup);

    // ===================== Helpers thời gian =====================

    private static DateOnly WeekStartOf(DateOnly date)
    {
        var dt = date.ToDateTime(TimeOnly.MinValue);
        var isoYear = ISOWeek.GetYear(dt);
        var isoWeek = ISOWeek.GetWeekOfYear(dt);
        return DateOnly.FromDateTime(ISOWeek.ToDateTime(isoYear, isoWeek, DayOfWeek.Monday));
    }

    private static DateTimeOffset ToUtc(DateOnly date) => new(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

    private const decimal Epsilon = 0.001m;

    // ===================== Load dữ liệu thô =====================

    private async Task<List<CriteriaAssessment>> LoadAssessmentsAsync(DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken ct)
        => await db.CriteriaAssessments
            .Where(a => a.CreatedAt >= fromUtc && a.CreatedAt < toUtc)
            .AsNoTracking()
            .ToListAsync(ct);

    /// <summary>Load đủ dữ liệu (mở rộng theo tuần đầy đủ) cho 1 khoảng [periodStart, periodEndExclusive) rồi tính aggregate thuần.</summary>
    private async Task<PeriodAggregate> GetPeriodAggregateAsync(
        DateOnly periodStart, DateOnly periodEndExclusive, List<Criteria> activeCriteria, CancellationToken ct)
    {
        var expandedFrom = WeekStartOf(periodStart);
        var expandedTo = WeekStartOf(periodEndExclusive.AddDays(-1)).AddDays(7);
        var items = await LoadAssessmentsAsync(ToUtc(expandedFrom), ToUtc(expandedTo), ct);
        return ComputePeriodAggregate(periodStart, periodEndExclusive, items, activeCriteria);
    }

    private static PeriodAggregate ComputePeriodAggregate(
        DateOnly periodStart, DateOnly periodEndExclusive, List<CriteriaAssessment> items, List<Criteria> activeCriteria)
    {
        var periodStartUtc = ToUtc(periodStart);
        var periodEndUtc = ToUtc(periodEndExclusive);

        // Giá trị đại diện của mỗi (tuần, chỉ tiêu) = record CreatedAt lớn nhất trong tuần đó
        // (đúng rule "1 tuần có thể có vài ngày khác nhau có thao tác -> lấy CreatedAt lớn nhất").
        var byWeekCriteria = items
            .GroupBy(a => (Week: WeekStartOf(DateOnly.FromDateTime(a.CreatedAt.UtcDateTime)), a.CriteriaId))
            .ToDictionary(g => g.Key, g => g.OrderByDescending(a => a.CreatedAt).First());

        // Với TỪNG chỉ tiêu: chỉ những tuần THỰC SỰ có record của chính chỉ tiêu đó
        // rơi vào đúng phạm vi [periodStart, periodEndExclusive) mới được tính — KHÔNG carry-forward.
        var criteriaRelevantWeeks = items
            .Where(a => a.CreatedAt >= periodStartUtc && a.CreatedAt < periodEndUtc)
            .Select(a => (Week: WeekStartOf(DateOnly.FromDateTime(a.CreatedAt.UtcDateTime)), a.CriteriaId))
            .Distinct()
            .ToLookup(x => x.CriteriaId, x => x.Week);

        var perCriteria = new Dictionary<Guid, decimal?>();
        foreach (var c in activeCriteria)
        {
            var weeks = criteriaRelevantWeeks[c.Id].ToList();
            if (weeks.Count == 0) { perCriteria[c.Id] = null; continue; }
            var vals = weeks
                .Select(w => byWeekCriteria.TryGetValue((w, c.Id), out var a) ? (decimal?)a.ProgressPercent : null)
                .Where(v => v.HasValue).Select(v => v!.Value).ToList();
            perCriteria[c.Id] = vals.Count > 0 ? vals.Average() : null;
        }

        // "Tiến độ chung" của phạm vi = TRUNG BÌNH CỘNG "Tiến độ chung theo tuần" của các
        // kỳ-tuần có ít nhất 1 chỉ tiêu bất kỳ có record trong phạm vi (union).
        // Khi phạm vi = đúng 1 tuần, tập này chỉ có 1 phần tử -> suy biến về công thức
        // bình quân gia quyền chuẩn của riêng tuần đó.
        var relevantWeeks = criteriaRelevantWeeks.SelectMany(g => g).Distinct().ToList();
        var weeklyOveralls = new List<decimal>();
        var weeklyGroupOveralls = new Dictionary<Guid, List<decimal>>();
        foreach (var w in relevantWeeks)
        {
            decimal num = 0, den = 0;
            var gnum = new Dictionary<Guid, decimal>();
            var gden = new Dictionary<Guid, decimal>();
            foreach (var c in activeCriteria)
            {
                if (!byWeekCriteria.TryGetValue((w, c.Id), out var a)) continue;
                var contrib = c.MaxScore * a.ProgressPercent / 100m;
                num += contrib; den += c.MaxScore;
                gnum[c.GroupId] = gnum.GetValueOrDefault(c.GroupId) + contrib;
                gden[c.GroupId] = gden.GetValueOrDefault(c.GroupId) + c.MaxScore;
            }
            if (den > 0) weeklyOveralls.Add(num / den * 100m);
            foreach (var (gid, d) in gden)
            {
                if (d <= 0) continue;
                if (!weeklyGroupOveralls.TryGetValue(gid, out var list)) weeklyGroupOveralls[gid] = list = [];
                list.Add(gnum[gid] / d * 100m);
            }
        }

        var overall = weeklyOveralls.Count > 0 ? weeklyOveralls.Average() : (decimal?)null;
        var perGroup = activeCriteria.Select(c => c.GroupId).Distinct().ToDictionary(
            gid => gid,
            gid => weeklyGroupOveralls.TryGetValue(gid, out var l) && l.Count > 0 ? (decimal?)l.Average() : null);

        return new PeriodAggregate(overall, perCriteria, perGroup);
    }

    /// <summary>"Kỳ liền trước có dữ liệu" — gần nhất trước periodStart có ít nhất 1 CriteriaAssessment, KHÔNG nhất thiết liền kề theo lịch.</summary>
    private async Task<DateOnly?> FindPreviousPeriodStartAsync(string mode, DateOnly periodStart, CancellationToken ct)
    {
        var beforeUtc = ToUtc(periodStart);
        var latest = await db.CriteriaAssessments
            .Where(a => a.CreatedAt < beforeUtc)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => (DateTimeOffset?)a.CreatedAt)
            .FirstOrDefaultAsync(ct);
        if (latest is null) return null;
        var d = DateOnly.FromDateTime(latest.Value.UtcDateTime);
        return mode switch
        {
            "week" => WeekStartOf(d),
            "month" => new DateOnly(d.Year, d.Month, 1),
            "year" => new DateOnly(d.Year, 1, 1),
            _ => null,
        };
    }

    private static DateOnly PeriodEndOf(string mode, DateOnly start) => mode switch
    {
        "week" => start.AddDays(7),
        "month" => start.AddMonths(1),
        "year" => new DateOnly(start.Year + 1, 1, 1),
        _ => start.AddDays(1),
    };

    private static (string Text, string Badge) StatusFor(decimal? v, decimal? d)
    {
        if (v is null) return ("Chưa có dữ liệu", "bwork");
        if (v.Value >= 99.999m) return ("Hoàn thành", "bdone");
        if (d is not null && d.Value <= Epsilon) return ("Không tăng", "bstall");
        return ("Đang thực hiện", "bwork");
    }

    // ===================== Dashboard =====================

    public async Task<DashboardResponseDto> GetDashboardAsync(string mode, DateOnly date, int year, CancellationToken ct)
    {
        mode = mode.ToLowerInvariant();
        if (mode is not ("week" or "month" or "year"))
            throw ApiException.BadRequest("DASHBOARD_MODE_INVALID", "mode phải là week|month|year.");

        var activeCriteria = await db.Criteria.Include(c => c.Group).AsNoTracking().OrderBy(c => c.Code).ToListAsync(ct);

        DateOnly periodStart = mode switch
        {
            "week" => WeekStartOf(date),
            "month" => new DateOnly(date.Year, date.Month, 1),
            _ => new DateOnly(year, 1, 1),
        };
        var periodEnd = PeriodEndOf(mode, periodStart);

        var current = await GetPeriodAggregateAsync(periodStart, periodEnd, activeCriteria, ct);
        var prevStart = await FindPreviousPeriodStartAsync(mode, periodStart, ct);
        PeriodAggregate? previous = prevStart.HasValue
            ? await GetPeriodAggregateAsync(prevStart.Value, PeriodEndOf(mode, prevStart.Value), activeCriteria, ct)
            : null;

        var delta = (current.Overall.HasValue && previous?.Overall != null) ? current.Overall - previous.Overall : null;

        int up = 0, flat = 0, down = 0, done = 0;
        foreach (var c in activeCriteria)
        {
            var v = current.PerCriteria.GetValueOrDefault(c.Id);
            if (v is null) continue;
            if (v.Value >= 99.999m) done++;
            var pv = previous?.PerCriteria.GetValueOrDefault(c.Id);
            if (pv is null) continue;
            if (v.Value > pv.Value + Epsilon) up++;
            else if (v.Value < pv.Value - Epsilon) down++;
            else flat++;
        }

        var groups = activeCriteria
            .GroupBy(c => c.GroupId)
            .Select(g => new DashboardGroupProgressDto
            {
                GroupId = g.Key,
                GroupCode = g.First().Group?.Code ?? "",
                GroupName = g.First().Group?.Name ?? "",
                Progress = current.PerGroup.GetValueOrDefault(g.Key),
            })
            .OrderBy(g => g.GroupCode, StringComparer.Ordinal)
            .ToList();

        var table = activeCriteria.Select(c =>
        {
            var v = current.PerCriteria.GetValueOrDefault(c.Id);
            var pv = previous?.PerCriteria.GetValueOrDefault(c.Id);
            var d = (v.HasValue && pv.HasValue) ? v - pv : null;
            var (badgeText, _) = StatusFor(v, d);
            return new DashboardTableRowDto
            {
                CriteriaId = c.Id,
                Code = c.Code,
                Name = c.Name,
                GroupCode = c.Group?.Code ?? "",
                GroupName = c.Group?.Name ?? "",
                MaxScore = c.MaxScore,
                PreviousValue = pv,
                CurrentValue = v,
                Delta = d,
                Badge = badgeText,
            };
        }).OrderBy(r => r.Code, StringComparer.Ordinal).ToList();

        var trend = await BuildTrendAsync(mode, year, activeCriteria, ct);

        return new DashboardResponseDto
        {
            Mode = mode,
            PeriodLabel = FormatPeriodLabel(mode, periodStart, year),
            Kpi = new DashboardKpiDto
            {
                OverallProgress = current.Overall,
                Delta = delta,
                PreviousPeriodLabel = prevStart.HasValue ? FormatPeriodLabel(mode, prevStart.Value, prevStart.Value.Year) : null,
                Up = up,
                Flat = flat,
                Down = down,
                Done = done,
                TotalCriteria = activeCriteria.Count,
            },
            Groups = groups,
            Trend = trend,
            Table = table,
        };
    }

    private async Task<List<DashboardTrendPointDto>> BuildTrendAsync(string mode, int year, List<Criteria> activeCriteria, CancellationToken ct)
    {
        var points = new List<DashboardTrendPointDto>();
        if (mode == "week")
        {
            var isoWeeksInYear = ISOWeek.GetWeeksInYear(year);
            for (var w = 1; w <= isoWeeksInYear; w++)
            {
                var start = DateOnly.FromDateTime(ISOWeek.ToDateTime(year, w, DayOfWeek.Monday));
                var agg = await GetPeriodAggregateAsync(start, start.AddDays(7), activeCriteria, ct);
                if (agg.Overall.HasValue)
                    points.Add(new DashboardTrendPointDto { Label = $"{year}-W{w:00}", Value = agg.Overall });
            }
        }
        else
        {
            for (var m = 1; m <= 12; m++)
            {
                var start = new DateOnly(year, m, 1);
                var agg = await GetPeriodAggregateAsync(start, start.AddMonths(1), activeCriteria, ct);
                if (agg.Overall.HasValue)
                    points.Add(new DashboardTrendPointDto { Label = $"Th.{m}", Value = agg.Overall });
            }
        }
        return points;
    }

    private static string FormatPeriodLabel(string mode, DateOnly start, int year) => mode switch
    {
        "week" => $"Tuần {ISOWeek.GetWeekOfYear(start.ToDateTime(TimeOnly.MinValue))}/{ISOWeek.GetYear(start.ToDateTime(TimeOnly.MinValue))} ({start:dd/MM}–{start.AddDays(6):dd/MM/yyyy})",
        "month" => $"Tháng {start.Month}/{start.Year}",
        _ => $"Năm {year}",
    };

    // ===================== Report (text/HTML "Xuất báo cáo") =====================

    public async Task<ReportResponseDto> GetReportAsync(string mode, DateOnly date, int year, CancellationToken ct)
    {
        var dashboard = await GetDashboardAsync(mode, date, year, ct);
        var ups = dashboard.Table.Where(r => r.Delta is > 0).OrderByDescending(r => r.Delta).Take(8).ToList();
        var flats = dashboard.Table.Where(r => r.Delta is not null && Math.Abs(r.Delta.Value) <= Epsilon && (r.CurrentValue ?? 0) < 100).Take(8).ToList();

        string title;
        string html;
        var overallText = dashboard.Kpi.OverallProgress is null ? "—" : $"{dashboard.Kpi.OverallProgress:0.##}%";

        if (mode == "week")
        {
            title = "Báo cáo tiến độ DTI — theo tuần";
            var deltaText = dashboard.Kpi.Delta is null
                ? ""
                : $", {(dashboard.Kpi.Delta >= 0 ? "tăng" : "giảm")} <b>{Math.Abs(dashboard.Kpi.Delta.Value):0.##} điểm %</b> so với {dashboard.Kpi.PreviousPeriodLabel}";
            html = $"""
                <b>BÁO CÁO NHANH TIẾN ĐỘ CHỈ SỐ CHUYỂN ĐỔI SỐ</b><br>Kỳ cập nhật: <b>{dashboard.PeriodLabel}</b>.<br><br>
                Tiến độ chung hiện đạt <b>{overallText}</b>{deltaText}.
                Có <b>{dashboard.Kpi.Up} chỉ tiêu tăng</b>, <b>{dashboard.Kpi.Flat} chỉ tiêu không thay đổi</b>, <b>{dashboard.Kpi.Down} chỉ tiêu giảm</b> và <b>{dashboard.Kpi.Done}/{dashboard.Kpi.TotalCriteria} chỉ tiêu hoàn thành 100%</b>.<br><br>
                <b>Chỉ tiêu tăng nhiều:</b> {(ups.Count > 0 ? string.Join("; ", ups.Select(o => $"{o.Code} (+{o.Delta:0.##} điểm %)")) : "Chưa có")}.<br><br>
                <b>Chỉ tiêu chưa tăng cần chú ý:</b> {(flats.Count > 0 ? string.Join(", ", flats.Select(o => o.Code)) : "Không có")}.
                """;
        }
        else if (mode == "month")
        {
            title = "Báo cáo tiến độ DTI — theo tháng";
            var deltaText = dashboard.Kpi.Delta is null
                ? ""
                : $", {(dashboard.Kpi.Delta >= 0 ? "tăng" : "giảm")} <b>{Math.Abs(dashboard.Kpi.Delta.Value):0.##} điểm %</b> so với {dashboard.Kpi.PreviousPeriodLabel}";
            html = $"""
                <b>BÁO CÁO TỔNG HỢP THEO THÁNG — CHỈ SỐ CHUYỂN ĐỔI SỐ</b><br>Tháng: <b>{dashboard.PeriodLabel}</b>.<br><br>
                Tiến độ trung bình tháng đạt <b>{overallText}</b>{deltaText}.<br><br>
                <b>Chỉ tiêu tăng nhiều (trung bình tháng):</b> {(ups.Count > 0 ? string.Join("; ", ups.Select(o => $"{o.Code} (+{o.Delta:0.##} điểm %)")) : "Chưa có")}.<br><br>
                <b>Chỉ tiêu chưa tăng cần chú ý:</b> {(flats.Count > 0 ? string.Join(", ", flats.Select(o => o.Code)) : "Không có")}.
                """;
        }
        else
        {
            title = $"Báo cáo tiến độ DTI — tổng hợp năm {year}";
            html = $"""
                <b>BÁO CÁO TỔNG HỢP NĂM {year} — CHỈ SỐ CHUYỂN ĐỔI SỐ</b><br>Tổng hợp dữ liệu trong năm {year} (trung bình cộng, KHÔNG carry-forward — kỳ không có thao tác cho 1 chỉ tiêu bị loại khỏi mẫu tính, không tính là 0%).<br><br>
                Tiến độ trung bình năm {year} đạt <b>{overallText}</b>.
                """;
        }

        return new ReportResponseDto { Title = title, ContentHtml = html };
    }

    // ===================== Danh sách năm/kỳ có dữ liệu (dropdown) =====================

    public async Task<PeriodOptionsDto> GetPeriodOptionsAsync(int? year, CancellationToken ct)
    {
        var currentYear = DateTimeOffset.UtcNow.Year;
        var years = await db.CriteriaAssessments
            .Select(a => a.CreatedAt.Year)
            .Distinct()
            .ToListAsync(ct);
        if (!years.Contains(currentYear)) years.Add(currentYear);
        years.Sort();

        var result = new PeriodOptionsDto { Years = years };
        if (year is null) return result;

        var activeCriteria = await db.Criteria.Include(c => c.Group).AsNoTracking().ToListAsync(ct);
        var yearStartUtc = ToUtc(new DateOnly(year.Value, 1, 1));
        var yearEndUtc = ToUtc(new DateOnly(year.Value + 1, 1, 1));
        var itemsInYear = await LoadAssessmentsAsync(yearStartUtc, yearEndUtc, ct);

        var weekStarts = itemsInYear.Select(a => WeekStartOf(DateOnly.FromDateTime(a.CreatedAt.UtcDateTime))).Distinct().OrderByDescending(d => d).ToList();
        foreach (var ws in weekStarts)
        {
            var agg = await GetPeriodAggregateAsync(ws, ws.AddDays(7), activeCriteria, ct);
            result.WeeksInYear.Add(new PeriodOptionDto
            {
                Value = $"{ISOWeek.GetYear(ws.ToDateTime(TimeOnly.MinValue))}-W{ISOWeek.GetWeekOfYear(ws.ToDateTime(TimeOnly.MinValue)):00}",
                Date = ws,
                OverallProgress = agg.Overall,
            });
        }

        var monthStarts = itemsInYear.Select(a => new DateOnly(a.CreatedAt.UtcDateTime.Year, a.CreatedAt.UtcDateTime.Month, 1)).Distinct().OrderByDescending(d => d).ToList();
        foreach (var ms in monthStarts)
        {
            var agg = await GetPeriodAggregateAsync(ms, ms.AddMonths(1), activeCriteria, ct);
            result.MonthsInYear.Add(new PeriodOptionDto
            {
                Value = $"{ms.Year}-{ms.Month:00}",
                Date = ms,
                OverallProgress = agg.Overall,
            });
        }

        return result;
    }

    // ===================== Danh mục DTI grid =====================

    private static readonly System.Text.RegularExpressions.Regex WeekPeriodRegex = new(@"^(\d{4})-W(\d{2})$");
    private static readonly System.Text.RegularExpressions.Regex MonthPeriodRegex = new(@"^(\d{4})-(\d{2})$");

    public async Task<List<CriteriaRowDto>> GetCriteriaGridRowsAsync(
        int year, string? period, Guid? groupId, string? search, CancellationToken ct)
    {
        var isCurrentYear = year == DateTimeOffset.UtcNow.Year;
        var isAll = string.IsNullOrWhiteSpace(period) || period == "all";
        var isLive = isCurrentYear && isAll;

        var ownerMap = await db.AppUsers.AsNoTracking().ToDictionaryAsync(u => u.Id, ct);
        List<CriteriaRowDto> rows;

        if (isAll)
        {
            var yearStartUtc = ToUtc(new DateOnly(year, 1, 1));
            var yearEndUtc = ToUtc(new DateOnly(year + 1, 1, 1));
            var records = await db.CriteriaAssessments
                .Include(a => a.Evidences)
                .Where(a => a.CreatedAt >= yearStartUtc && a.CreatedAt < yearEndUtc)
                .OrderBy(a => a.CreatedAt)
                .AsNoTracking()
                .ToListAsync(ct);

            var criteriaIds = records.Select(r => r.CriteriaId).Distinct().ToList();
            // Chỉ tiêu có thể đã bị soft-delete sau đó — vẫn phải hiển thị lịch sử, NHƯNG chỉ khi
            // đang xem đúng LỊCH SỬ (năm khác/period cụ thể). Ở trạng thái Live (năm hiện tại +
            // "Tất cả"), một chỉ tiêu vừa bị soft-delete phải BIẾN MẤT khỏi grid ngay — đúng rule
            // đã chốt ở spec/danh-muc-dti/business-rules.md mục 1.3 ("Không xuất hiện trong danh
            // sách... khi tạo/sửa kỳ hiện tại"). Dùng query filter mặc định (IsDeleted=false) khi
            // isLive, chỉ IgnoreQueryFilters() khi thực sự xem lịch sử.
            var criteriaQuery = db.Criteria.Include(c => c.Group).Where(c => criteriaIds.Contains(c.Id));
            if (!isLive) criteriaQuery = criteriaQuery.IgnoreQueryFilters();
            var criteriaMap = await criteriaQuery.AsNoTracking().ToDictionaryAsync(c => c.Id, ct);

            rows = records
                .Select(a => MapRow(a, criteriaMap.GetValueOrDefault(a.CriteriaId), ownerMap, isLive))
                .Where(r => r != null)
                .Select(r => r!)
                .ToList();

            // Bổ sung (KHÔNG có trong spec gốc, tự quyết định để tránh grid trống hoàn toàn trên hệ
            // thống mới/chỉ tiêu chưa từng được đánh giá): mọi Criteria active CHƯA TỪNG có
            // CriteriaAssessment nào (ở BẤT KỲ năm nào, không riêng năm đang xem) vẫn hiện 1 dòng
            // placeholder (field đánh giá = null) — để tab "Chỉ tiêu" (CRUD) luôn thấy đủ danh mục,
            // không phụ thuộc việc đã có ai nhập liệu hay chưa. KHÔNG áp dụng cho Criteria đã có
            // record ở NĂM KHÁC nhưng không có record trong năm đang xem — trường hợp đó đúng theo
            // spec là "không hiện" (đổi năm xem lịch sử đúng năm đó).
            var everTouchedIds = (await db.CriteriaAssessments.IgnoreQueryFilters()
                .Select(a => a.CriteriaId).Distinct().ToListAsync(ct)).ToHashSet();
            var neverTouched = await db.Criteria.Include(c => c.Group)
                .Where(c => !everTouchedIds.Contains(c.Id))
                .AsNoTracking()
                .ToListAsync(ct);
            rows.AddRange(neverTouched.Select(c => MapRow(null, c, ownerMap, isLive)!));

            rows = rows.OrderBy(r => r.Code, StringComparer.Ordinal).ThenBy(r => r.AssessmentDate).ToList();
        }
        else
        {
            var (start, end) = ParsePeriod(period!, year);
            var startUtc = ToUtc(start);
            var endUtc = ToUtc(end);
            var records = await db.CriteriaAssessments
                .Include(a => a.Evidences)
                .Where(a => a.CreatedAt >= startUtc && a.CreatedAt < endUtc)
                .AsNoTracking()
                .ToListAsync(ct);
            var latestByCriteria = records.GroupBy(a => a.CriteriaId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(a => a.CreatedAt).First());

            var activeCriteria = await db.Criteria.Include(c => c.Group).AsNoTracking().OrderBy(c => c.Code).ToListAsync(ct);
            rows = activeCriteria
                .Select(c => MapRow(latestByCriteria.GetValueOrDefault(c.Id), c, ownerMap, isLive))
                .Where(r => r != null)
                .Select(r => r!)
                .ToList();
        }

        if (groupId.HasValue)
            rows = rows.Where(r => r.GroupId == groupId.Value).ToList();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            rows = rows.Where(r => (r.Code + " " + r.Name).Contains(s, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        return rows;
    }

    /// <summary>period dạng "YYYY-Www" (tuần ISO, vd "2026-W07") hoặc "YYYY-MM" (tháng, vd "2026-03").</summary>
    private static (DateOnly Start, DateOnly EndExclusive) ParsePeriod(string period, int year)
    {
        var weekMatch = WeekPeriodRegex.Match(period);
        if (weekMatch.Success)
        {
            var y = int.Parse(weekMatch.Groups[1].Value);
            var w = int.Parse(weekMatch.Groups[2].Value);
            var start = DateOnly.FromDateTime(ISOWeek.ToDateTime(y, w, DayOfWeek.Monday));
            return (start, start.AddDays(7));
        }
        var monthMatch = MonthPeriodRegex.Match(period);
        if (monthMatch.Success)
        {
            var y = int.Parse(monthMatch.Groups[1].Value);
            var m = int.Parse(monthMatch.Groups[2].Value);
            var start = new DateOnly(y, m, 1);
            return (start, start.AddMonths(1));
        }
        throw ApiException.BadRequest("PERIOD_INVALID", "period phải là 'all', 'YYYY-Www' (tuần) hoặc 'YYYY-MM' (tháng).");
    }

    private static CriteriaRowDto? MapRow(CriteriaAssessment? a, Criteria? c, Dictionary<Guid, AppUser> ownerMap, bool isEditable)
    {
        if (c is null) return null;
        var row = new CriteriaRowDto
        {
            CriteriaId = c.Id,
            Code = c.Code,
            Name = c.Name,
            GroupId = c.GroupId,
            GroupCode = c.Group?.Code ?? "",
            GroupName = c.Group?.Name ?? "",
            MaxScore = c.MaxScore,
            IsEditable = isEditable,
        };
        if (a is null) return row;

        row.AssessmentId = a.Id;
        row.ProgressPercent = a.ProgressPercent;
        row.SelfScore = a.SelfScore;
        row.VerifiedScore = a.VerifiedScore;
        row.Diff = (a.SelfScore.HasValue && a.VerifiedScore.HasValue) ? a.SelfScore - a.VerifiedScore : null;
        row.Status = a.Status;
        row.OwnerId = a.OwnerId;
        row.OwnerName = a.OwnerId.HasValue && ownerMap.TryGetValue(a.OwnerId.Value, out var owner) ? owner.FullName : null;
        row.Deadline = a.Deadline;
        row.Note = a.Note;
        row.AssessmentDate = DateOnly.FromDateTime(a.CreatedAt.UtcDateTime);
        row.Evidences = a.Evidences
            .Where(e => !e.IsDeleted)
            .OrderBy(e => e.OrderIndex)
            .Select(e => new CriteriaEvidenceDto { Id = e.Id, Content = e.Content, OrderIndex = e.OrderIndex })
            .ToList();
        return row;
    }
}

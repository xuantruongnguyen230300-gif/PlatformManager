namespace PlatformManager.Modules.DtiWeekly.Application.Dashboard;

public sealed record DashboardKpiDto(
    decimal? OverallProgress,
    decimal? Delta,
    int UpCount,
    int FlatCount,
    int DownCount,
    int DoneCount);

public sealed record GroupProgressDto(Guid GroupId, string GroupName, decimal? Progress);

public sealed record TrendPointDto(DateOnly WeekStart, decimal Progress);

/// <summary>Badge = "bdone"|"bstall"|"bwork"|"bnodata" — khớp 3 lớp CSS gốc của
/// dashboard.html + 1 trạng thái "chưa có dữ liệu" mới. Xem doc/ERD/ERD.md mục 4.</summary>
public sealed record CriteriaTableRowDto(
    Guid CriteriaId,
    string Code,
    string Name,
    Guid GroupId,
    string GroupName,
    decimal MaxScore,
    decimal? Progress,
    decimal? Delta,
    string Badge,
    string BadgeLabel);

public sealed class DashboardDto
{
    public bool HasData { get; init; }
    public required DashboardKpiDto Kpi { get; init; }
    public IReadOnlyList<GroupProgressDto> GroupProgress { get; init; } = [];
    public IReadOnlyList<TrendPointDto> Trend { get; init; } = [];
    public IReadOnlyList<CriteriaTableRowDto> Table { get; init; } = [];
}

/// <summary>1 lựa chọn kỳ trong dropdown — "value" truyền thẳng vào PeriodValue của
/// GET /api/criteria hoặc GET /api/dashboard ("2026-W33" cho tuần ISO, "2026-08" cho tháng).
/// Khớp IPeriodOptionDto ở src/FE/src/app/shared/models/period-options.model.ts (CONTRACT
/// DB-3/DM-8) — KHÔNG đổi lại thành mảng nguyên thuỷ.</summary>
public sealed record PeriodOptionDto(string Value, DateOnly Date, decimal? OverallProgress);

/// <summary>Danh sách kỳ có dữ liệu trong 1 năm — phục vụ dropdown chọn tuần/tháng ở FE
/// (dùng chung Dashboard + Danh mục DTI). GET /api/dashboard/periods.</summary>
public sealed class DashboardPeriodsDto
{
    /// <summary>Mọi năm có dữ liệu, LUÔN kèm năm hiện tại dù chưa có dữ liệu (CONTRACT DB-3).</summary>
    public IReadOnlyList<int> Years { get; init; } = [];
    public IReadOnlyList<PeriodOptionDto> WeeksInYear { get; init; } = [];
    public IReadOnlyList<PeriodOptionDto> MonthsInYear { get; init; } = [];
}

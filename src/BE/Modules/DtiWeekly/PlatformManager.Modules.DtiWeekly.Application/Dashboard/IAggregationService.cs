namespace PlatformManager.Modules.DtiWeekly.Application.Dashboard;

public interface IAggregationService
{
    Task<DashboardDto> GetDashboardAsync(int? year, string period, string? periodValue, CancellationToken ct);

    Task<DashboardPeriodsDto> GetPeriodsAsync(int? year, CancellationToken ct);

    /// <summary>HTML tiếng Việt — top 8 tăng nhiều nhất, top 8 dừng lại (delta≈0, chưa 100%).</summary>
    Task<string> GetReportHtmlAsync(int? year, string period, string? periodValue, CancellationToken ct);
}

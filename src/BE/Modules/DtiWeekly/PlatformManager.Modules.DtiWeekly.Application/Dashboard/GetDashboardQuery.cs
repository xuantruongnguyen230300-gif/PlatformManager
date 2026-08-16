using MediatR;
using PlatformManager.Modules.DtiWeekly.Application.Common;
using PlatformManager.Core.Application.Common.CQRS;
using PlatformManager.Core.Application.Common.Results;

namespace PlatformManager.Modules.DtiWeekly.Application.Dashboard;

/// <summary>GET /api/dashboard — KPI, tiến độ theo nhóm, trend series, bảng chỉ tiêu đầy
/// đủ kèm badge. Read-only (xem spec/dashboard-dti-weekly/business-rules.md mục 0).</summary>
public sealed record GetDashboardQuery(
    int? Year = null,
    string Period = PeriodRangeCalculator.PeriodAll,
    string? PeriodValue = null) : IQuery<DashboardDto>;

public sealed class GetDashboardHandler(IAggregationService aggregationService)
    : BaseResponse, IRequestHandler<GetDashboardQuery, IApiResult<DashboardDto>>
{
    public async Task<IApiResult<DashboardDto>> Handle(GetDashboardQuery query, CancellationToken ct)
    {
        var dashboard = await aggregationService.GetDashboardAsync(query.Year, query.Period, query.PeriodValue, ct);
        return Ok(dashboard);
    }
}

using MediatR;
using PlatformManager.Modules.DtiWeekly.Application.Common;
using PlatformManager.Core.Application.Common.CQRS;
using PlatformManager.Core.Application.Common.Results;

namespace PlatformManager.Modules.DtiWeekly.Application.Dashboard;

/// <summary>GET /api/dashboard/report — "Báo cáo nhanh", trả HTML tiếng Việt.</summary>
public sealed record GetDashboardReportQuery(
    int? Year = null,
    string Period = PeriodRangeCalculator.PeriodAll,
    string? PeriodValue = null) : IQuery<string>;

public sealed class GetDashboardReportHandler(IAggregationService aggregationService)
    : BaseResponse, IRequestHandler<GetDashboardReportQuery, IApiResult<string>>
{
    public async Task<IApiResult<string>> Handle(GetDashboardReportQuery query, CancellationToken ct)
    {
        var html = await aggregationService.GetReportHtmlAsync(query.Year, query.Period, query.PeriodValue, ct);
        return Ok(html);
    }
}

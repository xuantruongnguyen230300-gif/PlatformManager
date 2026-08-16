using MediatR;
using PlatformManager.Core.Application.Common.CQRS;
using PlatformManager.Core.Application.Common.Results;

namespace PlatformManager.Modules.DtiWeekly.Application.Dashboard;

/// <summary>GET /api/dashboard/periods — danh sách năm/tuần/tháng có dữ liệu, phục vụ
/// dropdown chọn kỳ ở FE.</summary>
public sealed record GetDashboardPeriodsQuery(int? Year = null) : IQuery<DashboardPeriodsDto>;

public sealed class GetDashboardPeriodsHandler(IAggregationService aggregationService)
    : BaseResponse, IRequestHandler<GetDashboardPeriodsQuery, IApiResult<DashboardPeriodsDto>>
{
    public async Task<IApiResult<DashboardPeriodsDto>> Handle(GetDashboardPeriodsQuery query, CancellationToken ct)
    {
        var periods = await aggregationService.GetPeriodsAsync(query.Year, ct);
        return Ok(periods);
    }
}

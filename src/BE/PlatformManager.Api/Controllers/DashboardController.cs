using MediatR;
using Microsoft.AspNetCore.Mvc;
using PlatformManager.Api.Common;
using PlatformManager.Modules.DtiWeekly.Application.Dashboard;

namespace PlatformManager.Api.Controllers;

// [Authorize] kế thừa từ ApiControllerBase — mọi user đã đăng nhập đều thao tác được.
[ApiController]
[Route("api/dashboard")]
public class DashboardController(ISender mediator) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] GetDashboardQuery query, CancellationToken ct)
        => HandleResult(await mediator.Send(query, ct));

    [HttpGet("periods")]
    public async Task<IActionResult> Periods([FromQuery] GetDashboardPeriodsQuery query, CancellationToken ct)
        => HandleResult(await mediator.Send(query, ct));

    [HttpGet("report")]
    public async Task<IActionResult> Report([FromQuery] GetDashboardReportQuery query, CancellationToken ct)
        => HandleResult(await mediator.Send(query, ct));
}

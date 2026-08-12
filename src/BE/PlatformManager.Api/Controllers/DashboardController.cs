using Microsoft.AspNetCore.Mvc;
using PlatformManager.Api.Common;
using PlatformManager.Api.Services;

namespace PlatformManager.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
public class DashboardController(AggregationService aggregationService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] string mode,
        [FromQuery] DateOnly? date,
        [FromQuery] int? year,
        CancellationToken ct)
    {
        var (d, y) = ResolveDateAndYear(date, year);
        var dashboard = await aggregationService.GetDashboardAsync(mode, d, y, ct);
        return Ok(ApiResponse.Ok(dashboard, HttpContext.TraceIdentifier));
    }

    /// <summary>Danh sách năm/kỳ-tuần/kỳ-tháng có dữ liệu — dùng cho dropdown lọc ở CẢ Dashboard
    /// lẫn Danh mục DTI (bổ sung ngoài endpoint list gốc, xem PeriodOptionsDto).</summary>
    [HttpGet("periods")]
    public async Task<IActionResult> GetPeriods([FromQuery] int? year, CancellationToken ct)
    {
        var options = await aggregationService.GetPeriodOptionsAsync(year, ct);
        return Ok(ApiResponse.Ok(options, HttpContext.TraceIdentifier));
    }

    [HttpGet("report")]
    public async Task<IActionResult> GetReport(
        [FromQuery] string mode,
        [FromQuery] DateOnly? date,
        [FromQuery] int? year,
        CancellationToken ct)
    {
        var (d, y) = ResolveDateAndYear(date, year);
        var report = await aggregationService.GetReportAsync(mode, d, y, ct);
        return Ok(ApiResponse.Ok(report, HttpContext.TraceIdentifier));
    }

    private static (DateOnly Date, int Year) ResolveDateAndYear(DateOnly? date, int? year)
    {
        var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.Date);
        var d = date ?? today;
        var y = year ?? d.Year;
        return (d, y);
    }
}

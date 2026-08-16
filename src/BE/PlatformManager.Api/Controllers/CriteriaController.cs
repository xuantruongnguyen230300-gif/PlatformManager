using MediatR;
using Microsoft.AspNetCore.Mvc;
using PlatformManager.Api.Common;
using PlatformManager.Modules.DtiWeekly.Application.Assessments;
using PlatformManager.Modules.DtiWeekly.Application.Criteria;

namespace PlatformManager.Api.Controllers;

// [Authorize] kế thừa từ ApiControllerBase — mọi user đã đăng nhập đều thao tác được (đã CHỐT
// với người dùng, không phân biệt role cho nghiệp vụ DTI Weekly).
[ApiController]
[Route("api/criteria")]
public class CriteriaController(ISender mediator) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] GetCriteriaListQuery query, CancellationToken ct)
        => HandleResult(await mediator.Send(query, ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCriteriaCommand cmd, CancellationToken ct)
        => HandleResult(await mediator.Send(cmd, ct));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCriteriaRequest request, CancellationToken ct)
        => HandleResult(await mediator.Send(
            new UpdateCriteriaCommand(id, request.Code, request.Name, request.GroupId, request.MaxScore), ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        => HandleResult(await mediator.Send(new DeleteCriteriaCommand(id), ct));

    [HttpPut("{id:guid}/assessment")]
    public async Task<IActionResult> UpdateAssessment(Guid id, [FromBody] UpdateAssessmentRequest request, CancellationToken ct)
        => HandleResult(await mediator.Send(
            new UpdateCriteriaAssessmentCommand(id, request.ProgressPercent, request.Note,
                request.ViewYear, request.ViewPeriod, request.ViewPeriodValue), ct));
}

public sealed record UpdateCriteriaRequest(string Code, string Name, Guid GroupId, decimal MaxScore);

public sealed record UpdateAssessmentRequest(
    decimal ProgressPercent, string? Note, int ViewYear, string ViewPeriod, string? ViewPeriodValue);

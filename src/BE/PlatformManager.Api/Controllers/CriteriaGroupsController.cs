using MediatR;
using Microsoft.AspNetCore.Mvc;
using PlatformManager.Api.Common;
using PlatformManager.Modules.DtiWeekly.Application.CriteriaGroups;

namespace PlatformManager.Api.Controllers;

// [Authorize] kế thừa từ ApiControllerBase — mọi user đã đăng nhập đều thao tác được (đã CHỐT
// với người dùng, không phân biệt role cho nghiệp vụ DTI Weekly).
[ApiController]
[Route("api/criteria-groups")]
public class CriteriaGroupsController(ISender mediator) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
        => HandleResult(await mediator.Send(new GetCriteriaGroupsListQuery(), ct));
}

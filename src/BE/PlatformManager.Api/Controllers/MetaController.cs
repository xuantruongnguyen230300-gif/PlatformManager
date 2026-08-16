using MediatR;
using Microsoft.AspNetCore.Mvc;
using PlatformManager.Api.Common;
using PlatformManager.Core.Application.Menu;

namespace PlatformManager.Api.Controllers;

// [Authorize] kế thừa từ ApiControllerBase (fail-closed mặc định) — sidebar chỉ hiển thị sau
// khi đăng nhập, không cần AllowAnonymous.
[ApiController]
[Route("api/meta")]
public class MetaController(ISender mediator) : ApiControllerBase
{
    [HttpGet("menu")]
    public async Task<IActionResult> Menu(CancellationToken ct)
        => HandleResult(await mediator.Send(new GetMenuQuery(), ct));
}

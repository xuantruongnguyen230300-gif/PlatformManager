using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlatformManager.Api.Common;
using PlatformManager.Core.Application.Common;
using PlatformManager.Core.Application.Permissions;

namespace PlatformManager.Api.Controllers;

/// <summary>Màn "Phân quyền" — CHỈ SuperAdmin (không cho Admin thường, tránh leo thang
/// quyền — xem doc/ke-hoach-xay-lai-corebase.md).</summary>
[ApiController]
[Route("api/admin/permissions")]
[Authorize(Roles = Roles.SuperAdmin)]
public class PermissionsController(ISender mediator) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
        => HandleResult(await mediator.Send(new GetPermissionMatrixQuery(), ct));

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdatePermissionMatrixCommand cmd, CancellationToken ct)
        => HandleResult(await mediator.Send(cmd, ct));
}

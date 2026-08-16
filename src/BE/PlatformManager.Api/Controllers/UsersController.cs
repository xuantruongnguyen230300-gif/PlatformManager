using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlatformManager.Api.Common;
using PlatformManager.Core.Application.Common;
using PlatformManager.Core.Application.Users;

namespace PlatformManager.Api.Controllers;

/// <summary>Màn "Quản trị người dùng" — gate Admin+SuperAdmin (xem
/// doc/ke-hoach-xay-lai-corebase.md).</summary>
[ApiController]
[Route("api/users")]
[Authorize(Roles = $"{Roles.SuperAdmin},{Roles.Admin}")]
public class UsersController(ISender mediator) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] GetUsersListQuery query, CancellationToken ct)
        => HandleResult(await mediator.Send(query, ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserCommand cmd, CancellationToken ct)
        => HandleResult(await mediator.Send(cmd, ct));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserRequest request, CancellationToken ct)
        => HandleResult(await mediator.Send(new UpdateUserCommand(id, request.Email, request.FullName, request.Roles), ct));

    [HttpPost("{id:guid}/lock")]
    public async Task<IActionResult> Lock(Guid id, CancellationToken ct)
        => HandleResult(await mediator.Send(new LockUserCommand(id), ct));

    [HttpPost("{id:guid}/unlock")]
    public async Task<IActionResult> Unlock(Guid id, CancellationToken ct)
        => HandleResult(await mediator.Send(new UnlockUserCommand(id), ct));
}

public sealed record UpdateUserRequest(string? Email, string FullName, IReadOnlyCollection<string> Roles);

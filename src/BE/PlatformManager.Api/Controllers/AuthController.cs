using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlatformManager.Api.Common;
using PlatformManager.Core.Application.Auth;

namespace PlatformManager.Api.Controllers;

/// <summary>
/// ApiControllerBase đã [Authorize] mặc định (fail-closed) — CHỈ Login tường minh
/// [AllowAnonymous] (chưa đăng nhập mới cần gọi). Logout/Me/ChangePassword KHÔNG cần khai lại
/// [Authorize] (kế thừa từ base) — trước đây Logout thiếu CẢ 2 attribute nên vô tình public,
/// nay tự động được bảo vệ đúng nhờ base class.
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController(ISender mediator) : ApiControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginCommand cmd, CancellationToken ct)
        => HandleResult(await mediator.Send(cmd, ct));

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken ct)
        => HandleResult(await mediator.Send(new LogoutCommand(), ct));

    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct)
        => HandleResult(await mediator.Send(new GetCurrentUserQuery(), ct));

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordCommand cmd, CancellationToken ct)
        => HandleResult(await mediator.Send(cmd, ct));
}

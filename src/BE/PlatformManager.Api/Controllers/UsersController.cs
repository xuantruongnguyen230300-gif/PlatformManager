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
        => HandleResult(await mediator.Send(new UpdateUserCommand(id, request.Email, request.FullName, request.Roles, request.Version), ct));

    [HttpPost("{id:guid}/lock")]
    public async Task<IActionResult> Lock(Guid id, CancellationToken ct)
        => HandleResult(await mediator.Send(new LockUserCommand(id), ct));

    [HttpPost("{id:guid}/unlock")]
    public async Task<IActionResult> Unlock(Guid id, CancellationToken ct)
        => HandleResult(await mediator.Send(new UnlockUserCommand(id), ct));
}

/// <summary>
/// <para><c>Version</c> là <c>ConcurrencyStamp</c> mà <c>GET /api/users</c> (danh sách) vừa trả về
/// — endpoint lấy MỘT người dùng theo id không tồn tại, đừng đi tìm —
/// client gửi lại nguyên văn để server phát hiện "có người khác vừa sửa" và trả 409 thay vì
/// ghi đè im lặng (xem <c>doc/contracts/users.md</c> §"Quyết định người dùng 2026-08-30").</para>
///
/// <para><b>Vì sao nullable chứ không bắt buộc ngay:</b> bắt buộc từ hôm nay nghĩa là mọi client
/// chưa kịp cập nhật đều nhận 409, tức khoá luôn màn Quản trị người dùng. Handler chỉ kiểm khi
/// client thật sự gửi. Siết thành bắt buộc là một bước RIÊNG, làm sau khi FE đã gửi — và lúc đó
/// nhớ xoá cả điều kiện <c>is not null</c> trong <c>UpdateUserCommand</c>, nếu không việc siết
/// chỉ nằm ở tên kiểu chứ không có hiệu lực.</para>
/// </summary>
public sealed record UpdateUserRequest(
    string? Email, string FullName, IReadOnlyCollection<string> Roles, string? Version = null);

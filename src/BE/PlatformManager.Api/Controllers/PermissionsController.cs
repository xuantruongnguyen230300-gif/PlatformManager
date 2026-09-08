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

    // ── PERM-2 — ma trận quyền theo TÀI NGUYÊN (ResourceKey × role) ───────────────────────────
    // TÁCH đường dẫn con "resources" chứ KHÔNG gộp vào 2 action trên: đây là ma trận khác hẳn.
    // PERM-1 ghi SysMenuRole (menu nhìn thấy được, vắng mặt = mở cho mọi người đã đăng nhập);
    // PERM-2 ghi RolePermission (quyền gọi API, vắng mặt = TỪ CHỐI). Hai ngữ nghĩa mặc định
    // ngược nhau — gộp chung 1 payload là mời gọi ghi nhầm bảng. Xem doc/contracts/permissions.md
    // §CONTRACT PERM-2.
    //
    // Gate quyền: class đã mang [Authorize(Roles = Roles.SuperAdmin)] + [Authorize] của
    // ApiControllerBase (2 attribute cộng dồn AND) — 2 action dưới KHÔNG khai lại, cố ý, để chỉ
    // có MỘT chỗ quyết định ai vào được màn "Phân quyền".

    /// <summary>GET /api/admin/permissions/resources — xem <c>GetResourcePermissionMatrixQuery</c>.</summary>
    [HttpGet("resources")]
    public async Task<IActionResult> GetResources(CancellationToken ct)
        => HandleResult(await mediator.Send(new GetResourcePermissionMatrixQuery(), ct));

    /// <summary>PUT /api/admin/permissions/resources — GHI ĐÈ TOÀN BỘ, xem
    /// <c>UpdateResourcePermissionMatrixCommand</c>.</summary>
    [HttpPut("resources")]
    public async Task<IActionResult> UpdateResources(
        [FromBody] UpdateResourcePermissionMatrixCommand cmd, CancellationToken ct)
        => HandleResult(await mediator.Send(cmd, ct));
}

using MediatR;
using Microsoft.AspNetCore.Mvc;
using PlatformManager.Api.Common;
using PlatformManager.Core.Application.Permissions;
using PlatformManager.Modules.DtiWeekly.Application.CriteriaGroups;

namespace PlatformManager.Api.Controllers;

// [Authorize] kế thừa từ ApiControllerBase — [RequirePermission] cộng dồn (không thay thế): chỉ
// role được cấp ResourceKeys.CriteriaGroups mới thao tác được. "Không phân biệt role" (comment
// cũ) là hành vi TRƯỚC KHI PERM-2 bật — nay phân quyền theo hành động qua RolePermission
// (Admin/User đã seed đủ cả 3 key, xem doc/contracts/permissions.md §"Rủi ro rollout").
[ApiController]
[Route("api/criteria-groups")]
[RequirePermission(ResourceKeys.CriteriaGroups)]
public class CriteriaGroupsController(ISender mediator) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
        => HandleResult(await mediator.Send(new GetCriteriaGroupsListQuery(), ct));
}

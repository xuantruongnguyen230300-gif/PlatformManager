using MediatR;
using Microsoft.AspNetCore.Mvc;
using PlatformManager.Business.Application.CriteriaGroups;
using PlatformManager.Core.Api;

namespace PlatformManager.Business.Api.Controllers;

/// <summary>
/// <b>DM-1</b> — danh mục nhóm chỉ tiêu. FE tải một lần lúc khởi tạo màn, dùng cho cả dropdown
/// lọc lẫn dropdown trong dialog.
///
/// <para>Endpoint ĐỌC ⇒ giữ <c>[Authorize]</c> trần (kế thừa từ <see cref="ApiControllerBase"/>),
/// KHÔNG gắn <c>[RequirePermission]</c>: quyền xem không cần key, chỉ cần đăng nhập (Q21 + §6.5
/// bước 2).</para>
/// </summary>
[ApiController]
[Route("api/criteria-groups")]
public class CriteriaGroupsController(ISender mediator) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
        => HandleResult(await mediator.Send(new GetCriteriaGroupsListQuery(), ct));
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlatformManager.Api.Common;
using PlatformManager.Api.Data;
using PlatformManager.Api.Dtos;

namespace PlatformManager.Api.Controllers;

[ApiController]
[Route("api/criteria-groups")]
public class CriteriaGroupsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var groups = await db.CriteriaGroups
            .AsNoTracking()
            .OrderBy(g => g.DisplayOrder)
            .Select(g => new CriteriaGroupDto { Id = g.Id, Code = g.Code, Name = g.Name, DisplayOrder = g.DisplayOrder })
            .ToListAsync(ct);
        return Ok(ApiResponse.Ok(groups, HttpContext.TraceIdentifier));
    }
}

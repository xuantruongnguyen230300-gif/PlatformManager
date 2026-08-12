using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlatformManager.Api.Common;
using PlatformManager.Api.Data;
using PlatformManager.Api.Dtos;
using PlatformManager.Api.Entities;
using PlatformManager.Api.Services;

namespace PlatformManager.Api.Controllers;

[ApiController]
[Route("api/criteria")]
public class CriteriaController(AppDbContext db, AggregationService aggregationService) : ControllerBase
{
    /// <summary>
    /// Grid "Danh mục DTI". period = "all" (mặc định, theo year) | "YYYY-Www" (tuần ISO) | "YYYY-MM" (tháng).
    /// isEditable trong mỗi dòng chỉ true khi year = năm hiện tại VÀ period = "all" — xem
    /// spec/danh-muc-dti/business-rules.md mục 2.4.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int? year,
        [FromQuery] string? period,
        [FromQuery] Guid? groupId,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var y = year ?? DateTimeOffset.UtcNow.Year;
        if (page < 1) page = 1;
        if (pageSize is < 1 or > 500) pageSize = 20;

        var rows = await aggregationService.GetCriteriaGridRowsAsync(y, period, groupId, search, ct);
        var paged = new PagedResult<CriteriaRowDto>
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = rows.Count,
            Items = rows.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
        };
        return Ok(ApiResponse.Ok(paged, HttpContext.TraceIdentifier));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCriteriaRequest req, CancellationToken ct)
    {
        await ValidateAsync(req.Code, req.Name, req.GroupId, req.MaxScore, excludeId: null, ct);

        var entity = new Criteria
        {
            Id = Guid.NewGuid(),
            Code = req.Code.Trim(),
            Name = req.Name.Trim(),
            GroupId = req.GroupId,
            MaxScore = req.MaxScore,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        db.Criteria.Add(entity);
        await db.SaveChangesAsync(ct);

        return Ok(ApiResponse.Ok(await ToDtoAsync(entity.Id, ct), HttpContext.TraceIdentifier));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCriteriaRequest req, CancellationToken ct)
    {
        var entity = await db.Criteria.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw ApiException.NotFound("CRITERIA_NOT_FOUND", "Không tìm thấy chỉ tiêu.");

        await ValidateAsync(req.Code, req.Name, req.GroupId, req.MaxScore, excludeId: id, ct);

        entity.Code = req.Code.Trim();
        entity.Name = req.Name.Trim();
        entity.GroupId = req.GroupId;
        entity.MaxScore = req.MaxScore;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        return Ok(ApiResponse.Ok(await ToDtoAsync(entity.Id, ct), HttpContext.TraceIdentifier));
    }

    /// <summary>
    /// Hard-delete nếu Criteria chưa từng có CriteriaAssessment nào (kể cả đã soft-delete);
    /// ngược lại chỉ soft-delete — xem spec/danh-muc-dti/business-rules.md mục 1.3.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var entity = await db.Criteria.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw ApiException.NotFound("CRITERIA_NOT_FOUND", "Không tìm thấy chỉ tiêu.");

        var hasHistory = await db.CriteriaAssessments
            .IgnoreQueryFilters()
            .AnyAsync(a => a.CriteriaId == id, ct);

        bool hardDeleted;
        if (hasHistory)
        {
            entity.IsDeleted = true;
            entity.UpdatedAt = DateTimeOffset.UtcNow;
            hardDeleted = false;
        }
        else
        {
            db.Criteria.Remove(entity);
            hardDeleted = true;
        }
        await db.SaveChangesAsync(ct);

        return Ok(ApiResponse.Ok(new { HardDeleted = hardDeleted }, HttpContext.TraceIdentifier));
    }

    private async Task ValidateAsync(string code, string name, Guid groupId, decimal maxScore, Guid? excludeId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length > 20)
            throw ApiException.BadRequest("CRITERIA_CODE_INVALID", "Mã chỉ tiêu bắt buộc, tối đa 20 ký tự.");
        if (string.IsNullOrWhiteSpace(name))
            throw ApiException.BadRequest("CRITERIA_NAME_REQUIRED", "Tên chỉ tiêu không được để trống.");
        if (maxScore <= 0)
            throw ApiException.BadRequest("CRITERIA_MAX_SCORE_INVALID", "Điểm tối đa phải > 0.");

        var groupExists = await db.CriteriaGroups.AnyAsync(g => g.Id == groupId, ct);
        if (!groupExists)
            throw ApiException.NotFound("CRITERIA_GROUP_NOT_FOUND", "Nhóm chỉ tiêu không tồn tại hoặc đã bị xoá.");

        var duplicate = await db.Criteria.AnyAsync(
            c => c.Code == code.Trim() && (excludeId == null || c.Id != excludeId), ct);
        if (duplicate)
            throw ApiException.Conflict("CRITERIA_CODE_DUPLICATE", $"Mã chỉ tiêu '{code}' đã tồn tại.");
    }

    private async Task<CriteriaDto> ToDtoAsync(Guid id, CancellationToken ct)
    {
        var c = await db.Criteria.Include(x => x.Group).FirstAsync(x => x.Id == id, ct);
        return new CriteriaDto
        {
            Id = c.Id,
            Code = c.Code,
            Name = c.Name,
            GroupId = c.GroupId,
            GroupName = c.Group?.Name ?? "",
            MaxScore = c.MaxScore,
        };
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlatformManager.Api.Common;
using PlatformManager.Api.Data;
using PlatformManager.Api.Dtos;
using PlatformManager.Api.Services;

namespace PlatformManager.Api.Controllers;

[ApiController]
[Route("api/criteria")]
public class AssessmentsController(AppDbContext db, AssessmentUpsertService upsertService) : ControllerBase
{
    /// <summary>
    /// Sửa Tiến độ %/Ghi chú — LUÔN ghi vào bản ghi của HÔM NAY (upsert-trong-ngày +
    /// copy-forward), bất kể client đang "xem" gì. `year`/`period` là query param
    /// TUỲ CHỌN, chỉ dùng làm defense-in-depth: nếu client gửi kèm ngữ cảnh đang xem
    /// và ngữ cảnh đó KHÔNG phải trạng thái "Live" (năm hiện tại + period=all), trả
    /// 409 — vì FE chỉ nên hiện control sửa khi isEditable=true (đã trả sẵn ở
    /// GET /api/criteria), đây chỉ là lớp phòng thủ phía server.
    /// Xem spec/danh-muc-dti/business-rules.md mục 2.4.
    /// </summary>
    [HttpPut("{id:guid}/assessment")]
    public async Task<IActionResult> UpsertAssessment(
        Guid id,
        [FromBody] UpsertAssessmentRequest req,
        [FromQuery] int? year,
        [FromQuery] string? period,
        CancellationToken ct)
    {
        var exists = await db.Criteria.AnyAsync(c => c.Id == id, ct);
        if (!exists)
            throw ApiException.NotFound("CRITERIA_NOT_FOUND", "Không tìm thấy chỉ tiêu.");

        if (year.HasValue)
        {
            var isCurrentYear = year.Value == DateTimeOffset.UtcNow.Year;
            var isAll = string.IsNullOrWhiteSpace(period) || period == "all";
            if (!isCurrentYear || !isAll)
                throw ApiException.Conflict(
                    "CRITERIA_ASSESSMENT_READONLY_PERIOD",
                    "Đang xem dữ liệu lịch sử — chỉ có thể sửa khi xem 'Tất cả' của năm hiện tại.");
        }

        var clampedProgress = req.ProgressPercent.HasValue ? Math.Clamp(req.ProgressPercent.Value, 0, 100) : (decimal?)null;

        var assessment = await upsertService.UpsertTodayAsync(id, a =>
        {
            if (clampedProgress.HasValue) a.ProgressPercent = clampedProgress.Value;
            if (req.Note is not null) a.Note = req.Note;
        }, ct);

        return Ok(ApiResponse.Ok(new
        {
            CriteriaId = id,
            assessment.ProgressPercent,
            assessment.Note,
            CreatedAt = DateOnly.FromDateTime(assessment.CreatedAt.UtcDateTime),
        }, HttpContext.TraceIdentifier));
    }
}

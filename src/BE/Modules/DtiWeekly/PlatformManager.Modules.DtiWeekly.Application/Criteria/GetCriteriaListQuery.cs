using MediatR;
using PlatformManager.Modules.DtiWeekly.Application.Assessments;
using PlatformManager.Modules.DtiWeekly.Application.Common;
using PlatformManager.Core.Application.Common.CQRS;
using PlatformManager.Core.Application.Common.Interfaces;
using PlatformManager.Core.Application.Common.Results;

namespace PlatformManager.Modules.DtiWeekly.Application.Criteria;

/// <summary>
/// GET /api/criteria — grid "Danh mục DTI". Year mặc định năm hiện tại, Period mặc định
/// "all". "Live" (năm hiện tại + all) = 1 dòng/Criteria active kèm bản ghi mới nhất (nếu
/// có) — editable. Mọi trạng thái khác = "lịch sử", liệt kê record thật trong khoảng thời
/// gian đã chọn (có thể nhiều dòng/1 chỉ tiêu), read-only — xem
/// spec/danh-muc-dti/business-rules.md mục 2.4.
/// </summary>
public sealed record GetCriteriaListQuery(
    int? Year = null,
    string Period = PeriodRangeCalculator.PeriodAll,
    string? PeriodValue = null,
    int Page = 1,
    int PageSize = 100,
    string? SearchText = null) : IQuery<CriteriaGridResultDto>;

public sealed class GetCriteriaListHandler(
    ICriteriaAssessmentQueryRepository assessmentQueryRepo, IDateTimeProvider clock)
    : BaseResponse, IRequestHandler<GetCriteriaListQuery, IApiResult<CriteriaGridResultDto>>
{
    public async Task<IApiResult<CriteriaGridResultDto>> Handle(GetCriteriaListQuery query, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
        var year = query.Year ?? today.Year;
        var isLive = PeriodRangeCalculator.IsLive(year, query.Period, today);

        List<CriteriaGridRowDto> rows;
        if (isLive)
        {
            rows = await assessmentQueryRepo.GetLiveRowsAsync(ct);
        }
        else
        {
            var (start, endExclusive) = PeriodRangeCalculator.Resolve(year, query.Period, query.PeriodValue);
            rows = await assessmentQueryRepo.GetRecordsInRangeAsync(start, endExclusive, includeInactiveCriteria: true, ct);
        }

        if (!string.IsNullOrWhiteSpace(query.SearchText))
        {
            var term = query.SearchText.Trim();
            rows = rows
                .Where(r => r.CriteriaCode.Contains(term, StringComparison.OrdinalIgnoreCase)
                            || r.CriteriaName.Contains(term, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        rows = [.. rows.OrderBy(r => r.CriteriaCode).ThenBy(r => r.AssessmentDate)];

        var total = rows.Count;
        var page = Math.Max(query.Page, 1);
        var pageSize = query.PageSize <= 0 ? 100 : query.PageSize;
        var paged = rows.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return Ok(new CriteriaGridResultDto
        {
            IsLive = isLive,
            Items = paged,
            Total = total,
            Page = page,
            PageSize = pageSize,
        });
    }
}

using PlatformManager.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using PlatformManager.Modules.DtiWeekly.Application.Assessments;
using PlatformManager.Modules.DtiWeekly.Domain.Entities;

namespace PlatformManager.Modules.DtiWeekly.Infrastructure.Persistence.Repositories;

/// <summary>
/// Dataset ở quy mô hiện tại nhỏ (62 chỉ tiêu, cập nhật tối đa hàng ngày) — các truy vấn
/// "live"/"range" cố ý fetch rồi group/aggregate trong C# thay vì ép EF dịch GroupBy phức
/// tạp sang SQL, đổi lấy code rõ ràng/chắc chắn đúng hơn là tối ưu sớm không cần thiết.
/// </summary>
public sealed class CriteriaAssessmentRepository(PlatformManagerDbContext db)
    : ICriteriaAssessmentRepository, ICriteriaAssessmentQueryRepository
{
    public Task<CriteriaAssessment?> FindByCriteriaAndDateAsync(Guid criteriaId, DateOnly date, CancellationToken ct)
    {
        var start = ToUtcStart(date);
        var end = start.AddDays(1);
        return db.Set<CriteriaAssessment>()
            .Where(a => a.CriteriaId == criteriaId && a.DateCreate >= start && a.DateCreate < end)
            .FirstOrDefaultAsync(ct);
    }

    public Task<CriteriaAssessment?> FindLatestBeforeAsync(Guid criteriaId, DateOnly beforeDateExclusive, CancellationToken ct)
    {
        var boundary = ToUtcStart(beforeDateExclusive);
        return db.Set<CriteriaAssessment>()
            .Where(a => a.CriteriaId == criteriaId && a.DateCreate < boundary)
            .OrderByDescending(a => a.DateCreate)
            .FirstOrDefaultAsync(ct);
    }

    public async Task AddAsync(CriteriaAssessment assessment, CancellationToken ct)
        => await db.Set<CriteriaAssessment>().AddAsync(assessment, ct);

    public Task<bool> AnyEverForCriteriaAsync(Guid criteriaId, CancellationToken ct)
        => db.Set<CriteriaAssessment>().IgnoreQueryFilters().AnyAsync(a => a.CriteriaId == criteriaId, ct);

    public async Task<List<CriteriaGridRowDto>> GetLiveRowsAsync(CancellationToken ct)
    {
        var criteriaList = await db.Set<Criteria>().ToListAsync(ct); // active only — global query filter
        if (criteriaList.Count == 0)
            return [];

        var criteriaIds = criteriaList.Select(c => c.Id).ToList();
        var groups = await LoadGroupsAsync(criteriaList.Select(c => c.GroupId), ct);

        var allAssessments = await db.Set<CriteriaAssessment>()
            .Where(a => criteriaIds.Contains(a.CriteriaId))
            .ToListAsync(ct);

        var latestByCriteria = allAssessments
            .GroupBy(a => a.CriteriaId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(a => a.DateCreate).First());

        var owners = await LoadOwnersAsync(latestByCriteria.Values.Select(a => a.OwnerId), ct);
        var evidenceLookup = await LoadEvidenceAsync(latestByCriteria.Values.Select(a => a.Id), ct);

        return [.. criteriaList.Select(criteria =>
        {
            latestByCriteria.TryGetValue(criteria.Id, out var latest);
            return ToRowDto(latest, criteria, groups, owners, evidenceLookup);
        })];
    }

    public async Task<List<CriteriaGridRowDto>> GetRecordsInRangeAsync(
        DateOnly startInclusive, DateOnly endExclusive, bool includeInactiveCriteria, CancellationToken ct)
    {
        var startUtc = ToUtcStart(startInclusive);
        var endUtc = ToUtcStart(endExclusive);

        var assessments = await db.Set<CriteriaAssessment>()
            .Where(a => a.DateCreate >= startUtc && a.DateCreate < endUtc)
            .ToListAsync(ct);

        if (assessments.Count == 0)
            return [];

        var criteriaIds = assessments.Select(a => a.CriteriaId).Distinct().ToList();

        IQueryable<Domain.Entities.Criteria> criteriaQuery = db.Set<Criteria>();
        if (includeInactiveCriteria)
            criteriaQuery = criteriaQuery.IgnoreQueryFilters();

        var criteriaById = await criteriaQuery
            .Where(c => criteriaIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, ct);

        var groups = await LoadGroupsAsync(criteriaById.Values.Select(c => c.GroupId), ct);
        var owners = await LoadOwnersAsync(assessments.Select(a => a.OwnerId), ct);
        var evidenceLookup = await LoadEvidenceAsync(assessments.Select(a => a.Id), ct);

        var rows = new List<CriteriaGridRowDto>();
        foreach (var assessment in assessments)
        {
            // Criteria không active và includeInactiveCriteria=false -> loại khỏi kết quả
            // (đây chính là cơ chế "mẫu số dùng danh mục hiện tại" cho AggregationService).
            if (!criteriaById.TryGetValue(assessment.CriteriaId, out var criteria))
                continue;

            rows.Add(ToRowDto(assessment, criteria, groups, owners, evidenceLookup));
        }

        return rows;
    }

    public async Task<List<DateOnly>> GetAllDistinctAssessmentDatesAsync(CancellationToken ct)
    {
        var timestamps = await db.Set<CriteriaAssessment>().Select(a => a.DateCreate).ToListAsync(ct);
        return [.. timestamps
            .Where(t => t.HasValue)
            .Select(t => DateOnly.FromDateTime(t!.Value.UtcDateTime))
            .Distinct()
            .OrderBy(d => d)];
    }

    private static DateTimeOffset ToUtcStart(DateOnly date) => new(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

    private async Task<Dictionary<Guid, CriteriaGroup>> LoadGroupsAsync(IEnumerable<Guid> groupIds, CancellationToken ct)
    {
        var ids = groupIds.Distinct().ToList();
        return ids.Count == 0
            ? []
            : await db.Set<CriteriaGroup>().IgnoreQueryFilters().Where(g => ids.Contains(g.Id)).ToDictionaryAsync(g => g.Id, ct);
    }

    private async Task<Dictionary<Guid, string>> LoadOwnersAsync(IEnumerable<Guid?> ownerIds, CancellationToken ct)
    {
        var ids = ownerIds.Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
        return ids.Count == 0
            ? []
            : await db.Users.Where(u => ids.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.FullName, ct);
    }

    private async Task<Dictionary<Guid, IReadOnlyList<string>>> LoadEvidenceAsync(IEnumerable<Guid> assessmentIds, CancellationToken ct)
    {
        var ids = assessmentIds.Distinct().ToList();
        if (ids.Count == 0)
            return [];

        var evidenceRows = await db.Set<CriteriaEvidence>()
            .Where(e => ids.Contains(e.CriteriaAssessmentId))
            .OrderBy(e => e.OrderIndex)
            .ToListAsync(ct);

        return evidenceRows
            .GroupBy(e => e.CriteriaAssessmentId)
            .ToDictionary(g => g.Key, IReadOnlyList<string> (g) => [.. g.Select(e => e.Content)]);
    }

    private static CriteriaGridRowDto ToRowDto(
        CriteriaAssessment? assessment,
        Domain.Entities.Criteria criteria,
        IReadOnlyDictionary<Guid, CriteriaGroup> groups,
        IReadOnlyDictionary<Guid, string> owners,
        IReadOnlyDictionary<Guid, IReadOnlyList<string>> evidenceLookup)
    {
        var groupName = groups.TryGetValue(criteria.GroupId, out var group) ? group.Name : "?";
        var ownerName = assessment?.OwnerId is { } ownerId && owners.TryGetValue(ownerId, out var name) ? name : null;
        var evidence = assessment is not null && evidenceLookup.TryGetValue(assessment.Id, out var ev)
            ? ev
            : (IReadOnlyList<string>)[];

        return new CriteriaGridRowDto(
            criteria.Id,
            criteria.Code,
            criteria.Name,
            criteria.GroupId,
            groupName,
            criteria.MaxScore,
            criteria.IsDelete,
            assessment?.Id,
            assessment?.DateCreate is { } dc ? DateOnly.FromDateTime(dc.UtcDateTime) : null,
            assessment?.ProgressPercent,
            assessment?.SelfScore,
            assessment?.VerifiedScore,
            assessment?.Status,
            assessment?.OwnerId,
            ownerName,
            assessment?.Deadline,
            assessment?.Note,
            evidence);
    }
}

using PlatformManager.Business.Application.Common;
using PlatformManager.Business.Application.Criteria;
using PlatformManager.Business.Domain.Entities;
using CriteriaEntity = PlatformManager.Business.Domain.Entities.Criteria;

namespace PlatformManager.Business.UnitTests.Application;

/// <summary>
/// Kho dữ liệu trong bộ nhớ cho ĐƯỜNG GHI TAY (DM-3…DM-6).
///
/// <para><b>Fake viết tay chứ không NSubstitute</b>, cùng lý do với
/// <see cref="FakeCriteriaImportRepository"/>: interface này có TRẠNG THÁI — nó phải nhớ thứ vừa
/// được thêm vào rồi trả lại đúng thứ đó ở lời gọi sau. Đúng bất biến đó là thứ hai test quan
/// trọng nhất của DM-6 treo vào (nhiều <c>PUT</c> vào cùng một kỳ KHÔNG sinh bản ghi thứ hai), nên
/// nó phải sống trong fake chứ không nằm rải ở từng ca kiểm.</para>
/// </summary>
internal sealed class FakeCriteriaWriteRepository : ICriteriaWriteRepository
{
    public List<CriteriaEntity> Criteria { get; } = [];

    public List<CriteriaAssessment> Assessments { get; } = [];

    public List<CriteriaGroup> Groups { get; } = [];

    public HashSet<Guid> Users { get; } = [];

    public List<CriteriaEntity> Removed { get; } = [];

    public int SavedAssessmentCount => Assessments.Count;

    public Task<CriteriaEntity?> GetTrackedAsync(Guid criteriaId, CancellationToken ct) =>
        Task.FromResult(Alive().FirstOrDefault(criteria => criteria.Id == criteriaId));

    public Task<CriteriaEntity?> FindByCodeAsync(string code, CancellationToken ct) =>
        Task.FromResult(Alive().FirstOrDefault(criteria =>
            string.Equals(criteria.Code, code, StringComparison.Ordinal)));

    public Task<bool> GroupExistsAsync(Guid groupId, CancellationToken ct) =>
        Task.FromResult(Groups.Any(group => group.Id == groupId));

    public Task<string?> FindGroupNameAsync(Guid groupId, CancellationToken ct) =>
        Task.FromResult(Groups.FirstOrDefault(group => group.Id == groupId)?.Name);

    public Task<bool> OwnerExistsAsync(Guid ownerId, CancellationToken ct) =>
        Task.FromResult(Users.Contains(ownerId));

    public Task<CriteriaAssessment?> FindAssessmentInRangeAsync(
        Guid criteriaId, DateOnly from, DateOnly to, CancellationToken ct) =>
        Task.FromResult(Assessments
            .Where(a => a.CriteriaId == criteriaId && a.AssessmentDate >= from && a.AssessmentDate <= to)
            .OrderByDescending(a => a.AssessmentDate)
            .FirstOrDefault());

    public Task<CriteriaAssessment?> FindLatestAssessmentBeforeAsync(
        Guid criteriaId, DateOnly before, CancellationToken ct) =>
        Task.FromResult(Assessments
            .Where(a => a.CriteriaId == criteriaId && a.AssessmentDate < before)
            .OrderByDescending(a => a.AssessmentDate)
            .FirstOrDefault());

    /// <summary>Đếm CẢ bản ghi đã xoá mềm — xem docstring của interface.</summary>
    public Task<bool> HasAnyAssessmentAsync(Guid criteriaId, CancellationToken ct) =>
        Task.FromResult(Assessments.Any(a => a.CriteriaId == criteriaId));

    public void Add(CriteriaEntity criteria) => Criteria.Add(criteria);

    public void AddAssessment(CriteriaAssessment assessment) => Assessments.Add(assessment);

    public void Remove(CriteriaEntity criteria)
    {
        Criteria.Remove(criteria);
        Removed.Add(criteria);
    }

    public Task<CriteriaRowDto?> FindRowAsync(Guid criteriaId, PeriodRange period, CancellationToken ct)
    {
        var criteria = Alive().FirstOrDefault(item => item.Id == criteriaId);
        if (criteria is null)
            return Task.FromResult<CriteriaRowDto?>(null);

        var group = Groups.First(item => item.Id == criteria.GroupId);

        var assessment = Assessments
            .Where(a => a.CriteriaId == criteriaId
                        && a.AssessmentDate >= period.Start && a.AssessmentDate <= period.End)
            .OrderByDescending(a => a.AssessmentDate)
            .FirstOrDefault();

        var rowPeriod = assessment is null ? null : PeriodRange.IsoWeekOf(assessment.AssessmentDate);

        return Task.FromResult<CriteriaRowDto?>(new CriteriaRowDto(
            criteria.Id, criteria.Code, criteria.Name, group.Id, group.Code, group.Name, criteria.MaxScore,
            assessment?.Id,
            assessment?.AssessmentDate,
            assessment?.ProgressPercent,
            assessment?.SelfScore,
            assessment?.VerifiedScore,
            null,
            assessment?.Status,
            assessment?.OwnerId,
            null,
            assessment?.Deadline,
            assessment?.Note,
            null,
            rowPeriod?.Value,
            rowPeriod is null ? null : PeriodLabels.Full(rowPeriod)));
    }

    private IEnumerable<CriteriaEntity> Alive() => Criteria.Where(criteria => !criteria.IsDeleted);
}

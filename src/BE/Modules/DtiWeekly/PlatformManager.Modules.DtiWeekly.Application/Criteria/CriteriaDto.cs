namespace PlatformManager.Modules.DtiWeekly.Application.Criteria;

public sealed record CriteriaDto(Guid Id, string Code, string Name, Guid GroupId, string GroupName, decimal MaxScore);

/// <summary>Hình chiếu gọn của Criteria — dùng làm "activeCriteria" cho AggregationService
/// (Application/Dashboard/AggregationService.cs), không cần Code/Name.</summary>
public sealed record CriteriaSummaryDto(Guid Id, Guid GroupId, decimal MaxScore);

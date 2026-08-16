namespace PlatformManager.Modules.DtiWeekly.Application.Assessments;

/// <summary>
/// 1 dòng grid "Danh mục DTI" — GET /api/criteria. Ở chế độ Live (AssessmentId có thể
/// null khi chỉ tiêu chưa từng được đánh giá), ở chế độ lịch sử luôn ứng với 1
/// CriteriaAssessment record có thật (AssessmentDate luôn có giá trị). Cũng được
/// AggregationService (Application/Dashboard) tái dùng làm input thô cho công thức tổng
/// hợp — 1 số field (OwnerName/Evidence) không dùng tới ở đó.
/// </summary>
public sealed record CriteriaGridRowDto(
    Guid CriteriaId,
    string CriteriaCode,
    string CriteriaName,
    Guid GroupId,
    string GroupName,
    decimal MaxScore,
    bool CriteriaIsDeleted,
    Guid? AssessmentId,
    DateOnly? AssessmentDate,
    decimal? ProgressPercent,
    decimal? SelfScore,
    decimal? VerifiedScore,
    string? Status,
    Guid? OwnerId,
    string? OwnerName,
    DateOnly? Deadline,
    string? Note,
    IReadOnlyList<string> Evidence);

public sealed class CriteriaGridResultDto
{
    /// <summary>True khi đang ở "Tất cả" của năm hiện tại (chưa thu hẹp tuần/tháng) —
    /// duy nhất trạng thái cho phép sửa (xem spec/danh-muc-dti/business-rules.md mục 2.4).</summary>
    public bool IsLive { get; init; }
    public IReadOnlyList<CriteriaGridRowDto> Items { get; init; } = [];
    public int Total { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}

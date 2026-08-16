namespace PlatformManager.Modules.DtiWeekly.Application.Assessments;

public sealed record CriteriaAssessmentDto(
    Guid Id,
    Guid CriteriaId,
    decimal ProgressPercent,
    decimal? SelfScore,
    decimal? VerifiedScore,
    string? Status,
    Guid? OwnerId,
    DateOnly? Deadline,
    string? Note,
    DateOnly AssessmentDate);

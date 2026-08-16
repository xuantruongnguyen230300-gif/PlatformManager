namespace PlatformManager.Modules.DtiWeekly.Application.Assessments;

public sealed record ImportRowErrorDto(int RowNumber, string? Code, string Message);

public sealed class ImportCsvResultDto
{
    public int TotalRows { get; init; }
    public int SuccessCount { get; init; }
    public int ErrorCount { get; init; }
    public int CriteriaCreatedCount { get; init; }
    public int GroupsCreatedCount { get; init; }
    public int OwnersCreatedCount { get; init; }
    public IReadOnlyList<ImportRowErrorDto> Errors { get; init; } = [];
}

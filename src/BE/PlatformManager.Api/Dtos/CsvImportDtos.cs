namespace PlatformManager.Api.Dtos;

public class CsvImportResultDto
{
    public int TotalRows { get; set; }
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
    public int CriteriaCreatedCount { get; set; }
    public int GroupsCreatedCount { get; set; }
    public int OwnersCreatedCount { get; set; }
    public List<CsvImportRowErrorDto> Errors { get; set; } = [];
}

public class CsvImportRowErrorDto
{
    public int RowNumber { get; set; }
    public string? Code { get; set; }
    public string Message { get; set; } = string.Empty;
}

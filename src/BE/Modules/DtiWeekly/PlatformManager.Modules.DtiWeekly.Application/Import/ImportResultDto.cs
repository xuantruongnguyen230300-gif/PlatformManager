namespace PlatformManager.Modules.DtiWeekly.Application.Import;

public sealed record ImportRowErrorDto(int RowNumber, string? Code, string Message);

/// <summary>
/// Kết quả 1 lượt import — serialize JSON lưu vào ImportJob.ResultJson khi Status = Succeeded,
/// trả nguyên vẹn qua GetImportJobStatusQuery (field "result" ở CONTRACT DM-7). Field giữ
/// nguyên so với ImportCsvResultDto cũ (trước khi tách job nền) — chỉ đổi tên cho đúng bản chất
/// format-agnostic (CSV lẫn Excel), không đổi/bớt field nào.
/// </summary>
public sealed class ImportResultDto
{
    public int TotalRows { get; init; }
    public int SuccessCount { get; init; }
    public int ErrorCount { get; init; }
    public int CriteriaCreatedCount { get; init; }
    public int GroupsCreatedCount { get; init; }
    public int OwnersCreatedCount { get; init; }
    public IReadOnlyList<ImportRowErrorDto> Errors { get; init; } = [];
}

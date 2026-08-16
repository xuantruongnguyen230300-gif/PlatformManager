namespace PlatformManager.Modules.DtiWeekly.Application.Assessments;

public interface ICsvImportService
{
    /// <summary>Đọc CSV tiếng Việt theo mẫu doc/ERD/example_db_ver1.csv, ghi qua
    /// AssessmentUpsertService — xem ImportCsvResultDto cho hình dạng kết quả.</summary>
    Task<ImportCsvResultDto> ImportAsync(Stream csvStream, CancellationToken ct);
}

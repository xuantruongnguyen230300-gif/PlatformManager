using PlatformManager.Core.Domain.Common;

namespace PlatformManager.Modules.DtiWeekly.Domain.Entities;

public enum ImportFileFormat { Csv, Xlsx, Xls }

public enum ImportJobStatus { Pending, Running, Succeeded, Failed }

/// <summary>
/// 1 lượt import CSV/Excel chạy NỀN qua Hangfire — xem CONTRACT DM-7
/// (doc/contracts/danh-muc-dti.md) + .claude/rules/cqrs-handler.md §"Command chạy lâu → job
/// nền". FE poll GET /api/import/{jobId} (GetImportJobStatusQuery) tới khi Status chuyển
/// Succeeded/Failed. Khác CriteriaAssessment/Criteria — entity này THUẦN kỹ thuật (theo dõi
/// tiến trình 1 tác vụ nền), không có ý nghĩa nghiệp vụ DTI riêng.
/// </summary>
public class ImportJob : BaseEntity
{
    public string FileName { get; private set; } = string.Empty;
    public ImportFileFormat Format { get; private set; }

    /// <summary>Đường dẫn tuyệt đối do IImportFileStorage.SaveAsync trả về — ImportJobRunner
    /// (chạy trong Hangfire worker, không còn Stream gốc của request HTTP) dùng lại để
    /// OpenReadAsync. Gán SAU Create() vì path phụ thuộc Id vừa sinh
    /// (App_Data/imports/{jobId}{extension}) — xem StartImportCommandHandler.</summary>
    public string StoragePath { get; private set; } = string.Empty;

    public ImportJobStatus Status { get; private set; } = ImportJobStatus.Pending;

    /// <summary>JSON của ImportResultDto — chỉ có giá trị khi Status = Succeeded.</summary>
    public string? ResultJson { get; private set; }

    /// <summary>Chỉ có giá trị khi Status = Failed — lỗi hạ tầng (job crash), KHÁC lỗi từng
    /// dòng (nằm trong ResultJson.Errors).</summary>
    public string? ErrorMessage { get; private set; }

    private ImportJob() { }

    public static ImportJob Create(string fileName, ImportFileFormat format)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new DomainException("IMPORT_JOB_FILE_NAME_REQUIRED", "Tên file không được để trống.");

        return new ImportJob
        {
            Id = EntityId.New(),
            FileName = fileName,
            Format = format,
            Status = ImportJobStatus.Pending,
        };
    }

    public void SetStoragePath(string storagePath)
    {
        if (string.IsNullOrWhiteSpace(storagePath))
            throw new DomainException("IMPORT_JOB_STORAGE_PATH_REQUIRED", "Đường dẫn lưu file không được để trống.");

        StoragePath = storagePath;
    }

    public void MarkRunning() => Status = ImportJobStatus.Running;

    public void MarkSucceeded(string resultJson)
    {
        Status = ImportJobStatus.Succeeded;
        ResultJson = resultJson;
        ErrorMessage = null;
    }

    public void MarkFailed(string errorMessage)
    {
        Status = ImportJobStatus.Failed;
        ErrorMessage = errorMessage;
    }
}

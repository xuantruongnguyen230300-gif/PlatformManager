namespace PlatformManager.Modules.DtiWeekly.Application.Import;

/// <summary>
/// Lưu file upload TRƯỚC khi enqueue Hangfire job — IFormFile/Stream KHÔNG sống sót qua ranh
/// giới request→job nền (xem .claude/rules/cqrs-handler.md §"Command chạy lâu → job nền").
/// Impl LocalImportFileStorage (Modules.DtiWeekly.Infrastructure) lưu vào
/// App_Data/imports/{jobId}{extension} — cần IWebHostEnvironment (ASP.NET Core) nên KHÔNG đặt
/// implementation ở Application.
/// </summary>
public interface IImportFileStorage
{
    /// <summary>Lưu nội dung file, trả về path (tuyệt đối) để ImportJob.StoragePath lưu lại —
    /// jobId cần truyền vào vì đường dẫn lưu file phụ thuộc jobId (App_Data/imports/{jobId}
    /// {extension}), không phải theo fileName gốc (tránh trùng tên khi 2 user cùng upload file
    /// tên giống nhau).</summary>
    Task<string> SaveAsync(Guid jobId, Stream content, string fileName, CancellationToken ct);

    /// <summary>Mở lại file đã lưu — path chính là giá trị trả về từ SaveAsync, đọc lại từ
    /// ImportJob.StoragePath khi Hangfire worker chạy ImportJobRunner.</summary>
    Task<Stream> OpenReadAsync(string path, CancellationToken ct);
}

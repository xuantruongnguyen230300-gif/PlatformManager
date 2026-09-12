using PlatformManager.Business.Domain.Entities;

namespace PlatformManager.Business.Application.Import;

/// <summary>
/// Trạng thái một lượt nạp, đọc cho endpoint poll. Bản ghi <c>ImportJob</c> mang
/// <c>ResultJson</c> dạng CHUỖI; việc dựng lại nó thành <see cref="ImportResultDto"/> thuộc
/// handler, không thuộc repository — repository không biết khuôn JSON nào.
/// </summary>
public sealed record ImportJobStatusSnapshot(
    string Status, string? ResultJson, string? ErrorMessage, string? ErrorCode);

/// <summary>
/// Đọc/ghi bảng theo dõi lượt nạp (<c>business."ImportJobs"</c>).
///
/// <para><b>KHÔNG có <c>SaveChanges</c> ở đây</b> — handler own <c>SaveChanges</c>, đúng một lần,
/// ở cuối (doc/huong_dan/quy-uoc/be-cqrs-handler.md §Handler). Với đường import thì luật đó còn
/// mang thêm một nghĩa cứng hơn: Q64 đòi CẢ LƯỢT NẠP chạy trong MỘT giao dịch, và một repository
/// tự lưu sẽ cắt giao dịch đó thành nhiều mảnh mà không ai thấy.</para>
/// </summary>
public interface IImportJobRepository
{
    void Add(ImportJob job);

    /// <summary>Bản ghi job ở dạng THEO DÕI ĐƯỢC (để sửa trạng thái rồi lưu).</summary>
    Task<ImportJob?> GetTrackedAsync(Guid jobId, CancellationToken ct);

    /// <summary>Chỉ đọc — dùng cho endpoint poll, không cần change tracker.</summary>
    Task<ImportJobStatusSnapshot?> GetStatusAsync(Guid jobId, CancellationToken ct);
}

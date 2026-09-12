namespace PlatformManager.Business.Domain.Entities;

/// <summary>
/// 4 trạng thái của một lần import — spec/danh-muc-dti/business-rules.md §1.5,
/// doc/contracts/danh-muc-dti.md DM-7 bước 2. Lưu nguyên văn tiếng Anh (đây là trạng thái
/// TIẾN TRÌNH, không phải giá trị nghiệp vụ người dùng chọn — khác hẳn
/// <see cref="AssessmentStatuses"/>).
/// </summary>
public static class ImportJobStatuses
{
    public const string Pending = "Pending";
    public const string Running = "Running";
    public const string Succeeded = "Succeeded";
    public const string Failed = "Failed";

    public static readonly string[] All = [Pending, Running, Succeeded, Failed];
}

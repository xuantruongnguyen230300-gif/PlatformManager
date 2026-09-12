using PlatformManager.Core.Domain.Common;

namespace PlatformManager.Business.Domain.Entities;

/// <summary>Catalog lỗi bất biến của <see cref="ImportJob"/>.</summary>
public static class ImportJobDomainErrors
{
    public static readonly DomainError FileNameRequired = new(
        "IMPORT_JOB.FILE_NAME_REQUIRED", "Tên file không được để trống.");

    public static readonly DomainError StoragePathRequired = new(
        "IMPORT_JOB.STORAGE_PATH_REQUIRED", "Đường dẫn file tạm không được để trống.");

    /// <summary>
    /// <c>TargetWeekEnd</c> phải là CHỦ NHẬT — nó là mốc kết thúc của một tuần ISO. Lưới an toàn
    /// thứ hai là <c>CHECK (EXTRACT(ISODOW FROM "TargetWeekEnd") = 7)</c> ở tầng DB (§1.4).
    /// </summary>
    public static readonly DomainError TargetWeekEndNotSunday = new(
        "IMPORT_JOB.TARGET_WEEK_END_INVALID", "Tuần đích phải neo vào ngày Chủ nhật của tuần ISO.");
}

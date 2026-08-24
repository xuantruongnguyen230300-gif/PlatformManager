namespace PlatformManager.Modules.DtiWeekly.Application.Import;

/// <summary>Cờ báo có tạo mới Criteria/CriteriaGroup/AppUser (owner) trong lúc xử lý dòng này
/// hay không — ImportJobRunner cộng dồn để build ImportResultDto.*CreatedCount.</summary>
public sealed record ImportRowOutcome(bool CriteriaCreated, bool GroupCreated, bool OwnerCreated);

/// <summary>
/// Lõi xử lý ĐÚNG 1 dòng import (resolve/create Criteria, upsert CriteriaAssessment hôm nay,
/// ghi đè CriteriaEvidence) — giữ NGUYÊN VẸN business rule từ CsvImportService.ImportAsync gốc,
/// chỉ đổi input từ đọc trực tiếp CsvReader sang đọc IReadOnlyDictionary&lt;string,string?&gt;
/// đã chuẩn hoá (đọc bởi IImportFileReader, bất kể CSV/Excel). KHÔNG tự SaveChanges, KHÔNG tự
/// bắt lỗi — caller (ImportJobRunner) own SaveChanges + catch lỗi từng dòng đúng như hành vi cũ.
/// </summary>
public interface IImportRowProcessor
{
    Task<ImportRowOutcome> ProcessRowAsync(IReadOnlyDictionary<string, string?> row, CancellationToken ct);
}

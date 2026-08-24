namespace PlatformManager.Modules.DtiWeekly.Application.Import;

/// <summary>Tên cột CSV/Excel theo mẫu doc/ERD/example_db_ver1.csv — dùng chung giữa
/// ImportJobRunner (chỉ cần đọc "Mã" để gắn vào ImportRowErrorDto.Code khi 1 dòng lỗi) và
/// ImportRowProcessor (đọc đủ 10 cột để resolve/tạo Criteria + upsert CriteriaAssessment).
/// Giữ NGUYÊN VẸN tên cột từ CsvImportService cũ.</summary>
internal static class ImportColumnNames
{
    public const string Code = "Mã";
    public const string Name = "Chỉ tiêu";
    public const string Group = "Nhóm";
    public const string MaxScore = "Điểm tối đa";
    public const string SelfScore = "Tự đánh giá";
    public const string VerifiedScore = "Thẩm định";
    public const string Status = "Trạng thái";
    public const string Owner = "Phụ trách";
    public const string Deadline = "Hạn xử lý";
    public const string Evidence = "Minh chứng/Ghi chú";
}

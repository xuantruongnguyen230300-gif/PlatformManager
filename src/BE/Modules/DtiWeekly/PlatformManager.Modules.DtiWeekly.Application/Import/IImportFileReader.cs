namespace PlatformManager.Modules.DtiWeekly.Application.Import;

/// <summary>
/// Đọc 1 file import (CSV/Excel) thành dòng dữ liệu TRUNG TÍNH định dạng — key = tên cột
/// (header dòng 1), value = nội dung ô/field. Cả CSV lẫn Excel đều dùng chung 10 cột theo mẫu
/// doc/ERD/example_db_ver1.csv — IImportRowProcessor không cần biết file gốc là định dạng nào.
///
/// 2 impl: CsvFileReader (project này, bọc lại CsvHelper — logic PORT NGUYÊN VẸN từ
/// CsvImportService cũ), ExcelFileReader (Modules.DtiWeekly.Infrastructure, dùng NPOI — package
/// NPOI chỉ thêm ở Infrastructure.csproj, KHÔNG kéo vào Application). Xem CONTRACT DM-7
/// (doc/contracts/danh-muc-dti.md) + .claude/rules/cqrs-handler.md §"Command chạy lâu → job nền".
/// </summary>
public interface IImportFileReader
{
    /// <summary>true nếu reader này đọc được file có tên fileName (xét theo phần mở rộng) —
    /// ImportJobRunner chọn đúng 1 reader trong tập IEnumerable&lt;IImportFileReader&gt; đã
    /// đăng ký bằng cách này, không cần switch theo enum ImportFileFormat.</summary>
    bool CanRead(string fileName);

    /// <summary>Đọc từng dòng dữ liệu (KHÔNG bao gồm dòng header) — ném exception nếu file lỗi
    /// định dạng/không đọc được, caller (ImportJobRunner) bắt ở tầng ngoài cùng, đánh dấu
    /// ImportJob.Status = Failed (khác lỗi từng dòng, được catch riêng theo từng phần tử yield
    /// ra từ đây).</summary>
    IAsyncEnumerable<IReadOnlyDictionary<string, string?>> ReadAsync(Stream stream, string fileName, CancellationToken ct);
}

namespace PlatformManager.Modules.DtiWeekly.Application.Assessments;

/// <summary>
/// Ranh giới đọc dạng lưới/tổng hợp — dùng bởi GetCriteriaListQuery (grid "Danh mục DTI") và
/// AggregationService (Dashboard). Tách khỏi <see cref="ICriteriaAssessmentRepository"/> (ghi/
/// kiểm tra tồn tại) theo ISP — xem comment ở interface đó. Cùng 1 class
/// (Infrastructure/Persistence/Repositories/CriteriaAssessmentRepository.cs) implement CẢ HAI.
/// </summary>
public interface ICriteriaAssessmentQueryRepository
{
    /// <summary>Chế độ Live — 1 dòng/Criteria ACTIVE, kèm bản ghi mới nhất (nếu có) bất kể
    /// ngày nào (không giới hạn theo kỳ).</summary>
    Task<List<CriteriaGridRowDto>> GetLiveRowsAsync(CancellationToken ct);

    /// <summary>Chế độ lịch sử/tổng hợp — liệt kê record thật có CreatedAt rơi vào
    /// [startInclusive, endExclusive). includeInactiveCriteria=true dùng
    /// IgnoreQueryFilters cho Criteria (vẫn thấy lịch sử của chỉ tiêu đã xoá mềm, dùng cho
    /// grid lịch sử); false = chỉ Criteria đang active (dùng cho AggregationService, mẫu số
    /// Σ MaxScore chỉ tính theo danh mục hiện tại).</summary>
    Task<List<CriteriaGridRowDto>> GetRecordsInRangeAsync(
        DateOnly startInclusive, DateOnly endExclusive, bool includeInactiveCriteria, CancellationToken ct);

    /// <summary>Toàn bộ ngày (phần DATE của CreatedAt) có ít nhất 1 record chưa xoá mềm —
    /// dùng để suy ra danh sách kỳ-tuần/tháng/năm có dữ liệu và tìm "kỳ liền trước" thuần
    /// trong C# (dataset nhỏ ở quy mô hiện tại, tránh cần date_trunc SQL thô).</summary>
    Task<List<DateOnly>> GetAllDistinctAssessmentDatesAsync(CancellationToken ct);
}

using PlatformManager.Modules.DtiWeekly.Domain.Entities;

namespace PlatformManager.Modules.DtiWeekly.Application.Assessments;

/// <summary>
/// Ranh giới ghi/kiểm tra tồn tại — dùng bởi AssessmentUpsertService (upsert-trong-ngày) và
/// DeleteCriteriaHandler (quyết định hard vs soft delete). Tách khỏi
/// <see cref="ICriteriaAssessmentQueryRepository"/> (đọc dạng lưới/tổng hợp, dùng bởi
/// GetCriteriaListQuery/AggregationService) theo ISP — 2 nhóm consumer này không giao nhau, tách
/// giúp mỗi consumer chỉ phụ thuộc đúng tập method mình cần (xem
/// .claude/rules/architecture.md §SOLID &amp; OOP). Cùng 1 class
/// (Infrastructure/Persistence/Repositories/CriteriaAssessmentRepository.cs) implement CẢ HAI —
/// không nhân bản logic, chỉ tách CONTRACT.
/// </summary>
public interface ICriteriaAssessmentRepository
{
    /// <summary>Record chưa xoá mềm của đúng CriteriaId có CreatedAt = date (bước 1 của
    /// quy tắc upsert-trong-ngày, xem doc/ERD/ERD.md mục "Kỳ (tuần/tháng/năm)").</summary>
    Task<CriteriaAssessment?> FindByCriteriaAndDateAsync(Guid criteriaId, DateOnly date, CancellationToken ct);

    /// <summary>Record gần nhất TRƯỚC beforeDateExclusive (CreatedAt lớn nhất, chưa xoá
    /// mềm) — dùng làm baseline copy-forward.</summary>
    Task<CriteriaAssessment?> FindLatestBeforeAsync(Guid criteriaId, DateOnly beforeDateExclusive, CancellationToken ct);

    Task AddAsync(CriteriaAssessment assessment, CancellationToken ct);

    /// <summary>Kể cả bản ghi đã xoá mềm — dùng để quyết định hard-delete vs soft-delete
    /// khi xoá Criteria (xem spec/danh-muc-dti/business-rules.md mục 1.3).</summary>
    Task<bool> AnyEverForCriteriaAsync(Guid criteriaId, CancellationToken ct);
}

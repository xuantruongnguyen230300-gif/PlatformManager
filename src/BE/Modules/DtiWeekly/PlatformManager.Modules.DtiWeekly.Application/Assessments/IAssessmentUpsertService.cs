using PlatformManager.Modules.DtiWeekly.Domain.Entities;

namespace PlatformManager.Modules.DtiWeekly.Application.Assessments;

/// <summary>
/// Lõi nghiệp vụ dùng chung cho CẢ sửa tay (UpdateCriteriaAssessmentCommand) lẫn Import CSV
/// (CsvImportService) — quy tắc "upsert-trong-ngày + copy-forward", xem doc/ERD/ERD.md mục
/// "Kỳ (tuần/tháng/năm) — khái niệm ngầm định".
/// </summary>
public interface IAssessmentUpsertService
{
    /// <summary>
    /// 1. Tìm record chưa xoá mềm của criteriaId có CreatedAt = hôm nay → nếu có, áp
    ///    <paramref name="apply"/> lên chính record đó (UPDATE, giữ nguyên CreatedAt).
    /// 2. Nếu chưa có → tìm record CreatedAt lớn nhất TRƯỚC hôm nay làm baseline, copy-forward
    ///    7 field nghiệp vụ, rồi áp <paramref name="apply"/> đè lên (mutation luôn thắng
    ///    baseline), INSERT record mới.
    /// 3. Nếu CHƯA từng có baseline nào VÀ apply có set SelfScore VÀ apply KHÔNG tự set
    ///    ProgressPercent → tự suy ra ProgressPercent = Clamp(SelfScore/MaxScore*100, 0, 100).
    /// KHÔNG tự SaveChanges — caller (handler) own SaveChanges đúng 1 lần ở cuối.
    /// </summary>
    Task<CriteriaAssessment> UpsertTodayAsync(Guid criteriaId, Action<CriteriaAssessment> apply, CancellationToken ct);
}

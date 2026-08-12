namespace PlatformManager.Api.Entities;

/// <summary>
/// 1 bản ghi đánh giá của 1 chỉ tiêu, gắn với 1 mốc thời gian (<see cref="BaseEntity.CreatedAt"/>).
/// Bảng trung tâm — KHÔNG còn AssessmentPeriod/PeriodId (đã bỏ, xem
/// doc/ERD/ERD.md mục "Kỳ (tuần/tháng/năm) — khái niệm ngầm định").
///
/// LƯU Ý QUAN TRỌNG: khác các bảng còn lại (nơi CreatedAt thuần audit),
/// CreatedAt của entity này VỪA là audit VỪA là field nghiệp vụ xác định
/// "kỳ" (phần ngày của CreatedAt = kỳ của record). Bất biến sau khi insert —
/// các lần lưu tiếp theo trong CÙNG 1 ngày UPDATE lại chính record đó (chỉ
/// đổi UpdatedAt + field nghiệp vụ), không insert record mới.
///
/// Unique index (CriteriaId, CAST(CreatedAt AS date)) filtered WHERE IsDeleted=false
/// được tạo bằng raw SQL trong migration (xem AppDbContext + migration đầu tiên).
/// </summary>
public class CriteriaAssessment : BaseEntity
{
    public Guid CriteriaId { get; set; }
    public decimal ProgressPercent { get; set; }          // 0..100, kẹp [0,100]
    public decimal? SelfScore { get; set; }
    public decimal? VerifiedScore { get; set; }
    public string? Status { get; set; }                    // "Chưa thực hiện"|"Đang thực hiện"|"Cần bổ sung minh chứng"|"Hoàn thành"
    public Guid? OwnerId { get; set; }
    public DateOnly? Deadline { get; set; }
    public string? Note { get; set; }

    public Criteria? Criteria { get; set; }
    public AppUser? Owner { get; set; }
    public ICollection<CriteriaEvidence> Evidences { get; set; } = new List<CriteriaEvidence>();
}

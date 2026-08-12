namespace PlatformManager.Api.Entities;

/// <summary>
/// Danh mục 62 chỉ tiêu chuyển đổi số — TĨNH, không đổi theo kỳ báo cáo.
/// Điểm số/tiến độ/trạng thái biến thiên theo kỳ nằm ở <see cref="CriteriaAssessment"/>.
/// Xem doc/ERD/ERD.md mục 2.
/// </summary>
public class Criteria : BaseEntity
{
    public string Code { get; set; } = string.Empty;      // vd "1.1", "4.22.1" — varchar(20)
    public string Name { get; set; } = string.Empty;      // text — có thể dài nhiều dòng
    public Guid GroupId { get; set; }
    public decimal MaxScore { get; set; }

    public CriteriaGroup? Group { get; set; }
    public ICollection<CriteriaAssessment> Assessments { get; set; } = new List<CriteriaAssessment>();
}

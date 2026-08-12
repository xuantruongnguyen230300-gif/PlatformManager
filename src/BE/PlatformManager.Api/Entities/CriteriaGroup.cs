namespace PlatformManager.Api.Entities;

/// <summary>
/// Danh mục 6 nhóm chỉ tiêu (tĩnh) — xem doc/ERD/ERD.md mục 1.
/// </summary>
public class CriteriaGroup : BaseEntity
{
    public string Code { get; set; } = string.Empty;   // "1".."6"
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }

    public ICollection<Criteria> Criteria { get; set; } = new List<Criteria>();
}

namespace PlatformManager.Business.Application.CriteriaGroups;

/// <summary>
/// Đọc danh mục nhóm chỉ tiêu. Hiện thực dùng EF sống ở <c>Business.Persistence</c> — tầng
/// Application chỉ biết interface này (ranh giới cưỡng chế bởi
/// <c>ApplicationLayerBoundaryTests</c>).
/// </summary>
public interface ICriteriaGroupRepository
{
    /// <summary>
    /// Mọi nhóm CHƯA xoá mềm, đã sắp theo <c>DisplayOrder</c> tăng dần — BE sắp, FE không sắp
    /// lại (DM-1).
    /// </summary>
    Task<IReadOnlyList<CriteriaGroupDto>> GetAllAsync(CancellationToken ct);
}

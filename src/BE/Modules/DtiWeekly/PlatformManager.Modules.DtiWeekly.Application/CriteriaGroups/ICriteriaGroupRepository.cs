using PlatformManager.Modules.DtiWeekly.Domain.Entities;

namespace PlatformManager.Modules.DtiWeekly.Application.CriteriaGroups;

public interface ICriteriaGroupRepository
{
    Task<List<CriteriaGroup>> GetAllAsync(CancellationToken ct);

    Task<CriteriaGroup?> GetByIdAsync(Guid id, CancellationToken ct);

    /// <summary>So khớp chính xác (trim, không phân biệt hoa/thường) — dùng khi Import CSV
    /// resolve tên nhóm, xem spec/danh-muc-dti/business-rules.md mục 2.2.</summary>
    Task<CriteriaGroup?> GetByNameAsync(string name, CancellationToken ct);

    Task AddAsync(CriteriaGroup group, CancellationToken ct);
}

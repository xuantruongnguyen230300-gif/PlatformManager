namespace PlatformManager.Modules.DtiWeekly.Application.Criteria;

public interface ICriteriaRepository
{
    Task<Domain.Entities.Criteria?> GetByIdAsync(Guid id, CancellationToken ct);

    /// <summary>Chỉ trong tập CHƯA xoá mềm — cho phép tái dùng Code sau khi đã xoá
    /// (xem spec/danh-muc-dti/business-rules.md mục 1.1).</summary>
    Task<bool> CodeExistsAsync(string code, Guid? excludeId, CancellationToken ct);

    /// <summary>So khớp Code trong tập chưa xoá mềm — dùng khi Import CSV resolve dòng
    /// theo Mã (xem spec/danh-muc-dti/business-rules.md mục 2.2).</summary>
    Task<Domain.Entities.Criteria?> GetByCodeAsync(string code, CancellationToken ct);

    Task<List<Domain.Entities.Criteria>> GetAllActiveAsync(CancellationToken ct);

    Task AddAsync(Domain.Entities.Criteria criteria, CancellationToken ct);

    void Remove(Domain.Entities.Criteria criteria);
}

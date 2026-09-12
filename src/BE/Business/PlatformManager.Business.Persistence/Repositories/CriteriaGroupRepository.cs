using Microsoft.EntityFrameworkCore;
using PlatformManager.Business.Application.CriteriaGroups;
using PlatformManager.Business.Domain.Entities;
using PlatformManager.Core.Infrastructure.Persistence;

namespace PlatformManager.Business.Persistence.Repositories;

/// <summary>Đọc danh mục nhóm chỉ tiêu (DM-1). Chỉ đọc — <c>AsNoTracking</c>.</summary>
internal sealed class CriteriaGroupRepository(PlatformManagerDbContext db) : ICriteriaGroupRepository
{
    public async Task<IReadOnlyList<CriteriaGroupDto>> GetAllAsync(CancellationToken ct) =>
        await db.Set<CriteriaGroup>().AsNoTracking()
            // BE sắp, FE không sắp lại (DM-1). Sắp phụ theo Code để thứ tự xác định nếu hai nhóm
            // lỡ mang cùng DisplayOrder — bảng seed §1.6 không cho phép, nhưng dữ liệu chạy thì
            // sửa được và một lưới đổi thứ tự giữa hai lần tải là lỗi không ai tái hiện nổi.
            .OrderBy(group => group.DisplayOrder)
            .ThenBy(group => group.Code)
            .Select(group => new CriteriaGroupDto(group.Id, group.Code, group.Name, group.DisplayOrder))
            .ToListAsync(ct);
}

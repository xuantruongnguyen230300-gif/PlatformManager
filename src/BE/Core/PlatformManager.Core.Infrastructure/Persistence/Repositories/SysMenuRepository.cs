using Microsoft.EntityFrameworkCore;
using PlatformManager.Core.Application.Menu;
using PlatformManager.Core.Domain.Entities;

namespace PlatformManager.Core.Infrastructure.Persistence.Repositories;

public sealed class SysMenuRepository(PlatformManagerDbContext db) : ISysMenuRepository
{
    /// <summary>
    /// Chỉ ĐỌC (dựng cây menu trả ra API) — AsNoTracking theo
    /// doc/huong_dan/quy-uoc/be-performance.md §"Khi viết repository/query mới". Không call-site
    /// nào sửa SysMenu lấy từ đây rồi SaveChanges; nếu về sau có, đường ghi đó phải dùng hàm
    /// RIÊNG, đừng bỏ AsNoTracking ở đây (bỏ đi là trả tracking cho MỌI đường đọc).
    /// </summary>
    public Task<List<SysMenu>> GetAllAsync(CancellationToken ct)
        => db.SysMenus.AsNoTracking().OrderBy(m => m.DisplayOrder).ToListAsync(ct);
}

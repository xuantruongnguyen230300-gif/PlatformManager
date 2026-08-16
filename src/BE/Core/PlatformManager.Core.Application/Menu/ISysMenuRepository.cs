using PlatformManager.Core.Domain.Entities;

namespace PlatformManager.Core.Application.Menu;

public interface ISysMenuRepository
{
    /// <summary>Toàn bộ SysMenu — dùng cho ma trận Phân quyền (mọi hàng, không lọc theo
    /// role).</summary>
    Task<List<SysMenu>> GetAllAsync(CancellationToken ct);
}

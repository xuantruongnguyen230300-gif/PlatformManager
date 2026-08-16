namespace PlatformManager.Core.Application.Menu;

/// <summary>
/// Application chỉ làm việc với TÊN role (string) — Infrastructure tự resolve sang
/// AppRole.Id cụ thể (Identity, Application không được biết kiểu này).
/// </summary>
public interface ISysMenuRoleRepository
{
    /// <summary>SysMenuId → danh sách tên role được gán. SysMenu không có key trong dict này
    /// = mở cho mọi user đã đăng nhập (quy ước đã CHỐT).</summary>
    Task<Dictionary<Guid, List<string>>> GetAssignedRoleNamesBySysMenuAsync(CancellationToken ct);

    /// <summary>Tên role hiện tại (vd của user đang đăng nhập) → danh sách SysMenuId mà
    /// user thấy được (mở cho mọi người HOẶC có ít nhất 1 role trùng khớp).</summary>
    Task<HashSet<Guid>> GetVisibleSysMenuIdsForRolesAsync(IReadOnlyCollection<string> roleNames, CancellationToken ct);

    /// <summary>Ghi đè TOÀN BỘ SysMenuRole theo ma trận gửi lên — UpdatePermissionMatrixCommand.</summary>
    Task ReplaceAllAsync(IReadOnlyDictionary<Guid, IReadOnlyCollection<string>> assignments, CancellationToken ct);
}

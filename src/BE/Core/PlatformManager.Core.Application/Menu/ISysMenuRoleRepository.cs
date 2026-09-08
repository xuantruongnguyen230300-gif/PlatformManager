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

    /// <summary>Ghi đè TOÀN BỘ SysMenuRole theo ma trận gửi lên — UpdatePermissionMatrixCommand.
    /// Từ 2026-08-31 ghi đè bằng xoá MỀM (dòng cũ ở lại với <c>IsDeleted = true</c>), xem
    /// doc/huong_dan/quy-uoc/be-entity-domain.md §"Quyết định người dùng 2026-08-31".</summary>
    Task ReplaceAllAsync(IReadOnlyDictionary<Guid, IReadOnlyCollection<string>> assignments, CancellationToken ct);

    /// <summary>
    /// Token phiên bản của TOÀN BỘ ma trận đang sống — <c>GET</c> trả kèm, <c>PUT</c> gửi lại,
    /// server tính lại rồi so (lệch ⇒ 409, không ghi gì). Chỉ tính trên dòng CHƯA xoá mềm; xem
    /// <see cref="Permissions.MatrixVersion"/> cho bẫy đi kèm.
    /// </summary>
    Task<string> GetVersionAsync(CancellationToken ct);
}

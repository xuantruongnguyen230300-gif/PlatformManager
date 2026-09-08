namespace PlatformManager.Core.Application.Permissions;

/// <summary>
/// Application chỉ làm việc với TÊN role (string) — Infrastructure tự resolve sang AppRole.Id cụ
/// thể (Identity, Application không được biết kiểu này). Mẫu tương tự ISysMenuRoleRepository,
/// nhưng ngược chiều mặc định: ResourceKey không có dòng nào = KHÔNG role nào được cấp (khác
/// SysMenuRole coi vắng mặt = mở cho mọi người) — xem RolePermission.
/// </summary>
public interface IRolePermissionRepository
{
    /// <summary>ResourceKey → danh sách tên role được cấp quyền. ResourceKey không có key trong
    /// dict này = KHÔNG role nào được cấp (deny-by-default).</summary>
    Task<Dictionary<string, List<string>>> GetAssignedRoleNamesByResourceKeyAsync(CancellationToken ct);

    /// <summary>Ghi đè TOÀN BỘ RolePermission theo ma trận gửi lên — UpdateResourcePermissionMatrixCommand.
    /// Từ 2026-08-31 ghi đè bằng xoá MỀM (dòng cũ ở lại với <c>IsDeleted = true</c>), xem
    /// doc/huong_dan/quy-uoc/be-entity-domain.md §"Quyết định người dùng 2026-08-31".</summary>
    Task ReplaceAllAsync(IReadOnlyDictionary<string, IReadOnlyCollection<string>> assignments, CancellationToken ct);

    /// <summary>Token phiên bản của toàn bộ ma trận đang sống — cùng cơ chế và cùng bẫy với
    /// <c>ISysMenuRoleRepository.GetVersionAsync</c>, xem <see cref="MatrixVersion"/>.</summary>
    Task<string> GetVersionAsync(CancellationToken ct);
}

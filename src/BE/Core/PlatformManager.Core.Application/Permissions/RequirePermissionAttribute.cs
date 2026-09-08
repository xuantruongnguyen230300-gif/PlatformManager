namespace PlatformManager.Core.Application.Permissions;

/// <summary>
/// Metadata THUẦN (không chứa logic runtime) — khai trên controller/action để đòi permission-key
/// cụ thể — khoá phải khớp một mục trong danh mục host khai qua <see cref="ICoreResourceKeySource"/>
/// (tách 2026-09-03), nếu không thì không role nào cấp được nó và endpoint 403 cho tất cả trừ
/// SuperAdmin. RequirePermissionFilter
/// (Core.Infrastructure) đọc attribute này lúc runtime qua EndpointMetadata. Không khai attribute
/// = không chặn thêm gì ngoài [Authorize] hiện có. Xem
/// doc/huong_dan/quy-uoc/be-api-controller.md §"Phân quyền theo hành động — permission-key đầy đủ".
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequirePermissionAttribute(string key) : Attribute
{
    public string Key { get; } = key;
}

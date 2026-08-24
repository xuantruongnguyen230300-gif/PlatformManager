namespace PlatformManager.Core.Application.Permissions;

/// <summary>
/// Metadata THUẦN (không chứa logic runtime) — khai trên controller/action để đòi permission-key
/// cụ thể (vd "criteria.manage", khớp hằng số ở ResourceKeys). RequirePermissionFilter
/// (Core.Infrastructure) đọc attribute này lúc runtime qua EndpointMetadata. Không khai attribute
/// = không chặn thêm gì ngoài [Authorize] hiện có. Xem
/// .claude/rules/api-controller.md §"Phân quyền theo hành động — permission-key đầy đủ".
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequirePermissionAttribute(string key) : Attribute
{
    public string Key { get; } = key;
}

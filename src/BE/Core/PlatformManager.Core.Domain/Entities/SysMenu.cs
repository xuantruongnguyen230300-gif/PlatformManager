using PlatformManager.Core.Domain.Common;

namespace PlatformManager.Core.Domain.Entities;

/// <summary>
/// Metadata menu điều hướng — dữ liệu thuần (Loại C theo
/// doc/huong_dan/wiki-core/be/03-metadata-driven-design.md §3.1), tự tham chiếu đúng 1 cấp
/// (ParentId trỏ vào chính SysMenu.Id, NULL = item gốc). Item cha (có con) có Route = NULL —
/// chỉ toggle expand/collapse.
/// </summary>
public class SysMenu : BaseEntity
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Route { get; private set; }
    public string? Icon { get; private set; }
    public Guid? ParentId { get; private set; }
    public int DisplayOrder { get; private set; }

    private SysMenu() { }

    public static SysMenu Create(string code, string name, string? route, string? icon, Guid? parentId, int displayOrder)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("SYS_MENU_CODE_REQUIRED", "Mã menu không được để trống.");
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("SYS_MENU_NAME_REQUIRED", "Tên menu không được để trống.");

        return new SysMenu
        {
            Id = EntityId.New(),
            Code = code.Trim(),
            Name = name.Trim(),
            Route = route,
            Icon = icon,
            ParentId = parentId,
            DisplayOrder = displayOrder,
        };
    }
}

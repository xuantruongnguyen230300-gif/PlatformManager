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
            throw new DomainException(SysMenuErrors.CodeRequired);
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException(SysMenuErrors.NameRequired);

        return new SysMenu
        {
            Code = code.Trim(),
            Name = name.Trim(),
            Route = route,
            Icon = icon,
            ParentId = parentId,
            DisplayOrder = displayOrder,
        };
    }

    /// <summary>
    /// Hồi sinh một menu ĐÃ XOÁ MỀM và đồng bộ lại toàn bộ định nghĩa của nó (mọi field trừ
    /// <see cref="BaseEntity.Id"/> và <see cref="Code"/> — <c>Code</c> chính là khoá dùng để tìm
    /// ra dòng này nên không đổi được ở đây).
    ///
    /// <para>Tồn tại vì <c>Code</c> chỉ unique trong tập CHƯA xoá mềm (index partial
    /// <c>IX_SysMenus_Code</c>, migration 0008). Không có đường hồi sinh thì mọi luồng "chưa có
    /// thì thêm" sẽ chèn dòng MỚI cùng <c>Code</c> bên cạnh dòng đã xoá mềm — Postgres không
    /// chặn, và bảng lặng lẽ có hai dòng cùng mã.</para>
    /// </summary>
    public void ReviveWith(string name, string? route, string? icon, Guid? parentId, int displayOrder)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException(SysMenuErrors.NameRequired);

        IsDeleted = false;
        Name = name.Trim();
        Route = route;
        Icon = icon;
        ParentId = parentId;
        DisplayOrder = displayOrder;
    }
}

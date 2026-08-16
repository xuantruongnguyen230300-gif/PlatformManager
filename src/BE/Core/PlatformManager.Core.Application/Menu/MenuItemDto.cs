namespace PlatformManager.Core.Application.Menu;

/// <summary>
/// Danh sách PHẲNG (không lồng cây) — FE (`shared/services/menu.service.ts`) tự dựng cây 1 cấp
/// từ `ParentId` (`buildMenuTree()`), khớp `IMenuItemDto` ở
/// `shared/models/menu-item.model.ts`. KHÔNG đổi lại thành cây lồng sẵn — đã đối chiếu trực
/// tiếp code FE thật, đây là hợp đồng đúng, không phải suy đoán.
/// </summary>
public sealed record MenuItemDto(
    Guid Id,
    Guid? ParentId,
    string Code,
    string Label,
    string? Icon,
    string? Route,
    int DisplayOrder);

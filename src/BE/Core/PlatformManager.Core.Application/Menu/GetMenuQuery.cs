using MediatR;
using PlatformManager.Core.Application.Common.CQRS;
using PlatformManager.Core.Application.Common.Interfaces;
using PlatformManager.Core.Application.Common.Results;

namespace PlatformManager.Core.Application.Menu;

/// <summary>GET /api/meta/menu — lọc theo role hiện tại đối chiếu SysMenuRole; màn không có
/// dòng nào trong SysMenuRole = mở cho mọi user đã đăng nhập. Trả DANH SÁCH PHẲNG — FE tự dựng
/// cây (xem MenuItemDto.cs).</summary>
public sealed record GetMenuQuery : IQuery<List<MenuItemDto>>;

public sealed class GetMenuHandler(ISysMenuRepository menuRepo, ISysMenuRoleRepository menuRoleRepo, ICurrentUser currentUser)
    : BaseResponse, IRequestHandler<GetMenuQuery, IApiResult<List<MenuItemDto>>>
{
    public async Task<IApiResult<List<MenuItemDto>>> Handle(GetMenuQuery query, CancellationToken ct)
    {
        var allMenus = await menuRepo.GetAllAsync(ct);
        var visibleIds = await menuRoleRepo.GetVisibleSysMenuIdsForRolesAsync(currentUser.Roles, ct);

        var visibleMenus = allMenus.Where(m => visibleIds.Contains(m.Id)).ToList();

        // Giữ cha nếu bất kỳ con nào visible (cha chỉ toggle expand, luôn hiện nếu có ít
        // nhất 1 con thấy được, kể cả khi bản thân mục cha bị gán role riêng không khớp).
        // FE cần record của cha có mặt trong danh sách phẳng để buildMenuTree() gắn con vào
        // đúng chỗ — thiếu record cha sẽ làm con "mồ côi" (parentId trỏ tới id không tồn tại
        // trong response, buildMenuTree() coi con đó là root).
        var visibleIdSet = visibleMenus.Select(m => m.Id).ToHashSet();
        var parentIdsNeeded = visibleMenus.Where(m => m.ParentId is not null).Select(m => m.ParentId!.Value).ToHashSet();
        foreach (var parentId in parentIdsNeeded)
        {
            if (visibleIdSet.Add(parentId) && allMenus.FirstOrDefault(m => m.Id == parentId) is { } parent)
                visibleMenus.Add(parent);
        }

        var flat = visibleMenus
            .OrderBy(m => m.DisplayOrder)
            .Select(m => new MenuItemDto(m.Id, m.ParentId, m.Code, m.Name, m.Icon, m.Route, m.DisplayOrder))
            .ToList();

        return Ok(flat);
    }
}

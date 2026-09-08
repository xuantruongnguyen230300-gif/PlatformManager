using MediatR;
using PlatformManager.Core.Application.Common;
using PlatformManager.Core.Application.Common.CQRS;
using PlatformManager.Core.Application.Common.Results;
using PlatformManager.Core.Application.Menu;

namespace PlatformManager.Core.Application.Permissions;

/// <summary>GET /api/admin/permissions — ma trận toàn bộ SysMenu × 3 role. Controller gate
/// [Authorize(Roles="SuperAdmin")] (chỉ SuperAdmin xem/sửa được).</summary>
public sealed record GetPermissionMatrixQuery : IQuery<PermissionMatrixDto>;

public sealed class GetPermissionMatrixHandler(ISysMenuRepository menuRepo, ISysMenuRoleRepository menuRoleRepo)
    : BaseResponse, IRequestHandler<GetPermissionMatrixQuery, IApiResult<PermissionMatrixDto>>
{
    public async Task<IApiResult<PermissionMatrixDto>> Handle(GetPermissionMatrixQuery query, CancellationToken ct)
    {
        var menus = await menuRepo.GetAllAsync(ct);
        var assignments = await menuRoleRepo.GetAssignedRoleNamesBySysMenuAsync(ct);

        // Token phiên bản đọc SAU dữ liệu ma trận, nhưng cả hai đều là truy vấn đọc trong cùng
        // một request nên khoảng hở là không đáng kể — và nếu có ai ghi xen vào đúng khe đó thì
        // hậu quả tệ nhất là client nhận token mới hơn dữ liệu, tức PUT kế tiếp bị 409 (an toàn)
        // chứ không phải ghi đè im lặng.
        var version = await menuRoleRepo.GetVersionAsync(ct);

        var rows = menus
            .OrderBy(m => m.DisplayOrder)
            .Select(m => new PermissionMatrixRowDto(
                m.Id, m.Code, m.Name, m.ParentId,
                assignments.TryGetValue(m.Id, out var roles) ? roles : []))
            .ToList();

        return Ok(new PermissionMatrixDto(Roles.All, rows, version));
    }
}

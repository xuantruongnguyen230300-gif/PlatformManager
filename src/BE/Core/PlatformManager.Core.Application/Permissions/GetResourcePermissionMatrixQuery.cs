using MediatR;
using PlatformManager.Core.Application.Common;
using PlatformManager.Core.Application.Common.CQRS;
using PlatformManager.Core.Application.Common.Results;

namespace PlatformManager.Core.Application.Permissions;

/// <summary>GET /api/admin/permissions/resources — ma trận ResourceKey × 3 role
/// (permission-key đầy đủ, KHÁC PermissionMatrix chỉ điều khiển menu nhìn thấy được). Controller
/// gate [Authorize(Roles="SuperAdmin")], giống PERM-1. Xem
/// doc/contracts/permissions.md CONTRACT PERM-2.</summary>
public sealed record GetResourcePermissionMatrixQuery : IQuery<ResourcePermissionMatrixDto>;

public sealed class GetResourcePermissionMatrixHandler(IRolePermissionRepository repo)
    : BaseResponse, IRequestHandler<GetResourcePermissionMatrixQuery, IApiResult<ResourcePermissionMatrixDto>>
{
    public async Task<IApiResult<ResourcePermissionMatrixDto>> Handle(GetResourcePermissionMatrixQuery query, CancellationToken ct)
    {
        var assignments = await repo.GetAssignedRoleNamesByResourceKeyAsync(ct);

        var rows = ResourceKeys.All
            .Select(key => new ResourcePermissionRowDto(
                key, ResourceKeys.DisplayNames[key],
                assignments.TryGetValue(key, out var roles) ? roles : []))
            .ToList();

        return Ok(new ResourcePermissionMatrixDto(Roles.All, rows));
    }
}

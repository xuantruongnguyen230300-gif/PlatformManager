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

/// <summary>
/// Hàng của ma trận dựng từ DANH MỤC (<see cref="ICoreResourceKeySource"/>) chứ không từ dữ liệu
/// trong bảng — deny-by-default nghĩa là "chưa role nào được cấp" được biểu diễn bằng KHÔNG CÓ
/// DÒNG trong <c>RolePermissions</c>, nên dựng hàng từ DB sẽ làm biến mất đúng những key nguy hiểm
/// nhất khỏi màn hình quản trị.
/// </summary>
public sealed class GetResourcePermissionMatrixHandler(
    IRolePermissionRepository repo,
    ICoreResourceKeySource resourceKeySource)
    : BaseResponse, IRequestHandler<GetResourcePermissionMatrixQuery, IApiResult<ResourcePermissionMatrixDto>>
{
    public async Task<IApiResult<ResourcePermissionMatrixDto>> Handle(GetResourcePermissionMatrixQuery query, CancellationToken ct)
    {
        var assignments = await repo.GetAssignedRoleNamesByResourceKeyAsync(ct);

        // Xem chú thích cùng chỗ ở GetPermissionMatrixHandler cho lý do đọc token sau dữ liệu.
        var version = await repo.GetVersionAsync(ct);

        var rows = resourceKeySource.Catalog()
            .Select(definition => new ResourcePermissionRowDto(
                definition.Key, definition.DisplayName,
                assignments.TryGetValue(definition.Key, out var roles) ? roles : []))
            .ToList();

        return Ok(new ResourcePermissionMatrixDto(Roles.All, rows, version));
    }
}

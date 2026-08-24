using FluentValidation;
using MediatR;
using PlatformManager.Core.Application.Common;
using PlatformManager.Core.Application.Common.CQRS;
using PlatformManager.Core.Application.Common.Interfaces;
using PlatformManager.Core.Application.Common.Results;

namespace PlatformManager.Core.Application.Permissions;

/// <summary>PUT /api/admin/permissions/resources — ghi đè TOÀN BỘ RolePermission theo ma trận
/// gửi lên (giống hệt UpdatePermissionMatrixCommand — PERM-1). Controller gate
/// [Authorize(Roles="SuperAdmin")]. Xem
/// doc/contracts/permissions.md CONTRACT PERM-2 §"Rủi ro rollout" trước khi migrate.</summary>
public sealed record UpdateResourcePermissionMatrixCommand(IReadOnlyCollection<ResourcePermissionEntryDto> Entries) : ICommand<bool>;

public sealed class UpdateResourcePermissionMatrixValidator : AbstractValidator<UpdateResourcePermissionMatrixCommand>
{
    public UpdateResourcePermissionMatrixValidator()
    {
        RuleForEach(x => x.Entries).ChildRules(entry =>
        {
            entry.RuleFor(e => e.ResourceKey).Must(k => ResourceKeys.All.Contains(k))
                .WithMessage("ResourceKey không hợp lệ — chỉ nhận các key đã khai ở ResourceKeys.");
            entry.RuleForEach(e => e.Roles).Must(r => Roles.All.Contains(r))
                .WithMessage("Role không hợp lệ — chỉ nhận SuperAdmin/Admin/User.");
        });
    }
}

public sealed class UpdateResourcePermissionMatrixHandler(IRolePermissionRepository repo, IUnitOfWork uow)
    : BaseResponse, IRequestHandler<UpdateResourcePermissionMatrixCommand, IApiResult<bool>>
{
    public async Task<IApiResult<bool>> Handle(UpdateResourcePermissionMatrixCommand cmd, CancellationToken ct)
    {
        var assignments = cmd.Entries.ToDictionary(
            e => e.ResourceKey, IReadOnlyCollection<string> (e) => e.Roles.Distinct().ToList());

        await repo.ReplaceAllAsync(assignments, ct);
        await uow.SaveChangesAsync(ct);
        return Ok(true);
    }
}

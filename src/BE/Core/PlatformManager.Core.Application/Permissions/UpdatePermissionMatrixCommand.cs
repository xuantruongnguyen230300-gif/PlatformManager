using FluentValidation;
using MediatR;
using PlatformManager.Core.Application.Common;
using PlatformManager.Core.Application.Common.CQRS;
using PlatformManager.Core.Application.Common.Interfaces;
using PlatformManager.Core.Application.Common.Results;
using PlatformManager.Core.Application.Menu;

namespace PlatformManager.Core.Application.Permissions;

/// <summary>PUT /api/admin/permissions — ghi đè TOÀN BỘ SysMenuRole theo ma trận gửi lên.
/// Controller gate [Authorize(Roles="SuperAdmin")] — Admin thường KHÔNG được sửa (rủi ro leo
/// thang quyền, xem doc/ke-hoach-xay-lai-corebase.md).</summary>
public sealed record UpdatePermissionMatrixCommand(IReadOnlyCollection<PermissionMatrixEntryDto> Entries) : ICommand<bool>;

public sealed class UpdatePermissionMatrixValidator : AbstractValidator<UpdatePermissionMatrixCommand>
{
    public UpdatePermissionMatrixValidator()
    {
        RuleForEach(x => x.Entries).ChildRules(entry =>
        {
            entry.RuleFor(e => e.SysMenuId).NotEmpty();
            entry.RuleForEach(e => e.Roles).Must(r => Roles.All.Contains(r))
                .WithMessage("Role không hợp lệ — chỉ nhận SuperAdmin/Admin/User.");
        });
    }
}

public sealed class UpdatePermissionMatrixHandler(ISysMenuRoleRepository menuRoleRepo, IUnitOfWork uow)
    : BaseResponse, IRequestHandler<UpdatePermissionMatrixCommand, IApiResult<bool>>
{
    public async Task<IApiResult<bool>> Handle(UpdatePermissionMatrixCommand cmd, CancellationToken ct)
    {
        var assignments = cmd.Entries.ToDictionary(
            e => e.SysMenuId, IReadOnlyCollection<string> (e) => e.Roles.Distinct().ToList());

        await menuRoleRepo.ReplaceAllAsync(assignments, ct);
        await uow.SaveChangesAsync(ct);
        return Ok(true);
    }
}

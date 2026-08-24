using MediatR;
using PlatformManager.Core.Application.Common.CQRS;
using PlatformManager.Core.Application.Common.Interfaces;
using PlatformManager.Core.Application.Common.Results;

namespace PlatformManager.Core.Application.Users;

/// <summary>POST /api/users/{id}/lock — khoá qua UserManager.SetLockoutEndDateAsync (xem
/// doc/ERD/ERD-corebase.md §1.2), không thêm cột IsActive riêng.</summary>
public sealed record LockUserCommand(Guid Id) : ICommand<bool>;

public sealed class LockUserHandler(IUserAdminService userAdminService, ICurrentUser currentUser)
    : BaseResponse, IRequestHandler<LockUserCommand, IApiResult<bool>>
{
    public async Task<IApiResult<bool>> Handle(LockUserCommand cmd, CancellationToken ct)
    {
        var target = await userAdminService.GetByIdAsync(cmd.Id, ct);
        if (target is null)
            return Fail<bool>(UserErrors.NotFound);

        // Luật 3 + 4 (SuperAdminAccountGuard) — PHẢI chạy TRƯỚC khi chạm tầng ghi, xem guard đó
        // để hiểu ngữ cảnh (khoá tài khoản SuperAdmin phải là SuperAdmin; tự khoá chính mình cấm
        // tuyệt đối, áp cho MỌI role).
        var guardError = SuperAdminAccountGuard.CheckLock(
            currentUser, cmd.Id, SuperAdminAccountGuard.ContainsSuperAdmin(target.Roles));
        if (guardError is not null)
            return Fail<bool>(guardError);

        var ok = await userAdminService.LockAsync(cmd.Id, ct);
        return Ok(ok);
    }
}

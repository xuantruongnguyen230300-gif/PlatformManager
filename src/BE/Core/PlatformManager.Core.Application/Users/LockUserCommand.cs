using MediatR;
using PlatformManager.Core.Application.Common.CQRS;
using PlatformManager.Core.Application.Common.Results;

namespace PlatformManager.Core.Application.Users;

/// <summary>POST /api/users/{id}/lock — khoá qua UserManager.SetLockoutEndDateAsync (xem
/// doc/ERD/ERD-corebase.md §1.2), không thêm cột IsActive riêng.</summary>
public sealed record LockUserCommand(Guid Id) : ICommand<bool>;

public sealed class LockUserHandler(IUserAdminService userAdminService)
    : BaseResponse, IRequestHandler<LockUserCommand, IApiResult<bool>>
{
    public async Task<IApiResult<bool>> Handle(LockUserCommand cmd, CancellationToken ct)
    {
        if (await userAdminService.GetByIdAsync(cmd.Id, ct) is null)
            return Fail<bool>(UserErrors.NotFound);

        var ok = await userAdminService.LockAsync(cmd.Id, ct);
        return Ok(ok);
    }
}

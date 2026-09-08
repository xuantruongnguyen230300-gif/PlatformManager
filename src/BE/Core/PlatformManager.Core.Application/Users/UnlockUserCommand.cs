using MediatR;
using PlatformManager.Core.Application.Common.CQRS;
using PlatformManager.Core.Application.Common.Results;

namespace PlatformManager.Core.Application.Users;

/// <summary>POST /api/users/{id}/unlock</summary>
public sealed record UnlockUserCommand(Guid Id) : ICommand<bool>;

public sealed class UnlockUserHandler(IUserAdminService userAdminService)
    : BaseResponse, IRequestHandler<UnlockUserCommand, IApiResult<bool>>
{
    public async Task<IApiResult<bool>> Handle(UnlockUserCommand cmd, CancellationToken ct)
    {
        if (await userAdminService.GetByIdAsync(cmd.Id, ct) is null)
            return Fail<bool>(UserErrors.NotFound);

        // Cùng lý do với LockUserHandler (finding BE-4): UnlockAsync trả false khi
        // SetLockoutEndDateAsync fail, và 200 + data=false làm FE báo "Đã mở khoá" cho một tài
        // khoản vẫn đang bị khoá.
        var ok = await userAdminService.UnlockAsync(cmd.Id, ct);
        return ok ? Ok(true) : Fail<bool>(UserErrors.UnlockFailed);
    }
}

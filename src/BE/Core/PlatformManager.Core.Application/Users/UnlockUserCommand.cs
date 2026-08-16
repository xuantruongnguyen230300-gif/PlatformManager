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

        var ok = await userAdminService.UnlockAsync(cmd.Id, ct);
        return Ok(ok);
    }
}

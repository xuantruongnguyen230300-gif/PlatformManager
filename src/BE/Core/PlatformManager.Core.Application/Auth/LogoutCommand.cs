using MediatR;
using PlatformManager.Core.Application.Common.CQRS;
using PlatformManager.Core.Application.Common.Results;

namespace PlatformManager.Core.Application.Auth;

/// <summary>POST /api/auth/logout</summary>
public sealed record LogoutCommand : ICommand<bool>;

public sealed class LogoutHandler(IIdentityService identityService)
    : BaseResponse, IRequestHandler<LogoutCommand, IApiResult<bool>>
{
    public async Task<IApiResult<bool>> Handle(LogoutCommand cmd, CancellationToken ct)
    {
        await identityService.SignOutAsync(ct);
        return Ok(true);
    }
}

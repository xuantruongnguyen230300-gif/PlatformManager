using MediatR;
using PlatformManager.Core.Application.Common.CQRS;
using PlatformManager.Core.Application.Common.Interfaces;
using PlatformManager.Core.Application.Common.Results;

namespace PlatformManager.Core.Application.Auth;

/// <summary>GET /api/auth/me — PHẢI trả kèm mustChangePassword (xem
/// doc/ke-hoach-xay-lai-corebase.md).</summary>
public sealed record GetCurrentUserQuery : IQuery<CurrentUserInfo>;

public sealed class GetCurrentUserHandler(ICurrentUser currentUser, IIdentityService identityService)
    : BaseResponse, IRequestHandler<GetCurrentUserQuery, IApiResult<CurrentUserInfo>>
{
    public async Task<IApiResult<CurrentUserInfo>> Handle(GetCurrentUserQuery query, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            return Fail<CurrentUserInfo>(AuthErrors.NotAuthenticated);

        var info = await identityService.GetUserInfoAsync(currentUser.UserId.Value, ct);
        if (info is null)
            return Fail<CurrentUserInfo>(AuthErrors.NotAuthenticated);

        return Ok(info);
    }
}

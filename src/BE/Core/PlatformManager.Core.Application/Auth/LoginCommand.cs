using FluentValidation;
using MediatR;
using PlatformManager.Core.Application.Common.CQRS;
using PlatformManager.Core.Application.Common.Results;

namespace PlatformManager.Core.Application.Auth;

/// <summary>POST /api/auth/login — cookie session (đã CHỐT).</summary>
public sealed record LoginCommand(string UserName, string Password) : ICommand<CurrentUserInfo>;

public sealed class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(x => x.UserName).NotEmpty();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public sealed class LoginHandler(IIdentityService identityService)
    : BaseResponse, IRequestHandler<LoginCommand, IApiResult<CurrentUserInfo>>
{
    public async Task<IApiResult<CurrentUserInfo>> Handle(LoginCommand cmd, CancellationToken ct)
    {
        var result = await identityService.SignInAsync(cmd.UserName, cmd.Password, ct);

        if (result.IsLockedOut)
            return Fail<CurrentUserInfo>(AuthErrors.LockedOut);

        if (!result.Succeeded || result.User is null)
            return Fail<CurrentUserInfo>(AuthErrors.InvalidCredentials);

        return Ok(result.User);
    }
}

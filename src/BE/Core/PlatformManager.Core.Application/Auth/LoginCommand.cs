using FluentValidation;
using MediatR;
using PlatformManager.Core.Application.Common.CQRS;
using PlatformManager.Core.Application.Common.Results;

namespace PlatformManager.Core.Application.Auth;

/// <summary>POST /api/auth/login — cookie session (đã CHỐT).</summary>
/// <param name="RememberMe">
/// Ô "Ghi nhớ đăng nhập" của màn đăng nhập (nối vào đường thật 2026-08-31 — trước đó ô này có
/// trên giao diện nhưng không nối vào đâu cả). <b>Mặc định <c>false</c></b>: request không gửi
/// trường này được hiểu là KHÔNG tích, tức cookie phiên. Mặc định phải nằm ở bên an toàn — một
/// client cũ chưa biết trường này sẽ nhận phiên ngắn hơn chứ không phải phiên dài hơn.
/// Xem doc/contracts/auth.md §POST /api/auth/login.
/// </param>
public sealed record LoginCommand(string UserName, string Password, bool RememberMe = false)
    : ICommand<CurrentUserInfo>;

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
        var result = await identityService.SignInAsync(cmd.UserName, cmd.Password, cmd.RememberMe, ct);

        if (result.IsLockedOut)
            return Fail<CurrentUserInfo>(AuthErrors.LockedOut);

        if (!result.Succeeded || result.User is null)
            return Fail<CurrentUserInfo>(AuthErrors.InvalidCredentials);

        return Ok(result.User);
    }
}

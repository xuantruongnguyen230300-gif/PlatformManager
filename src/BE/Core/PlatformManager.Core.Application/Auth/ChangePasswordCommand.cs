using FluentValidation;
using MediatR;
using PlatformManager.Core.Application.Common.CQRS;
using PlatformManager.Core.Application.Common.Interfaces;
using PlatformManager.Core.Application.Common.Results;

namespace PlatformManager.Core.Application.Auth;

/// <summary>POST /api/auth/change-password — set MustChangePassword=false sau khi thành
/// công. Áp dụng cho luồng bắt buộc đổi mật khẩu lần đăng nhập đầu (bootstrap account và
/// mọi user do Admin tạo).</summary>
public sealed record ChangePasswordCommand(string CurrentPassword, string NewPassword) : ICommand<bool>;

public sealed class ChangePasswordValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(6);
    }
}

public sealed class ChangePasswordHandler(ICurrentUser currentUser, IIdentityService identityService)
    : BaseResponse, IRequestHandler<ChangePasswordCommand, IApiResult<bool>>
{
    public async Task<IApiResult<bool>> Handle(ChangePasswordCommand cmd, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            return Fail<bool>(AuthErrors.NotAuthenticated);

        var result = await identityService.ChangePasswordAsync(
            currentUser.UserId.Value, cmd.CurrentPassword, cmd.NewPassword, ct);

        if (!result.Succeeded)
            return Fail<bool>(AuthErrors.ChangePasswordFailed, string.Join("; ", result.Errors));

        return Ok(true);
    }
}

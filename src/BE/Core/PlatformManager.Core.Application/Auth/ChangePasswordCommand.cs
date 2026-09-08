using FluentValidation;
using MediatR;
using PlatformManager.Core.Application.Common.CQRS;
using PlatformManager.Core.Application.Common.Interfaces;
using PlatformManager.Core.Application.Common.Results;
using PlatformManager.Core.Application.Users;

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

        // Tài khoản biến mất giữa lúc còn phiên đăng nhập — KHÔNG phải "đổi mật khẩu thất bại".
        // Trước 2026-09-05 nhánh này nhét một CÂU TIẾNG VIỆT vào danh sách Errors, nên nó ghép ra
        // "Đổi mật khẩu thất bại: Không tìm thấy người dùng."; sau khi danh sách đó thành khoá tra
        // bảng dịch (§11.2 bẫy 3) thì câu ấy sẽ trở thành một khoá dịch là một câu tiếng Việt.
        if (result.NotFound)
            return Fail<bool>(UserErrors.NotFound);

        if (!result.Succeeded)
            // Từng mã Identity đi ra fieldErrors kèm ĐÚNG ô nhập nó nói tới — PasswordMismatch nói
            // về ô "mật khẩu hiện tại", không phải ô "mật khẩu mới" (bẫy 1 của §11.2). Ánh xạ nằm ở
            // IdentityFieldErrors, dùng chung với đường tạo/sửa người dùng.
            return Fail<bool>(
                AuthErrors.ChangePasswordFailed,
                IdentityFieldErrors.Build(result.Errors, IdentityFormFields.ChangePassword));

        return Ok(true);
    }
}

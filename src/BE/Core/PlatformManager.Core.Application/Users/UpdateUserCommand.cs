using FluentValidation;
using MediatR;
using PlatformManager.Core.Application.Common;
using PlatformManager.Core.Application.Common.CQRS;
using PlatformManager.Core.Application.Common.Results;

namespace PlatformManager.Core.Application.Users;

/// <summary>PUT /api/users/{id} — sửa Email/FullName/Roles. Không đổi UserName/mật khẩu
/// qua đây (đổi mật khẩu đi qua luồng riêng — Auth/ChangePasswordCommand).</summary>
public sealed record UpdateUserCommand(Guid Id, string? Email, string FullName, IReadOnlyCollection<string> Roles)
    : ICommand<bool>;

public sealed class UpdateUserValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleForEach(x => x.Roles).Must(r => Roles.All.Contains(r))
            .WithMessage("Role không hợp lệ — chỉ nhận SuperAdmin/Admin/User.");
    }
}

public sealed class UpdateUserHandler(IUserAdminService userAdminService)
    : BaseResponse, IRequestHandler<UpdateUserCommand, IApiResult<bool>>
{
    public async Task<IApiResult<bool>> Handle(UpdateUserCommand cmd, CancellationToken ct)
    {
        if (await userAdminService.GetByIdAsync(cmd.Id, ct) is null)
            return Fail<bool>(UserErrors.NotFound);

        var ok = await userAdminService.UpdateAsync(cmd.Id, cmd.Email, cmd.FullName, cmd.Roles, ct);
        return ok ? Ok(true) : Fail<bool>(UserErrors.CreateFailed, "cập nhật thất bại");
    }
}

using FluentValidation;
using MediatR;
using PlatformManager.Core.Application.Common;
using PlatformManager.Core.Application.Common.CQRS;
using PlatformManager.Core.Application.Common.Results;

namespace PlatformManager.Core.Application.Users;

/// <summary>POST /api/users — Admin tạo user mới với mật khẩu tạm, MustChangePassword=true
/// bắt buộc (áp dụng chung cơ chế bootstrap-account, xem doc/ke-hoach-xay-lai-corebase.md).</summary>
public sealed record CreateUserCommand(
    string UserName, string? Email, string FullName, string TempPassword, IReadOnlyCollection<string> Roles)
    : ICommand<Guid>;

public sealed class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.UserName).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.TempPassword).NotEmpty().MinimumLength(6);
        RuleForEach(x => x.Roles).Must(r => Roles.All.Contains(r))
            .WithMessage("Role không hợp lệ — chỉ nhận SuperAdmin/Admin/User.");
    }
}

public sealed class CreateUserHandler(IUserAdminService userAdminService)
    : BaseResponse, IRequestHandler<CreateUserCommand, IApiResult<Guid>>
{
    public async Task<IApiResult<Guid>> Handle(CreateUserCommand cmd, CancellationToken ct)
    {
        if (await userAdminService.UserNameExistsAsync(cmd.UserName, ct))
            return Fail<Guid>(UserErrors.DuplicateUserName, cmd.UserName);

        if (!string.IsNullOrWhiteSpace(cmd.Email) && await userAdminService.EmailExistsAsync(cmd.Email, ct))
            return Fail<Guid>(UserErrors.DuplicateEmail, cmd.Email);

        var outcome = await userAdminService.CreateAsync(
            cmd.UserName, cmd.Email, cmd.FullName, cmd.TempPassword, cmd.Roles, ct);

        if (!outcome.Succeeded || outcome.UserId is null)
            return Fail<Guid>(UserErrors.CreateFailed, string.Join("; ", outcome.Errors));

        return Ok(outcome.UserId.Value);
    }
}

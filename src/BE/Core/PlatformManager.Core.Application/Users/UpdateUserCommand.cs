using FluentValidation;
using MediatR;
using PlatformManager.Core.Application.Common;
using PlatformManager.Core.Application.Common.CQRS;
using PlatformManager.Core.Application.Common.Interfaces;
using PlatformManager.Core.Application.Common.Results;

namespace PlatformManager.Core.Application.Users;

/// <summary>PUT /api/users/{id} — sửa Email/FullName/Roles. Không đổi UserName/mật khẩu
/// qua đây (đổi mật khẩu đi qua luồng riêng — Auth/ChangePasswordCommand).
///
/// <para><b><paramref name="Version"/> (2026-08-31):</b> <c>ConcurrencyStamp</c> nhận được cùng
/// dữ liệu người dùng, gửi lại nguyên văn; lệch ⇒ 409 và không ghi gì.</para></summary>
/// <param name="Version">
/// ⚠️ <b>Đã nối hết đường, nhưng CÒN TUỲ CHỌN</b> (2026-08-31): <c>UpdateUserRequest</c> có trường
/// <c>Version</c> và <c>UsersController</c> truyền nó xuống. Handler vẫn chỉ kiểm khi client
/// THẬT SỰ gửi token — chọn cách này thay vì "null ⇒ 409" là có chủ đích: bắt
/// buộc ngay hôm nay sẽ làm MỌI lệnh sửa người dùng trả 409, tức khoá luôn màn Quản trị người
/// dùng. Đổi sang bắt buộc = xoá đúng một điều kiện <c>is not null</c> trong handler. Khi làm,
/// test <c>UserUpdateVersionTests.Put_WithoutVersion_StillSucceeds</c> PHẢI đỏ — nó là mỏ neo cố ý
/// cho trạng thái nửa vời này; nếu nó vẫn xanh thì việc siết chưa có hiệu lực.
/// </param>
public sealed record UpdateUserCommand(
    Guid Id, string? Email, string FullName, IReadOnlyCollection<string> Roles, string? Version = null)
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

public sealed class UpdateUserHandler(IUserAdminService userAdminService, ICurrentUser currentUser)
    : BaseResponse, IRequestHandler<UpdateUserCommand, IApiResult<bool>>
{
    public async Task<IApiResult<bool>> Handle(UpdateUserCommand cmd, CancellationToken ct)
    {
        var target = await userAdminService.GetByIdAsync(cmd.Id, ct);
        if (target is null)
            return Fail<bool>(UserErrors.NotFound);

        // Luật 1 + 2 (SuperAdminAccountGuard) — NotFound PHẢI kiểm TRƯỚC (không tính được
        // targetHasSuperAdmin nếu user không tồn tại), guard PHẢI kiểm TRƯỚC khi chạm tầng ghi.
        var guardError = SuperAdminAccountGuard.CheckRoleChange(
            currentUser, cmd.Id, SuperAdminAccountGuard.ContainsSuperAdmin(target.Roles), cmd.Roles);
        if (guardError is not null)
            return Fail<bool>(guardError);

        // Tranh chấp ghi: token client gửi lên so với token vừa đọc từ DB trong CHÍNH request này.
        // Kiểm SAU guard (403 trước 409): người không có quyền sửa thì không cần biết trạng thái
        // bản ghi đã đổi hay chưa. Kiểm TRƯỚC UpdateAsync nên lệch ⇒ không ghi gì.
        //
        // Điều kiện `cmd.Version is not null` là chỗ DUY NHẤT làm rule này lỏng hơn card đã chốt —
        // xem docstring của UpdateUserCommand.Version cho lý do và cách siết lại.
        if (cmd.Version is not null && !string.Equals(cmd.Version, target.Version, StringComparison.Ordinal))
            return Fail<bool>(UserErrors.VersionConflict);

        var outcome = await userAdminService.UpdateAsync(cmd.Id, cmd.Email, cmd.FullName, cmd.Roles, ct);

        if (outcome.Succeeded)
            return Ok(true);

        // Bị xoá xen giữa lần đọc ở đầu handler và lần ghi vừa rồi — 404, KHÔNG phải "cập nhật thất
        // bại". Đây là lý do UpdateUserOutcome tách NotFound khỏi Errors (§11.3): gộp lại thì bản
        // ghi vừa bị xoá ra USER.UPDATE_FAILED kèm danh sách mã rỗng.
        if (outcome.NotFound)
            return Fail<bool>(UserErrors.NotFound);

        // UserErrors.UpdateFailed, KHÔNG phải CreateFailed (finding BE-8): mượn mã của đường tạo
        // làm message ghép ra "Tạo người dùng thất bại: cập nhật thất bại" và làm businessCode nói
        // sai hành động.
        //
        // Tham số ("Reasons", "không lưu được thay đổi") đã bỏ 2026-09-05 (§11.3): chuỗi đó là câu
        // BỊA TẠI CHỖ GỌI vì UpdateAsync khi ấy trả bool trần, không còn gì thật để nói — câu ghép
        // ra "Cập nhật người dùng thất bại: không lưu được thay đổi", nói hai lần cùng một điều.
        // Nay outcome mang mã Identity thật, và mã đi ra fieldErrors đúng ô nhập.
        return Fail<bool>(
            UserErrors.UpdateFailed,
            IdentityFieldErrors.Build(outcome.Errors, IdentityFormFields.UserForm));
    }
}

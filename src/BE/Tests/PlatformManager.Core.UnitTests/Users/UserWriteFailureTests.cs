using NSubstitute;
using PlatformManager.Core.Application.Common;
using PlatformManager.Core.Application.Common.Interfaces;
using PlatformManager.Core.Application.Common.Results;
using PlatformManager.Core.Application.Users;
using Xunit;

namespace PlatformManager.Core.UnitTests.Users;

/// <summary>
/// Findings BE-4 + BE-8 (audit 2026-08-29) — <b>thất bại của tầng ghi phải RA LỖI</b>.
///
/// <para><b>BE-4:</b> <c>LockUserHandler</c>/<c>UnlockUserHandler</c> trước đây trả
/// <c>Ok(ok)</c>, nên khi <c>UserAdminService</c> trả <c>false</c> (UpdateSecurityStampAsync
/// hoặc SetLockoutEndDateAsync fail) client vẫn nhận HTTP 200 với <c>data: false</c>. FE map
/// <c>data</c> thành <c>undefined</c> rồi hiện toast "Đã khoá tài khoản." — quản trị viên tin
/// là đã khoá trong khi CHƯA khoá. Nguy hiểm gấp đôi ở đường lock vì con dấu bảo mật đã đổi
/// TRƯỚC: người dùng bị đá phiên nhưng đăng nhập lại được.</para>
///
/// <para><b>BE-8:</b> <c>UpdateUserHandler</c> mượn <c>UserErrors.CreateFailed</c>, ghép ra câu
/// "Tạo người dùng thất bại: cập nhật thất bại" cho một thao tác SỬA.</para>
///
/// <para>Test đứng ở tầng handler với <c>IUserAdminService</c> giả: thứ đang chốt là HỢP ĐỒNG
/// trả về (mã lỗi + không còn 200 cho ca hỏng), không phải cách Identity hỏng.</para>
/// </summary>
public class UserWriteFailureTests
{
    private static readonly Guid CallerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TargetId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static ICurrentUser Caller(params string[] roles)
    {
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(CallerId);
        currentUser.Roles.Returns(roles);
        currentUser.IsInRole(Arg.Any<string>()).Returns(call => roles.Contains(call.Arg<string>()));
        return currentUser;
    }

    private static UserDto TargetUser(params string[] roles)
        => new(TargetId, "target", "target@example.com", "Người dùng đích", roles, false, false, null);

    private static IUserAdminService ServiceReturning(params string[] targetRoles)
    {
        var service = Substitute.For<IUserAdminService>();
        service.GetByIdAsync(TargetId, Arg.Any<CancellationToken>()).Returns(TargetUser(targetRoles));
        return service;
    }

    // ---------- BE-4: lock ----------

    [Fact(DisplayName = "LockAsync trả false → 422 USER.LOCK_FAILED, KHÔNG phải 200 + data=false")]
    public async Task Lock_WhenWriteLayerFails_ReturnsBusinessError()
    {
        var service = ServiceReturning(Roles.User);
        service.LockAsync(default, default).ReturnsForAnyArgs(false);

        var result = await new LockUserHandler(service, Caller(Roles.Admin))
            .Handle(new LockUserCommand(TargetId), CancellationToken.None);

        Assert.Equal(ErrorCode.BusinessRuleError, result.Code);
        Assert.Equal("USER.LOCK_FAILED", result.BusinessCode);

        // data=false đi kèm status 200 chính là ca hỏng cũ — khẳng định lại để bản vá không bị
        // "sửa" ngược thành Ok(false) kèm message.
        Assert.NotEqual(ApiResultStatus.SUCCESS, result.Status);
        Assert.False(result.Data);
    }

    [Fact(DisplayName = "LockAsync trả true → 200 data=true (không hồi quy đường thành công)")]
    public async Task Lock_WhenWriteLayerSucceeds_ReturnsOk()
    {
        var service = ServiceReturning(Roles.User);
        service.LockAsync(default, default).ReturnsForAnyArgs(true);

        var result = await new LockUserHandler(service, Caller(Roles.Admin))
            .Handle(new LockUserCommand(TargetId), CancellationToken.None);

        Assert.Equal(ErrorCode.Success, result.Code);
        Assert.True(result.Data);
    }

    // ---------- BE-4: unlock ----------

    [Fact(DisplayName = "UnlockAsync trả false → 422 USER.UNLOCK_FAILED")]
    public async Task Unlock_WhenWriteLayerFails_ReturnsBusinessError()
    {
        var service = ServiceReturning(Roles.User);
        service.UnlockAsync(default, default).ReturnsForAnyArgs(false);

        var result = await new UnlockUserHandler(service)
            .Handle(new UnlockUserCommand(TargetId), CancellationToken.None);

        Assert.Equal(ErrorCode.BusinessRuleError, result.Code);
        Assert.Equal("USER.UNLOCK_FAILED", result.BusinessCode);
        Assert.NotEqual(ApiResultStatus.SUCCESS, result.Status);
    }

    [Fact(DisplayName = "UnlockAsync trả true → 200 data=true")]
    public async Task Unlock_WhenWriteLayerSucceeds_ReturnsOk()
    {
        var service = ServiceReturning(Roles.User);
        service.UnlockAsync(default, default).ReturnsForAnyArgs(true);

        var result = await new UnlockUserHandler(service)
            .Handle(new UnlockUserCommand(TargetId), CancellationToken.None);

        Assert.Equal(ErrorCode.Success, result.Code);
        Assert.True(result.Data);
    }

    // ---------- BE-8: update ----------

    [Fact(DisplayName = "UpdateAsync bị Identity từ chối → USER.UPDATE_FAILED, message KHÔNG nói 'Tạo người dùng'")]
    public async Task Update_WhenWriteLayerFails_UsesUpdateErrorNotCreateError()
    {
        var service = ServiceReturning(Roles.User);
        service.UpdateAsync(default, default, default!, default!, default)
            .ReturnsForAnyArgs(UpdateUserOutcome.Rejected(["ConcurrencyFailure"]));

        var result = await new UpdateUserHandler(service, Caller(Roles.Admin)).Handle(
            new UpdateUserCommand(TargetId, "a@example.com", "Người dùng đích", [Roles.User]),
            CancellationToken.None);

        Assert.Equal("USER.UPDATE_FAILED", result.BusinessCode);
        Assert.NotEqual("USER.CREATE_FAILED", result.BusinessCode);

        // Chốt cả CÂU CHỮ: mã đúng mà message vẫn ghép từ template của đường tạo thì người dùng
        // vẫn đọc được "Tạo người dùng thất bại" khi họ đang sửa — đúng thứ finding BE-8 nói tới.
        Assert.NotNull(result.Message);
        Assert.DoesNotContain("Tạo người dùng", result.Message);
        Assert.Contains("Cập nhật người dùng", result.Message);
    }

    [Fact(DisplayName = "UpdateAsync trả true → 200 data=true")]
    public async Task Update_WhenWriteLayerSucceeds_ReturnsOk()
    {
        var service = ServiceReturning(Roles.User);
        service.UpdateAsync(default, default, default!, default!, default).ReturnsForAnyArgs(UpdateUserOutcome.Success());

        var result = await new UpdateUserHandler(service, Caller(Roles.Admin)).Handle(
            new UpdateUserCommand(TargetId, "a@example.com", "Người dùng đích", [Roles.User]),
            CancellationToken.None);

        Assert.Equal(ErrorCode.Success, result.Code);
        Assert.True(result.Data);
    }
}

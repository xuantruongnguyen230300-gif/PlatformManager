using NSubstitute;
using PlatformManager.Core.Application.Common;
using PlatformManager.Core.Application.Common.Interfaces;
using PlatformManager.Core.Application.Common.Results;
using PlatformManager.Core.Application.Users;
using Xunit;

namespace PlatformManager.Core.UnitTests.Users;

/// <summary>
/// NHÓM A — 2 luật bổ sung của đợt 2026-08-19 (người dùng chốt phương án (b)):
/// <list type="number">
///   <item>Khoá tài khoản có role SuperAdmin → phải là SuperAdmin. Nếu không có luật này thì
///     bản vá leo thang bịt hụt: Admin không cấp được quyền cho mình nhưng vẫn KHOÁ được đường
///     vào cuối cùng, đúng kịch bản break-glass sinh ra để tránh.</item>
///   <item>Tự gỡ role SuperAdmin / tự khoá tài khoản CHÍNH MÌNH → cấm tuyệt đối, kể cả người
///     gọi là SuperAdmin. Đây là ca "hạ quyền im lặng" fe-perf-core tìm ra — luật đợt trước cho
///     qua vì người gọi đúng là SuperAdmin, nên không lỗi nào bật ra.</item>
/// </list>
/// Luật "đổi tư cách SuperAdmin của NGƯỜI KHÁC" nằm ở SuperAdminRoleEscalationTests.
/// </summary>
public class SuperAdminAccountProtectionTests
{
    private static readonly Guid CallerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static ICurrentUser Caller(params string[] roles)
    {
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(CallerId);
        currentUser.Roles.Returns(roles);
        currentUser.IsInRole(Arg.Any<string>())
            .Returns(callInfo => roles.Contains(callInfo.Arg<string>()));
        return currentUser;
    }

    private static UserDto UserWith(Guid id, params string[] roles)
        => new(id, "u" + id.ToString("N")[..4], "u@example.com", "Người dùng", roles, false, false, null);

    // ---------- Luật 3: khoá tài khoản SuperAdmin ----------

    [Fact(DisplayName = "Admin khoá tài khoản SuperAdmin → 403 và LockAsync KHÔNG hề được gọi")]
    public async Task Admin_LockingSuperAdmin_IsForbidden_AndNeverReachesWriteLayer()
    {
        var service = Substitute.For<IUserAdminService>();
        service.GetByIdAsync(OtherId, Arg.Any<CancellationToken>())
            .Returns(UserWith(OtherId, Roles.SuperAdmin));
        var handler = new LockUserHandler(service, Caller(Roles.Admin));

        var result = await handler.Handle(new LockUserCommand(OtherId), CancellationToken.None);

        Assert.Equal(ErrorCode.AuthorizationError, result.Code);
        Assert.Equal("USER.SUPERADMIN_LOCK_FORBIDDEN", result.BusinessCode);
        await service.DidNotReceiveWithAnyArgs().LockAsync(default, default);
    }

    [Fact(DisplayName = "SuperAdmin khoá tài khoản SuperAdmin khác → cho qua")]
    public async Task SuperAdmin_LockingAnotherSuperAdmin_IsAllowed()
    {
        var service = Substitute.For<IUserAdminService>();
        service.GetByIdAsync(OtherId, Arg.Any<CancellationToken>())
            .Returns(UserWith(OtherId, Roles.SuperAdmin));
        service.LockAsync(default, default).ReturnsForAnyArgs(true);
        var handler = new LockUserHandler(service, Caller(Roles.SuperAdmin));

        var result = await handler.Handle(new LockUserCommand(OtherId), CancellationToken.None);

        Assert.Equal(ErrorCode.Success, result.Code);
        await service.Received(1).LockAsync(OtherId, Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Admin khoá user Admin/User thường → cho qua như cũ (không hồi quy)")]
    public async Task Admin_LockingOrdinaryUser_IsAllowed()
    {
        var service = Substitute.For<IUserAdminService>();
        service.GetByIdAsync(OtherId, Arg.Any<CancellationToken>())
            .Returns(UserWith(OtherId, Roles.Admin, Roles.User));
        service.LockAsync(default, default).ReturnsForAnyArgs(true);
        var handler = new LockUserHandler(service, Caller(Roles.Admin));

        var result = await handler.Handle(new LockUserCommand(OtherId), CancellationToken.None);

        Assert.Equal(ErrorCode.Success, result.Code);
        await service.Received(1).LockAsync(OtherId, Arg.Any<CancellationToken>());
    }

    // ---------- Luật 4: tự khoá chính mình ----------

    [Fact(DisplayName = "SuperAdmin tự khoá chính mình → 403 và LockAsync KHÔNG hề được gọi")]
    public async Task SuperAdmin_LockingSelf_IsForbidden_AndNeverReachesWriteLayer()
    {
        var service = Substitute.For<IUserAdminService>();
        service.GetByIdAsync(CallerId, Arg.Any<CancellationToken>())
            .Returns(UserWith(CallerId, Roles.SuperAdmin));
        var handler = new LockUserHandler(service, Caller(Roles.SuperAdmin));

        var result = await handler.Handle(new LockUserCommand(CallerId), CancellationToken.None);

        Assert.Equal(ErrorCode.AuthorizationError, result.Code);
        Assert.Equal("USER.SELF_LOCK_FORBIDDEN", result.BusinessCode);
        await service.DidNotReceiveWithAnyArgs().LockAsync(default, default);
    }

    [Fact(DisplayName = "Admin tự khoá chính mình → 403 (luật áp cho MỌI role, không riêng SuperAdmin)")]
    public async Task Admin_LockingSelf_IsForbidden()
    {
        var service = Substitute.For<IUserAdminService>();
        service.GetByIdAsync(CallerId, Arg.Any<CancellationToken>())
            .Returns(UserWith(CallerId, Roles.Admin));
        var handler = new LockUserHandler(service, Caller(Roles.Admin));

        var result = await handler.Handle(new LockUserCommand(CallerId), CancellationToken.None);

        Assert.Equal("USER.SELF_LOCK_FORBIDDEN", result.BusinessCode);
        await service.DidNotReceiveWithAnyArgs().LockAsync(default, default);
    }

    // ---------- Luật 2: tự gỡ role SuperAdmin của chính mình ----------

    [Fact(DisplayName = "SuperAdmin tự gỡ SuperAdmin của CHÍNH MÌNH → 403 và UpdateAsync KHÔNG hề được gọi")]
    public async Task SuperAdmin_RemovingOwnSuperAdmin_IsForbidden_AndNeverReachesWriteLayer()
    {
        var service = Substitute.For<IUserAdminService>();
        service.GetByIdAsync(CallerId, Arg.Any<CancellationToken>())
            .Returns(UserWith(CallerId, Roles.SuperAdmin, Roles.Admin));
        var handler = new UpdateUserHandler(service, Caller(Roles.SuperAdmin));

        // Đây chính là ca ĐI QUA được luật đợt trước: người gọi ĐÚNG là SuperAdmin.
        var result = await handler.Handle(
            new UpdateUserCommand(CallerId, "u@example.com", "Người dùng", [Roles.Admin]),
            CancellationToken.None);

        Assert.Equal(ErrorCode.AuthorizationError, result.Code);
        Assert.Equal("USER.SELF_SUPERADMIN_REMOVAL_FORBIDDEN", result.BusinessCode);
        await service.DidNotReceiveWithAnyArgs().UpdateAsync(default, default, default!, default!, default);
    }

    [Fact(DisplayName = "SuperAdmin sửa email của CHÍNH MÌNH, giữ nguyên SuperAdmin → cho qua")]
    public async Task SuperAdmin_EditingOwnProfile_KeepingSuperAdmin_IsAllowed()
    {
        var service = Substitute.For<IUserAdminService>();
        service.GetByIdAsync(CallerId, Arg.Any<CancellationToken>())
            .Returns(UserWith(CallerId, Roles.SuperAdmin, Roles.Admin));
        service.UpdateAsync(default, default, default!, default!, default).ReturnsForAnyArgs(true);
        var handler = new UpdateUserHandler(service, Caller(Roles.SuperAdmin));

        var result = await handler.Handle(
            new UpdateUserCommand(CallerId, "moi@example.com", "Tên Mới", [Roles.SuperAdmin, Roles.Admin]),
            CancellationToken.None);

        // Luật là "tự GỠ", không phải "không được tự sửa gì" — chặn nhầm ở đây sẽ khoá luôn
        // việc SuperAdmin tự đổi email của mình.
        Assert.Equal(ErrorCode.Success, result.Code);
        await service.ReceivedWithAnyArgs(1).UpdateAsync(default, default, default!, default!, default);
    }

    [Fact(DisplayName = "SuperAdmin gỡ SuperAdmin của NGƯỜI KHÁC → vẫn cho qua (không chặn nhầm sang người khác)")]
    public async Task SuperAdmin_RemovingSuperAdminOfSomeoneElse_IsStillAllowed()
    {
        var service = Substitute.For<IUserAdminService>();
        service.GetByIdAsync(OtherId, Arg.Any<CancellationToken>())
            .Returns(UserWith(OtherId, Roles.SuperAdmin, Roles.Admin));
        service.UpdateAsync(default, default, default!, default!, default).ReturnsForAnyArgs(true);
        var handler = new UpdateUserHandler(service, Caller(Roles.SuperAdmin));

        var result = await handler.Handle(
            new UpdateUserCommand(OtherId, "u@example.com", "Người dùng", [Roles.Admin]),
            CancellationToken.None);

        Assert.Equal(ErrorCode.Success, result.Code);
        await service.ReceivedWithAnyArgs(1).UpdateAsync(default, default, default!, default!, default);
    }

    [Fact(DisplayName = "Admin tự gỡ role Admin của chính mình → cho qua (luật chỉ bảo vệ SuperAdmin)")]
    public async Task Admin_RemovingOwnAdminRole_IsAllowed()
    {
        var service = Substitute.For<IUserAdminService>();
        service.GetByIdAsync(CallerId, Arg.Any<CancellationToken>())
            .Returns(UserWith(CallerId, Roles.Admin));
        service.UpdateAsync(default, default, default!, default!, default).ReturnsForAnyArgs(true);
        var handler = new UpdateUserHandler(service, Caller(Roles.Admin));

        var result = await handler.Handle(
            new UpdateUserCommand(CallerId, "u@example.com", "Người dùng", [Roles.User]),
            CancellationToken.None);

        // Cố ý KHÔNG mở rộng luật thành "không được tự đổi role của mình" — chỉ tư cách
        // SuperAdmin mới là đường vào cuối cùng của hệ thống.
        Assert.Equal(ErrorCode.Success, result.Code);
        await service.ReceivedWithAnyArgs(1).UpdateAsync(default, default, default!, default!, default);
    }

    // ---------- Unlock: cố ý KHÔNG chặn ----------

    [Fact(DisplayName = "Admin mở khoá tài khoản SuperAdmin → CỐ Ý cho qua (mở khoá là chiều khôi phục)")]
    public async Task Admin_UnlockingSuperAdmin_IsDeliberatelyAllowed()
    {
        var service = Substitute.For<IUserAdminService>();
        service.GetByIdAsync(OtherId, Arg.Any<CancellationToken>())
            .Returns(UserWith(OtherId, Roles.SuperAdmin));
        service.UnlockAsync(default, default).ReturnsForAnyArgs(true);
        var handler = new UnlockUserHandler(service);

        var result = await handler.Handle(new UnlockUserCommand(OtherId), CancellationToken.None);

        // Chốt bằng test để lần audit sau đọc ra ngay đây là quyết định có chủ đích, không phải
        // chỗ bị sót: mở khoá KHÔI PHỤC quyền truy cập, không cấp thêm gì cho người gọi.
        Assert.Equal(ErrorCode.Success, result.Code);
        await service.Received(1).UnlockAsync(OtherId, Arg.Any<CancellationToken>());
    }
}

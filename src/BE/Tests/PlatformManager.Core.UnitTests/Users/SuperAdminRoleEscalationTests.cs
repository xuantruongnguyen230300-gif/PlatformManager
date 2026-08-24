using NSubstitute;
using PlatformManager.Core.Application.Common;
using PlatformManager.Core.Application.Common.Interfaces;
using PlatformManager.Core.Application.Common.Results;
using PlatformManager.Core.Application.Users;
using Xunit;

namespace PlatformManager.Core.UnitTests.Users;

/// <summary>
/// NHÓM A — luồng QUYẾT ĐỊNH phân quyền của CreateUserHandler/UpdateUserHandler, KHÔNG cần
/// Postgres (<see cref="IUserAdminService"/> bị thay bằng substitute).
///
/// Đóng Finding 1 của audit doc/huong_dan/wiki-core/audit/2026-08-19-be-fe.md (OWASP A01 —
/// Broken Access Control): trước đây Admin gọi thẳng API là tự cấp được role SuperAdmin, và
/// nhờ break-glass ở RequirePermissionFilter thì role đó đi qua MỌI [RequirePermission].
/// Chặn duy nhất khi đó nằm ở model phía FE, tức không chặn gì cả với curl.
///
/// Mỗi test chặn đều khẳng định HAI thứ: (a) mã lỗi 403 đúng loại, (b) tầng ghi
/// (CreateAsync/UpdateAsync) KHÔNG HỀ được gọi — (b) mới chứng minh chặn xảy ra TRƯỚC khi
/// chạm tới nơi ghi dữ liệu, chứ không phải ghi xong rồi mới trả lỗi.
/// </summary>
public class SuperAdminRoleEscalationTests
{
    private static readonly Guid TargetId = Guid.Parse("2f2f4d1e-0d63-4f1a-9a3a-9b6f0c5f1c11");

    /// <summary>Người gọi ở file này LUÔN khác user đích — luật "tự gỡ quyền/tự khoá chính mình"
    /// là chuyện của SuperAdminAccountProtectionTests, tách ra để 2 luật không che nhau.</summary>
    private static readonly Guid CallerId = Guid.Parse("8c1b7a90-5f2e-4c6b-8d3a-1e4f7b2c9d55");

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

    private static UserDto TargetWith(params string[] roles)
        => new(TargetId, "target", "target@example.com", "Người dùng đích", roles, false, false, null);

    private static void AssertForbidden<T>(IApiResult<T> result)
    {
        Assert.Equal(ErrorCode.AuthorizationError, result.Code);
        Assert.Equal(ApiResultStatus.BUSINESS_ERROR, result.Status);
        Assert.Equal("USER.SUPERADMIN_ROLE_CHANGE_FORBIDDEN", result.BusinessCode);
    }

    // ---------- CreateUserHandler ----------

    [Fact(DisplayName = "Admin tạo user với role SuperAdmin → 403 và CreateAsync KHÔNG hề được gọi")]
    public async Task Admin_CreatingSuperAdmin_IsForbidden_AndNeverReachesWriteLayer()
    {
        var service = Substitute.For<IUserAdminService>();
        var handler = new CreateUserHandler(service, Caller(Roles.Admin));

        var result = await handler.Handle(
            new CreateUserCommand("kesau", "kesau@example.com", "Kẻ Sau", "TempPass1!", [Roles.SuperAdmin]),
            CancellationToken.None);

        AssertForbidden(result);
        await service.DidNotReceiveWithAnyArgs().CreateAsync(default!, default, default!, default!, default!, default);
    }

    [Fact(DisplayName = "SuperAdmin tạo user với role SuperAdmin → cho qua")]
    public async Task SuperAdmin_CreatingSuperAdmin_IsAllowed()
    {
        var service = Substitute.For<IUserAdminService>();
        service.CreateAsync(default!, default, default!, default!, default!, default)
            .ReturnsForAnyArgs(new CreateUserOutcome(true, TargetId, []));
        var handler = new CreateUserHandler(service, Caller(Roles.SuperAdmin));

        var result = await handler.Handle(
            new CreateUserCommand("ke2", "ke2@example.com", "Kế Nhiệm", "TempPass1!", [Roles.SuperAdmin]),
            CancellationToken.None);

        Assert.Equal(ErrorCode.Success, result.Code);
        Assert.Equal(TargetId, result.Data);
        await service.ReceivedWithAnyArgs(1).CreateAsync(default!, default, default!, default!, default!, default);
    }

    [Fact(DisplayName = "Admin tạo user với role Admin/User → cho qua như cũ (không hồi quy)")]
    public async Task Admin_CreatingOrdinaryRoles_IsAllowed()
    {
        var service = Substitute.For<IUserAdminService>();
        service.CreateAsync(default!, default, default!, default!, default!, default)
            .ReturnsForAnyArgs(new CreateUserOutcome(true, TargetId, []));
        var handler = new CreateUserHandler(service, Caller(Roles.Admin));

        var result = await handler.Handle(
            new CreateUserCommand("nv01", "nv01@example.com", "Nhân Viên", "TempPass1!", [Roles.Admin, Roles.User]),
            CancellationToken.None);

        Assert.Equal(ErrorCode.Success, result.Code);
        await service.ReceivedWithAnyArgs(1).CreateAsync(default!, default, default!, default!, default!, default);
    }

    // ---------- UpdateUserHandler — chiều THÊM (leo thang) ----------

    [Fact(DisplayName = "Admin sửa user thành có SuperAdmin → 403 và UpdateAsync KHÔNG hề được gọi")]
    public async Task Admin_GrantingSuperAdmin_IsForbidden_AndNeverReachesWriteLayer()
    {
        var service = Substitute.For<IUserAdminService>();
        service.GetByIdAsync(TargetId, Arg.Any<CancellationToken>()).Returns(TargetWith(Roles.User));
        var handler = new UpdateUserHandler(service, Caller(Roles.Admin));

        var result = await handler.Handle(
            new UpdateUserCommand(TargetId, "target@example.com", "Người dùng đích", [Roles.SuperAdmin]),
            CancellationToken.None);

        AssertForbidden(result);
        await service.DidNotReceiveWithAnyArgs().UpdateAsync(default, default, default!, default!, default);
    }

    [Fact(DisplayName = "SuperAdmin sửa user thành có SuperAdmin → cho qua")]
    public async Task SuperAdmin_GrantingSuperAdmin_IsAllowed()
    {
        var service = Substitute.For<IUserAdminService>();
        service.GetByIdAsync(TargetId, Arg.Any<CancellationToken>()).Returns(TargetWith(Roles.User));
        service.UpdateAsync(default, default, default!, default!, default).ReturnsForAnyArgs(true);
        var handler = new UpdateUserHandler(service, Caller(Roles.SuperAdmin));

        var result = await handler.Handle(
            new UpdateUserCommand(TargetId, "target@example.com", "Người dùng đích", [Roles.SuperAdmin]),
            CancellationToken.None);

        Assert.Equal(ErrorCode.Success, result.Code);
        await service.ReceivedWithAnyArgs(1).UpdateAsync(default, default, default!, default!, default);
    }

    // ---------- UpdateUserHandler — chiều GỠ (vô hiệu hoá break-glass) ----------

    [Fact(DisplayName = "Admin gỡ SuperAdmin khỏi user đang có → 403 và UpdateAsync KHÔNG hề được gọi")]
    public async Task Admin_RemovingSuperAdmin_IsForbidden_AndNeverReachesWriteLayer()
    {
        var service = Substitute.For<IUserAdminService>();
        service.GetByIdAsync(TargetId, Arg.Any<CancellationToken>())
            .Returns(TargetWith(Roles.SuperAdmin, Roles.Admin));
        var handler = new UpdateUserHandler(service, Caller(Roles.Admin));

        // Danh sách role gửi lên là TRỌN GÓI — bỏ "SuperAdmin" ra khỏi payload chính là hạ quyền.
        var result = await handler.Handle(
            new UpdateUserCommand(TargetId, "target@example.com", "Người dùng đích", [Roles.Admin]),
            CancellationToken.None);

        AssertForbidden(result);
        await service.DidNotReceiveWithAnyArgs().UpdateAsync(default, default, default!, default!, default);
    }

    [Fact(DisplayName = "SuperAdmin gỡ SuperAdmin khỏi user đang có → cho qua")]
    public async Task SuperAdmin_RemovingSuperAdmin_IsAllowed()
    {
        var service = Substitute.For<IUserAdminService>();
        service.GetByIdAsync(TargetId, Arg.Any<CancellationToken>())
            .Returns(TargetWith(Roles.SuperAdmin, Roles.Admin));
        service.UpdateAsync(default, default, default!, default!, default).ReturnsForAnyArgs(true);
        var handler = new UpdateUserHandler(service, Caller(Roles.SuperAdmin));

        var result = await handler.Handle(
            new UpdateUserCommand(TargetId, "target@example.com", "Người dùng đích", [Roles.Admin]),
            CancellationToken.None);

        Assert.Equal(ErrorCode.Success, result.Code);
        await service.ReceivedWithAnyArgs(1).UpdateAsync(default, default, default!, default!, default);
    }

    // ---------- Không được chặn NHẦM ----------

    [Fact(DisplayName = "Admin sửa email của một SuperAdmin nhưng GIỮ NGUYÊN SuperAdmin → cho qua")]
    public async Task Admin_EditingSuperAdmin_WithoutChangingSuperAdminMembership_IsAllowed()
    {
        var service = Substitute.For<IUserAdminService>();
        service.GetByIdAsync(TargetId, Arg.Any<CancellationToken>())
            .Returns(TargetWith(Roles.SuperAdmin, Roles.Admin));
        service.UpdateAsync(default, default, default!, default!, default).ReturnsForAnyArgs(true);
        var handler = new UpdateUserHandler(service, Caller(Roles.Admin));

        var result = await handler.Handle(
            new UpdateUserCommand(TargetId, "email-moi@example.com", "Tên Mới", [Roles.SuperAdmin, Roles.Admin]),
            CancellationToken.None);

        // Luật là "đổi TƯ CÁCH SuperAdmin", KHÔNG phải "Admin không được đụng user SuperAdmin".
        Assert.Equal(ErrorCode.Success, result.Code);
        await service.Received(1).UpdateAsync(
            TargetId, "email-moi@example.com", "Tên Mới",
            Arg.Is<IReadOnlyCollection<string>>(r => r.Contains(Roles.SuperAdmin)),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Admin sửa user thường với role Admin/User → cho qua như cũ (không hồi quy)")]
    public async Task Admin_UpdatingOrdinaryRoles_IsAllowed()
    {
        var service = Substitute.For<IUserAdminService>();
        service.GetByIdAsync(TargetId, Arg.Any<CancellationToken>()).Returns(TargetWith(Roles.User));
        service.UpdateAsync(default, default, default!, default!, default).ReturnsForAnyArgs(true);
        var handler = new UpdateUserHandler(service, Caller(Roles.Admin));

        var result = await handler.Handle(
            new UpdateUserCommand(TargetId, "target@example.com", "Người dùng đích", [Roles.Admin, Roles.User]),
            CancellationToken.None);

        Assert.Equal(ErrorCode.Success, result.Code);
        await service.ReceivedWithAnyArgs(1).UpdateAsync(default, default, default!, default!, default);
    }

    [Fact(DisplayName = "User không tồn tại → vẫn NotFound (404), không bị luật mới nuốt mất")]
    public async Task MissingUser_StillReturnsNotFound()
    {
        var service = Substitute.For<IUserAdminService>();
        service.GetByIdAsync(TargetId, Arg.Any<CancellationToken>()).Returns((UserDto?)null);
        var handler = new UpdateUserHandler(service, Caller(Roles.Admin));

        var result = await handler.Handle(
            new UpdateUserCommand(TargetId, null, "Không tồn tại", [Roles.SuperAdmin]),
            CancellationToken.None);

        Assert.Equal(ErrorCode.NotFound, result.Code);
        await service.DidNotReceiveWithAnyArgs().UpdateAsync(default, default, default!, default!, default);
    }
}

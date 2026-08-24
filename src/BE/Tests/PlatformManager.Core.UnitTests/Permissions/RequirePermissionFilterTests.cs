using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using NSubstitute;
using PlatformManager.Core.Application.Common;
using PlatformManager.Core.Application.Permissions;
using PlatformManager.Core.Infrastructure.Permissions;
using Xunit;

namespace PlatformManager.Core.UnitTests.Permissions;

/// <summary>
/// NHÓM A — luồng QUYẾT ĐỊNH của filter, KHÔNG cần Postgres (<see cref="IPermissionChecker"/> bị
/// thay bằng substitute). Ngữ nghĩa truy vấn (join AspNetRoles × RolePermissions, dòng mồ côi,
/// Name NULL) nằm ở nhóm B — PlatformManager.Core.IntegrationTests, chạy trên Postgres thật.
///
/// Đóng finding #1 của audit 2026-08-18: trước đây các nhánh này chỉ được kiểm THỦ CÔNG một lần
/// rồi mất. Xem doc/huong_dan/wiki-core/be/11-performance-caching.md §7.3.
/// </summary>
public class RequirePermissionFilterTests
{
    private const string Key = ResourceKeys.Criteria;

    private static AuthorizationFilterContext BuildContext(string[] roles, bool withAttribute = true)
    {
        var actionDescriptor = new ActionDescriptor
        {
            EndpointMetadata = withAttribute ? [new RequirePermissionAttribute(Key)] : [],
        };

        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(
                new ClaimsIdentity([.. roles.Select(r => new Claim(ClaimTypes.Role, r))], "TestAuth")),
        };

        return new AuthorizationFilterContext(
            new ActionContext(httpContext, new RouteData(), actionDescriptor), []);
    }

    [Fact(DisplayName = "Không khai [RequirePermission] → không chặn, KHÔNG chạm tầng kiểm quyền")]
    public async Task NoAttribute_DoesNotBlock_AndNeverCallsChecker()
    {
        var checker = Substitute.For<IPermissionChecker>();
        var context = BuildContext([Roles.User], withAttribute: false);

        await new RequirePermissionFilter(checker).OnAuthorizationAsync(context);

        Assert.Null(context.Result);
        await checker.DidNotReceiveWithAnyArgs().HasPermissionAsync(default!, default!, default);
    }

    [Fact(DisplayName = "Claim role rỗng → Forbid và KHÔNG chạm DB")]
    public async Task NoRoleClaim_Forbids_WithoutTouchingDatabase()
    {
        var checker = Substitute.For<IPermissionChecker>();
        var context = BuildContext([]);

        await new RequirePermissionFilter(checker).OnAuthorizationAsync(context);

        Assert.IsType<ForbidResult>(context.Result);
        // Đây là phần chỉ seam mới chứng minh được: không có role thì KHÔNG được phép bắn query.
        await checker.DidNotReceiveWithAnyArgs().HasPermissionAsync(default!, default!, default);
    }

    [Fact(DisplayName = "SuperAdmin → qua kể cả khi KHÔNG có RolePermission nào (break-glass)")]
    public async Task SuperAdmin_BypassesEvenWithoutAnyRolePermission()
    {
        var checker = Substitute.For<IPermissionChecker>();
        // Tầng kiểm quyền trả false = "không có dòng RolePermission nào cho role này".
        checker.HasPermissionAsync(default!, default!, default).ReturnsForAnyArgs(false);

        var context = BuildContext([Roles.SuperAdmin]);

        await new RequirePermissionFilter(checker).OnAuthorizationAsync(context);

        Assert.Null(context.Result);
        // Bypass phải cắt TRƯỚC khi tra DB — nếu sau này ai đó đảo thứ tự, test này đỏ.
        await checker.DidNotReceiveWithAnyArgs().HasPermissionAsync(default!, default!, default);
    }

    [Fact(DisplayName = "SuperAdmin kèm role khác → vẫn qua")]
    public async Task SuperAdmin_AmongOtherRoles_StillBypasses()
    {
        var checker = Substitute.For<IPermissionChecker>();
        checker.HasPermissionAsync(default!, default!, default).ReturnsForAnyArgs(false);

        var context = BuildContext([Roles.User, Roles.SuperAdmin]);

        await new RequirePermissionFilter(checker).OnAuthorizationAsync(context);

        Assert.Null(context.Result);
    }

    [Fact(DisplayName = "KHÔNG phải SuperAdmin và không có quyền → vẫn Forbid (bypass không nới lỏng gì khác)")]
    public async Task NonSuperAdmin_WithoutPermission_StillForbidden()
    {
        var checker = Substitute.For<IPermissionChecker>();
        checker.HasPermissionAsync(default!, default!, default).ReturnsForAnyArgs(false);

        var context = BuildContext([Roles.Admin, Roles.User]);

        await new RequirePermissionFilter(checker).OnAuthorizationAsync(context);

        Assert.IsType<ForbidResult>(context.Result);
    }

    [Fact(DisplayName = "KHÔNG phải SuperAdmin nhưng có quyền → cho qua")]
    public async Task NonSuperAdmin_WithPermission_IsAllowed()
    {
        var checker = Substitute.For<IPermissionChecker>();
        checker.HasPermissionAsync(default!, default!, default).ReturnsForAnyArgs(true);

        var context = BuildContext([Roles.Admin]);

        await new RequirePermissionFilter(checker).OnAuthorizationAsync(context);

        Assert.Null(context.Result);
    }

    [Fact(DisplayName = "Role trùng lặp trong claim → truyền xuống tầng kiểm quyền đã loại trùng, kèm đúng ResourceKey")]
    public async Task DuplicateRoleClaims_ArePassedDistinct_WithTheDeclaredKey()
    {
        var checker = Substitute.For<IPermissionChecker>();
        checker.HasPermissionAsync(default!, default!, default).ReturnsForAnyArgs(true);

        var context = BuildContext([Roles.Admin, Roles.Admin, Roles.User]);

        await new RequirePermissionFilter(checker).OnAuthorizationAsync(context);

        await checker.Received(1).HasPermissionAsync(
            Arg.Is<IReadOnlyCollection<string>>(r => r.Count == 2 && r.Contains(Roles.Admin) && r.Contains(Roles.User)),
            Key,
            Arg.Any<CancellationToken>());
    }
}

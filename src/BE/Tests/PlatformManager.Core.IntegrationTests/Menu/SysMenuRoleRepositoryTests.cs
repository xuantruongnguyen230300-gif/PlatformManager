using PlatformManager.Core.Domain.Entities;
using PlatformManager.Core.Infrastructure.Identity;
using PlatformManager.Core.Infrastructure.Persistence.Repositories;
using Xunit;

namespace PlatformManager.Core.IntegrationTests.Menu;

/// <summary>
/// NHÓM B — ngữ nghĩa hiển thị menu (<see cref="SysMenuRoleRepository.GetVisibleSysMenuIdsForRolesAsync"/>)
/// trên Postgres THẬT. Đợt tối ưu 2026-08-18 gộp 4 round-trip thành 1 query với 2 <c>EXISTS</c>;
/// 3 nhánh dưới đây trước đó chỉ được kiểm thủ công đúng một lần (finding #1, audit 2026-08-18).
///
/// Test chỉ khẳng định về CHÍNH các SysMenu do nó tạo (Contains/DoesNotContain), không khẳng định
/// về tổng số — vì "menu không gán role = mở cho mọi user" nên menu của test khác cũng nằm trong
/// kết quả, đó là đúng thiết kế.
/// </summary>
[Collection(PostgresCollection.Name)]
public class SysMenuRoleRepositoryTests(PostgresFixture fixture)
{
    private static string NewCode() => $"it-menu-{Guid.NewGuid():N}";
    private static string NewRoleName() => $"itrole{Guid.NewGuid():N}";

    private async Task<Guid> AddMenuAsync()
    {
        await using var db = fixture.CreateDbContext();
        var menu = SysMenu.Create(NewCode(), "Menu integration test", "/it", null, null, 999);
        db.SysMenus.Add(menu);
        await db.SaveChangesAsync();
        return menu.Id;
    }

    private async Task<Guid> AddRoleAsync(string roleName)
    {
        await using var db = fixture.CreateDbContext();
        var role = new AppRole(roleName) { Id = Guid.NewGuid(), NormalizedName = roleName.ToUpperInvariant() };
        db.Roles.Add(role);
        await db.SaveChangesAsync();
        return role.Id;
    }

    private async Task RestrictMenuToRoleAsync(Guid sysMenuId, Guid roleId)
    {
        await using var db = fixture.CreateDbContext();
        db.SysMenuRoles.Add(SysMenuRole.Create(sysMenuId, roleId));
        await db.SaveChangesAsync();
    }

    private async Task<HashSet<Guid>> GetVisibleAsync(params string[] roleNames)
    {
        await using var db = fixture.CreateDbContext();
        return await new SysMenuRoleRepository(db).GetVisibleSysMenuIdsForRolesAsync(roleNames, CancellationToken.None);
    }

    [Fact(DisplayName = "Menu KHÔNG có dòng SysMenuRole → mở cho mọi user đã đăng nhập")]
    public async Task MenuWithoutAnyRoleRow_IsVisibleToEveryone()
    {
        var openMenuId = await AddMenuAsync();
        var roleName = NewRoleName();
        await AddRoleAsync(roleName);

        Assert.Contains(openMenuId, await GetVisibleAsync(roleName));
    }

    [Fact(DisplayName = "roleNames rỗng → chỉ thấy menu không bị giới hạn, menu có gán role thì KHÔNG")]
    public async Task EmptyRoleNames_SeesOnlyUnrestrictedMenus()
    {
        var openMenuId = await AddMenuAsync();
        var restrictedMenuId = await AddMenuAsync();
        var roleId = await AddRoleAsync(NewRoleName());
        await RestrictMenuToRoleAsync(restrictedMenuId, roleId);

        var visible = await GetVisibleAsync();

        Assert.Contains(openMenuId, visible);
        Assert.DoesNotContain(restrictedMenuId, visible);
    }

    [Fact(DisplayName = "Role trong claim không tồn tại trong AspNetRoles → như trường hợp không có role")]
    public async Task UnknownRoleName_BehavesLikeNoRole()
    {
        var openMenuId = await AddMenuAsync();
        var restrictedMenuId = await AddMenuAsync();
        var roleId = await AddRoleAsync(NewRoleName());
        await RestrictMenuToRoleAsync(restrictedMenuId, roleId);

        var visible = await GetVisibleAsync("role-khong-ton-tai-" + Guid.NewGuid().ToString("N"));

        Assert.Contains(openMenuId, visible);
        Assert.DoesNotContain(restrictedMenuId, visible);
    }

    [Fact(DisplayName = "Role khớp dòng SysMenuRole → thấy menu bị giới hạn")]
    public async Task MatchingRole_SeesRestrictedMenu()
    {
        var restrictedMenuId = await AddMenuAsync();
        var roleName = NewRoleName();
        var roleId = await AddRoleAsync(roleName);
        await RestrictMenuToRoleAsync(restrictedMenuId, roleId);

        Assert.Contains(restrictedMenuId, await GetVisibleAsync(roleName));
    }

    [Fact(DisplayName = "Role KHÔNG khớp dòng SysMenuRole → không thấy menu bị giới hạn")]
    public async Task NonMatchingRole_DoesNotSeeRestrictedMenu()
    {
        var restrictedMenuId = await AddMenuAsync();
        var ownerRoleId = await AddRoleAsync(NewRoleName());
        await RestrictMenuToRoleAsync(restrictedMenuId, ownerRoleId);

        var otherRoleName = NewRoleName();
        await AddRoleAsync(otherRoleName);

        Assert.DoesNotContain(restrictedMenuId, await GetVisibleAsync(otherRoleName));
    }

    [Fact(DisplayName = "Nhiều role, chỉ 1 role khớp → vẫn thấy menu bị giới hạn")]
    public async Task ManyRoles_OnlyOneMatching_SeesRestrictedMenu()
    {
        var restrictedMenuId = await AddMenuAsync();
        var matchingRoleName = NewRoleName();
        var matchingRoleId = await AddRoleAsync(matchingRoleName);
        await RestrictMenuToRoleAsync(restrictedMenuId, matchingRoleId);

        var otherRoleName = NewRoleName();
        await AddRoleAsync(otherRoleName);

        Assert.Contains(restrictedMenuId, await GetVisibleAsync(otherRoleName, matchingRoleName));
    }
}

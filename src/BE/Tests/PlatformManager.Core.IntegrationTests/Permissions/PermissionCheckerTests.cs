using Microsoft.EntityFrameworkCore;
using PlatformManager.Core.Domain.Entities;
using PlatformManager.Core.Infrastructure.Identity;
using PlatformManager.Core.Infrastructure.Permissions;
using Xunit;

namespace PlatformManager.Core.IntegrationTests.Permissions;

/// <summary>
/// NHÓM B — ngữ nghĩa TRUY VẤN của <see cref="PermissionChecker"/> trên Postgres THẬT.
///
/// Vì sao không test bằng LINQ-to-Objects trên <c>List&lt;T&gt;</c>: cách đó chỉ kiểm được phép
/// tập hợp trong C#, KHÔNG kiểm được EF Core còn dịch đúng sang SQL sau khi nâng version — mà
/// đúng những nhánh dưới đây (INNER JOIN loại dòng mồ côi, <c>NULL IN (...)</c>) là hành vi của
/// SQL chứ không phải của C#.
///
/// Mỗi test tự sinh role/ResourceKey riêng (Guid) nên không phụ thuộc dữ liệu seed và không đụng
/// nhau.
/// </summary>
[Collection(PostgresCollection.Name)]
public class PermissionCheckerTests(PostgresFixture fixture)
{
    private static string NewKey() => $"it.{Guid.NewGuid():N}";
    private static string NewRoleName() => $"itrole{Guid.NewGuid():N}";

    private async Task<Guid> AddRoleAsync(string? roleName)
    {
        await using var db = fixture.CreateDbContext();
        var role = new AppRole { Id = Guid.NewGuid(), Name = roleName, NormalizedName = roleName?.ToUpperInvariant() };
        db.Roles.Add(role);
        await db.SaveChangesAsync();
        return role.Id;
    }

    private async Task GrantAsync(Guid roleId, string resourceKey)
    {
        await using var db = fixture.CreateDbContext();
        db.RolePermissions.Add(RolePermission.Create(roleId, resourceKey));
        await db.SaveChangesAsync();
    }

    private async Task<bool> HasPermissionAsync(string[] roleNames, string resourceKey)
    {
        await using var db = fixture.CreateDbContext();
        return await new PermissionChecker(db).HasPermissionAsync(roleNames, resourceKey, CancellationToken.None);
    }

    [Fact(DisplayName = "Role được cấp ResourceKey → true")]
    public async Task GrantedRole_ReturnsTrue()
    {
        var key = NewKey();
        var roleName = NewRoleName();
        var roleId = await AddRoleAsync(roleName);
        await GrantAsync(roleId, key);

        Assert.True(await HasPermissionAsync([roleName], key));
    }

    /// <summary>
    /// TRÁI TIM của finding #1 — chứng minh KHÔNG có cache/TTL nào ở đường phân quyền: thu hồi
    /// có hiệu lực NGAY ở lần kiểm kế tiếp, cấp lại cũng vậy. Nếu sau này ai đó thêm cache mà
    /// quên invalidate, test này đỏ. Xem
    /// doc/huong_dan/wiki-core/be/11-performance-caching.md §6.2 quyết định #5.
    /// </summary>
    [Fact(DisplayName = "Thu hồi → false NGAY; cấp lại → true NGAY (không cache, không TTL)")]
    public async Task RevokeThenGrant_TakesEffectImmediately()
    {
        var key = NewKey();
        var roleName = NewRoleName();
        var roleId = await AddRoleAsync(roleName);
        await GrantAsync(roleId, key);

        Assert.True(await HasPermissionAsync([roleName], key));

        await using (var db = fixture.CreateDbContext())
        {
            var granted = await db.RolePermissions.SingleAsync(x => x.RoleId == roleId && x.ResourceKey == key);
            db.RolePermissions.Remove(granted);
            await db.SaveChangesAsync();
        }

        Assert.False(await HasPermissionAsync([roleName], key));

        await GrantAsync(roleId, key);

        Assert.True(await HasPermissionAsync([roleName], key));
    }

    [Fact(DisplayName = "Role trong claim không tồn tại trong AspNetRoles → false")]
    public async Task UnknownRoleName_ReturnsFalse()
    {
        var key = NewKey();
        var roleId = await AddRoleAsync(NewRoleName());
        await GrantAsync(roleId, key);

        Assert.False(await HasPermissionAsync(["role-khong-ton-tai-" + Guid.NewGuid().ToString("N")], key));
    }

    [Fact(DisplayName = "Role hợp lệ nhưng KHÔNG được cấp ResourceKey → false")]
    public async Task ValidRole_WithoutTheKey_ReturnsFalse()
    {
        var roleName = NewRoleName();
        var roleId = await AddRoleAsync(roleName);
        await GrantAsync(roleId, NewKey()); // được cấp key KHÁC

        Assert.False(await HasPermissionAsync([roleName], NewKey()));
    }

    [Fact(DisplayName = "Nhiều role, chỉ 1 role có quyền → true")]
    public async Task ManyRoles_OnlyOneGranted_ReturnsTrue()
    {
        var key = NewKey();
        var grantedRoleName = NewRoleName();
        var otherRoleName = NewRoleName();
        var grantedRoleId = await AddRoleAsync(grantedRoleName);
        await AddRoleAsync(otherRoleName);
        await GrantAsync(grantedRoleId, key);

        Assert.True(await HasPermissionAsync([otherRoleName, grantedRoleName], key));
    }

    /// <summary>
    /// Nhánh "RolePermission mồ côi" mà reviewer nêu: trên schema thật KHÔNG thể tạo dòng mồ côi
    /// bằng tay (FK RolePermissions → AspNetRoles). Đường sinh ra nó thực tế là XOÁ ROLE — và FK
    /// khai <c>ON DELETE CASCADE</c> nên dòng quyền biến mất theo. Test đúng hành vi thật đó:
    /// xoá role thì mọi quyền của role đó hết hiệu lực ngay, không còn dòng treo lại.
    /// </summary>
    [Fact(DisplayName = "Xoá role → RolePermission bị CASCADE, không còn dòng mồ côi → false")]
    public async Task DeletingRole_CascadesPermissions_LeavingNoOrphanGrant()
    {
        var key = NewKey();
        var roleName = NewRoleName();
        var roleId = await AddRoleAsync(roleName);
        await GrantAsync(roleId, key);

        await using (var db = fixture.CreateDbContext())
        {
            db.Roles.Remove(await db.Roles.SingleAsync(r => r.Id == roleId));
            await db.SaveChangesAsync();
        }

        await using (var db = fixture.CreateDbContext())
        {
            Assert.False(await db.RolePermissions.AnyAsync(x => x.RoleId == roleId));
        }

        Assert.False(await HasPermissionAsync([roleName], key));
    }

    /// <summary>
    /// <c>AspNetRoles.Name</c> là cột NULL được. Trong SQL <c>NULL IN (...)</c> ra NULL chứ không
    /// phải TRUE → dòng đó không bao giờ khớp. Test chốt cứng: role không tên KHÔNG được vô tình
    /// khớp với bất kỳ claim nào (kể cả claim rỗng chuỗi).
    /// </summary>
    [Fact(DisplayName = "AspNetRoles.Name NULL → không khớp claim nào → false (deny-by-default)")]
    public async Task RoleWithNullName_NeverMatches()
    {
        var key = NewKey();
        var roleId = await AddRoleAsync(roleName: null);
        await GrantAsync(roleId, key);

        Assert.False(await HasPermissionAsync([""], key));
        Assert.False(await HasPermissionAsync(["null"], key));
    }
}

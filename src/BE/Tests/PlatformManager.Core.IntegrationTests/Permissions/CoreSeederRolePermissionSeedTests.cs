using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlatformManager.Core.Application.Common;
using PlatformManager.Core.Application.Permissions;
using PlatformManager.Core.Infrastructure.Persistence;
using Xunit;

namespace PlatformManager.Core.IntegrationTests.Permissions;

/// <summary>
/// <c>CoreSeeder.SeedRolePermissionsAsync</c> đọc vai được cấp từ
/// <see cref="ResourceKeyDefinition.SeedRoles"/> (mở rộng 2026-09-09) thay vì lặp cứng
/// <c>[Admin, User]</c> cho mọi key.
///
/// <para><b>Hai khẳng định của bộ này có sức nặng khác nhau, và cái thứ hai nặng hơn:</b></para>
/// <list type="number">
/// <item>Key khai vai HẸP → chỉ vai đó có dòng. Đây là năng lực mới.</item>
/// <item>Key KHÔNG khai gì → vẫn Admin + User, y như trước. Đây là thứ dễ mất nhất: seed là đường
/// dùng chung, và một mặc định trượt đi sẽ đổi quyền của mọi key hiện có cùng lúc, im lặng, chỉ
/// lộ ra thành 403 ở nơi trước đó chạy được.</item>
/// </list>
///
/// <para><b>Vì sao phải là INTEGRATION test:</b> thứ cần kiểm là TRẠNG THÁI bảng
/// <c>RolePermissions</c> sau khi seeder chạy — đọc lại từ một <c>DbContext</c> khác. Đọc từ chính
/// context của seeder sẽ "thấy" cả những dòng mới nằm trong change tracker, tức không phân biệt
/// được "đã ghi" với "định ghi".</para>
///
/// <para><b>Vì sao dùng nguồn GIẢ:</b> cùng lý lẽ đã ghi ở <c>CoreSeederMenuSeedSourceTests</c> —
/// bám vào danh mục key thật của host sẽ kéo tên key của DỰ ÁN vào một test của CORE, và sẽ đỏ mỗi
/// lần dự án thêm/bớt key vì lý do không liên quan.</para>
///
/// <para><b>Cô lập:</b> mọi key do bộ test này sinh ra mang tiền tố <see cref="KeyPrefix"/> nên
/// không đụng dòng seed thật; <see cref="DisposeAsync"/> xoá cứng chúng sau MỖI test.</para>
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class CoreSeederRolePermissionSeedTests : IAsyncLifetime
{
    private const string KeyPrefix = "it-seedrole-";

    private readonly PostgresFixture _fixture;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _prefix;

    public CoreSeederRolePermissionSeedTests(PostgresFixture fixture)
    {
        _fixture = fixture;

        // PHẢI đặt trước khi host boot — xem IntegrationTestHostEnvironment.
        IntegrationTestHostEnvironment.Configure(fixture.ConnectionString);

        _factory = new WebApplicationFactory<Program>();
        _prefix = $"{KeyPrefix}{Guid.NewGuid():N}-";
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await using (var db = _fixture.CreateDbContext())
        {
            await db.Database.ExecuteSqlRawAsync(
                @"DELETE FROM core.""RolePermissions"" WHERE ""ResourceKey"" LIKE {0}", $"{KeyPrefix}%");
        }

        await _factory.DisposeAsync();
    }

    [Fact(DisplayName = "Key khai SeedRoles = [Admin] → CHỈ Admin có dòng; User và SuperAdmin không có")]
    public async Task NarrowSeedRoles_GrantsOnlyThatRole()
    {
        var narrowKey = $"{_prefix}hep";

        await RunSeederAsync(new ResourceKeyDefinition(narrowKey, "Key hẹp") { SeedRoles = [Roles.Admin] });

        Assert.Equal([Roles.Admin], await RolesGrantedAsync(narrowKey));
    }

    /// <summary>
    /// Nghiệm thu "không đổi hành vi" của lượt mở rộng. Không có test này thì việc mặc định trượt
    /// khỏi <c>[Admin, User]</c> không có gì bắt được — mọi test còn lại của repo đều dùng
    /// SuperAdmin hoặc không đi qua <c>[RequirePermission]</c>.
    /// </summary>
    [Fact(DisplayName = "Key KHÔNG khai SeedRoles → vẫn Admin + User như trước lượt mở rộng")]
    public async Task DefaultSeedRoles_StillGrantAdminAndUser()
    {
        var defaultKey = $"{_prefix}mac-dinh";

        await RunSeederAsync(new ResourceKeyDefinition(defaultKey, "Key mặc định"));

        Assert.Equal([Roles.Admin, Roles.User], await RolesGrantedAsync(defaultKey));
    }

    /// <summary>
    /// Hai key khai vai khác nhau phải seed ĐỘC LẬP trong CÙNG một lượt. Tách thành hai lượt seed
    /// sẽ không đo được điều đang lo thật: seeder lặp theo VAI ở vòng ngoài, nên một cài đặt sai
    /// rất dễ cấp cho vai đang lặp toàn bộ danh mục thay vì đúng phần key khai vai đó.
    /// </summary>
    [Fact(DisplayName = "Hai key vai khác nhau trong cùng lượt seed → không key nào lây vai của key kia")]
    public async Task MixedCatalog_DoesNotLeakRolesBetweenKeys()
    {
        var narrowKey = $"{_prefix}mix-hep";
        var defaultKey = $"{_prefix}mix-mac-dinh";

        await RunSeederAsync(
            new ResourceKeyDefinition(narrowKey, "Key hẹp") { SeedRoles = [Roles.Admin] },
            new ResourceKeyDefinition(defaultKey, "Key mặc định"));

        Assert.Equal([Roles.Admin], await RolesGrantedAsync(narrowKey));
        Assert.Equal([Roles.Admin, Roles.User], await RolesGrantedAsync(defaultKey));
    }

    /// <summary>Lệnh <c>--seed</c> chạy được nhiều lần — lượt hai không được nhân đôi dòng nào.</summary>
    [Fact(DisplayName = "Seed hai lần → không nhân đôi dòng (idempotent)")]
    public async Task SecondRun_IsIdempotent()
    {
        var narrowKey = $"{_prefix}lap-lai";
        var definition = new ResourceKeyDefinition(narrowKey, "Key hẹp") { SeedRoles = [Roles.Admin] };

        await RunSeederAsync(definition);
        await RunSeederAsync(definition);

        await using var db = _fixture.CreateDbContext();
        Assert.Equal(1, await db.RolePermissions.CountAsync(x => x.ResourceKey == narrowKey));
    }

    /// <summary>
    /// Host khai sai phải hỏng TRƯỚC khi ghi dòng nào — nửa bảng quyền còn tệ hơn không seed, vì
    /// lượt chạy sau thấy "đã có" và không bao giờ dựng lại cho đủ.
    /// </summary>
    [Fact(DisplayName = "Host khai vai lạ → ném, và KHÔNG dòng nào được ghi")]
    public async Task InvalidRoleName_Throws_AndWritesNothing()
    {
        var goodKey = $"{_prefix}hop-le";
        var badKey = $"{_prefix}vai-la";

        await Assert.ThrowsAsync<InvalidOperationException>(() => RunSeederAsync(
            new ResourceKeyDefinition(goodKey, "Key hợp lệ"),
            new ResourceKeyDefinition(badKey, "Key khai vai lạ") { SeedRoles = ["Admn"] }));

        await using var db = _fixture.CreateDbContext();
        Assert.Equal(0, await db.RolePermissions
            .CountAsync(x => x.ResourceKey == goodKey || x.ResourceKey == badKey));
    }

    // ───────────────────────────── Hạ tầng của test ─────────────────────────────

    private sealed class StubResourceKeySource(IReadOnlyCollection<ResourceKeyDefinition> definitions)
        : ICoreResourceKeySource
    {
        public IReadOnlyCollection<ResourceKeyDefinition> GetResourceKeys() => definitions;
    }

    /// <summary>
    /// Chạy <c>CoreSeeder</c> THẬT với đúng đồ hình DI của host, chỉ thay MỘT dependency là danh
    /// mục permission-key — cùng khuôn với <c>CoreSeederMenuSeedSourceTests.RunSeederAsync</c>.
    /// <c>SeedRolePermissionsAsync</c> là <c>private</c> nên phải đi qua <c>SeedAsync()</c>; ba
    /// bước còn lại idempotent và đã chạy ở fixture.
    /// </summary>
    private async Task RunSeederAsync(params ResourceKeyDefinition[] definitions)
    {
        using var scope = _factory.Services.CreateScope();

        var seeder = ActivatorUtilities.CreateInstance<CoreSeeder>(
            scope.ServiceProvider, new StubResourceKeySource(definitions));

        await seeder.SeedAsync();
    }

    /// <summary>
    /// Tên các vai ĐANG có dòng <c>RolePermissions</c> cho key này, sắp theo thứ tự
    /// <c>Roles.All</c> để phép so bằng danh sách ổn định. Đọc lại từ một context khác context của
    /// seeder — xem docstring của class.
    ///
    /// <para>Trả về TÊN chứ không phải số đếm: một khẳng định "có 1 dòng" không phân biệt được
    /// "đúng Admin" với "đúng User", mà đó chính là thứ đang đo.</para>
    /// </summary>
    private async Task<IReadOnlyList<string>> RolesGrantedAsync(string resourceKey)
    {
        await using var db = _fixture.CreateDbContext();

        var roleNames = await db.RolePermissions
            .AsNoTracking()
            .Where(permission => permission.ResourceKey == resourceKey)
            .Join(db.Roles, permission => permission.RoleId, role => role.Id, (_, role) => role.Name!)
            .ToListAsync();

        return [.. Roles.All.Where(name => roleNames.Contains(name, StringComparer.Ordinal))];
    }
}

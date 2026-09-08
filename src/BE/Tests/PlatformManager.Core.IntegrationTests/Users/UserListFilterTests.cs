using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using PlatformManager.Core.Application.Common;
using PlatformManager.Core.Application.Common.Models;
using PlatformManager.Core.Application.Users;
using PlatformManager.Core.IntegrationTests.Auth;
using PlatformManager.Core.Infrastructure.Identity;
using Xunit;

namespace PlatformManager.Core.IntegrationTests.Users;

/// <summary>
/// CONTRACT USER-6 — <c>GET /api/users</c> lọc theo Vai trò + Trạng thái
/// (xem doc/contracts/users.md).
///
/// <para><b>Vì sao là integration test chứ không unit test:</b> thứ đang kiểm là biểu thức LINQ
/// có được EF dịch sang SQL hay không và <c>TotalCount</c> có đếm SAU khi lọc hay không. Một
/// <c>IUserAdminService</c> giả sẽ xanh với cả bản hiện thực sai (lọc trong bộ nhớ sau khi phân
/// trang) — đúng hai lỗi mà contract này sinh ra để chặn.</para>
///
/// <para><b>Dữ liệu test được khoanh vùng bằng <c>SearchText</c>:</b> database dùng chung cho cả
/// collection và đã có sẵn 2 tài khoản bootstrap (SuperAdmin/Admin) do
/// <c>PostgresFixture.SeedCoreAsync()</c> tạo, nên khẳng
/// định trên tổng số dòng toàn bảng sẽ vỡ khi có test khác thêm user. Mỗi lần chạy sinh một
/// <c>_token</c> riêng, mọi khẳng định đều đi kèm bộ lọc theo token đó.</para>
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class UserListFilterTests : IAsyncLifetime, IDisposable
{
    private const string Password = "Test@123456789";

    private readonly SessionTerminationFactory _factory;

    /// <summary>Nhãn duy nhất của lần chạy này — vừa là <c>SearchText</c> khoanh vùng, vừa là
    /// tiền tố UserName để không đụng tài khoản của test khác.</summary>
    private readonly string _token = $"flt{Guid.NewGuid():N}"[..11];

    private Guid _adminUnlocked;
    private Guid _adminLocked;
    private Guid _userUnlocked;
    private Guid _userLocked;
    private Guid _adminAndUser;

    public UserListFilterTests(PostgresFixture fixture)
    {
        IntegrationTestHostEnvironment.Configure(fixture.ConnectionString);
        _factory = new SessionTerminationFactory();
    }

    public void Dispose() => _factory.Dispose();

    public async Task InitializeAsync()
    {
        _adminUnlocked = await CreateUserAsync("1", [Roles.Admin], locked: false);
        _adminLocked = await CreateUserAsync("2", [Roles.Admin], locked: true);
        _userUnlocked = await CreateUserAsync("3", [Roles.User], locked: false);
        _userLocked = await CreateUserAsync("4", [Roles.User], locked: true);

        // User MANG 2 ROLE — bắt buộc phải có trong bộ dữ liệu: nếu bộ lọc role viết bằng phép
        // join thẳng vào AspNetUserRoles thay vì subquery id, người này sẽ xuất hiện HAI LẦN và
        // TotalCount đếm dư. Lỗi đó chỉ lộ ra khi có user nhiều role.
        _adminAndUser = await CreateUserAsync("5", [Roles.Admin, Roles.User], locked: false);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact(DisplayName = "Không truyền role/isLocked → không áp điều kiện nào")]
    public async Task NoFilter_ReturnsEveryUser()
    {
        var result = await GetListAsync(new UserListFilter(_token));

        Assert.Equal(5, result.TotalCount);
        AssertContainsExactly(result, _adminUnlocked, _adminLocked, _userUnlocked, _userLocked, _adminAndUser);

        // UserDto.Roles vẫn phải đầy đủ: FE dựa vào nó để gửi lại nguyên tập role khi PUT
        // (xem §Luật cấp/gỡ role SuperAdmin) — trả thiếu ở đây là âm thầm hạ quyền ở đường ghi.
        var multiRole = result.Items.Single(u => u.Id == _adminAndUser);
        Assert.Equal<string[]>([Roles.Admin, Roles.User], [.. multiRole.Roles.Order()]);
    }

    [Fact(DisplayName = "Lọc theo role → chỉ user mang role đó, user nhiều role KHÔNG bị đếm 2 lần")]
    public async Task RoleFilter_ReturnsOnlyUsersInRole()
    {
        var admins = await GetListAsync(new UserListFilter(_token, Role: Roles.Admin));

        Assert.Equal(3, admins.TotalCount);
        AssertContainsExactly(admins, _adminUnlocked, _adminLocked, _adminAndUser);
        Assert.All(admins.Items, dto => Assert.Contains(Roles.Admin, dto.Roles));

        var users = await GetListAsync(new UserListFilter(_token, Role: Roles.User));

        Assert.Equal(3, users.TotalCount);
        AssertContainsExactly(users, _userUnlocked, _userLocked, _adminAndUser);
    }

    [Fact(DisplayName = "Lọc theo role không ai mang → rỗng, không phải trả về tất cả")]
    public async Task RoleFilter_WithNoMatch_ReturnsEmpty()
    {
        var result = await GetListAsync(new UserListFilter(_token, Role: Roles.SuperAdmin));

        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }

    [Fact(DisplayName = "Lọc theo trạng thái → khớp đúng định nghĩa isLocked của UserDto")]
    public async Task IsLockedFilter_MatchesDtoDefinition()
    {
        var locked = await GetListAsync(new UserListFilter(_token, IsLocked: true));

        Assert.Equal(2, locked.TotalCount);
        AssertContainsExactly(locked, _adminLocked, _userLocked);

        // Bộ lọc và cột hiển thị phải nói cùng một điều — lệch nhau nghĩa là dòng lọt bộ lọc
        // "Đã khoá" lại hiển thị badge "Đang hoạt động".
        Assert.All(locked.Items, dto => Assert.True(dto.IsLocked));

        var active = await GetListAsync(new UserListFilter(_token, IsLocked: false));

        Assert.Equal(3, active.TotalCount);
        AssertContainsExactly(active, _adminUnlocked, _userUnlocked, _adminAndUser);
        Assert.All(active.Items, dto => Assert.False(dto.IsLocked));
    }

    [Fact(DisplayName = "Kết hợp role + trạng thái → giao của hai điều kiện (AND)")]
    public async Task RoleAndIsLocked_AreCombinedWithAnd()
    {
        var activeAdmins = await GetListAsync(new UserListFilter(_token, Roles.Admin, IsLocked: false));

        Assert.Equal(2, activeAdmins.TotalCount);
        AssertContainsExactly(activeAdmins, _adminUnlocked, _adminAndUser);

        var lockedAdmins = await GetListAsync(new UserListFilter(_token, Roles.Admin, IsLocked: true));

        Assert.Equal(1, lockedAdmins.TotalCount);
        AssertContainsExactly(lockedAdmins, _adminLocked);
    }

    [Fact(DisplayName = "TotalCount là tổng SAU khi lọc trên TOÀN BẢNG, không phải số dòng còn lại của trang")]
    public async Task TotalCount_CountsFilteredRowsAcrossAllPages()
    {
        // Đây là test chốt của contract. Bản hiện thực sai (lấy 1 trang rồi lọc trong bộ nhớ)
        // sẽ trả TotalCount = 1 và trang 2 rỗng — thanh phân trang khi đó báo 1 trang trong khi
        // có 3 dòng khớp.
        var page1 = await GetListAsync(new UserListFilter(_token, Roles.Admin), page: 1, pageSize: 1);

        Assert.Equal(3, page1.TotalCount);
        Assert.Single(page1.Items);

        var page3 = await GetListAsync(new UserListFilter(_token, Roles.Admin), page: 3, pageSize: 1);

        Assert.Equal(3, page3.TotalCount);
        Assert.Single(page3.Items);

        // 3 trang × 1 dòng phải phủ đúng 3 user khớp, không trùng nhau.
        var page2 = await GetListAsync(new UserListFilter(_token, Roles.Admin), page: 2, pageSize: 1);
        var ids = new[] { page1, page2, page3 }.SelectMany(p => p.Items).Select(u => u.Id).ToList();

        Assert.Equal(3, ids.Distinct().Count());
        Assert.Equal(
            new[] { _adminUnlocked, _adminLocked, _adminAndUser }.Order(),
            ids.Order());
    }

    private static void AssertContainsExactly(PagedList<UserDto> result, params Guid[] expected)
    {
        Assert.Equal(expected.Order(), result.Items.Select(u => u.Id).Order());
    }

    private async Task<PagedList<UserDto>> GetListAsync(UserListFilter filter, int page = 1, int pageSize = 50)
    {
        using var scope = _factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IUserAdminService>()
            .GetListAsync(page, pageSize, filter, CancellationToken.None);
    }

    private async Task<Guid> CreateUserAsync(string suffix, string[] roles, bool locked)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var userName = $"{_token}-{suffix}";
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = userName,
            Email = $"{userName}@it.local",
            // Token nằm trong FullName để SearchText khoanh vùng được, và số thứ tự đứng cuối
            // để OrderBy(FullName) của GetListAsync cho thứ tự xác định giữa các trang.
            FullName = $"Loc {_token} {suffix}",
            DateCreate = DateTimeOffset.UtcNow,
        };

        var created = await userManager.CreateAsync(user, Password);
        Assert.True(created.Succeeded, string.Join("; ", created.Errors.Select(e => e.Description)));

        var roleResult = await userManager.AddToRolesAsync(user, roles);
        Assert.True(roleResult.Succeeded, string.Join("; ", roleResult.Errors.Select(e => e.Description)));

        if (locked)
        {
            var lockResult = await scope.ServiceProvider.GetRequiredService<IUserAdminService>()
                .LockAsync(user.Id, CancellationToken.None);
            Assert.True(lockResult);
        }

        return user.Id;
    }
}

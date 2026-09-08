using System.Data.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using PlatformManager.Api.Modules;
using PlatformManager.Core.Application.Common.Models;
using PlatformManager.Core.Application.Users;
using PlatformManager.Core.Infrastructure.Identity;
using PlatformManager.Core.Infrastructure.Persistence;
using Xunit;

namespace PlatformManager.Core.IntegrationTests.Users;

/// <summary>
/// Finding BE-2 (audit 2026-08-29) — <c>GET /api/users</c> KHÔNG được đi DB một lần cho mỗi dòng.
///
/// <para>Bản trước dựng DTO bằng <c>foreach (var user in items) await ToDtoAsync(user)</c>, mà
/// <c>ToDtoAsync</c> gọi <c>UserManager.GetRolesAsync</c> → <c>pageSize=20</c> thành 21 query
/// (N+1, cấm ở doc/huong_dan/quy-uoc/be-performance.md §"Khi viết repository/query mới").</para>
///
/// <para><b>Cách đo:</b> đếm THẲNG số câu lệnh gửi xuống Postgres bằng
/// <see cref="DbCommandInterceptor"/>, rồi so số lệnh của một trang 1 dòng với một trang 5 dòng.
/// Khẳng định cốt lõi là <b>hai con số bằng nhau</b> — đó chính là định nghĩa "không N+1", và nó
/// không phụ thuộc vào việc hôm nay hiện thực dùng đúng mấy query. Một test chỉ so sánh kết quả
/// trả về sẽ XANH với cả bản N+1, vì bản đó cũng trả đúng dữ liệu; chỉ có số round-trip mới phân
/// biệt được hai bản.</para>
///
/// <para><b>Vì sao tự dựng ServiceProvider thay vì dùng host WebApplicationFactory:</b> cần gắn
/// interceptor vào ĐÚNG <c>DbContextOptions</c> mà service đang dùng. Host thật đăng ký
/// interceptor tường minh trong <c>AddCoreModule</c> nên không có chỗ chèn thêm từ bên ngoài mà
/// không sửa code sản phẩm. Provider ở đây dùng CHÍNH <c>PlatformManagerDbContext</c> và
/// <c>UserAdminService</c> thật, chỉ khác cách nối dây.</para>
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class UserListRoleBatchingTests : IAsyncLifetime, IDisposable
{
    private const string Password = "Test@123456789";

    private readonly PostgresFixture _fixture;
    private readonly CommandCountingInterceptor _commands = new();
    private readonly ServiceProvider _provider;

    /// <summary>Nhãn riêng của lần chạy — database dùng chung cho cả collection, mọi khẳng định
    /// phải khoanh vùng theo nhãn này (cùng khuôn với UserListFilterTests).</summary>
    private readonly string _token = $"btc{Guid.NewGuid():N}"[..11];

    private Guid _noRoleUserId;
    private Guid _twoRoleUserId;
    private string _roleA = string.Empty;
    private string _roleB = string.Empty;

    public UserListRoleBatchingTests(PostgresFixture fixture)
    {
        _fixture = fixture;

        var services = new ServiceCollection();
        services.AddLogging();
        // Đọc từ CHÍNH danh sách tầng của host, KHÔNG chép tay một assembly duy nhất (sửa
        // 2026-09-08, cùng lý do đã ghi ở PostgresFixture.CreateDbContext): tầng nghiệp vụ đầu tiên
        // xuất hiện thì model dựng ở đây tự có entity của nó, thay vì thiếu trong im lặng.
        foreach (var registrar in HostModuleRegistrars.Create())
            services.AddSingleton(new EfConfigurationAssembly(registrar.PersistenceAssembly));
        services.AddDbContext<PlatformManagerDbContext>(options => options
            .UseNpgsql(fixture.ConnectionString, npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "core"))
            .AddInterceptors(_commands));
        services.AddIdentityCore<AppUser>()
            .AddRoles<AppRole>()
            .AddEntityFrameworkStores<PlatformManagerDbContext>();

        _provider = services.BuildServiceProvider();
    }

    public void Dispose() => _provider.Dispose();

    public async Task InitializeAsync()
    {
        _roleA = await CreateRoleAsync();
        _roleB = await CreateRoleAsync();

        _noRoleUserId = await CreateUserAsync("1", []);
        await CreateUserAsync("2", [_roleA]);
        await CreateUserAsync("3", [_roleA]);
        await CreateUserAsync("4", [_roleB]);
        _twoRoleUserId = await CreateUserAsync("5", [_roleA, _roleB]);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact(DisplayName = "Số round-trip DB KHÔNG tăng theo số dòng của trang (N+1 đã bị chặn)")]
    public async Task RoundTripCount_DoesNotGrowWithPageSize()
    {
        // Lượt làm nóng: lần gọi đầu của một DbContext mới còn kèm chi phí một lần (dựng model,
        // mở connection). Không loại nó ra thì con số đo được lẫn nhiễu.
        await GetListAsync(pageSize: 5);

        _commands.Reset();
        await GetListAsync(pageSize: 1);
        var commandsForOneRow = _commands.Count;

        _commands.Reset();
        var fiveRows = await GetListAsync(pageSize: 5);
        var commandsForFiveRows = _commands.Count;

        Assert.Equal(5, fiveRows.Items.Count);

        // Đây là khẳng định chốt. Bản N+1 cũ: 3 lệnh cho 1 dòng, 7 lệnh cho 5 dòng.
        Assert.Equal(commandsForOneRow, commandsForFiveRows);

        // Trần tuyệt đối để bản sửa không âm thầm phình ra kiểu khác (vd thêm một query phụ mỗi
        // lần gọi): đếm + lấy trang + lấy role của cả trang = 3.
        Assert.True(
            commandsForFiveRows <= 3,
            $"Một trang danh sách user chỉ được tốn tối đa 3 lệnh SQL, đo được {commandsForFiveRows}.");
    }

    [Fact(DisplayName = "Gộp role theo lô vẫn trả ĐÚNG role của từng dòng, kể cả user 0 role và user 2 role")]
    public async Task BatchedRoles_AreMappedToTheRightUser()
    {
        var result = await GetListAsync(pageSize: 10);

        // Ghép sai user↔role là lỗi im lặng nguy hiểm hơn cả N+1: màn quản trị hiển thị quyền của
        // người khác, và FE gửi lại nguyên tập role đó khi PUT (xem doc/contracts/users.md
        // §Luật cấp/gỡ role SuperAdmin) — tức lỗi hiển thị biến thành lỗi GHI.
        var noRole = result.Items.Single(u => u.Id == _noRoleUserId);
        Assert.Empty(noRole.Roles);

        var twoRoles = result.Items.Single(u => u.Id == _twoRoleUserId);
        Assert.Equal<string[]>([.. new[] { _roleA, _roleB }.Order()], [.. twoRoles.Roles.Order()]);

        Assert.Equal(4, result.Items.Count(u => u.Roles.Contains(_roleA) || u.Roles.Contains(_roleB)));
    }

    private async Task<PagedList<UserDto>> GetListAsync(int pageSize)
    {
        using var scope = _provider.CreateScope();
        var service = new UserAdminService(
            scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>(),
            scope.ServiceProvider.GetRequiredService<PlatformManagerDbContext>());

        return await service.GetListAsync(1, pageSize, new UserListFilter(_token), CancellationToken.None);
    }

    private async Task<string> CreateRoleAsync()
    {
        var name = $"itrole{Guid.NewGuid():N}";

        await using var db = _fixture.CreateDbContext();
        db.Roles.Add(new AppRole(name) { Id = Guid.NewGuid(), NormalizedName = name.ToUpperInvariant() });
        await db.SaveChangesAsync();

        return name;
    }

    private async Task<Guid> CreateUserAsync(string suffix, string[] roles)
    {
        using var scope = _provider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var userName = $"{_token}-{suffix}";
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = userName,
            Email = $"{userName}@it.local",
            // Token nằm trong FullName để SearchText khoanh vùng được; số thứ tự đứng cuối để
            // OrderBy(FullName) cho thứ tự xác định.
            FullName = $"Lo {_token} {suffix}",
            DateCreate = DateTimeOffset.UtcNow,
        };

        var created = await userManager.CreateAsync(user, Password);
        Assert.True(created.Succeeded, string.Join("; ", created.Errors.Select(e => e.Description)));

        if (roles.Length > 0)
        {
            var roleResult = await userManager.AddToRolesAsync(user, roles);
            Assert.True(roleResult.Succeeded, string.Join("; ", roleResult.Errors.Select(e => e.Description)));
        }

        return user.Id;
    }

    /// <summary>
    /// Đếm MỌI câu lệnh gửi xuống database. Đếm cả 3 loại (reader/scalar/non-query) vì EF chọn
    /// loại nào là chi tiết nội bộ — đếm thiếu một loại thì test xanh giả khi hiện thực đổi cách
    /// truy vấn.
    /// </summary>
    private sealed class CommandCountingInterceptor : DbCommandInterceptor
    {
        private int _count;

        public int Count => Volatile.Read(ref _count);

        public void Reset() => Interlocked.Exchange(ref _count, 0);

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
        {
            Interlocked.Increment(ref _count);
            return result;
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _count);
            return ValueTask.FromResult(result);
        }

        public override InterceptionResult<object> ScalarExecuting(
            DbCommand command, CommandEventData eventData, InterceptionResult<object> result)
        {
            Interlocked.Increment(ref _count);
            return result;
        }

        public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<object> result,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _count);
            return ValueTask.FromResult(result);
        }

        public override InterceptionResult<int> NonQueryExecuting(
            DbCommand command, CommandEventData eventData, InterceptionResult<int> result)
        {
            Interlocked.Increment(ref _count);
            return result;
        }

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _count);
            return ValueTask.FromResult(result);
        }
    }
}

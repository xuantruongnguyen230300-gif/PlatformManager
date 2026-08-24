using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlatformManager.Core.Application.Auth;
using PlatformManager.Core.Application.Common;
using PlatformManager.Core.Infrastructure.Identity;
using Xunit;

namespace PlatformManager.Core.IntegrationTests.Auth;

/// <summary>
/// <c>SuperAdmin</c> KHÔNG bị khoá tài khoản do sai mật khẩu (quyết định người dùng 2026-08-20,
/// thực thi 2026-08-21 tại <see cref="IdentityService"/>).
///
/// Vấn đề gốc: <c>lockoutOnFailure: true</c> cho phép BẤT KỲ AI — không cần tài khoản, chưa đăng
/// nhập — khoá 15 phút một tài khoản bất kỳ bằng 5 lần đoán sai. Với tài khoản break-glass
/// <c>SuperAdmin</c> đó là đường để người ngoài tự khoá quản trị viên ra khỏi hệ thống;
/// <c>SuperAdminAccountGuard</c> chặn 4 đường ghi có xác thực nhưng KHÔNG chạm tới đường này vì
/// nó nằm TRƯỚC lúc đăng nhập.
///
/// Vì sao test ở tầng <see cref="IIdentityService"/> chứ không qua HTTP: kịch bản cần <b>6+ lượt
/// đăng nhập</b> (5 lần sai + 1 lần kiểm), trong khi policy rate limit "login" là 5 request/phút
/// và mọi request trong <c>TestServer</c> rơi chung khoá <c>"unknown-ip"</c>. Đi qua HTTP thì test
/// sẽ đỏ vì <b>429</b> — một lý do hoàn toàn khác thứ đang kiểm. Rate limit đã có bộ test riêng ở
/// <c>RateLimiting/</c>.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class SuperAdminLockoutExemptionTests : IAsyncLifetime
{
    private const string Password = "Test@12345";
    private const string WrongPassword = "Sai@00000";

    /// <summary>Khớp <c>options.Lockout.MaxFailedAccessAttempts</c> ở
    /// <c>Core.Infrastructure/DependencyInjection.cs</c>.</summary>
    private const int MaxFailedAttempts = 5;

    private readonly PostgresFixture _fixture;
    private readonly SessionTerminationFactory _factory;

    public SuperAdminLockoutExemptionTests(PostgresFixture fixture)
    {
        _fixture = fixture;
        IntegrationTestHostEnvironment.Configure(fixture.ConnectionString);
        _factory = new SessionTerminationFactory();
    }

    public Task InitializeAsync() => EnsureRolesAsync();

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Chiều NGƯỢC — chứng minh cơ chế khoá vẫn hoạt động bình thường. Thiếu ca này thì test dưới
    /// vẫn xanh kể cả khi ai đó tắt lockout cho TOÀN BỘ người dùng.
    /// </summary>
    [Fact(DisplayName = "User thường: 5 lần sai mật khẩu → BỊ khoá")]
    public async Task NormalUser_AfterMaxFailedAttempts_IsLockedOut()
    {
        var userName = NewUserName("normal");
        var userId = await CreateUserAsync(userName, [Roles.User]);

        await FailSignInAsync(userName, MaxFailedAttempts);

        Assert.NotNull(await ReadLockoutEndAsync(userId));

        // Đúng mật khẩu vẫn không vào được — khoá có hiệu lực thật, không chỉ là cột trong DB.
        var outcome = await SignInAsync(userName, Password);
        Assert.True(outcome.IsLockedOut);
        Assert.False(outcome.Succeeded);
    }

    [Fact(DisplayName = "SuperAdmin: 5 lần sai mật khẩu → KHÔNG bị khoá, vẫn đăng nhập được")]
    public async Task SuperAdmin_AfterMaxFailedAttempts_IsNotLockedOut()
    {
        var userName = NewUserName("super");
        var userId = await CreateUserAsync(userName, [Roles.SuperAdmin]);

        await FailSignInAsync(userName, MaxFailedAttempts);

        Assert.Null(await ReadLockoutEndAsync(userId));

        // Điều thật sự quan trọng: người ngoài KHÔNG khoá được quản trị viên ra khỏi hệ thống.
        //
        // Dùng CheckPasswordSignInAsync chứ không phải IIdentityService.SignInAsync: nhánh đăng
        // nhập THÀNH CÔNG gọi tiếp signInManager.SignInAsync để ghi cookie, mà việc đó cần
        // HttpContext — không tồn tại khi gọi từ DI scope ngoài request ("HttpContext must not be
        // null"). Phần đang kiểm là "mật khẩu đúng có được chấp nhận không", không phải việc phát
        // cookie; đường phát cookie đã có SessionTerminationTests phủ qua HTTP thật.
        var check = await CheckPasswordAsync(userName, Password);
        Assert.True(check.Succeeded);
        Assert.False(check.IsLockedOut);
    }

    /// <summary>
    /// Bẫy đã lường trước: tài khoản bootstrap mang CẢ <c>SuperAdmin</c> lẫn <c>Admin</c>
    /// (<c>CoreSeeder</c>). Miễn trừ phải xét theo "có role SuperAdmin hay không", không phải
    /// "role duy nhất là SuperAdmin" — nếu cài sai, chính tài khoản break-glass mất miễn trừ.
    /// </summary>
    [Fact(DisplayName = "SuperAdmin + Admin (như tài khoản bootstrap): vẫn được miễn khoá")]
    public async Task UserWithSuperAdminAmongOtherRoles_IsNotLockedOut()
    {
        var userName = NewUserName("boot");
        var userId = await CreateUserAsync(userName, [Roles.SuperAdmin, Roles.Admin]);

        await FailSignInAsync(userName, MaxFailedAttempts);

        Assert.Null(await ReadLockoutEndAsync(userId));
    }

    private async Task FailSignInAsync(string userName, int times)
    {
        for (var i = 0; i < times; i++)
        {
            var outcome = await SignInAsync(userName, WrongPassword);
            Assert.False(outcome.Succeeded);
        }
    }

    private async Task<LoginOutcome> SignInAsync(string userName, string password)
    {
        using var scope = _factory.Services.CreateScope();
        var identityService = scope.ServiceProvider.GetRequiredService<IIdentityService>();
        return await identityService.SignInAsync(userName, password, CancellationToken.None);
    }

    /// <summary>Kiểm mật khẩu + trạng thái khoá mà KHÔNG phát cookie (không cần HttpContext).
    /// <c>lockoutOnFailure: false</c> để chính phép kiểm này không làm sai lệch bộ đếm.</summary>
    private async Task<SignInResult> CheckPasswordAsync(string userName, string password)
    {
        using var scope = _factory.Services.CreateScope();
        var signInManager = scope.ServiceProvider.GetRequiredService<SignInManager<AppUser>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var user = await userManager.FindByNameAsync(userName);
        Assert.NotNull(user);

        return await signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: false);
    }

    private static string NewUserName(string prefix) => $"it-{prefix}-{Guid.NewGuid():N}"[..24];

    private async Task<Guid> CreateUserAsync(string userName, string[] roles)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = userName,
            Email = $"{userName}@it.local",
            FullName = userName,
            DateCreate = DateTimeOffset.UtcNow,
        };

        var created = await userManager.CreateAsync(user, Password);
        Assert.True(created.Succeeded, string.Join("; ", created.Errors.Select(e => e.Description)));

        var added = await userManager.AddToRolesAsync(user, roles);
        Assert.True(added.Succeeded, string.Join("; ", added.Errors.Select(e => e.Description)));

        return user.Id;
    }

    private async Task EnsureRolesAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();

        foreach (var roleName in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
                await roleManager.CreateAsync(new AppRole(roleName) { Id = Guid.NewGuid() });
        }
    }

    private async Task<DateTimeOffset?> ReadLockoutEndAsync(Guid userId)
    {
        await using var db = _fixture.CreateDbContext();
        return await db.Users.AsNoTracking().Where(u => u.Id == userId).Select(u => u.LockoutEnd).FirstAsync();
    }
}

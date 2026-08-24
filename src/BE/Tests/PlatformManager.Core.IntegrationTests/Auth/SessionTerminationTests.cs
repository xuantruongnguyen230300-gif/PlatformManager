using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlatformManager.Core.Application.Common;
using PlatformManager.Core.Infrastructure.Identity;
using Xunit;

namespace PlatformManager.Core.IntegrationTests.Auth;

/// <summary>
/// Chấm dứt phiên khi quyền/trạng thái thay đổi — kiểm chứng QUA <c>SecurityStampValidator</c>
/// THẬT, trên host thật, với cookie thật.
///
/// Vì sao KHÔNG test bằng mock/verify "<c>UpdateSecurityStampAsync</c> đã được gọi": đó là test
/// xanh mà không chứng minh gì — trạng thái "xanh" đó đã tồn tại suốt lúc lỗi còn sống. Thứ duy
/// nhất chứng minh được chính sách hoạt động là **request kế tiếp bằng chính cookie đó trả 401**
/// (xem doc/huong_dan/wiki-core/be/02-identity-auth.md §"Cách chứng minh nó hoạt động thật").
/// Mỗi test vì vậy luôn có cặp khẳng định: (1) hiệu ứng lên phiên đang chạy, (2) con dấu trong DB
/// đổi/không đổi đúng như bảng luật ở .claude/rules/api-controller.md §"Chấm dứt phiên".
///
/// Mỗi test method có host RIÊNG (xUnit dựng instance test class mới cho từng method) — CỐ Ý:
/// policy rate limit "login" trong Program.cs là 5 request/phút, host dùng chung sẽ khiến test
/// thứ 3 trở đi nhận 429 thay vì kết quả thật.
///
/// ⚠️ ĐỪNG "tối ưu" bằng cách gộp về một host dùng chung. Từ 2026-08-21 policy "login" đã phân
/// vùng THEO IP (Program.cs, ResolveRateLimitPartitionKey) — nghe như đã hết vấn đề, nhưng
/// KHÔNG: TestServer không có kết nối TCP thật nên RemoteIpAddress luôn null, mọi test ở đây rơi
/// chung vào khoá dự phòng "unknown-ip" ⇒ vẫn dùng chung đúng một bộ đếm. Muốn tách phân vùng
/// trong test thì phải chủ động đặt IP như
/// RateLimiting/RateLimitPartitionFactory.cs làm — file này CỐ Ý không làm vậy, vì thứ nó kiểm
/// là SecurityStampValidator chứ không phải rate limit.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class SessionTerminationTests : IAsyncLifetime
{
    private const string Password = "Test@12345";
    private const string NewPassword = "Test@98765";

    private readonly PostgresFixture _fixture;
    private readonly SessionTerminationFactory _factory;

    public SessionTerminationTests(PostgresFixture fixture)
    {
        _fixture = fixture;

        // PHẢI đặt trước khi host boot — xem lý do đầy đủ trong IntegrationTestHostEnvironment.
        IntegrationTestHostEnvironment.Configure(fixture.ConnectionString);

        _factory = new SessionTerminationFactory();
    }

    public Task InitializeAsync() => EnsureRolesAsync();

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    // ── Kịch bản ─────────────────────────────────────────────────────────────

    [Fact(DisplayName = "Khoá tài khoản → phiên ĐANG CHẠY của user đó bị từ chối (401)")]
    public async Task Lock_TerminatesRunningSession()
    {
        var victimName = NewUserName("victim");
        var victimId = await CreateUserAsync(victimName, [Roles.User]);
        var adminClient = await LoginAsync(await CreateAdminAsync());

        var victimClient = await LoginAsync(victimName);
        await AssertStatusAsync(HttpStatusCode.OK, victimClient.GetAsync("/api/auth/me"));

        var stampBefore = await ReadSecurityStampAsync(victimId);

        await AssertStatusAsync(HttpStatusCode.OK, adminClient.PostAsync($"/api/users/{victimId}/lock", null));

        // ĐÂY là bước chứng minh — cùng cookie đó, request kế tiếp phải 401. Khẳng định
        // "LockAsync trả true" KHÔNG chứng minh được gì (nó vẫn true suốt lúc lỗi còn sống).
        await AssertStatusAsync(HttpStatusCode.Unauthorized, victimClient.GetAsync("/api/auth/me"));

        Assert.NotEqual(stampBefore, await ReadSecurityStampAsync(victimId));
    }

    [Fact(DisplayName = "Đổi tập role → phiên đang chạy của user đích bị chấm dứt (401)")]
    public async Task RoleChange_TerminatesRunningSession()
    {
        var victimName = NewUserName("victim");
        var victimId = await CreateUserAsync(victimName, [Roles.User]);
        var adminClient = await LoginAsync(await CreateAdminAsync());

        var victimClient = await LoginAsync(victimName);
        await AssertStatusAsync(HttpStatusCode.OK, victimClient.GetAsync("/api/auth/me"));

        var stampBefore = await ReadSecurityStampAsync(victimId);

        // Email/FullName giữ NGUYÊN — chỉ tập role đổi, để test cô lập đúng một biến.
        await AssertStatusAsync(HttpStatusCode.OK, adminClient.PutAsJsonAsync(
            $"/api/users/{victimId}",
            new { email = DefaultEmail(victimName), fullName = victimName, roles = new[] { Roles.User, Roles.Admin } }));

        await AssertStatusAsync(HttpStatusCode.Unauthorized, victimClient.GetAsync("/api/auth/me"));

        Assert.NotEqual(stampBefore, await ReadSecurityStampAsync(victimId));
    }

    [Fact(DisplayName = "CHỈ sửa email/fullName (tập role giữ nguyên) → phiên KHÔNG bị ảnh hưởng")]
    public async Task EmailAndNameOnlyUpdate_DoesNotTerminateSession()
    {
        var victimName = NewUserName("victim");
        var victimId = await CreateUserAsync(victimName, [Roles.User]);
        var adminClient = await LoginAsync(await CreateAdminAsync());

        var victimClient = await LoginAsync(victimName);
        await AssertStatusAsync(HttpStatusCode.OK, victimClient.GetAsync("/api/auth/me"));

        var stampBefore = await ReadSecurityStampAsync(victimId);
        var newEmail = $"{victimName}-doi@it.local";

        await AssertStatusAsync(HttpStatusCode.OK, adminClient.PutAsJsonAsync(
            $"/api/users/{victimId}",
            new { email = newEmail, fullName = "Tên đã đổi", roles = new[] { Roles.User } }));

        // Chống test rỗng: chứng minh lệnh ghi THẬT SỰ đã chạy. Thiếu khẳng định này thì một
        // UpdateAsync hỏng hoàn toàn (không ghi gì) cũng làm test xanh.
        Assert.Equal(newEmail, await ReadEmailAsync(victimId));

        // Ca NGƯỢC CHIỀU — chống "bump quá tay": đá người ta ra khỏi hệ thống vì bị sửa tên là
        // thiệt hại không mua được gì (.claude/rules/api-controller.md §"Chấm dứt phiên" luật 1).
        Assert.Equal(stampBefore, await ReadSecurityStampAsync(victimId));
        await AssertStatusAsync(HttpStatusCode.OK, victimClient.GetAsync("/api/auth/me"));
    }

    [Fact(DisplayName = "Đổi mật khẩu → phiên HIỆN TẠI sống tiếp, phiên KHÁC của chính người đó bị chấm dứt")]
    public async Task ChangePassword_KeepsCallingSession_ButTerminatesOthers()
    {
        var victimName = NewUserName("victim");
        var victimId = await CreateUserAsync(victimName, [Roles.User], mustChangePassword: true);

        var sessionA = await LoginAsync(victimName);
        var sessionB = await LoginAsync(victimName);
        await AssertStatusAsync(HttpStatusCode.OK, sessionA.GetAsync("/api/auth/me"));
        await AssertStatusAsync(HttpStatusCode.OK, sessionB.GetAsync("/api/auth/me"));

        var stampBefore = await ReadSecurityStampAsync(victimId);

        await AssertStatusAsync(HttpStatusCode.OK, sessionA.PostAsJsonAsync(
            "/api/auth/change-password",
            new { currentPassword = Password, newPassword = NewPassword }));

        // Identity TỰ đổi con dấu bên trong ChangePasswordAsync. Nếu khẳng định này hỏng thì 2
        // khẳng định dưới trở nên vô nghĩa (không có gì để RefreshSignInAsync phải cứu).
        Assert.NotEqual(stampBefore, await ReadSecurityStampAsync(victimId));

        // RefreshSignInAsync đã cấp lại cookie mang con dấu MỚI ⇒ người vừa đổi mật khẩu KHÔNG bị
        // đá ra. Đây là ca trúng gần như 100% user mới (MustChangePassword = true).
        await AssertStatusAsync(HttpStatusCode.OK, sessionA.GetAsync("/api/auth/me"));

        // Phiên khác vẫn mang con dấu cũ ⇒ bị chấm dứt. ĐÚNG chuẩn bảo mật — không "sửa" nốt.
        await AssertStatusAsync(HttpStatusCode.Unauthorized, sessionB.GetAsync("/api/auth/me"));
    }

    [Fact(DisplayName = "Mở khoá → KHÔNG đổi con dấu, chỉ có hiệu lực cho lần đăng nhập sau")]
    public async Task Unlock_DoesNotBumpSecurityStamp()
    {
        var victimName = NewUserName("victim");
        var victimId = await CreateUserAsync(victimName, [Roles.User]);
        var adminClient = await LoginAsync(await CreateAdminAsync());

        await AssertStatusAsync(HttpStatusCode.OK, adminClient.PostAsync($"/api/users/{victimId}/lock", null));

        // Khoá có hiệu lực NGAY với lần đăng nhập MỚI (khác hẳn phiên đang chạy — xem
        // Lock_TerminatesRunningSession). AUTH.LOCKED_OUT = ErrorCode.BusinessRuleError = 422.
        var lockedLogin = await (await CreateClientAsync()).PostAsJsonAsync(
            "/api/auth/login", new { userName = victimName, password = Password });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, lockedLogin.StatusCode);

        var stampAfterLock = await ReadSecurityStampAsync(victimId);

        await AssertStatusAsync(HttpStatusCode.OK, adminClient.PostAsync($"/api/users/{victimId}/unlock", null));

        // Luật 1 — mở khoá đi theo chiều KHÔI PHỤC quyền truy cập, không thu hồi gì.
        Assert.Equal(stampAfterLock, await ReadSecurityStampAsync(victimId));

        // Chống test rỗng: unlock thật sự có tác dụng — đăng nhập lại được.
        var reLogin = await LoginAsync(victimName);
        await AssertStatusAsync(HttpStatusCode.OK, reLogin.GetAsync("/api/auth/me"));
    }

    // ── Hạ tầng test ─────────────────────────────────────────────────────────

    private static string NewUserName(string prefix) => $"it-{prefix}-{Guid.NewGuid():N}"[..24];

    private static string DefaultEmail(string userName) => $"{userName}@it.local";

    /// <summary>CSRF Lớp 2 áp cho MỌI method ghi kể cả login (xem
    /// CsrfTestClientExtensions.WithCsrfTokenAsync) — vì vậy hàm này giờ BẤT ĐỒNG BỘ, phải lấy
    /// token TRƯỚC khi trả về client cho bất kỳ POST/PUT nào.</summary>
    private async Task<HttpClient> CreateClientAsync()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            // https BẮT BUỘC: cookie phiên khai CookieSecurePolicy.Always (Program.cs) nên
            // CookieContainer sẽ KHÔNG gửi lại nó qua http ⇒ mọi request sau login thành 401 và
            // test đỏ vì lý do hoàn toàn khác thứ đang kiểm.
            BaseAddress = new Uri("https://localhost"),
        });
        return await client.WithCsrfTokenAsync();
    }

    private async Task<HttpClient> LoginAsync(string userName, string password = Password)
    {
        var client = await CreateClientAsync();
        await AssertStatusAsync(
            HttpStatusCode.OK, client.PostAsJsonAsync("/api/auth/login", new { userName, password }));

        // Token lấy lúc ANONYMOUS (CreateClientAsync ở trên) không dùng được cho request ghi SAU
        // khi đã đăng nhập — DefaultAntiforgery gắn token với danh tính lúc phát hành, đổi danh
        // tính giữa 2 lần là 403 "meant for a different claims-based user". Client này còn dùng
        // để lock/đổi role/đổi mật khẩu (đều là request ghi) nên PHẢI lấy token MỚI ngay sau khi
        // danh tính đã đổi.
        await client.WithCsrfTokenAsync();
        return client;
    }

    private async Task<string> CreateAdminAsync()
    {
        var name = NewUserName("admin");
        await CreateUserAsync(name, [Roles.Admin]);
        return name;
    }

    private async Task<Guid> CreateUserAsync(string userName, string[] roles, bool mustChangePassword = false)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = userName,
            Email = DefaultEmail(userName),
            FullName = userName,
            MustChangePassword = mustChangePassword,
            DateCreate = DateTimeOffset.UtcNow,
        };

        var created = await userManager.CreateAsync(user, Password);
        Assert.True(created.Succeeded, string.Join("; ", created.Errors.Select(e => e.Description)));

        var added = await userManager.AddToRolesAsync(user, roles);
        Assert.True(added.Succeeded, string.Join("; ", added.Errors.Select(e => e.Description)));

        return user.Id;
    }

    /// <summary>Idempotent — host chạy ở Development nên CoreSeeder thường đã tạo sẵn, nhưng test
    /// không được phụ thuộc vào việc seed có chạy thành công hay không.</summary>
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

    /// <summary>Đọc THẲNG từ DB bằng DbContext riêng — không qua DI của host, để không vô tình
    /// đọc lại giá trị đang nằm trong change tracker của scope đã ghi.</summary>
    private async Task<string?> ReadSecurityStampAsync(Guid userId)
    {
        await using var db = _fixture.CreateDbContext();
        return await db.Users.AsNoTracking().Where(u => u.Id == userId).Select(u => u.SecurityStamp).FirstAsync();
    }

    private async Task<string?> ReadEmailAsync(Guid userId)
    {
        await using var db = _fixture.CreateDbContext();
        return await db.Users.AsNoTracking().Where(u => u.Id == userId).Select(u => u.Email).FirstAsync();
    }

    /// <summary>Kèm body vào thông báo khi lệch — 401/422 trần không nói được lý do, mà lý do
    /// (envelope businessCode) chính là thứ cần biết khi test đỏ.</summary>
    private static async Task AssertStatusAsync(HttpStatusCode expected, Task<HttpResponseMessage> call)
    {
        var response = await call;
        if (response.StatusCode == expected)
            return;

        var body = await response.Content.ReadAsStringAsync();
        Assert.Fail($"Mong đợi {(int)expected} {expected}, nhận {(int)response.StatusCode} {response.StatusCode}. Body: {body}");
    }
}

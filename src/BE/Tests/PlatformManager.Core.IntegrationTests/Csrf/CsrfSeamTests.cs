using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using PlatformManager.Core.Application.Common;
using PlatformManager.Core.Infrastructure.Identity;
using Xunit;

namespace PlatformManager.Core.IntegrationTests.Csrf;

/// <summary>
/// SEAM ACTIVATION TEST cho CSRF Lớp 2 (<c>Program.cs</c> — <c>AddAntiforgery()</c> +
/// <c>app.Use(...)</c> validate cho method ghi) — chứng minh middleware THẬT SỰ nằm trong
/// pipeline HTTP, KHÔNG chỉ chứng minh <c>AddAntiforgery()</c> đã đăng ký DI đúng. Quên dòng
/// <c>app.Use(...)</c> (hoặc đặt nhầm vị trí) KHÔNG gây lỗi biên dịch, KHÔNG gây exception lúc
/// khởi động — request ghi thiếu token vẫn đơn giản đi lọt, im lặng. Xem
/// doc/huong_dan/wiki-core/be/04-testing-strategy.md §"Seam activation test — bắt buộc cho mọi
/// cross-cutting seam mới" và doc/huong_dan/wiki-core/be/02-identity-auth.md §CSRF.
///
/// Đo cả 2 chiều "trước"/"sau" (đúng mẫu đã dẫn ở 02-identity-auth.md): CÙNG một request ghi,
/// gọi khi THIẾU header → phải 403; gọi lại khi CÓ đúng header → phải 200. Chỉ kiểm 1 chiều sẽ
/// "pass" ngay cả khi middleware chưa từng chạy (luôn 200) hoặc luôn chặn vì lý do khác (luôn
/// 403) — phải thấy nó ĐỔI đúng lúc token đổi mới chứng minh được wiring thật.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class CsrfSeamTests : IAsyncLifetime
{
    private const string Password = "Test@12345";

    private readonly PostgresFixture _fixture;
    private readonly WebApplicationFactory<Program> _factory;

    public CsrfSeamTests(PostgresFixture fixture)
    {
        _fixture = fixture;

        // PHẢI đặt trước khi host boot — xem IntegrationTestHostEnvironment.
        IntegrationTestHostEnvironment.Configure(fixture.ConnectionString);

        _factory = new WebApplicationFactory<Program>();
    }

    public Task InitializeAsync() => EnsureRolesAsync();

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    [Fact(DisplayName =
        "POST ghi thiếu header X-XSRF-TOKEN → 403; CÙNG request kèm đúng token → 200 " +
        "(CSRF middleware thật sự nằm trong pipeline)")]
    public async Task WriteRequest_MissingCsrfHeader_IsForbidden_WithHeader_Succeeds()
    {
        var adminName = NewUserName("csrf-admin");
        await CreateUserAsync(adminName, [Roles.Admin]);
        var targetId = await CreateUserAsync(NewUserName("csrf-victim"), [Roles.User]);

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            // https BẮT BUỘC — cookie phiên VÀ cookie XSRF-TOKEN đều khai CookieSecurePolicy.Always
            // (Program.cs).
            BaseAddress = new Uri("https://localhost"),
        });

        // Token lúc ANONYMOUS — đủ dùng cho chính POST /api/auth/login (cả phát hành lẫn kiểm
        // đều lúc anonymous, không có gì để lệch danh tính).
        await client.WithCsrfTokenAsync();
        var login = await client.PostAsJsonAsync("/api/auth/login", new { userName = adminName, password = Password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        // Token MỚI — bắt buộc lấy lại NGAY sau khi danh tính đổi (xem
        // CsrfTestClientExtensions.WithCsrfTokenAsync).
        await client.WithCsrfTokenAsync();

        // ── TRƯỚC: gỡ header rồi gọi request ghi → PHẢI 403. Đây là khẳng định trọng tâm — nếu
        // middleware chưa thật sự nằm trong pipeline, request thiếu token vẫn lọt 200 và khẳng
        // định "SAU" bên dưới không còn chứng minh được gì (luôn 200 dù có/không có token).
        client.DefaultRequestHeaders.Remove("X-XSRF-TOKEN");
        var withoutToken = await client.PostAsync($"/api/users/{targetId}/lock", null);
        Assert.Equal(HttpStatusCode.Forbidden, withoutToken.StatusCode);

        // ── SAU: lấy lại đúng token rồi gọi lại CÙNG request → PHẢI 200. Chứng minh 403 ở trên
        // đến từ CSRF (thiếu token), không phải từ nguyên nhân khác (role/permission/route sai).
        await client.WithCsrfTokenAsync();
        var withToken = await client.PostAsync($"/api/users/{targetId}/lock", null);
        Assert.Equal(HttpStatusCode.OK, withToken.StatusCode);
    }

    // ── Hạ tầng test ─────────────────────────────────────────────────────────

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

    /// <summary>Idempotent — host chạy Development nên CoreSeeder thường đã tạo sẵn, nhưng test
    /// không được phụ thuộc việc seed có chạy thành công hay không.</summary>
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
}

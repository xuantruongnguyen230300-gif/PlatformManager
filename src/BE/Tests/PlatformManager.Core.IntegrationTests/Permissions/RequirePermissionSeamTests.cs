using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using PlatformManager.Core.Application.Permissions;
using PlatformManager.Core.Domain.Entities;
using PlatformManager.Core.Infrastructure.Identity;
using Xunit;

namespace PlatformManager.Core.IntegrationTests.Permissions;

/// <summary>
/// SEAM ACTIVATION TEST — chứng minh <c>RequirePermissionFilter</c> thật sự nằm trong pipeline
/// HTTP, KHÔNG chỉ đúng logic khi gọi trực tiếp. Khác <see cref="PermissionCheckerTests"/> (Nhóm
/// B, gọi thẳng <c>PermissionChecker</c> — không đi qua HTTP/MVC filter pipeline) và khác unit
/// test của riêng <c>RequirePermissionFilter</c> (Nhóm A, mock <c>IPermissionChecker</c>) — cả 2
/// đều chứng minh ĐƯỢC logic đúng nhưng KHÔNG chứng minh được filter có thật sự được đăng ký vào
/// <c>AddControllers(options =&gt; options.Filters.Add&lt;RequirePermissionFilter&gt;())</c> +
/// <c>AddPermissionInfrastructure()</c> ở <c>Program.cs</c> hay không. Quên 1 trong 2 dòng đó
/// KHÔNG gây lỗi biên dịch, KHÔNG gây exception lúc chạy — endpoint chỉ đơn giản không bị chặn gì
/// thêm ngoài <c>[Authorize]</c>, im lặng. Xem
/// doc/huong_dan/wiki-core/be/04-testing-strategy.md §"Seam activation test — bắt buộc cho mọi
/// cross-cutting seam mới".
///
/// Đo cả 2 chiều "trước" lẫn "sau" (đúng mẫu ở 02-identity-auth.md đã dẫn): role MỚI TẠO, CHƯA
/// được cấp <see cref="ResourceKeys.Criteria"/> → gọi <c>GET /api/criteria</c>
/// (<c>[RequirePermission(ResourceKeys.Criteria)]</c> trên <c>CriteriaController</c>) → PHẢI 403.
/// Seed đúng <see cref="RolePermission"/> cho role đó → gọi lại BẰNG CHÍNH CÙNG 1 COOKIE → PHẢI
/// 200. Chỉ assert 1 chiều (chỉ kiểm 403, hoặc chỉ kiểm 200) sẽ "pass" ngay cả khi endpoint luôn
/// trả cùng 1 mã vì lý do khác hẳn (vd luôn 403 vì bug không liên quan, hoặc luôn 200 vì filter
/// chưa từng chạy) — phải thấy nó ĐỔI đúng lúc dữ liệu đổi mới chứng minh được wiring thật.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class RequirePermissionSeamTests : IAsyncLifetime
{
    private const string Password = "Test@12345";

    private readonly PostgresFixture _fixture;
    private readonly WebApplicationFactory<Program> _factory;

    public RequirePermissionSeamTests(PostgresFixture fixture)
    {
        _fixture = fixture;

        // PHẢI đặt trước khi host boot — xem IntegrationTestHostEnvironment.
        IntegrationTestHostEnvironment.Configure(fixture.ConnectionString);

        _factory = new WebApplicationFactory<Program>();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    [Fact(DisplayName =
        "Role chưa có RolePermission cho criteria.manage → GET /api/criteria trả 403; " +
        "seed đúng quyền rồi gọi lại CÙNG cookie → 200")]
    public async Task Endpoint_TogglesForbiddenToOk_AsRolePermissionIsGrantedMidSession()
    {
        var roleName = $"itperm{Guid.NewGuid():N}"[..20];
        var userName = $"it-perm-{Guid.NewGuid():N}"[..24];
        Guid roleId;

        using (var scope = _factory.Services.CreateScope())
        {
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();
            var role = new AppRole(roleName) { Id = Guid.NewGuid() };
            var createRole = await roleManager.CreateAsync(role);
            Assert.True(createRole.Succeeded, string.Join("; ", createRole.Errors.Select(e => e.Description)));
            roleId = role.Id;

            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var user = new AppUser
            {
                Id = Guid.NewGuid(),
                UserName = userName,
                Email = $"{userName}@it.local",
                FullName = userName,
                DateCreate = DateTimeOffset.UtcNow,
            };
            var createUser = await userManager.CreateAsync(user, Password);
            Assert.True(createUser.Succeeded, string.Join("; ", createUser.Errors.Select(e => e.Description)));

            var addToRole = await userManager.AddToRoleAsync(user, roleName);
            Assert.True(addToRole.Succeeded, string.Join("; ", addToRole.Errors.Select(e => e.Description)));
        }

        var client = await CreateClientAsync();
        var login = await client.PostAsJsonAsync("/api/auth/login", new { userName, password = Password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        // ── TRƯỚC: role vừa tạo KHÔNG có dòng RolePermission nào cho "criteria.manage" ──────
        var before = await client.GetAsync("/api/criteria");
        Assert.Equal(HttpStatusCode.Forbidden, before.StatusCode);

        // ── Seed đúng RolePermission cho role đó (mô phỏng thao tác trên màn Phân quyền) ────
        await using (var db = _fixture.CreateDbContext())
        {
            db.RolePermissions.Add(RolePermission.Create(roleId, ResourceKeys.Criteria));
            await db.SaveChangesAsync();
        }

        // ── SAU: CÙNG 1 cookie, gọi lại → 200. Không cache/TTL nào ở đường phân quyền, xem ────
        // doc/huong_dan/wiki-core/be/11-performance-caching.md §6.2 quyết định #5 — nếu có ai đó
        // sau này lỡ thêm cache mà quên invalidate, đúng bước này sẽ bắt được.
        var after = await client.GetAsync("/api/criteria");
        Assert.Equal(HttpStatusCode.OK, after.StatusCode);
    }

    /// <summary>CSRF Lớp 2 áp cho MỌI method ghi kể cả login — xem
    /// CsrfTestClientExtensions.WithCsrfTokenAsync.</summary>
    private async Task<HttpClient> CreateClientAsync()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            // https BẮT BUỘC — cookie phiên khai CookieSecurePolicy.Always (Program.cs), thiếu
            // https thì CookieContainer không gửi lại cookie và request sau login sẽ 401 (sai
            // thứ đang kiểm).
            BaseAddress = new Uri("https://localhost"),
        });
        return await client.WithCsrfTokenAsync();
    }
}

using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using PlatformManager.Core.Application.Permissions;
using PlatformManager.Core.Domain.Entities;
using PlatformManager.Core.Infrastructure.Identity;
using Xunit;

namespace PlatformManager.Core.IntegrationTests.Import;

/// <summary>
/// SEAM ACTIVATION TEST cho job nền Hangfire (<c>StartImportCommand</c>/<c>IImportJobRunner</c>)
/// — chứng minh Hangfire THẬT SỰ dequeue và chạy job, KHÔNG chỉ chứng minh
/// <c>StartImportCommandHandler</c> tạo đúng shape <c>ImportJob</c>. Trước khi wiring (2026-08-24:
/// <c>AddHangfire()</c>/<c>AddHangfireServer()</c> ở <c>Program.cs</c> + package
/// Hangfire.Core/AspNetCore/PostgreSql), lỗi "quên nối dây" loại này KHÔNG gây lỗi biên dịch,
/// KHÔNG gây exception — job chỉ đơn giản nằm mãi ở <c>Status = Pending</c>, im lặng. Xem
/// doc/huong_dan/wiki-core/be/04-testing-strategy.md §"Seam activation test — bắt buộc cho mọi
/// cross-cutting seam mới".
///
/// Dùng file CSV RỖNG (chỉ header, 0 dòng dữ liệu) có chủ đích — mục đích DUY NHẤT của test này
/// là chứng minh WIRING (job được enqueue → Hangfire worker thật sự nhặt lên → chạy xong → ghi
/// Status), KHÔNG phải chứng minh business rule đọc CSV đúng (đã có unit test riêng cho
/// <c>ImportRowProcessor</c>/<c>CsvFileReader</c> lo phần đó). File rỗng vẫn đi trọn vòng đời
/// job thật: Pending → Running → Succeeded với <c>TotalRows = 0</c>.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class ImportBackgroundJobSeamTests : IAsyncLifetime
{
    private const string Password = "Test@12345";
    private static readonly TimeSpan PollTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(500);

    private readonly PostgresFixture _fixture;
    private readonly WebApplicationFactory<Program> _factory;

    public ImportBackgroundJobSeamTests(PostgresFixture fixture)
    {
        _fixture = fixture;

        // PHẢI đặt trước khi host boot — xem IntegrationTestHostEnvironment. Cùng biến
        // ConnectionStrings__Default này được AddHangfire(...UseNpgsqlConnection(...)) đọc, nên
        // Hangfire server của host test trỏ ĐÚNG Postgres container dùng chung với phần còn lại
        // của bộ test (tự tạo schema "hangfire" lúc khởi động lần đầu).
        IntegrationTestHostEnvironment.Configure(fixture.ConnectionString);

        _factory = new WebApplicationFactory<Program>();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    [Fact(DisplayName =
        "POST /api/import (CSV thật qua HTTP) → Hangfire dequeue thật → " +
        "GET /api/import/{jobId} chuyển Pending → Succeeded trong thời gian giới hạn")]
    public async Task StartImport_EnqueuesRealHangfireJob_WhichTransitionsToSucceeded()
    {
        var roleName = $"itjob{Guid.NewGuid():N}"[..20];
        var userName = $"it-job-{Guid.NewGuid():N}"[..24];

        using (var scope = _factory.Services.CreateScope())
        {
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();
            var role = new AppRole(roleName) { Id = Guid.NewGuid() };
            var createRole = await roleManager.CreateAsync(role);
            Assert.True(createRole.Succeeded, string.Join("; ", createRole.Errors.Select(e => e.Description)));

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

            await using var db = _fixture.CreateDbContext();
            db.RolePermissions.Add(RolePermission.Create(role.Id, ResourceKeys.Import));
            await db.SaveChangesAsync();
        }

        var client = await CreateClientAsync();
        var login = await client.PostAsJsonAsync("/api/auth/login", new { userName, password = Password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        // Token lấy lúc ANONYMOUS ở CreateClientAsync không dùng được cho POST /api/import SAU
        // khi đã đăng nhập (DefaultAntiforgery gắn token với danh tính lúc phát hành — xem
        // CsrfTestClientExtensions.WithCsrfTokenAsync). Lấy lại token MỚI ngay sau login.
        await client.WithCsrfTokenAsync();

        // ── Bước 1: POST /api/import — 200 (envelope, xem CONTRACT DM-7 §sửa 2026-08-24), data.jobId
        using var form = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("Mã\n")); // chỉ header, 0 dòng dữ liệu
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/csv");
        form.Add(fileContent, "file", "seam-test.csv");

        var startResponse = await client.PostAsync("/api/import", form);
        Assert.Equal(HttpStatusCode.OK, startResponse.StatusCode);

        var startBody = await startResponse.Content.ReadFromJsonAsync<JsonElement>();
        var jobId = startBody.GetProperty("data").GetProperty("jobId").GetGuid();

        // ── Bước 2: poll GET /api/import/{jobId} tới khi KHÔNG còn Pending/Running, hoặc hết giờ.
        // Đo cả trạng thái BAN ĐẦU (phải là Pending/Running — job vừa enqueue) lẫn trạng thái CUỐI
        // (phải Succeeded) — chỉ kiểm 1 chiều sẽ "pass" kể cả khi Hangfire server chưa từng chạy
        // (job kẹt mãi ở Pending) nếu vô tình không poll đủ lâu để lộ ra.
        var firstStatus = await GetImportStatusAsync(client, jobId);
        Assert.True(firstStatus is "Pending" or "Running" or "Succeeded",
            $"Trạng thái ban đầu bất thường: {firstStatus}");

        var deadline = DateTime.UtcNow + PollTimeout;
        var finalStatus = firstStatus;
        while (DateTime.UtcNow < deadline && finalStatus is "Pending" or "Running")
        {
            await Task.Delay(PollInterval);
            finalStatus = await GetImportStatusAsync(client, jobId);
        }

        Assert.Equal("Succeeded", finalStatus);
    }

    private static async Task<string> GetImportStatusAsync(HttpClient client, Guid jobId)
    {
        var response = await client.GetAsync($"/api/import/{jobId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("data").GetProperty("status").GetString()
            ?? throw new InvalidOperationException("Thiếu field 'status' trong response.");
    }

    /// <summary>CSRF Lớp 2 áp cho MỌI method ghi (login + POST /api/import) — xem
    /// CsrfTestClientExtensions.WithCsrfTokenAsync.</summary>
    private async Task<HttpClient> CreateClientAsync()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            // https BẮT BUỘC — cookie phiên khai CookieSecurePolicy.Always (Program.cs).
            BaseAddress = new Uri("https://localhost"),
        });
        return await client.WithCsrfTokenAsync();
    }
}

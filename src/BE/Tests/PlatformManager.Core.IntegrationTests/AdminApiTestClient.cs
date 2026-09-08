using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using PlatformManager.Core.Infrastructure.Identity;
using Xunit;

namespace PlatformManager.Core.IntegrationTests;

/// <summary>
/// Ba việc mà MỌI test gọi endpoint quản trị qua HTTP đều phải làm: tạo một tài khoản mang đúng
/// role cần thử, đăng nhập nó (kèm 2 lượt lấy token CSRF), và bóc envelope
/// <c>ApiResult&lt;T&gt;</c> ra khỏi response.
///
/// <para><b>Vì sao gom lại (2026-09-01):</b> ba đoạn này từng được chép nguyên si trong
/// <c>ResourcePermissionEndpointTests</c> và <c>PermissionMatrixDuplicateKeyTests</c>, và đợt sửa
/// theo hợp đồng mới còn định thêm bản sao thứ ba lẫn thứ tư. Chúng KHÔNG phải là thứ đang được
/// kiểm — chúng là điều kiện để bắt đầu kiểm — nên mỗi bản sao chỉ là thêm một chỗ để lệch. Bản
/// sao đã suýt lệch thật: chỉ cần một chỗ quên lượt <c>WithCsrfTokenAsync</c> thứ hai là mọi
/// <c>PUT</c> của class đó nhận 403 và người đọc đi tìm lỗi ở gate role.</para>
///
/// <para>KHÔNG gom vào đây những thứ khác nhau giữa các class (dựng/dọn dữ liệu, chọn factory) —
/// đó là phần thuộc về từng bài kiểm.</para>
/// </summary>
internal static class AdminApiTestClient
{
    /// <summary>Mật khẩu dùng cho mọi tài khoản do test tạo — xem
    /// <see cref="IntegrationTestHostEnvironment"/> cho lý do đặt tường minh trong mã test.</summary>
    public const string Password = "Test@123456789";

    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Envelope chung của mọi endpoint (xem doc/huong_dan/quy-uoc/be-api-controller.md
    /// §Envelope response). <c>Fields</c> chỉ xuất hiện ở ca <c>VALIDATION_ERROR</c> — thuộc tính
    /// null bị lược bỏ khi serialize.
    /// </summary>
    public sealed record Envelope<T>(
        T? Data, string? Status, string? Code, string? BusinessCode, string? Message, Dictionary<string, string[]>? Fields);

    /// <summary>Đọc <c>Data</c> và đòi <c>status = SUCCESS</c>.</summary>
    public static async Task<T> ReadDataAsync<T>(HttpResponseMessage response)
    {
        var envelope = await response.Content.ReadFromJsonAsync<Envelope<T>>(JsonOptions);
        Assert.NotNull(envelope);
        Assert.Equal("SUCCESS", envelope.Status);
        Assert.NotNull(envelope.Data);
        return envelope.Data;
    }

    /// <summary>Đọc envelope của response LỖI — không đòi <c>SUCCESS</c>, không đòi có
    /// <c>Data</c>.</summary>
    public static async Task<Envelope<object>> ReadEnvelopeAsync(HttpResponseMessage response)
    {
        var envelope = await response.Content.ReadFromJsonAsync<Envelope<object>>(JsonOptions);
        Assert.NotNull(envelope);
        return envelope;
    }

    /// <summary>Khẳng định <c>200</c> + <c>data: true</c> cho các lệnh ghi trả <c>bool</c>. Kèm
    /// body vào thông báo khi lệch: một 400/409 trần không nói được lý do, mà lý do (thông điệp
    /// validator hoặc mã nghiệp vụ) chính là thứ cần biết khi test đỏ.</summary>
    public static async Task AssertOkAsync(Task<HttpResponseMessage> call)
    {
        var response = await call;
        if (response.StatusCode != HttpStatusCode.OK)
            Assert.Fail($"Mong đợi 200 OK, nhận {(int)response.StatusCode}. Body: {await response.Content.ReadAsStringAsync()}");

        Assert.True(await ReadDataAsync<bool>(response));
    }

    /// <summary>
    /// Tạo tài khoản mới mang <paramref name="roles"/> (tạo role nếu chưa có) và trả về
    /// <c>UserName</c>. <paramref name="prefix"/> chỉ để người đọc log phân biệt được test nào
    /// sinh ra tài khoản nào.
    /// </summary>
    public static async Task<string> CreateUserAsync(
        WebApplicationFactory<Program> factory, string prefix, params string[] roles)
    {
        using var scope = factory.Services.CreateScope();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                Assert.True((await roleManager.CreateAsync(new AppRole(role) { Id = Guid.NewGuid() })).Succeeded);
        }

        var userName = $"it-{prefix}-{Guid.NewGuid():N}"[..24];
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

        return userName;
    }

    /// <summary>Tạo tài khoản mang <paramref name="roles"/> rồi trả về client ĐÃ đăng nhập.</summary>
    public static async Task<HttpClient> CreateLoggedInClientAsync(
        WebApplicationFactory<Program> factory, string prefix, params string[] roles)
        => await LoginAsync(factory, await CreateUserAsync(factory, prefix, roles));

    public static async Task<HttpClient> LoginAsync(WebApplicationFactory<Program> factory, string userName)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            // https BẮT BUỘC — cookie phiên khai CookieSecurePolicy.Always, xem
            // SessionTerminationTests.CreateClientAsync.
            BaseAddress = new Uri("https://localhost"),
        });

        await client.WithCsrfTokenAsync();
        var login = await client.PostAsJsonAsync("/api/auth/login", new { userName, password = Password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        // Lượt thứ hai KHÔNG thừa: token phát lúc ANONYMOUS không dùng được sau khi danh tính đổi
        // (xem CsrfTestClientExtensions) — thiếu nó thì mọi lệnh ghi nhận 403.
        await client.WithCsrfTokenAsync();
        return client;
    }
}

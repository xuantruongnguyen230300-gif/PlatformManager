using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using PlatformManager.Core.Application.Common;
using PlatformManager.Core.Application.Users;
using PlatformManager.Core.Infrastructure.Identity;
using Xunit;

namespace PlatformManager.Core.IntegrationTests.Users;

/// <summary>
/// Chứng minh <see cref="GetUsersListValidator"/> THẬT SỰ CHẠY trên đường
/// <c>[FromQuery] GetUsersListQuery</c> của <c>UsersController.List</c> — tức trần
/// <c>pageSize</c> = <see cref="GetUsersListValidator.MaxPageSize"/> và <c>page &gt;= 1</c> là
/// ràng buộc CÓ HIỆU LỰC với client, không chỉ là một object khởi tạo bằng <c>new</c> trong test.
///
/// <para><b>Khoảng trống mà test này lấp (2026-08-29):</b> trước bản này, hai ràng buộc trên chỉ
/// có unit test gọi thẳng <c>validator.Validate(new GetUsersListQuery(...))</c>
/// (<c>Tests/PlatformManager.Core.UnitTests/Users/GetUsersListValidatorTests.cs</c>). Bộ unit test
/// đó xanh trong MỌI tình trạng hỏng của phần nối: validator không được DI đăng ký, <c>ValidationBehavior</c>
/// bị gỡ khỏi pipeline, controller đổi sang nhận tham số rời rồi tự dựng query, hay
/// <c>[ApiController]</c> chặn sớm bằng ProblemDetails không đúng envelope. Đúng bài học vừa ghi
/// ở <see cref="PlatformManager.Core.IntegrationTests.Permissions.ResourcePermissionEndpointTests"/>:
/// toàn bộ test cũ của PERM-2 gọi thẳng handler nên chúng XANH suốt quãng thời gian route thật trả
/// 404. Chứng minh logic đúng không chứng minh nó được nối.</para>
///
/// <para><b>Ca "hợp lệ" ở <see cref="ValidPageSize_ReturnsOk"/> là bắt buộc, không phải trang
/// trí:</b> nếu thiếu nó, mọi khẳng định 400 dưới đây vẫn xanh trong tình trạng route
/// <c>GET /api/users</c> hỏng hoàn toàn hoặc gate role từ chối — vì khi đó thứ trả về cũng không
/// phải 200. Ca hợp lệ là mỏ neo phân biệt "validator từ chối" với "endpoint không dùng được".</para>
///
/// <para>Không ghi dữ liệu nghiệp vụ nào (chỉ tạo 1 tài khoản đăng nhập), nên không cần dọn dẹp;
/// vẫn giữ <c>[Collection]</c> vì dùng chung Postgres container.</para>
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class UserListQueryValidationSeamTests : IAsyncLifetime
{
    private const string Password = "Test@123456789";
    private const string UsersUrl = "/api/users";

    private readonly WebApplicationFactory<Program> _factory;

    public UserListQueryValidationSeamTests(PostgresFixture fixture)
    {
        // PHẢI đặt trước khi host boot — xem IntegrationTestHostEnvironment.
        IntegrationTestHostEnvironment.Configure(fixture.ConnectionString);
        _factory = new WebApplicationFactory<Program>();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    [Fact(DisplayName = "GET /api/users?pageSize=50 → 200 (mỏ neo: route + gate role hoạt động)")]
    public async Task ValidPageSize_ReturnsOk()
    {
        var client = await LoginAsync(await CreateUserAsync([Roles.Admin]));

        var response = await client.GetAsync($"{UsersUrl}?page=1&pageSize=50");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("SUCCESS", (await ReadEnvelopeAsync(response)).Status);
    }

    /// <summary>
    /// <c>1_000_000</c> là chính finding BE-3: trước khi có trần, số này đi thẳng xuống
    /// <c>Take(1_000_000)</c> — một request kéo trọn bảng user kèm role từng dòng. Test này khẳng
    /// định người gọi ngoài (không qua UI) bị chặn THẬT, chứ không chỉ bị chặn trong unit test.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(GetUsersListValidator.MaxPageSize + 1)]
    [InlineData(1_000_000)]
    public async Task PageSizeOutOfRange_Returns400_WithPageSizeField(int pageSize)
    {
        var client = await LoginAsync(await CreateUserAsync([Roles.Admin]));

        var response = await client.GetAsync($"{UsersUrl}?pageSize={pageSize}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await ReadEnvelopeAsync(response);
        Assert.Equal("VALIDATION_ERROR", error.Status);
        Assert.NotNull(error.Fields);

        // Khoá của `fields` là PascalCase ĐÚNG TÊN PROPERTY C# ("PageSize"), KHÔNG camelCase như
        // tham số query gửi lên và cũng khác `data` (camelCase). Khẳng định này chốt hợp đồng bind
        // lỗi của FE — xem GlobalExceptionHandler.NormalizeField + wiki-core/fe/02-http-envelope.md.
        // Nếu chỉ assert status 400 thì đổi DictionaryKeyPolicy sẽ làm FE hết tô đỏ được ô nào mà
        // test BE vẫn xanh.
        Assert.Contains(nameof(GetUsersListQuery.PageSize), error.Fields.Keys);
        Assert.DoesNotContain(nameof(GetUsersListQuery.Page), error.Fields.Keys);
    }

    /// <summary>
    /// <c>page=0</c> KHÔNG được vá âm thầm về 1: <c>Skip((0-1) * pageSize)</c> là offset ÂM,
    /// Npgsql ném lỗi ⇒ 500. Khẳng định 400 (chứ không phải "khác 200") là thứ duy nhất phân biệt
    /// bản đã vá với bản chưa vá.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public async Task PageBelowOne_Returns400_WithPageField(int page)
    {
        var client = await LoginAsync(await CreateUserAsync([Roles.Admin]));

        var response = await client.GetAsync($"{UsersUrl}?page={page}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await ReadEnvelopeAsync(response);
        Assert.Equal("VALIDATION_ERROR", error.Status);
        Assert.NotNull(error.Fields);
        Assert.Contains(nameof(GetUsersListQuery.Page), error.Fields.Keys);
        Assert.DoesNotContain(nameof(GetUsersListQuery.PageSize), error.Fields.Keys);
    }

    [Fact(DisplayName = "GET /api/users?role=Adminn → 400 kèm fields.Role (bộ lọc role đi cùng đường)")]
    public async Task InvalidRoleFilter_Returns400_WithRoleField()
    {
        var client = await LoginAsync(await CreateUserAsync([Roles.Admin]));

        // Sai chính tả — đúng ca mà lựa chọn "bỏ qua bộ lọc" (đã bị loại ở CONTRACT USER-6) sẽ
        // nuốt mất và trả về TOÀN BỘ danh sách.
        var response = await client.GetAsync($"{UsersUrl}?role=Adminn");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await ReadEnvelopeAsync(response);
        Assert.Equal("VALIDATION_ERROR", error.Status);
        Assert.NotNull(error.Fields);
        Assert.Contains(nameof(GetUsersListQuery.Role), error.Fields.Keys);
    }

    /// <summary>
    /// Hợp đồng TRÊN DÂY của nhánh 400 sau khi thêm <c>fieldErrors</c> (2026-09-03,
    /// doc/huong_dan/wiki-core/be/16-i18n-va-ma-loi.md §4(b)).
    ///
    /// <para><b>Vì sao đọc JSON thô ở đây</b> trong khi các test trên đọc qua record: thứ đang
    /// chốt là <b>tên và casing của từng trường</b>. Deserialize vào record dùng
    /// <c>JsonSerializerDefaults.Web</c> thì <c>fieldErrors</c>/<c>FieldErrors</c>/<c>fielderrors</c>
    /// đều khớp như nhau — nghĩa là khẳng định qua record không phân biệt được ba tên đó, còn FE
    /// thì phân biệt.</para>
    ///
    /// <para><b>Casing lệch nhau là CỐ Ý, và đó chính là thứ test này ghim:</b> khoá dictionary
    /// giữ PascalCase (<c>"PageSize"</c>, vì <c>DictionaryKeyPolicy</c> không set) trong khi
    /// property của phần tử là camelCase (<c>"code"</c>, <c>"message"</c>, vì chúng đi qua
    /// <c>PropertyNamingPolicy</c>). Không ai đoán được điều này từ tên trường — xem
    /// GlobalExceptionHandler.NormalizeField và wiki-core/fe/02-http-envelope.md.</para>
    ///
    /// <para><c>fields</c> vẫn phải còn: giai đoạn di trú song song chưa kết thúc (bước 11 của §7).
    /// Gỡ nó sớm là để client cũ gặp BE mới, mà envelope thì mọi màn hình đều đi qua.</para>
    /// </summary>
    [Fact(DisplayName = "GET /api/users?pageSize=0 → 400 kèm businessCode + fieldErrors[].code, fields VẪN còn")]
    public async Task ValidationEnvelope_CarriesCodes_OnTheWire()
    {
        var client = await LoginAsync(await CreateUserAsync([Roles.Admin]));

        var response = await client.GetAsync($"{UsersUrl}?pageSize=0");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = body.RootElement;

        Assert.Equal("VALIDATION.FAILED", root.GetProperty("businessCode").GetString());

        var pageSize = root.GetProperty("fieldErrors").GetProperty(nameof(GetUsersListQuery.PageSize));
        Assert.Equal("InclusiveBetweenValidator", pageSize[0].GetProperty("code").GetString());
        Assert.False(string.IsNullOrWhiteSpace(pageSize[0].GetProperty("message").GetString()));

        Assert.True(
            root.TryGetProperty("fields", out var legacy),
            "Trường `fields` biến mất — bước gỡ là bước 11 của §7 và phải làm CÙNG phía client, "
            + "không được xảy ra như hiệu ứng phụ của việc thêm `fieldErrors`.");
        Assert.Contains(nameof(GetUsersListQuery.PageSize), legacy.EnumerateObject().Select(p => p.Name));
    }

    // ── Hạ tầng test ─────────────────────────────────────────────────────────

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private sealed record Envelope(string? Status, string? Message, Dictionary<string, string[]>? Fields);

    private static async Task<Envelope> ReadEnvelopeAsync(HttpResponseMessage response)
    {
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>(JsonOptions);
        Assert.NotNull(envelope);
        return envelope;
    }

    private async Task<HttpClient> LoginAsync(string userName)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            // https BẮT BUỘC — cookie phiên khai CookieSecurePolicy.Always.
            BaseAddress = new Uri("https://localhost"),
        });

        // GET không cần CSRF, nhưng POST /api/auth/login thì CÓ (CSRF áp theo METHOD).
        await client.WithCsrfTokenAsync();
        var login = await client.PostAsJsonAsync("/api/auth/login", new { userName, password = Password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        return client;
    }

    private async Task<string> CreateUserAsync(string[] roles)
    {
        using var scope = _factory.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                Assert.True((await roleManager.CreateAsync(new AppRole(role) { Id = Guid.NewGuid() })).Succeeded);
        }

        var userName = $"it-ulq-{Guid.NewGuid():N}"[..24];
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
}

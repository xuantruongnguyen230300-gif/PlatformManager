using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using PlatformManager.Core.Application.Common;
using PlatformManager.Core.Application.Common.Models;
using PlatformManager.Core.Application.Common.Results;
using PlatformManager.Core.Application.Users;
using Xunit;

namespace PlatformManager.Core.IntegrationTests.Users;

/// <summary>
/// CONTRACT USER — <c>PUT /api/users/{id}</c> sau <b>quyết định người dùng 2026-08-30</b>, quyết
/// định 3: "thêm kiểm tranh chấp ghi — <c>GET</c> trả kèm <c>version</c>, <c>PUT</c> gửi lại; lệch
/// ⇒ <b>409</b>" (doc/contracts/users.md).
///
/// <para><b>Vế "hai admin sửa cùng một người"</b> mà card mô tả: người lưu sau ghi đè việc của
/// người lưu trước, im lặng, không dấu vết. Gate role thu hẹp <i>ai</i> ghi được, không thu hẹp
/// <i>bao nhiêu người</i> ghi cùng lúc.</para>
///
/// <para><b>⚠️ <c>version</c> ở đây đang là TUỲ CHỌN, khác PERM-1/PERM-2.</b> Thiếu token ở hai ma
/// trận phân quyền là 409; thiếu ở đây thì request đi qua như cũ. Đó là chủ đích, và lý do nằm ở
/// docstring <c>UpdateUserRequest</c>: bắt buộc ngay sẽ làm mọi client chưa cập nhật nhận 409, tức
/// khoá luôn màn Quản trị người dùng. <see cref="Put_WithoutVersion_StillSucceeds"/> khoá lại đúng
/// trạng thái nửa vời đó — <b>nó là mỏ neo, không phải lời khen</b>: ngày siết thành bắt buộc,
/// chính test đó phải đỏ và phải được sửa cùng lượt, thay vì việc siết lặng lẽ không có hiệu lực
/// (điều kiện <c>cmd.Version is not null</c> trong <c>UpdateUserHandler</c> còn nguyên).</para>
///
/// <para>Không cần dọn dữ liệu: mỗi test tự tạo tài khoản riêng và chỉ sửa tài khoản đó.</para>
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class UserUpdateVersionTests : IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;

    public UserUpdateVersionTests(PostgresFixture fixture)
    {
        // PHẢI đặt trước khi host boot — xem IntegrationTestHostEnvironment.
        IntegrationTestHostEnvironment.Configure(fixture.ConnectionString);
        _factory = new WebApplicationFactory<Program>();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    [Fact(DisplayName = "PUT /api/users/{id} với version vừa nhận từ GET → 200")]
    public async Task Put_WithFreshVersion_Succeeds()
    {
        var (client, target) = await ArrangeAsync();

        await AdminApiTestClient.AssertOkAsync(
            client.PutAsJsonAsync($"/api/users/{target.Id}", Body(target, "Tên mới", target.Version)));

        Assert.Equal("Tên mới", (await ReloadAsync(client, target.UserName)).FullName);
    }

    [Fact(DisplayName = "PUT với version bịa → 409 USER.VERSION_CONFLICT, KHÔNG ghi gì")]
    public async Task Put_WithStaleVersion_Returns409_AndLeavesUserUntouched()
    {
        var (client, target) = await ArrangeAsync();

        var response = await client.PutAsJsonAsync(
            $"/api/users/{target.Id}", Body(target, "Tên KHÔNG được lưu", Guid.NewGuid().ToString()));

        await AssertVersionConflictAsync(response);
        Assert.Equal(target.FullName, (await ReloadAsync(client, target.UserName)).FullName);
    }

    /// <summary>
    /// Hai admin cùng mở một tài khoản, cùng cầm một <c>version</c>: người lưu trước thắng, người
    /// lưu sau nhận 409. Đối xứng với phép nghiệm thu số 4 của
    /// <c>doc/contracts/permissions.md</c> — cùng một mẫu kiểm tranh chấp ghi, chỉ khác token
    /// (<c>ConcurrencyStamp</c> có sẵn của Identity, không thêm cột).
    /// </summary>
    [Fact(DisplayName = "Hai PUT liên tiếp cùng một version → lần đầu 200, lần sau 409 và không ghi đè")]
    public async Task TwoPuts_WithSameVersion_SecondIsRejected()
    {
        var (client, target) = await ArrangeAsync();
        var shared = target.Version;

        await AdminApiTestClient.AssertOkAsync(
            client.PutAsJsonAsync($"/api/users/{target.Id}", Body(target, "Người lưu trước", shared)));

        var second = await client.PutAsJsonAsync(
            $"/api/users/{target.Id}", Body(target, "Người lưu sau", shared));

        await AssertVersionConflictAsync(second);

        // Không chỉ "trả 409": việc của người lưu trước phải còn nguyên.
        Assert.Equal("Người lưu trước", (await ReloadAsync(client, target.UserName)).FullName);
    }

    /// <summary>Mỏ neo cho trạng thái nửa vời — xem cảnh báo ở docstring class trước khi sửa
    /// test này.</summary>
    [Fact(DisplayName = "PUT KHÔNG gửi version → vẫn 200 (version còn là TUỲ CHỌN ở endpoint này)")]
    public async Task Put_WithoutVersion_StillSucceeds()
    {
        var (client, target) = await ArrangeAsync();

        await AdminApiTestClient.AssertOkAsync(
            client.PutAsJsonAsync($"/api/users/{target.Id}", Body(target, "Không kèm token", version: null)));

        Assert.Equal("Không kèm token", (await ReloadAsync(client, target.UserName)).FullName);
    }

    // ── Hạ tầng test ─────────────────────────────────────────────────────────

    /// <summary>Client đăng nhập bằng Admin + một tài khoản đích mới tinh, đã đọc về qua API (nên
    /// <c>Version</c> là chuỗi THẬT mà client nhận được, không phải giá trị lấy lén từ DB).</summary>
    private async Task<(HttpClient Client, UserDto Target)> ArrangeAsync()
    {
        var client = await AdminApiTestClient.CreateLoggedInClientAsync(_factory, "ver", Roles.Admin);
        var targetName = await AdminApiTestClient.CreateUserAsync(_factory, "vtgt", Roles.User);

        return (client, await ReloadAsync(client, targetName));
    }

    private static object Body(UserDto target, string fullName, string? version) => new
    {
        email = target.Email,
        fullName,
        roles = target.Roles,
        version,
    };

    /// <summary>Đọc lại tài khoản qua chính <c>GET /api/users</c> — không đọc thẳng DB, vì thứ
    /// đang chốt gồm cả việc <c>version</c> có ĐƯỢC TRẢ RA cho client hay không. Đọc lén
    /// <c>ConcurrencyStamp</c> từ DbContext sẽ làm test xanh kể cả khi endpoint quên trả token,
    /// tức bỏ lọt đúng nửa đầu của hợp đồng.</summary>
    private static async Task<UserDto> ReloadAsync(HttpClient client, string userName)
    {
        var response = await client.GetAsync($"/api/users?searchText={userName}&pageSize=50");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var page = await AdminApiTestClient.ReadDataAsync<PagedList<UserDto>>(response);
        var user = page.Items.Single(u => u.UserName == userName);

        Assert.False(string.IsNullOrWhiteSpace(user.Version),
            "GET /api/users phải trả kèm `version` (ConcurrencyStamp) — thiếu nó thì FE không có gì để gửi lại ở PUT.");

        return user;
    }

    private static async Task AssertVersionConflictAsync(HttpResponseMessage response)
    {
        if (response.StatusCode != HttpStatusCode.Conflict)
            Assert.Fail($"Mong đợi 409, nhận {(int)response.StatusCode}. Body: {await response.Content.ReadAsStringAsync()}");

        var error = await AdminApiTestClient.ReadEnvelopeAsync(response);
        Assert.Equal("BUSINESS_ERROR", error.Status);
        Assert.Equal(nameof(ErrorCode.Conflict), error.Code);
        Assert.Equal(UserErrors.VersionConflict.BusinessCode, error.BusinessCode);
    }
}

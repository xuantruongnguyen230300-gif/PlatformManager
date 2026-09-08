using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlatformManager.Core.Application.Common;
using PlatformManager.Core.Application.Common.Results;
using PlatformManager.Api.Permissions;
using PlatformManager.Core.Application.Permissions;
using PlatformManager.Core.Infrastructure.Persistence;
using Xunit;

namespace PlatformManager.Core.IntegrationTests.Permissions;

/// <summary>
/// CONTRACT PERM-2 — <c>GET/PUT /api/admin/permissions/resources</c> (xem doc/contracts/permissions.md).
///
/// <para><b>Vì sao là integration test qua HTTP thật, không phải unit test của handler:</b> thứ
/// đang được kiểm chứng KHÔNG phải logic handler (nó đã đúng suốt từ 2026-08-18) mà là việc
/// <b>có endpoint nào gọi tới nó hay không</b>. Trong khoảng 2026-08-18 → 2026-08-29,
/// <c>GetResourcePermissionMatrixQuery</c>/<c>UpdateResourcePermissionMatrixCommand</c> tồn tại
/// đầy đủ kèm handler + validator + repository, docstring tự khai route
/// <c>/api/admin/permissions/resources</c>, nhưng <c>PermissionsController</c> KHÔNG có action nào
/// map vào route đó — FE gọi sẽ nhận 404. Không lỗi biên dịch, không test nào đỏ, vì mọi test khi
/// đó đều gọi thẳng handler. Bài học giống hệt <see cref="RequirePermissionSeamTests"/>: chứng minh
/// logic đúng KHÔNG chứng minh được nó đã được nối vào pipeline HTTP.</para>
///
/// <para><b>Vòng GHI → ĐỌC (không chỉ 2 lần smoke độc lập):</b> mỗi test ghi qua <c>PUT</c> rồi
/// đọc lại bằng <c>GET</c> và khẳng định thấy đúng thứ vừa ghi. Chỉ assert "PUT trả 200" sẽ xanh
/// kể cả khi action PUT bị nối nhầm sang <c>UpdatePermissionMatrixCommand</c> (PERM-1, ghi
/// <c>SysMenuRole</c> — bảng hoàn toàn khác); chỉ assert "GET trả 200" sẽ xanh kể cả khi nó trả
/// ma trận menu. Cặp ghi-rồi-đọc là thứ duy nhất chốt được đúng CẶP query/command.</para>
///
/// <para><b>🚧 Cập nhật 2026-09-01 theo quyết định người dùng 2026-08-30</b>
/// (doc/contracts/permissions.md §"Quyết định người dùng 2026-08-30"). Ba thay đổi làm bản trước
/// của class này sai:</para>
/// <list type="number">
///   <item><c>PUT</c> đòi <c>version</c> lấy từ <c>GET</c>; thiếu hoặc lệch ⇒ <b>409</b>. Nên mọi
///     lệnh ghi ở đây phải <c>GET</c> ngay trước để lấy token.</item>
///   <item><c>PUT</c> đòi payload phủ đủ danh mục key của host. Vì vậy payload được dựng
///     TỪ CHÍNH <c>rows</c> vừa <c>GET</c> về (<see cref="BuildPayload"/>) chứ không viết tay một
///     entry — đúng cách FE bắt buộc phải làm, và không vỡ khi danh mục thêm key.</item>
///   <item><c>entries: []</c> ĐỔI NGHĨA: trước là "thu hồi toàn bộ" (200), nay là <b>400</b>. Vòng
///     3 của <see cref="Put_ThenGet_RoundTripsTheAssignedRoles"/> vì thế chuyển sang gửi key kèm
///     <c>roles: []</c> — cách thu hồi sạch DUY NHẤT còn hợp lệ.</item>
/// </list>
///
/// <para><b>⚠️ Ghi đè toàn bộ — test này ghi vào bảng <c>RolePermissions</c> DÙNG CHUNG.</b>
/// <c>ReplaceAllAsync</c> đánh dấu xoá mềm mọi dòng đang sống rồi ghi lại theo payload, nên nó phá
/// dữ liệu quyền của bất kỳ test nào chạy song song.</para>
///
/// <para><b>Cơ chế cô lập đã chọn: reset dữ liệu TRƯỚC và SAU mỗi test</b> (cách 2 trong 3 cách của
/// doc/huong_dan/wiki-core/be/04-testing-strategy.md §"Test isolation trên Postgres thật" — bản
/// tự viết, chỉ chạm đúng 1 bảng, không cần thêm package Respawn).
/// <see cref="RestoreSeededRolePermissionsAsync"/> xoá sạch <c>RolePermissions</c> rồi chạy lại
/// <c>CoreSeeder</c> để đưa bảng về ĐÚNG trạng thái seed. Cách 1 (transaction rollback) KHÔNG dùng
/// được ở đây: mỗi test đi qua nhiều request HTTP nên dữ liệu phải commit thật thì <c>GET</c> ở
/// request sau mới đọc lại được.</para>
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class ResourcePermissionEndpointTests : IAsyncLifetime
{
    private const string ResourcesUrl = "/api/admin/permissions/resources";

    private readonly PostgresFixture _fixture;
    private readonly WebApplicationFactory<Program> _factory;

    public ResourcePermissionEndpointTests(PostgresFixture fixture)
    {
        _fixture = fixture;

        // PHẢI đặt trước khi host boot — xem IntegrationTestHostEnvironment.
        IntegrationTestHostEnvironment.Configure(fixture.ConnectionString);
        _factory = new WebApplicationFactory<Program>();
    }

    /// <summary>Reset TRƯỚC test, không chỉ sau (thêm 2026-09-01): <c>PermissionCheckerTests</c>
    /// cũng ghi vào <c>RolePermissions</c> (key/role tự sinh) và không dọn, nên trạng thái đầu vào
    /// của class này không được coi là "đúng như seed" chỉ vì bản thân nó dọn sạch lúc ra.</summary>
    public Task InitializeAsync() => RestoreSeededRolePermissionsAsync();

    public async Task DisposeAsync()
    {
        await RestoreSeededRolePermissionsAsync();
        await _factory.DisposeAsync();
    }

    /// <summary>
    /// Đưa <c>RolePermissions</c> về đúng trạng thái seed — xem khối "Cơ chế cô lập" ở docstring
    /// class.
    ///
    /// <para>Xoá sạch rồi gọi <c>CoreSeeder</c> thay vì tự chèn lại bằng tay: seeder LÀ nguồn sự
    /// thật của "trạng thái nền" (Admin + User × danh mục key của host), nên khi tập key
    /// hoặc quy tắc seed đổi thì bản khôi phục ở đây đi theo, không lệch âm thầm. Seeder idempotent
    /// nên gọi thừa không hại gì.</para>
    ///
    /// <para><b><c>IgnoreQueryFilters()</c> là bắt buộc kể từ 2026-08-31</b> (khi
    /// <c>ReplaceAllAsync</c> chuyển sang xoá mềm): thiếu nó thì <c>ExecuteDelete</c> chỉ xoá các
    /// dòng ĐANG SỐNG, và mỗi test để lại một thế hệ đã xoá mềm tích luỹ mãi trong bảng — vô hại
    /// về ngữ nghĩa nhưng biến mọi phép đếm dòng thô của test khác thành số không đoán được.</para>
    ///
    /// <para><c>ExecuteDeleteAsync</c> chứ không <c>RemoveRange</c>: 1 câu <c>DELETE</c> thẳng,
    /// không cần nạp entity lên change tracker chỉ để xoá.</para>
    /// </summary>
    private async Task RestoreSeededRolePermissionsAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PlatformManagerDbContext>();
        await db.RolePermissions.IgnoreQueryFilters().ExecuteDeleteAsync();

        var seeder = scope.ServiceProvider.GetRequiredService<CoreSeeder>();
        await seeder.SeedAsync();
    }

    [Fact(DisplayName =
        "GET /api/admin/permissions/resources → 200, trả ĐỦ danh mục key của host kèm tên hiển thị, 3 role và version")]
    public async Task Get_ReturnsEveryResourceKey_WithDisplayNameAndRoleColumns()
    {
        var client = await AdminApiTestClient.CreateLoggedInClientAsync(_factory, "perm2", Roles.SuperAdmin);

        var response = await client.GetAsync(ResourcesUrl);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var matrix = await AdminApiTestClient.ReadDataAsync<ResourcePermissionMatrixDto>(response);

        // Cột của ma trận = Roles.All, KHÔNG phải "các role đang có dòng trong RolePermissions" —
        // ma trận rỗng vẫn phải đủ 3 cột để FE vẽ được lưới checkbox.
        Assert.Equal<string[]>([.. Roles.All], [.. matrix.Roles]);

        // Hàng = toàn bộ danh mục key, kể cả key CHƯA role nào được cấp (deny-by-default nghĩa là
        // "không có dòng trong DB" — nếu handler dựng rows từ DB thay vì từ danh mục thì key
        // chưa cấp cho ai sẽ biến mất khỏi UI và không ai cấp được nữa).
        var catalog = HostResourceKeys.CatalogFrom(_factory.Services);
        Assert.Equal<string[]>(
            [.. catalog.Select(definition => definition.Key).Order()],
            [.. matrix.Rows.Select(r => r.ResourceKey).Order()]);

        // ResourceName đi kèm khoá trong danh mục của HOST (tách 2026-09-03), không lưu DB.
        var displayNames = catalog.ToDictionary(
            definition => definition.Key, definition => definition.DisplayName, StringComparer.Ordinal);
        Assert.All(matrix.Rows, row => Assert.Equal(displayNames[row.ResourceKey], row.ResourceName));

        // version (2026-08-31): FE giữ nguyên chuỗi này rồi gửi lại ở PUT. Khẳng định "không rỗng"
        // là mức tối thiểu có ý nghĩa — một hiện thực quên điền sẽ trả chuỗi rỗng chứ không lỗi.
        Assert.False(string.IsNullOrWhiteSpace(matrix.Version));
    }

    [Fact(DisplayName =
        "PUT rồi GET lại → đọc ra ĐÚNG tập role vừa ghi (chứng minh nối đúng cặp query/command PERM-2)")]
    public async Task Put_ThenGet_RoundTripsTheAssignedRoles()
    {
        var client = await AdminApiTestClient.CreateLoggedInClientAsync(_factory, "perm2", Roles.SuperAdmin);

        // Vòng 1 — cấp Admin + User.
        await PutAsync(client, AppResourceKeys.Import, [Roles.Admin, Roles.User]);
        Assert.Equal<string[]>([Roles.Admin, Roles.User], [.. (await GetRowAsync(client, AppResourceKeys.Import)).AssignedRoles.Order()]);

        // Vòng 2 — CHIỀU NGƯỢC LẠI. Bắt buộc phải có: chỉ đo chiều "cấp thêm" sẽ xanh cả khi
        // ReplaceAllAsync quên bước xoá dòng cũ (bug tích luỹ, quyền không bao giờ thu hồi được).
        await PutAsync(client, AppResourceKeys.Import, [Roles.Admin]);
        Assert.Equal<string[]>([Roles.Admin], [.. (await GetRowAsync(client, AppResourceKeys.Import)).AssignedRoles]);

        // Vòng 3 — thu hồi sạch. TỪ 2026-08-31 cách làm việc này là gửi key kèm `roles: []`, KHÔNG
        // phải `entries: []` (nay là 400 — xem Put_WithEmptyEntries_...). Đây là đảo ngữ nghĩa có
        // chủ đích, không phải hồi quy.
        await PutAsync(client, AppResourceKeys.Import, []);
        Assert.Empty((await GetRowAsync(client, AppResourceKeys.Import)).AssignedRoles);
    }

    [Fact(DisplayName = "PUT với resourceKey tự bịa → 400 ValidationError, KHÔNG ghi gì vào DB")]
    public async Task Put_WithUnknownResourceKey_IsRejected_AndLeavesDataUntouched()
    {
        var client = await AdminApiTestClient.CreateLoggedInClientAsync(_factory, "perm2", Roles.SuperAdmin);

        await PutAsync(client, AppResourceKeys.Import, [Roles.Admin]);
        var before = await SnapshotAsync();

        // Payload KHÔNG cần version: validator chạy trước handler, nên ca này phải ra 400 chứ
        // không phải 409 — chính thứ tự đó là điều đang được chốt.
        var rejected = await client.PutAsJsonAsync(ResourcesUrl, new
        {
            entries = new[] { new { resourceKey = "khong.ton.tai", roles = new[] { Roles.Admin } } },
        });

        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);

        // Chốt SHAPE của body 400, không chỉ status code. doc/contracts/permissions.md §Lỗi công
        // bố body này NGUYÊN VĂN và dặn FE bind theo đúng chuỗi khoá — khoá của `fields` là
        // PascalCase theo tên property C# (`Entries[0].ResourceKey`), KHÔNG camelCase như `data`.
        // Thiếu khẳng định này thì đổi tên property `Entries` hoặc đổi
        // GlobalExceptionHandler.NormalizeField sẽ làm FE tô đỏ sai ô mà toàn bộ test BE vẫn xanh:
        // hợp đồng đã công bố hỏng trong im lặng.
        var error = await AdminApiTestClient.ReadEnvelopeAsync(rejected);
        Assert.Equal("VALIDATION_ERROR", error.Status);
        Assert.NotNull(error.Fields);
        Assert.Contains("Entries[0].ResourceKey", error.Fields.Keys);

        // Payload này cũng THIẾU mọi key thật, nên từ 2026-08-31 nó vi phạm luôn rule "phủ đủ" —
        // hai lỗi độc lập cùng trả về một lượt. Khẳng định cả hai để không ai tưởng chỉ còn một
        // rule đang chạy.
        Assert.Contains(nameof(UpdateResourcePermissionMatrixCommand.Entries), error.Fields.Keys);

        // Validator chạy TRƯỚC handler ⇒ ReplaceAllAsync không được đụng vào bảng. Thiếu khẳng
        // định này thì một hiện thực "xoá sạch rồi mới validate" cũng qua được test trên.
        Assert.Equal(before, await SnapshotAsync());
    }

    [Fact(DisplayName = "PUT với role không thuộc SuperAdmin/Admin/User → 400")]
    public async Task Put_WithUnknownRole_IsRejected()
    {
        var client = await AdminApiTestClient.CreateLoggedInClientAsync(_factory, "perm2", Roles.SuperAdmin);
        var matrix = await GetMatrixAsync(client);

        var response = await client.PutAsJsonAsync(
            ResourcesUrl, BuildPayload(matrix, AppResourceKeys.Import, ["RootGod"], matrix.Version));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── Quyết định người dùng 2026-08-30 — phủ đủ + version (thêm 2026-09-01) ─────────────────

    /// <summary>
    /// Nghiệm thu số 1 của doc/contracts/permissions.md. Hai vế, và vế thứ hai mới là vế khó: mã
    /// 400 mà bảng vẫn bị xoá thì vô nghĩa — đó đúng là thứ xảy ra nếu rule "phủ đủ" bị đặt ở
    /// handler thay vì ở validator.
    ///
    /// <para>Đây cũng là ca ĐẢO NGỮ NGHĨA: cho tới 2026-08-30 payload này trả <c>200</c> và thu
    /// hồi toàn bộ ma trận (bẫy được ghi thẳng trong card). Test cũ khẳng định đúng hành vi đó;
    /// nay nó khẳng định điều ngược lại — cố ý.</para>
    /// </summary>
    [Fact(DisplayName = "PUT với entries: [] → 400 (KHÔNG còn là 'thu hồi toàn bộ'), bảng không đổi một dòng nào")]
    public async Task Put_WithEmptyEntries_Returns400_AndLeavesTableUntouched()
    {
        var client = await AdminApiTestClient.CreateLoggedInClientAsync(_factory, "perm2", Roles.SuperAdmin);
        var matrix = await GetMatrixAsync(client);
        var before = await SnapshotAsync();

        // Kèm version ĐÚNG: nếu thiếu, một hiện thực chỉ kiểm version (không kiểm phủ đủ) cũng trả
        // 409 và test sẽ xanh vì lý do sai. Có version đúng thì 400 chỉ có thể đến từ rule phủ đủ.
        var response = await client.PutAsJsonAsync(ResourcesUrl, new
        {
            version = matrix.Version,
            entries = Array.Empty<object>(),
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await AdminApiTestClient.ReadEnvelopeAsync(response);
        Assert.Equal("VALIDATION_ERROR", error.Status);
        Assert.NotNull(error.Fields);
        Assert.Contains(nameof(UpdateResourcePermissionMatrixCommand.Entries), error.Fields.Keys);

        Assert.Equal(before, await SnapshotAsync());
    }

    /// <summary>
    /// Thiếu <c>version</c> rơi vào 409 chứ KHÔNG phải 400 — quyết định người dùng 2026-08-30
    /// (<see cref="PermissionErrors.VersionConflict"/>): với một lệnh ghi đè toàn bộ, "không gửi
    /// token" không phân biệt được với "gửi token đã cũ", cả hai đều là ghi mà không biết mình
    /// đang ghi đè lên cái gì.
    /// </summary>
    [Fact(DisplayName = "PUT thiếu version → 409 Conflict, bảng không đổi một dòng nào")]
    public async Task Put_WithoutVersion_Returns409_AndLeavesTableUntouched()
    {
        var client = await AdminApiTestClient.CreateLoggedInClientAsync(_factory, "perm2", Roles.SuperAdmin);
        var matrix = await GetMatrixAsync(client);
        var before = await SnapshotAsync();

        var response = await client.PutAsJsonAsync(
            ResourcesUrl, BuildPayload(matrix, AppResourceKeys.Import, [Roles.User], version: null));

        await AssertVersionConflictAsync(response);
        Assert.Equal(before, await SnapshotAsync());
    }

    [Fact(DisplayName = "PUT với version bịa/cũ → 409 Conflict, bảng không đổi một dòng nào")]
    public async Task Put_WithStaleVersion_Returns409_AndLeavesTableUntouched()
    {
        var client = await AdminApiTestClient.CreateLoggedInClientAsync(_factory, "perm2", Roles.SuperAdmin);
        var matrix = await GetMatrixAsync(client);
        var before = await SnapshotAsync();

        var response = await client.PutAsJsonAsync(
            ResourcesUrl,
            BuildPayload(matrix, AppResourceKeys.Import, [Roles.User], version: new string('0', 64)));

        await AssertVersionConflictAsync(response);
        Assert.Equal(before, await SnapshotAsync());
    }

    /// <summary>
    /// Nghiệm thu số 4 — mô phỏng hai người cùng mở màn Phân quyền: cả hai cầm cùng một
    /// <c>version</c>, người lưu trước thắng, người lưu sau nhận 409 và KHÔNG ghi gì.
    ///
    /// <para>Lần ghi thứ nhất phải THẬT SỰ đổi ma trận, nếu không token không đổi và lần thứ hai
    /// hợp lệ một cách chính đáng — khi đó test sẽ đỏ vì hiểu sai cơ chế chứ không phải vì code
    /// sai. <c>version</c> là hàm băm của DỮ LIỆU, không phải bộ đếm lần ghi.</para>
    /// </summary>
    [Fact(DisplayName = "Hai PUT liên tiếp cùng một version → lần đầu 200, lần sau 409 và không ghi đè")]
    public async Task TwoPuts_WithSameVersion_SecondIsRejected()
    {
        var client = await AdminApiTestClient.CreateLoggedInClientAsync(_factory, "perm2", Roles.SuperAdmin);
        var shared = await GetMatrixAsync(client);

        await AdminApiTestClient.AssertOkAsync(client.PutAsJsonAsync(
            ResourcesUrl, BuildPayload(shared, AppResourceKeys.Import, [Roles.User], shared.Version)));

        var afterFirstWrite = await SnapshotAsync();

        var second = await client.PutAsJsonAsync(
            ResourcesUrl, BuildPayload(shared, AppResourceKeys.Import, [Roles.Admin], shared.Version));

        await AssertVersionConflictAsync(second);

        // Không chỉ "trả 409" — việc của người lưu trước phải còn nguyên.
        Assert.Equal(afterFirstWrite, await SnapshotAsync());
        Assert.Equal<string[]>([Roles.User], [.. (await GetRowAsync(client, AppResourceKeys.Import)).AssignedRoles]);
    }

    /// <summary>
    /// Bẫy của xoá mềm (2026-08-31): lưu ma trận LẦN THỨ HAI với cùng một cặp
    /// (<c>RoleId</c>, <c>ResourceKey</c>). Dòng cũ chỉ bị đánh dấu <c>IsDeleted</c> chứ không biến
    /// mất, nên nếu <c>IX_RolePermissions_RoleId_ResourceKey_Active</c> thiếu mệnh đề
    /// <c>WHERE "IsDeleted" = false</c> thì lần chèn thứ hai bắn <c>23505</c> và mọi lần lưu thứ
    /// hai của cùng một ô đều hỏng.
    ///
    /// <para>Chỉ chạy được trên Postgres thật — đó là lý do nó nằm ở đây chứ không ở ArchTest:
    /// khai <c>HasFilter</c> trong model EF không chứng minh được index trong DB (dựng từ file
    /// .sql) đã thật sự có filter.</para>
    /// </summary>
    [Fact(DisplayName = "Lưu ma trận HAI LẦN cùng một cặp (RoleId, ResourceKey) → 200 cả hai, xoá mềm không gây trùng khoá")]
    public async Task PutTwice_WithSamePair_SoftDeletesInsteadOfConflicting()
    {
        var client = await AdminApiTestClient.CreateLoggedInClientAsync(_factory, "perm2", Roles.SuperAdmin);

        await PutAsync(client, AppResourceKeys.Import, [Roles.Admin]);

        // Lần thứ hai với payload Y HỆT — đây là hành động làm nổ 23505 nếu index thiếu filter.
        await PutAsync(client, AppResourceKeys.Import, [Roles.Admin]);

        Assert.Equal<string[]>([Roles.Admin], [.. (await GetRowAsync(client, AppResourceKeys.Import)).AssignedRoles]);

        await using var db = _fixture.CreateDbContext();
        var pair = db.RolePermissions.Where(x => x.ResourceKey == AppResourceKeys.Import);

        // Qua query filter: đúng MỘT dòng đang sống cho cặp (Admin, import.manage).
        Assert.Equal(1, await pair.CountAsync());

        // Bỏ query filter: nhiều hơn — mỗi lần lưu để lại một thế hệ. Đây là HÀNH VI ĐÚNG sau khi
        // ReplaceAllAsync chuyển sang xoá mềm, không phải rác cần dọn: chính các dòng này là thứ
        // trả lời được "trước đó ma trận là gì". Khẳng định ">" thay vì một con số cố định vì số
        // thế hệ phụ thuộc số lần seed/lưu trước đó trong cùng test.
        var total = await db.RolePermissions.IgnoreQueryFilters().CountAsync(x => x.ResourceKey == AppResourceKeys.Import);
        Assert.True(total > await pair.CountAsync(),
            $"Phải còn dòng đã xoá MỀM của thế hệ trước (thấy tổng {total} dòng). " +
            "Bằng nhau nghĩa là ReplaceAllAsync đang xoá CỨNG — mất lịch sử phân quyền.");
    }

    [Fact(DisplayName = "Admin (không phải SuperAdmin) gọi GET/PUT resources → 403, không phải 404")]
    public async Task Admin_IsForbidden_OnBothVerbs()
    {
        var client = await AdminApiTestClient.CreateLoggedInClientAsync(_factory, "perm2", Roles.Admin);

        // 403 chứ KHÔNG 404 là khẳng định có ý nghĩa kép: vừa chứng minh gate role có hiệu lực,
        // vừa chứng minh route THẬT SỰ TỒN TẠI. Trước 2026-08-29 route này trả 404 cho mọi người —
        // một test chỉ chấp nhận "khác 200" sẽ xanh trong đúng tình trạng hỏng đó.
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(ResourcesUrl)).StatusCode);

        var put = await client.PutAsJsonAsync(ResourcesUrl, new { entries = Array.Empty<object>() });
        Assert.Equal(HttpStatusCode.Forbidden, put.StatusCode);

        // Với PUT, chỉ nhìn status code là MƠ HỒ: 403 do gate role (OnRedirectToAccessDenied,
        // Program.cs) và 403 do CSRF hỏng (GlobalExceptionHandler, AntiforgeryValidationException)
        // có CÙNG Status=BUSINESS_ERROR và CÙNG Code=AuthorizationError — chỉ khác `message`.
        // Khẳng định message là thứ duy nhất phân biệt được NGUỒN từ chối, tức là thứ duy nhất
        // chứng minh test này đang đo gate role chứ không phải đo CSRF.
        //
        // Đo thật 2026-08-29: gửi chính request này kèm header X-XSRF-TOKEN rác vẫn nhận đúng
        // "Không có quyền truy cập." — vì UseAuthorization cắt mạch TRƯỚC middleware validate
        // CSRF. Nghĩa là hôm nay nhánh CSRF chưa với tới được request này; nhưng thứ chặn nó là
        // THỨ TỰ MIDDLEWARE — một chi tiết đổi được bất kỳ lúc nào mà không ai nhớ tới test này.
        //
        // GET không cần khẳng định này vì CSRF chỉ áp cho method ghi.
        var denied = await AdminApiTestClient.ReadEnvelopeAsync(put);
        Assert.Equal("Không có quyền truy cập.", denied.Message);

        // Gate role cắt mạch TRƯỚC cả validator: payload `entries: []` ở trên là payload 400 kể từ
        // 2026-08-31, nhưng người không đủ quyền vẫn phải nhận 403 — không được lộ ra rằng payload
        // của họ hợp lệ hay không.
    }

    // ── Hạ tầng test ─────────────────────────────────────────────────────────

    /// <summary>
    /// Dựng body <c>PUT</c> PHỦ ĐỦ danh mục key từ chính ma trận vừa <c>GET</c>
    /// về, chỉ thay tập role của <paramref name="resourceKey"/>.
    ///
    /// <para>Đây đúng là thứ FE bắt buộc phải làm kể từ quyết định 2026-08-30 (gửi lại toàn bộ
    /// <c>rows</c>), nên dựng payload theo cách này vừa kiểm đúng hợp đồng, vừa không vỡ khi
    /// danh mục có thêm key.</para>
    /// </summary>
    private static object BuildPayload(
        ResourcePermissionMatrixDto matrix, string resourceKey, string[] roles, string? version)
        => new
        {
            version,
            entries = matrix.Rows
                .Select(row => new
                {
                    resourceKey = row.ResourceKey,
                    roles = row.ResourceKey == resourceKey ? roles : row.AssignedRoles.ToArray(),
                })
                .ToArray(),
        };

    /// <summary><c>GET</c> để lấy <c>version</c> mới nhất rồi <c>PUT</c> — gộp lại vì kể từ
    /// 2026-08-31 không còn cách ghi nào khác: token phải lấy ngay trước lệnh ghi.</summary>
    private static async Task PutAsync(HttpClient client, string resourceKey, string[] roles)
    {
        var matrix = await GetMatrixAsync(client);
        await AdminApiTestClient.AssertOkAsync(
            client.PutAsJsonAsync(ResourcesUrl, BuildPayload(matrix, resourceKey, roles, matrix.Version)));
    }

    private static async Task AssertVersionConflictAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var error = await AdminApiTestClient.ReadEnvelopeAsync(response);
        Assert.Equal("BUSINESS_ERROR", error.Status);

        // `code` on-wire là TÊN member của enum (JsonStringEnumConverter), không phải số — đúng
        // chuỗi mà doc/contracts/permissions.md §Lỗi dặn FE bind.
        Assert.Equal(nameof(ErrorCode.Conflict), error.Code);

        // businessCode phân biệt được NGUỒN của 409. Thiếu khẳng định này thì bất kỳ 409 nào khác
        // (kể cả một Conflict do Identity ném ra) cũng làm test xanh.
        Assert.Equal(PermissionErrors.VersionConflict.BusinessCode, error.BusinessCode);
    }

    private static async Task<ResourcePermissionMatrixDto> GetMatrixAsync(HttpClient client)
    {
        var response = await client.GetAsync(ResourcesUrl);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await AdminApiTestClient.ReadDataAsync<ResourcePermissionMatrixDto>(response);
    }

    private static async Task<ResourcePermissionRowDto> GetRowAsync(HttpClient client, string resourceKey)
        => (await GetMatrixAsync(client)).Rows.Single(r => r.ResourceKey == resourceKey);

    /// <summary>
    /// Ảnh chụp bảng <c>RolePermissions</c> ở dạng so sánh được — dùng để chứng minh request bị từ
    /// chối KHÔNG chạm vào dữ liệu (nghiệm thu số 1 của doc/contracts/permissions.md).
    ///
    /// <para><b>Chụp kèm <c>Id</c> và <c>IsDeleted</c>, qua <c>IgnoreQueryFilters()</c></b> — có
    /// chủ đích. Từ 2026-08-31 <c>ReplaceAllAsync</c> xoá mềm rồi chèn thế hệ mới, nên một ảnh chụp
    /// chỉ gồm cặp (RoleId, ResourceKey) của dòng đang sống sẽ KHÔNG đổi khi payload trùng nội dung
    /// hiện tại — tức test "bảng không đổi" sẽ xanh kể cả khi bảng vừa bị ghi lại toàn bộ.</para>
    /// </summary>
    private async Task<string[]> SnapshotAsync()
    {
        await using var db = _fixture.CreateDbContext();
        var rows = await db.RolePermissions
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Select(rp => rp.Id + "|" + rp.RoleId + "|" + rp.ResourceKey + "|" + rp.IsDeleted)
            .ToListAsync();

        return [.. rows.Order(StringComparer.Ordinal)];
    }
}

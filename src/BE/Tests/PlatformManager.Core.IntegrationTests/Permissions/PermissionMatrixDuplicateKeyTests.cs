using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using PlatformManager.Core.Application.Common;
using PlatformManager.Core.Application.Permissions;
using Xunit;

namespace PlatformManager.Core.IntegrationTests.Permissions;

/// <summary>
/// Khoá lại finding "key trùng trong <c>entries</c> → 500 thay vì 400" (2026-08-29), cho CẢ HAI
/// ma trận: PERM-1 (<c>PUT /api/admin/permissions</c>, key là <c>sysMenuId</c>) và PERM-2
/// (<c>PUT /api/admin/permissions/resources</c>, key là <c>resourceKey</c>).
///
/// <para><b>Lỗi gốc:</b> cả hai handler dựng <c>Dictionary</c> bằng <c>Entries.ToDictionary(…)</c>.
/// <c>ToDictionary</c> ném <see cref="ArgumentException"/> khi gặp key trùng, và không nhánh nào
/// trong <c>ExceptionHandlingBehavior</c>/<c>GlobalExceptionHandler</c> nhận diện loại exception
/// đó — nó rơi vào nhánh mặc định <c>SYSTEM_ERROR</c>, client nhận <b>500</b>. Trong khi
/// doc/contracts/permissions.md §Lỗi hứa <b>400 VALIDATION_ERROR</b>. Nay cả hai validator có rule
/// cấp collection chặn trước handler.</para>
///
/// <para><b>Vì sao phải là test qua HTTP THẬT, không phải unit test của validator:</b> thứ đang
/// được kiểm chứng là <b>MÃ TRẠNG THÁI mà client nhận được</b> — mà mã đó do chuỗi
/// <c>ValidationBehavior</c> → <c>ValidationException</c> → <c>GlobalExceptionHandler</c> quyết
/// định, không nằm trong validator. Một unit test chỉ khẳng định được "validator trả IsValid =
/// false"; nó xanh y hệt kể cả trong tình trạng hỏng cũ, vì tình trạng hỏng cũ nằm ở chỗ KHÔNG AI
/// GỌI validator trước <c>ToDictionary</c>. Đúng bài học của
/// <see cref="ResourcePermissionEndpointTests"/> và <see cref="RequirePermissionSeamTests"/>:
/// chứng minh logic đúng KHÔNG chứng minh nó đã được nối vào pipeline.</para>
///
/// <para><b>Khẳng định 400 chứ không phải "khác 200":</b> ca hỏng cũ trả 500, cũng "khác 200".
/// Test chỉ đòi <c>!IsSuccessStatusCode</c> sẽ xanh nguyên trong lúc bug còn nguyên — đây chính là
/// khác biệt duy nhất mà bản vá tạo ra, nên phải khẳng định đúng con số. Kiểm bằng cách tạm gỡ
/// rule khỏi validator: test chuyển đỏ với <c>Expected 400, Actual 500</c> (đo thật 2026-08-29).</para>
///
/// <para><b>🚧 Sửa 2026-09-01 — payload phải PHỦ ĐỦ ma trận.</b> Quyết định người dùng 2026-08-30
/// thêm một rule thứ hai lên cùng hai endpoint này ("thiếu phần tử ⇒ 400", xem
/// <c>UpdatePermissionMatrixCoverageValidator</c>). Bản trước của test PERM-1 gửi hai entry mang
/// một <c>Guid</c> tự sinh, nên từ 2026-08-31 payload đó vi phạm HAI rule cùng lúc (trùng key +
/// <c>sysMenuId</c> không tồn tại) và <c>fields["Entries"]</c> nhận hai thông điệp — khẳng định
/// <c>Assert.Single</c> vỡ. Nay payload dựng từ chính ma trận <c>GET</c> về rồi nhân đôi MỘT dòng
/// có thật, nên chỉ còn đúng một lỗi và test đo lại đúng một thứ như chủ ý ban đầu.</para>
///
/// <para><b>Không cần dọn dữ liệu:</b> mọi request trong class này đều bị validator từ chối TRƯỚC
/// handler, nên không request nào chạm được <c>ReplaceAllAsync</c> — không có gì để khôi phục.
/// Ảnh chụp trước/sau khẳng định thẳng điều đó thay vì chỉ tin lời hứa: một hiện thực "xoá sạch
/// rồi mới validate" (hoặc bản vá đặt nhầm chỗ, ví dụ try-catch quanh <c>ToDictionary</c> SAU khi
/// repo đã xoá) sẽ đỏ ở đây. Vẫn giữ
/// <c>[Collection(<see cref="PostgresCollection"/>)]</c> vì đọc bảng dùng chung.</para>
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class PermissionMatrixDuplicateKeyTests : IAsyncLifetime
{
    private const string MenuMatrixUrl = "/api/admin/permissions";
    private const string ResourceMatrixUrl = "/api/admin/permissions/resources";

    private readonly PostgresFixture _fixture;
    private readonly WebApplicationFactory<Program> _factory;

    public PermissionMatrixDuplicateKeyTests(PostgresFixture fixture)
    {
        _fixture = fixture;

        // PHẢI đặt trước khi host boot — xem IntegrationTestHostEnvironment.
        IntegrationTestHostEnvironment.Configure(fixture.ConnectionString);
        _factory = new WebApplicationFactory<Program>();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    [Fact(DisplayName =
        "PERM-2: PUT resources với resourceKey lặp → 400 VALIDATION_ERROR (KHÔNG phải 500), fields.Entries")]
    public async Task Put_Resources_WithDuplicateResourceKey_Returns400_NotServerError()
    {
        var client = await AdminApiTestClient.CreateLoggedInClientAsync(_factory, "dup", Roles.SuperAdmin);
        var before = await SnapshotRolePermissionsAsync();

        // Phủ đủ danh mục key của host (rule "thiếu phần tử ⇒ 400") RỒI nhân đôi một key — như vậy
        // lỗi duy nhất còn lại là lỗi trùng key, đúng thứ test này đo. Payload này FE sinh ra được
        // thật: ma trận PERM-2 bắt buộc gửi lại TOÀN BỘ rows, nên một lỗi ghép mảng (nối 2 lần
        // tải, hoặc append dòng vừa sửa thay vì thay thế) cho ra đúng hình dạng dưới đây.
        //
        // Nhân đôi key ĐẦU TIÊN của danh mục thay vì gọi tên một key cụ thể (tách 2026-09-03, xem
        // HostResourceKeys): test này nói về hành vi với key TRÙNG, không nói gì về dự án khai key
        // nào — buộc nó vào một tên cụ thể là gài một lý do đỏ không liên quan cho lần đổi danh mục sau.
        var allKeys = HostResourceKeys.AllFrom(_factory.Services);
        var duplicated = allKeys[0];

        var entries = allKeys
            .Select(key => new { resourceKey = key, roles = new[] { Roles.Admin } })
            .Append(new { resourceKey = duplicated, roles = new[] { Roles.User } })
            .ToArray();

        var response = await client.PutAsJsonAsync(ResourceMatrixUrl, new { entries });

        // Con số 400 LÀ nội dung của test — xem docstring class.
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await AdminApiTestClient.ReadEnvelopeAsync(response);
        Assert.Equal("VALIDATION_ERROR", error.Status);
        Assert.NotNull(error.Fields);

        // Khoá "Entries" (PascalCase, không chỉ số) vì rule nằm ở cấp COLLECTION: lỗi thuộc về
        // quan hệ GIỮA các phần tử, không quy được cho một chỉ số cụ thể. doc/contracts/permissions.md
        // §Lỗi công bố đúng khoá này cho FE.
        Assert.Contains(nameof(UpdateResourcePermissionMatrixCommand.Entries), error.Fields.Keys);

        // Thông điệp phải nêu ĐÍCH DANH key bị lặp — nếu không, người quản trị nhìn ma trận vài
        // chục dòng mà không biết sửa dòng nào. Assert.Single đồng thời chốt rằng payload chỉ vi
        // phạm ĐÚNG một rule cấp collection: thêm một thông điệp thứ hai ở đây nghĩa là test đã
        // lặng lẽ đo sang thứ khác.
        Assert.Contains(
            duplicated,
            Assert.Single(error.Fields[nameof(UpdateResourcePermissionMatrixCommand.Entries)]));

        // Validator chạy TRƯỚC handler ⇒ ReplaceAllAsync không được đụng bảng. Thiếu khẳng định
        // này thì một bản vá đặt sai chỗ (try-catch quanh ToDictionary, tức SAU khi repo đã xoá
        // sạch) cũng trả 400 và vẫn qua được test.
        Assert.Equal(before, await SnapshotRolePermissionsAsync());
    }

    [Fact(DisplayName =
        "PERM-1: PUT permissions với sysMenuId lặp → 400 VALIDATION_ERROR (KHÔNG phải 500), fields.Entries")]
    public async Task Put_MenuMatrix_WithDuplicateSysMenuId_Returns400_NotServerError()
    {
        var client = await AdminApiTestClient.CreateLoggedInClientAsync(_factory, "dup", Roles.SuperAdmin);
        var before = await SnapshotSysMenuRolesAsync();

        var matrix = await GetMenuMatrixAsync(client);
        var duplicated = matrix.Rows[0].SysMenuId;

        // Phủ đủ mọi SysMenu (dựng từ chính ma trận vừa GET) rồi nhân đôi dòng đầu — xem khối
        // "Sửa 2026-09-01" ở docstring class cho lý do không còn dùng Guid tự sinh.
        var entries = matrix.Rows
            .Select(row => new { sysMenuId = row.SysMenuId, roles = SanitizeRoles(row.AssignedRoles) })
            .Append(new { sysMenuId = duplicated, roles = new[] { Roles.User } })
            .ToArray();

        var response = await client.PutAsJsonAsync(MenuMatrixUrl, new { entries });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await AdminApiTestClient.ReadEnvelopeAsync(response);
        Assert.Equal("VALIDATION_ERROR", error.Status);
        Assert.NotNull(error.Fields);
        Assert.Contains(nameof(UpdatePermissionMatrixCommand.Entries), error.Fields.Keys);
        Assert.Contains(
            duplicated.ToString(),
            Assert.Single(error.Fields[nameof(UpdatePermissionMatrixCommand.Entries)]));

        // Cùng lý do như test PERM-2: chứng minh request 400 không xoá gì của SysMenuRole.
        Assert.Equal(before, await SnapshotSysMenuRolesAsync());
    }

    // ── Hạ tầng test ─────────────────────────────────────────────────────────

    /// <summary>
    /// Giữ lại các role thuộc <see cref="Roles.All"/> và bỏ phần còn lại.
    ///
    /// <para>Cần thiết vì database dùng chung: <c>SysMenuRoleRepositoryTests</c> gắn các role tên
    /// tự sinh (<c>itrole&lt;guid&gt;</c>) vào menu của chính nó và không dọn. Gửi thẳng
    /// <c>assignedRoles</c> lấy từ <c>GET</c> sẽ kéo theo những tên đó và làm request vi phạm rule
    /// "role phải thuộc SuperAdmin/Admin/User" — một lỗi thứ hai, không liên quan, phụ thuộc thứ
    /// tự chạy của test class.</para>
    /// </summary>
    private static string[] SanitizeRoles(IReadOnlyList<string> assignedRoles)
        => [.. assignedRoles.Where(role => Roles.All.Contains(role))];

    private static async Task<PermissionMatrixDto> GetMenuMatrixAsync(HttpClient client)
    {
        var response = await client.GetAsync(MenuMatrixUrl);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var matrix = await AdminApiTestClient.ReadDataAsync<PermissionMatrixDto>(response);

        // Chặn "xanh rỗng": ma trận không có dòng nào thì payload trùng key dựng bên dưới không
        // dựng được, và mọi khẳng định sau đó vô nghĩa. Ma trận rỗng nghĩa là seed chưa chạy —
        // xem PostgresFixture.SeedCoreAsync().
        Assert.NotEmpty(matrix.Rows);
        return matrix;
    }

    /// <summary>Ảnh chụp bảng <c>RolePermissions</c> ở dạng so sánh được — dùng để chứng minh
    /// request 400 KHÔNG chạm vào dữ liệu (cả hai lệnh đều là "ghi đè toàn bộ", nên một bản vá
    /// đặt sai chỗ sẽ xoá sạch bảng rồi mới báo lỗi).
    ///
    /// <para><c>IgnoreQueryFilters()</c> + chụp cả <c>Id</c>/<c>IsDeleted</c> kể từ 2026-08-31:
    /// <c>ReplaceAllAsync</c> nay xoá MỀM rồi chèn thế hệ mới, nên ảnh chụp chỉ gồm cặp khoá của
    /// dòng đang sống sẽ giống hệt nhau trước và sau một lượt ghi đè — tức test "bảng không đổi"
    /// xanh kể cả khi bảng vừa bị viết lại toàn bộ.</para></summary>
    private async Task<string[]> SnapshotRolePermissionsAsync()
    {
        await using var db = _fixture.CreateDbContext();
        var rows = await db.RolePermissions
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Select(rp => rp.Id + "|" + rp.RoleId + "|" + rp.ResourceKey + "|" + rp.IsDeleted)
            .ToListAsync();

        return [.. rows.Order(StringComparer.Ordinal)];
    }

    /// <summary>Xem <see cref="SnapshotRolePermissionsAsync"/> — cùng luật, bảng khác.</summary>
    private async Task<string[]> SnapshotSysMenuRolesAsync()
    {
        await using var db = _fixture.CreateDbContext();
        var rows = await db.SysMenuRoles
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Select(mr => mr.Id + "|" + mr.SysMenuId + "|" + mr.RoleId + "|" + mr.IsDeleted)
            .ToListAsync();

        return [.. rows.Order(StringComparer.Ordinal)];
    }
}

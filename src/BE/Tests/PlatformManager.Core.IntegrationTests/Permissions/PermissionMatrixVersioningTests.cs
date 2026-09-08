using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlatformManager.Core.Application.Common;
using PlatformManager.Core.Application.Common.Results;
using PlatformManager.Core.Application.Permissions;
using PlatformManager.Core.Infrastructure.Persistence;
using Xunit;

namespace PlatformManager.Core.IntegrationTests.Permissions;

/// <summary>
/// CONTRACT PERM-1 — <c>GET/PUT /api/admin/permissions</c> (ma trận <c>SysMenu × role</c>) sau
/// <b>quyết định người dùng 2026-08-30</b>: payload phải <b>phủ đủ</b> mọi <c>SysMenu</c>, và phải
/// mang <c>version</c> lấy từ <c>GET</c>. Nguồn: doc/contracts/permissions.md §"Quyết định người
/// dùng 2026-08-30" — 5 phép nghiệm thu ở đó, 4 phép đầu là phần của BE và nằm trong file này
/// (phép thứ 5 là hành vi nút Lưu của FE).
///
/// <para><b>Vì sao là file mới chứ không thêm vào
/// <see cref="PermissionMatrixDuplicateKeyTests"/>:</b> class kia cố ý KHÔNG ghi được gì — mọi
/// request của nó bị validator chặn nên nó không cần dọn dữ liệu, và đó là tính chất đáng giữ.
/// Các bài kiểm ở đây thì PHẢI ghi thật (không ghi thì không có "người lưu trước" để tạo ra 409),
/// nên chúng cần cơ chế khôi phục riêng.</para>
///
/// <para><b>Hai sự cố mà bộ test này canh</b> (chép ngắn từ card, để người đọc không phải mở
/// tài liệu mới hiểu vì sao các assert lại khắt khe đến vậy):</para>
/// <list type="number">
///   <item><b>Một cú bấm xoá sạch ma trận.</b> <c>GET</c> hỏng ⇒ FE gửi <c>entries: []</c> ⇒
///     validator cũ chỉ đòi <c>NotNull</c> nên đi qua ⇒ bảng bị xoá sạch, sidebar của mọi user
///     không phải SuperAdmin thành trắng, và không có nhật ký nào để dựng lại.</item>
///   <item><b>Hai người sửa cùng lúc thì người lưu sau xoá mất việc của người lưu trước</b> —
///     không cảnh báo, không dấu vết.</item>
/// </list>
///
/// <para><b>⚠️ Ghi đè toàn bộ vào bảng dùng chung.</b> Mỗi test reset <c>SysMenuRoles</c> về đúng
/// trạng thái seed ở cả hai đầu (xem <see cref="RestoreSeededSysMenuRolesAsync"/>). Reset ở đầu
/// KHÔNG thừa: <c>SysMenuRoleRepositoryTests</c> cũng ghi vào bảng này (với role tên tự sinh) và
/// không dọn, nên trạng thái đầu vào phụ thuộc thứ tự chạy nếu không tự dựng lại.</para>
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class PermissionMatrixVersioningTests : IAsyncLifetime
{
    private const string MatrixUrl = "/api/admin/permissions";

    private readonly PostgresFixture _fixture;
    private readonly WebApplicationFactory<Program> _factory;

    public PermissionMatrixVersioningTests(PostgresFixture fixture)
    {
        _fixture = fixture;

        // PHẢI đặt trước khi host boot — xem IntegrationTestHostEnvironment.
        IntegrationTestHostEnvironment.Configure(fixture.ConnectionString);
        _factory = new WebApplicationFactory<Program>();
    }

    public Task InitializeAsync() => RestoreSeededSysMenuRolesAsync();

    public async Task DisposeAsync()
    {
        await RestoreSeededSysMenuRolesAsync();
        await _factory.DisposeAsync();
    }

    // ── Nghiệm thu 3: GET → PUT ngay với version vừa nhận → 200 ───────────────────────────────

    [Fact(DisplayName = "GET rồi PUT ngay với version vừa nhận → 200, ma trận giữ nguyên nội dung")]
    public async Task Put_WithFreshVersion_Succeeds()
    {
        var client = await AdminApiTestClient.CreateLoggedInClientAsync(_factory, "perm1", Roles.SuperAdmin);
        var matrix = await GetMatrixAsync(client);

        await AdminApiTestClient.AssertOkAsync(
            client.PutAsJsonAsync(MatrixUrl, BuildPayload(matrix, matrix.Version)));

        // Ghi lại y nguyên nội dung cũ ⇒ đọc lại phải thấy y nguyên. Thiếu khẳng định này thì một
        // ReplaceAllAsync quên hẳn bước chèn (chỉ xoá) vẫn trả 200 và test vẫn xanh.
        var after = await GetMatrixAsync(client);
        Assert.Equal(AssignmentsOf(matrix), AssignmentsOf(after));
    }

    // ── Nghiệm thu 1 + 2: phủ đủ ─────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>Phép nghiệm thu số 1</b>, và là ca ĐẢO NGỮ NGHĨA có chủ đích: cho tới 2026-08-30 payload
    /// này trả <c>200</c> và có nghĩa "thu hồi toàn bộ". Card ghi rõ đây chính là request đã làm
    /// trắng sidebar của mọi user không phải SuperAdmin.
    ///
    /// <para>Card cũng dặn phải kiểm <b>cả hai vế</b>: "mã 400 mà bảng vẫn bị xoá thì vô nghĩa, và
    /// đó đúng là thứ xảy ra nếu ai đó chặn ở handler sau khi lệnh xoá đã chạy". Vế thứ hai là
    /// <see cref="SnapshotAsync"/> bên dưới.</para>
    /// </summary>
    [Fact(DisplayName = "PUT với entries: [] → 400 (KHÔNG còn là 'thu hồi toàn bộ'), bảng không đổi một dòng nào")]
    public async Task Put_WithEmptyEntries_Returns400_AndLeavesTableUntouched()
    {
        var client = await AdminApiTestClient.CreateLoggedInClientAsync(_factory, "perm1", Roles.SuperAdmin);
        var matrix = await GetMatrixAsync(client);
        var before = await SnapshotAsync();

        // version ĐÚNG có chủ đích: nếu bỏ trống, một hiện thực chỉ kiểm version mà quên rule phủ
        // đủ cũng trả lỗi và test sẽ xanh vì lý do sai. Có version đúng thì 400 chỉ có thể tới từ
        // rule phủ đủ.
        var response = await client.PutAsJsonAsync(MatrixUrl, new
        {
            version = matrix.Version,
            entries = Array.Empty<object>(),
        });

        await AssertCoverageRejectionAsync(response);
        Assert.Equal(before, await SnapshotAsync());
    }

    /// <summary>
    /// <b>Phép nghiệm thu số 2</b> — và là ca mạnh hơn hẳn ca <c>entries: []</c>: payload tải được
    /// 4 trên 7 menu trông hoàn toàn bình thường, <c>NotEmpty()</c> không thấy gì bất thường ở nó,
    /// nhưng nó vẫn xoá 3 menu kia. Đây là lý do quyết định chọn "phủ đủ" thay vì "cấm rỗng".
    /// </summary>
    [Fact(DisplayName = "PUT thiếu đúng MỘT sysMenuId → 400 kèm Code của menu bị thiếu, bảng không đổi")]
    public async Task Put_MissingOneSysMenu_Returns400_AndLeavesTableUntouched()
    {
        var client = await AdminApiTestClient.CreateLoggedInClientAsync(_factory, "perm1", Roles.SuperAdmin);
        var matrix = await GetMatrixAsync(client);
        Assert.True(matrix.Rows.Count >= 2, "Cần ít nhất 2 menu để bỏ bớt được một mà payload vẫn không rỗng.");

        var dropped = matrix.Rows[^1];
        var before = await SnapshotAsync();

        var response = await client.PutAsJsonAsync(MatrixUrl, new
        {
            version = matrix.Version,
            entries = matrix.Rows
                .Where(row => row.SysMenuId != dropped.SysMenuId)
                .Select(row => new { sysMenuId = row.SysMenuId, roles = SanitizeRoles(row.AssignedRoles) })
                .ToArray(),
        });

        var error = await AssertCoverageRejectionAsync(response);

        // Thông điệp nêu đích danh MÃ menu bị thiếu, không phải Id: người quản trị đọc lỗi phải
        // biết ngay thiếu mục nào mà không cần tra bảng.
        Assert.Contains(dropped.SysMenuCode, Assert.Single(error.Fields![nameof(UpdatePermissionMatrixCommand.Entries)]));

        Assert.Equal(before, await SnapshotAsync());
    }

    /// <summary>
    /// <c>sysMenuId</c> không tồn tại: <b>400</b>, không phải 500.
    ///
    /// <para>Trước rule này (bổ sung 2026-08-31) id lạ đi thẳng tới handler và vỡ khoá ngoại
    /// <c>FK_SysMenuRoles_SysMenus_SysMenuId</c> ⇒ client nhận <b>500</b>. Đó là lỗi CỦA CLIENT bị
    /// báo như lỗi hệ thống: người dùng tưởng hệ thống hỏng, và nó lọt vào log lỗi nghiêm trọng
    /// cùng chỗ với sự cố thật.</para>
    ///
    /// <para>Khẳng định đúng con số 400 là toàn bộ nội dung của test — ca hỏng cũ trả 500, cũng
    /// "khác 200", nên một test chỉ đòi <c>!IsSuccessStatusCode</c> sẽ xanh nguyên trong lúc bug
    /// còn nguyên.</para>
    /// </summary>
    [Fact(DisplayName = "PUT với sysMenuId không tồn tại → 400 (KHÔNG phải 500 do vỡ khoá ngoại), bảng không đổi")]
    public async Task Put_WithUnknownSysMenuId_Returns400_NotServerError()
    {
        var client = await AdminApiTestClient.CreateLoggedInClientAsync(_factory, "perm1", Roles.SuperAdmin);
        var matrix = await GetMatrixAsync(client);
        var before = await SnapshotAsync();

        var unknown = Guid.NewGuid();

        // Phủ đủ mọi menu THẬT rồi thêm một id lạ — như vậy chiều "thiếu" không bị vi phạm và lỗi
        // duy nhất còn lại đúng là lỗi id lạ.
        var response = await client.PutAsJsonAsync(MatrixUrl, new
        {
            version = matrix.Version,
            entries = matrix.Rows
                .Select(row => new { sysMenuId = row.SysMenuId, roles = SanitizeRoles(row.AssignedRoles) })
                .Append(new { sysMenuId = unknown, roles = new[] { Roles.Admin } })
                .ToArray(),
        });

        var error = await AssertCoverageRejectionAsync(response);
        Assert.Contains(
            unknown.ToString(),
            Assert.Single(error.Fields![nameof(UpdatePermissionMatrixCommand.Entries)]));

        Assert.Equal(before, await SnapshotAsync());
    }

    // ── Nghiệm thu 4: version ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Thiếu <c>version</c> rơi vào <b>409</b> chứ KHÔNG phải 400 — quyết định người dùng
    /// 2026-08-30 (xem <see cref="PermissionErrors.VersionConflict"/>): với một lệnh ghi đè toàn
    /// bộ, "không gửi token" không phân biệt được với "gửi token đã cũ"; cả hai đều là ghi mà
    /// không biết mình đang ghi đè lên cái gì.
    /// </summary>
    [Fact(DisplayName = "PUT thiếu version → 409 Conflict, bảng không đổi một dòng nào")]
    public async Task Put_WithoutVersion_Returns409_AndLeavesTableUntouched()
    {
        var client = await AdminApiTestClient.CreateLoggedInClientAsync(_factory, "perm1", Roles.SuperAdmin);
        var matrix = await GetMatrixAsync(client);
        var before = await SnapshotAsync();

        var response = await client.PutAsJsonAsync(MatrixUrl, BuildPayload(matrix, version: null));

        await AssertVersionConflictAsync(response);
        Assert.Equal(before, await SnapshotAsync());
    }

    [Fact(DisplayName = "PUT với version bịa → 409 Conflict, bảng không đổi một dòng nào")]
    public async Task Put_WithStaleVersion_Returns409_AndLeavesTableUntouched()
    {
        var client = await AdminApiTestClient.CreateLoggedInClientAsync(_factory, "perm1", Roles.SuperAdmin);
        var matrix = await GetMatrixAsync(client);
        var before = await SnapshotAsync();

        // 64 ký tự hex — cùng hình dạng token thật (SHA-256 hex thường), nên test đo đúng phép SO
        // KHỚP chứ không vô tình đo một phép kiểm định dạng nào đó.
        var response = await client.PutAsJsonAsync(MatrixUrl, BuildPayload(matrix, new string('0', 64)));

        await AssertVersionConflictAsync(response);
        Assert.Equal(before, await SnapshotAsync());
    }

    /// <summary>
    /// <b>Phép nghiệm thu số 4</b> — hai người cùng mở màn Phân quyền, cùng cầm một
    /// <c>version</c>: người lưu trước thắng, người lưu sau nhận 409 và KHÔNG ghi gì.
    ///
    /// <para><b>Lần ghi thứ nhất phải THẬT SỰ đổi ma trận</b>, nếu không token không đổi và lần
    /// thứ hai hợp lệ một cách chính đáng — khi đó test đỏ vì hiểu sai cơ chế chứ không phải vì
    /// code sai. <c>version</c> là hàm băm của DỮ LIỆU (<see cref="MatrixVersion"/>), không phải
    /// bộ đếm số lần ghi. Vì vậy tập role của lần ghi đầu được chọn sao cho chắc chắn khác tập
    /// hiện tại.</para>
    /// </summary>
    [Fact(DisplayName = "Hai PUT liên tiếp cùng một version → lần đầu 200, lần sau 409 và không ghi đè việc của lần đầu")]
    public async Task TwoPuts_WithSameVersion_SecondIsRejected()
    {
        var client = await AdminApiTestClient.CreateLoggedInClientAsync(_factory, "perm1", Roles.SuperAdmin);
        var shared = await GetMatrixAsync(client);
        var target = shared.Rows[0];

        // Khác tập hiện tại dù tập hiện tại là gì ⇒ hàm băm chắc chắn đổi sau lần ghi đầu.
        string[] firstWrite = target.AssignedRoles.Contains(Roles.Admin) ? [] : [Roles.Admin];

        await AdminApiTestClient.AssertOkAsync(client.PutAsJsonAsync(
            MatrixUrl, BuildPayload(shared, shared.Version, target.SysMenuId, firstWrite)));

        var afterFirstWrite = await SnapshotAsync();

        // Người thứ hai vẫn cầm token cũ (`shared.Version`) — đúng tình huống card mô tả.
        var second = await client.PutAsJsonAsync(
            MatrixUrl, BuildPayload(shared, shared.Version, target.SysMenuId, [Roles.User]));

        await AssertVersionConflictAsync(second);

        // Không chỉ "trả 409": việc của người lưu trước phải còn nguyên từng dòng.
        Assert.Equal(afterFirstWrite, await SnapshotAsync());

        var reread = await GetMatrixAsync(client);
        Assert.Equal<string[]>(
            [.. firstWrite.Order(StringComparer.Ordinal)],
            [.. reread.Rows.Single(r => r.SysMenuId == target.SysMenuId).AssignedRoles.Order(StringComparer.Ordinal)]);
    }

    // ── Xoá mềm ──────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Bẫy của xoá mềm (2026-08-31): lưu ma trận LẦN THỨ HAI với cùng một cặp
    /// (<c>SysMenuId</c>, <c>RoleId</c>). Dòng cũ chỉ bị đánh dấu <c>IsDeleted</c> chứ không biến
    /// mất, nên nếu <c>IX_SysMenuRoles_SysMenuId_RoleId_Active</c> thiếu mệnh đề
    /// <c>WHERE "IsDeleted" = false</c> thì lần chèn thứ hai bắn <c>23505</c> — và hậu quả không
    /// phải "một test đỏ" mà là <b>mọi lần lưu thứ hai của cùng một ô đều hỏng trên production</b>.
    ///
    /// <para>Đây đúng là cái bẫy đã dính một lần trên <c>SysMenu</c> (xem
    /// doc/huong_dan/wiki-core/be/13-core-data-migration.md mục 1), nên nó được canh riêng chứ
    /// không phó mặc cho các test khác vô tình đi qua.</para>
    ///
    /// <para>Chỉ chạy được trên Postgres thật: khai <c>HasFilter</c> trong model EF không chứng
    /// minh được index trong DB — vốn dựng từ file <c>.sql</c> chạy tay — đã thật sự có filter.
    /// Đó là toàn bộ lý do <see cref="PostgresFixture"/> dựng schema từ <c>.sql</c>.</para>
    /// </summary>
    [Fact(DisplayName = "Lưu ma trận HAI LẦN cùng một cặp (SysMenuId, RoleId) → 200 cả hai, không trùng khoá")]
    public async Task PutTwice_WithSamePair_SoftDeletesInsteadOfConflicting()
    {
        var client = await AdminApiTestClient.CreateLoggedInClientAsync(_factory, "perm1", Roles.SuperAdmin);
        var target = (await GetMatrixAsync(client)).Rows[0].SysMenuId;

        await PutAsync(client, target, [Roles.Admin]);

        // Lần thứ hai với CÙNG tập role — đây là hành động làm nổ 23505 nếu index thiếu filter.
        await PutAsync(client, target, [Roles.Admin]);

        var adminRoleId = await AdminRoleIdAsync();

        await using var db = _fixture.CreateDbContext();

        // Qua query filter: đúng MỘT dòng đang sống cho cặp đó.
        Assert.Equal(1, await db.SysMenuRoles.CountAsync(x => x.SysMenuId == target && x.RoleId == adminRoleId));

        // Bỏ query filter: phải NHIỀU HƠN — mỗi lần lưu để lại một thế hệ. Đây là HÀNH VI ĐÚNG
        // sau khi ReplaceAllAsync chuyển sang xoá mềm, không phải rác cần dọn: chính các dòng này
        // là thứ trả lời được "trước đó ma trận là gì", vế mà xoá cứng không trả lời được và cũng
        // là lý do của quyết định 2026-08-31.
        var total = await db.SysMenuRoles
            .IgnoreQueryFilters()
            .CountAsync(x => x.SysMenuId == target && x.RoleId == adminRoleId);

        Assert.True(total >= 2,
            $"Phải còn dòng đã xoá MỀM của thế hệ trước (thấy {total} dòng cho cặp này). " +
            "Bằng 1 nghĩa là ReplaceAllAsync đang xoá CỨNG — mất lịch sử phân quyền.");
    }

    // ── Hạ tầng test ─────────────────────────────────────────────────────────

    /// <summary>
    /// Dựng body <c>PUT</c> PHỦ ĐỦ mọi <c>SysMenu</c> từ chính ma trận vừa <c>GET</c> về; tuỳ chọn
    /// thay tập role của đúng một menu.
    ///
    /// <para>Đây đúng là thứ FE bắt buộc phải làm kể từ quyết định 2026-08-30 (gửi lại toàn bộ
    /// <c>rows</c>). Dựng payload theo cách này còn khiến test không vỡ khi seeder thêm menu, và
    /// không vỡ khi test class khác để lại menu trong bảng dùng chung.</para>
    /// </summary>
    private static object BuildPayload(
        PermissionMatrixDto matrix, string? version, Guid? overrideMenuId = null, string[]? overrideRoles = null)
        => new
        {
            version,
            entries = matrix.Rows
                .Select(row => new
                {
                    sysMenuId = row.SysMenuId,
                    roles = row.SysMenuId == overrideMenuId && overrideRoles is not null
                        ? overrideRoles
                        : SanitizeRoles(row.AssignedRoles),
                })
                .ToArray(),
        };

    /// <summary>
    /// Giữ lại các role thuộc <see cref="Roles.All"/>, bỏ phần còn lại — xem
    /// <c>PermissionMatrixDuplicateKeyTests.SanitizeRoles</c> cho lý do (database dùng chung, có
    /// test class khác gắn role tên tự sinh vào menu của nó).
    /// </summary>
    private static string[] SanitizeRoles(IReadOnlyList<string> assignedRoles)
        => [.. assignedRoles.Where(role => Roles.All.Contains(role))];

    /// <summary><c>GET</c> lấy version mới nhất rồi <c>PUT</c> — kể từ 2026-08-31 không còn cách
    /// ghi nào khác: token phải lấy ngay trước lệnh ghi.</summary>
    private static async Task PutAsync(HttpClient client, Guid sysMenuId, string[] roles)
    {
        var matrix = await GetMatrixAsync(client);
        await AdminApiTestClient.AssertOkAsync(
            client.PutAsJsonAsync(MatrixUrl, BuildPayload(matrix, matrix.Version, sysMenuId, roles)));
    }

    private static async Task<PermissionMatrixDto> GetMatrixAsync(HttpClient client)
    {
        var response = await client.GetAsync(MatrixUrl);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var matrix = await AdminApiTestClient.ReadDataAsync<PermissionMatrixDto>(response);

        // Chặn "xanh rỗng": ma trận rỗng thì rule "phủ đủ" thoả mãn với MỌI payload và cả file này
        // mất ý nghĩa. Ma trận rỗng nghĩa là seed chưa chạy — xem PostgresFixture.SeedCoreAsync().
        Assert.NotEmpty(matrix.Rows);
        Assert.False(string.IsNullOrWhiteSpace(matrix.Version));
        return matrix;
    }

    /// <summary>Tập gán quyền ở dạng so sánh được, dùng cho các khẳng định "nội dung không đổi".</summary>
    private static string[] AssignmentsOf(PermissionMatrixDto matrix)
        => [.. matrix.Rows
            .Select(row => $"{row.SysMenuId:N}:{string.Join(',', row.AssignedRoles.Order(StringComparer.Ordinal))}")
            .Order(StringComparer.Ordinal)];

    private static async Task<AdminApiTestClient.Envelope<object>> AssertCoverageRejectionAsync(HttpResponseMessage response)
    {
        if (response.StatusCode != HttpStatusCode.BadRequest)
            Assert.Fail($"Mong đợi 400, nhận {(int)response.StatusCode}. Body: {await response.Content.ReadAsStringAsync()}");

        var error = await AdminApiTestClient.ReadEnvelopeAsync(response);
        Assert.Equal("VALIDATION_ERROR", error.Status);
        Assert.NotNull(error.Fields);

        // Khoá "Entries" không kèm chỉ số — lỗi thuộc về TOÀN BỘ danh sách, không quy được cho một
        // phần tử. doc/contracts/permissions.md §Lỗi công bố đúng khoá này cho FE.
        Assert.Contains(nameof(UpdatePermissionMatrixCommand.Entries), error.Fields.Keys);
        return error;
    }

    private static async Task AssertVersionConflictAsync(HttpResponseMessage response)
    {
        if (response.StatusCode != HttpStatusCode.Conflict)
            Assert.Fail($"Mong đợi 409, nhận {(int)response.StatusCode}. Body: {await response.Content.ReadAsStringAsync()}");

        var error = await AdminApiTestClient.ReadEnvelopeAsync(response);
        Assert.Equal("BUSINESS_ERROR", error.Status);

        // `code` on-wire là TÊN member của enum (JsonStringEnumConverter), không phải số.
        Assert.Equal(nameof(ErrorCode.Conflict), error.Code);

        // businessCode phân biệt NGUỒN của 409 — thiếu nó thì bất kỳ Conflict nào khác cũng làm
        // test xanh.
        Assert.Equal(PermissionErrors.VersionConflict.BusinessCode, error.BusinessCode);
    }

    private async Task<Guid> AdminRoleIdAsync()
    {
        await using var db = _fixture.CreateDbContext();
        return await db.Roles.AsNoTracking().Where(r => r.Name == Roles.Admin).Select(r => r.Id).SingleAsync();
    }

    /// <summary>
    /// Ảnh chụp <c>SysMenuRoles</c> ở dạng so sánh được — dùng cho vế thứ hai của phép nghiệm thu
    /// số 1 ("bảng không đổi một dòng nào").
    ///
    /// <para><b>Chụp kèm <c>Id</c> và <c>IsDeleted</c>, qua <c>IgnoreQueryFilters()</c></b> — bắt
    /// buộc kể từ 2026-08-31. <c>ReplaceAllAsync</c> xoá mềm rồi chèn thế hệ mới, nên ảnh chụp chỉ
    /// gồm cặp (SysMenuId, RoleId) của dòng đang sống sẽ GIỐNG HỆT nhau trước và sau một lượt ghi
    /// đè trùng nội dung — tức khẳng định "không đổi" xanh kể cả khi bảng vừa bị viết lại toàn bộ,
    /// đúng thứ nó sinh ra để chặn.</para>
    /// </summary>
    private async Task<string[]> SnapshotAsync()
    {
        await using var db = _fixture.CreateDbContext();
        var rows = await db.SysMenuRoles
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Select(mr => mr.Id + "|" + mr.SysMenuId + "|" + mr.RoleId + "|" + mr.IsDeleted)
            .ToListAsync();

        return [.. rows.Order(StringComparer.Ordinal)];
    }

    /// <summary>
    /// Đưa <c>SysMenuRoles</c> về đúng trạng thái seed: xoá sạch (kể cả dòng đã xoá mềm — nếu
    /// không, thế hệ cũ tích luỹ và mọi phép đếm dòng thô thành số không đoán được) rồi chạy lại
    /// <c>CoreSeeder</c>.
    ///
    /// <para>Gọi seeder thay vì tự chèn lại bằng tay: seeder LÀ nguồn sự thật của trạng thái nền,
    /// nên khi tập menu hoặc quy tắc gán role đổi thì bản khôi phục ở đây đi theo, không lệch âm
    /// thầm. Seeder idempotent nên gọi thừa vô hại — nó cũng hồi sinh luôn menu nếu test nào đó
    /// vừa xoá mềm mất.</para>
    /// </summary>
    private async Task RestoreSeededSysMenuRolesAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PlatformManagerDbContext>();
        await db.SysMenuRoles.IgnoreQueryFilters().ExecuteDeleteAsync();

        await scope.ServiceProvider.GetRequiredService<CoreSeeder>().SeedAsync();
    }
}

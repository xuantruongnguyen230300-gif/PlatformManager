using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlatformManager.Core.Application.Common;
using PlatformManager.Core.Application.Menu;
using PlatformManager.Core.Domain.Entities;
using PlatformManager.Core.Infrastructure.Persistence;
using Xunit;

namespace PlatformManager.Core.IntegrationTests.Menu;

/// <summary>
/// Cơ chế seed menu của <c>CoreSeeder</c> đọc qua seam <see cref="ICoreMenuSeedSource"/> — tách
/// 2026-09-02, xem docstring của seam đó.
///
/// <para><b>Vì sao bộ test này tồn tại:</b> lượt tách đó chuyển DỮ LIỆU menu ra host và để lại
/// trong Core ba đoạn CƠ CHẾ mới mà không test nào chạm tới — ánh xạ <c>ParentCode → Id</c>, guard
/// trùng <c>Code</c>, guard <c>ParentCode</c> lạ. Nghiệm thu "hành vi seed không đổi" khi đó chỉ
/// tồn tại dưới dạng một lần chạy tay, tức không có gì canh nó cho lần sau. Ba đoạn ấy đều là
/// đường CHỈ chạy khi host khai sai, nên chúng không bao giờ được thực thi bởi bộ test còn lại.</para>
///
/// <para><b>Vì sao dùng nguồn GIẢ chứ không dùng <c>AppMenuSeedSource</c> thật:</b> thứ cần kiểm
/// là ca LỖI, mà host thật thì (đúng như mong đợi) không bao giờ tạo ra ca lỗi nào. Kiểm cơ chế
/// bằng dữ liệu thật sẽ chỉ chứng minh được rằng dữ liệu thật đang đúng — nó không chứng minh được
/// rằng guard còn hoạt động. Ngược lại, bám vào dữ liệu thật còn kéo nhãn/route/icon của DỰ ÁN vào
/// một test của CORE, đúng thứ mà lượt tách vừa gỡ đi (và đúng vết mà chú thích
/// <c>CoreSeederSoftDeletedMenuTests.MenuCode</c> đã dính).</para>
///
/// <para><b>Vì sao phải là INTEGRATION test:</b> hai trong bốn khẳng định là về TRẠNG THÁI DATABASE
/// sau khi seeder ném — "không để lại dòng rác" và "<c>ParentId</c> đã ghi xuống đúng". Cả hai đều
/// không kiểm được trên giá trị in-memory: change tracker của EF giữ nguyên mọi entity đã
/// <c>Add</c> kể cả khi <c>SaveChanges</c> chưa từng chạy, nên đọc lại từ chính context của seeder
/// sẽ "thấy" đúng những dòng chưa hề tồn tại trong bảng.</para>
///
/// <para><b>Cô lập dữ liệu:</b> mọi mã menu do test sinh ra mang tiền tố
/// <see cref="CodePrefix"/> + một GUID riêng cho từng test, nên không đụng 4 mục menu thật mà
/// <c>PostgresFixture.SeedCoreAsync()</c> đã dựng, cũng không đụng nhau. <see cref="DisposeAsync"/>
/// xoá CỨNG mọi dòng mang tiền tố đó sau MỖI test (xUnit dựng một instance test class cho mỗi
/// method).</para>
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class CoreSeederMenuSeedSourceTests : IAsyncLifetime
{
    /// <summary>Tiền tố nhận diện mọi dòng do bộ test này sinh ra — nền tảng của cả phép cô lập
    /// (không đụng dữ liệu thật) lẫn phép dọn (xoá cứng theo <c>LIKE</c>).</summary>
    private const string CodePrefix = "it-menuseed-";

    private readonly PostgresFixture _fixture;
    private readonly WebApplicationFactory<Program> _factory;

    /// <summary>Riêng cho từng test — hai test chạy nối nhau không được nhìn thấy dòng của nhau.</summary>
    private readonly string _prefix;

    public CoreSeederMenuSeedSourceTests(PostgresFixture fixture)
    {
        _fixture = fixture;

        // PHẢI đặt trước khi host boot — xem IntegrationTestHostEnvironment.
        IntegrationTestHostEnvironment.Configure(fixture.ConnectionString);

        _factory = new WebApplicationFactory<Program>();
        _prefix = $"{CodePrefix}{Guid.NewGuid():N}-";
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await CleanupSeededRowsAsync();
        await _factory.DisposeAsync();
    }

    // ───────────────────────────── 1. Ánh xạ ParentCode → Id ─────────────────────────────

    /// <summary>
    /// Host khai quan hệ cha-con bằng <c>Code</c> vì nó KHÔNG thể khai <c>Id</c> — <c>Id</c> chỉ
    /// tồn tại sau khi seeder dựng entity (xem <c>MenuSeedItem.ParentCode</c>). Phép phân giải ấy
    /// là toàn bộ giá trị của seam ở phía cấu trúc cây; hỏng nó thì menu con hoặc mồ côi (trồi lên
    /// đầu sidebar) hoặc trỏ vào một <c>Id</c> rác và vỡ ở <c>FK_SysMenus_SysMenus_ParentId</c>.
    ///
    /// <para>Danh sách khai CON TRƯỚC CHA có chủ đích: nó khoá luôn phần "mọi mục gốc được xử lý
    /// trước" của <c>SeedMenuAsync</c>. Khai cha trước con thì test vẫn xanh kể cả khi đoạn sắp xếp
    /// ấy bị gỡ — tức là xanh mà không đo thứ đang định đo.</para>
    /// </summary>
    [Fact(DisplayName = "ParentCode trỏ đúng → dòng con mang ParentId = Id của dòng cha vừa tạo (đọc lại từ DB)")]
    public async Task ParentCode_IsResolved_ToParentIdGeneratedInThisRun()
    {
        var parentCode = $"{_prefix}cha";
        var childCode = $"{_prefix}con";

        await RunSeederAsync(
            // CON đứng trước CHA — xem docstring.
            new MenuSeedItem(childCode, "Mục con", "/it-con", "pi-file", parentCode, 1, []),
            new MenuSeedItem(parentCode, "Mục cha", null, "pi-folder", null, 1, []));

        // Đọc lại từ một DbContext HOÀN TOÀN KHÁC context mà seeder vừa dùng: giá trị in-memory
        // không phân biệt được "đã ghi xuống bảng" với "mới chỉ nằm trong change tracker".
        await using var db = _fixture.CreateDbContext();

        var rows = await db.SysMenus
            .IgnoreQueryFilters()
            .Where(menu => menu.Code == parentCode || menu.Code == childCode)
            .ToListAsync();

        // Chặn "xanh rỗng": không có dòng nào thì mọi khẳng định dưới đây hoặc không chạy, hoặc
        // đúng một cách vô nghĩa. Đây cũng là bằng chứng seeder ĐÃ commit, không chỉ "không ném".
        Assert.Equal(2, rows.Count);

        var parent = rows.Single(menu => menu.Code == parentCode);
        var child = rows.Single(menu => menu.Code == childCode);

        Assert.NotEqual(Guid.Empty, parent.Id);
        Assert.Null(parent.ParentId);

        // Khẳng định trung tâm: con trỏ vào ĐÚNG Id mà lượt seed này sinh ra cho cha.
        Assert.Equal(parent.Id, child.ParentId);

        // Phần định nghĩa còn lại phải đi xuống nguyên vẹn — nếu seeder chỉ ghi Code/Name thì
        // khẳng định ParentId ở trên vẫn có thể đúng trong khi mọi thứ khác rơi mất.
        Assert.Equal("Mục con", child.Name);
        Assert.Equal("/it-con", child.Route);
        Assert.Equal("pi-file", child.Icon);
        Assert.Equal(1, child.DisplayOrder);
        Assert.Null(parent.Route);
    }

    // ───────────────────────────── 2. Guard trùng Code ─────────────────────────────

    /// <summary>
    /// <c>Code</c> là danh tính của mục menu, nên hai mục cùng mã là hai định nghĩa tranh nhau MỘT
    /// dòng. Không có guard thì lần seed đầu vỡ ở <c>IX_SysMenus_Code</c> với một <c>23505</c>
    /// không chỉ ra được chỗ khai sai, còn lần seed sau lại "thành công" vì cả hai đều tìm thấy
    /// dòng cũ — xem <c>CoreSeeder.GuardAgainstDuplicateCodes</c>.
    /// </summary>
    [Fact(DisplayName = "Hai mục cùng Code → InvalidOperationException nêu ĐÍCH DANH mã trùng, DB không dòng rác")]
    public async Task DuplicateCode_Throws_NamingTheDuplicate_AndWritesNothing()
    {
        var duplicated = $"{_prefix}trung";
        var innocent = $"{_prefix}khong-trung";

        await AssertRowCounterIsNotVacuousAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => RunSeederAsync(
            new MenuSeedItem(duplicated, "Bản khai A", "/it-a", null, null, 1, []),
            new MenuSeedItem(innocent, "Mục không liên quan", "/it-k", null, null, 2, []),
            new MenuSeedItem(duplicated, "Bản khai B", "/it-b", null, null, 3, [])));

        // ĐÍCH DANH, không chỉ "có ném": thông điệp phải chỉ ra mã nào trùng, và phải chỉ ra nơi
        // cần sửa (seam của host). Một thông điệp chung chung buộc người vận hành mở source đọc
        // tay đúng thứ mà guard này sinh ra để khỏi phải đọc.
        Assert.Contains(duplicated, ex.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(ICoreMenuSeedSource), ex.Message, StringComparison.Ordinal);

        // Chiều ngược — bắt buộc: thiếu nó thì một thông điệp liệt kê TẤT CẢ mã trong danh sách
        // cũng qua được khẳng định trên, và "đích danh" mất hết ý nghĩa.
        Assert.DoesNotContain(innocent, ex.Message, StringComparison.Ordinal);

        // Guard chạy TRƯỚC mọi lượt ghi ⇒ không dòng nào được tạo, kể cả mục không liên quan.
        Assert.Equal(0, await CountMenusAsync(duplicated, innocent));
    }

    // ───────────────────────────── 3. Guard ParentCode lạ ─────────────────────────────

    /// <summary>
    /// <c>ParentCode</c> không khớp mục nào là lỗi KHAI BÁO của host, không phải trạng thái dữ
    /// liệu. Hạ mục đó xuống thành mục gốc thay vì ném sẽ đẻ ra một menu mồ côi trồi lên đầu
    /// sidebar — hiện tượng rất khó truy ngược về nguyên nhân (xem <c>CoreSeeder.ResolveParentId</c>).
    ///
    /// <para>Mục gốc hợp lệ đứng TRƯỚC trong danh sách có chủ đích: nó đã đi qua
    /// <c>UpsertMenuAsync</c> (tức đã <c>Add</c> vào change tracker) lúc guard ném. Khẳng định
    /// "DB không còn dòng rác" bên dưới vì vậy đo đúng một thứ có thật — rằng lượt ném xảy ra
    /// TRƯỚC <c>SaveChangesAsync</c> và không để lại nửa cây menu.</para>
    /// </summary>
    [Fact(DisplayName = "ParentCode lạ → InvalidOperationException nêu ĐÍCH DANH mục con + mã cha, DB không dòng rác")]
    public async Task UnknownParentCode_Throws_NamingBothCodes_AndWritesNothing()
    {
        var rootCode = $"{_prefix}goc-hop-le";
        var orphanCode = $"{_prefix}mo-coi";
        var ghostParentCode = $"{_prefix}cha-khong-ton-tai";

        await AssertRowCounterIsNotVacuousAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => RunSeederAsync(
            new MenuSeedItem(rootCode, "Mục gốc hợp lệ", "/it-goc", null, null, 1, []),
            new MenuSeedItem(orphanCode, "Mục mồ côi", "/it-mo-coi", null, ghostParentCode, 1, [])));

        // Cần CẢ HAI mã: chỉ có mã cha thì không biết mục nào khai sai; chỉ có mã con thì không
        // biết nó đang trỏ đi đâu.
        Assert.Contains(orphanCode, ex.Message, StringComparison.Ordinal);
        Assert.Contains(ghostParentCode, ex.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(ICoreMenuSeedSource), ex.Message, StringComparison.Ordinal);

        // Mục gốc hợp lệ cũng KHÔNG được nằm lại: ném giữa chừng mà commit nửa cây menu còn tệ hơn
        // không seed gì, vì lần chạy sau sẽ thấy nó "đã có" và không bao giờ dựng lại cho đủ.
        Assert.Equal(0, await CountMenusAsync(rootCode, orphanCode, ghostParentCode));
    }

    // ───────────────────────────── 4. Guard tên role lạ ─────────────────────────────

    /// <summary>
    /// <b>Đây là ca NGUY HIỂM NHẤT trong bốn ca, và là ca duy nhất có hậu quả BẢO MẬT.</b>
    ///
    /// <para>Trước lượt tách seam, tên role đến từ hằng <c>Roles.*</c> nên compiler bảo vệ — bỏ qua
    /// im lặng là vô hại vì không thể sai. Nay <c>MenuSeedItem.Roles</c> là chuỗi TỰ DO do host
    /// khai, và một lỗi gõ (<c>"SuperAdmn"</c>) làm mục menu ấy không có dòng
    /// <c>SysMenuRole</c> nào. Theo hợp đồng của <c>MenuSeedItem.Roles</c>, "không có dòng nào"
    /// nghĩa là <b>mở cho MỌI user đã đăng nhập</b> — KHÔNG phải "không ai thấy". Tức một typo
    /// biến menu quản trị thành menu công khai, im lặng, và chỉ lộ ra khi có người để ý sidebar
    /// của mình có mục lạ.</para>
    ///
    /// <para>Vì hướng hỏng là "mở rộng quyền", thông điệp phải nói cả HẬU QUẢ chứ không chỉ nêu
    /// tên role sai: người đọc một dòng "role không tồn tại" sẽ mặc định đoán hậu quả là mục menu
    /// bị ẩn — đoán ngược hoàn toàn với sự thật, và sẽ hoãn việc sửa.</para>
    /// </summary>
    [Fact(DisplayName = "Roles khai tên role không tồn tại → InvalidOperationException nêu tên role sai VÀ cảnh báo mở cho mọi user")]
    public async Task UnknownRoleName_Throws_NamingRole_AndWarningAboutExposure()
    {
        var menuCode = $"{_prefix}quan-tri";
        const string TypoRole = "SuperAdmn"; // thiếu chữ 'i' của "SuperAdmin"

        await AssertRowCounterIsNotVacuousAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => RunSeederAsync(
            new MenuSeedItem(menuCode, "Mục quản trị", "/it-quan-tri", null, null, 1, [TypoRole])));

        Assert.Contains(TypoRole, ex.Message, StringComparison.Ordinal);

        // Cảnh báo HẬU QUẢ — xem docstring. Khẳng định trên cụm chữ chứ không trên nguyên câu để
        // không khoá cách hành văn, nhưng vẫn buộc thông điệp phải nói ra hướng hỏng.
        Assert.Contains("mọi user đã đăng nhập", ex.Message, StringComparison.OrdinalIgnoreCase);

        Assert.Equal(0, await CountMenusAsync(menuCode));
    }

    // ───────────────────────────── 5. Host có nối seam vào ─────────────────────────────

    /// <summary>
    /// Bốn test trên chạy trên nguồn GIẢ, nên không test nào trong số đó đỏ khi host quên đăng ký
    /// <see cref="ICoreMenuSeedSource"/>. Core cố ý KHÔNG có hiện thực mặc định (xem docstring của
    /// seam): thiếu đăng ký thì <c>CoreSeeder</c> không phân giải được từ DI và lệnh <c>--seed</c>
    /// thoát khác 0 — ồn ào, đúng như thiết kế. Test này khoá đúng phần nối dây ấy.
    ///
    /// <para>Nó cũng bắt LUÔN được lỗi gõ tên role trong dữ liệu host thật — thứ mà ca 4 chứng
    /// minh là <c>CoreSeeder</c> hiện chưa bắt.</para>
    /// </summary>
    [Fact(DisplayName = "Host đăng ký ICoreMenuSeedSource; danh sách khác rỗng, không trùng mã, mọi ParentCode/Roles đều phân giải được")]
    public void Host_RegistersSeedSource_WithSelfConsistentList()
    {
        using var scope = _factory.Services.CreateScope();

        var items = scope.ServiceProvider.GetRequiredService<ICoreMenuSeedSource>().GetMenuItems();

        Assert.NotEmpty(items);

        var codes = items.Select(item => item.Code).ToHashSet(StringComparer.Ordinal);
        Assert.Equal(items.Count, codes.Count); // không mã nào trùng

        // Chặn "xanh rỗng": không mục nào có cha thì Assert.All bên dưới quét tập rỗng và khẳng
        // định "mọi ParentCode đều phân giải được" trở nên vô nghĩa.
        var withParent = items.Where(item => item.ParentCode is not null).ToList();
        Assert.NotEmpty(withParent);
        Assert.All(withParent, item => Assert.Contains(item.ParentCode!, codes));

        // Cùng khuôn: liệt kê tập role được khai rồi khẳng định KHÁC RỖNG trước khi kiểm từng cái.
        var declaredRoles = items.SelectMany(item => item.Roles).Distinct(StringComparer.Ordinal).ToList();
        Assert.NotEmpty(declaredRoles);
        Assert.All(declaredRoles, roleName => Assert.Contains(roleName, Roles.All));
    }

    // ───────────────────────────── Hạ tầng của test ─────────────────────────────

    /// <summary>
    /// Nguồn menu GIẢ — lý do không dùng <c>AppMenuSeedSource</c> thật nằm ở docstring của class.
    /// </summary>
    private sealed class StubMenuSeedSource(IReadOnlyCollection<MenuSeedItem> items) : ICoreMenuSeedSource
    {
        public IReadOnlyCollection<MenuSeedItem> GetMenuItems() => items;
    }

    /// <summary>
    /// Chạy <c>CoreSeeder</c> THẬT với đúng đồ hình DI của host, chỉ thay MỘT dependency là nguồn
    /// menu. <see cref="ActivatorUtilities.CreateInstance{T}"/> phân giải 5 dependency còn lại
    /// (<c>DbContext</c>, <c>RoleManager</c>, <c>UserManager</c>, <c>IOptions</c>, <c>ILogger</c>)
    /// từ chính scope của host — dựng tay chúng ở đây sẽ là một bản sao thứ hai của đồ hình đó, và
    /// bản sao sẽ lệch (xem <c>PostgresFixture.SeedCoreAsync</c>).
    ///
    /// <para><c>SeedMenuAsync</c> là <c>private</c> nên phải đi qua <c>SeedAsync()</c>. Ba bước
    /// trước nó (role, quyền, tài khoản bootstrap) idempotent và đã chạy ở fixture, nên chúng
    /// không đổi trạng thái gì thêm.</para>
    /// </summary>
    private async Task RunSeederAsync(params MenuSeedItem[] items)
    {
        using var scope = _factory.Services.CreateScope();

        var seeder = ActivatorUtilities.CreateInstance<CoreSeeder>(
            scope.ServiceProvider, new StubMenuSeedSource(items));

        await seeder.SeedAsync();
    }

    /// <summary>
    /// Đếm dòng <c>SysMenus</c> mang một trong các mã đã cho, BỎ QUA query filter soft-delete —
    /// một dòng rác đã bị đánh dấu xoá vẫn là dòng rác.
    /// </summary>
    private async Task<int> CountMenusAsync(params string[] codes)
    {
        await using var db = _fixture.CreateDbContext();
        return await db.SysMenus.IgnoreQueryFilters().CountAsync(menu => codes.Contains(menu.Code));
    }

    /// <summary>
    /// Chứng minh <see cref="CountMenusAsync"/> THẬT SỰ nhìn thấy dòng mang mã do bộ test này sinh
    /// ra, TRƯỚC khi dùng nó để khẳng định "không còn dòng rác".
    ///
    /// <para><b>Vì sao cần:</b> mọi khẳng định "không có vi phạm" đều đúng một cách vô nghĩa nếu
    /// phép quét không quét trúng gì. Ở đây rủi ro rất cụ thể: sai schema (<c>core.</c>), sai cột,
    /// hay quên <c>IgnoreQueryFilters()</c> đều làm hàm đếm trả 0 cho MỌI thứ — và cả ba ca ném
    /// bên trên sẽ xanh vĩnh viễn kể cả khi seeder commit nguyên cây menu trước lúc ném.</para>
    ///
    /// <para>Thăm dò bằng một mã của CHÍNH bộ test này (không phải mã menu thật của dự án): nó
    /// chứng minh đúng đường đi mà các khẳng định kia dựa vào, mà không kéo dữ liệu host vào Core.</para>
    /// </summary>
    private async Task AssertRowCounterIsNotVacuousAsync()
    {
        var probeCode = $"{_prefix}tham-do";

        await using (var db = _fixture.CreateDbContext())
        {
            db.SysMenus.Add(SysMenu.Create(probeCode, "Dòng thăm dò", null, null, null, 0));
            await db.SaveChangesAsync();
        }

        Assert.Equal(1, await CountMenusAsync(probeCode));

        await using (var db = _fixture.CreateDbContext())
        {
            var removed = await db.Database.ExecuteSqlRawAsync(
                """DELETE FROM core."SysMenus" WHERE "Code" = {0}""", probeCode);

            Assert.Equal(1, removed);
        }

        Assert.Equal(0, await CountMenusAsync(probeCode));
    }

    /// <summary>
    /// Xoá CỨNG mọi dòng do bộ test này sinh ra. Xoá cứng chứ không xoá mềm: dòng xoá mềm vẫn nằm
    /// trong bảng và vẫn hiện ra với mọi phép đếm dùng <c>IgnoreQueryFilters()</c> — kể cả phép
    /// đếm của chính bộ test này ở lần chạy sau.
    /// </summary>
    private async Task CleanupSeededRowsAsync()
    {
        await using var db = _fixture.CreateDbContext();

        // SysMenuRoles trước — FK_SysMenuRoles_SysMenus_SysMenuId là ON DELETE CASCADE, nhưng
        // không dựa vào đó: xoá tường minh thì thứ tự đúng kể cả khi ràng buộc đổi.
        await db.Database.ExecuteSqlRawAsync(
            """
            DELETE FROM core."SysMenuRoles"
            WHERE "SysMenuId" IN (SELECT "Id" FROM core."SysMenus" WHERE "Code" LIKE {0})
            """,
            $"{CodePrefix}%");

        await db.Database.ExecuteSqlRawAsync(
            """DELETE FROM core."SysMenus" WHERE "Code" LIKE {0}""",
            $"{CodePrefix}%");
    }
}

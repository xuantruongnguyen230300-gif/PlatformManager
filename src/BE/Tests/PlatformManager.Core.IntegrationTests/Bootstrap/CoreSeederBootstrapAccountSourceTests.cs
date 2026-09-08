using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlatformManager.Core.Application.Bootstrap;
using PlatformManager.Core.Infrastructure.Persistence;
using Xunit;

namespace PlatformManager.Core.IntegrationTests.Bootstrap;

/// <summary>
/// Email và tên hiển thị của 2 tài khoản bootstrap đến từ host qua seam
/// <see cref="ICoreBootstrapAccountSource"/> — tách 2026-09-02, xem docstring của seam đó.
///
/// <para><b>Vì sao bộ test này tồn tại:</b> lượt tách chuyển 4 giá trị từ hằng số trong
/// <c>CoreSeeder</c> thành chuỗi TỰ DO do host khai, và để lại trong Core một guard mới mà không
/// test nào chạm tới. Guard đó chỉ chạy khi host khai sai, nên nó không bao giờ được thực thi bởi
/// phần còn lại của bộ test — tức nó có thể hỏng và mọi thứ vẫn xanh.</para>
///
/// <para><b>Vì sao có cả test đọc DB chứ không chỉ test guard:</b> guard xanh không chứng minh
/// được rằng seeder THẬT SỰ ghi giá trị của host xuống bảng. Một bản cài sai còn giữ nguyên hai
/// email cũ trong Core vẫn qua được mọi test guard —
/// <see cref="SeededAccounts_CarryEmailAndFullName_FromHostSeam"/> là thứ duy nhất bắt được ca đó,
/// và nó chỉ đo được trên database thật vì nó đọc lại dòng đã commit.</para>
///
/// <para><b>Bộ test này KHÔNG ghi gì.</b> Ba ca guard đều ném TRƯỚC khi seeder chạm tới tài khoản
/// nào, còn ca đọc DB dùng lại đúng 2 dòng mà <c>PostgresFixture.SeedCoreAsync()</c> đã dựng. Vì
/// vậy không có <c>DisposeAsync</c> dọn dẹp — không có gì để dọn.</para>
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class CoreSeederBootstrapAccountSourceTests : IAsyncLifetime
{
    /// <summary>Hai tên đăng nhập này Ở LẠI Core có chủ đích (chúng soi gương <c>Roles.*</c>), nên
    /// test được phép biết chúng — khác hẳn email/tên hiển thị vốn là dữ liệu của host.</summary>
    private const string SuperAdminUserName = "SuperAdmin";
    private const string AdminUserName = "Admin";

    private readonly PostgresFixture _fixture;
    private readonly WebApplicationFactory<Program> _factory;

    public CoreSeederBootstrapAccountSourceTests(PostgresFixture fixture)
    {
        _fixture = fixture;

        // PHẢI đặt trước khi host boot — xem IntegrationTestHostEnvironment.
        IntegrationTestHostEnvironment.Configure(fixture.ConnectionString);

        _factory = new WebApplicationFactory<Program>();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    // ───────────────────────── 1. Host có nối seam vào ─────────────────────────

    /// <summary>
    /// Core cố ý KHÔNG có hiện thực mặc định (xem docstring của seam): quên đăng ký thì
    /// <c>CoreSeeder</c> không phân giải được từ DI và lệnh <c>--seed</c> thoát khác 0. Test này
    /// khoá phần nối dây ấy, và khoá luôn hai điều kiện mà guard của seeder ép — để lỗi khai báo
    /// của host lộ ra ở đây chứ không lộ ra lúc chạy lệnh seed trên máy production.
    /// </summary>
    [Fact(DisplayName = "Host đăng ký ICoreBootstrapAccountSource; 2 hồ sơ khác rỗng và KHÔNG dùng chung email")]
    public void Host_RegistersBootstrapAccountSource_WithTwoDistinctProfiles()
    {
        var source = ResolveHostSource();

        Assert.False(string.IsNullOrWhiteSpace(source.SuperAdmin.Email));
        Assert.False(string.IsNullOrWhiteSpace(source.SuperAdmin.FullName));
        Assert.False(string.IsNullOrWhiteSpace(source.Admin.Email));
        Assert.False(string.IsNullOrWhiteSpace(source.Admin.FullName));

        Assert.NotEqual(source.SuperAdmin.Email, source.Admin.Email, StringComparer.OrdinalIgnoreCase);
    }

    // ───────────────────────── 2. Giá trị của host ĐI XUỐNG được tới bảng ─────────────────────────

    /// <summary>
    /// Khẳng định trung tâm của cả lượt tách: dòng trong <c>AspNetUsers</c> mang đúng email và tên
    /// hiển thị mà HOST khai, chứ không phải một cặp hằng số nào còn sót trong Core.
    ///
    /// <para>So với <b>giá trị đọc từ seam</b> chứ không so với chuỗi viết tay trong test: chép
    /// <c>"superadmin@platformmanager.local"</c> vào đây sẽ kéo đúng dữ liệu của dự án trở lại một
    /// test của Core — thứ mà lượt tách vừa gỡ đi — và test sẽ đỏ oan khi dự án đổi email hợp lệ.</para>
    ///
    /// <para>Đọc bằng một <c>DbContext</c> riêng: giá trị in-memory của change tracker không phân
    /// biệt được "đã ghi xuống bảng" với "mới chỉ được <c>Add</c>".</para>
    /// </summary>
    [Fact(DisplayName = "2 tài khoản đã seed mang ĐÚNG email + tên hiển thị mà host khai (đọc lại từ DB)")]
    public async Task SeededAccounts_CarryEmailAndFullName_FromHostSeam()
    {
        var source = ResolveHostSource();

        await using var db = _fixture.CreateDbContext();

        var users = await db.Users
            .Where(u => u.UserName == SuperAdminUserName || u.UserName == AdminUserName)
            .ToListAsync();

        // Chặn "xanh rỗng": không có dòng nào thì mọi khẳng định dưới đây không chạy.
        Assert.Equal(2, users.Count);

        var superAdmin = users.Single(u => u.UserName == SuperAdminUserName);
        var admin = users.Single(u => u.UserName == AdminUserName);

        Assert.Equal(source.SuperAdmin.Email, superAdmin.Email);
        Assert.Equal(source.SuperAdmin.FullName, superAdmin.FullName);
        Assert.Equal(source.Admin.Email, admin.Email);
        Assert.Equal(source.Admin.FullName, admin.FullName);

        // Phần LUẬT ở lại Core — kiểm cùng chỗ để một lượt "dọn dẹp" sau này không lỡ tay đẩy nốt
        // cờ này ra host: mật khẩu bootstrap chỉ dùng đúng một lần.
        Assert.True(superAdmin.MustChangePassword);
        Assert.True(admin.MustChangePassword);
    }

    // ───────────────────────── 3. Guard: trường rỗng ─────────────────────────

    /// <summary>
    /// Trước lượt tách, bốn giá trị này là hằng số nên không thể rỗng. Nay chúng là chuỗi tự do, và
    /// không tầng nào bên dưới chặn: Identity không bắt buộc email, còn <c>AppUser.FullName</c> mặc
    /// định đã là chuỗi rỗng. Không có guard thì lệnh seed vẫn thoát 0 và tài khoản quản trị hỏng
    /// nằm luôn trong DB thật.
    ///
    /// <para>Khẳng định chiều NGƯỢC (không nhắc property kia) là bắt buộc: thiếu nó thì một thông
    /// điệp liệt kê cả hai property cũng qua được, và chữ "đích danh" mất hết ý nghĩa — người đọc
    /// vẫn phải tự đoán dòng nào của host cần sửa.</para>
    ///
    /// <para>⚠️ Cả hai khẳng định đều so trên dạng ĐẦY ĐỦ <c>ICoreBootstrapAccountSource.Admin</c>
    /// chứ không so trên chữ <c>"Admin"</c> trần: <c>"Admin"</c> là chuỗi con của
    /// <c>"SuperAdmin"</c>, nên phép "không nhắc property kia" viết theo lối trần sẽ đỏ oan ở mọi
    /// ca mà SuperAdmin là bên khai sai.</para>
    /// </summary>
    [Theory(DisplayName = "Hồ sơ khai Email/FullName rỗng → InvalidOperationException nêu ĐÍCH DANH property của seam")]
    [InlineData("", "Quản trị viên hệ thống", "a@example.test", "Quản trị viên", "SuperAdmin", "Admin", "Email")]
    [InlineData("   ", "Quản trị viên hệ thống", "a@example.test", "Quản trị viên", "SuperAdmin", "Admin", "Email")]
    [InlineData("sa@example.test", "", "a@example.test", "Quản trị viên", "SuperAdmin", "Admin", "FullName")]
    [InlineData("sa@example.test", "Quản trị viên hệ thống", "", "Quản trị viên", "Admin", "SuperAdmin", "Email")]
    [InlineData("sa@example.test", "Quản trị viên hệ thống", "a@example.test", " ", "Admin", "SuperAdmin", "FullName")]
    public async Task BlankField_Throws_NamingTheOffendingProperty(
        string superAdminEmail, string superAdminFullName,
        string adminEmail, string adminFullName,
        string offendingProperty, string innocentProperty, string offendingField)
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => RunSeederAsync(
            superAdmin: new BootstrapAccountProfile(superAdminEmail, superAdminFullName),
            admin: new BootstrapAccountProfile(adminEmail, adminFullName)));

        var seam = nameof(ICoreBootstrapAccountSource);

        Assert.Contains($"{seam}.{offendingProperty}", ex.Message, StringComparison.Ordinal);
        Assert.Contains(offendingField, ex.Message, StringComparison.Ordinal);
        Assert.DoesNotContain($"{seam}.{innocentProperty}", ex.Message, StringComparison.Ordinal);
    }

    // ───────────────────────── 4. Guard: hai tài khoản chung một email ─────────────────────────

    /// <summary>
    /// <c>RequireUniqueEmail = false</c> nên KHÔNG có gì chặn ca này — không Identity, không index.
    /// Hậu quả rơi ra ngoài seeder: hai tài khoản quản trị không phân biệt được khi liên hệ hoặc
    /// khôi phục, tức ranh giới least-privilege 2026-08-24 bị xoá nhoà ở lớp con người dù bảng
    /// <c>AspNetUserRoles</c> vẫn đúng.
    ///
    /// <para>Ca chỉ khác nhau hoa/thường là ca đáng ngờ nhất và phải đỏ: Identity chuẩn hoá email
    /// trước khi ghi <c>NormalizedEmail</c>, nên hai chuỗi đó là CÙNG một email ở mọi phép tra.</para>
    /// </summary>
    [Theory(DisplayName = "2 hồ sơ dùng chung email (kể cả chỉ khác hoa/thường) → InvalidOperationException nêu email đó")]
    [InlineData("quantri@example.test", "quantri@example.test")]
    [InlineData("QuanTri@Example.test", "quantri@example.test")]
    public async Task SharedEmail_Throws_NamingTheEmail(string superAdminEmail, string adminEmail)
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => RunSeederAsync(
            superAdmin: new BootstrapAccountProfile(superAdminEmail, "Quản trị viên hệ thống"),
            admin: new BootstrapAccountProfile(adminEmail, "Quản trị viên")));

        Assert.Contains(nameof(ICoreBootstrapAccountSource), ex.Message, StringComparison.Ordinal);
        Assert.Contains(superAdminEmail, ex.Message, StringComparison.Ordinal);
    }

    // ───────────────────────── Hạ tầng của test ─────────────────────────

    private ICoreBootstrapAccountSource ResolveHostSource()
    {
        using var scope = _factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<ICoreBootstrapAccountSource>();
    }

    /// <summary>Nguồn GIẢ — ba ca guard là ca LỖI, mà host thật (đúng như mong đợi) không bao giờ
    /// tạo ra ca lỗi nào.</summary>
    private sealed class StubBootstrapAccountSource(
        BootstrapAccountProfile superAdmin, BootstrapAccountProfile admin) : ICoreBootstrapAccountSource
    {
        public BootstrapAccountProfile SuperAdmin { get; } = superAdmin;
        public BootstrapAccountProfile Admin { get; } = admin;
    }

    /// <summary>
    /// Chạy <c>CoreSeeder</c> THẬT với đúng đồ hình DI của host, chỉ thay MỘT dependency là nguồn
    /// tài khoản bootstrap — cùng khuôn với <c>CoreSeederMenuSeedSourceTests.RunSeederAsync</c>,
    /// đọc docstring ở đó cho lý do không dựng tay các dependency còn lại.
    ///
    /// <para><c>SeedBootstrapUserAsync</c> là <c>private</c> nên phải đi qua <c>SeedAsync()</c>.
    /// Hai bước trước nó (role, quyền) idempotent và đã chạy ở fixture nên không đổi trạng thái gì
    /// thêm; bước sau nó (menu) không bao giờ tới lượt vì guard đã ném.</para>
    /// </summary>
    private async Task RunSeederAsync(BootstrapAccountProfile superAdmin, BootstrapAccountProfile admin)
    {
        using var scope = _factory.Services.CreateScope();

        var seeder = ActivatorUtilities.CreateInstance<CoreSeeder>(
            scope.ServiceProvider, new StubBootstrapAccountSource(superAdmin, admin));

        await seeder.SeedAsync();
    }
}

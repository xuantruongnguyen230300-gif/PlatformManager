using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;
using PlatformManager.Core.Infrastructure.Persistence;
using Xunit;

namespace PlatformManager.Core.IntegrationTests.Production;

/// <summary>
/// Nhóm test DUY NHẤT chạy host thật ở cấu hình <b><c>Production</c></b> (chốt 2026-08-31, xem
/// doc/huong_dan/wiki-core/be/04-testing-strategy.md §"Quyết định người dùng 2026-08-31 — một nhóm
/// test chạy ở cấu hình Production").
///
/// <para><b>Vì sao tồn tại:</b> mọi test còn lại chạy ở <c>Development</c>, nên mỗi nhánh
/// <c>if (app.Environment.IsDevelopment())</c> / <c>IsProduction()</c> trong <c>Program.cs</c> chỉ
/// từng chạy theo MỘT chiều. Bốn lỗi dưới đây vì thế nằm ngoài tầm bộ test theo đúng cấu tạo của
/// nó — không phải vì viết thiếu test: Swagger lộ ra Internet, host phục vụ thật ghi dữ liệu seed
/// lên DB thật, allowlist CORS rỗng chặn sạch người dùng trong khi deploy vẫn báo thành công, và
/// <c>KnownProxies</c> khai sai khiến ai cũng giả được <c>X-Forwarded-*</c>.</para>
///
/// <para><b>KHÔNG chạy cả bộ test ở hai môi trường</b> — phần lớn test không quan tâm tới môi
/// trường, gấp đôi thời gian chạy để mua thêm rất ít. Bốn bất biến, một class.</para>
///
/// <para><b>Cách ly môi trường:</b> không có biến môi trường nào bị ghi đè — xem
/// <see cref="ProductionHostFactory"/> §"Vì sao KHÔNG đặt biến môi trường". Class vẫn nằm trong
/// <see cref="PostgresCollection"/> vì nó dùng container Postgres của collection.</para>
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class ProductionHostTests
{
    /// <summary>Database phụ (TRỐNG) cho bất biến 2 — xem
    /// <see cref="PostgresFixture.CreateEmptyDatabaseAsync"/>.</summary>
    private const string EmptyDatabaseName = "platformmanager_prod_boot";

    /// <summary>IP "người dùng thật" mà proxy khai trong <c>X-Forwarded-For</c> — dải tài liệu
    /// TEST-NET-3 (RFC 5737), không bao giờ trùng địa chỉ thật của ai.</summary>
    private const string ForwardedClientIp = "198.51.100.7";

    private readonly PostgresFixture _fixture;

    public ProductionHostTests(PostgresFixture fixture)
    {
        _fixture = fixture;

        // Đặt ConnectionStrings__Default (và phần cấu hình bắt buộc còn lại) cho các host dùng
        // database chính. Host Production tự ghi đè MÔI TRƯỜNG qua UseEnvironment, không đụng biến
        // môi trường nào — nên lời gọi này vẫn để nguyên trạng thái Development cho mọi test khác.
        IntegrationTestHostEnvironment.Configure(fixture.ConnectionString);
    }

    // ── Bất biến 1 — Swagger KHÔNG được map ở Production ──────────────────────

    /// <summary>
    /// <c>app.UseSwagger()/UseSwaggerUI()</c> nằm sau hàng rào <c>IsDevelopment()</c>. Bỏ hàng rào
    /// (hoặc đổi thành <c>!IsProduction()</c> rồi triển khai lên một môi trường tên khác) là công
    /// bố toàn bộ mô tả API — mọi route, mọi shape DTO — ra Internet mà KHÔNG có lỗi nào báo.
    ///
    /// <para><b>Có host Development đối chứng trong cùng test</b> có chủ đích: 404 một mình không
    /// chứng minh được gì (URL gõ sai cũng 404). Cặp 200-ở-Development / 404-ở-Production mới
    /// chứng minh 404 là DO môi trường.</para>
    /// </summary>
    [Theory(DisplayName = "Swagger KHÔNG map ở Production (nhưng vẫn map ở Development — đối chứng)")]
    [InlineData("/swagger/v1/swagger.json")]
    [InlineData("/swagger/index.html")]
    public async Task Swagger_IsNotMapped_InProduction(string swaggerPath)
    {
        await using var developmentFactory = new WebApplicationFactory<Program>();
        using var developmentResponse = await developmentFactory.CreateClient().GetAsync(swaggerPath);

        Assert.Equal(HttpStatusCode.OK, developmentResponse.StatusCode);

        await using var productionFactory = new ProductionHostFactory();
        using var productionResponse = await productionFactory.CreateClient().GetAsync(swaggerPath);

        Assert.Equal(HttpStatusCode.NotFound, productionResponse.StatusCode);
    }

    // ── Bất biến 2 — seeder KHÔNG chạy lúc host khởi động ─────────────────────

    /// <summary>
    /// Đo trên một database <b>TRỐNG</b> — đúng tình huống lần triển khai Production đầu tiên.
    ///
    /// <para>Trước 2026-08-30, <c>Program.cs</c> gọi <c>CoreSeeder.SeedAsync()</c> ngay trên đường
    /// khởi động (sau hàng rào <c>IsDevelopment()</c>). Hàng rào đó sai cả hai chiều: ở Production
    /// nó để database mới không có role/tài khoản/menu nào (không ai đăng nhập được), còn ở nơi nó
    /// mở thì tiến trình PHỤC VỤ THẬT có quyền ghi dữ liệu seed. Seed nay là lệnh riêng
    /// <c>--seed</c> (<c>SeedCommand.cs</c>) và test này canh chiều thứ hai: host phục vụ KHÔNG ghi
    /// một dòng nào.</para>
    ///
    /// <para>Đếm 4 bảng mà <c>CoreSeeder</c> ghi vào. Đếm <b>trước</b> khi boot cũng có ý nghĩa: nó
    /// chứng minh database thật sự trống, nên số 0 ở lần đếm sau không phải số 0 vô nghĩa.</para>
    /// </summary>
    [Fact(DisplayName = "Host Production khởi động trên DB TRỐNG mà KHÔNG ghi một dòng seed nào")]
    public async Task Seeder_DoesNotRun_OnHostStartup()
    {
        // ⚠️ ĐỐI CHỨNG DƯƠNG — không bỏ đi. Chứng minh CountCoreSeedRowsAsync ĐẾM ĐƯỢC dữ liệu
        // seed thật. Không có nó, một câu đếm nhắm sai bảng (bảng mà CoreSeeder không bao giờ ghi
        // vào) sẽ trả 0 ở MỌI database và test xanh vĩnh viễn — kể cả khi seeder chạy thật.
        // Database chính của collection đã được PostgresFixture.SeedCoreAsync() seed.
        await _fixture.SeedCoreAsync();
        Assert.True(
            await CountCoreSeedRowsAsync(_fixture.ConnectionString) > 0,
            "Câu đếm phải thấy dữ liệu seed trên database ĐÃ seed — nếu không, số 0 bên dưới vô nghĩa.");

        var emptyDatabase = await _fixture.CreateEmptyDatabaseAsync(EmptyDatabaseName);

        Assert.Equal(0, await CountCoreSeedRowsAsync(emptyDatabase));

        await using var factory = new ProductionHostFactory(connectionString: emptyDatabase);

        // /health đi qua AddDbContextCheck ⇒ chứng minh host đã khởi động XONG (không chỉ "dựng
        // xong DI") và mở được kết nối tới database của nó.
        using var health = await factory.CreateClient().GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, health.StatusCode);

        // ⚠️ HÀNG RÀO CHỐNG TEST RỖNG — không bỏ đi.
        //
        // Hai lần đếm ở trên/dưới chạy trên `emptyDatabase` qua kết nối RIÊNG, không đi qua host.
        // Nếu ghi đè `ConnectionStrings:Default` KHÔNG ăn (host vẫn nói chuyện với database chính
        // đã seed), cả hai lần đếm vẫn ra 0 và test vẫn XANH — trong khi nó chẳng đo gì cả:
        // "host không ghi vào database mà host chưa từng mở" là một khẳng định rỗng.
        //
        // Assert này đóng lỗ đó: hỏi CHÍNH host xem nó đang trỏ vào database nào.
        using (var scope = factory.Services.CreateScope())
        {
            var hostConnectionString = scope.ServiceProvider
                .GetRequiredService<PlatformManagerDbContext>()
                .Database.GetConnectionString();

            Assert.Equal(
                EmptyDatabaseName,
                new NpgsqlConnectionStringBuilder(hostConnectionString).Database);
        }

        Assert.Equal(0, await CountCoreSeedRowsAsync(emptyDatabase));
    }

    // ── Bất biến 3 — thiếu Cors:AllowedOrigins ⇒ host KHÔNG boot được ─────────

    /// <summary>
    /// Ở Production, <c>Cors:AllowedOrigins</c> không có nguồn nào cấp giá trị:
    /// <c>appsettings.json</c> — file cấu hình duy nhất được commit — không có khoá <c>Cors</c>,
    /// còn <c>appsettings.Development.json</c> (chỗ duy nhất có khoá đó; là cấu hình cục bộ từng
    /// máy, KHÔNG nằm trong repo) thì KHÔNG được nạp. Trước 2026-08-31, <c>Program.cs</c> đọc thẳng
    /// cấu hình và kết thúc bằng <c>?? []</c> — allowlist rỗng chặn MỌI origin, tức FE không gọi
    /// được một API nào, trong khi <c>/health</c> vẫn xanh và deploy vẫn báo thành công.
    ///
    /// <para>Khẳng định ở đây gồm HAI phần, và phần thứ hai mới là phần đáng giá: host phải ném
    /// <b>lúc khởi động</b> (<c>ValidateOnStart</c>), VÀ thông điệp phải nêu ĐÍCH DANH biến môi
    /// trường cần đặt. Một lỗi fail-fast không chỉ ra cách sửa chỉ đổi "hỏng âm thầm" thành "hỏng
    /// ồn ào" — người vận hành vẫn phải đi đọc source.</para>
    /// </summary>
    [Fact(DisplayName = "Thiếu Cors:AllowedOrigins ở Production ⇒ host KHÔNG boot, lỗi nêu đích danh biến môi trường")]
    public void MissingCorsAllowedOrigins_PreventsHostStartup_InProduction()
    {
        var factory = new ProductionHostFactory(configureCors: false);

        try
        {
            // CreateClient() là chỗ WebApplicationFactory thực sự dựng + START host.
            var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

            var validationException = Flatten(exception).OfType<OptionsValidationException>().FirstOrDefault();
            Assert.NotNull(validationException);

            // Tên BIẾN MÔI TRƯỜNG (dấu __), không phải tên khoá cấu hình (dấu :) — người vận hành
            // đặt được ngay thứ đọc được trong log, không phải tự dịch giữa hai cách viết.
            Assert.Contains(
                "Cors__AllowedOrigins__0",
                string.Join(" | ", validationException.Failures),
                StringComparison.Ordinal);
        }
        finally
        {
            factory.Dispose();
        }
    }

    // ── Bất biến 4 — X-Forwarded-* chỉ được tin từ KnownProxies ───────────────

    /// <summary>
    /// Bất biến KHÓ NHẤT và ĐÁNG NHẤT trong bốn cái: nó là thứ duy nhất phân biệt "đã cấu hình
    /// <c>UseForwardedHeaders</c>" với "đã cấu hình ĐÚNG". Cấu hình sai ở đây TỆ HƠN không cấu hình
    /// gì — bật <c>ForwardedHeaders</c> mà không giới hạn <c>KnownProxies</c> thì bất kỳ ai trên
    /// Internet cũng tự khai được <c>X-Forwarded-For</c>, tức tự chọn phân vùng rate limit và vô
    /// hiệu hoá hàng rào chống brute-force đăng nhập.
    ///
    /// <para>Hai chiều đo trong cùng một test:</para>
    /// <list type="bullet">
    /// <item><b>Từ loopback</b> (nơi nginx chạy — mô hình triển khai B): header ĐƯỢC tôn trọng ⇒
    /// <c>Request.Scheme</c> thành <c>https</c> (điều kiện để cookie <c>SecurePolicy=Always</c>
    /// phát được) và <c>RemoteIpAddress</c> thành IP thật của người dùng (điều kiện để rate limit
    /// phân vùng đúng).</item>
    /// <item><b>Từ IP bất kỳ khác</b>: header bị BỎ QUA hoàn toàn — scheme giữ <c>http</c>,
    /// <c>RemoteIpAddress</c> giữ nguyên địa chỉ kết nối thật.</item>
    /// </list>
    ///
    /// <para>Chiều thứ hai chính là chiều mà một cấu hình sai làm đỏ. Không có nó, test chỉ chứng
    /// minh "middleware có chạy", tức xanh y hệt khi <c>KnownProxies</c> bị xoá sạch.</para>
    ///
    /// <para><b>Không cần proxy thật:</b> host thật, middleware thật, tuỳ chọn thật — thứ duy nhất
    /// được giả lập là địa chỉ IP của kết nối, thứ mà <c>TestServer</c> không có (xem
    /// <see cref="ProductionHostFactory"/>).</para>
    /// </summary>
    [Theory(DisplayName = "X-Forwarded-* chỉ được tôn trọng khi đến từ loopback (KnownProxies)")]
    [InlineData("127.0.0.1", "https", ForwardedClientIp)]
    [InlineData("::1", "https", ForwardedClientIp)]
    [InlineData("203.0.113.9", "http", "203.0.113.9")]
    [InlineData("10.0.0.4", "http", "10.0.0.4")]
    public async Task ForwardedHeaders_AreHonoredOnlyFromKnownProxies(
        string connectionIp,
        string expectedScheme,
        string expectedRemoteIp)
    {
        await using var factory = new ProductionHostFactory();
        var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, ProductionHostFactory.SchemeProbePath);
        request.Headers.Add(ProductionHostFactory.ClientIpHeader, connectionIp);
        request.Headers.Add("X-Forwarded-Proto", "https");
        request.Headers.Add("X-Forwarded-For", ForwardedClientIp);

        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        Assert.Equal($"{expectedScheme}|{expectedRemoteIp}", await response.Content.ReadAsStringAsync());
    }

    // ── Trợ giúp ─────────────────────────────────────────────────────────────

    /// <summary>Tổng số dòng ở 4 bảng mà <c>CoreSeeder</c> ghi vào.</summary>
    private static async Task<long> CountCoreSeedRowsAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(
            """
            SELECT (SELECT count(*) FROM core."AspNetRoles")
                 + (SELECT count(*) FROM core."AspNetUsers")
                 + (SELECT count(*) FROM core."SysMenus")
                 + (SELECT count(*) FROM core."RolePermissions");
            """,
            connection);

        return (long)(await command.ExecuteScalarAsync())!;
    }

    /// <summary>Trải phẳng chuỗi <c>InnerException</c> — host khởi động qua nhiều lớp bọc, ngoại lệ
    /// thật hiếm khi nằm ở lớp ngoài cùng.</summary>
    private static IEnumerable<Exception> Flatten(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is AggregateException aggregate)
            {
                foreach (var inner in aggregate.InnerExceptions.SelectMany(Flatten))
                    yield return inner;
            }

            yield return current;
        }
    }
}

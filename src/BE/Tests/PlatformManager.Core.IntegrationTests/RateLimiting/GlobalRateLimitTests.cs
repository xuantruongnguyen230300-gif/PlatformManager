using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using PlatformManager.Core.Infrastructure.Identity;
using Xunit;

namespace PlatformManager.Core.IntegrationTests.RateLimiting;

/// <summary>
/// <c>RateLimiterOptions.GlobalLimiter</c> — hạn mức 100 request/phút/IP áp cho MỌI endpoint,
/// kể cả endpoint KHÔNG khai <c>[EnableRateLimiting]</c> — cộng thêm envelope chuẩn cho 429.
///
/// Lỗ hổng được chốt lại ở đây (sửa 2026-08-21, finding F8 của audit 2026-08-20): policy
/// <c>"default"</c> đã khai trong <c>Program.cs</c> từ lâu nhưng KHÔNG endpoint nào gắn nó —
/// <c>grep -rn "EnableRateLimiting|RequireRateLimiting|GlobalLimiter"</c> toàn <c>src/BE</c> chỉ
/// ra đúng 1 kết quả (<c>AuthController</c>). Tức mọi API trừ đăng nhập không có giới hạn nào.
/// Chọn <c>GlobalLimiter</c> thay vì rải attribute từng controller chính là để endpoint mới
/// KHÔNG THỂ quên — nhưng "không thể quên" là một lời khẳng định, và lời khẳng định phải có test.
///
/// Bốn test dưới đây phủ các hướng KHÁC nhau, cần cả bốn:
/// <list type="bullet">
/// <item><c>RealApiEndpoint…</c> — chiều THUẬN: một endpoint thật, chưa từng khai gì về rate
/// limit, vẫn bị siết. Đây là thứ chứng minh GlobalLimiter có tác dụng.</item>
/// <item><c>RejectedRequest…Envelope…</c> — 429 trả ĐÚNG shape <c>IApiResult</c> + có
/// <c>Retry-After</c>, thay cho body rỗng hoàn toàn trước đây.</item>
/// <item><c>HealthEndpoint…</c> / <c>HangfireDashboard…</c> — chiều NGƯỢC: hai đường được miễn
/// trừ KHÔNG bị siết kể cả khi hạn mức đã cạn. Chống "gắn quá tay" — thiếu chúng thì việc siết
/// nhầm monitoring/dashboard chỉ lộ ra khi orchestrator bắt đầu báo động giả trên môi trường
/// thật. Hai đường này miễn trừ bằng HAI cơ chế khác nhau (metadata <c>DisableRateLimiting</c>
/// trên endpoint <c>/health</c> vs phân vùng <c>GetNoLimiter</c> theo đường dẫn cho
/// <c>/hangfire</c>) nên phải có hai test, không gộp được.</item>
/// </list>
///
/// ⚠️ Ranh giới của bộ test NÀY, để không ai đọc nhầm phạm vi: mọi test ở đây ĐĂNG NHẬP TRƯỚC rồi
/// mới đo, nên chúng không nói gì về lưu lượng VÔ DANH. Lỗ hổng "request chưa đăng nhập tới
/// endpoint <c>[Authorize]</c> không hề chạm rate limiter" (đo thật 2026-08-21: 150 request vô
/// danh → 401 toàn bộ, 0 lượt 429) đã được ĐÓNG cùng ngày bằng cách chuyển
/// <c>app.UseRateLimiter()</c> lên trước <c>UseAuthentication</c>; test khoá lại nó nằm ở
/// <c>PipelineOrderRateLimitTests</c> — đọc file đó để hiểu vì sao vị trí middleware hiện tại là
/// "sau <c>UseCors</c>, trước <c>UseAuthentication</c>".
///
/// Mỗi test method có host RIÊNG (xUnit dựng instance test class mới cho mỗi method) — BẮT BUỘC:
/// bộ đếm cửa sổ 1 phút sống trong DI container của host, host dùng chung sẽ khiến test chạy sau
/// thừa hưởng bộ đếm đã cạn của test chạy trước.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class GlobalRateLimitTests : IAsyncLifetime
{
    /// <summary>Khớp <c>GlobalPermitLimitPerMinute = 100</c> trong <c>Program.cs</c>. Đổi con số
    /// ở đó thì phải đổi ở đây — CỐ Ý không đọc động từ cấu hình, vì test phải phát hiện được
    /// việc hạn mức bị nới ra một cách âm thầm.</summary>
    private const int GlobalPermitLimit = 100;

    /// <summary>Khớp <c>LoginPermitLimitPerMinute = 5</c> trong <c>Program.cs</c>.</summary>
    private const int LoginPermitLimit = 5;

    private const string Password = "Test@12345";

    private readonly RateLimitPartitionFactory _factory;

    public GlobalRateLimitTests(PostgresFixture fixture)
    {
        // Cùng lý do với LoginRateLimitPartitionTests/SessionTerminationTests: PHẢI đặt bằng biến
        // môi trường TRƯỚC khi host boot — Program.cs "đông cứng" ConnectionStrings:Default vào
        // closure của AddHangfire.
        IntegrationTestHostEnvironment.Configure(fixture.ConnectionString);

        _factory = new RateLimitPartitionFactory();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    // ── Kịch bản ─────────────────────────────────────────────────────────────

    [Fact(DisplayName = "Endpoint KHÔNG khai rate limit (GET /api/auth/me) vẫn bị GlobalLimiter siết")]
    public async Task RealApiEndpoint_ExceedingGlobalQuota_IsThrottled()
    {
        // /api/auth/me được chọn vì nó KHÔNG có [EnableRateLimiting] nào — nếu nó bị chặn thì chỉ
        // có thể do GlobalLimiter. Chọn một endpoint đã khai attribute sẽ không chứng minh được
        // điều đang cần chứng minh.
        var client = await LoginAsync("203.0.113.30");

        // Chính lượt POST /api/auth/login ở trên đã tiêu 1 slot global (GlobalLimiter CỘNG DỒN
        // với policy "login", không thay thế) ⇒ chỉ còn GlobalPermitLimit - 1 lượt.
        for (var i = 1; i <= GlobalPermitLimit - 1; i++)
        {
            var response = await client.GetAsync("/api/auth/me");
            Assert.True(
                response.StatusCode == HttpStatusCode.OK,
                $"Lượt {i}/{GlobalPermitLimit - 1} trả {(int)response.StatusCode} thay vì 200 — hạn mức global đang CHẶT hơn khai báo, hoặc phiên đăng nhập hỏng vì lý do khác.");
        }

        var throttled = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.TooManyRequests, throttled.StatusCode);

        // 429 do GLOBAL từ chối cũng phải có envelope + Retry-After y hệt 429 do policy endpoint
        // từ chối (cả hai đi qua cùng một OnRejected). Kiểm mỏng ở đây, kiểm từng field đầy đủ ở
        // test envelope bên dưới — để nếu ai đó nối OnRejected riêng cho một nhánh thì có test bắt.
        Assert.NotNull(throttled.Headers.RetryAfter);
        using var body = JsonDocument.Parse(await throttled.Content.ReadAsStringAsync());
        Assert.Equal("TooManyRequests", body.RootElement.GetProperty("code").GetString());
    }

    [Fact(DisplayName = "429 trả ĐÚNG envelope IApiResult + header Retry-After (không còn body rỗng)")]
    public async Task RejectedRequest_ReturnsApiResultEnvelope_WithRetryAfterHeader()
    {
        // Dùng policy "login" (5 lượt) thay vì global (100 lượt) chỉ vì rẻ hơn 20 lần — thứ đang
        // kiểm là OnRejected, mà OnRejected là MỘT hàm dùng chung cho cả hai limiter.
        var client = await CreateClientAsync("203.0.113.40");

        for (var attempt = 1; attempt <= LoginPermitLimit; attempt++)
            await AttemptLoginAsync(client);

        var response = await AttemptLoginAsync(client);
        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);

        // 1. Header Retry-After — trước bản sửa KHÔNG có. Phải nằm trong (0; 60] giây: giá trị đến
        //    TỪ METADATA của limiter chứ không hardcode, nên nếu ai đó đổi Window mà quên thì con
        //    số này tự đi theo (test chốt "không bịa", cố ý không chốt cứng đúng 60).
        Assert.NotNull(response.Headers.RetryAfter);
        var retryAfter = response.Headers.RetryAfter!.Delta;
        Assert.NotNull(retryAfter);
        Assert.InRange(retryAfter!.Value.TotalSeconds, 1, 60);

        // 2. Content-Type JSON — trước bản sửa response KHÔNG có Content-Type nào cả.
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        // 3. Envelope: từng field một. Khẳng định "parse được thành JSON" là chưa đủ — thứ FE dựa
        //    vào là đúng những tên field camelCase này với đúng những giá trị này.
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = body.RootElement;

        Assert.Equal("TooManyRequests", root.GetProperty("code").GetString());
        Assert.Equal("BUSINESS_ERROR", root.GetProperty("status").GetString());
        Assert.Equal("RATE_LIMIT.TOO_MANY_REQUESTS", root.GetProperty("businessCode").GetString());
        Assert.True(
            root.GetProperty("retryable").GetBoolean(),
            "429 PHẢI retryable=true — chờ hết cửa sổ rồi gọi lại là thành công, người dùng không cần sửa gì.");
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("message").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("traceId").GetString()));

        // 4. data/fields vắng mặt (DefaultIgnoreCondition = WhenWritingNull) — cùng quy ước với
        //    mọi response lỗi khác, FE không phải xử lý riêng cho 429.
        Assert.False(root.TryGetProperty("data", out _));
        Assert.False(root.TryGetProperty("fields", out _));
    }

    [Fact(DisplayName = "/health KHÔNG bị siết kể cả khi hạn mức global của chính IP đó đã cạn")]
    public async Task HealthEndpoint_IsNotThrottled_EvenAfterGlobalQuotaExhausted()
    {
        var client = await LoginAsync("203.0.113.50");

        // Vắt cạn hạn mức global của IP này. Không assert trong vòng lặp — bước xác nhận "đã cạn
        // THẬT" nằm ngay dưới, và đó mới là thứ làm khẳng định về /health có sức nặng.
        for (var i = 1; i <= GlobalPermitLimit; i++)
            await client.GetAsync("/api/auth/me");

        var apiResponse = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.TooManyRequests, apiResponse.StatusCode);

        // ĐÂY là khẳng định trọng tâm: cùng IP, cùng thời điểm, hạn mức đã cạn — /health vẫn 200.
        // Nó chứng minh DisableRateLimiting() trên MapHealthChecks bỏ qua CẢ GlobalLimiter, chứ
        // không chỉ bỏ qua policy có tên. /health bị siết = monitoring/orchestrator báo động giả.
        //
        // Test này còn là phép ĐO cho một nghi vấn cụ thể (2026-08-21): sau khi UseRateLimiter
        // chuyển lên TRƯỚC UseAuthentication, metadata endpoint có còn được đọc không? CÓ — nó
        // vẫn xanh, vì WebApplication tự chèn UseRouting ở đầu pipeline nên endpoint đã được phân
        // giải trước mọi middleware. Đã kiểm ngược ở thứ tự MỚI: gỡ .DisableRateLimiting() →
        // test này ĐỎ. Nhờ vậy /health giữ nguyên cơ chế metadata, không phải chuyển sang phân
        // vùng theo đường dẫn như /hangfire.
        var healthResponse = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, healthResponse.StatusCode);

        // Gọi thêm vài lần nữa để loại khả năng lượt 200 ở trên chỉ là do còn sót đúng 1 slot.
        for (var i = 1; i <= 5; i++)
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode);
    }

    [Fact(DisplayName = "/hangfire KHÔNG bị siết kể cả khi hạn mức global của chính IP đó đã cạn")]
    public async Task HangfireDashboard_IsNotThrottled_EvenAfterGlobalQuotaExhausted()
    {
        var client = await LoginAsync("203.0.113.60");

        for (var i = 1; i <= GlobalPermitLimit; i++)
            await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.GetAsync("/api/auth/me")).StatusCode);

        // Dashboard là nhánh middleware (app.Map nội bộ của Hangfire), KHÔNG phải endpoint, nên
        // không gắn được [DisableRateLimiting] như /health. Nó từng được miễn nhờ đặt
        // UseHangfireDashboard TRƯỚC UseRateLimiter; từ 2026-08-21 cách đó không dùng được nữa
        // (UseRateLimiter đã lên trên UseAuthentication, mà Dashboard bắt buộc nằm sau
        // UseAuthentication vì filter đọc HttpContext.User) ⇒ miễn trừ chuyển sang PHÂN VÙNG THEO
        // ĐƯỜNG DẪN: RateLimitPartition.GetNoLimiter trong IsExemptFromGlobalRateLimit
        // (Program.cs). Đã kiểm ngược: bỏ nhánh "/hangfire" khỏi hàm đó → test này ĐỎ.
        //
        // Khẳng định là "KHÁC 429" chứ không phải một mã cụ thể: user của test không có role
        // SuperAdmin nên HangfireDashboardAuthFilter sẽ từ chối (401/403 tuỳ phiên bản Hangfire) —
        // thứ đang kiểm là request có ĐẾN được nhánh Dashboard hay bị rate limiter chặn từ trước.
        var dashboardResponse = await client.GetAsync("/hangfire");
        Assert.True(
            dashboardResponse.StatusCode != HttpStatusCode.TooManyRequests,
            "/hangfire bị 429 — nhánh miễn trừ theo đường dẫn trong IsExemptFromGlobalRateLimit (Program.cs) đã mất. " +
            "Dashboard tự poll /hangfire/stats ~2 giây/lần nên nó sẽ ngốn hết hạn mức 100/phút dùng chung của IP đó " +
            "và đá admin ra khỏi app.");
    }

    // ── Hạ tầng test ─────────────────────────────────────────────────────────

    /// <summary>CSRF Lớp 2 áp cho MỌI method ghi kể cả login — xem
    /// CsrfTestClientExtensions.WithCsrfTokenAsync. GET /api/antiforgery/token TỰ nó được miễn
    /// GlobalLimiter (Program.cs) nên KHÔNG làm lệch phép đếm "GlobalPermitLimit - 1" của các
    /// test dưới (vẫn đúng CHỈ POST /api/auth/login tiêu 1 slot global trước vòng lặp).</summary>
    private async Task<HttpClient> CreateClientAsync(string clientIp)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            // https BẮT BUỘC: cookie phiên khai CookieSecurePolicy.Always (Program.cs) nên
            // CookieContainer sẽ KHÔNG gửi lại nó qua http ⇒ mọi request sau login thành 401 và
            // test đỏ vì lý do hoàn toàn khác thứ đang kiểm.
            BaseAddress = new Uri("https://localhost"),
        });

        // Đặt ở DefaultRequestHeaders (không phải từng request) để MỌI request của client này —
        // gồm cả lượt login — rơi vào CÙNG một phân vùng IP. Test đang đếm hạn mức của 1 IP nên
        // lệch phân vùng giữa các lượt là hỏng phép đếm.
        client.DefaultRequestHeaders.Add(RateLimitPartitionFactory.ClientIpHeader, clientIp);
        return await client.WithCsrfTokenAsync();
    }

    private async Task<HttpClient> LoginAsync(string clientIp)
    {
        var userName = $"it-glrl-{Guid.NewGuid():N}"[..24];
        await CreateUserAsync(userName);

        var client = await CreateClientAsync(clientIp);
        var response = await client.PostAsJsonAsync("/api/auth/login", new { userName, password = Password });

        if (response.StatusCode != HttpStatusCode.OK)
        {
            Assert.Fail(
                $"Đăng nhập thất bại ({(int)response.StatusCode}) — test chưa chạy tới phần rate limit. " +
                $"Body: {await response.Content.ReadAsStringAsync()}");
        }

        return client;
    }

    /// <summary>User KHÔNG gắn role nào — <c>GET /api/auth/me</c> chỉ cần <c>[Authorize]</c>
    /// (không <c>[RequirePermission]</c>), nên test không phải phụ thuộc vào việc seed role đã
    /// chạy xong hay chưa.</summary>
    private async Task CreateUserAsync(string userName)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = userName,
            Email = $"{userName}@it.local",
            FullName = userName,
            MustChangePassword = false,
            DateCreate = DateTimeOffset.UtcNow,
        };

        var created = await userManager.CreateAsync(user, Password);
        Assert.True(created.Succeeded, string.Join("; ", created.Errors.Select(e => e.Description)));
    }

    /// <summary>Đăng nhập bằng tài khoản KHÔNG tồn tại — không kích hoạt <c>lockoutOnFailure</c>
    /// lên bất kỳ tài khoản thật nào trong database dùng chung (Identity không tìm thấy user thì
    /// không có gì để đếm sai). Cùng lý do với <c>LoginRateLimitPartitionTests</c>.</summary>
    private static Task<HttpResponseMessage> AttemptLoginAsync(HttpClient client)
        => client.PostAsJsonAsync("/api/auth/login", new
        {
            userName = $"it-nobody-{Guid.NewGuid():N}"[..24],
            password = "SaiMatKhau@123",
        });
}

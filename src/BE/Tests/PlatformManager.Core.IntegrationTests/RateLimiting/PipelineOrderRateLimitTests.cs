using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace PlatformManager.Core.IntegrationTests.RateLimiting;

/// <summary>
/// Hai khẳng định phụ thuộc THỨ TỰ MIDDLEWARE của <c>app.UseRateLimiter()</c> trong
/// <c>Program.cs</c> — vị trí đúng là <b>sau <c>UseCors</c>, trước <c>UseAuthentication</c></b>.
///
/// <para><b>Lỗ hổng được chốt lại ở đây (đo thật 2026-08-21, sửa cùng ngày).</b> Thứ tự cũ là
/// <c>UseAuthentication → UseAuthorization → UseRateLimiter</c>. <c>UseRouting</c> chạy ở đầu
/// pipeline (WebApplication tự chèn) nên metadata <c>[Authorize]</c> đã sẵn sàng khi
/// <c>UseAuthorization</c> chạy ⇒ request CHƯA ĐĂNG NHẬP bị **cắt mạch bằng 401 TRƯỚC KHI** tới
/// rate limiter. Số đo trước bản sửa: 150 request vô danh liên tiếp tới <c>GET /api/auth/me</c>
/// trả 401 toàn bộ, <b>0 lượt 429</b>, và không tiêu slot nào của hạn mức 100. Tức
/// <c>GlobalLimiter</c> khi ấy chỉ bảo vệ lưu lượng ĐÃ ĐĂNG NHẬP — đúng phần lưu lượng ít cần
/// bảo vệ nhất trước flood.</para>
///
/// <para>Hai test dưới đây đi CẶP, đo hai chiều ngược nhau của cùng một quyết định:</para>
/// <list type="bullet">
/// <item><c>AnonymousRequest…IsThrottled</c> — chiều THUẬN: lưu lượng vô danh nay CÓ bị siết.
/// Đây là test đã được kiểm ngược: chạy trên <c>Program.cs</c> CŨ nó ĐỎ (lượt thứ 101 trả 401
/// thay vì 429).</item>
/// <item><c>PreflightOptions…DoNotConsumeQuota</c> — chiều NGƯỢC: đưa rate limiter lên sớm mà
/// không miễn <c>OPTIONS</c> thì hạn mức thực tế của FE bị **chia đôi** (mỗi request thật kèm 1
/// preflight). Miễn trừ này nằm trong hàm phân vùng của <c>GlobalLimiter</c>
/// (<c>RateLimitPartition.GetNoLimiter</c>), không phải ở thứ tự middleware.</item>
/// </list>
///
/// <para>⚠️ Vì sao KHÔNG đặt <c>UseRateLimiter</c> lên trước <c>UseCors</c> luôn cho "sớm nhất
/// có thể": response 429 khi đó mất header CORS, trình duyệt hiện cho người dùng một lỗi CORS mờ
/// mịt thay vì 429 đọc được — toàn bộ envelope 429 (xem <c>GlobalRateLimitTests</c>) trở thành vô
/// dụng phía FE. Vị trí hiện tại là điểm sớm nhất còn giữ được header CORS.</para>
///
/// Mỗi test method có host RIÊNG (xUnit dựng instance test class mới cho mỗi method) — BẮT BUỘC:
/// bộ đếm cửa sổ 1 phút sống trong DI container của host, host dùng chung sẽ khiến test chạy sau
/// thừa hưởng bộ đếm đã cạn của test chạy trước.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class PipelineOrderRateLimitTests : IAsyncLifetime
{
    /// <summary>Khớp <c>GlobalPermitLimitPerMinute = 100</c> trong <c>Program.cs</c>. Đổi con số
    /// ở đó thì phải đổi ở đây — CỐ Ý không đọc động từ cấu hình, vì test phải phát hiện được
    /// việc hạn mức bị nới ra một cách âm thầm.</summary>
    private const int GlobalPermitLimit = 100;

    /// <summary>Phải là origin CÓ trong <c>Cors:AllowedOrigins</c> của
    /// <c>appsettings.Development.json</c> — preflight từ origin lạ đi theo nhánh khác, không phải
    /// thứ đang muốn đo.</summary>
    private const string AllowedOrigin = "http://localhost:4200";

    private readonly RateLimitPartitionFactory _factory;

    public PipelineOrderRateLimitTests(PostgresFixture fixture)
    {
        // Cùng lý do với GlobalRateLimitTests/SessionTerminationTests: PHẢI đặt bằng biến môi
        // trường TRƯỚC khi host boot — Program.cs "đông cứng" ConnectionStrings:Default vào
        // closure của AddHangfire.
        IntegrationTestHostEnvironment.Configure(fixture.ConnectionString);

        _factory = new RateLimitPartitionFactory();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    // ── Kịch bản ─────────────────────────────────────────────────────────────

    [Fact(DisplayName = "Request VÔ DANH tới endpoint [Authorize] vẫn tiêu hạn mức và bị 429 (không chỉ 401)")]
    public async Task AnonymousRequest_ToAuthorizedEndpoint_IsThrottled()
    {
        // KHÔNG đăng nhập — đây chính là điểm khác biệt với GlobalRateLimitTests (test ở đó đăng
        // nhập trước, nên nó không hề chạm tới lỗ hổng này).
        var client = CreateClient("203.0.113.70");

        // Đúng GlobalPermitLimit lượt đầu được đi tiếp và bị chặn ở tầng auth ⇒ 401.
        // Khẳng định "== 401" (chứ không phải "!= 429") có sức nặng riêng: nó chứng minh việc đưa
        // rate limiter lên trước UseAuthentication KHÔNG làm hỏng đường trả 401 JSON của
        // ConfigureApplicationCookie — request vẫn đi xuyên qua limiter rồi mới bị auth từ chối.
        for (var i = 1; i <= GlobalPermitLimit; i++)
        {
            var response = await client.GetAsync("/api/auth/me");
            Assert.True(
                response.StatusCode == HttpStatusCode.Unauthorized,
                $"Lượt {i}/{GlobalPermitLimit} trả {(int)response.StatusCode} thay vì 401 — hạn mức global đang CHẶT hơn khai báo, hoặc /api/auth/me đã hết [Authorize].");
        }

        // ĐÂY là khẳng định trọng tâm. Trên Program.cs CŨ lượt này trả 401 (test ĐỎ): flood vô
        // danh không tốn slot nào nên hạn mức không bao giờ cạn.
        var throttled = await client.GetAsync("/api/auth/me");
        Assert.True(
            throttled.StatusCode == HttpStatusCode.TooManyRequests,
            $"Lượt thứ {GlobalPermitLimit + 1} của một IP VÔ DANH trả {(int)throttled.StatusCode} thay vì 429 — " +
            "app.UseRateLimiter() đã tuột xuống SAU app.UseAuthorization(), nên UseAuthorization cắt mạch bằng 401 " +
            "trước khi request tới rate limiter và GlobalLimiter chỉ còn bảo vệ lưu lượng ĐÃ ĐĂNG NHẬP.");
    }

    [Fact(DisplayName = "Preflight OPTIONS KHÔNG tiêu hạn mức — hạn mức thật của FE không bị chia đôi")]
    public async Task PreflightOptions_DoNotConsumeGlobalQuota()
    {
        var client = CreateClient("203.0.113.80");

        // Gửi NHIỀU HƠN hạn mức để nếu OPTIONS có bị đếm thì chính vòng lặp này đã 429.
        var burst = GlobalPermitLimit + 10;

        // 1. Preflight THẬT (Origin + Access-Control-Request-Method) — đúng thứ trình duyệt gửi
        //    trước mỗi request cross-origin. CORS middleware trả 204 và cắt mạch, nên ca này đo
        //    thêm một điều: UseRateLimiter vẫn nằm SAU UseCors.
        for (var i = 1; i <= burst; i++)
        {
            var response = await SendPreflightAsync(client, "/api/auth/me");
            Assert.True(
                response.StatusCode != HttpStatusCode.TooManyRequests,
                $"Preflight thứ {i}/{burst} bị 429 — mỗi request thật của FE kèm 1 preflight, nên đếm preflight là chia đôi hạn mức thực tế.");
        }

        // 2. OPTIONS TRẦN (không Origin) — KHÔNG phải preflight nên CORS middleware cho đi tiếp
        //    ⇒ nó ĐI TỚI rate limiter thật. Đây là ca chứng minh nhánh
        //    RateLimitPartition.GetNoLimiter cho HttpMethods.IsOptions có tác dụng; nếu chỉ test
        //    bằng preflight thật thì test vẫn xanh kể cả khi nhánh đó bị xoá.
        for (var i = 1; i <= burst; i++)
        {
            var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Options, "/api/auth/me"));
            Assert.True(
                response.StatusCode != HttpStatusCode.TooManyRequests,
                $"OPTIONS trần thứ {i}/{burst} bị 429 — nhánh miễn OPTIONS trong hàm phân vùng của GlobalLimiter đã mất.");
        }

        // 3. Khẳng định trọng tâm: sau {burst * 2} lượt OPTIONS, hạn mức của CHÍNH IP đó vẫn còn
        //    NGUYÊN — request nghiệp vụ đầu tiên vẫn đi xuyên qua limiter (401 vì chưa đăng nhập,
        //    KHÔNG phải 429). Đây mới là thứ chứng minh "không tiêu hạn mức", chứ không chỉ
        //    "bản thân OPTIONS không bị chặn".
        var real = await client.GetAsync("/api/auth/me");
        Assert.True(
            real.StatusCode == HttpStatusCode.Unauthorized,
            $"Request nghiệp vụ đầu tiên sau {burst * 2} lượt OPTIONS trả {(int)real.StatusCode} thay vì 401 — " +
            "OPTIONS đã ăn vào hạn mức 100/phút của IP đó.");
    }

    // ── Hạ tầng test ─────────────────────────────────────────────────────────

    private HttpClient CreateClient(string clientIp)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            // https cho khớp CookieSecurePolicy.Always của Program.cs — giữ cùng quy ước với
            // GlobalRateLimitTests để không tạo hai kiểu dựng client trong cùng bộ test.
            BaseAddress = new Uri("https://localhost"),
        });

        // Đặt ở DefaultRequestHeaders (không phải từng request) để MỌI request của client này rơi
        // vào CÙNG một phân vùng IP — test đang đếm hạn mức của 1 IP.
        client.DefaultRequestHeaders.Add(RateLimitPartitionFactory.ClientIpHeader, clientIp);
        return client;
    }

    private static Task<HttpResponseMessage> SendPreflightAsync(HttpClient client, string path)
    {
        var request = new HttpRequestMessage(HttpMethod.Options, path);
        request.Headers.Add("Origin", AllowedOrigin);
        request.Headers.Add("Access-Control-Request-Method", "GET");
        return client.SendAsync(request);
    }
}

using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace PlatformManager.Core.IntegrationTests.RateLimiting;

/// <summary>
/// Rate limit của <c>POST /api/auth/login</c> phải tính theo TỪNG IP, không phải một bộ đếm
/// dùng chung cho cả ứng dụng.
///
/// Lỗi được chốt lại ở đây (sửa 2026-08-21): <c>Program.cs</c> từng dùng
/// <c>options.AddFixedWindowLimiter("login", opt => …)</c> — overload đó tạo ĐÚNG MỘT limiter
/// cho toàn app. Hậu quả thật: (1) cả công ty chỉ có 5 lượt đăng nhập/phút cộng dồn; (2) một
/// người lạ KHÔNG cần tài khoản có thể khoá đăng nhập của mọi người bằng 5 request/phút. Điểm
/// (2) đặc biệt quan trọng vì quyết định "miễn lockout cho SuperAdmin" dựa trên tiền đề
/// "đã có rate limit theo IP làm hàng rào" — tiền đề đó khi ấy SAI.
///
/// Hai test dưới đây đi CẶP và cần cả hai:
/// <list type="bullet">
/// <item><c>SameIp…</c> chứng minh limiter thật sự đang chạy — thiếu nó thì test kia vẫn xanh
/// ngay cả khi ai đó xoá sạch rate limit.</item>
/// <item><c>DifferentIps…</c> chứng minh 2 IP không dùng chung hạn mức — thiếu nó thì bản sửa
/// không chứng minh được gì.</item>
/// </list>
///
/// Mỗi test method có host RIÊNG (xUnit dựng instance test class mới cho mỗi method) — BẮT BUỘC:
/// bộ đếm cửa sổ 1 phút sống trong DI container của host, host dùng chung sẽ khiến test chạy sau
/// thừa hưởng bộ đếm đã cạn của test chạy trước.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class LoginRateLimitPartitionTests : IAsyncLifetime
{
    /// <summary>Khớp <c>PermitLimit = 5</c> của policy "login" trong <c>Program.cs</c>. Đổi con
    /// số ở đó thì phải đổi ở đây — cố ý KHÔNG đọc động từ cấu hình, vì test phải phát hiện được
    /// việc hạn mức bị nới ra một cách âm thầm.</summary>
    private const int LoginPermitLimit = 5;

    private const string IpA = "203.0.113.10";
    private const string IpB = "203.0.113.20";

    private readonly RateLimitPartitionFactory _factory;

    public LoginRateLimitPartitionTests(PostgresFixture fixture)
    {
        // Cùng lý do với SessionTerminationTests: PHẢI đặt bằng biến môi trường TRƯỚC khi host
        // boot — Program.cs "đông cứng" ConnectionStrings:Default vào closure của AddHangfire.
        IntegrationTestHostEnvironment.Configure(fixture.ConnectionString);

        _factory = new RateLimitPartitionFactory();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    [Fact(DisplayName = "Cùng 1 IP vượt PermitLimit → request kế tiếp bị 429 (limiter thật sự đang chạy)")]
    public async Task SameIp_ExceedingPermitLimit_IsThrottled()
    {
        var client = await CreateClientAsync();

        for (var attempt = 1; attempt <= LoginPermitLimit; attempt++)
        {
            var status = await AttemptLoginAsync(client, IpA);
            Assert.True(
                status != HttpStatusCode.TooManyRequests,
                $"Lượt {attempt}/{LoginPermitLimit} đã bị 429 — hạn mức đang CHẶT hơn khai báo, hoặc bộ đếm bị dùng chung với test khác.");
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, await AttemptLoginAsync(client, IpA));
    }

    [Fact(DisplayName = "IP A cạn hạn mức → IP B VẪN đăng nhập được (2 IP không dùng chung bộ đếm)")]
    public async Task DifferentIps_DoNotShareTheSameQuota()
    {
        var client = await CreateClientAsync();

        // 1. Vắt cạn hạn mức của IP A và chứng minh nó ĐÃ cạn thật.
        for (var attempt = 1; attempt <= LoginPermitLimit; attempt++)
            await AttemptLoginAsync(client, IpA);

        Assert.Equal(HttpStatusCode.TooManyRequests, await AttemptLoginAsync(client, IpA));

        // 2. ĐÂY là khẳng định trọng tâm. Khẳng định "khác 429" là chưa đủ — đòi đúng 422
        //    (AUTH.INVALID_CREDENTIALS) chứng minh request của IP B đi XUYÊN QUA rate limiter và
        //    chạm tới handler thật, chứ không phải bị chặn ở một tầng khác rồi trả mã khác.
        Assert.Equal(HttpStatusCode.UnprocessableEntity, await AttemptLoginAsync(client, IpB));

        // 3. Bộ đếm của IP A không bị request của IP B "làm mới" — IP A vẫn phải đang bị chặn.
        Assert.Equal(HttpStatusCode.TooManyRequests, await AttemptLoginAsync(client, IpA));
    }

    // ── Hạ tầng test ─────────────────────────────────────────────────────────

    /// <summary>CSRF Lớp 2 áp cho MỌI method ghi kể cả login (dù ở đây login luôn thất bại do
    /// tài khoản không tồn tại) — xem CsrfTestClientExtensions.WithCsrfTokenAsync. GET
    /// /api/antiforgery/token được miễn CẢ GlobalLimiter LẪN policy "login" (chỉ POST
    /// /api/auth/login mới tính vào policy "login") nên KHÔNG làm lệch phép đếm
    /// <c>LoginPermitLimit</c> của các test dưới.</summary>
    private async Task<HttpClient> CreateClientAsync()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            // https cho khớp CookieSecurePolicy.Always của Program.cs — ở đây login luôn thất bại
            // nên chưa có cookie, nhưng giữ cùng quy ước với SessionTerminationTests để không tạo
            // hai kiểu dựng client khác nhau trong cùng bộ test.
            BaseAddress = new Uri("https://localhost"),
        });
        return await client.WithCsrfTokenAsync();
    }

    /// <summary>Đăng nhập bằng tài khoản KHÔNG tồn tại: luôn trả 422 AUTH.INVALID_CREDENTIALS khi
    /// không bị chặn, và quan trọng hơn là KHÔNG kích hoạt <c>lockoutOnFailure</c> lên bất kỳ
    /// tài khoản thật nào (Identity không tìm thấy user thì không có gì để đếm sai) — test này
    /// bắn hàng chục lần đăng nhập sai nên không được để lại tác dụng phụ cho test khác trong
    /// cùng database dùng chung.</summary>
    private static async Task<HttpStatusCode> AttemptLoginAsync(HttpClient client, string clientIp)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = JsonContent.Create(new
            {
                userName = $"it-nobody-{Guid.NewGuid():N}"[..24],
                password = "SaiMatKhau@123",
            }),
        };
        request.Headers.Add(RateLimitPartitionFactory.ClientIpHeader, clientIp);

        var response = await client.SendAsync(request);
        return response.StatusCode;
    }
}

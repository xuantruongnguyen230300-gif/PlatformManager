using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using PlatformManager.Core.Application.Common;
using PlatformManager.Core.Application.Common.Results;
using Xunit;

namespace PlatformManager.Core.IntegrationTests.Common;

/// <summary>
/// SEAM ACTIVATION TEST cho <c>ApiStatusCodeEnvelopeMiddleware</c> — hai mã mà hạ tầng ĐỊNH TUYẾN
/// tự sinh với thân rỗng (<b>404</b> không khớp route, <b>405</b> sai verb) phải rời khỏi BE dưới
/// dạng envelope <c>IApiResult&lt;T&gt;</c>, đúng lời hứa ở doc/contracts/auth.md §Envelope chung.
///
/// <para><b>Đo thật trước khi sửa (2026-09-04):</b>
/// <c>curl -sk -D - https://localhost:7168/api/khong-he-co</c> → <c>404</c>,
/// <c>Content-Length: 0</c>, thân rỗng; route đúng + sai verb → <c>405</c> cũng rỗng.</para>
///
/// <para><b>Bốn ca, và cả bốn đều cần.</b> Hai ca đầu là chiều THUẬN (đường mới có tác dụng). Hai
/// ca sau là chiều NGƯỢC — chúng canh đúng hai cách mà bản sửa này có thể gây hại, và cả hai đều
/// là hỏng IM LẶNG nếu không đo: nuốt 404 của nhánh ngoài API, và ghi đè 404 mà handler CHỦ ĐỘNG
/// trả (biến một mã nghiệp vụ đúng thành một mã định tuyến sai). Chỉ có chiều thuận thì một
/// middleware bắt-tất-cả vẫn xanh trọn vẹn.</para>
///
/// <para>KHÔNG có ca cho <c>/hangfire</c>: Dashboard đòi role <c>SuperAdmin</c> nên request vô danh
/// nhận 401 (đã mang envelope riêng từ <c>OnRedirectToLogin</c>) chứ không bao giờ tới 404 — đo ở
/// đó sẽ là một test luôn xanh vì lý do khác với lý do nó được viết ra. Nhánh <c>/health</c> ở ca 3
/// kiểm đúng cùng một luật (allowlist theo tiền tố <c>/api</c>) bằng một đường thật sự chạm tới
/// 404.</para>
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class RoutingEnvelopeTests : IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;

    public RoutingEnvelopeTests(PostgresFixture fixture)
    {
        // PHẢI đặt trước khi host boot — xem IntegrationTestHostEnvironment.
        IntegrationTestHostEnvironment.Configure(fixture.ConnectionString);

        _factory = new WebApplicationFactory<Program>();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    [Fact(DisplayName = "404 không khớp route dưới /api → envelope ROUTE.NOT_FOUND (trước 2026-09-04: thân rỗng)")]
    public async Task UnmatchedApiRoute_Returns_AnEnvelope()
    {
        var response = await CreateClient().GetAsync("/api/khong-he-co");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var envelope = await AdminApiTestClient.ReadEnvelopeAsync(response);

        Assert.Equal("BUSINESS_ERROR", envelope.Status);
        Assert.Equal("NotFound", envelope.Code);

        // Khẳng định GIÁ TRỊ của businessCode chứ không chỉ "có trường": nó là KHOÁ DỊCH ở bước
        // i18n và là thứ DUY NHẤT phân biệt ca này với USER.NOT_FOUND (ca 4) — hai ca cùng HTTP
        // 404 và cùng code "NotFound".
        Assert.Equal(InfrastructureErrors.RouteNotFound.BusinessCode, envelope.BusinessCode);
        Assert.Equal(InfrastructureErrors.RouteNotFound.MessageTemplate, envelope.Message);
    }

    [Fact(DisplayName = "405 sai verb dưới /api → envelope ROUTE.METHOD_NOT_ALLOWED, header Allow còn nguyên")]
    public async Task WrongVerbOnRealRoute_Returns_AnEnvelope()
    {
        // /api/auth/login CHỈ khai [HttpPost]. Cố ý chọn một verb ĐỌC (GET): verb ghi sẽ bị
        // middleware CSRF chặn 403 trước khi chạm tới nhánh 405, và ca đo được sẽ không còn là ca
        // định đo.
        var response = await CreateClient().GetAsync("/api/auth/login");

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);

        // Header Allow do hạ tầng định tuyến đặt — envelope chỉ THÊM thân, không được thay thế nó.
        // Mất header này là mất câu trả lời cho "vậy phải gọi bằng verb nào".
        Assert.Contains("POST", response.Content.Headers.Allow);

        var envelope = await AdminApiTestClient.ReadEnvelopeAsync(response);

        Assert.Equal("BUSINESS_ERROR", envelope.Status);
        Assert.Equal("MethodNotAllowed", envelope.Code);
        Assert.Equal(InfrastructureErrors.MethodNotAllowed.BusinessCode, envelope.BusinessCode);
    }

    [Fact(DisplayName = "404 NGOÀI /api giữ nguyên thân rỗng — middleware không nuốt nhánh khác")]
    public async Task UnmatchedRouteOutsideApi_IsLeftAlone()
    {
        // /health có endpoint, /health/<gì đó> thì không → 404 do cùng hạ tầng định tuyến sinh ra,
        // khác ca 1 ĐÚNG một điểm: tiền tố đường dẫn. Nếu allowlist "/api" bị bỏ hoặc viết sai,
        // đây là ca đỏ.
        var response = await CreateClient().GetAsync("/health/khong-he-co");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
    }

    [Fact(DisplayName = "404 do handler CHỦ ĐỘNG trả vẫn giữ USER.NOT_FOUND, không bị ghi đè")]
    public async Task HandlerAuthored404_KeepsItsBusinessCode()
    {
        var client = await AdminApiTestClient.CreateLoggedInClientAsync(_factory, "route-404", Roles.Admin);

        // LockUserCommand trả UserErrors.NotFound (404) cho id không tồn tại. Đây là đường mà bản
        // sửa này DỄ phá nhất: cùng HTTP 404, cùng đi tới cuối pipeline. Nó không bị chạm vì đã có
        // Content-Type + thân — nhưng "đã có thân" là một giả định về MVC, và giả định thì phải đo.
        var response = await client.PostAsync($"/api/users/{Guid.NewGuid()}/lock", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var envelope = await AdminApiTestClient.ReadEnvelopeAsync(response);

        Assert.Equal("USER.NOT_FOUND", envelope.BusinessCode);
        Assert.NotEqual(InfrastructureErrors.RouteNotFound.BusinessCode, envelope.BusinessCode);
    }

    private HttpClient CreateClient() => _factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        // https BẮT BUỘC — cookie phiên/CSRF khai CookieSecurePolicy.Always (Program.cs).
        BaseAddress = new Uri("https://localhost"),
    });
}

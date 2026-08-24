namespace PlatformManager.Core.IntegrationTests;

/// <summary>
/// Mô phỏng đúng thứ Angular <c>HttpClient</c> làm TỰ ĐỘNG cho mọi request thật: đọc cookie
/// <c>XSRF-TOKEN</c> rồi echo lại vào header <c>X-XSRF-TOKEN</c>. <c>HttpClient</c>/
/// <c>CookieContainer</c> thuần .NET không tự làm việc đó — đó là hành vi JavaScript-side của
/// trình duyệt/Angular, nên test phải giả lập tay.
///
/// Kể từ khi CSRF Lớp 2 được wire (<c>Program.cs</c>, xem
/// doc/huong_dan/wiki-core/be/02-identity-auth.md §CSRF), MỌI POST/PUT/PATCH/DELETE qua
/// <c>WebApplicationFactory</c> mà thiếu bước này sẽ nhận <b>403</b>
/// (<c>AntiforgeryValidationException</c> → <c>GlobalExceptionHandler</c>) — kể cả
/// <c>POST /api/auth/login</c>, vì CSRF áp theo METHOD, không loại trừ theo endpoint.
///
/// <para>⚠️ <b>SỬA 2026-08-24 (core-reviewer phát hiện):</b> bản trước đọc token từ BODY JSON
/// (<c>{ "token": "..." }</c>) của <c>GET /api/antiforgery/token</c> — SAI so với hành vi
/// Angular thật (<c>HttpXsrfInterceptor</c> chỉ đọc <c>document.cookie</c>, KHÔNG bao giờ đọc
/// body JSON). Vì Antiforgery trả về CẢ HAI giá trị giống hệt lúc đó (body và cookie cùng chứa
/// request-token, do lỗi <c>Program.cs</c> đặt sai tên cookie khiến cookie-token bị ghi nhầm vào
/// đúng tên "XSRF-TOKEN" — xem sửa ở <c>Program.cs</c>), việc đọc "sai chỗ nhưng vẫn ra đúng giá
/// trị" khiến bộ test XANH suốt trong khi hành vi Angular thật (đọc cookie) sẽ nhận 403. Bài học:
/// seam activation test phải mô phỏng ĐÚNG cách CLIENT THẬT lấy giá trị, không phải cách nào tiện
/// miễn còn xanh — nay đọc thẳng <c>Set-Cookie: XSRF-TOKEN=...</c> từ response, đúng những gì
/// trình duyệt/Angular thấy.</para>
///
/// Cookie <c>XSRF-TOKEN</c> tự động đi kèm các request SAU nhờ
/// <c>WebApplicationFactoryClientOptions.HandleCookies</c> mặc định <c>true</c> (client giữ
/// <c>CookieContainer</c> riêng, dùng lại cho mọi request tiếp theo của CHÍNH client đó).
/// </summary>
internal static class CsrfTestClientExtensions
{
    private const string CookieName = "XSRF-TOKEN";
    private const string HeaderName = "X-XSRF-TOKEN";

    /// <summary>
    /// Gọi <c>GET /api/antiforgery/token</c>, đọc token TỪ COOKIE thật (<c>Set-Cookie:
    /// XSRF-TOKEN=...</c>) rồi gắn header <c>X-XSRF-TOKEN</c> cho CHÍNH client này — gọi trước
    /// bất kỳ POST/PUT/PATCH/DELETE nào, kể cả trước lượt đăng nhập đầu tiên. Trả lại chính
    /// <paramref name="client"/> để dùng được theo kiểu fluent ngay tại nơi tạo client.
    ///
    /// <para>⚠️ <b>BẮT BUỘC gọi lại lần nữa NGAY SAU khi login thành công</b> — token phát lúc
    /// ANONYMOUS bị <c>DefaultAntiforgery</c> gắn với danh tính lúc phát hành ("meant for a
    /// different claims-based user"); dùng nguyên token đó cho request GHI sau khi đã đăng nhập
    /// sẽ bị từ chối 403, dù mọi thứ khác đều đúng. Idempotent (remove-rồi-add) nên gọi lại an
    /// toàn ở bất kỳ thời điểm nào danh tính vừa đổi.</para>
    /// </summary>
    public static async Task<HttpClient> WithCsrfTokenAsync(this HttpClient client)
    {
        var response = await client.GetAsync("/api/antiforgery/token");
        response.EnsureSuccessStatusCode();

        if (!response.Headers.TryGetValues("Set-Cookie", out var setCookieHeaders))
        {
            throw new InvalidOperationException(
                $"GET /api/antiforgery/token không set cookie '{CookieName}' nào (thiếu Set-Cookie).");
        }

        var xsrfCookie = setCookieHeaders.FirstOrDefault(
            h => h.StartsWith($"{CookieName}=", StringComparison.Ordinal))
            ?? throw new InvalidOperationException(
                $"Thiếu Set-Cookie: {CookieName} trong response GET /api/antiforgery/token.");

        // "XSRF-TOKEN=<value>; path=/; samesite=none; secure" — chỉ cần phần <value> trước dấu ';'
        // đầu tiên. ASP.NET Core percent-encode giá trị cookie (Uri.EscapeDataString) lúc
        // Response.Cookies.Append — Angular HttpXsrfCookieExtractor tự decodeURIComponent() khi
        // đọc document.cookie, nên test phải làm đúng bước đó để ra đúng giá trị gốc
        // (tokens.RequestToken) thay vì chuỗi đã encode.
        var rawValue = xsrfCookie[(CookieName.Length + 1)..].Split(';')[0];
        var token = Uri.UnescapeDataString(rawValue);

        client.DefaultRequestHeaders.Remove(HeaderName);
        client.DefaultRequestHeaders.Add(HeaderName, token);
        return client;
    }
}

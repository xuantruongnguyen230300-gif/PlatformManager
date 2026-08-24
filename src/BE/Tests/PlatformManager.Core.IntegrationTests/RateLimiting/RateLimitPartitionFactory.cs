using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace PlatformManager.Core.IntegrationTests.RateLimiting;

/// <summary>
/// Dựng ĐÚNG host <c>PlatformManager.Api</c> thật và cho phép test tự chọn IP người gọi.
///
/// Vì sao cần: thứ đang được kiểm chứng là <c>ResolveRateLimitPartitionKey</c> trong
/// <c>Program.cs</c> — nó đọc <c>HttpContext.Connection.RemoteIpAddress</c>. Nhưng
/// <c>TestServer</c> KHÔNG có kết nối TCP thật nên <c>RemoteIpAddress</c> luôn <c>null</c>;
/// mọi request của mọi test sẽ rơi vào cùng khoá dự phòng <c>"unknown-ip"</c> và không cách nào
/// phân biệt được "2 IP khác nhau" — tức không chứng minh được gì.
///
/// Cách làm: chèn 1 middleware ở ĐẦU pipeline (qua <see cref="IStartupFilter"/> — chạy TRƯỚC
/// toàn bộ middleware khai trong <c>Program.cs</c>, quan trọng nhất là trước
/// <c>app.UseRateLimiter()</c>) gán <c>RemoteIpAddress</c> theo header
/// <see cref="ClientIpHeader"/>. Rate limiter THẬT của host thật vẫn chạy nguyên vẹn — không
/// mock, không thay policy, không đổi con số. Test chỉ giả lập đúng một thứ mà TestServer không
/// có: địa chỉ IP của kết nối.
/// </summary>
internal sealed class RateLimitPartitionFactory : WebApplicationFactory<Program>
{
    /// <summary>Header CHỈ tồn tại trong test — production không đọc header nào để phân vùng
    /// (cố ý: đọc header do client gửi = cho client tự chọn phân vùng, xem cảnh báo
    /// ForwardedHeaders trong <c>Program.cs</c>).</summary>
    public const string ClientIpHeader = "X-Test-Client-Ip";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
        => builder.ConfigureTestServices(services =>
            services.AddSingleton<IStartupFilter, TestClientIpStartupFilter>());

    private sealed class TestClientIpStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use(async (context, nextMiddleware) =>
            {
                if (context.Request.Headers.TryGetValue(ClientIpHeader, out var raw)
                    && IPAddress.TryParse(raw.ToString(), out var clientIp))
                {
                    context.Connection.RemoteIpAddress = clientIp;
                }

                await nextMiddleware();
            });

            next(app);
        };
    }
}

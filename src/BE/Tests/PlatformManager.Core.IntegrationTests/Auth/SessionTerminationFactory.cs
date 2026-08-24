using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace PlatformManager.Core.IntegrationTests.Auth;

/// <summary>
/// Dựng ĐÚNG host <c>PlatformManager.Api</c> thật (không phải host mô phỏng) — bắt buộc, vì thứ
/// đang được kiểm chứng (<c>SecurityStampValidator</c> chấm dứt phiên) sống ở tầng cookie
/// middleware do <c>AddIdentity()</c> nối vào, không ở tầng handler. Test dựng host riêng sẽ
/// không phát hiện được nếu ai đó đổi <c>AddIdentity</c> → <c>AddIdentityCore</c> (validator
/// không còn được nối, cơ chế chết ÂM THẦM — xem
/// doc/huong_dan/wiki-core/be/02-identity-auth.md §"Cơ chế này CHẾT ÂM THẦM").
///
/// Thay đổi DUY NHẤT so với production: ép <c>ValidationInterval = TimeSpan.Zero</c> để validator
/// chạy MỌI request thay vì mỗi 30 phút. Đây là ngoại lệ hợp lệ DUY NHẤT của <c>Zero</c> (xem
/// §"Chính sách đã CHỐT" cùng file) — trong code sản phẩm KHÔNG được cấu hình
/// <c>ValidationInterval</c>, giữ nguyên mặc định 30 phút của Identity.
/// </summary>
internal sealed class SessionTerminationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // ConfigureTestServices chạy SAU toàn bộ đăng ký của Program.cs → Configure<T> ở đây là
        // lớp cuối cùng, đè giá trị mặc định mà không phải gỡ đăng ký nào.
        builder.ConfigureTestServices(services =>
            services.Configure<SecurityStampValidatorOptions>(options => options.ValidationInterval = TimeSpan.Zero));
    }
}

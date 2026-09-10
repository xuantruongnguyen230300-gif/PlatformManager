using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PlatformManager.Api.Common;
using PlatformManager.Core.Application.Permissions;
using PlatformManager.Core.Infrastructure.Storage;

namespace PlatformManager.Core.IntegrationTests.Production;

/// <summary>
/// Dựng ĐÚNG host <c>PlatformManager.Api</c> thật ở cấu hình <b><c>Production</c></b> — nhóm test
/// duy nhất trong repo làm việc đó (chốt 2026-08-31, xem
/// doc/huong_dan/wiki-core/be/04-testing-strategy.md §"Quyết định người dùng 2026-08-31").
///
/// <para><b>Vì sao KHÔNG đặt biến môi trường <c>ASPNETCORE_ENVIRONMENT=Production</c>:</b> biến môi
/// trường là trạng thái của CẢ TIẾN TRÌNH. <see cref="IntegrationTestHostEnvironment"/> đặt biến đó
/// thành <c>Development</c> cho mọi test còn lại; ghi đè rồi khôi phục là một cửa sổ rò rỉ — chỉ cần
/// một assert ném giữa chừng (hoặc một host khác boot chậm) là test khác thừa hưởng
/// <c>Production</c> và đỏ vì lý do chẳng liên quan gì tới thứ nó kiểm. Ở đây môi trường được truyền
/// qua <c>IWebHostBuilder.UseEnvironment</c>, tức phạm vi ĐÚNG BẰNG một host — không có gì để khôi
/// phục, và không có gì rò rỉ.</para>
///
/// <para><b>Vì sao <c>UseSetting</c> đủ sớm trong khi <c>ConfigureAppConfiguration</c> thì không:</b>
/// <c>WebApplicationFactory</c> chuyển mọi <c>UseSetting</c>/<c>UseEnvironment</c> thành THAM SỐ DÒNG
/// LỆNH (<c>--key=value</c>) đưa vào entry point, nên chúng có mặt ngay khi
/// <c>WebApplication.CreateBuilder(args)</c> dựng <c>builder.Configuration</c> — tức TRƯỚC cả
/// <c>AddHangfire(...UseNpgsqlConnection(...))</c> lẫn <c>builder.Environment.IsProduction()</c>. Đây
/// chính là hai chỗ mà <see cref="IntegrationTestHostEnvironment"/> cảnh báo là callback của factory
/// chạy quá muộn; <c>UseSetting</c> không dính vấn đề đó.</para>
/// </summary>
internal sealed class ProductionHostFactory : WebApplicationFactory<Program>
{
    /// <summary>Origin hợp lệ dùng cho các host Production CẦN boot được. Không trùng origin dev
    /// nào của máy đang chạy (chúng nằm ở <c>appsettings.Development.json</c> — cấu hình CỤC BỘ
    /// từng máy, KHÔNG có trong repo, xem doc/huong_dan/quy-uoc/repo-artifact.md §1.1) — để không
    /// ai nhầm rằng cấu hình Development đang được dùng lại ở đây (ở Production nó KHÔNG được
    /// nạp).</summary>
    public const string AllowedOrigin = "https://app.production.test";

    /// <summary>Header CHỈ tồn tại trong test, đặt <c>Connection.RemoteIpAddress</c> — thứ mà
    /// <c>TestServer</c> không có (không có kết nối TCP thật). Cùng khuôn với
    /// <c>RateLimitPartitionFactory.ClientIpHeader</c>.</summary>
    public const string ClientIpHeader = "X-Test-Client-Ip";

    /// <summary>Đường dẫn KHÔNG khớp endpoint nào của app ⇒ rơi xuống middleware dò ở CUỐI pipeline
    /// (xem <see cref="ForwardedHeadersProbeStartupFilter"/>).</summary>
    public const string SchemeProbePath = "/__test/forwarded-scheme";

    private static readonly PathString SchemeProbePathString = new(SchemeProbePath);

    /// <summary>
    /// Gốc kho file cho host Production trong test. Phải là đường dẫn TUYỆT ĐỐI —
    /// <c>StorageOptionsValidator</c> từ chối đường dẫn tương đối ở mọi môi trường.
    ///
    /// <para>KHÔNG cần tạo sẵn thư mục: <c>LocalFileStorage</c> chỉ tạo thư mục lúc thật sự ghi
    /// file, và không test Production nào ghi file.</para>
    /// </summary>
    public static readonly string StorageRootPath =
        Path.Combine(Path.GetTempPath(), "platformmanager-it-storage");

    private readonly bool _configureCors;
    private readonly string? _connectionString;
    private readonly bool _configureStorage;
    private readonly ICoreResourceKeySource? _resourceKeySource;

    /// <param name="configureCors">
    /// <c>false</c> = KHÔNG khai <c>Cors:AllowedOrigins</c>. Ở <c>Production</c> đó là cấu hình
    /// THIẾU thật sự: <c>appsettings.json</c> — file cấu hình DUY NHẤT được commit — không có khoá
    /// <c>Cors</c>, còn <c>appsettings.Development.json</c> (chỗ duy nhất có khoá đó, và là file
    /// cục bộ của từng máy chứ không nằm trong repo) thì không được nạp. Dùng cho bất biến 3.
    /// </param>
    /// <param name="connectionString">
    /// Ghi đè <c>ConnectionStrings:Default</c> cho riêng host này — dùng cho bất biến 2, nơi cần một
    /// database TRỐNG chứ không phải database đã seed dùng chung của collection.
    /// </param>
    /// <param name="configureStorage">
    /// <c>false</c> = KHÔNG khai <c>Storage:RootPath</c>. Ở <c>Production</c> đó cũng là cấu hình
    /// THIẾU thật sự, cùng lý do với <paramref name="configureCors"/>: <c>appsettings.json</c>
    /// không có khoá <c>Storage</c>. Dùng cho bất biến 5.
    /// </param>
    /// <param name="resourceKeySource">
    /// Khác <c>null</c> = THAY hiện thực <see cref="ICoreResourceKeySource"/> của host bằng bản
    /// truyền vào. Dùng cho bất biến 6: một danh mục permission-key khai SAI phải chặn host boot.
    ///
    /// <para>Đây là tham số duy nhất trong lớp này thay một DỊCH VỤ chứ không phải một dòng cấu
    /// hình — vì thứ cần làm sai ở đây là DỮ LIỆU do host cấp qua seam, và nó không đến từ
    /// <c>appsettings</c>. Mọi thứ khác của host giữ nguyên, kể cả
    /// <c>ResourceKeyCatalogStartupValidator</c> đang được kiểm.</para>
    /// </param>
    public ProductionHostFactory(
        bool configureCors = true,
        string? connectionString = null,
        bool configureStorage = true,
        ICoreResourceKeySource? resourceKeySource = null)
    {
        _configureCors = configureCors;
        _connectionString = connectionString;
        _configureStorage = configureStorage;
        _resourceKeySource = resourceKeySource;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Production);

        if (_configureCors)
            builder.UseSetting($"{CorsPolicyOptions.SectionName}:AllowedOrigins:0", AllowedOrigin);

        if (_configureStorage)
            builder.UseSetting($"{StorageOptions.SectionName}:RootPath", StorageRootPath);

        if (_connectionString is not null)
            builder.UseSetting("ConnectionStrings:Default", _connectionString);

        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<IStartupFilter, ForwardedHeadersProbeStartupFilter>();

            if (_resourceKeySource is not null)
                services.AddSingleton(_resourceKeySource);
        });
    }

    /// <summary>
    /// Hai middleware kẹp lấy TOÀN BỘ pipeline thật — không thay, không mock, không tắt gì của nó.
    ///
    /// <list type="number">
    /// <item><b>Trước</b> (chạy trước cả <c>app.UseForwardedHeaders</c>): gán
    /// <c>Connection.RemoteIpAddress</c> theo <see cref="ClientIpHeader"/>. Đây là ĐẦU VÀO của bất
    /// biến 4 — <c>ForwardedHeadersMiddleware</c> chỉ tin <c>X-Forwarded-*</c> khi địa chỉ kết nối
    /// nằm trong <c>KnownProxies</c>, mà <c>TestServer</c> để địa chỉ đó <c>null</c>.</item>
    /// <item><b>Sau</b> (điểm rơi cuối pipeline, chỉ khớp <see cref="SchemeProbePath"/>): đọc
    /// <c>Request.Scheme</c> và <c>Connection.RemoteIpAddress</c> SAU khi
    /// <c>UseForwardedHeaders</c> đã xử lý xong. Đây là ĐẦU RA của bất biến 4.</item>
    /// </list>
    ///
    /// <para>Middleware thứ hai đăng ký SAU <c>next(app)</c> nên nằm cuối cùng: request không khớp
    /// endpoint nào đi hết <c>UseEndpoints</c> rồi rơi vào đó. Nhờ vậy không phải thêm controller
    /// dò nào vào host, và mọi đường dẫn khác vẫn 404 y như production (bất biến 1 dựa vào điều
    /// này — middleware dò kiểm đường dẫn trước khi trả lời).</para>
    /// </summary>
    private sealed class ForwardedHeadersProbeStartupFilter : IStartupFilter
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

            app.Use(async (context, nextMiddleware) =>
            {
                if (!context.Request.Path.Equals(SchemeProbePathString, StringComparison.Ordinal))
                {
                    await nextMiddleware();
                    return;
                }

                await context.Response.WriteAsync(
                    $"{context.Request.Scheme}|{context.Connection.RemoteIpAddress}");
            });
        };
    }
}

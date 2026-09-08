using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using PlatformManager.Api.Common;
using PlatformManager.Api.Modules;
using PlatformManager.Api.Permissions;
using PlatformManager.Api.Seeding;
using PlatformManager.Core.Application.Bootstrap;
using PlatformManager.Core.Application.Common.Interfaces;
using PlatformManager.Core.Application.Auth;
using PlatformManager.Core.Application.Common.Results;
using PlatformManager.Core.Application.Menu;
using PlatformManager.Core.Application.Permissions;
using PlatformManager.Core.Infrastructure;
using PlatformManager.Core.Infrastructure.Modules;
using PlatformManager.Core.Infrastructure.Permissions;
using PlatformManager.Core.Infrastructure.Persistence;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Chế độ seed (`--seed`): tiến trình chạy CoreSeeder một lần rồi THOÁT, không mở cổng. Phải biết
// điều này TRƯỚC khi đăng ký DI vì nó đổi luật fail-fast của BootstrapOptions (xem AddCoreModule
// bên dưới) — doc/huong_dan/wiki-core/be/13-core-data-migration.md §"bootstrap Production bằng
// lệnh riêng".
var isSeedRun = SeedCommand.IsRequested(args);

// ── Logging: Serilog ghi file xoay vòng theo ngày, giữ 7 ngày ─────────────
// Chốt 2026-08-31, xem doc/huong_dan/wiki-core/be/07-observability.md §"Serilog ghi file, giữ 7
// ngày". Trước đó không có nơi nào lưu log: response trả traceId, FE hiện traceId, và không ai tra
// được gì từ mã đó.
//
// ⚠️ MỨC LOG DO SERILOG LỌC, KHÔNG PHẢI SECTION "Logging" — vì thế cấu hình mức nằm ở section
// "Serilog" của appsettings.json, và section "Logging" đã được BỎ khỏi appsettings.json thay vì để
// lại làm nguồn sự thật thứ hai (nó sẽ không có tác dụng gì mà vẫn trông như có).
//
// Lý do không hiển nhiên, đã ĐO THẬT 2026-08-31 khi thi công: `AddSerilog` tự đăng ký
// `AddFilter<SerilogLoggerProvider>(null, LogLevel.Trace)` — nghĩa là nó cố ý MỞ HẾT bộ lọc của
// Microsoft.Extensions.Logging cho provider của mình và nhận trách nhiệm lọc về phía Serilog. Lần
// chạy thử đầu tiên (đặt mức ở "Logging:LogLevel", MinimumLevel để Verbose) cho ra log đầy dòng
// [DBG] của Microsoft.AspNetCore dù appsettings khai Warning — đúng cái bẫy này.
//
// Vẫn dùng builder.Logging.AddSerilog (provider) chứ KHÔNG dùng builder.Host.UseSerilog: giữ
// ILoggerFactory chuẩn của .NET, mọi thư viện log qua ILogger<T> vẫn đi đúng đường.
//
// {TraceId} PHẢI có trong template — đó là giá trị nối log với mã người dùng đọc từ màn hình lỗi.
// Tên phải khớp thuộc tính do TraceIdLogEnrichmentMiddleware đẩy vào; lệch tên thì in ra rỗng và
// KHÔNG có lỗi nào báo.
const string LogOutputTemplate =
    "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{TraceId}] {SourceContext}: {Message:lj}{NewLine}{Exception}";

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(
    new LoggerConfiguration()
        // Mức log + Override đọc từ section "Serilog" của appsettings.json (luôn có mặt ở mọi môi
        // trường vì appsettings.json là file được commit). Đổi mức khi chẩn đoán sự cố = sửa cấu
        // hình, không phải build lại.
        .ReadFrom.Configuration(builder.Configuration)
        .Enrich.FromLogContext() // điều kiện để TraceIdLogEnrichmentMiddleware có tác dụng
        .WriteTo.Console(outputTemplate: LogOutputTemplate)
        .WriteTo.File(
            // "log-.txt" + RollingInterval.Day ⇒ file thật là logs/log-20260831.txt
            path: Path.Combine(builder.Environment.ContentRootPath, "logs", "log-.txt"),
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 7, // 7 file ngày = giữ 7 ngày (quyết định 2)
            outputTemplate: LogOutputTemplate,
            shared: true)
        .CreateLogger(),
    dispose: true);

// ── Services ─────────────────────────────────────────────────────────────
// Danh sách tầng của ứng dụng (Core + mọi tầng nghiệp vụ) — dựng MỘT lần rồi dùng cho CẢ BA
// đường nối: ApplicationPart ngay dưới đây, DI + cấu hình EF ở AddModules phía sau. Nguồn sự
// thật là HostModuleRegistrars.cs; thêm tầng mới thì sửa ở đó, không sửa file này.
// Xem doc/kien-truc-core-module.md §IModuleRegistrar.
var moduleRegistrars = HostModuleRegistrars.Create(requireBootstrapOptions: isSeedRun);

// Envelope IApiResult<T> đã CHỐT là camelCase (data/message/status/code/businessCode/
// traceId/retryable/fields) — frontend-expert đã code FE theo đúng quy ước này. Dùng ĐÚNG
// mặc định ASP.NET Core Web API (JsonNamingPolicy.CamelCase) cho TOÀN BỘ response — cả field
// envelope lẫn field bên trong mọi DTO payload (Data). Đặt tường minh (dù đây vốn đã là mặc
// định) để không ai lỡ tay đổi ngược lại PascalCase sau này.
builder.Services
    // options.Filters.Add<RequirePermissionFilter>() — CỘNG DỒN với [Authorize] fail-closed
    // sẵn có ở ApiControllerBase, không thay thế. Filter tự no-op nếu action không khai
    // [RequirePermission] (xem RequirePermissionFilter.cs). Yêu cầu AddPermissionInfrastructure()
    // đã đăng ký IPermissionChecker — gọi TRƯỚC dòng này (xem AddPermissionInfrastructure ở
    // dưới). Xem
    // doc/huong_dan/quy-uoc/be-api-controller.md §"Phân quyền theo hành động".
    .AddControllers(options => options.Filters.Add<RequirePermissionFilter>())
    // Đường 3 của seam IModuleRegistrar: controller nằm ngoài assembly host thì MVC không tự
    // thấy — mỗi tầng khai ApiAssembly được nạp làm ApplicationPart tại đây. Tầng khai null
    // (hôm nay: Core, controller còn nằm trong host) bị bỏ qua. PHẢI gọi trên IMvcBuilder này,
    // không có chỗ nối lại sau khi đã rời builder.
    .AddModuleApplicationParts(moduleRegistrars)
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// Lỗi model binding (sai kiểu, thiếu field bắt buộc của record vị trí, JSON hỏng) xảy ra TRƯỚC
// MediatR nên GlobalExceptionHandler không thấy — mặc định [ApiController] trả ValidationProblemDetails
// thô, không mang envelope. Lý do đầy đủ + hệ quả đã đo ở FE: Common/ModelBindingProblemFactory.cs.
builder.Services.Configure<ApiBehaviorOptions>(options =>
    options.InvalidModelStateResponseFactory = ModelBindingProblemFactory.Build);

// Http.Json.JsonOptions (Microsoft.AspNetCore.Http.Json) là cấu hình RIÊNG, KHÔNG dùng chung
// với Mvc.JsonOptions ở trên — GlobalExceptionHandler gọi HttpResponse.WriteAsJsonAsync() đi
// qua đường này. Cấu hình tường minh để 2 đường response (MVC + exception handler) LUÔN cùng
// 1 casing, tránh lệch shape giữa lỗi bắt bởi handler MediatR (đi qua MVC) và lỗi bắt bởi
// GlobalExceptionHandler (đi qua middleware toàn cục).
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

// Đường 1 và 2 của seam IModuleRegistrar: mỗi tầng tự đăng ký MediatR/FluentValidation/
// repository/dịch vụ của mình (RegisterServices), rồi nộp assembly chứa IEntityTypeConfiguration
// của mình cho DbContext quét (PersistenceAssembly). Host KHÔNG gọi AddCoreModule() thẳng —
// Core đi qua đúng seam mà tầng nghiệp vụ đi, để đường đó được chạy mỗi lần khởi động thay vì
// nằm chờ tới tầng đầu tiên. Thứ tự trong danh sách là thứ tự đăng ký, Core đứng đầu.
//
// requireBootstrapOptions (đã truyền vào lúc dựng danh sách phía trên): CHỈ đường chạy lệnh seed
// mới bắt buộc 2 mật khẩu bootstrap. Trước 2026-08-31 ValidateOnStart() là không điều kiện, nên
// tiến trình API đòi 2 secret mà nó không bao giờ đọc tới — người vận hành đặt secret, thấy app
// lên, và kết luận nhầm rằng đã bootstrap xong.
builder.Services.AddModules(builder.Configuration, moduleRegistrars);

// Dữ liệu menu của CHÍNH dự án này (nhãn/route/icon) — Core giữ CƠ CHẾ seed, host cung cấp DỮ
// LIỆU (tách 2026-09-02, xem ICoreMenuSeedSource). Core cố ý không có hiện thực mặc định: quên
// dòng này thì CoreSeeder không phân giải được từ DI và lệnh `--seed` thoát khác 0, thay vì âm
// thầm seed ra một sidebar trống. Singleton — bảng hằng số, không trạng thái.
builder.Services.AddSingleton<ICoreMenuSeedSource, AppMenuSeedSource>();

// Email + tên hiển thị của 2 tài khoản bootstrap (tách 2026-09-02, xem ICoreBootstrapAccountSource).
// Cùng lý do và cùng khuôn với dòng trên, nhưng hậu quả của việc quên nặng hơn: nếu Core có bản
// mặc định thì "quên dòng này" sẽ đẻ ra một tài khoản quản trị THẬT mang tên miền của dự án khác,
// và CoreSeeder không ghi đè tài khoản đã tồn tại nên nó ở lại vĩnh viễn. Vì thế Core cố ý không
// có bản mặc định — thiếu đăng ký thì `--seed` thoát khác 0 trước khi ghi.
builder.Services.AddSingleton<ICoreBootstrapAccountSource, AppBootstrapAccountSource>();

// Danh mục permission-key của dự án (tách 2026-09-03, xem ICoreResourceKeySource). Cùng khuôn với
// 2 dòng trên. Hậu quả của việc quên: RequirePermissionFilter là deny-by-default, nên một danh mục
// rỗng đồng nghĩa mọi endpoint có [RequirePermission] trả 403 cho tất cả trừ SuperAdmin — im lặng
// và rất khó lần ra. Vì thế Core không có bản mặc định: thiếu dòng này thì DI hỏng ngay.
builder.Services.AddSingleton<ICoreResourceKeySource, AppResourceKeySource>();

// Permission-by-action (RolePermission) — TÁCH khỏi AddCoreModule có chủ đích (xem
// PermissionInfrastructureExtensions.cs). Chỉ đăng ký DI; filter được gắn vào pipeline MVC ở
// AddControllers() phía trên.
builder.Services.AddPermissionInfrastructure();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Hangfire — hạ tầng job nền. ⚠️ Hôm nay KHÔNG job nghiệp vụ nào dùng nó: đường Import CSV/Excel
// đã bị gỡ cùng module DtiWeekly 2026-08-29. Giữ lại hạ tầng có chủ đích, xem
// doc/huong_dan/quy-uoc/be-cqrs-handler.md §"Command chạy lâu → job nền". Dùng CHUNG connection
// string "Default" với PlatformManagerDbContext — Hangfire tự tạo schema "hangfire" lúc khởi
// động lần đầu (KHÔNG đi qua EF Core migration).
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(builder.Configuration.GetConnectionString("Default"))));
builder.Services.AddHangfireServer();

// Seam IBackgroundJobScheduler → HangfireBackgroundJobScheduler. PHẢI gọi SAU AddHangfire()
// (hiện thực inject IBackgroundJobClient do AddHangfire đăng ký). Tầng Application enqueue qua
// seam này, KHÔNG gọi thẳng BackgroundJob.Enqueue — xem
// Core.Application/Common/Interfaces/IBackgroundJobScheduler.cs.
builder.Services.AddBackgroundJobInfrastructure();

// Notification (INotificationSender/SmtpNotificationSender): CỐ Ý CHƯA ĐĂNG KÝ (2026-08-29).
// AddNotificationInfrastructure() tồn tại ở Core.Infrastructure nhưng KHÔNG dòng nào gọi nó —
// đây là chủ đích, không phải bỏ sót. Hai lý do:
//   1. Chưa có consumer: không handler nào inject INotificationSender. Bật lên chỉ đăng ký một
//      service không ai dùng.
//   2. Bật lên sẽ LÀM APP KHÔNG KHỞI ĐỘNG ĐƯỢC: SmtpOptions dùng ValidateOnStart() và
//      appsettings.json chưa có section "Smtp" ⇒ OptionsValidationException ngay lúc boot, kéo
//      đỏ luôn toàn bộ integration test (chúng chạy host thật qua WebApplicationFactory).
// Bật khi nào: có use case thật cần gửi mail (vd quên mật khẩu). Trình tự: thêm section "Smtp"
// vào cấu hình TRƯỚC, rồi mới thêm dòng
// builder.Services.AddNotificationInfrastructure(builder.Configuration); ngay dưới đây.
//
// ⚠️ BỔ SUNG 2026-09-03 — nay còn MỘT dòng thứ hai bắt buộc, đứng cạnh dòng trên:
// builder.Services.AddScoped<INotificationTemplateRenderer, ...>(). INotificationSender nay nhận
// khoá + tham số + ngôn ngữ thay vì chuỗi đã dựng (doc/huong_dan/wiki-core/be/16-i18n-va-ma-loi.md
// §6), nên phải có ai đó biến khoá thành câu, và người đó là HOST — Core cố ý không có hiện thực
// mặc định, cùng khuôn với ICoreMenuSeedSource ngay phía trên. Quên dòng này thì app không phân
// giải được SmtpNotificationSender; đó là ý đồ, không phải thiếu sót.
//
// Câu hỏi chưa có lời đáp và PHẢI trả lời ở dòng gọi đầu tiên: lấy ngôn ngữ của người nhận ở đâu.
// AppUser không có cột nào lưu thứ đó (đối chiếu 2026-09-03), nên consumer đầu tiên hoặc thêm cột
// (đổi lược đồ DB), hoặc lấy từ phiên đang thao tác, hoặc chốt một hằng số ở đây — cả ba đều là
// quyết định của người dùng, không phải chi tiết cài đặt tự chọn.

// Health check — liveness (process còn sống, không kiểm dependency) tách khỏi readiness (DB
// connect được) để DB chậm tạm thời không khiến orchestrator restart oan 1 app đang khoẻ. Xem
// doc/huong_dan/wiki-core/be/07-observability.md §"Liveness vs readiness".
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
    .AddDbContextCheck<PlatformManagerDbContext>(tags: ["ready"]);

// CORS: AllowCredentials() + origin cụ thể từ config — TUYỆT ĐỐI không AllowAnyOrigin() khi
// dùng cookie (cookie sẽ bị trình duyệt âm thầm bỏ qua). Xem doc/huong_dan/quy-uoc/be-api-controller.md.
//
// ⚠️ SỬA 2026-08-31: trước đây đọc thẳng IConfiguration và kết thúc bằng `?? []` — dòng đó biến một
// cấu hình THIẾU thành allowlist RỖNG, mà allowlist rỗng chặn MỌI origin. Ở Production hôm nay
// không có khoá "Cors" nào ⇒ không một lời gọi API nào của FE tới được app, trong khi /health vẫn
// xanh và deploy vẫn báo thành công. Nay đi qua đúng khuôn IOptions<T> + ValidateDataAnnotations,
// xem CorsPolicyOptions.cs và doc/huong_dan/quy-uoc/be-architecture.md §"Quyết định người dùng
// 2026-08-31 — Cors:AllowedOrigins phải theo đúng khuôn này".
var corsOptionsBuilder = builder.Services.AddOptions<CorsPolicyOptions>()
    .Bind(builder.Configuration.GetSection(CorsPolicyOptions.SectionName))
    .ValidateDataAnnotations();

// Fail-fast CHỈ ở Production — app từ chối khởi động kèm thông điệp nêu đích danh biến môi trường
// còn thiếu. Development KHÔNG fail-fast: cấu hình dev là CỤC BỘ từng máy — appsettings.Development.json
// nằm NGOÀI repo, máy mới phải tự tạo (sửa 2026-09-08, lý do đầy đủ ở CorsPolicyOptions.cs). Thiếu nó
// thì allowlist rỗng, lộ ra ngay lần gọi API đầu tiên chứ không âm thầm trả mảng rỗng như bản cũ.
if (builder.Environment.IsProduction())
{
    corsOptionsBuilder.ValidateOnStart();
}

builder.Services.AddCors();
// Policy được dựng LƯỜI từ IOptions<CorsPolicyOptions> (ConfigureDefaultCorsPolicy) — không có
// đường tắt nào đọc cấu hình mà bỏ qua validation.
builder.Services.AddSingleton<IConfigureOptions<Microsoft.AspNetCore.Cors.Infrastructure.CorsOptions>,
    ConfigureDefaultCorsPolicy>();

// Rate limiting — chặn brute-force POST /api/auth/login (theo IP) + hạn mức nền cho MỌI request
// (theo IP, kể cả endpoint chưa khai gì) qua GlobalLimiter. Xem
// doc/huong_dan/quy-uoc/be-api-controller.md §"Rate limiting" +
// doc/huong_dan/wiki-core/be/09-security-beyond-auth.md.
//
// Phân vùng theo IP chỉ đúng khi RemoteIpAddress là IP THẬT của người dùng — điều đó do
// UseForwardedHeaders ở đầu pipeline bảo đảm (khai KnownProxies tường minh, xem phần Pipeline bên
// dưới). Không cần đọc header nào ở đây: ForwardedHeaders ghi đè thẳng vào Connection.RemoteIpAddress.
const int LoginPermitLimitPerMinute = 5;

// 200/phút, cửa sổ TRƯỢT 6 đoạn (mỗi đoạn 10 giây) — chốt 2026-08-30, xem
// doc/huong_dan/quy-uoc/be-api-controller.md §"Hiệu chỉnh hạn mức". Cửa sổ cố định cũ vừa cho phép
// dồn tới 2× hạn mức khi burst vắt qua ranh giới phút, vừa phạt tới 55 giây khi chạm hạn mức ở
// giây thứ 5. Cửa sổ trượt sửa cả hai chiều: trần đỉnh không đổi, sức chứa liên tục gấp đôi, thời
// gian hồi nhanh gấp 6 lần.
const int GlobalPermitLimitPerMinute = 200;
const int GlobalSegmentsPerWindow = 6;

// Hàng rào THỨ HAI cho đăng nhập — phân vùng theo TÊN ĐĂNG NHẬP, CỘNG DỒN với hàng rào theo IP.
// Chặn đúng kịch bản mà hàng rào theo IP bỏ lọt: brute-force PHÂN TÁN từ hàng nghìn IP cùng nhắm
// một tài khoản (chốt 2026-08-31, xem doc/huong_dan/wiki-core/be/09-security-beyond-auth.md
// §"Chính sách mật khẩu" quyết định 3).
//
// Dùng cửa sổ TRƯỢT có chủ đích, khác policy "login" theo IP: tài khoản SuperAdmin được miễn khoá
// tài khoản, nên hàng rào này là thứ duy nhất chặn dò mật khẩu vào nó — nhưng cũng chính vì thế nó
// phải trả lại lượt LIÊN TỤC để quản trị viên thật vẫn vào được (chỉ chậm hơn) thay vì bị từ chối
// sạch trong cả cửa sổ như khoá tài khoản.
const int LoginUserNamePermitLimit = 10;
const int LoginUserNameWindowMinutes = 5;
const int LoginUserNameSegmentsPerWindow = 5; // mỗi đoạn 1 phút ⇒ hồi ~2 lượt/phút

// KHÔNG đọc header nào do client gửi để chọn phân vùng (cho client tự chọn phân vùng là tự vô
// hiệu hoá rate limit) — chỉ đọc kết nối TCP thật. "unknown-ip" dùng chung cho mọi kết nối không
// xác định được IP (vd TestServer không có kết nối TCP thật).
static string ResolveRateLimitPartitionKey(HttpContext ctx)
    => ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown-ip";

// GlobalLimiter áp cho MỌI request đi qua UseRateLimiter — 2 nhánh phải miễn trừ:
// - OPTIONS: preflight CORS (và OPTIONS trần) không phải request nghiệp vụ, đếm vào sẽ chia đôi
//   hạn mức thật của FE (mỗi request thật kèm 1 preflight).
// - /hangfire: Dashboard là nhánh middleware của Hangfire (app.Map nội bộ), KHÔNG phải endpoint
//   ASP.NET Core nên KHÔNG gắn được [DisableRateLimiting] như /health — phải miễn theo đường dẫn.
//   Dashboard tự poll /hangfire/stats ~2 giây/lần, không miễn sẽ ngốn hết hạn mức và đá admin ra.
static bool IsExemptFromGlobalRateLimit(HttpContext ctx)
    => HttpMethods.IsOptions(ctx.Request.Method) || ctx.Request.Path.StartsWithSegments("/hangfire");

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Policy riêng cho login — PHÂN VÙNG THEO IP. KHÔNG dùng AddFixedWindowLimiter(policyName, …)
    // — overload đó tạo ĐÚNG MỘT limiter dùng chung cho toàn app, không phân vùng gì cả (xem cảnh
    // báo đầy đủ ở be-api-controller.md §"Rate limiting").
    options.AddPolicy("login", ctx => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: ResolveRateLimitPartitionKey(ctx),
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = LoginPermitLimitPerMinute,
            Window = TimeSpan.FromMinutes(1),
        }));

    // Hạn mức nền cho MỌI request còn lại (kể cả endpoint chưa khai [EnableRateLimiting]) — cũng
    // theo IP. CỘNG DỒN với policy "login", không thay thế.
    //
    // ⚠️ Nhánh miễn trừ PHẢI dùng key HẰNG (KHÔNG PHẢI ResolveRateLimitPartitionKey(ctx)).
    // PartitionedRateLimiter cache limiter THEO KEY — factory chỉ chạy ĐÚNG 1 LẦN cho mỗi key,
    // những lần sau CÙNG key sẽ tái dùng limiter ĐÃ TẠO trước đó bất kể nhánh nào gọi. Nếu key
    // miễn trừ trùng với key của FixedWindowLimiter thường (cùng là IP), request thường đi trước
    // sẽ "khoá" luôn key đó vào đúng FixedWindowLimiter đã cạn — request /hangfire đi sau cùng
    // IP bị ăn ké đúng bộ đếm đã cạn đó thay vì được miễn (đã bắt được bằng
    // GlobalRateLimitTests.HangfireDashboard_IsNotThrottled_EvenAfterGlobalQuotaExhausted).
    //
    // CreateChained: MỌI limiter trong chuỗi phải cấp lượt thì request mới đi tiếp — một cái từ
    // chối là cả request bị từ chối (vẫn đi qua OnRejected bên dưới). Đây là cách gắn hàng rào thứ
    // hai theo TÊN ĐĂNG NHẬP mà KHÔNG phải đụng vào AuthController: [EnableRateLimiting] chỉ gắn
    // được MỘT policy cho một action, còn GlobalLimiter thì cộng dồn được bao nhiêu tuỳ ý.
    options.GlobalLimiter = PartitionedRateLimiter.CreateChained(
        // (1) Hạn mức nền theo IP cho MỌI request.
        PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
            IsExemptFromGlobalRateLimit(ctx)
                ? RateLimitPartition.GetNoLimiter("exempt")
                : RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: ResolveRateLimitPartitionKey(ctx),
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = GlobalPermitLimitPerMinute,
                        Window = TimeSpan.FromMinutes(1),
                        SegmentsPerWindow = GlobalSegmentsPerWindow,
                    })),

        // (2) Hàng rào theo TÊN ĐĂNG NHẬP — CHỈ áp cho POST /api/auth/login, mọi đường khác đi qua
        // không tốn lượt. Tên đăng nhập do LoginUserNameRateLimitMiddleware đọc sẵn từ thân request
        // và cất vào HttpContext.Items (hàm chọn phân vùng chạy đồng bộ, không đọc thân được).
        //
        // ⚠️ Không gian khoá của limiter này TÁCH RIÊNG với limiter (1) — mỗi PartitionedRateLimiter
        // có bộ nhớ đệm limiter theo khoá của chính nó, nên khoá "exempt-not-login" ở đây không đụng
        // gì tới khoá "exempt" ở trên. Vẫn giữ tên khác nhau cho người đọc khỏi phải suy luận điều đó.
        PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
            LoginUserNameRateLimitMiddleware.IsLoginRequest(ctx)
                ? RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: LoginUserNameRateLimitMiddleware.ResolvePartitionKey(ctx),
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = LoginUserNamePermitLimit,
                        Window = TimeSpan.FromMinutes(LoginUserNameWindowMinutes),
                        SegmentsPerWindow = LoginUserNameSegmentsPerWindow,
                    })
                : RateLimitPartition.GetNoLimiter("exempt-not-login")));

    // 429 mặc định của middleware trả body RỖNG (không đi qua envelope IApiResult) — bọc lại để
    // FE không phải xử lý riêng cho rate limit. Dùng CHUNG cho cả policy "login" lẫn GlobalLimiter.
    options.OnRejected = async (context, ct) =>
    {
        // Retry-After là LỜI HỨA VỚI CLIENT, đã ghi trong doc/contracts/auth.md — mọi 429 phải có
        // nó. Không được để một chi tiết cấu tạo bên trong làm mất header này.
        //
        // ⚠️ PartitionedRateLimiter.CreateChained KHÔNG truyền metadata của limiter con ra lease
        // gộp (đo được 2026-09-01: 429 do GlobalLimiter chained từ chối trả về lease không có
        // MetadataName.RetryAfter, trong khi cùng đường đó trước khi chuyển sang chained thì có).
        // Vì vậy phải có đường lùi, nếu không client mất tín hiệu "chờ bao lâu" một cách im lặng.
        var retryAfterSeconds = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
            ? (int)Math.Ceiling(retryAfter.TotalSeconds)
            // Đường lùi = độ dài cửa sổ NGẮN NHẤT trong các hàng rào (hạn mức nền, 1 phút). Cố ý
            // chọn cận DƯỚI chứ không phải cận trên: thà client thử lại sớm và có thể nhận 429 lần
            // nữa, còn hơn bị bảo chờ 5 phút cho một hàng rào 1 phút.
            : (int)TimeSpan.FromMinutes(1).TotalSeconds;

        context.HttpContext.Response.Headers.RetryAfter =
            retryAfterSeconds.ToString(CultureInfo.InvariantCulture);

        context.HttpContext.Response.ContentType = "application/json";

        var result = new ApiResult<object>
        {
            Status = ApiResultStatus.BUSINESS_ERROR,
            Code = ErrorCode.TooManyRequests,
            BusinessCode = RateLimitErrors.TooManyRequests.BusinessCode,
            Message = RateLimitErrors.TooManyRequests.MessageTemplate,
            Retryable = true,
            TraceId = context.HttpContext.TraceIdentifier,
        };

        await context.HttpContext.Response.WriteAsJsonAsync(result, ct);
    };
});

// Cookie session (đã CHỐT — KHÔNG JWT). Override OnRedirectToLogin/OnRedirectToAccessDenied
// để trả thẳng 401/403 JSON — mặc định Identity redirect 302 sang trang Razor, sai hoàn
// toàn với API JSON (đây là gotcha rủi ro cao nhất, xem doc/ke-hoach-xay-lai-corebase.md).
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "PlatformManager.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.None; // FE (Angular, port khác) gọi cross-site
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // bắt buộc đi kèm SameSite=None
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
    options.SlidingExpiration = true;

    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";
        // Dùng lại AuthErrors.NotAuthenticated thay vì gõ lại "Chưa đăng nhập." — chuỗi đó đã
        // khai ở catalog, và bản gõ tay là nguồn thứ hai cho cùng một sự thật (.claude/CLAUDE.md §5).
        var result = ApiResult<object>.BusinessError(
            AuthErrors.NotAuthenticated,
            AuthErrors.NotAuthenticated.MessageTemplate);
        result.TraceId = context.HttpContext.TraceIdentifier;
        return context.Response.WriteAsJsonAsync(result);
    };

    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/json";
        // Đường ra của MỌI lần RequirePermissionFilter trả ForbidResult() — không có mã thì
        // toàn bộ 403 phân quyền ra FE không có gì để tra.
        var result = ApiResult<object>.BusinessError(
            InfrastructureErrors.Forbidden,
            InfrastructureErrors.Forbidden.MessageTemplate);
        result.TraceId = context.HttpContext.TraceIdentifier;
        return context.Response.WriteAsJsonAsync(result);
    };
});

// CSRF — Lớp 2. (SỬA 2026-08-31: chú thích cũ nói "Lớp 1 là SameSite ở ConfigureApplicationCookie
// phía trên" — SAI. Cookie phiên khai SameSite=None vì FE nằm ở origin khác, mà SameSite=None nghĩa
// là KHÔNG có lớp SameSite nào cả. Lớp 1 thật là kiểm header Origin trên mọi request ghi, xem
// OriginValidationMiddleware.cs + doc/huong_dan/wiki-core/be/02-identity-auth.md §"kiểm header
// Origin thay cho Lớp 1".) Mô hình SPA (không
// phải Razor form): FE gọi GET /api/antiforgery/token lúc load app, đọc REQUEST-TOKEN từ cookie
// "XSRF-TOKEN" (KHÔNG HttpOnly — Angular HttpClient PHẢI đọc được bằng JS), rồi tự gắn lại vào
// header "X-XSRF-TOKEN" cho mọi request ghi (đúng cơ chế double-submit-cookie). Xem
// doc/huong_dan/wiki-core/be/02-identity-auth.md §CSRF.
//
// ⚠️ SỬA 2026-08-24 (core-reviewer phát hiện): TRƯỚC đây đặt options.Cookie.Name = "XSRF-TOKEN"
// khiến chính CƠ CHẾ NỘI BỘ của AddAntiforgery ghi COOKIE-TOKEN (nửa "bí mật lưu server-side")
// vào cookie tên "XSRF-TOKEN" — nhưng Angular cần đọc REQUEST-TOKEN (nửa "gửi lại qua header"),
// hai nửa này là 2 giá trị KHÁC NHAU của cùng cơ chế double-submit, không phải bản sao của nhau.
// Angular vô tình echo cookie-token vào header ⇒ ValidateRequestAsync ném
// AntiforgeryValidationException với message đúng nghĩa "the cookie token and the request token
// were swapped" — KHOÁ MỌI request ghi thật từ trình duyệt, kể cả POST /api/auth/login. Bản sửa:
// để AddAntiforgery tự quản cookie NỘI BỘ (không đổi tên, giữ HttpOnly=true — JS không cần đọc
// cookie này), rồi endpoint /api/antiforgery/token bên dưới TỰ TAY set MỘT cookie RIÊNG tên
// "XSRF-TOKEN" chứa đúng REQUEST-TOKEN — đúng mẫu chuẩn của Microsoft cho SPA.
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.HttpOnly = true; // cookie NỘI BỘ — JS không cần đọc, không phải cookie Angular echo lại
    options.Cookie.SameSite = SameSiteMode.None; // cùng chính sách với cookie phiên — FE khác origin, vẫn phải gửi được
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.HeaderName = "X-XSRF-TOKEN";
});

var app = builder.Build();

// ── Chế độ lệnh: seed rồi THOÁT ──────────────────────────────────────────
// Đặt NGAY sau Build() và TRƯỚC mọi cấu hình pipeline: tiến trình seed không phục vụ request nào,
// không mở cổng, không chạy Hangfire server (hosted service chỉ khởi động khi app.Run()).
if (isSeedRun)
{
    return await SeedCommand.RunAsync(app);
}

// ── Pipeline ─────────────────────────────────────────────────────────────
// TraceId enrichment PHẢI đứng đầu — mọi log entry sinh ra sau đây mang traceId mà client nhìn
// thấy trong response lỗi. Đặt trước cả UseExceptionHandler: log của chính exception handler cũng
// cần tra được. Xem doc/huong_dan/wiki-core/be/07-observability.md §Serilog quyết định 3.
app.UseTraceIdLogEnrichment();

// UseForwardedHeaders — nginx cắt TLS rồi chuyển HTTP thuần vào Kestrel (mô hình triển khai B,
// chốt 2026-08-30: doc/huong_dan/wiki-core/fe/17-phuc-vu-va-trien-khai.md §2 và §6.3).
//
// Thiếu nó thì app TƯỞNG kết nối không bảo mật ⇒ từ chối phát cookie khai SecurePolicy=Always ⇒
// GET /api/antiforgery/token trả 500, FE không bao giờ có token, KHÔNG request ghi nào đi qua —
// mà /health vẫn xanh nên deploy vẫn báo thành công.
//
// ⚠️ KnownProxies khai TƯỜNG MINH và ở mức hẹp nhất có thể (chỉ loopback, nơi nginx chạy cùng máy).
// Bật ForwardedHeaders mà không khai proxy tin cậy thì TỆ HƠN KHÔNG BẬT: bất kỳ ai cũng giả được
// X-Forwarded-For để tự chọn phân vùng rate limit, tức tự vô hiệu hoá cả hàng rào chống brute-force.
// Request từ Internet không bao giờ đến từ 127.0.0.1 nên danh sách này là ranh giới thật.
//
// ForwardLimit = 1: đúng MỘT proxy (nginx). Nếu mai kia có CDN đứng trước, cách đúng là thêm module
// real_ip vào nginx với dải IP của nhà cung cấp — KHÔNG nâng ForwardLimit ở app (§6.3).
var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor,
    ForwardLimit = 1,
};
// Mặc định của ASP.NET Core đã có sẵn loopback trong KnownNetworks/KnownProxies, nhưng khai lại
// tường minh vì đây là ranh giới an toàn — người sửa sau phải thấy nó, không phải đi tra mặc định.
forwardedHeadersOptions.KnownProxies.Clear();
forwardedHeadersOptions.KnownIPNetworks.Clear(); // KnownNetworks đã lỗi thời (ASPDEPR005) trên .NET 10
forwardedHeadersOptions.KnownProxies.Add(IPAddress.Loopback);      // 127.0.0.1
forwardedHeadersOptions.KnownProxies.Add(IPAddress.IPv6Loopback);  // ::1
app.UseForwardedHeaders(forwardedHeadersOptions);

app.UseExceptionHandler(); // GlobalExceptionHandler — ValidationException -> 400+Fields, còn lại -> 500

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(CorsPolicyOptions.PolicyName);

// Envelope cho 404/405 do hạ tầng ĐỊNH TUYẾN sinh (thân rỗng) — xem
// Common/ApiStatusCodeEnvelopeMiddleware.cs.
//
// PHẢI đứng SAU UseCors: middleware này chỉ ghi THÂN response sau khi next() đã chạy xong, nên nếu
// nó nằm ngoài UseCors thì 404/405 vẫn giữ header CORS (CorsMiddleware đã áp header trên đường
// vào) — nhưng đặt sau vẫn là vị trí đúng theo cùng lý do đã ghi cho UseRateLimiter: mọi response
// lỗi mà trình duyệt phải đọc được đều nên nằm TRONG phạm vi CORS, không dựa vào thứ tự áp header.
//
// Đứng TRƯỚC UseRateLimiter/UseAuthentication cũng không sao: nó không cắt mạch request nào, chỉ
// điền thân cho response đã có status 404/405 và thân RỖNG. Mọi nhánh lỗi khác (401/403/429/400/500)
// đều đã ghi thân nên đi qua đây không bị chạm.
app.UseApiStatusCodeEnvelope();

// Đọc trước tên đăng nhập cho hàng rào rate limit thứ hai — PHẢI đứng TRƯỚC UseRateLimiter (hàm
// chọn phân vùng chạy đồng bộ, không tự đọc thân request được). Xem
// LoginUserNameRateLimitMiddleware.cs.
app.UseLoginUserNameCapture();

// PHẢI đứng sau UseCors (429 vẫn giữ header CORS, không hiện lỗi CORS mờ mịt phía trình duyệt)
// và TRƯỚC UseAuthentication (UseRouting tự chèn ở đầu pipeline nên endpoint đã phân giải xong —
// đặt sau UseAuthentication/UseAuthorization sẽ khiến request CHƯA đăng nhập bị cắt mạch 401
// trước khi chạm rate limiter, và GlobalLimiter chỉ còn bảo vệ lưu lượng ĐÃ đăng nhập — lỗ hổng đã
// đo thật 2026-08-21, xem PipelineOrderRateLimitTests).
app.UseRateLimiter();

app.UseAuthentication(); // PHẢI đứng TRƯỚC UseAuthorization (code cũ thiếu bước này)
app.UseAuthorization();

// CSRF Lớp 1 — kiểm header Origin cho MỌI request ghi (chốt 2026-08-31).
//
// ⚠️ PHẢI đứng TRƯỚC UseHangfireDashboard(). Dashboard là nhánh middleware TỰ xử lý và KHÔNG gọi
// next() cho request khớp "/hangfire" — đặt sau nó thì mọi POST retry/delete job nằm NGOÀI cả hai
// lớp CSRF. Cookie phiên khai SameSite=None (bắt buộc vì FE khác origin) nên trình duyệt VẪN gửi
// cookie cho form POST cross-site: một trang lạ submit form tới /hangfire/jobs/.../delete sẽ chạy
// dưới danh nghĩa admin.
//
// Đặt trước KHÔNG chặn nhầm admin, vì middleware này cho qua hai nhánh: request không có header
// Origin (OriginValidationMiddleware, nhánh IsNullOrEmpty) và request mang Origin trùng chính
// origin của API (nhánh selfOrigin). POST của Dashboard rơi vào một trong hai.
//
// (Sửa 2026-09-01 sau core-review. Chú thích trước đó lấy lý do của LỚP 2 — dashboard không mang
// X-XSRF-TOKEN — áp cho lớp này; lý do đó không đúng ở đây.)
app.UseOriginValidation();

// Hangfire Dashboard ("/hangfire") — CHỈ Roles.SuperAdmin (HangfireDashboardAuthFilter), đặt
// SAU UseAuthentication()/UseAuthorization() vì filter đọc HttpContext.User. Xem
// doc/huong_dan/wiki-core/be/07-observability.md §"Hangfire Dashboard".
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new HangfireDashboardAuthFilter()],
});

// FE lấy token lúc load app (hoặc lúc login) — GET không cần CSRF nên endpoint này KHÔNG cần
// tự bảo vệ bằng chính cơ chế nó phát hành. Miễn rate limit giống /health: gọi đúng 1 lần/phiên
// làm việc (SPA load) không phải lưu lượng cần siết, và giữ hạn mức GlobalLimiter dành cho
// request nghiệp vụ thật thay vì bị bước "lấy token" đứng trước ăn mất 1 slot.
app.MapGet("/api/antiforgery/token", (IAntiforgery antiforgery, HttpContext ctx) =>
{
    var tokens = antiforgery.GetAndStoreTokens(ctx);

    // Cookie RIÊNG, KHÁC cookie nội bộ của AddAntiforgery ở trên — chứa REQUEST-TOKEN (không
    // phải cookie-token). ĐÂY là giá trị Angular HttpXsrfInterceptor đọc rồi echo vào header
    // X-XSRF-TOKEN. Không set cookie này thì Angular không có gì để đọc; set nhầm giá trị (vd
    // cookie-token) thì đây chính là bug "tokens swapped" đã sửa ở AddAntiforgery phía trên.
    ctx.Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken!, new CookieOptions
    {
        HttpOnly = false, // Angular PHẢI đọc được bằng document.cookie — khác cookie nội bộ
        SameSite = SameSiteMode.None,
        Secure = true,
    });

    return Results.Ok(new { token = tokens.RequestToken });
}).DisableRateLimiting();

// Validate CSRF cho MỌI request ghi — CHỈ method, không loại trừ theo path. Đặt SAU
// UseHangfireDashboard(): dashboard là branch middleware TỰ xử lý và KHÔNG gọi next() cho
// request khớp "/hangfire" (xem GlobalRateLimitTests — lý do UseRateLimiter phải loại trừ
// path đó là vì nó đứng TRƯỚC nhánh Dashboard; đặt middleware này SAU nhánh Dashboard thì
// request "/hangfire" đã bị chặn lại ở đó, không bao giờ chạm tới đây — dashboard tự POST cho
// action retry/delete job mà không mang X-XSRF-TOKEN, đặt nhầm vị trí sẽ chặn nhầm chính admin).
// AntiforgeryValidationException ném ra được GlobalExceptionHandler dịch thành 403 (xem
// GlobalExceptionHandler.cs).
app.Use(async (ctx, next) =>
{
    if (HttpMethods.IsPost(ctx.Request.Method) || HttpMethods.IsPut(ctx.Request.Method) ||
        HttpMethods.IsDelete(ctx.Request.Method) || HttpMethods.IsPatch(ctx.Request.Method))
    {
        await ctx.RequestServices.GetRequiredService<IAntiforgery>().ValidateRequestAsync(ctx);
    }

    await next();
});

app.MapControllers();

// Liveness/readiness tách riêng — DB chậm tạm thời chỉ làm /health/ready Unhealthy (load
// balancer ngưng route traffic), KHÔNG làm /health/live Unhealthy (tránh orchestrator restart
// oan 1 app đang khoẻ). Xem doc/huong_dan/wiki-core/be/07-observability.md §"Liveness vs readiness".
// Không rate-limit health check — orchestrator/monitoring cần gọi các endpoint này thường
// xuyên, rate limit vào đây gây báo động giả (DisableRateLimiting() bỏ qua CẢ GlobalLimiter,
// không chỉ policy có tên).
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live"),
}).DisableRateLimiting();
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
}).DisableRateLimiting();
// Gộp cả 2 (không Predicate = kiểm mọi check đã đăng ký) — endpoint tổng hợp cho công cụ
// monitoring chỉ gọi 1 URL duy nhất, không cần phân biệt liveness/readiness.
app.MapHealthChecks("/health").DisableRateLimiting();

// Seed dữ liệu (DML — role/bootstrap-user/SysMenu/SysMenuRole/danh mục CSV) KHÔNG còn chạy trên
// đường khởi động này, ở BẤT KỲ môi trường nào (chốt 2026-08-30, xem
// doc/huong_dan/wiki-core/be/13-core-data-migration.md §"bootstrap Production bằng lệnh riêng").
// Vấn đề chưa bao giờ là CoreSeeder mà là "seed lúc app khởi động": hàng rào IsDevelopment() cũ vừa
// để một database Production mới không có role/tài khoản/menu nào (không ai đăng nhập được, và
// cũng không có đường tạo tài khoản đầu tiên), vừa cho tiến trình phục vụ thật quyền ghi dữ liệu seed.
//
// Đường thay thế: chạy chính binary này với tham số --seed (xem SeedCommand.cs) — seed một lần rồi
// thoát, không mở cổng. Vẫn KHÔNG BAO GIỜ tự chạy migration/DDL: schema áp bằng cách người dùng tự
// chạy tay file .sql sinh từ `dotnet ef migrations script`, xem doc/ke-hoach-xay-lai-corebase.md.
// Khi có module nghiệp vụ, seeder của nó gọi SAU CoreSeeder trong SeedCommand (module có thể cần
// role/user Core đã tồn tại).

await app.RunAsync();
return 0;

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using PlatformManager.Api.Common;
using PlatformManager.Core.Application.Common.Interfaces;
using PlatformManager.Core.Application.Common.Results;
using PlatformManager.Core.Infrastructure;
using PlatformManager.Core.Infrastructure.Permissions;
using PlatformManager.Core.Infrastructure.Persistence;
using PlatformManager.Modules.DtiWeekly.Infrastructure;
using PlatformManager.Modules.DtiWeekly.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// ── Services ─────────────────────────────────────────────────────────────
// Envelope IApiResult<T> đã CHỐT là camelCase (data/message/status/code/businessCode/
// traceId/retryable/fields) — frontend-expert đã code FE theo đúng quy ước này. Dùng ĐÚNG
// mặc định ASP.NET Core Web API (JsonNamingPolicy.CamelCase) cho TOÀN BỘ response — cả field
// envelope lẫn field bên trong mọi DTO payload (Data). Đặt tường minh (dù đây vốn đã là mặc
// định) để không ai lỡ tay đổi ngược lại PascalCase sau này.
builder.Services
    // options.Filters.Add<RequirePermissionFilter>() — CỘNG DỒN với [Authorize] fail-closed
    // sẵn có ở ApiControllerBase, không thay thế. Filter tự no-op nếu action không khai
    // [RequirePermission] (xem RequirePermissionFilter.cs). Yêu cầu AddPermissionInfrastructure()
    // đã đăng ký IPermissionChecker — gọi TRƯỚC dòng này (xem AddCoreModule ở dưới). Xem
    // doc/huong_dan/quy-uoc/be-api-controller.md §"Phân quyền theo hành động".
    .AddControllers(options => options.Filters.Add<RequirePermissionFilter>())
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

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

// Modular Monolith — mỗi module tự đăng ký MediatR/FluentValidation/EF configuration/repository
// cho riêng mình qua 1 extension method. Core TRƯỚC (module nghiệp vụ có thể cần role/user Core
// đã tồn tại lúc seed) — xem doc/kien-truc-core-module.md.
builder.Services.AddCoreModule(builder.Configuration);
builder.Services.AddDtiWeeklyModule(builder.Configuration);
// Thêm module nghiệp vụ mới: builder.Services.AddXxxModule(builder.Configuration);

// Permission-by-action (RolePermission) — TÁCH khỏi AddCoreModule có chủ đích (xem
// PermissionInfrastructureExtensions.cs). Chỉ đăng ký DI; filter được gắn vào pipeline MVC ở
// AddControllers() phía trên.
builder.Services.AddPermissionInfrastructure();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Hangfire — job nền cho Import CSV/Excel (StartImportCommand/IImportJobRunner), xem
// doc/huong_dan/quy-uoc/be-cqrs-handler.md §"Command chạy lâu → job nền". Dùng CHUNG connection
// string "Default" với PlatformManagerDbContext — Hangfire tự tạo schema "hangfire" lúc khởi
// động lần đầu (KHÔNG đi qua EF Core migration, xem 0004_role_permission_import_job.sql).
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(builder.Configuration.GetConnectionString("Default"))));
builder.Services.AddHangfireServer();

// Health check — liveness (process còn sống, không kiểm dependency) tách khỏi readiness (DB
// connect được) để DB chậm tạm thời không khiến orchestrator restart oan 1 app đang khoẻ. Xem
// doc/huong_dan/wiki-core/be/07-observability.md §"Liveness vs readiness".
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
    .AddDbContextCheck<PlatformManagerDbContext>(tags: ["ready"]);

// CORS: AllowCredentials() + origin cụ thể từ config — TUYỆT ĐỐI không AllowAnyOrigin() khi
// dùng cookie (cookie sẽ bị trình duyệt âm thầm bỏ qua). Xem .claude/rules/api-controller.md.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("Default", policy =>
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

// Rate limiting — chặn brute-force POST /api/auth/login (theo IP) + hạn mức nền cho MỌI request
// (theo IP, kể cả endpoint chưa khai gì) qua GlobalLimiter. Xem
// doc/huong_dan/quy-uoc/be-api-controller.md §"Rate limiting" +
// doc/huong_dan/wiki-core/be/09-security-beyond-auth.md.
//
// ⚠️ Phân vùng theo IP mất tác dụng khi chạy sau reverse proxy/load balancer — RemoteIpAddress
// khi đó là IP của proxy, cả hệ thống về lại MỘT phân vùng duy nhất. Hôm nay KHÔNG chạy sau
// reverse proxy nào ⇒ CHƯA cấu hình UseForwardedHeaders. Nếu triển khai sau proxy, PHẢI thêm
// UseForwardedHeaders với KnownProxies/KnownNetworks khai TƯỜNG MINH (không dùng
// ForwardedHeaders.All mặc định — bật mà không khai proxy tin cậy thì bất kỳ ai cũng giả mạo
// được X-Forwarded-For để tự chọn phân vùng, tệ hơn không bật).
const int LoginPermitLimitPerMinute = 5;
const int GlobalPermitLimitPerMinute = 100;

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
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
        IsExemptFromGlobalRateLimit(ctx)
            ? RateLimitPartition.GetNoLimiter("exempt")
            : RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: ResolveRateLimitPartitionKey(ctx),
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = GlobalPermitLimitPerMinute,
                    Window = TimeSpan.FromMinutes(1),
                }));

    // 429 mặc định của middleware trả body RỖNG (không đi qua envelope IApiResult) — bọc lại để
    // FE không phải xử lý riêng cho rate limit. Dùng CHUNG cho cả policy "login" lẫn GlobalLimiter.
    options.OnRejected = async (context, ct) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter =
                ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
        }

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
        var result = new ApiResult<object>
        {
            Status = ApiResultStatus.BUSINESS_ERROR,
            Code = ErrorCode.AuthenticationError,
            Message = "Chưa đăng nhập.",
            TraceId = context.HttpContext.TraceIdentifier,
        };
        return context.Response.WriteAsJsonAsync(result);
    };

    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/json";
        var result = new ApiResult<object>
        {
            Status = ApiResultStatus.BUSINESS_ERROR,
            Code = ErrorCode.AuthorizationError,
            Message = "Không có quyền truy cập.",
            TraceId = context.HttpContext.TraceIdentifier,
        };
        return context.Response.WriteAsJsonAsync(result);
    };
});

// CSRF — Lớp 2 (Lớp 1 là SameSite ở ConfigureApplicationCookie phía trên). Mô hình SPA (không
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

// ── Pipeline ─────────────────────────────────────────────────────────────
app.UseExceptionHandler(); // GlobalExceptionHandler — ValidationException -> 400+Fields, còn lại -> 500

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Default");

// PHẢI đứng sau UseCors (429 vẫn giữ header CORS, không hiện lỗi CORS mờ mịt phía trình duyệt)
// và TRƯỚC UseAuthentication (UseRouting tự chèn ở đầu pipeline nên endpoint đã phân giải xong —
// đặt sau UseAuthentication/UseAuthorization sẽ khiến request CHƯA đăng nhập bị cắt mạch 401
// trước khi chạm rate limiter, và GlobalLimiter chỉ còn bảo vệ lưu lượng ĐÃ đăng nhập — lỗ hổng đã
// đo thật 2026-08-21, xem PipelineOrderRateLimitTests).
app.UseRateLimiter();

app.UseAuthentication(); // PHẢI đứng TRƯỚC UseAuthorization (code cũ thiếu bước này)
app.UseAuthorization();

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

// Seed dữ liệu (DML — role/bootstrap-user/SysMenu/SysMenuRole/danh mục CSV) — CHỈ chạy ở
// Development. KHÔNG BAO GIỜ tự chạy migration/DDL (db.Database.MigrateAsync()) — schema
// áp dụng bằng cách người dùng tự chạy tay file .sql sinh từ `dotnet ef migrations script`,
// xem doc/ke-hoach-xay-lai-corebase.md. Bọc try/catch để app vẫn khởi động được (và vẫn trả
// 401/403 JSON đúng cho endpoint có [Authorize]) ngay cả khi schema CHƯA được áp dụng —
// tránh app crash cứng ngay từ đầu chỉ vì seed thất bại, dễ gây hiểu lầm "app lỗi" trong khi
// thực chất chỉ là "chưa chạy migration tay". CoreSeeder LUÔN chạy trước DtiWeeklySeeder (module
// có thể cần role/user Core đã tồn tại).
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var startupLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var coreSeeder = scope.ServiceProvider.GetRequiredService<CoreSeeder>();
        await coreSeeder.SeedAsync();

        var dtiWeeklySeeder = scope.ServiceProvider.GetRequiredService<DtiWeeklySeeder>();
        await dtiWeeklySeeder.SeedAsync();
    }
    catch (Exception ex)
    {
        startupLogger.LogWarning(
            ex,
            "Seed dữ liệu thất bại — có thể do CHƯA chạy tay file doc/ERD/migrations/0003_corebase_v2.sql " +
            "lên Postgres. App vẫn tiếp tục khởi động, nhưng endpoint cần DB sẽ lỗi cho tới khi schema sẵn sàng.");
    }
}

app.Run();

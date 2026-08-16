using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.Cookies;
using PlatformManager.Api.Common;
using PlatformManager.Core.Application.Common.Interfaces;
using PlatformManager.Core.Application.Common.Results;
using PlatformManager.Core.Infrastructure;
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
    .AddControllers()
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

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

var app = builder.Build();

// ── Pipeline ─────────────────────────────────────────────────────────────
app.UseExceptionHandler(); // GlobalExceptionHandler — ValidationException -> 400+Fields, còn lại -> 500

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Default");

app.UseAuthentication(); // PHẢI đứng TRƯỚC UseAuthorization (code cũ thiếu bước này)
app.UseAuthorization();

app.MapControllers();

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

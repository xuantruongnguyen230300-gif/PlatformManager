using Microsoft.EntityFrameworkCore;
using PlatformManager.Api.Common;
using PlatformManager.Api.Data;
using PlatformManager.Api.Services;

var builder = WebApplication.CreateBuilder(args);

const string CorsPolicyName = "FrontendDev";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:4200"];

// FE convention (src/FE/.claude/docs/api-client.md): request/response PascalCase, FLAT —
// tắt camelCase naming policy mặc định của System.Text.Json.
builder.Services.AddControllers().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.PropertyNamingPolicy = null;
    o.JsonSerializerOptions.DictionaryKeyPolicy = null;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddScoped<AssessmentUpsertService>();
builder.Services.AddScoped<AggregationService>();
builder.Services.AddScoped<CsvImportService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy =>
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

// Migrate + seed dữ liệu mẫu (6 CriteriaGroup + 62 Criteria) khi khởi động — demo local,
// KHÔNG áp dụng cho môi trường dùng chung (xem src/BE/CLAUDE.md § Maintenance Rules).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    await db.Database.MigrateAsync();
    await DbSeeder.SeedAsync(db, app.Environment.ContentRootPath, logger);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionMiddleware>();

app.UseCors(CorsPolicyName);

app.UseAuthorization();

app.MapControllers();

app.Run();

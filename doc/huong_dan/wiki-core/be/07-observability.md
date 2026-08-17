# 7. Quan sát hệ thống (Observability) — vượt ra ngoài logging

`TraceId` trong envelope response (đã có ở [01-core-components.md](01-core-components.md), #6) là bước đầu — nhưng để **thật sự tra được** "request này đi qua bao nhiêu module, chỗ nào chậm, chỗ nào lỗi" khi hệ thống lớn dần, cần thêm:

- **Health check endpoint** (`/health`) — không chỉ "app còn sống" mà kiểm tra được cả dependency (DB, cache, service ngoài) — dùng để biết sớm khi 1 phần hạ tầng có vấn đề, trước khi user báo lỗi.
- **Correlation ID xuyên suốt** — nếu có nhiều Process (xem [02-identity-auth.md](02-identity-auth.md)), `TraceId` phải được truyền qua header giữa các lời gọi HTTP nội bộ, để log của Process A và Process B cho cùng 1 request tra được chung 1 `TraceId`.
- **Metrics cơ bản** (số request/giây, latency p95/p99, tỉ lệ lỗi) — không cần hệ thống APM đắt tiền ngay, nhưng nên có ít nhất log định kỳ hoặc endpoint `/metrics` đơn giản để biết hệ thống đang khoẻ hay không **trước khi** có sự cố, không phải sau.

## Áp dụng vào PlatformManager

**Health check nên làm NGAY, không đợi "trước khi lên production"** — khác 2
mục còn lại (correlation ID xuyên Process, metrics) thật sự chưa cần khi còn
1 process. Chi phí gần bằng 0 (`Program.cs` hiện chưa có dòng `HealthCheck`
nào):

```csharp
builder.Services.AddHealthChecks()
    .AddDbContextCheck<PlatformManagerDbContext>();
// ...
app.MapHealthChecks("/health");
```

Lợi ích không phụ thuộc quy mô: biết ngay khi DB down thay vì đợi user báo
lỗi 500, và là điều kiện tiên quyết nếu sau này chạy Docker/K8s (liveness
probe cần endpoint này tồn tại từ trước, không phải thứ thêm vào lúc deploy
đầu tiên).

**Hangfire Dashboard (`/hangfire`) — giải quyết phần "xem lịch sử job chạy"
mà mục này từng để hoãn.** Kể từ khi chọn Hangfire cho pattern "job nền" (xem
[`src/BE/.claude/rules/cqrs-handler.md`](../../../../src/BE/.claude/rules/cqrs-handler.md)
§"Command chạy lâu → job nền"), Dashboard có sẵn miễn phí — không cần build
thêm UI theo dõi riêng. **Bắt buộc khoá quyền trước khi bật** — Dashboard mặc
định KHÔNG có auth, để mở nguyên là lộ toàn bộ job/data (kể cả nội dung
`ImportJob` đang chạy) ra ngoài:

```csharp
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new HangfireDashboardAuthFilter()],   // chỉ Roles.SuperAdmin, cùng mẫu PermissionsController
});
```

**Serilog — structured logging, nên làm sớm cùng đợt với health check** (rẻ,
ích ngay, khác nhóm "chờ đủ traffic mới cần" của metrics/correlation ID):

```csharp
builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day));
```

**Điều kiện bắt buộc, không phải tuỳ chọn:** enrich mỗi log entry bằng đúng
`HttpContext.TraceIdentifier` đang gán vào `IApiResult.TraceId`
(`ApiControllerBase.HandleResult`) — qua `LogContext.PushProperty("TraceId",
...)` trong 1 middleware đặt trước mọi middleware khác. Đây chính là giá trị
FE hiện cho user khi lỗi hệ thống (xem
[`fe/10-observability.md`](../../fe/10-observability.md) §"traceId — cầu nối
log FE ↔ log BE") — enrich sai/thiếu thì `traceId` user đưa cho support
**không tra được gì**, coi như tính năng chưa hoàn thành dù Serilog đã chạy.

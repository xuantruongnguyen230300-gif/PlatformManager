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
[`doc/huong_dan/quy-uoc/be-cqrs-handler.md`](../../quy-uoc/be-cqrs-handler.md)
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
[`fe/10-observability.md`](../fe/10-observability.md) §"traceId — cầu nối
log FE ↔ log BE") — enrich sai/thiếu thì `traceId` user đưa cho support
**không tra được gì**, coi như tính năng chưa hoàn thành dù Serilog đã chạy.

## Liveness vs readiness — 2 câu hỏi khác nhau, gộp chung 1 endpoint gây sự cố thật

> Bổ sung 2026-08-24, đối chiếu thực hành ngành cho hệ thống tầm trung: bullet
> "Health check endpoint" ở đầu file và đoạn code `/health` ở "Áp dụng vào
> PlatformManager" phía trên đều mô tả **một** endpoint duy nhất vừa trả lời
> "app còn sống" vừa kiểm dependency (DB). Đây đúng là điểm cần tách — gộp
> chung 2 việc này là lỗi thiết kế phổ biến, không phải chi tiết vặt.

**Vì sao đây là vấn đề THẬT.** `AddDbContextCheck<PlatformManagerDbContext>()`
mapped vào `/health` nghĩa là: DB chậm tạm thời (spike tải, migration đang
chạy, network hiccup) → `/health` trả Unhealthy. Nếu sau này endpoint này
được dùng làm **liveness probe** (Docker/K8s dùng nó để quyết định "có restart
container không"), orchestrator sẽ **restart một app đang chạy khoẻ mạnh**
chỉ vì DB chậm vài giây — mất đúng lúc tệ nhất (DB đang tải cao, restart app
dồn thêm request retry vào lúc DB cần thở). App bị giết không phải vì app
hỏng, mà vì nó trung thực báo cáo một dependency khác đang chậm.

**Phân biệt bắt buộc:**

| Loại | Trả lời câu hỏi | Kiểm gì | Dùng làm gì |
| --- | --- | --- | --- |
| **Liveness** | Process còn sống hay đã treo/deadlock? | Không kiểm dependency ngoài — chỉ app tự trả lời được | Orchestrator dùng để quyết định **restart** |
| **Readiness** | App đã sẵn sàng nhận traffic chưa? | DB connect được, dependency ngoài OK | Load balancer dùng để quyết định **route traffic tới hay không** |

```csharp
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
    .AddDbContextCheck<PlatformManagerDbContext>(tags: ["ready"]);

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live"),   // KHÔNG kiểm DB
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),  // kiểm đủ dependency
});
```

DB chậm giờ chỉ làm `/health/ready` chuyển Unhealthy (load balancer ngưng gửi
traffic mới tới instance đó — đúng hành vi mong muốn), còn `/health/live`
vẫn Healthy (process không bị restart oan).

**Áp dụng vào PlatformManager:** hôm nay 1 process, chưa chạy sau
orchestrator nào tự động restart theo health check — nên việc tách 2 endpoint
**không khẩn cấp** như bản thân mục "Health check nên làm NGAY" ở trên. Nhưng
chi phí tách gần bằng 0 (thêm 1 tag + 1 dòng `MapHealthChecks`), trong khi chi
phí *không* tách chỉ lộ ra đúng lúc đưa hệ thống vào Docker/K8s lần đầu — làm
cùng đợt với health check ban đầu rẻ hơn nhiều so với sửa lại sau khi đã có
restart loop chạy thật trong production.

## Structured logging — bộ field bắt buộc, không chỉ `TraceId`

> Bổ sung 2026-08-24: "Điều kiện bắt buộc" ở trên đã bắt enrich đúng **1**
> field (`TraceId`) — cần thiết nhưng không đủ. Structured logging chỉ phát
> huy giá trị "tra được qua công cụ" khi MỌI log entry mang cùng bộ field,
> không phải khi từng chỗ log tự chọn field khác nhau.

**Vì sao đây là vấn đề THẬT.** `LogContext.PushProperty` rải rác theo từng
middleware/handler cho ra log tra được — chỉ khi người viết code nhớ đúng
tên field mỗi lần. `UserId` viết `userId` ở chỗ này, `UserID` ở chỗ khác,
`AccountId` ở chỗ thứ ba — 3 tên cho cùng 1 khái niệm, và câu query "tìm mọi
log của user X" không gộp được cả 3. Đây là lỗi rất dễ xảy ra khi mỗi PR tự
thêm 1 dòng log theo cảm tính, không theo danh sách field đã chốt.

**Bộ field bắt buộc**, enrich cùng 1 chỗ với `TraceId` (middleware đặt trước
mọi middleware khác):

```csharp
app.Use(async (ctx, next) =>
{
    using (LogContext.PushProperty("TraceId", ctx.TraceIdentifier))
    using (LogContext.PushProperty("UserId", ctx.User?.FindFirstValue(ClaimTypes.NameIdentifier)))
    using (LogContext.PushProperty("RequestPath", ctx.Request.Path.Value))
    {
        await next();
    }
});
```

- `UserId` — `null` hợp lệ cho request chưa đăng nhập (`/login`, `/health`);
  đừng ép giá trị giả.
- `Timestamp` (UTC) — Serilog tự thêm sẵn, không cần enrich tay, nhưng kiểm
  đồng hồ server chạy UTC — log trộn giờ local và UTC là nguồn nhầm lẫn kinh
  điển khi so log của 2 nguồn khác nhau lúc điều tra sự cố lúc nửa đêm.
- `Environment` (`Development`/`Production`) — phân biệt log test và log
  thật khi dùng chung 1 sink, dễ xảy ra ở team nhỏ chưa tách hạ tầng log
  theo môi trường.

**Cấm log dữ liệu nhạy cảm — không chỉ ở payload request.**
[trien-khai/03-p2-platform-application.md](trien-khai/03-p2-platform-application.md)
§"`LoggingBehavior` — chi tiết nhỏ đáng sao chép" đã cấm log payload trong
pipeline MediatR ("không log payload để tránh rò rỉ PII") — đúng nhưng chỉ
chặn **một** cửa vào. Còn ít nhất 2 cửa khác cùng ghi vào hệ thống log tập
trung này chưa ai chặn:

1. **`Exception.Message`/`ToString()` vô tình mang dữ liệu nhạy cảm.** Một
   exception từ tầng đổi mật khẩu, hoặc từ Npgsql khi connection string sai,
   có thể mang theo giá trị input người dùng vừa gõ hoặc chuỗi kết nối.
   `logger.LogError(ex, "...")` mặc định log **toàn bộ** `ex.ToString()`.
2. **Middleware log request/response tự viết tay** (hay được thêm để "debug
   cho dễ") — log nguyên `Request.Body` cho `POST /api/auth/login` nghĩa là
   **password dạng plaintext** nằm trong file log, ngoài tầm kiểm soát quyền
   của bất kỳ ai đọc log (khác DB, nơi quyền truy cập kiểm soát được).

Quy tắc cho cả 2: danh sách field cấm log rõ ràng — `Password`,
`PasswordHash`, header `Authorization`, header `Cookie`, mọi dạng token. Cần
log request để debug thì log **tên field + độ dài chuỗi**, không log **giá
trị**.

## Alerting — log không ai xem là vô dụng đúng lúc cần nhất

> Bổ sung 2026-08-24: "Metrics cơ bản" ở đầu file liệt kê đúng 3 con số nên đo
> (request/giây, latency p95/p99, tỉ lệ lỗi) nhưng dừng ở "nên có" — chưa nói
> khi nào 3 con số đó bị coi là bất thường cần báo động ai đó. Vế FE tương ứng
> đã ghi nhận đúng vấn đề này ở
> [fe/10-observability.md](../fe/10-observability.md) §"Đã tới ngưỡng" ("phát
> hiện khi user tự báo — chậm hơn nhiều so với alert tự động"); vế BE ở đây
> trước đó chưa có.

**Vì sao đây là vấn đề THẬT.** Team 5-15 người không có ai trực 24/7 nhìn
dashboard. Tỉ lệ lỗi 500 tăng vọt lúc 2 giờ sáng mà không có cơ chế chủ động
báo thì sự cố chỉ lộ ra khi user đầu tiên báo lại (chậm nhiều giờ), hoặc khi
hậu quả đủ lớn để tự lộ ra (dữ liệu sai tích luỹ, job Hangfire dừng âm thầm).
File log chi tiết trong `logs/log-.txt` không đổi được điều đó nếu không ai
chủ động mở ra đọc.

**Ngưỡng khởi điểm — sai lúc đầu là bình thường, quan trọng là có ngưỡng để
chỉnh:**

| Chỉ số | Ngưỡng gợi ý | Kênh báo |
| --- | --- | --- |
| Tỉ lệ response 5xx | > 5% request trong 5 phút | Webhook Telegram/Slack tới nhóm dev |
| Latency p99 | > 2000ms liên tục 5 phút | Cùng kênh |
| Job Hangfire lỗi liên tiếp | ≥ 3 lần liên tiếp cùng 1 job | Cùng kênh — job nền lỗi âm thầm không ai gọi API để nhận thấy |
| `/health/ready` | Unhealthy liên tục > 1 phút | Cùng kênh, ưu tiên cao nhất — nghĩa là app đang không phục vụ được |

**Cách làm rẻ nhất cho quy mô 5-15 dev** — chưa cần hệ thống alerting chuyên
dụng (PagerDuty, Alertmanager): sink Serilog gửi thẳng entry mức `Error` trở
lên tới webhook Telegram/Slack. Nâng cấp lên hệ thống alerting thật khi đội
đủ lớn để cần phân loại mức độ nghiêm trọng/escalation, không phải trước đó.

## Nơi log tra được qua công cụ — chọn theo quy mô, không mặc định chọn cái mạnh nhất

> Bổ sung 2026-08-24: mục Serilog ở trên dừng ở `WriteTo.Console()` +
> `WriteTo.File(...)` — đúng cho local dev, nhưng file log rải trên từng máy
> chủ không tra cứu được qua công cụ, đúng thứ mà bộ field ở mục trên cần một
> nơi để tận dụng.

**Vì sao đây là vấn đề THẬT.** `grep traceId` trên đúng 1 file log còn chịu
được; ngay khi có ≥2 instance (kể cả chỉ vì zero-downtime deploy, chưa cần
"scale-out" thật), log nằm rải trên nhiều máy, và SSH vào từng máy để `grep`
là cách chậm nhất có thể tra cứu lúc sự cố đang diễn ra.

| Lựa chọn | Phù hợp khi | Chi phí vận hành |
| --- | --- | --- |
| **Seq** (self-host, free tier đủ cho 1 team) | Team 5-15 dev, đã dùng Serilog sẵn, muốn tự host, không muốn trả phí SaaS | Thấp — 1 container Docker, không cần người chuyên vận hành |
| **Application Insights** (Azure) | Hạ tầng đã chạy trên Azure sẵn | Rất thấp — `Serilog.Sinks.ApplicationInsights`, không tự host gì |
| Grafana + Prometheus + Loki tự host | Team đã có người quen vận hành observability stack | Cao — 3 thành phần tự vận hành, patch, backup |

**Gợi ý cho quy mô này:** `Serilog.Sinks.Seq` — đổi 1 dòng so với
`WriteTo.File` đang có, tra được qua UI, filter theo `TraceId`/`UserId` (bộ
field đã chuẩn hoá ở mục trên) mà không cần `grep` tay:

```csharp
builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .WriteTo.Seq(ctx.Configuration["Seq:ServerUrl"] ?? "http://localhost:5341"));
```

Không cần dựng Prometheus riêng cho metrics ở quy mô này — Seq nhận cả
structured log lẫn phục vụ được truy vấn kiểu "đếm request theo endpoint
trong 1 giờ", đủ cho nhu cầu "biết hệ thống có khoẻ không" mà không phải vận
hành thêm 1 hạ tầng metric riêng.

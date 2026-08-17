# API Controller & Envelope — src/BE

## Controller — mỏng, chỉ gửi tới MediatR

```csharp
[ApiController]
[Route("api/[controller]")]
public class CriteriaController(ISender mediator) : ApiControllerBase
{
    [HttpPost("list")]
    public async Task<IActionResult> List([FromBody] GetCriteriaListQuery query, CancellationToken ct)
        => HandleResult(await mediator.Send(query, ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => HandleResult(await mediator.Send(new GetCriteriaByIdQuery(id), ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCriteriaCommand cmd, CancellationToken ct)
        => HandleResult(await mediator.Send(cmd, ct));
}
```

- Action nhận request **phẳng** trực tiếp làm Command/Query (khi shape khớp)
  hoặc một Request DTO riêng rồi tự dựng Command — **không bao giờ** bọc body
  dạng `{ "Request": {...} }`.
- Controller **không chứa logic nghiệp vụ** — chỉ gọi `mediator.Send` và map
  kết quả qua `HandleResult` (base method, xem §Dispatcher bên dưới).
- `List` dùng `POST` (không `GET`) khi body cần mang filter/sort phức tạp;
  `GetById`/lookup đơn giản dùng `GET`.

## Envelope response — nhất quán cho MỌI endpoint

**Đã CHỐT (2026-08-15):** theo
`doc/huong_dan/wiki-core/be/trien-khai/03-p2-platform-application.md §4`,
thay cho `ApiResponse<T>` 5-field trước đây — envelope giàu hơn để sẵn chỗ
cho lỗi theo từng field, mã lỗi nghiệp vụ ổn định, và retry, thay vì phải
đổi shape khi cần đến (đúng nhu cầu "mở rộng theo khách hàng" của
PlatformManager).

```csharp
[JsonConverter(typeof(JsonStringEnumConverter<ErrorCode>))]
public enum ErrorCode
{
    Success             = 0,
    ValidationError     = 400,   // input không hợp lệ
    AuthenticationError = 401,   // token thiếu/sai
    AuthorizationError  = 403,   // đã đăng nhập nhưng thiếu quyền
    NotFound            = 404,   // resource chính của route không tồn tại
    Conflict            = 409,   // unique / concurrency
    BusinessRuleError   = 422,   // vi phạm quy tắc nghiệp vụ
    SystemError         = 500,
}

[JsonConverter(typeof(JsonStringEnumConverter<ApiResultStatus>))]
public enum ApiResultStatus { SUCCESS, VALIDATION_ERROR, BUSINESS_ERROR, SYSTEM_ERROR }
```

Giá trị `ErrorCode` **chính là** mã HTTP — không có bảng map thứ hai để lệch
(xem §Error → HTTP status mapping). On-wire là *tên* member
(`"code": "BusinessRuleError"`), không phải số — số đã có sẵn ở HTTP status
line.

```csharp
public static class ApiErrorIds
{
    // Nguồn DUY NHẤT map ErrorCode → ApiResultStatus — Status không bao giờ gán tay ở nơi khác
    public static ApiResultStatus StatusForCode(ErrorCode code) => code switch
    {
        ErrorCode.ValidationError => ApiResultStatus.VALIDATION_ERROR,
        ErrorCode.SystemError     => ApiResultStatus.SYSTEM_ERROR,
        _                         => ApiResultStatus.BUSINESS_ERROR,
    };
}

public interface IHasApiResultStatus { ApiResultStatus Status { get; } ErrorCode Code { get; } }

public interface IApiResult<T> : IHasApiResultStatus
{
    T? Data { get; }
    string? Message { get; }
    string? BusinessCode { get; }             // "{ENTITY}.{ERROR}" — nguồn ở ErrorDescriptor (cqrs-handler.md)
    string? TraceId { get; set; }
    bool? Retryable { get; }
    Dictionary<string, string[]>? Fields { get; }   // lỗi validate theo field — key = PascalCase khớp JSON đã serialize
}

public class ApiResult<T> : IApiResult<T>
{
    public T? Data { get; init; }
    public string? Message { get; init; }
    public ApiResultStatus Status { get; init; }
    public ErrorCode Code { get; init; }
    public string? BusinessCode { get; init; }
    public string? TraceId { get; set; }
    public bool? Retryable { get; init; }
    public Dictionary<string, string[]>? Fields { get; init; }

    public static ApiResult<T> Success(T? data, string? message = null)
        => new() { Status = ApiResultStatus.SUCCESS, Code = ErrorCode.Success, Data = data, Message = message };

    public static ApiResult<T> BusinessError(ErrorDescriptor error, string message) => new()
    {
        Status = ApiErrorIds.StatusForCode(error.ErrorCode), Code = error.ErrorCode,
        BusinessCode = error.BusinessCode, Message = message, Retryable = error.Retryable,
    };
}
```

**[ĐƠN GIẢN HOÁ] áp dụng cho PlatformManager** (theo
`doc/huong_dan/wiki-core/be/trien-khai/03-p2-platform-application.md §4.7`):
chỉ dùng `System.Text.Json` — bỏ hẳn bộ `ShouldSerialize*()`/dual-serializer
của bản gốc (dành cho hệ có cả Newtonsoft lẫn STJ, PlatformManager chỉ có 1
serializer). Cũng gộp `LogId` chung vào `TraceId` (không tách riêng) trừ khi
sau này cần phân biệt mã tra log nội bộ khỏi mã trace phân tán; và bỏ
`IApiResultEnrichable` riêng — `TraceId` set thẳng trong `IApiResult<T>`.

**Quyết định có chủ đích:** endpoint list/grid trả **cùng envelope**
(`IApiResult<PagedList<T>>`), không trả `PagedList<T>` trần. Lý do: tránh
FE phải viết 2 nhánh parse khác nhau tuỳ endpoint — một nguồn lỗi hay gặp
khi tách envelope theo loại endpoint.

`ErrorDescriptor` (nguồn của `BusinessCode`/`Message`/`Retryable` — khai cạnh
handler, không rải string literal) xem `cqrs-handler.md` §ErrorDescriptor.

## Error → HTTP status mapping

`ErrorCode` **là** mã HTTP (xem enum ở trên) — không có bảng tra thứ 2. Chọn
`ErrorCode` nào cho 1 lỗi cụ thể, theo đúng phân biệt của
`doc/huong_dan/wiki-core/be/trien-khai/06-p5-module-dau-tien.md §4`:

| `ErrorCode` | HTTP | Dùng khi |
| --- | ---: | --- |
| `ValidationError` | 400 | Tính được trọn vẹn từ payload, không cần DB — luôn kèm `Fields` |
| `NotFound` | 404 | Không tìm thấy **resource chính của route** (`{id}`) — kể cả đã soft-delete (không tách riêng 2 mã, tránh lộ thông tin tồn tại của dữ liệu đã xoá) |
| `BusinessRuleError` | 422 | FK/tham chiếu nằm **trong payload** (không phải resource của route), cần đọc DB để biết hợp lệ |
| `Conflict` | 409 | Xung đột **trạng thái của chính resource** — trùng giá trị unique, hoặc đang bị ràng buộc không cho thao tác |
| `AuthorizationError` | 403 | Đã đăng nhập nhưng thiếu quyền — **không** map về `BusinessRuleError`, FE cần phân biệt được để điều hướng sang màn xin quyền |
| (exception không mong đợi, qua middleware) | 500 | Bug/hạ tầng — không lộ chi tiết ra response |

## Dispatcher — `HandleResult`, mỏng, 1 chỗ map HTTP

```csharp
public abstract class ApiControllerBase : ControllerBase
{
    protected IActionResult HandleResult<T>(IApiResult<T> result)
    {
        result.TraceId ??= HttpContext.TraceIdentifier;
        var status = result.Code == ErrorCode.Success ? StatusCodes.Status200OK : (int)result.Code;
        return StatusCode(status, result);
    }
}
```

Mọi controller kế thừa `ApiControllerBase` thay vì `ControllerBase` trực
tiếp — **không** controller nào tự `try-catch`/tự hardcode status code
(`return StatusCode(500, ...)` rải rác tạo nguồn sự thật thứ 2 cho mapping
`ErrorCode → HTTP`, cấm).

## Exception-handling middleware toàn cục

Bắt 2 loại lỗi không đi qua `HandleResult`: exception **không mong đợi**
(bug, lỗi kết nối DB) và `FluentValidation.ValidationException` (validator
fail trước khi vào handler, xem `cqrs-handler.md` §Validator) — dịch cả hai
thành đúng `IApiResult` envelope ở trên, không lộ stack trace:

```csharp
app.UseExceptionHandler(a => a.Run(async ctx =>
{
    var traceId = ctx.TraceIdentifier;
    var exception = ctx.Features.Get<IExceptionHandlerFeature>()?.Error;
    // log exception đầy đủ ở đây (logger, không log ra response)

    var result = exception switch
    {
        FluentValidation.ValidationException vex => new ApiResult<object>
        {
            Status = ApiResultStatus.VALIDATION_ERROR,
            Code = ErrorCode.ValidationError,
            Message = "Dữ liệu không hợp lệ.",
            TraceId = traceId,
            Fields = vex.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()),
        },
        _ => new ApiResult<object>
        {
            Status = ApiResultStatus.SYSTEM_ERROR, Code = ErrorCode.SystemError,
            Message = "Đã có lỗi xảy ra.", TraceId = traceId,
        },
    };

    ctx.Response.StatusCode = (int)result.Code;
    await ctx.Response.WriteAsJsonAsync(result);
}));
```

**Không bao giờ** trả stack trace hay chi tiết exception nội bộ ra response
— chỉ `BusinessCode`/`ErrorCode` chung + `TraceId` để tra log phía server.

## CORS

Origin cho phép = đúng nơi `src/FE` chạy dev
(`http://localhost:4200` theo mặc định Angular CLI). Không mở `AllowAnyOrigin()`
một khi đã có auth thật (cookie/token) — CORS mở rộng cùng lúc với auth là
lỗ hổng bảo mật phổ biến.

## Auth/Permission

**Đã CHỐT: ASP.NET Core Identity.**

- Entity người dùng: `AppUser : IdentityUser<Guid>` (Infrastructure/Identity
  hoặc Domain tuỳ cách tổ chức — quyết định cụ thể khi scaffold; lưu ý
  `IdentityUser` gắn với hạ tầng Identity nên **không** đặt trong
  `Domain/Entities` cùng chỗ với entity nghiệp vụ thuần như `Criteria`).
- `DbContext` kế thừa `IdentityDbContext<AppUser, AppRole, Guid>` (hoặc
  `IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>` nếu chưa cần role
  tuỳ biến) — các bảng chuẩn (`AspNetUsers`, `AspNetRoles`,
  `AspNetUserRoles`, `AspNetUserClaims`, `AspNetUserLogins`,
  `AspNetUserTokens`, `AspNetRoleClaims`) tự sinh qua migration, **không**
  tự vẽ/tự tạo tay các bảng này.
- FK nghiệp vụ trỏ tới người dùng (vd `CriteriaAssessment.OwnerId`) tham
  chiếu `AppUser.Id` — xem `doc/ERD/PlatformManager.dbml` bảng `AppUsers`.
- Cơ chế cấp token cho SPA (Angular ở `src/FE`): **chưa chốt kiểu cụ thể**
  (cookie session của Identity vs JWT bearer phát hành riêng) — đây vẫn là
  quyết định cần hỏi người dùng trước khi implement endpoint đăng nhập đầu
  tiên, vì ảnh hưởng trực tiếp tới cách `frontend-expert` lưu/gửi
  credential. Chỉ riêng "dùng Identity làm nguồn user/role" là đã chốt.
- Vai trò/permission chi tiết theo từng tính năng (vd ai được sửa `Status`
  của `CriteriaAssessment`) — xem
  `spec/dashboard-dti-weekly/business-rules.md` mục Permission; nếu chưa đủ
  thông tin nghiệp vụ để chốt role, giữ nguyên placeholder trong spec đó,
  không tự bịa role ở tầng code.

### Phân quyền theo hành động — bản rút gọn (khác role, khác menu)

**Vấn đề thật đã tìm thấy (2026-08-17):** màn "Phân quyền" hiện chỉ điều khiển
`SysMenuRole` — tức "role nào thấy menu nào". Không có gì chặn 1 user role
`User` gọi thẳng `POST /api/criteria` dù matrix không cấp quyền — mọi
controller nghiệp vụ chỉ có `[Authorize]` trần (đăng nhập là đủ). Đây là gap
thật, không phải rủi ro lý thuyết.

**Bản rút gọn áp dụng ngay** — KHÔNG phải hệ thống đầy đủ ở
[05-p4-hosting-api.md §7](../../../../doc/huong_dan/wiki-core/be/trien-khai/05-p4-hosting-api.md)
(cái đó có `CrudActionResolver` suy luận action tự động + 2 cơ chế enforcement
song song — quá nặng cho 1 module hiện tại):

```csharp
// Platform.Domain hoặc Core.Application — pure metadata, không logic runtime
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequirePermissionAttribute(string key) : Attribute
{
    public string Key { get; } = key;   // vd "criteria.manage", khớp key trong PermissionMatrix
}

// 1 filter toàn cục, đọc thẳng permission hiện có của user (claim/role → key)
public class RequirePermissionFilter : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext ctx)
    {
        var attr = ctx.ActionDescriptor.EndpointMetadata
            .OfType<RequirePermissionAttribute>().FirstOrDefault();
        if (attr is null) return;   // không khai attribute = không chặn thêm (giữ nguyên [Authorize])

        var hasPermission = /* đọc PermissionMatrix theo role của user hiện tại */;
        if (!hasPermission) ctx.Result = new ForbidResult();
    }
}
```

- Không có `CrudActionResolver` — action luôn khai tường minh trong `key`
  (vd `"criteria.manage"` dùng chung cho create/update/delete), không suy từ
  tên method. Chấp nhận thô hơn (không phân biệt View/Create/Delete) để đổi
  lấy đơn giản — nâng cấp lên action rời (`PermissionAction`) khi thật sự cần
  phân biệt "xem được nhưng không sửa được".
- Chỉ 1 cơ chế enforcement (filter), không thêm `[Authorize(Policy=...)]`
  song song.
- Áp dụng cho endpoint ghi dữ liệu nghiệp vụ trước (Criteria/CriteriaGroups/
  Import) — endpoint chỉ đọc (Dashboard) có thể giữ `[Authorize]` trần thêm
  một thời gian nếu chưa có yêu cầu nghiệp vụ phân biệt ai xem được gì.

## Rate limiting

**Bổ sung khi chuyển sang giai đoạn product (2026-08-17)** — đối chiếu 12-Factor/
chuẩn ngành, không nằm trong lần rà soát VNR gốc. Chưa có gì chặn brute-force
`POST /api/auth/login`, hay 1 user spam `POST /api/import` (endpoint nặng —
xem `cqrs-handler.md` §"Command chạy lâu") làm nghẽn worker cho user khác.

.NET có middleware dựng sẵn từ .NET 7, không cần thư viện ngoài:

```csharp
// Program.cs
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Policy riêng cho login — chặt hơn nhiều so với API thường (chống brute-force)
    options.AddFixedWindowLimiter("login", opt =>
    {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromMinutes(1);
    });

    // Policy mặc định cho API còn lại — rộng hơn, chỉ chặn spam rõ ràng
    options.AddFixedWindowLimiter("default", opt =>
    {
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromMinutes(1);
    });
});

app.UseRateLimiter();
```

```csharp
[HttpPost("login")]
[EnableRateLimiting("login")]   // gắn riêng action nhạy cảm — không dùng chung policy "default"
public async Task<IActionResult> Login([FromBody] LoginCommand cmd, CancellationToken ct) => ...
```

- Policy `login` **theo IP** (mặc định của `FixedWindowLimiter` — partition
  key là remote IP) — không theo user (chưa đăng nhập thì chưa có user).
- `ImportController` dùng policy `default` là đủ ở quy mô hiện tại — endpoint
  đã tự giới hạn 20MB/file + chạy nền qua Hangfire (không block worker),
  rate limit ở đây chỉ chặn spam request tạo job, không phải chặn xử lý file
  lớn (đã có Hangfire lo).
- **Không** rate-limit health check (`/health`) — orchestrator/monitoring
  cần gọi endpoint này thường xuyên, rate limit vào đây gây báo động giả.

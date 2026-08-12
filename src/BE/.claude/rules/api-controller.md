# API Controller & Envelope — src/BE

## Controller — mỏng, chỉ gửi tới MediatR

```csharp
[ApiController]
[Route("api/[controller]")]
public class CriteriaController(ISender mediator) : ControllerBase
{
    [HttpPost("list")]
    public async Task<IActionResult> List([FromBody] GetCriteriaListQuery query, CancellationToken ct)
        => (await mediator.Send(query, ct)).ToApiResponse();

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => (await mediator.Send(new GetCriteriaByIdQuery(id), ct)).ToApiResponse();

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCriteriaCommand cmd, CancellationToken ct)
        => (await mediator.Send(cmd, ct)).ToApiResponse();
}
```

- Action nhận request **phẳng** trực tiếp làm Command/Query (khi shape khớp)
  hoặc một Request DTO riêng rồi tự dựng Command — **không bao giờ** bọc body
  dạng `{ "Request": {...} }`.
- Controller **không chứa logic nghiệp vụ** — chỉ gọi `mediator.Send` và map
  kết quả.
- `List` dùng `POST` (không `GET`) khi body cần mang filter/sort phức tạp;
  `GetById`/lookup đơn giản dùng `GET`.

## Envelope response — nhất quán cho MỌI endpoint

```csharp
public class ApiResponse<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public string? TraceId { get; init; }
}
```

```csharp
public static class ResultExtensions
{
    public static IActionResult ToApiResponse<T>(this Result<T> result)
    {
        var traceId = Activity.Current?.Id;
        if (result.IsSuccess)
            return new OkObjectResult(new ApiResponse<T> { Success = true, Data = result.Value, TraceId = traceId });

        var response = new ApiResponse<T>
        {
            Success = false, ErrorCode = result.ErrorCode, ErrorMessage = result.ErrorMessage, TraceId = traceId,
        };
        return result.ErrorType switch
        {
            ResultErrorType.NotFound => new NotFoundObjectResult(response),
            ResultErrorType.Conflict => new ConflictObjectResult(response),
            ResultErrorType.Validation => new BadRequestObjectResult(response),
            ResultErrorType.Forbidden => new ObjectResult(response) { StatusCode = 403 },
            _ => new ObjectResult(response) { StatusCode = 500 },
        };
    }
}
```

**Quyết định có chủ đích:** endpoint list/grid trả **cùng envelope**
(`ApiResponse<PagedList<T>>`), không trả `PagedList<T>` trần. Lý do: tránh
FE phải viết 2 nhánh parse khác nhau tuỳ endpoint — một nguồn lỗi hay gặp
khi tách envelope theo loại endpoint.

## Error → HTTP status mapping

| `ResultErrorType` | HTTP |
| --- | ---: |
| `NotFound` | 404 |
| `Conflict` | 409 |
| `Validation` | 400 |
| `Forbidden` | 403 |
| (exception không mong đợi, qua middleware) | 500 |

## Exception-handling middleware toàn cục

```csharp
app.UseExceptionHandler(a => a.Run(async ctx =>
{
    var traceId = ctx.TraceIdentifier;
    // log exception đầy đủ ở đây (logger, không log ra response)
    ctx.Response.StatusCode = 500;
    await ctx.Response.WriteAsJsonAsync(new ApiResponse<object>
    {
        Success = false, ErrorCode = "INTERNAL_ERROR",
        ErrorMessage = "Đã có lỗi xảy ra.", TraceId = traceId,
    });
}));
```

**Không bao giờ** trả stack trace hay chi tiết exception nội bộ ra response
— chỉ `ErrorCode` chung + `TraceId` để tra log phía server.

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

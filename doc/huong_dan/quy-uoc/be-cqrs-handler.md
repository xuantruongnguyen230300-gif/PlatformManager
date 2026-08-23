# CQRS & Handler — src/BE

## Command / Query qua MediatR

**Đã CHỐT (2026-08-15):** envelope trả về là `IApiResult<T>` (xem
`api-controller.md` §Envelope response), **không phải** `Result<T>` tự chế —
theo `doc/huong_dan/wiki-core/be/trien-khai/03-p2-platform-application.md`.

```csharp
public record CreateCriteriaCommand(string Code, string Name, string Group, decimal MaxScore)
    : IRequest<IApiResult<Guid>>;

public record GetCriteriaByIdQuery(Guid Id) : IRequest<IApiResult<CriteriaDto>>;

public record GetCriteriaListQuery(int Page = 1, int PageSize = 20, string? SearchText = null)
    : IRequest<IApiResult<PagedList<CriteriaDto>>>;
```

- Đặt tên query danh sách: `Get{Entity}sListQuery` (số nhiều trước `List`).
- Command/Query là `record` bất biến — không class có setter.

## Handler — 4 bước, handler own `SaveChanges`

```csharp
public class CreateCriteriaHandler(ICriteriaRepository repo, IUnitOfWork uow)
    : BaseResponse, IRequestHandler<CreateCriteriaCommand, IApiResult<Guid>>
{
    public async Task<IApiResult<Guid>> Handle(CreateCriteriaCommand cmd, CancellationToken ct)
    {
        // 1. Business rule cần DB (uniqueness) → trả Fail qua ErrorDescriptor, không throw
        if (await repo.CodeExistsAsync(cmd.Code, ct))
            return Fail<Guid>(CriteriaErrors.DuplicateCode, cmd.Code);

        // 2. Dựng entity qua domain factory — không new + gán property
        var entity = Criteria.Create(cmd.Code, cmd.Name, cmd.Group, cmd.MaxScore);

        // 3. Persist — handler own SaveChanges, repository KHÔNG tự commit
        await repo.AddAsync(entity, ct);
        await uow.SaveChangesAsync(ct);

        // 4. Trả kết quả
        return Ok(entity.Id);
    }
}
```

**Handler không cần:**
- `try-catch` cho lỗi hạ tầng bất ngờ — exception-handling middleware toàn
  cục lo việc đó.
- Validate format input (Required, MaxLength, regex) — `Validator`
  (FluentValidation) chạy trước handler qua MediatR pipeline behavior.

**Handler cần tự làm:**
- Validation cần DB (uniqueness, FK tồn tại) — vì `Validator` không có
  quyền truy cập DB theo convention ở đây (giữ validator thuần, dễ test).
- Business rule/state check (vd. "không sửa được khi đã duyệt").

## Validator (FluentValidation)

```csharp
public class CreateCriteriaValidator : AbstractValidator<CreateCriteriaCommand>
{
    public CreateCriteriaValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(500);
        RuleFor(x => x.MaxScore).GreaterThan(0);
    }
}
```

Đăng ký pipeline behavior chạy validator trước handler (MediatR
`IPipelineBehavior<TRequest, TResponse>`) — 1 lần cấu hình chung cho toàn bộ
Application, không lặp lại try/validate thủ công trong từng handler. Validator
**ném** `FluentValidation.ValidationException` khi fail — middleware toàn cục
(`api-controller.md` §Exception-handling) dịch nó thành `IApiResult` lỗi
`ErrorCode.ValidationError` (400) kèm `Fields`; validator **không** tự dựng
response.

## `ErrorDescriptor` — thay cho magic string, nguồn lỗi nghiệp vụ mong đợi

```csharp
public sealed record ErrorDescriptor(
    string BusinessCode,       // "{ENTITY}.{ERROR}" — UPPER_SNAKE có chấm
    ErrorCode ErrorCode,       // xem api-controller.md — giá trị enum = mã HTTP
    string MessageTemplate,    // "{0}", "{1}" placeholder cho string.Format
    bool Retryable = false);
```

Khai báo tập trung cạnh handler, **không** rải rác string literal trong code:

```csharp
// PlatformManager.Modules.DtiWeekly.Application/Criteria/CriteriaErrors.cs
public static class CriteriaErrors
{
    public static readonly ErrorDescriptor NotFound = new(
        "CRITERIA.NOT_FOUND", ErrorCode.NotFound, "Không tìm thấy chỉ tiêu.");

    public static readonly ErrorDescriptor DuplicateCode = new(
        "CRITERIA.DUPLICATE_CODE", ErrorCode.Conflict, "Mã chỉ tiêu '{0}' đã tồn tại.");
}
```

## `BaseResponse` — API mà handler thật sự dùng

Handler kế thừa `BaseResponse` và chỉ dùng 2 động từ — `Ok`/`Fail` — không tự
dựng `ApiResult<T>` bằng tay ở từng handler:

```csharp
public abstract class BaseResponse
{
    protected IApiResult<T> Ok<T>(T data, string? message = null)
        => ApiResult<T>.Success(data, message);

    protected IApiResult<T> Fail<T>(ErrorDescriptor error, params object[] args)
        => ApiResult<T>.BusinessError(error, string.Format(error.MessageTemplate, args));
}
```

**Quy tắc:**
- Lỗi nghiệp vụ **mong đợi** (not found, trùng code, vi phạm business rule)
  → `Fail<T>(descriptor, args)`, **không throw**.
- Lỗi **không mong đợi** (bug, lỗi kết nối DB) → để exception bay lên,
  middleware toàn cục bắt (`api-controller.md` §Exception-handling), trả
  `500` kèm `TraceId`, log đầy đủ, **không** lộ chi tiết nội bộ (stack trace)
  ra response cho client.
- `BusinessCode` dạng `UPPER_SNAKE` có chấm (`CRITERIA.NOT_FOUND`,
  `CRITERIA.DUPLICATE_CODE`) — không magic string rải rác nhiều nơi, khai tập
  trung ở `{Entity}Errors.cs` cạnh handler.

Chi tiết đầy đủ của `IApiResult<T>`/`ApiResult<T>`/`ErrorCode`/`ApiResultStatus`
xem `api-controller.md` §Envelope response.

## Grid / danh sách — envelope nhất quán

Endpoint list **vẫn dùng cùng envelope response** với endpoint đơn
(`IApiResult<PagedList<T>>`, xem `api-controller.md`) — không để response
list "trần" khác shape với response thường. Đây là quyết định thiết kế có
chủ đích để tránh FE phải viết logic parse 2 kiểu khác nhau — chính lỗi
"envelope drift" mà
`doc/huong_dan/wiki-core/be/trien-khai/03-p2-platform-application.md §5.2`
ghi nhận đã từng xảy ra thật ở hệ tham chiếu.

```csharp
public class PagedList<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }        // trên dây: "totalCount"
    // KHÔNG có TotalPages — xem quyết định bên dưới
}
```

> ### 📐 Shape phân trang — CHỐT một bản duy nhất (2026-08-23)
>
> **`PagedList<T>` = `{ items, page, pageSize, totalCount }`** cho **mọi** endpoint
> list, không có ngoại lệ.
>
> Trước đó tồn tại **ba** tên cho cùng một khái niệm — `PagedList` (`total`) ở quy
> ước và `contracts/users.md`; `IPagedResultDto` (`totalCount` + `totalPages`) ở
> `contracts/danh-muc-dti.md` và code FE; `PagedResult` (`TotalPages` với sentinel
> `-1`) ở `wiki-core/be/trien-khai/03-p2`. BE gửi `total`, FE đọc `totalCount` →
> `undefined`. Đúng loại *"vỡ runtime im lặng, build vẫn xanh"*.
>
> **Vì sao `totalCount` chứ không phải `total`:** `total` không nói rõ tổng của
> cái gì (dòng? trang? byte?), và `totalCount` là tên FE **đã dùng thật**.
>
> **Vì sao BỎ `totalPages`:** nó suy ra được từ `totalCount`/`pageSize`. Gửi kèm
> dữ liệu suy ra được nghĩa là tạo **hai nguồn có thể lệch nhau**. PrimeNG
> paginator chỉ cần `totalRecords` (= `totalCount`) và tự tính số trang. Sentinel
> `-1` của `TotalPages` bên VNR tồn tại chính vì nó là giá trị suy ra mà đôi khi
> không biết — đó là dấu hiệu nên bỏ, không phải nên chép.

## Audit log tối thiểu cho hành động nhạy cảm

`BaseEntity` (`UserCreate`/`UserUpdate`/`DateCreate`/`DateUpdate`) chỉ trả
lời "ai sửa **lần cuối**" — không có lịch sử "ai từng làm gì". Cho vài hành
động thật sự nhạy cảm (đổi `PermissionMatrix`, khoá/mở khoá user, xoá
`Criteria`) cần thêm 1 bảng audit riêng — **không** audit mọi write, chỉ
những hành động mà "ai làm, khi nào" có giá trị điều tra sau này.

```csharp
// Core.Application — 1 bảng duy nhất, đủ dùng ở quy mô hiện tại
public class AuditLogEntry
{
    public Guid Id { get; init; }
    public string EventType { get; init; } = default!;  // "Permission.Update", "User.Lock"...
    public string? EntityId { get; init; }
    public string UserId { get; init; } = default!;
    public string? Data { get; init; }                  // JSON snapshot, tuỳ chọn
    public DateTimeOffset DateCreate { get; init; }
}

public interface IAuditLogger
{
    void Log(string eventType, string? entityId, object? data = null);
}
```

Ghi **đồng bộ, trong cùng transaction** với hành động chính (gọi
`IAuditLogger.Log(...)` ngay trong handler, trước `SaveChangesAsync`) —
**KHÔNG** cần Channel/background dispatch non-blocking ở quy mô hiện tại
(traffic thấp, thêm 1 INSERT không đáng đo được độ trễ). Đây là bản rút gọn
của `AuditLogBehavior` + 4 interface
(`IAuditLogService`/`IAuditBackgroundChannel`/`IAuditLogger`/`IAuditLogReaderService`)
ở
[05-p4-hosting-api.md §12](../wiki-core/be/trien-khai/05-p4-hosting-api.md) —
nâng cấp lên Channel non-blocking khi đo được ghi đồng bộ thật sự ảnh hưởng
latency, không phải trước.

## Command chạy lâu → job nền (Hangfire)

**Finding thật (2026-08-17):** `ImportCsvCommand`/`CsvImportService.ImportAsync`
chạy **đồng bộ trong request HTTP** — ghi từng dòng 1 (`SaveChangesAsync`
mỗi dòng), giới hạn file 20MB. File lớn thật (vài nghìn dòng) có nguy cơ
timeout request thật sự, không phải rủi ro lý thuyết.

**Ngưỡng quyết định** — tách job nền (Hangfire) thay vì handler đồng bộ khi
1 trong các điều sau đúng: xử lý số dòng/file không có giới hạn trên rõ ràng
(import, export lớn), hoặc gọi ra ngoài mà latency không kiểm soát được
(email, tích hợp HTTP bên thứ 3 — xem thêm mục Notification ở
`architecture.md`). Command CRUD thường (tạo/sửa 1 bản ghi) **không** áp
dụng — chỉ thêm phức tạp không cần thiết.

**Pattern chuẩn — `202 + jobId + polling`:**

```csharp
// 1. Controller/Handler nhận request, lưu input, enqueue, trả ngay — KHÔNG đợi xử lý xong
[HttpPost]
public async Task<IActionResult> Import(IFormFile file, CancellationToken ct)
{
    var jobId = await mediator.Send(new StartImportCommand(file), ct);   // tạo ImportJob(Status=Pending), lưu file tạm
    BackgroundJob.Enqueue<IImportJobRunner>(r => r.RunAsync(jobId, CancellationToken.None));
    return Accepted(new { jobId });   // 202 — KHÔNG đợi Hangfire chạy xong
}

// 2. Endpoint riêng cho FE poll trạng thái
[HttpGet("{jobId:guid}")]
public async Task<IActionResult> GetStatus(Guid jobId, CancellationToken ct)
    => HandleResult(await mediator.Send(new GetImportJobStatusQuery(jobId), ct));
```

- **Job chạy trong Hangfire worker KHÔNG có `HttpContext`** — `IImportJobRunner`
  tự resolve scope DI riêng (`IServiceScopeFactory`), không inject
  `ICurrentUser`/`IHttpContextAccessor` như handler thường.
- File upload (`IFormFile`) **không sống sót** qua ranh giới request→job nền
  — phải ghi ra storage tạm (`IImportFileStorage`) TRƯỚC khi enqueue, job đọc
  lại từ đó, không truyền `Stream`/`IFormFile` vào job.
- Kết quả job ghi vào chính bản ghi job (`Status`/`ResultJson`/`ErrorMessage`)
  — FE poll qua `GetImportJobStatusQuery`, không qua cơ chế nào khác (SignalR/
  WebSocket chưa cần ở quy mô hiện tại — polling vài giây/lần là đủ).
- Pattern này dùng lại được cho bất kỳ command dài hơi nào khác sau này
  (không riêng Import) — xem phía FE tương ứng ở
  `doc/huong_dan/quy-uoc/fe-api-client.md` §"Long-running operation — poll pattern".

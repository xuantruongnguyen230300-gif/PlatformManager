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
    public int Total { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}
```

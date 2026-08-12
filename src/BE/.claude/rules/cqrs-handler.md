# CQRS & Handler — src/BE

## Command / Query qua MediatR

```csharp
public record CreateCriteriaCommand(string Code, string Name, string Group, decimal MaxScore)
    : IRequest<Result<Guid>>;

public record GetCriteriaByIdQuery(Guid Id) : IRequest<Result<CriteriaDto>>;

public record GetCriteriaListQuery(int Page = 1, int PageSize = 20, string? SearchText = null)
    : IRequest<Result<PagedList<CriteriaDto>>>;
```

- Đặt tên query danh sách: `Get{Entity}sListQuery` (số nhiều trước `List`).
- Command/Query là `record` bất biến — không class có setter.

## Handler — 4 bước, handler own `SaveChanges`

```csharp
public class CreateCriteriaHandler(ICriteriaRepository repo, IUnitOfWork uow)
    : IRequestHandler<CreateCriteriaCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateCriteriaCommand cmd, CancellationToken ct)
    {
        // 1. Business rule cần DB (uniqueness) → trả Result lỗi, không throw
        if (await repo.CodeExistsAsync(cmd.Code, ct))
            return Result<Guid>.Conflict($"CRITERIA_DUPLICATE_CODE: '{cmd.Code}' đã tồn tại.");

        // 2. Dựng entity qua domain factory — không new + gán property
        var entity = Criteria.Create(cmd.Code, cmd.Name, cmd.Group, cmd.MaxScore);

        // 3. Persist — handler own SaveChanges, repository KHÔNG tự commit
        await repo.AddAsync(entity, ct);
        await uow.SaveChangesAsync(ct);

        // 4. Trả kết quả
        return Result<Guid>.Success(entity.Id);
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
Application, không lặp lại try/validate thủ công trong từng handler.

## `Result<T>` — thay cho exception ở lỗi nghiệp vụ mong đợi

```csharp
public enum ResultErrorType { None, NotFound, Conflict, Validation, Forbidden }

public class Result<T>
{
    public bool IsSuccess { get; private init; }
    public T? Value { get; private init; }
    public string? ErrorCode { get; private init; }
    public string? ErrorMessage { get; private init; }
    public ResultErrorType ErrorType { get; private init; }

    public static Result<T> Success(T value) => new() { IsSuccess = true, Value = value };
    public static Result<T> NotFound(string msg) => new() { IsSuccess = false, ErrorMessage = msg, ErrorType = ResultErrorType.NotFound };
    public static Result<T> Conflict(string msg) => new() { IsSuccess = false, ErrorMessage = msg, ErrorType = ResultErrorType.Conflict };
    public static Result<T> Validation(string msg) => new() { IsSuccess = false, ErrorMessage = msg, ErrorType = ResultErrorType.Validation };
}
```

**Quy tắc:**
- Lỗi nghiệp vụ **mong đợi** (not found, trùng code, vi phạm business rule)
  → trả `Result<T>` lỗi, **không throw**.
- Lỗi **không mong đợi** (bug, lỗi kết nối DB) → để exception bay lên,
  middleware toàn cục bắt, trả `500` kèm `TraceId`, log đầy đủ, **không** lộ
  chi tiết nội bộ (stack trace) ra response cho client.
- `ErrorCode` dạng `UPPER_SNAKE` có ngữ nghĩa (`CRITERIA_NOT_FOUND`,
  `CRITERIA_DUPLICATE_CODE`) — không magic string rải rác nhiều nơi, khai
  hằng số ở `{Entity}Errors.cs` cạnh handler.

## Grid / danh sách — envelope nhất quán

Endpoint list **vẫn dùng cùng envelope response** với endpoint đơn (xem
`api-controller.md`) — không để response list "trần" khác shape với response
thường. Đây là quyết định thiết kế có chủ đích để tránh FE phải viết logic
parse 2 kiểu khác nhau.

```csharp
public class PagedList<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];
    public int Total { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}
```

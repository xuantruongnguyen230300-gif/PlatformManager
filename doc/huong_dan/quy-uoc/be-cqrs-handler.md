---
kind: luat
scope: core
verified: 2026-09-06
---

# CQRS & Handler — src/BE

## Command / Query qua MediatR

**Đã CHỐT (2026-08-15):** envelope trả về là `IApiResult<T>` (xem
[`be-api-controller.md`](be-api-controller.md) §Envelope response), **không phải** `Result<T>` tự chế —
theo `doc/tham-khao-ngoai/vnr-successor/03-p2-platform-application.md`.

Khai qua **`ICommand<T>` / `IQuery<T>`**, KHÔNG khai `IRequest<IApiResult<T>>` trực tiếp:

```csharp
// Common/CQRS/ICommand.cs + IQuery.cs — envelope bị ép ở tầng TYPE
public interface ICommand<TResult> : IRequest<IApiResult<TResult>>;
public interface IQuery<TResult> : IRequest<IApiResult<TResult>>;

// nơi dùng
public sealed record CreateCriteriaCommand(string Code, string Name, string Group, decimal MaxScore)
    : ICommand<Guid>;

public sealed record GetCriteriaByIdQuery(Guid Id) : IQuery<CriteriaDto>;

public sealed record GetCriteriasListQuery(int Page = 1, int PageSize = 20, string? SearchText = null)
    : IQuery<PagedList<CriteriaDto>>;
```

- Đặt tên query danh sách: `Get{Entity}sListQuery` (số nhiều trước `List`).
- Command/Query là **`sealed record`** bất biến — không class có setter.

> **🔄 LẬT 2026-09-06.** Bản trước dạy `: IRequest<IApiResult<Guid>>` trực tiếp và
> `public record` (không `sealed`). Cả **14** command/query trong `src/BE/Core` đều dùng
> `ICommand<T>`/`IQuery<T>` và đều `sealed record` — không cái nào theo khuôn cũ. Đây là
> file chủ của chủ đề CQRS, nên đoạn mẫu sai ở đây là đoạn mẫu người ta chép. Kiểm bằng
> lệnh (§6), đừng đếm tay:
>
> ```bash
> grep -rn ": IRequest<IApiResult" src/BE/Core --include=*.cs | grep -v "interface I"
> ```
>
> PASS: **rỗng**. Hai interface đó tồn tại chính để handler *không thể* "quên" trả
> envelope — docstring của `ICommand.cs:7-8` nói lại điều này và trỏ ngược về file này.

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
([`be-api-controller.md`](be-api-controller.md) §Exception-handling) dịch nó thành `IApiResult` lỗi
`ErrorCode.ValidationError` (400) kèm `Fields`; validator **không** tự dựng
response.

### ⚠️ Nhiều validator cho MỘT request — mỗi cái phải có `ValidationContext` RIÊNG

Một request được phép có **nhiều** validator (`ValidationBehavior` chạy mọi
`IValidator<TRequest>` đăng ký). Khi đó có một cái bẫy im lặng:

Rule kiểu `Custom` / `CustomAsync` báo lỗi bằng `context.AddFailure(...)` — tức **ghi thẳng
vào `ValidationContext`**, không phải trả về kết quả. Nếu behavior dựng **một** context rồi
truyền cho tất cả validator, thì `ValidationResult` của **mọi** validator đều chứa lỗi đó, và
việc gộp kết quả sẽ đếm nó **N lần** với N = số validator.

**Triệu chứng:** người dùng vi phạm đúng một luật nhưng nhận thông điệp lặp lại trong `fields`.
Không lỗi biên dịch, không exception, không log gì.

**Đo được 2026-09-01**, và đáng chú ý ở chỗ **vì sao nó ẩn lâu như vậy**: cho tới 2026-08-31
không lệnh nào có quá một validator, nên điều kiện kích hoạt chưa từng tồn tại. Đây không phải
hồi quy mới — là bẫy nằm sẵn từ đầu và vừa đủ điều kiện nổ. Bài học áp rộng hơn FluentValidation:
**thứ gì nhận trạng thái chia sẻ để ghi kết quả vào thì không dùng chung được giữa nhiều lượt chạy.**

Kiểm bằng lệnh:

```bash
grep -n "new ValidationContext" src/BE/Core/PlatformManager.Core.Application/Common/Behaviors/ValidationBehavior.cs
```

PASS khi lời dựng context nằm **bên trong** phép chiếu theo từng validator, không nằm ngoài.

## `ErrorDescriptor` — thay cho magic string, nguồn lỗi nghiệp vụ mong đợi

```csharp
public sealed record ErrorDescriptor(
    string BusinessCode,       // "{ENTITY}.{ERROR}" — UPPER_SNAKE có chấm
    ErrorCode ErrorCode,       // xem be-api-controller.md — giá trị enum = mã HTTP
    string MessageTemplate,    // chỗ giữ ĐẶT TÊN: "{UserName}", "{Email}" — KHÔNG phải "{0}"
    bool Retryable = false);
```

> **Ví dụ đổi 2026-09-05 từ `{Reasons}` sang `{Email}`.** `{Reasons}` từng là ví dụ ở đây,
> nhưng nó là chỗ giữ nhận một **danh sách mã lỗi đã nối chuỗi** — thứ vỡ khi đổi ngôn ngữ, và
> người dùng đã chốt 2026-09-05 gỡ bỏ (mã đi ra `fieldErrors`, xem
> [`../wiki-core/be/16-i18n-va-ma-loi.md`](../wiki-core/be/16-i18n-va-ma-loi.md) §11). Một chỗ
> giữ chỉ nên nhận **một giá trị**, không nhận danh sách — nêu nó làm mẫu là dạy lại đúng cái
> khuôn đang bị gỡ. ✅ Ba descriptor đó đã sạch `{Reasons}` từ 2026-09-05 — đếm bằng lệnh:
> `grep -rn 'CHANGE_PASSWORD_FAILED\|USER.CREATE_FAILED\|USER.UPDATE_FAILED' src/BE/Core --include=*Errors.cs`
> phải in 3 dòng và **không dòng nào** chứa `{`.

Khai báo tập trung cạnh handler, **không** rải rác string literal trong code:

> **Sửa 2026-09-05 — đường dẫn ví dụ trỏ vào một project bị CẤM dựng lại.** Bản trước ghi
> `PlatformManager.Modules.DtiWeekly.Application/Criteria/CriteriaErrors.cs`. Mô hình
> `Modules.<Tên>.*` đã bị thay bằng hai tầng `Core.*` / `Business.*`, và
> [`../../kien-truc-core-module.md`](../../kien-truc-core-module.md) nêu đích danh
> `Modules.DtiWeekly.*` là tên **không** được dựng lại. Một đoạn mẫu là thứ người ta chép,
> nên đường dẫn sai trong đoạn mẫu sinh ra project sai.
>
> *(Trích dẫn `Modules.DtiWeekly.Application` còn lại ở §"Command chạy lâu → job nền" là
> **văn bản lỗi mà ArchTest in ra** trong một lần canary 2026-08-28 — đó là ghi chép về một
> phép đo đã xảy ra, không phải chỉ dẫn. Giữ nguyên.)*

```csharp
// PlatformManager.Business.Application/Criteria/CriteriaErrors.cs
public static class CriteriaErrors
{
    public static readonly ErrorDescriptor NotFound = new(
        "CRITERIA.NOT_FOUND", ErrorCode.NotFound, "Không tìm thấy chỉ tiêu.");

    public static readonly ErrorDescriptor DuplicateCode = new(
        "CRITERIA.DUPLICATE_CODE", ErrorCode.Conflict, "Mã chỉ tiêu '{Code}' đã tồn tại.");
}
```

## `BaseResponse` — API mà handler thật sự dùng

Handler kế thừa `BaseResponse` và chỉ dùng 2 động từ — `Ok`/`Fail` — không tự
dựng `ApiResult<T>` bằng tay ở từng handler:

```csharp
public abstract class BaseResponse
{
    protected static IApiResult<T> Ok<T>(T data, string? message = null)
        => ApiResult<T>.Success(data, message);

    // Overload 1 — lỗi nghiệp vụ có tham số câu chữ
    protected static IApiResult<T> Fail<T>(
        ErrorDescriptor error,
        params (string Name, object? Value)[] args)
        => // ráp câu fallback VÀ dựng messageParams trong CÙNG một vòng lặp —
           // một nguồn, hai đầu ra, không lệch nhau được
           ...;

    // Overload 2 — lỗi cần gắn vào ĐÚNG Ô NHẬP (fieldErrors[].code, FE tra bảng dịch)
    protected static IApiResult<T> Fail<T>(
        ErrorDescriptor error, Dictionary<string, ApiFieldError[]>? fieldErrors)
        => ApiResult<T>.BusinessError(error, error.MessageTemplate, messageParams: null, fieldErrors: fieldErrors);
}
```

> *(Sửa 2026-09-06: bản trước khai `Ok<T>` là **instance method** trong khi thật ra nó
> `static` (`BaseResponse.cs:11`), và chỉ nêu **một** overload `Fail<T>`. Overload thứ hai
> (`BaseResponse.cs:78`) là đường duy nhất đưa mã lỗi xuống đúng ô nhập; thiếu nó trong
> file chủ nghĩa là người viết handler mới không biết nó tồn tại và sẽ nhồi mã vào
> `fields`.)*

> **🔄 LẬT 2026-09-05.** Bản trước của cả ba khối trên dạy khuôn **`{0}` + `params object[]` +
> `string.Format`**. Khuôn đó **không còn tồn tại** — chữ ký thật là
> `params (string Name, object? Value)[]`, chỗ giữ là **TÊN**, và câu được ráp bằng
> `MessageFormatter` của FluentValidation chứ không phải `string.Format`. Đối chiếu:
> `grep -n 'Fail<T>' src/BE/Core/PlatformManager.Core.Application/Common/Results/BaseResponse.cs`.
>
> **Vì sao đây là loại sai đắt nhất:** ai chép mẫu ở file chủ này sẽ khai template `{0}`, và
> `MessageFormatter` **giữ nguyên literal `{0}`** trong câu trả về — **không đỏ biên dịch, không
> test nào bắt**, chỉ lộ ra khi người dùng đọc được chữ `{0}` giữa giao diện. Đúng cơ chế thất bại
> số 1 mà [`.claude/CLAUDE.md`](../../../.claude/CLAUDE.md) §3 mô tả: *"rule sai không nằm yên —
> nó sinh ra code sai"*.
>
> Lý do đổi khuôn: chỗ giữ đặt tên đi thẳng ra `messageParams` trong envelope để client tự dựng
> câu theo ngôn ngữ của nó. File chủ của cơ chế:
> [`../wiki-core/be/16-i18n-va-ma-loi.md`](../wiki-core/be/16-i18n-va-ma-loi.md) §10.

**Quy tắc:**
- Lỗi nghiệp vụ **mong đợi** (not found, trùng code, vi phạm business rule)
  → `Fail<T>(descriptor, args)`, **không throw**.
- Lỗi **không mong đợi** (bug, lỗi kết nối DB) → để exception bay lên,
  middleware toàn cục bắt ([`be-api-controller.md`](be-api-controller.md) §Exception-handling), trả
  `500` kèm `TraceId`, log đầy đủ, **không** lộ chi tiết nội bộ (stack trace)
  ra response cho client.
- `BusinessCode` dạng `UPPER_SNAKE` có chấm (`CRITERIA.NOT_FOUND`,
  `CRITERIA.DUPLICATE_CODE`) — không magic string rải rác nhiều nơi, khai tập
  trung ở `{Entity}Errors.cs` cạnh handler.

### Mã lỗi **domain** cũng phải theo khuôn này — ✅ CÓ THẬT (đối chiếu 2026-09-03)

Khuôn `MIEN.MA_LOI` ở trên áp cho **mọi** chuỗi có thể ra tới field `businessCode`
của envelope, **không riêng** descriptor khai trong `{Entity}Errors.cs`. Domain
ném `DomainException` cũng nằm trong phạm vi đó.

> **Sửa 2026-09-03.** Bản trước của đoạn này viết ở thì hiện tại: *"chuỗi lập trình viên gõ trong
> `throw` đi thẳng ra envelope, vì `ExceptionHandlingBehavior.cs:59` dựng `ErrorDescriptor`
> **inline** từ `DomainException.Code`"*. Câu đó mô tả hiện trạng **trước** 2026-09-03 và nay
> không còn đúng ở cả ba vế: `DomainException.Code` không còn tồn tại (chỉ còn `Error` kiểu
> `DomainError`), chỗ dựng envelope đọc `error.BusinessCode` **từ catalog**, và "gõ chuỗi trong
> `throw`" nay là **lỗi biên dịch** chứ không phải hành vi.

Tầng Domain khai mã bằng một bản ghi RIÊNG,
`src/BE/Core/PlatformManager.Core.Domain/Common/DomainError.cs:36` — không phải
`ErrorDescriptor`, vì `Core.Domain` không được phụ thuộc `Core.Application` (luật
tầng ở [`be-architecture.md`](be-architecture.md), cưỡng chế bởi
`src/BE/Tests/PlatformManager.ArchTests/LayerDependencyTests.cs:47`). Khuôn khai
**giống hệt** `{Entity}Errors.cs` ở đây, chỉ khác tên kiểu; ví dụ sống:
`src/BE/Core/PlatformManager.Core.Domain/Entities/SysMenuErrors.cs:12`.

`DomainError` cố ý **không** mang `ErrorCode`: mã HTTP do LOẠI EXCEPTION quyết
định (`DomainException` ⇒ 422, `ConflictException` ⇒ 409) tại
hàm `ExceptionHandlingBehavior.BuildErrorResponse`
(`src/BE/Core/PlatformManager.Core.Application/Common/Behaviors/ExceptionHandlingBehavior.cs`).
Khai ở cả hai nơi là hai nguồn cho một sự thật, và chúng mâu thuẫn được mà vẫn
biên dịch.

| Trước 2026-09-03 | ✅ CÓ THẬT hôm nay |
| --- | --- |
| `DomainException` nhận `string code` tự do; 6 mã domain **không có dấu chấm** đang lưu hành | Chỉ nhận `DomainError` (`src/BE/Core/PlatformManager.Core.Domain/Common/DomainException.cs:18`); 6 mã đã vào catalog theo khuôn `MIEN.MA_LOI` |

**Nghiệm thu** — PASS 2026-09-03 (không in gì); trước đó có output:

```bash
grep -rho 'new DomainException("[A-Z0-9_.]*"' src/BE/Core --include=*.cs | grep -v '\.'
```

Hai luật quét mã nguồn chặn tái phát (dựng `DomainException` hoặc `ErrorDescriptor`
bằng chuỗi literal) nằm ở
`src/BE/Tests/PlatformManager.ArchTests/ErrorCodeSourceTests.cs:68`
(`DomainException_IsNever_BuiltFrom_AStringLiteral`) và `:88`
(`ErrorDescriptor_IsNever_BuiltFrom_AStringLiteral_OutsideACatalog`). Mỗi luật có một test
**đối chứng** đi kèm (`:112`, `:134`) chứng minh bộ dò thật sự bắt được vi phạm — không có
chúng thì một bộ dò hỏng sẽ xanh vĩnh viễn.

> *(🔄 LẬT 2026-09-06: hai số cũ `:60`/`:80` trỏ vào **giữa một docstring** và **giữa một
> chuỗi ghép của thông điệp assert** — không dòng nào là luật. Đúng loại citation mà cổng
> `check-docs.sh` §6 cho qua vì nó chỉ kiểm số dòng có nằm trong file.)*

Vì sao luật này phải đứng ở đây chứ không ở chỗ khác: đây là **khuôn khai báo mã**,
và `ErrorDescriptor.cs` đã trỏ về đúng mục này bằng docstring. Lý do i18n khiến
mã lỗi trở thành khoá dịch — tức vì sao việc này là tiên quyết cứng — thuộc
[`../wiki-core/be/16-i18n-va-ma-loi.md`](../wiki-core/be/16-i18n-va-ma-loi.md) §4.

Chi tiết đầy đủ của `IApiResult<T>`/`ApiResult<T>`/`ErrorCode`/`ApiResultStatus`
xem [`be-api-controller.md`](be-api-controller.md) §Envelope response.

## Grid / danh sách — envelope nhất quán

Endpoint list **vẫn dùng cùng envelope response** với endpoint đơn
(`IApiResult<PagedList<T>>`, xem [`be-api-controller.md`](be-api-controller.md)) — không để response
list "trần" khác shape với response thường. Đây là quyết định thiết kế có
chủ đích để tránh FE phải viết logic parse 2 kiểu khác nhau — chính lỗi
"envelope drift" mà
`doc/tham-khao-ngoai/vnr-successor/03-p2-platform-application.md §5.2`
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
> `-1`) ở `tham-khao-ngoai/vnr-successor/03-p2`. BE gửi `total`, FE đọc `totalCount` →
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

## Audit log tối thiểu cho hành động nhạy cảm — 📐 ĐÍCH ĐẾN, CHƯA THI CÔNG

> **🔄 LẬT 2026-09-06 — mục này chưa bao giờ có nhãn trạng thái, và người đọc mặc định nó
> mô tả code đang chạy.** Không có: `grep -rn "IAuditLogger\|AuditLogEntry" src/BE --include=*.cs | grep -v /obj/`
> ra **rỗng** (đối chiếu 2026-09-06). Không bảng audit, không interface, không migration.
> Toàn bộ mục là **khuôn cho lần dựng đầu tiên**.
>
> Thứ **có thật** hôm nay là cột audit trên từng dòng (`CreatedBy`/`UpdatedBy`/`CreatedAt`/
> `UpdatedAt` của `BaseEntity`, và cặp gắn tay trên `AppUser`) — cộng với việc hai bảng ma
> trận phân quyền **xoá mềm** nên giữ lại từng thế hệ, xem
> [`be-entity-domain.md`](be-entity-domain.md) §"Vì sao riêng quyết định 3 là bắt buộc".
> Đó là lý do nhu cầu bảng audit riêng chưa cấp bách cho ma trận, nhưng **vẫn còn nguyên**
> cho khoá/mở khoá tài khoản (`AppUser` nằm ngoài tầm `AuditInterceptor` — xem
> `Tests/PlatformManager.Core.IntegrationTests/Users/UserAdminAuditTests.cs`).

`BaseEntity` (`CreatedBy`/`UpdatedBy`/`CreatedAt`/`UpdatedAt`) chỉ trả
lời "ai sửa **lần cuối**" — không có lịch sử "ai từng làm gì". Cho vài hành
động thật sự nhạy cảm (đổi ma trận phân quyền, khoá/mở khoá user) cần thêm 1 bảng audit
riêng — **không** audit mọi write, chỉ những hành động mà "ai làm, khi nào" có giá trị
điều tra sau này.

```csharp
// Core.Application — 1 bảng duy nhất, đủ dùng ở quy mô hiện tại
public class AuditLogEntry
{
    public Guid Id { get; init; }
    public string EventType { get; init; } = default!;  // "Permission.Update", "User.Lock"...
    public string? EntityId { get; init; }
    public string UserId { get; init; } = default!;
    public string? Data { get; init; }                  // JSON snapshot, tuỳ chọn
    public DateTimeOffset CreatedAt { get; init; }
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
[05-p4-hosting-api.md §12](../wiki-core/../../tham-khao-ngoai/vnr-successor/05-p4-hosting-api.md) —
nâng cấp lên Channel non-blocking khi đo được ghi đồng bộ thật sự ảnh hưởng
latency, không phải trước.

## Command chạy lâu → job nền (Hangfire)

**Khuôn nhận diện, rút ra từ một ca đã xảy ra thật (2026-08-17):**
`ImportCsvCommand`/`CsvImportService.ImportAsync` chạy **đồng bộ trong request HTTP** —
ghi từng dòng một (`SaveChangesAsync` mỗi dòng), giới hạn file 20MB. File lớn thật (vài
nghìn dòng) có nguy cơ timeout request thật sự, không phải rủi ro lý thuyết.

> **🔄 LẬT 2026-09-06.** Bản trước mở đầu bằng *"**Finding thật (2026-08-17):**"* ở thì
> hiện tại, khiến mục này đọc như một lỗ hổng đang mở. Cả hai class đã xoá cùng module
> DtiWeekly **2026-08-29** — hôm nay `src/BE` không có đường import nào, và
> `IBackgroundJobScheduler` **không có nơi gọi nào**. Giữ ca cũ làm khuôn (hình dạng lặp
> lại ở mọi import/export sau này), bỏ vế hiện trạng.

**Ngưỡng quyết định** — tách job nền (Hangfire) thay vì handler đồng bộ khi
1 trong các điều sau đúng: xử lý số dòng/file không có giới hạn trên rõ ràng
(import, export lớn), hoặc gọi ra ngoài mà latency không kiểm soát được
(email, tích hợp HTTP bên thứ 3 — xem thêm mục Notification ở
[`be-architecture.md`](be-architecture.md)). Command CRUD thường (tạo/sửa 1 bản ghi) **không** áp
dụng — chỉ thêm phức tạp không cần thiết.

**Pattern chuẩn — `jobId + polling`:**

> ### ⚠️ Sửa 2026-09-05 — pattern này KHÔNG trả 202, nó trả 200 như mọi endpoint khác
>
> Bản trước của mục này đặt tên pattern là *"`202 + jobId + polling`"* và đoạn mẫu gọi
> `return Accepted(new { jobId })`. Cả hai **trái với dispatcher đang chạy**, và trái theo
> hai đường độc lập:
>
> | Bằng chứng (đối chiếu source 2026-09-05) | Hệ quả của đoạn mẫu cũ |
> | --- | --- |
> | [`ApiControllerBase.cs:37`](../../../src/BE/Core/PlatformManager.Core.Api/ApiControllerBase.cs) — `result.Code == ErrorCode.Success ? 200 : (int)result.Code` | Không có đường nào sinh ra 202. Muốn 202 thì controller phải tự đặt status, tức tạo nguồn sự thật thứ hai cho mapping `ErrorCode → HTTP` — đúng thứ §Dispatcher của [`be-api-controller.md`](be-api-controller.md) cấm |
> | [`ErrorCode.cs:11`](../../../src/BE/Core/PlatformManager.Core.Application/Common/Results/ErrorCode.cs) — enum không có member nào mang giá trị 202 | `HandleResult` không biểu diễn được trạng thái này |
> | `Accepted(new { jobId })` trả một object **trần** | Response rời BE **ngoài** `IApiResult<T>` — phá lời hứa "mọi response đi qua envelope" của [`../../contracts/auth.md`](../../contracts/auth.md), đúng lỗ hổng mà `ApiStatusCodeEnvelopeMiddleware` vừa bịt cho 404/405 |
>
> **"Đã bắt đầu chứ chưa xong" thể hiện ở tầng DỮ LIỆU** (`jobId` cần poll tiếp), **không ở
> HTTP status.** Đoạn mẫu dưới đây đã sửa theo đúng dispatcher.
>
> Dòng *"`202` + `jobId` trong envelope"* ở
> [`../wiki-core/be/15-import-export.md`](../wiki-core/be/15-import-export.md) §3 đã sửa
> cùng lượt — hai chỗ mô tả cùng một pattern thì không được nói khác nhau.

```csharp
// 1. Controller MỎNG — chỉ gọi mediator rồi HandleResult. KHÔNG enqueue ở đây,
//    KHÔNG tự đặt status code (xem cảnh báo trên).
[HttpPost]
public async Task<IActionResult> Import(IFormFile file, CancellationToken ct)
    => HandleResult(await mediator.Send(new StartImportCommand(file), ct));
    // -> 200 + envelope IApiResult<T> như mọi endpoint khác; `data` mang jobId.
    //    KHÔNG đợi job chạy xong. Client biết "chưa xong" vì nó cầm jobId để poll.
    //    `T` cụ thể là chuyện của từng hợp đồng — vd doc/contracts/danh-muc-dti.md DM-7
    //    chọn một DTO { jobId } thay vì Guid trần, để thêm trường sau không phá shape.

// 1b. Handler làm trọn use case: tạo ImportJob(Status=Pending) + lưu file tạm + enqueue.
//     Enqueue qua SEAM, không gọi thẳng Hangfire — xem cảnh báo bên dưới.
public async Task<IApiResult<Guid>> Handle(StartImportCommand cmd, CancellationToken ct)
{
    // ... tạo ImportJob, lưu file qua IImportFileStorage ...
    await scheduler.EnqueueAsync<IImportJobRunner>(r => r.RunAsync(job.Id, CancellationToken.None), ct);
    return Ok(job.Id);
}

// 2. Endpoint riêng cho FE poll trạng thái
[HttpGet("{jobId:guid}")]
public async Task<IActionResult> GetStatus(Guid jobId, CancellationToken ct)
    => HandleResult(await mediator.Send(new GetImportJobStatusQuery(jobId), ct));
```

- **Job chạy trong Hangfire worker KHÔNG có `HttpContext`** — `IImportJobRunner`
  tự resolve scope DI riêng (`IServiceScopeFactory`), không inject
  `ICurrentUser`/`IHttpContextAccessor` như handler thường.
- **Enqueue đi qua seam `IBackgroundJobScheduler`** (khai ở `*.Application`,
  hiện thực Hangfire ở `*.Infrastructure`) — **không** gọi thẳng
  `BackgroundJob.Enqueue` trong handler. Gọi thẳng nghĩa là tầng Application
  phụ thuộc `Hangfire.Core`, tức phụ thuộc hạ tầng cụ thể — vi phạm quy tắc
  phụ thuộc của Clean Architecture, và đúng khuôn mà repo này đã tách seam ở chỗ tương tự
  (`INotificationSender` cho email — seam duy nhất **còn tồn tại** ngoài
  `IBackgroundJobScheduler`; `IImportFileStorage` từng là chỗ thứ hai nhưng đã xoá cùng
  module DtiWeekly 2026-08-29, xem
  [`../wiki-core/be/14-file-storage.md`](../wiki-core/be/14-file-storage.md) §2).
  Đặt Enqueue lên controller **không** phải cách sửa: nó chỉ đổi một vi
  phạm này lấy một vi phạm khác (controller chứa logic) và cắt đôi một use case.

  > ✅ CÓ THẬT — đối chiếu 2026-08-28. Seam khai ở
  > [`IBackgroundJobScheduler.cs:19`](../../../src/BE/Core/PlatformManager.Core.Application/Common/Interfaces/IBackgroundJobScheduler.cs),
  > chữ ký `Task EnqueueAsync<TJob>(Expression<Func<TJob, Task>> methodCall, CancellationToken ct = default)`
  > (chỉ dùng `System.Linq.Expressions` của BCL). Hiện thực
  > [`HangfireBackgroundJobScheduler.cs:15`](../../../src/BE/Core/PlatformManager.Core.Infrastructure/BackgroundJobs/HangfireBackgroundJobScheduler.cs)
  > — inject `IBackgroundJobClient` chứ không dùng facade tĩnh `BackgroundJob`.
  > Đăng ký DI ở [`Program.cs:175`](../../../src/BE/PlatformManager.Api/Program.cs)
  > (`AddBackgroundJobInfrastructure()`, gọi **sau** `AddHangfire` ở `:164`, tách riêng
  > khỏi `AddCoreModule` để đọc `Program.cs` là thấy từng mảnh hạ tầng bật ở đâu
  > — cùng ý đồ thiết kế với `AddNotificationInfrastructure`, nhưng lưu ý
  > `AddNotificationInfrastructure` **chưa được gọi ở đâu cả** (đối chiếu lại 2026-09-06):
  > chưa có consumer, và `SmtpOptions.ValidateOnStart()` sẽ chặn app khởi động
  > vì thiếu section `Smtp` — lý do đầy đủ ghi ở `Program.cs:177-185`).
  > *(🔄 LẬT 2026-09-06: hai số cũ — `Program.cs:87` cho lời gọi và "comment ngay cạnh
  > dòng 87" — đều sai. Dòng 87 là `builder.Services` mở đầu cụm `AddControllers`, không
  > liên quan job nền.)*
  > **Chưa có handler nào gọi seam (2026-08-29).** Chỗ dùng thật duy nhất là
  > `StartImportCommand` của module DtiWeekly, đã xoá cùng module. Seam + hiện
  > thực + gate đều còn nguyên ở Core; chỗ dùng sẽ trở lại khi Import được dựng
  > ở `Core.Application` — xem [`../wiki-core/be/14-file-storage.md`](../wiki-core/be/14-file-storage.md).
  > Ghim `Newtonsoft.Json` chống NU1903 nằm ở `Core.Infrastructure.csproj`
  > cùng `Hangfire.Core`.

  > ⚠️ **Giới hạn của ArchTest — đo được, đừng tin quá tay.**
  > [`LayerDependencyTests.cs:35`](../../../src/BE/Tests/PlatformManager.ArchTests/LayerDependencyTests.cs)
  > liệt `Hangfire` vào danh sách cấm, dùng chung cho cả `Core.Application` lẫn
  > `Modules.*.Application`. Nhưng nó đọc `GetReferencedAssemblies()`, mà
  > Roslyn **lược khỏi manifest** mọi reference không có code nào chạm tới.
  > Canary 2026-08-28: thêm lại `PackageReference Hangfire.Core` mà **không**
  > có lời gọi nào → test vẫn **xanh**; thêm lại cả `BackgroundJob.Enqueue`
  > trong handler → test **đỏ**, báo đúng `Modules.DtiWeekly.Application đang
  > reference (không được phép): Hangfire.Core`. Kết luận: test bắt **code gọi
  > thẳng hạ tầng** (đúng thứ phá layer), **không** bắt `PackageReference`
  > thừa nằm im trong csproj — chỗ đó vẫn phải review bằng mắt.

- File upload (`IFormFile`) **không sống sót** qua ranh giới request→job nền
  — phải ghi ra storage tạm (`IFileStorage`, seam **chưa dựng**) TRƯỚC khi enqueue, job đọc
  lại từ đó, không truyền `Stream`/`IFormFile` vào job. Storage đó lưu ở đâu,
  cấu hình thế nào, dọn ra sao:
  [`../wiki-core/be/14-file-storage.md`](../wiki-core/be/14-file-storage.md).
- Kết quả job ghi vào chính bản ghi job (`Status`/`ResultJson`/`ErrorMessage`)
  — FE poll qua `GetImportJobStatusQuery`, không qua cơ chế nào khác (SignalR/
  WebSocket chưa cần ở quy mô hiện tại — polling vài giây/lần là đủ).
- Pattern này dùng lại được cho bất kỳ command dài hơi nào khác sau này
  (không riêng Import) — xem phía FE tương ứng ở
  `doc/huong_dan/quy-uoc/fe-api-client.md` §"Long-running operation — poll pattern".

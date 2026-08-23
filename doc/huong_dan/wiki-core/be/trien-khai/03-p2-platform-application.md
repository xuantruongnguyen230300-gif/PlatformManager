# P2 — `Platform.Application`

> 📍 **Tên project trong file này là của VNR.Successor, không phải PlatformManager.**
> Tra bảng ánh xạ + 4 mục "KHÔNG áp dụng" ở [`00-lo-trinh-tong-the.md`](00-lo-trinh-tong-the.md)
> §ĐỌC TRƯỚC. Tóm tắt: `Platform.*`→`Core.*` · `Module.{M}.*`→tầng nghiệp vụ (`Business.*`) ·
> `Processes/`→**1** host · JWT→**cookie session** · per-module DbContext→**1** DbContext chung.

> **Định nghĩa hoàn thành:** viết được **một handler giả lập** (`PingCommand` →
> `PingHandler`) chạy qua MediatR và trả về `IApiResult<string>` đúng envelope;
> ném `ValidationException` trong handler đó thì response tự động thành
> `Status=VALIDATION_ERROR`, `Code=ValidationError`, có `Fields` — **mà handler
> không có một dòng `try-catch` nào**. Chưa cần DbContext, chưa cần controller.

Đây là layer **quyết định hình dạng của mọi thứ về sau**. Sai ở P1 (Domain) thì
sửa tốn một buổi; sai ở P2 thì mọi handler, mọi controller, mọi màn hình FE phải
sửa theo. Ba quyết định không thể đảo ngược rẻ:

1. **Shape của response** — FE viết code parse theo nó.
2. **Thứ tự pipeline behavior** — sai thứ tự không gây lỗi biên dịch, không gây
   test đỏ, mà gây **commit dữ liệu sai** trong production (xem §6.1).
3. **Cách biểu diễn lỗi** — `string` message hay `ErrorDescriptor`. Chọn `string`
   thì i18n và analytics lỗi coi như không làm được nữa.

---

## 1. Bản đồ thư mục — cái gì làm ở P2, cái gì để sau

`VNR.Platform.Application` thật của Successor có **104 file**. Không cần 104 file
để rời P2. Bảng dưới tách rõ:

| Thư mục | File | P2 | Ghi chú |
| --- | --- | --- | --- |
| `CQRS/` | `IPlatformRequest.cs` | ✅ | marker — 6 dòng, nhưng là bản lề của cả pipeline |
| | `ITransactionalCommand.cs` | ✅ | marker opt-in transaction |
| | `Commands/ICommand.cs` | ✅ | 4 interface |
| | `Commands/CommandHandler.cs` | ✅ | base class |
| | `Queries/IQuery.cs` | ✅ | 4 họ query |
| | `Queries/QueryHandler.cs` | ✅ | base class |
| | `PagedResult.cs` | ✅ | kết quả phân trang + `IPagedResult` |
| | `ITransactionManager.cs` | ✅ | interface — impl ở P3 |
| | `Commands/DefaultCrudCommands.cs` | ⏳ P5 | chỉ cần khi làm pattern CatalogCrud |
| `Results/` | `ErrorCode.cs` | ✅ | enum, **giá trị = HTTP status** |
| | `ApiResultStatus.cs` | ✅ | enum 4 member |
| | `IApiResult.cs` | ✅ | + `IHasApiResultStatus`, `IApiResultEnrichable` |
| | `ApiResult.cs` | ✅ | impl + factory |
| | `ErrorDescriptor.cs` | ✅ | record — trái tim của error handling |
| | `FieldError.cs` | ✅ | record 4 field |
| | `ApiErrorIds.cs` | ✅ | nguồn DUY NHẤT sinh LogId/TraceId + map status |
| | `CommonErrors.cs` | ✅ | 7 descriptor dùng chung |
| | `BaseResponse.cs` | ✅ | API mà handler thật sự "nhìn thấy" |
| `Behaviors/` | `ExceptionHandlingBehavior.cs` | ✅ | outermost |
| | `LoggingBehavior.cs` | ✅ | |
| | `ValidationBehavior.cs` | ✅ | |
| | `ModuleTransactionBehaviorBase.cs` | ✅ | + `…Extensions.cs` |
| | `AuditLogBehavior.cs` | ⏳ P6 | cần `IAppLogService` + bảng log |
| `Context/` | `IApplicationContext.cs` | ✅ | **3 member, không hơn** |
| `Contracts/` | `BaseDto.cs` | ✅ | |
| | `BaseRequestGridModel.cs` / `BaseResponseGridModel.cs` | ⏳ P4 | khi làm grid |
| | `Contracts/Catalog/*` | ⏳ P5 | |
| `Exceptions/` | `ErrorDescriptorException.cs` | ✅ | cho chỗ không trả được envelope |
| | `BusinessException`, `ConflictException` | ✅ | 2 file nhỏ |
| `Validation/` | `ValidationErrorState.cs` | ✅ | gắn `ErrorDescriptor` vào FluentValidation |
| | `ValidationMessageExtensions.cs` | ✅ | extension `.WithError(...)` |
| `Extensions/` | `ValidationBehaviorExtensions.cs` | ✅ | DI |
| | `ModuleRegistrationExtensions.cs` | ⏳ P5 | |
| | `ProjectionBuilder`, `LookupEnricher`, `EnumTranslation*` | ⏳ P6 | |
| `Interfaces/` | `Security/ICurrentUser.cs` | ✅ | |
| | `Caching/ICacheService.cs` | ✅ | |
| | `Translation/ITranslationService.cs` | ✅ | |
| | `Data/*` (11), `Jobs/*` (19), `Logging/*` (11), `Notifications/*` (7), `Search/*` (3), `Integration/*` (6), `FileSystem/*` (4) | ⏳ | **khai khi Infrastructure tương ứng ra đời**, không khai trước |

> **~28 file để rời P2**, không phải 104. Nguyên tắc: `Interfaces/` chỉ được thêm
> khi có người **sắp implement** nó ở P3/P4. Một interface không có implementation
> là nợ kỹ thuật đội lốt kiến trúc.

---

## 2. Thứ tự thi công trong P2

```
A1  marker (IPlatformRequest, ITransactionalCommand)          ~30 phút
        │  không phụ thuộc gì, nhưng mọi thứ sau phụ thuộc nó
        ▼
A2  Results/*  — envelope + ErrorCode + ErrorDescriptor        1 ngày
        │  ⚠️ CHỐT Ở ĐÂY. Sau bước này không đổi shape nữa.
        ▼
A3  CQRS — ICommand/IQuery + CommandHandler/QueryHandler       0.5 ngày
        │  chỉ ~120 dòng, vì mọi thứ nặng đã nằm ở A2
        ▼
A4  Behaviors ×4 + thứ tự đăng ký                              1 ngày
        │  phần khó nhất của cả phase
        ▼
A5  Context + Contracts + Interfaces tối thiểu                 0.5 ngày
```

Lý do A2 phải trước A3: `ICommand<TResult>` khai `IRequest<IApiResult<TResult>>` —
tức là **chữ ký của CQRS phụ thuộc trực tiếp vào envelope**. Làm ngược thì viết
xong handler mới nhận ra `IApiResult` cần thêm field, phải sửa cả hai.

---

## 3. A1 — Hai marker interface

```csharp
// CQRS/IPlatformRequest.cs
namespace VNR.Platform.Application.CQRS;

/// <summary>Marker cho mọi request đi qua pipeline của Platform.</summary>
public interface IPlatformRequest { }
```

```csharp
// CQRS/ITransactionalCommand.cs
/// <summary>Opt-in marker cho TransactionBehavior.</summary>
public interface ITransactionalCommand { }
```

Hai interface rỗng này trông vô nghĩa. Chúng không vô nghĩa.

**`IPlatformRequest` giải quyết bài toán chung sống.** Mọi behavior đều ràng buộc
`where TRequest : IRequest<TResponse>, IPlatformRequest`. Nếu bỏ ràng buộc này,
behavior sẽ bọc **mọi** `IRequest` trong process — kể cả request của thư viện thứ
ba, của legacy `VNR.Core.Application`, của MediatR notification nội bộ. Kết quả:
`ExceptionHandlingBehavior` cố ép response của thư viện lạ thành `ApiResult` và
ném `InvalidCastException` ở chỗ không ai ngờ. Successor ghi thẳng lý do trong
code: *"tránh conflict với VNR.Core.Application requests"*.

**`ITransactionalCommand` là opt-in, không phải opt-out.** Không phải command nào
cũng cần transaction (query thì chắc chắn không). Mặc định-có-transaction nghĩa là
mọi `GET` cũng mở transaction DB → connection pool cạn dưới tải mà không ai hiểu
tại sao. Command nào ghi nhiều bảng thì tự khai thêm interface — một dòng, tường
minh, grep ra được.

---

## 4. A2 — Envelope: shape của mọi response

### 4.1 `ErrorCode` — giá trị enum **chính là** HTTP status

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
    TooManyRequests     = 429,   // rate limit — CHỈ middleware sinh (thêm 2026-08-21)
    SystemError         = 500,
}
```

Ba quyết định trong 10 dòng này:

**(a) Giá trị = mã HTTP.** `BaseApiController.MapToHttpStatusCode` chỉ cần
`(int)code`. Không có bảng map thứ hai để lệch. Muốn thêm 429 Too Many Requests?
Thêm một member, không sửa chỗ nào khác.

**(b) On-wire là *tên* member, không phải số.** `[JsonConverter(JsonStringEnumConverter)]`
làm response trả `"code": "BusinessRuleError"` chứ không phải `"code": 422`. Lý do:
số 422 đã có sẵn ở HTTP status line — lặp lại nó trong body là thừa; còn tên thì
đọc log thấy ngay, không cần tra bảng.

**(c) `ErrorCode` là *category*, `BusinessCode` là *chi tiết*.** `ErrorCode` cố
định 8 giá trị, không bao giờ nở ra. Mọi sự phong phú của lỗi nghiệp vụ nằm ở
`BusinessCode` (string, `"ORG.COMPANY.DUPLICATE_TAX_CODE"`). Trộn hai thứ này —
kiểu thêm `ErrorCode.CompanyTaxCodeDuplicated` — là con đường dẫn tới enum 200
member và một `switch` khổng lồ ở FE.

### 4.2 `ApiResultStatus` — 4 nhóm, phục vụ FE chứ không phục vụ BE

```csharp
[JsonConverter(typeof(JsonStringEnumConverter<ApiResultStatus>))]
public enum ApiResultStatus { SUCCESS, VALIDATION_ERROR, BUSINESS_ERROR, SYSTEM_ERROR }
```

| Status | Nghĩa | FE làm gì |
| --- | --- | --- |
| `SUCCESS` | | render data |
| `VALIDATION_ERROR` | FluentValidation fail | **bind lỗi vào từng field trên form** (nhờ `Fields`) |
| `BUSINESS_ERROR` | vi phạm nghiệp vụ, not found, trùng | toast/dialog, giữ nguyên form |
| `SYSTEM_ERROR` | exception, hạ tầng chết, timeout | trang lỗi + hiện `LogId` cho user đọc cho support |

Câu hỏi đúng khi thiết kế enum này không phải "có bao nhiêu loại lỗi" mà là
**"FE có bao nhiêu cách phản ứng khác nhau"**. Câu trả lời là 4. Thêm loại thứ 5
chỉ hợp lệ khi có cách phản ứng thứ 5.

### 4.3 Nối `ErrorCode` → `ApiResultStatus`: một nguồn duy nhất

```csharp
// Results/ApiErrorIds.cs
public static ApiResultStatus StatusForCode(ErrorCode code) => code switch
{
    ErrorCode.ValidationError => ApiResultStatus.VALIDATION_ERROR,
    ErrorCode.SystemError     => ApiResultStatus.SYSTEM_ERROR,
    _                         => ApiResultStatus.BUSINESS_ERROR,
};

/// Trả (LogId, TraceId) — format "{ProcessShortName}-{TraceId7}", vd "Platform-e465b10"
public static (string? LogId, string? TraceId) Build() { … }
```

`Status` **không bao giờ được gán bằng tay tại call site**. Nó luôn được suy ra từ
`ErrorCode`. Nếu để mỗi chỗ tự gán, sẽ có handler trả `Code=NotFound` +
`Status=SYSTEM_ERROR` và FE hiện trang lỗi đỏ cho một mã hàng không tồn tại.

Lý do `ApiErrorIds` là `static` dùng chung cho cả `ExceptionHandlingBehavior` lẫn
`BaseApiController.HandleRequest`, ghi nguyên văn trong Successor:

> *"Tách 2 bản = cùng 1 lỗi trả 2 `Status` khác nhau và 2 format TraceId khác nhau
> tuỳ đi đường nào → admin không grep được."*

Đây là hệ quả trực tiếp của Nguyên tắc 6 ở [00-lo-trinh](00-lo-trinh-tong-the.md)
("một luật = một nguồn"), áp cho error handling.

### 4.4 `ErrorDescriptor` — thay thế magic string

```csharp
public sealed record ErrorDescriptor(
    string    BusinessCode,          // "{MODULE}.{ENTITY}.{ERROR}" — UPPER_SNAKE có chấm
    ErrorCode ErrorCode,
    string    MessageTemplate,       // "{0}", "{1}" placeholder cho string.Format
    bool      Retryable = false,
    string?   TranslationModule = null)
{
    public string FormatMessage(params object[] args) => …;

    /// Dịch theo locale: tra BusinessCode qua ITranslationService, miss thì fallback
    /// về MessageTemplate, rồi string.Format với args.
    public string Resolve(ITranslationService? translator, params object[] args) => …;
}
```

Khai báo tập trung cạnh slice, **không** rải rác trong handler:

```csharp
// Modules/Organization/Application/Company/CompanyErrors.cs
public static class CompanyErrors
{
    public static readonly ErrorDescriptor NotFound = new(
        "ORG.COMPANY.NOT_FOUND", ErrorCode.NotFound,
        "Không tìm thấy công ty.", TranslationModule: "Organization");

    public static readonly ErrorDescriptor DuplicateTaxCode = new(
        "ORG.COMPANY.DUPLICATE_TAX_CODE", ErrorCode.Conflict,
        "Mã số thuế {0} đã được sử dụng.", TranslationModule: "Organization");
}
```

Bốn thứ có được ngay khi lỗi là một object thay vì một chuỗi:

| | Với `string` message | Với `ErrorDescriptor` |
| --- | --- | --- |
| i18n | phải sửa mọi handler | đổi file `i18nOrganization.en.json`, code không đụng |
| Đếm lỗi/alert | phải regex message tiếng Việt trong log | `GROUP BY businessCode` |
| HTTP status | tự nhớ trả 404 hay 422 | nằm ngay trong descriptor |
| FE xử lý riêng 1 lỗi | so sánh chuỗi (vỡ khi sửa chính tả) | `if (code === 'ORG.COMPANY.DUPLICATE_TAX_CODE')` |

**`Resolve()` là điểm duy nhất chứa logic fallback dịch.** Successor ghi rõ nó là
*"Nguồn DUY NHẤT cho cả handler (`BaseResponse.Fail`) lẫn pipeline
(`ExceptionHandlingBehavior`) — tránh 3 chỗ copy-paste cùng logic fallback."*

**`Retryable` không phải trang trí.** `false` = client retry cũng vô ích (sai dữ
liệu), `true` = lỗi tạm (deadlock, timeout gọi service ngoài) → FE/worker có thể
tự thử lại. Không có field này thì retry policy phải đoán theo status code.

### 4.5 `FieldError` — và một luật đặt tên dễ làm sai

```csharp
public sealed record FieldError(
    string    Field,      // PascalCase, khớp property đã serialize
    string?   Code,       // "Validation.MaxLength" hoặc "ORG.COMPANY.INVALID_TAX_CODE"
    string    Message,    // ĐÃ DỊCH, nhưng KHÔNG chứa nhãn field
    object[]? Args = null);
```

> **`Message` không được chứa tên field.** Trả `"Mã số thuế không được vượt quá
> 100 ký tự."` là sai; đúng là `"Không được vượt quá {0} ký tự."` + `Args=[100]`.

Vì sao: nhãn field đã được FE fetch sẵn từ `FieldName.*` để hiển thị trên form.
Nếu BE cũng nhúng nhãn vào message thì cùng một nhãn tồn tại ở 2 nơi, dịch 2 lần,
và khi đổi "Mã số thuế" → "MST" thì form đổi còn thông báo lỗi thì không. `Args`
tồn tại để FE dùng có cấu trúc (set `maxlength` cho input, highlight số).

Cùng lý do đó, `CommonErrors` giữ template trần:

```csharp
public static readonly ErrorDescriptor MaxLength = new(
    "COMMON.MAX_LENGTH", ErrorCode.ValidationError,
    "Không được vượt quá {0} ký tự.", TranslationModule: "Common");
```

7 descriptor dùng chung: `Required`, `MaxLength`, `RangeInvalid`, `InvalidEmail`,
`OutOfRange`, `MinValue`, `InvalidValue`.

> **Luật casing** (Successor ghi trong doc của `CommonErrors`): `BusinessCode`
> luôn **UPPER_SNAKE có chấm** — kể cả lỗi validation — để cùng namespace với lỗi
> nghiệp vụ, nhờ đó `FieldError.Code` trả về FE chỉ có **một** kiểu casing.
> PascalCase **chỉ** dành cho nhãn `FieldName.*`.

### 4.6 `IApiResult<T>` — read-only, cộng 2 seam non-generic

```csharp
public interface IHasApiResultStatus            // đọc Status mà không cần biết T
{
    ApiResultStatus Status { get; }
    ErrorCode       Code   { get; }
}

public interface IApiResultEnrichable           // host gán TraceId mà không reflection
{
    string? TraceId { get; set; }
}

public interface IApiResult<T> : IHasApiResultStatus
{
    T?      Data          { get; }
    string? Message       { get; }
    new ApiResultStatus Status { get; }
    string? BusinessCode  { get; }              // "{MODULE}.{ENTITY}.{ERROR}"
    string? TraceId       { get; }
    bool?   Retryable     { get; }
    string? LogId         { get; }
    Dictionary<string, FieldError[]>? Fields { get; }
}

public interface IApiResult : IApiResult<object> { }
```

Ba điểm thiết kế:

- **Interface chỉ có getter.** Handler tạo response qua factory rồi trả; không ai
  ở tầng trên sửa được nó nửa chừng.
- **`IHasApiResultStatus`** cho phép `ModuleTransactionBehavior` hỏi "thành công
  không?" mà không cần biết `T` — thứ khiến §6.4 khả thi.
- **`IApiResultEnrichable`** là ngoại lệ có kiểm soát cho đúng một field: host cần
  gán `TraceId` ở mọi request (hot path). Không có seam này, `BaseApiController`
  phải `GetProperty("TraceId").SetValue(...)` bằng reflection **mỗi request**.

### 4.7 `ApiResult<T>` — và mẹo hai serializer

```csharp
public class ApiResult<T> : IApiResult<T>, IApiResultEnrichable
{
    [JsonPropertyName("businessCode")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? BusinessCode { get; set; }
    // … TraceId, Retryable, LogId, Fields tương tự

    public bool ShouldSerializeBusinessCode() => BusinessCode is not null;
    public bool ShouldSerializeTraceId()      => TraceId      is not null;
    public bool ShouldSerializeRetryable()    => Retryable    is not null;
    public bool ShouldSerializeLogId()        => LogId        is not null;
    public bool ShouldSerializeFields()       => Fields       is not null;

    public static ApiResult<T> Success(T? data, string? message = null) => …;
    public static ApiResult<T> BusinessError(string message, T? data = default) => …;
    public static ApiResult<T> BusinessError(ErrorDescriptor error, string message, T? data = default) => …;
}

public class ApiResult : ApiResult<object>, IApiResult { }
```

Nhìn thì như code thừa — hai cơ chế cho cùng một việc "bỏ field null". Không thừa.
Nguyên văn lý do trong Successor:

> *"MVC serialize bằng Newtonsoft → KHÔNG đọc attribute STJ; nó honor convention
> `ShouldSerialize{Prop}()` (không cần Newtonsoft dependency ở Platform). Nhờ đó 2
> path trả CÙNG shape (null field bị omit), FE luôn parse 1 kiểu."*

Trong một app ASP.NET Core thật có **hai** đường response ra ngoài: middleware
(dùng `System.Text.Json`) và MVC pipeline (có thể đã cấu hình Newtonsoft cho
legacy). Nếu chỉ khai attribute STJ, response đi đường MVC sẽ có
`"logId": null, "fields": null` còn đường middleware thì không → FE gặp shape
khác nhau tuỳ lỗi xảy ra ở đâu. Convention `ShouldSerialize*()` là cách duy nhất
bảo Newtonsoft bỏ field **mà không** kéo package Newtonsoft vào `Platform`.

> **[ĐƠN GIẢN HOÁ]** Hệ thống mới chỉ dùng STJ ở mọi nơi → bỏ được 5 method
> `ShouldSerialize*`. Nhưng phải ghi vào ADR: *"chỉ dùng STJ"*. Ngày ai đó thêm
> `.AddNewtonsoftJson()` để đỡ một thư viện legacy, họ cần biết mình vừa phá cái gì.

### 4.8 `BaseResponse` — API mà handler thật sự dùng

Handler **không** gọi `ApiResult<T>.Success(...)` trực tiếp. Nó kế thừa
`BaseResponse` và dùng 4 method:

```csharp
public abstract class BaseResponse
{
    protected virtual ITranslationService? Translator => null;

    protected IApiResult<T> Ok<T>(T data, string? message = null)
        => ApiResult<T>.Success(data, message);

    protected IApiResult<T> Fail<T>(string message, T? data = default)
        => ApiResult<T>.BusinessError(message, data);

    protected IApiResult<T> Fail<T>(ErrorDescriptor error, params object[] args)
        => ApiResult<T>.BusinessError(error, error.Resolve(Translator, args));

    protected IApiResult<BaseResponseGridModel<T>> ResultKendoGrid<T>(
        PagedResult<T> result, string? message = null) where T : BaseDto => …;
}
```

Bề mặt API chỉ có 2 động từ — `Ok` và `Fail`. Handler viết ra đọc như tiếng Việt:

```csharp
if (company is null)
    return Fail<CompanyResponse>(CompanyErrors.NotFound);

if (await _repo.ExistsTaxCodeAsync(request.TaxCode, ct))
    return Fail<CompanyResponse>(CompanyErrors.DuplicateTaxCode, request.TaxCode);

return Ok(_mapper.ToResponse(company));
```

Overload `Fail<T>(string)` tồn tại cho lỗi dùng một lần thật sự. **Đừng để nó
thành mặc định** — mỗi lần dùng nó là mất i18n và mất `businessCode`. Một cách
kiểm soát rẻ: ArchTest đếm số call site `Fail<...>(string)`, cảnh báo khi vượt
ngưỡng (vd 20).

---

## 5. A3 — CQRS: mỏng vì envelope đã gánh hết

### 5.1 Command

```csharp
public interface ICommand : IRequest, IPlatformRequest { }
public interface ICommand<TResult> : IRequest<IApiResult<TResult>>, IPlatformRequest { }

public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand>
    where TCommand : class, ICommand { }

public interface ICommandHandler<in TCommand, TResult>
    : IRequestHandler<TCommand, IApiResult<TResult>>
    where TCommand : class, ICommand<TResult>
    where TResult  : notnull { }
```

`ICommand<TResult>` khai `IRequest<IApiResult<TResult>>` — envelope được **ép ở
tầng type**, không phải quy ước. Handler không thể "quên" trả envelope; nó sẽ
không biên dịch.

### 5.2 Query — 4 họ, và một bài học

```csharp
public interface IQuery<TResult> : IRequest<IApiResult<TResult>>, IPlatformRequest { }

public interface IQueryList<TResult> : IRequest<IApiResult<TResult>>, IPlatformRequest
    where TResult : class { }

// Pattern MỚI: trả PagedResult<T> trần — Kendo-free, type-safe
public interface IQueryListGrid<TRequest, TResult> : IRequest<PagedResult<TResult>>, IPlatformRequest
    where TRequest : class, IGridRequest, new()
    where TResult  : class
{ TRequest Request { get; } }

// Pattern CŨ: bọc envelope + model riêng của Kendo
public interface IQueryListGridLegacy<TResult>
    : IRequest<IApiResult<BaseResponseGridModel<TResult>>>, IPlatformRequest
    where TResult : BaseDto { }
```

> ⚠️ **Đây chính là bằng chứng code cho cảnh báo ở
> [00-lo-trinh §4](00-lo-trinh-tong-the.md#4-thứ-tự-phụ-thuộc--vì-sao-đúng-thứ-tự-đó)**
> ("viết module trước khi chốt envelope → grid trả shape khác endpoint thường").
> Successor đang tồn tại **hai** shape cho grid: `PagedResult<T>` trần và
> `IApiResult<BaseResponseGridModel<T>>`. FE phải có 2 nhánh parse, và mỗi lần
> thêm field vào envelope (`traceId`, `logId`) phải nhớ nhánh grid trần **không**
> có chúng.
>
> **Hệ mới: chọn MỘT.** Khuyến nghị `IApiResult<PagedResult<T>>` — grid cũng là
> một response như mọi response, không có lý do gì để nó đặc biệt. Nếu chọn trả
> `PagedResult<T>` trần thì phải chấp nhận `BaseApiController` có nhánh riêng để
> chèn `TraceId` cho nó (Successor làm đúng như vậy, và đó là chi phí phải trả mãi).
>
> ✅ **PlatformManager đã chốt (2026-08-23):** `IApiResult<PagedList<T>>`, với
> `PagedList<T>` = `{ items, page, pageSize, totalCount }`. Khác VNR hai điểm:
> tên là **`PagedList`** (không phải `PagedResult`), và **không có `TotalPages`**
> — nó suy ra được từ `totalCount`/`pageSize`, nên gửi kèm chỉ tạo hai nguồn có
> thể lệch. Sentinel `TotalPages = -1` ở §5.4 vì vậy **không áp dụng**.
> Định nghĩa chuẩn: `doc/huong_dan/quy-uoc/be-cqrs-handler.md` §Shape phân trang.

### 5.3 Base handler

```csharp
public abstract class CommandHandler<TCommand, TResult>
    : BaseResponse, IRequestHandler<TCommand, IApiResult<TResult>>
    where TCommand : ICommand<TResult>
{
    protected readonly IApplicationContext? Context;

    protected CommandHandler() { }
    protected CommandHandler(IApplicationContext context) => Context = context;

    protected override ITranslationService? Translator => Context?.Translation;

    public abstract Task<IApiResult<TResult>> Handle(TCommand request, CancellationToken ct);
}
```

`QueryHandler<TQuery, TResult>` giống hệt, thêm `where TResult : class`.

Base class này **chỉ làm 2 việc**: cho handler dùng `Ok`/`Fail`, và nối
`Translator` vào `Context.Translation` để `Fail(descriptor)` tự dịch. Nó
**không** chứa logic nghiệp vụ, không có method `protected` nào để handler con
gọi. Đó là ranh giới phân biệt "base class hợp lệ" với "base handler tầng giữa"
bị cấm ở [06-p5](06-p5-module-dau-tien.md).

Có 2 constructor (một rỗng) vì nhiều handler đơn giản không cần i18n/cache/user
— bắt chúng inject `IApplicationContext` chỉ để gọi `base(context)` là nghi thức
rỗng.

### 5.4 `PagedResult<T>` — và cái sentinel dễ mất

```csharp
public class PagedResult<T> : IPagedResult
{
    public IReadOnlyList<T> Data       { get; }
    public int              TotalCount { get; }
    public int              PageIndex  { get; }
    public int              PageSize   { get; }
    public int              TotalPages { get; }

    // …
    TotalPages = totalCount < 0
        ? -1                                                   // ← sentinel
        : pageSize > 0 ? (int)Math.Ceiling(totalCount / (double)pageSize) : 0;
}
```

> *"Total < 0 = sentinel 'không đếm lại' (`BaseRequestGridModel.SkipTotal`) →
> `TotalPages` PHẢI sentinel theo, KHÔNG được là 0: `Ceiling(-1 / 20.0) = 0` làm
> FE bind pager … sập về '0 trang' mỗi lần lật trang."*

Bối cảnh: `COUNT(*)` trên bảng lớn đắt hơn cả câu lấy data. Từ trang 2 trở đi FE
đã biết tổng số rồi, nên gửi `SkipTotal=true` để BE khỏi đếm lại; BE trả
`TotalCount = -1` nghĩa là "không đổi, dùng lại giá trị cũ". Một phép
`Math.Ceiling` vô tư sẽ biến `-1` thành `0` và pager biến mất.

`IPagedResult` non-generic tồn tại cho consumer runtime-generic (export
dispatcher) đọc `DataObject`/`TotalCount` **mà không cần reflection với
magic-string** `GetProperty("TotalCount")`.

---

## 6. A4 — Bốn behavior: phần khó nhất của P2

### 6.1 Thứ tự đăng ký — nguồn duy nhất đáng tin

Đây là đoạn code quan trọng nhất của cả file này, trích nguyên từ
`Hosting.CompositionRoot/BaseStartupServices.cs:141–160`:

```csharp
.RegisterCommandDispatcher()
// MediatR pipeline order (outermost → innermost) — first registered = outermost:
// 1. ExceptionHandling  — catch ALL exceptions, wrap thành ApiResult (outermost safety net)
// 2. LoggingBehavior    — structured log request/elapsed (IPlatformRequest only)
// 3. ValidationBehavior — FluentValidation cho IPlatformRequest (Commands/Queries)
// 4. TransactionBehavior — PER-MODULE (ADR-014)
// 5. CrudHandlerBehavior — fallback handler khi không có explicit handler
// 6. AuditLogBehavior   — đăng ký trong AddAuditCoreSetup (gọi SAU) ⇒ innermost
.RegisterExceptionHandlingBehavior()
.RegisterLoggingBehavior()
.RegisterPlatformValidators([.. assemblies])
.RegisterPlatformValidationBehavior()
.RegisterModuleTransactionBehaviors()
.RegisterCrudHandlerBehavior()
.AddVnrOpenTelemetry();
```

```
request
  └─ 1 ExceptionHandling ─┐
       └─ 2 Logging ──────┐
            └─ 3 Validation ──┐
                 └─ 4 Transaction ──┐
                      └─ 5 CrudHandler ──┐
                           └─ 6 AuditLog ──┐
                                └── HANDLER
```

> ### ⚠️ Bẫy: XML doc trong chính Successor đang SAI
>
> `LoggingBehavior` ghi *"position 1 — outermost"*. `ValidationBehavior` ghi
> *"position 2"*. `ExceptionHandlingBehavior` ghi *"Pipeline order: Logging →
> Validation → Transaction → Exception → Handler"*. **Cả ba mâu thuẫn với thứ tự
> đăng ký thật ở trên** — chúng là di tích của một lần đổi thứ tự mà không ai sửa
> comment.
>
> **Bài học vận hành:** thứ tự pipeline là **thuộc tính của Composition Root**,
> không phải của class. Comment trong class không thể đúng lâu vì tác giả class
> không kiểm soát nơi nó được đăng ký. Nơi duy nhất đáng ghi và đáng tin là chỗ
> đăng ký. Đây cũng là lý do `doc/huong_dan/quy-uoc/` của Successor có luật
> *"Verify bằng code, KHÔNG tin index/doc"*.

Vì sao đúng thứ tự này:

| Vị trí | Behavior | Vì sao ở đó |
| --- | --- | --- |
| 1 | Exception | Ngoài cùng thì bắt được lỗi của **chính các behavior khác** (validator ném, transaction ném). Đặt trong cùng thì lỗi của Validation lọt ra middleware → 500 trần không envelope |
| 2 | Logging | Ngoài Validation để log cả request bị chặn vì validation (nếu không, request sai sẽ không có dòng log nào) |
| 3 | Validation | Trước Transaction: dữ liệu sai thì **không mở transaction**, khỏi tốn connection |
| 4 | Transaction | Trong Validation, ngoài handler. Trong Exception là điểm sống còn — xem §6.4 |
| 5–6 | Crud, Audit | Sát handler nhất; audit ghi đúng cái handler thật sự làm |

### 6.2 `ValidationBehavior` — 20 dòng

```csharp
if (!_validators.Any()) return await next();

var context = new ValidationContext<TRequest>(request);
var results = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, ct)));
var failures = results.SelectMany(r => r.Errors).Where(f => f is not null).ToList();

if (failures.Count > 0) throw new ValidationException(failures);
return await next();
```

Behavior **ném exception**, không tự tạo response lỗi. Việc biến exception thành
`ApiResult` là của `ExceptionHandlingBehavior` — một trách nhiệm, một chỗ. Nếu
behavior này tự dựng envelope thì có 2 nơi biết format lỗi validation, và chúng sẽ
lệch nhau trong vòng 6 tháng.

`Task.WhenAll` chứ không phải `foreach await`: các validator độc lập, chạy song
song.

### 6.3 `ExceptionHandlingBehavior` — bậc thang catch và 3 bài học

```csharp
where TRequest  : IRequest<TResponse>, IPlatformRequest
where TResponse : class
```

Thứ tự catch (cụ thể → tổng quát) và ánh xạ:

| Exception | → `ErrorCode` | Ghi chú |
| --- | --- | --- |
| `ErrorDescriptorException` | *(lấy từ descriptor)* | tự mang mã của nó |
| `ConflictException` | `Conflict` (409) | |
| `BusinessException` | `BusinessRuleError` (422) | |
| `BusinessArgumentsValidatorException` | `BusinessRuleError` | legacy |
| `RequestArgumentsValidatorException` | `ValidationError` | legacy |
| `ValidationException` (FluentValidation) | `ValidationError` (400) | **+ dựng `Fields`** |
| `DomainException` | `BusinessRuleError` | `businessCode = ex.Code` |
| `UnauthorizedAccessException` | `AuthorizationError` (403) | |
| `OperationCanceledException` | *(ném lại khi `ct.IsCancellationRequested`)* | |
| `Exception` | `SystemError` (500) | + log + sinh `LogId` |

Ba bài học được ghi thành comment trong code, cả ba đều là hậu quả của một lần
làm sai đã sửa:

**(a) Không catch `ArgumentException` trần.**
> *"KHÔNG catch bare `ArgumentException` nữa — `ArgumentNullException` /
> `ArgumentException` từ BUG thật phải rơi xuống `catch(Exception)` → 500, không
> bị che thành 422."*

Một `NullReferenceException` hay `ArgumentNullException` là **bug của lập trình
viên**, không phải vi phạm nghiệp vụ. Map nó thành 422 nghĩa là: không có alert,
không có `LogId`, dashboard lỗi vẫn xanh, và user nhận thông báo "dữ liệu không
hợp lệ" cho một dòng code chết. Bug ẩn hàng tháng.

**(b) 403 không phải 422.**
> *"403 — hết quyền, KHÔNG phải vi phạm nghiệp vụ. Map về `BusinessRuleError`
> (422) khiến FE không phân biệt được 'thiếu quyền' với 'sai nghiệp vụ' → không
> thể route sang màn xin quyền."*

**(c) Không nuốt lỗi thành kết quả rỗng.** Khi `TResponse` không phải envelope
(vd `PagedResult<T>` — đúng hệ quả của §5.2), behavior **ném lại** exception gốc:
> *"KHÔNG trả `PagedResult.Empty` như trước: grid rỗng + HTTP 200 = nuốt lỗi im
> lặng, user tưởng 'không có data' trong khi SQL đang chết."*

Ngoài ra, hai chi tiết nhỏ mà quan trọng:

- `ToFieldError(ValidationFailure)` đọc `failure.CustomState is ValidationErrorState`
  → lấy `ErrorDescriptor` ra và gọi `error.Resolve(_translationService, args)`.
  Đây là mắt xích nối FluentValidation với `ErrorDescriptor` (xem §6.6).
- `NormalizeField` **chỉ** bỏ tiền tố `"Request."`, **giữ nguyên PascalCase**, vì
  *"API serialize PascalCase (Newtonsoft `DefaultNamingStrategy`)"*. Nếu behavior
  camelCase hoá trong khi API trả PascalCase thì FE bind lỗi vào field không tồn tại.
  **Luật: tên field trong `Fields` phải khớp đúng cách JSON đã serialize — quyết
  định một lần ở P4 rồi giữ nguyên.**

### 6.4 `ModuleTransactionBehavior` — chỗ dễ mất dữ liệu nhất

```csharp
public abstract class ModuleTransactionBehaviorBase<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>, ITransactionalCommand
{
    protected ModuleTransactionBehaviorBase(ITransactionManager tm, ILogger logger) { … }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        try
        {
            return await _transactionManager.ExecuteInTransactionAsync(async () =>
            {
                var response = await next();

                if (response is IHasApiResultStatus { Status: not ApiResultStatus.SUCCESS })
                    throw new TransactionRollbackSignalException(response);   // ép rollback

                return response;
            }, ct);
        }
        catch (TransactionRollbackSignalException signal)
        {
            return (TResponse)signal.Response;    // rollback xong, trả response gốc
        }
    }
}
```

**Vì sao phải có cái signal-exception trông kỳ quặc này:**

> *"Handler trả `Fail<T>(...)` KHÔNG throw (Result pattern) ⇒ transaction manager
> thấy 'thành công' và COMMIT. Nguy hiểm khi handler ĐÃ gọi `SaveChangesAsync` ở
> bước trước rồi mới quyết định thất bại ở bước sau."*

Kịch bản cụ thể:

```csharp
await _repo.AddAsync(order, ct);
await _uow.SaveChangesAsync(ct);                    // ① đã ghi DB

var payment = await _paymentGateway.ChargeAsync(...);
if (!payment.Ok)
    return Fail<OrderResponse>(OrderErrors.PaymentFailed);   // ② KHÔNG throw
```

Không có signal-exception: transaction commit ở ②. Đơn hàng nằm trong DB, tiền
chưa thu, response báo thất bại. Kết hợp Result pattern (không ném) với transaction
(rollback bằng exception) **bắt buộc** phải có cây cầu này.

Đây cũng là nơi `IHasApiResultStatus` (§4.6) trả công: behavior generic không biết
`T`, nhưng vẫn hỏi được "thành công không?" mà không reflection.

### 6.5 Đăng ký transaction behavior — vì sao phải hoãn

`ModuleTransactionBehaviorExtensions` không đăng ký ngay, mà bỏ vào một túi chờ:

```csharp
// Module gọi trong Add{Module}Infrastructure — chỉ KHAI BÁO
services.AddModuleTransactionBehavior(typeof(OrganizationTransactionBehavior<,>));

// Composition Root gọi ĐÚNG VỊ TRÍ trong chuỗi — mới thật sự đăng ký
services.RegisterModuleTransactionBehaviors();
```

`AddModuleTransactionBehavior` kiểm tra type là open-generic 2 tham số và có
implement `IPipelineBehavior<,>` (fail-fast lúc startup thay vì lỗi DI khó hiểu
lúc runtime), rồi thêm vào singleton `PendingModuleTransactionBehaviors`.
`RegisterModuleTransactionBehaviors` **gỡ** descriptor đó khỏi container — *"đây
là state lúc compose, KHÔNG phải service runtime"* — và flush từng type.

Vì sao không để module tự `AddTransient(typeof(IPipelineBehavior<,>), …)`:

```
RegisterModules() chạy TRƯỚC core wire
   ⇒ transaction behavior đăng ký đầu tiên
   ⇒ transaction thành OUTERMOST, bọc luôn ExceptionHandling
   ⇒ mọi exception bị ExceptionHandling nuốt thành ApiResult (không ném ra ngoài)
   ⇒ transaction KHÔNG BAO GIỜ thấy exception
   ⇒ COMMIT dữ liệu của lệnh ĐÃ THẤT BẠI
```

Không có test nào đỏ. Không có log nào đỏ. Chỉ có dữ liệu sai trong DB.

> **Nguyên tắc rút ra:** *thứ tự pipeline phải do một nơi duy nhất quyết định.*
> Bất kỳ cơ chế nào cho phép module tự chèn mình vào pipeline theo thứ tự load
> assembly đều là bom hẹn giờ. Đây là nội dung ADR-014 của Successor.

**Vì sao per-module chứ không một transaction dùng chung:** một process có thể
host nhiều module, mỗi module một `DbContext` **(VNR — PlatformManager dùng 1 DbContext chung, xem 00-lo-trinh mục KHÔNG áp dụng)**. Một `ITransactionManager` dùng
chung đăng ký trong DI sẽ bị **last-wins** — module đăng ký sau ghi đè module
trước, và transaction của module A chạy trên `DbContext` của module B. Vì thế mỗi
module kế thừa `ModuleTransactionBehaviorBase` và truyền `ITransactionManager`
**của riêng nó**.

### 6.6 `LoggingBehavior` — chi tiết nhỏ đáng sao chép

```csharp
public partial class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>, IPlatformRequest
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Handling {RequestName}")]
    static partial void LogHandling(ILogger logger, string requestName);
    // … LogHandled(elapsed), LogHandleError
}
```

- `[LoggerMessage]` source generator: *"zero allocation, 30–50% faster so với
  `_logger.LogXxx()` thông thường khi log level tắt"* — vì template được biên dịch
  sẵn, không boxing tham số, không format khi level bị tắt.
- **Không log payload.** *"Không log payload để tránh rò rỉ PII."* Log tên request
  + thời gian là đủ để dò hiệu năng; log body nghĩa là CCCD/lương/số tài khoản nằm
  trong file log và trong hệ thống log tập trung, ngoài tầm kiểm soát quyền.
- Behavior **ném lại** exception sau khi log. Log rồi nuốt là cách chắc chắn nhất
  để mất lỗi.

### 6.7 Nối `ErrorDescriptor` vào FluentValidation

Để `Fields` có `Code` machine-readable thay vì chuỗi tiếng Việt:

```csharp
// Validation/ValidationErrorState.cs — mang descriptor + args qua CustomState
// Validation/ValidationMessageExtensions.cs — extension .WithError(...)

RuleFor(x => x.Name)
    .NotEmpty().WithError(CommonErrors.Required)
    .MaximumLength(OrgFieldLengths.NameMaxLength)
        .WithError(CommonErrors.MaxLength, OrgFieldLengths.NameMaxLength);
```

`ExceptionHandlingBehavior.ToFieldError` đọc `CustomState` ra, gọi
`Resolve(translator, args)`, và ráp thành `FieldError`. Không có mắt xích này thì
`.WithMessage("Không được vượt quá 100 ký tự")` là magic string — mất i18n, mất
`Code`, và độ dài `100` bị lặp ở validator và ở EF config.

Chú ý `OrgFieldLengths.NameMaxLength` dùng cho **cả** validator lẫn `HasMaxLength`
ở P3 — Nguyên tắc 6 ("một luật = một nguồn").

---

## 7. A5 — Context, Contracts, Interfaces

### 7.1 `IApplicationContext` — đúng 3 member

```csharp
public interface IApplicationContext
{
    ICurrentUser        CurrentUser { get; }   // resolve từ JWT claims, không round-trip DB
    ICacheService       Cache       { get; }
    ITranslationService Translation { get; }
}
```

> *"Handler tự inject `IMapper`, `IUnitOfWork`, `IMediator`, `IHttpContextAccessor`
> trực tiếp qua constructor khi cần."*

`IApplicationContext` gom đúng những thứ **hầu như handler nào cũng cần** và
**không có gì thay thế được**. Cám dỗ lớn nhất trong đời một interface như thế này
là phình ra thành God Object: thêm `IUnitOfWork` cho tiện, thêm `IMediator` cho
tiện… rồi mọi handler phụ thuộc mọi thứ, unit test phải mock 12 thành viên, và
không nhìn vào constructor nào biết được handler thật sự dùng gì.

**Không có khái niệm HTTP trong đây.** Không `HttpContext`, không `IPrincipal`.
`ICurrentUser` là abstraction thuần Application — nhờ đó cùng handler chạy được
trong background job, trong CLI, trong test mà không dựng HTTP giả.

### 7.2 `BaseDto`

```csharp
public abstract class BaseDto
{
    public Guid            Id         { get; set; }
    public string?         UserCreate { get; set; }
    public string?         UserUpdate { get; set; }
    public DateTimeOffset? DateCreate { get; set; }
    public DateTimeOffset? DateUpdate { get; set; }
    public bool?           IsDelete   { get; set; }
}
```

> *"Không kế thừa `BaseEntityModel` để tránh DataAnnotation attributes không phù
> hợp với DTO layer."*

DTO là bản sao **có chủ đích** của entity, không phải entity tái sử dụng. Dùng lại
entity làm DTO thì `[Column]`, `[Required]`, navigation property, lazy loading
proxy đều rò ra API — và một ngày nào đó thêm field nội bộ vào entity là vô tình
publish nó ra ngoài.

### 7.3 Interfaces — khai theo nhu cầu, không khai trước

Rời P2 chỉ cần 3:

```csharp
public interface ICurrentUser { Guid? UserId { get; } string? UserName { get; } … }
public interface ICacheService { Task<T?> GetAsync<T>(string key, …); … }
public interface ITranslationService { string TranslateSync(string key, string module); … }
```

`Interfaces/Data/*` (11 file), `Jobs/*` (19), `Logging/*` (11)… của Successor là
kết quả tích luỹ nhiều năm. Copy chúng sang hệ mới ở P2 sẽ tạo ~60 interface không
có implementation — và mọi người sẽ tưởng hệ thống đã có job scheduler, có search
engine, có notification, trong khi không có gì cả.

**Luật: một interface chỉ được vào `Application/Interfaces/` khi biết ai sẽ
implement nó và ở phase nào.**

---

## 8. Những gì TUYỆT ĐỐI không được vào `Platform.Application`

| Cấm | Vì sao | Bắt bằng |
| --- | --- | --- |
| `Microsoft.EntityFrameworkCore` | Có EF là có `.Include()`, `.AsNoTracking()` trong handler → Application dính chặt vào ORM, không test được nếu không có DB | ArchTest `Application_MustNotDependOn_EntityFrameworkCore` |
| `Microsoft.AspNetCore.*` | `HttpContext` trong handler = handler không chạy được ngoài web (job, CLI, test) | `Application_MustNotDependOn_AspNetCore` |
| Reference tới `Platform.Persistence` | Đảo chiều phụ thuộc — Application phải **định nghĩa** interface, Persistence **implement** | `Application_MustNotDependOn_Persistence` |
| `Newtonsoft.Json` | Xem §4.7 — Platform trung lập với serializer, chỉ dùng convention | ArchTest hoặc review `.csproj` |
| Logic nghiệp vụ cụ thể | Đây là **Platform**, không phải Module. Không có `CompanyErrors` ở đây | review |
| Connection string, config đọc trực tiếp | Config là chuyện của Composition Root | review |
| Base handler tầng giữa (`CreateBusinessHandler<,,,>`) | Xem [06-p5](06-p5-module-dau-tien.md) — chỉ 2 cực | analyzer / review |

---

## 9. ArchTests cần thêm ở P2

Nối tiếp 2 test đã có từ P0:

```csharp
public class ApplicationLayerTests
{
    private static readonly Assembly App = typeof(ICommand<>).Assembly;

    [Fact]
    public void Application_MustNotDependOn_Infrastructure()
        => Types.InAssembly(App)
            .Should().NotHaveDependencyOnAny(
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore",
                "Newtonsoft.Json",
                "VNR.Platform.Persistence")
            .GetResult().IsSuccessful.Should().BeTrue();

    [Fact]
    public void Handlers_MustBe_SealedOrAbstract()          // tránh kế thừa handler
        => Types.InAssembly(App)
            .That().ImplementInterface(typeof(IRequestHandler<,>))
            .Should().BeSealed()
            .GetResult().IsSuccessful.Should().BeTrue();

    [Fact]
    public void Behaviors_MustBe_Constrained_To_IPlatformRequest()
    {
        // mọi IPipelineBehavior<,> trong Platform phải ràng buộc IPlatformRequest —
        // nếu không sẽ bọc cả request của thư viện thứ ba (xem §3)
    }
}
```

**Test quan trọng nhất của P2 lại không phải ArchTest mà là integration test cho
thứ tự pipeline.** Đây là thứ ArchTest không bắt được:

```csharp
[Fact]
public async Task Pipeline_Order_Must_Be_Exception_Outermost()
{
    // Arrange: DI thật, handler ném BusinessException
    // Assert: nhận được ApiResult với Status=BUSINESS_ERROR (KHÔNG phải exception thoát ra)
}

[Fact]
public async Task Failed_Result_Must_Rollback_Transaction()
{
    // Arrange: handler SaveChangesAsync rồi return Fail<T>(...)
    // Assert: bản ghi KHÔNG tồn tại trong DB (xem §6.4)
}
```

Hai test này bắt đúng hai lỗi mà cả compiler lẫn ArchTest đều mù, và cả hai đều
là lỗi **mất/sai dữ liệu**, không phải lỗi hiển thị.

---

## 10. Checklist rời P2

- [ ] `IPlatformRequest` / `ITransactionalCommand` có, và **mọi** behavior đều ràng buộc `IPlatformRequest`
- [ ] `ErrorCode` có giá trị = HTTP status; on-wire là string (`JsonStringEnumConverter`)
- [ ] `Status` được suy ra từ `ErrorCode` qua **một** hàm (`ApiErrorIds.StatusForCode`), không nơi nào gán tay
- [ ] `ErrorDescriptor` có, và ít nhất một slice mẫu dùng `{Aggregate}Errors` thay cho string
- [ ] `FieldError.Message` **không** chứa nhãn field; `Args` được điền
- [ ] `IApiResult<T>` chỉ có getter; có `IHasApiResultStatus` + `IApiResultEnrichable`
- [ ] Chốt **một** chính sách serializer, ghi vào ADR (nếu dùng cả 2 → có `ShouldSerialize*`)
- [ ] `BaseResponse` là bề mặt duy nhất handler dùng để tạo response
- [ ] Chốt **một** shape cho grid — không lặp lại `IQueryListGrid` vs `Legacy` (§5.2)
- [ ] `PagedResult.TotalPages` giữ sentinel `-1` khi `TotalCount < 0`
- [ ] 4 behavior đăng ký **đúng thứ tự**, comment thứ tự nằm ở **Composition Root**, không nằm trong class
- [ ] Transaction behavior đăng ký qua cơ chế **hoãn**, không để module tự chèn
- [ ] `Fail()` không throw vẫn **rollback** được (`TransactionRollbackSignalException`)
- [ ] `ExceptionHandlingBehavior` **không** catch `ArgumentException` trần; 403 map `AuthorizationError`; không nuốt lỗi thành kết quả rỗng
- [ ] `IApplicationContext` đúng 3 member, không có type HTTP nào trong cả layer
- [ ] `Interfaces/` chỉ chứa interface **sắp có** implementation
- [ ] ArchTests §9 xanh, và đã **kiểm chứng bằng cách làm nó đỏ**
- [ ] 2 integration test pipeline (thứ tự + rollback) xanh
- [ ] Chạy được `PingCommand` qua MediatR trả đúng envelope (Definition of Done ở đầu file)

---

**Tiếp theo:** [04-p3-platform-persistence.md](04-p3-platform-persistence.md) — nơi
implement những interface vừa khai ở đây (`ITransactionManager`, `IGenericRepository`,
`IUnitOfWork`), cùng `BaseDbContext`, bộ interceptor (audit/soft-delete/id-gen) và
"DIP Seam" — cơ chế cho phép `Infrastructure.*` phục vụ `Module.*.Domain` mà không
được phép reference nó.

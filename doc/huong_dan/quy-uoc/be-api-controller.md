---
kind: luat
scope: core
verified: 2026-09-06
---

# API Controller & Envelope — src/BE

## Controller — mỏng, chỉ gửi tới MediatR

```csharp
[ApiController]
[Route("api/criteria")]   // route tuong minh, khong dung token [controller]
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

> **Đoạn mẫu trên là khuôn, không phải code có thật.** `CriteriaController` đã xoá cùng
> module DtiWeekly 2026-08-29. Bốn controller đang chạy (đối chiếu 2026-09-06):
> `AuthController` (`api/auth`), `MetaController` (`api/meta`), `PermissionsController`
> (`api/admin/permissions`), `UsersController` (`api/users`) — tất cả nằm ở
> `src/BE/PlatformManager.Api/Controllers/`. Không cái nào dùng `POST list`: danh sách
> người dùng hiện đủ đơn giản để đi bằng `GET /api/users` + query string
> (`UsersController.cs:17`). Đếm bằng lệnh, đừng chép danh sách:
> `ls src/BE/PlatformManager.Api/Controllers/`

## Envelope response — nhất quán cho MỌI endpoint

**Đã CHỐT (2026-08-15):** theo
`doc/tham-khao-ngoai/vnr-successor/03-p2-platform-application.md §4`,
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
    MethodNotAllowed    = 405,   // route khớp, sai verb — CHỈ hạ tầng định tuyến sinh (thêm 2026-09-04)
    Conflict            = 409,   // unique / concurrency
    BusinessRuleError   = 422,   // vi phạm quy tắc nghiệp vụ
    TooManyRequests     = 429,   // vượt rate limit — xem §Rate limiting
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
    string? BusinessCode { get; }             // "{ENTITY}.{ERROR}" — nguồn ở ErrorDescriptor (be-cqrs-handler.md)
    string? TraceId { get; set; }
    bool? Retryable { get; }
    Dictionary<string, string[]>? Fields { get; }   // lỗi validate theo field — key = PascalCase, xem cảnh báo ngay dưới
    Dictionary<string, ApiFieldError[]>? FieldErrors { get; }  // CÙNG tập lỗi, dạng mã + câu — xem §"Lỗi validation cũng phải mang mã"
    Dictionary<string, string>? MessageParams { get; }  // tham số RỜI của câu mà BusinessCode trỏ tới — xem §messageParams
}

// Phần tử của FieldErrors. `Code` là ValidationFailure.ErrorCode của FluentValidation (TÊN
// VALIDATOR: "NotEmptyValidator", "InclusiveBetweenValidator"…), KHÔNG theo khuôn MIEN.MA_LOI —
// đó là hai hệ mã ở hai field khác nhau, có chủ đích.
public sealed record ApiFieldError(string Code, string Message, Dictionary<string, string>? MessageParams = null);

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
    public Dictionary<string, ApiFieldError[]>? FieldErrors { get; init; }
    public Dictionary<string, string>? MessageParams { get; init; }

    public static ApiResult<T> Success(T? data, string? message = null)
        => new() { Status = ApiResultStatus.SUCCESS, Code = ErrorCode.Success, Data = data, Message = message };

    public static ApiResult<T> BusinessError(
        ErrorDescriptor error, string message,
        Dictionary<string, string>? messageParams = null,
        Dictionary<string, ApiFieldError[]>? fieldErrors = null) => new()
    {
        Status = ApiErrorIds.StatusForCode(error.ErrorCode), Code = error.ErrorCode,
        BusinessCode = error.BusinessCode, Message = message, Retryable = error.Retryable,
        MessageParams = messageParams, FieldErrors = fieldErrors,
    };

    // Còn hai factory nữa, cùng file: ValidationError(error, fields, fieldErrors) — chỗ dựng
    // envelope 400 DUY NHẤT; SystemError(error) — nhánh 500.
}
```

> ### ⚠️ `fields` CỐ Ý khác casing với phần còn lại của payload
>
> Payload serialize **camelCase** (`PropertyNamingPolicy = JsonNamingPolicy.CamelCase`, đặt
> cho **cả** MVC lẫn `Http.Json` — hai chỗ trong `src/BE/PlatformManager.Api/Program.cs`,
> tìm bằng `grep -n PropertyNamingPolicy src/BE/PlatformManager.Api/Program.cs`).
> Nhưng **key** của `fields` giữ **PascalCase**, khớp **tên property C# gốc** (`MaxScore`,
> `UserName`) — **không** khớp casing của payload.
>
> Cơ chế: `DictionaryKeyPolicy` mặc định là `null` và **cố ý không set**. Comment giải
> thích nằm ngay trên `NormalizeField` trong
> `src/BE/PlatformManager.Api/Common/GlobalExceptionHandler.cs` — đối chiếu 2026-09-06 là
> `:115` (hàm `NormalizeField` ở `:119`). **Tìm lại bằng lệnh, đừng tin số dòng:**
>
> ```bash
> grep -n DictionaryKeyPolicy src/BE/PlatformManager.Api/Common/GlobalExceptionHandler.cs
> ```
>
> *(🔄 LẬT 2026-09-06: neo `:100-105` của lần sửa 2026-09-03 đã lệch — lần thứ **hai** ở
> đúng chỗ này, sau `:63-68` của bản trước nữa. Đây là bằng chứng cho chính điều đoạn này
> nói: neo bằng **tên hàm** thì `grep` tìm lại được, còn số dòng chỉ đúng cho tới lần sửa
> file kế tiếp. Cổng `check-docs.sh` mục 6 không bắt được vì cả `:63-68` lẫn `:100-105`
> đều nằm trong file.)*
>
> **Đừng "sửa cho nhất quán".** Set `DictionaryKeyPolicy = CamelCase` sẽ làm gãy toàn bộ
> việc bind lỗi vào field trên form phía FE — và gãy **im lặng**, không test nào bắt.
> FE đọc `fields['MaxScore']`, xem `doc/huong_dan/quy-uoc/fe-api-client.md`.
>
> *(Sửa 2026-08-23: bản trước ghi "key = PascalCase khớp JSON đã serialize" — sai, vì JSON
> đã serialize là camelCase. Chính lời giải thích sai đó mới là cái bẫy.)*

**[ĐƠN GIẢN HOÁ] áp dụng cho PlatformManager** (theo
`doc/tham-khao-ngoai/vnr-successor/03-p2-platform-application.md §4.7`):
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
handler, không rải string literal) xem [`be-cqrs-handler.md`](be-cqrs-handler.md) §ErrorDescriptor.

### Lỗi validation cũng phải mang mã — ✅ CÓ THẬT (đối chiếu 2026-09-03)

**Luật:** mọi envelope lỗi rời khỏi BE đều mang **mã máy đọc được**, kể cả nhánh
`ValidationError` (400). `message` là **dev-facing + fallback**, không phải hợp
đồng — client không được buộc phải hiển thị thẳng chuỗi BE trả về.

Phép đo trước khi sửa đã lật ngược chỗ cần sửa, nên ghi lại để người sau không
sửa nhầm chỗ:

| Trước 2026-09-03 | ✅ CÓ THẬT hôm nay (đối chiếu 2026-09-03) |
| --- | --- |
| `ApiResult<T>.ValidationError(...)` có **0 nơi gọi** — code chết | Là chỗ dựng envelope 400 **duy nhất**: `ApiResult<T>.ValidationError`, nhận `ErrorDescriptor` thay cho `string message` |
| Đường **thật** dựng envelope validation bằng tay, **0 dòng** nhắc `BusinessCode` | `GlobalExceptionHandler.TryHandleAsync` gọi `BuildValidationResult`, mã lấy từ catalog `ValidationErrors.Failed` (`VALIDATION.FAILED`) |
| Lỗi từng field chỉ có chuỗi | Thêm `FieldErrors` (`src/BE/Core/PlatformManager.Core.Application/Common/Results/IApiResult.cs:53` — 🔄 LẬT 2026-09-06, số cũ `:41` rơi vào giữa docstring); mã từng field = `ValidationFailure.ErrorCode` của FluentValidation, dự phòng ở hằng `ApiFieldError.UnspecifiedCode` khi rule `Custom` không cấp mã |

**Di trú SONG SONG, không đổi shape một nhát:** thêm trường mới **cạnh** `fields`
→ FE chuyển sang đọc trường mới → **rồi mới** gỡ `fields`. Envelope là thứ mọi
màn hình đi qua, nên một nhát đổi shape tạo ra khoảnh khắc FE cũ gặp BE mới. Bước
gỡ **không được bỏ** — `fields` hôm nay **vẫn còn**, FE mới chỉ nhận trường mới ở
mức kiểu.

**Nghiệm thu** — phải in `2`; bản chốt dùng `grep -c 'BusinessCode'` trên cùng
file đó và lệnh ấy nay in `0` dù việc **đã xong**, lý do đầy đủ ở
[`../wiki-core/be/16-i18n-va-ma-loi.md`](../wiki-core/be/16-i18n-va-ma-loi.md) §4(b):

```bash
grep -c 'ValidationErrors.Failed\|FieldErrors' src/BE/PlatformManager.Api/Common/GlobalExceptionHandler.cs
```

Vì sao luật này đổi lúc này: [`../wiki-core/be/16-i18n-va-ma-loi.md`](../wiki-core/be/16-i18n-va-ma-loi.md) §4.

### `messageParams` — tham số của câu thông điệp — ✅ CÓ THẬT (chốt 2026-09-04, thi công 2026-09-05)

> **Trạng thái: ✅ CÓ THẬT (đối chiếu 2026-09-05).** Trường đã có trong code ở cả hai chỗ
> (gốc envelope và từng phần tử `fieldErrors`). Bảng ở cuối mục đã đổ về cột phải.

**Vì sao envelope cần thêm trường:** dưới hướng i18n đã chốt, `message` tụt xuống vai trò
*dev-facing + fallback* và client dựng câu từ `businessCode`. Câu nào có tham số
(*"Tên đăng nhập 'abc' đã tồn tại"*, *"ít nhất 12 ký tự"*) thì client **không tách lại
được** tham số ra khỏi chuỗi BE đã ráp — nên nó buộc phải hiển thị nguyên câu tiếng Việt,
tức mọi thứ đã làm ở §"Lỗi validation cũng phải mang mã" dừng lại đúng trước cửa. Lý do
đầy đủ + cơ chế lấy giá trị:
[`../wiki-core/be/16-i18n-va-ma-loi.md`](../wiki-core/be/16-i18n-va-ma-loi.md) §10.

#### Hình dạng

```csharp
public interface IApiResult<T> : IHasApiResultStatus
{
    // … các trường hiện có

    // Tham số của câu mà BusinessCode trỏ tới. NULL (⇒ vắng mặt trên dây) khi mã không có
    // tham số — envelope KHÔNG phình thêm một khoá cho mọi lỗi, cùng cách Retryable/Fields
    // đang làm. Khoá là TÊN tham số, không phải số thứ tự.
    Dictionary<string, string>? MessageParams { get; }
}

// Cùng trường, cùng kiểu, cùng ý nghĩa — đặt cạnh mã của TỪNG field
public sealed record ApiFieldError(string Code, string Message, Dictionary<string, string>? MessageParams = null);
```

Trên dây (camelCase như mọi property của envelope; khoá của `fields`/`fieldErrors` vẫn
PascalCase theo cảnh báo casing ở trên):

```json
{
  "message": "Tên đăng nhập 'abc' đã tồn tại.",
  "status": "BUSINESS_ERROR", "code": "Conflict",
  "businessCode": "USER.DUPLICATE_USERNAME",
  "messageParams": { "UserName": "abc" },
  "traceId": "0HNO9S8JAP586:00000001"
}
```

**Luật đọc, một dòng:** `messageParams` luôn nằm **cạnh mã mà nó tham số hoá** — cạnh
`businessCode` ở gốc envelope, và cạnh `code` trong từng phần tử của `fieldErrors`. Đó là
**một** cơ chế đặt ở hai chỗ, không phải hai cơ chế: client viết đúng một hàm ráp câu.

Vì sao không gom hết về gốc envelope: một lần submit hỏng nhiều field thì mỗi field có bộ
tham số riêng, và hai field cùng fail một validator sẽ **ghi đè khoá của nhau** — câu của
field này hiện con số của field kia, hỏng im lặng. Bằng chứng và giải trình đầy đủ ở
[`../wiki-core/be/16-i18n-va-ma-loi.md`](../wiki-core/be/16-i18n-va-ma-loi.md) §10.1.

> ### ⚠️ KHÔNG chuyển tiếp cả từ điển của FluentValidation — allowlist khoá
>
> `ValidationFailure.FormattedMessagePlaceholderValues` **luôn** chứa `PropertyValue` =
> **giá trị người dùng vừa gõ**. Đổ thẳng nó vào `messageParams` nghĩa là mật khẩu mới
> vừa nhập đi ra HTTP response ở đúng lần nhập hỏng. Chỉ khoá trong allowlist mới ra
> ngoài, và `PropertyValue` không bao giờ nằm trong đó — chi tiết + phép nghiệm thu:
> [`../wiki-core/be/16-i18n-va-ma-loi.md`](../wiki-core/be/16-i18n-va-ma-loi.md) §10.4.

#### Trước 2026-09-05 → ✅ có thật hôm nay

| | Trước (đo 2026-09-04) | ✅ CÓ THẬT hôm nay (đối chiếu 2026-09-05) |
| --- | --- | --- |
| Trường trong envelope | **Không có.** `IApiResult<T>` dừng ở `FieldErrors` | `IApiResult<T>.MessageParams` + `ApiResult<T>.MessageParams`, nullable ⇒ vắng mặt trên dây khi null |
| Tham số của lỗi nghiệp vụ | truyền vào `Fail<T>(…, params object[] args)` rồi bị `string.Format` nuốt | `BaseResponse.Fail` nhận cặp **(tên, giá trị)**, dựng câu fallback và `messageParams` từ CÙNG một bộ giá trị |
| Tham số của lỗi validate | FluentValidation có sẵn, không ai đọc | `GlobalExceptionHandler.ToFieldError` chuyển tiếp phần đã qua allowlist `MessageParamPolicy` |
| `ApiFieldError` | 2 thành phần `Code` + `Message` | thêm `MessageParams` tuỳ chọn (tham số thứ 3, mặc định null) |

> **Neo bằng TÊN, không bằng số dòng** (bài học đã trả giá, xem `16-i18n-va-ma-loi.md` §4(b)):
> bản trước của bảng này neo `IApiResult.cs:41`, `BaseResponse.cs:12`, `ApiFieldError.cs:25`,
> `GlobalExceptionHandler.cs:95` — cả bốn đều **trôi** trong chính lượt thi công 2026-09-05, vì
> bản vá thêm XML doc giải thích lý do ngay phía trên chúng.

**Nghiệm thu** — trước in `0`, ✅ nay in `2`:

```bash
grep -c 'MessageParams' src/BE/Core/PlatformManager.Core.Application/Common/Results/IApiResult.cs
```

Bộ nghiệm thu đầy đủ (5 lệnh + 2 ca đối chứng):
[`../wiki-core/be/16-i18n-va-ma-loi.md`](../wiki-core/be/16-i18n-va-ma-loi.md) §10.8.

### File chủ của "hình dạng envelope" — giải trình `.claude/CLAUDE.md` §5 (2026-09-04)

Hai file cùng nói về envelope, và câu hỏi *"gộp hay không"* phải trả lời trước khi thêm
bất kỳ trường nào:

| File | Nó là chủ của cái gì |
| --- | --- |
| **File này**, §Envelope response | **Hình dạng**: trường nào tồn tại, kiểu gì, khi nào có mặt, ai gán |
| [`../wiki-core/fe/02-http-envelope.md`](../wiki-core/fe/02-http-envelope.md) | **Cách tiêu thụ**: interceptor, bind lỗi vào form, retry, huỷ request |

**Chốt: KHÔNG gộp — hai file đang chia đúng ranh giới**, một bên dựng, một bên tiêu thụ.
Gộp lại sẽ nhét cơ chế `HttpInterceptorFn`, `takeUntilDestroyed` và double-submit của
Angular vào một file quy ước BE, và người sửa envelope phải đọc qua chúng để tới chỗ mình
cần. Ranh giới hiện tại có ích thật, không phải trùng lặp.

**Nhưng một thứ thì phải chỉ có một chỗ: ĐỊNH NGHĨA trường.** Bản trước của
`fe/02-http-envelope.md` chép nguyên danh sách trường vào một `interface` TypeScript, và
bản chép đó **đã lệch** — đo được ngày 2026-09-04:

```bash
grep -cE 'fieldErrors|MethodNotAllowed' doc/huong_dan/wiki-core/fe/02-http-envelope.md src/FE/src/app/core/http/api-result.model.ts
```

**Hai chỗ lệch, đo bằng cùng một lệnh, cả hai đều nằm ở bản chép trong doc FE:**

| Thứ đã thêm | Vào BE + code FE | Vào bản chép ở `fe/02` |
| --- | --- | --- |
| trường `fieldErrors` | 2026-09-03 (`src/FE/src/app/core/http/api-result.model.ts:82`) | không |
| mã `MethodNotAllowed` | 2026-09-04 (`src/FE/src/app/core/http/api-result.model.ts:30`) | không |

Lệch sau **một ngày**, rồi lệch thêm lần thứ hai — đúng cơ chế hỏng mà
`.claude/CLAUDE.md` §3 lý do 2 ghi lại, và không gate nào bắt được vì cả hai bản đọc
riêng đều hợp lệ. Đây cũng là câu trả lời cho *"giữ cả hai cho chắc"*: bản chép không bao
giờ được sửa cùng lúc, nên "cho chắc" chính là thứ sinh ra bản sai.

Lượt sửa 2026-09-04 đã gỡ danh sách chép đó khỏi `fe/02-http-envelope.md`, nên lệnh trên
**không còn** đo được cái lệch cũ — nó ở đây làm bằng chứng cho luật, không phải phép
nghiệm thu đang chạy. Phép nghiệm thu cho luật là chính con mắt người review: một trường
mới xuất hiện trong file FE kèm mô tả kiểu/khi-nào-có-mặt là dấu hiệu bản sao đang mọc lại.

Vì vậy, luật áp cho mọi lần thêm trường về sau:

> Trường mới **định nghĩa ở đây**. `fe/02-http-envelope.md` chỉ trỏ đường, và chỉ mô tả
> **cách FE dùng** trường đó. Bản `interface` TypeScript sống thật là code
> (`src/FE/src/app/core/http/api-result.model.ts`), không phải một bản chép trong doc.

## Error → HTTP status mapping

`ErrorCode` **là** mã HTTP (xem enum ở trên) — không có bảng tra thứ 2. Chọn
`ErrorCode` nào cho 1 lỗi cụ thể, theo đúng phân biệt của
`doc/tham-khao-ngoai/vnr-successor/06-p5-module-dau-tien.md §4`:

| `ErrorCode` | HTTP | Dùng khi |
| --- | ---: | --- |
| `ValidationError` | 400 | Tính được trọn vẹn từ payload, không cần DB — luôn kèm `Fields` |
| `NotFound` | 404 | Không tìm thấy **resource chính của route** (`{id}`) — kể cả đã soft-delete (không tách riêng 2 mã, tránh lộ thông tin tồn tại của dữ liệu đã xoá) |
| `BusinessRuleError` | 422 | FK/tham chiếu nằm **trong payload** (không phải resource của route), cần đọc DB để biết hợp lệ |
| `Conflict` | 409 | Xung đột **trạng thái của chính resource** — trùng giá trị unique, hoặc đang bị ràng buộc không cho thao tác |
| `AuthorizationError` | 403 | Đã đăng nhập nhưng thiếu quyền — **không** map về `BusinessRuleError`, FE cần phân biệt được để điều hướng sang màn xin quyền |
| `TooManyRequests` | 429 | Vượt rate limit. FE phải phân biệt với lỗi nghiệp vụ để hiện "thử lại sau" thay vì "thao tác sai" — xem §Rate limiting |
| `MethodNotAllowed` | 405 | **Handler không bao giờ chọn mã này.** Hạ tầng định tuyến sinh, `ApiStatusCodeEnvelopeMiddleware` bọc envelope — xem §"404/405 do định tuyến" |
| (exception không mong đợi, qua middleware) | 500 | Bug/hạ tầng — không lộ chi tiết ra response |

## Dispatcher — `HandleResult`, mỏng, 1 chỗ map HTTP

```csharp
[Authorize]        // ← fail-CLOSED: mặc định chặn, endpoint công khai phải [AllowAnonymous] tường minh
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

Lớp cơ sở chỉ mang **`[Authorize]`** (đối chiếu source 2026-09-09,
[`ApiControllerBase.cs:31`](../../../src/BE/Core/PlatformManager.Core.Api/ApiControllerBase.cs)).
`[ApiController]` và `[Route(...)]` khai ở **từng controller cụ thể** — route
viết tường minh (`[Route("api/admin/permissions")]`), không dùng token
`[controller]`, để đổi tên class không làm đổi URL công khai.

Mọi controller kế thừa `ApiControllerBase` thay vì `ControllerBase` trực
tiếp — **không** controller nào tự `try-catch`/tự hardcode status code
(`return StatusCode(500, ...)` rải rác tạo nguồn sự thật thứ 2 cho mapping
`ErrorCode → HTTP`, cấm).

## Exception-handling middleware toàn cục

Bắt **3** loại lỗi không đi qua `HandleResult` và dịch cả ba thành đúng `IApiResult`
envelope ở trên, không lộ stack trace. Hiện thực là một lớp `IExceptionHandler` —
`src/BE/PlatformManager.Api/Common/GlobalExceptionHandler.cs`, đăng ký ở
`Program.cs:153` (`AddExceptionHandler<GlobalExceptionHandler>()`):

```csharp
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception exception, CancellationToken ct)
    {
        var traceId = ctx.TraceIdentifier;

        ApiResult<object> result = exception switch
        {
            // 1. Validator fail trước khi vào handler (xem be-cqrs-handler.md §Validator).
            //    Đi qua factory ApiResult<T>.ValidationError — chỗ dựng envelope 400 DUY NHẤT,
            //    mã lấy từ catalog (ValidationErrors.Failed), KÈM cả `fields` lẫn `fieldErrors`.
            ValidationException vex => BuildValidationResult(vex, traceId),

            // 2. Thiếu/sai X-XSRF-TOKEN trên request ghi — 403, KHÔNG phải 500: request bị TỪ
            //    CHỐI có chủ đích, không phải lỗi hệ thống.
            AntiforgeryValidationException => WithTrace(
                ApiResult<object>.BusinessError(
                    InfrastructureErrors.CsrfRejected,
                    InfrastructureErrors.CsrfRejected.MessageTemplate), traceId),

            // 3. Mọi thứ còn lại — bug, lỗi kết nối DB.
            _ => WithTrace(ApiResult<object>.SystemError(InfrastructureErrors.Unexpected), traceId),
        };

        // Log đầy đủ ở server; response chỉ mang mã + traceId.
        if (result.Code == ErrorCode.SystemError) logger.LogError(exception, "…TraceId={TraceId}", traceId);
        else                                      logger.LogWarning(exception, "…TraceId={TraceId}", traceId);

        ctx.Response.ContentType = "application/json";
        ctx.Response.StatusCode = (int)result.Code;
        await ctx.Response.WriteAsJsonAsync(result, ct);
        return true;
    }
}
```

> **🔄 LẬT 2026-09-06 — đoạn mẫu cũ dạy đúng thứ §"Lỗi validation cũng phải mang mã" ở
> trên vừa bỏ đi.** Bản trước là một lambda `app.UseExceptionHandler(a => a.Run(…))` dựng
> `new ApiResult<object> { … }` **bằng tay**: không `BusinessCode`, không `FieldErrors`,
> không nhánh CSRF. Ba sai lệch, mỗi cái một hướng:
> 1. **Sai kiểu hiện thực** — code thật là `IExceptionHandler` (đăng ký qua DI), không phải
>    inline lambda. Ai chép mẫu về sẽ đăng ký **nguồn thứ hai** cho cùng một việc.
> 2. **Sai hình dạng envelope** — dựng tay bỏ qua factory `ApiResult<T>.ValidationError`,
>    tức tái tạo đúng trạng thái *"đường thật dựng envelope validation bằng tay, 0 dòng
>    nhắc `BusinessCode`"* mà bảng ở §"Lỗi validation cũng phải mang mã" ghi là **đã sửa
>    2026-09-03**. Hai mục trong **cùng một file** nói ngược nhau.
> 3. **Thiếu nhánh CSRF** — `AntiforgeryValidationException` sẽ rơi vào `_` và ra **500**
>    thay vì 403.

**Không bao giờ** trả stack trace hay chi tiết exception nội bộ ra response
— chỉ `BusinessCode`/`ErrorCode` chung + `TraceId` để tra log phía server.

## 404/405 do định tuyến — nhánh KHÔNG đi qua handler

✅ CÓ THẬT (2026-09-04) — `src/BE/PlatformManager.Api/Common/ApiStatusCodeEnvelopeMiddleware.cs`,
đăng ký ở `Program.cs` ngay sau `app.UseCors(...)`.

Hai mã dưới đây do **hạ tầng định tuyến** sinh ra ở cuối pipeline với thân response **rỗng**, nên
chúng nằm ngoài tầm của cả ba bộ dựng envelope còn lại (MediatR/`HandleResult`,
`GlobalExceptionHandler`, `ModelBindingProblemFactory`):

| HTTP | Khi nào | `businessCode` |
| ---: | --- | --- |
| 404 | Không endpoint nào khớp đường dẫn | `ROUTE.NOT_FOUND` |
| 405 | Đường dẫn khớp, HTTP verb không được hỗ trợ | `ROUTE.METHOD_NOT_ALLOWED` |

Ba ràng buộc, cả ba đều là thứ hỏng **im lặng** nếu bỏ:

1. **Chỉ bọc tiền tố `/api`** — allowlist, không phải blocklist. `/hangfire`, `/swagger`,
   `/health`, và mọi nhánh phục vụ file tĩnh sau này đều nằm ngoài và **không** được chạm: trả JSON
   cho một tài nguyên tĩnh 404 biến một file thiếu thành một trang hỏng khó đoán.
2. **Không được ghi đè response đã có thân.** Phép thử là `HasStarted || ContentLength.HasValue ||
   ContentType != ""` — đúng phép thử `StatusCodePagesMiddleware` của ASP.NET Core dùng. Đây là thứ
   giữ nguyên 404 mà handler **chủ động** trả (`USER.NOT_FOUND`); ghi đè nó là thay một mã nghiệp vụ
   đúng bằng một mã định tuyến sai, và cả hai đều là HTTP 404 nên không ai thấy.
3. **Không dùng `MapFallback`.** Nó đăng ký route bắt-tất-cả cho mọi verb, nên trở thành ứng viên
   hợp lệ của request sai verb và **nuốt mất chính mã 405** — sai verb biến thành 404.

`ROUTE.NOT_FOUND` cố ý khác miền với `USER.NOT_FOUND`: hai ca cùng HTTP 404 và cùng
`code: "NotFound"`, nên `businessCode` là thứ **duy nhất** FE phân biệt được "gọi sai URL" (bug của
FE) với "bản ghi không tồn tại" (dữ liệu).

Test chốt: `src/BE/Tests/PlatformManager.Core.IntegrationTests/Common/RoutingEnvelopeTests.cs` —
2 ca chiều thuận + 2 ca chiều ngược (ràng buộc 1 và 2).

## CORS

Allowlist origin **đọc từ cấu hình**, bind qua `IOptions<CorsPolicyOptions>` (section
`Cors:AllowedOrigins`) — `src/BE/PlatformManager.Api/Common/CorsPolicyOptions.cs`. Không
hard-code origin trong `Program.cs`, và **không** mở `AllowAnyOrigin()` một khi đã có auth
thật (cookie/token) — CORS mở rộng cùng lúc với auth là lỗ hổng bảo mật phổ biến. Ở dev,
giá trị là nơi `src/FE` chạy (`http://localhost:4200` theo mặc định Angular CLI); ở
Production đặt qua biến môi trường `Cors__AllowedOrigins__0`, không commit domain thật.

> 📖 Luật fail-fast của section này (vì sao `?? []` là bẫy, vì sao `ValidateOnStart()` chỉ
> gắn ở Production): [`be-architecture.md`](be-architecture.md)
> §"Quyết định người dùng 2026-08-31".
>
> *(Sửa 2026-09-06: bản trước chỉ nói *"origin cho phép = nơi FE chạy dev"* và không nhắc
> tới cơ chế nào. Đọc xong không biết đặt giá trị đó ở đâu, nên rất dễ đi thẳng tới
> `WithOrigins("http://localhost:4200")` ghép cứng — đúng thứ `CorsPolicyOptions` sinh ra
> để thay thế.)*

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
- FK nghiệp vụ trỏ tới người dùng tham chiếu `AppUser.Id` — xem
  `doc/cau-truc-database.md` bảng **`AspNetUsers`**.

  > *(Sửa 2026-09-06: bản trước gọi bảng đó là `AppUsers` và lấy `CriteriaAssessment.OwnerId`
  > làm ví dụ. Tên bảng thật là `AspNetUsers` — `AppUser` là tên **class**, không phải tên
  > bảng; `doc/cau-truc-database.md:171` dùng đúng `AspNetUsers`. Entity ví dụ thì đã xoá
  > cùng module DtiWeekly 2026-08-29.)*
- **Cơ chế xác thực cho SPA: cookie session của ASP.NET Core Identity — ĐÃ CHỐT,
  KHÔNG JWT.** FE gửi kèm cookie mỗi request (`withCredentials`), **không** tự lưu
  token vào `localStorage`. Hợp đồng: `doc/contracts/auth.md`. Lý do + cơ chế thu
  hồi phiên: `doc/huong_dan/wiki-core/be/02-identity-auth.md`.

  > Tóm tắt lý do: hệ chạy **1 process** nên JWT không giải bài toán nào; và JWT
  > trần **không thu hồi được** — nút "khoá tài khoản" sẽ vô tác dụng cho tới khi
  > token hết hạn, trong khi cookie + `SecurityStampValidator` huỷ được phiên.
  > Khi nào nên xét lại (tách ≥2 process, có app bên thứ ba): xem `02-identity-auth.md`.
  >
  > ⚠️ Kèm ràng buộc bắt buộc khi thi công: **khoá tài khoản phải ĐỔI luôn
  > `SecurityStamp`**, nếu không người đang online sẽ không bị đá ra.
  >
  > *(Sửa 2026-08-23: mục này từng ghi "chưa chốt kiểu cụ thể" trong khi
  > `doc/contracts/auth.md`, `quy-uoc/README.md` và `fe-api-client.md` đều ghi "đã
  > CHỐT cookie" — và `README.md` còn trỏ thẳng vào đây làm nguồn.)*
- Vai trò/permission chi tiết theo từng tính năng (vd ai được sửa `Status`
  của `CriteriaAssessment`) — xem
  `spec/danh-muc-dti/business-rules.md` §6.5 "Quyền ghi"; nếu chưa đủ
  thông tin nghiệp vụ để chốt role, giữ nguyên placeholder trong spec đó,
  không tự bịa role ở tầng code.

  > *(Sửa 2026-09-06: dòng này từng trỏ `spec/dashboard-dti/business-rules.md`
  > **mục Permission** — sai hai lần. Thư mục `spec/dashboard-dti-weekly/` cũ đã
  > đổi tên, và mục "Permission" **chưa từng tồn tại** ở file dashboard: quyền là
  > chuyện của màn GHI, nên nó nằm ở file danh mục. Cổng không bắt được vì
  > `check-docs.sh` §4 chỉ kiểm đường dẫn mang tiền tố `src/` và `doc/`, không
  > kiểm `spec/`.)*

### Phân quyền theo hành động — bản rút gọn (khác role, khác menu)

**Vấn đề thật đã tìm thấy (2026-08-17), VẪN CÒN MỞ (đối chiếu 2026-09-06):** màn "Phân
quyền" điều khiển `SysMenuRole` — "role nào thấy **menu** nào" — chứ không chặn được lời
gọi API. Cơ chế chặn theo hành động thì **đã có** (xem dưới), nhưng **không endpoint sản
phẩm nào đang khai `[RequirePermission]`**, nên hôm nay `[Authorize]` (đăng nhập là đủ) là
lớp chặn thật sự duy nhất trên `AuthController`/`MetaController`/`PermissionsController`/
`UsersController`.

> *(🔄 LẬT 2026-09-06: ví dụ cũ là `POST /api/criteria` — endpoint đó đã xoá cùng module
> DtiWeekly 2026-08-29, nên câu văn mô tả một lỗ hổng trên một URL không tồn tại. Bản chất
> gap thì **không** đổi; xem [`../wiki-core/be/01-core-components.md`](../wiki-core/be/01-core-components.md)
> §Áp dụng #7 để biết vì sao "cơ chế đã có" và "cơ chế đang được dùng" là hai khẳng định
> khác nhau.)*

**Bản rút gọn áp dụng ngay** — KHÔNG phải hệ thống đầy đủ ở
[05-p4-hosting-api.md §7](../wiki-core/../../tham-khao-ngoai/vnr-successor/05-p4-hosting-api.md)
(cái đó có `CrudActionResolver` suy luận action tự động + 2 cơ chế enforcement
song song — quá nặng cho 1 module hiện tại):

> Đã hiện thực hoá thật (đối chiếu lại 2026-09-06 — đã vào repo; nhãn *"chưa commit"* của
> bản 2026-08-24 gỡ ngày này) — code chủ ở
> `Core.Application/Permissions/{RequirePermissionAttribute,IPermissionChecker}.cs`
> và `Core.Infrastructure/Permissions/RequirePermissionFilter.cs`; mẫu dưới
> đây chỉ minh hoạ ý tưởng, đọc code thật thay vì chép mẫu này 1:1 — code thật
> có thêm nhánh break-glass cho `SuperAdmin` (đã duyệt 2026-08-19, xem comment
> trong `RequirePermissionFilter.cs`) mà mẫu dưới lược bỏ cho gọn.

```csharp
// Core.Application — pure metadata, không logic runtime
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequirePermissionAttribute(string key) : Attribute
{
    public string Key { get; } = key;   // vd "import.manage", khớp hằng số trong ResourceKeys
}

// Core.Application — seam tra quyền, tách khỏi filter để test không cần Postgres
public interface IPermissionChecker
{
    Task<bool> HasPermissionAsync(IReadOnlyCollection<string> roleNames, string resourceKey, CancellationToken ct);
}

// Core.Infrastructure — 1 filter toàn cục, đọc role từ claim rồi hỏi IPermissionChecker
public sealed class RequirePermissionFilter(IPermissionChecker permissionChecker) : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext ctx)
    {
        var attr = ctx.ActionDescriptor.EndpointMetadata
            .OfType<RequirePermissionAttribute>().FirstOrDefault();
        if (attr is null) return;   // không khai attribute = không chặn thêm (giữ nguyên [Authorize])

        var roleNames = ctx.HttpContext.User.FindAll(ClaimTypes.Role).Select(c => c.Value).Distinct().ToList();
        if (roleNames.Count == 0) { ctx.Result = new ForbidResult(); return; }

        // Break-glass: SuperAdmin qua mọi [RequirePermission] mà không cần RolePermission —
        // chi tiết + lý do xem RequirePermissionFilter.cs thật.

        var ct = ctx.HttpContext.RequestAborted;
        if (!await permissionChecker.HasPermissionAsync(roleNames, attr.Key, ct))
            ctx.Result = new ForbidResult();
    }
}
```

- Không có `CrudActionResolver` — action luôn khai tường minh trong `key`
  (vd `"import.manage"` dùng chung cho mọi thao tác ghi của Import: tải file
  lên, huỷ job, xoá job), không suy từ tên method. Chấp nhận thô hơn (không
  phân biệt View/Create/Delete) để đổi lấy đơn giản — nâng cấp lên action rời
  (`PermissionAction`) khi thật sự cần phân biệt "xem được nhưng không sửa
  được".

  > Hai ví dụ trên dùng `import.manage` vì đó là **key duy nhất còn tồn tại**
  > (đối chiếu source 2026-09-03:
  > `src/BE/PlatformManager.Api/Permissions/AppResourceKeySource.cs`).
  > Bản trước lấy `criteria.manage` làm ví dụ — key đó đã bị gỡ 2026-08-29 cùng
  > module DtiWeekly, nên người đọc chép mẫu về sẽ khai một key không có thật.
  > Đừng chép danh sách key vào đây, đọc thẳng bằng lệnh:
  > `grep -n "public const string" src/BE/PlatformManager.Api/Permissions/AppResourceKeySource.cs`
  >
  > **Danh mục key nay do HOST khai, không phải Core** (tách 2026-09-03). Core chỉ giữ seam
  > `ICoreResourceKeySource`; thêm key mới thì sửa ở dự án, **không** sửa vào trong Core —
  > xem `doc/kien-truc-core-module.md`. Bản trước trỏ vào `Core.Application/.../ResourceKeys.cs`,
  > file đó không còn tồn tại.
- Chỉ 1 cơ chế enforcement (filter), không thêm `[Authorize(Policy=...)]`
  song song.
- Áp dụng cho endpoint **ghi** dữ liệu nghiệp vụ trước; endpoint chỉ đọc có thể
  giữ `[Authorize]` trần thêm một thời gian nếu chưa có yêu cầu nghiệp vụ phân
  biệt ai xem được gì.

  > 🚧 **Hiện chưa có endpoint nghiệp vụ nào (đối chiếu lại 2026-09-06).** Bản trước
  > của dòng này nêu đích danh Criteria/CriteriaGroups/Import và Dashboard —
  > toàn bộ đã bị gỡ cùng module DtiWeekly. Đếm lại thay vì tin danh sách chép:
  >
  > ```bash
  > grep -rn "\[RequirePermission" src/BE --include=*.cs | grep -v Tests | grep -v "///" | grep -v "//"
  > ```
  >
  > PASS hôm nay: **rỗng**. Module nghiệp vụ đầu tiên dựng lại là lúc áp quy tắc này.
  >
  > *(🔄 LẬT 2026-09-06: lệnh cũ là `grep -rn "RequirePermission" … | grep -v Tests` kèm
  > tiêu chí "hiện ra 0 kết quả ngoài project test" — **sai**, lệnh đó hôm nay in ra 8
  > dòng (docstring của `ICoreResourceKeySource`/`AppResourceKeySource`/`CoreSeeder`, chính
  > `RequirePermissionFilter.cs`, và 2 comment trong `Program.cs`). Một tiêu chí PASS sai
  > tệ hơn không có tiêu chí: người chạy thấy output, kết luận "có endpoint rồi", và bỏ
  > qua đúng lỗ hổng mục này tồn tại để canh. Lệnh mới lọc `[` mở đầu attribute và loại
  > comment.)*

## Rate limiting

**Bổ sung khi chuyển sang giai đoạn product (2026-08-17)** — đối chiếu 12-Factor/
chuẩn ngành, không nằm trong lần rà soát VNR gốc. Nỗi đau gốc: chưa có gì chặn
brute-force `POST /api/auth/login`, hay một user spam một endpoint nặng (import/export —
xem [`be-cqrs-handler.md`](be-cqrs-handler.md) §"Command chạy lâu") làm nghẽn worker cho
user khác.

> *(Sửa 2026-09-06: bản trước nêu đích danh `POST /api/import` — endpoint đó đã xoá cùng
> module DtiWeekly 2026-08-29. Nhu cầu thì không đổi, chỉ ví dụ là hết thật.)*

.NET có middleware dựng sẵn từ .NET 7, không cần thư viện ngoài:

```csharp
// Program.cs
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Policy riêng cho login — PHÂN VÙNG THEO IP (xem cảnh báo bên dưới)
    options.AddPolicy("login", ctx => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window      = TimeSpan.FromMinutes(1),
        }));

    // Giới hạn nền cho mọi request còn lại — cũng theo IP, nhưng CỬA SỔ TRƯỢT
    // (lý do ở §"Hiệu chỉnh hạn mức" bên dưới — khác thuật toán với policy "login")
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit       = 200,
                Window            = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 6,   // mỗi đoạn 10 giây
            }));
});

app.UseRateLimiter();
```

> ### ⚠️ KHÔNG dùng `AddFixedWindowLimiter(policyName, …)` cho rate limit theo IP
>
> Overload đó tạo **MỘT** limiter dùng chung cho cả policy — **không phân vùng gì
> cả**. Với `PermitLimit = 5`, đó là **5 lượt đăng nhập mỗi phút cho TOÀN HỆ
> THỐNG**, không phải mỗi IP.
>
> Hệ quả **ngược hẳn mục tiêu**: một kẻ tấn công đốt hết quota là **khoá đăng nhập
> của mọi người** — biến biện pháp chống brute-force thành lỗ hổng DoS. Còn
> brute-force phân tán qua nhiều IP thì **không** bị chặn riêng.
>
> Muốn phân vùng theo IP phải dùng `AddPolicy` + `RateLimitPartition.GetFixedWindowLimiter`
> như mẫu trên.
>
> *(Sửa 2026-08-23. Bản trước dùng sai overload **kèm câu giải thích sai** — "mặc
> định của `FixedWindowLimiter` — partition key là remote IP". Đây chính là ca mà
> `.claude/CLAUDE.md` §3 ghi lại: rule sai không nằm yên, `Program.cs` chép y theo
> nên mang nguyên lỗi.)*

```csharp
[HttpPost("login")]
[EnableRateLimiting("login")]   // gắn riêng action nhạy cảm — không dùng chung giới hạn nền
public async Task<IActionResult> Login([FromBody] LoginCommand cmd, CancellationToken ct) => ...
```

- Phân vùng **theo IP**, không theo user — chưa đăng nhập thì chưa có user.
- Endpoint import (khi dựng lại — hôm nay **chưa có**) dùng giới hạn nền (`GlobalLimiter`)
  là đủ ở quy mô hiện tại: nó tự giới hạn dung lượng/file + chạy nền qua Hangfire (không
  block worker), nên rate limit ở đây chỉ chặn spam request **tạo job**, không phải chặn
  xử lý file lớn.
- **Không** rate-limit health check (`/health`) — orchestrator/monitoring
  cần gọi endpoint này thường xuyên, rate limit vào đây gây báo động giả.

### Hiệu chỉnh hạn mức — ✅ ĐÃ THI CÔNG (quyết định người dùng 2026-08-30)

> *(🔄 LẬT 2026-09-06: tiêu đề này mang nhãn `🚧` cho tới hôm nay, trong khi tiểu mục
> "✅ CÓ THẬT (đối chiếu 2026-09-05) — ba hàng rào" ở cuối chính mục này đã xác nhận việc
> đổi `100 cố định → 200 trượt` **đã vào code**. Nhãn ở tiêu đề là thứ người đọc thấy
> trước, nên nó quyết định họ tin phần nào.)*

Hai con số ban đầu (`login = 5`, `global = 100`, cả hai cửa sổ cố định) là mặc
định hợp lý cho máy dev, chưa bao giờ đối chiếu với người dùng thật. Đối chiếu
lần đầu ngày 2026-08-30, với bối cảnh triển khai đã chốt ở
[`../wiki-core/fe/17-phuc-vu-va-trien-khai.md`](../wiki-core/fe/17-phuc-vu-va-trien-khai.md) §2:
**10–50 người dùng đồng thời, mỗi người một IP riêng** (làm việc phân tán, không
chung một đường truyền văn phòng).

#### Đặt đúng vai trò trước khi chọn số

Rate limit theo IP **không phải** phòng thủ DDoS — tấn công thật đến từ hàng nghìn
IP, mỗi IP gửi dưới ngưỡng là đi qua sạch. Việc đó thuộc về tầng mạng và web server.
Cơ chế này bảo vệ đúng hai thứ:

| Nguy cơ | Ai chặn |
|---|---|
| Vòng lặp retry hỏng ở FE, tab treo gọi API liên tục | `GlobalLimiter` |
| Brute-force đăng nhập từ một nguồn | policy `login`, cộng thêm Identity Lockout |
| Brute-force phân tán nhiều IP | **Không phải** hai cơ chế trên — Identity Lockout theo username (xem [`../wiki-core/be/09-security-beyond-auth.md`](../wiki-core/be/09-security-beyond-auth.md)) |
| Lưu lượng tấn công thể tích | Tầng mạng / nginx — **không** đặt kỳ vọng vào app |

Đặt đúng vai trò rồi thì hai con số đi hai hướng khác nhau, không cùng một công thức.

#### `login` — giữ nguyên 5/phút, cửa sổ cố định

Khớp với ngưỡng Identity Lockout (5 lần sai / 15 phút) nên hai lớp bổ trợ nhau
đúng thiết kế. Và với brute-force, **khoá cứng tới hết cửa sổ là điều ta muốn** —
đây chính là chỗ cửa sổ trượt sẽ làm yếu đi mục tiêu, nên đừng đổi cho "nhất quán".

#### `global` — 200/phút, **cửa sổ trượt** 6 đoạn

Đo thực tế trước khi chọn — **đếm bằng lệnh, đừng tin số viết ở đây**:

```bash
grep -rhoE "\.(get|post|put|delete)<" src/FE/src/app --include=*.ts | wc -l
```

App còn nhỏ. Một người dùng bận rộn đạt đỉnh khoảng 30–60 request/phút (mở app ~3; mỗi màn hình 1–4; mỗi lần lọc 1).

> *(Sửa 2026-08-31: bản trước ghi "26 lời gọi HTTP trong 4 service" — đếm lại ra
> **15 lời gọi, 6 file**. Con số đó là căn cứ chọn hạn mức nên nó không phải chi
> tiết trang trí; nay thay bằng lệnh, đúng §6 của `.claude/CLAUDE.md`. Kết luận
> không đổi: đỉnh thật vẫn thấp hơn 200/phút nhiều lần.)*

Đổi **thuật toán** quan trọng ngang đổi con số, và đây là lý do:

| | Cửa sổ cố định | Cửa sổ trượt (6 đoạn) |
|---|---|---|
| Trần khi burst vắt qua ranh giới phút | tới **2×** hạn mức | đúng **1×** |
| Chạm hạn mức ở giây thứ 5 thì tắc bao lâu | tới **55 giây** | bắt đầu hồi sau **10 giây** |

Cửa sổ cố định vừa nới lỏng trần một cách ngẫu nhiên, vừa phạt nặng người chạm
phải. Cửa sổ trượt sửa cả hai chiều.

Hệ quả cụ thể của việc đổi từ `100 cố định` sang `200 trượt`: trần đỉnh **không
đổi** (100 cố định đã cho phép dồn tới 200 qua ranh giới), sức chứa liên tục **gấp
đôi**, thời gian phục hồi **nhanh gấp 6 lần**.

#### ✅ CÓ THẬT (đối chiếu 2026-09-05) — ba hàng rào, không phải hai

Việc đổi `100 cố định → 200 trượt` **đã thi công**. Hôm nay có **ba** cơ chế chồng lên
nhau, cấu hình khai bằng hằng số ở đầu `src/BE/PlatformManager.Api/Program.cs`:

| # | Hàng rào | Phân vùng theo | Hạn mức | Kiểu cửa sổ | Hằng số |
|---|---|---|---|---|---|
| 1 | Policy `login` | IP (`Connection.RemoteIpAddress`) | 5/phút | `FixedWindow` | `Program.cs:244` |
| 2 | `GlobalLimiter` (mọi request) | IP | 200/phút | `SlidingWindow`, 6 đoạn × 10s | `Program.cs:251`, `:252` |
| 3 | `GlobalLimiter` nhánh login | **tên đăng nhập** | 10 / 5 phút | `SlidingWindow`, 5 đoạn × 1 phút | `Program.cs:263`–`:265` |

Hàng rào 2 và 3 nối bằng `PartitionedRateLimiter.CreateChained` (`Program.cs:312`), nên
chúng **cộng dồn** chứ không thay thế nhau.

**Hàng rào 3 chặn đúng kịch bản hai hàng rào kia bỏ lọt:** brute-force **phân tán** từ
hàng nghìn IP cùng nhắm một tài khoản. Phân vùng theo IP không thấy gì bất thường ở kịch
bản đó — mỗi IP chỉ thử vài lần. Nó dùng cửa sổ **trượt** có chủ đích, khác policy `login`:
`SuperAdmin` được miễn khoá tài khoản nên đây là thứ **duy nhất** chặn dò mật khẩu vào nó,
mà cũng vì thế nó phải trả lại lượt liên tục (~2 lượt/phút) để quản trị viên thật vẫn vào
được — chậm hơn, chứ không bị từ chối sạch cả cửa sổ.

Phân vùng **không đọc header nào do client gửi** (cho client tự chọn phân vùng là tự vô
hiệu hoá rate limit) — chỉ đọc kết nối TCP thật. `UseForwardedHeaders` ghi đè thẳng vào
`Connection.RemoteIpAddress` với `KnownProxies` khai tường minh, nên phân vùng theo IP tự
đúng khi chạy sau proxy.

> **🔄 LẬT 2026-09-05.** Bản trước là bảng *"có thật hôm nay → sẽ thành"* với cột trái ghi
> `global` = **100/phút `FixedWindow`** (đối chiếu 2026-08-30). Cột đó nay mô tả quá khứ:
> code đã là 200/`SlidingWindow`/6 đoạn. Nguy hiểm hơn cột sai là thứ bảng đó **không hề
> có** — hàng rào thứ ba theo tên đăng nhập, tức lá chắn brute-force phân tán, không xuất
> hiện ở bất kỳ dòng nào của tài liệu này. Người đọc doc để biết "hệ thống chặn đăng nhập
> sai thế nào" sẽ bỏ sót đúng cơ chế quan trọng nhất.

#### Đo lại sau khi chạy thật

Con số 200 là ước lượng có căn cứ, không phải số đo. Sau một tuần chạy thật, đếm
số lần 429 trong log: nếu có 429 nào **không** đi kèm sự cố FE thì hạn mức đang
chặt hơn hành vi thật, nới tiếp. Nếu không có 429 nào trong nhiều tuần thì hạn mức
đang không làm gì cả — vẫn giữ, vì nó là lưới chặn tai nạn, không phải công cụ đo tải.

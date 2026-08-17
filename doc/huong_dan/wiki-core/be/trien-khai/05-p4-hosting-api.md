# P4 — `Hosting.Api` + `Hosting.CompositionRoot`

> **Định nghĩa hoàn thành:** `GET /api/v1/health` trả 200. Một controller thật
> kế thừa `BaseCrudApiController<TResult,TRequest,TKey>` — **0 dòng code** —
> đã có đủ `list/export-excel/{id}/lookup/POST/PUT/DELETE/batch-delete`. Gọi
> endpoint đó khi chưa đăng nhập → 401. Gọi khi đã đăng nhập nhưng thiếu quyền
> → 403 (không phải exception 500). Gọi `Create` với payload thiếu field bắt
> buộc → 400 kèm `Fields` đúng field. `SaveChanges` ở P3 ném `ConflictException`
> → endpoint trả **409**, không phải 500.

P4 là nơi 3 phase trước **gặp nhau lần đầu qua HTTP**: `IApiResult<T>` (P2) đi
hết một vòng đời request thật, `ErrorCode` (P2) map ra HTTP status code thật,
và lỗi từ `BaseDbContext` (P3) chảy tới tay client dưới dạng JSON đọc được.
Nếu P2/P3 đúng mà P4 sai 1 chỗ — thường là quên `EnrichResponse` hoặc map sai
1 `ErrorCode` — thì toàn bộ công sức 2 phase trước bị che khuất sau lớp vỏ HTTP
sai.

---

## 1. Ba quyết định không thể đảo ngược sau khi có endpoint đầu tiên

| # | Quyết định | Cái giá nếu đổi sau |
| --- | --- | --- |
| 1 | **`BaseApiController` là dispatcher mỏng, không chứa business logic hay try-catch tuỳ tiện** | Nếu controller bắt đầu tự `try-catch` từng loại exception, N controller sẽ có N cách format lỗi khác nhau — chính lỗi mà P2 §6.3 (`ExceptionHandlingBehavior`) được sinh ra để triệt tiêu. Đảo ngược = audit lại toàn bộ controller |
| 2 | **HTTP status map trực tiếp từ `ErrorCode` (enum value = mã HTTP), không qua bảng tra cứu riêng** | `MapToHttpStatusCode` chỉ là `(int)code`. Nếu sau này có người thêm bảng tra cứu `ErrorCode → HttpStatus` riêng ở tầng API, sẽ có 2 nguồn sự thật — y hệt lỗi `ApiResultStatus` mà P2 §4.3 đã né bằng `ApiErrorIds.StatusForCode` |
| 3 | **Permission enforcement qua 1 filter toàn cục đọc attribute, không qua `[Authorize(Policy=...)]` rải rác từng action** | Đổi sang policy-per-action nghĩa là viết lại **mọi** controller đã có `[RequirePermission]` — attribute đó không tương thích ngược với cách khai policy thủ công |

---

## 2. File inventory — tối thiểu để thoát P4

| # | File | Project | Vai trò |
| --- | --- | --- | --- |
| 1 | `Controllers/BaseApiController.cs` | `Hosting.Api` | Dispatcher: `HandleRequest<T>()`, map `ErrorCode → HTTP`, enrich `TraceId` |
| 2 | `Controllers/BaseCrudApiController.cs` | `Hosting.Api` | 8 endpoint CRUD chuẩn, zero code ở controller con |
| 3 | `Controllers/IApiContext.cs` + `ApiContext.cs` | `Hosting.Api` | Bọc `IMediator` — controller chỉ biết `IApiContext`, không `new MediatR` trực tiếp |
| 4 | `Controllers/ApplicationContext.cs` | `Hosting.Api` | Implement `IApplicationContext` (P2 §7) — nơi DUY NHẤT ráp `ICurrentUser`+`ICacheService`+`ITranslationService` lại |
| 5 | `Middleware/GlobalExceptionHandler.cs` (`VnrExceptionHandler`) | `Hosting.Api` | `IExceptionHandler` — lưới an toàn ngoài cùng, bắt exception **thoát khỏi** MediatR pipeline |
| 6 | `Middleware/DefaultRouteConvention.cs` | `Hosting.Api` | Convention route mặc định cho controller quên khai `[Route]` |
| 7 | `Authorization/RequirePermissionFilter.cs` | `Hosting.Api` | **Cơ chế enforcement thật sự** cho `[RequirePermission]` — global `IAsyncAuthorizationFilter` |
| 8 | `Authorization/PermissionPolicyProvider.cs` + `PermissionAuthorizationHandler.cs` + `PermissionRequirement.cs` | `Hosting.Api` | Đường phụ — cho `[Authorize(Policy="Permission:{key}:{action}")]` khai tay, xem §7.4 |
| 9 | `Authorization/VnrAuthorizationExtensions.cs` | `Hosting.Api` | 1 dòng DI: `services.AddVnrPermissionAuthorization()` |
| 10 | `RequirePermissionAttribute.cs` | `Platform.Domain.Security` (đã có từ P1) | Pure metadata attribute — 3 cách dùng, xem §7.1 |
| 11 | `CrudActionResolver.cs` | `Platform.Application.Security` (đã có từ P2) | Suy `PermissionAction` từ tên method khi attribute không chỉ định |
| 12 | `Behaviors/AuditLogBehavior.cs` | `Hosting.CompositionRoot` | Pipeline behavior thứ 6 (P2 §6.1) — đặt ở đây vì cần `IHttpContextAccessor`, xem §8 |
| 13 | `BaseStartupServices.cs` (đoạn `AddCoreInfrastructure`) | `Hosting.CompositionRoot` | Composition Root thật — nơi thứ tự pipeline behavior (P2 §6.1) được **thi hành**, không chỉ mô tả |
| — | `Extensions/RegisterSwagger.cs`, `RegisterApiVersioning.cs`, `Swagger/*` | `Hosting.Api` | ⏳ cần cho Swagger UI/OpenAPI, không chặn DoD P4 (`curl` vẫn test được endpoint không cần Swagger) |
| — | `HealthChecks/*`, `Middleware/AccessLogMiddleware.cs`, `XssMiddleware.cs`, `MetricsAuthMiddleware.cs` | `Hosting.Api` | ⏳ P6 — hạ tầng vận hành, không phải nền móng request/response |

---

## 3. Thứ tự viết

```
B1. IApiContext/ApiContext + BaseApiController.HandleRequest       (0.5 ngày)
B2. MapToHttpStatusCode + EnrichResponse + 2 nhánh catch grid       (0.5 ngày —
    (xem §4.2)                                                      đây là phần dễ chép sai nhất)
B3. VnrExceptionHandler (lưới an toàn ngoài MediatR)                (~1 giờ)
B4. BaseCrudApiController (8 endpoint)                              (0.5 ngày)
B5. Routing convention (ApiVersion + [Route] pattern)               (~2 giờ)
B6. RequirePermissionAttribute + CrudActionResolver + Filter        (1 ngày)
B7. Wire AuditLogBehavior vào CompositionRoot (đúng vị trí innermost)(~1 giờ)
B8. Composition Root thật: gọi đúng thứ tự AddXxxBehavior            (0.5 ngày —
                                                                       kiểm chứng bằng test, không chỉ đọc code)
```

---

## 4. `BaseApiController` — dispatcher mỏng, đọc từng dòng

```csharp
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public abstract class BaseApiController : ControllerBase
{
    protected readonly IMediator _mediator;

    protected BaseApiController(IApiContext apiContext) => _mediator = apiContext.Mediator;

    protected async Task<IActionResult> HandleRequest<T>(IRequest<T> request, CancellationToken cancellationToken = default)
    {
        var ct = cancellationToken == CancellationToken.None ? HttpContext.RequestAborted : cancellationToken;
        T result;
        try
        {
            result = await _mediator.Send(request, ct);
        }
        catch (ErrorDescriptorException ex) { /* §4.2 */ }
        catch (FluentValidation.ValidationException ex) { /* §4.2 */ }

        if (result is IActionResult actionResult) return actionResult;

        if (result is IHasApiResultStatus apiResult)
        {
            EnrichResponse(result);
            return StatusCode(MapToHttpStatusCode(apiResult.Code), result);
        }

        return Ok(result);
    }

    private static int MapToHttpStatusCode(ErrorCode code)
        => code == ErrorCode.Success ? StatusCodes.Status200OK : (int)code;
}
```

### 4.1 Vì sao `[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]` nằm ở class, không phải từng action

Xác thực (authentication — "bạn là ai") và phân quyền (authorization — "bạn
được làm gì") là 2 lớp tách biệt trong hệ thống này:

- **Xác thực** khai 1 lần ở `BaseApiController` — mọi controller kế thừa **mặc
  định yêu cầu JWT hợp lệ**. Muốn endpoint public phải tường minh
  `[AllowAnonymous]` (opt-out), không phải mặc định public rồi tường minh khoá
  từng chỗ (opt-in) — an toàn hơn khi có người quên.
- **Phân quyền** là lớp riêng, tường minh per-resource qua `[RequirePermission]`
  (§7) — không có quyền mặc định nào được cấp chỉ vì đã đăng nhập.

### 4.2 Hai nhánh `catch` — vá cho response type không mang được envelope

Đây là chi tiết dễ bị coi là "code thừa" nếu không đọc comment gốc, nhưng thực
ra vá đúng 1 lỗ hổng thật: `ExceptionHandlingBehavior` (P2 §6.3) xây `ApiResult<T>`
đúng envelope khi `TResponse` là `IApiResult<T>` — nhưng **grid pipeline trả
`PagedResult<T>` trần**, không implement `IHasApiResultStatus`. Khi handler ném
lỗi (vd trang vượt quá tổng số), `ExceptionHandlingBehavior` không dựng được
envelope cho kiểu trả về đó nên phải **rethrow** — nếu `BaseApiController` không
bắt lại, exception rơi thẳng ra `VnrExceptionHandler` (§8) và trả **500**, dù
lỗi thật ra là 400 (client gửi filter sai, trang sâu quá) — client-fixable
error bị báo nhầm thành system error.

```csharp
catch (ErrorDescriptorException ex)
{
    var translator = HttpContext.RequestServices.GetService<ITranslationService>();
    var (logId, traceId) = ApiErrorIds.Build();
    var error = new ApiResult
    {
        // Status derive từ ErrorCode — KHÔNG hardcode BUSINESS_ERROR: 2 lỗi cùng loại
        // (GridErrors.InvalidFilter) không được trả Status khác nhau tuỳ đường nào ném ra nó.
        Status = ApiErrorIds.StatusForCode(ex.Descriptor.ErrorCode),
        Message = ex.Resolve(translator),
        Code = ex.Descriptor.ErrorCode,
        BusinessCode = ex.Descriptor.BusinessCode,
        LogId = logId,
        TraceId = traceId ?? HttpContext.TraceIdentifier,
    };
    return StatusCode(MapToHttpStatusCode(ex.Descriptor.ErrorCode), error);
}
catch (FluentValidation.ValidationException ex)
{
    // Dựng qua ValidationFailureMapper — CÙNG hàm mà ExceptionHandlingBehavior dùng,
    // để 2 đường ra (qua envelope bình thường vs qua đây) không bao giờ lệch Field/Code/Message.
    var translator = HttpContext.RequestServices.GetService<ITranslationService>();
    var (logId, traceId) = ApiErrorIds.Build();
    var (fields, message) = ValidationFailureMapper.Build(ex.Errors, translator, ex.Message);
    return StatusCode(MapToHttpStatusCode(ErrorCode.ValidationError), new ApiResult
    {
        Status = ApiErrorIds.StatusForCode(ErrorCode.ValidationError),
        Message = message, Code = ErrorCode.ValidationError, Fields = fields,
        LogId = logId, TraceId = traceId ?? HttpContext.TraceIdentifier,
    });
}
```

3 nguyên tắc rút ra, áp dụng cho bất kỳ response type mới nào sau này **không**
implement `IHasApiResultStatus` (không chỉ riêng grid):

1. **Không bịa logic build envelope lần 2** — gọi lại đúng hàm mà pipeline
   dùng (`ApiErrorIds.StatusForCode`, `ValidationFailureMapper.Build`). Copy-paste
   logic ra đây là nguồn drift đầu tiên sẽ xảy ra.
2. **`LogId`/`TraceId` build theo cùng format** (`ApiErrorIds.Build()`) —
   admin grep log theo 1 format duy nhất, không phải đoán response này sinh ra
   từ đường nào.
3. **Đây là cơ chế vá cho 1 loại response cụ thể (`PagedResult<T>`), không phải
   giấy phép để controller tự `try-catch` tuỳ ý.** Nếu một response type mới cần
   thêm 1 nhánh catch ở đây, trước tiên phải tự hỏi: response đó có nên implement
   `IHasApiResultStatus` ngay từ đầu không? Chỉ khi câu trả lời là "không, vì lý
   do kỹ thuật thật sự" (giống `PagedResult<T>` — dùng chung cho cả response
   thành công) mới thêm nhánh catch ở đây.

### 4.3 `EnrichResponse` — tại sao cast interface, không `GetProperty/SetValue`

```csharp
private void EnrichResponse(object result)
{
    if (result is IApiResultEnrichable enrichable)
        enrichable.TraceId ??= HttpContext.TraceIdentifier;
}
```

`IApiResultEnrichable` (P2 §4.6) tồn tại đúng để dòng này không phải reflection.
`HandleRequest<T>` chạy trên **mọi** request đi qua **mọi** controller — đây là
hot path thật sự của cả hệ thống, không phải chỗ được phép "tiện thì dùng
reflection cho nhanh viết code".

### 4.4 `Error(object result)` — lối thoát cho action không qua `HandleRequest`

```csharp
protected IActionResult Error(object result)
{
    if (result is IHasApiResultStatus apiResult)
    {
        EnrichResponse(result);
        return StatusCode(MapToHttpStatusCode(apiResult.Code), result);
    }
    return Ok(result);
}
```

Dùng khi action trả **non-JSON lúc thành công** (vd `File(...)` cho tải file
export) nhưng vẫn cần map đúng HTTP status khi handler trả lỗi — không hardcode
200 cho action đó. Đây là method public duy nhất khác `HandleRequest` mà
controller con được phép gọi trực tiếp — không phải cửa sau để bypass toàn bộ
cơ chế.

---

## 5. `BaseCrudApiController<TResult, TRequest, TKey>` — 0 dòng code ở module

```csharp
public abstract class BaseCrudApiController<TResult, TCreateRequest, TUpdateRequest, TKey> : BaseApiController
    where TResult : BaseDto, new()
    where TCreateRequest : class, new()
    where TUpdateRequest : class, new()
    where TKey : IEquatable<TKey>
{
    [HttpPost("list")]
    public virtual Task<IActionResult> GetAll([FromBody] BaseRequestGridModel gridRequest)
        => HandleRequest(new QueryListGrid<TResult>(gridRequest));

    [HttpPost("export-excel")]
    public virtual Task<IActionResult> ExportExcel([FromBody] BaseRequestGridModel gridRequest)
        => HandleRequest(new ExportExcelCommand<TResult>(gridRequest, PermissionResourceKey));

    [HttpGet("{id}")]
    public virtual Task<IActionResult> GetById([FromRoute] TKey id)
        => HandleRequest(new Query<TKey, TResult>(id));

    [HttpGet("lookup")]
    public virtual Task<IActionResult> Lookup(
        [FromQuery] string? search = null, [FromQuery] int take = 50,
        [FromQuery] List<Guid>? alwaysIncludes = null, [FromQuery] bool includeInactive = false)
        => HandleRequest(new QueryLookup<TResult> { Search = search, Take = take,
            AlwaysIncludes = alwaysIncludes ?? [], IncludeInactive = includeInactive });

    [HttpPost]
    public virtual Task<IActionResult> Create([FromBody] TCreateRequest dto)
        => HandleRequest(new CreateCommand<TCreateRequest, TResult>(dto));

    [HttpPut("{id}")]
    public virtual Task<IActionResult> Update([FromRoute] TKey id, [FromBody] TUpdateRequest dto)
        => HandleRequest(new UpdateCommand<TKey, TUpdateRequest, TResult>(id, dto));

    [HttpDelete("{id}")]
    public virtual Task<IActionResult> Delete([FromRoute] TKey id)
        => HandleRequest(new DeleteCommand<TKey, TResult>(id));

    [HttpPost("batch-delete")]
    public virtual Task<IActionResult> DeleteRange([FromBody] List<TKey> ids)
        => HandleRequest(new DeleteRangeCommand<TKey, TResult>(ids));
}

// Overload tiện dụng khi Create/Update dùng chung 1 DTO:
public abstract class BaseCrudApiController<TResult, TRequest, TKey>
    : BaseCrudApiController<TResult, TRequest, TRequest, TKey>
    where TResult : BaseDto, new() where TRequest : class, new() where TKey : IEquatable<TKey>;
```

Controller con thật (`ProvinceController`, đọc verbatim từ Successor) đúng
**4 dòng**:

```csharp
[RequirePermission(RefResourceKeyNames.Province)]
[ApiVersion("1")]
[Route("api/v{version:apiVersion}/ref/[controller]")]
public class ProvinceController : BaseCrudApiController<ProvinceResponse, ProvinceRequest, Guid>
{
    public ProvinceController(IApiContext apiContext) : base(apiContext) { }
}
```

**Đây chính là "pattern 1 — zero-handler" đã hứa ở
[00-lộ-trình §1 nguyên tắc #4](00-lo-trinh-tong-the.md).** `GetAll`/`Create`/
`Update`/`Delete` build sẵn `CreateCommand<,>`/`UpdateCommand<,,>`/... — những
command **generic** này không có handler riêng của `Province`, chúng rơi vào
`CrudHandlerBehavior` (P2 §5, fallback handler) chạy trực tiếp trên
`ICrudRepository<TEntity,TKey>` (P3 §8.1). Route → Command → generic fallback
handler → generic repository — không một dòng code nghiệp vụ nào cho catalog
CRUD thuần.

### 5.1 Vì sao `batch-delete` là `POST`, không phải `DELETE` kèm body

```csharp
[HttpPost("batch-delete")]
public virtual Task<IActionResult> DeleteRange([FromBody] List<TKey> ids) => ...
```

Comment gốc trích RFC 9110 §9.3.5: *"DELETE body has no defined semantics."*
`DELETE` với body là hành vi không chuẩn hoá — một số proxy/gateway/API client
tự ý drop body trên request `DELETE`. Chọn `POST /batch-delete` là quyết định
tường minh đánh đổi "đúng chuẩn REST cho single-resource DELETE" lấy "hoạt động
đáng tin cậy qua mọi hạ tầng trung gian" cho thao tác hàng loạt.

### 5.2 `PermissionResourceKey` — cầu nối giữa attribute và command cần biết resource

```csharp
protected virtual string PermissionResourceKey
{
    get
    {
        var attribute = GetType()
            .GetCustomAttributes(typeof(RequirePermissionAttribute), inherit: true)
            .Cast<RequirePermissionAttribute>().FirstOrDefault();
        return attribute?.ResourceKeys.FirstOrDefault() ?? string.Empty;
    }
}
```

`ExportExcelCommand<TResult>` cần biết **resource key nào** đang được export
(để ghi log/áp thêm rule) — nhưng resource key đã khai ở `[RequirePermission]`
trên class, không nên khai lại ở method. `PermissionResourceKey` đọc lại chính
attribute đó bằng reflection **1 lần/request** (không phải hot path bằng
`HandleRequest`, chấp nhận được) — giữ đúng nguyên tắc "một luật, một nguồn"
(P0 §1 nguyên tắc #6): resource key chỉ khai đúng 1 chỗ (`[RequirePermission]`),
mọi nơi khác đọc lại, không khai riêng.

---

## 6. Routing convention — 2 lớp

| Lớp | Cơ chế | Khi nào áp dụng |
| --- | --- | --- |
| Tường minh | `[ApiVersion("1")]` + `[Route("api/v{version:apiVersion}/{module-prefix}/[controller]")]` trên **từng controller** | Mọi controller thật trong module — không có ngoại lệ |
| Fallback | `DefaultRouteConvention : IControllerModelConvention` → gán `api/[controller]/[action]` cho controller **quên khai `[Route]`** | Lưới an toàn lúc dev — không phải quy ước chính thức để dựa vào |

```csharp
public class DefaultRouteConvention : IControllerModelConvention
{
    public void Apply(ControllerModel controller)
    {
        var assemblyName = controller?.ControllerType?.Assembly?.GetName().Name;
        if (_targetAssemblyNames.Contains(assemblyName) && controller != null
            && !controller.Attributes.OfType<RouteAttribute>().Any())
        {
            controller.Selectors.Add(new SelectorModel {
                AttributeRouteModel = new AttributeRouteModel(new RouteAttribute("api/[controller]/[action]")) });
        }
    }
}
```

`DefaultRouteConvention` chỉ áp dụng cho assembly nằm trong danh sách
`targetAssemblyNames` được truyền vào lúc đăng ký (`RegisterController.cs`) —
không quét toàn bộ process, tránh áp route ngầm định lên assembly bên thứ ba
(vd MVC framework nội bộ, Swagger UI). **[ĐƠN GIẢN HOÁ] cho hệ thống mới:** có
thể bỏ hẳn convention này và **bắt buộc** mọi controller khai `[Route]` tường
minh — ArchTest chặn thiếu `[Route]` thay vì fallback êm ái. Ít linh hoạt hơn
nhưng không có route "vô tình đúng" nhờ convention mà không ai chủ đích viết.

---

## 7. Auth/Permission — 2 lớp tách biệt, 1 attribute duy nhất phía dev

> **Trạng thái áp dụng (2026-08-17):** PlatformManager hiện dùng bản rút gọn
> của mục này — xem `src/BE/.claude/rules/api-controller.md` §"Phân quyền
> theo hành động" (1 attribute + 1 filter, không `CrudActionResolver`, không
> 2 cơ chế song song). Nâng cấp lên đầy đủ như dưới đây khi có module nghiệp
> vụ thứ 2+ hoặc cần suy luận action tự động theo tên method.

### 7.1 `[RequirePermission]` — 3 cách dùng, khai ở `Platform.Domain.Security` (P1)

```csharp
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequirePermissionAttribute : Attribute
{
    public RequirePermissionAttribute(string resourceKey);                              // auto-CRUD
    public RequirePermissionAttribute(string resourceKey, PermissionAction action);      // explicit
    public RequirePermissionAttribute(PermissionAction action, params string[] resourceKeys); // multi-key OR
}
```

| Cách dùng | Ví dụ | Khi nào |
| --- | --- | --- |
| Auto-CRUD | `[RequirePermission("ref.province")]` trên class | Controller kế thừa `BaseCrudApiController` — action tự suy `PermissionAction` từ tên method (§7.2) |
| Explicit | `[RequirePermission("ref.province", PermissionAction.Export)]` trên 1 method cụ thể | Method không thuộc 8 CRUD chuẩn (vd `Upload`, `Download`) — `CrudActionResolver` trả `null` nên **phải** khai tường minh, không có "đoán mò theo HTTP verb" |
| Multi-key OR | `[RequirePermission(PermissionAction.View, "hre.employee", "hre.employee-lite")]` | User cần **1 trong nhiều** resource key — check batch 1 round-trip qua `HasAnyPermissionAsync`, tránh N+1 |

**Vì sao attribute này khai ở `Platform.Domain.Security`, không phải
`Hosting.Api`:** attribute là *pure metadata* (không có logic runtime, không
`using Microsoft.AspNetCore.*`) — module viết entity/domain có thể gắn attribute
lên chính class controller của mình mà `Module.*.Api` chỉ cần reference
`Platform.Domain`, không cần biết `Hosting.Api` tồn tại lúc biên dịch attribute.
Logic enforcement thật (đọc attribute, gọi `IPermissionChecker`) nằm hoàn toàn ở
`Hosting.Api` (§7.3) — tách đúng theo hướng phụ thuộc `Module.Api →
Platform.Domain`, không phải `Module.Api → Hosting.Api` cho riêng phần này.

### 7.2 `CrudActionResolver` — bảng suy luận action, khai ở `Platform.Application.Security` (P2)

```csharp
private static readonly Dictionary<string, PermissionAction> MethodMap = new(StringComparer.OrdinalIgnoreCase)
{
    ["List"] = View, ["GetAll"] = View, ["Search"] = View,
    ["Lookup"] = Detail, ["GetById"] = Detail, ["Detail"] = Detail,
    ["Create"] = Create, ["Add"] = Create,
    ["Update"] = Modify, ["Edit"] = Modify, ["Modify"] = Modify,
    ["Delete"] = Delete, ["DeleteRange"] = Delete, ["Remove"] = Delete,
    ["Export"] = Export,
};

public static PermissionAction? Resolve(string actionMethodName, string httpMethod)
{
    if (MethodMap.TryGetValue(actionMethodName, out var action)) return action;         // exact match
    foreach (var (prefix, mapped) in MethodMap)
        if (actionMethodName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return mapped; // prefix (vd "CreateBulk" → Create)
    return null;   // KHÔNG fallback theo HTTP verb — method lạ = không đoán
}
```

XML doc gốc nhấn mạnh điều **cố ý không làm**: *"CỐ Ý không fallback theo HTTP
verb để tránh áp quyền 'đoán mò' lên endpoint non-CRUD."* Đây là nguyên tắc an
toàn quan trọng hơn tiện lợi: một method tên `Upload` gắn `POST` — nếu resolver
fallback theo verb (`POST → Create`), attribute auto-CRUD sẽ áp nhầm quyền
`Create` lên 1 hành động hoàn toàn khác về bản chất. Trả `null` buộc dev phải
tự khai tường minh `[RequirePermission(key, action)]` cho method lạ — chậm hơn
1 dòng code nhưng không bao giờ áp sai.

### 7.3 `RequirePermissionFilter` — cơ chế enforcement thật

`IAsyncAuthorizationFilter` (chạy **trước** model binding — chặn request sớm
nhất có thể, đúng khuyến nghị của Microsoft là ưu tiên hơn action filter):

```
1. [AllowAnonymous] trên action/controller?           → bỏ qua permission check
2. Gom attribute: method-level trước, class-level sau  → method-level THẮNG HẲN
   (không merge — có method-level thì bỏ qua class-level, không phải AND)
3. SuperAdmin claim ("hrm_is_super_admin" = "true")?   → bypass tất cả, 0 I/O
4. Với mỗi attribute còn lại (AND — tất cả phải pass):
     action = attribute.Action ?? CrudActionResolver.Resolve(methodName, httpMethod)
     action == null (method non-CRUD, chưa khai explicit) → coi như PASS attribute này
     1 resource key  → IPermissionChecker.HasPermissionAsync    (1 round-trip)
     N resource keys → IPermissionChecker.HasAnyPermissionAsync (1 round-trip, OR)
     fail bất kỳ key nào cần AND → 403, dừng ngay (không check tiếp attribute sau)
```

Điểm dễ hiểu lầm nhất: **method-level ghi đè hoàn toàn class-level, không cộng
dồn.** Một class gắn `[RequirePermission("ref.province")]` (auto-CRUD) nhưng
1 method riêng gắn `[RequirePermission("ref.province", PermissionAction.Export)]`
— thì **chỉ** attribute trên method đó được kiểm tra, class-level bị bỏ qua
hoàn toàn cho method đó. Muốn AND cả 2 mức phải sửa code filter (comment gốc
trỏ thẳng tới tài liệu nội bộ Successor mô tả "Cách B" cho nhu cầu này) — mặc
định không hỗ trợ.

### 7.4 2 cơ chế cùng tồn tại — cái nào mới là đường enforcement thật

`AddVnrPermissionAuthorization()` đăng ký **cả 2** hệ thống song song:

| Cơ chế | Kích hoạt bởi | Enforcement path |
| --- | --- | --- |
| `RequirePermissionFilter` (global MVC filter) | `[RequirePermission]` trên controller/action | **Đường thật** — đọc attribute trực tiếp, gọi `IPermissionChecker` thẳng, không qua `IAuthorizationService` |
| `PermissionPolicyProvider` + `PermissionAuthorizationHandler` | `[Authorize(Policy = "Permission:{key}:{action}")]` khai tay | Đường phụ — dùng hệ thống policy-based chuẩn của ASP.NET Core, đòi hỏi biết `key`/`action` **lúc biên dịch** (chuỗi cứng trong `Policy=`) |

Comment gốc của `RequirePermissionFilter` giải thích thẳng lý do có 2 cơ chế:
*"`RequirePermissionAttribute` supports auto-CRUD — action is resolved at
runtime... `AuthorizeAttribute.Policy` requires a compile-time string."*
`[RequirePermission]` (đường thật, dùng cho **mọi** controller CRUD) không thể
diễn đạt qua policy string cố định vì action của nó có thể suy luận runtime
(auto-CRUD). Cơ chế policy-based vẫn được giữ lại làm đường phụ cho những chỗ
hiếm cần khai tường minh 1 policy cụ thể ngoài khuôn CRUD.

**[ĐƠN GIẢN HOÁ] cho hệ thống mới:** nếu không có nhu cầu dùng
`[Authorize(Policy="Permission:...")]` khai tay ở đâu cả, có thể bỏ hẳn
`PermissionPolicyProvider`/`PermissionAuthorizationHandler`/`PermissionRequirement`
(3 file), chỉ giữ `RequirePermissionFilter` — nhưng phải ghi rõ vào ADR đây là
cắt bớt tính năng, không phải quên implement.

---

## 8. `VnrExceptionHandler` — lưới an toàn ngoài cùng, khác tầng với `ExceptionHandlingBehavior`

```csharp
public class VnrExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        if (httpContext.Response.HasStarted) return false;
        var logId = await _appLogService.LogErrorAsync("Unhandled exception", exception, httpContext.TraceIdentifier);
        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = "application/json";
        await httpContext.Response.WriteAsJsonAsync(new ApiResult
        {
            Status = ApiResultStatus.SYSTEM_ERROR,
            Message = ExceptionMessageBuilder.BuildForEnvironment(exception, _environment),
            LogId = logId, TraceId = httpContext.TraceIdentifier, Retryable = false
        }, ct);
        return true;
    }
}
```

**Không nhầm với `ExceptionHandlingBehavior` (P2 §6.3)** — 2 lưới an toàn ở 2
tầng khác nhau, bắt 2 loại lỗi khác nhau:

| | `ExceptionHandlingBehavior` | `VnrExceptionHandler` |
| --- | --- | --- |
| Tầng | MediatR pipeline (trong `_mediator.Send()`) | ASP.NET Core middleware (`UseExceptionHandler`) |
| Bắt exception từ | Handler, domain logic, mọi behavior khác trong pipeline | Middleware, action filter, **model binding** — thứ nằm **ngoài** `_mediator.Send()` |
| Luôn trả | `ApiResult<T>` đúng envelope, HTTP status theo `ErrorCode` | Luôn **500** — không biết `ErrorCode` gì vì exception chưa từng đi qua pipeline có `ErrorDescriptor` |
| Khi nào thực sự kích hoạt | Hầu hết mọi lỗi nghiệp vụ | Hiếm — vd JSON malformed khiến model binding tự throw trước khi vào controller |

`ExceptionMessageBuilder.BuildForEnvironment` (không viết lại chi tiết ở đây,
đã tồn tại từ `Platform.Application.Common`) ẩn stack trace ở môi trường
Production, hiện đầy đủ ở Development — cùng nguyên tắc với P2 §6.3
(`NormalizeField`): **không bao giờ lộ chi tiết nội bộ ra client Production**,
`VnrExceptionHandler` là lớp phòng thủ cuối cùng nên nguyên tắc này áp dụng
nghiêm ngặt nhất ở đây.

---

## 9. Cấm ở tầng Hosting.Api

| Cấm | Vì sao | ArchTest gợi ý |
| --- | --- | --- |
| Controller tự `try-catch` ngoài 2 nhánh đã có sẵn trong `HandleRequest` | Phá nguyên tắc "1 nơi format lỗi" — xem §1 quyết định #1 | Review thủ công + `T_API_NoCustomTryCatch` (quét `Controllers/*` tìm `catch` block ngoài base) |
| Controller inject `DbContext`/`IUnitOfWork`/repository trực tiếp | `Hosting.Api` **không** reference `Persistence` (P0 §1) — controller chỉ biết `IMediator` qua `IApiContext` | `T_LAYER_HostingApi_NoPersistence` |
| Hardcode HTTP status code (`return StatusCode(500, ...)`) thay vì đi qua `MapToHttpStatusCode(ErrorCode)` | Tạo nguồn sự thật thứ 2 cho mapping `ErrorCode → HTTP` — xem §1 quyết định #2 | Review thủ công |
| `[Authorize(Policy = "Permission:...")]` khai tay cho endpoint CRUD chuẩn (đáng lẽ dùng `[RequirePermission]` auto-CRUD) | 2 cơ chế cùng tồn tại cho 1 endpoint gây nhầm — policy string cứng không tự cập nhật khi resource key đổi tên | Review thủ công |
| `RequirePermissionAttribute` gắn action `null` (auto-CRUD) lên method không nằm trong `CrudActionResolver.MethodMap` mà không kiểm tra kỹ | Attribute âm thầm **không có tác dụng gì** (resolver trả `null` → filter pass luôn) — endpoint tưởng được bảo vệ nhưng thực ra mở toang | `T_SEC_UnprotectedAutoCrud` — quét method có `[RequirePermission(key)]` (auto-CRUD, không action) mà tên không khớp `MethodMap` |

---

## 10. Kiểm chứng bằng test thật — không chỉ đọc code

Đây là 3 test **phải chạy đỏ nếu cố tình phá**, giống tinh thần P0 §7 và P2 §9:

1. **`ConflictException` (P3 §6) → HTTP 409**: gọi `Create` 2 lần với cùng giá
   trị field unique → response thứ 2 phải là 409, `BusinessCode` đọc được,
   không phải 500.
2. **`ValidationException` → HTTP 400 kèm `Fields`**: gọi `Create` thiếu field
   bắt buộc → 400, `Fields["TenTruong"]` tồn tại, `Message` không lộ stack
   trace.
3. **Thiếu quyền → 403, không phải exception 500**: user có JWT hợp lệ nhưng
   không có `[RequirePermission]` yêu cầu → 403 tại `RequirePermissionFilter`,
   request **chưa từng chạm** `_mediator.Send()` (kiểm chứng bằng cách handler
   có side-effect quan sát được, vd log, và log đó **không xuất hiện**).

---

## 11. Checklist rời P4

- [ ] `GET /api/v1/health` (hoặc endpoint tương đương) trả 200 không cần JWT
- [ ] 1 controller kế thừa `BaseCrudApiController` — 4 dòng code, đủ 8 endpoint
- [ ] `HandleRequest<T>` map đúng `ErrorCode → HTTP` cho cả đường `IApiResult<T>` thường lẫn 2 nhánh catch (`ErrorDescriptorException`, `FluentValidation.ValidationException`)
- [ ] `EnrichResponse` set `TraceId` qua `IApiResultEnrichable`, không reflection
- [ ] `[RequirePermission]` hoạt động đủ 3 cách dùng (§7.1), method-level ghi đè class-level đúng như §7.3
- [ ] SuperAdmin claim bypass đúng, 0 I/O (kiểm chứng bằng cách không có call `IPermissionChecker` nào khi user là SuperAdmin)
- [ ] `VnrExceptionHandler` chỉ bắt exception thoát khỏi MediatR pipeline — không trùng lặp với `ExceptionHandlingBehavior`
- [ ] `Composition Root` (`AddCoreInfrastructure`) gọi đúng thứ tự: `RegisterExceptionHandlingBehavior → RegisterLoggingBehavior → RegisterPlatformValidationBehavior → RegisterModuleTransactionBehaviors → RegisterCrudHandlerBehavior`, và `AuditLogBehavior` đăng ký **sau** cùng (innermost) ở bước audit setup riêng
- [ ] Cả 3 test ở §10 đã viết và **đã kiểm chứng đỏ** khi cố tình phá (giống nguyên tắc P0 §7 và P2 §9 — ArchTest/integration test chưa từng đỏ là chưa được chứng minh)
- [ ] Route pattern nhất quán: `api/v{version:apiVersion}/{module-prefix}/[controller]`, mọi controller có `[ApiVersion]` + `[Route]` tường minh (hoặc rơi đúng vào `DefaultRouteConvention` nếu chọn giữ fallback)

---

**Tiếp theo:** [06-p5-module-dau-tien.md](06-p5-module-dau-tien.md) — dựng module
nghiệp vụ đầu tiên thật sự, đi qua cả 4 phase vừa xây (Domain → Application →
Persistence → Api) cho **2 pattern CRUD** cùng lúc: 1 entity catalog thuần
(zero-handler, giống `ProvinceController` ở §5) và 1 entity nghiệp vụ (vertical
slice tường minh, có handler riêng) — nơi ranh giới "2 cực, không có tầng giữa"
(P0 §1 nguyên tắc #4) được thi hành lần đầu trên code thật của chính hệ thống
mới, không phải trích dẫn từ Successor.

# P5 — Module đầu tiên: 2 pattern CRUD

> 📍 **Tên project trong file này là của VNR.Successor, không phải PlatformManager.**
> Tra bảng ánh xạ + 4 mục "KHÔNG áp dụng" ở [`00-lo-trinh-tong-the.md`](00-lo-trinh-tong-the.md)
> §ĐỌC TRƯỚC. Tóm tắt: `Platform.*`→`Core.*` · `Module.{M}.*`→tầng nghiệp vụ (`Business.*`) ·
> `Processes/`→**1** host · JWT→**cookie session** · per-module DbContext→**1** DbContext chung.

> **Định nghĩa hoàn thành:** 1 entity catalog thuần (giống `Cat_Province`) chạy
> đủ CRUD qua HTTP với **zero dòng handler**. 1 entity nghiệp vụ (có validate
> DB-dependent, có domain factory, có lỗi nghiệp vụ riêng) chạy đủ CRUD qua
> **vertical slice tường minh** — mỗi command/query có handler riêng, viết tay,
> không qua fallback nào. Cả hai cùng đi qua đúng 4 phase đã dựng (P1→P2→P3→P4)
> mà **không cần sửa** bất kỳ file nào ở 4 phase đó.

Đây là phase kiểm chứng toàn bộ core: nếu P1–P4 đúng, viết module đầu tiên chỉ
là "điền vào chỗ trống" theo khuôn đã có. Nếu phải quay lại sửa `Platform.*` ở
P5, nghĩa là một quyết định nào đó ở P1–P4 sai giả định — bắt lỗi ở đây rẻ hơn
nhiều so với bắt lỗi ở module thứ 5.

---

## 1. Ranh giới không thể mờ: khi nào dùng pattern nào

| | Pattern 1 — Zero-handler (Catalog) | Pattern 2 — Vertical slice (Nghiệp vụ) |
| --- | --- | --- |
| Dùng khi | Entity là danh mục thuần: Name/Code/IsActive, CRUD không có luật gì ngoài "trùng Code thì báo lỗi" | Entity có luật nghiệp vụ: validate phụ thuộc DB, ghi nhiều bảng cùng lúc, cascade thủ công, tính toán | 
| Ví dụ thật | `Cat_Province`, `Cat_Country`, `Cat_Currency`, `Cat_Major` (10+ entity trong `ReferenceData`) | `Scc_PotentialGroup` (ghi 2 bảng/lần tạo, validate trùng tên qua DB, cascade xoá mềm thủ công) |
| Handler | Không viết — `CrudHandlerBehavior` tự dispatch | Viết tay, 1 file/command hoặc query |
| Đăng ký | 1 dòng: `services.AddCatalogCrud<TEntity,TRequest,TResponse>()` | Tự động qua `RegisterPlatformValidators` (validator) + MediatR auto-scan handler theo `IRequestHandler<,>` |
| Mapping | `AutoMapper`, tự sinh từ `CrudEntityRegistry` | Tay, trong chính handler (domain factory `Entity.Create(...)`) |

**Không có tầng giữa.** Một entity "hơi có 1 chút luật" (vd validate FK đơn
giản) vẫn dùng được pattern 1 nếu luật đó diễn đạt được qua `CrudUniqueValidator`/
`CrudFkValidator` (§2.3) — chỉ chuyển sang pattern 2 khi luật **không** diễn đạt
được bằng 2 validator generic đó (cần đọc nhiều bảng, cần tính toán, cần ghi
nhiều bảng trong 1 transaction). Đây chính là ranh giới mà [00-lộ-trình §1
nguyên tắc #4](00-lo-trinh-tong-the.md) đã đặt tên "2 cực, không có tầng giữa" —
P5 là nơi ranh giới đó lần đầu phải áp dụng vào quyết định thật, không phải câu
trích dẫn.

---

## 2. Pattern 1 — Zero-handler catalog CRUD, đọc từ trong ra ngoài

### 2.1 Route → Command generic — không có gì thuộc về `Province`

`ProvinceController` (P4 §5) build `CreateCommand<ProvinceRequest, ProvinceResponse>`
— một kiểu **hoàn toàn generic**, không mang bất kỳ thông tin nào về
`Cat_Province`. `CrudHandlerBehavior` (P2 §5, đăng ký vị trí 5 trong pipeline)
kiểm tra trước tiên:

```csharp
public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
{
    // Có handler đăng ký tường minh cho command này? → nhường (pattern 2 luôn thắng nếu có)
    if (_serviceProvider.GetService(typeof(IRequestHandler<TRequest, TResponse>)) is not null)
        return await next();

    return await DispatchToCrudHandler(request, ct);   // fallback pattern 1
}
```

**Đây là luật ưu tiên quan trọng nhất của cả cơ chế**: nếu một entity **đã có**
handler tường minh (`IRequestHandler<CreateCommand<TRequest,TResult>, ...>`
đăng ký thật), pipeline luôn nhường cho handler đó — `CrudHandlerBehavior` chỉ
chạy khi **không có ai nhận việc**. Một module có thể bắt đầu bằng pattern 1
(catalog thuần) rồi sau này nâng cấp 1 action riêng (vd `Export`) lên handler
tường minh **mà không cần đụng tới các action CRUD khác** — chúng vẫn tiếp tục
rơi vào fallback.

### 2.2 Resolve entity từ DTO — registry trước, attribute sau

```csharp
private static Type? ResolveEntityFromDto(Type dtoType)
{
    var registered = CrudEntityRegistry.ResolveEntity(dtoType);      // ưu tiên 1 — O(1)
    if (registered is not null) return registered;

    var attr = dtoType.GetCustomAttribute<CrudEntityAttribute>();    // ưu tiên 2
    if (attr is not null) return attr.EntityType;

    return null;   // → InvalidOperationException, dispatch fail-fast lúc request đầu tiên
}
```

`CrudHandlerBehavior` không biết `Cat_Province` là gì cho tới khi nó tra
`CrudEntityRegistry` — registry được nạp **1 lần lúc khởi động**, qua đúng 1
dòng gọi trong `Add{Module}Infrastructure` (§2.4). Đây là cơ chế cho phép
`CreateCommand<ProvinceRequest, ProvinceResponse>` (Platform, không biết
`Cat_Province` tồn tại) và `Cat_Province` (module, không biết `CreateCommand`
tồn tại) gặp nhau đúng lúc runtime mà không project nào reference project kia
theo hướng sai.

### 2.3 `CrudUniqueValidator`/`CrudFkValidator` — luật chung, không phải business logic riêng

`CrudHandlerBehavior.DispatchToCrudHandler` gọi 2 validator generic **trước khi
dispatch**, không phải bên trong handler generic:

```csharp
case RequestKind.Create:
    var (entity, key) = ResolveOrThrow(args[1], requestType);
    await ValidateUnique(request, entity, key, cancellationToken);   // CrudUniqueValidator
    var cht = CrudDispatcher.MakeCrudHandlerType(args[1], args[0], entity, key);
    return await _dispatcher.InvokeAsync<TResponse>(cht, request, requestType, cancellationToken);

case RequestKind.Delete:
case RequestKind.DeleteRange:
    var (entity, _) = ResolveOrThrow(args[1], requestType);
    await ValidateFk(request, entity, args[0], cancellationToken);   // CrudFkValidator — chặn xoá khi còn bị tham chiếu
    ...
```

Đây là ranh giới thật của "1 chút luật vẫn ở pattern 1" (§1): trùng-unique-field
lúc tạo/sửa và còn-bị-tham-chiếu lúc xoá là 2 luật **đủ phổ quát** để Platform
viết chung 1 lần cho mọi catalog, thay vì bắt mỗi entity tự viết validator
riêng chỉ để kiểm tra đúng 2 điều đó.

### 2.4 Đăng ký — đúng 1 dòng, không mapper profile, không validator riêng

```csharp
// {Module}InfrastructureExtensions.cs
services.AddCatalogCrud<Cat_Province, ProvinceRequest, ProvinceResponse>();
```

1 dòng này (định nghĩa đầy đủ, đọc verbatim `CatalogCrudRegistrationExtensions.cs`)
làm **3 việc**:

```csharp
public static IServiceCollection AddCatalogCrud<TEntity, TRequest, TResponse>(
    this IServiceCollection services, bool requireCode = true)
    where TEntity : CatalogEntityBase<Guid>
    where TRequest : BaseCatalogRequest, new()
    where TResponse : BaseCatalogDto, new()
{
    CrudEntityRegistry.Register<TResponse, TRequest, TEntity>();                         // 1. DTO ↔ Entity
    CrudEntityRegistry.RegisterCatalogMapping(typeof(TRequest), typeof(TEntity), typeof(TResponse)); // 2. cho AutoMapper
    AddSharedCatalogValidators<TRequest, TResponse>(services, requireCode);              // 3. validator Name/Code Required
    return services;
}
```

`ReferenceDataInfrastructureExtensions.cs` thật của Successor có **hơn 10 dòng
này liên tiếp** (Country, Province, District, Ward, Village, Ethnic, Nationality,
Religion, Major, Qualification, Currency…) — mỗi dòng là 1 catalog CRUD hoàn
chỉnh, route→HTTP→DB, không một dòng handler nào khác trong toàn bộ luồng.

### 2.5 `CatalogAutoMapperProfile` — AutoMapper tự sinh, không viết profile tay

```csharp
public class CatalogAutoMapperProfile : Profile
{
    public CatalogAutoMapperProfile()
    {
        foreach (var mapping in CrudEntityRegistry.GetCatalogMappings())
        {
            var requestToEntity = CreateMap(mapping.RequestType, mapping.EntityType);
            foreach (var member in IgnoredTargetMembers)          // props của BaseEntity<Guid>
                requestToEntity.ForMember(member, opt => opt.Ignore());

            CreateMap(mapping.EntityType, mapping.ResponseType);   // Entity → Response
        }
    }
}
```

Comment gốc cảnh báo đúng 1 điều dễ vỡ: *"Profile này phải được scan SAU khi
tất cả `AddCatalogCrud` đã chạy."* `CrudEntityRegistry.GetCatalogMappings()`
đọc snapshot **tại thời điểm profile constructor chạy** — nếu `AddAutoMapper`
được gọi **trước** khi module gọi `AddCatalogCrud`, registry còn rỗng, profile
build ra 0 mapping, và lỗi chỉ lộ ra ở request đầu tiên dưới dạng
`AutoMapperMappingException` khó hiểu. Thứ tự gọi trong Composition Root
(`Add{Module}Infrastructure` trước `AddAutoMapper`/`RegisterApplicationPart`)
không phải tuỳ tiện.

---

## 3. Pattern 2 — Vertical slice tường minh, đọc từ file tree thật

### 3.1 Cây file thật (rút gọn từ `Modules/Succession/.../PotentialGroup/`)

```
PotentialGroup/
├── PotentialGroupErrors.cs                 ← toàn bộ ErrorDescriptor của entity này, 1 chỗ
├── PotentialGroupCodeGenerator.cs          ← business helper thuần, không phụ thuộc DB
├── PotentialGroupDeletionRule.cs           ← luật dùng CHUNG giữa handler Delete và query "delete-eligibility"
│
├── Commands/
│   ├── Create/
│   │   ├── CreateSccPotentialGroupCommand.cs      ← : CreateCommand<TRequest,Guid>, ISuccessionCommand, ITransactionalCommand
│   │   ├── CreateSccPotentialGroupRequest.cs      ← DTO body — KHÔNG có logic
│   │   ├── CreateSccPotentialGroupHandler.cs      ← : CommandHandler<TCommand,Guid>(context)
│   │   └── CreateSccPotentialGroupValidator.cs    ← : AbstractValidator<TCommand> — chỉ luật "loại 1"
│   ├── Update/  (cùng khuôn 4 file)
│   └── Delete/
│       ├── DeleteSccPotentialGroupCommand.cs
│       ├── DeleteSccPotentialGroupHandler.cs
│       └── DeleteSccPotentialGroupValidator.cs
│
├── Queries/
│   ├── GetById/
│   │   ├── GetSccPotentialGroupByIdQuery.cs
│   │   ├── SccPotentialGroupDetailResponse.cs
│   │   └── GetSccPotentialGroupByIdHandler.cs     ← : QueryHandler<TQuery,TResponse>(context)
│   ├── GetList/  (Query + Request + Handler + Validator + ListItem)
│   └── GetDeleteEligibility/  (query riêng cho UX xác nhận xoá — §3.5)
│
└── Services/
    ├── ISccPotentialGroupService.cs + SccPotentialGroupService.cs   ← đọc/ghi tái dùng giữa nhiều handler
    └── IPotentialGroupCriteriaValueResolver.cs                       ← seam cho phần đọc DB đặc thù
```

**Không có `Mapper.cs` trong cây này.** Handler tự dựng entity qua domain
factory (`Scc_PotentialGroup.Create(...)`) và tự dựng response field-by-field —
đây không phải thiếu sót, mà là hệ quả trực tiếp của §4: mapping tự động chỉ
rẻ khi mapping là 1-1 thuần tuý (pattern 1); một khi handler phải tính toán
(`Status = PotentialGroupStatuses.Normalize(...)`, `EntityValueLabels = BuildValueLabels(...)`),
"mapper" tự động sẽ phải nhồi logic nghiệp vụ vào cấu hình mapping — sai chỗ.

### 3.2 Command mang 2 marker — thiếu 1 cái là bug im lặng

```csharp
public class CreateSccPotentialGroupCommand
    : CreateCommand<CreateSccPotentialGroupRequest, Guid>,
      ISuccessionCommand, ITransactionalCommand;
```

| Marker | Khai ở | Vai trò |
| --- | --- | --- |
| `ITransactionalCommand` | `Platform.Application` (P2 §3) | "Lệnh này cần transaction" — marker chung, mọi module |
| `ISuccessionCommand` | `Module.Succession.Application.Abstractions` (module tự khai, plain interface không MediatR) | "Lệnh này thuộc module Succession" — cho `SuccessionTransactionBehavior<,>` biết **chỉ** bọc command của module mình |

Comment gốc của `ISuccessionCommand`, trích nguyên văn — đây là cảnh báo quan
trọng nhất của toàn bộ pattern 2:

> *"⚠️ Command có `ITransactionalCommand` mà QUÊN marker này sẽ chạy NGOÀI
> transaction, im lặng."*

Không build error, không log, không exception — command vẫn chạy, vẫn trả kết
quả đúng trong điều kiện bình thường, và chỉ lộ ra khi một bước giữa chừng thất
bại (network timeout, constraint violation ở bước 2 sau khi bước 1 đã
`SaveChanges`) mà đáng lẽ phải rollback cả hai. Đây chính xác là hệ quả đã cảnh
báo ở [03-p2 §6.5](03-p2-platform-application.md) ("COMMIT dữ liệu của lệnh ĐÃ
THẤT BẠI") — P5 là nơi bug đó **thật sự có thể xảy ra** nếu dev quên 1 dòng
`: ISuccessionCommand`.

### 3.3 `SuccessionTransactionBehavior` — behavior per-module implement thật

```csharp
internal sealed class SuccessionTransactionBehavior<TRequest, TResponse>(
    SuccessionUnitOfWork unitOfWork,
    ILogger<SuccessionTransactionBehavior<TRequest, TResponse>> logger)
    : ModuleTransactionBehaviorBase<TRequest, TResponse>(unitOfWork, logger)
    where TRequest : IRequest<TResponse>, ISuccessionCommand, ITransactionalCommand;
```

Đăng ký (`Add{Module}Services`, gọi từ `Add{Module}Infrastructure`):

```csharp
services.AddModuleUnitOfWork<ISuccessionUnitOfWork, SuccessionUnitOfWork>();
services.AddModuleTransactionBehavior(typeof(SuccessionTransactionBehavior<,>));
```

Comment gốc nhắc lại đúng luật đã đặt ở P2 §6.4-6.5 — **không** tự
`AddTransient(typeof(IPipelineBehavior<,>), ...)`:

> *"Transaction per-module: CHỈ khai ở đây, Composition Root flush vào đúng vị
> trí trong pipeline. KHÔNG `AddTransient(IPipelineBehavior<,>)` — sẽ bọc NGOÀI
> `ExceptionHandlingBehavior` và commit cả lệnh đã thất bại."*

`AddModuleTransactionBehavior` (P2, `Platform.Application`) không đăng ký
`IPipelineBehavior<,>` ngay — nó **gom lại** để `RegisterModuleTransactionBehaviors`
(P4 §Composition Root) chèn đúng vị trí thứ 4 trong pipeline (sau Validation,
trước CrudHandler). Module chỉ "khai ý định", Composition Root mới thật sự
quyết định vị trí — đúng nguyên tắc "thứ tự pipeline là thuộc tính của
Composition Root" đã rút ra ở [03-p2 §6.1](03-p2-platform-application.md).

### 3.4 Handler tự đọc DB, tự dựng entity, tự `SaveChangesAsync` — validator không làm 3 việc đó

```csharp
public sealed class CreateSccPotentialGroupHandler(
    IApplicationContext context,
    ISccPotentialGroupService potentialGroupService,
    ISuccessionUnitOfWork unitOfWork)
    : CommandHandler<CreateSccPotentialGroupCommand, Guid>(context)
{
    public override async Task<IApiResult<Guid>> Handle(CreateSccPotentialGroupCommand command, CancellationToken ct)
    {
        var name = command.Request.Name.Trim();

        // 1. Luật CẦN đọc DB (validator không thấy DB) — trùng tên
        if (await potentialGroupService.NameExistsAsync(name, cancellationToken: ct))
            return Fail<Guid>(PotentialGroupErrors.DuplicateName, name);

        // 2. Dựng entity qua DOMAIN FACTORY — không new trực tiếp, không mapper
        var group = Scc_PotentialGroup.Create(
            id: EntityId.New(),          // KHÔNG Guid.NewGuid() — xem P1 §EntityId
            name: name, code: PotentialGroupCodeGenerator.GenerateGroupCode(),
            status: /* ... */ "Active", description: null, minScore: command.Request.MinScore);

        await potentialGroupService.AddAsync(group, ct);
        // ... dựng thêm entity con liên quan (Scc_PotentialGroupCriteria) ...

        // 3. HANDLER tự gọi SaveChangesAsync — behavior KHÔNG gọi thay
        //    (đây là path ITransactionManager.ExecuteInTransactionAsync của P3 §9.1 — behavior chỉ
        //    mở/đóng transaction bao ngoài, chính handler quyết định khi nào save)
        await unitOfWork.SaveChangesAsync(ct);

        return Ok(group.Id);
    }
}
```

**Ranh giới validator vs handler** (đã nêu nguyên tắc ở P2 §6.2, đây là bằng
chứng thật): validator (`CreateSccPotentialGroupValidator`) chặn *"tổng trọng
số phải bằng 100"* — tính được trọn vẹn từ payload, không chạm DB. Handler chặn
*"tên đã tồn tại"* — cần query. Comment gốc của handler nói thẳng ranh giới này:

> *"Validator đã lo toàn bộ validation loại 1 ... trước khi tới đây. Handler chỉ
> còn phần phải chạm DB."*

### 3.5 TOCTOU — kiểm tra ở GET không thay thế kiểm tra lại ở handler

`DeleteSccPotentialGroupHandler` kiểm tra lại "còn nhân viên thuộc nhóm không"
dù đã có sẵn 1 endpoint riêng `GET .../{id}/delete-eligibility` cho UX (hộp
thoại xác nhận xoá hiện sẵn số lượng bị ảnh hưởng). Comment gốc:

> *"Kiểm LẠI dù GET .../delete-eligibility đã kiểm: endpoint đó là trải nghiệm
> người dùng, không phải kiểm soát. Giữa hai lời gọi có một thao tác của người
> dùng (bấm nút trong hộp thoại) — đủ thời gian cho một lần chạy phân nhóm; và
> gọi thẳng bằng curl thì bỏ qua nó hoàn toàn."*

**Nguyên tắc rút ra, áp dụng cho mọi vertical slice:** một query hiển thị
"trạng thái có cho phép hành động X không" cho UI **không bao giờ** là nguồn sự
thật cho quyết định — nó chỉ là gợi ý UX. Command thực thi hành động X phải tự
kiểm tra lại điều kiện đó tại đúng thời điểm ghi, dùng **cùng 1 hàm luật**
(`PotentialGroupDeletionRule.CountAssignedEmployeesAsync`, gọi từ cả 2 nơi) để
2 đường không lệch nhau.

### 3.6 Cascade thủ công — soft-delete không đi qua FK `ON DELETE CASCADE`

```csharp
// DeleteSccPotentialGroupHandler
List<Scc_PotentialGroupCriteria> criteria = await criteriaRepository.FindAllAsync(
    item => item.PotentialGroupId == groupId, cancellationToken);
if (criteria.Count > 0)
    await criteriaRepository.DeleteRangeAsync(criteria, cancellationToken);   // soft-delete TAY

await groupRepository.DeleteAsync(group, cancellationToken);                  // soft-delete nhóm
await unitOfWork.SaveChangesAsync(cancellationToken);
```

Comment gốc giải thích lỗi thật đứng sau đoạn code này:

> *"Cascade phải làm TAY. `Scc_PotentialGroupCriteria.PotentialGroupId` có hard
> FK `ON DELETE CASCADE`, nhưng `DeleteAsync` của Platform là xoá mềm
> (`IsDelete = true`) ⇒ DB chỉ thấy một `UPDATE`, FK cascade không chạy, tiêu
> chí ở lại nguyên vẹn và mồ côi."*

Đây là hệ quả trực tiếp của quyết định "mọi entity implement `ISoftDelete` xoá
mềm, không xoá vật lý" (P1) — `ON DELETE CASCADE` khai ở migration là cơ chế
của **xoá vật lý** (`DELETE FROM`), hoàn toàn không kích hoạt khi code chỉ chạy
`UPDATE ... SET "IsDelete" = true`. **Bất kỳ entity con nào có FK trỏ tới entity
cha soft-delete được, handler xoá entity cha phải tự cascade xoá mềm entity con
— không có cơ chế nào ở P1–P4 làm việc này tự động.** [ĐƠN GIẢN HOÁ] có thể cân
nhắc: viết 1 `DomainEventInterceptor` (P3 §7.3) phát `EntitySoftDeletedEvent`
rồi 1 handler chung xử lý cascade theo cấu hình — nhưng đó là cơ chế tổng quát
hoá, chưa nên viết cho tới khi có ≥3 entity cần cascade giống nhau (nguyên tắc
"khai theo nhu cầu", P0 §1 nguyên tắc #1).

---

## 4. `ErrorDescriptor` trong thực chiến — 4 mã HTTP, 1 bảng quyết định

`PotentialGroupErrors.cs` thật có **hơn 15 `ErrorDescriptor`**. Rút ra thành 1
bảng quyết định — đây là tri thức khó nhất của toàn bộ P5, vì taxonomy không
nằm trong code, nó nằm trong **cách đọc code** (comment `<remarks>` của từng
descriptor):

| `ErrorCode` | HTTP | Dùng khi | Ví dụ thật |
| --- | --- | --- | --- |
| `ValidationError` | 400 | Tính được **trọn vẹn từ payload**, không cần DB. Luôn kèm `Fields` | `TotalWeightInvalid` (tổng weight ≠ 100), `EntityNameRequired` (thiếu field) |
| `NotFound` | 404 | Không tìm thấy **resource chính của route** (`{id}` trên `GET`/`PUT`/`DELETE`) | `PotentialGroupErrors.NotFound` — id trên route không tồn tại hoặc đã xoá mềm |
| `BusinessRuleError` | 422 | FK/tham chiếu **nằm trong payload** (không phải resource chính của route), cần đọc DB để biết hợp lệ | `CriteriaFieldNotFound` — `Criteria[].EntityId` trỏ tới chỉ báo không tồn tại |
| `Conflict` | 409 | Xung đột **trạng thái của chính resource** — trùng giá trị unique, hoặc resource đang bị ràng buộc không cho thao tác | `DuplicateName` (tạo trùng tên), `InUse` (xoá khi còn bị tham chiếu) |

**3 quy tắc phân biệt rút ra từ chính comment gốc, đáng nhớ hơn cả bảng:**

1. **404 dành riêng cho resource chính của route, không dùng cho FK trong
   payload.** `Criteria[].EntityId` sai → 422, không phải 404 — vì `{id}` trên
   route mới là "cái mà request đang thao tác", còn `EntityId` trong payload là
   dữ liệu tham chiếu. Trộn 2 khái niệm này khiến FE không phân biệt được
   "route sai" (cần điều hướng lại) với "payload sai" (cần sửa form).
2. **Xoá mềm và chưa từng tồn tại phải trả cùng 1 lỗi (404), không tách 2 mã.**
   Trích nguyên văn: *"phân biệt hai ca đó sẽ tiết lộ sự tồn tại của dữ liệu
   người dùng không có quyền thấy."* Đây là nguyên tắc bảo mật, không phải tiện
   lợi code.
3. **409 vs 422 phân biệt bằng "xung đột của chính nó" vs "tham chiếu sai
   trong payload".** `InUse` (409) — nhóm này không xoá được vì trạng thái của
   chính nó (đang bị dùng). `CriteriaFieldNotFound` (422) — dữ liệu gửi lên trỏ
   sai, không phải trạng thái của resource đang thao tác có vấn đề.

Đây chính là các quyết định thiết kế đã được nêu tổng quát ở [02-p1
`DomainException`](02-p1-platform-domain.md) và [03-p2 §4.1
`ErrorCode`](03-p2-platform-application.md) — P5 là nơi áp dụng chúng vào 15+
tình huống thật, và bảng trên là kết quả rút gọn của 15 tình huống đó.

---

## 5. Mapper — 1 dòng quyết định dùng cái nào

| | AutoMapper | Riok.Mapperly |
| --- | --- | --- |
| Cơ chế | Reflection, runtime `CreateMap` | Source generator, compile-time |
| Dùng ở | `CatalogAutoMapperProfile` — cặp kiểu chỉ biết lúc chạy (qua `CrudEntityRegistry`, nạp bởi N lời gọi `AddCatalogCrud<>()` rải khắp N module) | Mapper khai tường minh khi cặp kiểu đã biết lúc viết code — vd `ApprovalWorkflowMapper` (module Succession) |
| Đăng ký | `TryAddSingleton`/`AddAutoMapper` quét profile 1 lần | `services.AddSingleton<TMapper>()` — **tường minh**, KHÔNG lọt vào convention scan (`[Mapper]` là attribute compile-time-only, scanner theo `IScopedService`/DbSet không thấy) |
| Pattern 2 (vertical slice) có cần mapper không? | Không bắt buộc — phần lớn handler dựng response **tay** (xem `GetSccPotentialGroupByIdHandler` §3.1) vì response cần logic (chuẩn hoá, gắn nhãn), không phải copy field 1-1 | Chỉ dùng Mapperly khi thật sự có mapping 1-1 lặp lại nhiều nơi trong cùng module (vd `ApprovalWorkflowMapper` dùng ở cả Create/Update/GetById của `ApprovalWorkflow`) |

**Quy tắc chọn:** cặp kiểu chưa biết lúc compile (dynamic/registry-driven) →
AutoMapper. Cặp kiểu cố định + cần zero-reflection + logic mapping đơn giản đủ
để generator viết được → Mapperly. Mapping có **bất kỳ logic tính toán nào**
(chuẩn hoá, gắn nhãn, gộp trường) → không dùng mapper nào cả, viết tay trong
handler — cả 2 thư viện đều không phải chỗ đúng để giấu business logic.

---

## 6. Đăng ký DI — nơi mọi thứ ráp lại

```csharp
// {Module}InfrastructureExtensions.cs — điểm vào duy nhất của module
public static IServiceCollection AddSuccessionInfrastructure(this IServiceCollection services, IConfiguration configuration)
{
    services.AddSuccessionDbContext(configuration);              // P3 — AddModuleDatabase<SuccessionDbContext>
    services.AddSuccessionServices();                             // §6.1 dưới đây
    services.AddModuleRepositories<SuccessionDbContext>();        // P3 — GenericRepository theo từng DbSet
    services.AddModuleConventions<SuccessionModule>();            // scan IScopedService, IEntitySearchConfig, DapperGridBase
    return services;
}

private static IServiceCollection AddSuccessionServices(this IServiceCollection services)
{
    services.AddModuleUnitOfWork<ISuccessionUnitOfWork, SuccessionUnitOfWork>();   // §3.3
    services.AddModuleTransactionBehavior(typeof(SuccessionTransactionBehavior<,>)); // §3.3
    services.AddSingleton<ApprovalWorkflowMapper>();               // §5 — Mapperly khai tường minh
    // ... AddCatalogCrud<>() nếu module có entity catalog xen giữa entity nghiệp vụ ...
    return services;
}
```

Ở tầng Composition Root (`BaseStartupServices.AddCoreInfrastructure`, P4 §3),
`RegisterMediatR(assemblies)` (auto-scan `IRequestHandler<,>` — bắt mọi handler
pattern 2) chạy **trước** `RegisterPlatformValidators([.. assemblies])`
(auto-scan `AbstractValidator<>` — bắt mọi validator pattern 2, nhưng **không**
bắt được validator generic của pattern 1 vì nó cần closed-generic tường minh,
đã đăng ký riêng trong `AddSharedCatalogValidators` ở §2.4). Không có bước nào
trong 2 lần scan này cần dev khai tay 1 dòng `services.AddScoped<IRequestHandler<...>, ...>()`
cho handler pattern 2 — viết đúng file đúng namespace là đủ để MediatR tìm
thấy.

---

## 7. Cấm ở P5

| Cấm | Vì sao | Bằng chứng/ArchTest |
| --- | --- | --- |
| Command mang `ITransactionalCommand` mà thiếu marker module (`ISuccessionCommand` hoặc tương đương) | Chạy ngoài transaction, im lặng — xem §3.2 | `T_MOD_TransactionMarkerPaired` — quét command implement `ITransactionalCommand` mà không implement marker module tương ứng |
| Generic base handler tầng giữa (`CreateBusinessHandler<TEntity,TRequest,TResponse,TKey>`) để "đỡ" cho entity nghiệp vụ đơn giản | Đúng nguyên tắc bị cấm ở [00-lộ-trình §1 nguyên tắc #4](00-lo-trinh-tong-the.md) — entity nghiệp vụ dù đơn giản đến đâu vẫn phải qua vertical slice tường minh, hoặc thật sự đơn giản thì nó là catalog, dùng pattern 1 | Review thủ công |
| Handler pattern 2 gọi `AutoMapper`/`IMapper` để né viết field-by-field khi response cần logic tính toán | Giấu business logic trong cấu hình mapping — không ai grep ra được luật nằm ở đâu | Review thủ công |
| Xoá cha (soft-delete) mà không cascade xoá mềm con có FK trỏ tới nó | Con mồ côi, vô hình trên UI, chỉ lộ khi soi DB trực tiếp — xem §3.6 | Test tích hợp: xoá cha → assert con cũng `IsDelete = true` |
| Query "kiểm tra điều kiện cho UX" (`delete-eligibility`, v.v.) được xem là đủ để command bỏ qua kiểm tra lại | TOCTOU — xem §3.5 | Review thủ công + code review checklist |
| `AddCatalogCrud<>()` gọi **sau** `AddAutoMapper` trong Composition Root | Profile build ra 0 mapping — xem §2.5 | Thứ tự cố định trong `Add{Module}Infrastructure`, review thủ công |

---

## 8. Checklist rời P5

- [ ] 1 entity catalog: đăng ký `AddCatalogCrud<>()`, chạy đủ CRUD qua HTTP, 0 dòng handler
- [ ] `CrudUniqueValidator`/`CrudFkValidator` kiểm chứng bằng test thật: tạo trùng Code → lỗi đúng; xoá khi còn bị tham chiếu → lỗi đúng
- [ ] 1 entity nghiệp vụ: đủ cây file vertical slice (Command/Request/Handler/Validator mỗi thao tác, `{Entity}Errors.cs` tập trung)
- [ ] Command nghiệp vụ có transaction mang **cả 2** marker (`ITransactionalCommand` + marker module) — kiểm chứng bằng test cố tình thiếu 1 marker, assert chạy ngoài transaction (giống tinh thần P0 §7: luật chưa từng đỏ là luật chưa được chứng minh)
- [ ] Validator chỉ chứa luật "loại 1" (tính từ payload); mọi luật cần đọc DB nằm ở handler
- [ ] `{Entity}Errors.cs` — mỗi `ErrorDescriptor` chọn đúng `ErrorCode` theo bảng quyết định §4 (không mặc định `BusinessRuleError` cho mọi thứ)
- [ ] Handler tự gọi `unitOfWork.SaveChangesAsync()` — không nhờ behavior save hộ
- [ ] Nếu entity nghiệp vụ có con phụ thuộc: cascade soft-delete đã viết tay + có test
- [ ] Nếu có query "gợi ý UX" riêng (kiểu `delete-eligibility`): command tương ứng tự kiểm tra lại cùng luật, không tin kết quả của query đó
- [ ] Mapper (nếu có) chọn đúng theo bảng §5 — không AutoMapper cho mapping có logic, không Mapperly cho cặp kiểu chưa biết lúc compile

---

**Tiếp theo:** [07-p6-archtests-gate.md](07-p6-archtests-gate.md) — 36 ArchTest
thật của Successor: cái nào phải có từ P0 (đã nêu 2 test ở
[01-p0 §7](01-p0-nen-mong-solution.md)), cái nào chỉ cần khi module thứ 2 xuất
hiện (luật cross-module), và cái nào là "gate mở rộng" chỉ bật khi hệ thống đã
đủ lớn để luật đó có ý nghĩa — cùng cách đọc 1 ArchTest thật để viết ArchTest
tương đương cho hệ thống mới.

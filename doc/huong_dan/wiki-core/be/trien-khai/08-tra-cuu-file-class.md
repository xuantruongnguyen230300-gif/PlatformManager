# Tra cứu file / class / interface

> 📍 **Tên project trong file này là của VNR.Successor, không phải PlatformManager.**
> Tra bảng ánh xạ + 4 mục "KHÔNG áp dụng" ở [`00-lo-trinh-tong-the.md`](00-lo-trinh-tong-the.md)
> §ĐỌC TRƯỚC. Tóm tắt: `Platform.*`→`Core.*` · `Module.{M}.*`→tầng nghiệp vụ (`Business.*`) ·
> `Processes/`→**1** host · JWT→**cookie session** · per-module DbContext→**1** DbContext chung.

> Mục lục ngược của toàn bộ `be/trien-khai/00→07`: tra theo **tên** để biết nó
> thuộc layer nào, phase nào tạo ra, làm gì trong 1 câu, và file nào có phân
> tích đầy đủ. Dùng khi đã đọc qua 1 lần và chỉ cần nhắc lại nhanh — không thay
> thế việc đọc file gốc lần đầu.
>
> Quy ước cột **Phase**: P0–P6 theo [00-lộ-trình](00-lo-trinh-tong-the.md).
> Quy ước cột **Nguồn**: mọi tên trong bảng này có thật ở
> `D:\Successor\VNR.Successor\src\backend\`, trừ dòng đánh dấu `[MỚI]` (khái
> niệm chỉ xuất hiện trong bộ tài liệu này, không có ở Successor).

---

## 1. `Platform.Domain` (P1)

| Tên | Loại | Namespace | Làm gì (1 câu) |
| --- | --- | --- | --- |
| `BaseEntity<TId>` | class | `Entities` | Gốc mọi entity — `Id`, 4 field audit, `IsDelete`, đều `get; set;` |
| `BaseEntity` | class | `Entities` | Shortcut `BaseEntity<Guid>` |
| `AggregateRoot<TId>` | class | `Entities` | `BaseEntity` + `IHasDomainEvents` — field nghiệp vụ phải `private set` |
| `CatalogEntityBase<TId>` | class | `Entities` | `BaseEntity` + `Code/Name/Description/IsActive` **public set** — cực đối lập với `AggregateRoot` |
| `EntityId.New()` | static method | `Entities` | Điểm sinh `Guid` duy nhất toàn hệ thống — không ai gọi `Guid.NewGuid()` trực tiếp |
| `IHasId<TKey>` | interface | `Entities.Interfaces` | `TKey Id { get; }` — cho `GenericRepository` ràng buộc type-safe |
| `IAuditEntity` | interface | `Entities.Interfaces` | `UserCreate/UserUpdate/DateCreate/DateUpdate`, `get; set;` — `AuditInterceptor` (P3) ghi vào |
| `ISoftDelete` | interface | `Entities.Interfaces` | `bool IsDelete { get; set; }` — global query filter (P3) lọc theo đây |
| `ICatalogEntity` | interface | `Entities.Interfaces` | `Code/Name/Description` — bộ ba của bảng danh mục |
| `IActiveStatus` | interface | `Entities.Interfaces` | `bool IsActive { get; set; }` |
| `IOptimisticConcurrency` | interface marker rỗng | `Entities.Interfaces` | Entity cần concurrency token — `ConcurrencyTokenConvention` (P3) tự gắn shadow property theo provider |
| `IChangeTracked` | interface marker rỗng | `Entities.Interfaces` | Entity cần audit trail chi tiết old/new value — `ChangeLogInterceptor` (P3, ⏳) đọc |
| `IHasId`/`IAuditEntity`/... | — | — | Xem đủ 5 interface P1 bắt buộc ở [02-p1 §4](02-p1-platform-domain.md) |
| `DomainException` | exception | `Exceptions` | Vi phạm invariant — có `Code` (không phải HTTP status) → HTTP 422 qua `ExceptionHandlingBehavior` (P2) |
| `DomainErrorCodes` | static class | `Exceptions` | Hằng tập trung mọi mã lỗi Domain, prefix `VO.*` |
| `BusinessException` | exception | `Exceptions` | Quy tắc nghiệp vụ ở tầng use-case → 400/422 |
| `ConflictException` | exception | `Exceptions` | Trùng khoá/xung đột — **dịch từ `DbUpdateException`** ở `BaseDbContext` (P3) → HTTP 409 |
| `ValueObject` | abstract class | `ValueObjects` | Base — chỉ cần override `GetEqualityComponents()` để có value equality |
| `ValueObject<TValue>` | class | `ValueObjects` | Biến thể "enum kiểu VO" — dùng khi cần logic đi kèm tập giá trị cố định |
| `EmailAddress`/`Money`/`RangeDate`/... | class | `ValueObjects` | Mẫu VO thật — private parameterless ctor, chuẩn hoá trong ctor, throw `DomainException` |
| `Enumeration<TEnum>` | abstract class (CRTP) | `Enumerations` | Thay `enum` C# — lưu `Name` (string) xuống DB, có `ResourceKey`/`Text`/`IsComputed` |
| `IEnumeration` | interface | `Enumerations` | Bản không-generic của `Enumeration<TEnum>` — cho reflection/serialize |
| `TryFromName`/`TryFromId` vs `FromName`/`FromId` | static method | `Enumerations` | **Input từ ngoài → luôn `TryFrom*`** — `From*` throw `InvalidOperationException` → 500 nếu lộ ra API |
| `IGenericRepository<TEntity,TKey>` | interface | `Repositories` | Mọi method trả `List<T>`/`T` — **không bao giờ `IQueryable`** (đây là cơ chế khiến Application không biết EF Core) |
| `IUnitOfWork` | interface | `Repositories` | `SaveChangesAsync` + 2 overload `ExecuteInTransactionAsync` (bọc EF execution strategy) |
| `ILegacyQuerySupport<TEntity,TKey>` | interface `[Obsolete]` | `Repositories` | Leak `IQueryable` — chỉ cho code migrate từ hệ cũ |
| `IDomainEvent` | interface rỗng | `Events` | **Cố ý không kế thừa `MediatR.INotification`** — giữ Domain zero-dependency |
| `DomainEventBase` | abstract class | `Events` | `IDomainEvent` + `OccurredOn` |
| `IHasDomainEvents` | interface | `Events` | `DomainEvents`/`AddDomainEvent`/`ClearDomainEvents` |
| `RequirePermissionAttribute` | attribute | `Security` | Pure metadata (3 ctor: auto-CRUD/explicit/multi-key OR) — enforcement thật nằm ở `Hosting.Api` (P4 §7) |
| `PermissionAction` | enum | `Enums` | View/Detail/Create/Modify/Delete/Export — dùng bởi `RequirePermissionAttribute` + `CrudActionResolver` |

Chi tiết đầy đủ: [02-p1-platform-domain.md](02-p1-platform-domain.md).

---

## 2. `Platform.Application` (P2)

| Tên | Loại | Namespace | Làm gì (1 câu) |
| --- | --- | --- | --- |
| `IPlatformRequest` | interface marker | `CQRS` | Chặn behavior bọc nhầm `IRequest` bên thứ ba/legacy |
| `ITransactionalCommand` | interface marker | `CQRS` | **Opt-in** — command cần transaction (mặc định KHÔNG có transaction) |
| `ITransactionManager` | interface | `CQRS` | `BeginAsync/CommitAsync/RollbackAsync/ExecuteInTransactionAsync` — implement bởi `UnitOfWork<TDbContext>` (P3) |
| `ErrorCode` | enum | `Results` | **Giá trị enum = HTTP status** (400/401/403/404/409/422/500), on-wire = tên member (string) |
| `ApiResultStatus` | enum | `Results` | Status bucket cho FE — derive từ `ErrorCode` qua `ApiErrorIds.StatusForCode`, không set tay |
| `ApiErrorIds.StatusForCode` | static method | `Results` | Nguồn DUY NHẤT map `ErrorCode → ApiResultStatus` |
| `ErrorDescriptor` | record | `Results` | `BusinessCode + ErrorCode + MessageTemplate(+i18n)` — nguồn cho cả `Fail<T>()` lẫn `ExceptionHandlingBehavior` |
| `FieldError` | record | `Results` | `Field/Code/Message/Args` — `Message` KHÔNG kèm tên field, `Field` PascalCase |
| `IApiResult<T>` / `IApiResult` | interface | `Results` | Envelope chuẩn — `Data/Message/Status/Code/BusinessCode/TraceId/Retryable/LogId/Fields` |
| `IHasApiResultStatus` | interface | `Results` | `Status`/`Code` — `BaseApiController` dùng để nhận diện response cần map HTTP |
| `IApiResultEnrichable` | interface | `Results` | `TraceId` settable — `EnrichResponse` (P4) cast thẳng, không reflection (hot path) |
| `ApiResult<T>` | class | `Results` | Implement `IApiResult<T>` — dual-serializer (`[JsonIgnore(WhenWritingNull)]` STJ + `ShouldSerialize*()` Newtonsoft) |
| `BaseResponse` | abstract class | `CQRS` | `Ok(T)`/`Fail<T>(ErrorDescriptor, args)` — mặt duy nhất handler chạm tới |
| `ICommand<TResult>` / `IQuery<TResult>` | interface | `CQRS.Commands`/`Queries` | `: IRequest<IApiResult<TResult>>` |
| `CreateCommand<TRequest,TResult>` / `UpdateCommand<TKey,TRequest,TResult>` / `DeleteCommand<TKey,TResult>` / `DeleteRangeCommand<TKey,TResult>` | class | `CQRS.Commands` | Command generic dùng chung — nền của **pattern 1 zero-handler** (P5 §2) |
| `Query<TKey,TResult>` / `QueryList<TResult>` / `QueryListGrid<TResult>` | class | `CQRS.Queries` | Query generic tương ứng |
| `IQueryListGrid` vs `IQueryListGridLegacy` | interface | `CQRS.Queries` | 2 hình dạng envelope grid cùng tồn tại — bằng chứng "envelope drift", ưu tiên `IApiResult<PagedResult<T>>` cho hệ thống mới |
| `CommandHandler<TCommand,TResult>` | abstract class | `CQRS.Commands` | Base handler — `IRequestHandler<TCommand, IApiResult<TResult>>`, có `Context: IApplicationContext?` |
| `QueryHandler<TQuery,TResult>` | abstract class | `CQRS.Queries` | Tương tự cho query |
| `PagedResult<T>` | class | `Contracts.Grid` | `TotalRow=-1` là sentinel "chưa đếm" — `Ceiling(-1/pageSize)=0` gây pager-collapse nếu không cẩn thận |
| `ValidationBehavior<TRequest,TResponse>` | pipeline behavior | `Behaviors` | Vị trí 3 — FluentValidation, trả 400 + `Fields` trước khi vào handler |
| `ExceptionHandlingBehavior<TRequest,TResponse>` | pipeline behavior | `Behaviors` | Vị trí 1 (outermost) — catch-ladder map exception → `ErrorCode`, `NormalizeField` PascalCase |
| `ModuleTransactionBehaviorBase<TRequest,TResponse>` | abstract pipeline behavior | `Behaviors` | Vị trí 4 — mỗi module kế thừa cho DbContext riêng (ADR-014), bọc `ITransactionManager.ExecuteInTransactionAsync` (KHÔNG tự save) |
| `TransactionRollbackSignalException` | exception | `Behaviors` | Tín hiệu nội bộ để behavior biết cần rollback dù handler không throw |
| `CrudHandlerBehavior<TRequest,TResponse>` | pipeline behavior | `Crud` | Vị trí 5 — fallback khi KHÔNG có handler tường minh đăng ký (lõi pattern 1, xem P5 §2.1) |
| `LoggingBehavior<TRequest,TResponse>` | pipeline behavior | `Behaviors` | Vị trí 2 — `[LoggerMessage]`, không log payload PII, rethrow |
| `.WithError(...)` | extension method | `Validation` | Cầu nối FluentValidation rule → `ErrorDescriptor` |
| `IApplicationContext` | interface | `Context` | Đúng 3 member: `CurrentUser/Cache/Translation` — KHÔNG khái niệm HTTP |
| `BaseDto` | class | `Contracts` | Base cho mọi Response DTO |
| `CrudEntityRegistry` | static class | `Crud` | Registry `DTO Type ↔ Entity Type`, nạp bởi `AddCatalogCrud<>()` — nền tảng resolve của pattern 1 |
| `CrudEntityAttribute` | attribute | `Crud` | `[CrudEntity(typeof(...))]` — ưu tiên thấp hơn registry, cao hơn convention tên |
| `CrudTypeResolver` | static class | `Crud` | Resolve `(EntityType, KeyType)` từ DTO — Registry trước, attribute sau |
| `ICrudRepository<TEntity,TKey>` | interface | `Crud` | `IGenericRepository` + `SaveChangesAsync` — CHỈ `CrudHandler` (Platform nội bộ) được inject |
| `ILegacyQuerySupport<TEntity,TKey>` | interface `[Obsolete]` | (P1, impl ở P3) | Leak `IQueryable` — chỉ cho migrate |
| `CatalogCrudRegistrationExtensions.AddCatalogCrud<TEntity,TRequest,TResponse>()` | extension method | `Crud` | **1 dòng** đăng ký đủ: registry + AutoMapper mapping + validator generic |
| `CatalogAutoMapperProfile` | AutoMapper `Profile` | `Contracts.Catalog` | Tự sinh `CreateMap` cho mọi catalog đã đăng ký — PHẢI scan SAU khi mọi `AddCatalogCrud` đã chạy |
| `CrudUniqueValidator` / `CrudFkValidator` | class | `Crud` | Luật generic dùng chung: trùng field unique lúc tạo/sửa, còn bị tham chiếu lúc xoá |
| `CrudActionResolver.Resolve(methodName, httpMethod)` | static method | `Security` (Application) | Suy `PermissionAction` từ tên method — **cố ý không fallback theo HTTP verb** |

Chi tiết đầy đủ: [03-p2-platform-application.md](03-p2-platform-application.md).

---

## 3. `Platform.Persistence` (P3)

| Tên | Loại | Namespace | Làm gì (1 câu) |
| --- | --- | --- | --- |
| `BaseDbContext` | abstract class | `Context` | `SchemaName` abstract, override `ConfigureConventions`/`OnModelCreating`, bắt `DbUpdateException` (SQLSTATE `23505`) → `ConflictException` |
| `PlatformConventions.ApplyAllConventions` | static method | `Context` | 9 convention theo thứ tự cố định — soft-delete filter, table naming, decimal/string precision, collation, FK index, `NoAction` cascade, UTC DateTime, **`ValueGeneratedNever`**, unaccent |
| `ApplyKeyGenerationConvention` | convention | `Context` | Guid PK → `ValueGenerated.Never` — né bug EF coi key-đã-set = "đã tồn tại" khi thêm child vào aggregate đang tracked |
| `FilteredSoftDeleteIndexConvention` | `IModelFinalizingConvention` | `Context` | Mọi unique index trên entity có `IsDelete` → tự thêm `WHERE IsDelete = false` |
| `ConcurrencyTokenConvention` | `IModelFinalizingConvention` | `Context` | Entity `IOptimisticConcurrency` → shadow `xmin` (Postgres) / `RowVersion` (SQL Server) |
| `IBoundedContext` | interface | `Context` | `ContextName`/`SchemaName` — DIP Seam cho phép enumerate mọi DbContext qua `IEnumerable<IBoundedContext>` mà không reference Module.*.Infrastructure |
| `SoftDeleteExtensions.ApplySoftDeleteFilter` | extension method | `Extensions` | Global query filter `WHERE IsDelete = false` — khác với `FilteredSoftDeleteIndexConvention` (index, không phải query) |
| `EntityIdGenerationInterceptor` | `SaveChangesInterceptor` | `Interceptors` | Lưới an toàn — tự điền `EntityId.New()` nếu entity `Added` mà `Id == Guid.Empty` |
| `AuditInterceptor` | `SaveChangesInterceptor` | `Interceptors` | Set `DateCreate/UserCreate/DateUpdate/UserUpdate` — singleton, resolve `ICurrentUser` scoped qua `CreateScope()` mỗi lần |
| `DomainEventInterceptor` | `SaveChangesInterceptor` | `Interceptors` | Dispatch domain event **sau** `SaveChangesAsync` thành công — chỉ override async path (né deadlock) |
| `ChangeLogInterceptor` | `SaveChangesInterceptor` | `Interceptors` | ⏳ Audit trail old/new value cho `IChangeTracked` — fire-and-forget qua `IAuditLogService` |
| `CatalogCacheInvalidationInterceptor` | `SaveChangesInterceptor` | `Interceptors` | ⏳ Bump cache version key khi `ICatalogEntity` đổi — cần `ICacheService` (chưa có ở P3) |
| `PostgresAccentInterceptor` | `DbCommandInterceptor` | `Interceptors` | ⏳ Rewrite `LIKE` → `f_unaccent() ILIKE` cho search tiếng Việt |
| `GenericRepository<TEntity,TKey,TDbContext>` | class | `Repositories` | Implement `ICrudRepository` + `ILegacyQuerySupport` — `Add/Update/Delete` **không gọi** `SaveChangesAsync` |
| `UnitOfWork<TDbContext>` | class | `UnitOfWork` | Implement CẢ `IUnitOfWork` + `ITransactionManager` — 2 bản `ExecuteInTransactionAsync` khác hành vi save (P3 §9.1) |
| `SchemaNames` | static class | (root) | Hằng tên schema — single source of truth |
| `BaseDesignTimeFactory<TContext>` | abstract class | `DesignTime` | `IDesignTimeDbContextFactory` — đọc connection string từ `Config/Common/Connections.json` cho `dotnet ef migrations` |
| `AddModuleDbContext<TDbContext>` | extension method | `DependencyInjection` | Đăng ký DbContext + auto-wire interceptor theo thứ tự cố định (fill-Id → audit → changelog → cache-invalidation) |
| `AddModuleRepositories<TDbContext>` | extension method | `DependencyInjection` | Scan `DbSet<T>` → đăng ký closed-generic `IGenericRepository`/`ICrudRepository` cho từng entity |
| `AddModuleUnitOfWork<TInterface,TImpl>` | extension method | `DependencyInjection` | Đăng ký concrete (self, để transaction behavior inject) + interface (cùng instance) |

Chi tiết đầy đủ: [04-p3-platform-persistence.md](04-p3-platform-persistence.md).

---

## 4. `Hosting.Api` + `Hosting.CompositionRoot` (P4)

| Tên | Loại | Namespace | Làm gì (1 câu) |
| --- | --- | --- | --- |
| `BaseApiController` *(VNR dùng JwtBearer; PlatformManager dùng cookie — xem 00-lo-trinh §Auth)* | abstract class | `Controllers` (Hosting.Api) | `[Authorize(JwtBearer)]` + `HandleRequest<T>()` — dispatcher mỏng, map `ErrorCode → HTTP` |
| `BaseApiController.MapToHttpStatusCode` | private static method | — | `code == Success ? 200 : (int)code` — **nguồn duy nhất** map HTTP, không bảng tra riêng |
| `BaseApiController.EnrichResponse` | private method | — | Cast `IApiResultEnrichable`, set `TraceId` — không reflection (hot path) |
| `BaseCrudApiController<TResult,TCreateRequest,TUpdateRequest,TKey>` | abstract class | `Controllers` | 8 endpoint CRUD chuẩn — controller con **0 dòng logic** |
| `IApiContext` / `ApiContext` | interface/class | `Controllers` | Bọc `IMediator` — controller không `new MediatR` trực tiếp |
| `ApplicationContext` | class (`internal`) | `Controllers` | Implement `IApplicationContext` (P2) — ráp `ICurrentUser+ICacheService+ITranslationService` |
| `VnrExceptionHandler` | `IExceptionHandler` | `Middleware` | Lưới an toàn NGOÀI MediatR pipeline — bắt exception từ middleware/filter/model-binding, luôn trả 500 |
| `DefaultRouteConvention` | `IControllerModelConvention` | `Middleware` | Fallback route `api/[controller]/[action]` cho controller quên khai `[Route]` |
| `RequirePermissionFilter` | `IAsyncAuthorizationFilter` | `Authorization` | **Cơ chế enforcement thật** cho `[RequirePermission]` — method-level ghi đè hoàn toàn class-level |
| `PermissionPolicyProvider` + `PermissionAuthorizationHandler` + `PermissionRequirement` | class | `Authorization` | Đường phụ — cho `[Authorize(Policy="Permission:{key}:{action}")]` khai tay (compile-time key) |
| `VnrAuthorizationExtensions.AddVnrPermissionAuthorization()` | extension method | `Authorization` | 1 dòng đăng ký cả 2 cơ chế trên |
| `AuditLogBehavior<TRequest,TResponse>` | pipeline behavior | `Behaviors` (Hosting.CompositionRoot) | Vị trí 6 (innermost) — ghi audit log cho Create/Update/Delete, cần `IHttpContextAccessor` nên đặt ở Hosting, không Platform |
| `BaseStartupServices.AddCoreInfrastructure` | static method | (Hosting.CompositionRoot) | **Composition Root thật** — nơi thứ tự 6 pipeline behavior được thi hành (registration order = pipeline order) |

Chi tiết đầy đủ: [05-p4-hosting-api.md](05-p4-hosting-api.md).

---

## 5. Module đầu tiên — mẫu thật (P5)

| Tên | Loại | Module ví dụ | Làm gì (1 câu) |
| --- | --- | --- | --- |
| `Cat_Province` + `ProvinceRequest`/`ProvinceResponse` + `ProvinceController` | entity + DTO + controller | ReferenceData | **Pattern 1 hoàn chỉnh** — 0 dòng handler, đăng ký qua `AddCatalogCrud<Cat_Province, ProvinceRequest, ProvinceResponse>()` |
| `Scc_PotentialGroup` + `CreateSccPotentialGroupCommand/Handler/Validator` + `PotentialGroupErrors` | entity + vertical slice | Succession | **Pattern 2 hoàn chỉnh** — command mang `ITransactionalCommand` + `ISuccessionCommand`, handler tự đọc DB + tự `SaveChangesAsync` |
| `ISuccessionCommand` | interface marker (module tự khai) | Succession | Cho `SuccessionTransactionBehavior` biết command nào thuộc module mình — **thiếu marker = chạy ngoài transaction, im lặng** |
| `SuccessionTransactionBehavior<TRequest,TResponse>` | `: ModuleTransactionBehaviorBase<,>` | Succession | Behavior transaction cụ thể của module — đăng ký qua `AddModuleTransactionBehavior(typeof(...))`, KHÔNG tự `AddTransient(IPipelineBehavior<,>)` |
| `DeleteSccPotentialGroupHandler` | handler | Succession | Mẫu **cascade soft-delete thủ công** (FK `ON DELETE CASCADE` không chạy khi chỉ có `UPDATE IsDelete=true`) + **TOCTOU re-check** |
| `AutoMapper` (`CatalogAutoMapperProfile`) vs `Riok.Mapperly` (`ApprovalWorkflowMapper`) | 2 thư viện mapper | ReferenceData / Succession | AutoMapper cho cặp kiểu chỉ biết lúc runtime (registry-driven); Mapperly cho cặp kiểu cố định biết lúc compile |

Chi tiết đầy đủ: [06-p5-module-dau-tien.md](06-p5-module-dau-tien.md).

---

## 6. ArchTests thật (P6) — tra theo mã `T_xxx`

| Mã | File | Canh gác |
| --- | --- | --- |
| `T_DOMAIN_01–03` | `DomainPurityTests` | Entity nghiệp vụ private-set; Domain không EF attribute; Domain không ref EF Core |
| `T_CONFIG_01` | `ConfigAccessArchTests` | Application/Domain không inject `IConfiguration` trực tiếp |
| `T_EVENT_01` | `EventSeamArchTests` | Business layer không `using MediatR;` (seam **integration event**, khác CQRS nội bộ) |
| `T_NAMING_01–03` | `NamingConventionTests` | `*Handler` kế thừa `CommandHandler`/`QueryHandler`; `*Validator` kế thừa `AbstractValidator`; `*Command` không abstract |
| `T_PERM_01–03` | `PermissionEnforcementTests` | Controller có `[RequirePermission]` class-level; cấm `[CheckAccess]`; cấm `[AllowAnonymous]` |
| `T_STRING_01` | `MagicStringArchTests` | `[RequirePermission]` dùng hằng số, không literal |
| `T_MASSASSIGN_01` | `MassAssignmentArchTests` | Request DTO không lộ `Id`/`IsAdmin`/audit field |
| `T_DI_01` | `DiRegistrationCompletenessTests` | Mọi service-interface được consume phải có đăng ký DI thật |
| `T091` | `BoundedContextArchTests` | `Infrastructure.A` không reference `Infrastructure.B` |
| `T_MOD_01–02` | `ModuleInstallerArchTests` | Mỗi `Module.*.Infrastructure` có đúng 1 `IModuleInstaller` (⏳ chỉ khi áp dụng pattern declarative install) |
| `T_IDX_001` | `IndexArchTests` | Mọi FK có index (lưới kiểm chứng `ApplyForeignKeyIndexConvention` không bị bypass) |
| `T_VO_001` | `OwnedTypeInlineArchTests` | `OwnsOne` Value Object table-split inline |
| `T_GRID_01` | `DapperGridRegistrationTests` | Mọi `DapperGridBase<,>` được phủ đăng ký `IGridQuery<,>` (⏳ P6) |
| `T_SEARCH_01` | `SearchArchTests` | Handler không tự inject `ISearchProvider` |
| `T_ENUM_001+` | `EnumerationArchTests` | Smart Enum member ↔ i18n parity |
| `T_ENUM_USE_01` | `SmartEnumHandlerUsageTests` | Handler không gọi `FromName`/`FromId` trực tiếp (phải `TryFrom*`) |
| `T_OPTION_01–02` | `OptionDtoArchTests` | `*OptionDto` kế thừa `SelectOptionDto`, là `record` |
| `T_SUPPRESS_01` | `SuppressionBudgetArchTests` | Tổng suppress Sonar trong `Src/` không vượt baseline |

Bảng đầy đủ (34 file, kèm cột "khi nào cần"): [07-p6-archtests-gate.md §2](07-p6-archtests-gate.md).

---

## 7. Tra theo tình huống — "tôi đang gặp vấn đề X, đọc đâu?"

| Tình huống | Đọc file:mục |
| --- | --- |
| Handler trả `IApiResult<T>` nhưng FE nói lúc thì `Code` là số lúc thì là chữ | [03-p2 §4.1](03-p2-platform-application.md) `ErrorCode` — value=HTTP status, on-wire=string |
| 2 lỗi giống nhau nhưng trả `Status` khác nhau tuỳ đường đi | [03-p2 §4.3](03-p2-platform-application.md) `ApiErrorIds.StatusForCode` — nguồn duy nhất |
| Muốn biết dùng 400 hay 404 hay 409 hay 422 cho 1 lỗi cụ thể | [06-p5 §4](06-p5-module-dau-tien.md) bảng quyết định 4 mã HTTP, rút từ 15+ `ErrorDescriptor` thật |
| Transaction "commit dữ liệu của lệnh đã thất bại" | [03-p2 §6.5](03-p2-platform-application.md) + [06-p5 §3.2](06-p5-module-dau-tien.md) (2 marker bắt buộc) |
| `DbUpdateConcurrencyException` khi thêm child vào aggregate đã `.Include()` | [04-p3 §4.1](04-p3-platform-persistence.md) `ApplyKeyGenerationConvention` |
| Xoá mềm entity cha xong, entity con vẫn còn (mồ côi) | [06-p5 §3.6](06-p5-module-dau-tien.md) cascade soft-delete phải làm tay |
| Insert trùng khoá dù đã soft-delete dòng cũ | [04-p3 §5](04-p3-platform-persistence.md) 2 lớp soft-delete (query filter vs index filter) |
| `NpgsqlRetryingExecutionStrategy` throw "does not support user-initiated transactions" | [04-p3 §9.2](04-p3-platform-persistence.md) `CreateExecutionStrategy()` bắt buộc |
| Muốn biết khi nào viết handler tay, khi nào để generic tự lo | [06-p5 §1](06-p5-module-dau-tien.md) ranh giới pattern 1 vs pattern 2 |
| `AutoMapperMappingException` ở request đầu tiên sau khi thêm `AddCatalogCrud` mới | [06-p5 §2.5](06-p5-module-dau-tien.md) thứ tự gọi `AddCatalogCrud` trước `AddAutoMapper` |
| Grid trả shape khác endpoint thường, FE phải parse 2 kiểu | [03-p2 §5](03-p2-platform-application.md) `IQueryListGrid` vs Legacy — envelope drift |
| Muốn biết vì sao không dùng `NetArchTest.Rules` | [07-p6 §1](07-p6-archtests-gate.md) |
| Muốn tạm loại 1 module khỏi 1 ArchTest vì đang có nợ kỹ thuật đã biết | [07-p6 §1.3](07-p6-archtests-gate.md) — loại trừ qua `ProjectReference`, không `[Skip]` |
| Permission check không chạy dù đã gắn `[RequirePermission]` | [05-p4 §7.2](05-p4-hosting-api.md) `CrudActionResolver` trả `null` cho method không khớp `MethodMap` |
| Handler `FromName()` throw 500 khi FE gửi giá trị lạ | [02-p1 §8](02-p1-platform-domain.md) luật `TryFrom*` cho input ngoài |

---

Đây là file cuối của series `be/trien-khai/`. Quay lại
[00-lộ-trình-tổng-thể.md](00-lo-trinh-tong-the.md) để xem toàn cảnh 7 phase,
hoặc [README.md](../../README.md) để quay về mục lục `wiki-core/`.

# P3 — `Platform.Persistence`

> **Định nghĩa hoàn thành:** một entity implement `IAuditEntity` + `ISoftDelete`
> được `Add()` qua `IGenericRepository`, `SaveChangesAsync()` qua `IUnitOfWork` —
> và khi đọc lại bằng `dbContext.Set<T>()` bình thường (không `IgnoreQueryFilters`),
> dòng đã soft-delete **không xuất hiện**, còn dòng còn sống thì có sẵn
> `DateCreate`/`UserCreate` mà handler **không hề set tay**. Đồng thời: insert
> trùng khoá tự nhiên (unique constraint) phải trả lỗi domain đọc được
> (`ConflictException` → HTTP 409), không phải `DbUpdateException` 500 lộ SQL ra
> ngoài.

P3 không tạo interface mới — `IGenericRepository<TEntity,TKey>` và `IUnitOfWork`
đã khai ở P1 (`Platform.Domain.Repositories`), `ITransactionManager` đã khai ở P2
(`Platform.Application.CQRS`). Việc của P3 là **implement** 3 interface đó, cộng
một tầng convention/interceptor chạy ngầm mà không handler nào phải biết tới.

Đây là phase dễ viết sai thầm lặng nhất trong cả lộ trình: mọi lỗi ở P1/P2 đều
gây lỗi biên dịch, còn lỗi ở P3 (thiếu 1 filter, sai thứ tự interceptor, quên
`ValueGeneratedNever`) thường **build xanh, chạy được, và chỉ lộ ra dưới tải hoặc
sau vài nghìn dòng dữ liệu**.

---

## 1. Vì sao 3 quyết định dưới đây không thể đảo ngược sau khi có module đầu tiên

| # | Quyết định | Cái giá nếu đổi sau |
| --- | --- | --- |
| 1 | **PK sinh ở tầng nào** — DB-generated hay app tự set qua `EntityId.New()` | Đổi từ DB-generated sang app-generated (hoặc ngược lại) sau khi đã có dữ liệu = viết lại toàn bộ migration + risk trùng khoá khi merge dữ liệu cũ |
| 2 | **Filter soft-delete áp global hay áp từng query** | Global filter (đúng) cài ở `OnModelCreating` — đổi sang per-query sau nghĩa là audit lại **mọi** LINQ query đã viết để thêm `.Where(x => !x.IsDelete)` thủ công, và chắc chắn sẽ quên vài chỗ |
| 3 | **`SaveChanges` gọi ở đâu — Repository hay UnitOfWork** | Đã chốt: Repository chỉ `Add/Update/Remove` vào ChangeTracker, **không** gọi `SaveChanges`. Đảo lại nghĩa là một entity có thể bị persist nửa chừng trong khi transaction bao ngoài (P2 §6.4) tưởng vẫn đang mở |

Quyết định #1 là quyết định đắt nhất. VNR.Successor chọn **app tự set PK** (mọi
`Guid` sinh qua `EntityId.New()`, xem [02-p1](02-p1-platform-domain.md)) và khai
tường minh `ValueGenerated.Never` cho EF biết trước — lý do kỹ thuật ở §4 dưới
đây không phải sở thích, mà là né một bug thật đã xảy ra.

---

## 2. File inventory — tối thiểu để thoát P3

| # | File | Namespace | Bắt buộc P3? |
| --- | --- | --- | --- |
| 1 | `Context/BaseDbContext.cs` | `Persistence.Context` | ✅ |
| 2 | `Context/PlatformConventions.cs` | `Persistence.Context` | ✅ |
| 3 | `Context/IBoundedContext.cs` | `Persistence.Context` | ✅ |
| 4 | `Interceptors/EntityIdGenerationInterceptor.cs` | `Persistence.Interceptors` | ✅ |
| 5 | `Interceptors/AuditInterceptor.cs` | `Persistence.Interceptors` | ✅ |
| 6 | `Extensions/SoftDeleteExtensions.cs` | `Persistence.Extensions` | ✅ |
| 7 | `Repositories/GenericRepository.cs` | `Persistence.Repositories` | ✅ |
| 8 | `UnitOfWork/UnitOfWork.cs` | `Persistence.UnitOfWork` | ✅ |
| 9 | `SchemaNames.cs` | `Persistence` | ✅ |
| 10 | `DesignTime/BaseDesignTimeFactory.cs` | `Persistence.DesignTime` | ✅ (cần ngay khi chạy `dotnet ef migrations add`) |
| 11 | `DependencyInjection/PlatformPersistenceExtensions.cs` (rút gọn — xem §7) | `Persistence.DependencyInjection` | ✅ (bản rút gọn) |
| 12 | `Interceptors/DomainEventInterceptor.cs` | `Persistence.Interceptors` | ⏳ — chỉ cần khi P1 đã có `IHasDomainEvents` **và** module đầu tiên thật sự raise domain event |
| 13 | `Interceptors/ChangeLogInterceptor.cs` | `Persistence.Interceptors` | ⏳ — hoãn tới khi có yêu cầu audit trail chi tiết (old/new value), không phải "cứ có audit field là đủ" |
| 14 | `Interceptors/CatalogCacheInvalidationInterceptor.cs` | `Persistence.Interceptors` | ⏳ — cần cache layer (`ICacheService`) trước, chưa có ở P3 |
| 15 | `Interceptors/PostgresAccentInterceptor.cs` | `Persistence.Interceptors` | ⏳ — chỉ cần khi có search tiếng Việt accent-insensitive |
| — | `Grid/*` (16 file), `Search/*`, `Repositories/GridRepository.cs`, `Repositories/EntityGridQuery.cs`, `Repositories/EntityLookupQuery.cs` | | ❌ — thuộc P6+, đây là cơ chế grid/lookup metadata-driven, không phải nền móng persistence |

**Successor thật có ~66 file trong `VNR.Platform.Persistence`.** P3 chỉ cần 11
file đầu (cột ✅). 4 interceptor còn lại (⏳) là **có thật và đáng dùng**, nhưng
mỗi cái phục vụ một nhu cầu chưa phát sinh ở P3 — thêm sớm nghĩa là code chạy mà
không ai kiểm chứng được nó đúng (không có domain event nào để dispatch, không
có audit trail nào cần so old/new). Cùng nguyên tắc "khai theo nhu cầu" đã áp
dụng cho `Interfaces/` ở [03-p2 §1](03-p2-platform-application.md).

---

## 3. Thứ tự viết trong P3

```
B1. SchemaNames + IBoundedContext        (~15 phút, thuần khai báo)
B2. PlatformConventions                   (0.5 ngày — nhiều convention nhỏ,
                                            từng cái phải test riêng)
B3. Interceptor bộ ba: Id-gen → Audit    (0.5 ngày — 2 interceptor đơn giản,
    (soft-delete nằm trong Conventions)   filter nằm trong B2 không phải B3)
B4. BaseDbContext                         (0.5 ngày — ráp B1-B3 lại)
B5. GenericRepository + UnitOfWork        (1 ngày — đây là nơi domain
                                            interface P1 gặp EF Core lần đầu)
B6. DesignTimeFactory + migration đầu tiên (0.5 ngày)
```

Lý do B2 (Conventions) đứng trước B3 (Interceptor): convention áp dụng lúc
**build model** (`OnModelCreating`, một lần khi app start), còn interceptor chạy
**mỗi lần `SaveChanges`** (runtime, mọi request). Convention sai → sai vĩnh viễn
cho tới khi migration lại. Interceptor sai → sai từng request, dễ sửa hơn nhưng
khó phát hiện hơn (không đỏ ở build, không đỏ ở migration).

---

## 4. `PlatformConventions` — vì sao mỗi convention tồn tại

`PlatformConventions.ApplyAllConventions()` (đọc verbatim tại
`Context/PlatformConventions.cs:54–71`) chạy **9 convention theo đúng thứ tự
liệt kê trong code** — thứ tự này không tuỳ tiện, có 2 chỗ phụ thuộc lẫn nhau
được ghi rõ trong comment gốc:

```csharp
public static void ApplyAllConventions(ModelBuilder modelBuilder, string schemaPrefix, string? providerName = null)
{
    ApplySoftDeleteFilter(modelBuilder);           // reflection-based (legacy compat, cross-interface)
    modelBuilder.ApplySoftDeleteFilter();          // interface-based (ISoftDelete — stronger contract)
    ApplyTableNamingConvention(modelBuilder, schemaPrefix);
    ApplyDecimalPrecision(modelBuilder);
    ApplyStringMaxLength(modelBuilder);
    ApplyStringCollation(modelBuilder, providerName);
    ApplyForeignKeyIndexConvention(modelBuilder);
    ApplyCascadeDeleteRestriction(modelBuilder);
    ApplyUtcDateTimeConvention(modelBuilder);
    ApplyKeyGenerationConvention(modelBuilder);
    ApplyUnaccentDbFunction(modelBuilder, providerName);
}
```

| # | Convention | Làm gì | [ĐƠN GIẢN HOÁ] cho hệ thống mới |
| --- | --- | --- | --- |
| 1–2 | `ApplySoftDeleteFilter` ×2 | Global query filter `WHERE IsDelete = false`. Chạy **2 lần** — 1 lần bằng reflection tìm property `IsDelete` bất kỳ (tương thích entity legacy không implement interface), 1 lần theo interface `ISoftDelete` thật | Chỉ cần bản interface-based (`modelBuilder.ApplySoftDeleteFilter()`, xem §5) — bản reflection chỉ tồn tại vì Successor có entity di sản trước khi có `ISoftDelete`. Hệ thống mới không có nợ đó |
| 3 | `ApplyTableNamingConvention` | Tên bảng = tên class entity, **không** cho EF tự số nhiều hoá | Giữ nguyên — số nhiều hoá tự động là nguồn nhầm lẫn kinh điển (`Entity` vs `Entities` vs `Entitys`) |
| 4 | `ApplyDecimalPrecision` | Mọi `decimal` mặc định `precision 18, scale 2` | Giữ nguyên |
| 5 | `ApplyStringMaxLength` | Mọi `string` chưa khai `MaxLength` → mặc định 500, tránh `nvarchar(max)` | Giữ nguyên — `nvarchar(max)` chặn index hiệu quả |
| 6 | `ApplyStringCollation` | SQL Server: `Vietnamese_CI_AI` (accent-insensitive) mọi cột string. PostgreSQL: bỏ qua (không có collation AI hiệu quả) → dựa vào `unaccent` extension ở query level | [ĐƠN GIẢN HOÁ] nếu chỉ dùng 1 provider — bỏ nhánh provider không dùng |
| 7 | `ApplyForeignKeyIndexConvention` | Auto-index **mọi FK** chưa có index | Giữ nguyên — quên index FK là nguyên nhân phổ biến nhất của "sao query chậm dần theo thời gian" |
| 8 | `ApplyCascadeDeleteRestriction` | Mọi FK → `DeleteBehavior.NoAction` | Giữ nguyên — cascade delete ngầm định của EF là quả bom hẹn giờ; xoá dây chuyền phải là quyết định tường minh của handler, không phải hành vi ngầm của ORM |
| 9 | `ApplyUtcDateTimeConvention` | `DateTime`/`DateTime?`: ghi → ép `Utc` nếu `Unspecified`; đọc → luôn gắn `Kind=Utc` | Giữ nguyên — giải quyết đúng 1 lỗi Npgsql cụ thể: *"Cannot write DateTime with Kind=Unspecified to timestamptz"*. Không có convention này, lỗi này sẽ tái diễn ở module đầu tiên có `DateTime` non-nullable |
| 10 | `ApplyKeyGenerationConvention` | Mọi PK kiểu `Guid`/`Guid?` → `ValueGenerated.Never` | **Giữ nguyên, không thương lượng** — xem lý do đầy đủ ở §4.1 |
| 11 | `ApplyUnaccentDbFunction` | Map hàm SQL `f_unaccent` (PostgreSQL) vào `HasDbFunction` để LINQ gọi được | ⏳ hoãn tới khi cần search tiếng Việt |

### 4.1 `ApplyKeyGenerationConvention` — bug thật đứng sau convention này

Đọc nguyên văn XML doc tại `PlatformConventions.cs:73–100`:

> *"Không khai báo này → EF coi mọi Guid PK là 'configured to use generated
> keys'. Hệ quả: khi một entity con MỚI (key đã set qua `EntityId.New()`) được
> thêm vào collection navigation của một aggregate ĐÃ TRACKED (vd sau
> `.Include()`), EF's fixup logic during `SaveChanges` coi key-đã-set = 'row đã
> tồn tại trong DB' → tracked sai state (không phải `Added`) → sinh UPDATE thay
> vì INSERT → `DbUpdateConcurrencyException` (0 rows affected)."*

Kịch bản cụ thể để hiểu tại sao đây không phải lý thuyết suông:

```
1. handler load Order (aggregate) kèm .Include(o => o.Lines)
   → EF tracked Order VÀ toàn bộ OrderLine hiện có
2. handler tạo OrderLine mới: new OrderLine { Id = EntityId.New(), ... }
3. order.Lines.Add(newLine)
4. unitOfWork.SaveChangesAsync()

Không có ApplyKeyGenerationConvention:
   EF thấy Id đã có giá trị (không phải Guid.Empty) trên 1 aggregate
   đang tracked → suy luận "đây là update, không phải insert"
   → sinh câu UPDATE OrderLine SET ... WHERE Id = @newId
   → 0 dòng bị ảnh hưởng (dòng đó chưa tồn tại trong DB)
   → DbUpdateConcurrencyException, ngẫu nhiên tuỳ thời điểm .Include() được gọi

Có ApplyKeyGenerationConvention (ValueGenerated.Never):
   EF không còn suy luận theo "key đã set = đã tồn tại"
   → tôn trọng EntityState do code set tường minh (Added qua .Add())
   → luôn sinh đúng INSERT
```

Convention này chỉ đúng vì hệ thống **cam kết không bao giờ để DB tự sinh
Guid PK**. Nếu một hệ thống mới chọn ngược lại (để DB sinh key), toàn bộ §4.1
không áp dụng — nhưng khi đó phải bỏ hẳn `EntityId.New()` tường minh ở handler
(P2 §4.8 ví dụ handler), không được để cả hai cơ chế cùng tồn tại nửa vời.

---

## 5. Soft-delete: 2 lớp, không phải 1

Có 2 nơi cùng xử lý soft-delete, mỗi nơi một trách nhiệm khác nhau — nhầm giữa
2 lớp này là lỗi hay gặp nhất khi copy code từ Successor:

| Lớp | File | Trách nhiệm |
| --- | --- | --- |
| Global **query filter** | `Extensions/SoftDeleteExtensions.cs` → `ApplySoftDeleteFilter(this ModelBuilder)` | Mọi `SELECT` (kể cả gián tiếp qua LINQ, navigation) tự thêm `WHERE "IsDelete" = false`. Gọi 1 lần trong `OnModelCreating` |
| **Unique index filter** | `Context/PlatformConventions.cs` → `FilteredSoftDeleteIndexConvention` (`IModelFinalizingConvention`, dòng 339–360) | Mọi **unique index** trên entity có `IsDelete` tự thêm `WHERE IsDelete = false` (Postgres) / `[IsDelete] = 0` (SQL Server) vào **định nghĩa index**, không phải vào query |

```csharp
// Extensions/SoftDeleteExtensions.cs
public static ModelBuilder ApplySoftDeleteFilter(this ModelBuilder modelBuilder)
{
    foreach (var entityType in modelBuilder.Model.GetEntityTypes())
    {
        if (!typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType)) continue;
        var parameter = Expression.Parameter(entityType.ClrType, "e");
        var property = Expression.Property(parameter, nameof(ISoftDelete.IsDelete));
        var condition = Expression.Equal(property, Expression.Constant(false));
        modelBuilder.Entity(entityType.ClrType).HasQueryFilter(Expression.Lambda(condition, parameter));
    }
    return modelBuilder;
}

public static IQueryable<T> IncludeDeleted<T>(this IQueryable<T> query) where T : class, ISoftDelete
    => query.IgnoreQueryFilters();
```

**Vì sao cần cả 2, thiếu 1 cái là bug:**

- Chỉ có query filter, thiếu index filter → tạo `Cat_CostCenter` có `Code`
  unique. Xoá mềm 1 dòng (`IsDelete = true`), rồi tạo dòng mới cùng `Code`.
  Query filter khiến `SELECT` không thấy dòng cũ → tưởng `Code` còn trống →
  nhưng **DB-level unique constraint vẫn thấy** dòng cũ (vật lý còn tồn tại)
  → `INSERT` thất bại với "duplicate key" dù nghiệp vụ đã "xoá" nó.
- Đây chính là kịch bản mà `ConflictException` ở `BaseDbContext.SaveChangesAsync`
  (§6) bắt được — nhưng bắt lỗi lúc runtime vẫn tệ hơn nhiều so với để filter
  index xử lý đúng ngay từ đầu (`WHERE IsDelete = false` trong index nghĩa là
  ràng buộc unique **chỉ tính trên dòng còn sống**, dòng đã xoá mềm không còn
  chiếm chỗ `Code` nữa).

`FilteredSoftDeleteIndexConvention` chạy ở `IModelFinalizingConvention` (không
phải trong `ApplyAllConventions` gọi từ `OnModelCreating`) vì nó phải bắt được
cả index khai trong `IEntityTypeConfiguration` — thứ chạy **sau**
`ApplyAllConventions` qua `ApplyConfigurationsFromAssembly()` ở module DbContext
kế thừa. Comment gốc ghi rõ điều này (dòng 334–338) — đây là chỗ dễ hiểu lầm
nhất nếu chỉ đọc `ApplyAllConventions` mà không biết `ConfigureConventions`
(nơi đăng ký `IModelFinalizingConvention`) chạy ở giai đoạn khác.

---

## 6. `BaseDbContext` — chữ ký đầy đủ

```csharp
public abstract class BaseDbContext : DbContext
{
    protected abstract string SchemaName { get; }         // "hre", "systems", "notification"...
    protected BaseDbContext(DbContextOptions options) : base(options) { }

    public bool IsNpgsql    => Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL";
    public bool IsSqlServer => Database.ProviderName == "Microsoft.EntityFrameworkCore.SqlServer";

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        => PlatformConventions.AddPlatformConventions(configurationBuilder, Database.ProviderName);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        PlatformConventions.ApplyAllConventions(modelBuilder, SchemaName, Database.ProviderName);
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        try { return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken); }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        { throw new ConflictException("Dữ liệu đã tồn tại — vi phạm ràng buộc duy nhất.", ex); }
    }
    // Override CẢ overload (bool acceptAllChangesOnSuccess) không tham số — mọi call SaveChanges
    // (kể cả EF gọi nội bộ từ SaveChanges() đồng bộ) đều đi qua nhánh bắt lỗi này.

    private static bool IsUniqueViolation(DbUpdateException exception)
        => exception.InnerException?.GetType().GetProperty("SqlState")?.GetValue(exception.InnerException) as string == "23505";
}
```

3 quyết định thiết kế đáng chú ý:

1. **`SchemaName` là `abstract`, không phải tham số constructor.** Module
   DbContext override property, không truyền string vào `base(...)` — tránh
   một lớp con quên truyền hoặc truyền sai schema của module khác (lỗi copy-paste
   giữa các `{Module}DbContext`).
2. **`IsUniqueViolation` đọc `SqlState` bằng reflection, không cast
   `PostgresException`.** `Platform.Persistence` **không** được reference
   `Npgsql.EntityFrameworkCore.PostgreSQL` trực tiếp (provider-agnostic — module
   nào dùng SQL Server vẫn build được `BaseDbContext`). Reflection trả `null`
   an toàn nếu exception không có property đó (SQL Server ném loại exception
   khác hẳn — `23505` là mã lỗi riêng của PostgreSQL cho unique-violation).
   `[ĐƠN GIẢN HOÁ]` cho hệ thống chỉ dùng 1 provider: có thể cast trực tiếp,
   nhưng ghi rõ trong ADR rằng đây là quyết định **khoá cứng 1 provider**.
3. **Override cả `SaveChangesAsync(bool, CancellationToken)` — không override
   `SaveChangesAsync(CancellationToken)`.** Overload không `acceptAllChanges`
   thực chất gọi overload có `acceptAllChanges = true` bên trong EF Core, nên
   override đúng 1 chỗ là đủ bắt mọi đường gọi.

---

## 7. Interceptor bộ ba — Id-gen, Audit, Domain (khi cần)

Cả 3 interceptor giữ chung 1 khuôn: `SaveChangesInterceptor`, đăng ký
**singleton**, override cả cặp `SavingChanges`/`SavingChangesAsync` (chạy
**trước** khi ghi DB) hoặc `SavedChanges`/`SavedChangesAsync` (chạy **sau** khi
ghi DB thành công).

### 7.1 `EntityIdGenerationInterceptor` — auto-fill Id nếu quên gọi `EntityId.New()`

```csharp
public sealed class EntityIdGenerationInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    { AssignMissingIds(eventData.Context); return base.SavingChanges(eventData, result); }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    { AssignMissingIds(eventData.Context); return base.SavingChangesAsync(eventData, result, cancellationToken); }

    private static void AssignMissingIds(DbContext? context)
    {
        if (context is null) return;
        foreach (var entry in context.ChangeTracker.Entries<BaseEntity<Guid>>())
            if (entry.State == EntityState.Added && entry.Entity.Id == Guid.Empty)
                entry.Entity.Id = EntityId.New();
    }
}
```

Đây là **lưới an toàn**, không phải cơ chế chính. `ApplyKeyGenerationConvention`
(§4.1) tắt hẳn khả năng EF tự sinh Guid — nếu handler quên gọi `EntityId.New()`
trước khi `Add()`, entity sẽ vào DB với `Id = Guid.Empty` (không có gì tự sửa).
Interceptor này khôi phục lại hành vi "tự điền nếu thiếu" một cách tường minh và
toàn cục, thay vì bắt buộc mọi handler tự nhớ.

### 7.2 `AuditInterceptor` — set `DateCreate`/`UserCreate`/`DateUpdate`/`UserUpdate`

```csharp
public sealed class AuditInterceptor(IServiceProvider serviceProvider) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    { ApplyAuditFields(eventData.Context); return base.SavingChanges(eventData, result); }
    // ...SavingChangesAsync tương tự...

    private void ApplyAuditFields(DbContext? context)
    {
        if (context == null) return;
        using var scope = serviceProvider.CreateScope();
        var userName = scope.ServiceProvider.GetService<ICurrentUser>()?.UserName ?? "system";
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries<IAuditEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.DateCreate = now; entry.Entity.UserCreate = userName;
                    entry.Entity.DateUpdate = now; entry.Entity.UserUpdate = userName;
                    break;
                case EntityState.Modified:
                    entry.Entity.DateUpdate = now; entry.Entity.UserUpdate = userName;
                    break;
            }
        }
    }
}
```

Hai chi tiết dễ bỏ sót khi tự viết lại:

- **Singleton nhưng resolve `ICurrentUser` (scoped) qua `IServiceProvider.CreateScope()`
  mỗi lần `SaveChanges`.** Interceptor đăng ký 1 lần cho cả app (đỡ tốn), nhưng
  danh tính người dùng đổi theo từng request — không thể inject `ICurrentUser`
  thẳng vào constructor.
- **Không throw nếu `ICurrentUser` chưa đăng ký** — fallback `"system"`. Cho
  phép seed script / background job chạy `SaveChanges` mà không cần
  `HttpContext`, không cần mock `ICurrentUser` giả.
- **`Added` set cả `DateUpdate`/`UserUpdate` bằng giá trị `DateCreate`/`UserCreate`.**
  Không để 2 cột đó `null` lúc mới tạo — FE không phải viết `DateUpdate ??
  DateCreate` ở mọi nơi hiển thị "lần sửa cuối".

### 7.3 `DomainEventInterceptor` — ⏳ hoãn, nhưng đọc để biết khi nào bật

Chỉ override **async path** (`SavedChangesAsync`), không override bản đồng bộ:

```csharp
public override async ValueTask<int> SavedChangesAsync(
    SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
{
    await DispatchDomainEventsAsync(eventData.Context, cancellationToken);
    return await base.SavedChangesAsync(eventData, result, cancellationToken);
}
```

Comment gốc giải thích lý do bỏ nhánh đồng bộ: *"tránh deadlock risk từ
`.GetAwaiter().GetResult()` trong ASP.NET Core"* — production code luôn đi qua
`SaveChangesAsync()`, viết cả bản đồng bộ chỉ để "cho đủ" sẽ phải gọi async
method từ context đồng bộ, đúng pattern gây deadlock kinh điển trên
`SynchronizationContext` của ASP.NET Core cũ (và vẫn có thể tái hiện tuỳ cấu
hình). **Không viết bản đồng bộ "cho đối xứng"** — đối xứng ở đây là nợ kỹ thuật,
không phải phong cách.

Chạy **sau** save (`SavedChangesAsync`, không phải `SavingChangesAsync`) vì
domain event mô tả "việc đã xảy ra" (`OrderPlacedEvent`, không phải
`OrderPlacingEvent`) — publish trước khi DB xác nhận commit là nói dối handler
khác rằng dữ liệu đã tồn tại trong khi có thể `SaveChanges` sẽ throw ngay sau
đó.

**Bật khi nào:** khi P1 đã dùng `IHasDomainEvents`/`AggregateRoot<TId>` **và**
module đầu tiên thật sự cần side-effect sau khi lưu (vd "gửi email sau khi tạo
đơn hàng") mà side-effect đó không cần nằm trong cùng transaction DB. Nếu module
đầu tiên chưa có nhu cầu này, khai `IHasDomainEvents` cho có ở P1 mà không dispatch
gì ở P3 là interface không có implementation thật — vi phạm nguyên tắc đã nêu ở
[03-p2 §1](03-p2-platform-application.md).

---

## 8. `GenericRepository<TEntity, TKey, TDbContext>` — nơi domain interface gặp EF Core lần đầu

```csharp
public class GenericRepository<TEntity, TKey, TDbContext>
    : ICrudRepository<TEntity, TKey>, ILegacyQuerySupport<TEntity, TKey>
    where TEntity : class, IHasId<TKey>
    where TKey : IEquatable<TKey>
    where TDbContext : DbContext
{
    private readonly TDbContext _dbContext;
    private readonly DbSet<TEntity> _dbSet;
    protected TDbContext DbContext => _dbContext;   // cho repository con truy cập DbSet khác / raw query

    public GenericRepository(TDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _dbSet = _dbContext.Set<TEntity>();
    }

    public virtual async Task<TEntity?> GetByIdAsync(TKey id, CancellationToken ct = default)
        => await _dbSet.FindAsync([id], ct);

    public virtual async Task AddAsync(TEntity entity, CancellationToken ct = default)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        await _dbSet.AddAsync(entity, ct);
        // KHÔNG gọi SaveChangesAsync ở đây.
    }

    public virtual Task DeleteAsync(TEntity entity, CancellationToken ct = default)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        if (entity is ISoftDelete softDeletable)
        {
            softDeletable.IsDelete = true;
            if (_dbContext.Entry(entity).State == EntityState.Detached)
                _dbSet.Update(entity);
        }
        else
        {
            _dbSet.Remove(entity);
        }
        return Task.CompletedTask;
    }

    // ICrudRepository — CHỈ CrudHandler (pattern 1 zero-handler) inject:
    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _dbContext.SaveChangesAsync(ct);
}
```

4 điều phải giữ nguyên khi viết lại cho hệ thống mới:

1. **`AddAsync`/`UpdateAsync`/`DeleteAsync` không gọi `SaveChangesAsync`.** Chúng
   chỉ đổi trạng thái `ChangeTracker`. `SaveChangesAsync` là hành động của
   `IUnitOfWork` (module handler tự inject) hoặc `ICrudRepository`
   (`CrudHandler` — pattern 1 — mới được phép gọi, xem §8.1). Gộp 2 việc này
   vào 1 lời gọi (`repo.AddAsync()` tự save luôn) là lỗi thiết kế phổ biến nhất
   khi copy repository pattern từ tutorial — nó phá vỡ khả năng gộp nhiều mutation
   vào 1 transaction.
2. **`DeleteAsync` tự rẽ nhánh soft-delete vs hard-delete** dựa trên
   `entity is ISoftDelete` — handler gọi `DeleteAsync` **luôn luôn** mà không
   cần biết entity có soft-delete hay không. Đây là điểm mà `ISoftDelete` (P1)
   thật sự trả giá trị: logic rẽ nhánh nằm đúng 1 chỗ, không rải ở từng handler.
3. **`GenericRepository<,,>` implement CẢ `ICrudRepository` (P2) lẫn
   `ILegacyQuerySupport` (P1, `[Obsolete]`).** `ILegacyQuerySupport` tồn tại để
   migrate dần code cũ từng leak `IQueryable` ra Application layer — hệ thống
   mới **không cần implement interface này**, chỉ cần biết nó tồn tại để không
   nhầm là API chính thức.
4. **`GetByIdsAsync` chặn cứng 1000 phần tử** (`throw ArgumentException`), còn
   `GetByIdsBatchAsync` tự chia batch 500. Không phải chi tiết vặt — đây là
   nguyên tắc "API tường minh về giới hạn thay vì im lặng chịu đựng": gọi
   `IN (...)` với 50,000 Guid vừa chậm vừa có thể vượt giới hạn tham số của
   driver DB; buộc caller chọn rõ "tôi biết chắc ít" (`GetByIdsAsync`, throw nếu
   sai) hay "tôi không chắc, cứ tự chia nhỏ giúp tôi" (`GetByIdsBatchAsync`).

### 8.1 Vì sao `ICrudRepository` tách riêng `SaveChangesAsync` khỏi `IGenericRepository`

```csharp
// Platform.Application.Crud/ICrudRepository.cs
public interface ICrudRepository<TEntity, TKey> : IGenericRepository<TEntity, TKey>
    where TEntity : class, IHasId<TKey>
    where TKey : IEquatable<TKey>
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

XML doc gốc: *"Tách riêng để dev không thấy `SaveChangesAsync` trong
IntelliSense khi dùng `IGenericRepository`."* Đây là ranh giới có chủ đích:

| Ai inject gì | Thấy `SaveChangesAsync`? |
| --- | --- |
| Module handler (vertical slice, pattern 2) inject `IGenericRepository<TEntity,TKey>` | ❌ — bắt buộc qua `IUnitOfWork.SaveChangesAsync()` để nằm đúng transaction bao ngoài |
| `CrudHandler` nội bộ Platform (pattern 1, zero-handler catalog CRUD) inject `ICrudRepository<TEntity,TKey>` | ✅ — CrudHandler chính là lớp duy nhất được phép tự save vì nó **là** đơn vị công việc cho catalog CRUD đơn giản |

Cùng một implementation (`GenericRepository<,,>`), 2 interface nhìn thấy 2 tập
method khác nhau — IntelliSense-level guardrail, không phải run-time check, và
đúng là arch rule "2 pattern CRUD, không có tầng giữa" (P0 §1 nguyên tắc #4)
được thực thi ngay từ chữ ký interface.

---

## 9. `UnitOfWork<TDbContext>` — hợp đồng kép cố ý

```csharp
public class UnitOfWork<TDbContext> : IUnitOfWork, ITransactionManager, IDisposable
    where TDbContext : DbContext
{
    private const int DefaultCommandTimeoutSeconds = 500;
    // ctor: Context.Database.SetCommandTimeout(DefaultCommandTimeoutSeconds)

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        => await Context.SaveChangesAsync(ct);

    public async Task BeginAsync(CancellationToken ct = default) { /* throw nếu đã có transaction đang chạy */ }
    public async Task CommitAsync(CancellationToken ct = default) { /* throw nếu không có transaction; finally DisposeTransaction() */ }
    public async Task RollbackAsync(CancellationToken ct = default) { /* im lặng return nếu không có transaction */ }

    /// IUnitOfWork path — TỰ gọi SaveChangesAsync. Dùng trong service/utility code
    /// gọi trực tiếp UoW, không đi qua ModuleTransactionBehaviorBase.
    public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action, CancellationToken ct = default)
    {
        var strategy = Context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await Context.Database.BeginTransactionAsync(ct);
            try
            {
                var result = await action();
                await Context.SaveChangesAsync(ct);       // ← CÓ save
                await transaction.CommitAsync(ct);
                return result;
            }
            catch { await transaction.RollbackAsync(ct); throw; }
        });
    }

    /// ITransactionManager path (explicit interface impl) — KHÔNG gọi SaveChangesAsync,
    /// handler tự gọi bên trong operation. Dùng bởi ModuleTransactionBehaviorBase (P2 §6.4).
    async Task<TResult> ITransactionManager.ExecuteInTransactionAsync<TResult>(
        Func<Task<TResult>> operation, CancellationToken ct)
    {
        var strategy = Context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await Context.Database.BeginTransactionAsync(ct);
            try
            {
                var result = await operation();           // ← KHÔNG SaveChanges
                await transaction.CommitAsync(ct);
                return result;
            }
            catch { await transaction.RollbackAsync(ct); throw; }
        });
    }
}
```

Module kế thừa với DbContext cụ thể của mình:

```csharp
public class OrgUnitOfWork(OrgDbContext ctx) : UnitOfWork<OrgDbContext>(ctx), IOrgUnitOfWork;
```

### 9.1 Vì sao 2 phương thức cùng tên `ExecuteInTransactionAsync` phải khác hành vi

Đây là điểm dễ hiểu lầm nhất trong toàn bộ P3 — **cùng một class**, cùng tên
method, nhưng 2 interface nhìn thấy 2 hành vi khác nhau nhờ *explicit interface
implementation* (`Task<TResult> ITransactionManager.ExecuteInTransactionAsync(...)`).

| Path | Ai gọi | `SaveChangesAsync` bên trong? | Vì sao |
| --- | --- | --- | --- |
| `IUnitOfWork.ExecuteInTransactionAsync` | Service/utility code tự quản lý transaction thủ công, không đi qua MediatR pipeline | ✅ Có | Đây là API "tiện dụng, tự đủ" — caller không cần biết gì thêm |
| `ITransactionManager.ExecuteInTransactionAsync` | `ModuleTransactionBehaviorBase` (P2 §6.4) — pipeline behavior bọc quanh **command handler** | ❌ Không | Vì handler đã tự gọi `unitOfWork.SaveChangesAsync()` **bên trong** `operation`. Nếu behavior save thêm 1 lần nữa → gọi `SaveChangesAsync` 2 lần trên cùng transaction — vô hại nếu không có gì đổi giữa 2 lần, nhưng **sai nếu handler cố tình chưa muốn save** (vd handler gọi 2 sub-step, mỗi step tự quyết có save hay không dựa trên business rule) |

Nói cách khác: `IUnitOfWork` path trả lời câu hỏi *"tôi có 1 khối code tuỳ ý cần
chạy trong transaction, hãy tự lo hết cho tôi"*. `ITransactionManager` path trả
lời câu hỏi khác hẳn — *"tôi (behavior) chỉ mở/đóng transaction bao ngoài,
CHÍNH handler bên trong quyết định khi nào save"*. Gộp 2 interface thành 1 (chỉ
giữ 1 bản `ExecuteInTransactionAsync` gọi `SaveChanges` vô điều kiện) sẽ phá
đúng use-case mà `ITransactionManager` được tạo ra để phục vụ.

### 9.2 `CreateExecutionStrategy()` — bắt buộc, không phải tuỳ chọn

Cả 2 nhánh đều bọc `BeginTransactionAsync` bên trong
`Context.Database.CreateExecutionStrategy().ExecuteAsync(...)`. Nếu provider
dùng retry strategy (`NpgsqlRetryingExecutionStrategy`/
`SqlServerRetryingExecutionStrategy` — bật khi cấu hình
`EnableRetryOnFailure()`), EF Core **cấm** tự mở `BeginTransactionAsync` ngoài
execution strategy và sẽ throw
`InvalidOperationException: "The configured execution strategy ... does not
support user-initiated transactions"` ngay tại lần gọi đầu tiên gặp lỗi tạm thời
(transient fault) cần retry. Bọc sẵn từ đầu (kể cả khi chưa bật retry) khiến
việc bật `EnableRetryOnFailure()` sau này **không cần sửa `UnitOfWork`** — chỉ
đổi 1 dòng cấu hình provider.

---

## 10. `SchemaName` — DIP Seam giữa Persistence và Module

`Context/IBoundedContext.cs`:

```csharp
public interface IBoundedContext
{
    string ContextName { get; }   // "Organization", "HumanResource"...
    string SchemaName { get; }    // "org", "hre"...
}
```

Comment gốc: *"SaveChanges is NOT exposed here — handler must go through
IUnitOfWork only."* Đây chính là **DIP Seam** mà [00-lộ-trình §4](00-lo-trinh-tong-the.md)
nhắc tới: mọi `{Module}DbContext` implement `IBoundedContext`, và
`AddModuleDbContext<TDbContext>()` (`PlatformPersistenceExtensions.cs:164–165`)
đăng ký nó dưới `IBoundedContext` không phải dưới kiểu `DbContext` cụ thể:

```csharp
if (typeof(IBoundedContext).IsAssignableFrom(typeof(TDbContext)))
    services.AddScoped<IBoundedContext>(sp => (IBoundedContext)sp.GetRequiredService<TDbContext>());
```

Nhờ đó, một service ở tầng **Infrastructure dùng chung** (ví dụ tool sinh
metadata cross-module, hoặc health-check liệt kê "process này có bao nhiêu
schema") có thể `IEnumerable<IBoundedContext>` để duyệt **mọi** DbContext đã
đăng ký trong process — mà không cần reference `Module.{X}.Infrastructure` của
từng module cụ thể (nếu reference trực tiếp sẽ vi phạm luật
`❌ Infrastructure.A → Infrastructure.B` ở [00-lộ-trình §3](00-lo-trinh-tong-the.md)).
`SchemaName` trên interface (không phải hard-code string ở nơi dùng) là điều
kiện để phép liệt kê này hoạt động mà không đụng vào biết-quá-nhiều.

---

## 11. Chiến lược FK — vì sao `NoAction` toàn cục và index tự động

Đã nêu ở §4 (convention #7, #8), nhắc lại ở đây vì đây là quyết định kiến trúc
độc lập, không chỉ là "1 dòng convention":

- **`DeleteBehavior.NoAction` cho MỌI FK** — không có ngoại lệ per-entity trong
  convention. Muốn cascade thật sự (hiếm, và luôn phải là quyết định nghiệp vụ
  tường minh), override **sau** `ApplyAllConventions` trong
  `IEntityTypeConfiguration` cụ thể của entity đó — không sửa convention toàn
  cục để nới cho 1 trường hợp.
- **FK luôn có index** — `ApplyForeignKeyIndexConvention` quét mọi FK chưa được
  index (kể cả index đã khai phủ FK dưới dạng prefix của composite index thì
  bỏ qua, tránh trùng) và tự thêm. Lý do thực dụng: EF Core tự tạo index cho FK
  trong nhiều trường hợp nhưng **không phải tất cả** (shadow FK là trường hợp
  hay lọt lưới) — convention này đóng nốt phần EF bỏ sót, không thay thế hoàn
  toàn cơ chế của EF.

---

## 12. Cấm ở tầng Persistence

| Cấm | Vì sao | ArchTest gợi ý |
| --- | --- | --- |
| Business logic trong `IEntityTypeConfiguration`/interceptor | Interceptor là **cross-cutting kỹ thuật** (audit/id-gen/cache), không phải nơi validate nghiệp vụ — validate đã có `ValidationBehavior` ở P2 | `T_PERSIST_NoBusinessLogic` |
| `Persistence` reference `Module.*.Application`/`Module.*.Domain` | Hướng phụ thuộc chỉ 1 chiều: `Module.*.Infrastructure` (kế thừa `BaseDbContext`) → `Platform.Persistence`, không ngược lại | `T_LAYER_PersistenceNoModule` |
| Repository trả `IQueryable<T>` ra ngoài (trừ `ILegacyQuerySupport` đã `[Obsolete]`) | Leak khả năng compose LINQ tuỳ ý ra Application layer — Application không được biết EF Core tồn tại (P2 §8) | `T_LAYER_App_NoEfCore` (đã có ở P2), cộng kiểm tra kiểu trả về của method public trong `Repositories/*` |
| Cast cứng `PostgresException`/`SqlException` trong `Platform.Persistence` (ngoài `BaseDbContext.IsUniqueViolation` — nơi DUY NHẤT được phép, và ở đó cũng chỉ dùng reflection) | Giữ `Platform.Persistence` provider-agnostic — module nào dùng SQL Server vẫn build được | Review thủ công + không `PackageReference Npgsql.*` trực tiếp trong `Platform.Persistence.csproj` |
| Gọi `DbContext.SaveChanges()` (đồng bộ) ở bất kỳ đâu ngoài `BaseDbContext` nội bộ | Toàn hệ thống cam kết async — sync `SaveChanges` bên trong request ASP.NET Core có nguy cơ deadlock (cùng lý do §7.3) | `T_PERSIST_NoSyncSaveChanges` |

---

## 13. Checklist rời P3

- [ ] `BaseDbContext` override đúng `ConfigureConventions` + `OnModelCreating` + `SaveChangesAsync(bool, CancellationToken)`
- [ ] `PlatformConventions.ApplyAllConventions` chạy đủ 9 bước theo đúng thứ tự — đặc biệt `ApplyKeyGenerationConvention` (§4.1) và cặp soft-delete filter (§5)
- [ ] Kiểm chứng §4.1 bằng test thật: load aggregate qua `.Include()`, thêm child mới, `SaveChangesAsync()` → phải sinh `INSERT`, không phải `UPDATE`/`DbUpdateConcurrencyException`
- [ ] Kiểm chứng §5 bằng test thật: soft-delete 1 dòng có unique constraint, tạo dòng mới cùng giá trị unique → phải thành công (không có `FilteredSoftDeleteIndexConvention` sẽ fail)
- [ ] `EntityIdGenerationInterceptor` + `AuditInterceptor` đăng ký **singleton**, gắn vào `DbContextOptionsBuilder` qua `AddInterceptors` trong `AddModuleDbContext`
- [ ] `GenericRepository<TEntity,TKey,TDbContext>.AddAsync/UpdateAsync/DeleteAsync` **không** gọi `SaveChangesAsync`
- [ ] `ICrudRepository.SaveChangesAsync` chỉ được `CrudHandler` (Platform nội bộ) inject — module handler chỉ thấy `IGenericRepository`
- [ ] `UnitOfWork<TDbContext>` implement cả `IUnitOfWork` và `ITransactionManager` — 2 bản `ExecuteInTransactionAsync` khác hành vi save (§9.1), cả 2 đều bọc `CreateExecutionStrategy()`
- [ ] `{Module}DbContext` implement `IBoundedContext`, override đúng `SchemaName`
- [ ] Migration đầu tiên chạy được qua `dotnet ef migrations add Init` (cần `BaseDesignTimeFactory` + `Config/Common/Connections.json`)
- [ ] Insert trùng khoá tự nhiên (unique constraint) trả `ConflictException` → HTTP 409 (kiểm chứng qua tầng P4, nhưng lỗi phải bắt đúng ở đây trước)
- [ ] Không có `PackageReference` tới `Npgsql.*`/`Microsoft.Data.SqlClient` bên ngoài phạm vi cho phép ở §12

---

**Tiếp theo:** [05-p4-hosting-api.md](05-p4-hosting-api.md) — nơi `BaseApiController`
gọi `IUnitOfWork`/repository vừa dựng ở đây (gián tiếp qua handler), map
`ErrorCode` (P2 §4.1) sang HTTP status thật, và đóng nốt vòng lặp: `ConflictException`
sinh ra ở `BaseDbContext.SaveChangesAsync` (§6) đi qua `ExceptionHandlingBehavior`
(P2 §6.3) rồi ra HTTP 409 ở P4 — kiểm chứng toàn bộ chuỗi 3 phase nối với nhau
đúng như DoD đã hứa ở đầu file 03.

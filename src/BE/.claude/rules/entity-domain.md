# Entity & Domain — src/BE

## Base entity

**Đã CHỐT (2026-08-15):** theo
`doc/huong_dan/wiki-core/be/trien-khai/02-p1-platform-domain.md §5` —
`BaseEntity` dùng `public get; set;` cho đúng 6 field kỹ thuật dưới đây,
**khác** với field nghiệp vụ của entity con (luôn `private set`, xem mục
Factory method bên dưới).

```csharp
// Domain/Common/BaseEntity.cs
public abstract class BaseEntity
{
    public Guid Id { get; set; }
    public string? UserCreate { get; set; }
    public string? UserUpdate { get; set; }
    public DateTimeOffset? DateCreate { get; set; }
    public DateTimeOffset? DateUpdate { get; set; }
    public bool IsDelete { get; set; }
}
```

- Setter **`public`** cho đúng 6 field kỹ thuật này — **không phải sơ suất**.
  `AuditInterceptor`/`EntityIdGenerationInterceptor` (tầng Infrastructure,
  chạy trong `SaveChangesAsync`) phải **ghi** được các field này từ bên ngoài
  entity; nếu để `protected`/`private`, interceptor phải dùng reflection hoặc
  shadow property — phức tạp hơn nhiều so với cái nó bảo vệ. Đánh đổi này chỉ
  áp dụng cho đúng 6 field kỹ thuật ở trên — **không** mở rộng sang field
  nghiệp vụ.
- `UserCreate`/`UserUpdate` nullable — bản ghi seed/migration không có user
  nào tạo; interceptor set `"system"` khi chưa có `ICurrentUser` (vd trước khi
  auth thật được implement).
- `IsDelete`: soft delete, filter bằng EF global query filter khai trong
  `DbContext.OnModelCreating` — không tự thêm `.Where(x => !x.IsDelete)` ở
  từng query riêng lẻ.

## Factory method — không `new` + gán property

```csharp
public class Criteria : BaseEntity
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Group { get; private set; } = string.Empty;
    public decimal MaxScore { get; private set; }

    private Criteria() { }   // EF Core cần ctor rỗng, private để không dùng ở ngoài

    public static Criteria Create(string code, string name, string group, decimal maxScore)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("CRITERIA_CODE_REQUIRED", "Mã chỉ tiêu không được để trống.");
        if (maxScore <= 0)
            throw new DomainException("CRITERIA_MAX_SCORE_INVALID", "Điểm tối đa phải > 0.");

        return new Criteria
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = name,
            Group = group,
            MaxScore = maxScore,
            DateCreate = DateTimeOffset.UtcNow,
        };
    }

    public void UpdateScore(decimal maxScore)
    {
        if (maxScore <= 0)
            throw new DomainException("CRITERIA_MAX_SCORE_INVALID", "Điểm tối đa phải > 0.");
        MaxScore = maxScore;
        DateUpdate = DateTimeOffset.UtcNow;
    }
}
```

**Vì sao:** factory + mutation method có tên nghiệp vụ (`UpdateScore`, không
`set MaxScore`) giữ mọi invariant ở một chỗ. Nếu ai đó có thể `entity.MaxScore
= -5` từ bên ngoài, invariant "điểm tối đa phải dương" chỉ còn là quy ước bằng
lời, không phải luật được compiler/entity ép buộc.

## Value Object

Dùng khi khái niệm có **≥2 field đi cùng nhau** hoặc có **luật định dạng**
cần validate — không bọc VO cho một `decimal`/`string` đơn lẻ không có luật
gì đặc biệt.

```csharp
public sealed record Percentage
{
    public decimal Value { get; }
    private Percentage(decimal value) => Value = value;

    public static Percentage Create(decimal value)
    {
        if (value < 0 || value > 100)
            throw new DomainException("PERCENTAGE_OUT_OF_RANGE", "Giá trị phải trong khoảng 0–100.");
        return new Percentage(value);
    }
}
```

EF Core mapping cho VO 1 giá trị: `HasConversion` (không `OwnsOne` — chỉ
dùng `OwnsOne` khi VO có nhiều field).

## DomainException

```csharp
public class DomainException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
```

Ném từ Domain khi vi phạm invariant. Application layer bắt và chuyển thành
`IApiResult<T>` lỗi qua `ErrorDescriptor` tương ứng (`ErrorCode.BusinessRuleError`,
422 — xem `cqrs-handler.md` §ErrorDescriptor) — không để `DomainException`
lọt thẳng ra `Api` layer thành lỗi 500 chung chung.

## RowVersion — optimistic concurrency cho entity nhiều người sửa

**Finding thật (2026-08-17):** `CriteriaAssessment` có 2 luồng ghi độc lập
đụng cùng 1 bản ghi — import CSV hàng loạt (ghi đè toàn bộ field) và sửa tay
từng field qua `UpdateCriteriaAssessmentCommand` — không có gì phát hiện khi
2 luồng ghi đè lên nhau, người sửa sau âm thầm mất thay đổi của người trước.
Xem quy tắc chung + lý do ở
[be/06-concurrency-control.md](../../../../doc/huong_dan/wiki-core/be/06-concurrency-control.md).

```csharp
public class CriteriaAssessment : BaseEntity
{
    // ... field nghiệp vụ hiện có
    public byte[] RowVersion { get; private set; } = default!;
}

// EF configuration
builder.Property(x => x.RowVersion).IsRowVersion();
```

EF Core tự thêm `WHERE "RowVersion" = @originalValue` vào UPDATE — ghi đè bởi
người khác trong lúc đang sửa → `DbUpdateConcurrencyException` → handler trả
`Conflict` (409) thay vì âm thầm ghi đè. **Chỉ thêm cho entity nhiều người
cùng sửa** (đúng `CriteriaAssessment`) — không thêm tràn lan cho entity chỉ 1
người sở hữu (vd hồ sơ cá nhân tự sửa).

## FK cross-module — 3 tầng (áp dụng khi có module nghiệp vụ thứ 2)

Hiện `Modules.DtiWeekly` là module nghiệp vụ duy nhất nên chưa có tình huống
FK trỏ sang module khác — ghi quy tắc trước để không phải quyết định vội khi
module thứ 2 xuất hiện (đối chiếu VNR.Successor — xem
`src/BE/.claude/rules/architecture.md` §"Cần dùng chung logic?"):

| Phạm vi | Loại FK | `DeleteBehavior` |
| --- | --- | --- |
| Cùng aggregate (vd `CriteriaEvidence` → `CriteriaAssessment`) | Hard FK | `Cascade` |
| Cùng module, khác aggregate (vd `Criteria` → `CriteriaGroup`) | Hard FK | `Restrict` |
| Khác module nghiệp vụ | Soft FK (`Guid?` thuần, không constraint DB) | Không có — chỉ `HasIndex`, validate tồn tại ở tầng Application (`await repo.ExistsAsync(id, ct)`) |

Lý do: ranh giới module quan trọng hơn ranh giới DB vật lý — dù 2 module
chung 1 database, FK cứng xuyên module tạo coupling ngầm mà ArchTest không
bắt được (khác với coupling qua `using` mà ArchTest quét được).

## Khi thêm entity mới

1. Đối chiếu `doc/ERD/example_db_ver1.csv` nếu liên quan tới domain "theo
   dõi tiêu chí" — nhưng nhớ đây là **dữ liệu mẫu**, không phải schema đã
   chốt (xem `src/BE/CLAUDE.md`).
2. Entity ở `{Core|Modules.<Ten>}.Domain/Entities/{Name}.cs` — Core nếu dùng
   lại được cho mọi module, Modules.<Ten> nếu đặc thù 1 domain nghiệp vụ (xem
   `doc/kien-truc-core-module.md`).
3. EF configuration ở
   `{Core|Modules.<Ten>}.Infrastructure/Persistence/Configurations/{Name}Configuration.cs`
   — cùng project với entity ở bước 2. FK sang module khác (nếu có) áp dụng
   bảng 3 tầng ở trên.
4. KHÔNG khai `DbSet<{Name}>` trên `PlatformManagerDbContext` nếu entity
   thuộc 1 Module (Core không được reference Modules.*.Domain) — Module tự
   gọi `Set<{Name}>()` trực tiếp trong repository của mình. Chỉ entity Core
   mới có `DbSet<{Name}>` đặt tên.
5. Migration: `dotnet ef migrations add Add{Name} --project
   PlatformManager.Core.Infrastructure --startup-project PlatformManager.Api`
   — đọc lại file migration sinh ra trước khi tin, đừng chạy `database
   update` mà không xem trước.

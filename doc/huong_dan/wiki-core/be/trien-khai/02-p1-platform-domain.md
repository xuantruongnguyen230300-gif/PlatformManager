# P1 — `Platform.Domain`

> **Định nghĩa hoàn thành:** project `Platform.Domain` build xanh với **zero
> `PackageReference`**, có đủ base entity / value object / exception /
> abstraction repository, và ArchTest `Domain_MustNotDependOn_AnyOtherLayer`
> xanh sau khi đã được kiểm chứng bằng cách cố tình làm nó đỏ.

Đây là phase duy nhất **không ai chờ nó và nó không chờ ai**. Chính vì vậy nó
là phase dễ làm hỏng nhất: không có compiler nào ngăn bạn thêm
`Microsoft.EntityFrameworkCore` vào đây, và một khi đã thêm thì mọi layer rule
phía sau đều vô nghĩa.

**Nguyên tắc duy nhất của phase này:** file trong `Platform.Domain` phải
compile được kể cả khi ta xoá sạch mọi project khác trong solution.

---

## 1. `.csproj` — bằng chứng của zero-dependency

Đây là file thật của `VNR.Platform.Domain`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>VNR.Platform.Domain</RootNamespace>
    <AssemblyName>VNR.Platform.Domain</AssemblyName>
  </PropertyGroup>

  <!-- KHÔNG có PackageReference — zero NuGet dependencies -->
</Project>
```

Dòng comment cuối **là một phần của thiết kế**, không phải chú thích thừa: nó
tồn tại để người mở PR nhìn thấy trước khi kịp thêm package. Giữ nguyên nó.

> **Ngoại lệ duy nhất được phép:** `System.ComponentModel.DataAnnotations`
> (`[Required]`, `[MaxLength]`) — đây là BCL, không phải hạ tầng. Luật
> "Domain không dính attribute hạ tầng" cấm `[Column]`, `[Table]`,
> `[JsonIgnore]`, `[Key]` — tức những attribute mà **một provider cụ thể**
> (EF Core, System.Text.Json) diễn giải. Ranh giới: attribute mô tả **ràng
> buộc nghiệp vụ** thì được; attribute mô tả **cách lưu/serialize** thì không.

---

## 2. Cây thư mục đích

Đây là inventory thật của `VNR.Platform.Domain`. Cột "P1?" cho biết cái nào
cần ngay ở phase này, cái nào để sau (nhưng vẫn nên biết chỗ nó sẽ nằm).

```
VNR.Platform.Domain/
├── Entities/
│   ├── BaseEntity.cs                    ✅ P1
│   ├── AggregateRoot.cs                 ✅ P1
│   ├── CatalogEntityBase.cs             ✅ P1
│   ├── EntityId.cs                      ✅ P1
│   └── Interfaces/
│       ├── IHasId.cs                    ✅ P1
│       ├── IAuditEntity.cs              ✅ P1
│       ├── ISoftDelete.cs               ✅ P1
│       ├── ICatalogEntity.cs            ✅ P1
│       ├── IActiveStatus.cs             ✅ P1
│       ├── IOptimisticConcurrency.cs    ⏳ khi có màn hình sửa đồng thời
│       ├── IEffectiveDated.cs           ⏳ khi có dữ liệu hiệu lực theo thời gian
│       ├── IChangeTracked.cs            ⏳
│       └── IHasCacheKey.cs              ⏳ khi làm cache versioning
├── Exceptions/
│   ├── DomainException.cs               ✅ P1
│   ├── DomainErrorCodes.cs              ✅ P1
│   ├── BusinessException.cs             ✅ P1
│   └── ConflictException.cs             ✅ P1
├── ValueObjects/
│   ├── ValueObject.cs                   ✅ P1
│   ├── ValueObjectOfT.cs                ✅ P1
│   ├── EmailAddress.cs · PhoneNumber.cs · Money.cs · RangeDate.cs   ✅ P1 (mẫu)
│   └── Address · BankAccount · CitizenId · Passport · Percentage
│       · SocialInsuranceNumber · TaxCode · TimeRange · WorkingHours  ⏳ theo nghiệp vụ
├── Enumerations/
│   ├── Enumeration.cs                   ✅ P1
│   ├── IEnumeration.cs                  ✅ P1
│   ├── EnumerationJsonConverter.cs      ⏳ P2 (cần khi serialize ra API)
│   └── YesNoEnum · CatalogStatusEnum · ApplicationModule · UIModule  ⏳ theo nghiệp vụ
├── Repositories/
│   ├── IGenericRepository.cs            ✅ P1
│   ├── IUnitOfWork.cs                   ✅ P1
│   └── ILegacyQuerySupport.cs           ⏳ chỉ khi migrate từ hệ cũ
├── Events/
│   ├── IDomainEvent.cs                  ✅ P1 (interface rỗng — rẻ, khai luôn)
│   ├── DomainEventBase.cs               ✅ P1
│   └── IHasDomainEvents.cs              ✅ P1
├── Tree/
│   ├── IHasParent.cs · IHasPathCode.cs  ⏳ khi có entity dạng cây
│   └── TreePathHelper.cs · TreeCycleDetector.cs                     ⏳
├── Abstractions/
│   ├── IReferenceReadService.cs         ⏳ P5+ (cross-module reference label)
│   └── IReferenceSummary.cs             ⏳
├── Enums/
│   └── PermissionAction.cs · …          ⏳ P4 (khi làm permission)
├── Resources/
│   ├── ResourceDefinitionModel.cs       ⏳ P4
│   └── Interfaces/IModulePermissionProvider.cs                      ⏳ P4
├── Security/
│   └── RequirePermissionAttribute.cs    ⏳ P4
└── GlobalUsings.cs                      ✅ P1 (cực ngắn — xem P0 §4)
```

**Đếm nhanh:** P1 cần đúng **~24 file**. Con số này quan trọng — nếu bạn thấy
mình đang viết file thứ 60 ở P1, gần như chắc chắn đang kéo nghiệp vụ vào
Domain.

---

## 3. Thứ tự viết — 6 nhóm, đúng thứ tự phụ thuộc

```
D1 Entity interfaces  ──► D2 Entity base classes
                                  │
D3 Exceptions ────────────────────┼──► D4 Value Objects
                                  │
                                  ├──► D5 Enumeration
                                  │
                                  └──► D6 Repository abstraction + Domain events
```

`D3` phải xong trước `D4` vì mọi Value Object đều throw `DomainException` khi
invariant vỡ. Đây là lý do đội mới hay viết VO trước rồi phải sửa lại toàn bộ:
họ throw `ArgumentException` ở lần đầu, rồi phát hiện `ArgumentException` không
map được sang mã lỗi 422 có `businessCode`.

---

## 4. D1 — Entity interfaces (viết trước base class)

Base class chỉ là **tổ hợp** của các interface. Viết interface trước để
`GenericRepository` và interceptor sau này ràng buộc theo *khả năng* chứ không
theo *class cụ thể*.

```csharp
// Interfaces/IHasId.cs
/// Cho phép GenericRepository truy cập Id type-safe thay vì magic string.
public interface IHasId<TKey> where TKey : IEquatable<TKey>
{
    TKey Id { get; }
}

// Interfaces/IAuditEntity.cs — 4 field audit, KHÔNG phải 2
public interface IAuditEntity
{
    string? UserCreate { get; set; }
    string? UserUpdate { get; set; }
    DateTimeOffset? DateCreate { get; set; }
    DateTimeOffset? DateUpdate { get; set; }
}

// Interfaces/ISoftDelete.cs
public interface ISoftDelete
{
    bool IsDelete { get; set; }
}

// Interfaces/ICatalogEntity.cs — bộ ba của mọi bảng danh mục
public interface ICatalogEntity
{
    string Code { get; set; }
    string Name { get; set; }
    string? Description { get; set; }
}

// Interfaces/IActiveStatus.cs
public interface IActiveStatus
{
    bool IsActive { get; set; }
}
```

### Vì sao `get; set;` chứ không `get;` cho audit/soft-delete

Vì `UnitOfWork.SaveChangesAsync()` và interceptor ở tầng Persistence phải
**ghi** vào chúng. Nếu để read-only, Persistence buộc phải dùng reflection hoặc
shadow property → phức tạp hơn nhiều so với cái nó bảo vệ. Ngược lại `IHasId`
chỉ có `get;` vì không ai được đổi Id sau khi tạo.

Đây là một đánh đổi có chủ đích, không phải cẩu thả: **encapsulation nhường
chỗ cho cross-cutting concern ở đúng 5 field kỹ thuật, và chỉ 5 field đó.**
Field nghiệp vụ trong `AggregateRoot` vẫn phải private-set.

### Ba interface để sau, nhưng cần hiểu ngay bây giờ

**`IOptimisticConcurrency` — marker rỗng, không có property:**

```csharp
public interface IOptimisticConcurrency;
```

Không có `RowVersion`/`Version` trong Domain là **cố ý**. Token concurrency là
concern của provider: PostgreSQL dùng shadow `uint "xmin"`, SQL Server dùng
shadow `byte[] "RowVersion"`. Nó được `ConcurrencyTokenConvention` ở tầng
Persistence bơm vào dưới dạng **shadow property**. Entity chỉ khai "tôi cần
kiểm soát đồng thời", không khai "kiểm soát bằng cột gì".

> Đây là một mẫu đáng học và tái dùng: **interface marker rỗng ở Domain +
> convention ở Persistence**. Nó cho phép đổi provider mà không đụng một dòng
> entity nào.

**`IEffectiveDated` — dữ liệu có hiệu lực theo thời gian:**

```csharp
public interface IEffectiveDated
{
    DateTimeOffset? EffectiveFrom { get; }
    DateTimeOffset? EffectiveTo { get; }
}
```

Quy ước biên: `From` **đóng** (`>=`), `To` **mở** (`<`). Quan trọng hơn bản
thân interface: đi kèm nó phải có **một chỗ duy nhất** so sánh biên —
`EffectiveDatedExtensions.IsEffectiveAt(asOf)` và `IsValidPeriod(from, to)`.
Không có nó, mỗi handler tự viết `x.From <= now && x.To >= now` và một nửa số
chỗ sẽ sai dấu `=` ở biên `To`.

---

## 5. D2 — Entity base classes

### `BaseEntity<TId>` — gốc của mọi entity

```csharp
using System.ComponentModel.DataAnnotations;
using VNR.Platform.Domain.Entities.Interfaces;

namespace VNR.Platform.Domain.Entities;

public abstract class BaseEntity<TId> : IAuditEntity, ISoftDelete, IHasId<TId>
    where TId : IEquatable<TId>
{
    [Required] public TId Id { get; set; } = default!;

    [MaxLength(50)] public string? UserCreate { get; set; }
    [MaxLength(50)] public string? UserUpdate { get; set; }

    public DateTimeOffset? DateCreate { get; set; }
    public DateTimeOffset? DateUpdate { get; set; }

    public bool IsDelete { get; set; } = false;
}

/// Shortcut cho BaseEntity<Guid> — dùng cho 90% entity.
public abstract class BaseEntity : BaseEntity<Guid> { }
```

Ba quyết định cần hiểu:

| Quyết định | Lý do |
| --- | --- |
| `DateTimeOffset?` chứ không `DateTime` | `DateTime` không mang offset → khi hệ thống chạy nhiều timezone hoặc container UTC vs máy local, bản ghi tạo lúc nào trở thành câu hỏi không trả lời được. Sửa sau = migration toàn bộ bảng |
| Audit field **nullable** | Bản ghi seed/migration không có user nào tạo. Ép `NOT NULL` → phải bịa `"system"` ở 30 chỗ |
| Có class không-generic `BaseEntity : BaseEntity<Guid>` | 90% entity dùng `Guid`. Không có shortcut thì mọi entity phải viết `BaseEntity<Guid>` — nhiễu thị giác vô ích |

### `AggregateRoot<TId>` — entity có invariant thật

```csharp
public abstract class AggregateRoot<TId> : BaseEntity<TId>, IHasDomainEvents
    where TId : IEquatable<TId>
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void AddDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents() => _domainEvents.Clear();
}

public abstract class AggregateRoot : AggregateRoot<Guid>;
```

**Ràng buộc đi kèm (không nằm trong code, phải viết vào rule):** class kế thừa
`AggregateRoot` **KHÔNG expose public setter cho field nghiệp vụ**. Entity tự
enforce invariant qua factory method + mutation method:

```csharp
public sealed class Company : AggregateRoot
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }

    private Company() { }                       // EF cần ctor không tham số

    public static Company Create(string code, string name)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("COMPANY.CODE_REQUIRED", "Mã công ty bắt buộc.");

        return new Company { Id = EntityId.New(), Code = code.Trim(), Name = name.Trim(), IsActive = true };
    }

    public void Deactivate()
    {
        if (!IsActive) return;                  // idempotent
        IsActive = false;
        AddDomainEvent(new CompanyDeactivatedEvent(Id));
    }
}
```

### `CatalogEntityBase<TId>` — cực đối lập, cố ý anemic

```csharp
public abstract class CatalogEntityBase<TId> : BaseEntity<TId>, ICatalogEntity, IActiveStatus
    where TId : IEquatable<TId>
{
    [MaxLength(50)]  public string Code { get; set; } = string.Empty;
    [MaxLength(255)] public string Name { get; set; } = string.Empty;
    [MaxLength(500)] public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}
```

`public set` ở đây **không phải sai sót**. Đây là nửa còn lại của nguyên tắc
"chỉ 2 cực, không có tầng giữa" (xem [00 §1](00-lo-trinh-tong-the.md)):

| | `AggregateRoot` | `CatalogEntityBase` |
| --- | --- | --- |
| Có invariant nghiệp vụ? | Có | Không — chỉ là bảng tra cứu |
| Setter | `private set` + mutation method | `public set` |
| Ai tạo/sửa? | Factory + method của chính entity | Generic CRUD handler, không cần handler riêng |
| Domain event | Có | Không |
| Chi phí 1 entity mới | ~15–20 file | ~3 file |

Chọn sai cực rất đắt. Quy tắc phân loại: **nếu tồn tại bất kỳ câu "khi X thì
không được Y" nào về entity này → `AggregateRoot`. Nếu không → catalog.**

### `EntityId` — một dòng, nhưng là điểm đổi duy nhất

```csharp
public static class EntityId
{
    public static Guid New() => Guid.NewGuid();
}
```

Toàn hệ thống sinh Id qua đây, **không ai gọi `Guid.NewGuid()` trực tiếp**.
Lý do: `Guid.NewGuid()` sinh Guid ngẫu nhiên → index B-tree phân mảnh nặng khi
bảng lớn. Khi nâng .NET 9 (`Guid.CreateVersion7()`) hoặc thêm thư viện UUIDv7,
ta chỉ sửa **một dòng** thay vì grep toàn repo.

> Đây là mẫu "seam một dòng" — chi phí bằng 0 lúc viết, giá trị rất lớn lúc
> cần đổi. Áp dụng cho cả `DateTimeOffset.UtcNow` (bọc thành `IClock`) nếu
> muốn test được logic phụ thuộc thời gian.

---

## 6. D3 — Exceptions

```csharp
// Exceptions/DomainException.cs
public class DomainException : Exception
{
    public const string DefaultCode = "DOMAIN.INVARIANT_VIOLATED";

    public string Code { get; }

    public DomainException(string message) : base(message) => Code = DefaultCode;
    public DomainException(string code, string message) : base(message) => Code = code;
    public DomainException(string message, Exception inner) : base(message, inner) => Code = DefaultCode;
}
```

`Code` là thứ khiến exception này khác `InvalidOperationException`. Pipeline
`ExceptionHandlingBehavior` (P2) bắt nó, đổ `Code` vào `IApiResult.BusinessCode`
và trả **HTTP 422**. FE nhận được mã ổn định để hiển thị message i18n, thay vì
phải parse chuỗi tiếng Việt.

> Domain **không** reference được `ErrorDescriptor` (nằm ở Application) — nên
> `DomainException` là dạng "coded error" tối giản, đủ để mang mã đi qua ranh
> giới layer.

```csharp
// Exceptions/DomainErrorCodes.cs — hằng tập trung một chỗ
public static class DomainErrorCodes
{
    public const string EmailRequired      = "VO.EMAIL_REQUIRED";
    public const string EmailInvalid       = "VO.EMAIL_INVALID";
    public const string PhoneInvalid       = "VO.PHONE_INVALID";
    public const string MoneyNegative      = "VO.MONEY_NEGATIVE";
    public const string MoneyCurrencyInvalid = "VO.MONEY_CURRENCY_INVALID";
    public const string RangeDateInvalid   = "VO.RANGE_DATE_INVALID";
    // … VNR.Successor có 25 mã, tất cả prefix VO.*, UPPER_SNAKE
}
```

Quy ước: **KHÔNG hardcode string literal ở từng VO.** Một file hằng cho toàn bộ
mã lỗi Domain. Lợi ích thật (không phải lý thuyết): khi làm i18n ở P6, bạn cần
danh sách đầy đủ mã lỗi để dịch — có file này thì là 1 lần đọc, không có thì
phải grep `throw new DomainException` khắp repo và vẫn sót.

```csharp
// BusinessException.cs / ConflictException.cs — cả hai đều là Exception 2-ctor thuần
public class BusinessException : Exception { /* (message) và (message, inner) */ }
public class ConflictException : Exception { /* (message) và (message, inner) */ }
```

`ConflictException` map sang **HTTP 409**, và điểm quan trọng: nó được **dịch
từ Infrastructure lên** — `BaseDbContext.SaveChangesAsync` bắt
`DbUpdateException` bọc lỗi PostgreSQL `23505` (unique violation) rồi ném lại
thành `ConflictException`. Handler không phải biết mã lỗi của Postgres.

### Bảng ánh xạ 3 exception (chốt ở P1, dùng ở P4)

| Exception | HTTP | Ai ném | Ý nghĩa |
| --- | --- | --- | --- |
| `DomainException` | 422 | Entity / Value Object | Vi phạm invariant nghiệp vụ, có `Code` |
| `BusinessException` | 400/422 | Handler | Quy tắc nghiệp vụ ở tầng use-case |
| `ConflictException` | 409 | Persistence (dịch từ DB) | Trùng khoá, xung đột đồng thời |

---

## 7. D4 — Value Objects

### Base class

```csharp
// ValueObjects/ValueObject.cs
public abstract class ValueObject
{
    protected abstract IEnumerable<object?> GetEqualityComponents();

    public override bool Equals(object? obj)
    {
        if (obj is null || obj.GetType() != GetType()) return false;
        return GetEqualityComponents().SequenceEqual(((ValueObject)obj).GetEqualityComponents());
    }

    public override int GetHashCode()
        => GetEqualityComponents()
            .Select(x => x?.GetHashCode() ?? 0)
            .Aggregate((a, b) => a ^ b);

    public static bool operator ==(ValueObject? a, ValueObject? b) => Equals(a, b);
    public static bool operator !=(ValueObject? a, ValueObject? b) => !Equals(a, b);
}
```

Class dẫn xuất chỉ phải viết `GetEqualityComponents()` — 3 dòng — và có ngay
value equality đúng. Đây là toàn bộ lý do base class này tồn tại.

### Hình dạng chuẩn của một VO (mẫu `EmailAddress`)

```csharp
public class EmailAddress : ValueObject
{
    public string Value { get; }

    private EmailAddress() { Value = string.Empty; }   // ① EF Core cần

    public EmailAddress(string value)                   // ② Cửa duy nhất để tạo
    {
        if (!IsValid(value))
            throw new DomainException(DomainErrorCodes.EmailInvalid, "Invalid email address");
        Value = value.Trim().ToLowerInvariant();         // ③ Chuẩn hoá TRONG ctor
    }

    public static bool IsValid(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException(DomainErrorCodes.EmailRequired, "Email is required");
        return Regex.IsMatch(value, @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$");
    }

    protected override IEnumerable<object?> GetEqualityComponents() { yield return Value; }

    public override string ToString() => Value;
}
```

Bốn điểm phải copy đúng ở **mọi** VO:

1. **`private` parameterless ctor** — EF Core materialize object không qua
   public ctor. Thiếu nó → runtime error lúc query, không phải lúc build.
2. **Không có setter public** — VO bất biến. Muốn đổi thì tạo cái mới.
3. **Chuẩn hoá trong ctor** (`Trim`, `ToLowerInvariant`) — nếu để bên ngoài
   chuẩn hoá thì `new EmailAddress("A@b.com") != new EmailAddress("a@b.com")`
   và cache/so sánh sẽ sai một cách rất khó tìm.
4. **Throw `DomainException` với mã từ `DomainErrorCodes`** — không dùng
   `ArgumentException`.

### `ValueObject<TValue>` — biến thể "enum kiểu VO"

```csharp
public class ValueObject<TValue> : ValueObject, IEquatable<ValueObject<TValue>>
{
    protected ValueObject() { }

    public TValue? Value { get; protected set; }

    public static IEnumerable<T> GetAll<T>() where T : ValueObject<TValue>
        => typeof(T).GetFields(BindingFlags.Public | BindingFlags.Static)
                    .Where(f => f.FieldType == typeof(T))
                    .Select(f => (T)f.GetValue(null)!);

    // + implicit conversion 2 chiều, ToString()
}
```

Dùng cho tập giá trị cố định nhưng cần logic đi kèm. **Đừng nhầm với
`Enumeration<TEnum>` ở §8** — ranh giới: nếu tập giá trị cần lưu xuống DB và
hiển thị nhãn i18n → `Enumeration`. Nếu chỉ là kiểu chặt trong bộ nhớ →
`ValueObject<T>`.

### Hai VO nên có sớm dù chưa cần: `Money` và `RangeDate`

```csharp
public class Money : ValueObject
{
    public decimal Amount { get; }
    public string CurrencyCode { get; }

    public Money(decimal amount, string currencyCode = "VND")
    {
        if (amount < 0)
            throw new DomainException(DomainErrorCodes.MoneyNegative, "Amount cannot be negative.");
        if (string.IsNullOrWhiteSpace(currencyCode) || currencyCode.Length != 3)
            throw new DomainException(DomainErrorCodes.MoneyCurrencyInvalid, "Currency code must be ISO 4217 (3 letters).");

        Amount = Math.Round(amount, 2);
        CurrencyCode = currencyCode.ToUpperInvariant();
    }

    public static Money Zero(string currencyCode = "VND") => new(0, currencyCode);

    public Money Add(Money other)      { AssertSameCurrency(other); return new(Amount + other.Amount, CurrencyCode); }
    public Money Subtract(Money other) { AssertSameCurrency(other); return new(Amount - other.Amount, CurrencyCode); }
    public Money Multiply(decimal f)   => new(Amount * f, CurrencyCode);
    // AssertSameCurrency → throw InvalidOperationException nếu khác tiền tệ
}
```

Giá trị thật của `Money` không phải "gói 2 field lại": nó khiến **cộng hai
loại tiền khác nhau trở thành lỗi runtime rõ ràng** thay vì một con số sai âm
thầm. `decimal` + `Math.Round(2)` cũng loại bỏ lớp bug làm tròn của `double`.

```csharp
public class RangeDate : ValueObject
{
    public DateTimeOffset? StartDate { get; }
    public DateTimeOffset? EndDate { get; }

    public RangeDate(DateTimeOffset? startDate, DateTimeOffset? endDate)
    {
        if (startDate.HasValue && endDate.HasValue && startDate > endDate)
            throw new DomainException(DomainErrorCodes.RangeDateInvalid, "StartDate must be ≤ EndDate.");
        StartDate = startDate; EndDate = endDate;
    }

    public bool Contains(DateTimeOffset date)
        => (StartDate is null || date >= StartDate) && (EndDate is null || date <= EndDate);

    public bool Overlaps(RangeDate other) { /* null → MinValue/MaxValue rồi so sánh giao */ }

    public TimeSpan? Duration => StartDate.HasValue && EndDate.HasValue ? EndDate - StartDate : null;
    public static RangeDate Empty => new(null, null);
}
```

`Overlaps` là lý do chính nên có VO này sớm: **logic "hai khoảng thời gian có
giao nhau không" bị viết sai ở hầu hết codebase** (thường quên trường hợp
open-ended). Viết đúng một lần ở đây.

> **Ghi chú EF Core 8:** VO map bằng `ComplexProperty` — inline cùng bảng chủ,
> **không** sinh bảng mới. Cấu hình ở tầng Persistence (P3), Domain không biết.

---

## 8. D5 — `Enumeration<TEnum>` (thay cho C# `enum`)

### Vì sao không dùng `enum` thuần

| Vấn đề của `enum` | `Enumeration<TEnum>` giải quyết thế nào |
| --- | --- |
| Lưu `int` xuống DB → đọc raw SQL không hiểu `3` là gì | Lưu **`Name` (string)**, đọc DB là hiểu ngay |
| Chèn giá trị giữa chừng → lệch toàn bộ dữ liệu cũ | Thêm field mới, số thứ tự không ảnh hưởng |
| Không gắn được nhãn i18n / mô tả | Có `ResourceKey`, `Text`, `Description` |
| Không thêm được hành vi | Là class — thêm method thoải mái |
| Không biểu diễn được "giá trị suy diễn" | Có cờ `IsComputed` |

### Chữ ký

```csharp
public abstract class Enumeration<TEnum>
    : IEnumeration, IEquatable<Enumeration<TEnum>>, IComparable<Enumeration<TEnum>>
    where TEnum : Enumeration<TEnum>          // CRTP — TEnum tự tham chiếu chính nó
{
    public int     Id          { get; }
    public string  Name        { get; }
    public string  ResourceKey { get; }       // mặc định "{TênEnum}.{Name}"
    public string? Text        { get; }
    public string? Description { get; }
    public bool    IsComputed  { get; }

    protected Enumeration(int id, string name, string? text = null,
                          string? description = null, string? resourceKey = null,
                          bool isComputed = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        // ResourceKey = resourceKey ?? $"{typeof(TEnum).Name}.{name}"
    }

    // Tra cứu — dùng ở EF ValueConverter / seed / internal
    public static IReadOnlyCollection<TEnum> GetAll();
    public static IReadOnlyCollection<string> GetNonComputedNames();
    public static TEnum FromId(int id);        // ❗ throw nếu không thấy
    public static TEnum FromName(string name); // ❗ throw nếu không thấy

    // Tra cứu — dùng ở HANDLER khi parse input từ FE
    public static bool TryFromId(int id, out TEnum? result);
    public static bool TryFromName(string name, out TEnum? result);
    public static TEnum? TryFromNameOrDefault(string? name);
}
```

### 🔴 Luật dùng `From*` vs `TryFrom*` — chỗ sai phổ biến nhất

```csharp
// ❌ SAI — FE gửi rác → InvalidOperationException → HTTP 500
var status = CatalogStatusEnum.FromName(request.Status);

// ✅ ĐÚNG — FE gửi rác là lỗi 422 của người dùng, không phải sự cố hệ thống
if (!CatalogStatusEnum.TryFromName(request.Status, out var status))
    return Fail<Response>(CompanyErrors.InvalidStatus);
```

`FromName`/`FromId` throw `InvalidOperationException` có liệt kê giá trị hợp lệ
— tiện cho developer, **thảm hoạ khi lộ ra API**. Quy tắc một câu: **input
đến từ ngoài hệ thống → luôn `TryFrom*`.**

### Chi tiết triển khai đáng chú ý

- **3 cache `Lazy<>`**: `_all` (nạp qua reflection `LoadAll()`), `_byId`,
  `_byName` với `StringComparer.OrdinalIgnoreCase`. Reflection chỉ chạy **một
  lần** cho mỗi kiểu.
- `LoadAll()` quét `BindingFlags.Public | Static | DeclaredOnly` các field có
  kiểu `TEnum` → khai giá trị bằng `public static readonly` field.
- **`IsComputed = true`** đánh dấu giá trị **không tồn tại trong DB**, chỉ là
  bộ lọc suy diễn. Ví dụ thật: `UserStatusEnum.NoAccount` tương ứng điều kiện
  `u.ProfileId IS NULL`. Vì vậy mới cần `GetNonComputedNames()` khi sinh
  constraint/seed.
- `IEnumeration` (bản không generic) tồn tại để reflection và serialization
  đọc được mà không cần biết `TEnum`:

  ```csharp
  public interface IEnumeration
  {
      int Id { get; } string Name { get; } string ResourceKey { get; }
      string? Text { get; } string? Description { get; }
  }
  ```

### Cách khai một enumeration cụ thể

```csharp
public sealed class CatalogStatusEnum : Enumeration<CatalogStatusEnum>
{
    public static readonly CatalogStatusEnum Draft    = new(1, nameof(Draft));
    public static readonly CatalogStatusEnum Active   = new(2, nameof(Active));
    public static readonly CatalogStatusEnum Archived = new(3, nameof(Archived));

    private CatalogStatusEnum(int id, string name) : base(id, name) { }
}
```

---

## 9. D6 — Repository abstraction & Domain events

### `IGenericRepository<TEntity, TKey>`

Điểm mấu chốt: **mọi method trả về `List<T>` / `T` / giá trị thô — KHÔNG bao
giờ trả `IQueryable`.** Đây chính là cơ chế khiến `Application` không cần biết
EF Core tồn tại. Trả `IQueryable` là cách phổ biến nhất để layer rule chết mà
ArchTest vẫn xanh.

```csharp
public interface IGenericRepository<TEntity, TKey>
    where TEntity : class, IHasId<TKey>
    where TKey : IEquatable<TKey>
{
    // ── Single entity ──────────────────────────────────────────
    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken ct = default);
    Task<bool>     ExistsAsync(TKey id, CancellationToken ct = default);   // SQL EXISTS, không load
    Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default);

    // ── Query đã materialize ───────────────────────────────────
    Task<List<TEntity>> GetAllAsync(CancellationToken ct = default);
    Task<List<TEntity>> FindAllAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default);
    Task<List<TEntity>> FindAllNoTrackingAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default);
    Task<List<TResult>> FindAllAsync<TResult>(Expression<Func<TEntity, bool>> predicate,
                                              Expression<Func<TEntity, TResult>> selector, CancellationToken ct = default);
    Task<List<TResult>> FindAllNoTrackingAsync<TResult>(…);
    Task<List<TResult>> FindAllIgnoringFiltersAsync<TResult>(…);           // 🔴 xem cảnh báo dưới
    Task<List<TEntity>> FindAllWithIncludesAsync(Expression<Func<TEntity, bool>> predicate,
                                                 CancellationToken ct = default,
                                                 params Expression<Func<TEntity, object>>[] includes);
    Task<List<TEntity>> GetByIdsAsync(IEnumerable<TKey> ids, CancellationToken ct = default);      // giới hạn cứng 1000
    Task<List<TEntity>> GetByIdsBatchAsync(IEnumerable<TKey> ids, CancellationToken ct = default); // tự chia lô 500
    Task<int>  CountAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken ct = default);
    Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default);

    // ── Mutation — KHÔNG method nào gọi SaveChanges ────────────
    Task AddAsync(TEntity entity, CancellationToken ct = default);
    Task UpdateAsync(TEntity entity, CancellationToken ct = default);
    Task DeleteAsync(TEntity entity, CancellationToken ct = default);      // soft-delete nếu ISoftDelete
    Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default);
    Task UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default);
    Task DeleteRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default);
}
```

**🔴 `FindAllIgnoringFiltersAsync` — method nguy hiểm nhất, và vì sao vẫn phải có.**
Global query filter tự loại bản ghi `IsDelete = true`. Hệ quả: mọi phép kiểm
tra tồn tại đi qua đường lọc đều kết luận **SAI** là "chưa có", rồi `INSERT`
trùng khoá chính. Dùng method này **chỉ khi câu hỏi thuộc về khoá chính, không
phải nghiệp vụ** — ví dụ "mã `ABC` đã từng tồn tại chưa (kể cả đã xoá mềm)".

**`GetByIdsAsync` giới hạn cứng 1000, `throw ArgumentException` nếu vượt.**
Không phải bảo thủ: `WHERE Id IN (…)` với 50.000 phần tử làm SQL Server/Postgres
sinh query plan khổng lồ rồi timeout. Ai cần nhiều hơn → dùng
`GetByIdsBatchAsync` (tự chia lô 500). Giới hạn **bằng exception** chứ không
bằng comment, vì comment không chặn được ai.

### `IUnitOfWork` — ai sở hữu `SaveChanges`

```csharp
public interface IUnitOfWork
{
    Task<int>  SaveChangesAsync(CancellationToken ct = default);   // tự điền audit field
    Task       ExecuteInTransactionAsync(Func<Task> operation, CancellationToken ct = default);
    Task<T>    ExecuteInTransactionAsync<T>(Func<Task<T>> operation, CancellationToken ct = default);
}
```

Luật đi kèm, phải viết vào `.claude/rules/`:

> **Repository KHÔNG BAO GIỜ gọi `SaveChanges`. Handler gọi, đúng một lần, ở
> cuối.** Repository gọi `SaveChanges` = mỗi thao tác một transaction riêng →
> không thể rollback nhóm, và "tạo Company rồi tạo Department con" có thể để
> lại Company mồ côi.

Hai overload `ExecuteInTransactionAsync` đều bọc **execution strategy** của EF
Core (retry khi mất kết nối tạm thời). Đây là lý do phải dùng nó thay vì
`BeginTransaction` thủ công: `BeginTransaction` + retry strategy sẽ throw
"The configured execution strategy does not support user-initiated transactions".

### Domain events — 3 file, viết luôn dù chưa dùng

```csharp
public interface IDomainEvent { }             // KHÔNG extends MediatR.INotification

public abstract class DomainEventBase : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public interface IHasDomainEvents
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
    void AddDomainEvent(IDomainEvent domainEvent);
    void ClearDomainEvents();
}
```

`IDomainEvent` **cố ý không kế thừa `MediatR.INotification`** — kế thừa là kéo
package MediatR vào Domain, phá zero-dependency. Cầu nối sang `INotification`
nằm ở `Platform.Application` (P2), và việc dispatch xảy ra **sau
`SaveChangesAsync()`** qua `DomainEventInterceptor` (P3).

> ⚠️ Trong VNR.Successor, cơ chế domain event đang có lỗi đã biết (ticket
> PLT-003) và `CLAUDE.md` của họ ghi rõ "don't use in production". Bài học rút
> ra: **khai 3 interface này ở P1 (rẻ, không rủi ro), nhưng đừng xây pipeline
> dispatch cho tới khi có use-case thật.**

---

## 10. Những gì **KHÔNG** được đưa vào `Platform.Domain`

| Thứ | Vì sao không | Nó thuộc về đâu |
| --- | --- | --- |
| `DbContext`, `IEntityTypeConfiguration`, `[Column]`, `[Table]` | Chi tiết lưu trữ | `Platform.Persistence` (P3) |
| `IApiResult<T>`, `ErrorDescriptor`, `ErrorCode` | Hình dạng phản hồi = concern ứng dụng | `Platform.Application` (P2) |
| `ICommand`, `IQuery`, handler, `MediatR` | Điều phối use-case | `Platform.Application` (P2) |
| `ICurrentUser`, `IDateTimeProvider` | Là abstraction hạ tầng, không phải khái niệm nghiệp vụ | `Platform.Application` khai, Infrastructure implement |
| DTO / Request / Response | Hợp đồng với bên ngoài | `Module.*.Contracts` |
| FluentValidation validator | Validate input, khác invariant nghiệp vụ | `Module.*.Application` |
| Entity nghiệp vụ cụ thể (`Company`, `User`) | Domain **platform** chỉ chứa cái dùng chung | `Module.*.Domain` |

Câu hỏi kiểm tra nhanh khi phân vân: **"Khái niệm này có tồn tại nếu ta bỏ hết
database, HTTP và framework đi không?"** Có → Domain. Không → chỗ khác.

---

## 11. ArchTest cho P1

Bổ sung vào file `LayerDependencyTests` đã tạo ở P0:

```csharp
[Fact]
public void Domain_MustNotDependOn_AnyOtherLayer()
{
    var result = Types.InAssembly(typeof(BaseEntity<>).Assembly)
        .Should().NotHaveDependencyOnAny(
            "Microsoft.EntityFrameworkCore",
            "Microsoft.AspNetCore",
            "MediatR",
            "FluentValidation",
            "VNR.Platform.Application",
            "VNR.Platform.Persistence")
        .GetResult();

    result.IsSuccessful.Should().BeTrue(
        $"Domain bị nhiễm: {string.Join(", ", result.FailingTypeNames ?? [])}");
}

[Fact]
public void Domain_Assembly_MustHave_ZeroPackageReference()
{
    // Kiểm tra ở mức assembly: ngoài System.* và chính nó, không được ref gì
    var referenced = typeof(BaseEntity<>).Assembly
        .GetReferencedAssemblies()
        .Select(a => a.Name!)
        .Where(n => !n.StartsWith("System") && !n.StartsWith("netstandard")
                    && n != "VNR.Platform.Domain")
        .ToList();

    referenced.Should().BeEmpty($"Domain đang ref: {string.Join(", ", referenced)}");
}

[Fact]
public void AggregateRoot_Descendants_MustNotHave_PublicSetter()
{
    var violations = typeof(BaseEntity<>).Assembly.GetTypes()
        .Where(t => t.IsClass && !t.IsAbstract && IsAggregateRoot(t))
        .SelectMany(t => t.GetProperties())
        .Where(p => p.SetMethod?.IsPublic == true && !IsAuditOrIdField(p.Name))
        .Select(p => $"{p.DeclaringType!.Name}.{p.Name}")
        .ToList();

    violations.Should().BeEmpty(
        $"AggregateRoot phải private-set field nghiệp vụ: {string.Join(", ", violations)}");
}
```

Test thứ hai là cái đáng giá nhất: nó bắt **mọi** package bị thêm vào Domain,
kể cả package chưa có trong danh sách cấm của test thứ nhất.

Test thứ ba chỉ áp cho `AggregateRoot`, **không** áp cho `CatalogEntityBase` —
đúng theo nguyên tắc 2 cực. Nếu viết nhầm thành áp cho mọi entity, bạn sẽ tự
phá pattern catalog của chính mình.

---

## 12. Checklist rời P1

- [ ] `Platform.Domain.csproj` **không có** `PackageReference` nào
- [ ] `GlobalUsings.cs` chỉ có `System` + `System.Collections.Generic`
- [ ] Đủ 5 interface entity: `IHasId`, `IAuditEntity`, `ISoftDelete`, `ICatalogEntity`, `IActiveStatus`
- [ ] `BaseEntity<TId>` + `BaseEntity` + `AggregateRoot<TId>` + `AggregateRoot` + `CatalogEntityBase<TId>`
- [ ] `EntityId.New()` tồn tại và **không nơi nào gọi `Guid.NewGuid()` trực tiếp** (grep để chắc)
- [ ] 4 exception + `DomainErrorCodes`, **không** literal string mã lỗi nào nằm rải rác
- [ ] `ValueObject` + `ValueObject<T>` + ít nhất 1 VO thật để kiểm chứng pattern
- [ ] `Enumeration<TEnum>` + `IEnumeration`, và rule `TryFrom*` cho input ngoài đã ghi vào `.claude/rules/entity-domain.md`
- [ ] `IGenericRepository` + `IUnitOfWork` — **không method nào trả `IQueryable`**
- [ ] 3 file domain event (interface thôi, chưa dispatch)
- [ ] 3 ArchTest §11 xanh, và **đã kiểm chứng bằng cách làm chúng đỏ**
- [ ] Bảng "không đưa vào Domain" §10 đã chép vào `.claude/rules/entity-domain.md`

---

> Tiếp theo: [03-p2-platform-application.md](03-p2-platform-application.md) —
> nơi định nghĩa `ICommand`/`IQuery`, 4 pipeline behavior, `IApiResult<T>` và
> `ErrorDescriptor`. Đó cũng là nơi cầu nối `IDomainEvent → INotification`
> được đặt.

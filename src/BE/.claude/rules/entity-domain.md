# Entity & Domain — src/BE

## Base entity

```csharp
// Domain/Common/BaseEntity.cs
public abstract class BaseEntity
{
    public Guid Id { get; protected set; }
    public DateTimeOffset CreatedAt { get; protected set; }
    public DateTimeOffset? UpdatedAt { get; protected set; }
    public bool IsDeleted { get; protected set; }
}
```

- Setter **`protected`** — không public setter trên entity nghiệp vụ.
- `IsDeleted`: soft delete, filter bằng EF global query filter khai trong
  `DbContext.OnModelCreating` — không tự thêm `.Where(x => !x.IsDeleted)` ở
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
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    public void UpdateScore(decimal maxScore)
    {
        if (maxScore <= 0)
            throw new DomainException("CRITERIA_MAX_SCORE_INVALID", "Điểm tối đa phải > 0.");
        MaxScore = maxScore;
        UpdatedAt = DateTimeOffset.UtcNow;
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
`Result<T>.Validation(...)` — không để `DomainException` lọt thẳng ra
`Api` layer thành lỗi 500 chung chung.

## Khi thêm entity mới

1. Đối chiếu `doc/ERD/example_db_ver1.csv` nếu liên quan tới domain "theo
   dõi tiêu chí" — nhưng nhớ đây là **dữ liệu mẫu**, không phải schema đã
   chốt (xem `src/BE/CLAUDE.md`).
2. Entity ở `Domain/Entities/{Name}.cs`.
3. EF configuration ở `Infrastructure/Persistence/Configurations/{Name}Configuration.cs`.
4. `DbSet<{Name}>` trong `DbContext`.
5. Migration: `dotnet ef migrations add Add{Name}` — đọc lại file migration
   sinh ra trước khi tin, đừng chạy `database update` mà không xem trước.

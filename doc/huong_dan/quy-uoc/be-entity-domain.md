---
kind: luat
scope: core
verified: 2026-09-06
---

# Entity & Domain — src/BE

## Base entity

**Đã CHỐT (2026-08-15):** theo
`doc/tham-khao-ngoai/vnr-successor/02-p1-platform-domain.md §5` —
`BaseEntity` có **6 field kỹ thuật**: `Id` là `init` (không ai gán từ ngoài sau khi
khởi tạo) và **5 field audit** dùng `public get; set;`. Cả sáu **khác** với field nghiệp
vụ của entity con (luôn `private set`, xem mục Factory method bên dưới).

> *(Sửa 2026-09-06: bản trước ghi *"dùng `public get; set;` cho đúng 6 field"* — gộp `Id`
> vào nhóm setter công khai, mâu thuẫn với chính khối code ngay dưới và với gạch đầu dòng
> "Setter **`public`** cho đúng **5 field audit**". Không sai nghiêm trọng, nhưng đây là
> chỗ đúng để đọc nhầm thành "gán `Id` từ ngoài cũng được".)*

```csharp
// Domain/Common/BaseEntity.cs
public abstract class BaseEntity
{
    // Danh tính: sinh NGAY lúc khởi tạo, không ai gán từ ngoài
    public Guid Id { get; init; } = EntityId.New();   // EntityId.New() => Guid.CreateVersion7()

    // 5 field audit: AuditInterceptor ghi lúc SaveChangesAsync
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}
```

- **`Id` là `init`, sinh bằng `Guid.CreateVersion7()`** — entity có danh tính
  hợp lệ ngay từ lúc khởi tạo. Không sinh Id bằng interceptor lúc
  `SaveChangesAsync`: làm vậy thì entity mang `Guid.Empty` trong suốt khoảng từ
  khi tạo tới khi lưu, và mọi thứ cần Id **trước** đó đều vỡ — gắn quan hệ giữa
  hai entity mới, phát domain event, trả Id về cho caller.
  Dùng **v7** chứ không phải `Guid.NewGuid()` (v4): v7 tăng dần theo thời gian
  nên khoá chính không làm phân mảnh trang index B-tree — vấn đề thật ở bảng
  ghi nhiều.
- Setter **`public`** cho đúng **5 field audit** — **không phải sơ suất**.
  `AuditInterceptor` (tầng Infrastructure, chạy trong `SaveChangesAsync`) phải
  **ghi** được các field này từ bên ngoài entity; nếu để `protected`/`private`,
  interceptor phải dùng reflection hoặc shadow property — phức tạp hơn nhiều so
  với cái nó bảo vệ. Đánh đổi này **không** mở rộng sang field nghiệp vụ, và
  **không** áp cho `Id`.

> **✅ CÓ THẬT (đối chiếu 2026-08-28) — code đã theo.** Bộ tên cũ
> `UserCreate`/`UserUpdate`/`DateCreate`/`DateUpdate`/`IsDelete` (kế thừa từ
> `VNR.Successor`) đã đổi sang quy ước .NET/EF Core ở trên, cùng lượt với `Id`
> chuyển từ `set` sang `init`:
> `src/BE/Core/PlatformManager.Core.Domain/Common/BaseEntity.cs:22` (`Id`) và
> `:28` (`IsDeleted`).
>
> *(Cập nhật 2026-08-31: đoạn này từng trỏ tới migration `20260828025008_RenameAuditColumns`
> và script `sql/0007_rename_audit_columns.sql`. Cả hai **không còn tồn tại** — đợt baseline
> lại lịch sử migration đã gộp toàn bộ vào một `InitialCreate` duy nhất, nên tên cột mới nay
> nằm thẳng trong đó. Xem [`../../cau-truc-database.md`](../../cau-truc-database.md) §5.2.)*
>
> Sai khác duy nhất so với đoạn code mẫu ở trên: giá trị mặc định viết là
> `EntityId.New()` chứ không phải `Guid.CreateVersion7()` trực tiếp —
> `src/BE/Core/PlatformManager.Core.Domain/Common/EntityId.cs:15` là điểm sinh
> Id duy nhất của toàn hệ thống (`AppUser`/`AppRole` cũng đi qua đó dù không kế
> thừa `BaseEntity`), và chính nó trả về `Guid.CreateVersion7()`. Viết literal ở
> 2 nơi sẽ phá seam một dòng đó.
- `CreatedBy`/`UpdatedBy` nullable — bản ghi seed/migration không có user
  nào tạo; interceptor set `"system"` khi chưa có `ICurrentUser`.
- `IsDeleted`: soft delete, filter bằng EF **global query filter khai trong
  `DbContext.OnModelCreating`** (một vòng lặp gắn cho mọi entity kế thừa
  `BaseEntity`) — không tự thêm `.Where(x => !x.IsDeleted)` ở từng query, và
  **không** khai lẻ ở từng `IEntityTypeConfiguration<T>`: khai lẻ nghĩa là
  entity mới nào quên khai thì mất filter, không có gì báo.

  > ✅ CÓ THẬT (đối chiếu lại 2026-09-06, mở file đếm dòng) — vòng lặp global ở
  > `src/BE/Core/PlatformManager.Core.Persistence/PlatformManagerDbContext.cs:102`
  > (trong `ApplySoftDeleteQueryFilters`, khai từ `:100`), gọi ở `:77` **sau**
  > `ApplyConfigurationsFromAssembly` (`:75`);
  > *(**🔄 LẬT 2026-09-06 — lần thứ BA cùng một chỗ.** Bốn số của lần đối chiếu
  > 2026-08-31 (`:83`/`:81`/`:58`/`:56`) đều sai, và `:83` rơi vào **giữa docstring**
  > của method — đúng khuôn lỗi mà chính ghi chú ngay dưới đây mô tả. Ba lần liên tiếp
  > ở đúng một chỗ nghĩa là số dòng thủ công trong file này không tự giữ được: khi
  > sửa, tra lại bằng
  > `grep -n "ApplySoftDeleteQueryFilters\|ApplyConfigurationsFromAssembly\|HasQueryFilter" <file>`
  > thay vì tin số đã ghi.)* lời khai lẻ trong
  > `*Configuration.cs` đã gỡ hết. Thứ tự này được các ArchTest ở
  > `src/BE/Tests/PlatformManager.ArchTests/SoftDeleteQueryFilterTests.cs` canh —
  > đảo thứ tự thì **mọi** entity kế thừa `BaseEntity` mất filter im lặng —
  > không chỉ phía Module: `SysMenu` (Core) cũng nằm trong đó. Đã thử, test bắt
  > được. Xem [`../wiki-core/be/01-core-components.md`](../wiki-core/be/01-core-components.md) #1.
  >
  > *Sửa 2026-08-28: ba số dòng trước đó (`:72`/`:55`/`:53`) đều trỏ vào comment,
  > không phải câu lệnh — chúng được ghi kèm nhãn "đã đối chiếu" mà không ai mở
  > file đếm. Và câu "5 entity của Module mất filter" bỏ sót `SysMenu` của chính
  > Core.*

### Lúc INSERT ghi cả **4** field audit — không chỉ hai field `Created*`

**Luật:** `AuditInterceptor` ghi theo trạng thái của entry, và hai nhánh **không** đối xứng:

| `EntityState` | Field được ghi |
|---|---|
| `Added` | **cả 4** — `CreatedAt`, `CreatedBy`, **và** `UpdatedAt` = `CreatedAt`, `UpdatedBy` = `CreatedBy` |
| `Modified` | đúng 2 — `UpdatedAt`, `UpdatedBy` |

**Vì sao ghi luôn `Updated*` lúc tạo** — để hai cột đó **không null** trong khoảng từ lúc bản
ghi ra đời tới lần sửa đầu tiên. Nếu để null, mọi nơi hiển thị "lần sửa cuối" phải tự viết
`UpdatedAt ?? CreatedAt`: FE ở từng màn danh sách, từng báo cáo, từng câu `ORDER BY`. Chỉ cần
một chỗ quên là bản ghi **chưa sửa lần nào** rơi xuống cuối danh sách sắp theo thời gian sửa —
sai im lặng, không lỗi, không test nào bắt.

Cái giá đã cân và **chấp nhận**: mất khả năng đọc `UpdatedAt is null` để biết "bản ghi này chưa
từng bị sửa". Câu hỏi đó hiếm khi được hỏi, còn "sửa lần cuối lúc nào" thì hỏi ở gần như mọi
màn hình. Ai cần phân biệt hai trạng thái đó thì so `UpdatedAt != CreatedAt`, không quay lại
bỏ hai dòng gán ở nhánh `Added`.

> ✅ CÓ THẬT (đối chiếu 2026-09-08, mở file đếm dòng) — trong
> `src/BE/Core/PlatformManager.Core.Persistence/Interceptors/AuditInterceptor.cs`:
> nhánh `case EntityState.Added:` (`:41`) ghi `CreatedAt`/`CreatedBy` (`:42-43`) **và**
> `UpdatedAt`/`UpdatedBy` (`:48-49`); nhánh `case EntityState.Modified:` (`:51`) chỉ ghi
> `:52-53`. Cả bốn dòng dùng chung một `now` (`:35`) nên bản ghi mới có `CreatedAt` **bằng
> đúng** `UpdatedAt`, không lệch vài mili giây.

**Phạm vi: chỉ entity kế thừa `BaseEntity`.** Interceptor lọc bằng
`context.ChangeTracker.Entries<BaseEntity>()` (`AuditInterceptor.cs:37`) — **không** phải "mọi
entity đang được `SaveChanges`". `AppUser` là `IdentityUser<Guid>` nên nằm ngoài tầm với của nó
(lý do đầy đủ: [`../../cau-truc-database.md`](../../cau-truc-database.md) §4.1), và bốn cột vết
của nó ghi tay ở tầng service — **theo luật trên chỉ một nửa**:

| Cặp cột | Ai ghi | Lúc INSERT |
|---|---|---|
| `BaseEntity.CreatedAt`/`UpdatedAt` | `AuditInterceptor` | cả 2, cùng giá trị |
| `AppUser.DateCreate`/`DateUpdate` | tay, ở service | **chỉ `DateCreate`** — `DateUpdate` để null tới lần sửa đầu (`UserAdminService.cs:151`, `CoreSeeder.cs:261`) |
| `AppUser.CreatedBy`/`UpdatedBy` | tay, ở service | cả 2 (`UserAdminService.cs:156-157`, `CoreSeeder.cs:265-266` — neo lại 2026-09-10) |

> **🔄 SỬA 2026-09-09 — hai ô trên từng nêu `UserLookupService` làm ca thứ ba, nay không còn.**
> `IUserLookupService` khi đó có nhánh **tự tạo `AppUser`** khi cột `Phụ trách` của file import
> không khớp ai, và nhánh đó ghi `DateCreate` mà bỏ trống `CreatedBy`/`UpdatedBy` — đúng loại
> lệch mà mục này sinh ra để ghi lại. Nhánh ấy đã bị **xoá** theo
> `spec/danh-muc-dti/business-rules.md` §6.3 (không khớp ai ⇒ `OwnerId` để trống, KHÔNG tạo tài
> khoản). Seam nay chỉ tra cứu, **không ghi cột nào của `AppUser`**, nên nó không còn thuộc bảng
> này — xem `src/BE/Core/PlatformManager.Core.Infrastructure/Identity/UserLookupService.cs:32`.

Ghi ra sự lệch này chứ không lấp nó, vì đây đúng là chỗ dễ suy diễn nhầm: đọc luật ở trên rồi
tưởng `AspNetUsers` cũng vậy. Muốn `AppUser` về cùng khuôn thì phải sửa tay ở **từng** điểm tạo
— chưa làm, và đó là một quyết định riêng chứ không phải hệ quả tự động của mục này.

### ✅ CÓ THẬT (đối chiếu 2026-09-05) — hai bảng phân quyền đã kế thừa `BaseEntity`

Quyết định người dùng 2026-08-31, đã thi công xong. Giữ lại phần "vì sao" bên dưới vì nó
là lý do **giữ** thiết kế này, không chỉ lý do dựng nó.

#### Hiện trạng

| Bảng | Kế thừa `BaseEntity`? | Có trường vết? |
|---|---|---|
| `SysMenu` | ✅ | ✅ |
| `SysMenuRole` — ai thấy màn hình nào | ✅ | ✅ |
| `RolePermission` — ai làm được hành động nào | ✅ | ✅ |
| `AppUser` — kế thừa `IdentityUser<Guid>` | ❌ *(và không thể)* | ✅ `CreatedBy`/`UpdatedBy` gắn tay, cạnh `DateCreate`/`DateUpdate` |

`AppUser` là ngoại lệ **cố ý**: C# không cho kế thừa hai lớp cơ sở, mà `IdentityUser<Guid>`
là bắt buộc. Nó mang trường vết tương đương chứ không mang `BaseEntity`.

Kiểm lại bằng lệnh, đừng tin bảng này:

```bash
grep -n "class SysMenuRole\|class RolePermission\|class SysMenu" src/BE/Core/PlatformManager.Core.Domain/Entities/*.cs
grep -n "CreatedBy\|UpdatedBy" src/BE/Core/PlatformManager.Core.Persistence/Identity/AppUser.cs
```

PASS: ba entity đầu đều hiện `: BaseEntity`; `AppUser` hiện cả `CreatedBy` lẫn `UpdatedBy`.

#### Ba quyết định — tất cả đã vào code

| # | Quyết định | Trạng thái |
|---|---|---|
| 1 | `SysMenuRole` và `RolePermission` kế thừa `BaseEntity` | ✅ `SysMenuRole.cs:22`, `RolePermission.cs:26` |
| 2 | `AppUser` thêm `CreatedBy`/`UpdatedBy` (không kế thừa `BaseEntity` được — đã là `IdentityUser<Guid>`) | ✅ `AppUser.cs:29`, `:32` |
| 3 | `ReplaceAllAsync` của hai repository đổi từ **xoá cứng** (`RemoveRange`) sang **xoá mềm** | ✅ `RolePermissionRepository.cs:52`, `SysMenuRoleRepository.cs:91` |

Cái bẫy đi kèm quyết định 3 — index unique phải lọc theo `IsDeleted` — cũng đã đóng:
`RolePermissionConfiguration.cs:28` và `SysMenuRoleConfiguration.cs:34` đều mang
`.HasFilter("\"IsDeleted\" = false")`.

> **🔄 LẬT 2026-09-05.** Bản trước mang nhãn `🚧` và bảng "Hiện trạng" ghi `❌` cho **cả
> hai** bảng phân quyền — trong khi cả hai đã kế thừa `BaseEntity` từ đợt thi công
> 2026-08-31. Mục này tự nó dặn *"kiểm lại bằng lệnh, đừng tin bảng này"*, và đúng là bảng
> đã sai: lệnh ngay bên dưới nó trả lời ngược lại với nó.

#### Vì sao riêng quyết định 3 là bắt buộc, không phải tuỳ chọn

Thêm cột thôi thì **không đủ** cho ma trận, và lý do nằm ở cách ghi chứ không ở cột:

| Câu hỏi | Chỉ thêm cột | Thêm cột + xoá mềm |
|---|---|---|
| Ai vừa ghi? | ✅ | ✅ |
| Lúc nào? | ✅ | ✅ |
| **Trước đó ma trận là gì?** | ❌ đã xoá vật lý | ✅ còn trong bảng |
| Ô nào vừa đổi? | ❌ mọi dòng cùng dấu thời gian | ✅ so được hai thế hệ |

Vế thứ ba mới là vế cho phép **khôi phục**. Biết ai làm mà không biết trước đó ra sao thì
đủ để quy trách nhiệm, không đủ để sửa — và hệ thống này không có nhật ký nào khác để bù.

#### Hạ tầng cần dùng đã có sẵn — không dựng gì mới

| Cần | Đã có ở |
|---|---|
| Lọc `e => !e.IsDeleted` tự động cho mọi `BaseEntity` | `src/BE/Core/PlatformManager.Core.Persistence/PlatformManagerDbContext.cs:119` — filter ĐẶT TÊN (`SoftDeleteFilterKey`, khai ở `:33`) |
| Điền `CreatedBy`/`UpdatedBy` tự động | `src/BE/Core/PlatformManager.Core.Persistence/Interceptors/AuditInterceptor.cs` |
| Khuôn index unique lọc theo `IsDeleted` | baseline `src/BE/PlatformManager.Api/Persistence/Migrations/20260831165117_InitialCreate.cs:327` (`filter: "\"IsDeleted\" = false"`) |

Nghĩa là mọi truy vấn đọc **không đổi một dòng code** — bộ lọc toàn cục lo phần đó.

#### Hai cái giá đã cân

1. **Bảng phình theo số lần lưu.** Mỗi lần lưu giữ lại một bản dòng cũ. Với ma trận vài
   chục dòng và tần suất sửa thấp, không đáng kể trong nhiều năm. Đã cân nhắc và **loại**
   job dọn định kỳ: thêm một thứ phải vận hành, và nó xoá đúng thứ ta vừa quyết giữ lại.
2. **Index unique phải thêm `WHERE "IsDeleted" = false`.** Không có nó thì lần chèn lại
   một cặp đã xoá mềm sẽ trùng khoá. Đây là **cái bẫy đã dính một lần** trên `SysMenu` —
   xem mục 1 của [`../wiki-core/be/13-core-data-migration.md`](../wiki-core/be/13-core-data-migration.md).

#### Thời điểm — cửa sổ đã dùng, và đã đóng

Cả ba quyết định đều đổi schema, nên chúng gần như **miễn phí** khi thi công: lịch sử
migration lúc đó đang được baseline lại và chưa có DB production
([`../../cau-truc-database.md`](../../cau-truc-database.md) §5.2), nên tất cả gọn trong
`InitialCreate` mới thay vì phải viết migration chạy trên dữ liệu thật.

Cửa sổ đó nay **đã đóng** — baseline `20260831165117_InitialCreate` là mốc cố định. Thay
đổi schema tương tự từ đây trở đi phải đi bằng migration riêng.

#### Nghiệm thu

| # | Phép thử | PASS |
|---|---|---|
| 1 | Lưu ma trận, rồi `SELECT` bảng **không** qua EF | Thấy cả dòng cũ (`IsDeleted = true`) lẫn dòng mới |
| 2 | `GET` ma trận qua API sau phép thử 1 | Chỉ trả dòng hiện hành — bộ lọc toàn cục hoạt động |
| 3 | Lưu ma trận hai lần với **cùng** một cặp `(SysMenuId, RoleId)` | Không lỗi trùng khoá — chứng minh index đã lọc theo `IsDeleted` |
| 4 | Sửa một người dùng | `UpdatedBy` mang tên người **thao tác**, không phải người bị sửa |

Phép thử 3 là phép thử canh cái bẫy ở mục 2 phần "hai cái giá". Phép thử 4 canh một nhầm
lẫn dễ xảy ra khi nối `AuditInterceptor` vào đường Identity.

## Factory method — không `new` + gán property

Khuôn thật đang chạy trong Core: `src/BE/Core/PlatformManager.Core.Domain/Entities/SysMenu.cs`.

```csharp
// Domain/Entities/CriteriaErrors.cs — catalog khai TRƯỚC, cạnh entity
public static class CriteriaErrors
{
    public static readonly DomainError CodeRequired = new(
        "CRITERIA.CODE_REQUIRED", "Mã chỉ tiêu không được để trống.");
    public static readonly DomainError MaxScoreInvalid = new(
        "CRITERIA.MAX_SCORE_INVALID", "Điểm tối đa phải > 0.");
}

// Domain/Entities/Criteria.cs
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
            throw new DomainException(CriteriaErrors.CodeRequired);
        if (maxScore <= 0)
            throw new DomainException(CriteriaErrors.MaxScoreInvalid);

        return new Criteria
        {
            // KHÔNG gán Id ở đây — BaseEntity đã sinh sẵn qua EntityId.New().
            // Gán lại là sinh GUID thứ hai rồi ghi đè cái thứ nhất: không sai
            // kết quả, nhưng là việc thừa và làm mờ chỗ nào thật sự sở hữu Id.
            Code = code,
            Name = name,
            Group = group,
            MaxScore = maxScore,
        };
    }

    public void UpdateScore(decimal maxScore)
    {
        if (maxScore <= 0)
            throw new DomainException(CriteriaErrors.MaxScoreInvalid);
        MaxScore = maxScore;
    }
}
```

> **🔄 LẬT 2026-09-06 — hai chỗ trong khối này từng dạy sai:**
> 1. `throw new DomainException("CRITERIA_CODE_REQUIRED", "…")` — chữ ký chuỗi tự do,
>    không còn biên dịch được từ 2026-09-03. Xem §DomainException bên dưới.
> 2. `CreatedAt = DateTimeOffset.UtcNow` trong factory và `UpdatedAt = DateTimeOffset.UtcNow`
>    trong mutation method. **Không gán tay hai field này** — `AuditInterceptor` điền chúng
>    lúc `SaveChangesAsync`
>    (`src/BE/Core/PlatformManager.Core.Persistence/Interceptors/AuditInterceptor.cs`),
>    và code mẫu gán tay dạy đúng thứ interceptor sinh ra để khỏi phải làm. Entity thật của
>    Core không gán: `SysMenu.Create` (`SysMenu.cs:29-37`) và `SysMenu.ReviveWith`
>    (`SysMenu.cs:55-60`) đều không chạm `CreatedAt`/`UpdatedAt`.

**Vì sao:** factory + mutation method có tên nghiệp vụ (`UpdateScore`, không
`set MaxScore`) giữ mọi invariant ở một chỗ. Nếu ai đó có thể `entity.MaxScore
= -5` từ bên ngoài, invariant "điểm tối đa phải dương" chỉ còn là quy ước bằng
lời, không phải luật được compiler/entity ép buộc.

## Value Object

Dùng khi khái niệm có **≥2 field đi cùng nhau** hoặc có **luật định dạng**
cần validate — không bọc VO cho một `decimal`/`string` đơn lẻ không có luật
gì đặc biệt.

```csharp
public static class PercentageErrors
{
    public static readonly DomainError OutOfRange = new(
        "PERCENTAGE.OUT_OF_RANGE", "Giá trị phải trong khoảng 0–100.");
}

public sealed record Percentage
{
    public decimal Value { get; }
    private Percentage(decimal value) => Value = value;

    public static Percentage Create(decimal value)
    {
        if (value < 0 || value > 100)
            throw new DomainException(PercentageErrors.OutOfRange);
        return new Percentage(value);
    }
}
```

> Hôm nay `src/BE` **chưa có Value Object nào** — kiểm:
> `grep -rn "HasConversion\|OwnsOne" src/BE --include=*.cs | grep -v /obj/` ra rỗng
> (đối chiếu 2026-09-06). Khối trên là khuôn, không phải mô tả hiện trạng.

EF Core mapping cho VO 1 giá trị: `HasConversion` (không `OwnsOne` — chỉ
dùng `OwnsOne` khi VO có nhiều field).

## DomainException — chỉ nhận `DomainError`, KHÔNG nhận chuỗi tự do

```csharp
// Domain/Common/DomainError.cs — bản ghi lỗi khai sẵn trong catalog
public sealed record DomainError(string BusinessCode, string MessageTemplate);

// Domain/Common/DomainException.cs
public class DomainException : Exception
{
    public DomainException(DomainError error, params object[] args) : base(Render(error, args))
        => Error = error;

    public DomainError Error { get; }
}
```

Mã lỗi **phải** khai trước trong catalog `{Entity}Errors.cs` đặt cạnh entity, rồi mới ném:

```csharp
// Domain/Entities/SysMenuErrors.cs
public static class SysMenuErrors
{
    public static readonly DomainError CodeRequired = new(
        "SYS_MENU.CODE_REQUIRED", "Mã menu không được để trống.");
}

// nơi ném
throw new DomainException(SysMenuErrors.CodeRequired);
```

- Khuôn `BusinessCode`: `MIEN.MA_LOI` **viết hoa, có dấu chấm**. Đây là hợp đồng công khai
  với FE (ra field `businessCode` của envelope), và `ErrorCatalogTests` canh khuôn + tính
  duy nhất của nó chung một rổ với `ErrorDescriptor`.
- `MessageTemplate` là câu **dev-facing + dự phòng**, không phải nguồn câu chữ cho người
  dùng cuối.
- `DomainError` **cố ý không mang `ErrorCode`**: status HTTP do **loại exception** quyết
  định, ánh xạ nằm đúng một chỗ ở `ExceptionHandlingBehavior` —
  `DomainException` ⇒ **422** (`ErrorCode.BusinessRuleError`),
  `ConflictException` ⇒ **409** (`ErrorCode.Conflict`). `ConflictException` có cùng chữ ký
  và cùng luật; hôm nay chưa nơi nào ném nó.
- Không để `DomainException` lọt thẳng ra `Api` layer thành 500 chung chung — behavior đã
  bắt (`…/Common/Behaviors/ExceptionHandlingBehavior.cs:44` và `:48`).

Chi tiết `ErrorDescriptor` phía Application: [`be-cqrs-handler.md`](be-cqrs-handler.md)
§ErrorDescriptor.

> **🔄 LẬT 2026-09-06 — luật này đang dạy một chữ ký KHÔNG CÒN BIÊN DỊCH ĐƯỢC.** Bản trước
> khai `DomainException(string code, string message)` với `public string Code { get; }`, và
> **ba** khối code mẫu trong file này ném theo chữ ký đó
> (`throw new DomainException("CRITERIA_CODE_REQUIRED", "…")`). Chữ ký thật đã đổi
> **2026-09-03** sang `DomainException(DomainError error, params object[] args)`
> (`src/BE/Core/PlatformManager.Core.Domain/Common/DomainException.cs:18`) — chuỗi tự do
> không còn nhận được. Ai chép code mẫu cũ ra sẽ gặp lỗi biên dịch ngay, nên ca này rơi
> vào loại "sai lộ ra sớm"; nhưng nó vẫn đúng khuôn mà `.claude/CLAUDE.md` §3 gọi là
> **rule sai sinh ra code sai**, chỉ khác ở chỗ trình biên dịch chặn hộ. Ba khối mẫu đã
> sửa cùng lượt này. Lý do đổi: chuỗi gõ tại chỗ `throw` đi thẳng ra `businessCode` mà
> không qua catalog nào, tạo hai hệ mã trong cùng một field.

## RowVersion — optimistic concurrency cho entity nhiều người sửa

**Hôm nay KHÔNG entity nào trong `src/BE` dùng recipe này** (đối chiếu 2026-09-06) —
`grep -rn "IsRowVersion" src/BE --include=*.cs | grep -v /obj/` ra rỗng. Mục này là khuôn
cho lần đầu tiên cần tới, không phải mô tả hiện trạng.

**Khuôn nhận diện** (rút ra từ ca `CriteriaAssessment` của module DtiWeekly, đã xoá cùng
module 2026-08-29 — giữ lại vì hình dạng lặp lại, không riêng DTI): một entity vừa nhận
**ghi hàng loạt** (import ghi đè toàn bộ field) vừa nhận **sửa tay từng field**
(đọc-rồi-ghi) là đúng hình dạng cần token theo dòng. Không có gì phát hiện khi hai luồng
ghi đè lên nhau; người sửa sau âm thầm mất thay đổi của người trước. Quy tắc chung + ranh
giới với token cấp tập hợp:
[be/06-concurrency-control.md](../wiki-core/be/06-concurrency-control.md).

```csharp
public class Something : BaseEntity
{
    // ... field nghiệp vụ

    // Optimistic concurrency token — PHẢI là uint, KHÔNG shadow property, KHÔNG byte[].
    public uint Version { get; private set; }
}

// EF configuration — API CHUẨN của EF Core, không phải helper riêng của Npgsql
builder.Property(x => x.Version).IsRowVersion();
```

⚠️ **Không áp recipe này cho `AppUser`/`AppRole`.** Chúng là entity của ASP.NET Core
Identity và đã có sẵn `ConcurrencyStamp`; đường `PUT /api/users/{id}` dùng đúng nó
(`Core/PlatformManager.Core.Application/Users/UpdateUserCommand.cs:62`). Thêm cột thứ hai
chồng lên là hai nguồn cho cùng một sự thật.

> **🔄 LẬT 2026-09-06.** Bản trước mở đầu bằng *"**Finding thật (2026-08-17):**
> `CriteriaAssessment` có 2 luồng ghi độc lập…"* và code mẫu mang tên chính entity đó.
> `CriteriaAssessment` và `UpdateCriteriaAssessmentCommand` đã xoá cùng module DtiWeekly
> 2026-08-29, nên câu đó đọc như một lỗ hổng đang mở trên code đang chạy — trong khi thực
> tế là không entity nào còn ở hình dạng ấy. Giữ lại phần **khuôn nhận diện** (vẫn đúng),
> bỏ vế hiện trạng (đã sai), và bổ sung cảnh báo về `AppUser` mà bản trước không có.

> ### ⚠️ `.IsRowVersion()` đúng hay sai tuỳ **kiểu CLR** của property, không tuỳ provider
>
> `.IsRowVersion()` trên `byte[]` ánh xạ sang kiểu `rowversion` của **SQL Server**,
> nơi **DB tự tăng giá trị mỗi lần UPDATE**. PostgreSQL **không có kiểu đó**: Npgsql
> tạo một cột `bytea` bình thường mà **không ai cập nhật**, nên
> `WHERE "RowVersion" = @original` **luôn khớp** — check concurrency **vô hiệu hoàn
> toàn, im lặng**. Không lỗi biên dịch, không lỗi lúc chạy; ghi đè vẫn xảy ra đúng
> như khi chưa làm gì.
>
> Nhưng `.IsRowVersion()` trên property CLR kiểu **`uint`** là chuyện KHÁC — đây là
> "standard EF Core mechanism" mà chính tài liệu Npgsql hướng dẫn dùng
> (www.npgsql.org/efcore/modeling/concurrency.html §"The PostgreSQL xmin system
> column"): Npgsql provider tự nhận diện property `uint` + `IsRowVersion` và bind
> thẳng vào cột hệ thống **`xmin`** có sẵn — không tạo cột mới, không cần migration
> riêng cho property này. Tóm lại: `byte[]` → sai trên Postgres (SQL Server rowversion
> giả); `uint` → đúng trên Postgres (bind `xmin` thật).
>
> **Đổi 2026-08-24 — `builder.UseXminAsConcurrencyToken()` (recipe cũ) KHÔNG CÒN BIÊN
> DỊCH ĐƯỢC.** Xác nhận bằng cách kiểm trực tiếp assembly
> `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3 (không còn symbol này) — method này
> bị chính Npgsql OBSOLETE rồi GỠ HẲN kể từ khoảng bản 7.x (commit gốc "Obsolete
> UseXminAsConcurrencyToken", npgsql/efcore.pg#2546, 2022-10-20), thay bằng đúng cách
> ở trên. Ai còn thấy `UseXminAsConcurrencyToken()` trong code cũ hơn — đó là dấu hiệu
> code chưa từng build được với package version hiện tại trong `.csproj`, không phải
> lựa chọn thiết kế.
>
> Đổi sang SQL Server sau này thì đây là **một trong số ít chỗ phải sửa theo
> provider** — lúc đó mới dùng `byte[] RowVersion` + `.IsRowVersion()`.
>
> *(Sửa 2026-08-23: hợp nhất recipe sai từng tồn tại song song ở 3 file — ca này được
> `.claude/CLAUDE.md` §3 lấy làm bằng chứng cho luật "một chủ đề, một file chủ". Sửa
> tiếp 2026-08-24: recipe hợp nhất đó tự nó cũng sai — `UseXminAsConcurrencyToken()`
> không còn tồn tại trong package version dự án đang dùng, phát hiện khi build thật
> lần đầu với `[RequirePermission]`/Hangfire/RowVersion cùng đợt.)*

EF Core tự thêm `WHERE xmin = @original` vào UPDATE — bị người khác ghi đè trong lúc
đang sửa → `DbUpdateConcurrencyException` → handler trả `Conflict` (409) thay vì âm
thầm ghi đè. **Chỉ thêm cho entity có ≥2 luồng ghi độc lập** chạm cùng bản ghi —
không thêm tràn lan cho entity chỉ 1 người sở hữu (vd hồ sơ cá nhân tự sửa).

## FK cross-module — 3 tầng (áp dụng khi có module nghiệp vụ thứ 2)

Hôm nay `src/BE` **không còn module nghiệp vụ nào**, và thư mục `src/BE/Modules/` cũng
**đã xoá 2026-09-08** (lý do + lệnh kiểm lại: `doc/kien-truc-core-module.md:42`). Ghi quy tắc
trước để không phải quyết định vội khi module đầu tiên được dựng lại (đối chiếu
VNR.Successor — xem `doc/huong_dan/quy-uoc/be-architecture.md` §"Cần dùng chung logic?"):

> **🔄 LẬT 2026-09-06.** Bản trước ghi *"Hiện `Modules.DtiWeekly` là module nghiệp vụ duy
> nhất"*. Sai từ 2026-08-29. Ba ví dụ trong bảng dưới (`CriteriaEvidence`,
> `CriteriaAssessment`, `Criteria`, `CriteriaGroup`) là entity của module đã xoá — giữ
> nguyên **có chủ đích** vì chúng minh hoạ ba mức phạm vi, nhưng đọc chúng như khuôn,
> đừng đi tìm file.

| Phạm vi | Loại FK | `DeleteBehavior` |
| --- | --- | --- |
| Cùng aggregate (vd `CriteriaEvidence` → `CriteriaAssessment`) | Hard FK | `Cascade` |
| Cùng module, khác aggregate (vd `Criteria` → `CriteriaGroup`) | Hard FK | `Restrict` |
| Khác module nghiệp vụ | Soft FK (`Guid?` thuần, không constraint DB) | Không có — chỉ `HasIndex`, validate tồn tại ở tầng Application (`await repo.ExistsAsync(id, ct)`) |

Lý do: ranh giới module quan trọng hơn ranh giới DB vật lý — dù 2 module
chung 1 database, FK cứng xuyên module tạo coupling ngầm mà ArchTest không
bắt được (khác với coupling qua `using` mà ArchTest quét được).

## Khi thêm entity mới

1. Đối chiếu schema đang chạy trước khi thiết kế bảng mới:
   [`../../cau-truc-database.md`](../../cau-truc-database.md) cho schema `core`,
   [`../../cau-truc-database-dti.md`](../../cau-truc-database-dti.md) cho schema
   `business` (4 bảng cụm DTI, đang sống).

   > **🔄 LẬT 2026-09-06.** Bước này trước đây bảo *"đối chiếu dữ liệu mẫu CSV
   > (`doc/ERD/` đã xoá 2026-08-23, sẽ bổ sung lại sau)"* — tức trỏ người đọc tới một thư
   > mục không tồn tại, cho một domain ("theo dõi tiêu chí") đã bị gỡ khỏi repo. Nguồn
   > schema thật đã gộp về hai file trên từ 2026-08-23.
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
5. Migration: dùng wrapper `src/BE/scripts/db.ps1` (tự cài `dotnet-ef`, và tự
   cưỡng chế luật "không bao giờ gọi `database update`"). Gọi thẳng `dotnet ef` thì
   **cả `--project` lẫn `--startup-project` đều là `PlatformManager.Api`** — file `.cs`
   migration + `ModelSnapshot` sống trong project host, không sống trong Core. Đọc lại
   file migration sinh ra trước khi tin.

   > **🔄 LẬT 2026-09-06 — bước này đang dạy một đường dẫn SAI.** Bản trước ghi *"đường
   > dẫn project là **`Core/PlatformManager.Core.Infrastructure`**"*. `db.ps1` đã đổi khỏi
   > giá trị đó và ghi rõ hai hậu quả của việc giữ nó
   > (`src/BE/scripts/db.ps1:76-82`): (a) `dotnet ef` sẽ ghi `Migrations/` +
   > `ModelSnapshot` **vào Core**, tức nhét lịch sử migration của dự án này vào thứ
   > Corebase sẽ ship đi; (b) `--project` phải khớp `MigrationsAssembly` khai ở
   > `PlatformManager.Api/PlatformManagerDbContextFactory.cs`, lệch thì EF báo thẳng
   > *"Your target project … doesn't match your migrations assembly"*. Luật này được
   > cưỡng chế bằng ArchTest `MigrationsLocationTests`. Chỉ thư mục `.sql`
   > (`Core/PlatformManager.Core.Persistence/Migrations/sql/`) là **ở lại**
   > Core — đó là artifact Corebase ship, và integration test đọc thẳng từ đó.

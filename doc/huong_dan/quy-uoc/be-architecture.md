# Architecture — src/BE

> Xem trước **`doc/kien-truc-core-module.md`** (root repo) để hiểu lý do và
> nguồn tham khảo thực tế đằng sau cấu trúc dưới đây — file này chỉ nêu quy
> tắc thực thi, không lặp lại phần lý luận.

## Project layout & dependency direction

> 🚧 **Layout dưới đây là ĐÍCH ĐẾN, chưa phải hiện trạng.** Hôm nay repo có 8 project theo mô
> hình `Modules.DtiWeekly.*`. Đọc bảng *"có thật hôm nay → sẽ thành"* ở đầu
> `doc/kien-truc-core-module.md` **trước khi tạo file mới**.

> **Chỉ 2 tầng ngang hàng: `Core.*` và `Business.*`** — với PlatformManager, nghiệp vụ là 1 khối
> thống nhất (DTI Weekly chỉ là tính năng đầu tiên trong `Business.*`), không phải nhiều domain
> độc lập. Chỉ tách thành nhiều module thật khi có domain nghiệp vụ ĐỘC LẬP thật xuất hiện.
>
> **Nhưng `Core.*` KHÔNG được biết tên đó.** Corebase sẽ tái sử dụng ở nhiều dự án khác (chốt
> 2026-08-23), và dự án khác có thể đặt tên tầng nghiệp vụ là `Modules.<Tên>.*`. Vì vậy Core chỉ
> thấy **`IModuleRegistrar`**, không hardcode chuỗi `"Business"` ở bất kỳ đâu — có ArchTest
> `Core_MustNotKnowBusinessName` canh. Lý do đầy đủ: `doc/kien-truc-core-module.md`.

```
src/BE/
├── Core/
│   ├── PlatformManager.Core.Domain/            ← BaseEntity, DomainException, EntityId,
│   │                                              ConflictException, SysMenu, SysMenuRole
│   ├── PlatformManager.Core.Common/            ← utility THUẦN, zero-dependency (không reference
│   │                                              project nào trong solution, chỉ BCL)
│   ├── PlatformManager.Core.Application/       ← CQRS/envelope dùng chung (ICommand, IQuery,
│   │                                              ApiResult, ErrorDescriptor, behaviors...),
│   │                                              Auth/, Users/, Menu/, Permissions/,
│   │                                              IModuleRegistrar ← seam để tầng nghiệp vụ cắm vào
│   ├── PlatformManager.Core.Persistence/       ← PlatformManagerDbContext, EF Configuration cho
│   │                                              entity Core, Interceptors, CoreSeeder
│   ├── PlatformManager.Core.Infrastructure/    ← IdentityService/UserAdminService/
│   │                                              UserLookupService (phần không phải EF)
│   └── PlatformManager.Core.Api/               ← AuthController, UsersController,
│                                                  MetaController, PermissionsController
├── Business/                                   ← 1 khối duy nhất, KHÔNG lồng thêm tên domain
│   ├── PlatformManager.Business.Domain/           ← MỌI entity nghiệp vụ (Criteria, ...)
│   ├── PlatformManager.Business.Application/      ← MỌI feature nghiệp vụ (Criteria/, Dashboard/...)
│   ├── PlatformManager.Business.Persistence/      ← EF Configuration + repository cho entity nghiệp vụ
│   ├── PlatformManager.Business.Infrastructure/   ← phần không phải EF (tích hợp ngoài, hiện gần trống)
│   └── PlatformManager.Business.Api/              ← MỌI controller nghiệp vụ
├── PlatformManager.Api/                        ← HOST MỎNG — composition root DUY NHẤT thấy cả
│                                                  Core.* lẫn Business.*, gộp controller qua
│                                                  AddApplicationPart, KHÔNG tự có controller riêng
└── Tests/PlatformManager.ArchTests/            ← test kiến trúc, chạy mỗi lần build
```

```
✅ Core.Domain          → (không phụ thuộc gì)
✅ Core.Application     → Core.Domain
✅ Core.Persistence     → Core.Application, Core.Domain
✅ Core.Infrastructure  → Core.Application, Core.Domain, Core.Persistence
✅ Core.Api             → Core.Application, Core.Domain
✅ Business.Domain          → Core.Domain
✅ Business.Application     → Core.Application, Core.Domain, Business.Domain
✅ Business.Persistence     → Core.Persistence, Business.Application, Business.Domain
✅ Business.Infrastructure  → Core.Infrastructure, Business.Application, Business.Domain
✅ Business.Api             → Business.Application, Business.Domain
✅ Api (host)            → mọi project Core.* + Business.* — nơi DUY NHẤT thấy cả 2 tầng

❌ Core.*                → Business.* (Core không được biết về nghiệp vụ)
❌ Core.Api/Business.Api → *.Persistence/*.Infrastructure trực tiếp (chỉ qua *.Application)
❌ *.Domain              → Microsoft.EntityFrameworkCore, ASP.NET Core, bất kỳ package hạ tầng nào
❌ *.Application         → Microsoft.EntityFrameworkCore trực tiếp (DbContext/AsQueryable/Include) — luôn qua interface
❌ *.Application         → *.Persistence/*.Infrastructure (bất kỳ project nào)
❌ *.Application         → IConfiguration trực tiếp — dùng IOptions<T>
```

**Vì sao giữ luật này ngay từ đầu:** chi phí giữ layer sạch từ slice đầu
tiên gần như bằng 0; chi phí gỡ rối sau khi Domain đã dính EF Core, hoặc
`Business.*` đã lỡ reference thẳng `Core.*` sai chiều, thì rất cao và thường
phải viết lại. Không có "code cũ" nào biện minh cho việc phá luật.

**Cần dùng chung logic?** Đưa logic đó lên `Core.Application` (nếu thật sự
generic, không đặc thù nghiệp vụ) — `Business.*` chỉ có 1 khối duy nhất nên
không có "module khác" để reference chéo; nếu về sau xuất hiện domain thật
sự độc lập, xem `doc/kien-truc-core-module.md` § Khi nào tách thành module
độc lập thật trước khi tự ý tạo project mới.

**DIP seam — ghi trước, áp dụng khi có module nghiệp vụ thứ 2:** nếu
`Core.Infrastructure` (vd 1 job nền dùng chung) cần đọc/ghi entity của 1
module cụ thể (vd dọn `CriteriaAssessment` cũ), **không** inject thẳng
`ICriteriaAssessmentRepository` của module đó — vi phạm `Core.* → Business.*`
cấm ở trên. Thay vào đó: khai interface hẹp ở `Core.Application` (vd
`IAssessmentCleanupService`), để `Modules.DtiWeekly.Infrastructure` tự
implement, `Core.Infrastructure` chỉ biết interface. Đối chiếu VNR.Successor
(đã áp dụng đúng mẫu này khi có ≥2 module) — xem
[be/trien-khai/04-p3-platform-persistence.md §10](../wiki-core/be/trien-khai/04-p3-platform-persistence.md)
cho thiết kế đầy đủ (`IBoundedContext`) nếu sau này cần enumerate nhiều
module cùng lúc.

**Notification — seam có sẵn, chỉ dùng khi có nhu cầu thật.** Rà toàn bộ
`spec/*/business-rules.md` (2026-08-17) không có yêu cầu nghiệp vụ nào cần
gửi thông báo — dựng cả hệ thống đa kênh (email/push/in-app) như VNR ngay
bây giờ là phình to không cần thiết. Chỉ khai seam tối thiểu:

```csharp
// Core.Application
public interface INotificationSender
{
    Task SendAsync(string to, string subject, string body, CancellationToken ct);
}
// Core.Infrastructure — impl đầu tiên, đọc IOptions<SmtpOptions> (không IConfiguration trực tiếp)
public sealed class SmtpNotificationSender(IOptions<SmtpOptions> options) : INotificationSender { ... }
```

**Use case đầu tiên có thật, không phải hạ tầng chết:** 1 Hangfire recurring
job quét `CriteriaAssessment.Deadline` sắp tới, gửi email nhắc qua
`INotificationSender` — xem
[`be/07-observability.md`](../wiki-core/be/07-observability.md)
cho Hangfire setup. Lưu ý đã biết: user tự tạo qua CSV/Excel import
(`UserLookupService.ResolveOrCreateByFullNameAsync`) có `Email = null` — job
phải tự bỏ qua case này, không throw. FE **không cần thay đổi gì** cho use
case này (email là kênh ngoài, không cần UI riêng).

## Cấu hình — fail-fast validation

**Nâng từ "chưa cần" lên "nên có sớm" khi chuyển sang giai đoạn product
(2026-08-17).** Hiện `IConfiguration` không leak vào Application/Domain (đã
xác nhận sạch) — nhưng phần đọc config ở Infrastructure/composition root
cũng chưa có validate nào: 1 giá trị bắt buộc (connection string, SMTP host
khi Notification implement) gõ sai/thiếu trong `appsettings.json` chỉ lộ ra
**lúc runtime chạm tới** (vd request đầu tiên gọi tới `SmtpNotificationSender`),
không phải lúc khởi động — chậm hơn nhiều so với biết ngay khi `dotnet run`.

```csharp
public sealed class SmtpOptions
{
    [Required] public string Host { get; init; } = default!;
    [Range(1, 65535)] public int Port { get; init; }
    [Required] public string FromAddress { get; init; } = default!;
}

// Program.cs / DependencyInjection.cs
services.AddOptions<SmtpOptions>()
    .Bind(configuration.GetSection("Smtp"))
    .ValidateDataAnnotations()
    .ValidateOnStart();   // app KHÔNG khởi động được nếu thiếu/sai — biết ngay, không đợi request đầu
```

- Áp dụng cho mọi `IOptions<T>` mới thêm sau này (SMTP, Hangfire connection
  string nếu tách riêng khỏi DB chính...) — không chỉ riêng Notification.
- Không cần bọc thêm `IConfigurationService` facade riêng (kiểu VNR cũ đang
  deprecate) — `IOptions<T>` + `ValidateOnStart()` là đủ, thêm 1 tầng facade
  chỉ tạo thêm chỗ để lệch.

## `PlatformManager.Api` — host mỏng, composition root duy nhất

`Program.cs` đăng ký 2 tầng qua 2 extension method riêng, mỗi extension
method tự gộp controller assembly `*.Api` của mình:
```csharp
services.AddCoreModule(configuration);      // DI + AddApplicationPart cho Core.Api
services.AddBusinessModule(configuration);  // DI + AddApplicationPart cho Business.Api
```
`PlatformManagerDbContext` (định nghĩa trong `Core.Persistence`) không được
hardcode reference tới `Business.*` — `Api` (host) truyền danh sách
`Assembly` (`*.Persistence` của từng tầng đã đăng ký) vào lúc cấu hình
DbContext, `OnModelCreating` gọi
`modelBuilder.ApplyConfigurationsFromAssembly(...)` cho từng assembly trong
danh sách đó. Mỗi tầng tự sở hữu `IEntityTypeConfiguration<T>` của entity
mình. Controller của `Core.Api`/`Business.Api` KHÔNG được tự inject
repository/DbContext — chỉ gọi qua `ISender` (MediatR), giữ đúng ranh giới
`*.Api → *.Persistence/*.Infrastructure` bị cấm ở trên.

## Vertical slice trong `*.Application`

Mỗi feature/entity có một thư mục riêng, chứa toàn bộ
command/query/handler/validator/DTO liên quan — không tách theo tầng kỹ
thuật (`Commands/`, `Queries/`, `Validators/` phẳng ở gốc):

```
<Layer>.Application/<Feature>/
├── Create{Entity}Command.cs
├── Update{Entity}Command.cs
├── Delete{Entity}Command.cs
├── Get{Entity}ByIdQuery.cs
├── Get{Entity}sListQuery.cs
├── {Entity}Validator.cs
├── {Entity}Dto.cs
└── I{Entity}Repository.cs
```

Lý do: khi cần hiểu/sửa một feature, mọi file liên quan nằm cạnh nhau — không
phải nhảy qua 4-5 thư mục theo tầng kỹ thuật để ráp lại bức tranh.

## Dependency Injection

- Đăng ký service theo convention (marker interface hoặc extension method
  theo layer) thay vì liệt kê tay từng service trong `Program.cs` khi số
  lượng service đã đủ lớn để việc liệt kê tay trở thành gánh nặng bảo trì.
  Ở giai đoạn đầu (ít service), đăng ký tay trong
  `DependencyInjection/ServiceCollectionExtensions.cs` của từng project là
  đủ — chỉ chuyển sang convention scan khi thực sự cần.
- `Api` là **composition root duy nhất** biết tới mọi `*.Infrastructure` —
  không project nào khác được reference bất kỳ `*.Infrastructure` nào.

## Thêm tính năng nghiệp vụ mới — checklist

**KHÔNG tạo project mới** cho mỗi tính năng — `Business.*` là 1 khối duy
nhất chứa mọi tính năng nghiệp vụ (DTI Weekly là tính năng đầu tiên, không
phải 1 "module" riêng).

1. Entity mới (nếu có) → `PlatformManager.Business.Domain/`.
2. Feature mới (Command/Query/Handler/Validator/DTO) → thư mục con mới
   trong `PlatformManager.Business.Application/<TênFeature>/` (vertical
   slice, đúng quy ước đã có).
3. EF Configuration + repository implementation →
   `PlatformManager.Business.Persistence/`.
4. Controller mới → `PlatformManager.Business.Api/Controllers/`.
5. Kiểm tra ArchTest `Core_MustNotReference_Business` vẫn pass sau khi thêm.

Chỉ khi tính năng đó thực ra là 1 **domain nghiệp vụ độc lập thật** (không
chia sẻ entity/quy trình gì với `Business.*` hiện có) mới xem xét tách
thành module riêng — đọc kỹ `doc/kien-truc-core-module.md` § Khi nào tách
thành module độc lập thật trước khi tạo project mới, đừng tự quyết một
mình nếu không chắc.

## SOLID & OOP — áp dụng cụ thể vào PlatformManager

> Kiểm tra 2026-08-16: các quy tắc dưới đây trước đó áp dụng NGẦM (qua Clean
> Architecture/CQRS) nhưng chưa viết thành luật tường minh — nay ghi rõ để
> agent tự kiểm tra được, không chỉ "cảm thấy đúng".

**S — Single Responsibility**: 1 Command/Query + 1 Handler = đúng 1 use
case (đã áp dụng qua vertical slice). 1 class chỉ có ĐÚNG 1 lý do để thay
đổi — nếu sửa 1 business rule buộc phải sửa class đó, VÀ đổi công nghệ lưu
trữ cũng buộc phải sửa CHÍNH class đó, đấy là dấu hiệu cần tách.

**O — Open/Closed**: thêm tính năng nghiệp vụ mới (`ErrorDescriptor`,
Command mới) KHÔNG được đòi sửa code đã có ở tầng thấp hơn (`Core.*`,
`ApiControllerBase`, `GlobalExceptionHandler`, `BaseResponse`) — chỉ được
thêm code mới. Cross-cutting concern mới đi qua MediatR pipeline behavior
(đã áp dụng: `ValidationBehavior`, `ExceptionHandlingBehavior`), không sửa
từng handler đã có. Nếu thấy mình đang sửa `Core.*` để phục vụ riêng
`Business.*` — dừng lại, đó là vi phạm OCP thật, không phải việc nhỏ.

**L — Liskov Substitution**: implementation của 1 interface phải dùng được
ở MỌI nơi interface đó được yêu cầu — không ném `NotImplementedException`/
`NotSupportedException` cho bất kỳ method nào trong interface. Implementation
"không cần" 1 method là dấu hiệu interface sai (quá rộng — xem ISP), không
phải lý do implement nửa vời. `AppUser` cố ý KHÔNG kế thừa `BaseEntity`
(Identity tự quản lý vòng đời khác) thay vì kế thừa rồi bỏ qua 1 phần hành
vi — đúng tinh thần LSP.

**I — Interface Segregation**: interface repository/service chỉ khai đúng
method consumer thực sự cần — không tạo `IRepository<T>` tổng quát rồi để
trống phần lớn method ở đa số implementation. Interface >6-8 method là dấu
hiệu nên cân nhắc tách theo nhóm (không phải luật cứng, dùng phán đoán) —
KHÔNG tách nhỏ vụn interface chỉ có 1-2 method dùng chung (trừu tượng hoá
sớm ngược hướng ISP).

**D — Dependency Inversion**: trụ cột chính của toàn bộ kiến trúc đang dùng
— `*.Application` định nghĩa interface (`I*Repository`, `IIdentityService`),
`*.Infrastructure` implement; tầng cao (business rule) không phụ thuộc tầng
thấp (EF Core/ASP.NET Core), tầng thấp phụ thuộc abstraction tầng cao. Toàn
bộ mục "Project layout & dependency direction" ở trên chính là DIP viết
thành luật ArchTest — không có gì thêm cần làm ngoài giữ nguyên kỷ luật đó.

**OOP — encapsulation/abstraction/inheritance/polymorphism**:
- *Encapsulation*: field nghiệp vụ `private set`, mutation qua method tên
  nghiệp vụ (`entity.Approve()`, không `entity.Status = ...`) — trừ 6 field
  kỹ thuật `public set` của `BaseEntity` (lý do ở `entity-domain.md`).
- *Abstraction*: mọi phụ thuộc ra ngoài `*.Domain`/`*.Application` đi qua
  interface — không bao giờ `new SomeInfrastructureClass()` trực tiếp
  trong 2 tầng này.
- *Inheritance*: CHỈ dùng cho `BaseEntity` (field kỹ thuật dùng chung) —
  không xây hierarchy nghiệp vụ nhiều tầng
  (`Criteria : BusinessEntity : AuditableEntity : BaseEntity`...). Ưu tiên
  composition (Value Object, service riêng) hơn kế thừa sâu cho logic
  nghiệp vụ.
- *Polymorphism*: qua interface khi thật sự có ≥2 implementation (hiện tại
  hoặc cận kề) — không ép dùng polymorphism khi 1 `switch`/`if` đơn giản là
  đủ (trừu tượng hoá sớm).

## Testing

- Domain: unit test thuần, không cần DB/HTTP — test factory method, mutation
  method, invariant.
- Application: unit test handler với repository giả lập (in-memory hoặc
  mock) — test logic nghiệp vụ, không test EF Core thật ở tầng này.
- Api: integration test gọi endpoint thật qua `WebApplicationFactory`, dùng
  DB test (container hoặc in-memory tuỳ độ trung thực cần thiết).
- `Tests/PlatformManager.ArchTests`: test kiến trúc (layer dependency, zero
  package reference ở Domain, Core không reference Business, `*.Api` không
  reference `*.Persistence`/`*.Infrastructure` trực tiếp) — chạy cùng
  `dotnet test`, coi là gate bắt buộc trước khi báo hoàn thành 1 phase.

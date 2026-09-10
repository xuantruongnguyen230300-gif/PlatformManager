---
kind: luat
scope: core
verified: 2026-09-06
---

# Architecture — src/BE

> Xem trước **`doc/kien-truc-core-module.md`** (root repo) để hiểu lý do và
> nguồn tham khảo thực tế đằng sau cấu trúc dưới đây — file này chỉ nêu quy
> tắc thực thi, không lặp lại phần lý luận.

## Project layout & dependency direction

> 🚧 **Layout dưới đây là ĐÍCH ĐẾN, chưa phải hiện trạng.** Từ 2026-08-29 repo
> **không còn module nghiệp vụ nào** — module duy nhất (`Modules.DtiWeekly.*`) đã
> xoá để xây lại, và bản thân thư mục `src/BE/Modules/` **đã xoá 2026-09-08**
> (lý do + lệnh kiểm lại: `doc/kien-truc-core-module.md:42`). Chạy
> `find src/BE -iname *.csproj` để biết số project chính xác hiện tại thay vì tin
> số cứng (đã lệch ít nhất 1 lần: 8 → 10 khi 2 project test mới được thêm
> 2026-08-24). Đọc bảng *"có thật hôm nay → sẽ thành"* ở đầu
> `doc/kien-truc-core-module.md` **trước khi tạo file mới**.

> **Chỉ 2 tầng ngang hàng: `Core.*` và `Business.*`** — với PlatformManager, nghiệp vụ là 1 khối
> thống nhất (DTI Weekly chỉ là tính năng đầu tiên trong `Business.*`), không phải nhiều domain
> độc lập. Chỉ tách thành nhiều module thật khi có domain nghiệp vụ ĐỘC LẬP thật xuất hiện.
>
> **Nhưng `Core.*` KHÔNG được biết tên đó.** Corebase sẽ tái sử dụng ở nhiều dự án khác (chốt
> 2026-08-23), và dự án khác có thể đặt tên tầng nghiệp vụ là `Modules.<Tên>.*`. Vì vậy Core chỉ
> thấy **`IModuleRegistrar`**, không hardcode chuỗi `"Business"` ở bất kỳ đâu.
> Lý do đầy đủ: `doc/kien-truc-core-module.md`.
>
> **Hai vế, nay CÙNG trạng thái** (vế 1 đối chiếu 2026-09-06, vế 2 đối chiếu 2026-09-08)**:**
>
> | Vế | Trạng thái |
> |---|---|
> | ArchTest canh chuỗi `"Business"` trong mã Core | ✅ **CÓ THẬT** — `CoreSource_MustNotContain_BusinessNameStringLiteral` (`src/BE/Tests/PlatformManager.ArchTests/CoreMustNotKnowBusinessNameTests.cs:80`), kèm **3** test đối chứng (`:162`, `:192`, `:211`). Thêm cả `Core_MustNotReference_AnyModulesAssembly` (`CoreModuleBoundaryTests.cs:32`) canh ở mức assembly |
> | Seam `IModuleRegistrar` | ✅ **CÓ THẬT (2026-09-08)** — thi công đủ 3 đường (DI · cấu hình EF · ApplicationPart) + ArchTest canh. Bảng neo `file:dòng` đầy đủ nằm ở `doc/kien-truc-core-module.md` §`IModuleRegistrar`; đây là file chủ của chủ đề, đừng chép bảng đó ra chỗ này |
>
> > **🔄 LẬT 2026-09-06.** Bản trước gộp hai vế này vào một nhãn `🚧 "Cả hai đều là ĐÍCH ĐẾN,
> > chưa thi công"` (đối chiếu 2026-08-27) và gọi ArchTest bằng tên `Core_MustNotKnowBusinessName`
> > — **không có test nào mang tên đó**. Hai lỗi ngược chiều nhau trong một câu: gate đã có thì
> > bảo là chưa, còn tên để đi tìm nó thì sai nên không tra ra được.
> >
> > **🔄 LẬT LẦN HAI 2026-09-08 — cùng khuôn, ngược chiều.** Ô "Seam `IModuleRegistrar`" ở trên
> > mang nhãn `📐 CHƯA CÓ` kèm bằng chứng "grep ra 3 dòng, cả ba trong một file test". Seam được
> > thi công 2026-09-08 và ô này không đổi theo, nên bằng chứng đó chết ngay hôm sau: chạy lại
> > **chính lệnh grep ấy** ra hàng chục dòng ở nhiều file sản phẩm. Bài học lặp lại y hệt lần
> > trước: **trạng thái thi công không thuộc file này**, nó thuộc file chủ
> > `doc/kien-truc-core-module.md`. Ở đây chỉ giữ một dòng trỏ đường — chép trạng thái ra file thứ
> > hai là tự hẹn ngày nó sai.

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
│   └── PlatformManager.Core.Api/               ← ApiControllerBase (base class BẮT BUỘC của mọi
│                                                  controller) + AuthController, UsersController,
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
✅ Business.Api             → Core.Api, Business.Application, Business.Domain
                              (Core.Api vì ApiControllerBase sống ở đó — xem §Q8 ngay dưới)
✅ Api (host)            → mọi project Core.* + Business.* — nơi DUY NHẤT thấy cả 2 tầng

❌ Core.*                → Business.* (Core không được biết về nghiệp vụ)
❌ Core.Api/Business.Api → *.Persistence/*.Infrastructure trực tiếp (chỉ qua *.Application)
❌ *.Domain              → Microsoft.EntityFrameworkCore, ASP.NET Core, bất kỳ package hạ tầng nào
❌ *.Application         → Microsoft.EntityFrameworkCore trực tiếp (DbContext/AsQueryable/Include) — luôn qua interface
❌ *.Application         → *.Persistence/*.Infrastructure (bất kỳ project nào)
❌ *.Application         → IConfiguration trực tiếp — dùng IOptions<T>
                           (ĐÚNG 1 ngoại lệ đã khai đích danh, đọc ngay dưới khối này)
```

### 🚧 ĐÃ CHỐT — ĐANG THI CÔNG (Q8, 2026-09-09) — `PlatformManager.Core.Api` giữ `ApiControllerBase`

Quyết định người dùng 2026-09-09: dựng project **`PlatformManager.Core.Api`** (đã có trong layout
đích ở khối cây trên), và `ApiControllerBase` chuyển về đó. Hệ quả lên chiều phụ thuộc: thêm
**`Business.Api → Core.Api`** và **`PlatformManager.Api → Core.Api`**; không cạnh nào khác đổi.

| Có thật hôm nay (2026-09-09) | Sẽ thành |
| --- | --- |
| ✅ `ApiControllerBase` **đã ở** `PlatformManager.Core.Api` — thi công 2026-09-09, `src/BE/Core/PlatformManager.Core.Api/ApiControllerBase.cs:32` | 4 controller Core cũng chuyển sang; host chỉ còn composition root |
| ✅ `Core.Api` **là project** trong `Core/`, khai ở `src/BE/PlatformManager.slnx` — thi công 2026-09-09 | `Business.Api` reference nó |
| `Business.Api` chưa tồn tại nên chưa ai cần base class từ ngoài host | reference `Core.Api` để kế thừa `ApiControllerBase` |

📖 Vì sao là `Core.Api` chứ không phải host hay `Core.Application` (vòng tròn tham chiếu ·
gate cấm `Microsoft.AspNetCore`): [`../../kien-truc-core-module.md`](../../kien-truc-core-module.md)
§`Core.Api` giữ `ApiControllerBase` — file chủ của quyết định, không lặp lại ở đây.

**Cạnh mới KHÔNG nới luật `*.Api ⇏ *.Persistence/*.Infrastructure`.** `Business.Api → Core.Api`
là tham chiếu tới một project **cùng tầng Api**, để lấy base class dùng chung — nó không mở
đường nào tới Persistence/Infrastructure. Khi viết
`Api_MustNotReference_PersistenceOrInfrastructure_Directly` (rule `📐` ở
[`../../kien-truc-core-module.md`](../../kien-truc-core-module.md) §ArchTest cần có), đừng viết
thành "`*.Api` không reference project nào khác".

### Ngoại lệ DUY NHẤT của luật `*.Application ⇏ IConfiguration` — khai 2026-09-08

Ngoại lệ này áp cho **đúng một chữ ký, gọi đúng tên**:
`IModuleRegistrar.RegisterServices(IServiceCollection, IConfiguration)` —
`src/BE/Core/PlatformManager.Core.Application/Modules/IModuleRegistrar.cs:44`.
Không áp cho kiểu `IConfiguration` nói chung, không áp cho bất kỳ thành viên nào khác của
`IModuleRegistrar`, không áp cho một chữ ký "tương tự" viết sau này.

**Ranh giới phân biệt là THỜI ĐIỂM, không phải kiểu dữ liệu.** `RegisterServices` chạy lúc
**composition** — khi `ServiceCollection` còn đang dựng, chưa có `ServiceProvider` nào, chưa có
request nào. Ở đó `IConfiguration` là **tham số truyền qua**: `Core.Application` chỉ **khai kiểu**
trong hợp đồng để mỗi tầng tự `Bind` `IOptions<T>` của **chính mình**; bản thân
`Core.Application` **không đọc một giá trị cấu hình nào**. Đó là điều kiện của ngoại lệ, và nó
đo được — xem lệnh nghiệm thu cuối mục.

**Luật gốc nhắm vào ca khác hẳn, và ca đó vẫn CẤM TUYỆT ĐỐI:** handler / validator / service của
`*.Application` nhận `IConfiguration` rồi đọc giá trị lúc chạy (`cfg["Foo"]`,
`cfg.GetSection(...).Get<T>()`). Bằng chứng ngữ cảnh cho ý định đó nằm ngay ở §"Cấu hình —
fail-fast validation" bên dưới: cả mục chỉ nói về `IOptions<T>` + `ValidateOnStart()`. Đọc config
lúc chạy đi vòng qua toàn bộ cơ chế ấy — giá trị thiếu/sai không lộ ra lúc khởi động mà lộ ra ở
request đầu tiên chạm tới, và handler đó thôi unit-test được nếu không dựng cấu hình thật.

🛑 **Không lấy ngoại lệ này làm tiền lệ.** Nó **không** nới luật thành *"`IConfiguration` được
phép ở Application nếu có lý do chính đáng"* — mọi lần vi phạm đều tự thấy mình có lý do chính
đáng. Muốn có ngoại lệ thứ hai thì phải sửa **cả** mục này **và** ArchTest ở dưới; hai việc phải
giải trình đó là cố ý, không phải thủ tục thừa.

Cưỡng chế bằng máy, không bằng câu văn — hai luật, hai tầng khác nhau (đối chiếu 2026-09-08):

| Canh gì | Test |
| --- | --- |
| `Core.Application` chỉ được reference **abstraction** cấu hình, không provider/binder nào | `Core_Application_MustNotReference_ConfigurationPackages_Beyond_Abstractions` (`src/BE/Tests/PlatformManager.ArchTests/LayerDependencyTests.cs:112`) |
| Trong mã nguồn `Core.Application`, `IConfiguration` chỉ được xuất hiện ở **đúng file khai hợp đồng** | `CoreApplicationSource_MustNotMention_IConfiguration_OutsideRegistrarContract` (`LayerDependencyTests.cs:151`) |

Luật thứ hai là luật thật sự chặn rủi ro tiền lệ: một handler nhận `IConfiguration` rồi dùng
`cfg["Foo"]` **không** tạo tham chiếu package mới nào, nên luật thứ nhất không thấy nó.

Nghiệm thu bằng tay — lệnh này phải in **đúng một** dòng, và dòng đó phải là chữ ký trong
`Modules/IModuleRegistrar.cs`:

```bash
grep -rn 'IConfiguration' src/BE/Core/PlatformManager.Core.Application --include='*.cs'
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
`IAssessmentCleanupService`), để `Modules.<Ten>.Infrastructure` tự
implement, `Core.Infrastructure` chỉ biết interface. Đối chiếu VNR.Successor
(đã áp dụng đúng mẫu này khi có ≥2 module) — xem
[../../tham-khao-ngoai/vnr-successor/04-p3-platform-persistence.md §10](../wiki-core/../../tham-khao-ngoai/vnr-successor/04-p3-platform-persistence.md)
cho thiết kế đầy đủ (`IBoundedContext`) nếu sau này cần enumerate nhiều
module cùng lúc.

**Notification — seam có sẵn, chỉ dùng khi có nhu cầu thật.** Rà toàn bộ
`spec/*/business-rules.md` (2026-08-17) không có yêu cầu nghiệp vụ nào cần
gửi thông báo — dựng cả hệ thống đa kênh (email/push/in-app) như VNR ngay
bây giờ là phình to không cần thiết. Chỉ khai seam tối thiểu:

```csharp
// Core.Application — nhận KHOÁ + THAM SỐ + ngôn ngữ, KHÔNG nhận chuỗi đã dựng xong
public interface INotificationSender
{
    Task SendAsync(NotificationRequest request, CancellationToken ct);
}
// Core.Infrastructure — impl đầu tiên, đọc IOptions<SmtpOptions> (không IConfiguration trực tiếp)
public sealed class SmtpNotificationSender(IOptions<SmtpOptions> options) : INotificationSender { ... }
```

> **🔄 LẬT 2026-09-06 — chữ ký trong khối này đã lỗi thời.** Bản trước khai
> `SendAsync(string to, string subject, string body, CancellationToken ct)`. Đổi
> **2026-09-03** sang `SendAsync(NotificationRequest request, …)`
> (`src/BE/Core/PlatformManager.Core.Application/Notifications/INotificationSender.cs:20`).
> Lý do ghi ngay trong docstring của nó: chữ ký cũ nhận **chuỗi đã dựng xong**, buộc nơi gọi
> tự chọn câu chữ trong khi nó là chỗ **ít biết nhất** về ngôn ngữ người nhận; và ba tham số
> `to`/`subject`/`body` mang **hình dạng của email**, không tái dùng được cho in-app/SMS/Zalo
> ZNS. Đổi được rẻ vì seam còn **0 consumer** — xem
> [`../wiki-core/be/12-notifications.md`](../wiki-core/be/12-notifications.md) §0.

**Seam có thật, hiện thực có thật, nhưng CHƯA ĐƯỢC ĐĂNG KÝ** (đối chiếu 2026-09-06):
`INotificationSender` + `SmtpNotificationSender` + `AddNotificationInfrastructure()` đều tồn
tại ở Core, nhưng **không dòng nào gọi** `AddNotificationInfrastructure()` — chủ đích, lý do
ghi thẳng trong `src/BE/PlatformManager.Api/Program.cs:177-185`: chưa có consumer nào, và bật
lên sẽ làm app **không khởi động được** (`SmtpOptions.ValidateOnStart()` gặp `appsettings.json`
thiếu section `Smtp`), kéo đỏ luôn toàn bộ integration test.

Use case dự kiến khi bật: một Hangfire recurring job quét mốc hạn của dữ liệu nghiệp vụ, gửi
email nhắc qua `INotificationSender` — xem
[`be/07-observability.md`](../wiki-core/be/07-observability.md) cho Hangfire setup. Lưu ý đã
biết và vẫn còn hiệu lực: user tự tạo qua import
(`IUserLookupService.ResolveOrCreateByFullNameAsync`,
`src/BE/Core/PlatformManager.Core.Application/Users/IUserLookupService.cs:19`) có
`Email = null` — job phải tự bỏ qua case này, không throw. FE **không cần thay đổi gì** (email
là kênh ngoài, không cần UI riêng).

> **🔄 LẬT 2026-09-06.** Bản trước tiêu đề *"Use case đầu tiên **có thật**, không phải hạ tầng
> chết"* và mô tả job quét `CriteriaAssessment.Deadline`. Entity đó đã xoá cùng module DtiWeekly
> 2026-08-29, và recurring job ấy chưa bao giờ tồn tại — hôm nay Hangfire mới chỉ dùng
> fire-and-forget và **không job nghiệp vụ nào** chạy trên nó. Đúng nghĩa đen, `INotificationSender`
> hiện **đang là** hạ tầng chết; nói ngược lại là che mất điều kiện phải xử lý trước khi bật.

## Cấu hình — fail-fast validation

**Nâng từ "chưa cần" lên "nên có sớm" khi chuyển sang giai đoạn product
(2026-08-17).** Lý do khi đó: 1 giá trị bắt buộc (connection string, SMTP host
khi Notification implement) gõ sai/thiếu trong `appsettings.json` chỉ lộ ra
**lúc runtime chạm tới** (vd request đầu tiên gọi tới `SmtpNotificationSender`),
không phải lúc khởi động — chậm hơn nhiều so với biết ngay khi `dotnet run`.

Trạng thái hôm nay (đối chiếu 2026-09-08):

- **`IConfiguration` KHÔNG còn tuyệt đối vắng mặt khỏi `Core.Application`** — nó xuất hiện đúng
  **1** lần, ở chữ ký `IModuleRegistrar.RegisterServices`
  (`src/BE/Core/PlatformManager.Core.Application/Modules/IModuleRegistrar.cs:44`), và
  `Core.Application` reference `Microsoft.Extensions.Configuration.Abstractions`
  (`src/BE/Core/PlatformManager.Core.Application/PlatformManager.Core.Application.csproj:18`).
  Đây là **ngoại lệ đã khai** ở §"Ngoại lệ DUY NHẤT…" phía trên, có 2 ArchTest giữ cho nó không
  lan ra. `*.Domain` thì vẫn tuyệt đối sạch — `Core.Domain` có **zero** package reference, canh
  bởi `LayerDependencyTests.cs:47`.
- Vế "chưa có validate nào" cũng không còn đúng: `ValidateOnStart()` nay là **luật có gate** —
  mọi `*Options` mang `[Required]` phải có một đường gọi `ValidateOnStart()`, canh bởi
  `OptionsValidateOnStartTests.EveryRequiredOptions_HasA_ValidateOnStart_CodePath`
  (`src/BE/Tests/PlatformManager.ArchTests/OptionsValidateOnStartTests.cs:36`).

> **🔄 SỬA 2026-09-08.** Bản trước mở đầu bằng *"Hiện `IConfiguration` không leak vào
> Application/Domain (đã xác nhận sạch) — nhưng phần đọc config ở Infrastructure/composition
> root cũng chưa có validate nào"*. Cả hai vế đều đã hết hạn, và vế đầu hết hạn theo đúng kiểu
> nguy hiểm nhất: nó tuyên bố **sạch tuyệt đối** đúng vào lúc có một ngoại lệ được cố ý mở
> (seam `IModuleRegistrar` thi công 2026-09-08). Người đọc câu cũ sẽ kết luận code đang **vi
> phạm** luật ở khối trên, rồi hoặc đi "sửa" một thiết kế đã chốt, hoặc mất niềm tin vào cả hai
> tài liệu. Ngoại lệ có khai thì phải khai ở **cả hai** chỗ nói về nó.

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

### ✅ CÓ THẬT (đối chiếu 2026-09-05) — `Cors:AllowedOrigins` đã về đúng khuôn `IOptions<T>`

Quyết định người dùng 2026-08-31, đã thi công. Trước đó có đúng một chỗ đọc cấu hình
**không** đi qua `IOptions<T>` nên lọt khỏi luật ở mục trên: `Program.cs` đọc thẳng
`configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? []`. Dấu `?? []` biến
một cấu hình **thiếu** thành allowlist **rỗng**, mà allowlist rỗng chặn **mọi** origin —
FE không gọi được một API nào, trong khi `/health` vẫn xanh và deploy vẫn báo thành công.

Hôm nay đường đọc là `src/BE/PlatformManager.Api/Common/CorsPolicyOptions.cs`:

| Thành phần | Ở đâu | Vai trò |
|---|---|---|
| `CorsPolicyOptions.AllowedOrigins` | `CorsPolicyOptions.cs:53` (thuộc tính `[Required]` ở `:48`, `[MinLength(1)]` ở `:50`) | Thông điệp lỗi nêu đích danh biến môi trường cần đặt |
| `ConfigureDefaultCorsPolicy` | `CorsPolicyOptions.cs:64` | Phân giải **lười** qua DI, nên không có đường tắt nào nhận mặc định rỗng |

> *(🔄 LẬT 2026-09-06: hai số cũ `:44`/`:58` đều lệch — `:44` là dấu `}` đóng khai báo, `:58`
> nằm giữa thân `AddPolicy`. Cả hai vẫn nằm trong file nên cổng `check-docs.sh` §6 cho qua.)*

Nghiệm thu — lệnh này phải **rỗng**, tức khuôn cũ đã biến mất hẳn:

```bash
grep -n 'GetSection("Cors:AllowedOrigins")' src/BE/PlatformManager.Api/Program.cs
```

Lập luận đã có sẵn trong dự án này, chỉ áp thêm một chỗ: docstring của
`BootstrapOptions` viết *"một tài khoản quản trị với mật khẩu ai cũng đoán được còn
tệ hơn hẳn một app từ chối khởi động."* Câu đó áp thẳng được — một app khởi động
thành công rồi chặn sạch mọi request của người dùng còn tệ hơn một app dừng lại và
nói thẳng nó thiếu gì.

Ranh giới: **chỉ fail-fast ở Production.** `ValidateOnStart()` chỉ gắn ở Production;
Development cố ý **không** fail-fast — để một máy chưa cấu hình xong vẫn boot được — và ở đó
allowlist rỗng lộ ra ngay lời gọi API đầu tiên chứ không âm thầm.

> **🔄 SỬA 2026-09-08 — vế "cấu hình sẵn trong repo" đã sai từ 2026-08-31.** Câu trên trước
> đây viết *"Development vẫn chạy được với cấu hình sẵn trong repo"*. Repo **không** mang
> sẵn cấu hình dev nào: `src/BE/.gitignore:20` (khuôn `appsettings.*.json`) loại
> `appsettings.Development.json` ra, kiểm bằng `git ls-files src/BE/PlatformManager.Api` —
> chỉ in `appsettings.json`, và file đó không có khoá `Cors`. Máy mới clone về vì vậy có
> allowlist **rỗng** cho tới khi tự tạo cấu hình cục bộ; cách làm ở
> [`repo-artifact.md`](repo-artifact.md) §1.1. Cùng câu sai này còn nằm trong chú thích
> `CorsPolicyOptions.cs` và `Program.cs`, sửa cùng ngày — và vì phần chú thích đó dài thêm
> 10 dòng, số dòng ở bảng trên cập nhật theo (`:38`/`:40`/`:43`/`:54` → `:48`/`:50`/`:53`/`:64`).

> **🔄 LẬT 2026-09-05.** Bản trước mang nhãn `🚧` và trình bày khuôn cũ ở thì hiện tại
> (*"Nó đọc thẳng `IConfiguration` và kết thúc bằng `?? []`"*), kèm một lệnh `grep` đưa ra
> làm **bằng chứng của vấn đề**. Lệnh đó nay trả về rỗng — tức chính bằng chứng mà mục này
> dựa vào đã tự bác bỏ nó.

> 📖 Bối cảnh triển khai làm khoá này thành bắt buộc:
> [`../wiki-core/fe/17-phuc-vu-va-trien-khai.md`](../wiki-core/fe/17-phuc-vu-va-trien-khai.md) §6

## `PlatformManager.Api` — host mỏng, composition root duy nhất

> 📐 **Mục này mô tả ĐÍCH ĐẾN, cùng phạm vi với banner ở §"Project layout".** Hiện trạng
> 2026-09-09: `Core.Api` **đã tồn tại** và `ApiControllerBase` đã chuyển vào đó (Q8, xem §trên,
> `src/BE/Core/PlatformManager.Core.Api/ApiControllerBase.cs:32`). `Business.Api` thì **chưa**, và
> bốn controller Core vẫn sống **trong chính host** (`src/BE/PlatformManager.Api/Controllers/`).
> `AddApplicationPart` thì đã có đường đi qua seam registrar, chỉ chưa có assembly nào để gộp:
> `ApiAssembly` của Core hiện là `null`.
>
> *(Thêm 2026-09-06: trước đây cả mục này không mang nhãn nào, nên câu "KHÔNG tự có controller
> riêng" đọc như một luật đang bị vi phạm — trong khi thực ra nó là đích chưa tới.)*

`Program.cs` **không** gọi từng tầng bằng một extension method riêng. Nó gom mọi
`IModuleRegistrar` rồi đăng ký một lượt qua `AddModules` — đường DI + cấu hình EF ở
`src/BE/PlatformManager.Api/Program.cs:144`, đường `ApplicationPart` ở `:107`:

```csharp
builder.Services.AddModules(builder.Configuration, moduleRegistrars);
```

> **🔄 SỬA 2026-09-09 — đoạn mẫu cũ dạy một đường không tồn tại.** Bản trước viết
> `services.AddCoreModule(configuration); services.AddBusinessModule(configuration);` như thể
> đó là cách host đăng ký hai tầng. Đo lại: `Program.cs` **không gọi `AddCoreModule` lần nào**
> — lời gọi duy nhất nằm trong
> `src/BE/Core/PlatformManager.Core.Infrastructure/Modules/CoreModuleRegistrar.cs:31`, tức Core
> đi **chung** đường registrar với tầng nghiệp vụ chứ không có lối riêng (§`IModuleRegistrar`
> của [`../../kien-truc-core-module.md`](../../kien-truc-core-module.md)). `AddBusinessModule`
> thì chưa bao giờ tồn tại. Cùng lượt gỡ neo `Program.cs:126` mà bản trước gán cho lời gọi
> `AddCoreModule(...)`: dòng đó là một dòng cấu hình `JsonNamingPolicy`, không liên quan.
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
5. Chạy `dotnet test` — hai ArchTest canh ranh giới này là
   `Core_MustNotReference_AnyModulesAssembly` (`CoreModuleBoundaryTests.cs:32`) và
   `CoreSource_MustNotContain_BusinessNameStringLiteral` (`CoreMustNotKnowBusinessNameTests.cs:80`).

   > *(Sửa 2026-09-06: bản trước gọi tên `Core_MustNotReference_Business` — không test nào mang
   > tên đó, nên người làm theo checklist sẽ tra không ra rồi bỏ qua bước này.)*

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
  kỹ thuật `public set` của `BaseEntity` (lý do ở [`be-entity-domain.md`](be-entity-domain.md)).
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
- Api: integration test gọi endpoint thật qua `WebApplicationFactory`, dùng **Postgres thật
  trong container** (`Testcontainers.PostgreSql` — `PostgresFixture`).
  **`UseInMemoryDatabase` bị CẤM**, không phải "tuỳ độ trung thực cần thiết": provider
  InMemory không có transaction, không có ràng buộc, không có index lọc, nên nó không chứng
  minh được gì về hành vi thật. Luật đầy đủ:
  [`../wiki-core/be/04-testing-strategy.md`](../wiki-core/be/04-testing-strategy.md).

  > **🔄 LẬT 2026-09-06 — dòng này đang cho phép đúng thứ một gate ĐANG CHẶN.** Bản trước ghi
  > *"dùng DB test (container hoặc **in-memory tuỳ độ trung thực cần thiết**)"*. `UseInMemoryDatabase`
  > bị `BannedDependencyTests` quét mã nguồn và đánh đỏ
  > (`src/BE/Tests/PlatformManager.ArchTests/BannedDependencyTests.cs:104`), kèm cả test đối chứng
  > chứng minh bộ dò bắt được (`:171-176`). Ai theo quy ước cũ sẽ viết code đỏ gate ngay lần
  > `dotnet test` đầu tiên — đúng khuôn "rule sai sinh ra code sai" ở `.claude/CLAUDE.md` §3.
- `Tests/PlatformManager.ArchTests`: test kiến trúc — chạy cùng `dotnet test`, coi là
  gate bắt buộc trước khi báo hoàn thành 1 phase.

  Đừng chép danh sách test vào đây (`.claude/CLAUDE.md` §6) — đọc thẳng từ nguồn:

  ```bash
  grep -rn 'public void ' src/BE/Tests/PlatformManager.ArchTests/*.cs
  ```

  Ba luật lớn nhất, đối chiếu 2026-09-08:

  | Luật | Test giữ nó |
  | --- | --- |
  | `Core.Domain` có **zero** package reference | `LayerDependencyTests.cs:47` |
  | `Core.Application` không chạm EF Core / ASP.NET Core / bất kỳ hạ tầng nào | `LayerDependencyTests.cs:61` |
  | Core không reference assembly nghiệp vụ nào | `CoreModuleBoundaryTests.cs` — `Core_MustNotReference_AnyModulesAssembly` |

  > **📐 ĐÍCH ĐẾN — CHƯA THI CÔNG: `*.Api` không reference `*.Persistence`/`*.Infrastructure`
  > trực tiếp.** Bản trước của gạch đầu dòng này nêu luật đó ở **thì hiện tại**, như thể đã
  > có test canh. Không có: `LayerDependencyTests` chỉ mang hai test kể trên, và không test
  > nào ở `ArchTests/` canh cạnh Api → Infrastructure (đối chiếu 2026-09-08).
  >
  > Và nó **chưa tới ngưỡng**: hôm nay `PlatformManager.Api` là composition root **duy nhất**
  > của solution (không có `Core.Api`), nên nó *phải* reference `Core.Infrastructure` để đăng
  > ký DI — đó là công việc của composition root, không phải vi phạm. Luật này chỉ có nghĩa
  > khi tách ra một project API riêng khỏi host; đúng lúc đó mới viết test.
  >
  > Danh sách project để tự kiểm ngưỡng:
  > `find src/BE -name '*.csproj' -not -path '*/obj/*' -not -path '*/bin/*'`

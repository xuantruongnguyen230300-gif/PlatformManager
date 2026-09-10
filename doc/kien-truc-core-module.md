---
kind: luat
scope: core
verified: 2026-09-06
---

# Kiến trúc Core ↔ Business — ranh giới tái sử dụng cho BE và FE

> Tài liệu quyết định (đã CHỐT 2026-08-16, sửa v3 cùng ngày). Agent (`backend-expert`,
> `frontend-expert`, `core-reviewer`) đọc file này để biết ranh giới bắt buộc giữa phần **Core**
> (dùng lại được cho mọi sản phẩm dựng trên nền tảng này) và phần **Business** (khối nghiệp vụ
> thống nhất, hiện có DTI Weekly là tính năng đầu tiên) — áp dụng khi sửa code hiện có VÀ khi
> thêm tính năng nghiệp vụ mới sau này.

## 🚧 ĐÃ CHỐT — ĐANG THI CÔNG (đối chiếu `src/BE/PlatformManager.slnx` + cây `src/BE` ngày 2026-08-29)

**Toàn bộ layout mô tả bên dưới là ĐÍCH ĐẾN, chưa phải hiện trạng.** Đọc bảng
này trước khi tạo bất kỳ file nào, để không tạo vào project chưa tồn tại.

**Số project: đếm bằng lệnh, đừng chép số vào đây** (`.claude/CLAUDE.md` §6) —
`find src/BE -iname '*.csproj'` cho số project trên đĩa, còn
`grep -c '<Project Path=' src/BE/PlatformManager.slnx` cho số project **thật sự
nằm trong solution đang build**. Hai con số này có thể khác nhau, và đã lệch ít
nhất 2 lần: 8 → 10 khi thêm 2 project test (2026-08-24), rồi giảm lại khi module
nghiệp vụ bị gỡ (2026-08-29). Bản trước của bảng này chép cứng "8 project" và
sai suốt từ đó.

| Có thật hôm nay (2026-08-29) | Sẽ thành |
| --- | --- |
| `Core.Domain`, `Core.Application`, `Core.Infrastructure`, `Core.Api` (2026-09-09), `Core.Persistence` (✅ tách 2026-09-10, xem §DbContext) | tách thêm `Core.Common` → 6 project |
| ✅ `ApiControllerBase` **đã ở** `Core.Api` — **Q8, chốt và thi công 2026-09-09** (`src/BE/Core/PlatformManager.Core.Api/ApiControllerBase.cs:32`) | 4 controller Core chuyển sang nốt; xem §`Core.Api` giữ `ApiControllerBase` bên dưới |
| **Không còn project `Modules.*` nào trong solution** — module nghiệp vụ duy nhất (`Modules.DtiWeekly.*`) đã gỡ 2026-08-29 | `Business.{Domain,Application,Persistence,Infrastructure,Api}` khi module nghiệp vụ đầu tiên được dựng lại |
| `PlatformManager.Api` (host mỏng) | giữ nguyên |
| `Tests/` — `PlatformManager.ArchTests`, `PlatformManager.Core.UnitTests`, `PlatformManager.Core.IntegrationTests` | giữ nguyên |

**Chưa tồn tại:** `Core.Common`, mọi `Business.*`. **`Core.Persistence` đã tách 2026-09-10.**
**`Core.Api` đã dựng 2026-09-09** (Q8) — hiện chỉ chứa `ApiControllerBase`.
`PlatformManagerDbContext` và `CoreSeeder` hiện ở `Core.Persistence/` (§DbContext, khối 2026-09-10);
mọi controller hiện ở `PlatformManager.Api/Controllers/`.

**`IModuleRegistrar` thì đã thi công 2026-09-08** — nó KHÔNG còn nằm trong danh sách
"chưa tồn tại" ở trên. Xem §`IModuleRegistrar` bên dưới cho bảng neo `file:dòng`.

> **`src/BE/Modules/` đã XOÁ 2026-09-08.** Trước đó thư mục còn trên đĩa nhưng bên
> trong chỉ là `obj/` của lần build cuối (kiểm trước khi xoá: 36 file, **toàn bộ**
> dưới `obj/`; `git ls-files src/BE/Modules` trả 66 đường dẫn nhưng **không đường
> nào còn tồn tại trên đĩa** — chúng là file nguồn đã bị xoá từ đợt 2026-08-29).
> Nghĩa là lượt xoá này chỉ dọn rác build chưa theo dõi bởi git.
>
> Kiểm lại bất cứ lúc nào:
>
> ```bash
> test -e src/BE/Modules && echo "CÒN — đọc lại mục này" || echo "đã xoá"
> ```
>
> Đừng dựng lại thư mục `Modules/` theo mô hình `Modules.<Tên>.*` — mô hình đó đã
> bị thay bằng `Business.*` từ v3 (xem §Quyết định BE ngay dưới).

ArchTest ranh giới **`Core_MustNotReference_AnyModulesAssembly`** và
**`Modules_MustNotReference_OtherModules`** (cả hai ở
`src/BE/Tests/PlatformManager.ArchTests/CoreModuleBoundaryTests.cs`) **vẫn phải
giữ nguyên**, dù hiện không còn assembly `Modules.*` nào để chúng bắt. Rule thứ
nhất vẫn kiểm thật (nó quét tham chiếu của `Core.*`, không cần module tồn tại);
rule thứ hai hiện qua một cách hiển nhiên và là **bảo hiểm cho module đầu tiên
được dựng lại**. Mục "ArchTest cần có" bên dưới yêu cầu bỏ rule thứ hai — điều
đó chỉ áp dụng **sau** khi tầng nghiệp vụ đã thật sự tồn tại dưới tên
`Business.*`.

> Vì sao phải ghi việc này ra: bản trước của file không có dấu trạng thái nào và
> viết ở thì hiện tại mô tả, nên `.claude/agents/backend-expert.md` đã chép
> nguyên cây thư mục sang và agent được chỉ đạo tạo file vào 7 project không tồn
> tại. Luật `.claude/CLAUDE.md` §4 sinh ra từ chính ca này. Bản 2026-08-23 lặp
> lại đúng lỗi đó ở quy mô nhỏ hơn: nó chép cứng "8 project" và liệt
> `Modules.DtiWeekly.*` là "có thật hôm nay", nên mọi lượt review đọc bảng này
> (`doc/huong_dan/quy-uoc/tieu-chi-review.md` §1 bắt buộc đọc trước khi chấm)
> đều chấm theo một hiện trạng không còn đúng.

## Vấn đề

Sau đợt xây lại CoreBase (Identity/SysMenu/phân quyền) + nghiệp vụ DTI Weekly, cả BE và FE đều
đang **trộn Core và nghiệp vụ trong cùng project/thư mục**, không có ranh giới thật:

- BE: `PlatformManager.Domain/Entities/` chứa cả `SysMenu`/`SysMenuRole` (Core) lẫn
  `CriteriaGroup`/`Criteria`/`CriteriaAssessment`/`CriteriaEvidence` (DTI Weekly) trong cùng
  1 assembly. `PlatformManager.Application/Auth,Users,Menu,Permissions/` (Core) nằm ngang hàng
  `Criteria,CriteriaGroups,Assessments,Dashboard/` (DTI Weekly) trong cùng 1 assembly.
  **Bằng chứng đã rò rỉ thật**: `PlatformManager.Application/Common/PeriodRangeCalculator.cs`
  — logic tính tuần/kỳ riêng của DTI Weekly — đang nằm trong `Common/`, nơi đáng lẽ chỉ chứa
  CQRS/envelope dùng chung.
- FE: `modules/{login,doi-mat-khau,quan-tri-nguoi-dung,phan-quyen}` (Core) nằm ngang hàng
  `modules/{dashboard,danh-muc-dti}` (DTI Weekly), cùng 1 quy ước thư mục, không phân biệt được.

Người dùng xác nhận **sẽ có thêm tính năng nghiệp vụ khác** trên nền tảng này sau DTI Weekly, và
đã làm rõ (2026-08-16): các tính năng đó thuộc **1 khối nghiệp vụ thống nhất** (không phải nhiều
domain độc lập khác nhau) — DTI Weekly chỉ là tính năng đầu tiên trong khối đó. Đây là tín hiệu
thật, nên áp dụng ranh giới Core↔Business ngay bây giờ là đúng thời điểm (theo tinh thần "Nhóm
A/B": không xây trước khi có nỗi đau thật, nhưng nỗi đau này đã hiện diện).

## Nghiên cứu thực tế đã tham khảo

**BE** — mô hình phân lớp Core/Business trong .NET, các nguồn được trích dẫn rộng rãi:
- **Jason Taylor — Clean Architecture template**: baseline 4-layer (Domain/Application/
  Infrastructure/Web) — đúng mô hình "1 khối nghiệp vụ thống nhất" (không tách nhiều module độc
  lập), khớp chính xác cách PlatformManager tổ chức `Business.*` sau khi làm rõ phạm vi.
- **Kamil Grzybek — "Modular Monolith with DDD"**, **NET-Architecture-Templates/ModularMonolith**,
  **meysamhadeli/booking-modular-monolith**: mô hình N-module độc lập (mỗi domain khác biệt = 1 bộ
  project riêng) — **không áp dụng cho PlatformManager hiện tại** vì chỉ có 1 khối nghiệp vụ, ghi
  lại ở mục "Khi nào tách thành module độc lập thật" bên dưới để dùng khi ngưỡng đó thật sự tới.
- **Milan Jovanović**: ArchTest là lớp bảo vệ ranh giới chính (không phải chỉ đặt tên project cho
  đẹp) — vẫn áp dụng dù chỉ có 2 đơn vị (Core, Business).
- **ABP Framework**: tiền lệ thật cho pattern `IModule`/`[DependsOn]` khi thật sự cần nhiều module
  — chưa cần ở quy mô 1 khối nghiệp vụ hiện tại.

**FE** — mô hình tổ chức Angular cho nhiều feature trong 1 app:
- **angulararchitects.io (Manfred Steyer)**: baseline chuẩn cho 1 Angular CLI app là cấu trúc
  thư mục domain/layer + `tsconfig.json` path alias, enforce bằng lint (Sheriff hoặc
  `eslint-plugin-import`), **KHÔNG cần Nx cho tới khi có ≥2 app thật**.
- **Cộng đồng Nx**: nhiều team rút lui khỏi Nx khi chỉ có 1 app — tính năng affected-build/cache
  không tạo giá trị thật, chi phí bảo trì tăng không tương xứng.
- Dự án hiện tại: 1 app, 1 team, chưa có app thứ 2 nào trên lộ trình → **chưa đủ ngưỡng dùng Nx**.

## Quyết định BE — Core ↔ Business (2 tầng), mỗi tầng 5 lớp (Domain/Application/Persistence/Infrastructure/Api)

> v3 (2026-08-16): sau khi xem cấu trúc thật trong Visual Studio, người dùng làm rõ 3 điểm quan
> trọng, ĐỔI HƯỚNG so với bản v1/v2 trước đó:
> 1. Mỗi tầng có **5** project (không phải 3/4) — tách riêng `Persistence` (EF Core/DbContext)
>    khỏi `Infrastructure` (phần còn lại — tích hợp ngoài DB).
> 2. Mỗi tầng tự có `Api` riêng — thay vì dùng chung 1 `PlatformManager.Api`.
> 3. **Quan trọng nhất — đổi mô hình**: nghiệp vụ tương lai là **1 khối thống nhất**, không phải
>    nhiều domain độc lập. Vì vậy **không** dùng mô hình "N-module độc lập" (`Modules.<Tên>.*`)
>    — chỉ có đúng 2 tầng ngang hàng: `Core.*` và `Business.*` (đổi tên từ
>    `Modules.DtiWeekly.*` — bỏ hẳn ý niệm "DtiWeekly là 1 trong nhiều module", DTI Weekly giờ chỉ
>    là 1 nhóm tính năng BÊN TRONG `Business.*`, không phải tên project).

```
src/BE/
├── Directory.Build.props / Directory.Packages.props   ← vẫn ở src/BE/, không đổi
├── Core/
│   ├── PlatformManager.Core.Domain/            ← BaseEntity, DomainException, EntityId,
│   │                                              ConflictException, SysMenu, SysMenuRole
│   ├── PlatformManager.Core.Common/            ← [CHỐT 2026-08-23] utility THUẦN, zero-dependency:
│   │                                              không reference Domain/Application/EF/ASP.NET.
│   │                                              Chặn utility bò dần vào Domain — thứ luôn xảy ra
│   │                                              khi không có chỗ đặt hợp lệ
│   ├── PlatformManager.Core.Application/       ← Auth/, Users/, Menu/, Permissions/, Common/
│   │                                              (ICommand, IQuery, ApiResult, ErrorDescriptor,
│   │                                              behaviors, interface I*Repository...)
│   ├── PlatformManager.Core.Persistence/       ← PlatformManagerDbContext, EF Configuration cho
│   │                                              SysMenu/SysMenuRole + AppUser/AppRole,
│   │                                              Interceptors, CoreSeeder.cs,
│   │                                              EfConfigurationAssembly, repository
│   │                                              implementation dùng EF trực tiếp
│   ├── PlatformManager.Core.Infrastructure/    ← phần CÒN LẠI không phải EF: IdentityService/
│   │                                              UserAdminService/UserLookupService (orchestrate
│   │                                              qua UserManager/SignInManager, không tự viết
│   │                                              LINQ/DbContext)
│   └── PlatformManager.Core.Api/               ← ApiControllerBase (base class BẮT BUỘC của mọi
│                                                  controller, kể cả controller nghiệp vụ — Q8)
│                                                  + AuthController, UsersController,
│                                                  MetaController, PermissionsController
├── Business/                                   ← KHÔNG lồng thêm "DtiWeekly/" — Business LÀ đơn
│                                                  vị, DTI Weekly chỉ là 1 nhóm tính năng bên trong
│   ├── PlatformManager.Business.Domain/           ← Criteria, CriteriaGroup, CriteriaAssessment,
│   │                                                 CriteriaEvidence — và MỌI entity nghiệp vụ
│   │                                                 thêm sau này (không tạo project Domain mới)
│   ├── PlatformManager.Business.Application/      ← Criteria/, CriteriaGroups/, Assessments/,
│   │                                                 Dashboard/, PeriodRangeCalculator.cs — và
│   │                                                 MỌI feature nghiệp vụ thêm sau này (thư mục
│   │                                                 con mới trong CÙNG project, không tạo
│   │                                                 project Application mới)
│   ├── PlatformManager.Business.Persistence/      ← EF Configuration cho entity nghiệp vụ,
│   │                                                 repository implementation dùng EF,
│   │                                                 BusinessSeeder.cs (đổi tên từ DtiWeeklySeeder)
│   ├── PlatformManager.Business.Infrastructure/   ← phần CÒN LẠI không phải EF (hiện gần như
│   │                                                 trống — chưa có tích hợp ngoài nào, đúng dự
│   │                                                 kiến, không bịa thêm code cho có)
│   └── PlatformManager.Business.Api/              ← CriteriaController, CriteriaGroupsController,
│                                                     DashboardController, ImportController — và
│                                                     controller của MỌI feature thêm sau này
├── PlatformManager.Api/                        ← HOST MỎNG — Program.cs, cấu hình
│                                                  cookie/CORS/exception-handler, gọi
│                                                  services.AddCoreModule(config) +
│                                                  services.AddBusinessModule(config), rồi GỘP
│                                                  controller từ Core.Api + Business.Api qua
│                                                  AddControllers().AddApplicationPart(...) —
│                                                  KHÔNG tự định nghĩa controller riêng nào
└── Tests/PlatformManager.ArchTests/            ← mở rộng rule (xem dưới)
```

**Nguyên tắc phụ thuộc bắt buộc:**
```
Core.Domain          → không phụ thuộc gì
Core.Application     → Core.Domain
Core.Persistence     → Core.Application, Core.Domain
Core.Infrastructure  → Core.Application, Core.Domain, Core.Persistence (cần kiểu AppUser/AppRole
                        do Persistence định nghĩa, để dùng UserManager<AppUser>)
Core.Api             → Core.Application, Core.Domain (controller chỉ cần ISender/MediatR — KHÔNG
                        cần biết Persistence/Infrastructure trực tiếp)

Business.Domain          → Core.Domain
Business.Application     → Core.Application, Core.Domain, Business.Domain
Business.Persistence     → Core.Persistence (cần type PlatformManagerDbContext dùng chung),
                            Business.Application, Business.Domain
Business.Infrastructure  → Core.Infrastructure, Business.Application, Business.Domain
Business.Api             → Core.Api (chỉ để kế thừa ApiControllerBase — Q8, 2026-09-09),
                            Business.Application, Business.Domain

PlatformManager.Api (host)  → MỌI project (Core.* + Business.*) — nơi DUY NHẤT được thấy cả 2
                                tầng, kể cả Persistence (cần cho design-time migration factory) và
                                Api (cần cho AddApplicationPart)

❌ Core.*                → Business.* (Core không được biết về nghiệp vụ)
❌ Core.Api/Business.Api → *.Persistence/*.Infrastructure trực tiếp (controller chỉ nói chuyện
                            qua MediatR, không tự inject repository/DbContext)
```

**Vì sao tách `Persistence` khỏi `Infrastructure`**: `Infrastructure` trước đây gộp 2 mối quan
tâm khác nhau — (a) CÁCH LƯU DỮ LIỆU (EF Core, DbContext, repository implementation) và (b) TÍCH
HỢP HỆ THỐNG KHÁC (email, file storage, API 3rd-party — chưa có nhưng sẽ có khi mở rộng). Gộp
chung khiến 1 class đổi vì lý do DB và 1 class khác đổi vì lý do tích hợp ngoài cùng nằm 1 project
— vi phạm Single Responsibility ở mức project. Tách riêng: đổi công nghệ lưu trữ chỉ đụng
`*.Persistence`, không đụng `*.Infrastructure` và ngược lại.

**Vì sao Core và Business mỗi bên tự có `Api` riêng**: controller của 1 tầng chỉ thấy
`*.Application` của CHÍNH tầng đó — không thể lỡ tay gọi Command/Query thuộc tầng khác dù đang
code trong file controller (ranh giới ép buộc bởi compiler, không chỉ quy ước). Host
`PlatformManager.Api` gộp lại bằng `AddControllers().AddApplicationPart(typeof(SomeMarker)
.Assembly)` cho từng assembly `*.Api` đã đăng ký — cơ chế chuẩn của ASP.NET Core cho đúng bài
toán "controller đến từ assembly khác", không cần thư viện ngoài.

### 🚧 ĐÃ CHỐT — ĐANG THI CÔNG: `Core.Api` giữ `ApiControllerBase` (Q8, 2026-09-09)

Quyết định người dùng 2026-09-09. Đây là lý do **thứ hai** để `Core.Api` tồn tại, độc lập với
lý do "controller mỗi tầng chỉ thấy `Application` của tầng đó" ở đoạn trên: nó là chỗ hợp lệ
**duy nhất** cho base class dùng chung của mọi controller.

| Có thật hôm nay (2026-09-09) | Sẽ thành |
| --- | --- |
| ✅ `ApiControllerBase` ở `PlatformManager.Core.Api` — thi công 2026-09-09, `src/BE/Core/PlatformManager.Core.Api/ApiControllerBase.cs:32` | giữ nguyên |
| Host là project **duy nhất** có controller, nên base class ở đó chưa gây vấn đề gì | `Business.Api` reference `Core.Api` để kế thừa nó |
| Chưa có cạnh `Business.Api → Core.Api` — `Core.Api` đã có, `Business.Api` thì chưa | cạnh đó **được phép**, khai ở khối "Nguyên tắc phụ thuộc bắt buộc" trên |

**Vì sao KHÔNG để nguyên ở host.** Base class này **bắt buộc bằng máy** — mọi controller phải
kế thừa nó, canh bởi `EveryController_Inherits_ApiControllerBase`
(`src/BE/Tests/PlatformManager.ArchTests/ControllerBaseInheritanceTests.cs:55`). Nhưng host
**phải** reference `Business.Api` để gọi `AddApplicationPart`; nếu `Business.Api` reference
ngược lại host để lấy base class thì thành vòng tròn tham chiếu, compiler từ chối. Nghĩa là
để base class ở host không phải "chưa gọn" — nó làm `Business.Api` **không dựng được**.

**Vì sao KHÔNG đặt vào `Core.Application`.** `ApiControllerBase` kế thừa `ControllerBase` và
mang `[Authorize]`, tức nó **là** ASP.NET Core. `Core.Application` bị cấm chạm
`Microsoft.AspNetCore` — tiền tố nằm trong `ForbiddenAssemblyPrefixes` của
`src/BE/Tests/PlatformManager.ArchTests/LayerDependencyTests.cs:32`. Đặt vào đó là làm đỏ một
gate đang canh đúng thứ nó sinh ra để canh.

**Cạnh `Business.Api → Core.Api` không mở đường nào tới Persistence/Infrastructure** — nó là
tham chiếu ngang giữa hai project cùng tầng Api. Khi viết rule `📐`
`Api_MustNotReference_PersistenceOrInfrastructure_Directly` (§ArchTest cần có), giữ đúng phạm
vi đó: cấm reference `*.Persistence`/`*.Infrastructure`, **không** cấm reference `*.Api` khác.

**Vì sao KHÔNG dùng mô hình N-module (`Modules.<Tên>.*`) nữa**: mô hình đó (đã thử ở v1/v2, xem
lịch sử) đúng khi có **nhiều domain nghiệp vụ độc lập thật** — nhưng người dùng xác nhận nghiệp vụ
tương lai vẫn là 1 khối thống nhất. Nếu ép theo khuôn N-module trong khi chỉ có 1 khối, sẽ phải
đặt tên giả-nhiều-module (`Modules.DtiWeekly.*`) cho 1 thứ thực chất chỉ có 1 — gây hiểu lầm (đúng
như người dùng phát hiện qua Visual Studio) và tạo áp lực sai phải "tách domain" khi thêm tính
năng, dù tính năng đó thuộc cùng 1 khối nghiệp vụ. Xem mục "Khi nào tách thành module độc lập
thật" bên dưới cho đúng thời điểm quay lại mô hình N-module.

**Thêm tính năng nghiệp vụ mới (vd sau DTI Weekly) — KHÔNG tạo project mới**: thêm 1 thư mục
feature mới trong CÙNG `PlatformManager.Business.Application/<TênFeature>/` (đúng vertical slice
đã có), entity mới (nếu có) vào `PlatformManager.Business.Domain/`, EF Configuration mới vào
`PlatformManager.Business.Persistence/`, controller mới vào `PlatformManager.Business.Api/
Controllers/`. Không tạo bộ 5 project mới cho mỗi tính năng — `Business.*` là 1 khối duy nhất chứa
mọi tính năng nghiệp vụ.

### `IModuleRegistrar` — seam để tầng nghiệp vụ tự cắm vào Core (CHỐT 2026-08-23)

> **Đây là thay đổi so với bản trước**, và lý do là bối cảnh đổi chứ không phải đổi ý.
> Bản trước viết *"CHƯA xây `IModule`/module-loader động — chỉ 2 đơn vị, chưa có gì để khái quát
> hoá"*. Lập luận đó đúng **khi PlatformManager là người tiêu thụ Core duy nhất**. Người dùng đã
> chốt 2026-08-23: **Corebase sẽ tái sử dụng ở nhiều dự án khác**. Khi đó seam này được dùng
> **một lần cho mỗi dự án**, không phải một lần duy nhất — nó thôi là trừu tượng hoá sớm.

Core khai **một** interface; mỗi tầng nghiệp vụ tự implement để đăng ký phần của mình:

```csharp
// Core.Application — Core KHÔNG reference tầng nghiệp vụ nào
public interface IModuleRegistrar
{
    string ModuleName { get; }                          // "Core", "Business", "Modules.Hrm"...
    void RegisterServices(IServiceCollection services, IConfiguration configuration);
    Assembly PersistenceAssembly { get; }               // để OnModelCreating quét EF Configuration
    Assembly? ApiAssembly { get; }                      // để AddApplicationPart; null nếu không có controller
}
```

Host (`PlatformManager.Api`) là nơi **duy nhất** biết cả hai tầng — nó gom mọi registrar rồi gọi
một lượt. Dự án mới chỉ cần implement `IModuleRegistrar` cho tầng nghiệp vụ của mình, **không
sửa một dòng nào trong `Core.*`**.

> ⚠️ Tham số `IConfiguration` của `RegisterServices` là **ngoại lệ đã khai** của luật
> `*.Application ⇏ IConfiguration`, không phải chỗ luật bị bỏ quên — điều kiện và giới hạn của
> ngoại lệ: [`huong_dan/quy-uoc/be-architecture.md`](huong_dan/quy-uoc/be-architecture.md)
> §"Ngoại lệ DUY NHẤT của luật `*.Application ⇏ IConfiguration`".

#### ✅ CÓ THẬT — thi công 2026-09-08 (đối chiếu cùng ngày)

Hợp đồng ở trên nay là code chạy thật, đúng 4 thành viên đã chốt, không thêm không bớt.

| Vai | Neo `file:dòng` |
| --- | --- |
| Hợp đồng — Core khai, Core không biết tên tầng nào | `src/BE/Core/PlatformManager.Core.Application/Modules/IModuleRegistrar.cs:29` |
| Core TỰ hiện thực nó | `src/BE/Core/PlatformManager.Core.Infrastructure/Modules/CoreModuleRegistrar.cs:26` — `ModuleName` `:28`, `PersistenceAssembly` `:39`, `ApiAssembly` `:62` |
| Cơ chế gom — đường DI + đường EF | `src/BE/Core/PlatformManager.Core.Infrastructure/Modules/ModuleRegistrationExtensions.cs:33` (`AddModules`), nộp `EfConfigurationAssembly` tại `:69` |
| Cơ chế gom — đường controller | cùng file, `:84` (`AddModuleApplicationParts`) |
| Danh sách tầng của dự án — nguồn sự thật DUY NHẤT | `src/BE/PlatformManager.Api/Modules/HostModuleRegistrars.cs:23` |
| Host nối đủ 3 đường | `src/BE/PlatformManager.Api/Program.cs:88` (dựng danh sách) · `:107` (ApplicationPart) · `:160` (DI + EF) |
| Design-time đọc CÙNG danh sách đó | `src/BE/PlatformManager.Api/PlatformManagerDbContextFactory.cs:128` |
| ArchTest canh | `src/BE/Tests/PlatformManager.ArchTests/ModuleRegistrarSeamTests.cs:58` (quên nối ⇒ đỏ) · `:127` · `:152` · `:202` (ba đường) |

**Core đi CHUNG đường với tầng nghiệp vụ, không có lối riêng.** Host không còn gọi
`AddCoreModule()` thẳng, và dòng `AddSingleton(new EfConfigurationAssembly(...))` đã rời khỏi
`AddCoreModule` — assembly Persistence của Core nay vào DI qua đúng `PersistenceAssembly` của
registrar. Đây là phần quyết định seam có dùng được hay chỉ là interface trang trí: một đường
code chỉ dành cho "người khác" sẽ không chạy lần nào cho tới khi người khác xuất hiện, và lần
đó là lúc tệ nhất để phát hiện nó hỏng.

**Đo được, không phải tuyên bố.** Canary chạy 2026-09-08: thêm một lớp hiện thực
`IModuleRegistrar` vào cây mã nguồn mà **không** thêm vào `HostModuleRegistrars.Create()` ⇒
đúng **1** test đỏ, nêu đích danh tên lớp và chỉ đúng chỗ phải sửa; gỡ canary ⇒ xanh lại.
Ba đường còn lại được chứng minh bằng một registrar **giả** dựng trong project test (không tạo
project module thật): dịch vụ của nó tới được DI, entity của nó vào được model EF **và nhận
luôn soft-delete do cơ chế Core áp**, assembly của nó thành `ApplicationPart`.

**Chưa có, nói rõ để không ai tưởng đã có:** `ApiAssembly` của Core là `null` — 4 controller Core
vẫn nằm trong project host. Nhánh "tầng có controller riêng" vì thế mới chỉ chạy trên registrar
giả của test, chưa có tầng sản phẩm nào đi qua nó.

> 🔄 **Sửa 2026-09-09 — mệnh đề NGUYÊN NHÂN của đoạn trên đã sai.** Bản trước viết
> *"(`Core.Api` chưa tách…)"*, mâu thuẫn thẳng với `:31` và `:36-37` của **chính file này**.
> `Core.Api` **đã dựng 2026-09-09** (Q8); lý do thật khiến `ApiAssembly` vẫn là `null` là
> **`Core.Api` chưa chứa controller nào** — trong đó mới chỉ có `ApiControllerBase`. Hai vế còn
> lại của đoạn ("`ApiAssembly` là `null`", "4 controller vẫn ở host") vẫn đúng nguyên. Lý do
> đầy đủ + điều kiện kích hoạt nay ghi tại chỗ:
> `src/BE/Core/PlatformManager.Core.Infrastructure/Modules/CoreModuleRegistrar.cs:41`.

**Đây là bản rút gọn của `IBoundedContext` trong `tham-khao-ngoai/vnr-successor/04-p3`** — giữ nguyên
tính đảo phụ thuộc (Core duyệt được mọi module mà không reference module nào), **bỏ** phần
mỗi-module-một-`DbContext` vì hệ này chỉ có 1 Postgres. Nếu sau này thật sự cần tách DbContext,
`IModuleRegistrar` mở rộng thêm được mà không phá chỗ đang dùng.

**KHÔNG xây** module-loader động kiểu `Manifest.cs`/bật-tắt-runtime của Orchard Core — đó là
nhu cầu khác (feature toggle lúc chạy), chưa có ca dùng.

### Đặt tên tầng nghiệp vụ — Core quy định SEAM, không quy định TÊN

Vì Corebase phục vụ nhiều dự án, và mỗi dự án có hình dạng nghiệp vụ khác nhau:

| Dự án có | Đặt tên | PlatformManager |
| --- | --- | --- |
| **Một** khối nghiệp vụ | `<Prefix>.Business.*` | ✅ đúng ca này |
| **Nhiều** domain độc lập thật | `<Prefix>.Modules.<Tên>.*` | — |

Core **không quan tâm** tên — nó chỉ thấy `IModuleRegistrar`. Vì vậy quyết định "một khối
`Business.*`" của PlatformManager vẫn đúng, mà dự án thứ hai không bị ép theo.

### DbContext — vẫn dùng chung 1 DbContext, sống ở `Core.Persistence`

1 Postgres duy nhất, FK xuyên tầng đã CHỐT (`CriteriaAssessment.OwnerId → AppUser.Id`).
`PlatformManagerDbContext` sống ở `Core.Persistence`. Cơ chế `EfConfigurationAssembly` (mỗi tầng
tự đăng ký assembly `*.Persistence` của mình qua DI, `OnModelCreating` gọi
`ApplyConfigurationsFromAssembly()` cho từng assembly đã đăng ký) GIỮ NGUYÊN không đổi — đã hoạt
động đúng, không có lý do sửa. Design-time factory (`IDesignTimeDbContextFactory`, dùng cho
`dotnet ef`) đặt ở `PlatformManager.Api` (host) — nơi duy nhất biết đủ mọi assembly `*.Persistence`
cần quét.

**Chốt 2026-09-04, đã thi công (đo cùng ngày — xem `doc/cau-truc-database.md` §5.3):**
`Migrations/*.cs` và `ModelSnapshot` cũng thuộc host, cùng lý do với factory trên — nhưng lý do
thật sự quan trọng là hai dự án **không thể cùng sở hữu một `ModelSnapshot`**. Riêng
`MigrationsAssembly` khai **chỉ ở design-time factory**, cố ý không khai ở Core: khai ở Core buộc
Core gọi tên assembly của host, đúng thứ Corebase mang sang dự án 2 rồi trỏ sai. Quyết định giữ **một** DbContext ở trên chính là đánh đổi được cân ở đó
(khoá ngoại core↔business đổi lấy snapshot dùng chung).
📖 Vấn đề, chốt, hệ quả và thao tác cho dự án 2: đọc `doc/cau-truc-database.md` §5.3

#### ✅ CÓ THẬT — tách `Core.Persistence` khỏi `Core.Infrastructure` (chốt + thi công + đối chiếu 2026-09-10)

Quyết định người dùng 2026-09-10: **tách trước khi dựng bất kỳ project `Business.*` nào**. Lý do:
cạnh `Business.Persistence → Core.Persistence` ở §"Nguyên tắc phụ thuộc bắt buộc" tồn tại để tầng
nghiệp vụ dùng chung **kiểu** `PlatformManagerDbContext`; chừng nào kiểu đó còn ở
`Core.Infrastructure` thì `Business.Persistence` chỉ lấy được nó bằng cách reference
`Core.Infrastructure` — cạnh đồ thị đích không khai. Phạm vi: **chỉ** `Core.Persistence` (không
tách `Core.Common`), refactor cấu trúc — **không đổi hành vi, không đổi schema, không migration**.

**Cái gì chuyển, cái gì ở lại** — 17 file chuyển bằng `mv` (không `git mv`), nội dung **giống
hệt** blob `HEAD` sau khi chuẩn hoá CRLF (17/17):

| Từ `Core.Infrastructure/…` | Sang `Core.Persistence/…` |
| --- | --- |
| `Persistence/` — `PlatformManagerDbContext`, 5 `*Configuration`, `AuditInterceptor`, 3 repository, `UnitOfWork`, `CoreSeeder`, `BootstrapOptions`, `EfConfigurationAssembly` | gốc project (bỏ tầng thư mục `Persistence/` thừa), giữ `Configurations/`, `Interceptors/`, `Repositories/` |
| `Identity/AppUser.cs`, `Identity/AppRole.cs` | `Identity/` — `IdentityDbContext<AppUser, AppRole, Guid>` buộc kiểu này đi cùng DbContext |
| `Persistence/Migrations/sql/` | `Migrations/sql/` — vẫn ở Core, đúng chốt §5.3 |
| **Ở lại** `Core.Infrastructure`: `IdentityService`/`UserAdminService`/`UserLookupService`, `AddCoreModule` (đăng ký DI + `UseNpgsql`), `CoreModuleRegistrar` | — `AddCoreModule` vẫn là composition duy nhất của Core; `Npgsql` không vào `Core.Persistence` |

| Vai | Neo `file:dòng` (đối chiếu 2026-09-10) |
| --- | --- |
| Project nằm trong solution | `src/BE/PlatformManager.slnx:5` |
| Chỉ hai cạnh ra: Domain + Application | `src/BE/Core/PlatformManager.Core.Persistence/PlatformManager.Core.Persistence.csproj:37` · `:38` |
| DbContext · `AppUser` | `src/BE/Core/PlatformManager.Core.Persistence/PlatformManagerDbContext.cs:26` · `src/BE/Core/PlatformManager.Core.Persistence/Identity/AppUser.cs:11` |
| `Core.Infrastructure → Core.Persistence` | `src/BE/Core/PlatformManager.Core.Infrastructure/PlatformManager.Core.Infrastructure.csproj:67` |
| Host thấy Persistence tường minh | `src/BE/PlatformManager.Api/PlatformManager.Api.csproj:44` |
| Registrar nộp assembly Persistence — dòng không đổi, kết quả tự theo kiểu | `src/BE/Core/PlatformManager.Core.Infrastructure/Modules/CoreModuleRegistrar.cs:39` |
| `MigrationsAssembly` vẫn là lời gọi duy nhất, ở factory | `src/BE/PlatformManager.Api/PlatformManagerDbContextFactory.cs:120` |
| ArchTests nhìn thấy assembly mới | `src/BE/Tests/PlatformManager.ArchTests/ArchTestSourceAccess.cs:57` · `src/BE/Tests/PlatformManager.ArchTests/CoreMustNotKnowBusinessNameTests.cs:156` |
| Luật mới — `*.Persistence ⇏ *.Infrastructure/*.Api` | `src/BE/Tests/PlatformManager.ArchTests/PersistenceLayerBoundaryTests.cs:45` (assembly) · `:70` (`.csproj`) |
| Đường dẫn artifact `.sql` | `src/BE/Tests/PlatformManager.ArchTests/MigrationsLocationTests.cs:153` · `src/BE/Tests/PlatformManager.Core.IntegrationTests/PostgresFixture.cs:200` |

**Vì sao giữ namespace cũ trong assembly mới (nợ có chủ đích).** `PlatformManagerDbContextModelSnapshot.cs`
và `…_InitialCreate.Designer.cs` ở host mang `using PlatformManager.Core.Infrastructure.Persistence;`
và `[DbContext(typeof(PlatformManagerDbContext))]`. Đó là tham chiếu **lúc biên dịch**, không chỉ
là tên kiểu dạng chuỗi. Đổi namespace thì hai file đó không biên dịch được nữa ⇒ buộc phải sửa
tay snapshot hoặc sinh migration — cả hai đều ngoài phạm vi lượt này. Trả nợ bằng một lượt
riêng: đổi namespace + `migrations add` sinh migration **rỗng** để snapshot đuổi kịp. Lượt đó cần
người dùng chốt.

**Nghiệm thu — đo 2026-09-10:**

| # | Phép thử | Kết quả |
| --- | --- | --- |
| 1 | `dotnet build src/BE/PlatformManager.slnx -c Release` | ✅ 0 cảnh báo, 0 lỗi (baseline cùng ngày cũng 0/0) |
| 2 | `grep -c '<Project Path=' src/BE/PlatformManager.slnx` | ✅ 8 → 9, bằng số `*.csproj` trên đĩa (9) |
| 3 | Đồ thị | ✅ `Core.Persistence.csproj` chỉ 2 `ProjectReference` (Domain, Application); `Core.Api.csproj` chỉ nhắc Persistence trong chú thích cấm; `MigrationsAssembly(` đúng 1 lời gọi thật, ở factory |
| 4 | `dotnet test` ArchTests · UnitTests | ✅ 68 → **70** (2 luật `PersistenceLayerBoundaryTests`) · 197 → **197** |
| 5 | **Canary** — một file trong `Core.Persistence`: controller kế thừa thẳng `ControllerBase` + literal `"DtiWeekly"` + dùng kiểu của `Core.Api`, kèm `ProjectReference` → `Core.Api` | ✅ đúng **4** test đỏ: `CoreSource_MustNotContain_BusinessNameStringLiteral`, `EveryController_Inherits_ApiControllerBase`, và cả hai luật `PersistenceLayerBoundaryTests`. Hoàn tác ⇒ 70/70 xanh |
| 6 | `dotnet ef migrations has-pending-model-changes --project PlatformManager.Api --startup-project PlatformManager.Api` | ✅ *"No changes have been made to the model since the last migration."* — không cần kết nối DB |
| 7 | `grep` đường dẫn cũ `Core.Infrastructure/Persistence` · `Core.Infrastructure/Identity/App*` trong `doc/ spec/ .claude/ src/` | ✅ chỉ còn: bảng "Từ → Sang" ngay trên, ghi chú lịch sử `doc/cau-truc-database.md` §5.3 nghiệm thu #1, và file lịch sử `doc/cau-truc-database.sql` (banner `TÀI LIỆU LỊCH SỬ`, cố ý không sửa) |
| 8 | `bash .claude/check-docs.sh` | ✅ PASS |
| 9 | `dotnet test` `Core.IntegrationTests` | ⏸ **chưa đo** — Docker không chạy trên máy thi công (`docker version`: không kết nối được daemon). Phần **chưa được xác minh bằng chạy thật**: `PostgresFixture` đọc `.sql` từ đường dẫn mới, và host boot thật với assembly mới. Phép thử 1 + 4 + 6 phủ việc biên dịch, model EF và đường dẫn tĩnh (`MigrationsLocationTests` kiểm cùng thư mục), nhưng không thay được lần chạy đó |

### ArchTest cần có

> ### 🚧 Trạng thái từng rule — đối chiếu 2026-09-01
>
> Mục này trước đây liệt 5 rule **không nhãn**, nên không phân biệt được *"đích đến chưa thi công"*
> với *"bị bỏ sót"*. Đối chiếu thật:
>
> | Rule | Trạng thái | Vì sao |
> |---|---|---|
> | `Core_MustNotReference_Business` | ✅ **Đã có, TÊN KHÁC** | Thi công dưới tên `Core_MustNotReference_AnyModulesAssembly` (`src/BE/Tests/PlatformManager.ArchTests/CoreModuleBoundaryTests.cs`). Cùng một luật |
> | `Core_MustNotKnowBusinessName` | ✅ **Đã có** (đối chiếu 2026-09-06) | Thi công dưới tên `CoreSource_MustNotContain_BusinessNameStringLiteral` (`src/BE/Tests/PlatformManager.ArchTests/CoreMustNotKnowBusinessNameTests.cs:80`), kèm 3 test tự-kiểm bộ dò (`:171`, `:201`, `:220` — 🔄 neo lại 2026-09-10: bộ cũ `:162`/`:192`/`:211` đã trôi, trỏ vào dòng trống/chú thích). 🔄 LẬT 2026-09-06 — ô này giữ nhãn `🚧 Đang thi công 2026-09-01` cho tới lượt đối chiếu này, tức 5 ngày sau khi test đã vào code |
> | `Api_MustNotReference_PersistenceOrInfrastructure_Directly` | ✅ **Đã có** (thi công + đối chiếu 2026-09-09) | `src/BE/Tests/PlatformManager.ArchTests/ApiLayerBoundaryTests.cs:61`, kèm luật thứ hai ở mức văn bản `.csproj` (`:109`). Xem khối 🔄 ngay dưới bảng |
> | `Application_MustNotReference_PersistenceOrInfrastructure` | ✅ **Đã có** (thi công + đối chiếu 2026-09-10) | Hai mức + một ca đối chứng ở `src/BE/Tests/PlatformManager.ArchTests/ApplicationLayerBoundaryTests.cs`: mức assembly `:39`, mức `.csproj` `:61`, ca `Business.Application → Core.Persistence` dựng tay `:100`. Áp cho **mọi** `*.Application`, không riêng Core. Sinh ra từ finding của `core-reviewer` ngay sau lượt tách `Core.Persistence`: trước đó `Core.Application` chỉ bị chặn nhờ **vòng tham chiếu** (hệ quả phụ, không phải luật), còn `Business.Application → Core.Persistence` không có gì chặn. Canary 2026-09-10 đỏ ở cả hai mức, mỗi mức đỏ đúng vì luật mới |
> | `OnlyHostApi_MustReference_BothUnits` | 📐 **ĐÍCH ĐẾN** | `Business.*` chưa tồn tại. Kiểm: `find src/BE -name "*.csproj" -not -path "*/obj/*" \| grep Business` → **PASS khi không dòng nào ra**, tức chưa có tầng nghiệp vụ để luật này đo |
> | `Core_Common_MustHaveZeroProjectReference` | 📐 **ĐÍCH ĐẾN** | `Core.Common` chưa là project riêng — hiện chỉ là thư mục `Common/` bên trong từng project |
>
> Hai rule `📐` còn lại **viết bây giờ sẽ là test rỗng**: chúng quét một tập không có phần tử nào,
> nên xanh vĩnh viễn mà không đo gì. Đó đúng khuôn lỗi mà bộ test này vừa phải sửa **hai** lần. Viết
> chúng **cùng lượt** dựng `Business.*` đầu tiên, và khi đó nhớ kèm khẳng định *"tập quét được khác
> rỗng"*.
>
> 🔄 **LẬT 2026-09-09 — `Api_MustNotReference_PersistenceOrInfrastructure_Directly` không còn rỗng.**
> Lý do hoãn cũ (*"không có assembly `*.Api` theo module, chỉ có host và host thì PHẢI reference
> Infrastructure"*) hết đúng khi `Core.Api` được dựng (Q8, cùng ngày): đó là một assembly `*.Api`
> **không phải host**, nên tập quét có phần tử thật. Trước đó ranh giới của `Core.Api` chỉ được canh
> bằng một khối chú thích trong `src/BE/Core/PlatformManager.Core.Api/PlatformManager.Core.Api.csproj:18`
> — chú thích không làm test nào đỏ.
>
> Ba điều đã giữ đúng khi viết:
>
> - **Phạm vi** — mọi assembly `*.Api` **trừ** host `PlatformManager.Api` (host là composition root,
>   nó bắt buộc thấy Infrastructure). Tập quét dẫn xuất từ `ProductAssemblies.All`, không liệt kê tay.
> - **Không cấm cạnh `*.Api → *.Api`** — `Business.Api → Core.Api` là cạnh hợp lệ đã khai ở
>   §"Nguyên tắc phụ thuộc bắt buộc" (kế thừa `ApiControllerBase`). Chỉ hai đoạn tên
>   `.Persistence`/`.Infrastructure` bị cấm.
> - **Khẳng định tập quét khác rỗng** — `ApiLayerBoundaryTests.cs:68` (mức assembly) và `:121` + `:148`
>   (mức `.csproj`). Không có nó thì luật này chính là thứ nó sinh ra để chống.
>
> **Vì sao có luật THỨ HAI ở mức `.csproj`.** `GetReferencedAssemblies()` chỉ thấy assembly thật sự
> có code chạm tới (giới hạn đã đo 2026-08-28, ghi ở `LayerDependencyTests.cs:22`). Canary 2026-09-09
> chứng minh hệ quả: thêm `<ProjectReference>` tới `Core.Infrastructure` vào `Core.Api.csproj` mà chưa
> gọi gì ⇒ **chỉ luật `.csproj` đỏ**, luật assembly vẫn xanh; thêm tiếp một lời gọi thật ⇒ **cả hai
> đỏ**. Bước "thêm reference cho tiện" là bước ĐẦU TIÊN của mọi ca phá ranh giới, nên nó phải bị bắt
> ngay ở đó.
>
> **Bổ sung 2026-09-08 — một rule THỨ SÁU, không có trong danh sách gốc.**
> `Registrar_MustBeWiredIntoHost`: mọi lớp hiện thực `IModuleRegistrar` trong cây `src/BE` (trừ
> `Tests/`) phải có mặt trong `HostModuleRegistrars.Create()`. Thi công dưới tên
> `EveryRegistrarDeclaredInSource_IsWiredIntoHostList`
> (`src/BE/Tests/PlatformManager.ArchTests/ModuleRegistrarSeamTests.cs:58`).
>
> Rule này **không rỗng** dù `Business.*` chưa tồn tại — nó đo trên `CoreModuleRegistrar` có
> thật, và ca đối chứng của bộ dò chạy trên mẫu văn bản dựng trong test. Kỳ vọng lấy từ VĂN BẢN
> mã nguồn, thực tế lấy từ DANH SÁCH của host: hai nguồn độc lập, đúng điều kiện để test không
> tự hỏi mình rồi tự trả lời.

- `Core_MustNotReference_Business` — `PlatformManager.Core.*` không được `GetReferencedAssemblies()`
  ra bất kỳ assembly `PlatformManager.Business.*` nào.
- `Api_MustNotReference_PersistenceOrInfrastructure_Directly` — mọi assembly `*.Api` không được
  reference `*.Persistence`/`*.Infrastructure` trực tiếp (chỉ qua `*.Application`).
- `OnlyHostApi_MustReference_BothUnits` — `PlatformManager.Api` (host) là project DUY NHẤT được
  phép reference cả `Core.*` lẫn `Business.*` cùng lúc; mọi project khác chỉ thuộc về đúng 1 tầng.
- `Core_MustNotKnowBusinessName` — **[CHỐT 2026-08-23]** không assembly `Core.*` nào được chứa
  string literal tên tầng nghiệp vụ (`"Business"`, `"DtiWeekly"`...). Mọi thứ đi qua
  `IModuleRegistrar`. Đây là rule giữ cho Corebase cắm được vào dự án thứ 2 mà không phải mổ lại.
- `Core_Common_MustHaveZeroProjectReference` — `Core.Common` không được reference project nào
  trong solution (chỉ BCL). Mất tính này thì nó thành `Core.Application` thứ hai.
- ~~Bỏ rule `Modules_MustNotReference_OtherModules`~~ — **đảo lại 2026-09-01: GIỮ.**
  Code đã giữ nó với lý do tốt hơn, ghi ngay tại chỗ
  (`src/BE/Tests/PlatformManager.ArchTests/CoreModuleBoundaryTests.cs:13-16`): mảng
  `ModuleAssemblies` hiện rỗng nên rule **qua một cách hiển nhiên**, nhưng nó là *bảo hiểm miễn
  phí* cho module đầu tiên được dựng lại — chỉ cần thêm bộ 3 assembly vào mảng là test tự phủ.
  Xoá đi thì lúc dựng module phải nhớ viết lại, mà đó chính là lúc dễ quên nhất.

  Đáng chú ý ở chỗ **code tự khai sự rỗng của mình** thay vì giả vờ đang bảo vệ. Đó là cách xử lý
  đúng cho một test chưa có gì để đo, và là lý do quyết định "bỏ" bị đảo.

## Quyết định FE — gom màn Core ra `platform/`, thêm ESLint boundary rule

> ### 🚧 Trạng thái khối cây bên dưới — đối chiếu 2026-09-06
>
> Việc **chuyển 4 màn Core sang `platform/` đã xong**; đó là phần "quyết định" của mục này và nó
> đúng. Nhưng khối cây viết ở thì hiện tại nên hai chi tiết đã lệch:
>
> | Trong khối cây | Thực tế `src/FE/src/app/` hôm nay |
> | --- | --- |
> | `platform/` có 4 thư mục | **5** — thêm `trang-chu/` (trang đích sau đăng nhập, cũng là màn Core) |
> | `modules/dashboard/` + `modules/danh-muc-dti/` | `modules/` **không tồn tại** — module DTI gỡ 2026-08-29 để xây lại (`doc/huong_dan/quy-uoc/fe-routing-guard.md` §1) |
>
> Giữ nguyên khối cây làm hình dạng ĐÍCH của tầng `modules/`; đừng đọc nó như hiện trạng.

```
src/FE/src/app/
├── core/              ← (giữ nguyên) singleton: http envelope, interceptor, auth service/guard, theme
├── shared/             ← (giữ nguyên) component dùng chung, dumb, đa feature
├── platform/           ← MỚI — các màn hình "Core" (đăng nhập/quản trị hệ thống), dùng lại được
│   ├── login/                  ← chuyển từ modules/login/
│   ├── doi-mat-khau/           ← chuyển từ modules/doi-mat-khau/
│   ├── quan-tri-nguoi-dung/    ← chuyển từ modules/quan-tri-nguoi-dung/
│   └── phan-quyen/             ← chuyển từ modules/phan-quyen/
└── modules/             ← TỪ NAY chỉ chứa tính năng NGHIỆP VỤ (không còn màn Core nào ở đây)
    ├── dashboard/
    └── danh-muc-dti/
```

Không đặt tên `modules/core/` (đụng tên với `core/` cấp trên gây nhầm) — dùng tên riêng
`platform/` cho rõ nghĩa "màn hình nền tảng, không phải nghiệp vụ cụ thể". Mỗi thư mục con giữ
nguyên cấu trúc `pages/components/services/models` đã có, chỉ đổi vị trí cha. FE **không** cần đổi
theo mô hình Core↔Business 2 tầng của BE — `modules/` phía FE vốn đã là "nhiều feature cùng 1 khối
nghiệp vụ" (dashboard + danh-muc-dti đều thuộc DTI Weekly), không có vấn đề đặt tên giả-nhiều-module
như BE gặp phải.

**Việc cần đổi khi chuyển**: 4 dòng `loadChildren` trong `app.routes.ts` (đường dẫn
`./modules/login/...` → `./platform/login/...`, tương tự 3 dòng còn lại) — đây là điểm chạm
chức năng DUY NHẤT. 2 chỗ chỉ là comment tài liệu (`styles.scss`, `auth-card.ts`) nên sửa cho
đúng nhưng không ảnh hưởng chạy được hay không.

**Không chuyển sang Nx / Angular multi-project workspace** ở đợt này — dự án hiện có 1 app, 1
team, chưa có app thứ 2 nào trên lộ trình, chi phí Nx (build/cache/learning) không có gì để đổi
lấy ở quy mô này. Cân nhắc lại khi thật sự có ≥2 app deploy riêng biệt dùng chung `core/`.

### ESLint boundary rule (gate — G8)

> 🔄 **LẬT 2026-09-06.** Đoạn này mở đầu bằng *"Dự án FE hiện **chưa cài ESLint** — cài qua
> `ng add @angular-eslint/schematics`"* và gọi G8 là "gate mới". Cả hai vế đã sai từ lâu:
> `src/FE/eslint.config.js` tồn tại, G8 đã khai ở đó (`eslint.config.js:89`), và bộ gate FE nay
> chạy tới **G12** chứ không dừng ở G7. Trạng thái thật của G8 hôm nay là **no-op có chủ đích** —
> `src/app/modules/` chưa tồn tại nên mảng zone rỗng và cả block bị bỏ hẳn (schema của
> `no-restricted-paths` đòi `zones` ≥ 1 phần tử). Đọc trạng thái từ
> `doc/huong_dan/wiki-core/fe/trien-khai/05-gate.md` §G8, không từ đoạn dưới.

Rule dùng là `no-restricted-paths` của `eslint-plugin-import` (đơn giản, phổ biến, không cần học
DSL riêng như Sheriff): chặn `modules/<feature-nghiệp-vụ>/*` import thẳng vào nội bộ 1 feature
nghiệp vụ khác (`modules/<feature-khac>/*`), trong khi vẫn cho phép import từ `core/`, `shared/`,
`platform/`. Khai đầy đủ thành gate **G8** trong
`doc/huong_dan/wiki-core/fe/trien-khai/05-gate.md`. Nâng cấp lên Sheriff chỉ khi số feature nghiệp vụ + độ phức tạp phân lớp nội bộ
từng feature tăng đủ lớn để 1 rule phẳng không còn đủ diễn đạt — chưa tới ngưỡng đó ở 2 feature.

## 🚧 Quyết định người dùng 2026-09-02 — Core giữ CƠ CHẾ, dự án cung cấp DỮ LIỆU

> **Vì sao tiêu đề vẫn `🚧` trong khi mọi dòng bảng ngay dưới đã đóng dấu `✅ ĐÃ TÁCH`.**
> Rà lại 2026-09-03: các dấu ✅ đó **đúng trong phạm vi chúng đo** — nhưng phạm vi ấy hẹp hơn ranh
> giới CoreBase. Toàn bộ phép nghiệm thu của lượt thi công chỉ quét `core/` và `shared/`, trong khi
> chính tài liệu này (khối cây `src/FE/src/app/` ở mục trên) khai `platform/` là **màn hình Core
> dùng lại được** — và `src/FE/eslint.config.js:89` nhắc lại đúng điều đó khi cố ý miễn gate G8 cho
> `platform/`. Lệnh quét hẹp hơn ranh giới thì dấu ✅ nó sinh ra cũng hẹp theo, mà không có gì
> trong dấu ✅ nói ra điều đó.
>
> Nới lệnh về đúng ranh giới thì lộ ngay chỗ chưa xong (đo được — xem §Nghiệm thu và §Bài học bên
> dưới). Sửa tiêu đề thành ✅ lúc này là đúng khuôn §4 `.claude/CLAUDE.md` cấm: sửa mô tả cho khớp
> mong muốn rồi coi việc là xong. Nhãn `🚧` bắt buộc kèm bảng *"có thật hôm nay → sẽ thành"*:

| Có thật hôm nay | Sẽ thành |
|---|---|
| Seam đã dựng đủ và host đã cấp dữ liệu cho từng cái (`ICoreMenuSeedSource`, `ICoreBootstrapAccountSource`, `CORE_ROUTES`, `CORE_BRANDING`, `createCorePreset`) | không đổi — phần cơ chế đã xong, và đó là phần khó nhất |
| Dữ liệu dự án đã rời `core/` và `shared/` — nhưng đó là **toàn bộ** những gì đã đo | đo trên **cả** `core/`, `shared/` **và** `platform/` |
| `platform/` đã nằm trong mọi phép quét §Nghiệm thu từ 2026-09-03, và phép thử **7** (tên sản phẩm) đã rỗng trên cả ba thư mục | phép thử **3** (route khai cứng) cũng rỗng — hiện chưa, xem hàng cuối bảng "Chỗ mang, đo được" |
| Phía BE: lưới đã nới từ `CoreSeeder.cs` lên toàn `src/BE/Core` (2026-09-03); phép thử **2**, **5**, **10** đều rỗng ở bề rộng mới | không đổi — nhưng nhãn hiển thị vẫn là **câu tiếng Việt dựng sẵn ở BE**, nay ở host thay vì Core. Nợ i18n, không phải nợ ranh giới |

### Phép thử đã chạy, và kết quả

Làm bản tĩnh của phép thử *"tách CoreBase sang dự án 2"*. Hai kết luận khác nhau:

| Chiều | Kết quả |
|---|---|
| **Cấu trúc** | ✅ Tách được. `Domain ← Application ← Infrastructure`, không project nào tham chiếu ngược lên `Api`. `CoreModuleBoundaryTests` + `CoreMustNotKnowBusinessNameTests` đang canh |
| **Dữ liệu** | ❌ **Chưa tách được** — Core mang theo thứ riêng của dự án này |

Chỗ mang, đo được:

| Nơi | Mang gì | Trạng thái |
|---|---|---|
| `CoreSeeder` (`Core.Infrastructure` lúc đo; nay `Core.Persistence`, 2026-09-10) | **4 mục menu** khai cứng: nhãn tiếng Việt, route `/quan-tri/nguoi-dung`, class icon `pi-shield` | ✅ **ĐÃ TÁCH 2026-09-02** — xem "Đã thi công (BE)" bên dưới |
| FE `core/auth/*.guard.ts` | **3 route** khai cứng: `/dang-nhap`, `/doi-mat-khau`, `/trang-chu` | ✅ **ĐÃ TÁCH 2026-09-02** — xem "Đã thi công (FE)" bên dưới |
| `CoreSeeder` (`Core.Infrastructure` lúc đo; nay `Core.Persistence`, 2026-09-10) | **2 email** `@platformmanager.local` + **2 tên hiển thị** tiếng Việt của tài khoản bootstrap | ✅ **ĐÃ TÁCH 2026-09-02** — xem "Đã thi công (BE) — tài khoản bootstrap" bên dưới |
| FE `core/theme/` (preset PrimeNG) | **10 hằng số màu** thương hiệu (`BRAND` `GOOD` `WARN` `BAD` `BG` `CARD` `TEXT` `MUTED` `LINE` `BORDER_STRONG`) | ✅ **ĐÃ TÁCH 2026-09-02** — xem "Đã thi công (FE — bảng màu + tên sản phẩm)" bên dưới |
| FE `core/title/page-title.strategy.ts` + template của `shared/components/{sidebar,auth-card}/` | **Tên sản phẩm** ở 3 chỗ — 2 chỗ đã biết, chỗ thứ 3 (`auth-card`) chỉ lộ ra lúc thi công | ✅ **ĐÃ TÁCH 2026-09-02** khỏi `core/` + `shared/` — xem mục cùng tên. ⚠️ Dấu này **không** phủ `platform/`, xem hàng dưới |
| FE `platform/**` — màn Core theo khối cây ở mục trên | **Tên sản phẩm** trong template lẫn `.ts` của màn Core | ✅ **ĐÃ TÁCH 2026-09-03** — xem "Đã thi công (FE) — đóng ba đường thoát" bên dưới. Canh bằng phép thử 7 |
| FE `platform/**` | **Route khai cứng** (`/trang-chu`, `/doi-mat-khau`) trong trang tự điều hướng, dù seam `CORE_ROUTES` đã có sẵn cho đúng việc đó | ✅ **ĐÃ TÁCH 2026-09-03** — xem "Đã thi công (FE) — route trong `platform/`" bên dưới. Canh bằng phép thử 3, nay **rỗng** |
| `ResourceKeys` (`Core.Application`) | **Nhãn hiển thị** của permission-key (`DisplayNames`), **và** một danh mục key ĐÓNG mà docstring lại ra chỉ thị cho module nghiệp vụ khai key *vào trong Core* | ✅ **ĐÃ TÁCH 2026-09-03** — seam `ICoreResourceKeySource`, host hiện thực bằng `AppResourceKeySource`. Canh bằng phép thử **10**. Còn một nửa nợ **i18n**, xem §nợ ngay dưới |

Dự án 2 sẽ khác cả nhãn, route lẫn ngôn ngữ. Hôm nay dùng lại Core thì **phải sửa vào trong
Core** — đúng thứ định nghĩa "CoreBase xong" ở §"Khi Core thật sự tách thành thư viện" loại trừ.

#### ✅ `ResourceKeys` — đã tách 2026-09-03, và nửa nợ còn lại KHÔNG cùng loại

Lớp `ResourceKeys` trong `Core.Application` từng mang **hai** khuyết tật khác hình dạng.
Ghi lại cả hai vì chúng kết thúc **khác nhau**, và gộp lại là mất đúng phần chưa xong.

| # | Khuyết tật | Kết cục |
|---|---|---|
| 1 | **Danh mục key ĐÓNG, mà docstring lại ra chỉ thị** *"module nghiệp vụ mới khai key của mình ở đây"* — tức Core chủ động bảo dự án ghi dữ liệu vào trong Core | ✅ **Hết 2026-09-03.** Danh mục ra host qua seam `ICoreResourceKeySource`; Core giữ ba đường tiêu thụ (ma trận GET, validator của PUT, seeder) vì cả ba là **cơ chế** |
| 2 | **Nhãn hiển thị** khai cứng (`DisplayNames[Import] = "Import CSV/Excel"`) | ⚠️ **Chuyển chỗ, chưa hết.** Nhãn nay ở `src/BE/PlatformManager.Api/Permissions/AppResourceKeySource.cs:53` — ra khỏi Core, nhưng vẫn là **câu chữ dựng sẵn ở BE** |

Khuyết tật 1 là cái đắt hơn, và đáng ghi lại kể cả khi đã hết: **một chỉ thị sai trong
docstring sinh ra vi phạm nhanh hơn nhiều so với một hằng số đặt nhầm chỗ.** Hằng số thì mỗi
lần thêm phải tự tay đặt vào; chỉ thị thì mỗi module mới đọc được lại làm theo một lần nữa,
và mỗi lần đều tin là mình đang làm đúng — nên nó không bao giờ bị báo lỗi.

**Nửa còn lại là nợ i18n, KHÔNG phải nợ ranh giới CoreBase — đừng gộp.** Ranh giới đã sạch:
phép thử 10 rỗng, và dự án 2 nay khai danh mục của nó mà không phải mổ vào Core. Thứ chưa
xong là `DisplayName` vẫn mang câu tiếng Việt thay vì một mã để FE tra bảng dịch — cùng lý
lẽ với `businessCode`. Bản đầy đủ của nợ đó ở
[`huong_dan/wiki-core/be/16-i18n-va-ma-loi.md`](huong_dan/wiki-core/be/16-i18n-va-ma-loi.md)
§4; `ICoreResourceKeySource.cs:6-13` cũng tự ghi nhận, kèm lý do vì sao lượt tách cố ý
không đụng vào (đổi kiểu giá trị là đổi hợp đồng `GET /api/admin/permissions/resources` và
kéo theo FE — hai việc khác nhau thì làm thành hai lượt).

Điều lượt tách **đã** đạt được cho nợ i18n: câu tiếng Việt đó nay nằm ở **một** chỗ tại host
thay vì trong Core, nên lần bật i18n chỉ phải sửa một điểm.

### Chốt

**Core giữ cơ chế, dự án cung cấp dữ liệu.** Cụ thể:

| | Core giữ | Dự án cung cấp |
|---|---|---|
| BE | *Cách* upsert menu, xử lý xoá mềm, gán role, seed tài khoản bootstrap | **Danh sách menu** qua một seam ở `Core.Application`; host hiện thực |
| FE | *Luật* chuyển hướng của guard (khi nào, đi đâu về **ngữ nghĩa**) | **Đường dẫn cụ thể** qua `InjectionToken`, app cung cấp |
| BE | *Số lượng và vai* tài khoản bootstrap — **đúng 2**, một `SuperAdmin` một `Admin`, `MustChangePassword = true`, mật khẩu đọc từ cấu hình | **Email + tên hiển thị** qua seam. `BootstrapOptions` **không đổi** |
| FE | *Cách* dẫn xuất thang màu (`mix`, `ramp`) và ánh xạ sang PrimeNG — ~143 dòng | **10 hằng số màu** + **tên ứng dụng**, qua `InjectionToken` |

Chi tiết quan trọng ở phía BE: mục cha tham chiếu bằng **`ParentCode`**, không phải `Id` — host
không biết `Id`, nó do `UpsertMenuAsync` sinh ra lúc chạy.

### Hai quyết định bổ sung 2026-09-02 — vì sao dừng ở ranh giới đó

Hai nhóm này **không cùng hình dạng** với nhóm menu, nên ranh giới tách cũng khác.

**Tài khoản bootstrap — Core giữ *số lượng*, không chỉ *cách làm*.** Việc có đúng hai tài
khoản, mỗi cái đúng một vai, là quyết định least-privilege đã chốt chứ không phải ngẫu nhiên;
biến nó thành danh sách tự do sẽ cho phép dự án 2 seed một tài khoản mang cả hai vai — đúng thứ
quyết định kia sinh ra để chặn. Ranh giới này còn giữ `BootstrapOptions` nguyên vẹn: nếu số
lượng tài khoản thành dữ liệu thì mật khẩu phải tra **theo khoá**, kéo theo biến môi trường,
tài liệu vận hành và phép nghiệm thu triển khai phải sửa theo.

Tên đăng nhập (`"SuperAdmin"`, `"Admin"`) **ở lại Core** vì chúng soi gương `Roles.*`. Đã kiểm
2026-09-02: mọi guard phân quyền dựa vào **vai**, không dựa vào tên —
`SuperAdminAccountGuard.cs:48` dùng `Roles.SuperAdmin`, `:69` và `:78` dùng `IsInRole`. Chỗ duy
nhất tên tồn tại như *dữ liệu* là `CoreSeeder.cs:49` (hằng `SuperAdminUserName`; neo lại 2026-09-10, trước ghi `:47`).

> 🔄 LẬT 2026-09-06 — số dòng ở câu trên là `:42` cho tới lượt đối chiếu này, và `:42` là một
> **tham số hàm dựng** (`ICoreBootstrapAccountSource bootstrapAccountSource`), không phải hằng tên
> đăng nhập. Chuỗi số dòng của chính câu này (`:35` → `:42` → `:47`) là bằng chứng cho thấy neo
> theo dòng vào một file đang được sửa liên tục sẽ trượt sau mỗi lượt, mà gate `check-docs.sh`
> mục 6 chỉ kiểm dòng có nằm trong file nên không bao giờ ĐỎ.

**Theme — file này vốn đã tách sẵn cơ chế khỏi dữ liệu.** Đo 2026-09-02: 32 giá trị hex, trong
đó **23** là `#000000`/`#ffffff` mà `mix()` dùng để đậm/nhạt (cơ chế), và **10** là hằng số đặt
tên ở đầu file (thương hiệu). Đẩy cả file ra host sẽ bắt mỗi dự án chép lại `ramp`/`mix` và
toàn thang `surface` 0–950 — nhân bản cơ chế, đúng thứ CoreBase sinh ra để tránh.

Cả hai seam **cố ý không có giá trị mặc định**, cùng khuôn với `CORE_ROUTES`: một bảng màu mặc
định sẽ biến "host quên khai" thành một giao diện sai thương hiệu mà không lỗi nào, còn một
email mặc định sẽ biến nó thành một tài khoản quản trị mang tên miền của dự án khác.

### Vì sao làm bây giờ, không đợi

Quy mô nhỏ — 4 mục menu và 3 route. Nhưng nó là **loại nợ đắt dần**: mỗi màn hình Core thêm
vào sau này lại thêm một mục menu khai cứng, và mỗi guard mới lại thêm một route. Làm lúc còn 7
mục thì rẻ; làm giữa lúc dựng dự án 2 thì vừa đắt vừa đúng lúc áp lực cao nhất.

### Nghiệm thu

```sh
# Ranh giới Core phía FE. platform/ NẰM TRONG — xem khối cây src/FE/src/app/ ở mục trên
# và src/FE/eslint.config.js:89 (miễn G8 cho platform/ vì đó là "màn Core").
FE_CORE="src/FE/src/app/core src/FE/src/app/shared src/FE/src/app/platform"

# Ranh giới Core phía BE = TOÀN BỘ src/BE/Core, không phải một file. Nới 2026-09-03,
# xem §"Bài học — vì sao ResourceKeys thoát lưới".
BE_CORE="src/BE/Core"

# Bỏ dòng chú thích + thư mục sinh tự động. Dữ liệu dự án sống trong CHUỖI, không trong
# văn xuôi; không lọc thì mọi neo `doc/...` trong XML doc đều thành báo động giả.
NOCODE=':[0-9]+: *(///|//|\*)|/obj/|/Migrations/'
```

| # | Phép thử | PASS |
|---|---|---|
| 1 | Seed trên DB trống sau khi sửa | **Đúng 4 mục menu, đúng cây cha-con, đúng 3 lời gán role** — hành vi không đổi |
| 2 | `grep -rnE '"[^"]*(trang-chu\|quan-tri\|phan-quyen\|pi-)' $BE_CORE --include=*.cs \| grep -vE "$NOCODE"` | **rỗng** — đã chạy 2026-09-03 |
| 3 | `grep -rniE "['\"]/(dang-nhap\|doi-mat-khau\|trang-chu)" $FE_CORE --include=*.ts --include=*.html \| grep -v '\.spec\.'` | **rỗng** (trừ chú thích lịch sử) |
| 4 | `dotnet test src/BE` + `npm test --prefix src/FE` | Cả hai **xanh**, và số test **không giảm** so với lần chạy trước — đếm bằng lệnh, không chép vào đây (§6 `.claude/CLAUDE.md`) |
| 5 | `grep -rnE '"[^"]*(@[a-z]+\.local\|Quản trị viên)' $BE_CORE --include=*.cs \| grep -vE "$NOCODE"` | **rỗng** — đã chạy 2026-09-03 |
| 6 | `grep -cE "^const [A-Z_]+ = '#" src/FE/src/app/core/theme/*.ts` | **0** |
| 7 | `grep -rniE "platform[_ -]?manager" $FE_CORE \| grep -v '\.spec\.' \| grep -vE "doc/\|src/BE/"` | **rỗng** — thứ còn lại chỉ là chú thích trỏ đường dẫn tài liệu |
| 8 | Gỡ dòng đăng ký DI của từng seam mới rồi khởi động | **Chết trước khi ghi/hiển thị**, không có bản mặc định im lặng |
| 9 | `grep -nE "#(000000\|ffffff)" src/FE/src/app/core/theme/core-preset.ts \| grep -vE "mix\(" \| grep -vE "^[0-9]+: \*"` | **rỗng** — hex đen/trắng còn lại **chỉ** nằm trong `mix()`, tức đầu mút của phép pha |
| 10 | `grep -rnE '"[a-z][a-z-]*\.[a-z][a-z-]*"' $BE_CORE --include=*.cs \| grep -vE "$NOCODE"` | **rỗng** — đã chạy 2026-09-03. Đối chứng bắt buộc: cùng lệnh trên `src/BE/PlatformManager.Api` phải **KHÔNG** rỗng (`AppResourceKeySource.cs:33`) — rỗng cả hai bên nghĩa là lệnh hỏng, không phải ranh giới sạch |

Những chi tiết đánh số dưới đây là **kết quả của một lần thoát lưới đã xảy ra thật**, không phải cầu
kỳ thừa — đọc §"Bài học — vì sao `platform/` thoát lưới" bên dưới trước khi rút gọn chúng đi:

0. **`$BE_CORE` là `src/BE/Core`, không phải `CoreSeeder.cs`** — cùng lỗi với #1, khác phía.
1. **`platform/` có trong `$FE_CORE`** — bỏ ra là quét hẹp hơn ranh giới.
2. **`-i` và `[_ -]?`** ở phép thử 7 — tên sản phẩm còn xuất hiện dưới dạng `snake_case` chữ thường.
3. **`--include=*.html`** ở phép thử 3 — dữ liệu dự án nấp trong template không bị `.ts`-only bắt.
4. **Phép thử 9 KHÔNG còn miễn trừ `contrastColor`** (siết 2026-09-03). Miễn trừ đó tồn tại vì
   `contrastColor` từng bị phân loại nhầm là cơ chế; nay giá trị đã về `APP_PALETTE.onPrimary` nên
   miễn trừ vừa thừa vừa nguy hiểm — nó sẽ nuốt luôn một hex thương hiệu mới nếu ai đó đặt vào
   đúng khoá ấy. Ranh giới nay là một câu kiểm được: **hex là cơ chế khi và chỉ khi nó là đối số
   của `mix()`**.

5. **`"[^"]*` ở phép thử 2 và 5, `$NOCODE` ở cả ba lệnh BE** — nới thư mục thôi thì hai lệnh
   này ĐỎ vì lý do sai: `pi-` khớp mọi neo `be-api-controller.md` trong XML doc (18 dòng), và
   `Quản trị viên` khớp một câu văn xuôi trong `UserErrors.cs`. Một lệnh ĐỎ toàn báo động giả
   sẽ bị bỏ qua sau đúng hai lần chạy, tức là hỏng y như một lệnh không bao giờ ĐỎ. **Dữ liệu
   dự án sống trong chuỗi, không trong văn xuôi** — đó là ranh giới hai lệnh này đo.

Phép thử 1 là phép thử thật: tách seam mà **đổi hành vi seed** thì không phải tách, là viết lại.

### ✅ Đã thi công — phần BE (2026-09-02)

Nửa BE của quyết định này đã vào code. Nửa FE ở mục ngay sau.

| Vai | File |
|---|---|
| Seam (Core khai, không hiện thực) | `src/BE/Core/PlatformManager.Core.Application/Menu/ICoreMenuSeedSource.cs:21` + `MenuSeedItem.cs:45` |
| Dữ liệu (host khai) | `src/BE/PlatformManager.Api/Seeding/AppMenuSeedSource.cs:41` |
| Đăng ký DI | `src/BE/PlatformManager.Api/Program.cs:166` |
| Cơ chế (giữ ở Core, không còn dữ liệu) | `src/BE/Core/PlatformManager.Core.Persistence/CoreSeeder.cs:301` (`SeedMenuAsync`) |

*(🔄 LẬT 2026-09-06 — cả **bốn** số dòng trong bảng trên đã trượt và được sửa lại lần này:
`MenuSeedItem.cs:36`→`:45`, `Program.cs:124`→`:133` (sửa tiếp 2026-09-10: `:133`→`:166`),
`CoreSeeder.cs:268`→`:274` (sửa tiếp 2026-09-10: `:274`→`:301`, dòng cũ đã trỏ vào giữa một lời gọi log). Bản trước ghi
"hai số dòng đã cập nhật 2026-09-02" — đúng lúc viết, sai từ lượt sửa code kế tiếp.)*

Core **cố ý không có hiện thực mặc định** cho seam: một bản mặc định trả danh sách rỗng sẽ biến
"host quên đăng ký" thành một database không mục menu nào và không lỗi nào. Thiếu đăng ký ⇒
`CoreSeeder` không phân giải được từ DI ⇒ tiến trình `--seed` chết trước khi ghi bất cứ thứ gì
(đo thật 2026-09-02: gỡ dòng đăng ký ⇒ ở **Development**, `ValidateOnBuild` của DI ném ngay tại
`builder.Build()`. Ở **Production** `ValidateOnBuild` **tắt** theo mặc định của host, nên lỗi lộ
ra muộn hơn một nhịp — tại `GetRequiredService<CoreSeeder>()` trong `SeedCommand`. Kết luận không
đổi (vẫn thoát mã 1, vẫn **trước** khi ghi bất cứ dòng nào), nhưng đừng suy ra "quên đăng ký thì
API không lên được" — ở Production API **vẫn lên bình thường**, chỉ lệnh seed là chết).

Bằng chứng cho phép thử #1 — chạy `--seed` thật lên một Postgres trống (schema dựng từ
`0001_initial_baseline.sql`), rồi đọc bảng: **4 dòng `SysMenus`** đúng `Code`/`Name`/`Route`/
`Icon`/`DisplayOrder`, `sys-user` + `phan-quyen` trỏ đúng `ParentId` của `quan-tri`, và **5 dòng
`SysMenuRoles`** (`quan-tri`→SuperAdmin+Admin, `sys-user`→SuperAdmin+Admin, `phan-quyen`→
SuperAdmin; `trang-chu` không dòng nào = mở cho mọi user đã đăng nhập). Chạy `--seed` lần hai:
vẫn 4 và 5 — idempotent. Lặp lại phép đo này bằng chính lệnh ở
`doc/huong_dan/wiki-core/be/13-core-data-migration.md` §"bootstrap Production bằng lệnh riêng".

### ✅ Đã thi công — phần FE, route của guard (2026-09-02)

| Vai | File |
|---|---|
| Seam (Core khai, không có mặc định) | `src/FE/src/app/core/config/core-routes.ts:16` (hợp đồng) · `:40` (token) · `:47` (hàm cung cấp) |
| Dữ liệu (host khai) | `src/FE/src/app/app.config.ts:33` (`APP_CORE_ROUTES`) |
| Đăng ký DI | `src/FE/src/app/app.config.ts:125` (`provideCoreRoutes(APP_CORE_ROUTES)`) |
| Nơi tiêu thụ | `core/auth/auth.guard.ts:22` · `core/auth/must-change-password.guard.ts:22` · `core/auth/role.guard.ts:26` · `core/interceptors/http-error.interceptor.ts:120` · `shared/components/topbar/topbar.ts:33` |

Giống seam menu ở BE, `CORE_ROUTES` **cố ý không có giá trị mặc định**. Lý do khác một chút:
một bộ route mặc định sẽ không làm hỏng gì lúc khởi động, nó chỉ điều hướng sai — người dùng
mất phiên bị đẩy tới một URL không tồn tại, và lỗi lộ ra ở màn hình chứ không ở lúc dựng ứng
dụng. Thiếu đăng ký ⇒ `inject(CORE_ROUTES)` ném `NG0201` ngay lần dựng đầu tiên.

Hệ quả đã gặp thật khi thi công: sửa `topbar.ts` sang dùng token làm 6 test trong `app.spec.ts`
đỏ với `NG0201`, vì bộ test dựng component mà không cấp token. Đó là **bằng chứng seam có
răng**, không phải phiền toái — đã sửa bằng cách cấp token trong chính file test.

### ✅ Đã thi công (BE) — tài khoản bootstrap (2026-09-02)

Nhóm thứ ba của bảng trên. **Không cùng hình dạng với seam menu** — đọc §"Hai quyết định bổ sung"
bên trên trước khi sửa gì ở đây.

| Vai | File |
|---|---|
| Seam (Core khai, không hiện thực) | `src/BE/Core/PlatformManager.Core.Application/Bootstrap/ICoreBootstrapAccountSource.cs:42` + `BootstrapAccountProfile.cs:23` |
| Dữ liệu (host khai) | `src/BE/PlatformManager.Api/Seeding/AppBootstrapAccountSource.cs:27` |
| Đăng ký DI | `src/BE/PlatformManager.Api/Program.cs:173` |
| Cơ chế + luật (giữ ở Core) | `src/BE/Core/PlatformManager.Core.Persistence/CoreSeeder.cs:136` (`SeedBootstrapUserAsync`) · `:179` (`GuardAgainstInvalidProfiles`) — neo lại 2026-09-10, trước ghi `:109`/`:152` (một chú thích và một dòng trống) |
| Test canh | `src/BE/Tests/PlatformManager.Core.IntegrationTests/Bootstrap/CoreSeederBootstrapAccountSourceTests.cs:30` |

**Hình dạng seam là phần quan trọng nhất, không phải vị trí file.** Nó phơi ra **hai thuộc tính có
tên** (`SuperAdmin`, `Admin`), không phải `IEnumerable<>`: nhờ vậy luật "đúng hai tài khoản, mỗi
cái một vai" do **compiler** ép, không cần guard runtime nào và dự án 2 không có cách nào khai một
tài khoản mang cả hai vai. `BootstrapOptions` **không đổi một dòng nào** — đúng ràng buộc ở §Chốt.

Ở lại Core, host không đụng tới được: tên đăng nhập `"SuperAdmin"`/`"Admin"` (soi gương `Roles.*`),
`MustChangePassword = true`, và việc mật khẩu đọc từ `BootstrapOptions`.

**Guard mới ở Core** (`CoreSeeder.GuardAgainstInvalidProfiles`) — sinh ra vì bốn giá trị này từng
là hằng số được compiler bảo vệ, nay là chuỗi tự do do host khai. Ba ca nó chặn đều **không** làm
Identity hay Postgres ồn lên một tiếng nào: email rỗng (Identity không bắt buộc email), tên hiển
thị rỗng (`AppUser.FullName` mặc định đã là chuỗi rỗng), và hai tài khoản dùng chung một email
(`RequireUniqueEmail = false` nên không index nào chặn — hậu quả rơi ra ngoài seeder, ở chỗ không
còn phân biệt được tài khoản break-glass với tài khoản dùng hằng ngày). Guard chạy trên **cả hai**
hồ sơ trước khi tạo tài khoản nào, để không để lại trạng thái nửa vời.

Nghiệm thu — **đã đo 2026-09-02**:

| Phép thử | Kết quả |
|---|---|
| `grep -nE "@platformmanager\.local\|Quản trị viên" .../CoreSeeder.cs` (phép thử #5) | **rỗng** |
| `grep -rnE "@platformmanager\.local" src/BE/Core` (mở rộng, không nằm trong bảng nghiệm thu) | **rỗng** |
| `dotnet build src/BE` | 0 error, 0 warning |
| `dotnet test src/BE` | xanh cả 3 project test, không test nào mất đi |

**Chưa đo lại, nói rõ để không ai tưởng đã đo:** phép thử #1 (chạy `--seed` thật lên Postgres
trống) và #8 (gỡ dòng đăng ký DI rồi khởi động) chưa chạy trực tiếp cho seam này. Kết luận về #8
suy ra từ seam menu cùng khuôn DI, chưa phải phép đo. Ngược lại, phần "giá trị của host đi xuống
tới bảng" thì **có** đo thật — bằng test đọc lại `AspNetUsers` từ database sau khi seed.

### ✅ Đã thi công — phần FE, bảng màu + tên sản phẩm (2026-09-02)

Hai mảnh cuối của quyết định này đi **hai đường khác nhau**, và sự khác nhau đó là ràng buộc kỹ
thuật chứ không phải lựa chọn phong cách.

| Vai | File |
|---|---|
| Seam bảng màu — hợp đồng + cơ chế, KHÔNG còn dữ liệu | `src/FE/src/app/core/theme/core-preset.ts:21` (`ICorePalette`) · `:135` (`createCorePreset`) |
| Dữ liệu bảng màu (host khai) | `src/FE/src/app/app.config.ts:64` (`APP_PALETTE`) |
| Wire bảng màu | `src/FE/src/app/app.config.ts:161` (`preset: createCorePreset(APP_PALETTE)`) |
| Seam tên sản phẩm (Core khai, không có mặc định) | `src/FE/src/app/core/config/core-branding.ts:16` (hợp đồng) · `:43` (token) · `:46` (hàm cung cấp) |
| Dữ liệu tên sản phẩm (host khai) | `src/FE/src/app/app.config.ts:48` (`APP_BRANDING`) |
| Đăng ký DI | `src/FE/src/app/app.config.ts:129` (`provideCoreBranding(APP_BRANDING)`) |
| Nơi tiêu thụ tên sản phẩm | `core/title/page-title.strategy.ts:62` · `shared/components/sidebar/sidebar.ts:48` · `shared/components/auth-card/auth-card.ts:28` · `platform/login/pages/login/login.page.ts:73` · `platform/trang-chu/pages/trang-chu/trang-chu.page.ts:40` — cả năm là dòng `inject(CORE_BRANDING)` |

**Bảng màu KHÔNG đi qua `InjectionToken`** — khác `CORE_ROUTES` và `CORE_BRANDING` ngay cạnh nó.
Preset được dựng ngay trong `providePrimeNG({ theme: { preset } })`, tức lúc **tạo object cấu
hình**, trước khi bất kỳ injector nào tồn tại; `inject()` ở đó ném `NG0203`. Vì vậy bảng màu là
**tham số hàm**. Hệ quả tốt kèm theo: quên truyền là lỗi **biên dịch**, sớm hơn cả lỗi lúc chạy —
nên seam này không cần mẹo "cố ý không có giá trị mặc định" như hai token kia.

Cơ chế ở lại Core đúng như đã chốt: `mix()`, `ramp()`, toàn bộ ánh xạ `semantic`/`colorScheme` và
thang `surface` 0–950. Các giá trị `#000000`/`#ffffff` **ở lại** — chúng là đầu mút của phép pha
đậm/nhạt, sản phẩm nào cũng pha về đen/trắng. Số lượng đếm bằng phép thử 9 ở §Nghiệm thu, không
chép vào đây (§6 `.claude/CLAUDE.md`).

**Không phải giá trị nào cũng nằm trong `mix()`, và chỗ đứng ngoài đó là DỮ LIỆU.** `primary.
contrastColor` từng là `'#ffffff'` khai cứng ngay trong file preset; nay nó đọc
`ICorePalette.onPrimary` (`src/FE/src/app/core/theme/core-preset.ts:56` khai hợp đồng · `:159`
tiêu thụ) và host cấp giá trị ở `src/FE/src/app/app.config.ts:79`. Giá trị **không đổi** — vẫn là
trắng, vẫn soi gương `--on-primary` ở `src/FE/src/styles.scss` § `--on-primary` — chỉ đổi chỗ khai.

Bản trước của đoạn này khẳng định ngược lại: *"Nó vẫn là cơ chế chứ không phải màu thương hiệu:
chữ đặt trên nền `brand` luôn là trắng"*. Câu đó sai ở chữ **luôn**. Mực trên nền `brand` bị ràng
buộc bởi chính `brand` mà sản phẩm chọn: brand tối cần mực trắng, brand sáng (vàng, cyan, lime)
cần mực tối — sản phẩm thứ hai chọn brand sáng sẽ phải mở `core/` ra sửa, đúng thứ định nghĩa
"CoreBase xong" loại trừ.

Và bản trước nữa của đoạn này ghi *"N giá trị `#000000`/`#ffffff` **bên trong `mix()`** ở lại"* —
con số chép tay đó vừa sai, vừa **gộp nhầm phân loại**: nó tính cả giá trị không thuộc `mix()` vào
tổng của `mix()`, nên chính câu ấy làm mảnh dữ liệu duy nhất đáng chú ý **vô hình**. Chuỗi ba bản
này là bài học đầy đủ: số sai còn có ngày ai đó đếm lại; **một phân loại sai thì tự bảo vệ mình
bằng một câu văn nghe hợp lý**, và phải có phép thử chạy được mới lật lại được — ở đây là phép
thử 9 sau khi bỏ miễn trừ `contrastColor`.

**Chỗ thứ ba của tên sản phẩm không có trong kế hoạch.** Kế hoạch nêu 2 chỗ (`APP_NAME` và
`sidebar.html`); phép kiểm quét chữ tắt thương hiệu trong template `shared/` tìm thêm
`shared/components/auth-card/auth-card.html` — cùng ô vuông `.brand-mark`, ở màn đăng nhập. Đây là
**lần thứ hai** một chỗ bị sót vì phép kiểm chỉ quét một tầng hoặc chỉ quét `.ts` (lần đầu:
`topbar.ts` khai cứng `/dang-nhap`, ghi ở mục ngay trên). Bài học: dữ liệu dự án nấp trong `.html`
dễ sót hơn hẳn trong `.ts`.

Một chỗ nữa được dọn cùng lượt, tuy không nằm trong quyết định: chú thích ở
`core/interceptors/credentials.interceptor.ts` chép tên cookie phiên mang tên sản phẩm. FE không
đọc tên cookie ở bất kỳ đâu — tên đó do BE đặt — nên chú thích nay trỏ về chỗ khai bên BE thay vì
chép lại.

Nghiệm thu đã chạy thật 2026-09-02 (số test **không giảm**: 251 trước và sau; `ng lint` sạch;
`ng build` thành công):

| # | Phép thử | Kết quả |
|---|---|---|
| 6 | `grep -cE "^const [A-Z_]+ = '#" src/FE/src/app/core/theme/*.ts` | `0` |
| 7 | `grep -rn "PlatformManager" src/FE/src/app/core src/FE/src/app/shared \| grep -v spec \| grep -vE "doc/\|src/BE/"` | rỗng |
| 8 | chữ tắt thương hiệu trong template `shared/` | rỗng |

> ⚠️ **Ba lệnh trên là bản HẸP đã dùng lúc thi công, giữ nguyên ở đây làm hiện vật.** Chúng không
> quét `platform/` và phân biệt hoa thường, nên kết quả "rỗng" của chúng chỉ nói được về `core/` và
> `shared/`. Bản đúng ranh giới là phép thử 3, 7, 9 ở §Nghiệm thu (dùng `$FE_CORE`); vì sao khác
> nhau thì đọc §"Bài học — vì sao `platform/` thoát lưới".

Hệ quả đã gặp thật, đúng như dự báo: cấp token cho `PageTitleStrategy`/`Sidebar`/`AuthCard` làm
một loạt file test đỏ `NG0201` vì chúng dựng component mà không cấp token — **bằng chứng seam có
răng**. Đã sửa bằng cách cấp token trong chính các file test đó. Danh sách và số lượng đếm bằng
lệnh, không chép vào đây (§6 `.claude/CLAUDE.md`):

| Phép thử | PASS |
|---|---|
| `grep -rl "provideCoreBranding\|CORE_BRANDING" src/FE/src --include=*.spec.ts` | Phủ **đúng** tập spec có dựng `PageTitleStrategy`/`Sidebar`/`AuthCard`. Thiếu file nào thì file đó đỏ `NG0201`, nên tập này tự canh chính nó |

Bản trước của đoạn này ghi *"4 file test đỏ"* rồi liệt kê **5** tên ngay câu sau. Hai con số chỏi
nhau trong cùng một đoạn văn mà không ai thấy — vì không con số nào đến từ một phép đếm, nên không
có gì để đối chiếu. Và lệnh trên phải bắt **cả** `provideCoreBranding`, không chỉ tên token: lúc rà
lại 2026-09-03, `grep` mỗi `CORE_BRANDING` trả về **thiếu một file** so với khi thêm tên hàm helper
— đúng loại hụt một đơn vị mà một con số chép tay không bao giờ tự khai ra.

Tên sản phẩm dùng trong test **cố ý khác** tên thật: dùng đúng tên thật thì test vẫn xanh cả khi
ai đó khai cứng lại chuỗi vào `core/`.

### ✅ Đã thi công — phần FE, đóng ba đường thoát (2026-09-03)

Lượt này không mở seam mới nào. Nó đi đóng đúng những gì lưới rộng hơn ở §Bài học bắt được, cộng
với một mảnh dữ liệu bị phân loại nhầm trong `core/`.

| Đường thoát | Chỗ mang | Nay đọc từ |
|---|---|---|
| #1 sai thư mục | `<h1>` của card đăng nhập (`platform/login/.../login.page.html:1`) — đây là **tên sản phẩm**, không phải tiêu đề màn hình, xem `doc/Design/Frontend/PlatformManager/Components/AuthCard.md` | `CORE_BRANDING.name`, inject ở `platform/login/pages/login/login.page.ts:73` |
| #1 sai thư mục | Đoạn dẫn trang chủ (`platform/trang-chu/.../trang-chu.page.html:24`) | `CORE_BRANDING.name`, inject ở `platform/trang-chu/pages/trang-chu/trang-chu.page.ts:40` |
| #2 sai chữ hoa thường | Khoá `localStorage` của sidebar (`shared/services/sidebar-state.service.ts:21`) | Khoá trung tính `core.sidebar.collapsed.v1` |
| — (phân loại sai) | `primary.contrastColor` trong preset PrimeNG | `ICorePalette.onPrimary` (`core/theme/core-preset.ts:56`), host khai ở `app.config.ts:79` |

**Hai chỗ trong `platform/` nguy hiểm hơn vẻ ngoài của chúng.** `<h1>` card đăng nhập nằm ngay
**dưới** ô vuông `.brand-mark` vốn đã đổ chữ từ token — nên dựng sản phẩm thứ hai sẽ ra một card
mang chữ tắt MỚI với dòng chữ tên CŨ ngay bên dưới, ở màn hình đầu tiên người dùng gặp. Không lỗi,
không cảnh báo, không pixel nào lệch.

**Vì sao đổi khoá `localStorage` được chấp nhận dù nó làm mất trạng thái đã lưu:** người dùng đang
thu gọn sidebar sẽ thấy nó mở lại **đúng một lần**. Không viết code di trú cho một cờ boolean bấm
lại mất một giây — nhưng lý do phải nằm trong chú thích tại chỗ, nếu không người sau sẽ đọc nó
thành lỗi. Xem `src/FE/src/app/shared/services/sidebar-state.service.ts:13`.

**Seam nay có răng ở cả ba nơi tiêu thụ, không chỉ một.** Trước lượt này chỉ
`page-title.strategy.spec.ts` assert lên chuỗi sinh ra từ token; hai nơi còn lại (`sidebar.html`,
`auth-card.html`) mới chỉ **cấp** token cho `TestBed` mà không assert gì lên chuỗi đã render — tức
viết lại template thành chuỗi cứng và gỡ dòng `inject()` thì toàn bộ bộ test vẫn xanh. Đo được:
mutation test 2026-09-03 (khai cứng lại cả 4 chỗ + gỡ `provideCoreBranding` + đổi `<title>`) làm
**đúng 8 test mới** đỏ, không test cũ nào đỏ.

Chốt chặn cho chính `app.config.ts` cũng mới có ở lượt này: `src/FE/src/app/app.config.spec.ts`.
Nó lấp một lỗ hổng thuộc loại tự-che-mắt — **mọi** spec khác dựng `TestBed` của riêng nó và tự khai
`provideCoreBranding(...)`, nên chúng xanh kể cả khi `app.config.ts` quên đăng ký; lỗi thật chỉ lộ
ra lúc chạy app thật. Cùng file đó canh `APP_BRANDING.name` khớp `<title>` tĩnh trong
`src/FE/src/index.html` — hai chuỗi phải giống nhau, lệch thì tên tab đổi trước mắt người dùng lúc
trang tải xong. Để đọc được `index.html` từ trong karma, target `test` của `angular.json` phục vụ
nó qua một mục `assets` riêng; `angular.json` là JSON nên lý do nằm trong chính file spec.

Nghiệm thu — **đã đo 2026-09-03**: phép thử 6, 7, 9 ở §Nghiệm thu **rỗng**; `ng lint` sạch;
`ng build` thành công; `npm test --prefix src/FE` xanh và số test **tăng** (đếm bằng lệnh, không
chép vào đây — §6 `.claude/CLAUDE.md`). Phép thử 3 **CHƯA rỗng** — route khai cứng trong `platform/`
chưa nằm trong phạm vi lượt này, xem hàng cuối bảng "Chỗ mang, đo được".

### 🚧 Bài học — vì sao `platform/` thoát lưới (ghi nhận 2026-09-03)

Lượt thi công phía FE đóng dấu `✅ ĐÃ TÁCH` cho *"tên sản phẩm"* và *"3 route khai cứng"*. Dấu ấy
được sinh ra bởi những lệnh chỉ quét `src/FE/src/app/core` và `src/FE/src/app/shared`. Nhưng ranh
giới CoreBase phía FE **rộng hơn thế** — nó là đúng tập `$FE_CORE` khai ở §Nghiệm thu, và khối cây
`src/FE/src/app/` ở mục
"Quyết định FE" khai thẳng `platform/` là *"các màn hình Core (đăng nhập/quản trị hệ thống), dùng
lại được"*, và `src/FE/eslint.config.js:89` nhắc lại đúng nghĩa đó khi cố ý miễn gate G8 cho
`platform/` vì *"màn Core không bị ràng buộc quy tắc này"*.

Hệ quả: **dấu ✅ chỉ rộng bằng lệnh sinh ra nó, nhưng nó không tự khai bề rộng của mình.** Người
đọc thấy *"Tên sản phẩm — ✅ ĐÃ TÁCH"* và kết luận Core đã sạch tên sản phẩm; thực tế câu đó chỉ
nói được về hai trong ba thư mục. Đây đúng là khuôn §4 `.claude/CLAUDE.md` gọi là dạng sai đắt
nhất: không lỗi biên dịch, không test nào đỏ, và nhãn "đã xong" được thiết kế để không ai kiểm lại.

**Ba đường thoát, cả ba đều đo được — và chúng KHÁC nhau, đừng gộp thành một:**

| # | Đường thoát | Vì sao lệnh cũ không thấy | Sửa ở lệnh |
|---|---|---|---|
| 1 | **Sai thư mục** — `platform/**` không có mặt trong bất kỳ phép quét nào | Danh sách thư mục chép tay, dựng từ trí nhớ *"Core là core/ + shared/"* thay vì từ định nghĩa ranh giới trong chính tài liệu này | biến `$FE_CORE` ở §Nghiệm thu — **một** nơi khai ranh giới, mọi lệnh dùng lại |
| 2 | **Sai chữ hoa thường** — khoá `localStorage` ở `src/FE/src/app/shared/services/sidebar-state.service.ts:21` mang tên sản phẩm dạng `snake_case` chữ thường (đã đổi sang khoá trung tính 2026-09-03; chú thích tại chỗ ghi lại vì sao) | `grep "PlatformManager"` phân biệt hoa thường **và** không chịu dấu ngăn. Chỗ này nằm trong `shared/` — tức thư mục ĐÃ được quét: nó thoát vì **hình dạng chuỗi**, không phải vì vị trí | `-i` + lớp ký tự `[_ -]?` ở phép thử 7 |
| 3 | **Sai loại file** — dữ liệu dự án nằm trong `.html` | `--include=*.ts` | thêm `--include=*.html` ở phép thử 3 |

Đường thoát #3 **đã được ghi ở mục ngay trên** (`auth-card.html`, và trước đó `topbar.ts`) kèm câu
kết luận *"dữ liệu dự án nấp trong `.html` dễ sót hơn hẳn trong `.ts`"* — vậy mà lệnh nghiệm thu
vẫn không được sửa theo. **Bài học được viết ra nhưng không được nạp vào lệnh thì không phải bài
học, chỉ là ghi chép.** Đó là lý do lượt này sửa thẳng vào bảng §Nghiệm thu chứ không thêm một
đoạn văn dặn dò nữa.

Đường thoát #2 đáng chú ý riêng: nó chứng minh **nới thư mục thôi là chưa đủ**. Một lệnh quét đúng
cả ba thư mục nhưng vẫn phân biệt hoa thường sẽ tiếp tục bỏ sót đúng khoá đó. Hai lỗi độc lập nhau,
và mỗi lỗi một mình đủ để sinh ra một dấu ✅ sai.

**Trạng thái hôm nay: đọc từ lệnh, không đọc từ đây.** Chạy phép thử 3, 7, 9 ở §Nghiệm thu.

Ba đường thoát ở bảng trên đã đóng (2026-09-03, xem mục "Đã thi công (FE) — đóng ba đường thoát").
Phép thử 3 mở rộng khi đó phơi ra một nhóm **chưa từng nằm trong quyết định nào**: các trang trong
`platform/` tự điều hướng bằng đường dẫn chữ, trong khi seam `CORE_ROUTES`
(`src/FE/src/app/core/config/core-routes.ts:40`) đã tồn tại sẵn cho đúng việc đó. Nhóm đó **đã
dọn xong cùng ngày** — xem mục dưới.

Nói rõ để không ai gộp nhầm: đóng ba đường thoát KHÔNG đồng nghĩa với "`platform/` đã sạch". Nó
chỉ có nghĩa là **lưới** đã đúng bề rộng — thứ lưới bắt được thì vẫn phải dọn bằng tay, và lượt
dọn đó là mục ngay sau đây.

### 🚧 Bài học — vì sao `ResourceKeys` thoát lưới (ghi nhận 2026-09-03)

Cùng khuôn với bài học `platform/` ngay trên, nhưng ở phía BE — và nó **lặp lại đúng lỗi mà
bài học kia vừa mô tả**, chỉ ba tuần sau, ở nửa còn lại của repo. Đó là dữ kiện đáng chú ý
nhất của mục này: một bài học được viết ra cho FE **không tự lan sang** BE.

Phép thử 2 và 5 grep đúng **một file**, `CoreSeeder.cs`. Nhưng ranh giới CoreBase phía BE là
**toàn bộ `src/BE/Core`** — chính tài liệu này khai như vậy ở khối cây §"Quyết định BE", và
`CoreModuleBoundaryTests` cưỡng chế đúng phạm vi đó. Lệnh hẹp hơn ranh giới thì dấu ✅ nó sinh
ra cũng hẹp theo.

`CoreSeeder.cs` được chọn không phải vì nó là ranh giới, mà vì nó là **nơi lượt trước tìm
thấy vi phạm**. Lệnh nghiệm thu bị viết theo *chỗ đã tìm ra lỗi*, chứ không theo *chỗ lỗi có
thể ở*. Vi phạm kế tiếp vì vậy chỉ cần nằm ở file khác là thoát — `ResourceKeys.cs` cách
`CoreSeeder.cs` đúng một thư mục.

**Nới thư mục KHÔNG đủ, và đây là chỗ bài học FE đã nói trước (đường thoát #2) mà lần này
vẫn phải trả giá lại.** Nới `$BE_CORE` rồi chạy nguyên hai mẫu cũ thì:

| Lệnh | Kết quả khi CHỈ nới thư mục | Vì sao |
|---|---|---|
| Phép thử 2 | ĐỎ **18 dòng**, không dòng nào là vi phạm | `pi-` khớp mọi neo `be-api-controller.md` trong XML doc |
| Phép thử 5 | ĐỎ **1 dòng**, không phải vi phạm | `Quản trị viên` khớp một câu văn xuôi trong comment `UserErrors.cs` |
| Cả hai | **Vẫn không thấy `ResourceKeys`** | Khoá `"import.manage"` và nhãn `"Import CSV/Excel"` không khớp mẫu nào của hai lệnh — không phải route, không phải icon, không phải email, và nhãn thì không có dấu tiếng Việt |

Ba dòng trên nói ba điều khác nhau, và **cả ba đều cần thiết**:

1. **Sai thư mục** → sửa bằng `$BE_CORE`.
2. **Sai hình dạng chuỗi** → sửa bằng `"[^"]*` (chỉ soi chuỗi) + `$NOCODE` (bỏ chú thích).
   Không có bước này thì lệnh ĐỎ toàn báo động giả, và một lệnh như vậy bị bỏ qua sau hai
   lần chạy — hỏng y hệt một lệnh không bao giờ ĐỎ, chỉ ồn hơn.
3. **Sai thứ đem đo** → hai lệnh cũ đo *route/icon/email*, còn `ResourceKeys` mang **nhãn
   hiển thị**. Không mẫu nào phủ được, nên phải là **phép thử 10**, một lệnh mới.

Điểm 3 là điểm dễ bỏ qua nhất: nới lệnh cũ cho rộng ra không bao giờ sinh ra lệnh đo một
thứ mà lệnh cũ chưa từng đo. **Một loại dữ liệu dự án mới cần một phép thử mới, không phải
một `grep` rộng hơn.**

#### Trình tự đã xảy ra thật trong ngày 2026-09-03 — ghi lại vì nó là bằng chứng, không phải giai thoại

| Lúc | Việc | Phép thử 10 |
|---|---|---|
| Khi lưới được nới | `ResourceKeys.cs` còn trong `Core.Application`, mang cả danh mục lẫn nhãn | **ĐỎ** |
| Sau khi khu thi công BE tách seam `ICoreResourceKeySource` | Danh mục + nhãn ra host (`AppResourceKeySource`) | **rỗng** |

Phép thử 10 vì vậy **đã được quan sát ở cả hai trạng thái** — nó từng ĐỎ trên vi phạm thật,
rồi tự chuyển sang rỗng khi vi phạm được gỡ. Đó là điều duy nhất phân biệt một phép thử với
một dòng chữ: **một lệnh chỉ đáng tin sau khi có người thấy nó ĐỎ.** Một lệnh viết ra lúc
mọi thứ đã xanh thì không ai biết nó có bao giờ ĐỎ được không.

Đối chứng để giữ tính chất đó về sau: chạy phép thử 10 trên `src/BE/PlatformManager.Api`
phải **KHÔNG** rỗng (`AppResourceKeySource.cs:33`). Rỗng cả Core lẫn host nghĩa là mẫu regex
đã hỏng, không phải ranh giới đã sạch — và hai kết luận đó nhìn giống hệt nhau trên màn hình.

Giới hạn đã biết của phép thử 10, ghi ra để lần sau không ai âm thầm thu hẹp nó: mẫu
`"chữ.chữ"` cũng khớp **tên file** (`appsettings.json`, `log-.txt`). Trong `src/BE/Core` hôm
nay không có chuỗi nào như vậy nên lệnh rỗng sạch; nếu sau này có, **đừng** siết mẫu về
`const string` cho hết ồn — đó đúng là đường thoát "sai hình dạng chuỗi" ở bảng trên. Loại
trừ đúng cái tên file gặp phải, giữ nguyên bề rộng còn lại.

Cũng vì vậy, thứ tiếng Việt **không** được dùng làm dấu hiệu ở phía BE — `src/BE/Core` cố ý
đầy câu tiếng Việt (toàn bộ catalog `*Errors.cs`). Đó là nợ i18n đã ghi riêng ở
[`huong_dan/wiki-core/be/16-i18n-va-ma-loi.md`](huong_dan/wiki-core/be/16-i18n-va-ma-loi.md),
**khác** nợ ranh giới Core; trộn hai thứ vào một lệnh thì lệnh đó không đo được cái nào.

**Trạng thái hôm nay: đọc từ lệnh, không đọc từ đây.** Chạy phép thử 2, 5, 10 ở §Nghiệm thu.

Và nói rõ để không ai gộp nhầm, y như bài học FE đã phải nói: lưới đúng bề rộng **không**
đồng nghĩa với "`src/BE/Core` đã sạch dữ liệu dự án". Nó chỉ có nghĩa là ba loại dữ liệu
**đã biết** (route/icon, email/tên hiển thị tài khoản, permission-key) nay đo được. Loại thứ
tư sẽ lại thoát, và sẽ lại chỉ lộ ra khi có người đi tìm nó.

### Đã thi công (FE) — route trong `platform/`

✅ **ĐÃ TÁCH 2026-09-03.** Mọi trang trong `platform/` nay đọc đích điều hướng từ
`inject(CORE_ROUTES)` thay vì viết đường dẫn tiếng Việt vào code. Trạng thái hôm nay đọc từ
**phép thử 3** ở §Nghiệm thu, không đọc từ đoạn này.

Điều đáng ghi lại không phải danh sách file, mà là **hình dạng của chỗ sót cuối cùng**:

| | Chỗ sót | Vì sao lượt trước không thấy |
|---|---|---|
| Lượt 2026-09-02 | `core/auth/*.guard.ts` | — (đây là lượt gốc) |
| Lượt 2026-09-02, sót | `shared/components/topbar/topbar.ts` | phép kiểm chỉ quét `core/` |
| Lượt 2026-09-03, sót | `platform/**/*.ts` | phép kiểm chỉ quét `core/` + `shared/` |
| Lượt 2026-09-03, sót lần cuối | `platform/**/*.html` — một `routerLink` chữ | danh sách bàn giao liệt bằng tay và chỉ liệt file `.ts` |

**Bốn lượt, cùng một lỗi, mỗi lượt nấp ở đúng chỗ mà phép kiểm lần trước chưa với tới.** Chỗ sót
cuối bị bắt bởi phép thử 3 — tức bởi **lệnh**, không phải bởi danh sách chép tay đi kèm yêu cầu.
Đó là lý do §Nghiệm thu là nơi giữ sự thật, còn mọi bảng liệt kê trong văn xuôi chỉ là minh hoạ.

Ràng buộc kèm theo, đã có sẵn và không đổi: `CORE_ROUTES` cố ý không có giá trị mặc định, nên
trang nào quên khai provider sẽ chết bằng `NullInjectorError` ngay lần điều hướng đầu — ồn ào và
đúng chỗ. Ba đích điều hướng phải là route có thật, khoá bằng máy ở
`src/FE/src/app/app.routes.spec.ts`.

## Nguyên tắc áp dụng khi thêm tính năng nghiệp vụ mới (tương lai)

**BE**: thêm thư mục feature mới trong `PlatformManager.Business.Application/<TênFeature>/` (đúng
vertical slice đã có) — KHÔNG tạo project mới, KHÔNG tạo `Modules.<Tên>.*` nào. Chỉ được
`ProjectReference` từ `Business.*` tới `Core.*`, không có "module khác" để tránh reference chéo
vì chỉ có 1 khối `Business.*`.

**FE**: tạo `modules/<ten-feature-nghiep-vu>/` theo đúng cấu trúc `pages/components/services/
models` đã có. Không import trực tiếp nội bộ 1 `modules/<feature khác>/*` (ESLint G8 sẽ chặn) —
cần dùng chung thì đưa lên `shared/`/`core/`. Nếu màn hình đó thuộc nhóm quản trị hệ thống
(không phải nghiệp vụ) thì đặt ở `platform/`, không phải `modules/`.

## Khi nào tách thành module độc lập thật (N-module, khác `Business.*` thống nhất)

> Chỉ áp dụng nếu sau này xuất hiện **domain nghiệp vụ thật sự độc lập** với khối `Business.*`
> hiện tại (ví dụ: "Quản lý tài sản" — không liên quan gì tới DTI Weekly/tiến độ đánh giá). Đây
> KHÔNG phải trường hợp "thêm 1 feature vào Business.*" — nếu vẫn cùng domain, xem mục trên.

Dấu hiệu thật để tách (cần rõ ràng, không suy đoán):
1. Domain mới không chia sẻ entity/nghiệp vụ nào có ý nghĩa với `Business.*` hiện tại (không chỉ
   khác tên — khác hẳn bản chất dữ liệu/quy trình).
2. Cần vòng đời phát triển/release độc lập với `Business.*` (team khác sở hữu, lịch deploy khác).
3. Đủ lớn để việc gộp chung vào `Business.Application/<TênFeature>/` làm project đó khó điều
   hướng/build chậm thật (không phải cảm giác "có vẻ nên tách").

Khi đó, tham khảo 3 repo N-module thật đã khảo sát (`kgrzybek/modular-monolith-with-ddd`,
`NET-Architecture-Templates/ModularMonolith`, `meysamhadeli/booking-modular-monolith`) — tạo
`PlatformManager.Modules.<TênDomainMới>.{Domain,Application,Persistence,Infrastructure,Api}` bên
cạnh `Core/` và `Business/` (đổi `Business/` thành 1 "module" trong họ `Modules/` lúc đó nếu muốn
nhất quán, hoặc giữ `Business/` như module đầu tiên, tuỳ đặt tên lúc đó), thêm ArchTest
`Modules_MustNotReference_OtherModules` trở lại. **Không làm trước khi có domain độc lập thật** —
đúng nguyên tắc Rule of Three/premature abstraction đã áp dụng xuyên suốt tài liệu này.

## Khi Core thật sự tách thành thư viện publish riêng (chưa phải bây giờ)

> Nghiên cứu thực tế cho câu hỏi kế tiếp: **khi các ngưỡng dưới đây THẬT SỰ chạm tới**, hệ thống
> production trông như thế nào — chưa phải việc cần làm bây giờ, ghi lại để agent có sẵn tham
> chiếu khi ngày đó tới.

**Ví dụ thật đã khảo sát**: ABP Framework (abp.io) — 592 package `Volo.Abp.*` trên NuGet, build từ
**1 monorepo duy nhất** rồi publish ra nhiều package (không phải viết Core và nghiệp vụ trong
nhiều repo riêng), toàn bộ version **lockstep** qua Central Package Management
(`Directory.Packages.props`), không version độc lập từng package. Orchard Core (OrchardCMS/
OrchardCore) là ví dụ thứ 2 — dùng `Manifest.cs` khai báo dependency giữa các feature (mạnh hơn
`AddXxxModule()` viết tay, cho bật/tắt lúc runtime qua UI) nhưng core của nó gắn chặt với mô hình
hosting riêng, không trung lập như `Volo.Abp.Core` — không phù hợp copy nguyên xi nếu
PlatformManager không cần feature-toggle runtime.

**Ngưỡng publish Core thành package thật** — 3 tín hiệu thật, cần ít nhất 1:
1. Có **repo/solution thứ 2** thật sự cần tiêu thụ Core.
2. Core cần **release cadence độc lập** với `Business.*`.
3. 2 team riêng sở hữu Core vs Business, cần ranh giới qua versioned artifact thay vì cùng review PR.

> **Cập nhật 2026-08-23 — tín hiệu #1 đã được tuyên bố.** Người dùng chốt Corebase **sẽ tái sử
> dụng ở nhiều dự án khác**. Nhưng "sẽ có" khác "đã có": chừng nào **chưa tồn tại repo/solution
> thứ 2 thật sự đang tiêu thụ Core**, vẫn giữ project reference trong-solution. Điều thay đổi
> **ngay bây giờ** là hai thứ rẻ và không thể lùi được nếu bỏ qua:
>
> 1. **`IModuleRegistrar`** (xem §Quyết định BE) — cắm được dự án thứ 2 mà không sửa `Core.*`.
> 2. **`Core.*` không được biết tên tầng nghiệp vụ** — không hardcode `"Business"` ở bất kỳ đâu
>    trong Core; mọi thứ đi qua registrar.
>
> Hai điều đó làm ngay thì ngày publish package chỉ còn là việc đóng gói. Bỏ qua thì phải mổ lại
> Core — đắt hơn nhiều lần.

PlatformManager hiện **chưa chạm điều kiện đủ để publish package** — chưa có repo thứ 2 đang
tiêu thụ thật. Giữ project reference trong-solution là đúng thời điểm.

**Cải tiến nhẹ, có thể làm ngay mà KHÔNG phải trừu tượng hoá sớm**: áp dụng MSBuild Central
Package Management (`Directory.Packages.props` ở root `src/BE/`) để version package tập trung 1
chỗ thay vì rải trong từng `.csproj` — kỹ thuật ABP đang dùng, rẻ, có lợi ngay cả khi chưa publish
gì. Chỉ làm khi người dùng yêu cầu, không tự ý.

## Ngưỡng nâng cấp tiếp — KHÔNG làm trước khi chạm ngưỡng

| Chưa làm | Làm khi nào |
|---|---|
| BE: tách `Business.*` thành N-module độc lập (`Modules.<Tên>.*`) | Khi có domain nghiệp vụ thật sự độc lập với DTI Weekly xuất hiện — xem "Khi nào tách thành module độc lập thật" |
| BE: `IModule` interface + module-loader động | Chỉ cần khi đã có ≥3 đơn vị độc lập (Core + ≥2 module) — 2 đơn vị hiện tại chưa cần |
| BE: DbContext/schema riêng theo từng đơn vị | Khi 1 đơn vị cần deploy độc lập/schema riêng thật sự |
| BE: publish `Core.*` thành NuGet package thật | Khi có repo/solution thứ 2 thật tiêu thụ, HOẶC cần release cadence độc lập, HOẶC 2 team sở hữu riêng |
| BE: `Manifest.cs`/module-loader kiểu Orchard Core (feature-toggle runtime) | Khi có hàng chục module thật cần bật/tắt độc lập lúc runtime |
| FE: chuyển sang Nx monorepo | Khi có ≥2 app thật deploy riêng biệt, không chỉ nhiều feature trong 1 app |
| FE: chuyển sang Sheriff (thay `no-restricted-paths`) | Khi 1 rule phẳng không còn đủ diễn đạt ranh giới nội bộ từng feature |

## Tài liệu liên quan

- `doc/huong_dan/quy-uoc/README.md`, `doc/huong_dan/quy-uoc/be-architecture.md` — quy tắc chi tiết layer BE, cần cập
  nhật khớp file này.
- `doc/huong_dan/quy-uoc/fe-architecture.md` — quy tắc chi tiết cấu trúc FE, đã khớp file này.
- `doc/tham-khao-ngoai/vnr-successor/00-lo-trinh-tong-the.md` §Ngưỡng đơn giản hoá — cùng
  tinh thần "chỉ làm tới mức cần, không xây trước".

# Kiến trúc Core ↔ Business — ranh giới tái sử dụng cho BE và FE

> Tài liệu quyết định (đã CHỐT 2026-08-16, sửa v3 cùng ngày). Agent (`backend-expert`,
> `frontend-expert`, `core-reviewer`) đọc file này để biết ranh giới bắt buộc giữa phần **Core**
> (dùng lại được cho mọi sản phẩm dựng trên nền tảng này) và phần **Business** (khối nghiệp vụ
> thống nhất, hiện có DTI Weekly là tính năng đầu tiên) — áp dụng khi sửa code hiện có VÀ khi
> thêm tính năng nghiệp vụ mới sau này.

## 🚧 ĐÃ CHỐT — ĐANG THI CÔNG (đối chiếu `PlatformManager.slnx` ngày 2026-08-23)

**Toàn bộ layout mô tả bên dưới là ĐÍCH ĐẾN, chưa phải hiện trạng.** Đọc bảng
này trước khi tạo bất kỳ file nào, để không tạo vào project chưa tồn tại.

| Có thật hôm nay (8 project) | Sẽ thành |
| --- | --- |
| `Core.Domain`, `Core.Application`, `Core.Infrastructure` | tách thêm `Core.Common` + `Core.Persistence` + `Core.Api` → **6 project** |
| `Modules.DtiWeekly.{Domain,Application,Infrastructure}` | gộp thành `Business.{Domain,Application,Persistence,Infrastructure,Api}` |
| `PlatformManager.Api` (host mỏng) | giữ nguyên |
| `Tests/PlatformManager.ArchTests` | giữ nguyên |

**Chưa tồn tại:** `Core.Common`, `Core.Persistence`, `Core.Api`, mọi `Business.*`, và `IModuleRegistrar`.
`PlatformManagerDbContext` và `CoreSeeder` hiện ở `Core.Infrastructure/Persistence/`;
mọi controller hiện ở `PlatformManager.Api/Controllers/`.

Vì repo đang chạy mô hình `Modules.<Tên>.*`, ArchTest hiện có
**`Core_MustNotReference_AnyModulesAssembly`** và
**`Modules_MustNotReference_OtherModules`** là **đúng và phải giữ** — mục
ArchTest bên dưới yêu cầu bỏ rule thứ hai, điều đó chỉ áp dụng **sau** khi đã
gộp xong sang `Business.*`.

> Vì sao phải ghi việc này ra: bản trước của file không có dấu trạng thái nào và
> viết ở thì hiện tại mô tả, nên `.claude/agents/backend-expert.md` đã chép
> nguyên cây thư mục sang và agent được chỉ đạo tạo file vào 7 project không tồn
> tại. Luật `.claude/CLAUDE.md` §4 sinh ra từ chính ca này.

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
│   └── PlatformManager.Core.Api/               ← AuthController, UsersController,
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
Business.Api             → Business.Application, Business.Domain

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

**Đây là bản rút gọn của `IBoundedContext` trong `wiki-core/be/trien-khai/04-p3`** — giữ nguyên
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

### ArchTest cần có

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
- Bỏ rule `Modules_MustNotReference_OtherModules` (không còn ý nghĩa — chỉ có 1 khối `Business.*`,
  không có "module khác" để so sánh).

## Quyết định FE — gom màn Core ra `platform/`, thêm ESLint boundary rule

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

### ESLint boundary rule (gate mới — G8)

Dự án FE hiện **chưa cài ESLint** — cài qua `ng add @angular-eslint/schematics`, sau đó thêm rule
`eslint-plugin-import`'s `no-restricted-paths` (đơn giản, phổ biến, không cần học DSL riêng như
Sheriff) chặn `modules/<feature-nghiệp-vụ>/*` import thẳng vào nội bộ 1 feature nghiệp vụ khác
(`modules/<feature-khac>/*`), trong khi vẫn cho phép import từ `core/`, `shared/`, `platform/`.
Ghi bổ sung thành gate **G8** trong `doc/huong_dan/wiki-core/fe/trien-khai/05-gate.md` (cùng
nhóm G1-G7 đã có). Nâng cấp lên Sheriff chỉ khi số feature nghiệp vụ + độ phức tạp phân lớp nội bộ
từng feature tăng đủ lớn để 1 rule phẳng không còn đủ diễn đạt — chưa tới ngưỡng đó ở 2 feature.

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
- `doc/huong_dan/wiki-core/be/trien-khai/00-lo-trinh-tong-the.md` §Ngưỡng đơn giản hoá — cùng
  tinh thần "chỉ làm tới mức cần, không xây trước".

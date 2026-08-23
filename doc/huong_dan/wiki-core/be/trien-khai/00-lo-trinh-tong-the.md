# Lộ trình triển khai core BE — tổng thể

> **Đây là phần thực hành của `wiki-core/be/`.** Các file `be/01-…` → `be/10-…`
> trả lời *"core gồm những gì và vì sao cần"*. Thư mục `be/trien-khai/` này trả
> lời *"làm thì làm theo thứ tự nào, đẻ ra file/class/interface nào"*.
>
> Nguồn đối chiếu: `D:\Successor\VNR.Successor\src\backend\` — một backend .NET 8
> production nhiều năm (8 module, 9 process, 36 file ArchTest). Mọi tên
> class/interface/file trong bộ tài liệu này đều **có thật ở đó**, không phải
> tên bịa cho ví dụ. Chỗ nào là đề xuất đơn giản hoá cho hệ thống mới sẽ ghi rõ
> `[ĐƠN GIẢN HOÁ]`.

---

## ⚠️ ĐỌC TRƯỚC — bộ này mô tả VNR, không phải PlatformManager

**Vai của `trien-khai/`: tài liệu THAM CHIẾU hình dạng chi tiết** (chữ ký class, thứ tự đăng ký
DI, cách một thành phần trông ra sao trong một hệ production thật). **Không phải đặc tả thi công
của PlatformManager.**

Bộ này dựng theo VNR.Successor — **~73 project, 8 module, 9 process**. PlatformManager là hệ
**tầm trung, ~13 project, 1 process**. Chép nguyên xi là mua toàn bộ chi phí mà không mua được
lợi ích.

### Bảng ánh xạ — đọc tên VNR, hiểu sang PlatformManager

| `trien-khai/` viết | PlatformManager là | Ghi chú |
| --- | --- | --- |
| `VNR.Platform.Domain` | `PlatformManager.Core.Domain` | |
| `VNR.Platform.Application` | `PlatformManager.Core.Application` | |
| `VNR.Platform.Common` | `PlatformManager.Core.Common` | |
| `VNR.Platform.Persistence` | `PlatformManager.Core.Persistence` | |
| `VNR.Infrastructure.*` (13 project) | **gộp** vào `Core.Infrastructure` | xem "không áp dụng" #1 |
| `VNR.Module.{M}.*` | tầng nghiệp vụ của dự án — `Business.*` với PlatformManager | tên do **dự án** đặt, Core không biết |
| `VNR.Hosting.Api` | `Core.Api` (base controller, middleware) | |
| `VNR.Hosting.CompositionRoot` | `PlatformManager.Api` (host mỏng) | |
| `VNR.Process.*` (9 process) | **1** `PlatformManager.Api` | xem "không áp dụng" #2 |

### KHÔNG áp dụng cho PlatformManager — 4 mục

1. **13 project `Infrastructure/` tách riêng từng mối bận tâm.** Đây là *assembly-per-namespace*:
   13 `.csproj`, 13 bộ package reference, build chậm — mà không cô lập được gì vì chúng luôn ship
   cùng nhau. PlatformManager gộp hết vào `Core.Infrastructure`.
2. **9 `Process` triển khai riêng.** Đơn vị triển khai chỉ có giá khi **thật sự** deploy riêng.
   PlatformManager chạy **1 process** — mọi thứ trong bộ này giả định nhiều process (JWT, phát
   hiện service, transaction xuyên process) đều không áp dụng.
3. **Mỗi module một `DbContext`** (`IBoundedContext`, `SchemaName`, `AddModuleDbContext<T>`).
   PlatformManager dùng **1 `PlatformManagerDbContext`** và tách ranh giới bằng **schema Postgres**
   (`core` / `business`) — được phần lớn giá trị mà không phải trả giá nhiều migration history.
   *Ý tưởng đảo phụ thuộc thì GIỮ*, dưới dạng `IModuleRegistrar` — xem `doc/kien-truc-core-module.md`.
4. **`Module.{M}.SharedKernel`** — chính VNR cũng đánh dấu "tuỳ chọn".

### Áp dụng NGƯỢC lại — thứ VNR có mà PlatformManager nên lấy

- **`Platform.Common`** → đã chốt thành `Core.Common` (2026-08-23).
- **`Module.{M}.Contracts` + `IBoundedContext`** → đã rút gọn thành **`IModuleRegistrar`** ở
  `Core.Application`. Đây là seam để tầng nghiệp vụ tự đăng ký mà Core không reference nó — điều
  kiện để Corebase cắm được vào dự án thứ hai.

### Auth: bộ này chốt JWT — PlatformManager dùng **cookie session**

`05-p4` §4.1 đặt `[Authorize(AuthenticationSchemes = JwtBearerDefaults...)]` lên `BaseApiController`.
Đó là quyết định **của VNR** (9 process, cần verify token xuyên process).

PlatformManager **1 process** → đã CHỐT **cookie session của ASP.NET Core Identity, KHÔNG JWT**
(`doc/contracts/auth.md`, `be/02-identity-auth.md`). Lý do: JWT trần **không thu hồi được** — nút
"khoá tài khoản" sẽ không có tác dụng cho tới khi token hết hạn; còn cookie có
`SecurityStampValidator`. Đọc `[Authorize(...)]` trong bộ này thành `[Authorize]` trần
(scheme mặc định `IdentityConstants.ApplicationScheme`).

---

## 1. Nguyên tắc chi phối toàn bộ lộ trình

| # | Nguyên tắc | Hệ quả thực tế |
| --- | --- | --- |
| 1 | **Core được xây trước module đầu tiên, nhưng chỉ đủ để module đầu tiên chạy** | Không viết `Outbox`, `IntegrationEvent`, multi-tenant ở Phase 1 chỉ vì "sau này cần". Xem Nhóm A/B ở [01-core-components](../01-core-components.md) |
| 2 | **Mỗi phase kết thúc bằng một thứ chạy được, không phải một thư mục đầy file** | Phase 4 xong = gọi được 1 endpoint thật trả JSON đúng envelope |
| 3 | **Luật kiến trúc phải có máy kiểm, không dựa vào review người** | ArchTests bắt đầu từ Phase 1, không để cuối (Phase 6 chỉ là *bổ sung*, không phải *bắt đầu*) |
| 4 | **Chỉ 2 cực, không có tầng giữa** | Entity CRUD thuần → pattern zero-handler. Entity nghiệp vụ → vertical slice tường minh. **Cấm base handler tầng giữa** (`CreateBusinessHandler<,,,>`) — xem [06-p5](06-p5-module-dau-tien.md) |
| 5 | **Cross-cutting đi qua pipeline behavior, không đi qua kế thừa** | Transaction/log/validation/exception là behavior; handler không biết chúng tồn tại |
| 6 | **Một luật = một nguồn** | Độ dài `Code` khai ở `{Entity}FieldLengths`, dùng lại bởi cả FluentValidation lẫn EF `HasMaxLength` |

---

## 2. Bảy phase — bảng tổng thể

| Phase | Tên | Đầu ra kiểm chứng được (Definition of Done) | Ước lượng |
| --- | --- | --- | --- |
| **P0** | Nền móng solution | `dotnet build` xanh trên solution rỗng có đủ project + `Directory.Build.props` bật `Nullable`/`TreatWarningsAsErrors` một phần | 0.5–1 ngày |
| **P1** | `Platform.Domain` | Có `BaseEntity<TId>`, `AggregateRoot<TId>`, `CatalogEntityBase<TId>`, `ICatalogEntity`, `ISoftDelete`, `EntityId`, `ValueObject`, `Enumeration<TEnum>`, `DomainException`, `IGenericRepository`/`IUnitOfWork` — **zero package reference** | 1–2 ngày |
| **P2** | `Platform.Application` | Có `ICommand`/`IQuery` + `CommandHandler`/`QueryHandler` + 4 pipeline behavior + `IApiResult<T>` + `ErrorDescriptor`. Viết được 1 handler giả lập chạy qua MediatR | 2–3 ngày |
| **P3** | `Platform.Persistence` | `BaseDbContext` + interceptor (audit/soft-delete/id-gen) + `GenericRepository` + `UnitOfWork`. Migration đầu tiên chạy được | 2–3 ngày |
| **P4** | `Hosting` + API | `BaseApiController.HandleRequest()` map `ErrorCode → HTTP`, envelope thống nhất, Swagger lên, 1 endpoint health chạy | 1–2 ngày |
| **P5** | Module đầu tiên | 1 entity catalog (pattern 1) + 1 entity nghiệp vụ (pattern 2) chạy full CRUD qua HTTP | 3–5 ngày |
| **P6** | Gate & mở rộng | ArchTests đủ bộ, analyzer, grid/search/i18n/permission | liên tục |

> **Không làm song song P1↔P2↔P3.** Dependency là một chiều và chặt: viết
> `CommandHandler` trước khi có `IApiResult` sẽ phải viết lại. Ngược lại P5 có
> thể bắt đầu ngay khi P4 xong, và P6 chạy song song với P5 trở đi.

---

## 3. Cấu trúc source code BE — cây thư mục đích

Đây là hình dạng **VNR.Successor thật** (rút gọn, bỏ những nhánh chỉ có nghĩa với
domain HRM của họ). Số trong ngoặc = số project thật.

```
src/backend/
├── VNR.Solution.sln
├── Directory.Build.props              ← cấu hình build tập trung (xem P0)
├── Directory.Packages.props           ← [KHUYẾN NGHỊ] central package management
├── CLAUDE.md                          ← file định hướng, KHÔNG chứa rule chi tiết
├── doc/huong_dan/quy-uoc/be-*.md                 ← rule chi tiết theo chủ đề (13 file)
│
├── Src/
│   ├── Platform/                      (4) ← CORE. Không biết gì về nghiệp vụ
│   │   ├── VNR.Platform.Domain/         ← zero-dependency
│   │   ├── VNR.Platform.Application/    ← CQRS, behaviors, contracts
│   │   ├── VNR.Platform.Common/         ← utility thuần, không phụ thuộc layer nào
│   │   └── VNR.Platform.Persistence/    ← BaseDbContext, repository, interceptor
│   │
│   ├── Infrastructure/                (13) ← hạ tầng kỹ thuật dùng chung, mỗi mối
│   │   ├── VNR.Infrastructure.Caching/     bận tâm 1 project riêng
│   │   ├── VNR.Infrastructure.Logging/
│   │   ├── VNR.Infrastructure.Security/
│   │   ├── VNR.Infrastructure.Storage/
│   │   ├── VNR.Infrastructure.Messaging/
│   │   ├── VNR.Infrastructure.Jobs/
│   │   ├── VNR.Infrastructure.Grid/
│   │   ├── VNR.Infrastructure.Dapper/
│   │   ├── VNR.Infrastructure.Email/
│   │   ├── VNR.Infrastructure.FileSystem/
│   │   ├── VNR.Infrastructure.Notification/
│   │   ├── VNR.Infrastructure.DataExchange/
│   │   └── VNR.Infrastructure.Configuration/
│   │
│   ├── Modules/                       (8 × 5–6 project) ← nghiệp vụ
│   │   └── {Module}/
│   │       ├── VNR.Module.{M}.Domain/          ← entity, VO, domain event
│   │       ├── VNR.Module.{M}.Application/     ← vertical slice: command/query/handler
│   │       ├── VNR.Module.{M}.Infrastructure/  ← DbContext module, repo impl, DI
│   │       ├── VNR.Module.{M}.Contracts/       ← zero-dep, chia sẻ cross-module
│   │       ├── VNR.Module.{M}.Api/             ← controller
│   │       └── VNR.Module.{M}.SharedKernel/    ← (tuỳ chọn) type dùng chung nội bộ module
│   │
│   ├── Hosting/                       (3)
│   │   ├── VNR.Hosting.Api/              ← BaseApiController, BaseCrudApiController, middleware
│   │   ├── VNR.Hosting.CompositionRoot/  ← BaseStartup, đăng ký DI toàn cục
│   │   └── VNR.Hosting.WorkerHost/       ← host cho background worker
│   │
│   └── Processes/                     (9) ← đơn vị TRIỂN KHAI (mỗi cái 1 port/1 container)
│       ├── VNR.Process.Gateway/
│       ├── VNR.Process.Identity/
│       ├── VNR.Process.MasterData/
│       └── … (mỗi process gom vài Module.Api + Module.Infrastructure)
│
├── Tests/
│   ├── VNR.ArchTests/                 ← 36 file — CI gate, PHẢI xanh trước khi PR
│   ├── VNR.UnitTests/
│   └── VNR.IntegrationTests/
│
└── Tools/
    ├── VNR.Analyzers/                 ← Roslyn analyzer riêng, gắn vào Module.*.Application
    ├── VNR.Analyzers.Tests/
    ├── VNR.Tools.GridScaffold/
    └── VNR.Tools.UiConfigGen/
```

### [ĐƠN GIẢN HOÁ] cho hệ thống mới / nhỏ hơn

Không phải hệ thống nào cũng cần 13 project Infrastructure và 9 Process. Rút gọn
hợp lệ, **giữ nguyên hướng phụ thuộc**:

| Quy mô | Platform | Infrastructure | Modules | Processes |
| --- | --- | --- | --- | --- |
| Demo / 1 nhóm nhỏ | gộp 4 → **2** (`Core`, `Core.Persistence`) | gộp thành **1** `Infrastructure` | **1** module, 3 project (Domain/Application/Api) | **1** process = `Api` |
| Vừa (5–15 dev) | giữ **4** | tách 3–5 project theo mối bận tâm thật sự (Caching, Logging, Security…) | 2–5 module × 5 project | 1–3 process |
| Lớn (như Successor) | 4 | 13 | 8 × 5–6 | 9 |

**Ranh giới KHÔNG được rút gọn ở bất kỳ quy mô nào:**

```
✅ Module.Api          → Module.Application → Module.Domain
✅ Module.Infrastructure → Module.Application → Module.Domain
✅ Module.Contracts    → (không phụ thuộc gì)
✅ Process.*           → Module.Api + Module.Infrastructure

❌ Module.Api          → Module.Infrastructure
❌ Module.Application  → Module.Infrastructure
❌ Module.Application  → Microsoft.EntityFrameworkCore
❌ Infrastructure.A    → Infrastructure.B          (gate T091)
❌ Infrastructure.*    → Module.*.Domain           (gate T091b — xem "DIP Seam" ở P3)
❌ Module.A            → Module.B.Domain | Module.B.Infrastructure
❌ Domain              → [Column], [JsonIgnore], bất kỳ attribute hạ tầng nào
```

---

## 4. Thứ tự phụ thuộc — vì sao đúng thứ tự đó

```
        P0 solution + build props
                 │
                 ▼
        P1 Platform.Domain ──────────── zero package. Không ai chờ nó, nó chờ không ai.
                 │
                 ▼
        P2 Platform.Application ─────── phụ thuộc Domain. Định nghĩa INTERFACE mà
                 │                       Persistence/Infrastructure sẽ implement.
        ┌────────┴────────┐
        ▼                 ▼
  P3 Persistence    Infrastructure.*    ← cả hai implement interface của P2
        └────────┬────────┘
                 ▼
        P4 Hosting (Api + CompositionRoot)
                 │
                 ▼
        P5 Module đầu tiên (Domain→Application→Infrastructure→Api)
                 │
                 ▼
        P6 Process gom module + ArchTests + analyzer
```

Sai lầm thường gặp khi làm sai thứ tự:

| Làm sai | Hậu quả gặp thật |
| --- | --- |
| Viết `DbContext` trước `BaseEntity` | Entity dính `[Key]`/`[Column]`, Domain hết sạch, phải gỡ toàn bộ sang `IEntityTypeConfiguration` |
| Viết handler trước `IApiResult`/`ErrorDescriptor` | Handler trả `string` message → magic string tràn lan, i18n không gắn được, phải sửa lại mọi handler |
| Viết controller trước behavior pipeline | Mỗi controller tự `try-catch` → 20 chỗ format lỗi khác nhau, FE không parse nổi |
| Viết module trước khi chốt envelope | Grid trả shape khác endpoint thường → FE phải có 2 nhánh parse (**lỗi này VNR.Successor đã dính thật**, xem P4 §envelope) |

---

## 5. Mục lục các file trong `be/trien-khai/`

| File | Nội dung |
| --- | --- |
| [01-p0-nen-mong-solution.md](01-p0-nen-mong-solution.md) | Solution layout, `Directory.Build.props`, `Directory.Packages.props`, quy ước đặt tên, global usings |
| [02-p1-platform-domain.md](02-p1-platform-domain.md) | Toàn bộ class/interface của `Platform.Domain` kèm chữ ký thật |
| [03-p2-platform-application.md](03-p2-platform-application.md) | CQRS base class, 4 pipeline behavior, `IApiResult<T>`, `ErrorDescriptor`, contracts |
| [04-p3-platform-persistence.md](04-p3-platform-persistence.md) | `BaseDbContext`, 6 interceptor, `GenericRepository`, `UnitOfWork`, DIP Seam, chiến lược FK |
| [05-p4-hosting-api.md](05-p4-hosting-api.md) | `BaseApiController`, `BaseCrudApiController`, envelope, `ErrorCode → HTTP`, auth/permission |
| [06-p5-module-dau-tien.md](06-p5-module-dau-tien.md) | 2 pattern CRUD, cây file vertical slice, mapper, validator, DI |
| [07-p6-archtests-gate.md](07-p6-archtests-gate.md) | 36 ArchTest thật của Successor — cái nào cần từ ngày đầu, cái nào để sau |
| [08-tra-cuu-file-class.md](08-tra-cuu-file-class.md) | Bảng tra cứu: mọi file/class/interface → thuộc layer nào, phase nào, làm gì |

---

## 6. Áp dụng vào PlatformManager

> **Chuyển giai đoạn (2026-08-17):** mục này viết lúc PlatformManager còn ở
> quy mô demo — từ giờ đã sang giai đoạn phát triển product thật. Danh sách
> "chưa cần ngay" bên dưới **vẫn đúng cho riêng trục kiến trúc multi-module/
> multi-process** (lý do là SỐ LƯỢNG MODULE, không phải giai đoạn — xem
> [be/01-core-components.md](../01-core-components.md) §Áp dụng, đoạn về
> #11/#12). Nhưng danh sách "cần ngay" phải MỞ RỘNG — bảng đầy đủ + lý do
> từng mục nằm ở
> [be/01-core-components.md](../01-core-components.md) §Áp dụng (đối chiếu
> thêm Clean Architecture template/12-Factor/OWASP ngoài VNR) — mục này chỉ
> giữ lại phần khung kiến trúc P0-P6, không lặp lại bảng đó.

PlatformManager dùng cột đầu của bảng [ĐƠN GIẢN HOÁ] §3 cho **trục kiến trúc**
(số project/module/process) — trục này **không đổi** theo giai đoạn demo hay
product, chỉ đổi khi số lượng module nghiệp vụ tăng:

- 4 project (`Domain` / `Application` / `Infrastructure` / `Api`) là **đúng mức**
  cho hiện tại — không tách thêm Platform/Modules/Processes khi chưa có module
  thứ hai.
- Nhưng **thứ tự P1→P2→P3→P4 vẫn áp dụng nguyên vẹn**, và các ranh giới ở §3
  ("KHÔNG được rút gọn") vẫn phải giữ — vì gỡ ra sau này rất đắt, còn giữ từ đầu
  gần như miễn phí.
- Các thứ **chưa cần ngay** (đúng nghĩa "chưa có module 2" hoặc "chưa có bằng
  chứng nhu cầu", không phải "đang demo"): Process tách riêng, Outbox, Dapper
  grid engine, metadata-driven, i18n 2 tầng, `IEntitySearchConfig`, caching. Ghi
  vào backlog, đừng viết trước.

**Danh sách "cần ngay" — xem bảng đầy đủ + lý do ở
[be/01-core-components.md](../01-core-components.md) §Áp dụng vào
PlatformManager**, tóm tắt: `BaseEntity` + soft delete, `IApiResult` envelope,
`ErrorDescriptor`, `ValidationBehavior`+`ExceptionHandlingBehavior`, layer rule
có ArchTest, health check, Serilog, **permission theo hành động (OWASP #1)**,
`RowVersion` cho entity nhiều luồng ghi, **rate limiting**, **CI pipeline tự
động** — 4 mục in đậm cuối là bổ sung mới khi chuyển sang giai đoạn product,
chưa có trong lần rà soát P0-P6 gốc.

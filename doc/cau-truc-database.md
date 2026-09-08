---
kind: luat
scope: core
verified: 2026-09-03
---

# Schema `core` — cấu trúc database phần nền tảng (PostgreSQL)

> ### Phạm vi file này — đọc trước khi tìm một bảng ở đây
>
> File này mô tả **schema `core`**: những bảng đi theo khi tách CoreBase sang dự án khác.
> Bảng nghiệp vụ (`business.*`, module DTI Weekly) tách ra
> [`cau-truc-database-business.md`](cau-truc-database-business.md) ngày 2026-09-03 —
> file đó khai `scope: du-an`, file này khai `scope: core`.
>
> **Tách file, KHÔNG tách database.** Hai schema vẫn nằm trong cùng một database, xem §1.1.
>
> *Vì sao tách:* khoá `scope` của §9 `.claude/CLAUDE.md` là khoá **của cả file**, nên một
> file mô tả lẫn hai bên không khai được giá trị nào cho đúng. Bản trước khai `scope: du-an`
> cho cả file — tức tuyên bố 11 bảng `core` (đăng nhập, phân quyền, menu) **không** đi theo
> khi tách CoreBase. Câu đó sai, và chỉ lộ ra vào đúng ngày người ta thật sự tách.

> ### 🚧 Đọc trước: mọi tham chiếu `sql/00NN_*.sql` bên dưới là LỊCH SỬ (2026-08-31)
>
> Sáu file `sql/0003…0008` **không còn tồn tại**. Đợt baseline lại lịch sử migration
> ngày 2026-08-31 (§5.2) gộp tất cả vào **một** file duy nhất:
> `src/BE/Core/PlatformManager.Core.Infrastructure/Persistence/Migrations/sql/0001_initial_baseline.sql`.
>
> Đếm bằng lệnh thay vì tin đoạn văn nào trong file này:
>
> ```bash
> ls src/BE/Core/PlatformManager.Core.Infrastructure/Persistence/Migrations/sql/
> ```
>
> Các đoạn nhắc tên file cũ được **giữ nguyên có chủ đích** — chúng ghi lại *vì sao* một
> quyết định schema được đưa ra, và xoá đi là mất lý do. Nhưng đừng đi tìm file, và đừng
> chạy chúng.
>
> Mô tả 5 bảng `business.*` đã chuyển sang
> [`cau-truc-database-business.md`](cau-truc-database-business.md) (2026-09-03) cùng với
> lý do vì sao chúng vẫn còn trong DB có tuổi mà không còn trong DB dựng mới.

> Tài liệu mô tả cấu trúc DB thật (không phải ERD dự kiến). Đối chiếu
> `doc/kien-truc-core-module.md` để hiểu lý do tách Core ↔ Business ở tầng
> code — tài liệu này mô tả cách ranh giới đó phản ánh xuống DB.

> ### 🚧 Đổi tên 6 cột audit — code + migration xong 2026-08-28, DB THẬT chưa chạy
>
> Bộ tên đích đã vào code và đã có migration; **database thật vẫn mang tên cũ**
> cho tới khi người dùng tự chạy script (agent không được phép chạm DB — xem
> `.claude/CLAUDE.md` §1). Hợp đồng đầy đủ:
> [`huong_dan/quy-uoc/be-entity-domain.md`](huong_dan/quy-uoc/be-entity-domain.md).
>
> | Cũ | Mới (đích) |
> | --- | --- |
> | `UserCreate` · `UserUpdate` | `CreatedBy` · `UpdatedBy` |
> | `DateCreate` · `DateUpdate` | `CreatedAt` · `UpdatedAt` |
> | `IsDelete` | `IsDeleted` |
> | `Id` (`get; set;`) | `Id` (`get; init;`, `EntityId.New()` = `Guid.CreateVersion7()`) |
>
> | Có thật hôm nay (2026-08-28) | Sẽ thành (sau khi chạy script) |
> | --- | --- |
> | Entity/EF configuration/repository/test trong `src/BE` dùng bộ tên mới | (đã xong) |
> | Migration `20260828025008_RenameAuditColumns` + file `0007_rename_audit_columns.sql` (**không còn tồn tại** — gộp vào baseline 2026-08-31) | (đã xong) |
> | DB dev/chung vẫn còn cột `UserCreate`/`DateCreate`/`IsDelete`… | Chạy `0007_rename_audit_columns.sql` rồi tới `cau-truc-database.sql` |
>
> Tên **index** nhúng tên cột cũ cũng đổi trong cùng migration:
> `UX_CriteriaAssessments_CriteriaId_DateCreate_Day` →
> `UX_CriteriaAssessments_CriteriaId_CreatedAt_Day`, và
> `IX_CriteriaAssessments_CriteriaId_DateCreate` →
> `IX_CriteriaAssessments_CriteriaId_CreatedAt`. Index thứ nhất **không nằm
> trong EF model** (xem cảnh báo ở §4 bên dưới) nên `dotnet ef` **không** sinh
> lệnh đổi tên cho nó — lệnh `ALTER INDEX ... RENAME TO` được viết tay vào
> `0007_*.sql`.
>
> ⚠️ **Thứ tự chạy có ý nghĩa:** `0007_*.sql` TRƯỚC, `cau-truc-database.sql`
> SAU. Đảo lại (hoặc bỏ `0007`) thì `CREATE UNIQUE INDEX IF NOT EXISTS` — vốn
> kiểm theo **tên** — tạo thêm một index **trùng nội dung** dưới tên mới, nằm
> song song với index tên cũ. Đây không phải suy đoán: đã tái hiện trên
> container Postgres 16 khi kiểm chứng `0007`.

## 1. Vì sao tách schema, không dồn hết vào `public`

Cùng 1 database Postgres (`postgres` — hoặc tên DB thật bạn đặt), nhưng chia
làm **2 schema do app tự khai** thay vì để tất cả bảng dùng chung schema mặc
định `public`:

- **`core`** — bảng nền tảng, dùng lại được cho mọi sản phẩm dựng trên hệ
  thống này (đăng nhập, phân quyền, menu điều hướng). Không chứa gì đặc thù
  nghiệp vụ DTI Weekly.
- **`business`** — bảng đặc thù nghiệp vụ (hiện là DTI Weekly, sẽ mở rộng
  thêm tính năng sau này — xem `doc/kien-truc-core-module.md`).

**Lý do**: phản ánh đúng ranh giới đã tách ở tầng code (`Core.*` project ↔
`Modules.DtiWeekly.*`/`Business.*` project) xuống tận DB — nhìn cây schema
trong bất kỳ công cụ quản trị DB nào (DBeaver, pgAdmin...) là thấy ngay
bảng nào thuộc nền tảng, bảng nào thuộc nghiệp vụ, không cần đọc code. Làm
ngay từ khi DB còn trống (chưa có dữ liệu thật) rẻ hơn nhiều so với tách
sau khi đã có dữ liệu.

Kết quả: schema `public` mặc định của Postgres **không chứa object nào của
app** — kể cả `__EFMigrationsHistory` đã chuyển vào `core`, và hàm hỗ trợ
index `criteria_assessment_date_utc()` đã chuyển vào `business`.

## 1.1. Hai schema, MỘT database — chốt 2026-09-03, không tách đôi

> **Quyết định người dùng 2026-09-03: giữ nguyên hiện trạng.** *"DB core"* và *"DB
> business"* trong mọi cách nói trước đây là **hai schema trong cùng một database**, không
> phải hai database. Ghi mục này ra vì cách gọi tắt đó đủ để ai đó tưởng đang thiếu một
> bước tách và tự đi tách.

Hai lý do giữ, cả hai đều mất nếu tách thật:

| Lý do | Mất gì khi tách đôi |
|---|---|
| Còn **khoá ngoại** đi xuyên schema | FK không đi qua ranh giới database được — ràng buộc toàn vẹn phải viết lại thành kiểm ở tầng app, tức thành thứ có thể quên |
| Còn **giao dịch chung** | Một `SaveChanges` chạm cả hai bên đang là atomic; qua hai database thì phải dựng giao dịch phân tán hoặc chấp nhận ghi nửa vời |

Cơ chế thi hành nằm ở
`src/BE/Core/PlatformManager.Core.Infrastructure/Persistence/PlatformManagerDbContext.cs`
(đối chiếu 2026-09-03):

| Ai vào schema nào | Cách |
|---|---|
| Entity **không** khai schema | Rơi vào `core` — `modelBuilder.HasDefaultSchema(CoreSchema)` tại `:67`, hằng số `CoreSchema` tại `:40` |
| Entity nghiệp vụ | Khai **tường minh** ngay tại `ToTable()` trong `IEntityTypeConfiguration<T>`, dùng hằng số `BusinessSchema` tại `:52` |

Mặc định nghiêng về `core` là **có chủ đích và có rủi ro đi kèm**: quên khai schema thì
bảng nghiệp vụ lặng lẽ nằm trong `core` mà không lỗi gì. Đổi mặc định sang `business` chỉ
đảo chiều rủi ro chứ không gỡ nó, mà lại làm mọi bảng nền tảng phải khai tay. Lưới bắt ca
này là ArchTest `CoreMustNotKnowBusinessNameTests`, không phải trí nhớ người viết.

### Schema thứ 3: `hangfire` — không do EF Core tạo

Trên DB thật bạn sẽ thấy **3 schema, không phải 2**. Hangfire tự tạo schema
riêng của nó (mặc định tên `hangfire`) cùng bộ bảng nội bộ
(`job`, `jobqueue`, `jobparameter`, `state`, `server`, `lock`, `counter`,
`hash`, `list`, `set`, `schema`...) **lúc app khởi động lần đầu**, không đi
qua EF Core migration và không có trong bất kỳ file `.sql` nào của repo.

Nguồn: `src/BE/PlatformManager.Api/Program.cs` —
`UsePostgreSqlStorage(c => c.UseNpgsqlConnection(...))` **không khai
`SchemaName`**, nên Hangfire dùng mặc định.

Hệ quả cần biết:

- Bảng `hangfire.*` **không nằm trong migration**, backup/restore theo
  migration sẽ không dựng lại chúng — nhưng cũng không cần: Hangfire tự tạo
  lại khi app khởi động.
- Đừng coi việc thấy schema `hangfire` là bug hay là "schema thứ 3 do ai đó
  tự thêm sai luật" — nó là hạ tầng job nền, khác hẳn schema nghiệp vụ nói ở
  §6 bên dưới.
- Muốn đổi tên/gộp schema này thì set `SchemaName` trong
  `UsePostgreSqlStorage` — nhưng đổi trên DB đã chạy sẽ mất toàn bộ job đang
  chờ, chỉ làm khi DB còn trống.

## 2. Danh sách bảng theo schema

> Nguồn sự thật cho mục này: `src/BE/PlatformManager.Api/Persistence/Migrations/PlatformManagerDbContextModelSnapshot.cs`
> (model EF hiện hành), **không phải** file `.sql` (file `.sql` là delta từng
> migration, không phản ánh trạng thái tổng).
>
> Snapshot **chuyển sang project host ngày 2026-09-04** — trước đó nó nằm trong
> `Core.Infrastructure`. Vai trò *"snapshot là nguồn sự thật cho danh sách bảng"* không
> đổi, chỉ đổi chỗ để file; lý do và hệ quả ở §5.3.

### Schema `core`

| Bảng | Mô tả |
|---|---|
| `AspNetUsers` | Tài khoản đăng nhập (ASP.NET Core Identity), mở rộng thêm `FullName`/`DateCreate`/`DateUpdate`/`CreatedBy`/`UpdatedBy`/`MustChangePassword`. ⚠️ Hai cột thời gian tên là `DateCreate`/`DateUpdate` — **không** phải `CreatedAt`/`UpdatedAt` như bảng kế thừa `BaseEntity`; xem §4.1 |
| `AspNetRoles` | Vai trò: `SuperAdmin`, `Admin`, `User` |
| `AspNetUserRoles` | Gán role cho user (nhiều-nhiều) |
| `AspNetUserClaims` | Claim cấp user (Identity sinh sẵn, chưa dùng) |
| `AspNetRoleClaims` | Claim cấp role (Identity sinh sẵn, chưa dùng) |
| `AspNetUserLogins` | Đăng nhập ngoài (Google/Microsoft...), chưa dùng |
| `AspNetUserTokens` | Token nội bộ Identity (reset password, 2FA) |
| `RolePermissions` | ✅ **Phân quyền theo hành động** (đã thi công 2026-08-24 — xem §2.1) — role nào được phép chạm `ResourceKey` nào. Kế thừa `BaseEntity` từ 2026-08-31: **PK đơn `Id`**, tính duy nhất của cặp (`RoleId`, `ResourceKey`) giữ bằng **unique index lọc** `IX_RolePermissions_RoleId_ResourceKey_Active … WHERE "IsDeleted" = false`. `ResourceKey` `varchar(100)`; FK `RoleId → AspNetRoles.Id` `ON DELETE CASCADE`. Index phụ `IX_RolePermissions_ResourceKey_RoleId` (`ResourceKey`, `RoleId`) — xem §4 |
| `SysMenus` | Menu điều hướng động (sidebar), tự tham chiếu `ParentId` cho cây 1 cấp |
| `SysMenuRoles` | Role nào được thấy menu nào (nhiều-nhiều) |
| `__EFMigrationsHistory` | Bảng nội bộ EF Core theo dõi migration đã áp dụng |

> **⚠️ `RolePermissions` rỗng = mọi role (trừ `SuperAdmin`) bị 403** ở endpoint
> nào mang `[RequirePermission]`. `RequirePermissionFilter` là deny-by-default.
>
> ### 🚧 Cập nhật 2026-08-29 — hạ tầng quyền còn nguyên, chỗ DÙNG nó thì không
>
> Đợt gỡ module DtiWeekly đã xoá cả 3 controller từng mang `[RequirePermission]`
> (`CriteriaController`/`CriteriaGroupsController`/`ImportController`). Hiện
> **không controller production nào** khai `[RequirePermission]` — đếm bằng
> `grep -rn 'RequirePermission(' src/BE --include=*.cs | grep -v /Tests/`.
> Hệ quả: bảng `RolePermissions` rỗng lúc này **không** chặn ai cả, và
> `RequirePermissionFilter` chỉ còn được canh bởi test qua probe controller
> (`src/BE/Tests/PlatformManager.Core.IntegrationTests/Permissions/PermissionSeamProbeController.cs`
> + `src/BE/Tests/PlatformManager.Core.IntegrationTests/Permissions/RequirePermissionSeamTests.cs`).
>
> Danh mục permission-key cũng đã rút lại — 2 key DTI (`criteria.manage`,
> `criteria-groups.manage`) gỡ cùng module, chỉ còn `import.manage`.
>
> **Sửa 2026-09-03:** lớp `ResourceKeys` trong Core **không còn tồn tại**. Danh mục
> nay là dữ liệu của host, cấp qua seam
> [`ICoreResourceKeySource.cs`](../src/BE/Core/PlatformManager.Core.Application/Permissions/ICoreResourceKeySource.cs)
> — host khai ở `src/BE/PlatformManager.Api/Permissions/AppResourceKeySource.cs`.
> `CoreSeeder.SeedRolePermissionsAsync()` lặp theo danh mục seam trả về nên tự co
> theo. `scripts/seed-role-permissions.sql` đã được sửa cùng chiều ngày
> **2026-08-29** — nay chỉ chèn `import.manage`
> (`scripts/seed-role-permissions.sql:62`), chính script ghi lý do ở dòng 52. Chỉ dẫn
> đối chiếu trong script trỏ sang `AppResourceKeySource.cs` từ **2026-09-04** (trước đó
> vẫn trỏ file `ResourceKeys.cs` đã xoá).
>
> Còn lại đúng một dư âm, và nó **không** tự biến mất: DB production nào đã chạy
> **bản script cũ** vẫn giữ 2 dòng quyền trỏ tới key không còn trong code. Vô hại
> (filter chỉ tra key nó cần) nhưng là rác — script ghi cách dọn ở dòng 77.
>
> *(Bản trước của đoạn này nói script "vẫn chèn cứng cả 3 key". Đúng khi viết,
> sai từ 2026-08-29. Sửa 2026-08-30.)*
>
> Cách seed **đã đổi 2026-08-30**: hàng rào `IsDevelopment()` quanh `CoreSeeder` đã gỡ, seed
> production nay là lệnh riêng `dotnet run -- --seed` gọi chính `CoreSeeder` (đọc
> `huong_dan/wiki-core/be/13-core-data-migration.md` §"Quyết định người dùng 2026-08-30").
> `scripts/seed-role-permissions.sql` giữ lại cho ca chỉ có quyền truy cập DB mà không chạy
> được binary
> (idempotent, `ON CONFLICT DO NOTHING`) — **bắt buộc chạy** trước hoặc cùng lúc
> với migration `0004_role_permission_import_job.sql` (**file này không còn tồn tại** — đã gộp vào `0001_initial_baseline.sql` khi baseline lại lịch sử 2026-08-31) **ngay khi có endpoint
> `[RequirePermission]` trở lại**, nếu không mọi role trừ `SuperAdmin` bị 403
> hàng loạt trên chính DB đó. Xem `doc/contracts/permissions.md` §"Rủi ro
> rollout" và §2.1 `doc/huong_dan/wiki-core/be/13-core-data-migration.md`
> ("expand trước, contract sau").
>
> *(Lịch sử: trước 2026-08-24, `CoreSeeder` không có method seed bảng này —
> bảng rỗng ở MỌI môi trường kể cả Development. Xem
> `doc/huong_dan/wiki-core/be/13-core-data-migration.md` §"Áp dụng vào
> PlatformManager" để biết đầy đủ bối cảnh khoảng trống đó.)*

> ### 🚧 ĐÓNG BĂNG 2026-08-29 — 3 dòng `SysMenus` mồ côi, giữ lại có chủ đích
>
> *(Banner này nằm ở phần `core` vì `SysMenus` là bảng `core`. Trước 2026-09-03 nó bị xếp
> nhầm dưới tiêu đề "Schema `business`" chỉ vì 3 dòng dữ liệu do module DTI gieo — dữ liệu
> của ai không đổi được bảng nằm ở schema nào.)*
>
> Bảng `SysMenus` (schema `core`) còn 3 dòng do seeder của module DTI gieo khi
> module còn tồn tại: `dashboard`, `danh-muc`, `danh-muc-dti`. `CoreSeeder`
> chỉ upsert những mục do **host khai** (từ 2026-09-02 qua `ICoreMenuSeedSource`; hôm nay 4
> mục) và **không bao giờ xoá** mục do module đóng góp, mà
> seeder của module thì đã biến mất cùng nó.
>
> **Triệu chứng nhìn thấy được** (đối chiếu 2026-08-29, chạy app thật): sidebar
> hiện 3 mục đó; bấm vào thì route không tồn tại nên wildcard đưa **âm thầm**
> về `/trang-chu` — người dùng không nhận được thông báo nào. Cùng lúc, trang
> chủ ngay bên cạnh lại ghi *"Chưa có module nghiệp vụ nào được cài đặt"*.
>
> **Quyết định của người dùng: GIỮ NGUYÊN**, chờ module DTI dựng lại. Khi đó
> seeder của module sẽ hồi sinh đúng 3 dòng này — `UpsertMenuAsync` đã xử lý ca
> xoá mềm nên không sinh bản trùng. Kiểm lại danh sách bằng lệnh thay vì tin
> đoạn văn này: `GET /api/meta/menu` với phiên SuperAdmin.
>
> Đây **không phải bug cần sửa** — ghi ở đây để lần sau ai gặp thì biết là có
> chủ đích, thay vì mở một phiếu lỗi rồi tự "dọn" mất.

### Schema `business` — đã tách sang file riêng (2026-09-03)

Năm bảng `business.*` (`CriteriaGroups`, `Criteria`, `CriteriaAssessments`,
`CriteriaEvidences`, `ImportJobs`) thuộc module DTI Weekly đã gỡ, nên chúng **không** đi
theo khi tách CoreBase. Toàn bộ mô tả cột, ràng buộc và trạng thái đóng băng của chúng nay
ở [`cau-truc-database-business.md`](cau-truc-database-business.md).

Ở lại file này đúng **một** điều, vì nó là luật của `core`: entity **không** khai schema sẽ
rơi vào `core` chứ không phải `business` — xem §1.1.

## 2.1. 🚧 `RolePermissions` — hạ tầng còn, đường dùng đã gỡ (2026-08-29)

> ### 🚧 Đọc banner này TRƯỚC bảng bên dưới
>
> Bảng "Kiểm 2026-08-24" ở dưới mô tả **một thời điểm đã qua**. Đợt gỡ module
> DtiWeekly ngày **2026-08-29** đã xoá phần lớn cột phải của nó. Cột "Kiểm
> 2026-08-29" là hiện trạng đang có; hai cột kia giữ lại để thấy chuyện gì đã
> xảy ra, **không phải để tra hiện trạng**.
>
> Đây đúng khuôn sai mà `.claude/CLAUDE.md` §4 mô tả: nhãn "đã xong" được thiết
> kế để không ai kiểm lại, nên khi code biến mất thì nhãn vẫn nằm nguyên đó.
> Bản 2026-08-24 của mục này khẳng định `[RequirePermission]` "ĐÃ GẮN" lên 3
> controller và `ImportController` gọi `StartImportCommand` — cả 4 file đó nay
> không tồn tại, mà không có lỗi biên dịch hay test nào báo.

| Mã | Kiểm 2026-08-23 (lạc hậu) | Kiểm 2026-08-24 (lạc hậu) | Kiểm 2026-08-29 (hiện trạng) |
| --- | --- | --- | --- |
| Entity/configuration `RolePermission` | chưa có | có | **còn** — `src/BE/Core/PlatformManager.Core.Domain/Entities/RolePermission.cs`, `src/BE/Core/PlatformManager.Core.Infrastructure/Persistence/Configurations/RolePermissionConfiguration.cs` |
| Entity `ImportJob` | chưa có | có | **đã xoá cùng module** — không còn file `ImportJob.cs` nào trong solution; bảng DB thì còn hay không tuỳ DB dựng lúc nào — xem [`cau-truc-database-business.md`](cau-truc-database-business.md) |
| `RequirePermissionFilter` + danh mục permission-key | chưa có | có, gắn lên 3 controller DTI | **còn**, deny-by-default — nhưng **không controller production nào khai `[RequirePermission]`** nữa. Lớp `ResourceKeys` trong Core **đã xoá** 2026-09-03; danh mục nay do host cấp qua seam, còn đúng `import.manage` |
| `AddPermissionInfrastructure()` + `options.Filters.Add<RequirePermissionFilter>()` ở `Program.cs` | chưa có | có | **còn** — filter vẫn nằm trong pipeline MVC, canh bằng seam activation test `RequirePermissionSeamTests` qua probe controller riêng của test |
| `CoreSeeder.SeedRolePermissionsAsync()` | chưa có | có, seed toàn bộ danh mục key cho `Admin`/`User` | **còn**, nay lặp theo danh mục **seam của host** trả về (lớp `ResourceKeys` trong Core đã xoá 2026-09-03) nên tự co theo |
| `Hangfire` (`AddHangfire`/`AddHangfireServer`/`UseHangfireDashboard`) ở `Program.cs` | chưa có | có | **còn** — năng lực dùng chung của Core, không thuộc module. Storage dùng chung connection string `Default`, tự tạo schema `hangfire` lúc khởi động lần đầu |
| `ImportController` gọi `StartImportCommand` (đường job nền) | chưa có (còn gọi `ImportCsvCommand` đồng bộ cũ) | có — `POST /api/import` | **đã xoá cùng module** — không còn `ImportController`, `StartImportCommand`, `IImportJobRunner`, `IImportFileStorage`, `IImportJobRepository`, `IImportFileReader` |
| `ModelSnapshot` khớp model hiện hành | chưa | khớp (migration thử ra RỖNG) | **KHÔNG khớp — có chủ đích** *(câu này đúng tới 2026-08-30, hết hạn từ 2026-08-31)*. Baseline ở §5.2 đã dựng lại snapshot khớp model; đối chiếu 2026-09-03 thì `grep -c CriteriaAssessment` trên snapshot ra `0`. Xem [`cau-truc-database-business.md`](cau-truc-database-business.md) §"Cái bẫy `DropTable`" |

**Định nghĩa đầy đủ để thi công** — đây là bản duy nhất còn lại sau khi gộp
`doc/ERD/` (đã xoá), nên ghi đủ cả tên constraint:

`core."RolePermissions"` — `Id` `uuid` NOT NULL · `RoleId` `uuid` NOT NULL ·
`ResourceKey` `varchar(100)` NOT NULL · 5 cột audit của `BaseEntity`
(`CreatedBy` `varchar(50)`, `UpdatedBy` `varchar(50)`, `CreatedAt`
`timestamptz`, `UpdatedAt` `timestamptz`, `IsDeleted` `boolean` NOT NULL)
· **PK đơn** `PK_RolePermissions` (`Id`)
· FK `FK_RolePermissions_AspNetRoles_RoleId` → `core."AspNetRoles"("Id")`
**ON DELETE CASCADE** (cùng quy ước `SysMenuRoles`: xoá role thì gỡ luôn quyền)
· **unique index lọc** `IX_RolePermissions_RoleId_ResourceKey_Active`
(`RoleId`, `ResourceKey`) `WHERE "IsDeleted" = false`
· index phụ `IX_RolePermissions_ResourceKey_RoleId`.

> ### Vì sao KHÔNG phải PK ghép (`RoleId`, `ResourceKey`) — sửa 2026-09-08
>
> Bảng này kế thừa `BaseEntity` từ 2026-08-31, mà `BaseEntity` kéo theo
> **soft-delete** (`IsDeleted` + global query filter). Với soft-delete, cùng một
> cặp (`RoleId`, `ResourceKey`) **phải được phép tồn tại nhiều lần**: một dòng
> đang sống cộng với các dòng đã xoá mềm giữ lại để tra lịch sử. PK ghép cấm
> đúng điều đó — cấp lại một quyền đã từng thu hồi sẽ vi phạm khoá chính.
>
> Vì vậy PK là `Id` (do `BaseEntity` cấp), còn tính duy nhất **theo nghiệp vụ**
> hạ xuống thành unique index **partial** chỉ áp trên các dòng chưa xoá
> (`WHERE "IsDeleted" = false`). Đây là cùng một quyết định, cùng lý do và cùng
> hệ quả với `SysMenuRoles` — xem
> [`huong_dan/quy-uoc/be-entity-domain.md`](huong_dan/quy-uoc/be-entity-domain.md)
> §"Quyết định người dùng 2026-08-31", và bảng *"có thật hôm nay → sẽ thành"*
> ở §5.2 của chính file này (dòng `SysMenuRole` / `RolePermission`).
>
> Bản trước của mục này ghi *"PK ghép … **không** có cột `BaseEntity`"* — đúng
> với schema trước 2026-08-31, sai từ ngày baseline. Đối chiếu 2026-09-08:
> `src/BE/Core/PlatformManager.Core.Infrastructure/Persistence/Migrations/sql/0001_initial_baseline.sql:106-116`
> (bảng + `PRIMARY KEY ("Id")`), `…:254` (unique index partial),
> `src/BE/Core/PlatformManager.Core.Domain/Entities/RolePermission.cs:26`
> (`: BaseEntity`).

Định nghĩa `business."ImportJobs"` **không** còn ở đây — nó là bảng nghiệp vụ, xem
[`cau-truc-database-business.md`](cau-truc-database-business.md) §`ImportJobs`.

### Vì sao `IX_RolePermissions_ResourceKey_RoleId` là bắt buộc, không phải tối ưu sớm

PK là `Id` (khoá thay thế, không mang nghĩa nghiệp vụ) nên **không seek được**
cho truy vấn nào cả. Unique index nghiệp vụ
`IX_RolePermissions_RoleId_ResourceKey_Active` thì dẫn đầu bằng `RoleId`. Nhưng
truy vấn nóng nhất hệ thống — `RequirePermissionFilter`, chạy trên **mọi**
request có `[RequirePermission]` — lọc **trước hết theo `ResourceKey`** rồi mới
ghép `RoleId`. Cả hai index kia đều không dẫn đầu đúng cột đó (quy tắc Q2 ở
`doc/huong_dan/quy-uoc/be-performance.md`). Index này dẫn đầu đúng cột được lọc
và phủ luôn cột join → **index-only scan**.

*(Sửa 2026-09-08: câu mở đầu cũ là "PK ghép dẫn đầu bằng `RoleId`" — mô tả
schema trước baseline 2026-08-31. Kết luận "index này bắt buộc" không đổi, chỉ
đổi lý do: trước là "PK dẫn sai cột", nay là "PK không mang nghĩa nghiệp vụ".)*

> **Đọc `EXPLAIN` cho đúng ở bảng nhỏ.** Với ~6 dòng `RolePermissions`, Postgres
> vẫn chọn **Seq Scan** vì cả bảng nằm gọn trong một page — đó là **lựa chọn
> ĐÚNG của planner, không phải dấu hiệu index vô dụng**. Index có giá trị khi số
> tổ hợp (role × resource key) tăng; chi phí duy trì gần bằng 0 vì bảng ghi rất
> hiếm. Đừng gỡ index chỉ vì `EXPLAIN` trên dữ liệu seed không dùng tới nó.

*(Tri thức trong mục này trước nằm ở header `doc/ERD/migrations/0005_*.sql`, file
đã xoá khi gộp nguồn schema — xem §5.)*

## 3. Quan hệ xuyên schema — luật một chiều

**FK được phép đi `business` → `core`. Không bao giờ ngược lại.** Nghiệp vụ được phép
biết về Core; Core không được biết về nghiệp vụ. Đây là cùng một luật đã chốt ở tầng code
(`doc/kien-truc-core-module.md`), chiếu xuống DB.

Luật ở lại file này vì nó là **ràng buộc của Core** — nó vẫn đúng, và vẫn phải được tuân,
kể cả khi không còn bảng nghiệp vụ nào. Thể hiện cụ thể hôm nay (đúng một khoá ngoại, từ
`CriteriaAssessments`) nằm ở
[`cau-truc-database-business.md`](cau-truc-database-business.md) §"Khoá ngoại xuyên schema",
vì nó mô tả một bảng `business`.

Chính chiều FK này là một trong hai lý do hai schema **ở lại chung một database** — §1.1.

## 4. Ràng buộc đáng chú ý

- **Soft-delete 2 lớp** — cột `IsDeleted` + filtered unique index chỉ tính trên
  dòng `IsDeleted = false`, nhờ đó xoá mềm một mã rồi tạo lại đúng mã đó vẫn
  thành công. Đây là **quy ước chung của cả hai schema**. Ca thuộc `core`:
  `IX_SysMenus_Code` (từ `0008`). Ca thuộc `business`
  (`IX_Criteria_Code_Active`, `IX_CriteriaGroups_Code_Active`):
  [`cau-truc-database-business.md`](cau-truc-database-business.md).

  > ✅ **`SysMenus` KHÔNG còn là ngoại lệ — vá 2026-08-28 bằng migration `0008`.**
  > `IX_SysMenus_Code` nay là unique **partial** `WHERE "IsDeleted" = false`,
  > khai tại
  > `src/BE/Core/PlatformManager.Core.Infrastructure/Persistence/Configurations/SysMenuConfiguration.cs`
  > và áp bằng
  > file `0008_sysmenu_code_partial_unique_index.sql` (**không còn tồn tại** — gộp
  > vào `0001_initial_baseline.sql` khi baseline lại lịch sử 2026-08-31).
  >
  > **Tên index giữ nguyên** `IX_SysMenus_Code` (không đổi sang hậu tố
  > `_Active` như 2 index kia) — tên cũ đã có mặt ở tài liệu này và trong DB
  > thật; đổi tên chỉ thêm một bước đồng bộ tay ở mọi môi trường.
  >
  > **Lịch sử — vì sao từng hỏng:** bản `0001` đã có
  > `ux_sysmenu_code ... WHERE "IsDeleted" = false`; mệnh đề partial **mất
  > trong lần dựng lại `0003`** và không ai ghi nhận, nên suốt từ đó "xoá mềm
  > một menu rồi tạo lại cùng `Code`" thất bại vì trùng khoá trong khi
  > `Criteria`/`CriteriaGroups` thì được. Phát hiện 2026-08-23, vá 2026-08-28.
  >
  > **Chống tái phát:** hành vi này nay có integration test chạy trên Postgres
  > thật —
  > `src/BE/Tests/PlatformManager.Core.IntegrationTests/Menu/SysMenuCodeUniquenessTests.cs`
  > (một ca thuận: tái dùng mã sau xoá mềm phải thành công; một ca ngược: hai
  > menu cùng mã cùng chưa xoá vẫn phải bị chặn `23505`). Gỡ `0008` khỏi
  > `PostgresFixture.MigrationScripts` là ca thuận đỏ ngay — đã kiểm bằng
  > canary 2026-08-28.
> ### Ràng buộc `UX_CriteriaAssessments_*` + hàm SQL — đã chuyển (2026-09-03)
>
> Unique index theo biểu thức trên `business."CriteriaAssessments"` và hàm
> `business.criteria_assessment_date_utc()` là object của schema `business`, nên mô tả
> chuyển sang [`cau-truc-database-business.md`](cau-truc-database-business.md)
> §"Ràng buộc riêng của schema này".
>
> **Bản SQL nguyên văn thì KHÔNG chép sang** — nó đã có sẵn ở
> [`cau-truc-database.sql`](cau-truc-database.sql) từ trước
> (`doc/cau-truc-database.sql:73` và `:92`). Khối `sql` từng nằm ở đây kèm câu *"đây là
> bản duy nhất còn lại, đừng xoá khối này"* là **bản thứ hai**, và câu tự nhận "duy nhất"
> chính là dấu hiệu: người viết tin mình đang giữ bản cuối nên không đi tìm bản kia. Gỡ
> theo §5 `.claude/CLAUDE.md` — một chủ đề, một file chủ.

> Quy ước *"luôn sinh script DELTA, không bao giờ sinh full"* — hệ quả trực tiếp của việc
> có DDL viết tay nằm ngoài EF — ở **§5**, đừng chép lại ở đây.

- **Hầu hết `Id` do ứng dụng tự sinh, KHÔNG để DB tự sinh** — mọi `Id` kiểu
  `uuid` ở `core` (`SysMenus`, `AspNetUsers`, `AspNetRoles`) đều **không** có
  `DEFAULT gen_random_uuid()`; quy ước này áp cho cả `business` khi module quay
  lại. Ứng dụng tự
  tạo `Guid` trước khi insert (`EntityId.New()`), tránh đúng lỗi EF Core
  hiểu nhầm key-đã-set = "đã tồn tại" khi thêm entity con vào collection đã
  tracked.
  **Ngoại lệ — 2 bảng Identity dùng identity column của Postgres:**
  `AspNetUserClaims.Id` và `AspNetRoleClaims.Id` là `integer` +
  `ValueGeneratedOnAdd()` (`UseIdentityByDefaultColumn`, tức
  `GENERATED BY DEFAULT AS IDENTITY`) — đây là schema chuẩn của ASP.NET Core
  Identity, **cố ý không sửa**. 2 bảng này hiện chưa dùng (xem §2), nhưng
  đừng khẳng định "không có `Id` nào DB tự sinh" khi đối soát schema.

### 4.1. Quyết định thiết kế phần Core — đọc trước khi sửa bảng Identity/Menu

Phần Core được tái dùng cho mọi sản phẩm dựng trên nền tảng này, nên những
quyết định dưới đây có tuổi thọ dài hơn nghiệp vụ DTI Weekly.

**`AppUser` KHÔNG kế thừa `BaseEntity` — và đó là chủ đích.** Identity tự quản
lý vòng đời user bằng field riêng (`LockoutEnd`, `SecurityStamp`…), không áp
`IsDeleted`. Bốn cột vết mở rộng thêm là `DateCreate` · `DateUpdate` ·
`CreatedBy` · `UpdatedBy`.

⚠️ **Hai cột thời gian KHÔNG trùng quy ước `BaseEntity`** — bảng kế thừa
`BaseEntity` dùng `CreatedAt`/`UpdatedAt`, còn bảng này giữ
`DateCreate`/`DateUpdate`. Đợt đổi tên 6 cột audit ngày 2026-08-28 (banner đầu
file) chỉ chạm entity kế thừa `BaseEntity`, mà `AppUser` là
`IdentityUser<Guid>` nên nằm ngoài phạm vi. **Đừng "sửa cho đồng bộ"** — cả
bằng cách bắt nó kế thừa `BaseEntity`, lẫn bằng cách đổi tên hai cột này: đây
là bảng do Identity sở hữu, đổi tên cột chỉ để cho đẹp là một migration có rủi
ro mà không đổi lấy được năng lực nào.

Hai cột `CreatedBy`/`UpdatedBy` thì **điền tay** ở tầng service, không qua
`AuditInterceptor` — interceptor chỉ chạm entity kế thừa `BaseEntity`.

> Đối chiếu 2026-09-08:
> `src/BE/Core/PlatformManager.Core.Infrastructure/Identity/AppUser.cs:14-15`
> (`DateCreate`/`DateUpdate`), `…:29,32` (`CreatedBy`/`UpdatedBy`), và DDL
> `src/BE/Core/PlatformManager.Core.Infrastructure/Persistence/Migrations/sql/0001_initial_baseline.sql`
> (bảng `core."AspNetUsers"`). Bản trước của đoạn này gọi hai cột thời gian là
> `CreatedAt`/`UpdatedAt` và khẳng định chúng *"cố ý đặt trùng quy ước
> `BaseEntity`"* — sai cả tên lẫn kết luận, và bỏ sót hai cột `CreatedBy`/`UpdatedBy`
> thêm ngày 2026-08-31.

**"SysUser" = `AppUser`/`AspNetUsers`, không phải bảng thứ hai.** Cần thêm
thông tin người dùng thì **mở rộng `AppUser`** (thêm cột), tuyệt đối không tạo
một bảng user song song.

**Khoá/mở tài khoản đi qua `LockoutEnd`, KHÔNG thêm cột `IsActive`.**

```
LockoutEnd IS NULL  hoặc  LockoutEnd < now()   →  "Đang hoạt động"
LockoutEnd >= now()                            →  "Đã khoá"
```

Khoá = `UserManager.SetLockoutEndDateAsync` với một mốc xa trong tương lai —
**không tự ghi cột này bằng tay**, và không thêm cột `IsActive` riêng (trùng
lặp khái niệm với cơ chế Identity sẵn có).

**KHÔNG hash mật khẩu trong SQL.** Identity dùng `PasswordHasher<TUser>`
(PBKDF2, salt ngẫu nhiên mỗi lần) — **không có cách nào tạo hash hợp lệ bằng
SQL thuần**. Tài khoản đầu tiên phải tạo qua code thật
(`UserManager.CreateAsync(user, password)`), không phải qua migration hay
script seed.

**Cây menu đúng MỘT cấp.** Item cha (`ParentId IS NULL` nhưng có con) có
`Route = NULL` — cha chỉ toggle mở/đóng, không điều hướng. Con không có con
riêng.

**`SysMenus.Code` là khoá ổn định, KHÔNG đổi sau khi đã dùng.** Route và quyền
có thể đổi; `Code` thì không — nó là key cho `@for track` phía FE và là điểm
neo của mọi tham chiếu menu.

**Seed đúng những gì có trang thật.** Không thêm mục menu cho trang chưa tồn
tại, kể cả khi đã dự phòng chỗ trong cấu trúc cây.

> *(Bảy quyết định trên trước nằm ở `doc/ERD/ERD-corebase.md`, file đã xoá khi
> gộp nguồn schema. Đây là bản duy nhất còn lại.)*

## 5. Dựng lại database từ đầu — hai bước, không phải một

> ### 🚧 Mục này mô tả quy trình CŨ (sửa 2026-08-30, chưa thi công xong)
>
> Hai chỗ dưới đây không còn đúng, giữ nguyên văn để thấy sai ở đâu:
>
> 1. **Bước 1 bảo chạy `dotnet ef database update`.** Lệnh đó bị
>    `.claude/settings.json` chặn thẳng, và đi ngược quyết định đóng băng
>    migration. [`README.md`](README.md) cùng lúc lại chỉ sang
>    [`db-khoi-tao.sql`](db-khoi-tao.sql) — **hai nguồn nói ngược nhau về cùng
>    một việc**, đúng thứ §5 của `.claude/CLAUDE.md` sinh ra để chặn.
> 2. **"Hai bước, không phải một" đã thành một bước.** `db-khoi-tao.sql` chứa
>    sẵn phần DDL viết tay (`grep -c criteria_assessment_date_utc doc/db-khoi-tao.sql`
>    ra khác 0), nên bước 2 không còn tách rời.
>
> Đường dựng DB **đang đúng hôm nay**: chạy [`db-khoi-tao.sql`](db-khoi-tao.sql)
> trong DBeaver, một file, một lần. Xem §5.2 cho việc sắp đổi.

**Nguồn schema từ 2026-08-23 chỉ còn hai file, cùng tên khác đuôi:**

| File | Vai | `scope` |
| --- | --- | --- |
| [`cau-truc-database.md`](cau-truc-database.md) *(chính file này)* | Mô tả schema **`core`** — bảng, cột, ràng buộc, quyết định thiết kế | `core` |
| [`cau-truc-database-business.md`](cau-truc-database-business.md) | Mô tả schema **`business`** (đóng băng, tách ra 2026-09-03) | `du-an` |
| [`cau-truc-database.sql`](cau-truc-database.sql) | **DDL viết tay** EF không sinh được — nay toàn bộ nội dung là `business`, mang banner lịch sử | `du-an` |

> Ba file, hai schema, **một** database (§1.1). Số file không phải số database.

```bash
# Bước 1 — EF dựng toàn bộ bảng/cột/khoá/index thông thường
dotnet ef database update --project src/BE/Core/PlatformManager.Core.Infrastructure \
                          --startup-project src/BE/PlatformManager.Api

# Bước 2 — BẮT BUỘC, không được bỏ
psql -d <database> -f doc/cau-truc-database.sql
```

> ### ⚠️ Bỏ bước 2 = DB thiếu ràng buộc, im lặng
>
> `doc/cau-truc-database.sql` chứa hàm `criteria_assessment_date_utc` và unique
> index theo **biểu thức hàm + partial filter** — EF Core không có cách nào sinh
> chúng từ entity/configuration. Thiếu chúng, ràng buộc *"1 đánh giá / 1 chỉ
> tiêu / 1 ngày"* biến mất và **dữ liệu trùng lọt vào mà không lỗi, không test
> nào báo**. Kiểm sau khi chạy: `\di business.*` phải thấy
> `UX_CriteriaAssessments_CriteriaId_CreatedAt_Day`.

**Sinh migration mới:** luôn dùng script **DELTA**, không bao giờ sinh full.

```bash
dotnet ef migrations script <MigrationTrước> <MigrationMới> --idempotent
```

Full script sẽ dựng lại DB **thiếu** 2 đoạn ở `cau-truc-database.sql` — nếu buộc
phải sinh full, chạy lại bước 2 sau đó.

> **Lịch sử:** trước 2026-08-23, schema được mô tả rải ở **6 nguồn** —
> `ERD.md`, `ERD-corebase.md`, 2 file `.dbml`, 5 file `migrations/*.sql`, và
> chính file này — với **6 con số bảng khác nhau** và 2 bộ tên cột `BaseEntity`
> mâu thuẫn. Toàn bộ `doc/ERD/` đã xoá; tri thức còn giá trị đã chuyển vào §2.1,
> §4 và §4.1. Luật nghiệp vụ DTI thì **không** chuyển — chúng đã có bản đầy đủ
> hơn ở `spec/danh-muc-dti/` và `spec/dashboard-dti/`.

### 5.1. ⚠️ SEED BẮT BUỘC sau khi chạy `0004` trên DB thật

`RequirePermissionFilter` là **deny-by-default**: bảng `core."RolePermissions"`
rỗng nghĩa là **MỌI role trừ `SuperAdmin`** (có bypass tường minh) đều bị
**403** ở mọi endpoint gắn `[RequirePermission]` (Criteria / CriteriaGroups /
Import) — kể cả thao tác họ vẫn làm được trước khi chạy `0004`.

`CoreSeeder.SeedRolePermissionsAsync()` cấp đủ key cho `Admin` + `User` (giữ
nguyên hành vi cũ), **và hôm nay seeder chỉ chạy khi `IsDevelopment()`** (gate
trong `Program.cs`).

> 🚧 **Đổi 2026-08-30 — câu "phải seed tay tương đương" không còn đúng.** Đường
> production nay là **một lệnh seed riêng** gọi chính `CoreSeeder`, nên nó phủ luôn
> bảng này; script SQL còn lại chỉ dành cho trường hợp chỉ có quyền truy cập DB mà
> không chạy được binary. Đọc
> [`huong_dan/wiki-core/be/13-core-data-migration.md`](huong_dan/wiki-core/be/13-core-data-migration.md)
> §"Quyết định người dùng 2026-08-30".

### 5.2. ✅ CÓ THẬT — baseline lại lịch sử migration (thi công xong 2026-08-31)

#### Cái mìn

Snapshot của EF đã lệch khỏi model thật. Đối chiếu 2026-08-30:

```bash
# 5 entity này CÓ trong snapshot, KHÔNG có trong source
for e in Criteria CriteriaAssessment CriteriaEvidence CriteriaGroup ImportJob; do
  echo "$e: snapshot=$(grep -c "$e" src/BE/PlatformManager.Api/Persistence/Migrations/PlatformManagerDbContextModelSnapshot.cs)"        "source=$(grep -rl "class $e" src/BE --include=*.cs | grep -v obj | grep -v Migrations | wc -l)"
done
```

`dotnet ef migrations add` sinh migration bằng cách **so model hiện tại với
snapshot**. Snapshot thừa 5 bảng ⇒ migration kế tiếp chứa **5 lệnh `DropTable`**.

Đường đi tới đó là một lệnh trông bình thường: `src/BE/scripts/db.ps1 -AddMigration <tên>`
rồi `-ScriptOutput <file>.sql`, và quy trình đã ghi là *"tự đọc lại rồi tự chạy tay
trên Postgres"*. `db.ps1` **không** có lỗi — nó không bao giờ chạm DB thật. Lưới an
toàn duy nhất là người đọc phát hiện ra `DROP TABLE` giữa một script sinh tự động.

`db-khoi-tao.sql` tạo cả 5 bảng đó, nên mọi DB dựng theo đúng hướng dẫn hiện tại
đều nằm trong tầm.

#### Chốt

**Baseline lại**: xoá 6 migration + snapshot, sinh một `InitialCreate` mới khớp
model hôm nay, sinh lại `db-khoi-tao.sql` từ đó.

Chốt được phương án này vì **chưa có DB production nào** (người dùng xác nhận
2026-08-30) — không có dữ liệu để mất, và không phải đối chiếu lại bảng
`__EFMigrationsHistory` của DB đang chạy. Cùng thao tác trên một DB có dữ liệu
thật thì phức tạp và rủi ro hơn hẳn; nếu tình huống đó xuất hiện sau này, **đừng**
chép quyết định này sang.

Năm bảng `business` biến khỏi `db-khoi-tao.sql`. Khi module DTI dựng lại, bảng của nó vào
**migration của host**, không phải một bộ migration riêng nằm trong module — bản viết ngày
2026-08-31 nói *"nó mang migration riêng của nó"*, và chốt 2026-09-04 (§5.3) sửa đúng chỗ đó:
một dự án chỉ có **một** `ModelSnapshot`, đặt ở host. Ranh giới Core ↔ Business ở tầng **code**
thì không đổi — [`kien-truc-core-module.md`](kien-truc-core-module.md).
Phương án thay thế (đưa 5 entity chết trở lại `Core` chỉ để EF biết tới chúng) đã
bị loại vì nó phá đúng ranh giới đó.

#### Có thật hôm nay → sẽ thành

| | Có thật hôm nay (đối chiếu 2026-08-30) | Sẽ thành |
|---|---|---|
| Số migration | ~~6~~ | ✅ **1** — `20260831165117_InitialCreate` |
| Snapshot vs model | Lệch 5 bảng | Khớp |
| `migrations add` kế tiếp | Sinh `DropTable` ×5 | Chỉ sinh thứ vừa đổi |
| `db-khoi-tao.sql` | ~~16 bảng~~ | ✅ **11 bảng** — sinh lại 2026-08-31 từ `Migrations/sql/0001_initial_baseline.sql` |
| `cau-truc-database.sql` | Chứa DDL viết tay cho `CriteriaAssessments` | ✅ Đã xét: **toàn bộ file là DTI**, nay mang banner `TÀI LIỆU LỊCH SỬ` và giữ lại để module dùng lại khi quay về |
| `SysMenuRole` / `RolePermission` | Không có trường vết nào | Kế thừa `BaseEntity` (5 cột audit), index unique thêm `WHERE "IsDeleted" = false` — chốt 2026-08-31 |
| `AspNetUsers` | Chỉ `DateCreate`/`DateUpdate` | Thêm `CreatedBy`/`UpdatedBy` — chốt 2026-08-31 |
| §5 ở trên | Bảo chạy `dotnet ef database update` (bị chặn) | Một đường duy nhất: `db-khoi-tao.sql` |

#### Nghiệm thu

| # | Phép thử | PASS | Kết quả 2026-08-31 |
|---|---|---|---|
| 1 | `dotnet ef migrations add ThuNghiem` trên model **không đổi gì** | Migration sinh ra **rỗng** | ✅ 0 lời gọi `migrationBuilder.` — đã xoá migration thử sau khi xem |
| 2 | `grep -c CriteriaAssessment` trên snapshot mới | `0` | ✅ `0` (trước baseline là 8) |
| 3 | Chạy `db-khoi-tao.sql` mới trên DB trống rồi khởi động app | App lên, không lỗi schema |
| 4 | `dotnet test` | Vẫn xanh toàn bộ — `PostgresFixture` dựng schema từ migration |

Ba thay đổi schema chốt ngày 2026-08-31 (hai dòng cuối bảng trên) **phải vào cùng lượt
baseline này**, không phải một migration riêng sau đó — đó là toàn bộ lý do chúng rẻ.
Lý do và đánh đổi: [`huong_dan/quy-uoc/be-entity-domain.md`](huong_dan/quy-uoc/be-entity-domain.md)
§"Quyết định người dùng 2026-08-31".

Phép thử 1 là phép thử quyết định: nó chứng minh snapshot đã khớp model. Nhớ xoá
migration thử nghiệm sau khi xem.

### 5.3. Migration thuộc host, Core ship `.sql` — chốt người dùng 2026-09-04

> **Trạng thái, tách theo từng phần** (đối chiếu 2026-09-04):
> phần **chỗ đặt file + cấu hình** đã ✅ có thật trong code, đo ở "Nghiệm thu" cuối mục;
> phần **thao tác cho dự án 2** là 📐 hướng dẫn cho việc chưa xảy ra (chưa có dự án 2);
> một mục 🚧 còn treo, liệt ở cuối.

#### Vấn đề — `ModelSnapshot` là một file trạng thái DÙNG CHUNG mà EF bắt buộc phải chính xác

EF **không đọc database** để biết cần sinh gì. `dotnet ef migrations add` so **model hiện
tại** (dựng từ entity + configuration) với **`PlatformManagerDbContextModelSnapshot.cs`** —
một file C# ghi lại *"EF tin rằng schema đang là thế này"*. Nó không phải cache sinh lại
được từ database: nó **là** trạng thái duy nhất EF dùng để tính delta, và mọi lần
`migrations add` đều **ghi đè toàn bộ** file đó bằng model mới (EF ghi lại cả model, không
ghi thêm phần).

Trước ngày 2026-09-04 file đó nằm trong `Core.Infrastructure`, tức **Core sở hữu nó**.

Ngày dự án 2 thêm bảng nghiệp vụ **đầu tiên**, nó chạy `migrations add`, và EF ghi đè đúng
file đó để thêm bảng của dự án 2 vào. Từ giây phút ấy **Core và dự án 2 cùng sở hữu một
file trạng thái** — và lần Core ship bản vá là lần hỏng. Có đúng hai đường đi, cả hai đều mất:

| Khi merge bản vá Core, ai thắng | Chuyện gì xảy ra sau đó |
| --- | --- |
| Giữ snapshot **của Core** (không có bảng dự án 2) | Lần `migrations add` kế tiếp ở dự án 2 so model (**có** bảng) với snapshot (**không** có) ⇒ EF kết luận bảng chưa tồn tại ⇒ sinh **`CREATE TABLE`** cho bảng **đang có dữ liệu thật** |
| Giữ snapshot **của dự án 2** (không có thay đổi Core) | Thay đổi schema của Core biến mất khỏi trạng thái ⇒ lần sinh sau EF sinh **lại** thứ đã áp, hoặc âm thầm bỏ qua thứ Core vừa đổi |

Vì sao phải viết dài như vậy ở đây: **triệu chứng không gợi ra nguyên nhân.** Người gặp nó
thấy một file migration sinh tự động chứa `CREATE TABLE` cho bảng mình biết chắc là đã có —
và phản xạ đầu tiên là nghi database, nghi connection string, nghi `__EFMigrationsHistory`,
chứ không nghi một file `.cs` mình chưa bao giờ mở. Nó **không gây lỗi biên dịch, không test
nào bắt được**, và file sinh ra trông hoàn toàn bình thường.

Repo này đã trúng đúng khuôn đó **một lần rồi**, chỉ khác nguyên nhân: §5.2 *"Cái mìn"* —
snapshot thừa 5 bảng của module đã gỡ ⇒ migration kế tiếp chứa 5 lệnh `DropTable`. Lần đó
nguyên nhân là gỡ module; lần này nguyên nhân là **hai chủ sở hữu**. Cùng một cơ chế:
snapshot lệch khỏi thực tế, EF tin snapshot.

#### Chốt — phương án A

| # | Quyết định |
| --- | --- |
| 1 | `Migrations/*.cs` **và** `ModelSnapshot` thuộc **project host của từng dự án**, mỗi dự án một bộ **riêng**, không dùng chung |
| 2 | **Core ship `.sql`**, không ship migration. Baseline `.sql` ở lại Core — nó là artifact schema Corebase giao cho dự án sau |
| 3 | Giữ **MỘT** `DbContext` (`PlatformManagerDbContext`, sống ở Core) — không tách đôi |

**Vì sao giữ một DbContext, dù hai DbContext cũng cho mỗi bên một snapshot riêng "miễn
phí".** Hai context trả giá bằng **khoá ngoại xuyên tầng**: FK từ bảng nghiệp vụ sang bảng
`core` (kiểu `CriteriaAssessment.OwnerId → AppUser.Id`) không khai được qua ranh giới hai
context — EF chỉ sinh FK trong phạm vi một model. Mất nó thì ràng buộc *"người sở hữu bản
ghi phải là user có thật"* rơi xuống tầng code, tức rơi xuống **không ai cưỡng chế**. Đó là
lý do người dùng chọn A. Ranh giới DbContext: [`kien-truc-core-module.md`](kien-truc-core-module.md)
§"DbContext".

**Vì sao phương án này hợp với cách repo đang vận hành, không phải một tầng phức tạp thêm:**
`dotnet ef database update` **bị chặn bằng máy** (`permissions.deny` của
[`.claude/settings.json`](../.claude/settings.json)). Schema áp bằng file `.sql` chạy tay.
Nghĩa là ở đây migration **chưa bao giờ** là đường áp schema — nó chỉ là **cỗ máy tính
delta để sinh ra file `.sql` đó**. Chuyển cỗ máy đó sang host không đổi thứ gì người vận
hành đang làm.

Hệ quả trực tiếp, cần nhớ vì nó xoá một loạt hiểu lầm: **`__EFMigrationsHistory` không phải
nguồn sự thật ở repo này.** Không lệnh nào của EF ghi vào nó (không ai chạy `database
update`), nên đừng đọc bảng đó để trả lời *"schema đã tới đâu"* — câu trả lời nằm ở việc
file `.sql` nào đã được chạy tay.

#### `MigrationsAssembly` khai đúng MỘT chỗ, và đó là chỗ design-time

`PlatformManagerDbContext` sống ở `Core.Infrastructure`, mà mặc định EF tìm migration trong
**assembly chứa DbContext**. Chuyển thư mục xong mà không khai `MigrationsAssembly` thì
`dotnet ef` không thấy migration nào. Phản xạ tự nhiên là khai ở **cả hai** chỗ gọi
`UseNpgsql` cho "khớp nhau" — đó là cái bẫy, và chốt 2026-09-04 quyết định ngược lại:

| Chỗ gọi `UseNpgsql` | Khai `MigrationsAssembly`? |
| --- | --- |
| Design-time factory, `src/BE/PlatformManager.Api/PlatformManagerDbContextFactory.cs:119` | **CÓ** — nơi duy nhất `dotnet ef` thật sự đọc options |
| Runtime, `src/BE/Core/PlatformManager.Core.Infrastructure/DependencyInjection.cs:63` | **KHÔNG**, cố ý — có khối chú thích tại chỗ giải thích |

Lý do không khai ở Core mạnh hơn "cho gọn": khai ở đó buộc `Core.Infrastructure` **gọi tên
assembly của host** — bằng chuỗi `"PlatformManager.Api"`, hoặc bằng một tham số mới xuyên
qua `AddCoreModule`. Cả hai đều là thứ **Corebase mang theo sang dự án thứ hai rồi trỏ sai**,
và ArchTest hôm nay không bắt được (rule cấm Core biết tên tầng nghiệp vụ chỉ cấm
`"business"`/`"dtiweekly"`, không cấm tên host).

Bỏ trống ở runtime là **đủ**, không phải là nợ: runtime không chạy migration — không chỗ
sản phẩm nào gọi `Database.Migrate()`/`GetPendingMigrations()`, `database update` bị chặn,
schema áp bằng `.sql`. Nếu sau này thật sự cần chạy migration lúc chạy thì **đừng gõ tên
host vào Core** — thêm seam để host tự nộp assembly, đúng khuôn `EfConfigurationAssembly`
đã có sẵn.

#### Hệ quả

1. **Core muốn đổi schema của chính nó thì ship `.sql` mới** (delta), **không ship
   migration**. Dự án tiêu thụ chạy file `.sql` đó tay, y hệt cách baseline đang được áp.
2. **Dự án 2 không bao giờ nhận file `Migrations/*.cs` nào từ Core** — thấy một file như vậy
   đi kèm bản phát hành Core thì đó là lỗi đóng gói, không phải thứ cần merge.
3. **Thư mục `sql/` ở lại Core, `.cs` thì không.** Hai loại file từng nằm cạnh nhau trong
   cùng một thư mục `Migrations/`, nay tách vai: `.sql` là **artifact ship đi**, `.cs` là
   **trạng thái riêng của một dự án**.
4. **Đường `.sql` có kiểm chứng tự động, không phải niềm tin.** `PostgresFixture` dựng schema
   cho integration test bằng chính các file `.sql` trong repo, không bằng `EnsureCreated()` —
   xem [`huong_dan/wiki-core/be/04-testing-strategy.md`](huong_dan/wiki-core/be/04-testing-strategy.md)
   §"Schema cho integration test". Nên `.sql` sai là test đỏ, không phải phát hiện lúc deploy.

#### 📐 Dự án 2 thêm bảng đầu tiên — làm theo đúng thứ tự này

Chưa có dự án 2 nên mục này là hướng dẫn cho việc **chưa xảy ra**. Bẫy nằm ngay ở bước đầu,
nên đọc hết trước khi gõ lệnh nào: host của dự án 2 bắt đầu với `Migrations/` **rỗng**, tức
snapshot rỗng. Chạy thẳng `migrations add` cho bảng mới ⇒ EF so model (bảng `core` + bảng
mới) với **không gì cả** ⇒ sinh `CREATE TABLE` cho **toàn bộ**, kể cả những bảng `core` đã
tồn tại trong database. Bước 3 dưới đây tồn tại để chặn đúng điều đó.

1. **Áp schema Core lên database** bằng file `.sql` Core ship (baseline, xem §5.2), chạy tay.
   Chưa động gì tới EF.
2. **Dựng design-time factory riêng trong host của dự án 2**, khai `MigrationsAssembly` trỏ
   về chính assembly host đó (khuôn: `PlatformManagerDbContextFactory` ở repo này). Đây là
   chỗ duy nhất khai, xem mục trên.
3. **Sinh migration mốc cho riêng Core, khi model CHƯA có entity nghiệp vụ nào.**

   ```bash
   dotnet ef migrations add CoreBaseline --project <host> --startup-project <host>
   ```

   Migration này **không bao giờ được chạy lên database** — database đã có schema đó từ bước
   1. Nó tồn tại **chỉ để** đặt snapshot vào đúng trạng thái *"các bảng `core` đã tồn tại"*.
4. **Đối chiếu mốc vừa sinh với `.sql` của Core trước khi đi tiếp.** Sai lệch ở bước này
   ngấm vào mọi delta về sau. Phép thử rẻ nhất: chạy lại `migrations add ThuNghiem` trên
   model **không đổi gì** — migration sinh ra phải **rỗng** (không lời gọi `migrationBuilder.`
   nào), rồi xoá migration thử. Đây đúng là phép thử đã dùng ở §5.2.
5. **Thêm entity nghiệp vụ + EF configuration** (khai `schema: "business"`, xem §6), rồi đăng
   ký assembly của tầng nghiệp vụ cho cả runtime lẫn design-time factory — cơ chế
   `EfConfigurationAssembly`, xem [`kien-truc-core-module.md`](kien-truc-core-module.md)
   §"DbContext".
6. **Sinh migration của bảng mới:**

   ```bash
   dotnet ef migrations add ThemBangX --project <host> --startup-project <host>
   ```

   **ĐỌC file sinh ra.** Nó phải chứa đúng bảng mới. Có bất kỳ `DropTable`/`CreateTable` nào
   cho bảng `core` ⇒ dừng, bước 3 hoặc 4 sai — đừng "sửa cho chạy".
7. **Sinh `.sql` DELTA để chạy tay** (không bao giờ sinh full — full sẽ dựng lại cả bảng đã có):

   ```bash
   dotnet ef migrations script CoreBaseline ThemBangX --idempotent --output <file>.sql
   ```

8. **Chạy file `.sql` đó lên database**, và thêm tên nó vào danh sách script của
   `PostgresFixture` để integration test dựng đúng schema thật (hệ quả #4).

**Khi Core ship bản vá schema về sau:** cập nhật Core (entity đổi theo), chạy `.sql` của Core
lên database, rồi `migrations add DongBoCore<phiên bản>` ở host để snapshot đuổi kịp — và
**không chạy** migration đó lên database, vì `.sql` đã áp rồi. Đúng vai của migration ở repo
này: tính delta, không áp schema.

#### Nghiệm thu — ✅ đã đo 2026-09-04

| # | Phép thử | PASS | Kết quả 2026-09-04 |
| --- | --- | --- | --- |
| 1 | `ls src/BE/Core/PlatformManager.Core.Infrastructure/Persistence/Migrations/` | Không còn file `.cs` nào, chỉ còn thư mục `sql/` | ✅ đúng vậy |
| 2 | `ls src/BE/PlatformManager.Api/Persistence/Migrations/` | Có migration `.cs` + `ModelSnapshot` | ✅ đúng vậy |
| 3 | `grep -rn "MigrationsAssembly" src/BE --include=*.cs` (bỏ `obj/`, bỏ chú thích và test) | Đúng **một** lời gọi thật, ở design-time factory | ✅ `PlatformManagerDbContextFactory.cs:119` |
| 4 | `grep -rn "Database.Migrate\|GetPendingMigrations" src/BE --include=*.cs` (bỏ `obj/`) | Không lời gọi nào ở **code sản phẩm** (khớp lý do bỏ trống `MigrationsAssembly` ở runtime) | ✅ chỉ còn trong chú thích và trong fixture của `MigrationsLocationTests` |
| 5 | `dotnet test` trên `PlatformManager.ArchTests` — có `MigrationsLocationTests` khoá chỗ đặt migration lẫn neo `MigrationsAssembly` | Xanh toàn bộ | ✅ 57/57 xanh |
| 6 | `dotnet test` trên `PlatformManager.Core.IntegrationTests` — `PostgresFixture` dựng schema từ `.sql` | Xanh | ⏸ **chưa đo** — Docker tắt trên máy đo (`docker version` không kết nối được). Không phải kết quả đỏ, là chưa chạy |
| 7 | `dotnet ef migrations add ThuNghiemViTriSnapshot --project PlatformManager.Api --startup-project PlatformManager.Api --output-dir Persistence/Migrations` trên model không đổi ⇒ migration rỗng | Migration rỗng | ✅ **0** lời gọi `migrationBuilder.` trong `Up`/`Down`. Hai file sinh ra đã xoá tay ngay sau khi đọc (`dotnet ef migrations remove` bị `settings.json` chặn; không cần nó vì migration rỗng ⇒ snapshot không bị đổi). Xác nhận lại sau khi dọn bằng `dotnet ef migrations has-pending-model-changes` → *"No changes have been made to the model since the last migration."* |

Phép thử 7 là phép thử quyết định cho *tính đúng của snapshot*; 1–3 chỉ chứng minh file đã
nằm đúng chỗ. Xanh ở 1–5 mà đỏ ở 7 nghĩa là đã chuyển file nhưng trạng thái sai — đúng tình
huống mục này sinh ra để ngăn.

`has-pending-model-changes` là cách rẻ hơn để lặp lại phép thử 7 về sau: nó trả lời đúng câu
hỏi *"model có khớp snapshot không"* mà **không sinh file nào**, nên chạy được bất cứ lúc nào
không sợ để lại rác trong `src/`.

#### 🚧 Còn treo — khối cảnh báo "ĐÓNG BĂNG" trong source

`src/BE/PlatformManager.Api/PlatformManagerDbContextFactory.cs` mở đầu bằng khối
*"🛑 DỪNG — KHÔNG CHẠY `dotnet ef migrations add`"* viết ngày 2026-08-29, nói `migrations add`
sẽ sinh `DropTable` cho 5 bảng `business`. **Tiền đề đó không còn đúng** sau đợt baseline
2026-08-31 (§5.2) — snapshot hiện hành không khai bảng `business` nào. Khối vẫn để nguyên,
có gắn cảnh báo ngược ở đầu, vì **đóng băng là quyết định của người dùng** và chỉ người dùng
gỡ được.

Cần người dùng chốt: gỡ đóng băng hay giữ. Giữ nguyên một cảnh báo sai không rẻ — cảnh báo
sai là cảnh báo sẽ bị bỏ qua, kể cả khi nó đúng trở lại.


## 6. Thêm bảng mới sau này — vào schema nào

Tự hỏi đúng câu đã dùng để quyết định `Core.*` hay `Business.*` ở tầng code
(xem `doc/kien-truc-core-module.md`): bảng đó có ý nghĩa với **mọi** sản
phẩm dựng trên nền tảng này (→ `core`), hay chỉ riêng nghiệp vụ hiện tại
(→ `business`)? EF Configuration của entity mới khai `schema: "core"` hoặc
`schema: "business"` tương ứng — không tạo schema thứ 3 trừ khi thật sự có
domain nghiệp vụ độc lập khác xuất hiện (xem `doc/kien-truc-core-module.md`
§ Khi nào tách thành module độc lập thật).

Lưu ý: schema `hangfire` (§1) **không** là ngoại lệ của luật này — nó không do
EF Configuration khai, không do ai "tạo schema thứ 3", mà do thư viện Hangfire
tự dựng lúc runtime.

# Cấu trúc Database — PlatformManager (PostgreSQL)

> Tài liệu mô tả cấu trúc DB thật (không phải ERD dự kiến). Đối chiếu
> `doc/kien-truc-core-module.md` để hiểu lý do tách Core ↔ Business ở tầng
> code — tài liệu này mô tả cách ranh giới đó phản ánh xuống DB.

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

> Nguồn sự thật cho mục này: `src/BE/Core/PlatformManager.Core.Infrastructure/
> Persistence/Migrations/PlatformManagerDbContextModelSnapshot.cs` (model EF
> Core hiện hành), **không phải** file `.sql` (file `.sql` là delta từng
> migration, không phản ánh trạng thái tổng).

### Schema `core`

| Bảng | Mô tả |
|---|---|
| `AspNetUsers` | Tài khoản đăng nhập (ASP.NET Core Identity), mở rộng thêm `FullName`/`DateCreate`/`DateUpdate`/`MustChangePassword` |
| `AspNetRoles` | Vai trò: `SuperAdmin`, `Admin`, `User` |
| `AspNetUserRoles` | Gán role cho user (nhiều-nhiều) |
| `AspNetUserClaims` | Claim cấp user (Identity sinh sẵn, chưa dùng) |
| `AspNetRoleClaims` | Claim cấp role (Identity sinh sẵn, chưa dùng) |
| `AspNetUserLogins` | Đăng nhập ngoài (Google/Microsoft...), chưa dùng |
| `AspNetUserTokens` | Token nội bộ Identity (reset password, 2FA) |
| `RolePermissions` | ✅ **Phân quyền theo hành động** (đã thi công 2026-08-24 — xem §2.1) — role nào được phép chạm `ResourceKey` nào. PK ghép (`RoleId`, `ResourceKey`), `ResourceKey` `varchar(100)`; FK `RoleId → AspNetRoles.Id` `ON DELETE CASCADE`. Index phụ `IX_RolePermissions_ResourceKey_RoleId` (`ResourceKey`, `RoleId`) — xem §4 |
| `SysMenus` | Menu điều hướng động (sidebar), tự tham chiếu `ParentId` cho cây 1 cấp |
| `SysMenuRoles` | Role nào được thấy menu nào (nhiều-nhiều) |
| `__EFMigrationsHistory` | Bảng nội bộ EF Core theo dõi migration đã áp dụng |

> **⚠️ `RolePermissions` rỗng = mọi role (trừ `SuperAdmin`) bị 403.**
> `RequirePermissionFilter` là deny-by-default. **✅ Đóng 2026-08-24:**
> `CoreSeeder.SeedRolePermissionsAsync()` (gọi ngay sau `SeedRolesAsync()`
> trong `SeedAsync()`) nay seed đủ 3 `ResourceKeys.All` cho `Admin`/`User` ở
> Development. `[RequirePermission]` cũng đã gắn lên cả 3 controller
> (`CriteriaController`/`CriteriaGroupsController`/`ImportController`) CÙNG
> lúc với đợt seed này — đúng thứ tự "expand trước, contract sau" ở §2.1
> `doc/huong_dan/wiki-core/be/13-core-data-migration.md`. Production (không đi
> qua `CoreSeeder`, chỉ chạy `IsDevelopment()`) seed bằng
> `scripts/seed-role-permissions.sql` (idempotent, `ON CONFLICT DO NOTHING`) —
> **bắt buộc chạy** trên mọi DB thật trước hoặc cùng lúc với việc áp migration
> `0004_role_permission_import_job.sql`, nếu không mọi role trừ `SuperAdmin`
> vẫn bị 403 hàng loạt trên chính DB đó. Xem `doc/contracts/permissions.md`
> §"Rủi ro rollout".
>
> *(Lịch sử: trước 2026-08-24, `CoreSeeder` không có method seed bảng này —
> bảng rỗng ở MỌI môi trường kể cả Development. Xem
> `doc/huong_dan/wiki-core/be/13-core-data-migration.md` §"Áp dụng vào
> PlatformManager" để biết đầy đủ bối cảnh khoảng trống đó.)*

### Schema `business`

| Bảng | Mô tả |
|---|---|
| `CriteriaGroups` | Nhóm chỉ tiêu đánh giá DTI Weekly |
| `Criteria` | Chỉ tiêu đánh giá cụ thể, thuộc 1 `CriteriaGroups` |
| `CriteriaAssessments` | Kết quả đánh giá 1 `Criteria` theo từng kỳ (phần ngày của `DateCreate` = kỳ). ✅ Optimistic concurrency (2026-08-24, migration `0006_criteria_assessment_row_version.sql`) — property CLR `Version` (`uint`) bind thẳng vào cột hệ thống `xmin` có sẵn của Postgres, KHÔNG tạo cột thật nào (xem `doc/huong_dan/quy-uoc/be-entity-domain.md` §RowVersion) |
| `CriteriaEvidences` | Minh chứng đính kèm 1 `CriteriaAssessments` (nhiều dòng/bản ghi) |
| `ImportJobs` | ✅ **Trạng thái job import CSV/Excel chạy nền qua Hangfire** (đã thi công 2026-08-24 — xem §2.1). Cột: `Id` (uuid, PK), `FileName` `varchar(260)` NOT NULL, `Format` `varchar(20)` NOT NULL, `StoragePath` `varchar(1000)` NOT NULL, `Status` `varchar(20)` NOT NULL, `ResultJson` `text`, `ErrorMessage` `text` + 5 field còn lại của `BaseEntity` (`Id` đã liệt kê ở
đầu, `BaseEntity` có 6 field tổng cộng). Index `IX_ImportJobs_Status` phục vụ endpoint poll `GET /api/import/{jobId}`. **Không có FK nào** — job độc lập với dữ liệu nó ghi ra |

> `ImportJobs` là bảng *trạng thái tiến trình*, khác 4 bảng còn lại (dữ liệu
> nghiệp vụ). Nó lưu **đường dẫn** file tạm (`StoragePath`), không lưu nội
> dung file — file upload không sống sót qua ranh giới request→job nền, xem
> `doc/huong_dan/quy-uoc/be-cqrs-handler.md` § "Command chạy lâu → job nền".

## 2.1. ✅ `RolePermissions`/`ImportJobs` — ĐÃ CHỐT, đã thi công (2026-08-24)

**Trạng thái (xác minh 2026-08-24, sau đợt nối dây Hangfire + permission-by-action
+ seed) — khác 2 bản trước đó (2026-08-23, 2026-08-24 sáng), không còn dòng
"chưa có" nào ở bảng dưới:**

| Mã | Kiểm 2026-08-23 (lạc hậu) | Kiểm 2026-08-24 |
| --- | --- | --- |
| Entity/configuration `RolePermission`, `ImportJob` | chưa có | **đã có** — `RolePermission.cs`, `RolePermissionConfiguration.cs`, `ImportJob.cs` |
| `RequirePermissionFilter`, `ResourceKeys` | chưa có | **đã có**, deny-by-default, ĐÃ GẮN lên `CriteriaController`/`CriteriaGroupsController`/`ImportController` (xem `doc/huong_dan/quy-uoc/be-api-controller.md`) |
| `AddPermissionInfrastructure()` + `options.Filters.Add<RequirePermissionFilter>()` ở `Program.cs` | chưa có | **đã có** — filter thật sự nằm trong pipeline MVC (kiểm bằng seam activation test `RequirePermissionSeamTests`, không chỉ unit test) |
| `CoreSeeder.SeedRolePermissionsAsync()` | chưa có | **đã có** — seed đủ 3 `ResourceKeys.All` cho `Admin`/`User` ở Development; production seed bằng `scripts/seed-role-permissions.sql` |
| `Hangfire` (`AddHangfire`/`AddHangfireServer`/`UseHangfireDashboard`) ở `Program.cs` | chưa có | **đã có** — package `Hangfire.Core`/`Hangfire.AspNetCore`/`Hangfire.PostgreSql`, storage dùng chung connection string `Default`, tự tạo schema `hangfire` lúc khởi động lần đầu. Kiểm bằng seam activation test `ImportBackgroundJobSeamTests` (poll `Pending` → `Succeeded` qua HTTP thật) |
| `ImportController` gọi `StartImportCommand` (đường job nền) | chưa có (còn gọi `ImportCsvCommand` đồng bộ cũ) | **đã có** — `POST /api/import` (đổi từ `/api/import/csv`), DI đủ `IImportJobRunner`/`IImportFileStorage`/`IImportJobRepository`/`IImportFileReader` (2 impl: CSV ở Application, Excel ở Infrastructure) |
| `ModelSnapshot` có 2 bảng này | chưa có | **đã có** — vá bằng cách sinh 1 migration tạm (`dotnet ef migrations add`) để buộc EF tự regenerate `PlatformManagerDbContextModelSnapshot.cs` đúng, rồi xoá migration tạm, giữ lại snapshot đã đúng. Xác nhận không còn model drift bằng `dotnet ef migrations add` thử lần nữa ra migration RỖNG |

**Định nghĩa đầy đủ để thi công** — đây là bản duy nhất còn lại sau khi gộp
`doc/ERD/` (đã xoá), nên ghi đủ cả tên constraint:

`core."RolePermissions"` — `RoleId` `uuid` NOT NULL · `ResourceKey`
`varchar(100)` NOT NULL · PK ghép `PK_RolePermissions` (`RoleId`, `ResourceKey`)
· FK `FK_RolePermissions_AspNetRoles_RoleId` → `core."AspNetRoles"("Id")`
**ON DELETE CASCADE** (cùng quy ước `SysMenuRoles`: xoá role thì gỡ luôn quyền)
· **không** có cột `BaseEntity` · index phụ `IX_RolePermissions_ResourceKey_RoleId`.

`business."ImportJobs"` — xem §2 cho danh sách cột · PK `PK_ImportJobs` (`Id`) ·
**không FK nào** · index `IX_ImportJobs_Status` · **có** 6 cột `BaseEntity`.

### Vì sao `IX_RolePermissions_ResourceKey_RoleId` là bắt buộc, không phải tối ưu sớm

PK ghép dẫn đầu bằng `RoleId`. Nhưng truy vấn nóng nhất hệ thống —
`RequirePermissionFilter`, chạy trên **mọi** request có `[RequirePermission]` —
lọc **trước hết theo `ResourceKey`** rồi mới ghép `RoleId`. PK **không seek
được** cho nó (quy tắc Q2 ở `doc/huong_dan/quy-uoc/be-performance.md`). Index này
dẫn đầu đúng cột được lọc và phủ luôn cột join → **index-only scan**.

> **Đọc `EXPLAIN` cho đúng ở bảng nhỏ.** Với ~6 dòng `RolePermissions`, Postgres
> vẫn chọn **Seq Scan** vì cả bảng nằm gọn trong một page — đó là **lựa chọn
> ĐÚNG của planner, không phải dấu hiệu index vô dụng**. Index có giá trị khi số
> tổ hợp (role × resource key) tăng; chi phí duy trì gần bằng 0 vì bảng ghi rất
> hiếm. Đừng gỡ index chỉ vì `EXPLAIN` trên dữ liệu seed không dùng tới nó.

*(Tri thức trong mục này trước nằm ở header `doc/ERD/migrations/0005_*.sql`, file
đã xoá khi gộp nguồn schema — xem §5.)*

## 3. Quan hệ xuyên schema

Chỉ 1 khoá ngoại đi từ `business` sang `core` (đúng chiều — nghiệp vụ được
phép biết về Core, Core không được biết về nghiệp vụ):

```
business.CriteriaAssessments.OwnerId → core.AspNetUsers.Id
```

Không có FK nào đi chiều ngược lại (`core` → `business`) — khớp đúng luật
"Core không được biết về Business" đã chốt ở tầng code.

## 4. Ràng buộc đáng chú ý

- **Soft-delete 2 lớp** — cột `IsDelete` + filtered unique index chỉ tính trên
  dòng `IsDelete = false`, nhờ đó xoá mềm một mã rồi tạo lại đúng mã đó vẫn
  thành công. Áp dụng cho `IX_Criteria_Code_Active` và
  `IX_CriteriaGroups_Code_Active`.

  > ⚠️ **`SysMenus` là NGOẠI LỆ — và đây là lỗi, không phải thiết kế.**
  > `IX_SysMenus_Code` là unique **không** filter (`ModelSnapshot` không có
  > `HasFilter`), nên hôm nay **xoá mềm một menu rồi tạo lại cùng `Code` sẽ
  > thất bại** vì trùng khoá — trong khi `Criteria`/`CriteriaGroups` thì được.
  >
  > Bản `0001` từng có `ux_sysmenu_code ... WHERE "IsDelete" = false`; mệnh đề
  > partial **mất trong lần dựng lại `0003`**, không ai ghi nhận. Phát hiện
  > 2026-08-23. Khi viết lại `src`, thêm `.HasFilter("\"IsDelete\" = false")`
  > vào cấu hình `SysMenu` — `CriteriaAssessmentConfiguration` đã có sẵn mẫu,
  > EF sinh được, không cần vá tay.
- **`UX_CriteriaAssessments_CriteriaId_DateCreate_Day`**: index duy nhất
  theo biểu thức (không phải theo cột thẳng) — ép buộc "1 `Criteria` chỉ có
  tối đa 1 `CriteriaAssessments` chưa xoá mềm mỗi ngày". Dùng hàm SQL
  `business.criteria_assessment_date_utc()` (đánh dấu `IMMUTABLE`) thay vì
  `CAST` trực tiếp — Postgres từ chối `CAST(timestamptz AS date)` thẳng
  trong index vì phụ thuộc `TimeZone` của session (lỗi `42P17`).

> ### ⚠️⚠️ BẪY NGUY HIỂM — `UX_CriteriaAssessments_CriteriaId_DateCreate_Day` KHÔNG có trong EF model
>
> Unique index này **chỉ tồn tại trong SQL vá tay**, EF Core **không biết nó**:
>
> `PlatformManagerDbContextModelSnapshot.cs` **chỉ có** `IX_CriteriaAssessments_CriteriaId_DateCreate`
> — index **non-unique**, không filter, không hàm. Không có dấu vết nào của
> `UX_*` hay của hàm SQL.
>
> **Nguyên văn 2 đoạn phải dựng lại bằng tay** (trước nằm trong
> `doc/ERD/migrations/0003_corebase_v2.sql`, file đã xoá khi gộp nguồn schema —
> đây là bản duy nhất còn lại, **đừng xoá khối này**):
>
> ```sql
> CREATE OR REPLACE FUNCTION business.criteria_assessment_date_utc(ts timestamptz)
> RETURNS date
> LANGUAGE sql
> IMMUTABLE
> AS $$
>     SELECT (ts AT TIME ZONE 'UTC')::date;
> $$;
>
> CREATE UNIQUE INDEX IF NOT EXISTS "UX_CriteriaAssessments_CriteriaId_DateCreate_Day"
>     ON business."CriteriaAssessments" ("CriteriaId", business.criteria_assessment_date_utc("DateCreate"))
>     WHERE "IsDelete" = false;
> ```
>
> Cả hai nằm **ngoài** block `DO $EF$` — tức EF không sinh và không xoá chúng.
> Hàm **bắt buộc** đánh dấu `IMMUTABLE`: viết thẳng `CAST("DateCreate" AS date)`
> trong biểu thức index sẽ bị Postgres từ chối với lỗi **`42P17`**
> *"functions in index expression must be marked IMMUTABLE"* — vì phép đổi
> `timestamptz`→`date` phụ thuộc `TimeZone` của session. **Lỗi này đã xảy ra
> thật, không phải giả định.**
>
> **Hệ quả:** chạy `dotnet ef migrations script` sinh lại **full script từ
> đầu** sẽ dựng DB **THIẾU** unique index này và **THIẾU** hàm
> `criteria_assessment_date_utc` → mất luôn ràng buộc "1 đánh giá/chỉ tiêu/ngày",
> dữ liệu trùng lọt vào im lặng. Không có test/compile error nào báo.
>
> **Quy ước bắt buộc — từ `0004` trở đi luôn sinh script DELTA**, không bao
> giờ sinh full:
> ```
> dotnet ef migrations script <MigrationTrước> <MigrationMới> --idempotent
> ```
> Nếu **buộc phải** sinh full script (dựng DB mới từ số 0), phải **chèn tay
> lại** 2 đoạn SQL nguyên văn ở trên sau khi sinh xong, rồi kiểm
> `\di business.*` thấy `UX_CriteriaAssessments_CriteriaId_DateCreate_Day`
> mới coi là xong.
>
> (Cảnh báo này trước đây chỉ nằm trong header `0004_*.sql` — nay đưa lên đây
> vì nó ảnh hưởng mọi lần đụng tới migration, không riêng lần đọc `0004`.)

- **Hầu hết `Id` do ứng dụng tự sinh, KHÔNG để DB tự sinh** — mọi `Id` kiểu
  `uuid` (`Criteria`, `CriteriaGroups`, `CriteriaAssessments`,
  `CriteriaEvidences`, `ImportJobs`, `SysMenus`, `AspNetUsers`,
  `AspNetRoles`) đều **không** có `DEFAULT gen_random_uuid()`; ứng dụng tự
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
`IsDelete`. Hai cột `DateCreate`/`DateUpdate` **cố ý đặt trùng quy ước
`BaseEntity` dù không kế thừa** — để đọc hiểu nhất quán, không phải để chia sẻ
code. Đừng "sửa cho đồng bộ" bằng cách bắt nó kế thừa.

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

**Nguồn schema từ 2026-08-23 chỉ còn hai file, cùng tên khác đuôi:**

| File | Vai |
| --- | --- |
| [`cau-truc-database.md`](cau-truc-database.md) *(chính file này)* | Mô tả để **đọc hiểu** — bảng, cột, ràng buộc, quyết định thiết kế |
| [`cau-truc-database.sql`](cau-truc-database.sql) | **DDL viết tay** — phần EF Core không sinh được |

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
> `UX_CriteriaAssessments_CriteriaId_DateCreate_Day`.

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
> hơn ở `spec/danh-muc-dti/` và `spec/dashboard-dti-weekly/`.

# Cấu trúc Database — PlatformManager (PostgreSQL)

### 5.1. ⚠️ SEED BẮT BUỘC sau khi chạy `0004` trên DB thật

`RequirePermissionFilter` là **deny-by-default**: bảng `core."RolePermissions"`
rỗng nghĩa là **MỌI role trừ `SuperAdmin`** (có bypass tường minh) đều bị
**403** ở mọi endpoint gắn `[RequirePermission]` (Criteria / CriteriaGroups /
Import) — kể cả thao tác họ vẫn làm được trước khi chạy `0004`.

`CoreSeeder.SeedRolePermissionsAsync()` cấp đủ key cho `Admin` + `User` (giữ
nguyên hành vi cũ), **nhưng seeder chỉ chạy khi `IsDevelopment()`** (gate
trong `Program.cs`). Với DB thật/production **phải seed tay tương đương** —
xem `doc/contracts/permissions.md` § "Rủi ro rollout".

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

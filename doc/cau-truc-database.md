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
| `RolePermissions` | **Phân quyền theo hành động** — role nào được phép chạm `ResourceKey` nào. PK ghép (`RoleId`, `ResourceKey`), `ResourceKey` `varchar(100)`; FK `RoleId → AspNetRoles.Id` `ON DELETE CASCADE`. Index phụ `IX_RolePermissions_ResourceKey_RoleId` (`ResourceKey`, `RoleId`) — xem §4 |
| `SysMenus` | Menu điều hướng động (sidebar), tự tham chiếu `ParentId` cho cây 1 cấp |
| `SysMenuRoles` | Role nào được thấy menu nào (nhiều-nhiều) |
| `__EFMigrationsHistory` | Bảng nội bộ EF Core theo dõi migration đã áp dụng |

> **⚠️ `RolePermissions` rỗng = mọi role (trừ `SuperAdmin`) bị 403.**
> `RequirePermissionFilter` là deny-by-default. `CoreSeeder.SeedRolePermissionsAsync()`
> cấp đủ key cho `Admin`/`User`, nhưng seeder **chỉ chạy khi `IsDevelopment()`**
> — DB thật/production phải seed tay. Xem §5.1 và `doc/contracts/permissions.md`
> § "Rủi ro rollout".

### Schema `business`

| Bảng | Mô tả |
|---|---|
| `CriteriaGroups` | Nhóm chỉ tiêu đánh giá DTI Weekly |
| `Criteria` | Chỉ tiêu đánh giá cụ thể, thuộc 1 `CriteriaGroups` |
| `CriteriaAssessments` | Kết quả đánh giá 1 `Criteria` theo từng kỳ (phần ngày của `DateCreate` = kỳ) |
| `CriteriaEvidences` | Minh chứng đính kèm 1 `CriteriaAssessments` (nhiều dòng/bản ghi) |
| `ImportJobs` | **Trạng thái job import CSV/Excel chạy nền qua Hangfire.** Cột: `Id` (uuid, PK), `FileName` `varchar(260)` NOT NULL, `Format` `varchar(20)` NOT NULL, `StoragePath` `varchar(1000)` NOT NULL, `Status` `varchar(20)` NOT NULL, `ResultJson` `text`, `ErrorMessage` `text` + 6 field `BaseEntity`. Index `IX_ImportJobs_Status` phục vụ endpoint poll `GET /api/import/{jobId}`. **Không có FK nào** — job độc lập với dữ liệu nó ghi ra |

> `ImportJobs` là bảng *trạng thái tiến trình*, khác 4 bảng còn lại (dữ liệu
> nghiệp vụ). Nó lưu **đường dẫn** file tạm (`StoragePath`), không lưu nội
> dung file — file upload không sống sót qua ranh giới request→job nền, xem
> `src/BE/.claude/rules/cqrs-handler.md` § "Command chạy lâu → job nền".

## 3. Quan hệ xuyên schema

Chỉ 1 khoá ngoại đi từ `business` sang `core` (đúng chiều — nghiệp vụ được
phép biết về Core, Core không được biết về nghiệp vụ):

```
business.CriteriaAssessments.OwnerId → core.AspNetUsers.Id
```

Không có FK nào đi chiều ngược lại (`core` → `business`) — khớp đúng luật
"Core không được biết về Business" đã chốt ở tầng code.

## 4. Ràng buộc đáng chú ý

- **Soft-delete 2 lớp** (mọi bảng nghiệp vụ + `SysMenus`): cột `IsDelete` +
  filtered unique index (vd `IX_Criteria_Code_Active` chỉ tính trên dòng
  `IsDelete = false`) — xoá mềm 1 mã rồi tạo lại đúng mã đó vẫn thành công.
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
> | | Có gì |
> |---|---|
> | `doc/ERD/migrations/0003_corebase_v2.sql` (~dòng 347-361) | `CREATE OR REPLACE FUNCTION business.criteria_assessment_date_utc(...)` + `CREATE UNIQUE INDEX IF NOT EXISTS "UX_CriteriaAssessments_CriteriaId_DateCreate_Day" ... WHERE "IsDelete" = false` — **viết tay, ngoài block `DO $EF$`** |
> | `PlatformManagerDbContextModelSnapshot.cs` | **CHỈ CÓ** `IX_CriteriaAssessments_CriteriaId_DateCreate` — index **non-unique**, không filter, không hàm. Không có dấu vết nào của `UX_*` hay của hàm SQL |
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
> lại** 2 đoạn trên từ `0003_corebase_v2.sql` sau khi sinh xong, rồi kiểm
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

## 5. File migration tương ứng

- **Migration C#** (nguồn sự thật, EF Core sinh ra):
  `src/BE/Core/PlatformManager.Core.Infrastructure/Persistence/Migrations/`
  — **3 migration**: `20260816150234_InitialCreate`,
  `20260818090451_AddRolePermissionAndImportJob`,
  `20260818101335_AddRolePermissionResourceKeyIndex`.
- **File `.sql`** (để chạy tay lên Postgres — người dùng tự chạy, **không
  agent nào tự động chạy DDL lên DB thật**): **3 file, không phải 1**, mỗi
  file có **2 bản** phải giữ đồng bộ cùng lúc khi migration đổi:

| # | File | Nội dung | Loại |
|---|---|---|---|
| `0003` | `0003_corebase_v2.sql` | Full script: 2 schema + Identity + SysMenu/SysMenuRole + 4 bảng DTI Weekly, **+ 2 đoạn vá tay** (hàm `criteria_assessment_date_utc` + `UX_CriteriaAssessments_CriteriaId_DateCreate_Day`) | full |
| `0004` | `0004_role_permission_import_job.sql` | `core."RolePermissions"` + `business."ImportJobs"` + `IX_ImportJobs_Status`. **Có bước SEED BẮT BUỘC đi kèm — xem §5.1** | delta |
| `0005` | `0005_role_permission_resource_key_index.sql` | `IX_RolePermissions_ResourceKey_RoleId` — chỉ 1 index, an toàn chạy trên DB đã có dữ liệu | delta |

2 vị trí (nội dung SQL khớp 100%, chỉ khác 2 dòng comment đầu file trỏ ngược
về bản kia):
- `doc/ERD/migrations/<file>.sql`
- `src/BE/Core/PlatformManager.Core.Infrastructure/Persistence/Migrations/sql/<file>.sql`

> **Đọc header trước khi chạy.** Cả 2 bản đều mang đầy đủ phần header comment
> (lý do sinh delta, cảnh báo seed, cảnh báo mất index vá tay). Sửa 1 bản mà
> quên bản kia → lần sau đọc nhầm bản thiếu thông tin.
>
> `0001_corebase_identity_sysmenu.sql` và `0002_seed_corebase.sql` trong
> `doc/ERD/migrations/` **ĐÃ BỊ THAY THẾ** bởi `0003` — chỉ giữ làm tài liệu
> lịch sử, **KHÔNG chạy** (xem `doc/ke-hoach-xay-lai-corebase.md`).

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

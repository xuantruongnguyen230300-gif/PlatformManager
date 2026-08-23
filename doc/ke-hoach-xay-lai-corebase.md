# Kế hoạch: Xoá & xây lại toàn bộ src/BE + src/FE (CoreBase + DTI Weekly)

> # 🗄️ TÀI LIỆU LỊCH SỬ — ĐÃ THỰC THI XONG
>
> Kế hoạch này **đã chạy xong** (commit `99d28ba [Core]: Core Version 1.0`).
> Giữ lại để tra cứu *vì sao* một quyết định được đưa ra — **không** dùng làm mô
> tả hiện trạng, và **không** dùng làm nguồn cho code mới.
>
> Câu *"đây vẫn là báo cáo/kế hoạch để duyệt, CHƯA thực thi bất kỳ thao tác xoá
> hay viết code nào"* ở §Bối cảnh bên dưới **không còn đúng** kể từ 2026-08-16.
>
> | Trong file này → | Thực tế hiện nay |
> | --- | --- |
> | Sơ đồ 4 project phẳng `PlatformManager.{Domain,Application,Infrastructure,Api}` | `Core.*` ×3 + `Modules.DtiWeekly.*` ×3 + `PlatformManager.Api` + ArchTests — xem [`kien-truc-core-module.md`](kien-truc-core-module.md) |
> | Đánh số phase FE F0 → F1 → F3 (bỏ F2) | F0…F3 + gate — xem [`huong_dan/wiki-core/fe/trien-khai/`](huong_dan/wiki-core/fe/trien-khai/) |
> | §"File quan trọng nhất" trỏ `PlatformManager.Infrastructure/…`, `PlatformManager.Application/…` | các đường dẫn đó **không còn tồn tại** |
> | `SysMenu.RequiredRole` | đã thay bằng bảng nối `SysMenuRole` |
>
> **Nguồn sống thay thế:** kiến trúc → [`kien-truc-core-module.md`](kien-truc-core-module.md);
> quy ước thi hành → [`huong_dan/quy-uoc/`](huong_dan/quy-uoc/); hợp đồng API →
> [`contracts/`](contracts/); giao diện → [`Design/`](Design/).

## Bối cảnh

Toàn bộ tài liệu quyết định (envelope `IApiResult<T>`, `BaseEntity` mới, Clean
Architecture 4 project, ASP.NET Core Identity cookie session, PrimeNG,
`SysMenu`...) đã "CHỐT" trong `wiki-core/` và ERD-corebase, nhưng khi đối
chiếu với code thật trong `src/BE`/`src/FE`, code hiện tại vẫn là bản demo
viết **trước** các quyết định đó — envelope cũ, `BaseEntity` cũ, không có
Identity, không có PrimeNG, không có CQRS/MediatR. Người dùng chọn: **xoá
sạch cả `src/BE` và `src/FE` (kể cả nghiệp vụ DTI Weekly đã chạy được), xây
lại từ đầu đúng theo kiến trúc đã chốt, gộp luôn CoreBase (Identity/SysMenu/
phân quyền) và nghiệp vụ DTI Weekly vào cùng 1 đợt xây dựng**.

Sau vòng hỏi-đáp làm rõ thứ 2, phạm vi mở rộng thêm: 1 màn hình "Phân quyền"
mới, model quyền theo màn hình đổi từ 1 role/màn (`RequiredRole`) sang
nhiều-nhiều (Role↔Màn hình), cách áp dụng thay đổi schema DB đổi từ
auto-migrate sang chỉ sinh file script (người dùng tự chạy tay trên
Postgres), và thêm luồng bắt buộc đổi mật khẩu lần đăng nhập đầu.

Toàn bộ source hiện tại đã nằm trong lịch sử git (không có thay đổi source
chưa commit đáng kể), nên xoá bằng lệnh xoá file thường (không phải lệnh
git) là an toàn, khôi phục được qua git nếu cần.

**Lưu ý quan trọng: đây vẫn là báo cáo/kế hoạch để duyệt, CHƯA thực thi bất
kỳ thao tác xoá hay viết code nào.**

## Mô hình phân quyền — bản CHỐT sau vòng làm rõ thứ 2

- 3 role: `SuperAdmin`, `Admin`, `User`.
- Tài khoản khởi tạo (bootstrap) tên **"SuperAdmin"**, giữ **cả 2 role**
  `SuperAdmin` + `Admin`, seed qua code (`UserManager.CreateAsync` — không
  qua SQL vì không hash được password bằng SQL thuần).
- Về sau, khi cấp tài khoản quản trị cho khách hàng thật, tài khoản đó chỉ
  giữ role `Admin` (không có `SuperAdmin`) — `SuperAdmin` là vai trò dành
  riêng cho tài khoản khởi tạo/vận hành hệ thống, không cấp đại trà.
- **Mật khẩu bootstrap**: hardcode 1 giá trị tạm trong `DbSeeder.cs` (ví dụ
  `SuperAdmin@123`) + cột mới `AppUser.MustChangePassword` (bool, default
  `true` cho tài khoản này) — bắt buộc đổi mật khẩu ngay sau lần đăng nhập
  đầu tiên trước khi vào được bất kỳ màn hình nào khác. Áp dụng chung luôn
  cho MỌI user do Admin tạo qua màn "Quản trị người dùng" (tạo với mật khẩu
  tạm → `MustChangePassword=true`), không chỉ riêng tài khoản bootstrap —
  tái dùng đúng 1 cơ chế.
- **Email**: chỉ là quy ước đặt tên cho tài khoản mẫu/seed (`tên.họ@gmail.com`),
  **không** validate cứng theo cú pháp này — khi tạo user thật qua UI, chỉ
  validate đúng định dạng email chuẩn (có `@`, có domain hợp lệ).

## Phân quyền màn hình — model nhiều-nhiều (thay cho `RequiredRole` đơn)

`SysMenu.RequiredRole` (1 role/màn, đã seed sẵn trong `0002_seed_corebase.sql`)
**không đủ** vì giờ 1 màn có thể cần gắn cả `SuperAdmin` lẫn `Admin`, và cần
1 màn hình để tự gán qua UI thay vì sửa code/DB tay. Thay bằng bảng nối:

```
SysMenuRole (SysMenuId FK, RoleId FK) — PK ghép, không cần BaseEntity
  (thuần bảng nối, giống AspNetUserRoles — xoá dòng = thu hồi quyền,
  không cần soft-delete)
```

Quy ước: **không có dòng nào cho 1 `SysMenu.Id`** = màn đó mở cho mọi user
đã đăng nhập (Dashboard, Danh mục DTI). **Có ≥1 dòng** = chỉ role đó (hoặc
các role đó) mới thấy. Seed:

| Màn hình | Role được gán |
|---|---|
| Dashboard | (không gán — mở cho mọi user) |
| Danh mục > DTI | (không gán — mở cho mọi user) |
| Quản trị hệ thống (nhóm cha) | `SuperAdmin`, `Admin` |
| Quản trị hệ thống > Người dùng | `SuperAdmin`, `Admin` |
| Quản trị hệ thống > **Phân quyền** (mới) | `SuperAdmin` **only** |

**Phân quyền chỉnh sửa ma trận quyền chỉ dành riêng `SuperAdmin`** (không
cho `Admin` thường) — lý do: nếu để `Admin` tự sửa được ai thấy màn nào, một
`Admin` có thể tự cấp quyền cho `User` vào thẳng màn quản trị, hoặc tự mở
rộng quyền của chính mình — rủi ro leo thang quyền qua UI. Đây là lựa chọn
mặc định hợp lý của tôi, có thể điều chỉnh nếu bạn muốn `Admin` cũng sửa
được.

### Màn hình mới: "Phân quyền" (`/quan-tri/phan-quyen`)

Ma trận checkbox: hàng = từng mục `SysMenu`, cột = 3 role — tick/bỏ tick
để gán/thu hồi quyền thấy màn hình đó, lưu qua 1 nút "Lưu thay đổi". Không
có UI tạo/xoá `SysMenu`/`AspNetRoles` mới ở đợt này (out of scope — chỉ
quản lý **quan hệ** giữa 2 tập đã có sẵn).

## Migration DB — chỉ sinh file script, KHÔNG tự chạy

**Đổi hoàn toàn cách tiếp cận trước đó** (trước đề xuất coi SQL là "tham
chiếu", chạy `dotnet ef database update` trực tiếp — người dùng từ chối vì
"DB rất quan trọng không thể tự tiện sửa đổi"). Cách làm mới:

1. Vẫn viết entity/EF Configuration bằng C# (Domain + Infrastructure) như
   bình thường — cần cho LINQ/type-safety, không đổi.
2. Sinh migration bằng `dotnet ef migrations add <Tên>` — chỉ tạo class
   C# migration trong source, **không đụng DB thật**.
3. Xuất ra file `.sql` bằng `dotnet ef migrations script --idempotent -o
   doc/ERD/migrations/000X_<ten>.sql` — đây là **sản phẩm bàn giao**, không
   phải bước tự động.
4. **Bạn (người dùng) tự chạy file `.sql` đó trên Postgres thật** (qua
   psql/pgAdmin/công cụ bạn chọn) — không agent nào, không app nào tự chạy
   DDL lên DB thật.
5. `Program.cs` **bỏ hẳn** `db.Database.MigrateAsync()` đang gọi tự động
   mỗi lần khởi động (đây chính là điều cần sửa — code hiện tại tự migrate
   mỗi lần chạy app, đúng thứ bạn không muốn). App khởi động với giả định
   schema đã có sẵn (đã chạy script tay trước đó).
6. **Seed dữ liệu** (`DbSeeder.cs` — role, tài khoản bootstrap, `SysMenu`+
   `SysMenuRole`, CSV chỉ tiêu DTI) vẫn tự chạy khi app khởi động — đây là
   **DML** (thêm dữ liệu, idempotent theo kiểu "chưa có thì thêm"), không
   phải DDL (đổi cấu trúc bảng), rủi ro thấp hơn nhiều nên giữ tự động,
   nhưng gate rõ chỉ chạy khi `IsDevelopment()` (code hiện tại đang thiếu
   điều kiện này — 1 lỗi cần sửa luôn thể).

File `doc/ERD/migrations/0001_corebase_identity_sysmenu.sql`/
`0002_seed_corebase.sql` hiện có sẽ **được thay bằng script mới sinh từ EF**
ở bước P3 (vì model đã đổi: bỏ cột `RequiredRole`, thêm bảng `SysMenuRole`,
thêm cột `MustChangePassword`, thêm bảng nghiệp vụ DTI Weekly chưa có trong
2 file này) — 2 file cũ giữ lại làm tài liệu tham khảo lịch sử, đánh dấu rõ
"đã thay thế".

## 1. Bước xoá

```powershell
# BE — xoá logic nghiệp vụ, giữ file scaffold project
Remove-Item -Recurse -Force src/BE/PlatformManager.Api/Entities, `
  src/BE/PlatformManager.Api/Data, `
  src/BE/PlatformManager.Api/Controllers, `
  src/BE/PlatformManager.Api/Services, `
  src/BE/PlatformManager.Api/Dtos, `
  src/BE/PlatformManager.Api/Common
Remove-Item -Force src/BE/PlatformManager.Api/Program.cs
# GIỮ: Properties/launchSettings.json, appsettings*.json, .csproj (viết lại ở P0),
#      PlatformManager.slnx, .http (viết lại ở P4)

# FE — xoá toàn bộ app tree, giữ file scaffold project
Remove-Item -Recurse -Force src/FE/src/app/core, src/FE/src/app/shared, src/FE/src/app/modules
Remove-Item -Force src/FE/src/app/app.config.ts, src/FE/src/app/app.routes.ts, `
  src/FE/src/app/app.ts, src/FE/src/app/app.html, src/FE/src/app/app.scss, src/FE/src/app/app.spec.ts
# GIỮ: main.ts, index.html, styles.scss (sửa ở F1), package.json (thêm dep), angular.json (sửa budget)

Remove-Item -Recurse -Force src/BE/PlatformManager.Api/bin, src/BE/PlatformManager.Api/obj -ErrorAction SilentlyContinue
```

**Không dùng git.** Sau khi xoá, bạn tự `git status`/`git add`/`git commit`
khi thấy phù hợp.

## 2. BE — P0 → P4

```
PlatformManager.Domain          → không phụ thuộc gì
PlatformManager.Application     → chỉ phụ thuộc Domain
PlatformManager.Infrastructure  → phụ thuộc Application + Domain
PlatformManager.Api             → phụ thuộc Application + Infrastructure
```

**P0**: 4 `.csproj` (`Domain` không package; `Application` +
`MediatR`+`FluentValidation`; `Infrastructure` +
`EFCore`+`Npgsql`+`Identity.EntityFrameworkCore`+`CsvHelper`; `Api` +
`OpenApi`+`Swashbuckle`), `Directory.Build.props`, add vào
`PlatformManager.slnx`.

**P1 — `Domain`**: `Common/BaseEntity.cs` (`Id,UserCreate,UserUpdate,
DateCreate,DateUpdate,IsDelete`, public get/set), `Common/EntityId.cs`,
`Common/DomainException.cs`, `Common/ConflictException.cs`, entity
`CriteriaGroup`/`Criteria`/`CriteriaAssessment`/`CriteriaEvidence` (factory
`Create()` + mutation có validate). `AppUser`/`AppRole` không đặt ở Domain
(thuộc Infrastructure, `IdentityUser<Guid>` là kiểu framework).

**P2 — `Application`**: envelope core (`ErrorCode`, `ApiResultStatus`,
`IApiResult<T>`, `ApiResult<T>`, `ErrorDescriptor`, `BaseResponse`),
`ICommand`/`IQuery`, `ValidationBehavior`, `ExceptionHandlingBehavior`
(`DomainException`→422, `ConflictException`→409), `ICurrentUser`.

Slice nghiệp vụ DTI Weekly (port gần nguyên vẹn logic hiện có):
`Application/CriteriaGroups/`, `Application/Criteria/` (CRUD + grid
live/historical), `Application/Assessments/` (`AssessmentUpsertService` —
same-day upsert/copy-forward/derive-ProgressPercent, `CsvImportService` —
parse CSV tiếng Việt, tự tạo Criteria/Group/AppUser),
`Application/Dashboard/` (`AggregationService` — `ComputePeriodAggregate`,
badge Epsilon=0.001m, báo cáo HTML).

Slice CoreBase:
- `Application/Auth/` — `LoginCommand`, `LogoutCommand`,
  `GetCurrentUserQuery`, **`ChangePasswordCommand`** (mới — set
  `MustChangePassword=false` sau khi đổi thành công), `IIdentityService`.
- `Application/Users/` — `CreateUserCommand` (tạo với mật khẩu tạm →
  `MustChangePassword=true`), `UpdateUserCommand`,
  `LockUserCommand`/`UnlockUserCommand`, `GetUsersListQuery`, `UserDto`,
  `IUserAdminService`.
- `Application/Menu/` — `GetMenuQuery` (lọc theo role hiện tại đối chiếu
  `SysMenuRole`, màn không có dòng nào = mở cho mọi user).
- `Application/Permissions/` (mới) — `GetPermissionMatrixQuery` (toàn bộ
  `SysMenu` × 3 role, đánh dấu ô nào đang được gán),
  `UpdatePermissionMatrixCommand` (`[Authorize(Roles="SuperAdmin")]`, ghi
  đè toàn bộ `SysMenuRole` theo ma trận gửi lên), `ISysMenuRoleRepository`.

**P3 — `Infrastructure`**: `AppUser : IdentityUser<Guid>` (+`FullName`,
`DateCreate`, `DateUpdate`, **`MustChangePassword` bool default true**),
`AppRole : IdentityRole<Guid>`, `PlatformManagerDbContext :
IdentityDbContext<AppUser,AppRole,Guid>`, EF Configuration mỗi entity
(`ValueGeneratedNever` cho `Id`, filtered unique index
`WHERE "IsDelete"=false`), `Configurations/SysMenuRoleConfiguration.cs`
(PK ghép `SysMenuId`+`RoleId`), `AuditInterceptor`, repository cho từng
`I*Repository`, **`DbSeeder.cs` mở rộng** (giữ seed CSV
`CriteriaGroup`/`Criteria`, thêm: 3 role qua `RoleManager`, tài khoản
`SuperAdmin` qua `UserManager.CreateAsync` với mật khẩu tạm hardcode +
`MustChangePassword=true`, gán 2 role, upsert 4 dòng `SysMenu` (thêm mục
"Phân quyền") + `SysMenuRole` theo bảng ở trên — **chỉ chạy khi
`IsDevelopment()`**).

Migration: `dotnet ef migrations add InitialCreate` (sinh class C#, không
đụng DB) → `dotnet ef migrations script --idempotent -o
doc/ERD/migrations/0003_corebase_v2.sql` (bao gồm cả Identity+SysMenu+
SysMenuRole+DTI Weekly business tables — thay thế `0001`/`0002` cũ) → **bàn
giao file, bạn tự chạy tay trên Postgres**. Riêng index filtered theo ngày
cho `CriteriaAssessment` cần vá tay vào file script (EF `HasIndex` không
diễn đạt được `CAST`) — **gotcha #5, đã xảy ra lỗi thật**: viết thẳng
`CAST("DateCreate" AS date)` trong biểu thức index bị Postgres từ chối với
`42P17 functions in index expression must be marked IMMUTABLE` (ép kiểu
timestamptz→date phụ thuộc `TimeZone` session, không deterministic). Phải
tạo hàm `CREATE OR REPLACE FUNCTION criteria_assessment_date_utc(ts
timestamptz) RETURNS date LANGUAGE sql IMMUTABLE AS $$ SELECT (ts AT TIME
ZONE 'UTC')::date; $$` rồi dùng `criteria_assessment_date_utc("DateCreate")`
trong index thay vì `CAST` trực tiếp — đúng cách bản migration gốc của app
(trước khi xây lại) từng giải quyết bằng hàm `assessment_date_utc()`. Nếu
sau này chạy lại `dotnet ef migrations script` (model đổi), phần vá tay này
PHẢI giữ nguyên bản có hàm IMMUTABLE wrapper, không quay lại `CAST` trực
tiếp — xem comment tương ứng ở
`CriteriaAssessmentConfiguration.cs`.

**P4 — `Api`**: `ApiControllerBase.HandleResult`, `GlobalExceptionHandler`
(`ValidationException`→400+`Fields`, còn lại→500, không lộ stack trace),
`HttpContextCurrentUser`, controller (`AuthController` — thêm
`POST /api/auth/change-password`, `UsersController`, `MetaController`,
**`PermissionsController`** — `GET/PUT /api/admin/permissions`,
`CriteriaController`, `CriteriaGroupsController`, `DashboardController`,
`ImportController`). `Program.cs` — điểm rủi ro cao nhất: override
`CookieAuthenticationEvents.OnRedirectToLogin`/`OnRedirectToAccessDenied`
trả 401/403 JSON (không redirect 302 sang Razor); CORS
`AllowCredentials()` + origin cụ thể; thêm `app.UseAuthentication()`
(hiện code chỉ có `UseAuthorization()`); **bỏ hẳn**
`db.Database.MigrateAsync()`; seed gate `IsDevelopment()`.

## 3. FE — F0 → F3

**F0 — Envelope**: `core/http/api-result.model.ts` (shape camelCase mới),
interceptor đọc đúng `body.message` (sửa bug đọc sai bản cũ), rethrow kèm
`apiResult` để form đọc `fields`.

**F1 — PrimeNG + zoneless + đồng bộ design**: `npm install primeng
@primeng/themes primeicons chart.js`, gỡ `zone.js`
(`provideZonelessChangeDetection()`), custom preset PrimeNG map token hiện
có, budget `angular.json` (`initial 500kb/1mb`, `anyComponentStyle
4kb/8kb`). Xây lại `shared/` + `modules/dashboard/` + `modules/danh-muc-dti/`
1:1 cấu trúc cũ, grid chuyển `p-table` (rủi ro cần kiểm chứng sớm: inline
edit double-click của `CriteriaGridTable` cần `pEditableColumn` custom).
`shared/services/menu.service.ts` gọi `GET /api/meta/menu`, sidebar
**động hoàn toàn** (không hard-code).

**F3 — Auth + Quản trị người dùng + Phân quyền + Đổi mật khẩu**:
`credentials.interceptor.ts` (đăng ký trước error interceptor),
`CurrentUserService` (plain signal — chưa cần `signalStore`), `AuthService`,
`authGuard`, `adminGuard` (kiểm `hasRole('Admin')`), **`superAdminGuard`**
(riêng cho màn Phân quyền), màn login Angular thật khớp
prototype login (đã xoá 2026-08-23). **Route `/doi-mat-khau` mới** — sau khi
`GET /api/auth/me` trả `mustChangePassword:true`, mọi điều hướng khác bị
chặn, tự đưa về màn này trước (guard-level, không cho vào Dashboard/màn nào
khác cho tới khi đổi xong). Màn `quan-tri-nguoi-dung` (grid `p-table` +
dialog thêm/sửa user, khớp prototype cũ đã xoá) —
gate `Admin`+`SuperAdmin`. Màn **`phan-quyen` mới** (ma trận checkbox
Role×SysMenu) — gate `SuperAdmin` only.

**Chủ động KHÔNG cài `@ngrx/signals`** ở đợt này — 4 màn hình
(dashboard/danh-muc-dti/quan-tri-nguoi-dung/phan-quyen) đều single-page,
`signal()` thường trong page là đủ theo đúng ngưỡng đã chốt.

## 4. Điểm bàn giao BE ↔ FE

`backend-expert` phát hành API Contract Card DRAFT khi vào P2
(`doc/contracts/auth.md`, `users.md`, `meta-menu.md`, `permissions.md`),
AGREED khi P3 xong, IMPLEMENTED kèm response thật sau P4.
`frontend-expert` làm song song ngay từ đầu: F0 (shape đã chốt sẵn), phần
lớn F1 (PrimeNG/zoneless/dashboard/danh-muc-dti — theo hợp đồng đã biết
trước). F3 (login/quan-tri-nguoi-dung/phan-quyen thật) chờ P3+P4 vì hành vi
cookie/CORS/lockout không giả lập có ý nghĩa ở FE. `core-reviewer` audit
sau BE P2, sau BE P3+P4, sau FE F0+F1.

## 5. Kiểm chứng theo từng giai đoạn

- BE P0: `dotnet build` xanh, `Application` không thấy kiểu EF/Identity.
- BE P3: `dotnet ef migrations script` sinh file thành công; **tự tay chạy
  file `.sql` đó trên Postgres local** để xác nhận không lỗi cú pháp; sau
  đó chạy app ở Development, xác nhận `DbSeeder` chèn đúng 3 role + tài
  khoản `SuperAdmin` + 4 `SysMenu` + `SysMenuRole`; xoá mềm 1 `Code` rồi
  tạo lại đúng `Code` đó phải thành công (chứng minh filtered index).
- BE P4: gọi thử mọi endpoint qua `.http`/curl; endpoint `[Authorize]` khi
  chưa đăng nhập → 401 JSON sạch (không 302); trùng field unique → 409;
  đăng nhập bằng tài khoản `SuperAdmin` → `GET /api/auth/me` trả
  `mustChangePassword:true`; đổi mật khẩu xong → trả `false`.
- FE F0: unit test interceptor cố tình assert theo field PascalCase cũ
  trước (phải fail), sửa lại rồi mới pass.
- FE F1: `ng build` xanh trong budget; so màn hình thật với
  prototype cũ (đã xoá 2026-08-23) để đối chiếu màu/bố cục; sidebar chỉ hiện đúng mục
  được `SysMenuRole` cho phép theo role đang đăng nhập.
- FE F3: đăng nhập `SuperAdmin` lần đầu → bị chặn, đưa về `/doi-mat-khau`,
  không vào được Dashboard cho tới khi đổi xong; user role `User` vào
  `/quan-tri/nguoi-dung` hoặc `/quan-tri/phan-quyen` → bị chặn ở FE **và**
  BE vẫn trả 403 nếu cố bỏ qua guard; role `Admin` (không phải SuperAdmin)
  vào `/quan-tri/phan-quyen` → cũng bị chặn (chỉ SuperAdmin).

## File quan trọng nhất khi thực thi

- `src/BE/PlatformManager.Api/Program.cs` — rủi ro cao nhất (cookie auth
  events override + bỏ auto-migrate).
- `src/BE/PlatformManager.Infrastructure/Persistence/PlatformManagerDbContext.cs`
  — nơi Identity + business entity + `SysMenuRole` + soft-delete hội tụ.
- `src/BE/PlatformManager.Infrastructure/Persistence/DbSeeder.cs` — toàn bộ
  logic seed role/bootstrap-user/MustChangePassword/SysMenu/SysMenuRole.
- `src/BE/PlatformManager.Application/Common/Behaviors/ExceptionHandlingBehavior.cs`
  — quyết định lời hứa envelope P2 có giữ được xuyên suốt hay không.
- `src/FE/src/app/core/interceptors/http-error.interceptor.ts` — sửa đúng
  bug đọc field PascalCase cũ.
- `src/FE/src/app/core/auth/current-user.service.ts` +
  guard liên quan — nơi enforce luồng bắt đổi mật khẩu lần đầu.
- `doc/cau-truc-database.sql` (DDL viết tay) + `dotnet ef database update` (mới, thay thế `0001`/`0002`)
  — file script bạn sẽ tự chạy tay trên Postgres thật.

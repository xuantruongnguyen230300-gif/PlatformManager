# ERD — CoreBase (Identity/Đăng nhập + SysMenu)

> **Trạng thái: DỰ KIẾN.** ERD cho lần migration đầu tiên của phần corebase
> `src/BE`, tách riêng khỏi [`ERD.md`](./ERD.md) (nghiệp vụ DTI) theo đúng
> hướng đã chốt 2026-08-15: *"hiện tại chỉ xây corebase chứ chưa áp dụng
> business vào"*. Đối chiếu lại với người dùng trước khi `dotnet ef
> migrations add` lần đầu.
>
> File DBML: [`PlatformManager-corebase.dbml`](./PlatformManager-corebase.dbml).
> Migration SQL: [`migrations/0001_corebase_identity_sysmenu.sql`](./migrations/0001_corebase_identity_sysmenu.sql)
> + [`migrations/0002_seed_corebase.sql`](./migrations/0002_seed_corebase.sql).

## Phạm vi — vì sao tách file riêng khỏi `ERD.md`

`ERD.md` hiện có mô tả nghiệp vụ DTI (Criteria/CriteriaAssessment/
CriteriaGroup) và vẫn đang dùng tên field `BaseEntity` **cũ**
(`CreatedAt`/`UpdatedAt`/`IsDeleted`) — chưa cập nhật theo quyết định
"theo blueprint đầy đủ" (2026-08-15, đổi `BaseEntity` sang
`DateCreate`/`DateUpdate`/`UserCreate`/`UserUpdate`/`IsDelete`), vì đây là
nợ tài liệu đã ghi nhận ở audit "Rà Soát Toàn Bộ Doc" nhưng **chưa tới lượt
sửa** — người dùng chọn ưu tiên corebase trước, nghiệp vụ DTI sau. File
**này** vì vậy:

- Dùng **đúng** tên field `BaseEntity` mới nhất — làm mẫu tham chiếu đúng
  cho `backend-expert` khi build corebase, không kế thừa lỗi tên field cũ.
- Chỉ chứa entity CoreBase: Identity (đăng nhập) + `SysMenu` (metadata menu).
  **Không** đụng tới Criteria/CriteriaAssessment/CriteriaGroup.
- Migration file này chạy **trước** migration nghiệp vụ DTI — bảng nghiệp vụ
  (`CriteriaAssessments.OwnerId`) có FK trỏ vào `AspNetUsers`.

## 1. Đăng nhập — ASP.NET Core Identity (đã CHỐT)

Không thiết kế lại từ đầu — dùng nguyên schema chuẩn của
`Microsoft.AspNetCore.Identity.EntityFrameworkCore` qua
`IdentityDbContext<AppUser, AppRole, Guid>`
(xem `src/BE/.claude/rules/api-controller.md` §Auth/Permission). 7 bảng:

| Bảng | Vai trò |
| --- | --- |
| `AspNetUsers` | Người dùng — **đây chính là "SysUser"** trong cách gọi thường ngày, không phải bảng riêng |
| `AspNetRoles` | Vai trò (Admin/User...) |
| `AspNetUserRoles` | Join nhiều-nhiều User↔Role |
| `AspNetUserClaims` | Claim cấp user — chưa dùng, tồn tại vì Identity luôn sinh |
| `AspNetUserLogins` | Đăng nhập ngoài (Google/Microsoft...) — chưa có nhu cầu |
| `AspNetUserTokens` | Token nội bộ (reset password, 2FA) — Identity tự quản lý |
| `AspNetRoleClaims` | Claim cấp role — chưa dùng |

**"SysUser" = `AppUser`/`AspNetUsers`, không phải khái niệm khác.** Tránh
nhầm lẫn tạo 1 bảng user thứ 2 song song — đúng đắn duy nhất là mở rộng
`AppUser` (thêm cột) khi cần thêm thông tin, không tạo bảng mới.

### 1.1. Mở rộng `AppUser` — 3 cột ngoài chuẩn Identity

| Cột | Lý do |
| --- | --- |
| `FullName` | Identity mặc định không có tên hiển thị đầy đủ — cần cho cột "Người dùng" ở màn Quản trị (`quan-tri-nguoi-dung.html`) |
| `DateCreate` | Identity mặc định không có ngày tạo — cần cho cột "Ngày tạo" |
| `DateUpdate` | Cùng lý do `DateCreate` |

`AppUser` **không kế thừa `BaseEntity`** — đã ghi rõ trong
`src/BE/.claude/rules/entity-domain.md`: Identity tự quản lý vòng đời user
bằng field riêng (`LockoutEnd`, `SecurityStamp`...), không áp `IsDelete`
(soft-delete qua `LockoutEnd`/`LockoutEnabled` sẵn có thay vì thêm cột mới).
Tên 2 cột `DateCreate`/`DateUpdate` cố ý đặt trùng quy ước `BaseEntity` dù
không kế thừa — để đọc hiểu nhất quán, không phải để chia sẻ code.

### 1.2. Trạng thái tài khoản → badge UI

`LockoutEnd` map thẳng vào badge "Đang hoạt động"/"Đã khoá" đã có ở
`quan-tri-nguoi-dung.html`:

```
LockoutEnd IS NULL hoặc LockoutEnd < now()  → "Đang hoạt động"
LockoutEnd >= now()                          → "Đã khoá"
```

Khoá tài khoản = set `LockoutEnd` = 1 mốc xa trong tương lai (hoặc
`DateTimeOffset.MaxValue`) qua `UserManager.SetLockoutEndDateAsync` — không
tự ghi cột này bằng tay, không tự thêm cột `IsActive` riêng (trùng lặp
khái niệm với cơ chế Identity sẵn có).

### 1.3. Không tự hash password trong migration/seed

`PasswordHash` **không** được seed sẵn trong SQL — Identity dùng
`PasswordHasher<TUser>` (PBKDF2, salt ngẫu nhiên mỗi lần) để hash, không có
cách nào tạo hash hợp lệ bằng tay trong SQL thuần. Tài khoản đầu tiên (nếu
cần) phải tạo qua code thật (`UserManager.CreateAsync(user, password)`),
không phải qua migration — xem ghi chú trong
`migrations/0002_seed_corebase.sql`.

## 2. `SysMenu` — metadata menu điều hướng

Đối chiếu `doc/huong_dan/wiki-core/be/03-metadata-driven-design.md` §3.1 —
đây là **Loại C** (dữ liệu thuần, DB tự do 100%, không có Tầng 2/3 vật lý
nào để lệch — khác cột grid Loại A, vốn *sinh từ code* và chỉ override phần
trình bày). Khớp 1:1 hợp đồng `IMenuItem` đã thiết kế trước ở
`doc/huong_dan/wiki-core/fe/11-grid-and-metadata.md` §Metadata sync — BE/FE
không phải đàm phán lại shape khi implement thật.

### 2.1. Vì sao build ngay bây giờ, không đợi ngưỡng

`be/03` nói menu "chưa cần bảng riêng" ở ngưỡng 2 màn hình (hard-code trong
route là đủ). Nay đã lên **3 nhóm điều hướng** (`Dashboard`, `Danh mục>DTI`,
`Quản trị hệ thống>Người dùng`) và người dùng đã minh thị yêu cầu dựng
`SysMenu` — ngưỡng đã chạm về mặt thực tế (không phải build trước phòng
hờ), đúng tinh thần Nhóm A/B: xây khi nỗi đau đã hiện diện, không phải khi
"có thể cần sau này".

### 2.2. Cấu trúc cây — tự tham chiếu, đúng 1 cấp

`ParentId` tự trỏ vào chính `SysMenu.Id`, `NULL` = item gốc. Chỉ hỗ trợ
**đúng 1 cấp lồng** (con không có con riêng) — khớp giới hạn đã ghi ở
`spec/sidebar-menu/ui-spec.md` mục 1.1 ("`children` không có `children` con
bên trong — chưa có nhu cầu cấp 3"). Item cha (`ParentId IS NULL` nhưng có
con) có `Route = NULL` — cha chỉ toggle expand/collapse, không điều hướng.

### 2.3. `RequiredRole` — phạm vi rút gọn có chủ đích

Đặt tên cột `RequiredRole` (không phải `RequiredPermission` dù hợp đồng FE
dùng tên tổng quát hơn) vì role Identity cụ thể **"chưa được chốt"** (câu
hỏi mở tồn đọng từ `ERD.md` cũ). Dùng thẳng tên `AspNetRoles.Name` (string,
không FK cứng — role có thể đổi tên/xoá mà không vỡ ràng buộc) là đủ cho
nhu cầu hiện tại (2-3 nhóm menu, chưa có ma trận quyền chi tiết theo
resource-action). Nâng cấp lên hệ permission-key thật (`[RequirePermission]`
kiểu `be/05-p4-hosting-api.md`) **khi** có nhu cầu phân quyền chi tiết hơn
role đơn — không thiết kế trước cho nhu cầu chưa xuất hiện.

### 2.4. Seed data — khớp đúng UI đã build, không bịa thêm

`migrations/0002_seed_corebase.sql` seed đúng 3 mục đã tồn tại thật trong
`doc/Prototype/{dashboard,danh-muc-dti,quan-tri-nguoi-dung}.html` — không
thêm mục menu nào chưa có trang thật tương ứng (vd chưa seed "Menu hệ
thống"/SysMenu-quản-trị dù đã dự phòng chỗ trong cấu trúc `children`, vì
chưa có trang UI thật cho nó — xem `spec/sidebar-menu/ui-spec.md` mục 1.4).

## 3. Thứ tự migration

```
0001_corebase_identity_sysmenu.sql   ← Identity 7 bảng + SysMenu (DDL)
0002_seed_corebase.sql               ← seed AspNetRoles + SysMenu (DML)
        │
        ▼ (sau, khi làm tới nghiệp vụ DTI — CHƯA chạy)
[migration nghiệp vụ DTI — chờ ERD.md cập nhật tên field BaseEntity trước]
```

Migration nghiệp vụ DTI **không** chạy trước khi `ERD.md`/
`PlatformManager.dbml` (bản cũ) được cập nhật tên field — nếu chạy migration
theo tên field cũ (`CreatedAt`/`UpdatedAt`/`IsDeleted`) rồi mới sửa
`entity-domain.md` sau, sẽ phải viết migration đổi tên cột trên bảng đã có
dữ liệu, tốn hơn nhiều so với sửa tài liệu trước khi có dòng SQL nào chạy
thật.

## 4. Câu hỏi còn mở (không tự quyết)

- **Đăng ký tài khoản mới**: qua màn hình riêng, hay chỉ Admin tạo qua màn
  Quản trị người dùng (`quan-tri-nguoi-dung.html` đã có nút "+ Thêm người
  dùng")? Ảnh hưởng có cần endpoint `/api/auth/register` công khai hay
  không.
- **Vai trò khởi tạo**: seed đúng 2 role gì (`migrations/0002` seed tạm
  "Admin"/"User" — cần xác nhận tên/số lượng role thật trước khi dùng lâu
  dài, đổi tên role sau khi đã gán cho user thật sẽ cần migrate dữ liệu).

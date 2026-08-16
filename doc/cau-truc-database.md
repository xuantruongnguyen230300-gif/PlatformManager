# Cấu trúc Database — PlatformManager (PostgreSQL)

> Tài liệu mô tả cấu trúc DB thật (không phải ERD dự kiến). Đối chiếu
> `doc/kien-truc-core-module.md` để hiểu lý do tách Core ↔ Business ở tầng
> code — tài liệu này mô tả cách ranh giới đó phản ánh xuống DB.

## 1. Vì sao tách schema, không dồn hết vào `public`

Cùng 1 database Postgres (`postgres` — hoặc tên DB thật bạn đặt), nhưng chia
làm **2 schema** thay vì để tất cả bảng dùng chung schema mặc định
`public`:

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

Kết quả: schema `public` mặc định của Postgres **hoàn toàn sạch** — không
còn bảng, hàm, hay object nào của app nằm ở đó (kể cả `__EFMigrationsHistory`
đã chuyển vào `core`, và hàm hỗ trợ index `criteria_assessment_date_utc()`
đã chuyển vào `business`).

## 2. Danh sách bảng theo schema

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
| `SysMenus` | Menu điều hướng động (sidebar), tự tham chiếu `ParentId` cho cây 1 cấp |
| `SysMenuRoles` | Role nào được thấy menu nào (nhiều-nhiều) |
| `__EFMigrationsHistory` | Bảng nội bộ EF Core theo dõi migration đã áp dụng |

### Schema `business`

| Bảng | Mô tả |
|---|---|
| `CriteriaGroups` | Nhóm chỉ tiêu đánh giá DTI Weekly |
| `Criteria` | Chỉ tiêu đánh giá cụ thể, thuộc 1 `CriteriaGroups` |
| `CriteriaAssessments` | Kết quả đánh giá 1 `Criteria` theo từng kỳ (phần ngày của `DateCreate` = kỳ) |
| `CriteriaEvidences` | Minh chứng đính kèm 1 `CriteriaAssessments` (nhiều dòng/bản ghi) |

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
- **Không có `Id` nào để DB tự sinh** (`DEFAULT gen_random_uuid()`) — ứng
  dụng luôn tự tạo `Guid` trước khi insert (`EntityId.New()`), tránh đúng
  lỗi EF Core hiểu nhầm key-đã-set = "đã tồn tại" khi thêm entity con vào
  collection đã tracked.

## 5. File migration tương ứng

- **Migration C#** (nguồn sự thật, EF Core sinh ra):
  `src/BE/Core/PlatformManager.Core.Infrastructure/Persistence/Migrations/`
- **File `.sql`** (để chạy tay lên Postgres — người dùng tự chạy, không
  agent nào tự động chạy DDL lên DB thật): có 2 bản giống nhau, giữ đồng bộ
  cùng lúc khi migration đổi —
  `doc/ERD/migrations/0003_corebase_v2.sql` và
  `src/BE/Core/PlatformManager.Core.Infrastructure/Persistence/Migrations/sql/0003_corebase_v2.sql`.

## 6. Thêm bảng mới sau này — vào schema nào

Tự hỏi đúng câu đã dùng để quyết định `Core.*` hay `Business.*` ở tầng code
(xem `doc/kien-truc-core-module.md`): bảng đó có ý nghĩa với **mọi** sản
phẩm dựng trên nền tảng này (→ `core`), hay chỉ riêng nghiệp vụ hiện tại
(→ `business`)? EF Configuration của entity mới khai `schema: "core"` hoặc
`schema: "business"` tương ứng — không tạo schema thứ 3 trừ khi thật sự có
domain nghiệp vụ độc lập khác xuất hiện (xem `doc/kien-truc-core-module.md`
§ Khi nào tách thành module độc lập thật).

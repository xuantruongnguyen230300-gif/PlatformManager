# 13. Core data — di trú & seed khi hệ thống đã có người dùng thật

> Phạm vi: **chỉ bảng `core` schema** — `AspNetUsers`/`AspNetRoles` (người
> dùng, vai trò), `SysMenus`/`SysMenuRoles` (menu điều hướng),
> `RolePermissions` (phân quyền theo hành động). **Không áp dụng cho bảng
> `business`** (`CriteriaAssessments`...) — dữ liệu nghiệp vụ có luật di trú
> riêng theo từng module, không thuộc phạm vi core dùng lại được.

## 0. Vì sao Core data cần luật riêng, khác dữ liệu nghiệp vụ

Một migration hỏng trên `CriteriaAssessments` làm sai lệch số liệu 1 module.
Một migration hỏng trên `RolePermissions`/`SysMenus` có thể khoá **toàn bộ
hệ thống cho mọi người dùng cùng lúc** — vì `RequirePermissionFilter` là
**deny-by-default**: "chưa có dòng `RolePermission` nào cho key này" nghĩa là
từ chối, không phải cho qua. Core data không chỉ *lưu* trạng thái hệ thống,
nó *là* cơ chế kiểm soát ai được làm gì — nên mọi thay đổi lên nó phải được
xử lý như một bản vá bảo mật, không phải một migration dữ liệu thông thường.

## 1. Ba rủi ro cụ thể đã có bằng chứng trong code thật (không phải giả định)

| # | Rủi ro | Vì sao xảy ra | Nguồn |
| --- | --- | --- | --- |
| 1 | Thêm `[RequirePermission("key.moi")]` lên 1 endpoint **đang chạy** mà không migrate `RolePermissions` cùng lúc → mọi user không phải `SuperAdmin` bị 403 ngay, kể cả thao tác họ vẫn làm được hôm qua | Deny-by-default; bảng seed rỗng = deny toàn bộ, không phải deny-riêng-key-đó | `doc/contracts/permissions.md` §"Rủi ro rollout" |
| 2 | Xoá mềm 1 `SysMenu` rồi tạo lại cùng `Code` → **thất bại**, trong khi `Criteria`/`CriteriaGroup` làm y hệt thì được | `IX_SysMenus_Code` là unique **không filter** theo `IsDelete` (khác 2 bảng kia) — mất mệnh đề partial index từ bản `0001` sang `0003`, chưa ai vá | `doc/cau-truc-database.md` §4 |
| 3 | `PUT` ghi đè toàn bộ ma trận `RolePermissions`/`SysMenuRoles` — bỏ tick 1 role ở 1 dòng **âm thầm mở rộng quyền** cho mọi user còn lại của role đó nếu thao tác/script sai logic diff | Cả 2 endpoint đều full-replace, không phải diff-and-patch — không có "xác nhận trước khi ghi đè" | `Screens/04-phan-quyen.md` § Normalize on redesign #5 |

## 2. Nguyên tắc thi công — expand trước, contract sau

Áp dụng đúng pattern "zero-downtime schema change" cho **cả schema lẫn dữ
liệu** của Core, không chỉ cột/bảng:

1. **Seed mới trước khi code cần nó chạy** — nếu sắp thêm `[RequirePermission]`
   mới, migration/script cấp quyền cho `RolePermissions` phải chạy **trước
   hoặc cùng lúc** với migration schema, không bao giờ sau. Ngược thứ tự =
   đúng kịch bản rủi ro #1 ở trên.
2. **`ResourceKey`/`SysMenu.Code`/`SysMenu.Id` là hợp đồng, không tái sử dụng
   sau khi khai tử.** Xoá 1 resource key hay đổi ý nghĩa 1 `Code` cũ sang màn
   hình khác = risk cấp nhầm quyền cho dòng `RolePermission` mồ côi còn sót
   lại trỏ vào key đó. Muốn đổi tên hiển thị thì sửa `Name`, giữ nguyên
   `Code`/key.
3. **Seed production KHÔNG được trông cậy vào `CoreSeeder`.** `CoreSeeder`
   (`SeedRolesAsync`/`SeedBootstrapUserAsync`/`SeedMenuAsync`) chỉ chạy khi
   `IsDevelopment()` — đây là quyết định đúng (seed code không nên tự chạy
   trên DB thật), nhưng hệ quả là **production cần một đường seed/migrate
   khác, tách biệt, chạy có kiểm soát** (migration SQL thủ công hoặc script
   vận hành riêng) — không phải "quên chưa làm", mà là một khoảng trống quy
   trình cần lấp trước khi có user thật ngoài đội dev.
4. **Đổi cấu trúc `SysMenus` (thêm/xoá/di chuyển node cây) phải kiểm
   `SysMenuRoles` mồ côi sau đó** — xoá 1 menu cha không tự xoá quyền các
   role đã được cấp cho nó; dọn dữ liệu tham chiếu treo là bước riêng, không
   tự động.

## 3. Break-glass — lưới an toàn cuối, không phải quy trình chính

`RequirePermissionFilter` có nhánh bypass tường minh cho `Roles.SuperAdmin`
— **cố ý**, đúng để một migration `RolePermissions` sai không khoá toàn bộ
đội vận hành (xem lịch sử ở `doc/contracts/permissions.md` §"Rủi ro
rollout": trước 2026-08-19 code từng KHÔNG có bypass này, và tài khoản
`SuperAdmin` thật đã bị 403 vì đúng lỗi này). Hệ quả cần nhớ khi thiết kế
migration:

- Bypass chỉ cứu được nếu **còn ít nhất 1 user mang đúng role `SuperAdmin`**.
  Một migration/script vô tình đổi role của toàn bộ `SuperAdmin` thật thì
  không còn lưới nào — chỉ còn sửa thẳng DB.
- Quyền `SuperAdmin` **không thu hồi được** qua UI ma trận phân quyền (theo
  thiết kế) — đừng viết script "dọn dẹp RolePermissions" mà giả định xoá
  hàng loạt sẽ ảnh hưởng tới `SuperAdmin`, nó sẽ không có tác dụng và có thể
  khiến người viết tưởng nhầm là đã xong.
- Break-glass là **lưới cuối cho lỗi migration**, không phải lý do bỏ qua
  §2 — vẫn phải seed đúng cho `Admin`/`User` trước khi bật `[RequirePermission]`
  mới, vì tuyệt đại đa số user không phải `SuperAdmin`.

## Áp dụng vào PlatformManager

✅ **Đóng 2026-08-24** (khoảng trống mô tả dưới đây tồn tại thật cho tới đúng
ngày này, không phải lý thuyết): `CoreSeeder.SeedRolePermissionsAsync()`
(`src/BE/Core/PlatformManager.Core.Infrastructure/Persistence/CoreSeeder.cs`)
nay seed đủ 3 `ResourceKeys.All` cho `Admin`/`User` ở Development, gọi ngay
sau `SeedRolesAsync()` trong `SeedAsync()`. Đường seed production tương ứng:
`scripts/seed-role-permissions.sql` (idempotent, `ON CONFLICT DO NOTHING`).
`[RequirePermission]` đã gắn lên cả 3 controller (`CriteriaController`/
`CriteriaGroupsController`/`ImportController`) CÙNG lúc với đợt seed này —
đúng thứ tự "expand trước, contract sau" ở mục 2.

Lịch sử (giữ để nhớ vì sao mục này từng là điều kiện chặn): trước 2026-08-24,
`RolePermissions` đã có entity/filter/`ResourceKeys` nhưng **không có bất kỳ
đường seed nào** — `CoreSeeder` không seed bảng này ở bất kỳ môi trường nào
(kể cả Development), và chưa có script/migration seed thủ công thay thế. Gắn
`[RequirePermission]` lên endpoint đang chạy thật lúc đó sẽ khiến mọi user
không phải `SuperAdmin` bị 403 hàng loạt.

`SysMenus` mang sẵn lỗi #2 ở mục 1 (unique index thiếu filter `IsDelete`) —
chưa vá; bất kỳ thao tác "xoá rồi tạo lại menu cùng Code" nào trước khi vá
sẽ fail, ghi nhớ khi viết script di trú menu.

## ✅ Quyết định người dùng 2026-08-24 — tách tài khoản bootstrap SuperAdmin/Admin

`CoreSeeder` trước đây seed **1 tài khoản duy nhất mang cả 2 role** (SuperAdmin
+ Admin gộp). Nay tách thành **2 tài khoản riêng, mỗi tài khoản đúng 1 role**
(`BootstrapOptions.SuperAdminPassword` / `.AdminPassword`, đọc qua
`IOptions<BootstrapOptions>` + `ValidateOnStart()`, không hardcode trong
source).

**Lý do (đã bàn trực tiếp với người dùng):** `SuperAdmin` là break-glass —
bypass mọi `[RequirePermission]` và quyền của nó **không thu hồi được** qua
UI ma trận phân quyền (xem mục 3 ở trên). Dùng chung 1 tài khoản cho việc
quản trị hàng ngày lẫn quyền tối cao nghĩa là **mọi phiên làm việc thường
ngày đều mang sẵn quyền cao nhất không cần thiết** — đúng nguyên tắc
least-privilege bị vi phạm nếu không tách. Tách ra: `Admin` dùng cho việc
hàng ngày (quyền giới hạn hơn), `SuperAdmin` chỉ đăng nhập khi thật sự cần
(khôi phục hệ thống, sửa quyền bị khoá nhầm) — giảm phạm vi thiệt hại nếu 1
phiên bị lộ.

Đánh đổi đã chấp nhận: quản lý 2 mật khẩu thay vì 1 — chấp nhận được ở quy
mô đội nhỏ.

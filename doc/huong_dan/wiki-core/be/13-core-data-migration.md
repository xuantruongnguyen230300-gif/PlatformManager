---
kind: luat
scope: core
verified: 2026-09-06
---

# 13. Core data — di trú & seed khi hệ thống đã có người dùng thật

> Phạm vi: **chỉ bảng `core` schema** — `AspNetUsers`/`AspNetRoles` (người
> dùng, vai trò), `SysMenus`/`SysMenuRoles` (menu điều hướng),
> `RolePermissions` (phân quyền theo hành động). **Không áp dụng cho bảng
> `business`** (`CriteriaAssessments`...) — dữ liệu nghiệp vụ có luật di trú
> riêng theo từng module, không thuộc phạm vi core dùng lại được.

> File này nói về di trú **DỮ LIỆU** core (user, role, menu, ma trận quyền) — **không** nói
> về cơ chế sinh migration schema. Hai thứ hay bị gộp vì cùng gọi là "migration", nhưng hỏng
> theo hai kiểu khác hẳn: sai dữ liệu core là khoá người dùng ngoài hệ thống, sai schema là
> mất bảng.
> 📖 Migration schema thuộc host, Core ship `.sql`: đọc `doc/cau-truc-database.md` §5.3

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
| 2 | ~~Xoá mềm 1 `SysMenu` rồi tạo lại cùng `Code` → **thất bại**~~ — **đã vá 2026-08-28** (migration `0008` thêm partial filter, có integration test chống tái phát) | `IX_SysMenus_Code` từng là unique **không filter** theo `IsDeleted` (khác 2 bảng kia) — mệnh đề partial mất từ bản `0001` sang `0003`. Giữ dòng này lại vì bài học vẫn đúng: một mệnh đề `WHERE` biến mất khỏi index không gây lỗi biên dịch và không test nào bắt được, cho tới khi có người thử tái dùng mã | `doc/cau-truc-database.md` §4 |
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
sau `SeedRolesAsync()` trong `SeedAsync()`.

> ### ✅ Đường seed production — ĐÃ CÓ (đối chiếu 2026-09-06; mục này ghi lại lúc nó còn thiếu)
>
> ```bash
> ls scripts/seed-role-permissions.sql    # PASS khi file có thật
> ```
>
> Bản trước của đoạn này khẳng định file đó tồn tại — **sai**, `scripts/` rỗng.
> Hậu quả nếu deploy production hôm nay: `CoreSeeder.SeedRolePermissionsAsync`
> chỉ chạy ở Development, nên bảng `RolePermissions` trống, và **mọi user không
> phải `SuperAdmin` bị 403 hàng loạt** — đúng kịch bản mà đoạn "Lịch sử" ngay
> dưới tuyên bố đã đóng.
>
> Chốt 2026-08-27: viết `scripts/seed-role-permissions.sql` idempotent
> (`ON CONFLICT DO NOTHING`), chạy có chủ đích khi triển khai — không nới
> `CoreSeeder` ra production, vì seed dữ liệu thật lúc app khởi động không kiểm
> soát được thời điểm và sẽ thành vấn đề khi chạy nhiều instance.
>
> **Cập nhật 2026-08-30: file đã có thật** (`ls scripts/seed-role-permissions.sql`
> nay PASS; viết ngày 2026-08-29). Nhưng nó chỉ phủ **một** bảng trong bốn bảng
> `core` mà mục này bao — xem mục quyết định 2026-08-30 ở cuối file cho ba bảng
> còn lại.
`[RequirePermission]` khi đó đã gắn lên cả 3 controller (`CriteriaController`/
`CriteriaGroupsController`/`ImportController`) CÙNG lúc với đợt seed này —
đúng thứ tự "expand trước, contract sau" ở mục 2.

> 🔄 **LẬT 2026-09-06.** Câu trên viết ở thì hiện tại, nhưng **cả 3 controller đó
> không còn tồn tại** — chúng bị xoá cùng module DtiWeekly ngày 2026-08-29.
> `ls src/BE/PlatformManager.Api/Controllers/` hôm nay chỉ có `AuthController`,
> `MetaController`, `PermissionsController`, `UsersController`, và **không controller
> nào đang khai `[RequirePermission]`** (gap đã biết, chủ sở hữu là
> [`../../quy-uoc/be-api-controller.md`](../../quy-uoc/be-api-controller.md)
> §Phân quyền theo hành động). Giữ câu trên như **ghi chép lịch sử về thứ tự triển
> khai đúng**, không đọc nó như hiện trạng.

Lịch sử (giữ để nhớ vì sao mục này từng là điều kiện chặn): trước 2026-08-24,
`RolePermissions` đã có entity/filter/`ResourceKeys` nhưng **không có bất kỳ
đường seed nào** — `CoreSeeder` không seed bảng này ở bất kỳ môi trường nào
(kể cả Development), và chưa có script/migration seed thủ công thay thế. Gắn
`[RequirePermission]` lên endpoint đang chạy thật lúc đó sẽ khiến mọi user
không phải `SuperAdmin` bị 403 hàng loạt.

`SysMenus` mang sẵn lỗi #2 ở mục 1 (unique index thiếu filter `IsDeleted`) —
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

## ✅ Quyết định người dùng 2026-08-30 — bootstrap Production bằng lệnh riêng (ĐÃ THI CÔNG)

> 🔄 **LẬT 2026-09-06.** Mục này còn nhãn `🚧` cho việc **đã xong**. Cả hai quyết định
> đã vào code: lệnh seed riêng ở `src/BE/PlatformManager.Api/Common/SeedCommand.cs`
> (nhận diện tham số tại `src/BE/PlatformManager.Api/Program.cs:36`, chạy rồi thoát tại
> `src/BE/PlatformManager.Api/Program.cs:456`), và `ValidateOnStart()` nay **có điều
> kiện** qua tham số `requireBootstrapOptions`
> (`src/BE/Core/PlatformManager.Core.Infrastructure/DependencyInjection.cs:164`).
> Phần "Khoảng trống" dưới đây mô tả trạng thái **trước** khi thi công.

### Khoảng trống (trạng thái TRƯỚC khi thi công)

Mục 2026-08-27 ở trên đóng đường seed production cho **`RolePermissions`**. Ba
bảng `core` còn lại trong phạm vi file này — `AspNetRoles`, `AspNetUsers`,
`SysMenus`/`SysMenuRoles` — **chưa có đường nào**. Toàn bộ `CoreSeeder.SeedAsync()`
nằm sau một hàng rào:

```bash
grep -n "IsDevelopment" src/BE/PlatformManager.Api/Program.cs   # PASS khi seeder KHÔNG còn bị gate này bọc
```

Hậu quả trên một database production mới: không role, không tài khoản, `SysMenus`
rỗng nên sidebar trắng. **Không ai đăng nhập được, và không có đường tạo tài khoản
đầu tiên** — tạo người dùng là chức năng đòi đăng nhập.

Có một chi tiết làm nó khó chẩn đoán hơn hẳn. `BootstrapOptions` **khi đó** khai
`ValidateOnStart()` **không điều kiện**, nên ở
Production app **bắt buộc** phải có `Bootstrap__SuperAdminPassword` và
`Bootstrap__AdminPassword` mới khởi động nổi — hai secret mà seeder không bao giờ
đọc tới trong môi trường đó. Người vận hành đặt secret, thấy app lên, và kết luận
đã bootstrap xong.

> Nay đã sửa: `ValidateOnStart()` chỉ gắn khi `requireBootstrapOptions` bật —
> `src/BE/Core/PlatformManager.Core.Infrastructure/DependencyInjection.cs:166`.
> *(🔄 LẬT 2026-09-06: trích dẫn cũ `src/BE/Core/PlatformManager.Core.Infrastructure/DependencyInjection.cs:71` trỏ vào một dòng chú
> thích về assembly migration, không liên quan gì tới `BootstrapOptions`.)*

### Vì sao không dùng SQL như `RolePermissions`

Phản xạ tự nhiên là làm giống mục 2026-08-27: viết thêm script SQL. Không được cho
`AspNetUsers` — mật khẩu phải đi qua bộ băm của ASP.NET Identity
(`UserManager.CreateAsync`,
`src/BE/Core/PlatformManager.Core.Infrastructure/Persistence/CoreSeeder.cs:242` — đối chiếu
2026-09-06, trích dẫn cũ `CoreSeeder.cs:115` đã lạc). `INSERT` viết tay sinh ra tài
khoản **không đăng nhập được**. Role và menu thì SQL làm được; tài khoản thì không,
và tách hai nửa ra hai cơ chế là tự tạo cho mình hai thứ phải giữ đồng bộ.

### Chốt

| # | Quyết định |
|---|---|
| 1 | Seed production chạy bằng **một lệnh riêng, một lần** — tiến trình seed xong thì thoát, không mở cổng. Tiến trình API phục vụ thật **không bao giờ** ghi dữ liệu seed |
| 2 | `BootstrapOptions` **chỉ** bị bắt buộc khi lệnh seed chạy. Tiến trình API không đòi hai secret đó nữa |

Quyết định 1 giữ nguyên nguyên tắc đã nêu ngày 2026-08-27 (*"không seed lúc app
khởi động"*) và nới nó đúng chỗ cần: vấn đề chưa bao giờ là **CoreSeeder**, mà là
**seed lúc khởi động**. Một lệnh riêng tách được hai thứ đó.

Quyết định 2 là hệ quả trực tiếp: hai mật khẩu quản trị chỉ dùng đúng một lần
trong đời hệ thống, không có lý do gì để chúng nằm thường trực trên máy production
sau đó. Giảm bề mặt bí mật là lý do; hiệu ứng phụ dễ thấy hơn là nó xoá luôn hiểu
lầm *"đặt được secret nghĩa là đã bootstrap"*.

### Đã chốt → đã thành (✅ đối chiếu 2026-09-06)

| | Trước (2026-08-30) | Đo được hôm nay |
|---|---|---|
| Chạy seeder | Chỉ ở Development, gate `IsDevelopment()` bọc quanh `CoreSeeder.SeedAsync()` | Gate đã gỡ — `grep -n "IsDevelopment" src/BE/PlatformManager.Api/Program.cs` **không còn dòng nào bọc seeder**. Đường chạy: `src/BE/PlatformManager.Api/Program.cs:36` (nhận `--seed`) → `:456` (`SeedCommand.RunAsync`) rồi thoát |
| `BootstrapOptions` | `ValidateOnStart()` không điều kiện — API đòi 2 secret ở mọi môi trường | Có điều kiện — `src/BE/Core/PlatformManager.Core.Infrastructure/DependencyInjection.cs:164`; tiến trình API không đòi 2 secret nữa |
| `RolePermissions` | Có `scripts/seed-role-permissions.sql` (từ 2026-08-29) | **Lệnh seed là đường chính** — nó gọi `SeedRolePermissionsAsync()` nên phủ luôn bảng này. Script SQL giữ lại cho trường hợp chỉ có quyền truy cập DB, không chạy được binary |

> 🔄 **LẬT 2026-09-06.** Dòng đầu bảng cũ trích `src/BE/PlatformManager.Api/Program.cs:376`
> làm bằng chứng cho gate `IsDevelopment()`; dòng 376 hôm nay là `TraceId` trong nhánh
> `OnRejected` của rate limiter — **không liên quan gì**. Đây đúng loại lỗi mà cổng
> `check-docs.sh` §6 không bắt được: số dòng vẫn nằm trong file nên vẫn PASS.

### Nghiệm thu

Trên một database **rỗng, đã chạy migration**:

| # | Phép thử | PASS |
|---|---|---|
| 1 | Khởi động API **không** đặt `Bootstrap__*` | App lên bình thường |
| 2 | Chạy lệnh seed | Thoát mã 0; `AspNetRoles` 3 dòng, `AspNetUsers` 2 dòng, `SysMenus` có mục Core |
| 3 | Chạy lệnh seed **lần thứ hai** | Thoát mã 0, không dòng nào nhân đôi |
| 4 | Đăng nhập bằng `Admin` | Vào được, và bị buộc đổi mật khẩu (`MustChangePassword = true`) |

Phép thử 3 kiểm tính idempotent — `CoreSeeder` hôm nay đã kiểm tra tồn tại trước
khi ghi, phép thử này canh cho nó không mất tính đó khi lệnh mới được thêm vào.

### Đã chốt sau đó — không đổi tên tài khoản

Câu hỏi *"đổi tên `SuperAdmin` thành tên khó đoán"* để ngỏ ở bản viết ngày 2026-08-30
đã được **quyết ngày 2026-08-31: GIỮ tên `SuperAdmin`.**

Lựa chọn đó hợp lý nhờ một quyết định cùng đợt: độ dài mật khẩu tối thiểu nâng từ 6 lên
**12 ký tự**, đưa không gian mật khẩu ra ngoài tầm dò vét. Tên đăng nhập biết sẵn chỉ tiết
kiệm cho kẻ tấn công một nửa bài toán, và nửa còn lại vừa thành bất khả thi.

> 📖 Chuỗi ba quyết định (tên biết sẵn + mật khẩu ngắn + miễn khoá) và cách bù:
> đọc [`09-security-beyond-auth.md`](09-security-beyond-auth.md) §"Chính sách mật khẩu"


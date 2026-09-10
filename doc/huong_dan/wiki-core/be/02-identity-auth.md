---
kind: luat
scope: core
verified: 2026-09-06
---

# 2. Xác thực (Identity) khi hệ thống có nhiều Process riêng biệt

## Câu hỏi: 2-3 Process riêng biệt thì Identity + JWT còn hợp lý không?

**Còn hợp lý — với 1 điều chỉnh quan trọng: không để MỖI process tự host riêng 1 bộ ASP.NET Core Identity đầy đủ.**

Nếu mỗi Process đều tự có `IdentityDbContext`/`UserManager`/`SignInManager`/endpoint `/login` riêng → 3 bản sao dữ liệu user, 3 nơi hash password, dễ lệch (đổi password ở Process A không ai biết Process B chưa cập nhật). Đây là lỗi kiến trúc thật, không phải lý thuyết.

## Mô hình đúng — 1 nơi phát hành token, còn lại chỉ xác thực token

Đây chính xác là cách VNR.Successor làm (đã xác nhận qua `architecture.md`): trong 6 Process của họ, chỉ **`VNR.Process.Identity`** (port 5004) sở hữu `UserAccessDbContext` kế thừa `IdentityDbContext` thật + host IdentityServer4 (`/connect/*`). 5 Process còn lại (`MasterData`, `HumanResource`, `Platform`, `Notification`...) **không đụng gì tới Identity package** — chỉ cấu hình `AddAuthentication().AddJwtBearer(...)` trỏ vào public signing key của Process Identity, xác thực token hoàn toàn stateless (không cần gọi DB, không cần gọi Process khác) mỗi request.

```
┌─────────────────────┐        ┌──────────────────────┐   ┌──────────────────────┐
│ Process A (Identity) │        │ Process B (nghiệp vụ)│   │ Process C (nghiệp vụ)│
│ - IdentityDbContext  │        │ - JwtBearer validator │   │ - JwtBearer validator │
│ - /login /refresh    │──JWT──▶│   (chỉ cần public key)│   │   (chỉ cần public key)│
│ - Phát hành JWT (RS256)│      │ - KHÔNG có Identity DB│   │ - KHÔNG có Identity DB│
└──────────────────────┘        └──────────────────────┘   └──────────────────────┘
```

Với `RS256` (khoá bất đối xứng): Process phát hành giữ private key ký token, các Process còn lại chỉ cần public key (qua JWKS endpoint hoặc file cấu hình) để verify — không cần gọi ngược lại Process Identity mỗi request, không tạo phụ thuộc runtime giữa các Process.

## Vậy có cần IdentityServer4/OpenIddict/Duende (authorization server thật) không?

**Không cần, nếu 2-3 Process đó đều là backend của chính bạn** (không phải app/bên thứ 3 độc lập). Chỉ cần **1 process phát hành JWT** bằng chính `SignInManager` của ASP.NET Core Identity, ký RS256, các process khác validate bearer token bình thường — đủ dùng, nhẹ, không cần thêm hạ tầng.

**Nên nâng cấp lên 1 authorization server thật (khuyến nghị: OpenIddict — mã nguồn mở, tích hợp thẳng lên ASP.NET Core Identity sẵn có, không mất phí như Duende) khi:**

| Tình huống | Vì sao cần |
|---|---|
| Có app/mobile của bên thứ 3 cần đăng nhập vào hệ thống bạn | Cần chuẩn OAuth2 (authorization code + PKCE), không tự chế được an toàn |
| Cần trang "cấp quyền" (consent) — app X xin quyền đọc dữ liệu Y | Đây đúng là bài toán OAuth2 scope, JWT tay không có khái niệm này |
| Số lượng client app tăng nhanh, cần thu hồi/theo dõi token tập trung | Authorization server có sẵn token introspection/revocation chuẩn |
| Cần SSO với hệ thống ngoài (AD/LDAP/SAML) | Cần 1 lớp trung gian dịch giao thức |

**Điểm quan trọng: 2 hướng không loại trừ nhau.** Bắt đầu bằng Identity + JWT đơn giản (1 process phát hành), khi thật sự cần OAuth2 chuẩn thì **lắp OpenIddict lên trên chính `UserManager`/`IdentityDbContext` đang có** — không phải viết lại từ đầu, vì OpenIddict được thiết kế để chạy cùng ASP.NET Core Identity chứ không thay thế nó.

## Áp dụng vào PlatformManager

Hiện tại 1 process duy nhất, chưa cần bàn — nhưng nếu sau này tách backend PlatformManager thành ≥2 process (ví dụ 1 process API chính + 1 worker xử lý import nền), áp dụng đúng mô hình trên: process API chính giữ Identity thật, worker chỉ cần validate JWT nếu có gọi API nội bộ (nhiều khả năng worker chạy nền không cần xác thực người dùng, chỉ cần service account riêng).

---

# Vòng đời phiên đăng nhập — chấm dứt phiên khi quyền/trạng thái thay đổi

> Mục này độc lập với phần bàn về nhiều process ở trên. Nó nói về hệ thống **hôm nay**:
> 1 process, **cookie session** của ASP.NET Core Identity (đã CHỐT — KHÔNG JWT, xem
> `doc/contracts/auth.md`). Đọc trước khi đụng vào bất kỳ đường ghi nào chạm role, trạng
> thái khoá, hoặc mật khẩu.

## Cơ chế: vì sao khoá tài khoản không đá được người đang online

Cookie authentication khôi phục danh tính **từ chính cookie**, không tra DB mỗi request. Mỗi
request tới, middleware giải mã cookie `PlatformManager.Auth` rồi dựng lại `ClaimsPrincipal`
từ những gì **đã in sẵn bên trong cookie** — tên đăng nhập, danh sách role, và một con dấu
(xem dưới). Không có câu truy vấn nào xuống `AspNetUsers` để hỏi "người này còn được vào
không".

Ví von: cookie là **thẻ ra vào đã in sẵn** tên và phòng ban. Bảo vệ nhìn thẻ rồi cho qua,
không gọi điện về phòng nhân sự từng lượt. Ghi "đã nghỉ việc" vào sổ nhân sự (`LockoutEnd`
trong DB) **không làm tấm thẻ đang cầm trên tay hết hiệu lực** — nó chỉ chặn lần xin cấp thẻ
mới. Trong code, "xin cấp thẻ mới" chính là `POST /api/auth/login`: chỉ ở đó
`CheckPasswordSignInAsync(..., lockoutOnFailure: true)` mới đọc `LockoutEnd` và trả
`IsLockedOut`.

Cộng thêm cấu hình cookie hiện tại (`Program.cs`, `ConfigureApplicationCookie`):
`ExpireTimeSpan = 14 ngày` + `SlidingExpiration = true` — mỗi lần cookie được dùng lại gần hết
hạn thì nó tự gia hạn. Nghĩa là với người thao tác đều tay, **cookie không bao giờ tự hết
hạn**. Hết hạn theo thời gian không phải cơ chế chấm dứt phiên đáng tin.

### Thứ duy nhất gọi ngược về DB: `SecurityStampValidator`

Identity nhét sẵn vào cookie một claim `AspNet.Identity.SecurityStamp` — bản sao của cột
`AspNetUsers.SecurityStamp` tại thời điểm đăng nhập. Cứ mỗi `ValidationInterval`
(**mặc định 30 phút**), `SecurityStampValidator` chạy trong sự kiện `OnValidatePrincipal` của
cookie middleware và làm đúng 1 việc: đọc `SecurityStamp` **thật trong DB** rồi so với con dấu
in trong cookie.

| Kết quả so sánh | Điều gì xảy ra |
| --- | --- |
| **Khớp** | Dựng lại principal từ DB (`CreateUserPrincipalAsync`) — **role claim được làm mới**, cookie được cấp lại, request đi tiếp bình thường |
| **Lệch** | `RejectPrincipal()` + `SignOutAsync()` — cookie bị xoá, request đó thành **401**, phiên chết |

**Điểm mấu chốt phải nhớ: validator CHỈ so con dấu.** Nó **không** kiểm `LockoutEnd`. Nên nếu
khoá tài khoản mà không đổi con dấu, thì đến cả lúc validator chạy nó vẫn dựng lại principal
và cho đi tiếp — người bị khoá dùng hệ thống bình thường vô thời hạn.

Đây là gốc rễ chung của cả 3 triệu chứng ở bảng dưới. Không phải 3 bug riêng lẻ.

## Hành động quản trị → độ trễ có hiệu lực → cơ chế chịu trách nhiệm

| Hành động | Trước khi áp chính sách | Sau khi áp chính sách | Cơ chế |
| --- | --- | --- | --- |
| **Khoá tài khoản** (`SetLockoutEndDateAsync`) | **Thực tế là không bao giờ** — cookie 14 ngày + sliding, validator không kiểm `LockoutEnd` | **≤ 30 phút** | `SecurityStampValidator` — chỉ khi con dấu bị đổi |
| **Gỡ/đổi role** của user | ≤ 30 phút, nhưng chỉ *làm mới claim* — phiên **sống tiếp** với role mới | ≤ 30 phút, phiên **bị huỷ**, buộc đăng nhập lại | như trên |
| **Sửa ma trận phân quyền** (`SysMenuRole`, `RolePermission`) | **NGAY** | NGAY (không đổi gì) | Đọc thẳng DB mỗi request — `IPermissionChecker` **cố ý không cache** |
| **Đổi mật khẩu** | Người đổi bị đá ra ~30 phút sau **không rõ lý do** (bug, xem §Cạm bẫy) | Phiên hiện tại **giữ nguyên**; các phiên khác của chính người đó bị huỷ ≤ 30 phút | Identity tự đổi con dấu + `RefreshSignInAsync` |
| **Đăng xuất** (`SignOutAsync`) | NGAY, nhưng **chỉ** phiên đang gọi | không đổi | Xoá cookie của chính request đó |
| **Mở khoá** | NGAY cho **lần đăng nhập sau** | không đổi | `LockoutEnd = null`, chỉ đọc lúc login |

**Chú ý sự khác nhau giữa 2 dòng đầu và dòng thứ 3** — đây là chỗ hay bị hiểu nhầm thành "mọi
thứ liên quan tới quyền đều trễ 30 phút":

- **Role của user** nằm **trong cookie** (claim) ⇒ đổi trong DB không có tác dụng tức thì.
- **Ma trận quyền** (`role × ResourceKey`, `role × menu`) nằm **trong DB** và được đọc lại
  mỗi request ⇒ thu hồi quyền của cả một role có hiệu lực **ngay lập tức**. Đây là chủ trương
  đã chốt, xem [`11-performance-caching.md`](11-performance-caching.md) §6.2 quyết định #5 và
  docstring `Core.Application/Permissions/IPermissionChecker.cs`.

## Chính sách đã CHỐT: ngưỡng chấp nhận 30 phút

**Giữ nguyên mặc định `SecurityStampValidatorOptions.ValidationInterval` (30 phút) — KHÔNG cấu
hình lại, KHÔNG sửa `Program.cs`.**

Đây là **lựa chọn có ý thức, không phải chỗ bị bỏ sót.** Người dùng đã cân nhắc và chấp nhận
độ trễ tới **1 tiếng**; mặc định 30 phút của Identity đã tốt hơn ngưỡng đó, nên không có lý do
để đụng vào. Hệ quả tích cực: **không thêm một query DB nào** so với hiện tại —
`SecurityStampValidator` vốn đã chạy sẵn (xem §"Chết âm thầm" bên dưới), việc áp chính sách
này không tạo chi phí runtime mới, nó chỉ **cho validator một thứ để phát hiện**.

⚠️ **Nếu sau này cần nhanh hơn** thì phải trả giá, và phải biết giá đó trước khi đổi:

```csharp
// KHÔNG có dòng này trong Program.cs hôm nay — và đó là ĐÚNG với chính sách hiện tại.
// Chỉ thêm khi có yêu cầu nghiệp vụ thật, kèm số đo.
builder.Services.Configure<SecurityStampValidatorOptions>(
    o => o.ValidationInterval = TimeSpan.FromMinutes(5));
```

- Mỗi chu kỳ validate = **~1 query `AspNetUsers` cho mỗi phiên đang hoạt động**. Hạ từ 30
  xuống 5 phút = gấp **6 lần** số lần validate.
- **Tuyệt đối không đặt `TimeSpan.Zero`** — nghĩa là validate **mọi request**, tức 1 query
  thừa cho **toàn bộ** API. Đó đúng là thứ mà chủ trương "không cache, tối ưu bằng index" ở
  [`11-performance-caching.md`](11-performance-caching.md) đang cố tránh. (Ngoại lệ hợp lệ duy
  nhất: bật `Zero` **trong integration test** để ép validator chạy ngay — xem §Cách chứng minh.)
- Đổi con số này thì phải cập nhật **cả** `doc/contracts/users.md` và `doc/contracts/auth.md`
  — con số 30 phút đã được ghi ra contract cho FE, lệch nhau là contract nói dối.

## Quy tắc bắt buộc: đường ghi nào phải đổi con dấu

> **LUẬT: Mọi đường ghi làm thay đổi *tập role* của một user, hoặc *trạng thái khoá* của tài
> khoản, đều PHẢI gọi `UserManager.UpdateSecurityStampAsync(user)` — và gọi TRƯỚC khi ghi
> thay đổi đó.**

Áp vào các đường ghi **đang có thật** hôm nay:

| Đường ghi | Đổi con dấu? | Ghi chú |
| --- | :---: | --- |
| `UserAdminService.LockAsync` | **CÓ** | Ngay trước `SetLockoutEndDateAsync` |
| `UserAdminService.UpdateAsync` — khi tập role **thực sự đổi** | **CÓ** | Tính `toAdd`/`toRemove` trước, rồi mới quyết định |
| `UserAdminService.UpdateAsync` — chỉ sửa email/fullName | **KHÔNG** | `toAdd`/`toRemove` đều rỗng ⇒ không có quyền nào thay đổi. Đổi con dấu ở đây là **đá người ta ra vì bị sửa tên** — thiệt hại không mua được gì |
| `UserAdminService.UnlockAsync` | **KHÔNG** | Mở khoá đi theo chiều *khôi phục* quyền truy cập, không thu hồi gì. Cùng tinh thần với "`unlock` cố ý không chặn" ở `doc/huong_dan/quy-uoc/be-api-controller.md` |
| `UserAdminService.CreateAsync` | **KHÔNG cần** | User mới chưa có phiên nào; `CreateAsync` đã sinh con dấu mới |
| `UpdatePermissionMatrixCommand` / `UpdateResourcePermissionMatrixCommand` | **KHÔNG** | Ma trận đọc thẳng DB mỗi request ⇒ **đã có hiệu lực NGAY**. Đổi con dấu ở đây còn **sai**: phải quét mọi user thuộc role đó rồi ghi từng dòng, tốn kém, mà không mua thêm gì |
| `IdentityService.ChangePasswordAsync` | *(Identity tự đổi)* | Bắt buộc `RefreshSignInAsync` sau đó — xem §Cạm bẫy |

### Thứ tự: con dấu TRƯỚC, quyền/khoá SAU

Hai lệnh ghi này **không nằm chung 1 transaction** — `UserManager` tự `SaveChanges` mỗi lần
gọi. Nên buộc phải chọn: hỏng ở giữa thì hỏng theo hướng nào.

| Thứ tự | Nếu bước sau lỗi | Đánh giá |
| --- | --- | --- |
| **Con dấu → khoá/role** *(chọn cái này)* | Phiên bị huỷ oan, nhưng quyền còn nguyên. Người dùng chỉ phải đăng nhập lại | **Phiền, không nguy hiểm** |
| Khoá/role → con dấu | Tài khoản đã bị khoá / đã gỡ quyền, nhưng phiên vẫn sống tới 14 ngày | **Chính là bức tranh lỗi đang sửa** |

**Hỏng theo hướng an toàn** ⇒ con dấu luôn đi trước.

> ### ✅ CÓ THẬT (thi công xong, đối chiếu 2026-09-06) — tiền đề "buộc phải chọn" là SAI
>
> Câu mở đầu mục này nói *"Hai lệnh ghi này **không nằm chung 1 transaction** —
> `UserManager` tự `SaveChanges` mỗi lần gọi. Nên buộc phải chọn."*
>
> Vế đầu đúng: `UserManager` **có** tự `SaveChanges`. Vế sau sai: các store của
> Identity dùng **chính** `DbContext` của ứng dụng, nên mở một transaction tường
> minh bao quanh cả cụm là làm được. Không hề "buộc phải chọn" — chỉ là chưa làm.
>
> **Chốt 2026-08-30: bọc cả cụm trong MỘT transaction.** Bảng chọn thứ tự ở trên
> **vẫn giữ nguyên giá trị** — nó là phòng thủ lớp hai cho trường hợp transaction
> không dùng được (vd đường ghi đi qua nhiều `DbContext`), và nó giải thích vì sao
> thứ tự hiện tại là thứ tự đúng. Thay đổi là: hỏng giữa chừng nay **hoàn tác sạch**
> thay vì để lại trạng thái nửa vời cần người đi dọn.
>
> Vì sao đổi: thứ tự chỉ chọn được *hướng* hỏng cho **một** cặp lệnh. Với ba cặp
> trở lên — con dấu → cập nhật → **gỡ role** → **thêm role** — không tồn tại thứ tự
> nào an toàn cả hai chiều. Gỡ trước rồi thêm hỏng ⇒ mất quyền; thêm trước rồi gỡ
> hỏng ⇒ **leo thang quyền**, tệ hơn. Đó là dấu hiệu bài toán cần transaction chứ
> không cần sắp xếp lại.
>
> **🔄 LẬT 2026-09-06 — nhãn `🚧` gỡ, việc đã xong.** Ba đường ghi đều đã bọc transaction
> tường minh quanh cả cụm:
>
> | Đường ghi | Mở transaction | Commit |
> |---|---|---|
> | `UserAdminService.CreateAsync` | `UserAdminService.cs:142` | `:178` |
> | `UserAdminService.UpdateAsync` | `:249` (con dấu ở `:257`) | `:293` |
> | `UserAdminService.LockAsync` | `:315` (con dấu ở `:324`, **trước** `SetLockoutEndDateAsync` ở `:332`) | `:336` |
> | `IdentityService.ChangePasswordAsync` | `:88` | `:119` — `RefreshSignInAsync` (`:134`) **cố ý ngoài** transaction vì nó ghi cookie, không ghi DB |
>
> Ca thật đã đo, xem [`../../../contracts/users.md`](../../../contracts/users.md)
> §`PUT /api/users/{id}`.

Chi tiết dễ vấp khi hiện thực: dùng **cùng một instance `AppUser`** đã lấy ra cho cả hai lệnh
ghi. `UserManager.UpdateAsync` tự làm mới `ConcurrencyStamp` **trên chính instance đó**; nếu
lấy lại một instance cũ (đã đọc từ trước lần ghi thứ nhất) để ghi tiếp thì sẽ ra
`ConcurrencyFailure`.

### Admin tự đổi role của chính mình → phiên của chính họ cũng bị huỷ

**Chấp nhận, KHÔNG làm ngoại lệ.** Một nhánh "nếu user đích là chính người gọi thì bỏ qua
đổi con dấu" tạo đúng loại lỗ hổng mà `SuperAdminAccountGuard` đang bịt — và là loại nhánh
đặc biệt rất dễ bị lợi dụng khi luật phình ra về sau. Người tự đổi quyền của mình thì biết
mình vừa làm gì; phải đăng nhập lại không phải điều bất ngờ với họ.

## Cạm bẫy: `ChangePasswordAsync` TỰ đổi con dấu

`UserManager.ChangePasswordAsync` gọi `UpdateSecurityStampInternal` **bên trong** — đây là
hành vi có sẵn của ASP.NET Core Identity, không phải thứ ai đó thêm vào code này. Về bảo mật
thì đúng: đổi mật khẩu phải giết mọi phiên cũ. Nhưng nó giết **cả phiên đang gọi**.

**Hậu quả nếu không xử lý:** người vừa đổi mật khẩu **thành công**, dùng tiếp bình thường, rồi
**~30 phút sau bị đá ra 401 không rõ lý do**. Cực khó chẩn đoán, vì lỗi không xảy ra tại thời
điểm thao tác — lúc đó mọi thứ trông vẫn ổn.

Nghiêm trọng hơn mức bình thường ở PlatformManager: **mọi** user do Admin tạo đều có
`MustChangePassword = true` (`AppUser.MustChangePassword` mặc định `true`, và
`UserAdminService.CreateAsync` gán tường minh) ⇒ đổi mật khẩu chính là việc **đầu tiên** một
người dùng mới làm. Tức cạm bẫy này bắn trúng gần như 100% người dùng mới.

> **BẮT BUỘC: gọi `SignInManager.RefreshSignInAsync(user)` sau khi đổi mật khẩu thành công.**

- Gọi **sau cùng** — sau cả lệnh ghi `MustChangePassword = false`, để cookie mới dựng lại từ
  trạng thái đã ổn định.
- Nó cấp lại cookie mang **con dấu mới** ⇒ phiên hiện tại sống tiếp.
- **Các phiên khác của chính người đó vẫn mang con dấu cũ ⇒ vẫn bị huỷ trong ≤30 phút.** Đây
  là hành vi **đúng chuẩn bảo mật** — **không** được "sửa" nốt phần này cho tiện.
- `IdentityService` đã inject sẵn `SignInManager<AppUser>` (dùng cho `SignInAsync`/
  `SignOutAsync`) — **không cần thêm phụ thuộc mới**.
- Ràng buộc: `RefreshSignInAsync` **ghi cookie vào response** ⇒ chỉ dùng được trong luồng HTTP
  request và trước khi response bắt đầu gửi. **Không** gọi được từ job nền Hangfire (không có
  `HttpContext` — xem `doc/huong_dan/quy-uoc/be-cqrs-handler.md` §"Command chạy lâu → job nền").

### Các API khác của `UserManager` cũng tự đổi con dấu

Cùng một cạm bẫy, khác cửa vào: `ResetPasswordAsync`, `AddPasswordAsync`,
`RemovePasswordAsync`, `SetEmailAsync`/`ChangeEmailAsync`, `SetPhoneNumberAsync`,
`SetTwoFactorEnabledAsync`... đều tự đổi `SecurityStamp`. Trước khi dùng bất kỳ API
`UserManager` nào cho một thao tác quản trị mới, **đọc source của đúng method đó** thay vì
suy đoán.

⚠️ **Một chỗ dễ "dọn dẹp" thành lỗi:** `UserAdminService.UpdateAsync` hiện gán thẳng
`user.Email = email` rồi `userManager.UpdateAsync(user)` — đường này **không** đổi con dấu.
Nếu ai đó refactor sang `userManager.SetEmailAsync(...)` cho "đúng chuẩn hơn", hành vi đổi âm
thầm: **mọi lần Admin sửa email sẽ đá user đó ra**. Biết trước để không sửa nhầm; nếu vẫn muốn
chuyển thì phải cập nhật cả bảng ở §"Quy tắc bắt buộc" lẫn `doc/contracts/users.md`.

## ⚠️ Cơ chế này CHẾT ÂM THẦM nếu đổi `AddIdentity` → `AddIdentityCore`

`Core.Infrastructure/DependencyInjection.cs` (`AddCoreModule`) đang dùng
**`AddIdentity<AppUser, AppRole>()`** — bản **đầy đủ**. Chính nó là thứ nối
`SecurityStampValidator` vào sự kiện `OnValidatePrincipal` của cookie
(`src/BE/Core/PlatformManager.Core.Infrastructure/DependencyInjection.cs:110`). Không có dòng
nào ở `Program.cs` làm việc này, và cũng **không cần** — nó đã được nối sẵn, đang chạy sẵn.

> **🔄 LẬT 2026-09-06.** Bản trước kết câu bằng *"Nó chỉ đang không phát hiện được gì vì hôm
> nay **không ai đổi con dấu**"*. Không còn đúng: `UserAdminService.LockAsync` (`:324`) và
> `UserAdminService.UpdateAsync` (`:257`) đều gọi `UpdateSecurityStampAsync`, và có 5 test
> integration chứng minh phiên thật sự bị chấm dứt
> (`src/BE/Tests/PlatformManager.Core.IntegrationTests/Auth/SessionTerminationTests.cs`).
> Câu cũ khiến người đọc kết luận cơ chế đang nằm im, tức đúng ngược lại.

`AddIdentityCore<AppUser>()` là một "tối ưu" rất hay được đề xuất cho API không dùng Razor UI
("mình có dùng trang đăng nhập Razor đâu"). Nếu đổi sang nó:

**`SecurityStampValidator` KHÔNG được nối. `OnValidatePrincipal` không còn ai xử lý. Toàn bộ
mục này ngừng hoạt động — không có lỗi biên dịch, không có test đỏ, không có dòng log nào.**
Khoá tài khoản lập tức quay về "không bao giờ có hiệu lực", và không có gì báo cho ai biết.

Nếu vì lý do nào đó **buộc** phải chuyển sang `AddIdentityCore`, phải tự nối lại đủ 3 thứ:

```csharp
services.AddIdentityCore<AppUser>(...)
    .AddRoles<AppRole>()                       // role claim — thiếu là RequirePermissionFilter mù
    .AddEntityFrameworkStores<PlatformManagerDbContext>()
    .AddSignInManager()                        // RefreshSignInAsync/SignInAsync
    .AddDefaultTokenProviders();

services.AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddIdentityCookies();                     // ← chính chỗ này nối SecurityStampValidator
```

…và **có test chứng minh phiên bị huỷ sau khi đổi con dấu**. Đừng tin là nó vẫn chạy.

## Cách chứng minh nó hoạt động thật — ✅ ĐÃ CÓ TEST (đối chiếu 2026-09-06)

Không chứng minh được bằng unit test — validator sống ở tầng cookie middleware, không phải ở
handler. Cách rẻ nhất là integration test qua `WebApplicationFactory` (xem
[`04-testing-strategy.md`](04-testing-strategy.md)). **Bộ test đó đã tồn tại:**

| File | Vai trò |
|---|---|
| `src/BE/Tests/PlatformManager.Core.IntegrationTests/Auth/SessionTerminationFactory.cs:29` | Ép `ValidationInterval = TimeSpan.Zero` — **thay đổi duy nhất** so với production |
| `src/BE/Tests/PlatformManager.Core.IntegrationTests/Auth/SessionTerminationTests.cs` | 5 ca, phủ **cả hai chiều** của bảng ở §"Quy tắc bắt buộc": khoá → 401, đổi role → 401, **chỉ sửa email/fullName → KHÔNG ảnh hưởng**, đổi mật khẩu → phiên hiện tại sống/phiên khác chết, mở khoá → không đổi con dấu |

Ba ca "KHÔNG bị ảnh hưởng" mới là phần đắt giá: chúng canh chiều ngược, tức chặn việc ai đó
"cho chắc" bằng cách đổi con dấu ở mọi đường ghi.

Các bước dưới đây giữ lại làm **khuôn cho seam khác** (xem
[`04-testing-strategy.md`](04-testing-strategy.md) §"Seam activation test"):

1. Trong cấu hình của **riêng test**, đặt
   `SecurityStampValidatorOptions.ValidationInterval = TimeSpan.Zero` để ép validator chạy mọi
   request (đây là ngoại lệ hợp lệ duy nhất của `Zero`, xem §Chính sách).
2. Đăng nhập → gọi 1 endpoint `[Authorize]` → phải **200**.
3. Gọi `POST /api/users/{id}/lock` cho chính user đó.
4. Gọi lại endpoint ở bước 2 bằng **cùng cookie** → phải **401**.

Bước 4 mới là thứ chứng minh chính sách này hoạt động. Test chỉ khẳng định "`LockAsync` trả
`true`" **không chứng minh được gì** — đó đúng là trạng thái đã xanh trong suốt lúc lỗi còn
tồn tại.

Đó cũng là lý do **không hardcode `ValidationInterval`** rải rác trong code sản phẩm: test cần
override được nó qua cấu hình.

---

# CSRF — lỗ hổng đặc thù của cookie auth, JWT không có

> Bổ sung 2026-08-24, sau khi rà nội dung `wiki-core` đối chiếu thực hành bảo
> mật chuẩn ngành cho hệ thống tầm trung: toàn bộ tài liệu trước đó **không
> có một dòng nào** về CSRF/anti-forgery, dù hệ thống đã CHỐT cookie session
> — đây là lỗ hổng OWASP xếp ngang hàng broken access control, và **chỉ tồn
> tại vì chọn cookie**. Đọc mục này cùng lúc với "Vòng đời phiên đăng nhập" ở
> trên, không tách rời.

## Vì sao JWT không có vấn đề này mà cookie có

JWT (khi FE tự gắn `Authorization: Bearer <token>` bằng JavaScript) **không
tự động gửi kèm** khi 1 trang web khác gọi API của bạn — trình duyệt không
biết token nằm ở đâu để tự đính kèm. Cookie thì ngược lại: **trình duyệt tự
động gắn cookie vào MỌI request tới đúng domain, bất kể request đó khởi phát
từ đâu.**

Kịch bản tấn công cụ thể: nạn nhân đã đăng nhập PlatformManager (cookie còn
hiệu lực), sau đó mở 1 trang độc hại ở tab khác. Trang đó chứa:

```html
<form action="https://platformmanager.example.com/api/users/abc/lock" method="POST"></form>
<script>document.forms[0].submit()</script>
```

Trình duyệt gửi request tới domain thật của bạn **kèm cookie session thật**
của nạn nhân — server thấy 1 request hợp lệ, đã đăng nhập, đúng quyền. Nạn
nhân không hề biết mình vừa khoá tài khoản của ai đó (hoặc tệ hơn, tự khoá
chính mình nếu là SuperAdmin — xem liên hệ với `SuperAdminAccountGuard` ở
dưới).

## Vì sao `[ValidateAntiForgeryToken]` kinh điển KHÔNG áp dụng thẳng được

Filter `[ValidateAntiForgeryToken]` của ASP.NET Core MVC được thiết kế cho
**Razor form** — token được render sẵn vào `<form>` lúc server trả HTML. Hệ
thống này là **SPA Angular gọi API JSON**, không có form server-render nào —
áp thẳng filter đó vào controller sẽ đòi hỏi FE gửi 1 token nó chưa từng
nhận được. Đây là lỗi thường gặp nhất khi mới đụng vào CSRF: nhầm "ASP.NET
Core có sẵn CSRF protection" với "tôi không cần làm gì thêm".

## Phòng thủ 2 lớp — bắt buộc cả hai, không chọn một

> ### ✅ Hai lớp, nhưng Lớp 1 KHÔNG phải `SameSite` (đối chiếu source 2026-09-06)
>
> Tiêu đề mục này và mẫu code "Lớp 1" ngay dưới giả định `SameSite=Strict`. Cookie phiên
> của dự án này khai `SameSiteMode.None`
> (`src/BE/PlatformManager.Api/Program.cs:432`; cookie antiforgery cũng vậy, `:487`), và nó
> **buộc phải là `None`**: FE nằm khác origin
> ([`../fe/17-phuc-vu-va-trien-khai.md`](../fe/17-phuc-vu-va-trien-khai.md) §2), cookie
> `Strict`/`Lax` sẽ không được gửi kèm và không ai đăng nhập được.
>
> `SameSite=None` **không phải một lớp yếu hơn — nó là không có lớp nào**. Nên chỗ của
> Lớp 1 đã được thay bằng **kiểm header `Origin`**, xem mục ngay dưới. Hai lớp thật sự tồn
> tại hôm nay:
>
> | | Cơ chế thật | Ở đâu |
> |---|---|---|
> | Lớp 1 | Mọi request ghi phải mang `Origin` trong allowlist, không thì 403 | `src/BE/PlatformManager.Api/Common/OriginValidationMiddleware.cs`, nối pipeline ở `Program.cs:587` |
> | Lớp 2 | Token antiforgery (`X-XSRF-TOKEN`) | `Program.cs:484` trở đi |
>
> > **🔄 LẬT 2026-09-06.** Bản trước tuyên bố *"hệ thống này đang chạy trên MỘT lớp"* và
> > dẫn hai bằng chứng, **cả hai đều sai ở thời điểm này**:
> > 1. `Program.cs:218` cho `SameSiteMode.None` — dòng đó, khi đo 2026-09-06, là
> >    `.Bind(builder.Configuration.GetSection(CorsPolicyOptions.SectionName))`, không liên
> >    quan cookie. Dòng đúng là `:432` (đo lại 2026-09-10).
> > 2. *"Chú thích ở `Program.cs:252` vẫn nói Lớp 1 là SameSite"* — chú thích đó **đã được
> >    sửa**, và nay nói **ngược lại** (`Program.cs:464-468`: *"chú thích cũ nói … SAI …
> >    Lớp 1 thật là kiểm header Origin"*). `:252` không trỏ vào chú thích nào — khi đo
> >    2026-09-06 nó là `const int GlobalSegmentsPerWindow = 6;`.
> >
> > Đây là dạng sai nguy hiểm nhất của tài liệu bảo mật: nó **báo động cho một lỗ hổng đã
> > vá**, nên người đọc hoặc đi vá lại thứ đã vá, hoặc học cách bỏ qua cảnh báo của file này.

### ✅ CÓ THẬT — kiểm header `Origin` thay cho Lớp 1 (chốt 2026-08-31, đối chiếu 2026-09-06)

`SameSite` không dùng được ở cross-origin, nhưng có một cơ chế khác dùng được:
**mọi request ghi (`POST`/`PUT`/`PATCH`/`DELETE`) phải mang header `Origin` nằm
trong allowlist, không thì 403.**

Vì sao nó là một lớp thật chứ không phải trang trí:

| | |
|---|---|
| Trình duyệt **luôn** gắn `Origin` cho request cross-site | Cam kết của trình duyệt, không phải quy ước tự nguyện |
| JavaScript **không** đặt hay sửa được `Origin` | Nằm trong danh sách header cấm ghi của Fetch |
| Độc lập hoàn toàn với cơ chế token | Hỏng cái này không kéo theo cái kia — đúng định nghĩa phòng thủ nhiều lớp |

Đây là khuyến nghị của OWASP cho đúng hình dạng SPA khác origin dùng cookie. Nó bắt
được **cả hai** kịch bản sai sót nêu ở khung trên.

#### Trạng thái thi công — đối chiếu source 2026-09-06

| | Trước (2026-08-31) | ✅ Hôm nay |
|---|---|---|
| Lớp 1 | `SameSite=None` ⇒ không tồn tại | `OriginValidationMiddleware` — 403 cho request ghi có `Origin` ngoài allowlist (`OriginValidationMiddleware.cs:47`), nối pipeline ở `Program.cs:587` |
| Lớp 2 | Token antiforgery, hoạt động đúng | Không đổi |
| Chú thích trong `Program.cs` | Nói Lớp 1 là `SameSite` | Đã sửa — `Program.cs:464-468` nói đúng, kèm lý do `SameSite` không dùng được |
| Mẫu code Lớp 1 trong file này | `SameSiteMode.Strict` | Giữ làm mẫu cho ca **cùng origin**; dự án này **không** thuộc ca đó — xem cảnh báo ngay dưới khối code |

So khớp origin dùng `string.Equals(..., OrdinalIgnoreCase)`, **không** so theo tiền tố
(`OriginValidationMiddleware.cs:88`) — `https://app.example.com.evil.net` bắt đầu bằng một
origin hợp lệ nhưng là site hoàn toàn khác. Test chốt:
`src/BE/Tests/PlatformManager.Core.IntegrationTests/Csrf/CsrfSeamTests.cs`.

#### Nghiệm thu

| # | Phép thử | PASS |
|---|---|---|
| 1 | `POST` request ghi với `Origin` đúng | 200 |
| 2 | `POST` với `Origin` là một domain lạ, token antiforgery **hợp lệ** | **403** — chứng minh lớp mới độc lập với token |
| 3 | `POST` **không** có `Origin` (vd gọi bằng `curl`) | Theo quyết định thi công — xem ghi chú dưới |
| 4 | Đăng nhập bình thường từ FE thật | Không ảnh hưởng gì |

Phép thử 2 là phép thử quyết định: token hợp lệ **mà vẫn bị chặn** mới chứng minh
đây là lớp thứ hai thật, không phải cùng một lớp viết hai lần.

Phép thử 3 là chỗ phải chọn có chủ đích, và **đã chọn khi thi công: CHO QUA**
(`OriginValidationMiddleware.cs:78-81`, ba lý do ghi ngay tại chỗ ở `:62-77`). Tóm tắt:
request không đi từ trình duyệt (script, công cụ tích hợp, kiểm thử) không có `Origin`;
kẻ tấn công **không** dùng được đường này vì trình duyệt không cho phép bỏ `Origin` trong
kịch bản CSRF; và **Lớp 2 vẫn áp đầy đủ** cho đúng những request đó, nên chúng không hề đi
vào hệ thống mà không qua kiểm tra nào.

Đổi hướng khi nào: nếu có yêu cầu "chỉ trình duyệt được ghi dữ liệu", đổi nhánh đó thành
403 và cấp cho script một đường xác thực riêng (API key/service account) — **không** nới
lỏng Lớp 2 để bù.

> *(Sửa 2026-09-06: bản trước để mục này ở dạng câu hỏi mở *"Chốt khi thi công"*, trong khi
> lựa chọn đã được chốt và ghi rõ trong code từ 2026-08-31. Để ngỏ một quyết định đã chốt
> mời gọi người sau chốt lại theo hướng khác.)*

### Lớp 1 — `SameSite` cookie attribute

```csharp
// Program.cs, ConfigureApplicationCookie — cùng chỗ đã cấu hình ExpireTimeSpan/SlidingExpiration
options.Cookie.SameSite = SameSiteMode.Strict;   // xem ngoại lệ dev bên dưới
options.Cookie.HttpOnly = true;                  // JS không đọc được cookie — chặn XSS đánh cắp cookie
options.Cookie.SecurePolicy = CookieSecurePolicy.Always;  // chỉ gửi qua HTTPS
```

`SameSite=Strict` chặn trình duyệt gửi cookie khi request khởi phát từ site
khác — vô hiệu hoá kịch bản tấn công ở trên hoàn toàn cho phần lớn trường
hợp. **Nhưng đây không phải lưới đủ một mình:**

- Một số trình duyệt cũ/cấu hình lạ không tôn trọng `SameSite` đúng chuẩn.
- Nếu FE và BE **khác origin thật sự** (không chỉ khác port lúc dev mà khác
  domain lúc production — vd `app.example.com` gọi `api.example.com`),
  `Strict` chặn luôn cả request hợp lệ. `Lax` nới hơn nhưng vẫn cho qua GET
  điều hướng top-level — không đủ mạnh một mình cho request ghi dữ liệu.

### Lớp 2 — custom header bắt buộc trên mọi request ghi

CSRF tấn công qua `<form>` hoặc request đơn giản **không thể tự thêm custom
HTTP header** (bị chặn bởi CORS preflight nếu header không nằm trong danh
sách "safe-listed"). Vì vậy: **mọi request `POST`/`PUT`/`PATCH`/`DELETE`
phải bắt buộc có 1 header tuỳ ý mà kẻ tấn công không đặt được**, ví dụ chuẩn
Angular `HttpClient` đã hỗ trợ sẵn (`XSRF-TOKEN` cookie + header
`X-XSRF-TOKEN`, cấu hình qua `withXsrfConfiguration()`), hoặc đơn giản hơn —
dùng `IAntiforgery` của ASP.NET Core theo mô hình SPA (không phải mô hình
Razor form):

```csharp
// Program.cs — cookie NỘI BỘ của AddAntiforgery giữ nguyên tên mặc định và HttpOnly=true.
// KHÔNG đổi tên nó thành "XSRF-TOKEN": xem bẫy "tokens swapped" ngay dưới khối này.
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.HttpOnly = true;                            // cookie nội bộ — JS không cần đọc
    options.Cookie.SameSite = SameSiteMode.None;               // FE khác origin, vẫn phải gửi được
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.HeaderName = "X-XSRF-TOKEN";                       // FE gửi REQUEST-TOKEN qua header này
});

// 1 endpoint nhỏ để FE lấy token lúc load app. Nó phải TỰ TAY set MỘT cookie RIÊNG chứa
// REQUEST-TOKEN — đây là bước dễ bỏ sót nhất, và bỏ sót thì Angular không có gì để đọc.
app.MapGet("/api/antiforgery/token", (IAntiforgery antiforgery, HttpContext ctx) =>
{
    var tokens = antiforgery.GetAndStoreTokens(ctx);

    ctx.Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken!, new CookieOptions
    {
        HttpOnly = false,               // Angular PHẢI đọc được bằng JS — khác cookie nội bộ ở trên
        SameSite = SameSiteMode.None,
        Secure = true,
    });

    return Results.Ok(new { token = tokens.RequestToken });
}).DisableRateLimiting();               // bước "lấy token" không nên ăn slot của GlobalLimiter

// Middleware validate — CHỈ áp cho method ghi, GET không cần
app.Use(async (ctx, next) =>
{
    if (HttpMethods.IsPost(ctx.Request.Method) || HttpMethods.IsPut(ctx.Request.Method) ||
        HttpMethods.IsDelete(ctx.Request.Method) || HttpMethods.IsPatch(ctx.Request.Method))
    {
        await ctx.RequestServices.GetRequiredService<IAntiforgery>().ValidateRequestAsync(ctx);
    }
    await next();
});
```

> ### ⚠️ Bẫy "tokens swapped" — đã dính một lần, đừng dính lại
>
> Double-submit-cookie có **hai** giá trị khác nhau: **COOKIE-TOKEN** (bí mật server giữ, nằm
> trong cookie nội bộ của `AddAntiforgery`) và **REQUEST-TOKEN** (thứ FE echo vào header).
> Đặt `options.Cookie.Name = "XSRF-TOKEN"` khiến cookie **nội bộ** mang tên mà Angular đang
> tìm, nên Angular echo nhầm cookie-token vào header ⇒ `ValidateRequestAsync` ném
> `AntiforgeryValidationException` *"the cookie token and the request token were swapped"* —
> **khoá mọi request ghi thật từ trình duyệt, kể cả `POST /api/auth/login`**. Ca này xảy ra
> thật và đã sửa 2026-08-24; lời giải thích đầy đủ nằm ngay trong
> `src/BE/PlatformManager.Api/Program.cs:474-483`.

> **🔄 LẬT 2026-09-06 — khối mẫu cũ có hai lỗi, một lỗi không biên dịch được:**
> 1. `HttpMethods.IsDeleted(...)` — **không tồn tại**. Tên đúng là `HttpMethods.IsDelete`
>    (code thật: `Program.cs:630`).
> 2. Endpoint mẫu chỉ `return Results.Ok(new { token = ... })` và **không set cookie
>    `XSRF-TOKEN`**. Ai chép mẫu về sẽ có một endpoint trả token mà Angular
>    `HttpXsrfInterceptor` không bao giờ đọc tới, nên **mọi** request ghi bị 403 — hỏng ở
>    runtime, không ở biên dịch. Đây đúng nửa còn lại của bẫy "tokens swapped": bản mẫu cũ
>    tránh được nửa đặt-sai-tên nhưng bỏ mất nửa phải-set-cookie-riêng.

Kẻ tấn công gửi form CSRF **không biết token** (không đọc được cookie/state
của nạn nhân từ site khác) nên request bị `ValidateRequestAsync` từ chối
trước khi chạm tới `[RequirePermission]`/`[Authorize]`.

## Liên hệ trực tiếp với rủi ro đã biết trong hệ thống này

`SuperAdminAccountGuard` được dựng để chặn chính người dùng tự khoá/tự gỡ
quyền `SuperAdmin` của mình **qua API hợp lệ**. CSRF là đường tấn công khiến
một request "hợp lệ" đó **không thực sự do người dùng chủ ý gửi** — 2 lớp
phòng thủ này bảo vệ đúng lớp mà `SuperAdminAccountGuard` không chạm tới
(guard kiểm *nội dung* request, CSRF-protection kiểm *nguồn gốc* request).
Thiếu CSRF-protection thì `SuperAdminAccountGuard` vẫn đúng nhưng dễ bị đánh
lừa để tự kích hoạt bởi chính nạn nhân.

## CORS — điều kiện đi kèm bắt buộc, không phải chi tiết vặt

`AllowCredentials()` (bắt buộc để cookie gửi được từ Angular qua
`withCredentials`) **không được đi cùng** `AllowAnyOrigin()` — trình duyệt
tự chặn tổ hợp này, nhưng nếu lỡ cấu hình origin động kiểu
`SetIsOriginAllowed(_ => true)` thì coi như tự vô hiệu hoá toàn bộ 2 lớp
phòng thủ CSRF ở trên (bất kỳ origin nào cũng coi là hợp lệ). Danh sách
origin cho phép phải là **danh sách tường minh, hữu hạn** (domain FE thật +
localhost dev), không suy luận động.

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
`SecurityStampValidator` vào sự kiện `OnValidatePrincipal` của cookie. Không có dòng nào ở
`Program.cs` làm việc này, và cũng **không cần** — nó đã được nối sẵn, đang chạy sẵn. Nó chỉ
đang không phát hiện được gì vì hôm nay **không ai đổi con dấu**.

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

## Cách chứng minh nó hoạt động thật

Không chứng minh được bằng unit test — validator sống ở tầng cookie middleware, không phải ở
handler. Cách rẻ nhất là integration test qua `WebApplicationFactory` (xem
[`04-testing-strategy.md`](04-testing-strategy.md)):

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

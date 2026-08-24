# API Contract Card — Auth

**Status: IMPLEMENTED** (2026-08-16) — build xanh, đã gọi thử qua curl (xem ví dụ response
thật bên dưới). Cơ chế: **cookie session** (ASP.NET Core Identity, KHÔNG JWT) — đã CHỐT.

> Ví dụ response thành công (login/me trả `Data` thật) **chưa capture được** vì môi trường
> build hiện tại không được phép tự áp schema DB thật lên Postgres (xem
> `doc/ke-hoach-xay-lai-corebase.md` gotcha #6) — người dùng cần tự chạy tay
> `doc/cau-truc-database.sql` (DDL viết tay) + `dotnet ef database update` trước, sau đó `frontend-expert`/người dùng có thể
> gọi thử luồng thành công đầy đủ. Các ví dụ lỗi (401/400/500) bên dưới đã verify **thật** (2026-08-16) với
> app chạy thật (không phải suy đoán).

## Envelope chung

Mọi response đi qua `IApiResult<T>` — camelCase: `data,message,status,code,businessCode,
traceId,retryable,fields`. Riêng `fields` (lỗi validate theo field) giữ nguyên **PascalCase**
key (khớp tên property C#, vd `"UserName"`) — xem `wiki-core/fe/02-http-envelope.md`.

## `POST /api/auth/login`

Request (`[FromBody]`, phẳng):

```json
{ "userName": "SuperAdmin", "password": "<Bootstrap:SuperAdminPassword>" }
```

⚠️ **Cập nhật 2026-08-24: KHÔNG còn mật khẩu hardcode `"SuperAdmin@123"`.** `CoreSeeder` seed
**2 tài khoản bootstrap RIÊNG BIỆT** — `SuperAdmin` (role `SuperAdmin` DUY NHẤT) và `Admin` (role
`Admin` DUY NHẤT) — mật khẩu đọc từ `BootstrapOptions` (User Secrets `Bootstrap:SuperAdminPassword`
/ `Bootstrap:AdminPassword` lúc dev, biến môi trường `Bootstrap__SuperAdminPassword` /
`Bootstrap__AdminPassword` lúc production; fail-fast — app không khởi động được nếu thiếu). Xem
`scripts/setup-database.sh` bước 5/5 và `src/BE/Core/PlatformManager.Core.Infrastructure/Persistence/BootstrapOptions.cs`.

Response thành công — `Data: CurrentUserInfo`:

```json
{
  "id": "guid", "userName": "SuperAdmin", "email": "superadmin@platformmanager.local",
  "fullName": "Quản trị viên hệ thống", "roles": ["SuperAdmin"], "mustChangePassword": true
}
```

(Trước 2026-08-24: `roles` là `["SuperAdmin", "Admin"]` — 1 tài khoản gộp cả 2 role. Đã tách
làm 2 tài khoản như trên; đăng nhập bằng `Admin` trả `"roles": ["Admin"]`.)

Lỗi:
| BusinessCode | HTTP | Khi nào |
| --- | --- | --- |
| `AUTH.INVALID_CREDENTIALS` | 422 | Sai user/password |
| `AUTH.LOCKED_OUT` | 422 | Tài khoản đang bị khoá |
| `RATE_LIMIT.TOO_MANY_REQUESTS` | **429** | Quá **5 lượt/phút TỪ CÙNG MỘT IP** (hoặc chạm hạn mức chung 100/phút/IP) — xem mục riêng bên dưới |
| (validation) | 400 | UserName/Password rỗng — đã verify thật 2026-08-16: |

```
$ curl -X POST /api/auth/login -d '{"UserName":"","Password":""}'
HTTP/1.1 400 Bad Request
{"message":"Dữ liệu không hợp lệ.","status":"VALIDATION_ERROR","code":"ValidationError",
 "traceId":"...","fields":{"UserName":["'User Name' must not be empty."],
 "Password":["'Password' must not be empty."]}}
```

### 🚦 429 Too Many Requests — rate limit (cập nhật 2026-08-21, lần 2)

**HAI tầng giới hạn, CỘNG DỒN — không phải một.** Rất dễ đọc nhầm, nên ghi rõ:

| Tầng | Hạn mức | Áp cho |
| --- | --- | --- |
| `GlobalLimiter` | **100 request/phút/IP** | **MỌI** endpoint, kể cả endpoint không khai gì |
| Policy `"login"` | **5 request/phút/IP** | Riêng `POST /api/auth/login` |

`POST /api/auth/login` tiêu **cả hai** limiter cho mỗi lượt. Mốc chặt hơn luôn chạm trước nên
hành vi thấy được vẫn là "429 ở lượt thứ 6", nhưng 5 lượt login đó **có** ăn vào hạn mức 100
chung của cùng IP — FE gọi nhiều API sau khi đăng nhập cần biết điều này.

**Điểm MỚI so với bản trước:** trước 2026-08-21 chỉ mỗi login có giới hạn; mọi API khác **không
có giới hạn nào** (policy `"default"` đã khai nhưng không endpoint nào gắn). Nay `GlobalLimiter`
phủ toàn bộ ⇒ **bất kỳ endpoint nào** cũng có thể trả 429, không riêng màn đăng nhập.

**Ngoại lệ — KHÔNG bao giờ trả 429:** `/health` và `/hangfire` (monitoring + dashboard quản trị).

#### ✅ 429 nay có ĐÚNG envelope + `Retry-After` (đổi shape — FE cần cập nhật)

Bản trước ghi *"429 KHÔNG có envelope — body RỖNG HOÀN TOÀN"* và để ngỏ câu hỏi có nên bọc
envelope hay không. **Người dùng đã chốt: bọc.** Đã triển khai qua
`RateLimiterOptions.OnRejected`, có integration test khẳng định từng field.

```
HTTP/1.1 429 Too Many Requests
Content-Type: application/json; charset=utf-8
Retry-After: 47
```
```json
{
  "message": "Bạn thao tác quá nhanh. Vui lòng thử lại sau 47 giây.",
  "status": "BUSINESS_ERROR",
  "code": "TooManyRequests",
  "businessCode": "RATE_LIMIT.TOO_MANY_REQUESTS",
  "traceId": "0HN...",
  "retryable": true
}
```

- `data` và `fields` **vắng mặt** (quy ước `WhenWritingNull` chung của toàn hệ thống) — giống
  mọi response lỗi khác, FE không cần nhánh xử lý riêng.
- `Retry-After` (giây) lấy **từ metadata của limiter**, không hardcode — nếu cửa sổ đổi thì giá
  trị tự đi theo. Dùng chính con số này cho đồng hồ đếm ngược, đừng giả định 60.
- `retryable: true` — đây là mã lỗi đầu tiên field này mang nghĩa thật: chờ hết rồi gọi lại là
  xong, người dùng không phải sửa gì.

#### 🔴 Việc FE cần làm (`frontend-expert`)

1. **`ApiErrorCode` trong `src/FE/src/app/core/http/api-result.model.ts` phải thêm
   `'TooManyRequests'`.** Union hiện liệt kê đúng 8 giá trị và **thiếu** giá trị này ⇒ response
   429 thật sẽ không khớp kiểu. BE **không** sửa file FE (ngoài phạm vi) — cần FE tự thêm.
2. **Gỡ nhánh xử lý đặc biệt cho 429** trong `httpErrorInterceptor` nếu có: trước đây interceptor
   bị dặn *không được* parse body 429 (vì rỗng). Nay 429 parse được như mọi lỗi khác, và
   `businessCode = "RATE_LIMIT.TOO_MANY_REQUESTS"` là thứ nên bind.
3. **429 không còn là chuyện riêng của màn đăng nhập** — bất kỳ màn nào cũng có thể gặp. Thông
   báo chung nên đọc `message` từ envelope (đã có sẵn số giây) thay vì tự soạn.
4. Đừng nhầm 429 với `AUTH.LOCKED_OUT` (422): 422 là **tài khoản** bị khoá (cần admin mở), 429 là
   **IP** đang bị siết (tự hết sau ≤ 1 phút).

BE tham chiếu: `src/BE/PlatformManager.Api/Program.cs` (`ResolveRateLimitPartitionKey`,
`AddRateLimiter` + `GlobalLimiter` + `OnRejected`), quy tắc ở
`doc/huong_dan/quy-uoc/be-api-controller.md` §"Rate limiting", test chốt
`src/BE/Tests/PlatformManager.Core.IntegrationTests/RateLimiting/` (`LoginRateLimitPartitionTests`
+ `GlobalRateLimitTests`).

## 🛡️ CSRF — `GET /api/antiforgery/token` (mới, 2026-08-24)

Cookie session (không phải JWT) ⇒ cần chống CSRF — xem
`doc/huong_dan/wiki-core/be/02-identity-auth.md` §CSRF cho lý do đầy đủ. Áp dụng **Lớp 2** (Lớp 1
là `SameSite` đã có sẵn ở cookie phiên): mọi request `POST`/`PUT`/`PATCH`/`DELETE` **bắt buộc**
mang header `X-XSRF-TOKEN`, kể cả `POST /api/auth/login` — CSRF áp theo METHOD, không có ngoại lệ
theo endpoint.

**Tên cookie/header — CHỌN ĐÚNG mặc định của Angular `HttpClient`, không cần tham số tuỳ biến:**

| | Giá trị |
| --- | --- |
| Cookie (server phát, KHÔNG `HttpOnly` — JS phải đọc được) | `XSRF-TOKEN` |
| Header (client tự gắn lại) | `X-XSRF-TOKEN` |
| Endpoint phát hành | `GET /api/antiforgery/token` (KHÔNG cần đăng nhập, KHÔNG bị rate-limit) |

Response `GET /api/antiforgery/token` **KHÔNG bọc `IApiResult`** (theo đúng mẫu SPA nêu ở
`02-identity-auth.md`, khác mọi endpoint khác của contract này):

```json
{ "token": "CfDJ8..." }
```

⚠️ **Sửa 2026-08-24 (core-reviewer phát hiện lỗi thật, đã chặn được TRƯỚC khi lên môi trường
dùng chung):** `IAntiforgery` của ASP.NET Core có **2 nửa khác nhau** của cùng cơ chế
double-submit-cookie — **cookie-token** (bí mật, server tự quản trong 1 cookie riêng, `HttpOnly`)
và **request-token** (giá trị phải gửi lại qua header). Bản đầu đặt nhầm tên cookie nội bộ của
`AddAntiforgery` thành `"XSRF-TOKEN"`, khiến chính cookie-token bị lộ ra đúng cái tên Angular tự
đọc — Angular echo nhầm cookie-token vào header thay vì request-token, và **mọi request ghi từ
trình duyệt thật bị 403**, kể cả `POST /api/auth/login`. Đã sửa: `AddAntiforgery` giữ nguyên tên
cookie nội bộ (không đổi, vẫn `HttpOnly`), còn endpoint `GET /api/antiforgery/token` **tự tay**
`Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken, ...)` — set MỘT cookie khác, đúng
chứa request-token, cho Angular đọc.

Gọi endpoint này đồng thời set cookie `XSRF-TOKEN` (chứa request-token, như trên) — bản thân body
JSON **không cần FE tự đọc** nếu dùng `withXsrfConfiguration()` mặc định của Angular (đọc thẳng
cookie, tự gắn header cho mọi request ghi tới cùng origin). Chỉ cần gọi 1 lần lúc app khởi động để
cookie tồn tại.

⚠️ **Cạm bẫy khác đã gặp khi build seam activation test (xem
`Tests/PlatformManager.Core.IntegrationTests/Csrf/CsrfSeamTests.cs`): token phát hành lúc
ANONYMOUS không dùng lại được cho request ghi SAU khi đã đăng nhập** — `IAntiforgery` của
ASP.NET Core còn gắn request-token với danh tính (`ClaimsPrincipal`) tại thời điểm phát hành; đổi
danh tính (login) rồi dùng token cũ sẽ bị từ chối `403` với thông điệp "meant for a different
claims-based user". **Cookie `XSRF-TOKEN` KHÔNG tự đổi giá trị theo thời gian hay theo sự kiện
login** — nó chỉ đổi khi có ai đó gọi lại `GET /api/antiforgery/token` (endpoint duy nhất set
cookie này, không có cơ chế tự động nào khác). Với Angular `withXsrfConfiguration()`, việc đọc lại
cookie ở MỖI request là tự động đúng — **miễn là FE tự gọi lại `GET /api/antiforgery/token` ngay
sau khi login thành công** để cookie mang giá trị mới khớp danh tính vừa đăng nhập; không tự gọi
lại thì cookie vẫn giữ token cũ (lúc anonymous) và request ghi đầu tiên sau login sẽ 403. Nêu ra ở
đây để `frontend-expert` biết: **không chỉ gọi 1 lần lúc app khởi động rồi coi là xong** — phải
gọi lại đúng lúc đăng nhập thành công.

Lỗi khi thiếu/sai token:

```
HTTP/1.1 403 Forbidden
{"message":"Yêu cầu bị từ chối — thiếu hoặc sai token chống giả mạo (CSRF).",
 "status":"BUSINESS_ERROR","code":"AuthorizationError","traceId":"..."}
```

Không có `businessCode` riêng (không phải lỗi nghiệp vụ) — FE nhận diện qua `code =
"AuthorizationError"` kèm `status = "BUSINESS_ERROR"`, cùng dạng với lỗi 403 phân quyền khác;
message khác biệt là điểm phân biệt duy nhất nếu cần hiển thị riêng.

**🔴 Việc FE cần làm (`frontend-expert`):**

1. Bật `withXsrfConfiguration()` (mặc định `cookieName: 'XSRF-TOKEN'`, `headerName:
   'X-XSRF-TOKEN'` — khớp thẳng, không cần tham số) trong `provideHttpClient(...)`.
2. Gọi `GET /api/antiforgery/token` lúc app khởi động (trước hoặc cùng lúc probe
   `GET /api/auth/me` hiện có) để cookie `XSRF-TOKEN` tồn tại trước request ghi đầu tiên
   (kể cả `POST /api/auth/login`).
3. **Gọi LẠI `GET /api/antiforgery/token` ngay sau khi `POST /api/auth/login` thành công** —
   token lúc anonymous không dùng được cho request ghi sau khi đã đăng nhập (xem cạm bẫy ở trên).
   Thiếu bước này: mọi request ghi ĐẦU TIÊN sau login (đổi mật khẩu, tạo/sửa user...) sẽ 403 dù
   người dùng thao tác hoàn toàn bình thường.
4. `withCredentials: true` (đã bật sẵn cho cookie phiên) cũng áp dụng cho cookie CSRF — không
   cần cấu hình thêm.

BE tham chiếu: `src/BE/PlatformManager.Api/Program.cs` (`AddAntiforgery`, endpoint
`/api/antiforgery/token`, middleware validate trước `MapControllers()`),
`src/BE/PlatformManager.Api/Common/GlobalExceptionHandler.cs` (dịch
`AntiforgeryValidationException` → 403), quy tắc ở
`doc/huong_dan/wiki-core/be/02-identity-auth.md` §CSRF, test chốt
`src/BE/Tests/PlatformManager.Core.IntegrationTests/Csrf/CsrfSeamTests.cs`.

## `POST /api/auth/logout`

Không cần body. Trả `Data: true`.

## `GET /api/auth/me` — `[Authorize]`

Trả `Data: CurrentUserInfo` giống login — **PHẢI có `mustChangePassword`** (đã verify field 2026-08-16
này có mặt trong DTO, xem `PlatformManager.Core.Application.Auth.CurrentUserInfo`).

Đã verify thật — gọi khi CHƯA đăng nhập trả đúng 401 JSON sạch (không 302 redirect —
điểm rủi ro cao nhất của Program.cs đã được xử lý đúng):

```
$ curl -i /api/auth/me
HTTP/1.1 401 Unauthorized
Content-Type: application/json; charset=utf-8
{"message":"Chưa đăng nhập.","status":"BUSINESS_ERROR","code":"AuthenticationError",
 "traceId":"0HNNRBJU4MMRL:00000001"}
```

## `POST /api/auth/change-password` — `[Authorize]`

Request:

```json
{ "currentPassword": "SuperAdmin@123", "newPassword": "MatKhauMoi@123" }
```

Thành công → `Data: true`, đồng thời `AppUser.MustChangePassword` đổi thành `false` (verify
qua code — `IdentityService.ChangePasswordAsync`, chưa chạy tay được vì cần DB).

Lỗi: `AUTH.CHANGE_PASSWORD_FAILED` (422) — kèm message chi tiết từ Identity (password không
đủ mạnh, sai mật khẩu hiện tại...).

### 🔐 Ảnh hưởng tới phiên đăng nhập: giữ phiên hiện tại, chấm dứt các phiên khác

| Phiên | Sau khi đổi mật khẩu thành công |
| --- | --- |
| **Phiên đang gọi endpoint này** | **Giữ nguyên** — người dùng dùng tiếp bình thường, KHÔNG bị đăng xuất |
| **Mọi phiên khác của chính người đó** (trình duyệt/máy khác) | **Bị chấm dứt** trong vòng **≤ 30 phút** — request kế tiếp của các phiên đó trả **401** |

Đây là hành vi **cố ý, đúng chuẩn bảo mật**: đổi mật khẩu phải vô hiệu hoá các phiên cũ, nhưng
không có lý do gì đá người vừa chủ động đổi mật khẩu ra khỏi hệ thống. BE giữ phiên hiện tại
bằng `SignInManager.RefreshSignInAsync` ngay sau khi đổi thành công.

Cơ chế và ngưỡng 30 phút: xem `doc/huong_dan/wiki-core/be/02-identity-auth.md` §"Vòng đời
phiên đăng nhập".

**FE lưu ý:** **không** cần tự gọi `POST /api/auth/logout` rồi bắt đăng nhập lại sau khi đổi
mật khẩu — cookie hiện tại vẫn hợp lệ. Luồng đúng cho user `mustChangePassword: true`: đổi mật
khẩu thành công → cập nhật `mustChangePassword` về `false` trong state (hoặc gọi lại
`GET /api/auth/me`) → đi thẳng vào ứng dụng.

## Lỗi hạ tầng không mong đợi — đã verify thật 2026-08-16 (không lộ stack trace)

```
$ curl -X POST /api/auth/login -d '{"UserName":"test","Password":"test123"}'
# (DB chưa sẵn sàng)
HTTP/1.1 500 Internal Server Error
{"message":"Đã có lỗi xảy ra.","status":"SYSTEM_ERROR","code":"SystemError","traceId":"..."}
```

## Ghi chú triển khai

- Cookie tên `PlatformManager.Auth`, `SameSite=None; Secure=Always` (bắt buộc đi kèm nhau khi
  FE ở origin khác — `http://localhost:4200` là "secure context" theo ngoại lệ trình duyệt
  cho `localhost`, nên vẫn hoạt động ở dev dù chạy `http`). CORS đã verify thật 2026-08-16 trả đúng
  `Access-Control-Allow-Credentials: true` + origin cụ thể (không `*`).
- `frontend-expert`: gọi API luôn kèm `credentials: 'include'`/`withCredentials: true`.
- **Vòng đời phiên:** cookie `ExpireTimeSpan = 14 ngày` + `SlidingExpiration = true` ⇒ với
  người dùng thao tác đều tay, cookie **thực tế không tự hết hạn**. Cơ chế chấm dứt phiên duy
  nhất (ngoài `logout`) là `SecurityStampValidator`, chu kỳ **30 phút**. Hệ quả FE cần biết:
  một phiên có thể **đột ngột nhận 401** ở request bất kỳ khi tài khoản bị khoá / bị đổi role /
  bị đổi mật khẩu ở nơi khác — interceptor xử lý 401 phải điều hướng về màn đăng nhập một cách
  êm, không coi đó là lỗi hệ thống. Chi tiết:
  `doc/huong_dan/wiki-core/be/02-identity-auth.md` §"Vòng đời phiên đăng nhập".

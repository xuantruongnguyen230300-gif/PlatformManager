---
kind: luat
scope: core
verified: 2026-09-06
---

# API Contract Card — Auth

**Status: IMPLEMENTED** (2026-08-16) — build xanh, đã gọi thử qua curl (xem ví dụ response
thật bên dưới). Cơ chế: **cookie session** (ASP.NET Core Identity, KHÔNG JWT) — đã CHỐT.

> Ví dụ response thành công (login/me trả `Data` thật) **chưa capture được** vì môi trường
> build hiện tại không được phép tự áp schema DB thật lên Postgres (xem
> `doc/ke-hoach-xay-lai-corebase.md` gotcha #6) — người dùng cần tự dựng DB trước (hai lệnh ở
> §`POST /api/auth/login` bên dưới), sau đó `frontend-expert`/người dùng có thể
> gọi thử luồng thành công đầy đủ.
>
> *(Sửa 2026-09-08: câu này trước bảo chạy `doc/cau-truc-database.sql` + `dotnet ef database
> update`. Cả hai đều sai hôm nay — lệnh `ef database update` bị `.claude/settings.json` chặn
> thẳng, và `cau-truc-database.sql` nay **toàn bộ là DDL của module DTI đã gỡ**, mang banner
> `TÀI LIỆU LỊCH SỬ`. Chạy đúng nó cũng không dựng nổi bảng `AspNetUsers`.)* Các ví dụ lỗi (401/400/500) bên dưới đã verify **thật** (2026-08-16) với
> app chạy thật (không phải suy đoán).

## Envelope chung

Mọi response đi qua `IApiResult<T>` — camelCase: `data,message,status,code,businessCode,
traceId,retryable,fields,fieldErrors,messageParams`. Riêng **khoá** của `fields`/`fieldErrors` (lỗi validate
theo field) giữ nguyên **PascalCase** (khớp tên property C#, vd `"UserName"`) — xem
`wiki-core/fe/02-http-envelope.md`.

Không phải trường nào cũng có mặt trong mọi response, và đó là chủ đích — ba trường dưới đây
**vắng hẳn** khi không có gì để nói, thay vì gửi `null` cho mọi lỗi:

| Trường | Có mặt khi | Trạng thái |
| --- | --- | --- |
| `fields` | nhánh 400 (lỗi validate) | ✅ CÓ THẬT (đối chiếu 2026-09-08, `src/BE/Core/PlatformManager.Core.Application/Common/Results/IApiResult.cs:21`) |
| `fieldErrors` | nhánh 400 — cùng tập lỗi với `fields`, dạng **mã + câu** | ✅ CÓ THẬT (đối chiếu 2026-09-08, `src/BE/Core/PlatformManager.Core.Application/Common/Results/IApiResult.cs:53`); chạy **song song** với `fields` cho tới bước gỡ |
| `messageParams` | **chỉ khi** mã đi kèm có tham số | ✅ CÓ THẬT (đối chiếu 2026-09-08, `src/BE/Core/PlatformManager.Core.Application/Common/Results/IApiResult.cs:80`). Nằm ở **hai** chỗ: gốc envelope, và trong từng phần tử `fieldErrors` — cùng tên, cùng kiểu, cùng cách đọc |

Định nghĩa đầy đủ của cả ba (kiểu, chỗ đặt, luật "vắng mặt khi không có tham số") ở
`doc/huong_dan/quy-uoc/be-api-controller.md` §Envelope response — card này **không** chép lại.
Hai ví dụ 404/405 ngay dưới là minh hoạ đúng luật đó: không có tham số nên không có
`messageParams`, không phải lỗi validate nên không có `fields`.

### 404 / 405 do định tuyến — envelope, không còn thân rỗng (2026-09-04)

Trước ngày này, lời hứa *"Mọi response đi qua `IApiResult<T>`"* ở trên **có hai ngoại lệ chưa ai
khai**: URL không khớp endpoint nào, và URL đúng nhưng sai HTTP verb. Cả hai rời khỏi BE với
`Content-Length: 0`. Đo thật (2026-09-04, trước khi sửa):

```
$ curl -sk -D - https://localhost:7168/api/khong-he-co
HTTP/1.1 404 Not Found
Content-Length: 0
```

Nay cả hai mang envelope. Đã gọi thử bằng curl trên app chạy thật (2026-09-04, sau khi sửa):

```
$ curl -sk -D - https://localhost:7168/api/khong-he-co
HTTP/1.1 404 Not Found
Content-Type: application/json; charset=utf-8
```
```json
{"message":"Không tìm thấy tài nguyên được yêu cầu.","status":"BUSINESS_ERROR",
 "code":"NotFound","businessCode":"ROUTE.NOT_FOUND","traceId":"0HNO9S8JAP586:00000001",
 "retryable":false}
```

```
$ curl -sk -D - https://localhost:7168/api/auth/login      # route chỉ khai [HttpPost]
HTTP/1.1 405 Method Not Allowed
Content-Type: application/json; charset=utf-8
Allow: POST
```
```json
{"message":"Phương thức HTTP không được hỗ trợ cho tài nguyên này.","status":"BUSINESS_ERROR",
 "code":"MethodNotAllowed","businessCode":"ROUTE.METHOD_NOT_ALLOWED",
 "traceId":"0HNO9S8JAP587:00000001","retryable":false}
```

| BusinessCode | HTTP | Khi nào |
| --- | ---: | --- |
| `ROUTE.NOT_FOUND` | 404 | Không endpoint nào khớp đường dẫn — **bug phía gọi**, không phải dữ liệu thiếu |
| `ROUTE.METHOD_NOT_ALLOWED` | 405 | Đường dẫn đúng, verb sai. Header `Allow` nói verb nào hợp lệ |

- **Chỉ áp cho tiền tố `/api`.** `/health`, `/hangfire`, `/swagger` giữ nguyên hành vi cũ (thân
  rỗng) — đã kiểm bằng curl cùng lượt: `GET /health/khong-he-co` vẫn `Content-Length: 0`.
- **`ROUTE.NOT_FOUND` ≠ `USER.NOT_FOUND`.** Cùng HTTP 404, cùng `code: "NotFound"`. Phân biệt bằng
  `businessCode`, KHÔNG bằng `message`: một bên nghĩa là FE gọi sai URL, bên kia nghĩa là bản ghi
  không tồn tại. 404 do handler chủ động trả **không** bị đổi (có test chốt).

#### 🔴 Việc FE cần làm (`frontend-expert`)

1. ~~**`ApiErrorCode` trong `src/FE/src/app/core/http/api-result.model.ts` phải thêm
   `'MethodNotAllowed'`.**~~ ✅ FE đã thêm (đối chiếu 2026-09-04,
   `src/FE/src/app/core/http/api-result.model.ts:30`). Giữ dòng này lại vì **lý do** của nó vẫn
   là luật: union thiếu một giá trị BE có thật ⇒ response 405 không khớp kiểu, đúng cùng lỗ mà
   `'TooManyRequests'` từng mắc (xem mục 429 bên dưới).
2. **Đừng bind 404 bằng `code`.** `code: "NotFound"` nay phủ cả hai nghĩa; nhánh "URL sai" và nhánh
   "bản ghi không có" phải tách theo `businessCode`.
3. `ROUTE.*` là tín hiệu **bug của chính FE** (gọi sai địa chỉ/verb), không phải lỗi để hiện cho
   người dùng đọc — cân nhắc log/telemetry thay vì toast nghiệp vụ.

BE tham chiếu: `src/BE/PlatformManager.Api/Common/ApiStatusCodeEnvelopeMiddleware.cs`, catalog
`src/BE/Core/PlatformManager.Core.Application/Common/Results/InfrastructureErrors.cs`, quy tắc ở
[`../huong_dan/quy-uoc/be-api-controller.md`](../huong_dan/quy-uoc/be-api-controller.md)
§"404/405 do định tuyến", test chốt
`src/BE/Tests/PlatformManager.Core.IntegrationTests/Common/RoutingEnvelopeTests.cs`.

## `POST /api/auth/login`

Request (`[FromBody]`, phẳng):

```json
{ "userName": "SuperAdmin", "password": "<Bootstrap:SuperAdminPassword>", "rememberMe": false }
```

> ### ✅ `rememberMe` — CÓ THẬT (đối chiếu 2026-09-06)
>
> | Chỗ | Bằng chứng |
> | --- | --- |
> | Trường của command, **mặc định `false`** | `src/BE/Core/PlatformManager.Core.Application/Auth/LoginCommand.cs:16` |
> | Truyền xuống Identity | `src/BE/Core/PlatformManager.Core.Application/Auth/LoginCommand.cs:33` |
> | `isPersistent: rememberMe`, **hết khai cứng `true`** | `src/BE/Core/PlatformManager.Core.Infrastructure/Identity/IdentityService.cs:55` |
> | Ô tick nối thật vào request ở FE | `src/FE/src/app/platform/login/pages/login/login.page.html:75-76` → `login.page.ts:102,184` |
>
> Nghĩa: request **không** gửi trường này được hiểu là KHÔNG tích ⇒ **cookie phiên**, chết khi
> đóng trình duyệt. `true` ⇒ cookie 14 ngày trượt. Mặc định nằm ở bên an toàn có chủ đích: một
> client cũ chưa biết trường này nhận phiên NGẮN hơn, không phải dài hơn.
>
> Nhãn ô nhập cũng đã đổi từ `Email` sang `Tên đăng nhập` — API **không đổi**, nó vốn luôn nhận
> `userName`. Nhãn cũ mới là thứ sai (`login.page.html:24` dùng khoá `shared.field.userName`).
>
> 🔄 **SỬA 2026-09-06.** Khối này trước đây là `🚧 Quyết định người dùng 2026-08-31 — thêm
> `rememberMe`` và khẳng định *"Có thật hôm nay: `isPersistent: true` khai cứng
> (`IdentityService.cs:33`)"*. Cả hai vế đều đã sai: việc đã làm xong ở cả BE lẫn FE, và dòng 33
> của file đó nay là `await identityService.SignInAsync(...)` — không phải chỗ nó chỉ. Lý do đầy
> đủ vì sao phải wire ô này vẫn giữ ở
> [`../Design/Frontend/PlatformManager/Screens/05-auth.md`](../Design/Frontend/PlatformManager/Screens/05-auth.md)
> § Normalize on redesign, mục 4.

⚠️ **Cập nhật 2026-08-24: KHÔNG còn mật khẩu hardcode `"SuperAdmin@123"`.** `CoreSeeder` seed
**2 tài khoản bootstrap RIÊNG BIỆT** — `SuperAdmin` (role `SuperAdmin` DUY NHẤT) và `Admin` (role
`Admin` DUY NHẤT) — mật khẩu đọc từ `BootstrapOptions` (User Secrets `Bootstrap:SuperAdminPassword`
/ `Bootstrap:AdminPassword` lúc dev, biến môi trường `Bootstrap__SuperAdminPassword` /
`Bootstrap__AdminPassword` lúc production; fail-fast — app không khởi động được nếu thiếu). Xem
`src/BE/Core/PlatformManager.Core.Infrastructure/Persistence/BootstrapOptions.cs`.
**Dựng DB mới để đăng nhập thử — đúng hai lệnh** (đối chiếu 2026-09-08):

```bash
# 1. Schema: MỘT file, chạy bằng psql/pgAdmin/DBeaver. Agent không chạy hộ (CLAUDE.md §1).
psql -d <database> -f doc/db-khoi-tao.sql

# 2. Tài khoản bootstrap: lệnh riêng, chạy một lần rồi thoát, KHÔNG mở cổng.
dotnet run --project src/BE/PlatformManager.Api -- --seed
```

PASS của bước 1: `psql -d <database> -c '\dt core.*'` liệt kê được `AspNetUsers`.
PASS của bước 2: đăng nhập được bằng `SuperAdmin` + `Bootstrap:SuperAdminPassword`.

Quy trình đầy đủ (gồm cả vì sao **không** dùng `dotnet ef database update`) ở
[`../cau-truc-database.md`](../cau-truc-database.md) §5 — card này không chép lại.

> *(Sửa 2026-09-08: chỗ này trước là một ghi chú lịch sử trỏ tới một script dựng DB **chưa
> từng tồn tại**, và không nói đường đi thật. Ghi chú giữ được lý do nhưng không giúp ai
> dựng được DB — mà đây là card Auth, tức đúng chỗ người ta cần một DB có tài khoản để gọi
> thử. Đường dẫn chết cũng làm đỏ `check-docs.sh` §4.)*

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
| `AUTH.LOCKED_OUT` | 422 | Tài khoản đang bị khoá — **giữ tách biệt có chủ đích**, xem ghi chú dưới bảng |
| `RATE_LIMIT.TOO_MANY_REQUESTS` | **429** | Chạm hạn mức đăng nhập theo IP, hoặc hạn mức nền — con số và thuật toán xem mục riêng bên dưới |
| (validation) | 400 | UserName/Password rỗng — đã verify thật 2026-08-16: |

```
$ curl -X POST /api/auth/login -d '{"UserName":"","Password":""}'
HTTP/1.1 400 Bad Request
{"message":"Dữ liệu không hợp lệ.","status":"VALIDATION_ERROR","code":"ValidationError",
 "traceId":"...","fields":{"UserName":["'User Name' must not be empty."],
 "Password":["'Password' must not be empty."]}}
```

> **Vì sao `LOCKED_OUT` không gộp vào `INVALID_CREDENTIALS` (chốt 2026-08-30).** Thông điệp
> riêng xác nhận tài khoản đó có thật, nên nhìn qua là một rò rỉ. Nhưng nó chỉ rò rỉ **một**
> tài khoản sau **5 lần đoán sai** cho riêng nó — không liệt kê hàng loạt được. Đường liệt kê
> hàng loạt thật là **chênh lệch thời gian phản hồi**, và đường đó đang được vá riêng. Đổi lại,
> gộp thông điệp khiến người dùng thật bị khoá không hiểu chuyện gì và thử lại liên tục — mà
> mỗi lần thử lại **gia hạn khoá thêm 15 phút**.
>
> 📖 Đầy đủ đánh đổi + phép nghiệm thu: [`../huong_dan/wiki-core/be/09-security-beyond-auth.md`](../huong_dan/wiki-core/be/09-security-beyond-auth.md)
> §"Liệt kê tài khoản qua đường đăng nhập"

### 🚦 429 Too Many Requests — rate limit (cập nhật 2026-08-21, lần 2)

**BA tầng giới hạn, CỘNG DỒN — không phải một.** Rất dễ đọc nhầm, nên ghi rõ:

> 🔄 **SỬA 2026-09-06.** Bản trước viết *"HAI tầng"* ngay trên một bảng có **ba** dòng, và đoạn
> văn dưới bảng còn ghi *"tiêu cả hai limiter"* + *"ăn vào hạn mức 100"*. Cả ba chỗ đều lệch
> khỏi `src/BE/PlatformManager.Api/Program.cs:244,251,263` (5 / 200 / 10) — bảng đã được cập nhật
> 2026-09-01 nhưng văn xuôi quanh nó thì không, đúng dạng lỗi `check-docs.sh` không bắt được.

| Tầng | Hạn mức | Áp cho |
| --- | --- | --- |
| `GlobalLimiter` | ✅ **200/phút/IP, cửa sổ trượt 6 đoạn** (2026-09-01) | **MỌI** endpoint, kể cả endpoint không khai gì |
| Policy `"login"` | **5 request/phút/IP** — không đổi | Riêng `POST /api/auth/login` |
| ✅ Hàng rào thứ hai theo **tên đăng nhập** | 10 lượt / 5 phút, cửa sổ trượt (2026-09-01) | Riêng `POST /api/auth/login` — chặn tấn công phân tán |

> 📖 Con số, thuật toán và lý do hiệu chỉnh là của file chủ, đừng chép về đây:
> đọc [`../huong_dan/quy-uoc/be-api-controller.md`](../huong_dan/quy-uoc/be-api-controller.md)
> §"Hiệu chỉnh hạn mức"

`POST /api/auth/login` tiêu **cả ba** limiter cho mỗi lượt. Mốc chặt hơn luôn chạm trước nên
hành vi thấy được vẫn là "429 ở lượt thứ 6" khi thử từ **một** IP, nhưng 5 lượt login đó **có**
ăn vào hạn mức 200 chung của cùng IP — FE gọi nhiều API sau khi đăng nhập cần biết điều này. Từ
**nhiều** IP khác nhau vào cùng một tên đăng nhập thì hàng rào thứ ba chạm trước: 429 ở lượt thứ
11 trong 5 phút, bất kể IP nào gửi.

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
`doc/huong_dan/wiki-core/be/02-identity-auth.md` §CSRF cho lý do đầy đủ. Áp dụng **Lớp 2** (🚧 Lớp 1 hiện
**KHÔNG tồn tại** — cookie khai `SameSite=None` vì FE khác origin; sẽ thay bằng kiểm header
`Origin`, xem `doc/huong_dan/wiki-core/be/02-identity-auth.md` §"Quyết định người dùng 2026-08-31"): mọi request `POST`/`PUT`/`PATCH`/`DELETE` **bắt buộc**
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
danh tính rồi dùng token cũ sẽ bị từ chối `403` với thông điệp "meant for a different
claims-based user". **Ràng buộc này đối xứng: nó áp cho CẢ HAI chiều đổi danh tính** — anonymous →
user (login) *và* user → anonymous (logout). **Cookie `XSRF-TOKEN` KHÔNG tự đổi giá trị theo thời gian hay theo sự kiện
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

> **🔄 LẬT 2026-09-03.** Bản trước viết: *"Không có `businessCode` riêng (không phải lỗi nghiệp
> vụ) — FE nhận diện qua `code = "AuthorizationError"`… **message khác biệt là điểm phân biệt duy
> nhất** nếu cần hiển thị riêng."* Câu đó dạy đúng thứ mà
> [`../huong_dan/quy-uoc/be-api-controller.md`](../huong_dan/quy-uoc/be-api-controller.md) cấm —
> buộc client so khớp **câu chữ** để phân nhánh, tức biến câu thành hợp đồng ngầm. Cùng đợt đã lật
> `contracts/users.md` theo cùng lý do; card này bị bỏ quên và một lượt kiểm độc lập tìm ra.

Nhánh này nay **có** `businessCode`: `AUTH.CSRF_REJECTED`. FE phân nhánh theo mã, KHÔNG theo
`message`. Ba nhánh 403/401 hạ tầng còn lại: `AUTH.ORIGIN_REJECTED`, `AUTH.FORBIDDEN`,
`AUTH.NOT_AUTHENTICATED`.

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
4. **Gọi LẠI `GET /api/antiforgery/token` ngay sau khi `POST /api/auth/logout` kết thúc** —
   đối xứng với bước 3, vì logout cũng là một lần đổi danh tính (user → anonymous). Thiếu bước
   này: cookie `XSRF-TOKEN` vẫn giữ token gắn với người dùng vừa thoát, và **`POST /api/auth/login`
   lần kế tiếp bị 403 `AUTH.CSRF_REJECTED`** — trừ khi người dùng tình cờ tải lại trang, vì lúc đó
   bước 2 mới chạy lại. Gọi lại ở nhánh chạy trong MỌI trường hợp (kể cả logout lỗi hoặc bị huỷ),
   không chỉ nhánh thành công.
5. `withCredentials: true` (đã bật sẵn cho cookie phiên) cũng áp dụng cho cookie CSRF — không
   cần cấu hình thêm.

> **Bổ sung 2026-09-08 — bug đã xảy ra thật trên trình duyệt.** Bản trước của mục này chỉ có bước
> 3 (mồi lại sau login), và FE cài đúng y như vậy. Hệ quả: đăng xuất rồi đăng nhập lại **trong
> cùng một lần tải trang** luôn hỏng với đúng thông điệp CSRF ở trên. Nguyên nhân sâu xa là câu
> chữ của chính cạm bẫy phía trên — nó viết *"đổi danh tính (login)"*, đóng khung vấn đề vào một
> chiều, trong khi ràng buộc của `IAntiforgery` không hề chỉ có một chiều. Người đọc cài đúng thứ
> được viết ra, và thứ được viết ra thiếu một nửa. Đã sửa cả hai chỗ cùng lúc.

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

Lỗi:

| Mã | HTTP | Khi nào |
| --- | --- | --- |
| `AUTH.NOT_AUTHENTICATED` | 401 | chưa đăng nhập |
| `AUTH.CHANGE_PASSWORD_FAILED` | 422 | Identity từ chối — sai mật khẩu hiện tại, mật khẩu mới không đạt chính sách. `message` là câu cố định `"Đổi mật khẩu thất bại."`; **lý do nằm ở `fieldErrors`** |
| `USER.NOT_FOUND` | 404 | tài khoản không còn (bị xoá khi phiên còn sống) — **thêm 2026-09-05** |

> **✅ Đã thi công 2026-09-05 — "message chi tiết" nay nằm ở `fieldErrors`.** Trước đó *"chi
> tiết"* là danh sách **mã Identity** nối bằng dấu chấm phẩy nhét vào giữa câu tiếng Việt:
> `Đổi mật khẩu thất bại: PasswordTooShort; PasswordRequiresDigit` — không dịch được, và không ô
> nhập nào được tô đỏ. Nay câu là `Đổi mật khẩu thất bại.` và từng mã đi ra một phần tử
> `fieldErrors` với `code` là chính mã đó, gắn vào **đúng ô**:
>
> ```json
> {
>   "status": "BUSINESS_ERROR", "code": "BusinessRuleError",
>   "businessCode": "AUTH.CHANGE_PASSWORD_FAILED",
>   "message": "Đổi mật khẩu thất bại.",
>   "fieldErrors": {
>     "CurrentPassword": [{ "code": "PasswordMismatch", "message": "PasswordMismatch" }]
>   }
> }
> ```
>
> **Khoá của `fieldErrors` là tên ô, PascalCase** — `CurrentPassword` / `NewPassword`. Ánh xạ đi
> theo **MÃ**, không theo endpoint: `PasswordMismatch` nói về mật khẩu **hiện tại**, nên nó
> KHÔNG rơi vào `NewPassword`. Mã không thuộc ô nào rơi vào khoá `"$record"`.
>
> `fieldErrors[].message` là **chính mã đó** — dev-facing + fallback. Câu cho người dùng do FE
> tra từ bảng dịch theo `code`; BE cố ý không dựng bộ chữ thứ hai (§3, §5.2 của file chủ).
>
> ⚠️ **FE mới đọc `fieldErrors` ở MỘT màn** (đối chiếu 2026-09-06). Màn đăng nhập đã chuyển:
> `src/FE/src/app/platform/login/pages/login/login.page.ts:209-216` đọc `result.fieldErrors` và
> dịch từng phần tử qua `ApiErrorMessageService.fieldMessage`. Màn đổi mật khẩu thì **chưa**:
> `src/FE/src/app/platform/doi-mat-khau/pages/doi-mat-khau/doi-mat-khau.page.ts:172` còn bind
> `result?.fields`, nên chính endpoint này vẫn hiển thị qua đường cũ. Lý do + nghiệm thu:
> [`../huong_dan/wiki-core/be/16-i18n-va-ma-loi.md`](../huong_dan/wiki-core/be/16-i18n-va-ma-loi.md) §11.
> Test: `Tests/PlatformManager.Core.IntegrationTests/Auth/IdentityCodeFieldErrorsTests.cs`.
>
> 🔄 **SỬA 2026-09-06.** Bản trước viết *"FE hôm nay chưa đọc `fieldErrors` (còn bind từ
> `fields`)"* — đúng vào 2026-09-05, sai từ 2026-09-05 khi màn đăng nhập được bọc i18n.

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
{"message":"Đã có lỗi xảy ra.","status":"SYSTEM_ERROR","code":"SystemError",
 "businessCode":"SYSTEM.UNEXPECTED","traceId":"..."}
```

| BusinessCode | HTTP | Khi nào |
| --- | ---: | --- |
| `SYSTEM.UNEXPECTED` | 500 | Exception không mong đợi (bug hoặc hạ tầng) rơi xuống `GlobalExceptionHandler`. **Mã duy nhất của nhánh 500** — mọi 500 đều mang đúng mã này |

- **Câu `message` cố ý không nói gì về nguyên nhân.** Chi tiết ở lại log phía BE, tra bằng
  `traceId` trong response.
- **FE bind theo `businessCode`, đừng bind theo `message`.** Câu tiếng Việt là dev-facing +
  fallback; mã mới là thứ ổn định.
- Không có `fields`/`fieldErrors`/`messageParams` — mã này không có tham số và không phải lỗi
  validate, đúng luật "vắng mặt khi không có gì để nói" ở §Envelope chung.

> **`businessCode` thêm vào nhánh 500 ngày 2026-09-03** — trước đó nhánh này dựng envelope từ
> một `string` trần nên không mang được mã nào. Ví dụ curl ở trên chụp ngày 2026-08-16, tức
> **trước** thay đổi, nên bản cũ của nó thiếu khoá `businessCode`; dòng đã bổ sung theo code.
> Đối chiếu 2026-09-08:
> `src/BE/Core/PlatformManager.Core.Application/Common/Results/ApiResult.cs:94-100`
> (`SystemError(ErrorDescriptor)` gán `BusinessCode`),
> `src/BE/Core/PlatformManager.Core.Application/Common/Results/InfrastructureErrors.cs:69-72`
> (`Unexpected` = `"SYSTEM.UNEXPECTED"`),
> `src/BE/PlatformManager.Api/Common/GlobalExceptionHandler.cs:42` (nhánh `_ =>` gọi nó).

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
  - ✅ CÓ THẬT (đối chiếu 2026-09-06) — FE đã hiện thực hoá trong `httpErrorInterceptor`:
    `src/FE/src/app/core/interceptors/http-error.interceptor.ts:100` quyết định "phiên đã chết",
    `:108` xoá state client (`CurrentUserService.clear()`), `:120` điều hướng về
    `CORE_ROUTES.signIn` kèm `returnUrl=<url hiện tại>` (cùng hình dạng `authGuard`), lỗi vẫn được
    rethrow cho nơi gọi. Ba ca KHÔNG điều hướng: request probe `SKIP_ERROR_TOAST` (`GET /auth/me`
    lúc khởi động), đang ở sẵn màn đăng nhập (401 = sai mật khẩu), và request 401 thứ hai trở đi
    của cùng một lần chết phiên (cờ
    `src/FE/src/app/core/interceptors/http-error.interceptor.ts:36`). Thông báo dùng `toast.warn`
    (`:109`) chứ không phải toast lỗi đỏ, đúng yêu cầu "không coi đó là lỗi hệ thống" ở trên; câu
    và tiêu đề nay là **khoá dịch** (`shared.httpError.endedText` / `endedTitle`, `:25-26`), không
    còn là chuỗi tiếng Việt khai cứng.

    🔄 **SỬA 2026-09-06.** Năm số dòng của mục này (`:125`, `:132`, `:141`, `:133`, `:32`) đều đã
    trôi sau đợt i18n 2026-09-05 — file chỉ có 146 dòng nên chúng vẫn "trong tầm" và
    `check-docs.sh` §6 vẫn cho qua, nhưng không dòng nào còn trỏ đúng thứ nó mô tả.

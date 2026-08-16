# API Contract Card — Auth

**Status: IMPLEMENTED** (2026-08-16) — build xanh, đã gọi thử qua curl (xem ví dụ response
thật bên dưới). Cơ chế: **cookie session** (ASP.NET Core Identity, KHÔNG JWT) — đã CHỐT.

> Ví dụ response thành công (login/me trả `Data` thật) **chưa capture được** vì môi trường
> build hiện tại không được phép tự áp schema DB thật lên Postgres (xem
> `doc/ke-hoach-xay-lai-corebase.md` gotcha #6) — người dùng cần tự chạy tay
> `doc/ERD/migrations/0003_corebase_v2.sql` trước, sau đó `frontend-expert`/người dùng có thể
> gọi thử luồng thành công đầy đủ. Các ví dụ lỗi (401/400/500) bên dưới đã verify **thật** với
> app chạy thật (không phải suy đoán).

## Envelope chung

Mọi response đi qua `IApiResult<T>` — camelCase: `data,message,status,code,businessCode,
traceId,retryable,fields`. Riêng `fields` (lỗi validate theo field) giữ nguyên **PascalCase**
key (khớp tên property C#, vd `"UserName"`) — xem `wiki-core/fe/02-http-envelope.md`.

## `POST /api/auth/login`

Request (`[FromBody]`, phẳng):

```json
{ "userName": "SuperAdmin", "password": "SuperAdmin@123" }
```

Response thành công — `Data: CurrentUserInfo`:

```json
{
  "id": "guid", "userName": "SuperAdmin", "email": "superadmin@platformmanager.local",
  "fullName": "Quản trị viên hệ thống", "roles": ["SuperAdmin", "Admin"], "mustChangePassword": true
}
```

Lỗi:
| BusinessCode | HTTP | Khi nào |
| --- | --- | --- |
| `AUTH.INVALID_CREDENTIALS` | 422 | Sai user/password |
| `AUTH.LOCKED_OUT` | 422 | Tài khoản đang bị khoá |
| (validation) | 400 | UserName/Password rỗng — đã verify thật: |

```
$ curl -X POST /api/auth/login -d '{"UserName":"","Password":""}'
HTTP/1.1 400 Bad Request
{"message":"Dữ liệu không hợp lệ.","status":"VALIDATION_ERROR","code":"ValidationError",
 "traceId":"...","fields":{"UserName":["'User Name' must not be empty."],
 "Password":["'Password' must not be empty."]}}
```

## `POST /api/auth/logout`

Không cần body. Trả `Data: true`.

## `GET /api/auth/me` — `[Authorize]`

Trả `Data: CurrentUserInfo` giống login — **PHẢI có `mustChangePassword`** (đã verify field
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

## Lỗi hạ tầng không mong đợi — đã verify thật (không lộ stack trace)

```
$ curl -X POST /api/auth/login -d '{"UserName":"test","Password":"test123"}'
# (DB chưa sẵn sàng)
HTTP/1.1 500 Internal Server Error
{"message":"Đã có lỗi xảy ra.","status":"SYSTEM_ERROR","code":"SystemError","traceId":"..."}
```

## Ghi chú triển khai

- Cookie tên `PlatformManager.Auth`, `SameSite=None; Secure=Always` (bắt buộc đi kèm nhau khi
  FE ở origin khác — `http://localhost:4200` là "secure context" theo ngoại lệ trình duyệt
  cho `localhost`, nên vẫn hoạt động ở dev dù chạy `http`). CORS đã verify thật trả đúng
  `Access-Control-Allow-Credentials: true` + origin cụ thể (không `*`).
- `frontend-expert`: gọi API luôn kèm `credentials: 'include'`/`withCredentials: true`.

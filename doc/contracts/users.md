# API Contract Card — Users (Quản trị người dùng)

**Status: AGREED** (2026-08-16) — Application (P2) + Infrastructure (P3) + Api (P4) đã code
xong, build xanh, pipeline auth/envelope đã verify thật (xem `auth.md`). Chưa chuyển
IMPLEMENTED vì chưa gọi thử được response THÀNH CÔNG có data thật (cần DB đã migrate + có
user) — `backend-expert` sẽ cập nhật ví dụ response thật + đổi status khi người dùng xác nhận
đã chạy `doc/ERD/migrations/0003_corebase_v2.sql`.

Gate: `[Authorize(Roles = "SuperAdmin,Admin")]` toàn bộ controller — khớp menu "Quản trị hệ
thống > Người dùng".

## Envelope

CamelCase (`data,message,status,code,businessCode,traceId,retryable,fields`) — xem `auth.md`.

## `GET /api/users?page=1&pageSize=20&searchText=...`

`Data: PagedList<UserDto>` — `{ items, total, page, pageSize }`. Mỗi `UserDto`:

```json
{
  "id": "guid", "userName": "nguyen.van.a", "email": "...", "fullName": "Nguyễn Văn A",
  "roles": ["User"], "isLocked": false, "mustChangePassword": true, "dateCreate": "2026-08-16T..."
}
```

## `POST /api/users`

Request:

```json
{
  "userName": "nguyen.van.a", "email": "nguyen.van.a@example.com", "fullName": "Nguyễn Văn A",
  "tempPassword": "TempPass@123", "roles": ["User"]
}
```

`Data: guid` (Id user mới tạo). `MustChangePassword=true` tự động (áp dụng chung cơ chế
bootstrap — xem `auth.md`).

Lỗi: `USER.DUPLICATE_USERNAME` (409), `USER.DUPLICATE_EMAIL` (409), `USER.CREATE_FAILED` (422,
kèm lỗi chi tiết từ Identity — vd password không đủ mạnh).

## `PUT /api/users/{id}`

Request: `{ "email": "...", "fullName": "...", "roles": ["User", "Admin"] }` — KHÔNG đổi
`userName`/mật khẩu qua đây. `Data: true`.

## `POST /api/users/{id}/lock` / `POST /api/users/{id}/unlock`

Không cần body. Khoá qua `UserManager.SetLockoutEndDateAsync` (không thêm cột `IsActive`
riêng — xem `doc/ERD/ERD-corebase.md` §1.2). `Data: true`.

## Lỗi chung

`USER.NOT_FOUND` (404) khi `{id}` không tồn tại.

## Câu hỏi mở gửi `frontend-expert`

Chưa xác nhận: màn "Quản trị người dùng" có cần hiển thị badge "Đang hoạt động"/"Đã khoá"
suy từ `isLocked` (đã có sẵn field) hay tự tính lại từ field khác — mặc định dùng thẳng
`isLocked` đã trả sẵn, không cần tính lại phía FE.

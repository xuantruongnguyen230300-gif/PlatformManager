# API Contract Card — Users (Quản trị người dùng)

**Status: AGREED** (2026-08-16) — Application (P2) + Infrastructure (P3) + Api (P4) đã code
xong, build xanh, pipeline auth/envelope đã verify thật (xem `auth.md`). Chưa chuyển
IMPLEMENTED vì chưa gọi thử được response THÀNH CÔNG có data thật (cần DB đã migrate + có
user) — `backend-expert` sẽ cập nhật ví dụ response thật + đổi status khi người dùng xác nhận
đã chạy `doc/ERD/migrations/0003_corebase_v2.sql`.

Gate: `[Authorize(Roles = "SuperAdmin,Admin")]` toàn bộ controller — khớp menu "Quản trị hệ
thống > Người dùng".

## 🔐 Luật cấp/gỡ role `SuperAdmin` — BE enforce từ 2026-08-19

**Chỉ người gọi đang mang role `SuperAdmin` mới được thay đổi tư cách `SuperAdmin` của bất kỳ
user nào.** Áp cho **cả** `POST /api/users` lẫn `PUT /api/users/{id}`, kiểm ở handler
(`CreateUserHandler`/`UpdateUserHandler`), **trước** khi chạm tầng ghi.

"Thay đổi" gồm **CẢ HAI CHIỀU** — cách kiểm là so tập role **hiện tại** của user đích với tập
role **gửi lên**:

| Người gọi | Trạng thái hiện tại của user đích | `roles` gửi lên | Kết quả |
| --- | --- | --- | --- |
| `Admin` | (user mới) | có `SuperAdmin` | **403** — leo thang đặc quyền |
| `Admin` | không có `SuperAdmin` | có `SuperAdmin` | **403** — leo thang đặc quyền |
| `Admin` | có `SuperAdmin` | **không** có `SuperAdmin` | **403** — hạ quyền/vô hiệu hoá break-glass |
| `Admin` | có `SuperAdmin` | **vẫn** có `SuperAdmin` | ✅ cho qua — sửa email/tên bình thường |
| `Admin` | bất kỳ | chỉ `Admin`/`User` | ✅ cho qua như cũ |
| `SuperAdmin` | bất kỳ | bất kỳ | ✅ cho qua |

Mã lỗi: **`USER.SUPERADMIN_ROLE_CHANGE_FORBIDDEN` (403, `code: "AuthorizationError"`)** —
KHÔNG map về `BusinessRuleError`, FE phân biệt được để hiển thị đúng "thiếu quyền" thay vì
"dữ liệu sai".

**FE lưu ý:** vì `PUT` nhận danh sách role **trọn gói**, form sửa user do `Admin` mở phải gửi
lại nguyên `roles` hiện có của user đích (kể cả `SuperAdmin`) khi chỉ đổi email/tên — bỏ sót
`SuperAdmin` trong payload nay là **403**, không còn âm thầm hạ quyền như trước. Việc ẩn tuỳ
chọn `SuperAdmin` trên UI (`quan-tri-nguoi-dung.model.ts`) vẫn giữ được, nhưng nay chỉ là trải
nghiệm — chặn thật nằm ở BE.

⚠️ **Ca 403 KHÔNG phủ hết — form vẫn phải bảo toàn role bất kể ai đang đăng nhập.** Khi người
gọi **chính là `SuperAdmin`**, BE (đúng luật trên) **cho qua**, nên payload thiếu `SuperAdmin`
sẽ **âm thầm hạ quyền** user đích, không có lỗi nào bật ra. Ca `Admin` ít nhất còn báo 403;
ca này im lặng. Vì vậy FE phải giữ lại các role nằm ngoài danh sách quản lý được trên form
(`ASSIGNABLE_ROLES`) rồi gửi kèm khi `PUT` — **luôn luôn**, không chỉ khi người thao tác là
`Admin`. `GET /api/users` đã trả `roles` đầy đủ nên FE có sẵn dữ liệu để làm việc này.

Lưu ý rộng hơn: `UpdateAsync` gỡ **mọi** role không có trong payload (không riêng `SuperAdmin`)
— hiện chỉ có 3 role nên `SuperAdmin` là ca duy nhất, nhưng luật "bảo toàn role không quản lý
được" nên viết tổng quát để còn đúng khi thêm role mới.

Ràng buộc định dạng: `roles` so khớp theo **tập hợp** — thứ tự và phần tử trùng lặp không ảnh
hưởng. Nhưng **chữ hoa/thường phải khớp chính xác** (`"SuperAdmin"`, không phải `"superadmin"`):
validator kiểm `Roles.All.Contains(r)` theo ordinal và chạy **trước** handler, nên sai casing
bị chặn ở tầng validation với `ValidationError` (400) kèm `fields`, không phải 403.

Nguồn: lượt review 2026-08-19 (OWASP A01 — Broken Access Control).
Test: `Tests/PlatformManager.Core.UnitTests/Users/SuperAdminRoleEscalationTests.cs` —
**test mới là bằng chứng sống**, không phải file report.

## 🔐 Bảo vệ tài khoản quản trị — 2 luật bổ sung (2026-08-19, đợt 2)

Luật ở trên chỉ chặn *người khác* leo thang. Hai đường còn lại đã bịt nốt — tất cả nằm ở
**một chỗ duy nhất**: `Core.Application/Users/SuperAdminAccountGuard.cs`.

| # | Luật | `businessCode` (403) | `message` BE trả về |
| --- | --- | --- | --- |
| 2 | Không ai được **tự gỡ** vai trò `SuperAdmin` của **chính mình** — kể cả chính `SuperAdmin` | `USER.SELF_SUPERADMIN_REMOVAL_FORBIDDEN` | *Bạn không thể tự gỡ vai trò SuperAdmin của chính mình. Hãy nhờ một SuperAdmin khác thực hiện.* |
| 3 | Chỉ `SuperAdmin` mới được **khoá** tài khoản có vai trò `SuperAdmin` | `USER.SUPERADMIN_LOCK_FORBIDDEN` | *Chỉ SuperAdmin mới được khoá tài khoản có vai trò SuperAdmin.* |
| 4 | Không ai được **tự khoá** tài khoản của chính mình — áp cho **mọi** role | `USER.SELF_LOCK_FORBIDDEN` | *Bạn không thể tự khoá tài khoản của chính mình. Nếu muốn kết thúc phiên làm việc, hãy đăng xuất.* |

Cả 3 `message` đều đã nói rõ lý do và lối ra, **FE hiển thị thẳng `message` là đủ**, không cần
map lại theo `businessCode`.

**Luật 2 là ca "hạ quyền im lặng"** mà FE phát hiện: luật §trên cho qua vì người gọi đúng là
`SuperAdmin`, nên trước đây không có lỗi nào bật ra. Nay chặn ở tầng dữ liệu — việc FE bảo toàn
role trên form vẫn cần (tránh cho người dùng gặp lỗi vô cớ), nhưng không còn là lớp chặn duy nhất.

Không chặn nhầm — vẫn cho qua bình thường: `SuperAdmin` sửa email của **chính mình** mà giữ
nguyên `SuperAdmin`; `SuperAdmin` gỡ `SuperAdmin` của **người khác**; `Admin` tự gỡ role `Admin`
của chính mình (chỉ tư cách `SuperAdmin` mới được bảo vệ); `Admin` khoá user thường.

### `POST /api/users/{id}/unlock` — CỐ Ý không chặn

Mở khoá **không** áp luật nào: nó đi theo chiều **khôi phục** quyền truy cập (chặn nó là chặn
đúng đường sửa sai) và không cấp thêm gì cho người gọi. Rủi ro đã cân nhắc và chấp nhận: `Admin`
mở khoá được một `SuperAdmin` vừa bị khoá có chủ đích — đó là hoàn tác một hành động quản trị,
không phải chiếm quyền, và người khoá vẫn khoá lại được. Đây là **quyết định**, không phải chỗ
bị sót; có test chốt hành vi (`SuperAdminAccountProtectionTests` §Unlock).

**Đã cân nhắc và LOẠI:** luật "không được hạ/khoá `SuperAdmin` **cuối cùng**" — phải đếm toàn
bảng mỗi lần ghi cộng bài toán race giữa 2 request đồng thời, mua quá ít an toàn so với chi phí
(người dùng chốt 2026-08-19).

Test: `Tests/PlatformManager.Core.UnitTests/Users/SuperAdminAccountProtectionTests.cs`.

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
kèm lỗi chi tiết từ Identity — vd password không đủ mạnh),
`USER.SUPERADMIN_ROLE_CHANGE_FORBIDDEN` (403 — `Admin` xin cấp `SuperAdmin`, xem §Luật cấp/gỡ
role `SuperAdmin`).

## `PUT /api/users/{id}`

Request: `{ "email": "...", "fullName": "...", "roles": ["User", "Admin"] }` — KHÔNG đổi
`userName`/mật khẩu qua đây. `Data: true`.

`roles` là danh sách **trọn gói** (thay thế toàn bộ role hiện có), không phải delta. Lỗi:
`USER.NOT_FOUND` (404), `USER.SUPERADMIN_ROLE_CHANGE_FORBIDDEN` (403 — cả khi thêm lẫn khi gỡ
`SuperAdmin`, xem §Luật cấp/gỡ role `SuperAdmin`).

### ⏱️ Đổi `roles` → phiên của user đó bị chấm dứt trong **≤ 30 phút**

Role của user nằm **trong cookie phiên** chứ không đọc lại từ DB mỗi request. Khi `PUT` làm
**tập role thực sự thay đổi** (thêm hoặc gỡ), phiên đang chạy của user đích bị **chấm dứt**
trong vòng ~30 phút — họ phải đăng nhập lại và nhận role mới. Không tức thì; cơ chế và lý do
xem `doc/huong_dan/wiki-core/be/02-identity-auth.md` §"Vòng đời phiên đăng nhập".

- **Chỉ sửa `email`/`fullName`** (tập role giữ nguyên) → **không** ảnh hưởng phiên nào. Người
  dùng không bị đăng xuất vì bị sửa tên.
- **Người thao tác tự đổi role của chính mình** → **phiên của chính họ cũng bị chấm dứt**. Đây
  là quyết định có chủ đích, không có ngoại lệ cho "chính mình". FE nên lường trước: sau thao
  tác này, phía người đó có thể nhận 401 và bị đưa về màn đăng nhập.
- Khác với `roles`, **ma trận phân quyền** (`permissions.md` — role × menu, role × resource)
  có hiệu lực **NGAY** vì được đọc thẳng từ DB mỗi request. Đừng gộp 2 thứ này làm một khi
  giải thích cho người dùng.

## `POST /api/users/{id}/lock` / `POST /api/users/{id}/unlock`

Không cần body. Khoá qua `UserManager.SetLockoutEndDateAsync` (không thêm cột `IsActive`
riêng — xem `doc/ERD/ERD-corebase.md` §1.2). `Data: true`.

Lỗi của `lock`: `USER.NOT_FOUND` (404), `USER.SUPERADMIN_LOCK_FORBIDDEN` (403),
`USER.SELF_LOCK_FORBIDDEN` (403) — xem §Bảo vệ tài khoản quản trị. `unlock` chỉ có
`USER.NOT_FOUND` (cố ý không chặn gì thêm).

### ⏱️ Khoá KHÔNG có hiệu lực tức thì với phiên đang chạy — trong vòng **≤ 30 phút**

Hệ thống dùng **cookie session**, danh tính được khôi phục từ chính cookie chứ không tra DB
mỗi request. Vì vậy `lock` **không đá được ngay** người đang online: phiên hiện tại của họ còn
sống thêm **tối đa ~30 phút** rồi mới bị chấm dứt (`SecurityStampValidator` chạy theo chu kỳ
mặc định 30 phút của Identity). Khi bị chấm dứt, request kế tiếp của họ trả **401** như chưa
đăng nhập.

Ngưỡng 30 phút là **chính sách đã chốt**, không phải bug — xem
`doc/huong_dan/wiki-core/be/02-identity-auth.md` §"Vòng đời phiên đăng nhập" cho cơ chế và lý
do. Riêng `POST /api/auth/login` thì bị chặn **ngay lập tức** (`AUTH.LOCKED_OUT`, xem
`auth.md`) — khoá luôn tức thì với *lần đăng nhập mới*, chỉ có phiên *đang chạy* mới trễ.

**FE lưu ý:** đừng hứa với người thao tác rằng nạn nhân "đã bị đăng xuất ngay". Nếu màn Quản
trị người dùng có thông báo sau khi khoá, dùng câu kiểu *"Đã khoá tài khoản. Phiên đang đăng
nhập của người dùng này sẽ bị chấm dứt trong vòng 30 phút."* — nói đúng sự thật rẻ hơn nhiều
so với việc quản trị viên tưởng đã chặn xong rồi phát hiện chưa.

`unlock` không có độ trễ nào cần lưu ý: nó chỉ ảnh hưởng tới lần đăng nhập sau.

## Lỗi chung

`USER.NOT_FOUND` (404) khi `{id}` không tồn tại.

## Câu hỏi mở gửi `frontend-expert`

Chưa xác nhận: màn "Quản trị người dùng" có cần hiển thị badge "Đang hoạt động"/"Đã khoá"
suy từ `isLocked` (đã có sẵn field) hay tự tính lại từ field khác — mặc định dùng thẳng
`isLocked` đã trả sẵn, không cần tính lại phía FE.

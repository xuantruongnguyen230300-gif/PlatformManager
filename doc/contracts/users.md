---
kind: luat
scope: core
verified: 2026-09-06
---

# API Contract Card — Users (Quản trị người dùng)

**Status: AGREED** (2026-08-16) — Application (P2) + Infrastructure (P3) + Api (P4) đã code
xong, build xanh, pipeline auth/envelope đã verify thật (2026-08-16) (xem `auth.md`). Chưa chuyển
IMPLEMENTED vì chưa gọi thử được response THÀNH CÔNG có data thật (cần DB đã migrate + có
user) — `backend-expert` sẽ cập nhật ví dụ response thật + đổi status khi người dùng xác nhận
đã chạy `doc/cau-truc-database.sql` (DDL viết tay) + `dotnet ef database update`.

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

> **Đồng bộ code ↔ card 2026-08-29 (finding D-5).** `UserErrors.cs` trước đó trả bản NGẮN HƠN,
> mất phần chỉ đường (*"Không thể tự gỡ quyền SuperAdmin của chính mình."*). Đã sửa **code cho
> khớp card**, không sửa card cho khớp code: card đã chốt "FE hiển thị thẳng `message`", nên câu
> chữ BE trả về **chính là** giao diện người dùng đọc — bản cụt chỉ nói "không được" mà không nói
> phải làm gì tiếp. Cột `message` ở bảng trên là **nguồn**; đổi câu chữ thì đổi ở đây trước.
> Có test so khớp nguyên văn:
> `src/BE/Tests/PlatformManager.Core.UnitTests/Users/SuperAdminAccountProtectionTests.cs`
> (`GuardMessages_MatchContractCardVerbatim`).

> ### 🔄 LẬT 2026-09-03 — `businessCode` là hợp đồng, `message` KHÔNG còn là nguồn
>
> Hai câu ngay trên đây bị lật, chép lại nguyên văn để không ai phải đoán đã lật cái gì:
>
> - `doc/contracts/users.md:77` — *"Cả 3 `message` đều đã nói rõ lý do và lối ra,
>   **FE hiển thị thẳng `message` là đủ**, không cần map lại theo `businessCode`."*
> - `doc/contracts/users.md:84` — *"Cột `message` ở bảng trên là **nguồn**; đổi câu
>   chữ thì đổi ở đây trước."*
>
> **Vì sao đổi.** Quyết định người dùng 2026-09-03: đổi ngôn ngữ **ngay trong app**,
> mỗi người tự chọn, **không tải lại trang**. Dưới cơ chế đó, một câu tiếng Việt do BE
> trả về là chuỗi **không dịch được** — nó đã cố định trước khi người dùng chọn ngôn
> ngữ, và không có gì ở FE tra ngược lại được. Vai hai cột vì vậy đảo:
>
> | | Trước 2026-09-03 | Từ 2026-09-03 |
> | --- | --- | --- |
> | `businessCode` | Thông tin phụ, FE không cần dùng | **Hợp đồng** — FE tra nó ra câu chữ theo ngôn ngữ đang chọn |
> | `message` | **Nguồn** câu chữ người dùng đọc | **dev-facing + fallback** khi FE chưa có bản dịch cho mã đó |
>
> **Cái gì KHÔNG đổi, để khỏi dọn nhầm:** ba `message` ở bảng trên **giữ nguyên câu
> chữ** và test `GuardMessages_MatchContractCardVerbatim` **giữ nguyên** — fallback vẫn
> phải là câu tử tế, và tiếng Việt vẫn là ngôn ngữ mặc định. Thứ đổi là **ai được coi
> là nguồn**: từ nay thêm/sửa một luật ở đây thì phần bắt buộc là **mã**, câu chữ đi
> kèm là bản mặc định.
>
> **Cùng đợt, cùng lý do:** mục *"FE cần làm"* ở §`lock`/`unlock` dưới đây (`doc/contracts/users.md:305`)
> cũng viết *"FE hiển thị thẳng `message`"* — đọc lại theo bảng trên, không đọc theo nghĩa cũ.
>
> 📖 Lý do đầy đủ + ba lỗi tiên quyết phía BE:
> [`../huong_dan/wiki-core/be/16-i18n-va-ma-loi.md`](../huong_dan/wiki-core/be/16-i18n-va-ma-loi.md)

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

CamelCase (`data,message,status,code,businessCode,traceId,retryable,fields,fieldErrors,
messageParams`) — xem `auth.md`, gồm cả bảng "trường nào có mặt khi nào". ✅ Trường
`messageParams` (tham số của câu thông báo) **đã thi công 2026-09-05**: nó chỉ xuất hiện ở lỗi
mang tham số — `USER.DUPLICATE_USERNAME` kèm `{"UserName":"…"}`, `USER.DUPLICATE_EMAIL` kèm
`{"Email":"…"}`. Các ví dụ response trong card này **chưa** cập nhật để có nó. Định nghĩa ở
`doc/huong_dan/quy-uoc/be-api-controller.md` §Envelope response.

> **✅ `{"Reasons":"…"}` đã biến mất — thi công 2026-09-05.** `USER.CREATE_FAILED` và
> `USER.UPDATE_FAILED` không còn tham số nào; câu của chúng là câu cố định
> (`"Tạo người dùng thất bại."` / `"Cập nhật người dùng thất bại."`). Từng **mã lỗi Identity**
> (`DuplicateUserName`, `PasswordTooShort`, `ConcurrencyFailure`…) đi ra `fieldErrors` với `code`
> là chính mã đó, gắn vào ô nhập mà mã đó nói tới:
>
> ```json
> {
>   "businessCode": "USER.CREATE_FAILED",
>   "message": "Tạo người dùng thất bại.",
>   "fieldErrors": {
>     "UserName": [{ "code": "DuplicateUserName", "message": "DuplicateUserName" }],
>     "TempPassword": [{ "code": "PasswordTooShort", "message": "PasswordTooShort" }]
>   }
> }
> ```
>
> Khoá là tên ô PascalCase, khớp `UserFormField` phía FE. Mã **không thuộc ô nhập nào**
> (`ConcurrencyFailure` — nó nói về bản ghi) rơi vào khoá `"$record"`; khoá đó cố ý bắt đầu bằng
> `$` để không bao giờ trùng tên một property thật. Envelope **không** đổi hình dạng —
> `fieldErrors` đã có sẵn từ 2026-09-03.
>
> ⚠️ **FE hôm nay chưa đọc `fieldErrors`** (còn bind từ `fields`), nên màn hình chưa đổi. Quyết
> định + lý do + nghiệm thu:
> [`../huong_dan/wiki-core/be/16-i18n-va-ma-loi.md`](../huong_dan/wiki-core/be/16-i18n-va-ma-loi.md) §11.

## `GET /api/users?page=1&pageSize=20&searchText=...`

`Data: PagedList<UserDto>` — `{ items, page, pageSize, totalCount }` (shape chuẩn duy nhất, xem `doc/huong_dan/quy-uoc/be-cqrs-handler.md` §Shape phân trang). Mỗi `UserDto`:

```json
{
  "id": "guid", "userName": "nguyen.van.a", "email": "...", "fullName": "Nguyễn Văn A",
  "roles": ["User"], "isLocked": false, "mustChangePassword": true, "dateCreate": "2026-08-16T..."
}
```

### Giới hạn phân trang — `pageSize` tối đa **200**, `page` tối thiểu **1** (BE enforce từ 2026-08-29)

| Tham số | Hợp lệ | Ngoài khoảng | Bỏ trống |
| --- | --- | --- | --- |
| `page` | `>= 1` | **400 `ValidationError`** kèm `fields.Page` | mặc định `1` |
| `pageSize` | `1..200` | **400 `ValidationError`** kèm `fields.PageSize` | mặc định `20` |

**FE không phải sửa gì** — ô chọn số dòng cho tối đa 50 (`[rowsPerPageOptions]="[10, 20, 50]"`),
nằm gọn trong khoảng hợp lệ.

Trước bản này **không có trần nào**: handler chỉ vá `pageSize <= 0` về 20, nên `?pageSize=1000000`
đi thẳng xuống `Take(1000000)` và kéo trọn bảng user trong một request (finding BE-3). Chọn **200**
vì nó dư 4 lần so với tuỳ chọn lớn nhất trên UI — đủ cho script quản trị/kiểm tra thủ công mà
không ai phải xin nới — trong khi vẫn chặn ca kéo cả bảng. Muốn hơn 200 dòng/trang thì phải nới ở
`GetUsersListValidator.MaxPageSize` **và** sửa mục này, không vá riêng ở handler.

Giá trị `0`/số âm nay cũng là **lỗi đầu vào**, không còn bị vá âm thầm về mặc định: giá trị hợp lệ
chỉ được quyết ở MỘT nơi (validator). Vá âm thầm khiến người gọi sai không bao giờ biết mình gửi
sai, và khiến hai nơi cùng "sở hữu" một quy tắc.

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

Lỗi: `USER.DUPLICATE_USERNAME` (409), `USER.DUPLICATE_EMAIL` (409), `USER.CREATE_FAILED` (422 —
Identity từ chối, vd password không đủ mạnh; ✅ từ 2026-09-05 *"chi tiết"* nằm ở `fieldErrors`
theo từng ô, `message` là câu cố định — xem §Envelope bên trên),
`USER.SUPERADMIN_ROLE_CHANGE_FORBIDDEN` (403 — `Admin` xin cấp `SuperAdmin`, xem §Luật cấp/gỡ
role `SuperAdmin`).

## `PUT /api/users/{id}`

Request: `{ "email": "...", "fullName": "...", "roles": ["User", "Admin"] }` — KHÔNG đổi
`userName`/mật khẩu qua đây. `Data: true`.

`roles` là danh sách **trọn gói** (thay thế toàn bộ role hiện có), không phải delta. Lỗi:
`USER.NOT_FOUND` (404), `USER.SUPERADMIN_ROLE_CHANGE_FORBIDDEN` (403 — cả khi thêm lẫn khi gỡ
`SuperAdmin`, xem §Luật cấp/gỡ role `SuperAdmin`), **`USER.UPDATE_FAILED` (422)** khi tầng ghi
Identity từ chối (vd `ConcurrencyFailure`).

> **✅ Quyết định người dùng 2026-09-05 — `USER.UPDATE_FAILED` nay NÓI ĐƯỢC vì sao. Đã thi công.**
> Trước đó câu trả về là *"Cập nhật người dùng thất bại: không lưu được thay đổi"* — nói hai lần
> cùng một điều, vì `IUserAdminService.UpdateAsync` trả `bool` trần nên mã lỗi Identity bị vứt
> ở **năm** chỗ trong tầng ghi, trước khi tới được handler; handler không có gì để báo nên phải
> bịa một câu tại chỗ gọi. Nay `UpdateAsync` trả `UpdateUserOutcome` (đúng khuôn `CreateAsync`
> ngay cạnh), mã thật đi ra `fieldErrors`, và `{Reasons}` đã bỏ khỏi descriptor. Đây là thay đổi
> **nội bộ BE** — `IUserAdminService` là interface của `Core.Application`, envelope không đổi.
>
> **Một mã lỗi ĐỔI Ý NGHĨA ở endpoint này:** bản ghi biến mất **giữa** lần đọc của handler và lần
> ghi (bị xoá xen vào) nay trả `USER.NOT_FOUND` (404) thay vì `USER.UPDATE_FAILED` (422). Trước
> đây hai ca đó gộp làm một vì `bool` không phân biệt được. Lý do đầy đủ + nghiệm thu:
> [`../huong_dan/wiki-core/be/16-i18n-va-ma-loi.md`](../huong_dan/wiki-core/be/16-i18n-va-ma-loi.md) §11.

> **`USER.UPDATE_FAILED` thêm 2026-08-29 (finding BE-8).** Trước đó đường sửa mượn mã của đường
> tạo, nên người dùng đang **sửa** một tài khoản đọc được câu *"Tạo người dùng thất bại: cập nhật
> thất bại"* và FE không phân biệt được hai đường bằng `businessCode`.

### ✅ Quyết định người dùng 2026-08-30 — ghi quyền phải kiểm kết quả, và phải nằm trong transaction (ĐÃ THI CÔNG, đối chiếu 2026-09-06)

**Đây là finding BE-4 lặp lại ở đường `PUT`, chỗ đợt 2026-08-29 không rà tới.**
§`lock`/`unlock` bên dưới đã đóng đúng lớp lỗi này cho hai endpoint đó; đường sửa
người dùng thì chưa.

`UserAdminService.UpdateAsync` ghi bốn lần. Hai lệnh đầu kiểm `.Succeeded`, hai lệnh
**ghi role** thì không, và hàm trả `true` bất kể chúng thành hay bại
(`src/BE/Core/PlatformManager.Core.Infrastructure/Identity/UserAdminService.cs`).

> **Đoạn trên mô tả hiện trạng NGÀY 2026-08-30, không phải hôm nay.** Quyết định 1 và 2 đã thi
> công 2026-08-31: cả bốn lệnh ghi đều kiểm `.Succeeded` và nằm trong một transaction. Cái
> **còn lại** là kiểu trả về: hàm khi đó vẫn là `Task<bool>`, nên `.Succeeded` bị rút gọn về
> `false` và mã lỗi Identity vẫn bị vứt — ✅ đã đổi sang `Task<UpdateUserOutcome>` ngày
> **2026-09-05**, xem ghi chú ở §`PUT /api/users/{id}` bên trên và
> [`../huong_dan/wiki-core/be/16-i18n-va-ma-loi.md`](../huong_dan/wiki-core/be/16-i18n-va-ma-loi.md) §11.
> (Số dòng cũ `:163` đã gỡ — nó trỏ sai từ lâu; neo bằng tên hàm, đúng bài học §10.9 của file
> chủ kia.)

Hậu quả nặng nhất nằm ở đúng thứ tự hiện tại — gỡ role trước, thêm role sau:

| Bước 3 (`RemoveFromRolesAsync`) | Bước 4 (`AddToRolesAsync`) | Người dùng còn lại gì | API trả |
|---|---|---|---|
| OK | OK | Đúng tập role mới | `200` ✓ |
| OK | **hỏng** | **Ít role hơn lúc đầu, có thể là không role nào** | `200` — vẫn báo thành công |

Không ai biết cho tới khi chính người đó phát hiện mình không vào được đâu nữa. Và
vì `200` nên FE hiện toast *"Đã lưu"* đúng theo hợp đồng.

Đảo thứ tự **không** cứu được: thêm trước rồi gỡ hỏng thì người đó giữ lại quyền lẽ
ra bị thu hồi — leo thang quyền, tệ hơn.

#### Chốt

| # | Quyết định |
|---|---|
| 1 | Kiểm `.Succeeded` cho **cả hai** lệnh ghi role; thất bại ⇒ `USER.UPDATE_FAILED` (422), không phải `200` |
| 2 | Bọc **cả bốn** lệnh ghi trong một transaction. Hỏng bất kỳ bước nào ⇒ hoàn tác sạch, **kể cả con dấu bảo mật** — người dùng không bị đăng xuất vì thao tác của người khác thất bại |
| 3 | Thêm kiểm tranh chấp ghi: `GET` trả kèm **`version`**, `PUT` gửi lại; lệch ⇒ **409** |

Quyết định 3 dùng `ConcurrencyStamp` mà ASP.NET Identity **đã có sẵn** trên
`AspNetUsers` — không thêm cột, không cần migration.
Cùng mẫu với ma trận phân quyền, xem [`permissions.md`](permissions.md)
§"Quyết định người dùng 2026-08-30".

> 📖 Vì sao tiền đề *"buộc phải chọn thứ tự vì không có transaction"* là sai, và vì
> sao bảng chọn thứ tự vẫn giữ giá trị: đọc
> [`../huong_dan/wiki-core/be/02-identity-auth.md`](../huong_dan/wiki-core/be/02-identity-auth.md)
> §"Thứ tự: con dấu TRƯỚC, quyền/khoá SAU"

#### ✅ Cả ba quyết định ĐÃ THI CÔNG — đối chiếu 2026-09-06

| | Bằng chứng |
|---|---|
| Ghi role hỏng ⇒ **422** `USER.UPDATE_FAILED`, không còn `200` | `src/BE/Core/PlatformManager.Core.Infrastructure/Identity/UserAdminService.cs:282-283,289-290` |
| Cả bốn lệnh ghi trong **một** transaction, hoàn tác sạch | cùng file `:249` (`BeginTransactionAsync`) → `:293` (`CommitAsync`) |
| Hai admin sửa cùng một người ⇒ **409** `USER.VERSION_CONFLICT` | `src/BE/Core/PlatformManager.Core.Application/Users/UpdateUserCommand.cs:62-63` |
| `GET /api/users` (danh sách) **có** `version` | `src/BE/Core/PlatformManager.Core.Application/Users/UserDto.cs:21` |

**Vẫn đúng, đừng gỡ:** **không có endpoint lấy một người dùng theo id** — `UsersController` chỉ
khai `[HttpGet]` danh sách, `[HttpPost]`, `[HttpPut("{id:guid}")]`, `[HttpPost("{id:guid}/lock")]`,
`[HttpPost("{id:guid}/unlock")]` (`src/BE/PlatformManager.Api/Controllers/UsersController.cs:17,21,25,29,33`).
Sửa 2026-09-01: ba chỗ trong repo từng mô tả `GET /api/users/{id}` như thể nó tồn tại.

**Một chỗ LỎNG hơn card, có chủ đích:** handler chỉ kiểm `version` khi client **có gửi** nó
(`cmd.Version is not null`, `UpdateUserCommand.cs:62`) — bỏ trống vẫn ghi đè được. Mỏ neo cố ý là
test `UserUpdateVersionTests.Put_WithoutVersion_StillSucceeds`; siết lại thì test đó phải đỏ.

✅ **FE đã bật lớp bảo vệ này — CÓ THẬT (đối chiếu 2026-09-08).** `IUser.Version` và
`IUpdateUserPayload.Version` đã tồn tại
(`src/FE/src/app/platform/quan-tri-nguoi-dung/models/quan-tri-nguoi-dung.model.ts`), mapper đọc
`version` từ envelope, và `PUT` từ màn Quản trị người dùng **luôn gửi khoá `version`** —
`stamp` thật khi có, `null` khi BE không cấp token. Trang lấy giá trị từ **bản ghi đang mở
form** (`formEditing`), không tra lại danh sách hiện tại: tra lại sẽ lấy nhầm bản vừa bị người
khác ghi đè và làm 409 không bao giờ xảy ra.

Ba test khoá lại đường này, vì nó gãy **im lặng** — bỏ `version` đi thì build, lint và toast
"Đã cập nhật" đều không đổi, chỉ có lớp chống ghi đè biến mất:
`services/quan-tri-nguoi-dung.service.spec.ts` (*"LUÔN gửi khoá `version`"*) và
`pages/quan-tri-nguoi-dung/quan-tri-nguoi-dung.page.spec.ts`
§"token chống ghi đè đi tới tận request".

**Hành vi 409 thì giữ nguyên hợp đồng, không thêm gì:** FE hiện `message` của BE trong form
(và trên toast qua `httpErrorInterceptor`) — cùng đường với 400/422, form **không** tự đóng.
Card này không mô tả hành vi màn hình nào khác cho ca tranh chấp, nên FE cố ý không phát minh
thêm. ⚠️ Hệ quả đã biết, chưa chốt cách xử lý: form giữ `version` cũ sau 409 nên bấm Lưu lại sẽ
409 tiếp cho tới khi người dùng đóng form và mở lại — đúng lối ra mà `message` của BE hướng dẫn
(*"Hãy tải lại danh sách rồi thực hiện lại"*).

> 🔄 **LẬT 2026-09-08.** Đoạn này trước ghi *"FE hôm nay đang ở đúng nhánh lỏng đó (đối chiếu
> 2026-09-06) … việc còn lại thuộc `frontend-expert`"*. Việc đó đã làm xong.
>
> **Điều KHÔNG đổi:** handler vẫn chỉ kiểm khi client có gửi token (`cmd.Version is not null`),
> và mỏ neo `UserUpdateVersionTests.Put_WithoutVersion_StillSucceeds` vẫn xanh. Nay FE đã gửi
> thật, nên bước siết `version` thành **bắt buộc** đã hết vật cản đã nêu ở
> `UpdateUserCommand.Version` (*"bắt buộc ngay hôm nay sẽ làm MỌI lệnh sửa người dùng trả 409"*)
> — nhưng đó là **quyết định của BE**, chưa chốt, và chốt nó là lúc mỏ neo kia phải đỏ.

> 🔄 **SỬA 2026-09-06.** Mục này trước đây là bảng *"Có thật hôm nay (đối chiếu 2026-08-30) → sẽ
> thành"* với cả bốn dòng cột trái đã hết đúng: quyết định 1 và 2 thi công 2026-08-31, quyết định
> 3 (`version`) cũng vậy. Một bảng "sẽ thành" cho việc đã xong là đúng khuôn §4 của
> `.claude/CLAUDE.md` cấm — nó bảo người đọc rằng lớp bảo vệ chưa có, trong khi nó có.

#### Chưa chốt

| Câu hỏi | Vì sao để ngỏ |
|---|---|
| `ConcurrencyFailure` nội bộ của Identity hiện trả **422**; kiểm `version` tường minh sẽ trả **409**. Có gộp về một mã không? | Cùng nghĩa với người dùng, nhưng gộp là thay đổi phá vỡ tương thích với FE đang chạy — cần quyết riêng |

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
riêng — xem `doc/cau-truc-database.md` §4.1). `Data: true`.

Lỗi của `lock`: `USER.NOT_FOUND` (404), `USER.SUPERADMIN_LOCK_FORBIDDEN` (403),
`USER.SELF_LOCK_FORBIDDEN` (403) — xem §Bảo vệ tài khoản quản trị — và **`USER.LOCK_FAILED`
(422)** khi tầng ghi thất bại. `unlock` không chặn gì thêm (cố ý), chỉ có `USER.NOT_FOUND` (404)
và **`USER.UNLOCK_FAILED` (422)**.

### ⚠️ Thao tác hỏng nay là LỖI, không còn là `200` + `data: false` (2026-08-29, finding BE-4)

Trước bản này cả hai handler trả `Ok(ok)`, nên khi tầng ghi Identity thất bại
(`UpdateSecurityStampAsync`/`SetLockoutEndDateAsync`) client vẫn nhận **HTTP 200 với
`data: false`**. FE map `data` thành `undefined` rồi hiện toast *"Đã khoá tài khoản."* — **quản
trị viên tin là đã khoá trong khi chưa khoá**.

Đường `lock` đắt hơn nữa vì nó đổi con dấu bảo mật **TRƯỚC** khi đặt lockout: hỏng giữa chừng để
lại trạng thái nửa vời — người dùng bị chấm dứt phiên (≤ 30 phút) nhưng **vẫn đăng nhập lại
được**. Vì vậy `USER.LOCK_FAILED` mang câu chữ nhắc kiểm lại trạng thái tài khoản trước khi coi
là đã khoá.

**FE cần làm:** chỉ hiện toast thành công khi envelope `status`/`code` báo thành công, không suy
từ việc "gọi xong không lỗi mạng". Hai mã mới là `BusinessRuleError` (422), FE hiển thị thẳng
`message` như các lỗi nghiệp vụ khác.

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

## CONTRACT USER-6 — Lọc danh sách theo Vai trò + Trạng thái (mở rộng `GET /api/users`)

- Status: **AGREED** (chốt 2026-08-29 bởi `backend-expert`; phát hành DRAFT cùng ngày bởi
  `frontend-expert`) — BE đã code, build xanh, có test tự động phủ cả 4 ca lọc. Chưa
  IMPLEMENTED vì chưa gọi thử qua HTTP trên DB thật (cùng lý do với phần đầu file này).
- Owner FE: `src/FE/src/app/platform/quan-tri-nguoi-dung/services/quan-tri-nguoi-dung.service.ts`
- Route: `GET /api/users` — **mở rộng query hiện có**, không thêm endpoint mới
- Verb: GET
- Request (query, tất cả optional — bổ sung 2 tham số vào bộ đã có `page`/`pageSize`/`searchText`):
    `role: string?` — tên role đúng casing BE (`SuperAdmin` · `Admin` · `User`); bỏ trống = mọi vai trò
    `isLocked: bool?` — `true` chỉ tài khoản đã khoá · `false` chỉ tài khoản đang hoạt động · bỏ trống = tất cả
- Response: **không đổi** — `PagedList<UserDto>` như mục `GET /api/users` ở trên. `totalCount`
  là tổng SAU khi lọc (đếm trên toàn bảng trước `Skip/Take`), nên thanh phân trang báo đúng số trang.
- Gửi cả `role` lẫn `isLocked` → hai điều kiện **giao nhau (AND)**. Không gửi tham số nào thì
  tham số đó không sinh điều kiện — hành vi cũ giữ nguyên 100%.
- User mang **nhiều role** chỉ xuất hiện **một lần** khi lọc theo một trong các role đó (lọc
  bằng subquery id, không join bung dòng) — nếu không, `totalCount` sẽ đếm dư.

### 🔒 Chốt câu hỏi để ngỏ: `role` không hợp lệ → **400 `ValidationError`**, không phải "trả rỗng"

`role` không thuộc `SuperAdmin`/`Admin`/`User` (kể cả sai chữ hoa/thường) bị chặn ở validator
**trước** handler → envelope lỗi `code: "ValidationError"` (400) kèm `fields.Role`, message
*"Vai trò lọc không hợp lệ — chỉ nhận SuperAdmin/Admin/User."*. Chuỗi rỗng/khoảng trắng thuần
vẫn là "không lọc", không phải giá trị sai.

**Đây là điểm DUY NHẤT lệch so với card DRAFT**, vốn ghi *"không có lỗi mới"*. Lý do chọn báo
lỗi thay vì hai phương án card đề xuất:

| Phương án | Chuyện gì xảy ra khi FE (hoặc một URL bookmark cũ) gửi `role=Adminn` |
| --- | --- |
| Bỏ qua bộ lọc | Trả **toàn bộ** danh sách. Người dùng thấy có dữ liệu, tưởng bộ lọc chạy. |
| Trả rỗng | Trả **0 dòng**. Người quản trị đọc thành một sự thật sai — *"hệ thống không có Admin nào"* — rồi hành động theo nó. |
| **400 + `fields.Role`** | Nói thẳng tham số nào sai. Lỗi lộ ra ở lần chạy đầu tiên. |

Hai phương án đầu đều im lặng; khác nhau chỉ ở việc câu trả lời sai theo hướng nào. Ngoài ra
repo này **đã có sẵn đúng một luật** cho giá trị role không hợp lệ — `CreateUserValidator`/
`UpdateUserValidator` trả `ValidationError` 400 kèm `fields` (xem §Luật cấp/gỡ role `SuperAdmin`,
đoạn nói về casing) — nên xử lý khác đi trên cùng tập giá trị ở endpoint list là drift.

**FE không phải sửa gì**: FE chỉ gửi 3 giá trị hợp lệ nên đường 400 này trên thực tế không chạy;
nó tồn tại để một lần gõ sai không biến thành dữ liệu sai. Nếu về sau FE cho người dùng nhập tự
do vào ô vai trò thì mới cần bind `fields.Role`.

> ⚠️ **Key của `fields` là PascalCase — `Role`, không phải `role`** (sửa 2026-08-29, finding D-4;
> 3 chỗ trong file này trước đó ghi sai). Envelope serialize camelCase, nhưng `Fields` là
> `Dictionary<string,string[]>` và `DictionaryKeyPolicy` cố ý **không** set, nên key giữ nguyên
> tên property C# gốc để FE bind thẳng vào control trên form. Áp cho mọi field, không riêng
> `Role`: `Page`, `PageSize`, `FullName`, `Email`… Cơ chế ở
> `src/BE/PlatformManager.Api/Common/GlobalExceptionHandler.cs` (`NormalizeField`).

### Hiện thực (2026-08-29)

- `GetUsersListQuery(Page, PageSize, SearchText, Role, IsLocked)` —
  `src/BE/Core/PlatformManager.Core.Application/Users/GetUsersListQuery.cs:17`;
  validator `GetUsersListValidator` cùng file dòng 56, trần `MaxPageSize = 200` ở dòng 59
  (đối chiếu lại 2026-09-06).
- Hai tham số đi xuống service gói trong `UserListFilter` —
  `src/BE/Core/PlatformManager.Core.Application/Users/IUserAdminService.cs:57`
  (`SearchText` và `Role` cùng kiểu `string?`, gói record để hoán vị nhầm không còn biên dịch được).
- Lọc dịch sang SQL ở `src/BE/Core/PlatformManager.Core.Infrastructure/Identity/UserAdminService.cs:51`
  (role, subquery `AspNetUserRoles`/`AspNetRoles` theo `NormalizedName`) và dòng 69 (`isLocked`,
  cùng định nghĩa `LockoutEnd` với `UserDto.IsLocked`). `CountAsync` chạy sau khi áp hết điều
  kiện, trước `Skip/Take`.
- Role của cả trang lấy bằng **một** query (join `AspNetUserRoles`/`AspNetRoles` theo tập id của
  trang) rồi dựng DTO từ dictionary — sửa 2026-08-29, finding BE-2: bản trước gọi
  `UserManager.GetRolesAsync` cho **từng dòng**, tức `pageSize=20` thành 21 round-trip.
  Đường **một** user (`IUserAdminService.GetByIdAsync`,
  `src/BE/Core/PlatformManager.Core.Infrastructure/Identity/UserAdminService.cs:32`) giữ nguyên
  `GetRolesAsync` (1 user, không có N+1 để tránh) — đây là **phương thức service**, hiện **không**
  có route HTTP nào gọi tới (xem §"Cả ba quyết định đã thi công" bên trên).

  > 🔄 **SỬA 2026-09-06.** Ba số dòng ở hai gạch đầu dòng trên (`IUserAdminService.cs:16`,
  > `UserAdminService.cs:32` cho bộ lọc role, `dòng 50` cho `isLocked`) đều trỏ sai; và dòng cuối
  > gọi `GET /api/users/{id}` là "đường", trong khi chính file này đã ghi rõ endpoint đó không tồn
  > tại — một file tự mâu thuẫn với chính nó cách nhau 90 dòng.
- Controller **không đổi**: `[FromQuery] GetUsersListQuery` tự bind 2 tham số mới.
- Không đổi schema DB, không thêm migration — lọc dùng đúng 7 bảng Identity sẵn có.

Test: `src/BE/Tests/PlatformManager.Core.IntegrationTests/Users/UserListFilterTests.cs`
(Postgres thật: lọc theo role, theo trạng thái, kết hợp, `totalCount` đúng khi phân trang, user
2 role không bị đếm 2 lần) + `src/BE/Tests/PlatformManager.Core.UnitTests/Users/GetUsersListValidatorTests.cs`
(chốt quyết định 400 ở trên, và trần `page`/`pageSize`) +
`src/BE/Tests/PlatformManager.Core.IntegrationTests/Users/UserListRoleBatchingTests.cs`
(đếm THẲNG số lệnh SQL: số round-trip của trang 5 dòng phải bằng trang 1 dòng — đó là định nghĩa
"không N+1"; test này đỏ trên bản hiện thực cũ).

### Ghi chú gốc của card (giữ lại)

- Hiện trạng BE **trước** bản này (2026-08-29): `GetUsersListQuery(int Page, int PageSize,
  string? SearchText)` và `IUserAdminService.GetListAsync(page, pageSize, searchText, ct)` —
  chưa có hai tham số này, nên FE gửi lên bị model binding bỏ qua và danh sách **không được lọc**.
- FE đã dựng sẵn UI (bảng lọc `<details class="filter">` trên `.toolbar`) và đã gửi 2 query
  param theo đúng tên trên. Tên tham số **giữ nguyên** như card DRAFT, FE không phải sửa dòng nào.
- Lọc phải chạy **ở tầng truy vấn**, không lọc sau khi phân trang: lọc trên trang hiện tại là
  kết quả sai (trang 1 lọc ra 3 dòng trong khi DB có 40 dòng khớp).

---
kind: luat
scope: core
verified: 2026-09-06
---

# API Contract Card — Permissions (Phân quyền)

**Status: AGREED** (2026-08-16) — build xanh, envelope/auth pipeline verify thật (xem
`auth.md`). Chuyển IMPLEMENTED khi có DB đã migrate để gọi thử response thật.

Gate: `[Authorize(Roles = "SuperAdmin")]` toàn bộ controller — **CHỈ SuperAdmin**, kể cả
`Admin` cũng bị 403 (tránh leo thang quyền qua UI, xem `doc/ke-hoach-xay-lai-corebase.md`).

## ✅ Quyết định người dùng 2026-08-30 — phiên bản ma trận + đòi phủ đủ (ĐÃ THI CÔNG đủ 3/3, đối chiếu 2026-09-06)

Áp cho **cả hai** `PUT` trong card này (PERM-1 và PERM-2) — chúng cùng một hình
dạng "ghi đè toàn bộ" nên cùng một lỗ hổng. Mục này là nguồn; hai mục endpoint bên
dưới chỉ trỏ về đây.

### Hai sự cố đã bịt — giữ lại vì lý do vẫn là luật

> 🔄 **SỬA 2026-09-06.** Mục này trước đây tên là *"Hai sự cố đang mở"* và mô tả bằng thì hiện
> tại. Cả hai đã bịt (bằng chứng ở §"Đã thi công" bên dưới), và **cả bốn** trích dẫn `file:dòng`
> của nó đều đã trôi — `phan-quyen.page.ts:65`, `phan-quyen.page.html:52` và `:80`,
> `SysMenuRoleRepository.cs:63`. Riêng dòng cuối là loại sai nguy hiểm nhất: nó chống lưng cho
> câu *"`RemoveRange` xoá cả bảng"* trong khi dòng 63 nay là `GetVersionAsync` và `RemoveRange`
> đã bị thay bằng xoá mềm từ 2026-08-31.

**Sự cố 1 — một cú bấm xoá sạch ma trận** (chuỗi đối chiếu 2026-08-30, nay đã cắt ở cả ba mắt
xích): `GET` hỏng ⇒ `loading` về `false` nhưng `rows` vẫn rỗng ⇒ nút Lưu của PERM-1 vẫn bật vì
thiếu `!dirty()` ⇒ gửi `entries: []` ⇒ validator chỉ đòi `NotNull` nên đi qua ⇒ `RemoveRange` xoá
cả bảng. Sidebar của mọi user không phải `SuperAdmin` thành trắng, và không có nhật ký nào để
dựng lại.

Nay: nút Lưu PERM-1 có `!dirty()`
(`src/FE/src/app/platform/phan-quyen/pages/phan-quyen/phan-quyen.page.html:63`, đối xứng với
PERM-2 ở `:114`); `entries` thiếu mục bị chặn ở **validator**, trước khi chạm dữ liệu; và tầng
ghi xoá **mềm** thay vì `RemoveRange`.

**Sự cố 2 — hai người sửa cùng lúc thì người lưu sau xoá mất việc của người lưu trước**, không
cảnh báo, không dấu vết. Gate `SuperAdmin` thu hẹp *ai* ghi được, không thu hẹp *bao nhiêu người*
ghi cùng lúc. Nay chặn bằng `version` (409), và FE **có** gửi nó ở cả hai tab
(`src/FE/src/app/platform/phan-quyen/services/phan-quyen.service.ts:48-53,75-82`) — khác với màn
Quản trị người dùng, nơi lớp bảo vệ cùng loại có ở BE nhưng FE chưa bật (xem `users.md`).

### Chốt

| # | Quyết định | Chặn được |
|---|---|---|
| 1 | `GET` trả kèm **`version`** — chuỗi băm của trạng thái ma trận hiện tại. `PUT` gửi lại đúng chuỗi đó; server tính lại từ DB rồi so, lệch thì **409**, không ghi gì | Sự cố thứ hai |
| 2 | `PUT` đòi payload **phủ đủ** mọi phần tử của ma trận (đủ `SysMenu` với PERM-1, đủ **danh mục permission-key** với PERM-2). Thiếu bất kỳ phần tử nào ⇒ **400** | Sự cố thứ nhất, và cả ca tải thiếu một phần |
| 3 | FE thêm `!dirty()` vào nút Lưu của PERM-1 cho đối xứng với PERM-2 | Sự cố thứ nhất, lớp ngoài |

Quyết định 2 mạnh hơn hẳn *"cấm `entries` rỗng"*: payload tải được 4 trên 7 menu vẫn xoá 3 menu
kia, và `NotEmpty()` không thấy gì bất thường ở payload đó.

Nó cũng **đổi ngữ nghĩa** đã ghi trong card: bỏ một `sysMenuId` khỏi `entries` trước đây có nghĩa
"thu hồi sạch mục đó", nay là **lỗi client**. Muốn thu hồi sạch thì gửi `roles: []` — đúng thứ
thông điệp lỗi của validator vẫn luôn hướng dẫn
(`src/BE/Core/PlatformManager.Core.Application/Permissions/UpdatePermissionMatrixCommand.cs:150`).
Đánh đổi này có chủ đích: một ngữ nghĩa mà **sự vắng mặt** mang ý nghĩa phá huỷ là ngữ nghĩa
không an toàn, vì mọi lỗi tải thiếu đều trở thành lệnh xoá hợp lệ.

### `version` tính thế nào

Băm trên tập cặp đã sắp thứ tự ổn định của chính bảng đó — **không** thêm cột, **không** thêm
bảng.

> 🚧 **Ghi chú 2026-08-31 — căn cứ đã đổi, kết luận thì không.** Bản trước viết
> *"Migration đang đóng băng nên đây là ràng buộc cứng"*. Quyết định 2026-08-30 **mở**
> một cửa sổ đổi schema (baseline lại lịch sử migration), nên lý do đó hết hiệu lực.
> Cách băm **vẫn được giữ**, vì hai lý do còn lại vẫn đúng: không có trạng thái phụ để
> lệch với dữ liệu thật, và nó phát hiện được mọi thay đổi kể cả do SQL tay gây ra.

> 📖 Vì sao token cấp tập hợp chứ không phải `RowVersion` theo dòng, và đánh đổi của cách băm:
> đọc [`../huong_dan/wiki-core/be/06-concurrency-control.md`](../huong_dan/wiki-core/be/06-concurrency-control.md)
> §"Cách chuẩn cho ca ghi đè cả tập"

### ✅ Đã thi công 2026-08-31 — nhưng CHƯA chạy kiểm chứng trên database thật

Phân biệt hai thứ, vì trộn chúng lại đúng là khuôn lỗi §4 của `.claude/CLAUDE.md`:

| | Trạng thái |
|---|---|
| **Code đã có** | ✅ Đối chiếu source 2026-08-31 — xem bảng dưới |
| **Hành vi đã kiểm chứng** | ✅ **2026-09-01** — `dotnet test src/BE/Tests/PlatformManager.Core.IntegrationTests` → **76/76 xanh** trên Postgres thật qua Testcontainers |

Ba thứ **chỉ lộ ra khi chạm database thật** nay đều có test chạy thật canh: 409 khi hai người ghi
cùng lúc; 400 mà bảng **không đổi một dòng nào** (ảnh chụp bảng đọc qua `IgnoreQueryFilters()` nên
thấy được cả dòng đã xoá mềm); và lưu hai lần cùng một cặp không vỡ khoá trùng.

> **Hai lỗi thật lộ ra ở đúng lượt chạy này**, cả hai đều không thể phát hiện bằng build hay unit test:
> `ValidationBehavior` dùng chung một `ValidationContext` nên mọi thông điệp lỗi bị nhân đôi khi một
> lệnh có 2 validator (xem [`../huong_dan/quy-uoc/be-cqrs-handler.md`](../huong_dan/quy-uoc/be-cqrs-handler.md)
> §"Nhiều validator cho MỘT request"); và `PartitionedRateLimiter.CreateChained` không truyền
> metadata `RetryAfter` ra lease gộp, làm mọi 429 mất header đó.

| Thành phần | Ở đâu |
|---|---|
| Tính token băm, kèm cảnh báo cấm `IgnoreQueryFilters()` | `src/BE/Core/PlatformManager.Core.Application/Permissions/MatrixVersion.cs:19` |
| `GET` trả `version` | `src/BE/Core/PlatformManager.Core.Application/Permissions/GetPermissionMatrixQuery.cs:25` |
| Xoá mềm thay `RemoveRange` | `src/BE/Core/PlatformManager.Core.Infrastructure/Persistence/Repositories/SysMenuRoleRepository.cs:91` |
| Rule phủ đủ + rule id lạ, **ở validator nên chặn trước khi chạm dữ liệu** | `src/BE/Core/PlatformManager.Core.Application/Permissions/UpdatePermissionMatrixCommand.cs:98` |

Một bổ sung ngoài quyết định gốc, đã chốt 2026-08-31: **`sysMenuId` không tồn tại ⇒ 400**, trước
đây là 500 do vỡ khoá ngoại — lỗi của client bị báo như lỗi hệ thống.

### ✅ Cả 7 dòng đã về đích — đối chiếu 2026-09-06

| | Bằng chứng |
|---|---|
| `GET` (cả hai) trả `version` | `src/BE/Core/PlatformManager.Core.Application/Permissions/GetPermissionMatrixQuery.cs:25,34` |
| `PUT` (cả hai) nhận `version` | `src/BE/Core/PlatformManager.Core.Application/Permissions/UpdatePermissionMatrixCommand.cs:22`; FE gửi ở `src/FE/src/app/platform/phan-quyen/services/phan-quyen.service.ts:51,78` |
| `ReplaceAllAsync` xoá **mềm**, không `RemoveRange` | `src/BE/Core/PlatformManager.Core.Infrastructure/Persistence/Repositories/SysMenuRoleRepository.cs:91` |
| Hàm băm `version` lọc `IsDeleted = false` (cấm `IgnoreQueryFilters()`) | `src/BE/Core/PlatformManager.Core.Application/Permissions/MatrixVersion.cs:19`; ràng buộc nhắc lại ở `SysMenuRoleRepository.cs:59-64` |
| Payload thiếu phần tử ⇒ **400** ở validator | `UpdatePermissionMatrixCommand.cs:98` (lớp), `:134-150` (chiều thiếu) |
| `sysMenuId` lạ ⇒ **400**, không còn 500 vỡ khoá ngoại | `UpdatePermissionMatrixCommand.cs:120-129` |
| Nút Lưu PERM-1 tắt khi `!dirty()` | `src/FE/src/app/platform/phan-quyen/pages/phan-quyen/phan-quyen.page.html:63` |

> 🔄 **SỬA 2026-09-06.** Mục này trước đây là bảng *"Có thật hôm nay (đối chiếu 2026-08-30) → sẽ
> thành"*. Cột trái mô tả một hệ thống không còn tồn tại từ 2026-08-31, và để nguyên thì nó dạy
> người đọc rằng `entries: []` vẫn xoá sạch bảng — sai theo đúng chiều nguy hiểm.

### Nghiệm thu

| # | Phép thử | PASS |
|---|---|---|
| 1 | `PUT` với `entries: []` | **400**, và bảng **không đổi một dòng nào** |
| 2 | `PUT` thiếu đúng một `sysMenuId` | **400** |
| 3 | `GET` → `PUT` ngay với `version` vừa nhận | **200** |
| 4 | Hai `PUT` liên tiếp cùng một `version` | Lần đầu 200, lần sau **409**, và lần sau không ghi gì |
| 5 | Mở màn Phân quyền khi `GET` hỏng, bấm Lưu | Nút Lưu **không bấm được** |

Phép thử 1 phải kiểm **cả hai vế** — mã 400 mà bảng vẫn bị xoá thì vô nghĩa, và đó đúng là thứ
xảy ra nếu ai đó chặn ở handler sau khi `RemoveRange` đã chạy.

## `GET /api/admin/permissions`

`Data: PermissionMatrixDto`:

```json
{
  "roles": ["SuperAdmin", "Admin", "User"],
  "rows": [
    { "sysMenuId": "guid", "sysMenuCode": "trang-chu", "sysMenuName": "Trang chủ",
      "parentId": null, "assignedRoles": [] },
    { "sysMenuId": "guid", "sysMenuCode": "phan-quyen", "sysMenuName": "Phân quyền",
      "parentId": "guid-của-quan-tri", "assignedRoles": ["SuperAdmin"] }
  ],
  "version": "<token băm, xem §version tính thế nào>"
}
```

> 🔄 **SỬA 2026-09-06.** Ví dụ trên thiếu `version` (trường thứ ba của
> `PermissionMatrixDto`, `src/BE/Core/PlatformManager.Core.Application/Permissions/PermissionMatrixDto.cs:12`,
> có từ 2026-08-31) và dòng đầu là `dashboard` — mục menu đó **không còn được seed ở đâu cả** kể
> từ khi module DtiWeekly bị gỡ (xem `meta-menu.md`). Đổi sang `trang-chu`, mục có thật.

`rows` liệt kê **toàn bộ** `SysMenu` (kể cả mục cha không có route) — FE tự dựng ma trận
checkbox hàng = `rows`, cột = `roles`.

## `PUT /api/admin/permissions`

Request — **ghi đè toàn bộ** `SysMenuRole` theo đúng nội dung gửi lên (không phải patch từng
phần):

```json
{
  "entries": [
    { "sysMenuId": "guid", "roles": ["SuperAdmin", "Admin"] },
    { "sysMenuId": "guid-phan-quyen", "roles": ["SuperAdmin"] }
  ],
  "version": "<token nhận từ GET gần nhất>"
}
```

`Data: true`.

> ✅ **Ngữ nghĩa ĐÃ ĐỔI — thi công 2026-08-31, đối chiếu 2026-09-06.** `sysMenuId` vắng khỏi
> `entries` **không còn** nghĩa là "xoá hết gán quyền cho mục đó"; nay là **lỗi 400**
> (`UpdatePermissionMatrixCommand.cs:134-150`). Muốn thu hồi sạch thì gửi mục đó với `roles: []`.
> `version` là **bắt buộc**: thiếu hoặc lệch ⇒ **409**, không ghi gì (`:163-164` — `cmd.Version`
> null cũng không bao giờ khớp, đó là hành vi đã chốt).
>
> 🔄 **SỬA 2026-09-06.** Khối này trước đây là `🚧 Đổi 2026-08-30, chưa thi công` và khẳng định
> *"Cho tới khi code đổi, mô tả đang đúng với code hôm nay: bỏ một mục ra khỏi `entries` vẫn xoá
> quyền của mục đó"*. Câu đó hết đúng từ 2026-08-31 và nó dạy đúng thao tác phá dữ liệu.

## Lỗi

| Tình huống | HTTP | `status` | Khoá trong `fields` |
| --- | --- | --- | --- |
| Role không thuộc `SuperAdmin\|Admin\|User` | 400 | `VALIDATION_ERROR` | `Entries[i].Roles[j]` |
| `sysMenuId` rỗng (`Guid.Empty`) | 400 | `VALIDATION_ERROR` | `Entries[i].SysMenuId` |
| **Cùng một `sysMenuId` xuất hiện ≥ 2 lần trong `entries`** | 400 | `VALIDATION_ERROR` | `Entries` |
| Entry thiếu hẳn khoá `roles` (`null`) | 400 | `VALIDATION_ERROR` | `Entries[i].Roles` |
| Body thiếu hẳn khoá `entries` (`null`) | 400 | `VALIDATION_ERROR` | `Entries` |
| ✅ `entries` thiếu bất kỳ `SysMenu` nào (kể cả `[]`) | 400 | `VALIDATION_ERROR` | `Entries` |
| ✅ `sysMenuId` không tồn tại | 400 | `VALIDATION_ERROR` | `Entries` |
| ✅ `version` thiếu, hoặc lệch với trạng thái DB | 409 | `BUSINESS_ERROR`, `code: Conflict` | — |
| Gọi bởi user không phải `SuperAdmin` | 403 | — | — |

403 là JSON sạch (verify pipeline auth chung, xem `auth.md`), KHÔNG lộ 302 redirect.

> **Ba dòng cuối của nhóm 400 là bản SỬA LỖI 2026-08-29, không phải bổ sung tài liệu** — PERM-1 mắc
> **đúng** lỗi của PERM-2 (`entries.ToDictionary(e => e.sysMenuId, …)` ném `ArgumentException` khi
> key trùng ⇒ **500** thay vì 400). Lý do đầy đủ + test chốt: xem cùng mục §Lỗi của CONTRACT PERM-2
> bên dưới. Rủi ro của PERM-1 lớn hơn một bậc: đây cũng là lệnh **ghi đè toàn bộ**, và FE bắt buộc
> gửi lại toàn bộ `rows` (xem §"Rủi ro cần `frontend-expert` lưu ý"), nên payload trùng là thứ một
> lỗi ghép mảng ở FE sinh ra được, không phải ca giả định.

## Rủi ro cần `frontend-expert` lưu ý

Đây là "ghi đè toàn bộ" — nếu FE chỉ gửi các `sysMenuId` đã thay đổi (thiếu các mục không đổi)
thì các mục thiếu sẽ bị XOÁ hết quyền. FE **phải** gửi đủ toàn bộ `rows` hiện có trong request
`PUT`, không chỉ những dòng người dùng vừa tick/bỏ tick.

✅ **Từ 2026-08-31** ràng buộc này đã chuyển từ *"FE phải nhớ"* thành *"server từ chối"* — payload
thiếu nhận 400 thay vì âm thầm xoá (đối chiếu 2026-09-06). Đó là mục đích: một ràng buộc chỉ sống
trong tài liệu thì mọi client mới đều vi phạm được đúng một lần, và lần đó là lần mất dữ liệu. FE
vẫn phải gửi đủ; khác biệt là hỏng thì hỏng to tiếng.

---

# CONTRACT PERM-2 — Phân quyền theo hành động (resource permission, mới 2026-08-18)

**Status: AGREED** (2026-08-29) — 2 endpoint đã được nối vào `PermissionsController` và có
integration test chạy qua HTTP thật chốt cứng route + shape + gate role
([`ResourcePermissionEndpointTests.cs`](../../src/BE/Tests/PlatformManager.Core.IntegrationTests/Permissions/ResourcePermissionEndpointTests.cs),
xanh 2026-08-29). Chuyển `IMPLEMENTED` khi đã gọi thử trên DB thật ngoài Testcontainers.
Đếm bằng lệnh, đừng chép số vào đây:
`grep -c "\[Fact" src/BE/Tests/PlatformManager.Core.IntegrationTests/Permissions/ResourcePermissionEndpointTests.cs`

> 🚨 **ĐỌC TRƯỚC KHI DÙNG — ma trận này chạy được nhưng CHƯA ĐIỀU KHIỂN THỨ GÌ (2026-08-29).**
> Hai sự thật đi kèm, đừng bỏ qua:
>
> 1. Danh mục key hiện chỉ còn **đúng một** — `import.manage`
>    ([`AppResourceKeySource.cs`](../../src/BE/PlatformManager.Api/Permissions/AppResourceKeySource.cs),
>    đối chiếu 2026-09-03). Hai key `criteria.manage` / `criteria-groups.manage` mà bản DRAFT của
>    card này liệt kê đã bị gỡ cùng module DtiWeekly.
>
>    *(Sửa 2026-09-03: bản trước trỏ `Core.Application/.../ResourceKeys.cs:19`. Danh mục đã rời
>    Core ra host cùng ngày — Core giữ seam `ICoreResourceKeySource`, dự án khai dữ liệu. Đây là
>    lý do card này neo bằng **tên file**, không neo số dòng.)*
> 2. **Không endpoint sản phẩm nào mang `[RequirePermission]`.** Chỗ dùng duy nhất là
>    `PermissionSeamProbeController` trong project test. Kiểm bằng lệnh, đừng tin số ở đây:
>    `grep -rn "RequirePermission(" --include=*.cs src/BE | grep -v /obj/ | grep -v /bin/`
>
> Hệ quả: tick/bỏ tick trên UI ghi đúng vào bảng `RolePermissions`, `GET` đọc lại đúng, nhưng
> **không request nào của người dùng bị ma trận này chặn hay cho qua** — chưa có endpoint nào hỏi
> tới. Đây là hạ tầng dựng SẴN chờ module nghiệp vụ quay lại, không phải lớp bảo vệ đang có tác
> dụng. Đừng kết luận "quyền đã được kiểm soát" chỉ vì màn hình này chạy.

Vá gap OWASP #1 Broken Access Control (endpoint nghiệp vụ trước đây chỉ
`[Authorize]` trần). Xem `doc/huong_dan/quy-uoc/be-api-controller.md`
§"Phân quyền theo hành động — bản rút gọn (khác role, khác menu)".
*(Sửa 2026-08-31: bản trước trỏ tới §"…permission-key đầy đủ" — mục đó không tồn tại, và
mục thật nói ngược lại: nó là **bản rút gọn**, không phải hệ thống đầy đủ.)* Đây là **ma trận riêng biệt** với
`SysMenuRole` ở trên (menu visibility) — không gộp chung 1 API, không gộp chung 1 màn hình con.

Gate: `[Authorize(Roles = "SuperAdmin")]` khai ở **class** `PermissionsController`
([`PermissionsController.cs:14`](../../src/BE/PlatformManager.Api/Controllers/PermissionsController.cs)),
cộng dồn với `[Authorize]` của `ApiControllerBase` — giống hệt PERM-1, 2 action `resources`
không khai lại attribute nào. Đã đối chiếu source 2026-08-29. `Admin` gọi 2 endpoint này nhận
**403** (không phải 404 — route có thật), có test chốt cả 2 verb.

## `GET /api/admin/permissions/resources`

Response **nguyên văn** (chép từ body thật gọi qua host `PlatformManager.Api`, 2026-08-29 — không
phải phác thảo). Envelope `ApiResult<T>` chung như mọi endpoint; **thuộc tính `null` bị lược bỏ**
(`DefaultIgnoreCondition = WhenWritingNull`) nên đừng chờ `message`/`fields` xuất hiện ở ca thành
công:

```json
{"data":{"roles":["SuperAdmin","Admin","User"],"rows":[{"resourceKey":"import.manage","resourceName":"Import CSV/Excel","assignedRoles":["Admin"]}],"version":"<token băm>"},"status":"SUCCESS","code":"Success","traceId":"0HNO5KKF8Q2LU"}
```

> 🔄 **SỬA 2026-09-06.** Body nguyên văn ở trên chụp ngày 2026-08-29, **trước** khi `version` được
> thêm vào `ResourcePermissionMatrixDto` (2026-08-31,
> `src/BE/Core/PlatformManager.Core.Application/Permissions/ResourcePermissionMatrixDto.cs:9`).
> Đã chèn trường vào đúng chỗ nó xuất hiện; giá trị để trống vì token phụ thuộc dữ liệu.

`code` on-wire là **tên member** (`"Success"`), không phải số — `JsonStringEnumConverter`.

Ba tính chất FE phải dựa vào (mỗi cái đều có test chốt):

- **`roles` luôn là `Roles.All` đủ 3 giá trị**, không phải "các role đang có dòng trong DB" — ma
  trận rỗng vẫn đủ 3 cột để vẽ lưới checkbox.
- **`rows` luôn liệt kê đủ danh mục permission-key**, kể cả key chưa cấp cho ai (khi đó
  `assignedRoles: []`). Deny-by-default nghĩa là "không có dòng trong DB", nên nếu `rows` dựng từ
  DB thì key chưa cấp sẽ biến mất khỏi UI và không ai cấp được nữa.
- **Hôm nay `rows` có đúng 1 phần tử** (`import.manage`) — xem cảnh báo đầu mục PERM-2. FE
  **không** được hardcode số dòng; render theo mảng trả về.

`rows` là **danh sách phẳng** (khác PERM-1 có cây cha/con qua `parentId`), không có khái niệm
cha/con. FE dựng ma trận checkbox đơn giản hơn PERM-1, không cần logic sắp cây (`toDisplayOrder`
của `PermissionMatrix` component không tái dùng được ở đây — cần 1 component dumb mới, đơn giản
hơn, KHÔNG cố gắng generalize `PermissionMatrix` để xử lý cả 2 trường hợp cây và phẳng).

## `PUT /api/admin/permissions/resources`

Request — **ghi đè toàn bộ** (giống hệt PERM-1 — FE phải gửi đủ toàn bộ `rows`, không chỉ dòng
vừa đổi). Body **có bọc** `entries`, không phải mảng phẳng ở gốc:

```json
{
  "entries": [
    { "resourceKey": "import.manage", "roles": ["Admin", "User"] }
  ],
  "version": "<token nhận từ GET gần nhất>"
}
```

`version` **bắt buộc** từ 2026-08-31 —
`src/BE/Core/PlatformManager.Core.Application/Permissions/UpdateResourcePermissionMatrixCommand.cs:18`.

Response thành công (nguyên văn, 2026-08-29):

```json
{"data":true,"status":"SUCCESS","code":"Success","traceId":"0HNO5KKF8Q2LT"}
```

Ba hành vi đã đo, FE nên biết:

| Gửi lên | Kết quả |
| --- | --- |
| `entries` chứa key + tập role mới | Xoá sạch gán cũ của MỌI key rồi ghi lại đúng payload |
| `entries` chứa key với `roles: []` | Key đó về không role nào (thu hồi sạch) |
| `entries: []` | ✅ **400** từ 2026-08-31 — payload phải phủ đủ danh mục key. Trước đó nó thu hồi TOÀN BỘ ma trận và vẫn trả `200 / data: true` |

> 🔄 **SỬA 2026-09-06.** Dòng cuối bảng trước đây ghi *"Thu hồi TOÀN BỘ ma trận — không phải
> 'không làm gì'. 🚧 Sắp thành 400"*, kèm câu *"`PUT` với `entries` rỗng trả `200 / data: true` và
> xoá sạch bảng"*. Cả hai mô tả hành vi đã bị thay từ 2026-08-31. FE vẫn **không** được gửi
> request rỗng như một cách "huỷ thao tác" — nay nó chỉ nhận 400 thay vì mất dữ liệu.

## Lỗi

| Tình huống | HTTP | `status` | Khoá trong `fields` |
| --- | --- | --- | --- |
| `resourceKey` không khớp `ResourceKeys` đã khai ở BE | 400 | `VALIDATION_ERROR` | `Entries[i].ResourceKey` |
| Role không thuộc `SuperAdmin\|Admin\|User` | 400 | `VALIDATION_ERROR` | `Entries[i].Roles[j]` |
| **Cùng một `resourceKey` xuất hiện ≥ 2 lần trong `entries`** | 400 | `VALIDATION_ERROR` | `Entries` |
| Entry thiếu hẳn khoá `roles` (`null`) | 400 | `VALIDATION_ERROR` | `Entries[i].Roles` |
| Body thiếu hẳn khoá `entries` (`null`) | 400 | `VALIDATION_ERROR` | `Entries` |
| ✅ `entries` thiếu bất kỳ key nào của danh mục (kể cả `[]`) | 400 | `VALIDATION_ERROR` | `Entries` |
| ✅ `version` thiếu, hoặc lệch với trạng thái DB | 409 | `BUSINESS_ERROR`, `code: Conflict` | — |
| Đăng nhập bằng `Admin` (không phải `SuperAdmin`) | 403 | — | — |
| Chưa đăng nhập | 401 | — | — |

> **Ba dòng in đậm/mới thêm 2026-08-29 là bản SỬA LỖI, không phải bổ sung tài liệu.** Cho tới lúc
> đó handler dựng `Dictionary` bằng `entries.ToDictionary(e => e.resourceKey, …)` mà không ai chặn
> key trùng: `ToDictionary` ném `ArgumentException`, loại exception này không có nhánh xử lý riêng
> nên client nhận **500 `SYSTEM_ERROR`** — trái hẳn với 400 mà mục này công bố. Tương tự,
> `entries`/`roles` thiếu cho `NullReferenceException` ⇒ cũng 500. Nay cả ba ca bị chặn ở validator
> (chạy TRƯỚC handler, nên **không** đụng vào `RolePermissions`). Có test qua HTTP thật chốt đúng
> con số 400: [`PermissionMatrixDuplicateKeyTests.cs`](../../src/BE/Tests/PlatformManager.Core.IntegrationTests/Permissions/PermissionMatrixDuplicateKeyTests.cs).

**Khoá `fields` của ca trùng là `Entries` (không có chỉ số)** — cố ý: lỗi thuộc về quan hệ *giữa*
các phần tử, không quy được cho một dòng cụ thể. FE hiển thị thông điệp ở mức toàn ma trận; thông
điệp có nêu đích danh key bị lặp để người dùng biết sửa dòng nào. Đây là ca FE sinh ra được thật:
`PUT` bắt buộc gửi lại **toàn bộ** `rows`, nên một lỗi ghép mảng (nối kết quả hai lần tải, hoặc
`push` dòng vừa sửa thay vì thay thế) cho ra đúng payload này.

Chặn `resourceKey` tự bịa là có chủ đích: gán quyền cho 1 key "ma" không controller nào đọc sẽ
tạo ảo giác đã phân quyền. Validator chạy **trước** handler, nên request 400 **không** đụng vào
`RolePermissions` (có test chống ca "xoá sạch rồi mới validate").

Body 400 nguyên văn (2026-08-29) — **khoá của `fields` là PascalCase theo tên property C#**, KHÔNG
camelCase như `data`; FE bind lỗi theo đúng chuỗi này:

```json
{"message":"Dữ liệu không hợp lệ.","status":"VALIDATION_ERROR","code":"ValidationError","traceId":"0HNO5KKF8Q2LV","fields":{"Entries[0].ResourceKey":["ResourceKey không hợp lệ — chỉ nhận các key đã khai ở ResourceKeys."],"Entries[0].Roles[0]":["Role không hợp lệ — chỉ nhận SuperAdmin/Admin/User."]}}
```

Chỉ số trong khoá là vị trí trong mảng `entries` gửi lên (`Entries[0]`), và với role sai là vị trí
trong mảng `roles` của entry đó (`Entries[0].Roles[0]`) — đủ để FE tô đỏ đúng ô checkbox.

## ⚠️ Rủi ro rollout — đọc kỹ trước khi migrate

Filter `RequirePermissionFilter` coi "chưa có `RolePermission` nào cho key này" = deny. Migration
tạo bảng `RolePermissions` **phải kèm seed mặc định** cấp đủ **mọi key trong danh mục
tại thời điểm đó** cho `Admin` + `User` (giữ nguyên hành vi trước khi vá) — nếu không, mọi user
không phải `SuperAdmin` sẽ bị 403 ngay khi `[RequirePermission]` đầu tiên lên production, kể cả
thao tác họ vẫn làm được trước đó. Đây là điều kiện DoD của PERM-2, không phải chi tiết tuỳ chọn.

Đếm bằng lệnh, đừng chép số vào đây (danh sách key đã đổi hai lần rồi):
`grep -n "public const string" src/BE/PlatformManager.Api/Permissions/AppResourceKeySource.cs`

> *(Sửa 2026-09-03: lệnh cũ đọc `Core.Application/Permissions/ResourceKeys.cs`. Danh mục key
> đã rời Core ra host cùng ngày — Core nay chỉ giữ seam `ICoreResourceKeySource`, dữ liệu do
> dự án khai. Xem `doc/kien-truc-core-module.md`.)*

Rủi ro này **chưa hiện thực hoá hôm nay** vì chưa endpoint nào mang `[RequirePermission]` (xem
cảnh báo đầu mục PERM-2) — nhưng nó sẽ nổ đúng vào ngày module nghiệp vụ đầu tiên gắn attribute
đó. Gắn `[RequirePermission]` đầu tiên = phải làm seed trong cùng lượt.

**`SuperAdmin` được miễn — nói rõ từ 2026-08-19.** Câu trên viết "mọi user không phải
`SuperAdmin`" ngay từ đầu, nhưng trong khoảng 2026-08-18 → 2026-08-19 **code KHÔNG có bypass
nào** (phát hiện khi rà lại sau audit) — tức tài liệu mô tả một đằng, code chạy một nẻo, và tài
khoản chỉ mang role `SuperAdmin` thực tế bị 403. Nay `RequirePermissionFilter` đã có bypass
tường minh cho `Roles.SuperAdmin`, code và tài liệu thống nhất. Hệ quả cho FE/vận hành:

- `SuperAdmin` **không cần** dòng `RolePermission` nào; gán thêm cũng không sai, chỉ thừa.
- Quyền của `SuperAdmin` **không thu hồi được** qua ma trận PERM-2 — bỏ tick cho `SuperAdmin`
  trên UI sẽ không làm nó mất quyền. Muốn chặn thì gỡ chính role `SuperAdmin` khỏi user.
- Lý do giữ bypass: tránh tự khoá hệ thống khi thu hồi nhầm. Xem
  `doc/huong_dan/quy-uoc/be-api-controller.md` §"Phân quyền theo hành động — bản rút gọn".

  > 🚧 **Sửa 2026-08-31.** Lý do thứ hai của dòng này từng là *"`CoreSeeder` chỉ chạy ở
  > Development nên không thể trông vào seed cho môi trường thật"*. Tiền đề đó **đã bị lật**
  > ngày 2026-08-30: production seed bằng một lệnh riêng gọi chính `CoreSeeder` — đọc
  > [`../huong_dan/wiki-core/be/13-core-data-migration.md`](../huong_dan/wiki-core/be/13-core-data-migration.md)
  > §"Quyết định người dùng 2026-08-30". Lý do thứ nhất (chống tự khoá) vẫn còn giá trị, nên
  > quyết định giữ bypass không đổi — chỉ căn cứ thì bớt một vế.

## FE

Trang "Phân quyền" (`phan-quyen.page.ts`) thêm 1 tab/section thứ 2 "Quyền theo tài nguyên" cạnh
ma trận menu hiện có — dùng component dumb mới (không tái dùng `PermissionMatrix` như đã nêu ở
trên), service gọi 2 endpoint trên, mapper riêng (`resourceKey`/`resourceName` không trùng shape
`sysMenuId`/`sysMenuName` của PERM-1).

**Cột `SuperAdmin` ở ma trận này: tick sẵn + disabled + chú thích** (2026-08-19, đóng finding
PARTIAL của lượt review 2026-08-19). Lý do: mục
"Rủi ro rollout" ở trên nói bỏ tick `SuperAdmin` không thu hồi được quyền, nhưng UI vẫn cho bỏ
tick + báo "đã lưu" → người quản trị tin nhầm là đã thu quyền. Đây là hiển thị đúng sự thật,
**không phải** lớp chặn — FE không bao giờ là ranh giới bảo mật, việc chặn thuộc BE.
`ResourcePermissionMatrix` **không** tự thêm/bớt role vào payload `PUT`, vẫn gửi nguyên
`assignedRoles` do BE trả — contract không đổi. ⚠️ Quy tắc này **CHỈ** áp cho PERM-2; ma trận
PERM-1 ghi `SysMenuRole` (không dính bypass), bỏ tick `SuperAdmin` ở đó vẫn có tác dụng thật nên
ô phải để bấm được — có test 2 chiều chốt cứng (`permission-matrix.spec.ts`,
`resource-permission-matrix.spec.ts`).

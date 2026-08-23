# API Contract Card — Permissions (Phân quyền)

**Status: AGREED** (2026-08-16) — build xanh, envelope/auth pipeline verify thật (xem
`auth.md`). Chuyển IMPLEMENTED khi có DB đã migrate để gọi thử response thật.

Gate: `[Authorize(Roles = "SuperAdmin")]` toàn bộ controller — **CHỈ SuperAdmin**, kể cả
`Admin` cũng bị 403 (tránh leo thang quyền qua UI, xem `doc/ke-hoach-xay-lai-corebase.md`).

## `GET /api/admin/permissions`

`Data: PermissionMatrixDto`:

```json
{
  "roles": ["SuperAdmin", "Admin", "User"],
  "rows": [
    { "sysMenuId": "guid", "sysMenuCode": "dashboard", "sysMenuName": "Dashboard",
      "parentId": null, "assignedRoles": [] },
    { "sysMenuId": "guid", "sysMenuCode": "phan-quyen", "sysMenuName": "Phân quyền",
      "parentId": "guid-của-quan-tri", "assignedRoles": ["SuperAdmin"] }
  ]
}
```

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
  ]
}
```

`sysMenuId` không có trong `entries` = xoá hết gán quyền cho mục đó (mở cho mọi user đã đăng
nhập). `Data: true`.

## Lỗi

Role không thuộc `SuperAdmin|Admin|User` → 400 (validation, `fields.Entries[i].Roles`).
Gọi bởi user không phải `SuperAdmin` → 403 JSON sạch (verify pipeline auth chung, xem
`auth.md`), KHÔNG lộ 302 redirect.

## Rủi ro cần `frontend-expert` lưu ý

Đây là "ghi đè toàn bộ" — nếu FE chỉ gửi các `sysMenuId` đã thay đổi (thiếu các mục không đổi)
thì các mục thiếu sẽ bị XOÁ hết quyền. FE **phải** gửi đủ toàn bộ `rows` hiện có trong request
`PUT`, không chỉ những dòng người dùng vừa tick/bỏ tick.

---

# CONTRACT PERM-2 — Phân quyền theo hành động (resource permission, mới 2026-08-18)

**Status: DRAFT** — vá gap OWASP #1 Broken Access Control (endpoint nghiệp vụ trước đây chỉ
`[Authorize]` trần). Xem thiết kế đầy đủ ở `doc/huong_dan/quy-uoc/be-api-controller.md`
§"Phân quyền theo hành động — permission-key đầy đủ". Đây là **ma trận riêng biệt** với
`SysMenuRole` ở trên (menu visibility) — không gộp chung 1 API, không gộp chung 1 màn hình con.

Gate: `[Authorize(Roles = "SuperAdmin")]`, giống hệt PERM-1.

## `GET /api/admin/permissions/resources`

`Data: ResourcePermissionMatrixDto`:

```json
{
  "roles": ["SuperAdmin", "Admin", "User"],
  "rows": [
    { "resourceKey": "criteria.manage", "resourceName": "Quản lý chỉ tiêu",
      "assignedRoles": ["SuperAdmin", "Admin", "User"] },
    { "resourceKey": "criteria-groups.manage", "resourceName": "Quản lý nhóm chỉ tiêu",
      "assignedRoles": ["SuperAdmin", "Admin"] },
    { "resourceKey": "import.manage", "resourceName": "Import CSV/Excel",
      "assignedRoles": ["SuperAdmin", "Admin"] }
  ]
}
```

`rows` là **danh sách phẳng** (khác PERM-1 có cây cha/con qua `parentId`) — đúng 3 key hiện có
(`ResourceKeys` ở BE), không có khái niệm cha/con. FE dựng ma trận checkbox đơn giản hơn PERM-1,
không cần logic sắp cây (`toDisplayOrder` của `PermissionMatrix` component không tái dùng được
ở đây — cần 1 component dumb mới, đơn giản hơn, KHÔNG cố gắng generalize `PermissionMatrix` để
xử lý cả 2 trường hợp cây và phẳng).

## `PUT /api/admin/permissions/resources`

Request — **ghi đè toàn bộ** (giống hệt PERM-1 — FE phải gửi đủ toàn bộ `rows`, không chỉ dòng
vừa đổi):

```json
{
  "entries": [
    { "resourceKey": "criteria.manage", "roles": ["SuperAdmin", "Admin", "User"] },
    { "resourceKey": "criteria-groups.manage", "roles": ["SuperAdmin", "Admin"] },
    { "resourceKey": "import.manage", "roles": ["SuperAdmin", "Admin"] }
  ]
}
```

`Data: true`.

## Lỗi

Role không thuộc `SuperAdmin|Admin|User` → 400. `resourceKey` không khớp `ResourceKeys` đã khai
ở BE → 400 (chặn FE gửi key tự bịa, tránh gán quyền cho 1 key "ma" không controller nào đọc).

## ⚠️ Rủi ro rollout — đọc kỹ trước khi migrate

Filter `RequirePermissionFilter` coi "chưa có `RolePermission` nào cho key này" = deny. Migration
tạo bảng `RolePermissions` **phải kèm seed mặc định** cấp đủ 3 key hiện có cho `Admin` + `User`
(giữ nguyên hành vi trước khi vá) — nếu không, mọi user không phải `SuperAdmin` sẽ bị 403 ngay
khi `[RequirePermission]` đầu tiên lên production, kể cả thao tác họ vẫn làm được trước đó. Đây
là điều kiện DoD của PERM-2, không phải chi tiết tuỳ chọn.

**`SuperAdmin` được miễn — nói rõ từ 2026-08-19.** Câu trên viết "mọi user không phải
`SuperAdmin`" ngay từ đầu, nhưng trong khoảng 2026-08-18 → 2026-08-19 **code KHÔNG có bypass
nào** (phát hiện khi rà lại sau audit) — tức tài liệu mô tả một đằng, code chạy một nẻo, và tài
khoản chỉ mang role `SuperAdmin` thực tế bị 403. Nay `RequirePermissionFilter` đã có bypass
tường minh cho `Roles.SuperAdmin`, code và tài liệu thống nhất. Hệ quả cho FE/vận hành:

- `SuperAdmin` **không cần** dòng `RolePermission` nào; gán thêm cũng không sai, chỉ thừa.
- Quyền của `SuperAdmin` **không thu hồi được** qua ma trận PERM-2 — bỏ tick cho `SuperAdmin`
  trên UI sẽ không làm nó mất quyền. Muốn chặn thì gỡ chính role `SuperAdmin` khỏi user.
- Lý do giữ bypass: tránh tự khoá hệ thống khi thu hồi nhầm, và `CoreSeeder` chỉ chạy ở
  Development nên không thể trông vào seed cho môi trường thật. Xem
  `doc/huong_dan/quy-uoc/be-api-controller.md` §"Phân quyền theo hành động".

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

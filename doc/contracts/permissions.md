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

# API Contract Card — Meta / Menu

**Status: AGREED** (2026-08-16) — BE đã sửa lại đúng theo shape danh sách phẳng mà FE
(`shared/models/menu-item.model.ts`, `shared/services/menu.service.ts`) đã code sẵn — không
còn lệch. Build xanh, envelope/auth pipeline verify thật (xem `auth.md`). Chuyển IMPLEMENTED
khi có DB đã migrate để gọi thử response thật (cây menu phụ thuộc dữ liệu seed `CoreSeeder.cs`).

> Lịch sử: bản đầu BE trả CÂY lồng sẵn (field `name`, không có `parentId`) — sai, không khớp
> `IMenuItemDto` phía FE. `core-reviewer` audit chéo BE↔FE phát hiện, coordinator quyết định BE
> đổi theo FE (không phải ngược lại, vì FE đã code + có sẵn `buildMenuTree()`). File
> `doc/contracts/meta-menu.md` (FE tự tạo, DRAFT, đúng shape phẳng từ đầu) đã gộp vào file này, xoá
> file gốc — chỉ còn 1 nguồn cho endpoint này.

## `GET /api/meta/menu` — `[Authorize]`

Trả `Data: MenuItemDto[]` — **danh sách PHẲNG**, KHÔNG lồng cây. FE tự dựng cây 1 cấp qua
`ParentId` (`shared/services/menu.service.ts` → `buildMenuTree()`). CHỈ các mục user hiện tại
được thấy (lọc theo `SysMenuRole` — mục không có dòng nào trong `SysMenuRole` = mở cho mọi
user đã đăng nhập).

```json
[
  { "id": "guid", "parentId": null, "code": "dashboard", "label": "Dashboard",
    "icon": "pi-th-large", "route": "/dashboard", "displayOrder": 1 },
  { "id": "guid", "parentId": null, "code": "danh-muc", "label": "Danh mục",
    "icon": "pi-folder", "route": null, "displayOrder": 2 },
  { "id": "guid-của-danh-muc", "parentId": "guid-của-danh-muc-ở-trên", "code": "danh-muc-dti",
    "label": "DTI", "icon": "pi-list", "route": "/danh-muc/dti", "displayOrder": 1 },
  { "id": "guid", "parentId": null, "code": "quan-tri", "label": "Quản trị hệ thống",
    "icon": "pi-cog", "route": null, "displayOrder": 3 },
  { "id": "guid", "parentId": "guid-của-quan-tri", "code": "sys-user", "label": "Người dùng",
    "icon": "pi-user", "route": "/quan-tri/nguoi-dung", "displayOrder": 1 },
  { "id": "guid", "parentId": "guid-của-quan-tri", "code": "phan-quyen", "label": "Phân quyền",
    "icon": "pi-shield", "route": "/quan-tri/phan-quyen", "displayOrder": 2 }
]
```

## Field

| Field | Kiểu | Ghi chú |
| --- | --- | --- |
| `id` | guid | |
| `parentId` | guid \| null | `null` = item gốc. Trỏ tới 1 item KHÁC cũng `parentId: null` (chỉ 1 cấp lồng) |
| `code` | string | Khoá ổn định, dùng cho `@for` track |
| `label` | string | Tên hiển thị — **KHÔNG phải `name`** |
| `icon` | string \| null | Class PrimeIcons THẬT (`pi-th-large`, `pi-folder`...) — xem mục Icon bên dưới |
| `route` | string \| null | `null` cho item cha (chỉ toggle expand/collapse, không điều hướng) |
| `displayOrder` | int | |

## Quy tắc hiển thị mục cha

Mục cha (`danh-muc`, `quan-tri`) luôn xuất hiện trong danh sách nếu **bất kỳ con nào** của nó
user thấy được — kể cả khi bản thân mục cha có `SysMenuRole` riêng không khớp role hiện tại
(thực tế `danh-muc`/`quan-tri` hiện KHÔNG có `SysMenuRole` riêng, chỉ các con mới có). Lý do
bắt buộc: FE dựng cây từ `parentId`, thiếu record cha trong response sẽ làm con "mồ côi"
(`buildMenuTree()` không tìm thấy cha, coi con đó là root sai vị trí).

**Ví dụ theo role**:
- Role `User` (không SuperAdmin/Admin): chỉ thấy `dashboard` + `danh-muc` + `danh-muc-dti` —
  KHÔNG thấy `quan-tri` (cả 2 con của nó đều gated Admin+).
- Role `Admin`: thấy thêm `quan-tri` + `sys-user`, KHÔNG thấy `phan-quyen`.
- Role `SuperAdmin`: thấy toàn bộ 6 mục.

## Icon — PrimeIcons đã CHỐT, trả THẲNG class CSS

`icon` là class PrimeIcons thật (`pi-th-large`, `pi-folder`, `pi-list`, `pi-cog`, `pi-user`,
`pi-shield`) — **không phải khoá trừu tượng cần FE map lại**. FE
(`shared/components/sidebar/sidebar.ts`) dùng nguyên `item.icon` làm class CSS, chỉ fallback
`pi-circle` khi BE trả `null`. Đã bỏ hẳn cơ chế map khoá qua bảng riêng (`menu-icon.util.ts` cũ
đã xoá) — 1 nguồn duy nhất, seed đúng ở `PlatformManager.Core.Infrastructure/Persistence/CoreSeeder.cs`.

## Lỗi mong đợi

Chưa đăng nhập → `401` theo envelope chuẩn (xem `auth.md`), không redirect 302.

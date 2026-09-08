---
kind: luat
scope: core
verified: 2026-09-06
---

# API Contract Card — Meta / Menu

**Status: AGREED** (2026-08-16) — BE đã sửa lại đúng theo shape danh sách phẳng mà FE
(`src/FE/src/app/core/menu/menu-item.model.ts`, `src/FE/src/app/core/menu/menu.service.ts`) đã
code sẵn — không còn lệch.

> 🔄 **SỬA 2026-09-06.** Hai đường dẫn FE ở câu trên trước đây là `shared/models/menu-item.model.ts`
> và `shared/services/menu.service.ts`. Cả hai thư mục đó nay **rỗng phần menu**: cặp file đã
> chuyển sang `core/menu/`. Cùng lỗi ở §`GET /api/meta/menu` và §Icon bên dưới, sửa cùng lượt. Build xanh, envelope/auth pipeline verify thật (xem `auth.md`). Chuyển IMPLEMENTED
khi có DB đã migrate để gọi thử response thật (cây menu phụ thuộc dữ liệu seed — xem
`AppMenuSeedSource.cs`).

> ⚠️ **Nguồn dữ liệu menu ĐÃ CHUYỂN CHỖ 2026-09-02.** Danh sách 4 mục không còn nằm trong
> `CoreSeeder` — nó chuyển sang host tại
> `src/BE/PlatformManager.Api/Seeding/AppMenuSeedSource.cs:41`, đi vào Core qua seam
> `src/BE/Core/PlatformManager.Core.Application/Menu/ICoreMenuSeedSource.cs:21`. Lý do: nhãn,
> route và icon là dữ liệu riêng dự án, để trong Core thì dự án thứ hai phải mổ vào Core mới
> dùng lại được (`doc/kien-truc-core-module.md` §"Core giữ CƠ CHẾ, dự án cung cấp DỮ LIỆU").
> **Shape endpoint, 4 mục, cây cha-con và lời gán role đều KHÔNG đổi** — đối chiếu bằng cách
> chạy `--seed` lên DB trống rồi đọc `core."SysMenus"`, đúng phép thử #1 của mục nghiệm thu ở
> tài liệu kia.

> **Shape của endpoint không đổi; DỮ LIỆU ví dụ thì có.** Đối chiếu
> `src/BE/PlatformManager.Api/Seeding/AppMenuSeedSource.cs:41` ngày
> **2026-08-29** (cập nhật đường dẫn 2026-09-02, nội dung không đổi): sau khi module DtiWeekly
> bị gỡ, seed chỉ còn **menu Core** —
> `trang-chu`, `quan-tri`, `sys-user`, `phan-quyen`. Hai mục `dashboard` và `danh-muc`/
> `danh-muc-dti` trong ví dụ cũ **không còn được seed ở đâu cả**; chúng thuộc menu nghiệp
> vụ, mà theo ranh giới đã chốt (`doc/kien-truc-core-module.md`) menu nghiệp vụ do seeder
> của chính module đóng góp — chưa có module nào thì chưa có mục nào.
>
> Vì vậy `displayOrder` của `quan-tri` là **3**, không phải 2: khoảng trống thứ 2 để dành
> cho nhánh nghiệp vụ chèn vào giữa. Đừng "sửa cho liền số".

> Lịch sử: bản đầu BE trả CÂY lồng sẵn (field `name`, không có `parentId`) — sai, không khớp
> `IMenuItemDto` phía FE. `core-reviewer` audit chéo BE↔FE phát hiện, coordinator quyết định BE
> đổi theo FE (không phải ngược lại, vì FE đã code + có sẵn `buildMenuTree()`). File
> `doc/contracts/meta-menu.md` (FE tự tạo, DRAFT, đúng shape phẳng từ đầu) đã gộp vào file này, xoá
> file gốc — chỉ còn 1 nguồn cho endpoint này.

## `GET /api/meta/menu` — `[Authorize]`

Trả `Data: MenuItemDto[]` — **danh sách PHẲNG**, KHÔNG lồng cây. FE tự dựng cây 1 cấp qua
`ParentId` (`src/FE/src/app/core/menu/menu.service.ts:31` → `buildMenuTree()`, gọi ở `:132`). CHỈ các mục user hiện tại
được thấy (lọc theo `SysMenuRole` — mục không có dòng nào trong `SysMenuRole` = mở cho mọi
user đã đăng nhập).

Ví dụ dưới là response cho `SuperAdmin` — tức **toàn bộ** menu đang được seed
(2026-08-29), không phải một mẫu rút gọn:

```json
[
  { "id": "guid", "parentId": null, "code": "trang-chu", "label": "Trang chủ",
    "icon": "pi-home", "route": "/trang-chu", "displayOrder": 1 },
  { "id": "guid", "parentId": null, "code": "quan-tri", "label": "Quản trị hệ thống",
    "icon": "pi-cog", "route": null, "displayOrder": 3 },
  { "id": "guid", "parentId": "guid-của-quan-tri", "code": "sys-user", "label": "Người dùng",
    "icon": "pi-user", "route": "/quan-tri/nguoi-dung", "displayOrder": 1 },
  { "id": "guid", "parentId": "guid-của-quan-tri", "code": "phan-quyen", "label": "Phân quyền",
    "icon": "pi-shield", "route": "/quan-tri/phan-quyen", "displayOrder": 2 }
]
```

`route` khớp đúng route FE (`src/FE/src/app/app.routes.ts`) — `/trang-chu`,
`/quan-tri/nguoi-dung`, `/quan-tri/phan-quyen`. Khi thêm mục menu mới, kiểm bằng
`grep -n "path:" src/FE/src/app/app.routes.ts` thay vì tin ví dụ này.

## Field

| Field | Kiểu | Ghi chú |
| --- | --- | --- |
| `id` | guid | |
| `parentId` | guid \| null | `null` = item gốc. Trỏ tới 1 item KHÁC cũng `parentId: null` (chỉ 1 cấp lồng) |
| `code` | string | Khoá ổn định, dùng cho `@for` track |
| `label` | string | Tên hiển thị — **KHÔNG phải `name`** |
| `icon` | string \| null | Class PrimeIcons THẬT (`pi-home`, `pi-cog`...) — xem mục Icon bên dưới |
| `route` | string \| null | `null` cho item cha (chỉ toggle expand/collapse, không điều hướng) |
| `displayOrder` | int | |

## Quy tắc hiển thị mục cha

Mục cha (hiện chỉ có `quan-tri`) luôn xuất hiện trong danh sách nếu **bất kỳ con nào** của nó
user thấy được — **kể cả khi bản thân mục cha có `SysMenuRole` riêng không khớp role hiện
tại**. Lý do bắt buộc: FE dựng cây từ `parentId`, thiếu record cha trong response sẽ làm con
"mồ côi" (`buildMenuTree()` không tìm thấy cha, coi con đó là root sai vị trí). Hiện thực ở
`src/BE/Core/PlatformManager.Core.Application/Menu/GetMenuQuery.cs:29` (vòng lặp kéo cha vào
sau khi lọc theo role).

> Sửa 2026-08-29: câu trong ngoặc ở bản trước — *"thực tế `danh-muc`/`quan-tri` hiện KHÔNG có
> `SysMenuRole` riêng"* — **sai**. `quan-tri` có `SysMenuRole` gán `SuperAdmin` + `Admin`
> (`src/BE/PlatformManager.Api/Seeding/AppMenuSeedSource.cs:49`), nên chính
> nó là ca mà nhánh "kéo cha vào" phục vụ, không phải ca giả định.

**Gán role đang seed** (2026-08-29):

| Mục | `SysMenuRole` |
| --- | --- |
| `trang-chu` | *(không dòng nào — mở cho mọi user đã đăng nhập)* |
| `quan-tri` | `SuperAdmin`, `Admin` |
| `sys-user` | `SuperAdmin`, `Admin` |
| `phan-quyen` | `SuperAdmin` |

**Ví dụ theo role** (suy ra từ bảng trên):
- Role `User`: chỉ thấy `trang-chu`. Không con nào của `quan-tri` visible nên `quan-tri` cũng
  không được kéo vào.
- Role `Admin`: thấy `trang-chu` + `quan-tri` + `sys-user`, KHÔNG thấy `phan-quyen`.
- Role `SuperAdmin`: thấy toàn bộ.

## Icon — PrimeIcons đã CHỐT, trả THẲNG class CSS

`icon` là class PrimeIcons thật — **không phải khoá trừu tượng cần FE map lại**. FE
(`src/FE/src/app/shared/components/sidebar/sidebar.ts:50-51`) dùng nguyên `item.Icon` làm class
CSS, chỉ fallback `pi-circle` khi BE trả `null`. Đã bỏ hẳn cơ chế map khoá qua bảng riêng (`menu-icon.util.ts` cũ
đã xoá) — 1 nguồn duy nhất, khai ở
`src/BE/PlatformManager.Api/Seeding/AppMenuSeedSource.cs`.

Danh sách icon đang seed **đếm bằng lệnh, đừng chép** (`.claude/CLAUDE.md` §6):

```bash
grep -o 'pi-[a-z-]*' src/BE/PlatformManager.Api/Seeding/AppMenuSeedSource.cs
```

*(Bản trước liệt cứng 6 icon, trong đó `pi-th-large`/`pi-folder`/`pi-list` thuộc menu nghiệp
vụ đã gỡ 2026-08-29 — đúng lý do §6 cấm chép danh sách vào văn bản.)*

## Lỗi mong đợi

Chưa đăng nhập → `401` theo envelope chuẩn (xem `auth.md`), không redirect 302.

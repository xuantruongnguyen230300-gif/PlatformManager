# doc/Prototype/ — mục lục cho agent

> Đây là **nguồn hình ảnh sống** (live source) của UI PlatformManager — static
> HTML/CSS/JS, không framework, không build step. `doc/Design/` trích xuất
> token/spec TỪ đây; không sửa ngược từ `doc/Design/` xuống đây (xem
> `doc/Design/CLAUDE.md` § Core Principle 4).

## Trang đầy đủ (multi-page, điều hướng qua sidebar thật)

| File | Vai trò | Sidebar |
| --- | --- | --- |
| [`login.html`](login.html) | Màn hình đăng nhập — trước xác thực, chưa có sidebar | Không |
| [`dashboard.html`](dashboard.html) | Dashboard DTI Weekly — chỉ xem, biểu đồ + bảng 62 chỉ tiêu | Có |
| [`danh-muc-dti.html`](danh-muc-dti.html) | Danh mục DTI — CRUD + nhập liệu theo tuần | Có |
| [`quan-tri-nguoi-dung.html`](quan-tri-nguoi-dung.html) | Người dùng hệ thống (SysUser) — mẫu màn hình corebase mặc định | Có |

Cả 3 trang có sidebar dùng **chung 1 khối CSS/HTML/JS** (xem
`spec/sidebar-menu/ui-spec.md`) — sửa sidebar phải sửa đồng bộ cả 3 file,
không sửa 1 nơi rồi để 2 nơi còn lại lệch.

## Component tách nhỏ (`components/`) — đọc nhanh khi implement 1 phần cụ thể

Không cần mở cả trang lớn để biết "nút này/icon này trông thế nào" — mở
đúng file dưới đây:

| File | Dùng khi |
| --- | --- |
| [`components/buttons.html`](components/buttons.html) | Cần biết đủ biến thể nút (primary/secondary/danger/text/icon-button) + 5 trạng thái |
| [`components/icons.html`](components/icons.html) | Cần biết icon nào dùng ở đâu — bảng map placeholder SVG → PrimeIcons class thật |
| [`components/data-grid.html`](components/data-grid.html) | Dựng 1 grid/danh sách mới (không phải DTI) — khung chung: search, sort header, badge trạng thái, action column, pagination |

## Quan hệ với các thư mục khác

```
doc/Prototype/*.html          ← NGUỒN SỐNG (sửa ở đây trước)
        │
        ├─▶ doc/Design/Frontend/PlatformManager/   ← trích xuất token/spec (chạy /design-extract-tokens...)
        ├─▶ spec/sidebar-menu/ui-spec.md            ← đặc tả hành vi/IA của sidebar
        └─▶ doc/huong_dan/wiki-core/fe/*.md         ← quyết định kiến trúc khi lên Angular thật
              (11-grid-and-metadata.md, 04-design-token-system.md, 05-component-library.md)
```

## Trạng thái — corebase, chưa nghiệp vụ

`login.html` và `quan-tri-nguoi-dung.html` là màn hình **corebase** (auth,
quản trị hệ thống) — dữ liệu trong 2 file này là minh hoạ tĩnh, không gọi
API thật, không gắn với `doc/ERD/ERD.md` (schema đó vẫn đang mô tả nghiệp
vụ DTI, ngoài phạm vi corebase hiện tại).

## Khi thêm 1 trang mới

1. Copy nguyên khối `<style>` phần token (`:root{...}`) + phần sidebar CSS
   từ 1 trong 3 trang đã có — **không** đổi giá trị token.
2. Copy nguyên khối HTML sidebar (`<aside class="sidebar">...</aside>` +
   `.sidebar-backdrop`) + JS sidebar (2 script cuối file) — **không** viết
   lại logic collapse/drawer.
3. Thêm 1 `<li>` nav item mới vào **cả 3 file hiện có** (đồng bộ menu toàn
   app), rồi mới thêm chính trang mới.
4. Đánh dấu `active` đúng 1 mục — mục của trang đang mở.

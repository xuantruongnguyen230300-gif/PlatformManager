# UI Spec — Left Sidebar Menu (điều hướng toàn app)

> ✅ **Đã implement (2026-08-11, cập nhật 2026-08-12)**: sidebar menu theo
> đúng spec này đã được thêm trực tiếp vào `doc/Prototype/dashboard.html`
> — không dùng Figma (bị chặn quota gói Starter, xem
> `spec/dashboard-dti-weekly/spec.md` § Đổi hướng). Đã verify bằng
> chrome-devtools-mcp: desktop (260px), thu gọn (72px, `localStorage` key
> `platform_manager_sidebar_collapsed_v1`), drawer mobile <980px (hamburger
> + backdrop + Escape + focus trap). **2026-08-12**: mở rộng sang 2 trang
> (`dashboard.html` + `doc/Prototype/danh-muc-dti.html` mới), sidebar giờ
> có 2 item gốc — "Dashboard" (phẳng) và "Danh mục" > "DTI" (có con, xem
> mục 1.2/2.5) — cùng lúc Dashboard đổi thành read-only (chi tiết
> `spec/dashboard-dti-weekly/ui-spec.md`) và toàn bộ CRUD/nhập liệu chuyển
> sang `danh-muc-dti.html` (`spec/danh-muc-dti/ui-spec.md`). Mở file trực
> tiếp trong trình duyệt để xem — không cần Figma.

> **Nguồn: GREENFIELD, không phải reverse-engineer.** `doc/Prototype/dashboard.html`
> hiện tại **không có sidebar** — chỉ có `.topbar` full-width, 1 màn hình
> duy nhất. Theo carve-out đã ghi ở `doc/Design/CLAUDE.md` § Fidelity
> Policy ("nếu màn hình được thiết kế hoàn toàn mới, không có trong
> prototype — dựng từ brief theo best-practice, không reverse-engineer code
> không tồn tại"), spec này áp dụng đúng tinh thần đó cho tài liệu
> `spec/` phía FE: **IA, layout, breakpoint, state là quyết định UX mới**,
> nhưng **màu sắc/token, font, bo góc, shadow tái dùng nguyên xi** từ
> `:root` trong `dashboard.html` để đồng bộ hình ảnh với màn hình đã có.
>
> **Cập nhật (2026-08-12)**: thêm module thứ 2 — "Danh mục > DTI"
> (`doc/Prototype/danh-muc-dti.html`), theo quyết định kiến trúc mới:
> Dashboard đổi thành read-only, toàn bộ CRUD/nhập liệu chuyển sang màn
> "Danh mục > DTI" (xem `spec/danh-muc-dti/business-rules.md` và
> `spec/danh-muc-dti/ui-spec.md`). Đây chính là tình huống mà field
> `children?: NavItem[]` ở mục 1.1 đã dự phòng từ trước — nay **dùng tới**,
> không còn "CHƯA dùng ở v1" nữa. Toàn bộ mục 1 dưới đây đã cập nhật lại
> theo đúng cấu trúc 2 item hiện có (1 phẳng + 1 có con).

> Bối cảnh: hiện có **2 điểm đến thật** — "Dashboard" (phẳng) và
> "Danh mục" (cha) > "DTI" (con). Sidebar được thiết kế để **cấu trúc dữ
> liệu điều hướng mở rộng được** cho nhiều module tương lai, nhưng **không**
> tự bịa thêm mục menu nào ngoài mục đã biết.

## 1. Information Architecture

### 1.1. Mô hình dữ liệu điều hướng (data-driven, không hardcode trong template)

Thiết kế menu theo danh sách phẳng các `NavItem`, **không** dùng lớp
`NavSection` (group có tiêu đề riêng ngoài `NavItem`) — `children` đã đủ để
biểu diễn nhóm "Danh mục" hiện có mà không cần thêm khái niệm mới. Shape
gốc **không đổi** so với lần thiết kế đầu (đã dự phòng đúng field cần
dùng):

```
NavItem
├── id: string            → khoá ổn định, dùng cho @for track (KHÔNG track theo index)
├── label: string          → tên hiển thị
├── icon: string            → tên/khoá icon (xem 1.3 — icon set chưa chốt)
├── route: string           → routerLink đích — CHỈ có ý nghĩa khi KHÔNG có children (xem 1.2)
├── section?: string        → nhãn nhóm (optional) — chỉ thêm khi có ≥2 nhóm chức năng
│                              rõ rệt (vd "Theo dõi" vs "Quản trị"); hiện tại "Danh mục" đã đóng
│                              vai trò phân nhóm qua `children`, chưa cần thêm `section` —
│                              2 khái niệm khác nhau (section = nhãn phẳng không click được,
│                              children = nhóm có thể click cha để expand/collapse)
├── badge?: number | 'dot'  → chỗ cho số đếm/thông báo tương lai (vd "3 chỉ tiêu quá hạn")
│                              — CHƯA có nguồn dữ liệu nào cho việc này ở slice hiện tại,
│                              chỉ giữ chỗ trong shape, không tự render badge giả
└── children?: NavItem[]    → submenu lồng cấp 2 — ĐÃ DÙNG (xem 1.2 "Danh mục" > "DTI").
                               Item cha có `children` thì `route` không dùng (cha chỉ toggle
                               expand/collapse, không điều hướng trực tiếp — xem mục 2.5).
                               Chỉ hỗ trợ ĐÚNG 1 cấp lồng (`children` không có `children` con
                               bên trong) — chưa có nhu cầu cấp 3, không tự thiết kế trước.
```

### 1.2. Cấu hình hiện tại (đúng thực trạng — 2 item gốc, 1 item có con)

```
[
  { id: 'dti-weekly', label: 'Dashboard', icon: 'dashboard', route: '/dashboard' },
  { id: 'danh-muc', label: 'Danh mục', icon: 'folder', children: [
      { id: 'danh-muc-dti', label: 'DTI', icon: 'list', route: '/danh-muc/dti' }
  ]}
]
```

`danh-muc` (cha) không có `route` — click vào cha chỉ toggle expand/collapse
`children`, không điều hướng (xem mục 2.5). `danh-muc-dti` (con) là điểm
đến thật duy nhất trong nhóm "Danh mục" hiện tại — khi có module thứ 2 cần
menu con (vd "Danh mục > Nhóm chỉ tiêu" nếu sau này mở CRUD `CriteriaGroup`
theo gợi ý ở `spec/danh-muc-dti/business-rules.md` mục 1.3 rule #5), chỉ
cần push thêm 1 phần tử vào mảng `children` — không đổi cấu trúc component.
Không có `section`, không có `badge` — chưa có bằng chứng/nhu cầu cho 2
field đó ở thời điểm hiện tại.

**Triển khai trong bản `doc/Prototype/*.html` (static, đa trang)**: vì đây
là 2 file HTML tĩnh riêng biệt (không phải 1 SPA có router), `route` được
hiện thực bằng `href` trỏ thẳng tới file trang đích cùng thư mục
(`dashboard.html`, `danh-muc-dti.html`) thay vì `routerLink` — đúng bản
chất kỹ thuật của prototype tĩnh nhiều trang, khi lên Angular thật sẽ đổi
thành `routerLink` theo `route` đã khai ở trên, không đổi UI/IA.

### 1.3. Icon set — quyết định hoãn, không phải thiếu sót

Dự án `src/FE` chưa scaffold, chưa chốt icon library (Material Symbols,
Lucide, Heroicons, hay SVG sprite tự quản lý). Spec này dùng **placeholder
SVG outline 20×20px** (stroke, không fill, khớp phong cách outline hiện có
trong `dashboard.html`). Đã hiện thực 3 icon placeholder trong
`doc/Prototype/{dashboard,danh-muc-dti}.html`: `dashboard` (lưới 4 ô),
`folder` (icon cặp tài liệu, cho item cha "Danh mục"), `list` (icon tài
liệu có dòng kẻ, cho item con "DTI"). Khi FE scaffold và chọn icon library,
thay các khoá `icon: '...'` bằng khoá đúng theo thư viện đã chọn — không
block spec này, rủi ro đổi icon sau này thấp (chỉ 3 khoá cần map lại).

## 2. Layout

### 2.1. Vị trí & kích thước

- Sidebar cố định bên trái, **full-height** (`100vh`/`100dvh`), tách biệt
  khỏi vùng content cuộn.
- **Desktop mở rộng (expanded)**: width **`260px`**. Lý do chọn 260px (nằm
  trong khoảng 240–280px người dùng đã gợi ý): đủ chỗ cho icon 20px + label
  tiếng Việt trên 1 dòng ở cỡ chữ 13–14px (font `Inter` — khớp
  `body{font-family:Inter,...}` đã có), không bị cắt chữ hay wrap 2 dòng gây
  lệch chiều cao item — kể cả khi có thêm item với label dài hơn label hiện
  tại ("Dashboard") trong tương lai, không riêng cho label ngắn đang có.
- **Desktop thu gọn (collapsed)**: width **`72px`** — đủ cho icon 20px +
  padding 2×20px + không gian bấm thoải mái (touch target ~44px chiều cao
  dù đây là desktop, vẫn giữ chuẩn a11y chung).

### 2.2. Có hỗ trợ collapse hay không — QUYẾT ĐỊNH: **CÓ**

Lý do (ghi rõ theo yêu cầu, không mặc định mà không giải thích):
1. **Chi phí dựng ngay bây giờ thấp, chi phí thêm sau cao.** Chỉ 1 item
   hiện tại khiến collapse *trông* thừa, nhưng hành vi collapse (persist
   trạng thái, style icon-only cho từng `NavItem`, animation width) là
   phần khung layout — thêm vào ngay từ đầu không tốn thêm công sức đáng
   kể so với thiết kế lại toàn bộ component khi có module thứ 2–3 cần thêm
   không gian ngang.
2. **Nhu cầu không gian ngang đã có bằng chứng cụ thể**: chính màn hình
   duy nhất hiện tại (`dashboard.html`) có bảng 62 chỉ tiêu với
   `table{min-width:1200px}` — một bảng rộng, thường xuyên cần cuộn ngang
   (`tablewrap{overflow:auto}`). Cho phép thu gọn sidebar còn 72px giải
   phóng ~188px chiều ngang cho đúng màn hình đang tồn tại — lợi ích thực
   tế, không phải suy đoán.
3. Đây là pattern chuẩn của mọi admin dashboard đa module (Ant Design Pro,
   Material admin templates...) — khớp yêu cầu "chuẩn UX/UI" của người
   dùng.

### 2.3. Sơ đồ bố cục desktop (≥980px)

```
┌───────────┬──────────────────────────────────────────┐
│           │  .topbar (sticky top:0, width = phần còn  │
│  sidebar  │  lại, KHÔNG full-viewport như hiện tại)   │
│  (260px   ├──────────────────────────────────────────┤
│  hoặc     │                                            │
│  72px)    │  main (nội dung hiện có của               │
│  full-    │  dashboard.html — không đổi)               │
│  height   │                                            │
│           │  (cuộn dọc độc lập trong vùng content)     │
└───────────┴──────────────────────────────────────────┘
```

Container gốc đổi từ `body > .topbar + main` (hiện tại) thành 1 shell 2
cột: cột trái = sidebar (`position:sticky` hoặc `fixed`, `top:0`,
`height:100vh`), cột phải = 1 flex-column chứa `.topbar` (sticky **trong
phạm vi cột phải**, không còn full-width) + `main` (giữ nguyên
`max-width:1450px;margin:auto` hiện có — chỉ co lại theo chiều rộng cột
phải, không đổi logic).

### 2.4. Cấu trúc nội dung sidebar (trên xuống dưới)

```
.sidebar
├── .sidebar-brand     → logo/tên app ("PlatformManager" hoặc tên ngắn) +
│                          (desktop only) nút toggle collapse — góc trên
├── nav[aria-label="Main"]
│    └── ul > li > a (routerLink) × N NavItem  → xem mục 1
└── .sidebar-footer (tuỳ chọn, KHÔNG dựng ở v1)
     → chỗ dành cho user menu/avatar khi auth Identity được UI hoá —
       CHƯA thiết kế, chỉ giữ chỗ khái niệm (xem mục "Ghi chú permission")
```

`.sidebar-footer` không có nội dung cụ thể ở v1 vì auth UI chưa được yêu
cầu thiết kế — không tự vẽ avatar/tên người dùng giả.

### 2.5. Hành vi item cha có `children` — QUYẾT ĐỊNH: accordion, mặc định mở

Item "Danh mục" có đúng 1 con ("DTI"). 2 lựa chọn khả dĩ: (a) accordion
click-để-mở/đóng, hoặc (b) luôn hiện phẳng con vì chỉ có 1 con (không cần
click). **Chọn (a) — accordion, nhưng mặc định ở trạng thái mở** khi tải
trang. Lý do:

1. **Chi phí dựng ngay rẻ hơn thêm sau** — cùng lý lẽ đã áp dụng cho quyết
   định collapse icon-only ở mục 2.2: khi có module thứ 2–3 cần vào nhóm
   "Danh mục" (vd "Nhóm chỉ tiêu" nếu mở CRUD `CriteriaGroup` sau này), cơ
   chế accordion đã có sẵn, không phải build lại từ đầu.
2. **Mặc định mở** giữ đúng trải nghiệm "phẳng" hiện tại khi chỉ có 1 con —
   người dùng thấy ngay "DTI" mà không cần thêm 1 cú click, không đánh đổi
   UX hiện tại chỉ vì chuẩn bị cho tương lai.
3. Trạng thái mở/đóng **không persist qua session** ở v1 (luôn mở lại khi
   tải trang) — với đúng 1 nhóm, việc nhớ trạng thái đóng của người dùng
   (nếu họ tự đóng) chỉ để lại 1 sidebar trông "thiếu" mất lối vào DTI mà
   không có lý do thực sự cần tiết kiệm không gian dọc (khác hẳn trường hợp
   collapse ngang 260↔72px đã có bằng chứng cụ thể ở mục 2.2). Khi có ≥2
   nhóm, cân nhắc lại việc persist theo từng nhóm.
4. Khi trang đang mở **thuộc về 1 con trong nhóm** (vd đang ở trang DTI),
   nhóm cha luôn ở trạng thái **mở + active-tinted** để phản ánh đúng vị trí
   hiện tại trong cây điều hướng — không có state "cha active nhưng con
   không active" trong phạm vi 1 con hiện tại.

**Desktop thu gọn (72px) — flyout khi hover/focus, KHÔNG dùng accordion**:
ở trạng thái collapsed, không đủ chỗ hiển thị `children` lồng trực tiếp
(icon-only 72px). Thay vào đó: hover hoặc focus (bàn phím) vào icon nhóm
cha → hiện **flyout popover** nổi bên phải icon (nền `--card`, viền
`--line`, shadow `--shadow` — tái dùng đúng token card sẵn có, không màu
mới), liệt kê các `children` với label đầy đủ (không rút gọn icon-only bên
trong flyout). Đây là pattern chuẩn cho sidebar thu gọn có menu lồng (Ant
Design Pro, VS Code activity bar...) — không cần click để mở, giảm số thao
tác so với việc phải mở rộng cả sidebar chỉ để thấy 1 con.

**Mobile drawer (<980px)**: dùng accordion giống desktop mở rộng (không có
khái niệm collapsed 72px ở mobile — xem mục 3), mặc định mở, click cha để
đóng/mở như bình thường.

## 3. Responsive

Dùng lại đúng 2 breakpoint đã tồn tại trong `dashboard.html`
(`@media(max-width:980px)`, `@media(max-width:560px)`) để nhất quán toàn
app — không thêm breakpoint mới.

### ≥980px — Desktop: sidebar cố định (static), không phải drawer

- Sidebar luôn hiển thị, không có backdrop, không có animation ẩn/hiện —
  chỉ animate giữa 2 width (260px ↔ 72px) khi bấm nút collapse.
- Trạng thái collapse/expand **ghi nhớ qua session** (đề xuất
  `localStorage`, key gợi ý `platform_manager_sidebar_collapsed_v1` — theo
  đúng quy ước đặt tên có hậu tố version như 2 key đã có
  `dti_weekly_history_v2`/`dti_weekly_draft_v2`; tên chính xác do FE quyết
  định lúc code, không phải phần bắt buộc của spec).

### <980px — Tablet/Mobile: sidebar chuyển thành drawer ẩn/hiện

- Sidebar **ẩn mặc định**, chuyển thành lớp phủ (`position:fixed`, full
  height, width `min(85vw, 300px)`, trượt vào từ trái —
  `transform:translateX(-100%)` khi đóng).
- Mở qua nút **hamburger** mới thêm vào bên trái `.topbar` (đặt trước
  `.logo`, không thay thế — cả hai cùng hiển thị; đã hiện thực đúng cách
  này ở cả `dashboard.html` lẫn `danh-muc-dti.html`). Spec chốt: **phải có
  1 điểm bấm mở drawer rõ ràng trong topbar ở breakpoint này**, vì topbar
  vẫn cần hoạt động bình thường song song (mỗi trang có action riêng theo
  ngữ cảnh — xem `spec/dashboard-dti-weekly/ui-spec.md` và
  `spec/danh-muc-dti/ui-spec.md`).
- Khi mở: có **backdrop** phủ nội dung phía sau
  (`rgba(20,28,40,.45)` — **tái dùng nguyên giá trị** đã có ở
  `dialog::backdrop` trong `dashboard.html`, không phát minh màu mới),
  bấm backdrop hoặc nhấn `Escape` → đóng drawer. Bấm 1 **nav item lá** (có
  `route`, vd "Dashboard"/"DTI") cũng đóng drawer; bấm **nav item cha** (có
  `children`, vd "Danh mục") chỉ toggle accordion, KHÔNG đóng drawer — 2
  hành vi khác nhau rõ rệt (xem mục 2.5).
- Không thu gọn còn icon-only ở mobile — dưới 980px chỉ có 2 trạng thái:
  ẩn hẳn (đóng) hoặc full width drawer (mở). Collapse icon-only (mục 2.2)
  **chỉ áp dụng ở desktop** — trên màn hẹp, thu gọn còn 72px vẫn chiếm quá
  nhiều diện tích quý giá so với để ẩn hoàn toàn. Flyout khi collapsed (mục
  2.5) cũng theo đó **chỉ áp dụng desktop**, không có ở mobile.

### <560px — Mobile nhỏ

- Kế thừa hành vi drawer của <980px, chỉ khác: width drawer tăng tỉ lệ
  `vw` lên gần full-screen hơn nếu màn hình rất hẹp (`min(90vw, 300px)`),
  chiều cao mỗi nav item item tăng nhẹ để đạt tối thiểu **44px touch
  target** (chuẩn accessibility cho thiết bị cảm ứng nhỏ) — nav item ở
  desktop có thể thấp hơn (theo nhịp `.btn{padding:9px 12px}` hiện có,
  ~38–40px) nhưng ở mobile nới padding dọc lên đảm bảo 44px.

### `@media print` — cần bổ sung khi implement

`dashboard.html` đã có sẵn quy tắc ẩn `.topbar,.filters,.fab,.no-print`
khi in. Khi thêm sidebar, **phải bổ sung `.sidebar` vào danh sách ẩn khi
in** (in báo cáo không cần menu điều hướng) — ghi chú này để FE không quên
khi migrate CSS.

## 4. States

### 4.1. Nav item (mỗi `NavItem`)

| State | Mô tả | Token dùng |
| --- | --- | --- |
| Default | nền trong suốt (kế thừa `--card` của sidebar), chữ + icon màu trung tính | text: `--text`, icon: `--muted` |
| Hover | nền tint nhẹ, con trỏ pointer | nền: `--bg` (`#f3f6fb`) — sidebar có nền `--card` (`#fff`) nên dùng `--bg` làm hover đủ tương phản, không cần token mới |
| Active (route hiện tại) | nền tint theo brand + vạch nhấn trái 3px + chữ/icon đổi màu brand + đậm | nền: tint 8% của `--brand` (`rgba(15,91,215,.08)` — tint phái sinh từ token có sẵn, không phải màu mới, cùng cách `.bdone/.bwork/.bstall` trong `dashboard.html` đã dùng nền tint đi kèm chữ đặc); vạch trái + chữ/icon: `--brand`; `font-weight:700` (khớp `.btn{font-weight:700}` đã có) |
| Focus (bàn phím, Tab) | outline rõ ràng quanh item — **CSS mới, `dashboard.html` hiện chưa định nghĩa `:focus-visible` cho bất kỳ phần tử nào**, cần bổ sung vì đây là điều hướng chính của app, bắt buộc hỗ trợ bàn phím | `outline:2px solid var(--brand); outline-offset:2px` |
| Disabled | **không thiết kế ở v1** — không có nav item nào cần disable (cả 2 item hiện có, "Dashboard" và "DTI", luôn khả dụng); nếu tương lai cần (vd module chưa mở quyền) sẽ bổ sung khi có nhu cầu thật |

### 4.1b. Nav item cha (có `children`) — bổ sung riêng so với 4.1

Item cha ("Danh mục") dùng lại đúng token default/hover/focus ở mục 4.1
(cùng class `.sidebar-navitem`, chỉ thêm phần tử `<button>` thay vì `<a>` vì
không điều hướng — xem mục 2.5), khác biệt duy nhất:

| State | Mô tả | Token dùng |
| --- | --- | --- |
| Active theo con (1 con trong `children` đang active) | Cha nhận **cùng style Active** ở mục 4.1 (nền tint + vạch trái + chữ/icon brand) dù bản thân cha không có `route` — phản ánh vị trí trong cây điều hướng | giống hệt Active ở 4.1, không thêm token mới |
| Mở (accordion expanded) | Chevron (mũi tên) xoay dọc (▾), `children` hiển thị | góc xoay 0°, mặc định |
| Đóng (accordion collapsed) | Chevron xoay ngang (▸), `children` ẩn (`display:none`) | góc xoay `-90deg`, `transition:transform .15s ease` |

### 4.2. Collapsed vs Expanded (desktop)

- **Expanded**: icon + label cùng hiển thị, canh trái.
- **Collapsed**: chỉ icon, canh giữa theo chiều ngang 72px; label ẩn khỏi
  layout nhưng **vẫn cần cho screen reader** (dùng `aria-label` trên `<a>`
  hoặc giữ span label với class ẩn kiểu `sr-only`, không dùng
  `display:none` nếu muốn label vẫn đọc được bằng trình đọc màn hình — chi
  tiết kỹ thuật, không phải quyết định UX, nhưng ghi chú để FE không bỏ
  sót a11y).
- **Tooltip khi collapsed (nav item lá, không có `children`)**: hover vào
  icon hiện label qua `title` attribute (giải pháp đơn giản, đủ dùng cho
  item không có con, không cần dựng component tooltip riêng ở v1 — nếu sau
  này cần tooltip đẹp hơn, nâng cấp thành component, không phải quyết định
  bây giờ).
- **Item cha khi collapsed**: KHÔNG dùng tooltip đơn giản như trên — dùng
  **flyout popover** liệt kê đầy đủ `children` (xem mục 2.5), vì bản thân
  cha không phải điểm đến (không có `route`) nên chỉ hiện label qua `title`
  là không đủ, người dùng cần thấy được (và click được) các con.

### 4.3. Drawer (mobile, <980px)

| State | Mô tả |
| --- | --- |
| Closed (mặc định) | `transform:translateX(-100%)`, backdrop ẩn, không bắt sự kiện |
| Opening/Open | trượt vào (`transform:translateX(0)`), backdrop hiện, focus chuyển vào drawer (focus trap trong lúc mở — a11y chuẩn cho off-canvas nav) |
| Closing | trượt ra, backdrop mờ dần, focus trả về nút hamburger đã mở nó |

## 5. Visual — token tái dùng từ `dashboard.html`

**Không phát minh token màu mới nào** — toàn bộ giá trị dưới đây map thẳng
từ `:root` đã có trong `dashboard.html`:

| Vùng UI sidebar | Token gốc | Giá trị |
| --- | --- | --- |
| Nền sidebar | `--card` | `#fff` |
| Viền phải sidebar | `--line` | `1px solid #dfe6ef` |
| Shadow (chỉ khi ở dạng drawer nổi trên mobile) | `--shadow` | `0 7px 24px rgba(23,39,67,.08)` |
| Brand/logo text | `--text` | `#152033` |
| Subtitle dưới brand (nếu có) | `--muted` | `#6d788b` |
| Nav item default text/icon | `--text` / `--muted` | như trên |
| Nav item hover nền | `--bg` | `#f3f6fb` |
| Nav item active nền | tint 8% của `--brand` | `rgba(15,91,215,.08)` (phái sinh, xem 4.1) |
| Nav item active text/icon/accent bar | `--brand` | `#0f5bd7` |
| Focus outline | `--brand` | `#0f5bd7` |
| Backdrop (mobile drawer) | (tái dùng giá trị `dialog::backdrop`) | `rgba(20,28,40,.45)` |
| Bo góc nav item | theo scale đã có ở `.btn` | `10px` |
| Font | `body{font-family}` | `Inter,Segoe UI,Arial,sans-serif` |

**Không cần thêm token mới.** Mọi giá trị "mới" (tint 8% của brand, outline
focus) là **phái sinh có công thức rõ ràng** từ token gốc, không phải màu
tự sáng tác ngoài palette.

## 6. Tích hợp layout tổng thể

Đổi từ layout hiện tại (`.topbar` full-width phía trên `main`) sang pattern
admin dashboard 2 vùng: **sidebar full-height cố định bên trái (dùng chung
cho mọi trang) + vùng nội dung bên phải chứa topbar-đã-thu-hẹp + main
(khác nhau theo từng trang)**, theo đúng khuyến nghị trong yêu cầu — lý do:
đây là pattern phổ biến nhất cho app nhiều module, tách rõ sidebar là nơi
điều hướng **toàn app, không đổi theo màn hình đang xem** khỏi phần bên
phải (topbar + toolbar hành động + `main`) là nơi chứa nội dung/hành động
**theo-ngữ-cảnh-màn-hình**. Sau các đợt cập nhật UI gần nhất (Dashboard đổi
read-only, CRUD chuyển sang "Danh mục > DTI" — xem
`spec/dashboard-dti-weekly/ui-spec.md` và `spec/danh-muc-dti/ui-spec.md`):
- `.topbar` của `dashboard.html` giờ **chỉ còn logo/title** ("Dashboard"),
  không còn action nào (màn chỉ xem, không có gì để hành động ở topbar).
- `.topbar` của `danh-muc-dti.html` cũng chỉ logo/title ("Danh mục"); toàn
  bộ action nhập liệu ("Import CSV"/"+ Thêm chỉ tiêu"/sửa inline Tiến độ %
  và Ghi chú trong lưới) nằm trong `main` — không nằm trong topbar của
  trang nào. **Cập nhật (vòng phản hồi #2)**: `danh-muc-dti.html` không
  còn cấu trúc 2 tab — đã gộp thành **1 lưới duy nhất**, xem
  `spec/danh-muc-dti/ui-spec.md`.

Không ảnh hưởng nguyên tắc phân tách sidebar-vs-phần-còn-lại đã nêu ở đây —
sidebar giữ nguyên hành vi/vị trí bất kể trang nào trong 2 trang đang mở.

- `.topbar` **không còn** `position:sticky` so với viewport mà so với vùng
  content bên phải (về mặt CSS: bọc `.topbar` + `main` trong 1 container
  flex-column có `overflow-y:auto` riêng, hoặc để `body` cuộn bình thường
  và sidebar dùng `position:fixed` — 2 cách kỹ thuật tương đương, FE chọn
  lúc code, không phải quyết định UX).
- Nội dung bên trong `main` (`.notice`, `.weekbar`, `.kpis`, `.layout`,
  bảng 62 chỉ tiêu, `#history`, `.footer`) **giữ nguyên 100%** — sidebar
  không đổi bất kỳ hành vi/element nào đã có trong `dashboard.html`, chỉ
  bọc thêm khung ngoài.
- `.fab` (mobile "Lưu tuần") giữ nguyên `position:fixed` góc phải dưới —
  không bị ảnh hưởng bởi sidebar vì sidebar (dạng drawer) nằm ở cạnh trái.

### Ghi chú triển khai Angular (định hướng, không phải code)

Theo `src/FE/.claude/docs/architecture.md`, sidebar + shell layout là mối
quan tâm **cross-cutting toàn app** (không thuộc riêng 1 feature) — nên đặt
ở `core/layout/` (component shell bọc `<router-outlet>`, singleton, load 1
lần) chứ không phải `modules/dashboard-dti-weekly/`. Danh sách `NavItem`
(mục 1.2) là cấu hình tĩnh — có thể khai trực tiếp dưới dạng hằng số trong
`core/layout/` cho tới khi có nhu cầu thật sự cần tải động theo
permission/role (xem mục 7). Đây chỉ là gợi ý định hướng cho lúc code,
không phải một phần bắt buộc phải tuân theo tuyệt đối của spec UI này.

## 7. Ghi chú permission (không thiết kế logic, chỉ giữ chỗ)

Danh sách nav item hiển thị **trong tương lai** có thể cần lọc theo
role/permission của người dùng đăng nhập (vd module quản trị chỉ hiện với
role Admin) — nhưng **role Identity cụ thể chưa được chốt** (xem
`doc/ERD/ERD.md` mục "Câu hỏi còn mở" và `spec/dashboard-dti-weekly/business-rules.md`
câu hỏi mở về permission, `spec/danh-muc-dti/business-rules.md` mục 4).
Spec này **không thiết kế** cơ chế ẩn/hiện theo role — với 2 item hiện có
và chưa có role nào được chốt, mọi người dùng đã đăng nhập đều thấy toàn bộ
menu (không có state "ẩn theo quyền" nào cần implement ở v1). Khi role được
chốt, bổ sung logic lọc ở tầng cấu hình `NavItem` (thêm field
`requiredRole?` vào shape ở mục 1.1), không phải viết lại cấu trúc
component.

## 8. Tóm tắt quyết định UX chính

| Quyết định | Lựa chọn | Lý do ngắn gọn |
| --- | --- | --- |
| Có collapse icon-only không | **Có**, 260px ↔ 72px | Chi phí dựng ngay rẻ hơn thêm sau; bảng 62 chỉ tiêu (`min-width:1200px`) hưởng lợi thực tế từ không gian ngang giải phóng |
| Mobile: sidebar tĩnh hay drawer | **Drawer** (ẩn mặc định, mở qua hamburger) | Chuẩn UX phổ biến nhất cho nav chính trên màn hẹp |
| Breakpoint | Dùng lại `980px`/`560px` có sẵn | Nhất quán toàn app, không thêm điểm gãy responsive mới không cần thiết |
| Phân nhóm menu (`NavSection` vs `children`) | Dùng `children` (đã có sẵn trong shape), **không** thêm `NavSection` | "Danh mục" là 1 điểm đến cha thật có thể chứa nhiều trang con — đúng ngữ nghĩa `children` (nhóm điều hướng lồng), khác `NavSection` (chỉ là nhãn phẳng, không click được) |
| Item cha có `children`: accordion hay luôn phẳng | **Accordion, mặc định mở** (desktop/mobile); **flyout khi hover/focus** ở trạng thái collapsed 72px | Mặc định mở giữ trải nghiệm gần như phẳng khi chỉ có 1 con; accordion sẵn sàng cho module thứ 2–3 vào nhóm mà không phải dựng lại; flyout là pattern chuẩn cho sidebar thu gọn có menu lồng (xem mục 2.5) |
| Token màu | Tái dùng 100% từ `:root` của `dashboard.html` | Đồng bộ hình ảnh với màn hình đã có, không tạo palette song song |
| Vị trí trong Angular app | `core/layout/` (đề xuất, không bắt buộc) | Sidebar là cross-cutting toàn app theo đúng định nghĩa `core/` trong `architecture.md`, không thuộc 1 feature cụ thể |

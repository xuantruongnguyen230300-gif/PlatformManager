---
kind: luat
scope: du-an
verified: 2026-09-06
project: "PlatformManager"
status: "draft"
updated: "2026-09-09"
component: "Card"
sources: ["src/FE/src/styles.scss", "src/FE/src/app/platform/phan-quyen/pages/phan-quyen/phan-quyen.page.html"]  # 2 nguồn dưới modules/dashboard/ đã xoá 2026-08-29 cùng module DtiWeekly
---

# Card

> 🗄️ **Chỗ nào dưới đây nhắc `/dashboard` hoặc `/danh-muc/dti` là LỊCH SỬ, không phải hiện trạng.**
> Module nghiệp vụ `DtiWeekly` — cả cây `src/FE/src/app/modules/` — đã xoá 2026-08-29 để xây lại; danh
> sách route sống hôm nay đọc thẳng ở `src/FE/src/app/app.routes.ts`. Spec này vẫn là `kind: luat` vì
> component còn dùng thật trên màn Core, nên **không** hạ cả file xuống `lich-su` — chỉ những chỗ nhắc
> màn đã xoá mới bị hạ cấp, và chúng đều mang dấu 🗄️ hoặc được viết ở thì quá khứ. Nội dung đầy đủ
> của hai màn đó nằm ở file chủ lịch sử [`../Screens/01-dashboard.md`](../Screens/01-dashboard.md) và
> [`../Screens/02-danh-muc-dti.md`](../Screens/02-danh-muc-dti.md) (§5 — một chủ đề một file chủ); code
> tra ở commit `98a5d96`.
>
> 🔄 **Đã đối chiếu toàn file 2026-09-06.** Khối ở chỗ này trước đây nói file chưa được so lại sau
> lần viết lại `styles.scss` ngày 2026-08-29 — nay đã so, và tìm ra hai chỗ sai: Reference markup chép
> hai câu tiếng Việt khai cứng (nay là khoá dịch), và nó đặt nút "Thêm người dùng" trong hàng `.title`
> trong khi nút đó đã chuyển xuống `<app-toolbar>` từ 2026-08-31 (xem [`Toolbar.md`](./Toolbar.md)).
> Bảng nguồn cuối file cũng đã bỏ bảy đường dẫn trỏ vào cây module đã xoá.
>
> Trích dẫn dạng `styles.scss § <selector>` neo theo tên selector chứ không theo số dòng; giá trị thật
> lấy bằng lệnh, đừng tin số chép trong bảng: `grep -n '^\.card' src/FE/src/styles.scss`.

**Description:** The generic white surface container (`styles.scss` § `.card`) used as the shell for every major section on every in-shell route (route sống liệt kê ở `src/FE/src/app/app.routes.ts`). It is the app's primary layering device: the palette comment that opens `:root` in `styles.scss` (`grep -n 'trông bẹt' src/FE/src/styles.scss`) records the deliberate move from "border-first" to "fill-first", so a card separates from the page by **shadow**, with `colors.line` left as a faint hairline rather than a structural border.

## Anatomy
Rectangle: bg `colors.card`, 1px `colors.line` border, `shadow` (two layers), `rounded.lg` radius, `spacing.card-padding` on all sides. The card supplies no internal layout beyond padding. Most instances open with the global `.title` row (`styles.scss` § `.title`) — a `space-between` flex holding an `<h2>` at `typography.h2-title` and, on the right, either a `.muted` caption or a `Button`. Both shapes ship: the user list keeps only the live row count there, because its "+ Thêm người dùng" button moved down into the toolbar on 2026-08-31 (`quan-tri-nguoi-dung.page.html:2-15`, and [`Toolbar.md`](./Toolbar.md)); the permission screen still puts its **Lưu thay đổi** button in the row (`phan-quyen.page.html:54-69`).

## Variants

| Variant | Classes | Key values | When to use |
| --- | --- | --- | --- |
| Generic section card | `card` | bg `colors.card`, border `colors.line`, `shadow`, `rounded.lg`, `spacing.card-padding` | Every top-level section: the user list (`quan-tri-nguoi-dung.page.html:1`), the permission matrix panel (`phan-quyen.page.html:1`) |

**Đây là biến thể duy nhất còn sống.** Các biến thể còn lại mà bản 2026-08-22 của spec này liệt kê
đã xoá cùng module `DtiWeekly` — xem mục 🗄️ ngay dưới. The one visually distinct surface in the app,
`.login-card`, is a *different* class with its own `spacing.auth-card-padding`; it is not a `.card`
variant (see the auth-shell component spec).

### 🗄️ Biến thể đã xoá cùng module `DtiWeekly` (2026-08-29)

Các dòng dưới đây **không còn là luật**: cả class lẫn màn hình dùng chúng đã xoá khỏi `src/FE` ngày
2026-08-29. Vì file đích không còn trên đĩa, cột "Từng dùng ở" viết ở **thì quá khứ** và **không mang
`file:dòng`** — không ai kiểm được một neo trỏ vào thứ đã biến mất. Kiểm bằng lệnh thay vì tin bảng:
`grep -n 'card kpi\|weekbar\|criteria-table-card\|dti-grid-card\|history-card' src/FE/src/styles.scss`.
Giữ lại vì chúng là đầu vào thiết kế cho lần xây lại — đọc kèm
[`../Screens/01-dashboard.md`](../Screens/01-dashboard.md).

| Biến thể (đã xoá 2026-08-29) | Classes | Key values khi còn sống | Từng dùng ở (màn đã xoá) |
| --- | --- | --- | --- |
| Generic section card trên dashboard | `card` | như trên | Hai panel trên cùng của màn dashboard |
| KPI tile | `card kpi` | same box + a fixed label/value/sub anatomy | `Components/KpiTile.md` từng tách riêng cho nó; **file spec đó cũng đã xoá 2026-08-29** — mô tả còn lại nằm trong `../Screens/01-dashboard.md` |
| Toolbar card | `weekbar card no-print` | same box; screen-local `.weekbar` adds the horizontal flex layout and is hidden when printing | Period toolbar của dashboard. Vai trò này nay thuộc [`Toolbar.md`](./Toolbar.md) |
| Criteria-table card | `card criteria-table-card` | same box + `margin-top:16px`, khai trong stylesheet riêng của component | Section lưới tiêu chí của dashboard |
| Catalogue grid card | `card dti-grid-card` | same box + `display:flex; flex-direction:column; min-height:0` để lưới bên trong co theo chiều cao còn lại | Shell của trang danh mục DTI |
| History card | `card history-card` | same box + screen-local layout khai trong stylesheet của trang dashboard | Panel "kỳ đã lưu" của dashboard |

🔄 **SỬA 2026-09-08 — gỡ 8 neo chết khỏi bảng này.** Sáu dòng trên trước đây gắn 8 trích dẫn dạng
ngắn trỏ vào `dashboard.page.html`, `period-toolbar.html`, `criteria-table.html`,
`criteria-table.scss`, `danh-muc-dti.page.html` và `danh-muc-dti.page.scss` — viết như thể vẫn mở
được. Cả sáu file đã xoá khỏi đĩa 2026-08-29 cùng cây `src/FE/src/app/modules/`, nên không neo nào
trong số đó kiểm được nữa. Từng neo **đã được đối chiếu với commit `98a5d96` ngày 2026-09-08 và cả 8
đều đúng tại commit đó** — nội dung bảng là lịch sử thật, chỉ đường dẫn là chết. Đường dẫn không được
chép lại vào đây, theo đúng luật ở § Sources cuối file này; cần mã thì tra
`git show 98a5d96 -- src/FE/src/app/modules/`.

Mọi modifier trên chỉ đổi layout hoặc margin — **không biến thể nào đổi background, border, radius,
shadow hay padding của `.card`.** Quy ước đó vẫn là luật; chỉ các ví dụ là lịch sử.

## States
<!-- Exactly these five rows, in this order — treatments as rendered by the shipped CSS. -->

| State | Treatment |
| --- | --- |
| default | bg `colors.card`, border 1px `colors.line`, `shadow`, `rounded.lg`, `spacing.card-padding` (`styles.scss` § `.card`) |
| hover | **Not styled** — no `.card:hover` rule exists in any stylesheet; the card is a static container, never a clickable surface |
| focus | **N/A** — no `tabindex`, not an interactive control. Focusable descendants (buttons, fields) style themselves |
| active | **N/A** — not interactive |
| disabled | **N/A** — not a form control; the card itself never dims. 🗄️ Ví dụ duy nhất cho "section không được sửa" là màn DTI đã xoá 2026-08-29: nó ẩn hẳn cụm action (`danh-muc-dti.page.html:36-41`) và hiện `NoticeBanner`. Không còn call site nào trên màn Core |

## Tokens Used
- `colors.card`, `colors.line`, `colors.text`
- `rounded.lg`
- `spacing.card-padding`
- `shadow` (`--shadow`, two layers — `Tokens/colors.md`)
- `typography.h2-title` (via the `.title` row)

🗄️ `margin-top:16px` on `.criteria-table-card` **was** a literal off the spacing scale (nearest step `--sp-5` is 14px). Class này đã xoá 2026-08-29; ghi lại để lần xây lại đừng lặp lại.

## Reference markup

```html
<div class="card">
  <div class="title">
    <h2>{{ 'quan-tri-nguoi-dung.title' | translate }}</h2>
    <span class="muted" aria-live="polite">
      {{ 'quan-tri-nguoi-dung.grid.totalCount' | translate: { count: totalCount() } }}
    </span>
  </div>
  …
</div>
```

🗄️ **Mẫu đã xoá cùng `DtiWeekly` (2026-08-29)** — giữ làm đầu vào thiết kế, không phải mã đang
chạy: class `.kpi` và component `app-group-progress-list` đều không còn trong `src/FE`.

```html
<div class="card">
  <div class="title">
    <h2>Tiến độ theo nhóm</h2>
    <span class="muted">Tuần hiện tại</span>
  </div>
  <app-group-progress-list [groups]="aggregate().Groups" />
</div>

<div class="card kpi">
  <div class="label">Tiến độ chung tuần này</div>
  <div class="value">82,1%</div>
  <div class="sub">Bình quân Tiến độ %, gia quyền theo Điểm tối đa</div>
</div>
```

Sources (đối chiếu 2026-09-06, chỉ liệt nguồn CÒN SỐNG): `src/FE/src/styles.scss` § `.card`, § `.title`, § `.title h2`, và khối chú thích mở `:root` — lý do fill-first, tìm bằng `grep -n 'trông bẹt' src/FE/src/styles.scss`; `src/FE/src/app/platform/quan-tri-nguoi-dung/pages/quan-tri-nguoi-dung/quan-tri-nguoi-dung.page.html:1`, `:2-15`; `src/FE/src/app/platform/phan-quyen/pages/phan-quyen/phan-quyen.page.html:1`, `:54-69`.

Nguồn của các biến thể đã xoá (dashboard, danh mục DTI, KPI tile, period toolbar) **không còn được liệt ở đây**: cả cây module nghiệp vụ biến mất 2026-08-29 nên mọi đường dẫn tới chúng chỉ còn là tên file trong lịch sử git — tra ở commit `98a5d96`, hoặc đọc mô tả màn ở [`../Screens/01-dashboard.md`](../Screens/01-dashboard.md). Chép lại đường dẫn chết vào đây chỉ để "giữ dấu vết" là cách bảng nguồn này đã mục ruỗng lần trước.

## Do / Don't

- ✅ Put every top-level section inside a `.card` — every in-shell route does, without exception.
- ✅ Separate cards with `shadow`, never a heavier border — reaching for `colors.border-strong` on a card reverts the fill-first decision recorded in the palette comment opening `:root` in `styles.scss`.
- ✅ Open a card with the `.title` row when it needs a heading; put the section's single action or a `.muted` caption on its right.
- ✅ Add screen-local layout via a second class and leave the `.card` box untouched — that is the shipped convention. (🗄️ Hai ví dụ cũ `dti-grid-card` / `criteria-table-card` đã xoá 2026-08-29 cùng màn của chúng.)
- ❌ Don't vary padding, radius or shadow per section; the app uses exactly one card treatment.
- ❌ Don't nest a `.card` inside a `.card` — no instance does.

## Normalize on redesign
1. `.card` keeps a 1px `colors.line` border **and** a two-layer shadow. After the fill-first change the border is largely vestigial — decide whether the hairline still earns its place, since removing it would simplify every surface to one mechanism.
2. 🗄️ **Hết hiệu lực cùng màn của nó, 2026-08-29.** `.criteria-table-card`'s `margin-top:16px` was off the spacing scale; the gap between the other dashboard sections came from the layout's `--sp-5` (14px), so the criteria grid sat 2px lower than its siblings for no stated reason. Ghi lại để lần xây lại đừng lặp lại.
3. 🗄️ **Hết hiệu lực, 2026-08-29.** Bốn modifier one-off của `.card` nằm rải ở bốn file, ba trong số đó chỉ đặt flex/margin; một utility `.card--fill` đã có thể gộp hết. Cả bốn đã xoá cùng `DtiWeekly`, nên đề xuất này chỉ còn giá trị làm nguyên tắc cho lần xây lại.

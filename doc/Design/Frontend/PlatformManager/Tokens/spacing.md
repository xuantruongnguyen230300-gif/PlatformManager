---
kind: luat
scope: du-an
verified: 2026-09-06
project: "PlatformManager"
status: "draft"
updated: "2026-09-06"
category: "spacing"
live_source: "src/FE/src/styles.scss"
---

# Spacing — PlatformManager Design System

> **Fidelity:** every value below is extracted from the live app AS-SHIPPED — never invent values outside this file. Proposed changes go to "Normalize on redesign" in the relevant spec, not here.

## Live Source & Extraction Method

**Re-extracted 2026-08-29**, mirroring the `src/FE/src/styles.scss` rewrite of the same day. Every `--sp-*`, `--fs-*`-adjacent and `--radius-*` value was re-read from `:root` and compared with the previous revision: **none of them moved**. What changed is the set of tokens (two added) and the set of literals around them, because the component library in `styles.scss` was rebuilt.

`:root` (`src/FE/src/styles.scss` § `:root`) holds the five-step `--sp-*` scale (§ `--sp-1` … `--sp-5`), the six-step `--radius-*` scale (§ `--radius-sm` … `--radius-pill`), three shell measurements (§ `--sidebar-w`, `--sidebar-w-collapsed`, `--container-max-width`) and — new this pass — two data-grid height tokens (§ `--grid-h`, `--grid-h-min`).

Spacing does not vary by theme (no dark mode exists — see `Tokens/colors.md`), so every token here belongs to the `global` set in `tokens.json`.

**Citation policy — no line numbers into `styles.scss` at all.** The rule and its reasoning are held in one place: `doc/Design/CLAUDE.md` § Neo trích dẫn vào `styles.scss`. Read it there. Every row below cites the file plus the identifier.

> 🔄 **SỬA 2026-09-06.** The previous revision kept the carve-out *"line numbers are given **only for the `:root` block** … which held still across two edits"* and pinned that block to a line range. The range was wrong: `:root` opens and closes 100 lines apart, at neither of the two numbers given. `Tokens/colors.md` dropped the same carve-out on 2026-09-04; this file and `Tokens/typography.md` were left carrying it. Locate the block with `grep -n '^:root' src/FE/src/styles.scss` instead.

## Token Table — Padding & Gap Scale

`src/FE/src/styles.scss` § `:root`. The scale is non-linear and tight — a deliberate compact-density choice.

| Name | Value | Live variable / selector | Declared at |
| --- | --- | --- | --- |
| sp-1 | `4px` | `--sp-1` — collapsed-sidebar nav padding, `.role-checkboxes` margin-top, `.lang-switch` gap + `.lang-switch__option` vertical padding | `src/FE/src/styles.scss` § `--sp-1` |
| sp-2 | `6px` | `--sp-2` — `.btn` vertical padding, `th`/`td` vertical padding, every input's vertical padding, `.form-row` gap, `.filter-chips` gap, `.form-error` margin, `.role-cell` gap, `.row-actions` gap, `.role-checkbox` gap | `src/FE/src/styles.scss` § `--sp-2` |
| sp-3 | `8px` | `--sp-3` — `.btn` horizontal padding, `th`/`td` horizontal padding, `.toolbar` gap + vertical padding, `.title` gap, `.dialog-actions` gap, `.notice` gap, `.user-cell` gap, `.toast-stack` gap | `src/FE/src/styles.scss` § `--sp-3` |
| sp-4 | `10px` | `--sp-4` — `.title` margin-bottom, `.form-row` margin-bottom, `.field` margin-bottom, `.toolbar` horizontal padding, `.seg-btn` horizontal padding, `.dialog-head` gap, `.toast-item` vertical padding, `.role-checkboxes` gap | `src/FE/src/styles.scss` § `--sp-4` |
| sp-5 | `14px` | `--sp-5` — `.card` padding, `dialog` padding, `.filter-panel` padding, `.toolbar` margin-bottom, `.notice` horizontal padding, `.dialog-actions` margin-top, `.toast-item` horizontal padding, `.toast-stack` offset from the viewport edges | `src/FE/src/styles.scss` § `--sp-5` |

`--sp-5` gained a use this pass: `.dialog-actions` now takes its `margin-top` from the scale, replacing three hand-typed values (`8px` ×2, `12px` ×4, `16px` ×1) that used to differ per dialog. `src/FE/src/styles.scss` records the swap and that it moves any given dialog by at most 2px.

### Padding & gap literals bypassing the scale — `styles.scss`

| Value | Selector | Declared at |
| --- | --- | --- |
| `1px var(--sp-2)` | `.text-good.filled` / `.text-warn.filled` / `.text-bad.filled` | `src/FE/src/styles.scss` |
| `4px var(--sp-2)` | `.btn.sm` (vertical literal, horizontal tokenised) | `src/FE/src/styles.scss` |
| `11px` | `.btn-block` | `src/FE/src/styles.scss` |
| `3px 6px` | `.badge` | `src/FE/src/styles.scss` |
| `0 4px` | `.filter-count` | `src/FE/src/styles.scss` |
| `2px var(--sp-2) 2px var(--sp-3)` | `.filter-chip` | `src/FE/src/styles.scss` |
| `10px 12px 10px 36px` | `.input-icon input` — the 36px left inset clears the leading `pi` icon | `src/FE/src/styles.scss` |
| `… … … 30px` | `.toolbar .input-icon input` — toolbar variant, smaller icon inset so the field matches button height | `src/FE/src/styles.scss` |
| `padding-right: 40px` | `.input-icon:has(.icon-btn) input` — clears the trailing show/hide button | `src/FE/src/styles.scss` |
| `margin-bottom: 20px` | `.field-row` | `src/FE/src/styles.scss` |
| `12px 4px` | `.footer` | `src/FE/src/styles.scss` |
| `margin-top: 2px` | `.notice > .pi` — optical alignment of the icon to the first text line | `src/FE/src/styles.scss` |

### Tên ghép chỉ có ở `DESIGN.md` — hai sổ, hai bộ khoá

**Ghi nhận 2026-09-08. Đây là mô tả một khoảng lệch đang tồn tại, không phải một luật.**

Khối `spacing:` của `DESIGN.md` khai một họ khoá **ghép** — kiểu `<thành-phần>-padding` — mà **không**
khoá nào trong họ đó có mặt ở file này hay ở `Tokens/tokens.json`. Đếm bằng lệnh, đừng chép danh sách
(`.claude/CLAUDE.md` §6):

```bash
grep -nE '^  [a-z0-9-]+-padding:' doc/Design/Frontend/PlatformManager/DESIGN.md
```

PASS của việc *"hai sổ đã khớp"* = mỗi khoá lệnh trên in ra hoặc có một dòng ở file này, hoặc được
tuyên bố tường minh là **bí danh chỉ dành cho Stitch**. Hôm nay chưa đạt: chúng chỉ tồn tại ở
`DESIGN.md`, trong khi các spec component thì trích chúng như thể là token thật.

**Sổ nào là chủ:** `Tokens/*` (file này + `tokens.json`). `DESIGN.md` là **bản chiếu** phục vụ nhập vào
Google Stitch — công cụ ngoài không nội suy được `{token.reference}` nên nó cần giá trị literal, và đó
là lý do duy nhất họ khoá ghép kia ra đời. Chiều bắt buộc: `DESIGN.md` không được lệch khỏi `Tokens/*`
hay khỏi source.

**Vì sao khoảng lệch này không tự lộ ra:** không cổng nào so hai sổ với nhau. `check-docs.sh` kiểm
đường dẫn và số dòng, không kiểm xem một tên token được trích ở spec có tồn tại ở sổ token không. Ca đã
xảy ra thật: `Components/Input.md` trích `spacing.input-padding` và `spacing.auth-input-padding` ở bốn
chỗ suốt nhiều ngày, cổng xanh trơn, và người đọc lần theo tham chiếu vào `Tokens/` thì không thấy gì.

**Hai loại khoá ghép, xử lý khác nhau — đừng gộp:**

| Loại | Ví dụ | Cách đúng |
| --- | --- | --- |
| Source **đã** ghép từ token thật | `input-padding` = `var(--sp-2) var(--sp-3)`; `cell-padding`, `button-padding` cùng dạng | Spec trích **hai token thành phần**. Đặt tên thứ ba là bịa ra một token source không có, và nó **giấu mất** việc giá trị đã được token hoá |
| Source dùng literal ngoài thang | `auth-input-padding` = `10px 12px 10px 36px` (`.input-icon input`) | Spec trích **literal**, kèm trỏ về bảng § Padding & gap literals ở trên — nơi file này đã ghi nó, có chủ đích là **không** đặt tên |

`Components/Input.md` đã được sửa theo đúng bảng này ngày 2026-09-08 và là ví dụ mẫu. Các spec còn lại
vẫn trích tên ghép (`Button.md`, `Card.md`, `Table.md`, `DataTable.md`, `NoticeBanner.md`, `AuthCard.md`,
`Screens/05-auth.md`, và bốn bộ `Prompts/`) — **chưa sửa, cố ý**: mỗi cái là một quyết định riêng cần
mở source ra đo, và sửa hàng loạt mà không đo là đúng cách sinh ra khoảng lệch này ngay từ đầu.

`tokens.json` **cố ý không** mang họ khoá này: W3C DTCG không có kiểu cho một shorthand padding nhiều
giá trị, nên nhét vào sẽ tạo ra biến Figma mà Tokens Studio không dựng lại được. Khoảng lệch giữa
`tokens.json` và `DESIGN.md` ở điểm này là **có lý do**; khoảng lệch giữa `DESIGN.md` và file này thì
không.

### Padding & gap literals in component SCSS

Read 2026-08-29; cited by file plus selector because these files were in flux.

| Value | Selector | Source file |
| --- | --- | --- |
| `32px 28px` | `.login-card` | `src/FE/src/app/shared/components/auth-card/auth-card.scss` |
| `24px` | `.login-brand` margin-bottom | `src/FE/src/app/shared/components/auth-card/auth-card.scss` |
| `28px` | `td.indent` padding-left (permission tree) | `src/FE/src/app/platform/phan-quyen/components/permission-matrix/permission-matrix.scss` |
| `9px` | `.sidebar-hamburger` | `src/FE/src/app/shared/components/topbar/topbar.scss` |
| `10px` | `main` padding ≤560px; `.sidebar-navitem` padding ≤560px | `src/FE/src/app/app.scss`, `src/FE/src/app/shared/components/sidebar/sidebar.scss` |
| `10px 12px` | collapsed-sidebar flyout `.sidebar-subitem` | `src/FE/src/app/shared/components/sidebar/sidebar.scss` |
| `38px` | `.sidebar-subitem` padding-left | `src/FE/src/app/shared/components/sidebar/sidebar.scss` |

## Token Table — Radius Scale

`src/FE/src/styles.scss` § `:root`. No value changed this pass.

| Name | Value | Live variable / selector | Declared at |
| --- | --- | --- | --- |
| radius-sm | `7px` | `--radius-sm` — `.btn`, `.icon-btn`, `.segmented`, every input, `.text-*.filled`, `.login-error`, `.badge.outline`, and `.skip-link` (bottom corners only) | `src/FE/src/styles.scss` § `--radius-sm` |
| radius-md | `9px` | `--radius-md` — `.toolbar`, `.notice`, `.filter-panel`, `.sidebar-navitem`, `.toast-item` | `src/FE/src/styles.scss` § `--radius-md` |
| radius-lg | `16px` | `--radius-lg` — `.card`, `.login-card` | `src/FE/src/styles.scss` § `--radius-lg` |
| radius-dialog | `15px` | `--radius-dialog` — native `dialog` | `src/FE/src/styles.scss` § `--radius-dialog` |
| radius-table | `12px` | `--radius-table` — `.tablewrap` | `src/FE/src/styles.scss` § `--radius-table` |
| radius-pill | `999px` | `--radius-pill` — `.badge`, `.filter-count`, `.filter-chip`, `.dialog-icon`, `.toast-icon`, `.lang-switch__option` | `src/FE/src/styles.scss` § `--radius-pill` |

`--radius-md` picked up three new consumers in the rewrite (`.toolbar`, `.notice`, `.filter-panel`); `.notice` previously used a `12px` literal.

### Radius literals bypassing the scale

| Value | Selector | Source file |
| --- | --- | --- |
| `0 3px 3px 0` | `.sidebar-navitem.active::before` rail | `src/FE/src/app/shared/components/sidebar/sidebar.scss` |
| `7px` | `.brand-mark` (sidebar) — numerically `--radius-sm`. 🔄 **SỬA 2026-09-06:** the previous revision also listed `.sidebar-toggle` here. That rule was reduced to `margin-left` plus an icon transition; its shape now comes from the global `.icon-btn`, and the file's own comment records the removal | `src/FE/src/app/shared/components/sidebar/sidebar.scss` |
| `10px` | collapsed-sidebar flyout submenu | `src/FE/src/app/shared/components/sidebar/sidebar.scss` |
| `12px` | `.brand-mark` (auth) — numerically `--radius-table` | `src/FE/src/app/shared/components/auth-card/auth-card.scss` |
| `50%` | `.avatar` (circle) | `src/FE/src/app/platform/quan-tri-nguoi-dung/components/user-grid-table/user-grid-table.scss` |

## Token Table — Breakpoints

Three widths plus print. Re-tallied 2026-09-06 with `grep -rn '@media' src/FE/src --include=*.scss`.

> 🔄 **SỬA 2026-09-06 — the print row named three files that carry no print rule.** `sidebar.scss` and `topbar.scss` each dropped their `@media print` block on 2026-08-29 (both files still carry a comment saying so), and `app.scss` never had one. Print rules are centralised in `styles.scss` by the convention in `doc/huong_dan/quy-uoc/fe-ui-conventions.md` § "In ấn".

| Name | Value | Live variable / selector | Declared at |
| --- | --- | --- | --- |
| breakpoint-tablet | `max-width: 980px` | sidebar becomes an off-canvas drawer, shell offset drops to 0, hamburger appears | `src/FE/src/app/app.scss`, `src/FE/src/app/shared/components/sidebar/sidebar.scss`, `src/FE/src/app/shared/components/topbar/topbar.scss` |
| breakpoint-mobile | `max-width: 560px` | `.toolbar` children stretch, `.filter-panel` narrows to `min(320px,86vw)`, `.form-grid` collapses to one column | `src/FE/src/styles.scss`, plus `app.scss`, `sidebar.scss`, `topbar.scss`, `trang-chu.page.scss` |
| breakpoint-desktop | `min-width: 981px` | one block — the collapsed-sidebar hover flyout | `src/FE/src/app/shared/components/sidebar/sidebar.scss` |
| breakpoint-print | `print` | two blocks, **both in `styles.scss`**: one hides `.no-print`, the other zeroes `.shell-content`'s `margin-left` and drops the `main` max-width. Sidebar and topbar disappear by carrying `.no-print` in their own templates, not by a rule of their own | `src/FE/src/styles.scss` **only** |

## Token Table — Structural Measurements

### Declared in `:root` — shell (`src/FE/src/styles.scss` § `--sidebar-w` … `--container-max-width`)

| Name | Value | Live variable / selector | Declared at |
| --- | --- | --- | --- |
| sidebar-w | `220px` | `--sidebar-w` — `.sidebar` width, `.shell-content` margin-left | `src/FE/src/styles.scss` § `--sidebar-w` |
| sidebar-w-collapsed | `60px` | `--sidebar-w-collapsed` — `.sidebar.collapsed`, `.shell-content.collapsed` | `src/FE/src/styles.scss` § `--sidebar-w-collapsed` |
| container-max-width | `1600px` | `--container-max-width` — `main` and `.topin` | `src/FE/src/styles.scss` § `--container-max-width` |

### Declared in `:root` — data-grid height, **new 2026-08-29** (`src/FE/src/styles.scss` § `--grid-h`, `--grid-h-min`)

Both re-verified present in `:root` on 2026-08-29.

| Name | Value | Live variable / selector | Declared at |
| --- | --- | --- | --- |
| grid-h | `calc(100dvh - 280px)` | `--grid-h` — `max-height` of `.tablewrap.scroll`. **No `p-table` consumes it any more** — see the correction below | `src/FE/src/styles.scss` § `--grid-h` |
| grid-h-min | `220px` | `--grid-h-min` — `min-height` of the same region | `src/FE/src/styles.scss` § `--grid-h-min` |

> 🔄 **SỬA 2026-09-06 — the PrimeNG half of this row was wrong.** The previous revision said `--grid-h` is "passed straight to PrimeNG as `scrollHeight` on the one shipped `p-table`", and cited a line of `user-grid-table.html`. Neither half survives: that file no longer contains a `p-table` at all — it renders `<app-data-grid class="grid-host">` and supplies only column templates — and the `p-table` that moved into `src/FE/src/app/shared/components/data-grid/data-grid.html` sets **`scrollHeight="flex"`**, handing the height to the CSS flex chain rather than to the token. The cited line number still resolved to a line inside the file, so the repo gate saw nothing. (The comment above `.tablewrap.scroll` in `src/FE/src/styles.scss` still advertises the old recipe; that stale comment lives in `src/`, which this file may not edit.)

These replace three hand-picked heights (`480` / `520` / `560px`) that each table used to choose for itself. The comment above `--grid-h` in `src/FE/src/styles.scss` (`grep -n 'grid-h:' src/FE/src/styles.scss`) records the reasoning: a fixed pixel height wastes space on a 1080p screen and overflows the fold on a 768p laptop, at which point the page itself scrolls and the sticky `th` scrolls away with it. The `280px` subtrahend is the fixed chrome above and below the grid — topbar, card title, toolbar, paginator.

⚠️ **`--grid-h` is deliberately overridden on list pages.** `.page-fill .tablewrap.scroll` (`src/FE/src/styles.scss`) resets `max-height: none` and `min-height: 0` so a flex chain decides the height instead. `src/FE/src/styles.scss` explains why: the fixed chrome is **not** 280px everywhere — it measured 443px on one list page and 280px on another — so any single constant is wrong somewhere. `--grid-h` is therefore the fallback for a grid **not** inside a `.page-fill` page, not the value most grids actually use. Recording only the token value here would misdescribe the app.

### Shipped as literals — `styles.scss`

| Name | Value | Live variable / selector | Declared at |
| --- | --- | --- | --- |
| icon-button | `24px × 24px` | `.icon-btn` | `src/FE/src/styles.scss` |
| checkbox | `16px × 16px`, `accent-color: var(--brand)` | `.check` | `src/FE/src/styles.scss` |
| input-w-num | `88px` | `.input.w-num` — integer fields | `src/FE/src/styles.scss` |
| input-w-pct | `76px` | `.input.w-pct` — percentage fields | `src/FE/src/styles.scss` |
| input-w-date | `132px` | `.input.w-date` | `src/FE/src/styles.scss` |
| input-w-sm | `150px` | `.input.w-sm` — codes, short keywords | `src/FE/src/styles.scss` |
| input-w-md | `240px` | `.input.w-md` — names, search | `src/FE/src/styles.scss` |
| input-w-lg | `360px` | `.input.w-lg` | `src/FE/src/styles.scss` |
| select-width | `auto`, `min-width: 120px`, `max-width: 240px` | `select.input`, `.toolbar select` | `src/FE/src/styles.scss` |
| textarea-min-height | `64px` | `.form-row textarea` | `src/FE/src/styles.scss` |
| toolbar-search-width | `260px` | `.toolbar .search` — the single declaration since 2026-08-29; the page-scoped `min-width: 220px` copy went with the `quan-tri-nguoi-dung.page.scss` file, which no longer exists | `src/FE/src/styles.scss` |
| toolbar-grow-min-width | `200px` | `.toolbar .grow` | `src/FE/src/styles.scss` |
| toolbar-separator | `1px` wide, `align-self: stretch` | `.toolbar-sep` | `src/FE/src/styles.scss` |
| filter-count | `min-width: 16px`, `height: 16px` | `.filter-count` | `src/FE/src/styles.scss` |
| filter-panel-width | `320px` (`min(320px, 86vw)` ≤560px) | `.filter-panel` | `src/FE/src/styles.scss` |
| filter-panel-offset | `calc(100% + 6px)` below the trigger | `.filter-panel` | `src/FE/src/styles.scss` |
| dialog-icon | `40px × 40px` | `.dialog-icon` | `src/FE/src/styles.scss` |
| dialog-width | `min(700px, 92vw)` | `dialog` | `src/FE/src/styles.scss` |
| dialog-width-form | `min(560px, 92vw)` | `dialog.form-dialog` | `src/FE/src/styles.scss` |
| dialog-width-confirm | `min(420px, 92vw)` | `dialog.confirm-dialog` | `src/FE/src/styles.scss` |
| input-icon-inset | `left: 12px` (`10px` in a toolbar) | `.input-icon > .pi` | `src/FE/src/styles.scss` |
| input-icon-button-inset | `right: 10px` | `.input-icon .icon-btn` | `src/FE/src/styles.scss` |

### Shipped as literals — component SCSS

Read 2026-08-29, re-read 2026-09-06; cited by file plus selector.

| Name | Value | Live variable / selector | Source file |
| --- | --- | --- | --- |
| shell-height | `100vh` with a `100dvh` override | `.shell`, `.shell-content`, `.sidebar`, `.login-shell` | `src/FE/src/app/app.scss`, `sidebar.scss`, `auth-card.scss` |
| sidebar-w-drawer-tablet | `min(85vw, 300px)` (≤980px, off-canvas) | `.sidebar` | `src/FE/src/app/shared/components/sidebar/sidebar.scss` |
| sidebar-w-drawer-mobile | `min(90vw, 300px)` (≤560px) | `.sidebar` | `src/FE/src/app/shared/components/sidebar/sidebar.scss` |
| sidebar-brand-height | `min-height: 50px` | `.sidebar-brand` | `src/FE/src/app/shared/components/sidebar/sidebar.scss` |
| sidebar-navitem-height-mobile | `min-height: 40px` (≤560px) | `.sidebar-navitem` | `src/FE/src/app/shared/components/sidebar/sidebar.scss` |
| sidebar-flyout-width | `min-width: 180px` | collapsed-sidebar submenu | `src/FE/src/app/shared/components/sidebar/sidebar.scss` |
| nav-icon | `18px × 18px` | `.navicon` | `src/FE/src/app/shared/components/sidebar/sidebar.scss` |
| active-nav-rail | `3px` wide, inset `5px` top/bottom, offset `-8px` left | `.sidebar-navitem.active::before` | `src/FE/src/app/shared/components/sidebar/sidebar.scss` |
| brand-mark-sidebar | `26px × 26px` | `.sidebar-brand .brand-mark` | `src/FE/src/app/shared/components/sidebar/sidebar.scss` |
| brand-mark-auth | `44px × 44px` | `.login-brand .brand-mark` | `src/FE/src/app/shared/components/auth-card/auth-card.scss` |
| login-card-max-width | `380px` | `.login-card` | `src/FE/src/app/shared/components/auth-card/auth-card.scss` |
| toast-stack-max-width | `min(400px, 90vw)` | `.toast-stack` | `src/FE/src/app/shared/components/toast/toast.scss` |
| toast-icon | `22px × 22px`, `--radius-pill`, glyph `11px` | `.toast-icon` — the severity disc; the smaller sibling of the 40px `.dialog-icon` | `src/FE/src/app/shared/components/toast/toast.scss` |
| toast-accent-bar | `border-left: 5px` | `.toast-item` — the severity stripe; `.notice` uses `4px` for the same idea | `src/FE/src/app/shared/components/toast/toast.scss` |
| avatar | `30px × 30px` | `.avatar` | `src/FE/src/app/platform/quan-tri-nguoi-dung/components/user-grid-table/user-grid-table.scss` |
| grid-empty padding | `var(--sp-6) var(--sp-4)` — ⚠️ `--sp-6` **is not declared anywhere**, so the vertical padding resolves to `0`; see § Normalize #7 | `.grid-empty` — the default "no data" row | `src/FE/src/app/shared/components/data-grid/data-grid.scss` |
| user-grid column min-widths | five inline `th` `min-width`s — `220` / `140` / `120` / `110` / `100px` | `#hdr` template | `src/FE/src/app/platform/quan-tri-nguoi-dung/components/user-grid-table/user-grid-table.html` |
| data-grid-min-width | unset on the one shipped grid → the table falls back to `min-width: auto` | `--data-grid-min-width`, bound from `<app-data-grid [minWidth]>`; scoped, **not** a `:root` token | `src/FE/src/app/shared/components/data-grid/data-grid.scss` |

> 🔄 **SỬA 2026-09-06 — the `690px` figure was a sum this document performed, not a value the app applies.** The five inline `th` `min-width`s are real and still add to 690, but nothing multiplies them into a table-level minimum: that job moved to `--data-grid-min-width`, and `user-grid-table.html` does not pass `[minWidth]`, so `.tablewrap :is(table)` resolves to the `auto` fallback. Recording 690px as a shipped measurement overstated what the browser is told.

### Grid templates

| Name | Value | Live variable / selector | Declared at |
| --- | --- | --- | --- |
| form-grid | `1fr 1fr`, gap `0 var(--sp-5)`; one column ≤560px | `.form-grid` | `src/FE/src/styles.scss` |
| page-fill chain | `flex: 1 1 auto` + `min-height: 0` at every link, so the grid takes the leftover viewport height | `.page-fill`, `.page-fill > .card`, `.page-fill > .card > *`, `.page-fill .grid-host`, `.page-fill .tablewrap.scroll`; the chain continues **inside** the grid component via `:host(.grid-host) { .tablewrap, p-table }` | `src/FE/src/styles.scss`, and `src/FE/src/app/shared/components/data-grid/data-grid.scss` |

### Z-index layers (no token declared)

| Value | Layer | Declared at |
| --- | --- | --- |
| `4` | sticky `th` | `src/FE/src/styles.scss` |
| `20` | `.topbar` | `src/FE/src/app/shared/components/topbar/topbar.scss` |
| `34` | `.sidebar-backdrop` | `src/FE/src/app/shared/components/sidebar/sidebar.scss` |
| `35` | `.sidebar` | `src/FE/src/app/shared/components/sidebar/sidebar.scss` |
| `40` | collapsed-sidebar flyout submenu | `src/FE/src/app/shared/components/sidebar/sidebar.scss` |
| `50` | `.filter-panel` (toolbar filter dropdown) | `src/FE/src/styles.scss` |
| `50` | `.app-update` — the "a new version is available" banner pinned to the top of the viewport | `src/FE/src/app/app.scss` |
| `60` | `.toast-stack` | `src/FE/src/app/shared/components/toast/toast.scss` |
| `60` | `.skip-link` — deliberately the joint-highest, because it is only visible while focused | `src/FE/src/app/app.scss` |

Native `<dialog>` elements sit on the browser's top layer and are not part of this stack.

> 🔄 **SỬA 2026-09-06 — two layers were missing.** The previous revision listed seven values and omitted both rules declared in `app.scss` (`.skip-link` at 60, `.app-update` at 50), which is also why its Normalize item said "seven hand-picked z-index values". Re-tally with `grep -rn 'z-index' src/FE/src --include=*.scss` rather than trusting the row count.

## Token Table — Elevation (Shadow)

| Name | Value | Live variable / selector | Declared at |
| --- | --- | --- | --- |
| shadow | `0 4px 16px rgba(23,39,67,.1), 0 1px 3px rgba(23,39,67,.06)` | `--shadow` — `.card`, `.login-card`, sidebar drawer + flyout | `src/FE/src/styles.scss` § `--shadow` |
| shadow-toast | `0 14px 38px rgba(23,39,67,0.26), 0 2px 6px rgba(23,39,67,0.12)` | `--shadow-toast` — `.toast-item`, deeper than `--shadow` on purpose | `src/FE/src/styles.scss` § `--shadow-toast` (promoted 2026-09-03, gate G11) |
| shadow-primary-hover | `0 8px 20px rgba(15,91,215,0.35)` | `.btn.primary:hover` | `src/FE/src/styles.scss` |
| shadow-btn-hover | `0 3px 10px rgba(23,39,67,0.1)` | `.btn:hover` | `src/FE/src/styles.scss` |
| shadow-panel | `0 16px 40px rgba(23,39,67,0.22)` | `.filter-panel` | `src/FE/src/styles.scss` |
| shadow-dialog | `0 24px 70px rgba(0,0,0,0.25)` | `dialog` | `src/FE/src/styles.scss` |
| shadow-focus-ring | `0 0 0 3px rgba(15,91,215,0.12)` | every input `:focus-visible` | `src/FE/src/styles.scss` |
| shadow-focus-ring-invalid | `0 0 0 3px rgba(160,43,43,0.14)` | `.input.invalid:focus-visible` | `src/FE/src/styles.scss` |

Full color values for the shadow tokens are cross-referenced in `Tokens/colors.md`.

### Motion (no token declared)

| Value | Where | Declared at |
| --- | --- | --- |
| `0.1s ease` | `.btn` transform | `src/FE/src/styles.scss` |
| `0.15s ease` | `.btn` background/shadow, `.icon-btn`, `.seg-btn`, input border/shadow, sidebar chevron | `src/FE/src/styles.scss`, plus `sidebar.scss` |
| `0.18s cubic-bezier(.2,.9,.3,1)` | `toast-in` entry animation | `src/FE/src/app/shared/components/toast/toast.scss` |
| `0.2s ease` | `.shell-content` margin, `.sidebar` width, sidebar toggle icon rotation | `src/FE/src/app/app.scss`, `src/FE/src/app/shared/components/sidebar/sidebar.scss` |
| `0.25s ease` | `.sidebar` transform (drawer slide) | `src/FE/src/app/shared/components/sidebar/sidebar.scss` |

A global `prefers-reduced-motion: reduce` guard **does** ship — `src/FE/src/styles.scss` § `@media (prefers-reduced-motion: reduce)` collapses every animation and transition to `.01ms` with `!important`, deliberately beating the per-component `transition` declarations above. A previous revision of this line said no guard existed; that was true of an earlier stylesheet and is not true of this one (re-read 2026-08-29).

## Chart Palette

<!-- N/A for spacing — no chart element carries a spacing token. Tokens/colors.md is the owner of the
     chart roles.

     SUA 2026-09-06: the previous comment said colors.md "records that the app ships no chart". That
     was true between 2026-08-29 and 2026-09-05 only. Decision Q17 restored four chart roles to
     colors.md on 2026-09-05, so this file was pointing at a sentence that no longer exists. -->

## Drift — 2026-08-22 extraction → 2026-08-29 rewrite

**Scale values: no change.** `--sp-1…5` (4/6/8/10/14px), `--radius-*` (7/9/16/15/12/999px), `--sidebar-w` (220px), `--sidebar-w-collapsed` (60px) and `--container-max-width` (1600px) were all re-read and all match the previous revision.

**Added to `:root`:** `--grid-h`, `--grid-h-min` — see § Structural Measurements.

**Added as literals:** the whole toolbar / filter family (`.toolbar-sep`, `.filter-count`, `.filter-panel`, `.filter-chip`), the six `.input.w-*` width classes, `.dialog-icon`, `.textarea` min-height, and z-index `50`.

**Removed (the selector no longer exists):** every measurement that belonged to the dashboard or the DTI catalogue — `chart-height` (220px), `progress-bar-height` (9px), `kpi-sub-min-height` (30px), `history-max-height` (240px), `criteria-grid-min-width` (1430px), `progress-input-width` (74px), `note-input-min-width` (130px), `filter-input-min-width` on the old `.filters` row, and the `kpis-grid-*` / `layout-grid-*` / `group-row-grid-*` / `histrow-grid` templates. `.form-grid` and the new `.page-fill` chain are the only grid templates left.

**Resolved since the previous revision:** the `.dialog-actions` duplication it flagged is now one shared rule in `styles.scss` with `--sp-5` margin-top, and the local copy in `user-form-dialog.scss` was deleted on 2026-08-29. The three hard-coded grid heights it did not yet know about became `--grid-h`.

## Resolved — no longer open

Three items moved here from § Normalize on redesign because the source changed. Re-checked 2026-08-29.

1. ~~**The permission matrices still hard-code `max-height: 560px`**~~ — **fixed in the source 2026-08-29.** Both matrices now use `.tablewrap.scroll`, so their height comes from `--grid-h`; `grep -rn '560px' src/FE/src/app/platform/phan-quyen/` returns only the two comments recording the swap.
2. ~~**`user-form-dialog.scss` re-declares `.dialog-actions`** with a hand-typed `gap: 8px` and `margin-top: 8px`~~ — **fixed in the source 2026-08-29.** The local copy was deleted; the file now holds only `.role-checkboxes` / `.role-checkbox`, and its header records what was removed and why.
3. ~~**`6px` appears as a literal in component SCSS while `--sp-2` *is* `6px`.**~~ — **fixed in the source 2026-08-29** for the two call sites this file listed: `.row-actions` and `.role-checkbox` both use `var(--sp-2)`. The genuinely off-scale values below are still open.

## Normalize on redesign
<!-- 2026-09-08: this file carried TWO `## Normalize on redesign` headings — an empty one
     directly above § Resolved, and this one. A reader who stopped at the empty heading
     concluded the section had been cleared. The empty duplicate was removed; this is the
     only Normalize section, and § Resolved above it holds the closed items. -->

1. **`--grid-h` is bypassed on every `.page-fill` page.** The token exists and is immediately overridden by the flex chain that supersedes it. Either scope it to the non-`page-fill` case in its own name, or drop it once every list page uses `.page-fill` — an override that always fires is a value nobody can reason about from the token table alone.
2. **The `--sp-*` scale is still bypassed by genuinely off-scale values** — `11px`, `20px`, `24px`, `28px`, `32px 28px`, `38px` — which need either a scale extension or rounding.
3. **No `--z-*` scale.** Hand-picked z-index values with no documented ordering — `34`/`35` in particular only make sense if you read both declarations, and two of the values are now used by two different layers each (`50`, `60`). Count them with the `grep` in § Z-index layers; the previous revision wrote "seven" and had already missed two.
4. **No motion tokens.** Five durations across the app, all literals. The `prefers-reduced-motion` guard that a previous revision of this item said was missing **does** ship (`src/FE/src/styles.scss` § `@media (prefers-reduced-motion: reduce)`); what is missing is the named scale, not the guard.
5. **`--radius-sm` (`7px`) is re-typed as a literal in `sidebar.scss`** (on `.brand-mark`), and `--radius-table` (`12px`) as a literal in `auth-card.scss`. 🔄 **SỬA 2026-09-06:** this item said "twice in `sidebar.scss`"; the second call site was `.sidebar-toggle`, whose shape moved to the global `.icon-btn` on 2026-08-29, so one is left.
6. **A `var(--sp-6)` reference exists with no `--sp-6` token behind it.** `.grid-empty` in `src/FE/src/app/shared/components/data-grid/data-grid.scss` asks for `padding: var(--sp-6) var(--sp-4)`, and the scale stops at `--sp-5`. With no fallback given, the vertical padding computes to `0`, so the default empty-grid row sits flush against the header rule. Two ways out, and they are a real design choice rather than a typo fix: extend the scale with a sixth step (the natural next value on this non-linear ramp is around `18–20px`), or point the rule at `--sp-5`. Recorded here rather than silently corrected because `src/` is out of scope for this area, and because the answer decides whether the scale grows. Found 2026-09-06; `grep -rn 'sp-6' src/FE/src` returns this one call site and no declaration.
7. **`container-max-width: 1600px` alongside `--sidebar-w: 220px`** means the content column can reach 1600px on top of a 220px rail — on a 1920px display the shell fills nearly edge to edge. Worth re-checking against the 1440px viewport the screenshots are captured at.

## Appendix: tokens.json rules

- Format: W3C DTCG — every token is an object with `$type` and `$value`.
- Top-level sets: `global` (theme-invariant) plus `light` and `dark` (theme overrides only). Spacing/radius/breakpoint/dimension tokens are theme-invariant and live entirely in `global`.
- Figma import via Tokens Studio: enable `global` + exactly ONE theme set at a time — never both themes together.

---
project: "PlatformManager"
status: "draft"
updated: "2026-08-22"
flow: "DTI Catalogue"
screens: ["DTI Catalogue"]
source_routes: ["/danh-muc/dti"]
---

# DTI Catalogue — Screens

The DTI Catalogue is the app's single data-entry surface: one card, one 12-column grid, where a digital-transformation officer maintains the `Criteria` catalogue (create/edit/delete), imports a whole period from CSV/Excel, and edits the two per-period fields — Tiến độ % and Ghi chú — inline in the grid. The Dashboard (`Screens/01-dashboard.md`) is read-only by design; every write in the product happens here. Four native `<dialog>` overlays (criteria form, delete confirm, import picker, import result) belong to this route rather than to routes of their own. Server-side filtering/paging is driven by four filter controls (search, group, year, period) and a read-only rule: the grid only offers write controls while the user is viewing "Tất cả (mới nhất trong năm)" of the **current** year. This spec cross-references `spec/danh-muc-dti/ui-spec.md` (§3 inline edit, §4 Layout, §5 columns, §6 actions, §6.9 year/period filters, §6.10 read-only rule, §6.11 grid card sizing) and `spec/danh-muc-dti/business-rules.md` (§1.1–1.3 CRUD + delete branches, §2.2 import mapping, §2.4 read-only) alongside the shipped Angular source.

> **Shell:** app shell — `Sidebar` + `Topbar` + `main` + `Toast` (`app.html:1-14`), rendered because this route does not set `data.noShell`. **`DESIGN.md` → Layout describes this shell correctly** as of the 2026-08-22 token refresh: it documents both of the app's shells (`DESIGN.md:418-422`) — the main one, `Sidebar` at `--sidebar-w` 220px / `--sidebar-w-collapsed` 60px with `.shell-content` offset to match, a sticky `Topbar` and a `main` capped at `--container-max-width`, which is the one this route renders — alongside the `noShell` auth shell, and all three breakpoints (980 / 560 / 981px).
> **Token vocabulary:** token names below are the live CSS custom properties declared in `styles.scss:10-79` (`--sp-*`, `--fs-*`, `--radius-*`, `--brand`, `--tonal-bg`, `--sidebar-w`, `--container-max-width`, …). `Tokens/*.md` + `Tokens/tokens.json` were **re-extracted from this app on 2026-08-22** and now match it 1:1 — the earlier warning here, that they still carried prototype-era names and values, no longer applies. Two token values changed again later that same day (`--warn` → `#965e08`, `--bad` → `#a02b2b`, to clear WCAG AA); `styles.scss` remains the tiebreaker if any doc disagrees.
> **Sources:** `src/FE/src/app/modules/danh-muc-dti/` (`danh-muc-dti.routes.ts`, `pages/danh-muc-dti/danh-muc-dti.page.{html,ts,scss}`, `components/{criteria-grid-table,criteria-form-dialog,confirm-dialog,import-dialog,import-result-dialog}/`, `services/danh-muc-dti.service.ts`, `models/danh-muc-dti.model.ts`), `src/FE/src/app/{app.html,app.scss,app.ts,app.routes.ts}`, `src/FE/src/app/shared/components/{sidebar,topbar,toast}/`, `src/FE/src/app/shared/services/period-options.service.ts`, `src/FE/src/app/core/interceptors/http-error.interceptor.ts`, `src/FE/src/app/shared/services/toast.service.ts`, `src/FE/src/styles.scss`, `spec/danh-muc-dti/ui-spec.md`, `spec/danh-muc-dti/business-rules.md`. Design-intent reference only (**not** as-shipped): the deleted prototype. File:line citations below use the bare filename of these paths.

---

## DTI Catalogue (`/danh-muc/dti`)

### Layout Blueprint

<!-- Region tree + structural measurements. Compose ONLY component names present in COMPONENTS.md. -->

- **App shell** (`app.html:1-14`) — `showShell()` is true for this route (no `data.noShell`)
  - `Sidebar` (`sidebar.html`, `Components/Sidebar.md`) — fixed left, `--sidebar-w` (220px) / `--sidebar-w-collapsed` (60px), `z-index:35`, `background --card`, right border `--line`
    - `.sidebar-brand`: `.brand-mark` "PM" square (26×26, `--brand` fill, `--on-primary` text) + `.brand-text` "PlatformManager" + `.sidebar-toggle` icon button
    - `nav > ul.sidebar-nav`: items from `GET /api/meta/menu` (server-driven — see Copy); the "Danh mục" group is expanded with its "DTI" child in the `routerLinkActive="active"` state on this route
    - `.sidebar-backdrop` — drawer dismiss overlay, `display:none` above `breakpoint.tablet`
  - `.shell-content` (`app.scss:11-22`) — `margin-left: var(--sidebar-w)`, flex column
    - `Topbar` (`topbar.html`, `Components/Topbar.md`) — sticky top, `z-index:20`, translucent white + `backdrop-filter: blur(10px)`, bottom border `--line`; inner `.topin` max-width `--container-max-width` (1600px), padding `--sp-4 --sp-5`
      - `Button` (secondary, `.btn.sidebar-hamburger.no-print`) — icon-only, hidden above `breakpoint.tablet`
      - `.logo h1` — route title text "Danh mục" (`--fs-lg`)
      - `.topbar-user` (`margin-left:auto`): user name text + `Button` (secondary) "Đăng xuất"
    - `main` (`app.scss:24-30`) — `max-width: var(--container-max-width)`, centered, padding `--sp-5`
      - `<router-outlet>` → **DanhMucDtiPage**
  - `Toast` (`toast.html`, `Components/Toast.md`) — `.toast-stack.no-print` fixed bottom-right (`--sp-5` inset), `z-index:60`, `max-width: min(360px, 90vw)`, `aria-live="polite"`; each `.toast-item` is a `Card`-like surface with a 4px left accent border colored by severity (`--good` / `--bad` / `--warn` / `--brand`) plus an icon-only close button

- **Page** (`danh-muc-dti.page.html`) — the entire screen is **one** `Card`
  - `Card` (`.card.dti-grid-card`, `danh-muc-dti.page.scss:1-5`) — `display:flex; flex-direction:column; min-height:0`; padding `--sp-5`, radius `--radius-lg`, shadow `--shadow`
    - `.title` row (`styles.scss:272-283`, `justify-content:space-between`, `margin-bottom --sp-4`)
      - `h2` "Danh mục & Đánh giá theo tuần" (`--fs-md`)
      - `.muted` record-count text `{{ totalCount() }} bản ghi` (`--fs-xs`, `--muted`)
    - `NoticeBanner` (`.notice`) — **conditional**, rendered only when `!isLive()` (`danh-muc-dti.page.html:7-11`)
    - `.filters` row (`.no-print`, `styles.scss:325-336`) — flex, `gap --sp-3`, `flex-wrap:wrap`, `margin --sp-4 0`
      - `Input` (search variant, filter tier) — `flex:1; min-width:220px`, debounced 300 ms
      - `Input` (select variant, filter tier) — group filter, options from `GET /api/criteria-groups`
      - `Input` (select variant, filter tier) — year filter, options from `GET /api/dashboard/periods` → `Years`
      - `Input` (select variant, filter tier) — period filter, options = fixed "all" + `WeeksInYear`
      - `.filters-actions` (`margin-left:auto`, `flex:none`) — **conditional**, rendered only when `isLive()`
        - `Button` (secondary) "Import CSV/Excel"
        - `Button` (primary) "+ Thêm chỉ tiêu"
    - `<app-criteria-grid-table>` → `DataTable` (`Components/DataTable.md` — the PrimeNG `p-table` grid mechanism; cell painting is the global `Table` primitive, `Components/Table.md`) (`criteria-grid-table.html:1-129`). `[scrollable]="true"`, `[lazy]="true"`, `[paginator]="true"`, `dataKey="CriteriaId"`, `styleClass="dti-grid"` (no CSS rule exists for `.dti-grid` anywhere — dead hook, see Normalize). `[scrollHeight]` is an inline px value recomputed in JS as `max(320, window.innerHeight − hostTop − 160)` on first render and on every `window resize` (`criteria-grid-table.ts:90-94`) — this is the Angular replacement for the prototype's `updateGridCardHeight()` card-height mechanism (`ui-spec.md` §6.11)
      - Header row — 12 `<th>`, combined `min-width` **1430px**: Mã 70 (frozen left) · Tên 220 · Nhóm 120 · Điểm tối đa 90 (`.num`) · Tự đánh giá 90 (`.num`) · Thẩm định 90 (`.num`) · Trạng thái 110 · Phụ trách 110 · Hạn xử lý 100 · Tiến độ % 130 (`.num`) · Ghi chú 180 · Hành động 120 (frozen right, `alignFrozen="right"`)
      - Body row — read-only text cells (cols 1–9) + the two editable cells + the action cell:
        - Tiến độ % cell: `CellIconButton` (`Components/CellIconButton.md`, which documents the whole inline-edit group) — the `.cell-editable` span (dashed `--brand` underline on hover, `role="button"`, `tabindex="0"`, activated by **double-click** or Enter) → on edit, `.cell-edit` flex with `Input` (progress-number variant, table tier, `width:74px`, spinners suppressed) + two `.cell-icon-btn` icon buttons (`.ok` ✓ `--good`, `.cancel` ✗ `--bad`)
        - Ghi chú cell: same pattern with `Input` (note-text variant, table tier, `min-width:130px`)
        - Hành động cell: two `ActionButton`s (`Components/ActionButton.md`, the global `.action-btn` in `styles.scss:211-251`) — "Sửa" and "Xoá" (`.action-btn.danger`)
      - Empty template — one `<tr><td colspan="12" class="muted">` message row
      - PrimeNG paginator (bottom of `p-table`) — page buttons + rows-per-page select `[10, 20, 50]`, default 20; part of `DataTable` (`Components/DataTable.md`), styled entirely by `PlatformManagerPreset`. No `no-print` marker
  - `<app-criteria-form-dialog>` — `Dialog` (`dialog.form-dialog`, `width: min(560px, 92vw)`)
    - `.title` row: `h2` (title swaps create/edit) + `Button` (secondary) "Đóng"
    - `.form-row` × 2 + `.form-grid` (2 equal columns) × 1 pair — each a label + `Input` (text / textarea / select / number, form tier: border `--line`, radius `--radius-sm`)
    - `.form-error` text block (`--bad`), rendered only when a message exists
    - `.dialog-actions` (`justify-content:flex-end`, gap 8px): `Button` (secondary) "Huỷ" + `Button` (primary) "Lưu chỉ tiêu"
  - `<app-confirm-dialog>` — `Dialog` (`dialog.confirm-dialog`, `width: min(420px, 92vw)`)
    - `.title` row: `h2` "Xác nhận" (no close button in this dialog)
    - `.confirm-message` paragraph (`font-size:13.5px`, `line-height:1.55` — a literal, not a token; see Normalize)
    - `.dialog-actions`: `Button` (secondary) "Huỷ" + `Button` (danger, `.btn.danger`) "Xoá"
  - `<app-import-dialog>` — `Dialog` (`.form-dialog`), lazily instantiated via `@defer (when importDialogOpen())`
    - `.title` row: `h2` "Import CSV/Excel" + `Button` (secondary) "Đóng"
    - `.form-row`: label + `Input` (native `<input type="file">`, `accept=".csv,.xlsx,.xls"`)
    - `.muted` chosen-file line, rendered only after a file is picked
    - `.dialog-actions`: `Button` (secondary) "Huỷ" + `Button` (primary, disable-able) with swapping label
  - `<app-import-result-dialog>` — `Dialog` (`.form-dialog`), lazily instantiated via `@defer (when importResultOpen())`
    - `.title` row: `h2` "Kết quả Import" + `Button` (secondary) "Đóng"
    - `.import-summary`: one summary `<p>` with `.ok`/`.err` colored counts + a `<ul>` of per-row errors (rendered only when `Errors.length > 0`)
    - `.dialog-actions`: `Button` (primary) "Đóng"

### Copy

<!-- Verbatim shipped strings — typos and mixed languages included — with localization key and file:line source. -->

| Element | Verbatim copy | Localization key | Source |
| --- | --- | --- | --- |
| Topbar page title | `Danh mục` | — (hardcoded, route `data.title`) | `danh-muc-dti.routes.ts:10` |
| Topbar hamburger `aria-label` | `Mở menu điều hướng` | — (hardcoded) | `topbar.html:6` |
| Topbar logout button (`title` + label) | `Đăng xuất` | — (hardcoded) | `topbar.html:18-19` |
| Sidebar brand mark / brand text | `PM` / `PlatformManager` | — (hardcoded) | `sidebar.html:3-4` |
| Sidebar collapse toggle `aria-label` (2 values) | `Mở rộng menu` / `Thu gọn menu` | — (hardcoded) | `sidebar.html:8` |
| Sidebar nav labels ("Danh mục", "DTI", …) | *(not in the view)* | — (server-driven, `GET /api/meta/menu` → `SysMenu.Label`) | `sidebar.html:32,46,60`; `menu.service.ts:130` |
| Toast close `aria-label` | `Đóng thông báo` | — (hardcoded) | `toast.html:8` |
| Card title | `Danh mục & Đánh giá theo tuần` (source writes `&amp;`) | — (hardcoded) | `danh-muc-dti.page.html:3` |
| Card record count (dynamic) | `<n> bản ghi` | — (hardcoded, interpolation `{{ totalCount() }} bản ghi`) | `danh-muc-dti.page.html:4` |
| Read-only notice banner | `Đang xem dữ liệu lịch sử — chỉ đọc. Quay lại "Tất cả (mới nhất trong năm)" của năm hiện tại để chỉnh sửa.` | — (hardcoded) | `danh-muc-dti.page.html:9` |
| Search placeholder | `Tìm mã hoặc tên chỉ tiêu...` | — (hardcoded) | `danh-muc-dti.page.html:14` |
| Group filter, default option | `Tất cả nhóm` | — (hardcoded) | `danh-muc-dti.page.html:16` |
| Group filter, option template (dynamic) | `<Code>. <Name>` | — (server data, `GET /api/criteria-groups`) | `danh-muc-dti.page.html:18` |
| Year filter `title` | `Chọn năm` | — (hardcoded) | `danh-muc-dti.page.html:21` |
| Period filter `title` | `Xem tổng hợp cả năm (mới nhất mỗi chỉ tiêu) hoặc 1 kỳ cụ thể đã lưu trong năm` | — (hardcoded) | `danh-muc-dti.page.html:28` |
| Period filter, default option | `Tất cả (mới nhất trong năm)` | — (hardcoded) | `danh-muc-dti.page.html:31` |
| Period filter, saved-period options (dynamic) | `<Value>` — ISO week/month key, e.g. `2026-W34` | — (server data, `GET /api/dashboard/periods`) | `danh-muc-dti.page.html:33` |
| Import button | `Import CSV/Excel` | — (hardcoded) | `danh-muc-dti.page.html:38` |
| Add-criteria primary button | `+ Thêm chỉ tiêu` | — (hardcoded) | `danh-muc-dti.page.html:39` |
| Grid column headers (12) | `Mã` / `Tên` / `Nhóm` / `Điểm tối đa` / `Tự đánh giá` / `Thẩm định` / `Trạng thái` / `Phụ trách` / `Hạn xử lý` / `Tiến độ %` / `Ghi chú` / `Hành động` | — (hardcoded) | `criteria-grid-table.html:18-29` |
| Nhóm cell template (dynamic) | `<GroupCode>. <GroupName>` | — (server data) | `criteria-grid-table.html:37` |
| Null-value placeholder (Trạng thái / Phụ trách / read-only Ghi chú) | `—` | — (hardcoded) | `criteria-grid-table.html:41,42,114` |
| Null-value placeholder (Điểm tối đa / Tự đánh giá / Thẩm định / Tiến độ % / Hạn xử lý) | `—` | — (hardcoded, `formatScore` / `formatPercent` / `formatDateVn`) | `criteria-grid-table.ts:25,29,34` |
| Number/percent/date formats | `vi-VN` locale — scores 2 decimals, progress 1 decimal + `%`, dates `dd/mm/yyyy` | — (hardcoded) | `criteria-grid-table.ts:24-36` |
| Inline-edit affordance `title` (2 values) | `Bấm đúp để sửa Tiến độ %` / `Bấm đúp để sửa Ghi chú` | — (hardcoded) | `criteria-grid-table.html:71,106` |
| Empty-note affordance text | `— bấm đúp để ghi chú` | — (hardcoded) | `criteria-grid-table.html:110` |
| Note input placeholder | `Nội dung đã làm / vướng mắc...` | — (hardcoded) | `criteria-grid-table.html:90` |
| Inline-edit confirm/cancel `title` | `Lưu` / `Huỷ` | — (hardcoded) | `criteria-grid-table.html:59,62,94,97` |
| Row action buttons | `Sửa` / `Xoá` | — (hardcoded) | `criteria-grid-table.html:118-119` |
| Grid empty message | `Không có chỉ tiêu nào khớp bộ lọc.` | — (hardcoded) | `criteria-grid-table.html:126` |
| Criteria form dialog title (2 values) | `Sửa chỉ tiêu` / `Thêm chỉ tiêu` | — (hardcoded) | `criteria-form-dialog.ts:46` |
| Criteria form field labels | `Mã` / `Tên chỉ tiêu` / `Nhóm` / `Điểm tối đa`, each followed by a `*` in `--bad` | — (hardcoded) | `criteria-form-dialog.html:8,13,19,27` |
| Criteria form placeholders | `vd 1.1` / `Nhập tên đầy đủ chỉ tiêu...` / `vd 10` | — (hardcoded) | `criteria-form-dialog.html:9,14,28` |
| Criteria form buttons | `Đóng` / `Huỷ` / `Lưu chỉ tiêu` | — (hardcoded) | `criteria-form-dialog.html:4,37,39` |
| Criteria form client validation (4 messages) | `Mã bắt buộc, tối đa 20 ký tự.` / `Tên chỉ tiêu bắt buộc.` / `Vui lòng chọn nhóm.` / `Điểm tối đa phải lớn hơn 0.` | — (hardcoded) | `criteria-form-dialog.ts:79,83,87,91` |
| Criteria save fallback error (when the API envelope carries no `message`) | `Không lưu được chỉ tiêu — thử lại sau.` | — (hardcoded) | `danh-muc-dti.page.ts:223` |
| Criteria save success toasts (2 values) | `Đã cập nhật chỉ tiêu.` / `Đã thêm chỉ tiêu.` | — (hardcoded) | `danh-muc-dti.page.ts:220` |
| Confirm dialog title / confirm button | `Xác nhận` / `Xoá` | — (hardcoded, passed as inputs from the page) | `danh-muc-dti.page.html:69,71` |
| Confirm dialog cancel button | `Huỷ` | — (hardcoded) | `confirm-dialog.html:7` |
| Delete confirm message — criterion **has** assessment data (dynamic) | `Chỉ tiêu "<Code>" đã có dữ liệu đánh giá — sẽ ẩn khỏi danh mục (soft-delete), lịch sử vẫn được giữ nguyên. Tiếp tục?` | — (hardcoded, JS template) | `danh-muc-dti.page.ts:237` |
| Delete confirm message — criterion **has no** assessment data (dynamic) | `Xoá hẳn chỉ tiêu "<Code>"? Chỉ tiêu này chưa có dữ liệu đánh giá nào nên sẽ bị xoá vĩnh viễn.` | — (hardcoded, JS template) | `danh-muc-dti.page.ts:238` |
| Delete success toast | `Đã xoá chỉ tiêu.` | — (hardcoded) | `danh-muc-dti.page.ts:250` |
| Inline-edit failure toasts (2 values) | `Không lưu được Tiến độ % — thử lại sau.` / `Không lưu được Ghi chú — thử lại sau.` | — (hardcoded) | `danh-muc-dti.page.ts:271,285` |
| Import dialog title | `Import CSV/Excel` | — (hardcoded) | `csv-import-dialog.html:3` |
| Import dialog file label | `Chọn file CSV/Excel` | — (hardcoded) | `csv-import-dialog.html:8` |
| Import dialog chosen-file line (dynamic) | `Đã chọn: <filename>` | — (hardcoded) | `csv-import-dialog.html:13` |
| Import dialog buttons | `Đóng` / `Huỷ` | — (hardcoded) | `csv-import-dialog.html:4,17` |
| Import dialog primary button (2 values) | `Đang nhập…` / `Nhập dữ liệu` | — (hardcoded) | `csv-import-dialog.html:19` |
| Import failure toast fallback (job `Failed` with no `ErrorMessage`) | `Import thất bại — thử lại sau.` | — (hardcoded) | `danh-muc-dti.page.ts:328` |
| Import result dialog title / buttons | `Kết quả Import` / `Đóng` (×2) | — (hardcoded) | `import-result-dialog.html:3,4,33` |
| Import result summary (dynamic) | `Tổng <n> dòng — <n> thành công, <n> lỗi.` | — (hardcoded) | `import-result-dialog.html:10-11` |
| Import result auto-create line (dynamic, only when > 0) | `Đã tự tạo mới <n> chỉ tiêu.` | — (hardcoded) | `import-result-dialog.html:13` |
| Import result per-row error (dynamic) | `Dòng <n> — mã "<code>" : <message>` — the space before the colon is real (template newlines collapse to a single space; `preserveWhitespaces` is not enabled) | — (hardcoded) | `import-result-dialog.html:20-24` |
| HTTP error toasts (shared interceptor fallbacks, 6 values) | `Không thể kết nối tới máy chủ. Kiểm tra kết nối mạng.` / `Bạn cần đăng nhập để tiếp tục.` / `Bạn không có quyền thực hiện thao tác này.` / `Không tìm thấy dữ liệu yêu cầu.` / `Bạn thao tác quá nhanh. Vui lòng chờ một lát rồi thử lại.` / `Đã có lỗi xảy ra. Vui lòng thử lại.` | — (hardcoded) | `http-error.interceptor.ts:18-36` |

### States

<!-- How each state renders: default / loading / empty / error / validation display. -->

- **loading:** `loading()` flips true at the top of the list `effect` and false in both `next` and `error` (`danh-muc-dti.page.ts:116-127`). It is passed straight to `p-table [loading]`, so PrimeNG renders its own mask + spinner over the grid body — there is **no skeleton and no app-authored spinner**. Rows already on screen stay visible under the mask. Fires on first paint and on **every** filter/paging change (year, period, group, debounced search, page, page size), because `requestParams` is a `computed` the effect depends on. `reload()` after a create/update/delete/import (`danh-muc-dti.page.ts:130-143`) deliberately does **not** touch `loading()` — post-mutation refetches show no loading affordance at all.
- **populated — live/editable (default on open):** `isLive()` is true when `selectedYear() === CURRENT_YEAR` **and** `selectedPeriod() === 'all'`, which are the initial signal values (`danh-muc-dti.page.ts:51-52,78`). No notice banner; `.filters-actions` ("Import CSV/Excel" + "+ Thêm chỉ tiêu") is present; each row's Tiến độ %/Ghi chú cells render as `.cell-editable` spans when the server sends `row.IsEditable === true`. `CURRENT_YEAR` is captured once at module evaluation (`danh-muc-dti.page.ts:25`), so a session left open across New Year keeps the stale year.
- **populated — read-only/historical:** any other year, or any specific period inside the current year, makes `isLive()` false → the `NoticeBanner` renders and `.filters-actions` is removed from the DOM entirely (`@if`, not `display:none`). Per-row inline editability is independent of this and comes from `row.IsEditable` off the API (`business-rules.md` §2.4, CONTRACT DM-6); a non-editable row renders its Tiến độ %/Ghi chú as plain text with no click affordance. **The Hành động column is gated the same way** (`criteria-grid-table.html:128-133`, fixed 2026-08-22): `@if (row.IsEditable)` renders "Sửa"/"Xoá", `@else` renders `<span class="muted">—</span>`.

  > Two mechanisms, not one. The **toolbar** ("Import CSV/Excel", "+ Thêm chỉ tiêu") gates on the page-level `isLive()` signal; the **two editable cells and the action cell** gate on the per-row server flag `row.IsEditable`. They coincide in practice — the API marks rows non-editable for exactly the periods that make `isLive()` false — but they are separate inputs, and a generator reproducing this screen should not collapse them into one condition.
- **empty:** `#emptymessage` renders exactly one row — `<tr><td colspan="12" class="muted">Không có chỉ tiêu nào khớp bộ lọc.</td></tr>` (`criteria-grid-table.html:124-128`). This is the **only** empty state: a genuinely empty catalogue, a filter with no matches, and a failed first load all render the same message. The record-count text still reads `0 bản ghi`, and the paginator still renders.
- **error — list/lookup fetch:** every failed request produces an error `Toast` from the shared interceptor (`http-error.interceptor.ts:82`) using the API envelope's `message` when present, else the status-based fallback. Beyond the toast: `getList` error only clears `loading()` and leaves `rows()` untouched (`danh-muc-dti.page.ts:125`) — first-load failure therefore shows the empty message, and a post-mutation `reload()` failure silently keeps stale rows on screen (`danh-muc-dti.page.ts:139-142`); `getGroups` error sets `groups` to `[]`, so the group filter shows only "Tất cả nhóm" (`danh-muc-dti.page.ts:103-106`); `getPeriodOptions` error sets `periodOptions` to `null`, so the year select falls back to a single option (the currently selected year) and the period select shows only "Tất cả (mới nhất trong năm)" (`danh-muc-dti.page.ts:108-114`, `danh-muc-dti.page.html:22,32`).
- **error — session expired (401 mid-session):** the interceptor clears the user context and navigates to `/dang-nhap?returnUrl=/danh-muc/dti` on top of the toast (`http-error.interceptor.ts:52-58,83`) — this screen has no 401 handling of its own.
- **inline edit — editing:** entered by **double-click** on a `.cell-editable` span, or `Enter` while it is keyboard-focused (`criteria-grid-table.html:72-73,107-108`). Exactly one cell across the grid can be in edit mode (`editingCell` signal, `criteria-grid-table.ts:73,96-104`). The cell swaps to `.cell-edit`: an autofocused `Input` (`appAutofocus`) plus ✓/✗ `.cell-icon-btn`s. `Enter` confirms, `Escape` cancels (`criteria-grid-table.ts:122-130`). Progress input is `type=number min=0 max=100 step=0.1` with native spinners suppressed.
- **inline edit — commit:** `confirmEdit` clamps progress to `[0,100]` via `Math.max(0, Math.min(100, Number(raw) || 0))` (`criteria-grid-table.ts:113-115`) — **silently**: out-of-range or non-numeric input becomes `0`/`100`/`0` with no message, no red border, no toast. The cell leaves edit mode immediately (`criteria-grid-table.ts:119`), before the `PUT` resolves; success triggers `reload()`, so the committed value only appears after the refetch lands.
- **inline edit — commit failure:** an error `Toast` ("Không lưu được Tiến độ % — thử lại sau." / "Không lưu được Ghi chú — thử lại sau.", `danh-muc-dti.page.ts:271,285`) plus the interceptor's own error toast for the same request. The cell has already closed and the typed value is gone — no retry, no dirty marker, no restore.
- **criteria form dialog:** opened via native `showModal()` from an `effect` watching `open()` (`criteria-form-dialog.ts:52-60`); `localError` is cleared on open. Create mode pre-selects the first group; edit mode pre-fills from the clicked row (`danh-muc-dti.page.ts:200-211`). Validation is **submit-time only and one message at a time** — the four checks short-circuit in order code → name → group → maxScore (`criteria-form-dialog.ts:78-93`) and the message renders in a single `.form-error` block; no field is marked `required`, no field gets an error border. A server rejection (e.g. 409 duplicate code) replaces that block with `err.apiResult?.message` while the dialog stays open (`danh-muc-dti.page.ts:222-224`); `errorMessage()` prefers the client message over the server one (`criteria-form-dialog.ts:47`). Native `ESC` and the two "Đóng"/"Huỷ" buttons all route through the `<dialog>` `close` event → `closed` output.
- **delete confirm dialog:** message text branches on `row.AssessmentId !== null` — soft-delete wording when assessment history exists, permanent-delete wording when it does not (`danh-muc-dti.page.ts:232-241`, `business-rules.md` §1.3). The confirm button emits and closes in the same expression (`confirm-dialog.html:8`), so it is never disabled or in a pending state; a failed `DELETE` just closes the dialog and leaves the interceptor toast (`danh-muc-dti.page.ts:252`).
- **import dialog — idle / file chosen / importing:** the primary button is `[disabled]="!selectedFile() || importing()"` and its label swaps to `Đang nhập…` while `importing()` (`csv-import-dialog.html:18-19`); disabled styling is `opacity:.5; cursor:not-allowed` (`styles.scss:191-194`). A `.muted` line confirms the chosen filename. The component is only instantiated once `importDialogOpen()` is true (`@defer (when …)`, `danh-muc-dti.page.html:76-83`).
- **import job polling:** `POST /import` returns `202` + `JobId` immediately; the page then polls `GET /import/{jobId}` every **1500 ms** until the status leaves `Pending`/`Running` (`danh-muc-dti.page.ts:23,301-335`). Throughout, the only visible state is the disabled "Đang nhập…" button — no progress bar, no percentage, no elapsed time, no cancel. The poll is torn down only by `takeUntilDestroyed`, i.e. by navigating away.
- **import succeeded:** `importing` false, import dialog closes, the result dialog opens with the `ICsvImportResult` payload, and the grid reloads (`danh-muc-dti.page.ts:318-324`). The result dialog renders the summary line always and the per-row error list only when `Errors.length > 0` (`import-result-dialog.html:16-28`); with zero errors the body is a single sentence.
- **import failed (job status `Failed`):** import dialog closes and an error `Toast` shows `status.ErrorMessage` or the fallback — **no result dialog**, because that component only renders a body when `result()` is non-null (`danh-muc-dti.page.ts:325-329`, `import-result-dialog.html:7`).
- **import request errored (network/4xx on the POST or any poll):** only `importing` is reset (`danh-muc-dti.page.ts:331-333`) — the import dialog **stays open** with its button re-enabled, and the interceptor toast is the sole explanation.
- **validation display (screen-wide):** there is no inline field validation anywhere on this screen. The criteria form's single `.form-error` string and the interceptor/feature toasts are the entire feedback surface; toasts auto-dismiss after **5000 ms** (`toast.service.ts:11`) and there is no persistent error region.

### Responsive

<!-- Behavior per breakpoint. -->

- **≥`breakpoint.tablet` (981px and up, desktop default):** `.shell-content` is offset by `margin-left: var(--sidebar-w)` (220px), or `--sidebar-w-collapsed` (60px) when the sidebar is collapsed (`app.scss:11-22`); the topbar hamburger is `display:none` (`topbar.scss:45-47`); a collapsed sidebar renders submenus as hover/focus-within flyouts positioned `left:100%` (`sidebar.scss:289-336`). `main` is capped at `--container-max-width` (1600px) with `--sp-5` padding.
- **<`breakpoint.tablet` (≤980px):** `.shell-content { margin-left: 0 !important }` (`app.scss:32-36`); the sidebar becomes an off-canvas drawer — `width: min(85vw, 300px)`, `transform: translateX(-100%)` until `.drawer-open`, with `--shadow` and a click-to-dismiss `.sidebar-backdrop` (`sidebar.scss:236-276`); the collapsed variant is neutralised in drawer mode (labels and brand text come back). The topbar hamburger becomes `display:flex` with `padding:9px` (`topbar.scss:49-56`). **The page itself has no rule at this breakpoint** — filters and grid are unchanged.
- **<`breakpoint.mobile` (≤560px):** `main` padding drops to `10px` (`app.scss:38-42`); `.filters-actions` loses `margin-left:auto` and takes `width:100%`, so "Import CSV/Excel" + "+ Thêm chỉ tiêu" wrap onto their own full-width row below the four filter controls (`danh-muc-dti.page.scss:14-19`) — this is the **only** page-owned media query on this screen; the topbar user name is hidden, leaving just the logout button (`topbar.scss:39-43`); the sidebar drawer widens to `min(90vw, 300px)` and nav items grow to `padding:10px; min-height:40px` (`sidebar.scss:278-287`).
- **Filters row (all viewports, no breakpoint):** `.filters` is `flex-wrap: wrap` with the search `Input` at `flex:1; min-width:220px` (`styles.scss:325-336`), so the four controls reflow intrinsically as the viewport narrows — the wrapping is driven by content width, not by a media query.
- **Grid (all viewports, no breakpoint):** `criteria-grid-table.scss` contains **no `@media` rule at all**. The 12 columns keep their `min-width` values at every size (combined **1430px**) and scroll horizontally inside the `p-table` scroll container; Mã (70px, left) and Hành động (120px, right) stay frozen via `pFrozenColumn`, leaving ~200px of scrollable columns at a 390px viewport. Vertical extent is the JS-computed `scrollHeight` — `max(320, innerHeight − hostTop − 160)` — recomputed on every `window resize` (`criteria-grid-table.ts:90-94`), which is the only viewport-reactive behaviour the grid has.
- **Print (`@media print`):** `.no-print` elements are hidden globally (`styles.scss:115-119`), which on this screen removes the `.filters` row and the toast stack, plus the sidebar, sidebar backdrop and topbar via their own print rules (`sidebar.scss:338-343`, `topbar.scss:58-62`). `.shell-content` loses its margin and `main` loses its max-width (`app.scss:44-52`). The `NoticeBanner` and the grid **do** print. There is no print rule releasing the grid's inline `scrollHeight` and the paginator is not marked `no-print` — see Normalize on redesign.

### Iconography

The shipped app loads **PrimeIcons v7** globally (`angular.json` → `styles: ["src/FE/src/styles.scss", "node_modules/primeicons/primeicons.css"]`), used as `<i class="pi pi-…">` elements. `Icons.md` was refreshed on 2026-08-22 and records this correctly — the prototype-era `library: "none"` claim this spec used to repeat is gone.

**This route renders both of the app's icon sets.** Besides the PrimeIcons glyphs written in its own templates, PrimeNG injects a second set at runtime as inline `<svg>`, with **no source line anywhere in `src/FE/`** — a grep for `pi-` misses it entirely, because these exist by switching a component on rather than by writing an icon. Two of the five reach this screen: the four paginator arrows, from `[paginator]="true"` on the grid (`criteria-grid-table.html:7`), and the loading spinner, from `[loading]` — bound by the page and forwarded into the grid (`danh-muc-dti.page.html:46` → `criteria-grid-table.html:3`), so the two binding sites produce one spinner, not two. PrimeNG's sort and filter icons do **not** render here: no template uses `pSortableColumn`, `[sortField]` or `[filters]`. Full enumeration, plus the missing `aria-hidden` and the English paginator labels, is in `Icons.md` § Per-Action Map (rows marked *PrimeNG SVG*) and § Normalize on redesign #7-9. The table below is this route's own map; every action not listed is still a plain text button.

| Action | Icon | Placement |
| --- | --- | --- |
| Confirm inline edit (Tiến độ % and Ghi chú) | `pi pi-check`, color `--good`, 12px, in a 24×24 ghost `.cell-icon-btn` | Inside the editing cell, right of the input (`criteria-grid-table.html:59-61,94-96`) |
| Cancel inline edit (Tiến độ % and Ghi chú) | `pi pi-times`, color `--bad`, 12px, in a 24×24 ghost `.cell-icon-btn` | Inside the editing cell, right of the ✓ button (`criteria-grid-table.html:62-64,97-99`) |
| Open navigation drawer | `pi pi-bars` | Topbar, leftmost, `≤breakpoint.tablet` only (`topbar.html:11`) |
| Log out | `pi pi-sign-out` (icon + "Đăng xuất" label) | Topbar, far right (`topbar.html:19`) |
| Collapse / expand sidebar | `pi pi-angle-left`, rotated 180° when collapsed | Sidebar brand row, far right (`sidebar.html:12`, `sidebar.scss:83-85`) |
| Expand / collapse a nav group | `pi pi-chevron-down`, rotated −90° when closed | Sidebar parent item, far right (`sidebar.html:33`, `sidebar.scss:167-170`) |
| Nav item glyphs | server-supplied PrimeIcons class per `SysMenu.Icon`, falling back to `pi-circle` | Sidebar, left of each label (`sidebar.html:31,45,59`; `sidebar.ts:12,37-39`) |
| Dismiss a toast | `pi pi-times` | Toast item, right edge (`toast.html:11`) |
| Paginate the grid (first / previous / next / last) | PrimeNG inline `<svg>`, **not** PrimeIcons — `data-p-icon="angle-double-left"`, `"angle-left"`, `"angle-right"`, `"angle-double-right"` | Paginator strip below the grid; injected by `p-table` because `[paginator]="true"` (`criteria-grid-table.html:7`), no `src/FE/` source line |
| Grid is loading | PrimeNG inline `<svg>` spinner, **not** PrimeIcons — `data-p-icon="spinner"` | Centred in the `p-table` loading mask while `[loading]` is true (`danh-muc-dti.page.html:46` → `criteria-grid-table.html:3`), no `src/FE/` source line |
| Search / filter / sort / import / add / edit / delete / save / cancel dialogs | — (plain text buttons and native controls, no icon) | Filters row, row actions, all four dialogs |

### Screenshots

<!-- Refs into Assets/Screenshots/danh-muc-dti/ — folder name follows the on-disk precedent set by
     Assets/Screenshots/dashboard/ (un-numbered flow stem), not the numbered spec filename. -->

**✅ The desktop shot exists — captured 2026-08-22 from the live Angular app.** That is the full target under `doc/Design/CLAUDE.md` § Rules (ONE desktop shot per screen, decided 2026-08-22). Every remaining row below is an **on-demand** state/viewport variant, *not* an outstanding debt: capture one when someone actually needs that case, and flip its status then.

**Shared prerequisites for every on-demand capture below:** start the API (`src/BE`, listening on `http://localhost:5027` per `src/FE/src/environments/environment.development.ts`; `environment.ts` ships the production-relative `/api` and does **not** name a host), then `cd src/FE && npm start` (`ng serve`, default `http://localhost:4200`). Log in at `http://localhost:4200/dang-nhap`, then navigate to `http://localhost:4200/danh-muc/dti`. The catalogue must be seeded with criteria across several groups and at least one saved period in the current year and one in a prior year.

| Screenshot path | Status | Capture instructions |
| --- | --- | --- |
| `Assets/Screenshots/danh-muc-dti/danh-muc-dti--desktop-1440.png` | captured 2026-08-22 | Live app, `/danh-muc/dti`, full page. Default live/editable state, sidebar expanded, no notice banner, `.filters-actions` visible. Captured against an **empty database**, so the grid shows its empty state while the 6 seeded criteria groups are still present in the group filter — real, not a capture failure. Environment as recorded in `UiInventory.md` § Screenshot Manifest (API on `:5027`, FE served with `npx ng serve --port 4201`). |
| `Assets/Screenshots/danh-muc-dti/danh-muc-dti--readonly--desktop-1440.png` | on demand | Same viewport; change the period filter to a specific saved week (or the year filter to a prior year) so `isLive()` is false — captures the `NoticeBanner` and the missing `.filters-actions`. |
| `Assets/Screenshots/danh-muc-dti/danh-muc-dti--inline-edit--desktop-1440.png` | on demand | Same viewport, live state; double-click a Tiến độ % cell to enter edit mode — captures the `.cell-edit` input plus the ✓/✗ icon buttons. |
| `Assets/Screenshots/danh-muc-dti/danh-muc-dti--empty--desktop-1440.png` | on demand | Same viewport; type a search term matching nothing (e.g. `zzzz`) and wait out the 300 ms debounce — captures the `Không có chỉ tiêu nào khớp bộ lọc.` row and `0 bản ghi`. Note this is the *no-match* empty state; the captured shot above already shows the *no-data* empty state. |
| `Assets/Screenshots/danh-muc-dti/danh-muc-dti--loading--desktop-1440.png` | on demand | Same viewport; throttle the network in DevTools (Slow 3G) and change the group filter — captures the PrimeNG `p-table` loading mask. |
| `Assets/Screenshots/danh-muc-dti/danh-muc-dti--populated--desktop-1440.png` | on demand | Same viewport, live state, against a **seeded** catalogue — page 1 of 20 rows, so the paginator and the frozen Mã/Hành động columns are exercised. The most useful next shot, since the captured one is empty-state. |
| `Assets/Screenshots/danh-muc-dti/criteria-form-dialog--desktop-1440.png` | on demand | Same viewport; click "+ Thêm chỉ tiêu", then submit with an empty Mã to also capture the `.form-error` block. |
| `Assets/Screenshots/danh-muc-dti/confirm-dialog--soft-delete--desktop-1440.png` | on demand | Same viewport; click "Xoá" on a row that **has** assessment data (`AssessmentId` non-null) — captures the soft-delete wording and the `.btn.danger` button. |
| `Assets/Screenshots/danh-muc-dti/import-dialog--importing--desktop-1440.png` | on demand | Same viewport; click "Import CSV/Excel", pick a `.csv`, click "Nhập dữ liệu" and capture during the 1500 ms poll loop — the button reads `Đang nhập…` and is disabled. |
| `Assets/Screenshots/danh-muc-dti/import-result-dialog--desktop-1440.png` | on demand | Same viewport; import a file containing at least one bad row so the per-row `<ul>` renders alongside the summary counts. |
| `Assets/Screenshots/danh-muc-dti/danh-muc-dti--tablet-900.png` | on demand | Default live state at 900×1400 — exercises `≤980px`: sidebar off-canvas (drawer closed), topbar hamburger visible, page content full-bleed. |
| `Assets/Screenshots/danh-muc-dti/danh-muc-dti--mobile-390.png` | on demand | Default live state at 390×1400 — exercises `≤560px`: `main` padding 10px, `.filters-actions` full-width on its own row, topbar user name hidden, grid horizontally scrolled to its left edge with both frozen columns visible. |

### Normalize on redesign

<!-- Screen-local quirks ONLY here — sections 1-6 stay as-shipped. Library-wide issues go to COMPONENTS.md → Known inconsistencies. -->

> ### ✅ Closed 2026-08-22 — kept for the record, not outstanding work
>
> These two items were fixed in the artifacts that owned them. They are held here because *why* they changed is worth keeping, but they are **not** part of the numbered list below, which is the list of things still to fix.
>
> - ~~The "Hành động" column renders "Sửa"/"Xoá" **unconditionally**~~ — **FIXED in `src/FE/` (2026-08-22).** The cell now gates on `row.IsEditable` like the two editable cells, rendering a muted `—` otherwise (`criteria-grid-table.html:128-133`), which also restores the prototype's behaviour (`danh-muc-dti.html:792`, `ui-spec.md` §6.10). Worth recording why this one mattered more than it looked: the read-only rule had been applied to the **toolbar** and to the **two editable cells** but not to the **action cell**, so a user browsing a past period could still delete a criterion — precisely what the rule exists to prevent. A guard applied to most of a surface reads as present until someone tries the one path it missed.
> - ~~**Documentation drift this screen exposes**~~ — **CLEARED.** Every artifact this bullet used to indict has since been corrected: `Tokens/*.md` + `tokens.json` + `DESIGN.md` were re-extracted from `styles.scss` (see the Token vocabulary note at the top of this file); `Icons.md` now declares **PrimeIcons v7 plus the PrimeNG inline-SVG set** and lists three Unicode legacy exceptions; `COMPONENTS.md` grew from 12 to 27 specs and now covers every element this screen composes — the `p-table` grid (`Components/DataTable.md`), `.action-btn` (`Components/ActionButton.md`), `.cell-icon-btn` + `.cell-editable` (`Components/CellIconButton.md`), the paginator (part of `DataTable.md`) and the `Sidebar`/`Topbar`/`Toast` shell; `Components/Table.md` was rewritten against `src/FE/` and its "Don't add pagination" line is gone; and `Components/Button.md` now records `.btn.danger` as a live, shipped variant used exactly once — on this screen's delete confirm.

1. Inline edit is **double-click-only** (`(dblclick)`, `criteria-grid-table.html:72,107`) — the prototype used a single click (`ui-spec.md` §3.1). The only discovery cue is a dashed `--brand` underline on `:hover` (`criteria-grid-table.scss:10-12`), which does not exist on touch, so on a phone the affordance is invisible and the gesture undiscoverable; keyboard users get `Enter` parity but nothing announces it → add a visible affordance and a single-click/tap path.
2. A failed inline-edit commit **loses the typed value**: `confirmEdit` clears `editingCell` synchronously (`criteria-grid-table.ts:119`) before the `PUT` resolves, so the error toast arrives after the cell has already reverted to the old value with no retry → keep the cell open (or hold the pending value) until the request settles.
3. The Trạng thái column renders as plain text (`{{ row.Status || '—' }}`, `criteria-grid-table.html:41`), whereas the prototype rendered it as a neutral `Badge` (`.badge.bwork`, `danh-muc-dti.html:787`, `ui-spec.md` §5 col. 7). `Badge` is a documented component that this screen consequently no longer uses → restore the badge treatment or record the plain-text choice deliberately.
4. Import gives no progress and no way out: the poll loop runs every 1500 ms with no timeout and no cancel, and the only state indicator is the disabled `Đang nhập…` button (`danh-muc-dti.page.ts:23,301-335`). A job stuck in `Pending` leaves the dialog in that state forever unless the user navigates away → add elapsed/percent feedback, a cancel action, and a client-side timeout.
5. An errored import request leaves the dialog open with its button re-enabled and only a toast to explain (`danh-muc-dti.page.ts:331-333`), while an import job that reports `Failed` closes the dialog and shows a toast with no result dialog (`danh-muc-dti.page.ts:325-329`) — two different failure paths with two different, easily-missed presentations → unify them into one persistent, in-dialog error surface.
6. Post-mutation refetches are invisible and failure-silent: `reload()` never sets `loading()` and swallows its error to keep stale rows on screen (`danh-muc-dti.page.ts:130-143`), so after a save the grid can display data that no longer matches the server with no indication → surface refetch state and refetch failure.
7. One empty message covers three different situations — empty catalogue, no filter matches, and failed first load all render `Không có chỉ tiêu nào khớp bộ lọc.` (`criteria-grid-table.html:126`, `danh-muc-dti.page.ts:125`) → split into distinct empty / no-results / load-failed states with a retry action on the last.
8. Print output is clipped: the grid's height is an inline px `scrollHeight` computed from the viewport (`criteria-grid-table.ts:90-94`) and, unlike the prototype's `@media print { #dtiGridCard { height:auto !important } }` (`danh-muc-dti.html:137`), nothing releases it for print; the paginator also lacks `no-print` → add a print rule that unsets the scroll height and hides paging controls.
9. Months are unreachable from this screen: `PeriodOptionsService` fetches `MonthsInYear` alongside `WeeksInYear` (`period-options.service.ts:33`) but the period filter only lists weeks (`danh-muc-dti.page.html:32`), so month periods that exist in the data cannot be selected here → either render them or stop fetching them.
10. Dead and duplicated style hooks: `styleClass="dti-grid"` is set on the `p-table` (`criteria-grid-table.html:14`) but no `.dti-grid` rule exists anywhere in the app, and `.filters-actions` is declared twice with identical rules — globally in `styles.scss:338-343` and again in `danh-muc-dti.page.scss:7-12`, where only the `≤560px` override is actually unique → remove the dead hook and the duplicate block.
11. `confirm-dialog.scss:2-3` hard-codes `font-size: 13.5px; line-height: 1.55` instead of using `--fs-*`, the one place on this screen that bypasses the token scale → move it onto a token.

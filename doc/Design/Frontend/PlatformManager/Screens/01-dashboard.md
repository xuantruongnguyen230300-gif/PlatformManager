---
project: "PlatformManager"
status: "draft"
updated: "2026-08-22"
flow: "DTI Weekly Dashboard"
screens: ["DTI Weekly Dashboard"]
source_routes: ["/dashboard"]
---

# DTI Weekly Dashboard — Screens

The Dashboard is the app's landing route (`''` redirects here, and so does the `**` wildcard — `app.routes.ts:10,37`) and its only **read-only** analytical surface: a digital-transformation officer picks a reporting period, then reads five KPI tiles, per-group progress bars, a trend line, the full criteria table for that period, and the list of saved periods. **Nothing on this screen writes data** — the page component says so in its own header comment ("Dashboard 100% đọc-only — không có action ghi dữ liệu nào ở màn này", `dashboard.page.ts:37-38`), and every editing affordance the prototype had (progress inputs, note inputs, "Lưu tuần này", "Sao lưu"/"Khôi phục", the mobile `.fab`) is gone; catalogue writes live on `/danh-muc/dti` (`Screens/02-danh-muc-dti.md`). The one action here is "Xuất báo cáo", which fetches server-rendered HTML into an in-page `Dialog` — not a separate route. This spec cross-references `doc/contracts/dashboard.md` (DB-1 aggregate, DB-2 report, DB-3 period options), `spec/dashboard-dti-weekly/ui-spec.md` (§1 read-only scope, §2 Layout, §3 Actions, §4 States, §5 Responsive, §6 UI↔ERD field map) and `spec/dashboard-dti-weekly/business-rules.md` (§0 the read-only decision, §3.2 delta epsilon, §3.3 weighted average, §3.5 counts, §5 badge vs. `Status`) alongside the shipped Angular source.

> **Rewritten 2026-08-22.** Until this pass, this file described the deleted prototype — a single-page `localStorage` app with inline editing and a floating save button. That file is **frozen history** (`doc/Design/CLAUDE.md` § Fidelity Policy) and is cited below only where it explains *why* a decision was made. Everything factual here was re-verified against `src/FE/src/app/modules/dashboard/`.
>
> **Shell:** app shell — `Sidebar` + `Topbar` + `main` + `Toast` (`app.html:1-14`), rendered because this route does not set `data.noShell`. Route guard is `authGuard` only — no role gate, because Dashboard has no `SysMenuRole` row (`dashboard.routes.ts:4-5,11`). **`DESIGN.md` → Layout describes this shell correctly** as of the 2026-08-22 token refresh.
> **Token vocabulary:** token names below are the live CSS custom properties in `styles.scss:19-85` minus the `--` prefix (`--sp-*` → `sp-*`, `--brand` → `brand`), plus the named structural measurements in `Tokens/spacing.md` (`breakpoint.tablet`, `dimension.chart-height`, `layout-grid-desktop`, …). `Tokens/*`, `tokens.json` and `DESIGN.md` were all re-extracted from `src/FE/src/styles.scss` on 2026-08-22 and now match this screen; values recorded here without a token name are genuinely untokenised literals and are called out as such.
> **Sources:** `src/FE/src/app/modules/dashboard/` (`dashboard.routes.ts`, `pages/dashboard/dashboard.page.{html,ts,scss}`, `components/{period-toolbar,kpi-summary,kpi-tile,group-progress-list,trend-chart,criteria-table,delta-indicator,status-badge,history-list,report-dialog}/`, `services/dashboard.{service,mapper}.ts`, `models/dashboard.model.ts`), `src/FE/src/app/{app.html,app.scss,app.ts,app.routes.ts}`, `src/FE/src/app/shared/components/{sidebar,topbar,toast}/`, `src/FE/src/app/shared/services/period-options.service.ts`, `src/FE/src/app/core/{interceptors/http-error.interceptor.ts,toast/toast.service.ts,theme/platform-manager-preset.ts,menu/menu.service.ts}`, `src/FE/src/styles.scss`, `doc/contracts/dashboard.md`, `spec/dashboard-dti-weekly/{ui-spec,business-rules}.md`. Design-intent reference only (**not** as-shipped): the deleted prototype. File:line citations below use the bare filename of these paths.

---

## DTI Weekly Dashboard (`/dashboard`)

### Layout Blueprint

<!-- Region tree + structural measurements. Compose ONLY component names present in COMPONENTS.md. -->

- **App shell** (`app.html:1-14`) — `showShell()` is true for this route (no `data.noShell`)
  - `Sidebar` (`sidebar.html:1-77`) — fixed left, `dimension.sidebar-w` / `dimension.sidebar-w-collapsed`, `z-index:35`, fill `card`, right border `line` (`sidebar.scss:3-20`). Menu tree comes from `GET /api/meta/menu`; the "Dashboard" item is a top-level leaf in the `routerLinkActive="active"` state on this route
  - `.shell-content` (`app.scss:11-22`) — `margin-left: dimension.sidebar-w` (or `-collapsed`), flex column
    - `Topbar` (`topbar.html:1-24`) — sticky, `z-index:20`, translucent white + `blur(10px)`, bottom border `line`; inner `.topin` capped at `dimension.container-max-width`, padding `sp-4 sp-5`
      - `Button` (icon-only modifier, `.btn.sidebar-hamburger.no-print`) — hidden above `breakpoint.tablet`
      - `.logo h1` — route title text "Dashboard" (`typography.h1-topbar`)
      - `.topbar-user` (`margin-left:auto`) — user name text + `Button` (default/tonal) "Đăng xuất"
    - `main` (`app.scss:24-30`) — `max-width: dimension.container-max-width`, centred, padding `sp-5`
      - `<router-outlet>` → **DashboardPage** (`dashboard.page.html:1-66`)
  - `Toast` (`toast.html:1-15`) — `.toast-stack.no-print`, fixed bottom-right at `sp-5` inset, `z-index:60`, `dimension.toast-stack-max-width`, `aria-live="polite"`, 4 severities, 5 s auto-dismiss

- **Page** — six stacked regions, **no page-level wrapper element**: the page component's template starts directly with the toolbar (`dashboard.page.html:1`)
  - `<app-period-toolbar>` → `Card` (Toolbar-card variant, `.weekbar.card.no-print`, `period-toolbar.html:1`) — horizontal flex, `gap sp-3`, `flex-wrap:wrap`, `margin-bottom sp-5` (`period-toolbar.scss:1-7`)
    - `<strong>` label text "Kỳ đang xem:" — plain markup
    - `.period-display` — **plain markup, deliberately not given its own component spec** (`COMPONENTS.md` § "Deliberately NOT given their own spec"): a read-only chip showing the server's `PeriodLabel`; border `border-strong`, radius `rounded.sm`, fill `bg`, text `muted`, padding `sp-2 sp-3`, `fs-sm` (`period-toolbar.scss:16-23`). It is a *display* element styled like an input, not an input — property for property it is the filter tier of `Components/Input.md` with only `background` and `color` changed, i.e. a fifth, read-only input tier. Promote it only if a second read-only-value chip appears, or fold it into `Input.md`; that decision is open
    - `Badge` (`.badge.bwork`, period-mode chip) — **conditional**, only while `isAllMode()` (`period-toolbar.html:5-7`)
    - `Input` (select variant, filter tier) — year picker, options from `GET /api/dashboard/periods` → `Years` (`period-toolbar.html:9-13`)
    - `Input` (select variant, filter tier) — **one of two, swapped by `@if`**: the week picker (`:15-26`, options = "current" + "all" + `WeeksInYear`) or the month picker (`:27-34`, options = "current" + `MonthsInYear`)
    - `SegmentedControl` (`.segmented` + `.seg-btn` × 2, `Components/SegmentedControl.md`, `period-toolbar.html:36-48`) — `inline-flex` with a shared 1px `line` border, `rounded.sm`, `overflow:hidden`, internal `border-left` divider, and an `.active` state filled `brand` on `on-primary` (`period-toolbar.scss:38-73`). It is the app's **only** view switcher
    - `.weekbar-actions` (`margin-left:auto`, `flex:none`) → `Button` (primary) "Xuất báo cáo"
  - `<app-kpi-summary>` → `.kpis` grid (`kpis-grid-desktop`: `repeat(5, 1fr)`, gap `sp-4`, `kpi-summary.scss:1-5`)
    - `KpiTile` × 5, in fixed order (`kpi-summary.html:2-6`): overall progress · delta vs. previous period (`tone` bound to the delta sign) · "Chỉ tiêu tăng" (`tone="good"`) · "Không tăng" (`tone="warn"`) · "Hoàn thành 100%". Only tiles 2–4 carry a tone; 1 and 5 are `default`
  - `section.layout` (`layout-grid-desktop`: `1.15fr 0.85fr`, gap `sp-5`, `margin-top sp-5`, `dashboard.page.scss:1-15`) — two `Card`s, each `display:flex; flex-direction:column` with a non-shrinking `.title`
    - `Card` — `.title` (h2 "Tiến độ theo nhóm" + `.muted` caption) → `<app-group-progress-list>`
      - `ProgressBar` × N — one `.group-row` per criteria group (`group-row-grid-desktop`: `210px 1fr 80px`), each = bold `Code. Name` + `.bar`/`.fill` track (`dimension.progress-bar-height`, track `surface-track`, fill `brand`, both `rounded.pill`) + right-aligned `.num` percentage. The list is `justify-content: space-between` so rows spread to the card's full height (`group-progress-list.scss:1-7`)
      - `@empty` branch — one `.muted` line
    - `Card` — `.title` (h2 with mode-dependent text + `.muted` caption) → `@defer (on viewport)` → `<app-trend-chart>`, with a `.chart-skeleton.muted` placeholder (`min-height` = `dimension.chart-height`, `dashboard.page.scss:21-26`)
      - `TrendChart` (`Components/TrendChart.md`) — PrimeNG `<p-chart type="line">` (`primeng` 20.2 + `chart.js` 4.5), fixed at `dimension.chart-height` × 100% via an inline `[style]` object (`trend-chart.html:8`), wrapped in a centring `.chart-wrap`. Its palette **is** tokenised — `chart-series-1`, `chart-series-1-fill`, `chart-axis-label`, `chart-grid` in `DESIGN.md` § Chart Palette — and resolved at runtime from `--brand` / `--muted` / `--line` by `readCssVar()` (`trend-chart.ts:13-16,55-61`), because a Chart.js 2D canvas cannot resolve `var()`. Single dataset, legend hidden, y-axis pinned `[0,100]` with a `%` tick suffix, `pointRadius: 4`, `tension: 0`, `fill: true`, `spanGaps: false` over a null-prefiltered series (`trend-chart.ts:85-126`)
  - `<app-criteria-table>` → `Card` (Criteria-table-card variant, `.card.criteria-table-card`, `margin-top:16px` — an untokenised literal, `criteria-table.scss:1-3`)
    - `.title` — h2 `{{ rows().length }} chỉ tiêu DTI` + `.muted` filtered/total count
    - `FilterBar` (`.filters.no-print`, `criteria-table.html:7-27`) — four controls, all **client-side** and all `Input`s at the filter tier: search text (`flex:1`, `dimension.filter-input-min-width`, **not debounced**) + group select + change select + sort select. There is **no** `.filters-actions` cluster on this screen
    - `.tablewrap` → `DataTable` (`criteria-table.html:29-77`) — PrimeNG `p-table`, `[scrollable]="true"`, `scrollHeight="480px"` (`criteria-table.html:36` — a static literal, not the computed height used on the catalogue), `[paginator]="true"`, `[rows]="20"`, `[rowsPerPageOptions]="[10, 20, 50]"`, `dataKey="CriteriaId"`. **Not `[lazy]`** — `[value]` is the already-filtered client array. Wrapper: border `border-strong`, `rounded.table`, `overflow:hidden` (`criteria-table.scss:5-9`)
      - Header row — 9 `<th>` with **percentage** widths totalling 100% (6/24/12/7/9/9/8/9/16), four of them `.num`; columns 5 and 6 have mode-dependent labels. No `min-width` anywhere, no frozen columns
      - Body row — read-only text cells painted by the global `Table` rules, plus `DeltaIndicator` in column 7 and `Badge` (via `<app-status-badge>`) in column 8; column 9 falls back to a `.muted` `—` when the note is empty. **No inputs, no action buttons, no row selection**
      - `#emptymessage` — one `<tr><td colspan="9" class="muted">` message row
      - PrimeNG paginator at the bottom, styled entirely by `PlatformManagerPreset`; **not** marked `no-print`
  - `section.card.history-card` (`margin-top:16px` — untokenised literal, `dashboard.page.scss:17-19`)
    - `.title` — h2 "Lịch sử các kỳ đã lưu" + `.muted` caption
    - `@defer (on viewport)` → `<app-history-list>`, with a `.chart-skeleton.muted` placeholder
      - `HistoryRow` × N (`histrow-grid`: `100px 1fr 90px 70px`) inside a `.history` scroller capped at `dimension.history-max-height` — date · progress · `DeltaIndicator` (or a `.muted` "Kỳ đầu" on the oldest row) · `Button` (default/tonal) "Xem"
      - `@empty` branch — one `.muted` line
  - `Footer` (`.footer`, `Components/Footer.md`, `dashboard.page.html:55-58`) — `typography.footer`, colour `muted`, padding `12px 4px`, containing a `brand`-coloured `routerLink` to `/danh-muc/dti`. Page content, not app shell: this is the app's only instance and the other five routes have none. Declared **once**, globally at `styles.scss:486-500` (it was duplicated in `dashboard.page.scss` until 2026-08-22 — see Normalize on redesign)
  - `<app-report-dialog>` → `Dialog` (default width variant, `dimension.dialog-width`, `report-dialog.html:1-11`)
    - `.title` row — h2 bound to the server's report title + `Button` (default/tonal) "Đóng"
    - `.report` body — server-rendered HTML injected via `[innerHTML]` after `bypassSecurityTrustHtml` (`report-dialog.ts:46`). **`.report` has no CSS rule anywhere in `src/FE/`** — the class is a dead style hook and the block inherits body typography (see Normalize on redesign)
    - `.dialog-actions` — `Button` (default/tonal) "Sao chép" + `Button` (primary) "In"

### Copy

<!-- Verbatim shipped strings — typos and mixed languages included — with localization key and file:line source. -->

| Element | Verbatim copy | Localization key | Source |
| --- | --- | --- | --- |
| Topbar page title | `Dashboard` | — (hardcoded, route `data.title`) | `dashboard.routes.ts:10` |
| Topbar hamburger `aria-label` | `Mở menu điều hướng` | — (hardcoded) | `topbar.html:6` |
| Topbar logout button (`title` + label) | `Đăng xuất` | — (hardcoded) | `topbar.html:18-19` |
| Topbar user name (dynamic) | `{{ currentUser.fullName() }}` | — (server data, `GET /api/auth/me`) | `topbar.html:17` |
| Sidebar brand mark / brand text | `PM` / `PlatformManager` | — (hardcoded) | `sidebar.html:3-4` |
| Sidebar collapse toggle `aria-label` (2 values) | `Mở rộng menu` / `Thu gọn menu` | — (hardcoded) | `sidebar.html:8` |
| Sidebar nav labels (incl. "Dashboard") | *(not in the view)* | — (server-driven, `GET /api/meta/menu` → `SysMenu.Label`) | `sidebar.html:32,46,60`; `menu.service.ts:22` |
| Toast close `aria-label` | `Đóng thông báo` | — (hardcoded) | `toast.html:8` |
| Period toolbar label | `Kỳ đang xem:` | — (hardcoded) | `period-toolbar.html:2` |
| Period display (dynamic) | server `PeriodLabel`, e.g. `Tuần 33/2026 (10/08–16/08/2026)` / `Tháng 8/2026` / `Năm 2026`; falls back to `—` | — (server data, `GET /api/dashboard`) | `period-toolbar.html:3`; `doc/contracts/dashboard.md` DB-1 |
| All-mode chip (dynamic, conditional) | `Tất cả · <year>` | — (hardcoded) | `period-toolbar.html:6` |
| Year select `title` | `Chọn năm` | — (hardcoded) | `period-toolbar.html:9` |
| Week select `title` | `Chọn 1 tuần cụ thể hoặc xem Tất cả` | — (hardcoded) | `period-toolbar.html:18` |
| Week select, default option | `— Kỳ hiện tại —` | — (hardcoded) | `period-toolbar.html:21` |
| Week select, aggregate option | `— Tất cả (tổng hợp theo năm) —` | — (hardcoded) | `period-toolbar.html:22` |
| Week select, saved-period options (dynamic) | `dd/mm/yyyy` + ` · <n,n>%` when progress is known | — (hardcoded template over server data) | `period-toolbar.ts:5-16` |
| Month select `title` | `Chọn tháng` | — (hardcoded) | `period-toolbar.html:28` |
| Month select, default option | `— Tháng hiện tại —` | — (hardcoded) | `period-toolbar.html:29` |
| Month select, options (dynamic) | `Tháng <n>` + ` · <n,n>%` when progress is known | — (hardcoded template over server data) | `period-toolbar.ts:18-21` |
| Segmented control `aria-label` | `Chế độ xem theo Tuần hoặc Tháng` | — (hardcoded) | `period-toolbar.html:36` |
| Segmented buttons | `Tuần` / `Tháng` | — (hardcoded) | `period-toolbar.html:38,46` |
| Export button | `Xuất báo cáo` | — (hardcoded) | `period-toolbar.html:51` |
| KPI 1 label (3 values) | `Tiến độ chung (tổng hợp năm)` / `Tiến độ chung tháng này` / `Tiến độ chung tuần này` | — (hardcoded) | `kpi-summary.ts:34-37` |
| KPI 1 sub | `Bình quân gia quyền theo điểm` | — (hardcoded) | `kpi-summary.html:2` |
| KPI 2 label (2 values) | `So với tháng trước` / `So với tuần trước` | — (hardcoded) | `kpi-summary.ts:39-41` |
| KPI 2 sub — fallback when there is no previous period | `Chưa có kỳ trước` | — (hardcoded) | `kpi-summary.ts:46` |
| KPI 2 sub — otherwise (dynamic) | server `PreviousPeriodLabel` | — (server data) | `kpi-summary.ts:46` |
| KPI 3 label / sub | `Chỉ tiêu tăng` / `Có tiến bộ so với kỳ trước` | — (hardcoded) | `kpi-summary.html:4` |
| KPI 4 label / sub | `Không tăng` / `Cần chú ý theo dõi` | — (hardcoded) | `kpi-summary.html:5` |
| KPI 5 label / value / sub | `Hoàn thành 100%` / `<done>/<total>` / `Số chỉ tiêu đạt đủ tiến độ` | — (hardcoded) | `kpi-summary.html:6`, `kpi-summary.ts:47` |
| KPI percent + delta formats, null placeholder | `vi-VN`, 1 decimal; percent `<n,n>%`; delta `↑ <n,n> đ.%` / `↓ <n,n> đ.%` / `<n,n> đ.%`; null → `—` | — (hardcoded) | `kpi-summary.ts:5-13` |
| Groups card title | `Tiến độ theo nhóm` | — (hardcoded) | `dashboard.page.html:23` |
| Groups card caption (3 values, dynamic) | `Tổng hợp năm <year>` / `Tháng hiện tại` / `Tuần hiện tại` | — (hardcoded) | `dashboard.page.html:24` |
| Group row label (dynamic) | `<GroupCode>. <GroupName>` | — (server data) | `group-progress-list.html:4` |
| Group row percentage / null placeholder | `<n,n>%` / `—` | — (hardcoded, `vi-VN`) | `group-progress-list.ts:4-6` |
| Groups empty message | `Chưa có dữ liệu nhóm chỉ tiêu.` | — (hardcoded) | `group-progress-list.html:9` |
| Chart card title (2 values) | `Biểu đồ tiến độ hàng tuần` / `Biểu đồ tiến độ hàng tháng` | — (hardcoded) | `dashboard.page.html:30` |
| Chart card caption (2 values, dynamic) | `Tổng hợp năm <year>` / `Tiến độ chung` | — (hardcoded) | `dashboard.page.html:31` |
| Chart `@defer` placeholder | `Đang tải biểu đồ…` | — (hardcoded) | `dashboard.page.html:36` |
| Chart empty message | `Chưa có đủ dữ liệu để vẽ biểu đồ.` | — (hardcoded) | `trend-chart.html:11` |
| Chart y-axis tick format | `<value>%` | — (hardcoded callback) | `trend-chart.ts:119` |
| Chart x-axis labels (dynamic) | server `Trend[].Label` — `YYYY-Www` in week mode, `Th.1`…`Th.12` in month/year mode | — (server data) | `doc/contracts/dashboard.md` DB-1 |
| Criteria card title (dynamic) | `<n> chỉ tiêu DTI` | — (hardcoded, interpolation `{{ rows().length }} chỉ tiêu DTI`) | `criteria-table.html:3` |
| Criteria card count caption (dynamic) | `<filtered>/<total> chỉ tiêu` | — (hardcoded) | `criteria-table.ts:89` |
| Search placeholder | `Tìm mã hoặc tên chỉ tiêu...` | — (hardcoded) | `criteria-table.html:8` |
| Group filter, default option | `Tất cả nhóm` | — (hardcoded) | `criteria-table.html:10` |
| Group filter, option template (dynamic) | `<Code>. <Name>` | — (derived from the loaded rows, `criteria-table.ts:54-60`) | `criteria-table.html:12` |
| Change filter options (5) | `Tất cả mức thay đổi` / `Chỉ tiêu tăng` / `Không tăng` / `Giảm` / `Hoàn thành` | — (hardcoded) | `criteria-table.html:16-20` |
| Sort options (3) | `Theo mã chỉ tiêu` / `Tăng nhiều nhất` / `Tiến độ thấp nhất` | — (hardcoded) | `criteria-table.html:23-25` |
| Grid column headers (7 fixed) | `Mã` / `Chỉ tiêu` / `Nhóm` / `Điểm tối đa` / `Tăng/giảm` / `Trạng thái` / `Ghi chú tuần` | — (hardcoded) | `criteria-table.html:41-44,47-49` |
| Grid column 5 header (3 values) | `Tuần trước` / `Tháng trước` / `—` | — (hardcoded) | `criteria-table.ts:47-49` |
| Grid column 6 header (3 values) | `Tuần này` / `Tháng này` / `Tất cả (TB)` | — (hardcoded) | `criteria-table.ts:50-52` |
| Nhóm cell template (dynamic) | `<GroupCode>. <GroupName>` | — (server data) | `criteria-table.html:56` |
| Percent cells / null placeholder | `<n,n>%` / `—` | — (hardcoded, `vi-VN`) | `criteria-table.ts:11-13` |
| Empty-note placeholder | `—` | — (hardcoded) | `criteria-table.html:66` |
| Delta cell text | `↑ +<n,n> đ.%` / `↓ <n,n> đ.%` / `<n,n> đ.%` / `—` | — (hardcoded, `vi-VN`, default suffix `' đ.%'`) | `delta-indicator.ts:25,35-40` |
| Status badge labels (4 values, dynamic) | `Hoàn thành` / `Đang thực hiện` / `Không tăng` / `Chưa có dữ liệu` | — (server-computed text, mapped to a class only) | `status-badge.ts:4-9`; `doc/contracts/dashboard.md` DB-1 |
| Status badge null placeholder | `—` | — (hardcoded) | `status-badge.html:4` |
| Grid empty message | `Không có chỉ tiêu nào khớp bộ lọc.` | — (hardcoded) | `criteria-table.html:73` |
| History card title / caption | `Lịch sử các kỳ đã lưu` / `Không ghi đè dữ liệu tuần cũ` | — (hardcoded) | `dashboard.page.html:45-46` |
| History `@defer` placeholder | `Đang tải lịch sử…` | — (hardcoded) | `dashboard.page.html:51` |
| History row progress label | `Tiến độ chung` | — (hardcoded) | `history-list.html:6` |
| History oldest-row marker | `Kỳ đầu` | — (hardcoded) | `history-list.html:10` |
| History row action button | `Xem` | — (hardcoded) | `history-list.html:14` |
| History empty message | `Chưa có tuần nào trong năm đang chọn.` | — (hardcoded) | `history-list.html:17` |
| Footer note | `Xem toàn bộ danh mục & nhập/cập nhật dữ liệu tại Danh mục > DTI.` (source writes `&amp;` and `&gt;`; "Danh mục > DTI" is the link text) | — (hardcoded) | `dashboard.page.html:56-57` |
| Report dialog default title (before the first fetch resolves) | `Báo cáo tiến độ DTI` | — (hardcoded, in two places) | `dashboard.page.ts:72`, `report-dialog.ts:41` |
| Report dialog title after fetch (dynamic) | server `Title` | — (server data, `GET /api/dashboard/report`) | `report-dialog.html:3` |
| Report dialog body (dynamic) | server `ContentHtml`, rendered verbatim — the FE composes **no** report text of its own | — (server data) | `report-dialog.html:6`; `doc/contracts/dashboard.md` DB-2 |
| Report dialog buttons | `Đóng` / `Sao chép` / `In` | — (hardcoded) | `report-dialog.html:4,8,9` |
| Copy success toast | `Đã sao chép báo cáo.` | — (hardcoded) | `dashboard.page.ts:167` |
| Copy failure toast | `Không sao chép được — trình duyệt chặn quyền truy cập clipboard.` | — (hardcoded) | `dashboard.page.ts:169` |
| HTTP error toasts (shared interceptor fallbacks, 6 values) | `Không thể kết nối tới máy chủ. Kiểm tra kết nối mạng.` / `Bạn cần đăng nhập để tiếp tục.` / `Bạn không có quyền thực hiện thao tác này.` / `Không tìm thấy dữ liệu yêu cầu.` / `Bạn thao tác quá nhanh. Vui lòng chờ một lát rồi thử lại.` / `Đã có lỗi xảy ra. Vui lòng thử lại.` | — (hardcoded) | `http-error.interceptor.ts:21,23,25,27,32,34` |

### States

<!-- How each state renders: default / loading / empty / error / validation display. -->

- **default (first paint):** `viewMode` = `'week'`, `selectedYear` = `new Date().getFullYear()` evaluated per component instance, `selectedWeekValue` = `''` (`dashboard.page.ts:62-65`). An empty week value means "no `date` param", and the contract makes the server pick today's period (`doc/contracts/dashboard.md` DB-1). Two `effect`s fire in parallel: one loads `GET /api/dashboard/periods?year=…`, the other `GET /api/dashboard?mode=week&year=…` (`dashboard.page.ts:100-118`). Until both land, the page renders `EMPTY_AGGREGATE` (`dashboard.page.ts:16-32`): period display `—`, KPI 1 and 2 `—`, KPIs 3–5 `0`/`0`/`0/0`, empty groups list, empty chart, empty table, `0 chỉ tiêu DTI`.
- **loading — there is no loading affordance.** `loading()` is set true before every aggregate fetch and false in both callbacks (`dashboard.page.ts:110,114,116`) but is **never read by the template** — grep for `loading` in `modules/dashboard/` returns only those four lines. `<app-criteria-table>` takes no `loading` input and its `p-table` has no `[loading]` binding, so PrimeNG's mask never appears either. Changing period, year or mode therefore swaps the whole page's numbers with no spinner, no skeleton and no dimming; the previous period's data stays fully legible on screen until the new response lands. See Normalize on redesign.
- **loading — the two `@defer` placeholders are the only "loading" text on the screen**, and they are about *code* loading, not data: `@defer (on viewport)` around `TrendChart` and `HistoryList` renders `Đang tải biểu đồ…` / `Đang tải lịch sử…` in a `.chart-skeleton` until the lazy chunk downloads (`dashboard.page.html:33-37,48-52`). Above the fold on a desktop viewport both usually resolve immediately.
- **populated — week mode, a specific period (the common case):** the period display shows the server's `PeriodLabel`; the year and week selects both have a value; the mode-dependent labels read "Tuần trước"/"Tuần này", "Tuần hiện tại", "Biểu đồ tiến độ hàng tuần" (`criteria-table.ts:47-52`, `dashboard.page.html:24,30`). All five KPI tiles, every group bar, the trend line and all table columns carry real numbers.
- **populated — no previous period:** the server returns `Delta: null` and `PreviousPeriodLabel: null`, so KPI 2 shows `—` with the sub-caption `Chưa có kỳ trước` and `tone="default"` (`kpi-summary.ts:9-18,46`). Table columns "Tuần trước" and "Tăng/giảm" render `—` per row. **Unlike the prototype, KPI 4 ("Không tăng") still shows a number, not `—`** — it is bound to `kpi().Flat` unconditionally (`kpi-summary.html:5`), and the backend counts flat only where a previous value exists (`business-rules.md` §3.5). The history panel marks the oldest row `Kỳ đầu` instead of a delta (`history-list.html:9-13`).
- **populated — month mode:** the segmented control swaps the period select to the month picker, KPI 1/2 labels become "Tiến độ chung tháng này"/"So với tháng trước", the groups caption becomes "Tháng hiện tại", the chart title becomes "…hàng tháng" and the table's two value columns become "Tháng trước"/"Tháng này". Switching mode resets **both** period values to `''` (`dashboard.page.ts:121-125`), so the view always lands on the current month rather than an unrelated period. The history panel is **not** mode-aware — it always lists weeks (`dashboard.page.html:49`).
- **populated — "Tất cả" (year aggregate):** picking `__ALL__` in the week select makes `isAllMode()` true, which switches the request to `Mode: 'year'` (`dashboard.page.ts:75-77,90-92`). The `Badge` chip `Tất cả · <year>` appears, KPI 1 becomes "Tiến độ chung (tổng hợp năm)", the two card captions become "Tổng hợp năm <year>", the table's column 5 header degrades to `—` and column 6 becomes "Tất cả (TB)". KPI 2 keeps the label "So với tuần trước" in this mode (`kpi-summary.ts:39-41`) — see Normalize on redesign.
- **empty — no data at all:** every region has its own empty branch and they are independent. Groups → `Chưa có dữ liệu nhóm chỉ tiêu.` (`group-progress-list.html:8-10`); chart → `Chưa có đủ dữ liệu để vẽ biểu đồ.`, shown whenever **no** trend point has a non-null value (`trend-chart.html:2-12`, `trend-chart.ts:50`); table → one `colspan="9"` `.muted` row, and the title reads `0 chỉ tiêu DTI` with `0/0 chỉ tiêu`; history → `Chưa có tuần nào trong năm đang chọn.` (`history-list.html:16-18`). KPI tiles have no empty branch — they render `—`/`0`/`0/0`.
- **empty — filtered table with zero matches:** identical to the no-data table state: the same `Không có chỉ tiêu nào khớp bộ lọc.` row, with the count caption reading `0/<total> chỉ tiêu`. The paginator still renders. One message therefore covers three situations — no data for the period, no filter matches, and a failed aggregate fetch (see Normalize on redesign).
- **error — aggregate fetch fails:** the shared interceptor shows an error `Toast` using the API envelope's `message` when present, else the status fallback (`http-error.interceptor.ts:82`). Beyond the toast the page does nothing: the `error` callback only clears `loading()` and leaves `aggregate()` untouched (`dashboard.page.ts:116`). On first load that means the page keeps `EMPTY_AGGREGATE` and looks exactly like a genuinely empty period; on a later period change it keeps the **previous** period's numbers on screen while the toolbar shows the newly-selected period. There is no retry control and no error region.
- **error — period-options fetch fails:** `periodOptions` is set to `null` (`dashboard.page.ts:104`). The year select then falls back to a single option — the currently selected year, injected by the `years` computed (`dashboard.page.ts:79-82`) — the week/month selects show only their default option, and the history panel shows its empty message even when saved periods exist.
- **error — report fetch fails:** the dialog simply never opens; `onExportReport`'s error branch is deliberately empty because the interceptor toast is the whole feedback (`dashboard.page.ts:155-157`). The button is not disabled and shows no pending state, so a slow report looks like a dead click.
- **error — session expired (401 mid-session):** the interceptor clears the user context and navigates to `/dang-nhap?returnUrl=/dashboard` on top of the toast (`http-error.interceptor.ts:52-58,83`). This screen has no 401 handling of its own.
- **report dialog — open:** `onExportReport` fetches `GET /api/dashboard/report` with the **same** query params as the aggregate, then sets title + HTML and flips `reportOpen` (`dashboard.page.ts:148-159`). An `effect` in the dialog calls native `showModal()` (`report-dialog.ts:51-58`), so it lands on the browser top layer over an `overlay-backdrop` scrim. The body is server-rendered HTML passed through `bypassSecurityTrustHtml` — the FE composes no report text. Closing works via the "Đóng" button and native `Esc`; both route through the `<dialog>` `close` event → `closed` output → `reportOpen(false)`. **Backdrop click does not close it** — no listener is wired (see Normalize on redesign).
- **report dialog — copy:** "Sao chép" reads `.report`'s `innerText` and writes it to `navigator.clipboard`, then emits success or error; the page turns that into a `Toast` (`report-dialog.ts:64-71`, `dashboard.page.ts:165-171`). The dialog stays open either way.
- **report dialog — print:** "In" calls `window.print()` (`report-dialog.ts:73-76`) with the dialog still open, so the printed page is the dashboard under the print rules below, not the report block alone (see Normalize on redesign).
- **history → period navigation:** clicking "Xem" emits the period value; the page forces `viewMode` back to `'week'` and sets `selectedWeekValue`, deliberately mirroring the prototype's `loadSavedWeek()` (`dashboard.page.ts:141-146`). This is the only cross-region interaction on the screen.
- **validation display:** **none, and none is possible** — the screen has no text input that accepts a value (the search box filters, it does not validate), no form, no submit and no write path. The interceptor toasts are the entire feedback surface; they auto-dismiss after 5000 ms (`toast.service.ts:11,48`) and there is no persistent error region anywhere on the page.

### Responsive

<!-- Behavior per breakpoint. -->

- **≥`breakpoint.desktop` / above `breakpoint.tablet` (981px and up, desktop default):** `.shell-content` is offset by `dimension.sidebar-w` (or `dimension.sidebar-w-collapsed` when collapsed, `app.scss:11-22`); the topbar hamburger is `display:none` (`topbar.scss:45-47`); a collapsed sidebar renders submenus as hover/focus-within flyouts at `left:100%` (`sidebar.scss:289-336`). `main` is capped at `dimension.container-max-width` with `sp-5` padding. Page grids: `.kpis` = `kpis-grid-desktop`, `.layout` = `layout-grid-desktop`, `.group-row` = `group-row-grid-desktop`.
- **≤`breakpoint.tablet` (980px):** `.shell-content { margin-left: 0 !important }` (`app.scss:32-36`); the sidebar becomes an off-canvas drawer at `dimension.sidebar-w-drawer-tablet`, `transform: translateX(-100%)` until `.drawer-open`, with `shadow` and a click-to-dismiss `.sidebar-backdrop` (`sidebar.scss:236-276`); the topbar hamburger becomes `display:flex` (`topbar.scss:49-56`). Page-owned rules: `.kpis` → `kpis-grid-tablet` (`repeat(2, 1fr)`, `kpi-summary.scss:7-11`), `.layout` → `layout-grid-tablet` (`1fr`, so the groups card stacks above the chart card, `dashboard.page.scss:35-39`), `.group-row` → `group-row-grid-tablet` (`group-progress-list.scss:31-35`).
- **≤`breakpoint.mobile` (560px):** `main` padding drops to `10px` (`app.scss:38-42`); the topbar user name is hidden, leaving just the logout button (`topbar.scss:39-43`); the sidebar drawer widens to `dimension.sidebar-w-drawer-mobile` and nav items grow to `min-height:40px` (`sidebar.scss:278-287`). Page-owned rules: `.kpis` gap drops to `8px` and the **fifth** tile spans the full row (`grid-column: 1 / -1`) so the 5-into-2 grid has no hole — reached through the app's **only** `::ng-deep`, `.kpis ::ng-deep .card:last-child` (`kpi-summary.scss:13-21`); `.kpi .value` drops to `18px` (`kpi-tile.scss:38-42`); every direct child of `.weekbar` takes `flex:1` and `.weekbar-actions` goes `margin-left:0; width:100%`, so "Xuất báo cáo" wraps onto its own full-width row (`period-toolbar.scss:75-84`); `.group-row` → `group-row-grid-mobile` (`group-progress-list.scss:37-41`).
- **Criteria table (all viewports, no breakpoint):** `criteria-table.scss` contains **no `@media` rule at all**, and neither the table nor any column declares a `min-width`. All nine columns are **percentage** widths summing to 100% (`criteria-table.html:41-49`), and `.tablewrap` is `overflow:hidden` — so the table never scrolls horizontally; it compresses. At a 390px viewport the 24%-wide "Chỉ tiêu" column is ~90px and wraps heavily. Vertical extent is the static `scrollHeight="480px"` (`criteria-table.html:36`) at every viewport, with the header pinned by the global sticky `th` rule. **This is a structural change from the prototype**, whose table held `min-width:1200px` and scrolled horizontally (`ui-spec.md` §5 ≤560px).
- **Filters row (all viewports, no breakpoint):** `.filters` is `flex-wrap: wrap` with the search `Input` at `flex:1; min-width: dimension.filter-input-min-width` (`styles.scss:327-338`), so the four controls reflow intrinsically — content-driven, not media-query-driven. The same is true of `.weekbar` above `breakpoint.mobile` (`period-toolbar.scss:1-7`).
- **Trend chart (all viewports):** `maintainAspectRatio: false` with `responsive: true` and a fixed `dimension.chart-height`, so the canvas fills its card's width and keeps a constant height at every breakpoint (`trend-chart.ts:112-113`, `trend-chart.html:8`).
- **History panel (all viewports):** `histrow-grid` has **no** responsive override — the four columns keep `100px 1fr 90px 70px` down to 390px, inside a scroller capped at `dimension.history-max-height` (`history-list.scss:1-17`).
- **`.card` and `.title` have no responsive rules at all.** `styles.scss` contains exactly **one** `@media` block in the whole file — the print block at `styles.scss:122-126`. The prototype's ≤560px card-padding reduction (14px→12px) and `.title { align-items: flex-start }` were **not ported**.
- **Print (`@media print`):** `.no-print` elements are hidden globally (`styles.scss:122-126`), which on this screen removes the whole period toolbar card and the criteria filters row, plus the sidebar, sidebar backdrop, topbar and toast stack via their own print rules (`sidebar.scss:338-343`, `topbar.scss:58-62`, `toast.html:1`). `.shell-content` loses its margin and `main` loses its max-width (`app.scss:44-52`). **No dashboard component declares a print rule of its own** — the KPI grid, both `.layout` cards, the chart canvas, the criteria table, the paginator, the history panel and the footer all print as laid out, and nothing releases the table's 480px scroll height or the history panel's 240px cap.

### Iconography

The shipped app loads **PrimeIcons v7** globally (`angular.json:38-39,100-101`), rendered as `<i class="pi pi-…">`. `Icons.md` was refreshed on 2026-08-22 and now records this correctly — the prototype-era `library: "none"` claim this spec used to repeat is gone.

**This screen authors no icon of its own.** Inside `modules/dashboard/` there is not a single `<i class="pi …">`: every action is a text `Button` or a native `<select>`, and the only directional cues are the literal `↑`/`↓` glyphs inside `DeltaIndicator`'s and `KpiSummary`'s formatted strings (`Icons.md` § Legacy Exceptions). Every PrimeIcons glyph visible while `/dashboard` is open therefore belongs to the app shell. The dashboard module also contains exactly **one** ARIA attribute pair — `role="group"` + `aria-label` on the segmented control (`period-toolbar.html:36`).

**But the screen is not icon-free — PrimeNG injects a second set at runtime.** `[paginator]="true"` on the criteria table (`criteria-table.html:32`) renders four inline `<svg>` arrows from PrimeNG's own icon components — `AngleDoubleLeftIcon`, `AngleLeftIcon`, `AngleRightIcon`, `AngleDoubleRightIcon`, each carrying a `data-p-icon="angle-…"` attribute. **Nothing in `src/FE/` names them**, so a grep for `pi-` misses them entirely; they exist because a component was switched on, not because an icon was written. PrimeNG's spinner (`SpinnerIcon`) does **not** appear on this screen — this `p-table` binds no `[loading]` — and neither do its sort/filter icons, since no template here uses `pSortableColumn`, `[sortField]` or `[filters]`. Full enumeration, including the missing `aria-hidden` and the English paginator labels, is in `Icons.md` § Per-Action Map (rows marked *PrimeNG SVG*) and § Normalize on redesign #7-9.

| Action | Icon | Placement |
| --- | --- | --- |
| Open navigation drawer | `pi pi-bars` | Topbar, leftmost, ≤`breakpoint.tablet` only (`topbar.html:11`) |
| Log out | `pi pi-sign-out` (icon + "Đăng xuất" label) | Topbar, far right (`topbar.html:19`) |
| Collapse / expand sidebar | `pi pi-angle-left`, rotated 180° when collapsed | Sidebar brand row, far right (`sidebar.html:12`, `sidebar.scss:83-85`) |
| Expand / collapse a nav group | `pi pi-chevron-down`, rotated −90° when closed | Sidebar parent item, far right (`sidebar.html:33`) |
| Nav item glyphs (incl. Dashboard's `pi-th-large`) | server-supplied PrimeIcons class per `SysMenu.Icon`, falling back to `pi-circle` | Sidebar, left of each label (`sidebar.html:31,45,59`; `Icons.md` § Per-Action Map) |
| Dismiss a toast | `pi pi-times` | Toast item, right edge (`toast.html:11`) |
| Increase / decrease indicator | literal `↑` / `↓` glyphs **inside the text**, not icon elements | Delta cells, KPI 2 value, history rows (`delta-indicator.ts:38`, `kpi-summary.ts:11`) |
| Paginate the criteria table (first / previous / next / last) | PrimeNG inline `<svg>`, **not** PrimeIcons — `data-p-icon="angle-double-left"`, `"angle-left"`, `"angle-right"`, `"angle-double-right"` | Paginator strip below the criteria table; injected by `p-table` because `[paginator]="true"` (`criteria-table.html:32`), with no `src/FE/` source line — see `Icons.md` § Per-Action Map |
| Switch week/month, pick year/period, export report, search/filter/sort, view a saved period, close/copy/print the report | — (text buttons and native controls, no icon) | Period toolbar, filters row, history rows, report dialog |

### Screenshots

<!-- Refs into Assets/Screenshots/dashboard/ — folder name follows the on-disk precedent (un-numbered flow stem), not the numbered spec filename. -->

**✅ Captured 2026-08-22 from the live Angular app** — one desktop shot, which is exactly the target under the 2026-08-22 policy (`doc/Design/CLAUDE.md` § Rules).

| Screenshot path | Status | Capture instructions |
| --- | --- | --- |
| `Assets/Screenshots/dashboard/dashboard--empty--desktop-1440.png` | captured 2026-08-22 | Live app, `/dashboard`, full page @ 1440 wide, sidebar expanded, week mode. Captured against an **empty database** — no period had been saved, so the KPI tiles read `—`/`0`, the group card reads `Chưa có dữ liệu nhóm chỉ tiêu.` and the trend card `Chưa có đủ dữ liệu để vẽ biểu đồ.`. That is a real state, not a capture failure, and the `--empty--` in the filename records it. To reproduce: start the API (`dotnet run --project src/BE/PlatformManager.Api --urls http://localhost:5027`; that origin is `apiBaseUrl` in `src/FE/src/environments/environment.development.ts:5`, **not** in `environment.ts`, which ships the production-relative `/api`), then the FE (`npx ng serve --port 4201`), sign in at `/dang-nhap` and open `/dashboard`. Captured via the chrome-devtools MCP; full environment recorded in `UiInventory.md` § Screenshot Manifest. No credentials are recorded in any design artifact. |

Every other view of this screen is **on demand** — captured only when someone actually needs that specific case, then added as a row here. This is deliberately *not* a backlog: an earlier pass queued 40 pending shots across 5 screens and none were ever taken.

- **Populated state** — the most useful next shot: pick a period that has a previous period saved, so KPI 2 and the "Tăng/giảm" column are populated and the trend line has ≥2 points. Full page @ 1440×1400 including the history panel and footer.
- Month mode · the "Tất cả" aggregate · the report dialog · the no-match table · tablet 900px · mobile 390px.

⚠️ **The prototype-era captures are not app screenshots and must never be reused as such.** They were taken on 2026-08-11 from the deleted prototype via `file://` with a seeded `localStorage`, and show a screen that no longer exists: a topbar toolbar with "Sao lưu"/"Khôi phục"/"Lưu tuần này", editable progress and note inputs, a hand-drawn `<canvas>` trend, a `.fab`, and the older palette/1450px container. Four of them survive under `Assets/Screenshots/dashboard/_superseded-prototype/` (its README says the same) purely for before/after comparison — never cite them from a spec, prompt pack or Figma export as evidence of current behaviour. The prototype's own `dashboard--desktop-1440.png` no longer exists; the live capture above is what replaced it.

| Frozen prototype file, now under `_superseded-prototype/` | Was | Replace with |
| --- | --- | --- |
| `dashboard--tablet-900.png` | prototype @ 900px | on demand only |
| `dashboard--mobile-390.png` | prototype @ 390px | on demand only |
| `dashboard--with-history--desktop-1440.png` | prototype, seeded history | on demand only |
| `report-dialog--desktop-1440.png` | prototype report dialog | on demand only |

### Normalize on redesign

<!-- Screen-local quirks ONLY here — sections 1-6 stay as-shipped. Library-wide issues go to COMPONENTS.md → Known inconsistencies. -->

- **Every fetch on this screen is silent.** `loading()` is computed and then never rendered (`dashboard.page.ts:69,110,114,116`; no template reference exists), and the criteria `p-table` has no `[loading]` binding even though PrimeNG supplies the mask for free and the sibling grids on `/danh-muc/dti` and `/quan-tri/nguoi-dung` use it. Changing year, period or mode replaces every number on the page with no indication that a request is in flight, and on a slow link the user reads the *old* period's data under the *new* period's label → bind `loading()` to the table and dim/disable the toolbar while a request is open.
- **A failed aggregate fetch is indistinguishable from an empty period.** The error branch only clears `loading()` and leaves `aggregate()` alone (`dashboard.page.ts:116`), so a first-load failure renders exactly the same "no data" empty states as a genuinely empty period, and a later failure silently keeps stale numbers on screen. One message — `Không có chỉ tiêu nào khớp bộ lọc.` — additionally covers three different situations (no data, no filter match, failed load) → split empty / no-results / load-failed, and give the last a retry action.
- **"Xuất báo cáo" has no pending or failure state.** The button is never disabled, shows no spinner, and its error branch is deliberately empty (`dashboard.page.ts:155-157`) — a slow or failing report is a click that appears to do nothing, explained only by a 5-second toast → add a pending state on the button and an in-place failure message.
- **The report dialog's backdrop does not close it.** Only the "Đóng" button and native `Esc` work (`report-dialog.html:1-11`) — no backdrop listener is wired, which is the same gap the prototype had (the deleted prototype, recorded in this spec's previous revision) → wire backdrop-click-to-close, or state the "Đóng/Esc only" rule deliberately.
- **"In" prints the dashboard, not the report.** `window.print()` is called with the dialog open (`report-dialog.ts:73-76`) and no dashboard component declares a `@media print` rule, so the output is the full page under the global print rules — while the one thing the user asked to print, the `.report` block, is inside a top-layer `<dialog>` → add a print stylesheet that isolates the report body, and release the table's 480px scroll height and the history panel's 240px cap for print.
- **`.report` is a dead style hook.** `report-dialog.html:6` sets `class="report"` but no `.report` rule exists anywhere in `src/FE/` (verified by grep over all SCSS), so server-rendered report HTML inherits raw body typography with no measure, spacing or heading scale. The prototype had a styled report block that was not ported (`DESIGN.md` § Components) → either style it against the type scale or drop the class.
- **The criteria table cannot be read on a phone.** Nine percentage-width columns with no `min-width`, no horizontal scroll (`.tablewrap { overflow:hidden }`) and no breakpoint mean the columns simply crush — at 390px "Chỉ tiêu" is ~90px wide. The prototype scrolled a 1200px table instead (`ui-spec.md` §5) → pick one deliberate behaviour: horizontal scroll with a frozen "Mã" column (as `/danh-muc/dti` does), or a stacked card layout below `breakpoint.tablet`.
- **The table's `scrollHeight` is a hardcoded `480px`** (`criteria-table.html:36`) with no token and no viewport awareness, so on a tall display the card wastes space and on a short one it double-scrolls inside an already-scrolling page. `/danh-muc/dti` solves the same problem with a measured height → converge on one mechanism and tokenise the result.
- **KPI 2's label is wrong in "Tất cả" mode.** `deltaLabel` only branches on `viewMode`, so the year-aggregate view still reads "So với tuần trước" over a year-over-year delta (`kpi-summary.ts:39-41`), while KPI 1 correctly switches to "(tổng hợp năm)" → add the `isAllMode()` branch to KPI 2.
- **KPI 2 uses a different delta rule from every other delta on the screen.** `KpiSummary` classifies direction with a bare `v > 0` / `v < 0` (`kpi-summary.ts:9-18`) while `DeltaIndicator` — used in the same table and history panel — applies `EPSILON = 0.001` per `business-rules.md` §3.2 (`delta-indicator.ts:4,27-33`). A delta of `0.0004` therefore renders as an increase in the KPI tile and as flat in the table → have `KpiSummary` use `DeltaIndicator`. Recorded library-wide as `COMPONENTS.md` § Known inconsistencies #8.
- **The history panel ignores month mode and the selected period.** It is always fed `WeeksInYear` (`dashboard.page.html:49`) and its empty message hardcodes the word "tuần" (`history-list.html:17`), so in month mode the user sees a week list; `MonthsInYear` is fetched and used only by the toolbar → make the panel mode-aware, or label it explicitly as a weekly log.
- ~~**`.footer` is declared twice, identically**~~ — **FIXED 2026-08-22**: the `dashboard.page.scss` copy was deleted, leaving `styles.scss:486-500` as the single declaration. **Still open:** `period-toolbar.scss:25-36` re-declares the `select` field rule that `styles.scss:347-363` already applies to `.weekbar select`, including the same focus ring → delete that duplicate too.
- **Two untokenised `margin-top:16px` literals** separate the criteria card and the history card (`criteria-table.scss:2`, `dashboard.page.scss:18`) while every other vertical rhythm on the page uses `sp-5` (14px) — including `.layout`'s own `margin-top` three lines away → move both onto the spacing scale.
- **Off-scale KPI typography.** The KPI value ships at `21px/850` and drops to `18px` below `breakpoint.mobile` (`kpi-tile.scss:11-13,38-42`); `typography.kpi-value` records the desktop size but the mobile step and the `850` weight are outside the scale entirely, and `.sub`'s `min-height:30px` is a bare literal → extend the type scale rather than adding a fourth off-scale size. Recorded library-wide as `COMPONENTS.md` § Known inconsistencies #10.
- **The chart palette exists twice.** `trend-chart.ts:55-61` reads `--brand`/`--muted`/`--line` at runtime *and* hardcodes the same three hex values as SSR fallbacks, so a token change in `styles.scss` silently desynchronises the server-rendered first paint → derive the fallback from the token layer or drop SSR support for the chart. Already flagged in `Tokens/colors.md` § Normalize on redesign.
- **Accessibility gaps this screen carries.** The progress bars have no `role="progressbar"` and no `aria-value*` (`group-progress-list.html:5`), so the numbers are announced but the bars are invisible to assistive tech; the delta arrows are text glyphs, not labelled elements; the trend chart is a bare `<canvas>` with no accessible summary or data table alternative; and the whole dashboard module declares exactly **one** ARIA attribute pair (`period-toolbar.html:36`). Screen-reader users get no signal when the period changes because no region is `aria-live` → add live-region announcements on period change and accessible names to the chart and bars.
- **`CriteriaTable` is a third `p-table` shape that `Components/DataTable.md` does not describe.** That spec states "Two instances exist" and "Paging is entirely server-side"; this one is client-side (`[value]` is a pre-filtered array, no `[lazy]`, no `[totalRecords]`, filtering and sorting done in a `computed`, `criteria-table.ts:62-89`), has percentage widths instead of `min-width`s, no frozen columns and no loading mask. Not a defect of this screen — a gap in the component spec to close on the next stage-4 run.

<!-- Undocumented-component pointers — RESOLVED 2026-08-22 by COMPONENTS.md pass C.
     Kept as a record of what was open and how it closed; the blueprint above now cites
     the specs directly.
       - TrendChart      -> WRITTEN, Components/TrendChart.md. The app's only chart; its
                            palette was already tokenised in DESIGN.md (chart-series-1 /
                            -fill / chart-axis-label / chart-grid).
       - SegmentedControl-> WRITTEN, Components/SegmentedControl.md. The app's only view
                            switcher; the rival TabBar spec was deleted 2026-08-23 as
                            describing markup that never shipped.
       - Footer          -> WRITTEN, Components/Footer.md. Declared once at
                            styles.scss:486-500 (the dashboard.page.scss duplicate
                            was deleted 2026-08-22).
     Deliberately NOT given their own spec (decided, not pending):
       - .period-display (period-toolbar.scss:16-23) — the filter tier of Input.md with
         only background/color changed; a fifth, read-only input tier. Promote or fold
         into Input.md only if a second read-only-value chip appears.
       - .chart-skeleton (dashboard.page.scss:21-26) — the @defer placeholder box. Used
         TWICE (chart + history list), so it belongs to this page, not to TrendChart;
         documented inside Components/TrendChart.md as the @defer placeholder state.
     -->

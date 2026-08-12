---
project: "PlatformManager"
status: "draft"
updated: "2026-08-11"
flow: "DTI Weekly Dashboard"
screens: ["DTI Weekly Dashboard"]
source_routes: ["#dashboard"]
---

# DTI Weekly Dashboard — Screens

The DTI Weekly dashboard is PlatformManager's only shipped screen: a single-page app where a digital-transformation officer picks a reporting period, enters progress % for 62 criteria, reviews auto-computed KPIs/trend/deltas against the previous period, and generates a quick text report. There is no navigation between screens — the "report" is an in-page `Dialog` overlay of this same screen, not a separate route. This spec cross-references `spec/dashboard-dti-weekly/ui-spec.md` (§2 Layout, §3 Actions, §4 States, §5 Responsive, §6 UI↔ERD field map) and `spec/dashboard-dti-weekly/business-rules.md` alongside the live `doc/Prototype/dashboard.html` markup.

> **Shell:** dashboard — see `DESIGN.md` → Layout.
> **Sources:** `doc/Prototype/dashboard.html`, `spec/dashboard-dti-weekly/ui-spec.md`, `spec/dashboard-dti-weekly/business-rules.md`

---

## DTI Weekly Dashboard (`#dashboard`)

### Layout Blueprint

<!-- Region tree + structural measurements. Compose ONLY component names present in COMPONENTS.md. -->

- **Topbar** (`.topbar`, sticky top, `z-index:20`, inner `.topin` max-width `dimension.container-max-width` 1450px, `spacing.xl` padding)
  - Logo text block (plain text, not a component): "DTI Weekly" heading + subtitle
  - Actions row (`.actions`, `spacing.xs` gap)
    - `Button` (secondary, `desktop`-only modifier) — "Sao lưu"
    - `Button` (secondary, `desktop`-only modifier, `<label>` wrapping a hidden file `Input`) — "Khôi phục"
    - `Button` (primary) — "Lưu tuần này"
- **Main** (`max-width:1450px`, `spacing.xl` padding)
  - `NoticeBanner` — static instructional text
  - Weekbar region — `Card` (`no-print`)
    - Label text "Kỳ đang cập nhật:" (plain text, not a component)
    - `Input` (date variant, filter tier) — `#weekDate`
    - `Input` (select variant, filter tier) — `#savedWeeks`
    - `Button` (secondary) — "Tạo tuần mới từ kỳ gần nhất"
    - `Button` (secondary) — "Báo cáo nhanh"
  - KPI grid (`.kpis`, 5 columns desktop → 2 columns ≤`breakpoint.tablet`, `spacing.lg` gap)
    - `KpiTile` × 5: "Tiến độ chung tuần này", "So với tuần trước" (value uses `DeltaIndicator`-style up/down/neutral coloring), "Chỉ tiêu tăng", "Không tăng", "Hoàn thành 100%"
  - Groups + trend region (`.layout`, 2 columns `1.15fr .85fr` desktop → 1 column ≤`breakpoint.tablet`, `spacing.lg` gap)
    - `Card` — "Tiến độ theo nhóm"
      - Group row × 6 (`.group-row`: group-name text + `ProgressBar` + numeric `%` text, one per `CriteriaGroup`)
    - `Card` — "Biểu đồ tiến độ hàng tuần"
      - Trend chart — a hand-drawn `<canvas id="trend">` (`dimension.chart-height` 245px), **not a documented reusable component** — the app has no chart-library/token system (see `Tokens/colors.md` § Chart Palette: "None"); rendered imperatively by `renderTrend()`, not composed from `Components/`
  - Criteria table region — `Card`
    - Title text "62 chỉ tiêu DTI" + dynamic count text (plain text, not a component)
    - Filters row (`.filters`, `no-print`, `spacing.xs` gap)
      - `Input` (search variant, filter tier) — `#q`
      - `Input` (select variant, filter tier) — `#groupFilter`
      - `Input` (select variant, filter tier) — `#changeFilter`
      - `Input` (select variant, filter tier) — `#sortBy`
    - `Table` (`.tablewrap` + `table`, 9 columns, up to 62 rows) — each row composes: text cells + `Input` (progress-number variant, table tier) + `DeltaIndicator` + `Badge` + `Input` (note-text variant, table tier)
  - History region — `Card`
    - Title text "Lịch sử các kỳ đã lưu" + muted caption "Không ghi đè dữ liệu tuần cũ"
    - `HistoryRow` × N (newest first), or empty-state muted text when `N=0`
  - Footer text block (plain text, not a component)
- `Fab` — "Lưu tuần" (mobile-only, fixed bottom-right, only rendered ≤`breakpoint.tablet`)
- `Dialog` (`#reportDialog`) — "Báo cáo nhanh tiến độ DTI"
  - Title row: heading text + `Button` (secondary) — "Đóng"
  - Report body — generated text block (`#reportBox`), not a component
  - Action row (`spacing.xs` gap): `Button` (secondary) — "Sao chép", `Button` (primary) — "In"

### Copy

<!-- Verbatim shipped strings — typos and mixed languages included — with localization key and file:line source. -->

| Element | Verbatim copy | Localization key | Source |
| --- | --- | --- | --- |
| Browser tab title | `DTI Weekly - Theo dõi tiến độ chuyển đổi số` | — (hardcoded) | `dashboard.html:8` |
| Logo heading | `DTI Weekly` | — (hardcoded) | `dashboard.html:63` |
| Logo subtitle | `Theo dõi tiến độ chuyển đổi số · 62 chỉ tiêu` | — (hardcoded) | `dashboard.html:63` |
| Topbar button | `Sao lưu` | — (hardcoded) | `dashboard.html:65` |
| Topbar label/button | `Khôi phục` | — (hardcoded) | `dashboard.html:66` |
| Topbar primary button | `Lưu tuần này` | — (hardcoded) | `dashboard.html:67` |
| Notice banner | `Mỗi tuần chọn ngày báo cáo, cập nhật **Tiến độ %** của từng chỉ tiêu rồi bấm **Lưu tuần này**. Hệ thống tự so với kỳ gần nhất trước đó và hiển thị **tăng/giảm bao nhiêu điểm %**.` | — (hardcoded) | `dashboard.html:74-75` |
| Weekbar label | `Kỳ đang cập nhật:` | — (hardcoded) | `dashboard.html:79` |
| Saved-period select, default option | `— Chọn kỳ đã lưu —` | — (hardcoded) | `dashboard.html:81` |
| Weekbar button | `Tạo tuần mới từ kỳ gần nhất` | — (hardcoded) | `dashboard.html:82` |
| Weekbar button | `Báo cáo nhanh` | — (hardcoded) | `dashboard.html:83` |
| KPI 1 label / sub | `Tiến độ chung tuần này` / `Bình quân gia quyền theo điểm` | — (hardcoded) | `dashboard.html:87` |
| KPI 2 label / default value / default sub | `So với tuần trước` / `—` / `Chưa có kỳ trước` | — (hardcoded) | `dashboard.html:88` |
| KPI 3 label / sub | `Chỉ tiêu tăng` / `Có tiến bộ so với kỳ trước` | — (hardcoded) | `dashboard.html:89` |
| KPI 4 label / sub | `Không tăng` / `Cần chú ý theo dõi` | — (hardcoded) | `dashboard.html:90` |
| KPI 5 label / default value / sub | `Hoàn thành 100%` / `0/62` / `Số chỉ tiêu đạt đủ tiến độ` | — (hardcoded) | `dashboard.html:91` |
| Groups card title / caption | `Tiến độ theo nhóm` / `Tuần hiện tại` | — (hardcoded) | `dashboard.html:96` |
| Trend card title / caption | `Biểu đồ tiến độ hàng tuần` / `Tiến độ chung` | — (hardcoded) | `dashboard.html:100` |
| Trend chart empty-state text | `Lưu ít nhất một kỳ để xem biểu đồ.` | — (hardcoded) | `dashboard.html:907` |
| Table card title | `62 chỉ tiêu DTI` | — (hardcoded) | `dashboard.html:106` |
| Table count text (dynamic) | `<n>/62 chỉ tiêu` | — (hardcoded, JS template `${arr.length}+'/62 chỉ tiêu'`) | `dashboard.html:883` |
| Search placeholder | `Tìm mã hoặc tên chỉ tiêu...` | — (hardcoded) | `dashboard.html:108` |
| Group filter, default option | `Tất cả nhóm` | — (hardcoded) | `dashboard.html:109` |
| Change filter options | `Tất cả mức thay đổi` / `Chỉ tiêu tăng` / `Không tăng` / `Giảm` / `Hoàn thành` | — (hardcoded) | `dashboard.html:110-112` |
| Sort options | `Theo mã chỉ tiêu` / `Tăng nhiều nhất` / `Tiến độ thấp nhất` | — (hardcoded) | `dashboard.html:113-115` |
| Table column headers | `Mã` / `Chỉ tiêu` / `Nhóm` / `Điểm tối đa` / `Tuần trước` / `Tuần này` / `Tăng/giảm` / `Trạng thái` / `Ghi chú tuần` | — (hardcoded) | `dashboard.html:120-121` |
| Note input placeholder | `Nội dung đã làm / vướng mắc...` | — (hardcoded) | `dashboard.html:893` |
| Badge text (3 values) | `Hoàn thành` / `Đang thực hiện` / `Không tăng` | — (hardcoded) | `dashboard.html:868-870` |
| History card title / caption | `Lịch sử các kỳ đã lưu` / `Không ghi đè dữ liệu tuần cũ` | — (hardcoded) | `dashboard.html:129` |
| History empty-state text | `Chưa có tuần nào được lưu.` | — (hardcoded) | `dashboard.html:902` |
| History row "view" button | `Xem` | — (hardcoded) | `dashboard.html:901` |
| Footer note | `Dữ liệu được lưu trên trình duyệt bằng LocalStorage. Nên dùng nút "Sao lưu" định kỳ để tải file JSON dự phòng.` | — (hardcoded) | `dashboard.html:133` |
| Fab label | `Lưu tuần` | — (hardcoded) | `dashboard.html:136` |
| Dialog title | `Báo cáo nhanh tiến độ DTI` | — (hardcoded) | `dashboard.html:139` |
| Dialog close button | `Đóng` | — (hardcoded) | `dashboard.html:139` |
| Dialog action buttons | `Sao chép` / `In` | — (hardcoded) | `dashboard.html:142-143` |
| Report heading (generated) | `BÁO CÁO NHANH TIẾN ĐỘ CHỈ SỐ CHUYỂN ĐỔI SỐ` | — (hardcoded, JS template) | `dashboard.html:920` |
| Report body (generated, verbatim template) | `Kỳ cập nhật: <b><date></b>.` / `Tiến độ chung hiện đạt <b><pct>%</b>[, tăng/giảm <b><delta> điểm %</b> so với kỳ <date>]. Có <b><n> chỉ tiêu tăng</b>, <b><n> chỉ tiêu không thay đổi</b>, <b><n> chỉ tiêu giảm</b> và <b><n>/62 chỉ tiêu hoàn thành 100%</b>.` / `Chỉ tiêu tăng nhiều: <list hoặc "Chưa có">.` / `Chỉ tiêu chưa tăng cần chú ý: <list hoặc "Không có">.` | — (hardcoded, JS template) | `dashboard.html:920-924` |
| Save confirmation alert | `Đã lưu tiến độ ngày <dd/mm/yyyy>.` | — (hardcoded) | `dashboard.html:843` |
| Copy confirmation alert | `Đã sao chép báo cáo.` | — (hardcoded) | `dashboard.html:927` |
| Restore success alert | `Khôi phục dữ liệu thành công.` | — (hardcoded) | `dashboard.html:933` |
| Restore error alert | `File sao lưu không hợp lệ.` | — (hardcoded) | `dashboard.html:933` |

### States

<!-- How each state renders: default / loading / empty / error / validation display. -->

- **default (has data entered, no previous period saved yet):** first-load state before any `AssessmentPeriod` has been saved. `#kDelta` shows `—`, `#prevLabel` shows "Chưa có kỳ trước"; `#kFlat` shows `—` (not `0`) since flat/up/down comparisons need a previous period; the "Tuần trước" and "Tăng/giảm" table columns show `—` for every row; every badge is either "Đang thực hiện" or "Hoàn thành" (never "Không tăng", which requires a defined delta); trend chart renders the empty-state text instead of a line; `#history` renders its empty-state text; `#savedWeeks` has only the default placeholder option. See screenshot `dashboard--desktop-1440.png`. (`ui-spec.md` §4 "Empty — chưa có kỳ nào được lưu"; `dashboard.html:851-857,867-871,904-907,897-903`)
- **has-previous-period (non-empty):** once at least one prior `AssessmentPeriod` exists, `#kDelta`/`#kFlat`/table "Tăng/giảm"/history deltas all populate with real up/down/flat values (epsilon `0.001`, see `business-rules.md` §3.2); the trend `<canvas>` draws a line/points per saved period (up to the 12 most recent); badges can now show all 3 values including "Không tăng". See screenshot `dashboard--with-history--desktop-1440.png`.
- **loading:** **none exists** — all computation is synchronous over in-memory/`localStorage` data; there is no network call anywhere in the app, so no spinner/skeleton state is ever shown (`ui-spec.md` §4).
- **empty (filtered table with zero matches):** `#countText` still updates correctly to `0/62 chỉ tiêu`; `tbody` renders as an empty string — there is **no** "no results found" message, just a visually empty table body (`ui-spec.md` §4, `dashboard.html:883-884`).
- **validation:** progress % input has **no visible validation state** — out-of-range or non-numeric input is silently clamped to `[0,100]` on `change` (`setProgress()`, `dashboard.html:832-834`); no inline error text, no red border, no toast. The **only** validation feedback anywhere in the app is the post-hoc `alert('File sao lưu không hợp lệ.')` when "Khôi phục" is given a malformed/incompatible JSON file (`dashboard.html:933`).
- **error:** no other error states exist — no 404/500 equivalent (single static file, no server), no network-failure UI (no network calls).
- **report dialog open:** `dialog#reportDialog` shown via native `showModal()` with `::backdrop` dimming; content is fully regenerated from current `draft`/`historyData` each time "Báo cáo nhanh" is clicked, so it always reflects the same state as the underlying dashboard at that moment. See screenshot `report-dialog--desktop-1440.png`.

### Responsive

<!-- Behavior per breakpoint. -->

- **≥`breakpoint.tablet` (980px, desktop default):** `.kpis` = 5-column grid; groups/trend `.layout` = 2 columns (`1.15fr .85fr`); `.group-row` = `230px 1fr 90px`; topbar shows "Sao lưu"/"Khôi phục" (`.actions .desktop`); `Fab` hidden (`dashboard.html:27,31,36`).
- **<`breakpoint.tablet` (≤980px):** `.kpis` collapses to 2 columns; `.layout` collapses to 1 column (groups panel stacks above trend panel); `.group-row` becomes `140px 1fr 75px`; `.actions .desktop` ("Sao lưu"/"Khôi phục") is **hidden entirely with no mobile equivalent** (see Normalize on redesign); `Fab` ("Lưu tuần") appears fixed bottom-right as the mobile save entry point (`dashboard.html:55`).
- **<`breakpoint.mobile` (≤560px):** `main`/`.topin` padding drops to `10px`; logo heading drops to `16px`; `.kpis` gap drops to `8px`, `Card` padding drops to `12px`, `KpiTile` value drops to `22px`; the **last** KPI tile ("Hoàn thành 100%") spans the full row width (`grid-column:1/-1`) since 5 tiles don't divide evenly into 2 columns; weekbar children (`Input`s + `Button`s) each stretch `flex:1` instead of sizing to content; `.group-row` becomes `110px 1fr 68px`; the section `.title` row switches from `align-items:center` to `flex-start` so long headings wrap without misaligning the trailing caption (`dashboard.html:56`).
- **Table (all viewports):** `Table` has **no breakpoint of its own** — it stays at `dimension.table-min-width` (1200px) and scrolls horizontally via `.tablewrap{overflow:auto}` at every screen size, including mobile (`dashboard.html:40`; see screenshots — the mobile capture below the table's scroll boundary shows the same fixed-width table, not a collapsed/stacked card layout).
- **Print (`@media print`):** `.topbar`, `.filters`, `Fab`, and every `.no-print`-tagged region (weekbar) are hidden; `.layout` collapses to 1 column; `Card` loses its shadow; `.tablewrap` stops clipping overflow and `table` drops its `min-width` so the full table prints without horizontal scrolling (`dashboard.html:57`). Triggered by the dialog's "In" button (`window.print()`, `dashboard.html:143`).

### Iconography

See `Icons.md` § Per-Action Map — this screen is the **only** screen in the app and every mapped action belongs to it. Summary: **no icon library** is loaded anywhere; every action is a plain text `Button`/`Input`/`Select`, and the only directional cues are literal `↑`/`↓` glyphs inline in `DeltaIndicator` text (see `Components/DeltaIndicator.md`).

| Action | Icon | Placement |
| --- | --- | --- |
| All 18 actions (period select, save, export/import, filter/sort, report dialog, etc.) | — (text button/native control, no icon) | See `Icons.md` for the full per-action table |

### Screenshots

<!-- Refs into Assets/Screenshots/01-dashboard/ -->

- `Assets/Screenshots/dashboard/dashboard--desktop-1440.png` — default/first-load state (no previous period), desktop 1440px
- `Assets/Screenshots/dashboard/dashboard--tablet-900.png` — same default state at the `breakpoint.tablet` boundary (900px, 2-col KPI grid, `.actions .desktop` hidden)
- `Assets/Screenshots/dashboard/dashboard--mobile-390.png` — same default state at `breakpoint.mobile` (390px, stacked layout, `Fab` visible)
- `Assets/Screenshots/dashboard/dashboard--with-history--desktop-1440.png` — has-previous-period state: populated deltas, trend line, mixed badge variants, desktop 1440px
- `Assets/Screenshots/dashboard/report-dialog--desktop-1440.png` — `Dialog` open on top of the has-previous-period state, desktop 1440px

<!-- Capture note: the two "with-history"/"report-dialog" screenshots required seeding one prior AssessmentPeriod into localStorage before load — see UiInventory.md → Screenshot Manifest for the exact reproducible seed snippet; this does not modify doc/Prototype/dashboard.html itself. -->

### Normalize on redesign

<!-- Screen-local quirks ONLY here — sections 1-6 stay as-shipped. Library-wide issues go to COMPONENTS.md → Known inconsistencies. -->

- No confirmation step before "Lưu tuần này" overwrites an existing period on the same date, or before "Khôi phục" replaces all data — both actions only `alert()` **after** completing (`dashboard.html:843,933`) → add a pre-action confirm for both, especially "Khôi phục" which is fully destructive.
- Clicking the `dialog#reportDialog` `::backdrop` does **not** close it (no click listener wired to the backdrop, only the "Đóng" button and native ESC key work) → either wire backdrop-click-to-close for consistency with common modal conventions, or intentionally document it as "must use Đóng/Esc" if kept.
- "Sao lưu"/"Khôi phục" have no reachable equivalent below 980px (see `UiInventory.md` § Normalize on Redesign #2 — project-wide) → this screen specifically loses **all** export/import capability on mobile, not just a visual variant; needs a mobile entry point (e.g. overflow menu) before this becomes a real mobile-usable screen.
- Table search/filter/sort controls (`.filters`) are also hidden under `@media print` (correctly, as controls shouldn't print) but there's no printed summary of which filter was active when "Báo cáo nhanh" → "In" was used — a printed report from a filtered view could look identical to an unfiltered one since the criteria table itself isn't part of the dialog/print output.

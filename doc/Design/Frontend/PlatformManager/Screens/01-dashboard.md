---
kind: luat
scope: du-an
verified: khong-ap-dung
project: "PlatformManager"
status: "target — not built"
updated: "2026-09-11"
flow: "DTI Dashboard"
screens: ["DTI Dashboard"]
source_routes: ["/trang-chu"]
---

# DTI Dashboard — Screens

> # 📐 ĐÍCH ĐẾN — CHƯA THI CÔNG
>
> **The screen is not built. Part of what it composes now is.**
> 🔄 **SỬA 2026-09-10.** The previous revision said *"This screen does not exist
> in `src/FE` … the rebuild has not started"* and told the reader to expect no
> `src/FE/src/app/modules` directory at all. That stopped being true on 2026-09-09.
> What is on disk, checked 2026-09-10:
>
> | Built | Still to build |
> | --- | --- |
> | `src/FE/src/app/modules/dashboard/components/` — `kpi-tile`, `progress-bar`, `trend-chart`, `history-row`, each with a `.spec.ts` beside it | every region that composes them: the period toolbar, the KPI row, the two-column region, the detail table, the history panel |
> | `dashboard.page.ts`, a **deliberate skeleton** carrying a title and one "under construction" line (`src/FE/src/app/modules/dashboard/pages/dashboard/dashboard.page.ts:8-10`) | the page itself |
> | `dashboard.routes.ts`, **deliberately not wired into `app.routes.ts`** — swapping the app's safe-landing route for an empty page is the thing being avoided (`src/FE/src/app/modules/dashboard/dashboard.routes.ts:8-16`) | the swap onto `/trang-chu` (Q3 / Q18) |
>
> Everything below is still a **design to be built**, approved by the product owner
> on 2026-09-04 and 2026-09-05, not a record of a running screen. Check rather than
> trust the table:
>
> ```bash
> grep -cE '^\s+loadChildren:' src/FE/src/app/app.routes.ts   # routed screens today
> ls src/FE/src/app/modules/dashboard/components              # the four built components
> grep -n 'CHƯA KHAI' src/FE/src/app/modules/dashboard/dashboard.routes.ts
> ```
>
> **`file:line` citations into `src/FE` are now possible for the component layer**
> and are used where they hold. For the regions and the page, which do not exist,
> none appears — there is nothing to point at. Core elements that ship (the app
> shell, the global component layer) are cited by **identifier**, per
> `doc/Design/CLAUDE.md` § Neo trích dẫn vào `styles.scss`.
>
> **This file replaces a historical one.** The previous revision carried a
> historical-document banner describing the `/dashboard` screen as it shipped
> before 2026-08-29; read that revision in git history (the code it describes was
> live at commit `98a5d96`) if you need the pre-retirement design. It is superseded
> here rather than kept, because the two would otherwise be two answers to one
> question — `.claude/CLAUDE.md` §5.

The Dashboard is the product's **read-only** analytical surface and, per decisions
Q3 and Q18, its **landing screen at the existing URL `/trang-chu`**. It does not
get a route of its own: it replaces the welcome page currently rendered there,
**in place**. Everything that points at that URL therefore stays exactly as it is
— the `''` redirect, the `**` wildcard, `roleGuard`'s fallback target, the
`APP_CORE_ROUTES.home` constant, and the seeded sidebar menu row. Nothing in
`src/FE/src/app/app.routes.ts` changes shape; the route's `loadChildren` target
changes what it loads.

Two consequences follow from sitting at that URL, and the rest of this spec is
shaped by them:

- **The route has no role guard and cannot have one** — `roleGuard` redirects a
  role-less user *here*, so guarding it would loop
  (`src/FE/src/app/core/auth/role.guard.ts`). Per decision Q21 that is fine:
  **every signed-in account may see the Dashboard**, no DTI permission required.
  There is therefore **no "you lack permission" state to design**. Write access on
  the catalogue screen is a separate question and is still open
  ([`02-danh-muc-dti.md`](./02-danh-muc-dti.md) § Cần chốt).
- **A brand-new deployment lands here with nothing in it.** That state is not
  optional and is specified in § States.

**What happens to the old welcome screen** ([`06-trang-chu.md`](./06-trang-chu.md)) —
🚧 **ĐÃ CHỐT — ĐANG THI CÔNG.** Decision **Q29**, 2026-09-05: the folder
`src/FE/src/app/platform/trang-chu/` is **deleted outright**, rather than being kept
beside the Dashboard or having business content written into it. Its content is not
relocated either — the account summary it showed (username, email, role badges, the
change-password link) goes with it, leaving the sidebar menu as the only route to
`/doi-mat-khau`. That screen spec now carries a historical-document banner.

The code is still there as this is written; the decision is made, the deletion is
not. Check rather than assume: `ls src/FE/src/app/platform/trang-chu`.

Two consequences the build inherits:

- **The URL survives, the folder does not.** `/trang-chu` stays the landing route,
  `APP_CORE_ROUTES.home` still resolves to it
  (`src/FE/src/app/core/config/core-routes.ts` § `home`), the `''` redirect, the `**`
  wildcard and the seeded menu row are all untouched, and the route keeps its test
  cover. Only the module behind the URL changes.
- **The next product built on this CoreBase inherits no home screen.** That folder is
  the only Core-level landing page there is; the Dashboard replacing it is *business*
  code and does not travel with CoreBase. A second product reusing the base therefore
  has to write its own landing screen. That is a real, accepted cost of Q29, recorded
  here because no other document in the design area would record it.

A digital-transformation officer picks a reporting period, then reads: five KPI
tiles, per-group progress bars, a trend line, the full criteria table for that
period, and the list of saved periods. **Nothing on this screen writes data.** The
single action is `Xuất báo cáo`, which per decision Q13 downloads an `.xlsx` file
directly — no dialog, no preview. Every write lives on the DTI catalogue
([`02-danh-muc-dti.md`](./02-danh-muc-dti.md)).

> **Shell:** the app shell — skip link + `Sidebar` + `Topbar` + `main` + `Toast`
> (`src/FE/src/app/app.html:18-41`), rendered because this route will not set
> `data.noShell`. `../DESIGN.md` → Layout describes this shell.
> **Sources:** `doc/Design/Frontend/PlatformManager/Prototypes/index.html`
> § `#screen-dashboard` — the prototype the product owner approved point by point on
> 2026-09-04 and 2026-09-05, the **only** source for this screen's layout and copy,
> and the same file that declares the component CSS it reuses. **Repointed
> 2026-09-10**: this line used to name the prototype twice, once by a path that lived
> **outside** the repo, so no second reader could open the half that mattered
> (`doc/Design/Frontend/PlatformManager/Prototypes/README.md` § In-repo master).
> **Every figure on this screen is read off that prototype.** They were computed from
> the BA's August 2026 spreadsheet, which is deliberately outside this repo (root `.gitignore`), so it is named here and not cited as a path. The in-repo sample
> `spec/danh-muc-dti/dti-mau-an-danh-62-dong.csv` is a **different** 62-row dataset — it backs shape claims in this
> folder, never these figures.
> `src/FE/src/styles.scss` and `src/FE/src/app/shared/components/{sidebar,topbar,toast}/`
> for the live Core layer this screen composes;
> `doc/Design/Frontend/PlatformManager/Prototypes/mau-xuat-bao-cao_Tuan-33-2026.xlsx` and
> `doc/Design/Frontend/PlatformManager/Prototypes/mau-xuat-bao-cao_Thang-8-2026.xlsx` for the export layout — anonymised
> copies built 2026-09-10 by rewriting **only** the data rows of the originals, so the
> sheet name, the identification block, the column widths, the header fill and the
> `TỔNG CỘNG` row are the originals'.
> API contract → `doc/contracts/dashboard.md`; business rules →
> `spec/dashboard-dti/business-rules.md` (both being rewritten in parallel on
> 2026-09-05 — if either disagrees with this file, that is a conflict to raise,
> not to resolve silently).
> **Token vocabulary:** token names are the live CSS custom properties in
> `src/FE/src/styles.scss` § `:root` minus the `--` prefix. Values quoted without
> a token name are literals in the approved prototype, recorded as such.

---

## DTI Dashboard (`/trang-chu`)

### Layout Blueprint

<!-- Region tree + structural measurements. Compose ONLY component names present in COMPONENTS.md. -->

- **App shell** (`src/FE/src/app/app.html:18-41`) — surrounds the route; not part of its own template
  - Skip link, then `Sidebar`, then `.shell-content` → `Topbar` → `main#main-content`, then `Toast` outside the shell conditional
  - `main` is capped at `container-max-width` with `spacing.sp-5` padding; the regions below are its direct children
- **Period toolbar** — `app-period-toolbar` → `<section class="toolbar no-print">`, the screen's first region and its primary control. `Toolbar` supplies the surface, so **no `.card` wrapper** (two stacked surfaces would give the bar a shadow and a second padding). Left to right:
  - `<strong>Kỳ đang xem:</strong>` then `.period-display` — a **read-only** value box, not an `Input`: 1px `colors.border-strong` border, `rounded.sm`, fill `colors.bg`, ink `colors.muted`, padding `spacing.sp-2` `spacing.sp-3`, `fontSize.fs-sm`, `white-space: nowrap` so the full period label never wraps (`doc/Design/Frontend/PlatformManager/Prototypes/index.html` § `app-period-toolbar .period-display`)
  - Year `<select>`, then **one** period `<select>` — the week list or the month list, never both, so the toolbar cannot gain a second row
  - `SegmentedControl` (`.segmented` / `.seg-btn`, `role="group"`) — `Tuần` / `Tháng`
  - `.toolbar-sep` (the flexible gap), then `.toolbar-actions` holding one `Button` `.btn.primary` — `Xuất báo cáo`
  - **This toolbar has no `Lọc` control.** Choosing a period is the page's primary act, not a filter condition; filtering belongs to the detail table's own toolbar further down
- **KPI row** — `app-kpi-summary` → `<section class="kpis">`, a `repeat(5, 1fr)` grid with `spacing.sp-4` gap, holding five `KpiTile`. Contents and tones in [`../Components/KpiTile.md`](../Components/KpiTile.md) § The five tiles
- **Two-column region** — `.layout`, a `1.15fr 0.85fr` grid with `spacing.sp-5` gap and `margin-top: spacing.sp-5`; each child is a `Card` laid out as a flex column so its body fills the remaining height (§ `#screen-dashboard .layout`)
  - **Left card** — `.title` row (`<h2>Tiến độ theo nhóm</h2>` + `<span class="muted">Tuần hiện tại</span>`) over `app-group-progress-list`: six `ProgressBar` rows, one per criteria group. See [`../Components/ProgressBar.md`](../Components/ProgressBar.md)
  - **Right card** — `.title` row (`<h2>Biểu đồ tiến độ hàng tuần</h2>` + `<span class="muted">Tiến độ chung</span>`) over `TrendChart`, lazy-loaded behind a viewport boundary with a `.chart-skeleton` placeholder reserving its full height. See [`../Components/TrendChart.md`](../Components/TrendChart.md), which since decision T7 on 2026-09-05 has **nothing open** — the last question, the month-mode x axis, is settled. That hand-off is closed too: `chart.js` returned to `src/FE/package.json` on 2026-09-09 (`src/FE/package.json:68` — `^4.5.0`) and `TrendChart` is built against it, importing PrimeNG's `ChartModule` and `chart.js` types at `src/FE/src/app/modules/dashboard/components/trend-chart/trend-chart.ts:4-5` (checked 2026-09-10). What is missing is the region that hosts the component, not its dependency — an earlier revision of this line still asked for the package to be added back. **Week mode plots twelve weeks, ending at the week being viewed** — 🔄 LẬT 2026-09-10 (Q54); the approved prototype draws six. The twelve tick labels, and the rule for fitting them on the axis (duyệt 2026-09-10, Q54), are in [`../Components/TrendChart.md`](../Components/TrendChart.md) § The x axis changed on 2026-09-05
- **Detail table** — `app-criteria-table` → `<section class="card criteria-table-card">` with `margin-top: 16px` (a literal, § `app-criteria-table .criteria-table-card`)
  - `.title` row — `<h2>62 chỉ tiêu DTI</h2>` + `<span class="muted">62/62 chỉ tiêu</span>`. The right-hand count is the same contract the Core user grid uses; see [`02-danh-muc-dti.md`](./02-danh-muc-dti.md) § Layout Blueprint for why it stays
  - `Toolbar` — **the same contract as the catalogue screen**, no new controls, and in the DOM order the shipped component actually renders (`src/FE/src/app/shared/components/toolbar/toolbar.html`): `.input-icon.search` (fixed 260px) → `<details class="filter">` → `.toolbar-sep` → `.filter-chips` → `.toolbar-actions`. The filter panel holds **two** `.form-row` conditions — `Nhóm chỉ tiêu` and `Trạng thái` — over a `.filter-foot`; the sort `<select>` sits in `.toolbar-actions`, because sorting is not a filter condition. The approved state shows **no** `.filter-count` and **no** `.filter-chip`, matching the unfiltered `62/62 chỉ tiêu` in the title.
    > Decision **Q22** removed the third condition (`Mức thay đổi so với kỳ trước`) and two of the four sort options (`Tăng nhiều nhất`, `Tiến độ thấp nhất`). Sorting now offers `Theo mã chỉ tiêu` and `Chênh lệch lớn nhất` — the two that have a column behind them. The prototype still draws all three conditions and all four options; **the prototype is the stale side**, see § Normalize on redesign.
  - **`Table`, not `DataTable`** (decision T4) — a plain table inside `.tablewrap`, scrolled at `--grid-h` / `--grid-h-min`, with **no paginator**, no lazy loading and no rows-per-page control. This region is read-only and shows the whole period at once, so it needs none of the `p-table` mechanism that [`../Components/DataTable.md`](../Components/DataTable.md) documents. Only the catalogue screen uses `DataTable`. Keeping them apart is deliberate: a second `DataTable` variant that merely turns paging off would put two grid mechanisms in the library for one job. **9 columns**, per decision Q8:

    | # | Header | Alignment | Content |
    | --- | --- | --- | --- |
    | 1 | `Mã` | left, 5% | bold criteria code — three levels are possible (`4.22.11`), not just two |
    | 2 | `Chỉ tiêu` | left, 26% | full criterion name |
    | 3 | `Nhóm` | left, 13% | group label as `Code. Name`, e.g. `1. Hạ tầng và Nền tảng số` — decision **Q42**, 2026-09-10 (§ Copy → Group names) |
    | 4 | `Điểm tối đa` | `.num`, 8% | integer |
    | 5 | `Tự đánh giá` | `.num`, 9% | decimal, Vietnamese comma |
    | 6 | `Thẩm định` | `.num`, 9% | decimal |
    | 7 | `Chênh lệch` | `.num`, 9% | `DeltaIndicator` — **computed**, never stored: `Thẩm định − Tự đánh giá` (decision Q25 — positive green, negative red, zero grey; the direction and its history are owned by [`../Components/DeltaIndicator.md`](../Components/DeltaIndicator.md)) |
    | 8 | `Trạng thái` | left, 10% | `Badge`, colour-mapped per decision Q10 |
    | 9 | `Minh chứng/Ghi chú` | left, 11% | free text, or `<span class="muted">—</span>` when empty |

    The three week-comparison columns of the pre-retirement design (`Tuần trước`, `Tuần này`, `Tăng-giảm`) are **gone**. Period-over-period movement is carried by the trend chart and by KPI tiles 3 and 4 instead, so the table does not repeat it. Column count is unchanged at 9; the set is not
- **History panel** — `<section class="card history-card">` with `margin-top: 16px` (§ `#screen-dashboard .history-card`); `.title` row (`<h2>Lịch sử các kỳ đã lưu</h2>` + `<span class="muted">Không ghi đè dữ liệu tuần cũ</span>`) over `app-history-list`: one `HistoryRow` per saved period, newest first. See [`../Components/HistoryRow.md`](../Components/HistoryRow.md)
- **`Footer`** — one closing line pointing at the catalogue screen

<!-- Component gap — reviewed 2026-09-05. Every region composes from an indexed spec:
       Toolbar -> Components/Toolbar.md          SegmentedControl -> Components/SegmentedControl.md
       Button  -> Components/Button.md           KpiTile          -> Components/KpiTile.md
       Card    -> Components/Card.md             ProgressBar      -> Components/ProgressBar.md
       Table   -> Components/Table.md            TrendChart       -> Components/TrendChart.md
       Badge   -> Components/Badge.md            DeltaIndicator   -> Components/DeltaIndicator.md
       Input   -> Components/Input.md            HistoryRow       -> Components/HistoryRow.md
       FormRow -> Components/FormRow.md          Footer           -> Components/Footer.md
       NoticeBanner -> Components/NoticeBanner.md
       Sidebar / Topbar / Toast -> their own specs.
     NOT used here: DataTable. Decision T4 — this screen's table is read-only and
     unpaged, so it composes Table alone. DataTable belongs to 02-danh-muc-dti.md.
     Genuinely screen-local and deliberately NOT promoted: `.period-display` (a
     read-only value box, one instance, no states, no variants), `.kpis` (a grid
     container belonging to KpiTile's row), `.layout` (a two-column page grid) and
     `.chart-skeleton` (a defer placeholder shared with the history panel).
     Promote `.period-display` only if a second read-only value box appears. -->

### Copy

<!-- Verbatim shipped strings — typos and mixed languages included — with localization key and file:line source. -->

The app HAS an i18n layer: `@ngx-translate/core` v18 reading **two** key groups split by
folder — Core at `src/FE/public/i18n/`, project at `src/FE/public/i18n-app/`
(`doc/huong_dan/wiki-core/fe/08-i18n.md` § Khuôn CoreBase). This screen is business, so its
keys belong to the **project** group. Every string below is therefore the
**Vietnamese rendering of a key that must be allocated when this screen is built** — the
Localization key column reads `key TBA` because this screen has never shipped, not because the
copy may be inlined. Pasting a Vietnamese literal into a template fails the build:
`scripts/fe-gate.sh` § G12 scans every `.html` for Vietnamese diacritics.

```bash
bash scripts/fe-gate.sh
```

PASS = the G12 section reports no hits. Each row below needs a `dashboard.*` key in
`src/FE/public/i18n-app/vi.json` and an English sibling in `src/FE/public/i18n-app/en.json`
before the markup can pass.

🔄 **SỬA 2026-09-08.** The previous revision of this paragraph said *"No i18n layer exists in
the app"* and instructed hardcoding. It was already false when written — the i18n layer landed
2026-09-05 — and following it would have failed G12.

🔄 **SỬA 2026-09-10.** The two paragraphs above named the **Core** bundle
`src/FE/public/i18n/vi.json`. The project bundle landed 2026-09-09 and already carries a
`dashboard` group (`src/FE/public/i18n-app/vi.json:9-28`), so a key added to the Core bundle
would go in the wrong file. The rows below still read `key TBA`: the only keys allocated so far
belong to the skeleton page, not to this design.

Source for every DTI row is
`doc/Design/Frontend/PlatformManager/Prototypes/index.html` § `#screen-dashboard`; the region is named instead of a line
number, because line numbers in a 5500-line prototype renumber silently.

| Element | Verbatim copy | Localization key | Source region |
| --- | --- | --- | --- |
| Period toolbar label | `Kỳ đang xem:` | key TBA | `app-period-toolbar` |
| Period value, week mode | `Tuần 33/2026 (10/08 – 16/08/2026)` | — (composed) | `.period-display` |
| Period value, month mode | `Tháng 8/2026 (01/08 – 31/08/2026)` | — (composed) | `app-period-toolbar` comment, approved 2026-09-05 |
| Year select, accessible name | `Chọn năm` | key TBA, on `title` + `aria-label` | `app-period-toolbar` |
| Week select, accessible name | `Xem tổng hợp cả năm hoặc 1 tuần cụ thể` / `title` `Chọn 1 tuần cụ thể hoặc xem Tất cả` | key TBA | `app-period-toolbar` |
| Week select — current-period option | `— Kỳ hiện tại —` | key TBA | `app-period-toolbar` |
| Week select — all-periods option | `— Tất cả (tổng hợp theo năm) —` | key TBA | `app-period-toolbar` |
| Week select — one option | `Tuần 33 · 10/08 – 16/08 · 82,1%` | — (composed) | `app-period-toolbar` |
| Month select — current-period option | `— Tháng hiện tại —` | key TBA | `app-period-toolbar` |
| Month select — one option | `Tháng 8 · 01/08 – 31/08 · 81,0%` | — (composed) | `app-period-toolbar` |
| View-mode switch | `Tuần` · `Tháng` | key TBA | `.segmented` |
| View-mode switch, group name | `Chế độ xem theo Tuần hoặc Tháng` | key TBA, on `aria-label` | `.segmented` |
| Export button | `Xuất báo cáo` | key TBA | `.toolbar-actions` |
| Export button `title`, no filter applied | `Tải file Excel (.xlsx) của kỳ đang xem — Tuần 33/2026 (10/08 – 16/08/2026)` | — (composed) | `.toolbar-actions` |
| Export button `title`, **filters applied** | `Tải file Excel (.xlsx) — CHỈ các chỉ tiêu đang lọc (7/62)` | — (composed) | `.toolbar-actions` — settled 2026-09-06 |
| "All periods" chip, when that option is chosen | `Tất cả · 2026` in a `.badge.warn` | — (composed) | `app-period-toolbar`, currently a disabled branch |
| KPI labels and captions | five rows; **tiles 1 and 2 change label with the period mode** (decision Q52, 2026-09-10) | key TBA | see [`../Components/KpiTile.md`](../Components/KpiTile.md) § The five tiles and § Tiles 1 and 2 follow the period mode — that file is the master for both |
| KPI values in the post-import state | `—` · `—` · `0` · `0` · `26/62` (tiles 1–5, in order) | — (composed) | decision T12; the labels stay as above, only the values change. § States |
| First-run banner — nothing imported | `Chưa có dữ liệu DTI nào. Vào Danh mục DTI để nhập file hoặc thêm chỉ tiêu đầu tiên.` (`Danh mục DTI` is the link) | key TBA | **Written by this spec 2026-09-05; not drawn in the prototype.** § States |
| Post-import banner — no `Tiến độ %` yet | `Đã có 62 chỉ tiêu, nhưng chưa chỉ tiêu nào có Tiến độ %. Thanh tiến độ theo nhóm và biểu đồ sẽ hiện ngay khi có số liệu. Nhập Tiến độ % tại Danh mục DTI.` (count composed; `Danh mục DTI` is the link) | — (composed) | **Written by this spec 2026-09-05 for decision Q32; not drawn in the prototype.** § States |
| Loading — accessible name on the busy overlay | `Đang tải số liệu…` | key TBA, on `aria-label` | **Written by this spec 2026-09-05 for decision Q34; not drawn in the prototype.** § States |
| Group panel heading | `Tiến độ theo nhóm` | key TBA | `.layout` left card |
| Group panel caption | `Tuần hiện tại` | key TBA | `.layout` left card |
| Group names | `1. Hạ tầng và Nền tảng số` · `2. Nhân lực số` · `3. An toàn thông tin, an ninh mạng` · `4. Hoạt động chính quyền số` · `5. Hoạt động Kinh tế số` · `6. Hoạt động Xã hội số` | — (composed from the server's `Code` + `Name`) | `app-group-progress-list`. **Decision Q42, 2026-09-10** confirms this form: `Code. Name`, where `Code` is `"1"`…`"6"` in the order the template file lists the groups and `Name` is that file's `Nhóm` string with no prefix. The in-repo sample `spec/danh-muc-dti/dti-mau-an-danh-62-dong.csv` carries exactly these six names, unprefixed and in this order (measured 2026-09-10) |
| Chart panel heading | `Biểu đồ tiến độ hàng tuần` | key TBA; reads `hàng tháng` in month mode | `.layout` right card |
| Chart panel caption | `Tiến độ chung` | key TBA | `.layout` right card |
| Chart accessible label | `Biểu đồ đường tiến độ chung theo tuần, từ tuần 22 đến tuần 33 năm 2026` | — (composed) | `app-trend-chart`. 🔄 LẬT 2026-09-10 (Q54): the window is twelve weeks, so the prototype's `từ tuần 28` is stale. The wording across a year boundary waits on `doc/contracts/dashboard.md` § CONTRACT DB-1 |
| Chart placeholder while loading | `Đang tải biểu đồ…` | key TBA | `.chart-skeleton`, currently a resolved branch |
| Detail table heading | `62 chỉ tiêu DTI` | — (composed) | `app-criteria-table` `.title` |
| Detail table count | `62/62 chỉ tiêu` | — (composed, `aria-live`) | `app-criteria-table` `.title` |
| Search placeholder | `Tìm mã hoặc tên chỉ tiêu...` (three dots, not an ellipsis character) | key TBA | `.input-icon.search` |
| Search accessible name | `Tìm mã hoặc tên chỉ tiêu` | key TBA, on `aria-label` | `.input-icon.search` |
| Filter trigger | `Lọc` | key TBA | `<summary class="btn">` |
| Filter condition 1 | `Nhóm chỉ tiêu` → `Tất cả nhóm` + the six group names | key TBA + server data | `.filter-panel` |
| Filter condition 2 | `Trạng thái` → `Tất cả trạng thái` · `Chưa thực hiện` · `Đang thực hiện` · `Cần bổ sung minh chứng` · `Hoàn thành` | key TBA | `.filter-panel` |
| Filter footer | `Xoá lọc` · `Áp dụng` | key TBA | `.filter-foot` |
| Sort select | `Sắp xếp danh sách` (accessible name) → `Theo mã chỉ tiêu` · `Chênh lệch lớn nhất` | key TBA | `.toolbar-actions` |
| Chip remove button | `Bỏ lọc <chip label>` (`aria-label`), a `pi pi-times` glyph, **no `title`** | — (composed by `<app-toolbar>`) | `.filter-chip`, per the shipped component |
| Table headers | `Mã` · `Chỉ tiêu` · `Nhóm` · `Điểm tối đa` · `Tự đánh giá` · `Thẩm định` · `Chênh lệch` · `Trạng thái` · `Minh chứng/Ghi chú` | key TBA | `app-criteria-table` `<thead>` |
| Status values | `Chưa thực hiện` · `Đang thực hiện` · `Cần bổ sung minh chứng` · `Hoàn thành` | — (server values, fixed set of four) | `app-status-badge` |
| Empty note cell | `—` (em dash, in `.muted`) | key TBA | `app-criteria-table` `<tbody>` |
| History panel heading | `Lịch sử các kỳ đã lưu` | key TBA | `.history-card` `.title` |
| History panel caption | `Không ghi đè dữ liệu tuần cũ` | key TBA | `.history-card` `.title` |
| History row, period | `10/08 – 16/08/2026` | — (composed) | `app-history-list` |
| History row, progress | `Tiến độ chung` + a bold percentage | key TBA + value | `app-history-list` |
| History row, oldest period | `Kỳ đầu` in `.muted` | key TBA | `app-history-list` |
| History row action | `Xem` | key TBA | `app-history-list` |
| Footer | `Xem toàn bộ danh mục & nhập/cập nhật dữ liệu tại Danh mục > DTI.` — the last two words are a link | key TBA | `.footer` |
| Shell copy (sidebar brand, logout, hamburger, toast dismiss) | unchanged from the live shell | — | see [`../Components/Sidebar.md`](../Components/Sidebar.md), [`../Components/Topbar.md`](../Components/Topbar.md) and [`../Components/Toast.md`](../Components/Toast.md) — the shell's copy belongs to those three, not to a screen spec |

**Copy notes.** The period label appears in four places on this one screen, and the
verbatim strings for each are in the table above: the value box, the select option,
the history row and the chart's x-axis tick.

> 📖 **The period-label *rule* is not owned by this file.** Which dates a week or a
> month spans, and the format each of the four labels takes, live in
> `spec/dashboard-dti/business-rules.md`. This spec records the shipped strings; it
> does not define how they are built. Restating the rule here would be the second
> source `.claude/CLAUDE.md` §5 forbids.

One formatting decision does belong here, because it is a copy fix rather than a
rule: decision **T5** settles the dash on the **spaced** form — `10/08 – 16/08` —
everywhere. The approved prototype contradicts itself, writing the value box and
the select options without spaces and the history row and chart axis with them.
The spaced form wins on legibility and on being the more common of the two. The
strings above are written in that form; the prototype is the side that needs
syncing (§ Normalize on redesign).

#### Error codes → copy — duyệt 2026-09-10 (Q56)

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Câu chữ đã được người dùng **duyệt nguyên văn 2026-09-10 (Q56)**; chưa màn nào dựng. Written for decision Q56 from the
> codes in `doc/contracts/dashboard.md` (§ Mã lỗi của DB-1 and § CONTRACT DB-4). **None of
> these keys exists in `src/FE/public/i18n-app/`** today, and nothing here ships before the
> product owner approves it.

The BE sends a `businessCode` and the FE uses it **as the translation key itself**, nested
by domain, in the project bundle `src/FE/public/i18n-app/{vi,en}.json`
(`doc/huong_dan/wiki-core/fe/08-i18n.md` § 1 — Mã lỗi BE dùng THẲNG làm khoá), so the key
column below is the code. The code left in it reaches the user as a `Toast` from the shell
— this screen has no in-page error surface (§ States → error).

| Code | Where it shows | Proposed copy (vi) | `messageParams` |
| --- | --- | --- | --- |
| `DASHBOARD.EXPORT_MODE_UNSUPPORTED` | toast, after `Xuất báo cáo` | `Chưa xuất được báo cáo cho chế độ Tất cả (cả năm). Chọn một tuần hoặc một tháng rồi bấm Xuất báo cáo.` | the contract's template carries `{Mode}`; the proposed sentence does not need it |

- 🔄 **LẬT 2026-09-10 (Q62):** `DASHBOARD.MODE_INVALID` and `DASHBOARD.STATUS_INVALID` were
  rows of this table, because a stale or hand-edited link could carry a bad `mode` or
  `status`. Q62 has the page reset an unknown URL value to its default, rewrite the URL and
  never call the API with the bad value — silently — so neither code can reach a user, and
  both left the table.
- **`EXPORT_MODE_UNSUPPORTED` is a safety net.** In `Tất cả` mode the button is already
  disabled (`spec/dashboard-dti/ui-spec.md` § 4), so the toast appears only if a request
  gets past that.
- **Left out:** a malformed `date`/`year` (binder `ValidationError`, not a catalog code) and
  the HTTP fallbacks — 403, 404, 429, offline — whose copy is Core's
  ([`05-auth.md`](./05-auth.md) § Copy).

### States

<!-- How each state renders: default / loading / empty / error / validation display. -->

- **default:** a period is selected and returns data. All six regions render as
  described above.
- **loading — decision Q34, 2026-09-05:** the screen fetches its aggregate on entry
  and on every period change. While that request is in flight the four data regions
  **dim, with one small spinner over them**: the KPI row, the group-progress panel,
  the chart card and the detail table. The period toolbar stays fully lit and usable —
  the control that started the fetch must not become unreachable during it — and so
  does the history panel, which does not change with the period being previewed.

  What Q34 replaces is **nothing at all.** Per the decision's own account, the
  pre-retirement build declared a `loading` flag that no template ever read, so
  switching period left the previous period's numbers standing — undimmed and
  unlabelled — until the response landed: five KPI tiles and a full table of figures
  that quietly belonged to a period the user was no longer looking at. That code was
  deleted on 2026-08-29 and cannot be re-measured today, so it is recorded here as the
  decision's stated reason rather than as a verified fact. The reason matters either
  way: the dim is not decoration, it is the only thing marking stale numbers as
  stale.

  Two distinctions to keep:
  - **Data loading is not code loading.** The chart also has a *code*-loading state —
    the `.chart-skeleton` placeholder shown while its lazy chunk downloads (§ Layout
    Blueprint). The two can happen at once, and they do different jobs: the skeleton
    reserves height so the card cannot jump, the dim marks content as out of date.
  - **The catalogue screen needs none of this.** Its grid is a `DataTable`, and
    `p-table`'s own `[loading]` mask already covers it, painted by the preset
    ([`../Components/DataTable.md`](../Components/DataTable.md) § Variants). Adding a
    second loading treatment there would put two mechanisms in the product for one
    job — [`02-danh-muc-dti.md`](./02-danh-muc-dti.md) § States says so explicitly.

  **The spinner is PrimeNG's `p-progressSpinner`** — decision **T10**, 2026-09-06 — and
  it is deliberately **not** a new row in [`../COMPONENTS.md`](../COMPONENTS.md). Three
  reasons, in the order they decided it: PrimeNG is already a dependency, so nothing is
  installed to get it; it takes its colour from the preset built by
  `createCorePreset(APP_PALETTE)`, so it needs no new token and cannot drift from the
  palette; and it is the same *kind* of thing as the paginator arrows and the `p-table`
  loading mask — **chrome PrimeNG paints at runtime**, catalogued in
  [`../Icons.md`](../Icons.md) § Per-Action Map rather than specified as a component of
  this library. The app owns no spinner CSS today and after T10 it needs none:

  ```bash
  grep -c '@keyframes' src/FE/src/styles.scss      # 0 today, and should stay 0
  grep -rc 'pi-spin' src/FE/src | grep -v ':0$'    # no output — no hand-rolled spinner
  ```

  **The dim is this screen's own, and it is one declaration:** `opacity: .5` on each of
  the four regions while the fetch is in flight, plus `aria-busy="true"` so the state is
  not carried by colour alone. The value is not invented — `.5` is exactly what the app
  already dims a disabled control by ([`../Components/Button.md`](../Components/Button.md)
  § States → disabled), so "unavailable right now" looks the same everywhere. It is an
  un-tokenised literal all the same; § Normalize on redesign.
- **empty — no data for the selected period:** each region degrades on its own
  rather than the page blanking. The chart swaps its plot for one `.muted`
  sentence; the group list and the history list each render their own `.muted`
  empty sentence; the detail table shows its empty message. The five KPI tiles
  render `—` in place of every value (see below).
- **empty — brand-new deployment, nothing imported yet:** the state a fresh
  install lands on, because this screen *is* the landing route (Q18). Every region
  is empty at once and the period selector has nothing to offer. The screen must
  still read as a working page that says where to go, so it renders a
  `NoticeBanner` above the KPI row pointing at the catalogue, and the five KPI
  tiles show `—` rather than zeros. The `Footer` link is the second route out.
  Default (information) severity, `pi pi-info-circle`. Verbatim copy:

  > `Chưa có dữ liệu DTI nào. Vào Danh mục DTI để nhập file hoặc thêm chỉ tiêu đầu tiên.`

  `Danh mục DTI` is the route out, rendered as the inline `a` child that `.notice`
  already contracts for ([`../Components/NoticeBanner.md`](../Components/NoticeBanner.md)
  § Anatomy). **Settled by decision T11, 2026-09-06**: a link, not a labelled button —
  widening the `NoticeBanner` contract for one action would work against the standing
  requirement to compose what already ships, and the prototype's own notice library
  uses the inline link in all four severities.
- **empty — criteria imported, no `Tiến độ %` entered yet.** ⚠️ **This is the
  normal state immediately after every import, not an edge case.** Decision Q24
  settles that import leaves `Tiến độ %` **blank** for the user to fill in, and
  decision Q11 has the group bars and the trend line drawn from `Tiến độ %`. So
  right after an import the catalogue is full and this screen is still largely
  empty: six **empty** progress bars — no fill and no figure, because a group's
  progress is *absent* rather than `0` (`spec/dashboard-dti/business-rules.md` §1.1,
  and [`../Components/ProgressBar.md`](../Components/ProgressBar.md) § Variants →
  `Awaiting data`) — and a trend line with nothing to plot. **The product
  owner has accepted this behaviour; it is not a defect and must not be "fixed" by
  silently deriving a progress figure.**

  It does have to be *legible*, and the difference from the state above matters:
  "nothing has been imported" and "62 criteria exist but nobody has recorded
  progress" call for different sentences and lead to different next actions. The
  detail table is fully populated in this state — every score column has values —
  which is what makes an empty group panel above it look broken unless the copy
  explains it.

  The figures the approved prototype draws (74,3% · 51,8% · … · 82,1%) are
  therefore **a state reached after someone entered progress**, not the state
  after import. Read them as an illustration of a working dashboard, not as
  something import produces.

  **Decision Q32 settles the treatment**: a `NoticeBanner` above the KPI row, default
  (information) severity with the `pi pi-info-circle` glyph, saying how many criteria
  exist, that no progress has been entered, that the bars and the chart will fill in
  once it is, and offering the route to the catalogue. Verbatim copy:

  > `Đã có 62 chỉ tiêu, nhưng chưa chỉ tiêu nào có Tiến độ %. Thanh tiến độ theo nhóm và biểu đồ sẽ hiện ngay khi có số liệu. Nhập Tiến độ % tại Danh mục DTI.`

  The count is composed from the response, not hardcoded — `62` is the row count of
  the in-repo sample dataset `spec/danh-muc-dti/dti-mau-an-danh-62-dong.csv` (measured 2026-09-10) and it is what the approved
  prototype shows everywhere else on this screen. `Danh mục DTI` is the route out, the same inline `a`
  child as in the state above, and settled the same way by decision T11.

  **What each KPI tile reads in this state — settled by decision T12, 2026-09-06.**
  The *rule* (what each tile is computed from) is owned by
  `spec/dashboard-dti/business-rules.md`; the table below records only what the screen
  shows. Labels are verbatim from
  [`../Components/KpiTile.md`](../Components/KpiTile.md) § The five tiles, in week
  mode; in month and year mode tiles 1 and 2 take the labels in that file's § Tiles 1
  and 2 follow the period mode (Q52).

  | # | Tile | Right after import | Why |
  | --- | --- | --- | --- |
  | 1 | `Tiến độ chung tuần này` | `—` | derived from `Tiến độ %`, which Q24 leaves blank |
  | 2 | `So với tuần trước` | `—` | same source, and on a first import there is no earlier period to compare with either |
  | 3 | `Chỉ tiêu tăng` | `0` | a count of criteria that moved; nothing has moved yet |
  | 4 | `Không tăng` | `0` | the same comparison and the same absence — **not** `62` |
  | 5 | `Hoàn thành` | the real figure — `26/62` on the BA's dataset | counted from `Trạng thái`, which the import file carries |

  **Tile 5 is what makes this state legible.** It is the one number that proves the
  import landed, so the screen reads as *"the data is in, the progress is not"* rather
  than as a failed import with a full table underneath it by coincidence. T12 also
  settles the tension the prototype contained: tile 1's sub-caption
  `Bình quân Tiến độ %, gia quyền theo Điểm tối đa` describes the **populated** state,
  not this one. That caption was itself corrected on **2026-09-09** — the earlier
  wording, `Bình quân gia quyền theo điểm (thật: 787,84/960)`, named a ratio of scores
  rather than the quantity the tile computes, and the parenthetical was a prototype
  provenance annotation that does not ship;
  [`../Components/KpiTile.md`](../Components/KpiTile.md) § The five tiles holds the
  decision.

  ⚠️ **Both banner strings are written by this spec on 2026-09-05, not read off the
  prototype.** `doc/Design/Frontend/PlatformManager/Prototypes/index.html` § `#screen-dashboard` draws neither state — Q32
  and the first-run copy both postdate its approval.
- **error:** a failed aggregate request surfaces through the app's global HTTP
  error handling as a `Toast` in the shell. This screen authors no in-page error
  banner of its own; whether it should — a `NoticeBanner.bad` above the KPI row is
  the obvious candidate — is unspecified.
- **access — every signed-in account, no exceptions.** Per decision **Q21** the
  Dashboard needs no DTI permission: `authGuard` + `mustChangePasswordGuard` and
  deliberately **no** `roleGuard`, which is also what the route position requires,
  since `roleGuard` redirects a role-less user here and a guarded fallback would
  loop (`src/FE/src/app/core/auth/role.guard.ts`). **There is consequently no
  "you lack permission" state on this screen and none should be designed.** An
  earlier revision of this spec specified one; Q21 removed the need for it. Write
  access on the catalogue is a separate, still-open question
  ([`02-danh-muc-dti.md`](./02-danh-muc-dti.md) § Cần chốt).
- **validation:** none. The screen has no input. The period selects, the view-mode
  switch, the search box, the filter panel and the sort select are all query
  controls; none can be invalid.
- **print:** the two `.no-print` regions (the period toolbar and the table toolbar)
  disappear, along with the shell's sidebar, topbar and toast stack. The scrolled
  regions are the problem: the detail table and the history list both cap their
  height, so a printed page silently truncates them. Carried in § Normalize on
  redesign.

### Responsive

<!-- Behavior per breakpoint. -->

- **≥981px (desktop default):** `.layout` is a `1.15fr 0.85fr` two-column grid; the KPI row is five equal columns; `main` is centred at `container-max-width` with `spacing.sp-5` padding.
- **≤980px (tablet):** three things change at once. `.layout` collapses to a single column (§ `@media (max-width: 980px)` → `#screen-dashboard .layout`), so the group panel sits above the chart. The KPI row drops to **two** columns (§ `app-kpi-summary .kpis`). The group rows narrow their name column from 210px to 140px (§ `app-group-progress-list .group-row`). The shell's sidebar becomes an off-canvas drawer opened from the topbar hamburger.
- **≤560px (mobile):** the KPI grid gap tightens to 8px and — because `app-kpi-tile` is `display: contents` — **all five** tiles span the full width rather than the intended four-in-two-columns-plus-one. That is defect **A1**, recorded as-drawn — `doc/Design/Frontend/PlatformManager/Prototypes/index.html` § `app-kpi-tile` and § `app-kpi-summary .kpis .card:last-child` (inside `@media (max-width: 560px)`). KPI values step down to 18px. Group-row name columns narrow again to 110px. The shell drops `main` padding and, in the topbar, hides the user's name **and both account labels** — the change-password and sign-out buttons keep only their glyphs (`Components/Topbar.md` § Variants, row *Compact user block*).
- **Not responsive at any breakpoint:** the trend chart's height (a fixed 220px from phone to 4K) and the history rows' grid template, which has no breakpoint variant at all — see [`../Components/HistoryRow.md`](../Components/HistoryRow.md) § Normalize #1. The detail table does not restack; it scrolls horizontally inside `.tablewrap`, which is the `Table` contract.

### Iconography

See [`../Icons.md`](../Icons.md) § Per-Action Map. The app loads **PrimeIcons v7**
globally and authors icons as `<i class="pi pi-*">` elements.

| Action | Icon | Placement |
| --- | --- | --- |
| Export the period as `.xlsx` | `pi pi-file-excel` | Leading, inside the `.btn.primary` in `.toolbar-actions` |
| Search the criteria table | `pi pi-search` (decorative) | Leading adornment inside `.input-icon.search` |
| Open the filter panel | `pi pi-filter` | Leading, inside `<summary class="btn">` |
| Open the navigation drawer (≤980px) | `pi pi-bars` | Shell topbar, left |
| Collapse / expand the sidebar | `pi pi-angle-left` | Shell sidebar brand row |
| Change password | `pi pi-key` | Shell topbar, right — first of the two account actions (added 2026-09-11) |
| Sign out | `pi pi-sign-out` | Shell topbar, right |
| Dismiss a toast | `pi pi-times` | Shell toast item, right |

⚠️ **`pi pi-file-excel` is not yet a row in [`../Icons.md`](../Icons.md) § Per-Action
Map.** It is a genuine addition introduced by decision Q13 (the export button), and
it is the only new action icon this screen brings. Add the row to `Icons.md` when
this screen is built — that file is the master for the icon map, and duplicating
the mapping here instead would create the second source `.claude/CLAUDE.md` §5
forbids.

Direction arrows (`↑` / `↓`) in the KPI tiles and the history rows are **not
icons** — they are literal characters inside the label text. `../Icons.md`
§ Legacy Exceptions.

### Screenshots

<!-- Refs into Assets/Screenshots/dashboard/ -->

**No screenshot of this design exists, and none can be captured**, because the
screen is not built. `Assets/Screenshots/dashboard/` currently holds captures of
the **deleted** pre-2026-08-29 build, plus a `_superseded-prototype/` folder of
captures from the prototype deleted in 2026-08-23. Neither shows this design; both
are kept for before/after comparison only and must not be cited as evidence of
anything current.

Under `doc/Design/CLAUDE.md` § Rules the target is **one desktop shot per screen**.
Exactly one row below is the real gap.

Prerequisites, once the screen is built:

> 📖 Capture environment (server URLs, allowed ports, database setup): read [`../../../CLAUDE.md`](../../../CLAUDE.md) § Rules

| Screenshot path | Status | Capture instructions |
| --- | --- | --- |
| `Assets/Screenshots/dashboard/dashboard--desktop-1440.png` | **blocked — screen not built** | Landing route @ 1440×900, full page, sidebar expanded, week mode, a period with data selected, no filter applied. This is the one shot that must exist. |
| `Assets/Screenshots/dashboard/dashboard--no-progress--desktop-1440.png` | blocked — screen not built | Same @ 1440×900 **immediately after an import, before anyone enters `Tiến độ %`** — a full detail table above six **empty** group bars — no fill and no figure, which is what the shipped component renders when a group's progress is absent (`src/FE/src/app/modules/dashboard/components/progress-bar/progress-bar.html:19-23`) — and an empty chart (§ States). ⚠️ **SỬA 2026-09-10**: this cell used to ask for *"six 0% group bars"*, the one rendering that component deliberately refuses. This is the state every deployment passes through and the one most likely to be mistaken for a bug, so it is worth capturing early. |
| `Assets/Screenshots/dashboard/dashboard--empty--desktop-1440.png` | blocked — **name already taken** by a capture of the deleted build | The existing file of this name shows the pre-retirement screen. Move or rename it before capturing the new empty state, or the two will be silently confused. |
| `Assets/Screenshots/dashboard/dashboard--month-mode--desktop-1440.png` | on demand | Same @ 1440×900 with `Tháng` selected — the only shot showing the month period label, the month select and the `Th.1 … Th.12` x axis. |
| `Assets/Screenshots/dashboard/dashboard--mobile-390.png` | on demand | @ 390×844 — the only shot showing the five-tiles-in-one-column artefact and the single-column `.layout`. |

### Normalize on redesign

<!-- Screen-local quirks ONLY here — sections 1-6 stay as-shipped. A quirk that spans components belongs in the component's own spec (`Components/<Name>.md` → Normalize on redesign), not here. -->

**Two divergences from the prototype are left — the second added 2026-09-10 by Q54 — and
neither is one of the four this section used to list.** An earlier revision named Q22, T1, T2 and T5. Re-measured against
`doc/Design/Frontend/PlatformManager/Prototypes/index.html` § `#screen-dashboard` on 2026-09-05, after the prototype's own
sync pass: the Q22 items **have since been applied** — the filter panel holds two
conditions and the sort select two options, each change carrying a
`SỬA 2026-09-05 (Q22)` comment — so that pair was a true divergence at the time it was
written and is simply closed now. T1 and T2 are different: they cannot apply to this
screen at all, because its toolbar is drawn in the *unfiltered* state and renders no
`.filter-chips` block to sit on the wrong side of the separator. They belong to the
catalogue screen only.

| Still divergent | The spec says | Decision | Check |
| --- | --- | --- | --- |
| The date-range dash written both ways — `10/08–16/08` in the period value box and the period selects, `10/08 – 16/08` in the history rows and the chart axis | the spaced form everywhere | T5 | `grep -c '[0-9]/[0-9][0-9]–[0-9]' doc/Design/Frontend/PlatformManager/Prototypes/index.html` — PASS = 0, and it is not 0 today. ⚠️ **Not every hit is in this screen's region** — at least one sits in a comment up in the CSS layer, so use the section-scoped form instead of the whole-file count: `awk '/id="screen-dashboard"/,/id="screen-nguoi-dung"/' doc/Design/Frontend/PlatformManager/Prototypes/index.html \| grep -c '[0-9]/[0-9][0-9]–[0-9]'`. Corrected 2026-09-10; the previous cell claimed every hit was in-region |
| The trend chart's week window — six x-axis labels (weeks 28–33) and an accessible label reading `từ tuần 28` | twelve weeks, ending at the week being viewed | Q54 | `grep -c 'y="208">' doc/Design/Frontend/PlatformManager/Prototypes/index.html` counts the x-axis tick labels — PASS = 12; it read 6 on 2026-09-10 |

Three things this spec describes are **not in the prototype at all**, and they are
additions rather than drift: the two first-run banners (Q32 and its first-run twin)
and the loading dim (Q34). Each is marked where it appears.

Screen-local quirks, unchanged:

1. **Two `margin-top: 16px` literals** sit off the spacing scale — on the detail-table card and the history card (`spacing.sp-5` is the nearest step). Two cards' vertical rhythm is therefore set by a number no token controls.
2. **The footer link points at an anchor that does not exist.** In the approved prototype the footer targets `#screen-danh-muc-dti` while the catalogue section's id is `screen-dti`, so the link is dead in the prototype itself. Harmless there; it means the real destination route for that link is **not** recorded anywhere and must be decided when the catalogue route is.
3. **Both scrolled regions truncate in print.** The detail table caps at `--grid-h` and the history list at 240px, and no `@media print` rule releases either — so the printed page shows a window onto the data with no indication that more exists. Defect **A4** — check it with `grep -n '@media print' doc/Design/Frontend/PlatformManager/Prototypes/index.html`: every block there hides chrome (`.proto-bar`, `.no-print`, sidebar, topbar) or widens `main`; none releases `--grid-h` or the 240px history cap.
4. **Two filter surfaces with different scopes, and nothing says so** — the period toolbar changes the whole page, the table toolbar changes one region. Decision **Q23** sharpens this rather than softening it: `Xuất báo cáo` now exports through the *table's* filters while sitting in the *period* toolbar, so the button's own bar is not the one that decides what it exports. A cue is needed — a count on the button, or moving it next to the filters.
5. **Sorting lives outside the filter panel** while every condition lives inside it. Defensible — sorting is not a condition — but the two controls look alike and sit on the same bar.
6. **The KPI row's empty treatment is copy, not structure.** Five tiles rendering `—` is the agreed answer (§ States), but `KpiTile` has no dedicated empty variant — the dash is just a string passed as the value. That works, and it means nothing stops a caller passing `0` instead and losing the distinction. See [`../Components/KpiTile.md`](../Components/KpiTile.md) § Normalize.
7. **The loading dim is an un-tokenised literal.** `opacity: .5` is borrowed from `.btn:disabled` so that "unavailable right now" reads the same everywhere, but no token carries the value and nothing links the two: change the button's disabled opacity and this screen keeps the old one, silently. A single `opacity-disabled` token would bind them.
8. **`Chỉ tiêu tăng: 0` beside `Không tăng: 0` is ambiguous, and the fixed tones make it worse.** Right after an import both counters read `0` (decision T12) because no comparison has happened — but the reader sees a green `0` next to an amber `0` above a table of 62 populated rows, which looks like a contradiction rather than an absence. `KpiTile` gives tiles 3 and 4 a fixed tone regardless of value ([`../Components/KpiTile.md`](../Components/KpiTile.md)), so neither can drop to neutral to say "nothing to compare yet". `—` would say it; `0` asserts a measurement that was never taken. Item 6 above is the same gap seen from the other side.

## Cần chốt

<!-- Decisions this file must NOT make on its own. Raised 2026-09-05, emptied 2026-09-06 by T10, T11 and T12. -->

**One proposal waits for the product owner** — the error-code copy (Q56, § Copy → Error
codes → copy). The trend-axis label fit opened the same day was approved on 2026-09-10
(Q54, [`../Components/TrendChart.md`](../Components/TrendChart.md)). Apart from that,
nothing is open on this screen. The three items raised on 2026-09-05 — what draws
the loading spinner, whether the first-run banners carry a link or a button, and how
many KPI tiles read `—` right after an import — were all answered on 2026-09-06 and
have moved into the table below.

Two things this spec settled on its own authority, recorded here so they are visible
rather than buried: the loading **dim** is `opacity: .5`, reusing the value
`.btn:disabled` already uses, and neither read-only-ish empty state gets an
explanatory banner beyond the copy written in § States. Both are visual detail, not
product decisions; if either is wrong it is a one-line correction, and § Normalize on
redesign carries the reason each could age badly.

### Answered elsewhere — do not re-ask

| Was open here | Answer | Where it lives now |
| --- | --- | --- |
| The route path | **`/trang-chu`**, in place — Q18 | this file's frontmatter + § intro |
| Who may see the Dashboard | **Everyone signed in**, no DTI permission — Q21. No "lacks permission" state exists | § States → access |
| Does `Xuất báo cáo` respect the table's filters | **Yes** — Q23, matching Core law `doc/huong_dan/wiki-core/be/15-import-export.md` § 4. The UX risk is logged as § Normalize #4 | `doc/contracts/dashboard.md` |
| The `Mức thay đổi` filter and two sort options | **Removed** — Q22. KPI tiles 3 and 4 stay; they display, they do not filter | § Layout + § Copy |
| How `Tiến độ %` is seeded on import | **It is not** — Q24 leaves it blank for the user. Consequence specified in § States | `spec/danh-muc-dti/business-rules.md` |
| What renders the chart, and the four `chart-*` roles | **`p-chart` over `chart.js`; roles restored** — Q17 | [`../Components/TrendChart.md`](../Components/TrendChart.md), [`../Tokens/colors.md`](../Tokens/colors.md) § Chart Palette |
| How KPI tile 5 counts `Hoàn thành` | By the manually-chosen `Trạng thái` value, not by `Tiến độ % = 100` | `spec/dashboard-dti/business-rules.md` |
| Does the trend chart follow the table's filters | **No** — it always shows overall progress for the period | `spec/dashboard-dti/business-rules.md` |
| The period-label formats, as a rule | ISO week, calendar month, four label formats, spaced dash (T5) | `spec/dashboard-dti/business-rules.md` |
| What becomes of the old welcome screen and its folder | **Deleted** — Q29, 2026-09-05. `/trang-chu`, `APP_CORE_ROUTES.home`, the `''` redirect, the `**` wildcard and the seeded menu row all stay exactly as they are | § intro; [`06-trang-chu.md`](./06-trang-chu.md) now carries a historical banner |
| How the page behaves while a period is loading | **The four data regions dim under one spinner** — Q34. Only the mechanism that draws the spinner is still open | § States → loading |
| Copy for the two empty states | **Written out verbatim** — Q32 for the post-import one, and its first-run twin beside it | § States, § Copy |
| Which way `Chênh lệch` subtracts, and what its colours mean | **`Thẩm định − Tự đánh giá`; positive green, negative red, zero grey** — Q25 | [`../Components/DeltaIndicator.md`](../Components/DeltaIndicator.md) |
| The x axis in month mode | **`Th.1 … Th.12`, not date ranges** — T7 | [`../Components/TrendChart.md`](../Components/TrendChart.md) |
| What draws the loading spinner | **PrimeNG's `p-progressSpinner`** — T10, 2026-09-06. Chrome PrimeNG paints, like the paginator and the `p-table` mask, so **no new row in `COMPONENTS.md`** and no new token | § States → loading |
| Whether the first-run banners carry a link or a button | **An inline link** — T11. The `.notice` contract has no labelled-button slot and is not being widened for one action | § States, § Copy |
| What each KPI tile reads right after an import | **`—` · `—` · `0` · `0` · the real `Hoàn thành` count** — T12 | § States; rule owned by `spec/dashboard-dti/business-rules.md` |
| How a group is labelled | **`Code. Name`** — `1. Hạ tầng và Nền tảng số`; `Code` `"1"`…`"6"` in template-file order, `Name` the file's `Nhóm` string unprefixed — Q42, 2026-09-10 | § Copy → Group names |
| Who writes the week-mode x-axis labels | **The backend**, as ready-made date ranges; the week code stays as the identity key and the frontend does no ISO-week arithmetic — Q43, 2026-09-10 | [`../Components/TrendChart.md`](../Components/TrendChart.md) § The x axis |
| What the trend series carries for a period with no data | **The period itself, with `value: null`** — every period up to the current one, so the line breaks in the right place — Q44, 2026-09-10 | [`../Components/TrendChart.md`](../Components/TrendChart.md) § Anatomy |
| How many weeks the trend chart shows in week mode | **Twelve, ending at the week being viewed** — Q54, 2026-09-10. How the labels fit was approved the same day (Q54); what happens at a year boundary is DB-1's to define | [`../Components/TrendChart.md`](../Components/TrendChart.md) § The x axis changed on 2026-09-05 |

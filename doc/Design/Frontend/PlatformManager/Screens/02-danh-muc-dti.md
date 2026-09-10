---
kind: luat
scope: du-an
verified: khong-ap-dung
project: "PlatformManager"
status: "target — not built"
updated: "2026-09-10"
flow: "DTI Catalogue"
screens: ["DTI Catalogue"]
source_routes: ["/danh-muc/dti"]
---

# DTI Catalogue — Screens

> # 📐 ĐÍCH ĐẾN — CHƯA THI CÔNG
>
> **The screen is not built. Its route and its module folder now are.**
> 🔄 **SỬA 2026-09-10.** The previous revision said *"This screen does not exist
> in `src/FE`… the rebuild has not started"* and told the reader to expect no
> `src/FE/src/app/modules` directory at all. That stopped being true on 2026-09-09.
> What is on disk, checked 2026-09-10:
>
> | Built | Still to build |
> | --- | --- |
> | The route `/danh-muc/dti`, declared and reachable (`src/FE/src/app/app.routes.ts:39-43`), carrying `authGuard` + `mustChangePasswordGuard` and no permission guard, per Q39 (`src/FE/src/app/modules/danh-muc-dti/danh-muc-dti.routes.ts:12-22`) | — |
> | `danh-muc-dti.page.ts`, an **intentional skeleton**: a title row and one "under construction" line, with the toolbar, the grid and the four dialogs left undrawn on purpose so no reader mistakes the shell for the screen (`src/FE/src/app/modules/danh-muc-dti/pages/danh-muc-dti/danh-muc-dti.page.ts:7-16`) | every region in § Layout Blueprint |
>
> So the URL resolves today and shows a placeholder. Everything below is still a
> **design to be built**, approved by the product owner on 2026-09-04 and
> 2026-09-05, not a record of a running screen. Check rather than trust the table:
>
> ```bash
> grep -n 'danh-muc/dti' src/FE/src/app/app.routes.ts
> grep -n 'KHUNG' src/FE/src/app/modules/danh-muc-dti/pages/danh-muc-dti/danh-muc-dti.page.html
> ```
>
> **No `file:line` citation into `src/FE` appears below for the screen's own
> regions** — the files they would point at still do not exist. Core elements that
> ship (the app shell, the global component layer) are cited by **identifier**, per
> `doc/Design/CLAUDE.md` § Neo trích dẫn vào `styles.scss`.
>
> **This file replaces a historical one.** The previous revision carried a
> historical-document banner describing `/danh-muc/dti` as it shipped before
> 2026-08-29; read it in git history (that code was live at commit `98a5d96`) if
> you need the pre-retirement design. It is superseded here rather than kept
> alongside, because two files answering one question is what `.claude/CLAUDE.md`
> §5 exists to prevent.

The DTI Catalogue is the product's **single data-entry surface**. One `Card`, one
wide grid, four dialogs: a digital-transformation officer maintains the criteria
catalogue (create / edit / delete), imports a whole period from a spreadsheet, and
edits per-period values. The Dashboard ([`01-dashboard.md`](./01-dashboard.md)) is
read-only by design; **every write in the product happens here.**

Two decisions from 2026-09-05 shape it more than the rest:

- **Decision Q9 splits editing in two.** The six assessment fields — `Tự đánh giá`,
  `Thẩm định`, `Trạng thái`, `Phụ trách`, `Hạn xử lý`, `Minh chứng/Ghi chú` — are
  gathered into the `Sửa chỉ tiêu` dialog, because six is too many for inline
  editing. Inline editing in the grid keeps **exactly two** fields, unchanged from
  the pre-retirement design: `Tiến độ %` and `Minh chứng/Ghi chú`.
- **Decision Q6 puts import in scope**, accepting `.csv`, `.xlsx` and `.xls`. That
  follows Core law in `doc/huong_dan/wiki-core/be/15-import-export.md`; the
  asymmetry with export (which is `.xlsx` only) is deliberate and lives there.
- **Decision Q37 makes the week the only writable unit.** Months and years are
  computed from weeks, never typed into, so selecting one turns this screen — the
  product's only data-entry surface — read-only. "Single data-entry surface" therefore
  has a second half: *and only in week mode*.

The URL is **`/danh-muc/dti`**, settled by decision **Q33** on 2026-09-05: it is the
address the pre-retirement screen used, it is what the surviving pointers in
[`../Components/Footer.md`](../Components/Footer.md) and [`../DESIGN.md`](../DESIGN.md)
already say, and it matches the two-level shape of the live Core route
`/quan-tri/nguoi-dung`.

> **Shell:** the app shell — skip link + `Sidebar` + `Topbar` + `main` + `Toast`
> (`src/FE/src/app/app.html:16-39`), rendered because this route will not set
> `data.noShell`. `../DESIGN.md` → Layout describes this shell.
> **Sources:** `doc/Design/Frontend/PlatformManager/Prototypes/index.html`
> § `#screen-dti` — the prototype the product owner approved point by point on
> 2026-09-04 and 2026-09-05, the **only** source for this screen's layout and copy,
> and the same file that declares the component CSS it reuses. **Repointed
> 2026-09-10** from a second copy of it that lived outside the repo
> (`doc/Design/Frontend/PlatformManager/Prototypes/README.md` § In-repo master).
> **The column set** is backed by the in-repo sample dataset `spec/danh-muc-dti/dti-mau-an-danh-62-dong.csv` — 62 rows,
> eleven columns, `Phụ trách` and `Hạn xử lý` empty in all of them (measured
> 2026-09-10). **The figures and the value set** are the prototype's, computed from
> the BA's August 2026 spreadsheet, which is deliberately outside this repo (root `.gitignore`), so it is named rather than cited as a path; the sample is a different
> dataset and does not reproduce them.
> `src/FE/src/styles.scss` and
> `src/FE/src/app/shared/components/` for the live Core layer it composes.
> API contract → `doc/contracts/danh-muc-dti.md`; business rules →
> `spec/danh-muc-dti/business-rules.md`; UI spec →
> `spec/danh-muc-dti/ui-spec.md` (all three being written in parallel on
> 2026-09-05 — a disagreement with this file is a conflict to raise, not to
> resolve silently).
> **Token vocabulary:** token names are the live CSS custom properties in
> `src/FE/src/styles.scss` § `:root` minus the `--` prefix. Values quoted without
> a token name are literals in the approved prototype, recorded as such.

---

## DTI Catalogue (`/danh-muc/dti`)

### Layout Blueprint

<!-- Region tree + structural measurements. Compose ONLY component names present in COMPONENTS.md. -->

- **App shell** (`src/FE/src/app/app.html:16-39`) — surrounds the route
- **The page is a list screen**, so it uses the `.page-fill` contract: the grid
  card takes the remaining viewport height rather than the page growing past it
  (`src/FE/src/styles.scss` § `.page-fill`). The card itself is a flex column with
  `min-height: 0` so its scroll region can actually shrink
  (§ `#screen-dti .dti-grid-card`)
- **`Card`** — the whole page body, one surface, no second card anywhere
  - **`.title` row** — flex, space-between (`src/FE/src/styles.scss` § `.title`)
    - `<h2>Danh mục DTI</h2>` — renamed from `Danh mục & Đánh giá theo tuần` by decision Q16(a)
    - `<span class="muted" aria-live="polite">62 chỉ tiêu</span>` — **kept**, per decision Q16(b). Three reasons, all structural rather than aesthetic: `.title` declares `justify-content: space-between`, so the right-hand slot belongs to the contract and removing the element leaves the heading centred in a gap it was not designed for; the live Core screen `Quản trị người dùng` renders exactly this shape (`src/FE/src/app/platform/quan-tri-nguoi-dung/pages/quan-tri-nguoi-dung/quan-tri-nguoi-dung.page.html:12` — `<span class="muted" aria-live="polite">{{ totalCount() }} người dùng</span>`, pinned by a test in the sibling `.spec.ts`) so this is reuse, not invention; and `aria-live="polite"` is a WCAG 2.2 AA requirement here (`doc/huong_dan/wiki-core/fe/15-accessibility.md` § 3a — the rule is stated there as *"số dòng kết quả sau khi lọc/tìm kiếm"*, not at § 1 as an earlier note had it) because after a filter runs, the row count is the **only** evidence a screen-reader user gets that anything happened. Only the wording changes, from `N người dùng` to `N chỉ tiêu`
  - **Period banners** — one `NoticeBanner` slot, **five** mutually exclusive occupants, decided by the `Năm đánh giá` and `Kỳ trong năm` selections together. The five conditions split every year/period pair between them, so no test order is needed: **(a)** a **past year** with `Tất cả (mới nhất trong năm)` → the past-year read-only banner (decision T15); **(b)** a **month**, in any year → the aggregate read-only banner (decision Q37), in one of two wordings chosen by the year (decision **Q60**, 2026-09-10); **(c)** a **past week**, of the current year or of an earlier one (decision **Q41**, 2026-09-10) → the banner naming the week the edits will be written to; **(d)** current year and **`Tất cả (mới nhất trong năm)`** → the same target-period banner in its `— tuần hiện tại.` form, because the rows on screen come from several different weeks while every write goes to the current one (**settled 2026-09-09**; the reasoning is under § Copy); **(e)** current year with the **current week** selected → **no banner**, there being nothing to warn about — what is on screen and what a write lands on are the same period. Copy for all of them is in § Copy. ⚠️ Decision **Q20** reversed this region's meaning: a past period is now **editable**, so the banner no longer says "read-only" — it warns *where the writes are going*. **That copy was settled 2026-09-06** and its two strings sit in § Copy with the rest; they were moved out of § Cần chốt on 2026-09-09 so that one section holds the shipped strings. Suppressed in case **(e)** only. 🔄 **LẬT 2026-09-10 (Q41).** This list used to open *"Test the year **first**, because it outranks the period"*, with (a) reading *"`Năm đánh giá` is not the current year"* and (b) and (c) limited to the current year. That made every week of a past year read-only, which Q27 never allowed and Q41 now states outright. A month in a past year now falls under (b): that case is blocked for **one** reason only, `PERIOD_NOT_WEEKLY` — `PERIOD_OUT_OF_YEAR` is raised only when `Kỳ trong năm` is `Tất cả` — so the month banner's one instruction, *pick a specific week*, is the whole way out (decision **Q48**, 2026-09-10). 🔄 **SỬA 2026-09-10 (Q48):** this sentence used to say the case carries *both* codes; the conclusion — banner (b) — is unchanged, only the reason was wrong
  - **`Toolbar`** (`.toolbar.no-print`) — the same contract as the Dashboard's table toolbar, no new controls. **The DOM order below is the shipped component's, not the prototype's** (decision T1): `<app-toolbar>` renders search → filter → separator → chips → actions, while the approved prototype draws the chips *before* the separator. The component is the contract, so the spec follows it and the prototype is the side that needs syncing (§ Normalize on redesign).
    1. `.input-icon.search` — fixed 260px, `pi pi-search` adornment
    2. `<details class="filter">` → `<summary class="btn">` with `pi pi-filter`, the label `Lọc`, and a `.filter-count` badge **only when at least one condition differs from its default** → `.filter-panel` holding **four** `.form-row` conditions and a `.filter-foot`. A native `<details>`, so it opens without JavaScript.

       **What the badge counts** (decision T8): conditions that are **not** at their default. `Kỳ trong năm = Tất cả (mới nhất trong năm)` and `Năm =` the current year are the panel's own defaults, so neither is counted and neither raises a chip — a badge that reads `2` on a screen nobody has filtered teaches users to ignore it. In the approved state only `Nhóm chỉ tiêu` is set, so the badge reads `1` and there is exactly one chip. The same rule governs `.filter-chips` in slot 4
    3. `.toolbar-sep` — the flexible gap
    4. `.filter-chips` — one removable `.filter-chip` per applied condition, so the conditions stay visible after the panel closes. The remove control is an `IconButton` carrying a `pi pi-times` glyph and `aria-label="Bỏ lọc <chip label>"`, with **no `title`** (decision T2 — again the component's copy, not the prototype's `×` character and `Gỡ điều kiện` wording). The whole block only renders when there is at least one chip
    5. `.toolbar-actions` — two `Button`s: `Import CSV/Excel` (default) and `+ Thêm chỉ tiêu` (`.primary`)

    Every one of those five is conditional in the component, so a toolbar with no search, no filter or no chips collapses rather than leaving a gap. Anatomy and per-slot rules: [`../Components/Toolbar.md`](../Components/Toolbar.md).
  - **`DataTable`** — rendered by the shared **`<app-data-grid>`**, with `app-criteria-grid-table` as its caller, exactly the role `user-grid-table` plays on the Core screen. The caller declares the columns as `TemplateRef`s and asks for the two pins through the **`frozenColumns` input (chốt 2026-09-09)**; it **must not** import `p-table` or use `pFrozenColumn` itself, because `data-grid` is the one place `p-table` is imported ([`../Components/DataTable.md`](../Components/DataTable.md) § CHỐT 2026-09-06 and § Frozen edge columns). Height comes from the `page-fill` ⇄ `grid-host` flex chain with `scrollHeight="flex"`, **not** from `--grid-h`; the frame keeps `rounded.table`. **14 columns**, each with a `min-width` so the grid scrolls horizontally rather than crushing, and **both edge columns pinned against that scroll** (decision Q30, below):

    | # | Header | min-width | Alignment | Content |
    | --- | --- | --- | --- | --- |
    | 1 | `Mã` | 70px | left | bold code; **three levels occur** (`4.22.11`), so a two-level regex is wrong. **Frozen left** — Q30 |
    | 2 | `Tên` | 220px | left | full criterion name |
    | 3 | `Nhóm` | 120px | left | group label as `Code. Name` — `1. Hạ tầng và Nền tảng số` (decision **Q42**, 2026-09-10; the same form as the Dashboard, [`01-dashboard.md`](./01-dashboard.md) § Copy → Group names) |
    | 4 | `Kỳ của số liệu` | 110px | left | the **date range** of the week the row's figures were saved for — `10/08 – 16/08`, with **no week number** (decision Q38). **Conditional** — the three rules are below. Added by decision Q31 |
    | 5 | `Điểm tối đa` | 90px | `.num` | integer |
    | 6 | `Tự đánh giá` | 90px | `.num` | decimal |
    | 7 | `Thẩm định` | 90px | `.num` | decimal |
    | 8 | `Chênh lệch` | 90px | `.num` | `DeltaIndicator` — **computed, never stored, never typed**: `Thẩm định − Tự đánh giá` (decision Q25 — positive green, negative red, zero grey; [`../Components/DeltaIndicator.md`](../Components/DeltaIndicator.md) owns the direction and its history) |
    | 9 | `Trạng thái` | 150px | left | `Badge`, colour-mapped (decision Q10) |
    | 10 | `Phụ trách` | 110px | left | assignee name, or `—` |
    | 11 | `Hạn xử lý` | 100px | left | date, or `—` |
    | 12 | `Tiến độ %` | 130px | `.num` | **inline-editable** (`.cell-editable`) |
    | 13 | `Minh chứng/Ghi chú` | 220px | left | **inline-editable** (`.cell-editable`); one free-text field, not a list of evidence rows (decision Q5) |
    | 14 | `Hành động` | 120px | left | `Button` `.btn.sm` `Sửa` + `Button` `.btn.sm.danger` `Xoá`. **Frozen right** — Q30 |

    Two of these are structural additions to the pre-retirement grid: `Chênh lệch` (#8, decisions Q2 and Q8) and `Kỳ của số liệu` (#4, decision Q31). The header of `Minh chứng/Ghi chú` changed from `Ghi chú`, and status moved from bare text to a coloured `Badge`. The `min-width` values are the approved prototype's own, declared inline on each `<th>` (`doc/Design/Frontend/PlatformManager/Prototypes/index.html` § `app-criteria-grid-table` → `<thead>`) — read them there rather than trusting a total written into prose, which goes stale the first time one column moves.

    **`Kỳ của số liệu` — the three rules it carries** (decisions Q31 and Q26, 2026-09-05; Q38, 2026-09-06):

    1. **It renders only while `Kỳ trong năm` is `Tất cả (mới nhất trong năm)`.** In that mode the grid shows the newest saved figure per criterion, so different rows can come from different weeks and this column is the only thing that says which. The approved prototype demonstrates exactly that — its six rows come from four different weeks, and since the 2026-09-06 sync pass it draws them in the Q38/T14 form: `10/08 – 16/08` · `10/08 – 16/08` · `27/07 – 02/08` · `20/07 – 26/07` · `13/07 – 19/07` · `10/08 – 16/08`. Pick one specific period and every row shares it, so the column is dropped rather than printing one value fourteen times over. The grid is therefore **14 columns in `Tất cả` mode and 13 in period mode**; the two frozen edges are the same columns either way.
    2. **It shows a date range and nothing else** — `10/08 – 16/08`, not `Tuần 33` and not `Tuần 33: 10/08 – 16/08/2026` (decision Q38, 2026-09-06). Two reasons, and the second is the load-bearing one: a bare range fits 110px without widening an already over-wide grid, and **every row in this column is a week** (decision Q37 allows writing to weeks only), so there is no second unit to tell it apart from. This is a deliberate, reasoned **exception to decision Q12** and it is recorded as one in § Normalize on redesign so that a later consistency pass does not "fix" it.

       Two details of the value, both settled by **decision T14, 2026-09-06**, and both easy to get wrong from a glance at a neighbouring surface:
       - **The dash keeps its spaces** — `10/08 – 16/08`, per decision T5, which Q38 did not reopen. Q38's subject was dropping the week *number*; the dash convention is product-wide, and width is not a tie-breaker here since both forms clear 110px comfortably.
       - **The year is dropped too**, which is where this differs from the history rows on the Dashboard (`10/08 – 16/08/2026`). Every row in this grid belongs to the year in the `Năm đánh giá` filter, so the year is already stated one control away and repeating it 62 times buys nothing. Change that filter and the whole column changes with it.
    3. **After a save, the cell switches to the current week.** Decision Q26 sends every write made in `Tất cả` mode to the *current* week rather than to the week the row was read from, and this cell is the entire user-visible evidence of that. A row that read `27/07 – 02/08` and now reads `10/08 – 16/08` has been written into this week — a different event from overwriting the older one — and the user has to be able to tell the two apart at the moment it happens, not afterwards from an audit trail.

    **Frozen edge columns** (decision Q30, 2026-09-05) — `Mã` pinned left, `Hành động` pinned right, both against the horizontal scroll. Fourteen columns fit no viewport this product targets, and the two that must survive the scroll are precisely the identifier and the actions: every other column is a value you read once, while those two answer *which row is this* and *what may I do to it*. The two inline-editable columns sit at #12 and #13, so a user who has scrolled far enough to edit has already lost sight of `Mã` — that is the failure Q30 removes. This is **a variant of the component, not a patch on this screen**: the bindings, the conditions for using it and the costs it brings are recorded in [`../Components/DataTable.md`](../Components/DataTable.md) § Frozen edge columns, extended for this purpose on 2026-09-05 and re-shaped on **2026-09-09** into the `frozenColumns` input, so this screen's caller states *which* columns are pinned without importing PrimeNG's `TableModule` to say it. The static prototype cannot render it — it has no PrimeNG — and says so in a comment inside `app-criteria-grid-table`
  - **Inline-edit affordance** — `.cell-editable`: `cursor: pointer`, a transparent 1px dashed bottom border that turns `colors.brand` on hover, and a 2px `colors.brand` focus ring at 2px offset (§ `app-criteria-grid-table .cell-editable`, `:hover`, `:focus-visible`). Each cell carries `tabindex="0"` and `role="button"`, so the two editable columns are reachable by keyboard. **All of that is conditional**: the affordance is present only when the row is actually writable — a week (or `Tất cả`) is selected *and* the user holds the write key. In the two read-only states (Q37, Q39) the cells render as ordinary cells with no underline, no tab stop and no tooltip, because an affordance that does nothing is worse than none. Editing swaps the span for `.cell-edit` — a flex row holding a narrow right-aligned number input (74px) or a full-width text input, both borrowing the global `Input` treatment
  - **Paginator** — the `DataTable` contract, unmodified: page buttons plus a rows-per-page select offering 10 / 20 / 50, **defaulting to 10** (decision Q19, matching the live `Quản trị người dùng` grid — one default across the product, and no widening of the component contract). There is **no** `Hiển thị 1–10 trong 62 bản ghi` line: decision Q16(c) removed it because the live Core grid declares `[paginator]`, `[rows]`, `[first]`, `[totalRecords]` and `[rowsPerPageOptions]` but **not** `showCurrentPageReport`, and [`../Components/DataTable.md`](../Components/DataTable.md) records exactly that contract. Turning it on is a one-binding change but it widens the component contract, so it must be decided for **every** grid at once — § Cần chốt
- **Four dialogs**, all native `<dialog>` per [`../Components/Dialog.md`](../Components/Dialog.md), belonging to this route rather than to routes of their own:
  1. **`Sửa chỉ tiêu` / `Thêm chỉ tiêu`** — `app-criteria-form-dialog`, the `.form-dialog` width variant. Two field groups. **Group 1, the criterion itself:** `Mã` (required, `maxlength 20`, at most 4 digits per dot-separated segment — decision **Q58**, 2026-09-10), `Tên chỉ tiêu` (required, `<textarea>`), then a `.form-grid` pair `Nhóm` (required; a select whose options read `Code. Name` — `1. Hạ tầng và Nền tảng số`, decision **Q42**) + `Điểm tối đa` (required, `type=number`, `min 0.01`, `step 0.01`). **Group 2, the period assessment — new here per decision Q9:** a `.form-grid` pair `Tự đánh giá` + `Thẩm định` (both `type=number`, `min 0`, `step 0.01`), a `.form-grid` pair `Trạng thái` (the four values) + `Hạn xử lý` (`type=date`), then `Phụ trách` (a select, default `— Chưa phân công —`) and `Minh chứng/Ghi chú` (`<textarea>`). Errors surface in a `.form-error` block above `.dialog-actions`. Footer: `Huỷ` + `.primary` `Lưu chỉ tiêu`. **`Chênh lệch` has no field** — it is computed, and giving it an input would make it enterable
  2. **Delete confirmation** — [`../Components/ConfirmDialog.md`](../Components/ConfirmDialog.md), `.confirm-dialog`, exactly two buttons: `Huỷ` + `.danger` `Xoá`
  3. **`Import CSV/Excel`** — `app-import-dialog`, `.form-dialog`. One file input accepting `.csv,.xlsx,.xls`, a `.muted` line echoing the chosen filename, then `Huỷ` + `.primary` `Nhập dữ liệu`. This is the real destination of the toolbar button
  4. **`Kết quả Import`** — `app-import-result-dialog`, `.form-dialog`. A `.import-summary` paragraph with the totals (successes in `.ok` green, failures in `.err` red) over a `<ul>` of per-row failures, each naming the row number, the code and the reason. One `.primary` `Đóng` button

<!-- Component gap — reviewed 2026-09-05. Every region composes from an indexed spec:
       Card    -> Components/Card.md            Toolbar   -> Components/Toolbar.md
       Input   -> Components/Input.md           DataTable -> Components/DataTable.md
       Table   -> Components/Table.md           Badge     -> Components/Badge.md
       Button  -> Components/Button.md          IconButton-> Components/IconButton.md
       FormRow -> Components/FormRow.md         Dialog    -> Components/Dialog.md
       ConfirmDialog -> Components/ConfirmDialog.md
       DeltaIndicator-> Components/DeltaIndicator.md
       NoticeBanner  -> Components/NoticeBanner.md
       Sidebar / Topbar / Toast -> their own specs.
     Genuinely screen-local and deliberately NOT promoted: `.cell-editable` /
     `.cell-edit` (the inline-edit affordance, two columns, one screen) and
     `.import-summary` (a results block inside one dialog). Promote `.cell-editable`
     the moment a second grid becomes inline-editable — a hand-copied dashed
     underline in two stylesheets is the drift the 2026-08-29 consolidation removed. -->

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

PASS = the G12 section reports no hits. Each row below needs a `danh-muc-dti.*` key in
`src/FE/public/i18n-app/vi.json` and an English sibling in `src/FE/public/i18n-app/en.json`
before the markup can pass.

🔄 **SỬA 2026-09-08.** The previous revision of this paragraph said *"No i18n layer exists in
the app"* and instructed hardcoding. It was already false when written — the i18n layer landed
2026-09-05 — and following it would have failed G12.

🔄 **SỬA 2026-09-10.** The two paragraphs above named the **Core** bundle
`src/FE/public/i18n/vi.json`. The project bundle landed 2026-09-09 and already carries a
`danh-muc-dti` group (`src/FE/public/i18n-app/vi.json:2-8`), so a key added to the Core bundle
would go in the wrong file. The rows below still read `key TBA`: the three keys allocated so far
(`routeTitle`, `title`, `hint.underConstruction`) belong to the skeleton page, not to this design.

Source for every row is `doc/Design/Frontend/PlatformManager/Prototypes/index.html`
§ `#screen-dti`; the region is named rather than a line number.

| Element | Verbatim copy | Localization key | Source region |
| --- | --- | --- | --- |
| Card heading | `Danh mục DTI` | key TBA | `.title` |
| Card count | `62 chỉ tiêu` (`aria-live="polite"`) | — (composed) | `.title` |
| ~~Read-only banner~~ — **withdrawn 2026-09-05** | ~~`Đang xem dữ liệu lịch sử — chỉ đọc. Quay lại "Tất cả (mới nhất trong năm)" của năm hiện tại để chỉnh sửa.`~~ | — | The prototype still draws this sentence; decision **Q20** makes past periods editable, so it now states the opposite of the rule. **Do not ship this string.** Its replacement is the target-period banner two rows below, settled 2026-09-06 |
| Search placeholder | `Tìm mã hoặc tên chỉ tiêu...` (three dots, not an ellipsis character) | key TBA | `.input-icon.search` |
| Search accessible name | `Tìm mã hoặc tên chỉ tiêu` | key TBA, on `aria-label` | `.input-icon.search` |
| Filter trigger | `Lọc` + a `.filter-count` badge reading `1` in the approved state | key TBA + computed | `<summary class="btn">`; the badge counts **non-default** conditions only — decision T8 |
| Filter condition 1 | `Nhóm chỉ tiêu` → `Tất cả nhóm` + the six group names, each as `Code. Name` — `1. Hạ tầng và Nền tảng số` … `6. Hoạt động Xã hội số` (decision Q42) | key TBA + server data (`Code` + `Name`, composed) | `.filter-panel` |
| Filter condition 2 | `Trạng thái` → `Tất cả trạng thái` · `Chưa thực hiện` · `Đang thực hiện` · `Cần bổ sung minh chứng` · `Hoàn thành` | key TBA | `.filter-panel` |
| Filter condition 3 | `Năm đánh giá` → `2026` · `2025` · `2024` | — (server values) | `.filter-panel` |
| Filter condition 4 | `Kỳ trong năm` → `Tất cả (mới nhất trong năm)` · `Tuần 33: 10/08 – 16/08/2026` · `Tuần 34: 17/08 – 23/08/2026` · `Tuần 35: 24/08 – 30/08/2026` · `Tháng 8: 01/08 – 31/08/2026` · `Tháng 7: 01/07 – 31/07/2026` | — (composed) | `.filter-panel` |
| Filter condition 4, help text | `Xem tổng hợp cả năm (mới nhất mỗi chỉ tiêu) hoặc 1 kỳ cụ thể đã lưu trong năm. Mỗi kỳ ghi rõ từ ngày nào tới ngày nào` | key TBA, on `title` | `.filter-panel`. ⚠️ Recorded as approved; it predates decision Q37 and so says nothing about a month selection making the grid read-only. The banner carries that, not this tooltip — a `title` is invisible on a phone and to most screen-reader flows, so it is the wrong place for a rule |
| Filter footer | `Xoá lọc` · `Áp dụng` | key TBA | `.filter-foot` |
| Applied-condition chip | `Nhóm: 1. Hạ tầng và Nền tảng số` — **one chip, not two**; the group reads `Code. Name` here too (decision Q42) | — (composed) | `.filter-chips`; `Năm: 2026` raises no chip, the current year being the default — decision T8 |
| Chip remove button | `Bỏ lọc Nhóm: 1. Hạ tầng và Nền tảng số` (`aria-label`, composed as `'Bỏ lọc ' + chip label`), a `pi pi-times` glyph, **no `title`** | — (composed by `<app-toolbar>`) | `src/FE/src/app/shared/components/toolbar/toolbar.html:42-43` — **not** the prototype, which writes `Gỡ điều kiện…` and a bare `×` (decision T2) |
| Toolbar action 1 | `Import CSV/Excel` | key TBA | `.toolbar-actions` |
| Toolbar action 2 | `+ Thêm chỉ tiêu` | key TBA — the `+` is part of the label | `.toolbar-actions` |
| Grid headers | `Mã` · `Tên` · `Nhóm` · `Kỳ của số liệu` · `Điểm tối đa` · `Tự đánh giá` · `Thẩm định` · `Chênh lệch` · `Trạng thái` · `Phụ trách` · `Hạn xử lý` · `Tiến độ %` · `Minh chứng/Ghi chú` · `Hành động` | key TBA | `<thead>`; `Kỳ của số liệu` is present only in `Tất cả` mode — decision Q31 |
| Period cell value | `10/08 – 16/08` · `27/07 – 02/08` · `20/07 – 26/07` · `13/07 – 19/07` — the week's **date range only**, with no week number and no year | — (composed) | `Kỳ của số liệu` column. Decision **Q38** — a deliberate exception to Q12, reasoned in § Layout Blueprint rule 2 and recorded in § Normalize |
| Aggregate read-only banner (a month selected, **current year**) | `Đang xem số liệu tổng hợp của Tháng 8/2026 (01/08 – 31/08/2026) — chỉ đọc. Chọn một tuần cụ thể hoặc "Tất cả (mới nhất trong năm)" trong ô "Kỳ trong năm" để nhập hoặc sửa số liệu.` **ĐỀ XUẤT — CHỜ DUYỆT (Q60, 2026-09-10)** | — (composed) | Decision **Q60**: in the current year `Tất cả` is writable (case (d)), so the way out names both. Not drawn in the prototype. § States |
| Aggregate read-only banner (a month selected, **past year**) | `Đang xem số liệu tổng hợp của Tháng 8/2025 (01/08 – 31/08/2025) — chỉ đọc. Chọn một tuần cụ thể trong ô "Kỳ trong năm" để nhập hoặc sửa số liệu.` | — (composed) | **Written by this spec 2026-09-06 for decision Q37; not drawn in the prototype.** 🔄 LẬT 2026-09-10 (Q60): this was the one wording for a month in any year; it is now the past-year wording only, because `Tất cả` of a past year is read-only (T15) and must not be offered. The wording is unchanged — only the composed example period is now a 2025 month. § States |
| Past-year read-only banner (a past year **with `Tất cả`** — case (a)) | `Đang xem số liệu năm 2025 — chỉ đọc. Chọn một tuần cụ thể trong ô "Kỳ trong năm" để nhập hoặc sửa số liệu của tuần đó, hoặc chuyển ô "Năm đánh giá" về 2026.` | — (composed) | **Written by this spec for decision T15; not drawn in the prototype. Wording approved verbatim, duyệt 2026-09-10 (Q50).** It names both ways out, because after Q41 a specific week of the past year is writable too. Both years are composed from the filter and the clock. The superseded wording is recorded once, in the note below this table. § States |
| Target-period banner (a past week selected) | `Đang nhập cho Tuần 31/2026 (27/07 – 02/08/2026). Số liệu bạn sửa sẽ lưu vào tuần này, không phải tuần hiện tại.` | — (composed) | **Settled 2026-09-06 for decision Q20; written by this spec, not drawn in the prototype.** Moved here from § Cần chốt on 2026-09-09. § Layout Blueprint → Period banners |
| Target-period banner (`Tất cả (mới nhất trong năm)` selected) | `Đang nhập cho Tuần 33/2026 (10/08 – 16/08/2026) — tuần hiện tại.` | — (composed) | The same sentence with the contrast clause dropped, there being nothing to contrast with. Rendered in case **(d)** of § Layout Blueprint → Period banners — settled 2026-09-09, see the note below the table |
| Empty-catalogue banner | `Danh mục chưa có chỉ tiêu nào. Dùng nút "Import CSV/Excel" ở trên để nhập danh mục từ file.` | key TBA | **Written by this spec on 2026-09-05 for decision T9 — not drawn in the prototype.** See § States |
| Status values (the complete set of four) | `Chưa thực hiện` · `Đang thực hiện` · `Cần bổ sung minh chứng` · `Hoàn thành` | — (server values) | `app-status-badge` |
| Empty assignee / deadline cell | `—` (em dash) | key TBA | `<tbody>` |
| Empty note cell, inline-edit prompt | `— bấm đúp để ghi chú` | key TBA | `.cell-editable` |
| Inline-edit tooltips | `Bấm đúp để sửa Tiến độ %` · `Bấm đúp để sửa Minh chứng/Ghi chú` | key TBA, on `title` | `.cell-editable` |
| Row actions | `Sửa` · `Xoá` | key TBA | `Hành động` column |
| Rows-per-page select | `Số dòng mỗi trang` (accessible name) → `10` · `20` · `50` | key TBA | paginator |
| Form dialog heading | `Sửa chỉ tiêu` (edit) / `Thêm chỉ tiêu` (create) | key TBA | `app-criteria-form-dialog` |
| Form dialog close | `Đóng` | key TBA | `.title` |
| Form labels | `Mã` · `Tên chỉ tiêu` · `Nhóm` · `Điểm tối đa` · `Tự đánh giá` · `Thẩm định` · `Trạng thái` · `Hạn xử lý` · `Phụ trách` · `Minh chứng/Ghi chú` | key TBA | `.form-row` labels |
| Required marker | `*` in `.required`, ink `colors.bad` | key TBA | `.form-row` labels |
| Form placeholders | `vd 1.1` · `Nhập tên đầy đủ chỉ tiêu...` · `vd 10` · `vd 5` · `vd 0` · `Số văn bản - ngày - trích yếu, mỗi minh chứng 1 dòng...` | key TBA | `app-criteria-form-dialog` |
| Assignee, unassigned option | `— Chưa phân công —` | key TBA | `app-criteria-form-dialog` |
| Group select options (form dialog) | the six groups as `Code. Name` — `1. Hạ tầng và Nền tảng số` … `6. Hoạt động Xã hội số` (decision Q42) | — (server data, `Code` + `Name` composed) | `app-criteria-form-dialog`. **Not applied to import:** import matches a group by its bare `Name`, so the row error `Không tìm thấy nhóm "…"` keeps the bare name |
| Form validation error | `Mã chỉ tiêu "1.4" đã tồn tại trong danh mục.` — duyệt 2026-09-10 (Q56); câu cũ *"…trong nhóm này."* sai phạm vi, mã unique trên **toàn danh mục** chứ không theo nhóm (DM-3 `code`) | — (composed) | `.form-error`, currently a disabled branch |
| Form actions | `Huỷ` · `Lưu chỉ tiêu` | key TBA | `.dialog-actions` |
| Delete confirmation heading | `Xác nhận` | key TBA | `app-confirm-dialog` |
| Delete confirmation message | `Xoá chỉ tiêu "1.4 — Mức độ ứng dụng AI"?` | — (composed) | `.confirm-message` |
| Delete confirmation actions | `Huỷ` · `Xoá` | key TBA | `.dialog-actions` |
| Import dialog heading | `Import CSV/Excel` | key TBA | `app-import-dialog` |
| Import file label | `Chọn file CSV/Excel` | key TBA | `app-import-dialog` |
| Import chosen-file line | `Đã chọn: <filename>` in `.muted` | — (composed) | `app-import-dialog` |
| Import actions | `Huỷ` · `Nhập dữ liệu` | key TBA | `.dialog-actions` |
| Import result heading | `Kết quả Import` | key TBA | `app-import-result-dialog` |
| Import result summary | `Tổng 62 dòng — 59 thành công, 3 lỗi. Đã tự tạo mới 2 chỉ tiêu.` | — (composed) | `.import-summary` |
| Import row error — score over max | `Dòng 17 — mã "4.2": Điểm tự đánh giá (6) vượt quá điểm tối đa (5).` | — (composed) | `.import-summary` `<li class="err">` |
| Import row error — unknown status | `Dòng 42 — mã "4.15": Trạng thái "Đang xử lý" không thuộc 4 giá trị hợp lệ.` | — (composed) | `.import-summary` `<li class="err">` |
| Import row error — unknown group | `Dòng 55 — mã "6.3": Không tìm thấy nhóm "Xã hội số" (sai chính tả hoặc thừa khoảng trắng).` | — (composed) | `.import-summary` `<li class="err">` |
| Import result action | `Đóng` | key TBA | `.dialog-actions` |
| Shell copy | unchanged from the live shell | — | see [`../Components/Sidebar.md`](../Components/Sidebar.md), [`../Components/Topbar.md`](../Components/Topbar.md) and [`../Components/Toast.md`](../Components/Toast.md) — the shell's copy belongs to those three, not to a screen spec |

> 🗄️ **Superseded — do not ship (Q50, 2026-09-10):** from 2026-09-06 the past-year banner read *Đang xem số liệu năm 2025 — chỉ đọc. Chuyển ô "Năm đánh giá" về 2026 để nhập hoặc sửa số liệu.*, which named only the year switch as the way out.

**Copy notes.** Every period label in the filter panel carries its full date range,
and this is the one screen where the **week** and **month** forms sit in the same
list — `Tuần 33: 10/08 – 16/08/2026` beside `Tháng 8: 01/08 – 31/08/2026` — which
is why the prototype uses it to demonstrate both. The dash is the **spaced** form
throughout, per decision T5.

> 📖 **The period-label *rule* is not owned by this file.** Which dates a week or a
> month spans, and the format each label takes, live in
> `spec/dashboard-dti/business-rules.md`. This spec records the shipped strings, not
> how they are built.

The separator between the period name and its range is a **colon** here and a
**middle dot** on the Dashboard's period select. T5 settled the dash; it did not
settle this. Recorded as-drawn and carried in § Normalize on redesign.

> ### ✅ CHỐT 2026-09-09 — case (d) was two situations wearing one label, and it is now split
>
> This file used to contradict itself: § Layout Blueprint said the current week **and**
> `Tất cả` show no banner, while the 2026-09-06 settlement supplied the
> `— tuần hiện tại.` string for exactly that case. The contradiction came from one case
> merging two situations that behave differently:
>
> | `Kỳ trong năm` | What the grid shows | Where a write lands | Banner |
> | --- | --- | --- | --- |
> | **the current week** | week 33's figures | week 33 | **no** — you edit what you are looking at, so there is nothing to warn about |
> | **`Tất cả (mới nhất trong năm)`** | one row per criterion, each from whichever week it was last saved in — week 29, 31, 33 … | **the current week** | **yes** |
>
> The second row is why the banner has to exist. `spec/danh-muc-dti/business-rules.md`
> §5.3 step 1 sends a write carrying `period = "all"` to **the ISO week containing
> today**, so a user editing a row that displays week 29's number puts that value into
> week 33 — and nothing else on screen says so.
>
> **This mechanism has been named once before.** Decision **T15** (2026-09-06, in the
> same §5.3) found the cross-year version of it and wrote the sentence that fits this
> case word for word: *"Bộ chọn kỳ nói 'ghi được', còn lệnh ghi thì đi chỗ khác."* T15
> closed the cross-year half with a refusal,
> `400 CRITERIA.ASSESSMENT_PERIOD_OUT_OF_YEAR`. The within-year half is **not** a defect
> and no error code blocks it — it is the designed behaviour of `Tất cả` — so its remedy
> is a warning rather than a refusal, and this banner is the warning. Decision **Q26**
> completes the pair from the other end: after a save the `Kỳ của số liệu` cell switches
> to the current week, so the banner warns before and the column confirms after.

The three import error messages are the design's statement of what import
validates: a score above its maximum, a status outside the fixed set of four, and
an unmatched group name. The rules themselves belong to
`spec/danh-muc-dti/business-rules.md`, not here; this file records only how a
failure is presented.

#### Error codes → copy — duyệt 2026-09-10 (Q56)

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Câu chữ đã được người dùng **duyệt nguyên văn 2026-09-10 (Q56)**; chưa màn nào dựng. Written for decision Q56 from
> `doc/contracts/danh-muc-dti.md` — the error lists of DM-2 to DM-7 and § 2 Catalog mã lỗi.
> **None of these keys exists in `src/FE/public/i18n-app/`** today, and nothing here ships
> before the product owner approves it. The three approved import-row sentences in the table
> above stay exactly as they are.

**Keys.** A `businessCode` is its own translation key, nested by domain, in the project
bundle `src/FE/public/i18n-app/{vi,en}.json` (`doc/huong_dan/wiki-core/fe/08-i18n.md` § 1 —
Mã lỗi BE dùng THẲNG làm khoá; the lookup is
`src/FE/src/app/core/i18n/api-error-message.service.ts:94-98`). Placeholders use
ngx-translate's `{{Name}}` form, and every name is the contract's `messageParams` key, so
nothing has to be mapped. An import row's `rowNumber` is a field of its own, not a
parameter, so the `Dòng N — mã "X": ` prefix of the approved sentences is FE-owned —
proposed keys `danh-muc-dti.import.rowPrefix` = `Dòng {{rowNumber}} — mã "{{Code}}": ` and
`danh-muc-dti.import.rowPrefixNoCode` = `Dòng {{rowNumber}}: `.

**Import-row codes** — `result.errors[]`, one line each in the `Kết quả Import` dialog:

| Code | Proposed body, after the row prefix | Rendered example | `messageParams` |
| --- | --- | --- | --- |
| `IMPORT.ROW_SELF_SCORE_EXCEEDS_MAX` | approved copy, key only proposed: `Điểm tự đánh giá ({{SelfScore}}) vượt quá điểm tối đa ({{MaxScore}}).` | `Dòng 17 — mã "4.2": Điểm tự đánh giá (6) vượt quá điểm tối đa (5).` | `Code`, `SelfScore`, `MaxScore` |
| `IMPORT.ROW_STATUS_INVALID` | approved copy, key only proposed: `Trạng thái "{{Status}}" không thuộc 4 giá trị hợp lệ.` | `Dòng 42 — mã "4.15": Trạng thái "Đang xử lý" không thuộc 4 giá trị hợp lệ.` | `Code`, `Status` |
| `IMPORT.ROW_GROUP_NOT_FOUND` | approved copy, key only proposed: `Không tìm thấy nhóm "{{GroupName}}" (sai chính tả hoặc thừa khoảng trắng).` | `Dòng 55 — mã "6.3": Không tìm thấy nhóm "Xã hội số" (sai chính tả hoặc thừa khoảng trắng).` | `Code`, `GroupName` |
| `IMPORT.ROW_VERIFIED_SCORE_EXCEEDS_MAX` | `Điểm thẩm định ({{VerifiedScore}}) vượt quá điểm tối đa ({{MaxScore}}).` | `Dòng 18 — mã "4.2": Điểm thẩm định (6) vượt quá điểm tối đa (5).` | `Code`, `VerifiedScore`, `MaxScore` |
| `IMPORT.ROW_CODE_DUPLICATED_IN_FILE` | `Mã này đã xuất hiện ở dòng {{FirstRowNumber}} của cùng file.` | `Dòng 30 — mã "4.2": Mã này đã xuất hiện ở dòng 12 của cùng file.` | `Code`, `FirstRowNumber` |
| `IMPORT.ROW_CODE_MISSING` | `Chưa có mã chỉ tiêu.` — with the no-code prefix, since the contract sends no `Code` | `Dòng 23: Chưa có mã chỉ tiêu.` | none — `rowNumber` only |
| `IMPORT.ROW_CODE_SEGMENT_TOO_LONG` — decision **Q58**, 2026-09-10 | `Mỗi đoạn của mã (phần giữa các dấu chấm) chỉ được có tối đa 4 chữ số.` | `Dòng 31 — mã "4.12345": Mỗi đoạn của mã (phần giữa các dấu chấm) chỉ được có tối đa 4 chữ số.` | `Code` **assumed**, like every other `ROW_` code that carries one — the contract had not declared this code yet when read on 2026-09-10 |

**Request-level codes** — the whole request fails; the envelope carries the code:

| Code | Where it shows | Proposed copy (vi) | `messageParams` |
| --- | --- | --- | --- |
| `IMPORT.FILE_MISSING` | `.form-error`, `Import CSV/Excel` dialog | `Chưa chọn file. Chọn một file CSV hoặc Excel rồi bấm Nhập dữ liệu.` | none |
| `IMPORT.FILE_EMPTY` | `.form-error`, `Import CSV/Excel` dialog | `File không có dòng dữ liệu nào. Kiểm tra lại file rồi chọn lại.` | none |
| `IMPORT.FORMAT_UNSUPPORTED` | `.form-error`, `Import CSV/Excel` dialog | `File này không phải CSV hoặc Excel (.csv, .xlsx, .xls), kể cả khi đuôi tên file đúng. Lưu lại đúng định dạng rồi chọn lại.` | none |
| `IMPORT.FILE_TOO_LARGE` | `.form-error`, `Import CSV/Excel` dialog | `File quá lớn để nhập. Bỏ bớt ảnh nhúng hoặc định dạng trong file, hoặc chia thành nhiều file, rồi chọn lại.` | none declared — the ceiling is configuration, so the sentence names no number; showing one needs a parameter the contract does not declare |
| `IMPORT.JOB_NOT_FOUND` | toast, while waiting for the result | `Không tìm thấy lượt nhập này — có thể đã quá thời gian lưu. Hãy nạp lại file.` | none |
| `CRITERIA.DUPLICATE_CODE` | `.form-error`, `Sửa chỉ tiêu` / `Thêm chỉ tiêu` | `Mã chỉ tiêu "{{Code}}" đã tồn tại trong danh mục.` — duyệt 2026-09-10 (Q56), thay câu cũ *"…trong nhóm này."*: contract quy định mã unique trên **toàn danh mục**, không phải theo nhóm (DM-3 `code`) | `Code` — the name used by the catalog's `MessageTemplate` (§ 2) |
| `CRITERIA.GROUP_NOT_FOUND` | `.form-error`, form dialog | `Nhóm đã chọn không còn trong danh mục. Chọn lại nhóm rồi lưu.` | none declared |
| `CRITERIA.OWNER_NOT_FOUND` | `.form-error`, form dialog | `Người phụ trách đã chọn không còn trong hệ thống. Chọn lại người phụ trách rồi lưu.` | none declared |
| `CRITERIA.ASSESSMENT_SELF_SCORE_EXCEEDS_MAX` | `.form-error`, form dialog | `Điểm tự đánh giá không được lớn hơn điểm tối đa.` | none declared |
| `CRITERIA.ASSESSMENT_VERIFIED_SCORE_EXCEEDS_MAX` | `.form-error`, form dialog | `Điểm thẩm định không được lớn hơn điểm tối đa.` | none declared |
| `CRITERIA.ASSESSMENT_CONFLICT` | `.form-error` in the dialog; a toast after an inline edit | `Chưa lưu được — chỉ tiêu này vừa được người khác cập nhật. Mở lại để xem số liệu mới nhất rồi sửa lại.` | none declared |
| `CRITERIA.NOT_FOUND` | toast after `Sửa`, `Xoá` or an inline save; `.form-error` if the dialog is open | `Chỉ tiêu này không còn trong danh mục — có thể người khác vừa xoá.` | none declared |

🔄 **LẬT 2026-09-10 (Q62):** `CRITERIA.STATUS_INVALID` and `CRITERIA.ASSESSMENT_PERIOD_INVALID`
were rows of this table, because a stale or hand-edited link could carry a bad `status` or
`period`. Q62 has the page reset an unknown URL value to its default, rewrite the URL and
never call the API with the bad value — silently — so neither code can reach a user, and
both left the table.

**Left out, and why:**

- `CRITERIA.CODE_REQUIRED`, `CRITERIA.CODE_TOO_LONG`, `CRITERIA.NAME_REQUIRED`,
  `CRITERIA.MAX_SCORE_INVALID` — the form's native `required`, `maxlength 20` and `min 0.01`
  stop the submit first (§ States → validation).
- `CRITERIA.PROGRESS_PERCENT_INVALID` — the FE clamps to `[0, 100]` before sending (DM-6).
- `CRITERIA.ASSESSMENT_PERIOD_REQUIRED`, `IMPORT.PERIOD_REQUIRED`, `IMPORT.PERIOD_INVALID` —
  the FE always sends the filter's own value.
- `CRITERIA.ASSESSMENT_PERIOD_NOT_WEEKLY` / `…_OUT_OF_YEAR` and `IMPORT.PERIOD_NOT_WEEKLY` /
  `…_OUT_OF_YEAR` — in those states the write controls are disabled and the period banners
  (Q37, T15 with Q50) already say why; the contract maps these codes onto the same
  `editBlockedBy` reasons (DM-2).
- `403` without the write key — Core's HTTP fallback copy, and Q39 hides the controls anyway.

**An import job that ends `Failed`** carries no code, and its `errorMessage` is
developer-facing and must not be shown (DM-7, step 2). What the user sees — **ĐỀ XUẤT — CHỜ
DUYỆT (Q56)** — is a toast, while waiting for the result:
`Lượt nhập không hoàn tất — hệ thống gặp sự cố khi xử lý file. Kiểm tra lại file rồi nạp lại; nếu vẫn không được, báo cho quản trị hệ thống.`
With no `businessCode` to serve as the key, the key is FE-owned: proposed
`danh-muc-dti.import.jobFailed`. 🔄 SỬA 2026-09-10: this used to read *"not settled here"*.

### States

<!-- How each state renders: default / loading / empty / error / validation display. -->

- **default (the current week is selected and the user may write):** every write affordance is live — `+ Thêm chỉ tiêu`, `Import CSV/Excel`, the two inline-editable columns, and the per-row `Sửa` / `Xoá`. **No banner** — this is the one writable state that needs none, because what is displayed and what a write lands on are the same period (case **(e)** of § Layout Blueprint → Period banners, settled 2026-09-09). Decision **Q37** makes the *unit* part of this condition, not just the date: **a week is the only kind of period anything can be written to.**
- **editing a past week:** ⚠️ **Decision Q20 reversed this state; decision Q37 then narrowed it to weeks.** A past *week* is fully editable — **of the current year or of any earlier one** (decision **Q41**, 2026-09-10, restating Q27: *every period, past periods and earlier years included*) — the user picks it in the `Kỳ trong năm` filter and every write lands on *that* week, not on today. The write affordances stay live and the same controls do the same jobs; what changes is only that a `NoticeBanner` appears naming the target period, because the one thing the user must not be able to do is enter data believing it is going somewhere else.

  **Age is no longer a reason for read-only on this screen** — that was the state Q20 removed, and the prototype's sentence saying otherwise is withdrawn. Read-only does still exist here, **three times**, and none of the three is about age: the period *unit* (Q37, a month in any year), `Tất cả` in a past year, whose write would land in a year that is not on screen (T15), and the user's *permission* (Q39). A specific week of a past year is not among them (Q41). 🔄 **SỬA 2026-09-10:** this sentence used to say *"twice, for two reasons"* and left T15 out, contradicting § States → access below, which counts three.

  Two consequences the design has to carry rather than hide, both flagged for the rules owner:
  - **A report already exported can stop matching the data.** Editing a closed period changes figures that someone may have downloaded, and the `.xlsx` carries no revision marker (see [`01-dashboard.md`](./01-dashboard.md) § Layout).
  - **Who changed which period must be traceable**, and the audit fields already on every entity are the intended mechanism — no new screen surface is specified for it here.

  > 📖 The rule itself — what "the target period" means for a write, how the audit trail records it, whether anything at all is frozen — is owned by `spec/danh-muc-dti/business-rules.md` and `doc/contracts/danh-muc-dti.md`. This spec records only the screen surface.
- **`Tất cả (mới nhất trong năm)` is selected — writable, and the banner is not optional here (settled 2026-09-09):** the grid shows the newest saved figure per criterion, so different rows come from different weeks (§ Layout Blueprint → `Kỳ của số liệu`, decision Q31), while **every write goes to the ISO week containing today** (`spec/danh-muc-dti/business-rules.md` §5.3 step 1, `period = "all"`). Editing a row that displays week 29's number therefore writes week 33. Nothing is disabled and every write affordance stays live; the `NoticeBanner` in its `— tuần hiện tại.` form is the whole mechanism keeping a user from entering data in the belief that it lands where they are looking. The reasoning and the T15 precedent are under § Copy. This state cannot combine with a past year — T15 makes `Tất cả` outside the current year read-only, two bullets below.
- **a month is selected, in any year — the aggregate view, read-only (decision Q37, 2026-09-06):** `Kỳ trong năm` still lists months beside weeks, but choosing one puts the grid into a read-only state. `+ Thêm chỉ tiêu` and `Import CSV/Excel` stay **visible and disabled** (`Button` § States → disabled: `opacity: .5`, `cursor: not-allowed`); the per-row `Sửa` / `Xoá` do the same; the two `.cell-editable` columns lose their affordance for the duration. Search, all four filter conditions, sorting, paging and `Xuất báo cáo` are untouched — the month export is a first-class feature (decision Q15) and does not depend on this.

  **Why the unit decides this rather than a permission.** A month is not a period anything is saved *to*; it is the sum of the weeks inside it. Accepting a keystroke in a month view forces the app to invent which week the number belongs to, and the obvious guess is wrong in a way nobody would catch: anchoring `Tháng 8/2026` at its last day, `31/08`, lands in **ISO week 36**, a week that is in September. Q37 deletes the guess instead of making it safer — writes go to weeks, months are computed from them.

  A `NoticeBanner` (default severity, `pi pi-info-circle`) says so, in one of two wordings chosen by the year, because the way out it names has to be one that works (decision **Q60**, 2026-09-10):

  - **current year** — `Tất cả` is writable here (case (d)), so both ways out are named. **ĐỀ XUẤT — CHỜ DUYỆT (Q60):**

    > `Đang xem số liệu tổng hợp của Tháng 8/2026 (01/08 – 31/08/2026) — chỉ đọc. Chọn một tuần cụ thể hoặc "Tất cả (mới nhất trong năm)" trong ô "Kỳ trong năm" để nhập hoặc sửa số liệu.`

  - **past year** — `Tất cả` of a past year is read-only (T15), so only a specific week is named. Verbatim copy:

    > `Đang xem số liệu tổng hợp của Tháng 8/2025 (01/08 – 31/08/2025) — chỉ đọc. Chọn một tuần cụ thể trong ô "Kỳ trong năm" để nhập hoặc sửa số liệu.`

  🔄 **LẬT 2026-09-10 (Q60):** one wording — the second one, then shown with a 2026 month — used to cover a month in any year.

  The period name and its range are composed from the selection, in the format decision T5 fixed. **This is not the withdrawn banner of the prototype**: that one said *history* is read-only, which Q20 made false. This one says an *aggregate* is read-only, which is a property of the unit and will not change.
- **a past year with `Tất cả (mới nhất trong năm)` — read-only (decision T15, 2026-09-06; scope confirmed by Q41, 2026-09-10):** set `Năm đánh giá` to a past year **and leave `Kỳ trong năm` at `Tất cả`**, and the grid behaves exactly as the month case above — `+ Thêm chỉ tiêu` and `Import CSV/Excel` visible and disabled, row actions disabled, inline editing off — with its own banner. Reading, filtering, sorting, paging and export are untouched. The same past year with **a specific week** selected is **writable**: case (c), with the target-period banner (Q41). With **a month** selected it is the month case above (Q37), blocked by that case's single code `PERIOD_NOT_WEEKLY` and not by a second one: `PERIOD_OUT_OF_YEAR` belongs to `Tất cả` alone (decision **Q48**, 2026-09-10).

  🔄 **LẬT 2026-09-10 (Q41).** This bullet used to read *"a past year is selected — read-only… set `Năm đánh giá` to anything but the current year"*, which made every week of 2025 read-only. `doc/contracts/danh-muc-dti.md`, `spec/danh-muc-dti/ui-spec.md` and `spec/danh-muc-dti/business-rules.md` never said that; this file alone had widened T15 from the `Tất cả` shortcut to the whole year.

  **Why `Tất cả` in a past year is read-only when a week of that year is not.** (🔄 LẬT 2026-09-10, Q41: this heading read *"Why a whole year is read-only when a past week is not"*; the argument below it only ever covered `Tất cả`.) One rule runs through every period decision in this design: **a write must never land in a period the user cannot see on screen.** Q26 honours it by showing the destination in the `Kỳ của số liệu` cell; Q37 honours it by deleting the month unit rather than guessing a week inside it. Viewing 2025 with `Kỳ trong năm` at `Tất cả` breaks it worse than either: the write would land in the current week of **2026**, a year that is not on screen anywhere. Picking a specific week, a week of 2025 included, is different and stays editable: that week *is* on screen, named in the filter (decisions Q20 and Q41).

  Verbatim copy, duyệt 2026-09-10 (Q50):

  > `Đang xem số liệu năm 2025 — chỉ đọc. Chọn một tuần cụ thể trong ô "Kỳ trong năm" để nhập hoặc sửa số liệu của tuần đó, hoặc chuyển ô "Năm đánh giá" về 2026.`

  It names both ways out, because after Q41 a specific week of the past year is writable too. Both years are composed from the filter and the clock. The 2026-09-06 wording it replaces offered only the year switch; it is recorded once under § Copy as superseded and must not ship.

  **`isEditable` has three conditions, not two**, and all three must hold: the user holds the DTI write key (Q39) · the selected period is **not a month** (Q37) · **`Tất cả` only in the current year** (T15; a specific week of a past year passes, Q41). 🔄 **LẬT 2026-09-10 (Q41):** the third condition read *"the selected **year is the current year**"*, which failed every week of a past year. The rule itself belongs to `spec/danh-muc-dti/business-rules.md`; what this file fixes is that the screen shows *which* condition failed, because "the buttons are grey" is not an explanation.
- **the user has no DTI write permission — read-only, but not locked out (decision Q39, 2026-09-06):** a signed-in account without the write key **reaches this screen normally**. The route is not guarded, the sidebar entry is not hidden, nothing redirects. What it loses is every affordance that writes:

  | Hidden | Kept |
  | --- | --- |
  | `+ Thêm chỉ tiêu` and `Import CSV/Excel` in `.toolbar-actions` | search, all four filter conditions, the chips, sorting, paging |
  | the `Sửa` and `Xoá` buttons in the `Hành động` column | the column itself is dropped with them — an empty frozen column would be a permanent 120px of nothing |
  | the `.cell-editable` treatment on `Tiến độ %` and `Minh chứng/Ghi chú` — no dashed underline, no `tabindex="0"`, no `role="button"`, no tooltip | the two cells and their values, as ordinary cells |
  | — | `Xuất báo cáo` on the Dashboard, which never depended on this key |

  With both buttons gone `.toolbar-actions` is empty and the shared rule `:empty { display: none }` collapses it ([`../Components/Toolbar.md`](../Components/Toolbar.md) § Anatomy), so the bar keeps search, filter and chips without leaving a gap.

  **Hidden here, disabled in the aggregate state above — the difference is deliberate.** A disabled control says *"not right now"*; an absent one says *"not for you"*. Viewing a month is a thing the user can undo in one click, so the buttons stay and dim. Lacking the permission is not, so they go.

  **Why the route is not guarded at all.** The Dashboard already shows every criterion, every score and every status to any signed-in account (decision Q21). Blocking this screen would therefore hide nothing — the same 62 rows are one click away on the landing page. The only thing the write key really protects is the ability to *change* them, so that is the only thing taken away. Hiding the menu row too would be worse than useless: it would make a readable screen unreachable while its data stayed public.

  **No banner and no explanatory copy in this state, deliberately** — decision **Q51**, 2026-09-10, confirming Q39 on exactly this point. A user who never had the permission does not need to be told on every visit that they lack it; the screen simply offers less. That is the opposite of the aggregate state above, where the user *does* have the right and needs to know why it is unavailable right now.
- **loading:** the grid pages server-side, so every filter change, sort, page change and rows-per-page change is a round trip. The `DataTable` contract owns the loading treatment and this screen adds none of its own — `p-table`'s `[loading]` mask, painted by the preset ([`../Components/DataTable.md`](../Components/DataTable.md) § Variants). **Decision Q34 is explicit that this screen is the exception**: the dim-and-spinner treatment it specifies applies to the Dashboard, which has no `p-table` to lean on, and must not be rebuilt here on top of a mask that already works. One loading mechanism per surface.
- **empty — no criteria match the filters:** the `DataTable` empty message, and the `.title` count drops to the filtered number. Because that count is the `aria-live` region, it is what tells a screen-reader user the filter ran.
- **empty — the catalogue is empty:** a fresh deployment, before the first import ever runs. Decision **T9** settles the treatment: a `NoticeBanner` at default (information) severity with the `pi pi-info-circle` glyph, **above the toolbar**, not a blank row inside the grid. Verbatim copy:

  > `Danh mục chưa có chỉ tiêu nào. Dùng nút "Import CSV/Excel" ở trên để nhập danh mục từ file.`

  Two states must not be confused here, and using the grid's own empty row for both is what would confuse them. *"Your filter matched nothing"* is the common case and keeps the `DataTable` empty message exactly as it is; *"this deployment has never had any data"* is a once-per-install case that needs a sentence and a route out, and it is symmetrical with the Dashboard's own first-run banner ([`01-dashboard.md`](./01-dashboard.md) § States). The banner points at `Import CSV/Excel` rather than at `+ Thêm chỉ tiêu` because typing dozens of criteria by hand is not a real way to start; the second button stays enabled all the same.

  **Combined with the no-permission state (Q39) the banner drops its second sentence**, leaving `Danh mục chưa có chỉ tiêu nào.` alone. A reader without the write key cannot see the `Import CSV/Excel` button, so pointing at it would name a control that is not on their screen — the one way an empty state can be worse than a blank grid.

  ⚠️ **This copy is written by this spec, not read off the prototype.** `doc/Design/Frontend/PlatformManager/Prototypes/index.html` § `#screen-dti` draws no empty state at all — T9 is dated 2026-09-05, after the prototype was approved.
- **after a save while `Kỳ trong năm` is `Tất cả`:** the row's `Kỳ của số liệu` cell switches to the current week's date range, and that switch is the whole of the feedback. Decision Q26 routes the write to the current week, decision Q31 makes the destination visible; there is no dialog and no confirmation step in between. The figures stay where the user typed them, only the range moves. § Layout Blueprint → `Kỳ của số liệu`, rule 3.
- **error:** a failed request surfaces as a `Toast` from the shell. In-dialog failures render in `.form-error` inside the dialog instead, so the user does not lose the form.
- **validation:** three surfaces, deliberately different.
  1. **Field level** — `.required` markers on the four mandatory fields; native constraints on the numeric inputs (`min 0.01` / `step 0.01` for `Điểm tối đa`, `min 0` / `step 0.01` for the two scores); `maxlength 20` on `Mã`, and at most **4 digits in each dot-separated segment** (decision **Q58**, 2026-09-10; the rule is owned by `spec/danh-muc-dti/business-rules.md`) — the form blocks both before submit. No sentence is written for either: the form's native constraints carry no copy in this spec. ⚠️ The code is **not** two-level: `4.22.11` occurs in the real data, so any pattern assuming `N.N` rejects valid codes.
  2. **Form level** — one `.form-error` block above the actions, e.g. a duplicate code within a group.
  3. **Import level** — per-row failures listed in the result dialog, each naming the row number, the code and the reason. Import is all-or-nothing per row, not per file: the summary reports successes and failures side by side.
- **access:** reading needs a signed-in account and nothing more, as everywhere in the product (decision Q21). **Writing is governed by a single DTI permission key** (decision **Q27**, 2026-09-05) covering **every period**, past weeks and weeks of earlier years included (restated by decision **Q41**, 2026-09-10) — no separate right for a closed period, and no notion of closing one. Every write affordance is gated by that one condition, so they go live or dark together. The route itself carries **no** guard (decision **Q39**) even though, unlike the Dashboard, it could carry one without a redirect loop; the read-only state above is what a user without the key gets instead. The key's name belongs to `doc/contracts/danh-muc-dti.md`. ⚠️ **Three independent conditions now produce read-only** — no write key (Q39), a month selected in any year (Q37), and `Tất cả` in a past year (T15, scoped by Q41) — and they compose, with one limit: the two period conditions never hold together, because `PERIOD_OUT_OF_YEAR` is raised for `Tất cả` only and a month is never `Tất cả`, so a month of a past year is blocked by Q37 alone (decision **Q48**, 2026-09-10). A permitted user viewing a month, or `Tất cả` in a past year, sees dimmed buttons and a banner saying which; an unpermitted user sees no buttons at all and no banner, whatever the period (Q51). Permission is tested first because it changes what exists, not merely what is enabled.
- **print:** the toolbar is `.no-print`. The grid's scroll cap truncates on paper exactly as the Dashboard's does — § Normalize on redesign.

### Responsive

<!-- Behavior per breakpoint. -->

- **≥981px (desktop default):** the page fills the viewport height; the grid scrolls inside its own region — sized by the `page-fill` ⇄ `grid-host` flex chain with `scrollHeight="flex"`, **not** by `--grid-h` ([`../Components/DataTable.md`](../Components/DataTable.md) § SỬA 2026-09-06) — while the card, toolbar and paginator stay put.
- **All widths — the grid scrolls horizontally, it does not restack.** Fourteen columns with explicit `min-width` values sum well past a laptop viewport, so horizontal scrolling is the design, not a failure. That is the `Table` / `DataTable` contract and it is why every column declares a minimum rather than a percentage. Since decision Q30 the two edge columns (`Mã`, `Hành động`) stay put while the middle scrolls, at every breakpoint — the pin has no responsive variant.
- **≤980px (tablet):** the shell's sidebar becomes an off-canvas drawer; the toolbar wraps per the global `.toolbar` responsive rule. The grid is unchanged.
- **≤560px (mobile):** the global `.form-grid` collapses from two columns to one, so the four paired fields in the `Sửa chỉ tiêu` dialog stack. `main` padding shrinks and the topbar hides the user name. The grid is still unchanged — on a 390px screen the user scrolls sideways through fourteen columns, which is the least comfortable moment in the whole product (§ Normalize on redesign). The frozen edges make this **worse, not better, on a phone**: `Mã` at 70px and `Hành động` at 120px are held permanently, leaving under 200px of scrollport between them. Q30 solves a desktop problem and pays for it here.
- **The screen owns no media query of its own.** Every breakpoint effect above comes from the shell or from the global layer — the pre-retirement stylesheet's own `@media` block was empty and was removed during the 2026-08-29 consolidation.

### Iconography

See [`../Icons.md`](../Icons.md) § Per-Action Map. The app loads **PrimeIcons v7**
globally and authors icons as `<i class="pi pi-*">` elements.

| Action | Icon | Placement |
| --- | --- | --- |
| Search the catalogue | `pi pi-search` (decorative) | Leading adornment inside `.input-icon.search` |
| Open the filter panel | `pi pi-filter` | Leading, inside `<summary class="btn">` |
| Remove one filter condition | `pi pi-times` | Inside each `.filter-chip`, on an `IconButton` (`toolbar.html:43`). The prototype draws a bare `×` character here; the component uses a real icon — decision T2 |
| Import a spreadsheet | — (text button) | `.toolbar-actions` |
| Add a criterion | `+` — **part of the label text**, not an icon | `.toolbar-actions`, on the `.primary` button |
| Edit a row | — (text button, `.btn.sm`) | `Hành động` column |
| Delete a row | — (text button, `.btn.sm.danger`) | `Hành động` column |
| Delete confirmation severity | `pi pi-trash` inside `.dialog-icon.bad` | `ConfirmDialog`, per its own spec |
| Shell icons (hamburger, collapse, sign out, toast dismiss) | `pi pi-bars` · `pi pi-angle-left` · `pi pi-sign-out` · `pi pi-times` | App shell |

**This screen introduces no new icon.** It deliberately uses **text buttons** for
its row actions while the Core `Quản trị người dùng` grid uses icon buttons for
the same jobs — that divergence is a recorded decision, not an oversight
(both patterns are drawn side by side in `doc/Design/Frontend/PlatformManager/Prototypes/index.html`: § `#screen-dti` row actions use `.btn.sm` / `.btn.sm.danger`, § `#screen-nguoi-dung` row actions use `.icon-btn.primary` / `.icon-btn.danger`): a
destructive action on a business record gets a word, a routine one on an admin
grid gets a glyph. It is still a divergence, and it is carried in § Normalize on
redesign so the next reviewer does not have to rediscover the reasoning.

The `+` in `+ Thêm chỉ tiêu` is a **character inside the label text**, not an icon
element — the same pattern `../Icons.md` § Legacy Exceptions records for the status
dot and the delta arrows. The filter chip's remove control is **not** in that
category: the shipped `<app-toolbar>` renders a real `pi pi-times` element there
(`toolbar.html:43`), and only the prototype writes it as a bare `×`.

### Screenshots

<!-- Refs into Assets/Screenshots/danh-muc-dti/ -->

**No screenshot of this design exists, and none can be captured**, because the
screen is not built. `Assets/Screenshots/danh-muc-dti/danh-muc-dti--desktop-1440.png`
is a capture of the **deleted** pre-2026-08-29 build. It shows an 11-column grid
with plain-text status and no `Chênh lệch` column; keep it for before/after
comparison and never cite it as evidence of this design.

Under `doc/Design/CLAUDE.md` § Rules the target is **one desktop shot per screen**.

Prerequisites, once the screen is built:

> 📖 Capture environment (server URLs, allowed ports, database setup): read [`../../../CLAUDE.md`](../../../CLAUDE.md) § Rules

| Screenshot path | Status | Capture instructions |
| --- | --- | --- |
| `Assets/Screenshots/danh-muc-dti/danh-muc-dti--desktop-1440.png` | **blocked — screen not built; name already taken** by the capture of the deleted build. Move or rename that file first | Route @ 1440×900, full page, sidebar expanded, current year, `Tất cả (mới nhất trong năm)`, no filter applied, grid scrolled to the left edge. `Tất cả` mode means this shot carries all **14** columns including `Kỳ của số liệu`. This is the one shot that must exist. |
| `Assets/Screenshots/danh-muc-dti/danh-muc-dti--period-mode--desktop-1440.png` | on demand | Same @ 1440×900 with a **specific period** chosen in `Kỳ trong năm` — the only shot of the 13-column form, with `Kỳ của số liệu` absent (decision Q31, rule 1). |
| `Assets/Screenshots/danh-muc-dti/danh-muc-dti--frozen-scroll--desktop-1440.png` | on demand | Same @ 1440×900 **scrolled to the right-hand end of the grid** — the only shot that proves the pinned `Mã` and `Hành động` columns (decision Q30); everything else looks identical to the shot above. |
| `Assets/Screenshots/danh-muc-dti/danh-muc-dti--filtered--desktop-1440.png` | on demand | Same @ 1440×900 with the group and year conditions applied — the only shot showing `.filter-count` and the two `.filter-chip`s together. |
| `Assets/Screenshots/danh-muc-dti/danh-muc-dti--edit-dialog--desktop-1440.png` | on demand | Same @ 1440×900 with `Sửa chỉ tiêu` open on a populated criterion — the only shot of the two-group form introduced by decision Q9. |
| `Assets/Screenshots/danh-muc-dti/danh-muc-dti--import-result--desktop-1440.png` | on demand | Same @ 1440×900 with `Kết quả Import` open after a file with deliberate errors — documents the per-row failure list. |
| `Assets/Screenshots/danh-muc-dti/danh-muc-dti--past-period--desktop-1440.png` | on demand | Same @ 1440×900 with a **past** period selected in `Kỳ trong năm` — the shot of the target-period banner with the write controls still live (decision Q20). |
| `Assets/Screenshots/danh-muc-dti/danh-muc-dti--month-readonly--desktop-1440.png` | on demand | Same @ 1440×900 with a **month** chosen in `Kỳ trong năm` — the aggregate read-only state (decision Q37): the banner, the two dimmed toolbar buttons and a grid with no editable cells. |
| `Assets/Screenshots/danh-muc-dti/danh-muc-dti--no-write-permission--desktop-1440.png` | on demand | Same @ 1440×900 signed in **without** the DTI write key (decision Q39) — the toolbar with no `.toolbar-actions` at all and the `Hành động` column gone. Pairs with the shot above to show why one state dims and the other hides. |
| `Assets/Screenshots/danh-muc-dti/danh-muc-dti--mobile-390.png` | on demand | @ 390×844 — shows the horizontal-scroll reality of fourteen columns on a phone, and how little scrollport the two frozen edges leave between them. |

### Normalize on redesign

<!-- Screen-local quirks ONLY here — sections 1-6 stay as-shipped. A quirk that spans components belongs in the component's own spec (`Components/<Name>.md` → Normalize on redesign), not here. -->

**The prototype and this spec now agree — re-measured 2026-09-05.** An earlier
revision of this section listed three divergences (T1, T2, T5) and told the reader to
re-sync the prototype. That has since happened: `doc/Design/Frontend/PlatformManager/Prototypes/index.html` § `#screen-dti`
now places `.toolbar-sep` before `.filter-chips` and renders the chip's remove control
as an icon button with `aria-label="Bỏ lọc …"` and no `title`, each change carrying a
`SỬA 2026-09-05` comment that names its decision. The same pass brought in the
`Kỳ của số liệu` column (Q31), the single-chip filter count (T8) and the `Chênh lệch`
direction (Q25). T5 never applied to this screen: every period label in
`#screen-dti` already used the spaced dash — the twelve unspaced ones are all in
`#screen-dashboard`.

Do not take that on trust; it is the kind of claim that rots:

```bash
grep -c 'SỬA 2026-09-05' doc/Design/Frontend/PlatformManager/Prototypes/index.html                    # the sync pass left comments
awk 'NR>=3232' doc/Design/Frontend/PlatformManager/Prototypes/index.html | grep -c '[0-9]/[0-9][0-9]–[0-9]'   # T5 in #screen-dti — PASS = 0
```

Two things in this screen's design still cannot be read off the prototype, and both
are decisions rather than drift: the frozen edge columns (Q30 — no PrimeNG in a
static page) and the empty-catalogue banner (T9 — postdates the prototype). Both are
marked as such where they appear.

Screen-local quirks:

1. **Fourteen columns is past what a grid can carry, and Q30 treats the symptom.** Even on a 1440px desktop the user scrolls sideways; on a phone the screen is barely usable. Pinning the two edges (decision Q30) keeps the row identifiable while that happens, which is worth doing — but it does not make the grid narrower, and on a phone it makes the scrollport narrower still (§ Responsive). The underlying problem is the column set: `Phụ trách` and `Hạn xử lý` are **empty in every single one of the 62 rows** of the in-repo sample dataset `spec/danh-muc-dti/dti-mau-an-danh-62-dong.csv`, and `Chênh lệch` is derived from two columns already on screen. A grid carrying three columns that add nothing on the dataset the app is being built against is worth revisiting before build, not after. The same was recorded of the BA's original before that file was left out of the repo; the sample is the half anyone can now re-measure:

    ```bash
    python -c "
    import csv, io
    rows = list(csv.DictReader(io.open('spec/danh-muc-dti/dti-mau-an-danh-62-dong.csv', encoding='utf-8-sig', newline='')))
    print(len(rows), sum(1 for r in rows if r['Phụ trách'].strip() or r['Hạn xử lý'].strip()))
    "   # PASS = 62 0
    ```
2. **Two editing models on one grid.** Two columns edit in place; ten more edit in a dialog reached by a per-row button. The distinction is defensible (decision Q9) but nothing on screen signals it — the dashed underline appears on exactly two of fourteen cells and there is no legend.
3. **`Bấm đúp` (double-click) is the documented inline-edit gesture**, but the cells carry `role="button"` and `tabindex="0"`, and a keyboard user activates a button with Enter or Space, not a double-click. The tooltip therefore describes a gesture half the users cannot perform. Either document both, or make single-click the trigger.
4. **The empty-note prompt is copy pretending to be a value.** `— bấm đúp để ghi chú` sits in the data column and reads like content. A placeholder treatment (muted, italic, or an explicit empty state) would separate instruction from data.
5. **Period labels still use two separators across the product** — a colon here (`Tuần 33: 10/08 – 16/08/2026`), a middle dot on the Dashboard (`Tuần 33 · 10/08 – 16/08 · 82,1%`). Decision T5 settled the **dash**, not this. One owner now holds the formats (`spec/dashboard-dti/business-rules.md`), so the fix is to converge there rather than in either screen spec.
6. **Row actions diverge from the Core grid** — text buttons here, icon buttons on `Quản trị người dùng`. Deliberate and recorded, but it means the product teaches two vocabularies for "edit this row".
7. **The grid truncates in print** (capped at the height of its scroll region, which since the 2026-09-06 `scrollHeight="flex"` change comes from the flex chain rather than from `--grid-h`), and the toolbar vanishes with `.no-print`, so a printed page is a window onto the data with no indication more exists. Defect **A4** — `grep -n '@media print' doc/Design/Frontend/PlatformManager/Prototypes/index.html` returns only chrome-hiding blocks; none releases `--grid-h`.
8. **A second, near-identical import dialog exists in the prototype.** `app-csv-import-dialog` (`.csv` only) is kept beside `app-import-dialog` (`.csv,.xlsx,.xls`) for comparison, and the toolbar button routes to the second. Only one should survive into code — defect **A5**; both are still in `doc/Design/Frontend/PlatformManager/Prototypes/index.html` (§ `app-csv-import-dialog`, § `app-import-dialog`).
9. **Two inline-edit input classes are camelCase** (`.progressInput`, `.noteInput`) against the repo's kebab-case convention — defect A6 in the same list. Rename before they are typed into a stylesheet.
10. **The grid changes shape when a filter changes.** `Kỳ của số liệu` appears and disappears with the `Kỳ trong năm` condition (decision Q31, rule 1), so the column count, the total width and the horizontal scroll position all shift under the user as a side effect of filtering. It is the right call — a column repeating one value fourteen times is worse — but no other grid in the product does this, and nothing on screen announces it.
11. **The period cell is the one period display in the product that omits the period's *name*** — a deliberate exception to decision Q12, settled by **decision Q38 on 2026-09-06**, not an oversight. Q12 requires a period to be shown as name *and* range; `Kỳ của số liệu` shows `10/08 – 16/08` and stops. Two reasons: 110px will not hold `Tuần 33: 10/08 – 16/08/2026` and the column is already inside a grid that scrolls too far sideways; and after decision Q37 **every row in this column is a week**, so the name carries no information the range does not. ⚠️ **Do not "fix" this in a consistency pass.** If the unit rule ever changes — if a month can appear in this column — the exception dies with it and the name has to come back, because that is the moment the name starts distinguishing something.
12. **Three different conditions render a read-only grid.** No write key (Q39), a month selected (Q37) and `Tất cả` in a past year (T15, scoped by Q41 on 2026-09-10: a specific week of a past year is writable) all strip the write affordances, and a user can hit more than one at once. The design distinguishes them — hidden versus disabled, and a different banner per cause — but the permission case is carried by *absence*, which is the hardest thing for a user to read. Naming the reason in one line would be more robust than a grammar of emptiness. Decision **Q51** (2026-09-10) rejected that line for the permission case, so a redesign reopens it only through a new decision.
13. **The same date range is written three ways across the product.** `10/08 – 16/08` in this column (T14), `10/08 – 16/08/2026` in the Dashboard's history rows, and `Tuần 33 · 10/08 – 16/08 · 82,1%` in its period select. Each drops what its context already supplies, which is right in isolation and means a reader moving between the two screens sees the same week in three shapes. The formats have one owner (`spec/dashboard-dti/business-rules.md`); what has no owner is the *rule for what may be dropped*.
14. **This screen and `spec/danh-muc-dti/ui-spec.md` classify the same banner slot along two different axes** — recorded 2026-09-09, deliberately **not** resolved here. That file's § V3 lists four mutually exclusive roles: never-imported (T9) · read-only because a month or a year is selected (Q37) · read-only because the user lacks the write key (Q39) · target-week reminder. This file lists five, keyed off the `Năm đánh giá` / `Kỳ trong năm` pair: past year (T15) · month (Q37) · past week · `Tất cả` · current week. `ui-spec` also carries a **permission** role this list does not; decision **Q51** (2026-09-10) settles that half — the no-permission state shows no banner, so it is not an occupant of this slot and this list is right to leave it out. What remains is the axis question: this file splits **past year** out of the unit case where `ui-spec` folds it in — and the empty-catalogue banner (T9) is a further occupant of the same slot that this file describes in § States rather than in the Period-banners list at all. On that question each list is internally consistent and neither is wrong; what does not exist is one agreed enumeration of what may occupy that single slot, which is how a sixth role gets invented next time. The `ui-spec` side is being handled by its own owner.

## Cần chốt

<!-- Decisions this file must NOT make on its own. Raised 2026-09-05, trimmed three times since — after Q17–Q24, after Q25–Q34 and T7–T9, and after Q35–Q39 and T10–T13. -->

1. ~~**Copy for the target-period banner.**~~ **Settled 2026-09-06.** The banner names the
   target week and states where the write lands; it is **not** a warning that editing is
   blocked (Q20 inverted that). **The two verbatim strings are in § Copy**, alongside every
   other shipped string on this screen — moved there on **2026-09-09**, because a closed item
   in this section records a decision and must not become a second home for copy
   (`.claude/CLAUDE.md` §5). Two other places in this file said the copy was still open on the
   same day the settlement was written; both were corrected in the same pass.

   How the four surfaces divide the one story, so none of them repeats another: this banner
   says **where the write goes** · the `Kỳ của số liệu` column says **where each row came
   from** (Q31) · the month banner says **why nothing can be written** (Q37) · the year banner
   says the same for `Tất cả` in a past year (T15). Only the first two can appear together.
2. ~~**`showCurrentPageReport`.**~~ **Settled 2026-09-06: stays off.** Two reasons, and the
   second is the decisive one. It is not in the live `DataTable` contract (Q16c), and turning
   it on would mean adding it to [`../Components/DataTable.md`](../Components/DataTable.md)
   and to **every** grid including `Quản trị người dùng` — one screen cannot have it alone.
   But more simply: **the count is already on screen.** `.title` carries `62 chỉ tiêu` with
   `aria-live="polite"`, which is both the visible count and the one screen readers announce
   after a filter runs. A paginator line would repeat it in a second place, and a count
   written twice is a count that can disagree with itself.
3. ~~**The dash in `Kỳ của số liệu`.**~~ **Settled 2026-09-06 (T14):** the spaced form `10/08 – 16/08`, per T5 — Q38 dropped the week number, it did not reopen the dash convention. The year is dropped as well; see § Layout Blueprint rule 2.
4. ~~**A past year with `Kỳ trong năm = Tất cả`.**~~ **Settled 2026-09-06 (T15):** **read-only**, handled exactly like the month case. See § States. This applies to `Tất cả` only: a specific week of a past year is writable (Q41, 2026-09-10).
5. ~~**`spec/` folder naming.**~~ **Settled 2026-09-05:** `spec/danh-muc-dti/` and `spec/dashboard-dti/`. The three remaining pointers to the retired `spec/dashboard-dti-weekly/` were corrected the same day.
6. ~~**Copy for the user-visible error codes.**~~ **Duyệt 2026-09-10 (Q56):** người dùng duyệt **nguyên văn** toàn bộ bảng. Bảng câu chữ, và các mã cố ý bỏ ra kèm lý do từng nhóm, ở § Copy → Error codes → copy.
7. ~~**The current-year wording of the month banner.**~~ **Duyệt 2026-09-10 (Q60):** câu cho năm hiện tại duyệt nguyên văn; biến thể năm cũ giữ đúng câu đã duyệt trước đó. Xem § Copy và § States → a month is selected.

### Answered elsewhere — do not re-ask

| Was open here | Answer | Where it lives now |
| --- | --- | --- |
| Whether *age* makes a period read-only | **No** — Q20 makes a past week fully editable and the banner names the target week instead. Read-only on this screen comes from the period **unit** (Q37), the user's **permission** (Q39), or `Tất cả` in a past year, whose write would land outside the year on screen (T15). Never from the date alone: Q41 (2026-09-10) confirms a week of a past year is writable | § States |
| How `Tiến độ %` is seeded on import | **It is not** — Q24 leaves it blank for the user to enter. The Dashboard is empty until someone does | `spec/danh-muc-dti/business-rules.md` |
| Default page size | **10**, matching `Quản trị người dùng` — Q19 | § Layout → Paginator |
| Toolbar DOM order, and the chip remove button's copy and glyph | The **shipped component**, not the prototype — T1, T2 | § Layout, § Copy, § Normalize |
| The period-label formats, as a rule | Owned once, not per screen | `spec/dashboard-dti/business-rules.md` |
| The route path | **`/danh-muc/dti`** — decision Q33, 2026-09-05. It matches the pre-retirement route, the surviving pointers in `Footer.md` and `DESIGN.md`, and the shape of `/quan-tri/nguoi-dung` | this file's frontmatter + § intro |
| Who may write here | **One DTI permission key, covering every period** — Q27. No per-period right, no "closing" a period | § States → access |
| Which way `Chênh lệch` subtracts | **`Thẩm định − Tự đánh giá`** — Q25. Positive green, negative red | [`../Components/DeltaIndicator.md`](../Components/DeltaIndicator.md) |
| The empty-catalogue state | **A `NoticeBanner` above the toolbar**, with the copy written out — T9 | § States |
| Whether the grid pins any column | **Yes — `Mã` left, `Hành động` right** — Q30, specified as a component variant | [`../Components/DataTable.md`](../Components/DataTable.md) § Frozen edge columns |
| Which period a row's figures belong to | **The `Kỳ của số liệu` column**, present in `Tất cả` mode only, switching to the current period after a save — Q31 with Q26 | § Layout Blueprint |
| How the filter count and the chips are counted | **Non-default conditions only** — T8. `Kỳ = Tất cả` and the current year are defaults | § Layout Blueprint → Toolbar, § Copy |
| Whether the period cell keeps its week number | **No — the date range alone** — Q38, 2026-09-06. A reasoned exception to Q12, not a slip | § Layout Blueprint rule 2, § Normalize #11 |
| What a signed-in user without the write key sees | **This screen, read-only** — Q39. No route guard, no hidden menu row, no explanatory banner — the no-banner half confirmed by **Q51**, 2026-09-10 | § States |
| Whether a month or a year can be written to | **No — weeks only** — Q37. Months and years are computed from weeks; selecting one makes the grid read-only, with a banner. Export by month is unaffected | § States; rule owned by `spec/danh-muc-dti/business-rules.md` |
| The dash, and the year, in the period cell | **`10/08 – 16/08`** — spaced per T5, year dropped per T14, 2026-09-06 | § Layout Blueprint rule 2 |
| A past year with `Kỳ trong năm = Tất cả` | **Read-only** — T15, 2026-09-06, treated as the month case is. `isEditable` therefore has **three** conditions: write key · not a month · `Tất cả` only in the current year. 🔄 LẬT 2026-09-10 (Q41): the third condition read *"current year"*, which also failed a specific week of a past year | § States |
| Whether a specific week of a past year can be written | **Yes** — Q41, 2026-09-10, restating Q27 (*every period, past periods and earlier years included*). Past year + `Tất cả` stays read-only (T15); a month in any year stays read-only (Q37) | § States; rule owned by `spec/danh-muc-dti/business-rules.md` |
| How a group is labelled | **`Code. Name`** — `1. Hạ tầng và Nền tảng số`; `Code` `"1"`…`"6"` in template-file order, `Name` the file's `Nhóm` string unprefixed — Q42, 2026-09-10 | § Layout Blueprint → `Nhóm` column, § Copy |

---
kind: luat
scope: du-an
verified: 2026-09-06
project: "PlatformManager"
artifact: "Component index"
status: "current"
updated: "2026-09-10"
---

# Components — PlatformManager

This index is the **gate**. A screen spec may only compose components listed here; adding a
`Components/*.md` without a row does not make it composable.

Count the specs rather than trusting a number written in prose:

```bash
ls doc/Design/Frontend/PlatformManager/Components/*.md | wc -l
```

**Source of truth for every row:** the shared component layer in `src/FE/src/styles.scss`
(layer 2) and the Angular components under `src/FE/src/app/shared/components/`. The design
these were derived from lives in [`Prototypes/index.html`](./Prototypes/README.md) — open its
"Thư viện component" screen to see every variant and state side by side.

**Except for the rows marked 🚧.** Those five landed in `src/FE` on **2026-09-09** and every
value in them is now anchored to the shipped file — but **nothing composes them**: no page
template and no route reaches any of the five, so none has ever rendered in a browser. Read a
🚧 row as *"the component exists and can be imported"*, never as *"this is on screen"*. Each
spec's opening banner carries the commands for both halves. Count the rows, and check the
second half, rather than trusting this paragraph:

```bash
grep -l 'no call site on any page yet' doc/Design/Frontend/PlatformManager/Components/*.md | wc -l
grep -rn 'app-kpi-tile\|app-progress-bar\|app-trend-chart\|app-history-row\|app-delta-indicator' src/FE/src --include='*.html'
```

PASS for the first = the same number as the 🚧 rows above. It counts the **specs' own
frontmatter** rather than the glyph, on purpose: an emoji is four bytes, and this repo has
already lost a whole gate section to a `grep` that could not match one (`.claude/CLAUDE.md` §8).

PASS for the second = every hit is one of the five components' own templates. The day a
**page** template appears in that list, this paragraph and five banners have gone stale.

> 🔄 **SỬA 2026-09-10.** The marker was 📐 and this paragraph read *"specified but not
> implemented — no class and no element for them exists in `src/FE` yet"*. True the day it was
> written, false from 2026-09-09. The marker changed with it, because "design-only" and "built
> but uncomposed" are different states and a reader must not have to guess which one a row is in.

## The rule this index enforces

**One component, one definition.** No screen, page or component stylesheet re-declares a
button, input, badge, toolbar, table frame or dialog footer. Screens compose the classes below.
This is what makes the set implementable as shared Angular components: each row maps to exactly
one thing to build.

---

## Index

| Component | Spec | What it is |
|---|---|---|
| AuthCard | [Components/AuthCard.md](./Components/AuthCard.md) | The second shell — the two auth routes render **without** sidebar or topbar |
| AuthField | [Components/AuthField.md](./Components/AuthField.md) | The auth form layer (`.field`, `.field-row`, `.login-error`, `.btn-block`). The input itself is `Input.md` — the two tiers merged on 2026-08-29 |
| Avatar | [Components/Avatar.md](./Components/Avatar.md) | Initials disc (`.avatar`) — the app has no image-avatar path at all |
| Badge | [Components/Badge.md](./Components/Badge.md) | Pill label (`.badge`). Two families with different jobs: **status** (`.ok`/`.warn`/`.bad`/`.neutral`, semantic colour) and **identity** (`.outline`, bordered, no semantic colour) |
| Button | [Components/Button.md](./Components/Button.md) | Text button (`.btn`) + `.primary`, `.danger`, `.sm`, `.btn-block`. Bordered — a tonal fill alone does not separate from a card |
| Card | [Components/Card.md](./Components/Card.md) | The white surface container shelling every section |
| Check | [Components/Check.md](./Components/Check.md) | Checkbox treatment (`.check`) — merged from two byte-identical copies in the permission matrices |
| ConfirmDialog | [Components/ConfirmDialog.md](./Components/ConfirmDialog.md) | `<app-confirm-dialog>` — question / confirmation dialog on native `<dialog>`, severity icon, **exactly two buttons** |
| DataGrid | [Components/DataTable.md](./Components/DataTable.md) § CHỐT 2026-09-06 | `<app-data-grid>` — **lưới bản ghi dùng chung** ở `shared/components/data-grid/`. Sở hữu KHUNG (chiều cao cố định + cuộn bên trong, phân trang, loading); màn hình truyền cột qua `TemplateRef`. Từ 2026-09-06 đây là nơi DUY NHẤT import `p-table` |
| DataTable | [Components/DataTable.md](./Components/DataTable.md) | The PrimeNG `p-table` grid **mechanism** — lazy paging, scroll height, empty message, and since 2026-09-05 a 📐 frozen-edge-column variant with no shipped instance yet. Cell painting lives in `Table.md` |
| DeltaIndicator 🚧 | [Components/DeltaIndicator.md](./Components/DeltaIndicator.md) | Signed change text coloured by direction (`.delta` + `.up`/`.down`/`.flat`). The colour classes are global in `src/FE/src/styles.scss`; the wrapper that classifies and formats the number shipped 2026-09-09 in `shared/` — the only one of these five that belongs to CoreBase, because two screens use it |
| Dialog | [Components/Dialog.md](./Components/Dialog.md) | Native `<dialog>` in three width variants, plus the `.dialog-head` / `.dialog-actions` anatomy |
| Footer | [Components/Footer.md](./Components/Footer.md) | `.footer` — closing footnote line; page content, not app chrome |
| FormRow | [Components/FormRow.md](./Components/FormRow.md) | Dialog form group (`.form-row`, `.form-grid`, `.required`, `.form-error`) |
| HistoryRow 🚧 | [Components/HistoryRow.md](./Components/HistoryRow.md) | One saved-period row in the Dashboard's history panel (`.history` / `.histrow`) — date range, progress, movement, a `Xem` button |
| IconButton | [Components/IconButton.md](./Components/IconButton.md) | Icon-only ghost button (`.icon-btn`) + `.primary` / `.danger`. **Merged from five separate variants** that disagreed on size, radius and border |
| LanguageSwitcher | [Components/LanguageSwitcher.md](./Components/LanguageSwitcher.md) | `<app-language-switcher>` — đổi ngôn ngữ lúc chạy; một nút cho mỗi ngôn ngữ khai ở `CORE_I18N`. Hôm nay chỉ có trên màn đăng nhập |
| Input | [Components/Input.md](./Components/Input.md) | One input contract everywhere (`.input`), plus `.input-icon`, the six data-type width variants, `:disabled` and `.invalid` |
| KpiTile 🚧 | [Components/KpiTile.md](./Components/KpiTile.md) | Label / value / sub-caption stat tile — a `Card` with the `.kpi` modifier, four value tones, five to a row |
| NoticeBanner | [Components/NoticeBanner.md](./Components/NoticeBanner.md) | In-page banner (`.notice`) in four severities |
| ProgressBar 🚧 | [Components/ProgressBar.md](./Components/ProgressBar.md) | Track + fill (`.bar` / `.fill`) for one criteria group's progress. The app's only progress indicator; one brand colour at every level |
| SegmentedControl | [Components/SegmentedControl.md](./Components/SegmentedControl.md) | `.segmented` / `.seg-btn` — the app's only view switcher |
| Sidebar | [Components/Sidebar.md](./Components/Sidebar.md) | App-shell nav rail — **API-driven** menu tree, collapse rail, off-canvas drawer |
| Table | [Components/Table.md](./Components/Table.md) | The table primitive (`.tablewrap`, `.tablewrap.scroll`, global `th`/`td`/zebra) painting **every** table including PrimeNG's |
| Toast | [Components/Toast.md](./Components/Toast.md) | Floating notification stack — severity icon, title, body, dismiss |
| Toolbar | [Components/Toolbar.md](./Components/Toolbar.md) | `<app-toolbar>` — the above-list control surface: bounded search, filter panel behind a control with a count badge, removable condition chips, action group |
| Topbar | [Components/Topbar.md](./Components/Topbar.md) | App-shell sticky header — hamburger, route title, user + logout |
| TrendChart 🚧 | [Components/TrendChart.md](./Components/TrendChart.md) | The app's only chart — single-series line over the saved periods. **Restored 2026-09-05 by decision Q17** and built 2026-09-09 on PrimeNG `p-chart` over `chart.js`, which went back into `src/FE/package.json` the same day. The four `chart-*` roles stay out of the stylesheet by design: the component reads `:root` at runtime and hands the canvas literals (`Tokens/colors.md` § Chart Palette) |

**🚧 marks a spec whose component is built but not yet composed** — it exists under
`src/FE/src/app/`, it compiles, and no page or route renders it. A screen spec may
compose it (that is what these five are for), and a build task should still read the
spec's own `Cần chốt` section first. Everything unmarked is shipped code with a live
call site.

---

## Retired on 2026-08-29

Two things happened on this date. The dashboard and DTI-catalogue screens were removed from
`src/FE` to be rebuilt, taking their components with them; and the remaining component layer was
consolidated so that each kind of control has a single definition.

Verify any row below with `grep -rn "<class>" src/FE/src`. PASS = no hit outside a historical
comment — **except for the five that came back**, which were rebuilt on 2026-09-09 and now do
hit. `.delta` is the oldest of those exceptions: it survived the 2026-08-29 consolidation in
`src/FE/src/styles.scss` § `.delta` and had no caller at all until its wrapper landed. That
history is why the `DeltaIndicator` row below reads differently from the other four.

| Retired spec | Why | Where it went |
|---|---|---|
| ActionButton | Class `.action-btn` carried **two different components** — text buttons in one screen, icon-only buttons in another | Split by intent: `Button.md` (`.btn.sm`) and `IconButton.md` (`.icon-btn`) |
| CellIconButton | Inline-edit confirm/cancel pair; the screen that hosted it was removed. The class had **zero usages** even before that | `IconButton.md` |
| DeltaIndicator | Dashboard-only signed-change display | ⬅️ **Returned 2026-09-05** — [`Components/DeltaIndicator.md`](./Components/DeltaIndicator.md). Its colour classes never actually left `styles.scss`; only the wrapper did |
| Fab | Never ported to Angular; the batch action it floated does not exist | — |
| FilterBar | `.filters` superseded by a toolbar that puts conditions behind a filter control instead of spreading them across a row | `Toolbar.md` |
| HistoryRow | Dashboard history panel | ⬅️ **Returned 2026-09-05** — [`Components/HistoryRow.md`](./Components/HistoryRow.md) |
| KpiTile | Dashboard KPI tile | ⬅️ **Returned 2026-09-05** — [`Components/KpiTile.md`](./Components/KpiTile.md) |
| ProgressBar | Group-progress track, scoped to a dashboard component | ⬅️ **Returned 2026-09-05** — [`Components/ProgressBar.md`](./Components/ProgressBar.md) |
| RoleTag | A second chip primitive at the same size as `.badge` with different radius, padding and colour system | `Badge.md` — the `.outline` variant. Its argument was kept: a role name is an **identifier**, not a status, so it carries no semantic colour. What changed is that this is now a variant of one component rather than a separate class |
| TrendChart | The app's only chart | ⬅️ **Returned 2026-09-05** — [`Components/TrendChart.md`](./Components/TrendChart.md). Decision Q17 restored the four `chart-*` roles in `Tokens/colors.md` § Chart Palette the same day. The one item still to do outside this folder — `chart.js`, dropped from `src/FE/package.json` on 2026-09-04 — went back on 2026-09-09, and the component was built the same day |

> **TabBar** was deleted earlier, on 2026-08-23: it documented a switcher that never shipped.

### Five of them came back on 2026-09-05 — and one more thing did not

The rows above are **kept, not rewritten**. Five specs retired on 2026-08-29 were restored on
2026-09-05 because the rebuilt Dashboard and DTI catalogue compose them again: `KpiTile`,
`ProgressBar`, `TrendChart`, `DeltaIndicator` and `HistoryRow`. Each is a **restore, not a
redesign** — the values come from the approved prototype, which is the same design those
components were originally built from.

Why the retirement record stays visible rather than being tidied away: the reason each one was
retired (*"— screen removed"*, never *"— bad component"*) is exactly the evidence a future
reader needs to know these five were removed for a scheduling reason and not a design one. A
table that only ever showed the current state would have lost that.

**`report-dialog` is the one that is not coming back.** It never had a spec in `Components/`,
and after decision Q13 of 2026-09-05 it never will:

- `Xuất báo cáo` now downloads an `.xlsx` file directly — no dialog, no HTML preview, no copy
  or print button.
- The `app-report-dialog` CSS block at LỚP 4 of
  [`Prototypes/index.html`](./Prototypes/README.md) is therefore **dead style** — and since the
  2026-09-10 prototype merge the markup it used to style is gone as well: the dialog was removed
  from the prototype on 2026-09-05, when `Xuất báo cáo` became a direct `.xlsx` download. What
  survives is an empty comment header at LỚP 4, which should go the next time the prototype is
  touched.

  ```bash
  grep -c 'class="report"' doc/Design/Frontend/PlatformManager/Prototypes/index.html   # PASS = 0 — no markup left
  ```

  > 🔄 **SỬA 2026-09-10.** This bullet used to say the command *"returns the markup and nothing
  > declares a `.report` rule"* — defect **A3**. Half of that is now moot: there is no markup
  > left to be unstyled. The defect is recorded rather than deleted because it is the reason the
  > dead CSS header is still there.
- The endpoint behind it is replaced too. That is an API decision and it belongs to
  `doc/contracts/dashboard.md`, not here — this index records only that **no component and no
  spec exists for a report preview, and none should be added.**

---

## What a spec must contain

Each file carries a **Sources** section citing the live file it was derived from, and a table of
the five states — `default`, `:hover`, `:focus-visible`, `:active`, `:disabled`. Hand-rolled
components must define all five explicitly; this is where the pre-Angular prototype was weakest,
and porting it verbatim would have carried that gap forward.

Extending a component is preferred over adding one. If a screen needs a variant that is not
listed, add it to the existing spec and to the shared layer — do not introduce a parallel class.
That is the failure this index exists to prevent, and the Retired table above is what it looks
like when the rule is not held.

### There is no central "Known inconsistencies" list here, on purpose

An open library-level problem is recorded in **the spec of the component that owns the class** —
its `## Normalize on redesign` section, or `## Cần chốt` for a 📐 spec. This index routes you
there; it does not hold a second copy of the problem. Use the Index table to find the owning
spec from a class name, and check that every spec has exactly one such section:

```bash
grep -c '## Normalize' doc/Design/Frontend/PlatformManager/Components/*.md
```

Why not a central register: it would be a second home for issues that already have exactly one
each, and two homes drift (`.claude/CLAUDE.md` §5). The evidence is already in this folder —
`Components/FormRow.md` § Normalize #3 was **closed in its own spec on 2026-08-29**, while a
pointer to a central list left a reader believing a decision was still owed. Cross-spec debts
are the other half of the argument: the specs still carrying a *"chưa đối chiếu lại toàn file"*
banner, and those whose `Sources:` cite modules deleted on 2026-08-29, are already flagged in
each file **and** countable by command, so listing them here would be a hand-maintained count of
the kind `.claude/CLAUDE.md` §6 forbids:

```bash
grep -ln 'Chưa đối chiếu lại toàn file' doc/Design/Frontend/PlatformManager/Components/*.md
grep -ln 'modules/danh-muc-dti/\|modules/dashboard/' doc/Design/Frontend/PlatformManager/Components/*.md
```

Recorded 2026-09-08, after six screen specs were found pointing at a `COMPONENTS.md` §
*"Known inconsistencies"* that has never existed. They were stale copies of the boilerplate in
`doc/Design/Templates/Screen.md:78`, which already names the component spec as the destination;
all six now match that template again.

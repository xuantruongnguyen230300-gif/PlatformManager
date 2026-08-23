---
project: "PlatformManager"
status: "draft"
updated: "2026-08-22"
components_total: "26 documented + 1 obsolete"
---

# COMPONENTS.md — PlatformManager Component Library

> **Purpose:** index of the reusable UI components extracted from the live views, so designers and AI tools generate UI that matches the shipped product.
> **Principle:** every screen MUST be composed from these components — extend a spec in `Components/` instead of inventing new ones. All values come from `DESIGN.md` frontmatter and `Tokens/`; never hard-code colors/sizes outside them.

**Live source: `src/FE/` (Angular 20).** Every spec below was re-verified against the shipped app on 2026-08-22. Until this pass the index described the deleted prototype, which is **frozen history** as of the same date (`doc/Design/CLAUDE.md` § Fidelity Policy) — it covers 4 of 6 screens, its token values have drifted, and its interaction model differs. Nothing here may be sourced from it.

## General conventions

- **Five required states** — every component documents `default` / `hover` / `focus` / `active` / `disabled` as actually implemented. **This is no longer a formality:** the shipped app has real interactive states throughout. `.btn` ships all four interactive pseudo-classes — `:hover`, `:focus-visible`, `:active`, `:disabled` (`styles.scss:177-194`); `.action-btn` ships three (`:hover`, `:focus-visible`, `:disabled`, no `:active`); `.cell-icon-btn` ships two (`:hover`, `:focus-visible`); table rows highlight on hover; and `[disabled]` is bound at five real call sites. The prototype-era claim that "the entire stylesheet has exactly one interactive pseudo-class rule" is dead and has been removed.
- **Angular 20 + PrimeNG, no Storybook.** Anatomy and variants are read from standalone component templates (`*.html`), their scoped SCSS, and the global primitives in `src/FE/src/styles.scss` (`.card`, `.btn`, `.action-btn`, `.badge`, `.notice`, `.filters`, `.form-row`, `.field`/`.field-input`, `.delta`, `table`/`th`/`td`, `dialog`). PrimeNG supplies `p-table` and `p-chart`; its colours are mapped to the same tokens in `core/theme/platform-manager-preset.ts`.
- **Single light theme** — no dark mode, no `data-theme`, no toggle exists (`DESIGN.md` § Colors; `tokens.json`'s `dark` set is deliberately empty).
- **Icon set: PrimeIcons v7**, loaded globally via `angular.json:38-39,100-101`, rendered as `<i class="pi pi-…">`. See `Icons.md` for the per-action map. Three Unicode glyphs (`↑`/`↓`, `└`, `●`) remain as text rather than icons — recorded there as legacy exceptions.
- **Two shells** — the main shell (`Sidebar` + `Topbar` + `main`) and the bare auth shell used by routes carrying `data: { noShell: true }`. `Toast` overlays both.

## Component index

| Component | File | Summary |
| --- | --- | --- |
| ActionButton | [Components/ActionButton.md](./Components/ActionButton.md) | Ghost row-action button (`.action-btn`, `.danger`) — transparent until hover **by design**; the one control shipping all 5 states |
| AuthCard | [Components/AuthCard.md](./Components/AuthCard.md) | The second shell (`.login-shell`/`.login-card`/`.login-brand`) — the two auth routes render **without** sidebar or topbar |
| AuthField | [Components/AuthField.md](./Components/AuthField.md) | Auth input tier (`.field`, `.field-input`, `.toggle-visibility`, `.field-row`, `.login-error`) |
| Avatar | [Components/Avatar.md](./Components/Avatar.md) | Initials disc (`.avatar`) — 30px brand circle; the app has no image-avatar path at all |
| Badge | [Components/Badge.md](./Components/Badge.md) | Pill status label (`.badge`) — 5 colour variants across 3 contexts: the BE-computed criteria triad `.bdone`/`.bwork`/`.bstall`, the user-grid pair `.active`/`.locked`, and a period-mode chip |
| Button | [Components/Button.md](./Components/Button.md) | Tonal `.btn` + `.primary`, `.danger`, `.btn-block` and icon-only modifiers; all five states real, `[disabled]` bound at 5 call sites |
| Card | [Components/Card.md](./Components/Card.md) | The white surface container (`.card`) shelling every section on the four in-shell routes; separates by shadow, not border |
| CellIconButton | [Components/CellIconButton.md](./Components/CellIconButton.md) | Inline-edit ✓/✗ pair (`.cell-icon-btn.ok`/`.cancel`) + the `.cell-editable` double-click affordance |
| DataTable | [Components/DataTable.md](./Components/DataTable.md) | The PrimeNG `p-table` grid **mechanism** — `[lazy]` 10/20/50 paginator, frozen columns, loading mask, empty message. Cell painting lives in `Table.md` |
| DeltaIndicator | [Components/DeltaIndicator.md](./Components/DeltaIndicator.md) | `app-delta-indicator` — signed change text coloured `.up`/`.down`/`.flat` off a 0.001 epsilon; owns its own `vi-VN` formatting |
| Dialog | [Components/Dialog.md](./Components/Dialog.md) | Native `<dialog>`, 6 instances in 3 width variants (default 700px, `.form-dialog` 560px, `.confirm-dialog` 420px); no `p-dialog` anywhere |
| FilterBar | [Components/FilterBar.md](./Components/FilterBar.md) | Above-grid filter row (`.filters`, `.filters-actions`, `.search`) + the filter-tier field rule |
| Footer | [Components/Footer.md](./Components/Footer.md) | `.footer` — the dashboard's closing footnote line + inline brand `routerLink`; page content, not app chrome. The rule is declared **twice, identically** (global + page-scoped) |
| FormRow | [Components/FormRow.md](./Components/FormRow.md) | Dialog form group (`.form-row`, `.required`, `.form-error`, `.role-checkboxes`, `.form-grid`) |
| HistoryRow | [Components/HistoryRow.md](./Components/HistoryRow.md) | `.histrow` — one saved-period row (date · progress · delta · `Xem`) in the dashboard history panel |
| Input | [Components/Input.md](./Components/Input.md) | Native fields in **four** tiers (filter, table-cell edit, dialog form-row, auth `.field-input`) plus a checkbox treatment |
| KpiTile | [Components/KpiTile.md](./Components/KpiTile.md) | `app-kpi-tile` — `.card.kpi` with label/value/sub and a 4-value `KpiTone`; 5 instances in `KpiSummary` |
| NoticeBanner | [Components/NoticeBanner.md](./Components/NoticeBanner.md) | `.notice` — **conditional** read-only-mode explanation on the DTI catalogue (no longer the prototype's permanent workflow banner) |
| ProgressBar | [Components/ProgressBar.md](./Components/ProgressBar.md) | `.bar`/`.fill` group-progress track, component-scoped to `GroupProgressList`; the app's only progress indicator |
| RoleTag | [Components/RoleTag.md](./Components/RoleTag.md) | Neutral outlined role chip (`.role-tag`) in the user grid — one variant, deliberately no semantic colour |
| SegmentedControl | [Components/SegmentedControl.md](./Components/SegmentedControl.md) | `.segmented`/`.seg-btn` — the dashboard's Tuần\|Tháng view switcher: one bordered group, `overflow:hidden`, `.active` filled `brand`. The app's **only** view switcher |
| Sidebar | [Components/Sidebar.md](./Components/Sidebar.md) | App-shell nav rail (`.sidebar`) — **API-driven** menu tree, collapse rail, off-canvas drawer |
| Table | [Components/Table.md](./Components/Table.md) | The global table primitive (`.tablewrap` + the global `th`/`td`/zebra rules) that paints **every** table including PrimeNG's, plus the hand-rolled permission matrix. The grid mechanism lives in `DataTable.md` |
| Toast | [Components/Toast.md](./Components/Toast.md) | Fixed bottom-right notification stack (`.toast-stack`/`.toast-item`), 4 severities, 5 s auto-dismiss |
| Topbar | [Components/Topbar.md](./Components/Topbar.md) | App-shell sticky header (`.topbar`) — hamburger, route title `<h1>`, user + logout |
| TrendChart | [Components/TrendChart.md](./Components/TrendChart.md) | `app-trend-chart` — the app's **only** chart: PrimeNG `p-chart type="line"` behind `@defer (on viewport)`, one dataset, legend hidden, palette resolved from 3 tokens at runtime |

> **The index is the gate, not the file's existence.** A screen spec may only
> compose components listed in the table above. Adding a `Components/*.md`
> without an index row does not make it composable.
>
> Merged 2026-08-22: 11 refreshed (pass A) + 13 newly written (pass B) = **24
> documented**, plus `Fab` retained as obsolete — 25 files at that point, no
> duplicates, no dangling links.
>
> Pass C, 2026-08-22: the three specs listed as STILL UNWRITTEN — `TrendChart`,
> `SegmentedControl`, `Footer` — were written against `src/FE/`, taking the
> library to 27 documented + 1 obsolete. The unwritten list is now empty.
> `.chart-skeleton` is folded into `TrendChart.md` as the `@defer` placeholder
> state rather than given its own spec; `.period-display` remains undocumented
> (see the note below).
>
> Correction 2026-08-23: **`TabBar` deleted.** It documented a two-button view
> switcher on `/quan-tri/phan-quyen` that has never existed in `src/FE/` — the
> grep for its class and signal returns 0 matches, and `phan-quyen.page.html` is
> 19 lines holding one `.card` with one `<app-permission-matrix>` and no tabs.
> The second matrix it switched to belongs to `doc/contracts/permissions.md`
> PERM-2, which is still `Status: DRAFT` — a plan, not a shipped screen. Library
> is now **26 documented + 1 obsolete**. Verified index ↔ files on 2026-08-23:
> 27/27, no duplicates, no dangling links.

<!-- =========================================================================
     STILL UNWRITTEN — present in src/FE/, no spec either way:
       - (none) — cleared 2026-08-22 by pass C.

     Deliberately NOT given their own spec:
       - .chart-skeleton (dashboard.page.scss:21-26) — 5 declarations of flex
         centring, no states, no variants. Documented as the @defer placeholder
         state inside Components/TrendChart.md. Note it is shared with the
         history panel's placeholder (dashboard.page.html:51), so it belongs to
         the dashboard page, not to TrendChart.
       - .period-display (period-toolbar.scss:16-23) — a read-only chip styled
         like an input (border-strong / radius-sm / bg / muted / sp-2 sp-3 /
         fs-sm), one instance, no states, no variants, no interaction. Recorded
         as plain markup in Screens/01-dashboard.md. Promote it to a spec only
         if a second read-only-value chip appears, or fold it into Input.md as
         a fifth, non-editable tier — that decision is open.

     Note: DataTable.md and Table.md deliberately split one subject —
     Table.md owns the global cell/zebra/.tablewrap primitive that paints
     EVERY table (PrimeNG emits real <table> elements) plus the one
     hand-rolled permission matrix; DataTable.md owns the p-table grid
     mechanism (lazy paginator, frozen columns, loading mask). See the scope
     boundary blockquote at the top of Table.md.
     ========================================================================= -->

## Known inconsistencies (current code — normalize in redesigns)

Re-verified against `src/FE/` on 2026-08-22. The four prototype-era items are recorded with their outcome so the history is not lost.

1. ~~`.btn.danger` is dead CSS, never applied to any element.~~ **RESOLVED.** `.btn.danger` is live on the delete-confirmation action (`confirm-dialog.html:8`) and has its own hover shade `--bad-bg-hover` (`styles.scss:168-174`).
2. ~~No custom interactive states exist anywhere.~~ **RESOLVED.** All five states ship across the button families, and `[disabled]` is genuinely reachable — bound at `login.page.html:55`, `doi-mat-khau.page.html:58`, `phan-quyen.page.html:4`, `permission-matrix.html:25` and `csv-import-dialog.html:18`.
3. ~~Two "text field" treatments coexist for one role.~~ **STILL OPEN, AND WIDER — now four.** Filter (`border-strong`), table-cell edit (`border-strong`, `padding:5px`), dialog form-row (**`line`**), and auth (`border-strong`, `10px 12px 10px 36px`). Focus is also inconsistent: two different treatments ship and the table-cell and form-row tiers have **none**. See `Components/Input.md`.
4. ~~`--brand2` is declared but never consumed.~~ **RESOLVED.** `.btn.primary:hover` consumes it (`styles.scss:161-162`).
5. **`.tablewrap` has two conflicting contracts under one name.** `overflow:hidden` in the two PrimeNG grids, `overflow:auto; max-height:560px` in the permission matrix — declared separately in three component stylesheets. See `Components/Table.md`.
6. **`.dialog-actions` is duplicated in all six dialog stylesheets** and has already drifted into three `margin-top` values (8/12/16px), with one copy missing `gap`. See `Components/Dialog.md`.
7. **Two class vocabularies for one badge pair.** `.bdone`/`.active` are the same green pill and `.bstall`/`.locked` the same red pill, under four names in two files. See `Components/Badge.md`.
8. **Two implementations of the delta rule.** `DeltaIndicator` owns it, but `KpiSummary` computes its own delta text and tone for the KPI tile instead of using the component. See `Components/DeltaIndicator.md`.
9. **`<p-table styleClass="dti-grid">` references a class that does not exist** anywhere in `src/FE/` (`criteria-grid-table.html:14`) — dead attribute.
10. **Off-scale literals are widespread**: badge `10px`/`750`, KPI value `21px`/`850`, delta weight `850`, confirm message `13.5px`, progress track `9px`, table-cell input `padding:5px`, card `margin-top:16px`, notice `border-radius:12px` (hardcoded rather than `var(--radius-table)`). Weights and line-heights are not tokenised at all — see `Tokens/typography.md`.
11. **No elevation or motion scale.** Four uncontrolled shadows ship (`--shadow`, two `.btn` hover shadows, the dialog's `0 24px 70px`), and every `transition` duration is a literal.
12. **Accessibility gaps carried by these components**: `ProgressBar` has no `role="progressbar"`/`aria-value*`; `NoticeBanner` has no `aria-live`, so switching to read-only mode is announced to nobody; `DeltaIndicator`'s direction is a text glyph; the auth `.field-input` icons are decorative but not `aria-hidden` while the sidebar's are.

## Checklist when adding a new component
- [ ] Clear, consistent name; one file in `Components/`; anatomy, all variants, and the five states documented.
- [ ] Only token values from `DESIGN.md` / `Tokens/`; exact source file paths cited (`file:line` in `src/FE/`).
- [ ] Verified against shipped markup + CSS — never record a variant you cannot point to in the source.
- [ ] Row added to the index table above (and `components_total` bumped).
- [ ] If it replaces or absorbs something, say so rather than deleting the old spec.

## Obsolete

Specs kept deliberately, for components with **no live counterpart**. Marked `status: obsolete` in their own frontmatter with the grep that proves absence. Never compose a screen or a prompt pack from these; losing the record of why something went is how the same component gets re-invented later.

| Component | File | Removed because | Absence proof |
| --- | --- | --- | --- |
| Fab | [Components/Fab.md](./Components/Fab.md) | Never ported to Angular. The prototype's mobile floating "Lưu tuần" button existed to reach a **page-level batch save**; the shipped dashboard is read-only and editing moved to per-cell inline confirm on the DTI catalogue, so there is no batch action to float. Mobile reachability was solved structurally instead (sticky topbar + off-canvas drawer). | `grep -rni "fab" src/FE/src` → **0 matches** (verified 2026-08-22 at stage 3 and again at stage 4); `DESIGN.md` § Components records the same |

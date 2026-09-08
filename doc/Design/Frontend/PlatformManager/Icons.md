---
kind: luat
scope: du-an
verified: 2026-09-06
project: "PlatformManager"
status: "draft"
updated: "2026-09-06"
library: "PrimeIcons v7 (icon font, loaded globally via angular.json, authored as <i class=\"pi pi-*\">) + PrimeNG inline SVG (injected at runtime by the paginator and loading spinner of the one shipped p-table, not written in src/FE)"
legacy_exceptions: ["Unicode box-drawing glyph (└) as the permission-matrix tree branch", "Unicode bullet (●) inside the user-status Badge label"]
---

# Icons — PlatformManager Design System

> **Standard icon set: PrimeIcons v7.** `primeicons@^7.0.0` is a direct dependency and `node_modules/primeicons/primeicons.css` is registered in the global `styles` array of **both** the build and test targets. Confirm with `grep -n primeicons src/FE/package.json src/FE/angular.json` rather than trusting a line number — the previous revision cited `package.json:53` and `angular.json:39,101`, and by 2026-09-06 only one of those three numbers was still right. No `<link>` in `index.html` and no per-component import — the font is available everywhere. Every icon **authored in `src/FE/`** is the two-class form `<i class="pi pi-name">`; there is no `<svg>` sprite and no icon component wrapper in the app's own code.

> **But that is not the whole icon surface.** PrimeNG renders a **second set at runtime, as inline `<svg>`**, from its own icon components — nothing in `src/FE/` mentions them, so a grep for `pi-` misses them entirely. Enumerated in the Per-Action Map below and marked *PrimeNG SVG*; see § Normalize on redesign for what is wrong with them.

**Re-censused 2026-08-29** after the business module was removed and the shared component layer was consolidated. Two things moved: the dashboard and DTI-catalogue screens took their icons with them, and three new shared components (`Toast`, `ConfirmDialog`, `Toolbar`) brought icon sets of their own that the previous revision predates.

Re-run the census rather than trusting the table below to stay complete:

```bash
grep -rnoE 'pi-[a-z0-9-]+' src/FE/src/app --include=*.html --include=*.ts   | grep -v spec | grep -vE 'api-result|api-client|api-controller|api-base-url|api-error-message'
```

PASS = every remaining hit appears as a row in the Per-Action Map. Two families of false positive to know about:

- **Substring noise.** `api-result`, `api-client`, `api-controller`, `api-base-url` and `api-error-message` all contain `pi-` and are not icons; the filter above removes them.
- **Icon names inside a doc comment.** `sidebar.ts` names `pi-th-large` and `pi-folder` in a comment illustrating what the backend may send. They are examples, not shipped glyphs, and correctly have no row.

**Re-censused 2026-09-06.** Every row was re-located against the shipped templates.

## Library & Sizing

- **Icon element:** `<i class="pi pi-…">` — an icon-font glyph, inheriting `color` and `font-size` from its parent unless overridden.
- **Size:** not tokenised. Icons inherit the ambient font size in most places; the explicit sizes are `.input-icon > .pi` at `15px`, its toolbar variant `.toolbar .input-icon > .pi` at `13px`, `.icon-btn` at `12px` inside a 24×24px button, `.filter-chip .icon-btn` at `10px` inside a 16×16px button, `.dialog-icon` at `17px` inside a 40×40px disc, and `.toast-icon` at `11px` inside a 22×22px disc (all `src/FE/src/styles.scss` except `.toast-icon`, which is `src/FE/src/app/shared/components/toast/toast.scss`). Sidebar `.navicon` is `18px` (`src/FE/src/app/shared/components/sidebar/sidebar.scss`).
- **Gap to text:** whitespace in the template for `.btn` labels; `gap: var(--sp-3)` inside `.btn-block`, `.login-error`, `.notice` and `.toast-item`; `gap: var(--sp-2)` inside the `.filter` summary; absolute positioning for the two input-adornment cases.
- **Color:** always inherited. Decorative field/search adornments read `colors.muted`; `.icon-btn.primary` reads `colors.brand` and `.icon-btn.danger` reads `colors.bad`; `.dialog-icon` and `.toast-icon` take a fill/ink pair from their severity class; icons inside `.btn.primary` inherit `colors.on-primary`.
- **Accessibility:** sidebar icons are wrapped in `<span class="navicon" aria-hidden="true">` (`shared/components/sidebar/sidebar.html`), as are the toast severity disc (`toast.html`) and the dialog severity disc (`confirm-dialog.html`); the in-page banners on Người dùng and Phân quyền mark their `pi` glyph `aria-hidden` inline. `.input-icon > .pi` is `pointer-events:none`. Icon-only buttons carry `title` or `aria-label` instead of visible text (`user-grid-table.html`, `toast.html`, `confirm-dialog.html`, `toolbar.html`, `topbar.html`, `sidebar.html`) — locate each with `grep -n 'aria-hidden\|aria-label\|attr.title'`. **The PrimeNG SVG icons follow none of this** — they are not `aria-hidden` (§ Normalize #6).

## Per-Action Map

<!-- One row per action/context the shipped UI covers. Keep mappings stable across specs and Figma so dev handoff is 1:1. -->

Paths in the last column are relative to `src/FE/src/app/`.

> 🔄 **SỬA 2026-09-06 — the `file:line` column was replaced by `file`.** Re-running the census that day found the line number **wrong in 19 of the 24 rows that carried one**, some by a few lines and some by fifty-four (the permission-matrix footnote was cited at `:98` and sits at `:152`). Every wrong number still pointed *inside* its file, so `check-docs.sh` §4 passed on all of them — the gate asks whether a cited line exists, not whether it says what the row claims. Re-typing 19 numbers buys the same failure again at the next refactor, so the column now carries the file and the glyph is the anchor: `grep -n 'pi-pencil' <file>` answers the question in one command and cannot go stale. This is the same move `doc/Design/CLAUDE.md` § Neo trích dẫn made for `styles.scss`, for the same reason.

| Action/context | Icon | Library | Live class | Source file |
|----------------|------|---------|------------|-------------|
| Open mobile navigation drawer | hamburger | PrimeIcons | `pi pi-bars` | `shared/components/topbar/topbar.html` |
| Sign out | exit arrow | PrimeIcons | `pi pi-sign-out` | `shared/components/topbar/topbar.html` |
| Collapse / expand sidebar | left chevron | PrimeIcons | `pi pi-angle-left` | `shared/components/sidebar/sidebar.html` |
| Expand / collapse a nav group | down chevron | PrimeIcons | `pi pi-chevron-down` | `shared/components/sidebar/sidebar.html` |
| Nav item — Trang chủ | house | PrimeIcons | `pi-home` | BE-supplied, `AppMenuSeedSource.cs` § `Code: "trang-chu"` |
| Nav item — Quản trị hệ thống (group) | cog | PrimeIcons | `pi-cog` | BE-supplied, `AppMenuSeedSource.cs` § `Code: "quan-tri"` |
| Nav item — Người dùng | user | PrimeIcons | `pi-user` | BE-supplied, `AppMenuSeedSource.cs` § `Code: "sys-user"` |
| Nav item — Phân quyền | shield | PrimeIcons | `pi-shield` | BE-supplied, `AppMenuSeedSource.cs` § `Code: "phan-quyen"` |
| Nav item — icon missing from BE | circle (fallback) | PrimeIcons | `pi pi-circle` | `shared/components/sidebar/sidebar.ts` |
| Toast — success | check | PrimeIcons | `pi pi-check` inside `.toast-icon` | `shared/components/toast/toast.ts` |
| Toast — error | times | PrimeIcons | `pi pi-times` inside `.toast-icon` | `shared/components/toast/toast.ts` |
| Toast — warning | warning triangle | PrimeIcons | `pi pi-exclamation-triangle` inside `.toast-icon` | `shared/components/toast/toast.ts` |
| Toast — info | info in circle | PrimeIcons | `pi pi-info-circle` inside `.toast-icon` | `shared/components/toast/toast.ts` |
| Dismiss a toast | times | PrimeIcons | `pi pi-times` on an `.icon-btn` | `shared/components/toast/toast.html` |
| Confirm dialog — question (`severity="ask"`) | question in circle | PrimeIcons | `pi pi-question-circle` inside `.dialog-icon.ask` | `shared/components/confirm-dialog/confirm-dialog.ts` |
| Confirm dialog — success (`severity="ok"`) | check in circle | PrimeIcons | `pi pi-check-circle` inside `.dialog-icon.ok` | `shared/components/confirm-dialog/confirm-dialog.ts` |
| Confirm dialog — caution (`severity="warn"`) | warning triangle | PrimeIcons | `pi pi-exclamation-triangle` inside `.dialog-icon.warn` | `shared/components/confirm-dialog/confirm-dialog.ts` |
| Confirm dialog — destructive (`severity="bad"`) | trash can | PrimeIcons | `pi pi-trash` inside `.dialog-icon.bad` | `shared/components/confirm-dialog/confirm-dialog.ts` |
| Close a dialog | times | PrimeIcons | `pi pi-times` on `.icon-btn.dialog-close` | `shared/components/confirm-dialog/confirm-dialog.html`, `platform/quan-tri-nguoi-dung/components/user-form-dialog/user-form-dialog.html` |
| Toolbar — search adornment | magnifier (decorative) | PrimeIcons | `pi pi-search` inside `.input-icon.search` | `shared/components/toolbar/toolbar.html` |
| Toolbar — open the filter panel | funnel | PrimeIcons | `pi pi-filter` inside the `<summary class="btn">` | `shared/components/toolbar/toolbar.html` |
| Toolbar — remove one filter chip | times | PrimeIcons | `pi pi-times` on the chip's `.icon-btn` | `shared/components/toolbar/toolbar.html` |
| Edit a user row | pencil | PrimeIcons | `pi pi-pencil` on `.icon-btn.primary` | `platform/quan-tri-nguoi-dung/components/user-grid-table/user-grid-table.html` |
| Lock a user account | closed padlock | PrimeIcons | `pi pi-lock` (bound `[class.pi-lock]="!row.IsLocked"`) | `…/user-grid-table.html` |
| Unlock a user account | open padlock | PrimeIcons | `pi pi-lock-open` (bound `[class.pi-lock-open]="row.IsLocked"`) | `…/user-grid-table.html` |
| Home — "no business module yet" banner | info in circle | PrimeIcons | `pi pi-info-circle` inside `.notice` | `platform/trang-chu/pages/trang-chu/trang-chu.page.html` |
| Home — go to change password | key | PrimeIcons | `pi pi-key` inside an `<a class="btn">` | `platform/trang-chu/pages/trang-chu/trang-chu.page.html` |
| Permission matrix — explanatory footnote | info in circle | PrimeIcons | `pi pi-info-circle` (`aria-hidden`) | `platform/phan-quyen/pages/phan-quyen/phan-quyen.page.html` |
| Submit sign-in | enter arrow | PrimeIcons | `pi pi-sign-in` | `platform/login/pages/login/login.page.html` |
| Username field adornment | **person** (decorative) | PrimeIcons | `pi pi-user` | `platform/login/pages/login/login.page.html` |
| Password field adornment | closed padlock (decorative) | PrimeIcons | `pi pi-lock` | `login.page.html`, `doi-mat-khau.page.html` |
| New / confirm password adornment | key (decorative) | PrimeIcons | `pi pi-key` | `platform/doi-mat-khau/pages/doi-mat-khau/doi-mat-khau.page.html` |
| Reveal password | eye | PrimeIcons | `pi pi-eye` (bound `[class.pi-eye]="!showPassword()"`) | `login.page.html` |
| Hide password | eye with slash | PrimeIcons | `pi pi-eye-slash` (bound `[class.pi-eye-slash]="showPassword()"`) | `login.page.html` |
| Auth error banner | exclamation in circle | PrimeIcons | `pi pi-exclamation-circle` | `login.page.html`, `doi-mat-khau.page.html` |
| App-update banner — "a new version is available" | refresh arrows | PrimeIcons | `pi pi-refresh` inside `.notice.warn.app-update` | `app.html` |
| Người dùng — list failed to load | warning triangle | PrimeIcons | `pi pi-exclamation-triangle` (`aria-hidden`) inside `.notice.bad` | `platform/quan-tri-nguoi-dung/pages/quan-tri-nguoi-dung/quan-tri-nguoi-dung.page.html` |
| Người dùng — retry the failed load | refresh arrows | PrimeIcons | `pi pi-refresh` (`aria-hidden`) inside a `.btn` | `…/quan-tri-nguoi-dung.page.html` |
| Phân quyền — save conflict banner (both tabs) | warning triangle | PrimeIcons | `pi pi-exclamation-triangle` (`aria-hidden`) inside `.notice.warn` | `platform/phan-quyen/pages/phan-quyen/phan-quyen.page.html` |
| Paginate — first page | double left chevron | **PrimeNG SVG** | `<svg data-p-icon="angle-double-left">` (`AngleDoubleLeftIcon`) | Rendered by `p-paginator`; no FE source line — see the note below |
| Paginate — previous page | left chevron | **PrimeNG SVG** | `<svg data-p-icon="angle-left">` (`AngleLeftIcon`) | idem |
| Paginate — next page | right chevron | **PrimeNG SVG** | `<svg data-p-icon="angle-right">` (`AngleRightIcon`) | idem |
| Paginate — last page | double right chevron | **PrimeNG SVG** | `<svg data-p-icon="angle-double-right">` (`AngleDoubleRightIcon`) | idem |
| Table is loading | spinner | **PrimeNG SVG** | `<svg data-p-icon="spinner">` (`SpinnerIcon`) | Rendered by the one `p-table` under `[loading]` — see below |
| Menu-tree child row marker | literal glyph `└` (plain text, **not** an icon element) | — | `.tree-branch` | `platform/phan-quyen/components/permission-matrix/permission-matrix.html` |
| User status dot | literal glyph `●` inside the badge label text | — | `.badge.bad` / `.badge.ok` | `…/user-grid-table.html` |

**Sidebar icons are backend-owned.** The FE does not map abstract keys to icon classes; `doc/contracts/meta-menu.md` fixes the contract that `SysMenu.icon` **is** the literal PrimeIcons class, and `sidebar.ts:9-12,37-39` only supplies `pi-circle` when the BE sends `null`. The four values above are the seeded defaults (`AppMenuSeedSource.cs:44-57`) and can be changed in the database without an FE deploy — so treat them as current data, not hardcoded design. The nav-item rows deliberately show the class **without** the `pi ` prefix, because that is exactly what the API returns; the template supplies the `pi` base class itself (`sidebar.html:39,53,67`).

**The five PrimeNG SVG icons have no source line because nothing in `src/FE/` names them.** They appear because a component was switched on, not because an icon was written. There is still exactly **one** `p-table` in the app, and it carries both `[paginator]="true"` and `[loading]`, so it is the sole host of all five. Locate it with `grep -rln '<p-table' src/FE/src/app`.

> 🔄 **SỬA 2026-09-06 — the `p-table` moved.** The previous revision placed it in `user-grid-table.html` with three line citations. It now lives in the shared `shared/components/data-grid/data-grid.html`; `user-grid-table.html` renders `<app-data-grid>` and contributes only column templates. The icon inventory is unchanged — same one table, same five glyphs — but a spec that sent a reader to the old file would have found no `p-table` there at all.

`[loading]` bindings exist elsewhere and none adds a spinner:

- The user-admin page and `user-grid-table` each bind `[loading]` on a **child component**, which forwards it down to the single `p-table` — the same spinner, two hops up, not extra ones.
- The permission matrices are **not PrimeNG at all.** Nothing under `platform/phan-quyen/` imports `TableModule` or renders `p-table`; both matrices are hand-written `<table>` elements and `loading` there is the app's own `input<boolean>()`, whose only rendered effect is `[disabled]` on the checkboxes. Those screens have **no spinner, skeleton or progress text at all**, exactly as `Screens/04-phan-quyen.md` § States records.

PrimeNG's table also ships sort and filter icons (`SortAltIcon`, `ArrowUpIcon`, `ArrowDownIcon`, `FilterIcon`), but **none of them render here** — no template uses `pSortableColumn`, `[sortField]` or `[filters]`. The app's own funnel (`pi-filter`, toolbar) is unrelated to PrimeNG's `FilterIcon`; they would collide visually if column filters were ever switched on. Listed so a future `sortable` flag is understood to add icons nobody chose.

**Actions that deliberately ship without an icon.** Most `.btn`s are text-only — dialog close/cancel/save, "Lưu thay đổi" on both Phân quyền tabs, the lock confirmation, "+ Thêm người dùng", and the toolbar's clear/apply filter pair. The exceptions, all listed as rows above, are: the sign-in submit (`pi-sign-in`), the topbar's sign-out (`pi-sign-out`), the home page's change-password link (`pi-key`), and the two reload/retry buttons (`pi-refresh`). Every `<select>` in the filter panel is unadorned, and so are the two `<button>`s of the language switcher.

> 🔄 **SỬA 2026-09-06 — this paragraph used to quote the button labels as Vietnamese literals** (`Đóng`, `Huỷ`, `Lưu`, `Xoá lọc`, `Áp dụng`, `Đăng xuất`…). Those strings no longer exist in any template: every one is now a translation key resolved at runtime, so quoting them here described a template that has not shipped since the i18n runtime landed. Buttons are named by role above instead. It also missed the two `pi-refresh` buttons, which are genuine counter-examples to "text-only".

## Legacy Exceptions

<!-- As-shipped uses of non-standard icon sets. Record them faithfully — specs must show what ships, never silently swap in the standard set. Mirror the set names in `legacy_exceptions` frontmatter. -->

| Set | Where it lingers | On disk |
|-----|------------------|---------|
| Unicode box-drawing `└` | Child-row marker in the menu permission matrix's first column | `platform/phan-quyen/components/permission-matrix/permission-matrix.html:18` |
| Unicode bullet `●` | Baked into the user-status badge label (`● Đã khoá` / `● Đang hoạt động`), so the dot cannot be styled independently of the text | `platform/quan-tri-nguoi-dung/components/user-grid-table/user-grid-table.html:59,61` |

**One exception retired on 2026-08-29:** the Unicode arrows `↑` / `↓` prepended to the `DeltaIndicator` string. That component lived under the dashboard module and went with it — `grep -rn '↑' src/FE/src/app` returns nothing (checked 2026-08-29). If a signed-change display is rebuilt, Normalize item 1 below is the design decision that was never applied and should be applied then.

## Normalize on redesign

<!-- Numbered replacement plan: each legacy icon → its standard-library equivalent. Applied only during redesigns, never retro-fitted into as-shipped specs. -->

1. `●` in the user-status badge → a styled `<span>` dot or `pi pi-circle-fill`; today it is inside the label string, so it cannot be restyled without editing copy. The same applies to any `↑`/`↓` reintroduced with a rebuilt delta indicator: make them sibling elements (`pi pi-arrow-up` / `pi pi-arrow-down`) so direction can be sized and coloured independently of the number.
2. `└` in the permission matrix → CSS-drawn tree lines (`border-left`/`::before`), which survive font substitution and do not read as text content.
3. **Icon size is not tokenised** — six explicit glyph sizes (`10`/`11`/`12`/`13`/`15`/`17px`) plus the sidebar's `18px` appear as literals while the type scale stops at `--fs-lg` (15px). Add an icon-size scale rather than forcing icons onto the text scale; `Tokens/typography.md` § Normalize records the same gap from the other side.
4. **`.input-icon > .pi` adornments are decorative and `pointer-events:none` but are not `aria-hidden`**, unlike the sidebar's `.navicon`, the toast disc and the dialog disc — screen readers may announce the font glyph. Hide them consistently; it is a one-attribute change at two call sites.
5. ~~**The paginator announces itself in English.**~~ — 🔄 **SỬA 2026-09-06: this was fixed, by a different route than the item proposed.** `providePrimeNG` still carries no inline `translation` block, which is why the old wording looked true on a quick read of `app.config.ts`. But the i18n layer supplies one per language: each entry in `APP_I18N.languages` carries a `primeTranslation` bundle taken from the `primelocale` package, and `LanguageService.use()` calls `primeng.setTranslation(...)` on every switch. The paginator's screen-reader labels now follow the selected language.

   **What is left is narrower, and worth keeping.** `PrimeNG.translation` is a plain object rather than a signal, so under this app's zoneless change detection an already-rendered PrimeNG bundle does not pick up a mid-session language switch until the next navigation forces a re-render. First load is correct in either language; switching language while sitting on the user list leaves the paginator's labels in the previous one. The reasoning is recorded in `src/FE/src/app/core/i18n/core-i18n.ts` (`grep -n 'setTranslation' src/FE/src/app/core/i18n/core-i18n.ts`).
6. **The PrimeNG SVG icons carry no `aria-hidden`.** `BaseIcon` sets `data-p-icon` and nothing else, so the arrow and spinner glyphs are exposed to assistive tech as unnamed graphics sitting inside already-labelled buttons — the label is announced, then the shape again. The app cannot patch this per-instance; it belongs in a global CSS/base-class override or an upstream report.
7. **Two icon systems ship side by side** — an icon *font* the app authors (`pi pi-*`) and inline *SVG* the component library injects. They scale differently (`font-size` vs `width`/`height`), colour differently (`color` vs `fill`/`currentColor`), and only one of them is greppable from `src/FE/`. Any icon-size scale added under item 3 must cover both or it will silently apply to half the icons.
8. **`pi-times` carries three unrelated meanings** — *dismiss a toast*, *close a dialog*, *remove a filter chip* — and a fourth as the **error** severity glyph inside `.toast-icon`. The first three are all "get rid of this control", which is coherent; the fourth is not, and a red disc holding the same glyph as the adjacent dismiss button is the one place the reuse actually costs clarity.

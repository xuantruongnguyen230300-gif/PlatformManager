---
project: "PlatformManager"
status: "draft"
updated: "2026-08-22"
library: "PrimeIcons v7 (icon font, loaded globally via angular.json, authored as <i class=\"pi pi-*\">) + PrimeNG inline SVG (injected at runtime by p-paginator and p-table, not written in src/FE)"
legacy_exceptions: ["Unicode arrow glyphs (↑/↓) inside DeltaIndicator text", "Unicode box-drawing glyph (└) as the permission-matrix tree branch", "Unicode bullet (●) inside the user-status Badge label"]
---

# Icons — PlatformManager Design System

> **Standard icon set: PrimeIcons v7.** `primeicons@^7.0.0` is a direct dependency (`src/FE/package.json:35`) and `node_modules/primeicons/primeicons.css` is registered in the global `styles` array of **both** the build and test targets (`src/FE/angular.json:38-39,100-101`). No `<link>` in `index.html` and no per-component import — the font is available everywhere. Every icon **authored in `src/FE/`** is the two-class form `<i class="pi pi-name">`; there is no `<svg>` sprite and no icon component wrapper in the app's own code.

> **But that is not the whole icon surface.** PrimeNG renders a **second set at runtime, as inline `<svg>`**, from its own icon components — nothing in `src/FE/` mentions them, so a grep for `pi-` misses them entirely. They ship on every screen that paginates or loads. Enumerated in the Per-Action Map below and marked *PrimeNG SVG* in the Library column; see § Normalize on redesign #7-9 for what is wrong with them.

**This replaces the previous `library: "none"` declaration**, which described the deleted prototype (frozen history since 2026-08-22) and was already stale when the Angular app landed.

## Library & Sizing

- **Icon element:** `<i class="pi pi-…">` — an icon-font glyph, inheriting `color` and `font-size` from its parent unless overridden.
- **Size:** not tokenised. Icons inherit the ambient font size in most places; three call sites set it explicitly — `.field-input .pi` at `15px` (`styles.scss:525-531`), `.search .pi` at `13px` (`quan-tri-nguoi-dung.page.scss:11-18`), and `.cell-icon-btn` at `12px` inside a 24×24px button (`criteria-grid-table.scss:57-72`).
- **Gap to text:** whitespace in the template for `.btn` labels; `gap:8px` inside `.btn-block` (`styles.scss:606`) and `.login-error` (`styles.scss:619`); absolute positioning for the two input-adornment cases.
- **Color:** always inherited. Decorative field/search adornments read `colors.muted`; `.cell-icon-btn.ok` reads `colors.good` and `.cancel` reads `colors.bad`; icons inside `.btn.primary` inherit `colors.on-primary`.
- **Accessibility:** sidebar icons are wrapped in `<span class="navicon" aria-hidden="true">` (`sidebar.html:31,45,59`); `.field-input .pi` is `pointer-events:none`. Icon-only buttons carry `title` or `aria-label` instead of visible text (`user-grid-table.html:50,58-64`, `toast.html:8`, `topbar.html:6`). **The PrimeNG SVG icons follow none of this** — they are not `aria-hidden`, and their host paginator buttons are labelled in English (§ Normalize #7-8).

## Per-Action Map

<!-- One row per action/context the shipped UI covers. Keep mappings stable across specs and Figma so dev handoff is 1:1. -->

| Action/context | Icon | Library | Live class | Source file:line |
|----------------|------|---------|------------|------------------|
| Open mobile navigation drawer | hamburger | PrimeIcons | `pi pi-bars` | `shared/components/topbar/topbar.html:11` |
| Sign out | exit arrow | PrimeIcons | `pi pi-sign-out` | `shared/components/topbar/topbar.html:19` |
| Collapse / expand sidebar | left chevron | PrimeIcons | `pi pi-angle-left` | `shared/components/sidebar/sidebar.html:12` |
| Expand / collapse a nav group | down chevron | PrimeIcons | `pi pi-chevron-down` | `shared/components/sidebar/sidebar.html:33` |
| Nav item — Dashboard | grid | PrimeIcons | `pi pi-th-large` | BE-supplied, `CoreSeeder.cs:79` |
| Nav item — Danh mục (group) | folder | PrimeIcons | `pi pi-folder` | BE-supplied, `CoreSeeder.cs:80` |
| Nav item — DTI | list | PrimeIcons | `pi pi-list` | BE-supplied, `CoreSeeder.cs:81` |
| Nav item — Quản trị hệ thống (group) | cog | PrimeIcons | `pi pi-cog` | BE-supplied, `CoreSeeder.cs:82` |
| Nav item — Người dùng | user | PrimeIcons | `pi pi-user` | BE-supplied, `CoreSeeder.cs:83` |
| Nav item — Phân quyền | shield | PrimeIcons | `pi pi-shield` | BE-supplied, `CoreSeeder.cs:84` |
| Nav item — icon missing from BE | circle (fallback) | PrimeIcons | `pi pi-circle` | `shared/components/sidebar/sidebar.ts:12,37-39` |
| Dismiss a toast | times | PrimeIcons | `pi pi-times` | `shared/components/toast/toast.html:11` |
| Search users by name/email | magnifier (decorative adornment) | PrimeIcons | `pi pi-search` | `platform/quan-tri-nguoi-dung/pages/quan-tri-nguoi-dung/quan-tri-nguoi-dung.page.html:9` |
| Edit a user row | pencil | PrimeIcons | `pi pi-pencil` | `platform/quan-tri-nguoi-dung/components/user-grid-table/user-grid-table.html:51` |
| Lock a user account | closed padlock | PrimeIcons | `pi pi-lock` (bound `[class.pi-lock]="!row.IsLocked"`) | `…/user-grid-table.html:67` |
| Unlock a user account | open padlock | PrimeIcons | `pi pi-lock-open` (bound `[class.pi-lock-open]="row.IsLocked"`) | `…/user-grid-table.html:67` |
| Confirm an inline cell edit | check | PrimeIcons | `pi pi-check` | `modules/danh-muc-dti/components/criteria-grid-table/criteria-grid-table.html:60,95` |
| Cancel an inline cell edit | times | PrimeIcons | `pi pi-times` | `…/criteria-grid-table.html:63,98` |
| Submit sign-in | enter arrow | PrimeIcons | `pi pi-sign-in` | `platform/login/pages/login/login.page.html:56` |
| Username field adornment | envelope (decorative) | PrimeIcons | `pi pi-envelope` | `platform/login/pages/login/login.page.html:13` |
| Password field adornment | closed padlock (decorative) | PrimeIcons | `pi pi-lock` | `login.page.html:29`, `doi-mat-khau.page.html:13` |
| New / confirm password adornment | key (decorative) | PrimeIcons | `pi pi-key` | `platform/doi-mat-khau/pages/doi-mat-khau/doi-mat-khau.page.html:29,45` |
| Reveal password | eye | PrimeIcons | `pi pi-eye` (bound `[class.pi-eye]="!showPassword()"`) | `login.page.html:45` |
| Hide password | eye with slash | PrimeIcons | `pi pi-eye-slash` (bound `[class.pi-eye-slash]="showPassword()"`) | `login.page.html:45` |
| Auth error banner | exclamation in circle | PrimeIcons | `pi pi-exclamation-circle` | `login.page.html:4`, `doi-mat-khau.page.html:4` |
| Paginate — first page | double left chevron | **PrimeNG SVG** | `<svg data-p-icon="angle-double-left">` (`AngleDoubleLeftIcon`) | Rendered by `p-paginator`; no FE source line — see the note below |
| Paginate — previous page | left chevron | **PrimeNG SVG** | `<svg data-p-icon="angle-left">` (`AngleLeftIcon`) | idem |
| Paginate — next page | right chevron | **PrimeNG SVG** | `<svg data-p-icon="angle-right">` (`AngleRightIcon`) | idem |
| Paginate — last page | double right chevron | **PrimeNG SVG** | `<svg data-p-icon="angle-double-right">` (`AngleDoubleRightIcon`) | idem |
| Table is loading | spinner | **PrimeNG SVG** | `<svg data-p-icon="spinner">` (`SpinnerIcon`) | Rendered by `p-table` under `[loading]` — **2** grids, see below |
| Increase indicator | literal glyph `↑` (plain text, **not** an icon element) | — | `.delta.up` | `modules/dashboard/components/delta-indicator/delta-indicator.ts:38` |
| Decrease indicator | literal glyph `↓` (plain text, **not** an icon element) | — | `.delta.down` | `modules/dashboard/components/delta-indicator/delta-indicator.ts:38` |
| Menu-tree child row marker | literal glyph `└` (plain text) | — | `.tree-branch` | `platform/phan-quyen/components/permission-matrix/permission-matrix.html:16` |
| User status dot | literal glyph `●` inside the badge label text | — | `.badge.active` / `.badge.locked` | `…/user-grid-table.html:42,44` |

**Sidebar icons are backend-owned.** The FE does not map abstract keys to icon classes; `doc/contracts/meta-menu.md:66-69` fixes the contract that `SysMenu.icon` **is** the literal PrimeIcons class, and `sidebar.ts:9-12,37-39` only supplies `pi-circle` when the BE sends `null`. The six values above are the seeded defaults (`CoreSeeder.cs:79-84`) and can be changed in the database without an FE deploy — so treat them as current data, not hardcoded design.

**The five PrimeNG SVG icons have no source line because nothing in `src/FE/` names them.** They appear because a component was switched on, not because an icon was written:

- **Paginator arrows** — `[paginator]="true"` on three tables: `criteria-grid-table.html:7`, `criteria-table.html:32`, `user-grid-table.html:6`.
- **Spinner** — only **two** `p-table` instances render it: `criteria-grid-table.html:3` and `user-grid-table.html:4`. Five `[loading]` bindings exist in the app, but they are not five spinners:
  - `danh-muc-dti.page.html:46` and `quan-tri-nguoi-dung.page.html:16` bind `[loading]` on the **child component**, which forwards it into the `p-table` above — the same spinner, one hop up, not an extra one.
  - `phan-quyen.page.html:16` is **not PrimeNG at all.** Nothing under `platform/phan-quyen/` imports `TableModule` or renders `p-table`; the permission matrix is a hand-written `<table>` element (`permission-matrix.html:2`) and `loading` there is the app's own `input<boolean>()` (`permission-matrix.ts:43`), whose only rendered effect is `[disabled]` on the checkboxes (`permission-matrix.html:25`). That screen has **no spinner, skeleton or progress text at all**, exactly as `Screens/04-phan-quyen.md` § States records.

  Note the asymmetry: the dashboard's `criteria-table` **paginates but never loads** — it has `[paginator]="true"` and no `[loading]` binding, so it shows arrows and never a spinner.

PrimeNG's table also ships sort and filter icons (`SortAltIcon`, `ArrowUpIcon`, `ArrowDownIcon`, `FilterIcon`), but **none of them render here** — no template uses `pSortableColumn`, `[sortField]` or `[filters]`. They are listed only so a future `sortable` flag is understood to add icons nobody chose.

**Actions that deliberately ship without an icon.** Every `.btn` outside the auth submit is text-only: `Đóng`, `Huỷ`, `Sao chép`, `In`, `Lưu`, `Xem`, `Xuất báo cáo`, `Import CSV/Excel`, `+ Thêm chỉ tiêu`, `+ Thêm người dùng`, `Lưu thay đổi`, `Nhập dữ liệu`, the two `Phân quyền` tabs, and `Đổi mật khẩu`. The `+` in the two "add" buttons is a literal plus character in the label string, not an icon. Every `<select>` filter and the criteria search box are likewise unadorned — only the *user* search box has a magnifier.

## Legacy Exceptions

<!-- As-shipped uses of non-standard icon sets. Record them faithfully — specs must show what ships, never silently swap in the standard set. Mirror the set names in `legacy_exceptions` frontmatter. -->

| Set | Where it lingers | On disk |
|-----|------------------|---------|
| Unicode arrows `↑` / `↓` | Prepended to the delta string inside `DeltaIndicator`, so they are part of the text content rather than a sibling element | `modules/dashboard/components/delta-indicator/delta-indicator.ts:38` |
| Unicode box-drawing `└` | Child-row marker in the menu permission matrix's first column | `platform/phan-quyen/components/permission-matrix/permission-matrix.html:16` |
| Unicode bullet `●` | Baked into the user-status badge label (`● Đã khoá` / `● Đang hoạt động`), so the dot cannot be styled independently of the text | `platform/quan-tri-nguoi-dung/components/user-grid-table/user-grid-table.html:42,44` |

## Normalize on redesign

<!-- Numbered replacement plan: each legacy icon → its standard-library equivalent. Applied only during redesigns, never retro-fitted into as-shipped specs. -->

1. `↑`/`↓` in `DeltaIndicator` → `pi pi-arrow-up` / `pi pi-arrow-down` as sibling elements, so direction can be sized and coloured independently of the number and read correctly by assistive tech.
2. `●` in the user-status badge → a styled `<span>` dot or `pi pi-circle-fill`; today it is inside the translated label string, so it cannot be restyled without editing copy.
3. `└` in the permission matrix → CSS-drawn tree lines (`border-left`/`::before`), which survive font substitution and do not read as text content.
4. Icon size is not tokenised — `15px`, `13px` and `12px` appear as literals at three call sites while the type scale stops at `--fs-lg` (15px). Add an icon-size scale, or reference `--fs-*` where the values already coincide.
5. Two different affordances exist for "search": the user grid has a magnifier adornment, the criteria filters have a bare placeholder-only input (`criteria-table.html:8`, `danh-muc-dti.page.html:14`). Converge on one.
6. `.field-input` icons are decorative and `pointer-events:none` but are **not** `aria-hidden`, unlike the sidebar's `.navicon` wrappers — screen readers may announce the font glyph. Hide them consistently.
7. **The paginator announces itself in English.** PrimeNG labels its paginator buttons from `aria` defaults — `First Page`, `Previous Page`, `Next Page`, `Last Page` (`primeng-config.mjs`) — and `app.config.ts` passes `providePrimeNG({ theme })` with **no `translation` block**, so those defaults stand. Every visible string in this product is Vietnamese; the screen-reader labels on three paginated tables are not. Fix by adding `translation: { aria: { … } }` to `providePrimeNG`, which also covers any other PrimeNG component adopted later.
8. **The PrimeNG SVG icons carry no `aria-hidden`.** `BaseIcon` sets `data-p-icon` and nothing else, so the arrow and spinner glyphs are exposed to assistive tech as unnamed graphics sitting inside already-labelled buttons — the label is announced, then the shape again. The app cannot patch this per-instance; it belongs in a global CSS/base-class override or an upstream report.
9. **Two icon systems ship side by side** — an icon *font* the app authors (`pi pi-*`) and inline *SVG* the component library injects. They scale differently (`font-size` vs `width`/`height`), colour differently (`color` vs `fill`/`currentColor`), and only one of them is greppable from `src/FE/`. Any icon-size scale added under item 4 must cover both or it will silently apply to half the icons.

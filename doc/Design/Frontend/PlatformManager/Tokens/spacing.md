---
project: "PlatformManager"
status: "draft"
updated: "2026-08-22"
category: "spacing"
live_source: "src/FE/src/styles.scss"
---

# Spacing — PlatformManager Design System

> **Fidelity:** every value below is extracted from the live app AS-SHIPPED — never invent values outside this file. Proposed changes go to "Normalize on redesign" in the relevant spec, not here.

## Live Source & Extraction Method

**Live source changed 2026-08-22** — re-extracted from the shipped Angular 20 app (`src/FE/`) instead of `doc/Prototype/dashboard.html`, per `doc/Design/CLAUDE.md` § Fidelity Policy. The previous revision opened with *"No spacing scale/mixins exist"*; that is no longer true. The app ships a five-step `--sp-*` scale and a six-step `--radius-*` scale in `:root` (`src/FE/src/styles.scss:69-80`), plus three structural measurements (`:82-85`).

Same caveat as typography: `styles.scss:1-9` warns in the source that these compact-density values post-date the prototype-era extraction and that `/design-extract-tokens` had not been re-run. This refresh is that re-run.

Spacing does not vary by theme (no dark mode exists — see `Tokens/colors.md`), so every token here belongs to the `global` set in `tokens.json`.

## Token Table — Padding & Gap Scale

`:root`, `src/FE/src/styles.scss:69-73`. The scale is non-linear and tight — a deliberate "mật độ hiển thị (compact)" choice recorded in the header comment.

| Name | Value | Live variable / selector | Source line |
| --- | --- | --- | --- |
| sp-1 | `4px` | `--sp-1` — `.kpi .value` margin-top, `.action-btn` margin-right, `.role-checkboxes` margin-top, collapsed sidebar brand padding | `styles.scss` |
| sp-2 | `6px` | `--sp-2` — `.btn` vertical padding, `th`/`td` vertical padding, all input vertical padding, `.form-row` gap, `.sidebar-nav` padding, `.history` gap, `.histrow` padding | `styles.scss` |
| sp-3 | `8px` | `--sp-3` — `.btn` horizontal padding, `th`/`td` horizontal padding, `.filters` gap, `.title` gap, `.notice` vertical padding, `.toast-stack` gap, `.tabs-bar` gap, sidebar nav gap | `styles.scss` |
| sp-4 | `10px` | `--sp-4` — `.title` margin-bottom, `.form-row` margin-bottom, `.field` margin-bottom, `.filters` vertical margin, `.topin` vertical padding, `.kpis` gap, `.sidebar-brand` padding, `.seg-btn` horizontal padding | `styles.scss` |
| sp-5 | `14px` | `--sp-5` — `.card` padding, `dialog` padding, `main` padding, `.topin` horizontal padding, `.notice` horizontal padding + margin-bottom, `.layout` gap + margin-top, `.toast-stack` offsets, `.login-shell` padding | `styles.scss` |

### Padding & gap literals bypassing the scale

| Value | Selector | Source line |
| --- | --- | --- |
| `3px 6px` | `.badge` | `styles.scss:305` |
| `4px` | `.toggle-visibility` | `styles.scss:556` |
| `4px var(--sp-2)` | `.action-btn` (vertical literal, horizontal tokenised) | `styles.scss:215` |
| `5px` | `.progressInput`, `.noteInput` | `criteria-grid-table.scss:35`, `:53` |
| `6px` | collapsed-sidebar flyout submenu padding | `sidebar.scss:311` |
| `8px` | `.dialog-actions` gap (×5 dialogs), `.kpis` mobile gap, `.btn-block` gap, `.login-error` gap | `confirm-dialog.scss:9`, `criteria-form-dialog.scss:7`, `import-dialog.scss:5`, `report-dialog.scss:5`, `user-form-dialog.scss:29`, `kpi-summary.scss:15`, `styles.scss:604`, `:619` |
| `6px` | `.field-row label` gap, `.role-checkbox` gap, `.row-actions` gap | `styles.scss:581`, `user-form-dialog.scss:14`, `user-grid-table.scss:61` |
| `9px` | `.sidebar-hamburger` | `topbar.scss:54` |
| `10px` | `main` padding ≤560px, `.sidebar-navitem` padding ≤560px | `app.scss:40`, `sidebar.scss:284` |
| `10px 12px` | collapsed-sidebar flyout `.sidebar-subitem` | `sidebar.scss:320` |
| `10px 12px 10px 36px` | `.field-input input` (36px left inset clears the leading `pi` icon) | `styles.scss:536` |
| `11px` | `.btn-block` | `styles.scss:599` |
| `12px 4px` | `.footer` | `styles.scss:489` |
| `2px 8px` | `.role-tag` | `user-grid-table.scss:40` |
| `20px` | `.field-row` margin-bottom | `styles.scss:575` |
| `24px` | `.login-brand` margin-bottom | `auth-card.scss:25` |
| `28px` | `td.indent` padding-left (permission tree) | `permission-matrix.scss:9` |
| `32px 28px` | `.login-card` padding | `auth-card.scss:17` |
| `38px` | `.sidebar-subitem` padding-left | `sidebar.scss:185` |
| `12px` / `16px` | `.dialog-actions` and card margin-top (mixed, ×6) | `import-dialog.scss:6`, `import-result-dialog.scss:22`, `report-dialog.scss:6`, `dashboard.page.scss:18`, `criteria-table.scss:2`, `confirm-dialog.scss:10` |

## Token Table — Radius Scale

`:root`, `src/FE/src/styles.scss:75-80`.

| Name | Value | Live variable / selector | Source line |
| --- | --- | --- | --- |
| radius-sm | `7px` | `--radius-sm` — `.btn`, `.action-btn`, all inputs/selects, `.toast-close`, `.login-error` | `styles.scss` |
| radius-md | `9px` | `--radius-md` — `.sidebar-navitem`, `.toast-item` | `styles.scss` |
| radius-lg | `16px` | `--radius-lg` — `.card`, `.login-card` | `styles.scss` — raised 14px→16px, per the inline note "xu hướng bo góc lớn hơn 2026" |
| radius-dialog | `15px` | `--radius-dialog` — native `dialog` | `styles.scss` |
| radius-table | `12px` | `--radius-table` — `.tablewrap` (×4) | `styles.scss` |
| radius-pill | `999px` | `--radius-pill` — `.badge`, `.bar`, `.fill` | `styles.scss` |

### Radius literals bypassing the scale

| Value | Selector | Source line |
| --- | --- | --- |
| `3px` (`0 3px 3px 0`) | `.sidebar-navitem.active::before` rail | `sidebar.scss:147` |
| `6px` | `.toggle-visibility`, `.cell-icon-btn`, `.role-tag` | `styles.scss:557`, `criteria-grid-table.scss:60`, `user-grid-table.scss:39` |
| `7px` | `.brand-mark` (sidebar), `.sidebar-toggle` — numerically `--radius-sm` | `sidebar.scss:34`, `:59` |
| `10px` | collapsed-sidebar flyout submenu | `sidebar.scss:312` |
| `12px` | `.notice`, `.brand-mark` (auth) — numerically `--radius-table` | `styles.scss`, `auth-card.scss:31` |
| `50%` | `.avatar` (circle) | `user-grid-table.scss:16` |

## Token Table — Breakpoints

Three, used consistently across all 30 SCSS files (verified by tallying every `@media` block).

| Name | Value | Live variable / selector | Source line |
| --- | --- | --- | --- |
| breakpoint-tablet | `max-width: 980px` | `@media(max-width:980px)` — 6 blocks | `app.scss:32`, `sidebar.scss:236`, `topbar.scss:49`, `kpi-summary.scss:7`, `group-progress-list.scss:31`, `dashboard.page.scss:35` |
| breakpoint-mobile | `max-width: 560px` | `@media(max-width:560px)` — 8 blocks | `app.scss:38`, `sidebar.scss:278`, `topbar.scss:39`, `kpi-tile.scss:38`, `kpi-summary.scss:13`, `group-progress-list.scss:37`, `period-toolbar.scss:75`, `danh-muc-dti.page.scss:14` |
| breakpoint-desktop | `min-width: 981px` | `@media(min-width:981px)` — 1 block, the collapsed-sidebar hover flyout | `sidebar.scss:289` |
| breakpoint-print | `print` | `@media print` — 4 blocks | `styles.scss:115`, `app.scss:44`, `sidebar.scss:338`, `topbar.scss:58` |

## Token Table — Structural Measurements

### Declared in `:root` (`src/FE/src/styles.scss:82-85`)

| Name | Value | Live variable / selector | Source line |
| --- | --- | --- | --- |
| sidebar-w | `220px` | `--sidebar-w` — `.sidebar` width, `.shell-content` margin-left | `styles.scss` |
| sidebar-w-collapsed | `60px` | `--sidebar-w-collapsed` — `.sidebar.collapsed`, `.shell-content.collapsed` | `styles.scss` |
| container-max-width | `1600px` | `--container-max-width` — `main` and `.topin` | `styles.scss` |

### Shipped as literals

| Name | Value | Live variable / selector | Source line |
| --- | --- | --- | --- |
| shell-height | `100vh` with `100dvh` override | `.shell`, `.shell-content`, `.sidebar`, `.login-shell` | `app.scss:7-8,13-14`, `sidebar.scss:8-9`, `auth-card.scss:2-3` |
| sidebar-w-drawer-tablet | `min(85vw, 300px)` (≤980px, off-canvas) | `.sidebar` | `sidebar.scss:238` |
| sidebar-w-drawer-mobile | `min(90vw, 300px)` (≤560px) | `.sidebar` | `sidebar.scss:280` |
| sidebar-brand-height | `min-height: 50px` | `.sidebar-brand` | `sidebar.scss:28` |
| sidebar-navitem-height-mobile | `min-height: 40px` (≤560px) | `.sidebar-navitem` | `sidebar.scss:285` |
| sidebar-flyout-width | `min-width: 180px` | collapsed-sidebar submenu | `sidebar.scss:307` |
| dialog-width | `min(700px, 92vw)` | `dialog` | `styles.scss:468` |
| dialog-width-form | `min(560px, 92vw)` | `dialog.form-dialog` | `styles.scss:476` |
| dialog-width-confirm | `min(420px, 92vw)` | `dialog.confirm-dialog` | `styles.scss:480` |
| login-card-max-width | `380px` | `.login-card` | `auth-card.scss:12` |
| toast-stack-max-width | `min(360px, 90vw)` | `.toast-stack` | `toast.scss:9` |
| filter-input-min-width | `220px` | `.filters input`, `.search` | `styles.scss:334`, `quan-tri-nguoi-dung.page.scss:3` |
| matrix-max-height | `560px` | `.tablewrap` on both permission matrices | `permission-matrix.scss:5`, `resource-permission-matrix.scss:5` |
| history-max-height | `240px` | `.history` | `history-list.scss:5` |
| chart-height | `220px` | `p-chart [style]="{height:'220px'}"` + `.chart-wrap{min-height:220px}` + `.chart-skeleton{min-height:220px}` | `trend-chart.html:8`, `trend-chart.scss:6`, `dashboard.page.scss:25` |
| progress-bar-height | `9px` | `.bar` | `group-progress-list.scss:19` |
| kpi-sub-min-height | `30px` | `.kpi .sub` — reserves space so tiles align when the sub-line is empty | `kpi-tile.scss:34` |
| criteria-grid-min-width | `1430px` (sum of 12 inline `th` `min-width`s: 70+220+120+90+90+90+110+110+100+130+180+120) | `criteria-grid-table.html:18-29` | `criteria-grid-table.html:18-29` |
| user-grid-min-width | `690px` (sum of 5 inline `th` `min-width`s: 220+140+120+110+100) | `user-grid-table.html:16-20` | `user-grid-table.html:16-20` |
| progress-input-width | `74px` | `.progressInput` | `criteria-grid-table.scss:32` |
| note-input-min-width | `130px` | `.noteInput` | `criteria-grid-table.scss:50` |

### Grid templates

| Name | Value | Live variable / selector | Source line |
| --- | --- | --- | --- |
| kpis-grid-desktop | `repeat(5, 1fr)`, gap `--sp-4` | `.kpis` | `kpi-summary.scss:3-4` |
| kpis-grid-tablet | `repeat(2, 1fr)` (≤980px) | `.kpis` | `kpi-summary.scss:9` |
| kpis-grid-mobile | gap `8px`; last tile spans `1 / -1` (≤560px) | `.kpis` | `kpi-summary.scss:14-20` |
| layout-grid-desktop | `1.15fr 0.85fr`, gap `--sp-5` | `.layout` | `dashboard.page.scss:3-4` |
| layout-grid-tablet | `1fr` (≤980px) | `.layout` | `dashboard.page.scss:37` |
| group-row-grid-desktop | `210px 1fr 80px` | `.group-row` | `group-progress-list.scss:11` |
| group-row-grid-tablet | `140px 1fr 75px` (≤980px) | `.group-row` | `group-progress-list.scss:33` |
| group-row-grid-mobile | `110px 1fr 68px` (≤560px) | `.group-row` | `group-progress-list.scss:39` |
| histrow-grid | `100px 1fr 90px 70px` | `.histrow` | `history-list.scss:11` |
| form-grid | `1fr 1fr`, gap `0 var(--sp-5)` | `.form-grid` | `styles.scss:457-461` |

### Control sizes

| Name | Value | Live variable / selector | Source line |
| --- | --- | --- | --- |
| brand-mark-sidebar | `26px × 26px` | `.sidebar-brand .brand-mark` | `sidebar.scss:32-33` |
| brand-mark-auth | `44px × 44px` | `.login-brand .brand-mark` | `auth-card.scss:29-30` |
| avatar | `30px × 30px` | `.avatar` | `user-grid-table.scss:14-15` |
| sidebar-toggle | `24px × 24px` | `.sidebar-toggle` | `sidebar.scss:60-61` |
| nav-icon | `18px × 18px` | `.navicon` | `sidebar.scss:117-118` |
| cell-icon-btn | `24px × 24px` | `.cell-icon-btn` | `criteria-grid-table.scss:61-62` |
| toast-close | `22px × 22px` | `.toast-close` | `toast.scss:55-56` |
| checkbox | `16px × 16px`, `accent-color: var(--brand)` | permission matrices | `permission-matrix.scss:18-19`, `resource-permission-matrix.scss:9-10` |
| active-nav-rail | `3px` wide, inset `5px` top/bottom, offset `-8px` left | `.sidebar-navitem.active::before` | `sidebar.scss:140-148` |

### Z-index layers (no token declared)

| Value | Layer | Source line |
| --- | --- | --- |
| `4` | sticky `th` | `styles.scss:382` |
| `20` | `.topbar` | `topbar.scss:4` |
| `34` | `.sidebar-backdrop` | `sidebar.scss:229` |
| `35` | `.sidebar` | `sidebar.scss:14` |
| `40` | collapsed-sidebar flyout submenu | `sidebar.scss:314` |
| `60` | `.toast-stack` | `toast.scss:5` |

Native `<dialog>` elements sit on the browser's top layer and are not part of this stack.

## Token Table — Elevation (Shadow)

| Name | Value | Live variable / selector | Source line |
| --- | --- | --- | --- |
| shadow | `0 4px 16px rgba(23,39,67,.1), 0 1px 3px rgba(23,39,67,.06)` | `--shadow` — `.card`, `.login-card`, `.toast-item`, sidebar drawer, sidebar flyout | `styles.scss` |
| shadow-primary-hover | `0 8px 20px rgba(15,91,215,0.35)` | `.btn.primary:hover` | `styles.scss:157` |
| shadow-btn-hover | `0 3px 10px rgba(23,39,67,0.1)` | `.btn:hover` | `styles.scss:177` |
| shadow-dialog | `0 24px 70px rgba(0,0,0,0.25)` | `dialog` | `styles.scss:467` |
| shadow-focus-ring | `0 0 0 3px rgba(15,91,215,0.12)` | `.field-input input:focus-visible` | `styles.scss:544` |

Full color values for the shadow tokens are cross-referenced in `Tokens/colors.md`.

### Motion (no token declared)

| Value | Where | Source line |
| --- | --- | --- |
| `0.1s ease` | `.btn` transform | `styles.scss:147` |
| `0.15s ease` | `.btn`, `.action-btn`, `.cell-icon-btn`, `.seg-btn`, `.field-input input`, `.navchevron`, `toast-in` | `styles.scss:147,220,539`, `criteria-grid-table.scss:72`, `period-toolbar.scss:54`, `sidebar.scss:162`, `toast.scss:24` |
| `0.2s ease` | `.shell-content` margin, `.sidebar` width, `.sidebar-toggle i` transform | `app.scss:15`, `sidebar.scss:15`, `:70` |
| `0.25s ease` | `.sidebar` transform (drawer slide) | `sidebar.scss:15` |

## Chart Palette

<!-- N/A for spacing — see Tokens/colors.md. Chart box metrics are in § Structural Measurements (chart-height). -->

## Drift from the prototype-era extraction

| Token | Prototype-era value | Shipped value |
| --- | --- | --- |
| container-max-width | `1450px` | **`1600px`** |
| card padding | `15px` | `14px` (`--sp-5`) |
| radius-card *(now `radius-lg`)* | `14px` | **`16px`** |
| radius-button *(now `radius-sm`)* | `10px` | `7px` |
| radius-input | `8px` | `7px` (`--radius-sm`) |
| radius-select | `9px` | `7px` (`--radius-sm`) |
| radius-notice | `11px` | `12px` literal |
| radius-table | `12px` | `12px` — unchanged |
| radius-dialog | `15px` | `15px` — unchanged |
| radius-pill | `999px` | `999px` — unchanged |
| breakpoint-tablet / -mobile | `980px` / `560px` | unchanged |
| chart-height | `245px` | **`220px`** |
| group-row-grid-desktop | `230px 1fr 90px` | `210px 1fr 80px` |
| histrow-grid | `110px 1fr 95px 80px` | `100px 1fr 90px 70px` |

**Added (live, previously undocumented):** the whole `--sp-1…5` scale, `--sidebar-w`, `--sidebar-w-collapsed`, `breakpoint-desktop`, and every structural/control/z-index/motion row above.

**Removed (no longer exists in `src/FE/`):**

| Token | Prototype value | Why removed |
| --- | --- | --- |
| table-min-width | `1200px` | The global `table` rule sets `width:100%` only (`styles.scss:363-367`). Minimum widths are now per-column inline `th` styles — see `criteria-grid-min-width` / `user-grid-min-width` above. |
| space-fab-offset | `18px` | FAB not ported — `grep -rn fab src/FE/src` returns nothing. |
| space-fab-padding | `13px 17px` | Same. |
| shadow-fab | `0 12px 30px rgba(15,91,215,.3)` | Same. |

## Normalize on redesign

1. **The `--sp-*` scale is bypassed roughly as often as it is used.** `8px` appears as a literal in 8 places while `--sp-3` *is* `8px`; `10px` appears twice while `--sp-4` *is* `10px`. Those swaps are free (zero visual change). The genuinely off-scale values — `11px`, `20px`, `24px`, `28px`, `32px 28px`, `38px` — need either scale extension or rounding.
2. **`.dialog-actions` is redeclared in five dialog components** with the same three properties and a hand-typed `gap: 8px`, differing only in `margin-top` (`8px` / `12px` / `16px`). One shared class would remove five copies and the margin inconsistency.
3. ~~**`.footer` is declared twice**~~ — **FIXED 2026-08-22.** The `dashboard.page.scss` duplicate was deleted; `styles.scss:486-500` is the single declaration. (The old citation here also had the wrong line range for the global copy — `484-498` rather than `486-500`.)
4. **No `--z-*` scale.** Six hand-picked z-index values with no documented ordering; `34`/`35` in particular only make sense if you read both files.
5. **No motion tokens.** Four durations across 15 declarations, and no `prefers-reduced-motion` guard anywhere in `src/FE/`.
6. **`--radius-sm` (`7px`) is re-typed as the literal `7px` twice in `sidebar.scss`** (`:34`, `:59`), and `--radius-table` (`12px`) as a literal in `styles.scss:257` and `auth-card.scss:31`.
7. **`container-max-width: 1600px` with `--sidebar-w: 220px`** means the content column can reach 1600px on top of a 220px rail — on a 1920px display the shell fills nearly edge to edge. Worth re-checking against the 1440px design viewport the screenshots are captured at.

## Appendix: tokens.json rules

- Format: W3C DTCG — every token is an object with `$type` and `$value`.
- Top-level sets: `global` (theme-invariant) plus `light` and `dark` (theme overrides only). Spacing/radius/breakpoint/dimension tokens are theme-invariant and live entirely in `global`.
- Figma import via Tokens Studio: enable `global` + exactly ONE theme set at a time — never both themes together.

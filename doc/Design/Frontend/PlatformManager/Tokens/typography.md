---
project: "PlatformManager"
status: "draft"
updated: "2026-08-22"
category: "typography"
live_source: "src/FE/src/styles.scss"
---

# Typography — PlatformManager Design System

> **Fidelity:** every value below is extracted from the live app AS-SHIPPED — never invent values outside this file. Proposed changes go to "Normalize on redesign" in the relevant spec, not here.

## Live Source & Extraction Method

**Live source changed 2026-08-22** — re-extracted from the shipped Angular 20 app (`src/FE/`) instead of the deleted prototype, per `doc/Design/CLAUDE.md` § Fidelity Policy. The previous revision described a per-selector free-for-all with no scale; the app **does** have a scale now.

**The scale.** Five `--fs-*` custom properties in `:root` (`src/FE/src/styles.scss:63-67`) — the density-compact set. `styles.scss:1-9` records why they exist and warns, in the source itself, that this design system had not yet caught up: *"Giá trị font-size/spacing dùng đúng bộ token 'mật độ hiển thị (compact)' … muộn hơn Tokens/typography.md + Tokens/spacing.md … pipeline /design-extract-tokens chưa chạy lại."* This refresh is that re-run.

**Font family.** One stack, declared once on `body` (`styles.scss:91`) and inherited everywhere. No per-heading font, no `@font-face`. ⚠️ **Inter is NOT loaded on the current working copy** (checked 2026-08-23: `src/FE/src/index.html` is 14 lines, no font link). An earlier revision claimed, at `index.html:21-23`: `preconnect` to `fonts.googleapis.com` **and** `fonts.gstatic.com` (the `.woff2` files come from `gstatic`, so preconnecting to only the stylesheet host leaves the actual font fetch paying a fresh handshake), then a stylesheet at weights **400;500;600;700** with `display=swap`.

Until that landed the stack was a promise the app could not keep: nothing loaded the face, so rendering fell through to whatever the OS provided — Segoe UI on a stock Windows box, while every spec said Inter. **The gap is now narrower but not closed**: the app also ships weights **750, 800 and 850** (see § Font weights below), none of which are among the four loaded, so the browser synthesises or rounds them. Recorded as-shipped.

**Weights are not tokenised.** Six distinct `font-weight` values ship (400, 600, 700, 750, 800, 850) as literals across 24 declarations. There is no `--fw-*` scale. Documented below as observed values, not invented tokens.

**Sizes outside the scale.** Seven `font-size` literals bypass `--fs-*`. Listed below with source lines; flagged in § Normalize on redesign, not silently folded into the scale.

`grep` recipe used: `grep -rhoE 'font-(size|weight|family):\s*[^;]+' src/FE/src --include=*.scss | sort | uniq -c`. Result: 43 of 50 `font-size` declarations reference `var(--fs-*)`; 7 do not.

## Token Table

### Font family — `src/FE/src/styles.scss:91`

| Name | Value | Live variable / selector | Source line |
| --- | --- | --- | --- |
| font-family-base | `Inter, 'Segoe UI', Arial, sans-serif` | `body{font-family:…}` | `styles.scss:91` — Inter loaded from Google Fonts at `index.html:21-23` (weights 400;500;600;700 only) |

### Font size scale — `:root`, `src/FE/src/styles.scss:63-67`

| Name | Value | Live variable / selector | Source line |
| --- | --- | --- | --- |
| fs-xs | `11px` | `--fs-xs` | `styles.scss` — `.muted`, `th`, `.action-btn`, `.footer`, KPI label/sub, `.histrow`, avatar/brand marks, `.role-tag`, `.user-email` (12 uses) |
| fs-sm | `12px` | `--fs-sm` | `styles.scss` — the workhorse: `.btn`, `th`/`td`, all inputs/selects, `.notice`, `.badge` container, form labels, toast text, sidebar nav (25 uses) |
| fs-base | `13px` | `--fs-base` | `styles.scss:58` — set on `body` (`styles.scss`); the document's inherited base |
| fs-md | `14px` | `--fs-md` | `styles.scss` — `.title h2`, `.btn-block`, sidebar brand text (3 uses) |
| fs-lg | `15px` | `--fs-lg` | `styles.scss` — topbar `h1`, login brand mark (2 uses) |

### Composite roles as-shipped (size + weight per selector)

Reconstructed by pairing each selector's `font-size` with its `font-weight`; these are descriptions of shipped rules, not additional custom properties.

| Name | Value | Live variable / selector | Source line |
| --- | --- | --- | --- |
| body | `13px` / `400` | `body{font-size:var(--fs-base)}` | `styles.scss:89-95` |
| h1-topbar | `15px` / UA bold (`700`) | `.logo h1{font-size:var(--fs-lg)}` | `shared/components/topbar/topbar.scss:19-22` |
| h1-auth | `18px` / `800` | `.login-brand h1` | `shared/components/auth-card/auth-card.scss:41-45` |
| h2-title | `14px` / UA bold (`700`) | `.title h2{font-size:var(--fs-md)}` | `styles.scss:279-282` |
| button-label | `12px` / `700` | `.btn{font-size:var(--fs-sm);font-weight:700}` | `styles.scss:144-145` |
| button-block-label | `14px` / `700` | `.btn-block{font-size:var(--fs-md)}` (weight inherits `.btn`) | `styles.scss:597-605` |
| action-btn-label | `11px` / `700` | `.action-btn{font-size:var(--fs-xs);font-weight:700}` | `styles.scss:216-217` |
| table-header | `11px` / `700`, `letter-spacing:0.01em` | `th` | `styles.scss:378-388` |
| table-cell | `12px` / `400`, `line-height:1.4` | `th,td{font-size:var(--fs-sm);line-height:1.4}` | `styles.scss:369-376` |
| badge | `10px` / `750` | `.badge{font-size:10px;font-weight:750}` | `styles.scss:302-308` — **size not on the scale** |
| delta | `850`, size inherits `td` (`12px`) | `.delta{font-weight:850}` | `styles.scss:403-405` |
| kpi-value | `21px` / `850` | `.kpi .value` | `modules/dashboard/components/kpi-tile/kpi-tile.scss:11-13` — **size not on the scale** |
| kpi-label / kpi-sub | `11px` / `400` | `.kpi .label`, `.kpi .sub` (`line-height:1.4` on `.sub`) | `kpi-tile.scss:6-9`, `:29-35` |
| form-label | `12px` / `700` | `.form-row label`, `.field label` | `styles.scss:427-432`, `:509-515` |
| muted-caption | `11px` / `400` | `.muted{font-size:var(--fs-xs)}` | `styles.scss:285-288` |
| sidebar-nav-item | `12px` / `600` (`700` when `.active`) | `.sidebar-navitem` | `shared/components/sidebar/sidebar.scss:106-107`, `:134` |
| sidebar-brand-text | `14px` / `800` | `.brand-text` | `sidebar.scss:45-47` |
| brand-mark | `11px` / `800` (sidebar) · `15px` / `800` (auth) | `.brand-mark` | `sidebar.scss:40-41`, `auth-card.scss:37-38` |
| avatar-initials | `11px` / `800` | `.avatar` | `platform/quan-tri-nguoi-dung/components/user-grid-table/user-grid-table.scss:22-23` |
| toast-text | `12px` / `400`, `line-height:1.4` | `.toast-item`, `.toast-text` | `shared/components/toast/toast.scss:22`, `:43-46` |
| footer | `11px` / `400` | `.footer{font-size:var(--fs-xs)}` | `styles.scss:486-500` |

### Font weights as-shipped (no token declared)

| Value | Where | Source line |
| --- | --- | --- |
| `400` | body default only — **no explicit `font-weight: 400` declaration ships anywhere in `src/FE/`**; every 400 is inherited | — (nothing to cite) |
| `600` | sidebar nav item, `.role-checkbox` | `sidebar.scss:107`, `user-form-dialog.scss:16` |
| `700` | **12** declarations — `.btn`, `.action-btn`, `th`, form labels, `.seg-btn`, links, `.user-name`, `.topbar-user-name`, active nav item | `styles.scss:152,219,265,388,432,493,514,590`; `topbar.scss:34`; `sidebar.scss:134`; `period-toolbar.scss:50`; `user-grid-table.scss:28` |
| `750` | `.badge` only | `styles.scss:307` |
| `800` | brand marks (×2), brand text, avatar, auth `h1` | `sidebar.scss:40,46`; `auth-card.scss:37,44`; `user-grid-table.scss:22` |
| `850` | `.kpi .value`, `.delta` | `kpi-tile.scss:13`; `styles.scss:404` |

### Font sizes bypassing the `--fs-*` scale

| Value | Selector | Source line |
| --- | --- | --- |
| `10px` | `.badge` | `styles.scss:306` |
| `12px` | `.cell-icon-btn` | `modules/danh-muc-dti/components/criteria-grid-table/criteria-grid-table.scss:65` |
| `12px` | `.sidebar-navparent .navchevron` | `sidebar.scss:164` |
| `13px` | `.search .pi` | `platform/quan-tri-nguoi-dung/pages/quan-tri-nguoi-dung/quan-tri-nguoi-dung.page.scss:17` |
| `13.5px` | `.confirm-message` | `modules/danh-muc-dti/components/confirm-dialog/confirm-dialog.scss:2` |
| `15px` | `.field-input .pi` | `styles.scss:527` |
| `15px` | `.sidebar-navitem .navicon` | `sidebar.scss:124` |
| `18px` | `.login-brand h1` | `auth-card.scss:43` |
| `21px` | `.kpi .value` | `kpi-tile.scss:12` |

Note `12px` and `15px` are numerically identical to `--fs-sm` and `--fs-lg` — those four declarations could reference the token today with no visual change. `10px`, `13.5px`, `18px` and `21px` have no equivalent on the scale.

### Line heights as-shipped (no token declared)

| Value | Where | Source line |
| --- | --- | --- |
| `1` | `.cell-icon-btn` | `criteria-grid-table.scss:66` |
| `1.4` | `th,td`; `.kpi .sub`; `.toast-text` | `styles.scss:374`; `kpi-tile.scss:33`; `toast.scss:45` |
| `1.55` | `.confirm-message` | `confirm-dialog.scss:3` |
| `1.6` | `.import-summary` | `modules/danh-muc-dti/components/import-result-dialog/import-result-dialog.scss:3` |

### Responsive overrides

| Name | Value | Live variable / selector | Source line |
| --- | --- | --- | --- |
| kpi-value (mobile ≤560px) | `18px` | `@media(max-width:560px){.kpi .value{font-size:18px}}` | `kpi-tile.scss:38-42` |
| topbar-user-name (mobile ≤560px) | hidden (`display:none`) | `@media(max-width:560px){.topbar-user-name{display:none}}` | `topbar.scss:39-43` |

The prototype's `h1-logo` mobile override (18px→16px) was **not** ported — `.logo h1` stays `--fs-lg` at every viewport.

## Chart Palette

<!-- N/A for typography — see Tokens/colors.md. Chart tick labels inherit no font-size override; Chart.js applies its own defaults. -->

## Normalize on redesign

1. ~~**`Inter` is named but never loaded.**~~ — **FIXED 2026-08-22** (`index.html:21-23`, Google Fonts, weights 400;500;600;700, `display=swap`, preconnect to both hosts). **What remains:** three of the six shipped weights — `750`, `800`, `850` — are outside the loaded set, so the browser fakes or rounds them. Either add `800` to the Google Fonts request (`wght@400;500;600;700;800`) and round `750`/`850` onto real steps, or accept synthesis as a deliberate choice. Today it is neither — it is an oversight nobody has looked at. See item 3.
2. **Nine `font-size` literals bypass `--fs-*`** (listed above). Four of them (`12px`, `15px`) are exact scale values and can be swapped with zero visual change today. `10px`, `13.5px`, `18px`, `21px` need either a scale extension (`--fs-2xs`, `--fs-xl`, `--fs-2xl`) or rounding onto an existing step.
3. **No `--fw-*` scale.** Six weights ship as literals, including unusual synthetic steps (`750`, `850`) that most static font files cannot render and that the browser will fake or round to 700/800.
4. **No `--lh-*` scale.** Five line-height values, four of them one-offs.
5. **`13.5px` is a sub-pixel size** on a single selector (`.confirm-message`) — an accident rather than a decision.
6. **`.title h2` and `.logo h1` rely on the UA bold default** rather than declaring a weight, so the rendered weight depends on the user agent.

## Appendix: tokens.json rules

- Format: W3C DTCG — every token is an object with `$type` and `$value`.
- Top-level sets: `global` (theme-invariant) plus `light` and `dark` (theme overrides only). Typography tokens are theme-invariant (no dark-mode differences shipped) and live entirely in `global`.
- Figma import via Tokens Studio: enable `global` + exactly ONE theme set at a time — never both themes together.

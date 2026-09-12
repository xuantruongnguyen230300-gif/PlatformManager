---
kind: luat
scope: du-an
verified: 2026-09-06
project: "PlatformManager"
status: "draft"
updated: "2026-09-06"
category: "typography"
live_source: "src/FE/src/styles.scss"
---

# Typography — PlatformManager Design System

> **Fidelity:** every value below is extracted from the live app AS-SHIPPED — never invent values outside this file. Proposed changes go to "Normalize on redesign" in the relevant spec, not here.

## Live Source & Extraction Method

**Re-extracted 2026-08-29**, mirroring the `src/FE/src/styles.scss` rewrite of the same day. The font family changed; the size scale did not.

**The scale.** Five `--fs-*` custom properties in `:root` (`src/FE/src/styles.scss` § `--fs-xs` … `--fs-lg`), the density-compact set. Values are unchanged from the 2026-08-22 extraction — re-read and re-confirmed 2026-08-29.

**Font family — changed 2026-08-29.** One stack, declared once on `body` (`src/FE/src/styles.scss`) and inherited everywhere: `'Be Vietnam Pro', 'Segoe UI', Arial, sans-serif`. The previous stack named `Inter`.

**The face is self-hosted, and the `@font-face` blocks now live in the stylesheet itself.** The `@font-face` set is declared inside `src/FE/src/styles.scss` (§ `@font-face`) — 5 weights (400 / 500 / 600 / 700 / 800) × 3 `unicode-range` subsets (latin, latin-ext, vietnamese) — each pointing at an **absolute** `url(/fonts/<hash>.woff2)`. `angular.json` copies `public/**/*` to the output root and `index.html` carries `<base href="/">`, so `/fonts/…` resolves onto the files in `src/FE/public/fonts/`. Count the blocks and the files rather than copying a number:

```bash
grep -c '^@font-face' src/FE/src/styles.scss
ls src/FE/public/fonts/*.woff2 | wc -l
```

PASS = the two numbers are equal.

> ✅ **The broken font path is fixed — verified 2026-08-29.** The previous revision recorded a real defect: `index.html` linked a separate `src/FE/public/fonts/be-vietnam-pro.css` whose every `src: url(fonts/<hash>.woff2)` resolved one directory too deep, to `/fonts/fonts/…`. That stylesheet no longer exists (`ls src/FE/public/fonts/` returns `.woff2` files only) and `index.html` no longer links any font stylesheet — `src/FE/src/index.html` is 14 lines and contains no `<link rel="stylesheet">` at all. The comment above the set in `src/FE/src/styles.scss` (`grep -n 'tự phục vụ' src/FE/src/styles.scss`) records why the move was made: Angular rewrites `<link>` hrefs at build time, which is what mangled the relative path in the first place, whereas a bundler leaves an absolute `url(/…)` alone.
>
> Still **not** confirmed against a running browser — the check above is path arithmetic plus a directory listing, because the dev server was not started for this pass. The claim being made is narrower than "the font renders": it is that the two ends now agree.

**Weights are not tokenised.** Six distinct `font-weight` values ship as literals. There is no `--fw-*` scale. Documented below as observed values, not invented tokens.

**Sizes outside the scale.** Some `font-size` declarations bypass `--fs-*`; they are enumerated below with their selector, and flagged in § Normalize on redesign rather than silently folded into the scale.

Recipes used, re-runnable from the repo root:

- family / weights / sizes: `grep -rn 'font-\(size\|weight\|family\)' src/FE/src --include=*.scss`
- how many bypass the scale: total minus `grep -rc 'font-size: var' src/FE/src --include=*.scss`
- loaded weights: `awk '/^@font-face/,/^}/' src/FE/src/styles.scss | grep -o 'font-weight: [0-9]*' | sort -u`

**Citation policy — no line numbers into `styles.scss` at all.** The rule and its reasoning are held in one place: `doc/Design/CLAUDE.md` § Neo trích dẫn vào `styles.scss`. Read it there. Every row below cites the file plus the identifier.

> 🔄 **SỬA 2026-09-06.** The previous revision of this paragraph kept the carve-out *"line numbers are given **only for the `:root` block** … which held still across two edits"*, and pinned that block to a line range. The range was wrong: `:root` opens and closes 100 lines apart, at neither of the two numbers given. `Tokens/colors.md` § Live Source dropped the same carve-out on 2026-09-04 with the full reasoning; this file and `Tokens/spacing.md` were left carrying it. Locate the block with `grep -n '^:root' src/FE/src/styles.scss` instead.

## Token Table

### Font family — `src/FE/src/styles.scss`

| Name | Value | Live variable / selector | Declared at |
| --- | --- | --- | --- |
| font-family-base | `'Be Vietnam Pro', 'Segoe UI', Arial, sans-serif` | `body{font-family:…}` | `src/FE/src/styles.scss` — faces self-hosted from `src/FE/public/fonts/`, declared by the `@font-face` set in the same stylesheet (`src/FE/src/styles.scss` § `@font-face`) |

### Font size scale — `src/FE/src/styles.scss` § `:root`

| Name | Value | Live variable / selector | Declared at |
| --- | --- | --- | --- |
| fs-xs | `11px` | `--fs-xs` | `src/FE/src/styles.scss` § `--fs-xs` — `.muted`, `th`, `.btn.sm`, `.filter-chip`, `.footer`, sidebar brand mark, avatar initials, `.user-email` |
| fs-sm | `12px` | `--fs-sm` | `src/FE/src/styles.scss` § `--fs-sm` — the workhorse: `.btn`, `.seg-btn`, `th`/`td`, every input, `.notice`, form labels, `.dialog-desc`, toast text, sidebar nav |
| fs-base | `13px` | `--fs-base` | `src/FE/src/styles.scss` § `--fs-base` — set on `body` (`src/FE/src/styles.scss`); the document's inherited base |
| fs-md | `14px` | `--fs-md` | `src/FE/src/styles.scss` § `--fs-md` — `.title h2`, `.btn-block`, sidebar brand text |
| fs-lg | `15px` | `--fs-lg` | `src/FE/src/styles.scss` § `--fs-lg` — topbar `h1`, `.dialog-title`, auth brand mark |

None of the five moved in the 2026-08-29 rewrite.

### Composite roles as-shipped (size + weight per selector)

Reconstructed by pairing each selector's `font-size` with its `font-weight`; these describe shipped rules, they are not additional custom properties.

| Name | Value | Live variable / selector | Declared at |
| --- | --- | --- | --- |
| body | `13px` / `400` | `body{font-size:var(--fs-base)}` | `src/FE/src/styles.scss` |
| h1-topbar | `15px` / UA bold (`700`) | `.logo h1{font-size:var(--fs-lg)}` — weight not declared | `src/FE/src/app/shared/components/topbar/topbar.scss` |
| h1-auth | `18px` / `800` | `.login-brand h1` | `src/FE/src/app/shared/components/auth-card/auth-card.scss` — **size off the scale** |
| h2-title | `14px` / UA bold (`700`) | `.title h2{font-size:var(--fs-md)}` — weight not declared | `src/FE/src/styles.scss` |
| button-label | `12px` / `700` | `.btn` | `src/FE/src/styles.scss` |
| button-sm-label | `11px` / `700` (inherited from `.btn`) | `.btn.sm` — the in-row action button | `src/FE/src/styles.scss` |
| button-block-label | `14px` / `700` (inherited from `.btn`) | `.btn-block` | `src/FE/src/styles.scss` |
| segmented-label | `12px` / `700` | `.seg-btn` | `src/FE/src/styles.scss` |
| table-header | `11px` / `700`, `letter-spacing:.01em` | `th` | `src/FE/src/styles.scss` |
| table-cell | `12px` / `400`, `line-height:1.4` | `th,td` | `src/FE/src/styles.scss` |
| badge | `10px` / `750` | `.badge` | `src/FE/src/styles.scss` — **size off the scale, weight outside the loaded set** |
| delta | `850`, size inherits `td` (`12px`) | `.delta` | `src/FE/src/styles.scss` — **weight outside the loaded set** |
| text-emphasis | `700`, size inherits context | `.text-good` / `.text-warn` / `.text-bad` | `src/FE/src/styles.scss` |
| form-label | `12px` / `700` | `.form-row label`, `.field label` | `src/FE/src/styles.scss` |
| dialog-title | `15px` / `800` | `.dialog-title` | `src/FE/src/styles.scss` |
| dialog-desc | `12px` / `400`, `line-height:1.5` | `.dialog-desc` | `src/FE/src/styles.scss` |
| filter-count | `10px` / `800` | `.filter-count` — the active-filter counter on the toolbar | `src/FE/src/styles.scss` — **size off the scale** |
| filter-chip | `11px` / `700` | `.filter-chip` | `src/FE/src/styles.scss` |
| notice | `12px` / `400` | `.notice` | `src/FE/src/styles.scss` |
| muted-caption | `11px` / `400` | `.muted` | `src/FE/src/styles.scss` |
| footer | `11px` / `400` | `.footer` | `src/FE/src/styles.scss` |
| sidebar-nav-item | `12px` / `600` (`700` when `.active`) | `.sidebar-navitem` | `src/FE/src/app/shared/components/sidebar/sidebar.scss` |
| sidebar-brand-text | `14px` / `800` | `.brand-text` | `src/FE/src/app/shared/components/sidebar/sidebar.scss` |
| brand-mark | `11px` / `800` (sidebar) · `15px` / `800` (auth) | `.brand-mark` | `src/FE/src/app/shared/components/sidebar/sidebar.scss`, `src/FE/src/app/shared/components/auth-card/auth-card.scss` |
| avatar-initials | `11px` / `800` | `.avatar` | `src/FE/src/app/platform/quan-tri-nguoi-dung/components/user-grid-table/user-grid-table.scss` |
| topbar-user-name | `12px` / `700` | `.topbar-user-name` | `src/FE/src/app/shared/components/topbar/topbar.scss` |
| toast-text | `12px` / `400`, `line-height:1.45` | `.toast-item` sets the size, `.toast-text` the line-height and `--muted` ink; a `.toast-text:only-child` (no title) flips the ink to `--text` | `src/FE/src/app/shared/components/toast/toast.scss` |
| toast-title | `12px` / `800`, inherits the `.toast-item` size | `.toast-title` | `src/FE/src/app/shared/components/toast/toast.scss` |

### Font weights as-shipped (no token declared)

Loaded faces cover **400, 500, 600, 700, 800**. Two shipped weights fall outside that set.

| Value | Loaded? | Where | Declared at |
| --- | --- | --- | --- |
| `400` | yes | body default (inherited); **two** explicit declarations, both on Phân quyền — the resource-matrix role tag and the page-level explainer | `src/FE/src/app/platform/phan-quyen/components/resource-permission-matrix/resource-permission-matrix.scss`, `src/FE/src/app/platform/phan-quyen/pages/phan-quyen/phan-quyen.page.scss` |
| `500` | yes | **nothing uses it** — the face is loaded but never requested | — |
| `600` | yes | sidebar nav item, `.role-checkbox`, and the two DTI dialogs' "entering data for period X" line (`.form-target-period` / `.import-target`) | `src/FE/src/app/shared/components/sidebar/sidebar.scss`, `src/FE/src/app/platform/quan-tri-nguoi-dung/components/user-form-dialog/user-form-dialog.scss`, `src/FE/src/app/modules/danh-muc-dti/components/criteria-form-dialog/criteria-form-dialog.scss`, `src/FE/src/app/modules/danh-muc-dti/components/import-dialog/import-dialog.scss` |
| `700` | yes | the dominant emphasis weight — `.btn`, `.seg-btn`, `th`, form labels, `.filter-chip`, `.text-*`, links, `.user-name`, `.topbar-user-name`, active nav item | `src/FE/src/styles.scss` and component SCSS |
| `750` | **no** | `.badge` only | `src/FE/src/styles.scss` |
| `800` | yes | `.filter-count`, `.dialog-title`, brand marks, brand text, avatar, auth `h1` | `src/FE/src/styles.scss`, and component SCSS |
| `850` | **no** | `.delta` only | `src/FE/src/styles.scss` |

Be Vietnam Pro is shipped here as **static** faces, not a variable font, so `750` and `850` cannot be interpolated. Per the CSS font-matching algorithm both resolve upward to the nearest available heavier face — `800` in each case — which means `.badge` (750) and `.delta` (850) render at the same weight as everything already declared `800`. That is the shipped behaviour, recorded, not corrected.

### Font sizes bypassing the `--fs-*` scale

| Value | Selector | Declared at |
| --- | --- | --- |
| `10px` | `.badge` | `src/FE/src/styles.scss` |
| `10px` | `.filter-count` | `src/FE/src/styles.scss` |
| `10px` | `.filter-chip .icon-btn` | `src/FE/src/styles.scss` |
| `12px` | `.icon-btn` (glyph size) | `src/FE/src/styles.scss` |
| `12px` | `.sidebar-navparent .navchevron` | `src/FE/src/app/shared/components/sidebar/sidebar.scss` |
| `13px` | `.toolbar .input-icon > .pi` | `src/FE/src/styles.scss` |
| `15px` | `.input-icon > .pi` | `src/FE/src/styles.scss` |
| `15px` | `.sidebar-navitem .navicon` | `src/FE/src/app/shared/components/sidebar/sidebar.scss` |
| `17px` | `.dialog-icon` | `src/FE/src/styles.scss` |
| `18px` | `.login-brand h1` | `src/FE/src/app/shared/components/auth-card/auth-card.scss` |

`12px`, `13px` and `15px` are numerically identical to `--fs-sm`, `--fs-base` and `--fs-lg`; those declarations could reference the token today with no visual change. Most of them size an **icon glyph** rather than text, which is the likely reason they were written as literals — an icon is not on the text scale. `10px`, `17px` and `18px` have no equivalent on the scale.

### Line heights as-shipped (no token declared)

| Value | Where | Declared at |
| --- | --- | --- |
| `1` | `.icon-btn` | `src/FE/src/styles.scss` |
| `1.4` | `th,td` | `src/FE/src/styles.scss` |
| `1.45` | `.toast-text` | `src/FE/src/app/shared/components/toast/toast.scss` |
| `1.5` | `.dialog-desc` | `src/FE/src/styles.scss` |
| `1.5` | `.always-note` (permission matrix footnote) | `src/FE/src/app/platform/phan-quyen/components/resource-permission-matrix/resource-permission-matrix.scss` |
| `1.6` | `.import-summary` (the import-result dialog's summary sentence) | `src/FE/src/app/modules/danh-muc-dti/components/import-result-dialog/import-result-dialog.scss` |

### Responsive overrides

| Name | Value | Live variable / selector | Declared at |
| --- | --- | --- | --- |
| topbar-user-name (mobile ≤560px) | hidden (`display:none`) | `@media(max-width:560px){.topbar-user-name{…}}` | `src/FE/src/app/shared/components/topbar/topbar.scss` |

No `font-size` is overridden at any breakpoint. The previous revision listed a `kpi-value` mobile step-down (21px → 18px); the KPI tile no longer ships — see § Drift.

## Chart Palette

<!-- N/A for typography — no chart element carries a typography token. Tokens/colors.md is the owner
     of the chart roles.

     SUA 2026-09-06: the previous comment said colors.md "records that the app ships no chart". That
     was true between 2026-08-29 and 2026-09-05 only. Decision Q17 restored four chart roles to
     colors.md on 2026-09-05, so this file was pointing at a sentence that no longer exists. -->

## Drift — 2026-08-22 extraction → 2026-08-29 rewrite

| Item | Before | Now |
| --- | --- | --- |
| font-family-base | `Inter, 'Segoe UI', Arial, sans-serif` | `'Be Vietnam Pro', 'Segoe UI', Arial, sans-serif` |
| how the face is delivered | named only; nothing loaded it | self-hosted `@font-face` set declared inside `styles.scss` with absolute `/fonts/…` URLs. The intermediate state on the same day — a separate `public/fonts/be-vietnam-pro.css` linked from `index.html`, whose relative paths resolved one directory too deep — is gone; that file no longer exists |
| loaded weights | none | 400 / 500 / 600 / 700 / 800 |
| `--fs-xs` … `--fs-lg` | 11 / 12 / 13 / 14 / 15px | unchanged |
| shipped weights | 400, 600, 700, 750, 800, 850 | unchanged |

**Composite roles removed (the selector no longer exists):** `kpi-value`, `kpi-value-mobile`, `kpi-label`, `action-btn-label`. The dashboard KPI tile and the `.action-btn` class were dropped in the rewrite; `.btn.sm` is the surviving in-row action button.

**Composite roles added:** `button-sm-label`, `segmented-label`, `dialog-title`, `dialog-desc`, `filter-count`, `filter-chip`, `notice`, `text-emphasis`.

**Off-scale sizes removed:** `13.5px` (`.confirm-message`) and `21px` (`.kpi .value`) — both selectors are gone. `13.5px`, called out previously as an accidental sub-pixel size, no longer ships.

## Resolved — no longer open

1. ~~**The `@font-face` `src:` paths are one directory too deep** — `src/FE/public/fonts/be-vietnam-pro.css` asks for `fonts/<hash>.woff2` while sitting in `/fonts/`, so the request goes to `/fonts/fonts/…`.~~ — **fixed in the source 2026-08-29.** The separate font stylesheet was deleted, its `@font-face` blocks moved into `src/FE/src/styles.scss` § `@font-face`, and every `url()` is now the absolute `/fonts/<hash>.woff2`. See the verification note in § Live Source & Extraction Method for what was and was not checked.

## Normalize on redesign

1. **`750` and `850` are not loadable weights.** Both round up to `800` against the static faces that ship, so `.badge` and `.delta` are `800` in practice while the CSS claims otherwise. Either declare `800` directly, or ship the variable font that would make the intermediate steps real.
2. **`500` is loaded and never used.** Three `.woff2` files are downloaded for a weight no selector requests. Either adopt it (it is the natural step for `.sidebar-navitem`, currently `600`) or drop it from the subset build.
3. **No `--fw-*` scale.** The shipped weights are literals with no named steps. Count them rather than trusting a number: `grep -rhoE 'font-weight: *[0-9]+' src/FE/src --include=*.scss | sort -u` (the `@font-face` blocks declare the *loaded* set, so subtract those to get the *requested* set).
4. **No `--lh-*` scale.** Every line-height is a literal; list them with `grep -rhoE 'line-height: *[0-9.]+' src/FE/src --include=*.scss | sort -u`. Most are one-offs on a single selector.
5. **Eleven `font-size` literals bypass `--fs-*`.** Five are exact scale values and can be swapped with zero visual change. The icon-glyph sizes (`12`/`13`/`15`/`17px`) argue for a separate `--icon-*` scale rather than being forced onto the text scale.
6. **`.title h2` and `.logo h1` rely on the UA bold default** instead of declaring a weight, so the rendered weight depends on the user agent.

## Appendix: tokens.json rules

- Format: W3C DTCG — every token is an object with `$type` and `$value`.
- Top-level sets: `global` (theme-invariant) plus `light` and `dark` (theme overrides only). Typography tokens are theme-invariant (no dark-mode differences shipped) and live entirely in `global`.
- Figma import via Tokens Studio: enable `global` + exactly ONE theme set at a time — never both themes together.

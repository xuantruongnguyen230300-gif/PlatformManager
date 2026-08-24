---
project: "PlatformManager"
status: "draft"
updated: "2026-08-22"
version: "alpha"
name: "PlatformManager Design System"
description: "Tokens extracted from the shipped Angular 20 app (src/FE/src/styles.scss) — import into Stitch to generate screens matching the running product 1:1."
# The frontmatter IS the token dictionary — Google Stitch reads it directly.
# Every key below mirrors a live CSS custom property in src/FE/src/styles.scss :root,
# minus the '--' prefix. One name, one value, one place to check.
colors:
  primary: "#0f5bd7"                  # ALIAS of brand — required by the design.md schema.
                                      # Without a key literally named 'primary', Stitch
                                      # auto-generates its own key colors and ignores this
                                      # palette (lint rule 'missing-primary'). Same value as
                                      # --brand; keep the two in step.
  bg: "#eef2f8"                       # --bg  page background
  card: "#ffffff"                     # --card  every card/dialog/input surface (declared '#fff')
  surface-2: "#e1e7f1"                # --surface-2  hover tint, ghost/icon-only buttons
  tonal-bg: "#dbe7fa"                 # --tonal-bg  default .btn fill (pale brand tint)
  tonal-bg-hover: "#c7dbf5"           # .btn:hover  named 2026-08-22, literal in source
  tonal-ink: "#0f4a9e"                # --tonal-ink  text on tonal-bg
  text: "#152033"                     # --text
  muted: "#57647a"                    # --muted
  line: "#dfe6ef"                     # --line  faint hairline, non-interactive only
  border-strong: "#7e91b4"            # --border-strong  inputs/selects/tablewrap ONLY
  brand: "#0f5bd7"                    # --brand
  brand2: "#174ca8"                   # --brand2  .btn.primary:hover
  on-primary: "#ffffff"               # --on-primary  text/icon on brand
  good: "#0e7050"                     # --good
  good-bg: "#d9f2e6"                  # --good-bg
  warn: "#965e08"                     # --warn
  warn-bg: "#ffedc7"                  # --warn-bg
  bad: "#a02b2b"                      # --bad
  bad-bg: "#fbdcdc"                   # --bad-bg
  bad-bg-hover: "#f5c6c6"             # .btn.danger:hover  named 2026-08-22, literal in source
  bad-border: "#e5a8a8"               # .login-error border  named 2026-08-22, literal in source
  surface-track: "#edf1f6"            # --surface-track  progress-bar track
  surface-table-header: "#f8fafc"     # --surface-table-header  th + zebra stripe + chip
  text-table-header: "#536076"        # --text-table-header
  surface-notice: "#edf4ff"           # --surface-notice
  border-notice: "#cfe0ff"            # --border-notice
  surface-topbar: "rgba(255,255,255,0.95)"   # topbar.scss:5  literal + blur(10px)
  overlay-backdrop: "rgba(20,28,40,0.45)"    # dialog::backdrop + .sidebar-backdrop
  surface-nav-active: "rgba(15,91,215,0.08)" # .sidebar-navitem.active
  chart-series-1: "#0f5bd7"                  # trend-chart line + points (reads --brand)
  chart-series-1-fill: "rgba(15,91,215,0.12)" # trend-chart area fill
  chart-axis-label: "#57647a"                # trend-chart ticks (reads --muted)
  chart-grid: "#dfe6ef"                      # trend-chart y-grid (reads --line)
typography:
  body:
    fontFamily: "Inter, 'Segoe UI', Arial, sans-serif"
    fontSize: "13px"
    fontWeight: "400"
  h1-topbar:
    fontFamily: "Inter, 'Segoe UI', Arial, sans-serif"
    fontSize: "15px"
    fontWeight: "700"
  h1-auth:
    fontFamily: "Inter, 'Segoe UI', Arial, sans-serif"
    fontSize: "18px"
    fontWeight: "800"
  h2-title:
    fontFamily: "Inter, 'Segoe UI', Arial, sans-serif"
    fontSize: "14px"
    fontWeight: "700"
  button-label:
    fontFamily: "Inter, 'Segoe UI', Arial, sans-serif"
    fontSize: "12px"
    fontWeight: "700"
  button-block-label:
    fontFamily: "Inter, 'Segoe UI', Arial, sans-serif"
    fontSize: "14px"
    fontWeight: "700"
  action-btn-label:
    fontFamily: "Inter, 'Segoe UI', Arial, sans-serif"
    fontSize: "11px"
    fontWeight: "700"
  table-header:
    fontFamily: "Inter, 'Segoe UI', Arial, sans-serif"
    fontSize: "11px"
    fontWeight: "700"
  table-cell:
    fontFamily: "Inter, 'Segoe UI', Arial, sans-serif"
    fontSize: "12px"
    fontWeight: "400"
  badge:
    fontFamily: "Inter, 'Segoe UI', Arial, sans-serif"
    fontSize: "10px"
    fontWeight: "750"
  delta:
    fontFamily: "Inter, 'Segoe UI', Arial, sans-serif"
    fontSize: "12px"
    fontWeight: "850"
  kpi-value:
    fontFamily: "Inter, 'Segoe UI', Arial, sans-serif"
    fontSize: "21px"
    fontWeight: "850"
  kpi-label:
    fontFamily: "Inter, 'Segoe UI', Arial, sans-serif"
    fontSize: "11px"
    fontWeight: "400"
  form-label:
    fontFamily: "Inter, 'Segoe UI', Arial, sans-serif"
    fontSize: "12px"
    fontWeight: "700"
  muted-caption:
    fontFamily: "Inter, 'Segoe UI', Arial, sans-serif"
    fontSize: "11px"
    fontWeight: "400"
  sidebar-nav-item:
    fontFamily: "Inter, 'Segoe UI', Arial, sans-serif"
    fontSize: "12px"
    fontWeight: "600"
  sidebar-brand-text:
    fontFamily: "Inter, 'Segoe UI', Arial, sans-serif"
    fontSize: "14px"
    fontWeight: "800"
  toast-text:
    fontFamily: "Inter, 'Segoe UI', Arial, sans-serif"
    fontSize: "12px"
    fontWeight: "400"
  footer:
    fontFamily: "Inter, 'Segoe UI', Arial, sans-serif"
    fontSize: "11px"
    fontWeight: "400"
rounded:
  sm: "7px"           # --radius-sm  buttons, inputs, login-error
  md: "9px"           # --radius-md  sidebar nav item, toast
  lg: "16px"          # --radius-lg  card, login-card
  dialog: "15px"      # --radius-dialog
  table: "12px"       # --radius-table  tablewrap
  pill: "999px"       # --radius-pill  badge, progress bar
spacing:
  sp-1: "4px"                    # --sp-1
  sp-2: "6px"                    # --sp-2  button/cell vertical padding
  sp-3: "8px"                    # --sp-3  button/cell horizontal padding, filter gap
  sp-4: "10px"                   # --sp-4  title margin, field margin, kpi grid gap
  sp-5: "14px"                   # --sp-5  card/dialog/main padding, layout gap
  button-padding: "6px 8px"      # .btn  styles.scss:143
  button-block-padding: "11px"   # .btn-block  styles.scss:599
  cell-padding: "6px 8px"        # th,td  styles.scss:372
  input-padding: "6px 8px"       # .filters input/select  styles.scss:354
  auth-input-padding: "10px 12px 10px 36px"  # .field-input input  styles.scss:536
  notice-padding: "8px 14px"     # .notice  styles.scss:256
  badge-padding: "3px 6px"       # .badge  styles.scss:305
  card-padding: "14px"           # .card  styles.scss:132
  auth-card-padding: "32px 28px" # .login-card  auth-card.scss:17
components:
  # Component values interpolate tokens via {token.reference} — resolvable by Stitch only.
  page:
    backgroundColor: "{colors.bg}"
    textColor: "{colors.text}"
    typography: "{typography.body}"
  card:
    backgroundColor: "{colors.card}"
    textColor: "{colors.text}"
    rounded: "{rounded.lg}"
    padding: "{spacing.card-padding}"
  card-title:
    textColor: "{colors.text}"
    typography: "{typography.h2-title}"
  topbar:
    backgroundColor: "{colors.surface-topbar}"
    textColor: "{colors.text}"
    typography: "{typography.h1-topbar}"
    padding: "{spacing.sp-4}"
  sidebar:
    backgroundColor: "{colors.card}"
    textColor: "{colors.text}"
    typography: "{typography.sidebar-nav-item}"
    padding: "{spacing.sp-2}"
  sidebar-item-active:
    backgroundColor: "{colors.surface-nav-active}"
    textColor: "{colors.brand}"
    typography: "{typography.sidebar-nav-item}"
    rounded: "{rounded.md}"
    padding: "{spacing.sp-2}"
  sidebar-brand:
    backgroundColor: "{colors.brand}"
    textColor: "{colors.on-primary}"
    typography: "{typography.sidebar-brand-text}"
  button-tonal:
    backgroundColor: "{colors.tonal-bg}"
    textColor: "{colors.tonal-ink}"
    typography: "{typography.button-label}"
    rounded: "{rounded.sm}"
    padding: "{spacing.button-padding}"
  button-tonal-hover:
    backgroundColor: "{colors.tonal-bg-hover}"
    textColor: "{colors.tonal-ink}"
    typography: "{typography.button-label}"
    rounded: "{rounded.sm}"
    padding: "{spacing.button-padding}"
  button-primary:
    backgroundColor: "{colors.brand}"
    textColor: "{colors.on-primary}"
    typography: "{typography.button-label}"
    rounded: "{rounded.sm}"
    padding: "{spacing.button-padding}"
  button-primary-hover:
    backgroundColor: "{colors.brand2}"
    textColor: "{colors.on-primary}"
    typography: "{typography.button-label}"
    rounded: "{rounded.sm}"
    padding: "{spacing.button-padding}"
  button-danger:
    backgroundColor: "{colors.bad-bg}"
    textColor: "{colors.bad}"
    typography: "{typography.button-label}"
    rounded: "{rounded.sm}"
    padding: "{spacing.button-padding}"
  button-danger-hover:
    backgroundColor: "{colors.bad-bg-hover}"
    textColor: "{colors.bad}"
    typography: "{typography.button-label}"
    rounded: "{rounded.sm}"
    padding: "{spacing.button-padding}"
  button-block:
    backgroundColor: "{colors.brand}"
    textColor: "{colors.on-primary}"
    typography: "{typography.button-block-label}"
    rounded: "{rounded.sm}"
    padding: "{spacing.button-block-padding}"
  action-button:
    backgroundColor: "{colors.card}"
    textColor: "{colors.muted}"
    typography: "{typography.action-btn-label}"
    rounded: "{rounded.sm}"
    padding: "{spacing.sp-2}"
  action-button-hover:
    backgroundColor: "{colors.surface-2}"
    textColor: "{colors.text}"
    typography: "{typography.action-btn-label}"
    rounded: "{rounded.sm}"
    padding: "{spacing.sp-2}"
  input-field:
    backgroundColor: "{colors.card}"
    textColor: "{colors.text}"
    rounded: "{rounded.sm}"
    padding: "{spacing.input-padding}"
  auth-input-field:
    backgroundColor: "{colors.card}"
    textColor: "{colors.text}"
    rounded: "{rounded.sm}"
    padding: "{spacing.auth-input-padding}"
  form-label:
    textColor: "{colors.text}"
    typography: "{typography.form-label}"
  table-header:
    backgroundColor: "{colors.surface-table-header}"
    textColor: "{colors.text-table-header}"
    typography: "{typography.table-header}"
    padding: "{spacing.cell-padding}"
  table-cell:
    backgroundColor: "{colors.card}"
    textColor: "{colors.text}"
    typography: "{typography.table-cell}"
    padding: "{spacing.cell-padding}"
  table-row-zebra:
    backgroundColor: "{colors.surface-table-header}"
    textColor: "{colors.text}"
    typography: "{typography.table-cell}"
  table-row-hover:
    backgroundColor: "{colors.bg}"
    textColor: "{colors.text}"
    typography: "{typography.table-cell}"
  table-wrap:
    backgroundColor: "{colors.card}"
    rounded: "{rounded.table}"
  badge-success:
    backgroundColor: "{colors.good-bg}"
    textColor: "{colors.good}"
    typography: "{typography.badge}"
    rounded: "{rounded.pill}"
    padding: "{spacing.badge-padding}"
  badge-warning:
    backgroundColor: "{colors.warn-bg}"
    textColor: "{colors.warn}"
    typography: "{typography.badge}"
    rounded: "{rounded.pill}"
    padding: "{spacing.badge-padding}"
  badge-danger:
    backgroundColor: "{colors.bad-bg}"
    textColor: "{colors.bad}"
    typography: "{typography.badge}"
    rounded: "{rounded.pill}"
    padding: "{spacing.badge-padding}"
  role-tag:
    backgroundColor: "{colors.surface-table-header}"
    textColor: "{colors.text}"
    typography: "{typography.muted-caption}"
  kpi-tile:
    backgroundColor: "{colors.card}"
    textColor: "{colors.text}"
    typography: "{typography.kpi-value}"
    rounded: "{rounded.lg}"
    padding: "{spacing.card-padding}"
  kpi-label:
    textColor: "{colors.muted}"
    typography: "{typography.kpi-label}"
  delta-up:
    textColor: "{colors.good}"
    typography: "{typography.delta}"
  delta-down:
    textColor: "{colors.bad}"
    typography: "{typography.delta}"
  delta-flat:
    textColor: "{colors.muted}"
    typography: "{typography.delta}"
  progress-track:
    backgroundColor: "{colors.surface-track}"
    rounded: "{rounded.pill}"
  progress-fill:
    backgroundColor: "{colors.brand}"
    rounded: "{rounded.pill}"
  notice-banner:
    backgroundColor: "{colors.surface-notice}"
    textColor: "{colors.text}"
    typography: "{typography.table-cell}"
    rounded: "{rounded.table}"
    padding: "{spacing.notice-padding}"
  dialog:
    backgroundColor: "{colors.card}"
    textColor: "{colors.text}"
    rounded: "{rounded.dialog}"
    padding: "{spacing.sp-5}"
  dialog-backdrop:
    backgroundColor: "{colors.overlay-backdrop}"
  toast:
    backgroundColor: "{colors.card}"
    textColor: "{colors.text}"
    typography: "{typography.toast-text}"
    rounded: "{rounded.md}"
    padding: "{spacing.sp-3}"
  auth-card:
    backgroundColor: "{colors.card}"
    textColor: "{colors.text}"
    typography: "{typography.h1-auth}"
    rounded: "{rounded.lg}"
    padding: "{spacing.auth-card-padding}"
  auth-brand-mark:
    backgroundColor: "{colors.brand}"
    textColor: "{colors.on-primary}"
    typography: "{typography.h1-topbar}"
  login-error:
    backgroundColor: "{colors.bad-bg}"
    textColor: "{colors.bad}"
    typography: "{typography.table-cell}"
    rounded: "{rounded.sm}"
    padding: "{spacing.sp-3}"
  segmented-button-active:
    backgroundColor: "{colors.brand}"
    textColor: "{colors.on-primary}"
    typography: "{typography.table-cell}"
    rounded: "{rounded.sm}"
    padding: "{spacing.button-padding}"
  segmented-button-rest:
    backgroundColor: "{colors.card}"
    textColor: "{colors.muted}"
    typography: "{typography.table-cell}"
    rounded: "{rounded.sm}"
    padding: "{spacing.button-padding}"
  footer:
    textColor: "{colors.muted}"
    typography: "{typography.footer}"
  chart-line:
    backgroundColor: "{colors.chart-series-1-fill}"
    textColor: "{colors.chart-series-1}"
    typography: "{typography.muted-caption}"
  chart-axis:
    backgroundColor: "{colors.chart-grid}"
    textColor: "{colors.chart-axis-label}"
    typography: "{typography.muted-caption}"
---

> **Fidelity:** This file describes the app AS-SHIPPED — real extracted values, real copy, quirks included. Do not idealize. Proposed changes belong ONLY in the specs' "Normalize on redesign" sections.

## Overview

PlatformManager is an internal platform console whose flagship surface is **DTI Weekly** — weekly progress tracking against digital-transformation criteria. As of 2026-08-22 it ships as an **Angular 20** application (standalone components, signals, `@if`/`@for` control flow, no `NgModule`) in `src/FE/`, backed by a .NET solution in `src/BE/`. Six lazy-loaded routes are live: `/dashboard`, `/danh-muc/dti`, `/quan-tri/nguoi-dung`, `/quan-tri/phan-quyen`, `/dang-nhap`, `/doi-mat-khau`.

**Every token above was re-extracted on 2026-08-22 from `src/FE/src/styles.scss`** — the `:root` block at lines 19-78 holds all 24 named custom properties, and no custom property is declared anywhere else in the codebase (verified by exhaustive grep). Until this refresh the dictionary described the deleted prototype; per `doc/Design/CLAUDE.md` § Fidelity Policy the prototype is now **design-intent reference only** and had diverged materially — see the § Drift sections in `Tokens/colors.md` and `Tokens/spacing.md` for old → new deltas with source lines.

Token names mirror the live CSS custom property minus the `--` prefix. The prototype-era parallel vocabulary (`surface-badge-danger` for `--bad-bg`, `border-input` for `--border-strong`) is gone: it is what let the two sides drift unnoticed, and `styles.scss:45-48` shows the FE relying on the two vocabularies agreeing.

## Colors

A single flat light theme. `bg` (`#eef2f8`) is the page; `card` (`#ffffff`) is every card, dialog, input and table surface. Two text tiers (`text` / `muted`) and — importantly — **two** border tiers: `line` (`#dfe6ef`) is a faint hairline for cards and table rules, while `border-strong` (`#7e91b4`) is reserved for inputs and `.tablewrap` only. `styles.scss:21-27` records the reasoning: the app moved from "border-first" to "fill-first", so cards separate by shadow and secondary buttons by tonal fill, leaving a visible border only where it signals "you can type here".

That decision produced the tonal button family — `tonal-bg` / `tonal-ink` for the default `.btn`, with `surface-2` as the hover tint for ghost icon-only buttons. `brand` drives primary buttons, progress fill, the active nav item and the chart line; `brand2` is its hover shade (it is **no longer** the orphan token the prototype-era docs flagged — `.btn.primary:hover` consumes it at `styles.scss:155-156`).

`good` / `warn` / `bad` are the semantic triad, each paired with a pale `*-bg` surface for badges and tonal danger buttons. Three values reached this refresh as raw hex literals with `CÒN NỢ` comments asking design for names; they are now `tonal-bg-hover`, `bad-bg-hover` and `bad-border` (see § Do's and Don'ts and `Tokens/colors.md`).

The palette moved measurably on 2026-08-15 to fix contrast: `muted` `#6d788b`→`#57647a`, `good` `#14855b`→`#0e7050`, `warn` `#c07a00`→`#a8690a`, `bad` `#c83c3c`→`#b83232`, `bg` `#f3f6fb`→`#eef2f8`. `styles.scss:11-18` records the surface tiers had been measuring ~1.1:1–1.3:1, under WCAG 2.2's 3:1 for UI components.

**That pass did not go far enough.** On 2026-08-22 `designmd lint` measured the new `warn`/`bad` against the surfaces they actually sit on — `#a8690a` on `--warn-bg` = **3.88:1**, `#b83232` on `--bad-bg-hover` = **3.89:1** — both short of the 4.5:1 that 10px badge text needs (too small to qualify for the relaxed 3:1 large-text threshold). So the two moved a second time: `warn` `#a8690a`→**`#965e08`**, `bad` `#b83232`→**`#a02b2b`**. Current values are the frontmatter above. The fix landed in `styles.scss`, this file, `Tokens/colors.md`, `Tokens/tokens.json` **and `src/FE/src/app/core/theme/platform-manager-preset.ts`** — that last file re-declares 10 of these colours as TS constants for the PrimeNG ramps, and a palette change that skips it leaves CSS and the component library rendering different colours with nothing failing.

**No dark mode exists** — no `data-theme`, no `prefers-color-scheme`, no toggle; `trend-chart.ts:52-54` states the constraint in code. `Tokens/tokens.json` therefore holds this palette under `light` and leaves `dark` empty rather than inventing values.

`designmd lint` returns **0 errors and 6 warnings** on this frontmatter (verified 2026-08-22), in two categories — **neither is a real defect**:

1. **Contrast, false positive (2)** — `sidebar-item-active` and `chart-line` report exactly 1.00:1 because the linter reads their `backgroundColor` as an 8-digit hex (`#0f5bd714`) and compares the color against itself. Both are genuinely alpha-composited brand-over-surface in the app; the effective ratio is against `card`/`bg` underneath, not against the tint. Not a real finding.
2. **Orphaned tokens (4)** — `line`, `border-strong`, `bad-border` and `border-notice` are all *border* colors, and are heavily used in the shipped CSS. The design.md `components` schema simply has no border slot: its valid sub-tokens are `backgroundColor`, `textColor`, `typography`, `rounded`, `padding`, `size`, `height`, `width`. Adding `borderColor` was tested and returns a `broken-ref` warning instead, so the warning is unavoidable and is a schema limitation, not an unused token.

> **Was 8 warnings until 2026-08-22.** The other two were real: `badge-warning` (`warn` on `warn-bg`) measured **3.88:1** and `button-danger-hover` (`bad` on `bad-bg-hover`) **3.89:1**, both under WCAG AA's 4.5:1 for normal text. Because they were genuine accessibility debt rather than as-shipped quirks, they were fixed at the source (§ Colors) instead of being recorded — the Fidelity Policy preserves quirks, not defects.

## Typography

One stack — `Inter, 'Segoe UI', Arial, sans-serif`, quoted exactly as the CSS writes it — declared once on `body` (`styles.scss:91`) and inherited everywhere. ⚠️ **Inter is NOT loaded on the current working copy** (checked 2026-08-23: `src/FE/src/index.html` is 14 lines and contains no `@font-face`, `preconnect` or Google Fonts `<link>`). An earlier revision claimed it was added 2026-08-22 at `index.html:21-23` — that could not be verified. Until it is, the stack falls back to `Segoe UI`: a Google Fonts stylesheet at weights 400;500;600;700 with `display=swap`, preceded by `preconnect` to **both** `fonts.googleapis.com` and `fonts.gstatic.com` — the second one matters because the `.woff2` files come from `gstatic`, and preconnecting only to `googleapis` leaves the font fetch paying a fresh handshake.

Until that landed the stack was a promise the app could not keep: no `<link>`, no `@font-face`, so the first installed family won and a stock Windows box rendered Segoe UI while every spec said Inter. Note the app also uses weights **750, 800 and 850**, which are not among the four loaded — the browser synthesises or rounds them. Kept as-shipped rather than rounded to the loaded steps; see `Tokens/typography.md` § Normalize on redesign.

Unlike the prototype, the app **does** have a size scale: five `--fs-*` steps (11/12/13/14/15px) in `:root`, deliberately compact. `--fs-base` (13px) is set on `body`; `--fs-sm` (12px) is the workhorse across buttons, cells, inputs and labels. Nine `font-size` declarations still bypass the scale (`10px`, `13.5px`, `18px`, `21px` and four that are numerically identical to existing steps) — enumerated with source lines in `Tokens/typography.md`.

Weights and line-heights are **not** tokenised: six weights (400/600/700/750/800/850, including synthetic steps most static fonts will fake) and five line-heights ship as literals.

## Layout

**Two shells.** The main shell (`app.scss`, `app.html`) is a fixed left `Sidebar` (`--sidebar-w` 220px, `--sidebar-w-collapsed` 60px) with `.shell-content` offset by a matching `margin-left`, a sticky translucent `Topbar`, and a `main` capped at `--container-max-width` **1600px** (up from the prototype's 1450px) with `--sp-5` padding. The auth shell is the opposite: routes carrying `data: { noShell: true }` (`/dang-nhap`, `/doi-mat-khau`) render a bare `<router-outlet>` into a 100dvh centred `.login-shell` holding a 380px `.login-card`. `<app-toast />` overlays both.

Three breakpoints, used consistently across all 30 SCSS files: `max-width: 980px` (tablet — sidebar becomes an off-canvas drawer at `min(85vw,300px)`, grids collapse to one column), `max-width: 560px` (mobile), and a single `min-width: 981px` block for the collapsed-sidebar hover flyout. Four `@media print` blocks hide the sidebar, topbar and `.no-print` elements.

Grid templates, control sizes, the six-value z-index stack and motion durations are all tabulated with source lines in `Tokens/spacing.md`.

## Components

The shipped set, grounded in `src/FE/src/app/`. Full anatomy and 5-state tables live in `COMPONENTS.md`, re-run against `src/FE/` on 2026-08-22 and corrected 2026-08-23: **26 documented components plus 1 obsolete** (`Fab`, which no longer ships; `TabBar` was also removed 2026-08-23 — it documented a switcher that never shipped). The index is the composition gate — a screen spec may only compose what it lists.

`Sidebar` / `Topbar` / `Toast` (app shell, `shared/components/`) · `AuthCard` (`.login-shell` + `.login-card` + `.login-brand`) · `.card` and its `.kpi` tile variant · the `.btn` family (tonal default, `.primary`, `.danger`, `.btn-block`) · `.action-btn` and `.cell-icon-btn` (ghost, icon-only) · PrimeNG `p-table` with server-side paginators plus hand-rolled `<table>` matrices · form primitives (`.form-row`, `.field`, `.field-input`, `.role-checkboxes`) · `.filters` / `.search` · `.badge` (`.bdone`/`.bwork`/`.bstall`, plus `.active`/`.locked` on the user grid) · `.bar`/`.fill` progress · `.notice` · native `<dialog>` (default / `.form-dialog` / `.confirm-dialog`) · `.segmented`/`.seg-btn` · `TrendChart`.

Icons come from **two** sources, both catalogued in `Icons.md` (refreshed 2026-08-22): **PrimeIcons v7**, an icon font loaded globally via `angular.json` and authored as `<i class="pi pi-*">`; and **PrimeNG inline SVG**, injected at runtime by `p-paginator` and `p-table` (four paginator arrows and a loading spinner). The second set appears in no `src/FE/` source line, so a grep for `pi-` misses it entirely.

The FAB, the prototype's fixed-column 9-col table and the styled `.report` block were **not** ported.

## Chart Palette

The app ships **one** chart — `modules/dashboard/components/trend-chart/`, a PrimeNG `p-chart type="line"` (`primeng` 20.2 + `chart.js` 4.5). It has **no categorical palette**: a single dataset, legend hidden. Chart.js draws to a 2D canvas and cannot resolve `var()`, so `readCssVar()` (`trend-chart.ts:13-16`) resolves three tokens once via `getComputedStyle` and passes literals in:

| Chart role | Token | Value |
| --- | --- | --- |
| Series 1 line + points | `--brand` | `#0f5bd7` |
| Series 1 area fill | `--brand` @ 12% | `rgba(15,91,215,0.12)` |
| Axis tick labels (both axes) | `--muted` | `#57647a` |
| Y-axis grid | `--line` | `#dfe6ef` |
| X-axis grid, legend | — | not drawn |

As-shipped behaviour: y-axis pinned `[0,100]` with a `%` tick suffix, `pointRadius: 4`, `tension: 0` (straight segments), `spanGaps: false` over a series that keeps **every** period the API returns — a period with no value holds its slot on the x axis as `null`, so the line breaks at the hole rather than closing over it. Canvas fixed at 220px tall, named for assistive tech via `ariaLabel`. Empty state renders a `.muted` sentence instead of an empty chart. `trend-chart.ts:55-61` duplicates all three hex values as SSR fallbacks — a second uncontrolled copy of the palette, flagged in `Tokens/colors.md` § Normalize on redesign.

## Do's and Don'ts

- ✅ Use only the tokens above — every value traces to a `src/FE/` source line in `Tokens/*.md`.
- ✅ Respect the two border tiers: `line` for cards and table rules, `border-strong` for inputs and `.tablewrap` **only**. Reaching for `border-strong` on a button or card reverts the deliberate fill-first decision at `styles.scss:21-27`.
- ✅ Separate cards with `shadow`, not borders.
- ✅ Use the tonal button (`tonal-bg` / `tonal-ink`) for labelled secondary actions and the ghost `action-button` for icon-only ones — dense grids put two per row, so a permanent tonal fill would flood the table.
- ✅ Name new colors after their CSS custom property so `styles.scss` and this file cannot drift.
- ✅ Keep the runtime-computed badge triad (`bdone`/`bwork`/`bstall`) visually distinct from the DB `Status` field — different concepts (`spec/dashboard-dti-weekly/business-rules.md` §5).
- ❌ Don't invent colors, fonts, radii or spacing outside this frontmatter.
- ❌ Don't invent a dark theme — none ships; `tokens.json`'s `dark` set stays empty.
- ❌ Don't add a second chart series color — the one chart has one dataset and no categorical scale.
- ❌ Don't "fix" the contrast warnings, the un-tokenised weights, or the off-scale font sizes here — they are as-shipped facts. Fixes belong in the § Normalize on redesign lists, then in the live source first.

---

<!-- Lint before importing into Stitch:
       npx --yes --package=@google/design.md designmd lint <path-to-this-file>
     WARNING (Windows): the bare form `npx @google/design.md lint <path>` fails silently — always use --package.
     If lint rejects the house keys (project/status/updated), strip them from the exported copy only. -->

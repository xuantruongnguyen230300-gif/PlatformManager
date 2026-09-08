---
kind: luat
scope: du-an
verified: 2026-09-08
project: "PlatformManager"
status: "draft"
updated: "2026-08-29"
version: "alpha"
name: "PlatformManager Design System"
description: "Tokens extracted from the shipped Angular 20 app (src/FE/src/styles.scss) — import into Stitch to generate screens matching the running product 1:1."
# The frontmatter IS the token dictionary — Google Stitch reads it directly.
# Every COLOUR key below mirrors a live CSS custom property in src/FE/src/styles.scss
# :root, minus the '--' prefix — with no exceptions left: the last three alpha-composited
# colours were promoted into :root on 2026-09-03 (Tokens/colors.md § Resolved, item 2).
#
# CORRECTED 2026-09-08: that sentence used to be written about every key in the file, and
# it was false for the whole `<component>-padding` family under `spacing:` below. Those are
# NOT custom properties and never were — they are names this document invents so Stitch,
# which cannot interpolate {token.reference}, receives one literal shorthand string. They
# have no counterpart in Tokens/spacing.md or Tokens/tokens.json, which is a real ledger
# split, described in full at Tokens/spacing.md § "Tên ghép chỉ có ở DESIGN.md".
# Do not cite a `<component>-padding` key from a component spec as if it were a token:
# read the composition from the source instead (most are two real --sp-* tokens).
# Re-extracted 2026-08-29 after styles.scss was rewritten the same day; the three
# promoted keys re-checked against source 2026-09-08.
colors:
  primary: "#0f5bd7"                  # ALIAS of brand — required by the design.md schema.
                                      # Without a key literally named 'primary', Stitch
                                      # auto-generates its own key colors and ignores this
                                      # palette (lint rule 'missing-primary'). Same value as
                                      # --brand; keep the two in step.
  bg: "#cfdaea"                       # --bg  page background, toolbar fill, table row hover
  card: "#ffffff"                     # --card  every card/dialog/input surface (declared '#fff')
  surface-2: "#c1cde2"                # --surface-2  hover fill for the ghost .icon-btn
  tonal-bg: "#c4d8f6"                 # --tonal-bg  default .btn fill, .notice fill, .filter-chip
  tonal-ink: "#0f4a9e"                # --tonal-ink  text on tonal-bg, 5.82:1
  text: "#152033"                     # --text
  muted: "#4c576b"                    # --muted
  line: "#7a97bd"                     # --line  component boundary, 3.00:1 on card
  border-strong: "#6077a2"            # --border-strong  inputs/selects/tablewrap ONLY
  brand: "#0f5bd7"                    # --brand
  brand2: "#174ca8"                   # --brand2  .btn.primary:hover
  on-primary: "#ffffff"               # --on-primary  text/icon on brand
  btn-hover-bg: "#c7dbf5"             # --btn-hover-bg  .btn:hover fill (was named tonal-bg-hover)
  danger-border: "#e0a8a8"            # --danger-border  .btn.danger AND .login-error edge
                                      # (replaces bad-border-btn + bad-border-notice, which were
                                      # two reds one hex digit apart on the same role)
  danger-hover-bg: "#f5c6c6"          # --danger-hover-bg  .btn.danger:hover (was bad-bg-hover)
  th-ink: "#536076"                   # --th-ink  column-header text (was text-table-header)
  good: "#0e7050"                     # --good
  good-bg: "#d9f2e6"                  # --good-bg
  warn: "#965e08"                     # --warn
  warn-bg: "#ffedc7"                  # --warn-bg
  bad: "#a02b2b"                      # --bad
  bad-bg: "#fbdcdc"                   # --bad-bg
  surface-track: "#dbe4f0"            # --surface-track  progress track / disabled input fill
  surface-table-header: "#e9eff6"     # --surface-table-header  th + zebra stripe + neutral badge
  surface-topbar: "rgba(255,255,255,0.95)"   # --surface-topbar  .topbar background + blur(10px)
  overlay-backdrop: "rgba(20,28,40,0.45)"    # --overlay-backdrop  dialog::backdrop + .sidebar-backdrop
  surface-nav-active: "rgba(15,91,215,0.08)" # --surface-nav-active  .sidebar-navitem.active
typography:
  body:
    fontFamily: "'Be Vietnam Pro', 'Segoe UI', Arial, sans-serif"
    fontSize: "13px"
    fontWeight: "400"
  h1-topbar:
    fontFamily: "'Be Vietnam Pro', 'Segoe UI', Arial, sans-serif"
    fontSize: "15px"
    fontWeight: "700"
  h1-auth:
    fontFamily: "'Be Vietnam Pro', 'Segoe UI', Arial, sans-serif"
    fontSize: "18px"
    fontWeight: "800"
  h2-title:
    fontFamily: "'Be Vietnam Pro', 'Segoe UI', Arial, sans-serif"
    fontSize: "14px"
    fontWeight: "700"
  button-label:
    fontFamily: "'Be Vietnam Pro', 'Segoe UI', Arial, sans-serif"
    fontSize: "12px"
    fontWeight: "700"
  button-sm-label:
    fontFamily: "'Be Vietnam Pro', 'Segoe UI', Arial, sans-serif"
    fontSize: "11px"
    fontWeight: "700"
  button-block-label:
    fontFamily: "'Be Vietnam Pro', 'Segoe UI', Arial, sans-serif"
    fontSize: "14px"
    fontWeight: "700"
  segmented-label:
    fontFamily: "'Be Vietnam Pro', 'Segoe UI', Arial, sans-serif"
    fontSize: "12px"
    fontWeight: "700"
  table-header:
    fontFamily: "'Be Vietnam Pro', 'Segoe UI', Arial, sans-serif"
    fontSize: "11px"
    fontWeight: "700"
  table-cell:
    fontFamily: "'Be Vietnam Pro', 'Segoe UI', Arial, sans-serif"
    fontSize: "12px"
    fontWeight: "400"
  badge:
    fontFamily: "'Be Vietnam Pro', 'Segoe UI', Arial, sans-serif"
    fontSize: "10px"
    fontWeight: "750"
  delta:
    fontFamily: "'Be Vietnam Pro', 'Segoe UI', Arial, sans-serif"
    fontSize: "12px"
    fontWeight: "850"
  text-emphasis:
    fontFamily: "'Be Vietnam Pro', 'Segoe UI', Arial, sans-serif"
    fontSize: "12px"
    fontWeight: "700"
  form-label:
    fontFamily: "'Be Vietnam Pro', 'Segoe UI', Arial, sans-serif"
    fontSize: "12px"
    fontWeight: "700"
  dialog-title:
    fontFamily: "'Be Vietnam Pro', 'Segoe UI', Arial, sans-serif"
    fontSize: "15px"
    fontWeight: "800"
  dialog-desc:
    fontFamily: "'Be Vietnam Pro', 'Segoe UI', Arial, sans-serif"
    fontSize: "12px"
    fontWeight: "400"
  filter-count:
    fontFamily: "'Be Vietnam Pro', 'Segoe UI', Arial, sans-serif"
    fontSize: "10px"
    fontWeight: "800"
  filter-chip:
    fontFamily: "'Be Vietnam Pro', 'Segoe UI', Arial, sans-serif"
    fontSize: "11px"
    fontWeight: "700"
  notice:
    fontFamily: "'Be Vietnam Pro', 'Segoe UI', Arial, sans-serif"
    fontSize: "12px"
    fontWeight: "400"
  muted-caption:
    fontFamily: "'Be Vietnam Pro', 'Segoe UI', Arial, sans-serif"
    fontSize: "11px"
    fontWeight: "400"
  sidebar-nav-item:
    fontFamily: "'Be Vietnam Pro', 'Segoe UI', Arial, sans-serif"
    fontSize: "12px"
    fontWeight: "600"
  sidebar-brand-text:
    fontFamily: "'Be Vietnam Pro', 'Segoe UI', Arial, sans-serif"
    fontSize: "14px"
    fontWeight: "800"
  toast-text:
    fontFamily: "'Be Vietnam Pro', 'Segoe UI', Arial, sans-serif"
    fontSize: "12px"
    fontWeight: "400"
  footer:
    fontFamily: "'Be Vietnam Pro', 'Segoe UI', Arial, sans-serif"
    fontSize: "11px"
    fontWeight: "400"
rounded:
  sm: "7px"           # --radius-sm  buttons, icon buttons, segmented, inputs, login-error
  md: "9px"           # --radius-md  toolbar, notice, filter panel, sidebar nav item, toast
  lg: "16px"          # --radius-lg  card, login-card
  dialog: "15px"      # --radius-dialog
  table: "12px"       # --radius-table  tablewrap
  pill: "999px"       # --radius-pill  badge, filter count, filter chip, dialog icon
spacing:
  sp-1: "4px"                    # --sp-1
  sp-2: "6px"                    # --sp-2  button/cell vertical padding
  sp-3: "8px"                    # --sp-3  button/cell horizontal padding, toolbar gap
  sp-4: "10px"                   # --sp-4  title/field margin, toolbar horizontal padding
  sp-5: "14px"                   # --sp-5  card/dialog/filter-panel padding, dialog-actions margin
  grid-h-min: "220px"            # --grid-h-min  min height of a scrolling data grid.
                                 # Its sibling --grid-h is calc(100dvh - 280px) and is omitted
                                 # here because this dictionary holds literal values only —
                                 # see Tokens/spacing.md.
  button-padding: "6px 8px"      # .btn
  button-sm-padding: "4px 6px"   # .btn.sm  in-row action button
  button-block-padding: "11px"   # .btn-block
  cell-padding: "6px 8px"        # th,td
  input-padding: "6px 8px"       # .input and every field that shares its contract
  auth-input-padding: "10px 12px 10px 36px"  # .input-icon input  36px clears the leading icon
  toolbar-padding: "8px 10px"    # .toolbar
  notice-padding: "8px 14px"     # .notice
  badge-padding: "3px 6px"       # .badge
  filter-chip-padding: "2px 6px 2px 8px"  # .filter-chip
  card-padding: "14px"           # .card
  auth-card-padding: "32px 28px" # .login-card  auth-card.scss
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
    backgroundColor: "{colors.btn-hover-bg}"
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
    backgroundColor: "{colors.danger-hover-bg}"
    textColor: "{colors.bad}"
    typography: "{typography.button-label}"
    rounded: "{rounded.sm}"
    padding: "{spacing.button-padding}"
  button-sm:
    backgroundColor: "{colors.tonal-bg}"
    textColor: "{colors.tonal-ink}"
    typography: "{typography.button-sm-label}"
    rounded: "{rounded.sm}"
    padding: "{spacing.button-sm-padding}"
  button-block:
    backgroundColor: "{colors.brand}"
    textColor: "{colors.on-primary}"
    typography: "{typography.button-block-label}"
    rounded: "{rounded.sm}"
    padding: "{spacing.button-block-padding}"
  icon-button:
    backgroundColor: "{colors.card}"
    textColor: "{colors.muted}"
    typography: "{typography.table-cell}"
    rounded: "{rounded.sm}"
    padding: "{spacing.sp-1}"
  icon-button-hover:
    backgroundColor: "{colors.surface-2}"
    textColor: "{colors.text}"
    typography: "{typography.table-cell}"
    rounded: "{rounded.sm}"
    padding: "{spacing.sp-1}"
  icon-button-primary:
    backgroundColor: "{colors.tonal-bg}"
    textColor: "{colors.tonal-ink}"
    typography: "{typography.table-cell}"
    rounded: "{rounded.sm}"
    padding: "{spacing.sp-1}"
  icon-button-danger:
    backgroundColor: "{colors.bad-bg}"
    textColor: "{colors.bad}"
    typography: "{typography.table-cell}"
    rounded: "{rounded.sm}"
    padding: "{spacing.sp-1}"
  segmented-button-active:
    backgroundColor: "{colors.brand}"
    textColor: "{colors.on-primary}"
    typography: "{typography.segmented-label}"
    rounded: "{rounded.sm}"
    padding: "{spacing.button-padding}"
  segmented-button-rest:
    backgroundColor: "{colors.card}"
    textColor: "{colors.muted}"
    typography: "{typography.segmented-label}"
    rounded: "{rounded.sm}"
    padding: "{spacing.button-padding}"
  input-field:
    backgroundColor: "{colors.card}"
    textColor: "{colors.text}"
    rounded: "{rounded.sm}"
    padding: "{spacing.input-padding}"
  input-field-disabled:
    backgroundColor: "{colors.surface-track}"
    textColor: "{colors.muted}"
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
  toolbar:
    backgroundColor: "{colors.bg}"
    textColor: "{colors.text}"
    typography: "{typography.table-cell}"
    rounded: "{rounded.md}"
    padding: "{spacing.toolbar-padding}"
  filter-panel:
    backgroundColor: "{colors.card}"
    textColor: "{colors.text}"
    typography: "{typography.table-cell}"
    rounded: "{rounded.md}"
    padding: "{spacing.sp-5}"
  filter-count:
    backgroundColor: "{colors.brand}"
    textColor: "{colors.on-primary}"
    typography: "{typography.filter-count}"
    rounded: "{rounded.pill}"
  filter-chip:
    backgroundColor: "{colors.tonal-bg}"
    textColor: "{colors.tonal-ink}"
    typography: "{typography.filter-chip}"
    rounded: "{rounded.pill}"
    padding: "{spacing.filter-chip-padding}"
  table-header:
    backgroundColor: "{colors.surface-table-header}"
    textColor: "{colors.th-ink}"
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
  badge-neutral:
    backgroundColor: "{colors.surface-table-header}"
    textColor: "{colors.muted}"
    typography: "{typography.badge}"
    rounded: "{rounded.pill}"
    padding: "{spacing.badge-padding}"
  badge-outline:
    # The IDENTITY badge variant: role names, codes, classification labels. Fill is literally
    # `transparent` (no token) and the 1px `line` edge has no slot in the design.md component
    # schema — see the orphaned-token note under ## Colors. Replaced the retired `role-tag`
    # component on 2026-08-29; the class it described no longer exists in src/FE.
    textColor: "{colors.text}"
    typography: "{typography.badge}"
    rounded: "{rounded.sm}"
    padding: "{spacing.badge-padding}"
  delta-up:
    textColor: "{colors.good}"
    typography: "{typography.delta}"
  delta-down:
    textColor: "{colors.bad}"
    typography: "{typography.delta}"
  delta-flat:
    textColor: "{colors.muted}"
    typography: "{typography.delta}"
  text-emphasis-good:
    backgroundColor: "{colors.good-bg}"
    textColor: "{colors.good}"
    typography: "{typography.text-emphasis}"
    rounded: "{rounded.sm}"
  text-emphasis-warning:
    backgroundColor: "{colors.warn-bg}"
    textColor: "{colors.warn}"
    typography: "{typography.text-emphasis}"
    rounded: "{rounded.sm}"
  text-emphasis-danger:
    backgroundColor: "{colors.bad-bg}"
    textColor: "{colors.bad}"
    typography: "{typography.text-emphasis}"
    rounded: "{rounded.sm}"
  notice-info:
    backgroundColor: "{colors.tonal-bg}"
    textColor: "{colors.text}"
    typography: "{typography.notice}"
    rounded: "{rounded.md}"
    padding: "{spacing.notice-padding}"
  notice-success:
    backgroundColor: "{colors.good-bg}"
    textColor: "{colors.text}"
    typography: "{typography.notice}"
    rounded: "{rounded.md}"
    padding: "{spacing.notice-padding}"
  notice-warning:
    backgroundColor: "{colors.warn-bg}"
    textColor: "{colors.text}"
    typography: "{typography.notice}"
    rounded: "{rounded.md}"
    padding: "{spacing.notice-padding}"
  notice-danger:
    backgroundColor: "{colors.bad-bg}"
    textColor: "{colors.text}"
    typography: "{typography.notice}"
    rounded: "{rounded.md}"
    padding: "{spacing.notice-padding}"
  dialog:
    backgroundColor: "{colors.card}"
    textColor: "{colors.text}"
    rounded: "{rounded.dialog}"
    padding: "{spacing.sp-5}"
  dialog-backdrop:
    backgroundColor: "{colors.overlay-backdrop}"
  dialog-title:
    textColor: "{colors.text}"
    typography: "{typography.dialog-title}"
  dialog-desc:
    textColor: "{colors.muted}"
    typography: "{typography.dialog-desc}"
  dialog-icon-ask:
    backgroundColor: "{colors.tonal-bg}"
    textColor: "{colors.tonal-ink}"
    rounded: "{rounded.pill}"
  dialog-icon-danger:
    backgroundColor: "{colors.bad-bg}"
    textColor: "{colors.bad}"
    rounded: "{rounded.pill}"
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
  footer:
    textColor: "{colors.muted}"
    typography: "{typography.footer}"
---

> **Fidelity:** This file describes the app AS-SHIPPED — real extracted values, real copy, quirks included. Do not idealize. Proposed changes belong ONLY in the specs' "Normalize on redesign" sections.

## Overview

PlatformManager is an internal platform console. It ships as an **Angular 20** application (standalone components, signals, `@if`/`@for` control flow, no `NgModule`) in `src/FE/`, backed by a .NET solution in `src/BE/`.

**As of 2026-08-29 it ships Core screens only** — `/trang-chu`, `/quan-tri/nguoi-dung`, `/quan-tri/phan-quyen`, `/dang-nhap`, `/doi-mat-khau`. The `DtiWeekly` business module and its two routes (`/dashboard`, `/danh-muc/dti`) were removed that day to be rebuilt, and `/trang-chu` was added as the landing route and the target of every "go somewhere safe" redirect. Read the live list from `src/FE/src/app/app.routes.ts` and the census in `UiInventory.md` rather than from a list written here; an earlier revision of this paragraph said "six" and named two routes that no longer exist.

**Every token above was re-extracted on 2026-08-29 from `src/FE/src/styles.scss`**, which was rewritten the same day against the approved prototype at `doc/Design/Frontend/PlatformManager/Prototypes/index.html`, and updated again later that day for audit **FE-7**, which promoted four selector literals into `:root`. The `:root` block (`src/FE/src/styles.scss` § `:root`) holds every custom property in the codebase — colour/elevation, font sizes, spacing steps, radii, shell measurements and the two data-grid heights — and no custom property is declared anywhere else (`grep -rlE '^\s*--[a-z0-9-]+\s*:' src/FE/src --include=*.scss` returns only that file). Count them with the recipe in `Tokens/colors.md` § Live Source rather than from a number written here. Eight colours changed value, two dimension tokens are new and four colour tokens were renamed; the § Drift sections in `Tokens/colors.md`, `Tokens/typography.md` and `Tokens/spacing.md` carry the full old → new lists.

Token names mirror the live CSS custom property minus the `--` prefix. Four colour keys were renamed on 2026-08-29 to keep that rule true once audit FE-7 gave them real properties: `tonal-bg-hover` → `btn-hover-bg`, `bad-bg-hover` → `danger-hover-bg`, `text-table-header` → `th-ink`, and `bad-border-btn` + `bad-border-notice` collapsed into one `danger-border`. Nothing is left without a custom property behind it: the alpha-composited group (`surface-topbar`, `overlay-backdrop`, `surface-nav-active`) was promoted into `:root` on **2026-09-03** — `src/FE/src/styles.scss` § `--surface-topbar`, § `--overlay-backdrop`, § `--surface-nav-active` — and `Tokens/colors.md` records the promotion in § Resolved, item 2.

> 🔄 **SỬA 2026-09-08.** This sentence, the frontmatter header comment and the three inline comments beside those keys all still called them *"literals"* and pointed at `Tokens/colors.md` § **Shipped as literals**, a section that no longer exists — it was closed when the promotion landed. The **values** were never wrong; only the classification and the cross-reference were.

## Colors

A single flat light theme, recomputed on 2026-08-29 from the WCAG contrast formula rather than picked by eye. `bg` (`#cfdaea`) is the page; `card` (`#ffffff`) is every card, dialog, input and table surface. Two text tiers (`text` / `muted`) and **two** border tiers: `line` (`#7a97bd`) is the general component boundary — cards, the toolbar, table rules, the segmented control, the notice bar — while `border-strong` (`#6077a2`) is reserved for inputs and `.tablewrap`, where a border means "you can type here".

**What the 2026-08-29 pass changed and why.** Eight values moved, all of them surface or boundary colours; every text colour except `muted` stayed put because text already passed. The measurements are recorded in the stylesheet itself, in the palette comment that opens `:root` (`grep -n 'trông bẹt' src/FE/src/styles.scss`) and re-verified in `Tokens/colors.md` § Contrast, as measured:

| Token | Was | Now | Measured effect |
| --- | --- | --- | --- |
| `bg` | `#eef2f8` | `#cfdaea` | card vs page 1.12:1 → **1.41:1** |
| `line` | `#dfe6ef` | `#7a97bd` | component boundary 1.26:1 → **3.00:1** on card, clearing WCAG 2.2 SC 1.4.11 |
| `border-strong` | `#7e91b4` | `#6077a2` | **4.51:1**, one clearly darker step than the new `line` |
| `muted` | `#57647a` | `#4c576b` | would have fallen to 4.24:1 on the darker page; now **5.16:1** |
| `surface-2` | `#e1e7f1` | `#c1cde2` | icon-button hover fill **1.60:1** on card |
| `tonal-bg` | `#dbe7fa` | `#c4d8f6` | secondary-button fill **1.45:1** on card |
| `surface-track` | `#edf1f6` | `#dbe4f0` | fill vs track **4.68:1** |
| `surface-table-header` | `#f8fafc` | `#e9eff6` | zebra stripe 1.05:1 → **1.16:1** (was effectively invisible) |

The tonal button family survives the change: `tonal-bg` / `tonal-ink` for the default `.btn`, `surface-2` as the hover fill of the ghost `.icon-btn`. Because the darker `tonal-bg` still separates from `card` by only 1.45:1, the button's visible edge is now carried by a `line` border rather than by the fill — pushing the fill to 3:1 would have dropped the dark blue label below AA, so the boundary moved to where it costs nothing.

`good` / `warn` / `bad` are unchanged, each paired with a pale `*-bg` surface; all three clear AA against their own background for 10px badge text (5.15 / 4.66 / 5.69:1).

**Tokens that left `:root`, and the four that came back.** The rewrite pushed six properties out. Two are gone for good because their value has no consumer left — `--surface-notice` and `--border-notice`, since `.notice` now uses `tonal-bg` + `line`. The other four were promoted again the same day by audit FE-7 under names that match their job rather than their old call site: `--btn-hover-bg`, `--danger-hover-bg`, `--th-ink`, and `--danger-border`, which also re-merged the two reds (`#e0a8a8` / `#e5a8a8`) that the rewrite had split apart on one semantic role. `#e5a8a8` no longer appears in the stylesheet. Full before/after in `Tokens/colors.md` § Drift.

**No dark mode exists** — no `data-theme`, no `prefers-color-scheme`, no toggle. `Tokens/tokens.json` holds this palette under `light` and leaves `dark` empty rather than inventing values.

**Ten of these colours are re-declared in TypeScript** — as the `APP_PALETTE` constant in `src/FE/src/app/app.config.ts`, which `src/FE/src/app/core/theme/core-preset.ts` turns into the PrimeNG Aura ramps. They lived inside the preset file itself until 2026-09-02, when brand colours were moved out of `core/` as project-owned data; the ten values did not change. All ten were compared with `:root` on 2026-08-29 and match, including the four that moved. A palette change that skips that file leaves CSS and the component library rendering different colours with nothing failing — the stylesheet says so in the ⚠️ paragraph of its header comment (`grep -n APP_PALETTE src/FE/src/styles.scss`).

`designmd lint` returns **0 errors** on this frontmatter (re-verified 2026-08-29 after the FE-7 rename). Do not copy the warning count from here — re-run `npx --yes --package=@google/design.md designmd lint doc/Design/Frontend/PlatformManager/DESIGN.md` and read the `summary` block; the bare `npx @google/design.md lint` form fails silently on Windows. **None of the warnings is a real defect**, and the two categories are worth knowing before anyone tries to clear them:

1. **Contrast, false positive (1)** — `sidebar-item-active` reports exactly 1.00:1 because the linter reads its `backgroundColor` as an 8-digit hex (`#0f5bd714`) and compares brand against itself. In the app that tint is alpha-composited over `card`, so the effective ratio is against the surface underneath. The second such warning (`chart-line`) disappeared with the chart tokens.
2. **Orphaned tokens** — `line`, `border-strong` and `danger-border` are all *border* colours and are heavily used in the shipped CSS. The design.md `components` schema has no border slot: its valid sub-tokens are `backgroundColor`, `textColor`, `typography`, `rounded`, `padding`, `size`, `height`, `width`. Adding `borderColor` returns a `broken-ref` warning instead, so this is a schema limitation, not an unused token. `line` in particular is the token the 2026-08-29 pass spent the most effort on, and it is the edge that makes `badge-outline` readable — a component this dictionary therefore cannot express fully. Re-run the linter for the current count rather than trusting the number in this heading.

## Typography

One stack — `'Be Vietnam Pro', 'Segoe UI', Arial, sans-serif`, quoted exactly as the CSS writes it — declared once on `body` and inherited everywhere. It replaced `Inter` on 2026-08-29.

The face is **self-hosted**, and the `@font-face` blocks live in `src/FE/src/styles.scss` itself (§ `@font-face`) — weights 400/500/600/700/800 across the latin, latin-ext and vietnamese subsets, each pointing at an absolute `url(/fonts/<hash>.woff2)` that resolves onto `src/FE/public/fonts/`.

> ✅ **The broken font path is fixed — verified 2026-08-29.** An earlier revision of this paragraph recorded a real defect: `index.html` linked a separate `public/fonts/be-vietnam-pro.css` whose relative `src: url(fonts/…)` resolved one directory too deep. That stylesheet no longer exists and `index.html` links no stylesheet at all. The comment above that set (`grep -n 'tự phục vụ' src/FE/src/styles.scss`) records why: Angular rewrites `<link>` hrefs at build time, which is what mangled the path, while a bundler leaves an absolute `url(/…)` alone. Checked by path arithmetic against `angular.json` plus a directory listing, **not** against a running browser — see `Tokens/typography.md` for exactly what that claim covers.

The size scale is unchanged: five `--fs-*` steps (11/12/13/14/15px) in `:root`, deliberately compact. `--fs-base` (13px) is set on `body`; `--fs-sm` (12px) is the workhorse across buttons, cells, inputs and labels. Eleven `font-size` declarations bypass the scale — most of them sizing an icon glyph rather than text — and are enumerated with their selectors in `Tokens/typography.md`.

Weights and line-heights are **not** tokenised — both ship entirely as literals, enumerated with re-runnable recipes in `Tokens/typography.md` rather than counted here. `750` and `850` are **not among the loaded faces**, and because these are static faces rather than a variable font both resolve upward to `800` — so `.badge` and `.delta` render at the same weight as everything already declared `800`. Recorded as-shipped; see `Tokens/typography.md`.

## Layout

**Two shells.** The main shell (`app.scss`, `app.html`) is a fixed left `Sidebar` (`--sidebar-w` 220px, `--sidebar-w-collapsed` 60px) with `.shell-content` offset by a matching `margin-left`, a sticky translucent `Topbar`, and a `main` capped at `--container-max-width` **1600px** with `--sp-5` padding. The auth shell is the opposite: routes carrying `data: { noShell: true }` render a bare `<router-outlet>` into a 100dvh centred `.login-shell` holding a 380px `.login-card`. `<app-toast />` overlays both.

**New 2026-08-29 — data-grid height is a token, not a guess.** `--grid-h` (`calc(100dvh - 280px)`) and `--grid-h-min` (`220px`) size the scrolling region of a data grid, replacing three hand-picked heights (480 / 520 / 560px) that each table used to choose for itself. Read the caveat in `Tokens/spacing.md` before using them: inside a `.page-fill` page both are deliberately overridden and a flex chain decides the height instead, because the fixed chrome above and below a grid is not the same 280px on every page.

Three width breakpoints: `max-width: 980px` (tablet — sidebar becomes an off-canvas drawer at `min(85vw,300px)`), `max-width: 560px` (mobile — toolbar children stretch, `.form-grid` collapses to one column), and a single `min-width: 981px` block for the collapsed-sidebar hover flyout. `@media print` blocks hide the sidebar, the topbar and anything marked `.no-print`.

Grid templates, control sizes, the z-index stack and motion durations are all tabulated with sources in `Tokens/spacing.md`.

## Components

The shipped set, grounded in `src/FE/src/app/`. Full anatomy and 5-state tables live in `COMPONENTS.md`, which also carries a "Retired on 2026-08-29" table and the command for counting the specs. **The index is the composition gate** — a screen spec may only compose what it lists, so read it there rather than trusting the summary below.

App shell: `Sidebar` / `Topbar` / `Toast`; `AuthCard` (`.login-shell` + `.login-card` + `.login-brand`) is the second, shell-less frame the two auth routes use. Surfaces and layout: `Card`, `Table` (`.tablewrap`, global `th`/`td`, zebra), `Dialog` (native `<dialog>` in three width variants plus `.dialog-head` / `.dialog-actions`), `Footer`. Controls: the `.btn` family (tonal default, `.primary`, `.danger`, `.sm`, `.btn-block`), `IconButton` (`.icon-btn` ghost + `.primary` / `.danger`), `SegmentedControl`, `Check`, one `Input` contract with `.input-icon` and six width variants, `FormRow`, `AuthField`. Composed pieces: `Toolbar` (`<app-toolbar>` — bounded search, filter panel behind a counted control, removable chips, action group), `ConfirmDialog`, `DataTable` (the PrimeNG `p-table` mechanism), `Badge` (status `.ok`/`.warn`/`.bad`/`.neutral` and identity `.outline`), `NoticeBanner`, `Avatar`.

Icons come from **two** sources, both catalogued in `Icons.md` (re-censused 2026-08-29): **PrimeIcons v7**, an icon font loaded globally via `angular.json` and authored as `<i class="pi pi-*">`; and **PrimeNG inline SVG**, injected at runtime by the paginator and loading spinner of the app's one `p-table`. The second set appears in no `src/FE/` source line, so a grep for `pi-` misses it entirely.

Ten specs were retired on 2026-08-29 — some because the dashboard and DTI screens left, some because five separate icon-button variants and two rival chip primitives were consolidated into one definition each. `COMPONENTS.md` § Retired records where each one went.

## Chart Palette

**None — the app ships no chart.**

`find src/FE/src -iname '*trend*'` and `grep -rni chart src/FE/src` both return nothing (checked 2026-08-29). The `TrendChart` component that a previous revision of this file documented — a PrimeNG `p-chart type="line"` over `chart.js`, with `chart-series-1` / `chart-series-1-fill` / `chart-axis-label` / `chart-grid` in the frontmatter — no longer exists, and those four keys have been removed from the dictionary, from `Tokens/tokens.json` and from `Tokens/colors.md`.

`chart.js` is still listed in `src/FE/package.json`. It is an **unused dependency**: a package in a manifest is not evidence of a shipped chart, and must not be read as one. If a chart returns, extract its palette from the live component and add the keys back here first — never the other way round.

## Do's and Don'ts

- ✅ Use only the tokens above — every value traces to a `src/FE/` source line in `Tokens/*.md`.
- ✅ Respect the two border tiers: `line` for cards, toolbars and table rules, `border-strong` for inputs and `.tablewrap` **only**. `border-strong` on a button or a card destroys the one signal that says "you can type here".
- ✅ Separate cards with `shadow` **and** the 3:1 `line` border — after 2026-08-29 the boundary carries a measured contrast target, so removing it is a regression, not a style choice.
- ✅ Use the tonal button (`tonal-bg` / `tonal-ink`) for labelled secondary actions and the ghost `action-button` for icon-only ones — dense grids put two per row, so a permanent tonal fill would flood the table.
- ✅ Name new colors after their CSS custom property so `styles.scss` and this file cannot drift.
- 🗑️ ~~Keep the runtime-computed badge triad (`bdone`/`bwork`/`bstall`) visually distinct from the DB `Status` field~~ — **retired 2026-09-05.** Decision Q4 made `Status` a **user-chosen** field with four values (`Chưa thực hiện` · `Đang thực hiện` · `Cần bổ sung minh chứng` · `Hoàn thành`), so there is no runtime-computed triad left to keep distinct from it. One field, one semantic `Badge` set (`.ok`/`.warn`/`.bad`/`.neutral`) on both screens — see `spec/dashboard-dti/business-rules.md` and `Screens/01-dashboard.md`.
- ❌ Don't invent colors, fonts, radii or spacing outside this frontmatter.
- ❌ Don't invent a dark theme — none ships; `tokens.json`'s `dark` set stays empty.
- ❌ Don't add a chart palette — no chart ships. See § Chart Palette.
- ❌ Don't "fix" the un-tokenised weights, the off-scale font sizes, or the unresolved font URL here — they are as-shipped facts. Fixes belong in the § Normalize on redesign lists, and land in the live source before they land in a spec.

---

<!-- Lint before importing into Stitch:
       npx --yes --package=@google/design.md designmd lint <path-to-this-file>
     WARNING (Windows): the bare form `npx @google/design.md lint <path>` fails silently — always use --package.
     If lint rejects the house keys (project/status/updated), strip them from the exported copy only. -->

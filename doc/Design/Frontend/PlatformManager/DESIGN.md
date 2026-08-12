---
project: "PlatformManager"
status: "draft"
updated: "2026-08-11"
version: "alpha"
name: "PlatformManager Design System"
description: "Tokens extracted from the DTI Weekly dashboard prototype (doc/Prototype/dashboard.html) — import into Stitch to generate screens matching the shipped app 1:1."
colors:
  primary: "#0f5bd7"
  primary-alt: "#174ca8"
  surface-topbar: "rgba(255,255,255,.95)"
  on-primary: "#ffffff"
  bg: "#f3f6fb"
  surface: "#ffffff"
  text: "#152033"
  text-muted: "#6d788b"
  border: "#dfe6ef"
  success: "#14855b"
  warning: "#c07a00"
  danger: "#c83c3c"
  surface-notice: "#edf4ff"
  border-notice: "#cfe0ff"
  surface-track: "#edf1f6"
  surface-table-header: "#f8fafc"
  text-table-header: "#536076"
  border-input: "#cad4e1"
  surface-badge-success: "#e7f7f0"
  surface-badge-warning: "#fff3da"
  surface-badge-danger: "#fdecec"
  overlay-backdrop: "rgba(20,28,40,.45)"
  surface-report: "#f8fafc"
  border-report-dashed: "#cbd6e5"
typography:
  body:
    fontFamily: "Inter, Segoe UI, Arial, sans-serif"
    fontSize: "16px"
    fontWeight: "400"
  h1:
    fontFamily: "Inter, Segoe UI, Arial, sans-serif"
    fontSize: "18px"
    fontWeight: "700"
  h2:
    fontFamily: "Inter, Segoe UI, Arial, sans-serif"
    fontSize: "16px"
    fontWeight: "400"
  kpi-value:
    fontFamily: "Inter, Segoe UI, Arial, sans-serif"
    fontSize: "27px"
    fontWeight: "850"
  label:
    fontFamily: "Inter, Segoe UI, Arial, sans-serif"
    fontSize: "12px"
    fontWeight: "400"
  badge:
    fontFamily: "Inter, Segoe UI, Arial, sans-serif"
    fontSize: "11px"
    fontWeight: "750"
  table-cell:
    fontFamily: "Inter, Segoe UI, Arial, sans-serif"
    fontSize: "12.5px"
    fontWeight: "400"
  delta:
    fontFamily: "Inter, Segoe UI, Arial, sans-serif"
    fontSize: "12.5px"
    fontWeight: "850"
rounded:
  input: "8px"
  select: "9px"
  button: "10px"
  notice: "11px"
  table: "12px"
  card: "14px"
  dialog: "15px"
  pill: "999px"
spacing:
  2xs: "7px"           # table-cell input padding
  xs: "8px"             # actions/weekbar/filters/histrow gap
  sm: "9px 10px"         # filter/weekbar input,select padding
  sm-btn: "9px 12px"      # button padding
  sm-cell: "9px 8px"       # table th/td padding
  md: "10px"                # title/group-row gap
  md-notice: "11px 13px"     # notice padding
  lg: "12px"                  # grid gaps / section margin-top
  lg-notice-mb: "14px"         # notice margin-bottom
  lg-card: "15px"               # card padding
  xl: "16px"                     # topin/main padding
  fab-offset: "18px"              # fab right/bottom offset
components:
  page:
    backgroundColor: "{colors.bg}"
  button-primary:
    backgroundColor: "{colors.primary}"
    textColor: "{colors.on-primary}"
    typography: "{typography.body}"
    rounded: "{rounded.button}"
    padding: "{spacing.sm-btn}"
  button-secondary:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.text}"
    typography: "{typography.body}"
    rounded: "{rounded.button}"
    padding: "{spacing.sm-btn}"
  card:
    backgroundColor: "{colors.surface}"
    rounded: "{rounded.card}"
    padding: "{spacing.lg-card}"
  kpi-tile:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.text-muted}"
    typography: "{typography.kpi-value}"
    rounded: "{rounded.card}"
    padding: "{spacing.lg-card}"
  badge-success:
    backgroundColor: "{colors.surface-badge-success}"
    textColor: "{colors.success}"
    typography: "{typography.badge}"
    rounded: "{rounded.pill}"
  badge-warning:
    backgroundColor: "{colors.surface-badge-warning}"
    textColor: "{colors.warning}"
    typography: "{typography.badge}"
    rounded: "{rounded.pill}"
  badge-danger:
    backgroundColor: "{colors.surface-badge-danger}"
    textColor: "{colors.danger}"
    typography: "{typography.badge}"
    rounded: "{rounded.pill}"
  progress-track:
    backgroundColor: "{colors.surface-track}"
    rounded: "{rounded.pill}"
  progress-fill:
    backgroundColor: "{colors.primary}"
    rounded: "{rounded.pill}"
  input-field:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.text}"
    rounded: "{rounded.input}"
    padding: "{spacing.2xs}"
  table-header:
    backgroundColor: "{colors.surface-table-header}"
    textColor: "{colors.text-table-header}"
  dialog:
    backgroundColor: "{colors.surface}"
    rounded: "{rounded.dialog}"
  notice-banner:
    backgroundColor: "{colors.surface-notice}"
    textColor: "{colors.text}"
    rounded: "{rounded.notice}"
---

> **Fidelity:** This file describes the app AS-SHIPPED — real extracted values, real copy, quirks included. Do not idealize. Proposed changes belong ONLY in the specs' "Normalize on redesign" sections.

## Overview

PlatformManager's only shipped UI today is **DTI Weekly** — a single-page dashboard prototype (`doc/Prototype/dashboard.html`) for tracking weekly progress against 62 digital-transformation criteria. No framework, no build step: all CSS lives in one `<style>` block (tokens in `:root`, `dashboard.html:10-14`) and all behavior in two inline `<script>` blocks. Tokens above were extracted directly from that `:root` block plus every selector that consumes it — see `Tokens/colors.md`, `Tokens/typography.md`, `Tokens/spacing.md` for the full re-checkable, source-cited tables.

## Colors

The palette is a single flat light theme — `bg` (`#f3f6fb`) is the page background, `surface` (`#ffffff`) is every card/dialog/input background, `text`/`text-muted` are the two text tiers, and `border` (`#dfe6ef`) is the one hairline border color used everywhere (cards, tables, inputs, dividers). `primary` (`#0f5bd7`) drives the primary button, the progress-bar fill, and the trend-chart line. `success`/`warning`/`danger` are the semantic triad used for delta arrows and the three status badges (`bdone`/`bwork`/`bstall`), each paired with a matching pale `surface-badge-*` background. `primary-alt` (`#174ca8`) is declared in `:root` but **not consumed anywhere** in the shipped file — kept as-shipped, flagged in `UiInventory.md` § Normalize on Redesign. **No dark mode exists** — there is no theme toggle or `prefers-color-scheme` handling in the source; `Tokens/tokens.json` therefore keeps this palette under the `light` set only and leaves `dark` empty rather than inventing values.

`npx designmd lint` flags two categories of warning on this frontmatter, both as-shipped facts recorded here rather than fixed (per Fidelity Policy — this file never idealizes the live source): (1) **contrast ratio** — `text-muted` on `surface` (4.46:1) and all three badge text/background pairs (4.18:1 / 3.17:1 / 4.41:1) sit below WCAG AA's 4.5:1 for normal text, exactly as they render in `dashboard.html` today; (2) **orphaned tokens** — `border`, `border-notice`, `border-input`, `overlay-backdrop`, `surface-report`, `border-report-dashed` are genuinely used in the shipped CSS (as `border:`/`::backdrop` values, see `Tokens/colors.md`) but the design.md `components` sub-token set (`backgroundColor`/`textColor`/`typography`/`rounded`/`padding`/`size`/`height`/`width`) has no border-color slot to reference them through — a schema limitation, not an unused token.

## Typography

Single font stack — `Inter, Segoe UI, Arial, sans-serif` — declared once on `body` (`dashboard.html:16`) and inherited everywhere; no separate heading font, no webfont `<link>`. There is no named type-scale system: each selector sets `font-size`/`font-weight` directly (see `Tokens/typography.md` for the full per-selector table with source lines). The two mobile-only overrides (`h1-logo-mobile` 16px, `kpi-value-mobile` 22px) fire at `@media(max-width:560px)` (`dashboard.html:56`).

## Layout

One app shell only — no auth/login screen, no secondary layout. Structure top-to-bottom: sticky `.topbar` (max-width 1450px inner `.topin`) → `main` (same 1450px max-width) containing `.notice` banner, `.weekbar` period-selector card, `.kpis` 5-card grid, `.layout` 2-column grid (group-progress card + trend-chart card, ratio `1.15fr .85fr`), the 62-criteria table card, and the saved-periods history card → `.footer`. A `.fab` floating "Lưu tuần" button appears only below the `tablet` breakpoint (980px) as the mobile substitute for the topbar's primary save action. `dialog#reportDialog` is an in-page native `<dialog>` modal, not a separate route. Grid/measurement specifics (breakpoints, per-section `grid-template-columns`) are cited with source lines in `Tokens/spacing.md`.

## Components

Core set actually present in the markup (full spec with states lands in `COMPONENTS.md` at stage 4): `.btn` / `.btn.primary` buttons, `.card` (generic + `.kpi` variant), `.badge` (`.bdone`/`.bwork`/`.bstall` — a **runtime-computed** status distinct from the DB `Status` field, see `spec/dashboard-dti-weekly/business-rules.md` §5), `.bar`/`.fill` progress bar, `.progressInput`/`.noteInput` table inputs, `.tablewrap` sticky-header table, `dialog` modal, `.fab`. No component library, no Storybook — the markup itself is the anatomy source.

## Chart Palette

None — app has no charts.

## Do's and Don'ts

- ✅ Use only the palette/typography/spacing tokens above — every value traces to a `dashboard.html` source line in `Tokens/*.md`.
- ✅ Keep the badge system (`bdone`/`bwork`/`bstall`) visually separate from any future `Status` (4-value, DB) control — they are different concepts (see `spec/dashboard-dti-weekly/business-rules.md` §5, `ui-spec.md` §6.4).
- ❌ Don't invent colors, fonts, radii, or spacing not listed in the frontmatter or `Tokens/*.md`.
- ❌ Don't add controls for `Status`, `SelfScore`, `VerifiedScore`, `Owner`, `Deadline`, or `CriteriaEvidence` — out of scope for this slice (`spec/dashboard-dti-weekly/spec.md` § Quyết định đã chốt #2, #4; `ui-spec.md` §6.6).
- ❌ Don't invent a dark theme — none is shipped; `tokens.json`'s `dark` set stays empty.

---

<!-- Lint before importing into Stitch:
       npx --yes --package=@google/design.md designmd lint <path-to-this-file>
     WARNING (Windows): the bare form `npx @google/design.md lint <path>` fails silently — always use --package.
     If lint rejects the house keys (project/status/updated), strip them from the exported copy only. -->

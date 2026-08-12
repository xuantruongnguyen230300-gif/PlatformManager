---
project: "PlatformManager"
status: "draft"
updated: "2026-08-11"
source_paths: ["doc/Prototype/dashboard.html"]
screens_total: "1"
screens_captured: "1"
---

# UI Inventory — PlatformManager

> Stage-2 output and the pipeline's FIRST GATE: stages 3+ (Tokens, Components, Screens, Prompt Packs, Audit, Figma Export) refuse to run without this file complete. Record the app AS-SHIPPED — real routes, real copy, real assets, quirks included. Deviations belong ONLY in "Normalize on Redesign" below.
>
> Supplementary business/UI sources read alongside the live prototype: `spec/dashboard-dti-weekly/spec.md`, `spec/dashboard-dti-weekly/business-rules.md`, `spec/dashboard-dti-weekly/ui-spec.md` (flow name: **DTI Weekly dashboard**).

## Screen Census

<!-- One row per distinct screen/view/section reachable in the shipped app, grouped by flow. For a single-file prototype, "route" is a named section/anchor within the file. Copy source = where the visible text comes from (view literal, .resx, i18n JSON, …). Spec status: ⬜ pending | 🚧 draft | ✅ specced. -->

| Route | Live source file(s) | Layout | Copy source | Screenshot | Spec status |
|-------|---------------------|--------|-------------|------------|-------------|
| `#dashboard` (single-page app shell; contains `#reportDialog` as an in-page modal, not a separate route) | `doc/Prototype/dashboard.html` (lines 61–145 markup, 148–769 `DTI_ITEMS` data, 772–936 app logic) | `.topbar` (sticky) → `main` (`.notice`, `.weekbar.card`, `.kpis` 5-card grid, `.layout` 2-col grid [`#groups` + `canvas#trend`], criteria table card [`.filters` + `.tablewrap > table`], history card [`#history`]) → `.footer`; plus `.fab` (mobile-only floating button) and `dialog#reportDialog` (modal, triggered by "Báo cáo nhanh") | Hardcoded Vietnamese literals in HTML markup + JS template strings (`renderKPIs`, `renderGroups`, `renderTable`, `renderHistory`, `generateReport`) — no i18n layer | `Assets/Screenshots/dashboard/dashboard--desktop-1440.png` (+ tablet/mobile/history/report-dialog variants, see manifest) | ✅ `Screens/01-dashboard.md` |

## Brand Assets

<!-- Every logo / illustration / favicon the shipped UI actually loads. Copy each file as-is into Assets/Brand/ and cite where it came from — no re-exports or recolors. -->

| File in Assets/Brand/ | Live source path | Used in (view + size) | Notes |
|-----------------------|------------------|-----------------------|-------|
| — | — | — | **None yet** — `dashboard.html` has no `<img>`, no `<link rel="icon">`, no background-image/logo asset anywhere in the markup or CSS. The "logo" area (`.logo`) is text-only (`<h1>DTI Weekly</h1>` + subtitle), not an image. |

## Screenshot Manifest

<!-- One row per screenshot referenced by the census. Capture instructions must be reproducible by anyone: dev-server launch command + URL + viewport. Keep `screens_captured` in frontmatter in sync. -->

| Screenshot path | Status | Capture instructions |
|-----------------|--------|----------------------|
| `Assets/Screenshots/dashboard/dashboard--desktop-1440.png` | captured | Open `doc/Prototype/dashboard.html` directly via `file://` (no dev server) in a fresh browser profile (empty `localStorage`) @ 1440×1400. Shows the **first-load / no-previous-week** state. |
| `Assets/Screenshots/dashboard/dashboard--tablet-900.png` | captured | Same as above @ 900×1400 — exercises the `@media(max-width:980px)` breakpoint (2-col KPI grid, 1-col groups/trend layout, `.actions .desktop` hidden, `.fab` visible). |
| `Assets/Screenshots/dashboard/dashboard--mobile-390.png` | captured | Same as above @ 390×1400 — exercises the `@media(max-width:560px)` breakpoint (reduced padding/font-size, last KPI card full-width, `.weekbar` children `flex:1`). |
| `Assets/Screenshots/dashboard/dashboard--with-history--desktop-1440.png` | captured | Open `dashboard.html` @ 1440×1400, then in DevTools console seed one prior period before the page's own script runs (or via console before `init()` fires): `localStorage.setItem('dti_weekly_history_v2', JSON.stringify([{date:'<7-days-ago>', values: /* DTI_ITEMS.id → number, ~88% of each initialProgress */ {}, notes:{}}]))`, then reload. Shows the **has-previous-week** state: non-`—` delta KPIs, populated `canvas#trend`, `up`/`down`/`flat` badges, populated `#history`. |
| `Assets/Screenshots/dashboard/report-dialog--desktop-1440.png` | captured | Same seeded state as above @ 1440×1000, then click **"Báo cáo nhanh"** to open `dialog#reportDialog` via `showModal()`, capture with dialog + backdrop visible. |

## Normalize on Redesign (project-wide)

<!-- Numbered list of as-shipped quirks to fix in a future redesign — the ONLY place deviations from the shipped UI may be proposed. Screen-specific items live in the screen spec's own section. -->

1. No confirmation dialog before destructive actions — "Lưu tuần này" silently overwrites an existing period on the same date, and "Khôi phục" silently replaces all `history`/`draft` data; both only show a **post-hoc** `alert()`, never a pre-action confirm.
2. "Sao lưu"/"Khôi phục" (`.actions .desktop`) have **no mobile equivalent** — they vanish entirely below 980px with no menu fallback, unlike "Lưu tuần này" which gets a `.fab` substitute.
3. Progress % input (`.progressInput`) clamps out-of-range/non-numeric values to `[0,100]` **silently** — no inline validation message is ever shown to the user.
4. `--brand2:#174ca8` is declared in the `:root` token block but never referenced by any selector in the CSS or by any JS-generated inline style — an orphan token.
5. The 62-criteria table has no responsive breakpoint of its own (`min-width:1200px` at every viewport, scrolled horizontally via `.tablewrap{overflow:auto}`) — acceptable as shipped, flagged only as a candidate for a future adaptive/collapsible table treatment.

---
project: "<project-slug>"
status: "draft"
updated: "YYYY-MM-DD"
source_paths: []              # live UI source roots inventoried, e.g. ["doc/Prototype/dashboard.html"]
screens_total: "<n>"
screens_captured: "<n>"
---

# UI Inventory — <project>

> <!-- Stage-2 output and the pipeline's FIRST GATE: stages 3+ (Tokens, Components, Screens, Prompt Packs, Audit, Figma Export) refuse to run without this file complete. Record the app AS-SHIPPED — real routes, real copy, real assets, quirks included. Deviations belong ONLY in "Normalize on Redesign" below. -->

## Screen Census

<!-- One row per distinct screen/view/section reachable in the shipped app, grouped by flow. For a single-file prototype, "route" is a named section/anchor within the file. Copy source = where the visible text comes from (view literal, .resx, i18n JSON, …). Spec status: ⬜ pending | 🚧 draft | ✅ specced. -->

| Route | Live source file(s) | Layout | Copy source | Screenshot | Spec status |
|-------|---------------------|--------|-------------|------------|-------------|
| `#dashboard` | `doc/Prototype/dashboard.html` | dashboard shell (topbar + KPI grid) | hardcoded Vietnamese literals in HTML | `Assets/Screenshots/dashboard.png` | ⬜ |

## Brand Assets

<!-- Every logo / illustration / favicon the shipped UI actually loads. Copy each file as-is into Assets/Brand/ and cite where it came from — no re-exports or recolors. -->

| File in Assets/Brand/ | Live source path | Used in (view + size) | Notes |
|-----------------------|------------------|-----------------------|-------|
| | | | |

## Screenshot Manifest

<!-- One row per screenshot referenced by the census. Capture instructions must be reproducible by anyone: dev-server launch command + URL + viewport. Keep `screens_captured` in frontmatter in sync. -->

| Screenshot path | Status | Capture instructions |
|-----------------|--------|----------------------|
| `Assets/Screenshots/dashboard.png` | pending | open `doc/Prototype/dashboard.html` directly in a browser (no dev server needed) @ 1440x900 |

## Normalize on Redesign (project-wide)

<!-- Numbered list of as-shipped quirks to fix in a future redesign — the ONLY place deviations from the shipped UI may be proposed. Screen-specific items live in the screen spec's own section. -->

1. <!-- e.g. token adoption is inconsistent — several colors are hardcoded outside the :root block. -->

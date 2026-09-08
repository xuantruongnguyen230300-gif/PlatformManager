---
kind: luat
scope: core
verified: 2026-09-06
project: "<project-slug>"
status: "draft"
updated: "YYYY-MM-DD"
source_paths: []              # live UI source roots inventoried, e.g. ["src/FE/src/app"]
screens_total: "<n>"
screens_captured: "<n>"
---

# UI Inventory — <project>

> <!-- Stage-2 output and the pipeline's FIRST GATE: stages 3+ (Tokens, Components, Screens, Prompt Packs, Audit, Figma Export) refuse to run without this file complete. Record the app AS-SHIPPED — real routes, real copy, real assets, quirks included. Deviations belong ONLY in "Normalize on Redesign" below. -->

## Screen Census

<!-- One row per distinct screen/view/section reachable in the shipped app, grouped by flow. Copy source = where the visible text comes from (i18n keys, server-driven values, view literal). Spec status: ⬜ pending | 🚧 draft | ✅ specced. -->

<!-- ⚠️ Read "Copy source" against the app, not from memory. Writing "hardcoded literals in the
     template" for a project that has since grown an i18n runtime is a single wrong sentence that
     every screen spec and prompt pack downstream inherits — it happened here and was corrected
     2026-09-06. Check before filling the column:
       ls src/FE/public/i18n/            # translation bundles, if any
       bash scripts/fe-gate.sh           # G12 fails on a Vietnamese sentence in a template -->

| Route | Live source file(s) | Layout | Copy source | Screenshot | Spec status |
|-------|---------------------|--------|-------------|------------|-------------|
| `/<route>` | `src/FE/src/app/<layer>/<feature>/` | <shell + the blocks it wraps> | i18n keys under `<feature>.*`; server-driven values named explicitly | `Assets/Screenshots/<flow>/<view>--desktop-1440.png` | ⬜ |

## Brand Assets

<!-- Every logo / illustration / favicon the shipped UI actually loads. Copy each file as-is into Assets/Brand/ and cite where it came from — no re-exports or recolors. -->

| File in Assets/Brand/ | Live source path | Used in (view + size) | Notes |
|-----------------------|------------------|-----------------------|-------|
| | | | |

## Screenshot Manifest

<!-- One row per screenshot referenced by the census. Capture instructions must be reproducible by anyone: dev-server launch command + URL + viewport. Keep `screens_captured` in frontmatter in sync. -->

| Screenshot path | Status | Capture instructions |
|-----------------|--------|----------------------|
| `Assets/Screenshots/<flow>/<view>--desktop-1440.png` | pending | run both servers (see `doc/Design/CLAUDE.md` § Rules for the exact commands and ports), sign in, open `/<route>` @ 1440x900 |

## Normalize on Redesign (project-wide)

<!-- Numbered list of as-shipped quirks to fix in a future redesign — the ONLY place deviations from the shipped UI may be proposed. Screen-specific items live in the screen spec's own section. -->

1. <!-- e.g. token adoption is inconsistent — several colors are hardcoded outside the :root block. Count them: `bash scripts/fe-gate.sh` §G1/§G11. -->

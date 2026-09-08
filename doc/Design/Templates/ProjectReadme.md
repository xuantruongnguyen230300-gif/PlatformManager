---
kind: luat
scope: core
verified: 2026-09-06
project: "<project-slug>"
status: "draft"
updated: "YYYY-MM-DD"
title: "<Project Title>"
group: "Backend|Frontend|Shared"
stack: "<framework + component library + styling, e.g. Angular 20 (standalone + Signals, zoneless), PrimeNG + PrimeIcons v7, SCSS>"
source_paths: []              # live UI source roots, e.g. ["src/FE/src/app"]
current_stage: "<1-8>"
---

# Design — <Project Title> Design System

> <!-- One line: which shipped UI this documents and where its source lives. Specs record the app AS-SHIPPED (real copy, real assets, quirks); deviations belong ONLY in "Normalize on redesign" sections. -->

## Overview

<!-- 2-4 sentences: what the surface is, who uses it, and what this design system extraction covers. Mention the theme/design language of the live UI. -->

## Stack & Live Sources

<!-- Cite the exact files where the tokens/components/screens ship. These are the paths an inventory pass reads from, and the paths an agreed token value must be landed in — they are not where token values are decided (see Maintenance Rules below). -->

- **Stack**: <framework + component library + styling — mirror the `stack` frontmatter key>
- **Tokens source**: `<path/to/theme css, e.g. src/FE/src/styles.scss :root block>`
- **Views source**: `<path/to/views or components>`
- **Ignore**: <!-- legacy assets that are NOT part of the design system, if any -->

## Pipeline Status

<!-- Status: ⬜ not started | 🚧 in progress | ✅ done. Update after each stage; keep `current_stage` in frontmatter in sync. -->

| Stage | Skill command | Status |
|-------|---------------|--------|
| 1 Scaffold | `/design-new-project` | ⬜ |
| 2 UI Inventory | `/design-inventory-ui` | ⬜ |
| 3 Tokens | `/design-extract-tokens` | ⬜ |
| 4 Components | `/design-document-components` | ⬜ |
| 5 Screens | `/design-create-screens` | ⬜ |
| 6 Prompt Packs | `/design-generate-prompts` | ⬜ |
| 7 Audit | `/design-audit` | ⬜ |
| 8 Figma Export | `/design-export-figma` | ⬜ |

## Maintenance Rules

1. **Specs decide, code follows** — a token or theme change is agreed in `Tokens/*.md`, `Tokens/tokens.json` and the `DESIGN.md` frontmatter **first**; a value already sitting in code, or previewed in a prototype, is not yet a decision. The rule and its reasoning are held in one place — `doc/huong_dan/wiki-core/fe/04-design-token-system.md` § Chiều — do not restate them in the generated README. Extraction (`/design-inventory-ui`, `/design-extract-tokens`) still runs: it records what already ships, and bootstraps or re-syncs the specs, but it never decides a value.
2. **Then land it in code — in BOTH files** — apply the agreed values to the theme CSS `:root` **and** to the framework theme preset that re-declares part of the palette as constants (for an Angular + PrimeNG project: `src/FE/src/styles.scss` and the palette constant the preset builder consumes — here `APP_PALETTE` in `src/FE/src/app/app.config.ts`). Change one and not the other and the CSS and the component library render different colours with nothing failing.
3. **Then lint** — `npx --yes --package=@google/design.md designmd lint doc/Design/<Group>/<Project>/DESIGN.md` must pass with 0 errors (every `{token.reference}` resolves). The bare `npx @google/design.md lint` form fails silently on Windows — always use the `--package=…designmd` form.
4. **Cite sources** — every token, component, and screen spec cites its live source file (and line where useful) so dev handoff maps 1:1.

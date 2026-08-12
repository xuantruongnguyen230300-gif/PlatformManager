---
project: "<project-slug>"
status: "draft"
updated: "YYYY-MM-DD"
title: "<Project Title>"
group: "Backend|Frontend|Shared"
stack: "<e.g. Static HTML/CSS/JS prototype, or Angular + ng-zorro once chosen>"
source_paths: []              # live UI source roots, e.g. ["doc/Prototype/dashboard.html"]
current_stage: "<1-8>"
---

# Design — <Project Title> Design System

> <!-- One line: which shipped UI this documents and where its source lives. Specs record the app AS-SHIPPED (real copy, real assets, quirks); deviations belong ONLY in "Normalize on redesign" sections. -->

## Overview

<!-- 2-4 sentences: what the surface is, who uses it, and what this design system extraction covers. Mention the theme/design language of the live UI. -->

## Stack & Live Sources

<!-- The code is the source of truth. Cite the exact files tokens/components/screens are extracted from. -->

- **Stack**: <framework + theme, e.g. static HTML/CSS/JS prototype>
- **Tokens source**: `<path/to/theme css, e.g. doc/Prototype/dashboard.html :root block>`
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

1. **Live source first** — when the theme or UI changes, change the shipped code (theme CSS, views) before any spec; never edit spec values in isolation.
2. **Then mirror** — update `DESIGN.md` frontmatter, `Tokens/*.md`, and `Tokens/tokens.json` to match the live values.
3. **Then lint** — `npx --yes --package=@google/design.md designmd lint doc/Design/<Group>/<Project>/DESIGN.md` must pass with 0 errors (every `{token.reference}` resolves). The bare `npx @google/design.md lint` form fails silently on Windows — always use the `--package=…designmd` form.
4. **Cite sources** — every token, component, and screen spec cites its live source file (and line where useful) so dev handoff maps 1:1.

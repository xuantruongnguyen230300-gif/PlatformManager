---
project: "PlatformManager"
status: "draft"
updated: "2026-08-11"
title: "PlatformManager"
group: "Frontend"
stack: "Static HTML/CSS/JS prototype (pre-framework) — will migrate to whatever ships in src/FE/"
source_paths: ["doc/Prototype/dashboard.html"]
current_stage: "8-Figma Export (blocked — see Pipeline Status note)"
---

# Design — PlatformManager Design System

> Documents the DTI Weekly dashboard prototype at `doc/Prototype/dashboard.html`. Specs record the app AS-SHIPPED (real copy, real assets, quirks); deviations belong ONLY in "Normalize on redesign" sections.

## Overview

PlatformManager is a weekly progress-tracking dashboard ("DTI Weekly — Theo dõi tiến độ chuyển đổi số"). Today it exists only as a single self-contained HTML prototype (inline CSS + JS, no build step, no framework) — `src/FE/` and `src/BE/` are still empty. This design system extraction covers that prototype until a real frontend app lands.

## Stack & Live Sources

- **Stack**: static HTML/CSS/JS, no framework, no build step.
- **Tokens source**: the single `:root { ... }` block inside the `<style>` tag of `doc/Prototype/dashboard.html` (custom properties: `--bg`, `--card`, `--text`, `--muted`, `--line`, `--brand`, `--brand2`, `--good`, `--warn`, `--bad`, `--shadow`).
- **Views source**: `doc/Prototype/dashboard.html` (single file — sections toggled/rendered client-side, not separate routes).
- **Ignore**: none yet.

## Pipeline Status

| Stage | Skill command | Status |
|-------|---------------|--------|
| 1 Scaffold | `/design-new-project` | ✅ |
| 2 UI Inventory | `/design-inventory-ui` | ✅ |
| 3 Tokens | `/design-extract-tokens` | ✅ |
| 4 Components | `/design-document-components` | ✅ |
| 5 Screens | `/design-create-screens` | ✅ |
| 6 Prompt Packs | `/design-generate-prompts` | ✅ |
| 7 Audit | `/design-audit` | ✅ |
| 8 Figma Export | `/design-export-figma` | 🚫 superseded — see note |

> **Stage 8 superseded (2026-08-11):** the `figma` MCP tooling gap from earlier that day was fixed (installed the official `figma@claude-plugins-official` plugin), and a Figma file was partially built — but every Figma account tried (`se.dev@vnresource.vn` and `xuantruongnguyen230300@gmail.com`) hit the **Starter-plan MCP tool-call quota** (6 calls/month per Figma's own rate-limit docs) almost immediately. Rather than wait on a plan upgrade, the user chose to **drop the Figma export step** and instead extended `doc/Prototype/dashboard.html` directly with the sidebar menu (see `spec/sidebar-menu/ui-spec.md`) — that file is now the canonical visual reference, verified live via chrome-devtools-mcp (desktop/collapsed/mobile-drawer, no regressions to existing dashboard logic). Stages 1-7's artifacts (`Tokens/tokens.json`, `DESIGN.md`, `COMPONENTS.md`, `Screens/01-dashboard.md`, `Prompts/01-dashboard-prompts.md`) remain valid reference material for the eventual Angular build in `src/FE/` — only the Figma hand-off itself was dropped. If Figma access improves later (plan upgrade), `/design-export-figma PlatformManager` can still be run against this existing artifact set.

## Maintenance Rules

1. **Live source first** — when the theme or UI changes, change the shipped code (`doc/Prototype/dashboard.html`, or the future `src/FE/` app) before any spec; never edit spec values in isolation.
2. **Then mirror** — update `DESIGN.md` frontmatter, `Tokens/*.md`, and `Tokens/tokens.json` to match the live values.
3. **Then lint** — `npx --yes --package=@google/design.md designmd lint doc/Design/Frontend/PlatformManager/DESIGN.md` must pass with 0 errors (every `{token.reference}` resolves). The bare `npx @google/design.md lint` form fails silently on Windows — always use the `--package=…designmd` form.
4. **Cite sources** — every token, component, and screen spec cites its live source file (and line where useful) so dev handoff maps 1:1.
5. **Once `src/FE/` gets a real app**: update `source_paths` above to point there instead of (or in addition to) the static prototype, and re-run stage 2 (`/design-inventory-ui PlatformManager`) to refresh the census.

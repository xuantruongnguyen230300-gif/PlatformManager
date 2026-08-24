---
project: "PlatformManager"
status: "draft"
updated: "2026-08-22"
title: "PlatformManager"
group: "Frontend"
stack: "Angular 20 (standalone + Signals), PrimeNG + PrimeIcons v7, SCSS"
source_paths: ["src/FE/src/app", "src/FE/src/styles.scss"]
current_stage: "6-Prompt Packs complete (all 5 flows); 7-Audit next"
---

# Design — PlatformManager Design System

> Documents the **shipped Angular app** in `src/FE/`. Specs record the app AS-SHIPPED (real copy, real assets, quirks); deviations belong ONLY in "Normalize on redesign" sections.

## Overview

PlatformManager is an internal administration platform: a weekly digital-transformation progress dashboard plus the platform screens around it (sign-in, forced password change, user administration, permissions, DTI catalogue).

`src/FE/` is a complete Angular 20 application with **6 lazy-loaded routes** (`src/FE/src/app/app.routes.ts`), backed by the .NET solution in `src/BE/`. This design system is extracted from that app.

## Stack & Live Sources

- **Stack**: Angular 20 standalone + Signals, zoneless; PrimeNG components (`p-table`, `p-chart`) with a custom preset; PrimeIcons v7; SCSS.
- **Tokens source**: the `:root { ... }` block in **`src/FE/src/styles.scss`** — the `--sp-*`, `--fs-*` and `--radius-*` scales plus semantic colours (`--bg`, `--card`, `--surface-2`, `--text`, `--muted`, `--line`, `--border-strong`, `--brand`, `--good`/`--warn`/`--bad` and their `-bg` pairs, `--tonal-bg`, `--tonal-ink`, `--sidebar-w`, `--container-max-width`).
  ⚠️ `src/FE/src/app/core/theme/platform-manager-preset.ts` **re-declares 10 of these colours as TS constants** for the PrimeNG ramps. A value or name change must land in **both** files, or CSS and the component library render different colours with nothing failing.
- **Views source**: the 6 routes in `src/FE/src/app/app.routes.ts`. In-page `<dialog>` overlays and tab switches are **not** separate routes — they are documented inside the owning route's screen spec.
- **Ignore**: `src/FE/dist/`, `src/FE/node_modules/`.

## Pipeline Status

| Stage | Skill command | Status |
|-------|---------------|--------|
| 1 Scaffold | `/design-new-project` | ✅ |
| 2 UI Inventory | `/design-inventory-ui` | ✅ re-run 2026-08-22 against `src/FE` — 6 routes, 6 screenshots captured |
| 3 Tokens | `/design-extract-tokens` | ✅ re-run 2026-08-22 — lint 0 errors; also fixed 2 real WCAG AA failures |
| 4 Components | `/design-document-components` | ✅ complete 2026-08-22, corrected 2026-08-23 — **26 documented + 1 obsolete** (`TabBar` deleted 2026-08-23, documented a switcher that never shipped — see `COMPONENTS.md`). Pass C added `TrendChart`, `SegmentedControl`, `Footer`; the `STILL UNWRITTEN` list in `COMPONENTS.md` is empty |
| 5 Screens | `/design-create-screens` | ✅ all 6 screens, all sourced from `src/FE` |
| 6 Prompt Packs | `/design-generate-prompts` | ✅ complete 2026-08-22 — **all 5 flows** (`01`–`05`). `01-dashboard` was rewritten from scratch: the prototype-era pack described editable inputs, a FAB and a `Sao lưu`/`Khôi phục` pair that no longer ship. `05-auth` is new. Every value is a literal — verified 0 unresolved `{token.ref}` and 0 `var(--…)` in all five |
| 7 Audit | `/design-audit` | 🔁 **Ran 2026-08-22 → BLOCKED with 11 findings; all 11 fixed the same day.** Every one was documentation drift — none needed a `src/` change. `AUDIT.md` keeps the original verdict plus a resolution log. **Re-run `/design-audit PlatformManager` to issue a fresh verdict** |
| 8 Figma Export | `/design-export-figma` | 🚫 superseded — see note |

> **Stage 8 superseded (2026-08-11, still true):** every Figma account tried hit the **Starter-plan MCP tool-call quota** (6 calls/month) almost immediately. Rather than wait on a plan upgrade, the Figma hand-off was dropped. The artifact set here remains the hand-off. If Figma access improves, `/design-export-figma PlatformManager` can be run against it — but note `Tokens/tokens.json` changed substantially on 2026-08-22, so any previously-imported variables would need overwriting.

## Screens

| Route | Spec | Screenshot |
|-------|------|------------|
| `/dashboard` | `Screens/01-dashboard.md` | `Assets/Screenshots/dashboard/` |
| `/danh-muc/dti` | `Screens/02-danh-muc-dti.md` | `Assets/Screenshots/danh-muc-dti/` |
| `/quan-tri/nguoi-dung` | `Screens/03-quan-tri-nguoi-dung.md` | `Assets/Screenshots/quan-tri-nguoi-dung/` |
| `/quan-tri/phan-quyen` | `Screens/04-phan-quyen.md` | `Assets/Screenshots/phan-quyen/` |
| `/dang-nhap` + `/doi-mat-khau` | `Screens/05-auth.md` | `Assets/Screenshots/auth/` |

Policy: **one desktop screenshot per screen.** State and viewport variants are captured on demand — a pending backlog nobody works through hides which screens have *no* visual reference at all, which is the thing that matters.

## Maintenance Rules

1. **Live source first** — when the theme or UI changes, change `src/FE/` before any spec; never edit spec values in isolation.
2. **Then mirror** — update `DESIGN.md` frontmatter, `Tokens/*.md`, `Tokens/tokens.json`, **and `platform-manager-preset.ts`** to match.
3. **Then lint** — `npx --yes --package=@google/design.md designmd lint doc/Design/Frontend/PlatformManager/DESIGN.md` must pass with 0 errors. The bare `npx @google/design.md lint` form **fails silently on Windows** — always use the `--package=…designmd` form.
4. **Cite sources** — every token, component and screen spec cites its live source file (and line where useful) so dev handoff maps 1:1. Line numbers drift whenever `styles.scss` changes; re-verify rather than trusting an old citation.
5. **The `COMPONENTS.md` index is the gate** — a screen spec may only compose components listed there. Adding a `Components/*.md` without an index row does not make it composable.
6. **Never record credentials** in any design artifact, including screenshot capture instructions.

## Capturing screenshots

Both servers must be running:

```bash
dotnet run --project src/BE/PlatformManager.Api --urls http://localhost:5027
cd src/FE && npx ng serve --port 4202
```

`4200`–`4203` are in the backend's Development CORS allowlist, so any of them works without a config change. A new database needs `bash scripts/setup-database.sh` first — it applies the schema and checks that the bootstrap passwords are configured.

# CLAUDE.md — Design

Guidance for Claude Code working in `doc/Design/`, PlatformManager's Product Design home. See [README.md](./README.md) for the full UI project index and the AI → Figma → handoff workflow.

## Scope

- **This file governs**: `doc/Design/` and everything beneath it. It is self-contained — design work needs no sibling area's docs beyond the read-only lookups below.
- **In scope**: all specs, tokens, components, and screens under `doc/Design/<Backend|Frontend>/<Project>/` and `doc/Design/Shared/`.
- **Out of scope — do not modify**: `src/BE/`, `src/FE/`, `doc/Prototype/`, `doc/ERD/`. Exception: a token-update task starts in the live source (Core Principle 4) — then edits stay confined to the token values the task names.
- **When to cross (read-only, expected)**: reading live source in `src/FE/`, `src/BE/`, or `doc/Prototype/` to extract real CSS/token values (the code is the source of truth).

## 🎯 Core Principles (Read First)

### 1. Think Before Documenting
Don't assume specs. Don't hide confusion. Surface tradeoffs.
- State assumptions explicitly (e.g. which token maps to which live CSS value). If uncertain, ask.
- If multiple interpretations exist (e.g. is this a new component or a variant of an existing one), present them — don't pick silently.
- If reusing an existing component/token is simpler than adding a new one, say so.
- If something is unclear (no source file to cite, conflicting tokens), stop and ask.

### 2. Simplicity First
Minimum documentation that captures the real, live design. Nothing speculative.
- No components, screens, or states beyond what exists in the live app or was explicitly asked for.
- No invented tokens, colors, or spacing not derived from source — see `Rules`.
- No speculative "future" variants that aren't in the source code/UI yet.
- If a spec grows more elaborate than the thing it documents, trim it back.

### 3. Surgical Changes
Touch only the doc you must. Clean up only your own mess.
- Don't reorganize or reformat unrelated specs, components, or screens.
- Match the existing folder convention and markdown structure exactly, even where you'd choose differently.
- If you notice a stale or incorrect spec unrelated to your task, mention it — don't silently rewrite it.
- Remove only the parts of a spec that your own change made obsolete.

### 4. Goal-Driven Execution
Define success criteria against the live source. Verify before finishing.
- "Document component X" → cite its source file, confirm every token reference resolves, match states to the live UI.
- "Update tokens" → update the live source first, then mirror into `DESIGN.md` frontmatter and `Tokens/*`, and lint `DESIGN.md`.
- "Add a screen spec" → verify it composes only from components already listed in `COMPONENTS.md`.

## Fidelity Policy

Specs describe the app **as-shipped**: real copy, real logo usage, real quirks — so AI-generated screens resemble the existing product. Anything you would prefer different (mixed styles, inconsistent spacing, layout warts) is recorded **only** under a clearly-marked **"Normalize on redesign"** section — never silently idealized into the spec itself. Generators are told to reproduce the shipped UI exactly; redesigns consume the Normalize lists deliberately.

**Greenfield carve-out**: PlatformManager has no shipped frontend/backend app yet (`src/FE/` and `src/BE/` are empty) — the only shipped UI today is the static prototype `doc/Prototype/dashboard.html`. Until a real app lands, that prototype IS the live source for the Fidelity Policy. If a screen is designed net-new (no prototype coverage at all), build it from the brief via the Figma MCP instead of reverse-engineering nonexistent code — the one carve-out from as-shipped fidelity — then write a `Screens/*.md` spec documenting the result.

## Groups

- **`Backend/`** — private UIs embedded directly in a backend service under `src/BE/` once one exists.
- **`Frontend/`** — the public app under `src/FE/` (or, until that exists, the static prototype in `doc/Prototype/`).
- **`Shared/`** — cross-app brand material (`Assets/`, `UserFlows/`) that isn't scoped to a single project.

## Per-project folder convention

Every project folder (e.g. `Frontend/PlatformManager/`) follows. Canonical templates for every artifact live in [Templates/](./Templates/) — start from them, keep their frontmatter keys and section order exactly:

- `README.md` — project guide: stack, source paths, maintenance rules + the 8-stage pipeline status dashboard
- `UiInventory.md` — ground-truth census of the live app (routes, views, copy sources, brand-asset manifest, screenshot manifest) — the pipeline's first gate
- `DESIGN.md` — design.md format (YAML token frontmatter + guidance), importable into Google Stitch; opens with the fidelity blockquote
- `COMPONENTS.md` + `Components/` — component index and one spec per component (Sources cited, 5-state tables)
- `Tokens/` — colors, typography, spacing + `tokens.json` (W3C DTCG: `global` + `light` + `dark` sets, for Figma Tokens Studio); colors.md carries the chart palette (or an explicit "None — app has no charts")
- `Icons.md` — icon system + per-action map + declared legacy exceptions
- `Screens/NN-flow.md` — screen specs grouped by flow; every screen carries the 7 mandatory sections: Layout Blueprint · Copy · States · Responsive · Iconography · Screenshots · Normalize on redesign
- `Assets/Brand/` — real image files copied from the live app (original filenames kept); `Assets/Screenshots/<flow-stem>/<view>[--state][--viewport].png` (default = desktop-1440)
- `Prompts/<NN-flow>-prompts.md` — multi-tool prompt packs (Google Stitch, Claude Design, Google AI Studio, generic)
- `Exports/ExportLog.md` — append-only Figma export proof · `AUDIT.md` — latest audit verdict (PASS/BLOCKED)
- Created on demand: `Wireframes/`, `Mockups/`, `Prototypes/`, `UserFlows/`

## Pipeline & Skills

Design work moves through eight stages, one skill each — run in order; gates refuse when a prerequisite is missing:

| # | Stage | Skill | Gate | Primary output |
|---|-------|-------|------|----------------|
| 1 | Scaffold | `/design-new-project <Group>/<Project>` | refuses to overwrite | folder tree + README dashboard + UiInventory stub |
| 2 | UI Inventory | `/design-inventory-ui <project>` | project scaffolded | UiInventory.md census + Assets/Brand/ + screenshots (or pending) |
| 3 | Tokens | `/design-extract-tokens <project>` | UiInventory exists | Tokens/*, tokens.json, DESIGN.md (lint: 0 errors) |
| 4 | Components | `/design-document-components <project>` | census populated | COMPONENTS.md + Components/*.md |
| 5 | Screens | `/design-create-screens <project> [flow]` | components + tokens exist | Screens/NN-flow.md (7 mandatory sections) |
| 6 | Prompt Packs | `/design-generate-prompts <project> <flow>` | flow spec complete | Prompts/<flow>-prompts.md (4 tool sections) |
| 7 | Audit | `/design-audit <project>` | — | AUDIT.md PASS/BLOCKED + fix commands |
| 8 | Figma Export | `/design-export-figma <project>` | audit PASS + lint clean | Figma file + ExportLog entry |

## Rules

- English only for all documentation.
- Never hard-code colors/sizes in specs — reference tokens.
- Extend an existing component spec instead of inventing a new one.
- Never copy real secrets, API keys, or credentials from source files into any design artifact, prompt pack, or content pushed to Figma/Stitch.
- Single-app material stays in its project folder; only genuinely cross-app material goes in `Shared/`.
- Screenshots: capture via the chrome-devtools MCP whenever the target (dev server, or a static file opened directly) is reachable; otherwise record `pending` rows with exact capture instructions in the UiInventory Screenshot Manifest — never block on screenshots.
- Prompt packs resolve tokens to **literal values** (hex/px/font) — external tools cannot interpolate `{token.reference}`.
- Lint DESIGN.md with `npx --yes --package=@google/design.md designmd lint <path>` — the bare `npx @google/design.md lint` form **fails silently on Windows**. Gate = 0 errors; warnings are recorded as as-shipped facts, not blockers.

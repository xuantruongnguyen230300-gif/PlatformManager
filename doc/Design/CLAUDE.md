# CLAUDE.md — Design

Guidance for Claude Code working in `doc/Design/`, PlatformManager's Product Design home. See [README.md](./README.md) for the full UI project index and the AI → Figma → handoff workflow.

## Scope

- **This file governs**: `doc/Design/` and everything beneath it. It is self-contained — design work needs no sibling area's docs beyond the read-only lookups below.
- **In scope**: all specs, tokens, components, and screens under `doc/Design/<Backend|Frontend>/<Project>/` and `doc/Design/Shared/`.
- **Out of scope — do not modify**: `src/BE/`, `src/FE/`, `doc/Prototype/`, `doc/ERD/`. Exception: a token-update task starts in the live source (Core Principle 4) — then edits stay confined to the token values the task names.
- **When to cross (read-only, expected)**: reading live source in `src/FE/` or `src/BE/` to extract real CSS/token values (the code is the source of truth). 🧊 `doc/Prototype/` is **frozen history** — readable for design *intent* only, never for extraction.

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

**~~Greenfield carve-out~~ — EXPIRED 2026-08-22. The real app has landed.**

The earlier text said *"PlatformManager has no shipped frontend/backend app yet (`src/FE/` and `src/BE/` are empty) — the only shipped UI today is the static prototype"*. That is **no longer true**: `src/FE/` is a complete Angular 20 app with **6 routed screens** (`app.routes.ts`), and `src/BE/` is a running .NET solution.

**Live source, in priority order:**

1. **`src/FE/src/app/**`** — the shipped app. Component templates (`*.html`), scoped styles (`*.scss`), and `src/styles.scss` are the ground truth for layout, copy, and tokens.
2. **`doc/Prototype/*.html`** — **design INTENT reference only**, not as-shipped. It covers 4 of 6 screens and the app has since diverged from it. Cite it for original intent (why a layout looks the way it does), never as evidence of current behaviour.
3. **`spec/*/`** — business rules behind the UI.

Two screens (`/doi-mat-khau`, `/quan-tri/phan-quyen`) have **no prototype at all** — they were built directly in Angular. For those the app is the only source; do not invent a prototype lineage for them.

> Why this matters enough to write down: sourcing a spec from the prototype now would produce a document describing a screen that **is not what ships** — the exact "documentation describes something that doesn't exist" failure that `.claude/CLAUDE.md` §3 forbids.

## Groups

- **`Backend/`** — private UIs embedded directly in a backend service under `src/BE/` once one exists.
- **`Frontend/`** — the public app under `src/FE/` (Angular 20, 6 routes).
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
- Screenshots — **default is ONE desktop shot per screen** (decided 2026-08-22). That is what answers "what does this screen look like"; state and viewport variants are captured only when someone actually needs that case. Record the rest as `pending` rows with exact capture instructions in the UiInventory Screenshot Manifest, and never block on screenshots.
  > Why the cap: an earlier pass queued **40** pending shots across 5 screens and none were ever taken. A `pending` list nobody works through is worth the same as no list — and it hides which screens genuinely have no visual reference at all.
  Capture via the chrome-devtools MCP once the target is reachable. For PlatformManager that means **both** servers running: `dotnet run --project src/BE/PlatformManager.Api --urls http://localhost:5027`, and the Angular dev server in `src/FE` on **any port in 4200–4203** — all four are in the backend's Development CORS allowlist, so a busy 4200 needs no config change (`npx ng serve --port 4202`). Most screens need an authenticated session; a fresh database needs `bash scripts/setup-database.sh` first. Never record credentials in any design artifact — including in capture instructions.
- Prompt packs resolve tokens to **literal values** (hex/px/font) — external tools cannot interpolate `{token.reference}`.
- Lint DESIGN.md with `npx --yes --package=@google/design.md designmd lint <path>` — the bare `npx @google/design.md lint` form **fails silently on Windows**. Gate = 0 errors; warnings are recorded as as-shipped facts, not blockers.

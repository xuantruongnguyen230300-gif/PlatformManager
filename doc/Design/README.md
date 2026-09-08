---
kind: luat
scope: du-an
verified: 2026-09-06
---

# Design — Product Design Home

Single home for PlatformManager's Product Design work, covering every UI surface of the solution — the shipped Angular 20 app in `src/FE/` and, once one exists, any UI embedded in `src/BE/`.

Count the routed screens rather than reading a number out of prose here — the count changes whenever a module lands or is removed:

```bash
grep -cE '^\s+loadChildren:' src/FE/src/app/app.routes.ts
```

PASS = that number equals the row count of the Screen Census in
[`Frontend/PlatformManager/UiInventory.md`](./Frontend/PlatformManager/UiInventory.md). The `''`
redirect and the `**` wildcard are **not** routed screens; both resolve onto one of the counted
routes.

## Structure

```
doc/Design/
├── README.md                      # 📖 This guide — index & conventions
├── CLAUDE.md                      # 🤖 AI guidance: fidelity policy, conventions, pipeline
├── SETUP.md                       # 🛠️ Environment setup: MCP servers, prerequisites, first run
├── Templates/                     # 📋 Canonical template per artifact type
├── Shared/                        # 🌐 Cross-app, brand-level material (created on demand)
│   ├── Assets/                    #    Logos, illustrations, icon sources
│   └── UserFlows/                 #    Journeys spanning multiple apps
└── Frontend/                      # 🖥️ Frontend UI projects
    └── PlatformManager/           #    Angular 20 app — src/FE/
```

One folder per UI project, grouped by where its source code lives. New projects get a folder under the matching group (`Frontend/` or `Backend/`) when design work starts on that surface.

## UI project index

The Pipeline column tracks the 8 stages (1 Scaffold · 2 Inventory · 3 Tokens · 4 Components · 5 Screens · 6 Prompts · 7 Audit · 8 Figma) — see the project README dashboard for detail.

| Project | Group | Tech | Source code | Pipeline (1–8) |
| --- | --- | --- | --- | --- |
| [PlatformManager](./Frontend/PlatformManager/README.md) | Frontend | Angular 20 (standalone + Signals, zoneless), PrimeNG + PrimeIcons v7, SCSS | `src/FE/src/app`, `src/FE/src/styles.scss` | 1✅ 2✅ 3✅ 4✅ 5✅ 6✅ 7🔁 8⛔ |

> **Re-run again on 2026-08-29 after the app changed underneath the specs.** Two things happened
> that day: the `DtiWeekly` business module (dashboard + DTI catalogue) was removed from `src/FE`
> to be rebuilt, and the shared component layer plus the whole palette were rewritten. Stages 2–4
> were re-run against the result.
>
> 🔄 **SỬA 2026-09-08.** This paragraph continued *"Stages 5 and 6 are 🔁 because the artifacts for
> the **removed** screens are being retired rather than refreshed — `Screens/01`, `Screens/02` and
> their prompt packs now carry historical banners"*. Neither half is true any more, and the two
> halves stopped being true on different days. **Stage 5**: `Screens/01` and `Screens/02` were
> rewritten on 2026-09-05 as `status: "target — not built"` — approved designs for the DTI rebuild,
> not retired descriptions of deleted code — so the 🔁 there was already stale. **Stage 6**: their
> prompt packs were regenerated on 2026-09-08 from those specs, with the current palette, the current
> type face and the current token names. Read each artifact's own frontmatter rather than this
> paragraph:
>
> ```bash
> grep -H 'status:' doc/Design/Frontend/PlatformManager/Screens/*.md doc/Design/Frontend/PlatformManager/Prompts/*.md
> ```
>
> **Stage 7 is 🔁 and stale**: the last audit ran 2026-08-22, before both of those changes.
> `/design-audit PlatformManager` needs a fresh run before its verdict means anything.
> Stage 8 is ⛔ for an unrelated reason: every Figma account tried hit the Starter-plan MCP quota
> (6 tool calls/month), so the artifact set here is the hand-off instead.
>
> The 2026-08-22 pass is still worth knowing about: it came back BLOCKED with 11 findings, all
> fixed the same day, and every one was documentation drift from the same failure — a doc was
> corrected and the documents citing it were not. `Frontend/PlatformManager/AUDIT.md` keeps that
> verdict alongside its resolution log.

## Per-project folder convention

Every project folder follows the layout in [Templates/](./Templates/):

| Item | Purpose |
| --- | --- |
| `README.md` | Project guide: stack, source paths, maintenance rules + 8-stage pipeline dashboard |
| `UiInventory.md` | Ground-truth census of the live app (routes, views, copy sources, brand-asset + screenshot manifests) — the pipeline's first gate |
| `DESIGN.md` | [design.md format](https://stitch.withgoogle.com/docs/design-md/overview/) — YAML token frontmatter + design guidance, importable into Google Stitch; opens with the fidelity blockquote |
| `COMPONENTS.md` + `Components/` | Component index and one spec per component (Sources cited, 5-state tables) |
| `Tokens/` | Token reference (`colors.md`, `typography.md`, `spacing.md`) + `tokens.json` (W3C DTCG `global`+`light`+`dark`, for Figma Tokens Studio); chart palette or explicit "None" |
| `Icons.md` | Icon system + per-action map + declared legacy exceptions |
| `Screens/` | Screen specs grouped by flow — 7 mandatory sections per screen (Layout Blueprint · Copy · States · Responsive · Iconography · Screenshots · Normalize on redesign) |
| `Assets/` | `Brand/` (real files from the live app) + `Screenshots/<flow>/<view>[--state][--viewport].png` |
| `Prompts/` | Multi-tool prompt packs per flow (Stitch, Claude Design, Google AI Studio, generic) |
| `Exports/` + `AUDIT.md` | Append-only Figma export log · latest audit verdict |
| On demand | `Wireframes/`, `Mockups/`, `Prototypes/`, `UserFlows/` |

## Workflow: the 8-stage pipeline

The arc is **capture → generate (Stitch / Claude Design / Google AI Studio) → Figma → feature**, run as eight gated skills (see `CLAUDE.md` § Pipeline & Skills for the gate table):

1. `/design-new-project <Group>/<Project>` — scaffold from templates. *(Already run once for `Frontend/PlatformManager` — see below.)*
2. `/design-inventory-ui <project>` — census the live app: routes, views, copy sources; copy brand assets; capture screenshots (or record pending).
3. `/design-extract-tokens <project>` — live source → `Tokens/*`, `tokens.json`, `DESIGN.md`; lint to 0 errors.
4. `/design-document-components <project>` — component specs with source citations and 5-state tables.
5. `/design-create-screens <project> [flow]` — faithful screen specs (blueprint, verbatim copy, states, responsive, icons, screenshots).
6. `/design-generate-prompts <project> <flow>` — multi-tool prompt pack with tokens resolved to literal values.
7. `/design-audit <project>` — PASS/BLOCKED verdict with fix commands (`AUDIT.md`).
8. `/design-export-figma <project>` — tokens via Tokens Studio and/or the Figma MCP; screens mapped 1:1 to component specs; proof logged in `Exports/ExportLog.md`.

**Where things stand**: `Frontend/PlatformManager` has stages 1–4 current against `src/FE/`, with stages 5–6 mid-refresh after the 2026-08-29 module removal and palette rewrite. Next is `/design-audit PlatformManager` — its last verdict predates both changes. See [SETUP.md](./SETUP.md) for the one-time environment setup (MCP servers, prerequisites).

**Hand off**: every spec cites its source files so developers map designs 1:1 to existing markup.

## Conventions

- **English** for all documentation.
- **Never hard-code** colors/sizes in specs — reference tokens.
- **Compose from existing components** — extend a spec rather than inventing a new component.
- **Single-app material stays in its project folder**; only genuinely cross-app material goes in `Shared/`.

---
kind: luat
scope: core
verified: 2026-09-06
project: "<project>"
status: "draft"
updated: "YYYY-MM-DD"
flow: "<flow name, e.g. Dashboard Overview>"
screens: ["<Screen name>"]
source_routes: ["</route>"]
---

# <Flow> — Screens

<!-- Flow overview: 1-3 sentences on what the flow does, who reaches it, and how its screens chain together. Sections 1-6 of every screen record the app AS-SHIPPED (real copy, real assets, quirks); deviations go ONLY in "Normalize on redesign". -->

> **Shell:** <app shell | no app shell — see DESIGN.md → Layout>
> **Sources:** `<src/FE/src/app/<layer>/<feature>/>` <!-- `<layer>` = `platform/` for Core screens, `modules/` for business screens — see doc/kien-truc-core-module.md -->

---

## <Screen name> (`/route`)

<!-- Repeat this whole block once per entry in `screens`. The seven H3 sections are mandatory, in this exact order. -->

### Layout Blueprint

<!-- Region tree + structural measurements (widths, heights, paddings). Compose ONLY component names present in COMPONENTS.md — never invent one here. -->

- App shell (skip link → sidebar → sticky topbar → `main`)
  - Card
    - Toolbar (search + filter panel + right-pinned actions)
    - DataTable

### Copy

<!-- Verbatim shipped strings — typos and mixed languages included — with localization key and source file.
     ⚠️ Fill "Localization key" against the app, not from memory: a project with an i18n runtime has
     NO user-facing sentence left in its templates, and writing "— (hardcoded)" for one is a single wrong
     cell that every prompt pack downstream inherits. Check with `ls src/FE/public/i18n/` and
     `bash scripts/fe-gate.sh` (G12). Cite the file, not `file:line` — a line number rots silently and
     `check-docs.sh` §4 cannot tell a rotted one from a good one. -->

| Element | Verbatim copy | Localization key | Source |
| --- | --- | --- | --- |
| Title | `<rendered text>` | `<feature>.routeTitle` | `<...>/<name>.html` |

### States

<!-- How each state renders: default / loading / empty / error / validation display. Add 404/500 entries where this flow owns those pages. -->

- **default:** <...>
- **error:** <...>
- **validation:** <where and how field errors display>

### Responsive

<!-- Behavior per breakpoint (e.g. ≥980 / <980 / <560): what stacks, collapses, or hides. -->

### Iconography

<!-- One row per action icon — or a single pointer to the Icons.md map when the screen only uses mapped icons. -->

| Action | Icon | Placement |
| --- | --- | --- |
| <action> | `pi pi-<name>` (or `—` if the control is text-only) | <region> |

### Screenshots

<!-- Refs into Assets/Screenshots/<this-file-stem>/, or write: pending — see UiInventory, Screenshot Manifest.
     Naming: <view>[--<state>][--<viewport>].png; the desktop-1440 default is written out, e.g.
     sign-in--desktop-1440.png, sign-in--error--desktop-1440.png, sign-in--mobile-390.png. -->

- `Assets/Screenshots/<flow-stem>/<view>--desktop-1440.png`

### Normalize on redesign

<!-- Screen-local quirks ONLY here — sections 1-6 stay as-shipped. A quirk that spans components belongs in the component's own spec (`Components/<Name>.md` → Normalize on redesign), not here. -->

- <quirk as shipped> → <what a redesign should do instead>

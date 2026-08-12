---
project: "<project>"
status: "draft"
updated: "YYYY-MM-DD"
flow: "<flow name, e.g. Dashboard Overview>"
screens: ["<Screen name>"]
source_routes: ["<#anchor or /route>"]
---

# <Flow> — Screens

<!-- Flow overview: 1-3 sentences on what the flow does, who reaches it, and how its screens chain together. Sections 1-6 of every screen record the app AS-SHIPPED (real copy, real assets, quirks); deviations go ONLY in "Normalize on redesign". -->

> **Shell:** <dashboard | auth — see DESIGN.md → Layout>
> **Sources:** `<doc/Prototype/dashboard.html>`

---

## <Screen name> (`#anchor` or `/route`)

<!-- Repeat this whole block once per entry in `screens`. The seven H3 sections are mandatory, in this exact order. -->

### Layout Blueprint

<!-- Region tree + structural measurements (widths, heights, paddings). Compose ONLY component names present in COMPONENTS.md — never invent one here. -->

- Topbar (sticky, logo + actions)
  - Button × 3 (secondary, primary)
- KPI grid (5 columns)
  - Card × 5

### Copy

<!-- Verbatim shipped strings — typos and mixed languages included — with localization key and file:line source. -->

| Element | Verbatim copy | Localization key | Source |
| --- | --- | --- | --- |
| Title | `DTI Weekly` | — (hardcoded) | `dashboard.html:63` |

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
| Save week | — (text button) | topbar |

### Screenshots

<!-- Refs into Assets/Screenshots/<this-file-stem>/, or write: pending — see UiInventory, Screenshot Manifest.
     Naming: <view>.png = desktop-1440 default state; suffixes --<state> and --<viewport>, e.g. dashboard--error.png, dashboard--mobile-390.png. -->

- `Assets/Screenshots/<flow-stem>/dashboard.png`

### Normalize on redesign

<!-- Screen-local quirks ONLY here — sections 1-6 stay as-shipped. Library-wide issues go to COMPONENTS.md → Known inconsistencies. -->

- <quirk as shipped> → <what a redesign should do instead>

---
project: "<project-slug>"
status: "draft"
updated: "YYYY-MM-DD"
library: "<e.g. none yet — text-only buttons/badges>"
legacy_exceptions: []         # non-standard sets still shipped, e.g. ["Font Awesome", "Material Design Icons"]
---

# Icons — <project> Design System

> **Standard icon set: <library>.** <!-- Where it is hosted/loaded from — cite the live font/css file and the layout that loads it. -->

## Library & Sizing

<!-- Usage rules extracted from the live UI, not invented. Cite classes exactly as shipped. -->

- Icon element: <!-- exact markup, e.g. an emoji or inline SVG; when each style variant is used -->
- Size: <!-- default (inherit?) + known overrides with their classes/values -->
- Gap to text: <!-- e.g. spacing value -->
- Color: <!-- inheritance rule; when semantic color is allowed -->

## Per-Action Map

<!-- One row per action/context the shipped UI covers. Keep mappings stable across specs and Figma so dev handoff is 1:1. -->

| Action/context | Icon | Library | Live class | Source file:line |
|----------------|------|---------|------------|------------------|
| Save action | — (text button "Lưu tuần này") | none | `.btn.primary` | `dashboard.html:67` |

## Legacy Exceptions

<!-- As-shipped uses of non-standard icon sets. Record them faithfully — specs must show what ships, never silently swap in the standard set. Mirror the set names in `legacy_exceptions` frontmatter. -->

| Set | Where it lingers | On disk |
|-----|------------------|---------|
| | | |

## Normalize on redesign

<!-- Numbered replacement plan: each legacy icon → its standard-library equivalent. Applied only during redesigns, never retro-fitted into as-shipped specs. -->

1. <!-- e.g. adopt a real icon set once the app moves off the static prototype. -->

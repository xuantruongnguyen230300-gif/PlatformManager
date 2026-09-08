---
kind: luat
scope: core
verified: 2026-09-06
project: "<project-slug>"
status: "draft"
updated: "YYYY-MM-DD"
library: "<the icon set(s) actually loaded, e.g. PrimeIcons v7 (icon font, loaded via angular.json) + any set a component library injects at runtime>"
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

**Last column is a file, never `file:line`.** The glyph name is the anchor — `grep -n 'pi-pencil' <file>`
answers "where" in one command and cannot rot. A line number can be wrong while still pointing inside
its file, and `check-docs.sh` §4 passes on every one of those. Same move as
`doc/Design/CLAUDE.md` § Neo trích dẫn vào `styles.scss`, same reason.

| Action/context | Icon | Library | Live class | Source file |
|----------------|------|---------|------------|-------------|
| Edit row | `pi pi-pencil` | PrimeIcons v7 | `.icon-btn.primary` | `<shared/components/.../<name>.html>` |

## Legacy Exceptions

<!-- As-shipped uses of non-standard icon sets. Record them faithfully — specs must show what ships, never silently swap in the standard set. Mirror the set names in `legacy_exceptions` frontmatter. -->

| Set | Where it lingers | On disk |
|-----|------------------|---------|
| | | |

## Normalize on redesign

<!-- Numbered replacement plan: each legacy icon → its standard-library equivalent. Applied only during redesigns, never retro-fitted into as-shipped specs. -->

1. <!-- e.g. replace the Unicode glyph standing in for a tree branch with the icon set's own equivalent. -->

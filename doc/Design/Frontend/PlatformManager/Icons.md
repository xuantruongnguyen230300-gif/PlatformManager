---
project: "PlatformManager"
status: "draft"
updated: "2026-08-11"
library: "none — text-only buttons/badges, no icon font or SVG icon system loaded anywhere in dashboard.html"
legacy_exceptions: []
---

# Icons — PlatformManager Design System

> **Standard icon set: none.** `doc/Prototype/dashboard.html` loads no icon font (`<link>`/`@font-face`), no SVG sprite, no icon component library. Every action is a plain text `Button`/`Input`/`Select`, and every directional/status cue is either color (`Badge`, `DeltaIndicator`) or a literal Unicode glyph (`↑`/`↓`) inline in a JS template string (`dashboard.html:853,886,901`) — not a rendered icon element.

## Library & Sizing

- Icon element: **none** — no `<svg>`, `<i class="icon-...">`, or emoji is used as a UI icon anywhere in the shipped markup.
- Size: N/A.
- Gap to text: N/A.
- Color: N/A — the only "iconography" is the plain-text arrow glyphs `↑`/`↓`, which inherit the surrounding `DeltaIndicator` text color (`colors.success`/`colors.danger`, see `Components/DeltaIndicator.md`).

## Per-Action Map

<!-- One row per action/context the shipped UI covers. Keep mappings stable across specs and Figma so dev handoff is 1:1. -->

| Action/context | Icon | Library | Live class | Source file:line |
|----------------|------|---------|------------|------------------|
| Change reporting period | — (native `<input type="date">`) | none | `#weekDate` | `dashboard.html:80` |
| Load a saved period | — (native `<select>`) | none | `#savedWeeks` | `dashboard.html:81` |
| Create new period from latest | — (text button) | none | `.btn` | `dashboard.html:82` |
| Enter progress % per criterion | — (native `<input type="number">`, `%` shown as plain sibling text) | none | `.progressInput` | `dashboard.html:891` |
| Enter weekly note per criterion | — (native `<input type="text">`) | none | `.noteInput` | `dashboard.html:893` |
| Save week (primary, topbar) | — (text button "Lưu tuần này") | none | `.btn.primary` | `dashboard.html:67` |
| Save week (mobile floating) | — (text button "Lưu tuần") | none | `.fab` | `dashboard.html:136` |
| Export/backup data | — (text button "Sao lưu") | none | `.btn.desktop` | `dashboard.html:65` |
| Import/restore data | — (text label "Khôi phục" wrapping a hidden `<input type="file">`) | none | `.btn.desktop` | `dashboard.html:66` |
| View a saved period (history row) | — (text button "Xem") | none | `.btn` | `dashboard.html:901` |
| Search criteria by code/name | — (native text `<input>`, placeholder text only, no search-glyph icon) | none | `#q` | `dashboard.html:108` |
| Filter by group | — (native `<select>`) | none | `#groupFilter` | `dashboard.html:109` |
| Filter by change level | — (native `<select>`) | none | `#changeFilter` | `dashboard.html:110-112` |
| Sort table | — (native `<select>`) | none | `#sortBy` | `dashboard.html:113-115` |
| Generate quick report | — (text button "Báo cáo nhanh") | none | `.btn` | `dashboard.html:83` |
| Close report dialog | — (text button "Đóng") | none | `.btn` | `dashboard.html:139` |
| Copy report to clipboard | — (text button "Sao chép") | none | `.btn` | `dashboard.html:142` |
| Print report | — (text button "In") | none | `.btn.primary` | `dashboard.html:143` |
| Increase indicator | literal glyph `↑` (plain text, not an icon element) | none | `.delta.up` / `.value.good` | `dashboard.html:853,886,901` |
| Decrease indicator | literal glyph `↓` (plain text, not an icon element) | none | `.delta.down` / `.value.bad` | `dashboard.html:853,886` |

## Legacy Exceptions

<!-- As-shipped uses of non-standard icon sets. Record them faithfully — specs must show what ships, never silently swap in the standard set. Mirror the set names in `legacy_exceptions` frontmatter. -->

| Set | Where it lingers | On disk |
|-----|------------------|---------|
| — | none found | — |

## Normalize on redesign

<!-- Numbered replacement plan: each legacy icon → its standard-library equivalent. Applied only during redesigns, never retro-fitted into as-shipped specs. -->

1. Adopt a real icon set (e.g. for save/export/import/search/filter/sort actions and up/down indicators) once the app moves off the static prototype — the shipped app relies entirely on text labels and color, which is functional but not iconographically differentiated.

---
project: "<project>"
status: "draft"
updated: "YYYY-MM-DD"
components_total: "<n>"
---
<!-- This template holds TWO skeletons. PART A becomes COMPONENTS.md (the library index); PART B becomes Components/<Name>.md (one file per component). Cut at the PART B delimiter. -->

# COMPONENTS.md — <project> Component Library

> **Purpose:** index of the reusable UI components extracted from the live views, so designers and AI tools generate UI that matches the shipped product.
> **Principle:** every screen MUST be composed from these components — extend a spec in `Components/` instead of inventing new ones. All values come from `DESIGN.md` frontmatter and `Tokens/`; never hard-code colors/sizes outside them.

## General conventions
<!-- House rules that apply to every component: the five required states, framework/theme, icon set, dark-mode mechanism. -->

## Component index

| Component | File | Summary |
| --- | --- | --- |
| Button | [Components/Button.md](./Components/Button.md) | Primary/danger variants, pill radius |

## Known inconsistencies (current code — normalize in redesigns)
<!-- Library-wide quirks shipped in the product, recorded AS-IS with the convergence target. Screen-local quirks stay in that screen's "Normalize on redesign". -->
1. <inconsistency as shipped> — converge on <target>.

## Checklist when adding a new component
- [ ] Clear, consistent name; one file in `Components/`; anatomy, all variants, and the five states documented.
- [ ] Only token values from `DESIGN.md` / `Tokens/`; exact source file paths cited.
- [ ] Row added to the index table above (and `components_total` bumped).

<!-- ==================== PART B — Components/<Name>.md ==================== -->
---
project: "<project>"
status: "draft"
updated: "YYYY-MM-DD"
component: "<Name>"
sources: ["<doc/Prototype/dashboard.html>"]
---

# <Name>
**Description:** <!-- One sentence: what it does + the base CSS class it wraps. -->

## Anatomy
<!-- Structure in prose, e.g. `[icon (optional)] [label]` — plus radius, font, gaps. -->

## Variants

| Variant | Classes | Key values | When to use |
| --- | --- | --- | --- |
| Primary | `btn primary` | bg `var(--brand)`, text `#fff` | Main action of the view |

## States
<!-- Exactly these five rows, in this order — treatments as rendered by the shipped CSS. -->

| State | Treatment |
| --- | --- |
| default | <shipped treatment> |
| hover | <shipped treatment> |
| focus | <shipped treatment> |
| active | <shipped treatment> |
| disabled | <shipped treatment> |

## Tokens Used
<!-- Bullets of DESIGN.md / Tokens/ names this component consumes; a raw value with no token behind it is an inconsistency to log. -->
## Reference markup

```html
<button class="btn primary" onclick="saveWeek()">Lưu tuần này</button>
```

Sources: `<file1>`, `<file2>` <!-- the exact views the spec was extracted from; mirror the frontmatter `sources` list. -->

## Do / Don't

- ✅ <hard usage rule>
- ❌ <observed misuse to avoid>

## Normalize on redesign
<!-- Component-local quirks ONLY here — everything above records the app AS-SHIPPED. -->

---
project: "PlatformManager"
status: "draft"
updated: "2026-08-11"
components_total: "12"
---

# COMPONENTS.md — PlatformManager Component Library

> **Purpose:** index of the reusable UI components extracted from the live views, so designers and AI tools generate UI that matches the shipped product.
> **Principle:** every screen MUST be composed from these components — extend a spec in `Components/` instead of inventing new ones. All values come from `DESIGN.md` frontmatter and `Tokens/`; never hard-code colors/sizes outside them.

## General conventions

- **Five required states** — every component below documents `default` / `hover` / `focus` / `active` / `disabled`, as actually implemented (or not) in `doc/Prototype/dashboard.html`. Critical as-shipped fact: the entire stylesheet has exactly **one** interactive pseudo-class rule (`.btn:active{transform:translateY(1px)}`, `dashboard.html:24`) — no `:hover`, no `:focus`, no `:disabled` rule exists anywhere, and no element in the markup ever carries a `disabled` attribute. Every component's `hover`/`focus`/`disabled` row below therefore reads "not styled — browser UA default" / "unreachable", which is the honest as-shipped state, not an omission.
- **No framework, no component library, no Storybook** — anatomy/variants are read directly from CSS class selectors and markup in the single `<style>`/`<script>` blocks of `dashboard.html`.
- **Single light theme** — no dark mode, no theme toggle exists in the shipped app (see `DESIGN.md` § Colors).
- **Icon set** — none. `dashboard.html` uses no icon font/SVG icon system anywhere; all visual cues are text, color, or Unicode arrow glyphs (`↑`/`↓`) inline in JS template strings (e.g. `dashboard.html:853,886,901`). See `Icons.md` (stage 4 note — no icon system to document beyond this).

## Component index

| Component | File | Summary |
| --- | --- | --- |
| Button | [Components/Button.md](./Components/Button.md) | `.btn` default/primary variants; `.btn.danger` is CSS-only, unused in markup |
| Card | [Components/Card.md](./Components/Card.md) | Generic bordered/shadowed container (`.card`) used as the shell for every section |
| KpiTile | [Components/KpiTile.md](./Components/KpiTile.md) | Label/value/sub-caption stat tile (`.card.kpi`), 5 instances in the KPI grid |
| Badge | [Components/Badge.md](./Components/Badge.md) | Runtime-computed status pill (`.badge` + `.bdone`/`.bwork`/`.bstall`) |
| ProgressBar | [Components/ProgressBar.md](./Components/ProgressBar.md) | Group-progress track + fill (`.bar`/`.fill`) |
| Table | [Components/Table.md](./Components/Table.md) | Sticky-header, horizontally-scrolling 62-row criteria table (`.tablewrap` + `table`) |
| Dialog | [Components/Dialog.md](./Components/Dialog.md) | Native `<dialog>` modal for the quick report (`dialog#reportDialog`) |
| Fab | [Components/Fab.md](./Components/Fab.md) | Mobile-only floating "Lưu tuần" button (`.fab`) |
| Input | [Components/Input.md](./Components/Input.md) | Text/number/date/select fields — two tiers: filter/weekbar vs. table-cell |
| NoticeBanner | [Components/NoticeBanner.md](./Components/NoticeBanner.md) | Static instructional banner (`.notice`) |
| DeltaIndicator | [Components/DeltaIndicator.md](./Components/DeltaIndicator.md) | Up/down/flat change text (`.delta` + `.up`/`.down`/`.flat`), reused in KPI, table, history |
| HistoryRow | [Components/HistoryRow.md](./Components/HistoryRow.md) | One saved-period row in the history list (`.histrow`) |

## Known inconsistencies (current code — normalize in redesigns)

1. `.btn.danger{color:var(--bad)}` is defined in CSS (`dashboard.html:24`) but **no element in the markup ever carries the `danger` class** — dead/unused style rule. Converge on: either wire it to a genuine destructive action (e.g. "Khôi phục", which currently uses the neutral `.btn` style despite overwriting all data) or remove the dead rule.
2. **No custom interactive states exist anywhere** — every `hover`/`focus`/`disabled` treatment across all 12 components below is "browser UA default" or "unreachable" (no `disabled` attribute is ever set in the markup). Converge on: define explicit hover/focus-visible/disabled treatments per component for accessibility and affordance.
3. Two different "text field" treatments coexist for the same conceptual role — filter/weekbar `input,select` (`border:1px solid var(--line)`, `border-radius:9px`, `padding:9px 10px`, `dashboard.html:34`) vs. table-cell `.progressInput`/`.noteInput` (`border:1px solid #cad4e1`, `border-radius:8px`, `padding:7px`, `dashboard.html:44-45`). Converge on one shared input treatment (see `Components/Input.md`).
4. `--brand2:#174ca8` (declared `dashboard.html:12`) is never consumed by any component below — see `Tokens/colors.md`.

## Checklist when adding a new component
- [ ] Clear, consistent name; one file in `Components/`; anatomy, all variants, and the five states documented.
- [ ] Only token values from `DESIGN.md` / `Tokens/`; exact source file paths cited.
- [ ] Row added to the index table above (and `components_total` bumped).

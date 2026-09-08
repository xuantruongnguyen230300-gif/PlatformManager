---
kind: luat
scope: core
verified: 2026-09-06
project: "<project>"
artifact: "Component index"
status: "draft"
updated: "YYYY-MM-DD"
---
<!-- This template holds TWO skeletons. PART A becomes COMPONENTS.md (the library index); PART B becomes Components/<Name>.md (one file per component). Cut at the PART B delimiter. -->
<!-- When instantiated into doc/Design/<Group>/<Project>/, both parts take `scope: du-an` and `verified: chua-doi-chieu` — see doc/Design/CLAUDE.md § Per-project folder convention. `scope: core` above belongs to the TEMPLATE itself, which is CoreBase tooling. -->

# Components — <project>

<!-- Open by saying that this index is the GATE: a screen spec may only compose components listed here, and a Components/*.md without a row here is not composable. -->

Count the specs rather than trusting a number written in prose
(`.claude/CLAUDE.md` §6 — never hard-code a component total, in the frontmatter or the body):

```bash
ls doc/Design/<Group>/<project>/Components/*.md | wc -l
```

**Source of truth for every row:** <!-- the shared component layer (global stylesheet) + the framework components it wraps; cite exact paths. -->

<!-- If any row is specified but NOT implemented, mark it and say so here, with a command that counts those rows. Do not let a spec-only row read as a description of running code. -->

## The rule this index enforces

<!-- One component, one definition. No screen/page/component stylesheet re-declares a button, input, badge, toolbar, table frame or dialog footer. State it as a rule, then say what it buys: each row maps to exactly one thing to build. -->

---

## Index

| Component | Spec | What it is |
|---|---|---|
| Button | [Components/Button.md](../Frontend/PlatformManager/Components/Button.md) | <one line: the base class it wraps + the variants that matter> — link target here points at the live example so this template's own links resolve; in the generated file it is `./Components/Button.md` |

## What a spec must contain

<!-- Every file cites a Sources section against the live file, and a table of the five states — `default`, `:hover`, `:focus-visible`, `:active`, `:disabled`. Hand-rolled components must define all five explicitly. -->

<!-- Close with: extending a component is preferred over adding one; a variant that is missing gets added to the existing spec AND to the shared layer, never as a parallel class. -->

## Checklist when adding a new component

- [ ] Clear, consistent name; one file in `Components/`; anatomy, all variants, and the five states documented.
- [ ] Only token values from `DESIGN.md` / `Tokens/`; exact source file paths cited.
- [ ] Row added to the `## Index` table above.
- [ ] Frontmatter carries `kind` / `scope` / `verified` (`.claude/CLAUDE.md` §9) — the gate `check-docs.sh` §10 fails without all three.

<!-- ==================== PART B — Components/<Name>.md ==================== -->
---
kind: luat
scope: du-an
verified: chua-doi-chieu
project: "<project>"
status: "draft"
updated: "YYYY-MM-DD"
component: "<Name>"
sources:
  - "<src/FE/src/styles.scss>"
  - "<src/FE/src/app/shared/components/<component>/<component>.html>"
---

# <Name>
**Description:** <!-- One sentence: what it does + the base CSS class it wraps. -->

## Anatomy
<!-- Structure in prose, e.g. `[icon (optional)] [label]` — plus radius, font, gaps. -->

## Variants

| Variant | Classes | Key values | When to use |
| --- | --- | --- | --- |
| Primary | `btn primary` | bg `var(--brand)`, text `var(--on-primary)` | Main action of the view |

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

<!-- Copy the shipped markup verbatim. Two constraints it must respect, both enforced by machine:
     - no hard-coded colour/size — reference tokens (doc/Design/CLAUDE.md § Rules);
     - no user-facing sentence written into the template — text comes from the i18n layer, and
       gate G12 in scripts/fe-gate.sh fails on a Vietnamese sentence in an .html template. -->

```html
<button class="btn primary" type="submit">{{ 'shared.action.save' | translate }}</button>
```

Sources: `<file1>`, `<file2>` <!-- the exact views the spec was extracted from; mirror the frontmatter `sources` list. -->

## Do / Don't

- ✅ <hard usage rule>
- ❌ <observed misuse to avoid>

## Normalize on redesign
<!-- Component-local quirks ONLY here — everything above records the app AS-SHIPPED. -->

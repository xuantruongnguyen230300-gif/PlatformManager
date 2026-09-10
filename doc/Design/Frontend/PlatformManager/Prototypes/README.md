---
kind: luat
scope: du-an
verified: 2026-09-06
project: "PlatformManager"
artifact: "Interactive prototype"
status: "visual reference — not a source"
updated: "2026-09-10"
---

# Interactive Prototype — PlatformManager

A single self-contained HTML file that renders the whole product: seven screens, the
shared component library, and every documented component state. Open
[`index.html`](./index.html) in a browser — no build step, no dev server, no backend.
Fonts and icons are vendored under [`assets/`](./assets/), so it also works offline.

> ### 🔒 In-repo master, anonymised — merged 2026-09-10
>
> Until 2026-09-10 there were **two** files named `index.html`: this one, and a newer
> working copy the reviewer kept in a folder deliberately outside the repo (see the
> root `.gitignore`). Component specs cited the out-of-repo one, so their *approved*
> copy and geometry rested on a file nobody who clones this repository can open —
> and `.claude/CLAUDE.md` §5 forbids two files describing one thing.
>
> This file is the merged result and the **only** one any document may cite. Two
> different things happened in that merge:
>
> - **Content.** The 2026-09-04 / -09-05 / -09-06 sync work came across whole — the
>   six-group DTI structure, the re-cut detail-table columns, the date-range period
>   labels, the `150px` history grid, the removed report dialog, the toolbar chip
>   order. Tile 1's KPI caption was also brought forward to the wording decided on
>   2026-09-09 in [`../Components/KpiTile.md`](../Components/KpiTile.md).
> - **Anonymisation.** Organisation names, locality names, the source spreadsheet's
>   filename and real internal document numbers were replaced with illustrative
>   stand-ins. DOM structure, class names, geometry and template copy were **not**
>   touched — each of those is something a spec measures, and losing one loses a
>   measurement.
>
> ⚠️ **What was deliberately left alone, and is still open.** The numeric values were
> kept. Component specs and prompt packs quote them as their approved figures, so
> re-fabricating them would silently invalidate every one of those measurements — and
> the derived figures are already in this repository's history through those specs.
> They are unattributable now that no organisation is named anywhere in the file, but
> they remain figures computed from a real dataset. Whether to re-fabricate them is a
> product-owner decision, not a documentation one.
>
> ```bash
> git check-ignore -v doc/Design/Frontend/PlatformManager/Prototypes/index.html
> ```
>
> PASS = **no match** (exit code 1). That is the entire point of the merge: a citation
> is evidence only if the second reader can open what it points at.

## Why this file exists

It is where a change is **seen** before it is committed to anywhere else. Changing a colour, a
spacing step or a component shape here takes one line and is visible immediately across
all seven screens, because every screen composes the same component layer.

That makes it a **visual reference, not a source.** A change previewed here has no effect
until it is written into [`../Tokens/`](../Tokens/) (`*.md` + `tokens.json`) and the
[`../DESIGN.md`](../DESIGN.md) frontmatter — that is the record the team and the dev
handoff read from, and `src/FE/` follows it. Nothing is decided by editing this file.

## How it is organised

The file is deliberately layered. Each layer carries a banner comment naming it.

| Layer | What lives there | Edit here when you want to change… |
| --- | --- | --- |
| **1 — Design tokens** | The `:root` block: colours, type scale, spacing, radii, grid height | anything global — one line changes all seven screens |
| **2 — Shared component library** | `.btn` · `.icon-btn` · `.input` · `.badge` · `.toolbar` · `.tablewrap` · `dialog` · `.notice` · `.check` · `.segmented` | the shape of one component, everywhere it appears |
| **3 — App shell** | sidebar, topbar, toast stack | navigation frame and floating notifications |
| **4 — Per-screen styles** | one block per screen, prefixed `#screen-*` or `app-*` | one screen only |

**One component, one definition.** No screen re-declares a button, input, badge, toolbar,
table frame or dialog footer in its own block — screens only compose classes from layer 2.
This is the property that makes the file usable as the basis for shared Angular components:
each entry in layer 2 maps to exactly one component to build.

## Screens

Seven, switched from the purple toolbar at the top of the page (that bar is prototype
tooling, not part of the design):

Dashboard · DTI catalogue · Users · Permissions · Sign-in · Change password ·
**Component library**

The component library screen lists every shared component with its class name and its
static states, plus a written description of hover / focus / disabled behaviour taken from
the live rules. It is the map for the Angular component work.

## Prototype tooling

The purple bar can switch screens, collapse the sidebar, open every dialog, fire toasts,
and preview tablet / mobile widths.

The tablet and mobile buttons load the page into an **iframe with a real viewport** rather
than narrowing a container. Media queries resolve against the viewport, not against a
parent element, so a narrowed `div` would never trigger the responsive layout — the mobile
rendering would silently be wrong.

## Design decisions previewed here (2026-08-29)

Each of these was agreed after being seen in this file, and is recorded in
[`../Tokens/`](../Tokens/) — this section is the rationale, not the record.

**Contrast.** Border and surface tokens were recomputed against the WCAG contrast formula
rather than picked by eye. Component boundaries now reach the 3:1 non-text threshold;
before the pass, a card separated from the page background by 1.12:1 and table row banding
by 1.05:1, which is what made the interface read as flat.

**Typography.** Be Vietnam Pro, vendored locally at five weights across the Latin,
Latin-Extended and Vietnamese subsets. The previous stack named a face that was never
loaded, so rendering fell through to whatever the operating system supplied.

**Data grids.** Grid height is derived from a flex chain that runs from the page shell down
to the scroll container, so the grid fills the space actually available and scrolls
internally, with the column header pinned. It is not a fixed pixel height: the chrome above
and below a grid differs per screen and per viewport, so any constant is wrong somewhere.

**Toolbars.** One toolbar pattern on its own surface. The search field is bounded rather
than elastic, and filter conditions live behind a `Filter` control that carries a count
badge and exposes applied conditions as removable chips.

## Relationship to the rest of this project folder

```
../Tokens/*.md + tokens.json + ../DESIGN.md   <- the source: what has been decided
                 |  code follows
src/FE/src/styles.scss  +  APP_PALETTE in src/FE/src/app/app.config.ts
                 |  rendered for review
index.html (this folder)                      <- visual reference: what it looks like
```

Direction decided 2026-08-27, re-confirmed by the product owner 2026-08-29. The rule is held
by [`doc/huong_dan/wiki-core/fe/04-design-token-system.md`](../../../../huong_dan/wiki-core/fe/04-design-token-system.md)
§ Chiều — read the reasoning there rather than restating it here.

For which specs are current and which are mid-refresh, read each file's own `updated:`
frontmatter, and the parent [`README.md`](../README.md) § Pipeline Status.

> **Correction (2026-08-29).** An earlier revision of this section said `Tokens/`,
> `Components/` and `Screens/` predated the 2026-08-29 redesign and that *"where the two
> disagree, this file is the newer artefact"*. That was true when it was written and is
> not any more: `Tokens/`, `COMPONENTS.md` + `Components/` and `Icons.md` were rewritten
> from the shipped `src/FE/src/styles.scss` on 2026-08-29, and `Screens/` is mid-refresh.
> More importantly, "newer" was never the test — a prototype is a preview whatever its
> date, and only what is recorded in `../Tokens/` counts as agreed.

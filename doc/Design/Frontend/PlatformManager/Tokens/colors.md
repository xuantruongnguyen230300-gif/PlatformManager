---
kind: luat
scope: du-an
verified: 2026-09-06
project: "PlatformManager"
status: "draft"
updated: "2026-09-06"
category: "colors"
live_source: "src/FE/src/styles.scss"
---

# Colors — PlatformManager Design System

> **Fidelity:** every value below is a real value of the shipped palette, checked against the live app — never invent values while recording what ships. Agreeing a **new** value is a different act, and it starts here: decide it in this file (plus `tokens.json` and the `DESIGN.md` frontmatter), then land it in code. Anything you would prefer different in the *shipped* UI goes to "Normalize on redesign" in the relevant spec, not here.

## Live Source & Extraction Method

**Re-synced 2026-08-29 — a one-time catch-up, not the working direction.** `src/FE/src/styles.scss` was rewritten the same day against the approved preview at `doc/Design/Frontend/PlatformManager/Prototypes/index.html`, so this pass read the shipped values back into this file to close the gap that rewrite opened. The standing direction is the opposite one: **this file is the source and `styles.scss` follows it** (`doc/Design/CLAUDE.md` Core Principle 4). The reasoning is held in exactly one place — `doc/huong_dan/wiki-core/fe/04-design-token-system.md` § Chiều — read it there rather than restating it here. Extraction tooling (`/design-extract-tokens`) records what already ships; it never decides a value.

**Where the values live.** `src/FE/src/styles.scss` declares every custom property in a single `:root { … }` block (`src/FE/src/styles.scss` § `:root`). Count them rather than copying a number — an earlier revision of this line said "42", which stopped being true the moment audit FE-7 added four more:

```bash
grep -cE '^\s*--[a-z0-9-]+\s*:' src/FE/src/styles.scss
```

PASS = that count equals the sum of the rows in the `:root` tables of this file, `Tokens/typography.md` and `Tokens/spacing.md`. Verified exhaustively on 2026-08-29: `grep -rlE '^\s*--[a-z0-9-]+\s*:' src/FE/src --include=*.scss` returns `src/FE/src/styles.scss` and nothing else, and `grep -rn setProperty src/FE/src` returns nothing — there is no second `:root`, no component-scoped custom property, no runtime `style.setProperty()`. There is no Style Dictionary and no token build step; the Angular CLI compiles the SCSS as-is.

**Naming rule.** A design token's name is **exactly its live CSS custom property, minus the `--` prefix** (`--bad-bg` → `bad-bg`). Colors that ship as literals inside a selector rather than in `:root` are listed in their own table; their **value** is real and cited, the **name** is this document's.

**One deliberate exception.** `DESIGN.md` frontmatter carries an extra key `primary` with the same value as `brand`, required by the design.md schema — without a key literally named `primary`, `designmd lint` raises `missing-primary` and Stitch ignores this palette. It is deliberately **not** mirrored into `tokens.json`, where a duplicate would create two Figma variables for one color. If `brand` ever changes, change `primary` with it.

**How themes switch: they do not.** No `data-theme` attribute, no `prefers-color-scheme` query, no theme toggle, no alternate palette anywhere in `src/FE/`. The single shipped palette is therefore the `light` set in `tokens.json`; the `dark` set stays empty rather than inventing values.

**PrimeNG consistency check.** `src/FE/src/app/app.config.ts` re-declares part of this palette in its `APP_PALETTE` constant, which `src/FE/src/app/core/theme/core-preset.ts` expands into the PrimeNG Aura ramps. The ⚠️ paragraph in the header comment of `src/FE/src/styles.scss` (`grep -n APP_PALETTE src/FE/src/styles.scss`) warns that a change made here which skips that file makes CSS and PrimeNG render two different colors with nothing failing. Read the current key set rather than copying a list — it has grown once already:

```bash
sed -n '/^export const APP_PALETTE/,/^};/p' src/FE/src/app/app.config.ts
```

PASS = every key's value equals the matching `:root` property in the tables below. Compared key by key on **2026-09-06**: all match, `onPrimary` included.

> 🔄 **SỬA 2026-09-06.** The previous revision said *"re-declares **ten** of these values"* and listed them. `onPrimary: '#ffffff'` was added on 2026-09-03 — it moved out of `core/theme/core-preset.ts`, where it had been the hard-coded constant `contrastColor`, so that a product whose brand colour needs dark ink would not have to edit the platform. The value did not change; only its home did. The `styles.scss` header comment still says "10 giá trị màu", which is the same stale count in `src/` — out of scope for this file to edit.

**Citation policy — no line numbers into `styles.scss` at all (changed 2026-09-04).** The rule and its reasoning are held in one place: `doc/Design/CLAUDE.md` § Neo trích dẫn vào `styles.scss`. Read it there.

> The previous revision of this paragraph carved out an exception — *"line numbers are given **only** for the `:root` block, which held still across two edits"* — and kept `:38-117` plus one number per token row. **That carve-out was wrong, and it failed exactly as predicted for everything else.** The 2026-08-29 rewrite later grew a 12-line table of contents above `:root`, which pushed the whole block down by 12: `--bg` was cited at `:44` and is declared at `:56`, `--brand` at `:58` against `:70`, `--shadow-toast` correct only because it was written after the shift. Nothing reported it, because the repo gate only asks whether a cited line is **inside** the file — and in a 1015-line file every number under 1015 is. All rows now cite the file plus the identifier.

## Contrast, as measured

The 2026-08-29 palette was computed with the WCAG relative-luminance formula, not picked by eye; the palette comment that opens `:root` in `src/FE/src/styles.scss` (`grep -n 'Bảng màu 2026-08-29' src/FE/src/styles.scss`) records the measurements that motivated it. Every ratio below was recomputed from the shipped hex values on 2026-08-29 and is re-checkable from this table alone.

| Pair | Ratio | Threshold it targets |
| --- | --- | --- |
| `card` on `bg` — card lifts off the page | **1.41:1** | no formal minimum; was 1.12:1 |
| `line` on `card` — component boundary | **3.00:1** | WCAG 2.2 SC 1.4.11, 3:1 non-text |
| `border-strong` on `card` — input boundary | **4.51:1** | one visible step darker than `line` |
| `muted` on `bg` | **5.16:1** | AA body text, 4.5:1 |
| `muted` on `card` | **7.28:1** | AA |
| `muted` on `surface-2` (icon-button hover) | **4.54:1** | AA, worst case |
| `tonal-ink` on `tonal-bg` | **5.82:1** | AA |
| `brand` on `surface-track` — progress fill vs track | **4.68:1** | 3:1 non-text |
| `surface-table-header` on `card` — zebra stripe | **1.16:1** | no minimum; was 1.05:1 |
| `text` on `card` | **16.33:1** | AAA |
| `good` / `warn` / `bad` on their `*-bg` | **5.15 / 4.66 / 5.69:1** | AA for 10px badge text |

`line` measures **2.13:1** against `bg`, not 3:1 — the 3:1 target is met where the boundary actually has to be seen, which is on `card`. Recorded, not hidden.

## Token Table

### Semantic base — `src/FE/src/styles.scss` § `:root`

| Name | Value (light) | Value (dark) | Live variable | Declared at |
| --- | --- | --- | --- | --- |
| bg | `#cfdaea` | *(not shipped)* | `--bg` | `src/FE/src/styles.scss` § `--bg` — page background; also `.toolbar` fill and table row hover |
| card | `#ffffff` | *(not shipped)* | `--card` | `src/FE/src/styles.scss` § `--card` — declared as the shorthand `#fff`; `tokens.json` and `DESIGN.md` carry the 6-digit form for tool compatibility |
| surface-2 | `#c1cde2` | *(not shipped)* | `--surface-2` | `src/FE/src/styles.scss` § `--surface-2` — hover fill for the ghost `.icon-btn` |
| tonal-bg | `#c4d8f6` | *(not shipped)* | `--tonal-bg` | `src/FE/src/styles.scss` § `--tonal-bg` — default `.btn` fill, `.notice` fill, `.filter-chip` fill |
| tonal-ink | `#0f4a9e` | *(not shipped)* | `--tonal-ink` | `src/FE/src/styles.scss` § `--tonal-ink` — text on `--tonal-bg`; deliberately darker than `--brand` |
| text | `#152033` | *(not shipped)* | `--text` | `src/FE/src/styles.scss` § `--text` |
| muted | `#4c576b` | *(not shipped)* | `--muted` | `src/FE/src/styles.scss` § `--muted` |
| line | `#7a97bd` | *(not shipped)* | `--line` | `src/FE/src/styles.scss` § `--line` — component boundary: card, toolbar, table rules, `.segmented`, `.notice` |
| border-strong | `#6077a2` | *(not shipped)* | `--border-strong` | `src/FE/src/styles.scss` § `--border-strong` — inputs/selects/textarea and `.tablewrap` ONLY; it says "you can type here" |
| brand | `#0f5bd7` | *(not shipped)* | `--brand` | `src/FE/src/styles.scss` § `--brand` |
| brand2 | `#174ca8` | *(not shipped)* | `--brand2` | `src/FE/src/styles.scss` § `--brand2` — consumed by `.btn.primary:hover` |
| good | `#0e7050` | *(not shipped)* | `--good` | `src/FE/src/styles.scss` § `--good` |
| good-bg | `#d9f2e6` | *(not shipped)* | `--good-bg` | `src/FE/src/styles.scss` § `--good-bg` |
| warn | `#965e08` | *(not shipped)* | `--warn` | `src/FE/src/styles.scss` § `--warn` |
| warn-bg | `#ffedc7` | *(not shipped)* | `--warn-bg` | `src/FE/src/styles.scss` § `--warn-bg` |
| bad | `#a02b2b` | *(not shipped)* | `--bad` | `src/FE/src/styles.scss` § `--bad` |
| bad-bg | `#fbdcdc` | *(not shipped)* | `--bad-bg` | `src/FE/src/styles.scss` § `--bad-bg` |
| shadow | `0 4px 16px rgba(23,39,67,.1), 0 1px 3px rgba(23,39,67,.06)` | *(not shipped)* | `--shadow` | `src/FE/src/styles.scss` § `--shadow` — two layers; `.card`, `.login-card`, sidebar drawer + flyout. **Not** `.toast-item`, which declares a deeper shadow of its own — see § Elevation used outside `--shadow` |

### Surface / text roles — `src/FE/src/styles.scss` § `:root`

| Name | Value (light) | Value (dark) | Live variable | Declared at |
| --- | --- | --- | --- | --- |
| on-primary | `#ffffff` | *(not shipped)* | `--on-primary` | `src/FE/src/styles.scss` § `--on-primary` — declared as `#fff`; text/icon on `--brand` (brand mark, avatar, `.btn.primary`, `.seg-btn.active`, `.filter-count`) |
| btn-hover-bg | `#c7dbf5` | *(not shipped)* | `--btn-hover-bg` | `src/FE/src/styles.scss` § `--btn-hover-bg` — `.btn:hover` fill |
| danger-border | `#e0a8a8` | *(not shipped)* | `--danger-border` | `src/FE/src/styles.scss` § `--danger-border` — the edge of anything that means *danger*: `.btn.danger` border **and** `.login-error` border |
| danger-hover-bg | `#f5c6c6` | *(not shipped)* | `--danger-hover-bg` | `src/FE/src/styles.scss` § `--danger-hover-bg` — `.btn.danger:hover` fill |
| th-ink | `#536076` | *(not shipped)* | `--th-ink` | `src/FE/src/styles.scss` § `--th-ink` — column-header text; 5.49:1 on `surface-table-header` |
| surface-track | `#dbe4f0` | *(not shipped)* | `--surface-track` | `src/FE/src/styles.scss` § `--surface-track` — progress-bar track and disabled-input fill; deliberately pale because it is a **fill**, not a boundary |
| surface-table-header | `#e9eff6` | *(not shipped)* | `--surface-table-header` | `src/FE/src/styles.scss` § `--surface-table-header` — `th` fill, even-row zebra stripe, `.badge.neutral` |

> **These four were promoted on 2026-08-29 (audit FE-7).** `btn-hover-bg`, `danger-border`, `danger-hover-bg` and `th-ink` were selector literals in the previous revision of this file and are now real custom properties, so under the naming rule above their names changed with them: `tonal-bg-hover` → `btn-hover-bg`, `bad-bg-hover` → `danger-hover-bg`, `text-table-header` → `th-ink`, and `bad-border-btn` + `bad-border-notice` collapsed into the single `danger-border`. The comment above them in `src/FE/src/styles.scss` (`grep -n 'audit FE-7' src/FE/src/styles.scss`) records why the FE side promoted them: gate G1 only scans `src/FE/src/app`, so a literal inside `styles.scss` itself was invisible to it and kept regrowing after each manual cleanup.

### Alpha-composited surfaces — promoted to `:root` on 2026-09-03

| Name | Value (light) | Value (dark) | Live variable | Declared at |
| --- | --- | --- | --- | --- |
| surface-topbar | `rgba(255,255,255,0.95)` | *(not shipped)* | `--surface-topbar` — `.topbar{background:…}` (+ `backdrop-filter: blur(10px)`) | `src/FE/src/styles.scss` § `--surface-topbar` |
| overlay-backdrop | `rgba(20,28,40,0.45)` | *(not shipped)* | `--overlay-backdrop` — `dialog::backdrop{background:…}` **and** `.sidebar-backdrop{background:…}`, now one declaration read from two places | `src/FE/src/styles.scss` § `--overlay-backdrop` |
| surface-nav-active | `rgba(15,91,215,0.08)` | *(not shipped)* | `--surface-nav-active` — `.sidebar-navitem.active{background:…}` | `src/FE/src/styles.scss` § `--surface-nav-active` |

> **These three were promoted on 2026-09-03 when gate G11 went in.** They kept the
> names this table already gave them and their values are byte-identical, so nothing
> on screen changed — this was a split, not a redesign. What forced the move: all three
> lived in `shared/` SCSS, which is inside CoreBase, so this project's brand alpha would
> have travelled to a second product and refused to follow its palette. Gate G1 never
> saw them because G1 only scans `#rrggbb`; G11 scans the decimal form too.

Four rows fewer than the previous revision: `tonal-bg-hover`, `bad-bg-hover`, `bad-border-btn`/`bad-border-notice` and `text-table-header` left this table on **2026-08-29** when audit FE-7 promoted them to `:root` — they are now in the § Surface / text roles table above under their live property names.

### Elevation used outside `--shadow`

| Name | Value (light) | Value (dark) | Live variable | Declared at |
| --- | --- | --- | --- | --- |
| shadow-primary-hover | `0 8px 20px rgba(15,91,215,0.35)` | *(not shipped)* | `.btn.primary:hover{box-shadow:…}` | `src/FE/src/styles.scss` |
| shadow-btn-hover | `0 3px 10px rgba(23,39,67,0.1)` | *(not shipped)* | `.btn:hover{box-shadow:…}` | `src/FE/src/styles.scss` |
| shadow-panel | `0 16px 40px rgba(23,39,67,0.22)` | *(not shipped)* | `.filter-panel{box-shadow:…}` — the toolbar filter dropdown | `src/FE/src/styles.scss` |
| shadow-dialog | `0 24px 70px rgba(0,0,0,0.25)` | *(not shipped)* | `dialog{box-shadow:…}` | `src/FE/src/styles.scss` |
| shadow-toast | `0 14px 38px rgba(23,39,67,0.26), 0 2px 6px rgba(23,39,67,0.12)` | *(not shipped)* | `--shadow-toast` — `.toast-item{box-shadow:…}`; two layers, deliberately deeper than `--shadow`: a toast floats over content, a card sits in it | `src/FE/src/styles.scss` § `--shadow-toast` (promoted 2026-09-03 with the three surfaces above) |
| shadow-focus-ring | `0 0 0 3px rgba(15,91,215,0.12)` | *(not shipped)* | every input `:focus-visible`; other controls use `outline: 2px solid var(--brand)` | `src/FE/src/styles.scss` |
| shadow-focus-ring-invalid | `0 0 0 3px rgba(160,43,43,0.14)` | *(not shipped)* | `.input.invalid:focus-visible` | `src/FE/src/styles.scss` |

## Chart Palette

📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Four role names, restored **2026-09-05**, consumed by exactly one component: [`../Components/TrendChart.md`](../Components/TrendChart.md).

| Role | Resolves to | Value | Chart element |
| --- | --- | --- | --- |
| chart-series-1 | `brand` | `#0f5bd7` | the series line, and the point fill |
| chart-series-1-fill | `brand` at **12%** alpha | `rgba(15,91,215,0.12)` | the area under the line |
| chart-axis-label | `muted` | `#4c576b` | tick labels on **both** axes |
| chart-grid | `line` | `#7a97bd` | grid lines on the **y** axis only; the x axis draws none |

**None of these four is a CSS custom property, and none should become one.** They are *role names* over three base tokens already in `:root`. A `<canvas>` cannot resolve `var(--x)`, so the component reads `--brand` / `--muted` / `--line` once through `getComputedStyle` and passes literal strings to the chart library — the indirection is the whole reason the roles exist. Verify they are absent from the stylesheet rather than trusting this paragraph:

```bash
grep -c 'chart' src/FE/src/styles.scss     # PASS = 0
```

**One series, no categorical palette.** There is no `chart-series-2`, and none should be invented: the design has a single line. A second series is a design decision, not a token gap.

> ### This line has now been flipped **twice** — read the whole history before flipping it again.
>
> | When | What this section said | Why |
> | --- | --- | --- |
> | before 2026-08-29 | the four roles, with values | a `TrendChart` (PrimeNG `p-chart type="line"` over `chart.js`) shipped and consumed them |
> | 2026-08-29 | `None — app has no charts` | the DTI module was removed, taking the only consumer with it. Correct at the time: `find src/FE/src -iname '*trend*'` and `grep -rni chart src/FE/src` both returned nothing |
> | 2026-09-04 | unchanged, but the note under it went stale | `chart.js` was dropped from `src/FE/package.json` the same day, for the same reason |
> | **2026-09-05** | the four roles again (above) | **decision Q17 — the product owner asked for the chart back**, rendered by `p-chart` as before. This is a product decision reversing a cleanup, not a correction of an error: the 2026-08-29 removal was right about the code as it then stood |
>
> The earlier revision of this note also claimed *"`chart.js` remains in `src/FE/package.json` as an **unused dependency**"*. That stopped being true on 2026-08-29+6 days — it was removed 2026-09-04, and the removal is recorded in that file's own `//dependencies` block. Re-adding it is now a prerequisite of building the chart, and it is **not** something this file can do: `src/` is out of scope for the design area (`doc/Design/CLAUDE.md` § Scope). Tracked in [`../Components/TrendChart.md`](../Components/TrendChart.md).
>
> The lesson worth keeping: *a package in `package.json` is not evidence of a shipped chart, and its absence is not evidence that no chart is wanted.* Read the component spec, not the manifest.

## Drift — 2026-08-22 extraction → 2026-08-29 rewrite

**Values changed (same token, new number).** Eight, all computed from the WCAG contrast formula:

| Token | Value before | Value now | Why |
| --- | --- | --- | --- |
| bg | `#eef2f8` | `#cfdaea` | card lifted off the page: 1.12:1 → 1.41:1 |
| muted | `#57647a` | `#4c576b` | darker to hold AA against the darker page: was 4.24:1 on the new `bg`, now 5.16:1 |
| line | `#dfe6ef` | `#7a97bd` | component boundary 1.26:1 → **3.00:1** on `card`, clearing WCAG 2.2 SC 1.4.11 |
| border-strong | `#7e91b4` | `#6077a2` | kept one clearly darker step than the new `line`: 4.51:1 |
| surface-2 | `#e1e7f1` | `#c1cde2` | icon-button hover fill now readable at 1.60:1 on `card` |
| tonal-bg | `#dbe7fa` | `#c4d8f6` | secondary-button fill, 1.45:1 on `card`; the boundary itself moved to the `line` border |
| surface-track | `#edf1f6` | `#dbe4f0` | progress fill vs track now 4.68:1 |
| surface-table-header | `#f8fafc` | `#e9eff6` | zebra stripe 1.05:1 → 1.16:1 (was effectively invisible) |

**Left `:root`, then came back — the round trip, closed 2026-08-29.** The stylesheet rewrite earlier that day pushed four values out of `:root` and back into selectors; audit **FE-7**, later the same day, promoted them again under new names. Both halves are recorded because the intermediate state is what the previous revision of this file describes, and someone reading it needs to know which end they are at.

| Former token | Out of `:root` (rewrite) | Back in `:root` as (audit FE-7) |
| --- | --- | --- |
| `--text-table-header` | literal `#536076` in `th` | **`--th-ink`** (`src/FE/src/styles.scss` § `--th-ink`) |
| `--tonal-bg-hover` | literal `#c7dbf5` in `.btn:hover` | **`--btn-hover-bg`** (`src/FE/src/styles.scss` § `--btn-hover-bg`) |
| `--bad-bg-hover` | literal `#f5c6c6` in `.btn.danger:hover` | **`--danger-hover-bg`** (`src/FE/src/styles.scss` § `--danger-hover-bg`) |
| `--bad-border` | literal, **split in two** — `#e0a8a8` on `.btn.danger`, `#e5a8a8` on `.login-error` | **`--danger-border`** (`src/FE/src/styles.scss` § `--danger-border`), one value again at `#e0a8a8`; `#e5a8a8` no longer appears anywhere in the stylesheet (`grep -c e5a8a8 src/FE/src/styles.scss` → 0, checked 2026-08-29) |
| `--surface-notice` (`#edf4ff`) | gone; `.notice` now fills with `var(--tonal-bg)` | not restored — the value has no consumer |
| `--border-notice` (`#cfe0ff`) | gone; `.notice` now borders with `var(--line)` | not restored — the value has no consumer |

**Removed entirely (the consumer no longer existed):** the four `chart-*` roles (`chart-series-1`, `chart-series-1-fill`, `chart-axis-label`, `chart-grid`). ⬅️ **Restored 2026-09-05** by decision Q17 — see § Chart Palette, which carries the full flip history. The row stays here because the 2026-08-29 removal was correct at the time and the reasoning is worth keeping.

**Added:** `shadow-panel` and `shadow-focus-ring-invalid` (both new selectors in the 2026-08-29 stylesheet), and the four promoted properties `btn-hover-bg` / `danger-border` / `danger-hover-bg` / `th-ink` (see the round-trip table above).

**Renamed 2026-08-29 — update any spec that still uses the left column.** These are token *names*, not values, so a stale reference resolves to nothing rather than to a wrong colour:

| Old spec name | Live property name now |
| --- | --- |
| `tonal-bg-hover` | `btn-hover-bg` |
| `bad-bg-hover` | `danger-hover-bg` |
| `bad-border-btn` **and** `bad-border-notice` | `danger-border` (one token) |
| `text-table-header` | `th-ink` |

**Unchanged:** `card`, `tonal-ink`, `text`, `brand`, `brand2`, `good`, `good-bg`, `warn`, `warn-bg`, `bad`, `bad-bg`, `shadow`, `on-primary`, and every literal in the topbar / sidebar / backdrop group.

## Resolved — no longer open

Items move here from § Normalize on redesign when the **code** changed — never when only the wording did. Count them by reading the list; do not quote a number.

1. ~~**Four `bad`-family reds where two would do** — `#e0a8a8` (button border) and `#e5a8a8` (notice border) differ by one hex digit on the same semantic role.~~ — **fixed in the source 2026-08-29** by audit FE-7. Both edges now read the single `--danger-border` custom property (`src/FE/src/styles.scss` § `--danger-border`), and `#e5a8a8` appears nowhere in the stylesheet any more (`grep -c e5a8a8 src/FE/src/styles.scss` → 0, checked 2026-08-29). The `bad` family is now `--bad`, `--bad-bg`, `--danger-hover-bg`, `--danger-border` — four names, four distinct jobs.
2. ~~**Eight colors ship as literals inside selectors**, four of them values a previous stylesheet had already promoted to `:root`.~~ — **half fixed 2026-08-29, closed 2026-09-03.** Audit FE-7 promoted `#c7dbf5`, `#f5c6c6`, `#e0a8a8` and `#536076` back into `:root` as `--btn-hover-bg`, `--danger-hover-bg`, `--danger-border` and `--th-ink`. The remaining topbar / backdrop / nav-active alpha group went in on **2026-09-03** as `--surface-topbar`, `--overlay-backdrop` and `--surface-nav-active` (`src/FE/src/styles.scss` § `--surface-nav-active` … `--surface-topbar`), together with `--shadow-toast` (§ `--shadow-toast`).
3. ~~**`overlay-backdrop` `rgba(20,28,40,0.45)` is declared twice**~~ — **fixed 2026-09-03.** `dialog::backdrop` (`src/FE/src/styles.scss` § `dialog::backdrop`) and `.sidebar-backdrop` (`src/FE/src/app/shared/components/sidebar/sidebar.scss:211`) now both read `var(--overlay-backdrop)`; the value exists once.

**Why these two closed together, and what now keeps them closed.** Both were invisible to gate G1, which only greps `#rrggbb` — the same decision written in decimal walked straight past it. Gate **G11** (`scripts/fe-gate.sh`) closes that hole: no `rgb()`/`rgba()` literal is allowed in the SCSS of `core/`, `shared/` or `platform/`, with one syntax-only exemption for `rgb(var(--x) / a)`. It is deliberately **not** exempt by value — `rgba(0,0,0,.5)` looks harmless and is still a design decision that belongs in this file. Verified red-then-green on 2026-09-03: red listed exactly the four lines above, green after the promotion.

## Normalize on redesign

1. **Both focus rings are alpha-composited brand/bad** written as literals. A `--brand-rgb` / `--bad-rgb` channel triplet would let them be written as `rgb(var(--brand-rgb) / 8%)` and stay in step with the base color.
2. **`--card` is declared as `#fff`** while every other color in `:root` is 6-digit. Cosmetic, but every consumer of this file has to normalise it.
3. **`line` measures 2.13:1 against `bg`.** Boundaries drawn on the page background rather than on a card — the `.toolbar` border is the live case — do not reach the 3:1 the token was chosen for. Either accept it (the toolbar also changes fill to separate itself) or introduce a second boundary color for on-`bg` use.
4. **The chart library has to come back before the chart can.** ~~`chart.js` is an unused dependency in `src/FE/package.json`; removing it prevents the next reader from concluding that a chart ships.~~ That advice was taken on **2026-09-04** and reversed by decision **Q17** on **2026-09-05**, one day later. `src/FE/package.json` § `dependencies` currently lists no chart package while § Chart Palette above declares four chart roles — a gap that is deliberate and visible rather than hidden, because the design area may not edit `src/`. The next reader should conclude from § Chart Palette, not from the manifest. Owned by [`../Components/TrendChart.md`](../Components/TrendChart.md).

## Appendix: tokens.json rules

- Format: W3C DTCG — every token is an object with `$type` and `$value`.
- Top-level sets: `global` (theme-invariant) plus `light` and `dark` (theme overrides only).
- Figma import via Tokens Studio: enable `global` + exactly ONE theme set at a time — never both themes together. In this project `light` holds the app's single shipped palette and `dark` is an intentionally empty set — do not enable a theme set that does not exist in the shipped app.

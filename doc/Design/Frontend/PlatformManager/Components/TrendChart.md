---
kind: luat
scope: du-an
verified: khong-ap-dung
project: "PlatformManager"
status: "target — not built"
updated: "2026-09-05"
component: "TrendChart"
sources:
  - "doc/Design/Frontend/PlatformManager/Prototypes/index.html"
---

# TrendChart

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** No chart component exists in `src/FE`:
>
> ```bash
> grep -rni 'chart' src/FE/src        # PASS for the claim above = zero hits
> ```
>
> **The two decisions that used to block this file are now answered** (decision
> Q17, 2026-09-05 — the product owner asked for the chart back, rendered by
> PrimeNG's `p-chart` as before):
>
> | Was open | Now |
> | --- | --- |
> | Which renderer | **`p-chart` over `chart.js`**, the same pairing as the pre-2026-08-29 build |
> | Whether the four `chart-*` token roles return | **Yes.** Declared again in [`../Tokens/colors.md`](../Tokens/colors.md) § Chart Palette and in `Tokens/tokens.json`, with the full flip history kept in that section |
>
> **One prerequisite remains, and it is not a decision — it is work in `src/`.**
> `chart.js` was dropped from `src/FE/package.json` on 2026-09-04 (recorded in that
> file's own `//dependencies` block) because this component was its only consumer.
> It has to be re-added before the chart can be built. The design area may not edit
> `src/` (`doc/Design/CLAUDE.md` § Scope), so that is a hand-off, tracked in
> § Handoff below rather than as an open question.

**Description:** The app's only chart — a single-series line plotting overall DTI
progress across the saved periods, mounted inside a `Card` and lazy-loaded so its
code does not sit in the initial bundle. It is a dumb component: points in, no
outputs, no service, no click handling, no selection.

Retired 2026-08-29 with the screen that hosted it; restored 2026-09-05 because the
approved prototype puts it back on the rebuilt Dashboard.

> **Citation policy.** Style values cite
> `doc/Design/Frontend/PlatformManager/Prototypes/index.html` by **selector name**;
> geometry and axis copy cite `Prototype/index.html` § `#screen-dashboard`.

## Anatomy

`.chart-wrap` → **either** the chart canvas **or** a one-sentence `.muted` empty
state. Nothing else.

- **`.chart-wrap`** — `flex: 1`, flex-centred on both axes, `width: 100%`,
  `min-height: 220px` (§ `app-trend-chart .chart-wrap`). It paints **no surface of
  its own**: no background, border, radius or shadow. The white surface behind it
  is the host [`Card.md`](./Card.md), and the `.title` row above it belongs to the
  card too.
- **The plot** — sized `height: 220px` × `width: 100%`, so the chart fills the
  card's width and keeps a constant height at every breakpoint.
- **Empty state** — one `.muted` sentence; the plot is not rendered at all. Gate:
  at least one period must carry a value.
- **Deferred placeholder** — `.chart-skeleton`, flex-centred at the same 220px, is
  shown while the lazy chunk downloads. It belongs to the **host page**, not to
  this component, and it is shared with the history panel.

### Painting rules, as drawn in the approved prototype

| Element | Treatment | Design token | Anchor |
| --- | --- | --- | --- |
| Series line | 2px stroke, no fill | `colors.chart-series-1` | § `.proto-chart-line` |
| Series points | radius 4, 1.5px stroke in the card colour | `colors.chart-series-1` on `colors.card` | § `.proto-chart-point circle` |
| Area under the line | fill at **12% opacity**, no stroke | `colors.chart-series-1-fill` | § `.proto-chart-area` |
| Grid lines, **y only** | 1px | `colors.chart-grid` | § `.proto-chart-grid line` |
| Grid lines, x | **not drawn** | — | — |
| Tick labels, both axes | 11px | `colors.chart-axis-label` | § `.proto-chart-axis-y text`, § `.proto-chart-axis-x text` |
| Legend | **not drawn** — one series needs none | — | — |

The four `chart-*` names are **roles, not CSS custom properties** — see
[`../Tokens/colors.md`](../Tokens/colors.md) § Chart Palette, which is their master
and which records what each resolves to. Do not restate the resolution here.

The y axis is pinned to `[0, 100]` with a `%` tick suffix — `100%` / `75%` / `50%`
/ `25%` / `0%` — so two periods are always visually comparable and a run of high
values does not silently rescale the axis.

The line is drawn **straight-segment**, not smoothed. A missing period keeps its
slot on the x axis and breaks the line rather than being dropped: a gap in the
data must read as a gap, not as a straight run between its two neighbours.

⚠️ **The `.proto-*` selectors above are prototype scaffolding**, a static SVG
standing in for a real chart renderer. Their geometry is the approved *design*;
their implementation is not a shipped decision. See § Cần chốt.

### The x axis changed on 2026-09-05

Decision Q12 replaced the period codes on the x axis with **date ranges**:

| Mode | X-axis labels | Approved values |
| --- | --- | --- |
| Week | date range per tick | `06/07 – 12/07` · `13/07 – 19/07` · `20/07 – 26/07` · `27/07 – 02/08` · `03/08 – 09/08` · `10/08 – 16/08` |
| Month | month abbreviation, **not** a date range — decision T7 | `Th.1` … `Th.12` |

The month mode deliberately keeps the short form: twelve date-range labels do not
fit across one card width. A month's range is read from the period selector and the
`Kỳ đang xem` label instead.

**Settled 2026-09-05 by decision T7**, which was the last open question on this
component. What T7 closed is whether the month axis should be widened to carry date
ranges like the week axis does; the answer is no. The asymmetry it leaves behind is
real and stays on the § Normalize on redesign list as item 5 — a reader switching
modes still gets no cue that the axis changed kind.

## Variants

| Variant | Classes | Key values | When to use |
| --- | --- | --- | --- |
| Line chart, with data | `.chart-wrap` → the plot | single series; line and points `colors.chart-series-1`, area `colors.chart-series-1-fill`, y-grid `colors.chart-grid`, ticks `colors.chart-axis-label`; y pinned `[0,100]` with `%`; straight segments, point radius 4 | Any period selection returning at least one value |
| Empty | `.chart-wrap` → `<p class="muted">` | ink `colors.muted`, `fontSize.fs-xs`; no plot rendered | Every period in range is empty |
| Deferred placeholder | `.chart-skeleton muted` (host page) | ink `colors.muted`, flex-centred, 220px reserved | While the lazy chunk downloads — this is about *code*, not data |

There is **no** variant of the chart itself: no bar, area or donut type, no compact
or sparkline size, no dark treatment, no second series.

## States
<!-- Exactly these five rows, in this order — treatments as rendered by the shipped CSS. -->

| State | Treatment |
| --- | --- |
| default | Plot painted with the palette above at 220px × 100% inside a flex-centred `.chart-wrap`. The empty variant instead renders one `colors.muted` sentence |
| hover | **No design-owned treatment.** No `:hover` rule exists on `.chart-wrap` or on any `.proto-*` selector, and no tooltip surface is specified. Whatever renderer is chosen will supply its own tooltip in its own palette — the one surface on this screen painted outside the token layer. See § Normalize on redesign |
| focus | **Not applicable — nothing here can take focus.** The plot has no `tabindex` and the empty state is a `<p>`. The component contributes zero tab stops, so the data is unreachable by keyboard. An accessible *name* on the plot does not change this |
| active | **Not applicable — there is no selection model.** Points in, nothing out; no click handler, no "current period" marker on the line |
| disabled | **Not applicable — not a control.** The two conditions that would justify a dimmed look are handled by swapping content instead: no data swaps in the `.muted` sentence, code-not-loaded swaps in the host's `.chart-skeleton` |

## Tokens Used

- `colors.chart-series-1` — series line and point fill
- `colors.chart-series-1-fill` — the area under the line
- `colors.chart-axis-label` — tick labels, both axes
- `colors.chart-grid` — y grid
- `colors.card` — the point stroke, so points read as discs punched out of the card
- `colors.muted` + `fontSize.fs-xs` via `.muted` — empty state and placeholder copy
- **No** radius, border, background, shadow or spacing token — `.chart-wrap` declares none, and the surface belongs to the host `Card`

The four `chart-*` roles were removed from the token layer on 2026-08-29 with this
component and **restored 2026-09-05** by decision Q17. They are declared in
[`../Tokens/colors.md`](../Tokens/colors.md) § Chart Palette and in
`Tokens/tokens.json`; that section is their master and carries the reason the line
has now been flipped twice.

Un-tokenised literals: the `220px` height (which appears in **two** places, the
plot and `.chart-wrap`'s `min-height`, plus a third in the host page's
`.chart-skeleton`), the `11px` tick size, the 2px line stroke and the point
radius 4. The 12% fill opacity is **not** in this list — it is carried by
`colors.chart-series-1-fill`, which is exactly what that role exists for.

## Reference markup

```html
<!-- Prototype/index.html § #screen-dashboard — the host card and the defer boundary -->
<div class="card">
  <div class="title">
    <h2>Biểu đồ tiến độ hàng tuần</h2>
    <span class="muted">Tiến độ chung</span>
  </div>
  <app-trend-chart>
    <div class="chart-wrap">
      <!-- the plot: 220px tall, 100% wide, role="img" with a describing label -->
    </div>
  </app-trend-chart>
</div>
```

Verbatim copy: card heading `Biểu đồ tiến độ hàng tuần` · card caption
`Tiến độ chung` · plot accessible label
`Biểu đồ đường tiến độ chung theo tuần, từ tuần 28 đến tuần 33 năm 2026`. These are the
Vietnamese renderings, not markup: the app translates through `@ngx-translate/core` v18
(`src/FE/public/i18n/{vi,en}.json`), so each needs a key with an English sibling, and the
accessible label needs a parameterised key because it carries the week numbers and the year.
Inlining any of them fails `scripts/fe-gate.sh` § G12. (Corrected 2026-09-08; the previous
sentence read *"All hardcoded Vietnamese — there is no i18n layer"*.)

Sources: `doc/Design/Frontend/PlatformManager/Prototypes/index.html`
(§ `app-trend-chart .chart-wrap`, § `#screen-dashboard .chart-skeleton`, and the
prototype-only block § `.proto-chart`, § `.proto-chart-grid line`,
§ `.proto-chart-axis-y text`, § `.proto-chart-axis-x text`, § `.proto-chart-area`,
§ `.proto-chart-line`, § `.proto-chart-point circle`),
`Prototype/index.html` § `#screen-dashboard` → `app-trend-chart` (the approved
geometry, the six date-range x labels and the accessible label),
`src/FE/package.json` § `//dependencies` (the 2026-09-04 note recording that the
chart library was dropped — the removal decision Q17 reverses),
[`../Tokens/colors.md`](../Tokens/colors.md) § Chart Palette (master of the four
`chart-*` roles and of the flip history).

## Do / Don't

- ✅ Keep every chart colour flowing from a token. A canvas renderer cannot resolve `var(--x)`, so whatever is chosen must read the custom properties once and pass literal strings in — that indirection is the entire reason the `chart-*` role names existed.
- ✅ Mount it inside a `Card` with a `.title` row. The chart supplies no heading, surface or padding of its own.
- ✅ Keep it behind a lazy boundary with a placeholder that reserves the full 220px, so the card does not jump when the chunk lands.
- ✅ Handle "no data" by swapping in the `.muted` sentence, never by rendering an empty pair of axes.
- ✅ Keep the y axis pinned to `[0, 100]`. An auto-scaled axis makes a 2-point movement look like a cliff.
- ❌ Don't add a second series or a categorical palette. There is one series, and no series-2 colour is defined anywhere in the token layer.
- ❌ Don't add a legend; one hidden series needs none.
- ❌ Don't drop empty periods from the axis. Removing the label removes the gap, and the chart then asserts a continuity that does not exist — the precise failure the straight-line-with-a-break rule prevents.
- ❌ Don't claim `.chart-skeleton` as this component's class; it belongs to the host page and is shared with the history panel.

## Normalize on redesign

1. **Hover is whatever the renderer supplies.** No tooltip surface, text colour, radius or padding is specified, so the chart's one interactive affordance will render in a third-party palette that no token controls and that changes on library upgrade.
2. **No keyboard access and no data-table alternative.** An accessible name on the plot is not access — a sighted keyboard user still cannot reach a data point. Providing the series as a visually-hidden `<table>` closes this and item 1 at once, since a table needs no tooltip.
3. **The 220px height is three unlinked literals** — the plot, `.chart-wrap`'s `min-height` and the host page's `.chart-skeleton`. Two live in a different file from the third, so changing the chart height means editing all three or the placeholder stops matching the chart.
4. **Sizing is fixed, not fluid.** A hard 220px means the chart is the same height on a 390px phone and a 1600px desktop while every card around it reflows. There is no responsive height step.
5. **The x axis uses two vocabularies** — date ranges in week mode, month abbreviations in month mode (§ Anatomy). Defensible on space grounds, but a reader switching modes gets no cue that the axis changed kind.
6. **`flex: 1` sits on the child, not the host.** Defect **A2** — `doc/Design/Frontend/PlatformManager/Prototypes/index.html` § `app-trend-chart .chart-wrap` declares `flex: 1`, and the host declares no `display: flex`, so the wrapper does not actually stretch to the card's height.

## Handoff — one prerequisite outside this folder

**`chart.js` must be re-added to `src/FE/package.json`** before this component can
be built. It was removed on 2026-09-04 when this was its only consumer, and
decision Q17 reversed that on 2026-09-05. The design area may not edit `src/`
(`doc/Design/CLAUDE.md` § Scope), so this is a hand-off to whoever builds the
screen, not an open question — the decision is made.

## Cần chốt

<!-- Open questions this spec must not answer on its own. Raised 2026-09-05, emptied the same day. -->

**Nothing is open on this component.** The last item — the month-mode x-axis labels —
was settled by decision T7 on 2026-09-05. What remains is not a question but a task
in a folder this one may not edit: § Handoff above.

### Answered elsewhere — do not re-ask

| Was open here | Answer | Owner |
| --- | --- | --- |
| Month-mode x-axis labels | **`Th.1 … Th.12`, not date ranges** — decision T7, 2026-09-05 | § The x axis changed on 2026-09-05 |
| What renders the chart | `p-chart` over `chart.js`, as before — decision Q17, 2026-09-05 | § banner above |
| Whether the four `chart-*` roles return | Yes — decision Q17 | [`../Tokens/colors.md`](../Tokens/colors.md) § Chart Palette |
| Whether the chart follows the detail table's filters | **No.** The line always shows overall progress for the period; the table's filters change only the table | `spec/dashboard-dti/business-rules.md` |

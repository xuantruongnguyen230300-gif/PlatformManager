---
project: "PlatformManager"
status: "draft"
updated: "2026-08-22"
component: "TrendChart"
sources:
  - "src/FE/src/app/modules/dashboard/components/trend-chart/trend-chart.ts"
  - "src/FE/src/app/modules/dashboard/components/trend-chart/trend-chart.html"
  - "src/FE/src/app/modules/dashboard/components/trend-chart/trend-chart.scss"
  - "src/FE/src/app/modules/dashboard/pages/dashboard/dashboard.page.html"
  - "src/FE/src/app/modules/dashboard/pages/dashboard/dashboard.page.scss"
  - "src/FE/src/app/modules/dashboard/models/dashboard.model.ts"
---

# TrendChart
**Description:** `<app-trend-chart>` — the app's **only** chart: a PrimeNG `p-chart type="line"` (`primeng` 20.2 on `chart.js` 4.5) that plots overall DTI progress across the saved periods, wrapped in a centring `.chart-wrap` box and lazy-loaded by the host page behind `@defer (on viewport)`. It is a dumb component: one `input.required<ITrendPoint[]>()`, **no** `output()`, no service, no click handling.

## Anatomy

`.chart-wrap` → **either** `<p-chart type="line">` **or** a one-sentence `.muted` empty state. Nothing else — the component template is thirteen lines.

- **`.chart-wrap`** — `flex: 1`, `display:flex` centred on both axes, `width: 100%`, `min-height: dimension.chart-height`. It paints no surface of its own: no background, border, radius or shadow. The white surface behind it is the host `Card`, and the `.title` row above it belongs to the card too.
- **`<p-chart>`** — sized by an inline `[style]` object, `height: dimension.chart-height` × `width: 100%`, and by `responsive: true` + `maintainAspectRatio: false` in the options, so the canvas fills the card's width and keeps a constant height at every breakpoint.
- **Empty state** — `<p class="muted">` carrying one sentence; the canvas is not rendered at all. Gate: `hasData()` = "at least one point has a non-null `Value`".
- **Deferred placeholder** — `.chart-skeleton.muted` (same `min-height: dimension.chart-height`, flex-centred) shown while the lazy chunk downloads. It lives in the **host page's** stylesheet, not this component's, and the same class is reused for the history panel's placeholder.

**Data shape and painting rules, as shipped.** `ITrendPoint` is `{ Label: string; Value: number | null }`. `chartData` maps **every** point the API returns — labels come from the unfiltered array, and a point with no value is passed through as `null` while the rest are clamped to `[0, 100]`. Combined with `spanGaps: false`, a period with no data **keeps its slot on the x axis and breaks the line**, so the axis reads as a continuous run of periods with a visible hole. `tension: 0` (straight segments), `pointRadius: 4`, `fill: true`. The y axis is pinned `min: 0` / `max: 100` with a `%` tick suffix.

> **Changed 2026-08-22.** Until then `chartData` filtered nulls out *before* building `labels`, which deleted the missing period from the axis entirely: its two neighbours were drawn adjacent and joined by a solid segment, asserting a continuity that did not exist. The same filter also made `spanGaps: false` dead configuration, since no null ever reached Chart.js. Regression-tested in `trend-chart.spec.ts`.

**Colour resolution — the load-bearing mechanism.** Chart.js draws to a 2D canvas, which cannot resolve `var(--x)` the way CSS can. `readCssVar()` therefore reads **exactly three** custom properties once via `getComputedStyle(document.documentElement)` and passes literal strings into the dataset and scale options:

| Chart role | Reads | Design token | Chart.js option |
| --- | --- | --- | --- |
| Series line | `--brand` | `colors.chart-series-1` | `datasets[0].borderColor` |
| Series points | `--brand` | `colors.chart-series-1` | `datasets[0].pointBackgroundColor` |
| Area under the line | `--brand` @ **12%** alpha via `hexToRgba()` | `colors.chart-series-1-fill` | `datasets[0].backgroundColor` |
| Tick labels, **both** axes | `--muted` | `colors.chart-axis-label` | `scales.x.ticks.color`, `scales.y.ticks.color` |
| Grid lines, **y only** | `--line` | `colors.chart-grid` | `scales.y.grid.color` |
| Grid lines, x | — | — | `scales.x.grid.display: false` |
| Legend | — | — | `plugins.legend.display: false` |

**One dataset, no categorical palette.** There is no series 2, so there is no second chart colour to specify and none exists in `Tokens/colors.md` or `DESIGN.md`.

**Recorded constraint — colours cannot change at runtime.** `trend-chart.ts:52-54` states it in the code: the three values are read once at construction *because* there is no dark mode and no theme switch to react to. Under SSR (`document` absent) the same three values are supplied as hardcoded hex fallbacks. Any redesign that introduces a theme switch must also make this component re-resolve; today it deliberately does not.

## Variants

| Variant | Classes | Key values | When to use |
| --- | --- | --- | --- |
| Line chart, with data | `.chart-wrap` → `<p-chart type="line">` | Single dataset; line + points `colors.chart-series-1`, area `colors.chart-series-1-fill`, ticks `colors.chart-axis-label`, y-grid `colors.chart-grid`; y `[0,100]` with `%` suffix; `pointRadius: 4`, `tension: 0`, `fill: true` | Any period selection that returns at least one non-null `Value` |
| Empty | `.chart-wrap` → `<p class="muted">` | `colors.muted`, `typography.muted-caption`; canvas not rendered | Every `Trend` point is `null`, or `Trend` is empty (the dashboard's initial `EMPTY_AGGREGATE`) |
| Deferred placeholder | `.chart-skeleton muted` (host page) | `colors.muted`, `typography.muted-caption`, flex-centred at `dimension.chart-height` | While `@defer (on viewport)` downloads the lazy chunk — about *code*, not data |

There is **no** variant of the chart itself: no bar/area/donut type, no compact or sparkline size, no dark treatment, no second series. `p-chart`'s `type` is hardcoded to `"line"` in the template.

## States
<!-- Exactly these five rows, in this order — treatments as rendered by the shipped CSS. -->

`trend-chart.scss` is eight lines of layout and contains **no** pseudo-class rule; `chartOptions` configures only `responsive`, `maintainAspectRatio`, `plugins.legend` and `scales`. The rows below record that precisely rather than calling it "not styled".

| State | Treatment |
| --- | --- |
| default | Canvas painted with the palette above, at `dimension.chart-height` × 100% inside a flex-centred `.chart-wrap`. Empty variant instead renders one `colors.muted` sentence at `typography.muted-caption` |
| hover | **No app-authored treatment.** No `:hover` rule exists in `trend-chart.scss` and `chartOptions` sets **no** `plugins.tooltip`, `interaction` or `hover*` option — so Chart.js's own library defaults apply: a nearest-point tooltip in the library's default dark surface, and the library's default point hover radius. That tooltip is the one surface in the app painted entirely outside the token layer; see § Normalize on redesign |
| focus-visible | **Not applicable — nothing here can take focus.** PrimeNG renders the chart as a `<canvas>` with no `tabindex`, the component sets none, and neither `trend-chart.scss` nor `styles.scss` authors a focus rule that could match it. The empty state is a `<p>`. The component therefore contributes zero stops to the tab order, and the data is unreachable by keyboard. Note this is **not** fixed by the `ariaLabel` added 2026-08-22: that gives the canvas a *name* for screen readers, which is a different thing from being reachable — a sighted keyboard user still cannot get to any data point. See § Normalize on redesign #3 |
| active / selected | **Not applicable — the chart has no selection model.** `points` is a one-way `input.required`, the component declares **no** `output()`, `chartOptions` configures no `onClick`, and PrimeNG's own `(onDataSelect)` is not bound. Clicking a point does nothing; no point, series or period is ever marked as current |
| disabled | **Not applicable — not a control.** Nothing can be disabled: there is no `[disabled]` binding possible on a chart, and the app never dims it. The two conditions that *would* justify a disabled look are handled by swapping content instead — no data swaps the canvas for the `.muted` sentence, and code-not-loaded-yet swaps it for the host page's `.chart-skeleton` |

## Tokens Used
- `colors.chart-series-1` (`--brand`), `colors.chart-series-1-fill` (`--brand` at 12% alpha), `colors.chart-axis-label` (`--muted`), `colors.chart-grid` (`--line`) — the four names already carried in `DESIGN.md` § Chart Palette and `Tokens/colors.md` § Chart Palette
- `colors.muted` + `typography.muted-caption` — via the global `.muted` class, used by both the empty state and the `@defer` placeholder
- `dimension.chart-height` (`Tokens/spacing.md` § Structural Measurements) — a **named measurement, not a CSS custom property**: it ships as three separate literals (`p-chart` inline `[style]`, `.chart-wrap` `min-height`, `.chart-skeleton` `min-height`)
- No radius, border, background, shadow or spacing token — `.chart-wrap` declares none, and the surface belongs to the host `Card`
- Motion: none authored. Chart.js's default animation runs on data change; there is no motion scale to reference (`Tokens/spacing.md` § Motion)
- Icons: none

## Reference markup

```html
<!-- trend-chart.html — the whole template -->
<div class="chart-wrap">
  @if (hasData()) {
    <p-chart type="line" [data]="chartData()" [options]="chartOptions()" [style]="{ height: '220px', width: '100%' }" />
  } @else {
    <p class="muted">Chưa có đủ dữ liệu để vẽ biểu đồ.</p>
  }
</div>
```

```html
<!-- dashboard.page.html — how it is mounted: inside a Card, behind @defer (on viewport) -->
<div class="card">
  <div class="title">
    <h2>Biểu đồ tiến độ hàng {{ viewMode() === 'month' ? 'tháng' : 'tuần' }}</h2>
    <span class="muted">{{ isAllMode() ? 'Tổng hợp năm ' + selectedYear() : 'Tiến độ chung' }}</span>
  </div>
  @defer (on viewport) {
    <app-trend-chart [points]="aggregate().Trend" />
  } @placeholder {
    <div class="chart-skeleton muted">Đang tải biểu đồ…</div>
  }
</div>
```

```ts
// trend-chart.ts — the palette contract, reproduced because it is the component's design surface
borderColor: this.colors.brand,                     // colors.chart-series-1
backgroundColor: hexToRgba(this.colors.brand, 0.12) // colors.chart-series-1-fill
pointBackgroundColor: this.colors.brand,            // colors.chart-series-1
plugins: { legend: { display: false } },
scales: {
  y: { min: 0, max: 100, ticks: { color: this.colors.muted, … }, grid: { color: this.colors.line } },
  x: { ticks: { color: this.colors.muted }, grid: { display: false } },
}
```

Verbatim copy: empty state `Chưa có đủ dữ liệu để vẽ biểu đồ.` · placeholder `Đang tải biểu đồ…` (host page). Both are hardcoded Vietnamese in the templates — there is no i18n layer.

Sources: `src/FE/src/app/modules/dashboard/components/trend-chart/trend-chart.html:1-13`, `trend-chart.scss:1-8`, `trend-chart.ts:13-16` (`readCssVar`), `:18-24` (`hexToRgba`), `:31-36` (the "no interpolation of missing points" decision), `:39-44` (inputs + `hasData`), `:46-55` (three-token resolution + the no-theme-switch constraint), `:57-74` (`chartData`), `:76-92` (`chartOptions`), `src/FE/src/app/modules/dashboard/pages/dashboard/dashboard.page.html:28-38` (host card + `@defer`), `dashboard.page.scss:21-26` (`.chart-skeleton`), `src/FE/src/app/modules/dashboard/models/dashboard.model.ts:95-98` (`ITrendPoint`), `src/FE/package.json:34,36` (`chart.js` 4.5, `primeng` 20.2), `src/FE/src/styles.scss` — `--brand`, `--muted`, `--line` in `:root`

## Do / Don't

- ✅ Keep every chart colour flowing from a token through `readCssVar()`. Hardcoding a hex in the dataset is exactly what this indirection exists to prevent, and it is the reason the four `chart-*` names exist in `DESIGN.md`.
- ✅ Mount it inside a `Card` with a `.title` row — the chart supplies no heading, surface or padding of its own.
- ✅ Keep it behind `@defer (on viewport)` with a `.chart-skeleton` placeholder that reserves `dimension.chart-height`, so the card does not jump when the chunk lands.
- ✅ Handle "no data" by swapping in the `.muted` sentence, never by rendering an empty axis pair.
- ❌ Don't add a second dataset or a categorical palette — there is one series, and no series-2 colour is defined anywhere in the token layer.
- ❌ Don't add a legend; it is explicitly disabled, and one hidden series needs none.
- ❌ Don't reach for `.chart-skeleton` as this component's own class — it belongs to the host page and is shared with the history panel.

## Normalize on redesign
1. **The palette exists twice.** `trend-chart.ts:55-61` reads `--brand` / `--muted` / `--line` at runtime **and** repeats the same three hex values as SSR fallbacks, so a token change in `styles.scss` silently desynchronises the server-rendered first paint. Either derive the fallbacks from the token layer or drop them now that the app builds with `--ssr=false`. Also logged in `Tokens/colors.md` § Normalize #2.
2. **`hexToRgba()` assumes a 6-digit hex.** It slices character pairs out of the string, so a 3-digit value or an `rgb()`/`hsl()` token would produce `rgba(NaN, NaN, NaN, 0.12)` and a fill that silently disappears. `--brand` is 6-digit today, and as of 2026-08-22 **so is every colour in `:root`** — `--card` was the one shorthand (`#fff`) and was normalised to `#ffffff` during the contrast pass, so no 3-digit value remains for this to trip over. The fragility is unchanged though: nothing stops the next shorthand from being added, and the failure is silent (a fill that quietly disappears, not an error).
3. **~~The chart has no accessible name~~ — FIXED 2026-08-22.** PrimeNG 20 renders the canvas as `role="img"` with an optional `ariaLabel` input, and `role="img"` discards all child content — so an unbound `ariaLabel` shipped as an image with **no name at all**, which a screen reader announces as an empty region. `trend-chart.html` now binds `[ariaLabel]="chartAriaLabel()"`, a computed that states the chart type, how many periods carry data, how many are missing, and the first and last labels with their values (`trend-chart.ts` § `chartAriaLabel`). Covered by `trend-chart.spec.ts` § "canvas phải có tên cho trình đọc màn hình".
   **Still open:** no `tabindex`, no data-table alternative. A name is not keyboard access — a sighted keyboard user still cannot reach any data point. Providing the series as a visually-hidden `<table>` would close both this and item 4's tooltip dependency.
4. **Hover is entirely Chart.js's default.** No tooltip surface, text colour, radius or padding is configured, so the one interactive affordance on the chart renders in a third-party palette that no token controls and that will change when the library is upgraded.
5. **`dimension.chart-height` is three unlinked literals** — the `p-chart` inline `[style]`, `.chart-wrap { min-height }` and `.chart-skeleton { min-height }`. Two of them live in different files from the third; changing the chart height means editing all three or the placeholder stops matching the chart.
6. **~~`spanGaps: false` is dead configuration~~ — FIXED 2026-08-22.** The pre-filter was dropped, not the option: `chartData` now maps **every** point the API returns, keeping the label on the x axis and putting `null` in the data array where `Value` is null, so `spanGaps: false` has something real to act on and the line breaks at the hole.
   Why this was worth fixing rather than recording: filtering nulls removed the *label* too, so a missing period vanished from the axis and its two neighbours were drawn adjacent and joined by a solid segment. The chart asserted a continuity that did not exist — the precise failure `spanGaps: false` is meant to prevent, produced by the code that also disabled it. Covered by `trend-chart.spec.ts` § "kỳ thiếu số liệu phải GIỮ CHỖ trên trục x", including a control case proving fully-populated series gain no spurious nulls.
7. **Sizing is fixed, not fluid.** `maintainAspectRatio: false` plus a hard `220px` means the chart is the same height on a 390px phone and a 1600px desktop, while every card around it reflows. There is no responsive height step.

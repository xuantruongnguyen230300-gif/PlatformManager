---
project: "PlatformManager"
status: "draft"
updated: "2026-08-22"
component: "KpiTile"
sources: ["src/FE/src/app/modules/dashboard/components/kpi-tile/kpi-tile.html", "src/FE/src/app/modules/dashboard/components/kpi-tile/kpi-tile.scss", "src/FE/src/app/modules/dashboard/components/kpi-tile/kpi-tile.ts", "src/FE/src/app/modules/dashboard/components/kpi-summary/kpi-summary.html"]
---

# KpiTile
**Description:** Label / value / sub-caption stat tile — a `.card` (see `Card.md`) with the `.kpi` modifier and a fixed three-line internal anatomy. Shipped as a real Angular component, `app-kpi-tile` (`kpi-tile.ts:16-21`), a dumb presentational component with four signal inputs and no business logic. It renders exactly five times, inside `KpiSummary` (`kpi-summary.html:1-7`).

## Anatomy
`.card.kpi` → `.label` (small caption, `typography.kpi-label`, `colors.muted`) → `.value` (the hero number, 21px/850, `margin-top: spacing.sp-1`) → `.sub` (small caption, `typography.kpi-label`, `colors.muted`, `line-height:1.4`, `min-height:30px`). No icon, no chart, no trend sparkline inside the tile.

`.sub` is conditional in the template (`@if (sub())`, `kpi-tile.html:4-6`) but its `min-height:30px` reserves two lines' worth of space so the five tiles keep a common baseline even when captions wrap unevenly. The host is `display:contents` (`kpi-tile.scss:1-3`) so the `.card` participates directly in the parent's five-column grid rather than nesting inside a wrapper element.

## Variants

Tone is a typed input — `KpiTone = 'default' | 'good' | 'warn' | 'bad'` (`kpi-tile.ts:3`) — bound to `.value` via `[class]="tone()"`. It colours **only the value**, never the label, sub or card.

| Variant | Classes | Key values | When to use |
| --- | --- | --- | --- |
| Default tone | `.value` (class literally `default`, which matches no CSS rule) | inherits `colors.text` | `Tiến độ chung…` and `Hoàn thành 100%` — passed no `tone` (`kpi-summary.html:2,6`) |
| Good tone | `.value.good` | text `colors.good` (`kpi-tile.scss:16-18`) | `Chỉ tiêu tăng`, always (`kpi-summary.html:4`); and the delta tile when its computed `deltaTone()` is good |
| Warn tone | `.value.warn` | text `colors.warn` (`kpi-tile.scss:20-22`) | `Không tăng`, always (`kpi-summary.html:5`) |
| Bad tone | `.value.bad` | text `colors.bad` (`kpi-tile.scss:24-26`) | The delta tile when `deltaTone()` is bad |
| Without sub-caption | `.sub` not rendered | `@if (sub())` false — the input defaults to `''` (`kpi-tile.ts:19`) | No shipped instance uses it; all five pass a `sub`. Reachable but currently unexercised |

**The five shipped tiles**, in order (`kpi-summary.html:2-6`):

| # | Label | Tone | Sub-caption |
| --- | --- | --- | --- |
| 1 | computed `progressLabel()` | default | `Bình quân gia quyền theo điểm` |
| 2 | computed `deltaLabel()` | computed `deltaTone()` | computed `prevSub()` |
| 3 | `Chỉ tiêu tăng` | `good` | `Có tiến bộ so với kỳ trước` |
| 4 | `Không tăng` | `warn` | `Cần chú ý theo dõi` |
| 5 | `Hoàn thành 100%` | default | `Số chỉ tiêu đạt đủ tiến độ` |

Tiles 3 and 4 carry a **fixed** tone regardless of value — `Chỉ tiêu tăng` is always green and `Không tăng` always amber, even at zero. Only tile 2 changes colour with its data.

## States
<!-- Exactly these five rows, in this order — treatments as rendered by the shipped CSS. -->

| State | Treatment |
| --- | --- |
| default | `.label` `typography.kpi-label` / `colors.muted`; `.value` 21px / 850 (18px below the 560px breakpoint, `kpi-tile.scss:38-42`) tinted per tone; `.sub` `typography.kpi-label` / `colors.muted` / `min-height:30px`. Card box inherited from `Card.md` |
| hover | **Not styled** — inherits `.card`, which has no `:hover` rule; the tile is static content, never a link or filter |
| focus | **N/A** — no `tabindex`, no focusable descendant |
| active | **N/A** — not interactive |
| disabled | **N/A** — not a form control |

## Tokens Used
- `colors.text`, `colors.muted`, `colors.good`, `colors.warn`, `colors.bad`
- `colors.card`, `colors.line`, `rounded.lg`, `spacing.card-padding`, `shadow` — all inherited from `Card.md`
- `spacing.sp-1` (label→value and value→sub gaps)
- `typography.kpi-value`, `typography.kpi-label`

`font-size:21px` (and the 18px mobile step) and `font-weight:850` are literals at `kpi-tile.scss:12-13,40` — both sit off the type scale, which tops out at `--fs-lg` (15px) and has no weight tokens. `min-height:30px` on `.sub` is likewise a literal.

## Reference markup

```html
<!-- component template -->
<div class="card kpi">
  <div class="label">{{ label() }}</div>
  <div class="value" [class]="tone()">{{ value() }}</div>
  @if (sub()) {
    <div class="sub">{{ sub() }}</div>
  }
</div>

<!-- the five shipped call sites -->
<section class="kpis">
  <app-kpi-tile [label]="progressLabel()" [value]="progressValue()" sub="Bình quân gia quyền theo điểm" />
  <app-kpi-tile [label]="deltaLabel()" [value]="deltaValue()" [tone]="deltaTone()" [sub]="prevSub()" />
  <app-kpi-tile label="Chỉ tiêu tăng" [value]="kpi().Up + ''" tone="good" sub="Có tiến bộ so với kỳ trước" />
  <app-kpi-tile label="Không tăng" [value]="kpi().Flat + ''" tone="warn" sub="Cần chú ý theo dõi" />
  <app-kpi-tile label="Hoàn thành 100%" [value]="doneFraction()" sub="Số chỉ tiêu đạt đủ tiến độ" />
</section>
```

Sources: `src/FE/src/app/modules/dashboard/components/kpi-tile/kpi-tile.html:1-7`, `kpi-tile.scss:1-42`, `kpi-tile.ts:3,16-21` (inputs + `KpiTone`), `src/FE/src/app/modules/dashboard/components/kpi-summary/kpi-summary.html:1-7` (five instances), `kpi-summary.scss:1-21` (grid + responsive)

## Do / Don't

- ✅ Keep the fixed three-part anatomy (label → value → sub); no shipped tile varies it.
- ✅ Pass `value` as a pre-formatted **string** — the component takes `input.required<string>()` and does no formatting, so callers coerce numbers themselves (`kpi().Up + ''`).
- ✅ Colour the value only. Tinting the label or card is not supported by any rule.
- ✅ Keep `.sub`'s reserved height so a five-tile row stays baseline-aligned when one caption wraps.
- ❌ Don't add icons, sparklines or secondary values inside a tile — none exists.
- ❌ Don't make a tile clickable; the KPI row is a read-out, not a filter control.
- ❌ Don't pass `tone="default"` expecting a rule — it emits `class="default"`, which nothing styles; that is how the neutral case works.

## Normalize on redesign
1. `tone="default"` renders a real but meaningless class. Either add a `.default` rule or map the neutral case to no class.
2. The value's `21px`/`850` and mobile `18px` are off both the type scale and the (non-existent) weight scale — see `Tokens/typography.md`.
3. The mobile rule `.kpis ::ng-deep .card:last-child { grid-column: 1/-1 }` (`kpi-summary.scss:18-20`) is a layout artefact of five items on two columns, and it reaches through `::ng-deep` into the child component's card. An adaptive grid, or a modifier input on the tile, would avoid piercing encapsulation.
4. Tiles 3 and 4 hardcode `good`/`warn` regardless of value, so `Chỉ tiêu tăng: 0` still reads green. Either derive the tone from the value or drop the colour on those two.

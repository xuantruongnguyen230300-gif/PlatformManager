---
kind: luat
scope: du-an
verified: khong-ap-dung
project: "PlatformManager"
status: "target — not built"
updated: "2026-09-05"
component: "KpiTile"
sources:
  - "doc/Design/Frontend/PlatformManager/Prototypes/index.html"
---

# KpiTile

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** No `.kpi` rule and no `app-kpi-tile` element
> exist in `src/FE` today: the dashboard module that owned them was removed on
> 2026-08-29 and the rebuild has not started. Nothing below describes running
> code. Confirm that rather than trusting this sentence:
>
> ```bash
> grep -rn 'app-kpi-tile' src/FE/src
> grep -rn 'class="[^"]*kpi' src/FE/src --include='*.html'
> ```
>
> PASS for the claim above = zero hits from **both**. When hits appear, re-anchor
> every value in this file to the shipped file and change `verified:`.
>
> Use those two, not a bare `grep -rn 'kpi'` — an earlier revision of this banner
> did exactly that and claimed zero hits, while the command actually returns one:
> a **comment** in `src/FE/src/app/platform/trang-chu/pages/trang-chu/trang-chu.page.scss`
> that mentions `.kpis` in passing. A self-check that fails on its own file is
> worse than none, because the next reader stops trusting the banner rather than
> the command.

**Description:** Label / value / sub-caption stat tile — a `Card` with the `.kpi`
modifier and a fixed three-line internal anatomy. It is the unit the dashboard's
KPI row is made of, rendered five times inside one `.kpis` grid.

This spec was retired on 2026-08-29 with the screen that hosted it and is restored
on 2026-09-05 because the rebuilt Dashboard composes it again. **It is a restore,
not a redesign**: every value below is one already drawn in the approved
prototype, not a new proposal.

> **Citation policy.** Style values cite
> `doc/Design/Frontend/PlatformManager/Prototypes/index.html` plus the **selector
> name** — never a line number (`doc/Design/CLAUDE.md` § Neo trích dẫn vào
> `styles.scss`, same reason: a 5000-line file renumbers silently). Markup and
> copy cite `Prototype/index.html` § `#screen-dashboard`, the copy of that
> prototype the product owner approved point by point on 2026-09-04 / 2026-09-05.

## Anatomy

`.card.kpi` → `.label` → `.value` → `.sub`. No icon, no sparkline, no secondary
number inside the tile.

| Part | Treatment | Anchor |
| --- | --- | --- |
| host `app-kpi-tile` | `display: contents`, so the `.card` participates directly in the parent's five-column grid instead of nesting inside a wrapper box | § `app-kpi-tile` |
| `.label` | `fontSize.fs-xs`, ink `colors.muted` | § `app-kpi-tile .kpi .label` |
| `.value` | `21px` / weight `850`, `margin-top: spacing.sp-1`; tinted by tone | § `app-kpi-tile .kpi .value` |
| `.sub` | `fontSize.fs-xs`, ink `colors.muted`, `line-height: 1.4`, `min-height: 30px` | § `app-kpi-tile .kpi .sub` |

`.sub`'s reserved `min-height: 30px` is what keeps five tiles baseline-aligned when
one caption wraps to two lines and the others do not. The card box itself —
surface, border, radius, shadow, padding — is inherited from
[`Card.md`](./Card.md) and is not re-declared here.

The row container is a sibling concern: `.kpis` is a `repeat(5, 1fr)` grid with a
`spacing.sp-4` gap (§ `app-kpi-summary .kpis`).

## Variants

Tone colours **only the value**, never the label, the sub-caption or the card.

| Variant | Classes | Key values | When to use |
| --- | --- | --- | --- |
| Default tone | `card kpi` → `.value` with no tone class | value inherits `colors.text` | A figure that carries no judgement — the overall-progress tile and the completed-count tile |
| Good tone | `.value.good` | ink `colors.good` (§ `app-kpi-tile .kpi .value.good`) | A positive movement |
| Warn tone | `.value.warn` | ink `colors.warn` (§ `app-kpi-tile .kpi .value.warn`) | A figure that needs watching but is not a failure |
| Bad tone | `.value.bad` | ink `colors.bad` (§ `app-kpi-tile .kpi .value.bad`) | A negative movement |
| Without sub-caption | `.sub` not rendered | the tile loses its reserved 30px | No tile in the approved prototype omits it; the branch exists but is unexercised |

### The five tiles of the rebuilt Dashboard

Copy taken verbatim from `Prototype/index.html` § `#screen-dashboard` →
`app-kpi-summary`. The figures are the real ones measured from
`spec/DTI_CanGiuoc_2026-08-11.csv`.

| # | Label | Value shown | Tone | Sub-caption |
| --- | --- | --- | --- | --- |
| 1 | `Tiến độ chung tuần này` | `82,1%` | default | `Bình quân gia quyền theo điểm (thật: 787,84/960)` |
| 2 | `So với tuần trước` | `↑ 2,3 đ.%` | good | `Tuần 32/2026 (03/08–09/08/2026)` |
| 3 | `Chỉ tiêu tăng` | `18` | good | `Có tiến bộ so với kỳ trước` |
| 4 | `Không tăng` | `27` | warn | `Cần chú ý theo dõi` |
| 5 | `Hoàn thành` | `26/62` | default | `Số chỉ tiêu ở trạng thái Hoàn thành (thật)` |

⚠️ **The `(thật …)` parentheticals are prototype provenance annotations**, not
necessarily shipping copy — the prototype's own header comment says numbers marked
`(thật)` were computed directly from the BA's CSV, to separate them from the
illustrative ones. Whether they ship is listed under § Cần chốt.

Tiles 3 and 4 carry a **fixed** tone regardless of value, so `Chỉ tiêu tăng: 0`
still renders green. That was true before the retirement and is unchanged in the
approved prototype; it is carried in § Normalize on redesign rather than quietly
corrected here.

## States
<!-- Exactly these five rows, in this order — treatments as rendered by the shipped CSS. -->

| State | Treatment |
| --- | --- |
| default | `.label` `fontSize.fs-xs` / `colors.muted`; `.value` 21px / 850 tinted per tone (18px below 560px, § `@media (max-width: 560px)` → `app-kpi-tile .kpi .value`); `.sub` `fontSize.fs-xs` / `colors.muted` / `min-height: 30px`. Card box from `Card.md` |
| hover | **Not styled** — inherits `.card`, which authors no `:hover`. The tile is a read-out, never a link or a filter |
| focus | **Not applicable** — no `tabindex`, no focusable descendant |
| active | **Not applicable** — not interactive |
| disabled | **Not applicable** — not a form control |

## Tokens Used

- `colors.text`, `colors.muted`, `colors.good`, `colors.warn`, `colors.bad`
- `colors.card`, `colors.line`, `rounded.lg`, `shadow`, card padding — all inherited from `Card.md`
- `spacing.sp-1` (label → value and value → sub gaps), `spacing.sp-4` (grid gap on `.kpis`)
- `fontSize.fs-xs` (label and sub)

Un-tokenised literals, carried forward unchanged from the retired spec:
`font-size: 21px` and its `18px` mobile step, `font-weight: 850`, and
`min-height: 30px` on `.sub`. All three sit off the scales recorded in
[`../Tokens/typography.md`](../Tokens/typography.md) — the type scale tops out at
`--fs-lg` and the app declares no weight tokens at all.

## Reference markup

```html
<!-- Prototype/index.html § #screen-dashboard → app-kpi-summary -->
<section class="kpis">
  <app-kpi-tile>
    <div class="card kpi">
      <div class="label">Tiến độ chung tuần này</div>
      <div class="value">82,1%</div>
      <div class="sub">Bình quân gia quyền theo điểm (thật: 787,84/960)</div>
    </div>
  </app-kpi-tile>
  <app-kpi-tile>
    <div class="card kpi">
      <div class="label">So với tuần trước</div>
      <div class="value good">↑ 2,3 đ.%</div>
      <div class="sub">Tuần 32/2026 (03/08–09/08/2026)</div>
    </div>
  </app-kpi-tile>
  <!-- three more tiles: Chỉ tiêu tăng · Không tăng · Hoàn thành -->
</section>
```

Sources: `doc/Design/Frontend/PlatformManager/Prototypes/index.html`
(§ `app-kpi-tile`, § `app-kpi-tile .kpi .label`, § `.value`, § `.value.good`,
§ `.value.warn`, § `.value.bad`, § `.sub`, § `app-kpi-summary .kpis`, and the two
`@media` blocks at 980px and 560px), `Prototype/index.html` § `#screen-dashboard`
→ `app-kpi-summary` (the five approved tiles and their copy),
`spec/DTI_CanGiuoc_2026-08-11.csv` (the figures), [`Card.md`](./Card.md) (the
inherited box).

## Do / Don't

- ✅ Keep the fixed three-part anatomy — label → value → sub. No approved tile varies it.
- ✅ Pass `value` as a **pre-formatted string**. The tile does no number formatting, so `82,1%`, `↑ 2,3 đ.%` and `26/62` are all produced by the caller and the Vietnamese decimal comma is decided in one place.
- ✅ Colour the value only; no rule tints the label, the sub or the card.
- ✅ Keep `.sub`'s reserved height so a five-tile row stays baseline-aligned when one caption wraps.
- ❌ Don't add an icon, a sparkline or a second figure inside a tile — none exists in the approved design.
- ❌ Don't make a tile clickable. The KPI row is a read-out; filtering happens in the `Toolbar` below it.
- ❌ Don't re-declare the card surface inside `.kpi`. `.kpi` is a modifier on `Card`, and the moment it sets its own background or radius there are two card definitions again.

## Normalize on redesign

1. **Tiles 3 and 4 hardcode their tone.** `Chỉ tiêu tăng` is always `good` and `Không tăng` always `warn`, so a zero renders green and a zero renders amber. Derive the tone from the value, or drop the colour on those two.
2. **`21px` / `850` / `18px` / `30px` are off every scale** (`../Tokens/typography.md`, `../Tokens/spacing.md`). The weight in particular is synthetic — no loaded face is 850, so it rounds up and renders identically to 800.
3. **The mobile last-child rule reaches through the host.** Because `app-kpi-tile` is `display: contents`, `app-kpi-summary .kpis .card:last-child { grid-column: 1 / -1 }` matches **every** tile, not the fifth — so below 560px all five span the full width instead of "four in two columns plus one full-width". Recorded here as-drawn rather than silently corrected. Anchor: `doc/Design/Frontend/PlatformManager/Prototypes/index.html` § `app-kpi-tile` (`display: contents`) and § `app-kpi-summary .kpis .card:last-child`, inside the `@media (max-width: 560px)` block — the prototype carries its own comment above that rule naming the same consequence. (The working note this used to cite, `Prototype/GHI-CHU-CAN-SUA.md`, is a private file outside the repo; its defect ids `A1`…`A6` are kept here only as labels.)
4. **The `(thật …)` annotations.** See § Cần chốt — if they ship, the sub-caption is carrying data provenance, which is a different job from captioning a number.

## Cần chốt

<!-- Open questions this spec must not answer on its own. Raised 2026-09-05. -->

1. **Do the `(thật: 787,84/960)` and `(thật)` parentheticals ship?** They read as prototype annotations marking which figures came from the BA's real CSV. If they ship, tile 1's caption runs long and will wrap past the reserved 30px on a narrow column.
2. **Does tile 5 count `Hoàn thành` by the manually-chosen `Trạng thái` value, or by `Tiến độ % = 100`?** The label changed from `Hoàn thành 100%` (progress-derived, pre-retirement) to `Hoàn thành` (status-derived, per decision Q4) — the two definitions give different numbers on the same data. Owned by `spec/dashboard-dti/business-rules.md`.
3. **Tiles 3 and 4 depend on a period-over-period comparison** that no longer has a column in the detail table (decision Q8 removed `Tăng/giảm`). Whether these two tiles survive is open item 5 of the 2026-09-05 brief.

---
kind: luat
scope: du-an
verified: 2026-09-10
project: "PlatformManager"
status: "built 2026-09-09 in src/FE — no call site on any page yet"
updated: "2026-09-10"
component: "KpiTile"
sources:
  - "src/FE/src/app/modules/dashboard/components/kpi-tile/kpi-tile.ts"
  - "src/FE/src/app/modules/dashboard/components/kpi-tile/kpi-tile.html"
  - "src/FE/src/app/modules/dashboard/components/kpi-tile/kpi-tile.scss"
  - "doc/Design/Frontend/PlatformManager/Prototypes/index.html"
---

# KpiTile

> ✅ **CÓ THẬT — dựng 2026-09-09, đối chiếu 2026-09-10.** `app-kpi-tile` exists at
> `src/FE/src/app/modules/dashboard/components/kpi-tile/`. It landed in the **feature
> module**, not in `shared/`: only the Dashboard composes it, and
> `doc/huong_dan/quy-uoc/fe-architecture.md` § Bên trong `shared/` keeps a
> one-feature component out of the shared layer.
>
> | What | Where it shipped |
> | --- | --- |
> | `display: contents` on the host | `src/FE/src/app/modules/dashboard/components/kpi-tile/kpi-tile.scss:8-10` |
> | `.label` / `.value` / `.sub` treatments | `src/FE/src/app/modules/dashboard/components/kpi-tile/kpi-tile.scss:12-48` |
> | The four value tones, bound as `[class.good]` / `[class.warn]` / `[class.bad]` | `src/FE/src/app/modules/dashboard/components/kpi-tile/kpi-tile.html:7-12` |
> | The `18px` value size below 560px | `src/FE/src/app/modules/dashboard/components/kpi-tile/kpi-tile.scss:50-54` |
>
> ```bash
> grep -rn 'app-kpi-tile' src/FE/src --include='*.ts'    # PASS = at least one hit
> grep -c '\.kpi' src/FE/src/styles.scss                 # PASS = 0 — still component-scoped
> ```
>
> 🛑 **Built is not composed.** No page template renders `<app-kpi-tile>`, so the
> five-across `.kpis` grid this tile is the unit of **does not exist anywhere in
> `src/FE`** — it belongs to the host page, and the Dashboard page is still a stub
> (`src/FE/src/app/modules/dashboard/pages/dashboard/dashboard.page.html:1-10`) and
> `DASHBOARD_ROUTES` is deliberately left out of `app.routes.ts`
> (`src/FE/src/app/modules/dashboard/dashboard.routes.ts:8-13`).
>
> The banner this replaces set **PASS = zero hits** for the same two greps. The
> first criterion has flipped. Its warning still stands: use the two commands above
> rather than a bare `grep -rn 'kpi'`, which also returns a passing mention of
> `.kpis` in a comment in
> `src/FE/src/app/platform/trang-chu/pages/trang-chu/trang-chu.page.scss:10`.

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
> copy cite `doc/Design/Frontend/PlatformManager/Prototypes/index.html` § `#screen-dashboard`, the copy of that
> prototype the product owner approved point by point on 2026-09-04 / 2026-09-05.

## Anatomy

`.card.kpi` → `.label` → `.value` → `.sub`. No icon, no sparkline, no secondary
number inside the tile.

| Part | Treatment | Anchor |
| --- | --- | --- |
| host `app-kpi-tile` | `display: contents`, so the `.card` participates directly in the parent's five-column grid instead of nesting inside a wrapper box | § `app-kpi-tile` |
| `.label` | `fontSize.fs-xs`, ink `colors.muted` | § `app-kpi-tile .kpi .label` |
| `.value` | `21px` / weight `850`, `margin-top: spacing.sp-1`; tinted by tone | § `app-kpi-tile .kpi .value` |
| `.sub` | `fontSize.fs-xs`, ink `colors.muted`, `margin-top: spacing.sp-1`, `line-height: 1.4`, `min-height: 30px` | § `app-kpi-tile .kpi .sub`; as built, `src/FE/src/app/modules/dashboard/components/kpi-tile/kpi-tile.scss:41-47` |

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

Copy taken verbatim from `doc/Design/Frontend/PlatformManager/Prototypes/index.html` § `#screen-dashboard` →
`app-kpi-summary`, **except tile 1's sub-caption, corrected 2026-09-09** — see the
⚠️ below the table. The figures are the prototype's own and illustrate a
**populated** period; they are not what an import produces. They were computed from
the BA's August 2026 spreadsheet, which is deliberately outside this repo (root `.gitignore`), so it is named rather than cited as a path. The in-repo sample `spec/danh-muc-dti/dti-mau-an-danh-62-dong.csv`
is a **different** 62-row dataset and reproduces none of them — do not "verify" these
five tiles against it.

| # | Label | Value shown | Tone | Sub-caption |
| --- | --- | --- | --- | --- |
| 1 | `Tiến độ chung tuần này` | `82,1%` | default | `Bình quân Tiến độ %, gia quyền theo Điểm tối đa` |
| 2 | `So với tuần trước` | `↑ 2,3 đ.%` | good | `Tuần 32/2026 (03/08–09/08/2026)` |
| 3 | `Chỉ tiêu tăng` | `18` | good | `Có tiến bộ so với kỳ trước` |
| 4 | `Không tăng` | `27` | warn | `Cần chú ý theo dõi` |
| 5 | `Hoàn thành` | `26/62` | default | `Số chỉ tiêu ở trạng thái Hoàn thành` |

> ### ⚠️ CHỐT 2026-09-09 — the `(thật …)` parentheticals do NOT ship, and tile 1's caption was wrong about itself
>
> **They do not ship.** `(thật: 787,84/960)` and `(thật)` were prototype provenance
> annotations: the prototype's header comment used `(thật)` to mark the numbers taken
> straight from the source spreadsheet, separating them from the illustrative ones.
> That is a note to the people reviewing a prototype, not a caption for a user — a
> sub-caption captions a number, it does not carry data provenance (§ Normalize #4).
>
> **The prototype now agrees — 2026-09-10.** It was merged with the reviewer's
> out-of-repo working copy and anonymised in the same pass: both parentheticals are
> gone, tile 1 carries the corrected caption, and the header comment says the figures
> are illustrative instead of marking which ones were real. Check rather than trust:
>
> ```bash
> grep -cE 'class="sub">[^<]*\(thật' doc/Design/Frontend/PlatformManager/Prototypes/index.html   # PASS = 0
> grep -c 'class="sub">Bình quân Tiến độ %, gia quyền theo Điểm tối đa' doc/Design/Frontend/PlatformManager/Prototypes/index.html   # PASS = 1
> ```
>
> **Tile 1's caption had to change as well, and that is the part that was actively
> misleading.** `Bình quân gia quyền theo điểm` describes a ratio of **scores**. The
> tile does not compute that: `kpi.overallProgress` is the mean of **`Tiến độ %`**
> weighted by `Điểm tối đa` (`spec/dashboard-dti/business-rules.md` §1.1), and since
> decision **Q24** left `Tiến độ %` blank on import the two quantities have **no
> relationship at all**. The caption now names the quantity the tile actually shows.
>
> **What that means for the value beside it.** `82,1%` is the score ratio, so it
> illustrates a period someone has already filled in. Right after an import tile 1
> reads `—` (decision T12, [`../Screens/01-dashboard.md`](../Screens/01-dashboard.md)
> § States), and the master file warns in as many words against using a score figure
> as the expected value of a Dashboard test.
>
> Tile 5's caption was already correct and keeps its wording — only the `(thật)` is
> gone.

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
<!-- doc/Design/Frontend/PlatformManager/Prototypes/index.html § #screen-dashboard → app-kpi-summary -->
<section class="kpis">
  <app-kpi-tile>
    <div class="card kpi">
      <div class="label">Tiến độ chung tuần này</div>
      <div class="value">82,1%</div>
      <div class="sub">Bình quân Tiến độ %, gia quyền theo Điểm tối đa</div>
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
`@media` blocks at 980px and 560px), `doc/Design/Frontend/PlatformManager/Prototypes/index.html` § `#screen-dashboard`
→ `app-kpi-summary` (the five approved tiles and their copy),
the BA's August 2026 spreadsheet for where the figures were computed — outside this
repo (root `.gitignore`), so read the figures off the prototype above —
[`Card.md`](./Card.md) (the inherited box).

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
3. **The mobile last-child rule reaches through the host.** Because `app-kpi-tile` is `display: contents`, `app-kpi-summary .kpis .card:last-child { grid-column: 1 / -1 }` matches **every** tile, not the fifth — so below 560px all five span the full width instead of "four in two columns plus one full-width". Recorded here as-drawn rather than silently corrected. Anchor: `doc/Design/Frontend/PlatformManager/Prototypes/index.html` § `app-kpi-tile` (`display: contents`) and § `app-kpi-summary .kpis .card:last-child`, inside the `@media (max-width: 560px)` block — the prototype carries its own comment above that rule naming the same consequence. (The working note this used to cite is a file under `Prototype/` that the root `.gitignore` keeps out of the repo — `git check-ignore -v Prototype/GHI-CHU-CAN-SUA.md` prints the rule, and nobody but its author can open it; its defect ids `A1`…`A6` are kept here only as labels. Reworded 2026-09-10 so the line carries the check instead of a bare path.)
4. ~~**The `(thật …)` annotations.**~~ **Closed 2026-09-09 — they do not ship** (§ The five tiles). What they raised survives as a rule for any caption written after them: a sub-caption captions a number, it does not carry data provenance.

## Cần chốt

<!-- Open questions this spec must not answer on its own. Raised 2026-09-05; all three closed 2026-09-09. -->

1. ~~**Do the `(thật: 787,84/960)` and `(thật)` parentheticals ship?**~~ **Closed 2026-09-09 — no.** They are prototype provenance annotations, and tile 1's caption was rewritten in the same decision because it named a different quantity than the tile computes. Reasoning and the new strings: § The five tiles.
2. ~~**Does tile 5 count `Hoàn thành` by the manually-chosen `Trạng thái` value, or by `Tiến độ % = 100`?**~~ **Closed 2026-09-09 — by `Trạng thái`.** `kpi.done` counts criteria whose `status` is `Hoàn thành`, **the value the user picked**, and is never derived from a score or a percentage. Owner, with the rest of the counting rules: `spec/dashboard-dti/business-rules.md` §1.4. That file also keeps a separate `progressPercent ≥ 100` threshold for *colour and display* and states that it does **not** change `status` — so the two ideas coexist without this tile having to choose between them.
3. ~~**Tiles 3 and 4 depend on a period-over-period comparison** that no longer has a column in the detail table.~~ **Closed 2026-09-09 — both survive.** `spec/dashboard-dti/business-rules.md` §1.6 is the master table for the five tiles (settled 2026-09-06 as T12) and it lists `Chỉ tiêu tăng` (`kpi.up`) and `Không tăng` (`kpi.flat`) among them. Removing the `Tăng/giảm` **column** from the detail table (Q8) did not remove the **comparison** — it is still computed for the counts, and §1.6 records that both read `0` right after an import rather than `62`.

**No open question remains in this file.**

---
kind: luat
scope: du-an
verified: khong-ap-dung
project: "PlatformManager"
status: "target — not built"
updated: "2026-09-05"
component: "HistoryRow"
sources:
  - "doc/Design/Frontend/PlatformManager/Prototypes/index.html"
---

# HistoryRow

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Neither `.history` nor `.histrow` is declared
> anywhere in `src/FE` — the dashboard module that owned them was removed on
> 2026-08-29. Check rather than trust:
>
> ```bash
> grep -rn 'histrow' src/FE/src
> ```
>
> PASS for the claim above = zero hits. Restored 2026-09-05 as design input for
> the rebuild, with **one deliberate change** to the retired values — see § Anatomy.

**Description:** One saved-period row in the Dashboard's `Lịch sử các kỳ đã lưu`
panel (`.histrow`), rendered once per period, newest first. The panel exists to
make one promise visible: saving a new period does not overwrite an old one.

> **Citation policy.** Style values cite
> `doc/Design/Frontend/PlatformManager/Prototypes/index.html` by **selector name**;
> markup and copy cite `Prototype/index.html` § `#screen-dashboard`.

## Anatomy

`.history` is the scroll container — flex column, gap `spacing.sp-2`,
`max-height: 240px`, `overflow: auto`
(§ `app-history-list .history`). Inside it, one `.histrow` per period: a
**four-column grid**, gap `spacing.sp-3`, `align-items: center`, padding
`spacing.sp-2`, 1px `colors.line` bottom border, `fontSize.fs-xs`
(§ `app-history-list .histrow`).

| Cell | Content | Component |
| --- | --- | --- |
| 1 | The period's **date range**, bold — `10/08 – 16/08/2026` | — plain `<b>` |
| 2 | `Tiến độ chung <b>82,1%</b>` | — plain text |
| 3 | The movement against the previous period, **or** `Kỳ đầu` on the oldest row | [`DeltaIndicator.md`](./DeltaIndicator.md), or `.muted` copy |
| 4 | A default-variant `Xem` button | [`Button.md`](./Button.md) |

### The one value that changes from the retired spec

⚠️ **The grid template must widen.** The retired component used
`100px 1fr 90px 70px`, sized for a single date (`16/08/2026`). Decision Q12 makes
every period display its **full range** — `10/08 – 16/08/2026` — and 100px does
not hold that string. The approved prototype flags this in its own comment as the
**only** style value the date-range decision forces to change.

**Decided 2026-09-05 (T3): the template is `150px 1fr 90px 70px`.** This spec is
the **single** place that value is written. `Screens/01-dashboard.md` and
`spec/dashboard-dti/ui-spec.md` carry a one-line pointer here and no number of
their own — writing it twice is precisely how two sources for one value appear.

## Variants

| Variant | Classes | Key values | When to use |
| --- | --- | --- | --- |
| History row | `histrow` | 4-column grid, gap `spacing.sp-3`, padding `spacing.sp-2`, bottom border 1px `colors.line`, `fontSize.fs-xs`, `align-items: center` | One per saved period in the selected year |
| Oldest row | `histrow` + `.muted` in cell 3 | identical box; cell 3 reads `Kỳ đầu` instead of a delta | The first period on record, which has nothing to compare against |
| No overall figure | `histrow` | identical box; cell 2's number renders `—` | The period exists but carries no overall progress |
| Empty state | — no `.histrow` rendered | one `.muted` sentence instead of the list | The selected **year** has no saved periods — note the copy must stay year-scoped, because the list is filtered by year |

## States
<!-- Exactly these five rows, in this order — treatments as rendered by the shipped CSS. -->

| State | Treatment |
| --- | --- |
| default | Grid as above; bottom border `colors.line`; text `colors.text` at `fontSize.fs-xs`; cell 3 coloured by `DeltaIndicator`'s own variant rules |
| hover | **Not styled at row level** — no `.histrow:hover` rule. Unlike table rows, which do highlight (see [`Table.md`](./Table.md)). The `Xem` button inside has its own hover from [`Button.md`](./Button.md) |
| focus | **Not applicable at row level** — a plain `<div>` with no `tabindex`. Only the `Xem` button is focusable, via `src/FE/src/styles.scss` § `.btn:focus-visible` |
| active | **Not applicable at row level** — the row carries no click handler; only the `Xem` button has `:active` |
| disabled | **Not applicable** — not a form control. The `Xem` button is never disabled in this list |

## Tokens Used

- `colors.line` (row separator), `colors.text`, `colors.muted`
- `spacing.sp-2` (row padding and container gap), `spacing.sp-3` (column gap)
- `fontSize.fs-xs` (row text)
- Everything [`Button.md`](./Button.md) and [`DeltaIndicator.md`](./DeltaIndicator.md) bring with them

Un-tokenised literals: the four-column grid template and the container's
`max-height: 240px`. Both are carried in `../Tokens/spacing.md` among the other
structural literals.

## Reference markup

```html
<!-- Prototype/index.html § #screen-dashboard → app-history-list -->
<app-history-list>
  <div class="history">
    <div class="histrow">
      <b>10/08 – 16/08/2026</b>
      <span>Tiến độ chung <b>82,1%</b></span>
      <app-delta-indicator><span class="delta up">↑ +2,3 đ.%</span></app-delta-indicator>
      <button type="button" class="btn">Xem</button>
    </div>
    <!-- newer to older … -->
    <div class="histrow">
      <b>06/07 – 12/07/2026</b>
      <span>Tiến độ chung <b>68,9%</b></span>
      <span class="muted">Kỳ đầu</span>
      <button type="button" class="btn">Xem</button>
    </div>
  </div>
</app-history-list>
```

Sources: `doc/Design/Frontend/PlatformManager/Prototypes/index.html`
(§ `app-history-list`, § `app-history-list .history`, § `app-history-list .histrow`
— and the comment attached to the last of these recording that the grid template
was copied verbatim from the deleted Angular stylesheet),
`Prototype/index.html` § `#screen-dashboard` → `app-history-list` (the six approved
rows, the date-range format and the `Kỳ đầu` branch),
`src/FE/src/styles.scss` (§ `.btn`, § `.muted` — the live classes the row composes).

## Do / Don't

- ✅ Sort ascending by date to compute each row's movement against its immediate predecessor, **then** reverse for display. Computing on the reversed list inverts every sign.
- ✅ Print `Kỳ đầu` on the oldest row rather than a zero. "Nothing to compare" and "no change" are different facts, and the panel is where the distinction is most visible.
- ✅ Keep the full date range in cell 1 (decision Q12) and widen the column to fit it — a truncated range is worse than the single date it replaced.
- ✅ Keep `Xem` as the only per-row action, so the hit target is deliberate and the row itself stays non-clickable.
- ✅ Keep the empty-state copy year-scoped. A generic "no periods saved" would misreport a year that simply has none while other years do.
- ❌ Don't add edit or delete affordances to a row. The panel is a read-only jump list, and the promise it exists to make is that old periods are not touched.
- ❌ Don't make the whole row clickable without also giving it hover and focus treatments — today it has neither.

## Normalize on redesign

1. **Fixed pixel columns with no responsive override.** `.group-row` in [`ProgressBar.md`](./ProgressBar.md) has two breakpoint variants; this row has none, so it keeps one template from 1440px down to 390px. Widening column 1 for the date range (§ Cần chốt) makes that worse, not better.
2. **No row hover, while the tables directly above it do highlight.** Two similar scan-and-pick lists give opposite feedback on the same screen.
3. **`max-height: 240px` is a literal**, and the scroll area has no fade or shadow indicating more rows below — the panel silently hides periods.
4. **The row's movement figure may be derived client-side.** In the retired implementation this panel recomputed each delta in the browser while the detail table received deltas pre-computed by the server: two sources for one rule. Decide once, in `spec/dashboard-dti/business-rules.md`, before either is built again.

## Cần chốt

<!-- Open questions this spec must not answer on its own. Raised 2026-09-05. -->

1. ~~**The new width of grid column 1.**~~ **Resolved 2026-09-05 (T3) — `150px`.** Recorded once, in § Anatomy above.
2. **Whether the month mode changes cell 1's format.** Q12 fixes the week format; a month period spans `01/08 – 31/08/2026`, which is longer still.

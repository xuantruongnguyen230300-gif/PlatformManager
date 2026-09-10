---
kind: luat
scope: du-an
verified: 2026-09-10
project: "PlatformManager"
status: "built 2026-09-09 in src/FE — no call site on any page yet"
updated: "2026-09-10"
component: "HistoryRow"
sources:
  - "src/FE/src/app/modules/dashboard/components/history-row/history-row.ts"
  - "src/FE/src/app/modules/dashboard/components/history-row/history-row.html"
  - "src/FE/src/app/modules/dashboard/components/history-row/history-row.scss"
  - "doc/Design/Frontend/PlatformManager/Prototypes/index.html"
---

# HistoryRow

> ✅ **CÓ THẬT — dựng 2026-09-09, đối chiếu 2026-09-10.** `app-history-row` exists at
> `src/FE/src/app/modules/dashboard/components/history-row/`, in the feature module
> rather than in `shared/` (`doc/huong_dan/quy-uoc/fe-architecture.md` § Bên trong
> `shared/`) — only the Dashboard has a saved-period row.
>
> **Decision T3 shipped as written**, and `.histrow` sits on the component's own host
> element rather than on a wrapper `<div>`, so the parent flex `gap` survives.
>
> | What | Where it shipped |
> | --- | --- |
> | `.histrow` on the host, with the reason | `src/FE/src/app/modules/dashboard/components/history-row/history-row.ts:53-57` |
> | `150px 1fr 90px 70px`, gap, padding, bottom border, `fs-xs` | `src/FE/src/app/modules/dashboard/components/history-row/history-row.scss:9-17` |
> | The four cells, in order | `src/FE/src/app/modules/dashboard/components/history-row/history-row.html:5-42` |
> | Cell 3 — `Kỳ đầu` on the oldest row, otherwise `DeltaIndicator` | `src/FE/src/app/modules/dashboard/components/history-row/history-row.html:27-31` |
>
> ```bash
> grep -rn 'app-history-row' src/FE/src --include='*.ts'   # PASS = at least one hit
> grep -rn 'histrow' src/FE/src                            # PASS = at least one hit
> ```
>
> 🛑 **Built is not composed, and the panel around it was not built at all.**
> `.history` — the scroll container with `max-height: 240px` described in § Anatomy —
> has **no shipped owner**: only the row exists. No page renders either; the
> Dashboard page is still a stub
> (`src/FE/src/app/modules/dashboard/pages/dashboard/dashboard.page.html:1-10`) and
> `DASHBOARD_ROUTES` is deliberately left out of `app.routes.ts`
> (`src/FE/src/app/modules/dashboard/dashboard.routes.ts:8-13`).
>
> The banner this replaces set `grep -rn 'histrow' src/FE/src` at **zero hits**. That
> has inverted, and the second command above is now the direct check.

**Description:** One saved-period row in the Dashboard's `Lịch sử các kỳ đã lưu`
panel (`.histrow`), rendered once per period, newest first. The panel exists to
make one promise visible: saving a new period does not overwrite an old one.

> **Citation policy.** Style values cite
> `doc/Design/Frontend/PlatformManager/Prototypes/index.html` by **selector name**;
> markup and copy cite `doc/Design/Frontend/PlatformManager/Prototypes/index.html` § `#screen-dashboard`.

## Anatomy

`.history` is the scroll container — flex column, gap `spacing.sp-2`,
`max-height: 240px`, `overflow: auto`
(§ `app-history-list .history`). Inside it, one `.histrow` per period: a
**four-column grid**, gap `spacing.sp-3`, `align-items: center`, padding
`spacing.sp-2`, 1px `colors.line` bottom border, `fontSize.fs-xs`
(§ `app-history-list .histrow`).

| Cell | Content | Component |
| --- | --- | --- |
| 1 | The period's **date range**, bold — `10/08 – 16/08/2026` in week mode, `01/08 – 31/08/2026` in month mode. One format for both, nothing dropped — § The month form of cell 1 | — plain `<b>` |
| 2 | `Tiến độ chung <b>82,1%</b>` | — plain text |
| 3 | The movement against the previous period, **or** `Kỳ đầu` on the oldest row | [`DeltaIndicator.md`](./DeltaIndicator.md), or `.muted` copy |
| 4 | A default-variant `Xem` button. As built it also carries `.no-print` and an `aria-label` naming the period, so the row's one action names *which* period it opens (`src/FE/src/app/modules/dashboard/components/history-row/history-row.html:35-42`) | [`Button.md`](./Button.md) |

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

### The month form of cell 1 — measured 2026-09-09

**Month mode uses the same shape as week mode: `01/08 – 31/08/2026`.** One format for
both, `{dd/MM} – {dd/MM/yyyy}`, with nothing dropped — which is exactly what the
period-label owner already requires of every label
(`spec/dashboard-dti/business-rules.md` §6.1 and §6.2b, where the date range is one of
the two components that may never be lost). **No exception is needed anywhere**, and
none should be registered.

This question stood open for four days on a premise nobody had measured: § Cần chốt
item 2 asserted a month range was *"longer still"* than a week's. Measured, the two are
identical:

| Mode | String | Characters |
| --- | --- | ---: |
| Week — the form already specified for this cell | `10/08 – 16/08/2026` | 18 |
| Month | `01/08 – 31/08/2026` | 18 |

So the 150px column (T3) that comfortably holds one holds the other, and there is no
tight-space problem to design around. Count them rather than trusting the table:

```bash
python -c "print(len('10/08 \u2013 16/08/2026'), len('01/08 \u2013 31/08/2026'))"   # PASS = 18 18
```

> 🔄 **SỬA 2026-09-09.** An earlier revision of this section, written the same day,
> specified a short name form `Tháng 8/2026` and justified it with decision **T7**, the
> trend chart's `Th.1 … Th.12` month axis. **Both halves were wrong, and it is
> withdrawn.** The premise was wrong because the two strings measure the same. The
> precedent was wrong because the chart axis packs twelve labels into one width and
> genuinely runs out of room, while this panel gives every period a row to itself and
> competes with nothing — a crowding argument does not travel from one to the other.
> That revision also carried a warning that it depended on an exception not registered
> in the period-label master; the warning was right, and the correct response turned out
> to be dropping the exception rather than registering it. Kept rather than deleted
> because the failure it names is cheap to repeat: *a spec inherited a comparison it had
> never measured, and four days of an open question rested on it.*

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
<!-- doc/Design/Frontend/PlatformManager/Prototypes/index.html § #screen-dashboard → app-history-list -->
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
`doc/Design/Frontend/PlatformManager/Prototypes/index.html` § `#screen-dashboard` → `app-history-list` (the six approved
rows, the date-range format and the `Kỳ đầu` branch),
`src/FE/src/styles.scss` (§ `.btn`, § `.muted` — the live classes the row composes).

## Do / Don't

- ✅ Sort ascending by date to compute each row's movement against its immediate predecessor, **then** reverse for display. Computing on the reversed list inverts every sign.
- ✅ Print `Kỳ đầu` on the oldest row rather than a zero. "Nothing to compare" and "no change" are different facts, and the panel is where the distinction is most visible.
- ✅ Keep the full date range in cell 1 (decision Q12) and widen the column to fit it — a truncated range is worse than the single date it replaced. **Month mode takes the same form**, and it fits the same column (§ The month form of cell 1).
- ✅ Keep `Xem` as the only per-row action, so the hit target is deliberate and the row itself stays non-clickable.
- ✅ Keep the empty-state copy year-scoped. A generic "no periods saved" would misreport a year that simply has none while other years do.
- ❌ Don't add edit or delete affordances to a row. The panel is a read-only jump list, and the promise it exists to make is that old periods are not touched.
- ❌ Don't make the whole row clickable without also giving it hover and focus treatments — today it has neither.

## Normalize on redesign

1. **Fixed pixel columns with no responsive override.** `.group-row` in [`ProgressBar.md`](./ProgressBar.md) has two breakpoint variants; this row has none, so it keeps one template from 1440px down to 390px. Widening column 1 to 150px for the date range (§ Anatomy) makes that worse, not better.
2. **No row hover, while the tables directly above it do highlight.** Two similar scan-and-pick lists give opposite feedback on the same screen.
3. **`max-height: 240px` is a literal**, and the scroll area has no fade or shadow indicating more rows below — the panel silently hides periods.
4. **The row's movement figure may be derived client-side.** In the retired implementation this panel recomputed each delta in the browser while the detail table received deltas pre-computed by the server: two sources for one rule. Decide once, in `spec/dashboard-dti/business-rules.md`, before either is built again.

## Cần chốt

<!-- Open questions this spec must not answer on its own. Raised 2026-09-05; the last one closed 2026-09-09. -->

1. ~~**The new width of grid column 1.**~~ **Resolved 2026-09-05 (T3) — `150px`.** Recorded once, in § Anatomy above.
2. ~~**Whether the month mode changes cell 1's format.**~~ **Closed 2026-09-09 by measurement — it does not.** Both strings run to **18 characters** (`10/08 – 16/08/2026` and `01/08 – 31/08/2026`), so a month range sits in the same 150px column a week range already does, and cell 1 keeps one format for both modes: `{dd/MM} – {dd/MM/yyyy}`, nothing dropped. The measurement, and the wrong answer it replaces, are in § The month form of cell 1.

**No open question remains in this file**, and nothing is owed to another file — this cell now follows the period-label rules as written, with no exception to register.

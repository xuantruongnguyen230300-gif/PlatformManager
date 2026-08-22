---
project: "PlatformManager"
status: "draft"
updated: "2026-08-22"
component: "HistoryRow"
sources: ["src/FE/src/app/modules/dashboard/components/history-list/history-list.html", "src/FE/src/app/modules/dashboard/components/history-list/history-list.scss", "src/FE/src/app/modules/dashboard/components/history-list/history-list.ts"]
---

# HistoryRow
**Description:** One saved-period row in the dashboard's "Lịch sử các kỳ đã lưu" panel (`.histrow`). Rendered once per period by `HistoryList` (`history-list.html:1-19`), newest first. **Confirmed live** — `grep -rn "histrow" src/FE/src` returns `history-list.html:3` and `history-list.scss:9`.

## Anatomy
`.history` (the scroll container: `flex column`, `gap: spacing.sp-2`, `max-height:240px`, `overflow:auto`) → one `.histrow` per period. Each row is a **4-column grid** (`100px 1fr 90px 70px`, `gap: spacing.sp-3`, `align-items:center`, `spacing.sp-2` padding, 1px `colors.line` bottom border, `typography.muted-caption` size):

1. **Date** — `<b>{{ row.DateLabel }}</b>`, formatted `dd/mm/yyyy` in TypeScript (`history-list.ts:5-8`)
2. **Progress** — `Tiến độ chung <b>…</b>`, the number formatted `vi-VN` to one decimal with a `%` suffix, or `—` when null
3. **Change** — `DeltaIndicator` (see `DeltaIndicator.md`), **or** `<span class="muted">Kỳ đầu</span>` for the oldest row
4. **Action** — a default-variant `Button` labelled `Xem` (see `Button.md`), emitting `view` with the period value

## Variants

| Variant | Classes | Key values | When to use |
| --- | --- | --- | --- |
| History row | `histrow` | grid `100px 1fr 90px 70px`, `gap: spacing.sp-3`, padding `spacing.sp-2`, bottom border 1px `colors.line`, `typography.muted-caption`, `align-items:center` | One per saved period in the selected year |
| First-period row | `histrow` + `.muted` in cell 3 | identical box; cell 3 renders `Kỳ đầu` instead of a `DeltaIndicator` | The oldest row in the list (`row.IsFirst`), which has nothing to compare against |
| Null-progress row | `histrow` | identical box; cell 2's value renders `—` | `row.Progress === null` — the period exists but carries no overall figure |
| Empty state | — (no `.histrow` rendered) | `<div class="muted">Chưa có tuần nào trong năm đang chọn.</div>` (`history-list.html:17`) | `@empty` — the selected **year** has no saved periods. Note the copy is year-scoped, matching the year filter above it |

## States
<!-- Exactly these five rows, in this order — treatments as rendered by the shipped CSS. -->

| State | Treatment |
| --- | --- |
| default | grid layout as above; bottom border `colors.line`; text `colors.text`; the change cell coloured by `DeltaIndicator`'s own variant rules |
| hover | **Not styled** — no `.histrow:hover` rule. (Unlike table rows, which do highlight — see `Table.md`.) The `Xem` `Button` inside has its own hover |
| focus | **N/A at row level** — the row is a plain `<div>` with no `tabindex`. Only the `Xem` `Button` is focusable, using `.btn:focus-visible` (`styles.scss:182-185`) |
| active | **N/A at row level** — the row has no click handler; only the `Xem` `Button` carries `.btn:active` |
| disabled | **N/A** — not a form control. The `Xem` button is never `[disabled]` in this list |

## Tokens Used
- `colors.line`, `colors.text`, `colors.muted`
- `spacing.sp-2` (row padding and container gap), `spacing.sp-3` (column gap)
- `typography.muted-caption` (`--fs-xs`, the row's font size)
- Plus everything `Button` and `DeltaIndicator` bring with them

The grid template `100px 1fr 90px 70px` and the container's `max-height:240px` are literals with no token behind them.

## Reference markup

```html
<div class="history">
  @for (row of rows(); track row.Value) {
    <div class="histrow">
      <b>{{ row.DateLabel }}</b>
      <span>
        Tiến độ chung
        <b>{{ row.Progress === null ? '—' : row.Progress.toLocaleString('vi-VN', { minimumFractionDigits: 1, maximumFractionDigits: 1 }) + '%' }}</b>
      </span>
      @if (row.IsFirst) {
        <span class="muted">Kỳ đầu</span>
      } @else {
        <app-delta-indicator [value]="row.Delta" />
      }
      <button type="button" class="btn" (click)="view.emit(row.Value)">Xem</button>
    </div>
  } @empty {
    <div class="muted">Chưa có tuần nào trong năm đang chọn.</div>
  }
</div>
```

Sources: `src/FE/src/app/modules/dashboard/components/history-list/history-list.html:1-19` (markup, both branches, empty state), `history-list.scss:1-17` (`.history` container + `.histrow` grid), `history-list.ts:36-54` (sort, delta computation, `IsFirst`, reverse), `src/FE/src/app/modules/dashboard/pages/dashboard/dashboard.page.html:43-53` (the card + `@defer (on viewport)` wrapper)

## Do / Don't

- ✅ Sort ascending by date to compute each row's delta against its immediate predecessor, then `.reverse()` for display — that is the shipped order of operations (`history-list.ts:37-53`), and computing deltas on the reversed list would invert every sign.
- ✅ Show `Kỳ đầu` rather than a zero delta on the oldest row — "nothing to compare" is a distinct fact from "no change".
- ✅ Keep `Xem` as the only per-row action; the row itself is not clickable, so the hit target is deliberately the button.
- ✅ Keep the empty-state copy year-scoped — the list is filtered by the selected year, and a generic "no periods saved" message would misreport a year that simply has none.
- ❌ Don't add edit or delete affordances to a row; the panel is a read-only jump list.
- ❌ Don't make the whole row clickable without also giving it focus and hover treatments — today it has neither.

## Normalize on redesign
1. **The row derives its own delta on the client.** `HistoryList` reuses `GET /api/dashboard/periods` and computes each delta in the browser (`history-list.ts:41-44`), while the criteria grid's deltas arrive pre-computed from the backend. Two sources for one rule.
2. Fixed pixel columns (`100px 1fr 90px 70px`) with no responsive override — the panel keeps the same template at every breakpoint, unlike `.group-row`, which has two. Long date or delta strings will crowd at narrow widths.
3. No row hover, while the tables directly above it do highlight rows — inconsistent feedback for two similar scan-and-pick lists.
4. `max-height:240px` on the scroll container is a literal, and the scroll area has no visual affordance (no fade or shadow) indicating more rows exist below.

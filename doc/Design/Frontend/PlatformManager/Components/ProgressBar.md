---
project: "PlatformManager"
status: "draft"
updated: "2026-08-22"
component: "ProgressBar"
sources: ["src/FE/src/app/modules/dashboard/components/group-progress-list/group-progress-list.html", "src/FE/src/app/modules/dashboard/components/group-progress-list/group-progress-list.scss"]
---

# ProgressBar
**Description:** Horizontal track + fill bar (`.bar` / `.fill`) showing one criteria group's weighted progress in the dashboard's "Tiến độ theo nhóm" panel. It is the app's **only** progress indicator, rendered once per group inside `GroupProgressList` (`group-progress-list.html:1-11`).

Unlike most primitives, `.bar`/`.fill` are **not** global — they are declared in the component's own stylesheet (`group-progress-list.scss:18-29`), so the bar exists only on the dashboard.

## Anatomy
`.group-row` (3-column grid: `210px 1fr 80px`, `gap: spacing.sp-3`, `align-items:center`, `typography.table-cell`) → cell 1 the bold group name (`{{ GroupCode }}. {{ GroupName }}`) → cell 2 `.bar` (track) containing one `.fill` → cell 3 `.num` with the bold numeric percentage. **The bar carries no label of its own** — the name and the number are sibling grid cells, not overlays.

Track: `height:9px`, bg `colors.surface-track`, `rounded.pill`, `overflow:hidden`. Fill: `height:100%`, bg `colors.brand`, `rounded.pill`, width set via Angular's `[style.width.%]` binding.

## Variants

| Variant | Classes | Key values | When to use |
| --- | --- | --- | --- |
| Track | `bar` | bg `colors.surface-track`, `rounded.pill`, `height:9px`, `overflow:hidden` (`group-progress-list.scss:18-23`) | Always present on every group row |
| Fill | `fill` | bg `colors.brand`, `rounded.pill`, `height:100%`, width = `clampWidth(g.Progress)` percent (`group-progress-list.scss:25-29`) | Foreground indicator; width bound per row |
| Empty state | — (no rows rendered) | `<div class="muted">Chưa có dữ liệu nhóm chỉ tiêu.</div>` (`group-progress-list.html:8-10`) | `@empty` branch when the groups array is empty |

**One colour only.** The fill is `colors.brand` for every group at every completion level — there is no good/warn/bad threshold recolouring, and no second bar variant anywhere in the app.

## States
<!-- Exactly these five rows, in this order — treatments as rendered by the shipped CSS. -->

| State | Treatment |
| --- | --- |
| default | track `colors.surface-track` at 9px tall; fill `colors.brand` at the clamped percentage; both `rounded.pill`, clipped by the track's `overflow:hidden` |
| hover | **Not styled** — no `:hover` rule on `.bar`, `.fill` or `.group-row`; the bar is not interactive |
| focus | **N/A** — no `tabindex`, not a control; it is a plain `<div>` pair, not `role="progressbar"` |
| active | **N/A** — not interactive |
| disabled | **N/A** — not a form control |

**No transition.** The fill has no `transition` declaration, so a width change on re-render snaps rather than animates.

## Tokens Used
- `colors.surface-track`, `colors.brand`, `colors.muted`
- `rounded.pill`
- `spacing.sp-3` (row gap)
- `typography.table-cell` (row text)

`height:9px` and the three grid column templates (`210px 1fr 80px`, `140px 1fr 75px` at ≤980px, `110px 1fr 68px` at ≤560px) are literals with no token behind them.

## Reference markup

```html
<div class="group-progress-list">
  @for (g of groups(); track g.GroupId) {
    <div class="group-row">
      <div><b>{{ g.GroupCode }}. {{ g.GroupName }}</b></div>
      <div class="bar"><div class="fill" [style.width.%]="clampWidth(g.Progress)"></div></div>
      <div class="num"><b>{{ formatPercent(g.Progress) }}</b></div>
    </div>
  } @empty {
    <div class="muted">Chưa có dữ liệu nhóm chỉ tiêu.</div>
  }
</div>
```

Sources: `src/FE/src/app/modules/dashboard/components/group-progress-list/group-progress-list.html:1-11` (markup + empty state), `group-progress-list.scss:18-29` (`.bar`/`.fill`), `:9-16` (`.group-row` grid), `:31-41` (responsive column templates)

## Do / Don't

- ✅ Clamp the fill width to 0–100 before binding — the template calls `clampWidth()` rather than passing the raw progress.
- ✅ Pair every bar with its numeric percentage in the sibling `.num` cell; the bar alone never carries a value.
- ✅ Keep `overflow:hidden` on the track so the fill's pill radius is clipped to the track's at low percentages.
- ✅ Render the `@empty` muted sentence rather than an empty list when there are no groups.
- ❌ Don't recolour the fill by completion level — the app deliberately uses one brand fill everywhere.
- ❌ Don't add a percentage label inside the bar; the layout reserves an 80px column for it instead.
- ❌ Don't reuse `.bar`/`.fill` outside `GroupProgressList` — they are component-scoped, so the classes resolve to nothing elsewhere.

## Normalize on redesign
1. **Not accessible.** The bar is two plain `<div>`s with no `role="progressbar"`, `aria-valuenow`, `aria-valuemin`/`max` or label association. Assistive tech sees only the sibling text; the bar itself is invisible to it.
2. `height:9px` and all three responsive column templates are literals off every scale.
3. `.bar`/`.fill` are generic names in component scope. If a second progress indicator is ever needed the names will have to be promoted to global or renamed — decide now which.
4. No `transition` on the fill width, so period switches redraw instantly with no sense of change.

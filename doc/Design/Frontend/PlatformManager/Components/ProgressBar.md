---
project: "PlatformManager"
status: "draft"
updated: "2026-08-11"
component: "ProgressBar"
sources: ["doc/Prototype/dashboard.html"]
---

# ProgressBar
**Description:** Horizontal track + fill bar (`.bar`/`.fill`, `dashboard.html:37`) showing one `CriteriaGroup`'s weighted progress in the "Tiến độ theo nhóm" panel. Rendered 6 times by `renderGroups()` (`dashboard.html:858-866`), one per group.

## Anatomy
`.bar` (track) containing one `.fill` (colored bar) whose inline `width:<percent>%` is set by JS per render. No label inside the bar itself — the group name and numeric `%` sit in sibling grid cells (`.group-row`, see the screen spec for the row layout).

## Variants

| Variant | Classes | Key values | When to use |
| --- | --- | --- | --- |
| Track | `bar` | bg `colors.surface-track`, `rounded.pill`, `height:10px`, `overflow:hidden` | Always-present background of every group row |
| Fill | `fill` | bg `colors.primary`, `rounded.pill`, `height:100%`, inline `width` = `min(100, weighted group progress)%` | Foreground indicator; width computed in `renderGroups()` |

## States
<!-- Exactly these five rows, in this order — treatments as rendered by the shipped CSS. -->

| State | Treatment |
| --- | --- |
| default | track `colors.surface-track`; fill `colors.primary`, width per computed group %, both `rounded.pill` |
| hover | **Not styled** — no `:hover` rule; the bar is not interactive |
| focus | **N/A** — not a focusable/interactive element |
| active | **N/A** — not interactive |
| disabled | **N/A** — not a form control |

## Tokens Used
- `colors.surface-track`, `colors.primary`
- `rounded.pill`

## Reference markup

```html
<div class="group-row">
  <div><b>1. Hạ tầng và Nền tảng số</b></div>
  <div class="bar"><div class="fill" style="width:74.3%"></div></div>
  <div class="num"><b>74,3%</b></div>
</div>
```

Sources: `doc/Prototype/dashboard.html:37` (CSS), `doc/Prototype/dashboard.html:858-866` (`renderGroups()`, generates the markup above per group)

## Do / Don't

- ✅ Always clamp the fill width to a maximum of 100% — `renderGroups()` does this explicitly (`Math.min(100,p)`).
- ✅ Pair every `ProgressBar` with its numeric `%` value in a sibling cell — the bar alone never carries a text label.
- ❌ Don't recolor the fill per group — the shipped app always uses `colors.primary` regardless of group or completion level.

## Normalize on redesign
1. None specific to ProgressBar beyond the library-wide items in `COMPONENTS.md` § Known inconsistencies.

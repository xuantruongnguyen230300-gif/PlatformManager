---
kind: luat
scope: du-an
verified: khong-ap-dung
project: "PlatformManager"
status: "target — not built"
updated: "2026-09-05"
component: "ProgressBar"
sources:
  - "doc/Design/Frontend/PlatformManager/Prototypes/index.html"
---

# ProgressBar

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Neither `.bar` nor `.fill` is declared in
> `src/FE/src/styles.scss` and no `app-group-progress-list` element exists — the
> dashboard module that owned both was removed on 2026-08-29. Check rather than
> trust:
>
> ```bash
> grep -rn 'group-progress\|\.bar\b' src/FE/src --include='*.scss' --include='*.html'
> ```
>
> PASS for the claim above = zero hits. This is a restore of the retired spec, not
> a new design.

**Description:** Horizontal track + fill bar (`.bar` / `.fill`) showing one
criteria group's weighted progress in the Dashboard's `Tiến độ theo nhóm` panel.
It is the app's **only** progress indicator, rendered once per group inside
`app-group-progress-list`.

Unlike most primitives it is **not** global — `.bar` / `.fill` are declared inside
the list component's own scope, so the bar exists only where that component is
mounted. That was true before the 2026-08-29 retirement and the approved
prototype keeps it that way.

> **Citation policy.** Style values cite
> `doc/Design/Frontend/PlatformManager/Prototypes/index.html` plus the **selector
> name**, never a line number. Markup and copy cite `Prototype/index.html`
> § `#screen-dashboard`.

## Anatomy

`.group-row` is a three-column grid — `210px 1fr 80px`, gap `spacing.sp-3`,
`align-items: center`, `fontSize.fs-sm` (§ `app-group-progress-list .group-row`):

1. **Name** — `<b>{{ code }}. {{ name }}</b>`, e.g. `1. Hạ tầng và Nền tảng số`
2. **Track** — `.bar` containing exactly one `.fill`
3. **Number** — `.num` with the bold percentage, e.g. `74,3%`

**The bar carries no label of its own.** The name and the number are sibling grid
cells, not overlays — which is why the third column is reserved at 80px rather
than the percentage being printed inside the track.

The list container `.group-progress-list` is a flex column with
`justify-content: space-between` and gap `spacing.sp-3`, so the rows distribute
down the full height of the host `Card`
(§ `app-group-progress-list .group-progress-list`).

## Variants

| Variant | Classes | Key values | When to use |
| --- | --- | --- | --- |
| Track | `bar` | fill `colors.surface-track`, `rounded.pill`, `height: 9px`, `overflow: hidden` (§ `app-group-progress-list .bar`) | Always present on every group row |
| Fill | `fill` | fill `colors.brand`, `rounded.pill`, `height: 100%`, width bound per row (§ `app-group-progress-list .fill`) | The foreground indicator |
| Empty state | — no rows rendered | one `.muted` sentence instead of the list | The period returns no groups |

**One colour only.** The fill is `colors.brand` for every group at every completion
level — there is no good/warn/bad threshold recolouring and no second bar variant
anywhere in the design. A group at 51,8% and a group at 100,0% differ by length,
not by hue.

### The six groups of the rebuilt Dashboard

Values verbatim from `Prototype/index.html` § `#screen-dashboard` →
`app-group-progress-list`; percentages are total `Tự đánh giá` ÷ total
`Điểm tối đa` measured from `spec/DTI_CanGiuoc_2026-08-11.csv`.

| Row | Label | Fill width |
| --- | --- | --- |
| 1 | `1. Hạ tầng và Nền tảng số` | `74,3%` |
| 2 | `2. Nhân lực số` | `51,8%` |
| 3 | `3. An toàn thông tin, an ninh mạng` | `100,0%` |
| 4 | `4. Hoạt động chính quyền số` | `85,1%` |
| 5 | `5. Hoạt động Kinh tế số` | `100,0%` |
| 6 | `6. Hoạt động Xã hội số` | `67,6%` |

Per decision Q11 the bar is driven by **`Tiến độ %`**, not by the three score
columns — those are read-only in the detail table. How `Tiến độ %` is seeded on
import is an open question; see § Cần chốt.

## States
<!-- Exactly these five rows, in this order — treatments as rendered by the shipped CSS. -->

| State | Treatment |
| --- | --- |
| default | Track `colors.surface-track` at 9px tall; fill `colors.brand` at the clamped percentage; both `rounded.pill`, the fill clipped by the track's `overflow: hidden` |
| hover | **Not styled** — no `:hover` rule on `.bar`, `.fill` or `.group-row`; the bar is not interactive |
| focus | **Not applicable** — a plain pair of `<div>`s with no `tabindex`, and no `role="progressbar"` |
| active | **Not applicable** — not interactive |
| disabled | **Not applicable** — not a form control |

**No transition.** The fill declares no `transition`, so switching period snaps the
width rather than animating it.

## Tokens Used

- `colors.surface-track` (track), `colors.brand` (fill), `colors.muted` (empty state)
- `rounded.pill` (both track and fill)
- `spacing.sp-3` (row gap and list gap)
- `fontSize.fs-sm` (row text)

Un-tokenised literals: `height: 9px`, and the three grid templates
`210px 1fr 80px` (desktop), `140px 1fr 75px` (≤980px),
`110px 1fr 68px` (≤560px). Carried in `../Tokens/spacing.md` alongside the other
structural literals.

## Reference markup

```html
<!-- Prototype/index.html § #screen-dashboard → app-group-progress-list -->
<app-group-progress-list>
  <div class="group-progress-list">
    <div class="group-row">
      <div><b>1. Hạ tầng và Nền tảng số</b></div>
      <div class="bar"><div class="fill" style="width: 74.3%"></div></div>
      <div class="num"><b>74,3%</b></div>
    </div>
    <!-- five more rows, one per group -->
  </div>
</app-group-progress-list>
```

Sources: `doc/Design/Frontend/PlatformManager/Prototypes/index.html`
(§ `app-group-progress-list .group-progress-list`, § `.group-row`, § `.bar`,
§ `.fill`, plus the `@media` blocks at 980px and 560px),
`Prototype/index.html` § `#screen-dashboard` → `app-group-progress-list` (the six
approved rows), `spec/DTI_CanGiuoc_2026-08-11.csv` (the percentages),
[`Card.md`](./Card.md) (the host surface).

## Do / Don't

- ✅ Clamp the fill width to 0–100 before binding; a stored `Tiến độ %` above 100 would otherwise overflow the track.
- ✅ Pair every bar with its numeric percentage in the sibling `.num` cell. The bar alone never carries a value, and a screen reader gets the number from that cell.
- ✅ Keep `overflow: hidden` on the track so the fill's pill radius is clipped to the track's at low percentages — without it a 2% fill renders as a lozenge wider than its own value.
- ✅ Render the muted empty sentence rather than an empty list when a period has no groups.
- ❌ Don't recolour the fill by completion level. One brand fill everywhere is a deliberate decision, not an omission.
- ❌ Don't print the percentage inside the bar; the layout reserves an 80px column for it.
- ❌ Don't reuse `.bar` / `.fill` outside this list — they are component-scoped and resolve to nothing elsewhere. If a second progress indicator is ever needed, promote them first.

## Normalize on redesign

1. **Not accessible.** Two plain `<div>`s with no `role="progressbar"`, no `aria-valuenow` / `aria-valuemin` / `aria-valuemax` and no label association. Assistive tech sees only the sibling text; the bar itself is invisible to it. This is the highest-value fix in this file.
2. **`height: 9px` and all three responsive column templates are literals** off every scale.
3. **`.bar` / `.fill` are generic names in component scope.** A second progress indicator will force a rename or a promotion to global — decide which before the second call site exists, not after.
4. **No `transition` on the fill width**, so a period switch redraws instantly and the change is invisible.
5. **`flex: 1` sits on the child, not the host.** Defect **A2** — `doc/Design/Frontend/PlatformManager/Prototypes/index.html` § `app-group-progress-list .group-progress-list` declares `flex: 1`, and no rule anywhere in that file gives `app-group-progress-list` a `display: flex`. The host is not a flex container and declares no `display: flex`, so the list does not actually stretch to the card's height and the `justify-content: space-between` above is inert.

## Cần chốt

<!-- Open questions this spec must not answer on its own. Raised 2026-09-05. -->

1. **How `Tiến độ %` is seeded on import.** The prototype assumes `Tự đánh giá ÷ Điểm tối đa`, rounded — which is the only assumption under which the six percentages above reproduce the real CSV. The alternative is an independent field that starts at 0, in which case every bar reads 0 immediately after the first import and this panel is empty on day one. Open item 2 of the 2026-09-05 brief; owned by `spec/danh-muc-dti/business-rules.md`.

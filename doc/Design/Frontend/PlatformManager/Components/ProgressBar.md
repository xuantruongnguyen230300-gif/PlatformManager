---
kind: luat
scope: du-an
verified: 2026-09-10
project: "PlatformManager"
status: "built 2026-09-09 in src/FE — no call site on any page yet"
updated: "2026-09-10"
component: "ProgressBar"
sources:
  - "src/FE/src/app/modules/dashboard/components/progress-bar/progress-bar.ts"
  - "src/FE/src/app/modules/dashboard/components/progress-bar/progress-bar.html"
  - "src/FE/src/app/modules/dashboard/components/progress-bar/progress-bar.scss"
  - "doc/Design/Frontend/PlatformManager/Prototypes/index.html"
---

# ProgressBar

> ✅ **CÓ THẬT — dựng 2026-09-09, đối chiếu 2026-09-10.** `app-progress-bar` exists at
> `src/FE/src/app/modules/dashboard/components/progress-bar/`, in the feature module
> rather than in `shared/`: only the Dashboard renders it, and
> `doc/huong_dan/quy-uoc/fe-architecture.md` § Bên trong `shared/` keeps a
> one-feature component out of the shared layer.
>
> **The scoping decision this spec recorded survived the build**, and so did the
> row/host decision: `.group-row` is set on the component's own host element rather
> than on a wrapper `<div>`, so the parent flex `gap` is not broken.
>
> | What | Where it shipped |
> | --- | --- |
> | `.group-row` on the host, with the reason | `src/FE/src/app/modules/dashboard/components/progress-bar/progress-bar.ts:46-51` |
> | The three-column grid `210px 1fr 80px` | `src/FE/src/app/modules/dashboard/components/progress-bar/progress-bar.scss:8-15` |
> | `.bar` track and `.fill`, component-scoped | `src/FE/src/app/modules/dashboard/components/progress-bar/progress-bar.scss:19-31` |
> | The two responsive steps, 980px and 560px | `src/FE/src/app/modules/dashboard/components/progress-bar/progress-bar.scss:33-43` |
> | Name / track / number, in that order | `src/FE/src/app/modules/dashboard/components/progress-bar/progress-bar.html:2-34` |
>
> ```bash
> grep -rn 'app-progress-bar' src/FE/src --include='*.ts'   # PASS = at least one hit
> grep -c '\.bar\b' src/FE/src/styles.scss                  # PASS = 0 — still NOT global
> ```
>
> 🛑 **Built is not composed, and the list around it was not built at all.** There is
> no `app-group-progress-list` in `src/FE`: the container this spec describes
> (§ Anatomy) has no shipped owner, only the row does. No page renders either — the
> Dashboard page is still a stub
> (`src/FE/src/app/modules/dashboard/pages/dashboard/dashboard.page.html:1-10`) and
> `DASHBOARD_ROUTES` is deliberately left out of `app.routes.ts`
> (`src/FE/src/app/modules/dashboard/dashboard.routes.ts:8-13`).
>
> The banner this replaces set **PASS = zero hits** for
> `group-progress|\.bar\b`. Half of that has inverted and half has not: the element
> exists, and `.bar` is still absent from the global stylesheet — which is the
> outcome this spec asked for.

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
> name**, never a line number. Markup and copy cite `doc/Design/Frontend/PlatformManager/Prototypes/index.html`
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
| Awaiting data | `bar` rendered with **no `.fill`** (shipped — `src/FE/src/app/modules/dashboard/components/progress-bar/progress-bar.html:19-23`) | empty track; the `.num` cell reads `—`, matching how an absent value is drawn on the KPI tiles (decision T12, [`../Screens/01-dashboard.md`](../Screens/01-dashboard.md) § States) | The group has no `Tiến độ %` in the period — **the normal state right after every import** (decision Q24). Recorded 2026-09-09 |
| Empty state | — no rows rendered | one `.muted` sentence instead of the list | The period returns no groups |

**One colour only.** The fill is `colors.brand` for every group at every completion
level — there is no good/warn/bad threshold recolouring and no second bar variant
anywhere in the design. A group at 51,8% and a group at 100,0% differ by length,
not by hue.

### The six groups of the rebuilt Dashboard

Values verbatim from `doc/Design/Frontend/PlatformManager/Prototypes/index.html` § `#screen-dashboard` →
`app-group-progress-list`; each percentage is total `Tự đánh giá` ÷ total
`Điểm tối đa` for its group, computed from the BA's August 2026 spreadsheet, which is deliberately outside this repo (root `.gitignore`) — so the values are read off the
prototype rather than re-measured here. The in-repo sample `spec/danh-muc-dti/dti-mau-an-danh-62-dong.csv` is a
**different** 62-row dataset and yields different percentages for the same six group
names. Read the ⚠️ below the table before treating any of them as a value this bar
can produce.

| Row | Label | Fill width |
| --- | --- | --- |
| 1 | `1. Hạ tầng và Nền tảng số` | `74,3%` |
| 2 | `2. Nhân lực số` | `51,8%` |
| 3 | `3. An toàn thông tin, an ninh mạng` | `100,0%` |
| 4 | `4. Hoạt động chính quyền số` | `85,1%` |
| 5 | `5. Hoạt động Kinh tế số` | `100,0%` |
| 6 | `6. Hoạt động Xã hội số` | `67,6%` |

Per decision Q11 the bar is driven by **`Tiến độ %`**, not by the three score
columns — those are read-only in the detail table.

> ### ⚠️ Those six percentages are a POPULATED-state illustration, not an import result
>
> They are a ratio of **scores** — `Σ Tự đánh giá ÷ Σ Điểm tối đa`. The bar renders
> **`Tiến độ %`**, which decision **Q24** (2026-09-05) leaves **blank** on import as a
> hand-entered field; the file the BA supplies carries no such column at all. The two
> quantities therefore have **no relationship** — the master file states this in as many
> words (`spec/danh-muc-dti/business-rules.md` §6.4,
> `spec/dashboard-dti/business-rules.md` §1.1) and warns against using the score figures
> as an expected value in a test.
>
> **Consequence, and it is the everyday state rather than an edge case: right after an
> import all six bars are EMPTY.** Not `0%` — *"no data yet"*. A criterion with no
> `Tiến độ %` is excluded from the calculation instead of counted as zero, so the group
> figure is **absent**, and an absent figure is drawn as the `Awaiting data` variant
> above, never as a zero-length fill with `0,0%` beside it. Those are different facts and
> the screen has to keep them apart (`Screens/01-dashboard.md` § States, decision Q32).
>
> Recorded 2026-09-09, closing § Cần chốt item 1.

## States
<!-- Exactly these five rows, in this order — treatments as rendered by the shipped CSS. -->

| State | Treatment |
| --- | --- |
| default | Track `colors.surface-track` at 9px tall; fill `colors.brand` at the clamped percentage; both `rounded.pill`, the fill clipped by the track's `overflow: hidden` |
| hover | **Not styled** — no `:hover` rule on `.bar`, `.fill` or `.group-row`; the bar is not interactive |
| focus | **Not applicable** — nothing here takes focus: no `tabindex` anywhere, and `role="progressbar"` is not a focusable role. The role **is** present as of 2026-09-09 (`src/FE/src/app/modules/dashboard/components/progress-bar/progress-bar.html:10-18`); it makes the bar readable, not reachable |
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
<!-- doc/Design/Frontend/PlatformManager/Prototypes/index.html § #screen-dashboard → app-group-progress-list -->
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
`doc/Design/Frontend/PlatformManager/Prototypes/index.html` § `#screen-dashboard` → `app-group-progress-list` (the six
approved rows), the BA's August 2026 spreadsheet for where the percentages were
computed — outside this repo (root `.gitignore`), so read them off the prototype above —
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

1. ~~**Not accessible.**~~ **Closed 2026-09-09, checked 2026-09-10.** The item read *"two plain `<div>`s with no `role="progressbar"`, no `aria-valuenow` / `aria-valuemin` / `aria-valuemax` and no label association … the highest-value fix in this file"*. The built component carries all of it — `role="progressbar"`, `aria-label` from the group name, `aria-valuemin` / `aria-valuemax` / `aria-valuenow` — at `src/FE/src/app/modules/dashboard/components/progress-bar/progress-bar.html:10-18`. One detail was decided there rather than here and is worth knowing: in the *Awaiting data* variant `aria-valuenow` is **omitted** and `aria-valuetext` says so in words, because a `0` would read as a measured zero. Struck through rather than deleted: the item is why the built component has any of this.
2. **`height: 9px` and all three responsive column templates are literals** off every scale.
3. **`.bar` / `.fill` are generic names in component scope.** A second progress indicator will force a rename or a promotion to global — decide which before the second call site exists, not after.
4. **No `transition` on the fill width**, so a period switch redraws instantly and the change is invisible.
5. **`flex: 1` sits on the child, not the host.** Defect **A2** — `doc/Design/Frontend/PlatformManager/Prototypes/index.html` § `app-group-progress-list .group-progress-list` declares `flex: 1`, and no rule anywhere in that file gives `app-group-progress-list` a `display: flex`. The host is not a flex container and declares no `display: flex`, so the list does not actually stretch to the card's height and the `justify-content: space-between` above is inert.

## Cần chốt

<!-- Open questions this spec must not answer on its own. Raised 2026-09-05; the last one closed 2026-09-09. -->

1. ~~**How `Tiến độ %` is seeded on import.**~~ **Closed 2026-09-09 — it is not seeded at all.** This spec guessed `Tự đánh giá ÷ Điểm tối đa`, on the reasoning that it was the only assumption under which the six percentages above reproduce the real CSV. That guess is **wrong**: decision **Q24** makes `Tiến độ %` a hand-entered field left blank by import, and the guessed formula was considered and rejected because it would leave one field half-computed and half-typed. Owner: `spec/danh-muc-dti/business-rules.md` §6.4. What it means for this component is in the ⚠️ under § The six groups and in the `Awaiting data` row of § Variants.

**No open question remains in this file.**

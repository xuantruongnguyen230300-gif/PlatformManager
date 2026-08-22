---
project: "PlatformManager"
status: "draft"
updated: "2026-08-22"
component: "Card"
sources: ["src/FE/src/styles.scss", "src/FE/src/app/modules/dashboard/pages/dashboard/dashboard.page.html", "src/FE/src/app/modules/dashboard/components/kpi-tile/kpi-tile.html", "src/FE/src/app/platform/phan-quyen/pages/phan-quyen/phan-quyen.page.html"]
---

# Card
**Description:** The generic white surface container (`.card`, `styles.scss:134-140`) used as the shell for every major section on the four in-shell routes. It is the app's primary layering device: `styles.scss:21-27` records the deliberate move from "border-first" to "fill-first", so a card separates from the page by **shadow**, with `colors.line` left as a faint hairline rather than a structural border.

## Anatomy
Rectangle: bg `colors.card`, 1px `colors.line` border, `shadow` (two layers), `rounded.lg` radius, `spacing.card-padding` on all sides. The card supplies no internal layout beyond padding. Most instances open with the global `.title` row (`styles.scss:274-285`) — a `space-between` flex holding an `<h2>` at `typography.h2-title` and, on the right, either a `.muted` caption or a `Button`.

## Variants

| Variant | Classes | Key values | When to use |
| --- | --- | --- | --- |
| Generic section card | `card` | bg `colors.card`, border `colors.line`, `shadow`, `rounded.lg`, `spacing.card-padding` | Every top-level section: the two dashboard panels (`dashboard.page.html:21,28`), the user list (`quan-tri-nguoi-dung.page.html:1`), both permission panels (`phan-quyen.page.html:11,33`) |
| KPI tile | `card kpi` | same box + a fixed label/value/sub anatomy | See `KpiTile.md` — documented separately because its internal structure repeats identically five times |
| Toolbar card | `weekbar card no-print` | same box; screen-local `.weekbar` adds the horizontal flex layout and is hidden when printing | The dashboard period toolbar (`period-toolbar.html:1`) |
| Criteria-table card | `card criteria-table-card` | same box + `margin-top:16px` (`criteria-table.scss:1-3`) | The dashboard criteria grid section (`criteria-table.html:1`) |
| Catalogue grid card | `card dti-grid-card` | same box + `display:flex; flex-direction:column; min-height:0` so the grid inside can size to the remaining height (`danh-muc-dti.page.scss:1-5`) | The DTI catalogue page shell (`danh-muc-dti.page.html:1`) |
| History card | `card history-card` | same box + screen-local layout in `dashboard.page.scss` | The saved-periods panel (`dashboard.page.html:43`) |

Every modifier above changes only layout or margin — **no variant alters the card's background, border, radius, shadow or padding.** The one visually distinct surface in the app, `.login-card`, is a *different* class with its own `spacing.auth-card-padding`; it is not a `.card` variant (see the auth-shell component spec).

## States
<!-- Exactly these five rows, in this order — treatments as rendered by the shipped CSS. -->

| State | Treatment |
| --- | --- |
| default | bg `colors.card`, border 1px `colors.line`, `shadow`, `rounded.lg`, `spacing.card-padding` (`styles.scss:134-140`) |
| hover | **Not styled** — no `.card:hover` rule exists in any stylesheet; the card is a static container, never a clickable surface |
| focus | **N/A** — no `tabindex`, not an interactive control. Focusable descendants (buttons, fields) style themselves |
| active | **N/A** — not interactive |
| disabled | **N/A** — not a form control. A section that must not be edited hides its actions instead (`danh-muc-dti.page.html:36-41`) and shows a `NoticeBanner`; the card itself never dims |

## Tokens Used
- `colors.card`, `colors.line`, `colors.text`
- `rounded.lg`
- `spacing.card-padding`
- `shadow` (`--shadow`, two layers — `Tokens/colors.md`)
- `typography.h2-title` (via the `.title` row)

`margin-top:16px` on `.criteria-table-card` is a literal that sits off the spacing scale (nearest step `--sp-5` is 14px).

## Reference markup

```html
<div class="card">
  <div class="title">
    <h2>Tiến độ theo nhóm</h2>
    <span class="muted">Tuần hiện tại</span>
  </div>
  <app-group-progress-list [groups]="aggregate().Groups" />
</div>

<div class="card">
  <div class="title">
    <h2>Danh sách người dùng</h2>
    <button type="button" class="btn primary" (click)="openCreateForm()">+ Thêm người dùng</button>
  </div>
  …
</div>

<div class="card kpi">
  <div class="label">Tiến độ chung tuần này</div>
  <div class="value">82,1%</div>
  <div class="sub">Bình quân gia quyền theo điểm</div>
</div>
```

Sources: `src/FE/src/styles.scss:134-140` (`.card`), `:270-281` (`.title` row), `:21-27` (fill-first rationale), `src/FE/src/app/modules/dashboard/pages/dashboard/dashboard.page.html:21,28,43`, `src/FE/src/app/modules/dashboard/components/kpi-tile/kpi-tile.html:1`, `src/FE/src/app/modules/dashboard/components/criteria-table/criteria-table.html:1` + `criteria-table.scss:1-3`, `src/FE/src/app/modules/danh-muc-dti/pages/danh-muc-dti/danh-muc-dti.page.html:1` + `danh-muc-dti.page.scss:1-5`, `src/FE/src/app/platform/quan-tri-nguoi-dung/pages/quan-tri-nguoi-dung/quan-tri-nguoi-dung.page.html:1`, `src/FE/src/app/platform/phan-quyen/pages/phan-quyen/phan-quyen.page.html:11,33`, `src/FE/src/app/modules/dashboard/components/period-toolbar/period-toolbar.html:1`

## Do / Don't

- ✅ Put every top-level section inside a `.card` — all four in-shell routes do, without exception.
- ✅ Separate cards with `shadow`, never a heavier border — reaching for `colors.border-strong` on a card reverts the fill-first decision at `styles.scss:21-27`.
- ✅ Open a card with the `.title` row when it needs a heading; put the section's single action or a `.muted` caption on its right.
- ✅ Add screen-local layout via a second class (`dti-grid-card`, `criteria-table-card`) and leave the `.card` box untouched — that is the shipped convention.
- ❌ Don't vary padding, radius or shadow per section; the app uses exactly one card treatment.
- ❌ Don't nest a `.card` inside a `.card` — no instance does.

## Normalize on redesign
1. `.card` keeps a 1px `colors.line` border **and** a two-layer shadow. After the fill-first change the border is largely vestigial — decide whether the hairline still earns its place, since removing it would simplify every surface to one mechanism.
2. `.criteria-table-card`'s `margin-top:16px` is off the spacing scale; the gap between the other dashboard sections comes from the layout's `--sp-5` (14px), so the criteria grid sits 2px lower than its siblings for no stated reason.
3. Four one-off card modifiers exist across four files, three of which only set flex/margin. A `.card--fill` layout utility would cover them.

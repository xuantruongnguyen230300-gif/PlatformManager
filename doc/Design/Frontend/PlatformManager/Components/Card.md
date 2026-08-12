---
project: "PlatformManager"
status: "draft"
updated: "2026-08-11"
component: "Card"
sources: ["doc/Prototype/dashboard.html"]
---

# Card
**Description:** Generic bordered, shadowed white container (`.card`, `dashboard.html:28`) used as the visual shell for every major section of the dashboard — weekbar, KPI tiles (see `KpiTile.md`), groups panel, trend-chart panel, criteria-table panel, history panel.

## Anatomy
Rectangle: background `colors.surface`, 1px border `colors.border`, `rounded.card` radius (14px), `shadow.card` elevation, `spacing.lg-card` padding (15px) on all sides. Content is section-specific (heading + body); the Card itself supplies no internal layout beyond padding.

## Variants

| Variant | Classes | Key values | When to use |
| --- | --- | --- | --- |
| Generic section card | `card` | bg `colors.surface`, border `colors.border`, radius `rounded.card`, shadow `shadow.card`, padding `spacing.lg-card` | Weekbar, groups panel, trend-chart panel, criteria-table panel, history panel |
| KPI card | `card kpi` | same box as generic + specific label/value/sub children | See `KpiTile.md` — documented separately because its internal anatomy (not just the box) repeats identically 5 times |

## States
<!-- Exactly these five rows, in this order — treatments as rendered by the shipped CSS. -->

| State | Treatment |
| --- | --- |
| default | bg `colors.surface`, border `colors.border`, `shadow.card`, `rounded.card` |
| hover | **Not styled** — `.card` is a static container, no `:hover` rule exists |
| focus | **N/A** — `.card` itself is not a focusable element (no `tabindex`, not an interactive control) |
| active | **N/A** — not an interactive element |
| disabled | **N/A** — not a form control; concept doesn't apply |

## Tokens Used
- `colors.surface`, `colors.border`
- `rounded.card`
- `spacing.lg-card`
- `shadow.card` (`Tokens/spacing.md` § Elevation)

## Reference markup

```html
<div class="card kpi"><div class="label">Tiến độ chung tuần này</div><div class="value" id="kProgress">0%</div><div class="sub">Bình quân gia quyền theo điểm</div></div>
<div class="card">
 <div class="title"><h2>Tiến độ theo nhóm</h2><span class="muted">Tuần hiện tại</span></div>
 <div id="groups"></div>
</div>
```

Sources: `doc/Prototype/dashboard.html:28` (CSS), `doc/Prototype/dashboard.html:78,87-91,95-99,105,128` (markup instances)

## Do / Don't

- ✅ Every top-level dashboard section sits inside a `.card` — don't introduce a bare, unbordered section.
- ✅ Reuse the same padding (`spacing.lg-card`) regardless of section — the shipped app never varies card padding.
- ❌ Don't add a custom shadow/radius per section — the app uses exactly one card treatment everywhere.

## Normalize on redesign
1. None specific to Card beyond the library-wide items in `COMPONENTS.md` § Known inconsistencies.

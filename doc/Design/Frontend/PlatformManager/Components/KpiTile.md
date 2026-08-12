---
project: "PlatformManager"
status: "draft"
updated: "2026-08-11"
component: "KpiTile"
sources: ["doc/Prototype/dashboard.html"]
---

# KpiTile
**Description:** Label/value/sub-caption stat tile — a `.card` (see `Card.md`) with the `.kpi` modifier plus a fixed 3-line internal anatomy (`dashboard.html:29,87-91`). Renders 5 times in `.kpis`, one per KPI computed by `renderKPIs()`/`stats()` (`dashboard.html:851-857`).

## Anatomy
`.card.kpi` → `.label` (small caption) → `.value` (large number/text, the hero content) → `.sub` (small caption, static help text or a dynamic period label for the delta tile). No icon, no chart inside the tile itself.

## Variants

| Variant | Classes | Key values | When to use |
| --- | --- | --- | --- |
| Neutral value | `.value` (no extra class) | text `colors.text` | "Tiến độ chung tuần này" (`#kProgress`), "Hoàn thành 100%" (`#kDone`) |
| Good value | `.value.good` | text `colors.success` | "Chỉ tiêu tăng" (`#kUp`) always; "So với tuần trước" (`#kDelta`) when delta > 0 |
| Bad value | `.value.bad` | text `colors.danger` | "So với tuần trước" (`#kDelta`) when delta < 0 |
| Warn value | `.value.warn` | text `colors.warning` | "Không tăng" (`#kFlat`) label uses `.warn` class in markup (`dashboard.html:90`); the value itself only turns `.good`/`.bad`/neutral via `renderKPIs()`, never `.warn`, in the current JS |
| Empty/dash state | — | literal `—` character, no color class | "So với tuần trước" and "Không tăng" both render `—` when no previous period exists yet (`dashboard.html:856`) |

## States
<!-- Exactly these five rows, in this order — treatments as rendered by the shipped CSS. -->

| State | Treatment |
| --- | --- |
| default | `.label` 12px `colors.text-muted`; `.value` 27px/weight 850 `colors.text` (or good/bad/warn per variant); `.sub` 12px `colors.text-muted` |
| hover | **Not styled** — static content, no `:hover` rule |
| focus | **N/A** — not a focusable/interactive element |
| active | **N/A** — not interactive |
| disabled | **N/A** — not a form control |

## Tokens Used
- `colors.surface`, `colors.border`, `colors.text`, `colors.text-muted`, `colors.success`, `colors.warning`, `colors.danger`
- `rounded.card`, `spacing.lg-card`, `shadow.card` (inherited from `Card.md`)
- `typography.kpi-value` (27px/850), `typography.label` (12px/400, shared with the generic label style)

## Reference markup

```html
<div class="card kpi"><div class="label">Tiến độ chung tuần này</div><div class="value" id="kProgress">0%</div><div class="sub">Bình quân gia quyền theo điểm</div></div>
<div class="card kpi"><div class="label">So với tuần trước</div><div class="value" id="kDelta">—</div><div class="sub" id="prevLabel">Chưa có kỳ trước</div></div>
<div class="card kpi"><div class="label">Chỉ tiêu tăng</div><div class="value good" id="kUp">0</div><div class="sub">Có tiến bộ so với kỳ trước</div></div>
<div class="card kpi"><div class="label">Không tăng</div><div class="value warn" id="kFlat">0</div><div class="sub">Cần chú ý theo dõi</div></div>
<div class="card kpi"><div class="label">Hoàn thành 100%</div><div class="value" id="kDone">0/62</div><div class="sub">Số chỉ tiêu đạt đủ tiến độ</div></div>
```

Sources: `doc/Prototype/dashboard.html:29` (CSS), `doc/Prototype/dashboard.html:87-91` (markup, 5 instances), `doc/Prototype/dashboard.html:851-857` (`renderKPIs()` value/class logic)

## Do / Don't

- ✅ Keep the fixed 3-part anatomy (label → value → sub) — the shipped app never varies it per tile.
- ✅ Show `—` (not `0` or blank) for KPIs that depend on a previous period when none exists yet.
- ❌ Don't add icons or secondary values inside a KPI tile — not present in the shipped design.
- ❌ Don't invent a `.warn`-colored `.value` — the JS only ever assigns `.good`/`.bad`/neutral to `#kDelta`'s value (`renderKPIs()`); `.warn` is only used on the static `#kFlat` label wrapper.

## Normalize on redesign
1. The mobile last-tile-full-width rule (`.kpis .card:last-child{grid-column:1/-1}` at ≤560px, `dashboard.html:56`) is a layout artifact of a 5-item grid on 2 columns — flag for a future adaptive grid rather than a hard-coded "last child" rule.

---
project: "PlatformManager"
status: "draft"
updated: "2026-08-11"
component: "DeltaIndicator"
sources: ["doc/Prototype/dashboard.html"]
---

# DeltaIndicator
**Description:** Inline text showing a period-over-period change, colored by direction (`.delta` + `.up`/`.down`/`.flat`, `dashboard.html:46`). Reused in three places with three slightly different renderers: the "So với tuần trước" KPI value (`renderKPIs()`), the table's "Tăng/giảm" column (`renderTable()`), and each `HistoryRow`'s change span (`renderHistory()`) — all sharing the same epsilon-threshold logic (`business-rules.md` §3.2: `>+0.001` up, `<-0.001` down, else flat).

## Anatomy
Single inline text span: optional `↑`/`↓` arrow glyph prefix + formatted number + unit suffix (`đ.%` in the table/history renderers; the KPI tile renderer uses its own format, see below). No icon font — arrows are literal Unicode characters (`↑`/`↓`) in the JS template strings.

## Variants

| Variant | Classes | Key values | When to use |
| --- | --- | --- | --- |
| Up | `delta up` | text `colors.success` | `delta > 0.001` |
| Down | `delta down` | text `colors.danger` | `delta < -0.001` |
| Flat | `delta flat` | text `colors.text-muted` | `delta` defined and `\|delta\| <= 0.001` |
| Undefined (no previous period) | — (renders literal `—`, no delta class applied in the table cell) | text default | `deltaOf(id)` returns `null` — no earlier period exists for this criterion yet |

## States
<!-- Exactly these five rows, in this order — treatments as rendered by the shipped CSS. -->

| State | Treatment |
| --- | --- |
| default | `font-weight:850`, color per variant above, `white-space:nowrap` |
| hover | **N/A** — plain text, not interactive |
| focus | **N/A** — not focusable |
| active | **N/A** — not interactive |
| disabled | **N/A** — not a form control |

## Tokens Used
- `colors.success`, `colors.danger`, `colors.text-muted`
- `typography.delta` (weight 850)

## Reference markup

```html
<!-- table "Tăng/giảm" column -->
<span class="delta up">↑ +8,4 đ.%</span>
<span class="delta down">↓ 2,0 đ.%</span>
<span class="delta flat">0,0 đ.%</span>

<!-- KPI "So với tuần trước" value (own format, not a .delta span, but same up/down/flat class logic) -->
<div class="value good" id="kDelta">↑ 9,8 đ.%</div>
```

Sources: `doc/Prototype/dashboard.html:46` (CSS), `doc/Prototype/dashboard.html:851-857` (`renderKPIs()`), `doc/Prototype/dashboard.html:872-896` esp. `886-887,892` (`renderTable()`), `doc/Prototype/dashboard.html:897-903` (`renderHistory()`)

## Do / Don't

- ✅ Use the exact epsilon threshold (`0.001`) everywhere a delta is classified — never a strict `===`/`>0` comparison on floating-point progress values (see `spec/dashboard-dti-weekly/business-rules.md` §3.2).
- ✅ Render `—` (not `0,0`) when no previous period exists — "undefined" and "flat" are different states with different meaning.
- ❌ Don't invent a distinct icon set for up/down — the shipped app uses plain Unicode `↑`/`↓` characters inline in text, not an icon component.

## Normalize on redesign
1. None specific to DeltaIndicator beyond the library-wide items in `COMPONENTS.md` § Known inconsistencies.

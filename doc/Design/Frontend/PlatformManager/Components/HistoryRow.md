---
project: "PlatformManager"
status: "draft"
updated: "2026-08-11"
component: "HistoryRow"
sources: ["doc/Prototype/dashboard.html"]
---

# HistoryRow
**Description:** One saved-period row in the "Lịch sử các kỳ đã lưu" list (`.histrow`, `dashboard.html:50`), rendered once per entry in `historyData` (newest first) by `renderHistory()` (`dashboard.html:897-903`).

## Anatomy
4-column grid: period date (bold) → "Tiến độ chung **X%**" text → `DeltaIndicator`-style change span (see `DeltaIndicator.md`; uses the same up/down color logic but not the literal `.delta` class) → "Xem" `Button` that calls `loadSavedWeek(date)`.

## Variants

| Variant | Classes | Key values | When to use |
| --- | --- | --- | --- |
| History row | `histrow` | `grid-template-columns:110px 1fr 95px 80px`, `gap:8px`, `padding:8px`, border-bottom 1px `colors.border`, `font-size:12px` | One per saved `AssessmentPeriod`, newest first |
| Empty state | — (no `.histrow` rendered) | `.muted` text | Shown instead of any rows when `historyData` is empty: `<div class="muted">Chưa có tuần nào được lưu.</div>` |

## States
<!-- Exactly these five rows, in this order — treatments as rendered by the shipped CSS. -->

| State | Treatment |
| --- | --- |
| default | grid layout per above; bottom border `colors.border`; text `colors.text` (dates/labels), delta span colored per `DeltaIndicator` rules |
| hover | **Not styled** — no `.histrow:hover` rule |
| focus | **N/A** at the row level — only the "Xem" `Button` inside is individually focusable |
| active | **N/A** at the row level — only the "Xem" `Button` has the shared `.btn:active` press effect |
| disabled | **N/A** — the row itself is not a form control; its `Button` is never disabled (see `Button.md`) |

## Tokens Used
- `colors.border`, `colors.text`, `colors.success`, `colors.danger`
- `typography.label` (12px/400 — shipped `.histrow` size matches the generic label style, no dedicated token needed)
- `spacing.xs` (8px gap and 8px padding — both map to `spacing.xs`)

## Reference markup

```html
<div class="histrow">
  <b>11/08/2026</b>
  <span>Tiến độ chung <b>82,1%</b></span>
  <span class="good">↑ +9,8 đ.%</span>
  <button class="btn" onclick="loadSavedWeek('2026-08-11')">Xem</button>
</div>
```

Sources: `doc/Prototype/dashboard.html:50` (CSS), `doc/Prototype/dashboard.html:897-903` (`renderHistory()`, builds the markup above per saved period)

## Do / Don't

- ✅ Sort newest-first (`sortedHistory().reverse()`) — matches the shipped order exactly.
- ✅ Show the empty-state muted text when no periods are saved yet — don't render an empty list silently.
- ❌ Don't add edit/delete affordances to a row — "Xem" (view/load into draft) is the only action the shipped app exposes here.

## Normalize on redesign
1. None specific to HistoryRow beyond the library-wide items in `COMPONENTS.md` § Known inconsistencies.

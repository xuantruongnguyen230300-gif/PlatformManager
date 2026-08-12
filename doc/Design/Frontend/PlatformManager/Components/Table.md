---
project: "PlatformManager"
status: "draft"
updated: "2026-08-11"
component: "Table"
sources: ["doc/Prototype/dashboard.html"]
---

# Table
**Description:** Horizontally-scrolling data table with a sticky header (`.tablewrap` wrapping `table`, `dashboard.html:39-43`) that lists all 62 DTI criteria, one row per criterion, rendered by `renderTable()` (`dashboard.html:872-896`).

## Anatomy
`.tablewrap` (scroll container, `overflow:auto`, bordered, `rounded.table`) → `table` (`min-width:1200px`, `border-collapse:collapse`) → `thead` (`th`, `position:sticky;top:0`) with 9 columns → `tbody#tbody` (one `<tr>` per criterion). Columns, left to right: Mã, Chỉ tiêu, Nhóm, Điểm tối đa (`.num`), Tuần trước (`.num`), Tuần này (`input.progressInput`, `.num`), Tăng/giảm (`.delta`, `.num`), Trạng thái (`.badge`, see `Badge.md`), Ghi chú tuần (`input.noteInput`).

## Variants

| Variant | Classes | Key values | When to use |
| --- | --- | --- | --- |
| Header cell | `th` | bg `colors.surface-table-header`, text `colors.text-table-header`, `position:sticky;top:0`, left-aligned | Column headers, stays pinned while the table body scrolls |
| Body cell (text) | `td` | `padding:9px 8px`, `font-size:12.5px`, `vertical-align:top` | Mã, Chỉ tiêu, Nhóm, Trạng thái, Ghi chú tuần |
| Body cell (numeric) | `td.num` | `text-align:right`, `font-variant-numeric:tabular-nums` | Điểm tối đa, Tuần trước, Tuần này, Tăng/giảm |

## States
<!-- Exactly these five rows, in this order — treatments as rendered by the shipped CSS. -->

| State | Treatment |
| --- | --- |
| default | 1px `colors.border` bottom-border per row/cell (`th,td{border-bottom:1px solid var(--line)}`); sticky header stays visible while `.tablewrap` scrolls |
| hover | **Not styled** — no `tr:hover` row-highlight rule exists anywhere in the stylesheet |
| focus | **N/A** at the table/row level — only the interactive cells inside (`Input` — see `Input.md`) are individually focusable |
| active | **N/A** — the table itself is not an interactive control |
| disabled | **N/A** — table structure is always rendered; individual cell inputs never carry `disabled` (see `Input.md`) |

## Tokens Used
- `colors.surface-table-header`, `colors.text-table-header`, `colors.border`, `colors.surface`
- `rounded.table`
- `typography.table-cell` (12.5px)

## Reference markup

```html
<div class="tablewrap">
 <table>
  <thead><tr>
   <th style="width:65px">Mã</th><th style="min-width:350px">Chỉ tiêu</th><th>Nhóm</th><th class="num">Điểm tối đa</th>
   <th class="num">Tuần trước</th><th class="num">Tuần này</th><th class="num">Tăng/giảm</th><th>Trạng thái</th><th>Ghi chú tuần</th>
  </tr></thead>
  <tbody id="tbody"></tbody>
 </table>
</div>
```

Sources: `doc/Prototype/dashboard.html:39-43` (CSS), `doc/Prototype/dashboard.html:117-126` (header markup), `doc/Prototype/dashboard.html:872-896` (`renderTable()`, builds each `<tr>`)

## Do / Don't

- ✅ Keep the table at `table-min-width` (1200px) and let `.tablewrap` scroll horizontally — this is the shipped, intentional behavior at every viewport (no responsive column-collapse exists).
- ✅ Keep the header sticky (`position:sticky;top:0`) so column labels stay visible during vertical scroll.
- ❌ Don't add a row-hover highlight without flagging it as a Normalize item — none exists in the current app.
- ❌ Don't add pagination — the shipped table always renders the full filtered set (up to 62 rows) at once.

## Normalize on redesign
1. No responsive/adaptive treatment for narrow viewports — the table always requires horizontal scrolling below ~1200px (see `Tokens/spacing.md` § Structural Measurements, `table-min-width`).
2. No row-hover affordance — large data tables typically benefit from one; currently absent.

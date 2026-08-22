---
project: "PlatformManager"
status: "draft"
updated: "2026-08-22"
component: "DataTable"
sources:
  - "src/FE/src/app/platform/quan-tri-nguoi-dung/components/user-grid-table/user-grid-table.html"
  - "src/FE/src/app/platform/quan-tri-nguoi-dung/components/user-grid-table/user-grid-table.ts"
  - "src/FE/src/app/platform/quan-tri-nguoi-dung/components/user-grid-table/user-grid-table.scss"
  - "src/FE/src/app/modules/danh-muc-dti/components/criteria-grid-table/criteria-grid-table.html"
  - "src/FE/src/app/modules/danh-muc-dti/components/criteria-grid-table/criteria-grid-table.ts"
  - "src/FE/src/styles.scss"
  - "src/FE/src/app/core/theme/platform-manager-preset.ts"
---

# DataTable
**Description:** The PrimeNG `p-table` server-side data grid as shipped — `[lazy]` paging with a 10/20/50 rows-per-page selector, a built-in loading mask, a custom empty message, and (on the criteria grid) left- and right-frozen columns. Two instances exist. It is distinct from `Table.md`, which documents the hand-rolled `<table>` markup; both share the same global `th`/`td` cell rules.

## Anatomy

`<p-table>` with three named templates and no custom PrimeNG CSS anywhere in the app:

- **`<ng-template #header>`** — one `<tr>` of `<th>`s, each carrying an inline `style="min-width:…"`. Painted by the global `th` rule: fill `colors.surface-table-header`, text `colors.text-table-header`, `typography.table-header`, `position:sticky; top:0`, `z-index:4`, left-aligned, `letter-spacing:0.01em`.
- **`<ng-template #body let-row>`** — one `<tr>` per record. Cells take the global `th, td` rule: bottom border 1px `colors.line`, padding `spacing.cell-padding`, `typography.table-cell`, `line-height:1.4`, `vertical-align:top`. `.num` cells add `text-align:right` + `font-variant-numeric:tabular-nums`.
- **`<ng-template #emptymessage>`** — a single `<tr>` with one `colspan`-ed `.muted` cell.

Row rhythm is also global: `tbody tr:nth-child(even)` gets `colors.surface-table-header` and `tbody tr:hover` gets `colors.bg`.

**Paging** is entirely server-side. `[lazy]="true"` suppresses PrimeNG's client-side slicing; `[first]` is derived as `(page() - 1) * pageSize()` and `[totalRecords]` comes from the API. `onLazyLoad` converts PrimeNG's zero-based `first`/`rows` back into a 1-based page and re-emits it as `pageChange` — neither component calls a service itself.

**Theming** comes from `PlatformManagerPreset` (`providePrimeNG({ theme: { preset, options: { darkModeSelector: false } } })`), which derives Aura's 50→950 ramps from the app's own token values, pinning step 500 to the token itself. That preset — not any stylesheet in the app — is what colours the paginator, the loading mask and the frozen-column shadows. Verified: no `p-datatable`, `p-paginator` or frozen-column selector appears in any SCSS file in `src/FE/src/app`, and the app's single `::ng-deep` (`kpi-summary.scss:18`) targets `.card` on the dashboard, not the grid.

## Variants

| Variant | Classes / bindings | Key values | When to use |
| --- | --- | --- | --- |
| Wrapped grid | `.tablewrap > p-table` | Wrapper: border 1px `colors.border-strong`, radius `rounded.table`, `overflow:hidden` | User grid — 5 columns, `spacing.user-grid-min-width` (690px), default page size 10, `dataKey="Id"` |
| Bare scrollable grid | `p-table[scrollable] styleClass="dti-grid"` | No `.tablewrap`; `:host { display:block }` only. `[scrollHeight]` is a computed pixel string | Criteria grid — 12 columns, `spacing.criteria-grid-min-width` (1430px), default page size 20, `dataKey="CriteriaId"` |
| Frozen left column | `<th pFrozenColumn>` / `<td pFrozenColumn>` | Column 1 (`Mã`), `min-width:70px` | Criteria grid only — keeps the record code visible across 1430px of horizontal scroll |
| Frozen right column | `pFrozenColumn alignFrozen="right"` | Column 12 (`Hành động`), `min-width:120px` | Criteria grid only — keeps the row actions reachable |
| Numeric column | `<th class="num">` / `<td class="num">` | Right-aligned, tabular figures | Điểm tối đa, Tự đánh giá, Thẩm định, Tiến độ % |
| Loading | `[loading]="loading()"` | PrimeNG's built-in overlay mask + spinner, coloured by the Aura preset; not styled or overridden by the app | Any fetch, including page changes and post-mutation refetches |
| Empty | `#emptymessage` | One `.muted` cell spanning all columns | Filter matched nothing |
| Paginator | `[paginator]="true" [rowsPerPageOptions]="[10, 20, 50]"` | Rendered by PrimeNG at the bottom of the table; **not** marked `no-print` | Always — both grids paginate unconditionally |

Height behaviour differs by instance. The user grid grows with its rows. The criteria grid measures itself after render — `window.innerHeight - hostTop - 160`, floored at `320` — and re-measures on `resize`, so the body scrolls inside a viewport-fitted box while the header stays pinned.

## States
<!-- Exactly these five rows, in this order — treatments as rendered by the shipped CSS. -->

Primary unit: the body row (`tbody tr`).

| State | Treatment |
| --- | --- |
| default | Bottom hairline `colors.line` per cell, padding `spacing.cell-padding`, `typography.table-cell`; even rows tinted `colors.surface-table-header`; header pinned at `top:0` on `colors.surface-table-header` |
| hover | `background: colors.bg` on the whole row (`styles.scss:396-398`) — a global rule, so it applies to both grids and to the hand-rolled matrices alike |
| focus-visible | **Not applicable at row level** — rows carry no `tabindex` and no `:focus-visible` rule is authored for `tr`. Focus lands on the controls *inside* a row (`CellIconButton.md`, `ActionButton.md`) and on the paginator buttons, which draw their ring from the Aura preset, not from app CSS |
| active / selected | **Not applicable — row selection is not enabled.** Neither instance sets `selectionMode`, `[(selection)]` or `[selectionMode]`; `dataKey` is present for row identity/tracking only. There is consequently no selected-row fill in the app, and PrimeNG's selection styles never activate |
| disabled | **Not applicable — a grid has no disabled state, and the app never fakes one.** Read-only mode is expressed *per row* by not rendering the row's controls (`@if (row.IsEditable)` → otherwise `<span class="muted">—</span>`), and in-flight state is expressed by the loading mask. No `:disabled` rule exists for any table element |

## Tokens Used
- `colors.card` (table background), `colors.line` (cell rules), `colors.surface-table-header` (header fill + zebra), `colors.text-table-header`, `colors.bg` (row hover), `colors.border-strong` (`.tablewrap` edge), `colors.muted` (empty message)
- `rounded.table`
- `spacing.cell-padding`, `spacing.user-grid-min-width`, `spacing.criteria-grid-min-width`
- `typography.table-header` (11px/700), `typography.table-cell` (12px/400)
- Layer: `z-index:4` on the sticky `th`
- Indirect: every PrimeNG-rendered part (paginator, loading mask, frozen shadows) resolves through `PlatformManagerPreset`, whose ten base constants mirror `:root` exactly

## Reference markup

```html
<!-- User grid: wrapped, five columns, page size 10 -->
<div class="tablewrap">
  <p-table
    [value]="rows()" [loading]="loading()" [lazy]="true" [paginator]="true"
    [rows]="pageSize()" [first]="(page() - 1) * pageSize()" [totalRecords]="totalCount()"
    [rowsPerPageOptions]="[10, 20, 50]" (onLazyLoad)="onLazyLoad($event)" dataKey="Id">
    <ng-template #header>
      <tr>
        <th style="min-width:220px">Người dùng</th>
        <th style="min-width:140px">Vai trò</th>
        <th style="min-width:120px">Trạng thái</th>
        <th style="min-width:110px">Ngày tạo</th>
        <th style="min-width:100px; text-align:right">Hành động</th>
      </tr>
    </ng-template>
    <ng-template #body let-row> … </ng-template>
    <ng-template #emptymessage>
      <tr><td colspan="5" class="muted">Không có người dùng nào khớp bộ lọc.</td></tr>
    </ng-template>
  </p-table>
</div>

<!-- Criteria grid: scrollable, twelve columns, first and last frozen -->
<p-table … [scrollable]="true" [scrollHeight]="gridHeight() + 'px'" dataKey="CriteriaId" styleClass="dti-grid">
  <ng-template #header>
    <tr>
      <th pFrozenColumn style="min-width:70px">Mã</th>
      …
      <th pFrozenColumn alignFrozen="right" style="min-width:120px">Hành động</th>
    </tr>
  </ng-template>
  <ng-template #emptymessage>
    <tr><td colspan="12" class="muted">Không có chỉ tiêu nào khớp bộ lọc.</td></tr>
  </ng-template>
</p-table>
```

```ts
/** Server-side pagination — PrimeNG's zero-based event converted back to a 1-based page. */
onLazyLoad(event: TableLazyLoadEvent): void {
  const rows = event.rows ?? this.pageSize();
  const first = event.first ?? 0;
  this.pageChange.emit({ Page: Math.floor(first / rows) + 1, PageSize: rows });
}
```

Column headers, verbatim. User grid: `Người dùng` · `Vai trò` · `Trạng thái` · `Ngày tạo` · `Hành động`. Criteria grid: `Mã` · `Tên` · `Nhóm` · `Điểm tối đa` · `Tự đánh giá` · `Thẩm định` · `Trạng thái` · `Phụ trách` · `Hạn xử lý` · `Tiến độ %` · `Ghi chú` · `Hành động`.

Sources: `src/FE/src/app/platform/quan-tri-nguoi-dung/components/user-grid-table/user-grid-table.html:1-80`, `src/FE/src/app/platform/quan-tri-nguoi-dung/components/user-grid-table/user-grid-table.scss:1-5`, `src/FE/src/app/platform/quan-tri-nguoi-dung/components/user-grid-table/user-grid-table.ts:34-45`, `:68-73`, `src/FE/src/app/modules/danh-muc-dti/components/criteria-grid-table/criteria-grid-table.html:1-31`, `:138-143`, `src/FE/src/app/modules/danh-muc-dti/components/criteria-grid-table/criteria-grid-table.ts:61-71`, `:80-94`, `:132-138`, `src/FE/src/app/modules/danh-muc-dti/components/criteria-grid-table/criteria-grid-table.scss:1-3`, `src/FE/src/styles.scss:365-403` (global table/cell/zebra/hover rules), `src/FE/src/app/app.config.ts:22-29` (theme wiring), `src/FE/src/app/core/theme/platform-manager-preset.ts:60-69` (the ten mirrored constants), `src/FE/package.json:33-36` (primeng 20.2)

## Do / Don't

- ✅ Keep grids dumb. Both components take `rows`/`loading`/`totalCount`/`page`/`pageSize` as inputs and emit `pageChange` plus row-action outputs; the smart page owns every API call and refetch.
- ✅ Keep `[lazy]="true"` paired with `[totalRecords]` and a derived `[first]`. Dropping `lazy` makes PrimeNG slice the current page again, showing 10 of the 10 rows you already fetched.
- ✅ Declare column widths as inline `min-width` on the `<th>`. That is the shipped mechanism — there is no global table `min-width` and no column-config object.
- ✅ Reach for `pFrozenColumn` only when the row genuinely exceeds the viewport; the 690px user grid deliberately does not use it.
- ✅ Let the Aura preset colour PrimeNG's own chrome. Every value in it is derived from `:root`, so overriding it in a stylesheet creates a second source of truth.
- ❌ Don't use `p-table` for the two permission matrices — those are hand-rolled `<table>` **on purpose** (see `Table.md`); they need a full checkbox matrix, no paging and a `max-height` scroll box, none of which `p-table` was adding value for.
- ❌ Don't add row selection styling; no grid enables selection, so a selected-row treatment would never appear.
- ❌ Don't disable a grid to express read-only — hide the row's controls instead.

## Normalize on redesign
1. **Only one of the two grids is wrapped in `.tablewrap`.** The user grid gets the `colors.border-strong` frame and `rounded.table`; the criteria grid has no frame at all, so the two data screens are visibly inconsistent at the container level.
2. **The paginator is not marked `no-print`** while the filter bar above it is, so printed grids carry page controls.
3. **The criteria grid's height formula hard-codes `- 160`** as "room for pagination and card padding" and re-runs on every `resize` event with no debounce.
4. **Column min-widths are inline styles**, split across twelve `<th>` elements — the reason `Tokens/spacing.md` has to record the grid widths as a *sum*. A column config would make the total explicit.
5. **PrimeNG chrome is invisible to this design system.** Paginator spacing, mask opacity and frozen-column shadows are all Aura defaults derived from the preset; none is recorded as a token, so a PrimeNG upgrade can move them silently.
6. **Two different default page sizes** (10 and 20) for two grids with the same paginator options.

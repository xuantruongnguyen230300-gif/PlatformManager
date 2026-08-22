---
project: "PlatformManager"
status: "draft"
updated: "2026-08-22"
component: "FilterBar"
sources:
  - "src/FE/src/styles.scss"
  - "src/FE/src/app/modules/danh-muc-dti/pages/danh-muc-dti/danh-muc-dti.page.html"
  - "src/FE/src/app/modules/danh-muc-dti/pages/danh-muc-dti/danh-muc-dti.page.scss"
  - "src/FE/src/app/platform/quan-tri-nguoi-dung/pages/quan-tri-nguoi-dung/quan-tri-nguoi-dung.page.html"
  - "src/FE/src/app/platform/quan-tri-nguoi-dung/pages/quan-tri-nguoi-dung/quan-tri-nguoi-dung.page.scss"
---

# FilterBar
**Description:** The wrapping row of grid filters that sits between a card's `.title` and its data grid (`.filters`, global) — search box, dropdowns, and an optional right-aligned action cluster (`.filters-actions`). It also owns the `.filters input/select` field rule (the "filter tier" cross-documented in `Input.md`) and the icon-prefixed `.search` wrapper.

## Anatomy

`.filters` — row flex, `flex-wrap:wrap`, `align-items:center`, gap `spacing.sp-3`, margin `spacing.sp-4` 0. It paints nothing: no background, no border, no padding. A nested rule gives every direct-or-descendant `<input>` `flex:1` and `min-width: spacing.filter-input-min-width` (220px), so the text field is the element that absorbs the leftover width while the selects stay at their content size.

**Fields** — `.filters input`, `.filters select` (and, by the same rule, `.weekbar input` / `.weekbar select`, which belong to the dashboard's period toolbar): border 1px `colors.border-strong`, radius `rounded.sm`, fill `colors.card`, padding `spacing.sp-2` `spacing.sp-3`, `--fs-sm`. The heavier border is deliberate — `styles.scss:351-352` records that inputs are the one place a visible border still earns its keep ("this is where you type"), after buttons and cards moved to fill-and-shadow.

**`.search`** (component-scoped, user-admin screen only) — a `position:relative` wrapper with `flex:1` and `min-width: spacing.filter-input-min-width`, holding a leading `pi pi-search` glyph absolutely positioned at `left:10px`, vertically centred by `top:50%; transform:translateY(-50%)`, in `colors.muted` at `13px` literal. Its `<input>` takes `width:100%` and `padding-left:32px` to clear the glyph, overriding the shorthand padding from the shared field rule.

**`.filters-actions`** — row flex, gap `spacing.sp-3`, `margin-left:auto`, `flex:none`. The `margin-left:auto` is what pushes the cluster to the right edge of the wrapping row; `flex:none` keeps it from being squeezed by the `flex:1` text field.

## Variants

| Variant | Classes | Key values | When to use |
| --- | --- | --- | --- |
| Search-only bar | `filters` | One `.search` wrapper, nothing else | `/quan-tri/nguoi-dung` |
| Multi-filter bar | `filters no-print` | One bare `<input>` + three `<select>`s | `/danh-muc/dti` — search, group, year, period |
| With action cluster | `filters` + `filters-actions` | Cluster pinned right by `margin-left:auto` | `/danh-muc/dti` while `isLive()` — `Import CSV/Excel` (`.btn`) and `+ Thêm chỉ tiêu` (`.btn.primary`) |
| Action cluster stacked | `.filters-actions` @ `spacing.breakpoint-mobile` | `margin-left:0`, `width:100%` — the cluster drops to its own full-width line | ≤560px, declared in `danh-muc-dti.page.scss:14-19` |
| Read-only bar | `filters` without `filters-actions` | Cluster not rendered at all | `/danh-muc/dti` when viewing a past period — the whole `@if (isLive())` block is skipped, matching the row-level rule in `ActionButton.md` |
| Icon-prefixed field | `.search > i.pi + input` | 32px left inset, glyph at `left:10px` | Free-text search on the user-admin screen. The criteria screen's search input is **not** wrapped and has no glyph |

## States
<!-- Exactly these five rows, in this order — treatments as rendered by the shipped CSS. -->

`.filters` is a layout container; the interactive states below belong to its fields.

| State | Treatment |
| --- | --- |
| default | Transparent wrapping flex row, gap `spacing.sp-3`, margin `spacing.sp-4` 0; no surface of its own |
| hover | **Not applicable** — `styles.scss` authors no `.filters:hover`, and no `:hover` rule exists for `.filters input`/`.filters select` either, so the fields do not change on hover. (PrimeNG's `formField.hoverBorderColor` in the theme preset applies to PrimeNG-rendered controls, not to these native ones.) |
| focus-visible | **Not applicable** to the container — it has no `tabindex`. Its fields do: `outline: 2px solid colors.brand`, `outline-offset: 1px` (`styles.scss:359-362`) |
| active / selected | **Not applicable** — the bar has no selected concept and no `:active` rule; filters express their state through the field's own value, not through a styled chip or pill |
| disabled | **Not applicable** — no filter control is ever disabled in the shipped app: `grep` finds no `[disabled]` binding on any `.filters` field, and no `:disabled` rule exists for the filter tier. When filtering is not allowed (past periods) the app hides the *actions*, never the filters |

### States — `.filters input` / `.filters select` (supplementary; same rule documented as the filter tier in `Input.md`)

| State | Treatment |
| --- | --- |
| default | Border 1px `colors.border-strong`, radius `rounded.sm`, fill `colors.card`, padding `spacing.sp-2` `spacing.sp-3`, `--fs-sm` |
| hover | **Not styled** — no `:hover` rule for these selectors anywhere in `src/FE` |
| focus-visible | `outline: 2px solid colors.brand`, `outline-offset: 1px`; the border colour does **not** change (unlike the auth field — see `AuthField.md`) |
| active / selected | **Not applicable** — native `<select>` option highlighting is UA-rendered; nothing is authored |
| disabled | **Not applicable** — never set in markup, no `:disabled` rule authored |

## Tokens Used
- `colors.card`, `colors.border-strong`, `colors.brand`, `colors.muted`
- `rounded.sm`
- `spacing.sp-2`, `spacing.sp-3`, `spacing.sp-4`, `spacing.filter-input-min-width`, `spacing.breakpoint-mobile`
- `typography.table-cell` size (`--fs-sm`) on the fields
- Un-tokenised literals: the search glyph's `left:10px` and `13px` size, and the input's `padding-left:32px` — catalogued in `Tokens/spacing.md`
- Icons: PrimeIcons v7 — `pi-search`

## Reference markup

```html
<!-- /quan-tri/nguoi-dung — search only -->
<div class="filters">
  <div class="search">
    <i class="pi pi-search"></i>
    <input type="text" placeholder="Tìm theo tên hoặc email..." [value]="searchInput()" (input)="onSearchInputEvent($event)" />
  </div>
</div>

<!-- /danh-muc/dti — four filters plus a right-aligned action cluster -->
<div class="filters no-print">
  <input placeholder="Tìm mã hoặc tên chỉ tiêu..." [value]="searchInput()" (input)="onSearchInputEvent($event)" />
  <select [value]="selectedGroupId()" (change)="onGroupSelectChange($event)">
    <option value="">Tất cả nhóm</option>
    …
  </select>
  <select [value]="selectedYear()" title="Chọn năm" (change)="onYearSelectChange($event)">…</select>
  <select [value]="selectedPeriod()"
    title="Xem tổng hợp cả năm (mới nhất mỗi chỉ tiêu) hoặc 1 kỳ cụ thể đã lưu trong năm"
    (change)="onPeriodSelectChange($event)">
    <option value="all">Tất cả (mới nhất trong năm)</option>
    …
  </select>
  @if (isLive()) {
    <div class="filters-actions">
      <button type="button" class="btn" (click)="openImportDialog()">Import CSV/Excel</button>
      <button type="button" class="btn primary" (click)="openCreateForm()">+ Thêm chỉ tiêu</button>
    </div>
  }
</div>
```

Sources: `src/FE/src/styles.scss:327-338` (`.filters`), `:340-345` (`.filters-actions`), `:347-363` (field rule + focus), `src/FE/src/app/modules/danh-muc-dti/pages/danh-muc-dti/danh-muc-dti.page.html:13-42`, `src/FE/src/app/modules/danh-muc-dti/pages/danh-muc-dti/danh-muc-dti.page.scss:7-19`, `src/FE/src/app/platform/quan-tri-nguoi-dung/pages/quan-tri-nguoi-dung/quan-tri-nguoi-dung.page.html:7-12`, `src/FE/src/app/platform/quan-tri-nguoi-dung/pages/quan-tri-nguoi-dung/quan-tri-nguoi-dung.page.scss:1-19`

## Do / Don't

- ✅ Put the bar inside the `.card` that owns the grid, directly under the `.title` row — both shipped screens do, and the `spacing.sp-4` vertical margin is tuned for that position.
- ✅ Let exactly one text `<input>` carry `flex:1`; the selects size to content. Two flexible fields make the row rebalance every time an option list changes.
- ✅ Use `colors.border-strong` here and nowhere else outside inputs and `.tablewrap` — that boundary is the whole point of the two-tier border system (`DESIGN.md` § Do's and Don'ts).
- ✅ Hide the *action cluster*, not the filters, when the view is read-only.
- ✅ Mark the bar `no-print` when it carries actions — the criteria screen does; the user-admin screen does not.
- ❌ Don't add a background or border to `.filters` itself; it is deliberately a bare flex row on the card surface.
- ❌ Don't style a filter's "has a value" state — no such affordance ships, and adding one silently changes what a printed or screenshotted view means.

## Normalize on redesign
1. **`.filters-actions` is declared twice, identically** — `styles.scss:340-345` and `danh-muc-dti.page.scss:7-12` (same four properties). Only the `≤560px` override is genuinely local. Delete the component copy.
2. **The user-admin `.filters` is not marked `no-print`** while the criteria one is, so the search box prints on one screen and not the other.
3. **Only one of the two search fields has a `pi-search` glyph.** The criteria screen's search is a bare input with the same role — the `.search` wrapper should either be shared or dropped.
4. **`.search` re-types `min-width:220px`** that the global `.filters input` rule already provides, as a literal in both places.
5. **The fields have no hover feedback at all**, which leaves the border as the only "you can type here" cue — thin for a control that is also the primary way to narrow a 12-column grid.

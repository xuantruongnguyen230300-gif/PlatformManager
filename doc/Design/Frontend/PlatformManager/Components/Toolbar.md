---
kind: luat
scope: du-an
verified: 2026-09-06
project: "PlatformManager"
status: "draft"
updated: "2026-09-06"
component: "Toolbar"
sources:
  - "src/FE/src/styles.scss"
  - "src/FE/src/app/shared/components/toolbar/toolbar.ts"
  - "src/FE/src/app/shared/components/toolbar/toolbar.html"
  - "src/FE/src/app/shared/components/toolbar/toolbar.scss"
  - "src/FE/src/app/platform/quan-tri-nguoi-dung/pages/quan-tri-nguoi-dung/quan-tri-nguoi-dung.page.html"
  - "src/FE/public/i18n/vi.json"
---

# Toolbar
**Description:** The band that sits between a card's `.title` and its data grid and gathers everything that *acts on the list below it*: a search field, a **Lọc** button that drops a filter panel, a strip of removable chips for the conditions currently applied, and a right-aligned action cluster. Unlike the row it replaced, it is a **surface of its own** — its own fill, border and radius — not a loose row of controls floating on the card.

> **This component replaced `FilterBar`.** Before the redesign of **2026-08-29** the app had three different answers to "the row above a grid": `.weekbar` on the dashboard (`margin-bottom: sp-5`), `.filters` on the DTI catalogue (`margin: sp-4 0`), and no wrapper at all on the user list — the reason is recorded in the comment above `styles.scss` § `.toolbar` (`grep -n 'weekbar' src/FE/src/styles.scss`). `Components/FilterBar.md` documented `.filters` and was deleted on the same date; `.filters` no longer exists as a selector anywhere in `src/FE/src`.
>
> Two behavioural decisions came with the merge, both recorded in the comment above `styles.scss` § `.toolbar`:
> 1. **The search box no longer takes `flex: 1`.** A 900px-wide search field does not make anyone type faster; it only pushes the filter button and the action cluster to opposite ends of the screen. It is pinned to `dimension.toolbar-search-width` (260px).
> 2. **Filter conditions moved off the bar and into a dropdown.** Laying every condition out horizontally is workable at two conditions and stops working at three — real data grids put them behind a button, which is what ships.

## Anatomy

Two things carry this name, and they are the same shape from two directions:

- **The global class `.toolbar`** (`styles.scss` § `.toolbar`) — usable directly on a `<div>`. No page does that any more; the user-management page moved onto the component.
- **The Angular component `<app-toolbar>`** (`src/FE/src/app/shared/components/toolbar/toolbar.ts`) — which puts `.toolbar no-print` on **its own host element** and supplies the search / filter / chips / actions structure through inputs and two content slots.

The host-class detail is deliberate and fragile enough that the source explains it (`toolbar.ts:22-25`): wrapping the markup in an extra `<div>` would slide an inline `<app-toolbar>` element between `.card` and `.toolbar`, breaking the flex chain that `.page-fill .toolbar { flex: none }` (`styles.scss` § `.page-fill .toolbar`) depends on — the bar would then shrink with the grid on a short viewport.

**Box.** `display:flex` · `gap: spacing.sp-3` · `flex-wrap: wrap` · `align-items:center` · fill `colors.bg` · border 1px `colors.line` · radius `radius.md` · padding `spacing.sp-3 spacing.sp-4` · `margin-bottom: spacing.sp-5`. The fill is the page background rather than `colors.surface-table-header`, and the comment inside `styles.scss` § `.toolbar` says why: at 1.16:1 against `colors.card` the table-header surface is very nearly invisible, so it would not read as a separate plane.

**Children, in DOM order:**

| Part | Class | Shape |
| --- | --- | --- |
| Search | `.input-icon.search` | Fixed `dimension.toolbar-search-width`, `flex:none`; a `pi-search` glyph and an `<input type="search">` on the shared field contract (`Input.md`) with a tighter left inset and the WebKit clear button suppressed — `styles.scss` § `.toolbar .search`, `.toolbar .input-icon input`, `.toolbar .input-icon > .pi` |
| Filter | `.filter` > `<summary class="btn">` + `.filter-panel` | A native `<details>` — open/close is browser behaviour, no state and no JavaScript (`toolbar.html:16-34`). The summary borrows the whole `Button.md` contract via `class="btn"`; the default marker is removed on both engines (`styles.scss` § `.filter > summary::-webkit-details-marker` and § `.filter > summary::marker`) |
| Active-condition counter | `.filter-count` | A pill on the Lọc button: `radius.pill`, min-width 16px × height 16px, `colors.brand` fill, `colors.on-primary` text, `typography.filter-count`. **Its job is to say the list is filtered while the panel is shut** — the source calls omitting it "the classic mistake of this kind of toolbar" (`styles.scss` § `.filter-count`) |
| Separator | `.toolbar-sep` | A 1px `colors.line` hairline, `align-self:stretch`, between the search/filter group and the action group |
| Applied chips | `.filter-chips` > `.filter-chip` | `radius.pill` tonal chips (`colors.tonal-bg` fill, 1px `colors.line`, `colors.tonal-ink` text, `typography.filter-chip`), each ending in a 16px `IconButton` that removes that one condition |
| Actions | `.toolbar-actions` | `margin-left:auto`, `flex:none`, `gap: spacing.sp-3`. `:empty { display:none }` (`styles.scss` § `.toolbar-actions:empty`) — the component always renders the wrapper because Angular cannot ask "did anyone project into `[actions]`?", and at ≤`breakpoint.mobile` the empty wrapper would otherwise claim `width:100%` and leave a blank row |

**The filter panel** (`styles.scss` § `.filter-panel`, with § `.filter-foot`): absolutely positioned `6px` below the button, `z-index:50`, `dimension.filter-panel-width` wide, `colors.card` fill, 1px `colors.line`, `radius.md`, `shadow.panel`, `padding: spacing.sp-5`, left-aligned. It is filled with `FormRow.md` groups (last one loses its bottom margin) and closed by `.filter-foot` — `justify-content: space-between`, separated by a `spacing.sp-4` top pad over a 1px `colors.line` rule, holding **Xoá lọc** on the left and a `.btn.primary` **Áp dụng** on the right.

One rule inside the panel exists purely to undo a cascade accident (`styles.scss` § `.filter-panel select`): the panel is *inside* `.toolbar`, so `.toolbar select { width:auto }` — declared later — beat `.form-row select { width:100% }` and left the panel's selects ragged, sized by their longest option. Width is restored explicitly.

### Component API

`<app-toolbar>` — standalone, `OnPush`, host `class="toolbar no-print"` (`toolbar.ts:45`).

| Kind | Name | Type / default | Notes |
| --- | --- | --- | --- |
| input | `showSearch` | `boolean` = `true` | Set false for a bar that only carries actions |
| input | `searchPlaceholder` | `string` = `''` | Empty **on purpose**, not an unfilled default (`toolbar.ts:71`): the parent passes an already-translated sentence, so this input must not also carry a translation key. Empty ⇒ the template falls back to `shared.toolbar.searchPlaceholder` through `\| translate` at the point of display, so the default follows the language switcher without the component injecting anything (G4 keeps `components/` dumb) |
| input | `searchAriaLabel` | `string` = `''` → `shared.toolbar.searchAriaLabel` | There is no visible `<label>` — the bar has no room — so the accessible name must come through here. Pages are expected to override it with the thing being searched |
| input | `searchDisabled` | `boolean` = `false` | Blocks typing, not just the colour |
| model | `searchValue` | `string` = `''` | Two-way, `[(searchValue)]`. **The component does not debounce**; each page picks its own delay (server call vs. in-memory filter), and baking one in would force every page onto it (`toolbar.ts:85`) |
| input | `hasFilter` | `boolean` = `false` | Must be explicit for the same reason `.toolbar-actions:empty` exists — Angular cannot detect projection, and without the flag every bar would grow an empty Lọc button |
| input | `filterCount` | `number` = `0` | Renders `.filter-count` when > 0 |
| input | `filterClearLabel` | `string` = `''` → `shared.toolbar.clearFilter` | Same empty-default contract as the two search labels |
| input | `filterApplyLabel` | `string` = `''` → `shared.toolbar.applyFilter` | Same |
| input | `chips` | `readonly IToolbarChip[]` = `[]` | `IToolbarChip = { Key: string; Label: string }`. `Key` is an opaque identifier handed back on removal — the toolbar deliberately understands nothing about filter semantics (`toolbar.ts:9-12`) |
| output | `chipRemove` | `string` | The `Key` of the chip whose × was pressed |
| output | `filterApply` | `void` | Emitted **after** the panel closes — the parent usually refetches and re-renders, and a panel left hanging over changed data is not a state anyone wants (`toolbar.ts:120-123`) |
| output | `filterClear` | `void` | Same close-then-emit order |
| method | `closeFilterPanel()` | `void` | Public. Native `<details>` closes on neither outside-click nor Escape; both are added by hand |
| slot | `[filter]` | content | Projected into `.filter-panel`, above `.filter-foot` |
| slot | `[actions]` | content | Projected into `.toolbar-actions` |

Escape is bound at **document** level, not on the `<details>` (`toolbar.ts:51`): while the panel is open, focus may be on a field inside it, on the Lọc button, or nowhere at all after a click on empty space. Binding to the element catches only the middle case — and would require giving a non-interactive wrapper a `tabindex`, which the a11y lint rule blocks for good reason. The outside-click handler ignores clicks landing inside `<details>`, otherwise the very click that opens the panel would close it again on the way up to `document` (`toolbar.ts:136-144`).

The component's stylesheet is **intentionally empty** (`toolbar.scss:1-8`): the filter body and the action buttons arrive through `<ng-content>`, and Angular's scoped styles do not pierce projected content, so any rule written there would be a second, silently-diverging copy that also would not apply.

## Variants

| Variant | Classes / inputs | Key values | When to use |
| --- | --- | --- | --- |
| Full bar | `.toolbar` with search + filter + chips + actions | All parts present, `.toolbar-sep` visible | A server-paged list with more than two filter conditions — the user-management list is the shipped example |
| Search + actions | `showSearch` true, `hasFilter` false | No `.filter`, separator still drawn (`showSeparator = showSearch() ‖ hasFilter()`) | A list filtered by text alone |
| Actions only | `showSearch` false, `hasFilter` false | No separator; `.toolbar-actions` is the only child | A card whose header actions need the toolbar surface |
| Growing control | any child with `.grow` | `flex:1`, `min-width:200px` (`styles.scss` § `.toolbar .grow`) | An **opt-in** escape hatch for a control that genuinely should stretch — the search box is deliberately not one |

**Responsive** (`styles.scss` § `@media (max-width: 560px)` — the block that opens with `.toolbar .search`, at `breakpoint.mobile`): the search box goes full-width; every direct child gets `flex:1` **except** `.filter` and `.filter-chips`, which keep `flex:none` because they are not input fields and stretching them is meaningless; `.toolbar-actions` loses `margin-left:auto` and takes the full row; `.toolbar-sep` is hidden; and the panel narrows to `min(320px, 86vw)`.

## States
<!-- Exactly these five rows, in this order — treatments as rendered by the shipped CSS. -->

| State | Treatment |
| --- | --- |
| default | Bar closed: `colors.bg` plate, 1px `colors.line`, `radius.md`. Lọc button in its resting `.btn` tonal fill, `.filter-count` present only when conditions are applied, panel not rendered on screen |
| hover | **The bar itself has no hover rule** — it is a container. Its parts each hover on their own contract: `.btn` on the Lọc summary (`colors.btn-hover-bg` + `shadow.btn-hover`), `IconButton` on a chip's × (inverting to a `colors.brand` fill because grey on a tonal chip reads as a fault, `styles.scss` § `.filter-chip .icon-btn:hover`), and the shared input rule on the search field |
| focus-visible | **No ring on the container.** The `<summary>` takes `.btn`'s `2px solid colors.brand` ring at `outline-offset: 2px`; the search input takes the field contract's `colors.brand` border plus `shadow.focus-ring`; chip removes take `IconButton`'s tighter 1px offset. Tab order follows DOM order: search → Lọc → chip removes → actions |
| active / selected | **This is the real one: `.filter[open] > summary` inverts** to a `colors.brand` fill with `colors.on-primary` text and a matching `colors.brand` border, and `.filter-count` inverts with it — `colors.on-primary` fill, `colors.brand` digits (`styles.scss` § `.filter[open] > summary` and § `.filter[open] > summary .filter-count`). The open panel is therefore visibly anchored to a filled button. There is no *selected* sense elsewhere; chips are removable, not toggleable |
| disabled | **Only the search field can be disabled**, via `searchDisabled` → the shared `:disabled` input treatment (`styles.scss` § `.input:disabled`). Neither `.toolbar`, `.filter` nor `.toolbar-actions` has a disabled treatment; a bar with nothing to offer is not rendered rather than dimmed. Projected buttons carry their own `[disabled]` |

## Tokens Used
- `colors.bg` (bar fill), `colors.line` (bar border, separator, panel border, `.filter-foot` rule, chip border), `colors.card` (panel fill)
- `colors.brand` + `colors.on-primary` (open-state summary, `.filter-count`, chip-remove hover), `colors.tonal-bg` + `colors.tonal-ink` (chip fill and text)
- `radius.md` (bar, panel), `radius.pill` (`.filter-count`, `.filter-chip`)
- `spacing.sp-2` (chip gap), `spacing.sp-3` (bar gap, bar vertical pad, action gap, `.filter-foot` gap), `spacing.sp-4` (bar horizontal pad, panel form-row gap, `.filter-foot` top pad), `spacing.sp-5` (bar bottom margin, panel padding, `.filter-foot` top margin)
- `dimension.toolbar-search-width` (260px), `dimension.filter-panel-width` (320px)
- `shadow.panel` (the dropdown)
- `typography.filter-count`, `typography.filter-chip`, `typography.button-label` (via `.btn` on the summary)
- Un-tokenised literals: `top: calc(100% + 6px)`, `z-index: 50`, `.toolbar-sep` `1px`, `.grow`'s `min-width:200px`, the counter's `16px` box and `0 4px` padding, the chip's `2px … 2px …` vertical padding, the panel's `min(320px, 86vw)` mobile width

## Reference markup

```html
<app-toolbar
  [searchValue]="searchInput()"
  (searchValueChange)="onSearchValueChange($event)"
  [searchPlaceholder]="'quan-tri-nguoi-dung.filter.searchPlaceholder' | translate"
  [searchAriaLabel]="'quan-tri-nguoi-dung.filter.searchAriaLabel' | translate"
  [hasFilter]="true"
  [filterCount]="activeFilterCount()"
  [chips]="filterChips()"
  (filterApply)="onApplyFilters()"
  (filterClear)="onClearFilters()"
  (chipRemove)="onRemoveFilter($event)"
>
  <div filter class="form-row">
    <label for="userFilterRole">{{ 'shared.field.role' | translate }}</label>
    <select id="userFilterRole" [value]="roleDraft()" (change)="onRoleDraftChange($event)">
      <option value="">{{ 'quan-tri-nguoi-dung.filter.allRoles' | translate }}</option>
      <option value="SuperAdmin">SuperAdmin</option>
    </select>
  </div>

  <button actions type="button" class="btn primary" (click)="openCreateForm()">
    + {{ 'quan-tri-nguoi-dung.action.create' | translate }}
  </button>
</app-toolbar>
```

The shipped page puts the `filter` / `actions` slot markers **directly on the projected elements** rather than on an `<ng-container>` wrapper — the selectors are `[filter]` and `[actions]`, so both forms work, but the page is the one to copy. It also uses the one-way `[searchValue]` + `(searchValueChange)` pair instead of the `[(searchValue)]` banana box; `model()` supports both.

```scss
.toolbar {
  display: flex;
  gap: var(--sp-3);
  flex-wrap: wrap;
  align-items: center;
  background: var(--bg);
  border: 1px solid var(--line);
  border-radius: var(--radius-md);
  padding: var(--sp-3) var(--sp-4);
  margin-bottom: var(--sp-5);
}
.toolbar-actions { display: flex; gap: var(--sp-3); margin-left: auto; flex: none; }
.toolbar-actions:empty { display: none; }
.toolbar .search { width: 260px; flex: none; }
.toolbar-sep { width: 1px; align-self: stretch; background: var(--line); flex: none; }
.filter[open] > summary { background: var(--brand); border-color: var(--brand); color: var(--on-primary); }
```

Sources: `src/FE/src/styles.scss` § `.toolbar` through § `@media (max-width: 560px)`, § `.page-fill .toolbar` (the `.page-fill` flex contract), § `.toolbar .search` and § `.toolbar .input-icon input` (the toolbar search field), `src/FE/src/app/shared/components/toolbar/toolbar.ts:54-145`, `src/FE/src/app/shared/components/toolbar/toolbar.html:1-61`, `src/FE/src/app/shared/components/toolbar/toolbar.scss:1-8`, `src/FE/src/app/platform/quan-tri-nguoi-dung/pages/quan-tri-nguoi-dung/quan-tri-nguoi-dung.page.html:20-58`

> 🔄 **SỬA 2026-09-06** — bản trước sai ba nhóm:
> 1. **Bốn nhãn mặc định ghi là câu tiếng Việt** (`'Tìm kiếm…'`, `'Tìm kiếm'`, `'Xoá lọc'`, `'Áp dụng'`). Cả bốn nay mặc định `''`, và câu mặc định đến từ `shared.toolbar.*` ngay tại chỗ hiển thị trong template.
> 2. **Normalize #1 và #2 mô tả một bản chép tay đã biến mất** — trang Người dùng nay dùng `<app-toolbar>` thật. Xem hai mục đó.
> 3. **Mười ba neo `file:dòng` trỏ sai chỗ** sau khi `toolbar.ts` dài thêm phần chú thích i18n; đã neo lại theo bản hiện tại.

## Do / Don't

- ✅ Put **everything that acts on the list** on this surface — search, filter, and the "add" action alike. The user list moved its "+ Thêm người dùng" button down from `.title` for exactly this reason, leaving `.title` with only the section name and a count (`quan-tri-nguoi-dung.page.html:2-15`).
- ✅ Keep `no-print` on it. The bar is a control, not data; a printed page needs the table only (`doc/huong_dan/quy-uoc/fe-ui-conventions.md`).
- ✅ Show `.filter-count` whenever conditions are applied, and keep the chips *outside* the panel — both exist so a user can tell a filtered list from an unfiltered one without reopening anything.
- ✅ Render chips from the **applied** conditions, never from the panel's draft values. The panel edits a draft; **Áp dụng** promotes it.
- ✅ Set `searchAriaLabel` per page. The fallback `shared.toolbar.searchAriaLabel` is a placeholder, not an answer — the user list passes `quan-tri-nguoi-dung.filter.searchAriaLabel` instead.
- ❌ Don't wrap `<app-toolbar>`'s markup in an extra element or move `.toolbar` off the host — it breaks the `.page-fill` flex chain silently, with no compile error (`toolbar.ts:22-25`).
- ❌ Don't add rules to `toolbar.scss`. Scoped styles do not reach projected content; a shared class belongs in `styles.scss`.
- ❌ Don't lay filter conditions out flat on the bar again. That is the pattern this component replaced.
- ❌ Don't put a third button in `.filter-foot`. It is `space-between` for exactly two.

## Normalize on redesign
1. ~~**The component has no consumers yet.**~~ — **đã xử lý**, đối chiếu 2026-09-06. The user-management page now renders `<app-toolbar>` (`quan-tri-nguoi-dung.page.html:20-58`) and the hand-rolled `.toolbar` block is gone. Count consumers with a command rather than a number written here:
   ```bash
   grep -rn '<app-toolbar' src/FE/src/app
   ```
2. ~~**Two chip-remove glyphs are in play.**~~ — **đã xử lý** with #1. One glyph is left, `<i class="pi pi-times">` inside the component (`toolbar.html:52`), labelled from `shared.toolbar.removeFilter` with the chip label interpolated.
3. **The filter panel is not a focus trap and is not announced.** It has no `role`, no `aria-expanded` on the summary beyond what `<details>` implies, and focus is not moved into it on open nor returned to the button on Escape.
4. **`.filter-count` is unlabelled.** A bare digit next to "Lọc" reads as "Lọc 2" to a screen reader with no indication that 2 is a count of active conditions.
5. **`z-index: 50` is a bare literal** with no elevation scale behind it, and the app has no z-index registry — the dialog backdrop, the sidebar drawer and this panel each pick their own number.
6. **The 6px panel offset is off the spacing scale** (`spacing.sp-2` is 6px, so the value is right and the reference is missing).
7. **`.toolbar-sep` still draws when the search box is the only thing to its left and the action cluster is empty**, leaving a hairline against nothing; `showSeparator` tests the left side but never the right.

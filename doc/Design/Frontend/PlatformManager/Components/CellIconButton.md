---
project: "PlatformManager"
status: "draft"
updated: "2026-08-22"
component: "CellIconButton"
sources:
  - "src/FE/src/app/modules/danh-muc-dti/components/criteria-grid-table/criteria-grid-table.scss"
  - "src/FE/src/app/modules/danh-muc-dti/components/criteria-grid-table/criteria-grid-table.html"
  - "src/FE/src/app/modules/danh-muc-dti/components/criteria-grid-table/criteria-grid-table.ts"
---

# CellIconButton
**Description:** The 24px confirm/cancel pair that appears inside a grid cell while it is being edited (`.cell-icon-btn.ok` / `.cell-icon-btn.cancel`), together with the affordance that starts the edit (`.cell-editable`) and the flex box that holds them (`.cell-edit`). Component-scoped to the criteria grid — the only inline-edit surface in the app.

## Anatomy

**`.cell-editable`** — the resting affordance: a `<span>` with `cursor:pointer`, `border-bottom: 1px dashed transparent` and `padding-bottom:1px`. The transparent dashed border is reserved space, so revealing it on hover costs no layout shift. It carries `tabindex="0"`, `role="button"` and an explanatory `title`, and is activated by **double-click** or **Enter**.

**`.cell-edit`** — replaces the span while editing: row flex, `align-items:center`, gap `spacing.sp-2`. Its `input` takes `flex:1; min-width:0` so the two buttons never get squeezed out. The field itself is the table-cell input tier (`.progressInput` / `.noteInput`, documented in `Input.md`), auto-focused by the `appAutofocus` directive.

**`.cell-icon-btn`** — `spacing.cell-icon-btn` square (24×24), `border:1px solid transparent`, `background:transparent`, radius `6px` literal, `flex:none`, `padding:0`, `font-size:12px` / `line-height:1` literals, centred flex, `colors.muted`, `transition: background 0.15s ease, color 0.15s ease`. Two modifiers colour it: `.ok` → `colors.good` with a `colors.good-bg` hover fill; `.cancel` → `colors.bad` with a `colors.bad-bg` hover fill.

**Interaction model, as shipped:** double-click (not single-click) enters edit mode — a deliberate change from the prototype, recorded in `criteria-grid-table.ts:38-47`. While editing, `Enter` commits and `Escape` cancels, mirroring the two buttons exactly. Commit clamps the progress value to `[0, 100]` and emits an output; the smart page calls the API and refetches. Editing is offered only when `row.IsEditable`; otherwise the cell renders as plain text with no affordance at all.

## Variants

| Variant | Classes | Key values | When to use |
| --- | --- | --- | --- |
| Confirm | `cell-icon-btn ok` | `colors.good` glyph, hover fill `colors.good-bg`, icon `pi-check`, `title="Lưu"` | Commit the cell edit (equivalent to `Enter`) |
| Cancel | `cell-icon-btn cancel` | `colors.bad` glyph, hover fill `colors.bad-bg`, icon `pi-times`, `title="Huỷ"` | Discard the cell edit (equivalent to `Escape`) |
| Base (unmodified) | `cell-icon-btn` | `colors.muted` glyph, hover fill `colors.surface-2` | Declared but **not used in shipped markup** — every instance carries `.ok` or `.cancel`. The base is the fallback the class list resolves to, and the source of the shared geometry |
| Editable cell, numeric | `.cell-editable` in a `.num` cell | Renders `formatPercent(row.ProgressPercent)`; `title="Bấm đúp để sửa Tiến độ %"` | Tiến độ % column, when `row.IsEditable` |
| Editable cell, text | `.cell-editable` | Renders `row.Note` or the prompt `— bấm đúp để ghi chú`; `title="Bấm đúp để sửa Ghi chú"` | Ghi chú column, when `row.IsEditable` |
| Read-only cell | *(no wrapper)* | Bare interpolation — `formatPercent(...)` or `row.Note || '—'` | When `row.IsEditable` is false; the affordance is absent, not disabled |

## States
<!-- Exactly these five rows, in this order — treatments as rendered by the shipped CSS. -->

Primary interactive element: `.cell-icon-btn`.

| State | Treatment |
| --- | --- |
| default | 24px transparent square, `border:1px solid transparent`, radius `6px` literal, glyph in `colors.good` (`.ok`) or `colors.bad` (`.cancel`) |
| hover | Base fill `colors.surface-2` with `colors.text`; `.ok` overrides the fill to `colors.good-bg` and `.cancel` to `colors.bad-bg`. Because the modifier hover blocks are nested inside `.ok`/`.cancel`, they win on specificity and the glyph keeps its semantic colour rather than turning `colors.text` |
| focus-visible | `outline: 2px solid colors.brand`, `outline-offset: 1px` (`criteria-grid-table.scss:95-98`) — the same tight offset `ActionButton` uses for dense rows |
| active / selected | **Not applicable — no pressed or selected treatment exists.** No `:active` rule is authored, and neither button is a toggle: both are one-shot commands that immediately unmount the whole `.cell-edit` block by clearing `editingCell` |
| disabled | **Not applicable — the pair cannot exist in a disabled context.** No `:disabled` rule is authored in `criteria-grid-table.scss` and no `[disabled]` binding appears on either button. They render only inside `@if (isEditing(...))`, which can only be reached through `startEdit()`, which returns early unless `row.IsEditable`. A row that may not be edited never produces these buttons at all |

### States — `.cell-editable` (supplementary)

| State | Treatment |
| --- | --- |
| default | Plain cell text with `cursor:pointer` and a **transparent** 1px dashed bottom border holding the space |
| hover | `border-bottom-color: colors.brand` — the dashed underline appears, the only visual hint that the cell is editable |
| focus-visible | `outline: 2px solid colors.brand`, `outline-offset: 2px` |
| active / selected | **Not applicable** — no `:active` rule. The "activated" state is not a style but a swap: the span is replaced by `.cell-edit` |
| disabled | **Not applicable** — a non-editable cell renders as bare text without the `.cell-editable` wrapper, so there is nothing to disable |

## Tokens Used
- `colors.muted`, `colors.text`, `colors.surface-2`, `colors.good`, `colors.good-bg`, `colors.bad`, `colors.bad-bg`, `colors.brand`
- `spacing.sp-2` (gap in `.cell-edit`), `spacing.cell-icon-btn` (24×24)
- Un-tokenised literals: radius `6px`, `font-size:12px`, `line-height:1`, `padding-bottom:1px` — catalogued in `Tokens/spacing.md`
- Motion: `0.15s ease` on `background` and `color`
- Icons: PrimeIcons v7 — `pi-check` (`.ok`), `pi-times` (`.cancel`)

## Reference markup

```html
@if (isEditing(row.CriteriaId, 'progress')) {
  <div class="cell-edit">
    <input #progressInput appAutofocus type="number" min="0" max="100" step="0.1"
      class="progressInput" [value]="row.ProgressPercent ?? 0"
      (keydown)="onCellKeydown($event, row, 'progress', progressInput)" />
    <button type="button" class="cell-icon-btn ok" title="Lưu"
      (click)="confirmEdit(row, 'progress', progressInput.value)">
      <i class="pi pi-check"></i>
    </button>
    <button type="button" class="cell-icon-btn cancel" title="Huỷ" (click)="cancelEdit()">
      <i class="pi pi-times"></i>
    </button>
  </div>
} @else if (row.IsEditable) {
  <span class="cell-editable" tabindex="0" role="button" title="Bấm đúp để sửa Tiến độ %"
    (dblclick)="startEdit(row, 'progress')" (keydown.enter)="startEdit(row, 'progress')">
    {{ formatPercent(row.ProgressPercent) }}
  </span>
} @else {
  {{ formatPercent(row.ProgressPercent) }}
}
```

```ts
onCellKeydown(event: KeyboardEvent, row: ICriteriaRow, field: EditField, input: HTMLInputElement): void {
  if (event.key === 'Enter')       { event.preventDefault(); this.confirmEdit(row, field, input.value); }
  else if (event.key === 'Escape') { event.preventDefault(); this.cancelEdit(); }
}
```

Verbatim copy: `Lưu` · `Huỷ` (button titles) · `Bấm đúp để sửa Tiến độ %` · `Bấm đúp để sửa Ghi chú` (cell titles) · `— bấm đúp để ghi chú` (empty-note prompt) · `Nội dung đã làm / vướng mắc...` (note input placeholder).

Sources: `src/FE/src/app/modules/danh-muc-dti/components/criteria-grid-table/criteria-grid-table.scss:5-18` (`.cell-editable`), `:20-29` (`.cell-edit`), `:57-99` (`.cell-icon-btn`), `src/FE/src/app/modules/danh-muc-dti/components/criteria-grid-table/criteria-grid-table.html:45-115`, `src/FE/src/app/modules/danh-muc-dti/components/criteria-grid-table/criteria-grid-table.ts:38-47` (the double-click decision), `:96-130` (edit state machine), `src/FE/src/app/shared/directives/autofocus.directive.ts`

## Do / Don't

- ✅ Keep the buttons and the keys in lock-step: `.ok` must do exactly what `Enter` does and `.cancel` exactly what `Escape` does. `confirmEdit`/`cancelEdit` are shared by both paths, which is what guarantees it.
- ✅ Auto-focus the field on entering edit mode (`appAutofocus`) — a double-click that does not land the caret makes the interaction feel broken.
- ✅ Keep the resting dashed border transparent rather than absent; that is what stops the row from twitching on hover.
- ✅ Give the resting span `tabindex="0"` + `role="button"` + a `title` that names the column — it is the only keyboard route into the editor.
- ✅ Render read-only cells as bare text. Not rendering the affordance is the shipped way of saying "not editable", the same rule `ActionButton` follows for row actions.
- ❌ Don't switch to single-click activation. Double-click is a recorded, deliberate divergence from the prototype for a 12-column grid where single-click would trigger constantly during horizontal scrolling.
- ❌ Don't let `.ok`/`.cancel` fall back to the neutral hover colour; the semantic tint is what distinguishes commit from discard at 24px.

## Normalize on redesign
1. **Double-click has no discoverable affordance until hover.** The dashed underline appears only on pointer hover, so touch and keyboard users get no visual cue that a cell is editable — the `title` is the only hint, and `title` is not exposed on touch.
2. **`(dblclick)` has no keyboard-equivalent parity problem, but `(keydown.enter)` on a `role="button"` span does not handle `Space`**, which the button role implies.
3. **Four off-scale literals** in one 40-line rule: radius `6px` (vs `rounded.sm` 7px), `font-size:12px` (numerically `--fs-sm`), `line-height:1`, `padding-bottom:1px`.
4. **`.cell-icon-btn` and `.action-btn` are two ghost icon buttons with different geometry, different radius and different hover fills**, both used inside the same grid row. One ghost-button primitive with a size modifier would remove the divergence.
5. **The base (unmodified) `.cell-icon-btn` styling is unreachable** in shipped markup — every instance carries `.ok` or `.cancel`.
6. **No `prefers-reduced-motion` guard** on the colour transition.

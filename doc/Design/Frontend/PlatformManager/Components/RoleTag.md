---
project: "PlatformManager"
status: "draft"
updated: "2026-08-22"
component: "RoleTag"
sources:
  - "src/FE/src/app/platform/quan-tri-nguoi-dung/components/user-grid-table/user-grid-table.scss"
  - "src/FE/src/app/platform/quan-tri-nguoi-dung/components/user-grid-table/user-grid-table.html"
---

# RoleTag
**Description:** The small outlined chip that lists a user's roles in the user grid (`.role-tag`) — one chip per role string returned by the API. It is deliberately *not* a `Badge`: it carries no semantic colour, because a role name is an identifier, not a status.

## Anatomy

`<span class="role-tag">{{ role }}</span>` — a single text node, no icon, no remove button, no truncation. `display:inline-block`, border 1px `colors.line`, radius `6px` literal, padding `2px 8px` literal, `typography.muted-caption` size (`--fs-xs`), text `colors.text`, fill `colors.surface-table-header`, `margin-right:4px` literal.

The fill is the same pale surface the column headers and the zebra stripe use — chosen so the chip reads as an element *inside* the table rather than an object floating on it (the source comment says exactly that). Because the fill matches the even-row stripe, a chip on an even row shows only its border; on an odd (white) row it shows a faint plate.

Chips are emitted by a bare `@for` over `row.Roles` with no separator element — `margin-right` is the only gap, and a user with no roles renders an empty cell (no placeholder dash, unlike the other nullable columns in the same grid).

## Variants

| Variant | Classes | Key values | When to use |
| --- | --- | --- | --- |
| Role chip | `role-tag` | Border `colors.line`, fill `colors.surface-table-header`, text `colors.text`, radius `6px`, padding `2px 8px` | Every role string in the "Vai trò" column — `SuperAdmin`, `Admin`, `User` all render identically |

There is **exactly one variant**. No size, colour or emphasis modifier exists: `grep` finds `.role-tag` in one stylesheet and one template, with no companion class. In particular `SuperAdmin` is *not* highlighted here, even though it is treated specially everywhere else in the feature (omitted from the role picker, preserved on save, guarded server-side).

## States
<!-- Exactly these five rows, in this order — treatments as rendered by the shipped CSS. -->

| State | Treatment |
| --- | --- |
| default | `display:inline-block`, border 1px `colors.line`, radius `6px` literal, padding `2px 8px` literal, `--fs-xs`, `colors.text` on `colors.surface-table-header`, `margin-right:4px` |
| hover | **Not applicable — the chip is not interactive.** `user-grid-table.scss` authors no `.role-tag:hover`. The row underneath still takes the global `tbody tr:hover` fill (`colors.bg`), which changes the chip's *surroundings* but not the chip |
| focus-visible | **Not applicable** — a plain `<span>`: no `tabindex`, no `role`, not in the tab order |
| active / selected | **Not applicable** — chips are read-only output. Role *selection* happens in the user dialog's `.role-checkboxes` (see `FormRow.md`), which is a different control entirely |
| disabled | **Not applicable** — not a form control and never conditionally rendered; a role the viewer cannot change still displays normally |

## Tokens Used
- `colors.line`, `colors.text`, `colors.surface-table-header`
- `typography.muted-caption` size (`--fs-xs`, 11px) — note the chip uses `colors.text`, not `colors.muted`, so it is the size step only
- Un-tokenised literals: radius `6px`, padding `2px 8px`, `margin-right:4px` — catalogued in `Tokens/spacing.md` (`4px` is numerically `spacing.sp-1`)

## Reference markup

```html
<td>
  @for (role of row.Roles; track role) {
    <span class="role-tag">{{ role }}</span>
  }
</td>
```

```scss
.role-tag {
  display: inline-block;
  border: 1px solid var(--line);
  border-radius: 6px;
  padding: 2px 8px;
  font-size: var(--fs-xs);
  color: var(--text);
  background: var(--surface-table-header); /* same pale fill as the column header — the chip lives inside the table */
  margin-right: 4px;
}
```

Sources: `src/FE/src/app/platform/quan-tri-nguoi-dung/components/user-grid-table/user-grid-table.scss:36-45`, `src/FE/src/app/platform/quan-tri-nguoi-dung/components/user-grid-table/user-grid-table.html:35-39`

## Do / Don't

- ✅ Render one chip per role, in the order the API returns them — the component does no sorting and no normalising (role names are compared ordinally end to end, so casing is never "corrected").
- ✅ Keep the chip neutral. Colour in this grid is reserved for `Badge` (`.badge.active` / `.badge.locked`), so adding a tint here would compete with the account-status column right next to it.
- ✅ Keep the fill equal to `colors.surface-table-header` so the chip stays visually inside the table surface.
- ❌ Don't use `.role-tag` outside a table cell — the fill is chosen against table surfaces and would read as a stray grey plate on `colors.card`.
- ❌ Don't make it removable or clickable; role editing lives in the dialog, and a chip that looks interactive here would promise an action the grid does not offer.

## Normalize on redesign
1. **Three un-tokenised literals** in a seven-line rule: radius `6px` (vs `rounded.sm` 7px), padding `2px 8px`, `margin-right:4px` (= `spacing.sp-1`).
2. **`margin-right` on every chip** leaves a trailing gutter after the last one; a `gap` on a flex/inline-flex cell would be exact.
3. **A user with no roles renders a blank cell** while every other nullable column in the same grid falls back to `—` (`Email`, `Ngày tạo`).
4. **The chip shares a rounded-rectangle silhouette with `Badge`** at a nearby size but with a different radius, different padding and a different colour system — worth converging into one chip primitive with `neutral` and `semantic` variants.
5. **Long role names cannot wrap or truncate** — the column is `min-width:140px` and the chips simply overflow into extra lines with no `max-width`.

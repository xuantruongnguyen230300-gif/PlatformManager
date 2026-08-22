---
project: "PlatformManager"
status: "draft"
updated: "2026-08-22"
component: "ActionButton"
sources:
  - "src/FE/src/styles.scss"
  - "src/FE/src/app/platform/quan-tri-nguoi-dung/components/user-grid-table/user-grid-table.html"
  - "src/FE/src/app/platform/quan-tri-nguoi-dung/components/user-grid-table/user-grid-table.scss"
  - "src/FE/src/app/modules/danh-muc-dti/components/criteria-grid-table/criteria-grid-table.html"
---

# ActionButton
**Description:** The compact **ghost** row-action button (`.action-btn`, global) — transparent until hover, used for the high-frequency per-row controls inside data grids. It is the deliberate counterpart to `.btn`: `Button.md`'s tonal fill is for labelled page-level actions, this one is for the two-per-row controls that would flood a long table if they carried a permanent fill.

## Anatomy

`[optional leading icon] [optional text label]` in a single line, no wrapper. Box: `border:1px solid transparent`, `background:transparent`, radius `rounded.sm`, padding `4px` `spacing.sp-2` (vertical literal, horizontal tokenised), `typography.action-btn-label`, `colors.muted`, `cursor:pointer`, `transition: background 0.15s ease, color 0.15s ease`, and `margin-right: spacing.sp-1`.

The transparent 1px border is load-bearing, not decoration: it holds the box model steady so hover and focus can change colour without a 1px layout jump — the same technique `.btn` uses.

`margin-right` is likewise structural. In the criteria grid the two buttons sit directly inside a `<td>` with no flex container, so that margin is the **only** thing separating them. In the user grid they sit in a `.row-actions` flex box (`gap:6px`, `justify-content:flex-end`) where the margin is redundant.

**Design decision, recorded as fact:** ghost is the intended treatment. Until 2026-08-20 `styles.scss` carried two consecutive `.action-btn` blocks at equal specificity, and the later one (solid border + white fill) silently overrode the ghost block above it, so the documented intent had no effect on screen. The solid block was removed and the ghost kept — partly because it is the intent written at the top of the block, and partly because `user-grid-table.scss` had already written its `:disabled` rule assuming a transparent default. The geometry (`padding`, `font-size`, `font-weight`, `margin-right`) was carried over from the deleted block.

## Variants

| Variant | Classes | Key values | When to use |
| --- | --- | --- | --- |
| Neutral, icon-only | `action-btn` | `colors.muted` glyph on transparent; hover fills `colors.surface-2` | User grid "Sửa" (`pi-pencil`) and the unlock action (`pi-lock-open`), inside `.row-actions` |
| Neutral, text label | `action-btn` | Same box, label instead of glyph, `typography.action-btn-label` | Criteria grid "Sửa" |
| Danger, icon-only | `action-btn danger` | `colors.bad` glyph; hover fills `colors.bad-bg` and keeps `colors.bad` (it does **not** fall back to `colors.text` like the neutral hover) | User grid lock action (`pi-lock`) — `[class.danger]="!row.IsLocked"`, so only the *locking* direction reads as destructive; unlocking is neutral |
| Danger, text label | `action-btn danger` | Same | Criteria grid "Xoá" |
| Absent (read-only row) | — | Replaced by `<span class="muted">—</span>` | Criteria grid when `row.IsEditable` is false (viewing a past period) — the buttons are not disabled, they are not rendered at all |

## States
<!-- Exactly these five rows, in this order — treatments as rendered by the shipped CSS. -->

All five are genuinely implemented — this is the one control in the app that ships the complete set in a single block.

| State | Treatment |
| --- | --- |
| default | `background:transparent`, `border:1px solid transparent`, `colors.muted` (or `colors.bad` for `.danger`), radius `rounded.sm`, padding `4px`/`spacing.sp-2`, `typography.action-btn-label` |
| hover | Neutral: `background: colors.surface-2`, `color: colors.text`. Danger: `background: colors.bad-bg`, `color: colors.bad` (restated so the neutral hover colour cannot win) |
| focus-visible | `outline: 2px solid colors.brand`, `outline-offset: 1px` — a tighter offset than `.btn`'s 2px, so the ring stays inside a dense grid row |
| active / selected | **Not applicable — no pressed or selected treatment exists.** `styles.scss` authors no `.action-btn:active` rule (unlike `.btn`, which nudges `translateY(1px)`), and the button is never a toggle: every use fires a one-shot `output()` and the row re-renders from server data |
| disabled | `opacity:0.45`, `cursor:not-allowed`, **and** a nested `&:hover` that forces `background:transparent` + `color: colors.muted`, so a disabled button does not light up under the pointer. Because that nested rule ties source order with `.action-btn.danger:hover` and comes later, a disabled danger button also hovers to transparent/muted. Only one binding sets it in the shipped app: the user grid's self-lock guard |

## Tokens Used
- `colors.muted`, `colors.text`, `colors.surface-2`, `colors.bad`, `colors.bad-bg`, `colors.brand`
- `rounded.sm`
- `spacing.sp-1` (`margin-right`), `spacing.sp-2` (horizontal padding); the `4px` vertical padding is a literal — catalogued in `Tokens/spacing.md` § Padding & gap literals bypassing the scale
- `typography.action-btn-label` (11px/700)
- Motion: `0.15s ease` on `background` and `color` — no motion token exists
- Icons: PrimeIcons v7 — `pi-pencil`, `pi-lock`, `pi-lock-open` in the shipped call sites

## Reference markup

```html
<!-- Icon-only pair, user grid. The lock button is danger only in the locking direction,
     and is the app's single disabled ActionButton. -->
<div class="row-actions">
  <button type="button" class="action-btn" title="Sửa" (click)="editRow.emit(row)">
    <i class="pi pi-pencil"></i>
  </button>
  <button
    type="button"
    class="action-btn"
    [class.danger]="!row.IsLocked"
    [disabled]="isSelfLockBlocked(row)"
    [title]="isSelfLockBlocked(row)
      ? 'Không thể tự khoá tài khoản của chính mình — dùng Đăng xuất'
      : row.IsLocked ? 'Mở khoá tài khoản' : 'Khoá tài khoản'"
    (click)="toggleLock.emit(row)">
    <i class="pi" [class.pi-lock]="!row.IsLocked" [class.pi-lock-open]="row.IsLocked"></i>
  </button>
</div>

<!-- Text-labelled pair, criteria grid — rendered only for an editable row. -->
@if (row.IsEditable) {
  <button type="button" class="action-btn" (click)="editRow.emit(row)">Sửa</button>
  <button type="button" class="action-btn danger" (click)="deleteRow.emit(row)">Xoá</button>
} @else {
  <span class="muted">—</span>
}
```

Sources: `src/FE/src/styles.scss:197-212` (the ghost decision + merge note), `:213-253` (the rule), `src/FE/src/app/platform/quan-tri-nguoi-dung/components/user-grid-table/user-grid-table.html:49-69`, `src/FE/src/app/platform/quan-tri-nguoi-dung/components/user-grid-table/user-grid-table.scss:59-68`, `src/FE/src/app/platform/quan-tri-nguoi-dung/components/user-grid-table/user-grid-table.ts:63-66` (`isSelfLockBlocked`), `src/FE/src/app/modules/danh-muc-dti/components/criteria-grid-table/criteria-grid-table.html:128-133`

## Do / Don't

- ✅ Use `.action-btn` for controls that repeat once per row, and `.btn` for the one or two page-level actions in a `.title` bar or `.filters` row. Two tonal fills per row across 20 rows is exactly the flooding the ghost treatment exists to prevent.
- ✅ Always give an icon-only instance a `title` (and prefer a `[title]` that also explains *why* it is disabled, as the self-lock guard does) — the button has no visible label to read.
- ✅ Reserve `.danger` for the destructive direction only. The lock control toggles the class rather than carrying it permanently, so "unlock" never reads as destructive.
- ✅ Prefer **not rendering** a row action the user may not perform over rendering it disabled — the criteria grid does exactly that for read-only periods. Disable only when the control must stay visible to explain itself (the self-lock case).
- ❌ Don't give it a permanent border or fill. That is the exact regression removed on 2026-08-20, and it silently defeats the `:disabled` rule, which assumes a transparent default.
- ❌ Don't rely on `margin-right` for spacing inside a flex container — `.row-actions` already sets `gap`, so the margin adds an asymmetric trailing gutter.

## Normalize on redesign
1. **`margin-right: var(--sp-1)` is unconditional**, so every instance carries a trailing gutter — including the last button in a `.row-actions` flex row that already has `gap:6px`. The two mechanisms overlap; pick one.
2. **`.row-actions` uses a `6px` literal gap** while `spacing.sp-2` is `6px` — a free swap.
3. **The `4px` vertical padding is off-scale** (`spacing.sp-1` is 4px, so this is also a free swap; it is the horizontal half that is already tokenised).
4. **No `:active` treatment.** `.btn` presses 1px and this one does not, so the two button families feel different under the pointer.
5. **`opacity:0.45` on disabled** puts `colors.muted` text at roughly half contrast; combined with `typography.action-btn-label` at 11px this is likely below any contrast floor. Measure before shipping a redesign.

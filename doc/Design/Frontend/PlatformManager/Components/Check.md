---
kind: luat
scope: du-an
verified: 2026-09-06
project: "PlatformManager"
status: "draft"
updated: "2026-09-06"
component: "Check"
sources:
  - "src/FE/src/styles.scss"
  - "src/FE/src/app/platform/phan-quyen/components/permission-matrix/permission-matrix.html"
  - "src/FE/src/app/platform/phan-quyen/components/permission-matrix/permission-matrix.scss"
  - "src/FE/src/app/platform/phan-quyen/components/resource-permission-matrix/resource-permission-matrix.html"
  - "src/FE/src/app/platform/phan-quyen/components/resource-permission-matrix/resource-permission-matrix.scss"
  - "src/FE/src/app/platform/quan-tri-nguoi-dung/components/user-form-dialog/user-form-dialog.html"
  - "src/FE/src/app/platform/quan-tri-nguoi-dung/components/user-form-dialog/user-form-dialog.scss"
  - "src/FE/src/app/platform/login/pages/login/login.page.html"
---

# Check
**Description:** The app's checkbox (`.check`) — a **native `<input type="checkbox">` recoloured with `accent-color`**, not a custom control. The whole rule is a size, a cursor, an accent, a focus ring and a disabled treatment — read it rather than trusting a number written here: `grep -n '^\.check' src/FE/src/styles.scss` and the two lines that follow the block. There is no wrapper element, no pseudo-element tick, no indeterminate state and no radio counterpart.

> **This class exists because of a merge.** Before the redesign of **2026-08-29** the two permission matrices each declared their own `input[type='checkbox']` block, and the source records that the two blocks were **identical word for word** (the comment above `styles.scss` § `.check`, `permission-matrix.scss:5-6`). Both scoped copies are gone; each stylesheet now keeps only what is genuinely local to it — the tree indent in one, the break-glass column labels in the other — and says so at the top of the file.

## Anatomy

A bare `<input type="checkbox" class="check">`, always with an accessible name — either an `aria-label` (in a matrix cell, where the column header alone is not enough context) or a wrapping `<label>` (in a form).

`width` / `height` `dimension.checkbox` (16×16) · `cursor: pointer` · `accent-color: colors.brand`.

**`accent-color` is the whole visual decision.** The checked fill, the tick glyph and the platform's own hover/press feedback stay native and simply take the brand hue. That is why this spec has no "checked" rule to cite: the app never authors one. It also means the control renders with the host platform's exact shape — a rounded square on macOS, a sharper one on Windows — which is a deliberate consequence of not building a custom checkbox, not an oversight.

Three shipped contexts, all using the same class with no modifier:

| Context | Wrapper | Source |
| --- | --- | --- |
| Permission matrix cell | `<td class="num">`, one per role column, with `aria-label` built from the row name and the role. The menu matrix concatenates (`row.SysMenuName + ' — ' + role`); the resource matrix resolves a key instead (`phan-quyen.grid.cellAriaLabel`, or `…cellAriaLabelAlwaysAllowed` on the always-on column) because that sentence has to be translatable | `permission-matrix.html:22-32`, `resource-permission-matrix.html:22-34` |
| Role picker in the user dialog | `.role-checkbox` — a `<label>` flex row (`gap: spacing.sp-2`, `typography.form-label` size at weight 600) inside `.role-checkboxes`, a `role="group"` labelled by the form-row label | `user-form-dialog.html:122-135`, `user-form-dialog.scss:8-21` |
| "Ghi nhớ đăng nhập" on sign-in | An inline `<label>` inside `.field-row`, bound both ways to `rememberMe()` | `login.page.html:71-79` |

## Variants

| Variant | Classes | Key values | When to use |
| --- | --- | --- | --- |
| Checkbox | `check` | 16×16, `accent-color: colors.brand`, `cursor:pointer` | Every checkbox in the app |

**There is exactly one variant, and no size, tone or shape modifier exists.** `grep -rn "\.check" src/FE/src` finds the one declaration block plus its call sites (checked 2026-08-29). The three contexts differ only in what wraps the input.

There is also **no radio button anywhere in the app** — exclusive choice is expressed by `<select>` or by `SegmentedControl.md`.

## States
<!-- Exactly these five rows, in this order — treatments as rendered by the shipped CSS. -->

| State | Treatment |
| --- | --- |
| default | 16×16 native checkbox, `cursor:pointer`, `accent-color: colors.brand`. Unchecked appearance is the platform's own; **checked** is the platform's own tick on a `colors.brand` fill, with no authored rule |
| hover | **No `.check:hover` is authored.** The browser's native checkbox hover shading applies unchanged. In a matrix the surrounding row still lifts to `colors.bg` through the global `tbody tr:hover`, and in the role picker the `<label>` around it carries `cursor:pointer` so the whole row is the target — but the box itself is untouched by app CSS |
| focus-visible | `outline: 2px solid colors.brand` at `outline-offset: 2px` (`styles.scss` § `.check:focus-visible`) — the app's standard ring geometry, matching `.btn` and `.seg-btn` rather than `IconButton`'s tighter 1px offset |
| active / selected | **"Selected" is the checked state**, and it is entirely native via `accent-color`. No `:checked`, `:active` or `:indeterminate` rule is authored. A partially-filled group therefore has no tri-state affordance — a matrix row where some roles are on and some off looks like N independent boxes, which is exactly what it is |
| disabled | `cursor: not-allowed`, `opacity: .6` (`styles.scss` § `.check:disabled`), over the platform's own dimmed rendering. Genuinely reachable in both matrices: `[disabled]="loading()"` freezes the menu matrix while a save is in flight (`permission-matrix.html:28`), and `[disabled]="isDisabled(role)"` locks the always-on SuperAdmin column in the resource matrix (`resource-permission-matrix.html:28`) |

## Tokens Used
- `colors.brand` — both the `accent-color` and the focus ring
- `dimension.checkbox` (16px)
- Un-tokenised literals: `opacity: .6`, `outline-width: 2px`, `outline-offset: 2px` (the focus ring geometry is a repeated literal across every control — see `Tokens/colors.md`)
- Nothing else. There is no border, radius, fill or shadow token in play, because the box is native

## Reference markup

```html
<!-- matrix cell: the accessible name must be built, the column header is not enough -->
<td class="num">
  <input
    type="checkbox"
    class="check"
    [checked]="isChecked(row, role)"
    [disabled]="loading()"
    (change)="permissionToggle.emit({ SysMenuId: row.SysMenuId, Role: role })"
    [attr.aria-label]="row.SysMenuName + ' — ' + role"
  />
</td>

<!-- form: a wrapping label makes the whole row clickable -->
<label class="role-checkbox">
  <input type="checkbox" class="check" [checked]="isRoleSelected(role)" (change)="toggleRole(role)" />
  {{ role }}
</label>
```

```scss
.check {
  width: 16px;
  height: 16px;
  cursor: pointer;
  accent-color: var(--brand);
}
.check:focus-visible { outline: 2px solid var(--brand); outline-offset: 2px; }
.check:disabled { cursor: not-allowed; opacity: .6; }
```

Sources: `src/FE/src/styles.scss` § `.check` (with § `.check:focus-visible` and § `.check:disabled`), `src/FE/src/app/platform/phan-quyen/components/permission-matrix/permission-matrix.html:22-33`, `src/FE/src/app/platform/phan-quyen/components/permission-matrix/permission-matrix.scss:1-6`, `src/FE/src/app/platform/phan-quyen/components/resource-permission-matrix/resource-permission-matrix.html:22-35`, `src/FE/src/app/platform/quan-tri-nguoi-dung/components/user-form-dialog/user-form-dialog.html:122-135`, `src/FE/src/app/platform/quan-tri-nguoi-dung/components/user-form-dialog/user-form-dialog.scss:8-21`, `src/FE/src/app/platform/login/pages/login/login.page.html:71-79`

> 🔄 **SỬA 2026-09-06** — bản trước sai ba chỗ:
> 1. **Normalize #1 khẳng định ô "Ghi nhớ đăng nhập" không nối vào đâu** — nó đã được nối thật. Xem mục đó.
> 2. **Lệnh đếm ở phần mở đầu trỏ sai khoảng dòng** (`sed -n '666,673p'`) — khối `.check` đã trôi sang chỗ khác. Thay bằng `grep` theo định danh, đúng khuôn `doc/Design/CLAUDE.md` § "Neo trích dẫn vào `styles.scss`".
> 3. **Năm neo `file:dòng` trỏ sai chỗ** sau khi hai ma trận và hộp thoại người dùng được bọc i18n; đặc biệt `resource-permission-matrix.scss:1-6` — file đó đang ngắn hơn và không còn nói gì về `.check`.

## Do / Don't

- ✅ Keep it native. `accent-color` gives the brand colour without giving up keyboard behaviour, the platform tick, or the announced role and checked state.
- ✅ Give every box a name. In a matrix cell that means `aria-label` composed from the row **and** the column (`row.SysMenuName + ' — ' + role`); a screen-reader user reading a grid of unlabelled boxes has nothing to go on.
- ✅ Wrap it in a `<label>` in forms, so the text is part of the hit target.
- ✅ Use the real `[disabled]` binding, not a visual dim. Both matrices do, and it is what keeps a locked column genuinely locked.
- ❌ Don't redeclare size, accent, focus or disabled in a component's scoped SCSS. That is exactly the duplication removed on 2026-08-29, and the two copies had already begun to differ in which states they defined.
- ❌ Don't build a custom checkbox with a pseudo-element tick. Nothing in the app does, and it would cost the native semantics for nothing gained.
- ❌ Don't reach for a radio button. There are none; use `<select>` or `SegmentedControl.md`.

## Normalize on redesign
1. ~~**The "Ghi nhớ đăng nhập" checkbox on the sign-in screen is not wired to anything.**~~ — **đã xử lý**, đối chiếu 2026-09-06. It now carries `[checked]="rememberMe()"` and `(change)="onRememberMeChange($event)"`, and the flag travels in the `POST /api/auth/login` body (`login.page.html:71-79`). Default is **unchecked**, which is the session-cookie branch — the safer of the two.
2. **16×16 is below the pointer-target guidance of WCAG 2.2 SC 2.5.8.** In a matrix cell (`padding: spacing.sp-2 spacing.sp-3`) the effective target is a little larger, but only the wrapping `<label>` in the role picker gets it comfortably clear.
3. **No indeterminate state exists**, so a permission matrix cannot show "some of this group's menus are granted" at the group row — every box is independently binary.
4. **The focus ring is a literal repeated on every control** (`2px solid var(--brand)` + an offset) with no token behind it, which is why `IconButton`'s 1px offset was able to drift away from everything else.
5. **`opacity: .6` on disabled stacks on top of the browser's own dimming**, so a disabled checkbox is measurably fainter than a disabled `.btn` (`.5`) or a disabled `IconButton` (`.45`) — three different disabled opacities across three controls.
6. **The rendered shape is platform-dependent** because the box is native. It is the right trade, but a design mock will not match every machine exactly, and generated screens should not draw a specific corner radius on it.

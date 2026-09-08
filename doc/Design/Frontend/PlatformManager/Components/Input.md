---
kind: luat
scope: du-an
verified: 2026-09-06
project: "PlatformManager"
status: "draft"
updated: "2026-08-29"
component: "Input"
sources:
  - "src/FE/src/styles.scss"
  - "src/FE/src/app/shared/components/toolbar/toolbar.html"
  - "src/FE/src/app/platform/quan-tri-nguoi-dung/components/user-form-dialog/user-form-dialog.html"
  - "src/FE/src/app/platform/quan-tri-nguoi-dung/pages/quan-tri-nguoi-dung/quan-tri-nguoi-dung.page.html"
  - "src/FE/src/app/platform/phan-quyen/components/permission-matrix/permission-matrix.html"
  - "src/FE/src/app/platform/login/pages/login/login.page.html"
---

# Input
**Description:** Native form controls (`<input>`, `<select>`, `<textarea>`) as painted by the **single** field contract in `src/FE/src/styles.scss` § 4. The 2026-08-29 redesign merged three earlier field treatments — page-level, dialog-level and auth-level — into one rule, added `:disabled` and `.invalid` states that previously did not exist, and introduced six width variants keyed to the *kind* of data being typed.

> **Citation policy.** Values cite `src/FE/src/styles.scss` plus the **selector name**, not a line number — the file was rewritten on 2026-08-29 and line numbers in it are not yet stable evidence.

All controls inherit `font: inherit` from the global reset, so no context restates the font family.

## Anatomy

One rule paints every field, whichever of the four contexts it sits in:

```
.input,
.toolbar input, .toolbar select,
.form-row input, .form-row select, .form-row textarea,
.input-icon input
```

Box: `border: 1px solid colors.border-strong`, radius `rounded.sm`, fill `colors.card`, padding `spacing.sp-2` `spacing.sp-3`, `typography.table-cell` size, ink `colors.text`, transition on `border-color` and `box-shadow` at `duration.fast`.

`colors.border-strong` (4.51:1 on card) rather than `colors.line` (3.00:1) is deliberate and is the whole point of the merge: an input is **the one place a border still has a job to do** — it says "you can type here". The source states this inline. `.tablewrap` is the only other consumer of `border-strong`.

Labels are always external: a `<label>` above the field in forms and on the auth screens, a `placeholder` plus `aria-label` in the toolbar, a column header in a table. Only `.input-icon` has in-field adornments — a leading `pi` glyph and, on a password field, a trailing `.icon-btn` reveal toggle (see `AuthField.md`).

Inside `.form-row` a field is `width: 100%` by default; a width variant overrides that (see below). A `<select>` is the exception in both directions — `select.input` and `.toolbar select` are `width: auto` with `min-width: dimension.select-min-width` and a 240px cap, so a picker sizes to its longest option instead of stretching.

## Variants

| Variant | Classes / selector | Key values | When to use |
| --- | --- | --- | --- |
| Base field | `.input` | The shared box above | Any standalone field; the user form dialog's four text fields carry it |
| Toolbar field | `.toolbar input`, `.toolbar select` | Same box, no class needed — the ancestor selector applies it | Search and filter controls inside `.toolbar` |
| Form-row field | `.form-row input/select/textarea` | Same box plus `width: 100%` | Every dialog and filter-panel field (see `FormRow.md`) |
| Icon field | `.input-icon input` | Same box plus `width: 100%` and its own padding `10px 12px 10px 36px` — the 36px left inset clears the leading glyph. Off-scale, and recorded as such in `Tokens/spacing.md` § Padding & gap literals bypassing the scale | Auth fields and the toolbar search box (see `AuthField.md`) |
| Textarea | `.form-row textarea` | Adds `min-height: dimension.textarea-min-height`, `resize: vertical` | Multi-line notes. **No shipped call site today** |
| Select | `select.input`, `.toolbar select` | `width: auto`, `min-width: dimension.select-min-width`, `max-width: 240px` | Role and status pickers in the user-list filter panel |
| Width — integer | `.input.w-num` | `dimension.input-w-num`, `text-align: right`, browser spinners suppressed | Scores, counts |
| Width — percentage | `.input.w-pct` | `dimension.input-w-pct`, `text-align: right`, spinners suppressed | Percentage fields |
| Width — date | `.input.w-date` | `dimension.input-w-date` | Date pickers |
| Width — short | `.input.w-sm` | `dimension.input-w-sm` | Codes, short keywords |
| Width — medium | `.input.w-md` | `dimension.input-w-md` | Names, emails, search — the **only width variant with a shipped call site** (all four fields of the user form dialog) |
| Width — long | `.input.w-lg` | `dimension.input-w-lg` | Full sentences |
| Checkbox | `.check` | `dimension.checkbox` square, `accent-color: colors.brand`, `cursor: pointer` | Both permission matrices, the user dialog's role group, and the sign-in remember-me box — one rule, every call site |

**Width is an interface signal, not decoration.** A field stretched across a whole row for a two-digit number is both ugly and a false promise about how much text is wanted. Inside `.form-row` the `.w-*` rules are repeated at higher specificity so they beat the `width: 100%` default — that repetition is intentional, not a copy-paste slip.

## States
<!-- Exactly these five rows, in this order — treatments as rendered by the shipped CSS. -->

Primary interactive element: the `input` / `select` / `textarea` itself.

| State | Treatment |
| --- | --- |
| default | Border 1px `colors.border-strong`, radius `rounded.sm`, fill `colors.card`, padding `spacing.sp-2` `spacing.sp-3`, `typography.table-cell` size, ink `colors.text` |
| hover | **Not styled** — no `:hover` rule exists for any field in any context. Hover in this family belongs to the trailing `.icon-btn` of an `.input-icon`, which takes it from the icon-button rule |
| focus | `outline: none`, `border-color: colors.brand`, `box-shadow: shadow.focus-ring`. This applies to **all four contexts** from one selector list. `.invalid` fields keep the red border and swap the glow to `shadow.focus-ring-invalid`. Checkboxes use the house ring instead: `outline: 2px solid colors.brand`, `outline-offset: 2px` |
| active | **Not applicable** — a text field has no pressed state and no `:active` rule is authored |
| disabled | fill `colors.surface-track`, ink `colors.muted`, `cursor: not-allowed` — declared for all four contexts. Checkboxes get `cursor: not-allowed` and `opacity: .6`. **Reachable**: both permission matrices bind `[disabled]` on every checkbox while a fetch or save is in flight, and `Toolbar` exposes `[searchDisabled]` on its search field |

### Error state — `.invalid`

Not one of the five, but a real shipped modifier: `.input.invalid` (and the same class on a `.form-row` field) sets `border-color: colors.bad`, and its focus glow turns red. It pairs with the `.form-error` line documented in `FormRow.md`, and the two always ship together: the control takes `.invalid` + `aria-invalid="true"` + `aria-describedby`, and a `.form-error` carrying the matching `[id]` renders under it. Call sites are in the user dialog and on `/doi-mat-khau`; list them with `grep -rn '\[class.invalid\]' src/FE/src/app --include='*.html'`.

## Tokens Used
- `colors.card`, `colors.border-strong`, `colors.brand`, `colors.text`, `colors.muted`, `colors.bad`, `colors.surface-track`
- `rounded.sm`
- `spacing.sp-2`, `spacing.sp-3` — the shared field padding is `var(--sp-2) var(--sp-3)`, two real tokens, not one composite
- `dimension.input-w-num`, `dimension.input-w-pct`, `dimension.input-w-date`, `dimension.input-w-sm`, `dimension.input-w-md`, `dimension.input-w-lg`, `dimension.select-min-width`, `dimension.textarea-min-height`, `dimension.checkbox`
- `typography.table-cell` (field text), `typography.form-label` (external labels)
- `shadow.focus-ring`, `shadow.focus-ring-invalid`
- `duration.fast`

The `select` `max-width: 240px` cap, the `.input-icon input` padding `10px 12px 10px 36px` (and the toolbar's `30px` variant of it), and the `-webkit-appearance: none` resets (number spinners, the WebKit search clear button) are literals with no token behind them.

> ### 🔄 SỬA 2026-09-08 — this spec was citing two token names that exist in only one of the two ledgers
>
> Until today § Anatomy, § Variants, § States and this list all referenced **`spacing.input-padding`** and **`spacing.auth-input-padding`**. Neither name exists in `Tokens/tokens.json` or `Tokens/spacing.md` — the project's token ledger. Both exist only in the `spacing:` block of `DESIGN.md`, the Stitch dictionary. A reader following the reference from here into `Tokens/` found nothing.
>
> Resolved by citing what the ledger actually holds, which is also what the source holds:
>
> - **`input-padding`** was never a token to begin with. The shipped declaration is `padding: var(--sp-2) var(--sp-3)` — two real custom properties. Giving that pair a third name hid the fact that the value is already fully tokenised, and invented a token the live source does not have (`doc/Design/CLAUDE.md` § Rules forbids exactly that).
> - **`auth-input-padding`** *is* a genuine off-scale literal, and `Tokens/spacing.md` already catalogues it by value and selector in § Padding & gap literals bypassing the scale — with no name, deliberately. So it is quoted here as the literal it is, with a pointer to that row.
>
> The wider problem is **not** this spec's to close and is recorded where it belongs: `Tokens/spacing.md` § Tên ghép chỉ có ở `DESIGN.md`. `DESIGN.md` declares a whole family of composite padding names, most of which other specs cite the same way this one did.

## Reference markup

```html
<!-- base field with a width variant, inside a dialog form row -->
<div class="form-row">
  <label for="ufEmail">Email <span class="required">*</span></label>
  <input id="ufEmail" class="input w-md" type="email" placeholder="ten@congty.vn"
         [value]="emailField()" (input)="onEmailInput($event)" />
</div>

<!-- toolbar select: sized by content, not stretched -->
<select id="userFilterRole" [value]="roleDraft()" (change)="onRoleDraftChange($event)">
  <option value="">Tất cả vai trò</option>
  <option value="SuperAdmin">SuperAdmin</option>
  <option value="Admin">Admin</option>
  <option value="User">User</option>
</select>

<!-- icon field: leading glyph + search input -->
<div class="input-icon search">
  <i class="pi pi-search"></i>
  <input type="search" placeholder="Tìm theo tên hoặc email..." aria-label="Tìm theo tên hoặc email"
         [value]="searchInput()" (input)="onSearchInputEvent($event)" />
</div>

<!-- checkbox, disabled while the matrix is loading or saving -->
<input type="checkbox" class="check" [checked]="isChecked(row, role)" [disabled]="loading()"
       (change)="permissionToggle.emit({ SysMenuId: row.SysMenuId, Role: role })"
       [attr.aria-label]="row.SysMenuName + ' — ' + role" />
```

Sources: `src/FE/src/styles.scss` (§ 4 — the merged field contract, the `.w-*` width variants, `select.input`, the `:disabled` and `.invalid` rules, `.input-icon`, `.check`), `src/FE/src/app/shared/components/toolbar/toolbar.html`, `src/FE/src/app/platform/quan-tri-nguoi-dung/components/user-form-dialog/user-form-dialog.html`, `src/FE/src/app/platform/quan-tri-nguoi-dung/pages/quan-tri-nguoi-dung/quan-tri-nguoi-dung.page.html`, `src/FE/src/app/platform/phan-quyen/components/permission-matrix/permission-matrix.html`, `src/FE/src/app/platform/phan-quyen/components/resource-permission-matrix/resource-permission-matrix.html`, `src/FE/src/app/platform/login/pages/login/login.page.html`

## Do / Don't

- ✅ Use `colors.border-strong` for fields and nothing else (bar `.tablewrap`). It is the one signal that separates "you can type here" from a decorative boundary.
- ✅ Pick the width variant that matches the data: `.w-num` for a score, `.w-md` for a name. A full-width field for a two-digit number misstates how much input is wanted.
- ✅ Let the shared rule paint the field. A component stylesheet that re-declares a field loses the focus ring, the disabled fill and the invalid border in one move — that is precisely the drift the 2026-08-29 merge removed.
- ✅ Give a toolbar search field an `aria-label`; there is no room for a visible label there and `Toolbar` exposes `[searchAriaLabel]` for exactly this.
- ✅ Use `.check` for every checkbox. All three call sites in the app do, so focus and disabled behave the same everywhere.
- ❌ Don't add a `:hover` border to a native field — none exists, and generated screens with one will not match.
- ❌ Don't stretch a `<select>`; `width: auto` with a min and max is the shipped behaviour and it stops filter pickers from staggering by option length.
- ❌ Don't rely on PrimeNG form-field theming. The preset maps it, but no PrimeNG input component is used anywhere in the app.

## Normalize on redesign
1. **Five of the six width variants have no shipped call site** (`.w-num`, `.w-pct`, `.w-date`, `.w-sm`, `.w-lg`). They were designed against a data-entry grid that is not in the app today. Keep them only when that grid lands.
2. ~~**`.invalid` and `.form-error` are wired to nothing.**~~ — **Wrong when written; corrected 2026-09-08.** Templates do set `[class.invalid]` and do render a `.form-error` under the failing field, in the user dialog and on `/doi-mat-khau` (`grep -rn '\[class.invalid\]' src/FE/src/app --include='*.html'`). What *is* still outstanding is coverage, not wiring: **`/dang-nhap` has no per-field errors at all**, so the same validation failure reads differently on the two auth screens (`Screens/05-auth.md` records the same split).
3. **`textarea` has no call site**, so `dimension.textarea-min-height` and `resize: vertical` are untested in the running app.
4. **The `.form-row .w-*` block is a specificity workaround.** Six selectors exist only to beat `width: 100%` declared four lines above. A single `:where()` on the default, or ordering the width rules last, would remove the duplication.
5. **The toolbar variant overrides padding and glyph size from a descendant selector**, so an `.input-icon` outside a toolbar and one inside it are two visually different fields under one class name.

## Resolved in the 2026-08-29 redesign
<!-- Items that used to sit in "Normalize on redesign" and were actually done. Kept, not deleted, so the history is not lost. -->
1. **Three input tiers for one role — resolved 2026-08-29.** Page fields used `border-strong`, dialog fields used the faint `line`, and auth fields used `border-strong` with their own padding. All three now render from one selector list with one border colour. The old `Input.md` recorded this as the library's widest inconsistency.
2. **Focus was inconsistent and partly missing — resolved 2026-08-29.** Dialog and table-cell fields previously had *no* authored focus style and fell back to the browser default. One `:focus-visible` block now covers all four contexts.
3. **No disabled appearance — added 2026-08-29.** `:disabled` previously matched nothing, so a disabled field rendered in the browser's own grey, outside every token. It now fills `colors.surface-track`.
4. **No error appearance — added 2026-08-29.** `.invalid` did not exist. Templates began setting it in the 2026-09-05 i18n/validation phase; see Normalize #2 for the part still outstanding (`/dang-nhap` coverage).
5. **Checkbox styling was component-scoped — resolved 2026-08-29.** The 16px `accent-color` box was declared inside one permission matrix, so the user dialog's and the sign-in's checkboxes were unstyled browser defaults. `.check` is now global and all three call sites use it.

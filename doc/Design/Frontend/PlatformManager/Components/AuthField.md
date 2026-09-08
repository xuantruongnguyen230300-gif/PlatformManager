---
kind: luat
scope: du-an
verified: 2026-09-06
project: "PlatformManager"
status: "draft"
updated: "2026-08-29"
component: "AuthField"
sources:
  - "src/FE/src/styles.scss"
  - "src/FE/src/app/platform/login/pages/login/login.page.html"
  - "src/FE/src/app/platform/doi-mat-khau/pages/doi-mat-khau/doi-mat-khau.page.html"
---

# AuthField
**Description:** The auth-form layer — `.field`, `.field-row`, `.login-error` and `.btn-block` (`src/FE/src/styles.scss` § 9 "Khối đăng nhập", plus `.btn-block` in § 3.1). It is the label / options-line / error-block / full-width-submit scaffolding that only the two auth screens use.

**Scope shrank on 2026-08-29.** The input itself is no longer here. `.field-input` was **renamed `.input-icon` and merged with the toolbar's search field** — one icon-prefixed input contract for the whole app — so the box, its border, its focus ring, its disabled and invalid states, and the trailing show/hide button now belong to [`Input.md`](./Input.md). What is left in this spec is only what an auth form has and no other surface does.

> **Citation policy.** Values cite `src/FE/src/styles.scss` plus the **selector name**, not a line number — § 9 was rewritten twice on 2026-08-29 and a line number in a file being edited is evidence that expires.

> **Why all four are global, and why that is not a style choice.** The forms are projected into `AuthCard` through `<ng-content>`, and Angular's emulated view encapsulation does not reach projected content: a rule scoped to `auth-card.scss` gets a host attribute selector that the projected markup never carries, so it silently matches nothing. Scoping these classes would leave both auth screens unstyled with no compile error and no console warning. The source states this at the head of § 9, and both page stylesheets ship **zero** rules of their own as the other half of the arrangement.

## Anatomy

`.field` — the label-plus-control block, `margin-bottom: spacing.sp-4`. Its `label` is `display:block`, `typography.form-label`, `colors.text`, `margin-bottom: spacing.sp-2`. The control it wraps is an `.input-icon` (`Input.md`); on the change-password screen a `.form-error` div (`FormRow.md`) may follow the control inside the same `.field`.

`.field-row` — the options line under the last field: row flex, `align-items:center`, `justify-content:space-between`, `margin-bottom:20px` literal, `--fs-sm`. Its `label` is a flex row with gap `spacing.sp-2`, `colors.text`, `cursor:pointer`; its `a` is `colors.brand`, weight 700, underlined only on `:hover`.

`.login-error` — the form-level error block above the `<form>`: fill `colors.bad-bg`, a 1px edge in `colors.danger-border`, ink `colors.bad`, radius `rounded.sm`, padding `spacing.sp-3`, `--fs-sm`, `margin-bottom: spacing.sp-4`, `align-items:center`, gap `spacing.sp-3`. Its base rule is `display:none`; `.show` flips it to `display:flex`. Content is a `pi-exclamation-circle` glyph plus the message `<span>`.

`.btn-block` — the full-width submit: `width:100%`, `padding:11px` literal, `--fs-md`, centred flex with gap `spacing.sp-3` so an icon and a label sit together. It is a `.btn` modifier and takes every state from `Button.md`; it is listed here because the auth screens are its only call sites.

## Variants

| Variant | Classes / markup | Key values | When to use |
| --- | --- | --- | --- |
| Field block | `.field > label + .input-icon` | `margin-bottom: spacing.sp-4`; label `typography.form-label` | Every field on both auth screens |
| Field block with error | `.field > label + .input-icon + .form-error` | The error div carries `[id]`, targeted by the input's `aria-describedby` | `/doi-mat-khau` only — all three of its fields can carry a per-field message |
| Options row | `.field-row` | Checkbox label left, link right | `/dang-nhap` only — `Ghi nhớ đăng nhập` and `Quên mật khẩu?` |
| Form-level error | `.login-error.show` | The `bad` triad: `colors.bad-bg` fill, `colors.danger-border` edge, `colors.bad` ink | Both screens, above the `<form>`, wrapped in `@if`. On `/doi-mat-khau` it now carries **only** errors with no field attached — anything with `fields` renders under its own input instead |
| Full-width submit | `.btn.primary.btn-block` | `width:100%`, `padding:11px`, `--fs-md` | The single submit button on each auth screen |

Every input inside a `.field` carries the native `required` attribute and a real `autocomplete` token (`username`, `current-password`, `new-password`) — the only place in the app where either appears.

## States
<!-- Exactly these five rows, in this order — treatments as rendered by the shipped CSS. -->

Primary interactive element: `.field-row a` (the "Quên mật khẩu?" link). It is the only element this spec still owns that has interactive states of its own — the input's five states are `Input.md`'s, and the submit button's are `Button.md`'s.

| State | Treatment |
| --- | --- |
| default | `colors.brand`, `font-weight:700`, `text-decoration:none` |
| hover | `text-decoration: underline` — the only change; the colour does not shift |
| focus-visible | **Not styled here.** No `.field-row a:focus-visible` rule exists, so the link falls back to the browser's default focus ring rather than the app's `outline: 2px solid colors.brand` — the one control in the auth form outside the shared focus treatment |
| active / selected | **Not applicable** — no `:active` rule is authored, and a link with `href="javascript:void(0)"` has no selected concept |
| disabled | **Not applicable** — an `<a>` is not a form control; nothing on either auth screen disables it, and no rule exists |

### States — the containers (supplementary)

`.field`, `.field-row` and `.login-error` are static blocks: none is focusable, none carries a `:hover`, `:focus-visible`, `:active` or `:disabled` rule in `styles.scss`, and none is ever bound to `[disabled]`. `.login-error` has exactly one binary state, `display:none` ⇄ `.show`, and that is driven by whether Angular rendered it at all — see Normalize #1.

## Tokens Used
- `colors.text`, `colors.brand`, `colors.bad`, `colors.bad-bg`, `colors.danger-border` (the `.login-error` edge — the same token the `.btn.danger` border uses)
- `rounded.sm` (`.login-error`)
- `spacing.sp-2`, `spacing.sp-3`, `spacing.sp-4`
- `typography.form-label` (labels), `typography.table-cell` size (`--fs-sm`) for the options row and the error text, `typography.button-block-label` (`--fs-md`) for the submit
- Un-tokenised literals: `.field-row` `margin-bottom:20px`, `.btn-block` `padding:11px` — both catalogued in `Tokens/spacing.md` § Padding & gap literals bypassing the scale
- Icons: PrimeIcons v7 — `pi-exclamation-circle` (`.login-error`), `pi-sign-in` (the sign-in submit)

## Reference markup

```html
<!-- form-level error: only messages with no field attached reach this block -->
@if (errorMessage()) {
  <div class="login-error show">
    <i class="pi pi-exclamation-circle"></i>
    <span>{{ errorMessage() }}</span>
  </div>
}

<!-- a field: label, then an .input-icon (Input.md), then an optional per-field message -->
<div class="field">
  <label for="currentPassword">Mật khẩu hiện tại</label>
  <div class="input-icon">
    <i class="pi pi-lock"></i>
    <input id="currentPassword" type="password" placeholder="Nhập mật khẩu hiện tại"
      autocomplete="current-password" required
      [class.invalid]="!!fieldErrors()['CurrentPassword']"
      [attr.aria-invalid]="fieldErrors()['CurrentPassword'] ? 'true' : null"
      [attr.aria-describedby]="fieldErrors()['CurrentPassword'] ? errorId('CurrentPassword') : null"
      [value]="currentPassword()" (input)="onCurrentPasswordInput($event)" />
  </div>
  @if (fieldErrors()['CurrentPassword']; as message) {
    <div class="form-error" [id]="errorId('CurrentPassword')">{{ message }}</div>
  }
</div>

<div class="field-row">
  <label><input type="checkbox" class="check" /> Ghi nhớ đăng nhập</label>
  <a href="javascript:void(0)">Quên mật khẩu?</a>
</div>

<button type="submit" class="btn primary btn-block" [disabled]="submitting()">
  <i class="pi pi-sign-in"></i> {{ submitting() ? 'Đang đăng nhập…' : 'Đăng nhập' }}
</button>
```

Sources: `src/FE/src/styles.scss` (§ 9 `.field`, `.field label`, `.field-row`, `.login-error`, `.login-error.show`, plus its head comment on why the block is global; § 3.1 `.btn-block`), `src/FE/src/app/platform/login/pages/login/login.page.html`, `src/FE/src/app/platform/doi-mat-khau/pages/doi-mat-khau/doi-mat-khau.page.html`, `src/FE/src/app/shared/components/auth-card/auth-card.html` (the `<ng-content />` these classes are projected through)

## Do / Don't

- ✅ Keep these classes in global `styles.scss`. This is a content-projection constraint, not a stylistic preference — the source comment at the head of § 9 says so, and moving them scoped breaks both screens with no error message.
- ✅ Reach for `Input.md`'s `.input-icon` for the control inside a `.field`. There is no auth-only input tier any more; re-adding one would restart the divergence the 2026-08-29 merge closed.
- ✅ Keep `required` and a real `autocomplete` token on every auth input; password managers and the browser's own validation depend on them.
- ✅ Put a message with a field key **under its field** (`.form-error` inside the `.field`) and keep `.login-error` for messages that belong to no field. That split is what the change-password screen now ships.
- ✅ Express "in flight" on the submit button only — both screens bind `[disabled]="submitting()"` and swap the label to `Đang đăng nhập…` / `Đang lưu…`, and neither disables a field.
- ❌ Don't re-declare `.field`, `.field-row`, `.login-error` or `.btn-block` in a page or component stylesheet. One definition each; that is the rule `COMPONENTS.md` exists to hold.
- ❌ Don't use `.login-error` outside the auth screens. The in-page equivalent for the rest of the app is `NoticeBanner.md` (`.notice`), which ships four severities where this block has one.
- ❌ Don't stack a `.field-row` on the change-password screen; it has no options line, and the rhythm below the last field is tuned for the submit button following directly.

## Normalize on redesign
1. **`.login-error`'s `display:none` default is dead code.** The block is only rendered inside `@if (errorMessage())` **and** always with `.show` already applied, so the base rule can never be observed. It is a leftover from the pre-Angular pattern where JS toggled the class. Drop `display:none`/`.show` and let `@if` do the work.
2. **`Ghi nhớ đăng nhập` is not wired to anything** — the checkbox has no binding, no signal and no persistence — and `Quên mật khẩu?` is an `href="javascript:void(0)"` with no handler and no target route. Both render as working affordances that do nothing.
3. **The "Quên mật khẩu?" link has no `:focus-visible` rule**, so it is the only interactive element on the auth screens not carrying the app's 2px brand outline. It is also not a real link, which is why nobody noticed.
4. **Two off-scale literals remain**: `.field-row` `margin-bottom:20px` and `.btn-block` `padding:11px`. Both were left as-shipped when the input tier merged; neither has an exact match in the `--sp-*` scale, so each needs a decision rather than a swap.
5. **`.login-error` and `.notice.bad` are the same object twice.** Both are a `bad`-family strip with an icon and a message; `.notice` gained four severities on 2026-08-29 while `.login-error` kept exactly one. Folding the auth block into `NoticeBanner` would leave one error strip in the app instead of two.

## Resolved in the 2026-08-29 redesign
<!-- Items that used to sit in "Normalize on redesign" and were actually done. Kept, not deleted, so the history is not lost. -->
1. **A third input tier existed only for auth — resolved 2026-08-29.** `.field-input` was renamed `.input-icon` and merged with the user-list search field. The two had the same structure and differed only in icon size (15px vs 13px) and left inset (36px vs 32px); the merged class keeps the auth measurements and overrides them inside `.toolbar`, where the field must match the height of the button beside it. The input contract now lives in `Input.md`.
2. **The soft focus ring was the odd one out — resolved 2026-08-29 by generalising it, not removing it.** `outline:none` plus a 3px brand box-shadow is now the treatment for *every* input in the app, not just auth ones, so it is a deliberate input-wide convention rather than a one-screen exception. The forced-colours-mode concern moved with it to `Input.md`.
3. **`.toggle-visibility` was a fifth icon-button variant — resolved 2026-08-29.** The show/hide password button is now a plain `.icon-btn` positioned by `.input-icon .icon-btn`, so it shares one size, radius, hover fill and focus ring with every other icon-only button. Its old `colors.bg` hover fill — which disagreed with every other ghost button — went with it.
4. **Six off-scale literals in one family — reduced to two, 2026-08-29.** The glyph inset, glyph size, toggle inset and toggle padding all moved into `Input.md` with the merge. What remains here is `20px` and `11px`.
5. **Errors had nowhere to land except one shared block — resolved 2026-08-29.** `/doi-mat-khau` now renders a `.form-error` under each invalid field with `aria-invalid` and `aria-describedby` on the input, and `.login-error` is reserved for messages carrying no field key. Previously every 400 collapsed into one sentence at the top of the form.
6. **The edge colour had two names for one role — resolved 2026-08-29.** This spec used to carry a "token drift, not mine to fix" note saying `Tokens/colors.md` and `tokens.json` still called this edge `bad-border-notice`, one hex digit from `bad-border-btn`. Both docs have since landed the rename: `#e5a8a8` no longer ships and the single `colors.danger-border` (`#e0a8a8`) covers the `.btn.danger` border and the `.login-error` edge alike (`Tokens/colors.md` § Drift, `Tokens/tokens.json` → `light.color.danger-border`, `styles.scss` § `--danger-border`). Note removed 2026-08-29.

---
project: "PlatformManager"
status: "draft"
updated: "2026-08-22"
component: "AuthField"
sources:
  - "src/FE/src/styles.scss"
  - "src/FE/src/app/platform/login/pages/login/login.page.html"
  - "src/FE/src/app/platform/doi-mat-khau/pages/doi-mat-khau/doi-mat-khau.page.html"
---

# AuthField
**Description:** The icon-prefixed field treatment used only on the two auth screens (`.field` → `.field-input` → `input`, plus `.toggle-visibility`, `.field-row` and `.login-error`) — the app's **third and most decorated input tier**, beyond the filter and table-cell tiers in `Input.md` and the dialog tier in `FormRow.md`. All of it lives in global `styles.scss`, not in `auth-card.scss`, because the markup is content-projected through `AuthCard` and Angular's emulated encapsulation cannot reach projected content.

## Anatomy

`.field` — the label-plus-control block, `margin-bottom: spacing.sp-4`. Its `label` is `display:block`, `--fs-sm`, weight 700, `colors.text`, `margin-bottom: spacing.sp-2`.

`.field-input` — `position:relative`, row flex, `align-items:center`. Three layered children:
- **Leading glyph** `.pi` — `position:absolute; left:12px`, `colors.muted`, `font-size:15px` literal, `pointer-events:none` so it never steals the click that should focus the field.
- **`input`** — `width:100%`, border 1px `colors.border-strong`, radius `rounded.sm`, fill `colors.card`, padding `spacing.auth-input-padding` (`10px 12px 10px 36px` — the 36px left inset is what clears the glyph), `--fs-sm`, `colors.text`, `transition: border-color 0.15s ease, box-shadow 0.15s ease`. Noticeably taller than every other field in the app (10px vertical against `spacing.sp-2`'s 6px).
- **`.toggle-visibility`** (password fields only) — `position:absolute; right:10px`, `border:0`, `background:none`, `colors.muted`, `padding:4px`, `display:flex`, radius `6px` literal. Holds `pi-eye` / `pi-eye-slash`, swapped by the `showPassword()` signal, with a matching `aria-label`.

`.field-row` — the options line under the last field: row flex, `align-items:center`, `justify-content:space-between`, `margin-bottom:20px` literal, `--fs-sm`. Its `label` is a flex row with `gap:6px` literal, `colors.text`, `cursor:pointer`; its `a` is `colors.brand`, weight 700, no underline until `:hover`.

`.login-error` — the inline error block above the form: fill `colors.bad-bg`, border 1px `colors.bad-border`, text `colors.bad`, radius `rounded.sm`, padding `spacing.sp-3`, `--fs-sm`, `margin-bottom: spacing.sp-4`, `align-items:center`, `gap:8px` literal. Its base rule is `display:none`; `.show` flips it to `display:flex`. Content is a `pi-exclamation-circle` glyph plus the message `<span>`.

## Variants

| Variant | Classes / markup | Key values | When to use |
| --- | --- | --- | --- |
| Text field | `.field > label + .field-input > i.pi + input` | 36px left inset for the glyph | `/dang-nhap` → `Email` (`pi-envelope`, `type="text"`, `autocomplete="username"`, placeholder `ten@congty.vn`) |
| Password field with toggle | adds `.toggle-visibility` | `[type]` swaps `password` ⇄ `text`; icon swaps `pi-eye` ⇄ `pi-eye-slash` | `/dang-nhap` → `Mật khẩu` (`pi-lock`, placeholder `Nhập mật khẩu`) — the **only** field in the app with a visibility toggle |
| Password field without toggle | `.field-input` with no trailing button | Fixed `type="password"` | All three fields on `/doi-mat-khau`: `Mật khẩu hiện tại` (`pi-lock`, `Nhập mật khẩu hiện tại`), `Mật khẩu mới` (`pi-key`, `Nhập mật khẩu mới`), `Xác nhận mật khẩu mới` (`pi-key`, `Nhập lại mật khẩu mới`) |
| Options row | `.field-row` | Checkbox label left, link right | `/dang-nhap` only — `Ghi nhớ đăng nhập` and `Quên mật khẩu?` |
| Inline error | `.login-error.show` | `colors.bad` triad: `bad-bg` fill, `bad-border` edge, `bad` ink | Both screens, above the `<form>`, wrapped in `@if (errorMessage())` |

Every field also carries the native `required` attribute and a real `autocomplete` token (`username`, `current-password`, `new-password`) — the only place in the app where either appears.

## States
<!-- Exactly these five rows, in this order — treatments as rendered by the shipped CSS. -->

Primary interactive element: `.field-input input`.

| State | Treatment |
| --- | --- |
| default | Border 1px `colors.border-strong`, radius `rounded.sm`, fill `colors.card`, `colors.text`, padding `spacing.auth-input-padding`, `--fs-sm`; leading glyph `colors.muted` at 15px |
| hover | **Not styled** — `styles.scss:533-548` authors no `:hover` for the input. The only hover in this family is on `.toggle-visibility` (see below) |
| focus-visible | The app's **only soft-ring focus treatment**: `outline:none`, `border-color: colors.brand`, `box-shadow: colors.shadow-focus-ring` (`0 0 0 3px` brand @ 12%). Every other control in the app uses `outline: 2px solid var(--brand)` instead |
| active / selected | **Not applicable** — a text input has no pressed or selected state, and no `:active` rule is authored for any selector in this family |
| disabled | **Not applicable** — no auth input is ever disabled: neither page binds `[disabled]` on a field, and no `:disabled` rule exists in `styles.scss:502-624`. Both pages express "in flight" on the **submit button** instead (`[disabled]="submitting()"` plus a label swap to `Đang đăng nhập…` / `Đang lưu…`), which is `Button.md`'s state |

### States — `.toggle-visibility` (supplementary)

| State | Treatment |
| --- | --- |
| default | `border:0`, `background:none`, `colors.muted`, `padding:4px`, radius `6px` literal, absolutely placed at `right:10px` |
| hover | `color: colors.text`, `background: colors.bg` — note it fills with the **page** background, not `colors.surface-2` like every other ghost button in the app |
| focus-visible | `outline: 2px solid colors.brand`, `outline-offset: 1px` |
| active / selected | No `:active` rule. The pressed/toggled semantic is carried by the icon swap (`pi-eye` ⇄ `pi-eye-slash`) and the `aria-label` swap (`Hiện mật khẩu` ⇄ `Ẩn mật khẩu`) — there is no `aria-pressed` |
| disabled | **Not applicable** — never disabled; no binding, no rule |

## Tokens Used
- `colors.card`, `colors.border-strong`, `colors.brand`, `colors.text`, `colors.muted`, `colors.bg`, `colors.bad`, `colors.bad-bg`, `colors.bad-border`, `colors.shadow-focus-ring`
- `rounded.sm`; the toggle's `6px` radius is an un-tokenised literal
- `spacing.sp-2`, `spacing.sp-3`, `spacing.sp-4`, `spacing.auth-input-padding`
- `typography.form-label` (labels), `typography.table-cell` size (`--fs-sm`) for inputs, options row and error text
- Un-tokenised literals: glyph `left:12px` / `font-size:15px`, toggle `right:10px` / `padding:4px`, `.field-row` `margin-bottom:20px` and `gap:6px`, `.login-error` `gap:8px` — all catalogued in `Tokens/spacing.md`
- Icons: PrimeIcons v7 — `pi-envelope`, `pi-lock`, `pi-key`, `pi-eye`, `pi-eye-slash`, `pi-exclamation-circle`

## Reference markup

```html
@if (errorMessage()) {
  <div class="login-error show">
    <i class="pi pi-exclamation-circle"></i>
    <span>{{ errorMessage() }}</span>
  </div>
}

<div class="field">
  <label for="password">Mật khẩu</label>
  <div class="field-input">
    <i class="pi pi-lock"></i>
    <input id="password" [type]="showPassword() ? 'text' : 'password'" placeholder="Nhập mật khẩu"
      autocomplete="current-password" required [value]="password()" (input)="onPasswordInput($event)" />
    <button type="button" class="toggle-visibility" (click)="togglePasswordVisibility()"
      [attr.aria-label]="showPassword() ? 'Ẩn mật khẩu' : 'Hiện mật khẩu'">
      <i class="pi" [class.pi-eye]="!showPassword()" [class.pi-eye-slash]="showPassword()"></i>
    </button>
  </div>
</div>

<div class="field-row">
  <label><input type="checkbox" /> Ghi nhớ đăng nhập</label>
  <a href="javascript:void(0)">Quên mật khẩu?</a>
</div>
```

Sources: `src/FE/src/styles.scss:502-507` (why these classes are global), `:508-518` (`.field`), `:520-571` (`.field-input`, glyph, input, toggle), `:573-597` (`.field-row`), `:609-624` (`.login-error`), `src/FE/src/app/platform/login/pages/login/login.page.html:2-53`, `src/FE/src/app/platform/login/pages/login/login.page.scss:1-6` (confirms zero component-scoped CSS), `src/FE/src/app/platform/doi-mat-khau/pages/doi-mat-khau/doi-mat-khau.page.html:2-56`

## Do / Don't

- ✅ Keep these classes in global `styles.scss`. This is a technical constraint of content projection through `AuthCard`, not a stylistic preference — the source comment at `styles.scss:502-507` says so explicitly.
- ✅ Give every field a leading `pi` glyph and keep `pointer-events:none` on it; without that the glyph swallows clicks near the left edge.
- ✅ Keep the 36px left inset in step with the glyph's `left:12px` — they are two halves of one measurement.
- ✅ Keep `required` and a real `autocomplete` token on every auth input; password managers and the browser's own validation depend on them.
- ✅ Show failures inline in `.login-error` **and** let `httpErrorInterceptor` toast transport-level failures — the two channels are complementary, not duplicates.
- ❌ Don't reuse this tier outside the auth screens. Its height, glyph inset and soft focus ring are all specific to a 380px centred card.
- ❌ Don't disable the fields while submitting; the shipped pattern disables only the submit button.

## Normalize on redesign
1. **`.login-error`'s `display:none` default is dead code.** The block is only ever rendered inside `@if (errorMessage())` **and** always with `.show` already applied, so the base rule can never be observed. It is a leftover from the static prototype where JS toggled the class. Drop `display:none`/`.show` and let `@if` do the work.
2. **`Ghi nhớ đăng nhập` is not wired to anything** — the checkbox has no binding, no signal and no persistence — and `Quên mật khẩu?` is an `href="javascript:void(0)"` with no handler and no target route. Both render as working affordances that do nothing.
3. **This family is a third input tier**, with its own height, border colour and focus treatment. Converging it with the filter and dialog tiers is the library-wide item in `COMPONENTS.md`.
4. **The focus ring is the odd one out.** Every other control uses a 2px brand outline; only this one uses `outline:none` plus a soft box-shadow. Removing the outline entirely is also a forced-colours-mode risk.
5. **`.toggle-visibility` hover fills with `colors.bg`** while `.action-btn`, `.toast-close` and `.sidebar-toggle` all use `colors.surface-2` for the same gesture.
6. **Six off-scale literals** in one family (`12px`, `15px`, `10px`, `4px`, `20px`, `6px`) — the densest cluster of un-tokenised values in the app.
7. **No `aria-pressed` on the visibility toggle**; the state is conveyed only by the swapped `aria-label`.

---
project: "PlatformManager"
status: "draft"
updated: "2026-08-22"
component: "Input"
sources: ["src/FE/src/styles.scss", "src/FE/src/app/modules/danh-muc-dti/components/criteria-grid-table/criteria-grid-table.scss", "src/FE/src/app/platform/login/pages/login/login.page.html", "src/FE/src/app/platform/quan-tri-nguoi-dung/pages/quan-tri-nguoi-dung/quan-tri-nguoi-dung.page.scss"]
---

# Input
**Description:** Native form controls (`<input>`, `<select>`, `<textarea>`). The shipped app defines **four visual tiers plus a checkbox treatment** for what is conceptually one field role — they differ in border colour, padding and adornments. This spec documents all of them rather than picking one; convergence is a Normalize item.

All controls inherit `font: inherit` from the global reset (`styles.scss:104-109`), so no tier restates the font family.

## Anatomy
Single-line native control, no floating label. Labels are always external — a `<label>` above the field in the form and auth tiers, a placeholder or `title` attribute in the filter tier, and a column header in the table tier. Only the auth tier has in-field adornments (a leading icon and, on the password field, a trailing reveal toggle).

## Variants

| Variant | Classes / selector | Key values | When to use |
| --- | --- | --- | --- |
| Filter / weekbar | `.filters input`, `.filters select`, `.weekbar input`, `.weekbar select` | border 1px `colors.border-strong`, `rounded.sm`, bg `colors.card`, `spacing.input-padding`, `typography.table-cell` (`styles.scss:347-363`) | Above-grid filters and the dashboard period toolbar: criteria search, group/year/period/change/sort selects (`criteria-table.html:8-26`, `danh-muc-dti.page.html:14-35`, `period-toolbar.html:9-34`) |
| Filter with search adornment | `.search input` + `.search .pi` | filter tier + `width:100%`, `padding-left:32px`; icon absolutely positioned at `left:10px`, `13px`, `colors.muted` (`quan-tri-nguoi-dung.page.scss:1-19`) | The user-grid search box only — the sole filter in the app with a magnifier |
| Table cell edit | `.progressInput`, `.noteInput` | border 1px `colors.border-strong`, `rounded.sm`, `padding:5px`, `typography.table-cell`; progress is `width:74px`, `text-align:right`, spinners suppressed (`appearance:textfield`); note is `width:100%`, `min-width:130px` (`criteria-grid-table.scss:31-55`) | Inline double-click editing of `Tiến độ %` and `Ghi chú` in the DTI catalogue grid, rendered only while `isEditing()` is true |
| Form row (dialog) | `.form-row input`, `.form-row select`, `.form-row textarea` | border 1px **`colors.line`** — the faint tier, not `border-strong` — `rounded.sm`, `spacing.input-padding`, bg `colors.card`, `width:100%`, `typography.table-cell`; textarea adds `min-height:64px`, `resize:vertical` (`styles.scss:436-450`) | Every dialog form field: criteria create/edit, user create/edit, CSV import file picker |
| Auth field | `.field-input input` | border 1px `colors.border-strong`, `rounded.sm`, bg `colors.card`, `spacing.auth-input-padding` (`10px 12px 10px 36px` — left inset reserves room for the icon), `typography.table-cell`, text `colors.text`, transition on `border-color`/`box-shadow` (`styles.scss:533-548`) | Sign-in and change-password fields only |
| Checkbox | `input[type="checkbox"]` | `16px` square, `accent-color: colors.brand`, `cursor:pointer` (`permission-matrix.scss:17-27`, `resource-permission-matrix.scss:8-24`) | The two permission matrices. Also used unstyled (browser default) for `role-checkboxes` in the user dialog (`user-form-dialog.html:48`) and the sign-in "Ghi nhớ đăng nhập" box (`login.page.html:51`) |

**Label treatments differ by tier too:** `.form-row label` is `typography.form-label` with `gap:var(--sp-2)` from its field (`styles.scss:429-434`); `.field label` is the same size/weight but `display:block` with `margin-bottom:var(--sp-2)` (`styles.scss:511-517`). Required fields append `<span class="required">*</span>` in the dialog tier only.

## States
<!-- Exactly these five rows, in this order — treatments as rendered by the shipped CSS. -->

| State | Treatment |
| --- | --- |
| default | per-tier border/radius/padding/background above; `font: inherit` from `styles.scss:104-109` |
| hover | **Not styled on any native control** — no `:hover` rule for `input`/`select`/`textarea` in any tier. (PrimeNG's own form widgets would use `formField.hoverBorderColor: BRAND` from the preset, but the app uses native controls everywhere, so this never renders.) The auth password **reveal toggle** does have one: `color: colors.text`, bg `colors.bg` (`styles.scss:561-564`) |
| focus | **Ships, and differs by tier.** Filter/weekbar: `outline: 2px solid colors.brand; outline-offset: 1px` (`styles.scss:359-362`). Auth: outline suppressed and replaced by `border-color: colors.brand` + `box-shadow: 0 0 0 3px rgba(15,91,215,.12)` (`styles.scss:543-547`). Checkboxes: `outline: 2px solid colors.brand; outline-offset: 2px`. **Table-cell and form-row tiers author no focus rule** — browser UA default only |
| active | **Not styled** — no `:active` rule on any control; the `.btn:active` press offset does not apply to fields |
| disabled | **Reachable only for checkboxes.** The resource matrix disables the break-glass role column: `cursor: not-allowed; opacity: .6` (`resource-permission-matrix.scss:20-24`); the menu matrix disables all boxes while `loading()`. **No text input or select in the app is ever `disabled` or `readonly`** — read-only screens hide the control instead (`danh-muc-dti.page.html:36-41` drops the whole action bar; `criteria-grid-table.html:66-79` renders plain text instead of an editable span) |

## Tokens Used
- `colors.card`, `colors.line`, `colors.border-strong`, `colors.brand`, `colors.text`, `colors.muted`, `colors.bg`
- `rounded.sm`
- `spacing.input-padding`, `spacing.auth-input-padding`, `spacing.sp-2`
- `typography.table-cell`, `typography.form-label`

`padding:5px` (table tier), `width:74px`, `min-width:130px`, the `16px` checkbox box and the `rgba(15,91,215,.12)` focus glow are literals in the source with no token behind them.

## Reference markup

```html
<!-- filter tier -->
<input placeholder="Tìm mã hoặc tên chỉ tiêu..." [value]="searchText()" (input)="onSearchInput($event)" />
<select [value]="groupFilter()" (change)="onGroupFilterChange($event)">…</select>

<!-- filter tier with search adornment -->
<div class="search">
  <i class="pi pi-search"></i>
  <input type="text" placeholder="Tìm theo tên hoặc email..." [value]="searchInput()" (input)="onSearchInputEvent($event)" />
</div>

<!-- table-cell tier (only while editing) -->
<input #progressInput appAutofocus type="number" min="0" max="100" step="0.1" class="progressInput"
       [value]="row.ProgressPercent ?? 0" (keydown)="onCellKeydown($event, row, 'progress', progressInput)" />

<!-- form-row tier -->
<div class="form-row">
  <label for="ufEmail">Email <span class="required">*</span></label>
  <input id="ufEmail" type="email" placeholder="ten@congty.vn" [value]="emailField()" (input)="onEmailInput($event)" />
</div>

<!-- auth tier, icon + reveal toggle -->
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
```

Sources: `src/FE/src/styles.scss:104-109` (font reset), `:343-359` (filter tier), `:419-447` (form-row tier), `:504-567` (auth `.field`/`.field-input`/toggle), `src/FE/src/app/modules/danh-muc-dti/components/criteria-grid-table/criteria-grid-table.scss:31-55` (table tier) + `criteria-grid-table.html:45-115` (edit markup), `src/FE/src/app/platform/quan-tri-nguoi-dung/pages/quan-tri-nguoi-dung/quan-tri-nguoi-dung.page.scss:1-19` (search adornment), `src/FE/src/app/platform/login/pages/login/login.page.html:10-53`, `src/FE/src/app/platform/phan-quyen/components/permission-matrix/permission-matrix.scss:17-27`, `src/FE/src/app/platform/phan-quyen/components/resource-permission-matrix/resource-permission-matrix.scss:8-24`

## Do / Don't

- ✅ Match the tier to the context: `border-strong` where the user types into a bare surface (filters, table cells, auth), `line` inside a dialog where the card already frames the form.
- ✅ Keep `appAutofocus` on the first field of a dialog and on a cell entering edit mode — the shipped pattern in both places.
- ✅ Clamp `.progressInput` with `min="0" max="100" step="0.1"` and suppress the number spinners, as shipped.
- ✅ Express read-only by **not rendering the control** — that is how both read-only paths work today; do not introduce a `disabled` text input, which has no styling.
- ❌ Don't add a visible per-field validation state — none exists. Errors surface in one shared block: `.form-error` in dialogs (`styles.scss:453-457`) and `.login-error` on the auth screens (`styles.scss:609-624`).
- ❌ Don't add a `:hover` border to native fields; only the auth reveal toggle has one.
- ❌ Don't rely on PrimeNG form-field theming — the preset maps it (`platform-manager-preset.ts:109-118`) but no PrimeNG input component is used anywhere.

## Normalize on redesign
1. **Four tiers for one role.** Border colour alone splits three ways (`border-strong` in filter/table/auth, `line` in dialogs) and padding four ways (`--sp-2 --sp-3`, `5px`, `10px 12px 10px 36px`). Converge on one input treatment with documented modifiers.
2. **Focus is inconsistent and partly absent.** Two different focus treatments ship (outline ring vs. border + glow) and the table-cell and form-row tiers have **none at all** — dialog forms and inline cell edits currently rely on the browser default.
3. No field-level error state. A dialog with five fields shows one message for whichever rule failed first, with no indication of which field is at fault.
4. `padding:5px` and `width:74px` in the table tier are off every spacing scale.
5. The auth focus glow `rgba(15,91,215,.12)` duplicates `colors.surface-nav-active`'s value as a literal — same colour, two uncontrolled copies.
6. Checkbox styling is duplicated verbatim in `permission-matrix.scss` and `resource-permission-matrix.scss`, and the user dialog's and sign-in's checkboxes get neither — so three of five checkbox call sites are unstyled browser default.

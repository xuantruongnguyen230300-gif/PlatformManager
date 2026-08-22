---
project: "PlatformManager"
status: "draft"
updated: "2026-08-22"
component: "FormRow"
sources:
  - "src/FE/src/styles.scss"
  - "src/FE/src/app/platform/quan-tri-nguoi-dung/components/user-form-dialog/user-form-dialog.html"
  - "src/FE/src/app/platform/quan-tri-nguoi-dung/components/user-form-dialog/user-form-dialog.scss"
  - "src/FE/src/app/modules/danh-muc-dti/components/criteria-form-dialog/criteria-form-dialog.html"
  - "src/FE/src/app/modules/danh-muc-dti/components/criteria-form-dialog/criteria-form-dialog.scss"
  - "src/FE/src/app/modules/danh-muc-dti/components/import-dialog/import-dialog.html"
---

# FormRow
**Description:** The stacked label-over-field group used inside every `<dialog>` form (`.form-row`, global), together with its three companions: the required marker (`.required`), the form-level error line (`.form-error`), and the horizontal checkbox group (`.role-checkboxes`). This is the app's **third input treatment** — distinct from the filter tier and the table-cell tier documented in `Input.md`, and from the auth tier in `AuthField.md`.

## Anatomy

`.form-row` — column flex, gap `spacing.sp-2`, `margin-bottom: spacing.sp-4`. Two nested rules do the real work:
- **Label** — matches both `label` and `.form-row-label`: `--fs-sm`, weight 700, `colors.text`. The `.form-row-label` alias exists so a *group* of controls can carry a caption without an invalid `<label for>` — the roles group uses `<span id="ufRolesLabel" class="form-row-label">` plus `aria-labelledby`.
- **Field** — matches `input`, `select` and `textarea` alike: border 1px `colors.line`, radius `rounded.sm`, padding `spacing.sp-2` `spacing.sp-3`, fill `colors.card`, `width:100%`, `--fs-sm`. `textarea` adds `min-height:64px` and `resize:vertical`.

Note the border tier: the dialog field uses the faint `colors.line`, **not** the `colors.border-strong` that the filter and auth fields use. That is what makes this a third tier rather than a reuse.

`.required` — the `*` marker inside a label, `color: colors.bad`. It is **not** global: the identical three-line rule is declared separately in `user-form-dialog.scss:1-3` and `criteria-form-dialog.scss:1-3`.

`.form-error` (global) — `colors.bad`, `--fs-sm`, margin `spacing.sp-2` 0. Rendered conditionally, once per dialog, below the last row and above the action bar. It carries either a local validation message or a server message; there is **no per-field error slot** anywhere in the app.

`.role-checkboxes` (component-scoped, user dialog only) — row flex, gap `spacing.sp-4`, `margin-top: spacing.sp-1`, `role="group"` + `aria-labelledby`. Each entry is a `.role-checkbox` `<label>`: row flex, gap `6px` literal, `--fs-sm`, weight 600, `cursor:pointer`, wrapping a native `<input type="checkbox">` and the role name as a text node. A sibling `.role-preserved` paragraph (`margin: spacing.sp-2 0 0`, `--fs-sm`, `colors.muted`) appears only when the edited user holds roles outside the assignable set.

`.form-grid` (global) — `display:grid`, `1fr 1fr`, gap `0 spacing.sp-5`. It exists purely to place two `.form-row`s side by side; the criteria dialog is its only user.

## Variants

| Variant | Classes | Key values | When to use |
| --- | --- | --- | --- |
| Text field row | `form-row` | `<label for>` + `<input>` | Tên đăng nhập, Email, Họ tên, Mật khẩu tạm, Mã |
| Textarea row | `form-row` | `min-height:64px`, `resize:vertical` | Tên chỉ tiêu |
| Select row | `form-row` | Same box as the input; native `<select>` | Nhóm |
| Number row | `form-row` | `type="number"` with `min`/`step`; no spinner suppression here (unlike the grid's `.progressInput`) | Điểm tối đa |
| File row | `form-row` | `<input type="file" accept=".csv,.xlsx,.xls">` — the shared field rule applies, so the UA file button sits inside a bordered 100%-wide box | Import dialog |
| Group-caption row | `form-row` + `.form-row-label` | `<span>` caption + `aria-labelledby`, not `<label for>` | Vai trò (the checkbox group) |
| Paired rows | `.form-grid > .form-row × 2` | Two columns, `0 spacing.sp-5` gap | Nhóm + Điểm tối đa in the criteria dialog |
| Required marker | `.required` | `colors.bad` asterisk appended inside the label text | Every field in both dialogs except the import file picker |
| Form-level error | `.form-error` | `colors.bad`, `--fs-sm` | One per dialog, conditional |

## States
<!-- Exactly these five rows, in this order — treatments as rendered by the shipped CSS. -->

Primary interactive element: the `input` / `select` / `textarea` inside `.form-row`.

| State | Treatment |
| --- | --- |
| default | Border 1px `colors.line`, radius `rounded.sm`, fill `colors.card`, padding `spacing.sp-2` `spacing.sp-3`, `width:100%`, `--fs-sm`; label above in `--fs-sm`/700 `colors.text` |
| hover | **Not styled** — `styles.scss` authors no `:hover` rule for `.form-row input/select/textarea`, and neither dialog stylesheet adds one |
| focus-visible | **Not styled — and this is the gap, not a design choice.** Unlike `.filters input` (`styles.scss:359-362`) and `.field-input input` (`:543-547`), the `.form-row` field rule has **no** `:focus-visible` block, so dialog fields fall back to the browser's default focus ring. Verified by reading `styles.scss:423-451` plus both dialog stylesheets |
| active / selected | **Not applicable** for the text fields — no `:active` rule and no selected concept. For `.role-checkboxes` the selected state is the native checked box, styled only by `accent-color` on the permission matrices, not here — the dialog's checkboxes are entirely UA-rendered |
| disabled | **Not applicable** — no `.form-row` field is ever disabled: no `[disabled]` binding exists on any of them, and no `:disabled` rule is authored. The dialogs express "not allowed" by **omitting** rows instead: Tên đăng nhập and Mật khẩu tạm are rendered only in create mode (`@if (!editing())`), never shown greyed out. The only disabled control in these dialogs is the submit `.btn` in the import dialog, which belongs to `Button.md` |

## Tokens Used
- `colors.card`, `colors.line`, `colors.text`, `colors.bad`, `colors.muted`
- `rounded.sm`
- `spacing.sp-1`, `spacing.sp-2`, `spacing.sp-3`, `spacing.sp-4`, `spacing.sp-5`, `spacing.form-grid` (the `1fr 1fr` template in `Tokens/spacing.md` § Grid templates)
- `typography.form-label` (12px/700, labels), `typography.table-cell` size (`--fs-sm`, fields and messages)
- Un-tokenised literals: `min-height:64px` on `textarea`, `gap:6px` in `.role-checkbox` — catalogued in `Tokens/spacing.md`

## Reference markup

```html
<div class="form-row">
  <label for="ufEmail">Email <span class="required">*</span></label>
  <input id="ufEmail" type="email" placeholder="ten@congty.vn" [value]="emailField()" (input)="onEmailInput($event)" />
</div>

<div class="form-grid">
  <div class="form-row">
    <label for="cfGroup">Nhóm <span class="required">*</span></label>
    <select id="cfGroup" #groupSelect [value]="…">…</select>
  </div>
  <div class="form-row">
    <label for="cfMax">Điểm tối đa <span class="required">*</span></label>
    <input id="cfMax" #maxScoreInput type="number" min="0.01" step="0.01" placeholder="vd 10" [value]="…" />
  </div>
</div>

<div class="form-row">
  <span id="ufRolesLabel" class="form-row-label">Vai trò <span class="required">*</span></span>
  <div class="role-checkboxes" role="group" aria-labelledby="ufRolesLabel">
    @for (role of assignableRoles; track role) {
      <label class="role-checkbox">
        <input type="checkbox" [checked]="isRoleSelected(role)" (change)="toggleRole(role)" />
        {{ role }}
      </label>
    }
  </div>
  @if (preservedRoles().length > 0) {
    <p class="role-preserved">
      Vai trò hệ thống: <strong>{{ preservedRoles().join(', ') }}</strong> — giữ nguyên, không
      thay đổi khi lưu.
    </p>
  }
</div>

@if (errorMessage()) {
  <div class="form-error">{{ errorMessage() }}</div>
}
```

The checkbox group renders exactly two boxes — `ASSIGNABLE_ROLES = ['Admin', 'User']`. `SuperAdmin` is deliberately absent from the picker and is instead preserved verbatim through save, which is what `.role-preserved` explains to the user.

Sources: `src/FE/src/styles.scss:422-451` (`.form-row`), `:453-457` (`.form-error`), `:459-463` (`.form-grid`), `src/FE/src/app/platform/quan-tri-nguoi-dung/components/user-form-dialog/user-form-dialog.html:7-65`, `src/FE/src/app/platform/quan-tri-nguoi-dung/components/user-form-dialog/user-form-dialog.scss:1-24`, `src/FE/src/app/platform/quan-tri-nguoi-dung/models/quan-tri-nguoi-dung.model.ts:82`, `src/FE/src/app/platform/quan-tri-nguoi-dung/components/user-form-dialog/user-form-dialog.ts:68`, `:77-79`, `src/FE/src/app/modules/danh-muc-dti/components/criteria-form-dialog/criteria-form-dialog.html:7-34`, `src/FE/src/app/modules/danh-muc-dti/components/criteria-form-dialog/criteria-form-dialog.scss:1-3`, `src/FE/src/app/modules/danh-muc-dti/components/import-dialog/import-dialog.html:7-10`

## Do / Don't

- ✅ Keep one `.form-row` per field, label first, and pair `<label for>` with the control's `id` — every text field in both dialogs does.
- ✅ Use `.form-row-label` (not `<label>`) whenever the caption describes a *group*, and back it with `role="group"` + `aria-labelledby`.
- ✅ Put the error line once, between the last row and `.dialog-actions`, and drive it from a single `errorMessage()` that merges local and server messages.
- ✅ Omit a field the user may not edit rather than disabling it — that is the shipped pattern for Tên đăng nhập and Mật khẩu tạm in edit mode.
- ✅ Keep the roles group data-driven from `ASSIGNABLE_ROLES` and keep re-sending any role outside it; the payload is whole-set, so a dropped role is a silent privilege change.
- ❌ Don't use `colors.border-strong` on a dialog field — that would merge this tier into the filter tier without a decision.
- ❌ Don't invent a per-field inline error; nothing in the app renders one, and screens generated with one would not match.

## Normalize on redesign
1. **Dialog fields have no authored focus style.** The other two input tiers each ship one; this one relies on the UA default, so focus visibility varies by browser. Highest-value fix in this spec.
2. **`.required` is duplicated verbatim in two component stylesheets** (`user-form-dialog.scss:1-3`, `criteria-form-dialog.scss:1-3`) for a rule that is used in exactly the same way in both. It belongs next to `.form-row` in `styles.scss`.
3. **A third input treatment exists at all.** Filter tier (`colors.border-strong` + focus ring), dialog tier (`colors.line`, no focus ring) and auth tier (`colors.border-strong` + brand border + soft ring) are three answers to one question — see `COMPONENTS.md` § Known inconsistencies.
4. **Only form-level errors are possible.** The BE envelope already returns per-field errors (`Fields` in the API result), but the UI has no slot to render them, so field-specific feedback is flattened into one line.
5. **`.role-checkbox` uses a `6px` literal gap** where `spacing.sp-2` is 6px, and `min-height:64px` on `textarea` is entirely off-scale.
6. **`.form-grid` is a two-column grid with no responsive collapse** — at `spacing.breakpoint-mobile` the paired rows stay side by side inside a `min(560px, 92vw)` dialog.

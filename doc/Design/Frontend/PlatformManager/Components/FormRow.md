---
kind: luat
scope: du-an
verified: 2026-09-08
project: "PlatformManager"
status: "draft"
updated: "2026-09-08"
component: "FormRow"
sources:
  - "src/FE/src/styles.scss"
  - "src/FE/src/app/platform/quan-tri-nguoi-dung/components/user-form-dialog/user-form-dialog.html"
  - "src/FE/src/app/platform/quan-tri-nguoi-dung/components/user-form-dialog/user-form-dialog.scss"
  - "src/FE/src/app/platform/quan-tri-nguoi-dung/components/user-form-dialog/user-form-dialog.ts"
---

# FormRow

> 🗄️ **Chỗ nào dưới đây nhắc `/dashboard` hoặc `/danh-muc/dti` là LỊCH SỬ, không phải hiện trạng.**
> Module nghiệp vụ `DtiWeekly` — cả cây `src/FE/src/app/modules/` — đã xoá 2026-08-29 để xây lại; danh
> sách route sống hôm nay đọc thẳng ở `src/FE/src/app/app.routes.ts`. Spec này vẫn là `kind: luat` vì
> component còn dùng thật trên màn Core, nên **không** hạ cả file xuống `lich-su` — chỉ những chỗ nhắc
> màn đã xoá mới bị hạ cấp, và chúng đều mang dấu 🗄️ hoặc được viết ở thì quá khứ. Nội dung đầy đủ
> của hai màn đó nằm ở file chủ lịch sử [`../Screens/01-dashboard.md`](../Screens/01-dashboard.md) và
> [`../Screens/02-danh-muc-dti.md`](../Screens/02-danh-muc-dti.md) (§5 — một chủ đề một file chủ); code
> tra ở commit `98a5d96`.
>
> ✅ **Đối chiếu TOÀN FILE với source ngày 2026-09-08** (`verified: 2026-09-08`). Lượt đó tìm ra file
> tự cãi chính nó ở **5 chỗ**, cãi `Input.md` ở 4 chỗ và cãi source ở 3 chỗ — mỗi chỗ nay được sửa
> tại chỗ kèm dòng ghi lý do, không xoá lặng lẽ (§4, §5).
>
> ⚠️ **Bản trước của khối này viết** *"Chưa đối chiếu lại toàn file … đúng như `verified:
> chua-doi-chieu` ở frontmatter"* trong khi frontmatter khi đó ghi `verified: 2026-09-06`. Hai câu
> cạnh nhau, ngược nhau, và **không cổng nào bắt được**: `check-docs.sh` §10 chỉ hỏi *"có đủ ba
> khoá không"*, không đọc xem thân file có nói ngược lại khoá đó không. Giữ lại vì đây là cách hỏng
> rẻ nhất để tái phát — một lần sửa frontmatter mà quên thân file.
>
> Trích dẫn dạng `styles.scss § <selector>` chỉ neo theo tên selector chứ không theo số dòng; giá trị
> thật lấy bằng lệnh, đừng tin số chép trong bảng:
> `grep -n '^\.form-row\|^\.required' src/FE/src/styles.scss`.

**Description:** The stacked label-over-field group used inside every `<dialog>` form (`.form-row`, global), together with its three companions: the required marker (`.required`), the error line (`.form-error`, rendered **per field and at form level**), and the horizontal checkbox group (`.role-checkboxes`). The field **inside** the row is not this spec's to define: since the 2026-08-29 consolidation `.form-row input`, `.form-row select` and `.form-row textarea` render from the one shared input contract documented in `Input.md`, alongside `.input`, the toolbar fields and `.input-icon input`. This spec owns the row — label, layout, required marker, error line, checkbox group — not the box.

## Anatomy

`.form-row` — column flex, gap `spacing.sp-2`, `margin-bottom: spacing.sp-4`. Two nested rules do the real work:
- **Label** — matches both `label` and `.form-row-label`: `--fs-sm`, weight 700, `colors.text`. The `.form-row-label` alias exists so a *group* of controls can carry a caption without an invalid `<label for>` — the roles group uses `<span id="ufRolesLabel" class="form-row-label">` plus `aria-labelledby`.
- **Field** — `input`, `select` and `textarea` are **not** styled by a rule of their own. They appear in the shared selector list at `src/FE/src/styles.scss` § `.form-row input` (the same declaration that serves `.input`, `.toolbar input`/`select` and `.input-icon input`), which gives them border 1px `colors.border-strong`, radius `rounded.sm`, padding `spacing.sp-2` `spacing.sp-3`, fill `colors.card` and `--fs-sm`. What `.form-row` adds on top is only `width:100%`; `textarea` adds `min-height:64px` and `resize:vertical` (§ `.form-row textarea`). Focus, disabled and invalid come from that same shared list — §§ `.form-row input:focus-visible`, `.form-row input:disabled`, `.form-row input.invalid`.

> ⚠️ **`width:100%` is the default, not what the shipped dialog renders.** Every one of the four text fields in `user-form-dialog.html` carries `class="input w-md"`, and `styles.scss` § `.form-row .input.w-md` re-declares the width at higher specificity precisely so the variant beats the default — so they render at 240px, not full-row. Recorded 2026-09-08; the width variants are `Input.md`'s to own, this note only stops a reader concluding the dialog is a column of full-width boxes. Check: `grep -n 'class="input' src/FE/src/app/platform/quan-tri-nguoi-dung/components/user-form-dialog/user-form-dialog.html`.

> ### 🔄 SỬA 2026-09-08 — there is no separate dialog border tier, and there has not been since 2026-08-29
>
> This paragraph used to read: *"Note the border tier: the dialog field uses the faint `colors.line`, **not** the `colors.border-strong` that the filter and auth fields use. That is what makes this a third tier rather than a reuse."* The bullet above it said `border 1px colors.line`.
>
> Both were wrong. The shipped stylesheet puts `.input`, `.toolbar input`/`select`, `.form-row input`/`select`/`textarea` and `.input-icon input` on **one** selector list with **one** `border: 1px solid var(--border-strong)`, and the comment directly above that list records the merge by name — it states that the pre-consolidation code had three versions and that the dialog one used the faint `--line`, i.e. exactly what this spec was still describing. `Input.md` § "Resolved in the 2026-08-29 redesign" items 1–2 had it right the whole time; this file was the one left behind.
>
> Why it mattered more than a wrong adjective: § Do / Don't carried the instruction *"Don't use `colors.border-strong` on a dialog field"*, which told anyone following the spec to write CSS that breaks the shared contract.
>
> Check rather than trusting either revision — by identifier, not by line number (`doc/Design/CLAUDE.md` § Neo trích dẫn):
>
> ```bash
> grep -n 'form-row input' src/FE/src/styles.scss
> grep -n 'border: 1px solid var(--border-strong)' src/FE/src/styles.scss
> ```
>
> PASS = every `.form-row` field selector appears only inside a shared list whose border is `var(--border-strong)`, and no rule anywhere gives a `.form-row` field `var(--line)`.

`.required` — the `*` marker inside a label, `color: colors.bad`. **Nay là class toàn cục** — `src/FE/src/styles.scss` § `.required` (đối chiếu 2026-09-08). ⚠️ Neo này trước viết `styles.scss` kèm số dòng `904`; số dòng vào `styles.scss` bị `doc/Design/CLAUDE.md` § Neo trích dẫn cấm tuyệt đối, và cổng không bắt được vì 904 nhỏ hơn độ dài file nên nó "nằm trong file" theo đúng nghĩa đen — sửa 2026-09-08. 🗄️ Bản 2026-08-22 khẳng định nó **không** toàn cục và được khai lặp ở hai stylesheet dialog; cả tuyên bố đó lẫn một trong hai file đã hết hiệu lực từ 2026-08-29 — sửa tại chỗ vì để nguyên là spec nói sai (§4). Bản chép còn sống đã bị gỡ, và chỗ gỡ có ghi lại lý do: `src/FE/src/app/platform/quan-tri-nguoi-dung/components/user-form-dialog/user-form-dialog.scss:1-6`. Bản chép kia nằm ở `criteria-form-dialog.scss` — file đó **đã xoá** 2026-08-29 cùng module `DtiWeekly`, nên neo `:1-3` cũ được gỡ khỏi đây thay vì giữ một trích dẫn không ai mở được (đã đối chiếu tại commit `98a5d96` ngày 2026-09-08: đúng, `.required { color: var(--bad) }` chiếm đúng 3 dòng đầu file). Kiểm: `grep -n '^\.required' src/FE/src/styles.scss`.

`.form-error` (global) — `colors.bad`, `--fs-sm`, margin `spacing.sp-2` 0. Rendered conditionally at **two** levels, both from this one class:
- **Per field** — inside the `.form-row`, directly under the control, carrying an `[id]` produced by `errorId(<FieldName>)` which that control's `aria-describedby` points at. The control simultaneously takes `.invalid` and `aria-invalid="true"`.
- **Form level** — one instance below the last row and above the action bar, for a server message that carries no field name.

🔄 **SỬA 2026-09-08.** The previous revision said *"Rendered conditionally, once per dialog … there is **no per-field error slot** anywhere in the app."* That was already false when written: the user dialog and `/doi-mat-khau` both render one slot per field. Three sibling specs (`AuthField.md` § Variants, `Screens/05-auth.md`, `UiInventory.md`) recorded the shipped behaviour correctly, so this file was the odd one out.

Locate the call sites rather than trusting a count written here (`.claude/CLAUDE.md` §6):

```bash
grep -rn 'class="form-error"' src/FE/src/app --include='*.html'
```

PASS = every hit that also carries `[id]="errorId(` is a per-field slot; a hit without `[id]` is the form-level one.

`.role-checkboxes` (component-scoped, user dialog only) — row flex, gap `spacing.sp-4`, `margin-top: spacing.sp-1`, `role="group"` + `aria-labelledby`; the group itself carries `aria-invalid` / `aria-describedby` (not the individual boxes — the "pick at least one role" error is about the whole group). Each entry is a `.role-checkbox` `<label>`: row flex, `align-items:center`, gap `spacing.sp-2`, `--fs-sm`, weight 600, `cursor:pointer`, wrapping a `<input type="checkbox" class="check">` and the role name as a text node.

A sibling `.role-preserved` paragraph appears only when the edited user holds roles outside the assignable set. It declares **one** property of its own — `margin-top: spacing.sp-2` (`user-form-dialog.scss:25-27`); its `--fs-xs` size and `colors.muted` ink come from the global `.muted` class it also carries (`styles.scss` § `.muted`), i.e. `typography.muted-caption`.

> 🔄 **SỬA 2026-09-08 — hai giá trị ở đoạn này từng sai.**
> - `gap` của `.role-checkbox` được tả là *"`6px` literal"* ở đây, ở § Tokens Used và ở § Normalize #5, trong khi `Tokens/spacing.md` § `sp-2` đã liệt chính nó là một call site của `--sp-2`. Source đứng về phía `Tokens/spacing.md`: `user-form-dialog.scss:17` là `gap: var(--sp-2);`. Món nợ kỹ thuật này **đã trả xong 2026-08-29** (`Tokens/spacing.md` § Normalize #3 ghi lại), spec thì vẫn ghi là còn nợ suốt 10 ngày. Một khoản nợ đã trả mà sổ vẫn ghi nợ đắt đúng bằng một khoản nợ chưa trả: người đọc đi sửa lại thứ đã đúng.
> - `.role-preserved` được tả là tự khai `--fs-sm` + `colors.muted`. Nó không khai gì trong hai thứ đó — cỡ chữ thật là `--fs-xs` (11px) vì đến từ `.muted`, không phải 12px. Chính stylesheet nói ra điều này ở `user-form-dialog.scss:23-24`.
>
> Kiểm cả hai: `grep -n 'gap\|margin-top' src/FE/src/app/platform/quan-tri-nguoi-dung/components/user-form-dialog/user-form-dialog.scss` và `grep -n '^\.muted' src/FE/src/styles.scss`.

`.form-grid` (global) — `display:grid`, `1fr 1fr`, gap `0 spacing.sp-5`. It exists purely to place two `.form-row`s side by side. 🗄️ Bản cũ ghi criteria dialog là người dùng duy nhất — dialog đó đã xoá 2026-08-29; call site hôm nay lấy bằng `grep -rn 'form-grid' src/FE/src/app`.

## Variants

| Variant | Classes | Key values | When to use |
| --- | --- | --- | --- |
| Text field row | `form-row` | `<label for>` + `<input class="input w-md">` — the shipped dialog puts the width variant on every field (see § Anatomy) | Tên đăng nhập, Email, Họ tên, Mật khẩu tạm, Mã |
| Textarea row | `form-row` | `min-height:64px`, `resize:vertical` | 🗄️ “Tên chỉ tiêu” thuộc criteria dialog, đã xoá 2026-08-29 |
| Select row | `form-row` | Same box as the input; native `<select>` | 🗄️ “Nhóm” thuộc criteria dialog, đã xoá 2026-08-29 |
| Number row | `form-row` | `type="number"` with `min`/`step` | 🗄️ “Điểm tối đa” thuộc criteria dialog, đã xoá 2026-08-29 cùng `.progressInput` của grid mà dòng này từng đem ra so |
| File row | `form-row` | `<input type="file" accept=".csv,.xlsx,.xls">` — the shared field rule applies, so the UA file button sits inside a bordered 100%-wide box | 🗄️ Import dialog, đã xoá 2026-08-29 — không còn call site |
| Group-caption row | `form-row` + `.form-row-label` | `<span>` caption + `aria-labelledby`, not `<label for>` | Vai trò (the checkbox group) |
| Paired rows | `.form-grid > .form-row × 2` | Two columns, `0 spacing.sp-5` gap | Hai ô ngắn đi cặp. 🗄️ Ví dụ cũ “Nhóm + Điểm tối đa” thuộc criteria dialog, đã xoá 2026-08-29 |
| Required marker | `.required` | `colors.bad` asterisk appended inside the label text | Mọi ô bắt buộc trong dialog nhập liệu |
| Per-field error | `.form-error` + `[id]="errorId(<Field>)"` | `colors.bad`, `--fs-sm`; paired with `.invalid` + `aria-invalid` + `aria-describedby` on the control | Under any field the server or local validation rejected |
| Form-level error | `.form-error` (no `[id]`) | `colors.bad`, `--fs-sm` | One per dialog, conditional — only for messages with no field attached |

## States
<!-- Exactly these five rows, in this order — treatments as rendered by the shipped CSS. -->

Primary interactive element: the `input` / `select` / `textarea` inside `.form-row`.

| State | Treatment |
| --- | --- |
| default | Border 1px `colors.border-strong` — the shared input contract, `styles.scss` § `.form-row input`, **not** a dialog-only border (corrected 2026-09-08; this row read `colors.line`). Radius `rounded.sm`, fill `colors.card`, padding `spacing.sp-2` `spacing.sp-3`, `--fs-sm`; label above in `--fs-sm`/700 `colors.text`. Width is `100%` **only when no `.w-*` variant is present** — the shipped dialog puts `.w-md` on all four fields, so what actually renders is 240px (see § Anatomy) |
| hover | **Not styled** — `styles.scss` authors no `:hover` rule for `.form-row input/select/textarea`, and the dialog's own stylesheet adds none: `user-form-dialog.scss:1-6` declares in its header that it holds only the role-checkbox group. (`confirm-dialog.scss` carries no rules at all — see `ConfirmDialog.md`) |
| focus-visible | **Styled** — `styles.scss` § `.form-row input:focus-visible` removes the default outline and draws the brand ring: `border-color: var(--brand)` plus a 3px `box-shadow` halo, shared with `.input-icon input:focus-visible`. <br><br>⚠️ **Sửa 2026-09-04.** Bản trước khẳng định ngược lại — *"Not styled — and this is the gap, not a design choice… dialog fields fall back to the browser's default focus ring"* — kèm chữ *"Verified by reading"*. Một lượt kiểm độc lập mở source ra và bác bỏ. Cổng không bắt được vì nó chỉ kiểm neo **phân giải được**, không đọc hiểu điều câu văn khẳng định. Ghi lại thay vì xoá lặng lẽ: chữ *"Verified"* gắn lên một khẳng định chưa ai kiểm là đúng khuôn §4 cấm. |
| active / selected | **Not applicable** for the text fields — no `:active` rule and no selected concept. For `.role-checkboxes` the selected state is the native checked box painted by the **global `.check` rule** — 16px square, `accent-color: colors.brand`, `cursor:pointer`, plus its own `:focus-visible` ring and `:disabled` treatment (`styles.scss` § `.check`, documented in `Input.md` § Variants → Checkbox). ⚠️ **Corrected 2026-09-08:** this cell read *"styled only by `accent-color` on the permission matrices, not here — the dialog's checkboxes are entirely UA-rendered"*. That was the **pre-2026-08-29** arrangement, and `Input.md` § Resolved item 5 had already recorded the merge: `user-form-dialog.html:131` carries `class="check"`, and `user-form-dialog.scss:1-6` says in as many words that the box is not re-declared locally because the global class covers it |
| disabled | **Styled, but never triggered.** ⚠️ Corrected 2026-09-08: this row said *"no `:disabled` rule is authored"*. One is — `styles.scss` § `.form-row input:disabled` fills `colors.surface-track`, inks `colors.muted` and sets `cursor:not-allowed`, shared with every other input tier. What remains true is that no shipped dialog reaches it: no `[disabled]` binding exists on any `.form-row` field, because the dialogs express "not allowed" by **omitting** rows instead — Tên đăng nhập and Mật khẩu tạm are rendered only in create mode (`@if (!editing())`), never shown greyed out. 🗄️ Control disabled duy nhất mà bản cũ nêu — nút submit của import dialog — đã xoá 2026-08-29 |

## Tokens Used
- `colors.card`, `colors.border-strong`, `colors.text`, `colors.bad`, `colors.muted` — plus `colors.brand` (focus ring) and `colors.surface-track` (disabled fill), both inherited from the shared input contract rather than declared here. `colors.line` was listed here until 2026-09-08 and is **not** used by any `.form-row` rule.
- `rounded.sm`
- `spacing.sp-1`, `spacing.sp-2`, `spacing.sp-3`, `spacing.sp-4`, `spacing.sp-5`, `spacing.form-grid` (the `1fr 1fr` template in `Tokens/spacing.md` § Grid templates)
- `typography.form-label` (12px/700, labels), `typography.table-cell` size (`--fs-sm`, fields and messages), `typography.muted-caption` (`--fs-xs`, the `.role-preserved` line via `.muted`)
- `dimension.checkbox` — the role boxes, inherited from the global `.check` rule rather than declared here
- Un-tokenised literal: `min-height:64px` on `textarea` — catalogued in `Tokens/spacing.md`. ⚠️ `gap:6px` in `.role-checkbox` was listed here until 2026-09-08 and is **not** a literal: the source uses `var(--sp-2)`, whose value happens to be 6px

## Reference markup

```html
<div class="form-row">
  <label for="ufEmail">Email <span class="required">*</span></label>
  <input
    id="ufEmail"
    class="input w-md"
    type="email"
    [class.invalid]="!!fieldErrors()['Email']"
    [attr.aria-invalid]="fieldErrors()['Email'] ? 'true' : null"
    [attr.aria-describedby]="fieldErrors()['Email'] ? errorId('Email') : null"
    placeholder="ten@congty.vn"
    [value]="emailField()"
    (input)="onEmailInput($event)"
  />
  @if (fieldErrors()['Email']; as message) {
    <div class="form-error" [id]="errorId('Email')">{{ message }}</div>
  }
</div>

<div class="form-grid">
  <div class="form-row">
    <label for="ufEmail">…</label>
    <input id="ufEmail" class="input w-md" type="email" … />
  </div>
  <div class="form-row">
    <label for="ufFullName">…</label>
    <input id="ufFullName" class="input w-md" … />
  </div>
</div>

<div class="form-row">
  <span id="ufRolesLabel" class="form-row-label">Vai trò <span class="required">*</span></span>
  <div class="role-checkboxes" role="group" aria-labelledby="ufRolesLabel">
    @for (role of assignableRoles; track role) {
      <label class="role-checkbox">
        <input type="checkbox" class="check" [checked]="isRoleSelected(role)" (change)="toggleRole(role)" />
        {{ role }}
      </label>
    }
  </div>
  @if (fieldErrors()['Roles']; as message) {
    <div class="form-error" [id]="errorId('Roles')">{{ message }}</div>
  }
  @if (preservedRoles().length > 0) {
    <p class="role-preserved muted">
      {{ 'quan-tri-nguoi-dung.dialog.preservedRoles' | translate: { roles: preservedRoles().join(', ') } }}
    </p>
  }
</div>

@if (generalError(); as message) {
  <div class="form-error">{{ message }}</div>
}
```

> 🔄 **SỬA 2026-09-08 — hai lỗi trong chính đoạn mẫu này.**
> 1. Ô tick thiếu `class="check"`. Đoạn mẫu là thứ người ta **chép**, nên một class thiếu ở đây không phải sai sót mô tả mà là một lỗi giao diện sắp được sinh ra: chép nguyên văn cho ra ô tick trần của trình duyệt, mất `accent-color`, mất vòng focus và mất treatment `:disabled` — đúng cái mà đợt hợp nhất 2026-08-29 vừa gỡ đi (`Input.md` § Resolved item 5). Cổng không đọc được markup mẫu, và `.claude/CLAUDE.md` §8 tự khai đây là lỗ mù loại 1.
> 2. Khối lỗi cấp form gọi `errorMessage()`, nhưng tên thật trong component là **`generalError()`** (`user-form-dialog.ts:226`) — `errorMessage()` là tên ở màn `/doi-mat-khau`, không phải ở dialog này.
> 3. Khối `.form-grid` ở giữa lấy từ criteria dialog — hộp thoại **đã xoá 2026-08-29**, và nó viết `<select>`/`<input type="number">` trần, không có `class="input"`. Thay bằng cặp `Email` + `Họ tên` của `user-form-dialog.html:76-113`, tức call site `.form-grid` **đang chạy**. Một đoạn mẫu lấy từ màn đã chết là dạng sai khó thấy nhất trong khu này: nó vẫn "hợp lệ" về cú pháp, chỉ là nó tả một sản phẩm không còn tồn tại.

## Vai trò được giữ nguyên — ✅ ĐÃ SHIP (chốt 2026-09-06, đối chiếu source 2026-09-08)

**Vấn đề nó giải:** ô tick chỉ hiện `ASSIGNABLE_ROLES` = `['Admin', 'User']`
(`src/FE/src/app/platform/quan-tri-nguoi-dung/models/quan-tri-nguoi-dung.model.ts:125`). Khi sửa một
tài khoản mang `SuperAdmin`, cả hai ô đều **không tích** và trước 2026-09-06 không có một chữ nào nói
vai trò kia vẫn còn.

**Đây không phải lỗ hổng bảo mật** — backend chặn việc đổi nó bằng
`USER.SUPERADMIN_ROLE_CHANGE_FORBIDDEN`. Nó là **lỗ hổng niềm tin**: người sửa hiểu là tài khoản
không có vai trò nào, tích thêm `Admin`, bấm Lưu, rồi hoặc gặp 403 không giải thích được, hoặc
tưởng mình vừa cấp thêm quyền. Giao diện nói một đằng, hệ thống làm một nẻo.

**Hình dạng đã ship:** dưới nhóm `.role-checkboxes`, khi tài khoản đang sửa có vai trò **không nằm
trong** `ASSIGNABLE_ROLES`, một `<p class="role-preserved muted">` hiện tên vai trò đó và rằng nó được
giữ nguyên.

| Ràng buộc đã chốt | Vì sao | Ship ở đâu |
|---|---|---|
| Chỉ hiện khi **có** vai trò ngoài danh sách | Tài khoản thường không có dòng thừa | `@if (preservedRoles().length > 0)` — `user-form-dialog.html:143` |
| Lấy từ `user.Roles` trừ đi `ASSIGNABLE_ROLES` — **không** khai cứng chuỗi `'SuperAdmin'` | Thêm vai trò hệ thống thứ hai sau này thì dòng này tự đúng, không phải nhớ sửa | `preservedRoles` — `user-form-dialog.ts:167-170` |
| Câu đi qua khoá dịch, tên vai trò là tham số | Mọi chuỗi người dùng đọc đều phải dịch được (§G12) | `quan-tri-nguoi-dung.dialog.preservedRoles` — `src/FE/public/i18n/vi.json:178`, `src/FE/public/i18n/en.json:178` |
| Chỉ đọc — **không** thành ô tick | Cho tick nghĩa là cho gỡ, mà BE cấm gỡ. Một điều khiển luôn bị từ chối còn tệ hơn không có điều khiển | `<p>`, không phải `<input>` — `user-form-dialog.html:144`; lý do chép lại trong comment `user-form-dialog.html:139-142` |

Không chạm BE. Ca hiển thị được khoá bằng test: `user-form-dialog.spec.ts:95` (`🛑 HIỂN THỊ: có role
ngoài ASSIGNABLE_ROLES → hiện dòng "được giữ nguyên", kèm đúng tên role`) và
`user-form-dialog.spec.ts:42` cho nhánh gửi lại role.

Nhóm ô tick vẫn chỉ render đúng hai ô. `SuperAdmin` cố ý vắng mặt trong picker và được gửi lại nguyên
vẹn khi lưu — đó chính là điều `.role-preserved` nói cho người dùng biết.

> ### 🔄 SỬA 2026-09-08 — file này từng vừa tả nó là đã ship, vừa tuyên bố nó không tồn tại
>
> Trước hôm nay § Anatomy và câu chốt cuối mục này mô tả `.role-preserved` như một thứ đang chạy,
> trong khi **cùng file** có một khối `🔄 SỬA 2026-09-06` khẳng định *"**Không có gì như thế trong
> code** — `grep -rn "role-preserved" src/FE/src/app` trả rỗng"* và một tiêu đề `📐 PHẢI XÂY`. Hai
> khẳng định loại trừ nhau, cách nhau 80 dòng.
>
> Bên đúng là bên nói **đã ship**, và bằng chứng thì có ở năm nơi: `user-form-dialog.html:144`,
> `user-form-dialog.scss:25-27`, `user-form-dialog.ts:167`, `user-form-dialog.spec.ts:95`,
> `src/FE/public/i18n/vi.json:178`. Lệnh `grep` mà khối cũ dùng làm bằng chứng **hôm nay trả về kết quả** — nó đúng vào
> ngày 2026-09-06, rồi code về, và không ai quay lại chạy lại nó.
>
> Bài học giữ lại vì nó là bài học chung, không riêng gì mục này: **một lệnh `grep` chép vào tài liệu
> là ảnh chụp, không phải phép kiểm** — nó chỉ có giá trị nếu người đọc chạy lại. Khối cũ dán kết quả
> *"trả rỗng"* thay vì dán lệnh và tiêu chí PASS, nên nó trở thành một khẳng định tự tin về hiện trạng
> mà `check-docs.sh` không bao giờ đọc tới (`.claude/CLAUDE.md` §8, lỗ mù loại 1).
>
> Chạy lại thay vì tin: `grep -rn 'role-preserved\|preservedRoles' src/FE/src/app` — PASS = có hit ở
> cả `.html`, `.scss`, `.ts`.

Sources: `src/FE/src/styles.scss` § `.form-row`, § `.form-row label`, § `.form-row input`, § `.form-error`, § `.form-grid`, § `.required`, § `.check`, § `.muted`, `src/FE/src/app/platform/quan-tri-nguoi-dung/components/user-form-dialog/user-form-dialog.html:31-154`, `src/FE/src/app/platform/quan-tri-nguoi-dung/components/user-form-dialog/user-form-dialog.scss:1-27`, `src/FE/src/app/platform/quan-tri-nguoi-dung/components/user-form-dialog/user-form-dialog.ts:71-83` (gộp `fieldErrors` theo ô), `:167-170` (`preservedRoles`), `:207` (`fieldErrors`), `:226-227` (`generalError`), `:289-291` (`errorId`), `src/FE/src/app/platform/quan-tri-nguoi-dung/models/quan-tri-nguoi-dung.model.ts:125` (`ASSIGNABLE_ROLES`), `src/FE/public/i18n/vi.json:178`.

> 🗄️ **Nguồn đã chết, tách khỏi dòng `Sources:` ngày 2026-09-08.** Ba neo dưới đây từng nằm ngay trên dòng `Sources:` ở trên: `criteria-form-dialog.html:1-42`, `criteria-form-dialog.scss:1-3` và `csv-import-dialog.html:1-22`, tất cả dưới `src/FE/src/app/modules/danh-muc-dti/`, **đã xoá** 2026-08-29 cùng module `DtiWeekly`. Tra ở commit `98a5d96`.
>
> **Cả ba đều đúng vào ngày viết** — đối chiếu 2026-09-08 tại commit đó: `criteria-form-dialog.html` dài đúng 42 dòng, `csv-import-dialog.html` đúng 22 dòng, và `criteria-form-dialog.scss` dòng `1-3` đúng là ba dòng khai `.required { color: var(--bad) }`. Chúng là neo **chết**, không phải neo **bịa** — phân biệt này quan trọng vì cách sửa khác hẳn nhau.
>
> Vì sao phải rời khỏi dòng `Sources:`: một mệnh đề *"… đã xoá 2026-08-29 …"* gắn ở cuối dòng khiến cổng miễn trừ **toàn bộ** neo trên dòng đó — kể cả các neo sống. Cổng đã bịt lỗ này (miễn trừ lịch sử không còn áp cho dòng `Sources:`) đúng vì trò đó. Dòng `Sources:` là **danh sách bằng chứng**, không phải chỗ kể lịch sử: mọi thứ trên đó phải mở được hôm nay.

## Do / Don't

- ✅ Keep one `.form-row` per field, label first, and pair `<label for>` with the control's `id` — every text field in the shipped dialog does.
- ✅ Use `.form-row-label` (not `<label>`) whenever the caption describes a *group*, and back it with `role="group"` + `aria-labelledby`.
- ✅ Render errors at **both** levels, the way the shipped dialog does: a `.form-error` carrying `[id]="errorId(<Field>)"` inside each failing `.form-row`, and **one** un-`id`'d `.form-error` between the last row and `.dialog-actions` for messages with no field name. The form-level line is suppressed while any field error exists (`user-form-dialog.ts:226-227`), so the same failure is never stated twice — once vaguely above once precisely. ⚠️ **Corrected 2026-09-08.** This bullet read *"Put the error line once … and drive it from a single `errorMessage()` that merges local and server messages"*, contradicting § Anatomy **32 lines above it in the same file** — and naming a member (`errorMessage()`) that belongs to `/doi-mat-khau`, not to this dialog.
- ✅ Omit a field the user may not edit rather than disabling it — that is the shipped pattern for Tên đăng nhập and Mật khẩu tạm in edit mode.
- ✅ Keep the roles group data-driven from `ASSIGNABLE_ROLES` and keep re-sending any role outside it; the payload is whole-set, so a dropped role is a silent privilege change.
- ❌ Don't re-declare the field box in a dialog stylesheet — border, radius, padding, focus, disabled and invalid all come from the one shared list at `styles.scss` § `.form-row input`. A local copy loses the focus ring and the disabled fill in a single move. ⚠️ **Corrected 2026-09-08.** This bullet read *"Don't use `colors.border-strong` on a dialog field — that would merge this tier into the filter tier without a decision"*: the exact opposite of what ships, so following the spec produced CSS that broke the shared contract.
- ❌ Don't render a per-field error without also setting `.invalid` + `aria-invalid` + `aria-describedby` on the control — the shipped dialogs always set all four together, and a red message with no red border and no screen-reader link is half the pattern.

## Normalize on redesign
1. ~~**Dialog fields have no authored focus style.**~~ — **Closed; recorded 2026-09-08.** `styles.scss` § `.form-row input:focus-visible` drops the UA outline and draws `border-color: var(--brand)` plus a 3px brand halo, in the same block that serves `.input`, the toolbar fields and `.input-icon input`. Kept rather than deleted, because of how long it survived: the § States focus row was corrected on **2026-09-04**, and this bullet went on calling the same thing the spec's *"highest-value fix"* for four more days. A spec can contradict itself and no gate will notice — `check-docs.sh` resolves anchors, it does not read claims.
2. ~~**`.required` is duplicated verbatim in two component stylesheets** (`user-form-dialog.scss:1-3`, `criteria-form-dialog.scss:1-3`)~~ — **Đã xử lý 2026-08-29:** rule đã nằm toàn cục cạnh `.form-row` trong `styles.scss` § `.required`, và một trong hai bản chép mất theo module `DtiWeekly`.
3. ~~**A third input treatment exists at all.**~~ — **Closed 2026-08-29 in code; recorded here 2026-09-08.** Filter, dialog and auth fields were three answers to one question; they are now one selector list with one border, one focus ring, one disabled fill and one invalid border. `Input.md` § "Resolved in the 2026-08-29 redesign" items 1–2 is the record of the merge. ⚠️ This bullet also pointed at **`COMPONENTS.md` § Known inconsistencies**, a section that does not exist in that file — list its real sections with `grep -n '^## ' doc/Design/Frontend/PlatformManager/COMPONENTS.md`. A dead pointer on top of a closed item is how a reader concludes the inconsistency is still open and goes looking for a decision nobody has to make. **Resolved 2026-09-08:** there is no central register and there will not be one; a library-level problem is recorded in the owning component's own `## Normalize on redesign` — see `COMPONENTS.md` § *There is no central "Known inconsistencies" list here, on purpose*, where this bullet is the worked example.
4. ~~**Only form-level errors are possible.**~~ — **Resolved; recorded 2026-09-08.** The BE envelope's per-field errors now land in a slot per field; the form-level line is left for messages with no field name. The claim that no such slot exists was wrong on the day it was written — see the SỬA note under § Anatomy. ⚠️ The envelope key is **`fieldErrors`**, not `Fields`: this bullet named the legacy `Fields` map, which the dialog stopped reading on 2026-09-06 precisely because the BE leaves it empty for business errors and fills `fieldErrors` instead — the source records the swap in its own words at `user-form-dialog.ts:137-138`. The same stale premise was cleared out of `Screens/05-auth.md` the same day; naming the wrong key is not cosmetic, it is the difference between a spec that describes a working slot and one that describes a slot that never fills.
5. ~~**`.role-checkbox` uses a `6px` literal gap** where `spacing.sp-2` is 6px~~ — **Đã xử lý trong source 2026-08-29; ghi nhận ở đây 2026-09-08.** `user-form-dialog.scss:17` là `gap: var(--sp-2)`, và `Tokens/spacing.md` § `sp-2` đã liệt `.role-checkbox` là call site của token này từ cùng ngày đó. Giữ lại chứ không xoá, vì cái đáng nhớ là **độ trễ**: món nợ được trả trong code ngày 2026-08-29, sổ ghi nợ ở `Tokens/spacing.md` được cập nhật cùng ngày, còn spec này vẫn ghi "còn nợ" ở **ba** chỗ (§ Anatomy, § Tokens Used, và dòng này) thêm 10 ngày nữa. Không cổng nào so được hai sổ với nhau — chỉ có người đọc cả hai mới thấy.
   Phần **vẫn còn mở**: `min-height:64px` trên `textarea` là hoàn toàn ngoài thang, và `Input.md` § Normalize #3 ghi thêm rằng `textarea` chưa có call site nào đang chạy, nên giá trị này chưa từng được kiểm bằng mắt trên app thật.
6. ~~**`.form-grid` is a two-column grid with no responsive collapse**~~ — **Đã xử lý 2026-08-29:** `styles.scss` § `.form-grid` nay kèm một `@media` thu gọn về một cột ở màn hẹp. Kiểm: `grep -n -A1 '^\.form-grid' src/FE/src/styles.scss`.

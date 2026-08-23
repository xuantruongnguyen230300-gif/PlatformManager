---
project: "PlatformManager"
status: "draft"
updated: "2026-08-22"
component: "Dialog"
sources: ["src/FE/src/styles.scss", "src/FE/src/app/modules/dashboard/components/report-dialog/report-dialog.html", "src/FE/src/app/modules/danh-muc-dti/components/confirm-dialog/confirm-dialog.html", "src/FE/src/app/modules/danh-muc-dti/components/criteria-form-dialog/criteria-form-dialog.html", "src/FE/src/app/platform/quan-tri-nguoi-dung/components/user-form-dialog/user-form-dialog.html"]
---

# Dialog
**Description:** Native `<dialog>` modal (`styles.scss:466-484`). **Six instances ship**, in three width variants, each wrapped in its own Angular component that opens via `showModal()` and re-emits the native `(close)` event. PrimeNG's `p-dialog` is **not** used anywhere — every modal in the app is the browser-native element.

## Anatomy
`<dialog>` (borderless, `rounded.dialog`, `spacing.sp-5` padding, `box-shadow: 0 24px 70px rgba(0,0,0,.25)`, browser-centred) → `.title` row (`styles.scss:274-285`: `space-between` flex, `<h2>` at `typography.h2-title`, plus an optional `Đóng` `Button` on the right) → body (form rows, a report block, or a message paragraph) → `.dialog-actions` footer (`justify-content:flex-end`, `gap:8px`, `margin-top:12px`) holding one or two `Button`s.

`.dialog-actions` is **not global** — it is re-declared in **all six** component stylesheets, and the six copies do not agree:

| Declared in | `gap` | `margin-top` |
| --- | --- | --- |
| `report-dialog.scss:1-6` | 8px | 12px |
| `csv-import-dialog.scss:1-6` | 8px | 12px |
| `import-result-dialog.scss:19-23` | **absent** | 12px |
| `criteria-form-dialog.scss:5-10` | 8px | **8px** |
| `user-form-dialog.scss:26-31` | 8px | **8px** |
| `confirm-dialog.scss:6-11` | 8px | **16px** |

## Variants

| Variant | Classes | Key values | When to use |
| --- | --- | --- | --- |
| Wide (default) | `dialog` (no modifier) | `width: min(700px, 92vw)` (`styles.scss:470`) | The dashboard quick report only (`report-dialog.html:1`) — the widest content, a generated HTML report |
| Form | `dialog.form-dialog` | `width: min(560px, 92vw)` (`styles.scss:477-479`) | The four data-entry / result modals: criteria create-edit, user create-edit, CSV import, import result |
| Confirm | `dialog.confirm-dialog` | `width: min(420px, 92vw)` (`styles.scss:481-483`) | Destructive confirmation only (`confirm-dialog.html:1`) |
| Backdrop | `dialog::backdrop` | bg `colors.overlay-backdrop` (`rgba(20,28,40,.45)`, `styles.scss:473-475`) | Automatic on `showModal()`; never rendered by `show()` |

### The six shipped instances

| Component | Variant | Title | Footer actions |
| --- | --- | --- | --- |
| `report-dialog` | Wide | bound `title()` | `Sao chép` (default) · `In` (primary) — plus `Đóng` in the title row |
| `criteria-form-dialog` | Form | bound `title()` | `Huỷ` (default) · `Lưu chỉ tiêu` (primary) — plus `Đóng` in the title row |
| `user-form-dialog` | Form | bound `title()` | `Huỷ` (default) · `Lưu` (primary) — plus `Đóng` in the title row |
| `import-dialog` | Form | `Import CSV/Excel` | `Huỷ` (default) · `Nhập dữ liệu` (primary, `[disabled]`) — plus `Đóng` in the title row |
| `import-result-dialog` | Form | `Kết quả Import` | `Đóng` (primary) — plus a second `Đóng` (default) in the title row |
| `confirm-dialog` | Confirm | bound `title()`, passed `"Xác nhận"` | `Huỷ` (default) · bound `confirmLabel()` (**danger**) |

Note the asymmetry: `confirm-dialog` is the only one **without** a title-row `Đóng` button, and `import-result-dialog` is the only one offering `Đóng` twice.

## States
<!-- Exactly these five rows, in this order — treatments as rendered by the shipped CSS. -->

| State | Treatment |
| --- | --- |
| default (closed) | Not rendered — a native `<dialog>` without the `open` attribute is `display:none`. Two dialogs are additionally `@defer`-ed so their component is not even instantiated until requested (`danh-muc-dti.page.html:76-87`) |
| default (open) | Browser-centred, `rounded.dialog`, `spacing.sp-5` padding, borderless, `box-shadow: 0 24px 70px rgba(0,0,0,.25)`, width per variant; `::backdrop` fills the viewport with `colors.overlay-backdrop` |
| hover | **Not styled** — no `dialog:hover` rule; hover belongs to the `Button`s inside (see `Button.md`) |
| focus | **Browser default focus trap.** `showModal()` makes the dialog the top layer and confines Tab to it; Escape fires `(close)`, which every component re-emits as a `closed` output. No custom `:focus` CSS is authored on the dialog. `appAutofocus` moves initial focus to the first field in the two form dialogs (`criteria-form-dialog.html:9`, `user-form-dialog.html:12`) |
| active | **N/A for the dialog element** — it is not a control and has no `:active` rule. Its footer `Button`s carry the shared `.btn:active` press offset (`styles.scss:187-189`) |
| disabled | **N/A for the dialog element.** The state lives on its buttons — notably `csv-import-dialog.html:18`, whose primary action is `[disabled]` until a file is chosen, and `confirm-dialog`'s actions, which are never disabled |

## Tokens Used
- `colors.card` (the `<dialog>` background comes from the UA default white, matching `colors.card`), `colors.overlay-backdrop`, `colors.text`
- `rounded.dialog`
- `spacing.sp-5` (dialog padding)
- `typography.h2-title` (title row)

The `0 24px 70px rgba(0,0,0,.25)` elevation, the three `min(…, 92vw)` widths, and the `8px`/`12px`/`16px` footer values are literals — there is no elevation scale (`Tokens/spacing.md`).

## Reference markup

```html
<!-- Confirm variant — the only danger footer in the app -->
<dialog #dialogEl class="confirm-dialog" (close)="onNativeClose()">
  <div class="title"><h2>{{ title() }}</h2></div>
  <p class="confirm-message">{{ message() }}</p>
  <div class="dialog-actions">
    <button type="button" class="btn" (click)="dialogEl.close()">Huỷ</button>
    <button type="button" class="btn danger" (click)="confirmed.emit(); dialogEl.close()">{{ confirmLabel() }}</button>
  </div>
</dialog>

<!-- Form variant -->
<dialog #dialogEl class="form-dialog" (close)="onNativeClose()">
  <div class="title">
    <h2>{{ title() }}</h2>
    <button type="button" class="btn" (click)="dialogEl.close()">Đóng</button>
  </div>
  <div class="form-row">
    <label for="cfCode">Mã <span class="required">*</span></label>
    <input id="cfCode" #codeInput appAutofocus maxlength="20" placeholder="vd 1.1" [value]="editing()?.Code ?? ''" />
  </div>
  @if (errorMessage()) { <div class="form-error">{{ errorMessage() }}</div> }
  <div class="dialog-actions">
    <button type="button" class="btn" (click)="dialogEl.close()">Huỷ</button>
    <button type="button" class="btn primary" (click)="onSubmit(…)">Lưu chỉ tiêu</button>
  </div>
</dialog>
```

Sources: `src/FE/src/styles.scss:466-484` (element + three width variants + backdrop), `:270-281` (`.title`), `:449-453` (`.form-error`), `src/FE/src/app/modules/dashboard/components/report-dialog/report-dialog.html:1-11` + `report-dialog.scss:1-6`, `src/FE/src/app/modules/danh-muc-dti/components/confirm-dialog/confirm-dialog.html:1-10` + `confirm-dialog.scss:1-11`, `src/FE/src/app/modules/danh-muc-dti/components/criteria-form-dialog/criteria-form-dialog.html:1-42`, `src/FE/src/app/modules/danh-muc-dti/components/csv-import-dialog/csv-import-dialog.html:1-22`, `src/FE/src/app/modules/danh-muc-dti/components/import-result-dialog/import-result-dialog.html:1-35`, `src/FE/src/app/platform/quan-tri-nguoi-dung/components/user-form-dialog/user-form-dialog.html:1-71`, `src/FE/src/app/modules/danh-muc-dti/pages/danh-muc-dti/danh-muc-dti.page.html:76-87` (`@defer` gating)

## Do / Don't

- ✅ Open via native `showModal()` — it supplies the focus trap, Escape handling and `::backdrop` for free. Don't reimplement as a positioned `<div>`.
- ✅ Match the width variant to content weight: `confirm-dialog` for a one-sentence question, `form-dialog` for data entry, the default for the wide report.
- ✅ Put the destructive action in the footer as `btn danger` and the escape hatch (`Huỷ`) to its left — the shipped order.
- ✅ Surface server and validation errors in the single `.form-error` block above the footer (`styles.scss:453-457`), which is what all three form dialogs do.
- ✅ Wrap the two heaviest dialogs in `@defer` as the catalogue page does, so their code loads on demand.
- ❌ Don't stack or nest dialogs — the app never has two open at once; `import-result-dialog` opens only after `import-dialog` has closed.
- ❌ Don't add a confirm-before-close step to a read-only dialog; `Đóng` calls `close()` directly.
- ❌ Don't reach for PrimeNG's `p-dialog` — no instance uses it, and its chrome would not match these three widths.

## Normalize on redesign
1. `.dialog-actions` is duplicated in **six** component stylesheets rather than declared once globally, and the copies have already drifted into three different `margin-top` values (8/12/16px) with one missing `gap` entirely — so the footer sits at a different height in each dialog. Promote it to `styles.scss` and pick one spacing step.
2. `<dialog>` has no explicit `background` — it relies on the UA default white happening to equal `colors.card`. Set it from the token.
3. Close affordances are inconsistent: four dialogs have a title-row `Đóng`, `confirm-dialog` has none, `import-result-dialog` has two. Decide one pattern.
4. The `Đóng` control is a text `Button`, not an icon-only close — while `Toast` uses `pi pi-times` for the same job. Converge.
5. The elevation `0 24px 70px rgba(0,0,0,.25)` is a fourth uncontrolled shadow value alongside `--shadow` and the two `.btn` hover shadows. There is no elevation scale.
6. `report-dialog` renders `[innerHTML]="safeContent()"` — the only innerHTML binding in the app. Its internal `.report` markup is generated in TypeScript and therefore has no component spec; it cannot be reproduced from `Components/`.

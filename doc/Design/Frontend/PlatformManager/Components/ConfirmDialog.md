---
kind: luat
scope: du-an
verified: 2026-09-06
project: "PlatformManager"
status: "draft"
updated: "2026-09-06"
component: "ConfirmDialog"
sources:
  - "src/FE/src/styles.scss"
  - "src/FE/src/app/shared/components/confirm-dialog/confirm-dialog.ts"
  - "src/FE/src/app/shared/components/confirm-dialog/confirm-dialog.html"
  - "src/FE/src/app/shared/components/confirm-dialog/confirm-dialog.scss"
  - "src/FE/src/app/shared/components/confirm-dialog/confirm-dialog.spec.ts"
  - "src/FE/src/app/platform/quan-tri-nguoi-dung/pages/quan-tri-nguoi-dung/quan-tri-nguoi-dung.page.html"
  - "src/FE/src/app/platform/phan-quyen/pages/phan-quyen/phan-quyen.page.html"
---

# ConfirmDialog
**Description:** The ask-before-you-act modal (`<app-confirm-dialog>`, `dialog.confirm-dialog`) — a narrow native `<dialog>` opening on a coloured round icon, a bold question, an optional explanatory line, and **exactly two buttons**. It is the app's stop-and-think surface: destructive deletes, lock/unlock, unsaved-changes prompts. `Dialog.md` owns the `<dialog>` primitive and its width variants; this spec owns the confirm anatomy and the component that drives it.

## Anatomy

```
<dialog class="confirm-dialog">      ← min(420px, 92vw), radius.dialog, shadow.dialog, padding sp-5
  .dialog-head                       ← flex, gap sp-4, align-items:flex-start, margin-bottom sp-4
    .dialog-icon.<severity>          ← 40px pill, one of 4 colour pairs, glyph 17px
    <div>
      .dialog-title                  ← typography.dialog-title
      .dialog-desc                   ← optional; typography.dialog-desc, colors.muted
    .icon-btn.dialog-close           ← margin-left:auto
  <ng-content />                     ← optional extra body
  .dialog-actions                    ← flex, justify-content:flex-end, gap sp-3, margin-top sp-5
    .btn                             ← cancelLabel → shared.action.cancel   (focused on open)
    .btn.primary | .btn.danger       ← confirmLabel → shared.action.confirm
```

**The severity icon carries the meaning before the words are read** — that is stated as the reason it exists (the comment above `styles.scss` § `.dialog-head`). Four pairs ship (`styles.scss` § `.dialog-icon.ask`, `.dialog-icon.ok`, `.dialog-icon.warn`, `.dialog-icon.bad`), each reusing an existing semantic pair rather than inventing a colour:

| Severity | Fill / glyph | Default icon |
| --- | --- | --- |
| `ask` (default) | `colors.tonal-bg` / `colors.tonal-ink` | `pi-question-circle` |
| `ok` | `colors.good-bg` / `colors.good` | `pi-check-circle` |
| `warn` | `colors.warn-bg` / `colors.warn` | `pi-exclamation-triangle` |
| `bad` | `colors.bad-bg` / `colors.bad` | `pi-trash` |

The icon map lives in `confirm-dialog.ts:22-27`; a page may override the glyph alone via the `icon` input while keeping the severity's colour — the case it was written for is a `bad`-severity action that is not a deletion.

**Exactly two buttons — this is a settled decision, not an omission.** `confirm-dialog.ts:42-43` records it: the three-button variant was considered and dropped by the user. Both labels are inputs, and both default to the **empty string** rather than to a Vietnamese sentence (`confirm-dialog.ts:62-70`) — same contract as `Toolbar.md`: the parent passes an already-translated sentence, so the input must not also carry a key. Empty ⇒ the template falls back to `shared.action.confirm` / `shared.action.cancel` through `| translate` at the point of display, so the default labels follow the language switcher without this dumb component injecting anything. The confirm button swaps `.primary` for `.danger` when `confirmDanger` is set. The global `.dialog-actions .push` rule (`styles.scss` § `.dialog-actions .push`) does support a left-pushed retreat button for a three-choice footer, but **this component never emits it** — that rule belongs to `Dialog.md`.

**Why native `<dialog>`, not a hand-built overlay and not PrimeNG's `p-dialog`** (`confirm-dialog.ts:37-40`, and the scope decision in `doc/huong_dan/wiki-core/fe/05-component-library.md`): `showModal()` gives the focus trap, the backdrop layer, Escape-to-close and focus restoration for free. Re-implementing those four is large and easy to get subtly wrong — but the trade is that the markup must stay exactly as it is. Wrapping the `<dialog>` in a custom overlay silently destroys all four behaviours and produces no compile error.

**Closing is the fragile part, and the component solves it explicitly.** `<dialog>` fires **one** `close` event for every way out — confirm, cancel, ×, Escape. The component records the intended outcome before closing and reads it back in the `(close)` handler (`confirm-dialog.ts:100`, `:128-147`):

| Exit | Recorded outcome | Emits |
| --- | --- | --- |
| **Đồng ý** | `'confirm'` | `confirmed` |
| **Huỷ** or **×** | `'cancel'` | `cancelled` |
| **Escape** (browser closes it directly, through no button) | `null` | `cancelled` |
| `close()` called by the parent | `'silent'` | nothing — emitting `cancelled` here would loop against the parent's own `(cancelled)` handler |

**Focus lands on Huỷ, not on ×.** `showModal()` focuses the first focusable element, which is the close button; the component overrides that and pulls focus to the cancel button (`confirm-dialog.ts:113`), so a reflexive Enter on a question that can destroy data takes the safe exit. It is done in code rather than with an `autofocus` attribute, which the a11y lint rule blocks. `confirm-dialog.spec.ts:75-79` pins this behaviour.

**Accessibility wiring:** `aria-labelledby` always points at `.dialog-title`; `aria-describedby` is bound to `.dialog-desc` only when a description exists and is `null` otherwise, so the attribute never dangles (`confirm-dialog.html:4-5`, `confirm-dialog.ts:90-91`). Ids are per-instance from a module counter, so two dialogs on one page cannot collide. The severity badge is `aria-hidden="true"` — its meaning is already in the title.

## Variants

| Variant | Classes / inputs | Key values | When to use |
| --- | --- | --- | --- |
| Ask (default) | `severity="ask"` | Tonal blue badge, `pi-question-circle`, confirm button `.btn.primary` | A reversible decision — "save these changes?", "leave without saving?" |
| Destructive | `severity="bad"` + `[confirmDanger]="true"` | `colors.bad-bg` badge, `pi-trash`, confirm button `.btn.danger` | Delete, and any action that cannot be undone. The badge and the button must agree — a red badge over a blue confirm button sends two signals |
| Warning | `severity="warn"` | `colors.warn-bg` badge, `pi-exclamation-triangle` | Something will go wrong but nothing is lost — an action that will be rejected, a limit about to be crossed |
| Positive | `severity="ok"` | `colors.good-bg` badge, `pi-check-circle` | Confirming a benign completion |
| With extra body | any severity + projected content | Content lands between `.dialog-head` and `.dialog-actions` | The question needs a list, a table row preview, or a second input |

`severity` and `confirmDanger` are **independent inputs**. Colour of the badge and colour of the confirm button are set separately, which is flexible and is also the easiest way to ship an inconsistent dialog — see Normalize #2.

## States
<!-- Exactly these five rows, in this order — treatments as rendered by the shipped CSS. -->

| State | Treatment |
| --- | --- |
| default | **Closed.** The `<dialog>` is in the DOM but `open` is false, so the UA hides it entirely and it is out of the accessibility tree and out of the tab order. `open()` calls `showModal()`, which paints the `colors.overlay-backdrop` layer over the page and traps focus; the panel is `dimension.dialog-width-confirm` (`min(420px, 92vw)`), `radius.dialog`, `shadow.dialog`, `padding: spacing.sp-5` |
| hover | **The dialog surface has no hover rule** — it is a container. Its three controls hover on their own contracts: `Button.md` for Huỷ and Đồng ý (`colors.btn-hover-bg`, or `colors.danger-hover-bg` on `.danger`), `IconButton.md` for the × (`colors.surface-2` fill). The backdrop is inert |
| focus-visible | **Focus is trapped inside the dialog** by `showModal()` and cannot reach the page behind it. On open it is placed on **Huỷ** deliberately. Each control draws its own ring: `2px solid colors.brand` at `outline-offset: 2px` for the two `.btn`s, `1px` offset for the × |
| active / selected | No `:active` on the container. `.btn` presses with `transform: translateY(1px)`; the × has no press treatment (`IconButton.md` Normalize #1). The **open** dialog is the only "selected" sense there is, and it is exclusive — one modal at a time, by definition of `showModal()`. `open()` called while already open is a no-op guarded in code, because native `showModal()` throws in that case (`confirm-dialog.ts:106`, pinned by `confirm-dialog.spec.ts:69-73`) |
| disabled | **Only the confirm button can be disabled**, through `[confirmDisabled]`, and it blocks the action for real rather than only dimming: the click emits nothing and the dialog stays open (`confirm-dialog.spec.ts:183-193`). Its purpose is the double-submit window while the parent is saving. **Huỷ, × and Escape are never disabled** — the escape route stays open even mid-save |

## Tokens Used
- `colors.overlay-backdrop` (the `::backdrop` layer, shared with the sidebar drawer)
- `colors.tonal-bg` / `colors.tonal-ink` · `colors.good-bg` / `colors.good` · `colors.warn-bg` / `colors.warn` · `colors.bad-bg` / `colors.bad` — the four severity pairs
- `colors.text` (title), `colors.muted` (description)
- `radius.dialog` (panel), `radius.pill` (severity badge)
- `spacing.sp-2` (title-to-description gap), `spacing.sp-3` (footer button gap), `spacing.sp-4` (badge-to-text gap, head bottom margin), `spacing.sp-5` (panel padding, footer top margin)
- `dimension.dialog-width-confirm` (420px), `dimension.dialog-icon` (40px)
- `shadow.dialog`
- `typography.dialog-title`, `typography.dialog-desc`, `typography.button-label`
- Un-tokenised literals: badge glyph `17px`, the `92vw` cap, `line-height: 1.5` on the description

## Reference markup

```html
<!-- shipped call site: the lock prompt on /quan-tri/nguoi-dung -->
<app-confirm-dialog
  severity="warn"
  [title]="'quan-tri-nguoi-dung.dialog.lockTitle' | translate"
  [description]="lockConfirmDescription()"
  [confirmLabel]="'quan-tri-nguoi-dung.dialog.lockConfirm' | translate"
  [confirmDanger]="true"
  (confirmed)="onLockConfirmed()"
  (cancelled)="onLockCancelled()"
/>
```

Every string reaching the dialog is already translated by the caller; the component itself only supplies the two **fallback** labels, and it does that from keys, not from literals. `lockConfirmDescription()` picks between `quan-tri-nguoi-dung.dialog.lockDescription` and `…lockDescriptionNamed` depending on whether the row has a name to interpolate.

```scss
.dialog-head {
  display: flex;
  gap: var(--sp-4);
  align-items: flex-start;
  margin-bottom: var(--sp-4);
}
.dialog-icon {
  width: 40px;
  height: 40px;
  border-radius: var(--radius-pill);
  flex: none;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 17px;
}
.dialog-icon.ask  { background: var(--tonal-bg); color: var(--tonal-ink); }
.dialog-icon.bad  { background: var(--bad-bg);   color: var(--bad); }
.dialog-title { font-size: var(--fs-lg); font-weight: 800; color: var(--text); }
.dialog-desc  { color: var(--muted); margin-top: var(--sp-2); line-height: 1.5; font-size: var(--fs-sm); }
.dialog-close { margin-left: auto; flex: none; }
dialog.confirm-dialog { width: min(420px, 92vw); }
```

Sources: `src/FE/src/styles.scss` § `.dialog-actions`, `.dialog-head`, `.dialog-icon`, `.dialog-title`, `.dialog-desc`, `.dialog-close`, plus § `dialog` and § `dialog.confirm-dialog` (the primitive and the confirm width), `src/FE/src/app/shared/components/confirm-dialog/confirm-dialog.ts:56-148`, `src/FE/src/app/shared/components/confirm-dialog/confirm-dialog.html:1-46`, `src/FE/src/app/shared/components/confirm-dialog/confirm-dialog.scss:1-7`, `src/FE/src/app/shared/components/confirm-dialog/confirm-dialog.spec.ts:59-209`, `src/FE/src/app/platform/quan-tri-nguoi-dung/pages/quan-tri-nguoi-dung/quan-tri-nguoi-dung.page.html:107-115`, `src/FE/src/app/platform/phan-quyen/pages/phan-quyen/phan-quyen.page.html:172-181`

> 🔄 **SỬA 2026-09-06** — bản trước sai ba nhóm:
> 1. **Nhãn hai nút ghi là `'Đồng ý'` / `'Huỷ'`**. Cả hai input nay mặc định `''`, và câu mặc định đến từ `shared.action.confirm` / `shared.action.cancel` trong template.
> 2. **Normalize #1 nói component không có nơi dùng** — nay có hai. Xem mục đó.
> 3. **Mười hai neo `file:dòng` trỏ sai chỗ** sau khi `confirm-dialog.ts` dài thêm phần chú thích i18n (139 → 148 dòng); đã neo lại theo bản hiện tại. Ví dụ Reference markup cũng đã thay bằng một lời gọi có thật — ví dụ cũ trích một màn DTI đã xóa 2026-08-29.

## Do / Don't

- ✅ Keep it to two buttons. The third-choice footer is a different component, and the decision to drop it here is recorded in the source.
- ✅ Match the badge to the button: `severity="bad"` goes with `[confirmDanger]="true"`.
- ✅ Write the title as the **question**, ending in a question mark, with the object named. The shipped pair does this: title `Khoá tài khoản này?`, description spelling out that the session ends within 30 minutes rather than immediately. Put the consequence in `description` — that is what the muted second line is for.
- ✅ Use `confirmDisabled` while the parent is saving. It is the only correct way to stop a double submit here, and it deliberately leaves the exit routes open.
- ✅ Listen for `cancelled` and treat it as a real answer. Escape produces it, and Escape is how most people close a modal.
- ✅ Call `close()` from the parent when the *underlying reason* disappears (the record was deleted elsewhere) — that path is silent by design.
- ❌ Don't wrap the `<dialog>` in an overlay `<div>`. It costs the focus trap, the backdrop, Escape and focus restoration, with no error to warn you.
- ❌ Don't add `autofocus` to any control. The lint rule blocks it, and focus placement is already handled in `open()`.
- ❌ Don't rename the outputs to `confirm`/`cancel`. `cancel` is a real DOM event of `<dialog>`; the past-tense names avoid the collision and `@angular-eslint/no-output-native` enforces it.
- ❌ Don't use this for a form. A dialog that collects input is `.form-dialog` — see `Dialog.md` and `FormRow.md`.
- ❌ Don't leave `title` unwritten — it is `input.required` and it is the dialog's accessible name.

## Normalize on redesign
1. ~~**The component has no consumers.**~~ — **đã xử lý**, đối chiếu 2026-09-06. Two screens now drive it: the account-lock prompt on `/quan-tri/nguoi-dung` (`quan-tri-nguoi-dung.page.html:107-115`) and the unsaved-changes prompt behind `canDeactivate` on `/quan-tri/phan-quyen` (`phan-quyen.page.html:172-181`). Both pick `severity="warn"` with `[confirmDanger]="true"` — the badge says "nothing is destroyed", the button says "you lose what you were doing", and that pairing is deliberate, not the mismatch Normalize #2 warns about. Count call sites with a command rather than a number written here:
   ```bash
   grep -rn '<app-confirm-dialog' src/FE/src/app
   ```
2. **`severity` and `confirmDanger` are independent, and nothing enforces agreement.** A `bad` badge over a `.primary` confirm button compiles, renders, and tells the user two different things. Deriving the button tone from the severity (with an explicit escape hatch) would remove the failure mode.
3. **The 17px badge glyph is off the `fontSize.fs-*` scale**, as is the 40px badge box against the spacing scale — `dimension.dialog-icon` records the value but no scale explains it.
4. **The × and the Huỷ button do the same thing.** Two controls, one outcome, in a 420px panel; the × exists mostly because dialogs conventionally have one.
5. **Nothing announces the dialog's severity.** The badge is `aria-hidden` and the colour is the only carrier, so a screen-reader user gets "Xoá chỉ tiêu 6.4?" with no signal that this one is destructive. The severity belongs in the title text, or in visually-hidden text next to it.
6. **`.dialog-actions` is right-aligned with the confirm button last**, which is the Windows order; the app has no recorded stance on button order and no other two-button footer to be consistent with.

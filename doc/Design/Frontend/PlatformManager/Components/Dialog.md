---
kind: luat
scope: du-an
verified: 2026-09-06
project: "PlatformManager"
status: "draft"
updated: "2026-08-22"
component: "Dialog"
sources: ["src/FE/src/styles.scss", "src/FE/src/app/platform/quan-tri-nguoi-dung/components/user-form-dialog/user-form-dialog.html", "src/FE/src/app/shared/components/confirm-dialog/confirm-dialog.html", "src/FE/src/app/shared/components/confirm-dialog/confirm-dialog.scss"]
---

# Dialog

> 🗄️ **Chỗ nào dưới đây nhắc `/dashboard` hoặc `/danh-muc/dti` là LỊCH SỬ, không phải hiện trạng.**
> Module nghiệp vụ `DtiWeekly` — cả cây `src/FE/src/app/modules/` — đã xoá 2026-08-29 để xây lại; danh
> sách route sống hôm nay đọc thẳng ở `src/FE/src/app/app.routes.ts`. Spec này vẫn là `kind: luat` vì
> component còn dùng thật trên màn Core, nên **không** hạ cả file xuống `lich-su` — chỉ những chỗ nhắc
> màn đã xoá mới bị hạ cấp, và chúng đều mang dấu 🗄️ hoặc được viết ở thì quá khứ. Nội dung đầy đủ
> của hai màn đó nằm ở file chủ lịch sử [`../Screens/01-dashboard.md`](../Screens/01-dashboard.md) và
> [`../Screens/02-danh-muc-dti.md`](../Screens/02-danh-muc-dti.md) (§5 — một chủ đề một file chủ); code
> tra ở commit `98a5d96`.
>
> ⚠️ **Đối chiếu từng phần, không phải toàn file.** Lượt 2026-09-08 mở source kiểm § Anatomy, § Variants,
> § States và toàn bộ neo của dòng `Sources:`; phần còn lại vẫn mang ngày của lượt trước
> (`verified: 2026-09-06` ở frontmatter). Đừng đọc dòng này thành "cả file đã kiểm".
>
> Bản trước của chính dòng này viết *"Chưa đối chiếu lại toàn file … **đúng như `verified:
> chua-doi-chieu` ở frontmatter**"* trong khi frontmatter khi đó ghi `verified: 2026-09-06` — thân file
> và frontmatter nói ngược nhau, và `check-docs.sh` §10 chỉ đếm xem ba khoá có mặt hay không nên nó
> không có cách nào thấy. `FormRow.md` mang y hệt câu đó, cùng một lỗi, phát hiện cùng ngày.
>
> Trích dẫn dạng `styles.scss § <selector>` chỉ neo theo tên selector chứ không theo số dòng; giá trị
> thật lấy bằng lệnh, đừng tin số chép trong bảng:
> `grep -n '^dialog \|^\.dialog-actions' src/FE/src/styles.scss`.

**Description:** Native `<dialog>` modal (`styles.scss` § `dialog`), in three width variants; mỗi instance là một component Angular riêng, mở bằng `showModal()` và phát lại sự kiện `(close)` của trình duyệt. 🗄️ Bản 2026-08-22 của spec này đếm **sáu** instance; bốn trong số đó (`report-dialog`, `criteria-form-dialog`, `import-dialog`, `import-result-dialog`) đã xoá 2026-08-29 cùng module `DtiWeekly`. Đếm bằng lệnh thay vì tin số chép (§6): `find src/FE/src/app -type d -name '*dialog*'`. PrimeNG's `p-dialog` is **not** used anywhere — every modal in the app is the browser-native element.

## Anatomy
`<dialog>` (borderless, `rounded.dialog`, `spacing.sp-5` padding, `box-shadow: 0 24px 70px rgba(0,0,0,.25)`, browser-centred) → `.title` row (`styles.scss` § `.title`: `space-between` flex, `<h2>` at `typography.h2-title`, plus an optional `Đóng` `Button` on the right) → body (form rows, a report block, or a message paragraph) → `.dialog-actions` footer (`justify-content:flex-end`, gap `spacing.sp-3`, `margin-top: spacing.sp-5`) holding one or two `Button`s. ⚠️ **Corrected 2026-09-08:** this parenthesis read `gap:8px`, `margin-top:12px` — the pre-consolidation per-component values — while the paragraph immediately below it already gave the tokenised ones. Two sentences, two answers, three lines apart.

`.dialog-actions` **nay là class toàn cục** — `styles.scss` § `.dialog-actions`, `gap: var(--sp-3)` +
`margin-top: var(--sp-5)`. Giữ nguyên tuyên bố ngược lại của bản 2026-08-22 thì spec này đang nói sai
(§4), nên nó được sửa tại chỗ ở đây và ở § Normalize #1.

> 🗄️ **Tình trạng cũ — đã dọn 2026-08-29, giữ lại làm bài học.** Trước ngày đó `.dialog-actions`
> được khai lại trong từng stylesheet component và các bản không khớp nhau — ba giá trị `margin-top`
> khác nhau và một bản quên hẳn `gap`, nên chân hộp thoại ở mỗi dialog cao thấp một kiểu. Chính
> source ghi lại chuyện này trong comment ở
> `src/FE/src/app/shared/components/confirm-dialog/confirm-dialog.scss`. Bốn trong sáu file dưới đây
> đã xoá cùng `DtiWeekly`; hai file còn lại không còn khai `.dialog-actions` nữa.
>
> **No line numbers below, deliberately.** Every row describes a declaration that no longer
> exists, so a line range could only point at something else. Four of the six files were
> deleted outright; in the two that survive, the block was removed.
>
> > 🔄 **SỬA 2026-09-08 — chẩn đoán cũ sai, và sai theo hai hướng ngược nhau.**
> >
> > Bản trước viết: *"`user-form-dialog.scss` dòng `26-31` và `confirm-dialog.scss` dòng `6-11` đều
> > vượt quá cuối file. Cổng không bắt được vì cả hai viết dạng tên file trần, không có tiền tố
> > `src/`."* (Hai neo đó nay viết ở dạng "tên file + dòng" tách rời, cố ý — viết lại đúng khuôn
> > `tên:số` sẽ làm cổng đi phân giải một neo mà cả mục này đang giải thích là **không** nên phân
> > giải.)
> >
> > **Lý do thì nay đã sai:** cổng xử lý được tên trần từ 2026-09-08 (`check-docs.sh` §4c2 tự nối lại
> > đường dẫn từ tên cơ sở), và nó kiểm **đuôi** của range chứ không còn chỉ kiểm đầu. Cả ba neo đều
> > đỏ ngay lượt chạy đầu tiên sau khi vá.
> >
> > **Còn hai neo thì không cùng một loại lỗi**, và gộp chúng làm một là chỗ chẩn đoán cũ hỏng nặng
> > nhất — đối chiếu git 2026-09-08:
> >
> > - `user-form-dialog.scss` dòng `26-31` — **chưa bao giờ đúng.** Bản duy nhất từng chứa khối
> >   `.dialog-actions` là commit `99d28ba`, ở đó file dài **25 dòng** và khối nằm ở dòng `20-25`.
> >   Neo này lệch 6 dòng ngay từ ngày viết, chứ không phải trôi theo thời gian.
> > - `confirm-dialog.scss` dòng `6-11` — **đúng chính xác**, nhưng cho một file **khác**: bản dưới
> >   `modules/danh-muc-dti/` (11 dòng, `.dialog-actions` chiếm đúng `:6-11`), đã xoá 2026-08-29.
> >   Hôm nay cái tên trần đó phân giải sang `src/FE/src/app/shared/components/confirm-dialog/confirm-dialog.scss`
> >   — một file **còn sống**, dài 7 dòng, không liên quan gì. Đây là **va chạm tên cơ sở**, không
> >   phải neo sai.
> >
> > Vì sao đáng ghi: va chạm tên cơ sở là cách hỏng riêng của dạng viết tắt, và nó **nguy hiểm hơn**
> > một neo chết. Neo chết thì không mở được nên người đọc biết ngay là hỏng; neo va chạm mở ra một
> > file có thật, đọc trôi chảy, và nói một chuyện khác hẳn. Hai component cùng tên ở hai tầng
> > (`shared/` và module nghiệp vụ) là chuyện bình thường trong repo này — nên cách chữa duy nhất là
> > **viết đủ đường dẫn**, đúng như `check-docs.sh` §4c2 nói khi nó báo lỗi "trùng tên".
>
> | Declared in | `gap` | `margin-top` |
> | --- | --- | --- |
> | `report-dialog.scss` — deleted 2026-08-29 | 8px | 12px |
> | `csv-import-dialog.scss` — deleted 2026-08-29 | 8px | 12px |
> | `import-result-dialog.scss` — deleted 2026-08-29 | **absent** | 12px |
> | `criteria-form-dialog.scss` — deleted 2026-08-29 | 8px | **8px** |
> | `src/FE/src/app/platform/quan-tri-nguoi-dung/components/user-form-dialog/user-form-dialog.scss` — file still exists, block removed (its header comment records the removal) | 8px | **8px** |
> | `src/FE/src/app/shared/components/confirm-dialog/confirm-dialog.scss` — file still exists, block removed (its header comment records the removal) | 8px | **16px** |

## Variants

| Variant | Classes | Key values | When to use |
| --- | --- | --- | --- |
| Wide (default) | `dialog` (no modifier) | `width: min(700px, 92vw)` (`styles.scss` § `dialog`) | 🗄️ **Không còn call site.** Biến thể mặc định này chỉ từng dùng cho báo cáo nhanh của dashboard (`report-dialog.html:1`) — nội dung rộng nhất, một khối HTML sinh sẵn — đã xoá 2026-08-29 |
| Form | `dialog.form-dialog` | `width: min(560px, 92vw)` (`styles.scss` § `dialog.form-dialog`) | Modal nhập liệu. Còn sống: user create-edit. 🗄️ Ba cái còn lại — criteria create-edit, CSV import, import result — đã xoá 2026-08-29 |
| Confirm | `dialog.confirm-dialog` | `width: min(420px, 92vw)` (`styles.scss` § `dialog.confirm-dialog`) | Destructive confirmation only (`confirm-dialog.html:1`) |
| Backdrop | `dialog::backdrop` | bg `colors.overlay-backdrop` (`rgba(20,28,40,.45)`, `styles.scss` § `dialog::backdrop`) | Automatic on `showModal()`; never rendered by `show()` |

### Các instance còn sống

| Component | Variant | Title | Footer actions |
| --- | --- | --- | --- |
| `user-form-dialog` | Form | bound `title()` | `Huỷ` (default) · `Lưu` (primary) — plus `Đóng` in the title row |
| `confirm-dialog` | Confirm | bound `title()`, passed `"Xác nhận"` | `Huỷ` (default) · bound `confirmLabel()` (**danger**) |

Đường dẫn thật của hai component này lấy bằng lệnh, đừng chép (§6):
`find src/FE/src/app -type d -name '*dialog*'` — `confirm-dialog` đã chuyển sang `shared/components/`,
nên nó có spec riêng ở [`ConfirmDialog.md`](./ConfirmDialog.md).

### 🗄️ Bốn instance đã xoá cùng module `DtiWeekly` (2026-08-29)

Không còn trong `src/FE`. Giữ lại làm đầu vào thiết kế cho lần xây lại; nội dung màn ở
[`../Screens/01-dashboard.md`](../Screens/01-dashboard.md) và
[`../Screens/02-danh-muc-dti.md`](../Screens/02-danh-muc-dti.md).

| Component (đã xoá) | Variant | Title | Footer actions khi còn sống |
| --- | --- | --- | --- |
| `report-dialog` | Wide | bound `title()` | `Sao chép` (default) · `In` (primary) — plus `Đóng` in the title row |
| `criteria-form-dialog` | Form | bound `title()` | `Huỷ` (default) · `Lưu chỉ tiêu` (primary) — plus `Đóng` in the title row |
| `import-dialog` | Form | `Import CSV/Excel` | `Huỷ` (default) · `Nhập dữ liệu` (primary, `[disabled]`) — plus `Đóng` in the title row |
| `import-result-dialog` | Form | `Kết quả Import` | `Đóng` (primary) — plus a second `Đóng` (default) in the title row |

🗄️ Ghi chú bất đối xứng của bản cũ — `confirm-dialog` là dialog duy nhất **không** có nút `Đóng` ở
hàng tiêu đề, còn `import-result-dialog` là cái duy nhất có `Đóng` hai lần — nay chỉ còn đúng nửa đầu:
`import-result-dialog` đã xoá 2026-08-29.

## States
<!-- Exactly these five rows, in this order — treatments as rendered by the shipped CSS. -->

| State | Treatment |
| --- | --- |
| default (closed) | Not rendered — a native `<dialog>` without the `open` attribute is `display:none`. 🗄️ Mẫu `@defer` (component chưa dựng cho tới khi được yêu cầu) từng dùng cho hai dialog của màn DTI đã xoá 2026-08-29 (`danh-muc-dti.page.html:76-87`); không màn Core nào đang dùng lại |
| default (open) | Browser-centred, `rounded.dialog`, `spacing.sp-5` padding, borderless, `box-shadow: 0 24px 70px rgba(0,0,0,.25)`, width per variant; `::backdrop` fills the viewport with `colors.overlay-backdrop` |
| hover | **Not styled** — no `dialog:hover` rule; hover belongs to the `Button`s inside (see `Button.md`) |
| focus | **Browser default focus trap.** `showModal()` makes the dialog the top layer and confines Tab to it; Escape fires `(close)`, which every component re-emits as a `closed` output. No custom `:focus` CSS is authored on the dialog. `appAutofocus` moves initial focus to the first field of a form dialog (`user-form-dialog.html:12`; 🗄️ dialog thứ hai `criteria-form-dialog.html:9` đã xoá 2026-08-29) |
| active | **N/A for the dialog element** — it is not a control and has no `:active` rule. Its footer `Button`s carry the shared `.btn:active` press offset (`styles.scss` § `.btn:active`) |
| disabled | **N/A for the dialog element.** The state lives on its buttons — `confirm-dialog`'s actions are never disabled. 🗄️ Ví dụ "primary `[disabled]` cho tới khi chọn file" là `csv-import-dialog.html:18`, đã xoá 2026-08-29 |

## Tokens Used
- `colors.card` (the `<dialog>` background comes from the UA default white, matching `colors.card`), `colors.overlay-backdrop`, `colors.text`
- `rounded.dialog`
- `spacing.sp-5` (dialog padding)
- `typography.h2-title` (title row)

- `spacing.sp-3` (footer gap), `spacing.sp-5` (footer `margin-top`) — from the global `.dialog-actions`

The `0 24px 70px rgba(0,0,0,.25)` elevation and the three `min(…, 92vw)` widths are literals — there is no elevation scale (`Tokens/spacing.md`). ⚠️ **Corrected 2026-09-08:** this sentence also listed *"the `8px`/`12px`/`16px` footer values"* as live literals. They are not live — they are the seven per-component copies the 2026-08-29 consolidation deleted, and the footer now takes both values from the scale. Listing a dead value among live ones is how a redesign ends up re-introducing it.

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

Sources: `src/FE/src/styles.scss` § `dialog` (element + the two width variants `dialog.form-dialog` / `dialog.confirm-dialog` + `dialog::backdrop`), § `.title`, § `.dialog-actions`, § `.form-error`, `src/FE/src/app/platform/quan-tri-nguoi-dung/components/user-form-dialog/user-form-dialog.html:1-167`, `src/FE/src/app/shared/components/confirm-dialog/confirm-dialog.html:1-46`, `src/FE/src/app/shared/components/confirm-dialog/confirm-dialog.scss:1-7` (the header comment that records the `.dialog-actions` consolidation).

> 🗄️ **Nguồn đã chết, tách khỏi dòng `Sources:` ngày 2026-09-08.** Sáu neo dưới đây từng nằm trên dòng `Sources:` ở trên và **đã xoá** 2026-08-29 cùng module `DtiWeekly` — bốn dưới `src/FE/src/app/modules/danh-muc-dti/` (`confirm-dialog.html` 10 dòng + `confirm-dialog.scss` 11 dòng, `criteria-form-dialog.html` 42 dòng, `csv-import-dialog.html` 22 dòng, `import-result-dialog.html` 35 dòng, `danh-muc-dti.page.html` 87 dòng — khối `@defer` ở dòng 76-87) và hai dưới `src/FE/src/app/modules/dashboard/` (`report-dialog.html` 11 dòng + `report-dialog.scss` 6 dòng). Tra ở commit `98a5d96`.
>
> **Không neo nào trong sáu neo đó là neo bịa** — đối chiếu từng cái tại commit `98a5d96` ngày 2026-09-08: mọi range đều dừng đúng ở dòng cuối của file nó trỏ tới. Đáng nói riêng là `report-dialog.scss` dòng `1-6`, cái mà cổng hôm nay báo *"không tìm thấy file nào tên"*: file đó **có thật**, dài đúng 6 dòng, và chứa đúng khối `.dialog-actions` mà bảng phía trên mô tả (`gap:8px`, `margin-top:12px`). Một neo cổng không phân giải được là neo **chết**, chưa nói lên điều gì về việc nó có từng đúng hay không — hai chuyện đó phải tra git mới biết, và trong file này chúng ra hai kết quả ngược nhau (xem khối SỬA ở § Anatomy).
>
> Vì sao phải rời khỏi dòng `Sources:`: một mệnh đề *"… đã xoá 2026-08-29 …"* ở cuối dòng khiến cổng miễn trừ **toàn bộ** neo trên dòng đó, sống lẫn chết. Cổng đã bịt lỗ này. Dòng `Sources:` là **danh sách bằng chứng** — mọi thứ trên đó phải mở được hôm nay; chuyện đã xảy ra thì kể ở thân file, đúng chỗ này.

## Do / Don't

- ✅ Open via native `showModal()` — it supplies the focus trap, Escape handling and `::backdrop` for free. Don't reimplement as a positioned `<div>`.
- ✅ Match the width variant to content weight: `confirm-dialog` for a one-sentence question, `form-dialog` for data entry, the default for wide read-only content. (🗄️ Người dùng duy nhất của bản rộng — báo cáo dashboard — đã xoá 2026-08-29, nên biến thể này hiện không có call site.)
- ✅ Put the destructive action in the footer as `btn danger` and the escape hatch (`Huỷ`) to its left — the shipped order.
- ✅ Surface errors at **two** levels, the way the shipped form dialog does: one `.form-error` per failing field inside its own `.form-row`, plus a single un-`id`'d `.form-error` above the footer for messages with no field name (`styles.scss` § `.form-error`; the split is `FormRow.md`'s to own). ⚠️ **Corrected 2026-09-08.** This bullet read *"mỗi dialog đúng một khối, **không có slot lỗi theo từng ô**"*. `user-form-dialog.html` renders five per-field slots and one form-level one, and has since 2026-08-29 — the same false claim `FormRow.md` was carrying, in a second file.
- ✅ Bọc dialog nặng trong `@defer` để code chỉ tải khi cần. (🗄️ Mẫu tham chiếu là trang danh mục DTI, đã xoá 2026-08-29 — hiện chưa màn Core nào áp dụng lại.)
- ❌ Don't stack or nest dialogs — the app never has two open at once. (🗄️ Tiền lệ: `import-result-dialog` chỉ mở sau khi `import-dialog` đóng; cả hai đã xoá 2026-08-29.)
- ❌ Don't add a confirm-before-close step to a read-only dialog; `Đóng` calls `close()` directly.
- ❌ Don't reach for PrimeNG's `p-dialog` — no instance uses it, and its chrome would not match these three widths.

## Normalize on redesign
1. ~~`.dialog-actions` is duplicated in component stylesheets rather than declared once globally~~ — **Đã xử lý 2026-08-29.** Nó đã được đưa lên toàn cục ở `styles.scss` § `.dialog-actions` với một bước spacing duy nhất (`--sp-3` / `--sp-5`); lý do ghi ngay trong comment ở `src/FE/src/app/shared/components/confirm-dialog/confirm-dialog.scss`.
2. `<dialog>` has no explicit `background` — it relies on the UA default white happening to equal `colors.card`. Set it from the token.
3. **Vẫn còn, nhưng nhỏ đi.** Bản cũ: bốn dialog có `Đóng` ở hàng tiêu đề, `confirm-dialog` không có, `import-result-dialog` có hai — 🗄️ phần lớn bằng chứng đó đã xoá 2026-08-29. Trong hai dialog còn sống thì bất đối xứng vẫn nguyên (`user-form-dialog` có, `confirm-dialog` không), nên vẫn phải chốt một mẫu.
4. The `Đóng` control is a text `Button`, not an icon-only close — while `Toast` uses `pi pi-times` for the same job. Converge.
5. The elevation `0 24px 70px rgba(0,0,0,.25)` is a fourth uncontrolled shadow value alongside `--shadow` and the two `.btn` hover shadows. There is no elevation scale.
6. 🗄️ **Hết hiệu lực 2026-08-29.** `report-dialog` rendered `[innerHTML]="safeContent()"` — the only innerHTML binding in the app; its internal `.report` markup was generated in TypeScript and therefore had no component spec. Component đã xoá; giữ lại vì lần xây lại màn báo cáo sẽ đụng đúng vấn đề này.

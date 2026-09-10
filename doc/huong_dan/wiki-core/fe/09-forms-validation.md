---
kind: luat
scope: core
verified: 2026-09-06
---

# 9. Form & Validation

> ### 🛑 ĐỌC TRƯỚC — repo này **KHÔNG dùng Reactive Forms** (đối chiếu 2026-09-06)
>
> Nhiều mẫu bên dưới viết bằng `FormGroup` / `Validators` / `AbstractControl` / `AsyncValidatorFn`
> / `this.form.get(...)`. **Không file nào trong `src/FE/src` import `ReactiveFormsModule`,
> `FormBuilder` hay `FormGroup`:**
>
> ```bash
> grep -rn "ReactiveFormsModule\|FormBuilder\|FormGroup" src/FE/src --include=*.ts | grep -v spec
> # hôm nay: 0 dòng
> ```
>
> Mọi form đang chạy giữ giá trị bằng **`signal()`** và bind `(input)` trên thẻ `<input>` thuần —
> xem `src/FE/src/app/platform/login/pages/login/login.page.ts:93` (`userName`/`password` là
> `signal('')`) và `src/FE/src/app/platform/quan-tri-nguoi-dung/components/user-form-dialog/user-form-dialog.ts`.
>
> **Điều đó KHÔNG làm file này vô dụng** — các mục về validate 2 lớp, bind lỗi từ `fieldErrors`,
> `dirty` + điều hướng, khoá nút lúc gửi đều là luật độc lập với cơ chế form. Nhưng đừng chép
> nguyên khối `FormGroup` nào ở đây vào code: nó sẽ là **cơ chế thứ hai** trong một codebase
> đang có đúng một, tức đúng thứ §5 `.claude/CLAUDE.md` cấm. Chưa có quyết định "đổi sang
> Reactive Forms"; nếu cần đổi, chốt ở đây trước rồi mới viết code.
>
> 🔄 LẬT 2026-09-06: trước bản này file không có một dòng nào nói ra khoảng cách đó, nên nó đọc
> như thể `FormGroup` là hiện trạng.

## Container — drawer/side-panel trước, modal sau

Đã có trong `ui-conventions.md` — nhắc lại lý do: modal che toàn màn hình
làm mất ngữ cảnh (không thấy được dữ liệu nền khi điền form phức tạp nhiều
bước). Side-panel giữ được ngữ cảnh, tự nhiên hỗ trợ responsive (full-width
khi màn hình hẹp). Modal/dialog nhỏ **chỉ** dùng cho xác nhận ngắn
(`confirm-dialog`, đã có sẵn trong `styles.scss`).

## Validate 2 lớp — giống nguyên tắc BE

| Lớp | Kiểm tra gì | Khi nào chạy |
|---|---|---|
| Client (Angular `Validators`/custom) | Format, required, độ dài — phản hồi tức thì, không cần round-trip | Lúc gõ/blur |
| Server (`ValidationBehavior`/`ErrorDescriptor` phía BE) | Business rule cần DB (trùng mã, FK tồn tại) | Lúc submit |

Client validate **không thay thế** server validate — chỉ để UX phản hồi
nhanh. Submit vẫn phải xử lý được lỗi 400 (`fields` + `fieldErrors`) trả về từ BE dù client
đã "pass" hết (race condition, dữ liệu đổi giữa lúc mở form và lúc submit).

> Bổ sung 2026-08-24, đối chiếu thực hành ngành cho hệ thống tầm trung: đoạn
> trên coi validate 2 lớp là vấn đề *đồng bộ dữ liệu tại thời điểm submit*
> (race condition). Còn một rủi ro khác, độc lập với race condition: **2 bộ
> luật viết bằng 2 ngôn ngữ, ở 2 codebase khác nhau, không có gì tự động giữ
> chúng khớp nhau.** Angular `Validators.required` và FluentValidation
> `RuleFor(x => x.Email).NotEmpty()` là 2 định nghĩa tay, độc lập — sửa một
> bên (nới lỏng field từ bắt buộc thành tuỳ chọn theo yêu cầu nghiệp vụ mới)
> mà quên bên kia thì FE và BE lệch luật **thật**, không phải lý thuyết: FE
> cho submit vì tưởng field hợp lệ, BE từ chối với 400; hoặc ngược lại FE
> chặn nhầm một giá trị BE vẫn chấp nhận.
>
> Không có cách nào loại bỏ hoàn toàn rủi ro này nếu không sinh rule từ 1
> nguồn chung (ngoài phạm vi hiện tại — chưa có nhu cầu). Giảm nhẹ được bằng
> kỷ luật: **FE validate chỉ để phản hồi nhanh, KHÔNG bao giờ tự quyết "hợp
> lệ" thay BE** — mọi lỗi 400 từ BE, kể cả field FE tưởng đã "chắc chắn
> đúng", đều phải hiển thị được (đúng cơ chế bind `fieldErrors` ở mục dưới). Khi
> 2 luật lệch nhau, **BE luôn thắng** và FE phải có đường hiển thị lỗi đó —
> không tự đoán, không im lặng bỏ qua vì "chắc chắn đã pass validate rồi".

## Async validator — debounce bắt buộc cho field check qua API

> Bổ sung 2026-08-24, đối chiếu thực hành ngành cho hệ thống tầm trung: file
> này chưa bàn tới field cần validate qua API (check trùng email/username) —
> khoảng trống thật, vì đây là dạng lỗi rất dễ mắc: validate mỗi phím gõ.

Vì sao vấn đề THẬT: field như "email đăng nhập" cần hỏi BE "đã tồn tại
chưa" — nếu chạy `AsyncValidatorFn` theo đúng nhịp Angular mặc định
(`updateOn: 'change'`), gõ 10 ký tự bắn **10 request**, phần lớn cho giá trị
mà người dùng chưa gõ xong. Tệ hơn tần suất: network không đảm bảo thứ tự
response — response của ký tự thứ 3 (đã cũ) có thể về **sau** response của
ký tự thứ 7, đè kết quả mới bằng kết quả cũ nếu không huỷ request cũ.

> 📐 **CHƯA THI CÔNG (đối chiếu 2026-09-06)** — chưa field nào validate qua API, và mẫu dưới
> dùng `AsyncValidatorFn` của Reactive Forms (xem cảnh báo đầu file). Luật **300ms** thì áp bất
> kể cơ chế: nó là con số đã chốt ở [13-performance.md](13-performance.md) §6.

```ts
// vd validate email trùng lúc tạo user (Reactive Forms — chưa dùng trong repo này)
export function uniqueEmailValidator(userService: UserService): AsyncValidatorFn {
  return (control: AbstractControl): Observable<ValidationErrors | null> =>
    timer(300).pipe(                                                // 300ms — cùng chuẩn debounce
      switchMap(() => userService.checkEmailExists(control.value)),
      map(exists => (exists ? { emailTaken: true } : null)),
    );
}
```

- **`timer(300)` thay vì `debounceTime`.** Angular tự huỷ (`unsubscribe`)
  lần chạy `AsyncValidatorFn` trước đó mỗi khi control cần validate lại —
  đặt độ trễ ngay đầu pipe bằng `timer()` tận dụng đúng cơ chế huỷ có sẵn đó
  để debounce, không cần tự quản lý subscription. `switchMap` lo phần còn
  lại: nếu 1 request cũ chưa kịp huỷ mà vẫn đang bay, giá trị mới bắt đầu sẽ
  huỷ nó trước khi nhận response.
- **300ms — dùng đúng số đã chốt** ở
  `doc/huong_dan/wiki-core/fe/13-performance.md` §6 cho debounce ô tìm kiếm,
  không tự chọn số khác cho async validator — cùng một loại quyết định (chờ
  người dùng ngừng gõ trước khi gọi API), tách ra 2 con số khác nhau chỉ tạo
  thêm một chỗ phải nhớ mà không mua thêm gì.
- Chọn `updateOn: 'change'` hay `'blur'` là quyết định UX riêng của từng
  form (blur ít gọi API hơn nhưng phản hồi chậm hơn) — debounce ở trên áp
  dụng đúng bất kể chọn cái nào.

## Bind lỗi từ `fieldErrors` vào form

Key từ BE là **PascalCase** (`MaxScore`, `Roles`) — cố ý khác phần còn lại của payload, xem
[`../../quy-uoc/fe-api-client.md`](../../quy-uoc/fe-api-client.md) §Envelope.

> ### ⚠️ Envelope có HAI trường lỗi theo ô — chỉ MỘT trong hai dịch được
>
> | Trường | Kiểu | Khai tại | Dịch được? |
> | --- | --- | --- | --- |
> | `fields` | `Record<string, string[]>` | `src/FE/src/app/core/http/api-result.model.ts:77` | ❌ chuỗi trần BE dựng sẵn |
> | `fieldErrors` | `Record<string, ApiFieldError[]>` | `src/FE/src/app/core/http/api-result.model.ts:98` | ✅ mang `code` + `messageParams` |
>
> Mục này nói về **`fieldErrors`** — đó là trường `groupServerFieldErrors` nhận, và là trường
> **duy nhất** tra được sang bảng dịch qua `ApiErrorMessageService.fieldMessage`.
>
> **`fields` VẪN CÒN trên dây, chưa gỡ.** Hai trường cố ý chạy song song cho tới bước 11 của
> [`../be/16-i18n-va-ma-loi.md`](../be/16-i18n-va-ma-loi.md) §7 — bước "DỌN: gỡ trường cũ khỏi
> envelope", `doc/huong_dan/wiki-core/be/16-i18n-va-ma-loi.md:547`. Đừng đọc thấy `fieldErrors` ở
> đây rồi tưởng `fields` đã biến mất; và ngược lại, đừng viết form mới đọc `result.fields`: code
> vẫn chạy, câu vẫn hiện ra, chỉ **mất sạch mã lỗi và mọi bản dịch** — hỏng IM LẶNG. Đây là lỗi
> đã xảy ra thật ngày 2026-09-06, ghi lại tại chỗ ở
> `src/FE/src/app/platform/quan-tri-nguoi-dung/components/user-form-dialog/user-form-dialog.ts:116-117`
> (*"BE **để trống** `fields` và chỉ điền `fieldErrors`, nên bản trước bỏ lọt toàn bộ mã Identity"*).

> ### 🔄 LẬT 2026-09-06 — mẫu cũ gọi một hàm KHÔNG tồn tại, và giải sai bài toán
>
> Nguyên văn cũ: `this.form.get(toCamelCase(key))?.setErrors({ server: messages[0] })`, kèm câu
> *"cần 1 hàm `toCamelCase` dùng chung ở `core/`"*. Hai vấn đề:
>
> - **`toCamelCase` không tồn tại** ở bất cứ đâu trong `src/FE/src` — và nó cũng **không nên**
>   tồn tại: cách làm thật **giữ nguyên PascalCase**, tra thẳng `fieldErrors()['Roles']`. Đổi
>   casing chỉ thêm một phép biến đổi có thể sai giữa hai đầu đã khớp sẵn.
> - Mẫu cũ **bỏ sót cạm bẫy thật.** `RuleForEach(x => x.Roles)` của FluentValidation phát
>   `PropertyName` dạng `Roles[0]`, `Roles[1]`, và BE giữ nguyên chuỗi đó. Tra thẳng
>   `fields['Roles']` sẽ **trượt, và lỗi biến mất im lặng** — người dùng thấy form từ chối mà
>   không ô nào đỏ.
>
> Bản đang chạy — `groupServerFieldErrors`, `src/FE/src/app/core/http/server-field-errors.ts:35`
> (đối chiếu 2026-09-10) — làm đúng **ba** việc, không việc nào bỏ được:
>
> 1. **Cắt hậu tố chỉ số** `[\d+]` ở cuối khoá.
> 2. **Dịch từng lỗi** qua callback `translate` — phần tử của `fieldErrors` là `ApiFieldError`
>    (mã lỗi + tham số), **không** phải câu tiếng Việt dựng sẵn. (Phần tử của `fields` **là** chuỗi
>    dựng sẵn — đó là trường khác, xem bảng đầu mục.)
> 3. **Gộp nhiều thông điệp về cùng một ô** thành một chuỗi — mỗi ô chỉ có một chỗ để hiện.

```ts
// core/http/server-field-errors.ts — giữ PascalCase, cắt chỉ số, dịch, gộp thông điệp
export function groupServerFieldErrors(
  fieldErrors: Record<string, ApiFieldError[]> | null,
  translate: (error: ApiFieldError) => string,
): Record<string, string> {
  if (!fieldErrors) return {};
  const grouped: Record<string, string[]> = {};
  for (const [rawKey, errors] of Object.entries(fieldErrors)) {
    const key = rawKey.replace(/\[\d+\]$/, '');          // `Roles[0]` → `Roles`
    grouped[key] = [...(grouped[key] ?? []), ...errors.map(translate)];
  }
  return Object.fromEntries(Object.entries(grouped).map(([key, messages]) => [key, messages.join(' ')]));
}
```

> 🔄 **SỬA 2026-09-10 — chữ ký ở mẫu cũ KHÔNG biên dịch được, chép ra là gãy.** Bản trước
> khai `fields: Record<string, string[]>` và **không có** tham số `translate`. Kiểu phần tử thật
> là `ApiFieldError`, và không có nó thì không dịch được lỗi qua `ApiErrorMessageService` — mẫu cũ
> ngầm giả định BE trả câu tiếng Việt dựng sẵn, tức đi ngược [08-i18n.md](08-i18n.md).
> Neo `user-form-dialog.ts:59` kèm theo cũng trỏ nhầm dòng.
>
> **Vá tiếp cùng ngày:** tham số đổi tên `fields` → `fieldErrors`. Kiểu đã đúng từ lượt trên,
> nhưng cái TÊN thì vẫn đang dạy sai tên trường của envelope — và nó là chữ ký công khai ở tầng
> đáy, tức thứ người viết form tiếp theo chép theo, đúng lúc trường trùng tên kia sắp bị gỡ ở
> bước 11. Thân hàm không đổi.

> ### 🔄 CHUYỂN CHỖ 2026-09-10 — hàm nay ở `core/http/`, KHÔNG chép lại vào dialog mới
>
> Trước ngày này hàm nằm **không export** trong file component của một màn `platform/`
> (`user-form-dialog.ts`). Nó không biết gì về màn đó: đầu vào là `fieldErrors` của envelope,
> thứ mọi form gặp 400 đều nhận. Nay ở `src/FE/src/app/core/http/server-field-errors.ts`, cạnh
> `api-result.model.ts` nơi `ApiFieldError` được khai; `user-form-dialog.ts` import lại từ đó.
>
> Lý do nâng, không phải "cho gọn": spec Danh mục DTI đặc tả **bốn** dialog —
> `spec/danh-muc-dti/ui-spec.md` V9–V12 (`spec/danh-muc-dti/ui-spec.md:122-125`), 📐 **CHƯA DỰNG**,
> xem chính spec đó khai *"không dialog nào dưới đây đã dựng"* (`spec/danh-muc-dti/ui-spec.md:21`).
> Người viết dialog thứ hai hoặc chép lại hàm, hoặc bỏ sót mẹo (1) — và bỏ sót thì lỗi biến mất im
> lặng, đúng ca đã mô tả ngay trên. Lý do nâng vẫn đứng vững dù bốn dialog kia chưa tồn tại: nơi
> gọi thứ hai là điều **spec đã chốt sẽ có**, còn cái giá của một bản sao thì trả ngay lúc nó ra
> đời. Hành vi khoá bằng `server-field-errors.spec.ts` (khoá có chỉ số, nhiều thông điệp một ô,
> `fieldErrors` null).
>
> Chữ ký và hành vi **giữ nguyên** trong lượt chuyển này — nếu cần đổi, chốt ở đây trước.

## Message hiển thị

Ưu tiên đọc thẳng `messages[0]` (đã là câu hoàn chỉnh do BE dịch qua
`ErrorDescriptor.Resolve`) — **không** tự ráp lại "Trường X: " + message,
vì `FieldError.Message` phía BE cố ý **không** chứa tên field — label field FE tự lấy
từ chính form (`<label>`), tránh 2 nơi cùng giữ tên field rồi lệch nhau khi
đổi copy.

🔄 LẬT 2026-09-06: câu trên trước đây neo bằng chứng vào `../../../tham-khao-ngoai/vnr-successor/03-p2-platform-application.md`
§4.5. File đó khai `kind: tham-chieu` ở frontmatter — nó mô tả lộ trình xây dựng của **một dự án
khác** (VNR.Successor), **không phải luật của repo này** (`.claude/CLAUDE.md` §9). Luật đang thi
hành về `ErrorDescriptor`/`fields` nằm ở
[`../../quy-uoc/be-cqrs-handler.md`](../../quy-uoc/be-cqrs-handler.md) và
[`../../quy-uoc/be-api-controller.md`](../../quy-uoc/be-api-controller.md). Bản thân quy tắc FE
ở đây không đổi.

## Form dirty + điều hướng đi — quy ước chung, không xử lý ca-by-ca

> Bổ sung 2026-08-24, đối chiếu thực hành ngành cho hệ thống tầm trung: file
> này chưa có quy ước chung nào cho việc mất dữ liệu chưa lưu khi điều hướng
> đi — và đây không phải rủi ro lý thuyết:
> `doc/Design/Frontend/PlatformManager/Screens/04-phan-quyen.md:165` ghi
> nhận màn phân quyền có `dirty()` signal nhưng **không có `CanDeactivate`
> guard nào đọc nó** — điều hướng đi (bấm sidebar, back trình duyệt) âm thầm
> bỏ toàn bộ thay đổi, trên một màn mà save là **ghi đè toàn bộ**. Sửa từng
> màn một khi phát hiện (đúng cách finding đó được tìm ra) không chặn được
> màn **tiếp theo** mắc lỗi tương tự — cần quy ước áp cho mọi form có khả
> năng mất dữ liệu, không phải sửa từng ca.

> ### ✅ CÓ THẬT — thi công xong 2026-08-31
>
> ```bash
> grep -rn "canDeactivate\|beforeunload" src/FE/src --include=*.ts | grep -v spec   # PASS khi khác rỗng
> ```
>
> Đã thi công cả hai lớp, đối chiếu 2026-08-31:
>
> | Thành phần | Ở đâu |
> |---|---|
> | `IHasUnsavedChanges` + `unsavedChangesGuard` | `src/FE/src/app/core/guards/unsaved-changes.guard.ts:12,27` |
> | `canDeactivate()` của component | `src/FE/src/app/platform/phan-quyen/pages/phan-quyen/phan-quyen.page.ts:199` |
> | `@HostListener('window:beforeunload')` | `src/FE/src/app/platform/phan-quyen/pages/phan-quyen/phan-quyen.page.ts:231` |
> | Khai trong route | `src/FE/src/app/platform/phan-quyen/phan-quyen.routes.ts:17` |
>
> Bốn phép nghiệm thu dưới đây đã có test tự động canh. Đừng chép số test vào đây
> (`.claude/CLAUDE.md` §6) — chạy `npx ng test --watch=false --browsers=ChromeHeadless`, PASS khi
> **0 failure**.
>
> 🔄 LẬT 2026-09-06: bản trước trỏ `@HostListener` vào `phan-quyen.page.ts:141`, một dòng nằm
> giữa nhánh `error:` của lời gọi tải ma trận — không liên quan. Và nó chép *"bộ FE xanh
> 235/235"*, đúng loại con số §6 cấm: nó chỉ đúng trong đúng ngày viết ra.
>
> Nghiệm thu:
>
> | # | Phép thử | PASS |
> |---|---|---|
> | 1 | Tick một ô ở màn Phân quyền rồi bấm một mục khác trên sidebar | Hiện hộp thoại xác nhận của app |
> | 2 | Chọn "ở lại" | Vẫn ở màn cũ, **các ô vừa tick còn nguyên** |
> | 3 | Tick một ô rồi đóng tab | Trình duyệt hiện hộp thoại cảnh báo của nó |
> | 4 | **Không** sửa gì rồi rời trang | Đi thẳng, không hỏi |
>
> Phép thử 4 quan trọng ngang ba phép kia: một guard hỏi cả khi không có gì để mất sẽ bị
> người dùng học cách bấm qua theo phản xạ, và lúc đó nó ngừng bảo vệ được gì.

Hai lớp riêng biệt, **cần cả hai**, không lớp nào thay được lớp kia:

| Lớp | Chặn được | Không chặn được |
| --- | --- | --- |
| `CanDeactivate` guard | Điều hướng **trong** Angular Router — click sidebar, back button SPA, gõ route khác trong app | Đóng tab, F5, đóng browser, gõ thẳng URL ngoài |
| `window:beforeunload` | Đóng tab, F5, đóng browser | Điều hướng trong SPA (Router không đụng tới sự kiện này) |

```ts
// core/guards/unsaved-changes.guard.ts — dùng chung cho MỌI form
export interface IHasUnsavedChanges {
  canDeactivate(): Observable<boolean> | boolean;
}

export const unsavedChangesGuard: CanDeactivateFn<IHasUnsavedChanges> = (component) =>
  component.canDeactivate();
```

```ts
// component form — mỗi feature tự quyết định "hỏi thế nào" (dùng lại
// confirm-dialog đã có, xem §Container ở đầu file), guard chỉ hỏi "có cần hỏi không"
export class PhanQuyenPage implements IHasUnsavedChanges {
  // Tên class là `ConfirmDialog`, không phải `ConfirmDialogComponent`.
  private readonly leaveConfirm = viewChild.required(ConfirmDialog);
  private leaveDecision: Subject<boolean> | null = null;

  canDeactivate(): Observable<boolean> {
    if (!this.dirty()) return of(true);          // sạch thì đi thẳng, KHÔNG hỏi
    const decision = new Subject<boolean>();
    this.leaveDecision = decision;
    this.leaveConfirm().open();                  // open() trả void — xem ghi chú dưới
    return decision.asObservable();
  }
  // Template nghe `(confirmed)` / `(cancelled)` rồi đẩy true/false vào `leaveDecision`.

  @HostListener('window:beforeunload', ['$event'])
  onBeforeUnload(event: BeforeUnloadEvent): void {
    if (this.dirty()) {
      event.preventDefault();
      event.returnValue = '';   // trình duyệt tự hiện dialog mặc định — KHÔNG custom được text (Chrome ≥51)
    }
  }
}
```

```ts
// feature.routes.ts — khai cạnh canActivate, cùng chỗ đã chốt ở fe-routing-guard.md
{
  path: '',
  canActivate: [authGuard, mustChangePasswordGuard, superAdminGuard],
  canDeactivate: [unsavedChangesGuard],
  loadComponent: () => import('./pages/phan-quyen/phan-quyen.page').then(m => m.PhanQuyenPage),
}
```

- **`CanDeactivateFn` nhận chính component instance** — guard dùng chung ở
  `core/` chỉ gọi `component.canDeactivate()`, còn "hỏi thế nào" (dialog nào,
  message gì) do từng feature tự quyết — tận dụng đúng `confirm-dialog` đã
  có sẵn trong template của chính component đó, không cần dựng thêm 1
  service overlay toàn cục.
- `beforeunload` **không thể custom message** trên trình duyệt hiện đại
  (Chrome ≥51 trở đi luôn hiện text mặc định của trình duyệt, bỏ qua bất kỳ
  chuỗi nào gán vào `returnValue`) — chỉ cần `preventDefault()` +
  `returnValue = ''` để trigger dialog, không cố ráp câu tiếng Việt vào đó.
- Interface `IHasUnsavedChanges` đặt ở `core/` — mọi form áp dụng cùng 1
  guard, không viết lại logic `CanDeactivate` riêng từng feature.

> ### ⚠️ Sửa 2026-08-31 — mẫu cũ mô tả một API không tồn tại
>
> Bản trước viết `confirmDialog().open('…')` như thể `open()` **nhận tham số và trả
> `Observable<boolean>`**. Component dùng chung thật thì khác:
> `open(): void`, tiêu đề/mô tả truyền qua `input()`, câu trả lời về qua **output**
> `confirmed` / `cancelled`
> (`src/FE/src/app/shared/components/confirm-dialog/confirm-dialog.ts:75,94`).
>
> Vì sao đáng sửa chứ không phải chi tiết vặt: người làm theo mẫu cũ sẽ thấy nó
> **không biên dịch được**, và lối thoát dễ nhất lúc đó là dựng một dialog thứ hai
> cho khớp mẫu — đúng thứ `COMPONENTS.md` tồn tại để ngăn. Cách bắc cầu bằng
> `Subject<boolean>` ở trên giữ đúng component dùng chung.

## Khoá form trong lúc đang gửi — ✅ CÓ THẬT (chốt 2026-08-31, đối chiếu lại 2026-09-06)

Nút gửi phải **tắt trong suốt thời gian request đang bay**, và bật lại khi có kết quả.

🔄 LẬT 2026-09-06: tiêu đề mục này còn mang nhãn 🚧 và đoạn ngay dưới mô tả *"Hiện trạng:
`user-form-dialog.html:134` không có `[disabled]` nào"* — trong khi cuối mục đã ghi ✅ thi công
xong. Một mục vừa nói chưa làm vừa nói đã làm thì người đọc tin vế nào cũng được. Đoạn hiện
trạng cũ nay chuyển sang **thì quá khứ**:

Trước 2026-08-31, nút gửi của dialog người dùng không có `[disabled]` và đường gửi không có cờ
"đang gửi" — mạng chậm thì người dùng bấm lại.

Thiệt hại **có giới hạn** nên mục này từng ở nhóm ưu tiên thấp: tên đăng nhập là duy nhất nên
lần thứ hai bị BE từ chối. Nhưng người dùng nhận thông báo *"tên đăng nhập đã tồn tại"*
ngay sau khi vừa tạo thành công chính tài khoản đó — một thông điệp đúng về mặt kỹ thuật
và vô nghĩa với người đọc.

Màn đăng nhập **đã làm đúng** việc này (`[disabled]="submitting()"`), nên đây là quy ước
đã tồn tại trong code mà chưa được viết ra và chưa áp đều.

> 📖 Trạng thái `:disabled` của ô nhập đã có sẵn trong lớp component dùng chung — đọc
> [`../../../Design/Frontend/PlatformManager/Components/Input.md`](../../../Design/Frontend/PlatformManager/Components/Input.md)

Nghiệm thu: bấm nút gửi hai lần thật nhanh ⇒ **đúng một** request rời khỏi client.

> ✅ **CÓ THẬT — thi công xong 2026-08-31, đối chiếu lại 2026-09-06.**
> `user-form-dialog.html:154` khai `[disabled]="saving()"`, và `onSubmit()` tự chặn khi đang gửi
> (`user-form-dialog.ts:252`, `if (this.saving()) return;`) thay vì chỉ dựa vào thuộc tính
> `disabled` của nút — vì `disabled` là **giao diện**, không phải bất biến của luồng: nút bị tắt
> vẫn có thể bị kích bằng phím Enter ở một số đường. Nút Huỷ **không** bị khoá, có chủ đích.
>
> *(🔄 LẬT 2026-09-06: trích dẫn cũ là `:137`, một dòng `<div class="form-error">` thuộc khối lỗi
> của ô `Roles` — không phải nút gửi.)*

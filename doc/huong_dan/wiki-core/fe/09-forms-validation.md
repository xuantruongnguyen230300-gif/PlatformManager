# 9. Form & Validation

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
nhanh. Submit vẫn phải xử lý được lỗi 400 (`fields`) trả về từ BE dù client
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
> đúng", đều phải hiển thị được (đúng cơ chế bind `fields` ở mục dưới). Khi
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

```ts
// vd validate email trùng lúc tạo user
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

## Bind lỗi từ `fields` vào form

```ts
private setFieldErrors(fields: Record<string, string[]>): void {
  for (const [key, messages] of Object.entries(fields)) {
    this.form.get(toCamelCase(key))?.setErrors({ server: messages[0] });
  }
}
```

Key từ BE là **PascalCase** (`MaxScore`) — form control Angular quy ước
thường `camelCase` (`maxScore`) — cần 1 hàm `toCamelCase` dùng chung ở
`core/`, không lặp lại logic map key ở từng form (đúng nguyên tắc "1 luật =
1 nguồn").

## Message hiển thị

Ưu tiên đọc thẳng `messages[0]` (đã là câu hoàn chỉnh do BE dịch qua
`ErrorDescriptor.Resolve`) — **không** tự ráp lại "Trường X: " + message,
vì `FieldError.Message` phía BE cố ý **không** chứa tên field (xem
`be/trien-khai/03-p2-platform-application.md` §4.5) — label field FE tự lấy
từ chính form (`<label>`), tránh 2 nơi cùng giữ tên field rồi lệch nhau khi
đổi copy.

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
  private readonly confirmDialog = viewChild.required(ConfirmDialogComponent);

  canDeactivate(): Observable<boolean> {
    return this.dirty() ? this.confirmDialog().open('Bạn có thay đổi chưa lưu. Rời khỏi trang?') : of(true);
  }

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

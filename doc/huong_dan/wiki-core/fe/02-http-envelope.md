---
kind: luat
scope: core
verified: 2026-09-06
---

# 2. HTTP Client & Envelope — tiêu thụ `IApiResult<T>` từ BE

## Vấn đề gốc

BE trả về đúng 1 envelope `IApiResult<T>` cho MỌI endpoint — **danh sách trường
đọc ở** `doc/huong_dan/quy-uoc/be-api-controller.md` §Envelope response, không
liệt kê lại ở đây (lý do ở §Model ngay dưới). Nếu FE tự đoán field hoặc dùng tên khác
(`Message`/`Success`/`ErrorMessage` — shape cũ), hậu quả không phải lỗi
biên dịch mà là **lỗi runtime âm thầm**: field `undefined`, message rỗng,
lỗi nghiệp vụ cụ thể bị thay bằng câu chung chung. Đây đúng là lỗi đã từng
xảy ra thật ở PlatformManager (interceptor đọc `body.Message` trong khi BE
trả `ErrorMessage`) trước khi BE đổi hẳn sang shape mới — không lặp lại nó.

## Model — file này KHÔNG định nghĩa envelope, chỉ nói cách đọc nó

> **Định nghĩa envelope — trường nào tồn tại, kiểu gì, khi nào có mặt — nằm ở đúng một
> chỗ:** [`../../quy-uoc/be-api-controller.md`](../../quy-uoc/be-api-controller.md)
> §Envelope response. Bản mirror sống thật của FE là **code**, không phải một danh sách
> chép trong tài liệu: `src/FE/src/app/core/http/api-result.model.ts`.
>
> *(Sửa 2026-09-04. Bản trước chép nguyên `interface IApiResult<T>` và union
> `ApiErrorCode` vào đây — và bản chép đó lệch **hai lần trong hai ngày**: thiếu
> `fieldErrors` (vào BE và vào model FE ngày 2026-09-03) rồi thiếu `MethodNotAllowed`
> (2026-09-04). Cả hai lần không gate nào bắt được, vì mỗi bản đọc riêng đều hợp lệ.
> Giải trình đầy đủ và luật áp cho mọi lần thêm trường về sau: file chủ, §"File chủ của
> 'hình dạng envelope'".)*

Ba điều dưới đây là thứ **FE sở hữu** khi tiêu thụ envelope — đây mới là nội dung của
file này, và chúng không lệch được vì không nơi nào khác nói về chúng:

- **`code` là chuỗi, không phải số** (khớp `[JsonConverter(JsonStringEnumConverter)]` phía
  BE) — đọc theo tên, không cast sang `number`. Tập giá trị hợp lệ đọc ở file chủ.
- **Casing thật của field JSON** (`data` hay `Data`) phải xác nhận bằng một lần gọi
  thật/Swagger trước khi code — đừng đoán theo cấu hình `System.Text.Json` mặc định.
- **Khoá của `fields` / `fieldErrors` giữ PascalCase**, cố ý khác phần còn lại của
  payload — xem §`fields` bên dưới.

> 📖 Tham số của câu thông báo (`messageParams` — ✅ **ĐANG DÙNG từ 2026-09-05**, đối chiếu
> 2026-09-06): hình dạng ở file chủ §`messageParams`; cách FE ráp tham số vào bảng dịch thuộc
> [`08-i18n.md`](08-i18n.md). Không mô tả trường đó ở file này.
>
> 🔄 LẬT 2026-09-06: bản trước gắn nhãn 🚧 *"chốt 2026-09-04, chưa thi công"*. Đã thi công:
> `ApiErrorMessageService.messageFor` (`src/FE/src/app/core/i18n/api-error-message.service.ts`)
> ráp `messageParams` vào câu lấy từ bảng dịch theo `businessCode`, và `fieldErrors[].code`
> đi cùng đường. Nơi tiêu thụ đầu tiên là màn đăng nhập.

## Tài nguyên tĩnh KHÔNG đi qua chuỗi interceptor — chốt 2026-09-05

**Luật: mọi request lấy file tĩnh (bảng dịch, JSON cấu hình, tài liệu trong `public/`) phải
dùng `HttpBackend`, không dùng `HttpClient`.**

`apiBaseUrlInterceptor` viết lại **mọi** URL tương đối thành `environment.apiBaseUrl + url`.
Đó là hành vi đúng cho lời gọi API, nhưng sai cho file tĩnh: `public/` được phục vụ ở **gốc
site**, không nằm sau `apiBaseUrl`. Quan sát thật trong `ng test`: request `/i18n/vi.json`
bay thành `.../api/i18n/vi.json` rồi **404**.

`HttpBackend` bỏ qua toàn bộ chuỗi interceptor, và điều đó đúng ngữ nghĩa cho cả hai
interceptor còn lại — không chỉ tránh được lỗi đường dẫn:

| Interceptor | Vì sao file tĩnh không nên đi qua |
|---|---|
| `apiBaseUrlInterceptor` | `public/` ở gốc site, không sau `apiBaseUrl` |
| CSRF / cookie phiên | Bảng dịch không cần danh tính; gửi kèm cookie là rò không cần thiết |
| `httpErrorInterceptor` | Một toast đỏ bắn ra **trước khi app kịp vẽ** là vô nghĩa với người dùng |

> **🔄 SỬA 2026-09-05 — dòng miễn trừ cũ là dòng chết.**
>
> `api-base-url.interceptor.ts` **từng** miễn trừ tiền tố `'/assets'` (đã gỡ, xác nhận lại
> 2026-09-06: thân interceptor nay chỉ còn một điều kiện — URL đã tuyệt đối thì giữ nguyên).
> Repo này **không dùng
> `/assets`** — nó dùng `public/`, ra thẳng gốc site. Nghĩa là điều kiện đó chưa bao giờ
> đúng với một request nào, và nó tạo ấn tượng sai rằng tài nguyên tĩnh "đã được lo".
> Bảng dịch né được là nhờ `useHttpBackend: true` của loader, **không** nhờ dòng miễn trừ.
>
> Đã gỡ dòng chết thay vì mở rộng nó thành danh sách tiền tố: một danh sách như vậy phải
> nhớ cập nhật mỗi lần thêm thư mục tĩnh mới, và nó hỏng **im lặng** khi ai đó quên —
> đúng loại danh sách `.claude/CLAUDE.md` §6 nói sẽ mục ruỗng. Luật "tĩnh thì dùng
> `HttpBackend`" không có danh sách nào để quên.

## Interceptor — 1 chỗ duy nhất dịch lỗi

> 🔄 LẬT 2026-09-06 — **khối dưới đây là KHUNG XƯƠNG, không phải bản sao của code đang chạy.**
> Bản trước đọc như một bản chép và đã lệch ở ba chỗ đủ để dạy sai:
>
> | Bản trước nói | Thực tế trong `src/FE/src/app/core/interceptors/http-error.interceptor.ts` |
> |---|---|
> | Kiểu `ApiHttpError` khai ở `core/http/api-http-error.ts` | **File đó chưa bao giờ tồn tại.** Kiểu thật tên `IHttpErrorWithApiResult`, khai trong `src/FE/src/app/core/http/api-result.model.ts` |
> | Câu toast = `body?.message ?? fallbackMessageForStatus(...)` | Đi qua `ApiErrorMessageService.messageFor/titleFor` (`src/FE/src/app/core/i18n/api-error-message.service.ts`) — hai bảng câu dự phòng theo mã HTTP đã CHUYỂN sang service đó ngày 2026-09-05 |
> | Interceptor chỉ hiện toast | Còn xử lý **401 phiên chết** (xoá state, điều hướng về `CORE_ROUTES.signIn` kèm `returnUrl`) và tôn trọng cờ `SKIP_ERROR_TOAST` |
>
> Đọc code khi cần bản đầy đủ; khối dưới chỉ giữ **hai bài học** mà mọi bản đều phải theo.

```ts
export const httpErrorInterceptor: HttpInterceptorFn = (req, next) => {
  const toast = inject(ToastService);          // ← LẤY Ở ĐÂY, xem cảnh báo (1)

  return next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      const body = (err.error ?? null) as IApiResult<unknown> | null;
      // ... quyết định toast / điều hướng ...
      return throwError(() => Object.assign(err, { apiResult: body }));  // ← KHÔNG spread, xem cảnh báo (2)
    }),
  );
};
```

> ### ⚠️ Hai lỗi trong bản trước — đều hỏng im lặng
>
> **(1) `inject()` không được gọi trong `catchError`.** Thân `HttpInterceptorFn` chạy
> trong injection context, nhưng callback của `catchError` chạy **bất đồng bộ, sau
> đó** — ngoài context. Kết quả: **`NG0203` lúc chạy**, không phải lỗi biên dịch.
> Phải `inject()` ở **thân interceptor**, trước `return next(req)`.
>
> **(2) `{ ...err }` phá prototype của `HttpErrorResponse`.** Spread tạo object
> literal thuần → mất prototype → `err instanceof HttpErrorResponse` ở **mọi** nơi
> phía sau trả `false`, và phép ép kiểu `(err as HttpErrorResponse)` trở thành ép
> kiểu dối: **build xanh, runtime sai**. Dùng `Object.assign(err, {...})` để giữ
> nguyên đối tượng gốc.
>
> *(Sửa 2026-08-23. Hai cảnh báo trên **vẫn đúng nguyên** với code hiện tại — đối chiếu
> 2026-09-06: mọi `inject()` đều nằm ở thân interceptor trước `return next(req)`, và
> `Object.assign(err, ...)` được giữ thay cho spread.)*

- Đọc **`message`**, không phải `Message`/`ErrorMessage`.
- Giữ nguyên `body` (đặt vào `apiResult`) để nơi gọi (thường là component
  form) tự đọc `fields`/`businessCode` khi cần xử lý riêng — interceptor chỉ
  lo phần chung (toast), không quyết định thay UI cụ thể.
- **Câu dự phòng theo mã HTTP** chỉ dùng khi response **không** có `body` hợp
  lệ (network lỗi, CORS chặn, ProblemDetails từ model-binding — trường hợp
  hiếm còn sót ở BE, xem `api-controller.md` ghi chú §Error → HTTP status
  mapping) — không phải đường chính.

  🔄 LẬT 2026-09-06: bản trước gọi nó là hàm `fallbackMessageForStatus` **của interceptor**.
  Từ 2026-09-05 hai bảng câu dự phòng (`messageFor` / `titleFor`) sống ở
  `src/FE/src/app/core/i18n/api-error-message.service.ts`. Lý do chuyển đáng nhớ: màn đăng nhập
  hiện lỗi ở **hai** kênh (toast + khối inline) và trước đó mỗi kênh có một bộ câu riêng — hai
  bộ câu cho cùng một sự kiện là hai bộ sẽ lệch nhau.

## Lỗi theo ô — đọc `fieldErrors`, KHÔNG đọc `fields`

**Luật (chốt 2026-09-06):** mọi form đọc **`fieldErrors`** và dịch từng phần tử qua
`ApiErrorMessageService.fieldMessage`. `fields` là trường **cũ, đang trên đường gỡ** — không
viết code mới đọc nó.

```ts
// trong component form
const errors = err.apiResult?.fieldErrors;          // Record<string, ApiFieldError[]>
if (errors) {
  this.form.setFieldErrors(
    Object.fromEntries(
      Object.entries(errors).map(([field, list]) => [
        field,
        list.map((e) => this.errorMessages.fieldMessage(e)).join(' '),
      ]),
    ),
  );
}
```

### Vì sao KHÔNG còn đọc `fields` được nữa — không phải chuyện gọn gàng

Hai trường **không mang cùng dữ liệu ở mọi nhánh**:

| Nhánh lỗi | `fields` | `fieldErrors` |
|---|---|---|
| 400 — validate đầu vào (FluentValidation) | ✅ có, câu để hiện thẳng | ✅ có, cùng bộ khoá, phần tử là **mã + câu** |
| Lỗi nghiệp vụ — mã Identity (`PasswordTooShort`…) | ❌ **để trống** | ✅ **chỉ trường này** được điền |

Vế thứ hai là chỗ chết người, và nó **cố ý**: nội dung ở đó là **mã** (`PasswordTooShort`), mà
`fields` thì mang chuỗi để hiện thẳng — đổ mã vào `fields` là đặt một định danh tiếng Anh xuống
dưới ô nhập của giao diện tiếng Việt.

**Hậu quả đo được cho màn nào còn đọc `fields`:** đổi mật khẩu quá ngắn → `fields` rỗng → không ô
nào được tô đỏ, người dùng chỉ thấy một câu chung ở đầu form, **dù BE đã nói chính xác ô nào
sai**. Không lỗi, không cảnh báo — form đơn giản là im lặng bỏ qua thông tin đã có sẵn trên dây.

### Casing của khoá — **không đổi**

Khoá của cả hai trường là **PascalCase**, khớp tên property C# gốc, cố ý khác casing camelCase của
phần còn lại trong payload. Component đọc `fieldErrors['MaxScore']`, không tự camelCase lại.

> ⚠️ Lựa chọn có chủ đích, **không phải bug**. BE giữ `DictionaryKeyPolicy` ở `null` và
> `NormalizeField` không đổi casing — xem
> `doc/huong_dan/quy-uoc/be-api-controller.md` §Envelope response. Ai "sửa cho nhất quán" ở phía
> BE sẽ làm gãy im lặng toàn bộ việc bind lỗi vào form.
>
> *(Sửa 2026-08-23: bản trước ghi "khớp property C# **đã serialize**" — sai, vì property đã
> serialize là camelCase.)*

### Trình tự gỡ `fields` — expand/contract, ba bước

Envelope là thứ **mọi** màn hình đi qua, nên đổi hình dạng một nhát tạo ra khoảnh khắc client cũ
gặp BE mới. Trình tự đã chốt ở docstring `IApiResult.FieldErrors`:

| Bước | Việc | Trạng thái |
|---|---|---|
| 1 | Thêm `fieldErrors` **cạnh** `fields` | ✅ 2026-09-03 |
| 2 | Mọi client chuyển sang đọc `fieldErrors` | ✅ **2026-09-06** |
| 3 | Gỡ `fields` ở **cả hai** phía | ⬜ chưa — lượt riêng, sau khi bước 2 chạy thật một thời gian |

Bước 3 **không** được gộp vào bước 2. Đó là điểm duy nhất của cả trình tự: giữ hai trường song
song đủ lâu để một client chưa kịp cập nhật không gãy.

Nghiệm thu bước 2 — lệnh phải in **rỗng**:

```bash
grep -rn "apiResult?.fields\|result?.fields\|\.fields" src/FE/src/app --include=*.ts | grep -v spec | grep -v "fieldErrors"
```

Chi tiết bind vào form ở [09-forms-validation.md](09-forms-validation.md).

## `businessCode` — so lỗi cụ thể, không so `message`

```ts
if (err.apiResult?.businessCode === 'PERMISSION.VERSION_CONFLICT') {
  // xử lý riêng (vd nạp lại rồi mời thử lại) — ổn định qua các bản dịch/đổi câu chữ UI
}
```

🔄 LẬT 2026-09-06: ví dụ cũ dùng `'CRITERIA.DUPLICATE_CODE'` — miền `CRITERIA` không còn màn
hình nào (module gỡ 2026-08-29). Ca thật đang chạy là tranh chấp ghi ở màn phân quyền:
`src/FE/src/app/platform/phan-quyen/pages/phan-quyen/phan-quyen.page.ts:52` so `businessCode`
là **chính**, `status === 409` chỉ là lưới đỡ thứ hai.

So `message` để rẽ nhánh logic là lỗi hay gặp: đổi 1 chữ trong câu thông báo
tiếng Việt (không phải lỗi nghiệp vụ) làm logic rẽ nhánh chết theo —
`businessCode` mới là hợp đồng ổn định giữa 2 phía.

## Service pattern — không đổi so với `api-client.md`

Envelope là chi tiết của tầng `core/`, không rò lên `services/` của feature:

```ts
list(params: IListParams): Observable<IPositionRow[]> {
  // Đường dẫn NGẮN, KHÔNG có `/api` — xem cảnh báo ngay dưới.
  return this.http.post<IApiResult<PositionDto[]>>('/positions/list', params)
    .pipe(map(res => unwrapData(res).map(mapPositionDtoToRow)));
}
```

> ### 🛑 Đường dẫn trong service KHÔNG được mang tiền tố `/api`
>
> 🔄 LẬT 2026-09-06 — cả ba khối mẫu trong file này trước đây viết `'/api/positions/...'`, và
> đó là **lỗi sinh ra code sai**: `apiBaseUrlInterceptor` ghép `environment.apiBaseUrl` vào
> trước mọi URL tương đối, mà giá trị đó **đã chứa `/api`** (`src/FE/src/environments/environment.ts`
> = `/api`). Chép mẫu cũ ⇒ request bay tới `/api/api/positions/...` ⇒ 404, và 404 đó trông
> hệt như "BE chưa làm endpoint".
>
> Mọi service thật đang theo đúng luật này — đối chiếu 2026-09-06:
>
> ```bash
> grep -rn "this.http\.\(get\|post\|put\|delete\)<" src/FE/src --include=*.ts | grep -v spec
> # PASS: không dòng nào chứa '/api/
> ```

Service unwrap `data`, map DTO → model, và **để lỗi bay lên** qua
`httpErrorInterceptor` — không tự `catchError` lặp lại logic dịch lỗi
(giống nguyên tắc BE: middleware toàn cục lo lỗi chung, chỗ cần ngữ cảnh cụ
thể — như bind `fields` vào form — mới tự xử lý thêm).

## Hủy request khi rời trang giữa chừng — `takeUntilDestroyed()`

> Bổ sung 2026-08-24, đối chiếu thực hành ngành cho hệ thống tầm trung: file
> này bàn kỹ cách **đọc** response nhưng chưa bàn thời điểm subscribe
> **không còn cần** — `HttpClient` trả về `Observable`, không tự huỷ theo
> vòng đời component.

User điều hướng sang trang khác (đổi route) trong lúc request cũ chưa về —
nếu component bị destroy mà `subscribe()` không được huỷ, hệ quả không chỉ
là request chạy phí công: callback `next()` vẫn chạy khi component đã biến
mất, và nếu nó ghi vào 1 store/service dùng chung (`providedIn: 'root'`),
response **đến trễ của trang cũ** có thể ghi đè state mà trang mới vừa load
xong — race condition không có lỗi biên dịch, không có test đỏ, chỉ lộ ra
khi mạng chậm đúng lúc user thao tác nhanh.

```ts
export class PositionDetailPage {
  private readonly destroyRef = inject(DestroyRef);
  private readonly service = inject(PositionService);
  protected readonly item = signal<IPositionRow | null>(null);

  ngOnInit(): void {
    this.service.getById(this.route.snapshot.params['id'])
      .pipe(takeUntilDestroyed(this.destroyRef))   // huỷ subscribe khi component destroy
      .subscribe(item => this.item.set(item));
  }
}
```

`takeUntilDestroyed()` gọi **không tham số** chỉ hợp lệ trong injection
context (constructor, field initializer) — gọi trong `ngOnInit()` như trên
**bắt buộc** truyền `DestroyRef` tường minh, cùng dạng bẫy với `inject()`
trong `catchError` đã nêu ở trên: thiếu injection context không lỗi biên
dịch, chỉ sai lúc chạy. Với luồng master-detail (đổi tham số route liên tục,
vd click nhanh qua nhiều dòng danh sách), ưu tiên `switchMap` trên
`Observable` của route param thay vì gọi lại `subscribe()` thủ công mỗi lần
— `switchMap` tự huỷ request trước đó khi có request mới, không cần đợi
component destroy.

> ### ✅ Vi phạm đã đóng — thi công xong 2026-08-31
>
> Đoạn trên nói *"luồng master-detail"*, nhưng luật áp rộng hơn thế: **mọi** đường
> tải lại theo tham số người dùng đổi được đều cần nó. Danh sách người dùng **từng** là một
> ca như vậy — nó dùng `.subscribe()` trần. Hệ quả đo được lúc đó:
>
> ```
> Gõ "ngu"     → request A rời client
> Gõ "nguyen"  → request B rời client
> B về trước   → bảng hiện kết quả "nguyen"  ✓
> A về sau     → bảng hiện kết quả "ngu"     ✗   ô tìm kiếm vẫn ghi "nguyen"
> ```
>
> Debounce 300ms **giảm** tần suất chứ không loại bỏ — chỉ cần mạng giật một nhịp.
> Không có gì báo cho người dùng biết bảng đang hiện kết quả của câu hỏi khác.
>
> **Đã đổi sang `switchMap` ngày 2026-08-31, xác nhận lại 2026-09-06** — mọi lượt tải (cả
> `effect()` theo URL lẫn `reload()`) đi qua một `Subject` chung
> (`src/FE/src/app/platform/quan-tri-nguoi-dung/pages/quan-tri-nguoi-dung/quan-tri-nguoi-dung.page.ts:251`).
>
> Một bẫy phát hiện lúc thi công, ghi lại vì nó không hiển nhiên: `catchError` phải nằm
> **bên trong** inner observable. Đặt ngoài thì một lỗi lọt qua `switchMap` sẽ **giết luôn
> dòng chảy**, và từ đó mọi lượt tải sau im lặng không chạy — hỏng nặng hơn hẳn thứ đang
> đi sửa.
>
> Nghiệm thu: bóp băng thông xuống mức rất chậm, gõ nhanh hai chuỗi khác nhau vào ô
> tìm kiếm ⇒ bảng cuối cùng **luôn** khớp chuỗi cuối cùng trong ô.

## Retry khi lỗi mạng tạm thời — không retry lỗi nghiệp vụ

> Bổ sung 2026-08-24, đối chiếu thực hành ngành cho hệ thống tầm trung: mất
> mạng thoáng qua (wifi chập chờn, chuyển từ wifi sang 4G) là lỗi **khác bản
> chất** với lỗi 4xx/5xx mà interceptor ở trên đang dịch — nhầm 2 loại này
> làm interceptor hoặc retry sai chỗ: retry mãi 1 lỗi 403 (không bao giờ hết
> lỗi), hoặc không retry 1 lỗi mạng đáng lẽ tự khỏi sau 1 giây.

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG (đối chiếu 2026-09-10).** Toán tử dưới đây chưa được viết.
> Tiêu chí PASS: `grep -rn "retryTransient" src/FE/src` trả **0 dòng**. Muốn biết `core/http/`
> hiện có gì thì `ls src/FE/src/app/core/http/` — đừng chép danh sách vào đây
> (`.claude/CLAUDE.md` §6). Bản trước liệt kê tay 5 file và danh sách đó mục ruỗng đúng
> **cùng ngày** `server-field-errors.ts` được nâng lên thư mục này, trong khi lệnh `grep` ngay
> bên cạnh — thứ THẬT SỰ chứng minh điều đoạn văn muốn nói — vẫn đúng nguyên.

```ts
// core/http/retry-transient.operator.ts  (chưa tồn tại — đích đến)
import { retry, throwError, timer } from 'rxjs';

export function retryTransient<T>() {
  return retry<T>({
    count: 2,
    delay: (error: HttpErrorResponse, retryCount) => {
      if (error.status !== 0) return throwError(() => error);  // 4xx/5xx thật: fail ngay, KHÔNG thử lại
      return timer(retryCount * 500);                           // status 0 = network lỗi: backoff 500ms, 1000ms
    },
  });
}
```

`error.status === 0` là tín hiệu đáng tin cho "request chưa từng tới được
server" (mất mạng, DNS lỗi, CORS preflight chặn) — **không** phải kết quả
nghiệp vụ nào cả, nên retry an toàn. Mọi status khác (kể cả 5xx) là response
**có thật** từ server — retry mù có thể lặp lại đúng lỗi (server đang lỗi
thật, không tự khỏi) hoặc tệ hơn, nếu áp cho `POST` ghi dữ liệu, retry sau
khi request đầu đã xử lý xong ở server (chỉ mất response) sẽ ghi trùng —
đây chính là lý do BE phải có `Idempotency-Key` cho endpoint ghi
(`doc/huong_dan/wiki-core/be/09-security-beyond-auth.md`). Vì vậy
`retryTransient()` chỉ áp cho request **đọc** (`GET`, tự nhiên idempotent):

```ts
list(): Observable<IPositionRow[]> {
  return this.http.get<IApiResult<PositionDto[]>>('/positions')
    .pipe(retryTransient(), map(res => unwrapData(res).map(mapPositionDtoToRow)));
}
```

## Double-submit khi bấm nút Save 2 lần liên tiếp

> Bổ sung 2026-08-24, đối chiếu thực hành ngành cho hệ thống tầm trung: đây
> là lớp phòng thủ ở **FE**, bổ sung cho `Idempotency-Key` ở BE
> (`be/09-security-beyond-auth.md`) chứ không thay thế — chặn ở UI rẻ hơn
> nhiều (không cần round-trip) và chặn được cả những double-click không bao
> giờ chạm tới tầng HTTP nếu chặn đúng chỗ.

```ts
protected readonly saving = signal(false);

save(): void {
  if (this.saving()) return;      // chặn ngay cả khi [disabled] chưa kịp render lại DOM
  this.saving.set(true);
  this.service.create(this.form.getRawValue())
    .pipe(finalize(() => this.saving.set(false)))
    .subscribe({
      next: () => this.router.navigate(['..']),
      error: () => { /* toast đã lo ở interceptor, chỉ cần reset saving */ },
    });
}
```

```html
<button [disabled]="saving()" (click)="save()">Lưu</button>
```

`[disabled]` trên template **không đủ một mình**: binding chỉ cập nhật DOM ở
lần change detection kế tiếp, còn double-click thật (2 lần bấm cách nhau vài
chục mili-giây) có thể xảy ra trước khi Angular kịp re-render nút. Guard
`if (this.saving()) return;` ở đầu hàm mới là lớp chặn thật — set `saving`
**trước** khi gọi API (không đợi vào `next`), và luôn reset qua `finalize()`
để không kẹt nút vĩnh viễn khi lỗi.

## Upload file lớn — progress thật, không phải spinner mù

> Bổ sung 2026-08-24, đối chiếu thực hành ngành cho hệ thống tầm trung: màn
> import CSV/Excel gọi `HttpClient.post()` mặc định **không** phát sự kiện
> nào cho tới khi cả file upload xong — với file vài MB qua mạng chậm, user
> nhìn spinner đứng yên nhiều giây không biết có đang chạy hay đã treo.

```ts
import { HttpEventType } from '@angular/common/http';

upload(file: File): Observable<number> {
  const formData = new FormData();
  formData.append('file', file);

  return this.http.post<IApiResult<ImportResultDto>>('/positions/import', formData, {
    reportProgress: true,
    observe: 'events',
  }).pipe(
    filter(e => e.type === HttpEventType.UploadProgress || e.type === HttpEventType.Response),
    map(e => e.type === HttpEventType.UploadProgress
      ? Math.round((100 * e.loaded) / (e.total ?? e.loaded))   // total có thể undefined — fallback tránh chia lỗi
      : 100),
  );
}
```

Lưu ý bắt buộc khi dùng cho import: đây là % của **upload** (đẩy file lên
server), không phải % **xử lý** (server parse CSV, validate từng dòng, ghi
DB) — 2 giai đoạn có thể lệch xa nhau về thời gian (upload 2MB mất 1 giây,
nhưng validate 10.000 dòng mất 30 giây sau đó). `UploadProgress` không phủ
được giai đoạn xử lý; cần cơ chế riêng (polling job status hoặc SignalR) nếu
muốn hiện tiến trình xử lý thật — nếu chỉ dùng `UploadProgress` một mình,
UI nên chuyển sang trạng thái "đang xử lý..." (không còn %) ngay sau khi
progress chạm 100, tránh hiểu lầm 100% nghĩa là đã xong.

# 2. HTTP Client & Envelope — tiêu thụ `IApiResult<T>` từ BE

## Vấn đề gốc

BE trả về đúng 1 envelope cho MỌI endpoint —
`IApiResult<T> { data, message, status, code, businessCode, traceId,
retryable, fields }` (xem `doc/huong_dan/quy-uoc/be-api-controller.md`
§Envelope response). Nếu FE tự đoán field hoặc dùng tên khác
(`Message`/`Success`/`ErrorMessage` — shape cũ), hậu quả không phải lỗi
biên dịch mà là **lỗi runtime âm thầm**: field `undefined`, message rỗng,
lỗi nghiệp vụ cụ thể bị thay bằng câu chung chung. Đây đúng là lỗi đã từng
xảy ra thật ở PlatformManager (interceptor đọc `body.Message` trong khi BE
trả `ErrorMessage`) trước khi BE đổi hẳn sang shape mới — không lặp lại nó.

## Model — khớp 1:1 với BE

```ts
// core/http/api-result.model.ts
export type ApiResultStatus = 'SUCCESS' | 'VALIDATION_ERROR' | 'BUSINESS_ERROR' | 'SYSTEM_ERROR';

export type ApiErrorCode =
  | 'Success' | 'ValidationError' | 'AuthenticationError' | 'AuthorizationError'
  | 'NotFound' | 'Conflict' | 'BusinessRuleError' | 'TooManyRequests' | 'SystemError';

export interface IApiResult<T> {
  data: T | null;
  message: string | null;
  status: ApiResultStatus;
  code: ApiErrorCode;
  businessCode: string | null;
  traceId: string | null;
  retryable: boolean | null;
  fields: Record<string, string[]> | null;
}
```

`code` là chuỗi (khớp `[JsonConverter(JsonStringEnumConverter)]` phía BE),
**không phải số** — đọc theo tên, không cast sang `number`. Casing thật của
field JSON (`data` hay `Data`) phải xác nhận bằng 1 lần gọi thật/Swagger
trước khi code — đừng đoán theo cấu hình `System.Text.Json` mặc định.

## Interceptor — 1 chỗ duy nhất dịch lỗi

```ts
// core/http/api-http-error.ts — để nơi gọi không phải ép kiểu bằng tay
export type ApiHttpError = HttpErrorResponse & { apiResult: IApiResult<unknown> | null };

// core/interceptors/http-error.interceptor.ts
export const httpErrorInterceptor: HttpInterceptorFn = (req, next) => {
  const toast = inject(ToastService);          // ← LẤY Ở ĐÂY, xem cảnh báo (1)

  return next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      const body = err.error as IApiResult<unknown> | null;
      toast.error(body?.message ?? fallbackMessageForStatus(err.status));

      Object.assign(err, { apiResult: body });  // ← KHÔNG spread, xem cảnh báo (2)
      return throwError(() => err as ApiHttpError);
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
> *(Sửa 2026-08-23. Đây là đoạn sẽ được chép nguyên si vào
> `core/interceptors/http-error.interceptor.ts` ở bước F0 — tức file đầu tiên khi
> dựng lại app.)*

- Đọc **`message`**, không phải `Message`/`ErrorMessage`.
- Giữ nguyên `body` (đặt vào `apiResult`) để nơi gọi (thường là component
  form) tự đọc `fields`/`businessCode` khi cần xử lý riêng — interceptor chỉ
  lo phần chung (toast), không quyết định thay UI cụ thể.
- `fallbackMessageForStatus` chỉ dùng khi response **không** có `body` hợp
  lệ (network lỗi, CORS chặn, ProblemDetails từ model-binding — trường hợp
  hiếm còn sót ở BE, xem `api-controller.md` ghi chú §Error → HTTP status
  mapping) — không phải đường chính.

## `fields` — bind vào form, không gộp chung toast

```ts
// trong component form
this.service.create(payload).subscribe({
  error: (err) => {
    const fields = err.apiResult?.fields as Record<string, string[]> | undefined;
    if (fields) this.form.setFieldErrors(fields);   // bind từng ô, KHÔNG chỉ toast chung
  },
});
```

Key của `fields` là **PascalCase**, khớp **tên property C# gốc** — **cố ý khác**
casing của phần còn lại trong payload, vốn là camelCase. Component đọc
`fields['MaxScore']`, không tự lowercase/camelCase lại một cách ngầm định.

> ⚠️ Đây là lựa chọn có chủ đích, **không phải bug**. BE giữ `DictionaryKeyPolicy`
> ở `null` và `NormalizeField` không đổi casing — xem
> `doc/huong_dan/quy-uoc/be-api-controller.md` §Envelope response. Ai đó "sửa cho
> nhất quán" ở phía BE sẽ làm gãy im lặng toàn bộ việc bind lỗi vào form.
>
> *(Sửa 2026-08-23: bản trước ghi "khớp property C# **đã serialize**" — sai, vì
> property đã serialize là camelCase.)*

Chi tiết bind vào form ở [09-forms-validation.md](09-forms-validation.md).

## `businessCode` — so lỗi cụ thể, không so `message`

```ts
if (err.apiResult?.businessCode === 'CRITERIA.DUPLICATE_CODE') {
  // xử lý riêng (vd gợi ý đổi mã) — ổn định qua các bản dịch/đổi câu chữ UI
}
```

So `message` để rẽ nhánh logic là lỗi hay gặp: đổi 1 chữ trong câu thông báo
tiếng Việt (không phải lỗi nghiệp vụ) làm logic rẽ nhánh chết theo —
`businessCode` mới là hợp đồng ổn định giữa 2 phía.

## Service pattern — không đổi so với `api-client.md`

Envelope là chi tiết của tầng `core/`, không rò lên `services/` của feature:

```ts
list(params: IListParams): Observable<IPositionRow[]> {
  return this.http.post<IApiResult<PositionDto[]>>('/api/positions/list', params)
    .pipe(map(res => (res.data ?? []).map(mapPositionDtoToRow)));
}
```

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

## Retry khi lỗi mạng tạm thời — không retry lỗi nghiệp vụ

> Bổ sung 2026-08-24, đối chiếu thực hành ngành cho hệ thống tầm trung: mất
> mạng thoáng qua (wifi chập chờn, chuyển từ wifi sang 4G) là lỗi **khác bản
> chất** với lỗi 4xx/5xx mà interceptor ở trên đang dịch — nhầm 2 loại này
> làm interceptor hoặc retry sai chỗ: retry mãi 1 lỗi 403 (không bao giờ hết
> lỗi), hoặc không retry 1 lỗi mạng đáng lẽ tự khỏi sau 1 giây.

```ts
// core/http/retry-transient.operator.ts
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
  return this.http.get<IApiResult<PositionDto[]>>('/api/positions')
    .pipe(retryTransient(), map(res => (res.data ?? []).map(mapPositionDtoToRow)));
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

  return this.http.post<IApiResult<ImportResultDto>>('/api/positions/import', formData, {
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

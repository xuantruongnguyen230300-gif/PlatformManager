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

# F0 — Nền móng

> **Định nghĩa hoàn thành:** `ng build` xanh trên app zoneless mới; cây thư
> mục đúng 4 tầng; gọi **một endpoint cố tình trả lỗi nghiệp vụ** từ BE thật
> (hoặc mock) → toast hiện đúng `message` của BE, **không** phải chuỗi rỗng
> hay `undefined`; và có ít nhất 1 test interceptor đã **từng đỏ** trước khi
> code chạy đúng.

## 1. Scaffold

```bash
ng new PlatformManager --style=scss --ssr=false --routing
```

Bật **zoneless** ngay từ đầu trong `app.config.ts`
(`provideZonelessChangeDetection()`) — không để `zone.js` rồi gỡ sau. Gỡ sau
nghĩa là mọi component viết trong lúc còn zone đều chưa được kiểm chứng dưới
chế độ zoneless, và lỗi lộ ra rải rác chứ không tập trung.

> 📖 Cây thư mục 4 tầng (`core/ shared/ platform/ modules/`) và cấu trúc bên
> trong một feature: [`../../../quy-uoc/fe-architecture.md`](../../../quy-uoc/fe-architecture.md)
> §Tầng app. Tạo sẵn 4 thư mục rỗng ở bước này để không ai "tạm để đây rồi
> chuyển sau".

## 2. `core/http` — envelope là thứ viết trước tiên

Mọi service sau này đều đi qua đây, nên sai ở đây là sai lan ra toàn app.

| File | Việc |
| --- | --- |
| `core/http/api-result.model.ts` | `IApiResult<T>` đủ **8 field**, khớp 1:1 BE |
| `core/http/api-http-error.ts` | Kiểu `ApiHttpError` để nơi gọi khỏi ép kiểu tay |
| `core/interceptors/http-error.interceptor.ts` | Dịch lỗi → toast, gắn `apiResult` vào error |
| `core/interceptors/with-credentials.interceptor.ts` | Gửi cookie session kèm mọi request |
| `core/services/toast.service.ts` | Nơi duy nhất hiện thông báo lỗi chung |

> 📖 Định nghĩa `IApiResult<T>` và **bản interceptor đúng** (đã vá 2 lỗi
> hỏng-im-lặng: `inject()` ngoài injection context, và spread phá prototype
> `HttpErrorResponse`): [`../02-http-envelope.md`](../02-http-envelope.md).
> Chép nguyên si từ đó, đừng viết lại từ trí nhớ.
>
> 📖 `withCredentials` và điều kiện CORS phía BE:
> [`../07-auth-identity.md`](../07-auth-identity.md).

## 3. Thứ tự viết

```
1. IApiResult<T> — thuần khai báo type                        30 phút
        │
        ▼
2. httpErrorInterceptor + ToastService                        nửa ngày
        │
        ▼
3. Test interceptor — CHO NÓ ĐỎ TRƯỚC                          1 giờ
   (assert theo shape sai, chạy thấy fail, rồi sửa cho xanh)
        │
        ▼
4. withCredentialsInterceptor + đăng ký cả 2 trong app.config  30 phút
```

Bước 3 không được bỏ. Một test viết xong **xanh ngay từ đầu** không chứng
minh được gì — nó có thể đang xanh vì assert sai chứ không vì code đúng.

## Kiểm chứng

- [ ] `ng build` xanh, `app.config.ts` có `provideZonelessChangeDetection()`
- [ ] `IApiResult<T>` đủ 8 field, tên khớp field JSON **thật** — xác nhận
      bằng 1 lần gọi thật hoặc Swagger, **không đoán** theo cấu hình mặc định
- [ ] Test interceptor đã kiểm chứng đỏ→xanh, không chỉ xanh sẵn
- [ ] `fields` bind được vào ít nhất 1 form thử (không chỉ toast) — xác nhận
      key **PascalCase** đọc đúng, xem `../02-http-envelope.md` §`fields`
- [ ] 4 thư mục `core/ shared/ platform/ modules/` đã tồn tại

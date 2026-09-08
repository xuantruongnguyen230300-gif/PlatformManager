---
kind: luat
scope: core
verified: 2026-09-06
---

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
> §Tầng app.
>
> 🔄 **LẬT 2026-09-06** — bản trước dặn *"tạo sẵn 4 thư mục rỗng ở bước này"*. Không làm được:
> git không theo dõi thư mục rỗng, nên `modules/` biến mất khỏi bản clone ngay lần kế tiếp và
> lời dặn tự sinh ra một checklist không bao giờ tick đúng. Tầng nào chưa có file thì chưa có
> thư mục — `src/FE/src/app/` hôm nay có `core/ shared/ platform/`, không có `modules/`.

## 2. `core/http` — envelope là thứ viết trước tiên

Mọi service sau này đều đi qua đây, nên sai ở đây là sai lan ra toàn app.

| File | Việc |
| --- | --- |
| `core/http/api-result.model.ts` | `IApiResult<T>` khớp 1:1 BE — **8 field bắt buộc** (`data` `message` `status` `code` `businessCode` `traceId` `retryable` `fields`) + **2 field tuỳ chọn** (`fieldErrors` `messageParams`) |
| `core/http/api-result.model.ts` | Chứa luôn `IHttpErrorWithApiResult` — nơi gọi khỏi ép kiểu tay |
| `core/interceptors/api-base-url.interceptor.ts` | Gắn domain/port của API vào URL tương đối — chạy **trước** hai cái dưới |
| `core/interceptors/credentials.interceptor.ts` | Gửi cookie session kèm mọi request (hàm `withCredentialsInterceptor`) |
| `core/interceptors/http-error.interceptor.ts` | Dịch lỗi → toast, gắn `apiResult` vào error — đăng ký **cuối** |
| `core/toast/toast.service.ts` | Nơi duy nhất hiện thông báo lỗi chung |

> Sửa 2026-08-27: bảng này từng lệch 3 dòng. `core/toast/toast.service.ts` là
> vị trí đúng đã chốt ở [`05-gate.md`](05-gate.md) §G9 (bản trước ghi
> `core/services/`). Hai dòng kia đổi theo tên file thật — khác biệt thuần đặt
> tên, không phải quyết định kiến trúc.
>
> 🔄 **LẬT 2026-09-06** — hai chỗ nữa trong bảng đã lệch so với `src/FE`:
> (1) envelope ghi *"đủ **8 field**"*, nhưng BE đã thêm `fieldErrors` và `messageParams`
> (`src/FE/src/app/core/http/api-result.model.ts:98`, `:124`) nên hình dạng thật là **8 bắt
> buộc + 2 tuỳ chọn**; (2) bảng thiếu hẳn `apiBaseUrlInterceptor`, trong khi
> `src/FE/src/app/app.config.ts:155` đăng ký **ba** interceptor và **thứ tự** giữa chúng là
> ràng buộc thật, không phải chi tiết.

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
- [ ] `IApiResult<T>` đủ 8 field bắt buộc + 2 tuỳ chọn, tên khớp field JSON **thật** — xác nhận
      bằng 1 lần gọi thật hoặc Swagger, **không đoán** theo cấu hình mặc định
- [ ] Test interceptor đã kiểm chứng đỏ→xanh, không chỉ xanh sẵn
- [ ] `fields` bind được vào ít nhất 1 form thử (không chỉ toast) — xác nhận
      key **PascalCase** đọc đúng, xem `../02-http-envelope.md` §`fields`
- [ ] 3 thư mục `core/ shared/ platform/` đã tồn tại (`modules/` dựng cùng module nghiệp vụ đầu tiên — hôm nay chưa tồn tại, xem [00-lo-trinh-tong-the.md](00-lo-trinh-tong-the.md) §Phạm vi)

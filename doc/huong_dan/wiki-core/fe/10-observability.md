# 10. Observability phía client

## `traceId` — cầu nối log FE ↔ log BE

Mọi response lỗi từ BE đều có `traceId` trong `IApiResult<T>` (xem
[02-http-envelope.md](02-http-envelope.md)). Khi hiển thị lỗi hệ thống
(`SYSTEM_ERROR`), **hiện `traceId` cho user** (dạng nhỏ, copy được) — đây là
cách duy nhất để support tra đúng log phía server khi user báo lỗi, không
phải đoán theo thời gian/màn hình.

```ts
// toast/dialog lỗi hệ thống
`Đã có lỗi xảy ra. Mã tra cứu: ${apiResult.traceId}`
```

## Log console — chỉ dev, không production

`console.error` cho lỗi không mong đợi **chỉ** bật ở `environment.development.ts`
(`environment.production: false`) — production build không log chi tiết lỗi
ra console (tránh lộ traceId/stack ra người dùng cuối tò mò mở DevTools, dù
đây không phải bí mật nhạy cảm, vẫn nên tối giản bề mặt lộ thông tin).

## Đã tới ngưỡng — Nhóm B trước đây, giờ nên làm

**Cập nhật (2026-08-17):** mục này viết "hoãn tới khi chuẩn bị production
thật" — PlatformManager giờ đã ở đúng giai đoạn đó (chuyển từ demo sang phát
triển product, xem
[be/01-core-components.md](../be/01-core-components.md) §Áp dụng). Áp dụng
lại đúng ngưỡng đã tự đặt ra:

- **Gửi lỗi client-side lên dịch vụ tracking (Sentry hoặc tương đương) — nên
  làm sớm, không còn "chưa cần khi demo/nội bộ".** Không có cơ chế này thì
  lỗi JS runtime ở máy user thật (khác máy dev) không ai biết đã xảy ra, chỉ
  phát hiện khi user tự báo — chậm hơn nhiều so với alert tự động.

## Vẫn hoãn — bằng chứng chưa đổi, không phải giai đoạn

- **Metrics hiệu năng** (Core Web Vitals, thời gian load route) — vẫn chưa
  có nhiều người dùng đồng thời để số liệu có ý nghĩa thống kê. Khác Sentry
  ở trên: đây hoãn vì **thiếu bằng chứng traffic**, không phải vì "còn demo"
  — khi có đủ user đồng thời mới bật, không phải khi "đã là product".

Ghi ngưỡng ở đây để không quên — không xây trước khi chạm đúng nỗi đau,
đúng nguyên tắc Nhóm A/B xuyên suốt cả bộ tài liệu.

## Global error handler — lỗi runtime JS không qua HTTP interceptor

> Bổ sung 2026-08-24, đối chiếu thực hành ngành cho hệ thống tầm trung: mục
> `traceId` ở đầu file chỉ phủ được lỗi đi qua `HttpClient` — interceptor bắt
> `HttpErrorResponse`, không thấy được gì ngoài phạm vi HTTP. Lỗi runtime
> thuần (ném trong template binding, trong `computed()`/lifecycle hook — vd
> `ngOnInit` gọi `undefined.property`) **không đi qua HTTP**, nên
> interceptor không bắt được. Không có `ErrorHandler` riêng thì lớp lỗi này
> rơi thẳng vào console DevTools của user, không ai phía dev biết nó đã xảy
> ra — khác hẳn lỗi API, vốn ít nhất còn có log phía BE.

Angular tách 2 nguồn lỗi runtime, cần đăng ký cả hai nhưng cùng trỏ về 1 chỗ
xử lý — không phải 2 đường báo lỗi song song:

- **Lỗi Angular tự bắt được** (trong change detection, template, lifecycle
  hook) — đi qua class `ErrorHandler` đăng ký trong DI. Ghi đè `handleError`
  để xử lý tại đây.
- **Lỗi ném ngoài vùng Angular theo dõi** (`setTimeout` thô, event listener
  DOM gắn tay, promise reject không `await`) — cần
  `provideBrowserGlobalErrorListeners()` gắn listener `error`/
  `unhandledrejection` lên `window`, **forward vào chính `ErrorHandler`** ở
  trên thay vì xử lý riêng.

```ts
// app.config.ts
import { ApplicationConfig, ErrorHandler, provideBrowserGlobalErrorListeners } from '@angular/core';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    { provide: ErrorHandler, useClass: GlobalErrorHandler },
    // ... các provider khác
  ],
};
```

```ts
// core/errors/global-error-handler.ts
@Injectable()
export class GlobalErrorHandler implements ErrorHandler {
  handleError(error: unknown): void {
    if (!environment.production) console.error(error);   // xem §"Log console" trên
    // gửi report — xem mục "Gửi lỗi FE về đâu" ngay dưới
  }
}
```

## Gửi lỗi FE về đâu — không dừng ở toast cho user

> Bổ sung 2026-08-24, đối chiếu thực hành ngành cho hệ thống tầm trung: mục
> "Đã tới ngưỡng" ở trên chốt **làm gì** (gửi lỗi client-side lên dịch vụ
> tracking) nhưng chưa nói **nối vào đâu** — chính là `ErrorHandler` vừa
> đăng ký ở trên, không phải một cơ chế báo lỗi riêng. Toast báo "đã có lỗi
> xảy ra" xong là hết việc của UI, nhưng nếu dừng ở đó thì không ai phía
> dev/support biết lỗi vừa xảy ra trừ khi user tự report — chậm hơn nhiều
> so với alert tự động, đúng khoảng trống mà Sentry được chọn để lấp ở trên.

Bắt đầu bằng class tự viết ở mục trên (không phụ thuộc vendor, endpoint tự
dựng); khi có ngân sách cho Sentry, chỉ cần đổi provider — không phải sửa
chỗ nào khác gọi `ErrorHandler`, vì cả 2 cùng implement chung 1 token:

```ts
// app.config.ts — thay ErrorHandler tay bằng handler của Sentry khi đã có ngân sách
import { createErrorHandler } from '@sentry/angular';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    { provide: ErrorHandler, useValue: createErrorHandler({ logErrors: !environment.production }) },
  ],
};
```

Context bắt buộc đính kèm mỗi báo cáo — thiếu context thì dev nhận đúng 1
dòng "Error: undefined is not an object", không tra được gì:

- **`traceId`** của lời gọi API gần nhất nếu lỗi xảy ra ngay sau 1 request
  lỗi — cùng giá trị đã hiện cho user ở mục `traceId` trên, nối 2 đầu log
  FE/BE qua đúng 1 mã thay vì đoán theo thời gian.
- URL route hiện tại (`Router.url`) — lỗi runtime thường gắn với 1 màn hình
  cụ thể, không phải ngẫu nhiên.
- Định danh user (username/role đủ dùng để hỏi lại "đang thao tác gì", không
  cần thông tin nhạy cảm hơn).

Chưa có ngân sách cho Sentry hay dịch vụ tương đương? Tối thiểu vẫn cần 1
endpoint nhận báo cáo lỗi runtime — dựng nhẹ hơn nhiều so với "chưa làm gì":
`POST /api/client-errors` nhận `{ message, stack, url, traceId, userId }` rồi
ghi log Serilog phía BE (xem [be/07-observability.md](../be/07-observability.md))
— không cần dashboard riêng, chỉ cần lỗi runtime **có nơi để đi** thay vì
chết trong console máy user.

## RUM (Real User Monitoring) — khác Lighthouse, đo trải nghiệm THẬT

> Bổ sung 2026-08-24, đối chiếu thực hành ngành cho hệ thống tầm trung: mục
> "Vẫn hoãn" ở trên nêu đúng bằng chứng còn thiếu (chưa đủ traffic đồng
> thời), nhưng chưa giải thích RUM khác gì Lighthouse — bổ sung khái niệm để
> khi ngưỡng traffic tới, không phải tra cứu lại từ đầu.

Lighthouse/Core Web Vitals đo lúc dev (`ng build` + audit, hoặc DevTools) đo
trên **1 máy, 1 lần, mạng giả lập** — gọi là *lab data*. Nó không thấy được
máy user thật cấu hình yếu, mạng 3G vùng xa, hay tab bị treo vì mở 30 tab
khác cùng lúc. **RUM đo chính những phiên thật đó**: mỗi lần user thật load
app, trình duyệt tự tính `LCP`/`INP`/`CLS` cho phiên đó rồi gửi mẫu về server
để tổng hợp (dùng P75, không phải trung bình — 1 phiên chậm bất thường không
kéo méo số liệu tổng).

**Công cụ phù hợp quy mô tầm trung:** nếu đã chọn Sentry cho error tracking ở
mục trên, **không cần thêm công cụ RUM riêng** — bật
`browserTracingIntegration()` của `@sentry/angular` thì Web Vitals của mọi
phiên thật được thu thập tự động, hiện chung dashboard với lỗi runtime (1
request lỗi thường đi kèm 1 phiên có `LCP`/`INP` xấu — tra chung 1 chỗ dễ hơn
2 dashboard tách rời). Chỉ cần công cụ độc lập (npm package `web-vitals` của
Google, gửi thẳng về endpoint tự dựng) khi **chưa** dùng Sentry cho error
tracking — tự dựng riêng chỉ cho 3 con số này không đáng công so với bật
thêm đúng 1 dòng cấu hình trên SDK đã có sẵn.

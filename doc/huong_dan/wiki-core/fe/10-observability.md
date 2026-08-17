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

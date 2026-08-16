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

## Chưa cần (Nhóm B — hoãn tới khi chuẩn bị production thật)

- Gửi lỗi client-side (uncaught exception JS) lên 1 dịch vụ tracking
  (Sentry hoặc tương đương) — chưa cần khi còn demo/nội bộ.
- Metrics hiệu năng (Core Web Vitals, thời gian load route) — chưa có nhiều
  người dùng đồng thời để việc này có ý nghĩa.

Ghi ngưỡng ở đây để không quên — không xây trước khi chạm đúng nỗi đau,
đúng nguyên tắc Nhóm A/B xuyên suốt cả bộ tài liệu.

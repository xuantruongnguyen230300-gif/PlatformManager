# 7. Quan sát hệ thống (Observability) — vượt ra ngoài logging

`TraceId` trong envelope response (đã có ở [01-core-components.md](01-core-components.md), #6) là bước đầu — nhưng để **thật sự tra được** "request này đi qua bao nhiêu module, chỗ nào chậm, chỗ nào lỗi" khi hệ thống lớn dần, cần thêm:

- **Health check endpoint** (`/health`) — không chỉ "app còn sống" mà kiểm tra được cả dependency (DB, cache, service ngoài) — dùng để biết sớm khi 1 phần hạ tầng có vấn đề, trước khi user báo lỗi.
- **Correlation ID xuyên suốt** — nếu có nhiều Process (xem [02-identity-auth.md](02-identity-auth.md)), `TraceId` phải được truyền qua header giữa các lời gọi HTTP nội bộ, để log của Process A và Process B cho cùng 1 request tra được chung 1 `TraceId`.
- **Metrics cơ bản** (số request/giây, latency p95/p99, tỉ lệ lỗi) — không cần hệ thống APM đắt tiền ngay, nhưng nên có ít nhất log định kỳ hoặc endpoint `/metrics` đơn giản để biết hệ thống đang khoẻ hay không **trước khi** có sự cố, không phải sau.

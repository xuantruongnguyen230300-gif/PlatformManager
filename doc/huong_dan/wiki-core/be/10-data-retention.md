# 10. Dữ liệu tích luỹ theo thời gian — Soft-delete không phải là archival

Soft-delete ([01-core-components.md](01-core-components.md), #1) giải quyết "ẩn khỏi người dùng ngay lập tức", nhưng **không** giải quyết "dữ liệu phình to mãi mãi" — mọi bản ghi `IsDelete=true` vẫn nằm nguyên trong bảng, vẫn tốn dung lượng, vẫn ảnh hưởng tốc độ query (dù có global filter). Hệ thống chạy nhiều năm cần 1 chính sách dọn dẹp:

- Tối thiểu: 1 job định kỳ hard-delete các bản ghi đã soft-delete quá lâu (ví dụ >2 năm, tuỳ yêu cầu lưu trữ pháp lý).
- Nếu dữ liệu lịch sử vẫn cần giữ lại để tra cứu nhưng không cần truy vấn nhanh: chuyển sang bảng/schema "archive" riêng, không nằm chung bảng đang hoạt động hàng ngày.

**Không cần làm ngay** — chỉ cần **quyết định trước chính sách** (giữ bao lâu, ai duyệt xoá thật) trước khi dữ liệu tích luỹ đủ lớn để việc dọn dẹp trở thành 1 dự án riêng tốn kém.

## Tổng kết mức ưu tiên cho hệ thống mới

| Chủ đề | Vấn đề | Nên nghĩ tới từ khi nào |
|---|---|---|
| [04](04-testing-strategy.md) | Test pyramid + cấm InMemory DB | Ngay từ đầu — chi phí thấp, lợi ích ngay |
| [08](08-adr-practice.md) | ADR ghi quyết định kiến trúc | Ngay từ đầu — chi phí gần 0 |
| [09](09-security-beyond-auth.md) | Secret management, rate limiting cơ bản | Ngay từ đầu (production-ready tối thiểu) |
| [06](06-concurrency-control.md) | Optimistic concurrency (`RowVersion`) | Khi có entity nhiều người cùng sửa |
| [07](07-observability.md) | Health check, correlation ID | Khi chuẩn bị lên production thật |
| [05](05-cross-module-consistency.md) | Integration event + idempotency | Khi tách ≥2 module/Process thật sự ghi chéo nhau |
| [05](05-cross-module-consistency.md) (Outbox) | Outbox pattern | Chỉ khi có nghiệp vụ không chấp nhận mất event (tài chính...) |
| 10 (mục này) | Chính sách archival dữ liệu | Trước khi dữ liệu đủ lớn để thành vấn đề — quyết định sớm, thực thi sau |

# 5. Nhất quán dữ liệu khi 1 nghiệp vụ chạm nhiều module/Process

Đây là bài toán chắc chắn sẽ gặp khi hệ thống lớn dần theo đúng hướng modular đã bàn ở [02-identity-auth.md](02-identity-auth.md). Ví dụ: "Tạo đơn hàng" cần trừ tồn kho (module Inventory) + tạo hoá đơn (module Billing) — 2 module khác nhau, có nên gói trong 1 transaction DB không?

**Câu trả lời thực tế (không phải lý thuyết)**: **Không** cố gắng gói cross-module trong 1 transaction DB cứng (distributed transaction rất đắt, dễ deadlock, và đúng ra vi phạm ranh giới module — nếu module A phải biết transaction của module B thì 2 module đã coupling chặt, không còn "module" nữa). Thay vào đó:

1. Module A hoàn tất + `SaveChanges` **trước** — commit dứt điểm phần của mình.
2. **Sau khi commit thành công**, publish 1 "integration event" (dữ liệu thuần, không phải lời gọi hàm trực tiếp) — module B (hoặc module khác) tự lắng nghe, tự xử lý phần của mình, tự `SaveChanges` riêng.
3. Consumer (module B) **bắt buộc kiểm tra idempotency trước khi xử lý** (event có thể đến trùng lặp do retry mạng) — ví dụ: kiểm tra đã xử lý event này chưa trước khi trừ tồn kho lần 2.

```
Module A (Order)                       Module B (Inventory)
  1. Tạo Order + SaveChanges (commit)
  2. Publish "OrderCreatedEvent"  ───▶   3. Nhận event, kiểm tra đã xử lý chưa (idempotency)
                                          4. Trừ tồn kho + SaveChanges riêng
```

**Đánh đổi phải chấp nhận**: có 1 khoảng thời gian rất ngắn giữa bước 1 và bước 4 mà dữ liệu "chưa nhất quán tuyệt đối" (Order đã tạo nhưng tồn kho chưa trừ) — gọi là **eventual consistency** (nhất quán cuối cùng, không phải nhất quán tức thời). Đây là đánh đổi **chấp nhận được và phổ biến** cho hệ thống modular — cái không chấp nhận được là để "chưa xử lý xong" biến thành "không bao giờ xử lý" (mất event) — nên bước publish luôn đặt **sau** khi đã commit chắc chắn phần của mình, và consumer luôn phải idempotent.

**Khi nào cần chặt hơn nữa (Outbox pattern)**: nếu ngay cả rủi ro "publish event bị rớt giữa chừng" (commit DB xong nhưng publish message thất bại do mạng) cũng không chấp nhận được — ghi event vào chính 1 bảng trong cùng transaction với dữ liệu nghiệp vụ (`OutboxMessage`), rồi có 1 tiến trình nền riêng đọc bảng đó và publish thật — đảm bảo publish và ghi dữ liệu luôn cùng thành công hoặc cùng thất bại. Chỉ cần đầu tư Outbox khi đã có nghiệp vụ thật sự nhạy cảm với việc "mất 1 event" (ví dụ: giao dịch tài chính) — không cần làm ngay từ đầu.

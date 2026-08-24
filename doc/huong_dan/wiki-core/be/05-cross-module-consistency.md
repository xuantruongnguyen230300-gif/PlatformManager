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

---

## Outbox đã quyết định dùng — 5 câu hỏi vận hành chưa được trả lời

> Bổ sung 2026-08-24, đối chiếu thực hành Outbox/event-driven chuẩn ngành cho
> hệ thống tầm trung: đoạn "Khi nào cần chặt hơn nữa (Outbox pattern)" ở trên
> dừng ở mức khái niệm — ghi vào bảng, có tiến trình nền đọc và publish. Đó
> là điều kiện **cần**, chưa **đủ**: một Outbox chạy production còn phải trả
> lời 5 câu hỏi vận hành dưới đây, thiếu câu nào thì cơ chế "đảm bảo không
> mất event" ở trên vẫn đúng trên giấy nhưng hỏng âm thầm khi có traffic
> thật. [12-notifications.md](12-notifications.md) đã tham chiếu ngược lại
> đúng cơ chế này cho kênh thông báo — 5 mục dưới là phần chung cho mọi
> consumer, không riêng gì thông báo.

### 1. Polling interval & backoff khi publish thất bại

Tiến trình nền không nên polling liên tục (tốn CPU/DB vô ích khi không có gì
để gửi) cũng không nên polling quá thưa (kéo dài đúng khoảng "chưa nhất quán
tuyệt đối" đã chấp nhận ở trên). Hệ thống đã có Hangfire (xem
[12-notifications.md](12-notifications.md) §4) — dùng lại làm dispatcher,
không cần message broker riêng chỉ để polling:

```csharp
// Recurring job, mỗi 2 giây — Cron 6 trường (giây) Hangfire hỗ trợ sẵn
RecurringJob.AddOrUpdate<OutboxDispatcher>(
    "outbox-dispatcher",
    d => d.DispatchPendingAsync(CancellationToken.None),
    "*/2 * * * * *");

public async Task DispatchPendingAsync(CancellationToken ct)
{
    var batch = await _db.OutboxMessages
        .Where(m => m.PublishedAt == null && m.NextAttemptAt <= DateTime.UtcNow)
        .OrderBy(m => m.CreatedAt)
        .Take(50)
        .ToListAsync(ct);

    foreach (var msg in batch)
    {
        try
        {
            await _publisher.PublishAsync(msg.EventType, msg.Payload, ct);
            msg.PublishedAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            msg.AttemptCount++;
            // Backoff mũ, chặn trần 300s — 1 lần publish lỗi (broker tạm downtime)
            // không được ăn hết mọi lượt chạy job kế tiếp bằng retry ngay lập tức
            msg.NextAttemptAt = DateTime.UtcNow.AddSeconds(Math.Min(Math.Pow(2, msg.AttemptCount), 300));
            _logger.LogWarning(ex, "Publish outbox message {Id} thất bại, lần {Attempt}", msg.Id, msg.AttemptCount);
        }
    }
    await _db.SaveChangesAsync(ct);
}
```

### 2. Dọn dẹp bảng Outbox — nếu không, bảng tăng vô hạn

Message đã publish thành công không cần giữ vĩnh viễn trong bảng nghiệp vụ —
giữ mãi thì mỗi lượt dispatcher quét `WHERE PublishedAt == null` phải lướt
qua ngày càng nhiều dòng đã xong việc, và bảng phình to kéo chậm
backup/restore. Thêm 1 job dọn định kỳ, chạy ngoài giờ cao điểm:

```csharp
// Giữ 7 ngày để còn tra cứu khi có sự cố gần đây — không phải con số cứng,
// chỉnh theo nhu cầu tra cứu thật. Cần audit dài hạn thì archive trước khi
// xoá, đừng DELETE thẳng không giữ vết.
await _db.OutboxMessages
    .Where(m => m.PublishedAt != null && m.PublishedAt < DateTime.UtcNow.AddDays(-7))
    .ExecuteDeleteAsync(ct);
```

### 3. Poison message — dead-letter sau N lần, đừng chặn cả hàng đợi

Backoff mũ ở mục 1 xử lý đúng lỗi **tạm thời** (mạng, broker downtime).
Nhưng nếu message lỗi vì **chính nội dung của nó** (bug ở consumer, payload
sai) thì backoff vẫn retry **mãi mãi** — cần một trần cứng:

```csharp
const int MaxAttempts = 10;

if (msg.AttemptCount >= MaxAttempts)
{
    msg.Status = OutboxStatus.DeadLettered;  // ra khỏi tập "chưa publish", dừng retry
    _logger.LogError("Outbox message {Id} vào dead-letter sau {N} lần thử", msg.Id, MaxAttempts);
    continue; // KHÔNG throw — batch còn message khác cần xử lý tiếp
}
```

Đây chính là khái niệm dead-letter mà [12-notifications.md](12-notifications.md)
§3.2 đã áp dụng riêng cho kênh thông báo ("hết số lần thì chuyển sang trạng
thái thất bại cuối") — ở đây là bản tổng quát cho mọi consumer, thông báo chỉ
là một trường hợp dùng lại nó.

### 4. Ordering — publish đúng thứ tự KHÔNG đồng nghĩa consumer xử lý đúng thứ tự

`OrderBy(CreatedAt)` ở dispatcher đảm bảo **publish** theo đúng thứ tự tạo
ra, nhưng không đảm bảo **consumer xử lý** theo đúng thứ tự đó — nếu
`OrderCreatedEvent` rồi `OrderCancelledEvent` của cùng 1 đơn hàng publish
cách nhau vài trăm ms, consumer chạy qua hàng đợi nhiều worker song song
hoàn toàn có thể xử lý event thứ 2 **trước** event thứ 1.

Cách rẻ nhất cho hệ tầm trung: đừng dựng đảm bảo thứ tự toàn cục (đắt), để
**consumer tự kiểm tra** bằng dữ liệu nghiệp vụ — mỗi event mang theo số
phiên bản tăng dần của aggregate, consumer so với version đã lưu, event cũ
hơn thì bỏ qua thay vì áp dụng mù quáng theo thứ tự tới:

```csharp
if (@event.AggregateVersion <= currentInventory.LastProcessedOrderVersion)
{
    return; // Event cũ hơn dữ liệu đã xử lý — bỏ qua, không phải lỗi
}
```

Bước "kiểm tra idempotency trước khi xử lý" ở bước 3 của luồng phía trên
(kiểm **đã xử lý event này chưa**) và kiểm version ở đây (kiểm **event này
còn mới hơn dữ liệu hiện tại không**) là hai câu hỏi khác nhau — cần cả hai,
một cái chặn trùng lặp, một cái chặn out-of-order.

### 5. Schema evolution — đổi field trong payload, consumer cũ không được vỡ

Integration event là hợp đồng giữa các module, giống API contract, nhưng
không có compile-time check giữa producer và consumer khi 2 module deploy
lệch nhau tạm thời (rolling deploy). Quy ước tối thiểu:

- Chỉ **thêm** field mới ở dạng optional — không đổi kiểu, không xoá field
  cũ trong cùng version; consumer cũ chưa deploy kịp vẫn parse được, chỉ là
  chưa thấy field mới.
- Đổi không tương thích ngược (đổi kiểu, đổi ý nghĩa, xoá field) → tạo
  **event type mới** (`OrderCreated.v2`), publish song song cả 2 version
  trong giai đoạn chuyển tiếp tới khi mọi consumer đã chuyển, rồi mới ngừng
  publish version cũ.
- `EventType` là chuỗi tường minh có version trong tên (khớp cột `EventType`
  ở ví dụ dispatcher trên) — route theo chuỗi này, không suy luận version từ
  shape của payload.

# 10. Dữ liệu tích luỹ theo thời gian — Soft-delete không phải là archival

Soft-delete ([01-core-components.md](01-core-components.md), #1) giải quyết "ẩn khỏi người dùng ngay lập tức", nhưng **không** giải quyết "dữ liệu phình to mãi mãi" — mọi bản ghi `IsDelete=true` vẫn nằm nguyên trong bảng, vẫn tốn dung lượng, vẫn ảnh hưởng tốc độ query (dù có global filter). Hệ thống chạy nhiều năm cần 1 chính sách dọn dẹp:

- Tối thiểu: 1 job định kỳ hard-delete các bản ghi đã soft-delete quá lâu (ví dụ >2 năm, tuỳ yêu cầu lưu trữ pháp lý).
- Nếu dữ liệu lịch sử vẫn cần giữ lại để tra cứu nhưng không cần truy vấn nhanh: chuyển sang bảng/schema "archive" riêng, không nằm chung bảng đang hoạt động hàng ngày.

**Không cần làm ngay** — chỉ cần **quyết định trước chính sách** (giữ bao lâu, ai duyệt xoá thật) trước khi dữ liệu tích luỹ đủ lớn để việc dọn dẹp trở thành 1 dự án riêng tốn kém.

## Ngưỡng hard-delete cụ thể — và cái bẫy tham chiếu FK khi xoá `AppUser`

> Bổ sung 2026-08-24, đối chiếu thực hành ngành cho hệ thống tầm trung: dòng
> "job định kỳ hard-delete... ví dụ >2 năm, tuỳ yêu cầu lưu trữ pháp lý" ở
> trên đúng hướng nhưng dừng ở ví dụ — chưa nói cái bẫy thật khi thực thi:
> `AppUser` không phải một entity lá, hard-delete nó không giống hard-delete
> một bản ghi nghiệp vụ thông thường.

**Vì sao đây là vấn đề THẬT.** PlatformManager là hệ thống quản trị **nội
bộ** — `AppUser` không chỉ là "1 dòng dữ liệu người dùng" mà còn là danh
tính được tham chiếu xuyên suốt: người thực hiện đánh giá, người được gán
role, người tạo job import (xem
[12-notifications.md](12-notifications.md) §"Nguồn người nhận" —
`AppUser` đã là điểm neo cho nhiều luồng khác). Hard-delete thẳng dòng
`AspNetUsers` sau khi soft-delete đủ lâu, nếu làm ẩu, vi phạm khoá ngoại (FK
`Restrict`) hoặc xoá lan dữ liệu nghiệp vụ không liên quan tới quyết định
"xoá tài khoản này" (FK `Cascade`) — bản đánh giá của cả một kỳ biến mất chỉ
vì người thực hiện đánh giá đó đã nghỉ việc từ lâu.

**Phân biệt bắt buộc trước khi hard-delete `AppUser`:**

| Loại bản ghi | Xử lý khi hard-delete |
| --- | --- |
| Dữ liệu thuộc về chính user đó, không ai khác cần (session, token, preference cá nhân) | Xoá thật theo FK `Cascade` — an toàn |
| Dữ liệu do user đó tạo ra nhưng thuộc về nghiệp vụ chung (bản đánh giá, job import, lịch sử duyệt) | **Không xoá** — chỉ gỡ định danh cá nhân, xem "Quyền được quên" dưới |

Ngưỡng thời gian cụ thể (2 năm hay khác) **không phải quyết định kỹ thuật**
— đây là chính sách nhân sự/pháp lý nội bộ (quy định lưu trữ hồ sơ lao động
sau khi chấm dứt hợp đồng), kỹ thuật chỉ thực thi đúng con số đã được duyệt,
không tự chọn khi viết job Hangfire.

## "Quyền được quên" — khi có yêu cầu xoá vĩnh viễn dữ liệu cá nhân của 1 người cụ thể

> Bổ sung 2026-08-24: mục này chưa có trong file trước đó — soft-delete giải
> quyết "ẩn khỏi người dùng", nhưng khi có 1 yêu cầu cụ thể "xoá hết dữ liệu
> cá nhân của tôi" (nhân viên nghỉ việc, hoặc theo chính sách bảo vệ dữ liệu
> cá nhân nội bộ), soft-delete và hard-delete-sau-N-năm ở trên **không trả
> lời được** — yêu cầu này cần xử lý ngay, không đợi hết ngưỡng thời gian.

**Vì sao đây là vấn đề THẬT.** Câu hỏi không phải "xoá `AppUser`" — như mục
trên vừa chỉ ra, xoá thẳng dòng đó kéo theo rủi ro xoá lan hoặc vỡ FK. Câu
hỏi thật là **gỡ được thông tin định danh cá nhân (tên, email) trong khi vẫn
giữ được dữ liệu nghiệp vụ đã gắn với đúng `Id` đó**. Nhầm 2 việc này làm 1
là lý do phổ biến nhất khiến team hoãn xử lý yêu cầu vô thời hạn — "xoá thì
sợ vỡ dữ liệu, không xoá thì không đáp ứng được yêu cầu".

**Cách làm chuẩn ngành: anonymize (giữ `Id`, xoá thông tin định danh),
KHÔNG hard-delete, cho entity bị tham chiếu rộng như `AppUser`:**

```csharp
// Minh hoạ hướng đi — không phải API đã có sẵn trong code
public async Task<Result> Handle(AnonymizeUserCommand cmd, CancellationToken ct)
{
    var user = await _userManager.FindByIdAsync(cmd.UserId);
    if (user is null) return Result.NotFound();

    user.FullName = $"Người dùng đã xoá ({user.Id.ToString()[..8]})";
    user.Email = null;
    user.PhoneNumber = null;
    user.IsDelete = true;                                  // vẫn đi qua soft-delete có sẵn
    await _userManager.UpdateAsync(user);
    await _userManager.UpdateSecurityStampAsync(user);      // huỷ mọi phiên còn sống — xem 02-identity-auth.md

    return Result.Success();
}
```

- **`Id` giữ nguyên** — mọi FK trỏ tới `AppUser` (bản ghi đánh giá do người
  này thực hiện, role đã gán...) vẫn hợp lệ, chỉ có tên/email hiển thị đổi
  thành placeholder. Lịch sử "ai đã thực hiện việc này" vẫn tra được là
  *một người cụ thể đã tồn tại*, chỉ không còn lộ thông tin định danh thật.
- **Khác hard-delete ở mục trên** — anonymize làm **ngay khi có yêu cầu**,
  không đợi ngưỡng nhiều năm; hard-delete thật (xoá hẳn dòng `AspNetUsers`)
  chỉ nên làm sau khi đã anonymize và không còn FK nào cần giữ định danh.
- **Rà danh sách bảng cần xử lý cho mỗi yêu cầu** — đây là việc rà **thủ
  công 1 lần cho mỗi yêu cầu**, khác các bảng số liệu-lặp-lại bị cấm chép tay
  ở `.claude/CLAUDE.md` §6. Nguyên tắc lọc: mọi FK trỏ tới `AspNetUsers.Id`
  — chạy 1 truy vấn catalog của chính DB
  (`information_schema.table_constraints` lọc `constraint_type = 'FOREIGN KEY'`,
  đối chiếu cột tham chiếu tới `AspNetUsers`) để có danh sách đầy đủ, không
  dựa trí nhớ.

## Archival thật ra làm thế nào — không chỉ "khác soft-delete"

> Bổ sung 2026-08-24: tiêu đề file đã đúng hướng ("soft-delete không phải
> archival") nhưng nội dung trước đó chỉ có 1 dòng "chuyển sang bảng/schema
> archive riêng" — chưa nói khi nào coi là cần và chuyển bằng cơ chế gì. Đây
> là khoảng trống thật: 2 câu hỏi đó mới là phần khó, "archival khác
> soft-delete" chỉ là tiền đề.

**Khi nào cần — bằng chứng, không phải lịch cố định.** Cùng nguyên tắc "đo
trước khi tối ưu" ở [11-performance-caching.md](11-performance-caching.md)
§5: archival chỉ đáng làm khi có bằng chứng cụ thể, ví dụ `EXPLAIN ANALYZE`
cho thấy query trên bảng hoạt động hàng ngày (`CriteriaAssessment` — đang
tăng khoảng 3.200 dòng/năm theo số đo ở
[11-performance-caching.md](11-performance-caching.md) §6.1) bắt đầu seq
scan vì phần lớn dòng là dữ liệu cũ không ai còn truy vấn. **Không** archival
theo lịch cố định ("cứ 2 năm chuyển 1 lần") khi chưa có số đo — đó là tối ưu
theo cảm tính, đúng thứ §5 của file performance đang cố tránh.

**Làm thế nào — 2 cách, chọn theo mức đầu tư hạ tầng đội đang có:**

| Cách | Khi phù hợp | Chi phí |
| --- | --- | --- |
| Bảng "archive" riêng cùng DB (bảng song song, job định kỳ chuyển dữ liệu rồi xoá ở bảng gốc) | Vẫn cần query được dữ liệu cũ qua SQL thông thường (báo cáo lịch sử nhiều năm) | Thấp — không cần hạ tầng mới, chỉ 1 job Hangfire + 1 bảng |
| Postgres table partitioning theo khoảng thời gian (`PARTITION BY RANGE`) | Bảng đơn đã đủ lớn để `VACUUM`/backup định kỳ chậm rõ rệt, cần drop nguyên 1 partition cũ thay vì xoá từng dòng | Trung bình — đổi schema, cần kế hoạch migrate dữ liệu hiện có vào đúng partition |

**Chưa cần xét "export sang cold storage ngoài DB"** (S3/Blob, nén file) ở
quy mô 5-15 dev/dữ liệu nội bộ — đó là bước tiếp theo chỉ khi cả 2 cách trên
vẫn không đủ (dữ liệu tới hàng chục triệu dòng), và kéo theo bài toán mới
(chuẩn hoá schema file, khôi phục khi cần) không đáng đầu tư trước khi có
bằng chứng thật ở quy mô đang bàn.

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

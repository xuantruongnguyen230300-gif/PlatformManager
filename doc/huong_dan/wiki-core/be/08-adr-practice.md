# 8. Ghi lại quyết định kiến trúc (ADR) — thói quen, không phải kỹ thuật

VNR đánh số kỷ luật mọi quyết định lớn (`ADR-008` entity base class exception, `ADR-011` owned event seam, `ADR-014` transaction per-module...) — mỗi quyết định kiến trúc quan trọng có 1 file ngắn ghi: **bối cảnh → các lựa chọn đã cân nhắc → quyết định → lý do**. Giá trị thực tế: 6 tháng sau, không ai (kể cả chính bạn) nhớ nổi "tại sao lúc đó chọn cách này mà không phải cách kia" — ADR trả lời câu hỏi đó mà không cần đoán lại hoặc hỏi người cũ.

**Khuyến nghị**: bắt đầu ngay từ ngày đầu hệ thống mới, dù chỉ 1-2 câu mỗi quyết định lớn (không cần format phức tạp) — chi phí gần như 0, giá trị tích luỹ theo thời gian rất lớn.

---

> Bổ sung 2026-08-24, đối chiếu thực hành ngành cho hệ thống tầm trung: đoạn
> trên nói ADR là gì và tại sao cần, nhưng bỏ ngỏ 3 câu hỏi mà đội ngũ thật sẽ
> hỏi ngay khi có ≥2 người viết ADR. Thiếu câu trả lời cho cả 3 là cách phổ
> biến nhất khiến "ghi lại quyết định kiến trúc" chết dần: không ai viết vì
> không biết khi nào cần, hoặc viết cho mọi thứ rồi không ai đọc, hoặc viết
> xong không ai biết nó đã lỗi thời.

## Ngưỡng: khi nào BẮT BUỘC viết ADR, khi nào không cần

Không có ngưỡng rõ dẫn tới 1 trong 2 thái cực, cả hai đều làm ADR mất giá trị:
**viết cho mọi quyết định** (kể cả chọn tên biến, chọn thư viện log) — số
lượng tăng nhanh hơn số người có thời gian đọc, ADR biến thành nghĩa địa file
không ai mở; hoặc **không viết cho gì cả** ("để sau, code trước đã") — quay
lại đúng vấn đề gốc mục trên đang giải quyết: 6 tháng sau không ai nhớ vì sao.

**Viết ADR khi quyết định thoả ít nhất 1 trong 3 tiêu chí:**

| Tiêu chí | Ví dụ trong chính hệ thống này |
| --- | --- |
| Ảnh hưởng **≥2 module/layer**, không phải sửa cục bộ 1 file | Cookie session thay vì JWT ([`02-identity-auth.md`](02-identity-auth.md)) — chạm Auth, FE `HttpClient`, CORS, mọi controller `[Authorize]` |
| **Khó đảo ngược** — đảo lại tốn hơn hẳn "sửa code" | KHÔNG cache permission matrix ([`11-performance-caching.md`](11-performance-caching.md) §6.2 mục 5) — đảo lại sau này phải quét lại toàn bộ đường ghi để thêm invalidation |
| Có **≥2 lựa chọn hợp lý đã cân nhắc**, chọn 1 vì lý do không hiển nhiên nhìn vào code | OpenIddict thay vì tự chế JWT khi cần OAuth2 thật ([`02-identity-auth.md`](02-identity-auth.md) §"Vậy có cần...") |

**Không cần ADR khi:** quyết định cục bộ trong 1 file/1 hàm, đảo ngược rẻ
(đổi tên biến, đổi thứ tự tham số), hoặc chỉ có đúng 1 cách làm hợp lý — không
thực sự có lựa chọn thứ 2 đáng cân nhắc. Comment trong code là đủ cho những
trường hợp này; viết ADR cho nó phí công cả người viết lẫn người đọc.

## Superseded — ADR cũ KHÔNG được sửa, chỉ được đóng lại

**Sai lầm phổ biến nhất khi vận hành ADR lâu dài:** sửa trực tiếp nội dung
ADR cũ cho khớp quyết định mới. Việc này xoá mất đúng thứ ADR tồn tại để giữ
— bối cảnh **tại thời điểm quyết định cũ**. 6 tháng sau không ai biết quyết
định ban đầu từng là gì, hay vì sao nó bị đổi.

**Luật:** khi 1 quyết định thay thế 1 ADR đã có, ADR cũ **giữ nguyên nội dung
gốc**, chỉ thêm 1 dòng trạng thái ở đầu file:

```markdown
> **Superseded by ADR-014** (2026-09-10) — xem lý do đổi ở ADR-014 §Bối cảnh.
> Nội dung dưới đây giữ nguyên làm bản ghi lịch sử, KHÔNG còn là quyết định
> đang áp dụng.
```

ADR mới phải trỏ ngược lại ADR cũ nó thay thế, không chỉ đi 1 chiều — người
đọc ADR-014 cần biết ngay "trước đây từng quyết khác, đây là lần thứ mấy đổi
ý" mà không phải tự tìm.

Đây cùng nguyên tắc "1 chủ đề — 1 file chủ, tài liệu đã chết mang banner lịch
sử thay vì bị sửa cho khớp hiện tại" — chỉ khác là áp dụng cho từng ADR riêng
lẻ thay vì cả file tài liệu.

## Review trước khi CHỐT — không tự viết tự chốt một mình

Một ADR do 1 người viết và tự đánh dấu "đã chốt" chỉ ghi lại 1 góc nhìn tại 1
thời điểm — giá trị lớn nhất của ADR (buộc cân nhắc lựa chọn khác trước khi
quyết) mất đi nếu không ai phản biện trước khi chốt.

**Với đội 5-15 dev, không cần quy trình nặng như code review đầy đủ (không
cần 2 approve, không cần checklist riêng) — nhưng tối thiểu:**

- ADR ở trạng thái **Draft/Proposed** cho tới khi ít nhất 1 người khác (không
  phải tác giả) đọc và đồng ý — kể cả chỉ là 1 dòng phản hồi trong PR/chat,
  không cần họp riêng.
- Đổi trạng thái sang **Accepted** là hành động tường minh, có dấu vết
  (comment PR, hoặc dòng "Accepted (ngày, đồng ý bởi X)" ngay trong file) —
  không phải mặc định ngay khi file được tạo.
- Ngoại lệ hợp lý duy nhất: hệ thống hiện **chỉ có 1 người phát triển** — khi
  đó review-trước-khi-chốt không áp dụng được (không có người thứ 2), ghi ADR
  là đủ. Bổ sung bước review ngay khi có người thứ 2 tham gia, đừng đợi tới
  khi đã tích luỹ nhiều ADR không ai review.

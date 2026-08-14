# 8. Ghi lại quyết định kiến trúc (ADR) — thói quen, không phải kỹ thuật

VNR đánh số kỷ luật mọi quyết định lớn (`ADR-008` entity base class exception, `ADR-011` owned event seam, `ADR-014` transaction per-module...) — mỗi quyết định kiến trúc quan trọng có 1 file ngắn ghi: **bối cảnh → các lựa chọn đã cân nhắc → quyết định → lý do**. Giá trị thực tế: 6 tháng sau, không ai (kể cả chính bạn) nhớ nổi "tại sao lúc đó chọn cách này mà không phải cách kia" — ADR trả lời câu hỏi đó mà không cần đoán lại hoặc hỏi người cũ.

**Khuyến nghị**: bắt đầu ngay từ ngày đầu hệ thống mới, dù chỉ 1-2 câu mỗi quyết định lớn (không cần format phức tạp) — chi phí gần như 0, giá trị tích luỹ theo thời gian rất lớn.

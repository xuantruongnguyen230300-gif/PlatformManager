---
feature: "dashboard-dti-weekly"
status: "ready-for-design"
updated: "2026-08-11"
---

# Spec — Dashboard "DTI Weekly"

Tổng quan nghiệp vụ cho màn hình dashboard theo dõi tiến độ chuyển đổi số
hàng tuần, dựng từ prototype `doc/Prototype/dashboard.html`. Tài liệu này là
điểm vào — chi tiết nằm ở 2 file song song bên dưới, viết bởi 2 agent phối
hợp qua `SendMessage` (đã đồng bộ 2 chiều, không có mâu thuẫn còn sót):

- **[business-rules.md](./business-rules.md)** (`backend-expert`) — entity,
  validation, công thức tính (delta, chênh lệch, tiến độ chung/nhóm), quy
  tắc "kỳ" (nay suy ra từ `CriteriaAssessment.CreatedAt`, không còn bảng
  `AssessmentPeriod` — xem cập nhật 2026-08-12).
- **[ui-spec.md](./ui-spec.md)** (`frontend-expert`) — layout, 15 action,
  states, responsive, map field UI ↔ ERD.

Nguồn dữ liệu nền: [doc/ERD/PlatformManager.dbml](../../doc/ERD/PlatformManager.dbml)
+ [doc/ERD/ERD.md](../../doc/ERD/ERD.md) (4 entity nghiệp vụ:
`CriteriaGroup`, `Criteria`, `CriteriaAssessment`, `CriteriaEvidence`, cộng
`AppUser` từ ASP.NET Core Identity — **[ĐÃ BỎ, 2026-08-12]** `AssessmentPeriod`
không còn tồn tại, xem `ERD.md` mục "Kỳ (tuần/tháng/năm)").

## Tóm tắt

Cán bộ chuyên trách chuyển đổi số nhập **% tiến độ** cho 62 chỉ tiêu theo
**từng kỳ báo cáo (tuần)**, hệ thống tự so với kỳ liền trước (delta), tổng
hợp KPI (tiến độ chung, số chỉ tiêu tăng/không tăng/hoàn thành), biểu đồ xu
hướng, và xuất báo cáo nhanh. Đây là **slice đầu tiên** của app — chỉ tái
tạo đúng những gì `dashboard.html` đã có, không mở rộng phạm vi ngoài các
quyết định đã chốt dưới đây.

## Quyết định đã chốt (người dùng)

| # | Quyết định | Ảnh hưởng |
| --- | --- | --- |
| 1 | `Owner`/`Deadline` gắn theo **từng kỳ** trên `CriteriaAssessment` (không cố định trên `Criteria`) | ERD |
| 2 | `Status` (4 giá trị) là field **nhập tay, lưu DB**, nhưng đến từ **quy trình thẩm định riêng** — dashboard tuần này **không có** UI cho field này (khớp đúng prototype) | ERD + UI — không thêm control mới |
| 3 | Auth dùng **ASP.NET Core Identity** — `Owner` → `OwnerId` (FK `AppUser.Id`) | ERD + `src/BE/CLAUDE.md` |
| 4 | `SelfScore`/`VerifiedScore` **tĩnh**, chỉ từ quy trình thẩm định riêng, không sửa qua dashboard tuần | Business rules + UI |
| 5 | "Sao lưu"/"Khôi phục" **giữ lại** ở backend thật, đổi ý nghĩa thành export/import dữ liệu thật qua API (không còn là backup kỹ thuật localStorage) | UI (không đổi) + Business rules (đổi semantics khi implement) |

## Phạm vi Phase 3

Tái tạo **đúng như prototype hiện có** (7 khu vực: topbar, weekbar, KPI
summary 5 ô, tiến độ theo nhóm + biểu đồ xu hướng, bảng 62 chỉ tiêu (8 cột)
với filter/sort, lịch sử các kỳ, dialog báo cáo nhanh) — không thêm control
nào cho `Status`/`SelfScore`/`VerifiedScore`/`Owner`/`Deadline`/
`CriteriaEvidence` (theo quyết định #2 và #4 ở trên, và mục 6.6 của
`ui-spec.md`). Trung thực với 3 breakpoint (`980px`, `560px`, print) và toàn
bộ 15 action đã liệt kê trong `ui-spec.md` mục 3.

**Đổi hướng (2026-08-11)**: Figma bị chặn bởi giới hạn quota gói Starter
(6 lần gọi tool/tháng, xem `doc/Design/Frontend/PlatformManager/README.md`
§ Pipeline Status) và không giải quyết được ngay — người dùng quyết định
**bỏ nhánh export Figma**, dùng chính `doc/Prototype/dashboard.html` (đã mở
rộng thêm sidebar menu, xem `spec/sidebar-menu/ui-spec.md`) làm giao diện
tham khảo/handoff duy nhất. Toàn bộ artifact stage 1-7 (token, component
spec, screen spec) trong `doc/Design/` vẫn giữ nguyên giá trị tham khảo cho
việc code Angular sau này — chỉ riêng bước "đẩy lên Figma" không còn là mục
tiêu bắt buộc.

## Câu hỏi còn mở (không chặn Phase 3)

Không ảnh hưởng tới hình dạng UI đang thiết kế — chỉ ảnh hưởng backend/
tương lai:

1. Quy ước chia "tuần" khi group-by `CriteriaAssessment.CreatedAt` (tuần
   ISO hay quy ước khác) — trước đây hỏi dưới dạng "ràng buộc lịch tuần chặt
   cho `AssessmentPeriod`" (bảng đó đã bỏ, 2026-08-12).
2. Permission chi tiết theo Role trong ASP.NET Core Identity.
3. `CriteriaEvidence.Content` có cần tách cấu trúc (DocNumber/DocDate)?
4. Cơ chế cấp token cho SPA (cookie session Identity vs JWT bearer riêng).

Xem chi tiết ở mục "Câu hỏi còn mở" của từng file `business-rules.md` /
`ui-spec.md`.

## Bước tiếp theo

`doc/Prototype/dashboard.html` (đã có sidebar menu, verify bằng
chrome-devtools-mcp: desktop 260px, thu gọn 72px, drawer mobile <980px, toàn
bộ 15 action + logic cũ không đổi) là giao diện tham khảo hiện tại — dùng
trực tiếp file này khi `frontend-expert` dựng app Angular thật trong
`src/FE/`. Artifact `doc/Design/Frontend/PlatformManager/` (token, component
spec, screen spec) vẫn dùng làm tài liệu tham chiếu chi tiết song song.

# 6. Kiểm soát ghi đè khi nhiều người sửa cùng lúc (Concurrency)

Gotcha rất thực tế: 2 người cùng mở 1 bản ghi, cùng sửa, người lưu sau **ghi đè mất luôn** thay đổi của người lưu trước — họ không biết vì UI vẫn báo "lưu thành công".

## Cách chuẩn — Optimistic Concurrency bằng token phiên bản

Nguyên lý: mỗi bản ghi mang một **token phiên bản** đổi giá trị ở mọi lần UPDATE. Khi ghi, câu lệnh kèm thêm `WHERE <token> = @giá_trị_lúc_đọc`. Nếu người khác đã ghi trước → token không còn khớp → UPDATE ảnh hưởng **0 dòng** → EF Core ném `DbUpdateConcurrencyException` → BE trả **409** kèm thông điệp rõ ("dữ liệu đã bị người khác thay đổi, tải lại trang") thay vì âm thầm ghi đè.

**Token đó là gì thì tuỳ provider — đây là điểm dễ sai nhất:**

| Provider | Token | Khai thế nào |
| --- | --- | --- |
| **PostgreSQL (đang dùng)** | cột hệ thống **`xmin`**, DB tự tăng sẵn | property CLR `uint` + `.IsRowVersion()` — Npgsql tự bind vào `xmin` |
| SQL Server | kiểu `rowversion`, DB tự tăng | `byte[] RowVersion` + `.IsRowVersion()` |

> ⚠️ Dùng nhầm công thức SQL Server trên PostgreSQL thì Npgsql tạo một cột `bytea` **không ai cập nhật** — điều kiện `WHERE` luôn khớp và **check concurrency vô hiệu hoàn toàn, im lặng**. Không lỗi biên dịch, không lỗi lúc chạy.

> 📖 **Recipe thi hành cụ thể** (code mẫu, khi nào thêm, khi nào không): [`doc/huong_dan/quy-uoc/be-entity-domain.md`](../../quy-uoc/be-entity-domain.md) §RowVersion. Bản đó là **file chủ** — mục này chỉ nói *vì sao cần*, không lặp lại *làm thế nào*.

**Chỉ cần cho**: entity có ≥2 luồng ghi độc lập chạm cùng bản ghi (danh mục dùng chung, cấu hình hệ thống, bản ghi trạng thái workflow). Không cần cho entity chỉ 1 người sở hữu/sửa (ví dụ hồ sơ cá nhân do đúng người đó tự sửa).


## Áp dụng vào PlatformManager

Ca đầu tiên cần áp dụng: `CriteriaAssessment` (`src/BE/Modules/DtiWeekly/PlatformManager.Modules.DtiWeekly.Domain/Entities/CriteriaAssessment.cs`) — có 2 luồng ghi độc lập chạm cùng bản ghi: import CSV hàng loạt (`CsvImportService`, ghi đè toàn bộ field) và sửa tay từng field (`UpdateCriteriaAssessmentCommand`, partial-update qua `AssessmentUpsertService.UpsertTodayAsync` — đọc-rồi-ghi, không có gì phát hiện ghi đè). Recipe cụ thể xem [`doc/huong_dan/quy-uoc/be-entity-domain.md`](../../../../doc/huong_dan/quy-uoc/be-entity-domain.md) §"RowVersion — optimistic concurrency". Các entity còn lại của module (`Criteria`, `CriteriaGroup`) hiện chỉ 1 luồng ghi (CRUD qua `CriteriaController`) — chưa cần RowVersion cho tới khi có luồng ghi thứ 2 tương tự.

# 6. Kiểm soát ghi đè khi nhiều người sửa cùng lúc (Concurrency)

Gotcha rất thực tế: 2 người cùng mở 1 bản ghi, cùng sửa, người lưu sau **ghi đè mất luôn** thay đổi của người lưu trước — họ không biết vì UI vẫn báo "lưu thành công".

## Cách chuẩn — Optimistic Concurrency qua cột `RowVersion`/`ETag`

```csharp
public class Customer : BaseEntity
{
    // EF Core tự tăng giá trị này mỗi lần UPDATE — không cần code tự quản lý
    public byte[] RowVersion { get; private set; } = default!;
}

builder.Property(x => x.RowVersion).IsRowVersion();
```

Khi UPDATE, EF Core tự thêm `WHERE "RowVersion" = @originalValue` vào câu lệnh — nếu dữ liệu đã bị người khác đổi trong lúc mình đang sửa, `RowVersion` không còn khớp → UPDATE ảnh hưởng 0 dòng → EF Core ném `DbUpdateConcurrencyException` → BE trả lỗi 409 rõ ràng ("dữ liệu đã bị người khác thay đổi, tải lại trang") thay vì âm thầm ghi đè.

**Chỉ cần cho**: entity có khả năng nhiều người cùng sửa (danh mục dùng chung, cấu hình hệ thống, bản ghi trạng thái workflow). Không cần cho entity chỉ 1 người sở hữu/sửa (ví dụ hồ sơ cá nhân do đúng người đó tự sửa).

## Áp dụng vào PlatformManager

Ca đầu tiên cần áp dụng: `CriteriaAssessment` (`src/BE/Modules/DtiWeekly/PlatformManager.Modules.DtiWeekly.Domain/Entities/CriteriaAssessment.cs`) — có 2 luồng ghi độc lập chạm cùng bản ghi: import CSV hàng loạt (`CsvImportService`, ghi đè toàn bộ field) và sửa tay từng field (`UpdateCriteriaAssessmentCommand`, partial-update qua `AssessmentUpsertService.UpsertTodayAsync` — đọc-rồi-ghi, không có gì phát hiện ghi đè). Recipe cụ thể (cột `RowVersion` + `.IsRowVersion()`) xem [`src/BE/.claude/rules/entity-domain.md`](../../../../src/BE/.claude/rules/entity-domain.md) §"RowVersion — optimistic concurrency". Các entity còn lại của module (`Criteria`, `CriteriaGroup`) hiện chỉ 1 luồng ghi (CRUD qua `CriteriaController`) — chưa cần RowVersion cho tới khi có luồng ghi thứ 2 tương tự.

import { Injectable, inject, signal } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { ToastService } from '../../../../core/toast/toast.service';
import { ICriteriaRow } from '../../models/danh-muc-dti.model';
import { DanhMucDtiService } from '../../services/danh-muc-dti.service';

/**
 * LUỒNG XOÁ (DM-5): giữ dòng đang chờ xác nhận, gọi `DELETE`, rồi báo kết quả.
 *
 * ## FE KHÔNG đoán trước cứng hay mềm
 *
 * 🛑 BE quyết, FE chỉ **đọc** `hardDeleted` trong kết quả. Đừng đoán trước bằng
 * `row.AssessmentId !== null` để đổi câu xác nhận: trường đó chỉ phản ánh **kỳ đang xem**, không
 * phản ánh toàn bộ lịch sử nhiều năm — bản thiết kế cũ làm vậy và câu xác nhận sai trong đúng ca
 * người dùng cần nó đúng nhất (một chỉ tiêu có lịch sử 2025 nhưng chưa có số liệu tuần này).
 *
 * Vì vậy câu **hỏi** chỉ nói "xoá chỉ tiêu nào", còn câu **báo kết quả** mới nói xoá kiểu gì.
 *
 * 🛑 `@Injectable()` KHÔNG `providedIn: 'root'` — vòng đời trùng vòng đời trang.
 */
@Injectable()
export class CriteriaDeleteFlow {
  private readonly service = inject(DanhMucDtiService);
  private readonly toast = inject(ToastService);
  private readonly translate = inject(TranslateService);

  /** Dòng đang chờ xác nhận. `null` = không có hộp thoại nào mở. */
  readonly target = signal<ICriteriaRow | null>(null);
  readonly deleting = signal(false);

  ask(row: ICriteriaRow): void {
    this.deleting.set(false);
    this.target.set(row);
  }

  cancel(): void {
    this.target.set(null);
  }

  confirm(onDeleted: () => void): void {
    const row = this.target();
    if (!row || this.deleting()) return;
    this.deleting.set(true);

    this.service.remove(row.CriteriaId).subscribe({
      next: (result) => {
        this.deleting.set(false);
        this.target.set(null);
        // Xoá đổi `totalCount` và có thể làm trang hiện tại rỗng ⇒ **tải lại lưới**, không gỡ dòng
        // tại chỗ: gỡ tại chỗ để lại một trang 9 dòng trong khi server đã có dòng thứ 10 để trám.
        onDeleted();
        this.toast.success(
          this.translate.instant(
            result.HardDeleted ? 'danh-muc-dti.toast.deletedHard' : 'danh-muc-dti.toast.deletedSoft',
          ) as string,
        );
      },
      error: () => {
        // Câu lỗi do `httpErrorInterceptor` bắn toast (`CRITERIA.NOT_FOUND` đã có bản dịch). Đóng
        // hộp thoại vì không còn gì để xác nhận lại — bấm `Xoá` lần nữa sẽ nhận đúng lỗi đó.
        this.deleting.set(false);
        this.target.set(null);
      },
    });
  }
}

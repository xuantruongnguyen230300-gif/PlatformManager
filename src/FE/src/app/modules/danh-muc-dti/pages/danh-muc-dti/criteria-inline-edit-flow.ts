import { Injectable, inject, signal } from '@angular/core';
import { ICriteriaRow, IWritePeriod } from '../../models/danh-muc-dti.model';
import { DanhMucDtiService } from '../../services/danh-muc-dti.service';

/**
 * Một lần lưu inline — mang **đúng trường vừa sửa** (DM-6 + Q74).
 *
 * `undefined` = không đụng tới; `null` = xoá trắng. Lớp này **không** điền thêm gì: mọi phép
 * "cho đủ bộ" đều là một lời ghi mà người dùng không hề yêu cầu.
 */
export interface IInlineEditRequest {
  readonly CriteriaId: string;
  readonly ProgressPercent?: number | null;
  readonly Note?: string | null;
}

/**
 * LUỒNG SỬA INLINE (DM-6) — đúng 2 trường, `PUT` ghi đè cả hai.
 *
 * Tách khỏi trang theo cùng khuôn `user-form-flow.ts`: lớp này không biết bộ lọc, không biết
 * trang mấy; nó nhận "người dùng vừa lưu một ô" + kỳ đích + dòng gốc, và trả dòng mới về qua
 * callback.
 *
 * ## Lưu hỏng ⇒ ô TỰ hoàn nguyên, không cần code hoàn nguyên
 *
 * Ô trong lưới luôn vẽ từ `rows()` của `CriteriaListFeed`, mà `rows()` chỉ đổi khi trang gọi
 * `replaceRow` — tức **sau** một response thành công. Nên request hỏng nghĩa là ô hiện lại giá trị
 * cũ, đúng thứ `ui-spec` §5.4 yêu cầu (*"hoàn nguyên giá trị ô … không để ô hiển thị giá trị chưa
 * lưu như thể đã lưu"*). Câu lỗi thì `httpErrorInterceptor` đã bắn toast — kể cả
 * `409 CRITERIA.ASSESSMENT_CONFLICT`, mã đã có bản dịch riêng trong `i18n-app`.
 *
 * 🛑 `@Injectable()` KHÔNG `providedIn: 'root'` — vòng đời trùng vòng đời trang.
 */
@Injectable()
export class CriteriaInlineEditFlow {
  private readonly service = inject(DanhMucDtiService);

  /** Chỉ tiêu đang có request bay — dùng để chặn lưu chồng lên chính nó. */
  readonly saving = signal<string | null>(null);

  /**
   * @param period cặp `{ Period, Year }` do `buildWritePeriod` dựng. `null` ⇒ ô lọc đang chọn một
   *   **tháng**, và khi đó không có lời ghi nào để gửi (Q37) — luồng bình thường không tới được
   *   đây vì `isEditable = false` đã tắt editor, nên đây là lưới chặn thứ hai.
   * @param source dòng ĐANG hiện — nguồn của `Version`, token chống ghi đè.
   */
  save(
    request: IInlineEditRequest,
    period: IWritePeriod | null,
    source: ICriteriaRow,
    onSaved: (row: ICriteriaRow) => void,
  ): void {
    if (!period || this.saving() === request.CriteriaId) return;
    this.saving.set(request.CriteriaId);

    this.service
      .updateAssessment(request.CriteriaId, {
        Period: period.Period,
        Year: period.Year,
        // Truyền THẲNG, giữ nguyên `undefined` của trường không đổi — mapper là chỗ duy nhất
        // quyết khoá nào ra dây (Q74).
        ProgressPercent: request.ProgressPercent,
        Note: request.Note,
        // `Version` lấy từ DÒNG, không từ ô nhập nào: thứ cần so là trạng thái người này ĐÃ NHÌN
        // THẤY lúc bắt đầu sửa. Bỏ nó đi thì mọi lần ghi đều thắng và mất dữ liệu sẽ im lặng —
        // Q20 làm chuyện hai người cùng sửa lại một kỳ đã qua thành chuyện thường, không còn hiếm.
        Version: source.Version,
      })
      .subscribe({
        next: (row) => {
          this.saving.set(null);
          onSaved(row);
        },
        // Không `set(null)` trong `finalize`: `subscribe` này không có nhánh `complete` nào khác,
        // và hai nhánh tường minh đọc dễ hơn một toán tử đặt ở giữa chuỗi.
        error: () => this.saving.set(null),
      });
  }
}

import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { DeltaIndicator } from '../../../../shared/components/delta-indicator/delta-indicator';

/**
 * Một dòng "kỳ đã lưu" trong bảng Lịch sử của Dashboard — bốn ô: khoảng ngày · tiến độ chung ·
 * biến động so với kỳ liền trước · nút `Xem`.
 *
 * Hợp đồng đầy đủ: `doc/Design/Frontend/PlatformManager/Components/HistoryRow.md`.
 *
 * ## Vì sao ở `modules/dashboard/` chứ không ở `shared/`
 *
 * Đúng một màn dùng nó, và `doc/huong_dan/quy-uoc/fe-architecture.md` §Tầng app cấm đưa thứ chỉ
 * một feature dùng vào `shared/`. Cái giá cụ thể của việc đặt nhầm: `shared/` đi theo khi tách
 * CoreBase, còn chuỗi của component này mang tiền tố `dashboard.*` và nằm ở `public/i18n-app/`
 * (nhóm dự án) nên **ở lại** — sản phẩm thứ hai sẽ nhận được component và render ra chính chuỗi
 * khoá, không lỗi nào báo. Tầng của component và tiền tố khoá của nó phải đi cùng nhau, đó là
 * toàn bộ ý nghĩa của khuôn `<màn>.<nhóm>.<tên>` ở doc/huong_dan/wiki-core/fe/08-i18n.md §2.
 *
 * `DeltaIndicator` thì ngược lại: hai màn dùng nên nó ở `shared/`, khoá mang tiền tố `shared.*` và
 * nằm ở nhóm CoreBase (`public/i18n/`) để đi cùng chuyến — xem JSDoc `TEXT_KEYS` ở file đó. Import
 * `modules/` → `shared/` dưới đây là chiều được phép; chiều bị cổng G8 chặn là
 * `modules/<A>` → `modules/<B>` (`src/FE/eslint.config.js`).
 *
 * ## Ô 1 — MỘT khuôn cho cả chế độ Tuần lẫn chế độ Tháng
 *
 * `{dd/MM} – {dd/MM/yyyy}`: `10/08 – 16/08/2026` (tuần) và `01/08 – 31/08/2026` (tháng). Không có
 * khuôn thứ hai, không có ngoại lệ nào phải đăng ký.
 *
 * 🛑 **Đừng rút gọn chế độ Tháng thành `Tháng 8/2026`.** Phương án đó được chốt rồi HOÀN LẠI trong
 * cùng ngày 2026-09-09, sau khi đo: hai chuỗi dài **bằng nhau** (18 ký tự), nên tiền đề *"tháng dài
 * hơn, không đủ chỗ"* đơn giản là sai; và `spec/dashboard-dti/business-rules.md` §6.2b chốt khoảng
 * ngày là một trong hai thành phần KHÔNG BAO GIỜ được lược. Tiền lệ T7 (trục X biểu đồ dùng
 * `Th.1…Th.12`) không áp được: trục X chen 12 nhãn trên một đường, còn ở đây mỗi kỳ có một hàng
 * riêng và không cạnh tranh chiều ngang với ai.
 *
 * ## Ô 3 — `Kỳ đầu`, không phải một số 0
 *
 * Hàng cũ nhất không có gì để so. In `0,0 đ.%` ở đó là bịa ra một phép đo chưa từng xảy ra; hai ý
 * *"chưa có gì để so"* và *"có so, và không đổi"* phải phân biệt được, và đây là chỗ khác biệt đó
 * nhìn rõ nhất.
 *
 * ## Thứ tự tính rồi mới đảo — cạm bẫy dấu
 *
 * Trang cha tính biến động trên danh sách xếp TĂNG dần theo ngày, rồi mới đảo để hiện mới-trước.
 * Tính trên danh sách đã đảo thì mọi dấu bị lật (§ Do / Don't).
 */
@Component({
  selector: 'app-history-row',
  standalone: true,
  imports: [TranslatePipe, DeltaIndicator],
  templateUrl: './history-row.html',
  styleUrl: './history-row.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    // `.histrow` trên chính host, cùng lý do với `.group-row` của ProgressBar: thẻ bọc trung gian
    // sẽ ngắt `gap` của flex column bên ngoài.
    class: 'histrow',
  },
})
export class HistoryRow {
  /** Mốc đầu của kỳ. Thiếu một trong hai mốc ⇒ ô 1 hiện `—`. */
  readonly periodStart = input<Date | null>(null);

  /** Mốc cuối của kỳ — mốc DUY NHẤT mang năm trong chuỗi hiển thị. */
  readonly periodEnd = input<Date | null>(null);

  /** Tiến độ chung của kỳ, thang 0–100. `null` ⇒ ô 2 hiện `—`. */
  readonly overallProgress = input<number | null | undefined>(null);

  /**
   * Biến động so với kỳ LIỀN TRƯỚC, tính bằng điểm phần trăm. Bỏ qua khi `isFirstPeriod` bật.
   */
  readonly delta = input<number | null | undefined>(null);

  /** Hàng cũ nhất trên bảng — ô 3 in `Kỳ đầu` thay cho một delta. */
  readonly isFirstPeriod = input<boolean>(false);

  /** `localeId` của ngôn ngữ đang chọn (cổng G4 — không `inject(LanguageService)`). */
  readonly localeId = input<string>('vi');

  /** Người dùng bấm `Xem`. Trang cha đổi query param để nạp lại toàn trang theo kỳ đó. */
  readonly view = output<void>();

  protected readonly hasPeriod = computed<boolean>(
    () => this.periodStart() !== null && this.periodEnd() !== null,
  );

  /**
   * MỘT bộ định dạng cho cả hai mốc — và đây là chỗ dễ hỏng nhất của ô này.
   *
   * 🛑 **Đừng dựng bộ định dạng thứ hai chỉ có `day` + `month` cho mốc đầu.** ICU của Chrome trả
   * về dấu ngăn KHÁC nhau cho hai cấu hình đó ở locale `vi`: `{day, month}` cho ra `10-08` (gạch
   * nối) trong khi `{day, month, year}` cho ra `16/08/2026` (gạch chéo). Kết quả là ô 1 hiện
   * `10-08 – 16/08/2026` — hai nửa của cùng một khoảng ngày viết bằng hai ký hiệu khác nhau. Lỗi
   * này đã xảy ra thật ở lượt dựng đầu (2026-09-09) và bị `history-row.spec.ts` bắt.
   *
   * Cách chắc chắn: định dạng ĐẦY ĐỦ rồi CẮT phần năm ra (`fromText`), nên hai mốc dùng chung
   * đúng một dấu ngăn theo định nghĩa, ở mọi locale.
   */
  private readonly dateFormatter = computed<Intl.DateTimeFormat>(
    () =>
      new Intl.DateTimeFormat(this.localeId(), {
        day: '2-digit',
        month: '2-digit',
        year: 'numeric',
      }),
  );

  /** `10/08` — mốc đầu, KHÔNG có năm. Năm chỉ viết một lần, ở mốc cuối (§6.1 mục 3). */
  protected readonly fromText = computed<string>(() => {
    if (!this.hasPeriod()) return '';

    // Bỏ phần `year`, rồi bỏ nốt dấu ngăn bị mồ côi ở đầu hoặc cuối. Xử lý cả hai đầu vì thứ tự
    // ngày/tháng/năm đổi theo locale — `vi` để năm ở cuối, có locale để năm ở đầu.
    const parts = this.dateFormatter()
      .formatToParts(this.periodStart() as Date)
      .filter((part) => part.type !== 'year');
    while (parts.length > 0 && parts[0].type === 'literal') parts.shift();
    while (parts.length > 0 && parts[parts.length - 1].type === 'literal') parts.pop();

    return parts.map((part) => part.value).join('');
  });

  /** `16/08/2026` — mốc cuối, có năm. */
  protected readonly toText = computed<string>(() =>
    this.hasPeriod() ? this.dateFormatter().format(this.periodEnd() as Date) : '',
  );

  /**
   * Tham số dùng chung cho HAI câu: chuỗi hiện ra ở ô 1, và nhãn trợ năng của nút `Xem`.
   *
   * Cả hai đi qua bảng dịch với cùng cặp tham số, nên dấu ` – ` (en dash, có khoảng trắng hai bên
   * — chốt T5) chỉ tồn tại ở MỘT chỗ: file bảng dịch. Ghép chuỗi trong TS ở đây là dựng bản sao
   * thứ hai của cùng một khuôn.
   *
   * Vì sao nút `Xem` cần nhãn riêng: sáu nút giống hệt nhau trên một bảng thì trình đọc màn hình
   * chỉ đọc ra sáu chữ "Xem", không phân biệt được kỳ nào.
   */
  protected readonly rangeParams = computed<{ From: string; To: string }>(() => ({
    From: this.fromText(),
    To: this.toText(),
  }));

  protected readonly hasProgress = computed<boolean>(() => {
    const progress = this.overallProgress();
    return progress !== null && progress !== undefined && Number.isFinite(progress);
  });

  /** `82,1%` — cùng khuôn với cột số của `ProgressBar`. */
  protected readonly progressText = computed<string>(() => {
    if (!this.hasProgress()) return '';
    return new Intl.NumberFormat(this.localeId(), {
      style: 'percent',
      minimumFractionDigits: 1,
      maximumFractionDigits: 1,
    }).format((this.overallProgress() as number) / 100);
  });

}

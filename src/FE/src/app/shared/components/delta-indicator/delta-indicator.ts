import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

/**
 * Hai kiểu số mà `.delta` phải vẽ — KHÁC NHAU về đơn vị lẫn số chữ số thập phân, và trộn hai
 * kiểu vào một là cách nhanh nhất để một cột đọc sai đơn vị.
 *
 * | Kiểu | Đại lượng | Khuôn |
 * | --- | --- | --- |
 * | `score` | `Thẩm định − Tự đánh giá`, chênh lệch ĐIỂM | 2 chữ số thập phân, có dấu, KHÔNG đơn vị |
 * | `percentagePoint` | biến động giữa hai kỳ, tính bằng ĐIỂM PHẦN TRĂM | mũi tên + 1 chữ số + `đ.%` |
 *
 * Hợp đồng: `doc/Design/Frontend/PlatformManager/Components/DeltaIndicator.md`
 * § "Two different numbers, one component".
 */
export type DeltaFormat = 'score' | 'percentagePoint';

/** Ba hướng ứng với ba lớp màu `.delta.up` / `.delta.down` / `.delta.flat` ở `styles.scss`. */
export type DeltaDirection = 'up' | 'down' | 'flat';

/**
 * Ngưỡng coi một hiệu số là BẰNG KHÔNG.
 *
 * 🛑 `ε = 0.001`, chốt 2026-09-09, MỘT giá trị cho cả hai kiểu ở trên. Nguồn:
 * `spec/dashboard-dti/business-rules.md` §1.4 (đếm `up`/`flat`/`down`) và
 * `spec/danh-muc-dti/business-rules.md` §3.1 (`Chênh lệch`).
 *
 * Vì sao không so `=== 0`: hiệu số sinh ra từ phép trừ hai số thập phân, nên `0.38 - 0.38` có thể
 * ra `1e-17` — một chỉ tiêu KHÔNG đổi sẽ hiện `+0,00` màu xanh, tự tin và sai.
 */
export const DELTA_EPSILON = 0.001;

/**
 * Chữ số thập phân của từng kiểu — hai số này là hình dạng đã duyệt, không phải tuỳ chọn.
 * `score` hai chữ số (`+2,96`), `percentagePoint` một chữ số (`↑ +2,3 đ.%`).
 */
const FRACTION_DIGITS: Readonly<Record<DeltaFormat, number>> = {
  score: 2,
  percentagePoint: 1,
};

/**
 * Khoá dịch cho từng ca. Mỗi khoá là MỘT câu trọn vẹn có chỗ cắm `{{Value}}` — mũi tên, khoảng
 * trắng và đơn vị đều nằm TRONG bảng dịch, không ghép bằng `+` trong template
 * (doc/huong_dan/quy-uoc/fe-ui-conventions.md §i18n).
 *
 * Vì sao `score` cũng có khoá dù câu của nó chỉ là `{{Value}}`: để template chỉ có MỘT nhánh
 * hiển thị. Nhánh thứ hai (`@if` quanh một interpolation trần) đưa khoảng trắng của template vào
 * chuỗi render ra, và `.delta` là chỗ khoảng trắng thừa nhìn thấy được — nó nằm trong ô bảng canh
 * phải.
 *
 * ## 🛑 Năm khoá này nằm ở nhóm CoreBase (`public/i18n/`), không phải nhóm dự án
 *
 * Component này ở `shared/`, tức nó **đi theo** khi tách CoreBase sang sản phẩm thứ hai
 * (doc/kien-truc-core-module.md). Chuỗi của nó vì vậy phải đi cùng chuyến, nếu không sản phẩm mới
 * render ra chính chuỗi khoá — ngx-translate không báo lỗi khi tra trượt, nó trả về tên khoá.
 * Ranh giới Core ↔ dự án tách bằng FILE (doc/huong_dan/wiki-core/fe/08-i18n.md §5), nên "đi cùng"
 * ở đây nghĩa là nằm trong `src/FE/public/i18n/{vi,en}.json`.
 *
 * Bốn component anh em của màn dashboard (`KpiTile`, `ProgressBar`, `TrendChart`, `HistoryRow`)
 * ở `modules/dashboard/components/`, nên chuỗi của chúng mang tiền tố `dashboard.*` và **ở lại**
 * `public/i18n-app/`. Tiền tố khác nhau ⇒ **không nhánh gốc nào bắc qua hai file**, và đó là tính
 * chất phải giữ: `app-i18n.spec.ts` chỉ chặn trùng **LÁ**, nó không thấy được một nhánh nằm ở cả
 * hai nguồn.
 *
 * `shared.format.absentValue` (`—`) đi theo Core vì component này dùng nó, và ba component ở
 * `modules/` cũng tra đúng khoá đó — bảng dịch lúc chạy là bản ĐÃ GHÉP mọi nguồn, và chiều phụ
 * thuộc `modules/` → Core là chiều được phép. Nó nằm dưới `shared.format` chứ không dưới
 * `shared.deltaIndicator` vì nó **không thuộc component nào**: nó trả lời câu hỏi *"vẽ thế nào một
 * giá trị KHÔNG tồn tại"*, tức một quyết định định dạng, cùng loại với đầu ra của `Intl`. Đoạn
 * giữa `format` theo tiền lệ `shared.app` / `shared.httpError` / `shared.menu` — tên một nhóm
 * chuỗi dùng chung không có component chủ.
 *
 * `delta-indicator.spec.ts` kiểm điều này bằng cách đọc RIÊNG `/i18n/`, không tin câu trên.
 */
export const TEXT_KEYS = {
  absent: 'shared.format.absentValue',
  score: 'shared.deltaIndicator.score',
  up: 'shared.deltaIndicator.pointUp',
  down: 'shared.deltaIndicator.pointDown',
  flat: 'shared.deltaIndicator.pointFlat',
} as const;

/**
 * Chữ chênh lệch tô màu theo hướng — `<span class="delta up|down|flat">`.
 *
 * Hợp đồng đầy đủ: `doc/Design/Frontend/PlatformManager/Components/DeltaIndicator.md`.
 *
 * ## Ranh giới: component này làm HAI việc, và chỉ hai việc đó
 *
 * 1. **Phân loại** số vào up / down / flat theo `DELTA_EPSILON`;
 * 2. **Định dạng** số theo locale đang chọn.
 *
 * Cả hai đều là thứ mà mọi nơi gọi sẽ tự làm lại nếu component không làm — và ba nơi tự làm là
 * ba luật epsilon lệch nhau. Vì vậy nơi gọi truyền số THÔ CÓ DẤU, không truyền chuỗi đã format.
 *
 * ## Vì sao màu không nằm trong file `.scss` của chính nó
 *
 * `.delta` / `.delta.up` / `.delta.down` / `.delta.flat` đã có sẵn ở `src/FE/src/styles.scss` §5
 * và **không** được khai lại ở đây: chúng là lớp màu TOÀN CỤC để hai màn hình dùng chung không
 * trôi khỏi nhau. File `.scss` cạnh đây chỉ đóng góp `display: inline-block` cho chính thẻ host,
 * đúng như bản dựng đã duyệt.
 *
 * ## Ca "không có gì để so" — xử lý ở ĐÂU
 *
 * Truyền `null` thì component hiện `—` màu xám. Nhưng chỗ nào có câu chữ tốt hơn thì nơi gọi tự
 * lo: danh sách lịch sử in `Kỳ đầu` ở hàng cũ nhất thay vì một dấu gạch trống nghĩa.
 */
@Component({
  selector: 'app-delta-indicator',
  standalone: true,
  imports: [TranslatePipe],
  templateUrl: './delta-indicator.html',
  styleUrl: './delta-indicator.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DeltaIndicator {
  /**
   * Hiệu số THÔ, còn nguyên dấu. `null`/`undefined`/`NaN` ⇒ ca "không có gì để so".
   *
   * 🛑 Đừng truyền chuỗi đã format vào đây. Chiều trừ (`Thẩm định − Tự đánh giá`, quyết định Q25)
   * là việc của tầng dữ liệu; component chỉ đọc dấu của con số nhận được.
   */
  readonly value = input<number | null | undefined>(null);

  /** Kiểu đại lượng — xem `DeltaFormat`. */
  readonly format = input<DeltaFormat>('score');

  /**
   * `localeId` của ngôn ngữ đang chọn, để `Intl.NumberFormat` đặt đúng dấu thập phân.
   *
   * Là `input()` chứ KHÔNG `inject(LanguageService)`: đây là component dumb trong `shared/`
   * (cổng G4). Trang cha đọc `LanguageService.localeId()` rồi truyền xuống.
   */
  readonly localeId = input<string>('vi');

  /** Có số để hiển thị không — `NaN` cũng tính là không, vì nó tô màu được nhưng vô nghĩa. */
  protected readonly hasValue = computed<boolean>(() => {
    const value = this.value();
    return value !== null && value !== undefined && Number.isFinite(value);
  });

  /**
   * Hướng, đã áp epsilon. Không có giá trị ⇒ `flat`: ô vẫn phải mang một lớp màu, và xám là màu
   * "không nói gì" (§ Variants, hàng "No comparable value").
   */
  protected readonly direction = computed<DeltaDirection>(() => {
    if (!this.hasValue()) return 'flat';
    const value = this.value() as number;
    if (value > DELTA_EPSILON) return 'up';
    if (value < -DELTA_EPSILON) return 'down';
    return 'flat';
  });

  /**
   * Con số đã định dạng theo locale, kèm dấu.
   *
   * `signDisplay: 'exceptZero'` là chỗ dấu `+`/`−` được sinh ra — KHÔNG ghép tay bằng `'+' + n`,
   * vì cách ghép tay cho ra `+-5,00` ở đúng ca số âm (`spec/dashboard-dti/ui-spec.md` §8).
   */
  protected readonly formatted = computed<string>(() => {
    if (!this.hasValue()) return '';
    const digits = FRACTION_DIGITS[this.format()];
    return new Intl.NumberFormat(this.localeId(), {
      minimumFractionDigits: digits,
      maximumFractionDigits: digits,
      signDisplay: 'exceptZero',
    }).format(this.value() as number);
  });

  /** Khoá dịch của ca hiện tại — xem `TEXT_KEYS`. */
  protected readonly textKey = computed<string>(() => {
    if (!this.hasValue()) return TEXT_KEYS.absent;
    if (this.format() === 'score') return TEXT_KEYS.score;
    return TEXT_KEYS[this.direction()];
  });

  /** Tham số cắm vào câu đã dịch. */
  protected readonly textParams = computed<{ Value: string }>(() => ({ Value: this.formatted() }));
}

import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

/** Cận trên/dưới của thanh. Giá trị ngoài khoảng này bị KẸP, không bị từ chối. */
const PERCENT_MIN = 0;
const PERCENT_MAX = 100;

/**
 * Một hàng "tiến độ theo nhóm": tên nhóm · thanh · phần trăm.
 *
 * Hợp đồng đầy đủ: `doc/Design/Frontend/PlatformManager/Components/ProgressBar.md`.
 * Bố cục ba cột `210px 1fr 80px` (và hai bậc responsive) nằm ở SCSS cạnh file này.
 *
 * ## 🛑 RỖNG khác 0 — đây là phần dễ làm sai nhất của component này
 *
 * `percent = null` ⇒ **track trống, KHÔNG có `.fill`**, và cột số hiện `—`.
 * `percent = 0` ⇒ có `.fill` dài 0, cột số hiện `0,0%`.
 *
 * Hai thứ đó là hai sự thật khác nhau: *"chưa ai nhập"* và *"đã đo, kết quả bằng không"*. Và ca
 * đầu là ca THƯỜNG NGÀY chứ không phải ca hiếm — quyết định Q24 để `Tiến độ %` **trống** khi
 * import, người dùng tự nhập sau, nên **ngay sau mỗi lần import cả sáu thanh nhóm đều rỗng**
 * (`spec/danh-muc-dti/business-rules.md` §6.4, `spec/dashboard-dti/business-rules.md` §1.1).
 *
 * Sáu con số trong bản dựng đã duyệt (74,3% · 51,8% · …) là trạng thái SAU KHI người dùng đã nhập
 * tiến độ, và chúng là tỉ lệ ĐIỂM — không phải `Tiến độ %`. Đừng dùng chúng làm giá trị kỳ vọng
 * của một test.
 *
 * ## Vì sao cột số bên phải là BẮT BUỘC
 *
 * Thanh này mang thông tin nghiệp vụ, mà một thanh không có số thì màu và chiều dài là kênh DUY
 * NHẤT — người không phân biệt được độ dài chính xác sẽ không đọc được gì. Con số ở cột ba không
 * phải trang trí (`spec/dashboard-dti/ui-spec.md` §3.3).
 *
 * ## Một màu duy nhất
 *
 * `.fill` luôn là `--brand` ở mọi mức hoàn thành. Không có ngưỡng đổi màu xanh/vàng/đỏ, và đó là
 * quyết định chứ không phải thiếu sót (§ Variants).
 */
@Component({
  selector: 'app-progress-bar',
  standalone: true,
  imports: [TranslatePipe],
  templateUrl: './progress-bar.html',
  styleUrl: './progress-bar.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    // `.group-row` nằm trên CHÍNH host, không bọc thêm `<div>`: danh sách cha là một flex column
    // với `gap`, nên một thẻ bọc trung gian sẽ ngắt chuỗi đó và các hàng dính vào nhau. Cùng bài
    // học đã ghi ở `shared/components/toolbar/toolbar.ts`.
    class: 'group-row',
  },
})
export class ProgressBar {
  /** Tên nhóm, đã dịch — ví dụ `1. Hạ tầng và Nền tảng số`. */
  readonly label = input<string>('');

  /**
   * Tiến độ theo thang 0–100. `null`/`undefined`/`NaN` ⇒ biến thể *Awaiting data*.
   *
   * Giá trị ngoài khoảng bị KẸP chứ không bị bỏ: một `Tiến độ %` lỡ lưu thành `120` phải vẽ ra
   * một thanh đầy, không phải một thanh tràn khỏi track.
   */
  readonly percent = input<number | null | undefined>(null);

  /**
   * `localeId` của ngôn ngữ đang chọn. Là `input()` chứ không `inject(LanguageService)` — cổng G4.
   */
  readonly localeId = input<string>('vi');

  protected readonly hasValue = computed<boolean>(() => {
    const percent = this.percent();
    return percent !== null && percent !== undefined && Number.isFinite(percent);
  });

  /** Bề rộng `.fill`, đã kẹp về `[0, 100]`. Chỉ đọc khi `hasValue()`. */
  protected readonly clamped = computed<number>(() =>
    Math.min(PERCENT_MAX, Math.max(PERCENT_MIN, this.percent() as number)),
  );

  /**
   * Phần trăm bằng chữ cho cột ba: `74,3%`.
   *
   * `style: 'percent'` của `Intl` chứ không phải nối `'%'` bằng tay — vị trí và khoảng trắng
   * trước ký hiệu `%` khác nhau giữa các locale.
   */
  protected readonly percentText = computed<string>(() => {
    if (!this.hasValue()) return '';
    return new Intl.NumberFormat(this.localeId(), {
      style: 'percent',
      minimumFractionDigits: 1,
      maximumFractionDigits: 1,
    }).format(this.clamped() / 100);
  });

  /** `aria-valuenow` chỉ tồn tại khi có số thật — thiếu dữ liệu thì thuộc tính vắng mặt. */
  protected readonly ariaValueNow = computed<number | null>(() =>
    this.hasValue() ? this.clamped() : null,
  );
}

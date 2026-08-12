import { Component, computed, input, output } from '@angular/core';

import { DashboardMode, IPeriodOption } from '../../models/dashboard.model';

const MONTH_LABELS = [
  'Tháng 1', 'Tháng 2', 'Tháng 3', 'Tháng 4', 'Tháng 5', 'Tháng 6',
  'Tháng 7', 'Tháng 8', 'Tháng 9', 'Tháng 10', 'Tháng 11', 'Tháng 12'
];

export const ALL_WEEKS_VALUE = '__ALL__';

/**
 * Toolbar "weekbar" — segmented Tuần/Tháng + dropdown Năm + dropdown chọn kỳ
 * cụ thể (hoặc "Tất cả" = `mode='year'`) + nút "Xuất báo cáo" ghim cố định
 * phải. Dumb component: mọi lựa chọn chỉ phát `output()`, page cha quyết
 * định gọi lại `DashboardService.getDashboard()`.
 *
 * **Khác thiết kế gốc**: dropdown Năm nay LUÔN hiện (không chỉ khi bật "Tất
 * cả") — API thật (`GET /api/dashboard/periods?year=`) chỉ liệt kê kỳ-tuần
 * theo TỪNG năm, không có endpoint "mọi kỳ mọi năm" như bản thiết kế ban đầu
 * giả định, nên cần chọn năm trước để nạp đúng danh sách tuần.
 *
 * **[FIX] Vì sao mỗi `<option>` trong template có thêm `[selected]` thay vì
 * chỉ dựa vào `[value]` trên `<select>`**: với `<select>` gốc (không dùng
 * `NgModel`/reactive forms), khi các `<option>` được sinh động bằng `@for`,
 * Angular set property `value` của `<select>` trước khi trình duyệt kịp có
 * đủ `<option>` con để khớp — trình duyệt tự fallback chọn option ĐẦU TIÊN,
 * ghi đè lựa chọn đúng ngay sau đó khi các `<option>` được thêm vào DOM. Bug
 * quan sát được: chuyển sang "Tháng" lần đầu, dropdown hiện "Tháng 1" dù data
 * đang tải đúng là tháng hiện tại. Khắc phục triệt để bằng cách đánh dấu
 * trực tiếp đúng `<option>` qua `[selected]="điều kiện khớp"` — không phụ
 * thuộc thứ tự tạo DOM.
 */
@Component({
  selector: 'app-period-toolbar',
  standalone: true,
  templateUrl: './period-toolbar.html',
  styleUrl: './period-toolbar.scss'
})
export class PeriodToolbar {
  readonly mode = input.required<DashboardMode>();
  readonly periodLabel = input('Kỳ đang xem:');
  readonly weekDate = input<string | null>(null);
  readonly weeksInYear = input<IPeriodOption[]>([]);
  readonly years = input<number[]>([]);
  readonly year = input(new Date().getFullYear());
  readonly monthValue = input(new Date().getMonth() + 1);

  readonly modeChange = output<DashboardMode>();
  readonly yearChange = output<number>();
  readonly weekSelect = output<string>();
  readonly monthChange = output<number>();
  readonly exportReport = output<void>();

  readonly allWeeksValue = ALL_WEEKS_VALUE;
  readonly monthLabels = MONTH_LABELS;
  readonly monthNumbers = Array.from({ length: 12 }, (_, i) => i + 1);

  /**
   * `weekDate` (ngày thật, "YYYY-MM-DD") và `weeksInYear[].Value` (kỳ ISO,
   * "YYYY-Www") là 2 định dạng khác nhau — không thể so trực tiếp. Tìm đúng
   * `Value` của tuần đang chọn để đánh dấu `<option>` tương ứng qua
   * `[selected]` (xem lý do dùng `[selected]` thay vì chỉ `[value]` trên
   * `<select>` trong `period-toolbar.html`).
   */
  readonly selectedWeekValue = computed(() => {
    if (this.mode() === 'year') return this.allWeeksValue;
    const date = this.weekDate();
    if (!date) return '';
    const match = this.weeksInYear().find((w) => w.Date === date);
    return match ? match.Value : '';
  });

  formatDate(value: string): string {
    const [y, m, d] = value.slice(0, 10).split('-');
    return `${d}/${m}/${y}`;
  }
}

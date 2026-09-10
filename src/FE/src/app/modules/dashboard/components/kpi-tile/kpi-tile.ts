import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

/**
 * Sắc thái tô cho CON SỐ — và chỉ con số. Không rule nào tô nhãn, chú thích hay chính thẻ card
 * (`doc/Design/Frontend/PlatformManager/Components/KpiTile.md` § Variants).
 */
export type KpiTone = 'default' | 'good' | 'warn' | 'bad';

/**
 * Ô chỉ số: nhãn → giá trị → chú thích. Một `Card` mang thêm lớp `.kpi`.
 *
 * Hợp đồng đầy đủ: `doc/Design/Frontend/PlatformManager/Components/KpiTile.md`.
 *
 * ## Vì sao host là `display: contents`
 *
 * Dải KPI là một lưới `repeat(5, 1fr)` do TRANG cha khai (`.kpis`). Nếu thẻ `<app-kpi-tile>` là
 * một hộp thật, nó chen vào giữa lưới và `.card` — lưới sẽ chia ô cho thẻ bọc, còn card bên trong
 * co theo nội dung. `display: contents` gỡ thẻ bọc khỏi cây bố cục để `.card` là con trực tiếp của
 * lưới. Đây là giá trị đã duyệt, không phải mẹo: § Anatomy, hàng `host app-kpi-tile`.
 *
 * ⚠️ **Hệ quả đã biết, ghi lại để không ai "sửa nhầm" ở tầng này:** vì host là `display: contents`,
 * một rule `:last-child` viết ở lưới cha sẽ khớp MỌI card chứ không khớp card thứ năm. Bản dựng đã
 * duyệt giữ nguyên hành vi đó (§ Normalize on redesign #3) và chỗ sửa nằm ở SCSS của dải KPI, không
 * nằm ở đây.
 *
 * ## Component này KHÔNG định dạng số
 *
 * `value` là chuỗi ĐÃ ĐỊNH DẠNG. `82,1%`, `↑ 2,3 đ.%` và `26/62` là ba khuôn khác hẳn nhau; gói
 * cả ba vào đây nghĩa là ô KPI phải hiểu đơn vị của từng ô, tức nó không còn dùng lại được. Dấu
 * phẩy thập phân được quyết định MỘT chỗ ở trang cha, cùng chỗ với mọi con số khác của màn hình
 * (§ Do / Don't).
 *
 * ## `—` KHÁC `0`
 *
 * Bỏ trống `value` (hoặc truyền `null`) ⇒ ô hiện `—`. Đây là ca THƯỜNG GẶP chứ không phải ca hiếm:
 * ngay sau khi import, ô 1 và ô 2 chưa có gì để tính (`spec/dashboard-dti/business-rules.md` §1.6).
 * Vẽ `0` ở đó là tuyên bố "tiến độ toàn xã: 0%" ngay sau khi nạp một file có 26 chỉ tiêu đã hoàn
 * thành — một câu sai, hiển thị tự tin, và không ai kiểm lại vì nó trông như số bình thường.
 */
@Component({
  selector: 'app-kpi-tile',
  standalone: true,
  imports: [TranslatePipe],
  templateUrl: './kpi-tile.html',
  styleUrl: './kpi-tile.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class KpiTile {
  /** Nhãn ô — câu ĐÃ DỊCH do trang cha truyền xuống, không phải khoá dịch. */
  readonly label = input<string>('');

  /**
   * Con số đã định dạng. Rỗng / `null` ⇒ hiện `—`.
   *
   * 🛑 Đừng "an toàn hoá" bằng cách để trang cha truyền `'0'` khi chưa có dữ liệu — xem JSDoc của
   * class.
   */
  readonly value = input<string | null | undefined>(null);

  /** Sắc thái tô cho con số. */
  readonly tone = input<KpiTone>('default');

  /**
   * Chú thích dưới con số. Rỗng ⇒ không render `.sub`, và ô mất 30px chiều cao dự trữ — bản dựng
   * đã duyệt không có ô nào bỏ trống, nhánh này tồn tại nhưng chưa dùng (§ Variants, hàng cuối).
   */
  readonly sub = input<string>('');

  /** Có số thật để hiện không. Chuỗi toàn khoảng trắng tính là KHÔNG. */
  protected readonly hasValue = computed<boolean>(() => (this.value() ?? '').trim().length > 0);

  protected readonly subText = computed<string>(() => this.sub().trim());
}

import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { ChangeDetectionStrategy, Component, PLATFORM_ID, computed, inject, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { ChartModule } from 'primeng/chart';
import type { ChartData, ChartOptions } from 'chart.js';

/**
 * Một điểm trên đường xu hướng.
 *
 * `Label` là chuỗi ĐÃ ĐỊNH DẠNG cho trục X — khoảng ngày `06/07 – 12/07` ở chế độ tuần, `Th.1`…
 * `Th.12` ở chế độ tháng/năm (chốt T7). Component này KHÔNG tự dựng nhãn kỳ: khuôn nhãn là luật
 * nghiệp vụ, chủ ở `spec/dashboard-dti/business-rules.md` §6.2.
 *
 * Casing `PascalCase` + prefix `I` theo quy ước model app
 * (doc/huong_dan/quy-uoc/fe-api-client.md §"Quy tắc cứng" mục 2) — đây là model, không phải DTO.
 */
export interface ITrendPoint {
  readonly Label: string;
  /**
   * `null` = kỳ KHÔNG có dữ liệu (Q44, 2026-09-10). Phần tử vẫn phải có mặt trong `points`.
   *
   * 🛑 Đừng "dọn" các phần tử `null` cho gọn, và đừng kẹp chúng về `0`. Lý do ở ràng buộc 2 trong
   * JSDoc của class.
   */
  readonly Value: number | null;
}

/** Trục Y ghim cứng, không tự co giãn theo dữ liệu. */
const Y_MIN = 0;
const Y_MAX = 100;

/** Bốn vạch chia + đáy ⇒ 0 / 25 / 50 / 75 / 100, đúng năm đường lưới của bản dựng đã duyệt. */
const Y_STEP = 25;

/**
 * Độ mờ của vùng tô dưới đường — chính là vai trò `chart-series-1-fill`
 * (`doc/Design/Frontend/PlatformManager/Tokens/colors.md` § Chart Palette: *brand ở 12% alpha*).
 *
 * 🛑 Nó nằm ở đây, trong TS, chứ KHÔNG thành một custom property trong `styles.scss`, và đó là
 * quyết định đã ghi trong chính file token: *"None of these four is a CSS custom property, and
 * none should become one"* — kèm phép kiểm `grep -c 'chart' src/FE/src/styles.scss` PASS = 0. Lý
 * do: `<canvas>` không phân giải được `var(--x)`, nên bốn vai trò `chart-*` chỉ có thể tồn tại
 * dưới dạng chuỗi literal đọc ra lúc chạy — thêm chúng vào `:root` là dựng bản sao thứ hai của ba
 * token nền (`--brand`, `--muted`, `--line`) rồi để hai bản lệch nhau khi đổi bảng màu.
 */
const SERIES_FILL_ALPHA = 0.12;

/** Cỡ chữ nhãn trục, literal ngoài thang token — đúng như bản dựng đã duyệt. */
const AXIS_FONT_SIZE = 11;

/** Bề dày đường và bán kính điểm, cũng là literal của bản dựng. */
const LINE_WIDTH = 2;
const POINT_RADIUS = 4;
const POINT_BORDER_WIDTH = 1.5;

/**
 * Ghép alpha vào một màu đọc từ token.
 *
 * Nhận `#rgb` / `#rrggbb`; mọi dạng khác trả về nguyên xi (chart.js tự hiểu, và một chuỗi rỗng —
 * ca không phải trình duyệt — vẫn là chuỗi rỗng chứ không thành `rgba(NaN…)`).
 */
function withAlpha(color: string, alpha: number): string {
  const hex = color.trim();
  if (!hex.startsWith('#')) return hex;

  const body = hex.slice(1);
  const full =
    body.length === 3
      ? body
          .split('')
          .map((char) => char + char)
          .join('')
      : body;
  if (full.length !== 6) return hex;

  const red = Number.parseInt(full.slice(0, 2), 16);
  const green = Number.parseInt(full.slice(2, 4), 16);
  const blue = Number.parseInt(full.slice(4, 6), 16);
  return `rgba(${red}, ${green}, ${blue}, ${alpha})`;
}

/**
 * Biểu đồ đường xu hướng — biểu đồ DUY NHẤT của app, một chuỗi số liệu, không chú giải.
 *
 * Hợp đồng đầy đủ: `doc/Design/Frontend/PlatformManager/Components/TrendChart.md`.
 * Quy tắc chung cho biểu đồ: `doc/huong_dan/wiki-core/fe/12-charting.md`.
 *
 * ## Bốn ràng buộc, mỗi cái chặn một cách đọc sai số liệu
 *
 * 1. **Trục Y ghim `[0, 100]`.** Trục tự co giãn biến một biến động 2 điểm thành một vách đá.
 * 2. **Mỗi kỳ một ô trên trục; kỳ rỗng để `null`, đường ngắt đúng chỗ đó** — không nội suy,
 *    không `spanGaps`. Một đường liền qua chỗ không có số liệu là số liệu bịa
 *    (`spec/dashboard-dti/business-rules.md` §1.5).
 *
 *    > 🔄 **LẬT 2026-09-10 (Q44).** Ràng buộc này trước đây đọc *"Chỉ vẽ điểm CÓ dữ liệu — không
 *    > chèn `null` cho đủ 52 tuần"*, và nó **sai về kỹ thuật**: trên trục **category** của
 *    > `chart.js`, bỏ hẳn một điểm thì hai điểm kề nhau được nối THẲNG — trục không có ô nào cho
 *    > kỳ bị bỏ, nên không có khoảng đứt nào để nhìn thấy. Chỉ một `null` nằm đúng ô của kỳ (khi
 *    > `spanGaps` tắt) mới ngắt được đường. BE nay trả **đủ** các kỳ của phạm vi
 *    > (`doc/contracts/dashboard.md` DB-1 luật 2); vế "không nội suy" giữ nguyên.
 * 3. **`tension: 0`** — đường gấp khúc, không làm mượt. Đường cong bịa ra những giá trị trung
 *    gian chưa từng được đo.
 * 4. **Giá trị kẹp `[0, 100]`** trước khi vẽ.
 *
 * ## Màu lấy từ token, đọc LÚC CHẠY
 *
 * `<canvas>` không hiểu `var(--brand)`: chart.js đưa thẳng chuỗi màu vào `fillStyle`, nên
 * `'var(--muted)'` sẽ **im lặng** vẽ ra màu đen mặc định. Vì vậy component đọc ba token nền qua
 * `getComputedStyle` một lần rồi truyền chuỗi literal xuống thư viện — đúng cơ chế mà bốn vai trò
 * `chart-*` ở `Tokens/colors.md` § Chart Palette sinh ra để mô tả.
 *
 * ## Huỷ chart khi component chết
 *
 * `p-chart` tự gọi `chart.destroy()` trong `onDestroy` của nó (`primeng/chart`), nên component này
 * KHÔNG giữ một `Chart` thứ hai để tự huỷ — hai chỗ cùng huỷ một instance là chỗ sinh lỗi
 * "destroy sau destroy". `trend-chart.spec.ts` kiểm điều đó bằng `Chart.getChart(canvas)` sau khi
 * huỷ fixture, chứ không tin vào câu này.
 *
 * ## Nạp chậm là việc của TRANG, không phải của component
 *
 * Đây là thứ nặng nhất màn hình và thường nằm dưới nếp gấp. Trang cha bọc thẻ này trong
 * `@defer (on viewport)` kèm placeholder giữ đủ 220px; `.chart-skeleton` thuộc về trang, không
 * thuộc component này (§ Do / Don't).
 */
@Component({
  selector: 'app-trend-chart',
  standalone: true,
  imports: [ChartModule, TranslatePipe],
  templateUrl: './trend-chart.html',
  styleUrl: './trend-chart.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TrendChart {
  private readonly document = inject(DOCUMENT);
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));

  /**
   * Chuỗi điểm, đã sắp xếp theo thời gian, **đủ mọi kỳ của phạm vi** — kỳ rỗng mang `Value: null`.
   *
   * 🛑 Đừng lọc bỏ phần tử `null` trước khi truyền vào — xem ràng buộc 2 ở JSDoc của class.
   */
  readonly points = input<readonly ITrendPoint[]>([]);

  /**
   * Câu tóm tắt xu hướng cho trình đọc màn hình — *kết luận*, không phải mô tả hình dạng.
   *
   * Trang cha tính từ CHÍNH dữ liệu đã truyền vào `points`, không viết tay một câu tĩnh: hai bên
   * lệch nhau còn tệ hơn không có nhãn nào (`wiki-core/fe/12-charting.md` §Accessibility).
   */
  readonly summary = input<string>('');

  /** `localeId` của ngôn ngữ đang chọn (cổng G4 — không `inject(LanguageService)`). */
  readonly localeId = input<string>('vi');

  /**
   * "Có gì để vẽ không" — đo bằng *"có kỳ nào MANG GIÁ TRỊ không"*, **không** bằng *"mảng có phần
   * tử không"* (`spec/dashboard-dti/ui-spec.md` §5.3, sửa theo Q44).
   *
   * Sau Q44 mảng gần như không bao giờ rỗng: ngay sau import, BE vẫn trả đủ 12 kỳ với `value`
   * vắng mặt ở cả 12. Đo bằng `length` khi đó sẽ vẽ ra một cặp trục trống hoàn toàn — đúng thứ
   * trạng thái rỗng sinh ra để thay thế.
   */
  protected readonly hasData = computed<boolean>(() => this.points().some((point) => point.Value !== null));

  /**
   * Ba token nền đọc một lần từ `:root`, cộng màu mặt card cho viền điểm.
   *
   * `computed` không phụ thuộc signal nào ⇒ chỉ chạy một lần. Đúng ý: bảng màu là tĩnh, còn
   * `getComputedStyle` thì buộc trình duyệt tính lại layout nếu gọi mỗi lần vẽ.
   */
  private readonly palette = computed(() => {
    const series = this.readToken('--brand');
    return {
      /** Vai trò `chart-series-1` — đường và ruột điểm. */
      series,
      /** Vai trò `chart-series-1-fill` — vùng tô dưới đường. */
      seriesFill: withAlpha(series, SERIES_FILL_ALPHA),
      /** Vai trò `chart-axis-label` — nhãn vạch CẢ HAI trục. */
      axisLabel: this.readToken('--muted'),
      /** Vai trò `chart-grid` — lưới ngang; trục X không vẽ lưới. */
      grid: this.readToken('--line'),
      /** Viền điểm, để điểm đọc như đĩa tròn đục ra khỏi mặt card. */
      card: this.readToken('--card'),
    };
  });

  protected readonly chartData = computed<ChartData<'line'>>(() => {
    const palette = this.palette();
    const points = this.points();

    return {
      labels: points.map((point) => point.Label),
      datasets: [
        {
          // `null` đi thẳng vào dataset và giữ đúng ô của kỳ; chỉ giá trị THẬT mới bị kẹp.
          data: points.map((point) => (point.Value === null ? null : Math.min(Y_MAX, Math.max(Y_MIN, point.Value)))),
          borderColor: palette.series,
          backgroundColor: palette.seriesFill,
          pointBackgroundColor: palette.series,
          pointBorderColor: palette.card,
          pointBorderWidth: POINT_BORDER_WIDTH,
          pointRadius: POINT_RADIUS,
          borderWidth: LINE_WIDTH,
          tension: 0,
          fill: true,
          // 🛑 Khai TƯỜNG MINH dù `false` đã là mặc định của chart.js: bật nó lên là nối liền qua
          // đúng chỗ không có số liệu, tức xoá mất thứ Q44 vừa dựng ra. Một dòng ở đây làm ý định
          // hiện rõ trên diff nếu có người đổi.
          spanGaps: false,
        },
      ],
    };
  });

  protected readonly chartOptions = computed<ChartOptions<'line'>>(() => {
    const palette = this.palette();
    const percent = this.percentFormatter();

    return {
      responsive: true,
      // BẮT BUỘC đi kèm chiều cao cố định 220px khai ở SCSS: để `true` (mặc định) thì chart.js giữ
      // tỉ lệ khung và bỏ qua chiều cao của thẻ chứa.
      maintainAspectRatio: false,
      // Một chuỗi số liệu thì chú giải chỉ lặp lại tiêu đề card.
      plugins: { legend: { display: false } },
      scales: {
        x: {
          // Trục X KHÔNG vẽ lưới — chỉ trục Y có.
          grid: { display: false },
          ticks: { color: palette.axisLabel, font: { size: AXIS_FONT_SIZE } },
        },
        y: {
          min: Y_MIN,
          max: Y_MAX,
          grid: { color: palette.grid },
          ticks: {
            color: palette.axisLabel,
            font: { size: AXIS_FONT_SIZE },
            stepSize: Y_STEP,
            callback: (value) => percent.format(Number(value) / 100),
          },
        },
      },
    };
  });

  /** Giá trị từng điểm cho bảng thay thế dành cho trình đọc màn hình. */
  protected readonly rows = computed<readonly { Label: string; Text: string | null }[]>(() => {
    const percent = this.percentFormatter();
    return this.points().map((point) => ({
      Label: point.Label,
      // `null` ⇒ ô để trống chỗ này và template điền `—`: bảng thay thế phải nói được "kỳ này
      // không có số", chứ không lặng lẽ in `0%`.
      Text: point.Value === null ? null : percent.format(Math.min(Y_MAX, Math.max(Y_MIN, point.Value)) / 100),
    }));
  });

  private percentFormatter(): Intl.NumberFormat {
    return new Intl.NumberFormat(this.localeId(), {
      style: 'percent',
      minimumFractionDigits: 0,
      maximumFractionDigits: 1,
    });
  }

  /**
   * Đọc một custom property từ `:root`.
   *
   * Ngoài trình duyệt trả chuỗi rỗng — chart.js khi đó lùi về màu mặc định của nó, và ca đó không
   * bao giờ nhìn thấy được vì `p-chart` cũng chỉ dựng canvas trên trình duyệt.
   */
  private readToken(name: string): string {
    if (!this.isBrowser) return '';
    return getComputedStyle(this.document.documentElement).getPropertyValue(name).trim();
  }
}

import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { provideTranslateService } from '@ngx-translate/core';
import Chart from 'chart.js/auto';
import { UIChart } from 'primeng/chart';
import { APP_I18N } from '../../../../app.config';
import { CORE_I18N } from '../../../../core/i18n/core-i18n';
import { useTranslationsInTest } from '../../../../core/i18n/i18n.testing';
import { ITrendPoint, TrendChart } from './trend-chart';

/** Sáu kỳ liên tiếp — cùng chuỗi nhãn mà bản dựng đã duyệt vẽ trên trục X ở chế độ tuần. */
const POINTS: readonly ITrendPoint[] = [
  { Label: '06/07 – 12/07', Value: 68.9 },
  { Label: '13/07 – 19/07', Value: 71.7 },
  { Label: '20/07 – 26/07', Value: 74.4 },
  { Label: '27/07 – 02/08', Value: 77.6 },
  { Label: '03/08 – 09/08', Value: 79.8 },
  { Label: '10/08 – 16/08', Value: 82.1 },
];

/**
 * Bốn nhóm phép kiểm, mỗi nhóm chặn một cách đọc sai số liệu hoặc một cách rò tài nguyên:
 *
 *  1. **Trục Y ghim `[0, 100]`** — trục tự co giãn biến biến động 2 điểm thành một vách đá.
 *  2. **Chỉ vẽ điểm CÓ dữ liệu**, và giá trị bị kẹp — không nội suy, không chèn `null`.
 *  3. **Màu là chuỗi literal đọc từ token lúc chạy.** `<canvas>` không phân giải `var(--x)`:
 *     truyền thẳng `'var(--brand)'` xuống chart.js thì nó vẽ ra màu mặc định và KHÔNG báo gì.
 *  4. **Chart bị huỷ khi component chết.** Rò canvas là lỗi kinh điển của chart.js trong Angular;
 *     phép kiểm hỏi thẳng registry của chart.js chứ không tin lời hứa của thư viện.
 */
describe('TrendChart', () => {
  let fixture: ComponentFixture<TrendChart>;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideTranslateService(),
        { provide: CORE_I18N, useValue: APP_I18N },
      ],
    });
    await useTranslationsInTest();
    fixture = TestBed.createComponent(TrendChart);
  });

  function render(inputs: Record<string, unknown>): HTMLElement {
    for (const [name, value] of Object.entries(inputs)) {
      fixture.componentRef.setInput(name, value);
    }
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  /** Thể hiện `p-chart` đang gắn — nơi đọc ra đúng cấu hình đã truyền xuống chart.js. */
  function chartComponent(): UIChart {
    return fixture.debugElement.query(By.directive(UIChart)).componentInstance as UIChart;
  }

  function tokenValue(name: string): string {
    return getComputedStyle(document.documentElement).getPropertyValue(name).trim();
  }

  // ---------------------------------------------------------------- trạng thái mặc định
  describe('có dữ liệu', () => {
    it('dựng p-chart trong .chart-wrap, không có câu trạng thái rỗng', () => {
      const host = render({ points: POINTS });

      expect(host.querySelector('.chart-wrap')).not.toBeNull();
      expect(host.querySelector('p-chart canvas')).not.toBeNull();
      expect(host.querySelector('.chart-wrap > p.muted')).toBeNull();
    });

    it('một chuỗi số liệu, không chú giải', () => {
      render({ points: POINTS });
      const chart = chartComponent();

      expect(chart.type).toBe('line');
      expect(chart.data.datasets.length).toBe(1);
      expect(chart.options.plugins.legend.display).toBeFalse();
    });

    it('đường gấp khúc, có tô nền, điểm bán kính 4', () => {
      render({ points: POINTS });
      const dataset = chartComponent().data.datasets[0];

      expect(dataset.tension).toBe(0);
      expect(dataset.fill).toBeTrue();
      expect(dataset.pointRadius).toBe(4);
    });
  });

  // ---------------------------------------------------------------- trục
  describe('trục', () => {
    it('🛑 trục Y ghim [0, 100] — không co giãn theo dữ liệu', () => {
      render({ points: POINTS });
      const scales = chartComponent().options.scales;

      expect(scales.y.min).toBe(0);
      expect(scales.y.max).toBe(100);
    });

    it('vạch Y hậu tố % và bước 25 ⇒ 0/25/50/75/100', () => {
      render({ points: POINTS });
      const ticks = chartComponent().options.scales.y.ticks;

      expect(ticks.stepSize).toBe(25);
      expect(ticks.callback(75)).toBe('75%');
      expect(ticks.callback(0)).toBe('0%');
    });

    it('lưới chỉ vẽ ở trục Y — trục X không vẽ lưới', () => {
      render({ points: POINTS });
      const scales = chartComponent().options.scales;

      expect(scales.x.grid.display).toBeFalse();
      expect(scales.y.grid.color).toBe(tokenValue('--line'));
    });

    it('nhãn trục X lấy nguyên xi từ dữ liệu — component không tự dựng nhãn kỳ', () => {
      render({ points: POINTS });

      expect(chartComponent().data.labels).toEqual([
        '06/07 – 12/07',
        '13/07 – 19/07',
        '20/07 – 26/07',
        '27/07 – 02/08',
        '03/08 – 09/08',
        '10/08 – 16/08',
      ]);
    });

    it('chế độ tháng: nhãn Th.1 … Th.12 đi thẳng qua, không bị đổi thành khoảng ngày', () => {
      render({
        points: [
          { Label: 'Th.1', Value: 10 },
          { Label: 'Th.2', Value: 20 },
        ],
      });

      expect(chartComponent().data.labels).toEqual(['Th.1', 'Th.2']);
    });
  });

  // ---------------------------------------------------------------- dữ liệu
  describe('🛑 chỉ vẽ điểm CÓ dữ liệu', () => {
    it('số điểm vẽ ra đúng bằng số điểm truyền vào — không chèn thêm cho đủ 52 tuần', () => {
      render({ points: POINTS.slice(0, 3) });
      const chart = chartComponent();

      expect(chart.data.labels.length).toBe(3);
      expect(chart.data.datasets[0].data.length).toBe(3);
    });

    it('không có giá trị null nào lọt vào chuỗi', () => {
      render({ points: POINTS });

      expect(chartComponent().data.datasets[0].data.every(Number.isFinite)).toBeTrue();
    });

    it('giá trị bị kẹp về [0, 100]', () => {
      render({
        points: [
          { Label: 'a', Value: -20 },
          { Label: 'b', Value: 140 },
        ],
      });

      expect(chartComponent().data.datasets[0].data).toEqual([0, 100]);
    });
  });

  // ---------------------------------------------------------------- màu từ token
  describe('🛑 màu đọc từ token lúc chạy, không phải chuỗi var()', () => {
    it('đường và điểm dùng đúng giá trị của --brand', () => {
      render({ points: POINTS });
      const dataset = chartComponent().data.datasets[0];

      expect(dataset.borderColor).toBe(tokenValue('--brand'));
      expect(dataset.pointBackgroundColor).toBe(tokenValue('--brand'));
    });

    it('ca đối chứng — KHÔNG chuỗi màu nào còn dạng var(...), thứ canvas không hiểu', () => {
      render({ points: POINTS });
      const dataset = chartComponent().data.datasets[0];

      for (const color of [
        dataset.borderColor,
        dataset.backgroundColor,
        dataset.pointBackgroundColor,
        dataset.pointBorderColor,
      ]) {
        expect(String(color)).not.toContain('var(');
      }
    });

    it('vùng tô là --brand ở 12% — vai trò chart-series-1-fill', () => {
      render({ points: POINTS });

      expect(String(chartComponent().data.datasets[0].backgroundColor)).toMatch(
        /^rgba\(15, 91, 215, 0\.12\)$/,
      );
    });

    it('nhãn vạch CẢ HAI trục dùng --muted', () => {
      render({ points: POINTS });
      const scales = chartComponent().options.scales;

      expect(scales.x.ticks.color).toBe(tokenValue('--muted'));
      expect(scales.y.ticks.color).toBe(tokenValue('--muted'));
    });
  });

  // ---------------------------------------------------------------- trạng thái rỗng
  describe('không kỳ nào có số liệu', () => {
    it('hiện MỘT câu, KHÔNG vẽ cặp trục trống', () => {
      const host = render({ points: [] });

      expect(host.querySelector('p-chart')).toBeNull();
      expect(host.querySelector('canvas')).toBeNull();
      expect(host.querySelector('.chart-wrap > p.muted')?.textContent?.trim()).toBe(
        'Chưa có kỳ nào có dữ liệu để vẽ biểu đồ.',
      );
    });

    it('vùng bọc vẫn còn — chỗ của biểu đồ không biến mất', () => {
      expect(render({ points: [] }).querySelector('.chart-wrap')).not.toBeNull();
    });
  });

  // ---------------------------------------------------------------- trợ năng
  describe('trợ năng — canvas là vùng trống với trình đọc màn hình', () => {
    it('câu tóm tắt của trang cha đi vào aria-label của canvas', () => {
      const host = render({ points: POINTS, summary: 'Tiến độ chung tăng đều qua 6 tuần.' });

      expect(host.querySelector('canvas')?.getAttribute('aria-label')).toBe(
        'Tiến độ chung tăng đều qua 6 tuần.',
      );
    });

    it('thiếu câu tóm tắt vẫn có nhãn dự phòng, không để canvas không tên', () => {
      const host = render({ points: POINTS, summary: '' });

      expect(host.querySelector('canvas')?.getAttribute('aria-label')).toBe(
        'Biểu đồ đường tiến độ chung theo các kỳ đã lưu',
      );
    });

    it('bảng thay thế liệt kê ĐỦ mọi điểm, ẩn thị giác nhưng còn trong cây trợ năng', () => {
      const host = render({ points: POINTS });
      const table = host.querySelector('table.sr-only') as HTMLTableElement;
      const rows = table.querySelectorAll('tbody tr');

      expect(table).not.toBeNull();
      expect(rows[0].querySelectorAll('td').length).toBe(POINTS.length);
      expect(rows[1].querySelectorAll('td').length).toBe(POINTS.length);
      expect(rows[0].querySelectorAll('td')[0].textContent?.trim()).toBe('06/07 – 12/07');
      expect(rows[1].querySelectorAll('td')[5].textContent?.trim()).toBe('82,1%');
    });

    it('🛑 bảng thay thế KHÔNG dùng display:none — nếu không trình đọc màn hình cũng bỏ qua', () => {
      const host = render({ points: POINTS });
      const table = host.querySelector('table.sr-only') as HTMLElement;
      const style = getComputedStyle(table);

      expect(style.display).not.toBe('none');
      expect(style.visibility).not.toBe('hidden');
    });
  });

  // ---------------------------------------------------------------- vòng đời
  describe('🛑 huỷ chart khi component chết', () => {
    it('registry của chart.js không còn giữ canvas sau khi fixture bị huỷ', () => {
      const host = render({ points: POINTS });
      const canvas = host.querySelector('canvas') as HTMLCanvasElement;

      expect(Chart.getChart(canvas)).toBeDefined();

      fixture.destroy();

      expect(Chart.getChart(canvas)).toBeUndefined();
    });

    it('ca đối chứng — trước khi huỷ thì registry CÓ giữ, nên phép kiểm trên không xanh khống', () => {
      const canvas = render({ points: POINTS }).querySelector('canvas') as HTMLCanvasElement;

      expect(Chart.getChart(canvas)).toBeDefined();
    });
  });
});

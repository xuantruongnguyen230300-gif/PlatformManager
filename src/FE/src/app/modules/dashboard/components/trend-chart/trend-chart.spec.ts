import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ChartData } from 'chart.js';
import { ITrendPoint } from '../../models/dashboard.model';
import { TrendChart } from './trend-chart';

function pt(label: string, value: number | null): ITrendPoint {
  return { Label: label, Value: value };
}

/**
 * Hai lỗi đã sửa 2026-08-22, cả hai đều thuộc loại "trông vẫn chạy bình thường":
 *
 * 1. KHOẢNG TRỐNG BỊ NUỐT. `chartData` lọc `Value === null` ra khỏi mảng TRƯỚC khi dựng `labels`,
 *    nên một kỳ thiếu số liệu biến mất khỏi trục x thay vì để lại chỗ trống. Hai kỳ không liền
 *    nhau bị vẽ sát nhau và nối bằng một đoạn thẳng liền — biểu đồ khẳng định một tính liên tục
 *    không có thật. Lọc xong thì `spanGaps: false` cũng thành mã chết: mảng data không còn `null`
 *    nào để nó xử lý.
 *
 * 2. CANVAS KHÔNG TÊN. PrimeNG render `<canvas role="img">`, mà `role="img"` bỏ qua mọi nội dung
 *    con. Không truyền `ariaLabel` thì trình đọc màn hình gặp một ảnh trống rỗng.
 *
 * Mỗi lỗi đi kèm một test ĐỐI CHỨNG cho ca ngược lại. Chỉ test ca có khoảng trống thì một bản sửa
 * quá tay (chèn `null` bừa) vẫn xanh; chỉ test ca đủ dữ liệu thì bản cũ cũng xanh.
 */
describe('TrendChart', () => {
  let fixture: ComponentFixture<TrendChart>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TrendChart],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();

    fixture = TestBed.createComponent(TrendChart);
  });

  function render(points: ITrendPoint[]): void {
    fixture.componentRef.setInput('points', points);
    fixture.detectChanges();
  }

  /** `chartData` là `protected` (chỉ template dùng), nhưng hình dạng dataset LÀ hợp đồng với Chart.js. */
  function dataset(): { labels: unknown[]; data: (number | null)[] } {
    const cmp = fixture.componentInstance as unknown as { chartData: () => ChartData<'line'> };
    const d = cmp.chartData();
    return { labels: d.labels ?? [], data: d.datasets[0].data as (number | null)[] };
  }

  function canvasAriaLabel(): string | null {
    const canvas = fixture.nativeElement.querySelector('canvas[role="img"]') as HTMLElement | null;
    return canvas?.getAttribute('aria-label') ?? null;
  }

  describe('kỳ thiếu số liệu phải GIỮ CHỖ trên trục x', () => {
    it('giữ label của kỳ thiếu và đặt null vào đúng vị trí đó', () => {
      render([pt('W1', 50), pt('W2', null), pt('W3', 70)]);

      const { labels, data } = dataset();
      // Cả 3 kỳ đều còn trên trục — W2 không được phép biến mất.
      expect(labels).toEqual(['W1', 'W2', 'W3']);
      // null nằm ĐÚNG giữa, để `spanGaps: false` ngắt nét tại đó.
      expect(data).toEqual([50, null, 70]);
    });

    it('ĐỐI CHỨNG — chuỗi đủ dữ liệu không bị chèn null nào', () => {
      render([pt('W1', 50), pt('W2', 60)]);

      const { labels, data } = dataset();
      expect(labels).toEqual(['W1', 'W2']);
      expect(data).toEqual([50, 60]);
    });

    it('vẫn clamp giá trị về [0, 100] và không clamp nhầm null thành 0', () => {
      render([pt('W1', -5), pt('W2', null), pt('W3', 150)]);

      expect(dataset().data).toEqual([0, null, 100]);
    });
  });

  describe('canvas phải có tên cho trình đọc màn hình', () => {
    it('mô tả số kỳ và hai đầu chuỗi', () => {
      render([pt('W1', 50), pt('W2', 60)]);

      const label = canvasAriaLabel();
      expect(label).toContain('2 kỳ có số liệu');
      expect(label).toContain('W1');
      expect(label).toContain('W2');
      // Không kỳ nào thiếu ⇒ không nhắc tới khoảng trống.
      expect(label).not.toContain('chưa có số liệu');
    });

    it('nói rõ có bao nhiêu kỳ thiếu số liệu', () => {
      render([pt('W1', 50), pt('W2', null), pt('W3', 70)]);

      expect(canvasAriaLabel()).toContain('1 kỳ chưa có số liệu');
    });

    it('ĐỐI CHỨNG — không dữ liệu thì không render canvas trống, mà hiện câu giải thích', () => {
      render([pt('W1', null), pt('W2', null)]);

      // Không có canvas ⇒ không có ảnh vô danh nào để trình đọc màn hình vấp phải.
      expect(fixture.nativeElement.querySelector('canvas')).toBeNull();
      expect((fixture.nativeElement.textContent ?? '').trim()).toContain(
        'Chưa có đủ dữ liệu để vẽ biểu đồ.',
      );
    });
  });
});

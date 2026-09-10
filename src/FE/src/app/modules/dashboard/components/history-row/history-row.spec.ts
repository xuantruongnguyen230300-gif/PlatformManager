import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideTranslateService } from '@ngx-translate/core';
import { APP_I18N } from '../../../../app.config';
import { CORE_I18N } from '../../../../core/i18n/core-i18n';
import { useTranslationsInTest } from '../../../../core/i18n/i18n.testing';
import { HistoryRow } from './history-row';

/** Tuần ISO 33/2026 — đúng kỳ mà bản dựng đã duyệt vẽ ở hàng trên cùng. */
const WEEK_START = new Date(2026, 7, 10);
const WEEK_END = new Date(2026, 7, 16);

/** Tháng 8/2026 — kỳ dùng để chứng minh ô 1 KHÔNG đổi khuôn giữa hai chế độ. */
const MONTH_START = new Date(2026, 7, 1);
const MONTH_END = new Date(2026, 7, 31);

/**
 * Ba thứ được canh ở đây:
 *
 *  1. **Ô 1 có MỘT khuôn cho cả tuần lẫn tháng** — `{dd/MM} – {dd/MM/yyyy}`. Phương án rút gọn
 *     chế độ tháng thành `Tháng 8/2026` đã bị bác 2026-09-09; test này là thứ sẽ đỏ nếu có người
 *     đề xuất lại nó.
 *  2. **`Kỳ đầu` thay cho một delta bằng 0** ở hàng cũ nhất. "Chưa có gì để so" và "có so, không
 *     đổi" là hai sự thật khác nhau.
 *  3. **`—` cho ô 2 khi kỳ không có tiến độ chung.**
 */
describe('HistoryRow', () => {
  let fixture: ComponentFixture<HistoryRow>;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideTranslateService(),
        { provide: CORE_I18N, useValue: APP_I18N },
      ],
    });
    await useTranslationsInTest();
    fixture = TestBed.createComponent(HistoryRow);
  });

  function render(inputs: Record<string, unknown>): HTMLElement {
    for (const [name, value] of Object.entries(inputs)) {
      fixture.componentRef.setInput(name, value);
    }
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  /** Ô 1 là thẻ `<b>` đầu tiên của hàng. */
  function periodText(host: HTMLElement): string {
    return (host.querySelector('b') as HTMLElement).textContent?.trim() ?? '';
  }

  function normalized(host: HTMLElement, selector: string): string {
    return (host.querySelector(selector)?.textContent ?? '').replace(/\s+/g, ' ').trim();
  }

  // ---------------------------------------------------------------- trạng thái default
  it('bốn ô theo đúng thứ tự: khoảng ngày · tiến độ · biến động · nút Xem', () => {
    const host = render({
      periodStart: WEEK_START,
      periodEnd: WEEK_END,
      overallProgress: 82.1,
      delta: 2.3,
    });

    expect(host.classList).toContain('histrow');
    expect(periodText(host)).toBe('10/08 – 16/08/2026');
    expect(normalized(host, 'span')).toBe('Tiến độ chung 82,1%');
    expect(host.querySelector('app-delta-indicator .delta.up')?.textContent?.trim()).toBe(
      '↑ +2,3 đ.%',
    );
    expect(host.querySelector('button.btn')?.textContent?.trim()).toBe('Xem');
  });

  it('con số tiến độ in đậm — nhãn không đậm', () => {
    const host = render({ periodStart: WEEK_START, periodEnd: WEEK_END, overallProgress: 82.1 });
    const bolds = [...host.querySelectorAll('b')].map((node) => node.textContent?.trim());

    expect(bolds).toContain('82,1%');
  });

  // ---------------------------------------------------------------- ô 1, hai chế độ
  describe('🛑 ô 1 — MỘT khuôn cho cả chế độ Tuần lẫn Tháng', () => {
    it('chế độ tuần', () => {
      expect(periodText(render({ periodStart: WEEK_START, periodEnd: WEEK_END }))).toBe(
        '10/08 – 16/08/2026',
      );
    });

    it('chế độ tháng dùng ĐÚNG khuôn đó — KHÔNG rút gọn thành "Tháng 8/2026"', () => {
      const text = periodText(render({ periodStart: MONTH_START, periodEnd: MONTH_END }));

      expect(text).toBe('01/08 – 31/08/2026');
      expect(text).not.toContain('Tháng');
    });

    it('hai chuỗi dài BẰNG NHAU — tiền đề "tháng dài hơn nên phải rút gọn" là sai', () => {
      const week = periodText(render({ periodStart: WEEK_START, periodEnd: WEEK_END }));
      const month = periodText(render({ periodStart: MONTH_START, periodEnd: MONTH_END }));

      expect(month.length).toBe(week.length);
      expect(week.length).toBe(18);
    });

    it('🛑 hai mốc dùng CÙNG một dấu ngăn ngày/tháng — bẫy ICU đã dính thật 2026-09-09', () => {
      // `Intl` với `{day, month}` (không năm) trả `10-08` ở locale vi, còn `{day, month, year}`
      // trả `16/08/2026`: hai nửa của một khoảng ngày viết bằng hai ký hiệu khác nhau. Phép kiểm
      // này so hai dấu ngăn với nhau thay vì ghim một ký tự cứng, nên nó vẫn đúng ở locale khác.
      const [from, to] = periodText(render({ periodStart: WEEK_START, periodEnd: WEEK_END })).split(
        ' – ',
      );

      expect(from.replace(/\d/g, '')).toBe(to.replace(/\d/g, '').slice(0, from.replace(/\d/g, '').length));
      expect(from).toBe('10/08');
    });

    it('năm chỉ viết MỘT lần, ở mốc cuối', () => {
      const text = periodText(render({ periodStart: WEEK_START, periodEnd: WEEK_END }));

      expect(text.match(/2026/g)?.length).toBe(1);
    });

    it('thiếu mốc ⇒ `—`', () => {
      expect(periodText(render({ periodStart: WEEK_START, periodEnd: null }))).toBe('—');
      expect(periodText(render({ periodStart: null, periodEnd: WEEK_END }))).toBe('—');
    });
  });

  // ---------------------------------------------------------------- ô 2
  describe('ô 2 — tiến độ chung', () => {
    it('có số ⇒ phần trăm 1 chữ số thập phân', () => {
      expect(
        normalized(render({ periodStart: WEEK_START, periodEnd: WEEK_END, overallProgress: 82.1 }), 'span'),
      ).toBe('Tiến độ chung 82,1%');
    });

    it('🛑 kỳ không có tiến độ chung ⇒ `—`, không phải 0,0%', () => {
      expect(
        normalized(
          render({ periodStart: WEEK_START, periodEnd: WEEK_END, overallProgress: null }),
          'span',
        ),
      ).toBe('Tiến độ chung —');
    });
  });

  // ---------------------------------------------------------------- ô 3
  describe('ô 3 — biến động, hoặc `Kỳ đầu`', () => {
    it('hàng cũ nhất in `Kỳ đầu` và KHÔNG dựng DeltaIndicator', () => {
      const host = render({
        periodStart: WEEK_START,
        periodEnd: WEEK_END,
        overallProgress: 68.9,
        isFirstPeriod: true,
      });

      expect(host.querySelector('.muted')?.textContent?.trim()).toBe('Kỳ đầu');
      expect(host.querySelector('app-delta-indicator')).toBeNull();
    });

    it('🛑 `Kỳ đầu` KHÁC một delta bằng 0 — hai câu khác nhau, không thay thế nhau', () => {
      const first = render({ periodStart: WEEK_START, periodEnd: WEEK_END, isFirstPeriod: true });
      expect(first.querySelector('.muted')?.textContent?.trim()).toBe('Kỳ đầu');

      const zero = render({
        periodStart: WEEK_START,
        periodEnd: WEEK_END,
        isFirstPeriod: false,
        delta: 0,
      });
      expect(zero.querySelector('app-delta-indicator .delta.flat')?.textContent?.trim()).toBe(
        '0,0 đ.%',
      );
    });

    it('không phải kỳ đầu mà cũng không có delta ⇒ DeltaIndicator hiện `—`', () => {
      const host = render({
        periodStart: WEEK_START,
        periodEnd: WEEK_END,
        isFirstPeriod: false,
        delta: null,
      });

      expect(host.querySelector('app-delta-indicator .delta')?.textContent?.trim()).toBe('—');
    });
  });

  // ---------------------------------------------------------------- ô 4
  describe('ô 4 — nút Xem', () => {
    it('bấm phát sự kiện view đúng một lần', () => {
      const host = render({ periodStart: WEEK_START, periodEnd: WEEK_END });
      let emitted = 0;
      fixture.componentInstance.view.subscribe(() => emitted++);

      (host.querySelector('button.btn') as HTMLButtonElement).click();

      expect(emitted).toBe(1);
    });

    it('nhãn trợ năng kèm khoảng ngày — sáu nút "Xem" giống nhau thì phải phân biệt được', () => {
      const button = render({ periodStart: WEEK_START, periodEnd: WEEK_END }).querySelector(
        'button.btn',
      ) as HTMLButtonElement;

      expect(button.getAttribute('aria-label')).toBe('Xem kỳ 10/08 – 16/08/2026');
    });

    it('nút mang .no-print — chỉ có nghĩa khi bấm được', () => {
      const button = render({ periodStart: WEEK_START, periodEnd: WEEK_END }).querySelector(
        'button.btn',
      ) as HTMLButtonElement;

      expect(button.classList).toContain('no-print');
    });

    it('hàng KHÔNG bắt click — chỉ nút mới là vùng bấm', () => {
      const host = render({ periodStart: WEEK_START, periodEnd: WEEK_END });
      let emitted = 0;
      fixture.componentInstance.view.subscribe(() => emitted++);

      host.click();

      expect(emitted).toBe(0);
    });
  });

  // ---------------------------------------------------------------- locale
  it('đổi locale đổi cả ngày lẫn số', async () => {
    await useTranslationsInTest('en');
    const host = render({
      periodStart: WEEK_START,
      periodEnd: WEEK_END,
      overallProgress: 82.1,
      localeId: 'en-US',
    });

    expect(periodText(host)).toBe('08/10 – 08/16/2026');
    expect(normalized(host, 'span')).toBe('Overall progress 82.1%');
  });
});

import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideTranslateService } from '@ngx-translate/core';
import { APP_I18N } from '../../../../app.config';
import { CORE_I18N } from '../../../../core/i18n/core-i18n';
import { useTranslationsInTest } from '../../../../core/i18n/i18n.testing';
import { ProgressBar } from './progress-bar';

/**
 * Phép kiểm đắt nhất trong file này là ca **Awaiting data** — và nó đắt vì nó là trạng thái
 * THƯỜNG NGÀY, không phải ca hiếm: quyết định Q24 để `Tiến độ %` trống khi import, nên ngay sau
 * mỗi lần nạp file, cả sáu thanh nhóm đều rỗng.
 *
 * "Rỗng" ở đây có nghĩa đen: **không có phần tử `.fill` nào trong DOM**, và cột số hiện `—`. Một
 * `.fill` rộng 0% trông giống hệt trên màn hình nhưng nói một câu khác — *"đã đo, kết quả bằng
 * không"* — nên phép kiểm phải soi sự TỒN TẠI của `.fill`, không soi bề rộng của nó.
 */
describe('ProgressBar', () => {
  let fixture: ComponentFixture<ProgressBar>;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideTranslateService(),
        { provide: CORE_I18N, useValue: APP_I18N },
      ],
    });
    await useTranslationsInTest();
    fixture = TestBed.createComponent(ProgressBar);
  });

  function render(inputs: Record<string, unknown>): HTMLElement {
    for (const [name, value] of Object.entries(inputs)) {
      fixture.componentRef.setInput(name, value);
    }
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  function fill(host: HTMLElement): HTMLElement | null {
    return host.querySelector('.bar .fill');
  }

  function numText(host: HTMLElement): string {
    return host.querySelector('.num')?.textContent?.trim() ?? '';
  }

  // ---------------------------------------------------------------- trạng thái default
  describe('có dữ liệu', () => {
    it('ba ô: tên nhóm · track chứa fill · phần trăm bằng chữ', () => {
      const host = render({ label: '1. Hạ tầng và Nền tảng số', percent: 74.3 });

      expect(host.querySelector('b')?.textContent?.trim()).toBe('1. Hạ tầng và Nền tảng số');
      expect(host.querySelector('.bar')).not.toBeNull();
      expect(fill(host)).not.toBeNull();
      expect(numText(host)).toBe('74,3%');
    });

    it('bề rộng fill = phần trăm', () => {
      expect((fill(render({ percent: 74.3 })) as HTMLElement).style.width).toBe('74.3%');
    });

    it('luôn 1 chữ số thập phân — 100 hiện 100,0%', () => {
      expect(numText(render({ percent: 100 }))).toBe('100,0%');
    });

    it('🛑 cột số là BẮT BUỘC, không phải trang trí — thanh không bao giờ đứng một mình', () => {
      const host = render({ label: '2. Nhân lực số', percent: 51.8 });

      expect(host.querySelector('.num')).not.toBeNull();
      expect(numText(host)).toBe('51,8%');
    });
  });

  // ---------------------------------------------------------------- kẹp giá trị
  describe('kẹp về [0, 100]', () => {
    it('trên 100 ⇒ thanh đầy, không tràn khỏi track', () => {
      expect((fill(render({ percent: 120 })) as HTMLElement).style.width).toBe('100%');
    });

    it('âm ⇒ thanh rỗng nhưng VẪN có .fill (đã đo, kết quả dưới 0)', () => {
      const host = render({ percent: -5 });

      expect(fill(host)).not.toBeNull();
      expect((fill(host) as HTMLElement).style.width).toBe('0%');
      expect(numText(host)).toBe('0,0%');
    });
  });

  // ---------------------------------------------------------------- Awaiting data
  describe('🛑 biến thể Awaiting data — RỖNG, không phải 0%', () => {
    it('null ⇒ KHÔNG có .fill nào trong DOM', () => {
      const host = render({ label: '1. Hạ tầng và Nền tảng số', percent: null });

      expect(host.querySelector('.bar')).not.toBeNull();
      expect(fill(host)).toBeNull();
    });

    it('null ⇒ cột số hiện `—`', () => {
      expect(numText(render({ percent: null }))).toBe('—');
    });

    it('undefined và NaN cũng là Awaiting data', () => {
      expect(fill(render({ percent: undefined }))).toBeNull();
      expect(fill(render({ percent: Number.NaN }))).toBeNull();
      expect(numText(render({ percent: Number.NaN }))).toBe('—');
    });

    it('🛑 ca đối chứng — 0 THẬT thì có .fill và hiện 0,0%, hai câu khác nhau', () => {
      const measuredZero = render({ percent: 0 });

      expect(fill(measuredZero)).not.toBeNull();
      expect(numText(measuredZero)).toBe('0,0%');
    });
  });

  // ---------------------------------------------------------------- trợ năng
  describe('trợ năng — hai thẻ div trần thì trình đọc màn hình không thấy gì', () => {
    it('track khai role progressbar kèm min/max và tên nhóm', () => {
      const bar = render({ label: '3. An toàn thông tin', percent: 100 }).querySelector(
        '.bar',
      ) as HTMLElement;

      expect(bar.getAttribute('role')).toBe('progressbar');
      expect(bar.getAttribute('aria-label')).toBe('3. An toàn thông tin');
      expect(bar.getAttribute('aria-valuemin')).toBe('0');
      expect(bar.getAttribute('aria-valuemax')).toBe('100');
      expect(bar.getAttribute('aria-valuenow')).toBe('100');
    });

    it('thiếu dữ liệu ⇒ KHÔNG có aria-valuenow, thay bằng aria-valuetext nói thẳng', () => {
      const bar = render({ label: '1. Hạ tầng', percent: null }).querySelector('.bar') as HTMLElement;

      expect(bar.getAttribute('aria-valuenow')).toBeNull();
      expect(bar.getAttribute('aria-valuetext')).toBe('Chưa có dữ liệu');
    });
  });

  // ---------------------------------------------------------------- locale
  it('định dạng theo locale — en-US dùng dấu chấm', () => {
    expect(numText(render({ percent: 74.3, localeId: 'en-US' }))).toBe('74.3%');
  });

  // ---------------------------------------------------------------- bốn trạng thái "không áp dụng"
  it('không tương tác: không nút, không tabindex, hàng không bắt sự kiện', () => {
    const host = render({ label: '1. Hạ tầng', percent: 74.3 });

    expect(host.querySelector('button')).toBeNull();
    expect(host.querySelector('[tabindex]')).toBeNull();
  });
});

import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideTranslateService } from '@ngx-translate/core';
import { APP_I18N } from '../../../../app.config';
import { CORE_I18N } from '../../../../core/i18n/core-i18n';
import { useTranslationsInTest } from '../../../../core/i18n/i18n.testing';
import { KpiTile } from './kpi-tile';

/**
 * Ô KPI gần như không có logic — nên test ở đây chỉ canh đúng những chỗ CÓ NHÁNH, và mỗi nhánh
 * đều là một cách hỏng đã được nêu đích danh trong spec:
 *
 *  1. **Giá trị vắng mặt hiện `—`, không hiện `0`.** Ngay sau import, ô 1 và ô 2 chưa có gì để
 *     tính (`spec/dashboard-dti/business-rules.md` §1.6). Vẽ `0` ở đó là tuyên bố "tiến độ 0%"
 *     ngay sau khi nạp một file có 26 chỉ tiêu đã hoàn thành.
 *  2. **Chỉ CON SỐ mang màu** — nhãn, chú thích và mặt card không đổi màu theo sắc thái.
 *  3. **Không có chú thích thì không render `.sub`**, và ô mất 30px dự trữ.
 */
describe('KpiTile', () => {
  let fixture: ComponentFixture<KpiTile>;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideTranslateService(),
        { provide: CORE_I18N, useValue: APP_I18N },
      ],
    });
    await useTranslationsInTest();
    fixture = TestBed.createComponent(KpiTile);
  });

  function render(inputs: Record<string, unknown>): HTMLElement {
    for (const [name, value] of Object.entries(inputs)) {
      fixture.componentRef.setInput(name, value);
    }
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  function textOf(host: HTMLElement, selector: string): string {
    return host.querySelector(selector)?.textContent?.trim() ?? '';
  }

  // ---------------------------------------------------------------- trạng thái default
  it('ba phần theo đúng thứ tự nhãn → giá trị → chú thích, bên trong một .card.kpi', () => {
    const host = render({
      label: 'Tiến độ chung tuần này',
      value: '82,1%',
      sub: 'Bình quân Tiến độ %, gia quyền theo Điểm tối đa',
    });

    expect(host.querySelector('.card.kpi')).not.toBeNull();
    expect(textOf(host, '.label')).toBe('Tiến độ chung tuần này');
    expect(textOf(host, '.value')).toBe('82,1%');
    expect(textOf(host, '.sub')).toBe('Bình quân Tiến độ %, gia quyền theo Điểm tối đa');
  });

  it('giá trị là chuỗi ĐÃ ĐỊNH DẠNG — component không đụng vào nó', () => {
    expect(textOf(render({ value: '26/62' }), '.value')).toBe('26/62');
    expect(textOf(render({ value: '↑ 2,3 đ.%' }), '.value')).toBe('↑ 2,3 đ.%');
  });

  // ---------------------------------------------------------------- ca vắng mặt
  describe('🛑 giá trị vắng mặt ⇒ `—`, KHÔNG phải `0`', () => {
    it('null hiện dấu gạch', () => {
      expect(textOf(render({ value: null }), '.value')).toBe('—');
    });

    it('chuỗi rỗng hiện dấu gạch', () => {
      expect(textOf(render({ value: '' }), '.value')).toBe('—');
    });

    it('chuỗi toàn khoảng trắng cũng là vắng mặt', () => {
      expect(textOf(render({ value: '   ' }), '.value')).toBe('—');
    });

    it('ca đối chứng — số 0 THẬT vẫn hiện `0`, không bị nuốt thành dấu gạch', () => {
      expect(textOf(render({ value: '0' }), '.value')).toBe('0');
    });
  });

  // ---------------------------------------------------------------- sắc thái
  describe('sắc thái tô ĐÚNG con số', () => {
    it('mặc định: không lớp màu nào', () => {
      const value = render({ value: '82,1%', tone: 'default' }).querySelector('.value') as HTMLElement;

      expect(value.classList.contains('good')).toBeFalse();
      expect(value.classList.contains('warn')).toBeFalse();
      expect(value.classList.contains('bad')).toBeFalse();
    });

    it('good / warn / bad gắn đúng lớp tương ứng', () => {
      for (const tone of ['good', 'warn', 'bad'] as const) {
        const value = render({ value: '18', tone }).querySelector('.value') as HTMLElement;
        expect(value.classList).toContain(tone);
      }
    });

    it('🛑 nhãn và chú thích KHÔNG nhận lớp màu', () => {
      const host = render({ label: 'Chỉ tiêu tăng', value: '18', sub: 'Có tiến bộ', tone: 'good' });

      expect((host.querySelector('.label') as HTMLElement).classList.contains('good')).toBeFalse();
      expect((host.querySelector('.sub') as HTMLElement).classList.contains('good')).toBeFalse();
      expect((host.querySelector('.card') as HTMLElement).classList.contains('good')).toBeFalse();
    });
  });

  // ---------------------------------------------------------------- biến thể không chú thích
  it('không có chú thích ⇒ KHÔNG render .sub', () => {
    expect(render({ value: '18', sub: '' }).querySelector('.sub')).toBeNull();
    expect(render({ value: '18', sub: '   ' }).querySelector('.sub')).toBeNull();
  });

  // ---------------------------------------------------------------- bốn trạng thái "không áp dụng"
  it('ô KPI là chỗ ĐỌC: không nút, không link, không tabindex', () => {
    const host = render({ label: 'Hoàn thành', value: '26/62', sub: 'Số chỉ tiêu' });

    expect(host.querySelector('button')).toBeNull();
    expect(host.querySelector('a')).toBeNull();
    expect(host.querySelector('[tabindex]')).toBeNull();
  });
});

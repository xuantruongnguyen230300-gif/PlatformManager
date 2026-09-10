import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideTranslateService } from '@ngx-translate/core';
import { APP_I18N } from '../../../app.config';
import { CORE_I18N } from '../../../core/i18n/core-i18n';
import { useTranslationsInTest } from '../../../core/i18n/i18n.testing';
import { DELTA_EPSILON, DeltaIndicator, TEXT_KEYS } from './delta-indicator';

/**
 * Ba thứ được canh ở đây, và cả ba đều KHÔNG lộ ra lúc biên dịch:
 *
 *  1. **Luật epsilon.** `> 0` trên số thập phân sinh từ phép trừ sẽ tô xanh một chỉ tiêu KHÔNG
 *     đổi. Ca `2.2` trong dữ liệu thật là `0,38` so với `0,38`, phải đọc là `0,00`.
 *  2. **`—` khác `0,00`.** Hai ca này cùng dùng lớp `.delta.flat`, chỉ khác chữ — nên chỉ có test
 *     đọc CHỮ mới phân biệt được chúng. Đây là ca ngày-đầu-chạy-thật.
 *  3. **Chuỗi của component nằm ở nhóm CoreBase**, xem `describe` cuối file.
 *
 * Bảng dịch nạp từ file THẬT (`useTranslationsInTest`), nên mũi tên và đơn vị `đ.%` được kiểm
 * đúng như trình duyệt nhận: gõ sai khoá thì chuỗi render ra chính là khoá và assert sẽ đỏ.
 * `CORE_I18N` phải có trong TestBed vì helper đọc danh sách nguồn từ đó — nó nạp MỌI nguồn, nên
 * các `it` render bên dưới không phân biệt được khoá đến từ nguồn nào. Phép phân biệt đó là việc
 * của `describe` cuối file, và nó cố ý đọc thẳng một nguồn thay vì đi qua helper.
 */
describe('DeltaIndicator', () => {
  let fixture: ComponentFixture<DeltaIndicator>;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideTranslateService(),
        { provide: CORE_I18N, useValue: APP_I18N },
      ],
    });
    await useTranslationsInTest();
    fixture = TestBed.createComponent(DeltaIndicator);
  });

  /** Đặt input rồi render, trả về chính thẻ `.delta`. */
  function render(inputs: Record<string, unknown>): HTMLElement {
    for (const [name, value] of Object.entries(inputs)) {
      fixture.componentRef.setInput(name, value);
    }
    fixture.detectChanges();
    return (fixture.nativeElement as HTMLElement).querySelector('.delta') as HTMLElement;
  }

  function textOf(inputs: Record<string, unknown>): string {
    return render(inputs).textContent?.trim() ?? '';
  }

  // ---------------------------------------------------------------- trạng thái default
  describe('kiểu score — cột Chênh lệch, 2 chữ số thập phân, không đơn vị', () => {
    it('dương ⇒ lớp .up, dấu + do signDisplay sinh ra', () => {
      const delta = render({ value: 2.96, format: 'score' });

      expect(delta.classList).toContain('up');
      expect(delta.textContent?.trim()).toBe('+2,96');
    });

    it('âm ⇒ lớp .down', () => {
      const delta = render({ value: -5, format: 'score' });

      expect(delta.classList).toContain('down');
      expect(delta.textContent?.trim()).toBe('-5,00');
    });

    it('bằng 0 ⇒ lớp .flat và chữ 0,00 — KHÔNG có dấu', () => {
      const delta = render({ value: 0, format: 'score' });

      expect(delta.classList).toContain('flat');
      expect(delta.textContent?.trim()).toBe('0,00');
    });
  });

  // ---------------------------------------------------------------- luật epsilon
  describe(`epsilon ${DELTA_EPSILON} — nhiễu làm tròn KHÔNG được tính là biến động`, () => {
    it('sai số nhỏ hơn epsilon vẫn là flat, không phải up', () => {
      const delta = render({ value: 0.0004, format: 'score' });

      expect(delta.classList).toContain('flat');
      expect(delta.classList).not.toContain('up');
    });

    it('sai số âm nhỏ hơn epsilon vẫn là flat, không phải down', () => {
      expect(render({ value: -0.0004, format: 'score' }).classList).toContain('flat');
    });

    it('vượt epsilon thì mới đổi hướng', () => {
      expect(render({ value: 0.002, format: 'score' }).classList).toContain('up');
      expect(render({ value: -0.002, format: 'score' }).classList).toContain('down');
    });

    it('ca đối chứng — phép so `=== 0` sẽ ĐỎ ở đây, epsilon thì không', () => {
      // 0.38 - 0.38 ra đúng 0, nhưng 0.1 + 0.2 - 0.3 thì không: đây là hình dạng thật của nhiễu.
      const noise = 0.1 + 0.2 - 0.3;

      expect(noise === 0).toBeFalse();
      expect(render({ value: noise, format: 'score' }).classList).toContain('flat');
    });
  });

  // ---------------------------------------------------------------- ca "không có gì để so"
  describe('không có giá trị — `—`, KHÔNG phải 0', () => {
    it('null ⇒ dấu gạch, lớp .flat', () => {
      const delta = render({ value: null, format: 'score' });

      expect(delta.classList).toContain('flat');
      expect(delta.textContent?.trim()).toBe('—');
    });

    it('undefined ⇒ dấu gạch', () => {
      expect(textOf({ value: undefined, format: 'score' })).toBe('—');
    });

    it('NaN ⇒ dấu gạch, không phải chuỗi "NaN"', () => {
      expect(textOf({ value: Number.NaN, format: 'score' })).toBe('—');
    });

    it('🛑 `—` và `0,00` là HAI chuỗi khác nhau dù CÙNG lớp .flat', () => {
      const absent = textOf({ value: null, format: 'score' });
      const measuredZero = textOf({ value: 0, format: 'score' });

      expect(absent).not.toBe(measuredZero);
    });
  });

  // ---------------------------------------------------------------- kiểu percentagePoint
  describe('kiểu percentagePoint — mũi tên + 1 chữ số + đơn vị đ.%', () => {
    it('tăng ⇒ mũi tên lên, dấu +, đơn vị đ.%', () => {
      const delta = render({ value: 2.3, format: 'percentagePoint' });

      expect(delta.classList).toContain('up');
      expect(delta.textContent?.trim()).toBe('↑ +2,3 đ.%');
    });

    it('giảm ⇒ mũi tên xuống', () => {
      const delta = render({ value: -5, format: 'percentagePoint' });

      expect(delta.classList).toContain('down');
      expect(delta.textContent?.trim()).toBe('↓ -5,0 đ.%');
    });

    it('không đổi ⇒ KHÔNG mũi tên, vẫn có đơn vị', () => {
      expect(textOf({ value: 0, format: 'percentagePoint' })).toBe('0,0 đ.%');
    });

    it('🛑 cùng một con số, hai kiểu ⇒ hai chuỗi khác nhau', () => {
      const asScore = textOf({ value: 2.3, format: 'score' });
      const asPoints = textOf({ value: 2.3, format: 'percentagePoint' });

      expect(asScore).toBe('+2,30');
      expect(asPoints).toBe('↑ +2,3 đ.%');
    });
  });

  // ---------------------------------------------------------------- locale
  describe('định dạng theo locale', () => {
    it('en-US dùng dấu chấm thập phân và đơn vị tiếng Anh', async () => {
      await useTranslationsInTest('en');
      const delta = render({ value: 2.3, format: 'percentagePoint', localeId: 'en-US' });

      expect(delta.textContent?.trim()).toBe('↑ +2.3 pp');
    });

    it('🛑 dấu là một phần của SỐ, không ghép tay — số âm không ra `+-`', async () => {
      await useTranslationsInTest('en');

      expect(textOf({ value: -5, format: 'score', localeId: 'en-US' })).toBe('-5.00');
    });
  });

  // ---------------------------------------------------------------- bốn trạng thái "không áp dụng"
  it('không tương tác được: không tabindex, không phải nút — đúng 4 hàng "Not applicable"', () => {
    const delta = render({ value: 1, format: 'score' });

    expect(delta.getAttribute('tabindex')).toBeNull();
    expect(delta.tagName).toBe('SPAN');
    expect((fixture.nativeElement as HTMLElement).querySelector('button')).toBeNull();
  });

  // ------------------------------------------------- chuỗi phải đi cùng nền tảng khi tách CoreBase
  /**
   * Component này ở `shared/` ⇒ nó đi theo khi tách CoreBase. Nếu chuỗi của nó ở lại nhóm dự án
   * (`public/i18n-app/`, thư mục KHÔNG đi theo) thì sản phẩm thứ hai render ra chính chuỗi khoá —
   * `shared.deltaIndicator.pointUp` giữa một ô bảng — và **không gì báo**: ngx-translate coi
   * tra trượt là chuyện thường, nó trả về tên khoá thay vì ném lỗi. Build xanh, lint xanh, mọi
   * test render ở trên cũng xanh, vì chúng nạp CẢ HAI nguồn.
   *
   * Vì vậy phép kiểm này đọc RIÊNG nguồn CoreBase, không đi qua `useTranslationsInTest`.
   */
  describe('🛑 chuỗi của component nằm trong nhóm CoreBase, không nằm ở nhóm dự án', () => {
    /**
     * Tiền tố của nhóm CoreBase — thư mục được bê nguyên sang sản phẩm thứ hai
     * (doc/huong_dan/wiki-core/fe/08-i18n.md §Khuôn CoreBase). Viết thẳng ở đây là CÓ CHỦ ĐÍCH:
     * lấy nó từ `APP_I18N.resources[0]` sẽ biến phép kiểm thành "khoá nằm ở nguồn đầu tiên, bất kể
     * nguồn đó là gì", tức nó tự xanh lại khi có người đảo thứ tự hai nguồn.
     */
    const CORE_BUNDLE_PREFIX = '/i18n/';

    /** Đi theo đường `a.b.c` trong một bảng dịch; trả `undefined` khi tra trượt ở bất kỳ cấp nào. */
    function lookup(bundle: unknown, key: string): unknown {
      return key
        .split('.')
        .reduce<unknown>(
          (node, part) =>
            node !== null && typeof node === 'object'
              ? (node as Record<string, unknown>)[part]
              : undefined,
          bundle,
        );
    }

    async function loadCoreBundle(code: string): Promise<unknown> {
      const url = `${CORE_BUNDLE_PREFIX}${code}.json`;
      const response = await fetch(url);
      expect(response.ok).withContext(`không đọc được ${url}`).toBeTrue();
      return await response.json();
    }

    it('mọi khoá của component đọc được từ RIÊNG nhóm CoreBase, mọi ngôn ngữ', async () => {
      for (const language of APP_I18N.languages) {
        const bundle = await loadCoreBundle(language.code);

        for (const key of Object.values(TEXT_KEYS)) {
          expect(lookup(bundle, key))
            .withContext(
              `${CORE_BUNDLE_PREFIX}${language.code}.json thiếu "${key}" — component ở shared/ đi ` +
                `theo CoreBase thì chuỗi của nó phải đi cùng, không được ở lại public/i18n-app/`,
            )
            .toEqual(jasmine.any(String));
        }
      }
    });

    it('ca đối chứng — phép kiểm trên ĐỎ được: khoá của nhóm dự án KHÔNG có trong nguồn Core', async () => {
      // Không có `it` này thì `lookup` trả về một giá trị nào đó cho mọi khoá cũng vẫn xanh, và
      // phép kiểm trên không phân biệt được "đã kiểm" với "không kiểm gì cả".
      const bundle = await loadCoreBundle(APP_I18N.languages[0].code);

      expect(lookup(bundle, 'dashboard.grid.groupAwaitingData')).toBeUndefined();
      expect(lookup(bundle, 'dashboard.grid.trendPeriodHeader')).toBeUndefined();
    });
  });
});

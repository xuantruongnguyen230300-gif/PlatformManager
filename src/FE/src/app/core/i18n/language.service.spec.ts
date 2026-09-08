import { formatNumber } from '@angular/common';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideTranslateService } from '@ngx-translate/core';
import { PrimeNG } from 'primeng/config';
import { Translation } from 'primeng/api';
import localeVi from '@angular/common/locales/vi';
import { CORE_I18N, ICoreI18n } from './core-i18n';
import { LANGUAGE_STORAGE_KEY, LanguageService } from './language.service';

/**
 * Ba việc mà chốt i18n runtime tự nhận về tay mình (doc/huong_dan/wiki-core/fe/08-i18n.md §Cạm bẫy
 * 1/2/5) đều **không lộ ra lúc biên dịch** và **không làm hỏng thứ gì nhìn thấy được** khi quên —
 * `<html lang>` sai thì màn hình vẫn đẹp, chỉ trình đọc màn hình đọc sai giọng. Vì vậy chúng phải
 * có test, không có cách nào khác để biết chúng còn chạy.
 */

const PRIME_VI = { accept: 'Đồng ý' } as Translation;
const PRIME_EN = { accept: 'Yes' } as Translation;

const CONFIG: ICoreI18n = {
  languages: [
    { code: 'vi', label: 'Tiếng Việt', localeId: 'vi', localeData: localeVi, primeTranslation: PRIME_VI },
    { code: 'en', label: 'English', localeId: 'en-US', primeTranslation: PRIME_EN },
  ],
  defaultCode: 'vi',
  resources: ['/i18n/'],
};

function createService(config: ICoreI18n = CONFIG): LanguageService {
  TestBed.resetTestingModule();
  TestBed.configureTestingModule({
    providers: [
      // App chạy zoneless (`app.config.ts`), nên `TestBed` cũng phải zoneless — thiếu dòng này
      // Angular ném `NG0908 In this configuration Angular requires Zone.js`.
      provideZonelessChangeDetection(),
      { provide: CORE_I18N, useValue: config },
      // `TranslateNoOpLoader` mặc định: `use()` emit ngay một bảng rỗng. Test này kiểm cơ chế đổi
      // ngôn ngữ, không kiểm nội dung bảng dịch — nội dung có test riêng ở `app-i18n.spec.ts`.
      provideTranslateService(),
      PrimeNG,
    ],
  });
  return TestBed.inject(LanguageService);
}

describe('LanguageService', () => {
  /** Karma chạy test TRONG chính trang `<html lang="vi">` của app, nên mọi `it` đổi thẻ này phải
   * trả nó về — bỏ qua thì một test đổi sang `en` sẽ làm test sau đó đọc nhầm giá trị. */
  let originalLang: string;

  beforeEach(() => {
    originalLang = document.documentElement.lang;
    localStorage.removeItem(LANGUAGE_STORAGE_KEY);
  });

  afterEach(() => {
    document.documentElement.lang = originalLang;
    localStorage.removeItem(LANGUAGE_STORAGE_KEY);
  });

  it('🛑 init() đăng ký locale data — không có nó, DatePipe/DecimalPipe ném NG0701 lúc chạy', async () => {
    await createService().init();

    // `formatNumber` là chính hàm `DecimalPipe` gọi bên trong. Thiếu `registerLocaleData(vi)` thì
    // nó ném `NG0701 MISSING_LOCALE_DATA` — LÚC CHẠY, không phải lúc biên dịch, và chỉ ở nhánh
    // tiếng Việt (nhánh `en-US` gói sẵn trong Angular nên chạy hoàn hảo và che mất lỗi).
    expect(() => formatNumber(1234.5, 'vi')).not.toThrow();
    // Quy ước số của tiếng Việt: dấu chấm ngăn nhóm nghìn, dấu phẩy ngăn phần thập phân — ngược
    // hẳn `en-US`. Đây là bằng chứng dữ liệu locale ĐÚNG đã vào, không chỉ là "không ném".
    expect(formatNumber(1234.5, 'vi', '1.1-1')).toBe('1.234,5');
  });

  it('🛑 use() đặt document.documentElement.lang theo ngôn ngữ đang chọn (WCAG 3.1.1, mức A)', async () => {
    const service = createService();
    await service.init();
    expect(document.documentElement.lang).toBe('vi');

    await service.use('en');
    expect(document.documentElement.lang)
      .withContext(
        'quên bước này KHÔNG làm hỏng gì nhìn thấy được: giao diện vẫn đúng tiếng Anh, chỉ có ' +
          'trình đọc màn hình đọc bằng giọng tiếng Việt',
      )
      .toBe('en');

    await service.use('vi');
    expect(document.documentElement.lang).toBe('vi');
  });

  it('🛑 lựa chọn được NHỚ qua lần tải trang sau', async () => {
    const first = createService();
    await first.init();
    await first.use('en');
    expect(localStorage.getItem(LANGUAGE_STORAGE_KEY)).toBe('en');

    // Thể hiện mới = mô phỏng F5. Nghiệm thu số 3 của doc/huong_dan/wiki-core/fe/
    // 17-phuc-vu-va-trien-khai.md §3.1: "F5 sau khi đã chọn tiếng Anh → vẫn `en`".
    const afterReload = createService();
    await afterReload.init();
    expect(afterReload.current()).toBe('en');
    expect(document.documentElement.lang).toBe('en');
  });

  it('use() nối nhãn PrimeNG của đúng ngôn ngữ', async () => {
    const service = createService();
    await service.init();
    const primeng = TestBed.inject(PrimeNG);
    expect(primeng.translation.accept).toBe('Đồng ý');

    await service.use('en');
    expect(primeng.translation.accept).toBe('Yes');
  });

  it('localeId() theo ngôn ngữ đang chọn — đây là giá trị truyền làm THAM SỐ CUỐI của pipe', async () => {
    const service = createService();
    await service.init();
    expect(service.localeId()).toBe('vi');

    await service.use('en');
    // `en-US` chứ không `en`: mã locale của Angular khác mã ngôn ngữ, và pipe cần mã locale.
    expect(service.localeId()).toBe('en-US');
  });

  it('mã lạ còn sót trong localStorage lùi về ngôn ngữ mặc định thay vì làm app chết', async () => {
    // Xảy ra thật khi dự án BỎ BỚT một ngôn ngữ: trình duyệt của người dùng cũ vẫn giữ mã cũ.
    localStorage.setItem(LANGUAGE_STORAGE_KEY, 'ja');
    const service = createService();
    await service.init();

    expect(service.current()).toBe('vi');
    expect(document.documentElement.lang).toBe('vi');
  });

  it('🛑 defaultCode không khớp ngôn ngữ nào thì NÉM ngay — lỗi khai báo, không có đường lùi', async () => {
    const broken: ICoreI18n = { ...CONFIG, defaultCode: 'de' };
    const service = createService(broken);

    // Im lặng chọn đại phần tử đầu tiên sẽ cho một app chạy được ở ngôn ngữ không ai định chọn —
    // đúng dạng sai mà seam này (token không có giá trị mặc định) sinh ra để tránh.
    await expectAsync(service.init()).toBeRejectedWithError(/CORE_I18N\.defaultCode/);
  });
});

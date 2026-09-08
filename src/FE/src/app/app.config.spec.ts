import { EnvironmentProviders, Provider, ɵisEnvironmentProviders } from '@angular/core';
import { APP_BRANDING, APP_I18N, appConfig } from './app.config';
import { CORE_BRANDING } from './core/config/core-branding';
import { CORE_I18N } from './core/i18n/core-i18n';

/**
 * URL mà karma phục vụ `src/index.html` trong lúc chạy test.
 *
 * Nó KHÔNG tự có: `angular.json` → `projects.platform-manager.architect.test.options.assets`
 * khai thêm một mục `{ glob: "index.html", input: "src", output: "self-check" }`. Ghi lý do ở
 * đây vì `angular.json` là JSON — không đặt được chú thích vào chính chỗ khai, nên chỗ khai đó
 * trông như thừa và rất dễ bị dọn đi. Xoá nó thì `it` cuối file này ĐỎ kèm thông điệp chỉ thẳng
 * về đây, không phải đỏ một cách bí ẩn.
 *
 * Mục assets này chỉ nằm ở target `test`, không nằm ở `build` — bản build production KHÔNG kèm
 * thêm bản sao `index.html` nào.
 */
const INDEX_HTML_URL = '/self-check/index.html';

/**
 * Trải phẳng `appConfig.providers` — `provideCoreBranding()` trả về `EnvironmentProviders`, một
 * hộp kín chỉ mở được qua `ɵproviders` bên trong. Dùng `ɵisEnvironmentProviders` do chính
 * `@angular/core` export thay vì tự ép kiểu: khi Angular đổi hình dạng nội bộ, cái sai lộ ra ở
 * lỗi biên dịch của file này chứ không phải ở một phép kiểm âm thầm ngừng kiểm.
 */
function flattenProviders(providers: readonly (Provider | EnvironmentProviders)[]): unknown[] {
  const out: unknown[] = [];
  for (const entry of providers) {
    if (ɵisEnvironmentProviders(entry)) {
      out.push(...flattenProviders(entry.ɵproviders));
    } else if (Array.isArray(entry)) {
      out.push(...flattenProviders(entry as Provider[]));
    } else {
      out.push(entry);
    }
  }
  return out;
}

function providerFor(token: unknown): Record<string, unknown> | undefined {
  const found = flattenProviders(appConfig.providers).find(
    (provider) =>
      typeof provider === 'object' && provider !== null && (provider as { provide?: unknown }).provide === token,
  );
  return found as Record<string, unknown> | undefined;
}

/**
 * `CORE_BRANDING` là seam anh em của `CORE_ROUTES`: `core/` và `shared/` giữ CƠ CHẾ, app cấp
 * CHUỖI (doc/kien-truc-core-module.md §"Core giữ CƠ CHẾ, dự án cung cấp DỮ LIỆU"). Nhưng chỉ
 * `CORE_ROUTES` có chốt chặn bằng máy ở `app.routes.spec.ts`; phía branding thì không có gì —
 * tình trạng tới 2026-09-03.
 *
 * Vì sao khoảng trống đó đáng một file test: mọi test khác dựng `TestBed` **của riêng nó** và tự
 * khai `provideCoreBranding(...)`, nên chúng xanh kể cả khi `app.config.ts` quên đăng ký. Lỗi
 * thật khi đó chỉ lộ ra lúc chạy app thật, dưới dạng `NG0201` ở màn hình đầu tiên.
 *
 * Cách kiểm là ĐỌC HÌNH DẠNG provider chứ không dựng `appConfig.providers` trong `TestBed` — cùng
 * lý do đã ghi ở `core/title/page-title.strategy.spec.ts`: bộ providers thật kéo theo
 * `provideAuthInit()`/`provideCsrfInit()`, tức HTTP lúc khởi tạo, biến một phép kiểm tất định
 * thành phép kiểm phụ thuộc mạng.
 */
describe('app.config.ts — seam tên sản phẩm phải được đăng ký và khớp index.html', () => {
  it('🛑 appConfig có đăng ký CORE_BRANDING — thiếu là NG0201 ở ngay màn hình đầu tiên', () => {
    expect(providerFor(CORE_BRANDING))
      .withContext(
        'gỡ provideCoreBranding(APP_BRANDING) khỏi app.config.ts = PageTitleStrategy/Sidebar/AuthCard ' +
          'ném NG0201 lúc dựng; không test nào khác bắt được vì mỗi test tự cấp token cho TestBed của nó',
      )
      .toBeDefined();
  });

  it('giá trị đăng ký chính là APP_BRANDING, không phải một object khác dựng tại chỗ', () => {
    // Đăng ký một object khác sẽ làm `APP_BRANDING` thành hằng số chết: sửa nó không đổi gì trên
    // màn hình, mà mọi test đọc `APP_BRANDING` (kể cả `it` bên dưới) vẫn xanh.
    expect(providerFor(CORE_BRANDING)?.['useValue']).toBe(APP_BRANDING);
  });

  it('🛑 APP_BRANDING.name khớp <title> tĩnh trong src/index.html', async () => {
    // `<title>` của index.html là thứ hiện ra trong lúc app CHƯA bootstrap xong; `PageTitleStrategy`
    // ghi đè sau đó bằng hậu tố dựng từ `APP_BRANDING.name`. Lệch nhau thì tên tab đổi ngay trước
    // mắt người dùng lúc trang tải xong — không lỗi, không cảnh báo, chỉ trông như app hỏng.
    const response = await fetch(INDEX_HTML_URL);
    expect(response.ok)
      .withContext(`không đọc được ${INDEX_HTML_URL} — nhiều khả năng mục assets "self-check" đã bị gỡ khỏi angular.json`)
      .toBeTrue();

    const html = await response.text();
    const title = /<title>([\s\S]*?)<\/title>/.exec(html)?.[1]?.trim();

    expect(title).withContext('src/index.html phải có <title>').toBeDefined();
    expect(title)
      .withContext('sửa tên sản phẩm ở app.config.ts thì phải sửa cả <title> trong src/index.html')
      .toBe(APP_BRANDING.name);
  });
});

/**
 * `CORE_I18N` là seam thứ ba cùng khuôn với `CORE_ROUTES` / `CORE_BRANDING`, và nó có cùng khoảng
 * trống: mọi test khác dựng `TestBed` CỦA RIÊNG NÓ và tự cấp token, nên chúng xanh kể cả khi
 * `app.config.ts` quên đăng ký. Lỗi thật khi đó là `NG0201` ở màn hình đầu tiên của app thật.
 */
describe('app.config.ts — seam i18n phải được đăng ký', () => {
  it('🛑 appConfig có đăng ký CORE_I18N', () => {
    expect(providerFor(CORE_I18N))
      .withContext('gỡ provideCoreI18n(APP_I18N) khỏi app.config.ts = LanguageService ném NG0201 lúc khởi động')
      .toBeDefined();
  });

  it('giá trị đăng ký chính là APP_I18N, không phải một object khác dựng tại chỗ', () => {
    expect(providerFor(CORE_I18N)?.['useValue']).toBe(APP_I18N);
  });

  it('🛑 defaultCode nằm trong languages', () => {
    // Cùng ràng buộc mà `LanguageService.resolve` cưỡng chế lúc chạy — kiểm ở đây để nó đỏ trong
    // `ng test` thay vì đỏ ở lần khởi động app đầu tiên sau khi ai đó sửa danh sách ngôn ngữ.
    expect(APP_I18N.languages.map((language) => language.code)).toContain(APP_I18N.defaultCode);
  });

  /**
   * Angular chỉ biên dịch sẵn DUY NHẤT `en-US` vào core. Mọi `localeId` khác phải được nạp bằng
   * `registerLocaleData(...)`, và dữ liệu để nạp lấy từ `ICoreLanguage.localeData`. Thiếu nó thì
   * `DatePipe`/`DecimalPipe`/`CurrencyPipe` ném **NG0701** — lúc CHẠY, ở đúng màn hình có định
   * dạng ngày/số, không phải lúc build.
   *
   * <b>Vì sao kiểm HÌNH DẠNG chứ không kiểm HÀNH VI</b> (thêm 2026-09-05 sau một lượt kiểm độc
   * lập): phép thử "gọi `formatNumber` xem có ném không" nghe chắc hơn nhưng ở đây nó là phép thử
   * MÙ. `registerLocaleData` ghi vào một registry **toàn cục của tiến trình**, mà Karma chạy mọi
   * spec trong CÙNG một context trình duyệt — `language.service.spec.ts` đăng ký `vi` bằng dữ
   * liệu TestBed của riêng nó, nên đến lượt spec này chạy thì `vi` đã có sẵn trong registry và
   * `formatNumber` không ném, kể cả khi `APP_I18N` đã bị gỡ sạch `localeData`.
   *
   * Đó đúng là lỗ hổng đã đo được: gỡ `localeData: localeVi` khỏi `APP_I18N` → toàn bộ test vẫn
   * XANH. Bất biến hình dạng thì không có trạng thái toàn cục nào làm nhiễu được.
   */
  it('🛑 mọi ngôn ngữ có localeId khác en-US phải khai localeData — thiếu là NG0701 lúc chạy', () => {
    const thieu = APP_I18N.languages
      .filter((language) => language.localeId !== 'en-US' && language.localeData === undefined)
      .map((language) => `${language.code} (localeId '${language.localeId}')`);

    expect(thieu)
      .withContext(
        'Angular chỉ gói sẵn en-US. Ngôn ngữ trên thiếu `localeData` nên DatePipe/DecimalPipe sẽ ' +
          'ném NG0701 lúc chạy. Sửa: import locale ở app.config.ts rồi khai `localeData: <locale>`.',
      )
      .toEqual([]);
  });
});

import { ApplicationConfig, ErrorHandler, provideZonelessChangeDetection } from '@angular/core';
import { TitleStrategy, provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors, withXsrfConfiguration } from '@angular/common/http';
import { providePrimeNG } from 'primeng/config';
import localeVi from '@angular/common/locales/vi';
import { vi as primeVi } from 'primelocale/js/vi.js';
import { en as primeEn } from 'primelocale/js/en.js';
import { routes } from './app.routes';
import { ICoreRoutes, provideCoreRoutes } from './core/config/core-routes';
import { ICoreBranding, provideCoreBranding } from './core/config/core-branding';
import { ICoreI18n, provideCoreI18n } from './core/i18n/core-i18n';
import { httpErrorInterceptor } from './core/interceptors/http-error.interceptor';
import { apiBaseUrlInterceptor } from './core/interceptors/api-base-url.interceptor';
import { withCredentialsInterceptor } from './core/interceptors/credentials.interceptor';
import { provideAuthInit } from './core/auth/auth-init.provider';
import { provideCsrfInit } from './core/http/csrf-init.provider';
import { ICorePalette, createCorePreset } from './core/theme/core-preset';
import { PageTitleStrategy } from './core/title/page-title.strategy';
import { GlobalErrorHandler } from './core/errors/global-error.handler';

/**
 * Ba đường dẫn riêng của DỰ ÁN NÀY mà tầng `core/` cần để chuyển hướng. `core/` chỉ biết ngữ
 * nghĩa (`signIn`/`changePassword`/`home`) — chuỗi cụ thể nằm ở đây, vì đây là app, không phải
 * CoreBase. Sản phẩm thứ hai dựng trên nền tảng này chỉ cần đổi ĐÚNG ba dòng dưới, không phải mở
 * `core/auth/*.guard.ts` ra sửa. Xem core/config/core-routes.ts.
 *
 * 🛑 Ba giá trị này phải khớp `app.routes.ts` — sai một chữ thì guard chuyển hướng tới route không
 * tồn tại, rơi vào `**` và im lặng quay về trang chủ. `app.routes.spec.ts` khoá lại bằng máy, nên
 * đổi đường dẫn ở `app.routes.ts` mà quên đổi ở đây sẽ ĐỎ.
 *
 * Export ra ngoài chính là để test đó đọc được — không dùng cho mục đích nào khác.
 */
export const APP_CORE_ROUTES: ICoreRoutes = {
  signIn: '/dang-nhap',
  changePassword: '/doi-mat-khau',
  home: '/trang-chu',
};

/**
 * Tên sản phẩm — dữ liệu của DỰ ÁN NÀY. `core/title/page-title.strategy.ts` (hậu tố `<title>`)
 * và `shared/components/sidebar/sidebar.html` (ô thương hiệu) chỉ biết ngữ nghĩa
 * `name`/`shortName`; chuỗi nằm ở đây vì đây là app, không phải CoreBase. Xem
 * core/config/core-branding.ts.
 *
 * 🛑 `name` phải khớp `<title>` tĩnh trong `src/index.html` — chuỗi đó hiện ra trước khi app
 * bootstrap xong, lệch nhau thì tên tab đổi ngay trước mắt người dùng lúc trang tải xong.
 */
export const APP_BRANDING: ICoreBranding = {
  name: 'PlatformManager',
  shortName: 'PM',
};

/**
 * Bảng màu thương hiệu — cũng là dữ liệu của dự án này, nhưng đi đường KHÁC hai seam trên:
 * truyền thẳng làm tham số cho `createCorePreset()` chứ không qua `InjectionToken`. Lý do là
 * ràng buộc kỹ thuật, không phải sở thích: `providePrimeNG()` ngay bên dưới dựng preset lúc
 * **tạo object cấu hình này**, trước khi injector tồn tại — `inject()` ở đó ném `NG0203`.
 *
 * 🛑 Mọi giá trị PHẢI khớp `:root` trong `src/styles.scss`. CSS thuần đọc biến `--*`, component
 * PrimeNG đọc ramp dựng từ đây; lệch nhau là hai màu khác nhau trên cùng một màn hình mà không
 * gì đỏ. Chiều cập nhật: `doc/Design/.../Tokens/*` → `styles.scss` **và** hằng số này (xem
 * doc/huong_dan/wiki-core/fe/04-design-token-system.md §Chiều).
 */
export const APP_PALETTE: ICorePalette = {
  brand: '#0f5bd7',
  good: '#0e7050',
  warn: '#965e08',
  bad: '#a02b2b',
  bg: '#cfdaea',
  card: '#ffffff',
  text: '#152033',
  muted: '#4c576b',
  line: '#7a97bd',
  borderStrong: '#6077a2',
  // `--on-primary: #fff` ở styles.scss:80 — cùng một màu, viết đủ 6 chữ số theo luật của
  // `ICorePalette`. Về đây 2026-09-03: trước đó nó là hằng số `contrastColor` nằm trong
  // `core/theme/core-preset.ts`, nên sản phẩm dùng brand màu SÁNG (cần mực tối) phải mở
  // nền tảng ra sửa. Giá trị KHÔNG đổi một chút nào, chỉ đổi chỗ khai.
  onPrimary: '#ffffff',
};

/**
 * Danh sách ngôn ngữ của DỰ ÁN NÀY. `core/` chỉ biết CƠ CHẾ (nạp bảng dịch, đổi tại chỗ, đặt
 * `<html lang>`, nối nhãn PrimeNG); "sản phẩm này chạy vi + en" là DỮ LIỆU, nên nó nằm ở đây —
 * cùng khuôn với `APP_CORE_ROUTES` / `APP_BRANDING` phía trên. Xem core/i18n/core-i18n.ts.
 *
 * 🛑 `code` của mỗi ngôn ngữ PHẢI có file `src/FE/public/i18n/<code>.json` tương ứng. Sai tên là
 * lỗi LÚC CHẠY, không phải lúc biên dịch — `failOnError: true` ở `provideCoreI18n` biến nó thành
 * một lần khởi động hỏng ồn ào thay vì một giao diện hiện toàn khoá.
 *
 * `localeData` chỉ khai cho `vi`: `en-US` là locale Angular gói sẵn, đăng ký lại là thừa. Thiếu
 * dòng `localeVi` thì `DatePipe`/`DecimalPipe` ném `NG0701` lúc chạy, và chỉ ném ở nhánh tiếng
 * Việt — nhánh tiếng Anh chạy hoàn hảo sẽ che mất nó.
 */
export const APP_I18N: ICoreI18n = {
  languages: [
    {
      code: 'vi',
      label: 'Tiếng Việt',
      localeId: 'vi',
      localeData: localeVi,
      primeTranslation: primeVi,
    },
    // `localeId: 'en-US'` chứ không `'en'`: `en-US` là locale Angular biên dịch sẵn vào core, còn
    // `'en'` phải `registerLocaleData` như `vi`. Dùng bản gói sẵn thì bớt được một chỗ quên.
    { code: 'en', label: 'English', localeId: 'en-US', primeTranslation: primeEn },
  ],
  // Tiếng Việt là ngôn ngữ NGUỒN (chuỗi gốc do người viết code nghĩ ra là tiếng Việt), nên nó vừa
  // là mặc định vừa là ngôn ngữ dự phòng khi một khoá thiếu bản dịch `en`.
  defaultCode: 'vi',
  // Đúng MỘT nguồn hôm nay: khoá Core. Nhóm khoá dự án còn rỗng vì `platform/` chưa có màn nghiệp
  // vụ nào (doc/huong_dan/wiki-core/fe/08-i18n.md §Khuôn khoá dịch §5). Thêm nhóm đó về sau là
  // thêm một tiền tố vào mảng này, KHÔNG phải sửa file dịch của nền tảng.
  resources: ['/i18n/'],
};

export const appConfig: ApplicationConfig = {
  providers: [
    // Zoneless ngay từ đầu (đã CHỐT) — KHÔNG dùng provideZoneChangeDetection/zone.js, xem
    // doc/huong_dan/wiki-core/fe/13-performance.md §1.
    provideZonelessChangeDetection(),
    provideRouter(routes),
    // Bơm đường dẫn cho `core/` — PHẢI có, token `CORE_ROUTES` cố ý không có giá trị mặc định
    // (thiếu provider = `NullInjectorError` ngay lần điều hướng đầu, thay vì im lặng đi sai chỗ).
    provideCoreRoutes(APP_CORE_ROUTES),
    // Bơm tên sản phẩm cho `core/` và `shared/` — cùng luật với `CORE_ROUTES`: token cố ý không
    // có giá trị mặc định, quên khai là `NG0201` ngay lần dựng đầu chứ không phải một giao diện
    // mang tên sản phẩm khác. Xem core/config/core-branding.ts.
    provideCoreBranding(APP_BRANDING),
    // Tiêu đề trang: `title` khai ở CẤP `Route` trong từng `<feature>.routes.ts`, `PageTitleStrategy`
    // biến nó thành `<title>` của tab (WCAG 2.4.2 Page Titled, mức A — bắt buộc theo
    // doc/huong_dan/wiki-core/fe/15-accessibility.md §1) VÀ thành tiêu đề topbar cho `App`.
    // `useExisting` chứ không `useClass`: router và `App` phải dùng chung một thể hiện, nếu không
    // topbar sẽ bám vào thể hiện không bao giờ được router gọi. Lý do đầy đủ ở chính file strategy.
    { provide: TitleStrategy, useExisting: PageTitleStrategy },
    // `ErrorHandler` toàn cục — bắt ca "file chunk cũ đã biến mất sau deploy" và mời tải lại thay
    // vì để lại màn hình trắng không một chữ giải thích (doc/huong_dan/wiki-core/fe/
    // 17-phuc-vu-va-trien-khai.md §4). `useExisting` chứ không `useClass`, cùng lý do như
    // `PageTitleStrategy` ở trên: `App` inject THẲNG `GlobalErrorHandler` để đọc signal
    // `newVersionAvailable()`, nên Angular và `App` phải nhìn vào CÙNG MỘT thể hiện — `useClass`
    // sẽ tạo thể hiện thứ hai và dải thông báo không bao giờ hiện ra.
    { provide: ErrorHandler, useExisting: GlobalErrorHandler },
    // Thứ tự: apiBaseUrlInterceptor (gắn domain/port) → withCredentialsInterceptor (cookie
    // session, PHẢI trước httpErrorInterceptor — xem doc/huong_dan/wiki-core/fe/07-auth-identity.md)
    // → httpErrorInterceptor (dịch lỗi chung, cuối cùng).
    // withXsrfConfiguration({}) — CSRF Lớp 2 (double-submit-cookie), xem
    // doc/contracts/auth.md §"CSRF — GET /api/antiforgery/token". Đối tượng rỗng vì cookie/header
    // mặc định của Angular (`XSRF-TOKEN` / `X-XSRF-TOKEN`) đã khớp thẳng server, không cần tuỳ
    // biến — gọi tường minh ở đây chỉ để xác nhận CSRF đang BẬT (mặc định của `provideHttpClient`
    // vốn đã bật ngay cả khi không gọi hàm này). Angular luôn chạy interceptor CSRF nội bộ này
    // TRƯỚC mọi interceptor tuỳ biến trong `withInterceptors([...])` — nên nó thấy URL còn tương
    // đối (`/auth/login`) trước khi `apiBaseUrlInterceptor` viết lại thành URL tuyệt đối
    // (`environment.apiBaseUrl` ở dev là `http://localhost:5027/api`); nhờ vậy request KHÔNG bị
    // Angular coi là "cross-origin" rồi bỏ qua việc gắn header `X-XSRF-TOKEN`.
    provideHttpClient(
      withInterceptors([apiBaseUrlInterceptor, withCredentialsInterceptor, httpErrorInterceptor]),
      withXsrfConfiguration({}),
    ),
    providePrimeNG({
      theme: {
        preset: createCorePreset(APP_PALETTE),
        // Chưa có dark mode ở prototype gốc (doc/huong_dan/wiki-core/fe/04-design-token-system.md
        // §Dark mode) — tắt hẳn auto dark-mode-selector của PrimeNG để không lệch giao diện.
        options: { darkModeSelector: false },
      },
    }),
    // i18n runtime — cài đặt thư viện dịch, đăng ký locale data, khôi phục ngôn ngữ đã chọn và
    // đặt `<html lang>` TRƯỚC khi màn hình đầu tiên được vẽ (doc/huong_dan/wiki-core/fe/08-i18n.md
    // §Thứ tự thi công, bước 6). Đặt trên `provideAuthInit()`/`provideCsrfInit()` chỉ để nhóm
    // "khởi động" đọc theo thứ tự nói ra được — `provideAppInitializer` chạy song song, không có
    // ràng buộc thứ tự nào giữa ba cái.
    provideCoreI18n(APP_I18N),
    // Tải phiên đăng nhập hiện tại (nếu có) 1 lần lúc khởi động — xem core/auth/auth-init.provider.ts.
    provideAuthInit(),
    // Mồi cookie CSRF (`XSRF-TOKEN`) 1 lần lúc khởi động, TRƯỚC request ghi đầu tiên (kể cả
    // POST /api/auth/login) — xem core/http/csrf-init.provider.ts.
    provideCsrfInit(),
  ],
};

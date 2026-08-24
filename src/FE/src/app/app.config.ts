import { ApplicationConfig, provideZonelessChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors, withXsrfConfiguration } from '@angular/common/http';
import { providePrimeNG } from 'primeng/config';
import { routes } from './app.routes';
import { httpErrorInterceptor } from './core/interceptors/http-error.interceptor';
import { apiBaseUrlInterceptor } from './core/interceptors/api-base-url.interceptor';
import { withCredentialsInterceptor } from './core/interceptors/credentials.interceptor';
import { provideAuthInit } from './core/auth/auth-init.provider';
import { provideCsrfInit } from './core/http/csrf-init.provider';
import { PlatformManagerPreset } from './core/theme/platform-manager-preset';

export const appConfig: ApplicationConfig = {
  providers: [
    // Zoneless ngay từ đầu (đã CHỐT) — KHÔNG dùng provideZoneChangeDetection/zone.js, xem
    // doc/huong_dan/wiki-core/fe/13-performance.md §1.
    provideZonelessChangeDetection(),
    provideRouter(routes),
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
        preset: PlatformManagerPreset,
        // Chưa có dark mode ở prototype gốc (doc/huong_dan/wiki-core/fe/04-design-token-system.md
        // §Dark mode) — tắt hẳn auto dark-mode-selector của PrimeNG để không lệch giao diện.
        options: { darkModeSelector: false },
      },
    }),
    // Tải phiên đăng nhập hiện tại (nếu có) 1 lần lúc khởi động — xem core/auth/auth-init.provider.ts.
    provideAuthInit(),
    // Mồi cookie CSRF (`XSRF-TOKEN`) 1 lần lúc khởi động, TRƯỚC request ghi đầu tiên (kể cả
    // POST /api/auth/login) — xem core/http/csrf-init.provider.ts.
    provideCsrfInit(),
  ],
};

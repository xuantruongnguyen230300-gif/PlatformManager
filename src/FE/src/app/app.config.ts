import { ApplicationConfig, provideZonelessChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { providePrimeNG } from 'primeng/config';
import { routes } from './app.routes';
import { httpErrorInterceptor } from './core/interceptors/http-error.interceptor';
import { apiBaseUrlInterceptor } from './core/interceptors/api-base-url.interceptor';
import { withCredentialsInterceptor } from './core/interceptors/credentials.interceptor';
import { provideAuthInit } from './core/auth/auth-init.provider';
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
    provideHttpClient(withInterceptors([apiBaseUrlInterceptor, withCredentialsInterceptor, httpErrorInterceptor])),
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
  ],
};

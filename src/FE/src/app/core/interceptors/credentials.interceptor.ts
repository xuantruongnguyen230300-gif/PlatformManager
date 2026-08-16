import { HttpInterceptorFn } from '@angular/common/http';

/**
 * Auth dùng cookie session của ASP.NET Core Identity (KHÔNG JWT, đã CHỐT — xem
 * doc/contracts/auth.md §Ghi chú triển khai). Gắn `withCredentials: true` cho MỌI request để
 * trình duyệt gửi kèm cookie `PlatformManager.Auth` — thiếu bước này, mọi request luôn bị coi
 * là chưa đăng nhập dù đã login thành công (xem src/FE/.claude/docs/api-client.md §Auth).
 *
 * Đăng ký TRƯỚC `httpErrorInterceptor` trong `app.config.ts` — xem
 * doc/huong_dan/wiki-core/fe/07-auth-identity.md.
 */
export const withCredentialsInterceptor: HttpInterceptorFn = (req, next) => next(req.clone({ withCredentials: true }));

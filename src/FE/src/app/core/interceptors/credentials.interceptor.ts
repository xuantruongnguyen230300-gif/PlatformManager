import { HttpInterceptorFn } from '@angular/common/http';

/**
 * Auth dùng cookie session của ASP.NET Core Identity (KHÔNG JWT, đã CHỐT — xem
 * doc/contracts/auth.md §Ghi chú triển khai). Gắn `withCredentials: true` cho MỌI request để
 * trình duyệt gửi kèm cookie phiên — thiếu bước này, mọi request luôn bị coi
 * là chưa đăng nhập dù đã login thành công (xem doc/huong_dan/quy-uoc/fe-api-client.md §Auth).
 * Tên cookie CỐ Ý không nhắc ở đây: nó do BE đặt (`src/BE/PlatformManager.Api/Program.cs`,
 * `options.Cookie.Name`) và mang tên sản phẩm — FE không đọc tên cookie ở bất cứ đâu, chép nó
 * vào `core/` chỉ tạo thêm một chỗ phải sửa khi nền tảng này dựng sản phẩm thứ hai.
 *
 * Đăng ký TRƯỚC `httpErrorInterceptor` trong `app.config.ts` — xem
 * doc/huong_dan/wiki-core/fe/07-auth-identity.md.
 */
export const withCredentialsInterceptor: HttpInterceptorFn = (req, next) => next(req.clone({ withCredentials: true }));

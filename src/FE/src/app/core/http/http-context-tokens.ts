import { HttpContextToken } from '@angular/common/http';

/**
 * Đặt `true` trên request "probe" (vd `GET /api/auth/me` lúc khởi động app) — 401 ở request này
 * là tình huống BÌNH THƯỜNG (chưa đăng nhập), không phải lỗi cần báo cho user qua toast.
 * `httpErrorInterceptor` đọc token này để bỏ qua bước hiện toast nhưng VẪN rethrow lỗi bình
 * thường — nơi gọi (`CurrentUserService.load()`) tự `catchError` để chuyển 401 thành `null`.
 */
export const SKIP_ERROR_TOAST = new HttpContextToken<boolean>(() => false);

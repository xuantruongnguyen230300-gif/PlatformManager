import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';

import { NotificationService } from '../services/notification.service';

/**
 * Interceptor lỗi HTTP dùng chung toàn app — theo `api-client.md`: service của
 * feature không tự bắt lỗi HTTP lặp lại logic. Chuẩn hoá thông báo lỗi (ưu
 * tiên message do API trả về theo field `Message`/`message`, fallback theo
 * status code) rồi đẩy tiếp lỗi cho caller (service/page) tự quyết định xử lý
 * cục bộ nếu cần (vd hiện form-error trong dialog).
 */
export const httpErrorInterceptor: HttpInterceptorFn = (req, next) => {
  const notification = inject(NotificationService);

  return next(req).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse) {
        notification.error(resolveMessage(error));
      } else {
        notification.error('Đã xảy ra lỗi không xác định.');
      }
      return throwError(() => error);
    })
  );
};

function resolveMessage(error: HttpErrorResponse): string {
  const body = error.error as { Message?: string; message?: string } | null;
  if (body?.Message) return body.Message;
  if (body?.message) return body.message;

  switch (error.status) {
    case 0:
      return 'Không thể kết nối tới máy chủ. Kiểm tra lại kết nối mạng.';
    case 400:
      return 'Dữ liệu gửi lên không hợp lệ.';
    case 401:
      return 'Phiên đăng nhập đã hết hạn hoặc chưa đăng nhập.';
    case 403:
      return 'Bạn không có quyền thực hiện thao tác này.';
    case 404:
      return 'Không tìm thấy dữ liệu yêu cầu.';
    case 409:
      return 'Dữ liệu xung đột với bản ghi đã tồn tại.';
    default:
      return 'Máy chủ gặp lỗi, vui lòng thử lại sau.';
  }
}

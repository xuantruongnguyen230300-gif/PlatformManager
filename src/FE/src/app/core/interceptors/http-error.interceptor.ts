import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { IApiResult } from '../http/api-result.model';
import { SKIP_ERROR_TOAST } from '../http/http-context-tokens';
import { ToastService } from '../toast/toast.service';

/**
 * Chỉ dùng khi response KHÔNG có body `IApiResult<T>` hợp lệ (network lỗi, CORS chặn,
 * ProblemDetails từ model-binding) — không phải đường chính, xem
 * doc/huong_dan/wiki-core/fe/02-http-envelope.md §Interceptor.
 */
function fallbackMessageForStatus(status: number): string {
  switch (status) {
    case 0:
      return 'Không thể kết nối tới máy chủ. Kiểm tra kết nối mạng.';
    case 401:
      return 'Bạn cần đăng nhập để tiếp tục.';
    case 403:
      return 'Bạn không có quyền thực hiện thao tác này.';
    case 404:
      return 'Không tìm thấy dữ liệu yêu cầu.';
    default:
      return 'Đã có lỗi xảy ra. Vui lòng thử lại.';
  }
}

/**
 * 1 chỗ duy nhất dịch lỗi HTTP sang toast — đọc đúng `message` (KHÔNG phải `Message`/
 * `ErrorMessage` của envelope cũ đã bỏ, xem doc/huong_dan/wiki-core/fe/02-http-envelope.md
 * §Vấn đề gốc). Giữ nguyên `body` gắn vào `apiResult` trên error rethrow để nơi gọi (thường là
 * component form) tự đọc `fields`/`businessCode` — interceptor chỉ lo phần chung (toast).
 */
export const httpErrorInterceptor: HttpInterceptorFn = (req, next) => {
  const toast = inject(ToastService);

  return next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      const body = (err.error ?? null) as IApiResult<unknown> | null;
      // Request "probe" (vd kiểm tra phiên đăng nhập lúc khởi động) tự đánh dấu bỏ qua toast —
      // 401 ở đó là tình huống bình thường, không phải lỗi cần làm phiền user. Vẫn rethrow bình
      // thường để nơi gọi tự xử lý (CurrentUserService.load() tự catchError → null).
      if (!req.context.get(SKIP_ERROR_TOAST)) {
        const message = body?.message ?? fallbackMessageForStatus(err.status);
        toast.error(message);
      }
      return throwError(() => Object.assign(err, { apiResult: body }));
    }),
  );
};

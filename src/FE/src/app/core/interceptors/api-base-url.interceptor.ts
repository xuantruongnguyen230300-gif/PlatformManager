import { HttpInterceptorFn } from '@angular/common/http';
import { environment } from '../../../environments/environment';

/**
 * Gắn base URL API (`environment.apiBaseUrl`) trước mọi request tương đối — service của feature
 * chỉ gọi đường dẫn ngắn (vd `/criteria`), KHÔNG hardcode domain/port (xem
 * src/FE/.claude/docs/api-client.md §Service pattern). Request đã là absolute URL (bắt đầu
 * bằng `http`) hoặc gọi tài nguyên tĩnh (`/assets/...`) thì giữ nguyên, không prepend.
 */
export const apiBaseUrlInterceptor: HttpInterceptorFn = (req, next) => {
  if (/^https?:\/\//i.test(req.url) || req.url.startsWith('/assets')) {
    return next(req);
  }
  const url = `${environment.apiBaseUrl}${req.url.startsWith('/') ? '' : '/'}${req.url}`;
  return next(req.clone({ url }));
};

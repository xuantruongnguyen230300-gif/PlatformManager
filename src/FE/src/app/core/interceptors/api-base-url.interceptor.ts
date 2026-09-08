import { HttpInterceptorFn } from '@angular/common/http';
import { environment } from '../../../environments/environment';

/**
 * Gắn base URL API (`environment.apiBaseUrl`) trước mọi request tương đối — service của feature
 * chỉ gọi đường dẫn ngắn (vd `/criteria`), KHÔNG hardcode domain/port (xem
 * doc/huong_dan/quy-uoc/fe-api-client.md §Service pattern). Request đã là absolute URL (bắt đầu
 * bằng `http`) thì giữ nguyên, không prepend.
 *
 * **Tài nguyên tĩnh KHÔNG đi qua đây** — chúng dùng `HttpBackend`, thứ bỏ qua toàn bộ chuỗi
 * interceptor. Luật và lý do: doc/huong_dan/wiki-core/fe/02-http-envelope.md §"Tài nguyên tĩnh
 * KHÔNG đi qua chuỗi interceptor".
 *
 * Gỡ 2026-09-05: chỗ này từng miễn trừ tiền tố `'/assets'`. Repo dùng `public/`
 * (ra thẳng gốc site), KHÔNG dùng `/assets` — nên điều kiện đó chưa bao giờ đúng với một
 * request nào, mà lại tạo ấn tượng sai rằng tài nguyên tĩnh đã được lo. Bảng dịch né được là nhờ
 * `useHttpBackend: true` của loader, không nhờ dòng miễn trừ. Không thay bằng danh sách tiền tố
 * của `public/`: danh sách đó phải nhớ cập nhật mỗi lần thêm thư mục tĩnh và hỏng im lặng khi
 * quên.
 */
export const apiBaseUrlInterceptor: HttpInterceptorFn = (req, next) => {
  if (/^https?:\/\//i.test(req.url)) {
    return next(req);
  }
  const url = `${environment.apiBaseUrl}${req.url.startsWith('/') ? '' : '/'}${req.url}`;
  return next(req.clone({ url }));
};

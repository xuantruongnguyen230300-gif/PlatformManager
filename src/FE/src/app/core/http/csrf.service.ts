import { HttpClient, HttpContext } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, map, of } from 'rxjs';
import { SKIP_ERROR_TOAST } from './http-context-tokens';

/**
 * Response thô của `GET /api/antiforgery/token` — KHÔNG bọc `IApiResult` (ngoại lệ có chủ đích,
 * xem doc/contracts/auth.md §"CSRF — GET /api/antiforgery/token"). KHÔNG map sang model app: giá
 * trị `token` không được component/service nào tiêu thụ — mục đích DUY NHẤT của request này là
 * tác dụng phụ `Set-Cookie: XSRF-TOKEN`. Angular `HttpClient` (đã bật XSRF mặc định qua
 * `withXsrfConfiguration({})` trong `app.config.ts`) tự đọc lại cookie này ở MỖI request ghi tiếp
 * theo và gắn header `X-XSRF-TOKEN` — FE không tự cache/gắn tay token.
 */
interface ICsrfTokenResponseDto {
  token: string;
}

/**
 * Phát hành cookie CSRF (`XSRF-TOKEN`) — gọi 1 lần lúc app khởi động qua `provideCsrfInit()`
 * (`core/http/csrf-init.provider.ts`), TRƯỚC request ghi đầu tiên (kể cả `POST /api/auth/login`
 * — CSRF Lớp 2 áp theo METHOD, không có ngoại lệ theo endpoint đăng nhập).
 */
@Injectable({ providedIn: 'root' })
export class CsrfService {
  private readonly http = inject(HttpClient);

  /**
   * Không throw ra ngoài — lỗi ở bước "mồi" cookie CSRF không nên chặn app khởi động (nếu backend
   * tạm thời không tới được, `CurrentUserService.load()` chạy song song cũng sẽ tự xử lý; request
   * ghi đầu tiên sau đó vẫn có thể tự thất bại với 403 rõ ràng thay vì app treo ở màn trắng).
   */
  primeToken(): Observable<void> {
    return this.http
      .get<ICsrfTokenResponseDto>('/antiforgery/token', { context: new HttpContext().set(SKIP_ERROR_TOAST, true) })
      .pipe(
        map(() => undefined),
        catchError(() => of(undefined)),
      );
  }
}

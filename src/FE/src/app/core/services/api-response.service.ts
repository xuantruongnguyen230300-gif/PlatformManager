import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';

import { ApiResponse } from '../http/api-response.model';
import { NotificationService } from './notification.service';

/**
 * Giải nén `ApiResponse<T>` (envelope chuẩn của mọi API `src/BE`, xem
 * `core/http/api-response.model.ts`) thành `T` — dùng trong mọi
 * `*.service.ts` của feature thay vì lặp lại logic kiểm tra `Success`/hiện
 * lỗi ở từng service. Lỗi HTTP (4xx/5xx thật) đã được
 * `httpErrorInterceptor` xử lý; đây là lớp bổ sung cho trường hợp API trả
 * `200 OK` nhưng `Success:false` (lỗi nghiệp vụ được server chủ động báo).
 */
@Injectable({ providedIn: 'root' })
export class ApiResponseService {
  private readonly notification = inject(NotificationService);

  unwrap<T>(source$: Observable<ApiResponse<T>>): Observable<T> {
    return source$.pipe(
      map((res) => {
        if (!res.Success) {
          this.notification.error(res.ErrorMessage ?? 'Yêu cầu thất bại.');
          throw new Error(res.ErrorCode ?? 'API_ERROR');
        }
        return res.Data;
      })
    );
  }
}

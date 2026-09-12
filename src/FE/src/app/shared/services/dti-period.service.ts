import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { IApiResult, unwrapData } from '../../core/http/api-result.model';
import { IPeriodOptions, IPeriodOptionsDto } from '../models/dti-period.model';
import { mapPeriodOptionsDto } from './dti-period.mapper';

/**
 * `GET /api/dashboard/periods` — danh sách Năm/Kỳ có dữ liệu, DÙNG CHUNG cho cả hai màn DTI.
 *
 * Hợp đồng: `doc/contracts/dashboard.md` CONTRACT DB-3 (**AGREED** 2026-08-16).
 * Vì sao nó ở `shared/` chứ không ở một trong hai module: xem đầu `dti-period.model.ts`.
 *
 * ## Ba hành vi của hợp đồng mà nơi gọi ĐƯỢC PHÉP tin
 *
 * 1. **Năm rỗng KHÔNG phải lỗi** — `weeksInYear`/`monthsInYear` là mảng rỗng, `200`. Đừng đẩy ca
 *    này vào nhánh lỗi: ô `Năm` của người dùng sẽ bị khoá cứng ngay lần đầu họ mở một năm mới.
 * 2. `years` **luôn kèm năm hiện tại** dù chưa có dữ liệu — FE không tự chèn năm.
 * 3. Endpoint này **không có mã lỗi nghiệp vụ nào** (DB-3 § Mã lỗi). `year` không phải số nguyên
 *    thì rơi vào `ValidationError` của binder, do `httpErrorInterceptor` lo.
 *
 * ## 🚧 Khoảng trống hợp đồng đã ghi nhận (2026-09-10, vòng 1)
 *
 * Không trường nào của DB-3 nói **kỳ nào là kỳ HIỆN TẠI**. `spec/danh-muc-dti/ui-spec.md` §5.5
 * mục 2 lại yêu cầu dải băng V3 nêu nhãn tuần hiện tại và cấm suy ra bằng đồng hồ máy khách. Vì
 * vậy dải băng ca `Tất cả` chưa dựng ở vòng 1 — xem `danh-muc-dti.page.ts` §Dải băng V3. Câu hỏi
 * đã gửi `backend-expert`; đừng "vá" bằng cách đối chiếu `Date.now()` với `StartDate`/`EndDate`.
 */
@Injectable({ providedIn: 'root' })
export class DtiPeriodService {
  private readonly http = inject(HttpClient);

  /**
   * @param year bỏ trống = năm hiện tại (mặc định của BE). Truyền vào thì `weeksInYear` /
   *   `monthsInYear` là của đúng năm đó.
   */
  getPeriodOptions(year?: number): Observable<IPeriodOptions> {
    // Đường dẫn NGẮN, không tiền tố `/api` — `apiBaseUrlInterceptor` đã ghép sẵn
    // `environment.apiBaseUrl` (đã chứa `/api`). Xem fe-api-client.md §Service pattern.
    let params = new HttpParams();
    if (year !== undefined) params = params.set('year', year);

    return this.http
      .get<IApiResult<IPeriodOptionsDto>>('/dashboard/periods', { params })
      .pipe(map((res) => mapPeriodOptionsDto(unwrapData(res))));
  }
}

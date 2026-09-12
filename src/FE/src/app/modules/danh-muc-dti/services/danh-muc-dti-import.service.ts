import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { IApiResult, unwrapData } from '../../../core/http/api-result.model';
import { IImportJobStartedDto, IImportJobStatus, IImportJobStatusDto, IWritePeriod } from '../models/danh-muc-dti.model';
import { mapImportJobStatusDto } from './danh-muc-dti.mapper';

/**
 * DM-7 — nạp danh mục từ `.csv` / `.xlsx` / `.xls`. **Hai bước**, vì đây là thao tác chạy dài:
 * `POST /api/import` trả `jobId` ngay rồi FE **poll** `GET /api/import/{jobId}`.
 *
 * 📖 Khuôn poll (`takeWhile(..., true)` + `takeUntilDestroyed`) ở
 * `doc/huong_dan/quy-uoc/fe-api-client.md` §"Long-running operation — poll pattern". Vòng poll
 * **không** nằm trong service này mà ở `pages/danh-muc-dti/criteria-import-flow.ts`: nó phải chết
 * cùng vòng đời TRANG, mà một service `providedIn: 'root'` thì không có vòng đời đó.
 *
 * ⚠️ **HTTP của bước 1 là 200, KHÔNG phải 202.** `ApiControllerBase.HandleResult<T>` map mọi
 * response thành công về 200, và `ErrorCode` không có member nào mang 202 — "đã bắt đầu chứ chưa
 * xong" thể hiện ở tầng dữ liệu (`jobId` cần poll tiếp), không ở HTTP status. Mẫu trong
 * `fe-api-client.md` §Long-running mở đầu bằng *"BE trả 202 + jobId"*; hợp đồng DM-7 nói ngược
 * lại, có bằng chứng `file:dòng`, và **hợp đồng thắng**. Không có nhánh nào ở FE đọc status 202.
 *
 * Tách khỏi `DanhMucDtiService` chứ không nhét chung: đường import là `multipart/form-data` + một
 * vòng poll, không dùng chung một dòng nào với lưới. Gộp hai thứ đó cho ra một file mà không ai
 * đọc hết được, và cổng G5 thì đòi mỗi `*.service.ts` có `.spec.ts` cạnh nó — hai file nhỏ có hai
 * bộ test nói đúng chuyện của mình vẫn rẻ hơn một file lớn.
 */
@Injectable({ providedIn: 'root' })
export class DanhMucDtiImportService {
  private readonly http = inject(HttpClient);

  /**
   * Bước 1 — gửi file + **kỳ đích của toàn bộ file**.
   *
   * 🛑 `period` là **BẮT BUỘC** và đi trong chính `multipart`, không phải query string: đây là dữ
   * liệu của lời ghi, không phải trạng thái màn hình. Vắng mặt ⇒ `400 IMPORT.PERIOD_REQUIRED` —
   * server **không** âm thầm chọn hộ "hôm nay".
   *
   * 🛑 **KHÔNG** đính kèm tên/ID người nạp file. Danh tính lấy từ phiên đăng nhập ở phía server;
   * một trường do máy khách gửi lên thì máy khách sửa được, và nhật ký import là đúng chỗ không
   * được phép tin máy khách (Q35).
   *
   * ⚠️ **Đừng đặt `Content-Type` bằng tay.** `FormData` phải để trình duyệt tự sinh header kèm
   * `boundary`; đặt `multipart/form-data` trần là mất `boundary` và server không tách nổi phần nào
   * ra phần nào — lỗi 400 trông y như "file hỏng".
   */
  startImport(file: File, period: IWritePeriod): Observable<string> {
    const form = new FormData();
    form.append('file', file, file.name);
    form.append('period', period.Period);
    if (period.Year !== undefined) form.append('year', `${period.Year}`);

    return this.http
      .post<IApiResult<IImportJobStartedDto>>('/import', form)
      .pipe(map((res) => unwrapData(res).jobId));
  }

  /**
   * Bước 2 — một nhịp poll.
   *
   * `404 IMPORT.JOB_NOT_FOUND` (sai id, hoặc job đã bị dọn theo retention) là **tín hiệu DỪNG**:
   * nó đi ra đây dưới dạng lỗi HTTP, nên nơi gọi phải bắt và thoát vòng. Không có tín hiệu đó thì
   * FE poll vô hạn một job không bao giờ tồn tại.
   */
  getJobStatus(jobId: string): Observable<IImportJobStatus> {
    return this.http
      .get<IApiResult<IImportJobStatusDto>>(`/import/${jobId}`)
      .pipe(map((res) => mapImportJobStatusDto(unwrapData(res))));
  }
}

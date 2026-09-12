import { HttpClient, HttpParams, HttpResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { IApiResult, unwrapData } from '../../../core/http/api-result.model';
import {
  IDashboardAggregate,
  IDashboardAggregateDto,
  IDashboardParams,
  IExportedFile,
} from '../models/dashboard.model';
import { mapDashboardAggregateDto } from './dashboard.mapper';

/**
 * Tên file dùng khi **không đọc được** `Content-Disposition`.
 *
 * Header đó chỉ đọc được từ JavaScript khi cùng origin, hoặc khi server khai
 * `Access-Control-Expose-Headers`. Không có đường lùi thì trình duyệt lưu file thành `download`
 * không đuôi — mở bằng Excel không được, và người dùng không biết vì sao.
 *
 * 🛑 KHÔNG tự ghép `bao-cao-dti_Tuan-33-2026.xlsx` ở FE: số tuần ISO là phép tính lịch mà
 * `spec/danh-muc-dti/business-rules.md` §5.1 cấm FE tự làm, và một cái tên sai tuần còn tệ hơn một
 * cái tên chung chung — nó đi theo file qua email và không ai kiểm lại.
 */
const FALLBACK_EXPORT_FILE_NAME = 'bao-cao-dti.xlsx';

/** Đoạn nhận dạng JSON trong `Content-Type` — `application/json`, `application/problem+json`, … */
const JSON_CONTENT_TYPE = 'json';

/**
 * Gọi API màn Dashboard DTI — `doc/contracts/dashboard.md`.
 *
 * Hai endpoint: **DB-1** (`GET /api/dashboard`) và **DB-4** (`GET /api/dashboard/export`). Danh
 * sách Năm/Kỳ (DB-3) **không** ở đây — nó đi qua `shared/services/dti-period.service.ts` vì cả hai
 * màn DTI cùng dùng.
 *
 * 🛑 **DB-4 là endpoint DUY NHẤT của hệ thống không trả envelope** khi thành công (thân response
 * là bytes), nhưng **lỗi thì vẫn là `IApiResult` JSON**. `exportReport` vì vậy kiểm `Content-Type`
 * trước khi coi body là file — nếu không, người dùng nhận về một `.xlsx` hỏng chứa nguyên văn
 * thông báo lỗi, và triệu chứng lúc đó không trỏ về đâu cả.
 */
@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly http = inject(HttpClient);

  /**
   * DB-1 — toàn bộ số liệu của một kỳ trong MỘT lời gọi: `kpi` + `groups` + `trend` + `table`.
   *
   * 🛑 Ba tham số lọc (`search`/`groupId`/`status`) **chỉ áp cho `table`**; `kpi`, `groups`,
   * `trend` luôn tính trên TOÀN BỘ chỉ tiêu của kỳ. Đó là luật của BE, nhưng FE phải biết nó để
   * không đi kết luận ngược — xem `dashboard.page.ts` §Ca Q32 (proxy nhận dạng đọc từ `kpi`, KHÔNG
   * từ `table`, chốt Q69).
   *
   * `Date` và `Year` loại trừ nhau — xem `IDashboardParams`.
   */
  getAggregate(params: IDashboardParams): Observable<IDashboardAggregate> {
    let httpParams = new HttpParams().set('mode', params.Mode);

    if (params.Date) httpParams = httpParams.set('date', params.Date);
    else if (params.Year !== undefined) httpParams = httpParams.set('year', params.Year);

    if (params.Search) httpParams = httpParams.set('search', params.Search);
    if (params.GroupId) httpParams = httpParams.set('groupId', params.GroupId);
    // Chuỗi rỗng là một giá trị NGOÀI bốn giá trị Q4 ⇒ 400 `DASHBOARD.STATUS_INVALID`. Không lọc
    // thì không gửi khoá.
    if (params.Status) httpParams = httpParams.set('status', params.Status);

    return this.http
      .get<IApiResult<IDashboardAggregateDto>>('/dashboard', { params: httpParams })
      .pipe(map((res) => mapDashboardAggregateDto(unwrapData(res))));
  }

  /**
   * DB-4 — `Xuất báo cáo`. Tải **thẳng** file, không dialog, không xem trước (Q13).
   *
   * Tham số đúng bằng bộ của DB-1, không thêm gì: export **tôn trọng bộ lọc đang áp** (Q23), và
   * chính vì thế file mang thêm dòng `Bộ lọc đang áp` — nó rời khỏi màn hình, nên người nhận qua
   * email không có cách nào khác để biết nó được xuất lúc đang lọc gì.
   *
   * 🛑 `mode=year` **không** gửi tới đây: nút đã `disabled` ở chế độ đó (`400
   * DASHBOARD.EXPORT_MODE_UNSUPPORTED` là lưới chặn phía server, không phải đường đi bình thường).
   *
   * ## Vì sao `observe: 'response'` chứ không chỉ lấy body
   *
   * Cần **hai** thứ chỉ có ở header: `Content-Type` (để biết 200 này là file hay là một envelope
   * lỗi) và `Content-Disposition` (tên file). Lấy mỗi body là mất cả hai.
   */
  exportReport(params: IDashboardParams): Observable<IExportedFile> {
    let httpParams = new HttpParams().set('mode', params.Mode);

    if (params.Date) httpParams = httpParams.set('date', params.Date);
    else if (params.Year !== undefined) httpParams = httpParams.set('year', params.Year);

    if (params.Search) httpParams = httpParams.set('search', params.Search);
    if (params.GroupId) httpParams = httpParams.set('groupId', params.GroupId);
    if (params.Status) httpParams = httpParams.set('status', params.Status);

    return this.http
      .get('/dashboard/export', { params: httpParams, responseType: 'blob', observe: 'response' })
      .pipe(map((response) => this.readExport(response)));
  }

  /**
   * 🛑 **Kiểm `Content-Type` TRƯỚC khi coi body là file.** Nhận JSON ở status 200 nghĩa là hợp
   * đồng đã gãy ở phía server — và nếu bỏ qua phép kiểm này thì người dùng lưu được một `.xlsx`
   * mở lên chỉ có một dòng JSON. Ném lỗi để nó rơi vào đúng nhánh lỗi của trang.
   */
  private readExport(response: HttpResponse<Blob>): IExportedFile {
    const contentType = response.headers.get('Content-Type') ?? response.body?.type ?? '';
    if (contentType.includes(JSON_CONTENT_TYPE)) {
      throw new Error(`DB-4 trả JSON ở status ${response.status} — body không phải file`);
    }
    if (!response.body || response.body.size === 0) {
      throw new Error('DB-4 trả file RỖNG');
    }
    return { Blob: response.body, FileName: readFileName(response.headers.get('Content-Disposition')) };
  }
}

/**
 * Tách tên file khỏi `Content-Disposition`.
 *
 * Ưu tiên `filename*` (RFC 5987, có mã hoá) rồi mới tới `filename` ASCII — hợp đồng DB-4 gửi
 * **cả hai**, và bản ASCII tồn tại chính vì mọi hệ tệp mở được nó mà không phải giải mã.
 */
function readFileName(disposition: string | null): string {
  if (!disposition) return FALLBACK_EXPORT_FILE_NAME;

  const encoded = /filename\*=UTF-8''([^;]+)/i.exec(disposition);
  if (encoded) {
    try {
      return decodeURIComponent(encoded[1].trim());
    } catch {
      // Chuỗi mã hoá hỏng thì rơi xuống bản ASCII bên dưới — KHÔNG ném: tên file sai không đáng
      // để huỷ một lượt tải đã thành công.
    }
  }

  const plain = /filename="?([^";]+)"?/i.exec(disposition);
  return plain ? plain[1].trim() : FALLBACK_EXPORT_FILE_NAME;
}

import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { IApiResult, unwrapData } from '../../../core/http/api-result.model';
import { IOwnerPage, IOwnerPageDto } from '../models/danh-muc-dti.model';
import { mapOwnerPageDto } from './danh-muc-dti.mapper';

/**
 * Số option nạp một lượt. Dưới trần **200** mà `GET /api/users` cưỡng chế
 * (`doc/contracts/users.md` §Giới hạn phân trang — vượt trần là `400 ValidationError`, không phải
 * âm thầm cắt), và đủ rộng cho mọi tổ chức cỡ vừa mà không kéo cả bảng người dùng về.
 */
const OWNER_PAGE_SIZE = 100;

/**
 * Nguồn dữ liệu ô `Phụ trách` của dialog Thêm/Sửa — **tái dùng `GET /api/users`** (Q12a,
 * 2026-09-09). Không endpoint mới, không DTO mới.
 *
 * ## Vì sao ở `modules/danh-muc-dti/` chứ không ở `shared/`
 *
 * `shared/` dành cho thứ **≥2 feature** dùng — đó là lý do `DtiPeriodService` (DM-8/DB-3) ở đó:
 * cả Danh mục lẫn Dashboard đều gọi. Ô `Phụ trách` thì hôm nay chỉ có một nơi tiêu thụ, và một
 * API dùng chung chưa ai dùng chung là một API sẽ sai hình dạng lúc có người thứ hai dùng thật.
 *
 * ## Quyền: KHỚP SẴN, nhưng không được CƯỠNG CHẾ
 *
 * `UsersController` chặn `[Authorize(Roles = SuperAdmin,Admin)]`, mà lần seed đầu cấp key ghi DTI
 * cho **`Admin` và chỉ `Admin`** — nên người mở được dialog cũng gọi được endpoint này.
 *
 * ⚠️ Sự trùng khớp đó đến từ **dữ liệu seed**, không phải một ràng buộc máy giữ: cấp key DTI cho
 * một vai khác `Admin` qua màn Phân quyền là đủ để ô này nhận `403` trong khi phần còn lại của
 * dialog chạy bình thường. Nơi gọi phải xử lý như **lỗi tải một ô chọn** — ô `disabled` kèm câu
 * giải thích tại chỗ — **không** đóng dialog và **không** chặn lưu bốn trường danh mục.
 *
 * 🛑 Và **không** nới quyền của `UsersController` để phục vụ màn này.
 */
@Injectable({ providedIn: 'root' })
export class CriteriaOwnerService {
  private readonly http = inject(HttpClient);

  /**
   * @param searchText gõ tới đâu lọc tới đó. Rỗng ⇒ không gửi tham số — gửi chuỗi rỗng là gửi
   *   một bộ lọc, và server không có lý do nào để đoán rằng "rỗng" nghĩa là "bỏ lọc".
   */
  search(searchText: string): Observable<IOwnerPage> {
    let params = new HttpParams().set('page', 1).set('pageSize', OWNER_PAGE_SIZE);
    if (searchText) params = params.set('searchText', searchText);

    return this.http
      .get<IApiResult<IOwnerPageDto>>('/users', { params })
      .pipe(map((res) => mapOwnerPageDto(unwrapData(res))));
  }
}

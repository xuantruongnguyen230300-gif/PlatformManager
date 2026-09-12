import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { IApiResult, unwrapData } from '../../../core/http/api-result.model';
import {
  ICriteriaCreated,
  ICriteriaDto,
  ICriteriaGrid,
  ICriteriaGridDto,
  ICriteriaGroup,
  ICriteriaGroupDto,
  ICriteriaListParams,
  ICriteriaRow,
  ICriteriaRowDto,
  ICriteriaWritePayload,
  IDeleteCriteriaResult,
  IDeleteCriteriaResultDto,
  IInlineAssessmentPayload,
} from '../models/danh-muc-dti.model';
import {
  mapCriteriaDto,
  mapCriteriaGridDto,
  mapCriteriaGroupDto,
  mapCriteriaRowDto,
  mapDeleteCriteriaResultDto,
  toCriteriaWriteBody,
  toInlineAssessmentBody,
} from './danh-muc-dti.mapper';

/**
 * Gọi API màn Danh mục DTI — `doc/contracts/danh-muc-dti.md`. DM-1 · DM-2 (đọc) và DM-3 … DM-6
 * (ghi). **DM-7 (import) ở service riêng** — `danh-muc-dti-import.service.ts`: nó là
 * `multipart` + một vòng poll, không dùng chung dòng nào với năm phương thức ở đây.
 *
 * Gate BE: DM-1 và DM-2 giữ `[Authorize]` trần — ai đăng nhập cũng đọc được (Q39). Quyền GHI đi
 * qua khối `canWrite`/`isEditable` trong chính response của DM-2, không qua route guard; lớp thật
 * là `403` từ `[RequirePermission]` ở BE, còn ẩn/tắt control chỉ là lớp trải nghiệm.
 *
 * ## Hai đường ghi trả về HAI shape khác nhau, và đó là lý do trang xử lý chúng khác nhau
 *
 * | Đường | Response | Trang làm gì |
 * | --- | --- | --- |
 * | `create` (DM-3) | `CriteriaDto` — **không** có trường đánh giá nào | **tải lại lưới** (Q49) |
 * | `update` (DM-4) · `updateAssessment` (DM-6) | một **dòng lưới** đầy đủ | **thay dòng tại chỗ** |
 *
 * Đừng làm hai đường giống nhau cho gọn: chèn một `CriteriaDto` vào lưới sẽ dựng ra một hàng
 * thiếu `progressPercent`, thiếu `version`, thiếu `assessmentPeriod` — trông y như một chỉ tiêu
 * chưa có đánh giá, trong khi nó vừa được tạo kèm đánh giá.
 */
@Injectable({ providedIn: 'root' })
export class DanhMucDtiService {
  private readonly http = inject(HttpClient);

  /**
   * DM-1 — 6 nhóm chỉ tiêu, đã sắp theo `displayOrder` ở BE. **FE không sắp lại.**
   * Tải một lần lúc khởi tạo màn, dùng cho **cả** ô lọc `Nhóm chỉ tiêu` lẫn ô `Nhóm` trong hộp
   * thoại Thêm/Sửa — một lời gọi, hai nơi tiêu thụ.
   */
  getGroups(): Observable<ICriteriaGroup[]> {
    return this.http
      .get<IApiResult<ICriteriaGroupDto[]>>('/criteria-groups')
      .pipe(map((res) => unwrapData(res).map(mapCriteriaGroupDto)));
  }

  /**
   * DM-2 — lưới, phân trang **server-side**.
   *
   * 🛑 Ba tham số lọc chỉ được gửi khi CÓ giá trị. Gửi chuỗi rỗng cho `status` là gửi một giá trị
   * ngoài bốn giá trị Q4, và card chốt rõ: server trả **400 `CRITERIA.STATUS_INVALID`**, KHÔNG âm
   * thầm bỏ lọc. `year` và `period` thì luôn gửi — chúng có mặc định thật (năm hiện tại / `all`),
   * không phải "không lọc".
   *
   * `unwrapData` chứ **không** phải toán tử hợp nhất-null kèm một lưới rỗng làm giá trị mặc định:
   * lưới rỗng là câu trả lời HỢP LỆ của server ("không chỉ tiêu nào khớp bộ lọc"), nên dựng nó ra
   * từ một envelope hỏng là nói dối bằng đúng thứ người dùng tin nhất — một bảng trống kèm dòng
   * "không có dữ liệu", không toast, không dấu vết. Envelope thiếu `data` ném lỗi và rơi vào khối
   * lỗi của trang.
   *
   * *(Gọi TÊN toán tử thay vì gõ nguyên văn nó: lệnh grep của cổng ở
   * `doc/huong_dan/quy-uoc/fe-api-client.md` §Service pattern không phân biệt code với chú thích,
   * nên một chú thích tử tế sẽ làm cổng đỏ mà không có gì hỏng cả. Chính file đó cảnh báo đúng ca
   * này — và dòng này đã dính thật ngày 2026-09-10.)*
   */
  getGrid(params: ICriteriaListParams): Observable<ICriteriaGrid> {
    let httpParams = new HttpParams()
      .set('year', params.Year)
      .set('period', params.Period)
      .set('page', params.Page)
      .set('pageSize', params.PageSize);

    if (params.Search) httpParams = httpParams.set('search', params.Search);
    if (params.GroupId) httpParams = httpParams.set('groupId', params.GroupId);
    if (params.Status) httpParams = httpParams.set('status', params.Status);

    return this.http
      .get<IApiResult<ICriteriaGridDto>>('/criteria', { params: httpParams })
      .pipe(map((res) => mapCriteriaGridDto(unwrapData(res))));
  }

  /**
   * DM-3 — tạo chỉ tiêu. Trả `CriteriaDto`, **không** phải một dòng lưới ⇒ nơi gọi phải tải lại
   * lưới (Q49), không được chèn dòng.
   */
  create(payload: ICriteriaWritePayload): Observable<ICriteriaCreated> {
    return this.http
      .post<IApiResult<ICriteriaDto>>('/criteria', toCriteriaWriteBody(payload))
      .pipe(map((res) => mapCriteriaDto(unwrapData(res))));
  }

  /**
   * DM-4 — sửa chỉ tiêu, **MỘT** request cho cả 4 trường danh mục lẫn 6 trường đánh giá.
   *
   * 🛑 Không tách làm hai lời gọi "cho gọn": trên màn hình đó là **một** nút `Lưu chỉ tiêu` (Q9),
   * và tách đôi tạo ra một cửa sổ hỏng nửa vời — danh mục lưu xong, đánh giá lỗi, người dùng đọc
   * thông báo lỗi trong khi tên chỉ tiêu đã đổi. Một use case, một transaction.
   */
  update(id: string, payload: ICriteriaWritePayload): Observable<ICriteriaRow> {
    return this.http
      .put<IApiResult<ICriteriaRowDto>>(`/criteria/${id}`, toCriteriaWriteBody(payload))
      .pipe(map((res) => mapCriteriaRowDto(unwrapData(res))));
  }

  /**
   * DM-6 — sửa inline, đúng 2 trường nghiệp vụ.
   *
   * 🛑 Ngữ nghĩa là `PUT` **ghi đè cả hai**, không phải patch từng phần: nơi gọi LUÔN gửi cả
   * `ProgressPercent` lẫn `Note`, kể cả khi người dùng chỉ sửa một — bỏ trống một trường sẽ
   * null-hoá trường đó.
   */
  updateAssessment(id: string, payload: IInlineAssessmentPayload): Observable<ICriteriaRow> {
    return this.http
      .put<IApiResult<ICriteriaRowDto>>(`/criteria/${id}/assessment`, toInlineAssessmentBody(payload))
      .pipe(map((res) => mapCriteriaRowDto(unwrapData(res))));
  }

  /**
   * DM-5 — xoá. **BE** quyết xoá cứng hay mềm; FE chỉ đọc `hardDeleted` để chọn câu thông báo
   * SAU khi xoá.
   *
   * 🛑 FE **không** đoán trước bằng `row.assessmentId !== null`: trường đó chỉ phản ánh **kỳ đang
   * xem**, không phản ánh toàn bộ lịch sử nhiều năm — bản cũ làm vậy và câu xác nhận sai trong
   * đúng ca người dùng cần nó đúng nhất.
   */
  remove(id: string): Observable<IDeleteCriteriaResult> {
    return this.http
      .delete<IApiResult<IDeleteCriteriaResultDto>>(`/criteria/${id}`)
      .pipe(map((res) => mapDeleteCriteriaResultDto(unwrapData(res))));
  }
}

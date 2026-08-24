import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { DanhMucDtiService } from './danh-muc-dti.service';
import { IApiResult } from '../../../core/http/api-result.model';
import { IPagedResultDto } from '../../../core/http/paged-result.model';
import { ICriteriaRowDto } from '../models/danh-muc-dti.model';

/**
 * Payload thật của `GET /api/criteria` (`CriteriaGridResultDto` — cùng shape `PagedList<T>` đã
 * CHỐT: `items`/`totalCount`/`page`/`pageSize`, xem
 * doc/huong_dan/quy-uoc/be-cqrs-handler.md §"Shape phân trang"). Cố ý dùng `JSON.parse`:
 *
 * - hàng `c1` **không có** các key nullable (`progressPercent`, `note`, `status`...) — đúng những
 *   gì BE gửi khi giá trị là `null` (`DefaultIgnoreCondition = WhenWritingNull`);
 * - hàng `c2` có `progressPercent: 0` — số 0 hợp lệ, KHÔNG được biến thành `null`.
 */
const WIRE_JSON = `{
  "isLive": true,
  "items": [
    {
      "criteriaId": "c1", "code": "DTI-01", "name": "Chỉ tiêu 1",
      "groupId": "g1", "groupCode": "N1", "groupName": "Nhóm 1",
      "maxScore": 10, "evidences": [], "isEditable": true
    },
    {
      "criteriaId": "c2", "code": "DTI-02", "name": "Chỉ tiêu 2",
      "groupId": "g1", "groupCode": "N1", "groupName": "Nhóm 1",
      "maxScore": 10, "evidences": [], "isEditable": true,
      "assessmentId": "a2", "progressPercent": 0, "note": "", "status": "Đang thực hiện"
    }
  ],
  "totalCount": 42,
  "page": 2,
  "pageSize": 20
}`;

function envelope(): IApiResult<IPagedResultDto<ICriteriaRowDto>> {
  return {
    data: JSON.parse(WIRE_JSON) as IPagedResultDto<ICriteriaRowDto>,
    message: null,
    status: 'SUCCESS',
    code: 'Success',
    businessCode: null,
    traceId: 'trace-criteria',
    retryable: null,
    fields: null,
  };
}

/**
 * Chốt chặn cho 2 lỗi P0 cùng nằm trên đúng 1 đường gọi này:
 * 1. shape phân trang đọc sai field (`total` thay vì `totalCount`, hoặc field đã bỏ `totalPages`)
 *    → tổng số bản ghi hiển thị `undefined`;
 * 2. mapper đọc thẳng field nullable → `undefined` thay vì `null` khi BE bỏ key.
 */
describe('DanhMucDtiService.getList — đọc đúng payload BE thật', () => {
  let service: DanhMucDtiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection(), provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(DanhMucDtiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('lấy tổng số bản ghi từ `totalCount` và chuẩn hoá field nullable vắng mặt thành `null`', (done) => {
    service.getList({ Page: 2, PageSize: 20 }).subscribe((result) => {
      expect(result.TotalCount).withContext('trước đây đọc `total` → undefined').toBe(42);
      expect(result.Page).toBe(2);
      expect(result.PageSize).toBe(20);
      expect(result.Items.length).toBe(2);

      const [c1, c2] = result.Items;
      expect(c1.ProgressPercent === null).withContext('key vắng mặt → null, không undefined').toBeTrue();
      expect(c1.Note === null).toBeTrue();
      expect(c1.Status === null).toBeTrue();
      expect(c1.AssessmentId === null).toBeTrue();

      expect(c2.ProgressPercent).withContext('`?? null` KHÔNG được nuốt số 0').toBe(0);
      expect(c2.Note).withContext('`?? null` KHÔNG được nuốt chuỗi rỗng').toBe('');
      expect(c2.AssessmentId).toBe('a2');
      done();
    });

    const req = httpMock.expectOne((r) => r.url === '/criteria');
    expect(req.request.params.get('page')).toBe('2');
    req.flush(envelope());
  });
});

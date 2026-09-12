import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { IApiResult } from '../../../core/http/api-result.model';
import { ICriteriaGrid, ICriteriaGridDto, ICriteriaGroup } from '../models/danh-muc-dti.model';
import { DanhMucDtiService } from './danh-muc-dti.service';

function ok<T>(data: T): IApiResult<T> {
  return {
    data,
    message: null,
    status: 'SUCCESS',
    code: 'Success',
    businessCode: null,
    traceId: 'trace-danh-muc-dti',
    retryable: null,
    fields: null,
  };
}

const GRID_DTO: ICriteriaGridDto = {
  items: [],
  page: 1,
  pageSize: 10,
  totalCount: 0,
  canWrite: true,
  isEditable: false,
  editBlockedBy: ['PERIOD_NOT_WEEKLY'],
  isCurrentYear: true,
  currentPeriod: '2026-W33',
  currentPeriodLabel: 'Tuần 33/2026 (10/08 – 16/08/2026)',
};

describe('DanhMucDtiService (CONTRACT DM-1 + DM-2)', () => {
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

  it('getGroups(): GET /criteria-groups, giữ NGUYÊN thứ tự BE trả về', () => {
    let groups: ICriteriaGroup[] | undefined;
    service.getGroups().subscribe((value) => (groups = value));

    const req = httpMock.expectOne('/criteria-groups');
    expect(req.request.method).toBe('GET');
    req.flush(
      ok([
        { id: 'g1', code: '1', name: 'Hạ tầng và Nền tảng số', displayOrder: 1 },
        { id: 'g2', code: '2', name: 'Nhân lực số', displayOrder: 2 },
      ]),
    );

    expect(groups?.map((g) => g.Code)).withContext('BE sắp theo displayOrder, FE không sắp lại').toEqual(['1', '2']);
  });

  it('getGrid(): year/period/page/pageSize LUÔN gửi — chúng có mặc định thật, không phải "không lọc"', () => {
    service.getGrid({ Year: 2026, Period: 'all', Page: 1, PageSize: 10 }).subscribe();

    const req = httpMock.expectOne((r) => r.url === '/criteria');
    expect(req.request.params.get('year')).toBe('2026');
    expect(req.request.params.get('period')).toBe('all');
    expect(req.request.params.get('page')).toBe('1');
    expect(req.request.params.get('pageSize')).toBe('10');
    req.flush(ok(GRID_DTO));
  });

  it('🛑 status/groupId/search rỗng thì KHÔNG gửi — chuỗi rỗng là giá trị ngoài 4 giá trị Q4 ⇒ 400', () => {
    service.getGrid({ Year: 2026, Period: 'all', Page: 1, PageSize: 10, Search: '', GroupId: '', Status: '' }).subscribe();

    const req = httpMock.expectOne((r) => r.url === '/criteria');
    expect(req.request.params.has('status')).toBeFalse();
    expect(req.request.params.has('groupId')).toBeFalse();
    expect(req.request.params.has('search')).toBeFalse();
    req.flush(ok(GRID_DTO));
  });

  it('getGrid(): ba tham số lọc có giá trị thì đi nguyên văn — `status` gửi ĐÚNG chuỗi tiếng Việt', () => {
    service
      .getGrid({ Year: 2025, Period: '2025-W33', Page: 2, PageSize: 20, Search: '1.1', GroupId: 'g1', Status: 'Hoàn thành' })
      .subscribe();

    const req = httpMock.expectOne((r) => r.url === '/criteria');
    expect(req.request.params.get('search')).toBe('1.1');
    expect(req.request.params.get('groupId')).toBe('g1');
    expect(req.request.params.get('status'))
      .withContext('mã rút gọn kiểu "xong" sẽ nhận 400 CRITERIA.STATUS_INVALID')
      .toBe('Hoàn thành');
    req.flush(ok(GRID_DTO));
  });

  it('getGrid(): khối quyền cấp màn về tới model, kể cả khi lưới rỗng (ca T9)', () => {
    let grid: ICriteriaGrid | undefined;
    service.getGrid({ Year: 2026, Period: '2026-08', Page: 1, PageSize: 10 }).subscribe((value) => (grid = value));

    httpMock.expectOne((r) => r.url === '/criteria').flush(ok(GRID_DTO));

    expect(grid?.Items).toEqual([]);
    expect(grid?.Access.CanWrite).toBeTrue();
    expect(grid?.Access.IsEditable).toBeFalse();
    expect(grid?.Access.EditBlockedBy).toEqual(['PERIOD_NOT_WEEKLY']);
    expect(grid?.Access.CurrentPeriod).withContext('Q72 — luôn có mặt, kể cả lưới rỗng').toBe('2026-W33');
  });

  it('envelope thiếu `data` ném lỗi — KHÔNG dựng ra một lưới rỗng trông như câu trả lời hợp lệ', () => {
    let failed = false;
    service.getGrid({ Year: 2026, Period: 'all', Page: 1, PageSize: 10 }).subscribe({ error: () => (failed = true) });

    httpMock.expectOne((r) => r.url === '/criteria').flush(ok<ICriteriaGridDto | null>(null));

    expect(failed).toBeTrue();
  });
});

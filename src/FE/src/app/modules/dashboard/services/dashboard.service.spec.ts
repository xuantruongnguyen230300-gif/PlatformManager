import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { IApiResult } from '../../../core/http/api-result.model';
import { IDashboardAggregate, IDashboardAggregateDto } from '../models/dashboard.model';
import { DashboardService } from './dashboard.service';

function ok<T>(data: T): IApiResult<T> {
  return {
    data,
    message: null,
    status: 'SUCCESS',
    code: 'Success',
    businessCode: null,
    traceId: 'trace-dashboard',
    retryable: null,
    fields: null,
  };
}

const DTO: IDashboardAggregateDto = {
  mode: 'week',
  periodLabel: 'Tuần 33/2026 (10/08 – 16/08/2026)',
  periodStart: '2026-08-10',
  periodEnd: '2026-08-16',
  kpi: { up: 0, flat: 0, down: 0, done: 26, totalCriteria: 62 },
  groups: [],
  trend: [],
  table: [],
};

describe('DashboardService (CONTRACT DB-1)', () => {
  let service: DashboardService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection(), provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(DashboardService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('gọi đường dẫn NGẮN /dashboard, mode luôn có mặt', () => {
    let result: IDashboardAggregate | undefined;
    service.getAggregate({ Mode: 'week' }).subscribe((value) => (result = value));

    const req = httpMock.expectOne((r) => r.url === '/dashboard');
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('mode')).toBe('week');
    req.flush(ok(DTO));

    expect(result?.Kpi.TotalCriteria).toBe(62);
    expect(result?.Kpi.OverallProgress).toBeNull();
  });

  /**
   * 🛑 Q61: gửi CẢ `date` lẫn `year` ở chế độ tuần thì `year` phải bằng năm ISO của tuần chứa
   * `date`, lệch là `400`. Gửi đúng một trong hai làm cả lớp lỗi đó không tồn tại.
   */
  it('🛑 có `date` thì KHÔNG gửi `year` — tránh hẳn lớp lỗi Q61', () => {
    service.getAggregate({ Mode: 'week', Date: '2026-08-10', Year: 2026 }).subscribe();

    const req = httpMock.expectOne((r) => r.url === '/dashboard');
    expect(req.request.params.get('date')).toBe('2026-08-10');
    expect(req.request.params.has('year')).toBeFalse();
    req.flush(ok(DTO));
  });

  it('không có `date` thì gửi `year` — kỳ hiện tại của năm đó do server chọn', () => {
    service.getAggregate({ Mode: 'month', Year: 2025 }).subscribe();

    const req = httpMock.expectOne((r) => r.url === '/dashboard');
    expect(req.request.params.get('year')).toBe('2025');
    expect(req.request.params.has('date')).toBeFalse();
    req.flush(ok({ ...DTO, mode: 'month' }));
  });

  it('bộ lọc bảng chi tiết đi kèm; giá trị rỗng thì KHÔNG gửi khoá', () => {
    service.getAggregate({ Mode: 'year', Year: 2026, Search: '', GroupId: 'g1', Status: 'Hoàn thành' }).subscribe();

    const req = httpMock.expectOne((r) => r.url === '/dashboard');
    expect(req.request.params.has('search')).toBeFalse();
    expect(req.request.params.get('groupId')).toBe('g1');
    expect(req.request.params.get('status')).toBe('Hoàn thành');
    req.flush(ok({ ...DTO, mode: 'year' }));
  });

  it('envelope thiếu `data` ném lỗi thay vì dựng ra một dashboard rỗng trông như thật', () => {
    let failed = false;
    service.getAggregate({ Mode: 'week' }).subscribe({ error: () => (failed = true) });

    httpMock.expectOne((r) => r.url === '/dashboard').flush(ok<IDashboardAggregateDto | null>(null));

    expect(failed).toBeTrue();
  });
});

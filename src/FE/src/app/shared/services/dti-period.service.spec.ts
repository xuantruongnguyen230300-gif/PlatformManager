import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { IApiResult } from '../../core/http/api-result.model';
import { IPeriodOptions, IPeriodOptionsDto } from '../models/dti-period.model';
import { DtiPeriodService } from './dti-period.service';

function ok<T>(data: T): IApiResult<T> {
  return {
    data,
    message: null,
    status: 'SUCCESS',
    code: 'Success',
    businessCode: null,
    traceId: 'trace-dti-period',
    retryable: null,
    fields: null,
  };
}

const DTO: IPeriodOptionsDto = {
  years: [2026, 2025],
  weeksInYear: [{ value: '2026-W33', date: '2026-08-10', overallProgress: 82.1 }],
  monthsInYear: [{ value: '2026-08', date: '2026-08-01' }],
};

describe('DtiPeriodService (CONTRACT DB-3)', () => {
  let service: DtiPeriodService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection(), provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(DtiPeriodService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('gọi đường dẫn NGẮN /dashboard/periods — tiền tố /api do interceptor ghép', () => {
    let result: IPeriodOptions | undefined;
    service.getPeriodOptions().subscribe((value) => (result = value));

    // `expectOne` khớp đường dẫn nguyên văn: thêm '/api' vào service sẽ bay tới `/api/api/...`,
    // một 404 trông y hệt "BE chưa làm endpoint" (fe-api-client.md §Service pattern).
    const req = httpMock.expectOne('/dashboard/periods');
    expect(req.request.method).toBe('GET');
    expect(req.request.params.has('year')).withContext('bỏ trống year = năm hiện tại của BE').toBeFalse();
    req.flush(ok(DTO));

    expect(result?.Years).toEqual([2026, 2025]);
    expect(result?.WeeksInYear[0].Ordinal).toBe(33);
    expect(result?.MonthsInYear[0].OverallProgress)
      .withContext('overallProgress vắng mặt phải thành null, không phải 0')
      .toBeNull();
  });

  it('year truyền vào thì đi thành query param', () => {
    service.getPeriodOptions(2025).subscribe();

    const req = httpMock.expectOne((r) => r.url === '/dashboard/periods');
    expect(req.request.params.get('year')).toBe('2025');
    req.flush(ok(DTO));
  });

  it('năm chưa có dữ liệu trả 200 + hai mảng rỗng — KHÔNG được coi là lỗi', () => {
    let result: IPeriodOptions | undefined;
    let failed = false;
    service.getPeriodOptions(2030).subscribe({
      next: (value) => (result = value),
      error: () => (failed = true),
    });

    httpMock.expectOne((r) => r.url === '/dashboard/periods').flush(ok({ years: [2026], weeksInYear: [], monthsInYear: [] }));

    expect(failed).toBeFalse();
    expect(result?.WeeksInYear).toEqual([]);
  });

  it('envelope thiếu `data` ném lỗi rõ ràng thay vì trả một danh sách rỗng bịa ra', () => {
    let failed = false;
    service.getPeriodOptions().subscribe({ error: () => (failed = true) });

    httpMock.expectOne('/dashboard/periods').flush({ ...ok<IPeriodOptionsDto | null>(null) });

    expect(failed)
      .withContext(
        '`unwrapData` phải ném. Đọc `data` bằng toán tử hợp nhất-null kèm giá trị mặc định là dạng ' +
          'bị cấm ở fe-api-client.md §Service pattern — và tên nó cố ý KHÔNG gõ nguyên văn ở đây, ' +
          'vì lệnh grep của cổng quét cả chú thích.',
      )
      .toBeTrue();
  });
});

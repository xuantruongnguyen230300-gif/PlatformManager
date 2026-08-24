import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { PeriodOptionsService } from './period-options.service';
import { IApiResult } from '../../core/http/api-result.model';
import { IPeriodOptions, IPeriodOptionsDto } from '../models/period-options.model';

function ok<T>(data: T): IApiResult<T> {
  return {
    data,
    message: null,
    status: 'SUCCESS',
    code: 'Success',
    businessCode: null,
    traceId: 'trace-periods',
    retryable: null,
    fields: null,
  };
}

/** Payload đúng hình dạng wire (camelCase) — spec stub tầng HTTP thì phải nói bằng DTO. */
const DTO: IPeriodOptionsDto = {
  years: [2026, 2025],
  weeksInYear: [
    { value: '2026-W33', date: '2026-08-17', overallProgress: 62.5 },
    { value: '2026-W32', date: '2026-08-10', overallProgress: null },
  ],
  monthsInYear: [{ value: '2026-08', date: '2026-08-01', overallProgress: 41 }],
};

/**
 * Trọng tâm là RANH GIỚI WIRE, không phải "service có gọi được HTTP không". TypeScript bị xoá
 * lúc chạy: đổi tên field DTO mà quên sửa mapper thì build vẫn xanh còn runtime vỡ im lặng
 * (`undefined` chảy xuống template). Test này là chốt chặn duy nhất cho việc đó — nó khẳng
 * định camelCase phía server được đổi sang PascalCase phía model, KHÔNG phải chỉ so sánh 2 vật
 * thể "trông giống nhau".
 */
describe('PeriodOptionsService', () => {
  let service: PeriodOptionsService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection(), provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(PeriodOptionsService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('map camelCase (wire) → PascalCase (model app), giữ nguyên thứ tự và null', () => {
    let result: IPeriodOptions | undefined;
    service.getPeriodOptions().subscribe((r) => (result = r));

    httpMock.expectOne((req) => req.url === '/dashboard/periods').flush(ok(DTO));

    expect(result).toEqual({
      Years: [2026, 2025],
      WeeksInYear: [
        { Value: '2026-W33', Date: '2026-08-17', OverallProgress: 62.5 },
        { Value: '2026-W32', Date: '2026-08-10', OverallProgress: null },
      ],
      MonthsInYear: [{ Value: '2026-08', Date: '2026-08-01', OverallProgress: 41 }],
    });
  });

  it('không gửi tham số `year` khi caller không truyền', () => {
    service.getPeriodOptions().subscribe();

    const req = httpMock.expectOne((r) => r.url === '/dashboard/periods');
    expect(req.request.params.has('year')).toBeFalse();
    req.flush(ok(DTO));
  });

  it('gửi `year` khi caller truyền — kể cả giá trị 0 (falsy nhưng hợp lệ)', () => {
    // Kiểm tra tường minh nhánh `year != null` chứ không phải `if (year)`: dùng truthy check thì
    // 0 sẽ bị nuốt im lặng. Giá trị 0 vô nghĩa về nghiệp vụ nhưng đúng để khoá hành vi kỹ thuật.
    service.getPeriodOptions(0).subscribe();
    const zero = httpMock.expectOne((r) => r.url === '/dashboard/periods');
    expect(zero.request.params.get('year')).toBe('0');
    zero.flush(ok(DTO));

    service.getPeriodOptions(2025).subscribe();
    const year = httpMock.expectOne((r) => r.url === '/dashboard/periods');
    expect(year.request.params.get('year')).toBe('2025');
    year.flush(ok(DTO));
  });

  it('mảng rỗng vẫn ra mảng rỗng, không phải undefined', () => {
    let result: IPeriodOptions | undefined;
    service.getPeriodOptions(2030).subscribe((r) => (result = r));

    httpMock
      .expectOne((r) => r.url === '/dashboard/periods')
      .flush(ok<IPeriodOptionsDto>({ years: [], weeksInYear: [], monthsInYear: [] }));

    expect(result).toEqual({ Years: [], WeeksInYear: [], MonthsInYear: [] });
  });
});

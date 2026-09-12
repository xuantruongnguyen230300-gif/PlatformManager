import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { IApiResult } from '../../../core/http/api-result.model';
import { IOwnerPage } from '../models/danh-muc-dti.model';
import { CriteriaOwnerService } from './criteria-owner.service';

function ok<T>(data: T): IApiResult<T> {
  return {
    data,
    message: null,
    status: 'SUCCESS',
    code: 'Success',
    businessCode: null,
    traceId: 'trace-owner',
    retryable: null,
    fields: null,
  };
}

describe('CriteriaOwnerService (ô `Phụ trách` — TÁI DÙNG GET /api/users, Q12a)', () => {
  let service: CriteriaOwnerService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection(), provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(CriteriaOwnerService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('🛑 gọi `/users` — KHÔNG có endpoint riêng cho màn này', () => {
    service.search('').subscribe();
    const req = httpMock.expectOne((r) => r.url === '/users');
    expect(req.request.method).toBe('GET');
    req.flush(ok({ items: [], totalCount: 0 }));
  });

  it('`pageSize` gửi tường minh và NẰM DƯỚI trần 200 mà BE cưỡng chế', () => {
    service.search('').subscribe();
    const req = httpMock.expectOne((r) => r.url === '/users');
    const pageSize = Number(req.request.params.get('pageSize'));
    expect(pageSize).toBeGreaterThan(0);
    expect(pageSize).withContext('vượt trần là 400 ValidationError, không phải âm thầm cắt').toBeLessThanOrEqual(200);
    req.flush(ok({ items: [], totalCount: 0 }));
  });

  it('từ khoá rỗng ⇒ KHÔNG gửi `searchText` (rỗng không phải một bộ lọc)', () => {
    service.search('').subscribe();
    const req = httpMock.expectOne((r) => r.url === '/users');
    expect(req.request.params.has('searchText')).toBeFalse();
    req.flush(ok({ items: [], totalCount: 0 }));
  });

  it('có từ khoá ⇒ gửi `searchText`', () => {
    service.search('nguyen').subscribe();
    const req = httpMock.expectOne((r) => r.url === '/users');
    expect(req.request.params.get('searchText')).toBe('nguyen');
    req.flush(ok({ items: [], totalCount: 0 }));
  });

  it('nhãn option là HỌ TÊN — cột `Phụ trách` của lưới cũng đọc `ownerName`', () => {
    let page: IOwnerPage | undefined;
    service.search('').subscribe((value) => (page = value));
    httpMock.expectOne((r) => r.url === '/users').flush(
      ok({ items: [{ id: 'u1', userName: 'nguyen.van.a', fullName: 'Nguyễn Văn A' }], totalCount: 1 }),
    );

    expect(page?.Items[0].Name).toBe('Nguyễn Văn A');
  });

  it('🛑 `fullName` VẮNG MẶT ⇒ lùi về `userName`, KHÔNG để option rỗng', () => {
    let page: IOwnerPage | undefined;
    service.search('').subscribe((value) => (page = value));
    // `WhenWritingNull` ở BE: trường `null` không ra dây, nó biến mất khỏi JSON.
    httpMock
      .expectOne((r) => r.url === '/users')
      .flush(ok({ items: [{ id: 'u1', userName: 'nguyen.van.a' }], totalCount: 1 }));

    // Một dòng trắng trong `<select>` vẫn CHỌN ĐƯỢC, và người chọn không biết mình gán việc cho ai.
    expect(page?.Items[0].Name).toBe('nguyen.van.a');
  });

  it('`totalCount` giữ nguyên — nó nuôi câu "danh sách đã bị cắt, hãy gõ để tìm"', () => {
    let page: IOwnerPage | undefined;
    service.search('').subscribe((value) => (page = value));
    httpMock.expectOne((r) => r.url === '/users').flush(ok({ items: [{ id: 'u1', fullName: 'A' }], totalCount: 350 }));

    expect(page?.TotalCount).toBe(350);
    expect(page?.Items.length).toBe(1);
  });
});

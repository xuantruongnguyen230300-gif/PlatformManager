import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { CsrfService } from './csrf.service';

/**
 * `CsrfService.primeToken()` chỉ có 1 vai trò: bắn `GET /antiforgery/token` để server set cookie
 * `XSRF-TOKEN` — xem doc/contracts/auth.md §"CSRF — GET /api/antiforgery/token". Test quan trọng
 * nhất là "lỗi ở bước mồi không được throw ra ngoài", vì hàm này chạy trong `provideAppInitializer`
 * lúc app khởi động — throw ở đây sẽ chặn cả app không bootstrap được.
 */
describe('CsrfService', () => {
  let service: CsrfService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection(), provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(CsrfService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('gọi GET /antiforgery/token và hoàn tất bình thường khi thành công', (done) => {
    service.primeToken().subscribe({
      next: (value) => expect(value).toBeUndefined(),
      complete: () => done(),
    });

    httpMock.expectOne('/antiforgery/token').flush({ token: 'CfDJ8-fake-token' });
  });

  it('KHÔNG throw ra ngoài khi request lỗi — tự nuốt lỗi, hoàn tất bình thường', (done) => {
    service.primeToken().subscribe({
      next: (value) => expect(value).toBeUndefined(),
      error: () => fail('primeToken() không được reject ra ngoài'),
      complete: () => done(),
    });

    httpMock.expectOne('/antiforgery/token').flush(null, { status: 500, statusText: 'Server Error' });
  });
});

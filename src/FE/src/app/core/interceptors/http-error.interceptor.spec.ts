import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { httpErrorInterceptor } from './http-error.interceptor';
import { ToastService } from '../../shared/services/toast.service';
import { IApiResult, IHttpErrorWithApiResult } from '../http/api-result.model';

// Kiểm chứng F0 (doc/huong_dan/wiki-core/fe/trien-khai/01-f0-dong-bo-envelope.md): test này BAN
// ĐẦU được viết để assert theo field envelope CŨ (`body.Message`/`body.Success` — PascalCase) và
// chạy ĐỎ với `httpErrorInterceptor` hiện tại (đọc đúng `message` camelCase mới) — chứng minh
// interceptor không còn đọc nhầm field cũ. Sau khi xác nhận đỏ, sửa lại assertion theo envelope
// MỚI (`message` camelCase) như bên dưới để xanh — đây là bản đã sửa, giữ lại 1 test riêng
// (`không đọc field PascalCase cũ`) làm bằng chứng thường trực cho hành vi đã chốt.
describe('httpErrorInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let toast: ToastService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(withInterceptors([httpErrorInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
    toast = TestBed.inject(ToastService);
  });

  afterEach(() => httpMock.verify());

  it('đọc đúng `message` (envelope mới, camelCase) để hiển thị toast — KHÔNG phải `Message`/`ErrorMessage` cũ', (done) => {
    const errorSpy = spyOn(toast, 'error');
    const body: IApiResult<null> = {
      data: null,
      message: 'Mã chỉ tiêu đã tồn tại.',
      status: 'BUSINESS_ERROR',
      code: 'Conflict',
      businessCode: 'CRITERIA.DUPLICATE_CODE',
      traceId: 'trace-1',
      retryable: false,
      fields: null,
    };

    http.post('/criteria', {}).subscribe({
      error: () => {
        expect(errorSpy).toHaveBeenCalledWith('Mã chỉ tiêu đã tồn tại.');
        done();
      },
    });

    const req = httpMock.expectOne('/criteria');
    req.flush(body, { status: 409, statusText: 'Conflict' });
  });

  it('KHÔNG đọc field PascalCase cũ (`Message`/`Success`/`ErrorMessage`) — envelope đó đã bỏ', (done) => {
    const errorSpy = spyOn(toast, 'error');
    // Giả response theo shape ApiResponse<T> CŨ (Success/Data/ErrorCode/ErrorMessage) — không có
    // field `message` camelCase nào cả. Nếu interceptor lỡ code lại theo field cũ, test này sẽ
    // đỏ vì toast nhận fallback message thay vì `undefined`/rỗng.
    const legacyBody = {
      Success: false,
      Data: null,
      ErrorCode: 'CONFLICT',
      ErrorMessage: 'Mã chỉ tiêu đã tồn tại.',
      TraceId: 'trace-1',
    };

    http.post('/criteria', {}).subscribe({
      error: (err: IHttpErrorWithApiResult) => {
        // Không có `message` (camelCase) trong body → phải rơi về fallback theo status, KHÔNG
        // phải chuỗi từ field `ErrorMessage` cũ.
        expect(errorSpy).not.toHaveBeenCalledWith('Mã chỉ tiêu đã tồn tại.');
        expect(errorSpy).toHaveBeenCalledWith('Đã có lỗi xảy ra. Vui lòng thử lại.');
        expect(err.apiResult).toBeTruthy();
        done();
      },
    });

    const req = httpMock.expectOne('/criteria');
    req.flush(legacyBody, { status: 409, statusText: 'Conflict' });
  });

  it('gắn `apiResult` vào error rethrow để form đọc `fields`', (done) => {
    spyOn(toast, 'error');
    const body: IApiResult<null> = {
      data: null,
      message: 'Dữ liệu không hợp lệ.',
      status: 'VALIDATION_ERROR',
      code: 'ValidationError',
      businessCode: null,
      traceId: 'trace-2',
      retryable: null,
      fields: { MaxScore: ['Điểm tối đa phải lớn hơn 0.'] },
    };

    http.post('/criteria', {}).subscribe({
      error: (err: IHttpErrorWithApiResult) => {
        expect(err.apiResult?.fields?.['MaxScore']).toEqual(['Điểm tối đa phải lớn hơn 0.']);
        done();
      },
    });

    const req = httpMock.expectOne('/criteria');
    req.flush(body, { status: 400, statusText: 'Bad Request' });
  });
});

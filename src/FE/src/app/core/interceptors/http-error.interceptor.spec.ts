import {
  HttpClient,
  HttpContext,
  HttpErrorResponse,
  provideHttpClient,
  withInterceptors,
} from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { Router } from '@angular/router';
import { httpErrorInterceptor } from './http-error.interceptor';
import { ToastService } from '../toast/toast.service';
import { CurrentUserService } from '../auth/current-user.service';
import { SKIP_ERROR_TOAST } from '../http/http-context-tokens';
import { IApiResult, IHttpErrorWithApiResult } from '../http/api-result.model';
import { IToastAction, TOAST_AUTO_DISMISS_MS } from '../toast/toast.service';
import { provideTranslateService } from '@ngx-translate/core';
import { ICoreRoutes, provideCoreRoutes } from '../config/core-routes';
import { useTranslationsInTest } from '../i18n/i18n.testing';

/**
 * Bản sao ba đường dẫn `app.config.ts` (`APP_CORE_ROUTES`) bơm vào — chép chứ không import, vì
 * `core/` là tầng đáy và không được biết app cụ thể nào đang dùng nó (cùng lý do như
 * `core/auth/guards.spec.ts`). Nhờ vậy mọi assert cũ dưới đây giữ nguyên chuỗi `/dang-nhap`, tức
 * chúng vẫn đo đúng "hành vi KHÔNG ĐỔI" sau đợt tách đường dẫn 2026-09-02.
 */
const APP_ROUTES_TODAY: ICoreRoutes = {
  signIn: '/dang-nhap',
  changePassword: '/doi-mat-khau',
  home: '/trang-chu',
};

/**
 * Router giả — CỐ Ý không đổi `url` bên trong `navigate()`, giống router thật (điều hướng là bất
 * đồng bộ, `router.url` chưa kịp đổi trong cùng tick). Nhờ vậy test "nhiều 401 song song" thật sự
 * kiểm cờ chống lặp, chứ không vô tình được cứu bởi phép so sánh "đang ở màn đăng nhập".
 */
interface IRouterStub {
  url: string;
  navigate: jasmine.Spy;
}

// Kiểm chứng F0 (doc/huong_dan/wiki-core/fe/trien-khai/01-f0-nen-mong.md): test này BAN
// ĐẦU được viết để assert theo field envelope CŨ (`body.Message`/`body.Success` — PascalCase) và
// chạy ĐỎ với `httpErrorInterceptor` hiện tại (đọc đúng `message` camelCase mới) — chứng minh
// interceptor không còn đọc nhầm field cũ. Sau khi xác nhận đỏ, sửa lại assertion theo envelope
// MỚI (`message` camelCase) như bên dưới để xanh — đây là bản đã sửa, giữ lại 1 test riêng
// (`không đọc field PascalCase cũ`) làm bằng chứng thường trực cho hành vi đã chốt.
describe('httpErrorInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let toast: ToastService;
  let router: IRouterStub;
  let currentUser: CurrentUserService;

  beforeEach(async () => {
    router = {
      url: '/quan-tri/nguoi-dung',
      navigate: jasmine.createSpy('navigate').and.returnValue(Promise.resolve(true)),
    };

    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(withInterceptors([httpErrorInterceptor])),
        provideHttpClientTesting(),
        { provide: Router, useValue: router },
        provideCoreRoutes(APP_ROUTES_TODAY),
        // Interceptor nay dựng câu toast từ BẢNG DỊCH (`ApiErrorMessageService`), không còn hằng
        // số tiếng Việt trong file .ts. Nạp bảng dịch THẬT ở đây nên mọi assert bên dưới giữ
        // NGUYÊN chuỗi cũ — nghĩa là chúng vẫn đo đúng "hành vi không đổi" sau đợt bọc i18n
        // 2026-09-05, đồng thời trở thành phép kiểm phụ rằng khoá tương ứng CÓ THẬT trong
        // `public/i18n/vi.json` (thiếu khoá thì chuỗi render ra chính khoá và test ĐỎ).
        provideTranslateService(),
      ],
    });
    await useTranslationsInTest('vi');
    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
    toast = TestBed.inject(ToastService);
    currentUser = TestBed.inject(CurrentUserService);
  });

  // Cờ chống điều hướng lặp sống ở tầng module (interceptor là hàm, không có instance) và chỉ mở
  // lại khi promise của `navigate()` settle. Nhường 1 macrotask ở đây để nó kịp mở, nếu không
  // test SAU sẽ thấy cờ còn bật và tưởng interceptor không điều hướng.
  afterEach(async () => {
    await new Promise((resolve) => setTimeout(resolve, 0));
    httpMock.verify();
  });

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
        // Nhánh fallback đi kèm TIÊU ĐỀ (tham số thứ hai) — toast thiết kế mới đọc theo hai tầng
        // "chuyện gì xảy ra" rồi mới "chi tiết". Nhánh có `message` thật từ envelope thì KHÔNG có
        // tiêu đề (xem test đầu tiên: gọi đúng 1 tham số).
        expect(errorSpy).toHaveBeenCalledWith('Đã có lỗi xảy ra. Vui lòng thử lại.', 'Lỗi hệ thống');
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

  // ═══ 401 — phiên chết giữa chừng (doc/contracts/auth.md §"Vòng đời phiên") ══════════════════
  // Cookie sống 14 ngày + SlidingExpiration ⇒ phiên gần như không tự hết hạn; cơ chế chấm dứt
  // duy nhất ngoài logout là `SecurityStampValidator` (chu kỳ 30 phút). Nghĩa là 401 rơi vào
  // GIỮA lúc user đang gõ form, nơi không guard nào chạy — nếu interceptor không điều hướng thì
  // user kẹt lại trên form không bao giờ gửi được. Mỗi test dưới đây khoá một cạm bẫy riêng.
  describe('401 — phiên hết hạn', () => {
    const unauthorizedBody: IApiResult<null> = {
      data: null,
      message: 'Bạn chưa đăng nhập.',
      status: 'BUSINESS_ERROR',
      code: 'AuthenticationError',
      businessCode: null,
      traceId: 'trace-401',
      retryable: false,
      fields: null,
    };

    it('đường chính: xoá state phía client, điều hướng /dang-nhap kèm returnUrl, VẪN rethrow', (done) => {
      const clearSpy = spyOn(currentUser, 'clear');
      const warnSpy = spyOn(toast, 'warn');
      const errorSpy = spyOn(toast, 'error');

      http.get('/criteria').subscribe({
        error: (err: HttpErrorResponse & IHttpErrorWithApiResult) => {
          expect(clearSpy).toHaveBeenCalledTimes(1);
          expect(router.navigate).toHaveBeenCalledOnceWith(['/dang-nhap'], {
            queryParams: { returnUrl: '/quan-tri/nguoi-dung' },
          });
          // Yêu cầu 3: rethrow nguyên vẹn — nơi gọi cần biết request đã hỏng để tắt spinner.
          expect(err.status).toBe(401);
          expect(err.apiResult?.traceId).toBe('trace-401');
          // Cạm bẫy 4: KHÔNG toast đỏ. Hợp đồng nói 401 giữa phiên "không phải lỗi hệ thống";
          // câu của BE ("Bạn chưa đăng nhập.") cũng bị thay bằng lời giải thích dễ hiểu hơn.
          expect(errorSpy).not.toHaveBeenCalled();
          expect(warnSpy).toHaveBeenCalledOnceWith(
            'Vui lòng đăng nhập lại để tiếp tục.',
            'Phiên đăng nhập đã kết thúc',
          );
          done();
        },
      });

      httpMock.expectOne('/criteria').flush(unauthorizedBody, { status: 401, statusText: 'Unauthorized' });
    });

    it('CẠM BẪY 1 — request probe (SKIP_ERROR_TOAST): 401 là bình thường, KHÔNG điều hướng, không toast', (done) => {
      const clearSpy = spyOn(currentUser, 'clear');
      const warnSpy = spyOn(toast, 'warn');
      const errorSpy = spyOn(toast, 'error');

      // Đúng hình dạng `CurrentUserService.load()` gọi lúc khởi động app.
      http.get('/auth/me', { context: new HttpContext().set(SKIP_ERROR_TOAST, true) }).subscribe({
        error: () => {
          // Điều hướng ở đây sẽ đá khách vãng lai ra khỏi trang họ vừa mở.
          expect(router.navigate).not.toHaveBeenCalled();
          expect(clearSpy).not.toHaveBeenCalled();
          expect(warnSpy).not.toHaveBeenCalled();
          expect(errorSpy).not.toHaveBeenCalled();
          done();
        },
      });

      httpMock.expectOne('/auth/me').flush(unauthorizedBody, { status: 401, statusText: 'Unauthorized' });
    });

    it('CẠM BẪY 2 — đang ở /dang-nhap (sai mật khẩu): KHÔNG điều hướng lại, giữ toast lỗi của form', (done) => {
      // `returnUrl` người dùng đang giữ nằm trong URL hiện tại — điều hướng lại sẽ ghi đè nó.
      router.url = '/dang-nhap?returnUrl=%2Fquan-tri%2Fnguoi-dung';
      const clearSpy = spyOn(currentUser, 'clear');
      const errorSpy = spyOn(toast, 'error');
      const wrongPassword: IApiResult<null> = {
        ...unauthorizedBody,
        message: 'Tên đăng nhập hoặc mật khẩu không đúng.',
      };

      http.post('/auth/login', {}).subscribe({
        error: () => {
          expect(router.navigate).not.toHaveBeenCalled();
          expect(clearSpy).not.toHaveBeenCalled();
          expect(errorSpy).toHaveBeenCalledWith('Tên đăng nhập hoặc mật khẩu không đúng.');
          done();
        },
      });

      httpMock.expectOne('/auth/login').flush(wrongPassword, { status: 401, statusText: 'Unauthorized' });
    });

    it('CẠM BẪY 3 — 3 request song song cùng nhận 401: chỉ điều hướng và báo MỘT lần', (done) => {
      const clearSpy = spyOn(currentUser, 'clear');
      const warnSpy = spyOn(toast, 'warn');
      let errorCount = 0;

      const onError = (): void => {
        errorCount += 1;
        if (errorCount < 3) {
          return;
        }
        // Cả 3 vẫn phải rethrow (errorCount === 3), nhưng chỉ 1 lần điều hướng/toast/clear.
        expect(router.navigate).toHaveBeenCalledTimes(1);
        expect(clearSpy).toHaveBeenCalledTimes(1);
        expect(warnSpy).toHaveBeenCalledTimes(1);
        done();
      };

      http.get('/criteria').subscribe({ error: onError });
      http.get('/criteria').subscribe({ error: onError });
      http.get('/criteria').subscribe({ error: onError });

      const reqs = httpMock.match('/criteria');
      expect(reqs.length).toBe(3);
      reqs.forEach((r) => r.flush(unauthorizedBody, { status: 401, statusText: 'Unauthorized' }));
    });

    it('status khác 401 (403) giữ nguyên đường cũ: toast lỗi, KHÔNG điều hướng, KHÔNG xoá phiên', (done) => {
      const clearSpy = spyOn(currentUser, 'clear');
      const errorSpy = spyOn(toast, 'error');

      http.get('/criteria').subscribe({
        error: () => {
          expect(router.navigate).not.toHaveBeenCalled();
          expect(clearSpy).not.toHaveBeenCalled();
          expect(errorSpy).toHaveBeenCalledWith('Bạn không có quyền thực hiện thao tác này.', 'Không đủ quyền');
          done();
        },
      });

      httpMock.expectOne('/criteria').flush(null, { status: 403, statusText: 'Forbidden' });
    });
  });
});

/**
 * Song song với `core/auth/guards.spec.ts` §"đường dẫn đến từ CORE_ROUTES": interceptor cũng từng
 * khai cứng `const LOGIN_PATH = '/dang-nhap'` (gỡ 2026-09-02). Hai hành vi phụ thuộc đường dẫn —
 * ĐI ĐÂU khi phiên chết, và NHẬN RA đang ở màn đăng nhập (CẠM BẪY 2) — đều phải chạy theo token.
 *
 * Đường dẫn ở đây khác hẳn dự án này, nên một dòng khai cứng sót lại sẽ ĐỎ tại đây và chỉ tại đây.
 */
describe('httpErrorInterceptor — đường dẫn màn đăng nhập đến từ CORE_ROUTES', () => {
  const OTHER_PRODUCT_ROUTES: ICoreRoutes = {
    signIn: '/sign-in',
    changePassword: '/change-password',
    home: '/home',
  };

  const unauthorizedBody: IApiResult<null> = {
    data: null,
    message: 'Bạn chưa đăng nhập.',
    status: 'BUSINESS_ERROR',
    code: 'AuthenticationError',
    businessCode: null,
    traceId: 'trace-401',
    retryable: false,
    fields: null,
  };

  let http: HttpClient;
  let httpMock: HttpTestingController;
  let router: IRouterStub;

  beforeEach(async () => {
    router = {
      url: '/dashboard',
      navigate: jasmine.createSpy('navigate').and.returnValue(Promise.resolve(true)),
    };

    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(withInterceptors([httpErrorInterceptor])),
        provideHttpClientTesting(),
        { provide: Router, useValue: router },
        provideCoreRoutes(OTHER_PRODUCT_ROUTES),
        provideTranslateService(),
      ],
    });
    await useTranslationsInTest('vi');
    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
  });

  // Cùng lý do như describe đầu file: nhường 1 macrotask để cờ chống điều hướng lặp (tầng module)
  // kịp mở lại, nếu không test sau thấy cờ còn bật và tưởng interceptor không điều hướng.
  afterEach(async () => {
    await new Promise((resolve) => setTimeout(resolve, 0));
    httpMock.verify();
  });

  it('401 giữa phiên → điều hướng về CORE_ROUTES.signIn, không phải đường dẫn của dự án này', (done) => {
    http.get('/criteria').subscribe({
      error: () => {
        expect(router.navigate).toHaveBeenCalledOnceWith(['/sign-in'], {
          queryParams: { returnUrl: '/dashboard' },
        });
        done();
      },
    });

    httpMock.expectOne('/criteria').flush(unauthorizedBody, { status: 401, statusText: 'Unauthorized' });
  });

  it('CẠM BẪY 2 nhận ra màn đăng nhập theo CORE_ROUTES.signIn — đang ở /sign-in thì KHÔNG điều hướng lại', (done) => {
    router.url = '/sign-in?returnUrl=%2Fdashboard';

    http.post('/auth/login', {}).subscribe({
      error: () => {
        expect(router.navigate).not.toHaveBeenCalled();
        done();
      },
    });

    httpMock.expectOne('/auth/login').flush(unauthorizedBody, { status: 401, statusText: 'Unauthorized' });
  });
});


/**
 * ═══ `status === 0` — KHÔNG kết nối được tới máy chủ ════════════════════════════════════════════
 *
 * Trước 2026-09-10 ca này rơi vào đúng nhánh của một lỗi server thường: người rút dây mạng nhận
 * một toast không nói được chuyện gì vừa xảy ra và không có việc gì để làm tiếp. Bốn hành vi được
 * khoá ở đây, và chúng khoá lẫn nhau — bỏ bất kỳ cái nào thì ba cái còn lại vẫn có thể xanh trong
 * khi tính năng đã hỏng:
 *
 *  1. `status 0` đi vào nhánh mất kết nối (toast riêng, CÓ nút hành động);
 *  2. lỗi HTTP thường KHÔNG đi vào nhánh đó (toast cũ, KHÔNG nút) — đây là ca đối chứng, thiếu nó
 *     thì một nhánh bắt-tất-cả cũng làm test 1 xanh;
 *  3. bấm nút gọi lại ĐÚNG request vừa hỏng, và kết quả về ĐÚNG nơi đã đặt hàng;
 *  4. không bấm thì lỗi vẫn phải nổ ra sau cửa sổ thử lại — nếu không, mọi request hỏng lúc mất
 *     mạng sẽ treo vĩnh viễn và màn hình quay spinner mãi mãi.
 *
 * Đo thời gian bằng `jasmine.clock()` chứ không `fakeAsync()`: dự án đã bỏ zone.js (zoneless) —
 * cùng lý do đã ghi ở `core/toast/toast.service.spec.ts`.
 */
describe('httpErrorInterceptor — mất kết nối (status 0)', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let toast: ToastService;
  let clockInstalled = false;

  /** Lỗi mạng thật của trình duyệt: `ProgressEvent('error')` + `status: 0`. */
  function failWithNetworkError(url: string): void {
    httpMock.expectOne(url).error(new ProgressEvent('error'), { status: 0, statusText: 'Unknown Error' });
  }

  function useClock(): void {
    jasmine.clock().install();
    clockInstalled = true;
  }

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(withInterceptors([httpErrorInterceptor])),
        provideHttpClientTesting(),
        {
          provide: Router,
          useValue: { url: '/trang-chu', navigate: jasmine.createSpy('navigate') } as IRouterStub,
        },
        provideCoreRoutes(APP_ROUTES_TODAY),
        provideTranslateService(),
      ],
    });
    // Bảng dịch THẬT — nên mọi assert chuỗi bên dưới đồng thời là phép kiểm rằng khoá có mặt trong
    // `public/i18n/vi.json`. Thiếu khoá thì ngx-translate trả về chính chuỗi khoá và test ĐỎ.
    await useTranslationsInTest('vi');
    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
    toast = TestBed.inject(ToastService);
  });

  // Gỡ clock TRƯỚC khi `await` — còn clock thì `setTimeout(0)` dưới đây không bao giờ chạy và cả
  // spec treo.
  afterEach(async () => {
    if (clockInstalled) {
      jasmine.clock().uninstall();
      clockInstalled = false;
    }
    await new Promise((resolve) => setTimeout(resolve, 0));
    httpMock.verify();
  });

  it('1 · status 0 → toast MẤT KẾT NỐI riêng, kèm nút hành động "Thử lại"', () => {
    const errorSpy = spyOn(toast, 'error');

    const sub = http.get('/criteria').subscribe({ error: () => undefined });
    failWithNetworkError('/criteria');

    const [text, title, action] = errorSpy.calls.mostRecent().args as [string, string, IToastAction];
    expect(text).toBe('Không thể kết nối tới máy chủ. Kiểm tra kết nối mạng.');
    expect(title).toBe('Mất kết nối');
    expect(action.Label).toBe('Thử lại');
    expect(typeof action.Run).toBe('function');

    // Không bấm gì → huỷ đăng ký để cửa sổ thử lại không nổ sau khi spec đã kết thúc.
    sub.unsubscribe();
  });

  it('1b · navigator.onLine === false → câu MẠNH hơn về thiết bị, cùng tiêu đề', () => {
    // Spy trên prototype: `onLine` không phải thuộc tính riêng của `navigator`, nên
    // `spyOnProperty(navigator, ...)` sẽ không tìm thấy descriptor để thay.
    spyOnProperty(Navigator.prototype, 'onLine', 'get').and.returnValue(false);
    const errorSpy = spyOn(toast, 'error');

    const sub = http.get('/criteria').subscribe({ error: () => undefined });
    failWithNetworkError('/criteria');

    const [text, title] = errorSpy.calls.mostRecent().args;
    expect(text).toBe(
      'Thiết bị của bạn đang không có kết nối mạng. Kiểm tra Wi-Fi hoặc dây mạng rồi thử lại.',
    );
    expect(title).toBe('Mất kết nối');

    sub.unsubscribe();
  });

  it('2 · ĐỐI CHỨNG — lỗi HTTP thường (500) KHÔNG rơi vào nhánh này: không nút, lỗi nổ NGAY', () => {
    const errorSpy = spyOn(toast, 'error');
    let errored = false;

    http.get('/criteria').subscribe({ error: () => (errored = true) });
    httpMock.expectOne('/criteria').flush(null, { status: 500, statusText: 'Server Error' });

    // Nổ ngay trong cùng một tick — hành vi CŨ, không được đổi: chỉ ca mất kết nối mới hoãn lỗi.
    expect(errored).toBeTrue();
    expect(errorSpy).toHaveBeenCalledOnceWith('Đã có lỗi xảy ra. Vui lòng thử lại.', 'Lỗi hệ thống');
    // Đúng 2 đối số ⇒ KHÔNG có action. `toHaveBeenCalledWith` bỏ qua đối số thứ ba `undefined`,
    // nên phải đếm tay ở đây, nếu không một action lọt vào mọi toast lỗi vẫn xanh.
    expect(errorSpy.calls.mostRecent().args.length).toBe(2);
  });

  it('3 · bấm "Thử lại" gọi lại ĐÚNG request vừa hỏng, kết quả về ĐÚNG nơi đã đặt hàng', (done) => {
    let action: IToastAction | undefined;
    spyOn(toast, 'error').and.callFake((_text, _title, a) => void (action = a));

    http.post('/criteria', { code: 'DTI-01' }).subscribe({
      next: (value) => {
        // Đây là điều then chốt: response của lần gọi LẠI về tới người đặt hàng BAN ĐẦU. Một cách
        // hiện thực kiểu "tự subscribe lại trong interceptor" sẽ bắn request đi nhưng không bao
        // giờ chạy được nhánh này.
        expect(value).toEqual({ ok: true });
        done();
      },
      error: () => done.fail('không được rơi vào nhánh lỗi sau khi thử lại thành công'),
    });

    failWithNetworkError('/criteria');
    action!.Run();

    const retried = httpMock.expectOne('/criteria');
    expect(retried.request.method).toBe('POST');
    expect(retried.request.body).toEqual({ code: 'DTI-01' });
    retried.flush({ ok: true });
  });

  it('4 · không bấm gì → hết cửa sổ thử lại thì lỗi gốc nổ ra, và KHÔNG toast lần hai', () => {
    const errorSpy = spyOn(toast, 'error');
    let caught: HttpErrorResponse | undefined;

    useClock();
    http.get('/criteria').subscribe({ error: (err: HttpErrorResponse) => (caught = err) });
    failWithNetworkError('/criteria');

    jasmine.clock().tick(TOAST_AUTO_DISMISS_MS - 1);
    expect(caught).toBeUndefined();

    jasmine.clock().tick(1);
    expect(caught?.status).toBe(0);
    // Một sự cố, một toast: `catchError` phía dưới phải bỏ qua `status 0` vì cổng thử lại đã lo.
    // Toast thứ hai không chỉ thừa — nó đẩy toast mang nút bấm ra khỏi tầm mắt.
    expect(errorSpy).toHaveBeenCalledTimes(1);
  });

  it('5 · request probe (SKIP_ERROR_TOAST) mất kết nối → không toast, lỗi nổ NGAY', () => {
    const errorSpy = spyOn(toast, 'error');
    let errored = false;

    // `GET /auth/me` lúc khởi động đi đường này. Hoãn nó lại vài giây là treo màn hình khởi động
    // đúng vào lúc mạng hỏng — lúc app cần trả lời "chưa đăng nhập" nhanh nhất.
    http
      .get('/auth/me', { context: new HttpContext().set(SKIP_ERROR_TOAST, true) })
      .subscribe({ error: () => (errored = true) });
    failWithNetworkError('/auth/me');

    expect(errored).toBeTrue();
    expect(errorSpy).not.toHaveBeenCalled();
  });
});

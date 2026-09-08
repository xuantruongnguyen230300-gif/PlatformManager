import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, TestRequest, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { Router, provideRouter } from '@angular/router';
import { LoginPage } from './login.page';
import { provideCoreBranding } from '../../../../core/config/core-branding';
import { IApiResult } from '../../../../core/http/api-result.model';
import { httpErrorInterceptor } from '../../../../core/interceptors/http-error.interceptor';
import { ICurrentUserDto } from '../../../../core/auth/current-user.model';
import { ICoreRoutes, provideCoreRoutes } from '../../../../core/config/core-routes';
import { provideTranslateService } from '@ngx-translate/core';
import { useTranslationsInTest } from '../../../../core/i18n/i18n.testing';
import { CORE_I18N, ICoreI18n } from '../../../../core/i18n/core-i18n';
import { LanguageService } from '../../../../core/i18n/language.service';

/**
 * Bản sao ba đường dẫn `app.config.ts` (`APP_CORE_ROUTES`) bơm vào — CỐ Ý chép chứ không import
 * từ `app.config.ts`, cùng lý do đã ghi ở `core/auth/guards.spec.ts`: hai describe đầu file đo
 * hành vi hiện tại của màn hình, nên chúng phải giữ nguyên chuỗi cũ để chứng minh đợt tách
 * đường dẫn 2026-09-03 KHÔNG đổi hành vi.
 *
 * Việc chuỗi này khớp bảng route thật là chuyện của app và đã khoá riêng ở `app.routes.spec.ts`.
 */
const APP_ROUTES_TODAY: ICoreRoutes = {
  signIn: '/dang-nhap',
  changePassword: '/doi-mat-khau',
  home: '/trang-chu',
};

/**
 * Hai ngôn ngữ của harness. CỐ Ý không import `APP_I18N` từ `app.config.ts`: `platform/` nằm trong
 * CoreBase (doc/kien-truc-core-module.md) nên màn hình không được biết sản phẩm nào đang chạy nó.
 * Không khai `localeData` vì `LanguageService.init()` — nơi duy nhất gọi `registerLocaleData` —
 * không chạy trong các spec này; `primeTranslation` để rỗng vì màn đăng nhập không có component
 * PrimeNG nào.
 */
const TEST_I18N: ICoreI18n = {
  languages: [
    { code: 'vi', label: 'Tiếng Việt', localeId: 'vi', primeTranslation: {} },
    { code: 'en', label: 'English', localeId: 'en-US', primeTranslation: {} },
  ],
  defaultCode: 'vi',
  resources: ['/i18n/'],
};

/**
 * Chốt bốn quyết định 2026-08-31 của màn đăng nhập
 * (doc/Design/Frontend/PlatformManager/Screens/05-auth.md §Normalize on redesign, mục 4/5/6/10).
 *
 * Ba trong bốn mục là những thứ KHÔNG có test nào bắt được nếu chỉ nhìn build: ô tick không nối
 * vào request vẫn render đẹp, một link chết vẫn bấm được, thiếu `role="alert"` không đổi một pixel
 * nào. Chúng chỉ sai với người dùng, nên phải chốt bằng test.
 */
describe('LoginPage — nhãn, ô tick "Ghi nhớ đăng nhập" và thông báo lỗi', () => {
  let fixture: ComponentFixture<LoginPage>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([{ path: '**', children: [] }]),
        // `AuthCard` (khung của màn này) inject CORE_BRANDING để lấy chữ tắt thương hiệu —
        // token cố ý không có giá trị mặc định, harness phải tự cấp. Xem
        // core/config/core-branding.ts.
        provideCoreBranding({ name: 'Ứng Dụng Thử', shortName: 'UT' }),
        // `LoginPage` inject CORE_ROUTES để biết đi đâu sau khi đăng nhập — token cố ý không có
        // giá trị mặc định nên harness phải tự cấp, nếu không TestBed nổ NullInjectorError.
        provideCoreRoutes(APP_ROUTES_TODAY),
        // Bảng dịch THẬT được nạp ngay sau đây (`useTranslationsInTest`). Không cấp loader: mọi
        // assert dưới đây so với câu tiếng Việt trong `public/i18n/vi.json`, nên gõ sai một khoá
        // trong template sẽ làm chuỗi render ra chính khoá đó và test ĐỎ.
        provideTranslateService(),
        // `LanguageSwitcher` trong template của màn này inject `LanguageService`, mà service đó
        // inject `CORE_I18N` — token cố ý không có giá trị mặc định (core/i18n/core-i18n.ts), nên
        // harness phải tự cấp, nếu không TestBed nổ NG0201.
        { provide: CORE_I18N, useValue: TEST_I18N },
      ],
    });
    httpMock = TestBed.inject(HttpTestingController);
    await useTranslationsInTest('vi');
    await TestBed.inject(Router).navigate([]);
    fixture = TestBed.createComponent(LoginPage);
    fixture.detectChanges();
  });

  afterEach(() => httpMock.verify());

  function host(): HTMLElement {
    return fixture.nativeElement as HTMLElement;
  }

  function rememberCheckbox(): HTMLInputElement {
    return host().querySelector('.field-row .check') as HTMLInputElement;
  }

  /** Điền form rồi bấm Đăng nhập — trả về body của request đã rời khỏi client. */
  function submit(): Record<string, unknown> {
    (host().querySelector('#userName') as HTMLInputElement).value = 'nguyen.van.a';
    host().querySelector('#userName')?.dispatchEvent(new Event('input'));
    (host().querySelector('#password') as HTMLInputElement).value = 'MatKhau@123';
    host().querySelector('#password')?.dispatchEvent(new Event('input'));
    (host().querySelector('form') as HTMLFormElement).dispatchEvent(new Event('submit'));

    const req = httpMock.expectOne('/auth/login');
    const body = req.request.body as Record<string, unknown>;
    // Trả lỗi 401 để dừng luồng ngay tại đây: đăng nhập THÀNH CÔNG còn kéo theo mồi cookie CSRF và
    // một lượt điều hướng, cả hai đều không liên quan tới thứ test này đang đo.
    req.flush(null, { status: 401, statusText: 'Unauthorized' });
    return body;
  }

  it('ô đầu tiên là TÊN ĐĂNG NHẬP — nhãn, gợi ý và icon đều nói đúng thứ nó chứa (mục 6)', () => {
    const label = host().querySelector('label[for="userName"]') as HTMLElement;
    expect(label.textContent?.trim()).toBe('Tên đăng nhập');

    const input = host().querySelector('#userName') as HTMLInputElement;
    // Gợi ý không được là một địa chỉ email: tài khoản thật có dạng `SuperAdmin`/`nguyen.van.a`.
    expect(input.placeholder).not.toContain('@');
    // `type="text"` chứ KHÔNG `email` — native validation sẽ chặn nhầm tên đăng nhập hợp lệ.
    expect(input.type).toBe('text');
    expect(host().querySelector('.field .pi-envelope')).withContext('icon phong bì phải biến mất').toBeNull();
    expect(host().querySelector('.field .pi-user')).not.toBeNull();
  });

  it('payload vẫn gửi `userName` — đổi nhãn KHÔNG được đổi hợp đồng API (mục 6)', () => {
    const body = submit();
    expect(body['userName']).toBe('nguyen.van.a');
    expect(body['email']).toBeUndefined();
  });

  it('"Ghi nhớ đăng nhập" MẶC ĐỊNH không tích, và gửi `rememberMe: false` (mục 4)', () => {
    expect(rememberCheckbox().checked).toBeFalse();
    expect(submit()['rememberMe']).toBeFalse();
  });

  it('tích "Ghi nhớ đăng nhập" → gửi `rememberMe: true` (mục 4)', () => {
    const checkbox = rememberCheckbox();
    checkbox.checked = true;
    checkbox.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    expect(submit()['rememberMe']).toBeTrue();
  });

  it('không còn link chết "Quên mật khẩu?" — chỉ còn chữ chỉ đúng đường có thật (mục 5)', () => {
    const links = Array.from(host().querySelectorAll('a'));
    expect(links.length).withContext('màn đăng nhập không còn liên kết nào').toBe(0);
    expect(host().textContent).toContain('Quên mật khẩu? Liên hệ quản trị viên để được đặt lại.');
  });

  it('khối lỗi mang `role="alert"` — hỏng đăng nhập phải được ĐỌC LÊN, không chỉ đổi màu (mục 10)', () => {
    // Bấm Đăng nhập khi còn để trống → component tự đặt thông điệp, không cần gọi API.
    (host().querySelector('form') as HTMLFormElement).dispatchEvent(new Event('submit'));
    fixture.detectChanges();

    const error = host().querySelector('.login-error') as HTMLElement;
    expect(error).not.toBeNull();
    expect(error.getAttribute('role')).toBe('alert');
  });
});

/**
 * Hai chuỗi thương hiệu trên card đăng nhập — màn hình ĐẦU TIÊN người dùng của sản phẩm thứ hai
 * nhìn thấy, nên cũng là chỗ một chuỗi khai cứng gây thiệt hại lộ liễu nhất.
 *
 * Vì sao phải assert lên chuỗi ĐÃ RENDER chứ không chỉ cấp token như describe trên: cấp token
 * mới chỉ chứng minh component không nổ vì thiếu provider. Trước 2026-09-03, `<h1>` nhận thẳng
 * `title="<tên sản phẩm>"` từ template của `platform/login` — mà `platform/` NẰM TRONG CoreBase
 * (doc/kien-truc-core-module.md) — nên ô vuông `.brand-mark` ngay trên nó đã đổ tên mới từ token
 * còn dòng chữ dưới vẫn là tên cũ. Bộ test khi đó xanh hoàn toàn.
 *
 * Giá trị token CỐ Ý khác tên thật của dự án: assert lên đúng tên thật thì phép kiểm tự soi
 * gương — nó vẫn xanh sau khi ai đó khai cứng chuỗi trở lại.
 */
describe('LoginPage — chữ thương hiệu trên card đến từ CORE_BRANDING', () => {
  const TEST_BRANDING = { name: 'Ứng Dụng Thử', shortName: 'UT' };
  let fixture: ComponentFixture<LoginPage>;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([{ path: '**', children: [] }]),
        provideCoreBranding(TEST_BRANDING),
        provideCoreRoutes(APP_ROUTES_TODAY),
        provideTranslateService(),
        // `LanguageSwitcher` trong template của màn này inject `LanguageService`, mà service đó
        // inject `CORE_I18N` — token cố ý không có giá trị mặc định (core/i18n/core-i18n.ts), nên
        // harness phải tự cấp, nếu không TestBed nổ NG0201.
        { provide: CORE_I18N, useValue: TEST_I18N },
      ],
    });
    await useTranslationsInTest('vi');
    await TestBed.inject(Router).navigate([]);
    fixture = TestBed.createComponent(LoginPage);
    fixture.detectChanges();
  });

  it('🛑 `<h1>` của card là TÊN SẢN PHẨM lấy từ token, không phải chuỗi trong login.page.html', () => {
    const heading = (fixture.nativeElement as HTMLElement).querySelector('.login-brand h1') as HTMLElement;

    expect(heading).withContext('card đăng nhập phải có h1').not.toBeNull();
    expect(heading.textContent?.trim())
      .withContext('khai cứng tên ở đây = sản phẩm thứ hai hiện tên sản phẩm này ngay màn đăng nhập')
      .toBe(TEST_BRANDING.name);
  });

  it('🛑 ô vuông `.brand-mark` là chữ tắt ĐÃ RENDER từ token', () => {
    const mark = (fixture.nativeElement as HTMLElement).querySelector('.login-brand .brand-mark') as HTMLElement;

    expect(mark).withContext('card đăng nhập phải có ô vuông thương hiệu').not.toBeNull();
    expect(mark.textContent?.trim()).toBe(TEST_BRANDING.shortName);
  });
});

/**
 * Song song với `core/interceptors/http-error.interceptor.spec.ts` §"đường dẫn màn đăng nhập đến
 * từ CORE_ROUTES": `LoginPage` từng khai cứng `const DEFAULT_REDIRECT = '/trang-chu'` và
 * `navigateByUrl('/doi-mat-khau')` (gỡ 2026-09-03). `platform/` cũng nằm trong CoreBase, nên hai
 * đích đó phải chạy theo token y như bên `core/`.
 *
 * Ba đường dẫn dưới đây khác hẳn dự án này, nên một dòng khai cứng sót lại sẽ ĐỎ tại đây và CHỈ
 * tại đây: `app.routes.spec.ts` chỉ kiểm app khai đúng ba đường dẫn có thật, còn build và lint thì
 * chuỗi tiếng Việt nào cũng hợp lệ.
 */
describe('LoginPage — đích sau đăng nhập đến từ CORE_ROUTES', () => {
  const OTHER_PRODUCT_ROUTES: ICoreRoutes = {
    signIn: '/sign-in',
    changePassword: '/change-password',
    home: '/home',
  };

  function loginResponse(mustChangePassword: boolean): IApiResult<ICurrentUserDto> {
    return {
      data: {
        id: 'u1',
        userName: 'nguyen.van.a',
        email: null,
        fullName: 'Nguyễn Văn A',
        roles: ['User'],
        mustChangePassword,
      },
      message: 'OK',
      status: 'SUCCESS',
      // `code` là `ApiErrorCode` KHÔNG nullable — nhánh thành công mang `'Success'`, không phải
      // `null` (api-result.model.ts §IApiResult).
      code: 'Success',
      businessCode: null,
      traceId: 'trace-login',
      retryable: false,
      fields: null,
    };
  }

  let fixture: ComponentFixture<LoginPage>;
  let httpMock: HttpTestingController;
  let navigateByUrl: jasmine.Spy;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([{ path: '**', children: [] }]),
        provideCoreBranding({ name: 'Ứng Dụng Thử', shortName: 'UT' }),
        provideCoreRoutes(OTHER_PRODUCT_ROUTES),
        provideTranslateService(),
        // `LanguageSwitcher` trong template của màn này inject `LanguageService`, mà service đó
        // inject `CORE_I18N` — token cố ý không có giá trị mặc định (core/i18n/core-i18n.ts), nên
        // harness phải tự cấp, nếu không TestBed nổ NG0201.
        { provide: CORE_I18N, useValue: TEST_I18N },
      ],
    });
    httpMock = TestBed.inject(HttpTestingController);
    await useTranslationsInTest('vi');
    await TestBed.inject(Router).navigate([]);
    // Chặn ở `navigateByUrl` thay vì đọc `router.url` sau đó: `/home` và `/change-password` KHÔNG
    // có trong bảng route của harness nên điều hướng thật rơi vào `**` và nuốt mất chuỗi cần đo —
    // đúng kiểu lỗi im lặng mà test này sinh ra để bắt.
    navigateByUrl = spyOn(TestBed.inject(Router), 'navigateByUrl').and.returnValue(Promise.resolve(true));
    fixture = TestBed.createComponent(LoginPage);
    fixture.detectChanges();
  });

  afterEach(() => httpMock.verify());

  /** Điền form, bấm Đăng nhập, trả 200 để chạy TRỌN nhánh điều hướng (khác `submit()` ở trên). */
  function loginOk(mustChangePassword: boolean): void {
    const host = fixture.nativeElement as HTMLElement;
    (host.querySelector('#userName') as HTMLInputElement).value = 'nguyen.van.a';
    host.querySelector('#userName')?.dispatchEvent(new Event('input'));
    (host.querySelector('#password') as HTMLInputElement).value = 'MatKhau@123';
    host.querySelector('#password')?.dispatchEvent(new Event('input'));
    (host.querySelector('form') as HTMLFormElement).dispatchEvent(new Event('submit'));

    httpMock.expectOne('/auth/login').flush(loginResponse(mustChangePassword));
    // Đăng nhập thành công kéo theo mồi lại cookie CSRF (auth.service.ts §login). Không flush thì
    // `httpMock.verify()` ở `afterEach` đỏ vì còn request treo — lỗi lạc đề, che mất assert thật.
    httpMock.expectOne('/antiforgery/token').flush({ token: 'CfDJ8-fake-token' });
  }

  it('🛑 đăng nhập xong, không phải đổi mật khẩu → CORE_ROUTES.home', () => {
    loginOk(false);

    expect(navigateByUrl)
      .withContext('khai cứng "/trang-chu" ở đây = sản phẩm thứ hai đăng nhập xong rơi vào ** im lặng')
      .toHaveBeenCalledOnceWith(OTHER_PRODUCT_ROUTES.home);
  });

  it('🛑 đăng nhập xong, BE bắt đổi mật khẩu → CORE_ROUTES.changePassword', () => {
    loginOk(true);

    expect(navigateByUrl).toHaveBeenCalledOnceWith(OTHER_PRODUCT_ROUTES.changePassword);
  });
});

/**
 * ĐI BỘ XUYÊN TƯỜNG — màn đăng nhập song ngữ, ĐẦU-CUỐI (bước 7 của
 * doc/huong_dan/wiki-core/be/16-i18n-va-ma-loi.md §7, chi tiết phần FE ở
 * doc/huong_dan/wiki-core/fe/08-i18n.md §Thứ tự thi công).
 *
 * Mục tiêu KHÔNG phải "bọc xong màn đăng nhập" mà là chứng minh CẢ CHUỖI MẮT XÍCH chạy, trong khi
 * còn rẻ — bốn mắt xích, mỗi cái một `it`:
 *
 *  a) chuỗi tĩnh của màn đổi theo ngôn ngữ;
 *  b) thông điệp lỗi từ BE (`businessCode` → khoá dịch) — mắt xích chứng minh "hướng A" chạy thật;
 *  c) lỗi validate CÓ THAM SỐ (`fieldErrors[].messageParams`) ráp được vào câu dịch;
 *  d) đổi ngôn ngữ TẠI CHỖ: không tải lại, không gửi lại request, state trên form giữ nguyên.
 *
 * Dùng `httpErrorInterceptor` THẬT (không stub): `LoginPage` đọc `err.apiResult`, mà trường đó do
 * chính interceptor gắn vào. Stub nó đi thì test xanh trên một đường dữ liệu không tồn tại.
 */
describe('LoginPage — đi bộ xuyên tường: song ngữ đầu-cuối', () => {
  let fixture: ComponentFixture<LoginPage>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(withInterceptors([httpErrorInterceptor])),
        provideHttpClientTesting(),
        provideRouter([{ path: '**', children: [] }]),
        provideCoreBranding({ name: 'Ứng Dụng Thử', shortName: 'UT' }),
        provideCoreRoutes(APP_ROUTES_TODAY),
        provideTranslateService(),
        { provide: CORE_I18N, useValue: TEST_I18N },
      ],
    });
    httpMock = TestBed.inject(HttpTestingController);
    await useTranslationsInTest('vi');
    // Đi qua `LanguageService` chứ không chỉ `TranslateService`: đây là nơi DUY NHẤT đặt
    // `<html lang>` và nhãn PrimeNG, và mắt xích (d) đo đúng thẻ đó. Trong app thật việc này do
    // `provideCoreI18n` → `provideAppInitializer` làm lúc khởi động (core/i18n/core-i18n.ts).
    await TestBed.inject(LanguageService).use('vi');
    await TestBed.inject(Router).navigate([]);
    fixture = TestBed.createComponent(LoginPage);
    fixture.detectChanges();
  });

  afterEach(() => httpMock.verify());

  function host(): HTMLElement {
    return fixture.nativeElement as HTMLElement;
  }

  function errorText(): string {
    return (
      (host().querySelector('.login-error span') as HTMLElement | null)?.textContent?.trim() ?? ''
    );
  }

  function optionFor(label: string): HTMLButtonElement {
    return Array.from(host().querySelectorAll('.lang-switch__option')).find(
      (element) => element.textContent?.trim() === label,
    ) as HTMLButtonElement;
  }

  /** Bấm nút ngôn ngữ theo nhãn của nó và chờ bảng dịch được áp. */
  async function switchTo(label: string): Promise<void> {
    const button = optionFor(label);
    expect(button).withContext(`không tìm thấy nút ngôn ngữ "${label}"`).toBeDefined();

    button.click();
    // `LanguageService.use()` là async (nạp bảng dịch). Ở đây bảng đã nạp sẵn nên chỉ cần nhường
    // một microtask cho promise settle, rồi mới đo DOM.
    await Promise.resolve();
    await fixture.whenStable();
    fixture.detectChanges();
  }

  /** Điền form rồi bấm Đăng nhập; trả về request đang chờ để test tự quyết định flush cái gì. */
  function submitWith(userName: string, password: string): TestRequest {
    (host().querySelector('#userName') as HTMLInputElement).value = userName;
    host().querySelector('#userName')?.dispatchEvent(new Event('input'));
    (host().querySelector('#password') as HTMLInputElement).value = password;
    host().querySelector('#password')?.dispatchEvent(new Event('input'));
    (host().querySelector('form') as HTMLFormElement).dispatchEvent(new Event('submit'));
    return httpMock.expectOne('/auth/login');
  }

  it('🛑 (a) chuỗi TĨNH của màn đổi theo ngôn ngữ — nhãn, gợi ý, nút, câu hướng dẫn', async () => {
    const label = (): string =>
      (host().querySelector('label[for="userName"]') as HTMLElement).textContent?.trim() ?? '';
    const submitButton = (): string =>
      (host().querySelector('button[type="submit"]') as HTMLElement).textContent?.trim() ?? '';
    const passwordPlaceholder = (): string =>
      (host().querySelector('#password') as HTMLInputElement).placeholder;

    expect(label()).toBe('Tên đăng nhập');
    expect(submitButton()).toBe('Đăng nhập');
    expect(passwordPlaceholder()).toBe('Nhập mật khẩu');
    expect(host().textContent).toContain('Quên mật khẩu? Liên hệ quản trị viên để được đặt lại.');

    await switchTo('English');

    expect(label()).toBe('User name');
    expect(submitButton()).toBe('Sign in');
    expect(passwordPlaceholder()).toBe('Enter your password');
    expect(host().textContent).toContain('Forgot your password? Contact an administrator');
    // Không còn chữ Việt nào sót lại trên card — phép kiểm này bắt chuỗi QUÊN BỌC, thứ không lint
    // rule nào bắt được (doc/huong_dan/wiki-core/fe/08-i18n.md §Cạm bẫy 3). "Tiếng Việt" là nhãn
    // của chính nút ngôn ngữ, CỐ Ý không dịch — trừ nó ra.
    const withoutSwitcher = host().textContent?.replace('Tiếng Việt', '') ?? '';
    expect(withoutSwitcher)
      .withContext('còn chữ có dấu tiếng Việt trên giao diện tiếng Anh = còn chuỗi chưa bọc')
      .not.toMatch(/[àáảãạăâđêôơư]/i);
  });

  it('🛑 (b) AUTH.INVALID_CREDENTIALS — BE trả MÃ, FE dịch: tiếng Việt ở VI, tiếng Anh ở EN', async () => {
    // Envelope THẬT của nhánh sai mật khẩu: 422 + `businessCode`, `message` là câu tiếng Việt
    // dev-facing của BE (AuthErrors.InvalidCredentials). Nếu FE đọc `message` thay vì tra mã thì
    // vế tiếng Anh dưới đây ĐỎ — đó chính là điều mắt xích này sinh ra để chứng minh.
    const envelope: IApiResult<null> = {
      data: null,
      message: 'Tên đăng nhập hoặc mật khẩu không đúng.',
      status: 'BUSINESS_ERROR',
      code: 'BusinessRuleError',
      businessCode: 'AUTH.INVALID_CREDENTIALS',
      traceId: 'trace-1',
      retryable: false,
      fields: null,
    };
    submitWith('nguyen.van.a', 'sai-mat-khau').flush(envelope, {
      status: 422,
      statusText: 'Unprocessable Entity',
    });
    fixture.detectChanges();

    expect(errorText()).toBe('Tên đăng nhập hoặc mật khẩu không đúng.');

    await switchTo('English');

    expect(errorText())
      .withContext(
        'khối lỗi phải dịch LẠI khi đổi ngôn ngữ — giữ sẵn câu đã dịch thì người dùng thấy một ' +
          'câu tiếng Việt giữa giao diện tiếng Anh, và không có gì báo',
      )
      .toBe('The user name or password is incorrect.');
  });

  it('🛑 (c) lỗi validate CÓ THAM SỐ — messageParams ráp vào câu dịch của cả hai ngôn ngữ', async () => {
    // Ca có thật, bấm tay được: mật khẩu là MỘT DẤU CÁCH đi lọt cả `required` của trình duyệt lẫn
    // phép kiểm rỗng của màn hình, nhưng `NotEmpty()` của FluentValidation coi chuỗi toàn khoảng
    // trắng là rỗng ⇒ 400 kèm `fieldErrors`.
    const envelope: IApiResult<null> = {
      data: null,
      message: 'Dữ liệu không hợp lệ.',
      status: 'VALIDATION_ERROR',
      code: 'ValidationError',
      businessCode: 'VALIDATION.FAILED',
      traceId: 'trace-2',
      retryable: false,
      fields: { Password: ["'Password' không được rỗng."] },
      // Khoá `Password` là TÊN PROPERTY C# giữ PascalCase (GlobalExceptionHandler.NormalizeField);
      // `messageParams.PropertyName` là chuỗi FluentValidation tự sinh từ tên property đó.
      fieldErrors: {
        Password: [
          {
            code: 'NotEmptyValidator',
            message: "'Password' không được rỗng.",
            messageParams: { PropertyName: 'Password' },
          },
        ],
      },
    };
    submitWith('nguyen.van.a', ' ').flush(envelope, { status: 400, statusText: 'Bad Request' });
    fixture.detectChanges();

    // Hai điều được chứng minh cùng lúc:
    //  · tham số ĐƯỢC ráp (câu mang tên ô nhập, không phải "{{PropertyName}}" trần);
    //  · tham số bị GHI ĐÈ bằng nhãn của màn hình, nên câu tiếng Việt không lẫn chữ "Password".
    expect(errorText()).toBe('Vui lòng nhập Mật khẩu.');

    await switchTo('English');

    expect(errorText()).toBe('Please enter Password.');
  });

  it('🛑 (c-đối chứng) tham số KHÔNG ráp thì câu lộ ngay — phép kiểm trên đỏ được', () => {
    // Không có `it` này thì `it` trên vẫn xanh kể cả khi `fieldMessage` bỏ qua `messageParams`:
    // câu khi đó là "Vui lòng nhập {{PropertyName}}." — khác hẳn, nhưng phải có ai đó nói ra.
    const raw = 'Vui lòng nhập {{PropertyName}}.';
    expect(raw).not.toBe('Vui lòng nhập Mật khẩu.');
    expect(raw).toContain('{{PropertyName}}');
  });

  it('🛑 (d) đổi ngôn ngữ TẠI CHỖ: <html lang> đổi, state form giữ nguyên, KHÔNG gửi lại request', async () => {
    (host().querySelector('#userName') as HTMLInputElement).value = 'nguyen.van.a';
    host().querySelector('#userName')?.dispatchEvent(new Event('input'));
    (host().querySelector('.field-row .check') as HTMLInputElement).click();
    fixture.detectChanges();

    expect(document.documentElement.lang).toBe('vi');

    await switchTo('English');

    // WCAG 3.1.1 Language of Page (mức A) — việc quên không thấy đau: không pixel nào đổi màu khi
    // nó sai (doc/huong_dan/wiki-core/fe/08-i18n.md §Cạm bẫy 5).
    expect(document.documentElement.lang).toBe('en');
    // "Tại chỗ" = state đang có trên màn hình không mất. Tải lại trang sẽ xoá cả hai giá trị này.
    expect((host().querySelector('#userName') as HTMLInputElement).value).toBe('nguyen.van.a');
    expect((host().querySelector('.field-row .check') as HTMLInputElement).checked).toBeTrue();
    // `httpMock.verify()` ở `afterEach` là phần còn lại của phép kiểm: đổi ngôn ngữ mà bắn thêm
    // request nào (nạp lại bảng dịch qua HttpClient của app, gọi lại API) sẽ làm nó ĐỎ.
  });

  it('nút của ngôn ngữ ĐANG dùng mang aria-current, và mỗi nút mang lang riêng (WCAG 3.1.2)', async () => {
    expect(optionFor('Tiếng Việt').getAttribute('aria-current')).toBe('true');
    expect(optionFor('English').getAttribute('aria-current')).toBeNull();
    // Nhãn viết bằng CHÍNH ngôn ngữ đó, nên nó là "language of parts" và phải được đánh dấu —
    // thiếu `lang` thì trình đọc màn hình phát âm "English" bằng bộ âm tiếng Việt.
    expect(optionFor('English').getAttribute('lang')).toBe('en');
    expect(optionFor('Tiếng Việt').getAttribute('lang')).toBe('vi');

    await switchTo('English');

    expect(optionFor('English').getAttribute('aria-current')).toBe('true');
    expect(optionFor('Tiếng Việt').getAttribute('aria-current')).toBeNull();
  });
});

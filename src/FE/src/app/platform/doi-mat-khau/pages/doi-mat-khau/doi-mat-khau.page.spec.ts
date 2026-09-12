import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { Router, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { DoiMatKhauPage } from './doi-mat-khau.page';
import { provideCoreBranding } from '../../../../core/config/core-branding';
import { AuthService } from '../../../../core/auth/auth.service';
import { ApiFieldError, IApiResult } from '../../../../core/http/api-result.model';
import { ICoreRoutes, provideCoreRoutes } from '../../../../core/config/core-routes';
import { provideTranslateService } from '@ngx-translate/core';
import { useTranslationsInTest } from '../../../../core/i18n/i18n.testing';

/**
 * Bản sao ba đường dẫn `app.config.ts` (`APP_CORE_ROUTES`) bơm vào — chép chứ không import, cùng
 * lý do đã ghi ở `core/auth/guards.spec.ts`: describe đầu file đo hành vi hiện tại nên phải giữ
 * nguyên chuỗi cũ.
 */
const APP_ROUTES_TODAY: ICoreRoutes = {
  signIn: '/dang-nhap',
  changePassword: '/doi-mat-khau',
  home: '/trang-chu',
};

/** Envelope lỗi như `httpErrorInterceptor` gắn vào `HttpErrorResponse` (`apiResult`). */
function envelopeError(
  message: string,
  fieldErrors: Record<string, ApiFieldError[]> | null,
): { apiResult: IApiResult<unknown> } {
  return {
    apiResult: {
      data: null,
      message,
      status: 'VALIDATION_ERROR',
      code: 'ValidationError',
      businessCode: null,
      traceId: 'trace-change-password',
      retryable: null,
      fields: null,
      fieldErrors,
    },
  };
}

/**
 * FE-2 + FE-6 — form đổi mật khẩu bind `fields` vào từng ô và gắn đủ dấu hiệu a11y.
 *
 * Key của `fields` là **PascalCase khớp property C#** (`ChangePasswordCommand.CurrentPassword` /
 * `.NewPassword`) — ngoại lệ duy nhất so với phần còn lại của envelope vốn camelCase, xem
 * doc/huong_dan/quy-uoc/fe-api-client.md §Envelope. Camel hoá lại là làm lỗi biến mất im lặng.
 */
describe('DoiMatKhauPage — lỗi theo từng ô', () => {
  let fixture: ComponentFixture<DoiMatKhauPage>;
  let page: DoiMatKhauPage;
  let auth: AuthService;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        // `AuthCard` (khung của màn này) inject CORE_BRANDING để lấy chữ tắt thương hiệu —
        // token cố ý không có giá trị mặc định, harness phải tự cấp. Xem
        // core/config/core-branding.ts.
        provideCoreBranding({ name: 'Ứng Dụng Thử', shortName: 'UT' }),
        // `DoiMatKhauPage` inject CORE_ROUTES để biết đổi xong thì đi đâu — token cố ý không có
        // giá trị mặc định nên harness phải tự cấp, nếu không TestBed nổ NullInjectorError.
        provideCoreRoutes(APP_ROUTES_TODAY),
        provideTranslateService(),
      ],
    });
    await useTranslationsInTest();
    auth = TestBed.inject(AuthService);
    fixture = TestBed.createComponent(DoiMatKhauPage);
    page = fixture.componentInstance;
    fixture.detectChanges();
  });

  function el<T extends HTMLElement>(selector: string): T {
    return fixture.nativeElement.querySelector(selector) as T;
  }

  function fill(id: string, value: string): void {
    const input = el<HTMLInputElement>(`#${id}`);
    input.value = value;
    input.dispatchEvent(new Event('input'));
  }

  function errorTexts(): (string | undefined)[] {
    return Array.from(fixture.nativeElement.querySelectorAll('.form-error')).map((node) =>
      (node as HTMLElement).textContent?.trim(),
    );
  }

  it('bấm Lưu với form trống → báo HẾT 3 ô cùng lúc, không gọi API', () => {
    const spy = spyOn(auth, 'changePassword');

    page.onSubmit();
    fixture.detectChanges();

    expect(spy).not.toHaveBeenCalled();
    expect(errorTexts().length).toBe(3);
    expect(el<HTMLInputElement>('#currentPassword').getAttribute('aria-invalid')).toBe('true');
    expect(el<HTMLInputElement>('#newPassword').getAttribute('aria-invalid')).toBe('true');
    expect(el<HTMLInputElement>('#confirmPassword').getAttribute('aria-invalid')).toBe('true');
  });

  it('xác nhận không khớp → lỗi nằm dưới ĐÚNG ô xác nhận, không phải ô mật khẩu mới', () => {
    fill('currentPassword', 'MatKhauCu@1');
    fill('newPassword', 'MatKhauMoi@123');
    fill('confirmPassword', 'MatKhauMoi@124');
    fixture.detectChanges();

    page.onSubmit();
    fixture.detectChanges();

    const confirmInput = el<HTMLInputElement>('#confirmPassword');
    expect(confirmInput.getAttribute('aria-invalid')).toBe('true');
    expect(el<HTMLElement>(`#${confirmInput.getAttribute('aria-describedby')}`).textContent?.trim()).toBe(
      'Xác nhận mật khẩu mới không khớp.',
    );
    expect(el<HTMLInputElement>('#newPassword').hasAttribute('aria-invalid')).toBeFalse();
  });

  /**
   * Hồi quy của bug hằng số chính sách (tìm ra 2026-09-04): FE giữ 8 trong khi BE CƯỠNG CHẾ 12
   * (`options.Password.RequiredLength`), nên mật khẩu 9–11 ký tự được form cho qua rồi BE từ chối —
   * người dùng lãnh một vòng mạng và một câu từ chối không nói được con số nào.
   *
   * Test khoá CẢ HAI nửa của bản sửa, vì mỗi nửa một mình đều không đủ:
   *  (1) ngưỡng khớp BE — 11 ký tự KHÔNG được gửi đi;
   *  (2) câu báo lỗi KHÔNG chứa chữ số nào — con số hiện cho người dùng phải đến từ `messageParams`
   *      BE gửi kèm mã lỗi, không từ hằng số của FE (doc/huong_dan/wiki-core/fe/08-i18n.md §7,
   *      .../be/16-i18n-va-ma-loi.md §10.6). Thiếu (2) thì lần trôi sau lại hiện ra thành một câu
   *      nói sai con số, đúng kiểu hỏng vừa sửa.
   */
  it('mật khẩu mới 11 ký tự → chặn tại chỗ, và câu lỗi KHÔNG mang con số của FE', () => {
    const spy = spyOn(auth, 'changePassword');
    // Dài hơn ngưỡng CŨ (8) nhưng ngắn hơn ngưỡng BE (12) — đúng khe mà bug lọt qua.
    const elevenChars = 'MatKhau@123';
    expect(elevenChars.length).toBe(11);

    fill('currentPassword', 'MatKhauCu@1');
    fill('newPassword', elevenChars);
    fill('confirmPassword', elevenChars);
    fixture.detectChanges();

    page.onSubmit();
    fixture.detectChanges();

    expect(spy).not.toHaveBeenCalled();
    const newInput = el<HTMLInputElement>('#newPassword');
    expect(newInput.getAttribute('aria-invalid')).toBe('true');
    const shown = el<HTMLElement>(`#${newInput.getAttribute('aria-describedby')}`).textContent?.trim() ?? '';
    expect(shown).not.toBe('');
    expect(shown).not.toMatch(/[0-9]/);
  });

  it('`fields` của BE hiện dưới đúng ô, và câu chung mơ hồ bị ẩn đi', () => {
    spyOn(auth, 'changePassword').and.returnValue(
      throwError(() => envelopeError('Dữ liệu không hợp lệ.', {
        NewPassword: [{ code: 'TestValidator', message: 'Mật khẩu mới quá yếu.' }],
      })),
    );

    fill('currentPassword', 'MatKhauCu@1');
    fill('newPassword', 'MatKhauMoi@123');
    fill('confirmPassword', 'MatKhauMoi@123');
    page.onSubmit();
    fixture.detectChanges();

    const newInput = el<HTMLInputElement>('#newPassword');
    expect(newInput.getAttribute('aria-invalid')).toBe('true');
    expect(newInput.classList).toContain('invalid');
    expect(el<HTMLElement>(`#${newInput.getAttribute('aria-describedby')}`).textContent?.trim()).toBe(
      'Mật khẩu mới quá yếu.',
    );
    // Có lỗi ô cụ thể thì không lặp lại "Dữ liệu không hợp lệ." ở dải lỗi đầu form.
    expect(el<HTMLElement>('.login-error')).toBeNull();
  });

  it('lỗi KHÔNG có `fields` (422 AUTH.CHANGE_PASSWORD_FAILED) vẫn hiện ở dải lỗi đầu form', () => {
    spyOn(auth, 'changePassword').and.returnValue(
      throwError(() => envelopeError('Mật khẩu hiện tại không đúng.', null)),
    );

    fill('currentPassword', 'SaiRoi@1');
    fill('newPassword', 'MatKhauMoi@123');
    fill('confirmPassword', 'MatKhauMoi@123');
    page.onSubmit();
    fixture.detectChanges();

    expect(el<HTMLElement>('.login-error').textContent).toContain('Mật khẩu hiện tại không đúng.');
    expect(errorTexts().length).toBe(0);
  });

  it('🛑 mật khẩu mới quá phổ biến → câu nói phải làm gì, DƯỚI ô mật khẩu mới', () => {
    // Màn này đi qua ĐÚNG dàn `IPasswordValidator` như màn tạo người dùng: `ChangePasswordAsync`
    // chạy `AddTop10000PasswordValidator`, nên `CommonPassword` phát ra ở đây y hệt. BE ánh xạ mã
    // đó về ô mật khẩu MỚI — không phải ô mật khẩu hiện tại (bẫy 1 của `IdentityFieldErrors`).
    spyOn(auth, 'changePassword').and.returnValue(
      throwError(() =>
        envelopeError('Đổi mật khẩu thất bại.', {
          NewPassword: [{ code: 'CommonPassword', message: 'CommonPassword' }],
        }),
      ),
    );

    fill('currentPassword', 'MatKhauCu@1');
    fill('newPassword', 'qwerty123456');
    fill('confirmPassword', 'qwerty123456');
    page.onSubmit();
    fixture.detectChanges();

    const newInput = el<HTMLInputElement>('#newPassword');
    expect(newInput.getAttribute('aria-invalid')).toBe('true');
    const text = el<HTMLElement>(`#${newInput.getAttribute('aria-describedby')}`).textContent ?? '';
    expect(text).toContain('phổ biến');
    expect(text)
      .withContext('mã máy KHÔNG được lọt ra màn hình — đường lùi cũ trả thẳng `code`')
      .not.toContain('CommonPassword');
    expect(el<HTMLInputElement>('#currentPassword').hasAttribute('aria-invalid'))
      .withContext('ô mật khẩu HIỆN TẠI người dùng gõ đúng — đừng tô đỏ nó')
      .toBeFalse();
  });

  it('🛑 lỗi mức BẢN GHI (`$record`) vẫn hiện ở dải lỗi đầu form, không rơi vào lỗ đen', () => {
    // Bảng ánh xạ mã → ô của BE là allowlist; mã Identity ngoài bảng rơi về `$record`. Trước bản
    // vá 2026-09-11, khoá đó vừa không hiện dưới ô nào vừa làm dải lỗi đầu form biến mất.
    spyOn(auth, 'changePassword').and.returnValue(
      throwError(() =>
        envelopeError('Đổi mật khẩu thất bại.', {
          $record: [{ code: 'ConcurrencyFailure', message: 'ConcurrencyFailure' }],
        }),
      ),
    );

    fill('currentPassword', 'MatKhauCu@1');
    fill('newPassword', 'MatKhauMoi@123');
    fill('confirmPassword', 'MatKhauMoi@123');
    page.onSubmit();
    fixture.detectChanges();

    const banner = el<HTMLElement>('.login-error');
    expect(banner).withContext('không có dải lỗi = người dùng không biết vì sao bị từ chối').not.toBeNull();
    expect(banner.textContent?.trim()).toBe('Giá trị này không được chấp nhận. Vui lòng kiểm tra lại.');
    expect(errorTexts().length).withContext('`$record` không thuộc ô nào, đừng gắn bừa vào một ô').toBe(0);
  });
});

/**
 * Đường ra của màn này từng khai cứng `navigateByUrl('/trang-chu')` (gỡ 2026-09-03) — trong khi
 * đường VÀO (`mustChangePasswordGuard`) đã đọc `CORE_ROUTES` từ 2026-09-02. Hai đầu đọc hai nguồn
 * khác nhau là trạng thái lệch được: đổi bảng route thì guard đi đúng, còn màn hình thì không.
 *
 * Ba đường dẫn dưới khác hẳn dự án này, nên một chuỗi khai cứng sót lại sẽ ĐỎ tại đây và chỉ tại đây.
 */
describe('DoiMatKhauPage — đích sau khi đổi mật khẩu đến từ CORE_ROUTES', () => {
  const OTHER_PRODUCT_ROUTES: ICoreRoutes = {
    signIn: '/sign-in',
    changePassword: '/change-password',
    home: '/home',
  };

  let fixture: ComponentFixture<DoiMatKhauPage>;
  let auth: AuthService;
  let navigateByUrl: jasmine.Spy;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        provideCoreBranding({ name: 'Ứng Dụng Thử', shortName: 'UT' }),
        provideCoreRoutes(OTHER_PRODUCT_ROUTES),
        provideTranslateService(),
      ],
    });
    await useTranslationsInTest();
    auth = TestBed.inject(AuthService);
    // Chặn ở `navigateByUrl`: `/home` không có trong bảng route rỗng của harness nên điều hướng
    // thật sẽ không đi tới đâu và chuỗi cần đo biến mất.
    navigateByUrl = spyOn(TestBed.inject(Router), 'navigateByUrl').and.returnValue(Promise.resolve(true));
    fixture = TestBed.createComponent(DoiMatKhauPage);
    fixture.detectChanges();
  });

  it('🛑 đổi mật khẩu thành công → CORE_ROUTES.home', () => {
    spyOn(auth, 'changePassword').and.returnValue(of(undefined));
    const host = fixture.nativeElement as HTMLElement;
    for (const [id, value] of [
      ['currentPassword', 'MatKhauCu@1'],
      ['newPassword', 'MatKhauMoi@123'],
      ['confirmPassword', 'MatKhauMoi@123'],
    ]) {
      const input = host.querySelector(`#${id}`) as HTMLInputElement;
      input.value = value;
      input.dispatchEvent(new Event('input'));
    }
    fixture.componentInstance.onSubmit();

    expect(navigateByUrl)
      .withContext('khai cứng "/trang-chu" ở đây = sản phẩm thứ hai đổi mật khẩu xong thì đi lạc')
      .toHaveBeenCalledOnceWith(OTHER_PRODUCT_ROUTES.home);
  });
});

import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { provideTranslateService } from '@ngx-translate/core';
import { CurrentUserService } from '../../../core/auth/current-user.service';
import { ICoreRoutes, provideCoreRoutes } from '../../../core/config/core-routes';
import { useTranslationsInTest } from '../../../core/i18n/i18n.testing';
import { Topbar } from './topbar';

/** Đường dẫn THẬT của dự án — chỉ dùng cho nhóm test hỏi "màn hình có gì". */
const APP_ROUTES: ICoreRoutes = { signIn: '/dang-nhap', changePassword: '/doi-mat-khau', home: '/trang-chu' };

function signedInUser(): void {
  TestBed.inject(CurrentUserService).setUser({
    Id: 'u1',
    UserName: 'admin',
    Email: 'admin@example.com',
    FullName: 'Nguyễn Văn A',
    Roles: ['Admin'],
    MustChangePassword: false,
  });
}

/**
 * Bộ test này ra đời từ một REGRESSION THẬT (2026-09-11), nên nó đo đúng thứ đã hỏng.
 *
 * Lượt hoán đổi `/trang-chu` (Q3) xoá `platform/trang-chu/` — và trang đó là nơi DUY NHẤT chứa lối
 * vào **tự nguyện** của màn đổi mật khẩu. Hậu quả không làm đỏ gì cả: build xanh, lint xanh, 639
 * test xanh, luồng **ép buộc** vẫn chạy bình thường, và chỉ những người đã đổi mật khẩu rồi mới
 * phát hiện ra rằng họ không còn đường nào ngoài gõ thẳng URL.
 *
 * Đó là lý do phép kiểm phải nằm ở đây chứ không nằm trong spec của màn đổi mật khẩu: thứ biến mất
 * không phải cái màn, mà là **đường tới nó**.
 */
describe('Topbar — vùng tài khoản của app-shell', () => {
  let fixture: ComponentFixture<Topbar>;

  async function boot(authenticated: boolean): Promise<void> {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([{ path: '**', children: [] }]),
        provideTranslateService(),
        provideCoreRoutes(APP_ROUTES),
      ],
    });
    await useTranslationsInTest();
    if (authenticated) signedInUser();

    fixture = TestBed.createComponent(Topbar);
    fixture.componentRef.setInput('title', 'Trang chủ');
    fixture.detectChanges();
  }

  function host(): HTMLElement {
    return fixture.nativeElement as HTMLElement;
  }

  function changePasswordLink(): HTMLAnchorElement | null {
    return host().querySelector(`.topbar-user a[href="${APP_ROUTES.changePassword}"]`);
  }

  it('🛑 CÓ lối vào màn Đổi mật khẩu — đây là lối TỰ NGUYỆN duy nhất còn lại của app', async () => {
    await boot(true);
    expect(changePasswordLink()).withContext('mất nút này = chỉ còn cách gõ thẳng URL').not.toBeNull();
  });

  it('🛑 là `<a>` điều hướng, KHÔNG phải `<button>` — phải mở được ở tab mới', async () => {
    await boot(true);
    expect(changePasswordLink()?.tagName).toBe('A');
  });

  it('giữ TÊN TRUY CẬP ĐƯỢC kể cả khi nhãn bị ẩn ở khổ hẹp', async () => {
    await boot(true);
    // Nhãn của CẢ HAI hành động đều ẩn bằng CSS ở breakpoint mobile; thiếu `aria-label` thì trình
    // đọc màn hình chỉ đọc được "link"/"button" và người dùng bàn phím không biết nó làm gì.
    expect(changePasswordLink()?.getAttribute('aria-label')).toBe('Đổi mật khẩu');
    expect(host().querySelector('.topbar-user button')?.getAttribute('aria-label')).toBe('Đăng xuất');
  });

  it('🛑 hai nhãn cùng nằm trong `.topbar-action-label` — ẩn ở mobile thì ẩn CẢ HAI', async () => {
    await boot(true);
    // Bất đối xứng ở đây cho ra `[🔑][🚪 Đăng xuất]`: một glyph trần cạnh một nút có nhãn đầy đủ.
    const labels = [...host().querySelectorAll('.topbar-user .topbar-action-label')];
    expect(labels.map((el) => el.textContent?.trim())).toEqual(['Đổi mật khẩu', 'Đăng xuất']);
  });

  it('🛑 CHƯA đăng nhập ⇒ KHÔNG render vùng tài khoản, nên cũng không có lối vào', async () => {
    await boot(false);
    expect(host().querySelector('.topbar-user')).toBeNull();
    expect(changePasswordLink()).toBeNull();
  });

  it('vẫn giữ nút Đăng xuất — hai hành động tài khoản đứng cạnh nhau, không thay thế nhau', async () => {
    await boot(true);
    const buttons = [...host().querySelectorAll('.topbar-user button')] as HTMLButtonElement[];
    expect(buttons.length).toBe(1);
    expect(buttons[0].textContent?.trim()).toBe('Đăng xuất');
  });
});

/**
 * Đây là test CHỨNG MINH Topbar thật sự đi qua seam `CORE_ROUTES`, chứ không chỉ tình cờ trỏ đúng.
 *
 * 🛑 **Vì sao phải là một describe RIÊNG với đường dẫn NGOẠI LAI.** Bản đầu của bộ test này
 * (2026-09-11) khai hằng số đúng bằng đường dẫn thật của dự án rồi assert
 * `href === ROUTES.changePassword`. Phép kiểm đó **không thể đỏ**: thay
 * `[routerLink]="routes.changePassword"` bằng `routerLink="/doi-mat-khau"` thì nó vẫn xanh. Nó đo
 * *"có trỏ đúng chỗ không"*, trong khi tiêu đề nó tuyên bố là *"có đi qua seam không"* — đúng loại
 * test cho qua một hồi quy y hệt lần sau.
 *
 * Ba đường dẫn dưới đây KHÁC HẲN đường dẫn của dự án này — đúng thứ một sản phẩm thứ hai dựng trên
 * CoreBase sẽ khai. Chỗ nào trong Topbar còn khai cứng `/doi-mat-khau` hay `/dang-nhap` sẽ ĐỎ ở
 * đây và **chỉ** ở đây.
 *
 * Khuôn lấy nguyên từ `core/auth/guards.spec.ts` § "Guard — đường dẫn đến từ CORE_ROUTES". `shared/`
 * chịu **cùng** luật với `core/` vì nó cũng đi theo nền tảng sang sản phẩm khác
 * (`doc/kien-truc-core-module.md` §"Core giữ CƠ CHẾ, dự án cung cấp DỮ LIỆU") — và chính file
 * `topbar.ts` đã một lần khai cứng `/dang-nhap` rồi lọt lưới, vì phép kiểm khi đó chỉ quét `core/`.
 */
describe('Topbar — đường dẫn đến từ CORE_ROUTES, KHÔNG khai cứng trong shared/', () => {
  /** Đường dẫn của một sản phẩm giả định khác — không dòng nào trong `shared/` được biết trước. */
  const OTHER_PRODUCT_ROUTES: ICoreRoutes = {
    signIn: '/sign-in',
    changePassword: '/change-password',
    home: '/home',
  };

  let fixture: ComponentFixture<Topbar>;
  let httpMock: HttpTestingController;
  let router: Router;

  beforeEach(async () => {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([{ path: '**', children: [] }]),
        provideTranslateService(),
        provideCoreRoutes(OTHER_PRODUCT_ROUTES),
      ],
    });
    await useTranslationsInTest();
    httpMock = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
    signedInUser();

    fixture = TestBed.createComponent(Topbar);
    fixture.componentRef.setInput('title', 'Home');
    fixture.detectChanges();
  });

  it('🛑 liên kết Đổi mật khẩu đi theo `CORE_ROUTES.changePassword`, không phải `/doi-mat-khau`', () => {
    const link = (fixture.nativeElement as HTMLElement).querySelector('.topbar-user a');

    expect(link?.getAttribute('href')).toBe('/change-password');
    expect(link?.getAttribute('href'))
      .withContext('khai cứng ở đây = sản phẩm thứ hai có một liên kết chết trên mọi màn')
      .not.toBe('/doi-mat-khau');
  });

  it('🛑 đăng xuất xong điều hướng theo `CORE_ROUTES.signIn`, không phải `/dang-nhap`', async () => {
    const navigate = spyOn(router, 'navigateByUrl');
    (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>('.topbar-user button')?.click();

    httpMock.expectOne((req) => req.url === '/auth/logout').flush({ data: true, status: 'SUCCESS' });
    // `finalize()` của `AuthService.logout` mồi lại cookie CSRF — xả để `verify()` không báo đỏ ở
    // một chỗ chẳng liên quan.
    httpMock.match((req) => req.url === '/antiforgery/token').forEach((req) => req.flush({}));

    expect(navigate).toHaveBeenCalledWith('/sign-in');
  });

  afterEach(() => {
    httpMock.match(() => true).forEach((req) => req.flush({}));
    httpMock.verify();
  });
});

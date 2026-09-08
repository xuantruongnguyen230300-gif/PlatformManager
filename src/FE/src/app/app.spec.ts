import { TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { provideCoreRoutes } from './core/config/core-routes';
import { provideCoreBranding } from './core/config/core-branding';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { App } from './app';
import { GlobalErrorHandler } from './core/errors/global-error.handler';
import { provideTranslateService } from '@ngx-translate/core';
import { useTranslationsInTest } from './core/i18n/i18n.testing';

/**
 * Skip link — WCAG 2.2 AA 2.4.1 "Bypass Blocks", ràng buộc nghiệm thu của dự án
 * (doc/huong_dan/wiki-core/fe/15-accessibility.md §3d). Trước 2026-08-29 app-shell KHÔNG có link
 * này: người dùng bàn phím phải Tab qua toàn bộ sidebar ở MỌI trang mới tới được nội dung.
 */
describe('App shell — skip link', () => {
  async function render() {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
        // `Topbar` (trong app shell) inject CORE_ROUTES để biết đường về màn đăng nhập sau khi
        // đăng xuất. Token CỐ Ý không có giá trị mặc định — quên cấp là NG0201 ồn ào, thay vì
        // âm thầm điều hướng tới route không tồn tại. Xem core/config/core-routes.ts.
        provideCoreRoutes({ signIn: '/dang-nhap', changePassword: '/doi-mat-khau', home: '/trang-chu' }),
        // `Sidebar` (cũng trong app shell) inject CORE_BRANDING để đổ tên sản phẩm vào ô thương
        // hiệu — cùng loại seam, cùng lý do không có giá trị mặc định.
        provideCoreBranding({ name: 'Ứng Dụng Thử', shortName: 'UT' }),
        provideTranslateService(),
      ],
    });
    await useTranslationsInTest();
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    return fixture;
  }

  it('là phần tử focus được ĐẦU TIÊN trong shell và trỏ tới vùng nội dung chính', async () => {
    const host = (await render()).nativeElement as HTMLElement;

    const focusables = host.querySelectorAll('a[href], button, input, [tabindex]');
    const first = focusables[0] as HTMLElement;

    // Thứ tự DOM chính là thứ tự Tab — skip link đứng sau sidebar thì vô dụng.
    expect(first.classList.contains('skip-link'))
      .withContext('phần tử focus được đầu tiên phải là skip link')
      .toBeTrue();
    expect(first.getAttribute('href')).toBe('#main-content');
    expect(first.textContent?.trim()).toBeTruthy();
  });

  it('đích `#main-content` tồn tại và nhận được focus bằng chương trình', async () => {
    const host = (await render()).nativeElement as HTMLElement;

    const main = host.querySelector('#main-content') as HTMLElement | null;
    expect(main).withContext('href của skip link phải trỏ vào một phần tử có thật').not.toBeNull();
    expect(main?.tagName).toBe('MAIN');
    // Thiếu `tabindex="-1"` thì trình duyệt chỉ cuộn tới mà con trỏ bàn phím vẫn nằm ở sidebar.
    expect(main?.getAttribute('tabindex')).toBe('-1');
  });

  it('không in ra giấy — skip link chỉ có nghĩa khi tương tác', async () => {
    const host = (await render()).nativeElement as HTMLElement;
    expect((host.querySelector('.skip-link') as HTMLElement).classList).toContain('no-print');
  });
});

/**
 * Dải "đã có phiên bản mới" — người đang mở tab lúc deploy bấm sang một màn chưa tải sẽ xin một
 * file chunk không còn tồn tại; nginx trả `index.html` với HTTP **200**; trình duyệt cố chạy HTML
 * như JavaScript (doc/huong_dan/wiki-core/fe/17-phuc-vu-va-trien-khai.md §4). Không có dải này thì
 * kết cục là màn hình chết, không một chữ giải thích.
 */
describe('App shell — dải "đã có phiên bản mới"', () => {
  async function render() {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
        // `Topbar` (trong app shell) inject CORE_ROUTES để biết đường về màn đăng nhập sau khi
        // đăng xuất. Token CỐ Ý không có giá trị mặc định — quên cấp là NG0201 ồn ào, thay vì
        // âm thầm điều hướng tới route không tồn tại. Xem core/config/core-routes.ts.
        provideCoreRoutes({ signIn: '/dang-nhap', changePassword: '/doi-mat-khau', home: '/trang-chu' }),
        // `Sidebar` (cũng trong app shell) inject CORE_BRANDING để đổ tên sản phẩm vào ô thương
        // hiệu — cùng loại seam, cùng lý do không có giá trị mặc định.
        provideCoreBranding({ name: 'Ứng Dụng Thử', shortName: 'UT' }),
        provideTranslateService(),
      ],
    });
    await useTranslationsInTest();
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    return fixture;
  }

  it('bình thường KHÔNG hiện gì', async () => {
    const host = (await render()).nativeElement as HTMLElement;
    expect(host.querySelector('.app-update')).toBeNull();
  });

  it('lỗi tải chunk → hiện thông báo kèm nút tải lại, KHÔNG tự tải lại', async () => {
    const fixture = await render();
    const handler = TestBed.inject(GlobalErrorHandler);
    const reload = spyOn(handler, 'reloadApp');
    spyOn(console, 'error');

    handler.handleError(new TypeError('Failed to fetch dynamically imported module: /chunk-1.js'));
    fixture.detectChanges();

    const banner = (fixture.nativeElement as HTMLElement).querySelector('.app-update') as HTMLElement;
    expect(banner).not.toBeNull();
    expect(banner.getAttribute('role')).toBe('alert');
    expect(banner.textContent).toContain('Đã có phiên bản mới, tải lại để tiếp tục.');
    // Tự tải lại vứt mất dữ liệu người dùng đang nhập — đúng thứ unsavedChangesGuard đi bảo vệ.
    expect(reload).withContext('không được tự tải lại').not.toHaveBeenCalled();

    (banner.querySelector('.btn') as HTMLButtonElement).click();
    expect(reload).toHaveBeenCalled();
  });

  it('lỗi JavaScript khác KHÔNG làm hiện dải này', async () => {
    const fixture = await render();
    spyOn(console, 'error');

    TestBed.inject(GlobalErrorHandler).handleError(new TypeError('boom'));
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).querySelector('.app-update')).toBeNull();
  });
});

/**
 * Ô thương hiệu ở đầu sidebar — chốt chặn cho seam `CORE_BRANDING` tại ĐÚNG chỗ nó hiện ra.
 *
 * Vì sao cần thêm dù hai describe trên đã cấp token: cấp token chỉ chứng minh component KHÔNG
 * NỔ vì thiếu provider, không chứng minh nó ĐỌC token. Lỗ hổng đo được của tình trạng trước
 * 2026-09-03: viết lại template thành `<span class="brand-mark">PM</span>` cứng và gỡ dòng
 * `inject(CORE_BRANDING)` thì toàn bộ bộ test vẫn xanh — seam bị vô hiệu mà không gì đỏ.
 *
 * Giá trị token dùng ở đây CỐ Ý khác tên thật của dự án, cùng lý do đã ghi ở
 * `page-title.strategy.spec.ts`: assert lên đúng tên thật thì phép kiểm tự soi gương — nó xanh
 * cả khi chuỗi được khai cứng trở lại.
 */
describe('App shell — ô thương hiệu sidebar đọc CORE_BRANDING', () => {
  const TEST_BRANDING = { name: 'Ứng Dụng Thử', shortName: 'UT' };

  async function render() {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideCoreRoutes({ signIn: '/dang-nhap', changePassword: '/doi-mat-khau', home: '/trang-chu' }),
        provideCoreBranding(TEST_BRANDING),
        provideTranslateService(),
      ],
    });
    await useTranslationsInTest();
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  it('🛑 chữ tắt trong `.brand-mark` là chuỗi ĐÃ RENDER từ token, không phải hằng số trong template', async () => {
    const mark = (await render()).querySelector('.sidebar-brand .brand-mark') as HTMLElement;

    expect(mark).withContext('sidebar phải có ô vuông thương hiệu').not.toBeNull();
    expect(mark.textContent?.trim())
      .withContext('khai cứng chữ tắt trong sidebar.html = sản phẩm thứ hai mang chữ tắt của sản phẩm này')
      .toBe(TEST_BRANDING.shortName);
  });

  it('🛑 tên đầy đủ trong `.brand-text` là chuỗi ĐÃ RENDER từ token', async () => {
    const text = (await render()).querySelector('.sidebar-brand .brand-text') as HTMLElement;

    expect(text).withContext('sidebar phải có dòng chữ tên sản phẩm').not.toBeNull();
    expect(text.textContent?.trim()).toBe(TEST_BRANDING.name);
  });
});

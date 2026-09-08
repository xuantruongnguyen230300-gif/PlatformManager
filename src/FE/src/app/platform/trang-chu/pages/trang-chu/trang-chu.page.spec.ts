import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { TrangChuPage } from './trang-chu.page';
import { provideCoreBranding } from '../../../../core/config/core-branding';
import { ICoreRoutes, provideCoreRoutes } from '../../../../core/config/core-routes';
import { provideTranslateService } from '@ngx-translate/core';
import { useTranslationsInTest } from '../../../../core/i18n/i18n.testing';

/**
 * Đoạn dẫn của trang chủ gọi tên sản phẩm giữa một câu văn xuôi — dạng khai cứng khó thấy nhất
 * và là chỗ duy nhất trong `platform/` mà tên sản phẩm không nằm ở thuộc tính hay hằng số.
 *
 * Vì sao phải có test riêng cho một đoạn văn: `platform/` NẰM TRONG CoreBase
 * (doc/kien-truc-core-module.md), nên câu này đi theo nền tảng sang sản phẩm thứ hai. Không lỗi
 * biên dịch, không lint, không pixel nào lệch — chỉ có một câu giới thiệu sai tên sản phẩm ngay
 * trang đích mặc định sau khi đăng nhập. Đó đúng là loại lỗi chỉ test mới bắt được.
 *
 * Tên trong token CỐ Ý khác tên thật của dự án, cùng lý do đã ghi ở `page-title.strategy.spec.ts`:
 * assert lên đúng tên thật thì phép kiểm vẫn xanh sau khi ai đó khai cứng chuỗi trở lại.
 */
describe('TrangChuPage — tên sản phẩm trong đoạn dẫn đến từ CORE_BRANDING', () => {
  const TEST_BRANDING = { name: 'Ứng Dụng Thử', shortName: 'UT' };

  /**
   * Ba đường dẫn khác hẳn dự án này, cùng thủ thuật với `TEST_BRANDING` ngay trên: assert lên
   * đúng chuỗi thật (`/doi-mat-khau`) thì phép kiểm vẫn xanh sau khi ai đó khai cứng trở lại.
   */
  const OTHER_PRODUCT_ROUTES: ICoreRoutes = {
    signIn: '/sign-in',
    changePassword: '/change-password',
    home: '/home',
  };

  async function render(): Promise<HTMLElement> {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        // `CurrentUserService` (nguồn của `fullName()`/`user()`) inject `HttpClient`. Không nạp
        // sẵn người dùng nào: trang vẫn render đủ phần đang đo, và `@if (user())` bỏ qua khối
        // thông tin tài khoản — đúng thứ test này KHÔNG quan tâm.
        provideHttpClient(),
        provideHttpClientTesting(),
        // Nút "Đổi mật khẩu" dùng `[routerLink]` nên vẫn cần Router để phân giải.
        provideRouter([{ path: '**', children: [] }]),
        provideCoreBranding(TEST_BRANDING),
        // Đích của nút đó nay đến từ CORE_ROUTES — token cố ý không có giá trị mặc định.
        provideCoreRoutes(OTHER_PRODUCT_ROUTES),
        provideTranslateService(),
      ],
    });
    await useTranslationsInTest();
    const fixture = TestBed.createComponent(TrangChuPage);
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  it('🛑 đoạn dẫn nhắc tên lấy từ token, không phải chuỗi khai cứng trong trang-chu.page.html', async () => {
    const lead = (await render()).querySelector('.lead') as HTMLElement;

    expect(lead).withContext('trang chủ phải có đoạn dẫn').not.toBeNull();
    expect(lead.textContent)
      .withContext('khai cứng tên ở đây = trang chủ sản phẩm thứ hai giới thiệu sản phẩm này')
      .toContain(TEST_BRANDING.name);
  });

  it('🛑 nút "Đổi mật khẩu" trỏ theo CORE_ROUTES.changePassword, không phải chuỗi trong template', async () => {
    const link = (await render()).querySelector('.title a.btn') as HTMLAnchorElement;

    expect(link).withContext('trang chủ phải có lối đi tới màn đổi mật khẩu').not.toBeNull();
    // Đọc `href` đã phân giải chứ không đọc thuộc tính `routerLink`: `href` là thứ trình duyệt
    // thật sự đi tới, còn thuộc tính thì đúng cả khi binding chưa chạy.
    expect(link.getAttribute('href'))
      .withContext('khai cứng "/doi-mat-khau" ở đây = sản phẩm thứ hai có một nút dẫn vào hư vô')
      .toBe(OTHER_PRODUCT_ROUTES.changePassword);
  });
});

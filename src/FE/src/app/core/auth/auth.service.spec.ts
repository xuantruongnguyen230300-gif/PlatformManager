import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { of } from 'rxjs';
import { AuthService } from './auth.service';
import { CurrentUserService } from './current-user.service';
import { MenuService } from '../menu/menu.service';
import { CsrfService } from '../http/csrf.service';
import { IApiResult } from '../http/api-result.model';
import { ICurrentUserDto } from './current-user.model';

function ok<T>(data: T): IApiResult<T> {
  return {
    data,
    message: null,
    status: 'SUCCESS',
    code: 'Success',
    businessCode: null,
    traceId: 'trace-auth',
    retryable: null,
    fields: null,
  };
}

const USER_B_DTO: ICurrentUserDto = {
  id: 'userB',
  userName: 'userB',
  email: null,
  fullName: 'User B',
  roles: ['User'],
  mustChangePassword: false,
};

/**
 * Trọng tâm: mọi cache gắn với PHIÊN phải bị xoá đúng lúc đổi phiên. Menu lọc theo role — giữ lại
 * bản của user cũ là rò rỉ thông tin phân quyền (finding B3, xem menu.service.ts).
 */
describe('AuthService — xoá cache theo phiên', () => {
  let auth: AuthService;
  let currentUser: CurrentUserService;
  let menu: MenuService;
  let csrf: CsrfService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection(), provideHttpClient(), provideHttpClientTesting()],
    });
    auth = TestBed.inject(AuthService);
    currentUser = TestBed.inject(CurrentUserService);
    menu = TestBed.inject(MenuService);
    csrf = TestBed.inject(CsrfService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('logout() xoá cache menu', () => {
    const invalidate = spyOn(menu, 'invalidate').and.callThrough();

    auth.logout().subscribe();
    httpMock.expectOne('/auth/logout').flush(ok(true));

    expect(invalidate).toHaveBeenCalled();
    expect(currentUser.isAuthenticated()).toBeFalse();
  });

  it('logout() vẫn xoá phiên client khi API lỗi 500', () => {
    const invalidate = spyOn(menu, 'invalidate').and.callThrough();
    let errored = false;

    auth.logout().subscribe({ error: () => (errored = true) });
    httpMock.expectOne('/auth/logout').flush(null, { status: 500, statusText: 'Server Error' });

    // Regression: việc dọn từng nằm trong `tap()` (chỉ chạy nhánh next) → logout hỏng là phiên
    // client còn nguyên trong khi người dùng tin rằng mình đã thoát. Nay nằm ở `finalize()`.
    expect(errored).toBeTrue();
    expect(invalidate).toHaveBeenCalled();
    expect(currentUser.isAuthenticated()).toBeFalse();
  });

  it('logout() vẫn xoá phiên client khi caller huỷ đăng ký giữa chừng', () => {
    const invalidate = spyOn(menu, 'invalidate').and.callThrough();

    const sub = auth.logout().subscribe();
    const req = httpMock.expectOne('/auth/logout');
    sub.unsubscribe();

    expect(req.cancelled).toBeTrue();
    expect(invalidate).toHaveBeenCalled();
    expect(currentUser.isAuthenticated()).toBeFalse();
  });

  it('login() xoá cache menu trước khi gán user mới', () => {
    const order: string[] = [];
    spyOn(menu, 'invalidate').and.callFake(() => order.push('invalidate'));
    spyOn(currentUser, 'setUser').and.callFake(() => order.push('setUser'));
    spyOn(csrf, 'primeToken').and.returnValue(of(undefined));

    auth.login('userB', 'pwd').subscribe();
    httpMock.expectOne('/auth/login').flush(ok(USER_B_DTO));

    expect(order).toEqual(['invalidate', 'setUser']);
  });

  /**
   * Gap tìm thấy qua core-reviewer (2026-08-24): token CSRF lấy lúc ANONYMOUS gắn với danh tính
   * tại thời điểm phát hành — dùng lại sau khi đăng nhập (đổi danh tính) sẽ bị 403 "meant for a
   * different claims-based user" (xem doc/contracts/auth.md §CSRF). Không mồi lại thì luồng bắt
   * buộc đổi mật khẩu lần đầu (`mustChangePassword` → `POST /auth/change-password`, áp dụng cho
   * gần như mọi tài khoản mới) bị chặn CSRF ngay sau khi vừa đăng nhập thành công.
   */
  it('login() mồi lại cookie CSRF (primeToken()) NGAY sau khi gán user mới', () => {
    const order: string[] = [];
    spyOn(currentUser, 'setUser').and.callFake(() => order.push('setUser'));
    spyOn(csrf, 'primeToken').and.callFake(() => {
      order.push('primeToken');
      return of(undefined);
    });

    auth.login('userB', 'pwd').subscribe();
    httpMock.expectOne('/auth/login').flush(ok(USER_B_DTO));

    expect(order).toEqual(['setUser', 'primeToken']);
  });
});

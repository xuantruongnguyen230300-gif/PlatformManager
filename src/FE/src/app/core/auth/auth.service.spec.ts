import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { provideTranslateService } from '@ngx-translate/core';
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
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting(),
        // `AuthService` → `MenuService` → `TranslateService` (câu báo "không tải được menu",
        // thêm 2026-09-08). Không cần nạp bảng dịch: spec này không đọc câu nào, chỉ cần DI
        // phân giải được.
        provideTranslateService(),
      ],
    });
    auth = TestBed.inject(AuthService);
    currentUser = TestBed.inject(CurrentUserService);
    menu = TestBed.inject(MenuService);
    csrf = TestBed.inject(CsrfService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('logout() xoá cache menu', () => {
    spyOn(csrf, 'primeToken').and.returnValue(of(undefined));
    const invalidate = spyOn(menu, 'invalidate').and.callThrough();

    auth.logout().subscribe();
    httpMock.expectOne('/auth/logout').flush(ok(true));

    expect(invalidate).toHaveBeenCalled();
    expect(currentUser.isAuthenticated()).toBeFalse();
  });

  it('logout() vẫn xoá phiên client khi API lỗi 500', () => {
    spyOn(csrf, 'primeToken').and.returnValue(of(undefined));
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
    spyOn(csrf, 'primeToken').and.returnValue(of(undefined));
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

  /**
   * Bug thật trên trình duyệt (2026-09-08): đăng xuất rồi đăng nhập lại trong cùng một lần tải
   * trang luôn bị 403 `AUTH.CSRF_REJECTED`. Ràng buộc "request-token gắn với danh tính lúc phát
   * hành" đối xứng theo CẢ HAI chiều đổi danh tính, nhưng chỉ chiều login (test ngay trên) được
   * cài và được test — chiều logout thì không. Cookie giữ token của phiên vừa thoát, còn
   * `provideCsrfInit()` chỉ chạy lúc bootstrap nên không có gì mồi lại (topbar điều hướng bằng
   * router, không tải lại trang). Xem doc/contracts/auth.md §CSRF bước 4.
   */
  it('logout() mồi lại cookie CSRF (primeToken()) sau khi xoá phiên client', () => {
    const order: string[] = [];
    spyOn(currentUser, 'clear').and.callFake(() => order.push('clear'));
    spyOn(csrf, 'primeToken').and.callFake(() => {
      order.push('primeToken');
      return of(undefined);
    });

    auth.logout().subscribe();
    httpMock.expectOne('/auth/logout').flush(ok(true));

    expect(order).toEqual(['clear', 'primeToken']);
  });

  it('logout() vẫn mồi lại cookie CSRF khi API lỗi 500', () => {
    const primeToken = spyOn(csrf, 'primeToken').and.returnValue(of(undefined));

    auth.logout().subscribe({ error: () => undefined });
    httpMock.expectOne('/auth/logout').flush(null, { status: 500, statusText: 'Server Error' });

    // Cùng lý do với `finalize()` của 2 dòng dọn phiên: phiên client đã bị xoá kể cả khi logout
    // hỏng, nên cookie CSRF phải theo — nếu không, người dùng kẹt ở màn đăng nhập không vào lại
    // được, đúng trạng thái tệ nhất.
    expect(primeToken).toHaveBeenCalled();
  });

  it('logout() vẫn mồi lại cookie CSRF khi caller huỷ đăng ký giữa chừng', () => {
    const primeToken = spyOn(csrf, 'primeToken').and.returnValue(of(undefined));

    const sub = auth.logout().subscribe();
    const req = httpMock.expectOne('/auth/logout');
    sub.unsubscribe();

    expect(req.cancelled).toBeTrue();
    expect(primeToken).toHaveBeenCalled();
  });
});

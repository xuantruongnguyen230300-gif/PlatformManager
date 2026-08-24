import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { Sidebar } from './sidebar';
import { AuthService } from '../../../core/auth/auth.service';
import { CurrentUserService } from '../../../core/auth/current-user.service';
import { IApiResult } from '../../../core/http/api-result.model';
import { ICurrentUserDto } from '../../../core/auth/current-user.model';
import { IMenuItemDto } from '../../../core/menu/menu-item.model';

function envelope<T>(data: T): IApiResult<T> {
  return {
    data,
    message: null,
    status: 'SUCCESS',
    code: 'Success',
    businessCode: null,
    traceId: 'trace-scenario',
    retryable: null,
    fields: null,
  };
}

const MENU_DTO: IMenuItemDto[] = [
  { id: 'm1', parentId: null, code: 'dashboard', label: 'Dashboard', icon: null, route: '/dashboard', displayOrder: 1 },
  { id: 'm2', parentId: null, code: 'danh-muc', label: 'Danh mục', icon: null, route: '/danh-muc/dti', displayOrder: 2 },
];

function userDto(id: string, roles: string[]): ICurrentUserDto {
  return { id, userName: id, email: null, fullName: id, roles, mustChangePassword: false };
}

/**
 * KỊCH BẢN ĐO cho finding B3 (cache `GET /meta/menu`) — số liệu ghi ở
 * doc/huong_dan/wiki-core/be/11-performance-caching.md §7.
 *
 * Đo bằng ĐẾM SỐ REQUEST trên một kịch bản điều hướng xác định, không đo milli-giây (đúng tinh
 * thần src/BE/.claude/rules/performance.md §Đo: con số đếm được, lặp lại được). Harness là chính
 * `Sidebar` thật + `AuthService` thật, vì cơ chế sinh request nằm ở vòng đời component: app-shell
 * (`app.html`) bọc trong `@if (showShell())`, nên MỖI lần vào/ra route `noShell`
 * (`login`, `doi-mat-khau`) là một lần `Sidebar` bị huỷ rồi dựng lại → 1 lần gọi `getMenu()`.
 *
 * Test này vừa là số đo vừa là chốt chặn hồi quy: nếu ai đó bỏ cache, con số sẽ đổi và test đỏ.
 */
describe('Kịch bản đo B3 — số request GET /meta/menu trong 1 phiên làm việc', () => {
  let httpMock: HttpTestingController;
  let auth: AuthService;
  let currentUser: CurrentUserService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    httpMock = TestBed.inject(HttpTestingController);
    auth = TestBed.inject(AuthService);
    currentUser = TestBed.inject(CurrentUserService);
  });

  afterEach(() => httpMock.verify());

  /** Vào 1 route CÓ shell sau khi đang ở route `noShell` → app-shell dựng lại `Sidebar`. */
  function mountShell(): ComponentFixture<Sidebar> {
    const fixture = TestBed.createComponent(Sidebar);
    fixture.detectChanges();
    return fixture;
  }

  /** Trả về số request `/meta/menu` phát sinh kể từ lần gọi trước (và flush hết). */
  function drainMenuRequests(): number {
    const requests = httpMock.match('/meta/menu');
    requests.forEach((req) => req.flush(envelope(MENU_DTO)));
    return requests.length;
  }

  function login(id: string, roles: string[]): void {
    auth.login(id, 'pwd').subscribe();
    httpMock.expectOne('/auth/login').flush(envelope(userDto(id, roles)));
    // AuthService.login() mồi lại cookie CSRF ngay sau khi gán user mới (xem
    // core/auth/auth.service.ts) — phải flush cùng lúc, không thì httpMock.verify() ở afterEach
    // sẽ báo request treo.
    httpMock.expectOne('/antiforgery/token').flush({ token: 'fake-csrf-token' });
  }

  function logout(): void {
    auth.logout().subscribe();
    httpMock.expectOne('/auth/logout').flush(envelope(true));
  }

  it('S1: mở app → dashboard → danh mục → dashboard → đổi mật khẩu → dashboard → logout → login user khác → dashboard', () => {
    const steps: Record<string, number> = {};

    // B1. Đã đăng nhập sẵn (`/auth/me` lúc khởi động), vào dashboard → shell dựng lần 1.
    currentUser.setUser({
      Id: 'userA',
      UserName: 'userA',
      Email: null,
      FullName: 'User A',
      Roles: ['Admin'],
      MustChangePassword: false,
    });
    let shell = mountShell();
    steps['B1 shell dựng lần 1'] = drainMenuRequests();

    // B2. dashboard → danh mục DTI → dashboard: đều là route CÓ shell nên `Sidebar` không dựng
    // lại (chỉ `<router-outlet>` đổi) → không phát sinh request nào.
    steps['B2 điều hướng trong shell (2 lần)'] = drainMenuRequests();

    // B3. Vào "Đổi mật khẩu" (`data.noShell`) rồi quay lại dashboard → shell dựng lần 2.
    shell.destroy();
    shell = mountShell();
    steps['B3 shell dựng lần 2 (cùng phiên)'] = drainMenuRequests();

    // B4. Logout → đăng nhập tài khoản KHÁC → dashboard → shell dựng lần 3.
    shell.destroy();
    logout();
    login('userB', ['User']);
    shell = mountShell();
    steps['B4 shell dựng lần 3 (phiên MỚI)'] = drainMenuRequests();

    shell.destroy();

    // Số đo "sau" — ghi vào 11-performance-caching.md §7.
    expect(steps).toEqual({
      'B1 shell dựng lần 1': 1,
      'B2 điều hướng trong shell (2 lần)': 0,
      'B3 shell dựng lần 2 (cùng phiên)': 0,
      'B4 shell dựng lần 3 (phiên MỚI)': 1,
    });

    const total = Object.values(steps).reduce((sum, n) => sum + n, 0);
    expect(total).toBe(2);
  });

  it('S2: cùng 1 phiên, app-shell dựng lại 5 lần (ra/vào route noShell) → 1 request', () => {
    currentUser.setUser({
      Id: 'userA',
      UserName: 'userA',
      Email: null,
      FullName: 'User A',
      Roles: ['Admin'],
      MustChangePassword: false,
    });

    let calls = 0;
    for (let i = 0; i < 5; i++) {
      const shell = mountShell();
      calls += drainMenuRequests();
      shell.destroy();
    }

    // Điểm mấu chốt của cache: số request KHÔNG còn tỉ lệ thuận với số lần dựng shell.
    expect(calls).toBe(1);
  });
});

import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import {
  ActivatedRouteSnapshot,
  CanActivateFn,
  Router,
  RouterStateSnapshot,
  UrlTree,
  provideRouter,
} from '@angular/router';
import { authGuard } from './auth.guard';
import { mustChangePasswordGuard } from './must-change-password.guard';
import { adminGuard, roleGuard, superAdminGuard } from './role.guard';
import { CurrentUserService } from './current-user.service';
import { ICurrentUser } from './current-user.model';
import { ICoreRoutes, provideCoreRoutes } from '../config/core-routes';

/**
 * Bản sao ba đường dẫn mà `app.config.ts` (`APP_CORE_ROUTES`) bơm vào — CỐ Ý chép chứ không import
 * từ `app.config.ts`: file này thuộc `core/`, mà `core/` là tầng đáy, không được biết app cụ thể
 * nào đang dùng nó. Nhờ vậy các assert bên dưới giữ nguyên chuỗi cũ, tức chúng vẫn đo đúng "hành
 * vi KHÔNG ĐỔI" sau đợt tách đường dẫn 2026-09-02.
 *
 * Việc "app khai đúng ba đường dẫn có thật trong bảng route" là chuyện của app, và được khoá riêng
 * ở `src/FE/src/app/app.routes.spec.ts`.
 */
const APP_ROUTES_TODAY: ICoreRoutes = {
  signIn: '/dang-nhap',
  changePassword: '/doi-mat-khau',
  home: '/trang-chu',
};

function user(roles: string[], mustChangePassword = false): ICurrentUser {
  return {
    Id: 'u1',
    UserName: 'u1',
    Email: null,
    FullName: 'U1',
    Roles: roles,
    MustChangePassword: mustChangePassword,
  };
}

/**
 * Guard tách làm 3 (doc/huong_dan/quy-uoc/fe-routing-guard.md §3–§5). Trọng tâm test: mỗi guard
 * chỉ làm ĐÚNG việc của nó, và cả ba trả `UrlTree` chứ không gọi `router.navigate()` — gọi
 * `navigate()` bên trong guard tạo hai lần điều hướng chồng nhau (§3).
 */
describe('Guard — authGuard / mustChangePasswordGuard / roleGuard', () => {
  let currentUser: CurrentUserService;
  let router: Router;

  function run(guard: CanActivateFn, url = '/trang-chu'): boolean | UrlTree {
    const state = { url } as RouterStateSnapshot;
    const route = {} as ActivatedRouteSnapshot;
    return TestBed.runInInjectionContext(() => guard(route, state)) as boolean | UrlTree;
  }

  beforeEach(() => {
    TestBed.configureTestingModule({
      // `CurrentUserService` inject `HttpClient` (cho `load()`) — test không gọi `load()`, chỉ
      // `setUser()`, nhưng vẫn phải cấp provider để DI dựng được service.
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideCoreRoutes(APP_ROUTES_TODAY),
      ],
    });
    currentUser = TestBed.inject(CurrentUserService);
    router = TestBed.inject(Router);
  });

  describe('authGuard', () => {
    it('chưa đăng nhập → UrlTree /dang-nhap kèm returnUrl', () => {
      const navigate = spyOn(router, 'navigate');

      const result = run(authGuard, '/quan-tri/nguoi-dung');

      expect(result instanceof UrlTree).toBeTrue();
      expect(router.serializeUrl(result as UrlTree)).toBe('/dang-nhap?returnUrl=%2Fquan-tri%2Fnguoi-dung');
      expect(navigate).not.toHaveBeenCalled();
    });

    it('đã đăng nhập → cho qua', () => {
      currentUser.setUser(user(['User']));
      expect(run(authGuard)).toBeTrue();
    });

    it('KHÔNG tự kiểm mustChangePassword — việc đó của guard khác', () => {
      currentUser.setUser(user(['User'], true));
      expect(run(authGuard)).toBeTrue();
    });
  });

  describe('mustChangePasswordGuard', () => {
    it('mustChangePassword=true → UrlTree /doi-mat-khau', () => {
      currentUser.setUser(user(['User'], true));

      const result = run(mustChangePasswordGuard);

      expect(result instanceof UrlTree).toBeTrue();
      expect(router.serializeUrl(result as UrlTree)).toBe('/doi-mat-khau');
    });

    it('mustChangePassword=false → cho qua', () => {
      currentUser.setUser(user(['User']));
      expect(run(mustChangePasswordGuard)).toBeTrue();
    });

    it('sau markPasswordChanged() → cho qua ngay, không cần đăng nhập lại (§4 điểm 3)', () => {
      currentUser.setUser(user(['User'], true));
      currentUser.markPasswordChanged();
      expect(run(mustChangePasswordGuard)).toBeTrue();
    });
  });

  describe('roleGuard', () => {
    it('đủ 1 trong các role → cho qua', () => {
      currentUser.setUser(user(['Admin']));
      expect(run(roleGuard('Admin', 'SuperAdmin'))).toBeTrue();
    });

    it('thiếu quyền → UrlTree /trang-chu, KHÔNG có trang 403 (§5)', () => {
      currentUser.setUser(user(['User']));

      const result = run(roleGuard('SuperAdmin'));

      expect(result instanceof UrlTree).toBeTrue();
      expect(router.serializeUrl(result as UrlTree)).toBe('/trang-chu');
    });

    it('adminGuard nhận cả Admin lẫn SuperAdmin', () => {
      currentUser.setUser(user(['Admin']));
      expect(run(adminGuard)).toBeTrue();
      currentUser.setUser(user(['SuperAdmin']));
      expect(run(adminGuard)).toBeTrue();
    });

    it('superAdminGuard CHẶN Admin — chống leo thang quyền qua UI (§5)', () => {
      currentUser.setUser(user(['Admin']));
      expect(run(superAdminGuard) instanceof UrlTree).toBeTrue();
    });

    it('superAdminGuard cho SuperAdmin qua', () => {
      currentUser.setUser(user(['SuperAdmin']));
      expect(run(superAdminGuard)).toBeTrue();
    });
  });
});

/**
 * Đây là test CHỨNG MINH đợt tách 2026-09-02 thật sự có tác dụng, chứ không chỉ đổi chỗ chuỗi.
 *
 * Ba đường dẫn dùng ở đây KHÁC HẲN đường dẫn của dự án này (`/sign-in`, `/change-password`,
 * `/home`) — đúng thứ một sản phẩm thứ hai dựng trên CoreBase sẽ khai. Guard nào còn khai cứng
 * `/dang-nhap`, `/doi-mat-khau` hay `/trang-chu` sẽ ĐỎ ở đây và chỉ ở đây: bộ test cũ vẫn xanh
 * hoàn toàn vì nó chỉ hỏi "có về đúng chỗ của DỰ ÁN NÀY không".
 *
 * Vì sao phải tách: `core/` là CoreBase dùng lại cho sản phẩm khác
 * (doc/kien-truc-core-module.md). Đường dẫn khai cứng trong `core/` nghĩa là muốn dùng lại thì
 * phải sửa vào trong `core/`. Xem core/config/core-routes.ts.
 */
describe('Guard — đường dẫn đến từ CORE_ROUTES, KHÔNG khai cứng trong core/', () => {
  /** Đường dẫn của một sản phẩm giả định khác — không dòng nào trong `core/` được biết trước. */
  const OTHER_PRODUCT_ROUTES: ICoreRoutes = {
    signIn: '/sign-in',
    changePassword: '/change-password',
    home: '/home',
  };

  let currentUser: CurrentUserService;
  let router: Router;

  function run(guard: CanActivateFn, url = '/anywhere'): boolean | UrlTree {
    const state = { url } as RouterStateSnapshot;
    const route = {} as ActivatedRouteSnapshot;
    return TestBed.runInInjectionContext(() => guard(route, state)) as boolean | UrlTree;
  }

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideCoreRoutes(OTHER_PRODUCT_ROUTES),
      ],
    });
    currentUser = TestBed.inject(CurrentUserService);
    router = TestBed.inject(Router);
  });

  it('authGuard → CORE_ROUTES.signIn (kèm returnUrl), không phải đường dẫn của dự án này', () => {
    const result = run(authGuard, '/bat-ky');

    expect(router.serializeUrl(result as UrlTree)).toBe('/sign-in?returnUrl=%2Fbat-ky');
  });

  it('mustChangePasswordGuard → CORE_ROUTES.changePassword', () => {
    currentUser.setUser(user(['User'], true));

    const result = run(mustChangePasswordGuard);

    expect(router.serializeUrl(result as UrlTree)).toBe('/change-password');
  });

  it('roleGuard thiếu quyền → CORE_ROUTES.home', () => {
    currentUser.setUser(user(['User']));

    const result = run(roleGuard('SuperAdmin'));

    expect(router.serializeUrl(result as UrlTree)).toBe('/home');
  });

  it('adminGuard/superAdminGuard dựng sẵn ở tầng module cũng đọc token lúc CHẠY, không lúc khai báo', () => {
    // `adminGuard`/`superAdminGuard` được tạo 1 lần khi module nạp (`roleGuard('Admin', …)`),
    // trước khi mọi TestBed tồn tại. Nếu `inject(CORE_ROUTES)` bị gọi ở thân factory thay vì trong
    // guard trả về, hai hằng số này sẽ đóng băng đường dẫn của lần nạp đầu tiên.
    currentUser.setUser(user(['User']));

    expect(router.serializeUrl(run(adminGuard) as UrlTree)).toBe('/home');
    expect(router.serializeUrl(run(superAdminGuard) as UrlTree)).toBe('/home');
  });
});

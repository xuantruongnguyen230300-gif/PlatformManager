import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { CORE_ROUTES } from '../config/core-routes';
import { CurrentUserService } from './current-user.service';

/**
 * Chỉ trả lời MỘT câu hỏi: "đã đăng nhập chưa?". Không kiểm role, không kiểm cờ buộc đổi mật
 * khẩu — hai việc đó thuộc `roleGuard` (role.guard.ts) và `mustChangePasswordGuard`
 * (must-change-password.guard.ts). Xem doc/huong_dan/quy-uoc/fe-routing-guard.md §3.
 *
 * Trả `UrlTree` chứ KHÔNG gọi `router.navigate()` — `UrlTree` để Angular huỷ điều hướng cũ rồi
 * chuyển hướng trong MỘT chu kỳ; `navigate()` bên trong guard tạo hai lần điều hướng chồng nhau.
 *
 * `returnUrl` đi kèm để màn đăng nhập đưa user về đúng chỗ họ định vào sau khi đăng nhập.
 *
 * Đường dẫn màn đăng nhập đến từ `CORE_ROUTES` (core/config/core-routes.ts), KHÔNG khai cứng ở
 * đây — `core/` giữ luật chuyển hướng, app cung cấp đường dẫn.
 */
export const authGuard: CanActivateFn = (_route, state) => {
  const currentUser = inject(CurrentUserService);
  const router = inject(Router);
  const coreRoutes = inject(CORE_ROUTES);

  return currentUser.isAuthenticated()
    ? true
    : router.createUrlTree([coreRoutes.signIn], { queryParams: { returnUrl: state.url } });
};

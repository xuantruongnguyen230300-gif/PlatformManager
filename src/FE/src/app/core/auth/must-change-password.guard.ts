import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { CORE_ROUTES } from '../config/core-routes';
import { CurrentUserService } from './current-user.service';

/**
 * `GET /api/auth/me` trả `mustChangePassword` (doc/contracts/auth.md); user do quản trị viên tạo
 * mang giá trị `true`. Luật: khi cờ này bật, MỌI route khác màn đổi mật khẩu đều bị chặn —
 * `authGuard` KHÔNG chặn được vì người đó ĐÃ đăng nhập.
 * Xem doc/huong_dan/quy-uoc/fe-routing-guard.md §4.
 *
 * 🛑 KHÔNG gắn guard này cho chính route đổi mật khẩu (`CORE_ROUTES.changePassword`) — gắn vào là
 * VÒNG LẶP REDIRECT VÔ HẠN, và không có gì trong build/lint/test bắt được. Route đó chỉ cần
 * `authGuard`. Ràng buộc này được khoá lại bằng máy ở src/FE/src/app/app.routes.spec.ts.
 *
 * Đường dẫn đến từ `CORE_ROUTES` (core/config/core-routes.ts), KHÔNG khai cứng ở đây — `core/`
 * giữ luật chuyển hướng, app cung cấp đường dẫn.
 */
export const mustChangePasswordGuard: CanActivateFn = () => {
  const currentUser = inject(CurrentUserService);
  const router = inject(Router);
  const coreRoutes = inject(CORE_ROUTES);

  return currentUser.mustChangePassword() ? router.createUrlTree([coreRoutes.changePassword]) : true;
};

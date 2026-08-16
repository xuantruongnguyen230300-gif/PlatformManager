import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { CurrentUserService } from './current-user.service';

/**
 * Gate "Quản trị hệ thống > Người dùng" — `Admin` HOẶC `SuperAdmin` (khớp BE
 * `[Authorize(Roles="SuperAdmin,Admin")]`, xem doc/contracts/users.md). Luôn đặt SAU `authGuard`
 * trong `canActivate` — giả định user đã đăng nhập khi guard này chạy.
 */
export const adminGuard: CanActivateFn = () => {
  const currentUser = inject(CurrentUserService);
  const router = inject(Router);

  if (currentUser.hasRole('Admin') || currentUser.hasRole('SuperAdmin')) {
    return true;
  }
  return router.createUrlTree(['/dashboard']);
};

import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { CurrentUserService } from './current-user.service';

/**
 * Gate "Quản trị hệ thống > Phân quyền" — CHỈ `SuperAdmin`, kể cả `Admin` cũng bị chặn (khớp BE
 * `[Authorize(Roles="SuperAdmin")]`, xem doc/contracts/permissions.md — tránh leo thang quyền
 * qua UI, đã CHỐT ở doc/ke-hoach-xay-lai-corebase.md). Luôn đặt SAU `authGuard`.
 */
export const superAdminGuard: CanActivateFn = () => {
  const currentUser = inject(CurrentUserService);
  const router = inject(Router);

  if (currentUser.hasRole('SuperAdmin')) {
    return true;
  }
  return router.createUrlTree(['/dashboard']);
};

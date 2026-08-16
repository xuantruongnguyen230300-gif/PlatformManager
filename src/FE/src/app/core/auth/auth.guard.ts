import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { CurrentUserService } from './current-user.service';

const CHANGE_PASSWORD_PATH = '/doi-mat-khau';

/**
 * Chặn route cần đăng nhập + tự động ép về `/doi-mat-khau` khi `mustChangePassword===true` —
 * gộp 2 việc vào 1 guard (thay vì tách riêng, xem doc/ke-hoach-xay-lai-corebase.md §F3) vì cả
 * hai đều cần chạy TRƯỚC MỌI route đã đăng nhập, kể cả chính route `/doi-mat-khau` (guard đó tự
 * biết không redirect vòng lặp vào chính nó qua so khớp `state.url`).
 *
 * Áp dụng cho MỌI route cần đăng nhập, kể cả `/doi-mat-khau` — route đó KHÔNG được bỏ guard này
 * (vẫn cần chặn user chưa đăng nhập truy cập thẳng `/doi-mat-khau`).
 */
export const authGuard: CanActivateFn = (_route, state) => {
  const currentUser = inject(CurrentUserService);
  const router = inject(Router);

  if (!currentUser.isAuthenticated()) {
    return router.createUrlTree(['/dang-nhap'], { queryParams: { returnUrl: state.url } });
  }

  const isChangePasswordRoute = state.url.startsWith(CHANGE_PASSWORD_PATH);
  if (currentUser.mustChangePassword() && !isChangePasswordRoute) {
    return router.createUrlTree([CHANGE_PASSWORD_PATH]);
  }

  return true;
};

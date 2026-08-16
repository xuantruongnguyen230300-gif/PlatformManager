import { Routes } from '@angular/router';
import { authGuard } from '../../core/auth/auth.guard';

// `authGuard` áp cho CHÍNH route này (không bỏ) — vẫn cần chặn user chưa đăng nhập truy cập
// thẳng /doi-mat-khau; guard tự nhận diện route này qua state.url để không redirect vòng lặp.
export const DOI_MAT_KHAU_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./pages/doi-mat-khau/doi-mat-khau.page').then((m) => m.DoiMatKhauPage),
    data: { title: 'Đổi mật khẩu', noShell: true },
    canActivate: [authGuard],
  },
];

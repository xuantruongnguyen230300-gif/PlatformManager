import { Routes } from '@angular/router';
import { authGuard } from '../../core/auth/auth.guard';
import { mustChangePasswordGuard } from '../../core/auth/must-change-password.guard';

// Trang đích sau đăng nhập và là đích của mọi redirect "về chỗ an toàn" (role.guard.ts khi thiếu
// quyền, doi-mat-khau sau khi đổi xong, `**` khi gõ URL lạ). Vì vậy nó KHÔNG được mang guard theo
// vai trò nào — người dùng bị đá về đây phải luôn vào được, nếu không sẽ thành vòng lặp redirect.
//
// Thứ tự guard: authGuard → mustChangePasswordGuard (fe-routing-guard.md §6). Có shell (sidebar +
// topbar) nên KHÔNG khai `noShell`.
export const TRANG_CHU_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./pages/trang-chu/trang-chu.page').then((m) => m.TrangChuPage),
    title: 'trang-chu.routeTitle',
    canActivate: [authGuard, mustChangePasswordGuard],
  },
];

import { Routes } from '@angular/router';

// Route PUBLIC — không có authGuard (đây chính là nơi user CHƯA đăng nhập đến). `noShell: true`
// để `App` ẩn sidebar/topbar, xem app.ts.
export const LOGIN_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./pages/login/login.page').then((m) => m.LoginPage),
    data: { title: 'Đăng nhập', noShell: true },
  },
];

import { Routes } from '@angular/router';
import { authGuard } from '../../core/auth/auth.guard';
import { mustChangePasswordGuard } from '../../core/auth/must-change-password.guard';
import { adminGuard } from '../../core/auth/role.guard';

// Thứ tự guard: authGuard → mustChangePasswordGuard → roleGuard
// (doc/huong_dan/quy-uoc/fe-routing-guard.md §6) — đảo roleGuard lên trước sẽ đá người CHƯA đăng
// nhập về /trang-chu thay vì màn đăng nhập.
export const QUAN_TRI_NGUOI_DUNG_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/quan-tri-nguoi-dung/quan-tri-nguoi-dung.page').then((m) => m.QuanTriNguoiDungPage),
    title: 'quan-tri-nguoi-dung.routeTitle',
    canActivate: [authGuard, mustChangePasswordGuard, adminGuard],
  },
];

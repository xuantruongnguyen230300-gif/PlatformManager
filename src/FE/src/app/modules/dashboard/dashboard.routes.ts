import { Routes } from '@angular/router';
import { authGuard } from '../../core/auth/auth.guard';

// Mở cho MỌI user đã đăng nhập, không cần role riêng (đúng doc/contracts/meta-menu.md — Dashboard
// không có SysMenuRole) — chỉ cần authGuard.
export const DASHBOARD_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./pages/dashboard/dashboard.page').then((m) => m.DashboardPage),
    data: { title: 'Dashboard' },
    canActivate: [authGuard],
  },
];

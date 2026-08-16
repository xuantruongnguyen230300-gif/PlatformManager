import { Routes } from '@angular/router';
import { authGuard } from '../../core/auth/auth.guard';

// Mở cho MỌI user đã đăng nhập, không cần role riêng (đúng doc/contracts/meta-menu.md — Danh mục
// DTI không có SysMenuRole) — chỉ cần authGuard.
export const DANH_MUC_DTI_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./pages/danh-muc-dti/danh-muc-dti.page').then((m) => m.DanhMucDtiPage),
    data: { title: 'Danh mục' },
    canActivate: [authGuard],
  },
];

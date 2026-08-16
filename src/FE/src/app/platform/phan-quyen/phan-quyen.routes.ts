import { Routes } from '@angular/router';
import { authGuard } from '../../core/auth/auth.guard';
import { superAdminGuard } from '../../core/auth/super-admin.guard';

export const PHAN_QUYEN_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./pages/phan-quyen/phan-quyen.page').then((m) => m.PhanQuyenPage),
    data: { title: 'Phân quyền' },
    canActivate: [authGuard, superAdminGuard],
  },
];

import { Routes } from '@angular/router';
import { authGuard } from '../../core/auth/auth.guard';
import { adminGuard } from '../../core/auth/admin.guard';

export const QUAN_TRI_NGUOI_DUNG_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/quan-tri-nguoi-dung/quan-tri-nguoi-dung.page').then((m) => m.QuanTriNguoiDungPage),
    data: { title: 'Người dùng hệ thống' },
    canActivate: [authGuard, adminGuard],
  },
];

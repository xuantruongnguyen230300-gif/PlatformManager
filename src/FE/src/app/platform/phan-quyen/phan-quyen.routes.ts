import { Routes } from '@angular/router';
import { authGuard } from '../../core/auth/auth.guard';
import { mustChangePasswordGuard } from '../../core/auth/must-change-password.guard';
import { superAdminGuard } from '../../core/auth/role.guard';
import { unsavedChangesGuard } from '../../core/guards/unsaved-changes.guard';

// `superAdminGuard` (KHÔNG phải `adminGuard`) — `Admin` cũng bị chặn khỏi màn phân quyền, chống
// leo thang quyền qua UI. Thứ tự guard theo doc/huong_dan/quy-uoc/fe-routing-guard.md §5–§6.
export const PHAN_QUYEN_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./pages/phan-quyen/phan-quyen.page').then((m) => m.PhanQuyenPage),
    title: 'phan-quyen.routeTitle',
    canActivate: [authGuard, mustChangePasswordGuard, superAdminGuard],
    // Màn này lưu bằng GHI ĐÈ TOÀN BỘ và giữ thay đổi trong bộ nhớ cho tới lúc bấm Lưu — rời
    // trang im lặng là mất trắng. Xem fe/09-forms-validation.md §"Form dirty + điều hướng đi".
    canDeactivate: [unsavedChangesGuard],
  },
];

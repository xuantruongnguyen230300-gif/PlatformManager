import { Routes } from '@angular/router';

// Guard đặt TRONG route của từng feature (dashboard.routes.ts/danh-muc-dti.routes.ts/...), không
// cấu hình rời rạc ở đây — đúng quy ước src/FE/.claude/docs/architecture.md §Routing.
//
// `platform/` = màn hình Core dùng lại được cho mọi sản phẩm (đăng nhập, đổi mật khẩu, quản trị
// người dùng, phân quyền); `modules/` = module NGHIỆP VỤ (dashboard, danh-muc-dti) — xem
// doc/kien-truc-core-module.md.
export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
  {
    path: 'dang-nhap',
    loadChildren: () => import('./platform/login/login.routes').then((m) => m.LOGIN_ROUTES),
  },
  {
    path: 'doi-mat-khau',
    loadChildren: () => import('./platform/doi-mat-khau/doi-mat-khau.routes').then((m) => m.DOI_MAT_KHAU_ROUTES),
  },
  {
    path: 'dashboard',
    loadChildren: () => import('./modules/dashboard/dashboard.routes').then((m) => m.DASHBOARD_ROUTES),
  },
  {
    path: 'danh-muc/dti',
    loadChildren: () =>
      import('./modules/danh-muc-dti/danh-muc-dti.routes').then((m) => m.DANH_MUC_DTI_ROUTES),
  },
  {
    path: 'quan-tri/nguoi-dung',
    loadChildren: () =>
      import('./platform/quan-tri-nguoi-dung/quan-tri-nguoi-dung.routes').then((m) => m.QUAN_TRI_NGUOI_DUNG_ROUTES),
  },
  {
    path: 'quan-tri/phan-quyen',
    loadChildren: () => import('./platform/phan-quyen/phan-quyen.routes').then((m) => m.PHAN_QUYEN_ROUTES),
  },
  { path: '**', redirectTo: 'dashboard' },
];

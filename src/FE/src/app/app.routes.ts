import { Routes } from '@angular/router';

// Guard đặt TRONG route của từng feature (trang-chu.routes.ts/login.routes.ts/...), không
// cấu hình rời rạc ở đây — đúng quy ước doc/huong_dan/quy-uoc/fe-routing-guard.md §2. File này
// CHỈ `loadChildren`, không `loadComponent`, không import component nào: nó là bảng mục lục.
//
// `platform/` = màn hình Core dùng lại được cho mọi sản phẩm (đăng nhập, đổi mật khẩu, quản trị
// người dùng, phân quyền); `modules/` = module NGHIỆP VỤ — xem doc/kien-truc-core-module.md.
// Hiện KHÔNG có module nghiệp vụ nào (DtiWeekly gỡ 2026-08-29 để xây lại), nên `modules/` trống
// và `/trang-chu` là trang đích mặc định.
export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'trang-chu' },
  {
    path: 'dang-nhap',
    loadChildren: () => import('./platform/login/login.routes').then((m) => m.LOGIN_ROUTES),
  },
  {
    path: 'doi-mat-khau',
    loadChildren: () => import('./platform/doi-mat-khau/doi-mat-khau.routes').then((m) => m.DOI_MAT_KHAU_ROUTES),
  },
  {
    path: 'trang-chu',
    loadChildren: () => import('./platform/trang-chu/trang-chu.routes').then((m) => m.TRANG_CHU_ROUTES),
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
  { path: '**', redirectTo: 'trang-chu' },
];

import { Routes } from '@angular/router';

// Guard đặt TRONG route của từng feature (trang-chu.routes.ts/login.routes.ts/...), không
// cấu hình rời rạc ở đây — đúng quy ước doc/huong_dan/quy-uoc/fe-routing-guard.md §2. File này
// CHỈ `loadChildren`, không `loadComponent`, không import component nào: nó là bảng mục lục.
//
// `platform/` = màn hình Core dùng lại được cho mọi sản phẩm (đăng nhập, đổi mật khẩu, quản trị
// người dùng, phân quyền); `modules/` = module NGHIỆP VỤ — xem doc/kien-truc-core-module.md.
//
// `/trang-chu` VẪN là trang đích mặc định (đích của `''`, của `**`, và của mọi chuyển hướng "về
// chỗ an toàn"). Dashboard DTI sẽ THAY nó theo chốt Q3 (spec/dashboard-dti/business-rules.md),
// nhưng `modules/dashboard/` hôm nay mới là khung rỗng — hoán đổi bến an toàn của cả app để lấy
// một trang trắng là làm hỏng đúng thứ không được hỏng. Lý do đầy đủ ghi tại chỗ trong
// `modules/dashboard/dashboard.routes.ts`.
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
  // Màn NGHIỆP VỤ đầu tiên — đường dẫn `/danh-muc/dti` là chốt Q33 (spec/danh-muc-dti/ui-spec.md §2).
  {
    path: 'danh-muc/dti',
    loadChildren: () =>
      import('./modules/danh-muc-dti/danh-muc-dti.routes').then((m) => m.DANH_MUC_DTI_ROUTES),
  },
  { path: '**', redirectTo: 'trang-chu' },
];

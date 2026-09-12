import { Routes } from '@angular/router';

// Guard đặt TRONG route của từng feature (trang-chu.routes.ts/login.routes.ts/...), không
// cấu hình rời rạc ở đây — đúng quy ước doc/huong_dan/quy-uoc/fe-routing-guard.md §2. File này
// CHỈ `loadChildren`, không `loadComponent`, không import component nào: nó là bảng mục lục.
//
// `platform/` = màn hình Core dùng lại được cho mọi sản phẩm (đăng nhập, đổi mật khẩu, quản trị
// người dùng, phân quyền); `modules/` = module NGHIỆP VỤ — xem doc/kien-truc-core-module.md.
//
// `/trang-chu` là trang đích mặc định (đích của `''`, của `**`, và của mọi chuyển hướng "về chỗ
// an toàn"), và **từ 2026-09-11 nó là Dashboard DTI** — chốt Q3
// (spec/dashboard-dti/business-rules.md §"Dashboard thay /trang-chu"). Đường dẫn **không đổi**
// (Q18: `APP_CORE_ROUTES.home` vẫn là `/trang-chu`), thứ đổi là màn nằm sau nó; `platform/trang-chu/`
// đã bị gỡ (Q29) chứ không đứng cạnh.
//
// 🛑 Route này KHÔNG được mang guard theo VAI TRÒ — nó là đích của mọi redirect "về chỗ an toàn",
// nên một `adminGuard` ở đây là vòng lặp redirect vô hạn cho đúng nhóm người bị đá về
// (doc/huong_dan/quy-uoc/fe-routing-guard.md §1). Có test khoá lại trong `app.routes.spec.ts`.
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
    loadChildren: () => import('./modules/dashboard/dashboard.routes').then((m) => m.DASHBOARD_ROUTES),
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
  // `/tong-quan/dti` — route TẠM của vòng 1 — đã GỠ 2026-09-11 cùng lượt hoán đổi. Không giữ hai
  // đường vào cùng một màn: hai URL cho một trang là hai bookmark, hai mục lịch sử, và một trong
  // hai sẽ trôi khỏi mọi phép kiểm.
  { path: '**', redirectTo: 'trang-chu' },
];

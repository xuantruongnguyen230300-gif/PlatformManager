import { Routes } from '@angular/router';
import { authGuard } from '../../core/auth/auth.guard';

// 🛑 CHỈ `authGuard` — route này KHÔNG được gắn `mustChangePasswordGuard`, gắn vào là VÒNG LẶP
// REDIRECT VÔ HẠN (guard sẽ đẩy về đúng route đang chạy nó). Vẫn cần `authGuard` để chặn user
// chưa đăng nhập gõ thẳng /doi-mat-khau. Xem doc/huong_dan/quy-uoc/fe-routing-guard.md §4 —
// ràng buộc này được khoá lại bằng máy ở src/app/app.routes.spec.ts.
//
// `title` ở CẤP `Route`, `noShell` ở lại trong `data` — xem chú thích cùng chủ đề ở
// platform/login/login.routes.ts.
export const DOI_MAT_KHAU_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./pages/doi-mat-khau/doi-mat-khau.page').then((m) => m.DoiMatKhauPage),
    title: 'doi-mat-khau.routeTitle',
    data: { noShell: true },
    canActivate: [authGuard],
  },
];

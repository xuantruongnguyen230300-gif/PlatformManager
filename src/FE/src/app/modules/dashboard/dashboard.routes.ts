import { Routes } from '@angular/router';
import { authGuard } from '../../core/auth/auth.guard';
import { mustChangePasswordGuard } from '../../core/auth/must-change-password.guard';

// Màn NGHIỆP VỤ (doc/kien-truc-core-module.md): dashboard DTI chỉ có nghĩa với domain chuyển đổi
// số, nên nó thuộc `modules/`, không thuộc `platform/`.
//
// ✅ CHIẾM `/trang-chu` — hoán đổi xong 2026-09-11 (chốt Q3).
//
// Màn này **thay** `platform/trang-chu/` chứ không đứng cạnh: thư mục đó đã bị xoá (Q29), và route
// tạm `/tong-quan/dti` của vòng 1 đã gỡ khỏi `app.routes.ts` — không giữ hai đường vào cùng một màn.
// `APP_CORE_ROUTES.home` **không đổi**, vẫn `/trang-chu` (Q18): đổi là màn nằm sau đường dẫn, không
// phải đường dẫn.
//
// Vì sao việc này làm ở CUỐI: `/trang-chu` là đích của `''`, của `**` và của mọi chuyển hướng "về
// chỗ an toàn" (`roleGuard` khi thiếu quyền, `doi-mat-khau` sau khi đổi xong). Hoán đổi khi màn còn
// thiếu chức năng là làm hỏng đúng thứ không được phép hỏng — nên nó chỉ xảy ra sau khi nút
// `Xuất báo cáo` (DB-4) chạy được.
//
// 🛑 Route này KHÔNG được mang guard theo VAI TRÒ. Nó là bến an toàn, nên gắn `adminGuard` vào đây
// là vòng lặp redirect vô hạn cho đúng nhóm người bị đá về (doc/huong_dan/quy-uoc/fe-routing-guard.md
// §1). Hai guard dưới đây là đúng và đủ, và `app.routes.spec.ts` khoá lại cả THỨ TỰ của chúng.
export const DASHBOARD_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./pages/dashboard/dashboard.page').then((m) => m.DashboardPage),
    title: 'dashboard.routeTitle',
    canActivate: [authGuard, mustChangePasswordGuard],
  },
];

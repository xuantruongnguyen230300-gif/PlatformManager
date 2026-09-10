import { Routes } from '@angular/router';
import { authGuard } from '../../core/auth/auth.guard';
import { mustChangePasswordGuard } from '../../core/auth/must-change-password.guard';

// Màn NGHIỆP VỤ (doc/kien-truc-core-module.md): dashboard DTI chỉ có nghĩa với domain chuyển đổi
// số, nên nó thuộc `modules/`, không thuộc `platform/`.
//
// 🛑 CHƯA KHAI vào `src/FE/src/app/app.routes.ts` — CÓ CHỦ ĐÍCH, không phải quên. Theo chốt Q3
// (`spec/dashboard-dti/business-rules.md` §"Dashboard thay /trang-chu") màn này chiếm đường dẫn
// `/trang-chu`, tức nó THAY `platform/trang-chu/` chứ không đứng cạnh. Mà `/trang-chu` đang là
// đích của `''`, của `**` và của mọi chuyển hướng "về chỗ an toàn" (role.guard khi thiếu quyền,
// doi-mat-khau sau khi đổi xong) — hoán đổi khi trang này còn rỗng là làm hỏng bến an toàn của cả
// app để đổi lấy một trang trắng. Việc hoán đổi thuộc lượt dựng màn thật.
//
// Vì vậy file này hôm nay chỉ được `tsconfig.app.json` biên dịch (`include: src/**/*.ts`) và
// `ng lint` soi, không có đường nào từ router tới nó.
//
// 🛑 Khi hoán đổi: route này KHÔNG được mang guard theo VAI TRÒ, vì nó sẽ là đích của mọi redirect
// "về chỗ an toàn" — gắn `adminGuard` vào đây là vòng lặp redirect vô hạn cho đúng nhóm người bị
// đá về (doc/huong_dan/quy-uoc/fe-routing-guard.md §1). Hai guard dưới đây là đúng và đủ.
export const DASHBOARD_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./pages/dashboard/dashboard.page').then((m) => m.DashboardPage),
    title: 'dashboard.routeTitle',
    canActivate: [authGuard, mustChangePasswordGuard],
  },
];

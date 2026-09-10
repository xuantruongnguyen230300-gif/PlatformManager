import { Routes } from '@angular/router';
import { authGuard } from '../../core/auth/auth.guard';
import { mustChangePasswordGuard } from '../../core/auth/must-change-password.guard';

// Màn NGHIỆP VỤ đầu tiên của tầng `modules/` (doc/kien-truc-core-module.md): danh mục chỉ tiêu DTI
// chỉ có nghĩa với domain chuyển đổi số, không có nghĩa với mọi sản phẩm dựng trên nền tảng — nên
// nó KHÔNG thuộc `platform/`.
//
// Đường dẫn `/danh-muc/dti` là chốt Q33 (spec/danh-muc-dti/ui-spec.md §2): dùng lại đúng đường dẫn
// lịch sử của chính màn này trước khi bị gỡ 2026-08-29, cùng khuôn hai cấp với `/quan-tri/nguoi-dung`.
//
// 🛑 ĐÚNG HAI guard, theo chốt Q39: `authGuard → mustChangePasswordGuard`, KHÔNG guard theo quyền.
// Quyền GHI không chặn ở tầng route mà chỉ ẩn affordance ghi trong màn (ui-spec §5.6) — gắn thêm
// guard quyền ở đây sẽ đá người chỉ-đọc về trang chủ thay vì cho họ xem danh mục.
export const DANH_MUC_DTI_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/danh-muc-dti/danh-muc-dti.page').then((m) => m.DanhMucDtiPage),
    title: 'danh-muc-dti.routeTitle',
    canActivate: [authGuard, mustChangePasswordGuard],
  },
];

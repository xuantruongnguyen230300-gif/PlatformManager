import { ChangeDetectionStrategy, Component } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

/**
 * SMART — sẽ là route `/trang-chu` sau khi hoán đổi (chốt Q3). Hôm nay CHƯA có route nào trỏ tới:
 * lý do đầy đủ ở `../../dashboard.routes.ts`.
 *
 * 🚧 **KHUNG, CHƯA PHẢI MÀN THẬT (dựng 2026-09-09).** Màn đầy đủ — dải KPI, biểu đồ xu hướng, bảng
 * tổng hợp, xuất báo cáo — đặc tả ở `spec/dashboard-dti/ui-spec.md` và
 * `doc/Design/Frontend/PlatformManager/Screens/01-dashboard.md`.
 *
 * Biểu đồ sẽ dùng `chart.js` (gói đã thêm lại vào `package.json` ngày 2026-09-09 đúng vì màn này);
 * vòng quay chờ dữ liệu thì dùng `p-progressSpinner` của PrimeNG (chốt T10), KHÔNG vẽ bằng chart.
 */
@Component({
  selector: 'app-dashboard-page',
  standalone: true,
  imports: [TranslatePipe],
  templateUrl: './dashboard.page.html',
  styleUrl: './dashboard.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardPage {}

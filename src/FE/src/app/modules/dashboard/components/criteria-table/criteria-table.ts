import { Component, input } from '@angular/core';

import { TableScroll } from '../../../../shared/components/table-scroll/table-scroll';
import { IDashboardTableRow } from '../../models/dashboard.model';

const BADGE_CLASS: Record<string, string> = {
  'Hoàn thành': 'bdone',
  'Đang thực hiện': 'bwork',
  'Không tăng': 'bstall'
};

/**
 * Bảng "62 chỉ tiêu DTI" của Dashboard — dumb, 100% read-only. KHÔNG có cột
 * sticky (không có cột hành động Sửa/Xoá — xem
 * `spec/dashboard-dti-weekly/ui-spec.md` mục 2.4). `Badge` đã là chuỗi tiếng
 * Việt tính sẵn từ server (`statusFor()`), không cần map thêm — xem
 * `doc/contracts/dashboard.md` CONTRACT DB-1.
 */
@Component({
  selector: 'app-dashboard-criteria-table',
  standalone: true,
  imports: [TableScroll],
  templateUrl: './criteria-table.html',
  styleUrl: './criteria-table.scss'
})
export class DashboardCriteriaTable {
  readonly rows = input.required<IDashboardTableRow[]>();
  readonly prevColumnLabel = input('Tuần trước');
  readonly currentColumnLabel = input('Tuần này');

  formatPercent(value: number | null): string {
    return value === null || value === undefined ? '—' : `${value.toFixed(1)}%`;
  }

  formatDelta(value: number | null): string {
    if (value === null || value === undefined) return '—';
    const sign = value > 0 ? '+' : '';
    return `${sign}${value.toFixed(1)}%`;
  }

  deltaClass(value: number | null): string {
    if (value === null || value === undefined) return 'flat';
    if (value > 0.001) return 'up';
    if (value < -0.001) return 'down';
    return 'flat';
  }

  badgeClass(badge: string | null): string {
    return badge ? BADGE_CLASS[badge] ?? 'bwork' : '';
  }
}

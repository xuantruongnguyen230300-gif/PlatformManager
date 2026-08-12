import { Component, computed, input } from '@angular/core';

import { DashboardMode, IDashboardKpi } from '../../models/dashboard.model';

/**
 * 5 thẻ KPI đầu Dashboard — dumb component, chỉ render `kpi` đã tính sẵn từ
 * `GET /api/dashboard` (xem `doc/contracts/dashboard.md`). Nhãn 2 thẻ đầu đổi
 * động theo `mode` (week/month/year) — xem
 * `spec/dashboard-dti-weekly/ui-spec.md` mục 2.
 */
@Component({
  selector: 'app-kpi-summary',
  standalone: true,
  templateUrl: './kpi-summary.html',
  styleUrl: './kpi-summary.scss'
})
export class KpiSummary {
  readonly kpi = input.required<IDashboardKpi>();
  readonly mode = input.required<DashboardMode>();

  readonly progressLabel = computed(() => {
    switch (this.mode()) {
      case 'week':
        return 'Tiến độ chung tuần này';
      case 'month':
        return 'Tiến độ chung tháng này';
      default:
        return 'Tiến độ chung (Tất cả)';
    }
  });

  readonly deltaLabel = computed(() => {
    switch (this.mode()) {
      case 'week':
        return 'So với tuần trước';
      case 'month':
        return 'So với tháng trước';
      default:
        return 'So với năm trước';
    }
  });

  readonly deltaClass = computed(() => {
    const d = this.kpi().Delta;
    if (d === null || d === undefined) return 'flat';
    if (d > 0.001) return 'up';
    if (d < -0.001) return 'down';
    return 'flat';
  });

  formatPercent(value: number | null): string {
    return value === null || value === undefined ? '—' : `${value.toFixed(1)}%`;
  }

  formatDelta(value: number | null): string {
    if (value === null || value === undefined) return '—';
    const sign = value > 0 ? '+' : '';
    return `${sign}${value.toFixed(1)}%`;
  }
}

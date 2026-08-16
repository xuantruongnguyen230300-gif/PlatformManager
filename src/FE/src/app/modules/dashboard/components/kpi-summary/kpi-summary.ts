import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { DashboardViewMode, IDashboardKpi } from '../../models/dashboard.model';
import { KpiTile, KpiTone } from '../kpi-tile/kpi-tile';

function formatPercent(v: number | null): string {
  return v === null ? '—' : `${v.toLocaleString('vi-VN', { minimumFractionDigits: 1, maximumFractionDigits: 1 })}%`;
}

function formatDelta(v: number | null): string {
  if (v === null) return '—';
  const prefix = v > 0 ? '↑ ' : v < 0 ? '↓ ' : '';
  return `${prefix}${Math.abs(v).toLocaleString('vi-VN', { minimumFractionDigits: 1, maximumFractionDigits: 1 })} đ.%`;
}

function deltaTone(v: number | null): KpiTone {
  if (v === null) return 'default';
  return v > 0 ? 'good' : v < 0 ? 'bad' : 'default';
}

/** 5 KPI card đầu Dashboard — nhãn đổi theo mode (Tuần/Tháng/"Tất cả"), xem yêu cầu gốc task. */
@Component({
  selector: 'app-kpi-summary',
  standalone: true,
  imports: [KpiTile],
  templateUrl: './kpi-summary.html',
  styleUrl: './kpi-summary.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class KpiSummary {
  readonly kpi = input.required<IDashboardKpi>();
  readonly viewMode = input.required<DashboardViewMode>();
  readonly isAllMode = input<boolean>(false);

  protected readonly progressLabel = computed(() => {
    if (this.isAllMode()) return 'Tiến độ chung (tổng hợp năm)';
    return this.viewMode() === 'month' ? 'Tiến độ chung tháng này' : 'Tiến độ chung tuần này';
  });

  protected readonly deltaLabel = computed(() =>
    this.viewMode() === 'month' && !this.isAllMode() ? 'So với tháng trước' : 'So với tuần trước',
  );

  protected readonly progressValue = computed(() => formatPercent(this.kpi().OverallProgress));
  protected readonly deltaValue = computed(() => formatDelta(this.kpi().Delta));
  protected readonly deltaTone = computed(() => deltaTone(this.kpi().Delta));
  protected readonly prevSub = computed(() => this.kpi().PreviousPeriodLabel ?? 'Chưa có kỳ trước');
  protected readonly doneFraction = computed(() => `${this.kpi().Done}/${this.kpi().TotalCriteria}`);
}

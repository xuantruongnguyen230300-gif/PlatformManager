import { ChangeDetectionStrategy, Component, computed, effect, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DashboardService } from '../../services/dashboard.service';
import { PeriodOptionsService } from '../../../../shared/services/period-options.service';
import { IPeriodOptions } from '../../../../shared/models/period-options.model';
import { ALL_WEEKS_VALUE, DashboardViewMode, IDashboardAggregate, IDashboardQueryParams } from '../../models/dashboard.model';
import { PeriodToolbar } from '../../components/period-toolbar/period-toolbar';
import { KpiSummary } from '../../components/kpi-summary/kpi-summary';
import { GroupProgressList } from '../../components/group-progress-list/group-progress-list';
import { TrendChart } from '../../components/trend-chart/trend-chart';
import { HistoryList } from '../../components/history-list/history-list';
import { CriteriaTable } from '../../components/criteria-table/criteria-table';
import { ReportDialog, ReportCopyResult } from '../../components/report-dialog/report-dialog';
import { ToastService } from '../../../../shared/services/toast.service';

const EMPTY_AGGREGATE: IDashboardAggregate = {
  Mode: 'week',
  PeriodLabel: '—',
  Kpi: {
    OverallProgress: null,
    Delta: null,
    PreviousPeriodLabel: null,
    Up: 0,
    Flat: 0,
    Down: 0,
    Done: 0,
    TotalCriteria: 0,
  },
  Groups: [],
  Trend: [],
  Table: [],
};

/**
 * SMART — route target `dashboard`. Điều phối `period-toolbar`/`kpi-summary`/`trend-chart`/
 * `group-progress-list`/`history-list`/`criteria-table`/`report-dialog`, gọi `DashboardService`.
 * Dashboard 100% đọc-only — không có action ghi dữ liệu nào ở màn này (CRUD thuộc
 * `modules/danh-muc-dti`).
 */
@Component({
  selector: 'app-dashboard-page',
  standalone: true,
  imports: [
    RouterLink,
    PeriodToolbar,
    KpiSummary,
    GroupProgressList,
    TrendChart,
    HistoryList,
    CriteriaTable,
    ReportDialog,
  ],
  templateUrl: './dashboard.page.html',
  styleUrl: './dashboard.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardPage {
  private readonly dashboardService = inject(DashboardService);
  private readonly periodOptionsService = inject(PeriodOptionsService);
  private readonly toast = inject(ToastService);

  protected readonly viewMode = signal<DashboardViewMode>('week');
  protected readonly selectedYear = signal<number>(new Date().getFullYear());
  protected readonly selectedWeekValue = signal<string>('');
  protected readonly selectedMonthValue = signal<string>('');

  protected readonly periodOptions = signal<IPeriodOptions | null>(null);
  protected readonly aggregate = signal<IDashboardAggregate>(EMPTY_AGGREGATE);
  protected readonly loading = signal(false);

  protected readonly reportOpen = signal(false);
  protected readonly reportTitle = signal('Báo cáo tiến độ DTI');
  protected readonly reportContent = signal('');

  protected readonly isAllMode = computed(
    () => this.viewMode() === 'week' && this.selectedWeekValue() === ALL_WEEKS_VALUE,
  );

  protected readonly years = computed(() => {
    const list = this.periodOptions()?.Years ?? [];
    return list.includes(this.selectedYear()) ? list : [...list, this.selectedYear()].sort();
  });

  private readonly requestParams = computed<IDashboardQueryParams>(() => {
    const year = this.selectedYear();
    if (this.viewMode() === 'month') {
      const option = this.periodOptions()?.MonthsInYear.find((m) => m.Value === this.selectedMonthValue());
      return { Mode: 'month', Year: year, Date: option?.Date };
    }
    if (this.selectedWeekValue() === ALL_WEEKS_VALUE) {
      return { Mode: 'year', Year: year };
    }
    const option = this.periodOptions()?.WeeksInYear.find((w) => w.Value === this.selectedWeekValue());
    return { Mode: 'week', Year: year, Date: option?.Date };
  });

  constructor() {
    // effect() dùng ĐÚNG mục đích: side-effect đồng bộ với API bên ngoài Angular (gọi HTTP theo
    // filter đang chọn) — không dùng để derive state thuần (những cái đó đã là computed() ở trên).
    effect(() => {
      const year = this.selectedYear();
      this.periodOptionsService.getPeriodOptions(year).subscribe({
        next: (options) => this.periodOptions.set(options),
        error: () => this.periodOptions.set(null),
      });
    });

    effect(() => {
      const params = this.requestParams();
      this.loading.set(true);
      this.dashboardService.getAggregate(params).subscribe({
        next: (agg) => {
          this.aggregate.set(agg);
          this.loading.set(false);
        },
        error: () => this.loading.set(false),
      });
    });
  }

  onViewModeChange(mode: DashboardViewMode): void {
    this.viewMode.set(mode);
    this.selectedWeekValue.set('');
    this.selectedMonthValue.set('');
  }

  onYearChange(year: number): void {
    this.selectedYear.set(year);
    this.selectedWeekValue.set('');
    this.selectedMonthValue.set('');
  }

  onWeekValueChange(value: string): void {
    this.selectedWeekValue.set(value);
  }

  onMonthValueChange(value: string): void {
    this.selectedMonthValue.set(value);
  }

  onHistoryView(value: string): void {
    // Chọn 1 kỳ-tuần cụ thể (qua Lịch sử) luôn chuyển về mode Tuần — khớp hành vi gốc
    // (loadSavedWeek() trong doc/Prototype/dashboard.html).
    this.viewMode.set('week');
    this.selectedWeekValue.set(value);
  }

  onExportReport(): void {
    this.dashboardService.getReport(this.requestParams()).subscribe({
      next: (report) => {
        this.reportTitle.set(report.Title);
        this.reportContent.set(report.ContentHtml);
        this.reportOpen.set(true);
      },
      error: () => {
        // httpErrorInterceptor đã hiện toast — không cần xử lý thêm ở đây.
      },
    });
  }

  onReportClosed(): void {
    this.reportOpen.set(false);
  }

  onReportCopyResult(result: ReportCopyResult): void {
    if (result === 'success') {
      this.toast.success('Đã sao chép báo cáo.');
    } else {
      this.toast.error('Không sao chép được — trình duyệt chặn quyền truy cập clipboard.');
    }
  }
}

import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { Pagination } from '../../../../shared/components/pagination/pagination';
import { Topbar } from '../../../../shared/components/topbar/topbar';
import { DashboardCriteriaTable } from '../../components/criteria-table/criteria-table';
import { GroupProgressList } from '../../components/group-progress-list/group-progress-list';
import { HistoryList } from '../../components/history-list/history-list';
import { KpiSummary } from '../../components/kpi-summary/kpi-summary';
import { ALL_WEEKS_VALUE, PeriodToolbar } from '../../components/period-toolbar/period-toolbar';
import { ReportDialog } from '../../components/report-dialog/report-dialog';
import { TrendChart } from '../../components/trend-chart/trend-chart';
import {
  DashboardMode,
  IDashboardSummary,
  IDashboardTableRow,
  IPeriodOption,
  TableChangeFilter,
  TableSortBy
} from '../../models/dashboard.model';
import { DashboardService } from '../../services/dashboard.service';

const TABLE_PAGE_SIZE_OPTIONS = [10, 20, 50];

function pad2(n: number): string {
  return String(n).padStart(2, '0');
}

/**
 * Page SMART Dashboard — 100% read-only. Orchestrate chuyển đổi Tuần/Tháng/
 * "Tất cả" (mode='year'), nạp dữ liệu qua `DashboardService`, lọc/sắp xếp/
 * phân trang bảng 62 chỉ tiêu phía client (server trả nguyên bộ 1 lần gọi —
 * xem `doc/contracts/dashboard.md`). Component con hoàn toàn dumb.
 */
@Component({
  selector: 'app-dashboard-page',
  standalone: true,
  imports: [
    RouterLink,
    Topbar,
    PeriodToolbar,
    KpiSummary,
    GroupProgressList,
    TrendChart,
    DashboardCriteriaTable,
    Pagination,
    HistoryList,
    ReportDialog
  ],
  templateUrl: './dashboard.page.html',
  styleUrl: './dashboard.page.scss'
})
export class DashboardPage {
  private readonly service = inject(DashboardService);

  readonly tablePageSizeOptions = TABLE_PAGE_SIZE_OPTIONS;

  // ===== Period / mode state =====
  readonly mode = signal<DashboardMode>('week');
  readonly year = signal(new Date().getFullYear());
  readonly weekDate = signal<string | null>(null);
  readonly monthValue = signal(new Date().getMonth() + 1);

  readonly years = signal<number[]>([]);
  readonly weeksInYear = signal<IPeriodOption[]>([]);

  readonly summary = signal<IDashboardSummary | null>(null);
  readonly loading = signal(false);

  // ===== Table filter/sort/pagination (client-side) =====
  readonly tableSearch = signal('');
  readonly tableGroupFilter = signal('');
  readonly tableChangeFilter = signal<TableChangeFilter>('');
  readonly tableSortBy = signal<TableSortBy>('id');
  readonly tablePage = signal(1);
  readonly tablePageSize = signal(20);

  readonly reportDialogOpen = signal(false);
  readonly reportTitle = signal('');
  readonly reportContentHtml = signal('');

  readonly groupFilterOptions = computed(() => {
    const map = new Map<string, string>();
    (this.summary()?.Table ?? []).forEach((r) => map.set(r.GroupCode, r.GroupName));
    return Array.from(map.entries()).map(([GroupCode, GroupName]) => ({ GroupCode, GroupName }));
  });

  readonly filteredTableRows = computed<IDashboardTableRow[]>(() => {
    const all = this.summary()?.Table ?? [];
    const search = this.tableSearch().trim().toLowerCase();
    const group = this.tableGroupFilter();
    const change = this.tableChangeFilter();

    let rows = all.filter((r) => {
      if (search && !`${r.Code} ${r.Name}`.toLowerCase().includes(search)) return false;
      if (group && r.GroupCode !== group) return false;
      if (change === 'up' && !(r.Delta !== null && r.Delta > 0.001)) return false;
      if (change === 'flat' && !(r.Delta !== null && Math.abs(r.Delta) <= 0.001)) return false;
      if (change === 'down' && !(r.Delta !== null && r.Delta < -0.001)) return false;
      if (change === 'done' && !(r.CurrentValue !== null && r.CurrentValue >= 99.999)) return false;
      return true;
    });

    const sortBy = this.tableSortBy();
    if (sortBy === 'delta') {
      rows = [...rows].sort((a, b) => (b.Delta ?? -Infinity) - (a.Delta ?? -Infinity));
    } else if (sortBy === 'low') {
      rows = [...rows].sort((a, b) => (a.CurrentValue ?? Infinity) - (b.CurrentValue ?? Infinity));
    } else {
      rows = [...rows].sort((a, b) => a.Code.localeCompare(b.Code));
    }

    return rows;
  });

  readonly pagedTableRows = computed(() => {
    const rows = this.filteredTableRows();
    const start = (this.tablePage() - 1) * this.tablePageSize();
    return rows.slice(start, start + this.tablePageSize());
  });

  readonly prevColumnLabel = computed(() => {
    switch (this.mode()) {
      case 'week':
        return 'Tuần trước';
      case 'month':
        return 'Tháng trước';
      default:
        return '—';
    }
  });

  readonly currentColumnLabel = computed(() => {
    switch (this.mode()) {
      case 'week':
        return 'Tuần này';
      case 'month':
        return 'Tháng này';
      default:
        return 'Tất cả (TB)';
    }
  });

  readonly groupsSubtitle = computed(() => {
    switch (this.mode()) {
      case 'week':
        return 'Tuần hiện tại';
      case 'month':
        return 'Tháng hiện tại';
      default:
        return 'Tổng hợp toàn bộ năm';
    }
  });

  readonly periodLabelText = computed(() => {
    switch (this.mode()) {
      case 'week':
        return 'Kỳ đang xem:';
      case 'month':
        return 'Tháng đang xem:';
      default:
        return 'Đang xem:';
    }
  });

  constructor() {
    this.loadPeriodOptions(this.year());
    this.loadSummary();
  }

  // ===== Toolbar handlers =====

  onModeChange(mode: DashboardMode): void {
    this.mode.set(mode);
    this.loadSummary();
  }

  onYearChange(year: number): void {
    this.year.set(year);
    this.loadPeriodOptions(year);
    this.loadSummary();
  }

  onWeekSelect(value: string): void {
    if (!value) return;
    if (value === ALL_WEEKS_VALUE) {
      this.mode.set('year');
    } else {
      this.mode.set('week');
      this.weekDate.set(value.length === 10 ? value : this.resolveWeekDate(value));
    }
    this.loadSummary();
  }

  onMonthChange(month: number): void {
    this.monthValue.set(month);
    this.loadSummary();
  }

  onHistoryView(option: IPeriodOption): void {
    this.mode.set('week');
    this.weekDate.set(option.Date);
    this.loadSummary();
  }

  // ===== Table filters =====

  onTableSearch(value: string): void {
    this.tableSearch.set(value);
    this.tablePage.set(1);
  }

  onTableGroupFilter(value: string): void {
    this.tableGroupFilter.set(value);
    this.tablePage.set(1);
  }

  onTableChangeFilter(value: TableChangeFilter): void {
    this.tableChangeFilter.set(value);
    this.tablePage.set(1);
  }

  onTableSortBy(value: TableSortBy): void {
    this.tableSortBy.set(value);
    this.tablePage.set(1);
  }

  onTablePageChange(page: number): void {
    this.tablePage.set(page);
  }

  onTablePageSizeChange(size: number): void {
    this.tablePageSize.set(size);
    this.tablePage.set(1);
  }

  // ===== Report =====

  openReport(): void {
    const mode = this.mode();
    this.service
      .getReport({
        Mode: mode,
        Date: mode === 'week' ? this.weekDate() : mode === 'month' ? this.monthDateParam() : null,
        Year: this.year()
      })
      .subscribe((report) => {
        this.reportTitle.set(report.Title);
        this.reportContentHtml.set(report.ContentHtml);
        this.reportDialogOpen.set(true);
      });
  }

  onReportClosed(): void {
    this.reportDialogOpen.set(false);
  }

  // ===== Data loading =====

  private monthDateParam(): string {
    return `${this.year()}-${pad2(this.monthValue())}-01`;
  }

  private resolveWeekDate(periodValue: string): string {
    const option = this.weeksInYear().find((w) => w.Value === periodValue);
    return option?.Date ?? this.weekDate() ?? '';
  }

  private loadSummary(): void {
    const mode = this.mode();
    this.loading.set(true);
    this.service
      .getDashboard({
        Mode: mode,
        Date: mode === 'week' ? this.weekDate() : mode === 'month' ? this.monthDateParam() : null,
        Year: this.year()
      })
      .subscribe({
        next: (summary) => {
          this.summary.set(summary);
          this.loading.set(false);
          this.tablePage.set(1);
        },
        error: () => this.loading.set(false)
      });
  }

  private loadPeriodOptions(year: number): void {
    this.service.getPeriodOptions(year).subscribe((options) => {
      this.years.set(options.Years);
      this.weeksInYear.set(options.WeeksInYear);
      if (!this.weekDate() && options.WeeksInYear.length > 0) {
        const latest = [...options.WeeksInYear].sort((a, b) => b.Date.localeCompare(a.Date))[0];
        this.weekDate.set(latest.Date);
        this.loadSummary();
      }
    });
  }
}

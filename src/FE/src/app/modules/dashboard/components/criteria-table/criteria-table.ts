import { ChangeDetectionStrategy, Component, computed, input, signal } from '@angular/core';
import { TableModule } from 'primeng/table';
import { CriteriaChangeFilter, CriteriaSortBy, DashboardViewMode, ICriteriaTableRow } from '../../models/dashboard.model';
import { DeltaIndicator } from '../delta-indicator/delta-indicator';
import { StatusBadge } from '../status-badge/status-badge';

/** Epsilon so sánh Delta — khớp business rule BE, xem doc/contracts/dashboard.md. */
const EPSILON = 0.001;
const DONE_THRESHOLD = 99.999;

function formatPercent(v: number | null): string {
  return v === null ? '—' : `${v.toLocaleString('vi-VN', { minimumFractionDigits: 1, maximumFractionDigits: 1 })}%`;
}

interface IGroupOption {
  Code: string;
  Name: string;
}

/**
 * Bảng chỉ tiêu đầy đủ (đọc-only) — `p-table`, mọi lọc/sắp xếp/phân trang CLIENT-SIDE (BE trả
 * nguyên bảng của 1 kỳ, ~62 dòng, xem yêu cầu gốc task + doc/huong_dan/wiki-core/fe/11-grid-and-metadata.md
 * §Mẫu dùng p-table — ở đây KHÔNG dùng `[lazy]` vì dữ liệu đã tải hết 1 lần). Dumb theo nghĩa
 * không inject data service — state lọc/sắp xếp là UI state cục bộ của chính component, không
 * phải state nghiệp vụ toàn app.
 */
@Component({
  selector: 'app-criteria-table',
  standalone: true,
  imports: [TableModule, DeltaIndicator, StatusBadge],
  templateUrl: './criteria-table.html',
  styleUrl: './criteria-table.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CriteriaTable {
  readonly rows = input.required<ICriteriaTableRow[]>();
  readonly viewMode = input.required<DashboardViewMode>();
  readonly isAllMode = input<boolean>(false);

  protected readonly searchText = signal('');
  protected readonly groupFilter = signal('');
  protected readonly changeFilter = signal<CriteriaChangeFilter>('');
  protected readonly sortBy = signal<CriteriaSortBy>('id');

  protected readonly formatPercent = formatPercent;

  protected readonly prevHeader = computed(() =>
    this.isAllMode() ? '—' : this.viewMode() === 'month' ? 'Tháng trước' : 'Tuần trước',
  );
  protected readonly curHeader = computed(() =>
    this.isAllMode() ? 'Tất cả (TB)' : this.viewMode() === 'month' ? 'Tháng này' : 'Tuần này',
  );

  protected readonly groupOptions = computed<IGroupOption[]>(() => {
    const map = new Map<string, string>();
    for (const row of this.rows()) map.set(row.GroupCode, row.GroupName);
    return [...map.entries()]
      .map(([Code, Name]) => ({ Code, Name }))
      .sort((a, b) => a.Code.localeCompare(b.Code, undefined, { numeric: true }));
  });

  protected readonly filteredRows = computed<ICriteriaTableRow[]>(() => {
    const search = this.searchText().trim().toLowerCase();
    const group = this.groupFilter();
    const change = this.changeFilter();
    const sort = this.sortBy();

    let rows = this.rows().filter((r) => {
      if (search && !`${r.Code} ${r.Name}`.toLowerCase().includes(search)) return false;
      if (group && r.GroupCode !== group) return false;
      return true;
    });

    if (change) {
      rows = rows.filter((r) => this.matchesChangeFilter(r, change));
    }

    rows = [...rows];
    if (sort === 'delta') {
      rows.sort((a, b) => (b.Delta ?? -999) - (a.Delta ?? -999));
    } else if (sort === 'low') {
      rows.sort((a, b) => (a.CurrentValue ?? 0) - (b.CurrentValue ?? 0));
    } else {
      rows.sort((a, b) => a.Code.localeCompare(b.Code, undefined, { numeric: true }));
    }
    return rows;
  });

  protected readonly countLabel = computed(() => `${this.filteredRows().length}/${this.rows().length} chỉ tiêu`);

  private matchesChangeFilter(row: ICriteriaTableRow, filter: CriteriaChangeFilter): boolean {
    const d = row.Delta;
    switch (filter) {
      case 'up':
        return d !== null && d > EPSILON;
      case 'down':
        return d !== null && d < -EPSILON;
      case 'flat':
        return d !== null && Math.abs(d) <= EPSILON;
      case 'done':
        return row.CurrentValue !== null && row.CurrentValue >= DONE_THRESHOLD;
      default:
        return true;
    }
  }

  onSearchInput(event: Event): void {
    this.searchText.set((event.target as HTMLInputElement).value);
  }

  onGroupFilterChange(event: Event): void {
    this.groupFilter.set((event.target as HTMLSelectElement).value);
  }

  onChangeFilterChange(event: Event): void {
    this.changeFilter.set((event.target as HTMLSelectElement).value as CriteriaChangeFilter);
  }

  onSortByChange(event: Event): void {
    this.sortBy.set((event.target as HTMLSelectElement).value as CriteriaSortBy);
  }
}

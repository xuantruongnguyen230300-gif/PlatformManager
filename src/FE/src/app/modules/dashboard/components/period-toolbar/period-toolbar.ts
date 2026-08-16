import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { ALL_WEEKS_VALUE, DashboardViewMode } from '../../models/dashboard.model';
import { IPeriodOption } from '../../../../shared/models/period-options.model';

function formatDateVn(iso: string): string {
  const [y, m, d] = iso.split('-');
  return `${d}/${m}/${y}`;
}

function formatProgress(v: number | null): string {
  return v === null ? '' : ` · ${v.toLocaleString('vi-VN', { minimumFractionDigits: 1, maximumFractionDigits: 1 })}%`;
}

function weekOptionLabel(option: IPeriodOption): string {
  return `${formatDateVn(option.Date)}${formatProgress(option.OverallProgress)}`;
}

function monthOptionLabel(option: IPeriodOption): string {
  const [, month] = option.Value.split('-');
  return `Tháng ${Number(month)}${formatProgress(option.OverallProgress)}`;
}

/**
 * Dumb — chọn mode Tuần/Tháng + năm + kỳ cụ thể/"Tất cả". Không tự gọi API, chỉ phát output() cho
 * `DashboardPage` (smart) xử lý — xem doc/ke-hoach-xay-lai-corebase.md mô tả nghiệp vụ cần port.
 */
@Component({
  selector: 'app-period-toolbar',
  standalone: true,
  templateUrl: './period-toolbar.html',
  styleUrl: './period-toolbar.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PeriodToolbar {
  readonly viewMode = input.required<DashboardViewMode>();
  readonly years = input<number[]>([]);
  readonly selectedYear = input.required<number>();
  readonly weekOptions = input<IPeriodOption[]>([]);
  readonly selectedWeekValue = input<string>('');
  readonly monthOptions = input<IPeriodOption[]>([]);
  readonly selectedMonthValue = input<string>('');
  readonly periodLabel = input<string>('');
  readonly isAllMode = input<boolean>(false);

  readonly viewModeChange = output<DashboardViewMode>();
  readonly yearChange = output<number>();
  readonly weekValueChange = output<string>();
  readonly monthValueChange = output<string>();
  readonly exportReport = output<void>();

  protected readonly allWeeksValue = ALL_WEEKS_VALUE;
  protected readonly weekOptionLabel = weekOptionLabel;
  protected readonly monthOptionLabel = monthOptionLabel;

  onYearChange(event: Event): void {
    this.yearChange.emit(Number((event.target as HTMLSelectElement).value));
  }

  onWeekChange(event: Event): void {
    this.weekValueChange.emit((event.target as HTMLSelectElement).value);
  }

  onMonthChange(event: Event): void {
    this.monthValueChange.emit((event.target as HTMLSelectElement).value);
  }
}

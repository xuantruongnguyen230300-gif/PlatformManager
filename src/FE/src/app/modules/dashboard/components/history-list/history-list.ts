import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { IPeriodOption } from '../../../../shared/models/period-options.model';
import { DeltaIndicator } from '../delta-indicator/delta-indicator';

function formatDateVn(iso: string): string {
  const [y, m, d] = iso.split('-');
  return `${d}/${m}/${y}`;
}

interface IHistoryRow {
  Value: string;
  DateLabel: string;
  Progress: number | null;
  Delta: number | null;
  IsFirst: boolean;
}

/**
 * HistoryRow (danh sách) — tái dùng chính `IPeriodOption` từ `GET /api/dashboard/periods`
 * (không cần endpoint history riêng, xem doc/contracts/dashboard.md CONTRACT DB-3), tự tính
 * delta so kỳ liền trước trong danh sách. Click "Xem" phát `view` để `DashboardPage` chuyển
 * sang xem đúng kỳ đó (đóng vòng lặp lịch sử ↔ toolbar).
 */
@Component({
  selector: 'app-history-list',
  standalone: true,
  imports: [DeltaIndicator],
  templateUrl: './history-list.html',
  styleUrl: './history-list.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HistoryList {
  readonly weeks = input.required<IPeriodOption[]>();
  readonly view = output<string>();

  protected readonly rows = computed<IHistoryRow[]>(() => {
    const sorted = [...this.weeks()].sort((a, b) => a.Date.localeCompare(b.Date));
    return sorted
      .map((w, i) => {
        const prev = i > 0 ? sorted[i - 1] : null;
        const delta =
          prev && prev.OverallProgress !== null && w.OverallProgress !== null
            ? w.OverallProgress - prev.OverallProgress
            : null;
        return {
          Value: w.Value,
          DateLabel: formatDateVn(w.Date),
          Progress: w.OverallProgress,
          Delta: delta,
          IsFirst: i === 0,
        };
      })
      .reverse();
  });
}

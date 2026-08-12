import { Component, input, output } from '@angular/core';

import { IPeriodOption } from '../../models/dashboard.model';

/**
 * "Lịch sử các kỳ đã lưu" — dumb, liệt kê các kỳ-tuần có dữ liệu **trong năm
 * đang chọn** (`WeeksInYear` từ `GET /api/dashboard/periods?year=`, xem
 * `doc/contracts/danh-muc-dti.md` CONTRACT DM-8). Khác bản thiết kế gốc (từng
 * giả định 1 endpoint liệt kê MỌI kỳ mọi năm) — API thật chỉ cung cấp theo
 * từng năm, nên `frontend-expert` thu hẹp phạm vi UI đúng theo khả năng thật
 * của backend thay vì giữ giả định cũ. Bấm "Xem" phát `view(option)` — page
 * cha tự chuyển bộ lọc về "Tuần" đúng kỳ đó.
 */
@Component({
  selector: 'app-history-list',
  standalone: true,
  templateUrl: './history-list.html',
  styleUrl: './history-list.scss'
})
export class HistoryList {
  readonly items = input.required<IPeriodOption[]>();
  readonly year = input.required<number>();

  readonly view = output<IPeriodOption>();

  formatDate(value: string): string {
    const [y, m, d] = value.slice(0, 10).split('-');
    return `${d}/${m}/${y}`;
  }

  formatPercent(value: number | null): string {
    return value === null || value === undefined ? '—' : `${value.toFixed(1)}%`;
  }
}

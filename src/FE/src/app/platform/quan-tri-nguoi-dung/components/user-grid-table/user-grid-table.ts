import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TableLazyLoadEvent, TableModule } from 'primeng/table';
import { IUser } from '../../models/quan-tri-nguoi-dung.model';

function formatDateVn(iso: string): string {
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return '—';
  return d.toLocaleDateString('vi-VN');
}

function initials(fullName: string): string {
  const parts = fullName.trim().split(/\s+/);
  const last = parts.at(-1)?.[0] ?? '';
  const secondLast = parts.length > 1 ? parts.at(-2)?.[0] ?? '' : '';
  return `${secondLast}${last}`.toUpperCase();
}

/**
 * Grid người dùng — `p-table` server-side pagination (`[lazy]`), khớp bố cục
 * `doc/Prototype/quan-tri-nguoi-dung.html` (avatar chữ cái đầu, role tag, badge trạng thái, cột
 * Hành động ghim phải). Dumb — không tự gọi service, chỉ phát output().
 */
@Component({
  selector: 'app-user-grid-table',
  standalone: true,
  imports: [TableModule],
  templateUrl: './user-grid-table.html',
  styleUrl: './user-grid-table.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserGridTable {
  readonly rows = input.required<IUser[]>();
  readonly loading = input<boolean>(false);
  readonly totalCount = input<number>(0);
  readonly page = input<number>(1);
  readonly pageSize = input<number>(10);

  readonly editRow = output<IUser>();
  readonly toggleLock = output<IUser>();
  readonly pageChange = output<{ Page: number; PageSize: number }>();

  protected readonly formatDateVn = formatDateVn;
  protected readonly initials = initials;

  onLazyLoad(event: TableLazyLoadEvent): void {
    const rows = event.rows ?? this.pageSize();
    const first = event.first ?? 0;
    const page = Math.floor(first / rows) + 1;
    this.pageChange.emit({ Page: page, PageSize: rows });
  }
}

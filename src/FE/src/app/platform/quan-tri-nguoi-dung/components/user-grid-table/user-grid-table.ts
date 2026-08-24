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
  /** Id người đang đăng nhập — `null` khi chưa biết (không chặn dòng nào trong lúc đó). Dùng để
   * chặn UI trước cho `USER.SELF_LOCK_FORBIDDEN` (doc/contracts/users.md §"Bảo vệ tài khoản quản
   * trị" luật #4) — áp cho MỌI role, chỉ chặn "Khoá", không chặn "Mở khoá". */
  readonly currentUserId = input<string | null>(null);

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

  /** Chỉ đúng khi đang bấm "Khoá" (chưa khoá) trên chính dòng của người đang đăng nhập. */
  isSelfLock(row: IUser): boolean {
    return !row.IsLocked && this.currentUserId() !== null && row.Id === this.currentUserId();
  }

  lockButtonTitle(row: IUser): string {
    if (this.isSelfLock(row)) return 'Không thể tự khoá tài khoản của chính mình — dùng Đăng xuất';
    return row.IsLocked ? 'Mở khoá tài khoản' : 'Khoá tài khoản';
  }
}

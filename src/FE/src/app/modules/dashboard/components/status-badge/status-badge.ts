import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { CriteriaBadge } from '../../models/dashboard.model';

const BADGE_CLASS: Record<CriteriaBadge, string> = {
  'Hoàn thành': 'bdone',
  'Đang thực hiện': 'bwork',
  'Không tăng': 'bstall',
  'Chưa có dữ liệu': 'bwork',
};

/**
 * Badge — 1 trong 12 component đã tài liệu hoá
 * (doc/Design/Frontend/PlatformManager/Components/Badge.md). Giá trị TÍNH RUNTIME phía BE (xem
 * doc/contracts/dashboard.md), component chỉ map text → class hiển thị, không tự tính lại.
 */
@Component({
  selector: 'app-status-badge',
  standalone: true,
  templateUrl: './status-badge.html',
  styleUrl: './status-badge.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StatusBadge {
  readonly status = input.required<CriteriaBadge | null>();

  protected readonly cssClass = computed(() => (this.status() ? BADGE_CLASS[this.status() as CriteriaBadge] : ''));
}

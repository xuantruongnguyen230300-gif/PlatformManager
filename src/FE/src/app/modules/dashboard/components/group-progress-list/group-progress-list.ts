import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { IGroupProgress } from '../../models/dashboard.model';

function formatPercent(v: number | null): string {
  return v === null ? '—' : `${v.toLocaleString('vi-VN', { minimumFractionDigits: 1, maximumFractionDigits: 1 })}%`;
}

function clampWidth(v: number | null): number {
  if (v === null) return 0;
  return Math.min(100, Math.max(0, v));
}

/** ProgressBar — 1 trong 12 component đã tài liệu hoá (Components/ProgressBar.md), danh sách tiến độ theo nhóm. */
@Component({
  selector: 'app-group-progress-list',
  standalone: true,
  templateUrl: './group-progress-list.html',
  styleUrl: './group-progress-list.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GroupProgressList {
  readonly groups = input.required<IGroupProgress[]>();
  protected readonly formatPercent = formatPercent;
  protected readonly clampWidth = clampWidth;
}

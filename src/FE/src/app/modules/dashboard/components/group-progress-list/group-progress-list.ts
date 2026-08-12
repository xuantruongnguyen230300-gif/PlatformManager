import { Component, input } from '@angular/core';

import { IDashboardGroupProgress } from '../../models/dashboard.model';

/**
 * "Tiến độ theo nhóm" — dumb component, render 6 thanh tiến độ theo
 * `IDashboardGroupProgress[]` đã tính sẵn từ server.
 */
@Component({
  selector: 'app-group-progress-list',
  standalone: true,
  templateUrl: './group-progress-list.html',
  styleUrl: './group-progress-list.scss'
})
export class GroupProgressList {
  readonly groups = input.required<IDashboardGroupProgress[]>();
  readonly subtitle = input('');

  formatPercent(value: number | null): string {
    return value === null || value === undefined ? '—' : `${value.toFixed(1)}%`;
  }
}

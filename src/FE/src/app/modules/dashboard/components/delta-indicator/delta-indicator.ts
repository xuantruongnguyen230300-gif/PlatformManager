import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

/** Epsilon so sánh delta — khớp business rule BE (Badge/up-down-flat), xem doc/contracts/dashboard.md. */
const EPSILON = 0.001;

export type DeltaDirection = 'up' | 'down' | 'flat';

function formatVn(n: number, digits = 1): string {
  return n.toLocaleString('vi-VN', { minimumFractionDigits: digits, maximumFractionDigits: digits });
}

/**
 * DeltaIndicator — 1 trong 12 component đã tài liệu hoá
 * (doc/Design/Frontend/PlatformManager/Components/DeltaIndicator.md), tái dùng ở KPI/bảng/lịch sử.
 */
@Component({
  selector: 'app-delta-indicator',
  standalone: true,
  templateUrl: './delta-indicator.html',
  styleUrl: './delta-indicator.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DeltaIndicator {
  readonly value = input.required<number | null>();
  readonly suffix = input<string>(' đ.%');

  protected readonly direction = computed<DeltaDirection>(() => {
    const v = this.value();
    if (v === null) return 'flat';
    if (v > EPSILON) return 'up';
    if (v < -EPSILON) return 'down';
    return 'flat';
  });

  protected readonly text = computed(() => {
    const v = this.value();
    if (v === null) return '—';
    const prefix = v > EPSILON ? '↑ +' : v < -EPSILON ? '↓ ' : '';
    return `${prefix}${formatVn(Math.abs(v))}${this.suffix()}`;
  });
}

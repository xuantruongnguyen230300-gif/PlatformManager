import { ChangeDetectionStrategy, Component, input } from '@angular/core';

export type KpiTone = 'default' | 'good' | 'warn' | 'bad';

/**
 * KpiTile — 1 trong 12 component đã tài liệu hoá (doc/Design/Frontend/PlatformManager/Components/KpiTile.md),
 * dùng 5 lần trong `kpi-summary`. Dumb — chỉ nhận input(), không tự tính toán nghiệp vụ.
 */
@Component({
  selector: 'app-kpi-tile',
  standalone: true,
  templateUrl: './kpi-tile.html',
  styleUrl: './kpi-tile.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class KpiTile {
  readonly label = input.required<string>();
  readonly value = input.required<string>();
  readonly sub = input<string>('');
  readonly tone = input<KpiTone>('default');
}

import { Component, computed, input } from '@angular/core';

import { IDashboardTrendPoint } from '../../models/dashboard.model';

interface IChartPoint {
  X: number;
  Y: number;
  Label: string;
  Value: number;
}

const WIDTH = 700;
const HEIGHT = 220;
const PAD_X = 28;
const PAD_Y = 18;

/**
 * Biểu đồ xu hướng tiến độ — dumb component, vẽ bằng inline SVG (không dùng
 * `<canvas>` để tránh phải bọc `isPlatformBrowser` cho thao tác vẽ 2D context
 * — SVG render hoàn toàn qua Angular binding khai báo, an toàn SSR mặc định).
 * Không nội suy điểm thiếu dữ liệu — `points` chỉ chứa kỳ có dữ liệu, đúng
 * `spec/dashboard-dti-weekly/ui-spec.md` mục 2.2/2.2b.
 */
@Component({
  selector: 'app-trend-chart',
  standalone: true,
  templateUrl: './trend-chart.html',
  styleUrl: './trend-chart.scss'
})
export class TrendChart {
  readonly points = input.required<IDashboardTrendPoint[]>();
  readonly subtitle = input('');

  readonly viewBox = `0 0 ${WIDTH} ${HEIGHT}`;

  readonly chartPoints = computed<IChartPoint[]>(() => {
    const points = this.points();
    const innerW = WIDTH - PAD_X * 2;
    const innerH = HEIGHT - PAD_Y * 2;
    const n = points.length;
    return points.map((p, i) => {
      const x = n <= 1 ? PAD_X : PAD_X + (innerW * i) / (n - 1);
      const clamped = Math.min(100, Math.max(0, p.Value));
      const y = PAD_Y + innerH * (1 - clamped / 100);
      return { X: x, Y: y, Label: p.Label, Value: p.Value };
    });
  });

  readonly polylinePoints = computed(() => this.chartPoints().map((p) => `${p.X},${p.Y}`).join(' '));

  readonly baselineY = HEIGHT - PAD_Y;
}

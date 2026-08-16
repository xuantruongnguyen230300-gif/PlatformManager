import { isPlatformBrowser } from '@angular/common';
import { ChangeDetectionStrategy, Component, PLATFORM_ID, computed, inject, input } from '@angular/core';
import { ChartData, ChartOptions } from 'chart.js';
import { ChartModule } from 'primeng/chart';
import { ITrendPoint } from '../../models/dashboard.model';

/**
 * Đọc màu từ đúng CSS custom property trong `src/styles.scss` (KHÔNG hardcode hex trong TS) —
 * đọc 1 lần bằng `getComputedStyle`, vì canvas 2D context của Chart.js không tự resolve chuỗi
 * `var(--x)` (khác CSS thường), xem doc/huong_dan/wiki-core/fe/12-charting.md và
 * doc/huong_dan/wiki-core/fe/04-design-token-system.md §Quy tắc dùng token trong component.
 */
function readCssVar(name: string, fallback: string): string {
  const value = getComputedStyle(document.documentElement).getPropertyValue(name).trim();
  return value || fallback;
}

function hexToRgba(hex: string, alpha: number): string {
  const clean = hex.replace('#', '');
  const r = parseInt(clean.substring(0, 2), 16);
  const g = parseInt(clean.substring(2, 4), 16);
  const b = parseInt(clean.substring(4, 6), 16);
  return `rgba(${r}, ${g}, ${b}, ${alpha})`;
}

/**
 * Biểu đồ xu hướng — PrimeNG `p-chart` (Chart.js), thay bản SVG vẽ tay của prototype nhưng GIỮ
 * đúng hành vi: clamp giá trị [0,100], KHÔNG nội suy điểm thiếu (chỉ vẽ điểm BE trả — đã lọc
 * null phía dưới), xem doc/huong_dan/wiki-core/fe/12-charting.md.
 */
@Component({
  selector: 'app-trend-chart',
  standalone: true,
  imports: [ChartModule],
  templateUrl: './trend-chart.html',
  styleUrl: './trend-chart.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TrendChart {
  private readonly platformId = inject(PLATFORM_ID);

  readonly points = input.required<ITrendPoint[]>();

  protected readonly hasData = computed(() => this.points().some((p) => p.Value !== null));

  // Đọc token 1 lần (màu không đổi runtime — chưa có dark mode/theme switch, xem
  // fe/04-design-token-system.md §Dark mode). Fallback = đúng giá trị hex hiện có trong
  // styles.scss, chỉ dùng khi SSR (chưa có `document`) hoặc token bị xoá nhầm.
  private readonly colors = isPlatformBrowser(this.platformId)
    ? {
        brand: readCssVar('--brand', '#0f5bd7'),
        muted: readCssVar('--muted', '#57647a'),
        line: readCssVar('--line', '#dfe6ef'),
      }
    : { brand: '#0f5bd7', muted: '#57647a', line: '#dfe6ef' };

  protected readonly chartData = computed<ChartData<'line'>>(() => {
    const pts = this.points().filter((p) => p.Value !== null);
    return {
      labels: pts.map((p) => p.Label),
      datasets: [
        {
          data: pts.map((p) => Math.min(100, Math.max(0, p.Value as number))),
          borderColor: this.colors.brand,
          backgroundColor: hexToRgba(this.colors.brand, 0.12),
          pointBackgroundColor: this.colors.brand,
          pointRadius: 4,
          tension: 0,
          fill: true,
          spanGaps: false,
        },
      ],
    };
  });

  protected readonly chartOptions = computed<ChartOptions<'line'>>(() => ({
    responsive: true,
    maintainAspectRatio: false,
    plugins: { legend: { display: false } },
    scales: {
      y: {
        min: 0,
        max: 100,
        ticks: { color: this.colors.muted, callback: (v) => `${v}%` },
        grid: { color: this.colors.line },
      },
      x: {
        ticks: { color: this.colors.muted },
        grid: { display: false },
      },
    },
  }));
}

# 12. Biểu đồ Dashboard

## Hiện trạng

`dashboard.html` vẽ đường xu hướng bằng `<canvas>` tay (không thư viện) —
`DESIGN.md` §Chart Palette ghi "None — app has no charts" vì đây được coi
là 1 hình vẽ đơn lẻ, không phải hệ chart có palette riêng.

## Đã CHỐT LẠI (2026-08-15) — `p-chart` của PrimeNG (Chart.js), không thêm `ng2-charts` riêng

Cùng quyết định đảo ngược ở [04-design-token-system.md](04-design-token-system.md)
— PlatformManager đã chọn PrimeNG làm component library chính, và PrimeNG có
sẵn `p-chart` (wrapper Angular cho Chart.js, cùng engine mà `ng2-charts`
dùng) — dùng thẳng `p-chart` thay vì thêm 1 dependency riêng cho cùng 1
việc. Vẫn giữ đúng ưu điểm đã chọn ban đầu: canvas-based (cùng tinh thần
cách vẽ tay hiện tại), nhẹ hơn nhiều so với ECharts.

```bash
npm install chart.js   # peer dependency của p-chart — primeng đã có sẵn phần trong package.json
```

```html
<!-- modules/dashboard/components/trend-chart/trend-chart.html -->
<p-chart type="line" [data]="chartData()" [options]="chartOptions" />
```

```ts
export class TrendChart {
  readonly chartData = input.required<ChartData<'line'>>();
  readonly chartOptions: ChartOptions<'line'> = {
    responsive: true,
    plugins: { legend: { display: false } },
    scales: { y: { ticks: { color: 'var(--muted)' } } },   // màu đọc từ token, không hardcode
  };
}
```

Component chart **luôn** là dumb component (`components/`, nhận `input()`
data đã map sẵn) — page/service tự fetch + map dữ liệu thô sang shape
`ChartData<T>` của Chart.js, không để component chart biết `HttpClient`.

## Ngưỡng nâng cấp lên `ngx-echarts`

Chuyển khi cần **≥1**: dashboard có >5 loại biểu đồ khác nhau cùng lúc, cần
tương tác sâu (zoom, drill-down, brush-select), hoặc render dataset lớn
(hàng nghìn điểm) mượt trên canvas/WebGL. Không chuyển "cho chắc" — chi phí
học ECharts + bundle nặng hơn đáng kể chỉ đáng khi đã chạm nỗi đau thật.
Khác với quyết định Grid (đã chốt PrimeNG ngay từ đầu vì nỗi đau gần như
chắc chắn), biểu đồ vẫn giữ nguyên tắc "đợi ngưỡng" — dashboard hiện chỉ
cần 1 line chart, chưa có bằng chứng domain đòi hỏi chart phức tạp sớm như
Grid.

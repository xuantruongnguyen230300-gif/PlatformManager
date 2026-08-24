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

## Accessibility — canvas không đọc được bằng screen reader

> Bổ sung 2026-08-24, đối chiếu thực hành ngành cho hệ thống tầm trung:
> `p-chart` (và Chart.js nói chung) vẽ lên `<canvas>` — khác SVG, canvas
> **không có cấu trúc DOM nào** để trình đọc màn hình bám vào. Với người
> dùng screen reader, `<canvas>` là 1 vùng trống tuyệt đối: không đọc được
> trục, không đọc được điểm dữ liệu, không đọc được xu hướng. Đây không phải
> lỗi cấu hình sai — là giới hạn cố hữu của canvas, phải bù bằng nội dung
> thay thế, không sửa được bằng thêm thuộc tính HTML lên chính `<canvas>`.

**2 lớp bù, áp cho biểu đồ mang thông tin nghiệp vụ quan trọng** (không cần
cho chart trang trí thuần):

1. **`aria-label` tóm tắt xu hướng**, không phải mô tả hình dạng ("đường màu
   xanh đi lên") mà mô tả **kết luận** ("Xu hướng chỉ tiêu DTI tăng đều
   62→88 điểm qua 6 tuần gần nhất"). Chuỗi này tính từ cùng dữ liệu đã map
   cho `chartData()`, không phải chuỗi tĩnh viết tay — lệch giữa 2 bên còn
   tệ hơn không có `aria-label`.

```html
<div [attr.aria-label]="chartSummary()" role="img">
  <p-chart type="line" [data]="chartData()" [options]="chartOptions" />
</div>
```

2. **Bảng dữ liệu thay thế**, ẩn bằng class ẩn-thị-giác-giữ-AT (không
   `display: none` hay `@if` — cả 2 cách đó xoá luôn khỏi DOM, screen reader
   cũng bỏ qua theo). `@angular/cdk` (đã là dependency, xem
   [13-performance.md](13-performance.md) §3) có sẵn class `cdk-visually-hidden`
   qua mixin `a11y-visually-hidden` — include 1 lần trong stylesheet toàn cục:

```scss
// styles.scss — include 1 lần
@use '@angular/cdk' as cdk;
@include cdk.a11y-visually-hidden();
```

```html
<table class="cdk-visually-hidden">
  <caption>{{ chartSummary() }}</caption>
  <tr><th>Tuần</th>@for (p of dataPoints(); track p.week) {<th>{{ p.week }}</th>}</tr>
  <tr><th>Điểm</th>@for (p of dataPoints(); track p.week) {<td>{{ p.value }}</td>}</tr>
</table>
```

## Re-render khi data đổi liên tục — `update()`, không destroy/recreate

> Bổ sung 2026-08-24, đối chiếu thực hành ngành cho hệ thống tầm trung:
> dashboard tự refresh định kỳ (polling) là kịch bản gần như chắc chắn xảy
> ra ở màn hình dashboard DTI — chưa có cảnh báo nào về chi phí re-render
> chart mỗi vòng lặp.

`p-chart` phân biệt 2 cách nhận dữ liệu mới, chi phí khác hẳn nhau:

| Cách cập nhật | `p-chart` làm gì | Chi phí |
| --- | --- | --- |
| Gán **object `ChartData` mới** vào input `[data]` (spread/immutable — đúng cách Signals đang dùng toàn repo) | Nhận diện đổi reference → gọi `chart.update()` nội bộ, Chart.js chỉ vẽ lại phần đổi, animation nối tiếp mượt | Thấp |
| Gọi `reinit()`, hoặc bọc `<p-chart>` trong `@if` rồi toggle điều kiện đó mỗi lần data đổi | Chart.js `destroy()` rồi dựng lại **toàn bộ** canvas từ đầu — animation giật, tốn CPU | Cao, **tăng dần** theo tần suất refresh (vd polling 5s/lần) |

Quy tắc: polling cập nhật `chartData` bằng **object mới**, không mutate mảng
cũ tại chỗ — mutate tại chỗ thì Angular/PrimeNG **không** phát hiện được đổi
gì nên chart đứng im (khác lỗi ở trên nhưng cùng gốc: đọc lại cảnh báo
mutate-vs-signal ở [13-performance.md](13-performance.md) §1). Và **không**
đặt `@if` bao quanh riêng `<p-chart>` chỉ để ép re-render — nếu cần ẩn/hiện
chart theo điều kiện thật (chưa có data), tách biến điều kiện đó khỏi biến
polling data, đừng dùng chung 1 điều kiện cho cả 2 việc.

## Responsive trên mobile — chart co nhỏ thì đổi cách trình bày, không thu nhỏ mù

> Bổ sung 2026-08-24, đối chiếu thực hành ngành cho hệ thống tầm trung:
> `responsive: true` trong `chartOptions` mẫu ở trên co giãn được kích thước
> canvas theo khung chứa, nhưng co kích thước không tự làm biểu đồ **đọc
> được** — 6 tuần dữ liệu nhét vào ~320px ngang thường thành 1 dải nhãn trục
> X chồng chữ lên nhau, không đọc nổi trên điện thoại thật.

Không có công thức chung cho mọi chart — chọn 1 trong 3 theo dữ liệu thật:

- **Giảm số điểm hiển thị** trên viewport hẹp (vd chỉ hiện 4 tuần gần nhất
  thay vì 12, còn lại xem qua bảng dữ liệu đầy đủ ở mục Accessibility trên)
  — rẻ nhất, phù hợp khi xu hướng gần đây quan trọng hơn lịch sử đầy đủ trên
  màn hình nhỏ.
- **Đổi loại chart** khi mật độ điểm là vấn đề gốc — line chart nhiều điểm
  dồn thành bar chart theo kỳ gộp lớn hơn (tuần → tháng) dễ đọc hơn trên
  màn hẹp mà không mất thông tin quan trọng.
- **Cho phép pan/zoom** (plugin `chartjs-plugin-zoom`) chỉ khi 2 lựa chọn
  trên không đủ — thêm dependency + thêm thao tác cho user, chỉ đáng khi
  chart có nhiều chục điểm trở lên mà **không thể** rút gọn hợp lý (khác
  domain PlatformManager hiện tại — dashboard DTI theo tuần/tháng có trần
  điểm dữ liệu tự nhiên thấp, xem §Ngưỡng nâng cấp `ngx-echarts` ở trên).

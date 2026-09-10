---
kind: luat
scope: core
verified: 2026-09-06
---

# 12. Biểu đồ Dashboard

> ## ✅ BIỂU ĐỒ ĐẦU TIÊN ĐÃ VỀ (đối chiếu 2026-09-10)
>
> Hướng đã chốt (PrimeNG `p-chart`) giữ nguyên, và nay **có một hiện thực để soi** thay vì chỉ
> có mẫu: `src/FE/src/app/modules/dashboard/components/trend-chart/trend-chart.ts`. Đo lại thay
> vì tin câu này:
>
> ```bash
> grep -rn "p-chart\|primeng/chart\|chart.js" src/FE/src --include=*.ts --include=*.html | grep -v spec
> grep -n '"chart.js"' src/FE/package.json
> ```
>
> PASS = cả hai lệnh in ra dòng. Thứ **chưa** có là một **trang** dùng nó: `TrendChart` đã dựng
> và có test, nhưng `modules/dashboard/pages/dashboard/` còn là khung nên chưa ai lắp vào — vì
> vậy `chart.js` chưa xuất hiện trong bundle của `ng build`.

## Hiện trạng

✅ **CÓ THẬT (đối chiếu 2026-09-10)** — đúng **một** biểu đồ trong app: `TrendChart`, đường xu
hướng của Dashboard DTI, vẽ bằng `p-chart` (`import { ChartModule } from 'primeng/chart'` —
`src/FE/src/app/modules/dashboard/components/trend-chart/trend-chart.ts:4`). `chart.js` đã cài lại
ở `src/FE/package.json:68`, lý do ghi tại chỗ trong khối `//dependencies`
(`src/FE/package.json:49`).

> 🔄 **LẬT 2026-09-10 — mục này (và banner trên đầu file) nói dối theo đúng khuôn đã bị bắt hai
> lần ở `08-i18n.md`: số đo chạy trước, văn xuôi đứng yên.** Bản 2026-09-06 viết *"không màn hình
> nào trong `src/FE` có biểu đồ, và `chart.js` không nằm trong `src/FE/package.json`"*, kèm một
> lệnh `grep` và lời hứa *"hôm nay: 0 dòng"*. Cả ba mệnh đề đó hết hạn ngày 2026-09-09 khi
> `TrendChart` được dựng lại và `chart.js` được thêm lại — nhưng đoạn văn thì không ai chạy, nên
> không ai thấy.
>
> Bài học đã có sẵn ở `08-i18n.md` §Điểm xuất phát và lần này chỉ xác nhận thêm: cái sống sót là
> **lệnh + tiêu chí PASS**, cái chết là **con số và câu tường thuật kèm theo**
> ([`.claude/CLAUDE.md`](../../../../.claude/CLAUDE.md) §6). Banner mới ở trên vì vậy không chép
> lại kết quả `grep`, nó chỉ nêu PASS.

## Đã CHỐT LẠI (2026-08-15) — `p-chart` của PrimeNG (Chart.js), không thêm `ng2-charts` riêng

Cùng quyết định đảo ngược ở [04-design-token-system.md](04-design-token-system.md)
— PlatformManager đã chọn PrimeNG làm component library chính, và PrimeNG có
sẵn `p-chart` (wrapper Angular cho Chart.js, cùng engine mà `ng2-charts`
dùng) — dùng thẳng `p-chart` thay vì thêm 1 dependency riêng cho cùng 1
việc. Vẫn giữ đúng ưu điểm đã chọn ban đầu: canvas-based (cùng tinh thần
cách vẽ tay hiện tại), nhẹ hơn nhiều so với ECharts.

```bash
npm install chart.js   # peer dependency của p-chart — đã cài lại 2026-09-09 (package.json:68)
```

```html
<!-- modules/dashboard/components/trend-chart/trend-chart.html -->
<p-chart type="line" [data]="chartData()" [options]="chartOptions()" />
```

Component chart **luôn** là dumb component (`components/`, nhận `input()`
data đã map sẵn) — page/service tự fetch + map dữ liệu thô sang shape
`ChartData<T>` của Chart.js, không để component chart biết `HttpClient`.

### 🛑 Màu KHÔNG truyền được bằng `var(--token)` — canvas không phân giải CSS

Đây là bẫy đắt nhất của cả file, vì nó **không gây lỗi**: chart.js đưa thẳng chuỗi màu nhận được
vào `fillStyle` / `strokeStyle` của canvas 2D context. Chuỗi `'var(--muted)'` không phải màu hợp
lệ ở đó, nên canvas **âm thầm** lùi về màu mặc định (đen) — không cảnh báo, không exception,
build xanh, lint xanh. Người viết thấy chart vẽ ra bình thường và tưởng token đã áp.

Cơ chế **chạy được**: đọc token qua `getComputedStyle` **một lần** rồi truyền **chuỗi literal**
xuống thư viện.

```ts
// Rút gọn từ code đang chạy — bản đầy đủ ở
// src/FE/src/app/modules/dashboard/components/trend-chart/trend-chart.ts
export class TrendChart {
  private readonly document = inject(DOCUMENT);
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));

  /** Không phụ thuộc signal nào ⇒ chạy đúng một lần. `getComputedStyle` ép trình duyệt tính lại
   *  layout, nên gọi nó ở mỗi lần vẽ là trả giá thật. */
  private readonly palette = computed(() => ({
    series: this.readToken('--brand'),
    axisLabel: this.readToken('--muted'),
    grid: this.readToken('--line'),
  }));

  protected readonly chartOptions = computed<ChartOptions<'line'>>(() => {
    const palette = this.palette();
    return {
      responsive: true,
      maintainAspectRatio: false,          // bắt buộc khi chiều cao do SCSS quyết định
      plugins: { legend: { display: false } },
      scales: {
        x: { grid: { display: false }, ticks: { color: palette.axisLabel } },
        y: { grid: { color: palette.grid }, ticks: { color: palette.axisLabel } },
      },
    };
  });

  /** Ngoài trình duyệt trả chuỗi rỗng — chart.js khi đó lùi về màu mặc định của nó, và ca đó
   *  không bao giờ nhìn thấy được vì `p-chart` cũng chỉ dựng canvas trên trình duyệt. */
  private readToken(name: string): string {
    if (!this.isBrowser) return '';
    return getComputedStyle(this.document.documentElement).getPropertyValue(name).trim();
  }
}
```

Ba hệ quả kéo theo, đều đã ở trong code thật:

1. **`chartOptions` phải là `computed`, không phải field cố định.** Nó đọc `palette()`, mà
   `palette()` chỉ có giá trị sau khi có DOM. Mẫu cũ khai `chartOptions` là field khởi tạo trong
   constructor — với màu literal thì kiểu gì cũng sai thời điểm.
2. **Cần alpha thì tự ghép, đừng viết `rgba(var(--brand), .12)`** — cùng lý do trên, canvas không
   hiểu. Code thật có hàm `withAlpha()` chuyển `#rrggbb` → `rgba(r, g, b, a)`.
3. **Không dựng token `--chart-*` mới trong `styles.scss` cho việc này.** Bốn vai trò `chart-*`
   cố ý **không** là CSS custom property; quyết định và phép kiểm (`grep -c 'chart' src/FE/src/styles.scss`
   PASS = 0) nằm ở file chủ `doc/Design/Frontend/PlatformManager/Tokens/colors.md` § Chart Palette.
   Thêm chúng vào `:root` là dựng bản sao thứ hai của `--brand` / `--muted` / `--line` rồi để hai
   bản lệch nhau khi đổi bảng màu.

✅ **CÓ THẬT (đối chiếu 2026-09-10)** — cơ chế trên đang chạy:
`src/FE/src/app/modules/dashboard/components/trend-chart/trend-chart.ts:244-247` (`readToken`),
`:150-164` (`palette`), `:189-219` (`chartOptions` dùng `palette.axisLabel` / `palette.grid`),
và lý do viết thành JSDoc ở `:92-97`.

> 🔄 **SỬA 2026-09-10 — mẫu cũ ở mục này KHÔNG CHẠY ĐƯỢC, và nó là bẫy chép-dán.** Bản trước in
> nguyên văn `scales: { y: { ticks: { color: 'var(--muted)' } } }` kèm chú thích *"màu đọc từ
> token, không hardcode"* — tức nó **quảng cáo đúng thứ nó làm sai**. Người tiếp theo mở file chủ
> về biểu đồ ra chép sẽ nhận một biểu đồ đen mà không hiểu vì sao.
>
> Đáng nói là **chính file này đã biết sự thật** ở §Accessibility ngay bên dưới (*"canvas không có
> cấu trúc DOM nào… không sửa được bằng thêm thuộc tính HTML lên chính `<canvas>`"*), và
> `Tokens/colors.md` § Chart Palette cũng đã chốt ngược lại. Ba nguồn trong cùng một repo, hai
> nguồn đúng, một nguồn sai — và nguồn sai là nguồn có **code để chép**.

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

> Đây là **phần a11y thuộc chủ đề biểu đồ** — file chủ giữ nguyên ở đây. Điểm vào
> chung cho a11y: [15-accessibility.md](15-accessibility.md).

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
   cũng bỏ qua theo).

   🔄 LẬT 2026-09-06: bản trước ghi `@angular/cdk` *"đã là dependency"* và dẫn sang
   [13-performance.md](13-performance.md) §3 làm bằng chứng. **Chưa hề cài** — cùng câu sai này
   từng xuất hiện ở ba file (`13-performance.md` §3, `05-component-library.md` §Tab order, và
   đây), tất cả cùng trỏ vòng vào nhau. Dùng `cdk.a11y-visually-hidden()` **có** thêm một phụ
   thuộc mới; nếu không muốn, tự viết class `.visually-hidden` trong `styles.scss` cũng đủ.

   Mixin của CDK (khi đã cài) — include 1 lần trong stylesheet toàn cục:

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

# 13. Performance

## 1. Zoneless — đòn bẩy lớn nhất, làm ngay từ đầu

Angular 20.2+ đã ổn định `provideZonelessChangeDetection()` — bỏ hẳn
`zone.js`, change detection chạy theo đúng signal graph thay vì patch mọi
API async rồi check tràn lan. Codebase đã 100% signals-first (không dùng
`@Input()`/decorator cũ) nên gần như không cần sửa gì để tương thích.

```ts
// app.config.ts
export const appConfig: ApplicationConfig = {
  providers: [
    provideZonelessChangeDetection(),
    // ... các provider khác
  ],
};
```

```diff
  // package.json
- "zone.js": "~0.15.0"
```

**Kiểm chứng sau khi bật:** mọi UI cập nhật qua `signal.set()`/`patchState()`
vẫn phải render đúng — nếu có component nào tự mutate object/array **không**
qua signal rồi mong Angular tự phát hiện, nó sẽ **không** còn cập nhật (đây
chính là cạm bẫy phổ biến nhất khi chuyển sang zoneless) — audit lại chỗ
nào từng dựa vào Zone.js "tự động bắt được" thay đổi trước khi coi việc bật
zoneless là xong.

## 2. `@defer` cho khối nặng/dưới màn hình đầu

```html
@defer (on viewport) {
  <app-trend-chart [chartData]="chartData()" />
} @placeholder {
  <div class="chart-skeleton"></div>
}
```

Áp dụng cho: chart, history list (dashboard), import dialog (danh-muc-dti)
— những khối không cần cho lần render đầu tiên. Tách được cả chunk JS của
`p-chart`/Chart.js ra khỏi bundle route chính — quan trọng hơn từ khi
PrimeNG trở thành dependency chính (xem
[04-design-token-system.md](04-design-token-system.md)), vì tổng bundle
PrimeNG lớn hơn đáng kể so với phương án hand-rolled trước đây — `@defer`
là công cụ chính để bù lại chi phí bundle đó.

## 3. Virtual scroll khi danh sách dài

`CdkVirtualScrollViewport` (`@angular/cdk/scrolling`) khi 1 danh sách/bảng
vượt quá khả năng phân trang server hợp lý (hiếm, vì server-side pagination
— mục 5 — đã là mặc định) hoặc cho danh sách không phù hợp phân trang (vd
dropdown lookup nhiều trăm mục). Cùng package `@angular/cdk` đã thêm ở
[11-grid-and-metadata.md](11-grid-and-metadata.md).

## 4. Bundle budget — chặn phình to âm thầm

```json
// angular.json
"budgets": [
  { "type": "initial", "maximumWarning": "500kb", "maximumError": "1mb" },
  { "type": "anyComponentStyle", "maximumWarning": "4kb", "maximumError": "8kb" }
]
```

CI fail khi vượt `maximumError` — đây là gate G7, thêm vào
[trien-khai/05-gate.md](trien-khai/05-gate.md). Ngưỡng cụ thể điều chỉnh
theo thực tế đo được sau F4, không giữ số mặc định của `ng new` mãi mãi.

## 5. Server-side pagination là mặc định, không phải ngoại lệ

Đã đúng ở `danh-muc-dti` (`GetGrid` nhận `Page`/`PageSize`) — giữ nguyên
làm quy tắc cho **mọi** danh sách có khả năng vượt vài chục dòng. Client-side
filter/sort trên toàn bộ dataset chỉ chấp nhận được cho tập nhỏ đã tải hết
(vd danh sách 6 `CriteriaGroup` cố định).

## 6. Debounce input tìm kiếm — đã có, chuẩn hoá thành quy tắc

`onSearchInput` (`danh-muc-dti.page.ts`) debounce 300ms trước khi gọi API —
áp dụng quy tắc này cho **mọi** ô tìm kiếm gọi API, không để mỗi feature tự
chọn số debounce khác nhau (chuẩn: 300ms).

## Chưa cần (Nhóm B)

- `NgOptimizedImage` — chưa có ảnh thật nào ngoài favicon/logo (nếu có sau
  này, bật ngay vì chi phí gần 0).
- Service Worker/offline cache — chưa có yêu cầu dùng offline.

---

## 7. Core Web Vitals — ngưỡng cụ thể đo bằng Lighthouse (lab), khác RUM đã hoãn

> Bổ sung 2026-08-24, đối chiếu thực hành ngành cho hệ thống tầm trung: mục
> 1–6 ở trên tối ưu đúng kỹ thuật nhưng không neo vào **ngưỡng đo được** nào
> — "nhanh" không kèm số là khẩu hiệu, không phải quy tắc kiểm được. Khác
> việc đo Core Web Vitals bằng **RUM** (`web-vitals` library trên traffic
> thật) đã bàn và **cố ý hoãn** ở [10-observability.md](10-observability.md)
> mục "Vẫn hoãn" (hoãn vì chưa đủ user đồng thời để số liệu có ý nghĩa thống
> kê) — mục này là đo **lab** (Lighthouse, chạy trên 1 máy, không cần user
> thật), nên không vướng lý do hoãn đó và có thể làm ngay.

| Chỉ số | Ngưỡng "Good" | Ý nghĩa | Liên hệ trong file này |
| --- | --- | --- | --- |
| LCP (Largest Contentful Paint) | ≤ 2.5s | Phần tử lớn nhất trong viewport render xong | Bundle to (§4) trực tiếp trì hoãn LCP — JS chặn trước khi nội dung chính vẽ ra |
| INP (Interaction to Next Paint) | ≤ 200ms | Độ trễ từ lúc user thao tác tới lúc frame kế tiếp lên hình | Zoneless (§1) cải thiện trực tiếp — mỗi thao tác chỉ chạy đúng phần signal graph liên quan, không quét toàn bộ cây component |
| CLS (Cumulative Layout Shift) | ≤ 0.1 | Tổng mức layout bị dịch chuyển ngoài ý muốn | `@placeholder` của `@defer` (§2) phải giữ đúng kích thước khối thật — placeholder sai kích thước tự nó gây CLS khi nội dung thật load xong rồi đẩy layout |

Đo bằng Lighthouse CLI, không cần traffic thật:

```bash
npx lighthouse http://localhost:4200/dashboard --only-categories=performance --output=json --output-path=./lighthouse-report.json
```

Chạy tay trước release, cùng cách 4 lệnh gate khác đang chạy — repo chưa có
CI (xem [trien-khai/05-gate.md](trien-khai/05-gate.md)). G7 ở đó đã gate
bundle size bằng số cụ thể; Lighthouse CI (`@lhci/cli` với `assert` ngưỡng
LCP/INP/CLS) là bước tự nhiên tiếp theo khi cần thêm gate — chưa thêm ở đây
vì chưa có số đo thật trên bundle PrimeNG hiện tại để biết ngưỡng nào khả
thi. Đo trước, gate sau — cùng nguyên tắc "không cache khi chưa đo" ở
[../be/11-performance-caching.md](../be/11-performance-caching.md) §5.

## 8. Prefetch route kế tiếp — `PreloadingStrategy` có chủ đích, không tải hết

Mọi route hiện lazy-load — nghĩa là lần đầu điều hướng sang 1 route luôn trả
giá tải chunk JS của đúng route đó, kể cả khi route đó gần như chắc chắn
được vào tiếp theo (vd sau đăng nhập luôn vào dashboard). Angular Router có
`PreloadingStrategy` chạy **nền, sau khi route đầu đã render xong** — tải
trước chunk của route khác mà **không chặn** lần render đầu tiên, khác hẳn
"tải hết ngay từ đầu" (sẽ phá bundle budget ở §4).

`PreloadAllModules` (built-in) đơn giản nhất nhưng tải **mọi** route sau đó,
kể cả route hiếm dùng — ngược tinh thần "chỉ xây/tải khi chạm đúng nỗi đau"
xuyên suốt bộ tài liệu này. Ưu tiên chiến lược chọn lọc qua route `data`:

```ts
// core/routing/selective-preload.strategy.ts
@Injectable({ providedIn: 'root' })
export class SelectivePreloadStrategy implements PreloadingStrategy {
  preload(route: Route, load: () => Observable<unknown>): Observable<unknown> {
    return route.data?.['preload'] ? load() : of(null);
  }
}
```

```ts
// dashboard.routes.ts
{ path: 'danh-muc-dti', loadComponent: () => import('./danh-muc-dti.page'), data: { preload: true } }
```

```ts
// app.config.ts
provideRouter(routes, withPreloading(SelectivePreloadStrategy)),
```

Chỉ đánh dấu `preload: true` cho route có bằng chứng người dùng vào tiếp
theo với xác suất cao trong luồng chính — không đánh dấu tràn lan, cùng lý do
`PreloadAllModules` bị loại ở trên.

## 9. Script bên thứ 3 — nguyên tắc phòng ngừa trước khi có script đầu tiên

Chưa có script bên thứ 3 nào hôm nay (không analytics, không chat widget) —
nhưng đây đúng loại quyết định hay bị làm sai **ngay lần đầu tiên** rồi
không ai sửa lại, cùng bản chất với cạm bẫy rate limiting đã ghi ở
[../be/09-security-beyond-auth.md](../be/09-security-beyond-auth.md): cấu
hình sai không gây lỗi biên dịch, không crash — chỉ âm thầm làm chậm. Thẻ
`<script src="...">` không có `async`/`defer` **chặn parser HTML** cho tới
khi tải xong toàn bộ file — với script phục vụ từ CDN bên thứ 3 (độ trễ mạng
không kiểm soát được), đây phá thẳng ngưỡng LCP vừa đặt ở §7.

```html
<!-- ĐÚNG — không chặn parser -->
<script src="https://cdn.example.com/analytics.js" defer></script>

<!-- SAI — chặn render tới khi tải xong -->
<script src="https://cdn.example.com/analytics.js"></script>
```

- Ưu tiên `defer` hơn `async` cho phần lớn trường hợp (analytics, chat
  widget) — `defer` giữ đúng thứ tự thực thi và chỉ chạy sau khi HTML parse
  xong; `async` chạy ngay khi tải xong, có thể chen ngang giữa lúc parser
  đang chạy, khó đoán thời điểm hơn.
- SDK có gói `npm` chính thức (không phải `<script>` tag rời) thì load
  **sau** khi app đã render lần đầu — trong `@defer (on idle)` (§2) hoặc một
  provider tách riêng — không đăng ký ngay lúc bootstrap.
- Không thêm `<script>` chặn trong `index.html` chỉ vì tiện, nhất là cho bất
  cứ thứ gì không phục vụ lần render đầu tiên.

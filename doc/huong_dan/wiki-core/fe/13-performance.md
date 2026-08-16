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

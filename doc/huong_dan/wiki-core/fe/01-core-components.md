# 1. Core FE thật sự cần cho PlatformManager

## Nguyên tắc chọn lọc — Nhóm A vs Nhóm B

Cùng nguyên tắc với [be/01-core-components.md](../be/01-core-components.md):
core không phải "thêm càng nhiều abstraction càng chuyên nghiệp". Mỗi thành
phần dưới đây giải quyết 1 nỗi đau *thật* của việc dựng SPA nhiều màn hình,
nhiều người dùng cùng lúc — chỉ xây khi hệ thống đã/sắp chạm đúng nỗi đau đó.

**Khác biệt với BE về nguồn tham chiếu:** `wiki-core/be/` đúc kết từ đối
chiếu với 1 backend .NET production thật (VNR.Successor). Không có
"VNR.Successor frontend" tương đương để soi — nguồn ở đây là (1) kiến trúc
chính thức của Angular (signals, standalone, Angular CLI conventions —
framework đã áp đặt sẵn rất nhiều quyết định mà backend không có), (2) hệ
thống thiết kế thật của chính PlatformManager
(`doc/Design/Frontend/PlatformManager/`), và (3) kiến thức kiến trúc SPA đã
ổn định lâu năm ngoài Angular (ranh giới DTO/model, error boundary,
design-token pipeline). Vì vậy các file `fe/` trích dẫn tài liệu Angular
chính thức hoặc `doc/Design/` thay vì "dòng X file Y của VNR.Successor" như
bên `be/`.

## Danh sách thành phần core

| # | Thành phần | Nỗi đau nó giải quyết | Mức ưu tiên |
|---|---|---|---|
| 1 | **HTTP client + envelope-aware interceptor** | Đọc sai/đoán field response → nuốt mất message lỗi nghiệp vụ | Bắt buộc, ngày đầu |
| 2 | **DTO ↔ Model mapper** | Đổi field API không vỡ UI âm thầm (type bị xoá lúc runtime) | Bắt buộc, ngày đầu |
| 3 | **Design-token bridge** | Màu/spacing rải rác, đổi theme phải sửa N nơi | Bắt buộc, ngày đầu |
| 4 | **Thư viện component dùng chung** — PrimeNG cho phần tương tác phức tạp, hand-rolled cho phần đơn giản | Viết lại nút/thẻ/badge mỗi màn hình; tự viết Grid/Chart/input phức tạp tốn công + rủi ro mở rộng (xem [05-component-library.md](05-component-library.md)) | Bắt buộc, ngày đầu |
| 5 | **State pattern (`signal()` → `signalStore()` có điều kiện)** | Prop-drilling hoặc state trùng lặp giữa component | Bắt buộc, ngày đầu |
| 6 | **Auth/current-user + route guard** | Chặn nhầm/không chặn route cần quyền | Bắt buộc, ngay khi auth thật lên |
| 7 | **Form & validation display** | Form phức tạp tự chế mỗi nơi, lỗi field không nhất quán | Nên có sớm |
| 8 | **Testing (mapper/service trước)** | Lỗi wire boundary chỉ lộ ra khi chạy thật | Bắt buộc, ngày đầu |
| 9 | **Notification/toast abstraction** | Mỗi feature tự viết cách báo lỗi/thành công | Nên có sớm |
| 10 | **Observability phía client** (correlation với `traceId` BE) | Không tra được request nào gây lỗi khi user báo cáo sự cố | Khi chuẩn bị lên production |
| 11 | **i18n scaffolding** (`$localize`, chưa cần bật đa-locale) | Viết lại toàn bộ chuỗi khi bật đa ngôn ngữ | Nên có sớm — thư viện đã chốt |
| 12 | **Responsive/breakpoint token hoá** | Mỗi component tự định nghĩa `@media` riêng, không đồng bộ | Nên có sớm |
| 13 | **Grid engine + đồng bộ metadata với BE** | Tự viết grid nâng cao tốn kém, rủi ro mở rộng thật trong domain ERP/chuyển đổi số; menu/cột grid do BE điều khiển không có hợp đồng chung | **PrimeNG `p-table` ngay** (đã đảo ngược quyết định "đợi ngưỡng"), metadata JSON đã thiết kế sẵn — xem [11-grid-and-metadata.md](11-grid-and-metadata.md) |
| 14 | **Biểu đồ (charting)** | Tự vẽ canvas tay không mở rộng được khi cần nhiều loại biểu đồ | **PrimeNG `p-chart`** (Chart.js) — xem [12-charting.md](12-charting.md) |
| 15 | **Performance (zoneless, defer, virtual scroll, bundle budget)** | Zone.js overhead, bundle phình to âm thầm, list dài giật lag | Bắt buộc, ngày đầu — xem [13-performance.md](13-performance.md) |
| 16 | **Isolation lỗi runtime theo từng vùng UI** (`ErrorHandler` toàn cục + cô lập cục bộ tại nơi rủi ro) | 1 widget lỗi (biểu đồ, tính toán phức tạp) không được kéo theo cả trang, và không ai biết lỗi vừa xảy ra | Bắt buộc, ngày đầu (`ErrorHandler`) — cô lập từng widget chỉ khi widget đó rủi ro cao, xem mục dưới |
| 17 | **Runtime environment config** (1 bundle build ra chạy được nhiều môi trường, không hardcode lúc build) | Build lại riêng cho từng môi trường tốn CI; artifact test ở staging khác artifact thật sự deploy production | Nên có sớm — khi có ≥2 môi trường triển khai thật (staging + production) |
| 18 | **Feature flag / kill switch** cho tính năng rủi ro cao | Tắt nhanh 1 tính năng đang lỗi mà không cần `git revert` + build + deploy lại | Khi rollout 1 tính năng rủi ro cao lần đầu, không phải mặc định cho mọi feature |
| 19 | **Phát hiện mất kết nối mạng** (`navigator.onLine` + phân biệt lỗi mạng với lỗi nghiệp vụ) | Lỗi mạng hiện thành thông báo mơ hồ giống lỗi nghiệp vụ thật, sai hướng xử lý của user | Nên có sớm — chi phí gần bằng 0 |

## Áp dụng vào PlatformManager

Hiện đã có #2, #5 (đúng ngưỡng), #9 (mức tối giản) qua các mapper trong
`modules/*/services/*.service.ts`, quy ước `state/*.store.ts` trong
`architecture.md`, và `shared/components/toast`. **Chưa có** #1 đúng chuẩn
(đang đọc field envelope cũ — xem [02-http-envelope.md](02-http-envelope.md)),
#3 một phần (token tồn tại trong `styles.scss` nhưng 9 chỗ vẫn hardcode hex
— xem [04-design-token-system.md](04-design-token-system.md)), #4 thiếu
trạng thái tương tác (chính `doc/Design/.../COMPONENTS.md` tự ghi nhận), #6
(chặn bởi quyết định BE, nay đã chốt cookie session — xem
[07-auth-identity.md](07-auth-identity.md)), #8 gần như 0%. #10 chưa cần
(chưa production), #11 mới chốt thư viện chưa bật, #12 có breakpoint trong
prototype nhưng chưa hệ thống hoá.

## Bổ sung 2026-08-24 — đối chiếu thực hành ngành cho hệ thống tầm trung: 4 khoảng trống

> Đối chiếu bảng 15 thành phần ở trên với thực hành thật của senior frontend
> tại hệ thống tầm trung (5-15 dev, user thật), cùng nguyên tắc Nhóm A/B ở
> đầu file này: 4 điểm dưới đây trước đó **không có một dòng nào** trong toàn
> bộ `fe/` — kể cả [10-observability.md](10-observability.md) (nơi gần nhất
> bàn xử lý lỗi phía client) và [13-performance.md](13-performance.md) (nơi
> gần nhất bàn Service Worker/offline). Đã thêm 4 dòng #16-19 vào bảng trên;
> chi tiết + code mẫu ở dưới, cùng tinh thần với các mục đối chiếu ngành đã
> làm bên `be/` ([02-identity-auth.md](../be/02-identity-auth.md) §CSRF,
> [11-performance-caching.md](../be/11-performance-caching.md) §9).

### #16 — Isolation lỗi runtime: Angular KHÔNG có Error Boundary như React

**Vì sao vấn đề THẬT:** React có `componentDidCatch`/`getDerivedStateFromError`
— component cha thật sự "bắt" được lỗi ném ra từ quá trình render của
component con, nên thay được đúng vùng con đó bằng fallback UI mà không ảnh
hưởng phần còn lại của trang. **Angular không có API tương đương.** Không có
lifecycle hook nào cho phép 1 component bắt lỗi render của component con
được chiếu qua `<ng-content>`/`@if` — lỗi ném ra trong quá trình change
detection của một component đi thẳng lên `ErrorHandler` toàn cục, không dừng
lại ở component cha gần nhất.

Hệ quả nếu không xử lý gì: 1 lỗi runtime trong **bất kỳ** component nào (vd
biểu đồ nhận dữ liệu dạng lạ, phép tính client-side chia cho 0) làm lượt
change detection đang chạy dừng giữa chừng — phần UI đã render trước đó vẫn
còn trên DOM (không phải "màn hình trắng" tuyệt đối), nhưng các signal cập
nhật sau đó không còn chắc phản ánh đúng lên vùng bị lỗi, và người dùng không
có cách nào biết đang nhìn dữ liệu cũ hay lỗi thật.

**2 lớp bắt buộc, không thay thế nhau:**

**Lớp 1 — `ErrorHandler` toàn cục, lưới an toàn CUỐI, không phải cô lập.**
Ghi đè `ErrorHandler` để lỗi không ai bắt vẫn được log kèm `traceId` (xem
[10-observability.md](10-observability.md) §`traceId`) và báo cho user bằng
toast thay vì im lặng/console trắng:

```typescript
// core/error-handling/global-error-handler.ts
@Injectable()
export class GlobalErrorHandler implements ErrorHandler {
  private readonly toast = inject(ToastService);

  handleError(error: unknown): void {
    console.error(error); // chỉ dev — xem 10-observability.md §Log console
    this.toast.showError('Đã có lỗi xảy ra. Tải lại trang nếu vấn đề còn tiếp diễn.');
    // Khi tới ngưỡng bật Sentry (xem 10-observability.md): Sentry.captureException(error);
  }
}

// app.config.ts
providers: [
  { provide: ErrorHandler, useClass: GlobalErrorHandler },
]
```

**Lớp 2 — cô lập TẠI NƠI rủi ro, không phải qua 1 component bọc ngoài dùng
chung.** Vì Angular không cho bắt lỗi từ `<ng-content>`, "boundary" thật sự
chỉ làm được bằng cách **không để lỗi thoát ra khỏi chính widget đó** —
try/catch ngay trong `effect()`/service của widget, set 1 signal lỗi cục bộ,
widget tự `@if` fallback của chính nó:

```typescript
// widgets/trend-chart/trend-chart.component.ts — widget rủi ro cao (thư viện ngoài, Chart.js qua PrimeNG p-chart)
export class TrendChartComponent {
  readonly chartData = input.required<ChartData>();
  readonly renderError = signal<string | null>(null);

  constructor() {
    effect(() => {
      try {
        this.renderChart(this.chartData()); // lệnh gọi thư viện ngoài — nơi thật sự có thể throw
        this.renderError.set(null);
      } catch (err) {
        console.error(err);
        this.renderError.set('Không vẽ được biểu đồ.');
      }
    });
  }
}
```

```html
@if (renderError(); as err) {
  <div class="chart-fallback">{{ err }}</div>
} @else {
  <canvas #chartCanvas></canvas>
}
```

Chỉ đáng làm Lớp 2 cho widget thật sự rủi ro (gọi thư viện ngoài, tính toán
phức tạp trên dữ liệu không kiểm soát được) — **không** bọc mọi component
bằng try/catch, đúng nguyên tắc Nhóm A/B ở đầu file.

**Trường hợp riêng: `@defer` đã có sẵn 1 boundary thật — dùng nó.** Khối
`@error` của `@defer` (Angular 17+) là cơ chế boundary DUY NHẤT Angular cấp
sẵn, nhưng phạm vi hẹp hơn nhiều: nó chỉ bắt lỗi **tải chunk** (mạng đứt giữa
chừng khi lazy-load JS), không bắt lỗi logic bên trong component đã tải
xong. Các khối `@defer` đã có ở [13-performance.md](13-performance.md) §2
(chart, history list, import dialog) nên thêm `@error`:

```html
@defer (on viewport) {
  <app-trend-chart [chartData]="chartData()" />
} @error {
  <div class="chart-fallback">Không tải được biểu đồ.</div>
} @placeholder {
  <div class="chart-skeleton"></div>
}
```

### #17 — Runtime environment config: 1 bundle, nhiều môi trường

**Vì sao vấn đề THẬT:** `environment.ts`/`environment.prod.ts` truyền thống
của Angular CLI hoạt động qua `fileReplacements` — giá trị bị **inline cứng
vào JS lúc build** (`ng build --configuration=staging` cho ra 1 bundle khác
byte-for-byte so với `ng build --configuration=production`). Hệ quả: build
lại riêng cho từng môi trường, và artifact đã test ở staging **không phải**
artifact thật sự deploy lên production — vi phạm nguyên tắc "build once,
promote qua từng môi trường" (12-Factor App, đã dẫn ở
[be/01-core-components.md](../be/01-core-components.md) §Áp dụng — cùng
nguyên tắc, áp cho FE). Đổi 1 giá trị cấu hình (vd API base URL) dù không đổi
1 dòng code cũng đòi build + deploy lại toàn bộ.

**Cách hiện đại: đọc config từ 1 file JSON NGOÀI bundle, lúc app khởi động,
qua `provideAppInitializer`** — file đó nằm ngoài quy trình build Angular,
thay được trên server/container (vd mount qua ConfigMap) mà không đụng gì
tới bundle JS:

```typescript
// core/config/app-config.service.ts
@Injectable({ providedIn: 'root' })
export class AppConfigService {
  private readonly config = signal<AppRuntimeConfig | null>(null);
  readonly apiBaseUrl = computed(() => this.config()?.apiBaseUrl ?? '');

  async load(): Promise<void> {
    const res = await fetch('/config.json'); // file thật trên server, KHÔNG qua Angular build
    this.config.set(await res.json());
  }
}

// app.config.ts
providers: [
  provideAppInitializer(() => inject(AppConfigService).load()),
]
```

```json
// public/config.json — thay được ở từng môi trường, không rebuild
{ "apiBaseUrl": "https://api.staging.example.com" }
```

**NGƯỠNG khi cần:** chỉ đáng đổi khi có thật **≥2 môi trường triển khai**
(staging + production) và pipeline CI/CD muốn build 1 lần rồi promote — đúng
tinh thần "hoãn có bằng chứng, không hoãn theo giai đoạn" đã dùng xuyên suốt
bộ tài liệu này. Nếu hiện tại chỉ có 1 môi trường build/deploy,
`environment.ts` mặc định của Angular CLI vẫn đủ, không cần đổi ngay.

### #18 — Feature flag / kill switch cho tính năng rủi ro cao

**Vì sao vấn đề THẬT:** không có cơ chế nào khác ngoài `git revert` + build +
deploy lại để tắt 1 tính năng đang gây lỗi cho user thật — trong lúc pipeline
chạy, tính năng lỗi vẫn tiếp tục ảnh hưởng người dùng. Với tính năng rủi ro
cao thật sự (import job phiên bản mới, thay đổi luồng nghiệp vụ đang chạy),
khoảng thời gian "chờ deploy lại" đó là chi phí thật, không phải lý thuyết.

Ở quy mô 5-15 dev, **không cần** dịch vụ feature-flag SaaS đầy đủ (kiểu
LaunchDarkly) — chỉ cần đủ để đọc 1 cờ bật/tắt lúc bootstrap, dùng lại đúng
seam runtime config đã có ở #17 (không dựng 2 hệ thống riêng):

```typescript
// core/config/app-config.service.ts — mở rộng từ #17, cùng 1 config.json
readonly featureFlags = computed(() => this.config()?.featureFlags ?? {});
isEnabled(key: string): boolean {
  return this.featureFlags()[key] ?? false;
}
```

```html
<!-- danh-muc-dti.page.html -->
@if (appConfig.isEnabled('import-job-v2')) {
  <app-import-dialog-v2 />
} @else {
  <app-import-dialog-legacy />
}
```

Tắt nhanh = sửa `config.json` trên server rồi user tải lại trang — không cần
build/deploy lại code.

**NGƯỠNG khi cần:** chỉ thêm cờ cho **1 tính năng cụ thể** đang rollout rủi
ro cao — không tạo cờ "phòng khi cần" cho mọi feature mới. Trước khi có tính
năng nào cần tắt nhanh, dựa vào git revert + deploy lại là đủ.

### #19 — Phát hiện mất kết nối mạng (KHÔNG phải Service Worker/PWA)

> Khác với "Service Worker/offline cache" đã bị gạt sang Nhóm B ở
> [13-performance.md](13-performance.md) §Chưa cần (đúng — hệ thống quản trị
> nội bộ này không cần hoạt động khi mất mạng hoàn toàn) — đây là mức tối
> thiểu hơn nhiều: chỉ **phát hiện** mất mạng và báo đúng, không cache gì,
> không hoạt động offline thật.

**Vì sao vấn đề THẬT:** khi mất mạng, request không hề tới được BE — HTTP
client trả lỗi network (status `0`), khác hẳn lỗi nghiệp vụ trong envelope
`IApiResult` (xem [02-http-envelope.md](02-http-envelope.md)) vì BE chưa từng
nhận được request đó. Nếu code xử lý mọi lỗi HTTP như nhau, user thấy "Đã có
lỗi xảy ra" mơ hồ y hệt lỗi nghiệp vụ thật — trong khi hành động đúng của
user hoàn toàn khác nhau (kiểm tra mạng, không phải thử lại/liên hệ support).

```typescript
// core/http/network-status.interceptor.ts
export const networkStatusInterceptor: HttpInterceptorFn = (req, next) => {
  const toast = inject(ToastService);
  return next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      if (err.status === 0) { // request không tới được server — mất mạng/DNS/CORS
        toast.showError('Mất kết nối mạng. Kiểm tra lại đường truyền.');
      }
      return throwError(() => err);
    }),
  );
};
```

```typescript
// core/network/network-status.service.ts — banner khi mất mạng kéo dài
@Injectable({ providedIn: 'root' })
export class NetworkStatusService {
  readonly isOnline = signal(navigator.onLine);
  constructor() {
    window.addEventListener('online', () => this.isOnline.set(true));
    window.addEventListener('offline', () => this.isOnline.set(false));
  }
}
```

```html
<!-- app.html -->
@if (!networkStatus.isOnline()) {
  <div class="offline-banner">Mất kết nối mạng — thay đổi có thể chưa được lưu.</div>
}
```

Mức tối thiểu hợp lý cho quy mô này dừng ở đây: interceptor phân biệt lỗi
mạng với lỗi nghiệp vụ + banner `navigator.onLine`. Chi phí gần bằng 0, nên
có sớm — không cần thêm gì (Service Worker/cache offline vẫn đúng là Nhóm B,
giữ nguyên quyết định đã có ở [13-performance.md](13-performance.md)).

## Mục lục `fe/`

1. [Core components](01-core-components.md) — file này
2. [HTTP Client & Envelope](02-http-envelope.md) — tiêu thụ `IApiResult<T>`
3. [State management](03-state-management.md) — signal → signalStore
4. [Design-token system](04-design-token-system.md) — bridge với `doc/Design/`
5. [Component library](05-component-library.md) — xem `COMPONENTS.md` để biết số thật, 5 trạng thái
6. [Testing strategy](06-testing-strategy.md) — mapper/interceptor trước
7. [Auth/Identity](07-auth-identity.md) — cookie session
8. [i18n](08-i18n.md) — `@angular/localize`
9. [Forms & Validation](09-forms-validation.md)
10. [Observability](10-observability.md) — correlation với `traceId` BE
11. [Grid & Metadata sync](11-grid-and-metadata.md) — PrimeNG `p-table`, hợp đồng menu/cột với BE
12. [Charting](12-charting.md) — PrimeNG `p-chart`, ngưỡng nâng cấp `ngx-echarts`
13. [Performance](13-performance.md) — zoneless, `@defer`, virtual scroll, bundle budget

Phần thực hành (thứ tự làm, file cần sửa) ở
[fe/trien-khai/00-lo-trinh-tong-the.md](trien-khai/00-lo-trinh-tong-the.md).

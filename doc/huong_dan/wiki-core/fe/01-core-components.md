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

## Mục lục `fe/`

1. [Core components](01-core-components.md) — file này
2. [HTTP Client & Envelope](02-http-envelope.md) — tiêu thụ `IApiResult<T>`
3. [State management](03-state-management.md) — signal → signalStore
4. [Design-token system](04-design-token-system.md) — bridge với `doc/Design/`
5. [Component library](05-component-library.md) — 12 component, 5 trạng thái
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

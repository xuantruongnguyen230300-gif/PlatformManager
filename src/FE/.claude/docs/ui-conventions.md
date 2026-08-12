# UI Conventions — src/FE

## Angular 20 — bắt buộc cho code mới

- **Chỉ standalone component** — không `NgModule`.
- **Signals cho state**: `signal()` / `computed()` / `effect()`. `effect()`
  chỉ cho side-effect thật (vd. đồng bộ với API bên ngoài Angular) — state
  dẫn xuất luôn dùng `computed()`, không dùng `effect()` để gán lại một
  signal khác.
- **Input/Output kiểu signal**: `input()` / `input.required<T>()` /
  `output<T>()` — không dùng decorator `@Input()`/`@Output()`.
- **Control flow mới**: `@if` / `@for` / `@switch` / `@defer` — không dùng
  `*ngIf`/`*ngFor`/`*ngSwitch`.

## `@for` và `track`

`@for` **luôn** cần `track`, chọn đúng key theo dữ liệu:
- Mảng theo chỉ số cố định, không sắp xếp lại → `track $index`.
- Mảng object có id ổn định → `track item.id`.
- **Không bao giờ** track một field có thể null/undefined/trùng — Angular sẽ
  không phát hiện thay đổi đúng cách, dẫn tới UI không cập nhật dù signal đã
  đổi giá trị (lỗi rất khó debug vì không có exception).

## SSR safety (nếu SSR được bật)

Mọi truy cập `window` / `document` / `localStorage` / `navigator` phải bọc:

```ts
if (isPlatformBrowser(inject(PLATFORM_ID))) {
  // truy cập browser API ở đây
}
```

## Form & Dialog

- Form phức tạp / wizard nhiều bước → dùng drawer/side-panel thay vì modal
  che toàn màn hình, trừ khi xác nhận ngắn (dùng confirm dialog nhỏ gọn cho
  trường hợp đó).
- Responsive: side-panel/drawer chuyển sang full-width hoặc trượt từ dưới
  lên khi màn hình hẹp (`< 768px`) — quyết định breakpoint cụ thể theo thiết
  kế trong `doc/Design/` một khi đã có.

## Style & Design Tokens

- SCSS scoped theo component (`styleUrl`, không inline trừ khi component
  cực nhỏ).
- Màu/spacing/radius lấy từ CSS custom property đã định nghĩa trong file
  style toàn cục (`src/styles.scss` sau khi scaffold) — **không hardcode
  hex/px** khi token tương ứng đã tồn tại.
- Nếu chưa có token cho giá trị cần dùng → báo cáo, đừng tự phát minh token
  mới một cách ngầm định. Một khi `doc/Design/` đã chạy pipeline tới stage 3
  (`/design-extract-tokens`), token ở đó là nguồn tham chiếu.

## i18n

Chưa chốt thư viện i18n cho dự án (tuỳ chọn: `@angular/localize` tích hợp
sẵn, hoặc `ngx-translate` nếu cần đổi ngôn ngữ runtime không rebuild). Nếu
task cần thêm chuỗi đa ngôn ngữ và chưa có quyết định, hỏi trước khi tự chọn
thư viện — đây là quyết định ảnh hưởng toàn app.

## Testing

- Ưu tiên test `services/` (mapper, logic gọi API với `HttpClientTestingModule`
  hoặc tương đương) trước — đây là nơi lỗi wire boundary dễ xảy ra nhất.
- Component test khi component có logic đáng test (không chỉ render tĩnh).
- Không bắt buộc coverage 100% — ưu tiên test đúng chỗ rủi ro cao (mapper,
  service, validation logic) hơn là test dàn trải cho mọi component dumb.

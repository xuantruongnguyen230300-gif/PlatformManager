---
kind: luat
scope: core
verified: 2026-09-09
---

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
  style toàn cục (`src/FE/src/styles.scss` sau khi scaffold) — **không hardcode
  hex/px** khi token tương ứng đã tồn tại.
- Nếu chưa có token cho giá trị cần dùng → báo cáo, đừng tự phát minh token
  mới một cách ngầm định.
- **Nguồn token là `doc/Design/Frontend/PlatformManager/Tokens/`** (chốt
  2026-08-27), `styles.scss` đuổi theo — xem
  [`../wiki-core/fe/04-design-token-system.md`](../wiki-core/fe/04-design-token-system.md)
  §Chiều. Chiều đó không đổi. Nhưng **đừng chép trạng thái "code đi sau ở mấy token" vào đây**
  (`.claude/CLAUDE.md` §6) — đối chiếu bằng lệnh:

  ```bash
  grep -h -- '--brand:' src/FE/src/styles.scss
  grep -h 'brand:'      src/FE/src/app/app.config.ts
  grep -h 'theme-color' src/FE/src/index.html
  # PASS: cả ba in ra cùng MỘT mã hex
  ```

  🔄 LẬT 2026-09-06: bản trước ghi *"Hôm nay code **đang đi sau** ở 2 token màu"*. Không còn
  đúng — hai token đó (`--warn`, `--bad`) đã vào code từ 2026-08-28 và vẫn khớp hôm nay. Câu
  cũ còn kèm *"đừng lấy giá trị từ `styles.scss` làm chuẩn"*, đọc như thể `styles.scss` đang
  hỏng; thứ đáng nói là **thứ tự sửa** (`Tokens/*` trước, code sau), không phải một con số nợ.

## i18n

**File chủ: [`../wiki-core/fe/08-i18n.md`](../wiki-core/fe/08-i18n.md)** — quyết
định đã chốt, cú pháp đánh dấu chuỗi, ranh giới "chuỗi FE tự viết" vs "chuỗi BE
trả về", và các bước bật đa-locale. Đọc file đó trước khi viết chuỗi hiển thị
mới; nội dung không lặp lại ở đây.

✅ **Cơ chế đã chạy (đối chiếu 2026-09-06).** Phạm vi vi + en (chốt 2026-08-27), dịch lúc
**chạy** (chốt 2026-09-03) — thư viện đã cài, bảng dịch đã có, nút đổi ngôn ngữ đã có, và cổng
**G12** chặn chữ tiếng Việt lọt vào template (`bash scripts/fe-gate.sh`). Cú pháp đánh dấu
chuỗi chỉ có một nơi: file chủ ở trên; **đừng** chép nó xuống đây (§5).

Điều mục này **giữ nguyên hiệu lực**, vì nó là quy ước viết UI chứ không phải
chi tiết thư viện:

- Chuỗi hiển thị mới phải **bọc** — mỗi câu là một chuỗi trọn vẹn, có
  chỗ cắm tham số. Nối chuỗi bằng `+`, hoặc tự chọn dạng số ít/số nhiều bằng
  `if`, sẽ **chạy đúng khi chỉ có tiếng Việt** và chỉ vỡ lúc bật `en` — muộn
  nhất và đông người nhìn nhất. Cách viết đúng nằm ở file chủ.
- Xây thêm màn hình chưa bọc là tăng khối lượng rà, và khối lượng đó chỉ tăng.

🔄 LẬT 2026-09-06 — **đoạn ở đây trước kia ra lệnh ngược lại.** Nguyên văn: *"**Chưa bọc chuỗi
nào ngay bây giờ** khi thư viện chưa cài … Đây là nợ có chủ đích … nó tan cùng lượt cài thư
viện."* Thư viện **đã cài** (`@ngx-translate/core` v18), nợ đó **đã tan**, và cổng G12 nay đỏ
lên với đúng những chuỗi mà câu cũ cho phép để trần. Ai còn đọc câu cũ sẽ viết màn hình mới
không bọc rồi không hiểu vì sao cổng đỏ.

Luật hiện hành: **mọi câu người dùng đọc đến từ một file bảng dịch.** Kể cả
`title` của route — nó là **khoá dịch**, không phải câu; viết thẳng tiếng Việt vào đó thì tiêu
đề tab và tiêu đề topbar là hai chỗ duy nhất trên màn hình không đổi khi bấm sang English, và
không có gì báo.

🛑 **HAI thư mục bảng dịch, không phải một — chọn đúng thư mục trước khi thêm khoá.** Khoá dùng
được cho mọi sản phẩm dựng trên nền tảng đi vào `src/FE/public/i18n/`; khoá của riêng sản phẩm
này (mọi màn `modules/`) đi vào `src/FE/public/i18n-app/`. Đặt nhầm thì không gì đỏ hôm nay —
nó chỉ hiện ra vào ngày tách CoreBase, dưới dạng một nền tảng mang theo chuỗi của dự án cũ.
Danh sách nguồn, thứ tự ghép và lý do: [file chủ](../wiki-core/fe/08-i18n.md) §Khuôn CoreBase
(đối chiếu 2026-09-09).

⚠️ **Spec của màn `modules/` phải cấp `CORE_I18N` cho TestBed** — `useTranslationsInTest()` lấy
danh sách nguồn từ đó; không cấp thì nó chỉ nạp nhóm CoreBase và spec đỏ với thông điệp dạng
`Expected 'danh-muc-dti.title' to be 'Danh mục DTI'`.

## In ấn — quy ước `.no-print` đang tồn tại, nay được ghi lại

> Ghi nhận 2026-08-27: quy ước này đã chạy trong code từ trước mà **không tài
> liệu nào định nghĩa** — đúng dạng lỗi đã tìm ra với `src/environments/`. Người
> viết màn hình tiếp theo hoặc bỏ sót, hoặc phát minh lại một cách khác.

Màn hình có nội dung người dùng cần in (báo cáo, danh sách) phải in ra được
**bản dùng được**, không phải ảnh chụp nguyên giao diện.

- Gắn `.no-print` cho phần tử chỉ có nghĩa khi tương tác: sidebar, topbar,
  toolbar lọc, nút hành động, toast, phân trang.
- Quy tắc `@media print` đặt tập trung ở `styles.scss`, không rải vào từng
  component — nếu không, mỗi màn hình sẽ in ra một kiểu.
- Bảng dài: để trình duyệt ngắt trang tự nhiên, đừng ép chiều cao cố định.

Kiểm: `grep -rl "no-print" src/FE/src` — mỗi file trả về phải nằm trong nhóm
"chỉ có nghĩa khi tương tác" ở trên.

## Testing

- Ưu tiên test `services/` (mapper, logic gọi API với `HttpClientTestingModule`
  hoặc tương đương) trước — đây là nơi lỗi wire boundary dễ xảy ra nhất.
- Component test khi component có logic đáng test (không chỉ render tĩnh).
- Không bắt buộc coverage 100% — ưu tiên test đúng chỗ rủi ro cao (mapper,
  service, validation logic) hơn là test dàn trải cho mọi component dumb.

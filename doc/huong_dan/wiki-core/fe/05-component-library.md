# 5. Thư viện component dùng chung

## Phạm vi áp dụng — PrimeNG vs hand-rolled (Đã CHỐT 2026-08-15)

Sau khi đối chiếu thực tế thị trường ERP/chuyển đổi số (xem
[04-design-token-system.md](04-design-token-system.md) §Thư viện component),
PrimeNG là mặc định cho thành phần **tương tác phức tạp**. 9 component đơn
giản đã build (bảng dưới) **không bắt buộc migrate ngay** — chi phí viết lại
không tương xứng lợi ích khi chúng đã chạy đúng và khớp thiết kế 1:1.

| Component | Quyết định | Vì sao |
|---|---|---|
| Table/Grid (`danh-muc-dti`) | **PrimeNG `p-table`** — mọi grid mới; grid cũ giữ tới khi cần mở rộng lớn | Xem [11-grid-and-metadata.md](11-grid-and-metadata.md) — đây là thành phần rủi ro "chay" thật nhất |
| Chart | **PrimeNG `p-chart`** | Xem [12-charting.md](12-charting.md) |
| Dropdown/Select có tìm kiếm, multiselect, date-range picker, autocomplete | **PrimeNG** (`p-select`, `p-multiselect`, `p-datepicker`...) khi lần đầu cần — **không** tự viết tay | Đây đúng nhóm input phức tạp mà tự viết tốn công + dễ thiếu a11y (xem cảnh báo 5 trạng thái bên dưới) |
| Dialog | Giữ nguyên `<dialog>` gốc hiện có — chỉ đổi sang `p-dialog` khi cần animation/nested dialog thật sự | Dialog gốc đã đơn giản, đủ dùng, không có nỗi đau rõ ràng để đổi ngay |
| Button, Card, Badge, ProgressBar, NoticeBanner, DeltaIndicator, HistoryRow, KpiTile, Input (text/number cơ bản) | **Giữ nguyên hand-rolled** — không migrate | Đã build đúng, khớp `Components/*.md` 1:1, đơn giản, không có tính năng ẩn khó tái tạo — PrimeNG không mang lại lợi ích tương xứng chi phí đổi |

**Nguyên tắc chung khi phân vân:** component càng nhiều trạng thái tương
tác/logic ẩn (sort, filter, keyboard nav phức tạp, a11y nhiều quy tắc) →
càng nên dùng PrimeNG. Component càng đơn giản (chỉ hiển thị + 1-2 style
biến thể) → hand-rolled vẫn ổn, không đổi chỉ vì "cho đồng bộ".

## Nguồn — không phát minh thêm

`doc/Design/Frontend/PlatformManager/COMPONENTS.md` là mục lục đầy đủ và
DUY NHẤT — chạy `ls doc/Design/Frontend/PlatformManager/Components/*.md` để
có số lượng/tên chính xác hiện tại thay vì tin số hardcode ở đây (COMPONENTS.md
tự đánh dấu component nào đã obsolete, ví dụ `Fab`) — implement Angular
component **đúng theo spec đó**, không tự vẽ lại từ đầu. Mỗi component
Angular tương ứng 1 file spec trong `Components/*.md` — đọc trước khi code,
không đoán anatomy từ tên.

## 5 trạng thái bắt buộc — khoảng trống lớn nhất ở phần hand-rolled

> Component PrimeNG (Table/Chart/input phức tạp, xem §Phạm vi áp dụng ở
> trên) đã có sẵn `:hover`/`:focus-visible`/`:disabled`/a11y chuẩn — mục
> này chỉ áp dụng cho 9 component **giữ hand-rolled**.

`COMPONENTS.md` tự ghi nhận: prototype gốc **không có** `:hover`/`:focus`/
`:disabled` custom cho gần như mọi component (chỉ có đúng 1 rule
`.btn:active`). Khi chuyển sang Angular, đây là chỗ **phải làm tốt hơn bản
gốc**, không phải "port y nguyên" — Fidelity Policy của `doc/Design/` áp
dụng cho việc *tài liệu hoá* app hiện tại, không có nghĩa là code Angular
mới được phép thiếu accessibility.

Mỗi component dùng chung khi viết bằng Angular phải định nghĩa tường minh:

| Trạng thái | Yêu cầu tối thiểu |
|---|---|
| `default` | Đúng theo spec `Components/*.md` |
| `:hover` | Đổi thị giác rõ ràng (background/border/shadow) — không để trống |
| `:focus-visible` | Outline/ring rõ, đủ tương phản — **bắt buộc cho keyboard nav**, khác `:focus` (không phạt chuột click) |
| `:active` | Giữ hiệu ứng đã có (`translateY(1px)` cho `.btn`) nếu phù hợp |
| `:disabled`/`[disabled]` | `opacity` giảm + `cursor: not-allowed` + **thật sự chặn tương tác** (Angular `[disabled]` binding, không chỉ đổi style) |

## Composition — mở rộng spec, không tạo biến thể ngầm

Cần 1 biến thể chưa có trong `Components/*.md` (vd `Button` size nhỏ hơn) →
sửa spec trước (thêm vào `Components/Button.md`, báo cáo), rồi mới code —
đúng nguyên tắc "Extend a spec... instead of inventing new ones" đã ghi ở
`doc/Design/CLAUDE.md`.

## Vị trí trong cây thư mục

```
shared/components/<name>/
├── <name>.ts          # standalone, input()/output(), không inject service data
├── <name>.html
└── <name>.scss        # dùng token, không hex trần (xem 04-design-token-system.md)
```

Ngoại lệ đã ghi nhận (audit trước): component "app-shell" (`sidebar`,
`topbar`, `toast`) được phép inject service hạ tầng UI singleton
(`SidebarStateService`, `NotificationService`) dù nằm trong `components/` —
đây là ngoại lệ tường minh cho lớp vỏ app, **không** áp dụng cho component
hiển thị dữ liệu nghiệp vụ.

## Test trực quan

Trước khi coi 1 component "xong": kiểm tra thật 5 trạng thái qua
`chrome-devtools-mcp` hoặc thao tác tay (tab qua bằng bàn phím để thấy
`:focus-visible`, set `[disabled]="true"` để thấy trạng thái khoá) — không
chỉ đọc code rồi coi là đủ.

## Bundle size khi thêm module PrimeNG mới — đo TRƯỚC khi merge, không chỉ chặn khi vượt trần

> Bổ sung 2026-08-24, đối chiếu thực hành ngành cho hệ thống tầm trung:
> [13-performance.md](13-performance.md) §4 đã có `budgets` trong
> `angular.json` — đó là **lưới chặn tổng**, CI chỉ đỏ khi TỔNG bundle vượt
> `maximumError`. Nó không nói module nào vừa thêm gây ra phần tăng đó, và
> người viết PR chỉ biết khi CI đã đỏ — quá muộn để cân nhắc lại trước khi
> review. Thực hành ở đội 5-15 dev là đo **delta của riêng lần thêm đó**,
> đưa số vào PR, trước khi ai duyệt.

```bash
ng build --configuration production --stats-json
npx source-map-explorer "dist/*/browser/*.js" --html dist/bundle-report.html
```

- Chạy 1 lần **trước** khi thêm module PrimeNG mới (vd `MultiSelectModule`),
  1 lần **sau**, so KB chênh lệch — ghi số đó vào mô tả PR. Không cần dựng
  pipeline riêng, đây là lệnh chạy tay ~30 giây.
- Bổ sung, **không thay thế** `budgets` ở [13-performance.md](13-performance.md)
  §4: budget là lưới an toàn cuối (bắt được cả trường hợp tăng dần không ai
  để ý), số đo thủ công này là tín hiệu sớm cho đúng 1 thay đổi.
- PrimeNG là thư viện lớn — 1 module tưởng nhỏ (vd `p-datepicker` kéo theo
  locale data) có thể nặng hơn cảm giác "chỉ thêm 1 component". Đây chính
  là lý do `@defer` đã được khuyến nghị cho khối nặng ở
  [13-performance.md](13-performance.md) §2 — số đo ở đây là căn cứ để
  quyết định module nào cần `@defer`, module nào không.

## Tab order xuyên nhiều component — "5 trạng thái" ở trên là mức component, đây là mức trang

> Bổ sung 2026-08-24, đối chiếu thực hành ngành cho hệ thống tầm trung:
> bảng "5 trạng thái bắt buộc" ở trên đúng nhưng kiểm **từng component
> riêng lẻ** — `:focus-visible` của 1 `Button` không nói được gì về việc
> bấm Tab nhiều lần trên 1 trang thật (sidebar + topbar + table + dialog
> cùng lúc) có đi đúng thứ tự người dùng **nhìn thấy** không. Đây là lớp
> lỗi khác, chỉ lộ ra khi nhiều component ghép lại — dạng lỗi hay bị bỏ sót
> vì mỗi PR chỉ test đúng component mình vừa viết.

Quy tắc bắt buộc, áp cho mọi trang có ≥2 component tương tác:

- **Không bao giờ dùng `tabindex` dương** (`tabindex="1"`, `"2"`...) — chỉ
  `0` (nhập hàng đợi Tab tự nhiên theo DOM) hoặc `-1` (focus được bằng code,
  bỏ qua khi Tab). `tabindex` dương nhảy trước cả thứ tự DOM, và không ai
  nhớ nổi số đã dùng ở nơi khác khi trang có nhiều component.
- **Thứ tự DOM phải khớp thứ tự đọc trên màn hình.** CSS `order`/
  `grid-template-areas` đổi được vị trí **nhìn thấy** mà không đổi thứ tự
  Tab — component nào dùng `order` để sắp xếp lại layout (vd `.kpis` grid ở
  dashboard) phải tự kiểm bằng Tab thật, không suy từ code.
- **`<dialog>` gốc (đã chốt giữ hand-rolled, xem §Phạm vi áp dụng trên) tự
  bẫy focus khi mở bằng `showModal()`** trên trình duyệt hiện đại — Tab
  không thoát ra ngoài, focus tự trả về phần tử đã mở dialog lúc đóng. Đây
  là hành vi có sẵn của thẻ HTML, không phải code tự viết — nhưng vẫn phải
  **test thật** (không giả định), vì bọc sai markup quanh `<dialog>`
  (backdrop, overlay tự chế) dễ làm mất hành vi này mà không có lỗi biên
  dịch nào báo.
- **Overlay của PrimeNG** (panel `p-multiselect`, `p-datepicker`, `p-dialog`)
  tự quản focus trong phạm vi của chính nó — thứ đáng test thật ở đây là
  khi **2 overlay chồng nhau** (vd mở 1 dialog xác nhận từ bên trong
  `p-dialog`): `Esc` phải đóng đúng lớp trên cùng, focus phải trả về đúng
  chỗ — đây là tổ hợp cụ thể hay vỡ ở hệ thống nhiều dialog, không phải lý
  thuyết.
- Nếu 1 component hand-rolled mới **thật sự** cần tự bẫy focus (không dùng
  `<dialog>` gốc) → dùng `cdkTrapFocus` (`@angular/cdk/a11y`) thay vì tự
  viết — `@angular/cdk` đã là dependency của dự án (dùng cho
  `CdkVirtualScrollViewport`, xem [13-performance.md](13-performance.md)
  §3), `a11y` nằm cùng package, không phải thêm phụ thuộc mới:

```html
<!-- chỉ dùng khi KHÔNG có <dialog>/p-dialog gốc để bẫy focus sẵn -->
<div class="custom-panel" cdkTrapFocus [cdkTrapFocusAutoCapture]="true">
  ...
</div>
```

## Kiểm a11y tự động — bổ sung cho, không thay thế, "Test trực quan" ở trên

> Bổ sung 2026-08-24, đối chiếu thực hành ngành cho hệ thống tầm trung: mục
> "Test trực quan" ở trên hoàn toàn dựa vào tay (`chrome-devtools-mcp` hoặc
> bấm Tab thật) — đúng cho lần viết đầu, nhưng không có gì chặn regression
> nếu 1 lần sửa sau vô tình bỏ `aria-label`/đổi `role` sai. Phần vi phạm
> **cấu trúc DOM** (thiếu label, `role` sai, ARIA không hợp lệ, contrast
> tính từ style thật) máy kiểm được — không cần đợi review tay phát hiện.

`axe-core` chạy trực tiếp trên DOM đã render, không phụ thuộc framework test
— dùng thẳng được với Karma/Jasmine đã chọn ở
[06-testing-strategy.md](06-testing-strategy.md), không cần đổi sang Jest:

```bash
npm install -D axe-core
```

```ts
// shared/components/button/button.a11y.spec.ts
import axe from 'axe-core';

it('không có vi phạm a11y cấu trúc (axe-core)', async () => {
  const fixture = TestBed.createComponent(ButtonComponent);
  fixture.detectChanges();
  const results = await axe.run(fixture.nativeElement);
  expect(results.violations).toEqual([]);
});
```

- **Phạm vi: 9 component hand-rolled** ở §Phạm vi áp dụng trên — đây đúng
  nhóm không có a11y có sẵn từ thư viện. Không chạy `axe-core` lên component
  PrimeNG: thư viện tự chịu trách nhiệm a11y của chính nó (đây cũng là 1 lý
  do đã chọn PrimeNG cho input phức tạp, xem bảng quyết định trên).
- **Giới hạn phải biết:** `axe-core` bắt được vi phạm **cấu trúc/tĩnh**
  (thiếu label, ARIA sai, contrast) — **không** bắt được lỗi hành vi như tab
  order sai hay focus trap vỡ (mục "Tab order" trên) — 2 loại kiểm này bổ
  sung cho nhau, không loại nào thay được loại kia. Đừng bỏ bước Tab tay chỉ
  vì `axe-core` đã xanh.

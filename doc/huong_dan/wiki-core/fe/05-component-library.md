---
kind: luat
scope: core
verified: 2026-09-08
---

# 5. Thư viện component dùng chung

## Phạm vi áp dụng — PrimeNG vs hand-rolled (Đã CHỐT 2026-08-15)

Sau khi đối chiếu thực tế thị trường ERP/chuyển đổi số (xem
[04-design-token-system.md](04-design-token-system.md) §Thư viện component),
PrimeNG là mặc định cho thành phần **tương tác phức tạp**. Nhóm đơn giản (bảng dưới)
**không bắt buộc migrate** — chi phí viết lại không tương xứng lợi ích khi chúng đã chạy đúng
và khớp thiết kế 1:1.

> ### 🔄 LẬT 2026-09-06 — "9 component đơn giản **đã build**" là hai lỗi trong một câu
>
> **(a) Con số chép tay.** `.claude/CLAUDE.md` §6 cấm chép thứ đếm được bằng lệnh; chính file
> này ở §Nguồn cũng đã dặn *"chạy `ls` để có số chính xác thay vì tin số hardcode ở đây"* rồi
> lại hardcode ở đầu file. Đếm bằng lệnh:
>
> ```bash
> ls -d src/FE/src/app/shared/components/*/          # component Angular dùng chung
> ls doc/Design/Frontend/PlatformManager/Components/ # spec thiết kế
> ```
>
> **(b) "Đã build" hiểu sai hình dạng của chúng.** Nhóm đơn giản ở dòng cuối bảng dưới
> **không phải component Angular** và không có thư mục nào trong `shared/components/`. Những
> cái **đã có** là **lớp CSS toàn cục** khai trong `src/FE/src/styles.scss` (`.btn`, `.card`,
> `.badge`, `.input`, `.notice`, `.delta` — kèm `:hover` `:focus-visible` `:active`
> `:disabled`), dùng thẳng trên thẻ HTML. Đây là quyết định có chủ đích — thứ chỉ có style,
> không có hành vi, thì không cần một lớp bọc Angular — nhưng gọi nhầm nó là "component đã
> build" khiến người đọc đi tìm một thư mục không tồn tại, rồi kết luận là còn thiếu và dựng
> bản thứ hai.
>
> **🔄 LẬT 2026-09-08 — (b) đúng về cơ chế nhưng sai về danh sách.** Bản 2026-09-06 nói cả
> nhóm ở dòng cuối bảng là "lớp CSS toàn cục trong `styles.scss`". Ba cái trong đó —
> `ProgressBar`, `HistoryRow`, `KpiTile` — khi đó **không tồn tại dưới bất kỳ hình dạng nào**.
> Nói ở thì hiện tại rằng chúng "đã build" gây ra đúng thiệt hại mà (b) mô tả, chỉ theo chiều
> ngược: người đọc đi tìm một lớp CSS không có thật rồi kết luận mình vừa xoá nhầm.
>
> **🔄 LẬT LẠI 2026-09-10 — nay chúng CÓ THẬT, và không phải lớp CSS toàn cục.** Cả ba là
> component Angular đứng riêng ở `src/FE/src/app/modules/dashboard/components/`, cùng
> `TrendChart`. Bài học của (b) không đổi — **hình dạng thật** mới là thứ phải ghi, và hình
> dạng thật của chúng nay là thư mục component chứ không phải một lớp trong `styles.scss`.
> Đọc từ đĩa thay vì tin dòng này ([`.claude/CLAUDE.md`](../../../../.claude/CLAUDE.md) §6):
>
> ```bash
> ls src/FE/src/app/modules/dashboard/components
> ```
>
> Đếm bằng lệnh thay vì tin danh sách (`.claude/CLAUDE.md` §6) — **tiêu chí PASS: mỗi tên nêu
> ở thì hiện tại phải trúng ít nhất một dòng**:
>
> ```bash
> for c in btn card badge input notice delta; do
>   printf '%-8s ' "$c"; grep -c "^\.$c" src/FE/src/styles.scss
> done
> ```

| Component | Quyết định | Vì sao |
|---|---|---|
| Table/Grid | **PrimeNG `p-table`**, bọc trong `shared/components/data-grid/` (dùng chung, chốt 2026-09-06) — mọi grid mới đi qua đó, không dựng `p-table` rời | Xem [11-grid-and-metadata.md](11-grid-and-metadata.md) — đây là thành phần rủi ro "chay" thật nhất. 🔄 LẬT 2026-09-06: ô này trước neo vào `danh-muc-dti`, module đã gỡ 2026-08-29 |
| Chart | **PrimeNG `p-chart`** | File chủ: [12-charting.md](12-charting.md) — trạng thái, mẫu code và cách đọc token vào canvas đều ở đó, ô này cố ý **không** chép lại ([`.claude/CLAUDE.md`](../../../../.claude/CLAUDE.md) §5) |
| Dropdown/Select có tìm kiếm, multiselect, date-range picker, autocomplete | **PrimeNG** (`p-select`, `p-multiselect`, `p-datepicker`...) khi lần đầu cần — **không** tự viết tay | Đây đúng nhóm input phức tạp mà tự viết tốn công + dễ thiếu a11y (xem cảnh báo 5 trạng thái bên dưới) |
| Dialog | Giữ `<dialog>` gốc — ✅ đang chạy ở `shared/components/confirm-dialog/` và ở `platform/quan-tri-nguoi-dung/components/user-form-dialog/` (đối chiếu 2026-09-06, cả hai dùng `showModal()`). Chỉ đổi sang `p-dialog` khi cần animation/nested dialog thật sự | Dialog gốc đã đơn giản, đủ dùng, không có nỗi đau rõ ràng để đổi ngay |
| **Đã có:** Button, Card, Badge, NoticeBanner, DeltaIndicator, Input (text/number cơ bản) | **Giữ nguyên hand-rolled** — không migrate. Hình dạng thật: **lớp CSS toàn cục trong `src/FE/src/styles.scss`** (`.btn`, `.card`, `.badge`, `.notice`, `.delta`, `.input`), không phải component Angular (xem cảnh báo §Phạm vi áp dụng) | Đã chạy đúng, khớp `Components/*.md` 1:1, đơn giản, không có tính năng ẩn khó tái tạo — PrimeNG không mang lại lợi ích tương xứng chi phí đổi |
| ✅ **ĐÃ DỰNG (đối chiếu 2026-09-10):** ProgressBar, HistoryRow, KpiTile | **Hand-rolled**, cùng lý do dòng trên. Hình dạng thật: **component Angular** ở `src/FE/src/app/modules/dashboard/components/`, KHÔNG phải lớp CSS toàn cục — chúng thuộc một màn nghiệp vụ nên không lên `shared/` | Ba cái này chỉ hiển thị, không có logic ẩn — không có lý do gọi PrimeNG. Ghi tách dòng vì hình dạng thật của chúng khác hẳn nhóm dòng trên |

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
> này chỉ áp dụng cho nhóm **giữ hand-rolled** (dòng cuối bảng trên).

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

Component Angular dùng chung — mỗi cái một thư mục:

```
shared/components/<name>/
├── <name>.ts          # standalone, input()/output(), không inject service data
├── <name>.html
└── <name>.scss        # dùng token, không hex trần (xem 04-design-token-system.md)
```

Thứ **chỉ có style, không có hành vi** (`.btn`, `.card`, `.badge`, `.input`, `.notice`,
`.delta`) thì **không** tạo thư mục — nó là lớp CSS toàn cục trong `src/FE/src/styles.scss`.
Xem cảnh báo ở §Phạm vi áp dụng, kèm lệnh kiểm tên nào có thật.

**Ngoại lệ inject — cập nhật 2026-09-06.** Component "app-shell" được phép inject service hạ
tầng UI singleton dù nằm trong `components/`; đây là ngoại lệ tường minh cho lớp vỏ app,
**không** áp dụng cho component hiển thị dữ liệu nghiệp vụ. Đọc danh sách thật bằng lệnh thay
vì tin bảng chép tay (`.claude/CLAUDE.md` §6):

> 📖 Ranh giới này **đã có máy cưỡng chế từ 2026-09-08** (cổng G4). Ngoại lệ ở đây được cổng
> đọc theo hai trục rời nhau, và mở rộng nó không phải việc sửa một dòng FAIL — đọc
> [`trien-khai/05-gate.md`](trien-khai/05-gate.md) §G4 trước khi thêm bất cứ tên nào.

```bash
grep -rn "inject(" src/FE/src/app/shared/components/*/*.ts | grep -v spec
```

🔄 LẬT 2026-09-06 — bản trước liệt đúng ba cái (`sidebar`, `topbar`, `toast`) và gọi tên
service là **`NotificationService`**. Cả hai đều sai với code hôm nay: service thật tên
`ToastService` (`src/FE/src/app/core/toast/toast.service.ts`) — chép tên cũ ⇒ lỗi biên dịch —
và số nơi inject đã nhiều hơn ba. Điểm cần giữ nguyên là **ranh giới**, không phải danh sách.

🔄 LẬT 2026-09-08 — câu minh hoạ cho ranh giới đó **nêu sai component**. Bản trước viết
*"`data-grid` cố ý không inject `LanguageService` mà nhận `localeId` qua `input()`"*.
`data-grid` **không có** input `localeId` nào (đối chiếu 2026-09-08 — input của nó là
`loading` / `totalCount` / `page` / `pageSize`); chỗ thật sự nhận `localeId` là
`platform/quan-tri-nguoi-dung/components/user-grid-table/user-grid-table.ts`, đúng nơi có
`DatePipe` cần locale. Ranh giới thì **không đổi** và vẫn là điều cần nhớ: component trong
`components/` nhận locale qua `input()` chứ không `inject(LanguageService)` — lách chỗ đó từng
làm 37 test đỏ vì `LanguageService` đòi token `CORE_I18N` mà spec màn nghiệp vụ không cấp.
Kiểm bằng lệnh, **PASS = chỉ trúng `user-grid-table.ts`**:

```bash
grep -rn "localeId = input" src/FE/src/app --include=*.ts
```

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

> Đây là **phần a11y thuộc chủ đề component** — file chủ giữ nguyên ở đây. Điểm
> vào chung cho a11y: [15-accessibility.md](15-accessibility.md).

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
  `<dialog>` gốc) → dùng `cdkTrapFocus` (`@angular/cdk/a11y`) thay vì tự viết.

  🔄 LẬT 2026-09-06: bản trước nói `@angular/cdk` *"đã là dependency của dự án (dùng cho
  `CdkVirtualScrollViewport`)"*. **Chưa hề cài** — `grep -rn "@angular/cdk" src/FE/package.json
  src/FE/src` cho 0 dòng, và `CdkVirtualScrollViewport` ở
  [13-performance.md](13-performance.md) §3 cũng là đích đến chưa thi công. Nghĩa là dùng
  `cdkTrapFocus` **có** thêm một phụ thuộc mới; cân nhắc điều đó trước, đừng tin câu "miễn phí"
  của bản cũ:

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

- **Phạm vi: nhóm hand-rolled** ở §Phạm vi áp dụng trên — đây đúng
  nhóm không có a11y có sẵn từ thư viện. (🔄 LẬT 2026-09-06: bỏ con số "9", §6.) Không chạy `axe-core` lên component
  PrimeNG: thư viện tự chịu trách nhiệm a11y của chính nó (đây cũng là 1 lý
  do đã chọn PrimeNG cho input phức tạp, xem bảng quyết định trên).
- **Giới hạn phải biết:** `axe-core` bắt được vi phạm **cấu trúc/tĩnh**
  (thiếu label, ARIA sai, contrast) — **không** bắt được lỗi hành vi như tab
  order sai hay focus trap vỡ (mục "Tab order" trên) — 2 loại kiểm này bổ
  sung cho nhau, không loại nào thay được loại kia. Đừng bỏ bước Tab tay chỉ
  vì `axe-core` đã xanh.

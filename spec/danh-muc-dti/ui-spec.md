---
kind: luat
scope: du-an
verified: chua-doi-chieu
feature: "danh-muc-dti"
status: "📐 ĐÍCH ĐẾN — CHƯA THI CÔNG"
updated: "2026-09-09"
---

# UI Spec — Danh mục DTI

> 📐 **ĐÍCH ĐẾN — CHƯA THI CÔNG.** Toàn bộ file này mô tả màn hình **sẽ dựng**.
> Code cũ (`src/FE/src/app/modules/danh-muc-dti/`) đã bị xoá 2026-08-29 và thư
> mục `src/FE/src/app/modules/` hiện **không tồn tại** — không dòng nào dưới đây
> được đọc như hiện trạng. Chỗ nào trích `src/FE/src/styles.scss` hoặc
> `src/FE/src/app/app.routes.ts` là Core **đang sống**, đã mở file đối chiếu
> ngày 2026-09-05.

## 0. Nguồn và ranh giới của file này

| Câu hỏi | File chủ |
| --- | --- |
| Quy tắc nghiệp vụ (công thức, kỳ, import/export, hợp lệ hoá) | `spec/danh-muc-dti/business-rules.md` |
| Đường dẫn, tham số, shape JSON của endpoint | `doc/contracts/danh-muc-dti.md` |
| Layout blueprint + copy verbatim + iconography ở mức pixel | `doc/Design/Frontend/PlatformManager/Screens/02-danh-muc-dti.md` |
| Hợp đồng từng component | `doc/Design/Frontend/PlatformManager/COMPONENTS.md` → `Components/*.md` |
| Route, guard, thứ tự guard, state trên URL | `doc/huong_dan/quy-uoc/fe-routing-guard.md` |
| Control flow, form, i18n, in ấn, token | `doc/huong_dan/quy-uoc/fe-ui-conventions.md` |
| Tầng, mapper, casing DTO ↔ model | `doc/huong_dan/quy-uoc/fe-api-client.md` |

File này trả lời **đúng một** câu hỏi khác với các file trên: *"màn hình gồm
những vùng nào, vùng nào dùng component nào, mỗi trạng thái hiển thị gì, và
trường trên màn ứng với trường dữ liệu nào."* Không lặp lại công thức nghiệp vụ,
không lặp lại shape JSON.

Nguồn giao diện đã được người dùng duyệt từng điểm: `doc/Design/Frontend/PlatformManager/Prototypes/index.html`,
section `#screen-dti` (bắt đầu dòng 3236; CSS riêng của màn ở dòng 1647–1810).
Đây là bản dựng để duyệt, **không** phải nguồn tri thức chính thức — sau khi
`Screens/02-danh-muc-dti.md` được viết lại thì `doc/Design/` là nguồn.

## 1. Phạm vi màn

Một màn hình duy nhất, làm bốn việc:

1. **Đọc** danh mục chỉ tiêu DTI theo năm/kỳ, có tìm kiếm + lọc + phân trang.
   Đọc được theo **tuần, tháng và cả năm**.
2. **CRUD** bản thân chỉ tiêu (mã, tên, nhóm, điểm tối đa).
3. **Nhập đánh giá theo TUẦN** — 6 trường trong dialog (Q9) + 2 trường sửa inline.
4. **Import** file `.csv` / `.xlsx` / `.xls` chạy nền, có poll trạng thái và
   bảng kết quả từng dòng.

⚠️ **Đọc và ghi KHÔNG cùng tập đơn vị kỳ** (Q37). Đọc được cả tuần/tháng/năm, nhưng
ghi **chỉ theo tuần** — chọn tháng hoặc năm thì màn chuyển chỉ đọc (§5.5.1). Đây là
điều bất đối xứng dễ bị "sửa cho nhất quán" nhất trong cả file; lý do ở §3.1.

Ngoài phạm vi: xuất báo cáo (chỉ có ở Dashboard — Q13), biểu đồ, ô KPI tổng hợp.

## 2. Route, quyền truy cập, state trên URL

**Route đã chốt (Q33, người dùng chốt 2026-09-05): `/danh-muc/dti`.** Không còn là
đề xuất. Đây là đường dẫn **lịch sử** của chính màn này trước khi bị gỡ, còn ghi ở
`doc/Design/Frontend/PlatformManager/Components/Footer.md:92` và
`doc/Design/Frontend/PlatformManager/DESIGN.md:492`; dùng lại thay vì đặt tên mới,
và cùng khuôn hai cấp với `/quan-tri/nguoi-dung` của Core. Tầng:
`modules/danh-muc-dti/` (nghiệp vụ, không phải `platform/`) theo
`doc/kien-truc-core-module.md` §Nguyên tắc áp dụng khi thêm module mới.

- Khai `loadChildren` **một dòng** vào `src/FE/src/app/app.routes.ts` — hôm nay
  file đó chỉ có route Core. Đếm bằng lệnh, đừng chép số:
  `grep -c "loadChildren" src/FE/src/app/app.routes.ts`.
- Guard khai trong `danh-muc-dti.routes.ts` của chính feature: **đúng hai cái**,
  `authGuard → mustChangePasswordGuard` (`doc/huong_dan/quy-uoc/fe-routing-guard.md`
  §6). 🛑 **KHÔNG có guard quyền trên route này** — chốt Q39 (2026-09-06). Quyền
  ghi (Q27) không chặn ở tầng route mà chỉ ẩn affordance ghi trong màn; lý do đầy
  đủ ở §5.6. *(Bản trước của dòng này khai `[guard quyền GHI]` ở vị trí thứ ba —
  sai theo Q39.)*
- `title` khai ở **cấp `Route`**, không đặt vào `data` — WCAG 2.4.2, có test canh
  ở `src/FE/src/app/app.routes.spec.ts` (§2 quy tắc 3 của file routing).
- Mục sidebar **không hardcode** — thêm bản ghi `SysMenus` + `SysMenuRoles` phía
  BE (`doc/contracts/meta-menu.md`). 🛑 **Mục menu KHÔNG ẩn theo quyền ghi** (Q39):
  ai đăng nhập cũng thấy và cũng vào được, chỉ khác nhau ở chỗ có nút sửa hay
  không. `SysMenuRoles` vẫn tồn tại và vẫn là cơ chế chung của Core — điều này chỉ
  nói rằng **màn này** không dùng nó để lọc theo quyền ghi DTI.

**State lên URL** (`fe-routing-guard.md` §8 — quy ước này áp cho màn hình mới):
`q` (từ khoá), `groupId`, `status`, `year`, `period`, `page`, `pageSize`. Đổi bộ
lọc dùng `replaceUrl: true`. URL là nguồn sự thật; component đọc query param rồi
mới gọi API, **không** giữ bản sao state song song. Ở lại trong signal: dữ liệu
đã tải, cờ loading, dialog nào đang mở, bản nháp form chưa lưu.

## 3. Layout theo vùng

Khung ngoài là app shell (`Sidebar` + `Topbar` + `main` + `Toast`,
`src/FE/src/app/app.html`) — màn hình không tự vẽ khung này.

Trang bọc trong `.page-fill` (`src/FE/src/styles.scss` § `.page-fill`) để card chiếm hết
chiều cao còn lại và **chỉ vùng lưới cuộn**; toolbar và hàng tiêu đề giữ
`flex: none` nên không co lại trên viewport thấp. `.page-fill` là **nửa đầu** của
chuỗi chiều cao; nửa sau là class `.grid-host` trên chính thẻ `<app-data-grid>` —
thiếu nửa nào lưới cũng âm thầm về chiều cao nội dung (§3.2).

| # | Vùng | Component (có trong COMPONENTS.md) | Class / hợp đồng | Ghi chú |
| --- | --- | --- | --- | --- |
| V1 | Khung trang | `Card` | `.card` — `styles.scss` § `.card` | `.dti-grid-card` là layout page-local kèm theo, không phải component |
| V2 | Hàng tiêu đề | `Card` §Anatomy | `.title` — `styles.scss` § `.title` | `<h2>Danh mục DTI</h2>` + `<span class="muted" aria-live="polite">` với số chỉ tiêu (Q16 a + b) |
| V3 | Băng thông báo (có điều kiện) | `NoticeBanner` | `.notice` — `styles.scss` § `.notice` | **Bốn vai loại trừ nhau**, tối đa một băng render. Thứ tự ưu tiên: (a) "chưa import lần nào" — §5.3 (T9); (b) chỉ đọc vì chọn tháng/năm, **kèm cách thoát** — §5.5.1 (Q37); (c) chỉ đọc vì không có quyền ghi — §5.6.1 (Q39); (d) nhắc kỳ đích khi kỳ đang chọn **không trùng nơi lời ghi sẽ rơi vào** — **hai** ca: một **tuần đã qua**, **và** `Tất cả` (mọi lời ghi vào **tuần hiện tại**, `spec/danh-muc-dti/business-rules.md` §5.3 bước 1) — §5.5. Chọn đúng **tuần hiện tại** thì hai thứ trùng nhau ⇒ **không có băng**. Lối đi tiếp luôn là **link chữ inline**, không phải nút (T11) |
| V4 | Thanh công cụ | `Toolbar` | `.toolbar.no-print` — `styles.scss` § `.toolbar` | Xem §3.1 |
| V5 | Lưới dữ liệu | `DataTable` qua `<app-data-grid class="grid-host">` (biến thể **ghim cột**) + `Table` | `.tablewrap` do **chính component** dựng; chiều cao theo chuỗi flex `page-fill` ⇄ `grid-host`, `scrollHeight="flex"` | Xem §3.2. Biến thể ghim cột do Q30 mở rộng hợp đồng — `Components/DataTable.md` § Variants |
| V6 | Ô trạng thái trong lưới | `Badge` | `.badge.ok` / `.warn` / `.bad` / `.neutral` — `styles.scss` § `.badge` | Ánh xạ Q10 |
| V7 | Ô chênh lệch trong lưới | `DeltaIndicator` | `.delta.up` / `.down` / `.flat` — `styles.scss` § `.delta` | Spec do AGENT A khôi phục 2026-09-05 |
| V8 | Nút trong ô Hành động | `Button` | `.btn.sm` / `.btn.danger` — `styles.scss` § `.btn` | **Ẩn cả cột** khi không có quyền ghi (Q39, §5.6.1); `disabled` khi đang chọn tháng/năm (Q37, §5.5.1) |
| V9 | Dialog Thêm/Sửa chỉ tiêu | `Dialog` + `FormRow` + `Input` + `Button` | `dialog.form-dialog` / `.form-row` / `.form-grid` — `styles.scss` § các mục cùng tên | Xem §3.3 |
| V10 | Dialog Xác nhận xoá | `ConfirmDialog` | `dialog.confirm-dialog`, đúng **hai** nút | `src/FE/src/app/shared/components/confirm-dialog/` đã có sẵn |
| V11 | Dialog Import | `Dialog` + `FormRow` + `Button` | `dialog.form-dialog` | Xem §3.4 |
| V12 | Dialog Kết quả import | `Dialog` + `Button` | `dialog.form-dialog` | Xem §3.4 |

**Class page-local, KHÔNG phải component** — liệt kê thẳng ra đây để không thứ
nào ẩn đi: `.dti-grid-card` (flex column của trang, `doc/Design/Frontend/PlatformManager/Prototypes/index.html:1646`),
`.cell-editable` / `.cell-edit` / `.progressInput` / `.noteInput` (ô sửa inline,
`doc/Design/Frontend/PlatformManager/Prototypes/index.html:1662`), `.import-summary` (khối tóm tắt kết quả import,
`doc/Design/Frontend/PlatformManager/Prototypes/index.html:1783`). Đây là cùng cách xử lý mà
`doc/Design/Frontend/PlatformManager/Screens/06-trang-chu.md` dành cho `.lead` và
`.facts`: ghi thẳng là markup của trang, không phát minh thành component. Chỉ
promote lên `COMPONENTS.md` khi có màn thứ hai cần đúng thứ đó.

### 3.1 Thanh công cụ — dùng NGUYÊN hợp đồng có sẵn, không dựng control mới

Bắt buộc đúng bộ class này, đúng thứ tự DOM
(`doc/Design/Frontend/PlatformManager/Components/Toolbar.md` §Anatomy):

```
.toolbar.no-print
├── .input-icon.search        → <i class="pi pi-search"> + <input type="search">
├── details.filter
│   ├── summary.btn           → "Lọc" + .filter-count (chỉ render khi > 0)
│   └── .filter-panel
│       ├── .form-row × 4     → Nhóm chỉ tiêu · Trạng thái · Năm đánh giá · Kỳ trong năm
│       └── .filter-foot      → "Xoá lọc" (.btn) | "Áp dụng" (.btn.primary)
├── .toolbar-sep
├── .filter-chips             → .filter-chip × N, mỗi chip một .icon-btn gỡ điều kiện
└── .toolbar-actions          → "Import CSV/Excel" (.btn) · "+ Thêm chỉ tiêu" (.btn.primary)
                                 ẨN CẢ HAI khi không có quyền ghi (Q39, §5.6)
```

⚠️ **`.toolbar-sep` đứng TRƯỚC `.filter-chips`** — đây là thứ tự của component
đang chạy (`src/FE/src/app/shared/components/toolbar/toolbar.html`, đối chiếu
2026-09-05) và của `Toolbar.md` §Anatomy. Bản dựng để duyệt đặt chips trước sep;
**bản dựng sai, không phải component**. Thứ tự này không đổi được từ ngoài: cả năm
khối đều nằm trong template của `<app-toolbar>`, trang chỉ chiếu nội dung vào hai
slot `[filter]` và `[actions]`.

- **Dùng component `<app-toolbar>`** (`src/FE/src/app/shared/components/toolbar/`),
  không tự viết lại `<div class="toolbar">`: `showSearch` = true, `hasFilter` =
  true, `filterCount` = số điều kiện **đang áp**, bốn `.form-row` chiếu vào slot
  `[filter]`, hai nút chiếu vào slot `[actions]`, `chips` = danh sách
  `IToolbarChip`, `chipRemove` gỡ đúng một điều kiện.
- **Nút gỡ chip theo đúng component**: một `.icon-btn` mang
  `aria-label = "Bỏ lọc " + nhãn chip`, **không** có thuộc tính `title`, và ký
  hiệu là icon `<i class="pi pi-times">` — không phải ký tự `×`. Bản dựng để
  duyệt ghi `Gỡ điều kiện…` kèm `×`; copy đó **không tạo ra được** bằng
  `<app-toolbar>` mà chính spec này bắt buộc dùng.
- `searchAriaLabel` **phải** đặt lại thành `Tìm mã hoặc tên chỉ tiêu` — thanh
  không có `<label>` nhìn thấy được, nên tên gọi khả truy cập chỉ đến từ input đó.
- **Toolbar cố ý không debounce** (Toolbar.md §Component API) — mỗi trang tự chọn
  độ trễ. Đây là gọi API server-side nên đặt debounce ở page; đề xuất 300 ms.
- `filterApply` / `filterClear` phát **sau khi** panel đóng — trang refetch rồi
  mới render, không để panel treo trên dữ liệu đã đổi.
- **Không có ô sắp xếp** ở màn này (khác Dashboard). Thứ tự mặc định theo mã chỉ
  tiêu.
- **Cách đếm điều kiện — chốt T8 (2026-09-05): `.filter-count` chỉ đếm điều kiện
  KHÁC MẶC ĐỊNH.** Một ô luôn có giá trị mà giá trị đó là mặc định thì **không**
  phải một điều kiện người dùng đã đặt.

  | Ô | Mặc định | Có tính vào `.filter-count` không |
  | --- | --- | --- |
  | Nhóm chỉ tiêu | rỗng (tất cả) | chỉ khi đã chọn một nhóm |
  | Trạng thái | rỗng (tất cả) | chỉ khi đã chọn một trạng thái |
  | Năm đánh giá | **năm hiện tại** | **không** — trừ khi chọn năm khác |
  | Kỳ trong năm | `Tất cả` | **không** — trừ khi chọn một kỳ cụ thể |

  Cùng luật đó áp cho `.filter-chips`: chip chỉ hiện cho điều kiện **thật sự khác
  mặc định**, nên số chip luôn bằng `.filter-count`. Hệ quả cụ thể: bản dựng để
  duyệt hiện `.filter-count` = **2** cho {Nhóm, Năm=2026} — **bản dựng sai**, đúng
  phải là **1** (chỉ Nhóm). Không có điều kiện nào khác mặc định thì `.filter-count`
  **không render** và hàng chip trống.

  Ngoại lệ cố ý: kỳ đích vẫn hiện thường trực ở nơi khác khi nó **không trùng** kỳ
  đang chọn — nhưng đó là **băng nhắc kỳ đích** V3 (§5.5), không phải một chip lọc.
  Hai thứ trả lời hai câu hỏi khác nhau ("tôi đã lọc gì" ≠ "tôi đang ghi vào kỳ
  nào") nên không gộp. ⚠️ Điều kiện của băng **không** phải "kỳ đang chọn ≠ tuần
  hiện tại": ở chế độ `Tất cả` thì `.filter-count` = 0, không chip nào, kỳ đang chọn
  cũng **không** là một tuần — vậy mà băng **vẫn phải hiện** (§5.5). Đọc điều kiện
  theo kỳ đích, đừng đọc theo ô lọc.

Ô **Kỳ trong năm** liệt kê **cả tuần lẫn tháng**, mỗi mốc ghi rõ khoảng ngày
(Q12), cộng một option `all`. Ô này có **hai** vai, và vai thứ hai là mới từ Q20:
nó vừa lọc dữ liệu đang xem, vừa **chọn kỳ đích cho mọi lời ghi** (§5.5).

🛑 **Vai thứ hai chỉ hoạt động với TUẦN — chốt Q37 (2026-09-06).** Nhập liệu của hệ
thống này là **theo tuần**; tháng và năm là **tổng hợp tự tính**, không phải đơn vị
nhập. Ô lọc vẫn liệt đủ cả tuần lẫn tháng, nhưng lựa chọn quyết định bảng **sửa
được hay chỉ đọc**:

| Chọn gì ở `Kỳ trong năm` | Bảng | Kỳ đích của lời ghi |
| --- | --- | --- |
| Một **tuần** (`YYYY-Www`) | **sửa được** | chính tuần đó |
| `Tất cả` (`all`) | **sửa được** | **tuần hiện tại** (Q26 + Q37) |
| Một **tháng** (`YYYY-MM`) | **CHỈ ĐỌC** | — không có |
| Cả năm (nếu ô có option đó) | **CHỈ ĐỌC** | — không có |

⚠️ **Bảng trên còn một điều kiện nữa: KỲ ĐÍCH PHẢI NẰM TRONG NĂM ĐANG LỌC** — chốt
T15 (2026-09-06). Điều kiện này chỉ cắn vào **đúng một ô** của bảng trên:

| `Năm đánh giá` | `Kỳ trong năm` | Kỳ đích | Có nằm trong năm đang lọc? | Bảng |
| --- | --- | --- | --- | --- |
| năm hiện tại | một tuần | tuần đó | ✔ | **sửa được** |
| năm hiện tại | `Tất cả` | tuần hiện tại | ✔ | **sửa được** |
| **năm cũ** (2025) | **một tuần của 2025** | tuần đó | ✔ | **sửa được** |
| **năm cũ** (2025) | **`Tất cả`** | tuần hiện tại (**2026**) | ✘ | **CHỈ ĐỌC** |

**Chỉ lối tắt `Tất cả` bị chặn, không phải cả năm cũ.** Ca hỏng là `Năm = 2025` +
`Kỳ = Tất cả`: theo Q26 lối tắt này ghi vào **tuần hiện tại**, mà tuần hiện tại
thuộc **2026** — một năm người dùng **không nhìn thấy ở đâu trên màn**. Họ đang xem
số của 2025, sửa một dòng, và tin rằng bản ghi đi vào 2025. Cột `Kỳ của số liệu`
cũng không cứu được, vì T14 đã bỏ năm khỏi chuỗi hiển thị (§3.2.2 luật c) — theo
lập luận "mọi dòng đều thuộc năm đang lọc", một lập luận **chỉ đúng khi đọc**.

🛑 **Chọn một tuần cụ thể của năm cũ thì VẪN SỬA ĐƯỢC.** Nhập bù kỳ đã qua là quyết
định đã chốt ở Q20 và T15 **không** lật nó. Khác biệt giữa hai ca là kỳ đích **có
hiện trên màn hình hay không**: chọn `Tuần 33/2025` thì kỳ đích chính là thứ người
dùng vừa chọn; chọn `Tất cả` thì kỳ đích là một tuần của năm khác, không xuất hiện
ở đâu.

*(Bản trước của mục này ghi điều kiện là "`Năm đánh giá` phải là năm hiện tại" và
kết luận `Năm = 2025` chỉ đọc **bất kể** ô kỳ. Câu đó **quá tay** — áp nguyên văn
thì `Tuần 33/2025` cũng bị khoá, tức lật luôn Q20. Sửa 2026-09-06 theo bản hợp đồng
đã chốt.)*

Ca bị chặn xử lý **giống hệt** ca chọn tháng (cùng khuôn: kỳ đích không xác định
được từ thứ đang nhìn thấy): affordance ghi `disabled`, dải băng V3 nêu cách thoát
(§5.5.1). Mã lý do là `PERIOD_OUT_OF_YEAR` (§7.1a).

**Vì sao chặn tháng thay vì "cho nhập rồi tự quy đổi":** một tháng phải được neo
vào một ngày để ra kỳ đích, và mọi cách neo đều sai ở đâu đó. Neo `Tháng 8/2026`
vào 31/08 thì tuần ISO của ngày đó là **tuần 36** — nghĩa là người dùng nhập một
con số *tháng 8* và hệ thống ghi nó thành *tuần 36*, mà tuần 36 có tới 5 ngày nằm
trong **tháng 9**. Cột `Kỳ của số liệu` sẽ hiển thị đúng cái điều vô lý đó
(§3.2.2), và không có cách nào viết một dòng chữ giải thích cho hợp lý. Chặn ở
tầng lựa chọn là cách duy nhất làm câu "nhập cho đúng kỳ đang chọn" luôn đúng.

Xuất báo cáo **không** bị ràng buộc này: `mode=month` vẫn chạy bình thường vì
export là **đọc tổng hợp**, không phải ghi (`spec/dashboard-dti/ui-spec.md` §4).

> 📖 Định dạng nhãn kỳ (quy tắc + 4 khuôn chuỗi): file chủ là
> `spec/dashboard-dti/business-rules.md` §Quy tắc kỳ. Không chép khuôn ra đây.

### 3.2 Lưới

Lưới dùng component dùng chung **`<app-data-grid>`**
(`src/FE/src/app/shared/components/data-grid/`) — đó là **nơi DUY NHẤT khai
`p-table`** trong app (kiểm: `grep -rl "TableModule" src/FE/src/app`). Trang
**không** import `TableModule`, **không** tự dựng `<p-table>`, **không** tự viết
`.tablewrap`: `[lazy]`, `[paginator]`, `[scrollable]`, `scrollHeight="flex"` và khung
`.tablewrap` đều nằm **bên trong** component (`Components/DataTable.md`
§ Reference markup và § Do / Don't).

Trang truyền vào: `rows` · `loading` · `totalCount` · `page` · `pageSize` ·
`dataKey="CriteriaId"` · `minWidth` (tổng `min-width` của bộ cột) ·
`headerTemplate` / `bodyTemplate` / `emptyTemplate`; và nghe `pageChange`
(**1-based**, `{ page, pageSize }`). `rowsPerPageOptions` là input **có sẵn mặc
định** `[10, 20, 50]` — màn này dùng đúng bộ đó nên **không truyền lại**. Vì `.tablewrap` thuộc component nên cái bẫy
`.scroll` (`Components/DataTable.md` § Do / Don't — đặt `.scroll` làm header cuộn
mất và nhốt paginator vào vùng cuộn) **không còn là chỗ trang có thể làm sai**.

🛑 **Chiều cao lưới đi theo chuỗi flex, KHÔNG theo token `--grid-h` — sửa
2026-09-09.** `Components/DataTable.md` § Tokens Used chốt *"`dimension.grid-h` is
**not** used by this component any more — `scrollHeight="flex"` hands sizing to the
`page-fill` ⇄ `grid-host` flex chain"*, và § Normalize mục 6 ghi rõ binding
`scrollHeight="var(--grid-h)"` *"no longer exists"*. Hai việc phải làm **cùng
lúc**:

| Đặt ở đâu | Class |
| --- | --- |
| Trang (§3, đã có) | `.page-fill` |
| Chính thẻ `<app-data-grid>` | **`.grid-host`** |

Quên `.grid-host` thì lưới **âm thầm quay về chiều cao nội dung** — không lỗi biên
dịch, không test nào bắt (`Components/DataTable.md` § Normalize mục 6). *(Bản trước
của file này đặc tả `scrollHeight="var(--grid-h)"` ở ba chỗ — mục này, hàng V5 của
bảng vùng, và §6 Responsive; cả ba đều tả một binding không còn tồn tại. Quyết định
2026-09-09: sửa spec theo component, không sửa component theo spec.)*

**Số dòng mỗi trang mặc định là 10** (Q19) — bằng lưới `Quản trị người dùng`. Ba
lựa chọn `[10, 20, 50]` giữ nguyên. Không đặt một mặc định khác cho lưới này:
`Components/DataTable.md` § Normalize ghi rõ *"Only page size 10 ships"* sau khi
repo dọn đúng chuyện hai lưới hai mặc định ngày 2026-08-29, và đặt 20 ở đây là
tái tạo lại đúng thứ vừa dọn xong.

**KHÔNG có dòng `Hiển thị 1–10 trong 62 bản ghi`** (Q16 c): hợp đồng `DataTable`
không khai `showCurrentPageReport`. Số lượng đã nằm ở `.title` (V2), và ở đó nó
mang `aria-live="polite"` nên sau khi lọc, trình đọc màn hình vẫn có bằng chứng
rằng bộ lọc đã chạy (`doc/huong_dan/wiki-core/fe/15-accessibility.md` §3a).

> 📖 **Bảng cột** — số cột, thứ tự, header verbatim, `min-width` từng cột: file chủ
> là `doc/Design/Frontend/PlatformManager/Screens/02-danh-muc-dti.md`
> § Layout Blueprint. Không chép bảng ra đây. Bộ cột **đã đổi ngày 2026-09-05**:
> Q31 thêm một cột, nên đọc bản mới của file chủ chứ đừng nhớ theo bản cũ.

Bốn điều thuộc về file này chứ không thuộc bảng cột, nên ghi ở đây:

- `.num` (`styles.scss` § `.num`) cho mọi cột số — canh phải + `tabular-nums`, để
  hàng đơn vị thẳng cột giữa các dòng.
- **Đúng hai cột sửa inline**: `Tiến độ %` và `Minh chứng/Ghi chú` (Q9). Mọi cột
  khác chỉ đọc trong lưới.
- Tổng `min-width` của bộ cột vượt xa bề rộng khả dụng → lưới **cuộn ngang** trên
  gần như mọi màn hình. Đó là tiền đề của cả hai mục dưới đây.
- Cột `Kỳ của số liệu` **hiện/ẩn theo chế độ** (Q31) — xem §3.2.2. Đây là cột duy
  nhất của lưới không phải lúc nào cũng có mặt.

#### 3.2.1 Ghim cột — `Mã` bên trái, `Hành động` bên phải (Q30)

**Chốt 2026-09-05.** Lưới ghim **hai** cột: `Mã` ở mép trái, `Hành động` ở mép
phải. Mọi cột còn lại cuộn ngang ở giữa.

Lý do là hành vi thật của màn, không phải trang trí: bộ cột rộng hơn hẳn viewport
nên cuộn ngang là chuyện thường ngày, mà hai cột người dùng cần **cùng lúc** —
"dòng này là chỉ tiêu nào" (`Mã`) và "bấm sửa ở đâu" (`Hành động`) — lại nằm ở hai
đầu đối diện. Cuộn tới cuối để bấm `Sửa` thì mất dấu mình đang ở dòng nào; đó đúng
là ca hỏng mà ghim cột sinh ra để chặn.

⚠️ **Đây là MỞ RỘNG HỢP ĐỒNG `DataTable`, không phải bản vá riêng màn này.** Ghim
cột được khai thành **biến thể chung** của component ở
`doc/Design/Frontend/PlatformManager/Components/DataTable.md` § Variants. File này
**chỉ tuyên bố màn Danh mục dùng biến thể đó và ghim cột nào** — không định nghĩa
lại cơ chế, không đặt tên class riêng, không copy giá trị `min-width` sang đây.

📐 **Ghim cột khai qua input `frozenColumns` của `<app-data-grid>` — chốt
2026-09-09, CHƯA THI CÔNG.** Input đó **chưa tồn tại** trong component đang chạy
(`src/FE/src/app/shared/components/data-grid/data-grid.ts`), phải thêm trước khi
dựng màn này. Tên, kiểu và hành vi chính xác của input thuộc
`Components/DataTable.md` § Variants — file này chỉ nói **hai cột nào** bị ghim.

🛑 **KHÔNG đặt `pFrozenColumn` ở call site.** `pFrozenColumn` và `alignFrozen` là
directive của `TableModule`; dùng chúng trong `headerTemplate` / `bodyTemplate` của
trang buộc **trang** phải import `TableModule`, đúng thứ luật *"`data-grid` là nơi
duy nhất khai `p-table`"* (§3.2) cấm. Hai luật này va nhau, và cách gỡ đã chốt là
**giữ luật, mở hợp đồng component**: trang khai *cột nào bị ghim* qua input,
component chịu trách nhiệm gắn directive lên đúng ô.

Ba hệ quả giao diện thuộc về màn này:

- Cột `Mã` đã là cột đầu và `Hành động` đã là cột cuối trong bảng cột của file chủ
  — ghim **không** đổi thứ tự cột, chỉ đổi hành vi cuộn.
- `Hành động` mang `.no-print` khi in (§6); cột bị ghim vẫn phải biến mất trên bản
  in như trước, ghim không phải lý do giữ nó lại.
- Ở mốc ≤ 560px, hai cột ghim ăn gần hết bề rộng khả dụng. Kiểm bằng mắt trên
  viewport hẹp nhất trước khi coi là xong; nếu không còn chỗ cho cột giữa thì đó là
  vấn đề của hợp đồng `DataTable`, báo về `DataTable.md`, đừng tự tắt ghim ở đây.

#### 3.2.2 Cột `Kỳ của số liệu` — hiện có điều kiện, và tự đổi sau khi lưu (Q31)

**Chốt 2026-09-05, bổ sung luật (c) ngày 2026-09-06.** Lưới có thêm cột `Kỳ của số
liệu`, cho biết **giá trị đang hiển thị trên dòng này thuộc kỳ nào**. Ba luật đi
kèm, cả ba đều bắt buộc:

**(a) Cột chỉ render khi `Kỳ trong năm` = `Tất cả`.**

| Chế độ | Cột `Kỳ của số liệu` | Vì sao |
| --- | --- | --- |
| `Kỳ trong năm` = một kỳ cụ thể | **không render** | mọi dòng cùng một kỳ, và kỳ đó đã hiện ở ô lọc, ở chip và ở băng V3 — cột chỉ tốn chỗ trong một lưới vốn đã phải cuộn ngang |
| `Kỳ trong năm` = `Tất cả` | **render** | `all` là phép chiếu "bản ghi mới nhất trong năm", nên mỗi dòng có thể đến từ một kỳ khác nhau. Không có cột này thì lưới hiện 62 con số trông như cùng một kỳ trong khi chúng không phải |

Ẩn/hiện cột là thay đổi cấu trúc bảng, không phải `visibility: hidden`: dùng
`@if` quanh cả `<th>` lẫn `<td>` để số cột của header và của body luôn khớp nhau.
Đổi chế độ kéo theo đổi số cột → `colspan` của dòng rỗng trong `emptyTemplate` (§5.3) phải
tính theo số cột **đang render**, không hardcode.

**(b) Sau khi lưu, ô đó tự đổi sang TUẦN HIỆN TẠI.**

Đây không phải hiệu ứng phụ, nó là **phản hồi trực quan** của Q26: ở chế độ `Tất
cả`, lời ghi rơi vào **tuần hiện tại** chứ không ghi đè kỳ cũ đang hiển thị. Người
dùng vừa sửa một dòng đang hiển thị số của tuần 31 và thấy ô kỳ nhảy từ
`27/07–02/08` sang `31/08–06/09` biết ngay hai điều: bản ghi mới đã được tạo cho
tuần này, và số của tuần 31 **vẫn còn nguyên**.

*(Bản trước của mục này ghi "kỳ hiện tại". Sau Q37 chỉ còn một đơn vị nhập nên nói
thẳng là **tuần** — "kỳ" để mở khả năng nó là tháng, và khả năng đó đã bị đóng.)*

- Giá trị mới lấy **từ response** của lời ghi (§7.3, §7.4 — response là một dòng
  lưới đầy đủ), không phải do FE tự đặt bằng đồng hồ máy khách. Đồng hồ máy khách
  sai múi giờ hoặc lệch ngày là đủ để ô này nói dối đúng vào lúc nó quan trọng nhất.
- Áp cho **cả hai** đường ghi: dialog `Sửa chỉ tiêu` (V9) và hai ô sửa inline.
- Ở chế độ một **tuần** cụ thể, cột không render nên luật (b) không có gì để làm —
  kỳ đích chính là tuần đang xem và không có gì đổi.
- Ở chế độ **tháng/năm** thì không có lời ghi nào để mà đổi: bảng chỉ đọc (Q37,
  §3.1), và cột cũng không render.

**(c) Chuỗi trong ô: CHỈ khoảng ngày, bỏ số tuần — chốt Q38 (2026-09-06),
dấu gạch theo T14 (2026-09-06).**

| | Giá trị |
| --- | --- |
| Hiển thị | `10/08 – 16/08` |
| **Không** hiển thị | `Tuần 33`, `Tuần 33/2026`, và năm |
| Bề rộng cột | **110px** |
| Ô rỗng (chưa có bản ghi đánh giá nào trong năm) | `—` |

⚠️ **Có khoảng trắng quanh dấu gạch** — `10/08 – 16/08`, không phải `10/08–16/08`.
Chốt T14. Q38 bỏ **số tuần**, và chỉ số tuần: nó **không** lật quy ước dấu gạch của
T5, vốn áp cho toàn sản phẩm. Bề rộng **không** phải căn cứ để lệch ở đây — cả hai
bản đều vừa 110px, nên đổi dấu gạch chỉ đơn thuần tạo ra khuôn thứ năm mà không đổi
lấy được gì. *(Bản trước của bảng này ghi `10/08–16/08` — sai theo T14.)*

⚠️ **Đây là NGOẠI LỆ có chủ đích so với Q12**, và phải được ghi vào § Normalize của
`Screens/02-danh-muc-dti.md` để lần dọn dẹp sau không có ai "sửa cho nhất quán".
Q12 quy định mọi chỗ hiển thị kỳ đều ghi **cả tên kỳ lẫn khoảng ngày**; ở riêng ô
này bỏ tên kỳ vì hai lý do cộng lại:

1. **Cột hẹp.** Đây là cột thứ 14 trong một lưới vốn đã phải cuộn ngang (§3.2.1);
   `Tuần 33 · 10/08 – 16/08` cần gần gấp đôi 110px.
2. **Không còn gì để phân biệt.** Tên kỳ trong khuôn Q12 tồn tại để nói *đơn vị*
   của kỳ là tuần hay tháng. Sau Q37 **mọi dòng trong cột này đều là tuần**, nên
   chữ "Tuần" lặp lại 62 lần mà không mang thêm thông tin nào.

Ngoại lệ dừng ở **đúng ô này**, và nó hẹp hơn bản trước tưởng. Bốn khuôn nhãn kỳ
của `spec/dashboard-dti/business-rules.md` §Quy tắc kỳ **không đổi**, và mọi chỗ
hiển thị kỳ khác trên màn — ô lọc, chip, băng V3, tiêu đề dialog V9 — vẫn theo Q12
đầy đủ.

So với khuôn gần nhất là hàng Lịch sử trên Dashboard (`10/08 – 16/08/2026`, T5), ô
này khác **đúng một điều**:

| | Hàng Lịch sử (T5) | Ô `Kỳ của số liệu` |
| --- | --- | --- |
| Khoảng trắng quanh dấu gạch | có | **có** — giống, theo T14 |
| Năm | có | **BỎ** |

Bỏ năm là an toàn vì mọi dòng trong lưới đều thuộc **năm đang lọc** ở ô `Năm đánh
giá`, và năm đó hiện thường trực trên toolbar — lặp lại nó 62 lần trong một cột
110px không thêm thông tin nào.

**Sửa inline — đúng 2 trường** (Q9). Kích hoạt bằng **bấm đúp**; phần tử mang
`tabindex="0"` + `role="button"` + `title` mô tả thao tác. `Enter`/`Space` mở ô
sửa, `Enter` lưu, `Escape` huỷ — ràng buộc a11y số 2 của
`doc/huong_dan/wiki-core/fe/15-accessibility.md` §4: mọi thao tác làm được bằng
chuột phải làm được bằng bàn phím. Bản duyệt đã có viền focus 2px `--brand` cho
`.cell-editable:focus-visible`.

### 3.3 Dialog "Thêm / Sửa chỉ tiêu" — 10 trường, 2 nhóm

`Dialog` biến thể `form-dialog` (`width: min(560px, 92vw)`), tiêu đề `Thêm chỉ
tiêu` / `Sửa chỉ tiêu`, đóng bằng nút `Đóng` ở `.title` và bằng phím `Escape`.

**Nhóm 1 — bản thân chỉ tiêu** (4 trường, đều bắt buộc, đánh dấu `.required`):

| Trường | Control | Ràng buộc hiển thị |
| --- | --- | --- |
| `Mã` | `input`, `maxlength="20"`, placeholder `vd 1.1` | Mã có thể **3 cấp** (`4.22.11`) — không ràng buộc regex 2 cấp |
| `Tên chỉ tiêu` | `textarea` | |
| `Nhóm` | `select` | 6 nhóm, nạp từ API |
| `Điểm tối đa` | `input[type=number]`, `min=0.01`, `step=0.01` | |

`Nhóm` và `Điểm tối đa` nằm cùng một `.form-grid` (2 cột).

**Nhóm 2 — đánh giá theo kỳ** (6 trường Q9, đều không bắt buộc):

| Trường | Control | Bố cục |
| --- | --- | --- |
| `Tự đánh giá` | `input[type=number]`, `min=0`, `step=0.01` | `.form-grid` cùng `Thẩm định` |
| `Thẩm định` | `input[type=number]`, `min=0`, `step=0.01` | |
| `Trạng thái` | `select`, 4 giá trị Q4 | `.form-grid` cùng `Hạn xử lý` |
| `Hạn xử lý` | `input[type=date]` | |
| `Phụ trách` | `select`, mặc định `— Chưa phân công —`; danh sách người dùng nạp từ `GET /api/users` — xem ghi chú dưới bảng | một hàng riêng |
| `Minh chứng/Ghi chú` | `textarea` | một hàng riêng, **một ô text duy nhất** (Q5) |

**Nguồn dữ liệu ô `Phụ trách` — TÁI DÙNG `GET /api/users`, chốt 2026-09-09.** Trước
lượt này, không contract nào khai nguồn danh sách cho ô đó; nay **không cần endpoint
mới** — dùng lại endpoint đã có của màn `Quản trị người dùng`.

> 📖 Đường dẫn, tham số và shape: `doc/contracts/users.md` § `GET /api/users`.
> Không chép shape sang đây.

Bốn điều thuộc về giao diện, và chỉ có bấy nhiêu:

- **Quyền khớp sẵn theo cấu hình đã chốt.** `UsersController` chặn
  `[Authorize(Roles = SuperAdmin,Admin)]`
  (`src/BE/PlatformManager.Api/Controllers/UsersController.cs:14`), mà Q36 chốt lần
  seed đầu cấp key DTI cho **`Admin` và chỉ `Admin`**
  (`spec/danh-muc-dti/business-rules.md` §6.5) — nên người mở được dialog này đúng là
  người gọi được endpoint. Ai không có quyền ghi thì **không thấy dialog** (§5.6.1).
  ⚠️ Sự trùng khớp đó đến từ **dữ liệu seed**, không phải một ràng buộc được cưỡng
  chế: cấp key DTI cho một vai khác `Admin` qua màn phân quyền là đủ để ô này nhận
  `403` trong khi phần còn lại của dialog chạy bình thường. Xử lý như lỗi tải một ô
  chọn — ô `disabled` kèm câu giải thích tại chỗ, **không** đóng dialog và **không**
  chặn lưu bốn trường danh mục.
- **Option mặc định `— Chưa phân công —` mang giá trị rỗng**, nghĩa là *không phân
  công ai*. Mapper quy nó về đúng giá trị mà DM-4 quy định cho ca không có người phụ
  trách (`doc/contracts/danh-muc-dti.md`, `ownerId` ở §7.3) — **không** gửi chuỗi rỗng.
- **Nhãn hiển thị là họ tên** (`fullName` của `UserDto`), không phải tên đăng nhập —
  cột `Phụ trách` của lưới cũng hiện `ownerName` (§7.1), hai chỗ phải đọc giống nhau.
- **Danh sách có phân trang và có trần** — đừng giả định một lời gọi lấy hết. Số
  người dùng vượt trần thì ô phải cho tìm kiếm (tham số `searchText` của cùng
  endpoint) thay vì lặng lẽ cắt cụt danh sách; luật trần thuộc `doc/contracts/users.md`.

`Chênh lệch` **không xuất hiện** trong dialog — nó là trường tính, chỉ hiển thị ở
lưới. `Tiến độ %` cũng không có trong dialog — sửa inline (Q9).

**Một nút `Lưu chỉ tiêu` = MỘT lời gọi.** Bốn trường danh mục và object
`assessment` sáu trường đi chung một request `PUT /api/criteria/{id}` (hoặc
`POST /api/criteria` khi thêm mới) — CONTRACT DM-4 đã chốt như vậy 2026-09-05, và
lý do là của giao diện: tách đôi tạo ra cửa sổ hỏng nửa vời, trong đó tên chỉ tiêu
đã đổi trong khi dialog đang báo lỗi phần đánh giá.

`assessment` **vắng mặt** ≠ `assessment` có mọi trường rỗng: cái đầu nghĩa là
"không đụng tới dữ liệu đánh giá", cái sau là "xoá trắng dữ liệu đánh giá". Form
phải phân biệt được hai ca đó, đừng để mapper tự dựng object rỗng cho vui.

Lỗi từ server hiện ở `.form-error` (`styles.scss` § `.form-error`) ngay trên
`.dialog-actions`. `.dialog-actions` (`styles.scss` § `.dialog-actions`) chứa `Huỷ` (`.btn`) và
`Lưu chỉ tiêu` (`.btn.primary`).

Response trả về **cùng shape với một dòng của lưới**, nên đóng dialog xong FE
**thay đúng dòng đó tại chỗ**, không gọi lại cả danh sách.

### 3.4 Import — 2 dialog nối tiếp

**Dialog Import** — tiêu đề `Import CSV/Excel`, một `.form-row` chứa
`<input type="file" accept=".csv,.xlsx,.xls">`, một dòng `.muted` hiện tên file
đã chọn, `.dialog-actions` = `Huỷ` · `Nhập dữ liệu` (`.btn.primary`).

Import là thao tác **chạy dài**: `POST /api/import` trả `jobId` rồi FE **poll**
`GET /api/import/{jobId}`. Cách gọi và ràng buộc bắt buộc kèm theo nằm ở
`doc/huong_dan/quy-uoc/fe-api-client.md` §"Long-running operation — poll pattern"
— đọc trước khi viết dòng gọi đầu tiên. Trong lúc `Pending`/`Running`: nút `Nhập
dữ liệu` `disabled`, dialog **không** đóng, và tiến trình phải được thông báo qua
vùng `aria-live` (a11y §4 mục 4).

**Dialog Kết quả import** — mở khi job `Succeeded`. Khối `.import-summary` gồm
một câu tổng hợp (tổng số dòng, số thành công `.ok`, số lỗi `.err`, số chỉ tiêu
tự tạo mới) rồi một `<ul>`, mỗi lỗi một `<li class="err">` theo khuôn
`Dòng {số dòng} — mã "{mã}": {thông điệp}`. Đóng dialog → lưới **refetch**.

Job `Failed` (lỗi hạ tầng) **không** mở dialog kết quả — hiện `Toast` lỗi. Ranh
giới này thuộc contract: lỗi từng dòng đi vào `result.errors`, job crash đi vào
`status: "Failed"` (`doc/contracts/danh-muc-dti.md`).

## 4. Hành động

| Hành động | Điểm vào | Kết quả | Ghi chú |
| --- | --- | --- | --- |
| Tìm kiếm | `.input-icon.search` | refetch về trang 1 | debounce 300 ms, `replaceUrl` |
| Lọc | `details.filter` → `Áp dụng` | refetch về trang 1 | `.filter-count` + `.filter-chips` cập nhật |
| Gỡ một điều kiện | `.icon-btn` (`pi-times`) trên `.filter-chip` | refetch về trang 1 | `aria-label = "Bỏ lọc " + nhãn chip` — §3.1 |
| Xoá toàn bộ lọc | `Xoá lọc` | refetch về trang 1 | không xoá từ khoá tìm kiếm |
| Đổi trang / số dòng | paginator của `DataTable` | refetch | server-side, `[lazy]` |
| Thêm chỉ tiêu 🔒 | `+ Thêm chỉ tiêu` | mở V9 rỗng | |
| Sửa chỉ tiêu 🔒 | `Sửa` trên hàng | mở V9 đã điền | |
| Xoá chỉ tiêu 🔒 | `Xoá` trên hàng | mở V10 → xoá | thông điệp nêu **mã + tên**; xem ghi chú dưới |
| Sửa `Tiến độ %` 🔒 | bấm đúp ô `Tiến độ %` | ghi đè **cả hai** trường inline | FE tự kẹp `[0,100]` trước khi gửi |
| Sửa `Minh chứng/Ghi chú` 🔒 | bấm đúp ô `Minh chứng/Ghi chú` | ghi đè **cả hai** trường inline | |
| Import 🔒 | `Import CSV/Excel` | V11 → poll → V12 | |

🔒 = **hành động ghi**, và cả năm điểm vào của chúng chịu **ba** điều kiện độc lập,
phải cùng đúng thì mới dùng được:

| # | Điều kiện | Không thoả thì | Mã lý do (§7.1a) | Chốt |
| --- | --- | --- | --- | --- |
| 1 | Người dùng có quyền ghi DTI | **ẨN** điểm vào | `NO_WRITE_PERMISSION` | Q39, §5.6.1 |
| 2 | `Kỳ trong năm` là **tuần** hoặc `Tất cả` | **`disabled`** điểm vào | `PERIOD_NOT_WEEKLY` | Q37, §5.5.1 |
| 3 | **Kỳ đích nằm trong năm đang lọc** | **`disabled`** điểm vào | `PERIOD_OUT_OF_YEAR` | T15, §5.5.1 |

FE **không tự đánh giá ba điều kiện này** — đọc `canWrite` / `isEditable` /
`editBlockedBy` ở cấp màn (§7.1a). Bảng trên là để hiểu *vì sao*, không phải để
hiện thực lại.

🛑 **Đừng gộp ba điều kiện thành một cờ `readonly`.** Chúng chia làm hai nhóm với
hai cách hiển thị và hai câu nói khác nhau, và ranh giới là *người dùng có tự thoát
được không*:

| | Nghĩa | Hiển thị | Người dùng thoát bằng cách |
| --- | --- | --- | --- |
| Điều kiện 1 | **"không phải của bạn"** | `hidden` | không tự thoát được — phải được cấp quyền |
| Điều kiện 2 và 3 | **"không phải lúc này"** | `disabled` | đổi bộ lọc: chọn một tuần, hoặc quay về năm hiện tại |

Điều kiện 2 và 3 **cộng dồn chứ không thay nhau** — `editBlockedBy` có thể mang cả
hai mã cùng lúc, và dải băng V3 phải nêu **hết**, không phải một câu chung chung.

⚠️ Đừng đọc điều kiện 3 thành *"năm cũ thì cấm"*: `Năm = 2025` + `Kỳ = Tuần 40/2025`
**vẫn sửa được** (kỳ đích nằm trong 2025). Chỉ `Năm = 2025` + `Kỳ = Tất cả` mới
trượt, vì lối tắt đó ghi vào tuần hiện tại của **2026**. Xem bảng 4 dòng ở §3.1.

🛑 **Mọi hành động ghi đều ghi vào TUẦN ĐANG CHỌN** (Q20 + Q37), không phải vào
"hôm nay" — và khi ô `Kỳ trong năm` đang ở `Tất cả` thì kỳ đích là **tuần hiện
tại** (Q26), không phải kỳ của dòng đang hiển thị. Xem §5.5 và §7.4b; đây là ràng
buộc mạnh nhất của màn hình này và nó chi phối cả năm dòng 🔒 ở trên.

📌 **Sau mỗi lời ghi thành công ở chế độ `Tất cả`, ô `Kỳ của số liệu` của dòng đó
đổi sang tuần hiện tại** (Q31 luật b, §3.2.2). Không phải một hành động riêng của
người dùng, nhưng là **thay đổi nhìn thấy được** mà cả năm dòng 🔒 đều gây ra — nên
phải kiểm nó cùng lúc với việc kiểm giá trị đã lưu, đừng coi là hiệu ứng phụ tự
khắc đúng.

⚠️ **Câu xác nhận xoá KHÔNG được đổi theo dữ liệu của hàng.** CONTRACT DM-5 chốt
BE là nơi quyết xoá cứng hay xoá mềm, và FE chỉ đọc `hardDeleted` trong response
để chọn **thông báo sau khi xoá**. Đừng đoán trước bằng `AssessmentId` để đổi câu
hỏi: trường đó chỉ phản ánh **kỳ đang xem**, không phản ánh lịch sử nhiều năm —
bản cũ làm đúng như vậy và câu xác nhận sai trong đúng ca người dùng cần nó đúng
nhất.

Mọi hành động ghi thành công → `Toast` xác nhận, và hàng/lưới cập nhật lại **từ
response**, không tự sửa state cục bộ rồi coi như xong.

## 5. Trạng thái

### 5.1 Mặc định
Năm = năm hiện tại, kỳ = `Tất cả (mới nhất trong năm)`, không lọc nhóm/trạng
thái, không từ khoá, trang 1, **10 dòng/trang** (Q19). Hai ô sửa inline **bật**.

Năm hệ quả của trạng thái mặc định mà người thi công hay bỏ sót — cả năm đều đến
từ việc **mặc định là chế độ `Tất cả`**:

- **Băng V3 CÓ hiện ngay ở trạng thái mặc định** (§5.5, chốt 2026-09-09): `Tất cả`
  là một trong hai ca có băng nhắc kỳ đích, vì lời ghi rơi vào **tuần hiện tại**
  trong khi mỗi dòng đang hiện số của một kỳ khác nhau. Đây là **đường đi phổ biến
  nhất** của màn, nên băng đó là thứ người dùng thấy gần như mọi lần vào — không
  phải một ca biên. Đừng dựng nó như thông báo chỉ hiện khi có gì bất thường.
- **Cột `Kỳ của số liệu` CÓ render** ở trạng thái mặc định (§3.2.2 luật a). Nghĩa là
  bộ cột người dùng thấy đầu tiên là bộ **đầy đủ**, không phải bộ rút gọn — và mỗi
  ô đọc `10/08 – 16/08` chứ không phải `Tuần 33` (Q38 + T14, luật c).
- **`.filter-count` không render** — cả bốn ô lọc đều ở mặc định (T8, §3.1).
- **Bảng SỬA ĐƯỢC** (nếu có quyền ghi): mặc định thoả **cả ba** điều kiện của §4 —
  `Tất cả` nằm ở nhánh sửa-được của Q37, và `Năm` đang là năm hiện tại nên T15 cũng
  thoả. Đổi **một** trong hai ô đó là rơi sang chỉ đọc (§5.5.1).
- **Lời ghi rơi vào tuần hiện tại** (Q26 + Q37, §5.5), và mỗi lần lưu thì ô kỳ của
  dòng đó đổi theo (§3.2.2 luật b). Đây là đường đi phổ biến nhất của màn hình,
  không phải ca biên — kiểm nó trước, đừng để lại sau cùng.

### 5.2 Đang tải
Mặt nạ loading của `p-table` **bên trong `<app-data-grid>`** (`[loading]`), do preset PrimeNG vẽ — app không tự
style. Toolbar **không** bị khoá, trừ khi đang có thao tác ghi. Không dựng
skeleton riêng: hợp đồng `DataTable` đã có sẵn mặt nạ, thêm cái thứ hai là hai
ngôn ngữ loading trên cùng một màn hình.

🛑 **Màn này KHÔNG tự dựng lớp phủ đang tải** (Q34, 2026-09-05). Quyết định Q34 —
"làm mờ vùng số liệu + vòng quay nhỏ" — chỉ áp cho **Dashboard**, vì Dashboard
không có `p-table` bọc số liệu của nó. Ở đây lớp phủ đã có sẵn trong hợp đồng
`DataTable` và Core đang dùng nó qua `[loading]`, nên tự dựng thêm một lớp nữa là
hai cơ chế loading chồng lên nhau trên cùng một lưới. Trạng thái đang tải của
Dashboard: `spec/dashboard-dti/ui-spec.md` §5.2.

### 5.3 Rỗng
Hai ca **khác nhau**, và chốt T9 (2026-09-05) làm chúng khác nhau **cả về nơi
hiển thị**, không chỉ về câu chữ:

| Ca | Điều kiện | Hiển thị ở đâu |
| --- | --- | --- |
| **Lọc không khớp** | có ít nhất một điều kiện khác mặc định, hoặc có từ khoá | `emptyTemplate` truyền vào `<app-data-grid>` — một ô `.muted` `colspan` hết bảng |
| **Chưa import lần nào** | không điều kiện nào khác mặc định, không từ khoá, tổng số chỉ tiêu = 0 | **`NoticeBanner`** đặt trên lưới (cùng chỗ với V3), **không** phải dòng trống trong bảng |

**Vì sao ca thứ hai chuyển sang `NoticeBanner`** (T9): một dòng `.muted` nằm giữa
khung bảng trống trông như "bảng đang lỗi", và nó **không dẫn đi đâu được**. Ca này
là màn hình **đầu tiên** người dùng thật thấy khi hệ thống mới triển khai, và lúc
đó có đúng **một** việc làm được — nhập dữ liệu vào. Băng phải trỏ thẳng tới nút
`Import CSV/Excel` ở `.toolbar-actions` (V4) và nêu cả lối thứ hai là
`+ Thêm chỉ tiêu`. Đây là đối xứng của Q32 bên Dashboard: chỗ nào rỗng vì **chưa có
việc gì xảy ra** thì dùng băng có lối đi tiếp, chỗ nào rỗng vì **bộ lọc hẹp** thì
dùng thông điệp trong bảng.

- **Lối đi tiếp là LIÊN KẾT CHỮ inline trong thân băng, không phải nút** — chốt T11
  (2026-09-06). Hợp đồng `NoticeBanner` khai `a` inline (§ Anatomy: ink theo mức độ
  nghiêm trọng, weight 700, gạch chân khi hover) nhưng **không có slot cho nút có
  nhãn**. Mở rộng hợp đồng chỉ để đặt một cái nút là đi ngược đúng yêu cầu "dùng lại
  component có sẵn". *(Bản trước của mục này viết `không chứa được nút` như một
  khiếm khuyết và ngụ ý cần nút — T11 chốt ngược lại: link inline là đủ và là cách
  đúng.)*
- Băng dùng biến thể **mặc định** (thông tin), không phải `.warn` / `.bad` — chưa
  import lần nào không phải lỗi, không phải cảnh báo.
  (`doc/Design/Frontend/PlatformManager/Components/NoticeBanner.md` § Variants.)
- Khi băng hiện, lưới **vẫn render** với đủ header và một câu rỗng ngắn trong `emptyTemplate` —
  không ẩn bảng đi, vì header là thứ cho người dùng biết file import cần những cột
  nào.
- Băng và hai vai kia của V3 **loại trừ nhau trên thực tế**: chưa có chỉ tiêu nào
  thì không có gì để ghi vào tuần nào, và cũng không có gì để chỉ-đọc. Nếu nhiều
  điều kiện cùng đúng thì thứ tự ưu tiên là **rỗng → chỉ đọc (Q37/Q39) → nhắc kỳ
  đích**: cái trên nói việc cần làm trước cái dưới.
- Khi không có quyền ghi (Q39), băng rỗng **bỏ hai lối đi** vì cả hai đều đã bị ẩn —
  lúc đó nó chỉ còn là câu thông báo. Đừng trỏ tới một nút không tồn tại.
- `colspan` tính theo **số cột đang render**, nhớ rằng cột `Kỳ của số liệu` chỉ có
  ở chế độ `Tất cả` (§3.2.2).

Chuỗi verbatim của cả hai ca thuộc `Screens/02-danh-muc-dti.md` § Copy (AGENT A) —
không chép sang đây.

### 5.4 Lỗi
- Lỗi HTTP chung do `httpErrorInterceptor` bắt và bắn `Toast`; màn hình không tự
  dựng lại thông điệp.
- Lỗi **hợp lệ hoá** khi lưu dialog → `.form-error` trong dialog, dialog **không**
  đóng, input sai gắn `aria-invalid` + `aria-describedby` (a11y §4 mục 3).
- Lỗi khi lưu inline → hoàn nguyên giá trị ô về giá trị trước đó **và** hiện
  `Toast`; không để ô hiển thị giá trị chưa lưu như thể đã lưu.
- Job import `Failed` → `Toast`, dialog import đóng, lưới **không** refetch.

Hai mã lỗi của contract cần **cách hiển thị riêng**, không gộp vào toast lỗi chung:

| Mã lỗi | Khi nào gặp | Hiển thị |
| --- | --- | --- |
| `CRITERIA.ASSESSMENT_CONFLICT` (409) | người khác vừa sửa cùng bản ghi đánh giá của cùng kỳ | Không im lặng ghi đè. Báo rõ dữ liệu đã đổi, tải lại đúng dòng đó, **giữ nguyên** giá trị người dùng vừa gõ ở đâu đó để họ nhập lại được — mất chữ vừa gõ là cách nhanh nhất khiến người ta ngừng tin màn hình |
| `CRITERIA.DUPLICATE_CODE` (409) | mã trùng khi thêm/sửa | `.form-error` trong dialog, focus về ô `Mã` |

`version` (token optimistic concurrency) đọc từ dòng lưới và **gửi lại** ở cả
dialog lẫn sửa inline — đó là thứ duy nhất làm `ASSESSMENT_CONFLICT` phát hiện
được. Bỏ nó đi thì mọi lần ghi đều thắng, và lỗi mất dữ liệu sẽ im lặng. Q20 làm
`version` **quan trọng hơn trước**, không kém đi: khi nhiều người cùng được sửa
lại một kỳ đã qua, hai lời ghi vào cùng một bản ghi là chuyện thường chứ không
còn là ca hiếm.

> `CRITERIA.ASSESSMENT_READONLY_PERIOD` **đã bị bỏ** theo Q20 — không còn kỳ nào
> bị khoá ghi, nên không còn lỗi này để hiển thị. Bản trước của mục này có nó.

### 5.5 Nhập cho tuần đã qua — hành vi đã chốt (Q20 + Q26 + Q37)

**Người dùng chọn TUẦN, rồi nhập cho đúng tuần đó.** Không có tuần nào "chỉ đọc"
vì đã cũ, không có khái niệm "Live", và lời ghi **không** rơi vào ngày hôm nay.

| Điều | Hành vi |
| --- | --- |
| Kỳ đích khi đang chọn **một tuần** | **chính tuần đó**, gửi tường minh trong request |
| Kỳ đích khi đang ở `Tất cả` | **tuần hiện tại** (Q26 + Q37) — xem bảng dưới |
| Sửa được ở tuần nào | **mọi tuần**, kể cả tuần của năm cũ |
| Chọn **tháng** hoặc **năm** ở ô `Kỳ trong năm` | **KHÔNG ghi được** — bảng chỉ đọc (Q37), xem §5.5.1 |
| Năm cũ + `Tất cả` (kỳ đích rơi ra ngoài năm đang lọc) | **KHÔNG ghi được** — bảng chỉ đọc (T15), xem §5.5.1 |
| Năm cũ + **một tuần của năm đó** | **GHI ĐƯỢC** — kỳ đích nằm trong năm đang lọc |
| Cột `Hành động`, nút `.toolbar-actions`, hai ô inline | bật theo `canWrite` / `isEditable` ở **cấp màn** (§7.1a), không suy lại từ bộ lọc |

🛑 **"Kỳ đã qua" ≠ "tháng".** Hai thứ này dễ bị gộp và chúng ngược nhau: tuần **đã
qua** thì sửa được (Q20), còn **tháng** thì không sửa được kể cả tháng này (Q37).
Điều bị chặn là **đơn vị**, không phải **thời điểm**.

🛑 **"Năm đã qua" cũng KHÔNG bị chặn — chỉ lối tắt `Tất cả` của năm cũ bị.** T15
chặn đúng ca `Năm ≠ năm hiện tại` **kèm** `Kỳ = Tất cả`, vì lối tắt đó ghi vào tuần
hiện tại của năm nay. Sửa dữ liệu tuần 40 của 2025 thì vẫn làm được như thường: đặt
`Năm = 2025`, chọn **đúng tuần 40** ở ô `Kỳ trong năm` — lúc đó kỳ đích tường minh,
nằm trong năm đang xem, và không có gì để đoán sai.

*(Bản trước của mục này nói `Năm = 2025` chỉ đọc **bất kể** ô kỳ. Sai — nó lật Q20.
Sửa 2026-09-06; điều kiện đúng là **kỳ đích nằm trong năm đang lọc**, §3.1.)*

**Ca `Tất cả` — chốt Q26 (2026-09-05).** `all` **không phải một kỳ**, nó là phép
chiếu "bản ghi mới nhất của mỗi chỉ tiêu trong năm". Câu hỏi "ghi vào đâu" từng để
mở; nay đã có đáp án:

| Câu hỏi | Chốt Q26 |
| --- | --- |
| Có chặn ghi khi đang ở `Tất cả` không? | **KHÔNG.** Mọi control ghi vẫn bật |
| Có hỏi lại người dùng trước khi lưu không? | **KHÔNG.** Không dialog xác nhận kỳ, không bước trung gian |
| Lời ghi rơi vào kỳ nào? | **Tuần hiện tại** — tuần ISO chứa ngày hôm nay (Q37 làm câu này chỉ còn một nghĩa) |
| Có ghi đè kỳ cũ đang hiển thị trên dòng đó không? | **KHÔNG.** Số của kỳ cũ giữ nguyên; đây là một bản ghi mới cho tuần hiện tại |

Điểm dễ hiểu sai nhất nằm ở hai hàng cuối, nên nói thẳng: người dùng nhìn thấy số
của tuần 31 trên dòng đó và sửa nó, nhưng cái được ghi là **giá trị mới của tuần
hiện tại**, không phải sửa lại tuần 31. Hai chuyện đó cho ra hai lịch sử khác hẳn
nhau, và không có gì trên màn hình phân biệt được chúng — **trừ** cột `Kỳ của số
liệu` (§3.2.2). Đó chính là lý do Q31 tồn tại, và là lý do cột đó **bắt buộc** ở
chế độ `Tất cả` chứ không phải tuỳ chọn cho đẹp.

Ba điều giao diện **phải** làm cho đúng, vì Q20 + Q26 làm cái giá của việc nhầm kỳ
cao hơn hẳn:

1. **Kỳ đích phải nhìn thấy được ngay tại chỗ đang gõ.** Người dùng chọn `Tuần
   31` từ tháng trước rồi cuộn xuống sửa 20 dòng — không được để họ tin là đang
   sửa tuần này. Tối thiểu: kỳ đang chọn hiện thường trực trên `.filter-chip`, và
   dialog `Sửa chỉ tiêu` (V9) nêu kỳ đích trong tiêu đề hoặc ngay dưới nó.
2. **Ở chế độ `Tất cả`, dialog V9 nêu kỳ đích là TUẦN HIỆN TẠI**, không phải kỳ của
   dòng đang mở. Lấy nhãn tuần hiện tại từ `GET /api/dashboard/periods` (§7.2),
   đừng tự suy ra bằng đồng hồ máy khách.
3. **Sau khi lưu ở chế độ `Tất cả`, ô `Kỳ của số liệu` của dòng đó đổi sang tuần
   hiện tại** — luật (b) của §3.2.2. Đây là xác nhận duy nhất người dùng nhận được
   rằng bản ghi rơi vào tuần nào, nên không được bỏ qua vì "chỉ là một ô hiển thị".

Hai hệ quả **ngoài giao diện** mà file này chỉ ghi nhận, không định nghĩa — luật
thuộc `spec/danh-muc-dti/business-rules.md` §Quy tắc ghi:

- **Báo cáo đã xuất có thể lệch về sau.** Một file `.xlsx` xuất tuần 31 hôm nay
  không còn khớp dữ liệu tuần 31 sau khi có người sửa lại kỳ đó. Đây là hệ quả đã
  được chấp nhận của Q20, không phải lỗi.
- **Phải truy được ai sửa kỳ nào, lúc nào.** Dùng trường audit của `BaseEntity`
  đang có.

`NoticeBanner` ở **V3 đổi nghĩa**: nó không còn là dải "lịch sử — chỉ đọc" (khái
niệm đó đã bị Q20 xoá), mà là dải **nhắc kỳ đích** — để người dùng biết chắc mình
đang nhập cho tuần nào.

**Điều kiện hiện băng — HAI ca, chốt 2026-09-09:** băng hiện khi **kỳ đang nhìn
thấy không trùng nơi lời ghi sẽ rơi vào**.

| `Kỳ trong năm` | Lưới đang hiện số của | Lời ghi rơi vào | Băng V3 |
| --- | --- | --- | --- |
| **tuần hiện tại** | tuần hiện tại | tuần hiện tại | **KHÔNG** — hai thứ trùng nhau |
| **một tuần đã qua** | chính tuần đó | chính tuần đó | **CÓ** — nhắc rằng đang nhập cho tuần cũ |
| **`Tất cả`** | **mỗi dòng một kỳ khác nhau** | **tuần hiện tại** | **CÓ** — thêm 2026-09-09 |

🛑 **Ca `Tất cả` mới là ca nguy hiểm hơn, và nó từng bị bỏ sót.** Ở ca tuần đã qua,
thứ người dùng nhìn và thứ họ ghi **vẫn là một kỳ** — băng chỉ nhắc kỳ đó không phải
tuần này. Ở `Tất cả` thì khác hẳn: `all` là phép chiếu "bản ghi mới nhất trong năm"
nên dòng này hiện số của tuần 29, dòng kia của tuần 33, mà **mọi** lời ghi đều rơi
vào **tuần hiện tại** (`spec/danh-muc-dti/business-rules.md` §5.3 bước 1: `period =
"all"` ⇒ kỳ đích là tuần ISO chứa hôm nay). Người dùng sửa một dòng đang hiện số của
tuần 29 và giá trị đi vào tuần 33 — không có gì trên màn nói ra, vì đây là ca **hợp
lệ theo thiết kế** nên **không mã lỗi nào chặn**.

Đây đúng là cơ chế T15 đã mô tả — *"bộ chọn kỳ nói 'ghi được', còn lệnh ghi thì đi
chỗ khác"* (`spec/danh-muc-dti/business-rules.md` §5.3, khối T15) — chỉ khác ở chỗ
T15 là bản **xuyên năm** nên chặn được bằng `400
CRITERIA.ASSESSMENT_PERIOD_OUT_OF_YEAR`, còn bản **trong cùng năm** thì hợp lệ. Vì
vậy **dải băng là chỗ duy nhất báo được**, không phải một lời nhắc cho đẹp.

Hai thứ khác cũng nói về kỳ đích ở ca `Tất cả` và **không thay** được băng: dialog V9
nêu kỳ đích (mục 2 ngay trên) chỉ hiện khi đã mở dialog — không cứu được **sửa
inline**; còn cột `Kỳ của số liệu` (§3.2.2 luật b) chỉ đổi **sau khi** đã lưu. Băng là
thứ duy nhất nói **trước**.

Chuỗi verbatim của cả hai ca thuộc `Screens/02-danh-muc-dti.md` § Copy — không định
nghĩa ở đây.

### 5.5.1 Bộ lọc đưa bảng về CHỈ ĐỌC — hai ca (Q37 + T15)

**Chốt 2026-09-06.** Hai ca **khác nhau về nguyên nhân nhưng giống hệt nhau về cách
xử lý**, nên gộp vào một mục:

| Ca | Mã (§7.1a) | Điều kiện | Thoát bằng cách |
| --- | --- | --- | --- |
| **Q37** | `PERIOD_NOT_WEEKLY` | `Kỳ trong năm` là **tháng** hoặc cả năm | chọn một **tuần**, hoặc `Tất cả` |
| **T15** | `PERIOD_OUT_OF_YEAR` | kỳ đích **không nằm trong năm đang lọc** — thực tế là ca `năm cũ` + `Tất cả` | chọn một **tuần cụ thể của năm đang xem**, hoặc quay về năm hiện tại |

Cả hai **khác hẳn** trạng thái chỉ-đọc-vì-thiếu-quyền ở §5.6.1 — nguyên nhân khác,
lối thoát khác, nên **không gộp thành một nhánh template**:

| | Chỉ đọc vì bộ lọc (Q37 + T15) | Chỉ đọc vì không có quyền ghi (Q39) |
| --- | --- | --- |
| Nguyên nhân | lựa chọn của chính người dùng | tài khoản không được cấp key |
| Người dùng tự thoát được không | **CÓ** — đổi bộ lọc | **KHÔNG** — phải được cấp quyền |
| Affordance ghi | **hiện nhưng `disabled`** — *"không phải lúc này"* | **ẩn hẳn** — *"không phải của bạn"* |
| Có dải nhắc không | **CÓ**, và dải phải nêu cách thoát | có, nhưng chỉ để giải thích |

**Vì sao `disabled` chứ không ẩn:** người dùng này **có** quyền ghi và sẽ ghi ngay
sau đây; ẩn nút đi rồi hiện lại khi họ đổi ô lọc là làm toolbar nhảy và làm người
ta tưởng mình vừa mất quyền. `disabled` giữ nút ở nguyên chỗ và nói "chưa phải
lúc". Ngược lại, người không có quyền (§5.6) sẽ **không bao giờ** dùng được nút đó,
nên để nó nằm đấy mãi mãi chỉ là mời gọi vào ngõ cụt.

**Bốn affordance bị `disabled`** — đúng bằng bộ ở §5.6, không hơn: `+ Thêm chỉ
tiêu`, `Import CSV/Excel`, nút `Sửa`/`Xoá` trên hàng, và hai ô sửa inline (bấm đúp
không mở ô sửa, và ô mất `tabindex="0"` + `role="button"` để bàn phím không rơi vào
một control không làm gì).

**Lối thoát phải nói rõ trong V3, và phải nói ĐÚNG cái đang hỏng.** Dải băng không
được chỉ báo "chỉ đọc" — nó phải nói **làm gì để sửa được**, và nguồn của câu đó là
mảng `editBlockedBy` (§7.1a): **một dòng cho mỗi mã**, theo đúng thứ tự mảng. Một
câu chung chung kiểu "chọn lại bộ lọc để sửa" bắt người dùng thử từng ô.

Ca `Năm = 2025` + `Kỳ = Tháng 8` là lý do mảng phải mang **nhiều** phần tử: sửa
riêng đơn vị kỳ thành `Tất cả` thì vẫn trượt tiếp `PERIOD_OUT_OF_YEAR`. Nêu cả hai
mã cùng lúc cho người dùng thấy đích đến trong một lần — **một tuần cụ thể của
2025**. Chuỗi verbatim thuộc `Screens/02-danh-muc-dti.md` § Copy.

**Cột `Kỳ của số liệu` render hay không tuỳ ca** — nó chỉ phụ thuộc chế độ kỳ
(§3.2.2 luật a), không phụ thuộc trạng thái chỉ đọc:

| Ca | Cột `Kỳ của số liệu` |
| --- | --- |
| Q37 (chọn tháng) | **không render** — không phải chế độ `Tất cả` |
| T15 (`Năm = 2025` + `Kỳ = Tất cả`) | **CÓ render** — vẫn là chế độ `Tất cả` |

Ca T15 là chỗ duy nhất cột này xuất hiện trên một bảng chỉ đọc. Nó vẫn có ích: nó
cho biết mỗi dòng đến từ tuần nào của 2025. Nhưng nó **không** nói dòng đó thuộc năm
nào — T14 đã bỏ năm khỏi chuỗi. Đó chính là lý do T15 chặn ghi thay vì cho ghi rồi
trông chờ cột này cảnh báo.

### 5.6 Quyền

Chưa đăng nhập → `/dang-nhap?returnUrl=/danh-muc/dti`. Còn cờ
`mustChangePassword` → `/doi-mat-khau`. **Hết. Không có nhánh thứ ba** — thiếu
quyền ghi thì vẫn vào được (Q39, §5.6.1).

**Quyền ghi — chốt Q27 (2026-09-05): MỘT permission-key DTI duy nhất.**

| Câu hỏi | Chốt |
| --- | --- |
| Bao nhiêu key cho màn này | **một** — dùng chung cho thêm / sửa / xoá / sửa inline / import |
| Có tách "sửa kỳ hiện tại" ≠ "sửa kỳ cũ" không | **KHÔNG.** Ai có key thì sửa được **mọi kỳ**, kể cả kỳ đã qua |
| Có khái niệm "chốt kỳ" / "khoá kỳ" không | **KHÔNG tồn tại** ở bất kỳ tầng nào |
| Tên key | do `doc/contracts/danh-muc-dti.md` khai theo quy ước permission-key đang có — FE đọc từ đó, **không tự đặt** |

Hệ quả giao diện, và chỉ có bấy nhiêu:

- Màn hình **không** có ma trận bật/tắt theo từng kỳ. Không dựng logic "tuần này
  sửa được, tuần kia không" — nó không tồn tại. Bản trước của spec này còn dấu vết
  đó qua khái niệm "Live"; Q20 đã xoá khái niệm, Q27 đóng nốt câu hỏi quyền.
- **Quyền đọc từ CẤP MÀN, không phải từng dòng** (§7.1a, chốt 2026-09-06):
  `canWrite` quyết **ẩn**, `isEditable` quyết **`disabled`**, `editBlockedBy` cho
  biết lý do. FE không suy diễn lại từ `year`/`period`/tên vai trò. *(Bản trước ghi
  `isEditable` là trường của dòng — sai kể từ khi DM-2 chốt lại.)*

Ẩn affordance theo quyền **không thay** cho kiểm quyền phía BE — hai lớp độc lập
(`fe-routing-guard.md` §5). Q27 rút gọn câu hỏi *ai* được ghi xuống một key,
**không** rút gọn số lớp phải kiểm: BE vẫn phải từ chối lời ghi của người không có
key, kể cả khi FE đã ẩn hết nút.

### 5.6.1 Không có quyền ghi — vào được, chỉ đọc (Q39)

**Chốt 2026-09-06.** Người không có key DTI **vẫn vào được** `/danh-muc/dti` và
thấy đầy đủ dữ liệu; chỉ khác là không có gì để bấm.

| Thứ | Có quyền ghi | KHÔNG có quyền ghi |
| --- | --- | --- |
| Vào được route | có | **có** — không guard, không redirect |
| Mục sidebar | hiện | **hiện** — không ẩn theo quyền |
| Xem lưới, tìm kiếm, lọc, phân trang | có | **có** |
| Xuất báo cáo *(ở màn Dashboard)* | có | **có** |
| `+ Thêm chỉ tiêu` (`.toolbar-actions`) | hiện | **ẨN** |
| `Import CSV/Excel` (`.toolbar-actions`) | hiện | **ẨN** |
| `Sửa` / `Xoá` trong cột `Hành động` | hiện | **ẨN** |
| Sửa inline hai ô | bật | **TẮT** |

**Vì sao không chặn route** — lý do là phép đo, không phải sở thích: Dashboard ở
`/trang-chu` đã hiện **đủ 62 chỉ tiêu** cho mọi người đăng nhập (Q21, bảng chi tiết
9 cột ở V5 của `spec/dashboard-dti/ui-spec.md`). Chặn màn Danh mục vì thế **không
giấu được số liệu nào** — nó chỉ tạo ra một chuyển hướng khó hiểu tới một trang có
cùng dữ liệu. Khác biệt thật giữa hai màn là **có nút sửa hay không**, nên đó đúng
là chỗ quyền được thi hành.

Bốn ràng buộc thi công:

1. **ẨN, không `disabled`.** Người này không bao giờ dùng được các nút đó, nên một
   nút xám nằm mãi ở đấy chỉ là ngõ cụt. Ngược lại với ca chỉ-đọc-vì-chọn-tháng
   (§5.5.1), nơi nút **phải** `disabled` chứ không ẩn — hai ca, hai cách xử lý, và
   bảng so sánh ở §5.5.1 là chỗ giữ sự khác biệt đó.
2. **Cột `Hành động` ẩn CẢ CỘT khi không còn nút nào trong đó** — đừng để lại một
   cột trống chiếm chỗ trong lưới đã phải cuộn ngang. Cột này đang bị ghim bên phải
   (§3.2.1); bỏ cột đi thì chỉ còn **một** cột ghim, và đó là cấu hình hợp lệ của
   biến thể chứ không phải ca cần xử lý riêng.
3. **Ô sửa inline mất luôn ngữ nghĩa control**: bỏ `tabindex="0"`, bỏ
   `role="button"`, bỏ `title` mô tả thao tác. Chỉ chặn sự kiện bấm đúp là để lại
   một ô mà bàn phím Tab vẫn dừng vào và trình đọc màn hình vẫn đọc là "nút" —
   đúng dạng lỗi a11y mà `doc/huong_dan/wiki-core/fe/15-accessibility.md` §4 gọi
   tên.
4. **Không dựng trạng thái rỗng riêng.** Trang vẫn là trang bình thường, đầy dữ
   liệu. Nếu cần một dòng giải thích thì đó là `NoticeBanner` ở V3 với biến thể
   **mặc định** (thông tin) — không phải `.warn`, không phải `.bad`: không có
   quyền ghi là **cấu hình tài khoản**, không phải lỗi và không phải cảnh báo.

FE lấy quyền từ đâu, và trường nào của API phản ánh nó — thuộc
`doc/contracts/danh-muc-dti.md` (AGENT B khai trong cùng vòng 2026-09-06). FE
**không** tự suy quyền từ tên vai trò.

## 6. Responsive

Ba mốc, đều là mốc **đang có sẵn** trong Core — không đặt mốc mới.

| Mốc | Hành vi |
| --- | --- |
| ≥ 981px | Sidebar cố định; `main` giới hạn `--container-max-width`; lưới cuộn ngang khi tổng `min-width` của bộ cột vượt bề rộng khả dụng |
| ≤ 980px | `.shell-content` bỏ `margin-left`, sidebar thành drawer mở bằng hamburger ở topbar |
| ≤ 560px | `styles.scss` § `@media (max-width: 560px)` (khối mở đầu bằng `.toolbar .search`) — `.input-icon.search` chiếm trọn hàng; mọi con trực tiếp của `.toolbar` nhận `flex:1` **trừ** `.filter` và `.filter-chips`; `.toolbar-actions` mất `margin-left:auto`, chiếm trọn hàng; `.toolbar-sep` ẩn; `.filter-panel` co còn `min(320px, 86vw)`. cùng mốc đó, `.form-grid` rớt về **1 cột**, nên 3 cặp trường của V9 xếp dọc. Dialog rộng `min(560px, 92vw)` |

Chiều cao vùng cuộn **KHÔNG** lấy từ token `--grid-h` (sửa 2026-09-09, §3.2): nó
đến từ chuỗi flex `.page-fill` ⇄ `.grid-host` ⇄ `scrollHeight="flex"`, nên lưới co
theo chiều cao khả dụng **thật** của viewport — kể cả khi thanh địa chỉ trình duyệt
di động thu vào/nhả ra. `--grid-h` / `--grid-h-min` vẫn còn sống, nhưng chỉ cho
`.tablewrap.scroll` thuần (`Components/Table.md`), **không** dùng ở màn này.

**Hai cột ghim (Q30) là ràng buộc responsive, không chỉ là hiệu ứng cuộn.** Ở mốc
≤ 560px, `Mã` + `Hành động` chiếm phần lớn bề rộng khả dụng và phần cuộn ở giữa co
lại theo. Kiểm bằng mắt trên viewport hẹp nhất trước khi coi là xong; hết chỗ thì
báo về `Components/DataTable.md` § Variants, **không** tự tắt ghim riêng cho màn
này — tắt một chỗ là biến biến thể chung trở lại thành bản vá riêng, đúng thứ Q30
vừa gỡ bỏ.

**Bộ cột ≤ 560px còn phụ thuộc chế độ kỳ** (§3.2.2): ở `Tất cả` có thêm cột
`Kỳ của số liệu`, ở một kỳ cụ thể thì không. Đừng đo bề rộng bằng một chế độ rồi
kết luận cho cả hai.

**In ấn** (`fe-ui-conventions.md` §"In ấn"): `.toolbar` mang `.no-print`;
paginator, cột `Hành động`, mọi dialog và `Toast` cũng phải `.no-print`. Không ép
chiều cao cố định cho bảng khi in — để trình duyệt ngắt trang tự nhiên, nghĩa là
`@media print` phải gỡ `max-height` của vùng cuộn. Quy tắc `@media print` đặt tập
trung ở `styles.scss` (§ `@media print`), không rải vào từng component.

## 7. Ánh xạ trường UI ↔ trường dữ liệu

**Casing** (`fe-api-client.md` §"Quy tắc casing"): DTO giữ **nguyên xi**
`camelCase` server trả về; model app `PascalCase` + prefix `I`; mapper đặt trong
`services/` của feature và là **nơi duy nhất** casing đổi.

⚠️ **Hệ quả của casing đã chốt mà mapper phải xử lý:** trường `null` **không ra
dây** — nó **vắng mặt** khỏi JSON (`doc/contracts/danh-muc-dti.md` §0). Một chỉ
tiêu chưa có đánh giá không trả `"selfScore": null`, nó **không có khoá
`selfScore`**. Mapper phải đọc "vắng mặt" và "null" như nhau, và mọi ô trong bảng
7.1 mang dấu `?` đều rơi vào ca này.

Cột "DTO" dưới đây bám CONTRACT DM-2, trạng thái **AGREED** 2026-09-05 — nếu tên
trường đổi thì card là nguồn chốt, bảng này đuổi theo.

### 7.1 Hàng lưới (`GET /api/criteria`)

| Cột UI | DTO (camelCase) | Model app (PascalCase) | Kiểu | Ghi chú |
| --- | --- | --- | --- | --- |
| *(khoá hàng)* | `criteriaId` | `CriteriaId` | guid | `dataKey` của `p-table` |
| Mã | `code` | `Code` | string | ≤ 20 ký tự, có thể 3 cấp |
| Tên | `name` | `Name` | string | |
| Nhóm | `groupName` (+ `groupCode`, `groupId`) | `GroupName` / `GroupCode` / `GroupId` | string / guid | `groupId` dùng cho bộ lọc |
| Điểm tối đa | `maxScore` | `MaxScore` | number | > 0 |
| Tự đánh giá | `selfScore` | `SelfScore` | number \| null | null → `—` |
| Thẩm định | `verifiedScore` | `VerifiedScore` | number \| null | null → `—` |
| Chênh lệch | `diff` | `Diff` | number \| null | **TÍNH** = **Thẩm định − Tự đánh giá** (Q25, đổi chiều 2026-09-05). FE **không** tự tính lại khi BE đã trả. Màu: dương → `.delta.up` (xanh), âm → `.delta.down` (đỏ), 0 → `.delta.flat` (xám) |
| Trạng thái | `status` | `Status` | string \| null | 4 giá trị Q4 |
| Phụ trách | `ownerName` (+ `ownerId`) | `OwnerName` / `OwnerId` | string \| null | null → `—` |
| Hạn xử lý | `deadline` | `Deadline` | date \| null | hiển thị `dd/MM/yyyy` |
| Tiến độ % | `progressPercent` | `ProgressPercent` | number \| null | sửa inline, kẹp `[0,100]` |
| Minh chứng/Ghi chú | `note` | `Note` | string \| null | sửa inline, **một ô text** (Q5) |
| Kỳ của số liệu | *(trường kỳ của dòng — xem ghi chú dưới bảng)* | *(theo tên trường DTO)* | string? | **THÊM 2026-09-05 (Q31).** Chỉ render khi `period = "all"` (§3.2.2). Đọc-only. Hiển thị **chỉ khoảng ngày** `10/08 – 16/08` (Q38 + T14), 110px. `null`/vắng mặt (chỉ tiêu chưa có bản ghi đánh giá nào trong năm) → `—` |
| *(không hiển thị)* | `assessmentId`, `assessmentDate` | `AssessmentId`, `AssessmentDate` | guid? \| date? | định danh bản ghi đánh giá của kỳ; **không** dùng để đoán trước xoá cứng/mềm |
| *(không hiển thị)* | `version` | `Version` | string? | token optimistic concurrency — gửi lại khi ghi (§5.4) |

🛑 **`isEditable` KHÔNG còn là trường của dòng** — chuyển lên **cấp màn** ngày
2026-09-06, xem §7.1a. Bản trước của bảng này có nó ở đây và ghi *"FE đọc PER
ROW"*; **cả hai đều sai** kể từ khi DM-2 chốt lại. Đừng đi tìm nó trong `items[]`.

⚠️ **`Kỳ của số liệu` cần một trường DTO mà CONTRACT DM-2 chưa khai lúc file này
được viết.** Q31 yêu cầu API trả **kỳ của từng dòng** ở chế độ `all`; AGENT B khai
trường đó vào DM-2 trong cùng vòng 2026-09-05. Ba ràng buộc FE phụ thuộc vào, ghi
ra để đối chiếu khi card về:

- Trường phải mang **định danh kỳ** (`"YYYY-Www"` / `"YYYY-MM"`), không phải một
  ngày. `assessmentDate` đã có sẵn nhưng **không dùng được** cho việc này: một ngày
  không tự nói nó thuộc tuần ISO nào, và bắt FE quy ngày → kỳ là dựng nguồn sự thật
  thứ hai cho đúng phép tính mà `spec/dashboard-dti/business-rules.md` §Quy tắc kỳ
  đang giữ.
- Nhãn hiển thị thì FE dựng từ định danh kỳ theo khuôn chung — **trừ khi** DM-2
  chọn trả sẵn nhãn như `periodLabel` của Dashboard (§7.2 của
  `spec/dashboard-dti/ui-spec.md`). Card là nguồn chốt, bảng này đuổi theo.
- Ở `period` = một kỳ cụ thể, trường có thể vắng mặt — cột không render nên FE
  không được coi sự vắng mặt đó là lỗi dữ liệu.

`totalCount` không nằm trong hàng — nó là trường của `PagedList` bọc ngoài
(`{ items, page, pageSize, totalCount }`), và đi vào **hai** chỗ: caption ở `.title`
(V2) và `[totalRecords]` của paginator. Với `period = "all"` nó đếm **chỉ tiêu**,
không đếm bản ghi đánh giá — đó chính là lý do caption đọc được là `62 chỉ tiêu`.

### 7.1a Ba trường quyền ghi — CẤP MÀN, nằm cạnh `items`

**Chốt 2026-09-06.** Ba trường này ở **cùng cấp với `items`** trong response của
`GET /api/criteria`, **không** nằm trong từng dòng:

| Trường | Kiểu | Nghĩa |
| --- | --- | --- |
| `canWrite` | bool | Người dùng **có quyền ghi DTI** hay không (điều kiện 1 của §4). Chỉ phụ thuộc tài khoản, không phụ thuộc bộ lọc |
| `isEditable` | bool | **Cả ba** điều kiện của §4 cùng đúng ⇒ ghi được **ngay bây giờ, với bộ lọc hiện tại** |
| `editBlockedBy` | string[] | Danh sách mã lý do đang chặn. **Luôn có mặt** — ghi được thì là `[]`, không phải `null` |

**Vì sao lên cấp màn:** cả ba điều kiện đều thuộc **request**, không thuộc dòng —
quyền là của tài khoản, đơn vị kỳ và năm là của bộ lọc. Không dòng nào trong cùng
một response có thể khác dòng nào, nên một bản sao trên mỗi dòng vừa thừa **vừa vô
dụng đúng ở ca cần nó nhất**: lưới **rỗng** (ca T9) không có dòng nào để đọc, mà
`+ Thêm chỉ tiêu` và `Import CSV/Excel` vẫn phải quyết ẩn hay hiện. Đây là lỗ hổng
từng ghi ở §9 và nay đã đóng.

**`canWrite` là trường duy nhất dùng để ẨN.** `isEditable` dùng để `disabled`. Đừng
hoán đổi — đó đúng là ranh giới `hidden` ≠ `disabled` của §4.

#### Ba mã của `editBlockedBy` — thứ tự cố định

| # | Mã | Nghĩa | Giao diện |
| --- | --- | --- | --- |
| 1 | `NO_WRITE_PERMISSION` | tài khoản không có key ghi DTI (Q39) | **ẩn** affordance ghi (§5.6.1) |
| 2 | `PERIOD_NOT_WEEKLY` | `Kỳ trong năm` là tháng hoặc cả năm (Q37) | **`disabled`** + nhắc chọn tuần (§5.5.1) |
| 3 | `PERIOD_OUT_OF_YEAR` | kỳ đích **không nằm trong năm đang lọc** (T15) | **`disabled`** + nhắc đổi bộ lọc (§5.5.1) |

**Mảng luôn theo đúng thứ tự trên**, nên FE render tuần tự mà không phải sắp lại.

**Luật số phần tử — không đối xứng, và cố ý:**

| Trạng thái | `editBlockedBy` |
| --- | --- |
| Không có quyền ghi | **đúng MỘT** phần tử: `["NO_WRITE_PERMISSION"]` |
| Có quyền, mọi điều kiện đạt | `[]` |
| Có quyền, một hoặc hai điều kiện trượt | **MỌI** điều kiện đang trượt, theo thứ tự |

Hai vế đó có lý do khác nhau, đừng "sửa cho nhất quán":

- **Không có quyền ⇒ chỉ một mã.** Hai mã kia là lời mời *"đổi bộ lọc rồi sẽ ghi
  được"* — mời một người vĩnh viễn không ghi được đi đổi bộ lọc là dẫn họ vào ngõ
  cụt. Người này còn **không nhìn thấy** affordance nào để mà bật.
- **Có quyền ⇒ liệt kê hết.** Ca `Năm = 2025` **và** `Kỳ = Tháng 8` mà chỉ trả một
  mã thì người dùng sửa xong cái thứ nhất, tưởng đã xong, rồi vấp tiếp cái thứ hai.
  Trả cả hai cho họ thấy đích đến trong một lần: **một tuần cụ thể của 2025**.

**Ba ràng buộc thi công:**

1. **FE KHÔNG tự suy lại ba điều kiện** từ `year` / `period` / tên vai trò. Đọc
   `canWrite`, `isEditable`, `editBlockedBy` và làm theo. Tự suy là dựng nguồn sự
   thật thứ hai cho một luật đã đổi ba lần trong hai ngày.
2. **Mảng phải xử lý được nhiều phần tử.** Đừng viết `editBlockedBy[0]` — dải băng
   V3 nêu **mọi** thứ đang chặn (§5.5.1).
3. **Mã lạ không được làm mở khoá.** Gặp mã chưa biết thì vẫn coi là bị chặn (vì
   `isEditable` đã `false`) và hiện một câu chung; **không** bỏ qua rồi bật control.
   Bỏ sót theo hướng an toàn, không theo hướng cho ghi.

Response của **DM-4** (dialog) và **DM-6** (sửa inline) **không còn** `isEditable` —
chúng trả một dòng lưới, mà `isEditable` nay không thuộc dòng. Sau khi lưu, trạng
thái ghi **không đổi** (bộ lọc và quyền không đổi vì một lời ghi), nên FE **giữ
nguyên** ba trường cấp màn đang có; chỉ refetch danh sách mới nạp lại chúng.

### 7.2 Bộ lọc → tham số

| Điều khiển UI | Query param API | Query param URL | Giá trị |
| --- | --- | --- | --- |
| Ô tìm kiếm | `search` | `q` | chuỗi tự do; khớp Mã **hoặc** Tên, không phân biệt hoa/thường và **không phân biệt dấu** — FE không tự bỏ dấu trước khi gửi |
| Nhóm chỉ tiêu | `groupId` | `groupId` | guid; rỗng = tất cả |
| Trạng thái | `status` | `status` | đúng 1 trong 4 chuỗi tiếng Việt nguyên văn của Q4; rỗng = tất cả |
| Năm đánh giá | `year` | `year` | int; mặc định = năm hiện tại. Năm cũ **vẫn sửa được** nếu chọn một tuần cụ thể; chỉ `năm cũ` + `Tất cả` mới chỉ đọc (T15, §3.1) |
| Kỳ trong năm | `period` | `period` | `all` (mặc định) \| `YYYY-Www` \| `YYYY-MM` |
| Paginator | `page`, `pageSize` | `page`, `pageSize` | 1-based; mặc định `pageSize = 10` (Q19), trần 200; ô chọn hiện {10, 20, 50} |

⚠️ `status` sai giá trị → **400**, server **không** âm thầm bỏ lọc. Nghĩa là ô lọc
trạng thái chỉ được phát đi đúng bốn chuỗi đó — đừng gửi mã rút gọn kiểu `chua` /
`dang` / `xong` mà bản dựng dùng cho thuộc tính `value` của `<option>`. Nếu muốn
giữ mã rút gọn trên URL cho gọn thì mapper phải quy đổi trước khi gọi API.

⚠️ **`year` + `period` ở đây mang HAI vai** (Q20): vừa là điều kiện lọc của lưới,
vừa là **kỳ đích của mọi lời ghi** (§7.3, §7.4). Trước Q20 chúng chỉ có vai thứ
nhất, còn lời ghi luôn rơi vào hôm nay — đừng mang giả định cũ đó theo.

> 📖 Quy tắc kỳ (tuần ISO thứ Hai → Chủ nhật, tháng dương lịch, cách quy một
> `period` về khoảng ngày, và định dạng nhãn kỳ): file chủ là
> `spec/dashboard-dti/business-rules.md` §Quy tắc kỳ.

Nguồn danh sách năm/kỳ: `GET /api/dashboard/periods` (CONTRACT DB-3, **AGREED**),
gọi qua service **dùng chung** đặt ở `shared/services/` vì cả hai màn DTI cùng
dùng — quy tắc "≥2 feature dùng thì không còn ở `modules/<feature>/`"
(`doc/huong_dan/quy-uoc/fe-architecture.md`).

### 7.3 Dialog Thêm/Sửa → payload

**Một** request cho cả dialog — `POST /api/criteria` (thêm) hoặc
`PUT /api/criteria/{id}` (sửa). **Không có query param**; **kỳ đích** đi trong
thân request ở `assessment.period` (Q20, CONTRACT DM-4).

| Nhóm trường dialog | Vị trí trong payload |
| --- | --- |
| Mã / Tên / Nhóm / Điểm tối đa | phẳng ở gốc: `code`, `name`, `groupId`, `maxScore` |
| *(không phải trường của form)* | `assessment.period` — **kỳ đích**, bắt buộc khi có `assessment` |
| Tự đánh giá / Thẩm định / Trạng thái / Phụ trách / Hạn xử lý / Minh chứng | trong object lồng `assessment`: `selfScore`, `verifiedScore`, `status`, `ownerId`, `deadline`, `note`, kèm `version` |

Object lồng chứ không phải sáu trường phẳng trộn lẫn, vì hai nhóm ghi vào **hai
bảng khác nhau** với vòng đời khác nhau — và vì `assessment` **vắng mặt** phải
phân biệt được với `assessment` có mọi trường rỗng (xoá trắng dữ liệu đánh giá).

Response là **một dòng lưới đầy đủ** (cùng shape §7.1) → thay dòng tại chỗ.

### 7.4 Sửa inline → payload

`PUT /api/criteria/{id}/assessment`, **không có query param**; `period` là trường
thân request và là **kỳ đích** (Q20, CONTRACT DM-6).

🛑 **FE LUÔN gửi CẢ HAI trường**, kể cả khi người dùng chỉ sửa một — lấy giá trị
hiện tại của trường còn lại từ dòng đang có trong bộ nhớ. Ngữ nghĩa của endpoint
là `PUT` **ghi đè cả hai**, nên bỏ trống một trường sẽ **null-hoá** trường đó.
Gửi kèm `version`.

| Trường | Payload |
| --- | --- |
| *(không phải trường của ô sửa)* | `period` — **kỳ đích**, bắt buộc |
| Tiến độ % | `progressPercent` — FE kẹp `[0,100]` trước khi gửi |
| Minh chứng/Ghi chú | `note` |

Response cũng là một dòng lưới đầy đủ → thay dòng tại chỗ, không refetch danh sách.

### 7.4b Quy `Kỳ trong năm` → `period` của lời ghi — một hàm, một chỗ

Cả §7.3, §7.4 và import (§7.5) đều cần **cùng** một giá trị `period`, và cả ba
endpoint đều **từ chối `"all"`** (`400 CRITERIA.ASSESSMENT_PERIOD_INVALID`). Q26
định nghĩa phép quy đổi:

| Ô `Kỳ trong năm` đang chọn | `period` gửi đi |
| --- | --- |
| một **tuần** (`YYYY-Www`) | **chính nó** |
| `Tất cả` (`all`) | **định danh TUẦN hiện tại** (Q26 + Q37) — FE thay thế trước khi gửi |
| một **tháng** (`YYYY-MM`) hoặc cả năm | **không có lời ghi nào để gửi** — bảng chỉ đọc (Q37, §5.5.1) |

Sau Q37, `period` gửi đi **luôn có dạng `YYYY-Www`**. Một `YYYY-MM` ra dây là dấu
hiệu điều kiện chỉ-đọc ở §5.5.1 đã bị bỏ sót ở đâu đó, không phải một ca cần quy
đổi thêm — **đừng** viết nhánh quy tháng về tuần cho "chắc". Lý do ở §3.1: mọi cách
neo tháng vào một tuần đều sai.

⚠️ **Và `YYYY` của `period` phải khớp `Năm đánh giá` đang lọc** (T15) — đây chính là
phát biểu chính xác của điều kiện 3. Chọn `Tuần 33/2025` khi đang lọc 2025 thì khớp,
ghi được. Chọn `Tất cả` khi đang lọc 2025 thì FE thay bằng tuần hiện tại của
**2026**, lệch năm, và đó là ca T15 chặn.

Ràng buộc thi công: khi thay `all` bằng tuần hiện tại, **kiểm năm khớp trước khi
gửi**. Lệch thì đó là lỗi logic ở tầng bật/tắt control (`isEditable` lẽ ra đã
`false`), **không** phải ca cần tự sửa bằng cách đổi năm hộ người dùng.

Ba ràng buộc, cả ba đều là chỗ đã hỏng ở bản trước hoặc dễ hỏng lần sau:

1. **Đặt phép quy đổi ở đúng MỘT chỗ** — một hàm trong `services/` của feature, ba
   đường ghi cùng gọi. Ba bản sao của cùng một `if` là ba cơ hội để một đường ghi
   vào kỳ khác hai đường kia, và triệu chứng sẽ là "số nhảy lung tung" chứ không
   phải một lỗi trỏ về nguyên nhân.
2. **Không lấy tuần hiện tại từ đồng hồ máy khách.** Đọc từ
   `GET /api/dashboard/periods` (§7.2) — cùng nguồn với ô chọn kỳ. Máy khách lệch
   ngày hoặc lệch múi giờ vào đêm Chủ nhật là đủ để lời ghi rơi sang tuần khác.
3. **`"all"` không bao giờ được ra dây.** Nếu chưa nạp xong danh sách kỳ mà người
   dùng bấm lưu, **chặn ở FE** (nút chờ) thay vì gửi `"all"` và đọc `400` — lỗi đó
   không nói được gì có ích cho người dùng. Cùng lý do, `YYYY-MM` cũng không bao
   giờ ra dây (Q37).

### 7.5 Import

| UI | Endpoint / trường |
| --- | --- |
| Chọn file | `POST /api/import`, `multipart/form-data`, **field tên `file`**, chấp nhận `.csv` `.xlsx` `.xls` (Q6) → trả `{ jobId }` với HTTP **200** (không phải 202) |
| Kỳ đích của cả file | field `period` **bắt buộc**, đi cùng `multipart` (Q20). Luôn là **một tuần** `YYYY-Www` (Q37). Quy từ ô `Kỳ trong năm` theo §7.4b — ở chế độ `Tất cả` thì đó là **tuần hiện tại** (Q26), không phải "kỳ của từng dòng trong file" |
| Đang chạy | `GET /api/import/{jobId}` → `status` ∈ `Pending` / `Running` / `Succeeded` / `Failed` |
| Câu tổng hợp | `result.totalRows`, `result.successCount`, `result.errorCount`, `result.criteriaCreatedCount` |
| Danh sách lỗi | `result.errors[]` = `{ rowNumber, code?, message }` |
| Lỗi hạ tầng | `errorMessage` khi `status = "Failed"` (khi đó `result` **vắng mặt**) |

📌 **Nhật ký của import phải ghi ĐÚNG người nạp file, không phải `"system"`**
(Q35). Đây là hành vi đã chốt, nhưng nó **phụ thuộc một hạng mục Core chưa làm** và
là **điều kiện tiên quyết** trước khi bật import — mô tả đầy đủ (hiện trạng, ba
bước cần làm, phần nào của Core phải mở rộng) ở
`spec/danh-muc-dti/business-rules.md` §Import. Không mô tả lại ở đây.

Phần thuộc về giao diện, và chỉ có bấy nhiêu: **V12 không hiển thị tên người
nạp** — dấu vết đó nằm ở trường audit của bản ghi, không phải ở dialog kết quả. FE
**không** gửi kèm tên/ID người dùng trong `multipart` để "giúp" BE ghi nhật ký:
danh tính lấy từ phiên đăng nhập ở phía server, và một trường do máy khách gửi lên
thì máy khách sửa được.

`rowNumber` là **số dòng trong chính file người dùng gửi**, dòng 1 là header — câu
lỗi trên V12 phải in đúng số đó để người dùng mở file ra sửa được ngay. Đừng đánh
số lại theo thứ tự dòng dữ liệu.

Lỗi ở **bước 1** (trước khi có `jobId`) hiển thị ngay trong V11, không mở V12:
`IMPORT.FILE_MISSING` · `IMPORT.FILE_EMPTY` · `IMPORT.FORMAT_UNSUPPORTED` ·
`IMPORT.FILE_TOO_LARGE`, tất cả `400`. Lưu ý `FORMAT_UNSUPPORTED` có thể xảy ra
với file **đúng phần mở rộng** — server nhận diện định dạng bằng magic byte, nên
một file `.xlsx` đổi tên từ thứ khác vẫn bị từ chối. Thông điệp phải nói theo
hướng "nội dung file không phải CSV/Excel", không phải "sai đuôi file".

File import mang 11 cột, khớp header `spec/danh-muc-dti/dti-mau-an-danh-62-dong.csv`. Ánh xạ
cột → trường và luật xử lý dòng lỗi thuộc `spec/danh-muc-dti/business-rules.md`
§Import, không lặp lại ở đây.

⚠️ **Cột `Chênh lệch` trong file BA gửi sẽ NGƯỢC DẤU với cột `Chênh lệch` trên
lưới** (Q25, 2026-09-05). Không phải lỗi import: `diff` là **trường tính**, hệ
thống tự tính lại theo `Thẩm định − Tự đánh giá` và **không đọc** cột đó từ file.
Nhưng giao diện phải lường trước câu hỏi của người dùng — ai mở file gốc ra đối
chiếu với màn hình sẽ thấy `1.4` là `+5,00` trong file và `−5,00` trên lưới. Nếu
V12 hoặc `Screens/02-danh-muc-dti.md` § Copy có nhắc tới cột này thì phải nói rõ
đây là chiều tính mới, không phải dữ liệu bị sai.

🛑 **Sau import, cột `Tiến độ %` để TRỐNG** (Q24) — người dùng tự nhập, hệ thống
**không** suy ra từ `Tự đánh giá / Điểm tối đa`. Hệ quả phải nói thẳng với người
dùng ngay tại V12, vì nếu không nó trông y hệt một lỗi:

> Thanh tiến độ theo nhóm và biểu đồ trên Dashboard vẽ theo `Tiến độ %` (Q11).
> Nên **ngay sau khi import xong, Dashboard sẽ trống / 0%** cho tới khi có người
> nhập `Tiến độ %`. Đây là hành vi **đã được người dùng chấp nhận**, không phải
> lỗi nhập liệu và không phải lỗi import.

Vì vậy dialog Kết quả import (V12) phải nêu bước tiếp theo, không chỉ báo "xong":
sau câu tổng hợp, thêm một dòng nói rằng còn phải nhập `Tiến độ %` thì Dashboard
mới có số. Chuỗi verbatim thuộc `Screens/02-danh-muc-dti.md` § Copy.

## 8. Chuỗi hiển thị và i18n

✅ **Hạ tầng i18n ĐÃ CÓ và đang chạy** (đối chiếu source 2026-09-05):

| Có gì | Ở đâu |
| --- | --- |
| `@ngx-translate/core` + `@ngx-translate/http-loader` trong `dependencies` | `src/FE/package.json` — kiểm: `grep -n 'ngx-translate' src/FE/package.json` |
| Bảng dịch `vi` + `en` có nội dung thật | `src/FE/public/i18n/vi.json`, `src/FE/public/i18n/en.json` |
| Cơ chế nạp + đổi ngôn ngữ tại chỗ + đặt `<html lang>` + nhãn PrimeNG | `src/FE/src/app/core/i18n/core-i18n.ts` |
| Test canh khuôn khoá, parity `vi`↔`en`, parity tham số `{{…}}` | `src/FE/src/app/app-i18n.spec.ts` |

Nghĩa là **chuỗi mới ở màn này phải đi qua khoá dịch ngay từ đầu**, không viết
literal rồi hẹn bọc sau. Ba ràng buộc, cả ba đều là thứ **không đỏ lúc biên dịch**:

- **Khoá VIẾT HOA phải LỒNG hai cấp.** `ngx-translate` coi dấu chấm là ký tự phân
  cấp, nên khoá phẳng chứa dấu chấm **tra không trúng** và thư viện **không báo
  lỗi** — nó trả về chính chuỗi khoá, và người dùng đọc được tên khoá giữa giao
  diện. Có test canh.
- **Thêm khoá là thêm ở CẢ `vi` lẫn `en`**, kèm đúng bộ tham số `{{…}}`. Thiếu một
  bên nghĩa là người dùng ngôn ngữ kia đọc câu của ngôn ngữ khác qua fallback.
- **Mỗi câu là một chuỗi trọn vẹn có chỗ cắm tham số.** Cấm nối chuỗi bằng `+` để
  ghép câu (thông điệp xác nhận xoá dựng từ mã + tên là ca điển hình), và cấm tự
  chọn số ít/số nhiều bằng `if` — caption `{n} chỉ tiêu` là **một** chuỗi có tham
  số đếm.

> 📖 Khuôn khoá dịch và cách khai một khoá mới: `doc/huong_dan/wiki-core/fe/08-i18n.md`.

⚠️ **Hai file luật đang mô tả sai hiện trạng này** — nêu ra để người có thẩm quyền
sửa, tôi không tự sửa: `doc/huong_dan/wiki-core/fe/08-i18n.md` § Điểm xuất phát và
`doc/huong_dan/quy-uoc/fe-ui-conventions.md` §i18n vẫn dặn *"chưa bọc chuỗi nào
ngay bây giờ vì thư viện chưa cài"*. Ai đọc hai file đó rồi dựng màn hình mới sẽ
viết literal tiếng Việt trên một hạ tầng dịch đã sẵn sàng, và khối lượng rà lại
chỉ tăng lên.

Copy verbatim từng chuỗi thuộc `Screens/02-danh-muc-dti.md` (AGENT A); không chép
sang đây — §5 của `.claude/CLAUDE.md`, một chủ đề một file chủ.

## 9. Cần chốt — KHÔNG tự quyết

Vòng 2026-09-05 (Q25–Q34, T7–T9) đóng **bảy** mục; vòng 2026-09-06 (Q35–Q39,
T10–T15 + hợp đồng DM-2 cuối vòng) đóng nốt **hai** mục nữa. Vòng **2026-09-09** chốt
thêm bốn điều (chiều cao lưới · cách ghim cột · nguồn ô `Phụ trách` · điều kiện hiện
băng V3 — bốn hàng cuối của bảng dưới) và mở **một** mục mới, mục 4 dưới đây. Danh
sách còn lại đúng những gì thật sự chưa có đáp án:

1. ~~**`showCurrentPageReport`.**~~ **CHỐT 2026-09-06: giữ TẮT.** Lý do quyết định
   không phải hợp đồng mà là **thông tin đã có sẵn trên màn**: `.title` đang mang
   `62 chỉ tiêu` kèm `aria-live="polite"` — vừa là con số nhìn thấy, vừa là con số
   trình đọc màn hình đọc lên sau mỗi lần lọc. Bật thêm dòng `Hiển thị 1–10 trong 62`
   là viết con số đó lần thứ hai, và một con số viết hai chỗ là một con số có thể
   tự mâu thuẫn với chính nó.

   Lý do thứ hai vẫn giữ nguyên giá trị: bật lại là **mở rộng hợp đồng `DataTable`**
   và phải áp cho **cả** lưới `Quản trị người dùng`. ⚠️ Q30 vừa mở rộng hợp đồng đó
   một lần (ghim cột) — đừng đọc thành tiền lệ. Q30 mở rộng vì **hai** lưới đều sẽ
   cần khi bộ cột rộng; cái này chỉ một màn muốn, và đó chính là khác biệt.

2. ~~**Trường DTO cho cột `Kỳ của số liệu`**~~ — **ĐÃ KHỚP 2026-09-06.** CONTRACT
   DM-2 khai `assessmentPeriod` (`"YYYY-Www"` — tuần ISO) và `assessmentPeriodLabel`
   (chuỗi BE dựng sẵn). Sau Q37, `assessmentPeriod` **luôn** là một tuần, không bao
   giờ là `"YYYY-MM"`. Ba ràng buộc FE ở §7.1 đã khớp; không còn việc phải đối chiếu.

3. ~~**API truyền "vì sao không sửa được" như thế nào.**~~ — **ĐÃ ĐÓNG
   2026-09-06.** DM-2 chuyển `isEditable` lên **cấp màn** và thêm `canWrite` +
   `editBlockedBy` bên cạnh `items` (§7.1a). Cả hai lỗ hổng đều được giải quyết bởi
   cùng một thay đổi: `canWrite` là nguồn cấp màn nên **lưới rỗng vẫn quyết được**
   ẩn hay hiện hai nút, và `editBlockedBy` mang **mã lý do** nên giao diện phân biệt
   được `hidden` với `disabled` và nêu đúng thứ đang chặn. Không còn chặn thi công
   §5.5.1 hay §5.6.1.

4. **Hai file mô tả cùng khe V3 bằng hai taxonomy khác nhau — cần người duyệt hợp
   nhất, ĐỪNG tự gộp.** Ghi nhận 2026-09-09:

   | | Phân vai theo trục | Có vai "không có quyền ghi" (Q39) |
   | --- | --- | --- |
   | File này (§3, hàng V3) | *chưa import · chỉ đọc vì kỳ · chỉ đọc vì quyền · nhắc kỳ đích* | **có** |
   | `doc/Design/Frontend/PlatformManager/Screens/02-danh-muc-dti.md` § Layout Blueprint | **bộ lọc**: năm cũ (T15) · tháng (Q37) · tuần đã qua · mặc định | **không** |

   Hai cách chia không ánh xạ 1-1: một bên hỏi *"vì sao có băng"*, bên kia hỏi *"bộ
   lọc đang ở đâu"*. Hợp nhất là quyết định về **taxonomy dùng chung cho cả hai
   file**, không phải việc sửa chữ ở một chỗ — nên nó nằm ở đây thay vì được một
   agent tự quyết. Cho tới lúc đó: file này là nguồn cho **điều kiện hiện băng**,
   `Screens/…` là nguồn cho **chuỗi verbatim** (§5).

> **Đã chốt, không còn là câu hỏi mở** — ghi lại để người đọc bản trước không mang
> theo giả định cũ. **Bốn hàng cuối là của vòng 2026-09-09**; các hàng trên thuộc hai
> vòng 2026-09-05 và 2026-09-06:
>
> | Từng là câu hỏi mở | Chốt |
> | --- | --- |
> | Dialog gửi 1 hay 2 request | **Một** request, object `assessment` lồng — DM-4 (§7.3) |
> | Sửa inline là PATCH hay PUT | `PUT` **ghi đè cả hai** trường — DM-6 (§7.4) |
> | `pageSize` mặc định | **10** — Q19 (§3.2). *Bản trước của dòng này ghi 20 — sai.* |
> | `Tiến độ %` khởi tạo khi import | **Để trống**, người dùng tự nhập — Q24 (§7.5) |
> | Sửa được dữ liệu kỳ đã qua không | **Được** — Q20 (§5.5). Khái niệm "Live" và lỗi `ASSESSMENT_READONLY_PERIOD` đã bỏ |
> | Ai được **xem** Dashboard | Mọi người đăng nhập — Q21 (§5.6) |
> | Export có theo bộ lọc | **Có** — Q23, thuộc màn Dashboard |
> | Tên thư mục spec | `spec/danh-muc-dti/` + `spec/dashboard-dti/` (2026-09-05); ba chỗ trỏ tên cũ đã sửa |
> | **Ghi khi `Kỳ trong năm` = `Tất cả`** | **Rơi vào KỲ HIỆN TẠI** — Q26 (§5.5, §7.4b). Không chặn ghi, không hỏi lại, không ghi đè kỳ cũ đang hiển thị |
> | **Quyền GHI của màn này** | **MỘT permission-key DTI**, áp cho **mọi kỳ** kể cả kỳ đã qua — Q27 (§5.6). Không tách theo kỳ, không có "chốt kỳ" |
> | **Cột đóng băng** | **CÓ** — `Mã` trái + `Hành động` phải, và là **biến thể chung** của `DataTable` chứ không phải bản vá riêng màn — Q30 (§3.2.1) |
> | **Cột `Kỳ của số liệu`** | **THÊM**, chỉ hiện ở chế độ `Tất cả`, và tự đổi sang tuần hiện tại sau khi lưu — Q31 (§3.2.2) |
> | **Route của màn** | **`/danh-muc/dti`** — Q33 (§2) |
> | **Màu của `Chênh lệch`** | Công thức đổi thành **`Thẩm định − Tự đánh giá`**; dương → xanh, âm → đỏ, 0 → xám — Q25 (§7.1). Chiều mới làm màu **khớp ý nghĩa nghiệp vụ**: bị bác điểm ra số âm màu đỏ |
> | **Cách đếm điều kiện lọc** | Chỉ đếm điều kiện **khác mặc định** → `{Nhóm, Năm=2026}` ra **1**, không phải 2 — T8 (§3.1) |
> | **Danh mục rỗng vì chưa import lần nào** | **`NoticeBanner`** trỏ tới nút Import, không phải dòng trống trong bảng — T9 (§5.3) |
> | **Nhập được theo đơn vị kỳ nào** | **CHỈ TUẦN.** Chọn tháng/năm ⇒ bảng chỉ đọc, affordance ghi `disabled`, dải băng nêu cách thoát — Q37 (§3.1, §5.5.1). Export `mode=month` **vẫn chạy** |
> | **Cột `Kỳ của số liệu` hiển thị chuỗi gì** | **Chỉ khoảng ngày** `10/08 – 16/08` — bỏ số tuần (Q38) và bỏ năm, nhưng **giữ khoảng trắng quanh dấu gạch** của T5 (T14). §3.2.2 luật c |
> | **Không có quyền ghi thì thấy gì** | **Vào được, chỉ đọc.** Ẩn `+ Thêm chỉ tiêu` / `Import` / `Sửa` / `Xoá`, tắt sửa inline; **không** chặn route, **không** ẩn mục menu — Q39 (§5.6.1) |
> | **Dải băng dùng nút hay link** | **Link chữ inline** — T11 (§5.3). Hợp đồng `NoticeBanner` không có slot nút, và mở rộng nó chỉ để có một cái nút là đi ngược yêu cầu dùng lại component sẵn có |
> | **Dấu gạch trong cột `Kỳ của số liệu`** | **CÓ khoảng trắng**: `10/08 – 16/08` — T14 (§3.2.2 luật c). Q38 bỏ **số tuần**, không lật quy ước dấu gạch T5 của toàn sản phẩm; cả hai bản đều vừa 110px nên bề rộng không phải căn cứ để lệch |
> | **Kỳ đích rơi ra ngoài năm đang lọc** | **Bảng CHỈ ĐỌC** — T15 (§3.1, §5.5.1), xử lý giống ca chọn tháng của Q37. Thực tế chỉ cắn vào ca `năm cũ` + `Tất cả`: lời ghi rơi vào tuần hiện tại của **2026**, một năm không hiện ở đâu trên màn. `năm cũ` + **một tuần của năm đó** thì **vẫn sửa được** (Q20 không bị lật). Điều kiện ghi vì thế có **ba** vế, không phải hai |
> | **FE đọc quyền ghi ở đâu** | **CẤP MÀN**, cạnh `items`: `canWrite` (⇒ ẩn) · `isEditable` (⇒ `disabled`) · `editBlockedBy` (mảng mã lý do, luôn có mặt) — DM-2 chốt lại 2026-09-06 (§7.1a). **KHÔNG** còn là trường của dòng, và DM-4/DM-6 **không** trả nó nữa |
> | **Chiều cao lưới** | **Chuỗi flex `page-fill` ⇄ `grid-host` + `scrollHeight="flex"`** — 2026-09-09 (§3.2, §6). Token `--grid-h` **không** dùng cho lưới này. Spec sửa theo component đang chạy, không sửa component theo spec |
> | **Cách ghim cột về mặt kỹ thuật** | 📐 **Input `frozenColumns` của `<app-data-grid>`**, chưa thi công — 2026-09-09 (§3.2.1). **Không** đặt `pFrozenColumn` ở call site: nó buộc trang import `TableModule`, phá luật *"`data-grid` là nơi duy nhất khai `p-table`"*. Giữ luật, mở hợp đồng component |
> | **Nguồn dữ liệu ô `Phụ trách`** | **Tái dùng `GET /api/users`** — 2026-09-09 (§3.3). Không thêm endpoint mới; quyền khớp sẵn (Q36 seed key DTI cho `Admin`, `UsersController` gate `SuperAdmin`/`Admin`). Shape ở `doc/contracts/users.md`, không chép sang đây |
> | **Khi nào băng V3 nhắc kỳ đích hiện ra** | **HAI ca**: một **tuần đã qua**, **và** chế độ **`Tất cả`** — 2026-09-09 (§5.5). Ca `Tất cả` là ca bị bỏ sót và nguy hiểm hơn: mỗi dòng hiện số của một kỳ khác nhau trong khi **mọi** lời ghi rơi vào **tuần hiện tại** (`spec/danh-muc-dti/business-rules.md` §5.3 bước 1), và vì nó **hợp lệ theo thiết kế** nên không mã lỗi nào chặn — băng là chỗ duy nhất báo được. Cùng cơ chế T15 đã tả, chỉ khác là bản trong cùng năm. Chọn đúng **tuần hiện tại** thì **không** có băng. Hệ quả: băng hiện ngay ở trạng thái mặc định (§5.1) |

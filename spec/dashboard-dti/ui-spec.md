---
kind: luat
scope: du-an
verified: chua-doi-chieu
feature: "dashboard-dti"
status: "🚧 ĐÃ CHỐT — ĐANG THI CÔNG"
updated: "2026-09-10"
---

# UI Spec — Dashboard DTI (là trang chủ)

> 🚧 **ĐÃ CHỐT — ĐANG THI CÔNG (cập nhật 2026-09-10).** Phần lớn file này vẫn mô tả
> màn hình **sẽ dựng**: trang `modules/dashboard/pages/dashboard/` hôm nay là một
> **khung** có tiêu đề và một câu "đang xây dựng", chưa có vùng nào từ V0 đến V8.
> Nhưng **bốn component của màn thì đã có thật**, nên đừng đọc cả file như đích đến:
>
> | Có thật hôm nay (đối chiếu 2026-09-10) | Sẽ thành |
> | --- | --- |
> | `src/FE/src/app/modules/dashboard/components/{kpi-tile,progress-bar,trend-chart,history-row}/` — dựng xong, có test | Trang lắp chúng vào bố cục V0–V8 |
> | `src/FE/src/app/modules/dashboard/pages/dashboard/dashboard.page.html` — khung, 1 tiêu đề + 1 câu | Màn đầy đủ theo file này |
> | `src/FE/src/app/modules/dashboard/dashboard.routes.ts` — chưa khai vào `app.routes.ts` (có chủ đích, xem chú thích trong chính file đó) | Chiếm `/trang-chu` theo Q3 |
>
> Mọi mục dưới đây mô tả **bố cục và hành vi của trang**, trừ những chỗ mang nhãn
> `✅ CÓ THẬT` kèm ngày — chỗ đó neo vào component đã dựng. Màn `/trang-chu` **đang
> chạy hôm nay** là một trang chào mừng khác hẳn, mô tả ở
> `doc/Design/Frontend/PlatformManager/Screens/06-trang-chu.md`; màn này **thay
> thế** nó (Q3), và code của nó (`src/FE/src/app/platform/trang-chu/`) **bị xoá
> hẳn** theo Q29 — xem §2. Chỗ nào trích `src/FE/src/styles.scss` hoặc
> `src/FE/src/app/app.routes.ts` là Core đang sống, đã mở file đối chiếu
> ngày 2026-09-05.

## 0. Nguồn và ranh giới của file này

| Câu hỏi | File chủ |
| --- | --- |
| Công thức tổng hợp, quy tắc kỳ, quy tắc export | `spec/dashboard-dti/business-rules.md` |
| Đường dẫn, tham số, shape JSON của endpoint | `doc/contracts/dashboard.md` |
| Layout blueprint + copy verbatim + iconography ở mức pixel | `doc/Design/Frontend/PlatformManager/Screens/01-dashboard.md` |
| Hợp đồng từng component | `doc/Design/Frontend/PlatformManager/COMPONENTS.md` → `Components/*.md` |
| Route, guard, "bến an toàn", state trên URL | `doc/huong_dan/quy-uoc/fe-routing-guard.md` |
| Màn `/trang-chu` **cũ** (bị màn này thay, code xoá theo Q29) | `doc/Design/Frontend/PlatformManager/Screens/06-trang-chu.md` — mang banner `TÀI LIỆU LỊCH SỬ` từ 2026-09-05 |
| Biểu đồ: clamp, khoảng trống dữ liệu, nội dung thay thế | `doc/huong_dan/wiki-core/fe/12-charting.md` |

Nguồn giao diện đã được người dùng duyệt từng điểm: `doc/Design/Frontend/PlatformManager/Prototypes/index.html`,
section `#screen-dashboard` (bắt đầu dòng 2177; CSS riêng của màn ở dòng
1116–1330 và 1458–1478).

Màn này **100% chỉ đọc**. Mọi thao tác ghi dữ liệu DTI nằm ở màn Danh mục
(`spec/danh-muc-dti/ui-spec.md`). Một ô sửa được xuất hiện ở đây là dấu hiệu đã
dựng nhầm màn.

## 1. Phạm vi màn

1. **Chọn kỳ** — năm + tuần/tháng, hoặc "Tất cả (tổng hợp theo năm)".
2. **Đọc 5 ô KPI** của kỳ đang xem.
3. **Đọc tiến độ theo 6 nhóm** (thanh tiến độ) và **xu hướng theo kỳ** (biểu đồ
   đường) — cả hai vẽ theo `Tiến độ %` (Q11).
4. **Đọc bảng chi tiết 9 cột** của toàn bộ chỉ tiêu, có tìm kiếm, lọc (Nhóm +
   Trạng thái) và sắp xếp (2 lựa chọn) — bộ control đã chốt ở Q22, §3.5.
5. **Đọc lịch sử các kỳ đã lưu**.
6. **Xuất báo cáo** — tải thẳng một file `.xlsx`, không dialog, không xem trước
   (Q13).

## 2. Route, quyền truy cập, state trên URL

**Màn này chiếm route `/trang-chu`** (Q3). Đó là ràng buộc nặng nhất của cả file,
vì `/trang-chu` không phải một route bình thường:

- `''` và `**` đều redirect về đó (`src/FE/src/app/app.routes.ts`).
- `roleGuard` đưa người **thiếu quyền** về đó thay vì hiện trang 403.
- Màn đổi mật khẩu quay về đó sau khi lưu thành công.

🛑 Vì vậy route này **KHÔNG được mang guard theo vai trò** — gắn vào là **vòng
lặp redirect vô hạn** cho đúng nhóm người bị đá về. Ràng buộc này đã khoá bằng
máy ở `src/FE/src/app/app.routes.spec.ts`
(`doc/huong_dan/quy-uoc/fe-routing-guard.md` §1). Guard giữ nguyên đúng hai cái
đang có: `authGuard` → `mustChangePasswordGuard`.

✅ **Q21 đã chốt: mọi người đăng nhập đều xem được Dashboard**, không cần quyền
DTI. Nên màn này **không có** trạng thái "không có quyền" — không vẽ, không dựng
nhánh template cho nó. Ai qua được `authGuard` + `mustChangePasswordGuard` thì
thấy đầy đủ số liệu.

**Q18 — chiếm đúng URL `/trang-chu`, không đổi gì quanh nó:**

| Thứ | Trạng thái |
| --- | --- |
| `''` và `**` redirect | **không đổi**, vẫn đổ về `/trang-chu` |
| `APP_CORE_ROUTES.home` (`src/FE/src/app/app.config.ts`) | **không đổi** — vẫn `/trang-chu` |
| Hàng menu seed trỏ tới route này | **không đổi** |
| Guard | **không đổi** — `authGuard` → `mustChangePasswordGuard`, không role guard |

Nghĩa là ràng buộc "ba giá trị `APP_CORE_ROUTES` phải là route có thật"
(`fe-routing-guard.md` §10, có test canh ở `src/FE/src/app/app.routes.spec.ts`)
**tự thoả** — không có đường dẫn nào đổi. Đây là điểm mạnh của Q18: thay nội dung
tại chỗ chứ không dời route.

Tầng: `modules/dashboard/` (nghiệp vụ) theo `doc/kien-truc-core-module.md`, dù
route nằm ở đường dẫn mà `platform/trang-chu/` đang giữ. **Màn chào cũ bị thay tại
chỗ.**

**Q29 — chốt 2026-09-05, người dùng XÁC NHẬN LẠI 2026-09-09: XOÁ HẲN
`src/FE/src/app/platform/trang-chu/`.** Bảng dưới không đổi một dòng nào so với bản
2026-09-05 — lượt xác nhận chỉ đóng lại câu hỏi "có giữ màn chào ở đâu đó không".
Dashboard vào thẳng `/trang-chu`, URL không đổi (Q18), `APP_CORE_ROUTES.home` giữ
nguyên.

| Thứ | Số phận |
| --- | --- |
| Thư mục `src/FE/src/app/platform/trang-chu/` | **xoá hẳn**, không giữ ở route khác |
| URL `/trang-chu` | **giữ nguyên** — Dashboard DTI chiếm chỗ (Q18) |
| `APP_CORE_ROUTES.home` | **không đổi**, vẫn `/trang-chu`; vẫn là route có thật nên test canh ở `src/FE/src/app/app.routes.spec.ts` (`fe-routing-guard.md` §10) vẫn xanh |
| `Screens/06-trang-chu.md` | **không xoá** — gắn banner `TÀI LIỆU LỊCH SỬ` theo §5 của `.claude/CLAUDE.md`, AGENT A làm trong cùng vòng |

🛑 **Hệ quả phải nói thẳng, vì nó vượt ra ngoài dự án này: dự án thứ hai dựng trên
CoreBase sẽ KHÔNG có trang chủ.** Sau Q29, màn hình duy nhất từng phục vụ vai "bến
an toàn có nội dung" của Core biến mất, và cái thay chỗ nó (`modules/dashboard/`)
là màn **nghiệp vụ DTI**, không tách ra dùng lại được. Ai tách CoreBase sang sản
phẩm khác phải **tự viết một màn `/trang-chu` mới** — nếu không, `''`, `**`,
`roleGuard` và màn đổi mật khẩu đều trỏ về một route không có component.

Đây là đánh đổi đã được chấp nhận, không phải thiếu sót: giữ song song hai màn ở
cùng một URL là thứ không tồn tại được, và giữ màn chào ở một URL khác thì tạo ra
một trang không ai vào. Ghi lại ở đây để lần tách Core sau không phát hiện muộn.

**State lên URL** (`fe-routing-guard.md` §8): `mode` (`week`/`month`/`year`),
`year`, `date` hoặc `period`, và bộ lọc của bảng chi tiết (`q`, `groupId`,
`status`). Đổi kỳ hoặc đổi lọc dùng `replaceUrl: true`. Đây là màn người dùng sẽ
muốn gửi link ("xem đúng tuần tôi đang xem"), nên bỏ state vào signal là hỏng
đúng ba thứ mà §8 liệt kê: F5 mất kỳ đang xem, không gửi được link, nút Back nhảy
khỏi trang.

**Tham số lạ trên URL — Q62 (chốt 2026-09-10).** Link cũ hoặc URL sửa tay có thể mang
giá trị không hợp lệ (vd `mode` lạ, `status` ngoài 4 giá trị, `period` không thuộc
`year`). FE **tự đưa tham số đó về mặc định và sửa lại URL** (`replaceUrl: true`),
**không** báo gì cho người dùng, và **không** gọi API với giá trị sai. `fe-routing-guard.md`
§8 chỉ chốt URL là nguồn sự thật, chưa có quy ước cho giá trị lạ — nên luật này ghi ở đây.

Các mã `DASHBOARD.MODE_INVALID` / `DASHBOARD.STATUS_INVALID` /
`DASHBOARD.PERIOD_YEAR_MISMATCH` **vẫn giữ ở BE** (`doc/contracts/dashboard.md` DB-1 § Mã
lỗi) làm lưới chặn cuối. Người dùng bình thường không gặp chúng, nên chúng **không cần câu
hiển thị**.

## 3. Layout theo vùng

Khung ngoài là app shell (`Sidebar` + `Topbar` + `main` + `Toast`,
`src/FE/src/app/app.html`). Trang **không** dùng `.page-fill` — nội dung dài hơn
màn hình và cuộn theo trang, khác màn Danh mục.

| # | Vùng | Component (có trong COMPONENTS.md) | Class / hợp đồng | Ghi chú |
| --- | --- | --- | --- | --- |
| V0 | Băng thông báo đầu trang (có điều kiện) | `NoticeBanner` — lối đi tiếp là **link chữ inline**, không phải nút (T11) | `.notice` — `styles.scss` § `.notice` | **Ba vai loại trừ nhau**, tối đa một băng hiển thị: (a) "đã có chỉ tiêu, chưa có `Tiến độ %`" — §5.3.1 (Q32); (b) "chưa có kỳ nào trong năm" — §5.3; (c) lỗi tải, biến thể `.bad` — §5.4 |
| V1 | Thanh chọn kỳ | `Toolbar` + `SegmentedControl` + `Button` | `.toolbar.no-print` — `styles.scss` § `.toolbar`; `.segmented` / `.seg-btn` — § các mục cùng tên | Xem §3.1 |
| V2 | Dải KPI | `KpiTile` × 5 | lưới `.kpis` (page-local) | Xem §3.2 |
| V3 | Card "Tiến độ theo nhóm" | `Card` + `ProgressBar` | `.card` / `.title` — `styles.scss` § các mục cùng tên | 6 nhóm, xem §3.3 |
| V4 | Card "Biểu đồ tiến độ hàng tuần" | `Card` + `TrendChart` | `.card`; `p-chart type="line"`, cao 220px | Xem §3.4 |
| V5 | Card bảng chi tiết | `Card` + `Toolbar` + `Table` | `.card`; `.toolbar.no-print`; `.tablewrap` — `styles.scss` § `.tablewrap` | Bảng thuần, **không** `DataTable` (T4) — xem §3.5 |
| V6 | Ô trạng thái trong bảng | `Badge` | `.badge.ok` / `.warn` / `.bad` / `.neutral` — `styles.scss` § `.badge` | Ánh xạ Q10 |
| V7 | Ô chênh lệch trong bảng | `DeltaIndicator` | `.delta.up` / `.down` / `.flat` — `styles.scss` § `.delta` | Spec do AGENT A khôi phục 2026-09-05 |
| V8 | Card "Lịch sử các kỳ đã lưu" | `Card` + `HistoryRow` + `DeltaIndicator` + `Button` | `.card`; `.history` / `.histrow` | Xem §3.6 |
| V9 | Dòng chân trang | `Footer` | `.footer` — `styles.scss` § `.footer` | Một câu + link sang Danh mục DTI, đích là **`/danh-muc/dti`** (Q33) |

**Class page-local, KHÔNG phải component** — liệt kê thẳng ra để không thứ nào ẩn
đi: `.layout` (lưới 2 card `1.15fr 0.85fr`, `doc/Design/Frontend/PlatformManager/Prototypes/index.html:1116`), `.kpis`
(lưới 5 cột của dải KPI, `doc/Design/Frontend/PlatformManager/Prototypes/index.html:1184`), `.period-display` (ô hiển
thị kỳ **chỉ đọc**, `doc/Design/Frontend/PlatformManager/Prototypes/index.html:1169`), `.criteria-table-card` và
`.history-card` (chỉ đặt `margin-top`). Cùng cách xử lý mà
`doc/Design/Frontend/PlatformManager/Screens/06-trang-chu.md` dành cho `.lead` và
`.facts`.

⚠️ `.period-display` là ô **chỉ đọc**, cố ý **không** dùng hợp đồng `Input` — nó
không phải ô nhập. Đừng "sửa cho đồng bộ" thành `.input`.

### 3.1 Thanh chọn kỳ — dùng NGUYÊN hợp đồng `.toolbar`

Thứ tự DOM:

```
.toolbar.no-print
├── <strong>Kỳ đang xem:</strong>
├── .period-display          → nhãn kỳ đầy đủ, ví dụ "Tuần 33/2026 (10/08 – 16/08/2026)"
├── select                   → Năm
├── select                   → Kỳ (tuần HOẶC tháng — chỉ MỘT trong hai render)
├── .segmented               → .seg-btn "Tuần" | .seg-btn "Tháng"
├── .toolbar-sep
└── .toolbar-actions         → "Xuất báo cáo" (.btn.primary + pi-file-excel)
```

- Ở đây dùng **class toàn cục `.toolbar`** đặt thẳng lên một `<div>`, **không**
  dùng component `<app-toolbar>`. Lý do là hợp đồng, không phải sở thích:
  `<app-toolbar>` chỉ có hai slot `[filter]` và `[actions]`, không có slot cho
  các control **đứng trước** ô tìm kiếm — mà thanh này toàn control loại đó.
  `Components/Toolbar.md` §Anatomy khai rõ hai lối dùng và lối này là một trong
  hai (màn `Quản trị người dùng` cũng đang dùng lối đó).
- Thanh này **không có nút "Lọc"**: chọn kỳ là hành động chính của trang, không
  phải điều kiện lọc một danh sách.
- Thanh này **không kèm `.card`** — `.toolbar` đã tự mang nền + viền; chồng hai
  mặt nền là lỗi đã sửa ở bản duyệt.
- Hai `select` kỳ là **hai nhánh loại trừ nhau** theo chế độ đang chọn, nên
  toolbar không bao giờ dôi thêm hàng.

**Mọi chỗ hiển thị kỳ đều ghi rõ từ ngày đến ngày** (Q12) — bốn chỗ trên màn này:
`.period-display`, option trong `select` kỳ, `.sub` của ô KPI "So với…", và cột
đầu của hàng lịch sử (V8). **Cả bốn chỗ nhất quán, không có ngoại lệ nào** — kể cả
cột đầu của hàng lịch sử ở chế độ Tháng (chốt 2026-09-09, §3.6).

> 📖 **Quy tắc kỳ và định dạng nhãn kỳ** (tuần ISO thứ Hai → Chủ nhật, tháng dương
> lịch, bốn khuôn chuỗi): file chủ là `spec/dashboard-dti/business-rules.md`
> §Quy tắc kỳ. Không chép khuôn ra đây. Chuỗi copy verbatim thuộc
> `doc/Design/Frontend/PlatformManager/Screens/01-dashboard.md` § Copy.

`select` kỳ có thêm hai option đặc biệt: `— Kỳ hiện tại —` (giá trị rỗng, để
server tự chọn kỳ theo hôm nay) và `— Tất cả (tổng hợp theo năm) —` (`mode=year`).
Khi đang ở chế độ "Tất cả", thanh hiện thêm một `Badge` biến thể `.warn` để không
ai nhầm số tổng hợp cả năm với số của một tuần — và đó cũng là chế độ **không xuất
báo cáo được** (§4).

### 3.2 Dải KPI — 5 ô

Lưới `.kpis` (page-local) chứa 5 `KpiTile`. Mỗi tile là một `.card.kpi` gồm ba
dòng: `.label` (nhãn, `--fs-xs`, màu `muted`), `.value` (số lớn, 21px, weight
850, có biến thể `.good` / `.warn` / `.bad`), `.sub` (chú thích, `--fs-xs`,
`min-height: 30px` để 5 tile bằng nhau kể cả khi chú thích dài ngắn khác nhau).

> 📖 **Copy của 5 ô KPI** — nhãn `.label`, giá trị mẫu, biến thể màu và chú thích
> `.sub` của từng ô: file chủ là
> `doc/Design/Frontend/PlatformManager/Components/KpiTile.md`. Không chép ra đây.
> *(Bản trước của mục này chép bảng đó và đã lệch thật — nó ghi `Tiến độ chung của
> kỳ` / `So với kỳ trước` trong khi file chủ ghi `Tiến độ chung tuần này` / `So
> với tuần trước`. Đúng cái §5 của `.claude/CLAUDE.md` sinh ra để ngăn.)*

**Nhãn `.label` của ô 1 và ô 2 đổi THEO CHẾ ĐỘ kỳ — chốt Q52 (2026-09-10).** Ba bộ
nhãn Tuần / Tháng / Năm; FE chọn theo `mode` của response DB-1. Chế độ **Năm** là khi
người dùng chọn option `— Tất cả (tổng hợp theo năm) —` trong ô kỳ ở V1 (`mode=year`,
§3.1) — người dùng xác nhận 2026-09-10; không có nút "Năm" riêng trên `.segmented`. Chuỗi của cả ba chế độ
thuộc `KpiTile.md` § Copy — không chép sang đây.

> 🔄 **LẬT 2026-09-10 (Q52).** Trước ngày này copy của hai ô chỉ có một bộ, ghim chữ
> *"tuần"* (`Tiến độ chung tuần này` / `So với tuần trước`), và câu hỏi *"ở chế độ Tháng /
> Tất cả thì hai ô ghi gì"* bị để ngỏ — màn hình ở chế độ Tháng sẽ gọi số của một tháng là
> "tuần này". Bộ chữ "tuần" nay là copy của **riêng chế độ Tuần**.

Thuộc về file này, không thuộc `KpiTile.md`: ánh xạ **ô KPI → trường dữ liệu**
(§7.2) và hành vi khi thiếu dữ liệu (§5.3 — hiện `—`, không hiện `0`).

**Caption `.sub` của ô 1 SAI đại lượng — sửa 2026-09-09.** Chuỗi verbatim vẫn thuộc
`KpiTile.md` § Copy và không chép sang đây; điều thuộc file này là **vì sao** nó phải
đổi, vì đó là câu hỏi "ô này đang hiển thị con số của cái gì":

- Caption cũ mô tả ô 1 là *bình quân gia quyền theo điểm*, kèm chú thích
  `(thật: 787,84/960)`. Hai số đó là `Σ Tự đánh giá / Σ Điểm tối đa` đo trên
  **file BA gửi tháng 8/2026** — tức **% theo ĐIỂM**.
- Ô 1 **không** hiển thị đại lượng đó. `kpi.overallProgress` là bình quân
  **`Tiến độ %`** gia quyền theo `Điểm tối đa`
  (`spec/dashboard-dti/business-rules.md` §1.1). Sau Q24 hai đại lượng **không còn
  quan hệ nào**: `Tiến độ %` do người dùng gõ tay, `Tự đánh giá` đến từ file import.
  Một caption nói "gia quyền theo điểm" trong khi số là bình quân tiến độ sẽ được
  người đọc kiểm chứng bằng máy tính cầm tay, ra số khác, và kết luận màn hình sai.
- Caption đổi thành `Bình quân Tiến độ %, gia quyền theo Điểm tối đa`. Chuỗi chuẩn
  giữ ở `KpiTile.md` § Copy — hai chỗ lệch nhau thì **file đó thắng**.
- **Bỏ chú thích prototype khỏi caption của mọi ô**: `(thật: 787,84/960)` ở ô 1 và
  `(thật)` ở ô 5. Chúng là ghi chú của bản dựng để duyệt, đánh dấu số nào đo từ CSV
  thật — không phải copy sản phẩm, và người dùng cuối không có ngữ cảnh để đọc chúng.
  Nội dung caption ô 5 (`Số chỉ tiêu ở trạng thái Hoàn thành`) **giữ nguyên**, chỉ bỏ
  phần trong ngoặc.

Hai ô đếm `Chỉ tiêu tăng` / `Không tăng` **vẫn giữ** sau Q22. Chúng chỉ **hiển
thị** một con số BE đã tính (`kpi.up` / `kpi.flat`) — chúng không phải control,
nên việc Q22 bỏ ô lọc "Mức thay đổi" không đụng tới chúng.

### 3.3 Tiến độ theo nhóm — `ProgressBar`

Card có `.title` gồm `<h2>` và một `<span class="muted">` ghi kỳ đang xem. Thân
card là danh sách, mỗi nhóm một hàng ba cột `210px 1fr 80px`: tên nhóm (in đậm) ·
thanh `ProgressBar` (`.bar` > `.fill`, cao 9px, bo `--radius-pill`, nền
`--surface-track`, phần đầy `--brand`) · phần trăm (in đậm, canh phải).

Vẽ theo **`Tiến độ %`** (Q11), không phải theo điểm thẩm định. Danh sách nhóm lấy
từ API — **không hardcode ở FE**.

**Tên nhóm hiện dạng `Code. Name`** — `1. Hạ tầng và Nền tảng số` — chốt **Q42**
(2026-09-10). Ghép từ `groupCode` + `groupName` của `groups[]`: DB-1 mang sẵn cả hai
trường, không cần trường mới. Chuỗi verbatim của sáu nhóm:
`doc/Design/Frontend/PlatformManager/Screens/01-dashboard.md` § Copy.

🛑 **Hệ quả của Q24 rơi thẳng vào vùng này, và RỖNG ≠ 0.** `Tiến độ %` để **trống**
khi import, người dùng tự nhập. Nên **ngay sau khi import xong, mọi thanh nhóm ở đây
RỖNG — không phải bằng 0** — và biểu đồ ở V4 trống, cho tới khi có người nhập
`Tiến độ %` ở màn Danh mục. Đây là hành vi **đã được người dùng chấp nhận**, không
phải lỗi, và giao diện phải nói ra thay vì để người xem tự đoán (§5.3, hàng "đã có
chỉ tiêu, chưa có tiến độ").

Hai trạng thái đó khác nhau **ở DOM**, không chỉ khác ở chữ — đây là tiêu chí
nghiệm thu, không phải lời khuyên thẩm mỹ:

| `percent` | Track `.bar` | Cột số |
| --- | --- | --- |
| **rỗng** (`null` / `undefined` / `NaN`) | **không có phần tử `.fill` nào trong DOM**; `aria-valuenow` vắng mặt, thay bằng `aria-valuetext` = "Chưa có dữ liệu" | `—` |
| `0` | **có** `.fill`, `width: 0%`; `aria-valuenow="0"` | `0,0%` |

Vì sao phân biệt này quan trọng đến mức đáng một bảng riêng: **ngay sau import là
đường đi phổ biến nhất của ngày đầu chạy thật**, không phải ca biên. Người dùng
import file lên rồi mở dashboard — sáu thanh rỗng là màn hình đầu tiên họ nhìn thấy.
Một `.fill` rộng 0 ở đó nói *"đã đo, toàn xã đạt 0%"*, trong khi sự thật là *"chưa
ai nhập"*; cùng một luật `—` ≠ `0` mà §5.3 và §5.3.1 (bảng năm ô KPI) đã áp.

✅ **CÓ THẬT (đối chiếu 2026-09-10)** — component đã dựng đúng luật này, và nó là
bên đúng nếu hai bên còn lệch: `src/FE/src/app/modules/dashboard/components/progress-bar/progress-bar.html:19-23`
(nhánh `@if (hasValue())` bọc chính `.fill`) và
`src/FE/src/app/modules/dashboard/components/progress-bar/progress-bar.ts:14-17`
(JSDoc "RỖNG khác 0"). File chủ của hợp đồng component vẫn là
`doc/Design/Frontend/PlatformManager/Components/ProgressBar.md` (chốt 2026-09-09 theo Q24).

> 🔄 **SỬA 2026-09-10.** Ba dòng ở đầu mục này trước đây viết *"ngay sau khi import
> xong, **mọi thanh nhóm ở đây bằng 0**"* — nói **ngược** `ProgressBar.md` và ngược
> chính §5.3.1 nằm cùng file (*"phần trăm `—`, không phải `0%`"*). Code đã dựng theo
> `ProgressBar.md`; câu sai nằm ở đây. Đáng ghi lại vì nó lệch đúng ở chỗ đắt nhất:
> một mâu thuẫn trong ca **thường ngày** thì người thi công gặp nó ngay lần chạy đầu,
> và nếu tin nhầm bên thì không test nào bắt — cả hai cách render đều "chạy được".

Vì vậy các số trong bản dựng để duyệt (74,3% · 51,8% …) là trạng thái **sau khi
người dùng đã nhập tiến độ**, không phải trạng thái ngay sau import.

⚠️ Thanh này mang thông tin nghiệp vụ và **màu là kênh duy nhất** nếu chỉ có thanh
— nên phần trăm bằng chữ ở cột thứ ba là **bắt buộc**, không phải trang trí. Kèm
`role="img"` + `aria-label` nêu tên nhóm và phần trăm, hoặc `aria-valuenow` theo
`doc/huong_dan/wiki-core/fe/15-accessibility.md` §4 mục 6.

### 3.4 Biểu đồ xu hướng — `TrendChart`

`p-chart type="line"`, cao `220px`, rộng `100%`, bọc trong `.chart-wrap`
(page-local, `min-height: 220px`). Nạp bằng `@defer (on viewport)` với
placeholder `Đang tải biểu đồ…` — biểu đồ là thứ nặng nhất trang và thường nằm
dưới nếp gấp.

- Trục Y ghim `[0, 100]`, nhãn hậu tố `%`. `tension: 0` (đường gấp khúc),
  `fill: true`, `pointRadius: 4`.
- **Mỗi kỳ một ô trên trục; kỳ rỗng để trống, đường ngắt đúng chỗ đó.** BE trả **đủ**
  các kỳ, kỳ không có dữ liệu mang `value: null` (Q44, 2026-09-10 —
  `doc/contracts/dashboard.md` DB-1 luật 2). FE đưa `null` thẳng vào dataset: **không**
  kẹp nó về `0`, **không** bật `spanGaps`, không nội suy
  (`doc/huong_dan/wiki-core/fe/12-charting.md`).

  > 🔄 **LẬT 2026-09-10 (Q44).** Bản trước: *"Chỉ vẽ điểm CÓ dữ liệu — không nội suy,
  > không `spanGaps`"*. Trên trục category của `chart.js`, bỏ một điểm thì hai điểm kề nhau
  > được nối thẳng — không có khoảng đứt nào; chỉ `null` giữ đúng ô của kỳ mới ngắt được
  > đường. Lý do đầy đủ: `spec/dashboard-dti/business-rules.md` §1.5.
- **Trục X, chế độ tuần**: khoảng ngày, ví dụ `06/07 – 12/07` (Q12) — chuỗi **BE dựng
  sẵn** ở `trend[].periodLabel` (Q43, 2026-09-10). FE vẽ nguyên chuỗi, **không** tự quy
  đổi mã tuần ra ngày.
- **Trục X, chế độ tháng: `Th.1 … Th.12` — chốt T7 (2026-09-05), không còn là câu
  hỏi mở.** Không phải khoảng ngày, vì 12 nhãn khoảng-ngày không đủ chỗ trên trục.
  Sự bất đối xứng giữa hai chế độ là **có chủ ý và đã được chấp nhận**: chế độ tuần
  hiện tối đa vài nhãn nên khoảng ngày vừa chỗ, chế độ tháng thì không. Khoảng ngày
  của tháng đang xem đọc ở V1 (`.period-display`) và ở nhãn kỳ — thông tin không
  mất đi, nó chỉ không nằm trên trục. Đừng "sửa cho đồng bộ" thành khoảng ngày;
  file chủ của quyết định này là `Components/TrendChart.md`.

  > 🔄 **LẬT 2026-09-10 (Q54 + Q57).** Vế *"chế độ tuần hiện tối đa vài nhãn nên khoảng
  > ngày vừa chỗ"* không còn là tiền đề. Chế độ Tuần nay vẽ **cửa sổ 12 tuần kết thúc ở
  > tuần đang xem** (Q54 — người dùng chọn 12 thay cho 6 tuần W28–W33 của bản duyệt), **cắt
  > ở đầu năm** đang lọc: xem tuần 3 thì trục chỉ có tuần 1…3 (Q57). Phạm vi chính xác:
  > `doc/contracts/dashboard.md` DB-1 luật 2. Dấu ⚠️ *"mâu thuẫn chưa giải"* đặt ở đây sáng
  > cùng ngày được đóng bằng hai mã này.
  >
  > **Việc còn mở:** cho **12 nhãn khoảng ngày** vừa trục — chính lý do T7 dùng cho chế độ
  > tháng (*12 nhãn khoảng-ngày không đủ chỗ*) nay áp vào chế độ tuần. Đề xuất Design đang
  > soạn, **chờ duyệt**: `doc/Design/Frontend/PlatformManager/Components/TrendChart.md`.
  > File này không chọn.
- Biểu đồ **phải có nội dung thay thế** cho trình đọc màn hình (`role="img"` +
  `aria-label` mô tả kỳ đầu, kỳ cuối và xu hướng) — a11y §4 mục 6. Bảng chi tiết
  ở V5 không thay thế được, vì nó là số của **một** kỳ chứ không phải chuỗi thời
  gian.

### 3.5 Bảng chi tiết — 9 cột, chỉ đọc

Card có `.title` = `<h2>` + `<span class="muted">` dạng `{đang hiện}/{tổng} chỉ
tiêu`. Bên dưới là **`.toolbar`** dùng nguyên hợp đồng, rồi bảng.

Toolbar ở đây **có** ô tìm kiếm và **có** nút Lọc, nên dùng component
`<app-toolbar>`:

```
.toolbar.no-print
├── .input-icon.search        → ô tìm mã hoặc tên chỉ tiêu
├── details.filter
│   ├── summary.btn           → "Lọc" + .filter-count (khi > 0)
│   └── .filter-panel
│       ├── .form-row × 2     → Nhóm chỉ tiêu · Trạng thái
│       └── .filter-foot      → "Xoá lọc" | "Áp dụng"
├── .toolbar-sep
├── .filter-chips             → hiện khi có điều kiện đang áp
└── .toolbar-actions          → <select> sắp xếp
```

⚠️ Thứ tự trên là của **component đang chạy** — `.toolbar-sep` **trước**
`.filter-chips` (`src/FE/src/app/shared/components/toolbar/toolbar.html`, đối chiếu
2026-09-05; `Toolbar.md` §Anatomy). Bản dựng để duyệt đặt ngược; bản dựng sai.
Nút gỡ chip cũng theo component: `.icon-btn` mang
`aria-label = "Bỏ lọc " + nhãn chip`, **không** có `title`, ký hiệu là icon
`<i class="pi pi-times">` — không phải ký tự `×`.

**Bộ lọc còn đúng 2 điều kiện: Nhóm chỉ tiêu và Trạng thái** (Q22). Ô lọc `Mức
thay đổi so với kỳ trước` **đã bỏ**.

Option của ô `Nhóm chỉ tiêu` hiện dạng **`Code. Name`** (Q42, 2026-09-10) — cùng khuôn
với thanh nhóm ở §3.3, ghép từ `groupCode` + `groupName` (DB-1 mang cả hai ở `groups[]`
lẫn `table[]`).

**Ô sắp xếp còn đúng 2 lựa chọn** (Q22): `Theo mã chỉ tiêu` (mặc định) và
`Chênh lệch lớn nhất`. Hai lựa chọn `Tăng nhiều nhất` và `Tiến độ thấp nhất` **đã
bỏ**. Ô sắp xếp không phải điều kiện lọc nên nó ở lại trên hàng, phía
`.toolbar-actions`, không nhét vào `.filter-panel`.

⚠️ **`Chênh lệch lớn nhất` xếp theo `|diff|`, không theo giá trị có dấu** — và Q25
làm chỗ này dễ hỏng hơn trước. Sau khi công thức đổi thành `Thẩm định − Tự đánh
giá` (§7.3), giá trị âm là **ca đáng chú ý nhất** (bị bác điểm), nên xếp giảm dần
theo giá trị có dấu sẽ đẩy đúng những dòng cần xem xuống cuối. Luật so sánh và
epsilon thuộc `spec/dashboard-dti/business-rules.md`; ở đây chỉ ghi rằng nhãn
"lớn nhất" nói về **độ lệch**, không nói về dấu.

**Đếm điều kiện lọc — chốt T8 (2026-09-05): chỉ đếm điều kiện KHÁC MẶC ĐỊNH.**
`.filter-count` và `.filter-chips` cùng theo một luật, nên số chip luôn bằng số
trên nút. Ở toolbar này chỉ có hai ô và cả hai đều mặc định rỗng, nên luật rút gọn
thành: đã chọn Nhóm thì +1, đã chọn Trạng thái thì +1, chưa chọn gì thì
`.filter-count` **không render**. Ô sắp xếp **không** tính — nó không phải điều
kiện lọc, và một chip "sắp xếp theo mã" mà gỡ đi không đổi tập dòng nào sẽ dạy
người dùng sai về ý nghĩa của chip. Luật đầy đủ (gồm ca ô có mặc định khác rỗng như
`Năm`/`Kỳ`) ở `spec/danh-muc-dti/ui-spec.md` §3.1 — toolbar bên đó có bốn ô nên nó
là chỗ luật này được viết ra đủ.

> Cả ba thứ bị bỏ đều dựa trên phép so sánh với kỳ trước, mà `table[]` của DB-1
> không mang ra. Bỏ chúng là cách giữ cho mọi control trên màn đều **chạy được**
> — thay vì để lại ba ô bấm vào không có gì xảy ra. Hai ô KPI `Chỉ tiêu tăng` /
> `Không tăng` giữ nguyên: chúng hiển thị `kpi.up` / `kpi.flat`, không phải
> control.

Trạng thái **chưa lọc**: không có `.filter-count`, không có `.filter-chip` nào,
và caption đọc `{tổng}/{tổng} chỉ tiêu`.

🛑 **Lọc bảng KHÔNG được đổi KPI, thanh nhóm hay biểu đồ.** CONTRACT DB-1 chốt
`search`/`groupId`/`status` **chỉ áp cho `table`**; `kpi`, `groups`, `trend` luôn
tính trên **toàn bộ** chỉ tiêu của kỳ. Lý do thuộc về người đọc chứ không phải
phép tính: nếu lọc một nhóm mà ô "Tiến độ chung" (ô KPI 1 — nhãn theo chế độ kỳ, Q52) tụt theo, người xem sẽ
tưởng tiến độ toàn xã thay đổi — một con số đúng phép tính nhưng trả lời sai câu
hỏi đang được hỏi. Vì vậy caption `{đang hiện}/{tổng} chỉ tiêu` ở `.title` của V5
là **bắt buộc**: nó là chỗ duy nhất trên màn nói rằng bảng đang hẹp hơn phần còn
lại của trang.

> 📖 **Bảng cột** — số cột, thứ tự, header verbatim, bề rộng từng cột: file chủ là
> `doc/Design/Frontend/PlatformManager/Screens/01-dashboard.md` § Layout
> Blueprint. Không chép bảng ra đây.

Ba điều thuộc về file này chứ không thuộc bảng cột:

- **Dùng `Table`, KHÔNG dùng `DataTable`** (T4). Bảng thuần trong `.tablewrap`,
  cuộn dọc trong vùng cao cố định, **không paginator**, không `[lazy]`. `DB-1` trả
  trọn bộ chỉ tiêu của kỳ trong một lần nên không có gì để phân trang phía server,
  và cơ chế của `p-table` không đóng góp gì ở đây. Chọn `Table` để **không sinh ra
  biến thể thứ hai** của `DataTable` — `DataTable.md` khai đúng một biến thể
  (`[lazy]` + luôn có paginator), và một lưới đọc-only không phải lý do đủ để nới
  hợp đồng đó.
- `.num` (`styles.scss` § `.num`) cho bốn cột số — canh phải + `tabular-nums`.
- Xu hướng theo kỳ **không lặp lại trong bảng** — nó đã có ở V4 và ở hai ô KPI.

### 3.6 Lịch sử các kỳ đã lưu — `HistoryRow`

Card có `.title` = `<h2>` + `<span class="muted">`. Thân card là danh sách cuộn,
mỗi kỳ một `HistoryRow` — bốn cột:

1. Khoảng ngày của kỳ, in đậm — **một khuôn duy nhất cho cả chế độ Tuần lẫn Tháng**,
   xem bảng ngay dưới.
2. Tiến độ chung của kỳ.
3. `DeltaIndicator` so với kỳ liền trước; kỳ đầu tiên hiện `<span class="muted">Kỳ
   đầu</span>` thay vì một delta bịa bằng 0.
4. `Button` `Xem` — nạp lại toàn trang theo kỳ đó (đổi query param, không mở
   dialog).

**Ô 1 — MỘT khuôn cho cả hai chế độ, chốt 2026-09-09:**

| Chế độ | Ô 1 của mỗi hàng | Ví dụ |
| --- | --- | --- |
| **Tuần** | khoảng ngày | `10/08 – 16/08/2026` |
| **Tháng** | khoảng ngày, **cùng khuôn** | `01/08 – 31/08/2026` |

Cả hai là khuôn `{dd/MM} – {dd/MM/yyyy}` — chính hàng *"Dòng lịch sử kỳ"* đã có sẵn ở
`spec/dashboard-dti/business-rules.md` §6.2. **Không có ngoại lệ nào để đăng ký**, và
đây cũng không phải một khuôn mới.

🛑 **KHÔNG rút gọn chế độ Tháng thành `Tháng 8/2026`.** Phương án rút gọn từng được
chốt sáng 2026-09-09 rồi **hoàn lại cùng ngày** sau khi đo thật, nên ghi lại để không
ai đề xuất lại:

| | Chuỗi | Độ dài |
| --- | --- | --- |
| Tuần (khuôn đang dùng) | `10/08 – 16/08/2026` | **18 ký tự** |
| Tháng, khoảng ngày đầy đủ | `01/08 – 31/08/2026` | **18 ký tự** |

Hai chuỗi **dài bằng nhau**, mà ô 1 rộng 150px (T3, `Components/HistoryRow.md`
§ Anatomy) — nên tiền đề *"chế độ tháng dài hơn, không đủ chỗ"* của phương án rút gọn
đơn giản là **sai**. Tiền lệ T7 cũng không áp được: trục X có **12 nhãn chen trên một
trục** nên ở đó mới thật sự hết chỗ, còn vùng Lịch sử mỗi kỳ **một hàng riêng**, chiều
ngang không cạnh tranh với ai.

Và luật cấm thì đã có sẵn: `spec/dashboard-dti/business-rules.md` §6.2 chốt **khoảng
ngày là một trong hai thành phần KHÔNG BAO GIỜ được lược** — đúng thứ mà `Tháng 8/2026`
lược đi.

> 📖 Bề rộng cột và lưới của `.histrow`: file chủ là
> `doc/Design/Frontend/PlatformManager/Components/HistoryRow.md` § Anatomy (T3).
> Cột đầu phải nới so với lưới gốc vì nay nó chứa một **khoảng ngày** chứ không phải
> một ngày — giá trị chốt nằm ở file đó, không chép ra đây. Chuỗi của chế độ Tháng dài
> **bằng đúng** chuỗi của chế độ Tuần, nên nó **không** làm phát sinh yêu cầu bề rộng
> mới.

Danh sách này **không có endpoint riêng** — tái dùng danh sách kỳ của
`GET /api/dashboard/periods` và tự tính delta giữa hai kỳ liền kề ở FE
(`doc/contracts/dashboard.md` CONTRACT DB-3).

## 4. Hành động

| Hành động | Điểm vào | Kết quả |
| --- | --- | --- |
| Đổi năm | `select` Năm ở V1 | nạp lại toàn trang theo năm mới; danh sách kỳ nạp lại theo năm |
| Đổi kỳ | `select` Kỳ ở V1 | nạp lại toàn trang |
| Đổi chế độ Tuần ↔ Tháng | `.segmented` ở V1 | đổi `mode`, thay `select` kỳ, đổi nhãn trục X của V4 |
| Xem "Tất cả trong năm" | option `— Tất cả —` | `mode=year`; hiện `Badge` `.warn` ở V1 |
| **Xuất báo cáo** | `.btn.primary` ở V1 | **tải thẳng file `.xlsx`** của kỳ đang xem |
| Tìm kiếm trong bảng | `.input-icon.search` ở V5 | lọc bảng chi tiết |
| Lọc bảng | `details.filter` → `Áp dụng` | lọc bảng chi tiết |
| Sắp xếp bảng | `select` ở `.toolbar-actions` | đổi thứ tự bảng chi tiết — **hai** lựa chọn, cả hai xếp được **ở FE** trên `table[]` đã tải, xem ghi chú dưới |
| Xem một kỳ trong lịch sử | `Xem` ở V8 | nạp lại toàn trang theo kỳ đó |
| Sang màn Danh mục | link ở V9 | điều hướng |

**Sắp xếp là việc của FE, không phải tham số API.** Sau Q22 cả hai lựa chọn còn
lại (`Theo mã chỉ tiêu`, `Chênh lệch lớn nhất`) đều xếp theo trường **đã có** trong
`table[]` — `code` và `diff`. `DB-1` trả trọn bộ chỉ tiêu của kỳ trong một lần và
bảng **không phân trang** (T4), nên xếp ở FE cho ra đúng kết quả xếp ở BE mà không
tốn một vòng gọi. Vì vậy DB-1 **không** cần tham số `sort`, và đừng thêm. Đây là
mục §9 đã đóng, không phải khoảng trống còn lại.

**Xuất báo cáo — hành vi bắt buộc (Q13):**

- Bấm là **tải file ngay**. Không dialog, không xem trước, không nút Sao chép,
  không nút In. Component `report-dialog` của thiết kế cũ **đã gỡ**, và khối CSS
  `app-report-dialog` trong bản duyệt là style chết.
- Gọi `GET /api/dashboard/export` với đúng bộ tham số của DB-1 — kể cả bộ lọc
  đang áp ở V5 (`search`, `groupId`, `status`).
- **Chỉ `.xlsx`** — không CSV, không `.xls` (nhận vào có `.xls`, xuất ra thì
  không, theo `doc/huong_dan/wiki-core/be/15-import-export.md` §3).
- Chế độ **Tháng thì xuất TRỌN tháng**, không phải một tuần trong tháng (Q15).
  ⚠️ Q37 (chỉ **nhập** theo tuần) **không đụng tới điều này**: `mode=month` vẫn chạy
  bình thường vì export là **đọc tổng hợp**, không phải ghi. Ràng buộc "chỉ tuần"
  của Q37 chỉ áp cho ô `Kỳ trong năm` ở màn Danh mục
  (`spec/danh-muc-dti/ui-spec.md` §3.1) — đừng mang nó sang đây và vô hiệu hoá nút
  xuất ở chế độ tháng.
- Trong lúc chờ: nút `disabled` + chỉ báo tiến trình, và trạng thái đó phải nằm
  trong vùng `aria-live` — một nút bấm xong không đổi gì là lỗi giao diện kinh
  điển ở đây.
- Tên file và bố cục sheet do BE quyết (Q14) — không dựng lại ở FE. FE đọc tên
  file từ `Content-Disposition`, không tự ghép.

🛑 **Chế độ "Tất cả (tổng hợp theo năm)" KHÔNG xuất được.** CONTRACT DB-4 không hỗ
trợ `mode=year` và trả `400` + `DASHBOARD.EXPORT_MODE_UNSUPPORTED` — vì bố cục
file đã duyệt có khối nhận dạng kỳ với `Từ ngày`/`Đến ngày` của **một** kỳ (Q14),
mà "cả năm" không ánh xạ vào khuôn đó được. Giao diện phải nói điều này **trước**
khi người dùng bấm: khi `mode=year`, nút `Xuất báo cáo` ở trạng thái `disabled`
kèm `title` giải thích. Để nút bấm được rồi hiện toast lỗi là bắt người dùng phát
hiện một giới hạn đã biết trước bằng cách vấp phải nó.

⚠️ **Đây là endpoint duy nhất của hệ thống không trả envelope.** Thành công thì
thân response là **bytes của file**; **lỗi thì vẫn là `IApiResult` JSON** như mọi
endpoint khác. Nên FE **phải kiểm `Content-Type` trước khi coi body là file** —
nhận JSON nghĩa là lỗi, và phải đọc envelope ra để hiện đúng thông điệp. Bỏ bước
kiểm này thì người dùng nhận về một file `.xlsx` hỏng chứa nguyên văn thông báo
lỗi, và triệu chứng lúc đó không trỏ về đâu cả. Lỗi tải → `Toast`, **không** điều
hướng đi đâu.

## 5. Trạng thái

### 5.1 Mặc định
Không có tham số trên URL → `mode=week`, năm hiện tại, **kỳ hiện tại do server
chọn** (bỏ trống `date` và để BE dùng hôm nay — `doc/contracts/dashboard.md`
DB-1). Bảng chi tiết chưa lọc. Toàn bộ 5 ô KPI, 6 nhóm, biểu đồ, bảng và lịch sử
đều có dữ liệu.

### 5.2 Đang tải — làm mờ vùng số liệu + vòng quay nhỏ (Q34)

🛑 **Chốt 2026-09-05. Trước Q34 màn này KHÔNG có chỉ báo đang tải nào cả** — biến
`loading` tồn tại nhưng template không đọc, nên đổi kỳ thì số của **kỳ cũ đứng
nguyên** trên màn cho tới khi phản hồi về. Đó là lỗi đã ghi nhận, không phải lựa
chọn thiết kế: người dùng bấm sang tuần khác, thấy y nguyên bộ số cũ, và không có
gì cho biết đó là số của tuần nào. Bản này sửa nó.

Trang gọi **hai** nguồn: `GET /api/dashboard` (V2–V5) và `GET
/api/dashboard/periods` (select kỳ ở V1 + danh sách V8). Chúng về không cùng lúc,
nên trạng thái tải là **theo vùng**, không phải một màn trắng.

**Hành vi bắt buộc khi `GET /api/dashboard` đang chạy:**

| Vùng | Đang tải |
| --- | --- |
| V2 dải KPI | **mờ đi** |
| V3 tiến độ theo nhóm | **mờ đi** |
| V4 biểu đồ | **mờ đi** (khác với placeholder `@defer` — xem dưới) |
| V5 bảng chi tiết | mặt nạ loading sẵn có của `p-table` (`[loading]`) |
| V1 thanh chọn kỳ | **không mờ** — người dùng phải đổi kỳ tiếp được ngay |
| V8 lịch sử | **không mờ** — nó ăn theo nguồn thứ hai, xem dưới |

Kèm theo phần mờ là **một vòng quay nhỏ**, đặt ở giữa vùng đang tải. **Dùng
`p-progressSpinner` của PrimeNG** — chốt T10 (2026-09-06). Một vòng quay cho cả cụm
V2–V4, không phải mỗi vùng một cái: bốn vòng quay quay cùng lúc trên một trang
trông như bốn thứ hỏng riêng lẻ.

Ba lý do chọn `p-progressSpinner` thay vì tự vẽ, và cả ba đều là lý do **không phát
sinh việc**:

- **PrimeNG đã là dependency**, nên không thêm gói, không thêm bundle.
- **Nó ăn màu theo preset `createCorePreset(APP_PALETTE)`** đang chạy, nên **không
  cần khai token mới** trong `doc/Design/.../Tokens/`. Tự vẽ thì phải chọn màu, và
  chọn màu nghĩa là một token mới cần người duyệt.
- **App hiện không có animation quay nào** để dùng lại — kiểm:
  `grep -c '@keyframes' src/FE/src/styles.scss` → `0` (đối chiếu 2026-09-06). Tự vẽ
  nghĩa là viết `@keyframes` đầu tiên của repo, cho đúng một chỗ dùng.

🛑 **KHÔNG thêm hàng mới vào `COMPONENTS.md` cho vòng quay này.** Nó là **chrome do
PrimeNG vẽ**, cùng loại với paginator và mặt nạ loading của `p-table` — cả hai thứ
đó cũng không có hàng riêng. `COMPONENTS.md` liệt kê thứ **app** định nghĩa; thứ
thư viện vẽ thì thuộc hợp đồng của component chứa nó.

Năm ràng buộc, đều là chỗ dễ làm hỏng đúng thứ Q34 sinh ra để sửa:

1. **Mờ, KHÔNG ẩn.** Giữ số cũ nhìn thấy được ở dạng mờ để layout không sụp và mắt
   người dùng không mất chỗ neo. Ẩn đi rồi hiện lại làm trang giật hai lần mỗi lần
   đổi kỳ.
2. **Mờ phải kèm `aria-busy="true"` trên vùng đang tải** và vòng quay phải có nhãn
   text cho trình đọc màn hình. Chỉ làm mờ là tín hiệu **thuần thị giác** — người
   dùng trình đọc màn hình sẽ đọc trúng số cũ và tin đó là số mới
   (`doc/huong_dan/wiki-core/fe/15-accessibility.md` §4).
3. **Số mờ KHÔNG được copy/đọc ra như số thật.** Nếu một vùng mờ vẫn cho bôi đen
   và sao chép, đặt `inert` hoặc `pointer-events: none` cho khoảng thời gian đó.
4. **V4 có HAI trạng thái chờ khác nhau, đừng gộp.** `.chart-skeleton` của `@defer`
   là *"mã biểu đồ chưa tải xong"* (chuyện của code); lớp mờ + vòng quay là *"số
   liệu chưa về"* (chuyện của dữ liệu). Lần đầu vào trang có thể gặp cả hai nối
   tiếp nhau; đừng cho cái sau thay chỗ cái trước.
5. **V8 và `select` kỳ ở V1 theo nguồn thứ hai** (`/periods`): `select` kỳ
   `disabled` cho tới khi có danh sách; V8 giữ khung hàng. Chúng **không** mờ theo
   nhịp của `/api/dashboard`, vì hai nguồn về không cùng lúc và làm mờ cả trang
   theo nguồn chậm hơn là kéo dài trạng thái chờ một cách vô cớ.

**Còn lại đúng một thứ phải tự viết: lớp làm mờ.** `src/FE/src/styles.scss` không
có class nào cho nó — kiểm bằng ba lệnh, PASS = cả ba không hit (đối chiếu
2026-09-06): `grep -n "spinner" src/FE/src/styles.scss`,
`grep -n "loading" src/FE/src/styles.scss`, `grep -c '@keyframes' src/FE/src/styles.scss`
(ra `0`). Đừng dùng từ khoá `overlay` để kiểm — `--overlay-backdrop` **có tồn tại**
(`src/FE/src/styles.scss:108`) nhưng nó là scrim sau dialog và sau drawer mobile,
không liên quan gì tới trạng thái đang tải.

Lớp mờ đó là **markup page-local của riêng màn này** — cùng cách xử lý đang dành cho
`.kpis`, `.layout`, `.period-display` (§3). Nó là `opacity` + `pointer-events` trên
một vùng có sẵn, không phải một thành phần giao diện mới, nên **không** có hàng
trong `COMPONENTS.md` và **không** cần một quyết định thư viện. Chỉ promote lên khi
có màn thứ hai cần đúng thứ đó.

📌 **Màn Danh mục KHÔNG áp Q34.** Lưới ở đó nằm trong `p-table`, vốn đã có lớp phủ
sẵn và Core đang dùng qua `[loading]` — dựng thêm lớp mờ thứ hai là hai cơ chế
loading chồng nhau. Xem `spec/danh-muc-dti/ui-spec.md` §5.2.

### 5.3 Rỗng
**Năm** ca rỗng khác nhau, và gộp lại là mất thông tin:

**"Chưa có dữ liệu" KHÔNG phải lỗi.** CONTRACT DB-1 §0 chốt: BE trả `IApiResult`
**thành công** với `groups`/`table` là **mảng rỗng** và `trend` đủ các kỳ nhưng không
kỳ nào mang `value` (Q44, 2026-09-10 — trước đó ghi `trend` rỗng), không trả `404`.
Nên FE không được đẩy ca này vào nhánh xử lý lỗi, và trạng thái rỗng của biểu đồ nhận
biết bằng *"không kỳ nào có giá trị"*, **không** bằng *"mảng rỗng"*.

| Ca | Hiển thị |
| --- | --- |
| Chưa có kỳ nào trong năm đang chọn | `NoticeBanner` ở **V0**, nêu rõ năm nào chưa có dữ liệu và trỏ sang màn Danh mục để nhập/import; V2–V5 và V8 ẩn |
| **Đã có chỉ tiêu, chưa có `Tiến độ %`** | `NoticeBanner` + nút sang `/danh-muc/dti` — xem §5.3.1 (Q32) |
| Kỳ đang chọn không có dữ liệu (năm thì có) | KPI hiện `—` thay vì `0` — không có dữ liệu ≠ tiến độ bằng 0; V5 hiện thông điệp rỗng của bảng |
| Biểu đồ chỉ có **1 kỳ mang giá trị** (các kỳ khác `null`) | vẽ đúng 1 điểm, **không** vẽ đường; không nội suy để "cho đẹp" |
| Lịch sử chỉ có kỳ đầu tiên | hàng duy nhất hiện `Kỳ đầu` ở cột delta |

Phân biệt `—` và `0` là yêu cầu **nghiệp vụ**, không phải thẩm mỹ: cả hai đều là
một ô số nhìn giống nhau, và đọc nhầm sẽ ra kết luận ngược.

#### 5.3.1 Ngay sau import — "đã có chỉ tiêu, chưa có `Tiến độ %`" (Q32)

**Đây là trạng thái BÌNH THƯỜNG, và nó là màn hình người dùng thấy sau MỖI lần
import.** Chốt Q32, 2026-09-05.

Nguyên nhân là một dây chuyền hoàn toàn xác định, không phải ca hiếm:

1. Q24 — import **để trống** `Tiến độ %`, người dùng tự nhập.
2. Q11 — thanh tiến độ theo nhóm (V3) và biểu đồ (V4) vẽ **theo `Tiến độ %`**.
3. ⇒ Ngay sau import, V3 và V4 **không có gì để vẽ**, dù dữ liệu vừa vào đủ 62 chỉ
   tiêu và bảng chi tiết V5 đầy số.

Điều kiện nhận biết, và phải phân biệt được với hai ca rỗng khác:

| | Số chỉ tiêu của kỳ | Số chỉ tiêu có `Tiến độ %` | Đây là ca nào |
| --- | --- | --- | --- |
| 0 | — | | "chưa có kỳ nào / chưa có dữ liệu" |
| > 0 | **0** | | **ca này (Q32)** |
| > 0 | > 0 | | trạng thái mặc định §5.1 |

**Hiển thị:**

- **`NoticeBanner`** ở **V0**, biến thể **mặc định** (thông tin) — không
  phải `.warn`, không phải `.bad`. Đây không phải lỗi và không phải cảnh báo; nói
  ngược lại là dạy người dùng rằng import vừa rồi hỏng.
- Nội dung băng phải nói đủ **bốn** ý, theo đúng thứ tự người dùng cần: (a) đã có
  bao nhiêu chỉ tiêu; (b) chưa chỉ tiêu nào có `Tiến độ %`; (c) biểu đồ và thanh
  nhóm sẽ hiện khi có số liệu; (d) một **liên kết** sang `/danh-muc/dti`. Chuỗi
  verbatim thuộc `Screens/01-dashboard.md` § States (AGENT A) — không chép sang đây.
- **LIÊN KẾT CHỮ inline, KHÔNG phải nút** — chốt T11 (2026-09-06). Hợp đồng
  `NoticeBanner` khai sẵn `a` inline (`NoticeBanner.md` § Anatomy: ink theo mức độ
  nghiêm trọng, weight 700, gạch chân khi hover) nhưng **không có slot cho nút có
  nhãn**. Nhét một `Button` vào thân băng là **mở rộng hợp đồng** — đi ngược đúng
  yêu cầu "dùng lại component có sẵn", và để có thêm cái gì? Một affordance mà link
  inline đã làm được. *(Bản trước của mục này chốt ngược lại và lập luận rằng "việc
  duy nhất làm được thì xứng đáng một nút". Lập luận đó bỏ qua cái giá: một hàng mới
  trong hợp đồng `NoticeBanner` cho mọi màn về sau.)*
- **V3 và V4 vẫn render, ở trạng thái rỗng của chính chúng** — không ẩn. Thanh nhóm
  hiện tên 6 nhóm với phần trăm `—` (không phải `0%`, cùng lý do phân biệt `—`/`0`
  ở trên); biểu đồ hiện nội dung thay thế của `TrendChart`. Ẩn hai vùng này đi thì
  người dùng không biết chúng tồn tại, và không hiểu băng đang hứa hẹn cái gì.
- **V5 (bảng) và V8 (lịch sử) vẫn có số bình thường** — chúng không phụ thuộc
  `Tiến độ %`. Đây là điểm quan trọng nhất của trạng thái này: trang **không trống**,
  nó chỉ thiếu đúng hai vùng, và băng phải nói ra là thiếu vùng nào.
- **V2 (dải KPI) hiện MỘT PHẦN — chốt T12 (2026-09-06), năm ô không giống nhau:**

  | Ô | Ngay sau import | Vì sao |
  | --- | --- | --- |
  | 1 — `Tiến độ chung` | **`—`** | suy từ `Tiến độ %`, mà Q24 để trống |
  | 2 — `So với kỳ trước` | **`—`** | cũng suy từ `Tiến độ %`; không có kỳ trước để so |
  | 3 — `Chỉ tiêu tăng` | **`0`** | là phép **đếm**, và đếm được: không chỉ tiêu nào tăng |
  | 4 — `Không tăng` | **`0`** | cùng lý do ô 3 |
  | 5 — `Hoàn thành` | **số thật** | đếm theo **trạng thái**, mà trạng thái có sẵn trong file import — trên dữ liệu thật là `26/62` |

  🛑 **`—` và `0` ở đây KHÁC nghĩa nhau, đừng đồng bộ cho gọn.** `—` là *"không tính
  được"*; `0` là *"tính được, kết quả bằng không"*. Ô 1 hiện `0%` sẽ nói dối rằng
  toàn xã đạt 0% tiến độ, trong khi sự thật là chưa ai nhập. Ô 3 hiện `—` thì ngược
  lại — giấu đi một con số hoàn toàn xác định. Đây là cùng luật `—` ≠ `0` ở §5.3,
  chỉ khác là ở đây nó **không** áp đồng loạt cho cả dải.

  File chủ của luật này (ô nào tính được từ dữ liệu import, ô nào không) là
  `spec/dashboard-dti/business-rules.md` — bảng trên chỉ là mặt hiển thị.
- `Xuất báo cáo` (V1) **vẫn dùng được** — file `.xlsx` chứa 12 cột trong đó
  `Tiến độ %` để trống, và đó là dữ liệu hợp lệ.

⚠️ Các số trong bản dựng để duyệt (74,3% · 51,8% …) là trạng thái **sau khi người
dùng đã nhập tiến độ**. Đừng đọc chúng như thứ xuất hiện ngay sau import.

### 5.4 Lỗi
- `GET /api/dashboard` lỗi → `NoticeBanner` biến thể `.bad` ở **V0** **và**
  `Toast` từ `httpErrorInterceptor`. Không để trang trắng: đây là trang đích của
  mọi redirect, một trang trắng ở đây trông như app hỏng.
- `GET /api/dashboard/periods` lỗi → chỉ V1 (`select` kỳ) và V8 báo lỗi cục bộ;
  phần còn lại vẫn dùng được với kỳ mặc định.
- Lỗi xuất báo cáo → `Toast`, không đổi gì khác trên màn.

### 5.5 Chỉ đọc
Đây là **trạng thái duy nhất** của dữ liệu trên màn này. Không có ô sửa, không có
form, không có validation. Nếu một màn hình dashboard sinh ra ô nhập, nó đã bị
dựng nhầm.

### 5.6 Ai đến được màn này — ràng buộc bắt buộc của Q3

*(Mục này từng có tên "Không có quyền". Sau Q21 màn này **không có** trạng thái đó
nữa, nên tên cũ mô tả một thứ không tồn tại. Nội dung giữ lại vì ràng buộc routing
thì không đổi.)*

Vì `/trang-chu` **không có role guard** (§2), ba nhóm người sau đều **đến được**
màn này và mỗi nhóm phải thấy một thứ có nghĩa:

| Nhóm | Đến bằng đường nào | Hiển thị |
| --- | --- | --- |
| Chưa đăng nhập | không đến được | `authGuard` → `/dang-nhap?returnUrl=/trang-chu` |
| Đăng nhập, còn cờ đổi mật khẩu | không đến được | `mustChangePasswordGuard` → `/doi-mat-khau` |
| Đăng nhập, **bất kể quyền gì** | vào thẳng, **hoặc** bị `roleGuard` của màn khác đá về đây | **Dashboard đầy đủ.** Chốt Q21 (2026-09-05): chỉ cần đăng nhập là xem được, không cần quyền DTI |

Vì Q21 chốt "ai đăng nhập cũng xem được", màn này **không có** trạng thái
"không có quyền" — không vẽ, không dựng. Ba điều dưới đây vẫn phải giữ, vì chúng
là hệ quả của luật routing chứ không phải lựa chọn thiết kế:

1. **KHÔNG được chuyển hướng đi đâu.** Đây là bến an toàn; chuyển hướng từ đây là
   vòng lặp.
2. **KHÔNG được để trang trắng hay trang lỗi.** Người bị `roleGuard` đá về đây
   vừa bị từ chối một màn khác; gặp tiếp một trang trắng thì họ kết luận app hỏng.
3. **Phải có một thông điệp giải thích và một lối đi tiếp** — tối thiểu là
   `NoticeBanner` + app shell còn nguyên để dùng sidebar (menu do server cấp theo
   quyền, nên nó vẫn đúng cho từng người).

> **Hai phương án A/B của bản trước đã hết hiệu lực.** Bản trước liệt kê "A — ai
> đăng nhập cũng xem được" và "B — phải có quyền DTI", để ngỏ cho người dùng chọn.
> Q21 (2026-09-05) đã chọn **A**, nên mục này không còn là câu hỏi. Giữ lại đúng
> một dòng ghi nhận để người đọc bản trước không đi tìm chỗ chốt: CONTRACT DB-1
> khai `[Authorize]` fail-closed, **không** `[RequirePermission]`, và đó là bản
> khớp với quyết định.

**Đừng nhầm với quyền GHI.** Q27 chốt **một** permission-key DTI cho quyền ghi, và
key đó áp cho màn **Danh mục** (`spec/danh-muc-dti/ui-spec.md` §5.6). Màn này 100%
chỉ đọc nên nó không dùng key đó ở bất kỳ đâu — không guard theo key, không ẩn
vùng nào theo key. Hai câu hỏi khác nhau, hai đáp án khác nhau, và trộn chúng lại
là cách nhanh nhất gắn nhầm một role guard lên `/trang-chu` (§2 — vòng lặp
redirect).

📌 **Màn Danh mục cũng không chặn route nữa** (Q39, 2026-09-06): thiếu quyền ghi thì
vẫn vào được, chỉ là không có nút sửa. Và **lý do đưa ra chính là màn này** — bảng
chi tiết ở V5 vốn đã hiện đủ 62 chỉ tiêu cho mọi người đăng nhập (Q21), nên chặn
màn Danh mục không giấu được số liệu nào. Ghi lại ở đây vì đó là một ràng buộc màn
này **tạo ra** cho màn kia: nếu sau này V5 bị giới hạn theo quyền, tiền đề của Q39
sập theo và phải xem lại cả hai màn cùng lúc.

## 6. Responsive

| Mốc | Hành vi |
| --- | --- |
| ≥ 981px | `.kpis` 5 cột; `.layout` hai card `1.15fr 0.85fr`; hàng nhóm `210px 1fr 80px` |
| ≤ 980px | `.kpis` rớt về **2 cột**; `.layout` rớt về **1 cột** (biểu đồ xuống dưới danh sách nhóm); hàng nhóm co còn `140px 1fr 75px`; sidebar thành drawer |
| ≤ 560px | `.kpis` giữ 2 cột nhưng `gap: 8px`, **tile cuối chiếm trọn hàng** (`grid-column: 1 / -1`) nên 5 tile chia 2-2-1; `.value` của tile giảm 21px → 18px; hàng nhóm co còn `110px 1fr 68px`; toolbar áp `styles.scss` § `@media (max-width: 560px)` (ô tìm kiếm trọn hàng, `.toolbar-sep` ẩn, `.filter-panel` còn `min(320px, 86vw)`) |

Bảng chi tiết cuộn ngang ở màn hẹp — 9 cột khai theo phần trăm nên chúng co lại
chứ không tự xuống dòng.

**In ấn** (`fe-ui-conventions.md` §"In ấn"): cả **hai** `.toolbar` (V1 và V5)
mang `.no-print`, `Toast` và sidebar/topbar cũng bị ẩn theo quy tắc chung ở
`styles.scss` § `@media print`. Biểu đồ và bảng thì **in ra**. Không ép chiều cao cố định cho
bảng khi in — `@media print` phải gỡ `max-height` của vùng cuộn để trình duyệt
ngắt trang tự nhiên.

⚠️ Bản in **không** thay cho `Xuất báo cáo`. File `.xlsx` là bản dữ liệu dùng
lại được; bản in chỉ là ảnh của màn hình.

## 7. Ánh xạ trường UI ↔ trường dữ liệu

**Casing** (`fe-api-client.md` §"Quy tắc casing"): DTO giữ **nguyên xi**
`camelCase`; model app `PascalCase` + prefix `I`; mapper trong `services/` của
feature là **nơi duy nhất** casing đổi.

⚠️ **Hệ quả của casing đã chốt mà mapper phải xử lý:** trường `null` **không ra
dây** — nó **vắng mặt** khỏi JSON (`doc/contracts/danh-muc-dti.md` §0, file chủ về
casing cho cả hai màn). Mọi ô mang dấu `?` dưới đây phải được mapper đọc "vắng
mặt" và "null" như nhau.

Bảng dưới bám CONTRACT DB-1 / DB-3 / DB-4, cả ba đã **AGREED** 2026-09-05.

### 7.1 Chọn kỳ → tham số

| Điều khiển UI | Query param API | Query param URL |
| --- | --- | --- |
| `.segmented` Tuần/Tháng, option "Tất cả" | `mode` = `week` \| `month` \| `year` | `mode` |
| `select` Năm | `year` | `year` |
| `select` Kỳ | `date` (ngày bất kỳ trong kỳ); bỏ trống = kỳ hiện tại | `period` (`YYYY-Www` \| `YYYY-MM`) |

| Bộ lọc bảng chi tiết | Query param API | Query param URL |
| --- | --- | --- |
| Ô tìm kiếm ở V5 | `search` | `q` |
| Nhóm chỉ tiêu | `groupId` | `groupId` |
| Trạng thái | `status` — đúng 1 trong 4 chuỗi Q4, giá trị lạ → `400` | `status` |

Danh sách kỳ đến từ `GET /api/dashboard/periods` → `years`, `weeksInYear[]`,
`monthsInYear[]`, mỗi phần tử `{ value, date, overallProgress }`. `value` chính là
giá trị dùng cho `period` của `GET /api/criteria` ở màn Danh mục — **một** nguồn,
gọi qua service dùng chung ở `shared/services/`.

### 7.2 Dải KPI, nhóm, biểu đồ

| Phần tử UI | DTO | Model app | Ghi chú |
| --- | --- | --- | --- |
| Nhãn kỳ ở `.period-display` | `periodLabel` | `PeriodLabel` | BE trả sẵn chuỗi đã có khoảng ngày (Q12) — FE **không** tự ghép lại |
| *(mốc kỳ, dùng khi cần tự định dạng)* | `periodStart`, `periodEnd` | `PeriodStart`, `PeriodEnd` | BE ghi rõ, FE **không** tự tính từ số tuần (Q12) |
| KPI 1 — Tiến độ chung | `kpi.overallProgress` | `Kpi.OverallProgress` | `null` → `—`, **không** hiển `0`. Đại lượng là bình quân **`Tiến độ %`** gia quyền theo `Điểm tối đa` (`spec/dashboard-dti/business-rules.md` §1.1) — **KHÔNG** phải `Σ Tự đánh giá / Σ Điểm tối đa`. Caption của ô phải nói đúng đại lượng này (§3.2, sửa 2026-09-09) |
| KPI 2 — So với kỳ trước | `kpi.delta` + `kpi.previousPeriodLabel` | `Kpi.Delta` / `Kpi.PreviousPeriodLabel` | biến thể `.good`/`.bad` theo dấu |
| KPI 3 — Chỉ tiêu tăng | `kpi.up` | `Kpi.Up` | |
| KPI 4 — Không tăng | `kpi.flat` | `Kpi.Flat` | |
| KPI 5 — Hoàn thành | `kpi.done` / `kpi.totalCriteria` | `Kpi.Done` / `Kpi.TotalCriteria` | hiển `{done}/{total}` |
| *(không có chỗ dùng nào trên màn)* | `kpi.down` | `Kpi.Down` | Bản trước ghi *"dùng cho option lọc 'Giảm' ở V5"* — option đó **đã bỏ** cùng ô lọc "Mức thay đổi" (Q22, §3.5). Trường vẫn về trong response; FE map nhưng **không** render, và đừng dựng control mới chỉ để có chỗ dùng nó |
| Hàng nhóm | `groups[] = { groupId, groupCode, groupName, progress }` | `IGroupProgress` | `progress` là **`Tiến độ %`** (Q11). Nhãn hiển thị `{groupCode}. {groupName}` (Q42, §3.3) |
| Điểm biểu đồ | `trend[] = { period, periodLabel, value }` | `ITrendPoint` | **Một phần tử cho mỗi kỳ** (Q44). Kỳ rỗng thì `value` **vắng mặt** trên dây ⇒ mapper đổi thành `null`, **không** thành `0`. `periodLabel` là nhãn trục X BE dựng sẵn — FE không ghép lại (Q43); `period` là khoá. *(Trước 2026-09-10: `{ label, value }`, chỉ điểm có dữ liệu.)* |

### 7.3 Bảng chi tiết — 9 cột (bộ trường ĐỔI theo Q8)

| Cột UI | DTO | Model app | Ghi chú |
| --- | --- | --- | --- |
| *(khoá hàng)* | `criteriaId` | `CriteriaId` | |
| Mã | `code` | `Code` | |
| Chỉ tiêu | `name` | `Name` | |
| Nhóm | `groupName` (+ `groupCode`, `groupId`) | `GroupName` / `GroupCode` / `GroupId` | `groupId` dùng cho bộ lọc. Hiển thị `{groupCode}. {groupName}` — Q42, cùng khuôn §3.3; header và bề rộng cột theo `doc/Design/Frontend/PlatformManager/Screens/01-dashboard.md` § Layout Blueprint |
| Điểm tối đa | `maxScore` | `MaxScore` | |
| Tự đánh giá | `selfScore` | `SelfScore` | **THÊM** so với card cũ |
| Thẩm định | `verifiedScore` | `VerifiedScore` | **THÊM** |
| Chênh lệch | `diff` | `Diff` | **THÊM** — trường TÍNH, **`Thẩm định − Tự đánh giá`** (Q25, đổi chiều 2026-09-05). FE không tính lại. Màu qua `DeltaIndicator`: dương → `.up` (xanh), âm → `.down` (đỏ), 0 → `.flat` (xám) |
| Trạng thái | `status` | `Status` | **ĐỔI** — 4 giá trị Q4 do người dùng chọn tay, thay cho `badge` hệ thống tự tính của card cũ |
| Minh chứng/Ghi chú | `note` | `Note` | giữ nguyên |
| *(BỎ)* | ~~`previousValue`~~, ~~`currentValue`~~, ~~`delta`~~, ~~`badge`~~ | | Q8 bỏ 3 cột so sánh tuần; `badge` cũ tự tính nên mâu thuẫn Q4 |

Tên trường **cố tình trùng** với `GET /api/criteria` của màn Danh mục — hai màn
đọc cùng một tập dữ liệu, hai từ vựng khác nhau cho cùng một con số là mâu thuẫn
tự tạo. Sự trùng đó bao gồm cả **chiều tính** của `diff`: Q25 đổi công thức cho
**cả hai** màn cùng lúc, và để một màn tính ngược màn kia thì cùng một chỉ tiêu sẽ
hiện `+5,00` xanh ở đây và `−5,00` đỏ ở kia.

⚠️ **Q25 đổi dấu so với dữ liệu gốc BA gửi, và mọi số minh hoạ phải đổi theo.** Hai
ví dụ thật, đo trên **file BA gửi tháng 8/2026** (nêu bằng tên, không bằng đường dẫn — file cố ý ngoài repo; bộ mẫu
ẩn danh trong repo là dataset khác và tính `Chênh lệch` theo chiều ngược, không dùng thay được):

| Chỉ tiêu | Tự đánh giá | Thẩm định | `diff` **mới** (Q25) | Màu | `diff` cũ (đã bỏ) |
| --- | --- | --- | --- | --- | --- |
| 1.1 | 7,04 | 10 | **`+2,96`** | `.up` xanh | `−2,96` |
| 1.4 | 5 | 0 | **`−5,00`** | `.down` đỏ | `+5,00` xanh |

Hàng 1.4 là lý do Q25 tồn tại: tự chấm 5 điểm rồi bị thẩm định bác trắng là ca
**xấu nhất** trong bộ dữ liệu, mà công thức cũ tô nó **xanh**. Chiều mới làm màu
khớp ý nghĩa nghiệp vụ — dương là *được thẩm định cao hơn tự chấm*, âm là *bị trừ*.
Công thức và epsilon: `spec/dashboard-dti/business-rules.md`; bảng biến thể màu:
`doc/Design/Frontend/PlatformManager/Components/DeltaIndicator.md`.

⚠️ **Ánh xạ trạng thái → màu badge là việc của FE, không phải trường của API.**
`badge` cũ đã bị gỡ khỏi hợp đồng; FE nhận `status` (một trong bốn chuỗi Q4) và
tự chọn `.ok` / `.warn` / `.bad` / `.neutral` theo Q10. Luật ánh xạ ở
`spec/dashboard-dti/business-rules.md` §Trạng thái.

⚠️ **Bốn trường bị bỏ để lại một khoảng trống có thật, và Q22 đã đóng nó bằng cách
bỏ control chứ không bằng cách thêm dữ liệu.** `kpi.up` / `kpi.flat` / `kpi.down`
vẫn còn (ô KPI 3 và 4), tức BE **vẫn** so sánh với kỳ trước dù bảng không hiện cột
nào của phép so sánh đó — nhưng `table[]` không mang kết quả so sánh ra, nên
**không lọc và không sắp xếp theo nó được**. Vì vậy Q22 gỡ hẳn ô lọc "Mức thay
đổi" và hai tuỳ chọn sắp xếp dựa trên nó (§3.5), thay vì để lại ba control bấm vào
không có gì xảy ra. `kpi.down` **không** còn chỗ dùng nào trên màn này sau Q22 —
option lọc "Giảm" mà bản trước của dòng này nhắc tới đã bị bỏ cùng ô lọc đó.

### 7.4 Xuất báo cáo

| UI | Endpoint / header |
| --- | --- |
| Nút `Xuất báo cáo` | `GET /api/dashboard/export` — **đúng bộ tham số của DB-1**: `mode`, `date`, `year`, `search`, `groupId`, `status` |
| Chế độ `Tất cả` | `mode=year` **không hỗ trợ** → nút `disabled`, xem §4 |
| Kiểu nội dung | `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet` — FE kiểm trước khi coi body là file |
| Tên file | đọc từ `Content-Disposition` (có cả `filename` ASCII lẫn `filename*`), **không** tự ghép ở FE |
| *(đã gỡ)* | ~~`GET /api/dashboard/report`~~ — card DB-2 cũ, gỡ theo Q13 cùng với component `report-dialog` |

**Export tôn trọng bộ lọc đang áp ở V5 — đã chốt Q23 (2026-09-05), không còn là
giả định.** DB-4 nhận đúng object bộ lọc của DB-1, theo luật Core
`doc/huong_dan/wiki-core/be/15-import-export.md` §4, và có integration test canh:
*cùng bộ lọc → số dòng dữ liệu trong file `.xlsx` = số phần tử `table` của
`GET /api/dashboard`*. Việc giao diện còn lại là **chỉ báo cạnh nút** cho biết file
sắp tải chỉ chứa phần đang lọc — §9 mục 2, đó là câu hỏi về hình dạng chỉ báo chứ
không phải về hành vi export.

⚠️ **Tiêu đề cột `Chênh lệch` trong file `.xlsx` ghi rõ chiều tính:**
`Chênh lệch (Thẩm định − Tự đánh giá)` (Q25). FE không dựng file nên không đặt tiêu
đề đó — nhưng khi màn hình nhắc tới nội dung file xuất thì phải dùng đúng cách gọi
này, vì người mở file sẽ đối chiếu với cột `Chênh lệch` của bản BA gửi và thấy
ngược dấu. Đặc tả bố cục file: `spec/dashboard-dti/business-rules.md` §Export.

## 8. Chuỗi hiển thị và i18n

**Hạ tầng dịch ĐÃ CÓ** — sửa 2026-09-05 sau khi đo lại. Bản trước của mục này
viết *"chưa có thư viện dịch trong `src/FE/package.json`"*; câu đó **sai**:

| Có thật hôm nay | Kiểm bằng |
| --- | --- |
| `@ngx-translate/core` + `@ngx-translate/http-loader` trong `dependencies` | `grep -n 'ngx-translate' src/FE/package.json` |
| File dịch có nội dung thật (`AUTH.INVALID_CREDENTIALS`…) | `ls src/FE/public/i18n` |
| Test canh khuôn khoá | `src/FE/src/app/app-i18n.spec.ts` |

> ### 🔄 SỬA 2026-09-10 — hai file luật từng bị nêu ở đây **đã được sửa**
>
> Chỗ này từng cảnh báo `doc/huong_dan/wiki-core/fe/08-i18n.md` § Điểm xuất phát còn ghi
> *"🚧 0% … cả 5 lệnh in `0`"* và `doc/huong_dan/quy-uoc/fe-ui-conventions.md` §i18n còn viết
> *"khi thư viện chưa cài"*. Cả hai đã sửa 2026-09-10, nên cảnh báo cũ nay tự nó là câu sai —
> giữ nguyên sẽ khiến người đọc đi sửa một thứ đã đúng.
>
> **Khuôn khoá đã chốt và đã áp:** chuỗi của màn này dùng `dashboard.<nhóm>.<tên>` ở
> `src/FE/public/i18n-app/`; chuỗi dùng chung dùng `shared.<tên>.<tên>` ở
> `src/FE/public/i18n/`. Nhánh `components.*` từng tồn tại **đã bị gỡ hết** cùng ngày, vì nó
> không theo khuôn nào của `08-i18n.md` §2 và nó nằm ở **cả hai** file bảng dịch — ca mà
> `app-i18n.spec.ts` không bắt được (nó chỉ chặn trùng **lá**, không chặn nhánh bắc cầu).

Vì hạ tầng đã có, chuỗi mới của màn này **phải đi qua khoá dịch** ngay từ đầu,
không hardcode rồi bọc sau.
Màn này có ba chỗ dễ vi phạm nhất, đều là chỗ đang ghép chuỗi:

- **Nhãn kỳ và option kỳ** (§3.1) — bốn định dạng, mỗi cái phải là **một** chuỗi
  có tham số (`{tuần}`, `{từ ngày}`, `{đến ngày}`, `{tiến độ}`), không phải nối
  bằng `+`. Thứ tự thành phần trong câu đổi theo ngôn ngữ.
- **Định dạng số và ngày** — bản duyệt dùng dấu phẩy thập phân (`82,1%`,
  `+2,96`) và ngày `dd/MM/yyyy`, đúng locale `vi`. Đây là **định dạng theo
  locale**, không phải hằng số: dùng cơ chế format theo locale
  (`doc/huong_dan/wiki-core/fe/08-i18n.md`), đừng thay dấu chấm bằng dấu phẩy
  bằng tay. *(Bản trước của dòng này nêu `−2,96` — số của công thức cũ; Q25 đổi
  chiều nên chỉ tiêu 1.1 nay là `+2,96`, xem §7.3.)*
- **Dấu của `DeltaIndicator`** — dấu `+` / `−` là **một phần của giá trị**, không
  phải ký tự ghép thêm bằng `+` trong template. Dùng cơ chế `signDisplay` của
  format theo locale; ghép tay sẽ ra `+-5,00` ở đúng ca số âm.
- **KPI `{done}/{total}`** và caption `{đang hiện}/{tổng} chỉ tiêu` — chuỗi có
  tham số đếm, không tự chọn số ít/số nhiều bằng `if`.

Copy verbatim từng chuỗi thuộc `Screens/01-dashboard.md` (AGENT A).

## 9. Cần chốt — KHÔNG tự quyết

Vòng 2026-09-05 (Q25–Q34, T7–T9) đóng **bốn** mục; vòng 2026-09-06 (Q35–Q39,
T10–T13) đóng nốt mục lớn nhất còn lại. Vòng **2026-09-09** chốt thêm ba điều (caption
ô KPI 1 · nhãn ô 1 của hàng lịch sử ở chế độ Tháng · xác nhận lại Q29 — ba hàng cuối
của bảng dưới) và **không mở mục mới** nào. Vòng **2026-09-10** chốt Q42 · Q43 · Q44 ·
Q46 · Q47 · Q52 · Q54 · Q57 · Q59 · Q61 · Q62 · Q63 — sổ theo mã Q ở
`spec/dashboard-dti/business-rules.md` §7, mặt hiển thị ở §2, §3.2, §3.3, §3.4, §3.5, §7.2
của file này. Danh sách còn lại đúng một mục:

1. ~~**Chỉ báo "file xuất chỉ chứa phần đang lọc".**~~ **CHỐT 2026-09-06 — hai lớp,
   không phải một.**

   **Lớp 1, trên màn hình:** khi đang có điều kiện lọc, `title` của nút đổi thành
   `Tải file Excel (.xlsx) — CHỈ các chỉ tiêu đang lọc (<n>/62)`. Không đổi nhãn nút,
   không thêm dải băng: nút nằm ở V1, xa bộ lọc ở V5, nên chỗ duy nhất chắc chắn
   người dùng nhìn vào ngay trước khi bấm là chính cái nút.

   **Lớp 2, trong file — mới là lớp quan trọng:** khối nhận dạng kỳ ở đầu sheet thêm
   một dòng `Bộ lọc đang áp`, liệt kê các điều kiện, hoặc `Không lọc — đủ 62 chỉ tiêu`.

   Vì sao lớp 2 mới là lớp thật: **file sống lâu hơn màn hình.** Người mở file tuần
   sau, hoặc người nhận file qua email, không có cách nào biết nó được xuất lúc đang
   lọc gì. Một tooltip không đi theo file. Đây cũng là lý do không chọn phương án
   "chỉ cảnh báo trên giao diện".

   Phần hình dạng chỉ báo trên màn thuộc `Screens/01-dashboard.md`; phần dòng trong
   file thuộc `doc/contracts/dashboard.md` DB-4 và `spec/dashboard-dti/business-rules.md` §4.

> **Đã chốt, không còn là câu hỏi mở** — ghi lại để người đọc bản trước không mang
> theo giả định cũ. **Ba hàng cuối là của vòng 2026-09-09**; các hàng trên thuộc hai
> vòng 2026-09-05 và 2026-09-06:
>
> | Từng là câu hỏi mở | Chốt |
> | --- | --- |
> | Ai được **xem** Dashboard | Mọi người đăng nhập, không cần quyền DTI — Q21 (§2, §5.6). Route giữ `authGuard` + `mustChangePasswordGuard`, **không** role guard; không vẽ trạng thái "không có quyền" |
> | Lọc "Mức thay đổi" + sắp xếp "Tăng nhiều nhất" | **BỎ** cả ô lọc lẫn **hai** tuỳ chọn sắp xếp — Q22 (§3.5). DB-1 không cần `deltaBucket`/`sort`. Hai ô KPI `Chỉ tiêu tăng`/`Không tăng` **giữ** — chúng hiển thị, không lọc |
> | Bảng chi tiết là biến thể thứ hai của `DataTable`? | **KHÔNG** — dùng `Table` — T4 (§3.5) |
> | Export có theo bộ lọc | **CÓ** — Q23 (§7.4). Hệ quả giao diện còn lại: mục 2 ở trên |
> | Tên thư mục spec | `spec/danh-muc-dti/` + `spec/dashboard-dti/` (2026-09-05); ba chỗ trỏ tên cũ đã sửa |
> | **Số phận `platform/trang-chu/`** | **XOÁ HẲN** — Q29 (§2). `/trang-chu` và `APP_CORE_ROUTES.home` không đổi; `Screens/06-trang-chu.md` gắn banner LỊCH SỬ, không xoá. Hệ quả: dự án thứ hai dùng CoreBase **phải tự viết trang chủ** |
> | **Trục X biểu đồ ở chế độ THÁNG** | **`Th.1 … Th.12`**, không phải khoảng ngày — T7 (§3.4). Bất đối xứng với chế độ tuần là có chủ ý |
> | **Màu của `Chênh lệch`** | Công thức đổi thành **`Thẩm định − Tự đánh giá`**; dương → xanh, âm → đỏ, 0 → xám — Q25 (§7.3). Chốt một lần cho **cả hai** màn |
> | **Dashboard trống ngay sau import** | Trạng thái **bình thường** có tên riêng: "đã có chỉ tiêu, chưa có `Tiến độ %`" → `NoticeBanner` + nút sang `/danh-muc/dti` — Q32 (§5.3.1) |
> | **Chỉ báo đang tải** | **Làm mờ V2–V4 + một vòng quay nhỏ** — Q34 (§5.2). Bản trước **không có gì**, đó là lỗi đã ghi nhận |
> | **Vòng quay lấy ở đâu** | **`p-progressSpinner` của PrimeNG** — T10 (§5.2). Đã là dependency, ăn màu theo preset nên **không cần token mới**, và **KHÔNG** thêm hàng vào `COMPONENTS.md` (chrome do thư viện vẽ). Lớp làm mờ là markup page-local |
> | **Dải băng dùng nút hay link** | **Link chữ inline** — T11 (§5.3.1). Hợp đồng `NoticeBanner` không có slot nút; mở rộng nó chỉ để có một cái nút là đi ngược yêu cầu dùng lại component sẵn có |
> | **Ô KPI nào có số ngay sau import** | Ô 1 và 2 → **`—`**; ô 3 và 4 → **`0`**; ô 5 → **số thật** (`26/62` trên dữ liệu thật) — T12 (§5.3.1). `—` = không tính được, `0` = tính được và bằng không |
> | **Caption ô KPI 1 nói đại lượng gì** | **`Bình quân Tiến độ %, gia quyền theo Điểm tối đa`** — 2026-09-09 (§3.2, §7.2). Caption cũ tả `Σ Tự đánh giá / Σ Điểm tối đa` (**% theo điểm**), một đại lượng khác hẳn và sau Q24 không còn quan hệ nào với thứ ô này hiển thị. Chú thích prototype `(thật: 787,84/960)` và `(thật)` **bỏ khỏi cả năm ô** |
> | **Ô 1 của hàng lịch sử ở chế độ THÁNG** | **`01/08 – 31/08/2026`** — **đúng khuôn** `{dd/MM} – {dd/MM/yyyy}` của chế độ Tuần, không rút gọn (2026-09-09, §3.6). Chốt bằng **phép đo**, không bằng phỏng đoán: hai chuỗi cùng **18 ký tự** trong một ô 150px, nên tiền đề "chế độ tháng không đủ chỗ" là sai; T7 không áp được vì trục X có 12 nhãn chen nhau còn Lịch sử mỗi kỳ một hàng. Mục này treo bốn ngày vì chưa ai đo. *(Phương án `Tháng 8/2026` chốt sáng cùng ngày rồi **hoàn lại** — nó lược khoảng ngày, thứ `spec/dashboard-dti/business-rules.md` §6.2 cấm lược.)* |
> | **Số phận `platform/trang-chu/` — hỏi lại lần hai** | **Vẫn XOÁ HẲN** — người dùng xác nhận lại 2026-09-09 (§2). Không có gì đổi so với Q29: `/trang-chu` và `APP_CORE_ROUTES.home` giữ nguyên, hệ quả "dự án thứ hai phải tự viết trang chủ" vẫn còn nguyên hiệu lực |

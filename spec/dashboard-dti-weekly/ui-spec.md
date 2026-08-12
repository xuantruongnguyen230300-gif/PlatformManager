# UI Spec — Dashboard "DTI Weekly"

> Nguồn: `doc/Prototype/dashboard.html` (đọc toàn bộ CSS + cả 2 khối
> `<script>`), đối chiếu `doc/ERD/ERD.md` + `doc/ERD/PlatformManager.dbml` +
> `doc/ERD/example_db_ver1.csv`.
>
> Trạng thái pipeline design: `doc/Design/Frontend/PlatformManager/` mới có
> `README.md` + `UiInventory.md` (stage 1) — **chưa có `tokens.json` /
> `DESIGN.md` / component spec**. Spec này vì vậy mô tả UI/UX dựa trực tiếp
> trên prototype tĩnh, không tham chiếu token design (chưa tồn tại). Khi
> pipeline `/design-*` chạy tới stage extract-tokens, cần đối chiếu lại màu
> sắc/spacing trong bảng "Style thô" ở cuối file.
>
> Tài liệu song song bên backend: `spec/dashboard-dti-weekly/business-rules.md`
> (do `backend-expert` viết) — xác nhận khớp với spec này ở các điểm badge
> vs `Status`, `Owner`/`Deadline`/`CriteriaEvidence` không cần UI ở slice
> này. `doc/ERD/ERD.md` đã cập nhật mục "Quyết định đã CHỐT": `Status` nhập
> tay lưu DB trên `CriteriaAssessment` (đúng thiết kế ban đầu, nhưng UI cụ
> thể để nhập ở đâu vẫn là câu hỏi mở — xem mục 8.1), và Auth dùng ASP.NET
> Core Identity — `CriteriaAssessment.Owner` (text tự do) đã đổi thành
> `OwnerId` (FK → `AppUser.Id`).
>
> ## ⚠️ CẬP NHẬT KIẾN TRÚC (2026-08-12) — Dashboard đổi thành READ-ONLY
>
> Theo quyết định người dùng (xem `spec/dashboard-dti-weekly/business-rules.md`
> mục 0 và `spec/danh-muc-dti/business-rules.md`): **toàn bộ khả năng nhập
> liệu đã bị bỏ khỏi Dashboard**, chuyển sang màn hình mới
> **"Danh mục > DTI"** (`doc/Prototype/danh-muc-dti.html`, xem
> `spec/danh-muc-dti/ui-spec.md`). Dashboard (`doc/Prototype/dashboard.html`)
> giờ **chỉ hiển thị**: KPI, tiến độ theo nhóm, biểu đồ xu hướng, bảng 62
> chỉ tiêu (đọc), lịch sử các kỳ đã lưu, "Xuất báo cáo" (đổi tên từ "Báo
> cáo nhanh" ở vòng phản hồi #2, xem mục 2.3), bộ lọc Tuần/Tháng (mục 2.2),
> và chọn 1 kỳ đã lưu để **xem lại** (không phải nhập).
>
> Các mục dưới đây **giữ nguyên làm tài liệu lịch sử** cho những action đã
> **chuyển đi** (đánh dấu rõ **"[ĐÃ CHUYỂN SANG Danh mục DTI]"** tại từng
> mục cụ thể) — không xoá hẳn vì rule nghiệp vụ đằng sau (validate, upsert,
> công thức) **vẫn đúng nguyên vẹn**, chỉ đổi UI nào gọi tới chúng (xem
> `business-rules.md` mục 0 lý do giữ nguyên tài liệu rule). Phần layout/
> actions còn hiệu lực trên chính Dashboard đã cập nhật để khớp
> `dashboard.html` hiện tại.
>
> ## ⚠️ CẬP NHẬT VÒNG PHẢN HỒI #2 (2026-08-12, bản chốt) — bộ lọc Tuần/Tháng + "Xuất báo cáo"
>
> Dashboard **vẫn giữ nguyên read-only** (quyết định vòng #1 không đổi) —
> bổ sung: (1) bộ lọc **Tuần/Tháng** (`.segmented`) đổi toàn bộ KPI/tiến độ
> nhóm/biểu đồ/bảng sang xem tổng hợp theo tháng (công thức trung bình cộng
> — xem `spec/danh-muc-dti/business-rules.md` mục 3); (2) nút **"Báo cáo
> nhanh" đổi tên thành "Xuất báo cáo"** (`.btn.primary`), vẫn chỉ mở
> `dialog#reportDialog` (không ghi dữ liệu) nhưng nay Tuần/Tháng-aware. Chi
> tiết đầy đủ: mục 2.2/2.3.
>
> ## ⚠️ CẬP NHẬT VÒNG PHẢN HỒI #3 (2026-08-12) — mật độ hiển thị + phân trang + lựa chọn tuần cụ thể/"Tất cả" + bộ lọc năm + ghim "Xuất báo cáo"
>
> 4 nhóm điều chỉnh bổ sung, tất cả đã áp dụng trong `doc/Prototype/dashboard.html`:
> 1. **Mật độ hiển thị (compact)** — cùng bộ token `--fs-*`/`--sp-*`/`--radius-*`/`--sidebar-w*` như
>    `danh-muc-dti.html` (xem `spec/danh-muc-dti/ui-spec.md` mục 9) — không đổi màu/token thương hiệu.
> 2. **Phân trang bảng "62 chỉ tiêu DTI"** (10/20/50 dòng/trang) thay cho cuộn dọc liên tục — xem mục
>    2.4 (mới) và 3.16 (mới). **Không có sticky column** ở bảng này (bảng Dashboard không có cột hành
>    động Sửa/Xoá — chỉ đọc, đúng như hướng dẫn "nếu không có cột hành động thì bỏ qua").
> 3. **Cuộn ngang xác nhận lại** (`overflow-x:auto` tường minh trên `.tablewrap`, không đổi bản chất).
> 4. **`select#savedWeeks` thêm lựa chọn "Tất cả"** (`__ALL__`) — tổng hợp toàn bộ lịch sử thay vì 1
>    tuần lẻ (xem mục 2.2b mới); **chế độ Tháng tách riêng bộ lọc Năm** (`select#yearFilter`) +
>    **12 tháng** (`select#monthFilterDropdown`) thay cho `input type=month` gộp cũ (xem mục 2.2a cập
>    nhật); **"Xuất báo cáo" ghim cố định phải** qua `.weekbar-actions{margin-left:auto}` (không dịch
>    chuyển khi số control trong `weekbar` đổi lúc chuyển Tuần↔Tháng).
>
> ## ⚠️ CẬP NHẬT VÒNG PHẢN HỒI #4 (2026-08-12, ĐÃ CHỐT CHÍNH THỨC) — "Tất cả" theo NĂM + KHÔNG carry-forward + bộ lọc năm cho biểu đồ
>
> Ghi đè TODO ở vòng #3 (bên dưới) — người dùng đã chốt qua `backend-expert`:
>
> 1. **KHÔNG carry-forward** — 1 kỳ-tuần/tháng/năm không có thao tác nào cho 1 chỉ tiêu → **loại hẳn**
>    khỏi mẫu tính trung bình của chỉ tiêu đó (không tính 0%, không lấy giá trị kỳ trước). Cơ chế:
>    field `touched` trên mỗi period (ghi từ `danh-muc-dti.html`, xem `spec/danh-muc-dti/ui-spec.md`
>    banner vòng #4) — Dashboard đọc qua `isTouched(p, code)` (dữ liệu cũ trước khi có field này coi
>    như đã touched, fallback bao dung). Áp dụng cho **Tháng** (`monthlyOverallProgress`/
>    `monthlyCriteriaProgress`/`monthlyGroupProgress`) và **"Tất cả"** (`yearly*`) — KHÔNG áp dụng cho
>    KPI/bảng ở chế độ **Tuần đơn lẻ** (hiển thị đúng snapshot của kỳ đang xem, không phải 1 phép tính
>    trung bình đa-kỳ nên không thuộc phạm vi "carry-forward" bị cấm).
> 2. **"Tất cả" đổi ý nghĩa hoàn toàn**: KHÔNG còn là "toàn bộ lịch sử mọi thời điểm" (bản nháp vòng
>    #3) mà là **toàn bộ dữ liệu trong 1 NĂM được chọn** — thêm `select#allHistoryYearFilter` (chỉ
>    hiện khi bật "Tất cả"). Hàm kích hoạt đổi tên `showAllHistoryAggregate()` →
>    **`showYearAggregate()`**, các hàm tính/render đổi tên tương ứng: `renderKPIsAllHistory`→
>    `renderKPIsYearAggregate`, `renderGroupsAllHistory`→`renderGroupsYearAggregate`,
>    `renderTableAllHistory`→`renderTableYearAggregate`, `generateAllHistoryReport`→
>    `generateYearAggregateReport`, thêm mới `renderTrendYearAggregate`. Xem mục 2.2b (viết lại).
> 3. **Bộ lọc năm cho biểu đồ** (yêu cầu "biểu đồ cần bộ lọc theo tuần/tháng/năm"): biểu đồ xu hướng ở
>    chế độ **Tháng** (`renderTrendMonthly()`) đổi từ "12 tháng gần nhất kiểu cửa sổ trượt toàn cục"
>    sang **12 tháng Jan–Dec của năm đang chọn** (`select#yearFilter`), chỉ vẽ tháng có dữ liệu (không
>    nội suy). Biểu đồ ở chế độ **"Tất cả"** (`renderTrendYearAggregate()`, mới) vẽ theo TUẦN, chỉ
>    trong phạm vi năm đang chọn (`select#allHistoryYearFilter`).
> 4. **Tuần ISO (Thứ 2–CN)** và **không copy-forward `CriteriaEvidence`** — xác nhận đúng thiết kế cũ,
>    không có thay đổi UI nào ở Dashboard (rule dữ liệu thuần backend).
>
> Nguồn nghiệp vụ đầy đủ: `spec/dashboard-dti-weekly/business-rules.md` mục 3.3 (công thức "Tiến độ
> chung" viết lại hoàn toàn) — spec UI này không lặp lại công thức toán, chỉ mô tả UI/UX.
>
> ## ⚠️ ĐÁNH GIÁ VÒNG PHẢN HỒI #5 (2026-08-12) — KHÔNG áp dụng cơ chế "gói bảng trong 1 màn hình" ở đây
>
> Người dùng chỉ yêu cầu cơ chế "bảng + phân trang gói gọn trong 1 màn hình, chỉ `.tablewrap` cuộn dọc
> riêng" (xem `spec/danh-muc-dti/ui-spec.md` mục 6.11) cho `danh-muc-dti.html`, kèm gợi ý tự đánh giá có
> nên áp dụng nhất quán cho bảng "62 chỉ tiêu DTI" (`#tablePagination`) ở `dashboard.html` hay không.
> `frontend-expert` đã đọc `doc/Prototype/dashboard.html` và **quyết định KHÔNG áp dụng ở vòng này** —
> lý do kỹ thuật cụ thể (không phải ngại việc):
>
> 1. **Vị trí khác biệt về bản chất**: card bảng ở `danh-muc-dti.html` nằm ngay dưới 1 banner hướng dẫn
>    (gần đỉnh trang) — kỹ thuật đo `card.getBoundingClientRect().top` tại thời điểm tải trang phản ánh
>    đúng "khoảng trống còn lại của màn hình đầu tiên". Card `section.card` chứa bảng 62 chỉ tiêu ở
>    `dashboard.html` lại nằm **sau** `weekbar` + `section.kpis` (5 thẻ KPI) + `section.layout` (2 card
>    "Tiến độ theo nhóm"/"Biểu đồ xu hướng") — tổng chiều cao các phần này thường **đã vượt quá 1 màn
>    hình** trên nhiều viewport phổ biến, nên `rect.top` đo lúc tải trang (scroll=0) sẽ ra số rất lớn
>    (thậm chí lớn hơn `window.innerHeight`), khiến `max-height` bị kẹp về sàn an toàn tối thiểu
>    (`280px`) một cách tuỳ tiện — không phản ánh đúng ý định thật ("card vẫn có thể cao hơn nếu người
>    dùng CUỘN TỚI nó rồi mới xem là 1 màn hình", chứ không phải "card phải nằm gọn trong màn hình ĐẦU
>    TIÊN lúc tải trang"). Áp máy móc kỹ thuật này vào sẽ làm bảng bị co nhỏ giả tạo, TỆ HƠN hiện trạng.
> 2. **Vấn đề gốc người dùng báo không thực sự tồn tại y hệt ở đây**: người dùng phàn nàn "phải cuộn cả
>    trang xuống mới thấy phân trang" — ở Dashboard, việc cuộn xuống để **tới được** khu vực bảng 62 chỉ
>    tiêu (đi qua KPI/biểu đồ) vốn đã là hành vi bình thường/mong đợi (đây là dashboard nhiều khối nội
>    dung xếp dọc, không phải màn hình "chỉ có 1 bảng" như Danh mục DTI); vấn đề cụ thể "phân trang lọt
>    khỏi màn hình" có thể vẫn xảy ra (bảng dài + phân trang cuối bảng), nhưng cách khắc phục đúng đòi
>    hỏi đo "khoảng trống khi card đã ở đầu viewport sau khi cuộn tới" (vd `100vh - topbarHeight -
>    gutter`, không phải `100vh - rect.top hiện tại`) — một công thức KHÁC, chưa được kiểm chứng, không
>    nên áp nguyên xi cơ chế đã viết cho `danh-muc-dti.html`.
> 3. **Không có sticky column** ở bảng này (đã xác nhận vòng #3, mục 2.4) nên phần sửa xung đột z-index
>    2 chiều (mục 6.11 bên `danh-muc-dti.html`) **không áp dụng/không cần thiết** ở đây — nếu làm lại
>    cơ chế cuộn nội bộ cho Dashboard sau này, chỉ cần bật `overflow-y:auto` + sticky `<thead>` đơn giản,
>    không phải lo phần z-index cột trái/phải.
>
> **Kết luận**: `dashboard.html` giữ nguyên `.tablewrap{overflow-y:visible}` (cuộn dọc ở cấp trang) —
> không phải bỏ sót, mà là quyết định có chủ đích chờ người dùng xác nhận công thức đo phù hợp (đo tại
> thời điểm card cuộn tới đầu viewport, không phải tại thời điểm tải trang) trước khi áp dụng, tránh làm
> bảng bị co nhỏ giả tạo. Nếu người dùng xác nhận muốn áp dụng, `frontend-expert` sẽ quay lại thiết kế 1
> công thức đo riêng phù hợp bố cục nhiều-khối của Dashboard thay vì copy nguyên xi.

## 1. Tổng quan

- **Mục đích**: màn hình **chỉ xem** (read-only) cho phép cán bộ phụ trách
  chuyển đổi số (DTI) theo dõi **tiến độ % hoàn thành** của 62 chỉ tiêu
  chuyển đổi số theo **từng kỳ báo cáo (tuần)**, so sánh tự động với kỳ
  liền trước, và xuất báo cáo nhanh dạng văn bản. **Không còn nhập liệu ở
  màn này** — nhập/sửa dữ liệu thực hiện tại
  [Danh mục > DTI](../danh-muc-dti/ui-spec.md).
- **Người dùng**: cán bộ chuyên trách/đầu mối CĐS cấp xã/đơn vị, và bất kỳ
  ai cần xem tiến độ (lãnh đạo, người theo dõi) — không có vai trò xem-only
  hay phân quyền khác trong prototype (không có login/role trong
  `dashboard.html`), nhưng về mặt UX, đây giờ **đã là 1 màn "chỉ xem" cho
  mọi người dùng**, không phân biệt vai trò.
- **Tần suất dùng**: xem lại theo tuần — chọn 1 kỳ đã lưu (qua dropdown
  hoặc mục Lịch sử) để xem tiến độ, so với kỳ trước, xuất báo cáo nhanh.
  Việc "cập nhật % tiến độ, ghi chú, lưu tuần" đã **chuyển hẳn** sang
  Danh mục > DTI.
- **Lưu trữ ở bản prototype**: 100% phía client, `localStorage` (2 key:
  `dti_weekly_history_v2` cho các kỳ đã lưu, `dti_weekly_draft_v2` cho kỳ
  đang xem) — **dùng chung** với `doc/Prototype/danh-muc-dti.html` (cùng 2
  key), nên dữ liệu nhập/import ở Danh mục DTI hiển thị ngay khi mở lại
  Dashboard. Khi lên app thật, đây là phần sẽ thay bằng gọi API tới
  `AssessmentPeriod`/`CriteriaAssessment` (xem mục 6 và Contract Card cần
  viết riêng khi implement service layer).

## 2. Layout

Cấu trúc DOM chính, theo đúng thứ tự xuất hiện trong `dashboard.html`:

```
.sidebar                    → menu điều hướng toàn app, "Dashboard" active — xem
                               spec/sidebar-menu/ui-spec.md (KHÔNG đổi bởi read-only)
.topbar (sticky top, no-print)
  .topin
    .logo            → "Dashboard" + subtitle "Theo dõi tiến độ chuyển đổi số · Chỉ xem"
                         (KHÔNG còn action nào trong topbar — màn chỉ xem, không có toolbar)

main (max-width 1600px)
  .notice                    → [ĐÃ ĐỔI 2 lần] chỉ hiển thị, không nhập liệu + link sang Danh mục
                                > DTI + [MỚI vòng phản hồi #2] câu giới thiệu bộ lọc Tuần/Tháng
  section.weekbar.card (no-print)
    strong#periodLabel        [MỚI, ĐÃ ĐỔI vòng #3] text đổi động: "Kỳ đang xem:" (Tuần) ↔ "Đang
                               xem:" (Tuần + Tất cả) ↔ "Tháng đang xem:" (Tháng) — xem mục 2.2/2.2b
    input#weekDate type=date [disabled]   → ẩn khi ở chế độ Tháng HOẶC khi bật "Tất cả" — mục 2.1/2.2b
    span#allHistoryBadge [MỚI vòng #3]    → chỉ hiện khi bật "Tất cả" (badge "Tất cả" cạnh weekDate)
    select#savedWeeks                     → [ĐÃ ĐỔI vòng #3] thêm option "__ALL__" (Tất cả), ẩn khi ở
                                             chế độ Tháng, `onchange="onSavedWeekChange(this.value)"`
                                             (thay `loadSavedWeek` trực tiếp) — xem mục 2.2b
    ~~input#monthPicker type=month~~      → [ĐÃ BỎ vòng #3] thay bằng 2 select bên dưới
    select#yearFilter [MỚI vòng #3]       → chỉ hiện ở chế độ Tháng, `onchange="onYearMonthFilterChange()"`
    select#monthFilterDropdown [MỚI v#3]  → chỉ hiện ở chế độ Tháng, 12 tháng cố định — xem mục 2.2a
    .segmented (2 nút) [MỚI]              → #modeWeekBtn "Tuần" (mặc định active) / #modeMonthBtn "Tháng"
                                             (`onclick="setViewMode('week'|'month')"`) — xem mục 2.2
    .weekbar-actions [MỚI vòng #3]        → `margin-left:auto`, bọc riêng nút dưới đây để ghim phải
      btn.primary "Xuất báo cáo" [ĐÃ ĐỔI TÊN, từ "Báo cáo nhanh"] → mở lại dialog#reportDialog,
                                             nội dung theo đúng chế độ Tuần/Tháng/Tất cả — xem mục 2.3
    ~~btn "Tạo tuần mới từ kỳ gần nhất"~~ → [ĐÃ BỎ, chuyển sang Danh mục DTI — xem mục 3.3]
  section.kpis (grid 5 cột → 2 cột <980px)
    .card.kpi #kProgress (label#kProgressLabel [MỚI] đổi động) · #kDelta (label#kDeltaLabel [MỚI]
    đổi động) · #kUp · #kFlat · #kDone — GIỮ NGUYÊN cấu trúc, chỉ label 2 KPI đầu đổi text theo mode
  section.layout (grid 1.15fr/.85fr → 1 cột <980px)
    .card → "Tiến độ theo nhóm" → span#groupsSubtitle [MỚI] đổi động ("Tuần hiện tại"↔"Tổng hợp toàn
    bộ lịch sử" [MỚI v#3]↔"Tháng hiện tại") → #groups (6 .group-row) — GIỮ NGUYÊN cấu trúc
    .card → "Biểu đồ tiến độ hàng tuần" → span#trendSubtitle [MỚI] đổi động → canvas#trend
  section.card (bảng 62 chỉ tiêu)
    .title h2 "62 chỉ tiêu DTI" + #countText "start–end/total chỉ tiêu (tổng 62)" [ĐÃ ĐỔI vòng #3,
      format cũ "x/62 chỉ tiêu" — nay phản ánh khoảng đang hiển thị trên trang phân trang]
    .filters (no-print): #q (tìm), #groupFilter, #changeFilter, #sortBy — GIỮ NGUYÊN control, chỉ đổi
      handler sang `onTableFilterChange()` (reset về trang 1 trước khi `renderTable()`) — xem mục 2.4
    .tablewrap → table → thead: th#thPrev [MỚI id] "Tuần trước"↔"Tháng trước"↔"—" (Tất cả), th#thCurrent
      [MỚI id] "Tuần này"↔"Tháng này"↔"Tất cả (TB)" → tbody#tbody (tối đa `tablePageSize` hàng/trang)
      cột "Tuần này"/"Tháng này"/"Tất cả (TB)": text tĩnh `${progress}%` (chế độ Tháng/Tất cả: trung
        bình cộng, "—" nếu tháng/toàn lịch sử chưa có dữ liệu)
      cột "Ghi chú tuần": text tĩnh ở chế độ Tuần; ở chế độ Tháng/Tất cả luôn hiện ghi chú hướng dẫn
        xem 1 kỳ-tuần cụ thể (ghi chú vốn là dữ liệu theo từng kỳ-tuần)
    #tablePagination [MỚI vòng #3] → chọn số dòng/trang (10/20/50) + Trước/Sau — xem mục 2.4. KHÔNG
      có cột sticky (bảng này không có cột hành động Sửa/Xoá, khác `danh-muc-dti.html`)
  section.card (lịch sử)                                  → GIỮ NGUYÊN 100%, luôn liệt kê theo TUẦN
    .title h2 "Lịch sử các kỳ đã lưu"
    #history → .histrow mỗi kỳ đã lưu (mới nhất trước), nút "Xem" → loadSavedWeek() [ĐÃ ĐỔI NHỎ:
      nay tự chuyển bộ lọc về "Tuần" + tắt "Tất cả" nếu đang bật — xem mục 2.2b]
  .footer  → ghi chú "chỉ hiển thị — nhập/cập nhật ở Danh mục > DTI"

~~button.fab "Lưu"~~          → [ĐÃ BỎ HẲN — không còn hành động lưu ở Dashboard]

dialog#reportDialog          → h2#reportDialogTitle [MỚI id, đổi động theo Tuần/Tháng]
  #reportBox                 → nội dung report render bằng innerHTML — [MỚI] 2 nhánh nội dung
                                (theo tuần / theo tháng, xem mục 2.3)
  btn "Sao chép" · btn.primary "In"
```

### 2.1. Vì sao `input#weekDate` đổi thành `disabled` thay vì bỏ hẳn — quyết định UX (`frontend-expert` tự quyết theo yêu cầu)

`loadDraftForDate()` (hàm gốc gắn với `input#weekDate`) có tác dụng phụ:
nếu chọn 1 ngày **chưa từng lưu**, nó tự tạo 1 "draft xem trước" (copy giá
trị từ kỳ gần nhất, hoặc seed từ `initialProgress` nếu chưa có kỳ nào) và
ghi vào `localStorage` (`dti_weekly_draft_v2`). Đây là hành vi phù hợp cho
màn **nhập liệu** (xem trước 1 kỳ sắp tạo) nhưng **không phù hợp cho màn
chỉ xem** — người dùng Dashboard chỉ nên xem được các kỳ **đã thực sự tồn
tại** (đã lưu qua Danh mục DTI), không nên có khả năng "tạo" ra 1 kỳ ảo
chưa từng lưu chỉ bằng cách gõ ngày.

**Quyết định**: giữ nguyên `<input id="weekDate" type="date">` (không đổi
thành `<span>`) nhưng thêm thuộc tính **`disabled`**, bỏ `onchange`. Lý do
kỹ thuật: input `disabled` vẫn hỗ trợ gán `.value` bằng JS (các hàm gốc như
`init()`/`loadSavedWeek()` gán `weekDate.value=draft.date` vẫn chạy đúng,
hiển thị đúng ngày của kỳ đang xem), chỉ chặn tương tác **của người dùng**
— an toàn hơn đổi sang `<span>` (sẽ làm im lặng mọi lệnh gán `.value` hiện
có trong JS gốc, vi phạm nguyên tắc "giữ nguyên logic JS gốc"). Cách chọn
kỳ để xem duy nhất còn lại: `select#savedWeeks` (chỉ liệt kê kỳ **đã lưu
thật**) và nút "Xem" trong mục Lịch sử — cả hai đều không có rủi ro tạo
draft ảo.

### 2.2. Bộ lọc "Tuần / Tháng" — [MỚI, vòng phản hồi #2] tính năng bổ sung, KHÔNG phải nhập liệu

Người dùng yêu cầu thêm khả năng xem tổng hợp **theo tháng** bên cạnh xem
theo từng kỳ-tuần đã có — công thức lấy nguyên từ
`spec/danh-muc-dti/business-rules.md` mục 3 (đã chốt): **trung bình cộng**
`ProgressPercent` các kỳ-tuần thuộc tháng, áp dụng ở cả 2 cấp (từng
`Criteria` và "Tiến độ chung" toàn danh mục — tính "Tiến độ chung" cho mỗi
kỳ-tuần trước, rồi lấy trung bình cộng các kết quả đó theo tháng).

- **`.segmented` 2 nút "Tuần"/"Tháng"** (`setViewMode('week'|'month')`) —
  mặc định "Tuần" (hành vi y hệt trước khi có tính năng này, không đổi gì).
  **[ĐÃ ĐỔI vòng #3]** chuyển hẳn sang "Tuần" giờ luôn tắt "Tất cả" (`allHistoryMode=false`) — xem
  mục 2.2b — người dùng bấm lại dropdown nếu muốn xem "Tất cả" một lần nữa.
- Chọn **"Tháng"**: **[ĐÃ ĐỔI vòng #3]** thay vì 1 `input#monthPicker type=month` gộp, nay hiện 2
  select riêng — `select#yearFilter` (danh sách năm có dữ liệu + năm hiện tại) và
  `select#monthFilterDropdown` (12 tháng cố định, "Tháng 1".."Tháng 12") — ẩn `weekDate`/`savedWeeks`/
  `allHistoryBadge` (chọn ngày/kỳ/"Tất cả" cụ thể không còn ý nghĩa ở chế độ tổng hợp tháng), mặc định
  chọn **năm+tháng của kỳ gần nhất có dữ liệu** (`fillYearMonthFilters()` gọi khi vào chế độ Tháng,
  set `.value` cho cả 2 select từ `selectedMonth` hiện có). Đổi 1 trong 2 select (`onchange=
  "onYearMonthFilterChange()"`) ghép lại thành `selectedMonth='YYYY-MM'`, giữ nguyên toàn bộ logic
  tổng hợp phía dưới không đổi. **Lý do tách riêng Năm khỏi Tháng**: theo đúng yêu cầu người dùng "1
  năm có 12 tháng — cần chọn được đang xem năm nào trước khi xem 12 tháng của năm đó" — dropdown
  tháng độc lập rõ ràng hơn 1 input `type=month` gộp (đặc biệt khi lịch sử trải nhiều năm).
  `input#monthPicker`/`onMonthPickerChange()` giữ nguyên trong JS làm dead code (không còn phần tử
  DOM gọi), theo đúng quy ước sẵn có của file.
  Toàn bộ KPI/"Tiến độ theo nhóm"/biểu đồ/bảng chuyển sang tính theo tháng
  (xem hàm `*Monthly()` tương ứng trong JS — `renderKPIsMonthly()`,
  `renderGroupsMonthly()`, `renderTrendMonthly()`, `renderTableMonthly()`).
- **Tháng không có dữ liệu**: hiển thị `"—"` (KPI, ô bảng) — **không** suy
  ra 0% hay nội suy từ tháng liền kề, đúng rule đã chốt ở
  `business-rules.md` mục 3 ("Tháng không có dữ liệu... không suy ra bằng
  0% hay nội suy").
- **"Tháng trước" để so sánh** = tháng **gần nhất có dữ liệu** trước tháng
  đang xem (không nhất thiết là tháng liền kề theo lịch — vd nếu tháng 5
  không có kỳ nào được lưu, "tháng trước" của tháng 6 sẽ là tháng 4 nếu có
  dữ liệu) — cùng tinh thần "kỳ liền trước" đã áp dụng cho tuần (mục 3.2 cũ
  ở `business-rules.md`), áp dụng nhất quán sang cấp tháng.
- **Biểu đồ xu hướng ở chế độ Tháng**: trục X là "Tiến độ chung" trung bình từng tháng, trục Y % —
  **[ĐÃ ĐỔI vòng #4]** trước đây trục X lấy "tối đa 12 tháng gần nhất có dữ liệu" theo cửa sổ trượt
  toàn cục (không phân biệt năm); nay **scope đúng theo năm đang chọn** ở `select#yearFilter` — vẽ 12
  tháng Jan–Dec của năm đó, chỉ hiện tháng nào **có dữ liệu** (không nội suy tháng trống), đáp ứng yêu
  cầu "biểu đồ cần bộ lọc theo tuần/tháng/năm cho phù hợp nghiệp vụ".
- **Khu Lịch sử luôn theo TUẦN, không đổi theo bộ lọc** — quyết định UX
  (`frontend-expert` tự quyết): Lịch sử là **nhật ký các lần lưu gốc**
  (audit trail), ý nghĩa khác "xem tổng hợp" — trộn lẫn 2 khái niệm sẽ gây
  nhầm "1 dòng lịch sử = 1 tháng" trong khi dữ liệu lưu trữ vẫn luôn là
  tuần (đúng khẳng định "đơn vị lưu trữ vẫn là tuần" ở `business-rules.md`
  mục 3). Bấm "Xem" ở 1 dòng lịch sử **tự động chuyển bộ lọc về "Tuần"**
  nếu đang ở "Tháng" (sửa nhỏ trong `loadSavedWeek()`) — vì xem 1 kỳ-tuần
  cụ thể ngầm định người dùng muốn nhìn chi tiết theo tuần.
- **Không phát minh token màu mới**: `.segmented`/`.seg-btn` dùng lại
  `--line`/`--brand`/`--bg` sẵn có, cùng công thức nút active như
  `.sidebar-navitem.active` đã dùng ở nơi khác.

### 2.2b. Lựa chọn "Tất cả" trong `select#savedWeeks` — [ĐÃ CHỐT vòng #4, thay thế bản nháp vòng #3]

Người dùng yêu cầu: ở chế độ Tuần, ngoài chọn 1 tuần cụ thể (`select#savedWeeks` đã có sẵn từ trước,
đúng tinh thần "chọn xem 1 tuần nào tuỳ theo ngày import/thêm mới"), cần thêm lựa chọn **"Tất cả"** để
xem tổng hợp — **đã chốt: tổng hợp trong phạm vi 1 NĂM được chọn**, không phải toàn bộ lịch sử vô hạn
như bản nháp vòng #3.

- **UI**: `<option value="__ALL__">— Tất cả (tổng hợp dữ liệu trong 1 năm) —</option>` làm option thứ 2
  (ngay sau placeholder rỗng) trong `select#savedWeeks`. `onchange="onSavedWeekChange(this.value)"` —
  tách nhánh: `__ALL__` → **`showYearAggregate()`** (đổi tên từ `showAllHistoryAggregate()` theo yêu
  cầu); giá trị khác (ngày cụ thể hoặc rỗng) → giữ nguyên gọi `loadSavedWeek()`.
- **`select#allHistoryYearFilter`** [MỚI vòng #4] — chỉ hiện khi `allHistoryMode===true`, liệt kê mọi
  năm có dữ liệu + năm hiện tại (`listAvailableYears()`, tái dùng đúng hàm đã có cho chế độ Tháng).
  Mặc định chọn năm của kỳ gần nhất có dữ liệu (`fillAllHistoryYearFilter()`, gọi khi
  `showYearAggregate()`). Đổi năm (`onAllHistoryYearFilterChange()`) → tính lại toàn bộ KPI/nhóm/biểu
  đồ/bảng theo năm mới, reset trang bảng về 1.
- **Trạng thái bật "Tất cả"** (`allHistoryMode=true`): `input#weekDate` ẩn, `span#allHistoryBadge` hiện
  text **`"Tất cả · <năm>"`** (đổi động theo `allHistoryYear`, không còn text tĩnh "Tất cả"),
  `select#allHistoryYearFilter` hiện, `strong#periodLabel` đổi thành "Đang xem:".
- **Dữ liệu hiển thị khi bật "Tất cả"** — **[ĐÃ CHỐT, không còn best-guess]**: công thức
  `yearlyOverallProgress(year)`/`yearlyCriteriaProgress(year,id)`/`yearlyGroupProgress(year,groupId)`
  — trung bình cộng các kỳ-tuần **CÓ dữ liệu trong năm đó**, **KHÔNG carry-forward**: 1 chỉ tiêu chỉ
  được tính vào trung bình nếu có `touched[code]===true` ở kỳ-tuần đó (xem `isTouched()`, banner đầu
  file) — kỳ-tuần không có thao tác cho chỉ tiêu đó bị **loại hẳn khỏi mẫu số**, không tính là 0%. Cấp
  "Tiến độ chung" cũng vậy: mỗi kỳ-tuần tự tính "Tiến độ chung" của riêng nó (loại chỉ tiêu chưa touched
  khỏi mẫu số của kỳ-tuần đó), rồi lấy trung bình các kết quả theo năm (loại kỳ-tuần không có chỉ tiêu
  nào touched khỏi mẫu). KPI "So với tuần trước" hiện `—` cố định (không có khái niệm "kỳ liền trước"
  khi xem tổng hợp theo năm), "Chỉ tiêu tăng"/"Không tăng" cũng hiện `—`. Bảng 62 chỉ tiêu: cột "Tuần
  trước" luôn `—`, cột "Tăng/giảm" luôn `—`, cột "Ghi chú tuần" hiện gợi ý xem 1 kỳ cụ thể.
- **Biểu đồ xu hướng ở "Tất cả"** [MỚI vòng #4, hàm mới `renderTrendYearAggregate()`]: vẽ theo TUẦN,
  chỉ trong phạm vi **năm đang chọn** (`periodsInYear(allHistoryYear)`) — khác `renderTrend()` gốc (12
  kỳ gần nhất không giới hạn năm). Vẽ đúng giá trị `weightedProgress` ghi nhận của từng kỳ-tuần (KHÔNG
  áp dụng touched-only ở mức vẽ đường — đây là biểu đồ "lịch sử đã ghi gì", khác KPI trung bình).
- **Thoát "Tất cả"**: chọn 1 tuần cụ thể trong `select#savedWeeks` (qua `loadSavedWeek()`), bấm "Xem"
  ở khu Lịch sử, hoặc chuyển hẳn sang chế độ "Tháng"/"Tuần" — đều set lại `allHistoryMode=false`.
  `allHistoryYear` được **giữ lại** (không reset) để nhớ lựa chọn năm gần nhất khi bật lại "Tất cả".
- **Không ghi dữ liệu** — cùng nguyên tắc read-only của toàn bộ Dashboard, chỉ đọc `historyData`.

### 2.3. Nút "Xuất báo cáo" (đổi tên từ "Báo cáo nhanh") — [MỚI, vòng phản hồi #2]

**Bối cảnh yêu cầu**: người dùng muốn 1 nút "Lưu dữ liệu" quay lại Dashboard
nhưng **không phải nhập liệu** — ý nghĩa mới là "chốt/xuất báo cáo" cho kỳ
đang xem.

**Quyết định UX (`frontend-expert` tự quyết, có lý do)**: **không** thêm 1
nút mới cạnh nút "Báo cáo nhanh" đã có (2 nút cùng mở `dialog#reportDialog`
đứng cạnh nhau sẽ gây rối, người dùng không biết khác nhau ở đâu). Thay vào
đó: **hợp nhất thành đúng 1 nút**, đổi tên `"Báo cáo nhanh"` →
**`"Xuất báo cáo"`** và nâng cấp style thành `.btn.primary` (chiếm lại vị
trí thị giác nổi bật mà nút "Lưu dữ liệu" cũ từng có trong toolbar, dù giờ
nằm trong `weekbar` thay vì `toolbar` — `toolbar` đã bỏ hẳn từ vòng trước).
Lý do đổi tên thay vì giữ nguyên "Lưu dữ liệu": chữ "Lưu" gợi ý ghi dữ liệu
mới — **sai bản chất** trên 1 màn hình đã xác định là read-only; "Xuất báo
cáo" mô tả đúng hành vi (tạo bản tóm tắt để xem/copy/in), không gây hiểu
nhầm là nhập liệu.

- **Hành vi**: gọi `generateReport()` — hàm này **giờ có 3 nhánh** theo
  `viewMode`/`allHistoryMode` hiện tại:
  - `viewMode==='week'` và không bật "Tất cả": hành vi **y hệt "Báo cáo nhanh" cũ**, không đổi
    (dùng `stats()`, so kỳ hiện tại với kỳ liền trước).
  - `viewMode==='month'`: nhánh `generateMonthlyReport()` — nội
    dung report tính theo `monthlyOverallProgress()`/
    `monthlyCriteriaProgress()` (mục 2.2), tiêu đề dialog đổi thành
    "Báo cáo tiến độ DTI — theo tháng" (`h2#reportDialogTitle`).
  - **[MỚI vòng #3]** `viewMode==='week'` và bật "Tất cả": nhánh mới `generateAllHistoryReport()` —
    tiêu đề dialog "Báo cáo tiến độ DTI — tổng hợp toàn bộ lịch sử", nội dung tối giản (số kỳ tổng
    hợp + tiến độ trung bình toàn lịch sử, dùng `aggregateOverallProgress()` mục 2.2b) — không liệt
    kê "chỉ tiêu tăng nhiều"/"chưa tăng" (không có khái niệm delta ở chế độ tổng hợp toàn lịch sử).
- **[ĐÃ ĐỔI vòng #3] Ghim vị trí cố định**: nút nằm trong `.weekbar-actions{margin-left:auto}` — luôn
  ở cuối hàng `weekbar` bất kể số lượng control bên trái đổi (Tuần: `weekDate`+`savedWeeks`; Tháng:
  `yearFilter`+`monthFilterDropdown`) — không còn dịch chuyển vị trí khi chuyển chế độ, đúng yêu cầu
  "nút Xuất báo cáo sẽ nằm cố định phía right chứ không di chuyển trên toolbar".
- **Không ghi bất kỳ dữ liệu nào** — chỉ đọc `historyData` hiện có, tạo
  HTML hiển thị trong dialog rồi mở `showModal()`, đúng yêu cầu "không phải
  lưu giá trị nhập tay". Dialog vẫn có "Sao chép"/"In" như cũ, không đổi.

### 2.4. [MỚI vòng #3] Phân trang bảng "62 chỉ tiêu DTI" — `#tablePagination`

- Thay cho hiện toàn bộ 62 dòng đã lọc trong 1 lần cuộn dọc liên tục: `renderTable()` (cả 3 nhánh
  tuần thường/tháng/Tất cả) giờ đi qua 1 hàm dùng chung `renderTableRows(arr, rowFn)` — cắt `arr` theo
  `tablePage`/`tablePageSize` (mặc định `tablePageSize=20`) trước khi map ra `<tr>`, đồng thời cập
  nhật `#countText` (format `"start–end/filteredTotal chỉ tiêu (tổng 62)"`) và gọi
  `renderTablePagination()` để vẽ control.
- Control giống hệt cơ chế ở `danh-muc-dti.html` (xem `spec/danh-muc-dti/ui-spec.md` mục 6.7) — chọn
  số dòng/trang **10/20/50** (mặc định 20, `frontend-expert` tự chọn giá trị phổ biến) + 2 nút
  "‹ Trước"/"Sau ›" (tự `disabled` ở biên) + "Trang X/Y".
- **KHÔNG có cột sticky** ở bảng này — bảng "62 chỉ tiêu DTI" không có cột hành động Sửa/Xoá (Dashboard
  read-only, không CRUD), nên yêu cầu "cố định cột hành động" không áp dụng cho file này, theo đúng
  hướng dẫn "nếu dashboard.html không có cột hành động thì bỏ qua ý này cho file đó".
- **Reset về trang 1** khi: đổi từ khoá tìm kiếm/nhóm/mức thay đổi/sắp xếp (`onTableFilterChange()` —
  thay `oninput`/`onchange` cũ vốn gọi thẳng `renderTable()`), chuyển `setViewMode()`, đổi
  `yearFilter`/`monthFilterDropdown` (`onYearMonthFilterChange()`), chọn 1 tuần cụ thể hoặc bật "Tất
  cả" (`loadSavedWeek()`/`showAllHistoryAggregate()`).
- Rỗng (`total===0`, kể cả trường hợp gốc "chưa có kỳ nào"): `#tablePagination` render chuỗi rỗng
  (ẩn hẳn control), `tbody` rỗng — giữ đúng hành vi cũ "không có thông báo 'Không tìm thấy' riêng".

**Ghi chú kiến trúc component (khi build Angular)**: các khu vực trên map tự
nhiên thành `components/` dumb riêng (`kpi-summary`, `group-progress-list`,
`trend-chart`, `criteria-table`, `history-list`, `report-dialog`,
`weekbar`, và **mới**: `view-mode-toggle` cho `.segmented` Tuần/Tháng),
còn `pages/dashboard/dashboard.page.ts` đóng vai trò smart — giữ `draft`
state + `viewMode`/`selectedMonth` state (mới), gọi service, điều phối
giữa các component con. `weightedProgress`/`monthlyOverallProgress`... nên
tách thành pure function trong 1 `dti-progress.util.ts` dùng chung giữa
component thay vì lặp trong page.

## 3. Actions

Với mỗi action: điều kiện kích hoạt, hành vi, dữ liệu/entity bị ảnh hưởng.

### 3.1. [ĐÃ ĐỔI HÀNH VI] Hiển thị ngày kỳ đang xem — `input#weekDate` (nay `disabled`, KHÔNG còn `onchange`)

- **Trước đây**: `onchange="loadDraftForDate()"` — chọn ngày bất kỳ để "xem
  trước" 1 kỳ (kể cả kỳ chưa từng lưu, tự động seed dữ liệu). Hàm
  `loadDraftForDate()` **vẫn còn nguyên vẹn trong JS** (không xoá), nhưng
  **không còn phần tử nào gọi nó** — mô tả hành vi gốc dưới đây chỉ mang
  tính lưu trữ lịch sử/tài liệu hoá rule, không còn phản ánh UI hiện tại.
- **Hành vi gốc** (`loadDraftForDate()`, tham khảo lịch sử):
  1. Lấy ngày mới chọn (`weekDate.value`, mặc định hôm nay nếu rỗng).
  2. Nếu đã có kỳ lưu **đúng ngày này** trong `historyData` → nạp nguyên bản
     ghi đó vào `draft` (deep clone).
  3. Nếu chưa có → tìm kỳ **gần nhất trước** ngày này (`previousWeek()`);
     nếu không có kỳ trước nào thì lấy kỳ **mới nhất hiện có**
     (`latestWeek()`); copy `values` của kỳ đó làm giá trị khởi tạo, `notes`
     reset rỗng. Nếu chưa từng có kỳ nào được lưu → seed từ
     `initialProgress` gốc của từng chỉ tiêu (dữ liệu tĩnh trong
     `DTI_ITEMS`).
  4. Lưu `draft` vào `localStorage` (`saveDraft()`), render lại toàn bộ
     (`renderAll()`).
- **Hiện tại**: `input#weekDate` chỉ **hiển thị** ngày của kỳ đang xem
  (`weekDate.value` vẫn được các hàm khác gán đúng — xem mục 2.1), không
  còn tương tác được. Lý do đổi: xem mục 2.1.

### 3.2. Chọn kỳ đã lưu để xem — `select#savedWeeks` (`onchange="onSavedWeekChange(this.value)"` [ĐÃ ĐỔI vòng #3])

- **Điều kiện**: chỉ có tác dụng khi chọn 1 option khác rỗng (danh sách các
  kỳ đã lưu, sort giảm dần theo ngày, hiển thị `dd/mm/yyyy · xx.x%`), cộng thêm option đặc biệt
  **"Tất cả"** (`__ALL__`) — xem mục 2.2b.
- **[ĐÃ ĐỔI vòng #3] `onSavedWeekChange(val)`** tách nhánh trước khi gọi hành vi gốc: `val==='__ALL__'`
  → `showAllHistoryAggregate()` (mục 2.2b); ngược lại → gọi nguyên `loadSavedWeek(val)` như trước.
- **Hành vi** (`loadSavedWeek(date)`): tìm bản ghi trong `historyData` theo
  `date`, nếu có → clone làm `draft` mới, đồng bộ `weekDate.value`, lưu
  draft, **tắt "Tất cả" nếu đang bật** (`allHistoryMode=false`, thêm ở vòng #3), render lại toàn bộ.
  Nút "Xem" ở mỗi dòng trong khu Lịch sử (mục 3.9) gọi cùng hàm này.
- **Dữ liệu ảnh hưởng**: chỉ `draft` — nạp lại để **xem** một kỳ đã lưu.
  Trên Dashboard hiện tại, đây thuần tuý là hành vi **xem**, không còn theo
  sau bởi "sửa rồi lưu lại" (không còn nút lưu ở màn này) — nếu người dùng
  cần chỉnh sửa, phải sang [Danh mục > DTI](../danh-muc-dti/ui-spec.md).

### 3.3. ~~"Tạo tuần mới từ kỳ gần nhất"~~ — [ĐÃ CHUYỂN SANG Danh mục DTI]

- **Đã bỏ khỏi Dashboard**: nút `btn onclick="newFromLatest()"` không còn
  trong `section.weekbar` — đây là hành động **ghi dữ liệu** (chuẩn bị 1
  kỳ mới), không phù hợp màn read-only. Hàm `newFromLatest()` **vẫn còn
  nguyên vẹn trong JS** (không xoá), chỉ không còn phần tử nào gọi.
- **Đã dựng lại ở nơi mới**: `doc/Prototype/danh-muc-dti.html` (lưới duy
  nhất, không còn tab), tên nút "Tạo kỳ mới từ kỳ gần nhất" — xem
  `spec/danh-muc-dti/ui-spec.md` mục 6.3.

### 3.4. ~~Nhập "Tiến độ %" từng chỉ tiêu~~ — [ĐÃ ĐỔI: text tĩnh, ĐÃ CHUYỂN việc nhập sang Danh mục DTI]

- **Đã đổi trên Dashboard**: cột "Tuần này" trong bảng 62 chỉ tiêu giờ hiển
  thị **text tĩnh** `${fmt(v)}%` — không còn `input.progressInput`, không
  còn `onchange="setProgress(id,val)"`. Hàm `setProgress(id,val)` **vẫn còn
  nguyên vẹn trong JS gốc** (không xoá), chỉ không còn phần tử nào gọi.
- **Rule validate gốc** (áp dụng cho luồng nhập tay ở Danh mục DTI, xem
  `spec/danh-muc-dti/ui-spec.md`): ép kẹp giá trị trong khoảng `[0,100]`
  (`Math.max(0,Math.min(100,Number(val)||0))`, giá trị không hợp lệ/rỗng →
  0) — **rule này không đổi bản chất**, chỉ đổi UI nào gọi tới nó (xem
  `spec/dashboard-dti-weekly/business-rules.md` mục 0).
- **Dữ liệu ảnh hưởng (không đổi)**: `draft.values[criteriaId]` → map vào
  `CriteriaAssessment.ProgressPercent` của bản ghi `(CriteriaId, PeriodId)`
  khi lưu (nay lưu từ Danh mục DTI).

### 3.5. ~~Nhập "Ghi chú tuần" từng chỉ tiêu~~ — [ĐÃ ĐỔI: text tĩnh, ĐÃ CHUYỂN việc nhập sang Danh mục DTI]

- **Đã đổi trên Dashboard**: cột "Ghi chú tuần" hiển thị **text tĩnh**
  (`esc(draft.notes[id])`, hoặc `"—"` muted nếu rỗng) — không còn
  `input.noteInput`, không còn `onchange="setNote(id,val)"`. Hàm
  `setNote(id,val)` **vẫn còn nguyên vẹn trong JS gốc**, chỉ không còn phần
  tử nào gọi.
- **Dữ liệu ảnh hưởng (không đổi)**: `draft.notes[criteriaId]` → map vào
  `CriteriaAssessment.Note` khi lưu (nay lưu từ Danh mục DTI).

### 3.6. ~~"Lưu tuần này"/"Lưu dữ liệu" + `.fab`~~ — [ĐÃ BỎ HẲN khỏi Dashboard, ĐÃ CHUYỂN SANG Danh mục DTI]

- **Đã bỏ khỏi Dashboard**: cả `btn.primary onclick="saveWeek()"` (từng ở
  toolbar) lẫn `.fab` "Lưu" (mobile) đã bị xoá khỏi `dashboard.html`. Hàm
  `saveWeek()` **vẫn còn nguyên vẹn trong JS gốc** (không xoá), chỉ không
  còn phần tử nào gọi trên Dashboard.
- **Hành vi gốc** (`saveWeek()`, tham khảo — nay chạy ở Danh mục DTI):
  1. Chốt `draft.date` từ ô ngày (ở Danh mục DTI, ô ngày vẫn tương tác
     được, khác Dashboard).
  2. Deep-clone `draft` thành `snapshot`.
  3. Nếu đã tồn tại 1 bản ghi trong `historyData` có cùng `date` → **ghi
     đè** (upsert theo `date`); ngược lại → thêm mới.
  4. Sort lại `historyData` theo `date` tăng dần, ghi `localStorage`, nạp
     lại `select#savedWeeks`, render toàn bộ.
  5. `alert()` xác nhận đã lưu.
- **Dữ liệu ảnh hưởng (không đổi bản chất)**: **upsert 1 `AssessmentPeriod`**
  theo `PeriodDate`, và **upsert N `CriteriaAssessment`** — xem
  `spec/dashboard-dti-weekly/business-rules.md` mục 4. Tiêu đề section
  Lịch sử "Không ghi đè dữ liệu tuần cũ" vẫn đúng nguyên nghĩa (lưu ngày X
  không đụng kỳ khác).

### 3.7. ~~"Sao lưu" (export)~~ — [ĐÃ BỎ HẲN khỏi UI, cả Dashboard lẫn Danh mục DTI]

- Nút "Sao lưu"/export đã bị bỏ khỏi UI hoàn toàn ở đợt cập nhật trước, và
  **tiếp tục không có mặt** ở kiến trúc 2-trang hiện tại (không dựng lại ở
  Danh mục DTI). Hàm `exportBackup()` **vẫn còn nguyên vẹn trong JS gốc**
  của `dashboard.html`, không còn phần tử nào gọi.
- **✅ Đã chốt (người dùng quyết định)**: không cần UI export ở cả 2 trang,
  trừ khi có yêu cầu mới.

### 3.8. ~~"Import"~~ — [ĐÃ CHUYỂN Ý NGHĨA + CHUYỂN TRANG — nay là Import CSV ở Danh mục DTI, không còn ở Dashboard]

- **Đã bỏ khỏi Dashboard**: `label.btn "Import"` + `input#restoreFile
  type=file accept=.json` (`onchange="restoreBackup(this.files[0])"`) đã bị
  xoá khỏi `dashboard.html` cùng đợt xoá toolbar (mục 3.6). Hàm
  `restoreBackup(file)` **vẫn còn nguyên vẹn trong JS gốc** của
  `dashboard.html`, không còn phần tử nào gọi.
- **Ý nghĩa "Import" đã đổi hoàn toàn** ở nơi mới: `danh-muc-dti.html`
  (lưới duy nhất) có nút **"Import CSV"** — không còn đọc file JSON
  backup như hành vi gốc `restoreBackup()` mô tả dưới đây, mà đọc file
  **`.csv` theo mẫu `doc/ERD/example_db_ver1.csv`**, dùng parser CSV riêng
  (`parseCsv()`) và mapping cột theo
  `spec/danh-muc-dti/business-rules.md` mục 2.2 — xem chi tiết đầy đủ ở
  `spec/danh-muc-dti/ui-spec.md`, không lặp lại ở đây.
- **Hành vi gốc `restoreBackup(file)`** (tham khảo lịch sử, KHÔNG còn là
  hành vi của nút "Import" hiện tại): đọc file JSON qua `FileReader`,
  validate tối thiểu `Array.isArray(x.history)`, ghi đè toàn bộ
  `historyData`/`draft`, `alert` kết quả.

### 3.9. "Xem" từng dòng lịch sử — `btn` trong `.histrow` (`onclick="loadSavedWeek(date)"`)

- Giống hệt hành vi mục 3.2 (dùng chung hàm `loadSavedWeek`), chỉ khác nơi
  kích hoạt (từ danh sách lịch sử thay vì dropdown).

### 3.10. Tìm kiếm theo mã/tên — `input#q` (`oninput="renderTable()"`)

- **Điều kiện**: gõ tự do, filter theo real-time (`oninput`, không cần
  submit).
- **Hành vi**: lọc `DTI_ITEMS` theo chuỗi con (không phân biệt hoa/thường,
  đã `.toLowerCase()`) khớp trong `id + ' ' + name`. Chỉ ảnh hưởng hiển thị
  bảng, không đổi dữ liệu.

### 3.11. Lọc theo nhóm — `select#groupFilter` (`onchange="renderTable()"`)

- Options nạp động lúc `init()` từ 6 nhóm duy nhất xuất hiện trong
  `DTI_ITEMS` (dùng `Map` để khử trùng, giữ nguyên thứ tự xuất hiện đầu
  tiên "1".."6"). Chọn 1 nhóm → chỉ hiện chỉ tiêu thuộc nhóm đó trong bảng.

### 3.12. Lọc theo mức thay đổi — `select#changeFilter` (`onchange="renderTable()"`)

- 4 lựa chọn cố định: **"Chỉ tiêu tăng"** (`delta > 0.001`), **"Không
  tăng"** (`delta` xác định và `|delta| <= 0.001`), **"Giảm"**
  (`delta < -0.001`), **"Hoàn thành"** (`progress >= 99.999`). Lưu ý: các
  điều kiện "tăng/không tăng/giảm" chỉ áp dụng được khi có kỳ trước
  (`deltaOf()` trả `null` nếu chưa có kỳ trước) — nếu chưa có kỳ trước,
  filter theo delta sẽ luôn trả rỗng (không có chỉ tiêu nào khớp `d!==null`
  điều kiện).

### 3.13. Sắp xếp bảng — `select#sortBy` (`onchange="renderTable()"`)

- 3 lựa chọn: **"Theo mã chỉ tiêu"** (mặc định, sort tự nhiên theo `id`,
  dùng `localeCompare(..., {numeric:true})` để `"4.2" < "4.10"` đúng thứ
  tự số), **"Tăng nhiều nhất"** (sort giảm dần theo `delta`, chỉ tiêu chưa
  có delta (`null`) coi như `-999`, rơi xuống cuối), **"Tiến độ thấp
  nhất"** (sort tăng dần theo `progress` hiện tại).

### 3.14. "Xuất báo cáo" ([ĐÃ ĐỔI TÊN] từ "Báo cáo nhanh") — `btn.primary` (`onclick="generateReport()"`)

- **Điều kiện**: luôn khả dụng (kể cả chưa có kỳ trước — phần so sánh sẽ tự
  ẩn). Nằm trong `weekbar`, không còn trong `toolbar` (toolbar đã bỏ hẳn từ
  vòng cập nhật trước) — xem mục 2.3 để biết lý do đổi tên + đổi style
  primary.
- **[MỚI] `generateReport()` giờ rẽ nhánh theo `viewMode`** (xem mục 2.2/2.3
  — chi tiết đầy đủ ở đó, không lặp lại):
  - `viewMode==='week'` (mặc định): **hành vi y hệt bản gốc dưới đây,
    không đổi**.
  - `viewMode==='month'`: gọi `generateMonthlyReport()` — nhánh mới, dùng
    số liệu trung bình tháng thay vì kỳ-tuần.
- **Hành vi nhánh tuần** (`generateReport()`, không đổi): tính lại `stats()`
  (xem mục 6), lấy tối đa 8 chỉ tiêu tăng nhiều nhất (`delta > 0`, sort
  giảm dần) và tối đa 8 chỉ tiêu "chưa tăng cần chú ý" (`delta` xác định,
  `|delta| <= 0.001`, và **chưa** đạt 100% — loại trừ chỉ tiêu đã hoàn
  thành dù không đổi). Render đoạn text HTML vào `#reportBox` gồm: kỳ cập
  nhật, tiến độ chung + so kỳ trước (nếu có), số lượng tăng/không đổi/
  giảm/hoàn thành, danh sách 2 nhóm nói trên. Mở `dialog#reportDialog`
  (`showModal()`).
- **Dữ liệu ảnh hưởng**: chỉ đọc/tổng hợp, không ghi — cả 2 nhánh.

### 3.15. Trong dialog báo cáo: "Đóng" / "Sao chép" / "In"

- **Đóng**: `reportDialog.close()`.
- **Sao chép** (`copyReport()`): `navigator.clipboard.writeText(reportBox.innerText)`,
  `alert` "Đã sao chép báo cáo." khi thành công (không xử lý nhánh lỗi nếu
  Clipboard API bị từ chối quyền — không có `.catch()`).
- **In** (`window.print()`): mở hộp thoại in trình duyệt; có CSS
  `@media print` riêng ẩn topbar/filters/fab/`.no-print` khi in.

## 4. States

Chỉ liệt kê các trạng thái `dashboard.html` **thực sự xử lý** trong JS —
không suy diễn thêm.

- **Empty — chưa có kỳ nào được lưu**:
  - KPI "So với tuần trước" (`#kDelta`) hiện `—`, `#prevLabel` hiện "Chưa có
    kỳ trước".
  - KPI "Không tăng" (`#kFlat`) hiện `—` thay vì số (vì không có kỳ trước để
    so sánh — `s.p` là `null`).
  - Cột "Tuần trước" và "Tăng/giảm" trong bảng hiện `—` cho mọi dòng.
  - Badge trạng thái mọi dòng sẽ là "Đang thực hiện" hoặc "Hoàn thành" (điều
    kiện "Không tăng" cần có `delta` xác định — không kích hoạt khi chưa có
    kỳ trước).
  - Biểu đồ xu hướng (`renderTrend()`): nếu `sortedHistory()` rỗng, vẽ text
    "Lưu ít nhất một kỳ để xem biểu đồ." thay vì đường biểu đồ.
  - Khu Lịch sử (`#history`): nếu rỗng, hiện text muted "Chưa có tuần nào
    được lưu."
  - `select#savedWeeks` chỉ có option mặc định "— Chọn kỳ đã lưu —".
- **[ĐÃ BỎ] Validation khi nhập tiến độ**: không còn ý nghĩa trên Dashboard
  (không còn input để nhập) — rule "kẹp `[0,100]`" vẫn tồn tại nguyên vẹn
  cho luồng nhập tay ở Danh mục DTI (xem mục 3.4 + `spec/danh-muc-dti/ui-spec.md`).
- **[ĐÃ BỎ] Validation khi khôi phục file**: không còn ý nghĩa trên
  Dashboard (không còn nút Import) — validate CSV nay ở Danh mục DTI, khác
  hẳn cơ chế cũ (xem `spec/danh-muc-dti/ui-spec.md`).
- **Kết quả bảng lọc rỗng** (GIỮ NGUYÊN): `#countText` vẫn cập nhật đúng
  "0/62 chỉ tiêu", `tbody` render thành chuỗi rỗng (không có thông báo
  "Không tìm thấy kết quả" riêng — chỉ đơn giản là bảng trống).
- **Không có** (cập nhật cho Dashboard read-only): loading state (toàn bộ
  tính toán đồng bộ trên dữ liệu trong bộ nhớ/localStorage, không có gọi
  mạng nên không có spinner/skeleton nào trong prototype); error state nào
  (không còn action nào có thể lỗi trên màn này — mọi input/import đã
  chuyển sang Danh mục DTI); không có confirm dialog nào (không còn action
  ghi dữ liệu trên Dashboard để cần xác nhận).

## 5. Responsive

Có 3 breakpoint khai báo trong CSS: `@media(max-width:980px)`,
`@media(max-width:560px)`, và `@media print` (không phải responsive theo
kích thước màn hình nhưng cùng nhóm CSS điều kiện, đã mô tả ở mục 3.15).

### ≥980px (desktop mặc định)

- `.kpis`: grid 5 cột đều nhau.
- `.layout` (nhóm + biểu đồ): grid 2 cột tỉ lệ `1.15fr / .85fr`.
- `.group-row`: grid 3 cột **[ĐÃ ĐỔI vòng #3, compact]** `210px (tên nhóm) / 1fr (thanh bar) / 80px
  (số %)` — giảm từ `230px/1fr/90px` theo yêu cầu thu nhỏ mật độ hiển thị.
- `.topbar` chỉ còn logo/title, không còn toolbar hành động nào (đã bỏ
  toàn bộ — xem mục 2/3.6/3.8).

### ≤980px (tablet/mobile)

- `.kpis`: co còn **2 cột**.
- `.layout`: co còn **1 cột** (nhóm và biểu đồ xếp chồng).
- `.group-row`: co còn `140px / 1fr / 75px`.
- **[ĐÃ BỎ] `.fab`**: nút nổi "Lưu" đã bị xoá khỏi `dashboard.html` cùng
  đợt chuyển Dashboard sang read-only — không còn hành động lưu nào ở màn
  này, kể cả trên mobile, nên `.fab` không còn lý do tồn tại. CSS rule
  `.fab{display:none}`/`@media(max-width:980px){.fab{display:block}}` vẫn
  còn trong stylesheet (không dọn) nhưng vô hại vì không còn phần tử
  `.fab` nào trong DOM để match.

### ≤560px (mobile nhỏ)

- `main` và `.topin` giảm padding (`10px`).
- `.logo h1` giảm cỡ chữ còn `16px`.
- `.kpis` giảm gap còn `8px`; `.card` giảm padding còn `12px`;
  `.kpi .value` giảm cỡ chữ còn `22px`.
- **KPI cuối cùng** (`"Hoàn thành 100%"`, `.kpis .card:last-child`) chiếm
  trọn chiều rộng dòng (`grid-column:1/-1`) — vì 5 KPI trên lưới 2 cột dư
  1 ô lẻ, ô cuối được kéo full-width thay vì để trống bên cạnh.
- `.weekbar > *` mỗi phần tử con giãn đều `flex:1` (input ngày [disabled]/
  yearFilter+monthFilterDropdown tuỳ chế độ, select kỳ, `.segmented` Tuần/Tháng) thay vì co theo nội
  dung — đã bỏ nút "Tạo tuần mới" (mục 3.3), thêm `.segmented` (mục 2.2) so với bản trước.
  **[ĐÃ ĐỔI vòng #3]** `.weekbar-actions{margin-left:0;width:100%}` ở breakpoint này — nút "Xuất báo
  cáo" xuống hàng riêng full-width thay vì cố ghim phải (ghim phải chỉ có ý nghĩa ở màn rộng, ≤560px
  ưu tiên đọc được/bấm được hơn giữ đúng vị trí thị giác).
- `.group-row`: co tiếp còn `110px / 1fr / 68px`.
- `.title` đổi `align-items` từ `center` sang `flex-start` (tiêu đề dài
  xuống dòng không bị lệch với phần tử bên phải).
- Bảng 62 chỉ tiêu: **không có breakpoint riêng cho table** — `table` giữ
  `min-width:1200px` ở mọi kích thước màn hình, cuộn ngang qua
  `.tablewrap{overflow-x:auto}` **[ĐÃ ĐỔI vòng #3]** (tường minh hoá từ `overflow:auto`, xác nhận lại
  hoạt động theo phản hồi người dùng — xem `spec/danh-muc-dti/ui-spec.md` mục 6.8). **[MỚI vòng #3]**
  phân trang (`#tablePagination`, mục 2.4) thay cho cuộn dọc liên tục — không có breakpoint riêng,
  control tự `flex-wrap:wrap` khi hẹp.

## 6. Trường dữ liệu hiển thị — map UI ↔ ERD

### 6.1. KPI summary (`section.kpis`)

| UI field | Nguồn tính (JS) | Map ERD |
| --- | --- | --- |
| `#kProgress` "Tiến độ chung tuần này" | `weightedProgress(draft.values)` — bình quân gia quyền theo `maxScore` trên toàn bộ 62 chỉ tiêu của **kỳ hiện tại (draft)** | Tính từ `Σ(CriteriaAssessment.ProgressPercent/100 × Criteria.MaxScore) / Σ(Criteria.MaxScore)` cho `PeriodId` tương ứng — **không lưu cột riêng** |
| `#kDelta` "So với tuần trước" | `stats().delta = cur - prev`, `prev` từ `weightedProgress()` của kỳ liền trước (`previousWeek(draft.date)`) | Tính từ hiệu 2 giá trị `weightedProgress` ở 2 `AssessmentPeriod` khác nhau (kỳ hiện tại vs kỳ có `PeriodDate` lớn nhất nhỏ hơn kỳ hiện tại) |
| `#prevLabel` | ngày của kỳ trước (`s.p.date`) hoặc "Chưa có kỳ trước" | `AssessmentPeriod.PeriodDate` của kỳ liền trước |
| `#kUp` "Chỉ tiêu tăng" | đếm số chỉ tiêu có `delta > 0.001` | đếm `CriteriaAssessment` có `ProgressPercent` kỳ này > kỳ trước (theo cùng `CriteriaId`) |
| `#kFlat` "Không tăng" | đếm số chỉ tiêu có `delta` xác định và `|delta| <= 0.001` (hiện `—` nếu chưa có kỳ trước) | tương tự, `ProgressPercent` không đổi giữa 2 kỳ |
| `#kDone` "Hoàn thành 100%" | đếm số chỉ tiêu có `progress >= 99.999` trên tổng 62 | đếm `CriteriaAssessment.ProgressPercent >= 99.999` của kỳ hiện tại / tổng số `Criteria` active |

### 6.2. Tiến độ theo nhóm (`#groups`)

| UI field | Nguồn (JS) | Map ERD |
| --- | --- | --- |
| Tên nhóm (`arr[0].groupName`, tiền tố mã nhóm) | `DTI_ITEMS[].group` + `.groupName` | `CriteriaGroup.Code` + `CriteriaGroup.Name` |
| Thanh tiến độ nhóm (`.fill` width %) + số `%` | `Σ(maxScore × progress/100)/Σ(maxScore)` trong phạm vi các chỉ tiêu cùng `group` | tính từ `CriteriaAssessment` join `Criteria` lọc theo `GroupId`, cùng công thức 6.1 nhưng scope theo nhóm |

### 6.3. Biểu đồ xu hướng (`canvas#trend`)

| UI field | Nguồn (JS) | Map ERD |
| --- | --- | --- |
| Trục X (nhãn ngày, tối đa 12 điểm gần nhất) | `sortedHistory().slice(-12)` → `dateVN(x.date)` | `AssessmentPeriod.PeriodDate`, 12 kỳ gần nhất theo thứ tự tăng dần |
| Trục Y / điểm dữ liệu (đường line) | `weightedProgress(x.values)` từng kỳ trong lịch sử | tính lại `weightedProgress` cho mỗi `AssessmentPeriod` đã lưu (giống 6.1, không cache) |

### 6.4. Bảng 62 chỉ tiêu (`#tbody`) — mỗi cột

| Cột UI | Nguồn (JS) | Map ERD |
| --- | --- | --- |
| "Mã" | `DTI_ITEMS[].id` | `Criteria.Code` |
| "Chỉ tiêu" | `DTI_ITEMS[].name` | `Criteria.Name` |
| "Nhóm" (`group. groupName`) | `DTI_ITEMS[].group` + `.groupName` | `Criteria.GroupId` → `CriteriaGroup.Code`/`.Name` |
| "Điểm tối đa" | `DTI_ITEMS[].maxScore` | `Criteria.MaxScore` |
| "Tuần trước" (`.num`) | `prevValue(id)` — `draft.values` không có, đọc từ kỳ trước | `CriteriaAssessment.ProgressPercent` của kỳ liền trước, cùng `CriteriaId` |
| "Tuần này" ([ĐÃ ĐỔI] text tĩnh, trước là `input.progressInput`) | `valueOf(id) = draft.values[id]` — chỉ đọc trên Dashboard, nhập tay chuyển sang Danh mục DTI (mục 3.4) | `CriteriaAssessment.ProgressPercent` của kỳ hiện tại |
| "Tăng/giảm" (`.delta`) | `deltaOf(id) = valueOf(id) - prevValue(id)`, `null` nếu chưa có kỳ trước | **tính toán**, không lưu cột — hiệu `ProgressPercent` giữa 2 `CriteriaAssessment` cùng `CriteriaId`, khác `PeriodId` |
| "Trạng thái" (`.badge`) | `statusFor(v, d)` — **BADGE TÍNH ĐỘNG**, xem mục 6.5 | **KHÔNG map trực tiếp vào `CriteriaAssessment.Status`** — xem ghi chú quan trọng bên dưới |
| "Ghi chú tuần" ([ĐÃ ĐỔI] text tĩnh, trước là `input.noteInput`) | `draft.notes[id]` — chỉ đọc trên Dashboard, nhập tay chuyển sang Danh mục DTI (mục 3.5) | `CriteriaAssessment.Note` |

**Ghi chú quan trọng — 2 khái niệm "trạng thái" KHÁC NHAU, đừng nhầm lẫn
khi implement:**

1. **`CriteriaAssessment.Status`** (theo ERD/CSV) — trường **nhập tay, lưu
   DB**, 4 giá trị quan sát được trong `example_db_ver1.csv`: `"Chưa thực
   hiện"`, `"Đang thực hiện"`, `"Cần bổ sung minh chứng"`, `"Hoàn thành"`.
   Đây là đánh giá **định tính do con người chốt** khi thẩm định, độc lập
   với % tiến độ.
2. **Badge hiển thị trong `dashboard.html`** (cột "Trạng thái" trong bảng
   62 chỉ tiêu) — là giá trị **tính toán tại runtime từ hàm `statusFor(v,
   d)`**, KHÔNG đọc từ trường `Status` lưu DB nào cả (prototype không hề có
   khái niệm field `Status` — JS chỉ có `DTI_ITEMS` tĩnh và `draft.values`
   là % tiến độ). Logic:
   - `progress >= 99.999` → **"Hoàn thành"** (class `bdone`, xanh)
   - ngược lại, nếu có kỳ trước và `delta <= 0.001` → **"Không tăng"**
     (class `bstall`, đỏ)
   - còn lại → **"Đang thực hiện"** (class `bwork`, cam)
   - Chỉ 3 giá trị, **không có** "Chưa thực hiện" hay "Cần bổ sung minh
     chứng" trong badge này.

   **✅ Đã chốt (người dùng quyết định)**: cột "Trạng thái" trong bảng dashboard
   tuần vẫn giữ nguyên là **badge tính động** (`statusFor`) như prototype —
   `CriteriaAssessment.Status` (4 giá trị nhập tay) đến từ một **quy trình
   thẩm định riêng** (màn hình khác, chưa có prototype), **không** thuộc
   phạm vi màn hình dashboard tuần đang thiết kế. **Không cần thêm control
   nhập `Status` mới** vào bảng 62 chỉ tiêu ở Phase 3 (Figma) — giữ đúng 8
   cột như `dashboard.html` hiện có. Cùng quyết định áp dụng cho
   `SelfScore`/`VerifiedScore`: tĩnh, chỉ từ quy trình thẩm định riêng,
   không cần ô nhập trong dashboard tuần.

### 6.5. Lịch sử các kỳ đã lưu (`#history`)

| UI field | Nguồn (JS) | Map ERD |
| --- | --- | --- |
| Ngày kỳ (`dateVN(x.date)`) | `historyData[].date` | `AssessmentPeriod.PeriodDate` |
| "Tiến độ chung" | `weightedProgress(x.values)` | tính lại theo 6.1 cho từng `AssessmentPeriod` |
| Delta so kỳ liền trước kỳ đó | so `weightedProgress` của kỳ đó với kỳ liền trước nó trong `sortedHistory()` | tương tự 6.1, scope là cặp kỳ liên tiếp trong lịch sử (không phải so với kỳ hiện tại đang xem) |
| Nút "Xem" | `loadSavedWeek(x.date)` | không ghi dữ liệu, chỉ nạp lại `draft` |

### 6.6. Trường KHÔNG xuất hiện trong `dashboard.html` (đã xác nhận `CriteriaEvidence`, `Owner`, `Deadline`)

`dashboard.html` **hoàn toàn không có UI** cho: danh sách minh chứng
(`CriteriaEvidence`), người phụ trách (`Owner`), hạn xử lý (`Deadline`).
Cả 3 field/entity này chỉ có nguồn từ CSV, không có bằng chứng hành vi UI
tương ứng trong prototype — khớp với ghi nhận đã có sẵn ở
`doc/ERD/ERD.md` (câu hỏi còn mở #1 và #4, nay đã chốt giữ `Owner`/
`Deadline` theo từng kỳ trên `CriteriaAssessment`). Spec này xác nhận lại:
**không cần thiết kế UI cho 3 field này ở phạm vi "màn hình dashboard
tuần"** hiện tại — nếu cần, sẽ là màn hình/khu vực riêng (chi tiết 1 chỉ
tiêu) chưa có trong prototype.

**Cập nhật quan trọng cho màn hình tương lai**: theo `backend-expert`
(`doc/ERD/ERD.md` mục "Quyết định đã CHỐT" #3), auth đã chốt dùng ASP.NET
Core Identity — `CriteriaAssessment.Owner` không còn là text tự do mà đổi
thành `OwnerId` (FK → `AppUser.Id`). Không ảnh hưởng UI dashboard tuần hiện
tại (vẫn không có control nào cho field này ở đây), nhưng nếu sau này
thiết kế màn "chi tiết 1 chỉ tiêu" có ô chọn người phụ trách, đó phải là
**dropdown/autocomplete chọn `AppUser`** (theo `FullName`/`UserName`),
**không phải input text tự do** như suy đoán ban đầu.

## 7. Style thô (tham khảo — CHƯA phải token đã export)

Vì `doc/Design/` chưa chạy tới bước extract-tokens, ghi lại nguyên giá trị
CSS thô từ `:root` để dùng tạm khi build, và cần thay bằng token thật ngay
khi `doc/Design/Frontend/PlatformManager/Tokens/` có:

```
--bg:#f3f6fb        nền trang
--card:#fff         nền card
--text:#152033       chữ chính
--muted:#6d788b       chữ phụ/label
--line:#dfe6ef        viền
--brand:#0f5bd7       màu chính (nút primary, thanh tiến độ, biểu đồ)
--brand2:#174ca8       (dùng làm màu hover cho `.btn.primary` — bổ sung ở đợt polish CSS gần nhất)
--good:#14855b        tăng/hoàn thành
--warn:#c07a00         đang thực hiện/không tăng (theo ngữ cảnh)
--bad:#c83c3c          giảm
--shadow: 0 7px 24px rgba(23,39,67,.08)
```

Font: `Inter, Segoe UI, Arial, sans-serif`. Bo góc chủ đạo: `10px-15px`
tuỳ thành phần (button `10px`, card `14px`, dialog `15px`).

**[MỚI vòng #3] Token mật độ hiển thị (compact)** — bổ sung vào `:root`, giống hệt
`danh-muc-dti.html` để 2 trang đồng bộ 1 hệ mật độ (xem `spec/danh-muc-dti/ui-spec.md` mục 9 cho bảng
giá trị đầy đủ `--fs-*`/`--sp-*`/`--radius-*`/`--sidebar-w*`). Không đổi bảng màu ở trên. Riêng
`.kpi .value` giảm từ `27px` xuống **21px** (≤560px: xuống tiếp **18px**, từ `22px` cũ) — do đây là
con số nổi bật nhất trang, không dùng chung thang `--fs-*` (vẫn giữ hardcode cho phù hợp phân cấp thị
giác, nhưng đã giảm theo tinh thần compact chung).

## 8. Câu hỏi còn mở

Đã gửi trực tiếp cho `backend-expert` qua `SendMessage` (không chỉ ghi ở
đây) vì họ đang viết `business-rules.md` song song. `backend-expert` đã
phản hồi và xác nhận khớp — xem `spec/dashboard-dti-weekly/business-rules.md`
mục 5 và mục 8 (câu hỏi mở tương ứng phía backend).

### Đã chốt (người dùng quyết định)

1. ✅ **Cột "Trạng thái" trong UI** — giữ nguyên badge tính động
   (`statusFor`, 3 giá trị) như prototype. `CriteriaAssessment.Status`
   (4 giá trị nhập tay) đến từ quy trình thẩm định riêng, **không** cần
   control trong dashboard tuần — xem mục 6.4.
2. ✅ **`Owner`/`Deadline`** — đã chốt giữ theo từng kỳ trên
   `CriteriaAssessment` (`Owner` đổi thành `OwnerId` FK → `AppUser`, xem
   mục 6.6). UI dashboard tuần hiện tại **không** cần hiển thị/nhập 2 field
   này (không có bằng chứng UI) — nếu tương lai có màn hình "chi tiết 1 chỉ
   tiêu", `OwnerId` sẽ cần UI dạng dropdown/autocomplete chọn `AppUser`,
   không phải input text.
3. ✅ **`CriteriaEvidence`** — tương tự, không cần UI ở slice dashboard tuần
   này; có chủ đích, không phải bỏ sót.
4. ✅ **"Sao lưu"/"Khôi phục"** (export/import) — **cập nhật (đợt kiến trúc
   2-trang mới nhất, ghi đè mọi quyết định cũ ở dòng này)**: nút **"Sao
   lưu"/export đã bị bỏ khỏi UI hoàn toàn**, cả 2 trang (không dựng ở đâu,
   hàm `exportBackup()` chỉ giữ trong JS gốc phòng khi cần dùng lại — xem
   mục 3.7). Nút **"Khôi phục"/"Import"** đã **rời khỏi Dashboard hoàn
   toàn** và đổi ý nghĩa triệt để: không còn đọc JSON backup, mà là **Import
   CSV** theo mẫu `example_db_ver1.csv`, sống trong lưới duy nhất của
   `danh-muc-dti.html` (không còn cấu trúc tab từ vòng phản hồi #2) — xem
   mục 3.8 và `spec/danh-muc-dti/ui-spec.md`.
5. ✅ **Dashboard đổi thành read-only** (mới nhất, xem banner đầu file +
   `spec/dashboard-dti-weekly/business-rules.md` mục 0): toàn bộ khả năng
   nhập liệu (progress/note/lưu/tạo tuần mới/import) đã chuyển sang
   [Danh mục > DTI](../danh-muc-dti/ui-spec.md). Dashboard chỉ giữ vai trò
   xem/báo cáo. Sidebar cập nhật thêm nav item "Danh mục" > "DTI" — xem
   `spec/sidebar-menu/ui-spec.md`.
6. ✅ **[MỚI vòng #4] KHÔNG carry-forward + "Tất cả" = theo NĂM** — xem banner đầu file (mục "CẬP NHẬT
   VÒNG PHẢN HỒI #4") và mục 2.2b — công thức trung bình Tháng/"Tất cả" loại trừ kỳ không có thao tác
   khỏi mẫu tính (field `touched`), "Tất cả" luôn cần 1 năm làm phạm vi nền tảng.
7. ✅ **[MỚI vòng #4] Rule read-only khi Danh mục DTI xem lịch sử** — không ảnh hưởng UI Dashboard
   (Dashboard vốn đã 100% read-only), chỉ áp dụng cho `danh-muc-dti.html` — xem
   `spec/danh-muc-dti/ui-spec.md` mục 6.10.

### Còn mở (không chặn Phase 3 — không ảnh hưởng hình dạng UI dashboard tuần)

5. **Permission/vai trò** (từ `backend-expert` — `business-rules.md`
   câu hỏi mở #4): `dashboard.html` không có login/role nào, nên spec này
   **không giả định** bất kỳ hành vi ẩn/hiện nút hay khoá field theo vai
   trò nào (vd ai được sửa `Status`/điểm số/`OwnerId`) — toàn bộ action ở
   mục 3 mô tả đúng như prototype: mọi người dùng đã đăng nhập đều thấy và
   dùng được tất cả. Nếu sau này chốt vai trò Identity cụ thể, cần bổ sung
   riêng một mục "Phân quyền" vào spec này, không tự suy diễn trước.

# UI Spec — "Danh mục > DTI" (1 lưới: danh mục Chỉ tiêu + Đánh giá theo tuần)

> ## ⚠️ CẬP NHẬT (2026-08-12, vòng phản hồi #5 — ĐÃ CHỐT) — gói bảng + phân trang trong 1 màn hình, sửa xung đột sticky 2 chiều
>
> Vấn đề người dùng báo: `#gridPagination` nằm ở cuối bảng 62 dòng — với bảng cao hơn viewport, người
> dùng phải **cuộn cả trang** xuống mới thấy được nút phân trang. Đã sửa trong
> `doc/Prototype/danh-muc-dti.html`, chi tiết đầy đủ ở mục 6.11 (mới) — tóm tắt:
>
> 1. **`#dtiGridCard`** (card chứa "Danh mục & Đánh giá theo tuần") đổi sang **flex column**, chiều cao
>    **CỐ ĐỊNH** (`height`, không phải `max-height`) **tính động bằng JS** (`updateGridCardHeight()`, đo
>    `getBoundingClientRect().top` thực tế thay vì hardcode `calc(100vh - Npx)` — vì phần phía trên card
>    đổi chiều cao tuỳ độ rộng màn hình). `.tablewrap` là **flex item duy nhất `flex:1;min-height:0`**,
>    chiếm hết phần còn lại của card; `.pagination` neo cố định cuối card, **luôn nhìn thấy** không cần
>    cuộn trang. **[FIX cùng ngày, sau khi người dùng gửi screenshot]** ban đầu dùng `max-height` — khiến
>    card TỰ CO LẠI theo nội dung ở trang cuối/ít dòng (vd 2 dòng còn lại), đẩy `.pagination` lên sát
>    ngay dưới dữ liệu, để lại khoảng trắng lớn trước footer, vị trí nhảy lên/xuống tuỳ số dòng. Đổi
>    sang `height` cố định: card LUÔN chiếm đúng chiều cao đã đo được bất kể `.tablewrap` có ít hay nhiều
>    dòng — khi ít dòng, `.tablewrap` (đã `flex:1;min-height:0`) tự giãn chiếm khoảng trống thừa thay vì
>    card co lại, giữ `.pagination` luôn neo đúng 1 vị trí cố định ở đáy card (giống hệt layout khi đủ
>    dòng). Hàm cũng đổi tên `updateGridCardMaxHeight()` → **`updateGridCardHeight()`** cho khớp ngữ
>    nghĩa mới.
> 2. **`.tablewrap{overflow-y:auto}`** (đổi từ `visible`) — chỉ khi dữ liệu 1 trang (tối đa 50 dòng theo
>    lựa chọn phân trang) cao hơn phần không gian còn lại, **chỉ riêng phần thân bảng** mới có scrollbar
>    dọc riêng, KHÔNG phải cuộn cả trang web. `<thead>` vẫn `position:sticky;top:0` (đã có sẵn), nay thực
>    sự phát huy tác dụng dính trên khi cuộn dọc **bên trong** `.tablewrap` (trước đây scroll dọc xảy ra ở
>    cấp trang nên sticky top chỉ dính dưới `.topbar`, không phải trong khung bảng).
> 3. **Sửa xung đột z-index sticky 2 chiều** (header dính trên + cột Mã/Hành động dính trái/phải, phát
>    sinh khi bật scroll dọc thật bên trong `.tablewrap`): trước đây cột "Hành động" (`z-index:3`) VÔ
>    TÌNH cao hơn header thường (`z-index:2`) → khi cuộn dọc+ngang đồng thời, cột "Hành động" của các
>    dòng cuộn lên đè xuyên thấu lên trên header. Tier chuẩn mới: **góc header `z-index:5`** (không đổi)
>    **> header thường `z-index:4`** (tăng từ 2) **> cột sticky body trái/phải `z-index:2`** (cột "Hành
>    động" giảm từ 3 xuống 2, ngang cột "Mã") **> ô thường** (mặc định).
> 4. Các phần khác (banner hướng dẫn ngoài card, `.filters`, heading) **giữ nguyên vị trí bình thường**,
>    không nằm trong vùng cuộn — chỉ đánh dấu `flex:none` bên trong `#dtiGridCard` để nhường không gian
>    cho `.tablewrap`.
> 5. `@media print{#dtiGridCard{height:auto!important;max-height:none!important}}` — tắt hẳn giới hạn
>    chiều cao khi in (`height:auto` reset về chiều cao tự nhiên, `max-height:none` giữ lại phòng hờ),
>    tránh cắt mất dòng (đi cùng `.tablewrap{overflow:visible}` đã có sẵn cho print).
>
> **Vì sao dùng JS đo động thay vì `calc(100vh - Npx)` cố định như gợi ý ban đầu**: banner hướng dẫn
> phía trên card (`.notice` ngoài `#dtiGridCard`) xuống dòng số lượng khác nhau tuỳ độ rộng màn hình
> (nhiều dòng hơn ở `≤560px`), nên 1 con số px cố định sẽ đúng ở màn hình này nhưng sai ở màn hình khác.
> `updateGridCardHeight()` đo `card.getBoundingClientRect().top` (vị trí thực tế đã bao gồm mọi thứ
> phía trên, bất kể cao bao nhiêu) trừ cho `window.innerHeight`, có sàn an toàn `minHeight:280px` cho
> viewport rất thấp — chạy lại khi `resize`/`orientationchange`. **Không cần** chạy lại mỗi lần
> `renderGrid()` vì banner đọc-only `#dtiReadonlyBanner` nằm BÊN TRONG card, ẩn/hiện không đổi
> `offsetTop` của card.
>
> **Đã áp dụng thống nhất cho mọi breakpoint** (không tắt cơ chế ở mobile) — quyết định của
> `frontend-expert`: cơ chế đo động tự thích ứng mọi độ rộng nên không có rủi ro vỡ layout riêng cho
> mobile; hơn nữa vấn đề gốc (phải cuộn cả trang mới thấy phân trang) càng nghiêm trọng hơn trên mobile
> (viewport thấp hơn, thường có thanh trình duyệt che thêm) nên giữ cơ chế bật xuyên suốt càng có lợi.
>
> `dashboard.html` (bảng 62 chỉ tiêu DTI) **CHƯA áp dụng** cơ chế này ở vòng này — xem ghi chú cuối mục
> 6.11 lý do và điều kiện nếu áp dụng sau.

> ## ⚠️ CẬP NHẬT (2026-08-12, vòng phản hồi #4 — ĐÃ CHỐT CHÍNH THỨC) — bộ lọc Năm/Kỳ, "Tất cả trong 1 năm", KHÔNG carry-forward, RULE READ-ONLY khi xem lịch sử
>
> Người dùng đã chốt toàn bộ 4 câu hỏi mở nêu ở banner vòng #3 (bên dưới) qua `backend-expert`. Đây là
> **bản chốt cuối cùng**, ghi đè mọi suy đoán/TODO trước đó về "Tất cả"/carry-forward/read-only:
>
> 1. **KHÔNG carry-forward** — 1 kỳ không có thao tác (sửa/Import) cho 1 chỉ tiêu thì **loại hẳn** khỏi
>    mọi phép tính trung bình liên quan tới chỉ tiêu đó (không tính là giá trị cũ, không tính 0%). Cơ
>    chế: mỗi period (`draft`/`historyData[]` entry) có thêm field `touched` (map `{code:true}`) đánh
>    dấu chỉ tiêu nào **thực sự** được sửa/import trong CHÍNH kỳ đó — `blankValuesFromCatalog()` không
>    bao giờ copy `touched` từ `seedFrom`, chỉ `confirmEditCell()`/`importCsvFile()` mới ghi vào đây.
> 2. **"Tất cả" = toàn bộ dữ liệu trong 1 NĂM được chọn** — Danh mục DTI có thêm `select#dtiYearFilter`
>    (chọn năm) + `select#dtiPeriodFilter` ("Tất cả (mới nhất trong năm)" hoặc 1 kỳ cụ thể đã lưu trong
>    năm đó). Mặc định "Tất cả" hiển thị **giá trị mới nhất theo touched-tracking** của mỗi chỉ tiêu
>    trong năm đang chọn (KHÔNG phải carry-forward — "mới nhất" nghĩa là kỳ gần nhất có `touched[code]`,
>    bỏ qua các kỳ chỉ mang giá trị kế thừa). Xem mục 6.9 (mới).
> 3. **RULE READ-ONLY khi xem lịch sử — đã CHỐT chính thức** (không còn là đề xuất chờ xác nhận): grid
>    chỉ cho sửa (✓/✗ inline, Import CSV, "+Thêm chỉ tiêu"/Sửa/Xoá) khi đang xem đúng **"Tất cả" của
>    NĂM HIỆN TẠI** — mọi trạng thái khác (năm khác, hoặc đã thu hẹp về 1 kỳ cụ thể dù trong năm nay)
>    đều **CHỈ ĐỌC**, kèm banner `#dtiReadonlyBanner` giải thích. Lý do: việc ghi luôn nhắm vào "hôm
>    nay" bất kể đang xem gì — sửa khi đang xem dữ liệu quá khứ sẽ ghi nhầm vào sai ngày. Xem mục 6.10
>    (mới).
> 4. **Tuần ISO (Thứ 2–CN)** và **không copy-forward `CriteriaEvidence`** — xác nhận đúng thiết kế cũ,
>    không có thay đổi UI nào (rule dữ liệu thuần backend).
>
> Nguồn nghiệp vụ đầy đủ: `spec/danh-muc-dti/business-rules.md` mục 2.4/3 (bản viết lại của
> `backend-expert`) — spec UI này không lặp lại công thức, chỉ mô tả UI/UX + trỏ ngược khi cần.
>
> **Lưu ý quan trọng khi lên app thật**: "Tất cả" của 1 năm phía backend có thể trả về **nhiều bản ghi
> `CriteriaAssessment`/1 `Criteria`** (mỗi bản ghi = 1 lần có thao tác trong năm), khác với giả định "1
> dòng lưới = 1 chỉ tiêu = 1 giá trị" hiện tại. Quyết định UX của bản prototype này: **grid vẫn hiển
> thị đúng 62 dòng (1 dòng/chỉ tiêu)**, mỗi dòng lấy giá trị của lần thao tác **gần nhất trong năm**
> (không liệt kê phẳng từng lần sửa) — nếu sau này cần xem đầy đủ lịch sử chỉnh sửa của 1 chỉ tiêu
> trong năm, đó là 1 màn hình/khu vực "chi tiết chỉ tiêu" riêng, chưa có trong scope hiện tại.
>
> **Cập nhật (2026-08-12, vòng phản hồi #3 — mật độ hiển thị + phân trang + toolbar)**: người dùng
> yêu cầu 4 nhóm điều chỉnh UI/UX bổ sung, tất cả đã áp dụng trong `doc/Prototype/danh-muc-dti.html`:
> 1. **Mật độ hiển thị (compact)** — giảm font-size chung, kích thước sidebar, kích thước mọi nút
>    (kể cả Sửa/Xoá/✓/✗) qua 1 bộ token mới trong `:root` (`--fs-*`, `--sp-*`, `--radius-*`,
>    `--sidebar-w*`) — không đổi màu/token thương hiệu sẵn có. Xem mục 9 (Style thô) cập nhật.
> 2. **Phân trang lưới** (10/20/50 dòng/trang, mặc định 20) thay cho hiện toàn bộ chỉ tiêu cuộn dọc
>    liên tục — xem mục 6.7 (mới).
> 3. **Cuộn ngang** `.tablewrap{overflow-x:auto}` được xác nhận lại + **cột "Mã" sticky trái** và
>    **cột "Hành động" sticky phải** (luôn thấy nút Sửa/Xoá khi cuộn ngang) — xem mục 6.8 (mới).
> 4. **Bỏ hẳn "Tạo kỳ mới từ kỳ gần nhất" + label "Đang sửa kỳ:" + dropdown chọn kỳ** — kỳ sẽ được
>    xác định theo `createdDate` thay vì tách kỳ riêng (mô hình dữ liệu do `backend-expert` chốt song
>    song, chưa ảnh hưởng slice UI này). **"Import CSV"/"+ Thêm chỉ tiêu" chuyển vào chung 1 hàng với
>    bộ lọc** (`input#gq`/`select#gGroupFilter`), ghim cố định bên phải qua
>    `.filters-actions{margin-left:auto}`. Xem mục 4 (Layout) và mục 6.2 (đã đổi) cập nhật đầy đủ.
>
> Các mục dưới đây (bản chốt vòng #2) **vẫn còn hiệu lực** trừ khi bị đánh dấu rõ **"[ĐÃ ĐỔI vòng #3]"**
> tại từng chỗ cụ thể — không viết lại toàn bộ tài liệu để tránh trùng lặp.
>
> **Xác nhận từ `backend-expert` (cùng ngày, sau khi UI đã gỡ)**: việc bỏ hẳn dropdown "Đang sửa
> kỳ"/nút "Tạo kỳ mới" ở mục 4 dưới đây **khớp đúng hướng model dữ liệu mới** — không còn entity
> `AssessmentPeriod` tường minh, `CriteriaAssessment.CreatedAt` (cột audit chuẩn) nay **chính là**
> "kỳ": mỗi lần sửa 1 ô/Import chỉ tác động **hôm nay** (update nếu đã có record hôm nay, tự
> "copy-forward" 7 field từ record gần nhất rồi tạo mới nếu chưa có) — cơ chế server-side này thay
> thế hoàn toàn ý nghĩa "Tạo kỳ mới", không cần hành động UI nào nữa. Xem `doc/ERD/ERD.md` mục "Kỳ
> (tuần/tháng/năm) — khái niệm ngầm định" và `spec/danh-muc-dti/business-rules.md` mục 2.1/2.3 (do
> `backend-expert` viết) để biết chi tiết đầy đủ — spec UI này không lặp lại rule dữ liệu, chỉ xác
> nhận UI đã đúng hướng. **[ĐÃ CHỐT — xem banner vòng #4 phía trên]** 4 câu hỏi mở (#11–14 trong
> `business-rules.md`: định nghĩa "Tất cả", quy ước tuần ISO, carry-forward, copy-forward
> `CriteriaEvidence`) đã được người dùng chốt toàn bộ — không còn là câu hỏi mở.
>
> **Cập nhật (2026-08-12, vòng phản hồi #2 — BẢN CHỐT)**: người dùng yêu cầu
> bỏ hẳn cấu trúc 2 tab ("Chỉ tiêu" / "Đánh giá theo tuần") ở vòng đầu, gộp
> lại thành **đúng 1 lưới (bảng) duy nhất** — mỗi hàng là 1 chỉ tiêu, vừa
> hiển thị thông tin danh mục (Mã/Tên/Nhóm/Điểm tối đa) vừa hiển thị dữ liệu
> đánh giá của **kỳ đang sửa** (Tự đánh giá/Thẩm định/Trạng thái/Phụ trách/
> Hạn xử lý — chỉ đọc, từ Import; Tiến độ %/Ghi chú — **sửa trực tiếp
> inline** ngay trong ô). Toàn bộ nội dung dưới đây phản ánh đúng cấu trúc
> **1 lưới** đã hiện thực ở `doc/Prototype/danh-muc-dti.html` — **không còn
> khái niệm tab nào** trong file.
>
> Nguồn: `doc/Prototype/danh-muc-dti.html` (đọc toàn bộ CSS + 2 khối
> `<script>`), đối chiếu `spec/danh-muc-dti/business-rules.md` (nguồn
> nghiệp vụ đầy đủ, spec này **không lặp lại rule**, chỉ mô tả UI/UX và trỏ
> ngược khi cần), `doc/ERD/ERD.md`, `doc/ERD/example_db_ver1.csv`.
>
> **Bối cảnh phát sinh màn hình này**: Dashboard (`dashboard.html`) là
> **read-only** — toàn bộ khả năng nhập liệu (CRUD danh mục chỉ tiêu, nhập/
> import đánh giá theo tuần) tập trung **duy nhất** ở màn hình này. Cùng
> sidebar shell với Dashboard (xem `spec/sidebar-menu/ui-spec.md` — nav item
> "Danh mục" > "DTI"). Token màu/font/bo góc/shadow **tái dùng 100%** từ
> `:root` của `dashboard.html`.

## 1. Tổng quan

- **Mục đích**: 1 màn hình, **1 lưới**, gộp toàn bộ nhu cầu nhập liệu DTI:
  - **Import CSV** (cách nhập **chính**) — nạp toàn bộ danh mục + đánh giá
    cho 1 kỳ từ file theo mẫu `example_db_ver1.csv`.
  - **"+ Thêm chỉ tiêu" / "Sửa" / "Xoá"** — CRUD danh mục `Criteria`
    (Mã/Tên/Nhóm/Điểm tối đa) qua dialog.
  - **Sửa trực tiếp trong lưới** (edit-per-cell) — chỉnh "Tiến độ %"/"Ghi
    chú" của kỳ đang sửa, không cần mở form riêng.
- **Vì sao cột "Tiến độ %" nằm trong lưới này dù không có trong CSV mẫu**:
  `example_db_ver1.csv` không có cột % tiến độ tuần (chỉ có "Tự đánh giá"/
  "Thẩm định" theo điểm số) — `ProgressPercent` là khái niệm chỉ tồn tại
  trong logic JS gốc của `dashboard.html` (`DTI_ITEMS[].initialProgress`,
  `draft.values`). Vì màn này giờ là nơi **duy nhất** để nhập/sửa dữ liệu
  (Dashboard chỉ đọc), "Tiến độ %" **phải** có mặt và **sửa được** ở đây —
  đúng yêu cầu người dùng.
- **Người dùng**: giống Dashboard — cán bộ chuyên trách/đầu mối CĐS, không
  có phân quyền trong prototype (permission CRUD `Criteria` còn là câu hỏi
  mở, xem `business-rules.md` mục 4/5 câu #7).
- **Lưu trữ ở bản prototype**: 100% phía client, `localStorage`, **3 key**
  (không đổi so với thiết kế trước, chỉ đổi UI phía trên chúng):
  - `dti_weekly_history_v2` / `dti_weekly_draft_v2` — **DÙNG CHUNG với
    `dashboard.html`**. Mọi thay đổi ở lưới này (Import, sửa inline, "Tạo
    kỳ mới") ghi thẳng vào đây, nên hiển thị ngay khi mở lại Dashboard.
  - `platform_manager_criteria_catalog_v1` — danh mục `Criteria` (CRUD ở
    lưới này), seed lần đầu từ 62 dòng `doc/ERD/example_db_ver1.csv`.
  - Object period (`historyData[]`/`draft`) mở rộng thêm `selfScores`,
    `verifiedScores`, `statuses`, `owners`, `deadlines`, `evidences` —
    tương thích ngược với `dashboard.html` (chỉ đọc `.values`/`.notes`).
- **Không còn nút "Lưu dữ liệu" tổng nào** — quyết định UX quan trọng nhất
  của bản chốt này, xem mục 3 giải thích đầy đủ.

## 2. Giới hạn đã biết của bản prototype tĩnh nhiều trang (không đổi)

Giống hệt bản trước — nhắc lại ngắn gọn, không lặp chi tiết:

1. Danh mục `Criteria` (key riêng `platform_manager_criteria_catalog_v1`)
   **không đồng bộ ngược** về `dashboard.html` (vẫn dùng `DTI_ITEMS` tĩnh,
   không sửa logic gốc) — chỉ tiêu mới tạo ở lưới này sẽ không hiện trên
   bảng 62 chỉ tiêu của Dashboard, dù dữ liệu đánh giá vẫn lưu đúng vào
   `historyData` dùng chung.
2. `route` trong `NavItem` hiện thực bằng `href` trỏ file tĩnh (không phải
   `routerLink`/SPA navigation).
3. Cột "Phụ trách" sau Import chỉ là text tự do hiển thị, chưa resolve
   sang `AppUser` thật (câu hỏi mở #8, `business-rules.md`).

## 3. UX quan trọng nhất — Sửa trực tiếp trong lưới (edit-per-cell), KHÔNG còn nút "Lưu dữ liệu"

Đây là thay đổi UX lớn nhất so với bản 2-tab trước, cần hiểu rõ trước khi
đọc phần Actions:

### 3.1. Vòng đời 1 lần sửa 1 ô ("Tiến độ %" hoặc "Ghi chú")

```
[Xem]  --click vào ô-->  [Sửa]  --bấm ✓ (hoặc Enter)-->  [Đã lưu, quay về Xem]
                            |
                            +---bấm ✗ (hoặc Esc)-------->  [Huỷ, quay về Xem — KHÔNG lưu gì]
```

1. **Trạng thái Xem (mặc định)**: ô hiển thị giá trị hiện tại dạng text
   tĩnh (`<span class="cell-editable">`), có gạch chân nét đứt khi hover
   (dùng màu `--brand`) + con trỏ `pointer` — gợi ý "bấm được".
2. **Bấm vào ô** (`onclick="startEditCell(code, field)"`) → ô chuyển sang
   **trạng thái Sửa**: text tĩnh được thay bằng `<input>` (giữ đúng class
   `progressInput`/`noteInput` cũ để không phải viết lại style input) +
   **2 icon nút cạnh input**: **✓** (xanh, viền `--good`, tooltip "Lưu") và
   **✗** (đỏ, viền `--bad`, tooltip "Huỷ"). Input tự động `focus()` +
   `select()` (chọn sẵn toàn bộ text) ngay khi vào chế độ Sửa.
3. **Bấm ✓** (`onclick="confirmEditCell(code, field)"`, hoặc phím `Enter`
   trong input): đọc giá trị input, ghi vào `draft` (`values[code]` ép kẹp
   `[0,100]` nếu là Tiến độ %, hoặc `notes[code]` nguyên văn nếu là Ghi
   chú), **lưu ngay lập tức** vào `historyData` (gọi `commitDraftToHistory()`
   — xem mục 3.2), thoát chế độ Sửa, ô hiển thị lại giá trị MỚI.
4. **Bấm ✗** (`onclick="cancelEditCell()"`, hoặc phím `Escape`): thoát chế
   độ Sửa **mà không đọc/ghi giá trị input** — vì `draft` chưa từng bị sửa
   ở bước này (chỉ đọc khi bấm ✓), "khôi phục giá trị cũ" đơn giản là
   render lại đúng giá trị đang có trong `draft`, không cần lưu riêng một
   bản "giá trị trước khi sửa" nào khác.
5. **Chỉ 1 ô được sửa tại 1 thời điểm** (biến toàn cục `editingCell =
   {code, field, original}`) — bấm vào ô khác trong khi đang sửa 1 ô sẽ
   **tự động thoát** ô đang sửa dở (không lưu, giống bấm ✗) rồi mở ô mới,
   vì `renderGrid()` render lại toàn bộ lưới mỗi lần gọi và chỉ 1 ô khớp
   `editingCell` được vẽ ở chế độ Sửa.

**✅ Đã fix (2026-08-12, verify bằng chrome-devtools-mcp phát hiện)**: bản
đầu có nguy cơ ✗ không khôi phục đúng giá trị cũ nếu người dùng lỡ bấm
nhầm ✓ (2 icon 26×26px cạnh nhau, dễ trúng nhầm khi test/dùng thực tế) —
đã tăng kích thước icon lên 30×30px + gap 6px, **ẩn hẳn spin-arrow gốc**
của `<input type=number>` (nguyên nhân khiến giá trị bị đổi ngoài ý muốn
chỉ bằng 1 cú click, trước khi kịp bấm ✓/✗ — `::-webkit-inner/outer-spin-button{-webkit-appearance:none}`
+ `-moz-appearance:textfield`, vẫn gõ số bình thường hoặc dùng phím
lên/xuống bàn phím), và thêm guard trong `confirmEditCell()` (chỉ commit
đúng ô đang thật sự ở `editingCell`, chặn lời gọi lạc/trễ). Cơ chế cốt lõi
"✗ không đọc/ghi input" ở bước 4 phía trên **không đổi bản chất** — chỉ
củng cố thêm để giảm rủi ro bấm nhầm mục tiêu.

### 3.2. Vì sao KHÔNG còn nút "Lưu dữ liệu" — mỗi ✓ tự lưu ngay

**[QUYẾT ĐỊNH UX của `frontend-expert`, người dùng chỉ yêu cầu bỏ tab +
thêm UX ✓/✗, không nói rõ có giữ nút "Lưu dữ liệu" tổng hay không — đây là
suy luận hợp lý nhất theo tinh thần yêu cầu]:**

- Từ "✓ (xác nhận lưu)" trong mô tả yêu cầu đã hàm ý: bấm ✓ = **lưu thật**,
  không phải "tạm ghi vào bộ nhớ chờ bấm 1 nút Lưu tổng khác sau". Giữ thêm
  1 nút "Lưu dữ liệu" riêng sẽ **mâu thuẫn ngữ nghĩa** với chữ ✓ (lưu) đã
  có trên từng ô — người dùng bấm ✓ xong tưởng đã lưu nhưng thực ra chưa,
  dễ mất dữ liệu nếu quên bấm nút Lưu tổng rồi rời trang.
- Lưới giờ có tính chất "bảng dữ liệu trực tiếp" (spreadsheet-style admin
  grid) — nhất quán với cách "Sửa" chỉ tiêu (dialog) đã **lưu ngay khi bấm
  "Lưu chỉ tiêu"** trong dialog, không có bước xác nhận tổng nào khác. Áp
  dụng cùng triết lý cho ô Tiến độ %/Ghi chú.
- Hệ quả kỹ thuật: mỗi lần ✓ gọi `commitDraftToHistory()` — hàm này **upsert
  toàn bộ snapshot `draft` hiện tại** vào `historyData` theo `draft.date`
  (giống hệt `saveWeek()` gốc của `dashboard.html`, chỉ đổi tên + gọi tự
  động thay vì qua nút). Vì vậy **mỗi lần sửa xong 1 ô, toàn bộ kỳ đang sửa
  đã là dữ liệu "chính thức"** — không có khái niệm "nháp chưa lưu" nào
  tồn tại lâu dài trên trang này nữa (khác hẳn `draft` khái niệm cũ ở
  `dashboard.html`, nơi "nháp" có thể tồn tại nhiều bước trước khi bấm "Lưu
  tuần này").
- **Nút "Tạo kỳ mới từ kỳ gần nhất"** (vẫn giữ, xem mục 4.2) theo cùng
  triết lý: bấm là **lưu ngay** (tạo `AssessmentPeriod` mới = ngày hôm nay,
  copy `values` từ kỳ gần nhất) — không phải "tạo nháp chờ Lưu".
- **Import CSV** vốn đã lưu ngay từ thiết kế trước (không đổi) — nay nhất
  quán với toàn bộ phần còn lại của trang.

## 4. Layout

**[ĐÃ ĐỔI vòng #3]** Đã **bỏ hẳn** `.toolbar` (Import CSV/+ Thêm chỉ tiêu đứng riêng) và
`section.weekbar` (label "Đang sửa kỳ:"/`select#periodSelect`/btn "Tạo kỳ mới từ kỳ gần nhất") —
hợp nhất Import/Thêm vào cùng hàng `.filters` của lưới, ghim phải:

```
.sidebar                    → "DTI" (con của "Danh mục") active
.topbar → .logo "Danh mục" + subtitle, không action nào trong topbar

main (max-width 1600px)
  .notice           → giải thích: Import CSV là cách nhập chính, sửa inline
                       Tiến độ %/Ghi chú, CRUD chỉ tiêu qua "+ Thêm"/Sửa/Xoá

  .card#dtiGridCard [ID MỚI vòng #5 — display:flex;flex-direction:column, height CỐ ĐỊNH tính động bằng
                      JS (không phải max-height — xem lý do fix ở banner đầu file), xem mục 6.11]
    .title h2 "Danh mục & Đánh giá theo tuần" + #gridCountText          } flex:none — giữ nguyên vị trí
    .filters (no-print): #gq (tìm) + #gGroupFilter (select 6 nhóm)      } bình thường, không nằm trong
                         + .filters-actions{margin-left:auto} → label.btn "Import CSV"  } vùng cuộn
                           (input file .csv, hidden) + btn.primary "+ Thêm chỉ tiêu"     }
                         [ĐÃ ĐỔI vòng #3 — trước đây 2 nút này nằm trong .toolbar riêng phía trên]
    .muted            → ghi chú mẫu cột + giới hạn .csv/.xlsx (trước nằm trong .toolbar, nay 1 dòng
                        riêng ngay dưới .filters)                                        } flex:none
    .tablewrap → table[min-width:1900px] → thead (12 cột, xem mục 5) → tbody#gridBody
                 [ĐÃ ĐỔI vòng #3] cột "Mã" sticky trái + cột "Hành động" sticky phải — xem mục 6.8
                 [ĐÃ ĐỔI vòng #5] flex:1;min-height:0;overflow-y:auto — CHỈ khối này cuộn dọc riêng khi
                 dữ liệu 1 trang cao hơn phần còn lại của card, xem mục 6.11
    .pagination#gridPagination [MỚI vòng #3] → chọn số dòng/trang (10/20/50) + Trước/Sau — xem mục 6.7
                 [ĐÃ ĐỔI vòng #5] flex:none, neo cố định cuối card — luôn nhìn thấy, không cần cuộn trang

  .footer → ghi chú localStorage dùng chung với Dashboard

dialog#criteriaDialog     → form Thêm/Sửa chỉ tiêu (Mã/Tên/Nhóm/Điểm tối đa)
dialog#confirmDialog      → xác nhận chung (dùng cho Xoá chỉ tiêu)
dialog#importResultDialog → kết quả Import CSV
```

**[MỚI vòng #3] Vì sao bỏ hẳn "Đang sửa kỳ"/"Tạo kỳ mới" thay vì chỉ ẩn tạm**: người dùng xác nhận
kỳ sẽ được xác định theo `createdDate` của bản ghi (ngày import/thêm mới), không còn khái niệm
`AssessmentPeriod` tách rời do người dùng tự "tạo kỳ mới". `backend-expert` đang chốt model dữ liệu
song song (sẽ báo qua `SendMessage` khi xong) — cho tới lúc đó, `draft`/`historyData` trong
localStorage **vẫn giữ nguyên cơ chế cũ phía dưới UI** (chỉ gỡ phần UI điều khiển kỳ), lưới luôn hiển
thị/sửa vào kỳ **gần nhất đã lưu** (hoặc kỳ trống hôm nay nếu chưa từng có) — giống hệt hành vi mặc
định trước đây, chỉ khác là người dùng không còn cách nào chuyển sang xem kỳ khác hay tạo kỳ mới thủ
công từ màn này nữa. `fillPeriodSelect()`/`onPeriodSelectChange()`/`newPeriodFromLatest()` giữ nguyên
trong JS (theo đúng quy ước đã có của file — xem các hàm dead code khác như `loadDraftForDate()` ở
`dashboard.html`), `fillPeriodSelect()` đổi thành no-op an toàn (không còn phần tử DOM để cập nhật).

**Ghi chú kiến trúc component (khi build Angular)**: 1 lưới map thành 1
feature `modules/danh-muc-dti/` — `pages/danh-muc-dti/danh-muc-dti.page.ts`
(smart, giữ `draft`/`catalog` state, gọi service) điều phối các component
con: `criteria-grid-table` (dumb, nhận `rows`/`editingCell`, phát sự kiện
`cellEditStart`/`cellEditConfirm`/`cellEditCancel`/`edit`/`delete`),
`criteria-form-dialog`, `confirm-dialog`, `csv-import-dialog`,
`period-picker` (dropdown "Đang sửa kỳ"). Logic edit-per-cell (mục 3) nên
tách thành 1 `EditableCellComponent` dumb tái dùng cho cả 2 cột (Tiến độ %/
Ghi chú) thay vì lặp code — điểm khác biệt với bản HTML prototype (nơi 2
hàm `renderProgressCell`/`renderNoteCell` viết riêng cho đơn giản).

## 5. Cột của lưới (12 cột)

| # | Cột | Nguồn | Sửa được? |
| --- | --- | --- | --- |
| 1 | Mã | `catalog[].Code` | Qua dialog "Sửa" (cột 12) |
| 2 | Tên | `catalog[].Name` | Qua dialog "Sửa" |
| 3 | Nhóm | `catalog[].GroupId` + `groupName()` | Qua dialog "Sửa" |
| 4 | Điểm tối đa | `catalog[].MaxScore` | Qua dialog "Sửa" |
| 5 | Tự đánh giá | `draft.selfScores[code]` | **Không** — chỉ Import ghi đè (xem `business-rules.md` mục 2.2) |
| 6 | Thẩm định | `draft.verifiedScores[code]` | **Không** — chỉ Import |
| 7 | Trạng thái | `draft.statuses[code]` (badge `bwork` trung tính — KHÔNG phải badge tính động `statusFor()` của Dashboard, xem lưu ý ở mục 6) | **Không** — chỉ Import |
| 8 | Phụ trách | `draft.owners[code]` (text tự do) | **Không** — chỉ Import; xem giới hạn mục 2 #3 |
| 9 | Hạn xử lý | `draft.deadlines[code]` | **Không** — chỉ Import |
| 10 | **Tiến độ %** | `draft.values[code]` | **Có** — sửa inline (mục 3) |
| 11 | **Ghi chú** | `draft.notes[code]` | **Có** — sửa inline (mục 3) |
| 12 | Hành động | — | `action-btn` "Sửa" (mở dialog CRUD) + "Xoá" |

## 6. Actions

### 6.1. Tìm kiếm / lọc theo nhóm — `input#gq` / `select#gGroupFilter`

Filter real-time (`oninput`/`onchange`), không phân biệt hoa/thường, khớp
`Code + ' ' + Name`. Chỉ ảnh hưởng hiển thị, không đổi dữ liệu.

### 6.2. ~~Đổi "kỳ đang sửa"~~ — [ĐÃ BỎ khỏi UI vòng #3, xem mục 4]

Nội dung dưới đây **chỉ còn giá trị tài liệu lịch sử** (hành vi gốc trước khi bỏ) — `select#periodSelect`
không còn tồn tại trong DOM:

- Danh sách các kỳ **đã lưu** (sort giảm dần theo ngày, hiển thị
  `dd/mm/yyyy · xx.x%` — cùng định dạng `select#savedWeeks` cũ của
  Dashboard). Chọn 1 kỳ → nạp toàn bộ dữ liệu kỳ đó vào `draft`, render lại
  lưới — cột 5–11 đổi theo đúng dữ liệu của kỳ vừa chọn.
- **Mặc định khi mở trang (vẫn đúng, không đổi)**: kỳ **gần nhất đã lưu** (không phải "hôm nay")
  — nếu chưa từng có kỳ nào, mới dùng kỳ trống ngày hôm nay.
- **Không có date picker tự do** ở màn này (khác `input#weekDate` cũ của
  `dashboard.html` — đã bỏ theo đúng yêu cầu "không cần date picker riêng
  nữa vì đã bỏ tab") — chỉ chọn trong số kỳ **đã tồn tại**.

### 6.3. ~~"Tạo kỳ mới từ kỳ gần nhất"~~ — [ĐÃ BỎ khỏi UI vòng #3, xem mục 4]

Nội dung dưới đây **chỉ còn giá trị tài liệu lịch sử**:

- Tạo 1 kỳ mới `date = hôm nay`, copy `values`/`notes`/5 field Import từ kỳ
  gần nhất (hoặc trống nếu chưa có kỳ nào) — **lưu ngay** (không phải nháp,
  xem mục 3.2), cập nhật `select#periodSelect`, render lại lưới.
- Hàm `newPeriodFromLatest()` vẫn còn nguyên vẹn trong JS (dead code, không còn phần tử nào gọi) —
  cùng quy ước với các hàm dead code khác trong dự án (vd `loadDraftForDate()`/`saveWeek()` ở
  `dashboard.html`).

### 6.4. "Import CSV" — giữ nguyên rule mapping, chỉ đổi nơi render sau import

Hành vi/mapping **không đổi** so với thiết kế trước (xem
`business-rules.md` mục 2.2 và phần code `parseCsv()`/`importCsvFile()`)
— chỉ khác điểm cuối: sau khi import xong, gọi `renderGrid()` +
`fillPeriodSelect()` (lưới hợp nhất) thay vì `renderCriteriaTable()`/
`renderAssessmentAll()` (2 tab cũ đã bỏ). Tóm tắt nhanh (chi tiết đầy đủ ở
bản trước, không lặp lại toàn bộ):

- `PeriodDate` = ngày hệ thống lúc import (không có cột ngày trong mẫu).
- Ghi đè toàn bộ snapshot của kỳ trùng ngày (upsert).
- `Code` lạ → tự động tạo `Criteria` mới; nếu "Nhóm" cũng lạ → bỏ qua dòng,
  báo lỗi, không tự tạo nhóm.
- Cross-check "Chênh lệch" → cảnh báo không chặn.
- Kết quả hiện `dialog#importResultDialog` (thành công/cảnh báo/lỗi từng
  dòng).

### 6.5. "+ Thêm chỉ tiêu" / "Sửa" / "Xoá" — CRUD `Criteria` (không đổi so với thiết kế trước)

Giữ nguyên toàn bộ hành vi đã thiết kế (dialog form Mã/Tên/Nhóm/Điểm tối
đa, validate theo `business-rules.md` mục 1.1/1.2, xoá có xác nhận qua
`dialog#confirmDialog` với 2 nhánh hard/soft-delete theo mục 1.3) — chỉ
khác điểm re-render cuối cùng gọi `renderGrid()` (1 lưới) thay vì 2 hàm
render tách biệt của bản 2-tab cũ. Không lặp lại chi tiết rule ở đây, xem
bản đặc tả gốc trong `business-rules.md`.

### 6.6. Sửa "Tiến độ %" / "Ghi chú" inline — xem mục 3 (đã mô tả đầy đủ UX)

### 6.7. [MỚI vòng #3] Phân trang lưới — `#gridPagination`

- Thay cho hiện toàn bộ chỉ tiêu đã lọc trong 1 lần cuộn dọc liên tục: `renderGrid()` giờ cắt `arr`
  đã lọc/sort theo `gridPage`/`gridPageSize` (`gridPage=1, gridPageSize=20` mặc định) trước khi map ra
  `<tr>`. `#gridCountText` đổi format thành `"start–end/total chỉ tiêu (tổng N)"` (N = tổng toàn bộ
  `activeCatalog()`, không phụ thuộc lọc/trang).
- Control: `select` chọn số dòng/trang — 3 mức cố định **10/20/50** (frontend-expert tự chọn theo giá
  trị phổ biến, không phải yêu cầu chính xác từ người dùng) — `onchange="changeGridPageSize(this.value)"`
  (đổi `pageSize`, reset về trang 1). 2 nút "‹ Trước"/"Sau ›" (`changeGridPage(p)`, tự `disabled` ở biên
  trang đầu/cuối) + text "Trang X/Y" ở giữa.
- **Reset về trang 1** mỗi khi: đổi từ khoá tìm kiếm/nhóm (`onGridFilterChange()` — thay thế trực tiếp
  `oninput`/`onchange` cũ vốn gọi thẳng `renderGrid()`), sau khi Import CSV thành công (dữ liệu đổi
  nhiều). **Không** reset khi bấm ✓/✗ sửa 1 ô (giữ nguyên trang đang xem sau khi lưu 1 dòng).
- Rỗng (`total===0`): `#gridPagination` render chuỗi rỗng (ẩn hẳn control phân trang), giống hành vi
  "không hiện thông báo phân trang khi bảng trống".

### 6.8. [MỚI vòng #3] Cuộn ngang + cột sticky (Mã trái, Hành động phải)

- **Cuộn ngang**: `.tablewrap{overflow-x:auto}` (đổi từ `overflow:auto` — tường minh hoá trục cuộn,
  hành vi không đổi bản chất nhưng đã kiểm tra lại theo phản hồi người dùng báo "chưa cuộn ngang
  được" — `table` vẫn giữ `min-width:1900px` ép rộng hơn khung nhìn khi màn hình hẹp).
- **Cột "Hành động" (cuối bảng) sticky phải**: `position:sticky;right:0` + `background` trùng màu nền
  dòng (`#fff` dòng lẻ, `#f8fafc` dòng chẵn/`th`, `var(--bg)` khi hover — khớp đúng 3 trạng thái nền có
  sẵn của `tbody tr`) + `box-shadow` nhẹ bên trái để phân tách trực quan với vùng đang cuộn qua bên
  dưới nó. Đáp ứng đúng yêu cầu "cố định dòng sửa xoá... chỉ cho dịch ngang các dòng còn lại" — nút
  Sửa/Xoá (`.action-btn`) luôn nhìn thấy được bất kể cuộn tới đâu.
- **Cột "Mã" (đầu bảng) cũng sticky trái** (`position:sticky;left:0`) — bổ sung theo gợi ý "có thể cân
  nhắc" trong yêu cầu, giúp định vị đúng dòng đang xem trong lúc cuộn ngang qua các cột giữa.
- `z-index` phân lớp: `th` (header) cao nhất (`5`) để luôn nổi trên cả nội dung cuộn dọc lẫn 2 cột
  sticky ngang; `td` sticky (`2`/`3`) cao hơn `td` thường (không sticky, mặc định `0`) nhưng thấp hơn
  `th`.

### 6.9. [MỚI vòng #4, ĐÃ CHỐT] Bộ lọc Năm + Kỳ trong năm — `select#dtiYearFilter` / `select#dtiPeriodFilter`

- **`select#dtiYearFilter`** (`onchange="onDtiYearFilterChange()"`): liệt kê mọi năm có dữ liệu (suy từ
  `historyData[].date`) + năm hiện tại (luôn có mặt dù chưa có dữ liệu). Đổi năm → nạp lại
  `select#dtiPeriodFilter` theo năm mới (`fillDtiPeriodFilter()`), reset về "Tất cả", reset trang 1.
- **`select#dtiPeriodFilter`** (`onchange="onDtiPeriodFilterChange()"`): option đầu cố định `"Tất cả
  (mới nhất trong năm)"` (value `__ALL__`), theo sau là từng kỳ đã lưu **trong năm đang chọn** (sort
  giảm dần theo ngày, hiển thị `dd/mm/yyyy`).
- **Nguồn dữ liệu hiển thị theo lựa chọn** (`resolveRowData(code)` trong JS):
  - **"Tất cả"**: với mỗi chỉ tiêu, tìm kỳ **gần nhất trong năm có `touched[code]===true`**
    (`latestTouchedPeriodInYear()`) — dùng giá trị của kỳ đó cho cột 5–11. Không tìm thấy → mọi cột
    hiện `"—"` (chỉ tiêu chưa từng có thao tác trong năm đang chọn).
  - **1 kỳ cụ thể**: đọc thẳng dữ liệu của đúng ngày đó; nếu chỉ tiêu không `touched` trong đúng ngày
    này (vd bản ghi tồn tại nhưng không có trong `touched` — dữ liệu cũ trước khi có touched-tracking
    coi như đã touched, xem `isTouchedDti()`), cột Tiến độ %/Ghi chú hiện `"—"` thay vì giá trị kế thừa.
- **Không đổi việc sửa inline ghi vào đâu**: `confirmEditCell()` luôn ghi vào `draft` (kỳ hôm nay) —
  bộ lọc Năm/Kỳ chỉ đổi HIỂN THỊ, không đổi đích ghi. Vì vậy chỉ cho phép sửa khi đang xem đúng trạng
  thái mà "hiển thị" và "đích ghi" trùng nhau — xem mục 6.10 (rule read-only).
- Import CSV (`gridPage=1` + nạp lại `fillDtiYearFilter()`/`fillDtiPeriodFilter()` sau khi import
  thành công) và mỗi lần ✓ commit 1 ô (`commitDraftToHistory()`) đều làm mới 2 dropdown này để phản
  ánh kỳ vừa ghi (hôm nay) nếu nó là ngày mới chưa từng xuất hiện trong danh sách.

### 6.10. [MỚI vòng #4, ĐÃ CHỐT CHÍNH THỨC bởi `backend-expert` + người dùng] Rule read-only khi xem lịch sử

Không còn là đề xuất chờ xác nhận (khác banner vòng #3 cũ) — đây là hành vi bắt buộc:

- **Điều kiện "editable" (duy nhất 1 trạng thái)**: `isViewingCurrentPeriod()===true`, tức
  `dtiPeriodFilter.value==='__ALL__'` **VÀ** `dtiYearFilter.value===`năm hiện tại. Đây cũng là trạng
  thái **mặc định khi mở trang**.
- **Mọi trạng thái khác** (chọn năm khác, HOẶC thu hẹp về 1 kỳ cụ thể dù vẫn trong năm nay) →
  **CHỈ ĐỌC toàn bộ**:
  - `#dtiReadonlyBanner` (`.notice`) hiện: `"Đang xem dữ liệu lịch sử — chỉ đọc. Quay lại 'Tất cả (mới
    nhất trong năm)' của năm hiện tại để chỉnh sửa."`.
  - `#dtiFiltersActions` (label "Import CSV" + btn "+ Thêm chỉ tiêu") **ẩn hẳn** (`display:none`).
  - Ô "Tiến độ %"/"Ghi chú" mỗi dòng render dạng **text tĩnh** (không có `cell-editable`/click
    affordance) thay vì span có thể bấm — `renderProgressCell(code, v, editable)` /
    `renderNoteCell(code, note, editable)` nhận thêm tham số `editable` quyết định nhánh render.
  - Cột "Hành động" mỗi dòng hiện `"—"` (muted) thay vì 2 nút Sửa/Xoá.
- **Lý do (không phải chọn tuỳ hứng UX)**: việc ghi dữ liệu (`confirmEditCell()`/Import) luôn nhắm vào
  **"hôm nay"** bất kể đang xem kỳ/năm nào (xem mục 6.9) — nếu vẫn cho sửa khi đang hiển thị dữ liệu
  của 1 ngày/năm khác, người dùng sẽ tưởng đang sửa đúng bản ghi đang nhìn thấy nhưng thực ra ghi đè
  vào sai ngày → rule read-only là **bắt buộc về mặt đúng đắn dữ liệu**, không phải tinh chỉnh giao
  diện.
- **Không ảnh hưởng gì tới Dashboard** (`dashboard.html`) — Dashboard vốn đã 100% read-only từ trước
  (không có action nào để giới hạn thêm).

### 6.11. [MỚI vòng #5, ĐÃ CHỐT] Gói bảng + phân trang trong 1 màn hình — `#dtiGridCard`

- **Vấn đề gốc**: `.tablewrap` có `overflow-x:auto;overflow-y:visible` — scroll dọc thật sự xảy ra ở cấp
  **trang** (`body`), không phải trong khung bảng. Với bảng 62 dòng (hoặc tối đa 50 dòng/trang ở
  `gridPageSize=50`), chiều cao bảng thường vượt viewport → `#gridPagination` (nằm ngay sau
  `.tablewrap`) bị đẩy xuống dưới nếp gấp, người dùng phải cuộn cả trang mới thấy.
- **Cơ chế mới**: `#dtiGridCard{display:flex;flex-direction:column;min-height:0}` +
  `#dtiGridCard>.title,>.notice,>.filters,>.muted,>.pagination{flex:none}` (giữ chiều cao tự nhiên) +
  `#dtiGridCard>.tablewrap{flex:1;min-height:0}` (chiếm hết phần còn lại — `min-height:0` bắt buộc,
  thiếu dòng này flex item mặc định không co nhỏ hơn nội dung, làm hỏng cơ chế overflow). Chiều cao của
  `#dtiGridCard` được set bằng JS qua `card.style.height` (**CỐ ĐỊNH, không phải `max-height`** — xem lý
  do fix bên dưới), không khai báo trong CSS tĩnh.
- **`updateGridCardHeight()`** (cuối `<script>` đầu, gọi trong `init()` + `resize`/`orientationchange`):
  ```js
  const top = card.getBoundingClientRect().top;
  card.style.height = Math.max(280, window.innerHeight - top - 16) + 'px';
  ```
  `280px` = sàn an toàn cho viewport rất thấp (điện thoại nằm ngang); `16px` = khoảng chừa dưới cùng.
- **[FIX cùng ngày vòng #5, sau screenshot người dùng] Vì sao `height` chứ không phải `max-height`**:
  bản đầu dùng `card.style.maxHeight` — với `max-height`, chiều cao thật của box = `min(nội dung,
  max-height)`, nên khi trang hiện tại ít dòng (vd trang cuối chỉ còn 2 dòng), card TỰ CO LẠI theo nội
  dung thay vì giữ đúng khoảng đã tính, đẩy `.pagination` lên sát ngay dưới dữ liệu và để lại khoảng
  trắng lớn phía dưới trước khi tới `.footer` — vị trí `.pagination` nhảy lên/xuống tuỳ số dòng đang
  hiển thị. Đổi sang `height` cố định buộc card LUÔN chiếm đúng chiều cao đã đo được bất kể
  `.tablewrap` bên trong có ít hay nhiều dòng; khi ít dòng, `.tablewrap` (đã `flex:1;min-height:0`) tự
  giãn lấp đầy khoảng trống thừa thay vì để card co lại — `.pagination` nhờ vậy luôn neo đúng 1 vị trí
  cố định ở đáy card, giống hệt bố cục khi đủ dòng.
- **`.tablewrap{overflow-y:auto}`** (đổi từ `visible`) — khi nội dung 1 trang cao hơn không gian còn lại
  của card, **chỉ `.tablewrap` cuộn dọc riêng** (giữ nguyên `overflow-x:auto` cho cuộn ngang, không đổi).
  `<thead th>{position:sticky;top:0}` (đã có từ trước) nay dính đúng vào top của **khung cuộn nội bộ**
  này thay vì dính dưới `.topbar` ở cấp trang.
- **Sửa xung đột z-index sticky 2 chiều** (phát sinh vì trước đây scroll dọc chưa từng thực sự kích hoạt
  bên trong `.tablewrap` nên bug tiềm ẩn không lộ ra): tier mới —
  - Góc header (`th:first-child`, `th:last-child`, vừa dính top vừa dính left/right): `z-index:5`.
  - Header thường (`th`, chỉ dính top): `z-index:4` (tăng từ `2`).
  - Cột sticky body (`td:first-child` "Mã", `td:last-child` "Hành động", chỉ dính left/right):
    `z-index:2` (cột "Hành động" **giảm từ `3` xuống `2`** — đây là nguyên nhân gốc gây đè xuyên thấu, vì
    `3 > 2` khiến nó nổi trên cả header thường khi 2 hướng cuộn cùng lúc).
  - Ô thường: mặc định (auto/0), thấp nhất.
- **Print**: `@media print{#dtiGridCard{height:auto!important;max-height:none!important}}` — bỏ giới hạn
  chiều cao khi in (đi cùng `.tablewrap{overflow:visible}` đã có sẵn), tránh in thiếu dòng.
- **Không đổi** hành vi phân trang (mục 6.7) hay cột sticky trái/phải (mục 6.8) — chỉ đổi CƠ CHẾ CUỘN
  bao quanh chúng, số liệu/UX của từng control giữ nguyên 100%.
- **`dashboard.html` (bảng 62 chỉ tiêu DTI) CHỦ ĐỘNG KHÔNG áp dụng cơ chế này ở vòng #5** (đã đánh giá
  kỹ, không phải bỏ sót): kỹ thuật đo `getBoundingClientRect().top` tại thời điểm tải trang chỉ đúng khi
  card nằm ngay gần đỉnh trang (đúng trường hợp `danh-muc-dti.html`, chỉ dưới 1 banner hướng dẫn) —
  card bảng 62 chỉ tiêu ở `dashboard.html` nằm SAU `weekbar` + 5 thẻ KPI + 2 card "Tiến độ theo nhóm"/
  "Biểu đồ xu hướng", tổng chiều cao các khối này thường đã vượt 1 màn hình, nên đo tại thời điểm tải
  trang (scroll=0) sẽ ra số quá lớn, khiến card bị kẹp về sàn an toàn tối thiểu 1 cách tuỳ tiện — áp
  nguyên xi công thức này vào sẽ làm bảng co nhỏ giả tạo, tệ hơn hiện trạng. Chi tiết đầy đủ + điều
  kiện áp dụng đúng (đo tại thời điểm card cuộn tới đầu viewport, không phải lúc tải trang) — xem banner
  "ĐÁNH GIÁ VÒNG PHẢN HỒI #5" đầu `spec/dashboard-dti-weekly/ui-spec.md`.

## 7. States

- **Ô đang sửa (edit mode)**: input + 2 icon ✓/✗ luôn hiện đồng thời (không
  ẩn icon nào) — không có trạng thái "đang sửa nhưng chưa hiện nút lưu".
- **Validate khi bấm ✓ cho Tiến độ %**: ép kẹp `[0,100]` im lặng (giống
  hệt rule cũ `setProgress()`), không hiện thông báo lỗi riêng — giá trị
  ngoài khoảng tự động bị kẹp về biên trước khi lưu.
- **Ghi chú rỗng**: hiển thị `"— bấm để ghi chú"` màu muted ở trạng thái
  Xem (khác hẳn ô Tiến độ % luôn có giá trị số, không bao giờ "rỗng").
- **Lưới rỗng** (bộ lọc không khớp gì): 1 hàng `colspan=12` "Không có chỉ
  tiêu nào khớp bộ lọc."
- **Chưa có kỳ nào lưu**: `select#periodSelect` chỉ có 1 option
  "<ngày hôm nay> (chưa lưu)"; toàn bộ cột 5–9 hiện "—"; cột Tiến độ %
  mặc định `0%` cho mọi chỉ tiêu.
- **Xoá chỉ tiêu**: luôn qua `dialog#confirmDialog`, nội dung động theo 2
  nhánh (hard/soft) — không dùng `alert()` xác nhận kiểu cũ.
- **Kết quả Import**: luôn hiện `dialog#importResultDialog` (kể cả khi
  toàn bộ dòng lỗi) — không có trường hợp import "âm thầm" không phản hồi.
- **Không có**: loading state (đồng bộ hoàn toàn, không gọi mạng); nút
  "Lưu dữ liệu" tổng nào (xem mục 3.2 lý do bỏ hẳn).

## 8. Responsive

Dùng lại đúng breakpoint `560px` đã có. Sidebar/topbar/drawer kế thừa 100%
từ `spec/sidebar-menu/ui-spec.md`, không lặp lại.

- `≤560px`: `main`/`.topin` giảm padding còn `10px`; `.card` giảm padding
  còn `12px`; `.weekbar>*` giãn đều `flex:1`.
- Bảng lưới có `min-width:1900px` (12 cột, rộng hơn cả bảng Dashboard
  1200px lẫn bản "Đánh giá theo tuần" cũ 1500px vì gộp thêm 4 cột
  Mã/Tên/Nhóm/Điểm tối đa vào cùng 1 bảng) — cuộn ngang qua
  `.tablewrap{overflow-x:auto}`, cùng hành vi đã biết, không phải thiếu sót.
  Ô đang sửa (input + 2 icon) vẫn nằm gọn trong độ rộng cột "Tiến độ %"/
  "Ghi chú" nhờ `.cell-edit{display:flex}` + `input{flex:1;min-width:0}`.
- **[MỚI vòng #5]** Cơ chế "gói bảng + phân trang trong 1 màn hình" (mục 6.11) **áp dụng thống nhất ở
  mọi breakpoint**, không tắt riêng cho `≤560px` — vì được tính động bằng JS
  (`updateGridCardHeight()`, chạy lại khi `resize`) nên tự thích ứng đúng với mọi độ rộng, kể cả khi
  banner hướng dẫn phía trên xuống nhiều dòng hơn ở mobile (làm `card.getBoundingClientRect().top` lớn
  hơn — JS đo lại chính xác, không cần `calc()` riêng theo breakpoint). Sàn an toàn `minHeight:280px`
  áp dụng chung cho mọi viewport rất thấp (kể cả điện thoại nằm ngang). Chiều cao là `height` **CỐ
  ĐỊNH** (không phải `max-height` — xem lý do fix ở banner đầu file) nên card giữ nguyên kích thước dù
  trang hiện tại có bao nhiêu dòng, ở mọi breakpoint.
- `@media print`: ẩn `.sidebar`, `.filters`, `.no-print`;
  `#dtiGridCard{height:auto!important;max-height:none!important}`
  **[MỚI vòng #5]** tắt hẳn giới hạn chiều cao khi in — tái dùng đúng rule `.tablewrap{overflow:visible}`
  đã có.

## 9. Style thô

**[MỚI vòng #3] Token mật độ hiển thị (compact)** — bổ sung vào `:root` (cả 2 file, cùng giá trị để
đồng bộ), áp dụng qua `var()` ở mọi selector liên quan thay vì hardcode rải rác:

```
--fs-xs:11px   --fs-sm:12px   --fs-base:13px   --fs-md:14px   --fs-lg:15px
--sp-1:4px  --sp-2:6px  --sp-3:8px  --sp-4:10px  --sp-5:14px
--radius-sm:7px  --radius-md:9px
--sidebar-w:220px  --sidebar-w-collapsed:60px   (giảm từ 260px/72px)
```

Áp dụng: `body{font-size:var(--fs-base)}` (từ mặc định trình duyệt ~16px); `.btn`/`.action-btn` giảm
padding + font-size (`.action-btn` còn `4px 6px`/`--fs-xs`); `.cell-icon-btn` (✓/✗) giảm từ 30×30px
xuống **24×24px** (vẫn giữ nguyên toàn bộ cơ chế chống bấm nhầm ở mục 3.1 — chỉ đổi kích thước, không
đổi hành vi); `.badge` giảm còn `3px 6px`/`10px`; sidebar (`.sidebar-brand`/`.sidebar-navitem`/
`.sidebar-toggle`/icon SVG) giảm đồng bộ theo `--sidebar-w*`. **Không đổi** bất kỳ token màu/thương
hiệu nào (`--brand`/`--good`/`--warn`/`--bad`/...).

Class mới thuần layout (không
phải token màu), bổ sung so với thiết kế 2-tab cũ:

- `.cell-editable` — text tĩnh có thể bấm để sửa (gạch chân nét đứt màu
  `--brand` khi hover, `outline` `--brand` khi `:focus-visible` — hỗ trợ cả
  chuột và bàn phím, `tabindex="0"` + `onkeydown` Enter để mở edit mode).
- `.cell-edit` — flex container chứa input + 2 icon khi ở edit mode
  (`gap:6px`, tăng từ `4px` sau đợt fix bấm nhầm — xem mục 3.1).
- `.cell-icon-btn.ok` / `.cell-icon-btn.cancel` — nút tròn nhỏ **30×30px**
  (tăng từ 26×26px sau đợt fix), màu `--good`/`--bad` tương ứng, viền phái
  sinh nhạt (`#bfe3d2`/`#f3caca` — cùng công thức tint nhạt đã dùng ở
  `.bdone`/`.bstall`), không phát minh màu mới ngoài palette sẵn có.
- `.progressInput::-webkit-outer/inner-spin-button{-webkit-appearance:none}`
  + `-moz-appearance:textfield` — **ẩn spin-arrow gốc** của `<input
  type=number>` (bổ sung sau đợt fix, xem mục 3.1): arrow gốc trình duyệt
  quá nhỏ, dễ đổi giá trị ngoài ý muốn chỉ bằng 1 cú click lạc — người dùng
  vẫn gõ số hoặc dùng phím mũi tên lên/xuống trên bàn phím khi input đang
  focus, chỉ ẩn phần UI chuột dễ bấm nhầm.
- Đã **bỏ hẳn** `.tabs`/`.tab-btn` (không còn khái niệm tab trong CSS lẫn
  HTML).
- **[MỚI vòng #3]** `.pagination`/`.pg-btn`/`.pg-info` — control phân trang (mục 6.7), dùng lại
  `--line`/`--bg`/`--muted` sẵn có, không phát minh màu mới. `.filters-actions` — cụm 2 nút Import/Thêm
  ghim phải trong hàng `.filters` (mục 4), chỉ `margin-left:auto`, không thêm màu/token mới. Sticky
  cột Mã/Hành động (mục 6.8) dùng nền `#fff`/`#f8fafc`/`var(--bg)` đã có sẵn theo đúng 3 trạng thái nền
  hiện tại của dòng bảng, không phát minh giá trị mới.
- **[MỚI vòng #5]** `#dtiGridCard` — id mới (không phải class, vì chỉ 1 thẻ duy nhất trong trang) gắn
  thêm cạnh `.card` sẵn có, thuần layout (`display:flex;flex-direction:column`), không đổi
  màu/border/shadow/padding kế thừa từ `.card`. Không thêm token màu/spacing mới nào cho cơ chế viewport-
  fit — chỉ dùng `flex`/`min-height:0`/`overflow-y:auto` (thuộc tính layout) + `height` **cố định** set
  qua JS (`updateGridCardHeight()`, mục 6.11 — không phải `max-height`, xem lý do fix ở banner đầu
  file). z-index sticky 2 chiều chỉnh lại số (`2`→`4` cho `th` thường,
  `3`→`2` cho `td:last-child`) — không đổi màu nền `#f8fafc`/`#fff`/`var(--bg)` đã dùng cho các ô sticky.

## 10. Câu hỏi còn mở (kế thừa từ `business-rules.md`, không tự chốt thêm)

Không đổi so với bản thiết kế trước — xem `spec/danh-muc-dti/business-rules.md`
mục 5, không lặp lại. Bổ sung duy nhất 1 điểm phát sinh từ vòng phản hồi
này:

11. **[MỚI] Việc bỏ nút "Lưu dữ liệu" tổng, để mỗi ✓ tự lưu ngay, là suy
    luận UX của `frontend-expert`** (mục 3.2) — chưa phải câu người dùng
    xác nhận trực tiếp bằng văn bản dạng "có/không giữ nút Lưu tổng". Nếu
    sau này người dùng muốn có 1 bước xác nhận tổng trước khi ghi (vd để
    review nhiều thay đổi cùng lúc trước khi commit), đây là điểm cần
    quay lại thiết kế, không phải lỗi triển khai.
12. **[ĐÃ CHỐT, xem banner vòng #4 đầu file]** 4 câu hỏi #11–14 cũ (định nghĩa "Tất cả", carry-forward,
    tuần ISO, copy-forward `CriteriaEvidence`) — không còn mở, xem `business-rules.md` mục 2.4/3 bản
    mới nhất của `backend-expert`.
13. **[CÒN MỞ, không chặn]** Cách trình bày "Tất cả trong 1 năm" khi backend trả về nhiều bản ghi/1
    chỉ tiêu trong cùng năm (mỗi lần sửa = 1 record) — quyết định UX hiện tại (mục 6.9) là **thu gọn về
    1 dòng/chỉ tiêu, lấy giá trị lần thao tác gần nhất**, không liệt kê phẳng từng lần sửa. Nếu sau này
    cần xem đầy đủ audit trail 1 chỉ tiêu trong năm, cần thiết kế thêm màn "chi tiết chỉ tiêu" — chưa
    có trong scope hiện tại, không phải thiếu sót.
14. **[CÒN MỞ, không chặn]** Bộ lọc "1 kỳ cụ thể trong năm" ở `select#dtiPeriodFilter` hiện chỉ liệt kê
    theo **ngày đã lưu** (không nhóm theo tuần/tháng như gợi ý "tương tự tinh thần Dashboard" trong yêu
    cầu gốc) — quyết định thu hẹp phạm vi có chủ đích của `frontend-expert` để kịp giao trong vòng #4,
    vì điều kiện kích hoạt read-only chỉ cần phân biệt "Tất cả của năm hiện tại" vs "bất kỳ trạng thái
    thu hẹp nào khác" (đã đủ đúng dù không có UI nhóm theo tuần/tháng). Có thể bổ sung nhóm theo
    tuần/tháng ở vòng sau nếu người dùng cần.

# Business Rules — Danh mục DTI (Chỉ tiêu + Đánh giá theo tuần)

> Màn hình mới **"Danh mục > DTI"** (nested dưới menu cha "Danh mục" trong
> sidebar — xem `spec/sidebar-menu/ui-spec.md`), phát sinh từ quyết định
> kiến trúc UI mới (người dùng chốt):
>
> 1. **Dashboard đổi thành read-only** — bỏ toàn bộ khả năng nhập liệu trực
>    tiếp (không còn `input.progressInput`/`input.noteInput`/nút "Lưu dữ
>    liệu"/`.fab`). Dashboard chỉ còn hiển thị KPI, tiến độ nhóm, biểu đồ,
>    bảng 62 chỉ tiêu (đọc), lịch sử — xem
>    `spec/dashboard-dti-weekly/business-rules.md` mục 0 (đã cập nhật).
> 2. Toàn bộ nhập liệu chuyển vào màn hình này, dạng **2 tab**:
>    - **Tab "Chỉ tiêu"** — CRUD danh mục tĩnh (`Criteria`, có FK tới
>      `CriteriaGroup`).
>    - **Tab "Đánh giá theo tuần"** — chính là chức năng nhập
>      `ProgressPercent`/`Note` theo kỳ đã bỏ khỏi Dashboard, chuyển nguyên
>      xuống đây. **Cập nhật mới**: cơ chế nhập liệu CHÍNH của tab này giờ
>      là **Import file Excel/CSV** theo mẫu `doc/ERD/example_db_ver1.csv`
>      (không còn là JSON backup như `dashboard.html` gốc); nhập tay từng
>      dòng vẫn giữ làm cách bổ sung/điều chỉnh sau import — xem mục 2.
>
> Lưu ý: 2 tab thao tác trên 2 nhóm entity có **chu kỳ thay đổi khác nhau**
> (`Criteria` gần như tĩnh, `CriteriaAssessment` biến thiên theo tuần) —
> gộp chung 1 màn hình là quyết định UX có chủ đích của người dùng (thuận
> tiện thao tác), không phải nhầm lẫn mô hình dữ liệu. Data model (ERD) giữ
> nguyên như `doc/ERD/ERD.md` và `doc/ERD/PlatformManager.dbml` — tài liệu
> này **không** định nghĩa entity mới, chỉ bổ sung rule CRUD cho `Criteria`
> (trước đây `Criteria` chỉ được seed tĩnh từ CSV mẫu, chưa từng có CRUD)
> và xác nhận tab "Đánh giá theo tuần" tái dùng nguyên rule đã có ở
> `spec/dashboard-dti-weekly/business-rules.md`.

## 1. Tab "Chỉ tiêu" — CRUD `Criteria`

### 1.1. Create

| Field | Rule | Ghi chú |
| --- | --- | --- |
| `Code` | Required, unique (trong tập **chưa xoá mềm**), maxlength 20 | Khớp `doc/ERD/PlatformManager.dbml` (`varchar(20)`) + `src/BE/.claude/rules/entity-domain.md` |
| `Name` | Required, không giới hạn ngắn (`text`) | Có chỉ tiêu dài nhiều dòng (vd mã `4.22.1` trong CSV mẫu) |
| `GroupId` | Required, FK phải tồn tại và **chưa bị xoá mềm** (`CriteriaGroup.IsDeleted = false`) | Không cho gán vào một nhóm đã xoá |
| `MaxScore` | Required, `> 0` | Khớp `DomainException("CRITERIA_MAX_SCORE_INVALID", ...)` đã định nghĩa sẵn ở `entity-domain.md` |

**Lưu ý ràng buộc unique ở tầng DB:** vì `Criteria` dùng soft-delete
(`IsDeleted` từ `BaseEntity`), một unique index thuần trên cột `Code` sẽ
chặn cả việc tạo mới với `Code` trùng một bản ghi **đã xoá mềm** — có thể
không đúng ý nghiệp vụ (người dùng có thể muốn tái dùng `Code` sau khi xoá
một chỉ tiêu). Đề xuất: dùng unique **filtered/partial index**
(`WHERE "IsDeleted" = false`, PostgreSQL hỗ trợ tốt) thay vì unique index
thường trên `Code`; đồng thời check uniqueness ở handler phải query qua
repository (đã tự lọc `IsDeleted` theo EF global query filter — xem
`entity-domain.md`), không check trần trên toàn bộ bảng kể cả bản ghi đã
xoá.

**✅ Đã chốt (người dùng xác nhận)**: unique `Code` chỉ áp dụng trong tập
**chưa xoá mềm** — cho phép tái dùng `Code` sau khi một `Criteria` đã bị
xoá (mềm hoặc cứng). Dùng đúng cách tiếp cận filtered index nêu trên.

### 1.2. Update

Áp dụng rule field giống Create. Điểm cần quyết định riêng: **`Code` có
cho đổi sau khi tạo không?**

**Đề xuất: CHO PHÉP đổi tự do** (áp cùng rule unique như Create). Lý do:

- Theo `doc/ERD/PlatformManager.dbml`, FK duy nhất tham chiếu tới `Criteria`
  (`CriteriaAssessments.CriteriaId`) trỏ vào **`Id` (uuid, surrogate key)**,
  không trỏ vào `Code`. Đổi `Code` **không** phá vỡ bất kỳ ràng buộc tham
  chiếu nào ở tầng DB — khác trường hợp `Code` được dùng làm business key
  cho FK (khi đó đổi `Code` cần cascade), ở đây `Code` chỉ là dữ liệu hiển
  thị thuần tuý.
- Không có bằng chứng nào (CSV mẫu, `dashboard.html`) cho thấy `Code` được
  dùng làm khoá tra cứu từ hệ thống bên ngoài (vd tích hợp API khác định
  danh theo `Code`) — nếu có, đây sẽ là lý do chính đáng để hạn chế đổi,
  nhưng hiện tại chưa xác nhận được điều đó.

**Cảnh báo cần lưu ý khi implement (hệ quả, không phải lý do cấm đổi):** vì
các kỳ đánh giá lịch sử (`CriteriaAssessment`) tham chiếu `CriteriaId` chứ
**không** lưu snapshot `Code`/`Name` tại thời điểm đánh giá, đổi `Code`/
`Name` của một `Criteria` sẽ khiến **toàn bộ báo cáo lịch sử** (kể cả các
kỳ đã "chốt" từ nhiều tuần trước) hiển thị `Code`/`Name` MỚI, không giữ
nguyên như lúc đánh giá gốc. Đây là hành vi hợp lý cho hầu hết trường hợp
thực tế (sửa lỗi chính tả mã/tên) nhưng nếu nghiệp vụ cần "đóng băng" tên/
mã hiển thị trên báo cáo lịch sử, cần thiết kế snapshot riêng — **không làm
ở v1**, ghi nhận đây là giới hạn đã biết, không phải thiếu sót.

### 1.3. Delete — soft-delete có điều kiện, KHÔNG hard-delete tuỳ tiện

Đây là điểm quan trọng nhất của CRUD `Criteria`, vì `Criteria` là cha của
`CriteriaAssessment` (dữ liệu lịch sử đánh giá nhiều kỳ).

**Rule:**

1. Nếu `Criteria` **chưa từng có `CriteriaAssessment`** nào tham chiếu tới
   (kể cả `CriteriaAssessment` đã bị soft-delete) → cho phép **hard-delete**
   thật (xoá cứng khỏi DB). An toàn vì không có dữ liệu lịch sử nào phụ
   thuộc vào bản ghi này.
2. Nếu `Criteria` **đã có ≥1 `CriteriaAssessment`** tham chiếu (đã từng
   được đánh giá ở bất kỳ kỳ nào) → **CHỈ cho soft-delete**
   (`IsDeleted = true` qua `BaseEntity`), **không** cho hard-delete. Lý do:
   xoá cứng sẽ làm mất khả năng hiển thị lại lịch sử đánh giá các kỳ trước
   (Dashboard đọc lại `Criteria.Name`/`Code`/`MaxScore` khi hiển thị lịch
   sử/biểu đồ xu hướng, kể cả cho các chỉ tiêu đã ngừng theo dõi).
3. Sau khi soft-delete, `Criteria` đó:
   - **Không** xuất hiện trong danh sách chọn ở tab "Đánh giá theo tuần"
     khi tạo/sửa kỳ **hiện tại hoặc tương lai** — EF global query filter
     `IsDeleted = false` tự loại trừ theo đúng convention
     `entity-domain.md`, không cần thêm `.Where()` thủ công.
   - **Vẫn** phải hiển thị đầy đủ khi xem **lịch sử các kỳ đã lưu trước
     đó** có chứa `CriteriaAssessment` của nó — các query phục vụ
     Dashboard/lịch sử phải **chủ động** dùng `IgnoreQueryFilters()` (hoặc
     tương đương) cho `Criteria` khi join từ `CriteriaAssessment` lịch sử;
     nếu không, lịch sử cũ sẽ "mất" hàng dữ liệu chỉ vì chỉ tiêu gốc đã bị
     xoá mềm sau đó.
   - **Không** tính vào mẫu số `Σ MaxScore` của công thức "Tiến độ chung"
     (`spec/dashboard-dti-weekly/business-rules.md` mục 3.3/3.4) — **kể cả
     cho các kỳ lịch sử**, theo quyết định tạm chốt ở mục 5 câu hỏi #1
     (mẫu số dùng danh mục `Criteria` **hiện tại** cho **mọi** kỳ, không
     snapshot theo từng kỳ) — nhắc lại: đây là **quyết định mặc định, có
     thể xem lại sau**, không phải đã giải thích đầy đủ và chốt cứng.
4. **Không có** hành động "khôi phục" (un-delete) trong phạm vi yêu cầu
   này — nếu cần, đây là tính năng bổ sung sau, không thiết kế rule ở đây.
5. `CriteriaGroup` áp dụng cùng nguyên tắc "soft-delete có điều kiện" nếu
   scope CRUD sau này mở rộng sang cả nhóm: một nhóm đang có `Criteria` con
   chưa xoá mềm thì không nên cho xoá (kể cả soft-delete) cho tới khi nhóm
   đó rỗng. **[SUY LUẬN]** — nhiệm vụ hiện tại chỉ yêu cầu CRUD `Criteria`
   (Mã/Tên/Nhóm/Điểm tối đa), chưa yêu cầu CRUD `CriteriaGroup`; ghi nhận
   rule này để nhất quán nếu sau này mở rộng, không phải rule cần implement
   ngay.

## 2. Tab "Đánh giá theo tuần" — Import là cơ chế nhập liệu CHÍNH, nhập tay là bổ sung

> **Cập nhật (làm rõ nghiệp vụ mới nhất)**: người dùng xác nhận nút
> "Import" (trước đây đọc file JSON backup của `dashboard.html`) sẽ đổi
> thành đọc file **Excel/CSV theo đúng mẫu** `doc/ERD/example_db_ver1.csv`
> và trở thành **cách nhập liệu chính** cho tab này — không còn là tính
> năng phụ/khôi phục như bản Dashboard cũ. Nút "Lưu dữ liệu"/nhập tay từng
> dòng **vẫn giữ**, dùng để bổ sung/điều chỉnh sau khi đã import (2 đường
> nhập liệu song song, không loại trừ nhau).

### 2.1. `CriteriaAssessment.CreatedAt` khi Import — [ĐÃ ĐỔI, 2026-08-12] không còn `AssessmentPeriod`/`PeriodDate`

> **Cập nhật quan trọng**: theo quyết định người dùng *"bỏ tạo kỳ mới, vì
> kỳ được xác định theo ngày tạo createdDate chứ không có tách kỳ riêng"*
> (xem `doc/ERD/ERD.md` mục "Quyết định đã CHỐT" #4 và mục "Kỳ (tuần/tháng/
> năm) — khái niệm ngầm định"), bảng `AssessmentPeriod` và nút "Tạo kỳ mới
> từ kỳ gần nhất" **đã bị bỏ hoàn toàn**. Mục này viết lại toàn bộ rule
> Import cho khớp model mới — không còn khái niệm "chọn/tạo kỳ" ở bất kỳ
> bước nào của luồng Import.

**[SUY LUẬN — chưa có mẫu file Excel thật để xác nhận cấu trúc cột, chỉ có
`example_db_ver1.csv` vốn không có cột ngày]:**

1. **Ưu tiên #1**: nếu file import có sẵn 1 cột ghi rõ ngày của kỳ báo cáo
   (vd "Ngày báo cáo"/"Kỳ") → dùng giá trị đó làm `CreatedAt` (phần ngày)
   của các `CriteriaAssessment` được ghi trong lượt import đó.
2. **Fallback (mặc định hiện tại)**: nếu file **không có** cột ngày riêng
   — đúng thực trạng của `doc/ERD/example_db_ver1.csv` (rà lại toàn bộ cột:
   Mã, Chỉ tiêu, Nhóm, Điểm tối đa, Tự đánh giá, Thẩm định, Chênh lệch,
   Trạng thái, Phụ trách, Hạn xử lý, Minh chứng/Ghi chú — **không có cột
   ngày nào**) → dùng **ngày hệ thống ghi nhận tại thời điểm import**
   (server-side, lấy phần ngày, bỏ giờ) — tức đúng ngày mà thao tác import
   thực sự xảy ra, không có gì để "chọn" cả.
3. Vì mẫu hiện có luôn rơi vào nhánh #2, đây thực chất là hành vi mặc định
   áp dụng ngay — nhưng **cần xác nhận lại khi có mẫu Excel chính thức**
   (có thể khác cấu trúc CSV mẫu ban đầu, ví dụ có thêm cột ngày).
4. **Không còn date picker nào cho luồng Import** (đã đúng từ thiết kế
   trước, nay càng đúng hơn vì không còn "kỳ" nào để chọn) — mục 2.3 (nhập
   tay) cũng **không còn** chọn ngày, xem giải thích cập nhật ở đó.
5. **Cơ chế ghi mỗi dòng file** (thay cho "upsert theo `PeriodDate`" cũ):
   với mỗi dòng CSV (ứng với 1 `CriteriaId`), áp đúng rule **"upsert-trong-
   ngày + copy-forward"** đã mô tả ở `doc/ERD/ERD.md` mục "Kỳ (tuần/tháng/
   năm)" — nếu `CriteriaId` đó **đã có** 1 `CriteriaAssessment` với
   `CreatedAt` cùng ngày import (import 2 lần cùng ngày, hoặc vừa được sửa
   tay hôm nay trước khi import) → **UPDATE** ghi đè record đó (không tạo
   record thứ 2 cùng ngày); nếu **chưa có** → tạo record mới (copy-forward
   baseline từ record gần nhất trước đó cho các field mà **chính dòng CSV
   này không cung cấp** — thực tế Import luôn cung cấp đủ 7 field nên hiếm
   khi cần copy-forward, trừ `ProgressPercent`/`Note` nếu dòng CSV không có
   giá trị tương ứng, xem mục 2.2).

### 2.2. Mapping cột file Import → entity

| Cột file (theo `example_db_ver1.csv`) | Map vào | Field | Ghi chú |
| --- | --- | --- | --- |
| Mã | `Criteria` | `Code` | Khoá để match `Criteria` đã có trong danh mục (theo `Code`, trong tập chưa xoá mềm) |
| Chỉ tiêu | `Criteria` | `Name` | Chỉ dùng khi **tạo `Criteria` mới** — nếu `Code` đã tồn tại, **không** ghi đè `Name` hiện có qua import **[SUY LUẬN]** (tránh import vô tình sửa danh mục ngoài ý muốn — sửa `Name` nên đi qua CRUD tường minh ở tab "Chỉ tiêu") |
| Nhóm | `Criteria` | `GroupId` | Resolve theo tên nhóm khớp `CriteriaGroup.Name` — **✅ Đã chốt (2026-08-12 vòng 3)**: nếu không khớp nhóm nào đã có → **tự động tạo `CriteriaGroup` mới** (cùng tinh thần "hứng đủ dữ liệu, không mất data" đã áp dụng cho `Criteria` ở câu #5), map `Name` ← tên nhóm trong file, `Code` tự sinh (số nguyên lớn nhất trong các `Code` hiện có + 1, vì file không có cột mã nhóm riêng), `DisplayOrder` nối vào cuối. Đây là **thay đổi quyết định** so với bản trước (từng suy luận "báo lỗi, không tự tạo") — xem mục 5 câu hỏi #10 |
| Điểm tối đa | `Criteria` | `MaxScore` | Tương tự `Name` — chỉ áp dụng khi tạo `Criteria` mới |
| Tự đánh giá | `CriteriaAssessment` (kỳ import) | `SelfScore` | **✅ Đã chốt: ghi đè theo đúng nội dung file** — xem quyết định ở dưới (không còn tách biệt "field tĩnh") |
| Thẩm định | `CriteriaAssessment` (kỳ import) | `VerifiedScore` | Tương tự — ghi đè theo file |
| Chênh lệch | — | *(không lưu cột riêng)* | Chỉ dùng **cross-check**: nếu khác `Tự đánh giá - Thẩm định` tính lại → cảnh báo dòng đó, không chặn import **[SUY LUẬN]** |
| Trạng thái | `CriteriaAssessment` (kỳ import) | `Status` | Ghi đè theo file — tương tự |
| Phụ trách | `CriteriaAssessment` (kỳ import) | `OwnerId` | Cột là **text tên người**, field DB là FK `AppUser.Id` — ghi đè theo file. **✅ Đã chốt (2026-08-12 vòng 3)**: resolve theo `AppUser.FullName` khớp chính xác (trim); **chưa từng có** `AppUser` nào tên này → **tự động tạo mới**; **đã có nhưng trùng tên ≥2 user** (ambiguous, không rõ chọn ai) → giữ `OwnerId = null`, KHÔNG tự đoán/tự tạo thêm bản trùng. Xem mục 5 câu hỏi #8 |
| Hạn xử lý | `CriteriaAssessment` (kỳ import) | `Deadline` | Ghi đè theo file |
| Minh chứng/Ghi chú | `CriteriaEvidence` (gắn vào `CriteriaAssessment` kỳ import) | `Content` (tách theo dòng bắt đầu `"*"`, đúng rule đã có ở `doc/ERD/ERD.md` mục 5) | **Không** map vào `CriteriaAssessment.Note` — đó là "Ghi chú tuần" tự do, khái niệm khác; mẫu file hiện không có cột riêng cho "Ghi chú tuần" nên field này để trống sau import, chỉ điền được qua nhập tay (mục 2.3) |
| *(không có trong mẫu — "Tiến độ %")* | `CriteriaAssessment` (kỳ import) | `ProgressPercent` | Mẫu CSV **không có cột % tiến độ tuần** — đề xuất tính theo đúng công thức seed đã có ở `doc/ERD/ERD.md` (`ProgressPercent = SelfScore / MaxScore × 100`, kẹp `[0,100]`) **[SUY LUẬN]**; nếu mẫu Excel thật có cột riêng, ưu tiên đọc trực tiếp thay vì suy ra |

**✅ Đã chốt (người dùng xác nhận) — Import GHI ĐÈ TOÀN BỘ, không bảo vệ
riêng field nào:**

Khi import lại **đúng 1 ngày đã có dữ liệu** (trùng phần ngày của
`CreatedAt` — **[ĐÃ ĐỔI, 2026-08-12]** trước đây gọi là "trùng
`PeriodDate`"), toàn bộ dữ liệu dòng đó trong `CriteriaAssessment` được
**ghi đè theo đúng nội dung file** — bao gồm cả `SelfScore`/`VerifiedScore`/
`Status`/`OwnerId`/`Deadline` (không giữ nguyên giá trị cũ, không tách biệt
"field tĩnh" nữa cho luồng này). Nói cách khác: **1 file import trong 1
ngày = 1 snapshot đầy đủ cho đúng ngày đó**; import lại cùng ngày = ghi đè
hoàn toàn snapshot đó (UPDATE record đã có, giữ nguyên `CreatedAt` gốc).
Import vào ngày khác thì tạo bản ghi mới (copy-forward baseline rồi ghi đè
theo file, xem mục 2.1 #5), **không** so sánh/kiểm tra trùng với ngày khác.

**Quan hệ với quyết định cũ (`spec/dashboard-dti-weekly/business-rules.md`
mục 5 — "`SelfScore`/`VerifiedScore`/`Status` tĩnh, chỉ từ quy trình thẩm
định riêng, không sửa qua luồng tuần"): KHÔNG còn mâu thuẫn, vì quyết định
mới này chỉ ghi đè quyết định cũ cho riêng luồng Import.** Quyết định cũ
**vẫn đúng nguyên vẹn** cho luồng nhập tay/"Lưu dữ liệu" ở mục 2.3 — UI
nhập tay tiếp tục **không có** control nào cho 5 field này (đúng như
`dashboard.html` gốc), nên nhập tay chỉ có thể sửa `ProgressPercent`/
`Note`, không đụng được `SelfScore`/`VerifiedScore`/`Status`/`OwnerId`/
`Deadline`. Chỉ luồng Import mới có khả năng (và nay đã chốt: sẽ) ghi đè 5
field đó.

**Quy tắc "Code lạ" — `Criteria` chưa có trong danh mục khi import:**

**✅ Đã chốt (người dùng xác nhận) — TỰ ĐỘNG TẠO `Criteria` mới**, không
báo lỗi/bỏ qua. Người dùng nhấn mạnh ưu tiên "đảm bảo hứng được toàn bộ các
field trong file CSV" — không đánh đổi mất dữ liệu để lấy an toàn quy
trình. Rule cụ thể: khi gặp `Code` chưa có trong danh mục (trong tập chưa
xoá mềm), tự động tạo `Criteria` mới ngay trong cùng lượt import, map:
- `Code` ← cột "Mã"
- `Name` ← cột "Chỉ tiêu"
- `GroupId` ← resolve theo tên nhóm ở cột "Nhóm" khớp `CriteriaGroup.Name`
  đã seed sẵn
- `MaxScore` ← cột "Điểm tối đa"

...rồi tiếp tục ghi `CriteriaAssessment` cho `Criteria` vừa tạo như bình
thường (không phải 2 bước tách rời — tạo `Criteria` và ghi
`CriteriaAssessment` xảy ra trong cùng 1 giao dịch import).

**Câu hỏi mới phát sinh từ chính quyết định này**: nếu tên nhóm ở cột
"Nhóm" trong file **cũng không khớp** bất kỳ `CriteriaGroup.Name` nào đã
seed (nhóm lạ, không chỉ mã lạ) → xử lý sao? Vì mục 5 câu hỏi #3 đã chốt
"KHÔNG CRUD `CriteriaGroup` ở màn này", nhiều khả năng **không** nên tự
tạo `CriteriaGroup` mới ngầm theo cùng tinh thần "hứng đủ dữ liệu" ở trên
(rủi ro tạo nhóm rác do lỗi chính tả) — nhưng đây là suy luận, **chưa được
người dùng xác nhận trực tiếp** cho đúng trường hợp "nhóm lạ" (khác với
"mã lạ" đã được xác nhận) — xem câu hỏi mở #10.

### 2.3. Nhập tay / điều chỉnh sau import — vẫn giữ, không thay thế Import

Nút "Lưu dữ liệu"/sửa trực tiếp từng ô (`ProgressPercent`/`Note`) **vẫn giữ
nguyên** ở tab "Đánh giá theo tuần", dùng để bổ sung/điều chỉnh sau khi đã
import — không bắt buộc bỏ nhập tay. Validate (ép kẹp `[0,100]`) **giữ
nguyên y hệt** như đã tài liệu hoá ở `spec/dashboard-dti-weekly/business-rules.md`
mục 2.4/3.

**[ĐÃ ĐỔI, 2026-08-12] Không còn chọn `PeriodDate`/"kỳ đang sửa" qua date
picker.** Đây là hệ quả trực tiếp của việc bỏ `AssessmentPeriod`: nhập tay
**luôn** ghi vào **hôm nay** — bấm sửa 1 ô của 1 chỉ tiêu áp đúng rule
"upsert-trong-ngày + copy-forward" ở mục 2.1 #5 (nếu chỉ tiêu đó **đã có**
record hôm nay → cập nhật field đang sửa vào chính record đó; nếu **chưa
có** → copy-forward toàn bộ 7 field từ record gần nhất trước đó làm baseline,
rồi áp giá trị vừa sửa lên trên, tạo record mới với `CreatedAt = hôm nay`).
Người dùng **không còn khả năng** "chỉnh sửa vào một ngày quá khứ cụ thể"
qua UI này nữa (khác hành vi cũ, nơi date picker cho chọn ngày tuỳ ý) — nếu
cần sửa lại dữ liệu của một ngày đã qua, cách duy nhất là Import lại đúng
ngày đó qua file có cột ngày (mục 2.1 #1); **không có UI** cho việc sửa trực
tiếp một ngày quá khứ ở v1 — ghi nhận là giới hạn đã biết, không phải thiếu
sót.

Điểm không đổi từ trước: danh sách chỉ tiêu hiển thị để nhập ở tab này phải
lấy từ tập `Criteria` **active** (`IsDeleted = false`) tại thời điểm thao
tác — xem mục 1.3 rule #3.

### 2.4. [MỚI, 2026-08-12 vòng 2] Bộ lọc Năm/"Tất cả"/Tuần/Tháng — grid chuyển READ-ONLY khi xem quá khứ

> ✅ **Quyết định chính thức** (đề xuất bởi `backend-expert`, xác nhận cần
> thiết bởi `main` — không phải suy luận bỏ ngỏ). Ghi rõ ở đây theo đúng yêu
> cầu formalize thành rule chính thức.

**Bối cảnh**: theo quyết định người dùng (2026-08-12 vòng 2, xem
`doc/ERD/ERD.md` mục "Kỳ (tuần/tháng/năm)"), cả Danh mục DTI lẫn Dashboard
đều bắt buộc có 1 bộ lọc **Năm** làm phạm vi nền tảng (mặc định = năm hiện
tại), với "Tất cả" = toàn bộ dữ liệu trong năm đó (chưa thu hẹp thêm theo
tuần/tháng). Điều này có nghĩa **grid ở màn Danh mục DTI giờ có khả năng
hiển thị dữ liệu LỊCH SỬ** (khi người dùng đổi sang năm khác, hoặc thu hẹp
về 1 tuần/tháng cụ thể) — trong khi rule ghi dữ liệu (mục 2.1 #5, mục 2.3 ở
trên) **luôn và chỉ** tác động vào bản ghi của **hôm nay**, bất kể đang xem
gì. Nếu cứ để control sửa (✓/✗, "+Thêm chỉ tiêu", "Sửa", "Xoá", "Import
CSV") hoạt động bình thường trong khi grid đang hiển thị dữ liệu quá khứ,
người dùng sẽ hiểu lầm là đang sửa đúng bản ghi lịch sử đang nhìn thấy,
trong khi thực chất thao tác lưu sẽ tạo/ghi đè bản ghi **hôm nay** — vi phạm
ngầm tính bất biến của snapshot lịch sử.

**Rule chính thức:**

1. **Trạng thái "Live" (cho phép sửa)** = grid đang ở đúng **"Tất cả" của
   NĂM HIỆN TẠI** (năm chứa ngày hôm nay), **chưa** thu hẹp thêm theo tuần/
   tháng nào. Đây là trạng thái mặc định khi mở trang.
2. **Trạng thái "Lịch sử" (READ-ONLY)** = bất kỳ trạng thái nào khác trạng
   thái Live ở trên — cụ thể:
   - Đã chọn 1 **năm khác** năm hiện tại (kể cả nếu chọn lại đúng "Tất cả"
     của năm đó).
   - Đang ở năm hiện tại nhưng đã **thu hẹp về 1 tuần/tháng cụ thể** (kể cả
     nếu tuần/tháng đó là tuần/tháng chứa hôm nay) — cố tình đơn giản hoá
     theo hướng "hễ đã thu hẹp là xem lịch sử", tránh phải phân biệt thêm
     trường hợp đặc biệt "tuần hiện tại" (dễ gây rối UX hơn là lợi ích mang
     lại).
3. **Khi ở trạng thái READ-ONLY, TOÀN BỘ hành động ghi trên trang bị khoá**,
   không chỉ riêng ✓/✗ sửa inline:
   - Ẩn/khoá 2 icon ✓/✗ sửa Tiến độ %/Ghi chú (chỉ hiển thị text tĩnh, giống
     hành vi Dashboard read-only đã có).
   - Ẩn/khoá "+ Thêm chỉ tiêu", "Sửa", "Xoá" (CRUD `Criteria`) — **mặc dù**
     bản thân `Criteria` không có lịch sử theo ngày (chỉ có soft-delete),
     việc cho sửa danh mục "sống" trong khi mắt đang nhìn dữ liệu "đóng
     băng" của quá khứ dễ gây hiểu lầm nghiêm trọng hơn lợi ích tiện dụng —
     đây là lựa chọn ưu tiên **nhất quán UX**, không phải giới hạn kỹ thuật
     tầng dữ liệu.
   - Ẩn/khoá nút "Import CSV" — vì Import cũng luôn ghi vào hôm nay (mục
     2.1 #5), cùng lý do như trên.
4. **Thông báo rõ cho người dùng** khi ở chế độ READ-ONLY (vd banner "Đang
   xem dữ liệu lịch sử — quay lại 'Tất cả' của năm hiện tại để chỉnh sửa")
   — chi tiết UI cụ thể do `frontend-expert` thiết kế, rule này chỉ quy định
   **điều kiện kích hoạt** và **phạm vi bị khoá**.

**Không áp dụng cho Dashboard** — Dashboard vốn đã 100% read-only từ trước
(không có action ghi nào), nên rule này không có gì thay đổi thêm ở đó.

**[ĐÃ ĐỔI NGUỒN, 2026-08-12]** Trước đây đơn vị lưu trữ là **tuần** qua
bảng `AssessmentPeriod` (1 record = 1 tuần tường minh). Bảng đó **đã bị
bỏ** — nay "kỳ-tuần" **không còn là 1 record cụ thể nào**, mà là **kết quả
group-by** các `CriteriaAssessment.CreatedAt` theo tuần ISO (xem
`doc/ERD/ERD.md` mục "Kỳ (tuần/tháng/năm)"). "Tháng" (và tiềm năng "năm"
sau này) vẫn đúng tinh thần cũ — **chỉ là lớp group-by rộng hơn khi truy
vấn/báo cáo**, không phải bảng/cột riêng nào — chỉ khác nguồn group-by đổi
từ `AssessmentPeriod.PeriodDate` sang `CriteriaAssessment.CreatedAt`.

**✅ Đã chốt (người dùng xác nhận) — công thức tổng hợp N kỳ-tuần trong 1
tháng = TRUNG BÌNH CỘNG (không phải lấy kỳ gần cuối tháng), công thức KHÔNG
đổi bởi việc bỏ `AssessmentPeriod`:**

```
TiếnĐộTháng(Criteria x, tháng M) = trung bình( ProgressPercent(x, kỳ-tuần k) )
                                     với mọi kỳ-tuần k có ít nhất 1 CriteriaAssessment.CreatedAt của x thuộc tháng M
```

**✅ Đã chốt (2026-08-12 vòng 2) — KHÔNG carry-forward.** Chỉ những kỳ-tuần
**thực sự có** record cho chính chỉ tiêu `x` mới được đưa vào phép trung
bình — kỳ-tuần nào chỉ tiêu `x` không có record thì **loại hẳn khỏi phép
tính** (không tính là 0%, không tự điền giá trị từ tuần trước). Nếu 1 chỉ
tiêu **không có kỳ-tuần nào** có dữ liệu trong cả tháng M → `TiếnĐộTháng(x,
M) = "chưa có dữ liệu"` ("—"), không suy ra 0%. Đây là cùng nguyên tắc với
cách tính "Tiến độ chung" cấp tuần đã viết lại ở
`spec/dashboard-dti-weekly/business-rules.md` mục 3.3 — **2 công thức (tuần
và tháng) nay xử lý dữ liệu thiếu THỐNG NHẤT** (khác bản trước khi carry-
forward còn là đề xuất mở, từng lo ngại 2 công thức lệch nhau).

Áp dụng ở **cả 2 cấp**:
1. **Theo từng `Criteria`**: trung bình cộng `ProgressPercent` của chính
   chỉ tiêu đó qua các kỳ-tuần thuộc tháng M.
2. **Tổng hợp toàn danh mục** (KPI "Tiến độ chung tháng"): trung bình cộng
   giá trị **"Tiến độ chung"** (đã tính theo công thức bình quân gia quyền
   `Σ MaxScore` ở `spec/dashboard-dti-weekly/business-rules.md` mục 3.3)
   của từng kỳ-tuần thuộc tháng M — nghĩa là tính "Tiến độ chung" cho mỗi
   kỳ-tuần trước (như đã có), rồi lấy trung bình cộng các kết quả đó theo
   tháng (không phải gộp thẳng toàn bộ `CriteriaAssessment` của cả tháng
   vào 1 công thức bình quân gia quyền lớn — 2 cách tính này cho kết quả
   khác nhau khi số kỳ-tuần không đều giữa các chỉ tiêu, và trung bình của
   trung bình theo từng kỳ mới đúng tinh thần "trung bình cộng các kỳ-tuần
   trong tháng" đã chốt).

**So sánh giữa các tháng** = hiệu 2 số trung bình tháng liên tiếp
(`TiếnĐộTháng(M) - TiếnĐộTháng(M-1)`), dùng cùng logic delta + ngưỡng
epsilon `0.001` đã có ở `spec/dashboard-dti-weekly/business-rules.md`
mục 3.2 (tăng/giảm/không đổi).

- **✅ Đã chốt (2026-08-12 vòng 2) — mở rộng "theo năm" ("Tất cả" của 1
  năm)**: cùng công thức, cùng nguyên tắc loại trừ dữ liệu thiếu — trung
  bình cộng các kỳ-tuần **có dữ liệu** trong năm đó (`TiếnĐộNăm(x, năm Y) =
  trung bình( ProgressPercent(x, kỳ-tuần k) )` với mọi kỳ-tuần k thuộc năm Y
  **có** record cho `x`). Đây chính là công thức dùng khi Dashboard/Danh
  mục DTI ở trạng thái **"Tất cả"** của 1 năm (xem `doc/ERD/ERD.md` mục "Kỳ
  (tuần/tháng/năm)" — không còn "chưa thiết kế chi tiết" như bản trước, nay
  đã là rule chính thức vì "Tất cả" đã chốt nghĩa = phạm vi năm).
- **Tháng/Năm không có dữ liệu** (không có kỳ-tuần nào rơi vào đó): hiển
  thị "chưa có dữ liệu", **không** suy ra bằng 0% hay nội suy từ kỳ liền
  kề.

## 4. Permission — CRUD `Criteria`

Chưa đủ thông tin nghiệp vụ để chốt role cụ thể — giống placeholder đã ghi
ở `spec/dashboard-dti-weekly/business-rules.md` mục 6, không tự bịa role.
Câu hỏi cụ thể: xem mục 5.

## 5. Câu hỏi còn mở — cần người dùng quyết định, KHÔNG tự chốt

### Đã chốt/tạm chốt (người dùng quyết định)

1. **[TẠM CHỐT MẶC ĐỊNH — không phải đã hỏi & xác nhận tường minh]**
   Công thức "Tiến độ chung"/"Tiến độ theo nhóm" khi danh mục `Criteria`
   không còn tĩnh: áp dụng theo đề xuất ban đầu — mẫu số `Σ MaxScore` dùng
   **danh mục `Criteria` HIỆN TẠI cho MỌI kỳ** (kể cả kỳ lịch sử), đơn giản
   hơn snapshot-theo-kỳ. Người dùng xác nhận dùng tạm phương án này vì
   *"chưa hình dung rõ câu hỏi kỹ thuật này"* — ghi rõ đây là **quyết định
   mặc định, có thể xem lại sau** khi có nhu cầu rõ ràng hơn, không phải đã
   được giải thích đầy đủ và chốt cứng vĩnh viễn.
2. **✅ Đã chốt**: unique `Code` chỉ áp dụng trong tập **chưa xoá mềm** —
   cho phép tái dùng `Code` sau khi xoá. Xem mục 1.1.
3. **✅ Đã chốt**: `CriteriaGroup` **KHÔNG** cần CRUD ở màn "Danh mục > DTI"
   — "Nhóm" chỉ là dropdown chọn 1 trong 6 `CriteriaGroup` đã seed sẵn từ
   CSV, không phải mục tiêu CRUD của nhiệm vụ này.
4. **✅ Đã chốt**: Import **ghi đè toàn bộ** `CriteriaAssessment` của
   **ngày** trùng phần ngày của `CreatedAt` (trước đây gọi "trùng
   `PeriodDate`" — nay đã bỏ `AssessmentPeriod`, xem mục 2.1) theo đúng nội
   dung file (kể cả `SelfScore`/`VerifiedScore`/`Status`/`OwnerId`/
   `Deadline`), không bảo vệ riêng field nào — "1 file import trong 1 ngày =
   1 snapshot đầy đủ cho đúng ngày đó". Quyết định cũ ("5 field tĩnh, chỉ từ
   quy trình thẩm định riêng") vẫn đúng cho luồng nhập tay (mục 2.3, UI
   không có control cho 5 field này), chỉ đổi cho riêng luồng Import. Xem
   mục 2.2.
5. **✅ Đã chốt**: gặp `Code` lạ khi import (chưa có trong danh mục) →
   **tự động tạo `Criteria` mới** (map `Name`/`GroupId` resolve theo tên
   nhóm trong file/`MaxScore` từ file), rồi ghi `CriteriaAssessment` bình
   thường trong cùng giao dịch import — ưu tiên không mất dữ liệu, không
   báo lỗi/bỏ qua. Xem mục 2.2. **Phát sinh câu hỏi mới từ chính quyết định
   này** — xem câu hỏi mở #10.
6. **✅ Đã chốt**: công thức tổng hợp theo tháng = **TRUNG BÌNH CỘNG** các
   kỳ-tuần thuộc tháng đó (không phải lấy kỳ gần cuối tháng), áp dụng cho
   cả cấp từng `Criteria` và cấp tổng hợp toàn danh mục. So sánh giữa các
   tháng = hiệu 2 số trung bình liên tiếp, cùng ngưỡng epsilon đã có. Xem
   mục 3.

### Còn mở (chưa chốt)

7. **Permission CRUD `Criteria`**: role nào được Create/Update/Delete —
   đặc biệt: role nào được xoá (kể cả soft-delete) một `Criteria` đã có
   lịch sử đánh giá? Chưa có role Identity nào được chốt (xem
   `doc/ERD/ERD.md` mục "Câu hỏi còn mở",
   `spec/dashboard-dti-weekly/business-rules.md` mục 6) — placeholder,
   không tự bịa role.
8. **✅ Đã chốt (2026-08-12 vòng 3)** — xem mục "Đã chốt (2026-08-12, vòng 3)"
   bên dưới.
9. **Mẫu file Import thật (Excel) có cột ngày riêng cho kỳ báo cáo không?**
   `doc/ERD/example_db_ver1.csv` hiện tại **không có** cột ngày — mục 2.1
   tạm mặc định dùng ngày hệ thống lúc import làm phần ngày của `CreatedAt`.
   Cần xác nhận lại khi có mẫu Excel chính thức (có thể khác cấu trúc CSV
   mẫu ban đầu).
10. **✅ Đã chốt (2026-08-12 vòng 3)** — xem mục "Đã chốt (2026-08-12, vòng 3)"
    bên dưới.

### ✅ Đã chốt (2026-08-12, vòng 3) — Import tự tạo dữ liệu nền còn thiếu (Nhóm, Người phụ trách)

> **Quyết định người dùng (nguyên văn)**: *"nếu CriteriaGroup chưa tồn tại
> thì hãy tạo mới cho dòng đó vào CriteriaGroup"*, mở rộng thành nguyên tắc
> chung: *"khi import thì thông tin các bảng liên quan nếu chưa có sẽ được
> thêm mới đúng theo thông tin được import"*. Đây là **thay đổi quyết định**
> so với suy luận trước đó ở câu hỏi #10 (từng nghiêng về báo lỗi để tránh
> tạo nhóm rác do lỗi chính tả) — người dùng chấp nhận đánh đổi đó để ưu
> tiên tuyệt đối "không mất dữ liệu khi import", cùng tinh thần đã chốt cho
> `Criteria` ở câu #5.

15. **Nhóm lạ (câu hỏi #10 cũ)**: khi tạo `Criteria` mới mà tên ở cột "Nhóm"
    không khớp `CriteriaGroup.Name` nào đã có → **tự động tạo `CriteriaGroup`
    mới** trong cùng giao dịch import (không còn báo lỗi/bỏ qua dòng đó).
    `Code` của nhóm mới tự sinh (số nguyên lớn nhất trong các `Code` hiện có
    + 1) vì file CSV không có cột mã nhóm riêng; `DisplayOrder` nối vào cuối
    danh sách nhóm hiện có. **Rủi ro đã biết, được người dùng chấp nhận**:
    tên nhóm gõ sai chính tả giữa các lần import sẽ tạo nhóm trùng/gần trùng
    thay vì báo lỗi rõ ràng — không có cơ chế fuzzy-match, chỉ so khớp chính
    xác (trim, không phân biệt hoa/thường). Xem mục 2.2.
16. **Phụ trách không khớp `AppUser` nào (câu hỏi #8 cũ)**: resolve theo
    `AppUser.FullName` khớp chính xác — **chưa từng có** user nào tên này
    → **tự động tạo `AppUser` mới** (chỉ có `FullName`, không có field auth
    nào khác — entity `AppUser` ở bản này **không phải** ASP.NET Core
    Identity thật, xem comment tại `Entities/AppUser.cs`); **đã có nhưng
    trùng tên ≥2 user** (ambiguous) → **giữ nguyên hành vi cũ**, để
    `OwnerId = null`, không tự đoán chọn user nào (khác trường hợp "chưa
    có" — trường hợp này dữ liệu đã tồn tại, chỉ là không rõ ý người nhập).
    Xem mục 2.2.
    **Lưu ý khi nâng cấp lên ASP.NET Core Identity thật** (theo hướng đã
    chốt ở `doc/ERD/ERD.md` mục "Quyết định đã CHỐT" #3, chưa triển khai ở
    bản này): quyết định tự-tạo-user-từ-text-tên ở đây **chỉ phù hợp cho
    bản demo** hiện tại (không có password/email/login) — khi `AppUser`
    chuyển sang `IdentityUser<Guid>` thật, cần xem lại rule này (không thể
    tự tạo tài khoản đăng nhập hợp lệ chỉ từ 1 cột tên trong CSV, cần
    email tối thiểu + quy trình cấp mật khẩu/kích hoạt riêng).

### ✅ Đã chốt (2026-08-12, vòng 2) — 4 câu hỏi phát sinh từ việc bỏ `AssessmentPeriod` NAY ĐÃ CÓ QUYẾT ĐỊNH

> Không còn là câu hỏi mở — người dùng đã chốt cả 4 điểm. Chi tiết đầy đủ:
> `doc/ERD/ERD.md` mục "Câu hỏi còn mở" phần "Đã chốt (2026-08-12, vòng 2)".
> Tóm tắt phần ảnh hưởng trực tiếp tới màn "Danh mục DTI":

11. **Quy ước chia "tuần" = tuần ISO (thứ Hai–Chủ Nhật)** — đã xác nhận
    đúng đề xuất mặc định.
12. **Carry-forward: KHÔNG** — trái ngược đề xuất mặc định ban đầu của
    `backend-expert` (từng đề xuất CÓ). Kỳ/tuần/tháng/năm không có thao tác
    cho 1 chỉ tiêu → hiển thị "—", loại khỏi công thức trung bình (xem mục
    3 đã viết lại). Grid "Danh mục DTI" ở trạng thái Live (xem #13) hiển thị
    đúng những gì có, không tự bơm giá trị cũ vào ô trống.
13. **Định nghĩa "Tất cả" = toàn bộ dữ liệu trong 1 NĂM được chọn** (khác cả
    2 đề xuất cũ: không phải "trạng thái hiện hành", không phải "toàn bộ
    lịch sử vô hạn") — Danh mục DTI cần bộ lọc **Năm** (mặc định năm hiện
    tại), "Tất cả" = chưa lọc thêm tuần/tháng trong năm đó. Hệ quả: grid có
    thể hiển thị **nhiều dòng/1 chỉ tiêu** trong năm (xem mục 2.4 — rule
    READ-ONLY mới) — khác hẳn giả định "1 dòng/chỉ tiêu" trước đây.
14. **`CriteriaEvidence` KHÔNG copy-forward** — xác nhận đúng đề xuất tạm
    trước đó.

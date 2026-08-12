# Business Rules — DTI Weekly Dashboard

> Nghiệp vụ phía dữ liệu của màn hình `doc/Prototype/dashboard.html` (theo
> dõi tiến độ chuyển đổi số hàng tuần). Tài liệu này bổ sung cho
> `doc/ERD/ERD.md` (mô tả entity/quan hệ) — tập trung vào **validation,
> công thức tính toán, quy tắc nghiệp vụ**. Không lặp lại phần trích dẫn
> nguồn đã có ở ERD.md, chỉ tóm tắt và link ngược.
>
> Song song có `spec/dashboard-dti-weekly/ui-spec.md` (do `frontend-expert`
> viết) mô tả UI/UX của cùng màn hình — 2 file nên đọc cùng nhau.
>
> Quy ước trích dẫn: mọi rule đều ghi rõ **nguồn** (CSV / JS hàm nào) hoặc
> đánh dấu **[SUY LUẬN]** nếu không có bằng chứng trực tiếp trong 2 nguồn
> gốc (CSV, dashboard.html) — không bịa rule mà không gắn nhãn.

## 0. Cập nhật phạm vi UI (quyết định kiến trúc mới — Dashboard đổi read-only)

> **Quyết định người dùng (mới nhất, ghi đè giả định trước đó về Dashboard
> là nơi nhập liệu):**
> 1. **Dashboard (`doc/Prototype/dashboard.html` gốc) đổi thành CHỈ XEM
>    (read-only)** — bỏ toàn bộ khả năng nhập liệu trực tiếp trên màn hình
>    này: không còn `input.progressInput`, `input.noteInput`, nút "Lưu dữ
>    liệu"/`.fab` "Lưu". Dashboard chỉ còn hiển thị: KPI, tiến độ theo nhóm,
>    biểu đồ xu hướng, bảng 62 chỉ tiêu (đọc), lịch sử các kỳ đã lưu, "Báo
>    cáo nhanh".
> 2. **Chức năng nhập liệu chuyển sang màn hình mới "Danh mục > DTI"**
>    (nested dưới menu cha "Danh mục" trong sidebar — xem
>    `spec/sidebar-menu/ui-spec.md`), tab **"Đánh giá theo tuần"**. Toàn bộ
>    **entity, validation, công thức tính (mục 1–5 dưới đây)** của tài liệu
>    này **không đổi bản chất** — chỉ đổi UI nào đang gọi tới chúng. Chi
>    tiết đầy đủ cho màn hình mới: `spec/danh-muc-dti/business-rules.md`.
> 3. Màn hình mới cũng có tab **"Chỉ tiêu"** — CRUD danh mục `Criteria`
>    (trước đây không có CRUD, `Criteria` chỉ được seed tĩnh từ CSV mẫu) —
>    rule CRUD nằm hoàn toàn ở `spec/danh-muc-dti/business-rules.md`, không
>    lặp lại ở đây.
>
> **Vì sao giữ nguyên tài liệu này thay vì viết lại**: mục 1–5 mô tả rule ở
> **tầng entity/dữ liệu** (`CriteriaAssessment`) — các rule này đúng bất kể
> UI nào gọi tới (trước đây là Dashboard, nay là tab "Đánh giá theo tuần").
> Tách write-path sang màn khác không làm thay đổi công thức delta hay
> validation field — tài liệu này tiếp tục là **nguồn tham chiếu duy nhất**
> cho các rule đó. **[CẬP NHẬT 2026-08-12]** Riêng "quy tắc upsert" **đã đổi
> hẳn nguồn** — không còn `AssessmentPeriod`/upsert theo `PeriodDate`, thay
> bằng upsert-trong-ngày trực tiếp trên `CriteriaAssessment` (xem mục 4) —
> đây là thay đổi kiến trúc, không phải chỉ đổi UI, nên mục 4 đã viết lại
> hoàn toàn (không còn "giữ nguyên bản chất" như các mục khác).

## 1. Tóm tắt entity (trích `doc/ERD/ERD.md`)

Chi tiết đầy đủ + trích dẫn nguồn: xem `doc/ERD/ERD.md` và
`doc/ERD/PlatformManager.dbml`. Tóm tắt nhanh:

| Entity | Vai trò | Field chính |
| --- | --- | --- |
| `AppUser` | Người dùng hệ thống (ASP.NET Core Identity) | `Id`, `UserName`, `Email`, `FullName` |
| `CriteriaGroup` | Danh mục 6 nhóm chỉ tiêu (tĩnh) | `Code` ("1".."6"), `Name`, `DisplayOrder` |
| `Criteria` | Danh mục 62 chỉ tiêu (tĩnh, không đổi theo kỳ) | `Code`, `Name`, `GroupId`, `MaxScore` |
| `CriteriaAssessment` | 1 bản ghi đánh giá của 1 chỉ tiêu, gắn 1 mốc thời gian — bảng trung tâm | `CriteriaId`, `ProgressPercent`, `SelfScore`, `VerifiedScore`, `Status`, `OwnerId`, `Deadline`, `Note`, `CreatedAt` (**= "kỳ" của record**) |
| `CriteriaEvidence` | Danh sách minh chứng (1–N) của 1 `CriteriaAssessment` | `Content`, `OrderIndex` |

> **[ĐÃ BỎ, 2026-08-12]** `AssessmentPeriod` không còn tồn tại — "kỳ"
> (tuần/tháng/năm) nay suy ra từ việc nhóm `CriteriaAssessment.CreatedAt`
> theo thời gian, không còn bảng/entity riêng. Xem
> `doc/ERD/ERD.md` mục "Quyết định đã CHỐT" #4 và mục "Kỳ (tuần/tháng/năm)
> — khái niệm ngầm định" — đọc kỹ trước khi áp dụng bất kỳ rule nào bên
> dưới có nhắc tới "kỳ".

## 2. Validation rule theo field

### 2.1 `CriteriaGroup`

| Field | Rule | Nguồn |
| --- | --- | --- |
| `Code` | Required, unique, maxlength 10 | JS: `DTI_ITEMS[].group` chỉ có giá trị "1".."6" |
| `Name` | Required, maxlength 200 | CSV cột "Nhóm" |
| `DisplayOrder` | >= 0, default 0 | **[SUY LUẬN]** — phục vụ hiển thị đúng thứ tự 1→6 như JS `renderGroups()` |

### 2.2 `Criteria`

| Field | Rule | Nguồn |
| --- | --- | --- |
| `Code` | Required, unique, maxlength 20 | CSV cột "Mã" / JS `DTI_ITEMS[].id` |
| `Name` | Required, không giới hạn ngắn (kiểu `text`) — mã dài nhất quan sát được (`4.22.1`) chứa cả xuống dòng liệt kê 3 mục con | CSV cột "Chỉ tiêu" |
| `GroupId` | Required, FK phải tồn tại và chưa bị xoá mềm | Suy ra từ quan hệ `CriteriaGroup` 1–N `Criteria` |
| `MaxScore` | Required, `> 0` | CSV cột "Điểm tối đa" (giá trị quan sát được: 10, 20, 30); rule `> 0` khớp `src/BE/.claude/rules/entity-domain.md` — `DomainException("CRITERIA_MAX_SCORE_INVALID", ...)` |

**[SUY LUẬN]** Định dạng `Code` quan sát được trong 62 mã mẫu là dạng số
cách nhau bởi dấu chấm (`"1.1"`, `"4.22.10"`, `"4.22.11"`) — có thể dùng làm
gợi ý regex khi validate input, nhưng **không nên** khoá cứng thành rule
bắt buộc vì đây chỉ là quan sát từ 62 dòng mẫu, không phải quy tắc được 2
nguồn công bố tường minh.

### 2.3 ~~`AssessmentPeriod`~~ — ĐÃ BỎ (2026-08-12)

Không còn entity/bảng này — xem banner đầu mục 1. Rule unique "1 ngày chỉ
có tối đa 1 bản ghi/chỉ tiêu" chuyển thành 1 unique constraint ngay trên
`CriteriaAssessment`, xem mục 2.4.

### 2.4 `CriteriaAssessment`

| Field | Rule | Nguồn |
| --- | --- | --- |
| `CriteriaId` | Required, FK phải tồn tại | — |
| `CreatedAt` (kế thừa `BaseEntity`) | **[ĐÃ ĐỔI, 2026-08-12]** Required, bất biến sau khi insert — phần ngày của giá trị này chính là "kỳ" của record | `doc/ERD/ERD.md` mục "Kỳ (tuần/tháng/năm)" |
| `(CriteriaId, CAST(CreatedAt AS date))` | **[ĐÃ ĐỔI, 2026-08-12]** Unique (thay cho `(CriteriaId, PeriodId)` cũ) — 1 chỉ tiêu chỉ có đúng 1 bản ghi/ngày có thao tác | JS: `draft.values`/`historyData[].values` là dictionary keyed theo `criteria.id`, 1 giá trị/kỳ — nay "kỳ" = ngày |
| `ProgressPercent` | Required, **kẹp trong khoảng [0, 100]**; giá trị không phải số → mặc định 0 | JS `setProgress()`: `Math.max(0,Math.min(100,Number(val)\|\|0))` |
| `SelfScore` | Nullable. **[SUY LUẬN]** nếu có giá trị: `0 ≤ SelfScore ≤ Criteria.MaxScore` | Không có UI chỉnh sửa field này trong `dashboard.html` (chỉ là giá trị tĩnh seed từ `DTI_ITEMS[].sourceSelfScore`/CSV "Tự đánh giá") — invariant khoảng giá trị là suy luận hợp lý, không phải rule được nguồn xác nhận trực tiếp |
| `VerifiedScore` | Nullable. **[SUY LUẬN]** tương tự `SelfScore`: `0 ≤ VerifiedScore ≤ Criteria.MaxScore` | Tương tự — chỉ là giá trị tĩnh seed từ CSV "Thẩm định" |
| `Status` | Nullable ở v1 (xem cảnh báo mục 5) — nếu có giá trị, phải thuộc 1 trong 4 giá trị: `"Chưa thực hiện"`, `"Đang thực hiện"`, `"Cần bổ sung minh chứng"`, `"Hoàn thành"` | CSV cột "Trạng thái" — nhưng xem mục 5, `dashboard.html` **không có UI** cho field này |
| `OwnerId` | Nullable, FK → `AppUser.Id` nếu có | CSV cột "Phụ trách", luôn rỗng trong mẫu |
| `Deadline` | Nullable date | CSV cột "Hạn xử lý", luôn rỗng trong mẫu |
| `Note` | Nullable text. **[SUY LUẬN]** đề xuất maxlength 2000 ký tự làm safeguard backend | JS `.noteInput` không có thuộc tính `maxlength` trong HTML — không có giới hạn nguồn xác nhận, đề xuất trên chỉ để tránh input vô hạn |

### 2.5 `CriteriaEvidence`

| Field | Rule | Nguồn |
| --- | --- | --- |
| `Content` | Required, non-empty text | CSV — mỗi dòng bắt đầu `"*"` đều có nội dung |
| `OrderIndex` | >= 0, duy trì đúng thứ tự dòng gốc trong CSV | — |

## 3. Công thức tính "Chênh lệch" / delta tuần-qua-tuần

Có **2 công thức khác nhau, KHÔNG nhầm lẫn** — cả hai đều KHÔNG lưu cột
riêng, tính khi cần:

### 3.1 "Chênh lệch" (điểm số, trong CÙNG 1 kỳ)

```
Chênh lệch = CriteriaAssessment.SelfScore - CriteriaAssessment.VerifiedScore
```

Nguồn: cột "Chênh lệch" trong CSV — đã verify khớp công thức trên toàn bộ
62 dòng mẫu (vd mã `1.1`: `7.04 - 10 = -2.96`; mã `6.6`: `10 - 5.43 = 4.57`).
Đây là so sánh 2 loại điểm trong **cùng một bản ghi**, không liên quan gì
tới việc so sánh giữa các kỳ.

### 3.2 "Tăng/giảm" (delta tiến độ, SO VỚI KỲ TRƯỚC — theo từng chỉ tiêu)

Nguồn: `dashboard.html`, các hàm `previousWeek()`, `valueOf()`,
`prevValue()`, `deltaOf()`:

```js
function previousWeek(date){
 const arr = sortedHistory().filter(x=>x.date<date);
 return arr.length ? arr[arr.length-1] : null;   // kỳ gần nhất có PeriodDate < kỳ hiện tại
}
function valueOf(id){ return Number(draft.values[id] ?? 0) }        // ProgressPercent kỳ hiện tại
function prevValue(id){
 const p = previousWeek(draft.date);
 return p ? Number(p.values[id] ?? 0) : null;                       // ProgressPercent kỳ liền trước, hoặc null nếu chưa có kỳ nào trước đó
}
function deltaOf(id){
 const p = prevValue(id);
 return p===null ? null : valueOf(id)-p;
}
```

**[ĐÃ ĐỔI NGUỒN, 2026-08-12]** Diễn dịch sang backend (không còn
`AssessmentPeriod`/`PeriodId`): với 1 `CriteriaId` cố định, "kỳ đang xem"
(kỳ-tuần/kỳ-tháng W) và "kỳ liền trước" đều là kết quả group-by
`CriteriaAssessment.CreatedAt` (xem `doc/ERD/ERD.md` mục "Kỳ (tuần/tháng/
năm)") — `ProgressPercent` dùng cho mỗi kỳ lấy theo rule **as-of/carry-
forward đề xuất mặc định** (record `CreatedAt` mới nhất tính đến hết kỳ đó,
**CHƯA được người dùng xác nhận trực tiếp**, xem ERD mục "Câu hỏi còn mở"
**[MỚI #2]**). `Delta = ProgressPercent(kỳ hiện tại) - ProgressPercent(kỳ
liền trước)`. Nếu không có kỳ nào trước đó (kỳ đầu tiên của toàn hệ thống,
hoặc kỳ đầu tiên mà chỉ tiêu này xuất hiện) → `Delta = null`, hiển thị
`"—"`.

**So sánh bằng epsilon**, không so `===` tuyệt đối trên số thực — dùng
ngưỡng `0.001` giống JS ở mọi nơi so sánh (`stats()`, `renderTable()` filter
`changeFilter`):
- `v > pv + 0.001` → tăng ("up")
- `v < pv - 0.001` → giảm ("down")
- ngược lại (và `pv !== null`) → không tăng ("flat")

### 3.3 Tiến độ chung của 1 kỳ (KPI "Tiến độ chung tuần này") — [CÔNG THỨC VIẾT LẠI, 2026-08-12 vòng 2]

Nguồn tham khảo — JS `weightedProgress(values)` gốc:

```
TiếnĐộChung(kỳ) = Σ(MaxScore(x) × ProgressPercent(x, kỳ) / 100)  /  Σ(MaxScore(x))     — với x chạy qua TOÀN BỘ Criteria (kể cả chỉ tiêu chưa có ProgressPercent trong kỳ đó, coi như 0%)
```

**✅ Đã chốt (2026-08-12 vòng 2) — KHÔNG carry-forward, LOẠI TRỪ khỏi mẫu
số (thay thế hoàn toàn công thức trên):**

```
TiếnĐộChung(kỳ W) = Σ(MaxScore(x) × ProgressPercent(x, W) / 100)  /  Σ(MaxScore(x))
                     — với x CHỈ chạy qua tập con Criteria CÓ record
                       CriteriaAssessment.CreatedAt rơi vào đúng kỳ W
                       (đã loại "active" theo mục 1.3 rule #3 của
                       spec/danh-muc-dti/business-rules.md như cũ)
```

Khác biệt cốt lõi so với công thức cũ: chỉ tiêu **không có** record trong
kỳ W **bị loại khỏi CẢ tử số lẫn mẫu số** — không còn coi là `ProgressPercent
= 0` rồi vẫn cộng `MaxScore` vào mẫu số như hành vi gốc. Hệ quả: 1 kỳ có ít
chỉ tiêu được cập nhật (thực tế sẽ rất phổ biến trong model mới, vì không
còn "Tạo kỳ mới" copy toàn bộ 62 chỉ tiêu mỗi kỳ) vẫn cho ra `TiếnĐộChung`
phản ánh đúng **chất lượng** dữ liệu đã có, không bị kéo tụt giả tạo bởi số
lượng chỉ tiêu chưa động tới. Nếu **không có bất kỳ `Criteria` nào** có
record trong kỳ W → `TiếnĐộChung(W) = "chưa có dữ liệu"` ("—"), không phải
0%.

Delta của tiến độ chung so kỳ trước = `TiếnĐộChung(kỳ hiện tại) -
TiếnĐộChung(kỳ liền trước)`, cùng logic tìm "kỳ liền trước" ở mục 3.2 —
nếu 1 trong 2 kỳ là "—" (chưa có dữ liệu) thì `Delta = null`.

> **⚠️ Vẫn còn mở (không đổi bởi quyết định carry-forward)**: câu hỏi về
> mẫu số `Σ MaxScore` nên dùng danh mục `Criteria` **hiện tại** hay
> **snapshot tại từng kỳ lịch sử** khi `Criteria` có thể bị thêm/xoá qua
> CRUD (xem `spec/danh-muc-dti/business-rules.md` mục 5 câu hỏi #1) — đây
> là câu hỏi **khác**, độc lập với việc loại trừ chỉ tiêu thiếu dữ liệu
> trong kỳ (mục này). Cả 2 rule cộng dồn: trước tiên lọc theo "Criteria nào
> được tính cho kỳ này" (câu hỏi #1, chưa chốt — tạm dùng toàn bộ danh mục
> hiện tại), sau đó trong tập đó mới tiếp tục loại trừ chỉ tiêu không có
> record trong đúng kỳ W (mục này, đã chốt).

### 3.4 Tiến độ theo nhóm

Nguồn: JS `renderGroups()` — công thức giống mục 3.3 (đã viết lại) nhưng
mẫu số/tử số chỉ tính trên các `Criteria` thuộc `GroupId` đó **và** có
record trong kỳ đang xem, và **chỉ tính cho kỳ đang xem** (không có bản so
sánh delta theo nhóm trong `dashboard.html`).

### 3.5 Số liệu đếm (`kUp`/`kFlat`/`kDone`) — [CẬP NHẬT, 2026-08-12 vòng 2]

Nguồn tham khảo — JS `stats()` gốc: duyệt toàn bộ `Criteria`, với mỗi chỉ
tiêu, `done++` nếu `ProgressPercent(kỳ hiện tại) >= 99.999`; nếu
`prevValue !== null`: `up++`/`down++`/`flat++` theo ngưỡng epsilon ở mục
3.2.

**Diễn dịch mới (KHÔNG carry-forward)**: cả 4 số đếm (`kUp`/`kFlat`/`kDown`/
`kDone`) chỉ duyệt qua tập `Criteria` **có record trong đúng kỳ đang xem**
— chỉ tiêu không có record trong kỳ này **không được tính vào bất kỳ số đếm
nào** (kể cả `kDone`, khác hành vi gốc coi thiếu dữ liệu vẫn có thể được
đếm nếu giá trị cũ còn "dính" — nay không còn khái niệm giá trị "dính lại"
nữa). `up`/`down`/`flat` tiếp tục yêu cầu thêm điều kiện có kỳ liền trước
(`prevValue !== null`) như cũ.

## 4. Quy tắc "kỳ" — [VIẾT LẠI HOÀN TOÀN, 2026-08-12] không còn `AssessmentPeriod`, kỳ = ngày của `CreatedAt`

> Mục này trước đây mô tả rule cho bảng `AssessmentPeriod` (đã bỏ). Giữ lại
> các đoạn JS gốc **chỉ để đối chiếu lịch sử** ("nguồn tham khảo") — hành vi
> thật sự áp dụng nằm ở các đoạn "**Diễn dịch mới**" ngay sau mỗi đoạn tham
> khảo. Xem đầy đủ thiết kế thay thế ở `doc/ERD/ERD.md` mục "Kỳ (tuần/
> tháng/năm) — khái niệm ngầm định".

1. **~~Unique `PeriodDate`~~ → Unique `(CriteriaId, CAST(CreatedAt AS
   date))` trên `CriteriaAssessment`**: không tồn tại 2 record
   `CriteriaAssessment` của cùng 1 `CriteriaId` cùng ngày (`CreatedAt`) —
   khớp thiết kế mới `doc/ERD/PlatformManager.dbml`.

2. **Lưu lại cùng 1 ngày = UPDATE-tại-chỗ, không phải insert mới.** Nguồn
   tham khảo — JS `saveWeek()` gốc:
   ```js
   function saveWeek(){
    draft.date = weekDate.value || draft.date || today();
    const snapshot = JSON.parse(JSON.stringify(draft));
    const idx = historyData.findIndex(x=>x.date===snapshot.date);
    if(idx>=0) historyData[idx]=snapshot; else historyData.push(snapshot);
    ...
   }
   ```
   **Diễn dịch mới**: không còn 1 hành động "lưu cả kỳ cùng lúc" (không còn
   `saveWeek()`/nút "Lưu tuần này") — mỗi lần lưu chỉ tác động **1
   `CriteriaId`** (1 ô Tiến độ %/Ghi chú, hoặc 1 dòng Import). Với mỗi
   `CriteriaId`: nếu **đã có** record `CreatedAt` = hôm nay → **UPDATE**
   record đó (giữ nguyên `CreatedAt`, đổi `UpdatedAt` + field đang sửa);
   nếu **chưa có** → **copy-forward** baseline từ record `CreatedAt` gần
   nhất trước đó (toàn bộ 7 field nghiệp vụ), áp giá trị đang sửa lên trên,
   **INSERT** record mới với `CreatedAt = UpdatedAt = now`. Đây chính là cơ
   chế thay thế "Tạo kỳ mới từ kỳ gần nhất" — tự động, từng chỉ tiêu một,
   không cần hành động tường minh nào của người dùng. Không có khái niệm
   `409 Conflict "trùng ngày"` — luôn là upsert hợp lệ.

3. **"Không ghi đè dữ liệu tuần cũ"** (text cũ trên UI) vẫn đúng tinh thần:
   lưu 1 record ngày `A` cho 1 `CriteriaId` **không** ảnh hưởng tới các
   record ngày `B` khác (của bất kỳ `CriteriaId` nào) — mỗi record độc lập.
   Câu này **không** mâu thuẫn với rule #2 (update-tại-chỗ cùng ngày) — nó
   nói về việc các ngày *khác nhau* không bị đụng vào nhau.

4. **Không có ràng buộc "khoảng cách đúng 7 ngày" giữa 2 lần lưu liên
   tiếp** — càng đúng hơn trong model mới, vì mỗi lần lưu chỉ tạo/ghi đè
   đúng **1 ngày = hôm nay**, không còn khái niệm "chọn ngày kỳ" để mà ràng
   buộc khoảng cách. "Tuần"/"Tháng" chỉ còn là lớp group-by ở tầng đọc
   (Dashboard), không phải ràng buộc ở tầng ghi. **[SUY LUẬN — CHƯA CHỐT]**
   quy ước chia tuần cụ thể (ISO thứ Hai–Chủ Nhật hay khác) — xem
   `doc/ERD/ERD.md` mục "Câu hỏi còn mở" **[MỚI #1]**.

5. **~~Khởi tạo kỳ mới kế thừa dữ liệu kỳ trước~~ → Copy-forward tự động,
   từng chỉ tiêu, ngay trong rule #2** (không còn là "tiện ích UI" tách
   rời — nay là **rule bắt buộc ở tầng backend**, xảy ra ngầm mỗi khi có
   record mới cho 1 ngày mới). Nguồn tham khảo — JS `newFromLatest()`/
   `loadDraftForDate()` gốc copy `values`/`notes` từ kỳ gần nhất, hoặc seed
   `DTI_ITEMS[].initialProgress` nếu chưa từng có kỳ nào — **diễn dịch mới**:
   đúng tinh thần này, nhưng áp dụng **per-`CriteriaId`** thay vì cho cả
   danh mục cùng lúc, và copy-forward **cả 7 field** (không chỉ `values`/
   `notes`) vì Import cũng ghi qua cùng cơ chế này.

## 5. Quy tắc chuyển `Status` — CẢNH BÁO: không có bằng chứng ràng buộc trong nguồn

**Không tìm thấy bất kỳ ràng buộc chuyển trạng thái nào** (state machine,
"không cho lùi trạng thái", v.v.) trong `dashboard.html` — lý do: JS
**hoàn toàn không thao tác trên field `Status`**. Cụ thể, sau khi rà soát
lại toàn bộ `<script>` theo yêu cầu phối hợp với `frontend-expert`:

- `dashboard.html` **không có control nhập liệu nào** (không `<select>`,
  không nút chuyển trạng thái) cho một giá trị "Trạng thái" thủ công trong
  bảng 62 chỉ tiêu.
- Cột "Trạng thái" hiển thị trên bảng là **badge tính toán runtime**
  (`statusFor(v,d)`, 3 giá trị: Hoàn thành/Không tăng/Đang thực hiện — suy
  ra thuần từ `ProgressPercent` + delta, xem `doc/ERD/ERD.md` mục 4), hoàn
  toàn tách biệt khỏi field `Status` lưu DB theo CSV (4 giá trị).
- `Status` (4 giá trị, theo CSV) hiện chỉ có nguồn gốc từ **import CSV ban
  đầu** — không có bằng chứng nào cho thấy nó được cập nhật qua chính màn
  hình dashboard tuần này.

**Kết luận — không bịa transition rule.** Vì vậy:
- Business rule duy nhất áp được ở v1: `Status` (nếu có giá trị) phải thuộc
  tập 4 giá trị hợp lệ (validation domain, mục 2.4) — **không** có rule nào
  về thứ tự chuyển đổi hợp lệ giữa các giá trị (vd không có bằng chứng cấm
  chuyển từ "Hoàn thành" ngược về "Đang thực hiện").

**✅ QUYẾT ĐỊNH NGƯỜI DÙNG (đã chốt)**: `Status` đến từ một **quy trình thẩm
định riêng** (màn hình/luồng khác, chưa có prototype) — **không** phải màn
hình dashboard tuần này. Dashboard tuần **chỉ đọc** (nếu cần hiển thị) hoặc
**hoàn toàn không đụng tới** `Status`, khớp chính xác 100% với việc
`dashboard.html` không có control nhập nào cho field này — **đây không phải
thiếu sót cần bổ sung**, mà là đúng phạm vi thiết kế. Hệ quả cho Phase 3
(Figma): **không cần thêm control `Status` mới** vào slice dashboard tuần
đang thiết kế. Cùng quyết định áp dụng cho `SelfScore`/`VerifiedScore` —
**tĩnh, chỉ đến từ quy trình thẩm định riêng**, không sửa qua dashboard tuần
(khớp đúng hiện trạng `dashboard.html`, không cần thêm UI).

## 6. Permission dự kiến — PLACEHOLDER, chưa đủ thông tin để chốt

`dashboard.html` là ứng dụng client-side thuần dùng `localStorage`, **không
có** bất kỳ cơ chế đăng nhập/phân quyền nào — mọi người xem trang đều thấy
và sửa được toàn bộ dữ liệu. Vì vậy **không có bằng chứng nguồn** để suy ra
role/permission thật. Auth đã chốt dùng ASP.NET Core Identity (xem
`doc/ERD/ERD.md`, `src/BE/.claude/rules/api-controller.md` § Auth/
Permission), nhưng **role cụ thể trong Identity (tên Role, danh sách
permission theo Role) chưa được người dùng chốt** — không tự bịa.

Đặt câu hỏi cụ thể (xem mục 6 "Câu hỏi còn mở") thay vì tự chọn phương án.
Ghi tạm 1 khung khả dĩ **chỉ để tham khảo, KHÔNG PHẢI quyết định**:

| Hành động | Ai được làm? (CHƯA CHỐT) |
| --- | --- |
| Xem dashboard, KPI, lịch sử | Mọi user đã đăng nhập? |
| Sửa `ProgressPercent`/`Note`/`Evidence` của 1 `CriteriaAssessment` | Chủ sở hữu (`OwnerId` = user hiện tại)? Hay bất kỳ ai có role "nhập liệu"? |
| Sửa `Status`, `SelfScore`, `VerifiedScore`, gán `OwnerId` | Role "thẩm định"/"quản trị"? |
| Lưu `CriteriaAssessment` (sửa ô/Import — tự tạo record ngày mới nếu cần, đã bỏ khái niệm "Lưu tuần này" tường minh, xem mục 4) | Role nào? Có giới hạn chỉ được lưu chỉ tiêu do mình phụ trách không? |
| Sao lưu/khôi phục (export/import JSON — tính năng cũ của `dashboard.html`, chưa rõ có giữ ở bản backend hay không) | — |

## 7. Điểm cần `frontend-expert` biết (đã gửi trực tiếp)

- Đã xác nhận qua tin nhắn với `frontend-expert`: badge (done/working/
  stalled) và `Status` (DB) là 2 khái niệm khác nhau — cả 2 bên đã note rõ
  trong tài liệu của mình để tránh nhầm lẫn khi implement UI.
- `dashboard.html` không có UI nhập `Status` thủ công — ảnh hưởng tới
  phạm vi UI-spec của slice đầu tiên (xem mục 8 trong `ui-spec.md` mà
  `frontend-expert` đã ghi câu hỏi tương ứng).

## 8. Câu hỏi còn mở — cần người dùng quyết định, KHÔNG tự chốt

### Đã chốt (người dùng quyết định)

1. ✅ **`Status`** — đến từ quy trình thẩm định riêng (ngoài phạm vi dashboard
   tuần này). Dashboard tuần không có UI cho field này — đúng thiết kế, xem
   mục 5.
2. ✅ **`SelfScore`/`VerifiedScore`** — tĩnh, chỉ từ quy trình thẩm định
   riêng, không sửa qua dashboard tuần. Khớp đúng hiện trạng
   `dashboard.html` (không có input cho 2 field này) — không cần thêm UI.
3. ✅ **Sao lưu/Khôi phục (export/import)** — **giữ lại** ở phiên bản backend
   thật, nhưng đổi ý nghĩa: không còn là "backup kỹ thuật toàn bộ
   localStorage" (vì DB đã bền vững), mà là tính năng **export/import dữ
   liệu thật** qua API (vd export báo cáo kỳ hiện tại/lịch sử ra file, import
   dữ liệu từ file vào DB có kiểm soát — khác semantics "ghi đè toàn bộ, không
   validate sâu" của bản prototype, xem `ui-spec.md` mục 3.8). Chi tiết định
   dạng file/API cho export-import thật là quyết định riêng khi implement
   service layer, chưa cần chốt ở bước Figma.

### Còn mở (chưa chặn Phase 3 — Figma vẫn tái tạo đúng UI hiện có của
`dashboard.html`, các câu này ảnh hưởng backend/tương lai, không ảnh hưởng
hình dạng màn hình dashboard tuần đang thiết kế)

4. **✅ Đã chốt (2026-08-12 vòng 2)**: ~~Ràng buộc lịch tuần chặt cho
   `AssessmentPeriod`?~~ — bảng đó đã bỏ. Quy ước chia tuần khi group-by
   `CreatedAt` = **tuần ISO (thứ Hai–Chủ Nhật)**, đúng đề xuất mặc định ban
   đầu, nay đã được người dùng xác nhận trực tiếp.
5. **Permission chi tiết** — xem khung ở mục 6, chưa có role nào được chốt.

### ✅ Đã chốt (2026-08-12, vòng 2) — câu hỏi phát sinh từ việc bỏ hẳn `AssessmentPeriod`

> Không còn là câu hỏi mở. Chi tiết đầy đủ: `doc/ERD/ERD.md` mục "Câu hỏi
> còn mở" phần "Đã chốt (2026-08-12, vòng 2)". Tóm tắt phần ảnh hưởng trực
> tiếp tới Dashboard:

6. **Carry-forward: KHÔNG** — **ngược lại** đề xuất mặc định trước đó (từng
   đề xuất CÓ). KPI "Tiến độ chung tuần này" (mục 3.3, đã viết lại hoàn
   toàn) nay loại trừ chỉ tiêu không có dữ liệu trong kỳ đang xem khỏi cả
   tử số lẫn mẫu số, thay vì carry-forward hay coi = 0%.
7. **Định nghĩa "Tất cả" = toàn bộ dữ liệu trong 1 NĂM được chọn** — khác cả
   2 đề xuất cũ (không phải "trạng thái hiện hành" như Danh mục DTI đề
   xuất, không phải "toàn bộ lịch sử vô hạn" như Dashboard tự đề xuất).
   Dashboard cần bộ lọc **Năm** làm phạm vi nền tảng (mặc định năm hiện
   tại); "Tất cả" = chưa lọc thêm theo tuần/tháng trong năm đó. KPI/biểu
   đồ/bảng ở trạng thái "Tất cả" dùng công thức trung bình cộng mở rộng
   theo năm — xem `spec/danh-muc-dti/business-rules.md` mục 3 (đã viết lại
   phần "theo năm").

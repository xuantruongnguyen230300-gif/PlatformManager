---
kind: luat
scope: du-an
verified: 2026-09-06
---

# API Contract — Danh mục DTI (`modules/danh-muc-dti`)

> ## 📐 ĐÍCH ĐẾN — CHƯA THI CÔNG
>
> **Không có dòng code nào của tính năng này tồn tại hôm nay (đối chiếu lại 2026-09-08).**
> Module BE `DtiWeekly` và màn FE `danh-muc-dti` đều gỡ 2026-08-29; thư mục
> `src/BE/Modules/` — nơi rác build của nó còn sót lại — **đã xoá 2026-09-08**
> ([`../kien-truc-core-module.md`](../kien-truc-core-module.md), §bảng "có thật hôm nay").
> Solution BE hiện chỉ có `Core.*` + host + `Tests/` — đếm bằng
> `grep -c '<Project Path=' src/BE/PlatformManager.slnx`.
>
> Card này là **hợp đồng phải hiện thực**, không phải mô tả thứ đang chạy. Mọi câu ở dạng
> "trả về X" đọc là "phải trả về X".
>
> **Đích đến kiến trúc: `PlatformManager.Business.*`** (Domain/Application/Persistence/
> Infrastructure/Api) — **KHÔNG** dựng lại `Modules.DtiWeekly.*`. Ranh giới, thứ tự phụ
> thuộc và bảng *"có thật hôm nay → sẽ thành"*: [`../kien-truc-core-module.md`](../kien-truc-core-module.md).
>
> **Luật nghiệp vụ** (mô hình dữ liệu, công thức, quy tắc kỳ, quy tắc import) **không nằm
> ở đây** — file chủ là `spec/danh-muc-dti/business-rules.md`. Card này chỉ giữ *hình dạng
> đường dây*: route, tham số, shape, mã lỗi.

**Ngày viết lại: 2026-09-05.** Bản trước là 8 card `DRAFT` chưa từng được chốt, viết theo
mô hình nghiệp vụ cũ (thực thể `CriteriaEvidence` nhiều dòng, bộ trạng thái hệ thống tự
tính, import CSV-only). Bản này bám 16 quyết định chốt với người dùng ngày 2026-09-05, cộng
hai vòng sửa cùng ngày.

**Địa chỉ màn hình: `/danh-muc/dti`** — chốt Q33 (2026-09-05), khớp dấu vết trước khi màn bị
gỡ và khớp kiểu hai cấp của `/quan-tri/nguoi-dung`. Card này ghi địa chỉ vì Dashboard trỏ
sang đây bằng một nút (Q32, [`dashboard.md`](dashboard.md) §0); layout và điều hướng thì
thuộc `spec/danh-muc-dti/ui-spec.md`.

---

## 0. CASING — CHỐT, không còn để ngỏ

Bản trước ghi *"camelCase xuyên suốt, CHƯA XÁC NHẬN với backend-expert"* và trỏ sang một
cảnh báo ở `doc/contracts/meta-menu.md` **nay không còn tồn tại**. Hậu quả đã trả giá
thật: BE gửi `criteriaCode`/`criteriaName` trong khi FE đọc `code`/`name`, không ai đối
chiếu vì card không bao giờ rời trạng thái `DRAFT`.

**Chốt 2026-09-05 theo source đang chạy: camelCase xuyên suốt** (envelope + mọi DTO
payload), **khoá của `fields`/`fieldErrors` giữ PascalCase**, `code` ra dây là **tên
member enum**, thành công luôn **HTTP 200**, grid trả `PagedList<T>` bốn trường
`items`/`totalCount`/`page`/`pageSize`.

> **Ngoại lệ ĐÃ ĐĂNG KÝ, thêm 2026-09-06:** payload của DM-2 là một **superset cộng thêm** của
> `PagedList<T>` — bốn trường phân trang **giữ nguyên tên, nguyên vị trí, nguyên tầng**, cộng
> đúng **một** trường `canWrite` ở cùng cấp. Mapper lưới dùng chung đọc bốn khoá kia vẫn chạy
> nguyên vẹn; chỉ mapper của DTI đọc khoá thứ năm. Lý do buộc phải có: xem DM-2 mục 3. Đây
> **không** phải giấy phép lồng `PagedList` vào trong một object khác — lồng là phá mapper
> dùng chung, cộng thêm thì không.

📖 Luật đầy đủ + lý do + bằng chứng là chủ đề của file khác, **không chép lại ở đây**:
[`../huong_dan/quy-uoc/be-api-controller.md`](../huong_dan/quy-uoc/be-api-controller.md)
§Envelope response.

### Hệ quả riêng của DTI mà mapper phải xử lý — giữ ở đây vì file chủ không nói

⚠️ **Trường `null` KHÔNG ra dây; nó VẮNG MẶT khỏi JSON.**
`src/BE/PlatformManager.Api/Program.cs:97` và `:114` —
`DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull`, đặt cho **cả** đường MVC
lẫn `Http.Json` (đối chiếu source 2026-09-05).

Màn Danh mục DTI là chỗ luật đó cắn mạnh nhất, vì **đa số trường của một dòng lưới là
nullable**: một chỉ tiêu chưa có đánh giá trong kỳ không trả `"selfScore": null` — nó
**không có khoá `selfScore`** nào cả. Mapper phải đọc "vắng mặt" và "null" như nhau.

Bản trước của card viết `selfScore: number|null` mà không nói điều đó. Đây là loại lệch
không lộ ra cho tới khi có dữ liệu thật, và khi lộ thì trông như lỗi tính toán chứ không
như lỗi mapper.

*Ghi chú cho người bảo trì `be-api-controller.md`: §Envelope response hiện mô tả
`PropertyNamingPolicy` và `DictionaryKeyPolicy` nhưng **không** nêu
`DefaultIgnoreCondition` như một luật chung. Đoạn trên là hệ quả cho DTI, không phải bản
sao của một luật đã có ở đó.*

---

## 1. Quy ước dùng chung cho mọi card dưới đây

- **Kiểu ngày**: `date` = chuỗi `YYYY-MM-DD` (ngày thuần, không giờ, không offset).
  `dateTime` = ISO-8601 có offset.
- **`period`**: `"all"` | `"YYYY-Www"` (tuần ISO-8601, thứ Hai → Chủ nhật) |
  `"YYYY-MM"` (tháng dương lịch, ngày 1 → ngày cuối). Ý nghĩa từng giá trị và cách quy
  một `period` về khoảng ngày: `spec/danh-muc-dti/business-rules.md` §Quy tắc kỳ.
- **`status`**: đúng 4 giá trị, chuỗi nguyên văn tiếng Việt —
  `"Chưa thực hiện"` · `"Đang thực hiện"` · `"Cần bổ sung minh chứng"` · `"Hoàn thành"`.
  Người dùng **chọn tay**; hệ thống không tự tính, không tự đổi (Q4).
- **`diff` (Chênh lệch) là trường TÍNH, không lưu, không nhận từ client** —
  `diff = verifiedScore − selfScore` (Q2 + **Q25**). Gửi lên thì bị bỏ qua, không báo lỗi.

  > ⚠️ **Đảo chiều 2026-09-05.** Bản trước là `selfScore − verifiedScore`. Số ra dây sẽ
  > **ngược dấu với cột `Chênh lệch` trong file gốc của BA** — không ảnh hưởng import (cột đó
  > bị bỏ qua, xem DM-7), nhưng ảnh hưởng mọi test, mọi ảnh chụp màn hình và tiêu đề cột của
  > file xuất. Lý do + hai ca kiểm: `spec/danh-muc-dti/business-rules.md` §3.1.
- **Gate**: mọi controller kế thừa `ApiControllerBase` mang `[Authorize]` fail-closed sẵn.
  **Quyền GHI đã CHỐT (Q27, 2026-09-05): đúng một permission-key cho toàn bộ đường ghi DTI**
  — tạo/sửa/xoá chỉ tiêu, lưu đánh giá, sửa inline **và import** — áp cho **mọi kỳ**, kể cả
  kỳ đã qua. Tên key, quy ước đặt tên và ba bước khai: `spec/danh-muc-dti/business-rules.md`
  §6.5. Endpoint **đọc** (DM-1, DM-2, DM-8) giữ `[Authorize]` trần.
  `403` do filter phát sinh, **không** phải một `ErrorDescriptor` của catalog DTI.

  > **Q39 (2026-09-06) — thiếu quyền ghi thì vào được màn, nhưng CHỈ ĐỌC.** Không chặn ở
  > route, không ẩn mục menu. Người thiếu quyền vẫn gọi được DM-1/DM-2/DM-8 và vẫn xuất báo
  > cáo; họ chỉ nhận `403` nếu cố gọi một endpoint ghi. Để FE biết mà ẩn nút **trước** khi
  > người dùng bấm, DM-2 trả cờ `canWrite` (xem DM-2 mục 3).
- **Chỉ nhập theo TUẦN (Q37, 2026-09-06).** `period` **đích của một lời ghi** chỉ nhận
  `"YYYY-Www"` hoặc `"all"`. Một kỳ **tháng** là tổng hợp tự tính ⇒ chỉ đọc; gửi nó lên đường
  ghi nhận `400 …PERIOD_NOT_WEEKLY`. Đường **đọc** không đổi: `period = "YYYY-MM"` vẫn lọc
  lưới bình thường, và export `mode=month` vẫn chạy. Lý do (tuần ISO không nằm gọn trong
  tháng, neo `Tháng 8` ⇒ tuần 36): `spec/danh-muc-dti/business-rules.md` §5.1.
- **`"all"` không được nhảy năm (T15, 2026-09-06).** Mọi lời ghi mang `period = "all"` phải
  gửi kèm **`year`** — năm người dùng đang xem. `year` ≠ năm hiện tại ⇒
  `400 …PERIOD_OUT_OF_YEAR`, vì `"all"` giải nghĩa thành *tuần hiện tại*, tức một kỳ **không
  nằm trong năm đang hiển thị**. `year` **không** cần khi `period` là một tuần cụ thể — năm đã
  nằm sẵn trong chuỗi tuần, và ca đó vẫn ghi được bình thường (Q20 không bị lật).

  > **Nguyên tắc chung đằng sau Q26 + Q37 + T15, ghi ở đây vì nó áp cho cả ba card ghi:**
  > *lời ghi không bao giờ được rơi vào một kỳ mà người dùng không nhìn thấy trên màn hình.*
  > Bảng ba chiều lỗ hổng: `spec/danh-muc-dti/business-rules.md` §5.3.
- **Mã lỗi**: mọi mã dưới đây phải khai thành `ErrorDescriptor` trong catalog — xem §2.

---

## CONTRACT DM-1 — Danh sách nhóm chỉ tiêu

- **Status: AGREED** (2026-09-05)
- Route: `GET /api/criteria-groups`
- Query params: không có
- Response: `IApiResult<CriteriaGroupDto[]>`

```
data: [ { id: guid, code: string, name: string, displayOrder: int } ]
```

- Sắp xếp theo `displayOrder` tăng dần — BE sắp, FE không sắp lại.
- Chỉ trả nhóm chưa xoá mềm.
- Dữ liệu gốc: 6 nhóm của `spec/DTI_CanGiuoc_2026-08-11.csv`, seed sẵn. Danh sách 6 nhóm
  và số chỉ tiêu/điểm từng nhóm: `spec/danh-muc-dti/business-rules.md` §Danh mục nhóm.
- FE tải một lần lúc khởi tạo màn, dùng cho cả dropdown lọc lẫn dropdown trong dialog.

---

## CONTRACT DM-2 — Lưới Danh mục DTI (đọc, phân trang server-side)

- **Status: AGREED** (2026-09-05)
- Route: `GET /api/criteria`
- Query params — **đây là "object bộ lọc" dùng chung**, mọi endpoint đọc theo danh sách
  phải bind đúng bộ này (luật §4 của
  [`../huong_dan/wiki-core/be/15-import-export.md`](../huong_dan/wiki-core/be/15-import-export.md)):

```
search:   string?   // khớp Code HOẶC Name, không phân biệt hoa/thường + không phân biệt dấu
groupId:  guid?     // lọc theo nhóm
status:   string?   // 1 trong 4 giá trị Trạng thái; giá trị lạ -> 400, KHÔNG âm thầm bỏ lọc
year:     int?      // mặc định = năm hiện tại
period:   string?   // "all" (mặc định) | "YYYY-Www" | "YYYY-MM"
page:     int = 1
pageSize: int = 10  // trần 200
```

> **`pageSize` mặc định = 10** (chốt người dùng 2026-09-05, Q19). Bản trước ghi 20.
> Bằng đúng con số lưới `Quản trị người dùng` của Core đang dùng —
> `src/FE/src/app/platform/quan-tri-nguoi-dung/pages/quan-tri-nguoi-dung/quan-tri-nguoi-dung.page.ts:29`
> (`DEFAULT_PAGE_SIZE = 10`, đối chiếu lại 2026-09-06 — bản trước ghi `:27`, lệch 2 dòng).
> `rowsPerPageOptions` giữ `[10, 20, 50]`, tức hợp đồng `DataTable` **không** phải mở rộng.
>
> ⚠️ Lưu ý cho người thi công: mặc định của **query BE** ở Core lại là 20
> (`src/BE/Core/PlatformManager.Core.Application/Users/GetUsersListQuery.cs:19`, sửa số dòng
> 2026-09-06) — lưới
> người dùng luôn gửi `pageSize` tường minh nên chênh lệch đó chưa bao giờ lộ. Ở đây chốt
> **10 ở cả hai phía** để không dựng lại đúng cái bẫy đó cho màn thứ hai.

- Response: `IApiResult<CriteriaGridDto>`

```
CriteriaGridDto:                     // = PagedList<CriteriaRowDto> + khối quyền ghi CẤP MÀN
  items:      CriteriaRowDto[]
  page:       int
  pageSize:   int
  totalCount: int

  // ── Khối quyền ghi — CẤP MÀN, không lặp ở dòng nào. Xem mục 3 ──
  canWrite:      bool                // có quyền GHI dữ liệu DTI (Q39)
  isEditable:    bool                // = canWrite VÀ editBlockedBy rỗng
  editBlockedBy: string[]            // LUÔN có mặt; [] khi isEditable = true
                                     // "NO_WRITE_PERMISSION" | "PERIOD_NOT_WEEKLY" | "PERIOD_OUT_OF_YEAR"

CriteriaRowDto:
  criteriaId:      guid
  code:            string
  name:            string
  groupId:         guid
  groupCode:       string
  groupName:       string
  maxScore:        number
  assessmentId:    guid?     // vắng mặt nếu kỳ đang xem chưa có bản ghi đánh giá
  assessmentDate:  date?
  progressPercent: number?   // 0..100
  selfScore:       number?
  verifiedScore:   number?
  diff:            number?   // TÍNH = verifiedScore − selfScore (Q25 — đảo chiều 2026-09-05)
  status:          string?   // 1 trong 4 giá trị
  ownerId:         guid?     // FK sang core.AspNetUsers
  ownerName:       string?
  deadline:        date?
  note:            string?   // Minh chứng/Ghi chú — MỘT ô text, không phải danh sách (Q5)
  version:         string?   // token optimistic concurrency của bản ghi đánh giá

  // ── Q31, thêm 2026-09-05 — KỲ CỦA CHÍNH DÒNG NÀY ──
  assessmentPeriod:      string?   // "YYYY-Www" — tuần ISO chứa assessmentDate
  assessmentPeriodLabel: string?   // "Tuần 33/2026 (10/08 – 16/08/2026)" — BE dựng sẵn
```

**Năm điều bắt buộc, đừng suy diễn lại:**

1. **`evidences` KHÔNG còn.** Q5 chốt `Minh chứng/Ghi chú` là **một ô text** (`note`).
   Thực thể `CriteriaEvidence` của thiết kế cũ bị bỏ hẳn — mapper FE không được giữ
   nhánh đọc mảng.
2. **`diff` do BE tính, FE chỉ hiển thị.** Epsilon so sánh và quy tắc làm tròn ở
   `spec/danh-muc-dti/business-rules.md` §Công thức. FE tính lại là tạo nguồn sự thật thứ 2.
3. **Khối quyền ghi — BA trường, tất cả ở CẤP MÀN, một nguồn sự thật** (Q37 + Q39 + T15,
   2026-09-06).

   ```
   canWrite   = người gọi được phép GHI dữ liệu DTI          ← nguồn DUY NHẤT về quyền
   isEditable = canWrite
                VÀ  đơn vị kỳ đang chọn là TUẦN (hoặc "all")     ← Q37
                VÀ  năm đang lọc là năm hiện tại KHI kỳ = "all"  ← T15
   ```

   **Ba điều kiện**, mỗi cái hỏng một kiểu và mỗi cái có một lối ra khác nhau:

   | # | Điều kiện | Hỏng thì BE trả | Người dùng phải làm gì |
   | ---: | --- | --- | --- |
   | 1 | có quyền ghi | `403` (filter) | xin cấp quyền |
   | 2 | đơn vị kỳ là tuần hoặc `"all"` | `400 CRITERIA.ASSESSMENT_PERIOD_NOT_WEEKLY` | chọn một tuần |
   | 3 | `"all"` chỉ dùng khi đang xem năm hiện tại | `400 CRITERIA.ASSESSMENT_PERIOD_OUT_OF_YEAR` | chọn một tuần cụ thể của năm đó, hoặc về năm hiện tại |

   > **T15 KHÔNG lật Q20.** Chọn `Năm = 2025` rồi chọn **`Tuần 33/2025`** vẫn ghi được — kỳ
   > đích đúng bằng thứ người dùng đang nhìn. Điều T15 chặn là **lối tắt `"all"`**, thứ giải
   > nghĩa thành *tuần hiện tại của năm hiện tại* bất kể ô `Năm` đang chỉ vào đâu.

   | Trường | FE dùng để |
   | --- | --- |
   | `canWrite` | hiện/**ẩn** `+ Thêm chỉ tiêu`, `Import CSV/Excel`, `Sửa`, `Xoá` |
   | `isEditable` | bật/**tắt** sửa inline và nút Lưu của dialog |
   | `editBlockedBy` | chọn **lời nhắc** nào hiện trên dải băng, và chỉ đúng ô lọc đang chặn |

   > ### ⚠️ `isEditable` ĐÃ RỜI khỏi `CriteriaRowDto` (2026-09-06) — đừng đọc nó ở cấp dòng
   >
   > Bản trước đặt nó trong từng dòng. Cả **ba** điều kiện của nó đều ở **cấp request** (quyền
   > của người gọi · đơn vị kỳ đang chọn · năm đang lọc) nên giá trị giống hệt nhau ở cả 62
   > dòng — giữ một bản sao mỗi dòng chỉ tạo chỗ cho chúng lệch nhau, và **không dùng được ở
   > ca lưới rỗng** (T9), đúng lúc dải băng cần nói lý do nhất.
   >
   > Response của DM-4 và DM-6 vì thế **không** mang `isEditable` nữa (chúng trả một
   > `CriteriaRowDto`). Không mất mát: lời ghi vừa thành công nghĩa là quyền và bộ lọc không
   > đổi, FE giữ nguyên khối quyền cấp màn của lần tải lưới gần nhất.

   > ### ⚠️ Vì sao `canWrite` PHẢI ở cấp màn — lỗ hổng đo được ở giao của Q39 và T9
   >
   > Ca T9: người dùng vào màn khi **chưa import lần nào**, lưới có **0 dòng**. Nếu quyền chỉ
   > sống trong `CriteriaRowDto` thì lúc đó **không có dòng nào để đọc** — mà đúng hai nút
   > `+ Thêm chỉ tiêu` và `Import CSV/Excel` vẫn phải quyết ẩn hay hiện. Trớ trêu ở chỗ đó
   > lại là lúc nút Import **quan trọng nhất**: nó là việc duy nhất làm được trên màn hình
   > rỗng. Một cờ chỉ tồn tại khi đã có dữ liệu thì vắng mặt đúng lúc cần nhất.
   >
   > FE **không** được suy `canWrite` từ `items.length === 0` (lưới rỗng không nói gì về
   > quyền), cũng **không** được đoán từ role trong `GET /api/auth/me`: payload đó có `roles`
   > nhưng **không có permission-key** (`CurrentUserInfo` — `id`/`userName`/`email`/`fullName`/
   > `roles`/`mustChangePassword`, xem [`auth.md`](auth.md) §`GET /api/auth/me`), và ánh xạ
   > role → key là **dữ liệu chạy** sửa được ở màn Phân quyền. Suy từ role nghĩa là chép ma
   > trận phân quyền sang FE và để nó lệch ngay lần tick đầu tiên.

   > **Vì sao KHÔNG đưa permission-key vào `GET /api/auth/me` cho gọn:** `/me` được gọi **một
   > lần lúc khởi động** rồi giữ trong state. Quyền thì thu hồi được giữa phiên, và Core cố ý
   > **không cache** phép kiểm quyền để việc thu hồi có hiệu lực ngay
   > (`src/BE/Core/PlatformManager.Core.Application/Permissions/IPermissionChecker.cs:15`).
   > Nhét quyền vào một payload cached-at-bootstrap là dựng lại đúng cái cache mà Core đã cố ý
   > không làm — nút sửa vẫn hiện cho tới khi người dùng F5. `canWrite` tính **mỗi request**
   > nên không có cửa sổ lệch đó. (Đây cũng là lý do nó **không** phải một thay đổi Core: mở
   > rộng `CurrentUserInfo` là sửa hợp đồng `auth.md` — thứ mọi màn đều dùng — cho một
   > affordance của đúng một màn.)

   > **Ba trường, nhưng KHÔNG ba nguồn sự thật.** `canWrite` là nguồn; `isEditable` và
   > `editBlockedBy` **suy ra từ nó** trong **cùng một** lần đánh giá của request — bất biến
   > đầy đủ ở mục `editBlockedBy` dưới đây.
   >
   > Lưới rỗng vẫn có **đủ cả ba**, và đó chính là lý do chúng ở cấp màn: ca T9 cần biết cả
   > "có được bấm Import không" lẫn "nếu không thì vì sao", trong khi không có dòng nào để
   > đọc.

   ### `editBlockedBy` — `isEditable = false` phải nói LÝ DO, không chỉ nói "không"

   Ba điều kiện trượt cho ra **hai kiểu hiển thị** và **ba lời nhắc** khác nhau. Một `bool`
   trần không phân biệt được, và hai điều kiện có thể **cùng lúc** trượt — nên lý do là một
   **mảng mã**, không phải một mã.

   | Mã | Sinh ra khi | Người dùng tự thoát được không |
   | --- | --- | --- |
   | `NO_WRITE_PERMISSION` | `canWrite = false` (Q39) | **Không** — phải xin cấp quyền |
   | `PERIOD_NOT_WEEKLY` | kỳ đang chọn là **tháng** (Q37) | Có — đổi ô `Kỳ trong năm` |
   | `PERIOD_OUT_OF_YEAR` | `period = "all"` **và** `year` ≠ năm hiện tại (T15) | Có — đổi ô `Năm đánh giá`, hoặc chọn một tuần cụ thể |

   **Bất biến — server phải giữ, FE được phép tin:**

   ```
   isEditable = true            ⟺   canWrite = true  VÀ  editBlockedBy = []
   canWrite   = false           ⇒   editBlockedBy = ["NO_WRITE_PERMISSION"]   (ĐÚNG một phần tử)
   canWrite   = true            ⇒   editBlockedBy ⊆ ["PERIOD_NOT_WEEKLY", "PERIOD_OUT_OF_YEAR"]
   thứ tự phần tử                =   đúng thứ tự liệt kê ở bảng trên, luôn luôn
   ```

   > **Vì sao thiếu quyền thì KHÔNG kèm hai mã kia**, dù bộ lọc lúc đó cũng có thể đang sai:
   > hai mã kia là lời mời *"đổi bộ lọc đi rồi sửa được"* — nói câu đó với người không có
   > quyền là dắt họ đi một vòng rồi vẫn không sửa được. Mảng một phần tử ở đây **là** câu trả
   > lời: đây không phải thứ bộ lọc chữa được.
   >
   > Khi `canWrite = true` thì mảng liệt kê **mọi** điều kiện đang trượt, không phải cái đầu
   > tiên — hai ô lọc chặn cùng lúc là ca thật (`Năm = 2025` **và** `Kỳ = Tháng 8`), và một
   > dải băng chỉ nhắc một ô sẽ khiến người dùng sửa xong ô đó rồi vẫn thấy bảng chỉ đọc.

   > **`editBlockedBy` LUÔN có mặt, rỗng chứ không vắng mặt.** Luật `null` ⇒ khoá biến mất
   > (§0) áp cho `null`, không áp cho mảng rỗng — nhưng chỉ cần BE trả `null` thay cho `[]` là
   > FE nhận `undefined` và mọi chỗ `.includes(...)` ném lỗi. Trả `[]`.

   > **FE KHÔNG được tự suy lại ba điều kiện** từ bộ lọc nó đang giữ. Suy lại nghĩa là luật
   > sống ở hai nơi, và nơi thứ hai sẽ lệch ngay lần đầu có điều kiện thứ tư. Server là nguồn
   > duy nhất; FE **đọc** `editBlockedBy` rồi ánh xạ sang câu chữ — đúng khuôn "BE trả mã, FE
   > sở hữu câu chữ" của
   > [`../huong_dan/wiki-core/be/16-i18n-va-ma-loi.md`](../huong_dan/wiki-core/be/16-i18n-va-ma-loi.md)
   > §3. Chọn `ẩn` hay `disabled`, và viết câu nhắc, là việc của `spec/danh-muc-dti/ui-spec.md`.

   > **Một bộ từ vựng, không hai.** Hai mã lọc **trùng tên** với hai mã lỗi mà server trả khi
   > một request ghi lọt qua được lớp FE: `PERIOD_NOT_WEEKLY` ↔
   > `CRITERIA.ASSESSMENT_PERIOD_NOT_WEEKLY`, `PERIOD_OUT_OF_YEAR` ↔
   > `CRITERIA.ASSESSMENT_PERIOD_OUT_OF_YEAR`. Ánh xạ là **cơ học** (bỏ tiền tố
   > `CRITERIA.ASSESSMENT_`), nên bảng dịch của FE dùng lại được và hai đường không thể mô tả
   > cùng một tình huống bằng hai cái tên khác nhau. `NO_WRITE_PERMISSION` không có mã lỗi
   > tương ứng vì ca đó ra `403` từ filter, không qua catalog DTI.

   > **Lịch sử của `isEditable`, để không ai khôi phục nhầm một nhánh cũ:**
   >
   > | Bản | `true` khi |
   > | --- | --- |
   > | DRAFT | năm hiện tại **và** `period = "all"` — trạng thái "Live" |
   > | 2026-09-05 sáng (Q20) | `period` là một kỳ cụ thể |
   > | 2026-09-05 chiều (Q26 + Q27) | có quyền ghi — **không** phụ thuộc `period` |
   > | **2026-09-06 (Q37 + Q39)** | **có quyền ghi VÀ kỳ đang chọn là tuần hoặc `"all"`** |
   >
   > Điều kiện về kỳ quay lại ở bản cuối, nhưng là điều kiện **khác**: về **đơn vị** của kỳ
   > (tuần hay tháng), không phải về **tuổi** của kỳ (đã qua hay hiện tại). Kỳ đã qua vẫn ghi
   > được — Q20 không bị lật.

   Hệ quả về shape: **không trường nào của khối quyền lặp lại ở dòng.** Cả ba chỉ phụ thuộc
   quyền của người gọi, đơn vị kỳ và năm đang lọc — ba thứ ở cấp request — nên chúng có đúng
   **một** chỗ, cạnh `items`. `CriteriaRowDto` trở lại thuần dữ liệu.

   Ẩn control chỉ là lớp trải nghiệm — lớp thật là `403` ở BE (`[RequirePermission]`) và
   `400 CRITERIA.ASSESSMENT_PERIOD_NOT_WEEKLY` / `…_OUT_OF_YEAR` ở tầng validate. Luật đầy đủ + hệ quả (báo cáo
   đã xuất có thể lệch, dấu vết ai sửa kỳ nào): `spec/danh-muc-dti/business-rules.md` §5.4 và
   §5.6.
4. **`assessmentPeriod` / `assessmentPeriodLabel` — kỳ của TỪNG DÒNG (Q31).** Ở chế độ
   `period = "all"`, mỗi dòng có thể đến từ **một kỳ khác nhau** (chỉ tiêu này lấy bản ghi
   tuần 12, chỉ tiêu kia tuần 33) — lưới phải nói ra điều đó, nếu không người đọc so hai dòng
   như thể chúng cùng kỳ.

   - Cả hai **vắng mặt** khi dòng chưa có bản ghi đánh giá nào trong phạm vi đang xem.
   - **BE dựng sẵn chuỗi nhãn**, FE không tự ghép. Ghép được thì FE phải có lịch ISO riêng —
     đúng phép tính mà `spec/danh-muc-dti/business-rules.md` §5.1 cấm tự làm. Khuôn chuỗi và
     lý do: `spec/dashboard-dti/business-rules.md` §6.
   - Màn hình chỉ hiện cột này **ở chế độ `Tất cả`** và **tự đổi giá trị sau khi lưu** — hai
     luật đó là của UI (`spec/danh-muc-dti/ui-spec.md`), không phải của API. API **luôn** trả
     hai trường, ở mọi chế độ.
   - **`assessmentPeriod` LUÔN là một tuần** sau Q37 — không bao giờ là `"YYYY-MM"`, vì không
     có đường nào ghi dữ liệu vào một kỳ tháng.
   - **Q38 (2026-09-06):** cột trên lưới hiện **chỉ khoảng ngày** (`10/08–16/08`), bỏ số tuần
     — cột rộng 110px và sau Q37 thì mọi dòng đều là tuần. Đây là quyết định **hiển thị**;
     hợp đồng **không đổi**, vẫn trả đủ cả hai trường. Ngoại lệ này đăng ký ở
     `spec/dashboard-dti/business-rules.md` §6.2.
5. **Hành vi theo `period`** — quyết định 2026-09-05, khác bản DRAFT cũ ở ca `"all"`:

| `period` | Số dòng | `totalCount` đếm gì |
| --- | --- | --- |
| `"YYYY-Www"` / `"YYYY-MM"` | **đúng 1 dòng / 1 chỉ tiêu chưa xoá** — giá trị lấy từ bản ghi đánh giá có `assessmentDate` lớn nhất **nằm trong** khoảng ngày của kỳ. Không có bản ghi nào trong kỳ → dòng vẫn hiện, mọi trường đánh giá vắng mặt | số **chỉ tiêu** |
| `"all"` (mặc định) | **đúng 1 dòng / 1 chỉ tiêu chưa xoá** — bản ghi **mới nhất trong năm** `year` | số **chỉ tiêu** |

> ⚠️ **Đổi so với bản DRAFT cũ.** Bản cũ định nghĩa `"all"` là *"toàn bộ bản ghi
> `CriteriaAssessment` trong năm, có thể nhiều dòng / 1 chỉ tiêu"*, và `totalCount` khi đó
> đếm **bản ghi** chứ không đếm chỉ tiêu. Bỏ vì ba lý do đo được:
> caption của màn đọc là `"62 chỉ tiêu"` (Q16b) — với ngữ nghĩa cũ nó hiện một con số
> không phải số chỉ tiêu; nhãn của ô lọc trên prototype đã duyệt là
> `"Tất cả (mới nhất trong năm)"`, tức chính ngữ nghĩa mới; và một lưới mà số dòng phụ
> thuộc số lần ai đó bấm sửa thì không phân trang ổn định được.

---

## CONTRACT DM-3 — Tạo chỉ tiêu

- **Status: AGREED** (2026-09-05)
- Route: `POST /api/criteria`
- Request:

```
code:       string    // bắt buộc, maxlength 20, unique trong tập chưa xoá mềm
name:       string    // bắt buộc
groupId:    guid      // bắt buộc
maxScore:   number    // bắt buộc, > 0
assessment: object?   // TUỲ CHỌN — 6 trường của Q9, xem DM-4
```

- Response: `IApiResult<CriteriaDto>` — `{ id, code, name, groupId, groupName, maxScore }`
- Lỗi: `CRITERIA.CODE_REQUIRED` (400) · `CRITERIA.CODE_TOO_LONG` (400) ·
  `CRITERIA.NAME_REQUIRED` (400) · `CRITERIA.MAX_SCORE_INVALID` (400) ·
  `CRITERIA.GROUP_NOT_FOUND` (422) · `CRITERIA.DUPLICATE_CODE` (409)

> **`GROUP_NOT_FOUND` là 422, không phải 404** — `groupId` là FK nằm **trong payload**, không
> phải resource chính của route. Đúng bảng phân biệt ở
> [`../huong_dan/quy-uoc/be-api-controller.md`](../huong_dan/quy-uoc/be-api-controller.md)
> §"Error → HTTP status mapping". Bản card cũ ghi 404 — sai.

⚠️ **Regex validate `code` KHÔNG được giả định đúng 2 cấp.** Dữ liệu thật có mã ba cấp
(`4.22.11`). Ràng buộc `maxlength 20` vẫn đủ; luật đầy đủ ở
`spec/danh-muc-dti/business-rules.md` §Mã chỉ tiêu.

---

## CONTRACT DM-4 — Sửa chỉ tiêu (dialog "Sửa chỉ tiêu")

- **Status: AGREED** (2026-09-05)
- Route: `PUT /api/criteria/{id}`
- Query params: **không có.** Kỳ đích đi trong thân request (xem `assessment.period`).
- Request:

```
code:     string
name:     string
groupId:  guid
maxScore: number

assessment: {              // TUỲ CHỌN — vắng mặt = không đụng tới dữ liệu đánh giá
  period:        string    // BẮT BUỘC khi có `assessment` — KỲ ĐÍCH
                           // "YYYY-Www"  |  "all" (= tuần hiện tại, Q26)
                           // "YYYY-MM" -> 400 NOT_WEEKLY (Q37)
  year:          int?      // BẮT BUỘC khi period = "all" — năm đang xem (T15)
                           // bỏ qua khi period là một tuần cụ thể
  selfScore:     number?
  verifiedScore: number?
  status:        string?   // 1 trong 4 giá trị
  ownerId:       guid?
  deadline:      date?
  note:          string?
  version:       string?   // token đọc được ở DM-2; vắng mặt = không kiểm concurrency
}?
```

> **`assessment.period` là trường mới của vòng 2026-09-05 (Q20), và nó BẮT BUỘC.** Kỳ đích
> đi **trong thân request** chứ không phải trong query string, vì đây là **dữ liệu của lời
> ghi**, không phải trạng thái màn hình. Bản trước gửi `year`/`period` qua query như một
> thứ để server *từ chối*; nay nó là thứ server *ghi theo*.
>
> Không có giá trị mặc định. Vắng mặt ⇒ `400 CRITERIA.ASSESSMENT_PERIOD_REQUIRED`, **không**
> âm thầm rơi về "hôm nay" — im lặng chọn hộ kỳ là đúng cách một bản ghi đi lạc chỗ mà
> không ai biết.

> ### T15 (2026-09-06) — `"all"` không được nhảy năm
>
> Ca đo được: `Năm = 2025` + `Kỳ trong năm = Tất cả`, người dùng sửa một ô ⇒ theo Q26 lời ghi
> rơi vào **tuần hiện tại của 2026**, một năm họ không hề đang xem. Bộ chọn kỳ nói "ghi
> được", còn lệnh ghi thì đi chỗ khác.
>
> **Chốt: chỉ đọc, và chặn ở cả hai lớp.** FE tắt control (`isEditable = false`); BE từ chối
> `400 CRITERIA.ASSESSMENT_PERIOD_OUT_OF_YEAR`.
>
> **Vì sao request phải mang `year`:** chuỗi `"all"` tự nó **không chứa năm nào**. Server quy
> nó về tuần hiện tại và không có cách nào biết người dùng đang nhìn năm nào — trừ khi client
> nói ra. Thiếu `year` thì lớp chặn thứ hai **không tồn tại**, chỉ còn FE ẩn nút; mà ẩn nút là
> trải nghiệm, không phải luật.

> ### Q37 (2026-09-06) — kỳ đích chỉ được là TUẦN
>
> `"YYYY-MM"` **không còn** là kỳ đích hợp lệ ⇒ `400 CRITERIA.ASSESSMENT_PERIOD_NOT_WEEKLY`.
> Tháng là **tổng hợp tự tính**, không phải chỗ nhập liệu. Lý do đo được: mô hình lưu theo
> `AssessmentDate` (một ngày), nên ghi cho "tháng 8/2026" phải neo vào một ngày trong tháng —
> và **không ngày nào** trong một tháng bất kỳ có tuần ISO nằm gọn trong tháng đó (neo 31/08
> ⇒ tuần 36, vắt sang tháng 9). Lý lẽ đầy đủ + hai lối ra đã cân nhắc:
> `spec/danh-muc-dti/business-rules.md` §5.1.
>
> **Ba mã lỗi rời nhau, cố ý:** `REQUIRED` (client quên gửi) · `INVALID` (chuỗi sai khuôn) ·
> `NOT_WEEKLY` (đúng khuôn nhưng là kỳ tháng). Chỉ mã thứ ba mới dựng được câu dẫn đường
> *"chọn một tuần trong tháng để sửa"*; gộp chúng là mất đúng câu đó. Trong luồng bình thường
> FE không kích hoạt `NOT_WEEKLY` — chọn tháng thì `isEditable = false` nên không có control
> nào để bấm. Nó là **lưới chặn phía server**.

> ### `"all"` nay là giá trị HỢP LỆ — sửa 2026-09-05 (Q26)
>
> Bản sáng nay từ chối `"all"` bằng `400 CRITERIA.ASSESSMENT_PERIOD_INVALID`, lý luận rằng
> `"all"` không phải một kỳ nên server sẽ phải đoán. **Người dùng chốt ngược lại:** ở chế độ
> `Kỳ trong năm = Tất cả`, lời ghi **rơi vào kỳ hiện tại**, không chặn, không hỏi lại.
>
> | | Bản sáng | Bản này |
> | --- | --- | --- |
> | `period = "all"` khi ghi | `400 ASSESSMENT_PERIOD_INVALID` | **chấp nhận** — server quy về tuần ISO chứa hôm nay |
> | `period` vắng mặt | `400 ASSESSMENT_PERIOD_REQUIRED` | **không đổi**, vẫn 400 |
> | `ASSESSMENT_PERIOD_INVALID` | dùng cho cả `"all"` lẫn chuỗi sai khuôn | **chỉ còn** chuỗi sai khuôn (`"2026-W99"`, `"tuần 33"`, …) |
> | `period = "YYYY-MM"` khi ghi | hợp lệ | **`400 ASSESSMENT_PERIOD_NOT_WEEKLY`** (Q37, 2026-09-06) |
>
> **Server quy đổi, không phải FE.** Ba lý do: "hôm nay" là đồng hồ của server, một client
> lệch múi giờ hoặc sai giờ hệ thống sẽ ghi vào tuần khác; ba đường ghi (DM-4, DM-6, DM-7)
> phải cho ra cùng một kết quả nên phép quy đổi chỉ được có một chỗ cài; và FE biết kết quả
> ngay trong phản hồi qua `assessmentPeriod`/`assessmentPeriodLabel` (DM-2 mục 4) nên không
> cần đoán trước.
>
> **`period` vắng mặt vẫn là lỗi, đừng gộp nó vào ca `"all"`.** `"all"` là một lựa chọn người
> dùng nhìn thấy trên màn hình và chủ động để nguyên; `period` thiếu là một client quên gửi.
> Đối xử hai ca như nhau nghĩa là mọi bug quên-gửi-tham-số đều âm thầm ghi vào tuần này.
>
> Luật đầy đủ (kỳ đích → `AssessmentDate` nào, upsert theo kỳ, copy-forward):
> `spec/danh-muc-dti/business-rules.md` §5.3.

- Response: `IApiResult<CriteriaRowDto>` — **cùng shape với một dòng của DM-2**, để FE thay
  thẳng dòng trong lưới thay vì gọi lại danh sách.
- Lỗi: `CRITERIA.NOT_FOUND` (404) · `CRITERIA.DUPLICATE_CODE` (409) ·
  `CRITERIA.ASSESSMENT_SELF_SCORE_EXCEEDS_MAX` (422) ·
  `CRITERIA.ASSESSMENT_VERIFIED_SCORE_EXCEEDS_MAX` (422) ·
  `CRITERIA.STATUS_INVALID` (400) · `CRITERIA.OWNER_NOT_FOUND` (422) ·
  `CRITERIA.ASSESSMENT_PERIOD_REQUIRED` (400) ·
  `CRITERIA.ASSESSMENT_PERIOD_INVALID` (400) ·
  **`CRITERIA.ASSESSMENT_PERIOD_NOT_WEEKLY`** (400, mới 2026-09-06) ·
  **`CRITERIA.ASSESSMENT_PERIOD_OUT_OF_YEAR`** (400, mới 2026-09-06) ·
  `CRITERIA.ASSESSMENT_CONFLICT` (409) · lỗi validate 400 giống DM-3 — trong đó có ca
  `year` vắng mặt khi `period = "all"`: đó là **lỗi validate trường bắt buộc** (`fields`),
  không cần một mã nghiệp vụ riêng

> **`CRITERIA.ASSESSMENT_READONLY_PERIOD` đã GỠ khỏi hợp đồng** (Q20). Đừng khai nó trong
> catalog lỗi; đừng giữ nhánh xử lý nó ở FE.

**Vì sao 6 trường đánh giá đi CHUNG một request với 4 trường danh mục, không tách 2 lời
gọi:** trên màn hình đó là **một** nút `Lưu chỉ tiêu` (Q9). Tách đôi tạo ra một cửa sổ hỏng
nửa vời — danh mục lưu xong, đánh giá lỗi, người dùng thấy dialog báo lỗi trong khi tên chỉ
tiêu đã đổi. Một use case một transaction; đây cũng là lý do `assessment` là object lồng
chứ không phải 6 trường phẳng trộn lẫn với 4 trường danh mục: hai nhóm ghi vào **hai
bảng khác nhau** với vòng đời khác nhau, và `assessment` **vắng mặt** phải phân biệt được
với `assessment` có mọi trường `null` (xoá trắng dữ liệu đánh giá).

**Bản ghi đánh giá được ghi vào đâu**: quy tắc upsert theo ngày + copy-forward ở
`spec/danh-muc-dti/business-rules.md` §Ghi đánh giá. Card này không lặp lại.

---

## CONTRACT DM-5 — Xoá chỉ tiêu

- **Status: AGREED** (2026-09-05)
- Route: `DELETE /api/criteria/{id}`
- Response: `IApiResult<DeleteCriteriaResultDto>` — `{ hardDeleted: bool }`
  (`true` = xoá cứng vì chưa từng có bản ghi đánh giá nào; `false` = xoá mềm vì đã có lịch sử)
- Lỗi: `CRITERIA.NOT_FOUND` (404)
- **BE là nơi quyết định cứng hay mềm**, FE chỉ đọc `hardDeleted` để hiện thông báo sau khi
  xoá. FE **không** đoán trước bằng `row.assessmentId !== null` để đổi câu xác nhận: trường
  đó chỉ phản ánh **kỳ đang xem**, không phản ánh toàn bộ lịch sử nhiều năm — bản cũ làm vậy
  và câu xác nhận sai trong đúng ca người dùng cần nó đúng nhất.

---

## CONTRACT DM-6 — Sửa inline trong lưới (đúng 2 trường)

- **Status: AGREED** (2026-09-05)
- Route: `PUT /api/criteria/{id}/assessment`
- Query params: **không có.**
- Request:

```
period:          string    // BẮT BUỘC — KỲ ĐÍCH
                           // "YYYY-Www"  |  "all" (= tuần hiện tại, Q26)
                           // "YYYY-MM" -> 400 NOT_WEEKLY (Q37)
year:            int?      // BẮT BUỘC khi period = "all" — năm đang xem (T15)
progressPercent: number?   // BE kẹp [0,100]; FE cũng kẹp trước khi gửi
note:            string?
version:         string?   // token đọc ở DM-2
```

> `period` cùng ngữ nghĩa và **cùng luật** với `assessment.period` của DM-4 (Q20 + Q26 +
> Q37): kỳ **đích** của lời ghi, bắt buộc, không mặc định, **nhận `"all"`** với nghĩa "tuần
> hiện tại", và **từ chối kỳ tháng**. FE lấy nó từ chính ô lọc "Kỳ trong năm" mà người dùng
> đang chọn và gửi **nguyên giá trị đó** — kể cả khi giá trị đó là `"all"`. Đừng để FE tự quy
> `"all"` thành một tuần cụ thể; xem lý do ở khối Q26 của DM-4.
>
> Khi ô lọc đang chọn một **tháng**, FE không gửi request nào cả: `isEditable = false` nên ô
> sửa inline không mở được (DM-2 mục 3).

- **Đúng 2 trường nghiệp vụ, không hơn** (Q9): `Tiến độ %` và `Minh chứng/Ghi chú`. Bốn
  trường còn lại của bộ 6 chỉ sửa được qua dialog (DM-4). Đây là ràng buộc của hợp đồng,
  không phải giới hạn tạm thời.
- **FE LUÔN gửi cả 2 trường** kể cả khi chỉ sửa 1 — lấy giá trị hiện tại của trường còn lại
  từ dòng đang có trong bộ nhớ. Ngữ nghĩa đã chốt của endpoint này là **ghi đè cả hai**
  (`PUT`), không phải patch từng phần: một `PUT` mà bỏ trống trường nào thì null-hoá trường
  đó. Bản card cũ để ngỏ đúng câu hỏi này và dặn "backend-expert xác nhận sau" — nay chốt.
- Response: `IApiResult<CriteriaRowDto>` — cùng shape DM-2, FE thay dòng tại chỗ.
- Lỗi: `CRITERIA.NOT_FOUND` (404) · `CRITERIA.ASSESSMENT_PERIOD_REQUIRED` (400) ·
  `CRITERIA.ASSESSMENT_PERIOD_INVALID` (400) ·
  **`CRITERIA.ASSESSMENT_PERIOD_NOT_WEEKLY`** (400, mới 2026-09-06) ·
  **`CRITERIA.ASSESSMENT_PERIOD_OUT_OF_YEAR`** (400, mới 2026-09-06) ·
  `CRITERIA.ASSESSMENT_CONFLICT` (409) · `CRITERIA.PROGRESS_PERCENT_INVALID` (400)

---

## CONTRACT DM-7 — Import `.csv` / `.xlsx` / `.xls` (job nền + poll)

- **Status: AGREED** (2026-09-05)

### Bước 1 — bắt đầu import

- Route: `POST /api/import`, `multipart/form-data`
- Trường của form:
  - `file` — `.csv`, `.xlsx`, `.xls` (Q6). **Định dạng nhận diện bằng magic byte, không
    bằng phần mở rộng** — luật §2a của
    [`../huong_dan/wiki-core/be/15-import-export.md`](../huong_dan/wiki-core/be/15-import-export.md).
  - `period` — **BẮT BUỘC**, `"YYYY-Www"` | `"all"`. Kỳ đích của **toàn bộ** file. Cùng luật
    với DM-4/DM-6 (Q20 + Q26 + Q37 + T15): `"all"` ⇒ tuần hiện tại; vắng mặt ⇒
    `400 IMPORT.PERIOD_REQUIRED`; chuỗi sai khuôn ⇒ `400 IMPORT.PERIOD_INVALID`; **kỳ tháng
    ⇒ `400 IMPORT.PERIOD_NOT_WEEKLY`** (Q37 — nạp số liệu cả tháng thì nạp theo từng tuần, và
    đó cũng đúng cách BA đang làm việc: file mẫu của họ là file **một kỳ**).
  - `year` — **BẮT BUỘC khi `period = "all"`**, năm đang xem (T15). ≠ năm hiện tại ⇒
    `400 IMPORT.PERIOD_OUT_OF_YEAR`. Bỏ qua khi `period` là một tuần cụ thể.

    > ⚠️ Ca đáng lưu ý: nạp một file **của kỳ cũ** trong lúc màn hình đang ở chế độ `Tất cả`
    > sẽ đổ 62 dòng vào **tuần hiện tại**, không phải kỳ mà file nói tới — hệ thống không đọc
    > kỳ từ nội dung file. Theo Q26 đây là hành vi đúng và **không** chặn; việc cho người dùng
    > thấy kỳ đích trước khi bấm Nhập thuộc `spec/danh-muc-dti/ui-spec.md`.
- Response: `IApiResult<{ jobId: guid }>` — object bọc, **không** trả `Guid` trần, để thêm
  trường sau (vd `estimatedRows`) không phá shape.

> ### 🔴 ĐIỀU KIỆN TIÊN QUYẾT của card này — hạng mục Core, chốt Q35 (2026-09-06)
>
> **Không bật đường import lên môi trường thật khi nhật ký còn ghi `"system"`.** Job nền không
> có `HttpContext` nên `AuditInterceptor` ghi `UpdatedBy = "system"` cho cả 62 dòng — tức thao
> tác **rủi ro nhất** của hệ thống (nạp đè một kỳ đã báo cáo) lại là thao tác **không có tên
> người**. Người dùng chốt: phải ghi **đúng tài khoản đã bấm nút nạp file**.
>
> Ba bước, hiện trạng đã đo, và điều **không** phải sửa (`AuditInterceptor`) — cùng ranh giới
> Core: `spec/danh-muc-dti/business-rules.md` §5.6. Đây là **thay đổi Core**, đi qua
> `core-reviewer`; card này chỉ ghi ràng buộc thứ tự.

> **`period` là trường mới 2026-09-05 (Q20).** Bản trước ghi *"`AssessmentDate` = ngày hệ
> thống lúc import"*, tức nạp một file của tuần 33 vào tháng 9 sẽ ghi dữ liệu đó vào tuần
> 36. Nay người dùng chọn kỳ trước khi nạp, đúng cùng ngữ nghĩa với DM-4/DM-6 — **một luật
> ghi cho cả ba đường**, không phải ba luật.

> ⚠️ **HTTP status là 200, KHÔNG phải 202.** `ApiControllerBase.HandleResult<T>` map mọi
> response thành công về 200 — `src/BE/PlatformManager.Api/Common/ApiControllerBase.cs:25`
> — và `ErrorCode` không có member nào mang giá trị 202
> (`src/BE/Core/PlatformManager.Core.Application/Common/Results/ErrorCode.cs:11`). "Đã bắt
> đầu chứ chưa xong" thể hiện ở tầng dữ liệu (`jobId` cần poll tiếp), không ở HTTP status.
> Một endpoint tự trả 202 là ngoại lệ duy nhất phá quy ước "1 chỗ map HTTP" của toàn hệ
> thống.
>
> **Hai file `doc/` từng mô tả `202` cho đúng khuôn này đã được sửa 2026-09-05** — đối chiếu
> lại ngày 2026-09-05: [`../huong_dan/quy-uoc/be-cqrs-handler.md`](../huong_dan/quy-uoc/be-cqrs-handler.md)
> §"Command chạy lâu → job nền" nay mở đầu bằng khối *"pattern này KHÔNG trả 202"* và đoạn mẫu
> đã bỏ `return Accepted(...)`;
> [`../huong_dan/wiki-core/be/15-import-export.md`](../huong_dan/wiki-core/be/15-import-export.md)
> §3 nay ghi `200 + jobId trong envelope`. Không còn nguồn nào để chép nhầm — giữ đoạn này để
> người từng đọc bản cũ của cả ba file biết chúng đã hội tụ, chứ không phải còn lệch.

- Lỗi: `IMPORT.FILE_MISSING` (400) · `IMPORT.FILE_EMPTY` (400) ·
  `IMPORT.FORMAT_UNSUPPORTED` (400) · `IMPORT.FILE_TOO_LARGE` (400) ·
  `IMPORT.PERIOD_REQUIRED` (400) · `IMPORT.PERIOD_INVALID` (400) ·
  **`IMPORT.PERIOD_NOT_WEEKLY`** (400, mới 2026-09-06 — Q37) ·
  **`IMPORT.PERIOD_OUT_OF_YEAR`** (400, mới 2026-09-06 — T15)

### Bước 2 — poll trạng thái

- Route: `GET /api/import/{jobId}`
- Response: `IApiResult<ImportJobStatusDto>`

```
status:       "Pending" | "Running" | "Succeeded" | "Failed"
result: {                       // chỉ có khi status = "Succeeded"
  totalRows:            int
  successCount:         int
  errorCount:           int
  criteriaCreatedCount: int
  errors: [ {
    rowNumber:     int
    code:          string                       // businessCode, vd "IMPORT.ROW_GROUP_NOT_FOUND"
    messageParams: { [name: string]: string }?  // tham số RỜI, khoá là TÊN
  } ]
}?
errorMessage: string?           // chỉ có khi status = "Failed" — dev-facing, KHÔNG để hiển thị
```

> ### ⚠️ Sửa 2026-09-05 — `errors[].message` đã GỠ. BE không ghép câu tiếng Việt.
>
> Bản trước cho `errors[]` mang một câu tiếng Việt BE dựng sẵn
> (*"Điểm tự đánh giá (6) vượt quá điểm tối đa (5)"*). Trái
> [`../huong_dan/wiki-core/be/16-i18n-va-ma-loi.md`](../huong_dan/wiki-core/be/16-i18n-va-ma-loi.md)
> §3: với mọi kênh **có FE tiêu thụ**, **FE sở hữu câu chữ**, BE trả **mã + tham số có cấu
> trúc**. Chính §3 nêu đúng ca này làm ví dụ: BE ghép sẵn *"Mã chỉ tiêu 'ABC' đã tồn tại."*
> thì FE **không tách lại được** `ABC` ra khỏi câu.
>
> Cơ chế không phải xây mới — nó đã có thật trong envelope:
> `src/BE/Core/PlatformManager.Core.Application/Common/Results/ApiResult.cs:19`
> (`MessageParams`), chính sách lọc khoá ở
> `src/BE/Core/PlatformManager.Core.Application/Common/Results/MessageParamPolicy.cs`
> (đối chiếu 2026-09-05). `errors[]` dùng **cùng khuôn**: khoá là **TÊN** tham số
> (`{MaxScore}`), không phải số thứ tự — §10.5 của file i18n.
>
> Ví dụ một phần tử:
>
> ```
> { "rowNumber": 17,
>   "code": "IMPORT.ROW_SELF_SCORE_EXCEEDS_MAX",
>   "messageParams": { "Code": "4.2", "SelfScore": "6", "MaxScore": "5" } }
> ```
>
> FE ráp `messageParams` vào bảng dịch của `code`. **`Code` (mã chỉ tiêu) là một tham số,
> không phải một trường riêng** — nếu tách ra thành trường thì mọi mã lỗi không liên quan
> tới chỉ tiêu cũng phải mang nó.
>
> ⚠️ **Allowlist áp cho `messageParams` ở đây y như ở envelope**: chỉ đưa ra tham số mô tả
> **chính sách/vị trí**, không bao giờ đưa ra **giá trị người dùng vừa nhập** ngoài những
> giá trị đã có sẵn trong file họ tự gửi lên (mã, tên nhóm, điểm). Lý do và cơ chế:
> `MessageParamPolicy` + §10.4 của file i18n.

- `rowNumber` là **số dòng trong file người dùng gửi** (dòng 1 = header), để câu lỗi trên
  dialog kết quả trỏ đúng chỗ người dùng mở file ra sửa.
- `errors` là lỗi **từng dòng** — job vẫn `Succeeded`. `Failed` dành cho lỗi hạ tầng (file
  hỏng, job crash), khi đó `result` vắng mặt.
- `errorMessage` giữ đúng vai **dev-facing + fallback** như `message` của envelope gốc
  (§3 của file i18n) — FE ghi log/hiện cho quản trị, **không** dùng làm câu cho người dùng
  cuối. Lỗi hạ tầng thì không có mã nghiệp vụ để dịch, và đó là chủ đích.
- Ánh xạ 11 cột, quy tắc tạo mới / báo lỗi từng dòng, quy tắc khớp `Phụ trách`:
  `spec/danh-muc-dti/business-rules.md` §Import. Card này không lặp lại.
- **Cột `Chênh lệch` trong file: ĐỌC VÀO RỒI VỨT.** Nó là trường tính (§1), nên đường import
  không đọc, không đối chiếu, không báo lỗi khi lệch, và **không đòi nó phải có mặt**. Sau
  Q25 điều này còn quan trọng hơn: cột đó trong file gốc của BA tính theo chiều **cũ**, tức
  ngược dấu với thứ hệ thống tính ra ở **27/62 dòng**. Một đường import có đối chiếu sẽ từ
  chối gần nửa file gốc và đổ lỗi cho dữ liệu của BA.

### Hạ tầng — cái nào có sẵn, cái nào phải dựng

> 🔄 **LẬT 2026-09-06.** Mục này từng mang tiêu đề *"Hạ tầng dùng lại, không dựng mới"* và
> liệt cả hai seam như nhau. Đo lại: **chỉ seam job nền là có thật.** `IImportFileReader`,
> CsvHelper và NPOI **không tồn tại** ở `src/BE` (`grep -rn "IImportFileReader\|CsvHelper\|NPOI"
> src/BE --include=*.cs --include=*.csproj` → rỗng), và `be/15-import-export.md` mang nhãn
> `📐 ĐÍCH ĐẾN — CHƯA THI CÔNG` ở `doc/README.md`. Xếp một seam chưa tồn tại vào cùng danh
> sách với một seam đã chạy là cách người thi công ước lượng thiếu nguyên một hạng mục Core.

- ✅ **Có sẵn** — enqueue qua seam `IBackgroundJobScheduler`
  (`src/BE/Core/PlatformManager.Core.Application/Common/Interfaces/IBackgroundJobScheduler.cs:29`),
  hiện thực Hangfire ở
  `src/BE/Core/PlatformManager.Core.Infrastructure/BackgroundJobs/HangfireBackgroundJobScheduler.cs`
  (đối chiếu 2026-09-06). **Không** gọi thẳng `BackgroundJob.Enqueue` — `LayerDependencyTests`
  cưỡng chế việc chỉ `Core.Infrastructure` được biết tới Hangfire.
- 📐 **PHẢI DỰNG** — đọc file qua seam `IImportFileReader` ở Core (CsvHelper cho CSV, NPOI cho
  `.xls`/`.xlsx`). **NPOI chỉ được reference ở `Core.Infrastructure`** — luật §2 của
  `15-import-export.md`. Đây là hạng mục **Core** thứ ba của card này, cùng nhóm với Q35 và
  Q36 ở §4.
- File upload **không sống sót** qua ranh giới request → job nền: ghi ra storage tạm trước
  khi enqueue. 📖 [`../huong_dan/wiki-core/be/14-file-storage.md`](../huong_dan/wiki-core/be/14-file-storage.md).
- **Quyền của đường import: ĐÃ CHỐT (Q27, 2026-09-05)** — dùng **chung** permission-key ghi
  của cả nghiệp vụ DTI, không có key riêng cho import và **không** dùng lại `import.manage`
  (`src/BE/PlatformManager.Api/Permissions/AppResourceKeySource.cs:33` — key di sản của module
  đã gỡ). Tên key + lý do + ba bước khai: `spec/danh-muc-dti/business-rules.md` §6.5.

---

## CONTRACT DM-8 — Danh sách Năm/Kỳ có dữ liệu

- **Status: AGREED** (2026-08-16, giữ nguyên 2026-09-05)
- Dùng **chung** route với Dashboard: `GET /api/dashboard/periods`.
- Đặc tả đầy đủ ở [`dashboard.md`](dashboard.md) CONTRACT DB-3 — **không mô tả lại ở đây**.
- Owner FE: một service dùng chung ở `shared/`, không đặt trong `modules/danh-muc-dti/`
  (≥2 feature dùng — 📖 [`../huong_dan/quy-uoc/fe-architecture.md`](../huong_dan/quy-uoc/fe-architecture.md)).

---

## 2. Catalog mã lỗi — bắt buộc, và ArchTest cưỡng chế bằng máy

Mọi mã lỗi liệt ở các card trên phải khai thành `static readonly ErrorDescriptor` trong một
**file catalog** đặt tên `{Entity}Errors.cs`, cạnh handler:

| File | Đặt ở | Giữ mã có tiền tố |
| --- | --- | --- |
| `CriteriaErrors.cs` | `PlatformManager.Business.Application/Criteria/` | `CRITERIA.*` |
| `ImportErrors.cs` | `PlatformManager.Business.Application/Import/` | `IMPORT.*` |
| `DashboardErrors.cs` | `PlatformManager.Business.Application/Dashboard/` | `DASHBOARD.*` — xem [`dashboard.md`](dashboard.md) |

**Đây không phải quy ước thẩm mỹ — nó được kiểm bằng máy, và không khai là test ĐỎ:**

- `src/BE/Tests/PlatformManager.ArchTests/ErrorCodeSourceTests.cs:158` — file catalog nhận
  diện **bằng đuôi tên** `Errors.cs`. Dựng `ErrorDescriptor` bằng chuỗi literal ngay trong
  thân handler bị chặn ở đây.
- `src/BE/Tests/PlatformManager.ArchTests/ErrorCatalogTests.cs` — reflection đọc **giá trị
  thật** của từng descriptor sau khi biên dịch: canh khuôn `MIEN.MA_LOI` và canh **trùng
  `businessCode`** giữa mọi catalog. Hai test bù nhau; không cái nào một mình đủ.

Ràng buộc nội dung của từng descriptor:

- `BusinessCode` — `"{ENTITY}.{ERROR}"`, UPPER_SNAKE có chấm.
- `ErrorCode` — **giá trị enum CHÍNH LÀ mã HTTP**, không có bảng map thứ hai.
- `MessageTemplate` — chỗ giữ **ĐẶT TÊN** (`"Mã chỉ tiêu '{Code}' đã tồn tại."`), **không**
  phải `{0}`. Câu này giữ vai *dev-facing + fallback*; câu người dùng đọc do FE dựng từ
  `businessCode` + `messageParams`.
- `Retryable` — `false` cho toàn bộ mã của card này. Không mã nào ở đây là lỗi thoáng qua:
  trùng mã, sai nhóm, sai trạng thái, xung đột phiên bản đều cần người sửa dữ liệu rồi mới
  gọi lại. Đặt `true` nghĩa là bảo FE tự thử lại — và thử lại một `DUPLICATE_CODE` thì lần
  nào cũng hỏng y hệt.

📖 Khuôn `ErrorDescriptor` + ví dụ:
[`../huong_dan/quy-uoc/be-cqrs-handler.md`](../huong_dan/quy-uoc/be-cqrs-handler.md)
§`ErrorDescriptor`.

---

## 3. Bảng đối chiếu nhanh — bản DRAFT cũ → bản này

Để người từng đọc bản cũ không mang theo giả định đã bị bỏ.

| Bản DRAFT cũ | Bản 2026-09-05 | Vì sao |
| --- | --- | --- |
| `evidences: [...]` trong dòng lưới | **gỡ**, còn `note: string?` | Q5 |
| `badge` / trạng thái hệ thống tự tính | `status` 4 giá trị người dùng **chọn tay** | Q4 |
| `period="all"` trả nhiều dòng / 1 chỉ tiêu | 1 dòng / 1 chỉ tiêu, bản mới nhất trong năm | Xem ghi chú ở DM-2 |
| Không có bộ lọc `status` | `status` là tham số lọc của DM-2 | Q4 + prototype đã duyệt |
| `CRITERIA.GROUP_NOT_FOUND` = 404 | = **422** | FK trong payload, không phải resource của route |
| DM-6 để ngỏ "PATCH hay PUT toàn phần" | chốt **PUT ghi đè cả 2 trường** | Xem DM-6 |
| Dialog sửa có 4 trường | 4 trường danh mục + object `assessment` 6 trường | Q9 |
| Import CSV-only, đồng bộ | `.csv`/`.xlsx`/`.xls`, job nền + poll | Q6 |
| Casing "CHƯA XÁC NHẬN" | **CHỐT** theo source | §0 |

### Đổi thêm ở vòng sửa 2026-09-05 (sau audit)

| Bản 2026-09-05 (sáng) | Bản 2026-09-05 (vòng sửa) | Vì sao |
| --- | --- | --- |
| `pageSize` mặc định **20** | **10** | Q19 |
| Lời ghi luôn vào "hôm nay"; kỳ quá khứ **chỉ đọc**; `isEditable` true khi năm hiện tại + `period="all"` | Lời ghi mang **`period` đích tường minh**; `isEditable` true khi đang xem **một kỳ cụ thể** | **Q20** |
| `CRITERIA.ASSESSMENT_READONLY_PERIOD` (409) | **gỡ khỏi hợp đồng** | Q20 |
| `POST /api/import` chỉ nhận `file` | nhận thêm `period` **bắt buộc** | Q20 |
| `errors[]` mang `message` tiếng Việt do BE ghép | `{ rowNumber, code, messageParams }` | `16-i18n-va-ma-loi.md` §3 + §10 |
| Dùng lại `import.manage` (quyết định đơn phương) | đưa vào gói **quyền ghi chưa chốt** | Quyền là quyết định của người dùng |
| Chưa nói mã lỗi khai ở đâu | catalog `{Entity}Errors.cs` bắt buộc — §2 | ArchTest cưỡng chế |
| §0 chép nguyên luật casing chung | rút còn 1 dòng trỏ + **giữ** hệ quả `null` ⇒ vắng khoá | `.claude/CLAUDE.md` §5 |

### Đổi thêm ở vòng sửa THỨ BA (2026-09-05)

| Bản vòng 2 | Bản này | Vì sao |
| --- | --- | --- |
| `diff = selfScore − verifiedScore` | **`= verifiedScore − selfScore`** | **Q25** |
| `period = "all"` khi ghi ⇒ `400` | **chấp nhận** ⇒ server quy về kỳ hiện tại | **Q26** |
| `isEditable` = "đang xem một kỳ cụ thể" | **= "người gọi có quyền ghi DTI"** | Q26 + **Q27** |
| Quyền ghi "chưa chốt", `import.manage` để ngỏ | **một key** cho toàn bộ DTI, mọi kỳ | **Q27** |
| Dòng lưới không nói mình thuộc kỳ nào | thêm `assessmentPeriod` + `assessmentPeriodLabel` | **Q31** |
| Card không ghi địa chỉ màn | `/danh-muc/dti` | **Q33** |
| Nhật ký thay đổi: chưa nói gì | 4 trường audit của `BaseEntity`, **không** bảng lịch sử | **Q28** |

### Đổi thêm ở vòng chốt cuối (2026-09-06)

| Bản vòng 3 | Bản này | Vì sao |
| --- | --- | --- |
| Ghi được vào kỳ **tháng** | **chỉ ghi theo TUẦN**; kỳ tháng ⇒ `400 …PERIOD_NOT_WEEKLY` | **Q37** |
| `isEditable` = "có quyền ghi" | tách **hai cờ**: `canWrite` (quyền) + `isEditable` (`canWrite` **và** kỳ ghi được) | Q37 + **Q39** |
| Chưa nói người thiếu quyền thấy gì | vào được màn, **chỉ đọc**; không chặn route, không ẩn menu | **Q39** |
| Import ghi `UpdatedBy = "system"` — ghi thành mục còn mở | phải ghi **đúng người đăng nhập**; hạng mục **Core**, **điều kiện tiên quyết** của DM-7 | **Q35** |
| Seed cấp key DTI cho `Admin` + `User` — cảnh báo và bảo bỏ tick tay | seed **chỉ cấp cho `Admin`**; ngoại lệ có đăng ký | **Q36** |
| Cột `Kỳ của số liệu` hiện nhãn kỳ đầy đủ | hiện **chỉ khoảng ngày** `10/08–16/08` (chỉ là hiển thị, hợp đồng không đổi) | **Q38** |
| `"all"` ghi được ở mọi năm đang lọc | `"all"` + `year` ≠ năm hiện tại ⇒ `400 …PERIOD_OUT_OF_YEAR`; đường ghi nhận thêm `year` | **T15** |
| Cờ quyền nằm trong từng dòng | cả khối quyền (`canWrite` · `isEditable` · `editBlockedBy`) chuyển lên **cấp màn** — lưới rỗng vẫn quyết được ẩn/hiện nút và vẫn nói được lý do | lỗ hổng Q39 × T9 |
| `isEditable` là một `bool` trần | kèm **`editBlockedBy: string[]`** — phân biệt đủ 3 ca và diễn đạt được **hai điều kiện cùng trượt** | 3 lời nhắc ≠ 1 câu chung |

---

## 4. Cần chốt — KHÔNG tự quyết

**Card này không còn mục nào để ngỏ.** Mục cuối cùng — quyền GHI — đóng bằng **Q27**
(2026-09-05): đúng một permission-key cho toàn bộ đường ghi DTI, áp cho mọi kỳ, không tách
"sửa kỳ hiện tại" / "sửa kỳ cũ", không có khái niệm "chốt kỳ". Tên key và ba bước khai:
`spec/danh-muc-dti/business-rules.md` §6.5.

| Mục cũ | Đóng bằng | Ghi ở |
| --- | --- | --- |
| Quyền **ghi** của màn (kể cả import) | **Q27** | `spec/danh-muc-dti/business-rules.md` §6.5 · §1 của card này |
| Quyền **xem** Dashboard | Q21 | [`dashboard.md`](dashboard.md) §0 |
| `Tiến độ %` khi import | Q24 — để trống | `spec/danh-muc-dti/business-rules.md` §6.4 |
| Export theo bộ lọc đang áp | Q23 — CÓ | [`dashboard.md`](dashboard.md) DB-4 |
| Nhật ký "ai sửa kỳ nào" | Q28 — 4 trường audit của `BaseEntity`, không bảng lịch sử | `spec/danh-muc-dti/business-rules.md` §5.6 |
| Tên thư mục `spec/` | đã xong 2026-09-05 | dưới đây |

> **Tên thư mục `spec/` — ĐÃ XONG 2026-09-05.** Chốt `spec/danh-muc-dti/` +
> `spec/dashboard-dti/`; thư mục `spec/dashboard-dti-weekly/` không còn tồn tại. Cả **ba**
> chỗ trỏ tên cũ đã sửa cùng lượt: `doc/cau-truc-database.md`,
> `doc/Design/Frontend/PlatformManager/DESIGN.md`,
> `doc/huong_dan/quy-uoc/be-api-controller.md`.
>
> ⚠️ Ba tham chiếu đó **cổng không bắt được**: `check-docs.sh` §4 chỉ kiểm đường dẫn mang
> tiền tố `src/` và `doc/`, không kiểm `spec/`. Chúng trỏ vào một thư mục đã bị xoá mà gate
> vẫn xanh suốt. Nếu sau này còn đổi tên thư mục `spec/`, phải grep tay.

**Hai câu hỏi từng CÒN MỞ đã đóng ngày 2026-09-06** — bằng **Q37** (chỉ nhập theo tuần, nên
không có ca "ghi vào kỳ tháng" để mà báo sai đơn vị) và **Q35** (nhật ký phải mang đúng người
đăng nhập). Cả cụm DTI nay **không còn mục nghiệp vụ nào để ngỏ**:
`spec/danh-muc-dti/business-rules.md` §8.

⚠️ **Nhưng "hết mục cần chốt" KHÔNG có nghĩa là làm được ngay.** **Ba** hạng mục **Core** là
điều kiện tiên quyết, và cả ba đều đi qua `core-reviewer`:

| Hạng mục Core | Chặn cái gì | Đặc tả |
| --- | --- | --- |
| Danh tính trong job nền (Q35) | **DM-7 — toàn bộ đường import** | `spec/danh-muc-dti/business-rules.md` §5.6 |
| Seed permission-key theo vai (Q36) | quyền ghi đúng như đã chốt; nếu bỏ qua thì vai `User` được cấp sẵn, ngược Q36 | `spec/danh-muc-dti/business-rules.md` §6.5 |
| **Seam `IImportFileReader`** (thêm 2026-09-06) | **DM-7 — toàn bộ đường import.** Chưa tồn tại ở `src/BE`, xem DM-7 §Hạ tầng | [`../huong_dan/wiki-core/be/15-import-export.md`](../huong_dan/wiki-core/be/15-import-export.md) §2 |

---

## 5. Nghiệm thu — điều kiện chuyển card sang `IMPLEMENTED`

Không chuyển bằng "code xong, build xanh". Đủ **cả** danh sách dưới đây, không bỏ mục nào —
hai mục cuối là nghiệm thu của hai quyết định chốt ngày 2026-09-06:

1. Gọi thật từng route (Swagger hoặc `curl`) trên DB đã có dữ liệu, **dán shape response
   thật** vào card — không dán shape suy ra từ DTO.
2. Kiểm đúng ca "chỉ tiêu chưa có đánh giá": xác nhận các khoá `selfScore`/`verifiedScore`/
   `status` **vắng mặt** khỏi JSON chứ không phải bằng `null` (§0).
3. Kiểm ca lỗi `CRITERIA.DUPLICATE_CODE` trả `code: "Conflict"` + HTTP 409, và ca validate
   trả `fields` với khoá **PascalCase**.
4. Import một file `.xls` **đã đổi đuôi thành `.xlsx`** — phải nhận được
   `IMPORT.FORMAT_UNSUPPORTED` hoặc đọc đúng, **không** được ném exception hạ tầng (luật
   magic byte, §2a của `15-import-export.md`).
5. **Ca Q20:** chọn một kỳ **đã qua**, sửa một chỉ tiêu, rồi mở lại đúng kỳ đó — giá trị
   phải nằm ở kỳ được chọn, **không** ở kỳ hiện tại. Sau đó mở kỳ hiện tại: phải **không**
   thấy thay đổi nào.
6. **Ca B1:** gửi một file có dòng sai nhóm, xác nhận `errors[0]` mang `code` +
   `messageParams`, và **không** có khoá `message` nào.
7. **Ca Q25 — dấu của `diff`.** Trên dữ liệu gốc của BA: chỉ tiêu `1.1` phải trả
   `diff = 2.96` (**dương**), chỉ tiêu `1.4` phải trả `diff = -5.00`. Nếu ra `-2.96` thì
   công thức còn theo chiều cũ. Nạp lại **đúng file gốc** một lần nữa và xác nhận **không
   dòng nào** báo lỗi vì cột `Chênh lệch` — cột đó phải bị bỏ qua hoàn toàn.
8. **Ca Q26 — ghi ở chế độ `Tất cả`.** Đặt `period=all`, sửa một chỉ tiêu, rồi: (a) mở kỳ
   **tuần hiện tại** — phải thấy giá trị mới; (b) mở kỳ mà dòng đó **vốn lấy số** (vd tuần
   12) — số cũ phải **còn nguyên**; (c) phản hồi của chính lời ghi phải mang
   `assessmentPeriod` = tuần hiện tại. Không có bước (c) thì FE không đổi được ô
   `Kỳ của số liệu`, và Q31(b) hỏng trong im lặng.
9. **Ca Q27 + Q36 — quyền và seed.** Gọi một endpoint ghi bằng tài khoản **không** có key ⇒
   `403`. Mở màn phân quyền: key DTI phải **có mặt** trong `rows` (thiếu nó nghĩa là quên
   bước khai `Definitions`, và triệu chứng là 403 cho tất cả trừ SuperAdmin). Rồi kiểm đúng
   **ngoại lệ Q36**: sau khi seed, dòng key DTI có `Admin` và **KHÔNG có `User`** — luật seed
   mặc định cấp cho cả hai, nên đây là bước kiểm một ngoại lệ, thứ dễ bị một lần refactor
   "cho nhất quán" xoá mất mà không có triệu chứng nào
   (`spec/danh-muc-dti/business-rules.md` §6.5).
10. **Ca Q28 + Q35 — dấu vết.** Sửa một chỉ tiêu bằng tài khoản A, đọc `UpdatedBy`/`UpdatedAt`
    của bản ghi đánh giá: phải là tên đăng nhập của A. Rồi **chạy import bằng chính A**:
    `UpdatedBy` cũng phải là **A**, không phải `"system"`. Đây là nghiệm thu của hạng mục Core
    Q35 — nếu còn ra `"system"` thì đường import **chưa đủ điều kiện bật** (§5.6 của file luật
    nghiệp vụ), bất kể mọi thứ khác đã xanh.
11. **Ca Q37 — chỉ nhập theo tuần.** Chọn một kỳ **tháng** ở ô `Kỳ trong năm`: `isEditable`
    phải là `false` trong khi `canWrite` vẫn `true`. Gọi thẳng DM-4 hoặc DM-6 với
    `period = "2026-08"` ⇒ `400 CRITERIA.ASSESSMENT_PERIOD_NOT_WEEKLY` (không phải `INVALID`,
    không phải 200). Kiểm ca âm quan trọng: **export `mode=month` vẫn tải được file bình
    thường** — Q37 không chạm đường đọc.
12. **Ca Q39 — chỉ đọc vì thiếu quyền.** Đăng nhập bằng tài khoản không có key: DM-2 phải trả
    `200` với `canWrite = false` (**không** phải 403), đủ 62 dòng, và `GET /api/dashboard/export`
    vẫn tải được. Route `/danh-muc/dti` **không** bị chặn và mục menu **không** bị ẩn.
13. **Ca lưới RỖNG — chỗ cờ cấp dòng không cứu được.** Trên DB **chưa import lần nào**, gọi
    DM-2: response phải có `canWrite` **đúng giá trị** trong khi `items` là mảng rỗng. Chạy
    hai lần, một bằng tài khoản có quyền (`true`) và một bằng tài khoản không quyền (`false`)
    — nếu hai lần cho cùng kết quả thì cờ đang bị tính sai hoặc bị hardcode. Đây là ca mà bản
    trước của card **không** trả lời được, và cũng là lúc nút `Import CSV/Excel` quan trọng
    nhất.
14. **Ca T15 — `"all"` không nhảy năm.** Đặt `year = 2025`, `period = "all"`: DM-2 phải trả
    `isEditable = false` trong khi `canWrite` vẫn `true`. Gọi thẳng DM-6 với
    `period = "all"`, `year = 2025` ⇒ `400 CRITERIA.ASSESSMENT_PERIOD_OUT_OF_YEAR`. Rồi kiểm
    **ca âm bắt buộc**: cùng `year = 2025` nhưng `period = "2025-W33"` ⇒ **ghi thành công**,
    và giá trị nằm ở tuần 33 của **2025**. Thiếu ca âm này thì một bản cài "cấm ghi vào năm
    cũ" sẽ qua được nghiệm thu trong khi nó đã lật mất Q20.
15. **Ca `editBlockedBy` — bốn tổ hợp, kiểm đủ bốn.** Đây là trường dễ cài "gần đúng" nhất:
    một bản cài chỉ trả mã đầu tiên vẫn qua được ba ca đầu.

    | Bộ lọc / tài khoản | `canWrite` | `isEditable` | `editBlockedBy` |
    | --- | --- | --- | --- |
    | có quyền · `year` = năm hiện tại · kỳ = một tuần | `true` | `true` | `[]` |
    | có quyền · `year` = năm hiện tại · kỳ = **Tháng 8** | `true` | `false` | `["PERIOD_NOT_WEEKLY"]` |
    | có quyền · **`year` = 2025** · kỳ = **Tháng 8** | `true` | `false` | **`["PERIOD_NOT_WEEKLY", "PERIOD_OUT_OF_YEAR"]`** — hai phần tử, đúng thứ tự |
    | **không quyền** · bộ lọc bất kỳ, kể cả đang sai cả hai | `false` | `false` | **`["NO_WRITE_PERMISSION"]`** — đúng MỘT phần tử |

    Kiểm thêm: khi `isEditable = true`, khoá `editBlockedBy` **có mặt** với giá trị `[]` —
    không được vắng mặt (§0 chỉ bỏ khoá khi giá trị là `null`, và `null` ở đây là sai).

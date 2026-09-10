---
kind: luat
scope: du-an
verified: chua-doi-chieu
---

# Luật nghiệp vụ — Dashboard DTI

> ## 📐 ĐÍCH ĐẾN — CHƯA THI CÔNG (viết 2026-09-05)
>
> Không có dòng code nào của tính năng này tồn tại. `src/FE/src/app/modules/dashboard/` và
> module BE `DtiWeekly` gỡ 2026-08-29. Mọi câu dưới đây là **luật phải hiện thực**.
>
> **Đích đến kiến trúc: `PlatformManager.Business.*`** — query tổng hợp vào
> `Business.Application/Dashboard/`, controller vào `Business.Api`. **KHÔNG** dựng lại
> `Modules.DtiWeekly.*`. 📖 `doc/kien-truc-core-module.md`

**Dashboard 100% ĐỌC.** Không có hành động ghi nào, kể cả nút "Xuất báo cáo" — nó tải một
file về máy người dùng, không tạo dữ liệu trên server. Mọi lời ghi của sản phẩm nằm ở màn
Danh mục DTI.

**File này giữ gì:** công thức tổng hợp, quy tắc so kỳ, ánh xạ trạng thái → badge, quy tắc
export.
**File này KHÔNG giữ gì:** mô hình dữ liệu (→ `spec/danh-muc-dti/business-rules.md` §1 — file
chủ), route/shape (→ `doc/contracts/dashboard.md`), layout (→ `spec/dashboard-dti/ui-spec.md`),
luật import/export chung của Core (→ `doc/huong_dan/wiki-core/be/15-import-export.md`).

---

## 0. Ràng buộc trước mọi thứ khác — Dashboard là TRANG CHỦ

Dashboard **thay** `/trang-chu` (Q3). Route đó là bến an toàn của mọi fallback và **không có
role guard**. Nghĩa là màn này phải chịu được ba loại người dùng mà một màn nghiệp vụ bình
thường không bao giờ gặp:

| Ai đáp xuống đây | Phải thấy gì |
| --- | --- |
| Người vừa đăng nhập, hệ thống **chưa có dữ liệu DTI nào** | Trạng thái rỗng tử tế — không phải lỗi, không phải màn trắng |
| Người vừa import xong, **chưa ai nhập `Tiến độ %`** (Q24) | Trạng thái rỗng **thứ hai**, khác hẳn cái trên: có đủ chỉ tiêu ở bảng, nhưng KPI/thanh nhóm/biểu đồ chưa có gì để vẽ |
| Người bị điều hướng nhầm từ một route hỏng | Vẫn là màn này, vẫn hoạt động |

**"Chưa có dữ liệu" KHÔNG phải lỗi.** BE trả thành công với `groups`/`trend`/`table` rỗng,
không trả 404. Đây là ràng buộc hợp đồng, ghi ở `doc/contracts/dashboard.md` §0.

> **Ca "không có quyền xem" đã GỠ — Q21 (2026-09-05).** Mọi người đăng nhập đều xem được
> Dashboard, không cần quyền DTI. Không có `[RequirePermission]` trên đường đọc, không phải
> vẽ trạng thái "không có quyền" cho màn này. Quyền **ghi** của màn Danh mục là chuyện riêng
> và đã chốt riêng: `spec/danh-muc-dti/business-rules.md` §6.5.

---

## 1. Công thức tổng hợp

### 1.1 Đơn vị tính là `Tiến độ %`, gia quyền theo `MaxScore`

```
overallProgress = Σ( progressPercent_i × maxScore_i ) / Σ( maxScore_i )
```

- Gia quyền theo `MaxScore` chứ **không** lấy trung bình cộng: một chỉ tiêu 30 điểm không
  thể có cùng sức nặng với một chỉ tiêu 10 điểm. Trung bình cộng cho ra một con số trông
  hợp lý và sai một cách không ai kiểm được.
- Chỉ tiêu **không có** `progressPercent` trong kỳ ⇒ **loại khỏi cả tử số lẫn mẫu số**,
  không tính là 0. Tính là 0 làm một tuần chưa ai nhập liệu trông như một tuần tụt dốc.
- Mọi chỉ tiêu bị loại hết ⇒ `overallProgress` **vắng mặt** (không phải 0).
- Áp cùng công thức cho: `kpi.overallProgress`, `groups[].progress`, `trend[].value`, và
  `overallProgress` của từng kỳ trong `GET /api/dashboard/periods` (DB-3). **Một công thức,
  một chỗ cài** — bốn chỗ tính riêng sẽ lệch nhau.

> ### ⚠️ `Tiến độ %` và "% theo điểm" là HAI đại lượng khác nhau — sau Q24 thì không còn quan hệ nào
>
> Q24 (2026-09-05) chốt `Tiến độ %` để **trống** khi import và do người dùng **nhập tay**
> (`spec/danh-muc-dti/business-rules.md` §6.4). Không còn công thức nào nối nó với
> `Tự đánh giá` / `Điểm tối đa`. Ba hệ quả, cả ba đều dễ vấp:
>
> 1. Mọi con số tiến độ trong prototype và trong bảng tham chiếu (74,3% cho nhóm 1 · 82,1%
>    toàn bộ) là **% theo ĐIỂM** — `Σ Tự đánh giá / Σ Điểm tối đa`, đo từ file BA gửi.
> 2. Dashboard hiện **% theo `Tiến độ %`** (Q11). Hai con số **sẽ khác nhau**, và chênh bao
>    nhiêu thì **không đoán trước được** — vế sau phụ thuộc người nhập, không phụ thuộc dữ liệu.
> 3. Ngay sau import, vế sau **chưa có giá trị nào**: `overallProgress` và `groups[].progress`
>    **vắng mặt**, `trend` rỗng, trong khi `table` đã đủ 62 dòng.
>
> Đừng "sửa" khác biệt đó bằng cách đổi công thức, và **đừng dùng số theo điểm làm kỳ vọng cho
> test của Dashboard** — test sẽ đỏ trên dữ liệu đúng.
>
> *Bản trước của mục này so hai cách dưới **giả định A** (`progressPercent = round(selfScore /
> maxScore × 100)`) rồi kết luận chúng lệch 0,1 điểm phần trăm ở nhóm 1 vì `progressPercent`
> là số nguyên. Q24 đã bỏ giả định A, nên phép so đó mất căn cứ — ghi lại đây để người từng
> đọc con số 74,2% biết nó đi đâu.*

### 1.2 Gộp Tháng và "Tất cả trong năm"

- `mode=week` ⇒ tính trên đúng các bản ghi có `AssessmentDate` nằm trong tuần ISO đó.
- `mode=month` / `mode=year` ⇒ **trung bình cộng `overallProgress` của các kỳ-tuần CÓ dữ
  liệu** trong phạm vi, **không** carry-forward. Kỳ nào không có thao tác nào cho một chỉ
  tiêu thì chỉ tiêu đó bị loại khỏi mẫu tính của kỳ đó — chứ không kéo số tuần trước sang.

> Carry-forward nghe có vẻ "đầy đủ hơn", nhưng nó biến một tháng nghỉ thành một tháng tiến
> độ ổn định. Con số đó không sai về phép tính, nó chỉ trả lời một câu hỏi khác với câu
> người đọc đang hỏi.

### 1.3 So với kỳ trước (`delta`)

```
delta = overallProgress(kỳ hiện tại) − overallProgress(kỳ liền trước)
```

- Đơn vị là **điểm phần trăm** (`đ.%`), không phải phần trăm của phần trăm.
- "Kỳ liền trước" = kỳ liền kề **có dữ liệu**, không phải kỳ liền kề theo lịch. Không có kỳ
  nào trước ⇒ `delta` và `previousPeriodLabel` **vắng mặt**; FE hiện `—`, không hiện `0`.
- `previousPeriodLabel` ghi rõ khoảng ngày, đúng Q12 + T5 — vd
  `Tuần 32/2026 (03/08 – 09/08/2026)`. Khuôn chuỗi: §6.2.

### 1.4 Ba ô đếm chỉ tiêu

| Trường | Đếm gì |
| --- | --- |
| `kpi.up` | số chỉ tiêu có `progressPercent(kỳ này) − progressPercent(kỳ trước) > ε` |
| `kpi.flat` | số chỉ tiêu có `abs(Δ) ≤ ε` |
| `kpi.down` | số chỉ tiêu có `Δ < −ε` |
| `kpi.done` | số chỉ tiêu có `status = "Hoàn thành"` — **đếm theo trạng thái người dùng chọn**, không suy từ điểm |
| `kpi.totalCriteria` | số chỉ tiêu chưa xoá mềm trong kỳ |

- **`ε = 0.001`.** So `== 0` trên `decimal` sinh ra từ phép trừ là nguồn của ca "0 mà không
  bằng 0" — dùng epsilon ở mọi chỗ so sánh, kể cả `diff` (§`spec/danh-muc-dti/business-rules.md` §3.1).
- Chỉ tiêu thiếu dữ liệu ở **một trong hai** kỳ ⇒ không vào `up`/`flat`/`down` nào cả.
  Hệ quả bắt buộc phải biết: **`up + flat + down` có thể NHỎ HƠN `totalCriteria`** — đó là
  hành vi đúng, đừng "vá" cho ba số cộng lại bằng tổng.
- Ngưỡng "hoàn thành" của **màu sắc/hiển thị** dùng `progressPercent ≥ 100` với cùng epsilon;
  ngưỡng đó **không** đổi `status`, và `status` **không** đổi theo điểm (§2).

### 1.5 Biểu đồ xu hướng (`trend`)

| `mode` | `label` | Trục X hiển thị |
| --- | --- | --- |
| `week` | `"YYYY-Www"` | **khoảng ngày** — `06/07 – 12/07` (Q12) |
| `month`, `year` | `"Th.1" … "Th.12"` | `Th.1 … Th.12` |

- **Chỉ trả điểm CÓ dữ liệu.** Không nội suy, không chèn điểm `null` cho đủ 52 tuần. Chuỗi
  thưa vẽ ra đường đứt đoạn, và đó là sự thật; một đường liền do nội suy là số liệu bịa.
- Giá trị kẹp `[0, 100]`.
- **Trục X chế độ Tháng giữ `Th.1 … Th.12`, không đổi sang khoảng ngày — CHỐT 2026-09-05
  (T7), không còn để ngỏ.** Lý do: 12 nhãn khoảng-ngày không đủ chỗ trên trục. Chế độ tuần
  thì đã là khoảng ngày (Q12), và khoảng ngày của tháng vẫn đọc được ở thanh chọn kỳ + nhãn
  "Kỳ đang xem" — thông tin không mất, chỉ đổi chỗ.

### 1.6 Năm ô KPI NGAY SAU IMPORT — bảng chủ (T12, chốt 2026-09-06)

Đây là **file chủ** của câu hỏi *"vừa import xong thì ô KPI nào có số, ô nào chưa"*.
`Screens/01-dashboard.md` và `spec/dashboard-dti/ui-spec.md` trỏ về đây, không mô tả lại.

Bối cảnh: Q24 để `Tiến độ %` **trống** khi import, mà ô 1 và 2 lại suy từ chính trường đó —
nên trạng thái này là **trạng thái thường gặp nhất trong ngày đầu chạy thật**, không phải ca
hiếm.

| # | Ô | Trường | Ngay sau import | Vì sao |
| ---: | --- | --- | --- | --- |
| 1 | `Tiến độ chung` | `kpi.overallProgress` | **`—`** | Suy từ `Tiến độ %`, mà mọi chỉ tiêu đều bị loại khỏi mẫu tính (§1.1) ⇒ trường **vắng mặt** |
| 2 | `So với kỳ trước` | `kpi.delta` + `previousPeriodLabel` | **`—`** | Không có `overallProgress` của kỳ này thì không có hiệu số (§1.3) ⇒ cả hai **vắng mặt** |
| 3 | `Chỉ tiêu tăng` | `kpi.up` | **`0`** | Là số đếm, luôn có giá trị |
| 4 | `Không tăng` | `kpi.flat` | **`0`** | Là số đếm, luôn có giá trị |
| 5 | `Hoàn thành` | `kpi.done` | **số thật** — 26 trên dữ liệu BA gửi | Đếm theo **`status`**, mà `status` đến thẳng từ cột `Trạng thái` của file import |

**Hai chỗ dễ hiểu sai, cả hai đều đắt:**

> **a. Ô 4 hiện `0`, KHÔNG hiện `62`.** Trực giác nói "chưa ai nhập gì thì cả 62 chỉ tiêu đều
> không tăng". Luật §1.4 nói ngược: chỉ tiêu thiếu dữ liệu ở **một trong hai** kỳ thì không
> vào `up`/`flat`/`down` nào cả. Ngay sau import thì **cả hai** kỳ đều thiếu, nên cả ba số
> đều `0`. Đó là hành vi đúng — `flat` nghĩa là *"đã đo hai lần và không đổi"*, không phải
> *"chưa đo lần nào"*. Hệ quả kèm theo, đã ghi ở §1.4: `up + flat + down` **nhỏ hơn**
> `totalCriteria`, đừng "vá" cho ba số cộng lại bằng tổng.
>
> **b. `—` và `0` là HAI thứ khác nhau, và khác biệt đó phải sống sót tới tận màn hình.** Ô 1
> hiện `—` vì *chưa có dữ liệu*; ô 3 hiện `0` vì *đã tính và kết quả bằng không*. Nếu BE trả
> `0` cho ô 1 thay vì bỏ trường đi, dashboard sẽ tuyên bố "tiến độ toàn xã: 0%" ngay sau khi
> nạp một file có 26 chỉ tiêu đã hoàn thành — một câu sai, hiển thị tự tin, và không ai kiểm
> lại vì nó trông như một con số bình thường. Đây là lý do hợp đồng bắt các trường tổng hợp
> **vắng mặt** thay vì bằng `0` (`doc/contracts/dashboard.md` §0).

Ô thứ năm là ô **duy nhất** có số thật, và đó cũng là câu trả lời cho *"vậy màn hình có nói
được gì không"*: nó nói được **26/62 chỉ tiêu đã ở trạng thái Hoàn thành**, tức file đã nạp
thành công — đủ để người dùng tin rằng import chạy đúng, trong khi ba ô kia còn chờ họ nhập.

> `kpi.down` **không có ô KPI nào** trên màn hình (bộ 5 ô đã chốt ở Q22 + `KpiTile.md`). Nó
> vẫn nằm trong response vì `up`/`flat`/`down` là một bộ ba và cắt một phần tử ra khỏi phép
> đếm làm luật §1.4 khó đọc — không phải vì màn hình cần nó.

---

## 2. Trạng thái → Badge — bảng ánh xạ DUY NHẤT

Bộ 4 giá trị và luật "người dùng chọn tay, hệ thống không tự tính" là của
`spec/danh-muc-dti/business-rules.md` §4 — **file chủ**, không mô tả lại. Ở đây chỉ giữ ánh
xạ hiển thị, áp cho **cả hai** màn (Q10):

| `status` | Lớp badge |
| --- | --- |
| `Hoàn thành` | `.ok` |
| `Đang thực hiện` | `.warn` |
| `Cần bổ sung minh chứng` | `.bad` |
| `Chưa thực hiện` | `.neutral` |
| vắng mặt (kỳ chưa có đánh giá) | không render badge — hiện `—` |

> ⚠️ **Trường `badge` do BE tính đã BỎ.** Thiết kế cũ có bốn nhãn runtime
> (`Hoàn thành`/`Không tăng`/`Đang thực hiện`/`Chưa có dữ liệu`) mà **server** suy từ số
> liệu. Bộ đó không còn tồn tại ở bất kỳ tầng nào. Nếu thấy tên `badge` trong code hoặc
> trong một spec cũ, đó là di sản — không port lại.
>
> Hệ quả: **màu trên bảng chi tiết nay phản ánh ý chí người nhập liệu, không phản ánh số
> liệu.** Một chỉ tiêu đủ điểm vẫn có thể mang badge `.bad` nếu người dùng chọn
> `Cần bổ sung minh chứng` — và dữ liệu thật có đúng những dòng như vậy. Đó là hành vi đúng.

Chỉ báo chênh lệch (`.delta`) là chuyện **khác**, không liên quan tới `status`. Đây là **bảng
ánh xạ duy nhất** của nó — tô theo **dấu của `diff`**:

| Điều kiện | Lớp | Màu |
| --- | --- | --- |
| `diff > ε` | `.delta.up` | xanh |
| `abs(diff) ≤ ε` | `.delta.flat` | xám |
| `diff < −ε` | `.delta.down` | đỏ |

> ⚠️ **Bảng này KHÔNG đổi sau Q25 — nhưng nghĩa của nó thì đổi.** Ngày 2026-09-05 công thức
> `diff` đảo chiều thành `Thẩm định − Tự đánh giá`
> (`spec/danh-muc-dti/business-rules.md` §3.1). Ánh xạ dấu → màu giữ nguyên vì **đó chính là
> mục đích của việc đảo chiều**: trước đó một chỉ tiêu bị thẩm định bác trắng ra dấu **dương**
> và được tô **xanh**. Nếu thấy màu "sai" ở đâu đó, chỗ phải kiểm là công thức, **không phải**
> bảng này.

---

## 3. Bộ lọc của bảng chi tiết

Ba điều kiện: `search` (mã hoặc tên) · `groupId` · `status`.

**Luật phạm vi — chỗ dễ làm sai nhất của màn này:**

> Bộ lọc chỉ áp cho **`table`**. `kpi`, `groups`, `trend` LUÔN tính trên **toàn bộ** chỉ tiêu
> của kỳ.

Vì sao: nếu lọc một nhóm mà ô "Tiến độ chung" đổi theo, người đọc sẽ tưởng tiến độ toàn xã
vừa thay đổi. Con số đúng về phép tính, sai về câu hỏi nó đang trả lời — và không có cách
nào để người đọc biết mình đang bị lừa.

`status` nhận giá trị ngoài 4 giá trị hợp lệ ⇒ **`400`**, không âm thầm bỏ lọc và không trả
danh sách rỗng. Bỏ lọc thì một lỗi chính tả trả về **toàn bộ** danh sách; trả rỗng thì người
dùng đọc được một câu trả lời SAI mà tưởng là đúng ("không chỉ tiêu nào Hoàn thành") rồi hành
động theo. Cùng luật đã áp cho tham số `Role` của `GET /api/users` ở Core.

---

## 4. Export `.xlsx`

Luật chung của Core (hai đường theo ngưỡng số dòng, `ITabularWriter` + `IExportDefinition<T>`,
"export dùng CHUNG object bộ lọc với endpoint danh sách"):
`doc/huong_dan/wiki-core/be/15-import-export.md` §3–§4. **Không lặp lại ở đây.**

### 4.1 Phạm vi

| Luật | Giá trị |
| --- | --- |
| Định dạng | **chỉ `.xlsx`**. Không CSV, không `.xls` (Q13) |
| Chế độ | `week` · `month`. `year` **không hỗ trợ** ⇒ `400` |
| `mode=month` | xuất **trọn tháng**, không phải một tuần trong tháng (Q15). **Q37 KHÔNG chạm tới dòng này** — xem ghi chú dưới bảng |
| Đường đi | **đồng bộ**, stream thẳng. 62 dòng nằm rất xa ngưỡng cần job nền |
| Hành vi UI | tải thẳng file, **không dialog, không xem trước** (Q13) |

> **Q37 (chỉ nhập theo tuần) KHÔNG thu hẹp export.** Quyết định đó nói về đường **ghi**:
> không nhập liệu vào một kỳ tháng vì mô hình lưu theo ngày không neo được một tháng vào đúng
> một tuần ISO (`spec/danh-muc-dti/business-rules.md` §5.1). Xuất một tháng là phép **tổng
> hợp trên dữ liệu đã có**, không tạo bản ghi nào, nên không dính lỗi đó. `mode=month` giữ
> nguyên, kể cả khối `Gồm các tuần` ở §4.2.

Vì chỉ xuất `.xlsx`, **hai cái bẫy CSV + tiếng Việt của Core (`15-import-export.md` §5 — BOM
UTF-8 và dấu phân cách theo locale `vi-VN`) KHÔNG áp cho đường này.** Ghi ra để không ai đi
tìm chỗ ghi BOM trong writer XLSX. Chúng sẽ áp lại ngay khi có ai đó thêm `format=csv`.

### 4.2 Bố cục — đúng 1 sheet (Q14)

Chốt theo hai file mẫu người dùng đã duyệt: `doc/Design/Frontend/PlatformManager/Prototypes/mau-xuat-bao-cao_Tuan-33-2026.xlsx` và
`doc/Design/Frontend/PlatformManager/Prototypes/mau-xuat-bao-cao_Thang-8-2026.xlsx` (mở bằng cách giải nén — chúng là zip OOXML).

> **🔄 SỬA 2026-09-10 — hai file này nay CÓ trong repo, và số liệu bên trong đã đổi.** Bản cũ
> nằm ở `Prototype/` (bị `.gitignore` loại khỏi repo), nên người thứ hai clone về không mở được
> thứ mà mục này viện dẫn. Bản trong repo giữ nguyên **bố cục** — thứ mục này thật sự chốt: tên
> sheet động, khối nhận dạng kỳ 10 dòng, 12 cột và bề rộng, nền header `#0F5BD7`, dòng
> `TỔNG CỘNG`, cột `Tiến độ %` rỗng ở cả 62 dòng (Q24). **Số liệu thì thay bằng bộ mẫu ẩn danh**
> nên đừng dùng chúng làm mốc đối chiếu giá trị.

**Tên sheet:** `Tuần 33-2026` / `Tháng 8-2026` (dấu **gạch ngang**, không phải `/` — Excel
cấm `/ \ ? * [ ]` trong tên sheet).

| Dòng | Cột A | Cột B |
| ---: | --- | --- |
| 1 | `BÁO CÁO TIẾN ĐỘ CHUYỂN ĐỔI SỐ (DTI) — XÃ CẦN GIUỘC` (đậm, cỡ 14) | |
| 2 | `Kỳ báo cáo` | `Tuần 33/2026` · `Tháng 8/2026` |
| 3 | `Từ ngày` | `10/08/2026` · `01/08/2026` |
| 4 | `Đến ngày` | `16/08/2026` · `31/08/2026` |
| 5 | `Thuộc tháng` *(file tuần)* / `Gồm các tuần` *(file tháng)* | `Tháng 8/2026` · `Tuần 31 (27/07–02/08) · Tuần 32 (…) · …` |
| 6 | `Năm` | `2026` |
| 7 | `Số chỉ tiêu` | `62` |
| 8 | `Ngày xuất` | `05/09/2026 14:30` |
| 9 | `Người xuất` | tên đầy đủ người đang đăng nhập |
| 10 | `Bộ lọc đang áp` | `Nhóm: 1. Hạ tầng và Nền tảng số · Trạng thái: Cần bổ sung minh chứng` — hoặc `Không lọc — đủ 62 chỉ tiêu` |
| 11 | *(trống)* | |
| 12 | **header 12 cột** | |
| 13 → 13+N−1 | N dòng dữ liệu | |
| *(trống)* | | |
| cuối | `TỔNG CỘNG` | |

**Nhãn dòng 5 đổi theo chế độ, đó là chủ đích:** file tuần cần biết tuần đó thuộc tháng nào;
file tháng cần biết nó gộp những tuần nào.

⚠️ **`Gồm các tuần` liệt kê mọi tuần ISO GIAO với tháng, không phải mọi tuần nằm gọn trong
tháng.** File mẫu tháng 8/2026 liệt `Tuần 31 (27/07–02/08)` — bắt đầu từ tháng 7 — và
`Tuần 36 (31/08–06/09)` — kết thúc sang tháng 9. Đây là hệ quả trực tiếp của việc tuần ISO
không nằm gọn trong tháng (`spec/danh-muc-dti/business-rules.md` §5.1). Lấy "tuần nằm gọn
trong tháng" sẽ bỏ sót dữ liệu của hai tuần đầu-cuối.

### 4.3 12 cột — export là SUPERSET của import

Đúng thứ tự này, `Tiến độ %` chèn ở vị trí **11**, ngay **trước** `Minh chứng/Ghi chú`:

| # | Header | Nguồn | Ô trống thì ghi |
| ---: | --- | --- | --- |
| 1 | `Mã` | `Criteria.Code` | |
| 2 | `Chỉ tiêu` | `Criteria.Name` | |
| 3 | `Nhóm` | `CriteriaGroup.Name` | |
| 4 | `Điểm tối đa` | `MaxScore` | |
| 5 | `Tự đánh giá` | `SelfScore` | `—` |
| 6 | `Thẩm định` | `VerifiedScore` | `—` |
| 7 | `Chênh lệch (Thẩm định − Tự đánh giá)` | **tính** `= VerifiedScore − SelfScore` | `—` |
| 8 | `Trạng thái` | `Status` | `—` |
| 9 | `Phụ trách` | `OwnerName` | `—` |
| 10 | `Hạn xử lý` | `Deadline` | `—` |
| 11 | `Tiến độ %` | `ProgressPercent` | `—` |
| 12 | `Minh chứng/Ghi chú` | `Note` | `—` |

**11 cột đầu ánh xạ 1-1 với 11 cột của file import** (`spec/danh-muc-dti/business-rules.md`
§6.2), và `Minh chứng/Ghi chú` giữ nguyên tên. Vì vậy file tải về **nạp ngược lại được** —
người dùng sửa trong Excel rồi import lên, không mất trường nào.

> ### ⚠️ Cột 7 là ngoại lệ DUY NHẤT của quy tắc "trùng tên với file import" — và nó an toàn
>
> Tiêu đề ghi rõ công thức (`Chênh lệch (Thẩm định − Tự đánh giá)`) thay vì `Chênh lệch` trần,
> theo **Q25 (2026-09-05)**. Lý do: cột `Chênh lệch` trong file gốc BA gửi tính theo chiều
> **cũ** và vì thế **ngược dấu** với cột này ở 27/62 dòng. Ai đặt hai file cạnh nhau mà không
> có tiêu đề nói rõ chiều tính sẽ kết luận một trong hai file bị lỗi.
>
> Đổi tiêu đề **không phá round-trip**, vì cột `Chênh lệch` là trường **tính**: đường import
> **bỏ qua** nó hoàn toàn, không đọc, không đối chiếu, và **không đòi nó phải có mặt**
> (`spec/danh-muc-dti/business-rules.md` §6.2 cột 7 — nó không nằm trong nhóm cột bắt buộc).
> Mười cột còn lại giữ **đúng nguyên văn** tên của file import; đổi tên bất kỳ cột nào trong
> số đó **mới** là phá hợp đồng, vì import khớp header theo **tên**, không theo vị trí.

> Đây là **ràng buộc thiết kế**, không phải trùng hợp. Hai bộ cột lệch nhau thì vòng
> export → sửa → import âm thầm xoá trắng mọi trường mà export quên xuất. Thêm cột vào import
> mà không thêm vào export là đã phá hợp đồng này.

**Ô trống ghi `—`, không để rỗng.** Ba lý do: người đọc phân biệt được "chưa có dữ liệu" với
"file lỗi"; cột không bị Excel co lại; và đường import bỏ qua `—` như bỏ qua ô trống (luật ở
`spec/danh-muc-dti/business-rules.md` §6.3 — phải cài đúng thế, nếu không round-trip sẽ ghi
chữ `—` vào DB).

### 4.4 Dòng `TỔNG CỘNG`

| Cột | Nội dung |
| --- | --- |
| A | chữ `TỔNG CỘNG` (đậm) |
| D `Điểm tối đa` | tổng — dữ liệu thật: `960` |
| E `Tự đánh giá` | tổng — `787,84` |
| F `Thẩm định` | tổng — `606,27` |
| G `Chênh lệch (Thẩm định − Tự đánh giá)` | tổng — **`−181,57`** (`= 606,27 − 787,84`) |
| các cột còn lại | để trống |

**Cột `Tiến độ %` KHÔNG có dòng tổng** — cộng 62 giá trị phần trăm lại cho ra một con số vô
nghĩa (`Σ%` không phải `%`). Nếu cần "tiến độ chung", nó nằm ở khối nhận dạng kỳ, không nằm ở
dòng tổng.

### 4.5 Định dạng ô

- Header (**dòng 12** — dịch xuống 1 khi dòng `Bộ lọc đang áp` được thêm ngày 2026-09-06; đừng
  ghim số dòng vào code, đọc nó từ bảng bố cục ở §4.2): chữ **đậm, trắng**, nền `#0F5BD7`,
  canh giữa theo chiều dọc, bật xuống dòng.
- Dòng dữ liệu: canh **trên**, bật xuống dòng (cột `Chỉ tiêu` và `Minh chứng/Ghi chú` dài, và
  một ô ghi chú thật có thể trải nhiều dòng).
- Số ghi ra dạng **số**, không phải chuỗi — người nhận còn `SUM` được. Dấu thập phân do Excel
  hiển thị theo locale máy người dùng.
- Ngày ghi `dd/MM/yyyy`; `Ngày xuất` ghi `dd/MM/yyyy HH:mm`.
- Không đóng băng ô, không auto-filter, không gộp ô — hai file mẫu đã duyệt đều không có.

### 4.6 Tôn trọng bộ lọc đang áp — ĐÃ CHỐT (Q23, 2026-09-05)

Export dùng **cùng một object bộ lọc** với endpoint danh sách ⇒ đang lọc nhóm nào thì xuất
nhóm đó. Khớp luật Core `15-import-export.md` §4. Không còn là mục cần chốt.

Hai hệ quả bắt buộc:

- Khối nhận dạng kỳ, dòng `Số chỉ tiêu`, ghi số dòng **thực xuất** — không ghi cứng 62.
- **Dòng `Bộ lọc đang áp` (dòng 10) là bắt buộc, kể cả khi không lọc** — chốt 2026-09-06.
  Lý do nó nằm trong FILE chứ không chỉ trên màn hình: **file sống lâu hơn màn hình.** Người
  mở file tuần sau, hoặc người nhận nó qua email, không có cách nào biết nó được xuất lúc
  đang lọc gì; một tooltip không đi theo file. Khi không lọc thì vẫn ghi một dòng nói rõ
  `Không lọc — đủ 62 chỉ tiêu`, vì **dòng vắng mặt** không phân biệt được với "quên ghi".
- **Test nghiệm thu, khai đúng MỘT chỗ và đây là chỗ đó:** số dòng dữ liệu trong file **=** số
  phần tử `data.table` của `GET /api/dashboard` với **đúng** bộ tham số đó, chạy với ít nhất
  một bộ lọc khác mặc định. Một integration test, không thương lượng. Đây là thứ **duy nhất**
  giữ hai đường lọc không lệch nhau; thiếu nó thì triệu chứng lúc hỏng là "file tải về không
  khớp màn hình", và không ai biết bên nào đúng.

⚠️ **Rủi ro UX đã biết, không phải để bỏ qua:** nút "Xuất báo cáo" nằm ở thanh chọn kỳ **trên
cùng**, còn ba ô lọc nằm ở thanh công cụ của bảng **gần cuối trang**. Người dùng bấm Xuất sau
khi đã cuộn lên đầu có thể không nhớ mình còn đang lọc một nhóm. Cách nhắc (hiện bộ lọc đang
áp cạnh nút, hoặc trong tên file) thuộc `spec/dashboard-dti/ui-spec.md`.

---

## 5. Ranh giới Core ↔ Business cho màn này

| Core giữ | `Business.*` giữ |
| --- | --- |
| Đọc/ghi định dạng file (`IImportFileReader`, `ITabularWriter`, `IExportDefinition<T>`) | Query tổng hợp, **lọc theo gì**, cột nào xuất, format từng ô |
| Chạy job nền, lưu file, trần số dòng, dọn file hết hạn | Ý nghĩa nghiệp vụ của kỳ báo cáo |
| Quy đổi "tuần ISO thứ N của năm Y → khoảng ngày" (**thời gian thuần**) | Lọc bản ghi theo `AssessmentDate` trong khoảng đó (**nghiệp vụ**) |

Ranh giới cuối cùng là chỗ dễ trượt nhất: hai vế trông giống nhau. Phép thử —
*"sản phẩm thứ hai dựng trên nền tảng này có dùng câu này không?"* Câu đầu: có (lịch ISO là
lịch chung). Câu sau: không (`AssessmentDate` là bảng của DTI).

Nếu Core biết *"tuần 35/2026 nghĩa là gì với bảng đánh giá"*, Core đã lấn nghiệp vụ.

---

## 6. Quy tắc kỳ và NHÃN KỲ — file chủ của định dạng

Mục này là **file chủ** của *cách viết một kỳ ra chữ*. Nhiều spec đã trỏ về đây
(`Screens/01-dashboard.md`, `Screens/02-danh-muc-dti.md`, cả hai `ui-spec.md`); trước
2026-09-05 chúng trỏ vào một mục **chưa tồn tại**.

Ranh giới với file chủ kia, để không thành hai nguồn:

| Câu hỏi | File chủ |
| --- | --- |
| "Tuần ISO 33/2026 là từ ngày nào tới ngày nào", "kỳ suy từ trường nào", "đọc kỳ lấy bản ghi nào" | `spec/danh-muc-dti/business-rules.md` §5.1–§5.2 |
| **"Viết kỳ đó ra chữ thế nào"** | **mục này** |
| Chuỗi copy verbatim của một màn cụ thể | screen spec tương ứng ở `doc/Design/Frontend/PlatformManager/Screens/` |

### 6.1 Luật chung — áp cho MỌI chỗ hiển thị kỳ

1. **Luôn ghi rõ từ ngày đến ngày** (Q12). Không có ngoại lệ nào cho "chỗ chật" — chỗ chật
   thì rút gọn phần năm, không rút gọn khoảng ngày.
2. **Dấu gạch giữa hai mốc ngày có khoảng trắng hai bên**: ` – ` (en dash), **không** viết
   dính `10/08–16/08`. Chốt T5 (2026-09-05); prototype tự mâu thuẫn chỗ có chỗ không, và bản
   có khoảng trắng là bản dùng ở nhiều chỗ hơn, dễ đọc hơn ở cỡ chữ nhỏ.
3. **Ngày dạng `dd/MM`**, năm chỉ viết **một lần** và đặt ở **mốc cuối**.
4. Tuần dùng **số tuần ISO**; tháng dùng **số tháng dương lịch**. Không bao giờ hiện số tuần
   mà không kèm khoảng ngày — số tuần ISO là thứ gần như không ai nhẩm ra được.
5. **Ngoại lệ có chủ đích: nội dung bên trong file `.xlsx` xuất ra KHÔNG theo mục này**, nó
   theo hai file mẫu người dùng đã duyệt (§4.2 — vd `Tuần 31 (27/07–02/08)` viết dính). Luật
   §6.1 là luật **giao diện**; ô trong file Excel là bố cục đã chốt ở Q14, sửa nó cần duyệt
   lại file mẫu chứ không phải áp một quy ước của màn hình lên.

### 6.2 Các khuôn chuỗi

| Dùng ở | Khuôn | Ví dụ |
| --- | --- | --- |
| Nhãn kỳ đang xem | `Tuần {w}/{yyyy} ({dd/MM} – {dd/MM/yyyy})` | `Tuần 33/2026 (10/08 – 16/08/2026)` |
| | `Tháng {M}/{yyyy} ({dd/MM} – {dd/MM/yyyy})` | `Tháng 8/2026 (01/08 – 31/08/2026)` |
| | `Năm {yyyy}` | `Năm 2026` |
| Option trong ô chọn kỳ | `Tuần {w} · {dd/MM} – {dd/MM} · {tiến độ}` | `Tuần 33 · 10/08 – 16/08 · 82,1%` |
| Dòng lịch sử kỳ | `{dd/MM} – {dd/MM/yyyy}` | `10/08 – 16/08/2026` |
| Trục X biểu đồ, chế độ tuần | `{dd/MM} – {dd/MM}` | `06/07 – 12/07` |
| Trục X biểu đồ, chế độ tháng/năm | `Th.{M}` | `Th.1 … Th.12` (T7 — §1.5) |

> ### ⚠️ Ngoại lệ ĐÃ ĐĂNG KÝ của §6.1 mục 1 — cột `Kỳ của số liệu` (Q38, 2026-09-06)
>
> Cột `Kỳ của số liệu` của lưới Danh mục hiện **chỉ khoảng ngày**: `10/08–16/08`. Không có
> tên kỳ, không có năm.
>
> Đây là ngoại lệ **có chủ đích**, không phải chỗ quên áp luật, và có đúng hai lý do: cột rộng
> 110px không chứa nổi `Tuần 33/2026 (10/08 – 16/08/2026)`; và sau Q37 thì **mọi dòng đều là
> tuần**, nên tên kỳ không còn phân biệt được gì — nó lặp lại chữ "Tuần" 62 lần để nói một
> điều mà tiêu đề cột đã nói.
>
> Ghi vào đây thay vì chỉ ghi ở screen spec vì §6.1 là luật, và một luật có ngoại lệ không
> đăng ký thì lần rà soát sau sẽ có người "sửa cho nhất quán". Chuỗi verbatim và bề rộng cột:
> `doc/Design/Frontend/PlatformManager/Screens/02-danh-muc-dti.md` § Normalize.
>
> API **không** đổi theo ngoại lệ này: vẫn trả cả `assessmentPeriod` lẫn
> `assessmentPeriodLabel` (`doc/contracts/danh-muc-dti.md` DM-2). Cột hẹp chọn hiển thị phần
> nào là việc của màn hình, không phải của hợp đồng.

> **Một khác biệt còn tồn tại giữa hai màn, và nó là COPY chứ không phải luật:** option chọn
> kỳ của Dashboard ngăn ba đoạn bằng `·`, còn ô lọc `Kỳ trong năm` của màn Danh mục dùng dấu
> hai chấm (`Tuần 33: 10/08 – 16/08/2026`) vì nó chỉ có hai đoạn. Cả hai đều tuân §6.1. Nếu
> muốn hợp nhất ký hiệu ngăn đoạn thì đó là quyết định copy — chủ là screen spec, và
> `Screens/02-danh-muc-dti.md` § Cần chốt đã ghi nhận khác biệt này.

### 6.2b Luật LƯỢC BỎ — được phép bỏ gì khỏi nhãn kỳ (chốt 2026-09-06)

Cùng một tuần đang được viết **bốn** kiểu trong sản phẩm, và cả bốn đều đúng:

| Chỗ | Chuỗi | Lược bỏ gì | Vì sao được phép |
| --- | --- | --- | --- |
| Nhãn kỳ đang xem | `Tuần 33/2026 (10/08 – 16/08/2026)` | không lược gì | đứng một mình, không có ngữ cảnh nào đỡ |
| Option chọn kỳ | `Tuần 33 · 10/08 – 16/08 · 82,1%` | năm | ô chọn **năm** nằm ngay bên trái |
| Dòng lịch sử kỳ | `10/08 – 16/08/2026` | số tuần | cả bảng đều là kỳ, số tuần không phân biệt gì thêm |
| Cột `Kỳ của số liệu` | `10/08 – 16/08` | số tuần **và** năm | sau Q37 mọi dòng đều là tuần; năm nằm ở bộ lọc `Năm đánh giá` |

**Luật:** một nhãn kỳ được phép lược bỏ **đúng những thành phần mà ngữ cảnh trực tiếp
quanh nó đã nêu** — ô lọc kề bên, tiêu đề bảng, hoặc tính đồng nhất của mọi dòng trong
cùng bảng. Không được lược thứ gì mà người đọc phải rời mắt khỏi vùng đó mới tìm ra.

**Vì sao luật này cần tồn tại:** trước 2026-09-06 mỗi kiểu ra đời một lần, mỗi lần một
người quyết, và không ai giữ luật chung — nên kiểu thứ năm chắc chắn sẽ được tự chế ở
lần thêm chỗ hiển thị kỳ tiếp theo. Hai thành phần **không bao giờ** được lược: khoảng
ngày (nó là thứ người dùng thật sự đọc) và khoảng trắng quanh dấu gạch (§6.1, T5).

### 6.3 Ai dựng chuỗi — BE hay FE

| Chỗ | Ai dựng | Vì sao |
| --- | --- | --- |
| `periodLabel` / `previousPeriodLabel` của `GET /api/dashboard` | **BE** | Đã là trường của card DB-1 |
| Nhãn kỳ của **từng dòng lưới** (cột `Kỳ của số liệu`, Q31) | **BE** | Cùng lý do dưới đây |
| Option trong ô chọn kỳ | **FE**, từ `weeksInYear`/`monthsInYear` của DB-3 | DB-3 đã trả `value` + `date`, đủ dựng (ghi chú cuối DB-3) |

> **Vì sao BE dựng nhãn kỳ của từng dòng thay vì trả mã `"2026-W33"` cho FE tự ghép:** để
> ghép được, FE phải quy `"2026-W33"` về khoảng ngày, tức phải có **lịch ISO** của riêng nó.
> Đó đúng là phép tính mà `spec/danh-muc-dti/business-rules.md` §5.1 cảnh báo không được tự
> làm bằng `ngày / 7`, và đúng thứ §5 ở trên xếp vào Core (**thời gian thuần**). Dựng ở BE
> giữ lịch ISO ở **một** chỗ.

---

## 7. Cần chốt — KHÔNG tự quyết

Vòng chốt 2026-09-05 đóng **hết** các mục từng nằm ở đây. Giữ lại bảng đóng mục để người
từng đọc bản trước không mang theo giả định cũ:

| Mục cũ | Đóng bằng | Nay ghi ở |
| --- | --- | --- |
| Ai được xem Dashboard | **Q21** — mọi người đăng nhập, không cần quyền DTI | §0 |
| Export có tôn trọng bộ lọc không | **Q23** — CÓ | §4.6 |
| Bộ lọc "Mức thay đổi" + sắp xếp "Tăng nhiều nhất" | **Q22** — bỏ cả ô lọc lẫn 2 tuỳ chọn sắp xếp; **không** thêm `deltaBucket`/`sort` vào DB-1 | §3 (bộ lọc còn `search`/`groupId`/`status`) |
| Trục X biểu đồ chế độ Tháng | **T7** — giữ `Th.1 … Th.12` | §1.5, §6.2 |
| `Tiến độ %` khởi tạo khi import | **Q24** — để trống | `spec/danh-muc-dti/business-rules.md` §6.4; hệ quả ở §1.1 |
| Tên thư mục `spec/` | đã xong 2026-09-05 | ⚠️ cổng KHÔNG bắt được loại tham chiếu này — `check-docs.sh` §4 chỉ quét tiền tố `src/` và `doc/`, không quét `spec/` |
| Ô KPI nào có giá trị ngay sau import | **T12** (2026-09-06) — ô 1, 2 hiện `—`; ô 3, 4 hiện `0`; ô 5 có số thật | §1.6, file chủ |
| Nhãn kỳ của cột `Kỳ của số liệu` | **Q38** (2026-09-06) — chỉ khoảng ngày | §6.2 |

**Không còn mục nào để ngỏ trong cả cụm DTI.** Hai câu hỏi từng mở ở
`spec/danh-muc-dti/business-rules.md` §8 đã đóng ngày 2026-09-06 bằng **Q37** (chỉ nhập theo
tuần) và **Q35** (nhật ký job nền ghi đúng người). Cả hai **không** đổi gì ở màn này:
`mode=week` vẫn lọc theo `AssessmentDate` — Q37 chọn phương án **không** dựng cột kỳ riêng,
nên câu hỏi "lọc theo cột nào" tự tan.

Điều còn lại cần theo dõi ở đây là **hai hạng mục Core** làm điều kiện tiên quyết cho đường
ghi (`spec/danh-muc-dti/business-rules.md` §5.6 và §6.5). Dashboard **không** phụ thuộc chúng
— nó 100% đọc — nên chúng không chặn việc thi công màn này.

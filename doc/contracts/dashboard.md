---
kind: luat
scope: du-an
verified: 2026-09-06
---

# API Contract — Dashboard DTI (`modules/dashboard`)

> ## 🚧 ĐÃ CHỐT — ĐANG THI CÔNG (BE vòng 1 thi công 2026-09-10)
>
> Câu *"không có dòng code nào của tính năng này tồn tại"* (2026-09-05) **hết đúng từ
> 2026-09-10** cho phía BE:
>
> | Card | BE hôm nay |
> | --- | --- |
> | DB-1 `GET /api/dashboard` | ✅ có endpoint thật — cả 3 `mode`, `trend` theo Q43/Q44/Q54/Q57, ba mã lỗi Q61/Q63 |
> | DB-3 `GET /api/dashboard/periods` | ✅ có endpoint thật (dùng chung với DM-8) |
> | DB-4 `GET /api/dashboard/export` | ✅ `IMPLEMENTED` 2026-09-11 — gọi thật, xem § Nghiệm thu của card |
> | DB-2 | đã gỡ khỏi phạm vi từ 2026-09-05, không dựng lại |
>
> 🔴 **DB-1 và DB-3 vẫn `AGREED`, CHƯA `IMPLEMENTED`:** mục 1 của §3 đòi gọi thật ở cả 3 `mode`
> trên DB có dữ liệu rồi dán shape response THẬT. Lượt 2026-09-10 không làm được bước đó —
> schema `business` chưa áp lên database nào và Docker không chạy trên máy thi công. Lý do đầy
> đủ: [`danh-muc-dti.md`](danh-muc-dti.md) §banner.
>
> **Kiến trúc: `PlatformManager.Business.*`** — đã dựng đủ 5 project; không dựng lại
> `Modules.DtiWeekly.*`. 📖 [`../kien-truc-core-module.md`](../kien-truc-core-module.md)
>
> **Casing đã CHỐT** (camelCase xuyên suốt, `null` ⇒ khoá vắng mặt, khoá `fields` giữ
> PascalCase): file chủ là [`danh-muc-dti.md`](danh-muc-dti.md) §0 — **không mô tả lại ở
> đây**. Cảnh báo *"CHƯA XÁC NHẬN với backend-expert"* của bản trước đã hết hiệu lực.
>
> **Luật nghiệp vụ** (công thức tổng hợp, quy tắc kỳ, quy tắc export): file chủ là
> `spec/dashboard-dti/business-rules.md`.

**Ngày viết lại: 2026-09-05.** Dashboard **100% đọc — không có hành động ghi nào**, kể cả nút
"Xuất báo cáo" (nó chỉ tải file về, không tạo dữ liệu trên server).

---

## 0. Ràng buộc lớn nhất của màn này — Dashboard chiếm ĐÚNG URL `/trang-chu`

**Q18 (chốt 2026-09-05):** Dashboard **thay màn chào cũ tại chỗ**, giữ nguyên URL
`/trang-chu`. `''` và `**` vẫn đổ về đó, `APP_CORE_ROUTES.home` **không đổi**, hàng menu
seed **không đổi**. Đây là điểm khác bản sáng nay, vốn viết mơ hồ là "thay `/trang-chu`" —
có thể đọc thành "đổi URL".

Route đó là **bến an toàn của mọi fallback**. Guard giữ nguyên `authGuard` +
`mustChangePasswordGuard`, **không** role guard.

**Q21 (chốt 2026-09-05): mọi người đăng nhập đều xem được Dashboard, không cần quyền DTI.**
Câu hỏi "ai được xem" **không còn để ngỏ**. Hợp đồng vì thế đơn giản hơn hẳn:

| Ca | BE phải trả gì | KHÔNG được làm gì |
| --- | --- | --- |
| Chưa có dữ liệu nào trong năm đang xem | `IApiResult` **thành công**; `groups`/`table` là **mảng rỗng**; `trend` có **đủ các kỳ** nhưng **không kỳ nào mang `value`** (Q44 — DB-1 luật 2); các trường số của `kpi` vắng mặt | Không trả 404. "Chưa có dữ liệu" không phải lỗi |
| **Đã có chỉ tiêu nhưng chưa ai nhập `Tiến độ %`** (Q24 — trạng thái ngay sau import) | `table` **có đủ dòng**, `kpi.overallProgress` và `groups[].progress` **vắng mặt**, `trend` **đủ các kỳ, không kỳ nào mang `value`** | Không trả `0` thay cho "chưa có" — xem cảnh báo dưới |

> 🔄 **LẬT 2026-09-10 (Q44).** Hai ô `trend` ở bảng trên trước đây ghi *"mảng rỗng"* /
> *"rỗng"*. Sau Q44, `trend` luôn mang đủ các kỳ của phạm vi — "chưa có dữ liệu" nay là
> *không phần tử nào có `value`*, không phải *mảng rỗng*.
| Người gọi chưa đăng nhập | `401` qua `[Authorize]` fail-closed | — |

> ⚠️ **Không còn ca "thiếu quyền xem".** Bản sáng nay khai một ca `403` cho người thiếu
> quyền DTI và một mục "cần chốt" kèm theo. Q21 gỡ cả hai. FE **không** phải vẽ trạng thái
> "không có quyền" cho màn này.
>
> Quyền **ghi** là chuyện riêng của màn Danh mục, và nay **cũng đã chốt** (Q27, 2026-09-05):
> đúng **một** permission-key cho toàn bộ đường ghi DTI, áp cho **mọi kỳ** —
> `spec/danh-muc-dti/business-rules.md` §6.5. Dashboard không chạm tới nó vì không có đường
> ghi nào.

⚠️ **Ca thứ hai là ca thường gặp nhất trong ngày đầu chạy thật**, không phải ca hiếm: Q24
chốt `Tiến độ %` **để trống khi import**, mà thanh tiến độ nhóm và biểu đồ lại vẽ theo
chính trường đó (Q11). Nên ngay sau khi nạp file 62 chỉ tiêu, dashboard **trống** cho tới
khi có người nhập tay. Đây là hành vi **người dùng đã chấp nhận**, không phải lỗi — nhưng
nó phải phân biệt được với "0%" (đã nhập và thật sự bằng 0). Đó là lý do các trường tổng
hợp **vắng mặt** thay vì bằng `0`.

> **Ô KPI nào có số, ô nào hiện `—` ở đúng thời điểm đó** — chốt T12 (2026-09-06), khai
> **một chỗ**: `spec/dashboard-dti/business-rules.md` §1.6. Tóm tắt để không ai phải mở file
> mới biết có mà đọc: `overallProgress` và `delta` **vắng mặt**; `up`/`flat`/`down` là `0`
> (**không** phải 62 — xem lý do ở file chủ); `done` có **số thật** vì nó đếm theo `status`,
> mà `status` đến thẳng từ cột `Trạng thái` của file import.

Ràng buộc phía FE (ba trạng thái rỗng phải hiển thị tử tế) thuộc
`spec/dashboard-dti/ui-spec.md` và
📖 [`../huong_dan/quy-uoc/fe-routing-guard.md`](../huong_dan/quy-uoc/fe-routing-guard.md).

> **Q32 (2026-09-05) — ca thứ hai có lối đi ra, và nó chạm hợp đồng ở đúng một điểm.** FE hiện
> một dải `NoticeBanner` kèm nút dẫn sang màn Danh mục ở địa chỉ **`/danh-muc/dti`** (Q33), vì
> nhập `Tiến độ %` là việc duy nhất làm được lúc đó. Copy và bố cục dải băng thuộc
> `Screens/01-dashboard.md` + `spec/dashboard-dti/ui-spec.md` — card này chỉ chốt rằng BE
> **không** cần trường mới nào cho việc đó.

> ### 🔄 LẬT 2026-09-10 (Q69) — proxy nhận dạng ca Q32 đổi từ `table` sang `kpi`
>
> Bản trước chốt: *"FE phân biệt được ca này bằng `table` có phần tử **và**
> `kpi.overallProgress` vắng mặt"*. **Sai ở vế đầu**, và sai theo đúng luật 1 của chính card
> này: `search`/`groupId`/`status` **chỉ áp cho `table`**, còn `kpi` luôn tính trên toàn bộ chỉ
> tiêu của kỳ. Nên lọc một nhóm không có dòng nào ⇒ `table` rỗng trong khi kỳ vẫn đủ chỉ tiêu,
> và dải băng Q32 biến mất đúng lúc nó cần hiện.
>
> **Chốt: hai vế đều đọc từ `kpi`** — cùng một khối, cùng phạm vi tính, không bị bộ lọc động tới:
>
> ```
> ca Q32  ⟺  kpi.totalCriteria > 0  VÀ  kpi.overallProgress vắng mặt
> ```
>
> BE **vẫn không cần trường mới nào** — `totalCriteria` đã có sẵn trong `kpi`, đó là lý do vế kết
> luận của Q32 giữ nguyên. Bảng điều kiện ba ca (`spec/dashboard-dti/ui-spec.md` §5.3.1) là file
> chủ của **luật**; dòng trên là ánh xạ luật đó sang **trường trên dây**, thuộc card này.
>
> **Nghiệm thu:** ngay sau import, lọc `groupId` về một nhóm **không có chỉ tiêu nào khớp** ⇒
> `table` rỗng nhưng dải băng Q32 **vẫn hiện**. Dùng proxy cũ thì băng biến mất — đó là ca bắt lỗi.

---

## CONTRACT DB-1 — Tổng hợp Dashboard theo Tuần / Tháng / "Tất cả trong năm"

- **Status: AGREED** (2026-09-05) — **đổi shape `trend[]` ngày 2026-09-10 theo Q43**, người
  dùng chốt. Nhật ký thay đổi: §1 "Đổi ở vòng 2026-09-10".
- Route: `GET /api/dashboard`
- Query params:

```
mode:    string    // "week" | "month" | "year"  — chữ thường. "year" = "Tất cả" của 1 năm
date:    date?     // mode=week: ngày bất kỳ TRONG tuần muốn xem
                   // mode=month: ngày bất kỳ TRONG tháng muốn xem
                   // bỏ trống = kỳ hiện tại (hôm nay)
year:    int?      // mode=year: năm cần tổng hợp. mode=week/month: năm dùng để dựng `trend`
                   // mode=week: gửi CẢ `date` lẫn `year` thì `year` PHẢI bằng năm ISO của
                   //   tuần chứa `date`; lệch -> 400 (Q61). Bỏ trống `year` = năm ISO đó (Q63)
                   // bỏ trống = năm hiện tại — mode=month|year, và mode=week khi cũng bỏ trống `date`

// ── Bộ lọc của BẢNG CHI TIẾT — cùng object với endpoint export (DB-4) ──
search:  string?   // khớp Code HOẶC Name, không phân biệt hoa/thường + không phân biệt dấu (Q47)
                   // kiểu khớp: cùng luật khớp của DM-2 (Q59)
groupId: guid?
status:  string?   // 1 trong 4 giá trị Trạng thái; giá trị lạ -> 400
```

> **Q47 (chốt 2026-09-10) — `search` KHÔNG phân biệt dấu**, giống hệt `search` của DM-2
> ([`danh-muc-dti.md`](danh-muc-dti.md)). Trước ngày này card im lặng về dấu, và im lặng thì
> người cài sẽ mặc định so khớp nguyên văn. Cách cài (cột chuẩn hoá) có file chủ là
> `spec/danh-muc-dti/business-rules.md` §1 — không mô tả lại ở đây.
>
> **Q59 (chốt 2026-09-10) — kiểu khớp:** cùng luật khớp của DM-2 — file chủ vẫn là
> `spec/danh-muc-dti/business-rules.md` §1. DB-4 dùng chung object bộ lọc nên tự hưởng theo.

- Response: `IApiResult<DashboardAggregateDto>`

```
mode:                "week" | "month" | "year"
periodLabel:         string   // "Tuần 33/2026 (10/08 – 16/08/2026)"
                              // "Tháng 8/2026 (01/08 – 31/08/2026)"  |  "Năm 2026"
periodStart:         date     // mốc kỳ, GHI RÕ, không để FE tự tính (Q12)
periodEnd:           date

kpi: {
  overallProgress:     number?   // % — bình quân gia quyền theo maxScore
  delta:               number?   // điểm phần trăm so với kỳ liền trước
  previousPeriodLabel: string?
  up:                  int       // số chỉ tiêu tăng so với kỳ trước
  flat:                int       // số chỉ tiêu không tăng
  down:                int
  done:                int       // số chỉ tiêu ở trạng thái "Hoàn thành"
  totalCriteria:       int
}

groups: [ { groupId: guid, groupCode: string, groupName: string, progress: number? } ]

trend:  [ { period: string, periodLabel: string, value: number? } ]
        // period:      khoá định danh của kỳ — "YYYY-Www" | "YYYY-MM" (cùng khuôn `value` của DB-3)
        // periodLabel: nhãn trục X BE DỰNG SẴN — "06/07 – 12/07" | "Th.1" (Q43)
        // value:       null = kỳ không có dữ liệu (Q44) — phần tử VẪN có mặt

table:  [ {
  criteriaId:    guid
  code:          string
  name:          string
  groupId:       guid
  groupCode:     string
  groupName:     string
  maxScore:      number
  selfScore:     number?
  verifiedScore: number?
  diff:          number?    // TÍNH = verifiedScore − selfScore (Q2 + Q25 — ĐẢO CHIỀU 2026-09-05)
  status:        string?    // 1 trong 4 giá trị, người dùng chọn tay (Q4)
  note:          string?
} ]
```

### Bảng chi tiết: 9 cột, ĐỔI bộ cột (Q8)

Cột hiển thị, đúng thứ tự đã duyệt: **Mã · Chỉ tiêu · Nhóm · Điểm tối đa · Tự đánh giá ·
Thẩm định · Chênh lệch · Trạng thái · Minh chứng/Ghi chú**.

> ⚠️ **Đã BỎ khỏi bản trước:** `previousValue`, `currentValue`, `delta` (3 cột
> Tuần trước / Tuần này / Tăng-giảm) và trường `badge`. Xu hướng theo kỳ đã có ở biểu đồ
> `trend` + hai ô KPI `up`/`flat`, nên không lặp lại trong bảng. `badge` biến mất vì bộ 4
> trạng thái nay do **người dùng chọn tay** (Q4) — không còn nhãn nào để hệ thống tự tính.
> Ánh xạ trạng thái → màu badge là việc của FE, khai ở `spec/dashboard-dti/business-rules.md`
> §Trạng thái, không phải trường của API.

### Ba luật về phạm vi tính toán — đọc kỹ, đây là chỗ dễ làm sai nhất

1. **Bộ lọc `search`/`groupId`/`status` chỉ áp cho `table`.** `kpi`, `groups`, `trend` LUÔN
   tính trên **toàn bộ** chỉ tiêu của kỳ. Lý do: nếu lọc một nhóm mà ô "Tiến độ chung" đổi
   theo, người đọc sẽ tưởng tiến độ toàn xã thay đổi — một con số đúng về mặt phép tính
   nhưng sai về mặt câu hỏi nó đang trả lời.
2. **`trend` trả ĐỦ các kỳ của phạm vi; kỳ không có dữ liệu mang `value: null`** — **Q44
   (chốt 2026-09-10)**. Không nội suy: `null` là "không có số", không phải số đoán ra.
   - **"Đủ các kỳ" — theo chế độ:**
     - `mode=week` ⇒ **cửa sổ 12 tuần KẾT THÚC ở tuần đang xem** — tuần mà `date` chỉ tới
       (bỏ trống `date` = tuần hiện tại), **không** phải tuần hiện tại của lịch — **Q54**.
       Cửa sổ **cắt ở đầu năm** đang lọc: xem tuần 3 ⇒ trả tuần 1…3 (3 phần tử), không bao giờ
       lấn sang năm trước — **Q57** (cả hai chốt 2026-09-10). Tuần thuộc năm nào: định nghĩa
       ISO của `spec/danh-muc-dti/business-rules.md` §5.1 (tuần 1 có thể bắt đầu từ tháng 12
       năm trước).
     - `mode=month|year` ⇒ `year` là năm hiện tại ⇒ từ tháng đầu năm tới **tháng hiện tại**,
       không trả kỳ tương lai; `year` là năm đã qua ⇒ trọn năm.
     - Trong phạm vi đó, kỳ rỗng vẫn là `null` — Q44 không đổi.

   > 🔄 **LẬT 2026-09-10 (Q54 + Q57), cùng ngày với Q44.** Bản sáng áp *"từ kỳ đầu năm tới
   > kỳ hiện tại; năm đã qua ⇒ trọn năm"* cho **cả** chế độ Tuần — tức tới 37 rồi 52–53 nhãn
   > khoảng ngày trên một trục. Q54 thu hẹp riêng chế độ Tuần thành 12 tuần (thiết kế đã duyệt
   > vẽ 6; người dùng chọn 12). Không đổi shape.
   - **Hai trường định danh mỗi phần tử — Q43 (chốt 2026-09-10):** `period` là **khoá**
     (`"YYYY-Www"` / `"YYYY-MM"`); `periodLabel` là **nhãn trục X BE dựng sẵn** —
     `mode=week` ⇒ khoảng ngày `06/07 – 12/07`; `mode=month|year` ⇒ `"Th.1"` … `"Th.12"`
     (**chốt T7, 2026-09-05**, không đổi sang khoảng ngày; 12 nhãn khoảng-ngày không đủ chỗ
     trên trục). Khuôn chuỗi: `spec/dashboard-dti/business-rules.md` §6.2. FE **không** tự quy
     đổi mã tuần ra ngày.
   - Tên `periodLabel` trùng với `periodLabel` ở cấp gốc là **có chủ đích** — cùng vai "chuỗi
     hiển thị của một kỳ, BE dựng" — nhưng **khuôn khác**: cấp gốc là nhãn kỳ đầy đủ
     (`Tuần 33/2026 (10/08 – 16/08/2026)`), còn đây là khuôn trục X.
   - `value` null ⇒ theo luật casing (§0 của [`danh-muc-dti.md`](danh-muc-dti.md)) khoá `value`
     **vắng mặt** trên dây. Điều bắt buộc là **phần tử của kỳ vẫn có mặt**, với `period` +
     `periodLabel`; thiếu phần tử là thiếu kỳ trên trục.

   > 🔄 **LẬT 2026-09-10 (Q44 + Q43).** Bản trước: *"`trend` chỉ trả điểm CÓ dữ liệu — không
   > nội suy, không trả điểm `null` để lấp chỗ"*, và `mode=week` → `label = "YYYY-Www"`.
   >
   > **Lý do của bản trước sai về kỹ thuật:** trên trục category của `chart.js`, bỏ hẳn một
   > điểm thì hai điểm kề nhau được nối **thẳng** — trục không có ô cho kỳ bị bỏ, nên không có
   > chỗ đứt nào. Chỉ `null` nằm đúng ô của kỳ (khi `spanGaps` tắt) mới ngắt được đường. Đừng
   > "dọn" `null` khỏi `trend` cho gọn. Vế "không nội suy" giữ nguyên. Đổi `label` →
   > `period` + `periodLabel` là **đổi shape card `AGREED`** — nhật ký ở §1.
3. **Công thức** (bình quân gia quyền theo `maxScore`, epsilon so sánh, ngưỡng "hoàn
   thành", cách gộp tháng/năm từ các kỳ tuần) nằm ở `spec/dashboard-dti/business-rules.md`
   §Công thức. **BE tính, FE chỉ hiển thị** — FE tính lại là tạo nguồn sự thật thứ hai.

### ⚠️ Khoá nào VẮNG MẶT khỏi JSON — bảng cho mapper FE (đo 2026-09-10)

Cùng luật với [`danh-muc-dti.md`](danh-muc-dti.md) §"Khoá nào VẮNG MẶT" — bằng chứng cấu hình
serializer ghi ở đó, không chép lại. Phần riêng của DB-1:

| Khoá | Có mặt khi nào |
| --- | --- |
| `mode` · `periodLabel` · `periodStart` · `periodEnd` · `kpi` · `groups` · `trend` · `table` | **LUÔN** — bốn mảng/object cuối có thể RỖNG nhưng không bao giờ vắng |
| `kpi.up` · `kpi.flat` · `kpi.down` · `kpi.done` · `kpi.totalCriteria` | **LUÔN** — số đếm, không nullable |
| `kpi.overallProgress` | chỉ khi có ít nhất một chỉ tiêu có `Tiến độ %` trong kỳ. **Ngay sau import: VẮNG** |
| `kpi.delta` · `kpi.previousPeriodLabel` | chỉ khi tìm được kỳ liền trước **CÓ DỮ LIỆU** và cả hai kỳ đều tính được `overallProgress`. **Ngay sau import: VẮNG** |
| `groups[].progress` | chỉ khi nhóm đó có ít nhất một chỉ tiêu có `Tiến độ %` trong kỳ |
| `trend[].value` | chỉ khi kỳ đó có dữ liệu. **Phần tử của kỳ thì LUÔN có mặt** kèm `period` + `periodLabel` — đừng lọc bỏ phần tử thiếu `value` |
| `table[].selfScore` · `verifiedScore` · `status` · `note` | chỉ khi chỉ tiêu có bản ghi đánh giá trong kỳ |
| `table[].diff` | chỉ khi **cả hai** điểm có mặt |

⚠️ **Ba khoá `overallProgress`/`delta`/`previousPeriodLabel` vắng mặt là trạng thái THƯỜNG GẶP
NHẤT trong ngày đầu chạy thật**, không phải ca hiếm (§0 + §1.6 của file luật). FE phải hiện dấu
gạch cho chúng, và **không** được coi khoá vắng là `0`.

⚠️ **Proxy nhận dạng ca Q32 đọc từ `kpi`, không từ `table`:**
`kpi.totalCriteria > 0 VÀ kpi.overallProgress vắng mặt`. Dùng `table.length` là sai — lọc một
nhóm không có dòng nào làm `table` rỗng trong khi kỳ vẫn đủ chỉ tiêu, và dải băng biến mất đúng
lúc nó cần hiện (🔄 LẬT Q69, §0).

> 🔴 **Chưa dán được payload THẬT** — cùng lý do và cùng điều kiện gỡ như đã ghi ở
> [`danh-muc-dti.md`](danh-muc-dti.md).

### Mã lỗi của DB-1 — vá 2026-09-09

Bản trước khai *"`status` giá trị lạ -> 400"* trong khối query param mà **không nêu mã nào**,
và cả card không có mục lỗi. Đóng lỗ đó:

| Ca | Mã | HTTP |
| --- | --- | --- |
| `status` không thuộc 4 giá trị | **`DASHBOARD.STATUS_INVALID`** | 400 |
| `mode` không thuộc `"week"` / `"month"` / `"year"` (kể cả sai hoa/thường) | **`DASHBOARD.MODE_INVALID`** | 400 |
| `mode=week` và `year` **khác** năm ISO của tuần chứa `date` (Q61) | **`DASHBOARD.PERIOD_YEAR_MISMATCH`** | 400 |
| `date` / `year` sai kiểu | `ValidationError` + `fields` — binder, **không** phải mã catalog | 400 |

- Ba mã in đậm là **mã mới**, khai trong `DashboardErrors.cs` ở `Business.Application`
  cùng chỗ với `DASHBOARD.EXPORT_MODE_UNSUPPORTED` — khuôn và lý do cưỡng chế bằng máy: xem
  DB-4 §"Catalog mã lỗi".
- **`DASHBOARD.PERIOD_YEAR_MISMATCH` — Q61 (chốt 2026-09-10), tên do người dùng đặt sẵn.**
  Ví dụ: `date = 2025-12-29` thuộc tuần 1/2026, nên `year` phải là `2026`. Tuần thuộc năm nào:
  định nghĩa ISO của `spec/danh-muc-dti/business-rules.md` §5.1. `MessageTemplate` dùng chỗ
  giữ **đặt tên**, `Retryable = false`. Không mã nào có sẵn phủ ca này — `MODE_INVALID` nói về
  `mode`, còn binder chỉ bắt sai **kiểu**, không bắt hai giá trị đúng kiểu mà lệch nhau.
- **Bỏ trống `year` ⇒ lấy năm ISO của tuần chứa `date` — Q63 (chốt 2026-09-10).** Chỉ khi gửi
  **cả hai** mà lệch nhau mới ra `400` (Q61); bỏ trống thì không có gì để mà lệch. Bỏ trống
  **cả** `date` lẫn `year` thì giữ mặc định cũ: tuần hiện tại của năm hiện tại. Chế độ
  `month`/`year` **không có ca này** — một tháng dương lịch luôn nằm gọn trong một năm — nên
  Q61 và Q63 là luật của **riêng chế độ Tuần**.
- **Gửi `year` mà KHÔNG gửi `date` ⇒ tuần ISO CUỐI của năm đó — Q71 (chốt 2026-09-10).**
  Ca này trước đây **không có luật nào**, và sự im lặng đó đã sinh ra hai kết luận ngược nhau
  trong cùng một ngày: BE cài fail-closed (`400`), FE thì gửi đúng như card cho phép.

  | `date` | `year` | Kỳ đang xem |
  | --- | --- | --- |
  | có | vắng | tuần ISO chứa `date` (Q63) |
  | có | có, **khớp** năm ISO của tuần chứa `date` | tuần ISO đó |
  | có | có, **lệch** | `400 DASHBOARD.PERIOD_YEAR_MISMATCH` (Q61) |
  | **vắng** | **có, = năm ISO hiện tại** | **tuần hiện tại** — giữ nguyên hành vi cũ |
  | **vắng** | **có, ≠ năm ISO hiện tại** | **tuần ISO CUỐI của năm `year`** |
  | vắng | vắng | tuần hiện tại của năm hiện tại |

  ⚠️ **`400` CHỈ sinh ở hàng 3** — khi gửi **cả hai** mà lệch nhau. Một bản cài neo `date`
  mặc định vào *hôm nay* rồi áp Q61 sẽ bắn `400` cho **hàng 5**, và hàng 5 là đường đi hoàn
  toàn hợp lệ: DB-3 khai *"năm không có dữ liệu ⇒ `weeksInYear` **rỗng**, `200`, không phải
  lỗi"*, nên FE **không có** `date` nào để gửi. Đây là lỗi đã xảy ra thật và đã sửa.

  **Vì sao tuần CUỐI chứ không phải tuần 1:** khớp ngữ nghĩa *"mới nhất"* mà cả mô hình dùng
  (§5.2 của `spec/danh-muc-dti/business-rules.md` lấy bản ghi có `AssessmentDate` lớn nhất), và
  cho biểu đồ `trend` một cửa sổ 12 tuần đầy đủ thay vì đúng một cột — Q57 cắt cửa sổ ở **đầu**
  năm, nên neo vào tuần 1 sẽ trả về một trục chỉ có một điểm.

  **Vì sao SERVER quy đổi chứ không phải FE tự tính:** cùng lý do Q40 — lịch ISO chỉ có **một**
  bản cài, ở BE. Bắt FE dựng một `date` cho năm chưa có dữ liệu là dựng bản sao thứ hai của lịch
  ISO, cộng thêm một quyết định nghiệp vụ (*"năm cũ thì mở ra ở tuần nào"*) mà FE không có thẩm
  quyền chốt.

  **Nghiệm thu:** `GET /api/dashboard?mode=week&year=<năm chưa có dữ liệu>` (không `date`) ⇒
  `200`, `periodLabel` chỉ tuần cuối của năm đó. Trả `400` là bản cài còn theo hành vi cũ.
- **Người dùng bình thường không gặp ba mã trên — Q62 (chốt 2026-09-10).** FE đưa tham số lạ
  trên URL về mặc định **trước** khi gọi API (`spec/dashboard-dti/ui-spec.md` §2), nên các mã
  này là lưới chặn cuối ở BE và **không cần câu hiển thị**. Chúng vẫn phải có ở BE.
- **`DASHBOARD.MODE_INVALID` ≠ `DASHBOARD.EXPORT_MODE_UNSUPPORTED`.** Cái đầu nghĩa là *"giá
  trị này không phải một `mode`"*; cái sau nghĩa là *"`year` là `mode` hợp lệ, nhưng endpoint
  export không phục vụ nó"* (DB-4). Gộp làm một thì câu chữ FE hiện cho người dùng sai ở đúng
  ca hay gặp: bấm Xuất khi đang xem `Tất cả`.
- **`status` lạ KHÔNG được âm thầm bỏ lọc.** Bảng chi tiết trả về đủ dòng trong khi ô lọc vẫn
  hiện một trạng thái là ca hỏng người dùng không có cách nào phát hiện.
- `mode` là tham số **bắt buộc**; vắng mặt cũng ra `DASHBOARD.MODE_INVALID`, không cần mã
  `..._REQUIRED` riêng — cả hai ca đều kết thúc bằng "gửi lại một `mode` hợp lệ", và FE không
  bao giờ để trống nó.

---

## CONTRACT DB-2 — GỠ 2026-09-05

`GET /api/dashboard/report` (trả `{ title, contentHtml }` cho `report-dialog` bind qua
`bypassSecurityTrustHtml`) **không còn trong phạm vi**. Nút "Xuất báo cáo" nay tải thẳng
file `.xlsx`, không dialog, không xem trước (Q13) — thay bằng **DB-4** dưới đây.

Giữ mục này lại thay vì xoá trắng vì hai lý do: người từng đọc bản trước cần biết route đó
đã biến mất chứ không phải bị quên; và component `report-dialog` cùng khối CSS
`app-report-dialog` **không cần port sang `src/FE`**.

> 🔄 **SỬA 2026-09-06.** Vế thứ hai trước đây viết *"…trong prototype nay là **style chết**,
> cần gỡ chứ không cần port lại"*, tức giao một việc dọn dẹp cho một thứ không tồn tại: không có
> selector `report-dialog` nào trong `src/FE/src/styles.scss` (đối chiếu 2026-09-06), và
> thư mục prototype HTML cũ đã bị xoá 2026-08-23 (`.claude/CLAUDE.md` §7 — nguồn giao diện duy
> nhất nay là `doc/Design/`). Không có gì để gỡ — chỉ có thứ không được dựng lên.

---

## CONTRACT DB-3 — Danh sách Năm/Kỳ có dữ liệu (dùng CHUNG với Danh mục DTI)

- **Status: AGREED** (2026-08-16) — **giữ nguyên, không sửa ở lượt 2026-09-05.**
- Route: `GET /api/dashboard/periods`
- Query params: `year: int?` (nếu có, trả `weeksInYear`/`monthsInYear` của đúng năm đó)
- Response: `IApiResult<PeriodOptionsDto>`

```
years:        int[]     // mọi năm có dữ liệu, LUÔN kèm năm hiện tại dù chưa có dữ liệu
weeksInYear:  [ { value: string, date: date, overallProgress: number? } ]
monthsInYear: [ { value: string, date: date, overallProgress: number? } ]
```

- `value` chính là giá trị truyền vào `period` của `GET /api/criteria`
  ([`danh-muc-dti.md`](danh-muc-dti.md) DM-2) và vào `date`/`mode` của DB-1 —
  vd `"2026-W33"` (tuần ISO), `"2026-08"` (tháng).
- `overallProgress` của mỗi kỳ tính bằng **cùng** công thức bình quân gia quyền theo
  `maxScore` dùng cho DB-1 — một công thức, một chỗ cài.
- `history-list` trên Dashboard **không có endpoint riêng**: tái dùng `weeksInYear`, tự tính
  chênh lệch giữa các kỳ liền kề ở FE.
- Owner FE: một service dùng chung ở `shared/` (≥2 feature dùng — 📖
  [`../huong_dan/quy-uoc/fe-architecture.md`](../huong_dan/quy-uoc/fe-architecture.md)).

> **Bổ sung 2026-09-05 — hiển thị, không phải đổi shape.** Q12 yêu cầu mọi chỗ hiện kỳ phải
> ghi rõ **từ ngày đến ngày**. Shape trên **đủ dữ liệu** để FE dựng nhãn
> `"Tuần 33 · 10/08 – 16/08 · 82,1%"` (khuôn chuỗi + luật khoảng trắng quanh dấu gạch:
> `spec/dashboard-dti/business-rules.md` §6.2) mà không cần trường mới: `value` cho số tuần, `date`
> cho mốc đầu kỳ, và tuần ISO thì kết thúc sau đúng 6 ngày. Không thêm trường vào card
> `AGREED` khi dữ liệu đã đủ — thêm là phá một hợp đồng đã chốt để lấy thứ tính được.

### Mã lỗi của DB-3 — khai tường minh 2026-09-09: **KHÔNG có mã nghiệp vụ nào**

Đây là câu trả lời, không phải mục còn thiếu. Card này trước đây im lặng về lỗi, và im lặng
không phân biệt được *"không có mã"* với *"quên khai"* — người thi công gặp im lặng sẽ tự bịa
một mã, hoặc tự thêm một ràng buộc không ai chốt.

| Ca | Kết quả |
| --- | --- |
| `year` không phải số nguyên | `400 ValidationError` + `fields.Year` — của binder, không phải catalog |
| `year` là một năm **không có dữ liệu** | **`200`** — `weeksInYear`/`monthsInYear` là mảng **rỗng**, `years` vẫn luôn kèm năm hiện tại. Không phải lỗi |
| Không gửi `year` | `200` — mặc định năm hiện tại |

**Năm rỗng không phải lỗi** là điểm dễ làm sai nhất ở đây: endpoint này nuôi hai ô lọc của
**hai** màn, và trả 404/400 cho một năm chưa nhập số liệu sẽ khoá cứng ô `Năm` của người dùng
ngay lần đầu họ mở một năm mới — đúng lúc chưa thể có dữ liệu.

`DASHBOARD.STATUS_INVALID` và `DASHBOARD.MODE_INVALID` (DB-1) **không** áp cho DB-3: endpoint
này không nhận `mode` lẫn `status`.

---

## CONTRACT DB-4 — "Xuất báo cáo": tải thẳng file `.xlsx`

- **Status: IMPLEMENTED** (thi công + gọi thật 2026-09-11; chốt hợp đồng 2026-09-05) — thay DB-2 đã gỡ.
- Thi công: `src/BE/Business/PlatformManager.Business.Api/Controllers/DashboardController.cs` ·
  handler `…/Business.Application/Dashboard/ExportDashboardQuery.cs` ·
  bộ ghi NPOI `…/Business.Infrastructure/Export/DashboardExcelExportWriter.cs`

### Nghiệm thu — gọi thật trên `platformmanager_dev` ngày 2026-09-11

**Nhánh thành công KHÔNG bọc envelope, đúng như card khai** (dán nguyên văn header thật):

```
HTTP 200
Content-Type: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet
Content-Disposition: attachment; filename=bao-cao-dti_Tuan-33-2026.xlsx; filename*=UTF-8''bao-cao-dti_Tuan-33-2026.xlsx
```

Thân là bytes `.xlsx` thật (bắt đầu bằng chữ ký zip `PK`), **không** phải JSON.

**Nhánh lỗi VẪN bọc envelope** — đây là thứ làm ngoại lệ ở trên kiểm được:

| Gửi gì | HTTP | `Content-Type` | `businessCode` |
| --- | ---: | --- | --- |
| `mode=year` | 400 | `application/json` | `DASHBOARD.EXPORT_MODE_UNSUPPORTED` |
| `mode=quarter` | 400 | `application/json` | `DASHBOARD.MODE_INVALID` |
| `status` ngoài 4 giá trị | 400 | `application/json` | `DASHBOARD.STATUS_INVALID` |

**Kiểm §4.6 — số dòng file = số phần tử `data.table`**, chạy trên 62 chỉ tiêu thật với năm bộ
tham số khác nhau, tất cả **KHỚP**:

| Bộ tham số | `data.table` | dòng trong file |
| --- | ---: | ---: |
| `mode=week` | 62 | 62 |
| `mode=week` + `status=Hoàn thành` | 15 | 15 |
| `mode=week` + `search=4.2` | 1 | 1 |
| `mode=week` + `search=nen tang` (không dấu — Q47) | 8 | 8 |
| `mode=month` + `status=Đang thực hiện` | 16 | 16 |

**Bố cục file thật** (đọc ngược bằng cách giải nén OOXML): tên sheet `Tuần 33-2026`; dòng 1 tiêu
đề; dòng 5 `Thuộc tháng` = `Tháng 8/2026` ở file tuần, còn file tháng ghi `Gồm các tuần` =
`Tuần 31 (27/07 – 02/08) · … · Tuần 36 (31/08 – 06/09)` — **liệt kê cả tuần vắt sang tháng 7 và
tháng 9**, đúng luật "mọi tuần GIAO với tháng"; dòng 10 `Bộ lọc đang áp` = `Không lọc — đủ 62 chỉ
tiêu`; header 12 cột ở dòng 12; dòng cuối `TỔNG CỘNG` với tổng bốn cột điểm và **cột `Tiến độ %`
để trống**.
- Route: `GET /api/dashboard/export`
- Query params: **đúng bộ của DB-1**, thêm không gì cả:

```
mode:    "week" | "month"     // KHÔNG hỗ trợ "year" — xem ghi chú bên dưới
date:    date?
year:    int?
search:  string?
groupId: guid?
status:  string?
```

### Response — đây là endpoint DUY NHẤT của hệ thống không trả envelope

- **Thành công**: `200`, thân là **bytes của file**, không bọc `IApiResult<T>`.
  - `Content-Type: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`
  - `Content-Disposition: attachment; filename="bao-cao-dti_Tuan-33-2026.xlsx"; filename*=UTF-8''bao-cao-dti_Tuan-33-2026.xlsx`
  - Mẫu tên file: `bao-cao-dti_Tuan-{tuần}-{năm}.xlsx` · `bao-cao-dti_Thang-{tháng}-{năm}.xlsx`
    — chỉ ký tự ASCII, không dấu, không khoảng trắng. Trình duyệt và mọi hệ tệp mở được
    mà không cần giải mã; đó là lý do có `filename` ASCII cạnh `filename*`.
- **Lỗi**: **vẫn** là `IApiResult<T>` JSON như mọi endpoint khác (`400`/`403`/`429`/`500`).
  FE phải kiểm `Content-Type` của response trước khi coi body là file — nhận JSON tức là lỗi.
- **`mode=year` không hỗ trợ**: `400` + `DASHBOARD.EXPORT_MODE_UNSUPPORTED`. Bố cục file đã
  duyệt có khối nhận dạng kỳ với `Từ ngày`/`Đến ngày` của **một** kỳ (Q14) — "cả năm" không
  ánh xạ được vào khuôn đó mà không thiết kế lại file.
- **`mode=month` xuất TRỌN tháng** (Q15), không phải một tuần nằm trong tháng.

  > **Q37 (2026-09-06 — "chỉ nhập theo tuần") KHÔNG thu hẹp endpoint này.** Quyết định đó nói
  > về đường **ghi** của màn Danh mục: không nhập liệu vào một kỳ tháng, vì mô hình lưu theo
  > ngày không neo được một tháng vào đúng một tuần ISO. Xuất một tháng là phép **tổng hợp
  > trên dữ liệu đã có**, không tạo bản ghi nào. `mode=month` giữ nguyên, kể cả khối
  > `Gồm các tuần` của bố cục file. Đây là chỗ dễ "dọn nhầm" nhất khi đọc Q37 vội.
- **Catalog mã lỗi — bắt buộc, cưỡng chế bằng máy.** `DASHBOARD.EXPORT_MODE_UNSUPPORTED`
  phải khai thành `ErrorDescriptor` trong **`DashboardErrors.cs`** đặt ở
  `Business.Application` (khuôn ở
  [`../huong_dan/quy-uoc/be-cqrs-handler.md`](../huong_dan/quy-uoc/be-cqrs-handler.md)
  § `ErrorDescriptor`). Không phải quy ước đặt tên cho đẹp: `ArchTests` nhận diện file
  catalog **bằng đuôi `Errors.cs`** và bắt lỗi mã nào không khai trong đó — khai thiếu là
  test đỏ, không phải cảnh báo. `MessageTemplate` dùng chỗ giữ **đặt tên** (`{Mode}`),
  không phải `{0}`; khai `Retryable` tường minh.

> **Vì sao được phép không bọc envelope, trong khi luật nói "mọi response đi qua
> `IApiResult<T>`":** `HandleResult<T>` serialize `T` thành JSON
> (`src/BE/Core/PlatformManager.Core.Api/ApiControllerBase.cs:38`) — nhồi vài trăm KB bytes vào
> `data` dưới dạng base64 làm file phình ~33% và buộc FE phải giải mã trong bộ nhớ trước khi
> đưa cho người dùng lưu. Đây là ngoại lệ **có phạm vi hẹp và kiểm được**: chỉ áp cho nhánh
> thành công của endpoint tải file; nhánh lỗi vẫn đi đúng đường chung. Luật gốc:
> [`../huong_dan/wiki-core/be/15-import-export.md`](../huong_dan/wiki-core/be/15-import-export.md)
> §3 — "dưới ngưỡng thì trả thẳng `FileStreamResult`".

### Bố cục file — 1 sheet, khớp 2 file mẫu đã duyệt

Đặc tả đầy đủ từng ô (nhãn dòng 2–10, 12 cột của header, dòng `TỔNG CỘNG`, định dạng số) ở
`spec/dashboard-dti/business-rules.md` §Export. Card này chỉ giữ phần thuộc hợp đồng đường
dây:

- **Đúng 1 sheet.** Tên sheet: `Tuần 33-2026` / `Tháng 8-2026`.
- **12 cột = 11 cột của file import + `Tiến độ %`** ⇒ export là **superset** của import, tức
  file tải về nạp ngược lại được (round-trip). Đây là ràng buộc thiết kế, không phải trùng
  hợp: hai bộ cột lệch nhau thì người dùng sửa file export rồi import lại sẽ mất dữ liệu.
- **Cột `Nhóm` ghi TÊN TRẦN** — đúng chuỗi `CriteriaGroup.Name`, như file import — **Q55
  (chốt 2026-09-10)**. Dạng `Code. Name` của Q42 là luật **màn hình**, **không** áp cho file
  xuất: import khớp nhóm theo đúng chuỗi tên, nên một ô `1. Hạ tầng và Nền tảng số` nạp ngược
  lại sẽ không khớp nhóm nào — gãy đúng round-trip mà gạch đầu dòng trên bảo vệ.
- **Dòng 10 `Bộ lọc đang áp` là bắt buộc** (chốt 2026-09-06), kể cả khi không lọc thì vẫn
  ghi `Không lọc — đủ 62 chỉ tiêu`. Đây là phần thuộc hợp đồng vì nó là **bằng chứng đi theo
  file**: export tôn trọng bộ lọc (Q23), mà file thì rời khỏi màn hình — người nhận nó qua
  email không có cách nào biết nó được xuất lúc đang lọc gì.
- **Chỉ `.xlsx`** — và đây là chỗ **THU HẸP so với Core**, không phải làm theo Core.

  > ### ⚠️ Sửa 2026-09-05 — viện dẫn sai luật ở bản trước
  >
  > Bản sáng nay ghi *"Không CSV, không `.xls` — khớp `15-import-export.md` §3"*. Sai:
  > §3 của file đó khai **"Định dạng export: XLSX + CSV. KHÔNG làm `.xls`."** Nghĩa là
  > Core **cho** CSV; bỏ CSV không phải luật Core mà là **Q13 của người dùng** (nút tải
  > thẳng một file, không dialog chọn định dạng).
  >
  > | | Core (`15-import-export.md` §3) | Màn này |
  > | --- | --- | --- |
  > | `.xlsx` | có | có |
  > | `.csv` | **có** | **không** — Q13 thu hẹp |
  > | `.xls` | không | không |
  >
  > Phân biệt này không phải bắt bẻ câu chữ. Viện dẫn luật Core cho một lựa chọn của sản
  > phẩm nghĩa là người sau **không sửa được nó** khi người dùng đổi ý — họ sẽ tưởng đang
  > phá một luật nền tảng. Bỏ CSV là quyết định của **màn hình này**, gỡ lúc nào cũng được.
  >
  > Hệ quả kèm theo: hai cái bẫy CSV + tiếng Việt của Core (§5 — BOM UTF-8 và dấu phân cách
  > theo locale `vi-VN`) **chưa** phải xử lý cho đường này, và sẽ phải xử lý **ngay khi** ai
  > đó thêm `format=csv` trở lại.

### Projection của export là RIÊNG, không dùng lại `table[]` của DB-1 — Q68 (chốt 2026-09-10)

File xuất cần **12 cột** (`spec/dashboard-dti/business-rules.md` §4.3), trong đó ba cột
`Phụ trách` · `Hạn xử lý` · `Tiến độ %` **không có** trong `table[]` của DB-1 — bảng chi tiết
trên màn chỉ hiện 9 cột (Q8).

| Câu hỏi | Chốt |
| --- | --- |
| DB-1 `table[]` có thêm ba trường đó không | **KHÔNG** — shape của card `AGREED` giữ nguyên 9 cột |
| Export lấy dữ liệu ở đâu | **projection RIÊNG** của handler export, mang đủ 12 cột, kể cả `OwnerName` (tên đầy đủ tra từ `OwnerId`), `Deadline`, `ProgressPercent` |
| Hai đường có dùng chung gì không | **có — đúng object bộ lọc** (`search`/`groupId`/`status` + `mode`/`date`/`year`) và **đúng luật chọn bản ghi đại diện cho kỳ** (`spec/danh-muc-dti/business-rules.md` §5.2, Q46). Chỉ **bộ trường chiếu ra** là khác |

**Vì sao không phình `table[]` cho tiện:** ba trường đó không có cột nào trên màn để hiện, nên
thêm vào là bắt **mọi** lần tải Dashboard mang theo dữ liệu không ai đọc — cộng một lần tra tên
người cho từng dòng mà màn hình không dùng tới. Chiều ngược cũng sai: bỏ ba cột khỏi file xuất
là phá round-trip export → sửa → import (§4.3 của file luật).

⚠️ **Ràng buộc phải giữ khi cài hai projection rời nhau:** kiểm nghiệm thu *"số dòng file = số
phần tử `data.table`"* (`spec/dashboard-dti/business-rules.md` §4.6) là thứ duy nhất cưỡng chế
hai đường còn lọc giống nhau. Hai projection rời nhau nghĩa là một lần sửa bộ lọc ở một đường sẽ
**không** làm đường kia đỏ — trừ kiểm này. Đừng bỏ nó, và đừng để hai đường tự viết lại luật lọc:
tách **một** chỗ dựng `IQueryable` đã lọc, hai handler chỉ khác phép `Select`.

### Đường đồng bộ, không job nền

62 chỉ tiêu nằm **rất xa** ngưỡng cần job nền. Đi đường đồng bộ: không chạm đĩa, không tạo
`ExportJob`, không cần retention. Ngưỡng chuyển sang job nền là **cấu hình**, không phải
hằng số rải trong code (`15-import-export.md` §3).

> 📖 **Q9 (2026-09-09) — endpoint này ghi NPOI trực tiếp ở `Business.Infrastructure`, KHÔNG
> qua seam `ITabularWriter`.** Seam đó hoãn tới khi có người tiêu thụ thứ hai; lý do (bố cục
> file ở đây không diễn đạt được bằng "cột + dòng") và ràng buộc vẫn còn hiệu lực:
> [`../huong_dan/wiki-core/be/15-import-export.md`](../huong_dan/wiki-core/be/15-import-export.md)
> §7. Hợp đồng của DB-4 **không đổi** vì việc này.

### Tôn trọng bộ lọc đang áp — ĐÃ CHỐT (Q23, 2026-09-05)

Export dùng **cùng object bộ lọc** với `GET /api/dashboard`: đang lọc nhóm nào thì xuất
nhóm đó. Không còn là mục "cần chốt".

Hệ quả của Q47 (2026-09-10): `search` của export **không phân biệt dấu** y như DB-1 — không
phải một luật riêng của DB-4, nó tự đến vì hai endpoint dùng chung một object bộ lọc.

⚠️ **Rủi ro UX đã biết, ghi ra để FE xử lý chứ không phải để bỏ qua:** nút "Xuất báo cáo"
nằm ở **thanh chọn kỳ trên cùng**, còn ba ô lọc nằm ở **thanh công cụ của bảng gần cuối
trang**. Người dùng bấm Xuất khi đã cuộn lên đầu có thể không nhớ mình còn đang lọc một
nhóm. Cách hiển thị (nhắc bộ lọc đang áp cạnh nút, hoặc trong tên file) thuộc
`spec/dashboard-dti/ui-spec.md`.

📖 Test nghiệm thu ràng buộc này (số dòng file = số phần tử `data.table`) khai **đúng một
chỗ**: `spec/dashboard-dti/business-rules.md` §Export. Không chép ra đây.

---

## Rate limit và cache — áp cho MỌI endpoint của card này (Q70, chốt 2026-09-10)

| Câu hỏi | Chốt |
| --- | --- |
| Policy rate limit riêng cho DB-1 / DB-3 / DB-4 | **KHÔNG** — không gắn `[EnableRateLimiting]` vào controller Dashboard |
| Thứ canh chúng | **`GlobalLimiter`** đã chạy sẵn ở host, phủ mọi request đi qua `UseRateLimiter` |
| Cache (server hoặc HTTP) | **KHÔNG** — không `ResponseCache`, không `IMemoryCache`, không `ETag` |

**Vì sao không thêm policy riêng:** policy riêng chỉ đáng khi một endpoint có hồ sơ lạm dụng khác
phần còn lại — như `login`, nơi mỗi lần gọi là một lần đoán mật khẩu. Ba endpoint ở đây đều
`[Authorize]`, chỉ đọc, và chạy trên một tập vài chục chỉ tiêu. Thêm một con số phải chăm sóc cho
một rủi ro chưa đo được là thêm một chỗ hỏng ít người nhớ.

**Vì sao không cache — lý do nghiệp vụ, không phải lười:** dữ liệu đằng sau ba endpoint này **đổi
ngay sau mỗi lần sửa inline và mỗi lần import**, mà cả hai đều là thao tác người dùng làm rồi
**quay lại xem ngay**. Một bản cache dù chỉ vài chục giây cũng biến "số tôi vừa nhập đi đâu mất"
thành một báo lỗi không tái hiện được. Ngưỡng để xem lại: khi tập chỉ tiêu vượt xa quy mô hiện nay
hoặc có đo được một truy vấn chậm thật — **có số đo trước, mới thêm cache**.

---

## 1. Bảng đối chiếu nhanh — bản DRAFT cũ → bản này

| Bản DRAFT cũ | Bản 2026-09-05 | Vì sao |
| --- | --- | --- |
| DB-2 `GET /api/dashboard/report` trả HTML | **gỡ**, thay bằng DB-4 tải `.xlsx` | Q13 |
| `table[].previousValue`/`currentValue`/`delta` | thay bằng `selfScore`/`verifiedScore`/`diff` | Q8 |
| `table[].badge` (BE tự tính, 4 nhãn cũ) | **gỡ** — còn `status` 4 giá trị người dùng chọn tay | Q4 |
| `periodLabel: "Tháng 8/2026"` | `periodLabel` ghi rõ khoảng ngày + thêm `periodStart`/`periodEnd` | Q12 |
| Không có tham số lọc | `search`/`groupId`/`status` áp cho `table` | Prototype đã duyệt + luật §4 export |
| Casing "CHƯA XÁC NHẬN" | CHỐT — 📖 [`danh-muc-dti.md`](danh-muc-dti.md) §0 | Bằng chứng source |

### Đổi thêm ở vòng sửa thứ ba (2026-09-05)

| Bản trước | Bản này | Vì sao |
| --- | --- | --- |
| `table[].diff = selfScore − verifiedScore` | **`= verifiedScore − selfScore`** | **Q25** |
| Trục X chế độ tháng "đang viết theo, chưa chốt" | **chốt** `Th.1 … Th.12` | T7 |
| Chưa nói ô KPI nào có số ngay sau import | ô 1, 2 hiện `—`; ô 3, 4 hiện `0`; ô 5 có số thật — file chủ `spec/dashboard-dti/business-rules.md` §1.6 | **T12** (2026-09-06) |
| — | `mode=month` của DB-4 **không** bị Q37 thu hẹp | Q37 (2026-09-06) |
| §Cần chốt còn mục để ngỏ | **hết mục** — mọi mục đã có đáp án | Q21 · Q22 · Q23 · T7 · tên thư mục |

### Đổi ở vòng 2026-09-10 — nhật ký thay đổi của DB-1

| Bản trước | Bản này | Vì sao |
| --- | --- | --- |
| `trend: [ { label, value } ]`; `mode=week` → `label = "YYYY-Www"` | `trend: [ { period, periodLabel, value } ]` — `period` là khoá (`"YYYY-Www"` / `"YYYY-MM"`), `periodLabel` là nhãn trục X BE dựng sẵn (`06/07 – 12/07` / `Th.1`) | **Q43** — **đổi shape card `AGREED`**, người dùng chốt 2026-09-10 |
| `trend` chỉ trả điểm có dữ liệu | trả **đủ các kỳ**, kỳ rỗng `value: null` (khoá vắng mặt trên dây, phần tử vẫn có mặt); hai ô `trend` ở bảng §0 sửa theo | **Q44** |
| `search` im lặng về dấu | không phân biệt hoa/thường **và** dấu, như DM-2; DB-4 hưởng theo | **Q47** — khai rõ, **không** đổi shape |
| Nhóm hiển thị thế nào | `Code. Name` — **không** đổi shape: `groupCode` có sẵn ở `groups[]` và `table[]` | **Q42** |
| Hai bản ghi một tuần | lấy `AssessmentDate` lớn nhất — **không** đổi shape; luật ở `spec/dashboard-dti/business-rules.md` §1.2 | **Q46** |
| `trend` chế độ Tuần: mọi tuần từ đầu năm tới tuần hiện tại (Q44, bản sáng 2026-09-10) | **cửa sổ 12 tuần kết thúc ở tuần đang xem** (theo `date`), **cắt ở đầu năm** đang lọc — **không** đổi shape | **Q54 + Q57** |
| Cột `Nhóm` của file xuất — chưa nói rõ dạng | **tên trần** `CriteriaGroup.Name`; Q42 **không** áp cho file xuất, để round-trip qua import | **Q55** (DB-4) |
| `search` chưa nói kiểu khớp | cùng luật khớp của DM-2 — file chủ `spec/danh-muc-dti/business-rules.md` §1; DB-4 hưởng theo | **Q59** — **không** đổi shape |
| Bỏ trống `year` khi `date` thuộc năm khác | lấy **năm ISO của tuần chứa `date`**; chỉ lệch khi gửi **cả hai** mới `400` | **Q63** (2026-09-10) |
| `date` và `year` lệch năm ISO: chưa nói | `mode=week` ⇒ `400` + **`DASHBOARD.PERIOD_YEAR_MISMATCH`** (mã mới, `Retryable = false`) | **Q61** |
| Mã lỗi DB-1 chưa nói người dùng có gặp không | FE chặn tham số lạ trên URL trước khi gọi API; mã giữ ở BE, **không** cần câu hiển thị | **Q62** |

---

## 2. Cần chốt — hết mục, giữ bảng đóng mục

**Card này không còn mục nào để ngỏ.** Bảng dưới giữ lại các mục đã đóng, để người từng đọc
bản trước không đi tìm câu trả lời ở chỗ khác.

| Mục cũ | Đóng bằng | Đáp án | Ghi ở |
| --- | --- | --- | --- |
| Ai được xem Dashboard | **Q21** | mọi người đăng nhập; không `[RequirePermission]` trên đường đọc | §0 |
| Export có tôn trọng bộ lọc | **Q23** | **CÓ** | DB-4 §Tôn trọng bộ lọc |
| Bộ lọc "Mức thay đổi" + sắp xếp "Tăng nhiều nhất" | **Q22** | **BỎ** cả ô lọc lẫn 2 tuỳ chọn sắp xếp. DB-1 **không** thêm `deltaBucket`/`sort`, `table[]` **không** thêm trường chênh lệch so với kỳ trước. Hai ô KPI `up`/`flat` vẫn giữ — chúng chỉ hiển thị | DB-1 |
| Trục X biểu đồ chế độ Tháng | **T7** | giữ `Th.1 … Th.12` | DB-1, luật 2 |
| Tên thư mục `spec/` | đã xong 2026-09-05 | `spec/dashboard-dti/` | [`danh-muc-dti.md`](danh-muc-dti.md) §4 |

**Quyền GHI** không xuất hiện ở đây vì Dashboard **không có đường ghi nào** — nó đã chốt ở
màn kia: `spec/danh-muc-dti/business-rules.md` §6.5 (một key, mọi kỳ).

Hai câu hỏi **còn mở** của cả cụm DTI nằm ở `spec/danh-muc-dti/business-rules.md` §8. Một
trong hai chạm card này: nếu chốt lưu kỳ đích thành một cột riêng thay vì suy từ
`AssessmentDate`, thì `mode=week` của DB-1 phải nói rõ nó lọc theo cột nào.

---

## 3. Nghiệm thu — điều kiện chuyển card sang `IMPLEMENTED`

1. Gọi thật DB-1 ở cả 3 `mode` trên DB có dữ liệu, dán shape response thật vào card.
2. Gọi DB-1 khi **chưa có dữ liệu nào** — phải là `200` + mảng rỗng, **không** phải 404 (§0).
3. Tải file DB-4 ở cả `mode=week` và `mode=month`, mở bằng Excel: đúng 1 sheet, đúng 12 cột,
   có dòng `TỔNG CỘNG`, tiếng Việt không lỗi phông.
4. Chạy **test ràng buộc bộ lọc** — đặc tả đầy đủ (điều kiện, bộ tham số, vì sao không
   thương lượng được) khai **đúng một chỗ**: `spec/dashboard-dti/business-rules.md` §4.6.
5. **Ca Q25 — dấu của `Chênh lệch`.** Trên dữ liệu gốc của BA, chỉ tiêu `1.1` phải ra
   **`+2,96`** (không phải `−2,96`) và chỉ tiêu `1.4` phải ra **`−5,00`**; dòng `TỔNG CỘNG`
   của cột đó phải ra **`−181,57`**. Tiêu đề cột 7 trong file `.xlsx` phải đọc được nguyên văn
   `Chênh lệch (Thẩm định − Tự đánh giá)` — thiếu phần trong ngoặc là thiếu đúng thứ khiến
   người đối chiếu với file gốc không hoảng.
6. **Ca Q61 — `date` và `year` lệch năm ISO.** Gọi DB-1 với `mode=week&date=2025-12-29&year=2025`
   ⇒ `400` + `DASHBOARD.PERIOD_YEAR_MISMATCH` (ngày đó thuộc tuần 1/2026). Cùng `date` với
   `year=2026` ⇒ `200`, `trend` chỉ có đúng tuần 1 (Q57 — cửa sổ cắt ở đầu năm).
   Cùng `date` mà **bỏ trống** `year` ⇒ `200`, hiểu là năm 2026 (Q63) — không phải `400`.

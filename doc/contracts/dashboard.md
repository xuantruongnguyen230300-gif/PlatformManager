# API Contract — Dashboard DTI Weekly (`modules/dashboard`)

> Owner FE: `src/FE/src/app/modules/dashboard/services/dashboard.service.ts`
> Nguồn nghiệp vụ: `spec/dashboard-dti-weekly/business-rules.md`,
> `spec/dashboard-dti-weekly/ui-spec.md`. Dashboard **100% read-only**.
> Tất cả request/response **PascalCase, FLAT**. Trạng thái card: **IMPLEMENTED**
> — đã code, build sạch, đã tự smoke-test bằng curl với dữ liệu import thật từ
> `doc/ERD/example_db_ver1.csv`.
>
> **backend-expert đã SỬA route/shape khác với bản DRAFT** — người giao việc
> (`main`) đã chỉ định chính xác 2 route `GET /api/dashboard` và
> `GET /api/dashboard/report` trong yêu cầu gốc. Đọc mục "Khác biệt so với
> DRAFT" cuối file trước khi cập nhật `DashboardService`.

## CONTRACT DB-1 — Tổng hợp Dashboard theo Tuần/Tháng/Năm ("Tất cả")

- Status: **IMPLEMENTED**
- Route: `GET /api/dashboard` *(KHÔNG phải `/api/dashboard/summary`)*
- Query params:
  ```
  mode: string           // "week" | "month" | "year"  ("year" = "Tất cả" của 1 năm) — chữ THƯỜNG
  date: date?             // dùng cho mode=week (ngày bất kỳ trong tuần muốn xem) hoặc mode=month (ngày bất kỳ trong tháng); mặc định = hôm nay
  year: int?              // dùng cho mode=year (mặc định = năm hiện tại nếu bỏ trống); mode=week/month vẫn nhận year để build Trend đúng năm đang chọn
  ```
- Response: `ApiResponse<DashboardResponseDto>`
  ```
  Mode: string
  PeriodLabel: string             // vd "Tuần 33/2026 (10/08–16/08/2026)", "Tháng 8/2026", "Năm 2026"
  Kpi: DashboardKpiDto
  Groups: DashboardGroupProgressDto[]
  Trend: DashboardTrendPointDto[]
  Table: DashboardTableRowDto[]
  ```
  `DashboardKpiDto`:
  ```
  OverallProgress: number?      // null = "chưa có dữ liệu"
  Delta: number?                 // null khi không có kỳ liền trước có dữ liệu
  PreviousPeriodLabel: string?   // null khi không có kỳ trước
  Up: number
  Flat: number
  Down: number
  Done: number
  TotalCriteria: number          // mẫu số "x/N" — LUÔN = tổng Criteria active (62), không đổi theo kỳ
  ```
  `DashboardGroupProgressDto`: `{ GroupId: guid, GroupCode: string, GroupName: string, Progress: number? }`
  `DashboardTrendPointDto`: `{ Label: string, Value: number? }`
  — `mode=week`: Label = `"YYYY-Www"` (mọi tuần ISO CÓ dữ liệu trong `year`);
    `mode=month|year`: Label = `"Th.{1..12}"` (mọi tháng CÓ dữ liệu trong `year`).
    Chỉ trả điểm CÓ dữ liệu — không nội suy/không trả điểm null.
  `DashboardTableRowDto`:
  ```
  CriteriaId: guid
  Code: string
  Name: string
  GroupCode: string
  GroupName: string
  MaxScore: number
  PreviousValue: number?    // null nếu không có kỳ trước hoặc chỉ tiêu không có dữ liệu ở kỳ trước
  CurrentValue: number?      // null = "chưa có dữ liệu" ở kỳ đang xem
  Delta: number?              // null nếu 1 trong 2 giá trị trên null
  Badge: string?               // "Hoàn thành" | "Không tăng" | "Đang thực hiện" | "Chưa có dữ liệu" — TÍNH RUNTIME theo statusFor(), KHÔNG phải field Status lưu DB (khớp doc/ERD/ERD.md mục 4)
  ```
  **Khác DRAFT**: không có field `Note` riêng trong Table row (Note đọc qua
  `GET /api/criteria` ở màn Danh mục DTI, Dashboard không cần lặp lại) —
  `Badge` là string tiếng Việt (`"Hoàn thành"/"Không tăng"/"Đang thực hiện"`),
  KHÔNG phải enum tiếng Anh `"Done"|"Working"|"Stalled"` như DRAFT đề xuất —
  giữ đúng nguyên văn 3 giá trị JS gốc trong `dashboard.html` (`statusFor()`).
- Response thật đã gọi (curl, `mode=week`, rút gọn):
  ```json
  {"Success":true,"Data":{"Mode":"week","PeriodLabel":"Tuần 33/2026 (10/08–16/08/2026)","Kpi":{"OverallProgress":81.91,"Delta":null,"PreviousPeriodLabel":null,"Up":0,"Flat":0,"Down":0,"Done":39,"TotalCriteria":62},"Groups":[{"GroupId":"...","GroupCode":"1","GroupName":"Hạ tầng và Nền tảng số","Progress":72.475}, ...6 nhóm],"Trend":[...],"Table":[...62 dòng...]},"ErrorCode":null,"ErrorMessage":null,"TraceId":"..."}
  ```
  (`mode=month`, `mode=year` cũng đã verify — response shape giống hệt, chỉ
  khác `Mode`/`PeriodLabel`/công thức tổng hợp bên trong.)
- Công thức: `spec/dashboard-dti-weekly/business-rules.md` mục 3.3/3.4 (tuần,
  không carry-forward) + `spec/danh-muc-dti/business-rules.md` mục 2.4/3
  (tháng/năm = trung bình cộng các kỳ-tuần có dữ liệu). Implement tập trung ở
  `AggregationService.ComputePeriodAggregate` — 1 hàm thuần cho cả 3 cấp độ.

## CONTRACT DB-2 — "Xuất báo cáo" (text/HTML báo cáo nhanh)

- Status: **IMPLEMENTED** — [MỚI so với DRAFT, xem "Khác biệt so với DRAFT"]
- Route: `GET /api/dashboard/report`
- Query params: giống hệt DB-1 (`mode`, `date`, `year`)
- Response: `ApiResponse<ReportResponseDto>`
  ```
  Title: string          // vd "Báo cáo tiến độ DTI — theo tuần"
  ContentHtml: string     // HTML sẵn sàng render (tương đương innerHTML của #reportBox trong dashboard.html gốc)
  ```
- Response thật đã gọi (`mode=week`):
  ```json
  {"Success":true,"Data":{"Title":"Báo cáo tiến độ DTI — theo tuần","ContentHtml":"<b>BÁO CÁO NHANH TIẾN ĐỘ CHỈ SỐ CHUYỂN ĐỔI SỐ</b><br>Kỳ cập nhật: <b>Tuần 33/2026 (10/08–16/08/2026)</b>.<br><br>\nTiến độ chung hiện đạt <b>81.91%</b>.\nCó <b>0 chỉ tiêu tăng</b>, <b>0 chỉ tiêu không thay đổi</b>, <b>0 chỉ tiêu giảm</b> và <b>39/62 chỉ tiêu hoàn thành 100%</b>.<br><br>\n<b>Chỉ tiêu tăng nhiều:</b> Chưa có.<br><br>\n<b>Chỉ tiêu chưa tăng cần chú ý:</b> Không có."},"ErrorCode":null,"ErrorMessage":null,"TraceId":"..."}
  ```
- FE chỉ cần bind `ContentHtml` thẳng vào dialog báo cáo (`[innerHTML]` hoặc
  tương đương Angular an toàn — nhớ dùng `DomSanitizer` vì có thẻ `<b>`/`<br>`).
  BE tự tính text báo cáo (mirror đúng `generateReport()`/`generateMonthlyReport()`/
  `generateYearAggregateReport()` của `dashboard.html` gốc) — FE **không cần**
  tự dựng lại text từ `Kpi`/`Table` như DRAFT đề xuất.

## CONTRACT DB-3 — Danh sách năm/kỳ-tuần/kỳ-tháng có dữ liệu

- Status: **IMPLEMENTED** — dùng CHUNG route với Danh mục DTI, xem
  `doc/contracts/danh-muc-dti.md` mục **CONTRACT DM-8**
  (`GET /api/dashboard/periods?year=`) — thay thế cả 3 route riêng DB-2/DB-3/DB-4
  mà bản DRAFT đề xuất tách rời (`/years`, `/weeks`, `/history`). Không lặp lại
  chi tiết ở đây, xem DM-8.

---

## Khác biệt so với bản DRAFT của frontend-expert — vì sao sửa

| DRAFT | Đã đổi thành | Lý do |
| --- | --- | --- |
| `GET /api/dashboard/summary` | `GET /api/dashboard` | Route được `main` chỉ định trực tiếp trong yêu cầu gốc |
| Không có endpoint report riêng — FE tự tính text từ DB-1 | `GET /api/dashboard/report` (endpoint riêng, BE tính sẵn `ContentHtml`) | Route được `main` chỉ định trực tiếp — BE tính để khớp 100% với 3 hàm `generateReport()`/`generateMonthlyReport()`/`generateYearAggregateReport()` gốc, tránh FE phải chép lại logic dựng text |
| `StatusBadge: "Done"\|"Working"\|"Stalled"` | `Badge: "Hoàn thành"\|"Không tăng"\|"Đang thực hiện"\|"Chưa có dữ liệu"` | Giữ nguyên 3 giá trị tiếng Việt gốc từ `statusFor()` trong `dashboard.html`, không dịch sang enum tiếng Anh (đỡ 1 bước map ở FE) |
| 3 route riêng `/years`, `/weeks`, `/history` | 1 route `GET /api/dashboard/periods` (dùng chung với Danh mục DTI, xem DM-8) | Tránh trùng lặp logic — cả 2 màn cùng cần "năm/kỳ nào có dữ liệu" |
| `UpCount`/`FlatCount` nullable ở Mode=Year | `Up`/`Flat`/`Down`/`Done` luôn là số nguyên (0 nếu không áp dụng) | Đơn giản hoá — mode=year vẫn có "kỳ liền trước" (năm trước có dữ liệu) nên khái niệm up/down/flat vẫn tính được, không cần null hoá riêng cho Year |

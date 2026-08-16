# API Contract — Dashboard DTI Weekly (`modules/dashboard`)

> Owner FE: `src/FE/src/app/modules/dashboard/services/dashboard.service.ts` +
> `src/FE/src/app/shared/services/period-options.service.ts`
> Nguồn nghiệp vụ: `doc/ke-hoach-xay-lai-corebase.md` (mô tả hành vi cần port lại chính xác từ
> `doc/Prototype/dashboard.html`), bản Contract Card cũ (đã xoá cùng đợt dọn `src/BE`, nội dung gốc
> vẫn còn trong lịch sử git — dùng làm nền tham khảo route/shape, ĐÃ cập nhật lại theo envelope
> mới). Dashboard **100% read-only**.
>
> **CASING**: xem cảnh báo ở `doc/contracts/menu.md` — toàn bộ DTO dưới đây giả định camelCase
> xuyên suốt (envelope + payload), CHƯA XÁC NHẬN với `backend-expert`. Đây là điểm **QUAN TRỌNG
> NHẤT cần chốt trước khi implement thật** — nếu sai, toàn bộ mapper của cả `dashboard` lẫn
> `danh-muc-dti` phải sửa lại.
>
> Trạng thái card: **DRAFT** — FE đã code service/mapper theo đúng shape dưới đây (án theo hành vi
> gốc + route đã có tiền lệ ở bản Contract Card cũ), nhưng BE `src/BE` hiện chưa có
> `Infrastructure`/`DashboardController` để implement/verify — chưa gọi thật lần nào ở đợt F0+F1
> này.

## CONTRACT DB-1 — Tổng hợp Dashboard theo Tuần/Tháng/"Tất cả trong năm"

- Status: **DRAFT**
- Route: `GET /api/dashboard`
- Query params:
  ```
  mode: string        // "week" | "month" | "year" ("year" = "Tất cả" của 1 năm) — chữ thường
  date: date?          // mode=week: ngày bất kỳ trong tuần muốn xem; mode=month: ngày bất kỳ trong tháng.
                        // Bỏ trống = server tự dùng kỳ hiện tại (hôm nay) — FE dựa vào default này
                        // cho trạng thái mặc định khi mới vào trang (xem dashboard.page.ts).
  year: int?            // mode=year: năm cần tổng hợp (mặc định năm hiện tại nếu bỏ trống);
                        // mode=week/month vẫn nên nhận year để BE build đúng Trend theo năm đang chọn
  ```
- Response: `IApiResult<IDashboardAggregateDto>`
  ```
  data: {
    mode: "week"|"month"|"year",
    periodLabel: string,        // vd "Tuần 33/2026 (10/08–16/08/2026)", "Tháng 8/2026", "Năm 2026"
    kpi: {
      overallProgress: number|null, delta: number|null, previousPeriodLabel: string|null,
      up: int, flat: int, down: int, done: int, totalCriteria: int
    },
    groups: [ { groupId: guid, groupCode: string, groupName: string, progress: number|null } ],
    trend: [ { label: string, value: number|null } ],
      // mode=week: label="YYYY-Www" mọi tuần ISO CÓ dữ liệu trong `year`; mode=month|year:
      // label="Th.{1..12}". CHỈ trả điểm CÓ dữ liệu — KHÔNG nội suy/không trả điểm null (FE
      // `trend-chart` clamp [0,100] + không spanGaps, xem doc/huong_dan/wiki-core/fe/12-charting.md).
    table: [
      {
        criteriaId: guid, code: string, name: string, groupCode: string, groupName: string,
        maxScore: number, previousValue: number|null, currentValue: number|null, delta: number|null,
        badge: "Hoàn thành"|"Không tăng"|"Đang thực hiện"|"Chưa có dữ liệu"|null,
        note: string|null
      }
    ]
  }
  ```
- **Khác bản Contract Card cũ (đã xoá)**: cũ ghi "Table row KHÔNG có field `note`" — bản DRAFT này
  THÊM lại `note` vì `doc/Prototype/dashboard.html` (nguồn sống hiện tại, cột "Ghi chú tuần" trong
  bảng 62 chỉ tiêu) vẫn hiển thị ghi chú ngay trong bảng đọc-only — port lại đúng 1:1 theo yêu cầu
  gốc của task lượt này. `backend-expert` xác nhận lại field này có sẵn trong dữ liệu tổng hợp hay
  cần JOIN thêm.
- `badge` tính RUNTIME phía BE (epsilon so sánh Delta = `0.001`, ngưỡng "Hoàn thành" =
  `currentValue >= 99.999`) — FE chỉ hiển thị, không tự tính lại (xem
  `modules/dashboard/components/status-badge/`).
- Công thức: trung bình gia quyền theo `MaxScore`; Tháng/"Tất cả trong năm" = trung bình cộng các
  kỳ-tuần CÓ dữ liệu, KHÔNG carry-forward (kỳ không có thao tác cho 1 chỉ tiêu bị loại khỏi mẫu
  tính trung bình của chỉ tiêu đó).

## CONTRACT DB-2 — "Xuất báo cáo" (HTML báo cáo nhanh)

- Status: **DRAFT**
- Route: `GET /api/dashboard/report`
- Query params: giống hệt DB-1 (`mode`, `date`, `year`)
- Response: `IApiResult<IReportDto>`
  ```
  data: { title: string, contentHtml: string }
  ```
- FE (`report-dialog`) bind `contentHtml` qua `DomSanitizer.bypassSecurityTrustHtml` rồi
  `[innerHTML]` — BE tự tính sẵn HTML (tương đương `generateReport()`/`generateMonthlyReport()`/
  `generateYearAggregateReport()` trong `doc/Prototype/dashboard.html`), FE không tự dựng lại text.

## CONTRACT DB-3 — Danh sách Năm/Kỳ có dữ liệu (dùng CHUNG với Danh mục DTI)

- Status: **AGREED** (2026-08-16)
- Route: `GET /api/dashboard/periods`
- Query params: `year: int?` (nếu có, trả thêm `weeksInYear`/`monthsInYear` của đúng năm đó)
- Response: `IApiResult<IPeriodOptionsDto>`
  ```
  data: {
    years: int[],                 // mọi năm có dữ liệu, LUÔN kèm năm hiện tại dù chưa có dữ liệu
    weeksInYear: [ { value: string, date: date, overallProgress: number|null } ],
    monthsInYear: [ { value: string, date: date, overallProgress: number|null } ]
  }
  ```
  `value` là giá trị cần truyền vào `period` của `GET /api/criteria` (xem
  `doc/contracts/danh-muc-dti.md` CONTRACT DM-2) và `date`/`mode` của `GET /api/dashboard` — vd
  `"2026-W33"` (tuần ISO), `"2026-08"` (tháng).
- Owner FE thật sự: `shared/services/period-options.service.ts` — dùng chung bởi
  `modules/dashboard` (`period-toolbar`, `history-list`) VÀ `modules/danh-muc-dti`
  (dropdown năm/kỳ lọc grid) — đặt ở `shared/` theo đúng quy tắc "≥2 feature dùng thì không còn ở
  `modules/<feature>/`" (`src/FE/.claude/docs/architecture.md`).
- `history-list` (Dashboard) KHÔNG có endpoint riêng — tái dùng `weeksInYear` của route này, tự
  tính delta giữa các kỳ liền kề ở FE (không cần BE trả sẵn).

> ✅ **Đã sửa (2026-08-16, backend-expert)** — `DashboardPeriodsDto` nay trả đúng
> `{years, weeksInYear, monthsInYear}` với `weeksInYear`/`monthsInYear` là mảng object
> `{value, date, overallProgress}` (khớp `IPeriodOptionsDto` phía FE, không phải mảng giá trị
> thuần như trước). `overallProgress` mỗi tuần/tháng tính qua `PeriodAggregateCalculator.Compute`
> (cùng công thức bình quân gia quyền theo `MaxScore` dùng cho DB-1). `years` LUÔN kèm năm hiện
> tại dù chưa có dữ liệu. Build xanh — chưa gọi thử được response THÀNH CÔNG có data thật (cần
> DB đã migrate), CONTRACT DB-3 chuyển **AGREED** (đứng riêng, không phụ thuộc trạng thái DRAFT
> chung của DB-1/DB-2 trong file này).

## Trạng thái hiện tại phía FE

3 endpoint trên đã có service/mapper hoàn chỉnh (`dashboard.service.ts`,
`shared/services/period-options.service.ts`), UI đầy đủ (period-toolbar, kpi-summary, trend-chart
qua `p-chart`, group-progress-list, criteria-table qua `p-table` với lọc/sắp xếp/phân trang
CLIENT-SIDE, history-list, report-dialog) — `ng build` xanh. Chưa gọi được thật vì `src/BE` chưa có
`DashboardController`. Khi `backend-expert` có endpoint thật: xác nhận lại casing (xem cảnh báo đầu
file) bằng 1 lần gọi thật/Swagger trước khi chuyển card sang AGREED.

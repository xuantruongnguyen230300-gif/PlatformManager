# API Contract — Danh mục DTI (`modules/danh-muc-dti`)

> Owner FE: `src/FE/src/app/modules/danh-muc-dti/services/danh-muc-dti.service.ts`
> Nguồn nghiệp vụ: `spec/danh-muc-dti/business-rules.md`, `spec/danh-muc-dti/ui-spec.md`.
> Tất cả request/response **PascalCase, FLAT** (không bọc `{ Request: {...} }`,
> không bọc `{ data: {...} }`) — theo `src/FE/.claude/docs/api-client.md`.
> Trạng thái card: **IMPLEMENTED** — đã code, build sạch, migrate + seed local,
> đã tự smoke-test bằng curl (chi tiết response thật xem từng mục bên dưới).
>
> **backend-expert đã SỬA các route/shape khác với bản DRAFT ban đầu của
> frontend-expert** — người giao việc (`main`) đã chỉ định chính xác các route
> này trong yêu cầu gốc, nên ưu tiên hơn bản DRAFT tự đề xuất. Đọc kỹ mục
> "Khác biệt so với DRAFT" ở cuối file trước khi cập nhật `DanhMucDtiService`.

## CONTRACT DM-1 — Danh sách nhóm chỉ tiêu (dropdown "Nhóm")

- Status: **IMPLEMENTED**
- Route: `GET /api/criteria-groups`
- Response: `ApiResponse<CriteriaGroupDto[]>`
  ```
  Id: guid           // [MỚI so với DRAFT] cần để dùng làm GroupId khi Create/Update Criteria
  Code: string        // "1".."6"
  Name: string
  DisplayOrder: int
  ```
- Response thật đã gọi (curl, rút gọn):
  ```json
  {"Success":true,"Data":[{"Id":"1ea0ff47-...","Code":"1","Name":"Hạ tầng và Nền tảng số","DisplayOrder":1}, ...6 nhóm],"ErrorCode":null,"ErrorMessage":null,"TraceId":"..."}
  ```
- Ghi chú: dữ liệu tĩnh (6 nhóm seed sẵn từ CSV) — FE cache trong service.

## CONTRACT DM-2 — Lưới "Danh mục & Đánh giá theo tuần" (đọc, có phân trang)

- Status: **IMPLEMENTED**
- Route: `GET /api/criteria` *(KHÔNG phải `/api/criteria/grid` như DRAFT — xem
  "Khác biệt so với DRAFT")*
- Query params:
  ```
  year: int?               // mặc định = năm hiện tại nếu bỏ trống
  period: string?          // "all" (mặc định) | "YYYY-Www" (tuần ISO, vd "2026-W33") | "YYYY-MM" (tháng, vd "2026-08")
  groupId: guid?
  search: string?          // khớp Code + " " + Name, không phân biệt hoa/thường — BE tự lọc
  page: int = 1
  pageSize: int = 20        // tối đa 500
  ```
  **Format `period` KHÁC bản DRAFT** (DRAFT dùng `PeriodDate: date?` — 1 ngày cụ
  thể). Model dữ liệu thật (đã chốt ở `doc/ERD/ERD.md`) không có khái niệm "1
  ngày cụ thể" làm đơn vị lọc — chỉ có tuần ISO / tháng / "Tất cả (của 1 năm)".
  Lấy danh sách `period` hợp lệ cho dropdown qua `GET /api/dashboard/periods?year=`
  (xem contract mới DM-8 bên dưới, cùng dùng cho cả 2 màn).
- Response: `ApiResponse<PagedResult<CriteriaRowDto>>`
  ```
  PagedResult<T>: { Items: T[], Page: int, PageSize: int, TotalCount: int, TotalPages: int }
  ```
  `CriteriaRowDto`:
  ```
  CriteriaId: guid
  Code: string
  Name: string
  GroupId: guid
  GroupCode: string
  GroupName: string
  MaxScore: number
  AssessmentId: guid?       // [MỚI] null nếu chỉ tiêu chưa có dữ liệu ở phạm vi đang xem
  ProgressPercent: number?  // null = "—" (KHÔNG carry-forward)
  SelfScore: number?
  VerifiedScore: number?
  Diff: number?              // [MỚI] SelfScore - VerifiedScore, BE tính sẵn (= cột "Chênh lệch" CSV)
  Status: string?
  OwnerId: guid?
  OwnerName: string?         // resolve từ AppUsers.FullName, null nếu OwnerId null hoặc không khớp user
  Deadline: date?
  Note: string?
  AssessmentDate: date?      // [MỚI] ngày (CreatedAt) của bản ghi resolve được — "kỳ" của dòng này
  Evidences: { Id: guid, Content: string, OrderIndex: int }[]
  IsEditable: bool           // true CHỈ KHI year = năm hiện tại VÀ period = "all" (trạng thái "Live")
  ```
- **Hành vi theo `period` — QUAN TRỌNG, khác giả định DRAFT:**
  - **`period` = tuần/tháng cụ thể** → đúng **62 dòng** (1 dòng/Criteria active),
    giá trị resolve = record `CreatedAt` lớn nhất trong đúng phạm vi đó (nếu
    không có record nào trong phạm vi → mọi field đánh giá `null`).
  - **`period` = "all" (mặc định)** → **KHÔNG** phải "mỗi Criteria lấy dữ liệu
    kỳ-ngày gần nhất" như DRAFT giả định — đây là quyết định đã **chốt** ở
    `doc/ERD/ERD.md` mục "Kỳ (tuần/tháng/năm)": hiển thị **toàn bộ bản ghi
    CriteriaAssessment trong năm đó** (có thể **nhiều dòng/1 Criteria** nếu
    Criteria đó được sửa nhiều ngày khác nhau trong năm). `TotalCount` khi đó
    là tổng số **bản ghi**, không phải tổng số Criteria.
    - **Bổ sung của backend-expert (ngoài spec gốc, để tránh grid trống hoàn
      toàn trên hệ thống mới)**: Criteria active **chưa từng** có bất kỳ
      `CriteriaAssessment` nào (ở mọi năm) vẫn luôn xuất hiện 1 dòng
      placeholder (mọi field đánh giá = `null`) trong "all", để tab CRUD luôn
      thấy đủ danh mục dù chưa ai nhập liệu. Criteria có dữ liệu ở **năm
      khác** nhưng không có dữ liệu ở năm đang xem thì **không** xuất hiện —
      đúng theo spec (đổi năm = xem đúng năm đó).
  - Chỉ tiêu đã soft-delete vẫn hiện trong `period=all` lịch sử nếu có
    `CriteriaAssessment` trong năm đó (dùng `IgnoreQueryFilters` khi join).
- Response thật đã gọi (curl, `period` tuần cụ thể, rút gọn 1 dòng):
  ```json
  {"Success":true,"Data":{"Items":[{"CriteriaId":"19a37d47-...","Code":"1.1","Name":"...","GroupId":"1ea0ff47-...","GroupCode":"1","GroupName":"Hạ tầng và Nền tảng số","MaxScore":10.00,"AssessmentId":"06d5ffba-...","ProgressPercent":55.50,"SelfScore":7.04,"VerifiedScore":10.00,"Diff":-2.96,"Status":"Đang thực hiện","OwnerId":null,"OwnerName":null,"Deadline":null,"Note":"test note","AssessmentDate":"2026-08-12","Evidences":[],"IsEditable":true}],"Page":1,"PageSize":5,"TotalCount":1,"TotalPages":1},"ErrorCode":null,"ErrorMessage":null,"TraceId":"..."}
  ```

## CONTRACT DM-3 — Tạo chỉ tiêu

- Status: **IMPLEMENTED**
- Route: `POST /api/criteria`
- Request:
  ```
  Code: string        // required, unique trong tập chưa xoá mềm, maxlength 20
  Name: string         // required
  GroupId: guid         // required, FK tồn tại
  MaxScore: number      // required, > 0
  ```
- Response: `ApiResponse<CriteriaDto>` — `{ Id, Code, Name, GroupId, GroupName, MaxScore }`
- Lỗi mong đợi: `CRITERIA_CODE_INVALID` (400) · `CRITERIA_NAME_REQUIRED` (400) ·
  `CRITERIA_MAX_SCORE_INVALID` (400) · `CRITERIA_GROUP_NOT_FOUND` (404) ·
  `CRITERIA_CODE_DUPLICATE` (409)
- Đã verify: tạo trùng `Code` → 409 đúng như thiết kế; tạo mới rồi xoá (chưa có
  lịch sử) → hard-delete thành công.

## CONTRACT DM-4 — Sửa chỉ tiêu

- Status: **IMPLEMENTED**
- Route: `PUT /api/criteria/{id}`
- Request: giống DM-3 (cho phép đổi `Code` tự do — đã chốt ở business-rules.md mục 1.2)
- Response: `ApiResponse<CriteriaDto>`
- Lỗi mong đợi: `CRITERIA_NOT_FOUND` (404) · `CRITERIA_CODE_DUPLICATE` (409) ·
  các lỗi validate 400 giống DM-3

## CONTRACT DM-5 — Xoá chỉ tiêu

- Status: **IMPLEMENTED**
- Route: `DELETE /api/criteria/{id}`
- Response: `ApiResponse<{ HardDeleted: bool }>` (true = xoá cứng vì chưa từng
  có `CriteriaAssessment`; false = đã soft-delete vì đã có lịch sử)
- Lỗi mong đợi: `CRITERIA_NOT_FOUND` (404)
- Đã verify cả 2 nhánh (hard-delete Criteria mới tạo chưa có assessment; soft-delete Criteria "1.1" đã có assessment).

## CONTRACT DM-6 — Sửa inline Tiến độ %/Ghi chú (upsert hôm nay)

- Status: **IMPLEMENTED**
- Route: `PUT /api/criteria/{id}/assessment` *(KHÔNG phải `PATCH
  /api/criteria-assessments/{criteriaId}` như DRAFT — xem "Khác biệt so với DRAFT")*
- Query params (TUỲ CHỌN — xem "Hành vi bắt buộc"):
  ```
  year: int?
  period: string?     // "all" | "YYYY-Www" | "YYYY-MM"
  ```
- Request:
  ```
  ProgressPercent: number?   // kẹp [0,100] phía BE
  Note: string?
  ```
- Response:
  ```
  CriteriaId: guid
  ProgressPercent: number
  Note: string?
  CreatedAt: date        // ngày của bản ghi vừa upsert (LUÔN là hôm nay)
  ```
- **Hành vi bắt buộc (đã trả lời câu hỏi mở của FE — chọn phương án CÓ chặn,
  không phải phương án 2 FE đề xuất)**: request LUÔN ghi vào bản ghi của HÔM
  NAY (upsert-trong-ngày + copy-forward), bất kể `year`/`period` gửi kèm là gì.
  Nhưng nếu FE **có gửi** `year`/`period` (khuyến nghị: luôn gửi đúng state
  đang xem trên UI) và giá trị đó **không phải** trạng thái "Live" (năm hiện
  tại + period="all") → BE từ chối với `409 CRITERIA_ASSESSMENT_READONLY_PERIOD`
  thay vì âm thầm ghi đè bản ghi hôm nay trong khi FE tưởng đang sửa dữ liệu
  lịch sử đang hiển thị. Đây là **defense-in-depth** theo đúng yêu cầu gốc —
  FE vẫn phải tự ẩn control sửa khi `IsEditable=false` (từ DM-2), request này
  chỉ là lớp chặn thứ 2 phía server.
- Lỗi mong đợi: `CRITERIA_NOT_FOUND` (404) · `CRITERIA_ASSESSMENT_READONLY_PERIOD` (409)
- Đã verify: upsert thành công tạo/update đúng record hôm nay; gọi lại với
  `year=2020&period=all` (không phải Live) → nhận đúng 409.

## CONTRACT DM-7 — Import CSV

- Status: **IMPLEMENTED**
- Route: `POST /api/import/csv` *(KHÔNG phải `/api/criteria-assessments/import`
  — xem "Khác biệt so với DRAFT")*, `multipart/form-data`, field tên **`file`**
  (chữ thường, khớp tham số `IFormFile file` phía BE)
- Response: `ApiResponse<CsvImportResultDto>`
  ```
  TotalRows: int
  SuccessCount: int
  ErrorCount: int
  CriteriaCreatedCount: int   // [MỚI] số Criteria mới tự tạo do gặp Code lạ
  Errors: CsvImportRowErrorDto[]
  ```
  `CsvImportRowErrorDto`: `{ RowNumber: int, Code: string?, Message: string }`
  **Đơn giản hơn DRAFT**: không có khái niệm `Outcome: Warning` / `WarningCount`
  riêng (cross-check "Chênh lệch" không implement ở bản demo này) — chỉ có
  Success/Error nhị phân theo từng dòng.
- Mapping cột: đúng `spec/danh-muc-dti/business-rules.md` mục 2.2. `Code` lạ →
  tự tạo `Criteria` mới. Nhóm lạ (không khớp `CriteriaGroup.Name` nào) → lỗi
  dòng đó, không tự tạo nhóm. `CreatedAt` = ngày hệ thống lúc import.
  "Phụ trách" → match chính xác `AppUsers.FullName`; không khớp/trùng tên →
  `OwnerId=null` (không tự tạo `AppUser`).
- Lỗi mong đợi: `IMPORT_FILE_EMPTY` (400) khi không có file/file rỗng.
- Đã verify: import trực tiếp `doc/ERD/example_db_ver1.csv` → `{"TotalRows":62,"SuccessCount":62,"ErrorCount":0,"CriteriaCreatedCount":0,"Errors":[]}`
  (chạy sau khi đã seed nên toàn bộ Code đã tồn tại — ghi đè theo file). Minh
  chứng nhiều dòng (mã "1.4", "2.3"...) tách đúng theo tiền tố "*".

## CONTRACT DM-8 — Danh sách Năm/Kỳ có dữ liệu (cho 2 dropdown lọc) [MỚI — ngoài yêu cầu gốc]

- Status: **IMPLEMENTED**
- Route: `GET /api/dashboard/periods` *(đặt ở `DashboardController` — dùng
  CHUNG cho cả 2 màn Dashboard và Danh mục DTI, thay cho 2 route riêng
  `/api/criteria-assessments/periods` + `/api/dashboard/years` +
  `/api/dashboard/weeks` mà 2 bản DRAFT đề xuất riêng lẻ)*
- Query params: `year: int?` (nếu có, trả thêm `WeeksInYear`/`MonthsInYear`)
- Response: `ApiResponse<PeriodOptionsDto>`
  ```
  Years: int[]                      // mọi năm có dữ liệu, luôn kèm năm hiện tại
  WeeksInYear: PeriodOptionDto[]     // rỗng nếu không truyền year
  MonthsInYear: PeriodOptionDto[]    // rỗng nếu không truyền year
  ```
  `PeriodOptionDto`: `{ Value: string, Date: date, OverallProgress: number? }`
  — `Value` chính là giá trị cần truyền vào query `period` của `GET /api/criteria`
  và `GET /api/dashboard` (vd `"2026-W33"`, `"2026-08"`).
- Response thật đã gọi: `{"Success":true,"Data":{"Years":[2026],"WeeksInYear":[{"Value":"2026-W33","Date":"2026-08-10","OverallProgress":82.19}],"MonthsInYear":[{"Value":"2026-08","Date":"2026-08-01","OverallProgress":82.19}]},...}`
- Lý do bổ sung: cả DM-2 (`period`) lẫn `GET /api/dashboard` (`mode=week|month`)
  đều cần danh sách kỳ hợp lệ để đổ dropdown — yêu cầu gốc không liệt kê
  endpoint này nhưng không có cách nào FE tự suy ra được nếu thiếu.

---

## Khác biệt so với bản DRAFT của frontend-expert — vì sao sửa

Người giao việc (`main`) đã chỉ định **chính xác** danh sách route cho
`backend-expert` trong yêu cầu gốc của task này — ưu tiên hơn route tự đề
xuất trong DRAFT. Tóm tắt các điểm đã đổi:

| DRAFT | Đã đổi thành | Lý do |
| --- | --- | --- |
| `GET /api/criteria/grid` | `GET /api/criteria` | Route được chỉ định trực tiếp trong yêu cầu gốc |
| `PATCH /api/criteria-assessments/{criteriaId}` | `PUT /api/criteria/{id}/assessment` | Route + verb được chỉ định trực tiếp |
| `POST /api/criteria-assessments/import` | `POST /api/import/csv` | Route được chỉ định trực tiếp |
| `PeriodDate: date?` (1 ngày cụ thể) | `period: string?` ("all"\|"YYYY-Www"\|"YYYY-MM") | Model dữ liệu đã chốt không có khái niệm lọc theo 1 ngày tuỳ ý — chỉ có tuần ISO/tháng/"Tất cả của 1 năm" |
| "Tất cả" = mỗi Criteria lấy kỳ-ngày gần nhất trong năm | "Tất cả" = TOÀN BỘ bản ghi trong năm (nhiều dòng/1 Criteria) | Quyết định đã CHỐT ở `doc/ERD/ERD.md` (2026-08-12 vòng 2) — khác đề xuất ban đầu |
| Không có endpoint years/periods riêng cho Danh mục DTI | `GET /api/dashboard/periods` (dùng chung) | Cả 2 màn cùng cần, gộp 1 endpoint tránh trùng logic |

Câu hỏi DM-6 của FE ("BE có nên tự chặn khi không phải hôm nay?") — đã trả
lời: **CÓ chặn** (409 `CRITERIA_ASSESSMENT_READONLY_PERIOD`) khi FE gửi kèm
`year`/`period` không phải trạng thái Live — khác phương án 2 FE nghiêng về.

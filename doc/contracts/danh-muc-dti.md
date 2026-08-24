# API Contract — Danh mục DTI (`modules/danh-muc-dti`)

> Owner FE: `src/FE/src/app/modules/danh-muc-dti/services/danh-muc-dti.service.ts`
> Nguồn nghiệp vụ: `doc/ke-hoach-xay-lai-corebase.md` (hành vi cần port lại chính xác từ
> prototype danh-muc-dti, đã xoá 2026-08-23), bản Contract Card cũ (đã xoá, tham khảo qua git history) —
> route/shape kế thừa gần như nguyên vẹn, chỉ đổi lại envelope + casing.
>
> **CASING**: xem cảnh báo ở `doc/contracts/meta-menu.md` — camelCase xuyên suốt (envelope + payload),
> CHƯA XÁC NHẬN với `backend-expert`.
>
> Trạng thái card: **DRAFT** — FE đã code service/mapper/UI đầy đủ theo shape dưới đây, chưa gọi
> được thật vì `src/BE` hiện chưa có `Infrastructure`/`CriteriaController`.

## CONTRACT DM-1 — Danh sách nhóm chỉ tiêu (dropdown "Nhóm")

- Status: **DRAFT**
- Route: `GET /api/criteria-groups`
- Response: `IApiResult<ICriteriaGroupDto[]>`
  ```
  data: [ { id: guid, code: string, name: string, displayOrder: int } ]
  ```
- Dữ liệu tĩnh (6 nhóm seed sẵn từ CSV dữ liệu mẫu CSV (`doc/ERD/` đã xoá 2026-08-23, sẽ bổ sung lại sau)) — FE cache trong
  `DanhMucDtiPage` (load 1 lần lúc khởi tạo).

## CONTRACT DM-2 — Lưới "Danh mục & Đánh giá theo tuần" (đọc, phân trang server-side)

- Status: **DRAFT**
- Route: `GET /api/criteria`
- Query params:
  ```
  year: int?              // mặc định năm hiện tại nếu bỏ trống
  period: string?         // "all" (mặc định) | "YYYY-Www" (tuần ISO) | "YYYY-MM" (tháng)
  groupId: guid?
  search: string?         // khớp Code + " " + Name, không phân biệt hoa/thường, BE tự lọc — FE debounce 300ms trước khi gọi
  page: int = 1
  pageSize: int = 20       // tối đa gợi ý 500
  ```
- Response: `IApiResult<PagedList<ICriteriaRowDto>>`
  ```
  PagedList<T>: { items: T[], page: int, pageSize: int, totalCount: int }
  ```
  `ICriteriaRowDto`:
  ```
  criteriaId: guid, code: string, name: string, groupId: guid, groupCode: string, groupName: string,
  maxScore: number, assessmentId: guid|null, progressPercent: number|null, selfScore: number|null,
  verifiedScore: number|null, diff: number|null, status: string|null, ownerId: guid|null,
  ownerName: string|null, deadline: date|null, note: string|null, assessmentDate: date|null,
  evidences: [ { id: guid, content: string, orderIndex: int } ],
  isEditable: bool   // true CHỈ KHI year=năm hiện tại VÀ period="all" (trạng thái "Live")
  ```
- Hành vi theo `period` (kế thừa nguyên bản Contract Card cũ, đã verify thật (2026-08-16) ở lần build trước khi
  bị xoá — tin cậy cao dù chưa re-verify ở đợt này):
  - `period` = tuần/tháng cụ thể → đúng N dòng (1 dòng/`Criteria` active), giá trị resolve = record
    có `AssessmentDate` lớn nhất trong đúng phạm vi đó (không có record nào trong phạm vi → mọi
    field đánh giá `null`).
  - `period` = `"all"` (mặc định) → **KHÔNG** phải "mỗi Criteria lấy dữ liệu gần nhất" — hiển thị
    **toàn bộ bản ghi `CriteriaAssessment` trong năm đó** (có thể nhiều dòng/1 Criteria nếu sửa
    nhiều ngày khác nhau trong năm). `totalCount` khi đó là tổng số **bản ghi**, không phải tổng số
    Criteria. Criteria active chưa từng có `CriteriaAssessment` nào vẫn hiện 1 dòng placeholder
    (mọi field đánh giá `null`) để danh mục luôn đủ, không rỗng hoàn toàn trên hệ thống mới.
- FE (`criteria-grid-table`) đọc `isEditable` PER ROW để bật/tắt inline-edit — không tự suy luận
  lại "đang xem Live" ở tầng UI cho quyết định cho phép sửa/không (chỉ dùng để ẩn/hiện banner +
  nút Import/+Thêm ở `danh-muc-dti.page.ts`, xem `isLive` computed ở đó).

## CONTRACT DM-3 — Tạo chỉ tiêu

- Status: **DRAFT**
- Route: `POST /api/criteria`
- Request (FLAT, camelCase — xem cảnh báo casing đầu file):
  ```
  code: string      // required, unique trong tập chưa xoá mềm, maxlength 20
  name: string       // required
  groupId: guid        // required, FK tồn tại
  maxScore: number      // required, > 0
  ```
- Response: `IApiResult<ICriteriaDto>` — `{ id, code, name, groupId, groupName, maxScore }`
- Lỗi mong đợi: `CRITERIA.CODE_INVALID` (400) · `CRITERIA.NAME_REQUIRED` (400) ·
  `CRITERIA.MAX_SCORE_INVALID` (400) · `CRITERIA.GROUP_NOT_FOUND` (404) ·
  `CRITERIA.DUPLICATE_CODE` (409) — `businessCode` dạng `"{ENTITY}.{ERROR}"` theo
  `doc/huong_dan/quy-uoc/be-api-controller.md`.
- FE validate client trước (code≤20/tên bắt buộc/nhóm bắt buộc/điểm>0,
  `criteria-form-dialog.ts`), lỗi server hiển thị qua `err.apiResult?.message` (1 dòng lỗi chung
  trong dialog, không bind field-by-field ở bản F1 này — đủ dùng vì form chỉ 4 trường).

## CONTRACT DM-4 — Sửa chỉ tiêu

- Status: **DRAFT**
- Route: `PUT /api/criteria/{id}`
- Request: giống DM-3
- Response: `IApiResult<ICriteriaDto>`
- Lỗi mong đợi: `CRITERIA.NOT_FOUND` (404) · `CRITERIA.DUPLICATE_CODE` (409) · lỗi validate 400
  giống DM-3

## CONTRACT DM-5 — Xoá chỉ tiêu

- Status: **DRAFT**
- Route: `DELETE /api/criteria/{id}`
- Response: `IApiResult<IDeleteCriteriaResultDto>` — `{ hardDeleted: bool }` (true = xoá cứng vì
  chưa từng có `CriteriaAssessment`; false = đã soft-delete vì đã có lịch sử)
- Lỗi mong đợi: `CRITERIA.NOT_FOUND` (404)
- FE (`danh-muc-dti.page.ts` → `onDeleteRow`) tự đoán trước "sẽ hard hay soft delete" dựa vào
  `row.AssessmentId !== null` (CHỈ đúng cho dòng đang xem, không phản ánh chắc chắn TOÀN BỘ lịch sử
  nhiều năm của Criteria đó) để hiện đúng câu xác nhận — chỉ ảnh hưởng câu chữ hiển thị, hành vi xoá
  thật vẫn do BE quyết định qua response `hardDeleted`.

## CONTRACT DM-6 — Sửa inline Tiến độ %/Ghi chú (upsert hôm nay)

- Status: **DRAFT**
- Route: `PUT /api/criteria/{id}/assessment`
- Query params (FE luôn gửi kèm state đang xem trên UI):
  ```
  year: int?
  period: string?     // "all" | "YYYY-Www" | "YYYY-MM"
  ```
- Request:
  ```
  progressPercent: number|null   // BE kẹp [0,100]; FE cũng tự kẹp trước khi gửi (defense in depth)
  note: string|null
  ```
- **FE LUÔN gửi cả 2 field cùng lúc** (kể cả khi chỉ sửa 1 trong 2) — lấy giá trị hiện tại của field
  còn lại từ `row` đang có trong bộ nhớ, để tránh vô tình null-hoá field không được sửa nếu BE xử lý
  request như "ghi đè toàn bộ", không phải "patch từng phần". `backend-expert` xác nhận lại ngữ
  nghĩa PATCH vs PUT-toàn-phần khi review card này.
- Response: `IApiResult<{ criteriaId: guid, progressPercent: number, note: string|null, createdAt: date }>`
- Hành vi bắt buộc (kế thừa quyết định đã chốt ở bản cũ): request LUÔN ghi vào bản ghi HÔM NAY
  (upsert-trong-ngày + copy-forward các field khác từ bản ghi gần nhất), bất kể `year`/`period` gửi
  kèm. Nếu `year`/`period` gửi kèm KHÔNG phải trạng thái Live (năm hiện tại + `period="all"`) → BE
  từ chối `409 CRITERIA.ASSESSMENT_READONLY_PERIOD` — lớp chặn thứ 2 phía server, FE đã tự ẩn control
  sửa khi `row.IsEditable=false` (lớp chặn thứ 1).
- Lỗi mong đợi: `CRITERIA.NOT_FOUND` (404) · `CRITERIA.ASSESSMENT_READONLY_PERIOD` (409)

## CONTRACT DM-7 — Import CSV/Excel (chạy nền qua Hangfire)

- Status: **DRAFT** (quay lại DRAFT 2026-08-17 — đổi shape từ đồng bộ sang
  job nền + polling, kèm mở rộng định dạng file; card cũ mô tả version đồng
  bộ CSV-only đã lỗi thời, xem lý do ở
  `doc/huong_dan/quy-uoc/be-cqrs-handler.md` §"Command chạy lâu → job nền")
- **Bước 1 — bắt đầu import**: `POST /api/import`, `multipart/form-data`,
  field tên `file` — chấp nhận `.csv`/`.xlsx`/`.xls` (không còn riêng
  `/api/import/csv`). Trả ngay, KHÔNG đợi xử lý xong:
  ```
  IApiResult<{ jobId: guid }>
  ```
  ⚠️ Sửa 2026-08-24: HTTP status là **200** (không phải 202 Accepted như bản
  trước) — `ApiControllerBase.HandleResult<T>` map MỌI response thành công
  (kể cả "đã bắt đầu, chưa xử lý xong") về 200, đúng dispatcher chung áp dụng
  cho toàn bộ API (xem `src/BE/PlatformManager.Api/Common/ApiControllerBase.cs`).
  "Đã bắt đầu chứ chưa xong" thể hiện ở tầng dữ liệu (`jobId` cần poll tiếp),
  không phải HTTP status — 1 endpoint tự trả 202 sẽ là ngoại lệ duy nhất phá
  quy ước "1 chỗ map HTTP" của toàn hệ thống.
- **Bước 2 — poll trạng thái**: `GET /api/import/{jobId}`
  ```
  IApiResult<{
    status: "Pending" | "Running" | "Succeeded" | "Failed",
    result: {                          // chỉ có khi status = "Succeeded"
      totalRows: int, successCount: int, errorCount: int, criteriaCreatedCount: int,
      errors: [ { rowNumber: int, code: string|null, message: string } ]
    } | null,
    errorMessage: string | null,       // chỉ có khi status = "Failed"
  }>
  ```
- Mapping cột theo CSV gốc (dữ liệu mẫu CSV (`doc/ERD/` đã xoá 2026-08-23, sẽ bổ sung lại sau)) — `Code` lạ tự tạo `Criteria` mới; nhóm
  lạ (không khớp `CriteriaGroup.Name`) → lỗi dòng đó, không tự tạo nhóm; `AssessmentDate` = ngày hệ
  thống lúc import; "Phụ trách" match chính xác `AppUsers.FullName`, không khớp → `ownerId: null`.
  **Excel**: cùng 10 cột/tên cột như CSV, đọc từ **sheet đầu tiên**, dòng 1 = header — không hỗ trợ
  nhiều sheet/merged cell ở version đầu.
- Lỗi mong đợi: `IMPORT.FILE_EMPTY` (400) khi không có file/file rỗng ở bước 1;
  bước 2 không có lỗi nghiệp vụ riêng — lỗi xử lý từng dòng nằm trong
  `result.errors`, lỗi hạ tầng (job crash) phản ánh qua `status: "Failed"` +
  `errorMessage`.
- FE: `import-dialog` (đổi tên từ `csv-import-dialog` — chọn file CSV/Excel,
  xác nhận) → `startImport()` → poll `getImportJobStatus()` → khi
  `Succeeded`/`Failed` mới mở `import-result-dialog` (hiện tổng quan + danh
  sách lỗi từng dòng, hoặc thông báo lỗi hạ tầng nếu `Failed`). Chi tiết
  pattern poll xem `doc/huong_dan/quy-uoc/fe-api-client.md` §"Long-running
  operation — poll pattern".

## CONTRACT DM-8 — Danh sách Năm/Kỳ có dữ liệu

- Status: **AGREED** (2026-08-16) — dùng CHUNG route với Dashboard, xem `doc/contracts/dashboard.md`
  CONTRACT DB-3 (`GET /api/dashboard/periods`, đã sửa đúng shape `{value,date,overallProgress}`).
  Owner FE thật:
  `shared/services/period-options.service.ts` (không đặt trong `modules/danh-muc-dti/` vì 2 feature
  cùng dùng, xem `doc/huong_dan/quy-uoc/fe-architecture.md`).

---

## Trạng thái hiện tại phía FE

Toàn bộ 8 contract trên đã có service/mapper (`danh-muc-dti.service.ts`,
`danh-muc-dti.mapper.ts`) + UI đầy đủ: `criteria-grid-table` (`p-table` scrollable, cột Mã ghim
trái/Hành động ghim phải qua `pFrozenColumn`, inline-edit double-click Tiến độ %/Ghi chú với
Enter=lưu/Escape=huỷ/auto-focus qua `appAutofocus`, chiều cao grid tính động theo viewport qua
`afterNextRender`+resize listener), `criteria-form-dialog`, `confirm-dialog`, `csv-import-dialog`,
`import-result-dialog`. Bộ lọc năm/kỳ/nhóm/search (debounce 300ms) phân trang SERVER-SIDE qua
`p-table` `[lazy]="true"`. `ng build` xanh. Chưa gọi được thật vì `src/BE` chưa có
`CriteriaController`/`ImportController`. Khi có endpoint thật: xác nhận casing (đầu file) + hành vi
PATCH-vs-PUT ở DM-6 trước khi chuyển AGREED.

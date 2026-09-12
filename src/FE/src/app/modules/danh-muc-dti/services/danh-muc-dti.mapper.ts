import {
  ICriteriaAssessmentPayload,
  ICriteriaCreated,
  ICriteriaDto,
  ICriteriaGrid,
  ICriteriaGridDto,
  ICriteriaGroup,
  ICriteriaGroupDto,
  ICriteriaRow,
  ICriteriaRowDto,
  ICriteriaWriteAccess,
  ICriteriaWritePayload,
  IDeleteCriteriaResult,
  IDeleteCriteriaResultDto,
  IImportJobStatus,
  IImportJobStatusDto,
  IImportResult,
  IImportResultDto,
  IImportRowError,
  IImportRowErrorDto,
  IInlineAssessmentPayload,
  IOwnerOption,
  IOwnerOptionDto,
  IOwnerPage,
  IOwnerPageDto,
  IMPORT_JOB_STATES,
  ImportJobState,
} from '../models/danh-muc-dti.model';

/**
 * Nơi DUY NHẤT casing đổi cho màn Danh mục DTI — camelCase (dây) → PascalCase (app).
 *
 * ## Vì sao mỗi trường nullable đều có `?? null`, không có ngoại lệ nào
 *
 * §0 của `doc/contracts/danh-muc-dti.md`: BE bật `WhenWritingNull`, nên một chỉ tiêu chưa có bản
 * ghi đánh giá về tới đây **thiếu hẳn khoá** — `dto.selfScore` là `undefined`, không phải `null`.
 * Một model khai `number | null` mà thực tế mang `undefined` không sai ở đâu cả cho tới lúc có
 * người viết `=== null`; nhánh đó im lặng không bao giờ chạy. Đây là lỗi THẬT đã xảy ra ở
 * `platform/phan-quyen/services/phan-quyen.mapper.ts`.
 *
 * Màn này là chỗ luật đó cắn mạnh nhất: **đa số** trường của một dòng lưới là nullable, và cả một
 * lưới 62 dòng "chưa import lần nào" thì mọi ô đều rơi vào ca này.
 */
export function mapCriteriaRowDto(dto: ICriteriaRowDto): ICriteriaRow {
  return {
    CriteriaId: dto.criteriaId,
    Code: dto.code,
    Name: dto.name,
    GroupId: dto.groupId,
    GroupCode: dto.groupCode,
    GroupName: dto.groupName,
    MaxScore: dto.maxScore,
    AssessmentId: dto.assessmentId ?? null,
    AssessmentDate: dto.assessmentDate ?? null,
    ProgressPercent: dto.progressPercent ?? null,
    SelfScore: dto.selfScore ?? null,
    VerifiedScore: dto.verifiedScore ?? null,
    // 🛑 KHÔNG tính lại `verifiedScore − selfScore` ở đây kể cả khi hai trường kia có mặt: epsilon
    // so sánh và quy tắc làm tròn thuộc `spec/danh-muc-dti/business-rules.md` §Công thức, và tính
    // lại là dựng nguồn sự thật thứ hai cho một con số đã đổi chiều một lần (Q25).
    Diff: dto.diff ?? null,
    Status: dto.status ?? null,
    OwnerId: dto.ownerId ?? null,
    OwnerName: dto.ownerName ?? null,
    Deadline: dto.deadline ?? null,
    Note: dto.note ?? null,
    Version: dto.version ?? null,
    AssessmentPeriod: dto.assessmentPeriod ?? null,
    AssessmentPeriodLabel: dto.assessmentPeriodLabel ?? null,
  };
}

/**
 * Khối quyền ghi cấp màn.
 *
 * `EditBlockedBy` chuẩn hoá về mảng: card bảo đảm nó **luôn có mặt** và rỗng chứ không `null`,
 * nhưng chỉ cần một bản BE trả `null` là mọi `.includes(...)` phía FE ném lỗi — và lúc đó màn
 * hình trắng chứ không phải mất một dòng chữ. `?? []` là chốt chặn rẻ nhất cho ca đó.
 *
 * Ba giá trị boolean thì **không** có đường lùi: thiếu chúng là hợp đồng đã gãy, và đoán
 * `canWrite = false` cho một người thật sự có quyền cũng sai y như đoán ngược lại. Để nguyên
 * `undefined` rơi xuống nhánh falsy của template là hành vi an-toàn-mặc-định (ẩn nút), đúng chiều
 * mà §7.1a ràng buộc 3 yêu cầu.
 *
 * `CurrentPeriod` / `CurrentPeriodLabel` (Q72) cũng **không** có đường lùi, và lý do khác hẳn: card
 * chốt chúng **không bao giờ vắng mặt**, kể cả lưới rỗng và kể cả `canWrite = false`.
 */
export function mapCriteriaWriteAccess(dto: ICriteriaGridDto): ICriteriaWriteAccess {
  return {
    CanWrite: dto.canWrite === true,
    IsEditable: dto.isEditable === true,
    EditBlockedBy: dto.editBlockedBy ?? [],
    IsCurrentYear: dto.isCurrentYear === true,
    // Q72 — LUÔN có mặt theo card, nên đọc thẳng, KHÔNG `?? null` và KHÔNG `?? ''`.
    //
    // 🛑 Một đường lùi ở đây là đường lùi SAI hướng: `''` sẽ đi tiếp vào câu băng "nhắc kỳ đích"
    // và in ra `Đang nhập cho  — tuần hiện tại.` — một câu trông bình thường, không lỗi, và sai.
    // Hợp đồng gãy thì phải lộ ra ở chỗ nó gãy, không phải lộ ra thành một dòng chữ cụt.
    CurrentPeriod: dto.currentPeriod,
    CurrentPeriodLabel: dto.currentPeriodLabel,
  };
}

export function mapCriteriaGridDto(dto: ICriteriaGridDto): ICriteriaGrid {
  return {
    Items: (dto.items ?? []).map(mapCriteriaRowDto),
    Page: dto.page,
    PageSize: dto.pageSize,
    TotalCount: dto.totalCount,
    Access: mapCriteriaWriteAccess(dto),
  };
}

export function mapCriteriaGroupDto(dto: ICriteriaGroupDto): ICriteriaGroup {
  return {
    Id: dto.id,
    Code: dto.code,
    Name: dto.name,
    DisplayOrder: dto.displayOrder,
  };
}

// ===== VÒNG 2 — đường GHI =====

/**
 * DM-3 — response của `POST /api/criteria`.
 *
 * Nó **không** phải một dòng lưới: không có `progressPercent`, không có `assessmentPeriod`, không
 * có `version`. Đó chính là lý do Q49 bắt FE **tải lại lưới** sau khi thêm, thay vì chèn một dòng
 * nửa vời rồi để người dùng nhìn một hàng thiếu số.
 */
export function mapCriteriaDto(dto: ICriteriaDto): ICriteriaCreated {
  return {
    Id: dto.id,
    Code: dto.code,
    Name: dto.name,
    GroupId: dto.groupId,
    GroupName: dto.groupName,
    MaxScore: dto.maxScore,
  };
}

export function mapDeleteCriteriaResultDto(dto: IDeleteCriteriaResultDto): IDeleteCriteriaResult {
  // `hardDeleted` là bool nên không bao giờ bị `WhenWritingNull` nuốt; `=== true` ở đây là để một
  // envelope hỏng (thiếu khoá) rơi về "xoá mềm" — câu thông báo nhẹ hơn, đúng chiều an toàn.
  return { HardDeleted: dto.hardDeleted === true };
}

/**
 * DM-7 — trạng thái job.
 *
 * 🛑 **Chuỗi trạng thái LẠ quy về `Failed`, không quy về `Running`.** Một giá trị không thuộc bốn
 * giá trị của card nghĩa là hợp đồng đã gãy; coi nó là "đang chạy" thì FE poll mãi mãi một job
 * không bao giờ kết thúc — đúng kiểu hỏng chỉ lộ ra ở tab để mở qua đêm. `Failed` dừng vòng poll
 * và hiện lối ra ("nạp lại file").
 *
 * `result` **chỉ** có khi `Succeeded`; `errorMessage` và `errorCode` **chỉ** có khi `Failed`. Cả
 * ba đều nullable trên dây nên đều vắng mặt chứ không `null` (§0 của card).
 */
export function mapImportJobStatusDto(dto: IImportJobStatusDto): IImportJobStatus {
  const status = (IMPORT_JOB_STATES as readonly string[]).includes(dto.status)
    ? (dto.status as ImportJobState)
    : 'Failed';
  return {
    Status: status,
    Result: dto.result ? mapImportResultDto(dto.result) : null,
    ErrorMessage: dto.errorMessage ?? null,
    ErrorCode: dto.errorCode ?? null,
  };
}

function mapImportResultDto(dto: IImportResultDto): IImportResult {
  return {
    TotalRows: dto.totalRows ?? 0,
    SuccessCount: dto.successCount ?? 0,
    ErrorCount: dto.errorCount ?? 0,
    CriteriaCreatedCount: dto.criteriaCreatedCount ?? 0,
    Errors: (dto.errors ?? []).map(mapImportRowErrorDto),
  };
}

/**
 * `MessageParams` chuẩn hoá về **object rỗng**, không phải `null`.
 *
 * Một mã cố ý không mang tham số nào (`IMPORT.ROW_CODE_MISSING` — chính `Code` là thứ đang thiếu)
 * và ngx-translate nhận `{}` bình thường. Để `null` đi tiếp thì mỗi nơi hiển thị phải tự đoán, và
 * nơi nào quên thì câu mất tham số **im lặng** — nó vẫn in ra, chỉ thiếu con số.
 */
function mapImportRowErrorDto(dto: IImportRowErrorDto): IImportRowError {
  return {
    RowNumber: dto.rowNumber,
    Code: dto.code,
    MessageParams: dto.messageParams ?? {},
  };
}

// ===== Model app → dây (PascalCase → camelCase) =====
//
// Chiều NGƯỢC cũng đi qua đúng file này, cùng một lý do: casing đổi ở MỘT chỗ. Một payload ghi
// dựng thẳng trong service hay trong trang là chỗ thứ hai để `groupId` thành `GroupId` và nhận
// `400` mà không ai biết vì sao.

/** DM-3 / DM-4 — `assessment` LỒNG, và nó **vắng mặt** khi không có gì để ghi (khác với rỗng). */
export function toCriteriaWriteBody(payload: ICriteriaWritePayload): Record<string, unknown> {
  const body: Record<string, unknown> = {
    code: payload.Code,
    name: payload.Name,
    groupId: payload.GroupId,
    maxScore: payload.MaxScore,
  };
  if (payload.Assessment) body['assessment'] = toAssessmentBody(payload.Assessment);
  return body;
}

/**
 * 🛑 Trường `null` ĐI RA DÂY ở đây, cố ý — ngược chiều với luật đọc.
 *
 * `PUT` của DM-4 ghi đè trọn gói: bỏ `selfScore` khỏi body nghĩa là "giữ nguyên", còn gửi `null`
 * nghĩa là "xoá trắng ô đó". Người dùng xoá nội dung một ô rồi bấm Lưu phải ra vế thứ hai, nên
 * **không** được lọc `null` ra khỏi payload cho gọn.
 *
 * `year` thì ngược lại — nó chỉ có nghĩa khi `period = "all"`, nên chỉ đi khi có mặt.
 *
 * 🛑 **`version` cũng chỉ đi khi CÓ (Q74, 2026-09-11).** Card chốt *"vắng mặt = không kiểm
 * concurrency"*, và sau khi BE dựng `Assigned<T>` thì `version: null` **không còn** đồng nghĩa với
 * vắng mặt — nó là một giá trị được gán. Một chỉ tiêu chưa có bản ghi đánh giá không mang
 * `Version` nào, nên gửi `null` cho nó là nói một câu về concurrency mà ta không có ý nói.
 */
function toAssessmentBody(assessment: ICriteriaAssessmentPayload): Record<string, unknown> {
  const body: Record<string, unknown> = {
    period: assessment.Period,
    // Sáu trường dữ liệu LUÔN đi, kể cả `null`: hộp thoại là bộ soạn TRỌN GÓI — người dùng nhìn
    // thấy cả sáu ô, nên một ô họ xoá trắng nghĩa là "xoá trắng", đúng vế `null` của Q74.
    selfScore: assessment.SelfScore,
    verifiedScore: assessment.VerifiedScore,
    status: assessment.Status,
    ownerId: assessment.OwnerId,
    deadline: assessment.Deadline,
    note: assessment.Note,
  };
  if (assessment.Year !== undefined) body['year'] = assessment.Year;
  if (assessment.Version !== null && assessment.Version !== undefined) body['version'] = assessment.Version;
  return body;
}

/**
 * DM-6 — kỳ đích + **chỉ trường người dùng thật sự sửa**.
 *
 * ## 🔄 LẬT 2026-09-11 (Q74) — trước đây gửi CẢ HAI trường, nay chỉ gửi trường đã đổi
 *
 * Card DM-6 chốt *"FE LUÔN gửi cả 2 trường … lấy giá trị hiện tại của trường còn lại từ dòng đang
 * có trong bộ nhớ"*, và **lý do** nó nêu là *"một `PUT` mà bỏ trống trường nào thì null-hoá trường
 * đó"*. Lý do đó **hết đúng** khi BE dựng `Assigned<T>`: bỏ trống nay nghĩa là **giữ nguyên**.
 *
 * Giữ nguyên chữ của card sau khi lý do của nó biến mất sẽ dựng lại đúng thứ card muốn tránh,
 * chỉ ở chiều ngược: "điền cho đủ" bằng giá trị đọc lúc mở ô sửa là **ghi đè mù** thứ người khác
 * vừa đổi trong khoảng giữa — một lost update mà `version` chỉ chặn được khi có `version`.
 *
 * 🛑 Phân biệt ba trạng thái, đừng gộp hai cái đầu:
 *
 * | Giá trị vào | Ra dây | Nghĩa |
 * | --- | --- | --- |
 * | `undefined` | **không có khoá** | không đụng tới |
 * | `null` | `"note": null` | xoá trắng |
 * | `'abc'` | `"note": "abc"` | đặt giá trị |
 */
export function toInlineAssessmentBody(payload: IInlineAssessmentPayload): Record<string, unknown> {
  const body: Record<string, unknown> = { period: payload.Period };

  if (payload.Year !== undefined) body['year'] = payload.Year;
  // `!== undefined` chứ KHÔNG `!= null`: `null` phải đi ra dây, nó là lệnh xoá trắng.
  if (payload.ProgressPercent !== undefined) body['progressPercent'] = payload.ProgressPercent;
  if (payload.Note !== undefined) body['note'] = payload.Note;
  if (payload.Version !== undefined && payload.Version !== null) body['version'] = payload.Version;

  return body;
}

/**
 * `UserDto` → option của ô `Phụ trách`.
 *
 * `fullName` vắng mặt (luật `WhenWritingNull`) thì lùi về `userName` rồi mới tới `id`: một option
 * **không bao giờ** được rỗng — một dòng trắng trong `<select>` chọn được, và người chọn nó không
 * biết mình vừa gán việc cho ai.
 */
export function mapOwnerOptionDto(dto: IOwnerOptionDto): IOwnerOption {
  return { Id: dto.id, Name: dto.fullName || dto.userName || dto.id };
}

export function mapOwnerPageDto(dto: IOwnerPageDto): IOwnerPage {
  return {
    Items: (dto.items ?? []).map(mapOwnerOptionDto),
    // `totalCount` nuôi câu "danh sách đã bị cắt, hãy gõ để tìm" — trần `pageSize` của
    // `GET /api/users` là 200, và số người dùng vượt trần là ca có thật (`doc/contracts/users.md`).
    TotalCount: dto.totalCount ?? 0,
  };
}

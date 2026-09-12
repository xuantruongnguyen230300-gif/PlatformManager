// ===== Wire (DTO) — camelCase NGUYÊN XI, xem doc/contracts/danh-muc-dti.md =====
//
// 🛑 LUẬT ĐẮT NHẤT CỦA FILE NÀY (§0 của card): **trường `null` KHÔNG ra dây, nó VẮNG MẶT khỏi
// JSON.** `Program.cs` bật `DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull` cho cả
// đường MVC lẫn `Http.Json`. Một chỉ tiêu chưa có đánh giá **không** trả `"selfScore": null` — nó
// không có khoá `selfScore` nào cả.
//
// Vì vậy mọi trường nullable dưới đây khai `?:` **và** `| null`: hai hình dạng khác nhau cùng
// nghĩa "không có", mapper là nơi duy nhất quy chúng về một. Đây là chỗ dự án đã trả giá thật một
// lần (BE gửi `criteriaCode` trong khi FE đọc `code`, không ai đối chiếu) — TypeScript bị xoá lúc
// chạy nên một model nói dối về hình dạng dữ liệu không làm đỏ thứ gì.

/** DM-1 — `GET /api/criteria-groups`. */
export interface ICriteriaGroupDto {
  id: string;
  code: string;
  name: string;
  displayOrder: number;
}

/** DM-2 — một dòng lưới. Bộ trường và thứ tự bám nguyên card. */
export interface ICriteriaRowDto {
  criteriaId: string;
  code: string;
  name: string;
  groupId: string;
  groupCode: string;
  groupName: string;
  maxScore: number;
  assessmentId?: string | null;
  assessmentDate?: string | null;
  progressPercent?: number | null;
  selfScore?: number | null;
  verifiedScore?: number | null;
  /** TÍNH ở BE = `verifiedScore − selfScore` (Q25, đảo chiều 2026-09-05). FE **không** tính lại. */
  diff?: number | null;
  status?: string | null;
  ownerId?: string | null;
  ownerName?: string | null;
  deadline?: string | null;
  note?: string | null;
  /**
   * Token optimistic concurrency của bản ghi đánh giá — **gửi lại** ở cả hộp thoại (DM-4) lẫn sửa
   * inline (DM-6). Đó là thứ duy nhất làm `CRITERIA.ASSESSMENT_CONFLICT` phát hiện được; bỏ nó đi
   * thì mọi lần ghi đều thắng và lỗi mất dữ liệu sẽ im lặng.
   */
  version?: string | null;
  /** Q31 — kỳ của CHÍNH DÒNG NÀY. Luôn là một tuần sau Q37. */
  assessmentPeriod?: string | null;
  /** `"Tuần 33/2026 (10/08 – 16/08/2026)"` — **BE dựng sẵn**, FE không ghép lại. */
  assessmentPeriodLabel?: string | null;
}

/**
 * DM-2 — payload đầy đủ: `PagedList<CriteriaRowDto>` **cộng thêm** khối quyền ghi CẤP MÀN.
 *
 * Ngoại lệ đã đăng ký ở §0 của card: bốn trường phân trang giữ nguyên tên/vị trí/tầng, khối quyền
 * nằm CÙNG CẤP với `items`. Không lồng `PagedList` vào trong một object khác — lồng là phá mapper
 * phân trang dùng chung, cộng thêm thì không.
 */
export interface ICriteriaGridDto {
  items: ICriteriaRowDto[];
  page: number;
  pageSize: number;
  totalCount: number;

  canWrite: boolean;
  isEditable: boolean;
  /** LUÔN có mặt; `[]` khi ghi được. BE trả `null` ở đây là mọi `.includes(...)` của FE ném lỗi. */
  editBlockedBy: string[];
  /** Q66 — kỳ đang xem có thuộc NĂM HIỆN TẠI không. FE chọn biến thể câu nhắc theo đây (Q60). */
  isCurrentYear: boolean;
  /**
   * Q72 — **kỳ hiện tại**, tức tuần ISO chứa hôm nay, khuôn `"YYYY-Www"`.
   *
   * 🛑 KHÔNG khai `?:` / `| null`: card chốt **không bao giờ vắng mặt**, kể cả lưới rỗng và kể cả
   * `canWrite = false` — hai trường này mô tả **lịch**, không mô tả quyền. Cho chúng vào nhóm
   * nullable là mở một nhánh "chưa biết tuần nào" mà server không bao giờ sinh ra.
   */
  currentPeriod: string;
  /** Q72 — nhãn kỳ ĐẦY ĐỦ của `currentPeriod`, **BE dựng sẵn**. FE không ghép lại (Q40). */
  currentPeriodLabel: string;
}

// ===== Model app — PascalCase + prefix I =====

// Bốn giá trị Trạng thái và ánh xạ badge KHÔNG khai lại ở đây: chúng là "bảng ánh xạ DUY NHẤT"
// dùng chung với màn Dashboard, và G8 cấm hai module nghiệp vụ import chéo nhau — nên chỗ đứng duy
// nhất là `shared/models/dti-criteria-status.model.ts`. Import thẳng từ đó.

/**
 * Ba mã của `editBlockedBy`, đúng thứ tự cố định của card (§7.1a).
 *
 * 🛑 Mã LẠ không được làm mở khoá. Gặp mã chưa biết thì vẫn coi là bị chặn (`isEditable` đã
 * `false`) và hiện một câu chung — bỏ sót theo hướng AN TOÀN, không theo hướng cho ghi.
 */
export const EDIT_BLOCK_CODES = ['NO_WRITE_PERMISSION', 'PERIOD_NOT_WEEKLY', 'PERIOD_OUT_OF_YEAR'] as const;

export type EditBlockCode = (typeof EDIT_BLOCK_CODES)[number];

export interface ICriteriaGroup {
  readonly Id: string;
  readonly Code: string;
  readonly Name: string;
  readonly DisplayOrder: number;
}

export interface ICriteriaRow {
  readonly CriteriaId: string;
  readonly Code: string;
  readonly Name: string;
  readonly GroupId: string;
  readonly GroupCode: string;
  readonly GroupName: string;
  readonly MaxScore: number;
  readonly AssessmentId: string | null;
  readonly AssessmentDate: string | null;
  readonly ProgressPercent: number | null;
  readonly SelfScore: number | null;
  readonly VerifiedScore: number | null;
  readonly Diff: number | null;
  readonly Status: string | null;
  readonly OwnerId: string | null;
  readonly OwnerName: string | null;
  readonly Deadline: string | null;
  readonly Note: string | null;
  readonly Version: string | null;
  readonly AssessmentPeriod: string | null;
  readonly AssessmentPeriodLabel: string | null;
}

/**
 * Khối quyền ghi CẤP MÀN — ba trường, MỘT nguồn sự thật.
 *
 * | Trường | FE dùng để |
 * | --- | --- |
 * | `CanWrite` | hiện/**ẩn** affordance ghi |
 * | `IsEditable` | bật/**tắt** (`disabled`) affordance ghi |
 * | `EditBlockedBy` | chọn **lời nhắc** nào hiện trên dải băng V3 |
 *
 * 🛑 FE **KHÔNG** tự suy lại ba điều kiện từ bộ lọc nó đang giữ, và **không** suy quyền từ
 * `items.length` hay từ `roles` của `/api/auth/me` (`/me` được cache lúc khởi động, quyền thì thu
 * hồi được giữa phiên — card DM-2 mục 3 giải thích đủ). Server tính, FE đọc mã rồi tra bảng dịch.
 */
export interface ICriteriaWriteAccess {
  readonly CanWrite: boolean;
  readonly IsEditable: boolean;
  readonly EditBlockedBy: readonly string[];
  readonly IsCurrentYear: boolean;
  /**
   * Q72 — tuần ISO chứa hôm nay (`"YYYY-Www"`) và nhãn đầy đủ của nó, do **server** cấp.
   *
   * Ba trường `EditBlockedBy` / `IsEditable` / hai trường này cùng trả lời một câu hỏi — *"lời ghi
   * của tôi sẽ đi đâu, và có đi được không"* — nên chúng sinh ra trong **cùng một** lần đánh giá
   * của **cùng một** request và không bao giờ lệch nhau. Đó là lý do chúng ở DM-2 chứ không ở
   * DB-3: ghép hai response của hai thời điểm thì tuần hiện tại có thể đổi ở giữa.
   *
   * Nơi tiêu thụ là dải băng "nhắc kỳ đích" (§5.5) và câu kỳ đích trong hai hộp thoại ghi — cả ba
   * nói **cùng một** chuyện, nên cả ba đọc **cùng một** cặp trường này chứ không tự tính lại.
   */
  readonly CurrentPeriod: string;
  readonly CurrentPeriodLabel: string;
}

export interface ICriteriaGrid {
  readonly Items: ICriteriaRow[];
  readonly Page: number;
  readonly PageSize: number;
  readonly TotalCount: number;
  readonly Access: ICriteriaWriteAccess;
}

/** Bộ lọc gửi lên DM-2. `undefined` = không lọc; **không** gửi chuỗi rỗng. */
export interface ICriteriaListParams {
  readonly Search?: string;
  readonly GroupId?: string;
  readonly Status?: string;
  readonly Year: number;
  /** `"all"` | `"YYYY-Www"` | `"YYYY-MM"`. */
  readonly Period: string;
  readonly Page: number;
  readonly PageSize: number;
}

// ===== VÒNG 2 — đường GHI (DM-3 … DM-7) =====

/** DM-3 — `POST /api/criteria` trả `CriteriaDto`, **không** phải một dòng lưới (Q49). */
export interface ICriteriaDto {
  id: string;
  code: string;
  name: string;
  groupId: string;
  groupName: string;
  maxScore: number;
}

/** DM-5 — `{ hardDeleted }`. BE quyết cứng/mềm; FE chỉ đọc để chọn câu thông báo. */
export interface IDeleteCriteriaResultDto {
  hardDeleted: boolean;
}

/** DM-7 bước 1 — object bọc, **không** phải `Guid` trần (chỗ cho `estimatedRows` về sau). */
export interface IImportJobStartedDto {
  jobId: string;
}

/**
 * DM-7 bước 2 — một lỗi DÒNG.
 *
 * 🛑 **KHÔNG có khoá `message`.** BE gửi `code` + `messageParams` (khoá là TÊN tham số), FE tra
 * bảng dịch và tự ráp câu — `doc/huong_dan/wiki-core/be/16-i18n-va-ma-loi.md` §3. Bản hợp đồng cũ
 * có `message` do BE ghép sẵn; giữ nhánh đọc nó là giữ đúng thứ đã bị gỡ vì FE không tách lại
 * được tham số ra khỏi câu.
 */
export interface IImportRowErrorDto {
  rowNumber: number;
  code: string;
  messageParams?: Record<string, string> | null;
}

export interface IImportResultDto {
  totalRows: number;
  successCount: number;
  errorCount: number;
  criteriaCreatedCount: number;
  errors: IImportRowErrorDto[];
}

export interface IImportJobStatusDto {
  status: string;
  /** CHỈ có khi `status = "Succeeded"`. Lỗi hạ tầng ⇒ vắng mặt. */
  result?: IImportResultDto | null;
  /** CHỈ có khi `status = "Failed"` — **dev-facing**, KHÔNG hiển thị cho người dùng cuối. */
  errorMessage?: string | null;
  /**
   * `businessCode` của ca `Failed` — **TUỲ CHỌN**, thêm 2026-09-11.
   *
   * Trước đó nhánh `Failed` chỉ có `errorMessage` dev-facing, tức FE **không có gì để dịch** và
   * mọi lỗi hạ tầng phải dùng chung một câu. Nay ca nào có mã thì nói được đúng chuyện đã xảy ra —
   * ca đầu tiên là `IMPORT.FILE_TOO_MANY_ROWS` (Q75).
   *
   * Vắng mặt vẫn là ĐƯỜNG ĐI HỢP LỆ (job crash thật thì không có mã nghiệp vụ nào), nên nơi gọi
   * phải giữ nguyên câu chung làm đường lùi.
   */
  errorCode?: string | null;
}

// ===== Model app — đường GHI =====

/**
 * Cặp `{ Period, Year }` của MỘT lời ghi — dựng ở đúng **một** chỗ (`services/write-period.ts`),
 * ba đường ghi (DM-4, DM-6, DM-7) cùng dùng.
 *
 * 🛑 `Period` là **nguyên giá trị** ô `Kỳ trong năm`, kể cả `"all"` (Q40). FE **không** quy `"all"`
 * về tuần hiện tại — lịch ISO có đúng một bản cài, ở server.
 */
export interface IWritePeriod {
  readonly Period: string;
  /** Bắt buộc khi `Period = "all"` (T15); server bỏ qua khi `Period` là một tuần cụ thể. */
  readonly Year?: number;
}

/** Sáu trường đánh giá của dialog DM-4 + kỳ đích. Vắng mặt = **không đụng** dữ liệu đánh giá. */
export interface ICriteriaAssessmentPayload extends IWritePeriod {
  readonly SelfScore: number | null;
  readonly VerifiedScore: number | null;
  readonly Status: string | null;
  readonly OwnerId: string | null;
  readonly Deadline: string | null;
  readonly Note: string | null;
  readonly Version: string | null;
}

/** DM-3 / DM-4 — 4 trường danh mục + object `assessment` LỒNG (không phẳng, xem card DM-4). */
export interface ICriteriaWritePayload {
  readonly Code: string;
  readonly Name: string;
  readonly GroupId: string;
  readonly MaxScore: number;
  readonly Assessment?: ICriteriaAssessmentPayload;
}

/**
 * DM-6 — sửa inline, đúng 2 trường nghiệp vụ.
 *
 * ## 🛑 Q74 (2026-09-11) — `?:` ở đây KHÔNG phải "cho tiện", nó là NGỮ NGHĨA
 *
 * BE dựng `Assigned<T>` + `JsonConverter` để phân biệt **khoá vắng khỏi JSON** với **khoá mang
 * `null`**, và hai thứ đó nay làm hai việc khác nhau:
 *
 * | Gửi gì | BE làm gì |
 * | --- | --- |
 * | `note: null` | **xoá trắng** ghi chú |
 * | không gửi `note` | **giữ nguyên** giá trị cũ |
 *
 * Nên `undefined` = *"tôi không đụng tới trường này"*, còn `null` = *"xoá nó đi"*. Mapper
 * `toInlineAssessmentBody` là chỗ duy nhất dịch phân biệt đó ra dây.
 */
export interface IInlineAssessmentPayload extends IWritePeriod {
  readonly ProgressPercent?: number | null;
  readonly Note?: string | null;
  readonly Version?: string | null;
}

/** DM-3 — response của `POST`. Thiếu mọi trường đánh giá ⇒ **không** thay dòng, phải tải lại lưới. */
export interface ICriteriaCreated {
  readonly Id: string;
  readonly Code: string;
  readonly Name: string;
  readonly GroupId: string;
  readonly GroupName: string;
  readonly MaxScore: number;
}

export interface IDeleteCriteriaResult {
  readonly HardDeleted: boolean;
}

/** Bốn trạng thái job của DM-7, không hơn. Chuỗi lạ ⇒ coi như `Failed` (xem mapper). */
export const IMPORT_JOB_STATES = ['Pending', 'Running', 'Succeeded', 'Failed'] as const;

export type ImportJobState = (typeof IMPORT_JOB_STATES)[number];

/**
 * Một lỗi DÒNG đã chuẩn hoá. `RowNumber` là **số dòng trong chính file người dùng gửi**, dòng 1 là
 * header — in đúng số đó để họ mở file ra sửa được ngay, đừng đánh số lại theo thứ tự dòng dữ liệu.
 */
export interface IImportRowError {
  readonly RowNumber: number;
  readonly Code: string;
  /** Khoá là TÊN tham số (`Code`, `MaxScore`, …) — cắm thẳng vào `{{...}}` của ngx-translate. */
  readonly MessageParams: Record<string, string>;
}

export interface IImportResult {
  readonly TotalRows: number;
  readonly SuccessCount: number;
  readonly ErrorCount: number;
  readonly CriteriaCreatedCount: number;
  readonly Errors: readonly IImportRowError[];
}

export interface IImportJobStatus {
  readonly Status: ImportJobState;
  readonly Result: IImportResult | null;
  /** Dev-facing. Ghi log / hiện cho quản trị — **không** dùng làm câu cho người dùng cuối. */
  readonly ErrorMessage: string | null;
  /** `businessCode` của ca `Failed`, dùng làm KHOÁ DỊCH. `null` ⇒ lùi về câu chung. */
  readonly ErrorCode: string | null;
}

// ===== Ô `Phụ trách` — TÁI DÙNG `GET /api/users` (Q12a), KHÔNG có endpoint riêng =====

/**
 * Đúng hai trường mà dialog cần từ `UserDto`.
 *
 * Khai hẹp chứ không chép cả `UserDto` sang đây: màn này **không** dùng `roles`, `isLocked`,
 * `mustChangePassword` — và một bản sao đầy đủ của một DTO thuộc hợp đồng khác sẽ phải sửa theo
 * mỗi lần hợp đồng đó đổi, dù màn này không quan tâm.
 */
export interface IOwnerOptionDto {
  id: string;
  fullName?: string | null;
  userName?: string | null;
}

export interface IOwnerPageDto {
  items: IOwnerOptionDto[];
  totalCount: number;
}

export interface IOwnerOption {
  readonly Id: string;
  /** **Họ tên**, không phải tên đăng nhập — cột `Phụ trách` của lưới cũng đọc `ownerName`. */
  readonly Name: string;
}

export interface IOwnerPage {
  readonly Items: readonly IOwnerOption[];
  readonly TotalCount: number;
}

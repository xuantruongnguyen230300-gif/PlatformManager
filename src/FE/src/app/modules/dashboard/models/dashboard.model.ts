// ===== Wire (DTO) — camelCase NGUYÊN XI, xem doc/contracts/dashboard.md CONTRACT DB-1 =====
//
// 🛑 Luật casing đắt nhất, dùng chung với màn Danh mục (§0 của `doc/contracts/danh-muc-dti.md`,
// file chủ về casing cho cả hai màn): **trường `null` KHÔNG ra dây, nó VẮNG MẶT khỏi JSON.**
//
// Ở màn này luật đó mang một hệ quả nghiệp vụ, không chỉ là chi tiết kỹ thuật: `—` và `0` là HAI
// điều khác nhau và khác biệt đó phải sống sót tới tận màn hình. `kpi.overallProgress` vắng mặt
// nghĩa là *"chưa ai nhập Tiến độ %"*; `0` nghĩa là *"đã đo, kết quả bằng không"*. Map nhầm thành
// `0` là để dashboard tuyên bố "tiến độ toàn xã: 0%" ngay sau khi nạp một file có 26 chỉ tiêu đã
// hoàn thành — một câu sai, hiển thị tự tin, và không ai kiểm lại vì nó trông như số bình thường.

/** `"week"` | `"month"` | `"year"` — chữ thường. `"year"` = "Tất cả (tổng hợp theo năm)". */
export type DashboardModeDto = 'week' | 'month' | 'year';

export interface IDashboardKpiDto {
  /** % — bình quân **`Tiến độ %`** gia quyền theo `maxScore`. KHÔNG phải `Σ Tự đánh giá / Σ Điểm tối đa`. */
  overallProgress?: number | null;
  /** Điểm phần trăm so với kỳ liền trước. */
  delta?: number | null;
  previousPeriodLabel?: string | null;
  /** Ba số ĐẾM — luôn có giá trị, kể cả `0`. Vắng mặt là hợp đồng đã gãy, không phải "chưa có". */
  up: number;
  flat: number;
  down: number;
  done: number;
  totalCriteria: number;
}

export interface IDashboardGroupDto {
  groupId: string;
  groupCode: string;
  groupName: string;
  /** Vắng mặt = chưa chỉ tiêu nào của nhóm có `Tiến độ %`. Thanh RỖNG, không phải thanh 0%. */
  progress?: number | null;
}

export interface IDashboardTrendPointDto {
  /** Khoá định danh kỳ — `"YYYY-Www"` | `"YYYY-MM"`. */
  period: string;
  /** Nhãn trục X **BE dựng sẵn** (Q43): `"06/07 – 12/07"` ở chế độ tuần, `"Th.1"` ở chế độ tháng/năm. */
  periodLabel: string;
  /**
   * Vắng mặt = kỳ KHÔNG có dữ liệu (Q44). 🛑 Phần tử vẫn PHẢI có mặt: trên trục category của
   * `chart.js`, bỏ hẳn một điểm thì hai điểm kề nhau được nối THẲNG — không có ô nào cho kỳ bị
   * bỏ, nên không có chỗ đứt nào. Chỉ `null` nằm đúng ô của kỳ mới ngắt được đường.
   */
  value?: number | null;
}

export interface IDashboardTableRowDto {
  criteriaId: string;
  code: string;
  name: string;
  groupId: string;
  groupCode: string;
  groupName: string;
  maxScore: number;
  selfScore?: number | null;
  verifiedScore?: number | null;
  /** TÍNH ở BE = `verifiedScore − selfScore` (Q25). FE **không** tính lại, **không** đổi dấu. */
  diff?: number | null;
  status?: string | null;
  note?: string | null;
}

export interface IDashboardAggregateDto {
  mode: DashboardModeDto;
  /** `"Tuần 33/2026 (10/08 – 16/08/2026)"` — BE dựng sẵn, FE không ghép lại (Q12). */
  periodLabel: string;
  periodStart: string;
  periodEnd: string;
  kpi: IDashboardKpiDto;
  groups: IDashboardGroupDto[];
  trend: IDashboardTrendPointDto[];
  table: IDashboardTableRowDto[];
}

// ===== Model app — PascalCase + prefix I =====

export type DashboardMode = 'week' | 'month' | 'year';

export interface IDashboardKpi {
  readonly OverallProgress: number | null;
  readonly Delta: number | null;
  readonly PreviousPeriodLabel: string | null;
  readonly Up: number;
  readonly Flat: number;
  readonly Down: number;
  readonly Done: number;
  readonly TotalCriteria: number;
}

export interface IDashboardGroup {
  readonly GroupId: string;
  readonly GroupCode: string;
  readonly GroupName: string;
  readonly Progress: number | null;
}

export interface IDashboardTrendPoint {
  readonly Period: string;
  readonly PeriodLabel: string;
  readonly Value: number | null;
}

export interface IDashboardTableRow {
  readonly CriteriaId: string;
  readonly Code: string;
  readonly Name: string;
  readonly GroupId: string;
  readonly GroupCode: string;
  readonly GroupName: string;
  readonly MaxScore: number;
  readonly SelfScore: number | null;
  readonly VerifiedScore: number | null;
  readonly Diff: number | null;
  readonly Status: string | null;
  readonly Note: string | null;
}

export interface IDashboardAggregate {
  readonly Mode: DashboardMode;
  readonly PeriodLabel: string;
  readonly PeriodStart: string;
  readonly PeriodEnd: string;
  readonly Kpi: IDashboardKpi;
  readonly Groups: IDashboardGroup[];
  readonly Trend: IDashboardTrendPoint[];
  readonly Table: IDashboardTableRow[];
}

/**
 * Tham số của DB-1.
 *
 * FE gửi **đúng một** trong `Date` / `Year` ở chế độ week/month. Đó là **lựa chọn của FE**, không
 * phải ràng buộc của hợp đồng — đọc kỹ, vì hai điều này dễ bị gộp làm một:
 *
 * | Luật | Nói gì |
 * | --- | --- |
 * | **Q61** | gửi **CẢ HAI** mà `year` ≠ năm ISO của tuần chứa `date` ⇒ `400 DASHBOARD.PERIOD_YEAR_MISMATCH` |
 * | **Q63** | bỏ trống `year` ⇒ lấy năm ISO của `date`. Bỏ trống cả hai ⇒ tuần hiện tại của năm hiện tại |
 *
 * Gửi một cái thì không có gì để mà lệch, nên nhánh đó là nhánh an toàn nhất. Q61 **không** cấm
 * gửi `year` một mình, và chế độ `month`/`year` thì Q61/Q63 không áp (một tháng dương lịch luôn
 * nằm gọn trong một năm).
 *
 * 🛑 Gửi `Year` một mình là **đường đi bắt buộc phải có**, không phải ca cần vá: một năm chưa có
 * dữ liệu trả `weeksInYear` **rỗng** (DB-3, `200`), nên FE không có kỳ nào để neo `date` vào — và
 * **không được** tự dựng một ngày trong năm đó, vì quy đổi lịch ISO là việc của BE (Q40).
 *
 * *(Sửa 2026-09-10: bản trước gọi hai trường là "loại trừ nhau … và đó là chủ đích: Q61 chốt…" —
 * gán cho Q61 một điều nó không nói, rồi từ đó suy ra FE thiếu tham số.)*
 */
export interface IDashboardParams {
  readonly Mode: DashboardMode;
  /** Ngày bất kỳ TRONG kỳ muốn xem, `YYYY-MM-DD`. Bỏ trống = kỳ hiện tại do server chọn. */
  readonly Date?: string;
  readonly Year?: number;
  readonly Search?: string;
  readonly GroupId?: string;
  readonly Status?: string;
}

/**
 * DB-4 — kết quả một lượt `Xuất báo cáo`.
 *
 * 🛑 Đây là endpoint **DUY NHẤT** của hệ thống không bọc envelope ở nhánh **thành công** (thân
 * response là bytes của file), nhưng nhánh **lỗi** thì vẫn là `IApiResult` JSON như mọi endpoint
 * khác. Nên "nhận được 200" chưa đủ để coi body là file — xem `DashboardService.exportReport`.
 */
export interface IExportedFile {
  readonly Blob: Blob;
  /** Đọc từ `Content-Disposition`; có đường lùi vì header đó không phải lúc nào cũng đọc được. */
  readonly FileName: string;
}

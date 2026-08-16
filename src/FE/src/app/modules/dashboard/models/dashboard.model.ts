// ===== Wire (DTO) — camelCase theo envelope mới (doc/contracts/dashboard.md). BE trả nguyên
// dạng dưới đây LỒNG trong `IApiResult<T>.data` — xem core/http/api-result.model.ts. =====

export type DashboardApiMode = 'week' | 'month' | 'year';

export type CriteriaBadgeDto = 'Hoàn thành' | 'Không tăng' | 'Đang thực hiện' | 'Chưa có dữ liệu';

export interface IDashboardKpiDto {
  overallProgress: number | null;
  delta: number | null;
  previousPeriodLabel: string | null;
  up: number;
  flat: number;
  down: number;
  done: number;
  totalCriteria: number;
}

export interface IDashboardGroupProgressDto {
  groupId: string;
  groupCode: string;
  groupName: string;
  progress: number | null;
}

export interface IDashboardTrendPointDto {
  label: string;
  value: number | null;
}

export interface IDashboardTableRowDto {
  criteriaId: string;
  code: string;
  name: string;
  groupCode: string;
  groupName: string;
  maxScore: number;
  previousValue: number | null;
  currentValue: number | null;
  delta: number | null;
  badge: CriteriaBadgeDto | null;
  note: string | null;
}

export interface IDashboardAggregateDto {
  mode: DashboardApiMode;
  periodLabel: string;
  kpi: IDashboardKpiDto;
  groups: IDashboardGroupProgressDto[];
  trend: IDashboardTrendPointDto[];
  table: IDashboardTableRowDto[];
}

export interface IReportDto {
  title: string;
  contentHtml: string;
}

// `IPeriodOptionDto`/`IPeriodOptionsDto` (`GET /api/dashboard/periods`) dùng CHUNG với
// `modules/danh-muc-dti` — đã chuyển sang shared/models/period-options.model.ts, xem
// shared/services/period-options.service.ts.

// ===== Model app — PascalCase + prefix I, không hậu tố (src/FE/.claude/docs/api-client.md) =====

export type DashboardViewMode = 'week' | 'month';

/** Giá trị đặc biệt cho dropdown chọn kỳ ở mode Tuần — "Tất cả (tổng hợp theo năm)". */
export const ALL_WEEKS_VALUE = '__ALL__';

export type CriteriaBadge = CriteriaBadgeDto;

export interface IDashboardKpi {
  OverallProgress: number | null;
  Delta: number | null;
  PreviousPeriodLabel: string | null;
  Up: number;
  Flat: number;
  Down: number;
  Done: number;
  TotalCriteria: number;
}

export interface IGroupProgress {
  GroupId: string;
  GroupCode: string;
  GroupName: string;
  Progress: number | null;
}

export interface ITrendPoint {
  Label: string;
  Value: number | null;
}

export interface ICriteriaTableRow {
  CriteriaId: string;
  Code: string;
  Name: string;
  GroupCode: string;
  GroupName: string;
  MaxScore: number;
  PreviousValue: number | null;
  CurrentValue: number | null;
  Delta: number | null;
  Badge: CriteriaBadge | null;
  Note: string | null;
}

export interface IDashboardAggregate {
  Mode: DashboardApiMode;
  PeriodLabel: string;
  Kpi: IDashboardKpi;
  Groups: IGroupProgress[];
  Trend: ITrendPoint[];
  Table: ICriteriaTableRow[];
}

export interface IReport {
  Title: string;
  ContentHtml: string;
}

export interface IDashboardQueryParams {
  Mode: DashboardApiMode;
  Date?: string;
  Year?: number;
}

// ===== Filter/sort state của bảng chỉ tiêu (client-side hoàn toàn, xem
// doc/ke-hoach-xay-lai-corebase.md yêu cầu gốc của task) =====
export type CriteriaChangeFilter = '' | 'up' | 'flat' | 'down' | 'done';
export type CriteriaSortBy = 'id' | 'delta' | 'low';

/**
 * Wire types (DTO) theo `doc/contracts/dashboard.md` (Status: IMPLEMENTED).
 * Bọc trong `ApiResponse<T>` (xem `core/http/api-response.model.ts`). Chỉ
 * dùng trong `dashboard.service.ts`.
 */

export interface DashboardKpiDto {
  OverallProgress: number | null;
  Delta: number | null;
  PreviousPeriodLabel: string | null;
  Up: number;
  Flat: number;
  Down: number;
  Done: number;
  TotalCriteria: number;
}

export interface DashboardGroupProgressDto {
  GroupId: string;
  GroupCode: string;
  GroupName: string;
  Progress: number | null;
}

export interface DashboardTrendPointDto {
  Label: string;
  Value: number | null;
}

export interface DashboardTableRowDto {
  CriteriaId: string;
  Code: string;
  Name: string;
  GroupCode: string;
  GroupName: string;
  MaxScore: number;
  PreviousValue: number | null;
  CurrentValue: number | null;
  Delta: number | null;
  Badge: string | null;
}

export interface DashboardResponseDto {
  Mode: string;
  PeriodLabel: string;
  Kpi: DashboardKpiDto;
  Groups: DashboardGroupProgressDto[];
  Trend: DashboardTrendPointDto[];
  Table: DashboardTableRowDto[];
}

export interface ReportResponseDto {
  Title: string;
  ContentHtml: string;
}

export interface PeriodOptionDto {
  Value: string;
  Date: string;
  OverallProgress: number | null;
}

export interface PeriodOptionsDto {
  Years: number[];
  WeeksInYear: PeriodOptionDto[];
  MonthsInYear: PeriodOptionDto[];
}

/**
 * Model app cho feature Dashboard — prefix `I`, field PascalCase, KHÔNG hậu
 * tố. Khớp `doc/contracts/dashboard.md` (Status: IMPLEMENTED, backend-expert).
 * Dashboard 100% read-only.
 */

export type DashboardMode = 'week' | 'month' | 'year';

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

export interface IDashboardGroupProgress {
  GroupId: string;
  GroupCode: string;
  GroupName: string;
  Progress: number | null;
}

export interface IDashboardTrendPoint {
  Label: string;
  Value: number;
}

export interface IDashboardTableRow {
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

export interface IDashboardSummary {
  Mode: DashboardMode;
  PeriodLabel: string;
  Kpi: IDashboardKpi;
  Groups: IDashboardGroupProgress[];
  Trend: IDashboardTrendPoint[];
  Table: IDashboardTableRow[];
}

export interface IDashboardReport {
  Title: string;
  ContentHtml: string;
}

export interface IPeriodOption {
  Value: string;
  Date: string;
  OverallProgress: number | null;
}

export interface IPeriodOptions {
  Years: number[];
  WeeksInYear: IPeriodOption[];
  MonthsInYear: IPeriodOption[];
}

export interface IDashboardQuery {
  Mode: DashboardMode;
  Date: string | null;
  Year: number | null;
}

export type TableChangeFilter = '' | 'up' | 'flat' | 'down' | 'done';
export type TableSortBy = 'id' | 'delta' | 'low';

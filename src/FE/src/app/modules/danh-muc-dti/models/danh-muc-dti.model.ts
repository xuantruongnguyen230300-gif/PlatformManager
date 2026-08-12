/**
 * Model app cho feature Danh mục DTI — prefix `I`, field PascalCase, KHÔNG hậu
 * tố (khác DTO đặt trong `services/danh-muc-dti.dto.ts`, hậu tố `Dto`). Khớp
 * `doc/contracts/danh-muc-dti.md` (Status: IMPLEMENTED, backend-expert).
 */

export interface ICriteriaGroup {
  Id: string;
  Code: string;
  Name: string;
  DisplayOrder: number;
}

export type CellField = 'progress' | 'note';

export interface IEditingCell {
  CriteriaId: string;
  Field: CellField;
}

/** `Period` — "all" (mặc định) | "YYYY-Www" (tuần ISO) | "YYYY-MM" (tháng). */
export type PeriodValue = string;
export const PERIOD_ALL: PeriodValue = 'all';

export interface ICriteriaGridRow {
  CriteriaId: string;
  Code: string;
  Name: string;
  GroupId: string;
  GroupCode: string;
  GroupName: string;
  MaxScore: number;
  AssessmentId: string | null;
  ProgressPercent: number | null;
  SelfScore: number | null;
  VerifiedScore: number | null;
  Diff: number | null;
  Status: string | null;
  OwnerId: string | null;
  OwnerName: string | null;
  Deadline: string | null;
  Note: string | null;
  AssessmentDate: string | null;
  IsEditable: boolean;
}

export interface ICriteriaGridResult {
  IsEditable: boolean;
  Items: ICriteriaGridRow[];
  Page: number;
  PageSize: number;
  TotalCount: number;
  TotalPages: number;
}

export interface ICriteria {
  Id: string;
  Code: string;
  Name: string;
  GroupId: string;
  GroupName: string;
  MaxScore: number;
}

export interface ICriteriaFormValue {
  Code: string;
  Name: string;
  GroupId: string;
  MaxScore: number;
}

export interface IImportRowError {
  RowNumber: number;
  Code: string | null;
  Message: string;
}

export interface IImportResult {
  TotalRows: number;
  SuccessCount: number;
  ErrorCount: number;
  CriteriaCreatedCount: number;
  GroupsCreatedCount: number;
  OwnersCreatedCount: number;
  Errors: IImportRowError[];
}

export interface IPeriodOption {
  Value: PeriodValue;
  Date: string;
  OverallProgress: number | null;
}

export interface IPeriodOptions {
  Years: number[];
  WeeksInYear: IPeriodOption[];
  MonthsInYear: IPeriodOption[];
}

export interface ICriteriaGridQuery {
  Year: number;
  Period: PeriodValue;
  SearchText: string;
  GroupId: string;
  Page: number;
  PageSize: number;
}

/**
 * Wire types (DTO) — giữ nguyên xi casing/field server trả về theo
 * `doc/contracts/danh-muc-dti.md` (Status: IMPLEMENTED). Bọc trong
 * `ApiResponse<T>`/`PagedResult<T>` (xem `core/http/api-response.model.ts`).
 * Hậu tố `Dto`. Chỉ dùng trong `danh-muc-dti.service.ts`.
 */

export interface CriteriaGroupDto {
  Id: string;
  Code: string;
  Name: string;
  DisplayOrder: number;
}

export interface CriteriaRowDto {
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

export interface CriteriaDto {
  Id: string;
  Code: string;
  Name: string;
  GroupId: string;
  GroupName: string;
  MaxScore: number;
}

export interface CriteriaUpsertRequestDto {
  Code: string;
  Name: string;
  GroupId: string;
  MaxScore: number;
}

export interface CriteriaDeleteResponseDto {
  HardDeleted: boolean;
}

export interface AssessmentPatchRequestDto {
  ProgressPercent?: number;
  Note?: string;
}

export interface AssessmentPatchResponseDto {
  CriteriaId: string;
  ProgressPercent: number;
  Note: string | null;
  CreatedAt: string;
}

export interface CsvImportRowErrorDto {
  RowNumber: number;
  Code: string | null;
  Message: string;
}

export interface CsvImportResultDto {
  TotalRows: number;
  SuccessCount: number;
  ErrorCount: number;
  CriteriaCreatedCount: number;
  Errors: CsvImportRowErrorDto[];
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

// ===== Wire (DTO) — camelCase theo envelope mới. Xem doc/contracts/danh-muc-dti.md. =====

export interface ICriteriaGroupDto {
  id: string;
  code: string;
  name: string;
  displayOrder: number;
}

export interface IEvidenceDto {
  id: string;
  content: string;
  orderIndex: number;
}

export interface ICriteriaRowDto {
  criteriaId: string;
  code: string;
  name: string;
  groupId: string;
  groupCode: string;
  groupName: string;
  maxScore: number;
  assessmentId: string | null;
  progressPercent: number | null;
  selfScore: number | null;
  verifiedScore: number | null;
  diff: number | null;
  status: string | null;
  ownerId: string | null;
  ownerName: string | null;
  deadline: string | null;
  note: string | null;
  assessmentDate: string | null;
  evidences: IEvidenceDto[];
  isEditable: boolean;
}

export interface ICriteriaDto {
  id: string;
  code: string;
  name: string;
  groupId: string;
  groupName: string;
  maxScore: number;
}

export interface ICriteriaUpsertRequestDto {
  code: string;
  name: string;
  groupId: string;
  maxScore: number;
}

export interface IAssessmentUpsertRequestDto {
  progressPercent: number | null;
  note: string | null;
}

export interface IAssessmentUpsertResultDto {
  criteriaId: string;
  progressPercent: number;
  note: string | null;
  createdAt: string;
}

export interface IDeleteCriteriaResultDto {
  hardDeleted: boolean;
}

export interface ICsvImportRowErrorDto {
  rowNumber: number;
  code: string | null;
  message: string;
}

export interface ICsvImportResultDto {
  totalRows: number;
  successCount: number;
  errorCount: number;
  criteriaCreatedCount: number;
  errors: ICsvImportRowErrorDto[];
}

// ===== Model app — PascalCase + prefix I, không hậu tố =====

export interface ICriteriaGroup {
  Id: string;
  Code: string;
  Name: string;
  DisplayOrder: number;
}

export interface IEvidence {
  Id: string;
  Content: string;
  OrderIndex: number;
}

export interface ICriteriaRow {
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
  Evidences: IEvidence[];
  IsEditable: boolean;
}

export interface ICriteria {
  Id: string;
  Code: string;
  Name: string;
  GroupId: string;
  GroupName: string;
  MaxScore: number;
}

export interface ICriteriaUpsertPayload {
  Code: string;
  Name: string;
  GroupId: string;
  MaxScore: number;
}

export interface IAssessmentUpsertPayload {
  ProgressPercent: number | null;
  Note: string | null;
}

export interface IDeleteCriteriaResult {
  HardDeleted: boolean;
}

export interface ICsvImportRowError {
  RowNumber: number;
  Code: string | null;
  Message: string;
}

export interface ICsvImportResult {
  TotalRows: number;
  SuccessCount: number;
  ErrorCount: number;
  CriteriaCreatedCount: number;
  Errors: ICsvImportRowError[];
}

/** `Period`: `'all'` | `'YYYY-Www'` (tuần ISO) | `'YYYY-MM'` (tháng) — xem doc/contracts/danh-muc-dti.md. */
export interface ICriteriaListParams {
  Year?: number;
  Period?: string;
  GroupId?: string;
  Search?: string;
  Page: number;
  PageSize: number;
}

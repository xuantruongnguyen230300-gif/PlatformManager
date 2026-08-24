import {
  ICriteria,
  ICriteriaDto,
  ICriteriaGroup,
  ICriteriaGroupDto,
  ICriteriaRow,
  ICriteriaRowDto,
  ICsvImportResult,
  ICsvImportResultDto,
  ICsvImportRowError,
  ICsvImportRowErrorDto,
  IDeleteCriteriaResult,
  IDeleteCriteriaResultDto,
  IEvidence,
  IEvidenceDto,
} from '../models/danh-muc-dti.model';

export function mapCriteriaGroupDtoToModel(dto: ICriteriaGroupDto): ICriteriaGroup {
  return { Id: dto.id, Code: dto.code, Name: dto.name, DisplayOrder: dto.displayOrder };
}

function mapEvidenceDtoToModel(dto: IEvidenceDto): IEvidence {
  return { Id: dto.id, Content: dto.content, OrderIndex: dto.orderIndex };
}

export function mapCriteriaRowDtoToModel(dto: ICriteriaRowDto): ICriteriaRow {
  return {
    CriteriaId: dto.criteriaId,
    Code: dto.code,
    Name: dto.name,
    GroupId: dto.groupId,
    GroupCode: dto.groupCode,
    GroupName: dto.groupName,
    MaxScore: dto.maxScore,
    // `?? null` — BE bỏ hẳn key khi giá trị null (`DefaultIgnoreCondition = WhenWritingNull`), nên
    // field vắng mặt đọc ra `undefined` dù type khai `| null`. KHÔNG dùng `?? null` cho số 0/chuỗi
    // rỗng (giá trị hợp lệ) — chỉ áp cho field có thể thật sự vắng mặt trên dây.
    AssessmentId: dto.assessmentId ?? null,
    ProgressPercent: dto.progressPercent ?? null,
    SelfScore: dto.selfScore ?? null,
    VerifiedScore: dto.verifiedScore ?? null,
    Diff: dto.diff ?? null,
    Status: dto.status ?? null,
    OwnerId: dto.ownerId ?? null,
    OwnerName: dto.ownerName ?? null,
    Deadline: dto.deadline ?? null,
    Note: dto.note ?? null,
    AssessmentDate: dto.assessmentDate ?? null,
    Evidences: dto.evidences.map(mapEvidenceDtoToModel),
    IsEditable: dto.isEditable,
  };
}

export function mapCriteriaDtoToModel(dto: ICriteriaDto): ICriteria {
  return {
    Id: dto.id,
    Code: dto.code,
    Name: dto.name,
    GroupId: dto.groupId,
    GroupName: dto.groupName,
    MaxScore: dto.maxScore,
  };
}

export function mapDeleteResultDtoToModel(dto: IDeleteCriteriaResultDto): IDeleteCriteriaResult {
  return { HardDeleted: dto.hardDeleted };
}

function mapCsvRowErrorDtoToModel(dto: ICsvImportRowErrorDto): ICsvImportRowError {
  return { RowNumber: dto.rowNumber, Code: dto.code, Message: dto.message };
}

export function mapCsvImportResultDtoToModel(dto: ICsvImportResultDto): ICsvImportResult {
  return {
    TotalRows: dto.totalRows,
    SuccessCount: dto.successCount,
    ErrorCount: dto.errorCount,
    CriteriaCreatedCount: dto.criteriaCreatedCount,
    Errors: dto.errors.map(mapCsvRowErrorDtoToModel),
  };
}

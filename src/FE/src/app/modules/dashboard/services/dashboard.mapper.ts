import {
  IDashboardAggregate,
  IDashboardAggregateDto,
  IDashboardGroupProgressDto,
  IDashboardKpiDto,
  IDashboardTableRowDto,
  IDashboardTrendPointDto,
  IGroupProgress,
  IReport,
  IReportDto,
  ICriteriaTableRow,
  IDashboardKpi,
  ITrendPoint,
} from '../models/dashboard.model';

function mapKpiDtoToModel(dto: IDashboardKpiDto): IDashboardKpi {
  return {
    OverallProgress: dto.overallProgress,
    Delta: dto.delta,
    PreviousPeriodLabel: dto.previousPeriodLabel,
    Up: dto.up,
    Flat: dto.flat,
    Down: dto.down,
    Done: dto.done,
    TotalCriteria: dto.totalCriteria,
  };
}

function mapGroupProgressDtoToModel(dto: IDashboardGroupProgressDto): IGroupProgress {
  return {
    GroupId: dto.groupId,
    GroupCode: dto.groupCode,
    GroupName: dto.groupName,
    Progress: dto.progress,
  };
}

function mapTrendPointDtoToModel(dto: IDashboardTrendPointDto): ITrendPoint {
  return { Label: dto.label, Value: dto.value };
}

function mapTableRowDtoToModel(dto: IDashboardTableRowDto): ICriteriaTableRow {
  return {
    CriteriaId: dto.criteriaId,
    Code: dto.code,
    Name: dto.name,
    GroupCode: dto.groupCode,
    GroupName: dto.groupName,
    MaxScore: dto.maxScore,
    PreviousValue: dto.previousValue,
    CurrentValue: dto.currentValue,
    Delta: dto.delta,
    Badge: dto.badge,
    Note: dto.note,
  };
}

export function mapDashboardAggregateDtoToModel(dto: IDashboardAggregateDto): IDashboardAggregate {
  return {
    Mode: dto.mode,
    PeriodLabel: dto.periodLabel,
    Kpi: mapKpiDtoToModel(dto.kpi),
    Groups: dto.groups.map(mapGroupProgressDtoToModel),
    Trend: dto.trend.map(mapTrendPointDtoToModel),
    Table: dto.table.map(mapTableRowDtoToModel),
  };
}

export function mapReportDtoToModel(dto: IReportDto): IReport {
  return { Title: dto.title, ContentHtml: dto.contentHtml };
}

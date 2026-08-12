import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiResponse, PagedResult } from '../../../core/http/api-response.model';
import { ApiResponseService } from '../../../core/services/api-response.service';
import {
  ICriteria,
  ICriteriaFormValue,
  ICriteriaGridQuery,
  ICriteriaGridResult,
  ICriteriaGridRow,
  ICriteriaGroup,
  IImportResult,
  IPeriodOptions
} from '../models/danh-muc-dti.model';
import {
  AssessmentPatchRequestDto,
  AssessmentPatchResponseDto,
  CriteriaDeleteResponseDto,
  CriteriaDto,
  CriteriaGroupDto,
  CriteriaRowDto,
  CriteriaUpsertRequestDto,
  CsvImportResultDto,
  PeriodOptionsDto
} from './danh-muc-dti.dto';

function mapGroupDto(dto: CriteriaGroupDto): ICriteriaGroup {
  return { Id: dto.Id, Code: dto.Code, Name: dto.Name, DisplayOrder: dto.DisplayOrder };
}

function mapRowDto(dto: CriteriaRowDto): ICriteriaGridRow {
  return {
    CriteriaId: dto.CriteriaId,
    Code: dto.Code,
    Name: dto.Name,
    GroupId: dto.GroupId,
    GroupCode: dto.GroupCode,
    GroupName: dto.GroupName,
    MaxScore: dto.MaxScore,
    AssessmentId: dto.AssessmentId,
    ProgressPercent: dto.ProgressPercent,
    SelfScore: dto.SelfScore,
    VerifiedScore: dto.VerifiedScore,
    Diff: dto.Diff,
    Status: dto.Status,
    OwnerId: dto.OwnerId,
    OwnerName: dto.OwnerName,
    Deadline: dto.Deadline,
    Note: dto.Note,
    AssessmentDate: dto.AssessmentDate,
    IsEditable: dto.IsEditable
  };
}

function mapCriteriaDto(dto: CriteriaDto): ICriteria {
  return {
    Id: dto.Id,
    Code: dto.Code,
    Name: dto.Name,
    GroupId: dto.GroupId,
    GroupName: dto.GroupName,
    MaxScore: dto.MaxScore
  };
}

function mapImportResultDto(dto: CsvImportResultDto): IImportResult {
  return {
    TotalRows: dto.TotalRows,
    SuccessCount: dto.SuccessCount,
    ErrorCount: dto.ErrorCount,
    CriteriaCreatedCount: dto.CriteriaCreatedCount,
    Errors: dto.Errors.map((e) => ({ RowNumber: e.RowNumber, Code: e.Code, Message: e.Message }))
  };
}

function mapPeriodOptionsDto(dto: PeriodOptionsDto): IPeriodOptions {
  return {
    Years: dto.Years,
    WeeksInYear: dto.WeeksInYear.map((w) => ({ Value: w.Value, Date: w.Date, OverallProgress: w.OverallProgress })),
    MonthsInYear: dto.MonthsInYear.map((m) => ({ Value: m.Value, Date: m.Date, OverallProgress: m.OverallProgress }))
  };
}

/**
 * Service gọi API cho feature Danh mục DTI — theo `doc/contracts/danh-muc-dti.md`
 * (Status: IMPLEMENTED). Mọi phương thức trả về Model app (`I*`), giải nén
 * `ApiResponse<T>` qua `ApiResponseService` rồi map DTO→Model ngay trong
 * service này — component/page không bao giờ thấy DTO hay envelope.
 */
@Injectable({ providedIn: 'root' })
export class DanhMucDtiService {
  private readonly http = inject(HttpClient);
  private readonly apiResponse = inject(ApiResponseService);
  private readonly baseUrl = environment.apiBaseUrl;

  getGroups(): Observable<ICriteriaGroup[]> {
    return this.apiResponse
      .unwrap(this.http.get<ApiResponse<CriteriaGroupDto[]>>(`${this.baseUrl}/criteria-groups`))
      .pipe(map((list) => list.map(mapGroupDto)));
  }

  getGrid(query: ICriteriaGridQuery): Observable<ICriteriaGridResult> {
    let params = new HttpParams()
      .set('year', query.Year)
      .set('period', query.Period)
      .set('page', query.Page)
      .set('pageSize', query.PageSize);
    if (query.SearchText) params = params.set('search', query.SearchText);
    if (query.GroupId) params = params.set('groupId', query.GroupId);

    return this.apiResponse
      .unwrap(this.http.get<ApiResponse<PagedResult<CriteriaRowDto>>>(`${this.baseUrl}/criteria`, { params }))
      .pipe(
        map((paged) => {
          const items = paged.Items.map(mapRowDto);
          return {
            IsEditable: items.length > 0 ? items[0].IsEditable : query.Period === 'all',
            Items: items,
            Page: paged.Page,
            PageSize: paged.PageSize,
            TotalCount: paged.TotalCount,
            TotalPages: paged.TotalPages
          };
        })
      );
  }

  createCriteria(value: ICriteriaFormValue): Observable<ICriteria> {
    const body: CriteriaUpsertRequestDto = {
      Code: value.Code,
      Name: value.Name,
      GroupId: value.GroupId,
      MaxScore: value.MaxScore
    };
    return this.apiResponse
      .unwrap(this.http.post<ApiResponse<CriteriaDto>>(`${this.baseUrl}/criteria`, body))
      .pipe(map(mapCriteriaDto));
  }

  updateCriteria(id: string, value: ICriteriaFormValue): Observable<ICriteria> {
    const body: CriteriaUpsertRequestDto = {
      Code: value.Code,
      Name: value.Name,
      GroupId: value.GroupId,
      MaxScore: value.MaxScore
    };
    return this.apiResponse
      .unwrap(this.http.put<ApiResponse<CriteriaDto>>(`${this.baseUrl}/criteria/${id}`, body))
      .pipe(map(mapCriteriaDto));
  }

  deleteCriteria(id: string): Observable<{ HardDeleted: boolean }> {
    return this.apiResponse
      .unwrap(this.http.delete<ApiResponse<CriteriaDeleteResponseDto>>(`${this.baseUrl}/criteria/${id}`))
      .pipe(map((dto) => ({ HardDeleted: dto.HardDeleted })));
  }

  /** Luôn ghi vào bản ghi "hôm nay" — `year`/`period` chỉ để BE đối chiếu
   * trạng thái Live, từ chối 409 nếu FE đang xem dữ liệu lịch sử (xem DM-6). */
  patchAssessment(
    criteriaId: string,
    patch: { ProgressPercent?: number; Note?: string },
    context: { Year: number; Period: string }
  ): Observable<{ CriteriaId: string; ProgressPercent: number; Note: string | null; CreatedAt: string }> {
    const body: AssessmentPatchRequestDto = patch;
    const params = new HttpParams().set('year', context.Year).set('period', context.Period);
    return this.apiResponse
      .unwrap(
        this.http.put<ApiResponse<AssessmentPatchResponseDto>>(
          `${this.baseUrl}/criteria/${criteriaId}/assessment`,
          body,
          { params }
        )
      )
      .pipe(
        map((dto) => ({
          CriteriaId: dto.CriteriaId,
          ProgressPercent: dto.ProgressPercent,
          Note: dto.Note,
          CreatedAt: dto.CreatedAt
        }))
      );
  }

  importCsv(file: File): Observable<IImportResult> {
    const formData = new FormData();
    formData.append('file', file, file.name);
    return this.apiResponse
      .unwrap(this.http.post<ApiResponse<CsvImportResultDto>>(`${this.baseUrl}/import/csv`, formData))
      .pipe(map(mapImportResultDto));
  }

  getPeriodOptions(year?: number): Observable<IPeriodOptions> {
    let params = new HttpParams();
    if (year) params = params.set('year', year);
    return this.apiResponse
      .unwrap(this.http.get<ApiResponse<PeriodOptionsDto>>(`${this.baseUrl}/dashboard/periods`, { params }))
      .pipe(map(mapPeriodOptionsDto));
  }
}

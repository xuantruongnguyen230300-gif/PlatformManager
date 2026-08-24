import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { IApiResult, unwrapData } from '../../../core/http/api-result.model';
import { IPagedResult, IPagedResultDto, mapPagedResultDto } from '../../../core/http/paged-result.model';
import {
  IAssessmentUpsertPayload,
  IAssessmentUpsertResultDto,
  ICriteria,
  ICriteriaDto,
  ICriteriaGroup,
  ICriteriaGroupDto,
  ICriteriaListParams,
  ICriteriaRow,
  ICriteriaRowDto,
  ICriteriaUpsertPayload,
  ICsvImportResult,
  ICsvImportResultDto,
  IDeleteCriteriaResult,
  IDeleteCriteriaResultDto,
} from '../models/danh-muc-dti.model';
import {
  mapCriteriaDtoToModel,
  mapCriteriaGroupDtoToModel,
  mapCriteriaRowDtoToModel,
  mapCsvImportResultDtoToModel,
  mapDeleteResultDtoToModel,
} from './danh-muc-dti.mapper';

function toUpsertRequestBody(payload: ICriteriaUpsertPayload) {
  return { code: payload.Code, name: payload.Name, groupId: payload.GroupId, maxScore: payload.MaxScore };
}

/**
 * Gọi API Danh mục DTI (CRUD + đánh giá theo tuần) — xem doc/contracts/danh-muc-dti.md. Envelope
 * unwrap qua `data`, map DTO→model, lỗi bay lên qua `httpErrorInterceptor` (component form tự đọc
 * `err.apiResult?.fields` khi cần bind lỗi từng field — xem
 * doc/huong_dan/wiki-core/fe/02-http-envelope.md).
 */
@Injectable({ providedIn: 'root' })
export class DanhMucDtiService {
  private readonly http = inject(HttpClient);

  getGroups(): Observable<ICriteriaGroup[]> {
    return this.http
      .get<IApiResult<ICriteriaGroupDto[]>>('/criteria-groups')
      .pipe(map((res) => (res.data ?? []).map(mapCriteriaGroupDtoToModel)));
  }

  getList(params: ICriteriaListParams): Observable<IPagedResult<ICriteriaRow>> {
    let httpParams = new HttpParams().set('page', params.Page).set('pageSize', params.PageSize);
    if (params.Year != null) httpParams = httpParams.set('year', params.Year);
    if (params.Period) httpParams = httpParams.set('period', params.Period);
    if (params.GroupId) httpParams = httpParams.set('groupId', params.GroupId);
    if (params.Search) httpParams = httpParams.set('search', params.Search);

    return this.http
      .get<IApiResult<IPagedResultDto<ICriteriaRowDto>>>('/criteria', { params: httpParams })
      .pipe(
        map((res) =>
          mapPagedResultDto(
            res.data ?? { items: [], page: params.Page, pageSize: params.PageSize, totalCount: 0 },
            mapCriteriaRowDtoToModel,
          ),
        ),
      );
  }

  create(payload: ICriteriaUpsertPayload): Observable<ICriteria> {
    return this.http
      .post<IApiResult<ICriteriaDto>>('/criteria', toUpsertRequestBody(payload))
      .pipe(map((res) => mapCriteriaDtoToModel(unwrapData(res))));
  }

  update(id: string, payload: ICriteriaUpsertPayload): Observable<ICriteria> {
    return this.http
      .put<IApiResult<ICriteriaDto>>(`/criteria/${id}`, toUpsertRequestBody(payload))
      .pipe(map((res) => mapCriteriaDtoToModel(unwrapData(res))));
  }

  delete(id: string): Observable<IDeleteCriteriaResult> {
    return this.http
      .delete<IApiResult<IDeleteCriteriaResultDto>>(`/criteria/${id}`)
      .pipe(map((res) => mapDeleteResultDtoToModel(unwrapData(res))));
  }

  /**
   * Upsert-trong-ngày (hôm nay) — `year`/`period` CHỈ gửi kèm để BE tự chặn (409) khi FE đang xem
   * dữ liệu lịch sử (`IsEditable=false`) mà lỡ vẫn gọi được, xem CONTRACT DM-6.
   */
  upsertAssessment(
    criteriaId: string,
    payload: IAssessmentUpsertPayload,
    year?: number,
    period?: string,
  ): Observable<IAssessmentUpsertResultDto> {
    let httpParams = new HttpParams();
    if (year != null) httpParams = httpParams.set('year', year);
    if (period) httpParams = httpParams.set('period', period);
    return this.http
      .put<IApiResult<IAssessmentUpsertResultDto>>(
        `/criteria/${criteriaId}/assessment`,
        { progressPercent: payload.ProgressPercent, note: payload.Note },
        { params: httpParams },
      )
      .pipe(map((res) => unwrapData(res)));
  }

  importCsv(file: File): Observable<ICsvImportResult> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http
      .post<IApiResult<ICsvImportResultDto>>('/import/csv', formData)
      .pipe(map((res) => mapCsvImportResultDtoToModel(unwrapData(res))));
  }
}

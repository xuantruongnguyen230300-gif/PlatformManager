import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { IApiResult } from '../../../core/http/api-result.model';
import { IDashboardAggregate, IDashboardAggregateDto, IDashboardQueryParams, IReport, IReportDto } from '../models/dashboard.model';
import { mapDashboardAggregateDtoToModel, mapReportDtoToModel } from './dashboard.mapper';

function buildQueryParams(params: IDashboardQueryParams): HttpParams {
  let httpParams = new HttpParams().set('mode', params.Mode);
  if (params.Date) httpParams = httpParams.set('date', params.Date);
  if (params.Year != null) httpParams = httpParams.set('year', params.Year);
  return httpParams;
}

/**
 * Gọi API Dashboard (đọc-only) — xem doc/contracts/dashboard.md. Envelope unwrap qua `data`,
 * map DTO→model, để lỗi bay lên qua `httpErrorInterceptor` (core/) — không tự `catchError` lặp
 * lại logic dịch lỗi, xem doc/huong_dan/wiki-core/fe/02-http-envelope.md §Service pattern.
 *
 * `GET /api/dashboard/periods` (năm/kỳ khả dụng) KHÔNG nằm ở đây — dùng chung với
 * `modules/danh-muc-dti`, xem `shared/services/period-options.service.ts`.
 */
@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly http = inject(HttpClient);

  getAggregate(params: IDashboardQueryParams): Observable<IDashboardAggregate> {
    return this.http
      .get<IApiResult<IDashboardAggregateDto>>('/dashboard', { params: buildQueryParams(params) })
      .pipe(map((res) => mapDashboardAggregateDtoToModel(res.data as IDashboardAggregateDto)));
  }

  getReport(params: IDashboardQueryParams): Observable<IReport> {
    return this.http
      .get<IApiResult<IReportDto>>('/dashboard/report', { params: buildQueryParams(params) })
      .pipe(map((res) => mapReportDtoToModel(res.data as IReportDto)));
  }
}

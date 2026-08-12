import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiResponse } from '../../../core/http/api-response.model';
import { ApiResponseService } from '../../../core/services/api-response.service';
import {
  DashboardMode,
  IDashboardQuery,
  IDashboardReport,
  IDashboardSummary,
  IPeriodOptions
} from '../models/dashboard.model';
import { DashboardResponseDto, PeriodOptionsDto, ReportResponseDto } from './dashboard.dto';

function mapSummaryDto(dto: DashboardResponseDto): IDashboardSummary {
  return {
    Mode: dto.Mode as DashboardMode,
    PeriodLabel: dto.PeriodLabel,
    Kpi: { ...dto.Kpi },
    Groups: dto.Groups.map((g) => ({ ...g })),
    Trend: dto.Trend.filter((t) => t.Value !== null).map((t) => ({ Label: t.Label, Value: t.Value as number })),
    Table: dto.Table.map((r) => ({ ...r }))
  };
}

function mapPeriodOptionsDto(dto: PeriodOptionsDto): IPeriodOptions {
  return {
    Years: dto.Years,
    WeeksInYear: dto.WeeksInYear.map((w) => ({ Value: w.Value, Date: w.Date, OverallProgress: w.OverallProgress })),
    MonthsInYear: dto.MonthsInYear.map((m) => ({ Value: m.Value, Date: m.Date, OverallProgress: m.OverallProgress }))
  };
}

function buildParams(query: IDashboardQuery): HttpParams {
  let params = new HttpParams().set('mode', query.Mode);
  if (query.Date) params = params.set('date', query.Date);
  if (query.Year !== null) params = params.set('year', query.Year);
  return params;
}

/**
 * Service gọi API cho feature Dashboard — theo `doc/contracts/dashboard.md`
 * (Status: IMPLEMENTED). 100% read-only. Mapper DTO→Model đặt ngay trong
 * service này, component chỉ thấy Model app (`I*`).
 */
@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly http = inject(HttpClient);
  private readonly apiResponse = inject(ApiResponseService);
  private readonly baseUrl = environment.apiBaseUrl;

  getDashboard(query: IDashboardQuery): Observable<IDashboardSummary> {
    return this.apiResponse
      .unwrap(this.http.get<ApiResponse<DashboardResponseDto>>(`${this.baseUrl}/dashboard`, { params: buildParams(query) }))
      .pipe(map(mapSummaryDto));
  }

  getReport(query: IDashboardQuery): Observable<IDashboardReport> {
    return this.apiResponse
      .unwrap(this.http.get<ApiResponse<ReportResponseDto>>(`${this.baseUrl}/dashboard/report`, { params: buildParams(query) }))
      .pipe(map((dto) => ({ Title: dto.Title, ContentHtml: dto.ContentHtml })));
  }

  getPeriodOptions(year?: number): Observable<IPeriodOptions> {
    let params = new HttpParams();
    if (year) params = params.set('year', year);
    return this.apiResponse
      .unwrap(this.http.get<ApiResponse<PeriodOptionsDto>>(`${this.baseUrl}/dashboard/periods`, { params }))
      .pipe(map(mapPeriodOptionsDto));
  }
}

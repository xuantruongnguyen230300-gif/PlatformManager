import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { IApiResult, unwrapData } from '../../core/http/api-result.model';
import { IPeriodOption, IPeriodOptionDto, IPeriodOptions, IPeriodOptionsDto } from '../models/period-options.model';

function mapOptionDtoToModel(dto: IPeriodOptionDto): IPeriodOption {
  return { Value: dto.value, Date: dto.date, OverallProgress: dto.overallProgress };
}

/**
 * `GET /api/dashboard/periods` — dùng chung bởi `modules/dashboard` (dropdown năm/tuần + lịch
 * sử) và `modules/danh-muc-dti` (dropdown năm/kỳ lọc grid) — xem
 * doc/contracts/dashboard.md CONTRACT DB-3.
 */
@Injectable({ providedIn: 'root' })
export class PeriodOptionsService {
  private readonly http = inject(HttpClient);

  getPeriodOptions(year?: number): Observable<IPeriodOptions> {
    let httpParams = new HttpParams();
    if (year != null) httpParams = httpParams.set('year', year);
    return this.http.get<IApiResult<IPeriodOptionsDto>>('/dashboard/periods', { params: httpParams }).pipe(
      map((res) => {
        const dto = unwrapData(res);
        return {
          Years: dto.years,
          WeeksInYear: dto.weeksInYear.map(mapOptionDtoToModel),
          MonthsInYear: dto.monthsInYear.map(mapOptionDtoToModel),
        };
      }),
    );
  }
}

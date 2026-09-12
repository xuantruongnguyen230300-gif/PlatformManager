import { Injectable, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Observable, Subject, catchError, map, of, switchMap, tap } from 'rxjs';
import { DashboardService } from '../../services/dashboard.service';
import { IDashboardAggregate, IDashboardParams } from '../../models/dashboard.model';

/** KHOÁ DỊCH — câu nằm ở `public/i18n-app/<mã>.json`. */
const LOAD_ERROR_KEY = 'dashboard.error.loadFailed';

type AggregateOutcome = { readonly Ok: true; readonly Result: IDashboardAggregate } | { readonly Ok: false };

/**
 * Trạng thái của NGUỒN THỨ NHẤT — `GET /api/dashboard` (DB-1), nuôi V2…V5.
 *
 * Trang gọi **hai** nguồn và chúng về không cùng lúc; nguồn thứ hai (`GET /api/dashboard/periods`,
 * nuôi ô chọn kỳ ở V1 và danh sách V8) đi qua `shared/services/dti-period.service.ts`. Vì vậy
 * trạng thái tải là **theo vùng**, không phải một màn trắng — xem `dashboard.page.ts` §Đang tải.
 *
 * 🛑 `@Injectable()` KHÔNG `providedIn: 'root'`: vòng đời trùng vòng đời trang.
 */
@Injectable()
export class DashboardFeed {
  private readonly service = inject(DashboardService);

  readonly data = signal<IDashboardAggregate | null>(null);
  readonly loading = signal(false);
  readonly loadError = signal<string | null>(null);

  /**
   * `switchMap` là bắt buộc chứ không phải tối ưu: người dùng bấm sang tuần khác hai lần liên tiếp
   * thì kết quả của lượt ĐẦU có thể về sau, và khi đó cả năm ô KPI hiện số của một kỳ mà không có
   * gì trên màn hình nói ra. Đó đúng là ca hỏng mà Q34 (làm mờ khi đang tải) sinh ra để chặn, và
   * làm mờ một mình thì không chặn được nó.
   */
  private readonly requests = new Subject<IDashboardParams>();

  constructor() {
    this.requests
      .pipe(
        tap(() => {
          this.loading.set(true);
          this.loadError.set(null);
        }),
        switchMap((params) => this.fetch(params)),
        takeUntilDestroyed(),
      )
      .subscribe((outcome) => this.apply(outcome));
  }

  load(params: IDashboardParams): void {
    this.requests.next(params);
  }

  private fetch(params: IDashboardParams): Observable<AggregateOutcome> {
    return this.service.getAggregate(params).pipe(
      map((result): AggregateOutcome => ({ Ok: true, Result: result })),
      catchError((): Observable<AggregateOutcome> => of({ Ok: false })),
    );
  }

  /**
   * Hỏng ⇒ **XOÁ số liệu** rồi hiện dải băng `.bad` (§5.4). Giữ lại bộ số của kỳ trước là đúng lỗi
   * mà Q34 ghi nhận trước đây: người dùng đổi kỳ, thấy y nguyên bộ số cũ, và không có gì cho biết
   * đó là số của kỳ nào.
   */
  private apply(outcome: AggregateOutcome): void {
    this.loading.set(false);
    if (!outcome.Ok) {
      this.data.set(null);
      this.loadError.set(LOAD_ERROR_KEY);
      return;
    }
    this.loadError.set(null);
    this.data.set(outcome.Result);
  }
}

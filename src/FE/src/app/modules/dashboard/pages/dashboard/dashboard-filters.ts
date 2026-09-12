import { Injectable, computed, effect, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Params, Router } from '@angular/router';
import { Subject, catchError, debounceTime, distinctUntilChanged, of, switchMap } from 'rxjs';
import { CRITERIA_STATUSES } from '../../../../shared/models/dti-criteria-status.model';
import { IPeriodOption, IPeriodOptions } from '../../../../shared/models/dti-period.model';
import { DtiPeriodService } from '../../../../shared/services/dti-period.service';
import { toPlainDate } from '../../../../shared/services/dti-period.mapper';
import { DashboardMode, IDashboardParams } from '../../models/dashboard.model';

const DEBOUNCE_MS = 300;

/** Giá trị SENTINEL của ô chọn kỳ — không phải một mã kỳ, nên tách khỏi danh sách thật. */
const PERIOD_CURRENT = '';
const PERIOD_YEAR = 'year';

const MODES: readonly DashboardMode[] = ['week', 'month', 'year'];
const WEEK_PERIOD = /^\d{4}-W\d{1,2}$/;
const MONTH_PERIOD = /^\d{4}-\d{2}$/;
const GUID = /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/;

const YEAR_MIN = 2000;
const YEAR_MAX = 2100;

/** Hai lựa chọn sắp xếp còn lại sau Q22 — cả hai xếp được **ở FE** trên `table[]` đã tải. */
type DashboardSort = 'code' | 'diff';

interface ISanitizedState {
  readonly Mode: DashboardMode;
  readonly Year: number;
  readonly Period: string;
  readonly Search: string;
  readonly GroupId: string;
  readonly Status: string;
}

/** So hai bộ tham số DB-1 theo TỪNG TRƯỜNG — xem `equal` của `requestParams`. */
function sameParams(a: IDashboardParams, b: IDashboardParams): boolean {
  return (
    a.Mode === b.Mode &&
    a.Date === b.Date &&
    a.Year === b.Year &&
    a.Search === b.Search &&
    a.GroupId === b.GroupId &&
    a.Status === b.Status
  );
}

function readInt(raw: string | null): number | null {
  if (raw === null || raw.trim() === '') return null;
  const value = Number(raw);
  return Number.isInteger(value) ? value : null;
}
/**
 * BỘ LỌC + CHỌN KỲ của màn Dashboard: đọc/ghi URL, làm sạch tham số lạ, giữ bản nháp panel lọc,
 * nạp danh sách Năm/Kỳ (DB-3), và dựng bộ tham số cho DB-1.
 *
 * Tách khỏi `DashboardPage` (vòng 2, 2026-09-11) theo đúng khuôn `criteria-filters.ts` của màn
 * Danh mục và `user-list-feed.ts` của màn Core: lớp này **không biết** KPI, biểu đồ hay bảng chi
 * tiết tồn tại. Nó trả lời đúng một câu — *"người dùng đang xem kỳ nào, lọc gì"* — và trang dùng
 * câu đó để quyết định tải cái gì.
 *
 * 🛑 `@Injectable()` KHÔNG `providedIn: 'root'` — khai trong `providers` của trang nên vòng đời
 * trùng vòng đời trang. Lên `root` là để kỳ đang xem của lượt trước ghi đè URL của lượt mới.
 *
 * ## Ô sắp xếp KHÔNG lên URL
 *
 * `fe-routing-guard.md` §8 khai state trên URL cho **bộ lọc và kỳ** — hai thứ đổi TẬP dòng người
 * ta muốn gửi qua link. Thứ tự dòng thì không, và nó cũng không phải tham số API.
 */
@Injectable()
export class DashboardFilters {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly periodService = inject(DtiPeriodService);

  private readonly currentYear = new Date().getFullYear();

  private readonly queryMap = toSignal(this.route.queryParamMap, { requireSync: true });

  readonly periodOptions = signal<IPeriodOptions | null>(null);
  readonly statuses = CRITERIA_STATUSES;

  // ===== Q62 — làm sạch tham số URL trước khi gọi API =====

  private readonly sanitized = computed<ISanitizedState>(() => {
    const map = this.queryMap();
    const rawMode = map.get('mode') ?? 'week';
    const mode: DashboardMode = (MODES as readonly string[]).includes(rawMode) ? (rawMode as DashboardMode) : 'week';
    const rawYear = readInt(map.get('year'));
    const rawPeriod = map.get('period') ?? PERIOD_CURRENT;
    const rawStatus = map.get('status') ?? '';
    const rawGroup = map.get('groupId') ?? '';

    // Kỳ phải khớp ĐƠN VỊ của chế độ: một `period` dạng tháng khi đang ở chế độ Tuần là "ngoài
    // miền", cùng loại với `mode` lạ. Ở `mode=year` thì không có ô kỳ nào để giữ.
    const periodShapeOk =
      mode === 'week' ? WEEK_PERIOD.test(rawPeriod) : mode === 'month' ? MONTH_PERIOD.test(rawPeriod) : false;

    return {
      Mode: mode,
      Year: rawYear !== null && rawYear >= YEAR_MIN && rawYear <= YEAR_MAX ? rawYear : this.currentYear,
      Period: periodShapeOk ? rawPeriod : PERIOD_CURRENT,
      Search: map.get('q') ?? '',
      GroupId: GUID.test(rawGroup) ? rawGroup : '',
      Status: (CRITERIA_STATUSES as readonly string[]).includes(rawStatus) ? rawStatus : '',
    };
  });

  readonly isClean = computed<boolean>(() => {
    const map = this.queryMap();
    const clean = this.sanitized();
    const same = (key: string, value: string): boolean => {
      const raw = map.get(key);
      return raw === null || raw === value;
    };
    return (
      same('mode', clean.Mode) &&
      same('year', `${clean.Year}`) &&
      same('period', clean.Period) &&
      same('groupId', clean.GroupId) &&
      same('status', clean.Status)
    );
  });

  readonly mode = computed(() => this.sanitized().Mode);
  readonly year = computed(() => this.sanitized().Year);
  readonly period = computed(() => this.sanitized().Period);
  readonly searchText = computed(() => this.sanitized().Search);
  readonly groupFilter = computed(() => this.sanitized().GroupId);
  readonly statusFilter = computed(() => this.sanitized().Status);

  // ===== Ô tìm kiếm + bản nháp bộ lọc =====

  readonly searchInput = signal('');
  private readonly searchSubject = new Subject<string>();
  private lastPushedSearch = '';

  readonly groupDraft = signal('');
  readonly statusDraft = signal('');

  /**
   * Ô sắp xếp KHÔNG lên URL, và đó là chủ đích: `fe-routing-guard.md` §8 khai state trên URL cho
   * **bộ lọc và kỳ** — hai thứ đổi TẬP dòng người ta muốn gửi qua link. Thứ tự dòng thì không, và
   * nó cũng không phải tham số API (§4: cả hai lựa chọn xếp được ở FE trên `table[]` đã tải).
   */
  readonly sortBy = signal<DashboardSort>('code');

  // ===== Danh sách kỳ (nguồn thứ hai) =====

  private readonly yearRequests = new Subject<number>();

  /** Đơn vị kỳ đang bày trong ô chọn. Ở `mode=year` vẫn bày tuần — option `Tất cả` mới là thứ đang chọn. */
  readonly periodChoices = computed<IPeriodOption[]>(() => {
    const options = this.periodOptions();
    if (!options) return [];
    return this.mode() === 'month' ? options.MonthsInYear : options.WeeksInYear;
  });

  readonly years = computed<number[]>(() => {
    const options = this.periodOptions();
    return options?.Years.length ? options.Years : [this.year()];
  });

  /** Giá trị đang chọn của `<select>` kỳ — sentinel hoặc một mã kỳ thật. */
  readonly periodSelectValue = computed<string>(() =>
    this.mode() === 'year' ? PERIOD_YEAR : this.period(),
  );


  // ===== Toolbar của bảng chi tiết =====

  /** Chỉ hai điều kiện lọc ở màn này (Q22) — kỳ và năm là phép CHỌN, không phải điều kiện lọc. */
  readonly activeFilterCount = computed(() => (this.groupFilter() ? 1 : 0) + (this.statusFilter() ? 1 : 0));

  readonly requestParams = computed<IDashboardParams>(() => {
    const clean = this.sanitized();
    const option = this.periodChoices().find((item) => item.Value === clean.Period);

    return {
      Mode: clean.Mode,
      // Kỳ cụ thể ⇒ gửi `date` của mốc đầu kỳ; không có kỳ cụ thể ⇒ gửi `year` và để server chọn
      // kỳ hiện tại. Gửi ĐÚNG MỘT trong hai là lựa chọn của FE, không phải ràng buộc của hợp đồng:
      // Q61 chỉ nói về ca gửi **CẢ HAI** mà lệch nhau (`year` ≠ năm ISO của tuần chứa `date` ⇒
      // `400 DASHBOARD.PERIOD_YEAR_MISMATCH`), và Q63 nói bỏ trống `year` thì lấy năm ISO của
      // `date`. Gửi một cái thì không có gì để mà lệch — đó là lý do nhánh này an toàn nhất.
      //
      // 🛑 `option === undefined` là ĐƯỜNG ĐI HỢP LỆ, không phải ca cần vá: DB-3 khai rõ một năm
      // chưa có dữ liệu trả `weeksInYear` **rỗng** kèm `200`. Lúc đó FE **không** có kỳ nào để neo
      // và **không được** tự dựng một `date` trong năm đó — quy đổi lịch ISO là việc của BE
      // (Q40, `spec/danh-muc-dti/business-rules.md` §5.1). Gửi `year` trơ và để server quyết.
      Date: clean.Mode !== 'year' && option ? toPlainDate(option.StartDate) : undefined,
      Year: clean.Mode === 'year' || !option ? clean.Year : undefined,
      Search: clean.Search || undefined,
      GroupId: clean.GroupId || undefined,
      Status: clean.Status || undefined,
    };
  }, { equal: sameParams });


  constructor() {
    this.yearRequests
      .pipe(
        distinctUntilChanged(),
        switchMap((year) => this.periodService.getPeriodOptions(year).pipe(catchError(() => of(null)))),
        takeUntilDestroyed(),
      )
      .subscribe((value) => this.periodOptions.set(value));

    effect(() => this.yearRequests.next(this.year()));

    this.searchSubject
      .pipe(debounceTime(DEBOUNCE_MS), distinctUntilChanged(), takeUntilDestroyed())
      .subscribe((text) => {
        this.lastPushedSearch = text;
        this.patchQueryParams({ q: text || null });
      });

    effect(() => {
      const fromUrl = this.searchText();
      if (fromUrl === this.lastPushedSearch) return;
      this.lastPushedSearch = fromUrl;
      this.searchInput.set(fromUrl);
    });

    effect(() => {
      this.groupDraft.set(this.groupFilter());
      this.statusDraft.set(this.statusFilter());
    });

  }

  /**
   * 🛑 Gọi khi `isClean()` là `false`: viết lại URL, và trang KHÔNG gọi API ở nhịp đó.
   *
   * Lần điều hướng này làm `queryParamMap` phát lại, effect của trang chạy lần hai, và lúc đó
   * `isClean()` đã đúng — nên phép làm sạch xảy ra **trước** lần gọi DB-1 đầu tiên.
   */
  rewriteCleanUrl(): void {
    const clean = this.sanitized();
    this.patchQueryParams({
      mode: clean.Mode === 'week' ? null : clean.Mode,
      year: clean.Year === this.currentYear ? null : clean.Year,
      period: clean.Period || null,
      groupId: clean.GroupId || null,
      status: clean.Status || null,
    });
  }


  onModeChange(mode: DashboardMode): void {
    // Đổi đơn vị kỳ thì kỳ đang chọn không còn nghĩa — `2026-W33` không phải một tháng.
    this.patchQueryParams({ mode: mode === 'week' ? null : mode, period: null });
  }

  onPeriodSelect(event: Event): void {
    const value = (event.target as HTMLSelectElement).value;
    if (value === PERIOD_YEAR) {
      this.patchQueryParams({ mode: 'year', period: null });
      return;
    }
    // Rời chế độ `Tất cả` thì quay về Tuần — `.segmented` chỉ có hai nút, và Tuần là mặc định.
    const mode = this.mode() === 'year' ? 'week' : this.mode();
    this.patchQueryParams({ mode: mode === 'week' ? null : mode, period: value || null });
  }

  onYearChange(event: Event): void {
    const value = Number((event.target as HTMLSelectElement).value);
    // Đổi năm thì kỳ của năm cũ không còn trong danh sách — bỏ nó thay vì để server từ chối.
    this.patchQueryParams({ year: value === this.currentYear ? null : value, period: null });
  }

  onSearchValueChange(value: string): void {
    this.searchInput.set(value);
    this.searchSubject.next(value);
  }

  onGroupDraftChange(event: Event): void {
    this.groupDraft.set((event.target as HTMLSelectElement).value);
  }

  onStatusDraftChange(event: Event): void {
    this.statusDraft.set((event.target as HTMLSelectElement).value);
  }

  onSortChange(event: Event): void {
    this.sortBy.set((event.target as HTMLSelectElement).value as DashboardSort);
  }

  onApplyFilters(): void {
    this.patchQueryParams({ groupId: this.groupDraft() || null, status: this.statusDraft() || null });
  }

  onClearFilters(): void {
    this.patchQueryParams({ groupId: null, status: null });
  }

  onRemoveFilter(key: string): void {
    const patch: Params = {};
    patch[key] = null;
    this.patchQueryParams(patch);
  }

  /** Hàng lịch sử: nạp lại TOÀN TRANG theo kỳ đó — đổi query param, không mở dialog. */
  onViewPeriod(option: IPeriodOption): void {
    const mode: DashboardMode = option.Unit === 'week' ? 'week' : 'month';
    this.patchQueryParams({ mode: mode === 'week' ? null : mode, period: option.Value });
  }

  private patchQueryParams(patch: Params): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: patch,
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
  }
}

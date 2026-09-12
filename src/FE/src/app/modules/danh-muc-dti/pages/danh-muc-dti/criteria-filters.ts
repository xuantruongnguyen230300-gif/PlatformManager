import { Injectable, computed, effect, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Params, Router } from '@angular/router';
import { Subject, catchError, debounceTime, distinctUntilChanged, of, switchMap } from 'rxjs';
import { CRITERIA_STATUSES } from '../../../../shared/models/dti-criteria-status.model';
import { IPeriodOption, IPeriodOptions } from '../../../../shared/models/dti-period.model';
import { DtiPeriodService } from '../../../../shared/services/dti-period.service';
import { ICriteriaGroup, ICriteriaListParams } from '../../models/danh-muc-dti.model';
import { DanhMucDtiService } from '../../services/danh-muc-dti.service';
import { PERIOD_ALL } from '../../services/write-period';

const DEBOUNCE_MS = 300;

/** Q19 — bằng đúng lưới `Quản trị người dùng` của Core. Một mặc định cho cả sản phẩm. */
export const DEFAULT_PAGE_SIZE = 10;

/** Ô chọn số dòng của `DataTable` chỉ có ba giá trị; giá trị khác trên URL là "ngoài miền" (Q62). */
const PAGE_SIZE_OPTIONS: readonly number[] = [10, 20, 50];

/** Miền hợp lệ của `year` trên URL. Ngoài khoảng này là dữ liệu rác, không phải một năm cũ. */
const YEAR_MIN = 2000;
const YEAR_MAX = 2100;

const WEEK_PERIOD = /^\d{4}-W\d{1,2}$/;
const MONTH_PERIOD = /^\d{4}-\d{2}$/;
const GUID = /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/;

/** Bộ lọc đã LÀM SẠCH — mọi giá trị dưới đây chắc chắn nằm trong miền của ô lọc tương ứng. */
interface ISanitizedFilters {
  readonly Search: string;
  readonly GroupId: string;
  readonly Status: string;
  readonly Year: number;
  readonly Period: string;
  readonly Page: number;
  readonly PageSize: number;
}

/** So hai bộ lọc theo TỪNG TRƯỜNG — xem `equal` của `sanitized`. */
function sameFilters(a: ISanitizedFilters, b: ISanitizedFilters): boolean {
  return (
    a.Search === b.Search &&
    a.GroupId === b.GroupId &&
    a.Status === b.Status &&
    a.Year === b.Year &&
    a.Period === b.Period &&
    a.Page === b.Page &&
    a.PageSize === b.PageSize
  );
}

function readInt(raw: string | null): number | null {
  if (raw === null || raw.trim() === '') return null;
  const value = Number(raw);
  return Number.isInteger(value) ? value : null;
}

/**
 * BỘ LỌC của màn Danh mục DTI: đọc/ghi URL, làm sạch tham số lạ, giữ bản nháp panel lọc, và nạp
 * hai danh sách nuôi ô lọc (nhóm chỉ tiêu — DM-1; năm & kỳ — DM-8/DB-3).
 *
 * Tách khỏi `DanhMucDtiPage` (vòng 2, 2026-09-11) theo đúng khuôn `user-list-feed.ts` /
 * `user-form-flow.ts` của màn Core: lớp này **không biết** lưới tồn tại, không biết có mấy hộp
 * thoại, không biết dải băng nói gì. Nó trả lời đúng một câu — *"người dùng đang lọc gì"* — và
 * trang dùng câu đó để quyết định tải cái gì.
 *
 * 🛑 `@Injectable()` KHÔNG `providedIn: 'root'` — khai trong `providers` của trang nên vòng đời
 * trùng vòng đời trang, y hệt các `signal()` mà nó thay thế. Lên `root` là để bộ lọc của lượt xem
 * trước sống sót qua điều hướng rồi ghi đè URL của lượt mới.
 *
 * ## URL là nguồn sự thật (fe-routing-guard.md §8)
 *
 * `q`/`groupId`/`status`/`year`/`period`/`page`/`pageSize` đọc từ `queryParamMap` và ghi ngược lại
 * bằng `replaceUrl: true`. Không giữ bản sao trong `signal()`: hai nguồn cho cùng một giá trị sẽ
 * lệch nhau, và đó là thứ làm F5 mất bộ lọc, link gửi đi mở ra danh sách khác, nút Back nhảy khỏi
 * trang thay vì lùi một bước lọc.
 *
 * Ngoại lệ có chủ đích, đều thuộc cột "ở lại trong signal" của §8: bản nháp bộ lọc (chưa bấm
 * `Áp dụng`), chuỗi đang gõ trong ô tìm kiếm, dữ liệu đã tải.
 *
 * ## Q62 — tham số lạ trên URL: về mặc định, sửa URL, KHÔNG báo, KHÔNG gọi API
 *
 * `isClean()` so bộ lọc đã làm sạch với thứ đang thật sự nằm trên URL. Lệch ⇒ trang ghi lại URL và
 * **không** gọi DM-2 ở nhịp đó. Việc làm sạch vì vậy xảy ra **trước** lần gọi đầu tiên — người
 * dùng bình thường không bao giờ gặp `CRITERIA.STATUS_INVALID` /
 * `CRITERIA.ASSESSMENT_PERIOD_INVALID`, còn BE vẫn giữ hai mã đó làm lưới chặn cho nơi gọi khác.
 */
@Injectable()
export class CriteriaFilters {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly service = inject(DanhMucDtiService);
  private readonly periodService = inject(DtiPeriodService);

  /**
   * Năm mặc định của ô `Năm đánh giá` = năm DƯƠNG LỊCH hiện tại (T8).
   *
   * 🛑 Đây KHÔNG phải ngoại lệ của luật "không tự suy kỳ bằng đồng hồ máy khách". Luật đó cấm quy
   * một mã kỳ ra khoảng ngày, và cấm tự tính **tuần ISO** — tuần 1 có thể bắt đầu từ tháng 12 năm
   * trước, nên năm ISO ≠ năm dương lịch. "Hôm nay thuộc năm nào" thì không có chỗ nào để lệch.
   * Điều FE **không** quyết là *kỳ đang xem có thuộc năm hiện tại hay không* — đó là `isCurrentYear`
   * của DM-2 (Q66), và dải băng đọc từ đó chứ không từ dòng này.
   */
  readonly currentYear = new Date().getFullYear();

  /** `requireSync` được: `queryParamMap` phát ngay khi subscribe, nên không có nhịp "chưa biết bộ lọc". */
  private readonly queryMap = toSignal(this.route.queryParamMap, { requireSync: true });

  readonly groups = signal<ICriteriaGroup[]>([]);
  readonly periodOptions = signal<IPeriodOptions | null>(null);

  readonly statuses = CRITERIA_STATUSES;

  /**
   * Miền của `groupId` gồm cả "phải có trong danh sách DM-1" (§2), nhưng danh sách đó về SAU. Nên
   * lúc chưa tải xong thì chỉ kiểm khuôn GUID — loại bỏ một `groupId` hợp lệ chỉ vì response chưa
   * về sẽ xoá bộ lọc của một link người dùng vừa dán.
   *
   * 🛑 `equal` KHÔNG phải tối ưu — nó là ĐIỀU KIỆN ĐÚNG.
   *
   * `computed` mặc định so bằng `Object.is`, mà hàm này trả về một object literal MỚI mỗi lần
   * chạy. Nó phụ thuộc `groups()` (miền của `groupId` gồm cả "có trong danh sách DM-1"), nên khi
   * DM-1 về, `sanitized` sinh một object khác — giá trị y hệt — và `effect` gọi API ở trang chạy
   * LẦN HAI: hai request `/criteria` giống hệt nhau cho một lần vào màn.
   *
   * Đây là lỗi từng làm 15 test đỏ ở chính spec của trang, và triệu chứng thì đánh lạc hướng: lượt
   * tải thứ hai đặt `loading = true` trở lại, nên dải băng T9 và dòng "không khớp bộ lọc" biến mất
   * — trông như lỗi hiển thị chứ không như lỗi gọi API.
   */
  private readonly sanitized = computed<ISanitizedFilters>(
    () => {
      const map = this.queryMap();
      const rawGroup = map.get('groupId') ?? '';
      const knownGroups = this.groups();
      const groupIsKnown = knownGroups.length === 0 || knownGroups.some((group) => group.Id === rawGroup);

      const rawStatus = map.get('status') ?? '';
      const rawPeriod = map.get('period') ?? PERIOD_ALL;
      const rawYear = readInt(map.get('year'));
      const rawPage = readInt(map.get('page'));
      const rawPageSize = readInt(map.get('pageSize'));

      return {
        // Ô tìm kiếm là chuỗi tự do — không có "giá trị lạ" nào để làm sạch.
        Search: map.get('q') ?? '',
        GroupId: GUID.test(rawGroup) && groupIsKnown ? rawGroup : '',
        Status: (CRITERIA_STATUSES as readonly string[]).includes(rawStatus) ? rawStatus : '',
        Year: rawYear !== null && rawYear >= YEAR_MIN && rawYear <= YEAR_MAX ? rawYear : this.currentYear,
        Period:
          rawPeriod === PERIOD_ALL || WEEK_PERIOD.test(rawPeriod) || MONTH_PERIOD.test(rawPeriod)
            ? rawPeriod
            : PERIOD_ALL,
        Page: rawPage !== null && rawPage >= 1 ? rawPage : 1,
        PageSize: rawPageSize !== null && PAGE_SIZE_OPTIONS.includes(rawPageSize) ? rawPageSize : DEFAULT_PAGE_SIZE,
      };
    },
    { equal: sameFilters },
  );

  /**
   * URL đang mang ĐÚNG bộ lọc đã làm sạch chưa. `false` ⇒ một tham số lạ đang nằm trên thanh địa
   * chỉ, và lần gọi API phải chờ tới sau khi URL được viết lại.
   *
   * So sánh với giá trị THÔ chứ không với giá trị đã chuẩn hoá: `?page=1` và không có `page` cùng
   * cho ra `Page = 1`, và ta **không** muốn viết lại URL chỉ để bỏ một tham số vô hại — mỗi lần
   * viết lại là một lần điều hướng.
   */
  readonly isClean = computed<boolean>(() => {
    const map = this.queryMap();
    const clean = this.sanitized();
    const same = (key: string, value: string): boolean => {
      const raw = map.get(key);
      return raw === null || raw === value;
    };
    return (
      same('groupId', clean.GroupId) &&
      same('status', clean.Status) &&
      same('year', `${clean.Year}`) &&
      same('period', clean.Period) &&
      same('page', `${clean.Page}`) &&
      same('pageSize', `${clean.PageSize}`)
    );
  });

  readonly searchText = computed(() => this.sanitized().Search);
  readonly groupFilter = computed(() => this.sanitized().GroupId);
  readonly statusFilter = computed(() => this.sanitized().Status);
  readonly yearFilter = computed(() => this.sanitized().Year);
  readonly periodFilter = computed(() => this.sanitized().Period);
  readonly page = computed(() => this.sanitized().Page);
  readonly pageSize = computed(() => this.sanitized().PageSize);

  /**
   * Chuỗi ĐANG GÕ. Vẫn là signal (không đọc thẳng `searchText()`) vì URL chỉ cập nhật sau debounce
   * 300 ms — bắt ô nhập chờ URL sẽ làm chữ hiện ra giật một nhịp sau phím gõ.
   */
  readonly searchInput = signal('');
  private readonly searchSubject = new Subject<string>();

  /** Giá trị `q` mà CHÍNH TRANG NÀY vừa đẩy lên URL — dùng để phân biệt hai chiều thay đổi. */
  private lastPushedSearch = '';

  readonly groupDraft = signal('');
  readonly statusDraft = signal('');
  readonly yearDraft = signal(this.currentYear);
  readonly periodDraft = signal(PERIOD_ALL);

  /** Danh sách kỳ của năm ĐANG CHỌN, tuần trước rồi tháng — đúng thứ tự bản dựng đã duyệt. */
  readonly periodChoices = computed<IPeriodOption[]>(() => {
    const options = this.periodOptions();
    if (!options) return [];
    return [...options.WeeksInYear, ...options.MonthsInYear];
  });

  readonly years = computed<number[]>(() => {
    const options = this.periodOptions();
    // BE bảo đảm `years` luôn kèm năm hiện tại; chưa tải xong thì ô vẫn phải có ít nhất năm đang
    // lọc, nếu không `<select>` sẽ không khớp giá trị nào và trông như đang lọc năm khác.
    return options?.Years.length ? options.Years : [this.yearFilter()];
  });

  /**
   * T8 — chỉ đếm điều kiện **khác mặc định**. `Năm = năm hiện tại` và `Kỳ = Tất cả` là mặc định
   * của chính bảng lọc nên không tính; một badge đọc `2` trên màn chưa ai lọc dạy người dùng bỏ
   * qua nó. Cùng luật đó áp cho `.filter-chips`, nên số chip luôn bằng số trên nút.
   */
  readonly activeFilterCount = computed(
    () =>
      (this.groupFilter() ? 1 : 0) +
      (this.statusFilter() ? 1 : 0) +
      (this.yearFilter() !== this.currentYear ? 1 : 0) +
      (this.periodFilter() !== PERIOD_ALL ? 1 : 0),
  );

  readonly requestParams = computed<ICriteriaListParams>(() => {
    const clean = this.sanitized();
    return {
      Page: clean.Page,
      PageSize: clean.PageSize,
      Year: clean.Year,
      Period: clean.Period,
      Search: clean.Search || undefined,
      GroupId: clean.GroupId || undefined,
      Status: clean.Status || undefined,
    };
  });

  /**
   * Một lượt tải danh sách kỳ. Đi qua `switchMap` chứ không `subscribe` thẳng trong `effect`: đổi
   * năm liên tiếp sẽ mở N request chồng nhau, và request CŨ về sau sẽ đè danh sách kỳ của năm MỚI.
   */
  private readonly yearRequests = new Subject<number>();

  constructor() {
    // DM-1: tải một lần lúc khởi tạo màn. Hỏng thì ô lọc nhóm rỗng — không chặn lưới, vì lưới đọc
    // được mà không cần danh sách nhóm.
    this.service
      .getGroups()
      .pipe(takeUntilDestroyed())
      .subscribe({ next: (value) => this.groups.set(value), error: () => this.groups.set([]) });

    // DB-3: danh sách kỳ NẠP LẠI theo năm đang lọc — `weeksInYear`/`monthsInYear` là của đúng năm
    // đó. Năm chưa có dữ liệu trả hai mảng RỖNG kèm 200; đó không phải lỗi (DB-3 § Mã lỗi), nên
    // `catchError` chỉ bắt hỏng thật và trả `null` để ô kỳ về rỗng thay vì giết dòng chảy.
    this.yearRequests
      .pipe(
        distinctUntilChanged(),
        switchMap((year) => this.periodService.getPeriodOptions(year).pipe(catchError(() => of(null)))),
        takeUntilDestroyed(),
      )
      .subscribe((value) => this.periodOptions.set(value));

    effect(() => this.yearRequests.next(this.yearFilter()));

    // Ô tìm kiếm: gõ → chờ 300 ms → GHI LÊN URL. Debounce ở TRANG chứ không ở `Toolbar` (mỗi màn
    // một ngưỡng), và nó chặn được cả request thừa lẫn mục history thừa.
    this.searchSubject
      .pipe(debounceTime(DEBOUNCE_MS), distinctUntilChanged(), takeUntilDestroyed())
      .subscribe((text) => {
        this.lastPushedSearch = text;
        this.patchQueryParams({ q: text || null, page: null });
      });

    // URL đổi từ bên ngoài (Back/Forward, dán link) → kéo ô nhập theo.
    effect(() => {
      const fromUrl = this.searchText();
      if (fromUrl === this.lastPushedSearch) return;
      this.lastPushedSearch = fromUrl;
      this.searchInput.set(fromUrl);
    });

    // Bản nháp luôn khởi lại từ điều kiện ĐANG ÁP: mở panel ra phải thấy đúng thứ đang lọc, kể cả
    // khi điều kiện vừa bị gỡ bằng chip hoặc bị đổi bằng nút Back.
    effect(() => {
      this.groupDraft.set(this.groupFilter());
      this.statusDraft.set(this.statusFilter());
      this.yearDraft.set(this.yearFilter());
      this.periodDraft.set(this.periodFilter());
    });
  }

  /** 🛑 Gọi khi `isClean()` là `false`: viết lại URL, và KHÔNG gọi API ở nhịp đó. */
  rewriteCleanUrl(): void {
    const clean = this.sanitized();
    this.patchQueryParams({
      groupId: clean.GroupId || null,
      status: clean.Status || null,
      year: clean.Year === this.currentYear ? null : clean.Year,
      period: clean.Period === PERIOD_ALL ? null : clean.Period,
      page: clean.Page === 1 ? null : clean.Page,
      pageSize: clean.PageSize === DEFAULT_PAGE_SIZE ? null : clean.PageSize,
    });
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

  onYearDraftChange(event: Event): void {
    this.yearDraft.set(Number((event.target as HTMLSelectElement).value));
  }

  onPeriodDraftChange(event: Event): void {
    this.periodDraft.set((event.target as HTMLSelectElement).value);
  }

  /** Lọc lại thì trang hiện tại vô nghĩa — trang 5 của kết quả cũ thường rỗng ở kết quả mới. */
  applyFilters(): void {
    this.patchQueryParams({
      groupId: this.groupDraft() || null,
      status: this.statusDraft() || null,
      year: this.yearDraft() === this.currentYear ? null : this.yearDraft(),
      period: this.periodDraft() === PERIOD_ALL ? null : this.periodDraft(),
      page: null,
    });
  }

  /** `Xoá lọc` KHÔNG xoá từ khoá tìm kiếm — nó là một control riêng, ngoài bảng lọc. */
  clearFilters(): void {
    this.patchQueryParams({ groupId: null, status: null, year: null, period: null, page: null });
  }

  removeFilter(key: string): void {
    const patch: Params = { page: null };
    patch[key] = null;
    this.patchQueryParams(patch);
  }

  changePage(page: number, pageSize: number): void {
    this.patchQueryParams({
      page: page === 1 ? null : page,
      pageSize: pageSize === DEFAULT_PAGE_SIZE ? null : pageSize,
    });
  }

  /**
   * `merge` để mỗi lời gọi chỉ nói về tham số nó quan tâm; `null` xoá hẳn tham số khỏi URL (giá
   * trị mặc định không nên chiếm chỗ trên thanh địa chỉ). `replaceUrl: true` — đổi bộ lọc không
   * được đẻ ra mục history mới.
   */
  private patchQueryParams(patch: Params): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: patch,
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
  }
}
